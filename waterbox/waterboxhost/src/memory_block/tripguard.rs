
use super::MemoryBlock;
use std::sync::Mutex;
use crate::*;
use super::*;
use lazy_static::lazy_static;

lazy_static! {
	static ref GLOBAL_DATA: Mutex<GlobalData> = Mutex::new(GlobalData {
		initialized: false,
		active_blocks: Vec::new(),
	});
}

struct GlobalData {
	initialized: bool,
	active_blocks: Vec<MemoryBlockRef>, 
}

pub unsafe fn register(block: *mut MemoryBlock) {
	let mut data = GLOBAL_DATA.lock().unwrap();
	if !data.initialized {
		trip_pal::initialize();
		data.initialized = true;
	}
	data.active_blocks.push(MemoryBlockRef(block));
}

pub unsafe fn unregister(block: *mut MemoryBlock) {
	let mut data = GLOBAL_DATA.lock().unwrap();
	let pos = data.active_blocks.iter().position(|x| x.0 == block);
	match pos {
		Some(index) => {
			data.active_blocks.remove(index);
		},
		None => {
			panic!("Tried to unregister MemoryBlock which was not registered")
		}
	}
}

enum TripResult {
	Handled,
	NotHandled,
}

unsafe fn trip(addr: usize) -> TripResult {
	let data = GLOBAL_DATA.lock().unwrap();
	let memory_block = match data.active_blocks
		.iter()
		.find(|x| (*x.0).addr.contains(addr)) {
			Some(x) => &mut *x.0,
			None => return TripResult::NotHandled,
		};
	let page_start_addr = addr & !PAGEMASK;
	let page = &mut memory_block.pages[(addr - memory_block.addr.start) >> PAGESHIFT];
	if !page.status.writable() {
		std::intrinsics::breakpoint();
		return TripResult::NotHandled
	}
	page.maybe_snapshot(page_start_addr);
	page.dirty = true;
	match pal::protect(AddressRange { start: page_start_addr, size: PAGESIZE }, page.native_prot()) {
		Ok(()) => TripResult::Handled,
		_ => {
			std::intrinsics::breakpoint();
			std::process::abort();
		}
	}
}

#[cfg(windows)]
mod trip_pal {
	use super::*;
	use winapi::um::errhandlingapi::*;
	use winapi::um::winnt::*;
	use winapi::vc::excpt::*;

	pub fn initialize() {
		unsafe extern "system" fn handler(p_info: *mut EXCEPTION_POINTERS) -> i32 {
			let p_record = &*(*p_info).ExceptionRecord;
			let flags = p_record.ExceptionInformation[0];
			match p_record.ExceptionCode {
				STATUS_ACCESS_VIOLATION if (flags & 1) != 0 => (), // write exception
				STATUS_GUARD_PAGE_VIOLATION => {
					// guard exception

					// If this is ours, it's from a cothread stack.  If we're on a cothread
					// stack right now, we need to return without taking our lock because
					// the stack used in handler() and everything it calls might move onto
					// another page and trip again which would deadlock us.  (Alternatively,
					// this might be the second trip in a row while already servicing another one.)
					// This does not cause determinism issues because of get_stack_dirty() and the
					// windows specific code in set_protections().

					// The only problem here is that we might be swallowing a completely unrelated guard
					// exception by returning before checking whether it was in a waterbox block;
					// in which case we'll find out what we broke eventually.
					return EXCEPTION_CONTINUE_EXECUTION 
				},
				_ => return EXCEPTION_CONTINUE_SEARCH
			}
			let fault_address = p_record.ExceptionInformation[1] as usize;
			match trip(fault_address) {
				TripResult::Handled => EXCEPTION_CONTINUE_EXECUTION,
				TripResult::NotHandled => EXCEPTION_CONTINUE_SEARCH,
			}
		}
		unsafe {
			let res = AddVectoredExceptionHandler(1 /* CALL_FIRST */, Some(handler));
			assert!(!res.is_null(), "AddVectoredExceptionHandler failed");
		}
	}
}

#[cfg(unix)]
mod trip_pal {
	use libc::*;
	use super::*;

	type SaHandler = unsafe extern fn(i32) -> ();
	type SaSigaction = unsafe extern fn(i32, *const siginfo_t, *const ucontext_t) -> ();
	static mut SA_OLD: Option<Box<sigaction>> = None;

	pub fn initialize() {
		use std::mem::{transmute, zeroed};

		unsafe extern fn handler(sig: i32, info: *const siginfo_t, ucontext: *const ucontext_t) {
			let fault_address = (*info).si_addr() as usize;
			let write = (*ucontext).uc_mcontext.gregs[REG_ERR as usize] & 2 != 0;
			let rethrow = !write || match trip(fault_address) {
				TripResult::NotHandled => true,
				_ => false
			};
			if rethrow {
				let sa_old = SA_OLD.as_ref().unwrap();
				if sa_old.sa_flags & SA_SIGINFO != 0 {
					transmute::<usize, SaSigaction>(sa_old.sa_sigaction)(sig, info, ucontext);
				} else {
					transmute::<usize, SaHandler>(sa_old.sa_sigaction)(sig);
				}
				abort();
			}
		}
		unsafe {
			// TODO: sigaltstack is per thread, so this won't work
			// At the same time, one seems to be set up automatically on each thread, so this isn't needed.
			// let ss = stack_t {
			// 	ss_flags: 0,
			// 	ss_sp: Box::into_raw(Box::new(zeroed::<[u8; SIGSTKSZ]>())) as *mut c_void,
			// 	ss_size: SIGSTKSZ
			// };
			// let mut ss_old = stack_t {
			// 	ss_flags: 0,
			// 	ss_sp: 0 as *mut c_void,
			// 	ss_size: 0
			// };
			// assert!(sigaltstack(&ss, &mut ss_old) == 0, "sigaltstack failed");
			SA_OLD = Some(Box::new(zeroed()));
			let mut sa = sigaction {
				sa_mask: zeroed(),
				sa_sigaction: transmute::<SaSigaction, usize>(handler),
				sa_flags: SA_ONSTACK | SA_SIGINFO,
				sa_restorer: None,
			};
			sigfillset(&mut sa.sa_mask);
			assert!(sigaction(SIGSEGV, &sa, &mut **SA_OLD.as_mut().unwrap() as *mut sigaction) == 0, "sigaction failed");
		}
	}
}
