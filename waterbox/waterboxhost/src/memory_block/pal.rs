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

	fn error() -> anyhow::Error {
		anyhow!("WinApi failure code: {}", unsafe { winapi::um::errhandlingapi::GetLastError() })
	}
	fn ret(code: i32) -> anyhow::Result<()> {
		match code {
			0 => Err(error()),
			_ => Ok(())
		}
	}

	/// Open a file (not backed by the fs) for memory mapping
	/// Caller must close_handle() later or else leak
	pub fn open_handle(size: usize) -> anyhow::Result<Handle> {
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
				Err(error())
			} else {
				Ok(Handle(res as usize))
			}
		}
	}

	/// close a handle returned by open_handle()
	/// Unsafe:  Only call with handle returned by open_handle().  Do not call when that handle is mapped
	pub unsafe fn close_handle(handle: Handle) -> anyhow::Result<()> {
		ret(CloseHandle(handle.0 as *mut c_void))
	}

	/// Return a trapping handle.  Calling just about anything with this is bad
	pub fn bad() -> Handle {
		return Handle(INVALID_HANDLE_VALUE as usize);
	}

	/// Map a handle into an address range
	/// Probably shouldn't call with addr.size > handle's alloced size
	/// Leaks if not later unmapped
	pub fn map_handle(handle: &Handle, addr: AddressRange) -> anyhow::Result<()> {
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
				Ok(())
			} else {
				Err(error())
			}
		}
	}

	/// Unmaps an address range previously mapped by map_handle
	/// Unsafe:  Do not call with any `addr` that does not exactly match a previous successful map_handle
	pub unsafe fn unmap_handle(addr: AddressRange) -> anyhow::Result<()> {
		ret(UnmapViewOfFile(addr.start as *mut c_void))
	}

	fn prottoprot(prot: Protection) -> u32 {
		match prot {
			Protection::None => PAGE_NOACCESS,
			Protection::R => PAGE_READONLY,
			Protection::RW => PAGE_READWRITE,
			Protection::RX => PAGE_EXECUTE_READ,
			Protection::RWX => PAGE_EXECUTE_READWRITE,
			Protection::RWStack => PAGE_READWRITE | PAGE_GUARD,
		}
	}

	/// Map some anonymous bytes with no fd backing
	/// addr.start can be 0, which means the OS chooses a location, or non-zero, which gives fixed behavior like map_handle
	/// Returned address range will be identical in the case of non-zero, or give the actual address in the case of zero.
	pub fn map_anon(addr: AddressRange, initial_prot: Protection) -> anyhow::Result<AddressRange> {
		unsafe {
			let res = VirtualAlloc(addr.start as *mut c_void, 
				addr.size, MEM_RESERVE | MEM_COMMIT, 
				prottoprot(initial_prot)) as usize;
			match res {
				0 => Err(error()),
				p => Ok(AddressRange { start: p, size: addr.size })
			}
		}
	}

	/// Change memory protection on allocated bytes
	/// This should work safely on any aligned subset of map_anon or map_handle
	pub unsafe fn protect(addr: AddressRange, prot: Protection) -> anyhow::Result<()> {
		let p = prottoprot(prot);
		let mut old_protect: u32 = 0;
		let res = VirtualProtect(addr.start as *mut c_void, addr.size, p, &mut old_protect);
		ret(res)
	}

	/// Unmap bytes previously mapped by map_anon
	/// addr should exactly match the return value from map_anon (so if you mapped with start 0, you need to pass the actual start back)
	pub unsafe fn unmap_anon(addr: AddressRange) -> anyhow::Result<()> {
		ret(VirtualFree(addr.start as *mut c_void, 0, MEM_RELEASE))
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

	fn error() -> anyhow::Error {
		unsafe {
			let err = *__errno_location();
			anyhow!("Libc failure code: {}", err)
		}
	}
	fn ret(code: i32) -> anyhow::Result<()> {
		match code {
			0 => Ok(()),
			_ => Err(error()),
		}
	}

	/// Open a file (not backed by the fs) for memory mapping
	/// Caller must close_handle() later or else leak
	pub fn open_handle(size: usize) -> anyhow::Result<Handle> {
		unsafe {
			let s = std::ffi::CString::new("MemoryBlockUnix").unwrap();
			let fd = syscall(SYS_memfd_create, s.as_ptr(), MFD_CLOEXEC) as i32;
			if fd == -1 {
				return Err(error())
			}
			if ftruncate(fd, size as i64) != 0 {
				Err(error())
			} else {
				Ok(Handle(fd as usize))
			}
		}
	}

	/// close a handle returned by open_handle()
	/// Unsafe:  Only call with handle returned by open_handle().  Do not call when that handle is mapped
	pub unsafe fn close_handle(handle: Handle) -> anyhow::Result<()> {
		ret(libc::close(handle.0 as i32))
	}

	/// Return a trapping handle.  Calling just about anything with this is bad
	pub fn bad() -> Handle {
		return Handle(-1i32 as usize);
	}

	/// Map a handle into an address range
	/// Probably shouldn't call with addr.size > handle's alloced size
	/// Leaks if not later unmapped
	pub fn map_handle(handle: &Handle, addr: AddressRange) -> anyhow::Result<()> {
		unsafe {
			let res = mmap(addr.start as *mut c_void,
				addr.size,
				PROT_READ | PROT_WRITE | PROT_EXEC,
				MAP_SHARED | MAP_FIXED,
				handle.0 as i32,
				0
			);
			if res == addr.start as *mut c_void {
				Ok(())
			} else {
				Err(error())
			}
		}
	}

	/// Unmaps an address range previously mapped by map_handle
	/// Unsafe:  Do not call with any `addr` that does not exactly match a previous successful map_handle
	pub unsafe fn unmap_handle(addr: AddressRange) -> anyhow::Result<()> {
		ret(munmap(addr.start as *mut c_void, addr.size))
	}

	fn prottoprot(prot: Protection) -> i32 {
		match prot {
			Protection::None => PROT_NONE,
			Protection::R => PROT_READ,
			Protection::RW => PROT_READ | PROT_WRITE,
			Protection::RX => PROT_READ | PROT_EXEC,
			Protection::RWX => PROT_READ | PROT_WRITE | PROT_EXEC,
			Protection::RWStack => panic!("RWStack should not be passed to nix pal layer"),
		}
	}

	/// Map some anonymous bytes with no fd backing
	/// addr.start can be 0, which means the OS chooses a location, or non-zero, which gives fixed behavior like map_handle
	/// Returned address range will be identical in the case of non-zero, or give the actual address in the case of zero.
	pub fn map_anon(addr: AddressRange, initial_prot: Protection) -> anyhow::Result<AddressRange> {
		unsafe {
			let mut flags = MAP_PRIVATE | MAP_ANONYMOUS;
			if addr.start != 0 {
				flags |= MAP_FIXED | MAP_FIXED_NOREPLACE;
			}
			let ptr = mmap(addr.start as *mut c_void, addr.size, prottoprot(initial_prot), flags, -1, 0);
			match ptr {
				MAP_FAILED => Err(error()),
				p => Ok(AddressRange { start: p as usize, size: addr.size })
			}
		}
	}

	/// Change memory protection on allocated bytes
	/// This should work safely on any aligned subset of map_anon or map_handle
	pub unsafe fn protect(addr: AddressRange, prot: Protection) -> anyhow::Result<()> {
		let p = prottoprot(prot);
		ret(mprotect(addr.start as *mut c_void, addr.size, p))
	}

	/// Unmap bytes previously mapped by map_anon
	/// addr should exactly match the return value from map_anon (so if you mapped with start 0, you need to pass the actual start back)
	pub unsafe fn unmap_anon(addr: AddressRange) -> anyhow::Result<()> {
		ret(munmap(addr.start as *mut c_void, addr.size))
	}
}

