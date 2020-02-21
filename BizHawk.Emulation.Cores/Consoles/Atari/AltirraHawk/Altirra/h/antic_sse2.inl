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

#ifndef f_AT_ANTIC_SSE2_INL
#define f_AT_ANTIC_SSE2_INL

#include <intrin.h>
#include <at/atcore/intrin_sse2.h>

void ATAnticSetDMACycles_SSE2(void *dst0, uint32 start, uint32 end, uint8 cyclePattern, uint8 dmaMask) {
	__m128i *VDRESTRICT dst = (__m128i *)dst0;

	const auto vset1ub = [](const uint8 val) -> __m128i {
		__m128i v = _mm_cvtsi32_si128(val);
		v = _mm_unpacklo_epi8(v, v);
		v = _mm_shufflelo_epi16(v, 0);
		return _mm_unpacklo_epi64(v, v);
	};

	__m128i vinc =_mm_set1_epi8(16);
	__m128i vstart = vset1ub(start);
	__m128i vlimit = vset1ub(end - start - 0x80);
	__m128i vindices = _mm_sub_epi8(_mm_set_epi8(-0x71,-0x72,-0x73,-0x74,-0x75,-0x76,-0x77,-0x78,-0x79,-0x7A,-0x7B,-0x7C,-0x7D,-0x7E,-0x7F,-0x80), vstart);
	__m128i vbits = _mm_set_epi8(-0x80,0x40,0x20,0x10,0x08,0x04,0x02,0x01,-0x80,0x40,0x20,0x10,0x08,0x04,0x02,0x01);
	__m128i vnCycleMask = _mm_cmpeq_epi8(_mm_and_si128(vset1ub(cyclePattern), vbits), _mm_setzero_si128());
	__m128i vpat = _mm_andnot_si128(vnCycleMask, vset1ub(dmaMask));

	for(int i=0; i<8; ++i) {
		__m128i valid = _mm_cmpgt_epi8(vlimit, vindices);	// (limit > i) => (i < limit)

		*dst = _mm_or_si128(*dst, _mm_and_si128(valid, vpat));
		++dst;

		vindices = _mm_add_epi8(vindices, vinc);
	}
}

inline void ATAnticSetRefreshCycles_SSE2(uint8 *dmaPattern) {
	__m128i x01b = _mm_set1_epi8(0x01);
	__m128i x01d = _mm_set1_epi32(0x01);
	__m128i cyc0 = _mm_loadu_si128((const __m128i *)(dmaPattern + 25));
	__m128i cyc1 = _mm_loadu_si128((const __m128i *)(dmaPattern + 41));

	cyc0 =_mm_or_si128(cyc0, _mm_and_si128(_mm_add_epi32(_mm_cmpeq_epi8(_mm_and_si128(cyc0, x01b), x01b), x01d), x01b));
	cyc1 =_mm_or_si128(cyc1, _mm_and_si128(_mm_add_epi32(_mm_cmpeq_epi8(_mm_and_si128(cyc1, x01b), x01b), x01d), x01b));

	_mm_storeu_si128((__m128i *)(dmaPattern + 25), cyc0);
	_mm_storeu_si128((__m128i *)(dmaPattern + 41), cyc1);

	// For the very last cycle starting on 57, scan for the next available free cycle. This
	// starts in the middle of oword 3.
	const __m128i *dp16 = (const __m128i *)dmaPattern + 3;
	uint32 imask = 0x1FF;

	for(int i=3; i<8; ++i) {
		uint16 freeMask = ~(_mm_movemask_epi8(_mm_slli_epi16(*dp16, 7)) | imask);

		if (freeMask) {
			((uint8 *)dp16)[_tzcnt_u32(freeMask)] |= 0x01;
			break;
		}

		imask = 0;
		++dp16;
	}
}

inline void ATAnticClearDMACycles_SSE2(uint8 *dmaPattern) {
	__m128i *VDRESTRICT dst = (__m128i *)dmaPattern;
	__m128i zero = _mm_setzero_si128();

	dst[0] = zero;
	dst[1] = zero;
	dst[2] = zero;
	dst[3] = zero;
	dst[4] = zero;
	dst[5] = zero;
	dst[6] = zero;
	dst[7] = zero;
}

inline uint32 ATAnticDecodeMode2_SSE2(uint8 *dst, const uint8 *nameData, const uint8 *charData, uint32 x, uint32 limit, uint8 invMask, uint8 blinkMask) {
	__m128i zero = _mm_setzero_si128();
	__m128i vInvMask = _mm_shufflelo_epi16(_mm_cmpgt_epi32(_mm_cvtsi32_si128(invMask), zero), 0);
	__m128i vBlinkMask = _mm_shufflelo_epi16(_mm_cmpeq_epi32(_mm_cvtsi32_si128(blinkMask), zero), 0);
	__m128i x0Fb = _mm_set1_epi8(0x0F);
	__m128i *VDRESTRICT dst16 = (__m128i *)dst;

	uint32 numChars = (limit - x + 1) >> 1;
	x += numChars * 2;

	uint32 n8 = (numChars + 7) >> 3;
	while(n8--) {
		__m128i vNames = _mm_loadl_epi64((const __m128i *)nameData);
		nameData += 8;

		__m128i vChars = _mm_loadl_epi64((const __m128i *)charData);
		charData += 8;

		// expand name bit 7 to byte masks
		__m128i vHiBitMask = _mm_cmpgt_epi8(zero, vNames);

		// apply blink
		__m128i vGraphicData = _mm_andnot_si128(_mm_and_si128(vHiBitMask, vBlinkMask), vChars);

		// apply invert
		vGraphicData = _mm_xor_si128(vGraphicData, _mm_and_si128(vHiBitMask, vInvMask));

		// split into lo/hi and write
		__m128i vLoBits = _mm_and_si128(vGraphicData, x0Fb);
		__m128i vHiBits = _mm_and_si128(_mm_srli_epi16(vGraphicData, 4), x0Fb);

		_mm_storeu_si128(dst16, _mm_unpacklo_epi8(vHiBits, vLoBits));
		++dst16;
	}

	// check if we must trim off the last word -- we can safely overwrite but need to leave
	// zeroes
	if (numChars & 7) {
		__m128i finalMask = ATIntrinGetEndMask_SSE2((numChars & 7) * 2);

		_mm_storeu_si128(dst16 - 1, _mm_and_si128(_mm_loadu_si128(dst16 - 1), finalMask));
	}

	return x;
}

#endif
