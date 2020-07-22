use crate::*;
use std::collections::{BTreeMap, HashMap};
use syscall_defs::*;
use memory_block::{MemoryBlock, Protection};
use context::Context;

#[derive(Eq, PartialEq, Debug)]
enum ThreadState {
	Runnable,
	Waiting,
}

struct GuestThread {
	tid: u32,
	state: ThreadState,
	/// The rax value (ie, return from most recent syscall) that will be returned to the guest
	/// when this thread is next run
	rax: SyscallReturn,
	/// the rsp value that will be returned to the guest when this thread is next run
	rsp: usize,
	/// pthread_self
	thread_area: usize,
	/// set_tid_address
	tid_address: usize,
}

struct UnparkResult {
	tid: u32,
	has_more: bool
}

pub struct GuestThreadSet {
	next_tid: u32,
	threads: BTreeMap<u32, GuestThread>,
	/// waiters for each futex
	futicies: HashMap<usize, Vec<u32>>,
	/// currently running thread
	active_tid: u32,
}
impl GuestThreadSet {
	pub fn new() -> GuestThreadSet {
		let mut threads = BTreeMap::new();
		threads.insert(1, GuestThread {
			tid: 1,
			state: ThreadState::Runnable,
			rax: syscall_ok(0),
			rsp: 0, // the real initial state of guest_rsp is preloaded into Context by WaterboxHost
			thread_area: 0,
			tid_address: 0,
		});
		GuestThreadSet {
			next_tid: 2,
			threads,
			futicies: HashMap::new(),
			active_tid: 1,
		}
	}
	/// yield to anyone
	/// If current thread is still runnable, might noop
	/// Stows the return value to this call, and returns whatever that last thread's return call was
	fn swap_to_next(&mut self, context: &mut Context, ret: SyscallReturn) -> SyscallReturn {
		// TODO: Something more strategic here
		let mut candidates = self.threads.range(self.active_tid + 1..)
			.chain(self.threads.range(..self.active_tid + 1))
			.filter(|kvp| kvp.1.state == ThreadState::Runnable);

		match candidates.next() {
			Some((&tid, _)) => {
				if tid == self.active_tid {
					// yield call failed to change thread.  totally legal; do nothing
					ret
				} else {
					self.swap_to(context, tid, ret)
				}
			},
			None => {
				panic!("All threads fell asleep")
			}
		}
	}
	/// yield to a particular thread
	/// Stows the return value to this call, and returns whatever that last thread's return call was
	fn swap_to(&mut self, context: &mut Context, tid: u32, ret: SyscallReturn) -> SyscallReturn {
		let old_thread = self.threads.get_mut(&self.active_tid).unwrap();
		old_thread.rax = ret;
		old_thread.rsp = context.guest_rsp;
		let new_thread = self.threads.get_mut(&tid).unwrap();
		assert_eq!(new_thread.state, ThreadState::Runnable);
		context.guest_rsp = new_thread.rsp;
		self.active_tid = tid;
		new_thread.rax
	}
	fn park_other(&mut self, addr: usize, tid: u32) {
		assert_ne!(self.active_tid, tid);
		self.futicies.entry(addr)
			.or_insert_with(Vec::new)
			.push(tid);
		self.threads.get_mut(&tid).unwrap().state = ThreadState::Waiting;
	}
	fn park_me(&mut self, context: &mut Context, ret: SyscallReturn, addr: usize) -> SyscallReturn {
		self.futicies.entry(addr)
			.or_insert_with(Vec::new)
			.push(self.active_tid);
		self.threads.get_mut(&self.active_tid).unwrap().state = ThreadState::Waiting;
		self.swap_to_next(context, ret)
	}
	fn unpark_one(&mut self, addr: usize) -> Option<UnparkResult> {
		if let Some(queue) = self.futicies.get_mut(&addr) {
			if queue.len() > 0 {
				let tid = queue.remove(0);
				self.threads.get_mut(&tid).unwrap().state = ThreadState::Runnable;
				if queue.len() == 0 {
					self.futicies.remove(&addr);
					Some(UnparkResult {
						tid,
						has_more: false
					})
				} else {
					Some(UnparkResult {
						tid,
						has_more: true
					})
				}
			} else {
				None
			}
		} else {
			None
		}
	}

