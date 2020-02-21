#include <stdafx.h>
#include <windows.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/VDDisplay/internal/fontgdi.h>

// GetCharacterPlacement():
//	- Has no way to signal missing glyphs
//  - Returns garbage glyph indices if font substitution occurs (they do not
//    appear to relate to either the old or new font)
//  - Does not position diacritics correctly
//
// GetGlyphOutlineW():
//  - Only works with TrueType fonts; fails with bitmap fonts
//  - Cannot do ClearType antialiasing
//  - Does returns substituted glyphs if font is TrueType
//  - Cannot return substituted glyphs if base font is bitmap even if substitution
//	  is from a TrueType font
//
// GetGlyphIndicesW():
//	- Returns invalid if yen is requested on Tahoma or Microsoft Sans Serif in Japanese
//	  locale, but not Segoe UI
//	- Does not handle font substitution
//
// GetCharWidth32W():
//	- Docs say it only works on bitmap fonts, but in practice it works on TrueType
//	  too
//	- Usually does substitution, with notable exception of Calibri font
//
// GetCharABCWidthsW():
//	- Does handle font substitution, except with Calibri
//	- Fails with bitmap fonts, even when glyphs are pulled from other TrueType fonts
//
// DrawTextW():
//	- DT_CALCRECT returns total advance width, so it is useless for determing actual
//	  width
//
// GetTextExtentPoint32():
//	- Returned width is total advance width
//
// GetFontUnicodeRanges():
//	- Excludes yen symbol in Tahoma or Microsoft Sans Serif in Japanese locale

const int kClearTypePadding = 1;

bool VDCreateDisplaySystemFont(int height, bool bold, const char *fontName, IVDDisplayFont **font) {
	HFONT hfont = ::CreateFontA(height,
		0,
		0,
		0,
		bold ? FW_BOLD : FW_NORMAL,
		FALSE,
		FALSE,
		FALSE,
		DEFAULT_CHARSET,
		OUT_DEFAULT_PRECIS,
		CLIP_DEFAULT_PRECIS,
		DEFAULT_QUALITY,
		DEFAULT_PITCH | FF_DONTCARE,
		fontName);

	if (!hfont)
		return false;

	VDDisplayFontGDI *f;
	try {
		f = new VDDisplayFontGDI();
	} catch(...) {
		::DeleteObject(hfont);
		throw;
	}

	f->AddRef();
	if (!f->Init(hfont)) {
		f->Release();
		::DeleteObject(hfont);
		return false;
	}

	*font = f;
	return true;
}

VDDisplayFontGDI::VDDisplayFontGDI()
	: mhfont(NULL)
	, mhdc(NULL)
	, mhbm(NULL)
	, mhbmOld(NULL)
{
}

VDDisplayFontGDI::~VDDisplayFontGDI() {
	Shutdown();
}

int VDDisplayFontGDI::AddRef() {
	return vdrefcounted<IVDDisplayFont>::AddRef();
}

int VDDisplayFontGDI::Release() {
	return vdrefcounted<IVDDisplayFont>::Release();
}

void *VDDisplayFontGDI::AsInterface(uint32 iid) {
	if (iid == IVDDisplayFontGDI::kTypeID)
		return static_cast<IVDDisplayFontGDI *>(this);

	return NULL;
}

bool VDDisplayFontGDI::Init(HFONT font) {
	mhdc = ::CreateCompatibleDC(NULL);
	if (!mhdc)
		return false;

	mhfont = font;

	mhbmOld = SelectObject(mhdc, mhfont);
	if (!mhbmOld)
		return false;

	if (!GetTextMetricsW(mhdc, &mMetrics))
		return false;

	::SetTextAlign(mhdc, TA_BASELINE | TA_LEFT);
	::SetBkMode(mhdc, OPAQUE);

	VDPixmap px = {};
	uint32 c;
	px.format = nsVDPixmap::kPixFormat_XRGB8888;
	px.data = &c;
	px.w = 1;
	px.h = 1;

	GetGlyphImage(0x20, false, px);

	return true;
}

void VDDisplayFontGDI::Shutdown() {
	if (mhbmOld) {
		SelectObject(mhdc, mhbmOld);
		mhbmOld = NULL;
	}

	if (mhbm) {
		DeleteObject(mhbm);
		mhbm = NULL;
	}

	if (mhdc) {
		DeleteDC(mhdc);
		mhdc = NULL;
	}

	if (mhfont) {
		DeleteObject(mhfont);
		mhfont = NULL;
	}
}

