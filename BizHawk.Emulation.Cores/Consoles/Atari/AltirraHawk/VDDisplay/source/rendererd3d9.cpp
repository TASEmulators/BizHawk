//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2016 Avery Lee
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
//
//	This file is the DirectX 9 driver for the video display subsystem.
//	It does traditional point sampled and bilinearly filtered upsampling
//	as well as a special multipass algorithm for emulated bicubic
//	filtering.

#define DIRECTDRAW_VERSION 0x0900
#define INITGUID

#include <vd2/system/vdtypes.h>
#include <vd2/system/refcount.h>
#include <vd2/system/seh.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/direct3d.h>
#include <vd2/VDDisplay/textrenderer.h>
#include <displaydrvd3d9.h>
#include <d3d9.h>

namespace nsVDDisplay {
	extern const TechniqueInfo g_technique_boxlinear_2_0;
}

class VDDisplayRendererD3D9 : public vdrefcounted<IVDDisplayRendererD3D9> {
public:
	virtual bool Init(VDD3D9Manager *d3dmgr, IVDVideoDisplayDX9Manager *vidmgr);
	virtual void Shutdown();

	virtual bool Begin();
	virtual void End();

public:
	virtual const VDDisplayRendererCaps& GetCaps();

	VDDisplayTextRenderer *GetTextRenderer() { return &mTextRenderer; }

	virtual void SetColorRGB(uint32 color);
	virtual void FillRect(sint32 x, sint32 y, sint32 w, sint32 h);
	virtual void MultiFillRect(const vdrect32 *rects, uint32 n);

	virtual void AlphaFillRect(sint32 x, sint32 y, sint32 w, sint32 h, uint32 alphaColor);
	virtual void AlphaTriStrip(const vdfloat2 *pts, uint32 numPts, uint32 alphaColor);

	virtual void Blt(sint32 x, sint32 y, VDDisplayImageView& imageView);
	virtual void Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h);
	virtual void StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts);
	virtual void MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode);

	virtual void PolyLine(const vdpoint32 *points, uint32 numLines);

	virtual bool PushViewport(const vdrect32& r, sint32 x, sint32 y);
	virtual void PopViewport();

	virtual IVDDisplayRenderer *BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache);
	virtual void EndSubRender();

public:
	void UpdateViewport();
	void ApplyBaselineState();

	VDDisplayCachedImageD3D9 *GetCachedImage(VDDisplayImageView& imageView);

	VDD3D9Manager *mpD3DManager;
	vdrefptr<IVDVideoDisplayDX9Manager> mpVideoManager;
	vdlist<VDDisplayCachedImageD3D9> mCachedImages;

	uint32 mColor;
	sint32 mOffsetX;
	sint32 mOffsetY;

	sint32 mViewportBaseX;
	sint32 mViewportBaseY;
	sint32 mViewportBaseWidth;
	sint32 mViewportBaseHeight;
	vdrect32 mViewport;

	struct Context {
		uint32 mColor;
	};

	typedef vdfastvector<Context> ContextStack;
	ContextStack mContextStack;

	struct Viewport {
		vdrect32 mViewport;
		sint32 mOffsetX;
		sint32 mOffsetY;
	};

	typedef vdfastvector<Viewport> ViewportStack;
	ViewportStack mViewportStack;

	VDDisplayTextRenderer mTextRenderer;
};

bool VDDisplayRendererD3D9::Init(VDD3D9Manager *d3dmgr, IVDVideoDisplayDX9Manager *vidmgr) {
	mpD3DManager = d3dmgr;
	mpVideoManager = vidmgr;

	mTextRenderer.Init(this, 512, 512);
	return true;
}

void VDDisplayRendererD3D9::Shutdown() {
	while(!mCachedImages.empty()) {
		VDDisplayCachedImageD3D9 *img = mCachedImages.front();
		mCachedImages.pop_front();

		img->mListNodePrev = NULL;
		img->mListNodeNext = NULL;

		img->Shutdown();
	}

	mpVideoManager.clear();
	mpD3DManager = NULL;
}