	/// Similar to a limited subset of clone(2).
	/// flags are hardcoded to CLONE_VM | CLONE_FS
	/// | CLONE_FILES | CLONE_SIGHAND | CLONE_THREAD | CLONE_SYSVSEM
	/// | CLONE_SETTLS | CLONE_PARENT_SETTID | CLONE_CHILD_CLEARTID | CLONE_DETACHED.
	/// Child thread does not return to the same place the parent did; instead, it will begin at enter_guest_thread,
	/// which will `ret`, and accordingly the musl code arranges for an appropriate address to be on the stack.
	pub fn spawn(&mut self, memory_block: &mut MemoryBlock, thread_area: usize, guest_rsp: usize, parent_tid: *mut u32, child_tid: usize) -> Result<u32, SyscallError> {
		let tid = self.next_tid;

		unsafe {
			// peek inside the pthread struct to find the full area we must mark as stack-protected
			let pthread = std::slice::from_raw_parts(thread_area as *const usize, 13);
			let stack_end = pthread[12];
			let stack_size = pthread[13];
			let stack = AddressRange { start: stack_end - stack_size, size: stack_size };
			memory_block.mprotect(stack.align_expand(), Protection::RWStack)?;

			*parent_tid = tid;
		}

		let tid = self.next_tid;
		let thread = GuestThread {
			tid,
			state: ThreadState::Runnable,
			rax: syscall_ok(0),
			rsp: guest_rsp,
			thread_area,
			tid_address: child_tid,
		};
		self.threads.insert(tid, thread);
		self.next_tid += 1;
		Ok(tid)
	}

	pub fn exit(&mut self, context: &mut Context) -> SyscallReturn {
		if self.active_tid == 1 {
			unsafe { std::intrinsics::breakpoint() }
		}
		let addr = self.threads.get_mut(&self.active_tid).unwrap().tid_address;
		if addr != 0 {
			let atom = unsafe { &mut *(addr as *mut u32) };
			*atom = 0;
			self.unpark_one(addr);
		}
		let dead_tid = self.active_tid;
		let ret = self.swap_to_next(context, syscall_ok(0));
		if self.active_tid == dead_tid {
			panic!("Thread exited but no thread is available to replace it");
		}
		self.threads.remove(&dead_tid);
		ret
	}

	pub fn futex_wait(&mut self, context: &mut Context, addr: usize, compare: u32) -> SyscallReturn {
		let atom = unsafe { &mut *(addr as *mut u32) };
		if *atom != compare {
			syscall_err(EAGAIN)
		} else {
			self.park_me(context, syscall_ok(0), addr)
		}
	}
	
	pub fn futex_wake(&mut self, addr: usize, count: u32) -> usize {
		self.futex_requeue(addr, 0, count, 0)
			.unwrap()
	}

	pub fn futex_requeue(&mut self, addr_from: usize, addr_to: usize, mut wake_count: u32, mut requeue_count: u32) -> Result<usize, SyscallError> {
		// NB: musl only does wake_count = 0, requeue_count = 1
		let mut count = 0;

		while wake_count > 0 || requeue_count > 0 {
			if let Some(res) = self.unpark_one(addr_from) {
				count += 1;
				if wake_count > 0 {
					wake_count -= 1;
				} else {
					self.park_other(addr_to, res.tid);
					requeue_count -= 1;
				}
				if !res.has_more {
					break
				}
			} else {
				break
			}
		}

		Ok(count)
	}
	
	// don't handle priority inversion, or the clock information (how could we introduce a clock, anyway?)
	// use fair handoffs for simplicity
	pub fn futex_lock_pi(&mut self, context: &mut Context, addr: usize) -> SyscallReturn {
		let atom = unsafe { &mut *(addr as *mut u32) };
		if *atom == 0 {
			*atom = self.active_tid;
			return syscall_ok(0)
		}
		*atom |= FUTEX_WAITERS;
		self.park_me(context, syscall_ok(0), addr)
	}
	
	pub fn futex_unlock_pi(&mut self, context: &mut Context, addr: usize) -> SyscallReturn {
		let atom = unsafe { &mut *(addr as *mut u32) };
		match self.unpark_one(addr) {
			Some(res) => {
				if res.has_more {
					*atom = res.tid | FUTEX_WAITERS;
				} else {
					*atom = res.tid;
				}
				self.swap_to(context, res.tid, syscall_ok(0)) // "fair" unlock
			},
			None => {
				// unclear what to return here
				*atom = 0;
				syscall_ok(0)
			}
		}
	}
	
	pub fn set_tid_address(&mut self, addr: usize) -> u32 {
		let thread = self.threads.get_mut(&self.active_tid).unwrap();
		thread.tid_address = addr;
		thread.tid
	}

	pub fn yield_any(&mut self, context: &mut Context) -> SyscallReturn {
		self.swap_to_next(context, syscall_ok(0))
	}
}
