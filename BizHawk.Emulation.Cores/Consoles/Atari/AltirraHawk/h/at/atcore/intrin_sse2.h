//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - SSE2 vector intrinsics support
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

#ifndef f_AT_ATCORE_INTRIN_SSE2_H
#define f_AT_ATCORE_INTRIN_SSE2_H

#include <vd2/system/vdtypes.h>
#include <intrin.h>

inline void ATMaskedWrite_SSE2(__m128i src, __m128i mask, void *dstp) {
	__m128i dst = *(__m128i *)dstp;

	*(__m128i *)dstp = _mm_or_si128(_mm_and_si128(src, mask), _mm_andnot_si128(mask, dst));
}

// We do unaligned loads from this array, so it's important that we
// avoid data cache unit (DCU) split penalties on older CPUs. Minimum
// for SSE2 is Pentium 4, so we can assume at least 64 byte cache lines.
alignas(64) inline const uint8 g_ATWindowTable[48] = {
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
};

inline __m128i ATIntrinGetStartMask_SSE2(uint32 offset) {
	return _mm_loadu_si128((const __m128i *)(g_ATWindowTable + 16 - offset));
}

inline __m128i ATIntrinGetEndMask_SSE2(uint32 offset) {
	return _mm_loadu_si128((const __m128i *)(g_ATWindowTable + 32 - offset));
}

#endif
