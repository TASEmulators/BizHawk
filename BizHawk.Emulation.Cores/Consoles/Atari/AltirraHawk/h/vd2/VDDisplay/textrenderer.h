#ifndef f_VD2_VDDISPLAY_TEXTRENDERER_H
#define f_VD2_VDDISPLAY_TEXTRENDERER_H

#include <vd2/system/linearalloc.h>
#include <vd2/system/refcount.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/font.h>
#include <vd2/VDDisplay/renderer.h>

class VDDisplayTextRenderer {
public:
	enum Alignment {
		kAlignLeft,
		kAlignCenter,
		kAlignRight
	};

	enum VertAlign {
		kVertAlignTop,
		kVertAlignBaseline,
		kVertAlignBottom
	};

	VDDisplayTextRenderer();
	~VDDisplayTextRenderer();

	void Init(IVDDisplayRenderer *r, uint32 cachew, uint32 cacheh, bool useColor2Mode = false);

	void Begin();
	void End();

	void SetFont(IVDDisplayFont *font);
	void SetColorRGB(uint32 c);

	void SetAlignment(Alignment align = kAlignLeft, VertAlign valign = kVertAlignBaseline);
	void SetPosition(int x, int y);

	void DrawTextSpan(const wchar_t *text, uint32 numChars);
	void DrawTextLine(int x, int y, const wchar_t *text);
	void DrawPrearrangedText(int x, int y, const VDDisplayFontGlyphPlacement *glyphPlacements, uint32 n);

protected:
	struct HashNode {
		HashNode *mpNext;
		uintptr	mFontId;
		uint32	mGlyphIndex;
		sint16	mDx;
		sint16	mDy;
		uint16	mX;
		uint16	mY;
		uint16	mWidth;
		uint16	mHeight;
		int		mAdvance;
	};

	void Discard();
	const HashNode *PrepareGlyph(IVDDisplayFont *font, uint32 glyphIndex);

	void Clear();
	HashNode *Allocate(uintptr fontId, uint32 c, uint32 w, uint32 h);

	IVDDisplayRenderer *mpRenderer;

	VDPixmapBuffer		mCacheImage;
	VDDisplayImageView	mCacheImageView;

	uint32	mGeneration;
	uint32	mX;
	uint32	mY;
	uint32	mWidth;
	uint32	mHeight;
	uint32	mLineHeight;
	bool	mbUseColor2Mode;

	Alignment	mAlignment;
	VertAlign	mVertAlign;
	uint32		mColor;
	sint32		mDrawX;
	sint32		mDrawY;

	vdrefptr<IVDDisplayFont> mpFont;
	vdfastvector<VDDisplayBlt> mBlts;
	vdfastvector<VDDisplayFontGlyphPlacement> mGlyphPlacements;

	HashNode *mpHashTable[64];

	VDLinearAllocator mHashNodeAllocator;
};

#endif	// f_VD2_VDDISPLAY_TEXTRENDERER_H

