#include <windows.h>
#include <vd2/system/binary.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/rendercache.h>
#include <vd2/VDDisplay/internal/renderergdi.h>

///////////////////////////////////////////////////////////////////////////////
class VDDisplayCachedImageGDI : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayCachedImageGDI(const VDDisplayCachedImageGDI&);
	VDDisplayCachedImageGDI& operator=(const VDDisplayCachedImageGDI&);
public:
	enum { kTypeID = 'cimI' };

	VDDisplayCachedImageGDI();
	~VDDisplayCachedImageGDI();

	void *AsInterface(uint32 iid);

	bool Init(void *owner, const VDDisplayImageView& imageView);
	void Shutdown();

	void Update(const VDDisplayImageView& imageView);

public:
	void *mpOwner;
	HDC		mhdc;
	HBITMAP	mhbm;
	HGDIOBJ	mhbmOld;
	sint32	mWidth;
	sint32	mHeight;
	uint32	mUniquenessCounter;
};

VDDisplayCachedImageGDI::VDDisplayCachedImageGDI()
	: mpOwner(NULL)
	, mhdc(NULL)
	, mhbm(NULL)
	, mhbmOld(NULL)
	, mWidth(0)
	, mHeight(0)
{
	mListNodePrev = NULL;
	mListNodeNext = NULL;
}

VDDisplayCachedImageGDI::~VDDisplayCachedImageGDI() {
	if (mListNodePrev)
		vdlist_base::unlink(*this);
}

void *VDDisplayCachedImageGDI::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayCachedImageGDI::Init(void *owner, const VDDisplayImageView& imageView) {
	const VDPixmap& px = imageView.GetImage();
	int w = px.w;
	int h = px.h;

	HDC hdc = GetDC(NULL);
	if (hdc) {
		mhdc = CreateCompatibleDC(NULL);
		mhbm = CreateCompatibleBitmap(hdc, w, h);
	}

	if (!mhdc || !mhbm) {
		Shutdown();
		return false;
	}

	mhbmOld = SelectObject(mhdc, mhbm);

	mWidth = px.w;
	mHeight = px.h;
	mpOwner = owner;

	Update(imageView);
	return true;
}

void VDDisplayCachedImageGDI::Shutdown() {
	if (mhdc) {
		if (mhbmOld) {
			SelectObject(mhdc, mhbmOld);
			mhbmOld = NULL;
		}

		DeleteDC(mhdc);
	}

	if (mhbm) {
		DeleteObject(mhbm);
		mhbm = NULL;
	}

	mpOwner = NULL;
}

void VDDisplayCachedImageGDI::Update(const VDDisplayImageView& imageView) {
	mUniquenessCounter = imageView.GetUniquenessCounter();

	if (mhbm) {
		const VDPixmap& px = imageView.GetImage();

		VDPixmapLayout layout;
		VDPixmapCreateLinearLayout(layout, nsVDPixmap::kPixFormat_XRGB8888, mWidth, mHeight, 4);
		VDPixmapLayoutFlipV(layout);

		VDPixmapBuffer buf;
		buf.init(layout);

		VDPixmapBlt(buf, px);

		BITMAPINFO bi = {};
		bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bi.bmiHeader.biWidth = mWidth;
		bi.bmiHeader.biHeight = mHeight;
		bi.bmiHeader.biPlanes = 1;
		bi.bmiHeader.biBitCount = 32;
		bi.bmiHeader.biSizeImage = 0;
		bi.bmiHeader.biCompression = BI_RGB;

		VDVERIFY(::SetDIBits(mhdc, mhbm, 0, mHeight, buf.base(), &bi, DIB_RGB_COLORS));
	}
}

///////////////////////////////////////////////////////////////////////////////

VDDisplayRendererGDI::VDDisplayRendererGDI()
	: mhdc(NULL)
	, mSavedDC(0)
	, mPenColor(0)
	, mhPen((HPEN)::GetStockObject(BLACK_PEN))
	, mBrushColor(0)
	, mhBrush((HBRUSH)::GetStockObject(BLACK_BRUSH))
	, mWidth(0)
	, mHeight(0)
	, mpCurrentSubRender(NULL)
{
}

VDDisplayRendererGDI::~VDDisplayRendererGDI() {
}

void VDDisplayRendererGDI::Init() {
	mFallback.Init();
}

void VDDisplayRendererGDI::Shutdown() {
	while(!mCachedImages.empty()) {
		VDDisplayCachedImageGDI *img = mCachedImages.front();
		mCachedImages.pop_front();

		img->mListNodePrev = NULL;
		img->mListNodeNext = NULL;

		img->Shutdown();
	}
}

bool VDDisplayRendererGDI::Begin(HDC hdc, sint32 w, sint32 h) {
	mhdc = hdc;

	mSavedDC = SaveDC(mhdc);
	if (!mSavedDC)
		return false;

	mhPen = (HPEN)::GetStockObject(BLACK_PEN);
	mhBrush = (HBRUSH)::GetStockObject(BLACK_BRUSH);
	mPenColor = 0;
	mBrushColor = 0;

	mWidth = w;
	mHeight = h;

	mClipRect.set(0, 0, w, h);
	mOffsetX = 0;
	mOffsetY = 0;

	return true;
}

