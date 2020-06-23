
use std::ptr::null_mut;
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
		initialize();
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
		return TripResult::NotHandled
	}
	page.maybe_snapshot(page_start_addr);
	page.dirty = true;
	assert!(pal::protect(AddressRange { start: page_start_addr, size: PAGESIZE }, page.native_prot()));
	TripResult::Handled
}

#[cfg(windows)]
fn initialize() {
	use winapi::um::errhandlingapi::*;
	use winapi::um::winnt::*;
	use winapi::vc::excpt::*;

	unsafe extern "system" fn handler(p_info: *mut EXCEPTION_POINTERS) -> i32 {
		let p_record = &mut *(*p_info).ExceptionRecord;
		let flags = p_record.ExceptionInformation[0];
		match p_record.ExceptionCode {
			STATUS_ACCESS_VIOLATION if (flags & 1) != 0 => (), // write exception
			STATUS_GUARD_PAGE_VIOLATION => (), // guard exception
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
		assert!(res != null_mut(), "AddVectoredExceptionHandler failed");
	}
}

#[cfg(unix)]
type SaHandler = unsafe extern fn(i32) -> ();
#[cfg(unix)]
type SaSigaction = unsafe extern fn(i32, *const siginfo_t, *const ucontext_t) -> ();
#[cfg(unix)]
use libc::*;
#[cfg(unix)]
static mut ALTSTACK: [u8; SIGSTKSZ] = [0; SIGSTKSZ];
#[cfg(unix)]
static mut SA_OLD: Option<Box<sigaction>> = None;
#[cfg(unix)]
fn initialize() {
	use std::mem::{transmute, zeroed};

	unsafe extern fn handler(sig: i32, info: *const siginfo_t, ucontext: *const ucontext_t) {
		let fault_address = (*info).si_addr() as usize;
		let write = (*ucontext).uc_mcontext.gregs[REG_ERR as usize] & 2 != 0;
		let rethrow = !write || match trip(fault_address) {
			TripResult::NotHandled => true,
			_ => false
		};
		if rethrow {
			if SA_OLD.as_ref().unwrap().sa_flags & SA_SIGINFO != 0 {
				transmute::<usize, SaSigaction>(SA_OLD.as_ref().unwrap().sa_sigaction)(sig, info, ucontext);
			} else {
				transmute::<usize, SaHandler>(SA_OLD.as_ref().unwrap().sa_sigaction)(sig);
			}
			abort();
		}
	}
	unsafe {
		SA_OLD = Some(Box::new(zeroed::<sigaction>()));
		let ss = stack_t {
			ss_flags: 0,
			ss_sp: &mut ALTSTACK[0] as *mut u8 as *mut c_void,
			ss_size: SIGSTKSZ
		};
		assert!(sigaltstack(&ss, null_mut()) == 0, "sigaltstack failed");
		let mut sa = sigaction {
			sa_mask: zeroed::<sigset_t>(),
			sa_sigaction: transmute::<SaSigaction, usize>(handler),
			sa_flags: SA_ONSTACK | SA_SIGINFO,
			sa_restorer: None,
		};
		sigfillset(&mut sa.sa_mask);
		assert!(sigaction(SIGSEGV, &sa, &mut **SA_OLD.as_mut().unwrap() as *mut sigaction) == 0, "sigaction failed");
	}
}
