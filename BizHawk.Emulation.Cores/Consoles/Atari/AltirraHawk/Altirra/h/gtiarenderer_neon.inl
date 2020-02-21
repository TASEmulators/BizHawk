//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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

#ifndef f_GTIARENDERER_NEON_INL
#define f_GTIARENDERER_NEON_INL

#include <at/atcore/intrin_neon.h>
#include <arm64_neon.h>

namespace nsATGTIARenderer {
	// We do unaligned loads from this array, so it's important that we
	// avoid cache line split penalties on older CPUs. The Cortex-A73
	// uses 64-byte lines for loads (stores are only 16).
	const __declspec(align(64)) uint64 window_table[6] = {
		0, 0, (uint64)0 - 1, (uint64)0 - 1, 0, 0
	};

	const __declspec(align(16)) uint8 color_table_preshuffle[16] = { 8, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7 };
	const __declspec(align(16)) uint64 hires_splat_pf1[2] = { 0x0505050505050505, 0x0505050505050505 };
	const __declspec(align(16)) uint64 hires_mask_1[2] = { 0x0f000f000f000f00, 0x0f000f000f000f00 };
	const __declspec(align(16)) uint64 hires_mask_2[2] = { 0x0f0f00000f0f0000, 0x0f0f00000f0f0000 };
}

void atasm_gtia_render_lores_fast_neon(void *dst0, const uint8 *src, uint32 n, const uint8 *color_table) {
	using namespace nsATGTIARenderer;

	// load and preshuffle color table
	const uint8x16_t colorTable = vqtbl1q_u8(*(const uint8x16_t *)color_table, *(const uint8x16_t *)color_table_preshuffle);

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
		uint8x16_t mask2 = vld1q_u8((const uint8x16_t *)((const uint8 *)window_table + endingMaskOffset));

		uint8x16_t mask = vandq_u8(mask1, mask2);

		const uint8x16_t paletteIndices = vqtbl1q_u8(colorTable, *(const uint8x16_t *)src);

		uint8x16x2_t d = vld2q_u8(dst);
		d.val[0] = vbslq_u8(mask, paletteIndices, d.val[0]);
		d.val[1] = vbslq_u8(mask, paletteIndices, d.val[1]);
		vst2q_u8(dst, d);
	} else {
		// process initial oword
		if (startOffset) {
			ptrdiff_t startingMaskOffset = 16 - startOffset;

			uint8x16_t mask = vld1q_u8((const uint8x16_t *)((const uint8 *)window_table + startingMaskOffset));

			const uint8x16_t paletteIndices = vqtbl1q_u8(colorTable, *(const uint8x16_t *)src);
			src += 16;

			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, paletteIndices, d.val[0]);
			d.val[1] = vbslq_u8(mask, paletteIndices, d.val[1]);
			vst2q_u8(dst, d);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const uint8x16_t paletteIndices = vqtbl1q_u8(colorTable, *(const uint8x16_t *)src);
			src += 16;

			// double-up and write
			uint8x16x2_t paletteIndices2x { { paletteIndices, paletteIndices } };
			vst2q_u8(dst, paletteIndices2x);
			dst += 32;

			byteCounter -= 16;
		}

		// process final oword
		byteCounter &= 15;
		if (byteCounter) {
			ptrdiff_t endingMaskOffset = 32 - byteCounter;

			uint8x16_t mask = vld1q_u8((const uint8x16_t *)((const uint8 *)window_table + endingMaskOffset));

			const uint8x16_t paletteIndices = vqtbl1q_u8(colorTable, *(const uint8x16_t *)src);
			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, paletteIndices, d.val[0]);
			d.val[1] = vbslq_u8(mask, paletteIndices, d.val[1]);
			vst2q_u8(dst, d);
		}
	}
}

