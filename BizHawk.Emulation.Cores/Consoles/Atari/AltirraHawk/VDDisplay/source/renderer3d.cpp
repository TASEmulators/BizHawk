#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/bitmath.h>
#include <vd2/Kasumi/pixmapops.h>
#include "renderer3d.h"

extern const VDTDataView g_VDDispVPView_RenderFill;
extern const VDTDataView g_VDDispVPView_RenderBlit;
extern const VDTDataView g_VDDispFPView_RenderFill;
extern const VDTDataView g_VDDispFPView_RenderBlit;
extern const VDTDataView g_VDDispFPView_RenderBlitRBSwap;
extern const VDTDataView g_VDDispFPView_RenderBlitDirect;
extern const VDTDataView g_VDDispFPView_RenderBlitDirectRBSwap;
extern const VDTDataView g_VDDispFPView_RenderBlitStencil;
extern const VDTDataView g_VDDispFPView_RenderBlitStencilRBSwap;
extern const VDTDataView g_VDDispFPView_RenderBlitColor;
extern const VDTDataView g_VDDispFPView_RenderBlitColorRBSwap;
extern const VDTDataView g_VDDispFPView_RenderBlitColor2;
extern const VDTDataView g_VDDispFPView_RenderBlitColor2RBSwap;

VDDisplayCachedImage3D::VDDisplayCachedImage3D() {
	mListNodePrev = NULL;
	mListNodeNext = NULL;
}

VDDisplayCachedImage3D::~VDDisplayCachedImage3D() {
	if (mListNodePrev)
		vdlist_base::unlink(*this);
}

void *VDDisplayCachedImage3D::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayCachedImage3D::Init(IVDTContext& ctx, void *owner, const VDDisplayImageView& imageView) {
	mpHiBltNode.clear();

	const VDPixmap& px = imageView.GetImage();
	uint32 w = px.w;
	uint32 h = px.h;

	const VDTDeviceCaps& caps = ctx.GetDeviceCaps();
	if (!caps.mbNonPow2Conditional) {
		w = VDCeilToPow2(w);
		h = VDCeilToPow2(h);
	}

	if (w > caps.mMaxTextureWidth || h > caps.mMaxTextureHeight)
		return false;

	if (!ctx.CreateTexture2D(w, h, ctx.IsFormatSupportedTexture2D(kVDTF_B8G8R8A8) ? kVDTF_B8G8R8A8 : kVDTF_R8G8B8A8, 1, kVDTUsage_Default, NULL, ~mpTexture))
		return false;

	mWidth = px.w;
	mHeight = px.h;
	mTexWidth = w;
	mTexHeight = h;
	mpOwner = owner;
	mUniquenessCounter = imageView.GetUniquenessCounter() - 2;

	Update(imageView);
	return true;
}

void VDDisplayCachedImage3D::Shutdown() {
	mpTexture.clear();
	mpOwner = NULL;
	mpHiBltNode.clear();
}

void VDDisplayCachedImage3D::Update(const VDDisplayImageView& imageView) {
	uint32 newCounter = imageView.GetUniquenessCounter();

	mUniquenessCounter = newCounter;

	if (mpTexture) {
		const VDPixmap& px = imageView.GetImage();

		const uint32 numRects = imageView.GetDirtyListSize();

		VDTLockData2D lockData;
		if (mpTexture->Lock(0, NULL, lockData)) {
			VDPixmap dst = {};
			dst.format = nsVDPixmap::kPixFormat_XRGB8888;
			dst.w = mTexWidth;
			dst.h = mTexHeight;
			dst.pitch = lockData.mPitch;
			dst.data = lockData.mpData;

			VDPixmapBlt(dst, px);

			mpTexture->Unlock(0);
		}
	}
}

///////////////////////////////////////////////////////////////////////////