bool VDDisplayRendererD3D9::Begin() {
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();

	mOffsetX = 0;
	mOffsetY = 0;

	D3DVIEWPORT9 vp;
	HRESULT hr = dev->GetViewport(&vp);
	if (FAILED(hr))
		return false;

	const D3DMATRIX ident={
		1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1
	};

	dev->SetTransform(D3DTS_WORLD, &ident);
	dev->SetTransform(D3DTS_VIEW, &ident);

	mViewportBaseX = vp.X;
	mViewportBaseY = vp.Y;
	mViewportBaseWidth = vp.Width;
	mViewportBaseHeight = vp.Height;

	mViewport.set(vp.X, vp.Y, vp.X + vp.Width, vp.Y + vp.Height);

	UpdateViewport();

	ApplyBaselineState();

	return mpD3DManager->BeginScene();
}

void VDDisplayRendererD3D9::End() {
}

const VDDisplayRendererCaps& VDDisplayRendererD3D9::GetCaps() {
	static const VDDisplayRendererCaps kCaps = {
		true
	};

	return kCaps;
}

void VDDisplayRendererD3D9::SetColorRGB(uint32 color) {
	mColor = color | 0xFF000000;
}

void VDDisplayRendererD3D9::FillRect(sint32 x, sint32 y, sint32 w, sint32 h) {
	if ((w|h) < 0)
		return;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);

	nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4);
	if (!pvx)
		return;

	x += mOffsetX;
	y += mOffsetY;

	bool valid = false;
	__try {
		pvx[0].SetFF2((float)x,       (float)y,       mColor, 0, 0, 0, 0);
		pvx[1].SetFF2((float)x,       (float)(y + h), mColor, 0, 0, 0, 0);
		pvx[2].SetFF2((float)(x + w), (float)y,       mColor, 0, 0, 0, 0);
		pvx[3].SetFF2((float)(x + w), (float)(y + h), mColor, 0, 0, 0, 0);
		valid = true;
	} __except(1) {
		// lost device -> invalid dynamic pointer on XP - skip draw below
	}

	mpD3DManager->UnlockVertices();

	if (valid)
		mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);
}

void VDDisplayRendererD3D9::MultiFillRect(const vdrect32 *rects, uint32 n) {
	if (!n)
		return;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);

	while(n) {
		uint32 c = std::min<uint32>(n, 100);
		n -= c;

		nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(c * 4);
		if (!pvx)
			break;

		uint16 *pidx = mpD3DManager->LockIndices(c * 6);
		if (!pidx) {
			mpD3DManager->UnlockVertices();
			break;
		}

		bool valid = false;
		__try {
			for(uint32 i=0; i<c; ++i) {
				vdrect32 r = *rects++;

				r.translate(mOffsetX, mOffsetY);

				pvx[0].SetFF2((float)r.left,  (float)r.top,    mColor, 0, 0, 0, 0);
				pvx[1].SetFF2((float)r.left,  (float)r.bottom, mColor, 0, 0, 0, 0);
				pvx[2].SetFF2((float)r.right, (float)r.top,    mColor, 0, 0, 0, 0);
				pvx[3].SetFF2((float)r.right, (float)r.bottom, mColor, 0, 0, 0, 0);
				pvx += 4;
			}

			uint16 v = 0;
			for(uint32 i=0; i<c; ++i) {
				pidx[0] = v;
				pidx[1] = v+1;
				pidx[2] = v+2;
				pidx[3] = v+2;
				pidx[4] = v+1;
				pidx[5] = v+3;
				pidx += 6;
				v += 4;
			}

			valid = true;
		} __except(1) {
			// lost device -> invalid dynamic pointer on XP - skip draw below
		}

		mpD3DManager->UnlockIndices();
		mpD3DManager->UnlockVertices();

		if (!valid)
			break;

		mpD3DManager->DrawElements(D3DPT_TRIANGLELIST, 0, c * 4, 0, c * 2);
	}
}

