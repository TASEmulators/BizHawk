use crate::*;
use host::{ActivatedWaterboxHost, WaterboxHost};
use std::{os::raw::c_char, ffi::{/*CString, */CStr}, io::BufWriter};

/// The memory template for a WaterboxHost.  Don't worry about
/// making every size as small as possible, since the savestater handles sparse regions
/// well enough.  All values should be PAGESIZE aligned.
#[repr(C)]
pub struct MemoryLayoutTemplate {
	/// Absolute pointer to the start of the mapped space
	pub start: usize,
	/// Memory space for the elf executable.  The elf must be non-relocatable and
	/// all loaded segments must fit within [start..start + elf_size]
	pub elf_size: usize,
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
	pub fn make_layout(&self) -> anyhow::Result<WbxSysLayout> {
		let start = align_down(self.start);
		let elf_size = align_up(self.elf_size);
		let sbrk_size = align_up(self.sbrk_size);
		let sealed_size = align_up(self.sealed_size);
		let invis_size = align_up(self.invis_size);
		let plain_size = align_up(self.plain_size);
		let mmap_size = align_up(self.mmap_size);
		let mut res = unsafe { std::mem::zeroed::<WbxSysLayout>() };
		res.elf = AddressRange {
			start,
			size: elf_size
		};
		res.sbrk = AddressRange {
			start: res.elf.end(),
			size: sbrk_size
		};
		res.sealed = AddressRange {
			start: res.sbrk.end(),
			size: sealed_size
		};
		res.invis = AddressRange {
			start: res.sealed.end(),
			size: invis_size
		};
		res.plain = AddressRange {
			start: res.invis.end(),
			size: plain_size
		};
		res.mmap = AddressRange {
			start: res.invis.end(),
			size: mmap_size
		};
		if start >> 32 != (res.mmap.end() - 1) >> 32 {
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

/// stream writer
#[repr(C)]
pub struct CWriter {
	/// will be passed to callback
	pub userdata: usize,
	/// write bytes.  Return number of bytes written on success, or < 0 on failure.
	/// Permitted to write less than the provided number of bytes.
	pub callback: extern fn(userdata: usize, data: *const u8, size: usize) -> isize,
}
impl Write for CWriter {
	fn write(&mut self, buf: &[u8]) -> std::io::Result<usize> {
		let res = (self.callback)(self.userdata, buf.as_ptr(), buf.len());
		if res < 0 {
			Err(std::io::Error::new(std::io::ErrorKind::Other, "Callback signaled abnormal failure"))
		} else {
			Ok(res as usize)
		}
	}
	fn flush(&mut self) -> std::io::Result<()> {
		Ok(())
	}
}

/// stream reader
#[repr(C)]
pub struct CReader {
	/// will be passed to callback
	pub userdata: usize,
	/// Read bytes into the buffer.  Return number of bytes read on success, or < 0 on failure.
	/// permitted to read less than the provided buffer size, but must always read at least 1
	/// byte if EOF is not reached.  If EOF is reached, should return 0.
	pub callback: extern fn(userdata: usize, data: *mut u8, size: usize) -> isize,
}
impl Read for CReader {
	fn read(&mut self, buf: &mut [u8]) -> std::io::Result<usize> {
		let res = (self.callback)(self.userdata, buf.as_mut_ptr(), buf.len());
		if res < 0 {
			Err(std::io::Error::new(std::io::ErrorKind::Other, "Callback signaled abnormal failure"))
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
	std::io::copy(reader, &mut res)?;
	Ok(res)
}

/// Given a guest executable and a memory layout, create a new host environment.  All data will be immediately consumed from the reader,
/// which will not be used after this call.
#[no_mangle]
pub extern fn wbx_create_host(layout: &MemoryLayoutTemplate, module_name: *const c_char, wbx: &mut CReader, ret: &mut Return<*mut WaterboxHost>) {
	let res = (|| {
		let data = read_whole_file(wbx)?;
		WaterboxHost::new(&data[..], &arg_to_str(module_name)?[..], layout)
	})();
	ret.put(res.map(|boxed| Box::into_raw(boxed)));
}

/// Tear down a host environment.  May not be called while the environment is active.
#[no_mangle]
pub extern fn wbx_destroy_host(obj: *mut WaterboxHost, ret: &mut Return<()>) {
	let res = (|| {
		unsafe {
			if (*obj).active() {
				return Err(anyhow!("WaterboxHost is still active!"))
			}
			Box::from_raw(obj);
			Ok(())
		}
	})();
	ret.put(res);
}

/// Activate a host environment.  This swaps it into memory and makes it available for use.
/// Pointers to inside the environment are only valid while active.  Uses a mutex internally
/// so as to not stomp over other host environments in the same 4GiB slice.
/// Returns a pointer to the activated object, used to do most other functions.
#[no_mangle]
pub extern fn wbx_activate_host(obj: *mut WaterboxHost, ret: &mut Return<*mut ActivatedWaterboxHost>) {
	let res = (|| {
		unsafe {
			if (*obj).active() {
				return Err(anyhow!("WaterboxHost is already active!"))
			}
			Ok((&mut (*obj)).activate())
		}
	})();
	ret.put(res.map(|boxed| Box::into_raw(boxed)));
}

/// Deactivates a host environment, and releases the mutex.
#[no_mangle]
pub extern fn wbx_deactivate_host(obj: *mut ActivatedWaterboxHost, ret: &mut Return<()>) {
	unsafe { Box::from_raw(obj); }
	ret.put(Ok(()));
}

/// Returns the address of an exported function from the guest executable.  This pointer is only valid
/// while the host is active.  A missing proc is not an error and simply returns 0.
#[no_mangle]
pub extern fn wbx_get_proc_addr(obj: &mut ActivatedWaterboxHost, name: *const c_char, ret: &mut Return<usize>) {
	match arg_to_str(name) {
		Ok(s) => {
			ret.put(Ok(obj.get_proc_addr(&s)));
		},
		Err(e) => {
			ret.put(Err(e))
		}
	}
}

/// Calls the seal operation, which is a one time action that prepares the host to save states.
#[no_mangle]
pub extern fn wbx_seal(obj: &mut ActivatedWaterboxHost, ret: &mut Return<()>) {
	ret.put(obj.seal());
}

/// Mounts a file in the environment.  All data will be immediately consumed from the reader, which will not be used after this call.
/// To prevent nondeterminism, adding and removing files is very limited WRT savestates.  If a file is writable, it must never exist
/// when save_state is called, and can only be used for transient operations.  If a file is readable, it can appear in savestates,
/// but it must exist in every savestate and the exact sequence of add_file calls must be consistent from savestate to savestate.
#[no_mangle]
pub extern fn wbx_mount_file(obj: &mut ActivatedWaterboxHost, name: *const c_char, reader: &mut CReader, writable: bool, ret: &mut Return<()>) {
	let res: anyhow::Result<()> = (|| {
		obj.mount_file(arg_to_str(name)?, read_whole_file(reader)?, writable)?;
		Ok(())
	})();
	ret.put(res);
}

/// Remove a file previously added.  Writer is optional; if provided, the contents of the file at time of removal will be dumped to it.
/// It is an error to remove a file which is currently open in the guest.
#[no_mangle]
pub extern fn wbx_unmount_file(obj: &mut ActivatedWaterboxHost, name: *const c_char, writer: Option<&mut CWriter>, ret: &mut Return<()>) {
	let res: anyhow::Result<()> = (|| {
		let data = obj.unmount_file(&arg_to_str(name)?)?;
		if let Some(w) = writer {
			std::io::copy(&mut &data[..], w)?;
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
// pub extern fn wbx_set_missing_file_callback(obj: &mut ActivatedWaterboxHost, mfc_o: Option<&MissingFileCallback>) {
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
pub extern fn wbx_save_state(obj: &mut ActivatedWaterboxHost, writer: &mut CWriter, ret: &mut Return<()>) {
	// TODO: Is this bufwriter worth it because of the native<->managed transitions, or worth it only because
	// the managed side doesn't have Span support and so makes an extra copy?
	let mut buffered = BufWriter::new(writer);
	let res: anyhow::Result<()> = (|| {
		obj.save_state(&mut buffered)?;
		buffered.flush()?;
		Ok(())
	})();
	ret.put(res);
}
/// Load state.  Must not be called before seal.  Must not be called with any writable files mounted.
/// Must always be called with the same sequence and contents of readonly files that were in the save state.
/// Must be called with the same wbx executable and memory layout as in the savestate.
/// Errors generally poison the environment; sorry!
#[no_mangle]
pub extern fn wbx_load_state(obj: &mut ActivatedWaterboxHost, reader: &mut CReader, ret: &mut Return<()>) {
	ret.put(obj.load_state(reader));
}
