#![cfg_attr(docsrs, feature(doc_cfg, doc_cfg_hide))]
#![deny(unsafe_op_in_unsafe_fn)]
//! A (mostly) [libchdr](https://github.com/rtissera/libchdr) compatible C-API for [chd-rs](https://crates.io/crates/chd).
//!
//! For Rust consumers, consider using [chd-rs](https://crates.io/crates/chd) instead.
//!
//! The best way to integrate chd-rs in your C or C++ project is to instead vendor the [sources](https://github.com/SnowflakePowered/chd-rs) directly
//! into your project, with a compatible implementation of [libchdcorefile](https://github.com/SnowflakePowered/chd-rs/tree/master/chd-rs-capi/libchdcorefile)
//! for your platform as required.
//!
//! ## ABI compatibility with libchdr
//!
//! chd-rs-capi makes the following ABI-compatibility guarantees compared to libchdr when compiled statically.
//! * `chd_error` is ABI and API-compatible with [chd.h](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/include/libchdr/chd.h#L258)
//! * `chd_header` is ABI and API-compatible [chd.h](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/include/libchdr/chd.h#L302)
//! * `chd_file *` is an opaque pointer. It is **not layout compatible** with [chd.c](https://github.com/rtissera/libchdr/blob/cdcb714235b9ff7d207b703260706a364282b063/src/libchdr_chd.c#L265)
//! * The layout of `core_file *` is user-defined when the `chd_core_file` feature is enabled.
//! * Freeing any pointer returned by chd-rs with `free` is undefined behaviour. The exception are `chd_file *` pointers which can be safely freed with `chd_close`.

extern crate core;

mod header;

#[cfg(feature = "chd_core_file")]
mod chdcorefile;

#[cfg(feature = "chd_core_file")]
#[allow(non_camel_case_types)]
#[allow(unused)]
mod chdcorefile_sys;

use crate::header::chd_header;
use chd::header::Header;
use chd::metadata::{KnownMetadata, Metadata, MetadataTag};
pub use chd::Error as chd_error;
use chd::{Chd, Error};
use std::any::Any;
use std::ffi::{CStr, CString};
use std::fs::File;
use std::io::{BufReader, Cursor, Read, Seek};
use std::mem::MaybeUninit;
use std::os::raw::{c_char, c_int, c_void};
use std::path::Path;
use std::slice;

/// Open a CHD for reading.
pub const CHD_OPEN_READ: i32 = 1;
/// Open a CHD for reading and writing. This mode is not supported and will always return an error
/// when passed into a constructor function such as [`chd_open`](crate::chd_open).
pub const CHD_OPEN_READWRITE: i32 = 2;

/// Trait alias for `Read + Seek + Any`.
#[doc(hidden)]
pub trait SeekRead: Any + Read + Seek {
    fn as_any(&self) -> &dyn Any;
}

impl<R: Any + Read + Seek> SeekRead for BufReader<R> {
    fn as_any(&self) -> &dyn Any {
        self
    }
}

impl SeekRead for Cursor<Vec<u8>> {
    fn as_any(&self) -> &dyn Any {
        self
    }
}

#[allow(non_camel_case_types)]
/// An opaque type for an opened CHD file.
pub type chd_file = Chd<Box<dyn SeekRead>>;

fn ffi_takeown_chd(chd: *mut chd_file) -> Box<Chd<Box<dyn SeekRead>>> {
    unsafe { Box::from_raw(chd) }
}

fn ffi_expose_chd(chd: Box<Chd<Box<dyn SeekRead>>>) -> *mut chd_file {
    Box::into_raw(chd)
}

fn ffi_open_chd(
    filename: *const c_char,
    parent: Option<Box<chd_file>>,
) -> Result<chd_file, chd_error> {
    let c_filename = unsafe { CStr::from_ptr(filename) };
    let filename = std::str::from_utf8(c_filename.to_bytes())
        .map(Path::new)
        .map_err(|_| chd_error::InvalidParameter)?;

    let file = File::open(filename).map_err(|_| chd_error::FileNotFound)?;

    let bufread = Box::new(BufReader::new(file)) as Box<dyn SeekRead>;
    Chd::open(bufread, parent)
}

