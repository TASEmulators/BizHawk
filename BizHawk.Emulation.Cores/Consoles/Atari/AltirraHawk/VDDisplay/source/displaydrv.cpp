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

#include <vector>
#include <vd2/system/vdalloc.h>
#include <vd2/system/memory.h>
#include <vd2/system/log.h>
#include <vd2/system/memory.h>
#include <vd2/system/math.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/VDDisplay/compositor.h>
#include <vd2/VDDisplay/display.h>
#include <vd2/VDDisplay/displaydrv.h>

#define VDDEBUG_DISP (void)sizeof printf
//#define VDDEBUG_DISP VDDEBUG

#if 0
	#define DEBUG_LOG(x) VDLog(kVDLogInfo, VDStringW(L##x))
#else
	#define DEBUG_LOG(x)
#endif

using namespace nsVDPixmap;

///////////////////////////////////////////////////////////////////////////

namespace {

	#define TABROW(x)	TABENT(x+0),TABENT(x+1),TABENT(x+2),TABENT(x+3),TABENT(x+4),TABENT(x+5),TABENT(x+6),TABENT(x+7),TABENT(x+8),TABENT(x+9),TABENT(x+10),TABENT(x+11),TABENT(x+12),TABENT(x+13),TABENT(x+14),TABENT(x+15)
	#define TABLE		TABROW(0x00),TABROW(0x10),TABROW(0x20),TABROW(0x30),TABROW(0x40),TABROW(0x50),TABROW(0x60),TABROW(0x70),TABROW(0x80),TABROW(0x90),TABROW(0xA0),TABROW(0xB0),TABROW(0xC0),TABROW(0xD0),TABROW(0xE0),TABROW(0xF0),TABROW(0x100),TABROW(0x110),TABROW(0x120)

	// d     = spacing between shades
	// n     = number of shades
	//
	// require: dn = 255

	const uint8 rdithertab8[256+48]={
	#define	TABENT(x)	((x) > 255 ? 5*36 : (((x)*5) / 255)*36)
		TABLE
	#undef TABENT
	};
	const uint8 gdithertab8[256+48]={
	#define	TABENT(x)	((x) > 255 ? 5* 6 : (((x)*5) / 255)*6)
		TABLE
	#undef TABENT
	};
	const uint8 bdithertab8[256+48]={
	#define	TABENT(x)	((x) > 255 ? 5* 1 : (((x)*5) / 255)*1)
		TABLE
	#undef TABENT
	};
	#undef TABROW
	#undef TABLE
}

// 0 8 2 A
// C 4 E 6
// 3 B 1 9
// F 7 D 5

template<int d0, int d1, int d2, int d3>
struct VDDitherUtils {
	enum {
		rb0 = d0*51/16,
		rb1 = d1*51/16,
		rb2 = d2*51/16,
		rb3 = d3*51/16,
		g0 = d0*51/16,
		g1 = d1*51/16,
		g2 = d2*51/16,
		g3 = d3*51/16,
	};

	static void DoSpan8To8(uint8 *dstp, const uint8 *srcp, int w2, const uint8 *pLogPal, const uint8 *palette) {
		const uint8 *p;

		switch(w2 & 3) {
			do {
		case 0:	p = &palette[4*srcp[0]]; dstp[w2  ] = pLogPal[rdithertab8[rb0+p[2]] + gdithertab8[g0+p[1]] + bdithertab8[rb0+p[0]]];
		case 1:	p = &palette[4*srcp[1]]; dstp[w2+1] = pLogPal[rdithertab8[rb1+p[2]] + gdithertab8[g1+p[1]] + bdithertab8[rb1+p[0]]];
		case 2:	p = &palette[4*srcp[2]]; dstp[w2+2] = pLogPal[rdithertab8[rb2+p[2]] + gdithertab8[g2+p[1]] + bdithertab8[rb2+p[0]]];
		case 3:	p = &palette[4*srcp[3]]; dstp[w2+3] = pLogPal[rdithertab8[rb3+p[2]] + gdithertab8[g3+p[1]] + bdithertab8[rb3+p[0]]];

				srcp += 4;
			} while((w2 += 4) < 0);
		}
	}

