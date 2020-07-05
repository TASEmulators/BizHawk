use crate::*;
use crate::{memory_block::ActivatedMemoryBlock, syscall_defs::*};
use memory_block::{MemoryBlock, Protection};
use std::{os::raw::c_char, ffi::CStr};
use fs::{FileDescriptor, FileSystem/*, MissingFileCallback*/};
use elf::ElfLoader;
use cinterface::MemoryLayoutTemplate;
use goblin::elf::Elf;
use context::{CALLBACK_SLOTS, Context, ExternalCallback, thunks::ThunkManager};

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
}
impl WaterboxHost {
	pub fn new(image_file: Vec<u8>, module_name: &str, layout_template: &MemoryLayoutTemplate) -> anyhow::Result<Box<WaterboxHost>> {
		let thunks = ThunkManager::new()?;
		let wbx = Elf::parse(&image_file[..])?;
		let elf_addr = ElfLoader::elf_addr(&wbx);
		let layout = layout_template.make_layout(elf_addr)?;
		let mut memory_block = MemoryBlock::new(layout.all());
		let mut b = memory_block.enter();
		let elf = ElfLoader::new(&wbx, &image_file[..], module_name, &layout, &mut b)?;
		let fs = FileSystem::new();
		drop(b);
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
			context: Context {
				host_rsp: 0,
				guest_rsp: layout.main_thread.end(),
				dispatch_syscall: syscall,
				host_ptr: 0,
				extcall_slots: [None; 64],
			},
			thunks,
		});

		let mut active = res.activate();
		println!("Calling _start()");
		active.run_guest_simple(active.h.elf.entry_point());
		drop(active);

		Ok(res)
	}

	pub fn active(&self) -> bool {
		self.active
	}

	pub fn activate(&mut self) -> Box<ActivatedWaterboxHost> {
		context::prepare_thread();
		let h = unsafe { &mut *(self as *mut WaterboxHost) };
		let b = self.memory_block.enter();
		let mut res = Box::new(ActivatedWaterboxHost {
			h,
			b,
		});
		res.h.active = true;
		res.h.context.host_ptr = res.as_mut() as *mut ActivatedWaterboxHost as usize;
		res.h.thunks.update_context_ptr(&mut res.h.context as *mut Context).unwrap();
		res
	}
}
impl Drop for WaterboxHost {
	fn drop(&mut self) {
		unsafe { gdb::deregister(&self.image_file[..]) }
	}
}

pub struct ActivatedWaterboxHost<'a> {
	h: &'a mut WaterboxHost,
	b: ActivatedMemoryBlock<'a>,
}
impl<'a> Drop for ActivatedWaterboxHost<'a> {
	fn drop(&mut self) {
		self.h.active = false;
		self.h.context.host_ptr = 0;
	}
}

impl<'a> ActivatedWaterboxHost<'a> {
	pub fn get_external_callback_ptr(&mut self, callback: ExternalCallback, slot: usize) -> anyhow::Result<usize> {
		if slot >= CALLBACK_SLOTS {
			Err(anyhow!("slot must be less than {}", CALLBACK_SLOTS))
		} else {
			self.h.context.extcall_slots[slot] = Some(callback);
			Ok(context::get_callback_ptr(slot))
		}
	}
	pub fn get_proc_addr(&mut self, name: &str) -> anyhow::Result<usize> {
		let ptr = self.h.elf.get_proc_addr(name);
		if ptr == 0 {
			Ok(0)
		} else {
			self.h.thunks.get_thunk_for_proc(ptr, &mut self.h.context as *mut Context)
		}
	}
	fn check_sealed(&self) -> anyhow::Result<()> {
		if !self.h.sealed {
			Err(anyhow!("Not sealed!"))
		} else {
			Ok(())
		}
	}
	pub fn seal(&mut self) -> anyhow::Result<()> {
		if self.h.sealed {
			return Err(anyhow!("Already sealed!"))
		}

		fn run_proc(h: &mut ActivatedWaterboxHost, name: &str) {
			match h.h.elf.get_proc_addr(name) {
				0 => (),
				ptr => {
					println!("Calling {}()", name);
					h.run_guest_simple(ptr);
				},
			}
		}
		run_proc(self, "co_clean");
		run_proc(self, "ecl_seal");

		self.h.elf.seal(&mut self.b);
		self.b.seal();
		self.h.sealed = true;
		Ok(())
	}
	pub fn mount_file(&mut self, name: String, data: Vec<u8>, writable: bool) -> anyhow::Result<()> {
		self.h.fs.mount(name, data, writable)
	}
	pub fn unmount_file(&mut self, name: &str) -> anyhow::Result<Vec<u8>> {
		self.h.fs.unmount(name)
	}
	// pub fn set_missing_file_callback(&mut self, cb: Option<MissingFileCallback>) {
	// 	self.h.fs.set_missing_file_callback(cb);
	// }

