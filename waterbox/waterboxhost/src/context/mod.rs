use lazy_static::lazy_static;
use crate::*;
use memory_block::{Protection, pal};
use host::{WaterboxHost};
use syscall_defs::SyscallNumber;

pub mod thunks;

// manually match these up with interop.s
const ORG: usize = 0x35f00000000;

const CALL_GUEST_IMPL_ADDR: usize = ORG;
const CALL_GUEST_SIMPLE_ADDR: usize = ORG + 64;

pub const CALLBACK_SLOTS: usize = 64;
/// Retrieves a function pointer suitable for sending to the guest that will cause
/// the host to callback to `slot` when called.  Slot must be less than CALLBACK_SLOTS
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

/// Allowed type for callback functions that Waterbox cores can make back into the real world.
pub type ExternalCallback = extern "sysv64" fn(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize) -> usize;
/// Allowed type of the syscall service function
pub type SyscallCallback = extern "sysv64" fn(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize, nr: SyscallNumber, h: &mut WaterboxHost) -> SyscallReturn;

/// Structure used to track information for calls into waterbox code
/// Layout must be synced with interop.s
#[repr(C)]
pub struct Context {
	/// Used internally to track the host's most recent rsp when transitioned to Waterbox code.
	pub host_rsp: usize,
	/// Sets the guest's starting rsp, and used internally to track the guest's most recent rsp when transitioned to extcall or syscall
	pub guest_rsp: usize,
	/// syscall service function
	pub dispatch_syscall: SyscallCallback,
	/// This will be passed as the final parameter to dispatch_syscall, and is not otherwise used by the context tracking code
	pub host_ptr: usize,
	/// Host function pointers that will be called when the guest calls an extcall slot thunk (returned from `get_callback_ptr`)
	pub extcall_slots: [Option<ExternalCallback>; 64],
}
impl Context {
	/// Returns a suitably initialized context.  It's almost ready to use, but host_ptr must be set before each usage
	pub fn new(initial_guest_rsp: usize, dispatch_syscall: SyscallCallback) -> Context {
		Context {
			host_rsp: 0,
			guest_rsp: initial_guest_rsp,
			dispatch_syscall,
			host_ptr: 0,
			extcall_slots: [None; 64]
		}
	}
}

#[cfg(unix)]
thread_local!(static TIB: Box<[usize; 4]> = Box::new([0usize; 4]));

/// Prepares this host thread to be allowed to call guest code.  Noop if already called.
/// Only needs to happen once per host thread
pub fn prepare_thread() {
	// not per-thread setup, but setup that needs to happen anyway
	// todo: lazy_static isn't really the right idea here since we discard the value
	assert_eq!(INTEROP_AREA.start, ORG);

	// We stomp over [gs:0x18] and use it for our own mini-TLS to track the stack marshalling
	// On windows, that's a (normally unused and free for the plundering?) field in TIB
	// On linux, that register is not normally in use, so we put some bytes there and then use it
	#[cfg(unix)]
	unsafe {
		use libc::*;
		let mut gs = 0usize;
		assert_eq!(syscall(SYS_arch_prctl, 0x1004 /*ARCH_GET_GS*/, &gs), 0);
		if gs == 0 {
			TIB.with(|b| {
				gs = b.as_ref() as *const usize as usize;
				assert_eq!(syscall(SYS_arch_prctl, 0x1001 /*ARCH_SET_GS*/, gs), 0);
			});
		}
	}
}
