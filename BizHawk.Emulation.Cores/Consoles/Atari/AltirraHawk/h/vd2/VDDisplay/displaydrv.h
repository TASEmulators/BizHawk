//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2005 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_VD2_VDDISPLAY_DISPLAYDRV_H
#define f_VD2_VDDISPLAY_DISPLAYDRV_H

#include <windows.h>
#include <vd2/system/vectors.h>
#include <vd2/Kasumi/pixmap.h>

class VDStringA;
class IVDDisplayCompositor;
class IVDDisplayCompositionEngine;
struct VDVideoDisplayScreenFXInfo;

class IVDVideoDisplayMinidriverCallback {
public:
	// Release the current frame, as it has been rendered.
	virtual void ReleaseActiveFrame() = 0;

	// Request that the next frame in the queue be dispatched, if there is
	// one. This is only dispatched after the current call exits; it will
	// not occur during the call to RequestNextFrame().
	virtual void RequestNextFrame() = 0;

	// Queue a present. This causes PresentQueued() to be called on the
	// minidriver on the rendering thread. This method is thread-safe and
	// may be called asynchronously.
	virtual void QueuePresent() = 0;
};

struct VDVideoDisplaySourceInfo {
	VDPixmap	pixmap;
	int			bpp;
	int			bpr;
	bool		bAllowConversion;
	bool		bPersistent;
	bool		use16bit;
	IVDVideoDisplayMinidriverCallback *mpCB;
};

class VDINTERFACE IVDVideoDisplayMinidriver {
public:
	enum UpdateMode : uint32 {
		kModeNone		= 0x00000000,
		kModeVSync		= 0x00000004,
		kModeDoNotWait	= 0x00000800,
		kModeAll		= 0x00000f0f
	};

	enum FilterMode {
		kFilterAnySuitable,
		kFilterPoint,
		kFilterBilinear,
		kFilterBicubic,
		kFilterModeCount
	};

	virtual ~IVDVideoDisplayMinidriver() {}

	virtual bool PreInit(HWND hwnd, HMONITOR hmonitor) = 0;
	virtual bool Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info) = 0;
	virtual void Shutdown() = 0;

	virtual bool ModifySource(const VDVideoDisplaySourceInfo& info) = 0;

	virtual bool IsValid() = 0;
	virtual bool IsFramePending() = 0;
	virtual bool IsScreenFXSupported() const = 0;
	virtual void SetFilterMode(FilterMode mode) = 0;
	virtual void SetFullScreen(bool fullscreen, uint32 w, uint32 h, uint32 refresh, bool use16bit) = 0;
	virtual void SetDisplayDebugInfo(bool enable) = 0;
	virtual void SetColorOverride(uint32 color) = 0;
	virtual void SetHighPrecision(bool enable) = 0;
	virtual void SetDestRect(const vdrect32 *r, uint32 color) = 0;
	virtual void SetPixelSharpness(float xfactor, float yfactor) = 0;
	virtual bool SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX) = 0;
	virtual void SetCompositor(IVDDisplayCompositor *compositor) = 0;

	virtual bool Tick(int id) = 0;
	virtual void Poll() = 0;
	virtual bool Resize(int w, int h) = 0;
	virtual bool Invalidate() = 0;
	virtual bool Update(UpdateMode) = 0;
	virtual void Refresh(UpdateMode) = 0;
	virtual bool Paint(HDC hdc, const RECT& rClient, UpdateMode lastUpdateMode) = 0;
	virtual void PresentQueued() = 0;

	virtual bool SetSubrect(const vdrect32 *r) = 0;
	virtual void SetLogicalPalette(const uint8 *pLogicalPalette) = 0;

	virtual bool AreVSyncTicksNeeded() const = 0;

	virtual float GetSyncDelta() const = 0;
};

class VDINTERFACE VDVideoDisplayMinidriver : public IVDVideoDisplayMinidriver {
	VDVideoDisplayMinidriver(const VDVideoDisplayMinidriver&) = delete;
	VDVideoDisplayMinidriver& operator=(const VDVideoDisplayMinidriver&) = delete;
public:
	VDVideoDisplayMinidriver();
	~VDVideoDisplayMinidriver();

	bool PreInit(HWND hwnd, HMONITOR hmonitor) override;

	virtual bool IsFramePending() override;
	virtual bool IsScreenFXSupported() const override;
	virtual void SetFilterMode(FilterMode mode);
	virtual void SetFullScreen(bool fullscreen, uint32 w, uint32 h, uint32 refresh, bool use16bit);
	virtual void SetDisplayDebugInfo(bool enable);
	virtual void SetColorOverride(uint32 color);
	virtual void SetHighPrecision(bool enable);
	virtual void SetDestRect(const vdrect32 *r, uint32 color);
	virtual void SetPixelSharpness(float xfactor, float yfactor);
	virtual bool SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX);
	virtual void SetCompositor(IVDDisplayCompositor *compositor);

	virtual bool Tick(int id);
	virtual void Poll();
	virtual bool Resize(int w, int h);
	virtual bool Invalidate();
	virtual void PresentQueued() {}

	virtual bool SetSubrect(const vdrect32 *r);
	virtual void SetLogicalPalette(const uint8 *pLogicalPalette);

	virtual bool AreVSyncTicksNeeded() const { return true; }
	virtual float GetSyncDelta() const;

protected:
	virtual IVDDisplayCompositionEngine *GetDisplayCompositionEngine() = 0;

protected:
	static void GetFormatString(const VDVideoDisplaySourceInfo& info, VDStringA& s);
	void UpdateDrawRect();

	bool	mbDisplayDebugInfo;
	bool	mbHighPrecision;
	bool	mbDestRectEnabled;
	vdrect32	mClientRect;		// (0,0)-(w,h)
	vdrect32	mDrawRect;			// DestRect clipped against ClientRect
	vdrect32	mDestRect;
	uint32	mBackgroundColor;
	uint32	mColorOverride;
	float	mPixelSharpnessX;
	float	mPixelSharpnessY;

	vdrect32	mBorderRects[4];
	int			mBorderRectCount;

	IVDDisplayCompositor *mpCompositor;
};

IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverOpenGL();
IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverDirectDraw(bool enableOverlays, bool enableSecondaryDraw);
IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverGDI();
IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverDX9(bool clipToMonitor, bool use9ex);

#endif
