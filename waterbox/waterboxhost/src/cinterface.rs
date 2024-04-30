use crate::*;
use host::{WaterboxHost};
use std::{os::raw::c_char, io, ffi::{/*CString, */CStr}};
use context::ExternalCallback;

/// The memory template for a WaterboxHost.  Don't worry about
/// making every size as small as possible, since the savestater handles sparse regions
/// well enough.  All values should be PAGESIZE aligned.
#[repr(C)]
pub struct MemoryLayoutTemplate {
	/// Memory space to serve brk(2)
	pub sbrk_size: usize,
	/// Memory space to serve alloc_sealed(3)
	pub sealed_size: usize,
	/// Memory space to serve alloc_invisible(3)
	pub invis_size: usize,
	/// Memory space to serve alloc_plain(3)
	pub plain_size: usize,
	/// Memory space to serve mmap(2) and friends.
	/// Calls without MAP_FIXED or MREMAP_FIXED will be placed in this area.
	/// TODO: Are we allowing fixed calls to happen anywhere in the block?
	pub mmap_size: usize,
}
impl MemoryLayoutTemplate {
	/// checks a memory layout for validity
	pub fn make_layout(&self, elf_addr: AddressRange) -> anyhow::Result<WbxSysLayout> {
		let mut res = unsafe { std::mem::zeroed::<WbxSysLayout>() };
		res.elf = elf_addr.align_expand();

		let mut end = res.elf.end();
		let mut add_area = |size| {
			let a = AddressRange {
				start: end,
				size: align_up(size)
			};
			end = a.end();
			a
		};
		res.main_thread = add_area(1 << 20);
		res.alt_thread = add_area(1 << 20);
		res.sbrk = add_area(self.sbrk_size);
		res.sealed = add_area(self.sealed_size);
		res.invis = add_area(self.invis_size);
		res.plain = add_area(self.plain_size);
		res.mmap = add_area(self.mmap_size);

		if res.all().start >> 32 != (res.all().end() - 1) >> 32 {
			Err(anyhow!("HostMemoryLayout must fit into a single 4GiB region!"))
		} else {
			Ok(res)
		}
	}
}

/// "return" struct.  On successful funtion call, error_message[0] will be 0 and data will be the return value.
/// On failed call, error_message will contain a string describing the error, and data will be unspecified.
/// Any function that takes this object as an argument can fail and should be checked for failure, even if
/// it does not return data.
#[repr(C)]
pub struct Return<T> {
	pub error_message: [u8; 1024],
	pub data: T,
}
impl<T> Return<T> {
	pub fn put(&mut self, result: anyhow::Result<T>) {
		match result {
			Err(e) => {
				let s = format!("Waterbox Error: {:?}", e);
				let len = std::cmp::min(s.len(), 1023);
				self.error_message[0..len].copy_from_slice(&s.as_bytes()[0..len]);
				self.error_message[len] = 0;
			},
			Ok(t) => {
				self.error_message[0] = 0;
				self.data = t;
			}
		}
	}
}

/// write bytes.  Return 0 on success, or < 0 on failure.
/// Must write all provided bytes in one call or fail, not permitted to write less (unlike reader).
pub type WriteCallback = extern fn(userdata: usize, data: *const u8, size: usize) -> i32;
struct CWriter {
	/// will be passed to callback
	pub userdata: usize,
	pub callback: WriteCallback,
}
impl Write for CWriter {
	fn write(&mut self, buf: &[u8]) -> io::Result<usize> {
		let res = (self.callback)(self.userdata, buf.as_ptr(), buf.len());
		if res < 0 {
			Err(io::Error::new(io::ErrorKind::Other, "Callback signaled abnormal failure"))
		} else {
			Ok(buf.len())
		}
	}
	fn write_all(&mut self, buf: &[u8]) -> io::Result<()> {
		self.write(buf)?;
		Ok(())
	}
	fn flush(&mut self) -> io::Result<()> {
		Ok(())
	}
}