/// Opens a CHD file by file name, with a layout-undefined backing file pointer owned by
/// the library.
///
/// The result of passing an object created by this function into [`chd_core_file`](crate::chd_core_file)
/// is strictly undefined. Instead, all `chd_file*` pointers with provenance from `chd_open` should be
/// closed with [`chd_close`](crate::chd_close).
///
/// # Safety
/// * `filename` is a valid, null-terminated **UTF-8** string.
/// * `parent` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * `out` is aligned and can store a pointer to a `chd_file*`. On success, `out` will point to a valid `chd_file*`.
/// * After this function returns, `parent` is invalid and must not be used, otherwise it will be undefined behaviour. There is no way to retake ownership of `parent`.
#[no_mangle]
pub unsafe extern "C" fn chd_open(
    filename: *const c_char,
    mode: c_int,
    parent: *mut chd_file,
    out: *mut *mut chd_file,
) -> chd_error {
    // we don't support READWRITE mode
    if mode == CHD_OPEN_READWRITE {
        return chd_error::FileNotWriteable;
    }

    let parent = if parent.is_null() {
        None
    } else {
        Some(ffi_takeown_chd(parent))
    };

    let chd = match ffi_open_chd(filename, parent) {
        Ok(chd) => chd,
        Err(e) => return e,
    };

    unsafe { *out = ffi_expose_chd(Box::new(chd)) }
    chd_error::None
}

#[no_mangle]
/// Close a CHD file.
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * If `chd` is `NULL`, this does nothing.
pub unsafe extern "C" fn chd_close(chd: *mut chd_file) {
    if !chd.is_null() {
        unsafe { drop(Box::from_raw(chd)) }
    }
}

#[no_mangle]
/// Returns an error string for the corresponding CHD error.
///
/// # Safety
/// The returned string is leaked and the memory **should not and can not ever** be validly freed.
/// Attempting to free the returned pointer with `free` is **undefined behaviour**.
pub unsafe extern "C" fn chd_error_string(err: chd_error) -> *const c_char {
    // SAFETY: This will leak, but this is much safer than
    // potentially allowing the C caller to corrupt internal state
    // by returning an internal pointer to an interned string.
    let err_string = unsafe { CString::new(err.to_string()).unwrap_unchecked() };
    err_string.into_raw()
}

fn ffi_chd_get_header(chd: &chd_file) -> chd_header {
    match chd.header() {
        Header::V5Header(_) => header::get_v5_header(chd),
        Header::V1Header(h) | Header::V2Header(h) => h.into(),
        Header::V3Header(h) => h.into(),
        Header::V4Header(h) => h.into(),
    }
}
#[no_mangle]
/// Returns a pointer to the extracted CHD header data.
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * If `chd` is `NULL`, returns `NULL`.
/// * The returned pointer is leaked and the memory **should not and can not ever** be validly freed. Attempting to free the returned pointer with `free` is **undefined behaviour**. A non-leaking variant is provided in [`chd_read_header`](crate::chd_read_header).
pub unsafe extern "C" fn chd_get_header(chd: *const chd_file) -> *const chd_header {
    match unsafe { chd.as_ref() } {
        Some(chd) => {
            let header = ffi_chd_get_header(chd);
            Box::into_raw(Box::new(header))
        }
        None => std::ptr::null(),
    }
}

