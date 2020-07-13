#![cfg(test)]

use std::mem::transmute;
use super::*;

type TestResult = anyhow::Result<()>;

/// new / drop, activate / deactivate
#[test]
fn test_create() {
	// these tests don't test much anymore...
	drop(MemoryBlock::new(AddressRange { start: 0x36300000000, size: 0x50000 }));
	drop(MemoryBlock::new(AddressRange { start: 0x36b00000000, size: 0x2000 }));
	{
		let mut b = MemoryBlock::new(AddressRange { start: 0x36100000000, size: 0x65000 });
		b.activate();
		b.deactivate();
		b.activate();
		b.deactivate();
	}
	{
		let mut b = MemoryBlock::new(AddressRange { start: 0x36e00000000, size: 0x5000 });
		b.activate();
		b.deactivate();
		b.activate();
	}
}

/// simple test of dirt detection
#[test]
fn test_dirty() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36f00000000, size: 0x10000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		b.mmap_fixed(addr, Protection::RW, true)?;
		let ptr = b.addr.slice_mut();
		ptr[0x2003] = 5;
		assert!(b.pages[2].dirty);
		Ok(())
	}
}

/// dirt detection away from the start of a block
#[test]
fn test_offset() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36f00000000, size: 0x20000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		b.mmap_fixed(AddressRange { start: 0x36f00003000, size: 0x1000 }, Protection::RW, true)?;
		let ptr = b.addr.slice_mut();
		ptr[0x3663] = 12;
		assert!(b.pages[3].dirty);
		Ok(())
	}
}

/// dirt detection in RWStack area when $rsp does not point there, and it was just a conventional write
#[test]
fn test_stk_norm() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36200000000, size: 0x10000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		b.mmap_fixed(addr, Protection::RWStack, true)?;
		let ptr = b.addr.slice_mut();
		ptr[0xeeee] = 0xee;
		ptr[0x44] = 0x44;

		b.get_stack_dirty();

		assert!(b.pages[0].dirty);
		assert!(b.pages[14].dirty);
		assert_eq!(ptr[0x8000], 0);

		b.get_stack_dirty();

		// This is an unfair test, but it's just documenting the current limitations of the system.
		// Ideally, page 8 would be clean because we read from it but did not write to it.
		// Due to limitations of RWStack tracking on windows, it is dirty.
		#[cfg(windows)]
		assert!(b.pages[8].dirty);
		#[cfg(unix)]
		assert!(!b.pages[8].dirty);

		Ok(())
	}
}

/// dirt detection in RWStack area when $rsp points there
#[test]
fn test_stack() -> TestResult {
	use std::convert::TryInto;
	unsafe {
		let addr = AddressRange { start: 0x36f00000000, size: 0x10000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		b.mmap_fixed(addr, Protection::RW, true)?;
		let ptr = b.addr.slice_mut();
		let mut i = 0;

		ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xe0 ; i += 1; // mov rax,rsp
		ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xfc ; i += 1; // mov rsp,rdi
		ptr[i] = 0x50 ; i += 1; // push rax
		ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xc4 ; i += 1; // mov rsp,rax
		ptr[i] = 0xb0 ; i += 1; ptr[i] = 0x2a ; i += 1; // mov al,0x2a
		ptr[i] = 0xc3 ; // ret 

		b.mprotect(AddressRange { start: 0x36f00000000, size: 0x1000 }, Protection::RX)?;
		b.mprotect(AddressRange { start: 0x36f00008000, size: 0x8000 }, Protection::RWStack)?;
		let tmp_rsp = addr.end();
		let res = transmute::<usize, extern "sysv64" fn(rsp: usize) -> u8>(addr.start)(tmp_rsp);
		assert_eq!(res, 42);

		b.get_stack_dirty();

		assert!(b.pages[0].dirty);
		assert!(!b.pages[1].dirty);
		assert!(!b.pages[14].dirty);
		assert!(b.pages[15].dirty);
		
		let real_rsp = isize::from_le_bytes(ptr[addr.size - 8..].try_into().unwrap());
		let current_rsp = &real_rsp as *const isize as isize;
		assert!((real_rsp - current_rsp).abs() < 0x10000);
		Ok(())
	}
}

#[test]
fn test_state_basic() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36c00000000, size: 0x4000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		let ptr = b.addr.slice_mut();
		b.mmap_fixed(addr, Protection::RW, true)?;
		ptr[0x0000] = 20;
		ptr[0x1000] = 40;
		ptr[0x2000] = 60;
		ptr[0x3000] = 80;

		b.seal()?;
		let mut state0 = Vec::new();
		b.save_state(&mut state0)?;

		// no pages should be in the state
		assert!(state0.len() < 0x1000);

		ptr[0x1000] = 100;
		ptr[0x3000] = 44;

		let mut state1 = Vec::new();
		b.save_state(&mut state1)?;

		// two pages should be in the state
		assert!(state1.len() > 0x2000);
		assert!(state1.len() < 0x3000);

		b.load_state(&mut state0.as_slice())?;

		assert_eq!(ptr[0x0000], 20);
		assert_eq!(ptr[0x1000], 40);
		assert_eq!(ptr[0x2000], 60);
		assert_eq!(ptr[0x3000], 80);

		b.load_state(&mut state1.as_slice())?;

		assert_eq!(ptr[0x0000], 20);
		assert_eq!(ptr[0x1000], 100);
		assert_eq!(ptr[0x2000], 60);
		assert_eq!(ptr[0x3000], 44);

		Ok(())
	}
}

