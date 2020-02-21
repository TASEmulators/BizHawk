//	VirtualDub - Video processing and capture application
//	Display library - custom D3D9 shader support
//	Copyright (C) 1998-2016 Avery Lee
//
//	This program is free software; you can redistribute it and/or
//	modify it under the terms of the GNU General Public License
//	as published by the Free Software Foundation; either version 2
//	of the License, or (at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#ifndef f_VD2_VDDISPLAY_INTERNAL_CUSTOMSHADERD3D9_H
#define f_VD2_VDDISPLAY_INTERNAL_CUSTOMSHADERD3D9_H

#include <vd2/system/vectors.h>

class VDD3D9Manager;
struct IDirect3DTexture9;

struct VDDisplayCustomShaderPassInfo {
	float mTiming;
	uint32 mOutputWidth;
	uint32 mOutputHeight;
	bool mbOutputFloat;			// output surface is float type (float or half)
	bool mbOutputHalfFloat;		// output surface is half-float
};

class IVDDisplayCustomShaderPipelineD3D9 {
public:
	virtual ~IVDDisplayCustomShaderPipelineD3D9() = default;

	virtual bool ContainsFinalBlit() const = 0;
	virtual uint32 GetMaxPrevFrames() const = 0;
	virtual bool HasTimingInfo() const = 0;
	virtual const VDDisplayCustomShaderPassInfo *GetPassTimings(uint32& numPasses) = 0;

	virtual void Run(IDirect3DTexture9 *const *srcTextures, const vdsize32& texSize, const vdsize32& imageSize, const vdsize32& viewportSize) = 0;
	virtual void RunFinal(const vdrect32f& dstRect, const vdsize32& viewportSize) = 0;

	virtual IDirect3DTexture9 *GetFinalOutput(uint32& imageWidth, uint32& imageHeight) = 0;
};

IVDDisplayCustomShaderPipelineD3D9 *VDDisplayParseCustomShaderPipeline(VDD3D9Manager *d3d9mgr, const wchar_t *path);

#endif
