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

#ifndef f_GTIA_SSE2_INTRIN_INL
#define f_GTIA_SSE2_INTRIN_INL

#include <intrin.h>
#include <at/atcore/intrin_sse2.h>

#ifdef VD_CPU_X64
void atasm_update_playfield_160_sse2(void *dst0, const uint8 *src, uint32 n) {
	alignas(16) static const uint64 lowbit_mask[2] = { 0x0f0f0f0f0f0f0f0f, 0x0f0f0f0f0f0f0f0f };

	// load and preshuffle color table
	const __m128i pfMask = *(const __m128i *)lowbit_mask;

	if (!n)
		return;

	char *dst = (char *)dst0;
	const uint8 *srcEnd = src + n;
		
	// check if we have a starting source offset and remove it
	uint32 startOffset = (uint32)(uintptr)src & 15;
	uint32 endOffset = (uint32)(uintptr)srcEnd & 15;

	dst -= startOffset * 2;
	src -= startOffset;

	// check if we have overlapping start and stop masks
	if (!(((uintptr)src ^ (uintptr)srcEnd) & ~(uintptr)15)) {
		__m128i mask = _mm_and_si128(ATIntrinGetStartMask_SSE2(startOffset), ATIntrinGetEndMask_SSE2(endOffset));

		__m128i mask1 = _mm_unpacklo_epi8(mask, mask);
		__m128i mask2 = _mm_unpackhi_epi8(mask, mask);

		const __m128i anxData = *(const __m128i *)src;

		const __m128i evenColorCodes = _mm_and_si128(_mm_srli_epi32(anxData, 4), pfMask);
		const __m128i  oddColorCodes = _mm_and_si128(               anxData    , pfMask);

		ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenColorCodes, oddColorCodes), mask1, dst);
		ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenColorCodes, oddColorCodes), mask2, dst+16);
	} else {
		// process initial oword
		if (startOffset) {
			__m128i mask = ATIntrinGetStartMask_SSE2(startOffset);

			__m128i mask1 = _mm_unpacklo_epi8(mask, mask);
			__m128i mask2 = _mm_unpackhi_epi8(mask, mask);

			const __m128i anxData = *(const __m128i *)src;
			src += 16;

			const __m128i evenColorCodes = _mm_and_si128(_mm_srli_epi32(anxData, 4), pfMask);
			const __m128i  oddColorCodes = _mm_and_si128(               anxData    , pfMask);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenColorCodes, oddColorCodes), mask1, dst);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenColorCodes, oddColorCodes), mask2, dst+16);
			dst += 32;
		}

		// process main owords
		ptrdiff_t byteCounter = srcEnd - src - 16;

		while(byteCounter >= 0) {
			const __m128i anxData = *(const __m128i *)src;
			src += 16;

			const __m128i evenColorCodes = _mm_and_si128(_mm_srli_epi32(anxData, 4), pfMask);
			const __m128i  oddColorCodes = _mm_and_si128(               anxData    , pfMask);

			// double-up and write
			*(__m128i *)(dst + 0)  = _mm_unpacklo_epi8(evenColorCodes, oddColorCodes);
			*(__m128i *)(dst + 16) = _mm_unpackhi_epi8(evenColorCodes, oddColorCodes);
			dst += 32;

			byteCounter -= 16;
		}

		// process final oword
		byteCounter &= 15;
		if (byteCounter) {
			__m128i mask = ATIntrinGetEndMask_SSE2(endOffset);
			__m128i mask1 = _mm_unpacklo_epi8(mask, mask);
			__m128i mask2 = _mm_unpackhi_epi8(mask, mask);

			const __m128i anxData = *(const __m128i *)src;
			const __m128i evenColorCodes = _mm_and_si128(_mm_srli_epi32(anxData, 4), pfMask);
			const __m128i  oddColorCodes = _mm_and_si128(               anxData    , pfMask);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(evenColorCodes, oddColorCodes), mask1, dst);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(evenColorCodes, oddColorCodes), mask2, dst+16);
		}
	}
}
#endif