#[test]
fn test_state_unreadable() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36c00000000, size: 0x1000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		let ptr = b.addr.slice_mut();
		b.mmap_fixed(addr, Protection::RW, true)?;
		b.seal()?;

		ptr[200] = 200;
		ptr[500] = 100;
		b.mprotect(addr, Protection::None)?;
		let mut state0 = Vec::new();
		b.save_state(&mut state0)?;

		b.mprotect(addr, Protection::RW)?;
		ptr[300] = 50;
		ptr[600] = 11;
		b.mprotect(addr, Protection::None)?;
		let mut state1 = Vec::new();
		b.save_state(&mut state1)?;

		b.load_state(&mut state0.as_slice())?;
		b.mprotect(addr, Protection::R)?;
		assert_eq!(ptr[200], 200);
		assert_eq!(ptr[500], 100);
		assert_eq!(ptr[300], 0);
		assert_eq!(ptr[600], 0);

		b.load_state(&mut state1.as_slice())?;
		b.mprotect(addr, Protection::R)?;
		assert_eq!(ptr[200], 200);
		assert_eq!(ptr[500], 100);
		assert_eq!(ptr[300], 50);
		assert_eq!(ptr[600], 11);

		Ok(())
	}
}

#[test]
fn test_thready_stack() -> TestResult {
	use std::sync::{Arc, Barrier};
	use std::thread;

	let barrier = Arc::new(Barrier::new(16));
	let mut ress = Vec::<thread::JoinHandle<TestResult>>::new();
	for i in 0..16 {
		let blocker = barrier.clone();
		ress.push(thread::spawn(move|| {
			unsafe {
				let addr = AddressRange { start: 0x36000000000 + i * 0x100000000, size: PAGESIZE * 2 };
				let mut b = MemoryBlock::new(addr);
				b.activate();

				blocker.wait();
				b.mmap_fixed(addr, Protection::RWX, true)?;
				b.mprotect(AddressRange { start: addr.start + PAGESIZE, size: PAGESIZE }, Protection::RWStack)?;

				let ptr = b.addr.slice_mut();
				let mut i = 0;
	
				ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xe0 ; i += 1; // mov rax,rsp
				ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xfc ; i += 1; // mov rsp,rdi
				ptr[i] = 0x50 ; i += 1; // push rax
				ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xc4 ; i += 1; // mov rsp,rax
				ptr[i] = 0xb0 ; i += 1; ptr[i] = 0x2a ; i += 1; // mov al,0x2a
				ptr[i] = 0xc3 ; // ret 

				b.seal()?;

				assert!(!b.pages[0].dirty);
				assert!(!b.pages[1].dirty);
				let tmp_rsp = addr.end();
				let res = transmute::<usize, extern "sysv64" fn(rsp: usize) -> u8>(addr.start)(tmp_rsp);
				assert_eq!(res, 42);

				b.get_stack_dirty();
	
				assert!(!b.pages[0].dirty);
				assert!(b.pages[1].dirty);

				Ok(())
			}
		}));
	}
	for h in ress {
		match h.join() {
			Ok(v) => v,
			Err(_) => return Err(anyhow!("Thread error")),
		}?
	}

	Ok(())
}

