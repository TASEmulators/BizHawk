//	Altirra - Atari 800/800XL/5200 emulator
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
#include <vd2/system/binary.h>
#include <at/atcore/checksum.h>

uint64 ATComputeOffsetChecksum(uint64 offset) {
	uint8 buf[8];

	VDWriteUnalignedLEU64(buf, offset);

	return ATComputeBlockChecksum(kATBaseChecksum, buf, 8);
}

uint64 ATComputeBlockChecksum(uint64 hash, const void *src, size_t len) {
	const uint64 kFNV1Prime = 1099511628211;

	const uint8 *src8 = (const uint8 *)src;

	while(len--)
		hash = (hash * kFNV1Prime) ^ *src8++;

	return hash;
}

uint64 ATComputeZeroBlockChecksum(uint64 hash, size_t len) {
	const uint64 kFNV1Prime = 1099511628211;
	const uint64 kFNV1Offset = 14695981039346656037;
	uint64 multiplier = kFNV1Prime;

	for(;;) {
		if (len & 1)
			hash *= multiplier;

		len >>= 1;
		if (!len)
			break;

		multiplier *= multiplier;
	}

	return hash;
}
