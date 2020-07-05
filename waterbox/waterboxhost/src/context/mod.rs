use lazy_static::lazy_static;
use crate::*;
use memory_block::{Protection, pal};
use host::{ActivatedWaterboxHost};
use syscall_defs::SyscallNumber;
use std::ptr::null_mut;

pub mod thunks;

// manually match these up with interop.s
const ORG: usize = 0x35f00000000;

const CALL_GUEST_IMPL_ADDR: usize = ORG;
const CALL_GUEST_SIMPLE_ADDR: usize = ORG + 64;

pub const CALLBACK_SLOTS: usize = 64;
/// Retrieves a function pointer suitable for sending to the guest that will cause
/// the host to callback to `slot` when called
pub fn get_callback_ptr(slot: usize) -> usize{
	assert!(slot < CALLBACK_SLOTS);
	ORG + 0x100 + slot * 16
}

fn init_interop_area() -> AddressRange {
	unsafe {
		let bytes = include_bytes!("interop.bin");
		let addr = pal::map_anon(
			AddressRange { start: ORG, size: bytes.len() }.align_expand(),
			Protection::RW).unwrap();
		addr.slice_mut()[0..bytes.len()].copy_from_slice(bytes);
		pal::protect(addr, Protection::RX).unwrap();
		addr
	}
}

lazy_static! {
	static ref INTEROP_AREA: AddressRange = init_interop_area();
}

// https://github.com/rust-lang/rust/issues/53605
#[repr(C)]
union FuncCast<T: Copy> {
	pub p: usize,
	pub f: T,
}

/// Enter waterbox code with a function that takes 0 arguments
/// Returns the function's return value
const CALL_GUEST_SIMPLE: FuncCast<extern "sysv64" fn(entry_point: usize, context: &mut Context) -> usize> = FuncCast { p: CALL_GUEST_SIMPLE_ADDR };
/// Enter waterbox code with a function that takes 0 arguments
/// Returns the function's return value
pub fn call_guest_simple(entry_point: usize, context: &mut Context) -> usize{
	unsafe { (CALL_GUEST_SIMPLE.f)(entry_point, context) }
}


pub type ExternalCallback = extern "sysv64" fn(a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize) -> usize;

/// Layout must be synced with interop.s
#[repr(C)]
pub struct Context {
	pub host_rsp: usize,
	pub guest_rsp: usize,
	pub dispatch_syscall: extern "sysv64" fn(a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize, nr: SyscallNumber) -> SyscallReturn,
	pub host_ptr: usize,
	pub extcall_slots: [Option<ExternalCallback>; 64],
}

/// Prepares this host thread to be allowed to call guest code.  No op if already called.  Does nothing on Windows.
/// Only needs to happen once per host thread
pub fn prepare_thread() {
	#[cfg(unix)]
	{
		use libc::*;
		let mut gs = 0usize;
		arch_prctl(ARCH_GET_GS, &gs);
		if gs == 0 {
			gs = Box::into_raw(Box::new([0usize; 4]));
			arch_prctl(ARCH_SET_GS, gs);
		}
	}
}

/// Get the currently loaded ActivatedWaterboxHost
/// If called outside call_guest, will fail in various wonderful ways?
/// Unsafe:  The lifetime is really only good in the current function
pub unsafe fn access_context() -> &'static mut ActivatedWaterboxHost<'static> {
	let mut p: *mut ActivatedWaterboxHost;
	asm!("mov {}, [gs:0x18]", out(reg) p);
	if p == null_mut() {
		std::intrinsics::breakpoint();
	}
	return &mut *p;
}