#[test]
fn test_state_invisible() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36400000000, size: 0x4000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		let ptr = b.addr.slice_mut();
		b.mmap_fixed(addr, Protection::RW, true)?;
		ptr[0x0055] = 11;
		ptr[0x1055] = 22;
		b.mark_invisible(AddressRange { start: 0x36400001000, size: 0x2000 })?;
		ptr[0x2055] = 33;
		ptr[0x3055] = 44;

		b.seal()?;

		ptr[0x0055] = 0x11;
		ptr[0x1055] = 0x22;
		ptr[0x2055] = 0x33;
		ptr[0x3055] = 0x44;

		let mut state0 = Vec::new();
		b.save_state(&mut state0)?;

		// two pages should be in the state
		assert!(state0.len() > 0x2000);
		assert!(state0.len() < 0x3000);

		ptr[0x0055] = 0x55;
		ptr[0x1055] = 0x66;
		ptr[0x2055] = 0x77;
		ptr[0x3055] = 0x88;

		b.load_state(&mut state0.as_slice())?;

		assert_eq!(ptr[0x0055], 0x11);
		// Some current cores require this behavior, where the invisible values are actually left untouched.
		// (VB for config settings?)
		// In the long term, it might be nice to redefine things so that invisible means invisible and ephemeral,
		// and forcibly zero any active invisible page on loadstate.
		assert_eq!(ptr[0x1055], 0x66);
		assert_eq!(ptr[0x2055], 0x77);
		assert_eq!(ptr[0x3055], 0x44);

		Ok(())
	}
}

#[test]
fn test_dontneed() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36500000000, size: 0x10000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		b.seal()?;
		let ptr = b.addr.slice_mut();

		b.mmap_fixed(addr, Protection::RW, true)?;
		for i in 0..addr.size {
			ptr[i] = i as u8;
		}
		let addr2 = AddressRange { start: addr.start + 0x3000, size: 0x5000 };
		b.madvise_dontneed(addr2)?;
		let ptr2 = addr2.slice_mut();
		for i in 0..addr2.size {
			assert_eq!(ptr2[i], 0);
		}

		let mut state0 = Vec::new();
		b.save_state(&mut state0)?;
		assert!(state0.len() < 0xc000);

		Ok(())
	}
}

#[test]
fn test_remap_nomove() -> TestResult {
	let addr = AddressRange { start: 0x36600000000, size: 0x10000 };
	let mut b = MemoryBlock::new(addr);
	b.activate();

	b.mmap_fixed(AddressRange { start: addr.start, size: 0x4000 }, Protection::RWX, true)?;
	b.mremap_nomove(AddressRange { start: addr.start, size: 0x4000 }, 0x6000)?;
	assert_eq!(b.pages[3].status, PageAllocation::Allocated(Protection::RWX));
	assert_eq!(b.pages[5].status, PageAllocation::Allocated(Protection::RWX));
	b.mremap_nomove(AddressRange { start: addr.start, size: 0x6000 }, 0x3000)?;
	assert_eq!(b.pages[2].status, PageAllocation::Allocated(Protection::RWX));
	assert_eq!(b.pages[3].status, PageAllocation::Free);
	assert_eq!(b.pages[5].status, PageAllocation::Free);

	Ok(())
}

#[test]
fn test_mmap_move() -> TestResult {
	let addr = AddressRange { start: 0x36700000000, size: 0x10000 };
	let mut b = MemoryBlock::new(addr);
	b.activate();
	
	let p0 = b.mmap_movable(0x10000, Protection::RW, addr)?;
	assert_eq!(p0, 0x36700000000);
	b.munmap(AddressRange { start: 0x36700002000, size: 0x2000 })?;
	b.munmap(AddressRange { start: 0x3670000a000, size: 0x1000 })?;
	
	let p1: usize = b.mmap_movable(0x1000, Protection::RW, addr)?;
	assert_eq!(p1, 0x3670000a000); // fit in smallest hole

	Ok(())
}

#[test]
fn test_mremap_move_expand() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36800000000, size: 0x4000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		let ptr = b.addr.slice_mut();

		let initial_addr = AddressRange { start: 0x36800002000, size: 0x1000 };
		b.mmap_fixed(initial_addr, Protection::RW, true)?;
		ptr[0x2004] = 11;
		let p1 = b.mremap_maymove(initial_addr, 0x2000, addr)?;
		assert_eq!(p1, addr.start);
		assert_eq!(ptr[4], 11);
		b.mmap_fixed(initial_addr, Protection::RW, true)?;
		assert_eq!(ptr[0x2004], 0);
	}
	Ok(())
}

#[test]
fn test_mremap_move_shrink() -> TestResult {
	unsafe {
		let addr = AddressRange { start: 0x36900000000, size: 0x4000 };
		let mut b = MemoryBlock::new(addr);
		b.activate();
		let ptr = b.addr.slice_mut();

		let initial_addr = AddressRange { start: 0x36900001000, size: 0x3000 };
		b.mmap_fixed(initial_addr, Protection::RW, true)?;
		ptr[0x1004] = 11;
		let p1 = b.mremap_maymove(initial_addr, 0x1000, addr)?;
		assert_eq!(p1, addr.start);
		assert_eq!(ptr[4], 11);
		b.mmap_fixed(initial_addr, Protection::RW, true)?;
		assert_eq!(ptr[0x1004], 0);
	}
	Ok(())
}
