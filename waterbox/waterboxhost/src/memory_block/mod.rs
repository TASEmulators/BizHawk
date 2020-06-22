mod pageblock;
mod pal;
mod tripguard;

use std::ops::{DerefMut, Deref};
use parking_lot::ReentrantMutex;
use std::collections::HashMap;
use std::sync::Mutex;
use pageblock::PageBlock;
use crate::*;
use getset::Getters;
use lazy_static::lazy_static;
use crate::syscall_defs::*;
use itertools::Itertools;
use std::io;

lazy_static! {
	static ref LOCK_LIST: Mutex<HashMap<u32, ReentrantMutex<Option<MemoryBlockRef>>>> = Mutex::new(HashMap::new());
}

fn align_down(p: usize) -> usize {
	p & !PAGEMASK
}
fn align_up(p: usize) -> usize {
	((p - 1) | PAGEMASK) + 1
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Protection {
	None,
	R,
	RW,
	RX,
	RWX,
	RWStack
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
enum PageAllocation {
	/// not in use by the guest
	Free,
	/// in use by the guest system, with a particular allocation status
	Allocated(Protection),
}
impl PageAllocation {
	pub fn writable(&self) -> bool {
		use PageAllocation::*;
		match self {
			Allocated(Protection::RW) | Allocated(Protection::RWX) => true,
			_ => false,
		}
	}
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
	pub status: PageAllocation,
	/// if true, the page has changed from its ground state
	pub dirty: bool,
	pub snapshot: Snapshot,
}
impl Page {
	pub fn new() -> Page {
		Page {
			status: PageAllocation::Free,
			dirty: false,
			snapshot: Snapshot::ZeroFilled,
		}
	}
	/// Take a snapshot if one is not yet stored
	/// unsafe: caller must ensure pages are mapped and addr is correct
	pub unsafe fn maybe_snapshot(&mut self, addr: usize) {
		if match self.snapshot { Snapshot:: None => true, _ => false } {
			let mut snapshot = PageBlock::new();
			let src = std::slice::from_raw_parts(addr as *const u8, PAGESIZE);
			let dst = snapshot.slice_mut();
			dst.copy_from_slice(src);
			self.snapshot = Snapshot::Data(snapshot);	
		}
	}
}

#[derive(Getters)]
#[derive(Debug)]
pub struct MemoryBlock {
	#[get]
	pages: Vec<Page>,
	#[get]
	addr: AddressRange,
	#[get]
	sealed: bool,

	lock_index: u32,
	handle: pal::Handle,
	lock_count: u32,
}

pub struct MemoryBlockGuard<'a> {
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
	pub fn new(addr: AddressRange) -> Box<MemoryBlock> {
		if addr.start != align_down(addr.start) || addr.size != align_down(addr.size) {
			panic!("Addresses and sizes must be aligned!");
		}
		if addr.start >> 32 != (addr.end() - 1) >> 32 {
			panic!("MemoryBlock must fit into a single 4G region!");
		}
		let npage = addr.size >> PAGESHIFT;
		let mut pages = Vec::new();
		pages.reserve_exact(npage);
		for _ in 0..npage {
			pages.push(Page::new());
		}
		let handle = pal::open(addr.size).unwrap();
		let lock_index = (addr.start >> 32) as u32;
		// add the lock_index stuff now, so we won't have to check for it later on activate / drop
		{
			let map = &mut LOCK_LIST.lock().unwrap();
			map.entry(lock_index).or_insert(ReentrantMutex::new(None));
		}

		Box::new(MemoryBlock {
			pages,
			addr,
			sealed: false,

			lock_index,
			handle,
			lock_count: 0,
		})
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
		assert!(pal::map(&self.handle, self.addr));
		tripguard::register(self);
		MemoryBlock::refresh_protections(self.addr.start, self.pages.as_slice());
	}
	unsafe fn swapout(&mut self) {
		self.get_stack_dirty();
		assert!(pal::unmap(self.addr));
		tripguard::unregister(self);
	}

	pub fn active (&self) -> bool {
		self.lock_count > 0
	}

	fn validate_range(&mut self, addr: AddressRange) -> Result<&mut [Page], i32> {
		if addr.start < self.addr.start
			|| addr.end() > self.addr.end()
			|| addr.size == 0
			|| addr.start != align_down(addr.start)
			|| addr.size != align_down(addr.size) {
			Err(EINVAL)
		} else {
			let pstart = (addr.start - self.addr.start) >> PAGESHIFT;
			let pend = (addr.size) >> PAGESHIFT;
			Ok(&mut self.pages[pstart..pend])
		}
	}

	fn refresh_protections(mut start: usize, pages: &[Page]) {
		struct Chunk {
			addr: AddressRange,
			prot: Protection,
		};
		let chunks = pages.iter()
			.map(|p| {
				let prot = match p.status {
					#[cfg(windows)]
					PageAllocation::Allocated(Protection::RWStack) if p.dirty => Protection::RW,
					PageAllocation::Allocated(Protection::RW) if !p.dirty => Protection::R,
					PageAllocation::Allocated(Protection::RWX) if !p.dirty => Protection::RX,
					#[cfg(unix)]
					PageAllocation::Allocated(Protection::RWStack) => if p.dirty { Protection::RW } else { Protection::R },
					PageAllocation::Allocated(x) => x,
					PageAllocation::Free => Protection::None,
				};
				let pstart = start;
				start += PAGESIZE;
				Chunk {
					addr: AddressRange { start: pstart, size: PAGESIZE },
					prot,
				}
			})
			.coalesce(|x, y| if x.prot == y.prot {
				Ok(Chunk {
					addr: AddressRange { start: x.addr.start, size: x.addr.size + y.addr.size },
					prot: x.prot,
				})
			} else {
				Err((x, y))
			});

		for c in chunks {
			unsafe {
				assert!(pal::protect(c.addr, c.prot));
			}
		}
	}

	fn set_protections(start: usize, pages: &mut [Page], status: PageAllocation) {
		for p in pages.iter_mut() {
			p.status = status;
		}
		MemoryBlock::refresh_protections(start, pages);
		#[cfg(windows)]
		if status == PageAllocation::Allocated(Protection::RWStack) {
			// have to precapture snapshots here
			let mut addr = start;
			for p in pages {
				unsafe {
					p.maybe_snapshot(addr);
				}
				addr += PAGESIZE;
			}
		}
	}

	/// Updates knowledge on RWStack tripped areas.  Must be called before those areas change allocation type, or are swapped out.
	/// noop on linux
	fn get_stack_dirty(&mut self) {
		#[cfg(windows)]
		unsafe {
			let mut start = self.addr.start;
			let mut pindex = 0;
			while start < self.addr.end() {
				if !self.pages[pindex].dirty && self.pages[pindex].status == PageAllocation::Allocated(Protection::RWStack) {
					let mut res = pal::get_stack_dirty(start).unwrap();
					while res.size > 0 && start < self.addr.end() {
						if res.dirty && self.pages[pindex].status == PageAllocation::Allocated(Protection::RWStack) {
							self.pages[pindex].dirty = true;
						}
						res.size -= PAGESIZE;
						start += PAGESIZE;
						pindex += 1;
					}
				} else {
					start += PAGESIZE;
					pindex += 1;
				}
			}
		}
	}

	/// implements a subset of mmap(2)
	pub fn mmap_fixed(&mut self, addr: AddressRange, prot: Protection) -> SyscallResult {
		self.get_stack_dirty(); // not needed here technically?
		let pages = self.validate_range(addr)?;
		if pages.iter().any(|p| p.status != PageAllocation::Free) {
			// assume MAP_FIXED_NOREPLACE at all times
			return Err(EEXIST)
		}
		MemoryBlock::set_protections(addr.start, pages, PageAllocation::Allocated(prot));
		Ok(())
	}

	/// implements a subset of mprotect(2)
	pub fn mprotect(&mut self, addr: AddressRange, prot: Protection) -> SyscallResult {
		self.get_stack_dirty();
		let pages = self.validate_range(addr)?;
		if pages.iter().any(|p| p.status == PageAllocation::Free) {
			return Err(ENOMEM)
		}
		MemoryBlock::set_protections(addr.start, pages, PageAllocation::Allocated(prot));
		Ok(())
	}

	/// implements a subset of munmap(2)
	pub fn munmap(&mut self, addr: AddressRange) -> SyscallResult {
		self.get_stack_dirty();
		let pages = self.validate_range(addr)?;
		if pages.iter().any(|p| p.status == PageAllocation::Free) {
			return Err(EINVAL)
		}
		// we do not save the current state of unmapped pages, and if they are later remapped,
		// the expectation is that they will start out as zero filled.  accordingly, the most
		// sensible way to do this is to zero them now
		unsafe {
			pal::protect(addr, Protection::RW);
			std::ptr::write_bytes(addr.start as *mut u8, 0, addr.size);
			// simple state size optimization: we can undirty pages in this case depending on the initial state
			for p in pages.iter_mut() {
				p.dirty = match p.snapshot {
					Snapshot::ZeroFilled => false,
					_ => true
				};
			}
		}
		MemoryBlock::set_protections(addr.start, pages, PageAllocation::Free);
		Ok(())
	}

	pub fn seal(&mut self) {
		assert!(!self.sealed);
		for p in self.pages.iter_mut() {
			if p.dirty {
				p.dirty = false;
			} else {
				p.snapshot = Snapshot::ZeroFilled;
			}
		}
	}
}

impl IStateable for MemoryBlock {
	fn save_sate(&mut self, stream: Box<dyn Write>) -> Result<(), io::Error> {
		assert!(self.sealed);
		self.get_stack_dirty();
		Ok(())
	}
	fn load_state(&mut self, stream: Box<dyn Read>) -> Result<(), io::Error> {
		assert!(self.sealed);
		self.get_stack_dirty();
		Ok(())
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
	fn test_create() {
		drop(MemoryBlock::new(AddressRange { start: 0x36300000000, size: 0x50000 }));
		drop(MemoryBlock::new(AddressRange { start: 0x36b00000000, size: 0x2000 }));
		{
			let mut b = MemoryBlock::new(AddressRange { start: 0x36100000000, size: 0x65000 });
			b.activate();
			b.deactivate();
			b.enter();
		}
		{
			let mut b = MemoryBlock::new(AddressRange { start: 0x36e00000000, size: 0x5000 });
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

	#[test]
	fn test_dirty() -> SyscallResult {
		unsafe {
			let addr = AddressRange { start: 0x36f00000000, size: 0x10000 };
			let mut b = MemoryBlock::new(addr);
			let mut g = b.enter();
			g.mmap_fixed(addr, Protection::RW)?;
			let ptr = g.addr.slice_mut();
			ptr[0x2003] = 5;
			assert!(g.pages[2].dirty);
			Ok(())
		}
	}
}
