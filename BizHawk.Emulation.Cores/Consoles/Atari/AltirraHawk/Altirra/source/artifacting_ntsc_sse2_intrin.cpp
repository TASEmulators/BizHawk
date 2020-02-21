//	Altirra - Atari 800/800XL/5200 emulator
//	PAL artifacting acceleration - x86 SSE2
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
#include "artifacting.h"

#if defined(VD_COMPILER_MSVC) && (defined(VD_CPU_X86) || defined(VD_CPU_X64))
bool ATArtifactingEngine::ArtifactNTSC_SSE2(uint32 *dst0, const uint8 *src0, uint32 num7MHzPixels) {
	// The max we support is 456 pixels, the number of 7MHz cycles per scanline.
	static constexpr uint32_t kMaxPixels = 456;

	if (num7MHzPixels > kMaxPixels) {
		VDASSERT(false);
		return false;
	}

	// For simplicity, we require 8-pixel alignment.
	if (num7MHzPixels & 7) {
		VDASSERT(false);
		return false;
	}

	alignas(16) union {
		uint8 b[(kMaxPixels + 15 + 32) & ~15];
		sint8 sb[(kMaxPixels + 15 + 32) & ~15];
		__m128i v[(kMaxPixels + 15 + 32) >> 4];
	} intensity, luma;

	const uint8 *VDRESTRICT src = src0;
	const __m128i *VDRESTRICT src16 = (const __m128i *)src;
	const uint32_t n16 = (num7MHzPixels + 15) >> 4;
	__m128i x0Fb = _mm_set1_epi8(0x0F);
	for(uint32_t i=0; i<n16; ++i) {
		intensity.v[i + 1] = _mm_and_si128(src16[i], x0Fb);
	}

	// splat first valid pixel backward
	intensity.v[0] = _mm_shuffle_epi32(_mm_shufflelo_epi16(_mm_unpacklo_epi8(intensity.v[1], intensity.v[1]), 0), 0);

	// splat partial last oword if necessary
	if (num7MHzPixels & 15) {
		__m128i v = intensity.v[n16];

		*(__m128 *)&intensity.v[n16] = _mm_movelh_ps(_mm_castsi128_ps(v), _mm_castsi128_ps(_mm_shufflelo_epi16(_mm_unpackhi_epi8(v, v), 0)));
	}

	// splat final value
	intensity.v[n16+1] = _mm_shuffle_epi32(_mm_shufflehi_epi16(_mm_unpackhi_epi8(intensity.v[n16+1], intensity.v[n16+1]), 0xFF), 0xFF);

	__m128i phaseInvert = _mm_set1_epi16((short)0xFF00);
	__m128i inv = _mm_cmpeq_epi8(phaseInvert, phaseInvert);
	__m128i zero = _mm_setzero_si128();
	__m128i xFEb = _mm_set1_epi8(-2);
	__m128i hasArtifacting = zero;
	for(uint32 i=0; i<n16; ++i) {
		__m128i v0 = intensity.v[i];
		__m128i v1 = intensity.v[i+1];
		__m128i v2 = intensity.v[i+2];

		__m128i x0 = _mm_or_si128(_mm_srli_si128(v0, 15), _mm_slli_si128(v1,1));
		__m128i x1 = v1;
		__m128i x2 = _mm_or_si128(_mm_srli_si128(v1, 1), _mm_slli_si128(v2,15));

		// compute artifacting strength as deviation from range of neighbors
		__m128i artifactSignal = _mm_sub_epi8(_mm_subs_epu8(x1, _mm_max_epu8(x0, x2)), _mm_subs_epu8(_mm_min_epu8(x0, x2), x1));

		// negate every other pixel
		artifactSignal = _mm_sub_epi8(_mm_xor_si128(artifactSignal, phaseInvert), phaseInvert);

		hasArtifacting = _mm_or_si128(hasArtifacting, artifactSignal);

		intensity.v[i] = artifactSignal;

		// compute averaged luma
		__m128i avg = _mm_avg_epu8(_mm_srli_epi16(_mm_and_si128(_mm_add_epi8(x0, x2), xFEb), 1), x1);

		// compute artifacting mask
		__m128i noArtifactingMask = _mm_cmpeq_epi8(artifactSignal, zero);

		// select averaged or non-averaged luma based on artifacting presence
		luma.v[i] = _mm_or_si128(_mm_and_si128(x1, noArtifactingMask), _mm_andnot_si128(noArtifactingMask, avg));
	}

	// check if there was any artifacting
	if (_mm_movemask_epi8(_mm_cmpeq_epi8(hasArtifacting, zero)) == 0xFFFF)
		return false;

	if (mbEnableColorCorrection || mbGammaIdentity || mbBypassOutputCorrection) {
		{
			uint32 *VDRESTRICT dst = dst0;

			for(uint32 x=0; x<num7MHzPixels; ++x) {
				uint8 p = src[x];
				int art = intensity.sb[x]; 

				if (!art) {
					*dst++ = mPalette[p];
				} else {
					int c = p >> 4;
			
					__m128i chroma = _mm_loadu_si64(&mChromaVectors[c][0]);
					int y = mLumaRamp[luma.b[x]];

					__m128i color = _mm_adds_epi16(chroma, _mm_shufflelo_epi16(_mm_cvtsi32_si128(y), 0));

					color = _mm_adds_epi16(color, _mm_loadl_epi64((const __m128i *)&mArtifactRamp[art + 15]));
					color = _mm_srai_epi16(color, 6);
					color = _mm_packus_epi16(color, color);

					*dst++ = _mm_cvtsi128_si32(color);
				}
			}
		}

		if (mbEnableColorCorrection && !mbBypassOutputCorrection)
			ColorCorrect((uint8 *)dst0, N);
	} else {
		const __m128i xFFw = _mm_set1_epi16(255);
		uint32 *VDRESTRICT dst = dst0;

		for(uint32 x=0; x<num7MHzPixels; ++x) {
			uint8 p = src[x];
			int art = intensity.sb[x]; 

			if (!art) {
				*dst = mPalette[p];
			} else {
				int c = p >> 4;
			
				__m128i chroma = _mm_loadu_si64(&mChromaVectors[c][0]);
				int y = mLumaRamp[luma.b[x]];

				__m128i color = _mm_adds_epi16(chroma, _mm_shufflelo_epi16(_mm_cvtsi32_si128(y), 0));

				color = _mm_adds_epi16(color, _mm_loadl_epi64((const __m128i *)&mArtifactRamp[art + 15]));
				color = _mm_srai_epi16(color, 6);
				color = _mm_min_epi16(_mm_max_epi16(color, zero), xFFw);

				uint8_t *dst8 = (uint8_t *)dst;
				
				dst8[0] = mGammaTable[(unsigned)_mm_extract_epi16(color, 0)];
				dst8[1] = mGammaTable[(unsigned)_mm_extract_epi16(color, 1)];
				dst8[2] = mGammaTable[(unsigned)_mm_extract_epi16(color, 2)];
				dst8[3] = 0;
			}

			++dst;
		}
	}

	return true;
}