void VDDisplayRendererD3D9::AlphaFillRect(sint32 x, sint32 y, sint32 w, sint32 h, uint32 alphaColor) {
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
	dev->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
	dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

	nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4);
	if (!pvx)
		return;

	x += mOffsetX;
	y += mOffsetY;

	bool valid = false;
	__try {
		pvx[0].SetFF2((float)x,       (float)y,       alphaColor, 0, 0, 0, 0);
		pvx[1].SetFF2((float)x,       (float)(y + h), alphaColor, 0, 0, 0, 0);
		pvx[2].SetFF2((float)(x + w), (float)y,       alphaColor, 0, 0, 0, 0);
		pvx[3].SetFF2((float)(x + w), (float)(y + h), alphaColor, 0, 0, 0, 0);
		valid = true;
	} __except(1) {
		// lost device -> invalid dynamic pointer on XP - skip draw below
	}

	mpD3DManager->UnlockVertices();

	if (valid)
		mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);

	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
}

void VDDisplayRendererD3D9::AlphaTriStrip(const vdfloat2 *pts, uint32 numPts, uint32 alphaColor) {
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);
	dev->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
	dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCALPHA);

	constexpr uint32 maxBatchPts = (nsVDD3D9::kVertexBufferSize / sizeof(nsVDD3D9::Vertex)) & ~1;

	while(numPts >= 3) {
		const uint32 numBatchPts = std::min(numPts, maxBatchPts);

		nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(numBatchPts);
		if (!pvx)
			break;

		bool valid = false;
		vd_seh_guard_try {
			for(uint32 i=0; i<numPts; ++i) {
				pvx[i] = nsVDD3D9::Vertex((float)pts[i].x, (float)pts[i].y, alphaColor, 0, 0);
			}

			valid = true;
		} vd_seh_guard_except {
			// lost device -> invalid dynamic pointer on XP - skip draw below
		}

		mpD3DManager->UnlockVertices();

		if (!valid)
			break;

		mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, numBatchPts - 2);

		// if we reached the end, we're done
		if (numBatchPts >= numPts)
			break;

		// overlap last two points to maintain tristrip continuity
		pts += numBatchPts - 2;
		numPts -= numBatchPts - 2;
	}

	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
}

void VDDisplayRendererD3D9::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView) {
	VDDisplayCachedImageD3D9 *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	mOffsetX += x;
	mOffsetY += y;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	dev->SetTexture(0, cachedImage->mpD3DTexture);

	const sint32 w = cachedImage->mWidth;
	const sint32 h = cachedImage->mHeight;

	const float u0 = 0.0;
	const float u1 = (float)w / cachedImage->mTexWidth;
	const float v0 = 0.0;
	const float v1 = (float)h / cachedImage->mTexHeight;

	nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4);
	if (!pvx)
		return;

	bool valid = false;
	__try {
		pvx[0].SetFF2((float)x,       (float)y,       mColor, u0, v0, 0, 0);
		pvx[1].SetFF2((float)x,       (float)(y + h), mColor, u0, v1, 0, 0);
		pvx[2].SetFF2((float)(x + w), (float)y,       mColor, u1, v0, 0, 0);
		pvx[3].SetFF2((float)(x + w), (float)(y + h), mColor, u1, v1, 0, 0);
		valid = true;
	} __except(1) {
		// lost device -> invalid dynamic pointer on XP - skip draw below
	}

	mpD3DManager->UnlockVertices();

	if (valid)
		mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);	

	dev->SetTexture(0, NULL);
}