#[no_mangle]
/// Read a single hunk from the CHD file.
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * `buffer` must an aligned pointer to a block of initialized memory of exactly the hunk size for the input `chd_file*` that is valid for both reads and writes. This size can be found with [`chd_get_header`](crate::chd_get_header).
/// * If `chd` is `NULL`, returns `CHDERR_INVALID_PARAMETER`.
pub unsafe extern "C" fn chd_read(
    chd: *mut chd_file,
    hunknum: u32,
    buffer: *mut c_void,
) -> chd_error {
    match unsafe { chd.as_mut() } {
        None => chd_error::InvalidParameter,
        Some(chd) => {
            let hunk = chd.hunk(hunknum);
            if let Ok(mut hunk) = hunk {
                let size = hunk.len();
                let mut comp_buf = Vec::new();
                // SAFETY: The output buffer *must* be initialized and
                // have a length of exactly the hunk size.
                let output: &mut [u8] =
                    unsafe { slice::from_raw_parts_mut(buffer as *mut u8, size) };
                let result = hunk.read_hunk_in(&mut comp_buf, output);
                match result {
                    Ok(_) => chd_error::None,
                    Err(e) => e,
                }
            } else {
                chd_error::HunkOutOfRange
            }
        }
    }
}

fn find_metadata(
    chd: &mut chd_file,
    search_tag: u32,
    mut search_index: u32,
) -> Result<Metadata, Error> {
    for entry in chd.metadata_refs() {
        if entry.metatag() == search_tag || entry.metatag() == KnownMetadata::Wildcard.metatag() {
            if search_index == 0 {
                return entry.read(chd.inner());
            }
            search_index -= 1;
        }
    }
    Err(Error::MetadataNotFound)
}
#[no_mangle]
/// Get indexed metadata of the given search tag and index.
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * `output` must be an aligned pointer to a block of initialized memory of size exactly `output_len` that is valid for writes.
/// * `result_len` must be either NULL or an aligned pointer to a `uint32_t` that is valid for writes.
/// * `result_tag` must be either NULL or an aligned pointer to a `uint32_t` that is valid for writes.
/// * `result_flags` must be either NULL or an aligned pointer to a `uint8_t` that is valid for writes.
/// * If `chd` is `NULL`, returns `CHDERR_INVALID_PARAMETER`.
pub unsafe extern "C" fn chd_get_metadata(
    chd: *mut chd_file,
    searchtag: u32,
    searchindex: u32,
    output: *mut c_void,
    output_len: u32,
    result_len: *mut u32,
    result_tag: *mut u32,
    result_flags: *mut u8,
) -> chd_error {
    match unsafe { chd.as_mut() } {
        Some(chd) => {
            let entry = find_metadata(chd, searchtag, searchindex);
            match (entry, searchtag) {
                (Ok(meta), _) => {
                    unsafe {
                        let output_len = std::cmp::min(output_len, meta.value.len() as u32);
                        std::ptr::copy_nonoverlapping(
                            meta.value.as_ptr() as *const c_void,
                            output,
                            output_len as usize,
                        );

                        if !result_tag.is_null() {
                            result_tag.write(meta.metatag)
                        }
                        if !result_len.is_null() {
                            result_len.write(meta.length)
                        }
                        if !result_flags.is_null() {
                            result_flags.write(meta.flags)
                        }
                    }
                    chd_error::None
                }
                (Err(_), tag) => unsafe {
                    if (tag == KnownMetadata::HardDisk.metatag()
                        || tag == KnownMetadata::Wildcard.metatag())
                        && searchindex == 0
                    {
                        let header = chd.header();
                        if let Header::V1Header(header) = header {
                            let fake_meta = format!(
                                "CYLS:{},HEADS:{},SECS:{},BPS:{}",
                                header.cylinders,
                                header.heads,
                                header.sectors,
                                header.hunk_bytes / header.hunk_size
                            );
                            let cstring = CString::from_vec_unchecked(fake_meta.into_bytes());
                            let bytes = cstring.into_bytes_with_nul();
                            let len = bytes.len();
                            let output_len = std::cmp::min(output_len, len as u32);

                            std::ptr::copy_nonoverlapping(
                                bytes.as_ptr() as *const c_void,
                                output,
                                output_len as usize,
                            );
                            if !result_tag.is_null() {
                                result_tag.write(KnownMetadata::HardDisk.metatag())
                            }
                            if !result_len.is_null() {
                                result_len.write(len as u32)
                            }
                            return chd_error::None;
                        }
                    }
                    chd_error::MetadataNotFound
                },
            }
        }
        None => chd_error::InvalidParameter,
    }
}