VDDisplayRenderer3D::VDDisplayRenderer3D()
	: mColor(0)
	, mNativeColor(0)
	, mVBOffset(0)
	, mpContext(NULL)
	, mpVPFill(NULL)
	, mpVPBlit(NULL)
	, mpVFFill(NULL)
	, mpVFBlit(NULL)
	, mpFPFill(NULL)
	, mpFPBlit(NULL)
	, mpFPBlitDirect(NULL)
	, mpFPBlitStencil(NULL)
	, mpFPBlitColor(NULL)
	, mpFPBlitColor2(NULL)
	, mpVB(NULL)
	, mpIB(NULL)
	, mpSS(NULL)
	, mpSSPoint(NULL)
	, mpBS(NULL)
	, mpBSStencil(NULL)
	, mpBSColor(NULL)
	, mpRS(NULL)
	, mpDCtx(NULL)
{
}

bool VDDisplayRenderer3D::Init(IVDTContext& ctx) {
	mpContext = &ctx;

	bool rbswap = !ctx.IsFormatSupportedTexture2D(kVDTF_B8G8R8A8);

	if (!ctx.CreateVertexProgram(kVDTPF_MultiTarget, g_VDDispVPView_RenderFill, &mpVPFill) ||
		!ctx.CreateVertexProgram(kVDTPF_MultiTarget, g_VDDispVPView_RenderBlit, &mpVPBlit)) {
		Shutdown();
		return false;
	}

	static const VDTVertexElement kFillVertexFormat[]={
		{ offsetof(FillVertex, x), kVDTET_Float2, kVDTEU_Position, 0 },
		{ offsetof(FillVertex, c), kVDTET_UByte4N, kVDTEU_Color, 0 },
	};

	static const VDTVertexElement kBlitVertexFormat[]={
		{ offsetof(BlitVertex, x), kVDTET_Float2, kVDTEU_Position, 0 },
		{ offsetof(BlitVertex, c), kVDTET_UByte4N, kVDTEU_Color, 0 },
		{ offsetof(BlitVertex, u), kVDTET_Float2, kVDTEU_TexCoord, 0 },
	};

	uint16 indices[256 * 6];

	for(int i=0; i<256; ++i) {
		indices[i*6+0] = i*4+0;
		indices[i*6+1] = i*4+1;
		indices[i*6+2] = i*4+2;
		indices[i*6+3] = i*4+2;
		indices[i*6+4] = i*4+1;
		indices[i*6+5] = i*4+3;
	}

	VDTSamplerStateDesc ssdesc = {};
	ssdesc.mAddressU = kVDTAddr_Clamp;
	ssdesc.mAddressV = kVDTAddr_Clamp;
	ssdesc.mAddressW = kVDTAddr_Clamp;
	ssdesc.mFilterMode = kVDTFilt_BilinearMip;

	VDTSamplerStateDesc ssdesc2 = {};
	ssdesc2.mAddressU = kVDTAddr_Clamp;
	ssdesc2.mAddressV = kVDTAddr_Clamp;
	ssdesc2.mAddressW = kVDTAddr_Clamp;
	ssdesc2.mFilterMode = kVDTFilt_Point;

	VDTBlendStateDesc bsdesc = {};
	bsdesc.mbEnable = true;
	bsdesc.mSrc = kVDTBlend_SrcAlpha;
	bsdesc.mDst = kVDTBlend_InvSrcAlpha;
	bsdesc.mOp = kVDTBlendOp_Add;

	VDTBlendStateDesc bssdesc = {};
	bssdesc.mbEnable = true;
	bssdesc.mSrc = kVDTBlend_One;
	bssdesc.mDst = kVDTBlend_InvSrcAlpha;
	bssdesc.mOp = kVDTBlendOp_Add;

	VDTBlendStateDesc bscdesc = {};
	bscdesc.mbEnable = true;
	bscdesc.mSrc = kVDTBlend_SrcAlpha;		// (alpha_r, 0, 0) * color_r
	bscdesc.mDst = kVDTBlend_InvSrcColor;	// dest_r * (1 - alpha_r, 1, 1)
	bscdesc.mOp = kVDTBlendOp_Add;

	VDTRasterizerStateDesc rsdesc = {};
	rsdesc.mbEnableScissor = true;
	rsdesc.mCullMode = kVDTCull_None;

	if (!ctx.CreateVertexFormat(kFillVertexFormat, 2, mpVPFill, &mpVFFill) ||
		!ctx.CreateVertexFormat(kBlitVertexFormat, 3, mpVPBlit, &mpVFBlit) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, g_VDDispFPView_RenderFill, &mpFPFill) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, rbswap ? g_VDDispFPView_RenderBlitRBSwap : g_VDDispFPView_RenderBlit, &mpFPBlit) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, rbswap ? g_VDDispFPView_RenderBlitDirectRBSwap : g_VDDispFPView_RenderBlitDirect, &mpFPBlitDirect) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, rbswap ? g_VDDispFPView_RenderBlitStencilRBSwap : g_VDDispFPView_RenderBlitStencil, &mpFPBlitStencil) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, rbswap ? g_VDDispFPView_RenderBlitColorRBSwap : g_VDDispFPView_RenderBlitColor, &mpFPBlitColor) ||
		!ctx.CreateFragmentProgram(kVDTPF_MultiTarget, rbswap ? g_VDDispFPView_RenderBlitColor2RBSwap : g_VDDispFPView_RenderBlitColor2, &mpFPBlitColor2) ||
		!ctx.CreateVertexBuffer(kVBSize, true, NULL, &mpVB) ||
		!ctx.CreateIndexBuffer(256 * 6, false, false, indices, &mpIB) ||
		!ctx.CreateSamplerState(ssdesc, &mpSS) ||
		!ctx.CreateSamplerState(ssdesc2, &mpSSPoint) ||
		!ctx.CreateBlendState(bsdesc, &mpBS) ||
		!ctx.CreateBlendState(bssdesc, &mpBSStencil) ||
		!ctx.CreateBlendState(bscdesc, &mpBSColor) ||
		!ctx.CreateRasterizerState(rsdesc, &mpRS)
		)
	{
		Shutdown();
		return false;
	}

	mVBOffset = 0;

	mTextRenderer.Init(this, 256, 256, true);
	return true;
}