void VDDisplayRendererD3D9::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h) {
	VDDisplayCachedImageD3D9 *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	x += mOffsetX;
	y += mOffsetY;

	// do source clipping
	if (sx < 0) { x -= sx; w += sx; sx = 0; }
	if (sy < 0) { y -= sy; h += sy; sy = 0; }

	if ((w|h) < 0)
		return;

	if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - x; }
	if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - y; }

	if ((w|h) < 0)
		return;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	dev->SetTexture(0, cachedImage->mpD3DTexture);

	const float invsw = 1.0f / (float)cachedImage->mTexWidth;
	const float invsh = 1.0f / (float)cachedImage->mTexHeight;
	const float u0 = (float)sx * invsw;
	const float u1 = (float)(sx + w) * invsw;
	const float v0 = (float)sy * invsh;
	const float v1 = (float)(sy + h) * invsh;

	nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4);
	if (!pvx)
		return;

	bool valid = false;
	__try {
		pvx[0].SetFF2((float)x,       (float)y,       mColor, u0, v0, 0, 0);
		pvx[1].SetFF2((float)x,       (float)(y + h), mColor, u0, v1, 0, 0);
		pvx[2].SetFF2((float)(x + w), (float)y,       mColor, u1, v0, 0, 0);
		pvx[3].SetFF2((float)(x + w), (float)(y + h), mColor, u1, v1, 0, 0);
		valid = true;
	} __except(1) {
		// lost device -> invalid dynamic pointer on XP - skip draw below
	}

	mpD3DManager->UnlockVertices();

	if (valid)
		mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);	

	dev->SetTexture(0, NULL);
}

void VDDisplayRendererD3D9::StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts) {
	VDDisplayCachedImageD3D9 *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	dx += mOffsetX;
	dy += mOffsetY;

	// do source clipping
	if (sx < 0 || sx >= cachedImage->mWidth)
		return;

	if (sy < 0 || sy >= cachedImage->mHeight)
		return;

	if (sw > cachedImage->mWidth - sx || sh > cachedImage->mHeight)
		return;

	if (sw <= 0 || sh <= 0)
		return;

	if (dw <= 0 || dh <= 0)
		return;

	mpD3DManager->BeginScope(L"StretchBlt");

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	if (opts.mFilterMode == VDDisplayBltOptions::kFilterMode_Bilinear && opts.mSharpnessX > 0 && mpVideoManager->IsPS20Enabled()) {
		IVDVideoDisplayDX9Manager::EffectContext ectx = {};
		ectx.mpSourceTexture1 = cachedImage->mpD3DTexture;
		ectx.mSourceW = sw;
		ectx.mSourceH = sh;
		ectx.mSourceTexW = cachedImage->mTexWidth;
		ectx.mSourceTexH = cachedImage->mTexHeight;
		ectx.mSourceArea.set((float)sx, (float)sy, (float)sx + (float)sw, (float)sy + (float)sh);
		ectx.mViewportX = mViewport.left;
		ectx.mViewportY = mViewport.top;
		ectx.mViewportW = mViewport.width();
		ectx.mViewportH = mViewport.height();
		ectx.mOutputX = dx;
		ectx.mOutputY = dy;
		ectx.mOutputW = dw;
		ectx.mOutputH = dh;
		ectx.mDefaultUVScaleCorrectionX = 1.0f;
		ectx.mDefaultUVScaleCorrectionY = 1.0f;
		ectx.mChromaScaleU = 1.0f;
		ectx.mChromaScaleV = 1.0f;
		ectx.mPixelSharpnessX = opts.mSharpnessX;
		ectx.mPixelSharpnessY = opts.mSharpnessY;

		IDirect3DSurface9 *rt;
		HRESULT hr = dev->GetRenderTarget(0, &rt);

		if (SUCCEEDED(hr)) {
			mpVideoManager->RunEffect(ectx, nsVDDisplay::g_technique_boxlinear_2_0, rt);
			rt->Release();

			ApplyBaselineState();
			UpdateViewport();
		}
	} else {
		dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_TEXTURE);
		dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
		dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE);
		dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
		dev->SetTexture(0, cachedImage->mpD3DTexture);

		const float invsw = 1.0f / (float)cachedImage->mTexWidth;
		const float invsh = 1.0f / (float)cachedImage->mTexHeight;
		const float u0 = (float)sx * invsw;
		const float u1 = (float)(sx + sw) * invsw;
		const float v0 = (float)sy * invsh;
		const float v1 = (float)(sy + sh) * invsh;

		nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4);
		if (pvx) {
			bool valid = false;
			__try {
				pvx[0].SetFF2((float)dx,        (float)dy,        mColor, u0, v0, 0, 0);
				pvx[1].SetFF2((float)dx,        (float)(dy + dh), mColor, u0, v1, 0, 0);
				pvx[2].SetFF2((float)(dx + dw), (float)dy,        mColor, u1, v0, 0, 0);
				pvx[3].SetFF2((float)(dx + dw), (float)(dy + dh), mColor, u1, v1, 0, 0);
				valid = true;
			} __except(1) {
				// lost device -> invalid dynamic pointer on XP - skip draw below
			}

			mpD3DManager->UnlockVertices();

			if (valid) {
				if (opts.mFilterMode != VDDisplayBltOptions::kFilterMode_Point) {
					dev->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_LINEAR);
					dev->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_LINEAR);
				}

				mpD3DManager->DrawArrays(D3DPT_TRIANGLESTRIP, 0, 2);	

				if (opts.mFilterMode != VDDisplayBltOptions::kFilterMode_Point) {
					dev->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_POINT);
					dev->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_POINT);
				}
			}
		}

		dev->SetTexture(0, NULL);
	}

	mpD3DManager->EndScope();
}

