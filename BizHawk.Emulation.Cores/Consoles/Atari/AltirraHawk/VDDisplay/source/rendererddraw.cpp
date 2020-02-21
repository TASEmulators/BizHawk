#include <windows.h>
#include <ddraw.h>
#include <vd2/system/math.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/rendercache.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/internal/directdraw.h>
#include <vd2/VDDisplay/internal/rendererddraw.h>

class VDDisplayCachedImageDirectDraw : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayCachedImageDirectDraw(const VDDisplayCachedImageDirectDraw&);
	VDDisplayCachedImageDirectDraw& operator=(const VDDisplayCachedImageDirectDraw&);
public:
	enum { kTypeID = 'cimD' };

	VDDisplayCachedImageDirectDraw();
	~VDDisplayCachedImageDirectDraw();

	void *AsInterface(uint32 iid);

	bool Init(IVDDirectDrawManager *mgr, void *owner, const VDDisplayImageView& imageView, int surfaceFormat);
	void Shutdown();

	void Update(const VDDisplayImageView& imageView);
	void UpdateSoftware(const VDDisplayImageView& imageView);

public:
	void *mpOwner;
	vdrefptr<IDirectDrawSurface2> mpSurface;
	sint32	mWidth;
	sint32	mHeight;
	uint32	mUniquenessCounter;
	uint32	mUniquenessCounterSoftware;
	int		mSurfaceFormat;

	VDPixmapBuffer	mSoftBuffer;
};

VDDisplayCachedImageDirectDraw::VDDisplayCachedImageDirectDraw() {
	mListNodePrev = NULL;
	mListNodeNext = NULL;
}

VDDisplayCachedImageDirectDraw::~VDDisplayCachedImageDirectDraw() {
	if (mListNodePrev)
		vdlist_base::unlink(*this);
}

void *VDDisplayCachedImageDirectDraw::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayCachedImageDirectDraw::Init(IVDDirectDrawManager *mgr, void *owner, const VDDisplayImageView& imageView, int surfaceFormat) {
	const VDPixmap& px = imageView.GetImage();
	int w = px.w;
	int h = px.h;

	DDSURFACEDESC ddsd = {sizeof(DDSURFACEDESC)};

	ddsd.dwFlags				= DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;
	ddsd.dwWidth				= w;
	ddsd.dwHeight				= h;
	ddsd.ddsCaps.dwCaps			= DDSCAPS_OFFSCREENPLAIN;
	ddsd.ddpfPixelFormat		= mgr->GetPrimaryDesc().ddpfPixelFormat;

	vdrefptr<IDirectDrawSurface> surf;

	HRESULT hr = mgr->GetDDraw()->CreateSurface(&ddsd, ~surf, NULL);
	if (FAILED(hr))
		return false;

	hr = surf->QueryInterface(IID_IDirectDrawSurface2, (void **)~mpSurface);
	if (FAILED(hr))
		return false;

	mWidth = px.w;
	mHeight = px.h;
	mpOwner = owner;
	mSurfaceFormat = surfaceFormat;
	mUniquenessCounter = mUniquenessCounterSoftware = imageView.GetUniquenessCounter() - 2;

	return true;
}

void VDDisplayCachedImageDirectDraw::Shutdown() {
	mpSurface.clear();
	mpOwner = NULL;
}

void VDDisplayCachedImageDirectDraw::Update(const VDDisplayImageView& imageView) {
	uint32 newCounter = imageView.GetUniquenessCounter();
	bool partialUpdateOK = ((mUniquenessCounter + 1) == newCounter);
	mUniquenessCounter = newCounter;

	if (!mpSurface)
		return;

	HRESULT hr;
	if (mpSurface->IsLost()) {
		partialUpdateOK = false;

		hr = mpSurface->Restore();
		if (FAILED(hr))
			return;
	}

	const VDPixmap& px = imageView.GetImage();

	static const DWORD dwLockFlags = VDIsWindowsNT() ? DDLOCK_WRITEONLY | DDLOCK_WAIT : DDLOCK_WRITEONLY | DDLOCK_NOSYSLOCK | DDLOCK_WAIT;

	DDSURFACEDESC ddsd = {sizeof(DDSURFACEDESC)};
	hr = mpSurface->Lock(NULL, &ddsd, dwLockFlags, NULL);

	if (SUCCEEDED(hr)) {
		const uint32 numRects = imageView.GetDirtyListSize();

		VDPixmap dst = {};
		dst.format = mSurfaceFormat;
		dst.w = mWidth;
		dst.h = mHeight;
		dst.pitch = ddsd.lPitch;
		dst.data = ddsd.lpSurface;

		if (numRects && partialUpdateOK) {
			const vdrect32 *rects = imageView.GetDirtyList();

			for(uint32 i=0; i<numRects; ++i) {
				const vdrect32& r = rects[i];

				VDPixmapBlt(dst, r.left, r.top, px, r.left, r.top, r.width(), r.height());
			}
		} else {
			VDPixmapBlt(dst, px);
		}

		mpSurface->Unlock(NULL);
	}
}