/// Read bytes into the buffer.  Return number of bytes read on success, or < 0 on failure.
/// permitted to read less than the provided buffer size, but must always read at least 1
/// byte if EOF is not reached.  If EOF is reached, should return 0.
pub type ReadCallback = extern fn(userdata: usize, data: *mut u8, size: usize) -> isize;
struct CReader {
	pub userdata: usize,
	pub callback: ReadCallback,
}
impl Read for CReader {
	fn read(&mut self, buf: &mut [u8]) -> io::Result<usize> {
		let res = (self.callback)(self.userdata, buf.as_mut_ptr(), buf.len());
		if res < 0 {
			Err(io::Error::new(io::ErrorKind::Other, "Callback signaled abnormal failure"))
		} else {
			Ok(res as usize)
		}
	}
}

// #[repr(C)]
// pub struct MissingFileCallback {
// 	pub userdata: usize,
// 	pub callback: extern fn(userdata: usize, name: *const c_char) -> *mut MissingFileResult,
// }

// #[repr(C)]
// pub struct MissingFileResult {
// 	pub reader: CReader,
// 	pub writable: bool,
// }

fn arg_to_str(arg: *const c_char) -> anyhow::Result<String> {
	let cs = unsafe { CStr::from_ptr(arg as *const c_char) };
	match cs.to_str() {
		Ok(s) => Ok(s.to_string()),
		Err(_) => Err(anyhow!("Bad UTF-8 string")),
	}
}

fn read_whole_file(reader: &mut CReader) -> anyhow::Result<Vec<u8>> {
	let mut res = Vec::<u8>::new();
	io::copy(reader, &mut res)?;
	Ok(res)
}

/// Given a guest executable and a memory layout, create a new host environment.  All data will be immediately consumed from the reader,
/// which will not be used after this call.
#[no_mangle]
pub extern fn wbx_create_host(layout: &MemoryLayoutTemplate, module_name: *const c_char, callback: ReadCallback, userdata: usize, ret: &mut Return<*mut WaterboxHost>) {
	let mut reader = CReader {
		userdata,
		callback
	};
	let res = (|| {
		let data = read_whole_file(&mut reader)?;
		WaterboxHost::new(data, &arg_to_str(module_name)?[..], layout)
	})();
	ret.put(res.map(|boxed| Box::into_raw(boxed)));
}

/// Tear down a host environment.  If called while the environment is active, will deactivate it first.
#[no_mangle]
pub extern fn wbx_destroy_host(obj: *mut WaterboxHost, ret: &mut Return<()>) {
	let res = (|| {
		unsafe {
			drop(Box::from_raw(obj));
			Ok(())
		}
	})();
	ret.put(res);
}

/// Activate a host environment.  This swaps it into memory and makes it available for use.
/// Pointers to inside the environment are only valid while active.  Callbacks into the environment can only be called
/// while active.  Uses a mutex internally so as to not stomp over other host environments in the same 4GiB slice.
/// Ignored if host is already active.
#[no_mangle]
pub extern fn wbx_activate_host(obj: &mut WaterboxHost, ret: &mut Return<()>) {
	let res = (|| {
		obj.activate();
		Ok(())
	})();
	ret.put(res);
}

/// Deactivates a host environment, and releases the mutex.
/// Ignored if host is not active
#[no_mangle]
pub extern fn wbx_deactivate_host(obj: &mut WaterboxHost, ret: &mut Return<()>) {
	obj.deactivate();
	ret.put(Ok(()));
}

