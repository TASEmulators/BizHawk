//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_GTIA_NEON_INTRIN_INL
#define f_GTIA_NEON_INTRIN_INL

#include <arm64_neon.h>

void atasm_update_playfield_160_neon(void *dst0, const uint8 *src, uint32 n) {
	// We do unaligned loads from this array, so it's important that we
	// avoid data cache unit (DCU) split penalties on older CPUs. Minimum
	// for SSE2 is Pentium 4, so we can assume at least 64 byte cache lines.
	alignas(64) static const uint64 window_table[6] = {
		0, 0, (uint64)0 - 1, (uint64)0 - 1, 0, 0
	};

	// load and preshuffle color table
	const uint8x16_t pfMask = vmovq_n_u16(0x0f0f);

	if (!n)
		return;

	char *dst = (char *)dst0;
	const uint8 *srcEnd = src + n;
		
	// check if we have a starting source offset and remove it
	uintptr startOffset = (uintptr)src & 15;

	dst -= startOffset * 2;
	src -= startOffset;

	// check if we have overlapping start and stop masks
	if (!(((uintptr)src ^ (uintptr)srcEnd) & ~(uintptr)15)) {
		ptrdiff_t startingMaskOffset = 16 - startOffset;
		ptrdiff_t endingMaskOffset = 32 - ((uintptr)srcEnd & 15);

		uint8x16_t mask1 = vld1q_u8((const uint8x16_t *)((const uint8 *)window_table + startingMaskOffset));
		uint8x16_t mask3 = vld1q_u8((const uint8x16_t *)((const uint8 *)window_table + endingMaskOffset));

		uint8x16_t mask = vandq_u8(mask1, mask3);

		const uint8x16_t anxData = *(const uint8x16_t *)src;

		const uint8x16_t evenColorCodes = vshrq_n_u8(anxData, 4);
		const uint8x16_t  oddColorCodes = vandq_u8(anxData, pfMask);

		uint8x16x2_t d = vld2q_u8(dst);
		d.val[0] = vbslq_u8(mask, evenColorCodes, d.val[0]);
		d.val[1] = vbslq_u8(mask, oddColorCodes, d.val[1]);
		vst2q_u8(dst, d);
	} else {
		// process initial oword
		if (startOffset) {
			ptrdiff_t startingMaskOffset = 16 - startOffset;

			uint8x16_t mask = vld1q_u8((const uint8 *)window_table + startingMaskOffset);

			const uint8x16_t anxData = vld1q_u8(src);
			src += 16;

			const uint8x16_t evenColorCodes = vshrq_n_u8(anxData, 4);
			const uint8x16_t  oddColorCodes = vandq_u8(anxData, pfMask);

			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, evenColorCodes, d.val[0]);
			d.val[1] = vbslq_u8(mask, oddColorCodes, d.val[1]);
			vst2q_u8(dst, d);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const uint8x16_t anxData = vld1q_u8(src);
			src += 16;

			uint8x16x2_t colorCodes;
			colorCodes.val[0] = vshrq_n_u8(anxData, 4);
			colorCodes.val[1] = vandq_u8(anxData, pfMask);

			// double-up and write
			vst2q_u8(dst, colorCodes);
			dst += 32;

			byteCounter -= 16;
		}

		// process final oword
		byteCounter &= 15;
		if (byteCounter) {
			ptrdiff_t endingMaskOffset = 32 - byteCounter;

			uint8x16_t mask = vld1q_u8((const uint8 *)window_table + endingMaskOffset);

			const uint8x16_t anxData = vld1q_u8(src);
			const uint8x16_t evenColorCodes = vshrq_n_u8(anxData, 4);
			const uint8x16_t  oddColorCodes = vandq_u8(anxData, pfMask);

			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, evenColorCodes, d.val[0]);
			d.val[1] = vbslq_u8(mask, oddColorCodes, d.val[1]);
			vst2q_u8(dst, d);
		}
	}
}

#endif