void VDDisplayCachedImageDirectDraw::UpdateSoftware(const VDDisplayImageView& imageView) {
	uint32 newCounter = imageView.GetUniquenessCounter();
	bool partialUpdateOK = ((mUniquenessCounterSoftware + 1) == newCounter);
	mUniquenessCounterSoftware = newCounter;

	const VDPixmap& px = imageView.GetImage();

	mSoftBuffer.init(px.w, px.h, mSurfaceFormat);

	const uint32 numRects = imageView.GetDirtyListSize();

	if (numRects && partialUpdateOK) {
		const vdrect32 *rects = imageView.GetDirtyList();

		for(uint32 i=0; i<numRects; ++i) {
			const vdrect32& r = rects[i];

			VDPixmapBlt(mSoftBuffer, r.left, r.top, px, r.left, r.top, r.width(), r.height());
		}
	} else {
		VDPixmapBlt(mSoftBuffer, px);
	}
}

///////////////////////////////////////////////////////////////////////////

VDDisplayRendererDirectDraw::VDDisplayRendererDirectDraw()
	: mpddman(NULL)
	, mpddsComposition(NULL)
	, mWidth(0)
	, mHeight(0)
	, mColor(0)
	, mNativeColor(0)
	, mColorFormat(nsVDPixmap::kPixFormat_Null)
	, mpCurrentSubRender(NULL)
{
}

VDDisplayRendererDirectDraw::~VDDisplayRendererDirectDraw() {
}

void VDDisplayRendererDirectDraw::Init(IVDDirectDrawManager *ddman) {
	mpddman = ddman;
	VDVERIFY(mpddman->Init(this));

	mLockedPrimary.data = NULL;
	
	mFallback.Init();
}

void VDDisplayRendererDirectDraw::Shutdown() {
	while(!mCachedImages.empty()) {
		VDDisplayCachedImageDirectDraw *img = mCachedImages.front();
		mCachedImages.pop_front();

		img->mListNodePrev = NULL;
		img->mListNodeNext = NULL;

		img->Shutdown();
	}

	vdsaferelease <<= mpddsComposition;

	if (mpddman) {
		mpddman->Shutdown(this);
		mpddman = NULL;
	}
}

bool VDDisplayRendererDirectDraw::Begin(uint32 w, uint32 h, nsVDPixmap::VDPixmapFormat format) {
	if (mpddsComposition) {
		if (mWidth == w && mHeight == h && mColorFormat == format)
			return true;

		vdsaferelease <<= mpddsComposition;
	}

	mWidth = w;
	mHeight = h;
	mColorFormat = format;

	mClipRect.set(0, 0, w, h);
	mOffsetX = 0;
	mOffsetY = 0;

	DDSURFACEDESC ddsd = {sizeof(DDSURFACEDESC)};

	ddsd.dwFlags				= DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;
	ddsd.dwWidth				= w;
	ddsd.dwHeight				= h;
	ddsd.ddsCaps.dwCaps			= DDSCAPS_OFFSCREENPLAIN;
	ddsd.ddpfPixelFormat		= mpddman->GetPrimaryDesc().ddpfPixelFormat;

	IDirectDrawSurface *pdds = NULL;
	HRESULT hr = mpddman->GetDDraw()->CreateSurface(&ddsd, &pdds, NULL);
	if (FAILED(hr)) {
		VDDEBUG("VideoDriver/DDraw: Couldn't create offscreen composition surface\n");
		return false;
	}

	hr = pdds->QueryInterface(IID_IDirectDrawSurface2, (void **)&mpddsComposition);
	pdds->Release();

	if (FAILED(hr))
		return false;

	vdrefptr<IDirectDrawClipper> clipper;
	hr = mpddman->GetDDraw()->CreateClipper(0, ~clipper, NULL);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	struct RegionData {
		RGNDATAHEADER hdr;
		RECT r;
	} regiondata;
	
	regiondata.hdr.dwSize = sizeof(RGNDATAHEADER);
	regiondata.hdr.iType = RDH_RECTANGLES;
	regiondata.hdr.nCount = 1;
	regiondata.hdr.nRgnSize = 0;
	regiondata.hdr.rcBound.left = 0;
	regiondata.hdr.rcBound.top = 0;
	regiondata.hdr.rcBound.right = w;
	regiondata.hdr.rcBound.bottom = h;
	regiondata.r = regiondata.hdr.rcBound;

	hr = clipper->SetClipList((RGNDATA *)&regiondata, 0);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	hr = mpddsComposition->SetClipper(clipper);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	return true;
}

