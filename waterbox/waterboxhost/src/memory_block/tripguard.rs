
static global_data = Mutex::new(GlobalData {
	initialized: false,
	activeBlocks: Vec::new(),
});

struct GlobalData {
	initialized: bool,
	activeBlocks: Vec<*mut MemoryBlock>, 
}

pub unsafe fn register(block: *mut MemoryBlock) {
	let mut data = global_data.lock().unwrap();
	if !data.initialized {
		initialize();
		data.initialized = true;
	}
	data.activeBlocks.push(block);
}

pub unsafe fn unregister(block: *mut MemoryBlock) {
	let mut data = global_data.lock().unwrap();
	let pos = data.activeBlocks.into_iter().position(|x| x == block).unwrap();
	data.activeBlocks.remove(pos);
}

enum TripResult {
	Handled,
	NotHandled,
}

unsafe fn trip(addr: usize) -> TripResult {
	let mut data = global_data.lock().unwrap();
	let mut memoryBlock = match data.activeBlocks
		.into_iter()
		.find(|x| addr >= x.start && addr < x.end) {
			Some(x) => x,
			None => return NotHandled,
		}
	let pageStartAddr = addr & ~PAGEMASK;
	let mut page = &mut memoryBlock.pages[(addr - memoryBlock.start) >> PAGESHIFT];
	if !page.flags.contains(PageFlags::W) {
		NotHandled
	}
	if memoryBlock.sealed && page.snapshot == Snapshot::None {
		// take snapshot now
	}
}
