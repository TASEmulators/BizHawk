#ifndef f_VD2_VDDISPLAY_RENDERER3D_H
#define f_VD2_VDDISPLAY_RENDERER3D_H

#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <vd2/Tessa/Context.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/textrenderer.h>
#include "displaynode3d.h"

#ifdef _MSC_VER
#pragma once
#endif

class VDDisplayNodeContext3D;

class VDDisplayCachedImage3D : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayCachedImage3D(const VDDisplayCachedImage3D&);
	VDDisplayCachedImage3D& operator=(const VDDisplayCachedImage3D&);
public:
	enum { kTypeID = 'cim3' };

	VDDisplayCachedImage3D();
	~VDDisplayCachedImage3D();

	void *AsInterface(uint32 iid);

	bool Init(IVDTContext& ctx, void *owner, const VDDisplayImageView& imageView);
	void Shutdown();

	void Update(const VDDisplayImageView& imageView);

public:
	void *mpOwner;
	vdrefptr<IVDTTexture2D> mpTexture;
	sint32	mWidth;
	sint32	mHeight;
	sint32	mTexWidth;
	sint32	mTexHeight;
	uint32	mUniquenessCounter;

	vdrefptr<VDDisplayBlitNode3D> mpHiBltNode;
	float	mHiBltSharpnessX;
	float	mHiBltSharpnessY;
	vdrect32 mHiBltSrcRect;
};

class VDDisplayRenderer3D : public IVDDisplayRenderer {
public:
	VDDisplayRenderer3D();

	bool Init(IVDTContext& ctx);
	void Shutdown();

	void Begin(int w, int h, VDDisplayNodeContext3D& dctx);
	void End();

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

protected:
	struct FillVertex {
		float x;
		float y;
		uint32 c;
	};

	struct BlitVertex {
		float x;
		float y;
		uint32 c;
		float u;
		float v;
	};

	static constexpr uint32 kVBSize = 65536;

	void AddLines(const FillVertex *p, uint32 n, bool alpha);
	void AddLineStrip(const FillVertex *p, uint32 n, bool alpha);
	void AddQuads(const FillVertex *p, uint32 n, bool alpha);

	void AddQuads(const BlitVertex *p, uint32 n, BltMode bltMode);
	VDDisplayCachedImage3D *GetCachedImage(VDDisplayImageView& imageView);
	void ApplyBaselineState();

	uint32 mColor;
	uint32 mNativeColor;
	uint32	mVBOffset;
	sint32	mWidth;
	sint32	mHeight;
	vdrect32 mClipRect;
	sint32	mOffsetX;
	sint32	mOffsetY;

	IVDTContext *mpContext;
	IVDTVertexProgram *mpVPFill;
	IVDTVertexProgram *mpVPBlit;
	IVDTVertexFormat *mpVFFill;
	IVDTVertexFormat *mpVFBlit;
	IVDTFragmentProgram *mpFPFill;
	IVDTFragmentProgram *mpFPBlit;
	IVDTFragmentProgram *mpFPBlitDirect;
	IVDTFragmentProgram *mpFPBlitStencil;
	IVDTFragmentProgram *mpFPBlitColor;
	IVDTFragmentProgram *mpFPBlitColor2;
	IVDTVertexBuffer *mpVB;
	IVDTIndexBuffer *mpIB;
	IVDTSamplerState *mpSS;
	IVDTSamplerState *mpSSPoint;
	IVDTBlendState *mpBS;
	IVDTBlendState *mpBSStencil;
	IVDTBlendState *mpBSColor;
	IVDTRasterizerState *mpRS;

	VDDisplayNodeContext3D *mpDCtx;

	vdlist<VDDisplayCachedImage3D> mCachedImages;

	struct Context {
		uint32 mColor;
	};

	typedef vdfastvector<Context> ContextStack;
	ContextStack mContextStack;
	
	struct Viewport {
		vdrect32 mScissor;
		sint32 mOffsetX;
		sint32 mOffsetY;
	};

	vdfastvector<Viewport> mViewportStack;

	VDDisplayTextRenderer mTextRenderer;
};

#endif