bool VDDisplayRendererDirectDraw::End(sint32 x, sint32 y) {
	if (!mpddsComposition)
		return false;

	VDASSERT(!mpCurrentSubRender);

	UnlockPrimary();

	RECT rDst = { x, y, x + mWidth, y + mHeight };
	HRESULT hr = mpddman->GetPrimary()->Blt(&rDst, mpddsComposition, NULL, DDBLT_ASYNC | DDBLT_WAIT, NULL);

	return SUCCEEDED(hr);
}

const VDDisplayRendererCaps& VDDisplayRendererDirectDraw::GetCaps() {
	static const VDDisplayRendererCaps kCaps = {
		false
	};

	return kCaps;
}

void VDDisplayRendererDirectDraw::SetColorRGB(uint32 color) {
	if (mColor == color)
		return;

	mColor = color;

	switch(mColorFormat) {
		case nsVDPixmap::kPixFormat_XRGB1555:
			mNativeColor = ((color & 0xf80000) >> 9) + ((color & 0xf800) >> 6) + ((color & 0xf8) >> 3);
			break;

		case nsVDPixmap::kPixFormat_RGB565:
			mNativeColor = ((color & 0xf80000) >> 8) + ((color & 0xfc00) >> 5) + ((color & 0xf8) >> 3);
			break;

		case nsVDPixmap::kPixFormat_RGB888:
		case nsVDPixmap::kPixFormat_XRGB8888:
			mNativeColor = color;
			break;
	}
}

void VDDisplayRendererDirectDraw::FillRect(sint32 x, sint32 y, sint32 w, sint32 h) {
	if (!mpddsComposition)
		return;

	x += mOffsetX;
	y += mOffsetY;

	RECT r = { x, y, x + w, y + h };

	if (r.left < mClipRect.left)
		r.left = mClipRect.left;

	if (r.top < mClipRect.top)
		r.top = mClipRect.top;

	if (r.right > mClipRect.right)
		r.right = mClipRect.right;

	if (r.bottom > mClipRect.bottom)
		r.bottom = mClipRect.bottom;

	if (r.left >= r.right || r.top >= r.bottom)
		return;

	UnlockPrimary();

	DDBLTFX fx = {sizeof(DDBLTFX)};
	fx.dwFillColor = mNativeColor;
	mpddsComposition->Blt(&r, NULL, NULL, DDBLT_COLORFILL | DDBLT_ASYNC | DDBLT_WAIT, &fx);
}

void VDDisplayRendererDirectDraw::MultiFillRect(const vdrect32 *rects, uint32 n) {
	if (!mpddsComposition || !n)
		return;

	DDBLTFX fx = {sizeof(DDBLTFX)};
	fx.dwFillColor = mNativeColor;

	while(n--) {
		vdrect32 r0 = *rects++;

		r0.translate(mOffsetX, mOffsetY);

		RECT r = { r0.left, r0.top, r0.right, r0.bottom };

		if (r.left < mClipRect.left)
			r.left = mClipRect.left;

		if (r.top < mClipRect.top)
			r.top = mClipRect.top;

		if (r.right > mClipRect.right)
			r.right = mClipRect.right;

		if (r.bottom > mClipRect.bottom)
			r.bottom = mClipRect.bottom;

		if (r.left >= r.right || r.top >= r.bottom)
			continue;

		UnlockPrimary();
		mpddsComposition->Blt(&r, NULL, NULL, DDBLT_COLORFILL | DDBLT_ASYNC | DDBLT_WAIT, &fx);
	}
}

void VDDisplayRendererDirectDraw::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView) {
	VDDisplayCachedImageDirectDraw *cachedImage = GetCachedImage(imageView, false);

	if (!cachedImage)
		return;

	Blt(x, y, imageView, 0, 0, cachedImage->mWidth, cachedImage->mHeight);
}

