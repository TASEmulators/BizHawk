use crate::*;
use crate::{syscall_defs::*, context::Context, host::Environment, memory_block::Protection};
use std::sync::{Arc, atomic::{Ordering, AtomicU32, AtomicBool}};
use std::{thread::JoinHandle, mem::transmute};
use parking_lot_core::*;
use parking_lot::RwLock;

struct GuestThread {
	context: Context,
	native_thread: Option<JoinHandle<()>>,
	quit_signal: AtomicBool,
	environment: Arc<RwLock<Environment>>,
}

impl GuestThread {
	/// create a new thread that is ready to start executing code.
	pub fn new(cloneable_env_ref: *const Arc<RwLock<Environment>>, context: Context) -> Box<GuestThread> {
		unsafe {
			let res = Box::new(GuestThread {
				context,
				native_thread: None,
				quit_signal: AtomicBool::new(false),
				environment: (*cloneable_env_ref).clone(),
			});
			res
		}
	}
	fn start(&mut self) {
		let builder = std::thread::Builder::new()
			.name(format!("Waterbox thread guest TID {}", self.context.tid));
		
		unsafe {
			let handle = builder.spawn_unchecked(|| {
				while !self.quit_signal.load(Ordering::SeqCst) {
					{
						use std::ops::Try;

						let env = self.environment.read();
						let rax = host::syscall(
							self.context.rdi,
							self.context.rsi,
							self.context.rdx,
							self.context.rcx,
							self.context.r8,
							self.context.r9,
							SyscallNumber(self.context.rax),
							&*env
						);
						if rax == SyscallReturn::from_error(E_WBX_HOSTABORT) {
							continue
						}
						if rax == SyscallReturn::from_error(E_WBX_THREADEXIT) {
							self.quit_signal.store(true, Ordering::SeqCst); // signal that this thread quit successfully
							break
						}
						self.context.rax = rax.0;
						context::enter_guest_thread(&mut self.context);
					}
				}
			});
			self.native_thread = Some(handle.unwrap());
		}
	}
}
impl Drop for GuestThread {
	fn drop(&mut self) {
		if let Some(joiner) = std::mem::replace(&mut self.native_thread, None) {
			self.quit_signal.store(true, Ordering::SeqCst);
			let addr = self.context.park_addr.load(Ordering::SeqCst);
			if addr != 0 {
				unsafe {
					unpark_all(addr, HOST_ABORTED);
				}
			}
			let _ = joiner.join();
		}
	}
}

pub struct GuestThreadSet {
	next_tid: u32,
	threads: Vec<Box<GuestThread>>,
}

impl GuestThreadSet {
	pub fn new() -> GuestThreadSet {
		GuestThreadSet {
			next_tid: 2, // 1 is host thread, already created
			threads: Vec::new(),
		}
	}
	/// Similar to a limited subset of clone(2).
	/// flags are hardcoded to CLONE_VM | CLONE_FS
	/// | CLONE_FILES | CLONE_SIGHAND | CLONE_THREAD | CLONE_SYSVSEM
	/// | CLONE_SETTLS | CLONE_PARENT_SETTID | CLONE_CHILD_CLEARTID | CLONE_DETACHED.
	/// Child thread does not return to the same place the parent did; instead, it will begin at enter_guest_thread,
	/// which will `ret`, and accordingly the musl code arranges for an appropriate address to be on the stack.
	pub fn spawn(&mut self, env: &Environment, thread_area: usize, guest_rsp: usize, parent_tid: *mut u32, child_tid: usize) -> Result<u32, SyscallError> {
		let tid = self.next_tid;

		unsafe {
			// peek inside the pthread struct to find the full area we must mark as stack-protected
			let pthread = std::slice::from_raw_parts(thread_area as *const usize, 13);
			let stack_end = pthread[12];
			let stack_size = pthread[13];
			let stack = AddressRange { start: stack_end - stack_size, size: stack_size };
			env.memory_block.lock().mprotect(stack.align_expand(), Protection::RWStack)?;

			*parent_tid = tid;
		}

		let tid = self.next_tid;
		let mut context = Context::new(self.next_tid, &env.context_call_info, guest_rsp);
		context.thread_area = thread_area;
		context.clear_child_tid = child_tid;
		context.rax = NR_WBX_CLONE_IN_CHILD.0; // first syscall is to do the child part of startup (which is actually nothing)

		let mut g = GuestThread::new(env.cloneable_env_ref, context);
		g.start();
		self.threads.push(g);

		self.next_tid += 1;
		Ok(tid)
	}

	pub fn exit(&mut self, tid: u32, clear_tid_address: FutexWord) -> SyscallResult {
		if tid == 1 {
			unsafe { std::intrinsics::breakpoint() }
		}
		if clear_tid_address.addr != 0 {
			let atom = clear_tid_address.atom();
			atom.store(0, Ordering::SeqCst);
			futex_wake(clear_tid_address, 1);
		}
		// self.threads.retain(|x| x.context.tid == tid);
		Err(E_WBX_THREADEXIT)
	}
}

