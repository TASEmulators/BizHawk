//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
//	Application core library - memory utilities
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
#include <at/atcore/memoryutils.h>

uint32 ATRandomizeMemory8(uint8 *dst, size_t count, uint32 seed) {
	while(count--) {
		*dst++ = (uint8)seed;

		uint32 sbits = seed & 0xFF;
		seed = (seed >> 8) ^ (sbits << 24) ^ (sbits << 22) ^ (sbits << 18) ^ (sbits << 17);
	}

	return seed;
}

uint32 ATRandomizeMemory32(uint32 *dst, size_t count, uint32 seed) {
	uint32 sbits;
	while(count--) {
		*dst++ = seed;

		sbits = seed & 0xFFFF;
		seed = (seed >> 16) ^ (sbits << 16) ^ (sbits << 14) ^ (sbits << 10) ^ (sbits << 9);

		sbits = seed & 0xFFFF;
		seed = (seed >> 16) ^ (sbits << 16) ^ (sbits << 14) ^ (sbits << 10) ^ (sbits << 9);
	}

	return seed;
}

uint32 ATRandomizeMemory(void *dst0, size_t len, uint32 seed) {
	uint8 *dst = (uint8 *)dst0;

	if (len < 8) {
		ATRandomizeMemory8(dst, len, seed);
		return seed;
	}

	size_t align = (size_t)-(ptrdiff_t)dst & 7;
	if (align) {
		seed = ATRandomizeMemory8(dst, align, seed);
		dst += align;
		len -= align;
	}

	seed = ATRandomizeMemory32((uint32 *)dst, len >> 2, seed);
	dst += len & ~(size_t)3;

	size_t remain = len & 3;
	if (remain)
		seed = ATRandomizeMemory8(dst, remain, seed);

	return seed;
}