#endif

#if defined(VD_COMPILER_MSVC) && defined(VD_CPU_AMD64)

void ATArtifactNTSCAccum_SSE2(void *rout, const void *table, const void *src, uint32 count) {
	__m128i acc0 = _mm_setzero_si128();
	__m128i acc1 = acc0;
	__m128i acc2 = acc0;
	__m128i fast_impulse0;
	__m128i fast_impulse1;
	__m128i fast_impulse2;

	const __m128i * VDRESTRICT table2 = (const __m128i *)table + 8;
	char * VDRESTRICT dst = (char *)rout;
	const uint8 *VDRESTRICT src8 = (const uint8 *)src;

	count >>= 2;

	uint32 cur0123, next0123;

	do {
		cur0123 = *(const uint32 *)src8;

		if (_rotl(cur0123, 8) == cur0123)
			goto fast_path;

slow_path:
		{
			const __m128i * VDRESTRICT impulse0 = table2 + ((size_t)src8[0] << 4);
			acc0 = _mm_add_epi16(acc0, impulse0[-8]);
			acc1 = _mm_add_epi16(acc1, impulse0[-7]);
			acc2 = impulse0[-6];

			const __m128i * VDRESTRICT impulse1 = table2 + ((size_t)src8[1] << 4);
			acc0 = _mm_add_epi16(acc0, impulse1[-4]);
			acc1 = _mm_add_epi16(acc1, impulse1[-3]);
			acc2 = _mm_add_epi16(acc2, impulse1[-2]);

			const __m128i * VDRESTRICT impulse2 = table2 + ((size_t)src8[2] << 4);
			acc0 = _mm_add_epi16(acc0, impulse2[0]);
			acc1 = _mm_add_epi16(acc1, impulse2[1]);
			acc2 = _mm_add_epi16(acc2, impulse2[2]);

			const __m128i * VDRESTRICT impulse3 = table2 + ((size_t)src8[3] << 4);
			acc0 = _mm_add_epi16(acc0, impulse3[4]);
			acc1 = _mm_add_epi16(acc1, impulse3[5]);
			acc2 = _mm_add_epi16(acc2, impulse3[6]);
		}

		acc0 = _mm_srai_epi16(acc0, 4);
		acc0 = _mm_packus_epi16(acc0, acc0);
		_mm_storel_epi64((__m128i *)dst, acc0);
		dst += 8;

		acc0 = acc1;
		acc1 = acc2;

		src8 += 4;
	} while (--count);

xit:
	acc0 = _mm_srai_epi16(acc0, 4);
	acc1 = _mm_srai_epi16(acc1, 4);
	acc0 = _mm_packus_epi16(acc0, acc0);
	acc1 = _mm_packus_epi16(acc1, acc1);
	_mm_storel_epi64((__m128i *)dst, acc0);
	dst += 8;
	_mm_storel_epi64((__m128i *)dst, acc1);
	return;

fast_path_reload:
	cur0123 = next0123;
	if (cur0123 != _rotl(cur0123, 8))
		goto slow_path;

fast_path:
	{
		const __m128i *fast_impulse = table2 + ((size_t)(uint8)cur0123 << 2);

		fast_impulse0 = fast_impulse[0x1800 - 8];
		fast_impulse1 = fast_impulse[0x1800 - 7];
		fast_impulse2 = fast_impulse[0x1800 - 6];
	}

	for(int i=0; i<3; ++i) {
		src8 += 4;

		acc0 = _mm_add_epi16(acc0, fast_impulse0);
		acc1 = _mm_add_epi16(acc1, fast_impulse1);

		acc0 = _mm_srai_epi16(acc0, 4);
		acc0 = _mm_packus_epi16(acc0, acc0);
		_mm_storel_epi64((__m128i *)dst, acc0);
		dst += 8;

		acc0 = acc1;
		acc1 = fast_impulse2;

		if (!--count)
			goto xit;

		next0123 = *(const uint32 *)src8;

		if (cur0123 != next0123)
			goto fast_path_reload;
	} 

	// beyond this point, we can copy the previous four pixels indefinitely
	const __m128i repeat_pixels = _mm_loadl_epi64((const __m128i *)(dst - 8));
	for(;;) {
		_mm_storel_epi64((__m128i *)dst, repeat_pixels);
		dst += 8;
		src8 += 4;

		if (!--count)
			goto xit;

		next0123 = *(const uint32 *)src8;

		if (cur0123 != next0123)
			goto fast_path_reload;
	}
}