void VDDisplayRenderer3D::Shutdown() {
	while(!mCachedImages.empty()) {
		VDDisplayCachedImage3D *img = mCachedImages.front();
		mCachedImages.pop_front();

		img->mListNodePrev = NULL;
		img->mListNodeNext = NULL;

		img->Shutdown();
	}

	vdsaferelease <<=
		mpRS,
		mpBSColor,
		mpBSStencil,
		mpBS,
		mpSSPoint,
		mpSS,
		mpIB,
		mpVB,
		mpFPBlitColor,
		mpFPBlitColor2,
		mpFPBlitStencil,
		mpFPBlitDirect,
		mpFPBlit,
		mpFPFill,
		mpVFBlit,
		mpVFFill,
		mpVPBlit,
		mpVPFill;
}

void VDDisplayRenderer3D::Begin(int w, int h, VDDisplayNodeContext3D& dctx) {
	mWidth = w;
	mHeight = h;
	mpDCtx = &dctx;

	ApplyBaselineState();

	VDTViewport vp;
	vp.mX = 0;
	vp.mY = 0;
	vp.mWidth = w;
	vp.mHeight = h;
	vp.mMinZ = 0;
	vp.mMaxZ = 1;
	mpContext->SetViewport(vp);
	mpContext->SetScissorRect(vdrect32(0, 0, w, h));

	mClipRect.set(0, 0, w, h);
	mOffsetX = 0;
	mOffsetY = 0;
}

void VDDisplayRenderer3D::End() {
	mpContext->SetIndexStream(NULL);
	mpContext->SetRasterizerState(NULL);
	mpContext->SetBlendState(NULL);

	mpDCtx = NULL;
}

const VDDisplayRendererCaps& VDDisplayRenderer3D::GetCaps() {
	static const VDDisplayRendererCaps kCaps={
		true
	};

	return kCaps;
}