void atasm_update_playfield_320_sse2(void *dstpri0, void *dsthires0, const uint8 *src0, uint32 x, uint32 n) {
	if (!n)
		return;

	// align start pointers
	uint32 startOffset = (uint32)(uintptr)src0 & 15;
	uint32 endOffset = (uint32)(uintptr)(src0 + n) & 15;
	__m128i *VDRESTRICT dstpri16 = (__m128i *)((char *)dstpri0 + x - startOffset * 2);
	__m128i *VDRESTRICT dsthires16 = (__m128i *)((char *)dsthires0 + x - startOffset * 2);
	const __m128i *VDRESTRICT src = (const __m128i *)(src0 - startOffset);

	// compute start/end masks
	const __m128i startMask = ATIntrinGetStartMask_SSE2(startOffset);
	const __m128i endMask = ATIntrinGetEndMask_SSE2(endOffset);

	// check for overlap
	const __m128i pf2x16 = _mm_set1_epi8(PF2);
	const __m128i x03b = _mm_set1_epi8(0x03);
	if (!(((uintptr)src0 ^ (uintptr)(src0 + n)) & ~(uintptr)15)) {
		const __m128i startEndMask = _mm_and_si128(startMask, endMask);
		const __m128i startEndMask1 = _mm_unpacklo_epi8(startEndMask, startEndMask);
		const __m128i startEndMask2 = _mm_unpackhi_epi8(startEndMask, startEndMask);

		// write PF2 to priority map
		ATMaskedWrite_SSE2(pf2x16, startEndMask1, dstpri16);
		ATMaskedWrite_SSE2(pf2x16, startEndMask2, dstpri16 + 1);

		// write data to hires map
		const __m128i hdat = *src;
		const __m128i hdatl = _mm_and_si128(_mm_srli_epi16(hdat, 2), x03b);
		const __m128i hdath = _mm_and_si128(hdat, x03b);

		ATMaskedWrite_SSE2(_mm_unpacklo_epi8(hdatl, hdath), startEndMask1, dsthires16);
		ATMaskedWrite_SSE2(_mm_unpackhi_epi8(hdatl, hdath), startEndMask2, dsthires16 + 1);
	} else {
		uint32 n16 = (n - endOffset + startOffset) >> 4;

		if (startOffset) {
			ATMaskedWrite_SSE2(pf2x16, _mm_unpacklo_epi8(startMask, startMask), dstpri16);
			ATMaskedWrite_SSE2(pf2x16, _mm_unpackhi_epi8(startMask, startMask), dstpri16 + 1);
			dstpri16 += 2;
			--n16;
		}

		for(uint32 i=0; i<n16; ++i) {
			dstpri16[0] = pf2x16;
			dstpri16[1] = pf2x16;
			dstpri16 += 2;
		}

		if (endOffset) {
			ATMaskedWrite_SSE2(pf2x16, _mm_unpacklo_epi8(endMask, endMask), dstpri16);
			ATMaskedWrite_SSE2(pf2x16, _mm_unpackhi_epi8(endMask, endMask), dstpri16 + 1);
		}

		// process initial oword
		if (startOffset) {
			const __m128i hdata = *src++;
			const __m128i hdatl = _mm_and_si128(_mm_srli_epi16(hdata, 2), x03b);
			const __m128i hdath = _mm_and_si128(hdata, x03b);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(hdatl, hdath), _mm_unpacklo_epi8(startMask, startMask), dsthires16);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(hdatl, hdath), _mm_unpackhi_epi8(startMask, startMask), dsthires16+1);
			dsthires16 += 2;
		}

		// process main owords
		for(uint32 i=0; i<n16; ++i) {
			const __m128i hdata = *src++;
			const __m128i hdatl = _mm_and_si128(_mm_srli_epi16(hdata, 2), x03b);
			const __m128i hdath = _mm_and_si128(hdata, x03b);

			dsthires16[0] = _mm_unpacklo_epi8(hdatl, hdath);
			dsthires16[1] = _mm_unpackhi_epi8(hdatl, hdath);
			dsthires16 += 2;
		}

		// process final oword
		if (endOffset) {
			const __m128i hdata = *src;
			const __m128i hdatl = _mm_and_si128(_mm_srli_epi16(hdata, 2), x03b);
			const __m128i hdath = _mm_and_si128(hdata, x03b);

			ATMaskedWrite_SSE2(_mm_unpacklo_epi8(hdatl, hdath), _mm_unpacklo_epi8(endMask, endMask), dsthires16);
			ATMaskedWrite_SSE2(_mm_unpackhi_epi8(hdatl, hdath), _mm_unpackhi_epi8(endMask, endMask), dsthires16+1);
		}
	}
}

#endif