void VDDisplayRendererDirectDraw::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h) {
	VDDisplayCachedImageDirectDraw *cachedImage = GetCachedImage(imageView, false);

	if (!cachedImage)
		return;

	x += mOffsetX;
	y += mOffsetY;

	// do full clipping
	if ((w|h) < 0)
		return;

	// clip destination rect
	vdrect32 dst(x, y, x + w, y + h);

	if (dst.left < mClipRect.left) {
		sx += mClipRect.left - dst.left;
		dst.left = mClipRect.left;
	}

	if (dst.top < mClipRect.top) {
		sy += mClipRect.top - dst.top;
		dst.top = mClipRect.top;
	}

	if (dst.right > mClipRect.right)
		dst.right = mClipRect.right;

	if (dst.bottom > mClipRect.bottom)
		dst.bottom = mClipRect.bottom;

	// bail if we've dest-clipped out
	if (dst.empty())
		return;

	if (sx < 0) { dst.left -= sx; sx = 0; }
	if (sy < 0) { dst.top -= sy; sy = 0; }

	w = dst.width();
	h = dst.height();

	if ((w|h) < 0)
		return;

	if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - sx; }
	if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - sy; }

	if (w <= 0 || h <= 0)
		return;

	UnlockPrimary();

	RECT rDest = { dst.left, dst.top, dst.right, dst.bottom };
	RECT rSrc = { sx, sy, sx + w, sy + h };
	mpddsComposition->Blt(&rDest, cachedImage->mpSurface, &rSrc, DDBLT_ASYNC | DDBLT_WAIT, NULL);
}

void VDDisplayRendererDirectDraw::StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts) {
	VDDisplayCachedImageDirectDraw *cachedImage = GetCachedImage(imageView, false);

	if (!cachedImage)
		return;

	dx += mOffsetX;
	dy += mOffsetY;

	// do full clipping
	if (dw <= 0 || dh <= 0 || sw <= 0 || sh <= 0)
		return;

	// bail if there is source clipping
	if ((sx | sy) < 0)
		return;

	if (sx >= cachedImage->mWidth || cachedImage->mWidth - sx < sw)
		return;

	if (sy >= cachedImage->mHeight || cachedImage->mHeight - sy < sh)
		return;

	// clip destination rect
	const float factor_x = (float)sw / (float)dw;
	const float factor_y = (float)sh / (float)dh;
	float fsx1 = (float)sx;
	float fsy1 = (float)sy;
	float fsx2 = fsx1 + (float)sw;
	float fsy2 = fsy1 + (float)sh;

	vdrect32 dst(dx, dy, dx + dw, dy + dh);

	if (dst.left < mClipRect.left) {
		fsx1 += factor_x * (float)(mClipRect.left - dst.left);
		dst.left = mClipRect.left;
	}

	if (dst.top < mClipRect.top) {
		fsy1 += factor_y * (float)(mClipRect.top - dst.top);
		dst.top = mClipRect.top;
	}

	if (dst.right > mClipRect.right) {
		fsx2 += factor_x * (float)(mClipRect.right - dst.right);
		dst.right = mClipRect.right;
	}

	if (dst.bottom > mClipRect.bottom) {
		fsy2 += factor_y * (float)(mClipRect.bottom - dst.top);
		dst.bottom = mClipRect.bottom;
	}

	// bail if we've dest-clipped out
	if (dst.empty())
		return;

	UnlockPrimary();

	RECT rDest = { dst.left, dst.top, dst.right, dst.bottom };
	RECT rSrc = {
		VDFloorToInt(fsx1),
		VDFloorToInt(fsy1),
		VDCeilToInt(fsx2),
		VDCeilToInt(fsy2)
	};
	mpddsComposition->Blt(&rDest, cachedImage->mpSurface, &rSrc, DDBLT_ASYNC | DDBLT_WAIT, NULL);
}

void VDDisplayRendererDirectDraw::MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode) {
	if (LockPrimary() && mFallback.Begin(mLockedPrimary) && mFallback.PushViewport(mClipRect, mOffsetX, mOffsetY))
		mFallback.MultiBlt(blts, n, imageView, bltMode);
}

void VDDisplayRendererDirectDraw::PolyLine(const vdpoint32 *points, uint32 numLines) {
	if (!numLines)
		return;

	if (LockPrimary() && mFallback.Begin(mLockedPrimary) && mFallback.PushViewport(mClipRect, mOffsetX, mOffsetY)) {
		mFallback.SetColorRGB(mColor);
		mFallback.PolyLine(points, numLines);
	}
}