void VDDisplayRenderer3D::SetColorRGB(uint32 color) {
	mColor = color;
	mNativeColor = (VDSwizzleU32(color) >> 8) | 0xFF000000;
}

void VDDisplayRenderer3D::FillRect(sint32 x, sint32 y, sint32 w, sint32 h) {
	if ((w|h) < 0)
		return;

	x += mOffsetX;
	y += mOffsetY;

	FillVertex v[4] = {
		{ (float)x, (float)y, mNativeColor },
		{ (float)x, (float)(y + h), mNativeColor },
		{ (float)(x + w), (float)y, mNativeColor },
		{ (float)(x + w), (float)(y + h), mNativeColor },
	};

	AddQuads(v, 1, false);
}

void VDDisplayRenderer3D::MultiFillRect(const vdrect32 *rects, uint32 n) {
	FillVertex v[256];
	int i = 0;

	while(n--) {
		const vdrect32& r = *rects++;
		float x1 = (float)r.left + mOffsetX;
		float y1 = (float)r.top + mOffsetY;
		float x2 = (float)r.right + mOffsetX;
		float y2 = (float)r.bottom + mOffsetY;

		v[i].x = x1;
		v[i].y = y1;
		v[i].c = mNativeColor;
		++i;

		v[i].x = x1;
		v[i].y = y2;
		v[i].c = mNativeColor;
		++i;

		v[i].x = x2;
		v[i].y = y1;
		v[i].c = mNativeColor;
		++i;

		v[i].x = x2;
		v[i].y = y2;
		v[i].c = mNativeColor;
		++i;

		if (i >= 256) {
			AddQuads(v, i >> 2, false);
			i = 0;
		}
	}

	if (i)
		AddQuads(v, i >> 2, false);
}

void VDDisplayRenderer3D::AlphaFillRect(sint32 x, sint32 y, sint32 w, sint32 h, uint32 alphaColor) {
	alphaColor = VDRotateRightU32(VDSwizzleU32(alphaColor), 8);

	x += mOffsetX;
	y += mOffsetY;

	FillVertex v[4] = {
		{ (float)x, (float)y, alphaColor },
		{ (float)x, (float)(y + h), alphaColor },
		{ (float)(x + w), (float)y, alphaColor },
		{ (float)(x + w), (float)(y + h), alphaColor },
	};

	AddQuads(v, 1, true);
}

void VDDisplayRenderer3D::AlphaTriStrip(const vdfloat2 *pts, uint32 numPts, uint32 alphaColor) {
	if (!numPts)
		return;
	
	alphaColor = VDRotateRightU32(VDSwizzleU32(alphaColor), 8);

	// Even batch counts are easier as then we don't have to worry about flipping polarity.
	constexpr uint32 maxBatchPts = (kVBSize / sizeof(FillVertex)) & ~1;

	vdblock<FillVertex> vertices(numPts);
	for(uint32 i=0; i<numPts; ++i) {
		vertices[i] = FillVertex { pts[i].x, pts[i].y, alphaColor };
	}

	const FillVertex *src = vertices.data();
	while(numPts >= 3) {
		const uint32 numBatchPts = std::min(numPts, maxBatchPts);
		uint32 vtxbytes = sizeof(FillVertex) * numBatchPts;

		if (kVBSize - mVBOffset < vtxbytes)
			mVBOffset = 0;

		if (mpVB->Load(mVBOffset, vtxbytes, src)) {
			mpContext->SetBlendState(mpBS);
			mpContext->SetVertexFormat(mpVFFill);
			mpContext->SetVertexProgram(mpVPFill);
			mpContext->SetFragmentProgram(mpFPFill);
			mpContext->SetVertexStream(0, mpVB, mVBOffset, sizeof(FillVertex));
			mpContext->DrawPrimitive(kVDTPT_TriangleStrip, 0, numBatchPts - 2);

			mVBOffset += vtxbytes;
		}

		// if we reached the end, we're done
		if (numBatchPts >= numPts)
			break;

		// overlap last two points to maintain tristrip continuity
		src += numBatchPts - 2;
		numPts -= numBatchPts - 2;
	}
}