#[no_mangle]
/// Set codec internal parameters.
///
/// This function is not supported and always returns `CHDERR_INVALID_PARAMETER`.
pub extern "C" fn chd_codec_config(
    _chd: *const chd_file,
    _param: i32,
    _config: *mut c_void,
) -> chd_error {
    chd_error::InvalidParameter
}

#[no_mangle]
/// Read CHD header data from the file into the pointed struct.
///
/// # Safety
/// * `filename` is a valid, null-terminated **UTF-8** string.
/// * `header` is either `NULL`, or an aligned pointer to a possibly uninitialized `chd_header` struct.
/// * If `header` is `NULL`, returns `CHDERR_INVALID_PARAMETER`
pub unsafe extern "C" fn chd_read_header(
    filename: *const c_char,
    header: *mut MaybeUninit<chd_header>,
) -> chd_error {
    let chd = ffi_open_chd(filename, None);
    match chd {
        Ok(chd) => {
            let chd_header = ffi_chd_get_header(&chd);
            match unsafe { header.as_mut() } {
                None => Error::InvalidParameter,
                Some(header) => {
                    header.write(chd_header);
                    Error::None
                }
            }
        }
        Err(e) => e,
    }
}

#[no_mangle]
#[cfg(feature = "chd_core_file")]
#[cfg_attr(docsrs, doc(cfg(chd_core_file)))]
/// Returns the associated `core_file*`.
///
/// This method has different semantics than `chd_core_file` in libchdr.
///
/// The input `chd_file*` will be dropped, and all prior references to
/// to the input `chd_file*` are rendered invalid, with the same semantics as `chd_close`.
///
/// The provenance of the `chd_file*` is important to keep in mind.
///
/// If the input `chd_file*` was opened with [`chd_open`](crate::chd_open), the input `chd_file*` will be closed,
/// and the return value should be considered undefined. For now it is `NULL`, but relying on this
/// behaviour is unstable and may change in the future.
///
/// If the input `chd_file*` was opened with `chd_open_file` and the `chd_core_file` crate feature
/// is enabled, this method will return the same pointer as passed to `chd_input_file`, which may
/// be possible to cast to `FILE*` depending on the implementation of `libchdcorefile` that was
/// linked.
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * If `chd` is `NULL`, returns `NULL`.
/// * If `chd` has provenance from [`chd_open`](crate::chd_open), the returned pointer is undefined and must not be used.
/// * `chd` is **no longer valid** upon return of this function, and subsequent reuse of the `chd_file*` pointer is **undefined behaviour**.
pub unsafe extern "C" fn chd_core_file(chd: *mut chd_file) -> *mut chdcorefile_sys::core_file {
    if chd.is_null() {
        return std::ptr::null_mut();
    }

    let (file, _) = ffi_takeown_chd(chd).into_inner();
    let file_ref = file.as_any();

    let pointer = match file_ref.downcast_ref::<crate::chdcorefile::CoreFile>() {
        None => std::ptr::null_mut(),
        Some(file) => file.0,
    };
    std::mem::forget(file);
    pointer
}

