//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - binary program image definitions
//	Copyright (C) 2008-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <at/atio/programimage.h>

bool ATParseProgramImage(const void *src, uint32 len, vdfastvector<ATProgramBlock>& blocks) {
	const uint8 *src8 = (const uint8 *)src;

	while(len >= 2) {
		const uint32 blockImageOffset = (uint32)(src8 - (const uint8 *)src);

		if (src8[0] >= 0xFA && src8[1] == 0xFF) {
			const uint8 code = src8[0];
			src8 += 2;
			len -= 2;

			// skip FFFF signature
			if (code == 0xFF) {
				continue;
			}

			if (code == 0xFC) {
				// FFFC symbol definition
				if (len < 10)
					return false;

				ATProgramBlock& block = blocks.push_back();
				block.mImageOffset = blockImageOffset;
				block.mBaseAddress = VDReadUnalignedLEU16(src8 + 10);
				block.mLength = 10;
				
				src8 += 10;
				len -= 10;
			} else if (code == 0xFB || code == 0xFD) {
				// FFFB symbol reference / FFFD relocation fixup

				if (code == 0xFB) {
					if (len < 8)
						return false;

					len -= 8;
					src8 += 8;
				}

				ATProgramBlock block {};
				block.mImageOffset = blockImageOffset;
				block.mBaseAddress = 0;

				for(;;) {
					if (!len)
						return false;

					uint8 c = *src8++;
					--len;

					if (c == 0xFC)
						break;

					if (c == 0xFD) {
						if (len < 2)
							return false;

						len -= 2;
						src8 += 2;
					}

					if (c == 0xFE) {
						if (!len)
							return false;

						--len;
						++src8;
					}
				}

				block.mLength = (src8 - (const uint8 *)src) - block.mImageOffset;
				blocks.push_back(block);
			} else if (code == 0xFE) {
				// FFFE relocatable block
				if (len < 6)
					return false;

				ATProgramBlock& block = blocks.push_back();
				block.mImageOffset = blockImageOffset;
				block.mBaseAddress = VDReadUnalignedLEU16(src8 + 2);
				block.mLength = VDReadUnalignedLEU16(src8 + 4);

				src8 += 6;
				len -= 6;

				if (len < block.mLength) {
					blocks.pop_back();
					return false;
				}

				src8 += block.mLength;
				len -= block.mLength;
				continue;
			}
		}

		if (len < 4)
			return false;

		const uint16 segmentLo = VDReadUnalignedLEU16(src8);
		const uint16 segmentHi = VDReadUnalignedLEU16(src8 + 2);
		src8 += 4;
		len -= 4;

		ATProgramBlock block {};
		block.mImageOffset = blockImageOffset;
		block.mBaseAddress = segmentLo;

		if (segmentHi >= segmentLo) {
			block.mLength = (uint32)(segmentHi - segmentLo) + 1;
			block.mType = ATProgramBlockType::DOS;

			if (len < block.mLength)
				return false;

			len -= block.mLength;
			src8 += block.mLength;

			blocks.push_back(block);
		} else {
			block.mLength = 0;
			block.mType = ATProgramBlockType::Error;
			blocks.push_back(block);

			return false;
		}
	
	}

	return len == 0;
}

bool ATCheckProgramOverlap(uint32 start, uint32 len, const vdvector_view<const ATProgramBlock>& blocks) {
	if (!len)
		return false;

	const uint32 end = start + len;

	for(const ATProgramBlock& block : blocks) {
		if (block.mType == ATProgramBlockType::DOS) {
			if (block.mLength) {
				if (block.mBaseAddress < end && start < (uint32)block.mBaseAddress + block.mLength)
					return true;

				// check if we hit an INIT segment -- after this the memory
				// map may have changed and we must stop checking
				if (block.mBaseAddress < 0x02E4 && 0x02E2 < block.mBaseAddress + block.mLength)
					return false;
			}
		}
	}

	return false;
}
