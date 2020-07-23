use crate::*;
use crate::{syscall_defs::*};
use memory_block::{MemoryBlock, Protection};
use std::{os::raw::c_char, ffi::CStr};
use fs::{FileDescriptor, FileSystem/*, MissingFileCallback*/};
use elf::ElfLoader;
use cinterface::MemoryLayoutTemplate;
use goblin::elf::Elf;
use context::{CALLBACK_SLOTS, Context, ExternalCallback, thunks::ThunkManager};
use threading::GuestThreadSet;

pub struct WaterboxHost {
	fs: FileSystem,
	program_break: usize,
	elf: ElfLoader,
	layout: WbxSysLayout,
	memory_block: Box<MemoryBlock>,
	active: bool,
	sealed: bool,
	image_file: Vec<u8>,
	context: Context,
	thunks: ThunkManager,
	threads: GuestThreadSet,
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

		unsafe { gdb::register(&image_file[..]) }
		let mut res = Box::new(WaterboxHost {
			fs,
			program_break: layout.sbrk.start,
			elf,
			layout,
			memory_block,
			active: false,
			sealed: false,
			image_file,
			context: Context::new(layout.main_thread.end(), syscall),
			thunks,
			threads: GuestThreadSet::new(),
		});
		res.activate();
		println!("Calling _start()");
		res.run_guest_simple(res.elf.entry_point());
		res.deactivate();

		Ok(res)
	}

	pub fn active(&self) -> bool {
		self.active
	}

	pub fn activate(&mut self) {
		if !self.active {
			context::prepare_thread();
			self.context.host_ptr = self as *mut WaterboxHost as usize;
			self.memory_block.activate();
			self.active = true;
		}
	}

	pub fn deactivate(&mut self) {
		if self.active {
			self.context.host_ptr = 0;
			self.memory_block.deactivate();
			self.active = false;
		}
	}
}

impl Drop for WaterboxHost {
	fn drop(&mut self) {
		self.deactivate();
		unsafe { gdb::deregister(&self.image_file[..]) }
	}
}

