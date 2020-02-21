#include <vd2/system/math.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/renderersoft.h>

class VDDisplayCachedImageSoft : public vdrefcounted<IVDRefUnknown>, public vdlist_node {
	VDDisplayCachedImageSoft(const VDDisplayCachedImageSoft&);
	VDDisplayCachedImageSoft& operator=(const VDDisplayCachedImageSoft&);
public:
	enum { kTypeID = 'cimS' };

	VDDisplayCachedImageSoft();
	~VDDisplayCachedImageSoft();

	void *AsInterface(uint32 iid);

	bool Init(const VDDisplayImageView& imageView, int surfaceFormat);
	void Shutdown();

	void Update(const VDDisplayImageView& imageView);
	void UpdateSoftware(const VDDisplayImageView& imageView);

public:
	sint32	mWidth;
	sint32	mHeight;
	uint32	mUniquenessCounter;
	int		mSurfaceFormat;

	VDPixmapBuffer	mSoftBuffer;
};

VDDisplayCachedImageSoft::VDDisplayCachedImageSoft() {
	mListNodePrev = NULL;
	mListNodeNext = NULL;
}

VDDisplayCachedImageSoft::~VDDisplayCachedImageSoft() {
	if (mListNodePrev)
		vdlist_base::unlink(*this);
}

void *VDDisplayCachedImageSoft::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return NULL;
}

bool VDDisplayCachedImageSoft::Init(const VDDisplayImageView& imageView, int surfaceFormat) {
	const VDPixmap& px = imageView.GetImage();

	mWidth = px.w;
	mHeight = px.h;
	mSurfaceFormat = surfaceFormat;
	mUniquenessCounter = imageView.GetUniquenessCounter() - 2;

	return true;
}

void VDDisplayCachedImageSoft::Shutdown() {
}

