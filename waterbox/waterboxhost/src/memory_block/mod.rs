mod pageblock;
mod pal;
mod tripguard;

use std::sync::MutexGuard;
use std::ops::{DerefMut, Deref};
use pageblock::PageBlock;
use crate::*;
use getset::Getters;
use crate::syscall_defs::*;
use itertools::Itertools;
use std::io;
use std::sync::atomic::AtomicU32;

/// Tracks one lock for each 4GB memory area
mod lock_list {
	use lazy_static::lazy_static;
	use std::collections::HashMap;
	use std::sync::Mutex;
	use super::MemoryBlockRef;

	lazy_static! {
		static ref LOCK_LIST: Mutex<HashMap<u32, Box<Mutex<Option<MemoryBlockRef>>>>> = Mutex::new(HashMap::new());
	}

	unsafe fn extend<T>(o: &T) -> &'static T {
		std::mem::transmute::<&T, &'static T>(o)
	}
	pub fn maybe_add(lock_index: u32) {
		let map = &mut LOCK_LIST.lock().unwrap();
		map.entry(lock_index).or_insert_with(|| Box::new(Mutex::new(None)));	
	}
	pub fn get(lock_index: u32) -> &'static Mutex<Option<MemoryBlockRef>> {
		let map = &mut LOCK_LIST.lock().unwrap();
		unsafe {
			extend(map.get(&lock_index).unwrap())
		}
	}
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
			Allocated(a) => match a {
				Protection::RW | Protection::RWX | Protection::RWStack => true,
				_ => false
			}
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
	/// if true, the page has changed from its original state
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
	/// Compute the appropriate native protection value given this page's current status
	pub fn native_prot(&self) -> Protection {
		match self.status {
			#[cfg(windows)]
			PageAllocation::Allocated(Protection::RWStack) if self.dirty => Protection::RW,
			PageAllocation::Allocated(Protection::RW) if !self.dirty => Protection::R,
			PageAllocation::Allocated(Protection::RWX) if !self.dirty => Protection::RX,
			#[cfg(unix)]
			PageAllocation::Allocated(Protection::RWStack) => if self.dirty { Protection::RW } else { Protection::R },
			PageAllocation::Allocated(x) => x,
			PageAllocation::Free => Protection::None,
		}
	}
}

static NEXT_DEBUG_ID: AtomicU32 = AtomicU32::new(0);

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
	mutex_guard: Option<MutexGuard<'static, Option<MemoryBlockRef>>>,

	debug_id: u32,
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
		lock_list::maybe_add(lock_index);

		let debug_id = NEXT_DEBUG_ID.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
		let res = Box::new(MemoryBlock {
			pages,
			addr,
			sealed: false,

			lock_index,
			handle,
			lock_count: 0,
			mutex_guard: None,

			debug_id,
		});
		// res.trace("new");
		res
	}

	pub fn trace(&self, name: &str) {
		let ptr = unsafe { std::mem::transmute::<&Self, usize>(self) };
		let tid = unsafe { std::mem::transmute::<std::thread::ThreadId, u64>(std::thread::current().id()) };
		eprintln!("{}#{} {} [{}]@[{}] thr{}",
			name, self.debug_id, ptr, self.lock_count, self.lock_index, tid)
	}

	pub fn enter(&mut self) -> MemoryBlockGuard {
		self.activate();
		MemoryBlockGuard {
			block: self,
		}
	}

	/// lock self, and potentially swap this block into memory
	pub fn activate(&mut self) {
		// self.trace("activate");
		unsafe {
			if !self.active() {
				let area = lock_list::get(self.lock_index);
				let mut guard = area.lock().unwrap();

				let other_opt = guard.deref_mut();
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

				self.mutex_guard = Some(guard);
			}
			self.lock_count += 1;
		}
	}
	/// unlock self, and potentially swap this block out of memory
	pub fn deactivate(&mut self) {
		// self.trace("deactivate");
		unsafe {
			assert!(self.active());
			self.lock_count -= 1;
			if !self.active() {
				let mut guard = std::mem::replace(&mut self.mutex_guard, None).unwrap();
				#[cfg(debug_assertions)]
				{
					// in debug mode, forcibly evict to catch dangling pointers
					let other_opt = guard.deref_mut();
					match *other_opt {
						Some(MemoryBlockRef(other)) => {
							if other != self {
								panic!();
							}
							self.swapout();
							*other_opt = None;
						},
						None => {
							panic!()
						}
					}
				}
			}
		}
	}

	unsafe fn swapin(&mut self) {
		// self.trace("swapin");
		assert!(pal::map(&self.handle, self.addr));
		tripguard::register(self);
		MemoryBlock::refresh_protections(self.addr.start, self.pages.as_slice());
	}
	unsafe fn swapout(&mut self) {
		// self.trace("swapout");
		self.get_stack_dirty();
		assert!(pal::unmap(self.addr));
		tripguard::unregister(self);
	}

	pub fn active(&self) -> bool {
		self.lock_count > 0
	}
}

