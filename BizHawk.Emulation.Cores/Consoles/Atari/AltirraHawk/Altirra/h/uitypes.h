//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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

#ifndef f_AT_UITYPES_H
#define f_AT_UITYPES_H

enum ATDisplayFilterMode : uint32 {
	kATDisplayFilterMode_Point,
	kATDisplayFilterMode_Bilinear,
	kATDisplayFilterMode_Bicubic,
	kATDisplayFilterMode_AnySuitable,
	kATDisplayFilterMode_SharpBilinear,
	kATDisplayFilterModeCount
};

enum ATDisplayStretchMode : uint32 {
	kATDisplayStretchMode_Unconstrained,
	kATDisplayStretchMode_PreserveAspectRatio,
	kATDisplayStretchMode_SquarePixels,
	kATDisplayStretchMode_Integral,
	kATDisplayStretchMode_IntegralPreserveAspectRatio,
	kATDisplayStretchModeCount
};

enum ATFrameRateMode : uint32 {
	kATFrameRateMode_Hardware,
	kATFrameRateMode_Broadcast,
	kATFrameRateMode_Integral,
	kATFrameRateModeCount
};

enum {
	kATUISpeedFlags_Turbo = 0x01,
	kATUISpeedFlags_TurboPulse = 0x02,
	kATUISpeedFlags_Slow = 0x04,
	kATUISpeedFlags_SlowPulse = 0x08
};

enum ATVideoRecordingFrameRate {
	kATVideoRecordingFrameRate_Normal,
	kATVideoRecordingFrameRate_NTSCRatio,
	kATVideoRecordingFrameRate_Integral,
	kATVideoRecordingFrameRateCount
};

#endif	// f_AT_UITYPES_H