void VDDisplayFontGDI::GetMetrics(VDDisplayFontMetrics& metrics) {
	metrics.mAscent = mMetrics.tmAscent;
	metrics.mDescent = mMetrics.tmDescent;
}

void VDDisplayFontGDI::ShapeText(const wchar_t *s, uint32 n, vdfastvector<VDDisplayFontGlyphPlacement>& glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos) {
	size_t gpbase = glyphPlacements.size();

	glyphPlacements.resize(gpbase + n);

	int x = 0;
	int y = 0;
	int minPos = 0;
	int maxPos = 0;
	int minGPos = 0;
	int maxGPos = 0;

	if (n) {
		VDDisplayFontGlyphMetrics metrics;

		for(uint32 i=0; i<n; ++i) {
			VDDisplayFontGlyphPlacement& pl = glyphPlacements[gpbase + i];

			pl.mGlyphIndex = (uint32)(uint16)s[i];
			pl.mCellX = x;
			pl.mX = x;
			pl.mY = y;
			pl.mOriginalOffset = i;

			GetGlyphMetrics(pl.mGlyphIndex, metrics);
			x += metrics.mAdvance;

			if (minPos > x)
				minPos = x;

			if (maxPos < x)
				maxPos = x;


			pl.mX += metrics.mX;
			pl.mY -= metrics.mY;

			if (minGPos > pl.mX)
				minGPos = pl.mX;

			if (maxGPos < pl.mX + metrics.mWidth)
				maxGPos = pl.mX + metrics.mWidth;
		}
	}

	if (minGPos > minPos)
		minGPos = minPos;

	if (maxGPos < maxPos)
		maxGPos = maxPos;

	if (cellBounds)
		cellBounds->set(minPos, -mMetrics.tmAscent, maxPos, mMetrics.tmDescent);

	if (glyphBounds)
		glyphBounds->set(minGPos, -mMetrics.tmAscent, maxGPos, mMetrics.tmDescent);

	if (nextPos)
		*nextPos = vdpoint32(x, y);
}

void VDDisplayFontGDI::GetGlyphMetrics(uint32 c, VDDisplayFontGlyphMetrics& metrics) {
	GlyphMetricsCache::const_iterator it(mGlyphMetricsCache.find(c));

	if (it != mGlyphMetricsCache.end()) {
		metrics = it->second;
		return;
	}

	RenderGlyph(c, false, NULL, &metrics);
}

bool VDDisplayFontGDI::GetGlyphImage(uint32 c, bool inverted, const VDPixmap& dst) {
	if (dst.format != nsVDPixmap::kPixFormat_XRGB8888)
		return false;

	return RenderGlyph(c, inverted, &dst, NULL);
}