#[cfg(test)]
mod tests {
	use super::*;
	use std::mem::transmute;
	
	#[test]
	fn basic_test() -> anyhow::Result<()> {
		assert!(crate::PAGESIZE == 0x1000);
		// can't test the fault states (RWStack, R prohibits write, etc.) without cooperation of tripguard, so do that elsewhere
		unsafe {
			let size = 0x20000usize;
			let start = 0x36a00000000usize;
			let addr = AddressRange { start, size };
			let handle = open_handle(size).unwrap();

			map_handle(&handle, addr)?;
			protect(addr, Protection::RW)?;
			*((start + 0x14795) as *mut u8) = 42;
			unmap_handle(addr)?;

			map_handle(&handle, addr)?;
			protect(addr, Protection::R)?;
			assert_eq!(*((start + 0x14795) as *const u8), 42);
			unmap_handle(addr)?;

			map_handle(&handle, addr)?;
			protect(addr, Protection::RW)?;
			*(start as *mut u8) = 0xc3; // RET
			protect(addr, Protection::RX)?;
			transmute::<usize, extern fn() -> ()>(start)();
			protect(addr, Protection::RWX)?;
			*(start as *mut u8) = 0x90; // NOP
			*((start + 1) as *mut u8) = 0xb0; // MOV AL
			*((start + 2) as *mut u8) = 0x7b; // 123
			*((start + 3) as *mut u8) = 0xc3; // RET
			let i = transmute::<usize, extern fn() -> u8>(start)();
			assert_eq!(i, 123);
			unmap_handle(addr)?;

			close_handle(handle)?;
		}
		Ok(())
	}

	#[test]
	fn test_map_anon() -> anyhow::Result<()> {
		unsafe {
			let addr_in = AddressRange { start: 0x34100000000, size: 0x20000 };
			let addr = map_anon(addr_in, Protection::RW)?;
			assert_eq!(addr.start, addr_in.start);
			unmap_anon(addr)?;
		}
		unsafe {
			let addr_in = AddressRange { start: 0, size: 0x20000 };
			let addr = map_anon(addr_in, Protection::RW)?;
			addr.slice_mut()[0] = 13;
			unmap_anon(addr)?;
		}
		Ok(())
	}
}