	/// Run a guest entry point that takes no arguments
	pub fn run_guest_simple(&mut self, entry_point: usize) {
		context::call_guest_simple(entry_point, &mut self.h.context);
	}
}

const SAVE_START_MAGIC: &str = "ActivatedWaterboxHost_v1";
const SAVE_END_MAGIC: &str = "ʇsoHxoqɹǝʇɐMpǝʇɐʌᴉʇɔ∀";
impl<'a> IStateable for ActivatedWaterboxHost<'a> {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::write_magic(stream, SAVE_START_MAGIC)?;
		self.h.fs.save_state(stream)?;
		bin::write(stream, &self.h.program_break)?;
		self.h.elf.save_state(stream)?;
		self.b.save_state(stream)?;
		bin::write_magic(stream, SAVE_END_MAGIC)?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		self.check_sealed()?;
		bin::verify_magic(stream, SAVE_START_MAGIC)?;
		self.h.fs.load_state(stream)?;
		bin::read(stream, &mut self.h.program_break)?;
		self.h.elf.load_state(stream)?;
		self.b.load_state(stream)?;
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

extern "sysv64" fn syscall(a1: usize, a2: usize, a3: usize, a4: usize, _a5: usize, _a6: usize, nr: SyscallNumber) -> SyscallReturn {
	let h = unsafe { context::access_context() };
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
			let arena_addr = h.h.layout.mmap;
			let res = h.b.mmap(AddressRange { start: a1, size: a2 }, prot, arena_addr, no_replace)?;
			syscall_ok(res)
		},
		NR_MREMAP => {
			let arena_addr = h.h.layout.mmap;
			let res = h.b.mremap(AddressRange { start: a1, size: a2 }, a3, arena_addr)?;
			syscall_ok(res)
		},
		NR_MPROTECT => {
			let prot = arg_to_prot(a3)?;
			let res = h.b.mprotect(AddressRange { start: a1, size: a2 }, prot);
			syscall_ret(res)
		},
		NR_MUNMAP => syscall_ret(h.b.munmap(AddressRange { start: a1, size: a2 })),
		NR_MADVISE => {
			match a3 {
				MADV_DONTNEED => syscall_ret(h.b.madvise_dontneed(AddressRange { start: a1, size: a2 })),
				_ => syscall_ok(0),
			}
		},
		NR_STAT => {
			let name = arg_to_str(a1)?;
			syscall_ret(h.h.fs.stat(&name, arg_to_statbuff(a2)))
		},
		NR_FSTAT => {
			syscall_ret(h.h.fs.fstat(arg_to_fd(a1)?, arg_to_statbuff(a2)))
		},
		NR_IOCTL => syscall_ok(0),
		NR_READ => {
			unsafe {
				syscall_ret_i64(h.h.fs.read(arg_to_fd(a1)?, std::slice::from_raw_parts_mut(a2 as *mut u8, a3)))
			}
		},
		NR_WRITE => {
			unsafe {
				syscall_ret_i64(h.h.fs.write(arg_to_fd(a1)?, std::slice::from_raw_parts(a2 as *const u8, a3)))
			}
		},
		NR_READV => {
			let fd = arg_to_fd(a1)?;
			unsafe {
				let mut ret = 0;
				let iov = std::slice::from_raw_parts_mut(a2 as *mut Iovec, a3);
				for io in iov {
					if io.iov_base != 0 {
						ret += h.h.fs.read(fd, io.slice_mut())?;
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
						ret += h.h.fs.write(fd, io.slice())?;
					}
				}
				syscall_ok(ret as usize)
			}
		},
		NR_OPEN => {
			syscall_ret_val(h.h.fs.open(&arg_to_str(a1)?, a2 as i32, a3 as i32).map(|x| x.0 as usize))
		},
		NR_CLOSE => syscall_ret(h.h.fs.close(arg_to_fd(a1)?)),
		NR_LSEEK => syscall_ret_i64(h.h.fs.seek(arg_to_fd(a1)?, a2 as i64, a3 as i32)),
		NR_TRUNCATE => syscall_ret(h.h.fs.truncate(&arg_to_str(a1)?, a2 as i64)),
		NR_FTRUNCATE => syscall_ret(h.h.fs.ftruncate(arg_to_fd(a1)?, a2 as i64)),
		// TODO: 99% sure nothing calls this
		NR_SET_THREAD_AREA => syscall_err(ENOSYS),
		// TODO: What calls this?
		NR_SET_TID_ADDRESS => syscall_ok(8675309),
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
			let addr = h.h.layout.sbrk;
			let old = h.h.program_break;
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
				h.b.mmap_fixed(AddressRange { start: old, size: a1 - old }, Protection::RW, true).unwrap();
				println!("Allocated {} bytes on sbrk heap, usage {}/{}", a1 - old, a1 - addr.start, addr.size);
				a1
			} else {
				old
			};
			h.h.program_break = res;
			syscall_ok(res)
		},
		_ => syscall_ret(unimp(nr)),
	}
}
