mod pageblock;
mod pal;
mod tripguard;

use std::ops::{DerefMut, Deref};
use parking_lot::ReentrantMutex;
use std::collections::HashMap;
use std::sync::Mutex;
use pageblock::PageBlock;
use bitflags::bitflags;
use crate::*;
use getset::Getters;
use lazy_static::lazy_static;

lazy_static! {
	static ref LOCK_LIST: Mutex<HashMap<u32, ReentrantMutex<Option<MemoryBlockRef>>>> = Mutex::new(HashMap::new());
}

fn alignDown(p: usize) -> usize {
	p & !PAGEMASK
}
fn alignUp(p: usize) -> usize {
	((p - 1) | PAGEMASK) + 1
}

bitflags! {
	struct PageFlags: u32 {
		const R = 1;
		const W = 2;
		const X = 4;
		/// This page is mapped in the waterbox right now
		const ALLOCATED = 8;
		/// The contents of this page have changed since the dirty flag was set
		const DIRTY = 16;
		/// rsp might point here.  On some OSes, use an alternate method of dirt detection
		const STACK = 32;
	}
}
pub enum Protection {
	None,
	R,
	RW,
	RX,
	RWX,
	RWStack
}

#[derive(Debug)]
enum Snapshot {
	None,
	ZeroFilled,
	Data(PageBlock),
}

/// Information about a single page of memory
#[derive(Debug)]
struct Page {
	pub flags: PageFlags,
	pub snapshot: Snapshot,
}

#[derive(Getters)]
#[derive(Debug)]
struct MemoryBlock {
	#[get]
	pages: Vec<Page>,
	#[get]
	start: usize,
	#[get]
	length: usize,
	#[get]
	end: usize,
	#[get]
	sealed: bool,

	lock_index: u32,
	handle: pal::Handle,
	lock_count: u32,
}

struct MemoryBlockGuard<'a> {
	block: &'a mut MemoryBlock,
}
impl<'a> Drop for MemoryBlockGuard<'a> {
	fn drop(&mut self) {
		self.block.deactivate();
	}
}
impl<'a> Deref for MemoryBlockGuard<'a> {
	type Target = MemoryBlock;
	fn deref(&self) -> &MemoryBlock {
		self.block
	}
}
impl<'a> DerefMut for MemoryBlockGuard<'a> {
	fn deref_mut(&mut self) -> &mut MemoryBlock {
		self.block
	}
}

impl MemoryBlock {
	pub fn new(start: usize, length: usize) -> MemoryBlock {
		if start != alignDown(start) || length != alignDown(length) {
			panic!("Addresses and sizes must be aligned!");
		}
		let end = start + length;
		if start >> 32 != (end - 1) >> 32 {
			panic!("MemoryBlock must fit into a single 4G region!");
		}
		let npage = length >> PAGESHIFT;
		let mut pages = Vec::new();
		pages.reserve_exact(npage);
		for _ in 0..npage {
			pages.push(Page {
				flags: PageFlags::empty(),
				snapshot: Snapshot::None,
			});
		}
		let handle = pal::open(length).unwrap();
		let lock_index = (start >> 32) as u32;
		// add the lock_index stuff now, so we won't have to check for it later on activate / drop
		{
			let map = &mut LOCK_LIST.lock().unwrap();
			map.entry(lock_index).or_insert(ReentrantMutex::new(None));
		}

		MemoryBlock {
			pages,
			start,
			length,
			end,
			sealed: false,

			lock_index,
			handle,
			lock_count: 0,
		}
	}

	pub fn enter(&mut self) -> MemoryBlockGuard {
		self.activate();
		MemoryBlockGuard {
			block: self,
		}
	}

	/// lock self, and potentially swap this block into memory
	pub fn activate(&mut self) {
		unsafe {
			let map = &mut LOCK_LIST.lock().unwrap();
			let lock = map.get_mut(&self.lock_index).unwrap();
			std::mem::forget(lock.lock());
			let other_opt = lock.get_mut();
			match *other_opt {
				Some(MemoryBlockRef(other)) => {
					if other != self {
						assert!(!(*other).active());
						(*other).swapout();
						self.swapin();
						*other_opt = Some(MemoryBlockRef(self));
					}			
				},
				None => {
					self.swapin();
					*other_opt = Some(MemoryBlockRef(self));	
				}
			}
			self.lock_count += 1;
		}
	}
	/// unlock self, and potentially swap this block out of memory
	pub fn deactivate(&mut self) {
		unsafe {
			assert!(self.active());
			let map = &mut LOCK_LIST.lock().unwrap();
			let lock = map.get_mut(&self.lock_index).unwrap();
			self.lock_count -= 1;
			#[cfg(debug_assertions)]
			{
				let other_opt = lock.get_mut();
				let other = other_opt.as_ref().unwrap().0;
				assert_eq!(&*other, self);
				// in debug mode, forcibly evict to catch dangling pointers
				if !self.active() {
					self.swapout();
					*other_opt = None;
				}
			}
			lock.force_unlock();
		}
	}

	unsafe fn swapin(&mut self) {
		assert!(pal::map(&self.handle, self.start, self.length));
	}
	unsafe fn swapout(&mut self) {
		assert!(pal::unmap(self.start, self.length));
	}

	pub fn active (&self) -> bool {
		self.lock_count > 0
	}

	pub fn protect(&mut self, start: usize, length: usize, prot: Protection) {

	}
	pub fn seal(&mut self) {

	}
}

impl IStateable for MemoryBlock {
	fn save_sate(&mut self, stream: Box<dyn Write>) {

	}
	fn load_state(&mut self, stream: Box<dyn Read>) {

	}
}

impl Drop for MemoryBlock {
	fn drop(&mut self) {
		assert!(!self.active());
		let map = &mut LOCK_LIST.lock().unwrap();
		let lock = map.get_mut(&self.lock_index).unwrap();
		let other_opt = lock.get_mut();
		match *other_opt {
			Some(MemoryBlockRef(other)) => {
				if other == self {
					unsafe {
						self.swapout();
					}
					*other_opt = None;
				}
			},
			None => ()
		}
		let mut h = pal::bad();
		std::mem::swap(&mut h, &mut self.handle);
		unsafe {
			pal::close(h);
		}
	}
}

impl PartialEq for MemoryBlock {
	fn eq(&self, other: &MemoryBlock) -> bool {
		self as *const MemoryBlock == other as *const MemoryBlock
	}
}
impl Eq for MemoryBlock {}

struct MemoryBlockRef(*mut MemoryBlock);
unsafe impl Send for MemoryBlockRef {}

#[cfg(test)]
mod tests {
	use super::*;

	#[test]
	fn test_basic() {
		drop(MemoryBlock::new(0x36300000000, 0x50000));
		drop(MemoryBlock::new(0x36b00000000, 0x2000));
		{
			let mut b = MemoryBlock::new(0x36100000000, 0x65000);
			b.activate();
			b.deactivate();
			b.enter();
		}
		{
			let mut b = MemoryBlock::new(0x36e00000000, 0x5000);
			b.activate();
			b.activate();
			let mut guard = b.enter();
			guard.activate();
			guard.deactivate();
			drop(guard);
			b.deactivate();
			b.deactivate();
			b.enter();
		}
	}
}
