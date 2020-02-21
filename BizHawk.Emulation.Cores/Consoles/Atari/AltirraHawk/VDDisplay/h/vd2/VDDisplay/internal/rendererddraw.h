#ifndef f_VD2_VDDISPLAY_INTERNAL_RENDERERDDRAW_H
#define f_VD2_VDDISPLAY_INTERNAL_RENDERERDDRAW_H

#include <vd2/VDDisplay/internal/directdraw.h>
#include <vd2/VDDisplay/renderersoft.h>

class VDDisplayCachedImageDirectDraw;

class VDDisplayRendererDirectDraw : public IVDDisplayRenderer, protected IVDDirectDrawClient {
	VDDisplayRendererDirectDraw(const VDDisplayRendererDirectDraw&);
	VDDisplayRendererDirectDraw& operator=(const VDDisplayRendererDirectDraw&);
public:
	VDDisplayRendererDirectDraw();
	~VDDisplayRendererDirectDraw();

	void Init(IVDDirectDrawManager *ddman);
	void Shutdown();

	IDirectDrawSurface2 *GetCompositionSurface() const { return mpddsComposition; }

	bool Begin(uint32 w, uint32 h, nsVDPixmap::VDPixmapFormat format);
	bool End(sint32 x, sint32 y);

	const VDDisplayRendererCaps& GetCaps();
	VDDisplayTextRenderer *GetTextRenderer() { return NULL; }

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

public:
	void DirectDrawShutdown();
	void DirectDrawPrimaryRestored();

protected:
	void DrawLine(sint32 x1, sint32 y1, sint32 x2, sint32 y2);
	bool LockPrimary();
	void UnlockPrimary();

	VDDisplayCachedImageDirectDraw *GetCachedImage(VDDisplayImageView& imageView, bool softrender);

	IVDDirectDrawManager *mpddman;
	IDirectDrawSurface2 *mpddsComposition;
	sint32	mWidth;
	sint32	mHeight;
	vdrect32 mClipRect;
	sint32	mOffsetX;
	sint32	mOffsetY;

	uint32	mColor;
	uint32	mNativeColor;
	nsVDPixmap::VDPixmapFormat mColorFormat;

	VDDisplaySubRenderCache *mpCurrentSubRender;
	sint32	mSubRenderX;
	sint32	mSubRenderY;

	VDPixmap	mLockedPrimary;

	struct Viewport {
		vdrect32 mClipRect;
		sint32 mOffsetX;
		sint32 mOffsetY;
	};

	vdfastvector<Viewport> mViewportStack;

	VDDisplayRendererSoft mFallback;

	typedef vdlist<VDDisplayCachedImageDirectDraw> CachedImages;
	
	CachedImages mCachedImages;
};

#endif