bool VDDisplayFontGDI::RenderGlyph(uint32 c, bool inverted, const VDPixmap *dst, VDDisplayFontGlyphMetrics *dstMetrics) {
	VDASSERT(!dst || dst->format == nsVDPixmap::kPixFormat_XRGB8888);

	if (!mhbm) {
		mBitmapMargin = std::max<uint32>(mMetrics.tmMaxCharWidth, std::max<uint32>(2, mMetrics.tmOverhang)) + kClearTypePadding;
		mBitmapWidth = mMetrics.tmMaxCharWidth + 2*mBitmapMargin;

		BITMAPINFO bi = {0};
		bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bi.bmiHeader.biWidth = mBitmapWidth;
		bi.bmiHeader.biHeight = mMetrics.tmHeight;
		bi.bmiHeader.biPlanes = 1;
		bi.bmiHeader.biBitCount = 32;
		bi.bmiHeader.biCompression = BI_RGB;
		bi.bmiHeader.biSizeImage = 0;
		bi.bmiHeader.biClrUsed = 0;
		bi.bmiHeader.biClrImportant = 0;

		mhbm = ::CreateDIBSection(mhdc, &bi, DIB_RGB_COLORS, &mpvBits, NULL, 0);
		if (!mhbm)
			return false;

		::DeleteObject(::SelectObject(mhdc, mhbm));
	}

	HBRUSH hFillBrush;

	if (inverted) {
		hFillBrush = (HBRUSH)::GetStockObject(WHITE_BRUSH);
		::SetBkColor(mhdc, RGB(255, 255, 255));
		::SetTextColor(mhdc, RGB(0, 0, 0));
	} else {
		hFillBrush = (HBRUSH)::GetStockObject(BLACK_BRUSH);
		::SetBkColor(mhdc, RGB(0, 0, 0));
		::SetTextColor(mhdc, RGB(255, 255, 255));
	}

	RECT r = {0, 0, (LONG)mBitmapWidth, mMetrics.tmHeight};
	::FillRect(mhdc, &r, hFillBrush);

	WCHAR ch = c;
	VDVERIFY(ExtTextOutW(mhdc, mBitmapMargin, mMetrics.tmAscent, ETO_OPAQUE, NULL, &ch, 1, NULL));

	::GdiFlush();

	// Okay, now that the character has been rasterized, start scanning the bitmap to determine bounds.
	GlyphMetricsCache::insert_return_type result = mGlyphMetricsCache.insert(c);
	VDDisplayFontGlyphMetrics& metrics = result.first->second;
	
	if (result.second) {
		const char *scanSrc = (const char *)mpvBits;
		uint32 rawLeft = mBitmapWidth;
		uint32 rawRight = 0;

		for(LONG y=0; y<mMetrics.tmHeight; ++y) {
			const uint32 *scanRow = (const uint32 *)scanSrc;
			uint32 xl = 0;
			uint32 xr = mBitmapWidth;

			if (inverted) {
				for(; xl<mBitmapWidth; ++xl) {
					if (~scanRow[xl] & 0xffffff)
						break;
				}

				for(; xr>0; --xr) {
					if (~scanRow[xr-1] & 0xffffff)
						break;
				}
			} else {
				for(; xl<mBitmapWidth; ++xl) {
					if (scanRow[xl] & 0xffffff)
						break;
				}

				for(; xr>0; --xr) {
					if (scanRow[xr-1] & 0xffffff)
						break;
				}
			}

			if (xl < xr) {
				if (rawLeft > xl)
					rawLeft = xl;

				if (rawRight < xr)
					rawRight = xr;
			}

			scanSrc += mBitmapWidth * 4;
		}
		
		int left = (int)rawLeft - mBitmapMargin;
		int right = (int)rawRight - mBitmapMargin;

		if (right <= left)
			left = right = 0;

		metrics.mWidth = right - left;
		metrics.mHeight = mMetrics.tmHeight;
		metrics.mX = left;
		metrics.mY = mMetrics.tmAscent;
		metrics.mAdvance = metrics.mWidth;
		
		SIZE sz = {0};
		if (GetTextExtentPoint32W(mhdc, &ch, 1, &sz))
			metrics.mAdvance = sz.cx;
	}

	if (dstMetrics)
		*dstMetrics = metrics;

	// Extract bitmap if requested.
	if (dst) {
		int w = metrics.mWidth;
		int h = metrics.mHeight;

		if (w > dst->w)
			w = dst->w;

		if (h > dst->h)
			h = dst->h;

		char *dstrow = (char *)dst->data;
		const char *srcrow = (const char *)mpvBits + (r.bottom - 1) * mBitmapWidth * 4 + (mBitmapMargin + metrics.mX) * 4;

		while(h--) {
			const uint32 *srcp = (const uint32 *)srcrow;
			uint32 *dstp = (uint32 *)dstrow;

			if (inverted) {
				for(int x=0; x<w; ++x) {
					dstp[x] = ~srcp[x] | 0xFF000000;
				}
			} else {
				for(int x=0; x<w; ++x) {
					dstp[x] = srcp[x] | 0xFF000000;
				}
			}

			dstrow += dst->pitch;
			srcrow += -(ptrdiff_t)mBitmapWidth * 4;
		}
	}

	return true;
}

vdsize32 VDDisplayFontGDI::MeasureString(const wchar_t *s, uint32 n, bool includeOverhangs) {
	vdfastvector<VDDisplayFontGlyphPlacement> placement;
	vdrect32 bounds;

	ShapeText(s, n, placement, includeOverhangs ? NULL : &bounds, includeOverhangs ? &bounds : NULL, NULL);

	return vdsize32(bounds.right, bounds.height());
}

vdsize32 VDDisplayFontGDI::FitString(const wchar_t *s, uint32 n, uint32 maxWidth, uint32 *count) {
	INT maxFit = n;
	SIZE sz = {0, 0};

	GetTextExtentExPointW(mhdc, s, n, maxWidth, &maxFit, NULL, &sz);

	if (count)
		*count = maxFit;

	return vdsize32(sz.cx, sz.cy);
}