void ATArtifactNTSCAccumTwin_SSE2(void *rout, const void *table, const void *src, uint32 count) {
	__m128i acc0 = _mm_setzero_si128();
	__m128i acc1 = acc0;
	__m128i acc2 = acc0;

	__m128i fast_impulse0;
	__m128i fast_impulse1;
	__m128i fast_impulse2;

	char *VDRESTRICT dst = (char *)rout;
	const __m128i *VDRESTRICT impulse_table = (const __m128i *)table;
	const uint8 *VDRESTRICT src8 = (const uint8 *)src;

	count >>= 2;

	uint8 c0, c2;
	uint32 c0123;

	do {
		c0 = src8[0];
		c2 = src8[2];
		src8 += 4;

		if (c0 == c2)
			goto fast_path;

slow_path:
		{
			const __m128i * VDRESTRICT impulse0 = impulse_table + ((size_t)c0 << 3);
			const __m128i * VDRESTRICT impulse1 = impulse_table + ((size_t)c2 << 3);

			acc0 = _mm_add_epi16(acc0, impulse0[0]);
			acc1 = _mm_add_epi16(acc1, impulse0[1]);
			acc2 = impulse0[2];

			acc0 = _mm_add_epi16(acc0, impulse1[4]);
			acc1 = _mm_add_epi16(acc1, impulse1[5]);
			acc2 = _mm_add_epi16(acc2, impulse1[6]);
		}

		acc0 = _mm_srai_epi16(acc0, 4);
		acc0 = _mm_packus_epi16(acc0, acc0);
		_mm_storel_epi64((__m128i *)dst, acc0);
		dst += 8;

		acc0 = acc1;
		acc1 = acc2;
	} while(--count);

xit:
	acc0 = _mm_srai_epi16(acc0, 4);
	acc1 = _mm_srai_epi16(acc1, 4);
	acc0 = _mm_packus_epi16(acc0, acc0);
	acc1 = _mm_packus_epi16(acc1, acc1);
	_mm_storel_epi64((__m128i *)dst, acc0);
	dst += 8;
	_mm_storel_epi64((__m128i *)dst, acc1);
	return;

fast_path_reload:
	c0 = src8[0];
	c2 = src8[2];
	src8 += 4;

	if (c0 != c2)
		goto slow_path;

fast_path:
	c0123 = *(const uint32 *)(src8 - 4);

	{
		const __m128i * VDRESTRICT fast_impulse = impulse_table + ((size_t)c0 << 2);
		fast_impulse0 = fast_impulse[0x800];
		fast_impulse1 = fast_impulse[0x801];
		fast_impulse2 = fast_impulse[0x802];
	}

	for(int i=0; i<3; ++i) {
		acc0 = _mm_add_epi16(acc0, fast_impulse0);
		acc1 = _mm_add_epi16(acc1, fast_impulse1);

		acc0 = _mm_srai_epi16(acc0, 4);
		acc0 = _mm_packus_epi16(acc0, acc0);
		_mm_storel_epi64((__m128i *)dst, acc0);
		dst += 8;

		acc0 = acc1;
		acc1 = fast_impulse2;

		if (!--count)
			goto xit;

		if (*(const uint32 *)src8 != c0123)
			goto fast_path_reload;

		src8 += 4;
	}

	// beyond this point, we can copy the previous four pixels indefinitely
	const __m128i repeat_pixels = _mm_loadl_epi64((const __m128i *)(dst - 8));
	for(;;) {
		_mm_storel_epi64((__m128i *)dst, repeat_pixels);
		dst += 8;

		if (!--count)
			goto xit;

		if (*(const uint32 *)src8 != c0123)
			goto fast_path_reload;

		src8 += 4;
	}
}

#endif
