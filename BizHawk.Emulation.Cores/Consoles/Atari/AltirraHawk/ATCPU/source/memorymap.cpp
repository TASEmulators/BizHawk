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

#include <stdafx.h>
#include <at/atcpu/memorymap.h>

void ATCoProcReadMemory(const uintptr *readMap, void *dst, uint32 address, uint32 n) {
	while(n) {
		if (address >= 0x10000) {
			memset(dst, 0, n);
			break;
		}

		uint32 tc = 256 - (address & 0xff);
		if (tc > n)
			tc = n;

		const uintptr pageBase = readMap[address >> 8];

		if (pageBase & 1) {
			const auto *node = (const ATCoProcReadMemNode *)(pageBase - 1);

			for(uint32 i=0; i<tc; ++i)
				((uint8 *)dst)[i] = node->mpDebugRead(address++, node->mpThis);
		} else {
			memcpy(dst, (const uint8 *)(pageBase + address), tc);

			address += tc;
		}

		n -= tc;
		dst = (char *)dst + tc;
	}
}

void ATCoProcWriteMemory(const uintptr *writeMap, const void *src, uint32 address, uint32 n) {
	while(n) {
		if (address >= 0x10000)
			break;

		const uintptr pageBase = writeMap[address >> 8];

		if (pageBase & 1) {
			auto& writeNode = *(ATCoProcWriteMemNode *)(pageBase - 1);

			writeNode.mpWrite(address, *(const uint8 *)src, writeNode.mpThis);
			++address;
			src = (const uint8 *)src + 1;
			--n;
		} else {
			uint32 tc = 256 - (address & 0xff);
			if (tc > n)
				tc = n;

			memcpy((uint8 *)(pageBase + address), src, tc);

			n -= tc;
			address += tc;
			src = (const char *)src + tc;
		}
	}
}