impl WaterboxHost {
	pub fn get_external_callback_ptr(&mut self, callback: ExternalCallback, slot: usize) -> anyhow::Result<usize> {
		if slot >= CALLBACK_SLOTS {
			Err(anyhow!("slot must be less than {}", CALLBACK_SLOTS))
		} else {
			self.context.extcall_slots[slot] = Some(callback);
			Ok(context::get_callback_ptr(slot))
		}
	}
	pub fn get_external_callin_ptr(&mut self, ptr: usize) -> anyhow::Result<usize> {
		self.thunks.get_thunk_for_proc(ptr, &mut self.context as *mut Context)
	}
	pub fn get_proc_addr(&mut self, name: &str) -> anyhow::Result<usize> {
		let ptr = self.elf.get_proc_addr(name);
		if ptr == 0 {
			Ok(0)
		} else {
			self.thunks.get_thunk_for_proc(ptr, &mut self.context as *mut Context)
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

		let was_active = self.active;
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

		self.elf.seal(&mut self.memory_block);
		self.memory_block.seal()?;

		if !was_active {
			self.deactivate();
		}
		self.sealed = true;
		Ok(())
	}
	pub fn mount_file(&mut self, name: String, data: Vec<u8>, writable: bool) -> anyhow::Result<()> {
		self.fs.mount(name, data, writable)
	}
	pub fn unmount_file(&mut self, name: &str) -> anyhow::Result<Vec<u8>> {
		self.fs.unmount(name)
	}
	// pub fn set_missing_file_callback(&mut self, cb: Option<MissingFileCallback>) {
	// 	self.fs.set_missing_file_callback(cb);
	// }

	/// Run a guest entry point that takes no arguments
	pub fn run_guest_simple(&mut self, entry_point: usize) {
		context::call_guest_simple(entry_point, &mut self.context);
	}
}

const SAVE_START_MAGIC: &str = "ActivatedWaterboxHost_v1";
const SAVE_END_MAGIC: &str = "ʇsoHxoqɹǝʇɐMpǝʇɐʌᴉʇɔ∀";
impl IStateable for WaterboxHost {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::write_magic(stream, SAVE_START_MAGIC)?;
		self.fs.save_state(stream)?;
		bin::write(stream, &self.program_break)?;
		self.elf.save_state(stream)?;
		self.memory_block.save_state(stream)?;
		self.threads.save_state(&self.context, stream)?;
		bin::write_magic(stream, SAVE_END_MAGIC)?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::verify_magic(stream, SAVE_START_MAGIC)?;
		self.fs.load_state(stream)?;
		bin::read(stream, &mut self.program_break)?;
		self.elf.load_state(stream)?;
		self.memory_block.load_state(stream)?;
		self.threads.load_state(&mut self.context, stream)?;
		bin::verify_magic(stream, SAVE_END_MAGIC)?;
		Ok(())
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

extern "sysv64" fn syscall(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, _a6: usize,
	nr: SyscallNumber, h: &mut WaterboxHost
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
			let res = h.memory_block.mmap(AddressRange { start: a1, size: a2 }, prot, arena_addr, no_replace)?;
			syscall_ok(res)
		},
		NR_MREMAP => {
			let arena_addr = h.layout.mmap;
			let res = h.memory_block.mremap(AddressRange { start: a1, size: a2 }, a3, arena_addr)?;
			syscall_ok(res)
		},
		NR_MPROTECT => {
			let prot = arg_to_prot(a3)?;
			let res = h.memory_block.mprotect(AddressRange { start: a1, size: a2 }, prot);
			syscall_ret(res)
		},
		NR_MUNMAP => syscall_ret(h.memory_block.munmap(AddressRange { start: a1, size: a2 })),
		NR_MADVISE => {
			match a3 {
				MADV_DONTNEED => syscall_ret(h.memory_block.madvise_dontneed(AddressRange { start: a1, size: a2 })),
				_ => syscall_ok(0),
			}
		},
		NR_STAT => {
			let name = arg_to_str(a1)?;
			syscall_ret(h.fs.stat(&name, arg_to_statbuff(a2)))
		},
		NR_FSTAT => {
			syscall_ret(h.fs.fstat(arg_to_fd(a1)?, arg_to_statbuff(a2)))
		},
		NR_IOCTL => syscall_ok(0),
		NR_READ => {
			unsafe {
				syscall_ret_i64(h.fs.read(arg_to_fd(a1)?, std::slice::from_raw_parts_mut(a2 as *mut u8, a3)))
			}
		},
		NR_WRITE => {
			unsafe {
				syscall_ret_i64(h.fs.write(arg_to_fd(a1)?, std::slice::from_raw_parts(a2 as *const u8, a3)))
			}
		},
		NR_READV => {
			let fd = arg_to_fd(a1)?;
			unsafe {
				let mut ret = 0;
				let iov = std::slice::from_raw_parts_mut(a2 as *mut Iovec, a3);
				for io in iov {
					if io.iov_base != 0 {
						ret += h.fs.read(fd, io.slice_mut())?;
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
				for io in iov {
					if io.iov_base != 0 {
						ret += h.fs.write(fd, io.slice())?;
					}
				}
				syscall_ok(ret as usize)
			}
		},
		NR_OPEN => {
			syscall_ret_val(h.fs.open(&arg_to_str(a1)?, a2 as i32, a3 as i32).map(|x| x.0 as usize))
		},
		NR_CLOSE => syscall_ret(h.fs.close(arg_to_fd(a1)?)),
		NR_LSEEK => syscall_ret_i64(h.fs.seek(arg_to_fd(a1)?, a2 as i64, a3 as i32)),
		NR_TRUNCATE => syscall_ret(h.fs.truncate(&arg_to_str(a1)?, a2 as i64)),
		NR_FTRUNCATE => syscall_ret(h.fs.ftruncate(arg_to_fd(a1)?, a2 as i64)),
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
			let old = h.program_break;
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
				h.memory_block.mmap_fixed(AddressRange { start: old, size: a1 - old }, Protection::RW, true).unwrap();
				println!("Allocated {} bytes on sbrk heap, usage {}/{}", a1 - old, a1 - addr.start, addr.size);
				a1
			} else {
				old
			};
			h.program_break = res;
			syscall_ok(res)
		},
		NR_WBX_CLONE => {
			syscall_ret_val(
				h.threads
					.spawn(
						&mut h.memory_block, 
						a1, 
						a2, 
						a3 as *mut u32,
						a4
					)
					.map(|tid| tid as usize)
			)
		},
		NR_EXIT => {
			h.threads.exit(&mut h.context)
		},
		NR_FUTEX => {
			let op = a2 as i32 & !FUTEX_PRIVATE;
			match op {
				FUTEX_WAIT => {
					h.threads.futex_wait(&mut h.context, a1, a3 as u32)
				},
				// int *uaddr, int futex_op, int val,
				// const struct timespec *timeout,   /* or: uint32_t val2 */
				// int *uaddr2, int val3
				FUTEX_WAKE => {
					syscall_ok(h.threads.futex_wake(a1, a3 as u32))
				},
				FUTEX_REQUEUE => {
					syscall_ret_val(
						h.threads.futex_requeue(
							a1,
							a5,
							a3 as u32,
							a4 as u32
						)
					)
				},
				FUTEX_UNLOCK_PI => {
					h.threads.futex_unlock_pi(&mut h.context, a1)
				},
				FUTEX_LOCK_PI => {
					h.threads.futex_lock_pi(&mut h.context, a1)
				},
				_ => syscall_err(ENOSYS),
			}
		},
		NR_SET_THREAD_AREA => syscall_err(ENOSYS), // musl handles this in userspace
		NR_SET_TID_ADDRESS => syscall_ok(h.threads.set_tid_address(a1) as usize),
		NR_GETTID => syscall_ok(h.threads.get_tid() as usize),
		NR_RT_SIGPROCMASK => {
			// we don't (nor ever plan to?) deliver any signals to guests, so...
			syscall_ok(0)
		},
		NR_SCHED_YIELD => {
			h.threads.yield_any(&mut h.context)
		},
		NR_NANOSLEEP | NR_CLOCK_NANOSLEEP => {
			// We'll never be interrupted by signals, and isolate guest time from real time,
			// so don't need to examine the arguments here and can just treat this as another yield
			h.threads.yield_any(&mut h.context)
		},
		_ => syscall_ret(unimp(nr)),
	}
}
