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

#ifndef f_GTIARENDERER_SSSE3_INTRIN_INL
#define f_GTIARENDERER_SSSE3_INTRIN_INL

#include <intrin.h>
#include <at/atcore/intrin_sse2.h>

namespace nsATGTIARenderer {
	// We do unaligned loads from this array, so it's important that we
	// avoid data cache unit (DCU) split penalties on older CPUs. Minimum
	// for SSSE3 is Core 2, so we can assume at least 64 byte cache lines.
	const __declspec(align(64)) uint64 window_table[6] = {
		0, 0, (uint64)0 - 1, (uint64)0 - 1, 0, 0
	};

	const __declspec(align(16)) uint8 color_table_preshuffle[16] = { 8, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7 };
	const __declspec(align(16)) uint64 hires_splat_pf1[2] = { 0x0505050505050505, 0x0505050505050505 };
	const __declspec(align(16)) uint64 hires_mask_1[2] = { 0x0f000f000f000f00, 0x0f000f000f000f00 };
	const __declspec(align(16)) uint64 hires_mask_2[2] = { 0x0f0f00000f0f0000, 0x0f0f00000f0f0000 };
}

void atasm_gtia_render_lores_fast_ssse3(void *dst0, const uint8 *src, uint32 n, const uint8 *color_table) {
	using namespace nsATGTIARenderer;

	// load and preshuffle color table
	const __m128i colorTable = _mm_shuffle_epi8(*(const __m128i *)color_table, *(const __m128i *)color_table_preshuffle);

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

		__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingMaskOffset));
		__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingMaskOffset + 8));
		__m128i mask3 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingMaskOffset));
		__m128i mask4 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingMaskOffset + 8));

		mask1 = _mm_and_si128(mask1, mask3);
		mask2 = _mm_and_si128(mask2, mask4);

		mask1 = _mm_unpacklo_epi8(mask1, mask1);
		mask2 = _mm_unpacklo_epi8(mask2, mask2);

		const __m128i paletteIndices = _mm_shuffle_epi8(colorTable, *(const __m128i *)src);

		ATMaskedWrite_SSE2(_mm_unpacklo_epi8(paletteIndices, paletteIndices), mask1, dst);
		ATMaskedWrite_SSE2(_mm_unpackhi_epi8(paletteIndices, paletteIndices), mask2, dst+16);
	} else {
		// process initial oword
		if (startOffset) {
			ptrdiff_t startingMaskOffset = 16 - startOffset;

			__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingMaskOffset));
			__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingMaskOffset + 8));

			mask1 = _mm_unpacklo_epi8(mask1, mask1);
			mask2 = _mm_unpacklo_epi8(mask2, mask2);

			const __m128i paletteIndices = _mm_shuffle_epi8(colorTable, *(const __m128i *)src);
			src += 16;

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(paletteIndices, paletteIndices), mask1, dst);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(paletteIndices, paletteIndices), mask2, dst+16);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const __m128i paletteIndices = _mm_shuffle_epi8(colorTable, *(const __m128i *)src);
			src += 16;

			// double-up and write
			*(__m128i *)(dst + 0) = _mm_unpacklo_epi8(paletteIndices, paletteIndices);
			*(__m128i *)(dst + 16) = _mm_unpackhi_epi8(paletteIndices, paletteIndices);
			dst += 32;

			byteCounter -= 16;
		}

		// process final oword
		byteCounter &= 15;
		if (byteCounter) {
			ptrdiff_t endingMaskOffset = 32 - byteCounter;

			__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingMaskOffset));
			__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingMaskOffset + 8));

			mask1 = _mm_unpacklo_epi8(mask1, mask1);
			mask2 = _mm_unpacklo_epi8(mask2, mask2);

			const __m128i paletteIndices = _mm_shuffle_epi8(colorTable, *(const __m128i *)src);
			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(paletteIndices, paletteIndices), mask1, dst);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(paletteIndices, paletteIndices), mask2, dst+16);
		}
	}
}

