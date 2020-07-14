use lazy_static::lazy_static;
use crate::*;
use memory_block::{Protection, pal};
use host::{Environment};
use syscall_defs::SyscallNumber;
use std::sync::atomic::AtomicUsize;

pub mod thunks;

// manually match these up with interop.s
const ORG: usize = 0x35f00000000;

const CALL_GUEST_IMPL_ADDR: usize = ORG;
const CALL_GUEST_SIMPLE_ADDR: usize = ORG + 0x40;
const ENTER_GUEST_THREAD_ADDR: usize = ORG + 0x120;

pub const CALLBACK_SLOTS: usize = 64;
/// Retrieves a function pointer suitable for sending to the guest that will cause
/// the host to callback to `slot` when called.  Slot must be less than CALLBACK_SLOTS
pub fn get_callback_ptr(slot: usize) -> usize{
	assert!(slot < CALLBACK_SLOTS);
	ORG + 0x200 + slot * 16
}

fn init_interop_area() -> AddressRange {
	unsafe {
		let bytes = include_bytes!("interop.bin");
		let addr = pal::map_anon(
			AddressRange { start: ORG, size: bytes.len() }.align_expand(),
			Protection::RW
		).unwrap();
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

const CALL_GUEST_SIMPLE: FuncCast<extern "sysv64" fn(entry_point: usize, context: &mut Context) -> usize> = FuncCast { p: CALL_GUEST_SIMPLE_ADDR };
/// Enter waterbox code for the main thread with a function that takes 0 arguments
/// Returns the function's return value
pub fn call_guest_simple(entry_point: usize, context: &mut Context) -> usize {
	unsafe { (CALL_GUEST_SIMPLE.f)(entry_point, context) }
}
const ENTER_GUEST_THREAD: FuncCast<extern "sysv64" fn(context: &mut Context)> = FuncCast { p: ENTER_GUEST_THREAD_ADDR };
/// Enters waterbox code for a guest thread.
/// Assumes the guest is returning from a syscall; *context.guest_rsp should be an appropriate return address,
/// and context.rax should contain the syscall return value.
/// Returns when the guest next wants to make a syscall, with context.rax, context.{rdi..r9} appropriately filled with arguments
pub fn enter_guest_thread(context: &mut Context) {
	unsafe { (ENTER_GUEST_THREAD.f)(context) }
}

/// Allowed type for callback functions that Waterbox cores can make back into the real world.
pub type ExternalCallback = extern "sysv64" fn(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize) -> usize;
/// Allowed type of the syscall service function
pub type SyscallCallback = extern "sysv64" fn(
	a1: usize, a2: usize, a3: usize, a4: usize, a5: usize, a6: usize, nr: SyscallNumber, h: &Environment) -> SyscallReturn;

/// Structure used to track information for calls into waterbox code
/// Layout must be synced with interop.s
#[repr(C)]
pub struct Context {
	/// thread id.  1 is main thread and has different call procedures
	pub tid: u32,
	/// thread pointer as set by guest libc (pthread_self, etc)
	pub thread_area: usize,
	/// used by set_tid_address
	pub clear_child_tid: usize,
	/// a lock that this thread is waiting on
	pub park_addr: AtomicUsize,
	/// Data structure shared between all threads that describes how to call out in this guest
	pub context_call_info: *const ContextCallInfo,
	/// Used internally to track the host's most recent rsp when transitioned to Waterbox code.
	pub host_rsp: usize,
	/// Sets the guest's starting rsp, and used internally to track the guest's most recent rsp when transitioned to extcall or syscall
	pub guest_rsp: usize,

	// things only relevant to guest threads 
	// saved guest call data
	pub rax: usize,
	pub rdi: usize,
	pub rsi: usize,
	pub rdx: usize,
	pub rcx: usize,
	pub r8: usize,
	pub r9: usize,
	// saved guest nonvolatiles (besides rsp, which is above)
	pub rbx: usize,
	pub rbp: usize,
	pub r12: usize,
	pub r13: usize,
	pub r14: usize,
	pub r15: usize,	
}
unsafe impl Sync for Context {}
unsafe impl Send for Context {}

#[repr(C)]
pub struct ContextCallInfo {
	/// syscall service function (but only on the main thread, should we rework this??)
	pub dispatch_syscall: SyscallCallback,
	/// This will be passed as the final parameter to dispatch_syscall (but only on the main thread!), and is not otherwise used
	/// by the context code.  TODO:  Revist how main thread handoff works?
	pub host_ptr: *const Environment,
	/// Host function pointers that will be called when the guest calls an extcall slot thunk (returned from `get_callback_ptr`)
	pub extcall_slots: [Option<ExternalCallback>; 64],
}

impl Context {
	/// Returns a suitably initialized context
	pub fn new(tid: u32, context_call_info: *const ContextCallInfo, initial_guest_rsp: usize) -> Context {
		let mut res: Context = unsafe { std::mem::zeroed() };
		res.tid = tid;
		res.context_call_info = context_call_info;
		res.guest_rsp = initial_guest_rsp;
		res
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

/// Retrieve the currently active context.
/// Unsafe:  Pointer only valid when this thread is inside call_guest_simple, enter_guest_thread, or in
/// a call initiated by get_thunk_for_proc.  (So, generally only during syscall callbacks)
pub unsafe fn current_context() -> *mut Context {
	let mut ret: *mut Context;
	asm!("mov {} gs:0x18", out(reg) ret);
	ret
}