void VDDisplayRendererD3D9::MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode) {
	if (!n)
		return;

	VDDisplayCachedImageD3D9 *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	const float invsw = 1.0f / (float)cachedImage->mTexWidth;
	const float invsh = 1.0f / (float)cachedImage->mTexHeight;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTexture(0, cachedImage->mpD3DTexture);

	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, TRUE);

	for(int pass=0; pass<3; ++pass) {
		nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(4*n);
		if (!pvx)
			return;

		uint16 *pidx = mpD3DManager->LockIndices(6*n);
		if (!pidx) {
			mpD3DManager->UnlockVertices();
			return;
		}

		uint32 color;
		switch(pass) {
			case 0:
				color = ((mColor & 0xff0000) << 8) + 0xff0000;
				break;

			case 1:
				color = ((mColor & 0x00ff00) << 16) + 0x00ff00;
				break;

			case 2:
				color = ((mColor & 0x0000ff) << 24) + 0x0000ff;
				break;
		}

		bool valid = false;
		uint32 rects = 0;
		__try {
			for(uint32 i=0; i<n; ++i) {
				const VDDisplayBlt& blt = blts[i];
				sint32 x = blt.mDestX + mOffsetX;
				sint32 y = blt.mDestY + mOffsetY;
				sint32 sx = blt.mSrcX;
				sint32 sy = blt.mSrcY;
				sint32 w = blt.mWidth;
				sint32 h = blt.mHeight;

				// do source clipping
				if (sx < 0) { x -= sx; w += sx; sx = 0; }
				if (sy < 0) { y -= sy; h += sy; sy = 0; }

				if ((w|h) < 0)
					continue;

				if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - sx; }
				if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - sy; }

				if (w <= 0 || h <= 0)
					continue;

				const float u0 = (float)sx * invsw;
				const float u1 = (float)(sx + w) * invsw;
				const float v0 = (float)sy * invsh;
				const float v1 = (float)(sy + h) * invsh;
				pvx[0].SetFF2((float)x,       (float)y,       color, u0, v0, 0, 0);
				pvx[1].SetFF2((float)x,       (float)(y + h), color, u0, v1, 0, 0);
				pvx[2].SetFF2((float)(x + w), (float)y,       color, u1, v0, 0, 0);
				pvx[3].SetFF2((float)(x + w), (float)(y + h), color, u1, v1, 0, 0);
				pvx += 4;

				pidx[0] = rects*4+0;
				pidx[1] = rects*4+1;
				pidx[2] = rects*4+2;
				pidx[3] = rects*4+2;
				pidx[4] = rects*4+1;
				pidx[5] = rects*4+3;
				pidx += 6;

				++rects;
			}

			valid = true;
		} __except(1) {
			// lost device -> invalid dynamic pointer on XP - skip draw below
		}

		mpD3DManager->UnlockIndices();
		mpD3DManager->UnlockVertices();

		if (valid) {
			dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
			dev->SetTextureStageState(0, D3DTSS_COLORARG2, D3DTA_TEXTURE);
			dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_MODULATE);
			dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
			dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
			dev->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_SRCALPHA);
			dev->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_INVSRCCOLOR);

			mpD3DManager->DrawElements(D3DPT_TRIANGLELIST, 0, rects*4, 0, rects*2);	
		}
	}

	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
	dev->SetTexture(0, NULL);
}