	static void DoSpan15To8(uint8 *dstp, const uint16 *srcp, int w2, const uint8 *pLogPal) {
		uint32 px;

		switch(w2 & 3) {
			do {
		case 0:	px = srcp[0];
				dstp[w2  ] = pLogPal[rdithertab8[rb0 + ((px&0x7c00) >> 7)] + gdithertab8[g0 + ((px&0x03e0) >> 2)] + bdithertab8[rb0 + ((px&0x001f) << 3)]];
		case 1:	px = srcp[1];
				dstp[w2+1] = pLogPal[rdithertab8[rb1 + ((px&0x7c00) >> 7)] + gdithertab8[g1 + ((px&0x03e0) >> 2)] + bdithertab8[rb1 + ((px&0x001f) << 3)]];
		case 2:	px = srcp[2];
				dstp[w2+2] = pLogPal[rdithertab8[rb2 + ((px&0x7c00) >> 7)] + gdithertab8[g2 + ((px&0x03e0) >> 2)] + bdithertab8[rb2 + ((px&0x001f) << 3)]];
		case 3:	px = srcp[3];
				dstp[w2+3] = pLogPal[rdithertab8[rb3 + ((px&0x7c00) >> 7)] + gdithertab8[g3 + ((px&0x03e0) >> 2)] + bdithertab8[rb3 + ((px&0x001f) << 3)]];

				srcp += 4;
			} while((w2 += 4) < 0);
		}
	}

	static void DoSpan16To8(uint8 *dstp, const uint16 *srcp, int w2, const uint8 *pLogPal) {
		uint32 px;

		switch(w2 & 3) {
			do {
		case 0:	px = srcp[0];
				dstp[w2  ] = pLogPal[rdithertab8[rb0 + ((px&0xf800) >> 8)] + gdithertab8[g0 + ((px&0x07e0) >> 3)] + bdithertab8[rb0 + ((px&0x001f) << 3)]];
		case 1:	px = srcp[1];
				dstp[w2+1] = pLogPal[rdithertab8[rb1 + ((px&0xf800) >> 8)] + gdithertab8[g1 + ((px&0x07e0) >> 3)] + bdithertab8[rb1 + ((px&0x001f) << 3)]];
		case 2:	px = srcp[2];
				dstp[w2+2] = pLogPal[rdithertab8[rb2 + ((px&0xf800) >> 8)] + gdithertab8[g2 + ((px&0x07e0) >> 3)] + bdithertab8[rb2 + ((px&0x001f) << 3)]];
		case 3:	px = srcp[3];
				dstp[w2+3] = pLogPal[rdithertab8[rb3 + ((px&0xf800) >> 8)] + gdithertab8[g3 + ((px&0x07e0) >> 3)] + bdithertab8[rb3 + ((px&0x001f) << 3)]];

				srcp += 4;
			} while((w2 += 4) < 0);
		}
	}

	static void DoSpan24To8(uint8 *dstp, const uint8 *srcp, int w2, const uint8 *pLogPal) {
		switch(w2 & 3) {
			do {
		case 0:	dstp[w2  ] = pLogPal[rdithertab8[rb0+srcp[ 2]] + gdithertab8[g0+srcp[ 1]] + bdithertab8[rb0+srcp[ 0]]];
		case 1:	dstp[w2+1] = pLogPal[rdithertab8[rb1+srcp[ 5]] + gdithertab8[g1+srcp[ 4]] + bdithertab8[rb1+srcp[ 3]]];
		case 2:	dstp[w2+2] = pLogPal[rdithertab8[rb2+srcp[ 8]] + gdithertab8[g2+srcp[ 7]] + bdithertab8[rb2+srcp[ 6]]];
		case 3:	dstp[w2+3] = pLogPal[rdithertab8[rb3+srcp[11]] + gdithertab8[g3+srcp[10]] + bdithertab8[rb3+srcp[ 9]]];

				srcp += 12;
			} while((w2 += 4) < 0);
		}
	}

