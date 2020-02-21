//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
//	System constant values
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

#ifndef f_AT_ATCORE_CONSTANTS_H
#define f_AT_ATCORE_CONSTANTS_H

#include <vd2/system/fraction.h>

///////////////////////////////////////////////////////////////////////////
// Master clock frequencies (as fractions)
//
static constexpr VDFraction kATMasterClockFrac_NTSC		{ 3579545, 2 };
static constexpr VDFraction kATMasterClockFrac_PAL		{ 3546895, 2 };
static constexpr VDFraction kATMasterClockFrac_SECAM	{ 1781500, 1 };

///////////////////////////////////////////////////////////////////////////
// Master clock frequencies (as floats)
//
static constexpr float kATMasterClock_NTSC	= (float)kATMasterClockFrac_NTSC.asDouble();
static constexpr float kATMasterClock_PAL	= (float)kATMasterClockFrac_PAL.asDouble();
static constexpr float kATMasterClock_SECAM	= (float)kATMasterClockFrac_SECAM.asDouble();

///////////////////////////////////////////////////////////////////////////
// Video frame rates
//
static constexpr float kATFrameRate_NTSC	= kATMasterClock_NTSC	/ (114 * 262);
static constexpr float kATFrameRate_PAL		= kATMasterClock_PAL	/ (114 * 312);
static constexpr float kATFrameRate_SECAM	= kATMasterClock_SECAM	/ (114 * 312);

#endif
