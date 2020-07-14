use crate::*;
use crate::{syscall_defs::*};
use memory_block::{MemoryBlock, Protection};
use std::{os::raw::c_char, ffi::CStr, sync::Arc, ptr::null};
use fs::{FileDescriptor, FileSystem/*, MissingFileCallback*/};
use elf::ElfLoader;
use cinterface::MemoryLayoutTemplate;
use goblin::elf::Elf;
use context::{CALLBACK_SLOTS, Context, ExternalCallback, thunks::ThunkManager, ContextCallInfo};
use parking_lot::{RwLock, Mutex, RwLockWriteGuard};
use threading::{FutexWord, GuestThreadSet, futex_wait, futex_wake, futex_requeue, futex_unlock_pi, futex_lock_pi};

pub struct Environment {
	pub fs: Mutex<FileSystem>,
	pub program_break: Mutex<usize>,
	pub memory_block: Mutex<Box<MemoryBlock>>,
	pub threads: Mutex<GuestThreadSet>,
	pub layout: WbxSysLayout,
	pub context_call_info: ContextCallInfo,
	/// See comments on MemoryBlock::mirror_displacement
	pub mirror_displacement: usize,
	/// Threads can copy this to get their own handle to the environment
	pub cloneable_env_ref: *const Arc<RwLock<Environment>>,
}
unsafe impl Sync for Environment {}
unsafe impl Send for Environment {}
impl Environment {
	pub fn futex_word(&self, addr: usize) -> FutexWord {
		FutexWord {
			addr,
			mirror: addr.wrapping_add(self.mirror_displacement),
		}
	}
}

pub struct WaterboxHost {
	elf: ElfLoader,
	sealed: bool,
	image_file: Vec<u8>,
	main_thread_context: Context,
	thunks: ThunkManager,
	env: Arc<RwLock<Environment>>,
	/// Whenever the host is not active, hold this lock to prevent threads from entering
	inactive_lock: Option<RwLockWriteGuard<'static, Environment>>,
}
impl WaterboxHost {
	pub fn new(image_file: Vec<u8>, module_name: &str, layout_template: &MemoryLayoutTemplate) -> anyhow::Result<Box<WaterboxHost>> {
		let thunks = ThunkManager::new()?;
		let wbx = Elf::parse(&image_file[..])?;
		let elf_addr = ElfLoader::elf_addr(&wbx);
		let layout = layout_template.make_layout(elf_addr)?;
		let mut memory_block = MemoryBlock::new(layout.all());
		let elf = ElfLoader::new(&wbx, &image_file[..], module_name, &layout, &mut memory_block)?;
		let fs = FileSystem::new();
		let mirror_displacement = memory_block.mirror_displacement();

		unsafe { gdb::register(&image_file[..]) }
		let mut res = Box::new(WaterboxHost {
			// fs,
			// program_break: layout.sbrk.start,
			elf,
			// layout,
			// memory_block,
			sealed: false,
			image_file,
			main_thread_context: Context::new(1, null(), layout.main_thread.end()),
			thunks,
			env: Arc::new(RwLock::new(Environment {
				fs: Mutex::new(fs),
				program_break: Mutex::new(layout.sbrk.start),
				memory_block: Mutex::new(memory_block),
				threads: Mutex::new(GuestThreadSet::new()),
				layout,
				context_call_info: ContextCallInfo {
					dispatch_syscall: syscall,
					host_ptr: null(),
					extcall_slots: [None; 64],
				},
				mirror_displacement,
				cloneable_env_ref: null(),
			})),
			inactive_lock: None,
		});
		res.main_thread_context.context_call_info = &res.env.write().context_call_info;
		res.env.write().cloneable_env_ref = &res.env as *const _;
		res.deactivate(); // With no lock, we actually start as "active", but not really
		res.activate();
		println!("Calling _start()");
		res.run_guest_simple(res.elf.entry_point());
		res.deactivate();

		Ok(res)
	}

	pub fn active(&self) -> bool {
		match self.inactive_lock {
			None => false,
			_ => true,
		}
	}

	pub fn activate(&mut self) {
		if !self.active() {
			let mut env = std::mem::replace(&mut self.inactive_lock, None).unwrap();
			context::prepare_thread();
			env.context_call_info.host_ptr = &*env as *const Environment;
			env.memory_block.get_mut().activate();
		}
	}

