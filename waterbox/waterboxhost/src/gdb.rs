// https://sourceware.org/gdb/current/onlinedocs/gdb/Declarations.html#Declarations
#![allow(non_camel_case_types)]
#![allow(non_upper_case_globals)]

use std::{sync::Mutex, ptr::null_mut};
use lazy_static::lazy_static;

const JIT_NOACTION: u32 = 0;
const JIT_REGISTER_FN: u32 = 1;
const JIT_UNREGISTER_FN: u32 = 2;

#[repr(C)]
struct jit_code_entry {
	next_entry: *mut jit_code_entry,
	prev_entry: *mut jit_code_entry,
	symfile_addr: *const u8,
	symfile_size: u64
}
unsafe impl Send for jit_code_entry {}
unsafe impl Sync for jit_code_entry {}

#[repr(C)]
struct jit_descriptor {
	version: u32,
	action_flag: u32,
	relevant_entry: *mut jit_code_entry,
	first_entry: *mut jit_code_entry,
}
unsafe impl Send for jit_descriptor {}
unsafe impl Sync for jit_descriptor {}

#[no_mangle]
#[inline(never)]
extern fn __jit_debug_register_code() {}

#[no_mangle]
static mut __jit_debug_descriptor: jit_descriptor = jit_descriptor {
	version: 1,
	action_flag: JIT_NOACTION,
	relevant_entry: null_mut(), // 0 as *mut jit_code_entry,
	first_entry: null_mut(), // 0 as *mut jit_code_entry
};


lazy_static! {
	static ref LOCK: Mutex<()> = Mutex::new(());
}

/// unsafe:  the data should be valid until a matching unregister call
pub unsafe fn register(data: &[u8]) {
	let _guard = LOCK.lock().unwrap();
	let entry = Box::into_raw(Box::new(jit_code_entry {
		next_entry: null_mut(),
		prev_entry: null_mut(),
		symfile_addr: &data[0],
		symfile_size: data.len() as u64
	}));
	if __jit_debug_descriptor.first_entry == null_mut() {
		__jit_debug_descriptor.first_entry = entry;
	} else {
		let mut tail = __jit_debug_descriptor.first_entry;
		while (*tail).next_entry != null_mut() {
			tail = (*tail).next_entry;
		}
		(*tail).next_entry = entry;
		(*entry).prev_entry = tail;
	}
	__jit_debug_descriptor.relevant_entry = entry;
	__jit_debug_descriptor.action_flag = JIT_REGISTER_FN;
	__jit_debug_register_code();
}

/// unsafe: undefined if not exactly matching a register call
pub unsafe fn deregister(data: &[u8]) {
	let _guard = LOCK.lock().unwrap();

	let mut entry = __jit_debug_descriptor.first_entry;
	while (*entry).symfile_addr != &data[0] {
		entry = (*entry).next_entry;
	}
	if (*entry).next_entry != null_mut() {
		(*(*entry).next_entry).prev_entry = (*entry).prev_entry;
	}
	if (*entry).prev_entry != null_mut() {
		(*(*entry).prev_entry).next_entry = (*entry).next_entry;
	} else {
		__jit_debug_descriptor.first_entry = (*entry).next_entry;
	}
	__jit_debug_descriptor.relevant_entry = entry;
	__jit_debug_descriptor.action_flag = JIT_UNREGISTER_FN;
	__jit_debug_register_code();

	drop(Box::from_raw(entry));
}