void VDDisplayRendererGDI::End() {
	VDASSERT(!mpCurrentSubRender);

	if (mSavedDC) {
		RestoreDC(mhdc, mSavedDC);
		mSavedDC = 0;
	}

	if (mhPen) {
		DeleteObject(mhPen);
		mhPen = NULL;
	}

	if (mhBrush) {
		DeleteObject(mhBrush);
		mhBrush = NULL;
	}
}

const VDDisplayRendererCaps& VDDisplayRendererGDI::GetCaps() {
	static const VDDisplayRendererCaps kCaps = {
		false
	};

	return kCaps;
}

void VDDisplayRendererGDI::SetColorRGB(uint32 color) {
	mColor = color;
}

void VDDisplayRendererGDI::FillRect(sint32 x, sint32 y, sint32 w, sint32 h) {
	if (w <= 0 || h <= 0)
		return;

	UpdateBrush();

	x += mOffsetX;
	y += mOffsetY;

	RECT r = { x, y, x + w, y + h };
	::FillRect(mhdc, &r, mhBrush);
}

void VDDisplayRendererGDI::MultiFillRect(const vdrect32 *rects, uint32 n) {
	if (!n)
		return;

	UpdateBrush();

	while(n--) {
		const vdrect32& r = *rects++;

		RECT r2 = { r.left + mOffsetX, r.top + mOffsetY, r.right + mOffsetX, r.bottom + mOffsetY };
		::FillRect(mhdc, &r2, mhBrush);
	}
}

void VDDisplayRendererGDI::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView) {
	VDDisplayCachedImageGDI *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	x += mOffsetX;
	y += mOffsetY;

	BitBlt(mhdc, x, y, cachedImage->mWidth, cachedImage->mHeight, cachedImage->mhdc, 0, 0, SRCCOPY);
}

void VDDisplayRendererGDI::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h) {
	VDDisplayCachedImageGDI *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	x += mOffsetX;
	y += mOffsetY;

	// do source clipping
	if (sx < 0) { x -= sx; w += sx; sx = 0; }
	if (sy < 0) { y -= sy; h += sy; sy = 0; }

	if ((w|h) < 0)
		return;

	if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - sx; }
	if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - sy; }

	if (w <= 0 || h <= 0)
		return;

	BitBlt(mhdc, x, y, w, h, cachedImage->mhdc, sx, sy, SRCCOPY);
}

void VDDisplayRendererGDI::StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts) {
	VDDisplayCachedImageGDI *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	dx += mOffsetX;
	dy += mOffsetY;

	// do source clipping
	if (sx < 0 || sx >= cachedImage->mWidth)
		return;

	if (sy < 0 || sy >= cachedImage->mHeight)
		return;

	if (cachedImage->mWidth - sx < sw || cachedImage->mHeight - sy < sh)
		return;

	if (sw <= 0 || sh <= 0)
		return;

	if (dw <= 0 || dh <= 0)
		return;

	::StretchBlt(mhdc, dx, dy, dw, dh, cachedImage->mhdc, sx, sy, sw, sh, SRCCOPY);
}

void VDDisplayRendererGDI::MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode) {
	if (!n)
		return;

	VDDisplayCachedImageGDI *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	UpdateBrush();
	SelectObject(mhdc, mhBrush);

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

		switch(bltMode) {
			case kBltMode_Normal:
				BitBlt(mhdc, x, y, w, h, cachedImage->mhdc, sx, sy, SRCCOPY);
				break;

			case kBltMode_Stencil:
				// Merge the brush into the destination through the source:
				//
				// ROP3 0x00E20746 = DSPDxax
				//
				//   result = ((pattern ^ dest) & source) ^ dest
				//
				BitBlt(mhdc, x, y, w, h, cachedImage->mhdc, sx, sy, 0x00E20746);
				break;
		}
	}
}

void VDDisplayRendererGDI::PolyLine(const vdpoint32 *points, uint32 numLines) {
	if (!numLines)
		return;

	UpdatePen();
	SelectObject(mhdc, mhPen);

	POINT pt[100];

	pt[0].x = points[0].x + mOffsetX;
	pt[0].y = points[0].y + mOffsetY;

	while(numLines) {
		uint32 n = std::min<uint32>(vdcountof(pt) - 1, numLines);
		numLines -= n;

		for(uint32 i=1; i<=n; ++i) {
			pt[i].x = points[i].x + mOffsetX;
			pt[i].y = points[i].y + mOffsetY;
		}

		points += n;

		::Polyline(mhdc, pt, n + 1);
		pt[0] = pt[n];
	}
}