/// Returns a thunk suitable for calling an exported function from the guest executable.  This pointer is only valid
/// while the host is active.  A missing proc is not an error and simply returns 0.  The guest function must be,
/// and the returned callback will be, sysv abi, and will only pass up to 6 int/ptr args and no other arg types.
#[no_mangle]
pub extern fn wbx_get_proc_addr(obj: &mut WaterboxHost, name: *const c_char, ret: &mut Return<usize>) {
	match arg_to_str(name) {
		Ok(s) => {
			ret.put(obj.get_proc_addr(&s));
		},
		Err(e) => {
			ret.put(Err(e))
		}
	}
}
/// Returns a thunk suitable for calling an arbitrary entry point into the guest executable.  This pointer is only valid
/// while the host is active.  wbx_get_proc_addr already calls this internally on pointers it returns, so this call is
/// only needed if the guest exposes callin pointers that aren't named exports (for instance, if a function returns
/// a pointer to another function).
#[no_mangle]
pub extern fn wbx_get_callin_addr(obj: &mut WaterboxHost, ptr: usize, ret: &mut Return<usize>) {
	ret.put(obj.get_external_callin_ptr(ptr));
}
/// Returns the raw address of a function exported from the guest.  `wbx_get_proc_addr()` is equivalent to
/// `wbx_get_callin_addr(wbx_get_proc_addr_raw()).  Most things should not use this directly, as the returned
/// pointer will not have proper stack hygiene and will crash on syscalls from the guest.
#[no_mangle]
pub extern fn wbx_get_proc_addr_raw(obj: &mut WaterboxHost, name: *const c_char, ret: &mut Return<usize>) {
	match arg_to_str(name) {
		Ok(s) => {
			ret.put(obj.get_proc_addr_raw(&s));
		},
		Err(e) => {
			ret.put(Err(e))
		}
	}
}

/// Returns a function pointer suitable for passing to the guest to allow it to call back while active.
/// Slot number is an integer that is used to keep pointers consistent across runs:  If the host is loaded
/// at a different address, and some external function `foo` moves from run to run, things will still work out
/// in the guest because `foo` was bound to the same slot and a particular slot gives a consistent pointer.
/// The returned thunk will be, and the callback must be, sysv abi and will only pass up to 6 int/ptr args and no other arg types.
#[no_mangle]
pub extern fn wbx_get_callback_addr(obj: &mut WaterboxHost, callback: ExternalCallback, slot: usize, ret: &mut Return<usize>) {
	ret.put(obj.get_external_callback_ptr(callback, slot));
}

/// Calls the seal operation, which is a one time action that prepares the host to save states.
#[no_mangle]
pub extern fn wbx_seal(obj: &mut WaterboxHost, ret: &mut Return<()>) {
	ret.put(obj.seal());
}

/// Mounts a file in the environment.  All data will be immediately consumed from the reader, which will not be used after this call.
/// To prevent nondeterminism, adding and removing files is very limited WRT savestates.  Every file added must either exist
/// in every savestate, or never appear in any savestates.  All savestateable files must be added in the same order for every run.
#[no_mangle]
pub extern fn wbx_mount_file(obj: &mut WaterboxHost, name: *const c_char, callback: ReadCallback, userdata: usize, writable: bool, ret: &mut Return<()>) {
	let mut reader = CReader {
		userdata,
		callback
	};
	let res: anyhow::Result<()> = (|| {
		obj.mount_file(arg_to_str(name)?, read_whole_file(&mut reader)?, writable)?;
		Ok(())
	})();
	ret.put(res);
}

/// Remove a file previously added.  Writer is optional; if provided, the contents of the file at time of removal will be dumped to it.
/// It is an error to remove a file which is currently open in the guest.
/// If the file has been used in savestates, it does not make sense to remove it here, but nothing will stop you.
#[no_mangle]
pub extern fn wbx_unmount_file(obj: &mut WaterboxHost, name: *const c_char, callback_opt: Option<WriteCallback>, userdata: usize, ret: &mut Return<()>) {
	let res: anyhow::Result<()> = (|| {
		let data = obj.unmount_file(&arg_to_str(name)?)?;
		if let Some(callback) = callback_opt {
			let mut writer = CWriter {
				userdata,
				callback
			};
			io::copy(&mut &data[..], &mut writer)?;
		}
		Ok(())
	})();
	ret.put(res);
}

