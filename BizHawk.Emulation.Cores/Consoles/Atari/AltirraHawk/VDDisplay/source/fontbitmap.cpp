#include <stdafx.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/VDDisplay/internal/fontbitmap.h>

VDDisplayFontBitmap::VDDisplayFontBitmap() {
}

VDDisplayFontBitmap::~VDDisplayFontBitmap() {
	Shutdown();
}

void *VDDisplayFontBitmap::AsInterface(uint32 id) {
	return nullptr;
}

void VDDisplayFontBitmap::Init(const VDDisplayFontMetrics& metrics, uint32 numGlyphs, const wchar_t *chars, const VDDisplayBitmapFontGlyphInfo *glyphInfos, const VDPixmap& pixmap, uint32 missingGlyph, IVDRefCount *objectToOwn) {
	VDASSERT(missingGlyph < numGlyphs);

	mpObjectToOwn = objectToOwn;
	if (mpObjectToOwn)
		mpObjectToOwn->AddRef();

	mMetrics = metrics;
	mPixmap = pixmap;

	vdfastvector<uint32> sortIndex(numGlyphs);
	for(uint32 i=0; i<numGlyphs; ++i)
		sortIndex[i] = i;

	std::sort(sortIndex.begin(), sortIndex.end(), [chars](uint32 x, uint32 y) { return chars[x] < chars[y]; });

	mGlyphChars.resize(numGlyphs);
	for(uint32 i=0; i<numGlyphs; ++i)
		mGlyphChars[i] = chars[sortIndex[i]];

	mGlyphInfos.resize(numGlyphs);
	for(uint32 i=0; i<numGlyphs; ++i)
		mGlyphInfos[i] = glyphInfos[sortIndex[i]];

	mMissingGlyphIndex = sortIndex[missingGlyph];
}

void VDDisplayFontBitmap::Shutdown() {
	vdsaferelease <<= mpObjectToOwn;
}

void VDDisplayFontBitmap::GetMetrics(VDDisplayFontMetrics& metrics) {
	metrics = mMetrics;
}

void VDDisplayFontBitmap::ShapeText(const wchar_t *s, uint32 n, vdfastvector<VDDisplayFontGlyphPlacement>& glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos) {
	ShapeTextInternal(s, n, UINT32_MAX, &glyphPlacements, cellBounds, glyphBounds, nextPos, nullptr);
}

void VDDisplayFontBitmap::GetGlyphMetrics(uint32 c, VDDisplayFontGlyphMetrics& metrics) {
	if (c >= mGlyphInfos.size()) {
		metrics = {};
	} else {
		const auto& info = mGlyphInfos[c];

		metrics.mAdvance = info.mAdvance;
		metrics.mX = info.mCellX;
		metrics.mY = info.mCellY;
		metrics.mWidth = info.mWidth;
		metrics.mHeight = info.mHeight;
	}
}

bool VDDisplayFontBitmap::GetGlyphImage(uint32 c, bool inverted, const VDPixmap& dst) {
	if (c >= mGlyphInfos.size())
		return false;

	const auto& info = mGlyphInfos[c];

	VDPixmap px = mPixmap;
	px.w = info.mWidth;
	px.h = info.mHeight;
	px.data = (char *)px.data + info.mBitmapX * 4 + info.mBitmapY * px.pitch;

	VDPixmapBlt(dst, px);
	return true;
}

vdsize32 VDDisplayFontBitmap::MeasureString(const wchar_t *s, uint32 n, bool includeOverhangs) {
	vdrect32 bounds;

	ShapeTextInternal(s, n, UINT32_MAX, nullptr, includeOverhangs ? nullptr : &bounds, includeOverhangs ? &bounds : nullptr, nullptr, nullptr);

	return vdsize32(bounds.right, bounds.height());
}

vdsize32 VDDisplayFontBitmap::FitString(const wchar_t *s, uint32 n, uint32 maxWidth, uint32 *count) {
	vdrect32 cellBounds;
	ShapeTextInternal(s, n, maxWidth, nullptr, &cellBounds, nullptr, nullptr, count);

	return cellBounds.size();
}

void VDDisplayFontBitmap::ShapeTextInternal(const wchar_t *s, uint32 n, uint32 maxAdvance, vdfastvector<VDDisplayFontGlyphPlacement> *glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos, uint32 *count) {
	size_t gpbase = 0;

	if (glyphPlacements) {
		gpbase = glyphPlacements->size();
		glyphPlacements->resize(gpbase + n);
	}

	int x = 0;
	int y = 0;
	int minPos = 0;
	int maxPos = 0;
	int minGPos = 0;
	int maxGPos = 0;
	uint32 counted = 0;

	if (n) {
		for(uint32 i=0; i<n; ++i) {
			auto it = std::lower_bound(mGlyphChars.begin(), mGlyphChars.end(), s[i]);
			uint32 glyphIndex = mMissingGlyphIndex;

			if (it != mGlyphChars.end() && *it == s[i])
				glyphIndex = (uint32)(it - mGlyphChars.begin());

			const auto& glyphInfo = mGlyphInfos[glyphIndex];
			const int x0 = x;
			x += glyphInfo.mAdvance;

			if (x > 0 && (uint32)x > maxAdvance) {
				x -= glyphInfo.mAdvance;
				break;
			}

			if (minPos > x)
				minPos = x;

			if (maxPos < x)
				maxPos = x;

			int x2 = x0 + glyphInfo.mCellX;
			int y2 = y + glyphInfo.mCellY;

			if (glyphPlacements) {
				VDDisplayFontGlyphPlacement& pl = (*glyphPlacements)[gpbase + i];
				pl.mGlyphIndex = glyphIndex;
				pl.mCellX = x;
				pl.mX = x2;
				pl.mY = y2;
				pl.mOriginalOffset = i;
			}

			if (minGPos > x2)
				minGPos = x2;

			if (maxGPos < x2 + glyphInfo.mWidth)
				maxGPos = x2 + glyphInfo.mWidth;
		}
	}

	if (minGPos > minPos)
		minGPos = minPos;

	if (maxGPos < maxPos)
		maxGPos = maxPos;

	if (cellBounds)
		cellBounds->set(minPos, -mMetrics.mAscent, maxPos, mMetrics.mDescent);

	if (glyphBounds)
		glyphBounds->set(minGPos, -mMetrics.mAscent, maxGPos, mMetrics.mDescent);

	if (nextPos)
		*nextPos = vdpoint32(x, y);

	if (count)
		*count = counted;
}

void VDCreateDisplayBitmapFont(const VDDisplayFontMetrics& metrics, uint32 numGlyphs, const wchar_t *chars, const VDDisplayBitmapFontGlyphInfo *glyphInfos, const VDPixmap& pixmap, uint32 missingGlyph, IVDRefCount *objectToOwn, IVDDisplayFont **font) {
	vdrefptr<VDDisplayFontBitmap> newFont(new VDDisplayFontBitmap);

	newFont->Init(metrics, numGlyphs, chars, glyphInfos, pixmap, missingGlyph, objectToOwn);
	*font = newFont.release();
}