impl IStateable for GuestThreadSet {
	// TODO: Every syscall that returns E_WBX_HOSTABORT needs to be restartable in some way
	// TODO: Do we need to preserve the order of parking lot queues?
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		Err(anyhow!("NYI"))
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		Err(anyhow!("NYI"))	
	}
}

pub struct FutexWord {
	/// The address in the guest.  Used to read and write
	pub addr: usize,
	/// The address in the host's mirror space.  Used so we don't have to tear down parking lot queues
	/// on swapin / swapout (otherwise guests would "share" queues)
	pub mirror: usize,
}
impl FutexWord {
	/// atomic access in the guest space
	pub fn atom(&self) -> &AtomicU32 {
		unsafe { transmute(self.addr) }
	}
}

const HOST_ABORTED: UnparkToken = UnparkToken(1);

pub fn futex_wait(context: &mut Context, word: FutexWord, compare: u32) -> SyscallResult {
	let ret = unsafe {
		let atom = word.atom();
		let res = park(
			word.mirror,
			|| {
				atom.load(Ordering::SeqCst) == compare
			},
			|| {
				context.park_addr.store(word.mirror, Ordering::SeqCst)
			},
			|_, _| {},
			DEFAULT_PARK_TOKEN,
			None
		);
		match res {
			ParkResult::Invalid => {
				Err(EAGAIN)
			},
			ParkResult::Unparked(tok) if tok == DEFAULT_UNPARK_TOKEN => {
				Ok(())
			},
			ParkResult::Unparked(tok) if tok == HOST_ABORTED => {
				Err(E_WBX_HOSTABORT)
			},
			_ => panic!(),
		}
	};
	context.park_addr.store(0, Ordering::SeqCst);
	ret
}

pub fn futex_wake(word: FutexWord, count: u32) -> usize {
	let mut i = 0;
	unsafe {
		let res = unpark_filter(
			word.mirror,
			|_| {
				if i < count {
					i += 1;
					FilterOp::Unpark
				} else {
					FilterOp::Stop
				}
			},
			|_| DEFAULT_UNPARK_TOKEN
		);
		res.unparked_threads
	}
}

pub fn futex_requeue(word_from: FutexWord, word_to: FutexWord, wake_count: u32, requeue_count: u32) -> Result<usize, SyscallError> {
	let op = match (wake_count, requeue_count) {
		(0, 0) => return Ok(0),
		// musl only hits this variant
		(0, 1) => RequeueOp::RequeueOne, 
		(0, 0x7fffffff) => RequeueOp::RequeueAll,
		(1, 0) => RequeueOp::UnparkOne,
		(1, 0x7fffffff) => RequeueOp::UnparkOneRequeueRest,
		// parking_lot_core doesn't support all of the possibilities, so ehhhh
		_ => return Err(EINVAL),
	};

	unsafe {
		let res = unpark_requeue(
			word_from.mirror,
			word_to.mirror,
			|| op,
			|_, _| DEFAULT_UNPARK_TOKEN
		);
		Ok(res.unparked_threads + res.requeued_threads)
	}
}

// don't handle priority inversion, or the clock information (how could we introduce a clock, anyway?)
// always handoff ("fair") to reduce nondeterminism
pub fn futex_lock_pi(context: &mut Context, word: FutexWord) -> SyscallResult {
	unsafe {
		let atom = word.atom();
		let ret = loop {
			let owner = atom.compare_exchange_weak(
				0, context.tid, Ordering::SeqCst, Ordering::SeqCst);
			let owner_tid = match owner {
				Ok(_) => return Ok(()),
				Err(v) => v
			};
			let res = park(
				word.mirror,
				|| {
					atom.compare_exchange_weak(
						owner_tid, owner_tid | FUTEX_WAITERS, Ordering::SeqCst, Ordering::SeqCst
					).is_ok()
				},
				|| context.park_addr.store(word.mirror, Ordering::SeqCst),
				|_, _| {},
				DEFAULT_PARK_TOKEN,
				None
			);
			match res {
				ParkResult::Invalid => (),
				ParkResult::Unparked(tok) if tok == DEFAULT_UNPARK_TOKEN => {
					atom.store(atom.load(Ordering::SeqCst) & FUTEX_TID_MASK | context.tid, Ordering::SeqCst);
					break Ok(())
				},
				ParkResult::Unparked(tok) if tok == HOST_ABORTED => {
					break Err(E_WBX_HOSTABORT)
				},
				_ => panic!(),
			}
		};
		context.park_addr.store(0, Ordering::SeqCst);
		ret
	}
}

pub fn futex_unlock_pi(word: FutexWord) {
	unsafe {
		let atom = word.atom();
		unpark_one(
			word.mirror,
			|r| {
				if r.unparked_threads == 0 {
					atom.store(0, Ordering::SeqCst);
				} else if !r.have_more_threads {
					atom.fetch_and(!FUTEX_WAITERS, Ordering::SeqCst);
				}
				DEFAULT_UNPARK_TOKEN
			}
		);
	}
}

pub fn set_tid_address(addr: usize) -> u32 {
	unsafe {
		let context = context::current_context();
		(*context).clear_child_tid = addr;
		(*context).tid
	}
}
