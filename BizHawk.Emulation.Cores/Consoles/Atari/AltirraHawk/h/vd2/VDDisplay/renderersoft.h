#ifndef f_VD2_VDDISPLAY_RENDERERSOFT_H
#define f_VD2_VDDISPLAY_RENDERERSOFT_H

#include <vd2/VDDisplay/textrenderer.h>

class VDDisplayCachedImageSoft;

class VDDisplayRendererSoft : public IVDDisplayRenderer {
	VDDisplayRendererSoft(const VDDisplayRendererSoft&);
	VDDisplayRendererSoft& operator=(const VDDisplayRendererSoft&);
public:
	VDDisplayRendererSoft();
	~VDDisplayRendererSoft();

	void Init();

	bool Begin(const VDPixmap& primary);

	const VDDisplayRendererCaps& GetCaps();
	VDDisplayTextRenderer *GetTextRenderer() { return &mTextRenderer; }

	void SetColorRGB(uint32 color);
	void FillRect(sint32 x, sint32 y, sint32 w, sint32 h);
	void MultiFillRect(const vdrect32 *rects, uint32 n);

	void AlphaFillRect(sint32 x, sint32 y, sint32 w, sint32 h, uint32 alphaColor) {}
	void AlphaTriStrip(const vdfloat2 *pts, uint32 numPts, uint32 alphaColor) {}

	void Blt(sint32 x, sint32 y, VDDisplayImageView& imageView);
	void Blt(sint32 x, sint32 y, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 w, sint32 h);
	void StretchBlt(sint32 dx, sint32 dy, sint32 dw, sint32 dh, VDDisplayImageView& imageView, sint32 sx, sint32 sy, sint32 sw, sint32 sh, const VDDisplayBltOptions& opts);
	void MultiBlt(const VDDisplayBlt *blts, uint32 n, VDDisplayImageView& imageView, BltMode bltMode);

	void PolyLine(const vdpoint32 *points, uint32 numLines);

	virtual bool PushViewport(const vdrect32& r, sint32 x, sint32 y);
	virtual void PopViewport();

	IVDDisplayRenderer *BeginSubRender(const vdrect32& r, VDDisplaySubRenderCache& cache);
	void EndSubRender();

protected:
	void DrawLine(sint32 x1, sint32 y1, sint32 x2, sint32 y2);

	VDDisplayCachedImageSoft *GetCachedImage(VDDisplayImageView& imageView);

	uint32	mColor;
	uint32	mNativeColor;
	int		mBytesPerPixel;

	VDPixmap	mPrimary;
	sint32 mOffsetX;
	sint32 mOffsetY;

	void (*mpFillSpan)(void *, uint32, uint32, uint32);
	void (*mpFontBlt)(void *, ptrdiff_t, const void *, ptrdiff_t, uint32, uint32, uint32);
	void (*mpColorFontBlt)(void *, ptrdiff_t, const void *, ptrdiff_t, uint32, uint32, uint32);

	struct SubRenderEntry {
		uint32 mColor;
		uint32 mNativeColor;
	};

	typedef vdfastvector<SubRenderEntry> SubRenderStack;
	SubRenderStack mSubRenderStack;

	struct Viewport {
		VDPixmap mPrimary;
		sint32 mOffsetX;
		sint32 mOffsetY;
	};

	typedef vdfastvector<Viewport> ViewportStack;
	ViewportStack mViewportStack;

	VDDisplayTextRenderer mTextRenderer;
};

#endif
