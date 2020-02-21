//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - NEON vector intrinsics support
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

#ifndef f_AT_ATCORE_INTRIN_NEON_H
#define f_AT_ATCORE_INTRIN_NEON_H

#include <intrin.h>

inline void ATMaskedWrite_NEON(uint8x16_t src, uint8x16_t mask, void *dstp) {
	vst1q_u8((uint8_t *)dstp, vbslq_u8(mask, src, vld1q_u8((const uint8_t *)dstp)));
}

#endif
