// Platform abstraction layer over mmap/etc.  Doesn't do much checking, not meant for general consumption
use super::Protection;
use crate::*;

#[derive(Debug)]
pub struct Handle(usize);

#[cfg(windows)]
pub use win::*;
#[cfg(windows)]
mod win {
	use std::mem::{size_of, zeroed};
	use winapi::um::memoryapi::*;
	use winapi::um::winnt::*;
	use winapi::um::handleapi::*;
	use super::*;
	use std::ptr::{null, null_mut};
	use winapi::ctypes::c_void;

	fn error() {
		unsafe {
			let err = winapi::um::errhandlingapi::GetLastError();
			eprintln!("WinApi failure code: {}", err);
		}
	}

	pub fn open(size: usize) -> Option<Handle> {
		unsafe {
			let res = CreateFileMappingW(
				INVALID_HANDLE_VALUE,
				null_mut(),
				PAGE_EXECUTE_READWRITE,
				(size >> 32) as u32,
				size as u32,
				null()
			);
			if res == null_mut() {
				error();
				None
			} else {
				Some(Handle(res as usize))
			}
		}
	}

	pub unsafe fn close(handle: Handle) -> bool {
		CloseHandle(handle.0 as *mut c_void) != 0
	}

	pub fn bad() -> Handle {
		return Handle(INVALID_HANDLE_VALUE as usize);
	}

	pub fn map(handle: &Handle, addr: AddressRange) -> bool {
		unsafe {
			let res = MapViewOfFileEx(
				handle.0 as *mut c_void,
				FILE_MAP_ALL_ACCESS | FILE_MAP_EXECUTE,
				0,
				0,
				addr.size,
				addr.start as *mut c_void
			);
			if res == addr.start as *mut c_void {
				true
			} else {
				error();
				false
			}
		}
	}

	pub unsafe fn unmap(addr: AddressRange) -> bool {
		UnmapViewOfFile(addr.start as *mut c_void) != 0
	}

	pub unsafe fn protect(addr: AddressRange, prot: Protection) -> bool {
		let p = match prot {
			Protection::None => PAGE_NOACCESS,
			Protection::R => PAGE_READONLY,
			Protection::RW => PAGE_READWRITE,
			Protection::RX => PAGE_EXECUTE_READ,
			Protection::RWX => PAGE_EXECUTE_READWRITE,
			Protection::RWStack => PAGE_READWRITE | PAGE_GUARD,
		};
		let mut old_protect: u32 = 0;
		VirtualProtect(addr.start as *mut c_void, addr.size, p, &mut old_protect) != 0
	}

	pub struct StackTripResult {
		pub size: usize,
		pub dirty: bool,
	}
	/// Return true if the memory was dirtied.  Return size is effectively arbitrary; it may be larger
	/// or smaller than you expected.  DANGER:  If called on memory that is not currently in RWStack mode,
	/// it will generally return true even though that's not what you want.
	pub unsafe fn get_stack_dirty(start: usize) -> Option<StackTripResult> {
		let mut mbi = Box::new(zeroed::<MEMORY_BASIC_INFORMATION>());
		let mbi_size = size_of::<MEMORY_BASIC_INFORMATION>();
		if VirtualQuery(start as *const c_void, &mut *mbi, mbi_size) != mbi_size {
			error();
			None
		} else {
			Some(StackTripResult {
				size: mbi.RegionSize,
				dirty: mbi.Protect & PAGE_GUARD == 0,
			})
		}
	}
}

#[cfg(unix)]
pub use nix::*;
#[cfg(unix)]
mod nix {
	use super::*;
	use libc::*;

	fn error() {
		unsafe {
			let err = *__errno_location();
			eprintln!("Libc failure code: {}", err);
		}
	}

	pub fn open(size: usize) -> Option<Handle> {
		unsafe {
			let s = std::ffi::CString::new("MemoryBlockUnix").unwrap();
			let fd = syscall(SYS_memfd_create, s.as_ptr(), MFD_CLOEXEC) as i32;
			if fd == -1 {
				error();
				return None
			}
			if ftruncate(fd, size as i64) != 0 {
				error();
				None
			} else {
				Some(Handle(fd as usize))
			}
		}
	}

	pub unsafe fn close(handle: Handle) -> bool {
		libc::close(handle.0 as i32) == 0
	}

	pub fn bad() -> Handle {
		return Handle(-1i32 as usize);
	}

	pub fn map(handle: &Handle, addr: AddressRange) -> bool {
		unsafe {
			let res = mmap(addr.start as *mut c_void,
				addr.size,
				PROT_READ | PROT_WRITE | PROT_EXEC,
				MAP_SHARED | MAP_FIXED,
				handle.0 as i32,
				0
			);
			if res == addr.start as *mut c_void {
				true
			} else {
				error();
				false
			}
		}
	}

	pub unsafe fn unmap(addr: AddressRange) -> bool {
		munmap(addr.start as *mut c_void, addr.size) == 0
	}

	pub unsafe fn protect(addr: AddressRange, prot: Protection) -> bool {
		let p = match prot {
			Protection::None => PROT_NONE,
			Protection::R => PROT_READ,
			Protection::RW => PROT_READ | PROT_WRITE,
			Protection::RX => PROT_READ | PROT_EXEC,
			Protection::RWX => PROT_READ | PROT_WRITE | PROT_EXEC,
			Protection::RWStack => panic!("RWStack should not be passed to pal layer"),
		};
		mprotect(addr.start as *mut c_void, addr.size, p) == 0
	}
}

#[cfg(test)]
mod tests {
	use super::*;
	use std::mem::transmute;
	
	#[test]
	fn basic_test() {
		assert!(crate::PAGESIZE == 0x1000);
		// can't test the fault states (RWStack, R prohibits write, etc.) without cooperation of tripguard, so do that elsewhere
		unsafe {
			let size = 0x20000usize;
			let start = 0x36a00000000usize;
			let addr = AddressRange { start, size };
			let handle = open(size).unwrap();

			assert!(map(&handle, addr));
			assert!(protect(addr, Protection::RW));
			*((start + 0x14795) as *mut u8) = 42;
			assert!(unmap(addr));

			assert!(map(&handle, addr));
			assert!(protect(addr, Protection::R));
			assert_eq!(*((start + 0x14795) as *const u8), 42);
			assert!(unmap(addr));

			assert!(map(&handle, addr));
			assert!(protect(addr, Protection::RW));
			*(start as *mut u8) = 0xc3; // RET
			assert!(protect(addr, Protection::RX));
			transmute::<usize, extern fn() -> ()>(start)();
			assert!(protect(addr, Protection::RWX));
			*(start as *mut u8) = 0x90; // NOP
			*((start + 1) as *mut u8) = 0xb0; // MOV AL
			*((start + 2) as *mut u8) = 0x7b; // 123
			*((start + 3) as *mut u8) = 0xc3; // RET
			let i = transmute::<usize, extern fn() -> u8>(start)();
			assert_eq!(i, 123);
			assert!(unmap(addr));

			assert!(close(handle));
		}
	}
}
