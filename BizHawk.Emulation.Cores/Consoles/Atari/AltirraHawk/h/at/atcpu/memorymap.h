//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - CPU memory map support
//	Copyright (C) 2009-2016 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

//=========================================================================
// Coprocessor memory map
//
// The coprocessors access memory through a page map to support control
// registers, unmapped regions, and mirrored regions. The page map consists
// of two arrays of 256 entries, one for reads and another for writes.
// Each entry can be of two types:
//
// - An offset pointer to raw memory. Specifically, a pointer to the
//   memory for a page, with the base address of the page in bytes
//   subtracted off. For instance, a page mapped at $1000 would have
//   (buffer-0x1000) as the page map entry. This allows large regions of
//   memory to be mapped by doing a fill of all page entries with the same
//   value. The mapped memory must be aligned to an even boundary so that
//   the LSB of the page entry is cleared.
//
// - A reference to a custom read/write handler, of either type read node
//   for the read map or write node for the write map. The page map entry
//   is the address of the node with the LSB set.
//
// For read entries, there are two callbacks, one for a regular read and
// another for a debug read. A debug read suppresses all side effects for
// that read and does not change simulation state.
//

#ifndef f_ATCOPROC_MEMORYMAP_H
#define f_ATCOPROC_MEMORYMAP_H

#include <vd2/system/vdtypes.h>

struct ATCoProcReadMemNode {
	uint8 (*mpRead)(uint32 addr, void *thisptr);
	uint8 (*mpDebugRead)(uint32 addr, void *thisptr);
	void *mpThis;

	uintptr AsBase() const {
		return (uintptr)this + 1;
	}
};

struct ATCoProcWriteMemNode {
	void (*mpWrite)(uint32 addr, uint8 val, void *thisptr);
	void *mpThis;

	uintptr AsBase() const {
		return (uintptr)this + 1;
	}
};

void ATCoProcReadMemory(const uintptr *readMap, void *dst, uint32 start, uint32 len);
void ATCoProcWriteMemory(const uintptr *writeMap, const void *src, uint32 start, uint32 len);

#endif	// f_ATCOPROC_MEMORYMAP_H
