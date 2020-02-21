#ifndef f_VD2_VDDISPLAY_FONTBITMAP_H
#define f_VD2_VDDISPLAY_FONTBITMAP_H

#include <vd2/system/unknown.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vectors.h>
#include <vd2/system/vdstl.h>
#include <vd2/VDDisplay/font.h>

class VDDisplayFontBitmap final : public vdrefcounted<IVDDisplayFont> {
	VDDisplayFontBitmap(const VDDisplayFontBitmap&) = delete;
	VDDisplayFontBitmap& operator=(const VDDisplayFontBitmap&) = delete;
public:
	VDDisplayFontBitmap();
	~VDDisplayFontBitmap();

	void *AsInterface(uint32 id) override;

	void Init(const VDDisplayFontMetrics& metrics, uint32 numGlyphs, const wchar_t *chars, const VDDisplayBitmapFontGlyphInfo *glyphInfos, const VDPixmap& pixmap, uint32 missingGlyph, IVDRefCount *objectToOwn);
	void Shutdown();

	void GetMetrics(VDDisplayFontMetrics& metrics) override;

	void ShapeText(const wchar_t *s, uint32 n, vdfastvector<VDDisplayFontGlyphPlacement>& glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos) override;

	void GetGlyphMetrics(uint32 c, VDDisplayFontGlyphMetrics& metrics) override;
	bool GetGlyphImage(uint32 c, bool inverted, const VDPixmap& dst) override;

	vdsize32 MeasureString(const wchar_t *s, uint32 n, bool includeOverhangs) override;
	vdsize32 FitString(const wchar_t *s, uint32 n, uint32 maxWidth, uint32 *count) override;

protected:
	void ShapeTextInternal(const wchar_t *s, uint32 n, uint32 maxAdvance, vdfastvector<VDDisplayFontGlyphPlacement> *glyphPlacements, vdrect32 *cellBounds, vdrect32 *glyphBounds, vdpoint32 *nextPos, uint32 *count);

	VDPixmap mPixmap = {};

	IVDRefCount *mpObjectToOwn = nullptr;
	uint32 mMissingGlyphIndex = 0;

	VDDisplayFontMetrics mMetrics = {};
	vdfastvector<wchar_t> mGlyphChars;
	vdfastvector<VDDisplayBitmapFontGlyphInfo> mGlyphInfos;
};

#endif