void VDDisplayRenderer3D::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView) {
	const VDPixmap& px = imageView.GetImage();

	Blt(x, y, imageView, 0, 0, px.w, px.h);
}

void VDDisplayRenderer3D::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h) {
	if (w <= 0 || h <= 0)
		return;

	VDDisplayCachedImage3D *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	const float invtexw = 1.0f / (float)cachedImage->mTexWidth;
	const float invtexh = 1.0f / (float)cachedImage->mTexHeight;

	IVDTTexture *tex = cachedImage->mpTexture;
	mpContext->SetTextures(0, 1, &tex);
	mpContext->SetSamplerStates(0, 1, &mpSS);

	const float x0 = (float)(x + mOffsetX);
	const float y0 = (float)(y + mOffsetY);
	const float x1 = x0 + (float)w;
	const float y1 = y0 + (float)h;
	const float u0 = (float)sx * invtexw;
	const float v0 = (float)sy * invtexh;
	const float u1 = u0 + (float)w * invtexw;
	const float v1 = v0 + (float)h * invtexh;

	const BlitVertex v[4] = {
		{ x0, y0, 0xFFFFFFFFU, u0, v0 },
		{ x0, y1, 0xFFFFFFFFU, u0, v1 },
		{ x1, y0, 0xFFFFFFFFU, u1, v0 },
		{ x1, y1, 0xFFFFFFFFU, u1, v1 },
	};

	AddQuads(v, 1, kBltMode_Normal);
}

void VDDisplayRenderer3D::StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts) {
	if (dw <= 0 || dh <= 0)
		return;

	if (sw <= 0 || sh <= 0)
		return;

	VDDisplayCachedImage3D *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	if (sx < 0 || sy < 0 || sx >= cachedImage->mWidth || sy >= cachedImage->mHeight)
		return;

	if (cachedImage->mWidth - sx < sw || cachedImage->mHeight - sy < sh)
		return;

	const float invtexw = 1.0f / (float)cachedImage->mTexWidth;
	const float invtexh = 1.0f / (float)cachedImage->mTexHeight;

	// check if we need to create or reinit a hi-blt node
	if (opts.mFilterMode == VDDisplayBltOptions::kFilterMode_Bilinear && opts.mSharpnessX > 0) {
		const vdrect32 srcRect(sx, sy, sx + sw, sy + sh);

		float sharpX = opts.mSharpnessX;
		float sharpY = opts.mSharpnessY;

		if (sharpX <= 0) {
			sharpX = 1.0f;
			sharpY = 1.0f;
		}

		if (!cachedImage->mpHiBltNode || cachedImage->mHiBltSrcRect != srcRect || cachedImage->mHiBltSharpnessX != sharpX || cachedImage->mHiBltSharpnessY != sharpY)
			cachedImage->mpHiBltNode = NULL;

		if (!cachedImage->mpHiBltNode) {
			cachedImage->mHiBltSharpnessX = sharpX;
			cachedImage->mHiBltSharpnessY = sharpY;
			cachedImage->mHiBltSrcRect = srcRect;

			// create source node
			vdrefptr<VDDisplayTextureSourceNode3D> src(new VDDisplayTextureSourceNode3D);
			VDDisplaySourceTexMapping mapping = {};

			mapping.Init(cachedImage->mWidth, cachedImage->mHeight, cachedImage->mTexWidth, cachedImage->mTexHeight, !mpContext->IsFormatSupportedTexture2D(kVDTF_B8G8R8A8));

			if (src->Init(cachedImage->mpTexture, mapping)) {
				// create blit node
				vdrefptr<VDDisplayBlitNode3D> blitNode(new VDDisplayBlitNode3D);
				blitNode->SetDestArea(dx, dy, dw, dh);

				if (blitNode->Init(*mpContext, *mpDCtx, cachedImage->mWidth, cachedImage->mHeight, true, sharpX, sharpY, src)) {
					cachedImage->mpHiBltNode.swap(blitNode);
				}
			}
		}

		if (cachedImage->mpHiBltNode) {
			cachedImage->mpHiBltNode->SetDestArea(dx, dy, dw, dh);
			cachedImage->mpHiBltNode->Draw(*mpContext, *mpDCtx);
			ApplyBaselineState();
			return;
		}
	}

	IVDTTexture *tex = cachedImage->mpTexture;
	mpContext->SetTextures(0, 1, &tex);
	mpContext->SetSamplerStates(0, 1, opts.mFilterMode == VDDisplayBltOptions::kFilterMode_Bilinear ? &mpSS : &mpSSPoint);

	const float x0 = (float)(dx + mOffsetX);
	const float y0 = (float)(dy + mOffsetY);
	const float x1 = x0 + (float)dw;
	const float y1 = y0 + (float)dh;
	const float u0 = (float)sx * invtexw;
	const float v0 = (float)sy * invtexh;
	const float u1 = u0 + (float)sw * invtexw;
	const float v1 = v0 + (float)sh * invtexh;

	const BlitVertex v[4] = {
		{ x0, y0, 0xFFFFFFFFU, u0, v0 },
		{ x0, y1, 0xFFFFFFFFU, u0, v1 },
		{ x1, y0, 0xFFFFFFFFU, u1, v0 },
		{ x1, y1, 0xFFFFFFFFU, u1, v1 },
	};

	AddQuads(v, 1, kBltMode_Normal);
}