	static void DoSpan32To8(uint8 *dstp, const uint8 *srcp, int w2, const uint8 *pLogPal) {
		switch(w2 & 3) {
			do {
		case 0:	dstp[w2  ] = pLogPal[rdithertab8[rb0+srcp[ 2]] + gdithertab8[g0+srcp[ 1]] + bdithertab8[rb0+srcp[ 0]]];
		case 1:	dstp[w2+1] = pLogPal[rdithertab8[rb1+srcp[ 6]] + gdithertab8[g1+srcp[ 5]] + bdithertab8[rb1+srcp[ 4]]];
		case 2:	dstp[w2+2] = pLogPal[rdithertab8[rb2+srcp[10]] + gdithertab8[g2+srcp[ 9]] + bdithertab8[rb2+srcp[ 8]]];
		case 3:	dstp[w2+3] = pLogPal[rdithertab8[rb3+srcp[14]] + gdithertab8[g3+srcp[13]] + bdithertab8[rb3+srcp[12]]];

				srcp += 16;
			} while((w2 += 4) < 0);
		}
	}
};

void VDDitherImage8To8(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal, const uint8 *palette) {
	int h = dst.h;
	int w = dst.w;

	uint8 *dstp0 = (uint8 *)dst.data;
	const uint8 *srcp0 = (const uint8 *)src.data;

	do {
		int w2 = -w;

		uint8 *dstp = dstp0 + w - (w2&3);
		const uint8 *srcp = srcp0;

		switch(h & 3) {
			case 0: VDDitherUtils< 0, 8, 2,10>::DoSpan8To8(dstp, srcp, w2, pLogPal, palette); break;
			case 1: VDDitherUtils<12, 4,14, 6>::DoSpan8To8(dstp, srcp, w2, pLogPal, palette); break;
			case 2: VDDitherUtils< 3,11, 1, 9>::DoSpan8To8(dstp, srcp, w2, pLogPal, palette); break;
			case 3: VDDitherUtils<15, 7,13, 5>::DoSpan8To8(dstp, srcp, w2, pLogPal, palette); break;
		}

		dstp0 += dst.pitch;
		srcp0 = (const uint8 *)((const char *)srcp0 + src.pitch);
	} while(--h);
}

void VDDitherImage15To8(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal) {
	int h = dst.h;
	int w = dst.w;

	uint8 *dstp0 = (uint8 *)dst.data;
	const uint16 *srcp0 = (const uint16 *)src.data;

	do {
		int w2 = -w;

		uint8 *dstp = dstp0 + w - (w2&3);
		const uint16 *srcp = srcp0;

		switch(h & 3) {
			case 0: VDDitherUtils< 0, 8, 2,10>::DoSpan15To8(dstp, srcp, w2, pLogPal); break;
			case 1: VDDitherUtils<12, 4,14, 6>::DoSpan15To8(dstp, srcp, w2, pLogPal); break;
			case 2: VDDitherUtils< 3,11, 1, 9>::DoSpan15To8(dstp, srcp, w2, pLogPal); break;
			case 3: VDDitherUtils<15, 7,13, 5>::DoSpan15To8(dstp, srcp, w2, pLogPal); break;
		}

		dstp0 += dst.pitch;
		srcp0 = (const uint16 *)((const char *)srcp0 + src.pitch);
	} while(--h);
}

void VDDitherImage16To8(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal) {
	int h = dst.h;
	int w = dst.w;

	uint8 *dstp0 = (uint8 *)dst.data;
	const uint16 *srcp0 = (const uint16 *)src.data;

	do {
		int w2 = -w;

		uint8 *dstp = dstp0 + w - (w2&3);
		const uint16 *srcp = srcp0;

		switch(h & 3) {
			case 0: VDDitherUtils< 0, 8, 2,10>::DoSpan16To8(dstp, srcp, w2, pLogPal); break;
			case 1: VDDitherUtils<12, 4,14, 6>::DoSpan16To8(dstp, srcp, w2, pLogPal); break;
			case 2: VDDitherUtils< 3,11, 1, 9>::DoSpan16To8(dstp, srcp, w2, pLogPal); break;
			case 3: VDDitherUtils<15, 7,13, 5>::DoSpan16To8(dstp, srcp, w2, pLogPal); break;
		}

		dstp0 += dst.pitch;
		srcp0 = (const uint16 *)((const char *)srcp0 + src.pitch);
	} while(--h);
}