bool VDDisplayRendererGDI::PushViewport(const vdrect32& r, sint32 x, sint32 y) {
	vdrect32 r2 = r;

	r2.translate(mOffsetX, mOffsetY);

	if (r2.left < mClipRect.left)
		r2.left = mClipRect.left;

	if (r2.top < mClipRect.top)
		r2.top = mClipRect.top;

	if (r2.right < mClipRect.right)
		r2.right = mClipRect.right;

	if (r2.bottom < mClipRect.bottom)
		r2.bottom = mClipRect.bottom;

	if (r2.empty())
		return false;

	Viewport& vp = mViewportStack.push_back();
	vp.mClipRect = mClipRect;
	vp.mOffsetX = mOffsetX;
	vp.mOffsetY = mOffsetY;

	mOffsetX += x;
	mOffsetY += y;

	mClipRect = r2;

	UpdateViewport();
	return true;
}

void VDDisplayRendererGDI::PopViewport() {
	Viewport& vp = mViewportStack.back();
	mClipRect = vp.mClipRect;
	mOffsetX = vp.mOffsetX;
	mOffsetY = vp.mOffsetY;
	
	mViewportStack.pop_back();

	UpdateViewport();
}

IVDDisplayRenderer *VDDisplayRendererGDI::BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache) {
	VDASSERT(!mpCurrentSubRender);

	VDDisplayRenderCacheGeneric *pcr = vdpoly_cast<VDDisplayRenderCacheGeneric *>(cache.GetCache());
	if (!pcr) {
		pcr = new_nothrow VDDisplayRenderCacheGeneric;
		if (!pcr)
			return NULL;

		cache.SetCache(pcr);
	}

	uint32 w = std::min<uint32>(r.width(), mWidth);
	uint32 h = std::min<uint32>(r.height(), mHeight);

	if (!pcr->mBuffer.base() || pcr->mBuffer.w != w || pcr->mBuffer.h != h || pcr->mBuffer.format != nsVDPixmap::kPixFormat_XRGB8888) {
		if (!pcr->Init(cache, w, h, nsVDPixmap::kPixFormat_XRGB8888))
			return NULL;
	}

	if (pcr->mUniquenessCounter == cache.GetUniquenessCounter()) {
		Blt(r.left, r.top, pcr->mImageView);
	} else {
		if (mFallback.Begin(pcr->mBuffer)) {
			IVDDisplayRenderer *rdr = mFallback.BeginSubRender(vdrect32(0, 0, r.width(), r.height()), cache);

			if (rdr) {
				mpCurrentSubRender = &cache;
				mSubRenderX = r.left;
				mSubRenderY = r.top;

				pcr->mUniquenessCounter = cache.GetUniquenessCounter();
				return rdr;
			}
		}
	}

	return NULL;
}

void VDDisplayRendererGDI::EndSubRender() {
	VDASSERT(mpCurrentSubRender);

	mFallback.EndSubRender();

	VDDisplayRenderCacheGeneric *pcr = vdpoly_cast<VDDisplayRenderCacheGeneric *>(mpCurrentSubRender->GetCache());

	if (pcr) {
		pcr->mImageView.Invalidate();
		Blt(mSubRenderX, mSubRenderY, pcr->mImageView);
	}

	mpCurrentSubRender = NULL;
}

void VDDisplayRendererGDI::UpdateViewport() {
	::SelectClipRgn(mhdc, NULL);

	if (mClipRect != vdrect32(0, 0, mWidth, mHeight))
		::IntersectClipRect(mhdc, mClipRect.left, mClipRect.top, mClipRect.right, mClipRect.bottom);
}

void VDDisplayRendererGDI::UpdatePen() {
	if (mPenColor != mColor) {
		mPenColor = mColor;

		HPEN hNewPen = CreatePen(PS_SOLID, 0, VDSwizzleU32(mColor) >> 8);
		if (hNewPen) {
			DeleteObject(mhPen);
			mhPen = hNewPen;
		}
	}
}

void VDDisplayRendererGDI::UpdateBrush() {
	if (mBrushColor != mColor) {
		mBrushColor = mColor;

		HBRUSH hNewBrush = CreateSolidBrush(VDSwizzleU32(mColor) >> 8);
		if (hNewBrush) {
			DeleteObject(mhBrush);
			mhBrush = hNewBrush;
		}
	}
}

VDDisplayCachedImageGDI *VDDisplayRendererGDI::GetCachedImage(VDDisplayImageView& imageView) {
	VDDisplayCachedImageGDI *cachedImage = static_cast<VDDisplayCachedImageGDI *>(imageView.GetCachedImage(VDDisplayCachedImageGDI::kTypeID));

	if (cachedImage && cachedImage->mpOwner != this)
		cachedImage = NULL;

	if (!cachedImage) {
		cachedImage = new_nothrow VDDisplayCachedImageGDI;

		if (!cachedImage)
			return NULL;
		
		cachedImage->AddRef();
		if (!cachedImage->Init(this, imageView)) {
			cachedImage->Release();
			return NULL;
		}

		imageView.SetCachedImage(VDDisplayCachedImageGDI::kTypeID, cachedImage);
		mCachedImages.push_back(cachedImage);

		cachedImage->Release();
	} else {
		uint32 c = imageView.GetUniquenessCounter();

		if (cachedImage->mUniquenessCounter != c)
			cachedImage->Update(imageView);
	}

	return cachedImage;
}