void VDDisplayRenderer3D::MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode) {
	if (!n)
		return;

	VDDisplayCachedImage3D *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	const float invtexw = 1.0f / (float)cachedImage->mTexWidth;
	const float invtexh = 1.0f / (float)cachedImage->mTexHeight;

	BlitVertex v[256];
	int i = 0;

	IVDTTexture *tex = cachedImage->mpTexture;
	mpContext->SetTextures(0, 1, &tex);
	mpContext->SetSamplerStates(0, 1, &mpSS);

	for(int pass=0; pass<3; ++pass) {
		uint32 color = mNativeColor;

		if (bltMode == kBltMode_Color || bltMode == kBltMode_Color2) {
			switch(pass) {
				case 0:
					color = 0x0000ff + ((mNativeColor & 0xff) << 24);
					break;

				case 1:
					color = 0x00ff00 + ((mNativeColor & 0xff00) << 16);
					break;

				case 2:
					color = 0xff0000 + ((mNativeColor & 0xff0000) << 8);
					break;
			}
		}

		for(uint32 j=0; j<n; ++j) {
			const VDDisplayBlt& blt = blts[j];
			float w = (float)blt.mWidth;
			float h = (float)blt.mHeight;
			float x0 = (float)blt.mDestX + mOffsetX;
			float y0 = (float)blt.mDestY + mOffsetY;
			float x1 = x0 + w;
			float y1 = y0 + h;
			float u0 = (float)blt.mSrcX * invtexw;
			float v0 = (float)blt.mSrcY * invtexh;
			float u1 = u0 + w * invtexw;
			float v1 = v0 + h * invtexh;

			v[i].x = x0;
			v[i].y = y0;
			v[i].c = color;
			v[i].u = u0;
			v[i].v = v0;
			++i;

			v[i].x = x0;
			v[i].y = y1;
			v[i].c = color;
			v[i].u = u0;
			v[i].v = v1;
			++i;

			v[i].x = x1;
			v[i].y = y0;
			v[i].c = color;
			v[i].u = u1;
			v[i].v = v0;
			++i;

			v[i].x = x1;
			v[i].y = y1;
			v[i].c = color;
			v[i].u = u1;
			v[i].v = v1;
			++i;

			if (i >= 256) {
				AddQuads(v, i >> 2, bltMode);
				i = 0;
			}
		}

		if (pass == 0 && bltMode != kBltMode_Color && bltMode != kBltMode_Color2)
			break;
	}

	if (i)
		AddQuads(v, i >> 2, bltMode);
}