impl Drop for MemoryBlock {
	fn drop(&mut self) {
		// self.trace("drop");
		assert!(!self.active());
		let area = lock_list::get(self.lock_index);
		let mut guard = area.lock().unwrap();
		let other_opt = guard.deref_mut();
		match *other_opt {
			Some(MemoryBlockRef(other)) => {
				if other == self {
					unsafe { self.swapout(); }
					*other_opt = None;
				}
			},
			None => ()
		}
		let h = std::mem::replace(&mut self.handle, pal::bad());
		unsafe { pal::close(h); }
	}
}

impl MemoryBlock {
	fn validate_range(&mut self, addr: AddressRange) -> Result<&mut [Page], i32> {
		if addr.start < self.addr.start
			|| addr.end() > self.addr.end()
			|| addr.size == 0
			|| addr.start != align_down(addr.start)
			|| addr.size != align_down(addr.size) {
			Err(EINVAL)
		} else {
			let pstart = (addr.start - self.addr.start) >> PAGESHIFT;
			let psize = (addr.size) >> PAGESHIFT;
			Ok(&mut self.pages[pstart..pstart + psize])
		}
	}

	fn refresh_protections(mut start: usize, pages: &[Page]) {
		struct Chunk {
			addr: AddressRange,
			prot: Protection,
		};
		let chunks = pages.iter()
			.map(|p| {
				let cstart = start;
				start += PAGESIZE;
				Chunk {
					addr: AddressRange { start: cstart, size: PAGESIZE },
					prot: p.native_prot(),
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

impl PartialEq for MemoryBlock {
	fn eq(&self, other: &MemoryBlock) -> bool {
		self as *const MemoryBlock == other as *const MemoryBlock
	}
}
impl Eq for MemoryBlock {}

#[derive(Debug)]
pub struct MemoryBlockRef(*mut MemoryBlock);
unsafe impl Send for MemoryBlockRef {}

#[cfg(test)]
mod tests {
	use std::mem::transmute;
	use super::*;

	/// new / drop, activate / deactivate
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

	/// simple test of dirt detection
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

	/// dirt detection away from the start of a block
	#[test]
	fn test_offset() -> SyscallResult {
		unsafe {
			let addr = AddressRange { start: 0x36f00000000, size: 0x20000 };
			let mut b = MemoryBlock::new(addr);
			let mut g = b.enter();
			g.mmap_fixed(AddressRange { start: 0x36f00003000, size: 0x1000 }, Protection::RW)?;
			let ptr = g.addr.slice_mut();
			ptr[0x3663] = 12;
			assert!(g.pages[3].dirty);
			Ok(())
		}
	}

	/// dirt detection in RWStack area when $rsp points there
	#[test]
	fn test_stk_norm() -> SyscallResult {
		unsafe {
			let addr = AddressRange { start: 0x36200000000, size: 0x10000 };
			let mut b = MemoryBlock::new(addr);
			let mut g = b.enter();
			g.mmap_fixed(addr, Protection::RWStack)?;
			let ptr = g.addr.slice_mut();
			ptr[0xeeee] = 0xee;
			ptr[0x44] = 0x44;
			assert!(g.pages[0].dirty);
			assert!(g.pages[14].dirty);
			assert_eq!(ptr[0x8000], 0);

			// This is an unfair test, but it's just documenting the current limitations of the system.
			// Ideally, page 8 would be clean because we read from it but did not write to it.
			// Due to limitations of RWStack tracking on windows, it is dirty.
			#[cfg(windows)]
			assert!(g.pages[8].dirty);
			#[cfg(unix)]
			assert!(!g.pages[8].dirty);

			Ok(())
		}
	}

	/// dirt detection in RWStack area when $rsp points there
	#[test]
	fn test_stack() -> SyscallResult {
		use std::convert::TryInto;
		unsafe {
			let addr = AddressRange { start: 0x36f00000000, size: 0x10000 };
			let mut b = MemoryBlock::new(addr);
			let mut g = b.enter();
			g.mmap_fixed(addr, Protection::RW)?;
			let ptr = g.addr.slice_mut();
			let mut i = 0;

			ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xe0 ; i += 1; // mov rax,rsp
			ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xfc ; i += 1; // mov rsp,rdi
			ptr[i] = 0x50 ; i += 1; // push rax
			ptr[i] = 0x48 ; i += 1; ptr[i] = 0x89 ; i += 1; ptr[i] = 0xc4 ; i += 1; // mov rsp,rax
			ptr[i] = 0xb0 ; i += 1; ptr[i] = 0x2a ; i += 1; // mov al,0x2a
			ptr[i] = 0xc3 ; // ret 

			g.mprotect(AddressRange { start: 0x36f00000000, size: 0x1000 }, Protection::RX)?;
			g.mprotect(AddressRange { start: 0x36f00008000, size: 0x8000 }, Protection::RWStack)?;
			let tmp_rsp = addr.end();
			let res = transmute::<usize, extern "sysv64" fn(rsp: usize) -> u8>(addr.start)(tmp_rsp);
			assert_eq!(res, 42);
			assert!(g.pages[0].dirty);
			assert!(!g.pages[1].dirty);
			assert!(!g.pages[14].dirty);
			assert!(g.pages[15].dirty);
			
			let real_rsp = isize::from_le_bytes(ptr[addr.size - 8..].try_into().unwrap());
			let current_rsp = &real_rsp as *const isize as isize;
			assert!((real_rsp - current_rsp).abs() < 0x10000);
			Ok(())
		}
	}
}