	pub fn deactivate<'a>(&'a mut self) {
		if self.active() {
			let mut env = self.env.write();
			env.context_call_info.host_ptr = null();
			env.memory_block.get_mut().deactivate();
			self.inactive_lock = Some(unsafe { std::mem::transmute::<RwLockWriteGuard<'a, Environment>, RwLockWriteGuard<'static, Environment>>(env) });
		}
	}

	/// run code against the environment, temporarily acquiring a write lock if needed
	fn with_env_mut<R, F: FnOnce(&mut Environment) -> R>(&mut self, f: F) -> R {
		match &mut self.inactive_lock {
			Some(env) => f(&mut *env),
			None => f(&mut *self.env.write())
		}
	}

	/// run code against the environment, temporarily acquiring a read lock if needed
	fn with_env<R, F: FnOnce(&Environment) -> R>(&mut self, f: F) -> R {
		match &mut self.inactive_lock {
			Some(env) => f(&*env),
			None => f(&*self.env.read())
		}
	}
}

impl Drop for WaterboxHost {
	fn drop(&mut self) {
		self.deactivate();
		let env = std::mem::replace(&mut self.inactive_lock, None).unwrap();
		unsafe { gdb::deregister(&self.image_file[..]) }
		drop(env);
	}
}

impl WaterboxHost {
	pub fn get_external_callback_ptr(&mut self, callback: ExternalCallback, slot: usize) -> anyhow::Result<usize> {
		if slot >= CALLBACK_SLOTS {
			Err(anyhow!("slot must be less than {}", CALLBACK_SLOTS))
		} else {
			self.with_env_mut(|e| e.context_call_info.extcall_slots[slot] = Some(callback));
			Ok(context::get_callback_ptr(slot))
		}
	}
	pub fn get_external_callin_ptr(&mut self, ptr: usize) -> anyhow::Result<usize> {
		self.thunks.get_thunk_for_proc(ptr, &mut self.main_thread_context as *mut Context)
	}
	pub fn get_proc_addr(&mut self, name: &str) -> anyhow::Result<usize> {
		let ptr = self.elf.get_proc_addr(name);
		if ptr == 0 {
			Ok(0)
		} else {
			self.thunks.get_thunk_for_proc(ptr, &mut self.main_thread_context as *mut Context)
		}
	}
	pub fn get_proc_addr_raw(&mut self, name: &str) -> anyhow::Result<usize> {
		let ptr = self.elf.get_proc_addr(name);
		Ok(ptr)
	}
	fn check_sealed(&self) -> anyhow::Result<()> {
		if !self.sealed {
			Err(anyhow!("Not sealed!"))
		} else {
			Ok(())
		}
	}
	pub fn seal(&mut self) -> anyhow::Result<()> {
		if self.sealed {
			return Err(anyhow!("Already sealed!"))
		}

		let was_active = self.active();
		self.activate();

		fn run_proc(h: &mut WaterboxHost, name: &str) {
			match h.elf.get_proc_addr(name) {
				0 => (),
				ptr => {
					println!("Calling {}()", name);
					h.run_guest_simple(ptr);
				},
			}
		}
		run_proc(self, "co_clean");
		run_proc(self, "ecl_seal");

		let mut env = self.env.write();
		self.elf.seal(env.memory_block.get_mut());
		env.memory_block.get_mut().seal()?;
		drop(env);

		if !was_active {
			self.deactivate();
		}
		self.sealed = true;
		Ok(())
	}
	pub fn mount_file(&mut self, name: String, data: Vec<u8>, writable: bool) -> anyhow::Result<()> {
		self.with_env(|e| e.fs.lock().mount(name, data, writable))
	}
	pub fn unmount_file(&mut self, name: &str) -> anyhow::Result<Vec<u8>> {
		self.with_env(|e| e.fs.lock().unmount(name))
	}
	// pub fn set_missing_file_callback(&mut self, cb: Option<MissingFileCallback>) {
	// 	self.fs.set_missing_file_callback(cb);
	// }

	/// Run a guest entry point that takes no arguments
	pub fn run_guest_simple(&mut self, entry_point: usize) {
		context::call_guest_simple(entry_point, &mut self.main_thread_context);
	}
}

const SAVE_START_MAGIC: &str = "ActivatedWaterboxHost_v1";
const SAVE_END_MAGIC: &str = "ʇsoHxoqɹǝʇɐMpǝʇɐʌᴉʇɔ∀";
impl IStateable for WaterboxHost {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::write_magic(stream, SAVE_START_MAGIC)?;
		self.elf.save_state(stream)?;
		self.with_env_mut(|e| {
			e.fs.get_mut().save_state(stream)?;
			bin::write(stream, e.program_break.get_mut())?;
			e.memory_block.get_mut().save_state(stream)?;
			bin::write_magic(stream, SAVE_END_MAGIC)?;
			Ok(())
		})
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::verify_magic(stream, SAVE_START_MAGIC)?;
		self.elf.load_state(stream)?;
		self.with_env_mut(|e| {
			e.fs.get_mut().load_state(stream)?;
			bin::read(stream, e.program_break.get_mut())?;
			e.memory_block.get_mut().load_state(stream)?;
			bin::verify_magic(stream, SAVE_END_MAGIC)?;
			Ok(())
		})
	}
}