void VDDitherImage24To8(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal) {
	int h = dst.h;
	int w = dst.w;

	uint8 *dstp0 = (uint8 *)dst.data;
	const uint8 *srcp0 = (const uint8 *)src.data;

	do {
		int w2 = -w;

		uint8 *dstp = dstp0 + w - (w2&3);
		const uint8 *srcp = srcp0;

		switch(h & 3) {
			case 0: VDDitherUtils< 0, 8, 2,10>::DoSpan24To8(dstp, srcp, w2, pLogPal); break;
			case 1: VDDitherUtils<12, 4,14, 6>::DoSpan24To8(dstp, srcp, w2, pLogPal); break;
			case 2: VDDitherUtils< 3,11, 1, 9>::DoSpan24To8(dstp, srcp, w2, pLogPal); break;
			case 3: VDDitherUtils<15, 7,13, 5>::DoSpan24To8(dstp, srcp, w2, pLogPal); break;
		}

		dstp0 += dst.pitch;
		srcp0 += src.pitch;
	} while(--h);
}

void VDDitherImage32To8(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal) {
	int h = dst.h;
	int w = dst.w;

	uint8 *dstp0 = (uint8 *)dst.data;
	const uint8 *srcp0 = (const uint8 *)src.data;

	do {
		int w2 = -w;

		uint8 *dstp = dstp0 + w - (w2&3);
		const uint8 *srcp = srcp0;

		switch(h & 3) {
			case 0: VDDitherUtils< 0, 8, 2,10>::DoSpan32To8(dstp, srcp, w2, pLogPal); break;
			case 1: VDDitherUtils<12, 4,14, 6>::DoSpan32To8(dstp, srcp, w2, pLogPal); break;
			case 2: VDDitherUtils< 3,11, 1, 9>::DoSpan32To8(dstp, srcp, w2, pLogPal); break;
			case 3: VDDitherUtils<15, 7,13, 5>::DoSpan32To8(dstp, srcp, w2, pLogPal); break;
		}

		dstp0 += dst.pitch;
		srcp0 += src.pitch;
	} while(--h);
}

void VDDitherImage(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal) {
	VDASSERT(dst.w == src.w && dst.h == src.h);

	if (dst.w<=0 || dst.h<=0)
		return;

	if (dst.format == kPixFormat_Pal8) {
		switch(src.format) {
		case kPixFormat_Pal8:
			VDDitherImage8To8(dst, src, pLogPal, (const uint8 *)src.palette);
			break;
		case kPixFormat_XRGB1555:
			VDDitherImage15To8(dst, src, pLogPal);
			break;
		case kPixFormat_RGB565:
			VDDitherImage16To8(dst, src, pLogPal);
			break;
		case kPixFormat_RGB888:
			VDDitherImage24To8(dst, src, pLogPal);
			break;
		case kPixFormat_XRGB8888:
			VDDitherImage32To8(dst, src, pLogPal);
			break;
		}
	}
}

///////////////////////////////////////////////////////////////////////////////

VDVideoDisplayMinidriver::VDVideoDisplayMinidriver()
	: mbDisplayDebugInfo(false)
	, mbHighPrecision(false)
	, mbDestRectEnabled(false)
	, mClientRect(0, 0, 0, 0)
	, mDestRect(0, 0, 0, 0)
	, mDrawRect(0, 0, 0, 0)
	, mBackgroundColor(0)
	, mColorOverride(0)
	, mPixelSharpnessX(1.0f)
	, mPixelSharpnessY(1.0f)
	, mpCompositor(NULL)
{
}

VDVideoDisplayMinidriver::~VDVideoDisplayMinidriver() {
	vdsaferelease <<= mpCompositor;
}

bool VDVideoDisplayMinidriver::PreInit(HWND hwnd, HMONITOR hmonitor) {
	return true;
}

bool VDVideoDisplayMinidriver::IsFramePending() {
	return false;
}

bool VDVideoDisplayMinidriver::IsScreenFXSupported() const {
	return false;
}

void VDVideoDisplayMinidriver::SetFilterMode(FilterMode mode) {
}

void VDVideoDisplayMinidriver::SetFullScreen(bool fullscreen, uint32 w, uint32 h, uint32 refresh, bool use16bit) {
}

void VDVideoDisplayMinidriver::SetDisplayDebugInfo(bool enable) {
	mbDisplayDebugInfo = enable;
}

void VDVideoDisplayMinidriver::SetColorOverride(uint32 color) {
	mColorOverride = color;
}

