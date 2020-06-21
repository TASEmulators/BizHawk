
use std::ptr::null_mut;
use super::MemoryBlock;
use std::sync::Mutex;
use crate::*;
use super::*;
use lazy_static::lazy_static;

lazy_static! {
	static ref global_data: Mutex<GlobalData> = Mutex::new(GlobalData {
		initialized: false,
		active_blocks: Vec::new(),
	});
}

struct GlobalData {
	initialized: bool,
	active_blocks: Vec<MemoryBlockRef>, 
}

unsafe fn register(block: *mut MemoryBlock) {
	let mut data = global_data.lock().unwrap();
	if !data.initialized {
		initialize();
		data.initialized = true;
	}
	data.active_blocks.push(MemoryBlockRef(block));
}

unsafe fn unregister(block: *mut MemoryBlock) {
	let mut data = global_data.lock().unwrap();
	let pos = data.active_blocks.iter().position(|x| x.0 == block).unwrap();
	data.active_blocks.remove(pos);
}

enum TripResult {
	Handled,
	NotHandled,
}

unsafe fn trip(addr: usize) -> TripResult {
	let data = global_data.lock().unwrap();
	let memory_block = match data.active_blocks
		.iter()
		.find(|x| addr >= (*x.0).start && addr < (*x.0).end) {
			Some(x) => &mut *x.0,
			None => return TripResult::NotHandled,
		};
	let page_start_addr = addr & !PAGEMASK;
	let page = &mut memory_block.pages[(addr - memory_block.start) >> PAGESHIFT];
	if !page.flags.contains(PageFlags::W) {
		return TripResult::NotHandled
	}
	if memory_block.sealed && match page.snapshot { Snapshot::None => true, _ => false } {
		// take snapshot now
		let mut snapshot = PageBlock::new();
		let src = std::slice::from_raw_parts(page_start_addr as *const u8, PAGESIZE);
		let dst = snapshot.slice_mut();
		dst.copy_from_slice(src);
		page.snapshot = Snapshot::Data(snapshot);
	}
	page.flags.insert(PageFlags::DIRTY);
	let new_prot = if page.flags.contains(PageFlags::X) { Protection::RWX } else { Protection::RW };
	assert!(pal::protect(page_start_addr, PAGESIZE, new_prot));
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
		if p_record.ExceptionCode != STATUS_ACCESS_VIOLATION // only trigger on access violations...
			|| (flags & 1) != 0 { // ...due to a write attempts
			return EXCEPTION_CONTINUE_SEARCH
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
type sa_handler = unsafe extern fn(i32) -> ();
#[cfg(unix)]
type sa_sigaction = unsafe extern fn(i32, *const siginfo_t, *const ucontext_t) -> ();
#[cfg(unix)]
use libc::*;
#[cfg(unix)]
static mut altstack: [u8; SIGSTKSZ] = [0; SIGSTKSZ];
#[cfg(unix)]
static mut sa_old: Option<Box<sigaction>> = None;
#[cfg(unix)]
fn initialize() {
	use std::mem::{transmute, zeroed};

	unsafe extern fn handler(sig: i32, info: *const siginfo_t, ucontext: *const ucontext_t) {
		let faultAddress = (*info).si_addr() as usize;
		let write = (*ucontext).uc_mcontext.gregs[REG_ERR as usize] & 2 != 0;
		let rethrow = !write || match trip(faultAddress) {
			TripResult::NotHandled => true,
			_ => false
		};
		if rethrow {
			if sa_old.as_ref().unwrap().sa_flags & SA_SIGINFO != 0 {
				transmute::<usize, sa_sigaction>(sa_old.as_ref().unwrap().sa_sigaction)(sig, info, ucontext);
			} else {
				transmute::<usize, sa_handler>(sa_old.as_ref().unwrap().sa_sigaction)(sig);
			}
			abort();
		}
	}
	unsafe {
		sa_old = Some(Box::new(zeroed::<sigaction>()));
		let ss = stack_t {
			ss_flags: 0,
			ss_sp: &mut altstack[0] as *mut u8 as *mut c_void,
			ss_size: SIGSTKSZ
		};
		assert!(sigaltstack(&ss, null_mut()) == 0, "sigaltstack failed");
		let mut sa = sigaction {
			sa_mask: zeroed::<sigset_t>(),
			sa_sigaction: transmute::<sa_sigaction, usize>(handler),
			sa_flags: SA_ONSTACK | SA_SIGINFO,
			sa_restorer: None,
		};
		sigfillset(&mut sa.sa_mask);
		assert!(sigaction(SIGSEGV, &sa, &mut **sa_old.as_mut().unwrap() as *mut sigaction) == 0, "sigaction failed");
	}
}