void VDDisplayRendererD3D9::PolyLine(const vdpoint32 *points, uint32 numLines) {
	if (!numLines)
		return;

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);

	while(numLines) {
		uint32 c = std::min<uint32>(numLines, 100);
		numLines -= c;

		nsVDD3D9::Vertex *pvx = mpD3DManager->LockVertices(c + 1);
		if (!pvx)
			break;

		bool valid = false;
		__try {
			for(uint32 i=0; i<=c; ++i) {
				pvx[i].SetFF2((float)points[i].x + mOffsetX + 0.5f, (float)points[i].y + mOffsetY + 0.5f, mColor, 0, 0, 0, 0);
			}

			points += c;

			valid = true;
		} __except(1) {
			// lost device -> invalid dynamic pointer on XP - skip draw below
		}

		mpD3DManager->UnlockVertices();

		if (!valid)
			break;

		mpD3DManager->DrawArrays(D3DPT_LINESTRIP, 0, c);
	}
}

bool VDDisplayRendererD3D9::PushViewport(const vdrect32& r, sint32 x, sint32 y) {
	sint32 offX = x - r.left;
	sint32 offY = y - r.top;

	vdrect32 viewport(r);
	viewport.translate(mOffsetX, mOffsetY);

	// clip new viewport in existing viewport coordinates
	if (viewport.left < 0) {
		offX += viewport.left;
		viewport.left = 0;
	}

	if (viewport.top < 0) {
		offY += viewport.top;
		viewport.top = 0;
	}

	if (viewport.right > mViewport.width())
		viewport.right = mViewport.width();

	if (viewport.bottom > mViewport.height())
		viewport.bottom = mViewport.height();

	if (viewport.empty())
		return false;

	// translate from viewport to screen coordinates
	viewport.translate(mViewport.left, mViewport.top);

	Viewport& vp = mViewportStack.push_back();
	vp.mOffsetX = mOffsetX;
	vp.mOffsetY = mOffsetY;
	vp.mViewport = mViewport;

	mViewport = viewport;
	UpdateViewport();

	mOffsetX = offX;
	mOffsetY = offY;
	return true;
}

void VDDisplayRendererD3D9::PopViewport() {
	const Viewport& vp = mViewportStack.back();

	mOffsetX = vp.mOffsetX;
	mOffsetY = vp.mOffsetY;
	mViewport = vp.mViewport;

	mViewportStack.pop_back();

	UpdateViewport();
}

IVDDisplayRenderer *VDDisplayRendererD3D9::BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache) {
	if (!PushViewport(r, r.left, r.top))
		return nullptr;

	Context& c = mContextStack.push_back();
	c.mColor = mColor;

	SetColorRGB(0);
	return this;
}

void VDDisplayRendererD3D9::EndSubRender() {
	const Context& c = mContextStack.back();

	SetColorRGB(c.mColor);

	mContextStack.pop_back();

	PopViewport();
}

