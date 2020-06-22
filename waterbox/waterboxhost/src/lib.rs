#![crate_type = "cdylib"]

// TODO: Turn this off once we've built the exported public API
#![allow(dead_code)]

use std::io::{Read, Write, Error};

const PAGESIZE: usize = 0x1000;
const PAGEMASK: usize = 0xfff;
const PAGESHIFT: i32 = 12;

mod memory_block;
mod syscall_defs;

pub trait IStateable {
	fn save_sate(&mut self, stream: Box<dyn Write>) -> Result<(), Error>;
	fn load_state(&mut self, stream: Box<dyn Read>) -> Result<(), Error>;
}

#[derive(Debug, Clone, Copy)]
pub struct AddressRange {
	pub start: usize,
	pub size: usize,
}
impl AddressRange {
	pub fn end(&self) -> usize {
		self.start + self.size
	}
	pub fn contains(&self, addr: usize) -> bool {
		addr >= self.start && addr < self.end()
	}
	/// Unsafe: Pointers are unchecked and lifetime is not connected to the AddressRange
	pub unsafe fn slice(&self) -> &'static [u8] {
		std::slice::from_raw_parts(self.start as *const u8, self.size)
	}
	/// Unsafe: Pointers are unchecked and lifetime is not connected to the AddressRange
	pub unsafe fn slice_mut(&self) -> &'static mut [u8] {
		std::slice::from_raw_parts_mut(self.start as *mut u8, self.size)
	}
}

#[cfg(test)]
mod tests {
	#[test]
	fn test_pagesize() {
		assert_eq!(crate::PAGESIZE, page_size::get());
	}
}
