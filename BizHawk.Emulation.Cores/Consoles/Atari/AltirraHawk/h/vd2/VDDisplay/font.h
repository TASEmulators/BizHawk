#ifndef f_VD2_VDDISPLAY_FONT_H
#define f_VD2_VDDISPLAY_FONT_H

#include <vd2/system/unknown.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>

struct VDPixmap;
class IVDRefCount;

struct VDDisplayFontGlyphMetrics {
	int mWidth;
	int	mHeight;
	int mX;
	int mY;
	int mAdvance;
};

struct VDDisplayFontMetrics {
	int mAscent;
	int mDescent;
};

struct VDDisplayFontGlyphPlacement {
	uint32 mGlyphIndex;
	int mCellX;
	int mX;
	int mY;
	int mOriginalOffset;
};

class IVDDisplayFont : public IVDRefUnknown {
public:
	virtual void GetMetrics(VDDisplayFontMetrics&) = 0;
	virtual void GetGlyphMetrics(uint32 glyphIndex, VDDisplayFontGlyphMetrics& metrics) = 0;

	virtual void ShapeText(const wchar_t *s, uint32 n, vdfastvector<VDDisplayFontGlyphPlacement>& glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos) = 0;

	virtual bool GetGlyphImage(uint32 c, bool inverted, const VDPixmap& dst) = 0;

	virtual vdsize32 MeasureString(const wchar_t *s, uint32 n, bool includeOverhangs) = 0;
	virtual vdsize32 FitString(const wchar_t *s, uint32 n, uint32 maxWidth, uint32 *count) = 0;
};

bool VDCreateDisplaySystemFont(int height, bool bold, const char *fontName, IVDDisplayFont **font);

struct VDDisplayBitmapFontGlyphInfo {
	// Top-left corner of glyph image in font bitmap.
	int mBitmapX;
	int mBitmapY;

	// Offset from base position of glyph to top-left corner render position (top down).
	int mCellX;
	int mCellY;

	// Width and heigth of glyph image.
	int mWidth;
	int mHeight;

	// Advance amount for horizontal base position.
	int mAdvance;
};

void VDCreateDisplayBitmapFont(const VDDisplayFontMetrics& metrics, uint32 numGlyphs, const wchar_t *chars, const VDDisplayBitmapFontGlyphInfo *glyphInfos, const VDPixmap& pixmap, uint32 missingGlyph, IVDRefCount *objectToOwn, IVDDisplayFont **font);

#endif
