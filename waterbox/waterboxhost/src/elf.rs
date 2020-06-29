use goblin;
use goblin::elf64::{sym::*, section_header::*};
use crate::*;
use crate::memory_block::ActivatedMemoryBlock;
use crate::memory_block::Protection;
use std::collections::HashMap;

/// Special system import area
const IMPORTS_OBJECT_NAME: &str = "__wbxsysarea";

/// Section names that are not marked as readonly, but we'll make them readonly anyway
fn section_name_is_readonly(name: &str) -> bool {
	name.contains(".rel.ro")
		|| name.starts_with(".got")
		|| name == ".init_array"
		|| name == ".fini_array"
		|| name == ".tbss"
		|| name == ".sealed"
}

pub struct SectionInfo {
	name: String,
	addr: AddressRange,
}
pub struct ElfLoader {
	sections: Vec<SectionInfo>,
	exports: HashMap<String, AddressRange>,
	entry_point: usize,
	hash: Vec<u8>,
	import_area: AddressRange,
}
impl ElfLoader {
	pub fn new(data: &[u8],
		module_name: &str,
		layout: &WbxSysLayout,
		b: &mut ActivatedMemoryBlock
	) -> anyhow::Result<ElfLoader> {
		let wbx = goblin::elf::Elf::parse(data)?;

		let start = wbx.program_headers.iter()
			.filter(|x| x.p_vaddr != 0)
			.map(|x| x.vm_range().start)
			.min()
			.unwrap();
		let end = wbx.program_headers.iter()
			.filter(|x| x.p_vaddr != 0)
			.map(|x| x.vm_range().end)
			.max()
			.unwrap();
		if start < layout.elf.start || end > layout.elf.end() {
			return Err(anyhow!("{} from {}..{} did not fit in the provided region", module_name, start, end))
		}

		println!("Mouting `{}` @{:x}", module_name, start);
		println!("  Sections:");

		let mut sections = Vec::new();	

		for section in wbx.section_headers.iter() {
			let name = match wbx.shdr_strtab.get(section.sh_name) {
				Some(Ok(s)) => s,
				_ => "<anon>"
			};
			println!("    @{:x}:{:x} {}{}{} `{}` {} bytes",
				section.sh_addr,
				section.sh_addr + section.sh_size,
				if section.sh_flags & (SHF_ALLOC as u64) != 0 { "R" } else { " " },
				if section.sh_flags & (SHF_WRITE as u64) != 0 { "W" } else { " " },
				if section.sh_flags & (SHF_EXECINSTR as u64) != 0 { "X" } else { " " },
				name,
				section.sh_size
			);
			if section.sh_type != SHT_NOBITS
				&& name != "<anon>"
				&& section.sh_addr != 0 {
				let si = SectionInfo {
					name: name.to_string(),
					addr: AddressRange {
						start: section.sh_addr as usize,
						size: section.sh_size as usize
					}
				};
				sections.push(si);
			}
		}

		let mut exports = HashMap::new();
		let mut import_area_opt = None;

		for sym in wbx.syms.iter() {
			let name = match wbx.strtab.get(sym.st_name) {
				Some(Ok(s)) => s,
				_ => continue
			};
			if sym.st_visibility() == STV_DEFAULT && sym.st_bind() == STB_GLOBAL {
				exports.insert(
					name.to_string(),
					AddressRange { start: sym.st_value as usize, size: sym.st_size as usize }
				);
			}
			if name == IMPORTS_OBJECT_NAME {
				import_area_opt = Some(AddressRange { start: sym.st_value as usize, size: sym.st_size as usize });
			}
		}

		let import_area = match import_area_opt {
			Some(i) => {
				if i.size != std::mem::size_of::<WbxSysArea>() {
					return Err(anyhow!("Symbol {} is the wrong size", IMPORTS_OBJECT_NAME))
				}
				i
			},
			None => return Err(anyhow!("Symbol {} is missing", IMPORTS_OBJECT_NAME))
		};

		{
			let invis_opt = sections.iter().find(|x| x.name == ".invis");
			if let Some(invis) = invis_opt {
				for s in sections.iter() {
					if s.addr.align_expand().start < invis.addr.align_expand().start {
						if s.addr.align_expand().end() > invis.addr.align_expand().start {
							return Err(anyhow!("When aligned, {} partially overlaps .invis from below -- check linkscript.", s.name))
						}
					} else if s.addr.align_expand().start > invis.addr.align_expand().start {
						if invis.addr.align_expand().end() > s.addr.align_expand().start {
							return Err(anyhow!("When aligned, {} partially overlaps .invis from above -- check linkscript.", s.name))
						}
					} else {
						if s.name != ".invis" {
							return Err(anyhow!("When aligned, {} partially overlays .invis -- check linkscript", s.name))
						}
					}
				}
				b.mark_invisible(invis.addr.align_expand())?;
			}
		}

		b.mark_invisible(layout.invis)?;

		println!("  Segments:");
		for segment in wbx.program_headers.iter().filter(|x| x.p_vaddr != 0) {
			let addr = AddressRange {
				start: segment.vm_range().start,
				size: segment.vm_range().end - segment.vm_range().start
			};
			let prot_addr = addr.align_expand();
			let prot = match (segment.is_read(), segment.is_write(), segment.is_executable()) {
				(false, false, false) => Protection::None,
				(true, false, false) => Protection::R,
				(_, false, true) => Protection::RX,
				(_, true, false) => Protection::RW,
				(_, true, true) => Protection::RWX
			};
			println!("    %{:x}:{:x} {}{}{} {} bytes",
				addr.start,
				addr.end(),
				if segment.is_read() { "R" } else { " " },
				if segment.is_write() { "W" } else { " " },
				if segment.is_executable() { "X" } else { " " },
				addr.size
			);
			// TODO:  Using no_replace false here because the linker puts eh_frame_hdr in a separate segment that overlaps the other RO segment???
			b.mmap_fixed(prot_addr, Protection::RW, false)?;
			unsafe {
				let src = &data[segment.file_range()];
				let dst = AddressRange { start: addr.start, size: segment.file_range().end - segment.file_range().start }.slice_mut();
				dst.copy_from_slice(src);
			}
			b.mprotect(prot_addr, prot)?;
		}

		Ok(ElfLoader {
			sections,
			exports,
			entry_point: wbx.entry as usize,
			hash: bin::hash(data),
			import_area
		})
	}
	pub fn seal(&self, b: &mut ActivatedMemoryBlock) {
		for section in self.sections.iter() {
			if section_name_is_readonly(section.name.as_str()) {
				b.mprotect(section.addr, Protection::R).unwrap();
			}
		}
	}
	pub fn connect_syscalls(&mut self, _b: &mut ActivatedMemoryBlock, sys: &WbxSysArea) {
		let addr = self.import_area;
		unsafe { *(addr.start as *mut WbxSysArea) = *sys; }
	}
	pub fn clear_syscalls(&mut self, _b: &mut ActivatedMemoryBlock) {
		let addr = self.import_area;
		unsafe { addr.zero(); }
	}
	pub fn native_init(&mut self, _b: &mut ActivatedMemoryBlock) {
		println!("Calling _start()");
		unsafe {
			std::mem::transmute::<usize, extern "win64" fn() -> ()>(self.entry_point)();
		}
	}
	pub fn co_clean(&mut self, _b: &mut ActivatedMemoryBlock) {
		match self.get_proc_addr("co_clean") {
			0 => (),
			ptr => {
				println!("Calling co_clean()");
				unsafe {
					std::mem::transmute::<usize, extern "win64" fn() -> ()>(ptr)();
				}
			},
		}
	}
	pub fn get_proc_addr(&self, proc: &str) -> usize {
		match self.exports.get(proc) {
			Some(addr) => addr.start,
			None => 0,
		}
	}
}

const MAGIC: &str = "ElfLoader";

impl IStateable for ElfLoader {
	fn save_state(&mut self, stream: &mut dyn Write) -> anyhow::Result<()> {
		bin::write_magic(stream, MAGIC)?;
		bin::write_hash(stream, &self.hash[..])?;
		Ok(())
	}
	fn load_state(&mut self, stream: &mut dyn Read) -> anyhow::Result<()> {
		bin::verify_magic(stream, MAGIC)?;
		bin::verify_hash(stream, &self.hash[..])?;
		Ok(())
	}
}
