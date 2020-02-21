//	Altirra - Atari 800/800XL/5200 emulator
//	PAL artifacting acceleration - SSE2 intrinsics
//	Copyright (C) 2009-2011 Avery Lee
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
#include <intrin.h>
#include <emmintrin.h>

void ATArtifactBlend_SSE2(uint32 *dst, const uint32 *src, uint32 n) {
	      __m128i *VDRESTRICT dst16 = (      __m128i *)dst;
	const __m128i *VDRESTRICT src16 = (const __m128i *)src;

	uint32 n2 = n >> 2;

	while(n2--) {
		*dst16 = _mm_avg_epu8(*dst16, *src16);
		++dst16;
		++src16;
	}
}

void ATArtifactBlendExchange_SSE2(uint32 *dst, uint32 *blendDst, uint32 n) {
	__m128i *VDRESTRICT blendDst16 = (__m128i *)blendDst;
	__m128i *VDRESTRICT dst16      = (__m128i *)dst;

	uint32 n2 = n >> 2;

	while(n2--) {
		const __m128i x = *dst16;
		const __m128i y = *blendDst16;

		*blendDst16++ = x;
		*dst16++ = _mm_avg_epu8(x, y);
	}
}

template<int T_Intensity>
void ATArtifactBlendScanlinesN_SSE2(uint32 *dst0, const uint32 *src10, const uint32 *src20, uint32 n) {
	__m128i *VDRESTRICT dst = (__m128i *)dst0;
	const __m128i *VDRESTRICT src1 = (const __m128i *)src10;
	const __m128i *VDRESTRICT src2 = (const __m128i *)src20;
	const uint32 n4 = n >> 2;
	const __m128i zero = _mm_setzero_si128();

	for(uint32 i=0; i<n4; ++i) {
		if constexpr (T_Intensity == 0) {
			*dst++ = zero;
		} else {
			__m128i r = _mm_avg_epu8(*src1++, *src2++);

			if constexpr (T_Intensity == 1) {
				*dst++ = _mm_avg_epu8(_mm_avg_epu8(_mm_avg_epu8(r, zero), zero), zero);
			} else if constexpr (T_Intensity == 2) {
				*dst++ = _mm_avg_epu8(_mm_avg_epu8(r, zero), zero);
			} else if constexpr (T_Intensity == 3) {
				__m128i r4 = _mm_avg_epu8(r, zero);
				__m128i r2 = _mm_avg_epu8(r4, zero);
				*dst++ = _mm_avg_epu8(r2, r4);
			} else if constexpr (T_Intensity == 4) {
				*dst++ = _mm_avg_epu8(r, zero);
			} else if constexpr (T_Intensity == 5) {
				__m128i r4 = _mm_avg_epu8(r, zero);
				__m128i r6 = _mm_avg_epu8(r4, r);
				*dst++ = _mm_avg_epu8(r6, r4);
			} else if constexpr (T_Intensity == 6) {
				*dst++ = _mm_avg_epu8(r, _mm_avg_epu8(r, zero));
			} else if constexpr (T_Intensity == 7) {
				__m128i r4 = _mm_avg_epu8(r, zero);
				__m128i r6 = _mm_avg_epu8(r4, r);
				*dst++ = _mm_avg_epu8(r, r6);
			} else if constexpr (T_Intensity == 8) {
				*dst++ = r;
			}
		}
	}
}

void ATArtifactBlendScanlines_SSE2(uint32 *dst, const uint32 *src1, const uint32 *src2, uint32 n, float intensity) {
	int intensity8 = (int)(intensity * 8 + 0.5f);
	switch(intensity8) {
		case 1: ATArtifactBlendScanlinesN_SSE2<1>(dst, src1, src2, n); break;
		case 2: ATArtifactBlendScanlinesN_SSE2<2>(dst, src1, src2, n); break;
		case 3: ATArtifactBlendScanlinesN_SSE2<3>(dst, src1, src2, n); break;
		case 4: ATArtifactBlendScanlinesN_SSE2<4>(dst, src1, src2, n); break;
		case 5: ATArtifactBlendScanlinesN_SSE2<5>(dst, src1, src2, n); break;
		case 6: ATArtifactBlendScanlinesN_SSE2<6>(dst, src1, src2, n); break;
		case 7: ATArtifactBlendScanlinesN_SSE2<7>(dst, src1, src2, n); break;

		default:
			if (intensity < 0.5f)
				ATArtifactBlendScanlinesN_SSE2<0>(dst, src1, src2, n);
			else
				ATArtifactBlendScanlinesN_SSE2<8>(dst, src1, src2, n);
			break;
	}
}

void ATArtifactNTSCFinal_SSE2(void *dst0, const void *srcr0, const void *srcg0, const void *srcb0, uint32 count) {
	const __m128i *VDRESTRICT srcr = (const __m128i *)srcr0;
	const __m128i *VDRESTRICT srcg = (const __m128i *)srcg0;
	const __m128i *VDRESTRICT srcb = (const __m128i *)srcb0;
	__m128i *VDRESTRICT dst = (__m128i *)dst0;
	uint32 n8 = count >> 3;

	do {
		const __m128i r8 = *srcr++;
		const __m128i g8 = *srcg++;
		const __m128i b8 = *srcb++;

		const __m128i rb8a = _mm_unpacklo_epi8(b8, r8);
		const __m128i rb8b = _mm_unpackhi_epi8(b8, r8);
		const __m128i gg8a = _mm_unpacklo_epi8(g8, g8);
		const __m128i gg8b = _mm_unpackhi_epi8(g8, g8);

		*dst++ = _mm_unpacklo_epi8(rb8a, gg8a);
		*dst++ = _mm_unpackhi_epi8(rb8a, gg8a);
		*dst++ = _mm_unpacklo_epi8(rb8b, gg8b);
		*dst++ = _mm_unpackhi_epi8(rb8b, gg8b);
	} while(--n8);
}
