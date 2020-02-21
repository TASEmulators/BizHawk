//	Altirra - Atari 800/800XL/5200 emulator
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_VD2_VDDISPLAY_INTERNAL_SCREENFX_H
#define f_VD2_VDDISPLAY_INTERNAL_SCREENFX_H

#include <optional>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vectors.h>

void VDDisplayCreateGammaRamp(uint32 *gammaTex, uint32 len, bool enableInputConversion, bool useAdobeRgb, float outputGamma);
void VDDisplayCreateScanlineMaskTexture(uint32 *scanlineTex, ptrdiff_t pitch, uint32 srcH, uint32 dstH, uint32 texSize, float intensity);

struct VDDisplayDistortionMapping {
	float mScaleX;
	float mScaleY;
	float mSqRadius;

	void Init(float viewAngleX, float viewRatioY, float viewAspect);

	bool MapImageToScreen(vdfloat2& normDestPt) const;
	bool MapScreenToImage(vdfloat2& normDestPt) const;
};
#endif
