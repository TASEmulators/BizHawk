use crate::*;
use crate::{memory_block::ActivatedMemoryBlock, syscall_defs::*};
use memory_block::{MemoryBlock, Protection};
use std::{os::raw::c_char, ffi::CStr};
use fs::{FileDescriptor, FileSystem};
use elf::ElfLoader;
use cinterface::MemoryLayoutTemplate;

pub struct WaterboxHost {
	fs: FileSystem,
	program_break: usize,
	elf: ElfLoader,
	layout: WbxSysLayout,
	memory_block: Box<MemoryBlock>,
	active: bool,
	sealed: bool,
}
impl WaterboxHost {
	pub fn new(wbx: &[u8], module_name: &str, layout_template: &MemoryLayoutTemplate) -> anyhow::Result<Box<WaterboxHost>> {
		let layout = layout_template.make_layout()?;
		let mut memory_block = MemoryBlock::new(layout.all());
		let mut b = memory_block.enter();
		let elf = ElfLoader::new(wbx, module_name, &layout, &mut b)?;
		let fs = FileSystem::new();
		drop(b);
		let mut res = Box::new(WaterboxHost {
			fs,
			program_break: layout.sbrk.start,
			elf,
			layout,
			memory_block,
			active: false,
			sealed: false,
		});

		let mut active = res.activate();
		active.h.elf.connect_syscalls(&mut active.b, &mut active.sys);
		active.h.elf.native_init(&mut active.b);
		drop(active);

		Ok(res)
	}

	pub fn active(&self) -> bool {
		self.active
	}

	pub fn activate(&mut self) -> Box<ActivatedWaterboxHost> {
		let h = unsafe { &mut *(self as *mut WaterboxHost) };
		let b = self.memory_block.enter();
		let sys = WbxSysArea {
			layout: self.layout,
			syscall: WbxSysSyscall {
				ud: 0,
				syscall,
			}
		};
		let mut res = Box::new(ActivatedWaterboxHost {
			tag: TAG,
			h,
			b,
			sys
		});
		res.sys.syscall.ud = res.as_mut() as *mut ActivatedWaterboxHost as usize;
		res.h.active = true;
		res
	}
}

const TAG: u64 = 0xd01487803948acff;
pub struct ActivatedWaterboxHost<'a> {
	tag: u64,
	h: &'a mut WaterboxHost,
	b: ActivatedMemoryBlock<'a>,
	sys: WbxSysArea,
}
impl<'a> Drop for ActivatedWaterboxHost<'a> {
	fn drop(&mut self) {
		self.h.active = false;
	}
}

impl<'a> ActivatedWaterboxHost<'a> {
	pub fn get_proc_addr(&self, name: &str) -> usize {
		self.h.elf.get_proc_addr(name)
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
		self.h.elf.clear_syscalls(&mut self.b);
		self.h.elf.seal(&mut self.b);
		self.h.elf.connect_syscalls(&mut self.b, &self.sys);
		self.h.elf.co_clean(&mut self.b);
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
		self.h.elf.connect_syscalls(&mut self.b, &self.sys);
		Ok(())
	}
}

fn unimp(nr: SyscallNumber) -> SyscallResult {
	eprintln!("Stopped on unimplemented syscall {}", lookup_syscall(&nr));
	unsafe { std::intrinsics::breakpoint() }
	Err(ENOSYS)
}

fn gethost<'a>(ud: usize) -> &'a mut ActivatedWaterboxHost<'a> {
	let res = unsafe { &mut *(ud as *mut ActivatedWaterboxHost) };
	if res.tag != TAG {
		unsafe { std::intrinsics::breakpoint() }
		std::process::abort();
	}
	res
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

pub extern "win64" fn syscall(nr: SyscallNumber, ud: usize, a1: usize, a2: usize, a3: usize, a4: usize, _a5: usize, _a6: usize) -> SyscallReturn {
	let mut h = gethost(ud);
	match nr {
		NR_MMAP => {
			let prot = arg_to_prot(a3)?;
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
			let arena_addr = h.sys.layout.mmap;
			let res = h.b.mmap(AddressRange { start: a1, size: a2 }, prot, arena_addr)?;
			syscall_ok(res)
		},
		NR_MREMAP => {
			let arena_addr = h.sys.layout.mmap;
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
			let addr = h.sys.layout.sbrk;
			let old = h.h.program_break;
			let res = if a1 != align_down(a1) {
				old
			} else if a1 < addr.start || a1 > addr.end() {
				old
			} else if a1 > old {
				h.b.mmap_fixed(AddressRange { start: old, size: a1 - old }, Protection::RW).unwrap();
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
