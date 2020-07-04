use lazy_static::lazy_static;
use crate::*;
use memory_block::{Protection, pal};
use host::{ActivatedWaterboxHost};

mod call_trampolines;

// manually match these up with interop.s
const ORG: usize = 0x35f00000000;
const DEPART_ADDR: usize = ORG + 0x700;
const ANYRET_ADDR: usize = ORG + 0x900;
pub const CALLBACK_SLOTS: usize = 64;
/// Retrieves a function pointer suitable for sending to the guest that will cause
/// the host to callback to `slot` when called
pub fn get_callback_ptr(slot: usize, arg_count: usize) -> usize{
	assert!(slot < CALLBACK_SLOTS);
	assert!(arg_count <= 6);
	ORG + 0x1000 + slot * 128 + arg_count * 16
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

/// Enter waterbox code
/// context should be set in TLS already
/// context.call.(a1..a6) should contain any args to pass to the guest
/// context.guest.rsp should be set to guest_stack_limit - 0x10
/// guest entry point, a plain sysv64 c called function, should be in context.guest.rsp[0..7]
///
/// On return, examine context.call.nr:
///   -1 => guest has returned.  guest's actual return value was returned by depart.
///     Tear down the context and return to host.
///   0x800000000000 + n => guest has requested external registered call slot 'n' to be called.  args are in a1..a6
///     Perform the external call, then call anyret() and continue.
///   n => guest has requested syscall.  args are in a1..a6
///     Perform the external call, then call anyret() and continue.
const DEPART: FuncCast<extern "sysv64" fn() -> usize> = FuncCast { p: DEPART_ADDR };

/// Reenter waterbox code.  Call after depart() or previous anyret() call left a syscall/extcall in context.call
/// Return values are exactly the same as from depart()
const ANYRET: FuncCast<fn(r: usize) -> usize> = FuncCast { p: ANYRET_ADDR };

pub fn is_external_call(nr: usize) -> Option<usize> {
	if nr >= 0x8000000000000000 {
		Some(nr - 0x8000000000000000)
	} else {
		None
	}
}

/// Layout must be synced with interop.s
#[repr(C)]
pub struct JmpBuf {
	/// Saved right after the `call` instr
	pub rsp: usize,
	pub rbp: usize,
	pub rbx: usize,
	pub r12: usize,
	pub r13: usize,
	pub r14: usize,
	pub r15: usize,
}

/// Layout must be synced with interop.s
#[repr(C)]
#[derive(Copy, Clone)]
pub struct GuestCall {
	/// rax.  not used when calling run_guest_call
	pub nr: usize,
	/// rdi
	pub a1: usize,
	/// rsi
	pub a2: usize,
	/// rdx
	pub a3: usize,
	/// rcx
	pub a4: usize,
	/// r8
	pub a5: usize,
	/// r9
	pub a6: usize,
}

/// Layout must be synced with interop.s
#[repr(C)]
pub struct Context {
	pub host: JmpBuf,
	pub guest: JmpBuf,
	pub call: GuestCall
}
impl Context {
	pub fn new() -> Context {
		unsafe { std::mem::zeroed() }
	}
}

pub unsafe fn run_guest_thread<F: FnMut(&mut ActivatedWaterboxHost, &GuestCall) -> usize>(
	host: &mut ActivatedWaterboxHost, call: &GuestCall, guest_rip: usize, guest_rsp_limit: usize, 
	mut service_callback: F
) -> usize {
	let mut context = Context::new();

	#[cfg(unix)]
	let mut TIB = [0usize; 4];
	#[cfg(unix)]
	{
		use libc::*;
		TIB[3] = &context as *const Context as usize;
		arch_prctl(ARCH_SET_GS, &mut TIB[0] as *mut usize);
	}
	#[cfg(windows)]
	{
		let p = &context as *const Context as usize;
		asm!("mov [gs:0x18], {}", in(reg) p);
	}

	context.call = *call;
	context.guest.rsp = guest_rsp_limit - 0x10;
	*(context.guest.rsp as *mut usize) = guest_rip;

	let mut ret = (DEPART.f)();
	while (&context.call.nr as *const usize).read_volatile() != 0xffffffffffffffff {
		let val = service_callback(host, &context.call);
		ret = (ANYRET.f)(val);
	}

	#[cfg(unix)]
	{
		use libc::*;
		arch_prctl(ARCH_SET_GS, 0);
	}
	#[cfg(windows)]
	{
		asm!("mov [gs:0x18], 0");
	}

	ret
}


impl Context {
	// pub fn new() -> Context {

	// }
}