/// Set (or clear, with None) a callback to be called whenever the guest tries to load a nonexistant file.
/// The callback will be provided with the name of the requested load, and can either return null to signal the waterbox
/// to return ENOENT to the guest, or a struct to immediately load that file.  You may not call any wbx methods
/// in the callback.  If the MissingFileResult is provided, it will be consumed immediately and will have the same effect
/// as wbx_mount_file().  You may free resources associated with the MissingFileResult whenever control next returns to your code.
// #[no_mangle]
// pub extern fn wbx_set_missing_file_callback(obj: &mut WaterboxHost, mfc_o: Option<&MissingFileCallback>) {
// 	match mfc_o {
// 		None => obj.set_missing_file_callback(None),
// 		Some(mfc) => {
// 			let userdata = mfc.userdata;
// 			let callback = mfc.callback;
// 			obj.set_missing_file_callback(Some(Box::new(move |name| {
// 				let namestr = CString::new(name).unwrap();
// 				let mfr = callback(userdata, namestr.as_ptr() as *const c_char);
// 				if mfr == 0 as *mut MissingFileResult {
// 					return None
// 				}
// 				unsafe {
// 					let data = read_whole_file(&mut (*mfr).reader);
// 					match data {
// 						Ok(d) => Some(fs::MissingFileResult {
// 							data: d,
// 							writable: (*mfr).writable
// 						}),
// 						Err(_) => None,
// 					}
// 				}
// 			})));
// 		}
// 	}
// }

/// Save state.  Must not be called before seal.  Must not be called with any writable files mounted.
/// Must always be called with the same sequence and contents of readonly files.
#[no_mangle]
pub extern fn wbx_save_state(obj: &mut WaterboxHost, callback: WriteCallback, userdata: usize, ret: &mut Return<()>) {
	let mut writer = CWriter {
		userdata,
		callback
	};
	let res: anyhow::Result<()> = (|| {
		obj.save_state(&mut writer)?;
		Ok(())
	})();
	ret.put(res);
}
/// Load state.  Must not be called before seal.  Must not be called with any writable files mounted.
/// Must always be called with the same sequence and contents of readonly files that were in the save state.
/// Must be called with the same wbx executable and memory layout as in the savestate.
/// Errors generally poison the environment; sorry!
#[no_mangle]
pub extern fn wbx_load_state(obj: &mut WaterboxHost, callback: ReadCallback, userdata: usize, ret: &mut Return<()>) {
	let mut reader = CReader {
		userdata,
		callback
	};
	ret.put(obj.load_state(&mut reader));
}

/// Control whether the host automatically evicts blocks from memory when they are not active.  For the best performance,
/// this should be set to false.  Set to true to help catch dangling pointer issues.  Will be ignored (and forced to true)
/// if waterboxhost was built in debug mode.  This is a single global setting.
#[no_mangle]
pub extern fn wbx_set_always_evict_blocks(_val: bool) {
	#[cfg(not(debug_assertions))]
	{
		unsafe { ALWAYS_EVICT_BLOCKS = _val; }
	}
}

/// Retrieve the number of pages of guest memory that this host is tracking
#[no_mangle]
pub extern fn wbx_get_page_len(obj: &mut WaterboxHost, ret: &mut Return<usize>) {
	ret.put(Ok(obj.page_len()))
}

/// Retrieve basic information for a tracked guest page.  Index should be in 0..wbx_get_page_len().
/// 1 - readable, implies allocated
/// 2 - writable
/// 4 - executable
/// 0x10 - stack
/// 0x20 - allocated but not readable (guest-generated "guard")
/// 0x40 - invisible
/// 0x80 - dirty
#[no_mangle]
pub extern fn wbx_get_page_data(obj: &mut WaterboxHost, index: usize, ret: &mut Return<u8>) {
	if index >= obj.page_len() {
		ret.put(Err(anyhow!("Index out of range")))
	} else {
		ret.put(Ok(obj.page_info(index)))
	}
}