bool VDDisplayRendererDirectDraw::PushViewport(const vdrect32& r, sint32 x, sint32 y) {
	vdrect32 r2(r);

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

	mClipRect = r2;
	mOffsetX += x;
	mOffsetY += y;
	return true;
}

void VDDisplayRendererDirectDraw::PopViewport() {
	Viewport& vp = mViewportStack.back();
	mClipRect = vp.mClipRect;
	mOffsetX = vp.mOffsetX;
	mOffsetY = vp.mOffsetY;

	mViewportStack.pop_back();
}

IVDDisplayRenderer *VDDisplayRendererDirectDraw::BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache) {
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

	if (!pcr->mBuffer.base() || pcr->mBuffer.w != w || pcr->mBuffer.h != h || pcr->mBuffer.format != mColorFormat) {
		if (!pcr->Init(cache, w, h, mColorFormat))
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

void VDDisplayRendererDirectDraw::EndSubRender() {
	VDASSERT(mpCurrentSubRender);

	mFallback.EndSubRender();

	VDDisplayRenderCacheGeneric *pcr = vdpoly_cast<VDDisplayRenderCacheGeneric *>(mpCurrentSubRender->GetCache());

	if (pcr) {
		pcr->mImageView.Invalidate();
		Blt(mSubRenderX, mSubRenderY, pcr->mImageView);
	}

	mpCurrentSubRender = NULL;
}

void VDDisplayRendererDirectDraw::DirectDrawShutdown() {
	vdsaferelease <<= mpddsComposition;

	for(CachedImages::iterator it(mCachedImages.begin()), itEnd(mCachedImages.end()); it != itEnd; ++it) {
		VDDisplayCachedImageDirectDraw *img = *it;

		img->Shutdown();
	}

	mLockedPrimary.data = NULL;
}

void VDDisplayRendererDirectDraw::DirectDrawPrimaryRestored() {
}

bool VDDisplayRendererDirectDraw::LockPrimary() {
	if (!mLockedPrimary.data) {
		const DWORD dwLockFlags = VDIsWindowsNT() ? DDLOCK_WRITEONLY | DDLOCK_WAIT : DDLOCK_WRITEONLY | DDLOCK_NOSYSLOCK | DDLOCK_WAIT;

		DDSURFACEDESC ddsd = {sizeof(DDSURFACEDESC)};
		ddsd.ddpfPixelFormat.dwSize = sizeof(DDPIXELFORMAT);

		HRESULT hr = mpddsComposition->Lock(NULL, &ddsd, dwLockFlags, NULL);
		if (FAILED(hr))
			return false;

		mLockedPrimary.data = ddsd.lpSurface;
		mLockedPrimary.pitch = ddsd.lPitch;
		mLockedPrimary.w = ddsd.dwWidth;
		mLockedPrimary.h = ddsd.dwHeight;
		mLockedPrimary.format = mColorFormat;
	}

	return true;
}

void VDDisplayRendererDirectDraw::UnlockPrimary() {
	if (mLockedPrimary.data) {
		mLockedPrimary.data = NULL;

		mpddsComposition->Unlock(NULL);
	}
}

VDDisplayCachedImageDirectDraw *VDDisplayRendererDirectDraw::GetCachedImage(VDDisplayImageView& imageView, bool softrender) {
	VDDisplayCachedImageDirectDraw *cachedImage = static_cast<VDDisplayCachedImageDirectDraw *>(imageView.GetCachedImage(VDDisplayCachedImageDirectDraw::kTypeID));

	if (cachedImage && cachedImage->mpOwner != this)
		cachedImage = NULL;

	if (!cachedImage) {
		cachedImage = new_nothrow VDDisplayCachedImageDirectDraw;

		if (!cachedImage)
			return NULL;
		
		cachedImage->AddRef();
		if (!cachedImage->Init(mpddman, this, imageView, mColorFormat)) {
			cachedImage->Release();
			return NULL;
		}

		imageView.SetCachedImage(VDDisplayCachedImageDirectDraw::kTypeID, cachedImage);
		mCachedImages.push_back(cachedImage);

		cachedImage->Release();
	}

	uint32 c = imageView.GetUniquenessCounter();

	if (softrender) {
		if (cachedImage->mUniquenessCounterSoftware != c)
			cachedImage->UpdateSoftware(imageView);
	} else {
		if (cachedImage->mpSurface->IsLost() || cachedImage->mUniquenessCounter != c)
			cachedImage->Update(imageView);
	}

	return cachedImage;
}
