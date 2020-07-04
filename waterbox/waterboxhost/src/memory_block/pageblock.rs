use std::ptr::NonNull;
use crate::*;
use memory_block::{Protection, pal};

/// wraps the allocation of a single PAGESIZE bytes of ram, and is safe-ish to call within a signal handler
#[derive(Debug)]
pub struct PageBlock {
	ptr: NonNull<u8>,
}

impl PageBlock {
	pub fn new() -> PageBlock {
		unsafe {
			let addr = pal::map_anon(AddressRange { start: 0, size: PAGESIZE }, Protection::RW).unwrap();
			PageBlock {
				ptr: NonNull::new_unchecked(addr.start as *mut u8),
			}
		}
	}

	pub fn slice<'a>(&'a self) -> &'a [u8] {
		unsafe {
			std::slice::from_raw_parts(self.ptr.as_ptr(), PAGESIZE)
		}
	}
	pub fn slice_mut<'a>(&'a mut self) -> &'a mut [u8] {
		unsafe {
			std::slice::from_raw_parts_mut(self.ptr.as_ptr(), PAGESIZE)
		}
	}
	pub fn as_ptr(&self) -> *const u8 {
		self.ptr.as_ptr()
	}
	pub fn as_mut_ptr(&mut self) -> *mut u8 {
		self.ptr.as_ptr()
	}
}

impl Drop for PageBlock {
	fn drop(&mut self) {
		unsafe {
			pal::unmap_annon(AddressRange { start: self.ptr.as_ptr() as usize, size: PAGESIZE }).unwrap();
		}
	}
}

#[cfg(test)]
#[test]
fn basic_test() {
	let mut s = PageBlock::new();

	for x in s.slice().iter() {
		assert!(*x == 0);
	}
	let ml = s.slice_mut();
	for i in 0..PAGESIZE {
		ml[i] = i as u8;
	}
	let sl = s.slice();
	for i in 0..PAGESIZE {
		assert!(sl[i] == i as u8);
	}
}