void VDDisplayRendererD3D9::UpdateViewport() {
	const float ivpw = 1.0f / (float)mViewport.width();
	const float ivph = 1.0f / (float)mViewport.height();

	const D3DMATRIX proj = {
		{
		2.0f * ivpw, 0.0f, 0.0f, 0.0f,
		0.0f, -2.0f * ivph, 0.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 0.0f,
		-1.0f - 1.0f * ivpw, 1.0f + 1.0f * ivph, 0.0f, 1.0f
		}
	};

	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();
	D3DVIEWPORT9 vp;
	vp.X = mViewport.left;
	vp.Y = mViewport.top;
	vp.Width = mViewport.width();
	vp.Height = mViewport.height();
	vp.MinZ = 0;
	vp.MaxZ = 1;
	dev->SetViewport(&vp);
	dev->SetTransform(D3DTS_PROJECTION, &proj);
}

void VDDisplayRendererD3D9::ApplyBaselineState() {
	IDirect3DDevice9 *dev = mpD3DManager->GetDevice();

	dev->SetTextureStageState(0, D3DTSS_COLOROP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_COLORARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1);
	dev->SetTextureStageState(0, D3DTSS_ALPHAARG1, D3DTA_CURRENT);
	dev->SetTextureStageState(1, D3DTSS_COLOROP, D3DTOP_DISABLE);
	dev->SetTextureStageState(1, D3DTSS_ALPHAOP, D3DTOP_DISABLE);

	dev->SetSamplerState(0, D3DSAMP_ADDRESSU, D3DTADDRESS_CLAMP);
	dev->SetSamplerState(0, D3DSAMP_ADDRESSV, D3DTADDRESS_CLAMP);
	dev->SetSamplerState(0, D3DSAMP_MINFILTER, D3DTEXF_POINT);
	dev->SetSamplerState(0, D3DSAMP_MAGFILTER, D3DTEXF_POINT);
	dev->SetSamplerState(0, D3DSAMP_MIPFILTER, D3DTEXF_NONE);

	dev->SetRenderState(D3DRS_CULLMODE, D3DCULL_NONE);
	dev->SetRenderState(D3DRS_LIGHTING, FALSE);
	dev->SetRenderState(D3DRS_STENCILENABLE, FALSE);
	dev->SetRenderState(D3DRS_ZENABLE, FALSE);
	dev->SetRenderState(D3DRS_ALPHATESTENABLE, FALSE);
	dev->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);

	dev->SetVertexShader(NULL);
	dev->SetVertexDeclaration(mpD3DManager->GetVertexDeclaration());
	dev->SetPixelShader(NULL);
	dev->SetStreamSource(0, mpD3DManager->GetVertexBuffer(), 0, sizeof(nsVDD3D9::Vertex));
	dev->SetIndices(mpD3DManager->GetIndexBuffer());
}

VDDisplayCachedImageD3D9 *VDDisplayRendererD3D9::GetCachedImage(VDDisplayImageView& imageView) {
	VDDisplayCachedImageD3D9 *cachedImage = static_cast<VDDisplayCachedImageD3D9 *>(imageView.GetCachedImage(VDDisplayCachedImageD3D9::kTypeID));

	if (cachedImage && cachedImage->mpOwner != this)
		cachedImage = NULL;

	if (!cachedImage) {
		cachedImage = new_nothrow VDDisplayCachedImageD3D9;

		if (!cachedImage)
			return NULL;
		
		cachedImage->AddRef();
		if (!cachedImage->Init(mpD3DManager, this, imageView)) {
			cachedImage->Release();
			return NULL;
		}

		imageView.SetCachedImage(VDDisplayCachedImageD3D9::kTypeID, cachedImage);
		mCachedImages.push_back(cachedImage);

		cachedImage->Release();
	} else {
		uint32 c = imageView.GetUniquenessCounter();

		if (cachedImage->mUniquenessCounter != c)
			cachedImage->Update(imageView);
	}

	return cachedImage;
}

bool VDCreateDisplayRendererD3D9(IVDDisplayRendererD3D9 **pp) {
	*pp = new VDDisplayRendererD3D9;

	if (!*pp)
		return false;

	(*pp)->AddRef();
	return true;
}
