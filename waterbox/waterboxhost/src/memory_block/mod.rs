mod pageblock;
mod pal;

use pageblock::PageBlock;
use bitflags::bitflags;

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


enum Snapshot {
	None,
	ZeroFilled,
	Data(PageBlock),
}

/// Information about a single page of memory
struct Page {
	pub flags: PageFlags,
	pub snapshot: Snapshot,
}

struct MemoryBlock {
	pub pages: Vec<Page>,
	pub start: usize,
	pub length: usize,
	pub end: usize,
	pub sealed: bool,
}