void VDDisplayRenderer3D::PolyLine(const vdpoint32 *points, uint32 numLines) {
	if (!numLines)
		return;

	FillVertex v[256];
	int i = 0;

	do {
		v[i].x = (float)(points->x + mOffsetX + 0.5f);
		v[i].y = (float)(points->y + mOffsetY + 0.5f);
		v[i].c = mNativeColor;
		++i;
		++points;

		if (i >= vdcountof(v)) {
			AddLineStrip(v, i - 1, false);
			v[0] = v[i - 1];
			i = 1;
		}
	} while(numLines--);

	if (i > 1)
		AddLineStrip(v, i - 1, false);
}

bool VDDisplayRenderer3D::PushViewport(const vdrect32& r, sint32 x, sint32 y) {
	vdrect32 scissor(r);

	scissor.translate(mOffsetX, mOffsetY);

	if (scissor.left < mClipRect.left)
		scissor.left = mClipRect.left;

	if (scissor.top < mClipRect.top)
		scissor.top = mClipRect.top;

	if (scissor.right > mClipRect.right)
		scissor.right = mClipRect.right;

	if (scissor.bottom > mClipRect.bottom)
		scissor.bottom = mClipRect.bottom;

	if (scissor.empty())
		return NULL;

	Viewport& vp = mViewportStack.push_back();
	vp.mOffsetX = mOffsetX;
	vp.mOffsetY = mOffsetY;
	vp.mScissor = mClipRect;

	mpContext->SetScissorRect(scissor);

	mClipRect = scissor;
	mOffsetX += x;
	mOffsetY += y;

	return true;
}

void VDDisplayRenderer3D::PopViewport() {
	const Viewport& vp = mViewportStack.back();
	mClipRect = vp.mScissor;
	mOffsetX = vp.mOffsetX;
	mOffsetY = vp.mOffsetY;

	mpContext->SetScissorRect(mClipRect);

	mViewportStack.pop_back();
}

IVDDisplayRenderer *VDDisplayRenderer3D::BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache) {
	if (!PushViewport(r, r.left, r.top))
		return NULL;

	Context& c = mContextStack.push_back();
	c.mColor = mColor;

	SetColorRGB(0);
	return this;
}

void VDDisplayRenderer3D::EndSubRender() {
	const Context& c = mContextStack.back();

	SetColorRGB(c.mColor);

	mContextStack.pop_back();

	PopViewport();
}

void VDDisplayRenderer3D::AddLines(const FillVertex *p, uint32 n, bool alpha) {
	uint32 vtxbytes = sizeof(FillVertex) * n * 2;

	if (kVBSize - mVBOffset < vtxbytes)
		mVBOffset = 0;

	if (mpVB->Load(mVBOffset, vtxbytes, p)) {
		mpContext->SetBlendState(alpha ? mpBS : NULL);
		mpContext->SetVertexFormat(mpVFFill);
		mpContext->SetVertexProgram(mpVPFill);
		mpContext->SetFragmentProgram(mpFPFill);
		mpContext->SetVertexStream(0, mpVB, mVBOffset, sizeof(FillVertex));
		mpContext->DrawPrimitive(kVDTPT_Lines, 0, n);

		mVBOffset += vtxbytes;
	}
}

void VDDisplayRenderer3D::AddLineStrip(const FillVertex *p, uint32 n, bool alpha) {
	uint32 vtxbytes = sizeof(FillVertex) * (n + 1);

	if (kVBSize - mVBOffset < vtxbytes)
		mVBOffset = 0;

	if (mpVB->Load(mVBOffset, vtxbytes, p)) {
		mpContext->SetBlendState(alpha ? mpBS : NULL);
		mpContext->SetVertexFormat(mpVFFill);
		mpContext->SetVertexProgram(mpVPFill);
		mpContext->SetFragmentProgram(mpFPFill);
		mpContext->SetVertexStream(0, mpVB, mVBOffset, sizeof(FillVertex));
		mpContext->DrawPrimitive(kVDTPT_LineStrip, 0, n);

		mVBOffset += vtxbytes;
	}
}