void VDVideoDisplayMinidriver::SetHighPrecision(bool enable) {
	mbHighPrecision = enable;
}

void VDVideoDisplayMinidriver::SetDestRect(const vdrect32 *r, uint32 color) {
	if (r) {
		mDestRect = *r;

		if (mDestRect.right < mDestRect.left)
			mDestRect.right = mDestRect.left;

		if (mDestRect.bottom < mDestRect.top)
			mDestRect.bottom = mDestRect.top;

		mbDestRectEnabled = true;
	} else {
		mbDestRectEnabled = false;
	}

	mBackgroundColor = color;
	UpdateDrawRect();
}

void VDVideoDisplayMinidriver::SetPixelSharpness(float xfactor, float yfactor) {
	mPixelSharpnessX = xfactor;
	mPixelSharpnessY = yfactor;
}

bool VDVideoDisplayMinidriver::SetScreenFX(const VDVideoDisplayScreenFXInfo *screenFX) {
	return !screenFX;
}

void VDVideoDisplayMinidriver::SetCompositor(IVDDisplayCompositor *compositor) {
	if (mpCompositor == compositor)
		return;

	auto *dce = GetDisplayCompositionEngine();
	if (!dce)
		compositor = nullptr;

	if (mpCompositor) {
		mpCompositor->DetachCompositor();
		mpCompositor->Release();
	}

	mpCompositor = compositor;

	if (compositor) {
		compositor->AddRef();
		compositor->AttachCompositor(*dce);
	}
}

bool VDVideoDisplayMinidriver::Tick(int id) {
	return true;
}

void VDVideoDisplayMinidriver::Poll() {
}

bool VDVideoDisplayMinidriver::Resize(int width, int height) {
	mClientRect.set(0, 0, width, height);
	UpdateDrawRect();
	return true;
}

bool VDVideoDisplayMinidriver::Invalidate() {
	return false;
}

bool VDVideoDisplayMinidriver::SetSubrect(const vdrect32 *r) {
	return false;
}

void VDVideoDisplayMinidriver::SetLogicalPalette(const uint8 *pLogicalPalette) {
}

float VDVideoDisplayMinidriver::GetSyncDelta() const {
	return 0.0f;
}

void VDVideoDisplayMinidriver::GetFormatString(const VDVideoDisplaySourceInfo& info, VDStringA& s) {
	s.sprintf("%dx%d (%s)"
		, info.pixmap.w
		, info.pixmap.h
		, VDPixmapGetInfo(info.pixmap.format).name
		);
}

void VDVideoDisplayMinidriver::UpdateDrawRect() {
	mDrawRect = mClientRect;
	mBorderRectCount = 0;

	if (mbDestRectEnabled) {
		mDrawRect = mDestRect;

		if (mDrawRect.left < mClientRect.left)
			mDrawRect.left = mClientRect.left;

		if (mDrawRect.top < mClientRect.top)
			mDrawRect.top = mClientRect.top;

		if (mDrawRect.right > mClientRect.right)
			mDrawRect.right = mClientRect.right;

		if (mDrawRect.bottom > mClientRect.bottom)
			mDrawRect.bottom = mClientRect.bottom;

		vdrect32 *r = mBorderRects;
		if (mDrawRect.empty()) {
			*r++ = mClientRect;
		} else {
			if (mDrawRect.top > mClientRect.top) {
				r->left = mClientRect.left;
				r->top = mClientRect.top;
				r->right = mClientRect.right;
				r->bottom = mDrawRect.top;
				++r;
			}

			if (mDrawRect.left > mClientRect.left) {
				r->left = mClientRect.left;
				r->top = mDrawRect.top;
				r->right = mDrawRect.left;
				r->bottom = mDrawRect.bottom;
				++r;
			}

			if (mDrawRect.right < mClientRect.right) {
				r->left = mDrawRect.right;
				r->top = mDrawRect.top;
				r->right = mClientRect.right;
				r->bottom = mDrawRect.bottom;
				++r;
			}

			if (mDrawRect.bottom < mClientRect.bottom) {
				r->left = mClientRect.left;
				r->top = mDrawRect.bottom;
				r->right = mClientRect.right;
				r->bottom = mClientRect.bottom;
				++r;
			}
		}

		mBorderRectCount = r - mBorderRects;
	}
}
