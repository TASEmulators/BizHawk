#![crate_type = "cdylib"]

const PAGESIZE: usize = 0x1000;
const PAGEMASK: usize = 0xfff;
const PAGESHIFT: i32 = 12;

mod memory_block;

#[cfg(test)]
mod tests {
	#[test]
	fn it_works() {
		assert_eq!(2 + 2, 4);
	}
}