void VDDisplayCachedImageSoft::UpdateSoftware(const VDDisplayImageView& imageView) {
	uint32 newCounter = imageView.GetUniquenessCounter();
	bool partialUpdateOK = ((mUniquenessCounter + 1) == newCounter);
	mUniquenessCounter = newCounter;

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

namespace {
	void FillSpan16(void *dst0, uint32 x, uint32 c, uint32 n) {
		uint16 *dst = (uint16 *)dst0;

		do {
			dst[x++] = (uint16)c;
		} while(--n);
	}

	void FillSpan24(void *dst0, uint32 x, uint32 c, uint32 n) {
		uint8 *dst = (uint8 *)dst0;
		uint8 b = (uint8)(c >> 0);
		uint8 g = (uint8)(c >> 8);
		uint8 r = (uint8)(c >> 16);

		dst += x*3;

		do {
			dst[0] = b;
			dst[1] = g;
			dst[2] = r;
			dst += 3;
		} while(--n);
	}

	void FillSpan32(void *dst0, uint32 x, uint32 c, uint32 n) {
		uint32 *dst = (uint32 *)dst0;

		do {
			dst[x++] = c;
		} while(--n);
	}

	void FontBltXRGB1555(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 srb = color & 0x7c1f;
		const sint32 sg = color & 0x03e0;

		do {
			uint16 *dst16 = (uint16 *)dst;
			const uint16 *src16 = (const uint16 *)src;

			uint32 w2 = w;
			do {
				const sint32 alpha = src16[0] & 0x1f;

				if (alpha) {
					const sint32 drb = dst16[0] & 0x7c1f;
					const sint32 dg = dst16[0] & 0x7e0;

					const sint32 alpha1 = alpha + (alpha >> 4);

					const sint32 crb = (drb << 5) + (srb - drb) * alpha1 + 0x4010;
					const sint32 cg = (dg << 5) + (sg - dg) * alpha1 + 0x0200;

					dst16[0] = ((crb & 0x0f83e0) + (cg & 0x007c00)) >> 5;
				}

				++src16;
				++dst16;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void ColorFontBltXRGB1555(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = color & 0x7c00;
		const sint32 sg = color & 0x03e0;
		const sint32 sb = color & 0x001f;

		do {
			uint16 *dst16 = (uint16 *)dst;
			const uint16 *src16 = (const uint16 *)src;

			uint32 w2 = w;
			do {
				const uint16 alpha16 = src16[0];

				if (alpha16) {
					sint32 ar = alpha16 & 0x7c00;
					sint32 ag = alpha16 & 0x03e0;
					sint32 ab = alpha16 & 0x001f;

					const sint32 d = dst16[0];
					const sint32 dr = d & 0x7c00;
					const sint32 dg = d & 0x03e0;
					const sint32 db = d & 0x001f;

					ar += (ar & 0x4000) >> 4;
					ag += (ag & 0x0200) >> 4;
					ab += (ab & 0x0010) >> 4;

					const sint32 cr = (((sr - dr) * ar + 0x00100000) >> 15) & 0xfc00;
					const sint32 cg = (((sg - dg) * ag + 0x00004000) >> 10) & 0xffe0;
					const sint32 cb = (((sb - db) * ab + 0x00000010) >>  5);

					dst16[0] = (uint16)(d + cr + cg + cb);
				}

				++src16;
				++dst16;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void FontBltRGB565(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 srb = color & 0xf81f;
		const sint32 sg = color & 0x07e0;

		do {
			uint16 *dst16 = (uint16 *)dst;
			const uint16 *src16 = (const uint16 *)src;

			uint32 w2 = w;
			do {
				const sint32 alpha = src16[0] & 0x1f;

				if (alpha) {
					const sint32 drb = dst16[0] & 0xf81f;
					const sint32 dg = dst16[0] & 0x7e0;

					const sint32 alpha1 = alpha + (alpha >> 4);

					const sint32 crb = (drb << 5) + (srb - drb) * alpha1 + 0x8010;
					const sint32 cg = (dg << 5) + (sg - dg) * alpha1 + 0x0200;

					dst16[0] = ((crb & 0x1f03e0) + (cg & 0x00fc00)) >> 5;
				}

				++src16;
				++dst16;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void ColorFontBltRGB565(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = color & 0xf800;
		const sint32 sg = color & 0x07e0;
		const sint32 sb = color & 0x001f;

		do {
			uint16 *dst16 = (uint16 *)dst;
			const uint16 *src16 = (const uint16 *)src;

			uint32 w2 = w;
			do {
				const uint16 alpha16 = src16[0];

				if (alpha16) {
					sint32 ar = alpha16 & 0xf800;
					sint32 ag = alpha16 & 0x07e0;
					sint32 ab = alpha16 & 0x001f;

					const sint32 d = dst16[0];
					const sint32 dr = d & 0xf800;
					const sint32 dg = d & 0x07e0;
					const sint32 db = d & 0x001f;

					ar += (ar & 0x8000) >> 4;
					ag += (ag & 0x0400) >> 5;
					ab += (ab & 0x0010) >> 4;

					const sint32 cr = (((sr - dr) * ar + 0x00400000) >> 16) & 0xf800;
					const sint32 cg = (((sg - dg) * ag + 0x00008000) >> 11) & 0xffe0;
					const sint32 cb = (((sb - db) * ab + 0x00000010) >>  5);

					dst16[0] = (uint16)(d + cr + cg + cb);
				}

				++src16;
				++dst16;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void FontBltRGB888(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = (color >> 16) & 0xff;
		const sint32 sg = (color >>  8) & 0xff;
		const sint32 sb = (color >>  0) & 0xff;

		do {
			uint8 *dst8 = (uint8 *)dst;
			const uint8 *src8 = (const uint8 *)src;

			uint32 w2 = w;
			do {
				const sint32 ab = src8[0];

				if (ab) {
					const sint32 db = dst8[0];
					const sint32 dg = dst8[1];
					const sint32 dr = dst8[2];

					sint32 cr = (sr - dr)*ab + 128;
					sint32 cg = (sg - dg)*ab + 128;
					sint32 cb = (sb - db)*ab + 128;

					cr += cr >> 8;
					cg += cg >> 8;
					cb += cb >> 8;

					cr >>= 8;
					cg >>= 8;
					cb >>= 8;

					dst8[0] = (uint8)(db + cb);
					dst8[1] = (uint8)(dg + cg);
					dst8[2] = (uint8)(dr + cr);
				}

				src8 += 3;
				dst8 += 3;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void ColorFontBltRGB888(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = (color >> 16) & 0xff;
		const sint32 sg = (color >>  8) & 0xff;
		const sint32 sb = (color >>  0) & 0xff;

		do {
			uint8 *dst8 = (uint8 *)dst;
			const uint8 *src8 = (const uint8 *)src;

			uint32 w2 = w;
			do {
				const sint32 ab = src8[0];
				const sint32 ag = src8[1];
				const sint32 ar = src8[2];

				if (ar | ag | ab) {
					const sint32 db = dst8[0];
					const sint32 dg = dst8[1];
					const sint32 dr = dst8[2];

					sint32 cr = (sr - dr)*ar + 128;
					sint32 cg = (sg - dg)*ag + 128;
					sint32 cb = (sb - db)*ab + 128;

					cr += cr >> 8;
					cg += cg >> 8;
					cb += cb >> 8;

					cr >>= 8;
					cg >>= 8;
					cb >>= 8;

					dst8[0] = (uint8)(db + cb);
					dst8[1] = (uint8)(dg + cg);
					dst8[2] = (uint8)(dr + cr);
				}

				src8 += 3;
				dst8 += 3;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void FontBltXRGB8888(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = (color >> 16) & 0xff;
		const sint32 sg = (color >>  8) & 0xff;
		const sint32 sb = (color >>  0) & 0xff;

		do {
			uint8 *dst8 = (uint8 *)dst;
			const uint8 *src8 = (const uint8 *)src;

			uint32 w2 = w;
			do {
				const sint32 ab = src8[0];

				if (ab) {
					const sint32 db = dst8[0];
					const sint32 dg = dst8[1];
					const sint32 dr = dst8[2];

					sint32 cr = (sr - dr)*ab + 128;
					sint32 cg = (sg - dg)*ab + 128;
					sint32 cb = (sb - db)*ab + 128;

					cr += cr >> 8;
					cg += cg >> 8;
					cb += cb >> 8;

					cr >>= 8;
					cg >>= 8;
					cb >>= 8;

					dst8[0] = (uint8)(db + cb);
					dst8[1] = (uint8)(dg + cg);
					dst8[2] = (uint8)(dr + cr);
				}

				src8 += 4;
				dst8 += 4;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}

	void ColorFontBltXRGB8888(void *dst, ptrdiff_t dstpitch, const void *src, ptrdiff_t srcpitch, uint32 w, uint32 h, uint32 color) {
		const sint32 sr = (color >> 16) & 0xff;
		const sint32 sg = (color >>  8) & 0xff;
		const sint32 sb = (color >>  0) & 0xff;

		do {
			uint8 *dst8 = (uint8 *)dst;
			const uint8 *src8 = (const uint8 *)src;

			uint32 w2 = w;
			do {
				const sint32 ab = src8[0];
				const sint32 ag = src8[1];
				const sint32 ar = src8[2];

				if (ar | ag | ab) {
					const sint32 db = dst8[0];
					const sint32 dg = dst8[1];
					const sint32 dr = dst8[2];

					sint32 cr = (sr - dr)*ar + 128;
					sint32 cg = (sg - dg)*ag + 128;
					sint32 cb = (sb - db)*ab + 128;

					cr += cr >> 8;
					cg += cg >> 8;
					cb += cb >> 8;

					cr >>= 8;
					cg >>= 8;
					cb >>= 8;

					dst8[0] = (uint8)(db + cb);
					dst8[1] = (uint8)(dg + cg);
					dst8[2] = (uint8)(dr + cr);
				}

				src8 += 4;
				dst8 += 4;
			} while(--w2);

			dst = (char *)dst + dstpitch;
			src = (const char *)src + srcpitch;
		} while(--h);
	}
}

///////////////////////////////////////////////////////////////////////////

VDDisplayRendererSoft::VDDisplayRendererSoft()
	: mPrimary()
	, mColor(0)
	, mNativeColor(0)
	, mpFillSpan(NULL)
{
}

VDDisplayRendererSoft::~VDDisplayRendererSoft() {
}

void VDDisplayRendererSoft::Init() {
	mTextRenderer.Init(this, 512, 512);
}

bool VDDisplayRendererSoft::Begin(const VDPixmap& primary) {
	mPrimary = primary;
	mOffsetX = 0;
	mOffsetY = 0;

	mViewportStack.clear();

	switch(primary.format) {
		case nsVDPixmap::kPixFormat_XRGB1555:
			mpFillSpan = FillSpan16;
			mpFontBlt = FontBltXRGB1555;
			mpColorFontBlt = ColorFontBltXRGB1555;
			mBytesPerPixel = 2;
			break;

		case nsVDPixmap::kPixFormat_RGB565:
			mpFillSpan = FillSpan16;
			mpFontBlt = FontBltRGB565;
			mpColorFontBlt = ColorFontBltRGB565;
			mBytesPerPixel = 2;
			break;

		case nsVDPixmap::kPixFormat_RGB888:
			mpFillSpan = FillSpan24;
			mpFontBlt = FontBltRGB888;
			mpColorFontBlt = ColorFontBltRGB888;
			mBytesPerPixel = 3;
			break;

		case nsVDPixmap::kPixFormat_XRGB8888:
			mpFillSpan = FillSpan32;
			mpFontBlt = FontBltXRGB8888;
			mpColorFontBlt = ColorFontBltXRGB8888;
			mBytesPerPixel = 4;
			break;

		default:
			return false;
	}

	return true;
}

const VDDisplayRendererCaps& VDDisplayRendererSoft::GetCaps() {
	static const VDDisplayRendererCaps kCaps = {
		false
	};

	return kCaps;
}

void VDDisplayRendererSoft::SetColorRGB(uint32 color) {
	if (mColor == color)
		return;

	mColor = color;

	switch(mPrimary.format) {
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

void VDDisplayRendererSoft::FillRect(sint32 x, sint32 y, sint32 w, sint32 h) {
	x += mOffsetX;
	y += mOffsetY;

	if (x >= mPrimary.w || y >= mPrimary.h)
		return;

	if (w <= 0 || h <= 0)
		return;

	if (x < 0) {
		w += x;
		x = 0;
	}

	if (y < 0) {
		h += y;
		y = 0;
	}

	if (w > mPrimary.w - x)
		w = mPrimary.w - x;

	if (h > mPrimary.h - y)
		h = mPrimary.h - y;

	if (w <= 0 || h <= 0)
		return;

	char *dst = (char *)mPrimary.data + mPrimary.pitch * y;

	do {
		mpFillSpan(dst, x, mNativeColor, w);
		dst += mPrimary.pitch;
	} while(--h);
}

void VDDisplayRendererSoft::MultiFillRect(const vdrect32 *rects, uint32 n) {
	if (!n)
		return;

	while(n--) {
		const vdrect32& r = *rects++;

		if (!r.empty())
			FillRect(r.left, r.top, r.width(), r.height());
	}
}

void VDDisplayRendererSoft::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView) {
	VDDisplayCachedImageSoft *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	sint32 w = cachedImage->mWidth;
	sint32 h = cachedImage->mHeight;

	Blt(x, y, imageView, 0, 0, w, h);
}

void VDDisplayRendererSoft::Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h) {
	VDDisplayCachedImageSoft *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	x += mOffsetX;
	y += mOffsetY;

	// do full clipping
	if (x < 0) { sx -= x; w += x; x = 0; }
	if (y < 0) { sy -= y; h += y; y = 0; }
	if (sx < 0) { x -= sx; w += sx; sx = 0; }
	if (sy < 0) { y -= sy; h += sy; sy = 0; }

	if ((w|h) < 0)
		return;

	if (x + w > mPrimary.w) { w = mPrimary.w - x; }
	if (y + h > mPrimary.h) { h = mPrimary.h - y; }
	if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - x; }
	if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - y; }

	if ((w|h) < 0)
		return;

	VDMemcpyRect((char *)mPrimary.data + mPrimary.pitch * y + mBytesPerPixel * x,
		mPrimary.pitch,
		(const char *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy + mBytesPerPixel * sx,
		cachedImage->mSoftBuffer.pitch,
		w * mBytesPerPixel,
		h);
}

void VDDisplayRendererSoft::StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts) {
	VDDisplayCachedImageSoft *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	// check for source clipping
	if (sw <= 0 || sh <= 0)
		return;

	if (sx < 0 || sx >= cachedImage->mWidth || sy < 0 || sh >= cachedImage->mHeight)
		return;

	if (sw > cachedImage->mWidth - sx || sh > cachedImage->mHeight - sy)
		return;

	// do destination clipping
	if (dw <= 0 || dh <= 0)
		return;

	dx += mOffsetX;
	dy += mOffsetY;

	float factor_x = (float)sw / (float)dw;
	float factor_y = (float)sh / (float)dh;
	float fsx1 = (float)sx;
	float fsy1 = (float)sy;
	float fsx2 = fsx1 + (float)sw - 1.0f;
	float fsy2 = fsy1 + (float)sh - 1.0f;

	// do full clipping
	if (dx < 0) { fsx1 -= factor_x * dx; dw += dx; dx = 0; }
	if (dy < 0) { fsy1 -= factor_y * dy; dh += dy; dy = 0; }

	if ((dw|dh) < 0)
		return;

	if (dx + dw > mPrimary.w) { fsx2 += factor_x * (float)(mPrimary.w - (dx + dw)); dw = mPrimary.w - dx; }
	if (dy + dh > mPrimary.h) { fsy2 += factor_y * (float)(mPrimary.h - (dy + dh)); dh = mPrimary.h - dy; }

	if ((dw|dh) < 0)
		return;

	sint32 sx1 = VDRoundToInt(fsx1);
	sint32 sy1 = VDRoundToInt(fsy1);
	sint32 sx2 = VDRoundToInt(fsx2);
	sint32 sy2 = VDRoundToInt(fsy2);

	VDPixmapStretchBltNearest(mPrimary, dx, dy, dx + dw, dy + dh, cachedImage->mSoftBuffer, sx1, sy1, sx2 + 1, sy2 + 1);
}

void VDDisplayRendererSoft::MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode) {
	VDDisplayCachedImageSoft *cachedImage = GetCachedImage(imageView);

	if (!cachedImage)
		return;

	for(uint32 i=0; i<n; ++i) {
		const VDDisplayBlt& blt = blts[i];
		sint32 x = blt.mDestX + mOffsetX;
		sint32 y = blt.mDestY + mOffsetY;
		sint32 sx = blt.mSrcX;
		sint32 sy = blt.mSrcY;
		sint32 w = blt.mWidth;
		sint32 h = blt.mHeight;

		// do full clipping
		if (x < 0) { sx -= x; w += x; x = 0; }
		if (y < 0) { sy -= y; h += y; y = 0; }
		if (sx < 0) { x -= sx; w += sx; sx = 0; }
		if (sy < 0) { y -= sy; h += sy; sy = 0; }

		if ((w|h) < 0)
			continue;

		if (x + w > mPrimary.w) { w = mPrimary.w - x; }
		if (y + h > mPrimary.h) { h = mPrimary.h - y; }
		if (sx + w > cachedImage->mWidth) { w = cachedImage->mWidth - x; }
		if (sy + h > cachedImage->mHeight) { h = cachedImage->mHeight - y; }

		if (w <= 0 || h <= 0)
			continue;

		// software blitting time
		switch(bltMode) {
			case kBltMode_Stencil: {
				char *row = (char *)mPrimary.data + mPrimary.pitch * y;

				switch(mPrimary.format) {
					case nsVDPixmap::kPixFormat_XRGB1555:
					case nsVDPixmap::kPixFormat_RGB565:
						{
							uint16 *dst16 = (uint16 *)row + x;
							const uint16 *src16 = (const uint16 *)((const char *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy) + sx;

							while(h--) {
								for(sint32 j=0; j<w; ++j) {
									if (src16[j] & 31)
										dst16[j] = mNativeColor;
								}

								src16 = (const uint16 *)((const char *)src16 + cachedImage->mSoftBuffer.pitch);
								dst16 = (uint16 *)((char *)dst16 + mPrimary.pitch);
							}
						}
						break;

					case nsVDPixmap::kPixFormat_RGB888:
						{
							uint8 *dst24 = (uint8 *)row + x*3;
							const uint8 *src24 = (const uint8 *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy + sx*3;

							const uint8 mNativeRed   = (uint8)(mColor >> 16);
							const uint8 mNativeGreen = (uint8)(mColor >>  8);
							const uint8 mNativeBlue  = (uint8)(mColor >>  0);

							while(h--) {
								uint8 *dst2 = dst24;
								const uint8 *src2 = src24;

								for(sint32 j=0; j<w; ++j) {
									if (src2[0]) {
										dst2[0] = mNativeBlue;
										dst2[1] = mNativeGreen;
										dst2[2] = mNativeRed;
									}

									src2 += 3;
									dst2 += 3;
								}

								src24 += cachedImage->mSoftBuffer.pitch;
								dst24 += mPrimary.pitch;
							}
						}
						break;

					case nsVDPixmap::kPixFormat_XRGB8888:
						{
							uint32 *dst32 = (uint32 *)row + x;
							const uint32 *src32 = (const uint32 *)((const char *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy) + sx;

							while(h--) {
								for(sint32 j=0; j<w; ++j) {
									if (src32[j] & 0xFFFFFF)
										dst32[j] = mNativeColor;
								}

								src32 = (const uint32 *)((const char *)src32 + cachedImage->mSoftBuffer.pitch);
								dst32 = (uint32 *)((char *)dst32 + mPrimary.pitch);
							}
						}
						break;
				}
				break;
			}

			case kBltMode_Gray:{
				// software blitting time
				char *dst = (char *)mPrimary.data + mPrimary.pitch * y + mBytesPerPixel * x;
				const char *src = (const char *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy + mBytesPerPixel * sx;

				mpFontBlt(dst, mPrimary.pitch, src, cachedImage->mSoftBuffer.pitch, w, h, mNativeColor);
				break;
			}

			case kBltMode_Color:{
				// software blitting time
				char *dst = (char *)mPrimary.data + mPrimary.pitch * y + mBytesPerPixel * x;
				const char *src = (const char *)cachedImage->mSoftBuffer.data + cachedImage->mSoftBuffer.pitch * sy + mBytesPerPixel * sx;

				mpColorFontBlt(dst, mPrimary.pitch, src, cachedImage->mSoftBuffer.pitch, w, h, mNativeColor);
				break;
			}
		}
	}
}

void VDDisplayRendererSoft::PolyLine(const vdpoint32 *points, uint32 numLines) {
	if (!numLines)
		return;

	while(numLines--) {
		DrawLine(points[0].x + mOffsetX, points[0].y + mOffsetY, points[1].x + mOffsetX, points[1].y + mOffsetY);

		++points;
	}
}

bool VDDisplayRendererSoft::PushViewport(const vdrect32& r, sint32 x, sint32 y) {
	// clip rect
	sint32 x1 = r.left + mOffsetX;
	sint32 y1 = r.top + mOffsetY;
	sint32 x2 = r.right + mOffsetX;
	sint32 y2 = r.bottom + mOffsetY;
	sint32 offx = -x1;
	sint32 offy = -y1;

	if (x1 < 0) {
		offx += x1;
		x1 = 0;
	}

	if (y1 < 0) {
		offy += y1;
		y1 = 0;
	}

	if (x2 > mPrimary.w)
		x2 = mPrimary.w;

	if (y2 > mPrimary.h)
		y2 = mPrimary.h;

	if (x1 >= x2 || y1 >= y2)
		return false;

	// save context
	Viewport& sre = mViewportStack.push_back();
	sre.mPrimary = mPrimary;
	sre.mOffsetX = mOffsetX;
	sre.mOffsetY = mOffsetY;

	// shift to sub-render context
	mOffsetX += x + offx;
	mOffsetY += y + offy;

	mPrimary.data = (char *)mPrimary.data + mPrimary.pitch * y1 + mBytesPerPixel * x1;
	mPrimary.w = x2 - x1;
	mPrimary.h = y2 - y1;

	return true;
}

void VDDisplayRendererSoft::PopViewport() {
	// restore previous context
	Viewport& vp = mViewportStack.back();
	mPrimary = vp.mPrimary;
	mOffsetX = vp.mOffsetX;
	mOffsetY = vp.mOffsetY;

	mViewportStack.pop_back();
}

IVDDisplayRenderer *VDDisplayRendererSoft::BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache) {
	if (!PushViewport(r, r.left, r.top))
		return NULL;

	// save context
	SubRenderEntry& sre = mSubRenderStack.push_back();
	sre.mColor = mColor;
	sre.mNativeColor = mNativeColor;

	SetColorRGB(0);

	return this;
}

void VDDisplayRendererSoft::EndSubRender() {
	// restore previous context
	SubRenderEntry& sre = mSubRenderStack.back();
	mColor = sre.mColor;
	mNativeColor = sre.mNativeColor;

	mSubRenderStack.pop_back();

	PopViewport();
}

void VDDisplayRendererSoft::DrawLine(sint32 x1, sint32 y1, sint32 x2, sint32 y2) {
	// check for horizontal line
	if (y1 == y2) {
		if (x1 > x2) {
			std::swap(x1, x2);
			++x1;
			++x2;
		}

		if (y2 < 0 || y1 >= mPrimary.h)
			return;

		if (x1 < 0)
			x1 = 0;

		if (x2 > mPrimary.w)
			x2 = mPrimary.w;

		if (x1 >= x2)
			return;

		mpFillSpan(vdptroffset(mPrimary.data, mPrimary.pitch * y1), x1, mNativeColor, x2 - x1);
		return;
	}

	// check for vertical line
	if (x1 == x2) {
		if (y1 > y2) {
			std::swap(y1, y2);
			++y1;
			++y2;
		}

		if (x1 >= mPrimary.w || x1 < 0)
			return;

		if (y1 < 0)
			y1 = 0;

		if (y2 > mPrimary.h)
			y2 = mPrimary.h;

		if (y1 >= y2)
			return;

		char *dst = (char *)mPrimary.data + mPrimary.pitch * y1;
		while(y1++ < y2) {
			mpFillSpan(dst, x1, mNativeColor, 1);
			dst += mPrimary.pitch;
		}

		return;
	}

	// check for x major or y major
	uint32 dx = x1 < x2 ? (uint32)x2 - (uint32)x1 : (uint32)x1 - (uint32)x2;
	uint32 dy = y1 < y2 ? (uint32)y2 - (uint32)y1 : (uint32)y1 - (uint32)y2;

	bool horizontal;
	sint32 a1;
	sint32 a2;
	sint32 b1;
	sint32 b2;

	if (dx >= dy) {
		horizontal = true;
		a1 = x1;
		a2 = x2;
		b1 = y1;
		b2 = y2;
	} else {
		std::swap(dx, dy);
		horizontal = false;
		a1 = y1;
		a2 = y2;
		b1 = x1;
		b2 = x2;
	}

	bool firstPixel = true;
	if (a1 > a2) {
		std::swap(a1, a2);
		std::swap(b1, b2);
		firstPixel = false;
	}

	sint32 a0 = a1;
	sint32 a3 = a2;
	sint32 e = dx >> 1;

	// minor axis clipping
	sint32 clipB2 = horizontal ? mPrimary.h : mPrimary.w;
	if (b1 < b2) {
		if (b1 >= clipB2 || b2 < 0)
			return;

		if (b1 < 0) {
			int dist = -b1;
			//int abump = ceil((dist - 0.5) * dx / dy);
			int abump = (int)(((uint64)dist * dx - e) / dy) + 1;
			VDASSERT(abump > 0);

			// abump = min(x | e - dist*dx + x*dy >= 0)
			// abump = min(x | x >= ceil((dist*dx - e) / dy))
			// abump = ceil((dist*dx - e) / dy)

			a1 += abump;
		}

		if (b2 >= clipB2) {
			int dist = b2 - clipB2 + 1;
			int abump = (int)(((uint64)dist * dx - e) / dy) + 1;
			VDASSERT(abump > 0);

			a2 -= abump;
		}
	} else {
		if (b2 >= clipB2 || b1 < 0)
			return;

		if (b2 < 0) {
			int dist = -b2;
			//int abump = ceil((dist - 0.5) * dx / dy);
			int abump = (int)(((uint64)dist * dx - e) / dy) + 1;
			VDASSERT(abump > 0);

			a2 -= abump;
		}

		if (b1 >= clipB2) {
			int dist = b1 - clipB2 + 1;
			int abump = (int)(((uint64)dist * dx - e) / dy) + 1;
			VDASSERT(abump > 0);

			a1 += abump;
		}
	}

	// major axis clipping
	if (firstPixel) {
		if (a2 != a3)
			++a2;
	} else {
		if (a0 == a1)
			++a1;

		++a2;
	}

	if (horizontal) {
		if (a1 < 0)
			a1 = 0;

		if (a2 > mPrimary.w)
			a2 = mPrimary.w;
	} else {
		if (a1 < 0)
			a1 = 0;

		if (a2 > mPrimary.h)
			a2 = mPrimary.h;
	}

	if (a1 >= a2)
		return;

	if (a1 != a0) {
		uint64 t = (uint64)dy * (a1 - a0);
		uint32 bbump = 0;
		
		if (t > e) {
			bbump = (uint32)((t - e + dx - 1) / dx);
			t -= bbump * dx;
		}

		e -= (uint32)t;

		if (b1 < b2) {
			b1 += bbump;
			if (e < 0) {
				++b1;
				e += dx;
			}
		} else {
			b1 -= bbump;
			if (e < 0) {
				--b1;
				e += dx;
			}
		}
	}

	if (horizontal) {
		char *dst = (char *)mPrimary.data + mPrimary.pitch * b1;
		ptrdiff_t bstep = b2 > b1 ? mPrimary.pitch : -mPrimary.pitch;

		for(sint32 a = a1; a < a2; ++a) {
			mpFillSpan(dst, a, mNativeColor, 1);

			e -= dy;
			if (e < 0) {
				e += dx;
				dst += bstep;
			}
		}
	} else {
		char *dst = (char *)mPrimary.data + mPrimary.pitch * a1;
		ptrdiff_t bstep = b2 > b1 ? 1 : -1;

		for(sint32 a = a1; a < a2; ++a) {
			mpFillSpan(dst, b1, mNativeColor, 1);
			dst += mPrimary.pitch;

			e -= dy;
			if (e < 0) {
				e += dx;
				b1 += bstep;
			}
		}
	}
}

VDDisplayCachedImageSoft *VDDisplayRendererSoft::GetCachedImage(VDDisplayImageView& imageView) {
	VDDisplayCachedImageSoft *cachedImage = static_cast<VDDisplayCachedImageSoft *>(imageView.GetCachedImage(VDDisplayCachedImageSoft::kTypeID));

	if (!cachedImage) {
		cachedImage = new_nothrow VDDisplayCachedImageSoft;

		if (!cachedImage)
			return NULL;
		
		cachedImage->AddRef();
		if (!cachedImage->Init(imageView, mPrimary.format)) {
			cachedImage->Release();
			return NULL;
		}

		imageView.SetCachedImage(VDDisplayCachedImageSoft::kTypeID, cachedImage);

		cachedImage->Release();
	}

	uint32 c = imageView.GetUniquenessCounter();

	if (cachedImage->mUniquenessCounter != c)
		cachedImage->UpdateSoftware(imageView);

	return cachedImage;
}