#[no_mangle]
#[cfg(feature = "chd_core_file")]
#[cfg_attr(docsrs, doc(cfg(chd_core_file)))]
/// Open an existing CHD file from an opened `core_file` object.
///
/// Ownership is taken of the `core_file*` object and should not be modified until
/// `chd_core_file` is called to retake ownership of the `core_file*`.
///
/// # Safety
/// * `file` is a valid pointer to a `core_file` with respect to the implementation of libchdcorefile that was linked.
/// * `parent` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * `out` is aligned and can store a pointer to a `chd_file*`. On success, `out` will point to a valid `chd_file*`.
/// * Until the returned `chd_file*` in `out` is closed with [`chd_close`](crate::chd_close) or [`chd_core_file`](crate::chd_core_file), external mutation of `file` will result in undefined behaviour.
/// * After this function returns, `parent` is invalid and must not be used, otherwise it will be undefined behaviour. There is no way to retake ownership of `parent`.
pub unsafe extern "C" fn chd_open_file(
    file: *mut chdcorefile_sys::core_file,
    mode: c_int,
    parent: *mut chd_file,
    out: *mut *mut chd_file,
) -> chd_error {
    // we don't support READWRITE mode
    if mode == CHD_OPEN_READWRITE {
        return chd_error::FileNotWriteable;
    }

    let parent = if parent.is_null() {
        None
    } else {
        Some(ffi_takeown_chd(parent))
    };

    let core_file = Box::new(crate::chdcorefile::CoreFile(file)) as Box<dyn SeekRead>;
    let chd = match Chd::open(core_file, parent) {
        Ok(chd) => chd,
        Err(e) => return e,
    };

    unsafe { *out = ffi_expose_chd(Box::new(chd)) }
    chd_error::None
}

#[no_mangle]
#[cfg(feature = "chd_virtio")]
#[cfg_attr(docsrs, doc(cfg(chd_virtio)))]
/// Open an existing CHD file from an opened `core_file` object.
///
/// Ownership is taken of the `core_file*` object and should not be modified until
/// `chd_core_file` is called to retake ownership of the `core_file*`.
///
/// # Safety
/// * `file` is a valid pointer to a `core_file` with respect to the implementation of libchdcorefile that was linked.
/// * `parent` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
/// * `out` is aligned and can store a pointer to a `chd_file*`. On success, `out` will point to a valid `chd_file*`.
/// * Until the returned `chd_file*` in `out` is closed with [`chd_close`](crate::chd_close) or [`chd_core_file`](crate::chd_core_file), external mutation of `file` will result in undefined behaviour.
/// * After this function returns, `parent` is invalid and must not be used, otherwise it will be undefined behaviour. There is no way to retake ownership of `parent`.
pub unsafe extern "C" fn chd_open_core_file(
    file: *mut chdcorefile_sys::core_file,
    mode: c_int,
    parent: *mut chd_file,
    out: *mut *mut chd_file,
) -> chd_error {
    chd_open_file(file, mode, parent, out)
}

#[no_mangle]
/// Get the name of a particular codec.
///
/// This method always returns the string "Unknown"
pub extern "C" fn chd_get_codec_name(_codec: u32) -> *const c_char {
    b"Unknown\0".as_ptr() as *const c_char
}

#[cfg(feature = "chd_precache")]
use std::io::SeekFrom;

#[cfg(feature = "chd_precache")]
#[cfg_attr(docsrs, doc(cfg(chd_precache)))]
/// The chunk size to read when pre-caching the underlying file stream into memory.
pub const PRECACHE_CHUNK_SIZE: usize = 16 * 1024 * 1024;