void atasm_gtia_render_mode8_fast_neon(
	void *dst0,
	const uint8 *src,
	const uint8 *hiressrc,
	uint32 n,
	const uint8 *color_table)
{
	using namespace nsATGTIARenderer;

	char *dst = (char *)dst0;

	// precook color tables
	const uint8x16_t colorTable = vld1q_u8(color_table);
	const uint8x16_t playfieldColorTable = vqtbl1q_u8(colorTable, vld1q_u8(color_table_preshuffle));
	const uint8x16_t pf1ColorTable = vqtbl1q_u8(colorTable, vld1q_u8(hires_splat_pf1));
	const uint8x16_t evenMask = vld1q_u8(hires_mask_2);
	const uint8x16_t  oddMask = vld1q_u8(hires_mask_1);
	const uint8x16_t evenPixelTable = vbslq_u8(evenMask, pf1ColorTable, playfieldColorTable);
	const uint8x16_t  oddPixelTable = vbslq_u8(oddMask, pf1ColorTable, playfieldColorTable);

	if (!n)
		return;

	const uint8 *srcEnd = src + n;
		
	// check if we have and remove the start offset
	uintptr startOffset = (uintptr)src & 15;

	dst -= 2*startOffset;
	src -= startOffset;
	hiressrc -= startOffset;
		
	// check if we have overlapping start and stop masks
	if (!(((uintptr)src ^ (uintptr)srcEnd) & ~(uintptr)15)) {
		const ptrdiff_t startingWindowMask = 16 - startOffset;
		const ptrdiff_t endingWindowMask = 32 - ((uintptr)srcEnd & 15);

		uint8x16_t mask1 = vld1q_u8((const uint8 *)window_table + startingWindowMask);
		uint8x16_t mask3 = vld1q_u8((const uint8 *)window_table + endingWindowMask);

		uint8x16_t mask = vandq_u8(mask1, mask3);

		const uint8x16_t combinedData = vorrq_u8(vld1q_u8(src), vld1q_u8(hiressrc));
		const uint8x16_t evenPixels = vqtbl1q_u8(evenPixelTable, combinedData);
		const uint8x16_t  oddPixels = vqtbl1q_u8( oddPixelTable, combinedData);

		uint8x16x2_t d = vld2q_u8(dst);
		d.val[0] = vbslq_u8(mask, evenPixels, d.val[0]);
		d.val[1] = vbslq_u8(mask, oddPixels, d.val[1]);
		vst2q_u8(dst, d);
	} else {
		// process starting oword
		if (startOffset) {
			ptrdiff_t startingWindowOffset = 16 - startOffset;

			uint8x16_t mask = vld1q_u8((const uint8 *)window_table + startingWindowOffset);

			const uint8x16_t combinedData = vorrq_u8(vld1q_u8(src), vld1q_u8(hiressrc));
			src += 16;
			hiressrc += 16;

			const uint8x16_t evenPixels = vqtbl1q_u8(evenPixelTable, combinedData);
			const uint8x16_t  oddPixels = vqtbl1q_u8( oddPixelTable, combinedData);

			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, evenPixels, d.val[0]);
			d.val[1] = vbslq_u8(mask, oddPixels, d.val[1]);
			vst2q_u8(dst, d);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const uint8x16_t combinedData = vorrq_u8(vld1q_u8(src), vld1q_u8(hiressrc));
			src += 16;
			hiressrc += 16;

			uint8x16x2_t d;
			d.val[0] = vqtbl1q_u8(evenPixelTable, combinedData);
			d.val[1] = vqtbl1q_u8( oddPixelTable, combinedData);
			vst2q_u8(dst, d);
			dst += 32;
			byteCounter -= 16;
		}

		byteCounter &= 15;
		if (byteCounter) {
			const ptrdiff_t endingWindowOffset = 32 - byteCounter;

			uint8x16_t mask = vld1q_u8((const uint8 *)window_table + endingWindowOffset);

			const uint8x16_t combinedData = vorrq_u8(vld1q_u8(src), vld1q_u8(hiressrc));
			const uint8x16_t evenPixels = vqtbl1q_u8(evenPixelTable, combinedData);
			const uint8x16_t  oddPixels = vqtbl1q_u8( oddPixelTable, combinedData);

			uint8x16x2_t d = vld2q_u8(dst);
			d.val[0] = vbslq_u8(mask, evenPixels, d.val[0]);
			d.val[1] = vbslq_u8(mask, oddPixels, d.val[1]);
			vst2q_u8(dst, d);
		}	
	}
}

#endif