void atasm_gtia_render_mode8_fast_ssse3(
	void *dst0,
	const uint8 *src,
	const uint8 *hiressrc,
	uint32 n,
	const uint8 *color_table)
{
	using namespace nsATGTIARenderer;

	char *dst = (char *)dst0;

	// precook color tables
	const __m128i playfieldColorTable = _mm_shuffle_epi8(*(const __m128i *)color_table, *(const __m128i *)color_table_preshuffle);
	const __m128i pf1ColorTable = _mm_shuffle_epi8(*(const __m128i *)color_table, *(const __m128i *)hires_splat_pf1);
	const __m128i evenMask = *(const __m128i *)hires_mask_2;
	const __m128i  oddMask = *(const __m128i *)hires_mask_1;
	const __m128i evenPixelTable = _mm_or_si128(_mm_and_si128(evenMask, pf1ColorTable), _mm_andnot_si128(evenMask, playfieldColorTable));
	const __m128i  oddPixelTable = _mm_or_si128(_mm_and_si128( oddMask, pf1ColorTable), _mm_andnot_si128( oddMask, playfieldColorTable));

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

		__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingWindowMask));
		__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingWindowMask + 8));
		__m128i mask3 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingWindowMask));
		__m128i mask4 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingWindowMask + 8));

		mask1 = _mm_and_si128(mask1, mask3);
		mask2 = _mm_and_si128(mask2, mask4);

		mask1 = _mm_unpacklo_epi8(mask1, mask1);
		mask2 = _mm_unpacklo_epi8(mask2, mask2);

		const __m128i combinedData = _mm_or_si128(*(const __m128i *)src, *(const __m128i *)hiressrc);
		const __m128i evenPixels = _mm_shuffle_epi8(evenPixelTable, combinedData);
		const __m128i  oddPixels = _mm_shuffle_epi8( oddPixelTable, combinedData);

		ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenPixels, oddPixels), mask1, dst +  0);
		ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenPixels, oddPixels), mask2, dst + 16);
	} else {
		// process starting oword
		if (startOffset) {
			ptrdiff_t startingWindowOffset = 16 - startOffset;

			__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingWindowOffset));
			__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + startingWindowOffset + 8));

			mask1 = _mm_unpacklo_epi8(mask1, mask1);
			mask2 = _mm_unpacklo_epi8(mask2, mask2);

			const __m128i combinedData = _mm_or_si128(*(const __m128i *)src, *(const __m128i *)hiressrc);
			src += 16;
			hiressrc += 16;

			const __m128i evenPixels = _mm_shuffle_epi8(evenPixelTable, combinedData);
			const __m128i  oddPixels = _mm_shuffle_epi8( oddPixelTable, combinedData);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenPixels, oddPixels), mask1, dst +  0);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenPixels, oddPixels), mask2, dst + 16);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const __m128i combinedData = _mm_or_si128(*(const __m128i *)src, *(const __m128i *)hiressrc);
			src += 16;
			hiressrc += 16;

			const __m128i evenPixels = _mm_shuffle_epi8(evenPixelTable, combinedData);
			const __m128i  oddPixels = _mm_shuffle_epi8( oddPixelTable, combinedData);

			*(__m128i *)(dst +  0) = _mm_unpacklo_epi8(evenPixels, oddPixels);
			*(__m128i *)(dst + 16) = _mm_unpackhi_epi8(evenPixels, oddPixels);
			dst += 32;
			byteCounter -= 16;
		}

		byteCounter &= 15;
		if (byteCounter) {
			const ptrdiff_t endingWindowOffset = 32 - byteCounter;

			__m128i mask1 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingWindowOffset));
			__m128i mask2 = _mm_loadl_epi64((const __m128i *)((const uint8 *)window_table + endingWindowOffset + 8));

			mask1 = _mm_unpacklo_epi8(mask1, mask1);
			mask2 = _mm_unpacklo_epi8(mask2, mask2);

			const __m128i combinedData = _mm_or_si128(*(const __m128i *)src, *(const __m128i *)hiressrc);
			const __m128i evenPixels = _mm_shuffle_epi8(evenPixelTable, combinedData);
			const __m128i  oddPixels = _mm_shuffle_epi8( oddPixelTable, combinedData);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenPixels, oddPixels), mask1, dst +  0);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenPixels, oddPixels), mask2, dst + 16);
		}	
	}
}

#endif