void VDDisplayRenderer3D::AddQuads(const FillVertex *p, uint32 n, bool alpha) {
	uint32 vtxbytes = sizeof(FillVertex) * n * 4;

	if (kVBSize - mVBOffset < vtxbytes)
		mVBOffset = 0;

	if (mpVB->Load(mVBOffset, vtxbytes, p)) {
		mpContext->SetBlendState(alpha ? mpBS : NULL);
		mpContext->SetVertexFormat(mpVFFill);
		mpContext->SetVertexProgram(mpVPFill);
		mpContext->SetFragmentProgram(mpFPFill);
		mpContext->SetVertexStream(0, mpVB, mVBOffset, sizeof(FillVertex));
		mpContext->DrawIndexedPrimitive(kVDTPT_Triangles, 0, 0, n * 4, 0, n * 2);

		mVBOffset += vtxbytes;
	}
}

void VDDisplayRenderer3D::AddQuads(const BlitVertex *p, uint32 n, BltMode bltMode) {
	uint32 vtxbytes = sizeof(BlitVertex) * n * 4;

	if (kVBSize - mVBOffset < vtxbytes)
		mVBOffset = 0;

	if (mpVB->Load(mVBOffset, vtxbytes, p)) {
		mpContext->SetVertexFormat(mpVFBlit);
		mpContext->SetVertexProgram(mpVPBlit);
		mpContext->SetVertexStream(0, mpVB, mVBOffset, sizeof(BlitVertex));

		switch(bltMode) {
			case kBltMode_Normal:
				mpContext->SetFragmentProgram(mpFPBlit);
				mpContext->SetBlendState(NULL);
				break;

			case kBltMode_Stencil:
				mpContext->SetFragmentProgram(mpFPBlitStencil);
				mpContext->SetBlendState(mpBSStencil);
				break;

			case kBltMode_Color:
				mpContext->SetFragmentProgram(mpFPBlitColor);
				mpContext->SetBlendState(mpBSColor);
				break;

			case kBltMode_Color2:
				mpContext->SetFragmentProgram(mpFPBlitColor2);
				mpContext->SetBlendState(mpBSColor);
				break;
		}

		mpContext->DrawIndexedPrimitive(kVDTPT_Triangles, 0, 0, n * 4, 0, n * 2);

		mVBOffset += vtxbytes;
	}
}

VDDisplayCachedImage3D *VDDisplayRenderer3D::GetCachedImage(VDDisplayImageView& imageView) {
	VDDisplayCachedImage3D *cachedImage = static_cast<VDDisplayCachedImage3D *>(imageView.GetCachedImage(VDDisplayCachedImage3D::kTypeID));

	if (cachedImage && cachedImage->mpOwner != this)
		cachedImage = NULL;

	if (!cachedImage) {
		cachedImage = new_nothrow VDDisplayCachedImage3D;

		if (!cachedImage)
			return NULL;
		
		cachedImage->AddRef();
		if (!cachedImage->Init(*mpContext, this, imageView)) {
			cachedImage->Release();
			return NULL;
		}

		imageView.SetCachedImage(VDDisplayCachedImage3D::kTypeID, cachedImage);
		mCachedImages.push_back(cachedImage);

		cachedImage->Release();
	} else {
		uint32 c = imageView.GetUniquenessCounter();

		if (cachedImage->mUniquenessCounter != c)
			cachedImage->Update(imageView);
	}

	return cachedImage;
}

void VDDisplayRenderer3D::ApplyBaselineState() {
	float iw = 1.0f / (float)mWidth;
	float ih = 1.0f / (float)mHeight;

	const float trans2d[4]={
		2.0f * iw,
		-2.0f * ih,
		-1.0f,
		1.0f
	};

	mpContext->SetVertexProgramConstCount(2);
	mpContext->SetVertexProgramConstF(0, 1, trans2d);
	mpContext->SetIndexStream(mpIB);
	mpContext->SetBlendState(mpBS);
	mpContext->SetSamplerStates(0, 1, &mpSS);
	mpContext->SetRasterizerState(mpRS);
}