fn unimp(nr: SyscallNumber) -> SyscallResult {
	eprintln!("Stopped on unimplemented syscall {}", lookup_syscall(&nr));
	unsafe { std::intrinsics::breakpoint() }
	Err(ENOSYS)
}

fn arg_to_prot(arg: usize) -> Result<Protection, SyscallError> {
	use Protection::*;
	if arg != arg & (PROT_READ | PROT_WRITE | PROT_EXEC) {
		Err(EINVAL)
	} else if arg & PROT_EXEC != 0 {
		if arg & PROT_WRITE != 0 {
			Ok(RWX)
		} else {
			Ok(RX)
		}
	} else if arg & PROT_WRITE != 0 {
		Ok(RW)
	} else if arg & PROT_READ != 0 {
		Ok(R)
	} else {
		Ok(None)
	}
}

fn arg_to_fd(arg: usize) -> Result<FileDescriptor, SyscallError> {
	if arg < 0x80000000 {
		Ok(FileDescriptor(arg as i32))
	} else {
		Err(EBADFD)
	}
}

fn arg_to_str(arg: usize) -> Result<String, SyscallError> {
	let cs = unsafe { CStr::from_ptr(arg as *const c_char) };
	match cs.to_str() {
		Ok(s) => Ok(s.to_string()),
		Err(_) => Err(EINVAL),
	}
}

fn arg_to_statbuff<'a>(arg: usize) -> &'a mut KStat {
	unsafe { &mut *(arg as *mut KStat) }
}

