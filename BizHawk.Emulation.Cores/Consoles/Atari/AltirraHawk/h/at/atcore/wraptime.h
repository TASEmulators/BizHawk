//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - wrapped time helpers
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

#ifndef f_AT_ATCORE_WRAPTIME_H
#define f_AT_ATCORE_WRAPTIME_H

#include <vd2/system/vdtypes.h>

// Helper to make near-term time comparisons with 32-bit wrapped time.
// Typical usage:
//
//		ATWrapTime { time1 } < time2
//
// In many cases timestamps that need to be compared are close enough together
// that wrapping is not a concern -- such as those accumulated during a frame --
// and therefore good performance can be attained with only 32-bit time. For
// cases where the interval between timestamps could approach or exceed 2^31
// and this is unsafe, 64-bit time should be used instead.

struct ATWrapTime {
	uint32 t;

	constexpr bool operator< (uint32 u) const { return (sint32)(uint32)(t - u) <  0; }
	constexpr bool operator<=(uint32 u) const { return (sint32)(uint32)(t - u) <= 0; }
	constexpr bool operator> (uint32 u) const { return (sint32)(uint32)(t - u) >  0; }
	constexpr bool operator>=(uint32 u) const { return (sint32)(uint32)(t - u) >= 0; }
};

#endif