#[no_mangle]
#[cfg(feature = "chd_precache")]
#[cfg_attr(docsrs, doc(cfg(chd_precache)))]
/// Precache the underlying file into memory with an optional callback to report progress.
///
/// The underlying stream of the input `chd_file` is swapped with a layout-undefined in-memory stream.
///
/// If the provenance of the original `chd_file` is from [`chd_open`](crate::chd_open), then the underlying
/// stream is safely dropped.
///
/// If instead the underlying stream is a `core_file` opened from [`chd_open_file`](crate::chd_open_file),
/// or [`chd_open_core_file`](crate::chd_open_core_file), then the same semantics of calling [`chd_core_file`](crate::chd_core_file)
/// applies, and ownership of the underlying stream is released to the caller.
///
/// After precaching, the input `chd_file` no longer returns a valid inner stream when passed to [`chd_core_file`](crate::chd_core_file),
/// and should be treated as having the same provenance as being from [`chd_open`](crate::chd_open).
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
pub unsafe extern "C" fn chd_precache_progress(
    chd: *mut chd_file,
    progress: Option<unsafe extern "C" fn(pos: usize, total: usize, param: *mut c_void)>,
    param: *mut c_void,
) -> chd_error {
    let chd_file = if let Some(chd) = unsafe { chd.as_mut() } {
        chd
    } else {
        return chd_error::InvalidParameter;
    };

    // if the inner is already a cursor over Vec<u8>, then it's already cached.
    if chd_file.inner().as_any().is::<Cursor<Vec<u8>>>() {
        return chd_error::None;
    }

    let file = chd_file.inner();
    let length = if let Ok(length) = file.seek(SeekFrom::End(0)) {
        length as usize
    } else {
        return chd_error::ReadError;
    };

    let mut buffer = Vec::new();
    if let Err(_) = buffer.try_reserve_exact(length as usize) {
        return chd_error::OutOfMemory;
    }
    let mut done: usize = 0;
    let mut last_update_done: usize = 0;
    let update_interval: usize = (length + 99) / 100;

    if let Err(_) = file.seek(SeekFrom::Start(0)) {
        return chd_error::ReadError;
    }

    while done < length {
        let req_count = std::cmp::max(length - done, PRECACHE_CHUNK_SIZE);

        // todo: this is kind of sus...
        if let Err(_) = file.read_exact(&mut buffer[done..req_count]) {
            return chd_error::ReadError;
        }

        done += req_count;
        if let Some(progress) = progress {
            if (done - last_update_done) >= update_interval && done != length {
                last_update_done = done;
                unsafe {
                    progress(done, length, param);
                }
            }
        }
    }

    // replace underlying stream of chd_file
    let stream = Box::new(Cursor::new(buffer)) as Box<dyn SeekRead>;

    // take ownership of the existing chd file
    let chd_file = ffi_takeown_chd(chd);
    let (_file, parent) = chd_file.into_inner();

    let buffered_chd = match Chd::open(stream, parent) {
        Err(e) => return e,
        Ok(chd) => Box::new(chd),
    };

    let buffered_chd = ffi_expose_chd(buffered_chd);
    unsafe { chd.swap(buffered_chd) };

    chd_error::None
}

#[no_mangle]
#[cfg(feature = "chd_precache")]
#[cfg_attr(docsrs, doc(cfg(chd_precache)))]
/// Precache the underlying file into memory.
///
/// The underlying stream of the input `chd_file` is swapped with a layout-undefined in-memory stream.
///
/// If the provenance of the original `chd_file` is from [`chd_open`](crate::chd_open), then the underlying
/// stream is safely dropped.
///
/// If instead the underlying stream is a `core_file` opened from [`chd_open_file`](crate::chd_open_file),
/// or [`chd_open_core_file`](crate::chd_open_core_file), then the same semantics of calling [`chd_core_file`](crate::chd_core_file)
/// applies, and ownership of the underlying stream is released to the caller.
///
/// After precaching, the input `chd_file` no longer returns a valid inner stream when passed to [`chd_core_file`](crate::chd_core_file),
/// and should be treated as having the same provenance as being from [`chd_open`](crate::chd_open).
///
/// # Safety
/// * `chd` is either `NULL` or a valid pointer to a `chd_file` obtained from [`chd_open`](crate::chd_open), [`chd_open_file`](crate::chd_open_file), or [`chd_open_core_file`](crate::chd_open_core_file).
pub unsafe extern "C" fn chd_precache(chd: *mut chd_file) -> chd_error {
    chd_precache_progress(chd, None, std::ptr::null_mut())
}