pub extern "sysv64" fn syscall(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, _a6: usize,
	nr: SyscallNumber, h: &Environment
) -> SyscallReturn {
	match nr {
		NR_MMAP => {
			let mut prot = arg_to_prot(a3)?;
			let flags = a4;
			if flags & MAP_ANONYMOUS == 0 {
				// anonymous + private is easy
				// anonymous by itself is hard
				// nothing needs either right now
				return syscall_err(EOPNOTSUPP)
			}
			if flags & 0xf00 != 0 {
				// various unsupported flags
				return syscall_err(EOPNOTSUPP)
			}
			if flags & MAP_STACK != 0 {
				if prot == Protection::RW {
					prot = Protection::RWStack;
				} else {
					return syscall_err(EINVAL) // stacks must be readable and writable
				}
			}
			let no_replace = flags & MAP_FIXED_NOREPLACE != 0;
			let arena_addr = h.layout.mmap;
			let res = h.memory_block.lock().mmap(AddressRange { start: a1, size: a2 }, prot, arena_addr, no_replace)?;
			syscall_ok(res)
		},
		NR_MREMAP => {
			let arena_addr = h.layout.mmap;
			let res = h.memory_block.lock().mremap(AddressRange { start: a1, size: a2 }, a3, arena_addr)?;
			syscall_ok(res)
		},
		NR_MPROTECT => {
			let prot = arg_to_prot(a3)?;
			let res = h.memory_block.lock().mprotect(AddressRange { start: a1, size: a2 }, prot);
			syscall_ret(res)
		},
		NR_MUNMAP => syscall_ret(h.memory_block.lock().munmap(AddressRange { start: a1, size: a2 })),
		NR_MADVISE => {
			match a3 {
				MADV_DONTNEED => syscall_ret(h.memory_block.lock().madvise_dontneed(AddressRange { start: a1, size: a2 })),
				_ => syscall_ok(0),
			}
		},
		NR_STAT => {
			let name = arg_to_str(a1)?;
			syscall_ret(h.fs.lock().stat(&name, arg_to_statbuff(a2)))
		},
		NR_FSTAT => {
			syscall_ret(h.fs.lock().fstat(arg_to_fd(a1)?, arg_to_statbuff(a2)))
		},
		NR_IOCTL => syscall_ok(0),
		NR_READ => {
			unsafe {
				syscall_ret_i64(h.fs.lock().read(arg_to_fd(a1)?, std::slice::from_raw_parts_mut(a2 as *mut u8, a3)))
			}
		},
		NR_WRITE => {
			unsafe {
				syscall_ret_i64(h.fs.lock().write(arg_to_fd(a1)?, std::slice::from_raw_parts(a2 as *const u8, a3)))
			}
		},
		NR_READV => {
			let fd = arg_to_fd(a1)?;
			unsafe {
				let mut ret = 0;
				let iov = std::slice::from_raw_parts_mut(a2 as *mut Iovec, a3);
				let mut fs = h.fs.lock();
				for io in iov {
					if io.iov_base != 0 {
						ret += fs.read(fd, io.slice_mut())?;
					}
				}
				syscall_ok(ret as usize)
			}
		},
		NR_WRITEV => {
			let fd = arg_to_fd(a1)?;
			unsafe {
				let mut ret = 0;
				let iov = std::slice::from_raw_parts(a2 as *const Iovec, a3);
				let mut fs = h.fs.lock();
				for io in iov {
					if io.iov_base != 0 {
						ret += fs.write(fd, io.slice())?;
					}
				}
				syscall_ok(ret as usize)
			}
		},
		NR_OPEN => {
			syscall_ret_val(h.fs.lock().open(&arg_to_str(a1)?, a2 as i32, a3 as i32).map(|x| x.0 as usize))
		},
		NR_CLOSE => syscall_ret(h.fs.lock().close(arg_to_fd(a1)?)),
		NR_LSEEK => syscall_ret_i64(h.fs.lock().seek(arg_to_fd(a1)?, a2 as i64, a3 as i32)),
		NR_TRUNCATE => syscall_ret(h.fs.lock().truncate(&arg_to_str(a1)?, a2 as i64)),
		NR_FTRUNCATE => syscall_ret(h.fs.lock().ftruncate(arg_to_fd(a1)?, a2 as i64)),
		NR_SET_THREAD_AREA => syscall_err(ENOSYS), // musl handles this in userspace
		NR_SET_TID_ADDRESS => syscall_ok(threading::set_tid_address(a1) as usize),
		NR_SCHED_YIELD => syscall_ok(0),
		NR_CLOCK_GETTIME => {
			let ts = a2 as *mut TimeSpec;
			unsafe {
				(*ts).tv_sec = 1495889068;
				(*ts).tv_nsec = 0;
			}
			syscall_ok(0)
		},
		NR_BRK => {
			// TODO: This could be done on the C side
			let addr = h.layout.sbrk;
			let mut program_break = h.program_break.lock();
			let old = *program_break;
			let res = if a1 != align_down(a1) {
				old
			} else if a1 < addr.start {
				if a1 == 0 {
					println!("Initializing heap sbrk at {:x}:{:x}", addr.start, addr.end());
				}
				old
			} else if a1 > addr.end() {
				eprintln!("Failed to satisfy allocation of {} bytes on sbrk heap", a1 - old);
				old	
			} else if a1 > old {
				h.memory_block.lock().mmap_fixed(AddressRange { start: old, size: a1 - old }, Protection::RW, true).unwrap();
				println!("Allocated {} bytes on sbrk heap, usage {}/{}", a1 - old, a1 - addr.start, addr.size);
				a1
			} else {
				old
			};
			*program_break = res;
			syscall_ok(res)
		},
		NR_WBX_CLONE => {
			syscall_ret_val(
				h.threads.lock().spawn(h, a1, a2, a3 as *mut u32, a4)
					.map(|tid| tid as usize)
			)
		},
		NR_WBX_CLONE_IN_CHILD => syscall_ok(0),
		NR_EXIT => unsafe {
			let context = context::current_context();
			let tid = (*context).tid;
			let clear_tid_address = h.futex_word((*context).clear_child_tid);
			syscall_ret(h.threads.lock().exit(tid, clear_tid_address))
		},
		NR_FUTEX => {
			let op = a2 as i32 & !FUTEX_PRIVATE;
			let context = unsafe { &mut *context::current_context() };
			match op {
				FUTEX_WAIT => {
					syscall_ret(futex_wait(context, h.futex_word(a1), a3 as u32))
				},
				// int *uaddr, int futex_op, int val,
				// const struct timespec *timeout,   /* or: uint32_t val2 */
				// int *uaddr2, int val3
				FUTEX_WAKE => {
					syscall_ok(futex_wake(h.futex_word(a1), a3 as u32))
				},
				FUTEX_REQUEUE => {
					syscall_ret_val(
						futex_requeue(
							h.futex_word(a1),
							h.futex_word(a5),
							a3 as u32,
							a4 as u32
						)
					)
				},
				FUTEX_UNLOCK_PI => {
					futex_unlock_pi(h.futex_word(a1));
					syscall_ok(0)
				},
				FUTEX_LOCK_PI => {
					syscall_ret(futex_lock_pi(context, h.futex_word(a1)))
				},
				_ => syscall_err(ENOSYS),
			}
		},
		NR_RT_SIGPROCMASK => {
			// we don't (nor ever plan to?) deliver any signals to guests, so...
			syscall_ok(0)
		},
		_ => syscall_ret(unimp(nr)),
	}
}
