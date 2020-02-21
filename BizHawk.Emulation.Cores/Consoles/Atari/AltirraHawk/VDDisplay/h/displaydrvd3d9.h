//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2005 Avery Lee
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
//

#ifndef f_VD2_RIZA_DISPLAYDRVDX9_H
#define f_VD2_RIZA_DISPLAYDRVDX9_H

#include <vd2/system/refcount.h>
#include <vd2/VDDisplay/renderer.h>

struct IDirect3DTexture9;
class IVDVideoDisplayDX9Manager;
class VDD3D9Manager;

namespace nsVDDisplay {
	struct TechniqueInfo;
}

class VDINTERFACE IVDFontRendererD3D9 : public IVDRefCount {
public:
	virtual bool Init(VDD3D9Manager *d3dmgr) = 0;
	virtual void Shutdown() = 0;

	virtual bool Begin() = 0;
	virtual void DrawTextLine(int x, int y, uint32 textColor, uint32 outlineColor, const char *s) = 0;
	virtual void End() = 0;
};

bool VDCreateFontRendererD3D9(IVDFontRendererD3D9 **);

class VDINTERFACE IVDDisplayRendererD3D9 : public IVDRefCount, public IVDDisplayRenderer {
public:
	virtual bool Init(VDD3D9Manager *d3dmgr, IVDVideoDisplayDX9Manager *vidmgr) = 0;
	virtual void Shutdown() = 0;

	virtual bool Begin() = 0;
	virtual void End() = 0;
};

bool VDCreateDisplayRendererD3D9(IVDDisplayRendererD3D9 **);

class VDINTERFACE IVDVideoDisplayDX9Manager : public IVDRefCount {
public:
	enum CubicMode {
		kCubicNotInitialized,
		kCubicNotPossible,
		kCubicUsePS2_0Path,
		kMaxCubicMode = kCubicUsePS2_0Path
	};

	struct EffectContext {
		IDirect3DTexture9 *mpSourceTexture1;
		IDirect3DTexture9 *mpSourceTexture2;
		IDirect3DTexture9 *mpSourceTexture3;
		IDirect3DTexture9 *mpSourceTexture4;
		IDirect3DTexture9 *mpSourceTexture5;
		IDirect3DTexture9 *mpPaletteTexture;
		IDirect3DTexture9 *mpInterpFilterH;
		IDirect3DTexture9 *mpInterpFilterV;
		IDirect3DTexture9 *mpInterpFilter;
		uint32 mSourceW;
		uint32 mSourceH;
		uint32 mSourceTexW;
		uint32 mSourceTexH;
		uint32 mInterpHTexW;
		uint32 mInterpHTexH;
		uint32 mInterpVTexW;
		uint32 mInterpVTexH;
		uint32 mInterpTexW;
		uint32 mInterpTexH;

		/// Source rect.
		vdrect32f mSourceArea;

		/// Output viewport.
		int mViewportX;
		int mViewportY;
		int mViewportW;
		int mViewportH;

		/// Desired output width and height. May extend outside of viewport, in which case clipping is desired.
		int mOutputX;
		int mOutputY;
		int mOutputW;
		int mOutputH;

		float mDefaultUVScaleCorrectionX;
		float mDefaultUVScaleCorrectionY;
		float mFieldOffset;

		float mChromaScaleU;
		float mChromaScaleV;
		float mChromaOffsetU;
		float mChromaOffsetV;

		float mPixelSharpnessX;
		float mPixelSharpnessY;

		bool mbHighPrecision;

		// If enabled, map 'autobilinear' samplers to bilinear instead of point.
		bool mbAutoBilinear;

		bool mbUseUV0Scale;
		bool mbUseUV1Area;

		vdfloat2 mUV0Scale;
		vdrect32f mUV1Area;
	};

	virtual bool IsPS20Enabled() const = 0;
	virtual bool RunEffect(const EffectContext& ctx, const nsVDDisplay::TechniqueInfo& technique, IDirect3DSurface9 *pRTOverride) = 0;
};

class VDINTERFACE IVDVideoUploadContextD3D9 : public IVDRefCount {
public:
	virtual IDirect3DTexture9 *GetD3DTexture(int i) = 0;

	virtual bool Init(void *hmonitor, bool use9ex, const VDPixmap& source, bool allowConversion, bool preserveYCbCr, int buffers, bool use16bit) = 0;
	virtual void Shutdown() = 0;

	virtual bool Update(const VDPixmap& source) = 0;
};

class VDDisplayCachedImageD3D9 final : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayCachedImageD3D9(const VDDisplayCachedImageD3D9&) = delete;
	VDDisplayCachedImageD3D9& operator=(const VDDisplayCachedImageD3D9&) = delete;
public:
	enum { kTypeID = 'cim9' };

	VDDisplayCachedImageD3D9();
	~VDDisplayCachedImageD3D9();

	void *AsInterface(uint32 iid);

	bool Init(VDD3D9Manager *mgr, void *owner, const VDDisplayImageView& imageView);
	void Shutdown();

	void Update(const VDDisplayImageView& imageView);

public:
	void *mpOwner;
	vdrefptr<IDirect3DTexture9> mpD3DTexture;
	sint32	mWidth;
	sint32	mHeight;
	sint32	mTexWidth;
	sint32	mTexHeight;
	uint32	mUniquenessCounter;
};

bool VDCreateVideoUploadContextD3D9(IVDVideoUploadContextD3D9 **);

#endif
