#![crate_type = "cdylib"]

use std::io::{Read, Write};

const PAGESIZE: usize = 0x1000;
const PAGEMASK: usize = 0xfff;
const PAGESHIFT: i32 = 12;

mod memory_block;

pub trait IStateable {
	fn save_sate(&mut self, stream: Box<dyn Write>);
	fn load_state(&mut self, stream: Box<dyn Read>);
}

#[cfg(test)]
mod tests {
	#[test]
	fn test_pagesize() {
		assert_eq!(crate::PAGESIZE, page_size::get());
	}
}
