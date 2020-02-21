//	Altirra - Atari 800/800XL emulator
//	UI library
//	Copyright (C) 2008-2015 Avery Lee
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

#include <stdafx.h>
#include <windows.h>
#include <windowsx.h>
#include <commctrl.h>
#include <tchar.h>
#include <uxtheme.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>
#include <vd2/system/w32assist.h>
#include <vd2/system/math.h>
#include <at/atui/constants.h>
#include <at/atnativeui/uiframe.h>

#ifdef NTDDI_WIN10_RS3
#include <shellscalingapi.h>
#endif

#pragma comment(lib, "uxtheme")

// Requires Windows XP
#ifndef WM_THEMECHANGED
#define WM_THEMECHANGED 0x031A
#endif

#ifndef UIS_SET
#define UIS_SET                         1
#endif

#ifndef UIS_CLEAR
#define UIS_CLEAR                       2
#endif

#ifndef UIS_INITIALIZE
#define UIS_INITIALIZE                  3
#endif

#ifndef UISF_HIDEFOCUS
#define UISF_HIDEFOCUS                  0x1
#endif

#ifndef UISF_HIDEACCEL
#define UISF_HIDEACCEL                  0x2
#endif

// Requires Windows Vista
#if WINVER < 0x0600
	typedef enum DEVICE_SCALE_FACTOR {
		SCALE_100_PERCENT = 100,
		SCALE_140_PERCENT = 140,
		SCALE_180_PERCENT = 180
	} DEVICE_SCALE_FACTOR;
#endif

#pragma comment(lib, "msimg32")

///////////////////////////////////////////////////////////////////////////////

extern ATContainerWindow *g_pMainWindow;

///////////////////////////////////////////////////////////////////////////////

void ATInitUIFrameSystem() {
}

void ATShutdownUIFrameSystem() {
}

uint32 ATUIGetGlobalDpiW32() {
	HDC hdc = GetDC(nullptr);
	uint32 dpi = 0;

	if (hdc) {
		dpi = GetDeviceCaps(hdc, LOGPIXELSY);
		ReleaseDC(nullptr, hdc);
	}

	VDASSERT(dpi);

	return dpi ? dpi : 96;
}

uint32 ATUIGetWindowDpiW32(HWND hwnd) {
	uint32 dpi = 0;

	hwnd = ::GetAncestor(hwnd, GA_ROOT);

	if (VDIsAtLeast81W32()) {
		HMONITOR hmon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

		if (hmon) {
			dpi = ATUIGetMonitorDpiW32(hmon);
		}
	} else {
		HDC hdc = GetDC(hwnd);

		if (hdc) {
			dpi = GetDeviceCaps(hdc, LOGPIXELSY);
			ReleaseDC(hwnd, hdc);
		}
	}

	return dpi;
}

uint32 ATUIGetMonitorDpiW32(HMONITOR hMonitor) {
	uint32 dpi = 0;
	UINT dpiX = 0;
	UINT dpiY = 0;

	if (VDIsAtLeast81W32()) {
		HMODULE hmodShCore = VDLoadSystemLibraryW32("shcore");
		if (hmodShCore) {
			typedef HRESULT (__stdcall *tpGetDpiForMonitor)(HMONITOR, MONITOR_DPI_TYPE, UINT *, UINT *);
			tpGetDpiForMonitor pGetDpiForMonitor = (tpGetDpiForMonitor)GetProcAddress(hmodShCore, "GetDpiForMonitor");

			if (pGetDpiForMonitor) {
				HRESULT hr = pGetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, &dpiX, &dpiY);
				if (SUCCEEDED(hr) && dpiY) {
					dpi = dpiY;
				}
			}

			FreeLibrary(hmodShCore);
		}
	}

	if (!dpi)
		dpi = ATUIGetGlobalDpiW32();

	return dpi;
}

HFONT ATUICreateDefaultFontForDpiW32(uint32 dpi) {
	NONCLIENTMETRICS ncm = { sizeof(NONCLIENTMETRICS) };
	HFONT hfont = nullptr;

	if (SystemParametersInfo(SPI_GETNONCLIENTMETRICS, ncm.cbSize, &ncm, 0)) {
		if (dpi) {
			int globalDpi = ATUIGetGlobalDpiW32();

			ncm.lfMessageFont.lfHeight = MulDiv(ncm.lfMessageFont.lfHeight, (int)dpi, globalDpi);
		}

		hfont = CreateFontIndirect(&ncm.lfMessageFont);
	}

	if (!hfont)
		hfont = (HFONT)GetStockObject(DEFAULT_GUI_FONT);

	return hfont;
}

///////////////////////////////////////////////////////////////////////////////

ATContainerResizer::ATContainerResizer()
	: mhdwp(NULL)
{
}

void ATContainerResizer::LayoutWindow(HWND hwnd, int x, int y, int width, int height, bool visible) {
	VDASSERT(hwnd);
	VDASSERT(IsWindow(hwnd));

	if (visible && !(GetWindowLong(hwnd, GWL_STYLE) & WS_VISIBLE)) {
		mWindowsToShow.push_back(hwnd);
		SetWindowPos(hwnd, NULL, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
		return;
	}

	if (!mhdwp)
		mhdwp = BeginDeferWindowPos(4);

	if (mhdwp) {
		HDWP hdwp = DeferWindowPos(mhdwp, hwnd, NULL, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);

		if (hdwp) {
			mhdwp = hdwp;
			return;
		}
	}

	SetWindowPos(hwnd, NULL, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
}

void ATContainerResizer::ResizeWindow(HWND hwnd, int width, int height) {
	VDASSERT(hwnd);
	VDASSERT(IsWindow(hwnd));

	if (!mhdwp)
		mhdwp = BeginDeferWindowPos(4);

	if (mhdwp) {
		HDWP hdwp = DeferWindowPos(mhdwp, hwnd, NULL, 0, 0, width, height, SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOMOVE);

		if (hdwp) {
			mhdwp = hdwp;
			return;
		}

		// MSDN says that you should not call EndDeferWindowPos() on a failure, but Internet
		// lore says that this is necessary to avoid a leak:
		// http://www.itimdp4.com/dp4kb/a4000035.htm
	}

	VDVERIFY(SetWindowPos(hwnd, NULL, 0, 0, width, height, SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOMOVE));
}

void ATContainerResizer::Flush() {
	if (mhdwp) {
		VDVERIFY(EndDeferWindowPos(mhdwp));
		mhdwp = NULL;
	}

	while(!mWindowsToShow.empty()) {
		HWND hwnd = mWindowsToShow.back();

		ShowWindow(hwnd, SW_SHOWNOACTIVATE);
		mWindowsToShow.pop_back();
	}
}

///////////////////////////////////////////////////////////////////////////////

ATContainerSplitterBar::ATContainerSplitterBar()
	: mpControlledPane(NULL)
	, mbVertical(false)
	, mDistanceOffset(0)
{
	SetTouchMode(kATUITouchMode_Immediate);
}

ATContainerSplitterBar::~ATContainerSplitterBar() {
}

bool ATContainerSplitterBar::Init(HWND hwndParent, ATContainerDockingPane *pane, bool vertical) {
	mbVertical = vertical;
	mpControlledPane = pane;

	if (!mhwnd) {
		if (!CreateWindow(MAKEINTATOM(sWndClass), _T(""), WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS, 0, 0, 0, 0, hwndParent, (HMENU)(UINT)0, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this)))
			return false;
	}

	return true;
}

void ATContainerSplitterBar::Shutdown() {
	if (mhwnd)
		DestroyWindow(mhwnd);
}

void ATContainerSplitterBar::BeginDrag(int screenX, int screenY) {
	POINT pt = { screenX, screenY };
	MapWindowPoints(nullptr, GetParent(mhwnd), &pt, 1);

	InternalBeginDrag(pt.x, pt.y);
}

LRESULT ATContainerSplitterBar::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
	case WM_SIZE:
		OnSize();
		break;

	case WM_PAINT:
		OnPaint();
		break;

	case WM_LBUTTONDOWN:
		OnLButtonDown(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;

	case WM_LBUTTONUP:
		OnLButtonUp(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;

	case WM_MOUSEMOVE:
		OnMouseMove(wParam, (SHORT)LOWORD(lParam), (SHORT)HIWORD(lParam));
		return 0;

	case WM_CAPTURECHANGED:
		OnCaptureChanged((HWND)lParam);
		return 0;

	case WM_SETCURSOR:
		SetCursor(LoadCursor(NULL, mbVertical ? IDC_SIZEWE : IDC_SIZENS));
		return TRUE;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void ATContainerSplitterBar::OnPaint() {
	PAINTSTRUCT ps;

	if (HDC hdc = BeginPaint(mhwnd, &ps)) {
		RECT r;
		GetClientRect(mhwnd, &r);

		if (GetCapture() == mhwnd)
			FillRect(hdc, &r, (HBRUSH)(COLOR_3DSHADOW+1));
		else
			FillRect(hdc, &r, (HBRUSH)(COLOR_3DFACE+1));

		EndPaint(mhwnd, &ps);
	}
}

void ATContainerSplitterBar::OnSize() {
	InvalidateRect(mhwnd, NULL, TRUE);
}

void ATContainerSplitterBar::OnLButtonDown(WPARAM wParam, int x, int y) {
	POINT pt = {x, y};
	MapWindowPoints(mhwnd, GetParent(mhwnd), &pt, 1);

	InternalBeginDrag(pt.x, pt.y);
}

void ATContainerSplitterBar::OnLButtonUp(WPARAM wParam, int x, int y) {
	if (GetCapture() == mhwnd) {
		ReleaseCapture();
		InvalidateRect(mhwnd, NULL, FALSE);
	}
}

void ATContainerSplitterBar::OnMouseMove(WPARAM wParam, int x, int y) {
	if (GetCapture() != mhwnd)
		return;

	POINT pt = {x, y};
	MapWindowPoints(mhwnd, GetParent(mhwnd), &pt, 1);

	const vdrect32& rParentPane = mpControlledPane->GetParentPane()->GetArea();
	int parentW = rParentPane.width();
	int parentH = rParentPane.height();

	switch(mpControlledPane->GetDockCode()) {
	case kATContainerDockLeft:
		mpControlledPane->SetDockFraction((float)(mDistanceOffset + pt.x) / (float)parentW);
		break;
	case kATContainerDockRight:
		mpControlledPane->SetDockFraction((float)(mDistanceOffset - pt.x) / (float)parentW);
		break;
	case kATContainerDockTop:
		mpControlledPane->SetDockFraction((float)(mDistanceOffset + pt.y) / (float)parentH);
		break;
	case kATContainerDockBottom:
		mpControlledPane->SetDockFraction((float)(mDistanceOffset - pt.y) / (float)parentH);
		break;
	}
}

void ATContainerSplitterBar::OnCaptureChanged(HWND hwndNewCapture) {
}

void ATContainerSplitterBar::InternalBeginDrag(int x, int y) {
	RECT r;
	if (!GetClientRect(mhwnd, &r))
		return;

	const vdrect32& rPane = mpControlledPane->GetArea();

	switch(mpControlledPane->GetDockCode()) {
	case kATContainerDockLeft:
		mDistanceOffset = rPane.width() - x;
		break;
	case kATContainerDockRight:
		mDistanceOffset = rPane.width() + x;
		break;
	case kATContainerDockTop:
		mDistanceOffset = rPane.height() - y;
		break;
	case kATContainerDockBottom:
		mDistanceOffset = rPane.height() + y;
		break;
	}

	SetCapture(mhwnd);
	InvalidateRect(mhwnd, NULL, FALSE);
}

///////////////////////////////////////////////////////////////////////////////

ATDragHandleWindow::ATDragHandleWindow()
	: mX(0)
	, mY(0)
{
}

ATDragHandleWindow::~ATDragHandleWindow() {
}

VDGUIHandle ATDragHandleWindow::Create(int x, int y, int cx, int cy, VDGUIHandle parent, int id) {
	HWND hwnd = CreateWindowEx(WS_EX_TOPMOST|WS_EX_TOOLWINDOW|WS_EX_NOACTIVATE, (LPCTSTR)(uintptr_t)sWndClass, _T(""), WS_POPUP, x, y, cx, cy, (HWND)parent, (HMENU)(INT_PTR)id, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));

	if (hwnd)
		ShowWindow(hwnd, SW_SHOWNOACTIVATE);

	return (VDGUIHandle)hwnd;
}

void ATDragHandleWindow::Destroy() {
	if (mhwnd)
		DestroyWindow(mhwnd);
}

int ATDragHandleWindow::HitTest(int screenX, int screenY) {
	int xdist = screenX - mX;
	int ydist = screenY - mY;
	int dist = abs(xdist) + abs(ydist);

	if (dist >= 37)
		return -1;

	if (xdist < -18)
		return kATContainerDockLeft;

	if (xdist > +18)
		return kATContainerDockRight;

	if (ydist < -18)
		return kATContainerDockTop;

	if (ydist > +18)
		return kATContainerDockBottom;

	return kATContainerDockCenter;
}

LRESULT ATDragHandleWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			OnCreate();
			break;

		case WM_MOVE:
			OnMove();
			break;

		case WM_PAINT:
			OnPaint();
			return 0;

		case WM_ERASEBKGND:
			return FALSE;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void ATDragHandleWindow::OnCreate() {
	POINT pt[8]={
		{  0<<0, 37<<0 },
		{  0<<0, 38<<0 },
		{ 37<<0, 75<<0 },
		{ 38<<0, 75<<0 },
		{ 75<<0, 38<<0 },
		{ 75<<0, 37<<0 },
		{ 38<<0,  0<<0 },
		{ 37<<0,  0<<0 },
	};

	HRGN rgn = CreatePolygonRgn(pt, 8, ALTERNATE);
	if (rgn) {
		if (!SetWindowRgn(mhwnd, rgn, TRUE))
			DeleteObject(rgn);
	}

	OnMove();
}

void ATDragHandleWindow::OnMove() {
	RECT r;
	GetWindowRect(mhwnd, &r);
	mX = r.left + 37;
	mY = r.top + 37;
}

void ATDragHandleWindow::OnPaint() {
	PAINTSTRUCT ps;
	HDC hdc = BeginPaint(mhwnd, &ps);
	if (hdc) {
		RECT r;
		GetClientRect(mhwnd, &r);
		FillRect(hdc, &r, (HBRUSH)(COLOR_3DFACE + 1));

		int saveIndex = SaveDC(hdc);
		if (saveIndex) {
			SelectObject(hdc, GetStockObject(DC_PEN));

			uint32 a0 = GetSysColor(COLOR_3DSHADOW);
			uint32 a2 = GetSysColor(COLOR_3DFACE);
			uint32 a4 = GetSysColor(COLOR_3DHIGHLIGHT);
			uint32 a1 = (a0|a2) - (((a0^a2) & 0xfefefe)>>1);
			uint32 a3 = (a2|a4) - (((a2^a4) & 0xfefefe)>>1);
			uint32 b0 = GetSysColor(COLOR_3DDKSHADOW);

			MoveToEx(hdc, 0, 37, NULL);
			SetDCPenColor(hdc, a4);
			LineTo(hdc, 37, 0);
			SetDCPenColor(hdc, a3);
			LineTo(hdc, 74, 37);
			SetDCPenColor(hdc, b0);
			LineTo(hdc, 37, 74);
			SetDCPenColor(hdc, a1);
			LineTo(hdc, 0, 37);

			MoveToEx(hdc, 1, 37, NULL);
			SetDCPenColor(hdc, a4);
			LineTo(hdc, 37, 1);
			SetDCPenColor(hdc, a3);
			LineTo(hdc, 73, 37);
			SetDCPenColor(hdc, a0);
			LineTo(hdc, 37, 73);
			SetDCPenColor(hdc, a1);
			LineTo(hdc, 1, 37);

			MoveToEx(hdc, 19, 55, NULL);
			SetDCPenColor(hdc, a1);
			LineTo(hdc, 19, 19);
			LineTo(hdc, 55, 19);
			SetDCPenColor(hdc, a3);
			LineTo(hdc, 55, 55);
			LineTo(hdc, 19, 55);

			MoveToEx(hdc, 20, 54, NULL);
			SetDCPenColor(hdc, a3);
			LineTo(hdc, 20, 20);
			LineTo(hdc, 54, 20);
			SetDCPenColor(hdc, a1);
			LineTo(hdc, 54, 54);
			LineTo(hdc, 20, 54);

			RestoreDC(hdc, saveIndex);
		}

		EndPaint(mhwnd, &ps);
	}
}

///////////////////////////////////////////////////////////////////////////////

ATContainerDockingPane::ATContainerDockingPane(ATContainerWindow *parent)
	: mpParent(parent)
	, mpDockParent(NULL)
	, mArea(0, 0, 0, 0)
	, mCenterArea(0, 0, 0, 0)
	, mDockCode(-1)
	, mDockFraction(0)
	, mbFullScreen(false)
	, mbFullScreenLayout(false)
	, mbPinned(false)
	, mbLayoutInvalid(false)
	, mbDescendantLayoutInvalid(false)
	, mVisibleFrameIndex(-1)
	, mhwndTabControl(NULL)
{
}

ATContainerDockingPane::~ATContainerDockingPane() {
	while(!mChildren.empty()) {
		ATContainerDockingPane *child = mChildren.back();
		mChildren.pop_back();

		VDASSERT(child->mpDockParent == this);
		child->mpDockParent = NULL;
		child->mDockCode = -1;
		child->Release();
	}

	DestroyDragHandles();
	DestroySplitter();

	if (mhwndTabControl) {
		DestroyWindow(mhwndTabControl);
		mhwndTabControl = NULL;
	}
}

void ATContainerDockingPane::SetArea(ATContainerResizer& resizer, const vdrect32& area, bool parentContainsFullScreen) {
	bool fullScreenLayout = mbFullScreen || parentContainsFullScreen;

	if (mArea == area && mbFullScreenLayout == fullScreenLayout)
		return;

	if (mhwndTabControl) {
		if (fullScreenLayout)
			::ShowWindow(mhwndTabControl, SW_HIDE);
	}

	mArea = area;
	mbFullScreenLayout = fullScreenLayout;

	Relayout(resizer);
}

void ATContainerDockingPane::Clear() {
	AddRef();

	if (mhwndTabControl) {
		DestroyWindow(mhwndTabControl);
		mhwndTabControl = NULL;
	}

	mVisibleFrameIndex = -1;

	while(!mChildren.empty())
		mChildren.back()->Clear();

	while(!mContent.empty()) {
		ATFrameWindow *frame = mContent.back();

		// This should happen automatically if we had a window, but just in case, we flush it.
		mContent.pop_back();

		HWND hwnd = frame->GetHandleW32();

		if (hwnd)
			DestroyWindow(hwnd);

		frame->Release();
	}

	RemoveEmptyNode();

	Release();
}

void ATContainerDockingPane::InvalidateLayoutAll() {
	InvalidateLayout();

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it != itEnd; ++it) {
		ATContainerDockingPane *pane = *it;

		pane->InvalidateLayout();
	}
}

void ATContainerDockingPane::InvalidateLayout() {
	mbLayoutInvalid = true;

	for(ATContainerDockingPane *p = mpDockParent; p; p = p->mpDockParent) {
		if (p->mbDescendantLayoutInvalid)
			break;

		p->mbDescendantLayoutInvalid = true;
	}
}

void ATContainerDockingPane::UpdateLayout(ATContainerResizer& resizer) {
	if (mbLayoutInvalid) {
		Relayout(resizer);
	} else if (mbDescendantLayoutInvalid) {
		mbDescendantLayoutInvalid = false;

		for(ATContainerDockingPane *pane : mChildren)
			pane->UpdateLayout(resizer);
	}
}

void ATContainerDockingPane::Relayout(ATContainerResizer& resizer) {
	mbLayoutInvalid = false;
	mbDescendantLayoutInvalid = false;

	mCenterArea = mArea;

	if (mpSplitter) {
		HWND hwndSplitter = mpSplitter->GetHandleW32();

		::ShowWindow(hwndSplitter, mbFullScreenLayout ? SW_HIDE : SW_SHOWNOACTIVATE);

		switch(mDockCode) {
			case kATContainerDockLeft:
				resizer.LayoutWindow(hwndSplitter, mArea.right, mArea.top, mpParent->GetSplitterWidth(), mArea.height(), true);
				break;

			case kATContainerDockRight:
				resizer.LayoutWindow(hwndSplitter, mArea.left - mpParent->GetSplitterWidth(), mArea.top, mpParent->GetSplitterWidth(), mArea.height(), true);
				break;

			case kATContainerDockTop:
				resizer.LayoutWindow(hwndSplitter, mArea.left, mArea.bottom, mArea.width(), mpParent->GetSplitterHeight(), true);
				break;

			case kATContainerDockBottom:
				resizer.LayoutWindow(hwndSplitter, mArea.left, mArea.top - mpParent->GetSplitterHeight(), mArea.width(), mpParent->GetSplitterHeight(), true);
				break;
		}
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it != itEnd; ++it) {
		ATContainerDockingPane *pane = *it;

		vdrect32 rPane(mCenterArea);
		if (!mbFullScreenLayout) {
			int padX = 0;
			int padY = 0;

			if (!pane->mContent.empty()) {
				HWND hwndContent = pane->mContent.front()->GetHandleW32();

				if (hwndContent) {
					RECT rPad = {0,0,0,0};
					AdjustWindowRect(&rPad, GetWindowLong(hwndContent, GWL_STYLE), FALSE);

					padX += rPad.right - rPad.left;
					padY += rPad.bottom - rPad.top;
				}
			}

			int w = std::max<int>(VDRoundToInt(mArea.width() * pane->mDockFraction), padX);
			int h = std::max<int>(VDRoundToInt(mArea.height() * pane->mDockFraction), padY); 

			switch(pane->mDockCode) {
				case kATContainerDockLeft:
					rPane.right = rPane.left + w;
					mCenterArea.left = rPane.right + mpParent->GetSplitterWidth();
					break;

				case kATContainerDockRight:
					rPane.left = rPane.right - w;
					mCenterArea.right = rPane.left - mpParent->GetSplitterWidth();
					break;

				case kATContainerDockTop:
					rPane.bottom = rPane.top + h;
					mCenterArea.top = rPane.bottom + mpParent->GetSplitterHeight();
					break;

				case kATContainerDockBottom:
					rPane.top = rPane.bottom - h;
					mCenterArea.bottom = rPane.top - mpParent->GetSplitterHeight();
					break;

				case kATContainerDockCenter:
					VDASSERT(false);
					break;
			}
		}

		pane->SetArea(resizer, rPane, mbFullScreenLayout);

		if (pane->mbLayoutInvalid)
			pane->Relayout(resizer);
	}

	RepositionContent(resizer);
}

bool ATContainerDockingPane::GetFrameSizeForContent(vdsize32& sz) {
	double horizFraction = 1.0f;
	double vertFraction = 1.0f;
	int horizExtra = 0;
	int vertExtra = 0;

	if (mhwndTabControl) {
		RECT r = {0, 0, sz.w, sz.h};

		TabCtrl_AdjustRect(mhwndTabControl, TRUE, &r);

		sz.w = r.right - r.left;
		sz.h = r.bottom - r.top;
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it != itEnd; ++it) {
		ATContainerDockingPane *pane = *it;

		switch(pane->mDockCode) {
			case kATContainerDockLeft:
			case kATContainerDockRight:
				horizFraction -= pane->mDockFraction;
				horizExtra += mpParent->GetSplitterWidth() + 1;		// +1 for rounding bias
				break;

			case kATContainerDockTop:
			case kATContainerDockBottom:
				vertFraction -= pane->mDockFraction;
				vertExtra += mpParent->GetSplitterHeight() + 1;
				break;

			case kATContainerDockCenter:
				break;
		}
	}

	if (horizFraction < 1e-5f || vertFraction < 1e-5f)
		return false;

	sz.w = VDRoundToInt32((double)sz.w / horizFraction) + horizExtra;
	sz.h = VDRoundToInt32((double)sz.h / vertFraction) + vertExtra;
	return true;
}

void ATContainerDockingPane::SetSplitterEdges(uint8 edgeMask) {
	if (mSplitterEdges != edgeMask) {
		mSplitterEdges = edgeMask;

		UpdateChildEdgeFlags();
	}
}

int ATContainerDockingPane::GetDockCode() const {
	return mDockCode;
}

float ATContainerDockingPane::GetDockFraction() const {
	return mDockFraction;
}

void ATContainerDockingPane::SetDockFraction(float frac) {
	if (frac < 0.0f)
		frac = 0.0f;

	if (frac > 1.0f)
		frac = 1.0f;

	mDockFraction = frac;

	if (mpDockParent) {
		mpParent->SuspendLayout();
		mpDockParent->InvalidateLayout();
		mpParent->ResumeLayout();
	}
}

uint32 ATContainerDockingPane::GetContentCount() const {
	return (uint32)mContent.size();
}

ATFrameWindow *ATContainerDockingPane::GetContent(uint32 idx) const {
	return idx < mContent.size() ? mContent[idx] : NULL;
}

ATFrameWindow *ATContainerDockingPane::GetAnyContent(bool reqVisible, ATFrameWindow *exclude) const {
	if (!mContent.empty()) {
		for(FrameWindows::const_iterator it(mContent.begin()), itEnd(mContent.end());
			it != itEnd;
			++it)
		{
			ATFrameWindow *frame = *it;

			if (frame != exclude && (!reqVisible || frame->IsVisible()))
				return frame;
		}
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *child = *it;
		ATFrameWindow *content = child->GetAnyContent(reqVisible, exclude);

		if (content)
			return content;
	}

	return NULL;
}

uint32 ATContainerDockingPane::GetChildCount() const {
	return (uint32)mChildren.size();
}

ATContainerDockingPane *ATContainerDockingPane::GetChildPane(uint32 index) const {
	if (index >= mChildren.size())
		return NULL;

	return mChildren[index];
}

void ATContainerDockingPane::AddContent(ATFrameWindow *frame, bool deferResize) {
	VDASSERT(std::find(mContent.begin(), mContent.end(), frame) == mContent.end());

	if (mVisibleFrameIndex < 0)
		mVisibleFrameIndex = (int)mContent.size();

	mContent.push_back(frame);
	frame->AddRef();
	frame->SetPane(this);

	UpdateChildEdgeFlags();

	if (mpParent->IsLayoutSuspended())
		InvalidateLayout();
	else if (!deferResize) {
		ATContainerResizer resizer;
		RepositionContent(resizer);
		resizer.Flush();
	}
}

ATContainerDockingPane *ATContainerDockingPane::Dock(ATFrameWindow *frame, int code) {
	ATContainerDockingPane *newPane = this;

	if (code == kATContainerDockCenter) {
		// Check if we need to create a tab control -- this happens any time we start to
		// have more than one content frame.
		int n = (int)mContent.size();
		if (n >= 1) {
			if (n == 1) {
				VDASSERT(!mhwndTabControl);

				// Note that we create this as 1x1 instead of 0x0. 0x0 triggers a bug in comctl32 v6 where
				// if a tab is selected and the tab control is later resized, the tab control does an
				// InvalidateRect() with a null HWND.
				mhwndTabControl = CreateWindowExW(mpDockParent ? 0 : WS_EX_CLIENTEDGE, WC_TABCONTROLW, L"", WS_CHILD, 0, 0, 1, 1, mpParent->GetHandleW32(), (HMENU)0, VDGetLocalModuleHandleW32(), NULL);
				SetWindowPos(mhwndTabControl, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);

				if (mhwndTabControl) {
					SendMessage(mhwndTabControl, WM_SETFONT, (LPARAM)mpParent->GetLabelFont(), TRUE);

					// Create the tab for the existing item.
					ATFrameWindow *frame0 = mContent.front();
					TCITEMW tci = {};
					tci.mask = TCIF_TEXT | TCIF_PARAM;
					tci.pszText = (LPWSTR)frame0->GetTitle();
					tci.lParam = (LPARAM)frame0;

					SendMessageW(mhwndTabControl, TCM_INSERTITEMW, 0, (LPARAM)&tci);
				}
			}

			// add tab for new frame -- note that index must match new position in list
			if (mhwndTabControl) {
				TCITEMW tci = {};
				tci.mask = TCIF_TEXT | TCIF_PARAM;
				tci.pszText = (LPWSTR)frame->GetTitle();
				tci.lParam = (LPARAM)frame;

				int idx = (int)SendMessageW(mhwndTabControl, TCM_INSERTITEMW, (WPARAM)n, (LPARAM)&tci);

				if (idx >= 0)
					SendMessageW(mhwndTabControl, TCM_SETCURSEL, idx, 0);
			}

			// hide all existing frames
			for(int i=0; i<n; ++i)
				mContent[i]->SetVisible(false);

			mVisibleFrameIndex = n;
		}

		// Add the new frame.
		AddContent(frame, true);
	} else {
		vdrefptr<ATContainerDockingPane> pane(new ATContainerDockingPane(mpParent));

		switch(code) {
			case kATContainerDockLeft:
			case kATContainerDockRight:
				pane->mDockFraction = 1.0f;
				for(Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end()); it != itEnd; ++it) {
					ATContainerDockingPane *child = *it;

					if (child->mDockCode == kATContainerDockLeft || child->mDockCode == kATContainerDockRight)
						pane->mDockFraction -= child->mDockFraction;
				}

				if (pane->mDockFraction < 0.1f)
					pane->mDockFraction = 0.1f;
				else
					pane->mDockFraction *= 0.5f;
				break;

			case kATContainerDockTop:
			case kATContainerDockBottom:
				pane->mDockFraction = 1.0f;
				for(Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end()); it != itEnd; ++it) {
					ATContainerDockingPane *child = *it;

					if (child->mDockCode == kATContainerDockTop || child->mDockCode == kATContainerDockBottom)
						pane->mDockFraction -= child->mDockFraction;
				}

				if (pane->mDockFraction < 0.1f)
					pane->mDockFraction = 0.1f;
				else
					pane->mDockFraction *= 0.5f;
				break;
		}

		if (!mpDockParent)
			pane->mDockFraction *= 0.5f;

		pane->AddContent(frame, true);

		mChildren.push_back(pane);
		pane->AddRef();

		pane->mpDockParent = this;
		pane->mDockCode = code;
		pane->CreateSplitter();

		newPane = pane;

		UpdateChildEdgeFlags();
	}

	if (mpParent->IsLayoutSuspended())
		InvalidateLayout();
	else {
		ATContainerResizer resizer;
		Relayout(resizer);
		resizer.Flush();
	}

	return newPane;
}

bool ATContainerDockingPane::Undock(ATFrameWindow *frame) {
	// look up the frame in our content list
	FrameWindows::iterator itFrame = std::find(mContent.begin(), mContent.end(), frame);
	if (itFrame == mContent.end())
		return false;

	VDASSERT(!mpDockParent || !mpDockParent->mChildren.empty());

	// stash off the index of the tab (if we have tabs)
	int pos = (int)(itFrame - mContent.begin());

	// remove the frame
	frame->SetPane(NULL);
	mContent.erase(itFrame);

	if (mVisibleFrameIndex > pos)
		--mVisibleFrameIndex;

	if (mVisibleFrameIndex >= (int)mContent.size())
		mVisibleFrameIndex = mContent.empty() ? -1 : 0;

	// check if we have a tab control
	if (mhwndTabControl) {
		// kill the tab control entry if we only have one tab left
		if (mContent.size() <= 1) {
			DestroyWindow(mhwndTabControl);
			mhwndTabControl = NULL;

			// change the existing frame to non-tab mode
			if (!mContent.empty()) {
				if (mpDockParent) {
					ATFrameWindow *otherFrame = mContent.front();

					otherFrame->SetFrameMode(ATFrameWindow::kFrameModeFull);
					HWND hwndOther = otherFrame->GetHandleW32();
					if (hwndOther)
						SetWindowPos(hwndOther, NULL, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
				}

				// do a relayout
				if (mpParent->IsLayoutSuspended())
					InvalidateLayout();
				else {
					ATContainerResizer resizer;
					Relayout(resizer);
					resizer.Flush();
				}
			}
		} else {
			// otherwise, remove the tab
			SendMessageW(mhwndTabControl, TCM_DELETEITEM, (WPARAM)pos, 0);

			SendMessageW(mhwndTabControl, TCM_SETCURSEL, (WPARAM)mVisibleFrameIndex, 0);
			mContent[mVisibleFrameIndex]->SetVisible(true);
		}
	}

	// try to prune this node if we are empty
	if (mpDockParent && mContent.empty())
		RemoveEmptyNode();

	frame->Release();
	return true;
}

void ATContainerDockingPane::NotifyFontsUpdated() {
	for(ATFrameWindow *frame : mContent)
		frame->NotifyFontsUpdated();

	if (mhwndTabControl)
		SendMessage(mhwndTabControl, WM_SETFONT, (WPARAM)mpParent->GetLabelFont(), TRUE);

	InvalidateLayout();

	for(ATContainerDockingPane *child : mChildren)
		child->NotifyFontsUpdated();
}

void ATContainerDockingPane::NotifyDpiChanged(uint32 dpi) {
	for(ATFrameWindow *frame : mContent)
		frame->NotifyDpiChanged(dpi);

	for(ATContainerDockingPane *child : mChildren)
		child->NotifyDpiChanged(dpi);
}

void ATContainerDockingPane::RecalcFrame() {
	RecalcFrameInternal();

	InvalidateLayoutAll();

	ATContainerResizer resizer;
	UpdateLayout(resizer);
	resizer.Flush();
}

void ATContainerDockingPane::RecalcFrameInternal() {
	InvalidateLayout();

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *child = *it;

		child->RecalcFrameInternal();
	}

	for(FrameWindows::const_iterator itFrame(mContent.begin()), itFrameEnd(mContent.end());
		itFrame != itFrameEnd;
		++itFrame)
	{
		ATFrameWindow *frame = *itFrame;

		frame->RecalcFrame();
	}
}

ATFrameWindow *ATContainerDockingPane::GetVisibleFrame() const {
	return mVisibleFrameIndex >= 0 ? mContent[mVisibleFrameIndex] : NULL;
}

void ATContainerDockingPane::SetVisibleFrame(ATFrameWindow *frame) {
	FrameWindows::const_iterator it(std::find(mContent.begin(), mContent.end(), frame));

	if (it == mContent.end()) {
		VDASSERT(false);
		return;
	}

	sint32 idx = (sint32)(it - mContent.begin());

	if (mVisibleFrameIndex != idx) {
		mContent[mVisibleFrameIndex]->SetVisible(false);
		mContent[idx]->SetVisible(true);
		mVisibleFrameIndex = idx;

		if (mhwndTabControl)
			SendMessageW(mhwndTabControl, TCM_SETCURSEL, (WPARAM)idx, 0);
	}
}

void ATContainerDockingPane::UpdateModalState(ATFrameWindow *modalFrame) {
	for(FrameWindows::const_iterator itFrame(mContent.begin()), itFrameEnd(mContent.end());
		itFrame != itFrameEnd;
		++itFrame)
	{
		ATFrameWindow *frame = *itFrame;

		HWND hwndContent = frame->GetHandleW32();

		if (hwndContent)
			EnableWindow(hwndContent, !modalFrame || (modalFrame == frame));
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *pane = *it;

		pane->UpdateModalState(modalFrame);
	}
}

void ATContainerDockingPane::UpdateActivationState(ATFrameWindow *activeFrame) {
	if (!mContent.empty()) {
		const size_t n = mContent.size();
		int activeIndex = -1;

		for(size_t i=0; i<n; ++i) {
			ATFrameWindow *frame = mContent[i];
			const bool isActive = (frame == activeFrame);

			HWND hwndContent = frame->GetHandleW32();
			if (hwndContent)
				SendMessage(hwndContent, WM_NCACTIVATE, isActive, 0);

			if (isActive)
				activeIndex = (int)i;
		}

		// If we contain the active pane and have a tab control, update the active tab.
		if (activeIndex >= 0 && activeIndex != mVisibleFrameIndex) {
			if (mVisibleFrameIndex >= 0)
				mContent[mVisibleFrameIndex]->SetVisible(false);

			mContent[activeIndex]->SetVisible(true);

			mVisibleFrameIndex = activeIndex;

			if (mhwndTabControl)
				SendMessageW(mhwndTabControl, TCM_SETCURSEL, (WPARAM)activeIndex, 0);
		}
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *pane = *it;

		pane->UpdateActivationState(activeFrame);
	}
}

void ATContainerDockingPane::CreateDragHandles() {
	if (!mpDragHandle) {
		mpDragHandle = new ATDragHandleWindow;
		POINT pt = { (mCenterArea.left + mCenterArea.right - 75)/2, (mCenterArea.top + mCenterArea.bottom - 75)/2 };

		HWND hwndParent = mpParent->GetHandleW32();
		ClientToScreen(hwndParent, &pt);

		mpDragHandle->Create(pt.x, pt.y, 75, 75, NULL, 0);

		Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
		for(; it!=itEnd; ++it) {
			ATContainerDockingPane *pane = *it;
			pane->CreateDragHandles();
		}
	}
}

void ATContainerDockingPane::DestroyDragHandles() {
	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *pane = *it;
		pane->DestroyDragHandles();
	}

	if (mpDragHandle) {
		mpDragHandle->Destroy();
		mpDragHandle = NULL;
	}
}

void ATContainerDockingPane::CreateSplitter() {
	if (mpSplitter)
		return;

	if (mDockCode == kATContainerDockCenter)
		return;

	mpSplitter = new ATContainerSplitterBar;
	if (!mpSplitter->Init(mpParent->GetHandleW32(), this, mDockCode == kATContainerDockLeft || mDockCode == kATContainerDockRight)) {
		mpSplitter = NULL;
		return;
	}

	if (mpParent->IsLayoutSuspended()) {
		InvalidateLayout();
		return;
	}

	HWND hwndSplitter = mpSplitter->GetHandleW32();

	vdrect32 rSplit(mArea);

	switch(mDockCode) {
		case kATContainerDockLeft:
			rSplit.left = rSplit.right;
			rSplit.right += mpParent->GetSplitterWidth();
			break;
		case kATContainerDockRight:
			rSplit.right = rSplit.left;
			rSplit.left -= mpParent->GetSplitterWidth();
			break;
		case kATContainerDockTop:
			rSplit.top = rSplit.bottom;
			rSplit.bottom += mpParent->GetSplitterHeight();
			break;
		case kATContainerDockBottom:
			rSplit.bottom = rSplit.top;
			rSplit.top -= mpParent->GetSplitterHeight();
			break;
	}

	SetWindowPos(hwndSplitter, NULL, rSplit.left, rSplit.top, rSplit.width(), rSplit.height(), SWP_NOZORDER|SWP_NOACTIVATE);
}

void ATContainerDockingPane::DestroySplitter() {
	if (!mpSplitter)
		return;

	mpSplitter->Shutdown();
	mpSplitter = NULL;
}

bool ATContainerDockingPane::HitTestDragHandles(int screenX, int screenY, int& code, ATContainerDockingPane **ppPane) {
	if (mpDragHandle) {
		int localCode = mpDragHandle->HitTest(screenX, screenY);

		if (localCode >= 0) {
			code = localCode;
			*ppPane = this;
			AddRef();
			return true;
		}
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *pane = *it;
		if (pane && pane->HitTestDragHandles(screenX, screenY, code, ppPane))
			return true;
	}

	return false;
}

void ATContainerDockingPane::ActivateNearbySplitter(int screenX, int screenY, uint8 edgeFlags) {
	ATContainerDockingPane *parent = this;
	ATContainerDockingPane *child = nullptr;

	// Ascend the hierarchy, checking parents and older siblings. We are looking for splitters
	// facing the target, so this time we are looking for a splitter on the same side, i.e.
	// top docked pane matching top request.
	do {
		// find child in parent
		const auto chBegin = parent->mChildren.begin();
		const auto chEnd = parent->mChildren.end();
		auto it = chEnd;
		
		if (child) {
			it = std::find(chBegin, chEnd, child);

			if (it == chEnd) {
				VDASSERT(!"Docking child not found in parent.");
			}
		} else {
			if (it != chBegin)
				--it;
		}

		if (it != chEnd) {
			for(;;) {
				ATContainerDockingPane *sibling = *it;

				if (sibling->mpSplitter) {
					bool activate = false;

					if (sibling == child) {
						// For the local docking pane, check if it is docked on a side. If so, it will have a
						// splitter on the opposite side, i.e. a left docked pane will have a splitter on the
						// right that can satisfy a right request.
						switch(sibling->mDockCode) {
							case kATContainerDockLeft:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Right)
									activate = true;
								break;

							case kATContainerDockTop:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Bottom)
									activate = true;
								break;

							case kATContainerDockRight:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Left)
									activate = true;
								break;

							case kATContainerDockBottom:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Top)
									activate = true;
								break;
						}
					} else {
						switch(sibling->mDockCode) {
							case kATContainerDockLeft:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Left)
									activate = true;
								break;

							case kATContainerDockTop:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Top)
									activate = true;
								break;

							case kATContainerDockRight:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Right)
									activate = true;
								break;

							case kATContainerDockBottom:
								if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Bottom)
									activate = true;
								break;
						}
					}

					if (activate) {
						sibling->mpSplitter->BeginDrag(screenX, screenY);
						return;
					}
				}

				if (it == chBegin)
					break;

				--it;
			}
		}

		child = parent;
		parent = parent->mpDockParent;
	} while(parent);
}

void ATContainerDockingPane::UpdateFullScreenState() {
	mbFullScreen = false;

	for(FrameWindows::const_iterator it(mContent.begin()), itEnd(mContent.end());
		it != itEnd;
		++it)
	{
		if ((*it)->IsFullScreen())
			mbFullScreen = true;
	}

	Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end());
	for(; it!=itEnd; ++it) {
		ATContainerDockingPane *pane = *it;
		if (pane && pane->IsFullScreen()) {
			mbFullScreen = true;
			break;
		}
	}

	if (mpDockParent)
		mpDockParent->UpdateFullScreenState();
}

bool ATContainerDockingPane::IsFullScreen() const {
	return mbFullScreen;
}

void ATContainerDockingPane::RepositionContent(ATContainerResizer& resizer) {
	if (mContent.empty())
		return;

	int n = (int)mContent.size();

	RECT r = { mCenterArea.left, mCenterArea.top, mCenterArea.right, mCenterArea.bottom };

	if (mhwndTabControl) {
		if (r.right <= r.left || r.bottom <= r.top) {
			ShowWindow(mhwndTabControl, SW_HIDE);
			memset(&r, 0, sizeof r);
		} else {
			resizer.LayoutWindow(mhwndTabControl, r.left, r.top, r.right - r.left, r.bottom - r.top, true);

			SendMessageW(mhwndTabControl, TCM_ADJUSTRECT, FALSE, (LPARAM)&r);
		}
	}

	const int w = std::max<int>(r.right - r.left, 0);
	const int h = std::max<int>(r.bottom - r.top, 0);
	for(int i=0; i<n; ++i) {
		ATFrameWindow *frame = mContent[i];

		if (mbFullScreenLayout && !frame->IsFullScreen())
			frame->SetVisible(false);
		else {
			HWND hwndContent = frame->GetHandleW32();

			if (hwndContent)
				resizer.LayoutWindow(hwndContent, r.left, r.top, w, h, i == mVisibleFrameIndex);

			frame->Relayout(w, h);
		}
	}
}

void ATContainerDockingPane::RemoveAnyEmptyNodes() {
	AddRef();

	RemoveEmptyNode();

	if ((mpDockParent || mbPinned) && !mChildren.empty()) {
		vdfastvector<ATContainerDockingPane *> children;

		for(Children::const_iterator it(mChildren.begin()), itEnd(mChildren.end()); it != itEnd; ++it) {
			ATContainerDockingPane *child = *it;

			child->AddRef();
			children.push_back(child);
		}

		while(!children.empty()) {
			ATContainerDockingPane *child = children.back();
			children.pop_back();

			child->RemoveEmptyNode();
			child->Release();
		}
	}

	Release();
}

void ATContainerDockingPane::RemoveEmptyNode() {
	ATContainerDockingPane *parent = mpDockParent;
	if (!parent || !mContent.empty() || mbPinned)
		return;

	VDASSERT(!parent->mChildren.empty());

	VDASSERT(!mhwndTabControl);

	// Check if we have any children. If we do, promote the innermost one to center.
	if (!mChildren.empty()) {
		ATContainerDockingPane *child = mChildren.back();

		// Steal the content and the tab control.
		mContent.swap(child->mContent);
		mhwndTabControl = child->mhwndTabControl;
		child->mhwndTabControl = NULL;

		for(FrameWindows::const_iterator it(mContent.begin()), itEnd(mContent.end());
			it != itEnd;
			++it)
		{
			ATFrameWindow *frame = *it;

			frame->SetPane(this);
		}

		if (mpParent->IsLayoutSuspended())
			InvalidateLayout();
		else {
			ATContainerResizer resizer;
			Relayout(resizer);
			resizer.Flush();
		}

		child->RemoveEmptyNode();
		return;
	}

	// We don't have any children. Well, time to prune this node then.
	DestroySplitter();

	Children::iterator itDel(std::find(parent->mChildren.begin(), parent->mChildren.end(), this));
	VDASSERT(itDel != parent->mChildren.end());
	parent->mChildren.erase(itDel);

	mpDockParent = NULL;
	mDockCode = -1;
	mDockFraction = 0;
	Release();

	// NOTE: We're dead at this point!

	if (parent->mpParent->IsLayoutSuspended())
		parent->InvalidateLayout();
	else {
		ATContainerResizer resizer;
		parent->Relayout(resizer);
		resizer.Flush();
	}

	if (parent->mChildren.empty())
		parent->RemoveEmptyNode();
}

void ATContainerDockingPane::UpdateChildEdgeFlags() {
	uint8 contentEdgeFlags = mSplitterEdges;

	for(ATContainerDockingPane *child : mChildren) {
		switch(child->GetDockCode()) {
			case kATContainerDockLeft:
				child->SetSplitterEdges(contentEdgeFlags | (uint8)ATUIContainerEdgeFlags::Right);
				contentEdgeFlags |= (uint8)ATUIContainerEdgeFlags::Left;
				break;

			case kATContainerDockTop:
				child->SetSplitterEdges(contentEdgeFlags | (uint8)ATUIContainerEdgeFlags::Bottom);
				contentEdgeFlags |= (uint8)ATUIContainerEdgeFlags::Top;
				break;

			case kATContainerDockRight:
				child->SetSplitterEdges(contentEdgeFlags | (uint8)ATUIContainerEdgeFlags::Left);
				contentEdgeFlags |= (uint8)ATUIContainerEdgeFlags::Right;
				break;

			case kATContainerDockBottom:
				child->SetSplitterEdges(contentEdgeFlags | (uint8)ATUIContainerEdgeFlags::Top);
				contentEdgeFlags |= (uint8)ATUIContainerEdgeFlags::Bottom;
				break;
		}
	}

	if (mhwndTabControl || mbFullScreen)
		contentEdgeFlags = 0;

	for(ATFrameWindow *content : mContent)
		content->SetSplitterEdges(contentEdgeFlags);
}

void ATContainerDockingPane::OnTabChange(HWND hwndSender) {
	if (hwndSender != mhwndTabControl) {
		for(ATContainerDockingPane *child : mChildren)
			child->OnTabChange(hwndSender);
	} else {
		int idx = (int)SendMessageW(hwndSender, TCM_GETCURSEL, 0, 0);

		if (idx < 0 || (size_t)idx >= mContent.size())
			return;

		ATFrameWindow *frame = mContent[idx];

		mpParent->ActivateFrame(frame);
		HWND hwnd = frame->GetHandleW32();

		if (hwnd)
			::SetFocus(hwnd);
	}
}

///////////////////////////////////////////////////////////////////////////////

ATContainerWindow::ATContainerWindow() {
	mpDockingPane = new ATContainerDockingPane(this);
	mpDockingPane->AddRef();
	mpDockingPane->SetPinned(true);
}

ATContainerWindow::~ATContainerWindow() {
	VDASSERT(mUndockedFrames.empty());

	if (mpDragPaneTarget) {
		mpDragPaneTarget->Release();
		mpDragPaneTarget = NULL;
	}
	if (mpDockingPane) {
		mpDockingPane->Release();
		mpDockingPane = NULL;
	}
}

void *ATContainerWindow::AsInterface(uint32 id) {
	if (id == ATContainerWindow::kTypeID)
		return static_cast<ATContainerWindow *>(this);

	return ATUINativeWindow::AsInterface(id);
}

VDGUIHandle ATContainerWindow::Create(int x, int y, int cx, int cy, VDGUIHandle parent, bool visible) {
	return Create(sWndClass, x, y, cx, cy, parent, visible);
}

VDGUIHandle ATContainerWindow::Create(ATOM wc, int x, int y, int cx, int cy, VDGUIHandle parent, bool visible) {
	return (VDGUIHandle)CreateWindowEx(0, (LPCTSTR)(uintptr_t)wc, _T(""), WS_OVERLAPPEDWINDOW|WS_CLIPCHILDREN|(visible ? WS_VISIBLE : 0), x, y, cx, cy, (HWND)parent, NULL, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));
}

void ATContainerWindow::Destroy() {
	if (mhwnd) {
		DestroyWindow(mhwnd);
		mhwnd = NULL;
	}
}

void ATContainerWindow::Clear() {
	if (mpDockingPane)
		mpDockingPane->Clear();
}

void ATContainerWindow::AutoSize() {
	if (!mpDockingPane || !mhwnd || mpFullScreenFrame)
		return;

	WINDOWPLACEMENT wp = {sizeof(WINDOWPLACEMENT)};
	if (!GetWindowPlacement(mhwnd, &wp))
		return;

	if (wp.showCmd != SW_SHOWNORMAL)
		return;

	ATContainerDockingPane *centerPane = mpDockingPane;
	if (!centerPane)
		return;

	uint32 n = centerPane->GetContentCount();
	bool gotSize = false;
	vdsize32 sz;

	for(uint32 i=0; i<n; ++i) {
		ATFrameWindow *frame = centerPane->GetContent(i);
		if (frame && frame->GetIdealSize(sz)) {
			gotSize = true;
			break;
		}
	}

	if (!gotSize)
		return;

	if (!mpDockingPane->GetFrameSizeForContent(sz))
		return;

	RECT r = {0, 0, sz.w, sz.h};
	if (!AdjustWindowRect(&r, GetWindowLong(mhwnd, GWL_STYLE), GetMenu(mhwnd) != NULL))
		return;

	const int desiredWidth = (r.right - r.left);
	const int desiredHeight = (r.bottom - r.top);
	wp.rcNormalPosition.right = wp.rcNormalPosition.left + desiredWidth;
	wp.rcNormalPosition.bottom = wp.rcNormalPosition.top + desiredHeight;

	SetWindowPlacement(mhwnd, &wp);

	// Check for the case where the menu has forced the client rectangle to shrink
	// vertically due to wrapping -- in that case, measure the delta and attempt to
	// apply a one time correction.
	RECT r2;
	if (GetWindowRect(mhwnd, &r2) && r2.right - r2.left == desiredWidth && r2.bottom - r2.top == desiredHeight) {
		RECT rc;

		if (GetClientRect(mhwnd, &rc) && rc.right == sz.w && rc.bottom < sz.h) {
			wp.rcNormalPosition.bottom += (sz.h - rc.bottom);

			SetWindowPlacement(mhwnd, &wp);
		}
	}
}

void ATContainerWindow::Relayout() {
	OnSize();
}

void ATContainerWindow::UndockCurrentFrame() {
	if (mpModalFrame || mpFullScreenFrame || !mpActiveFrame)
		return;

	UndockFrame(mpActiveFrame);
}

void ATContainerWindow::CloseCurrentFrame() {
	if (mpModalFrame || mpFullScreenFrame || !mpActiveFrame)
		return;

	CloseFrame(mpActiveFrame);
}

ATContainerWindow *ATContainerWindow::GetContainerWindow(HWND hwnd) {
	if (hwnd) {
		ATOM a = (ATOM)GetClassLong(hwnd, GCW_ATOM);

		if (a == sWndClass || a == sWndClassMain) {
			ATUINativeWindow *w = (ATUINativeWindow *)GetWindowLongPtr(hwnd, 0);
			return vdpoly_cast<ATContainerWindow *>(w);
		}
	}

	return NULL;
}

uint32 ATContainerWindow::GetUndockedPaneCount() const {
	return (uint32)mUndockedFrames.size();
}

ATFrameWindow *ATContainerWindow::GetUndockedPane(uint32 index) const {
	if (index >= mUndockedFrames.size())
		return NULL;

	return mUndockedFrames[index];
}

void ATContainerWindow::SuspendLayout() {
	++mLayoutSuspendCount;
}

void ATContainerWindow::ResumeLayout() {
	VDASSERT(mLayoutSuspendCount);

	if (!--mLayoutSuspendCount) {
		if (mpDockingPane) {
			ATContainerResizer resizer;
			mpDockingPane->UpdateLayout(resizer);
			resizer.Flush();
		}
	}
}

void ATContainerWindow::NotifyFontsUpdated() {
	SuspendLayout();

	if (mpDockingPane)
		mpDockingPane->NotifyFontsUpdated();

	ResumeLayout();
}

bool ATContainerWindow::InitDragHandles() {
	mpDockingPane->CreateDragHandles();
	return true;
}

void ATContainerWindow::ShutdownDragHandles() {
	mpDockingPane->DestroyDragHandles();
}

void ATContainerWindow::UpdateDragHandles(int screenX, int screenY) {
	if (mpDragPaneTarget) {
		mpDragPaneTarget->Release();
		mpDragPaneTarget = NULL;
		mDragPaneTargetCode = -1;
	}

	mpDockingPane->HitTestDragHandles(screenX, screenY, mDragPaneTargetCode, &mpDragPaneTarget);
}

ATContainerDockingPane *ATContainerWindow::DockFrame(ATFrameWindow *frame, int code) {
	return DockFrame(frame, mpDockingPane, code);
}

ATContainerDockingPane *ATContainerWindow::DockFrame(ATFrameWindow *frame, ATContainerDockingPane *parent, int code) {
	parent->AddRef();
	if (mpDragPaneTarget)
		mpDragPaneTarget->Release();
	mpDragPaneTarget = parent;
	mDragPaneTargetCode = code;

	return DockFrame(frame);
}

ATContainerDockingPane *ATContainerWindow::DockFrame(ATFrameWindow *frame) {
	if (!mpDragPaneTarget)
		return NULL;

	HWND hwndActive = NULL;

	if (frame) {
		UndockedFrames::iterator it = std::find(mUndockedFrames.begin(), mUndockedFrames.end(), frame);
		if (it != mUndockedFrames.end()) {
			mUndockedFrames.erase(it);
			frame->Release();
		}

		HWND hwndFrame = frame->GetHandleW32();

		if (hwndFrame) {
			hwndActive = ::GetFocus();
			if (hwndActive && ::GetAncestor(hwndActive, GA_ROOT) != hwndFrame)
				hwndActive = NULL;

			if (::GetForegroundWindow() == hwndFrame) {
				::SetForegroundWindow(mhwnd);
				mpActiveFrame = frame;
			}

			if (!mpDragPaneTarget->GetParentPane() && mDragPaneTargetCode == kATContainerDockCenter)
				frame->SetFrameMode(ATFrameWindow::kFrameModeEdge);
			else
				frame->SetFrameMode(ATFrameWindow::kFrameModeFull);

			UINT style = GetWindowLong(hwndFrame, GWL_STYLE);
			style |= WS_CHILD | WS_SYSMENU;
			style &= ~(WS_POPUP | WS_THICKFRAME);		// must remove WS_SYSMENU for top level menus to work
			SetWindowLong(hwndFrame, GWL_STYLE, style);

			// Prevent WM_CHILDACTIVATE from changing the active window.
			mbBlockActiveUpdates = true;
			SetParent(hwndFrame, mhwnd);
			mbBlockActiveUpdates = false;

			SetWindowPos(hwndFrame, NULL, 0, 0, 0, 0, SWP_NOMOVE|SWP_NOSIZE|SWP_FRAMECHANGED|SWP_NOACTIVATE);

			UINT exstyle = GetWindowLong(hwndFrame, GWL_EXSTYLE);
			exstyle |= WS_EX_TOOLWINDOW;
			exstyle &= ~WS_EX_WINDOWEDGE;
			SetWindowLong(hwndFrame, GWL_EXSTYLE, exstyle);

			SendMessage(mhwnd, WM_CHANGEUISTATE, MAKELONG(UIS_INITIALIZE, UISF_HIDEACCEL|UISF_HIDEFOCUS), 0);

			if (hwndActive)
				::SetFocus(hwndActive);
		}
	}

	if (frame)
		frame->RecalcFrame();

	ATContainerDockingPane *newPane = mpDragPaneTarget->Dock(frame, mDragPaneTargetCode);

	if (frame && hwndActive)
		NotifyFrameActivated(mpActiveFrame);

	return newPane;
}

void ATContainerWindow::AddUndockedFrame(ATFrameWindow *frame) {
	VDASSERT(std::find(mUndockedFrames.begin(), mUndockedFrames.end(), frame) == mUndockedFrames.end());

	mUndockedFrames.push_back(frame);
	frame->AddRef();
}

void ATContainerWindow::UndockFrame(ATFrameWindow *frame, bool visible, bool destroy) {
	HWND hwndFrame = frame->GetHandleW32();
	VDASSERT(hwndFrame);

	UINT style = GetWindowLong(hwndFrame, GWL_STYLE);

	if (mpActiveFrame == frame) {
		mpActiveFrame = ChooseNewActiveFrame(frame);

		if (!visible)
			::SetFocus(mhwnd);
	}

	if (mpFullScreenFrame == frame) {
		mpFullScreenFrame = NULL;
		frame->SetFullScreen(false);
	}

	if (style & WS_CHILD) {
		ShowWindow(hwndFrame, SW_HIDE);

		ATContainerDockingPane *framePane = frame->GetPane();
		if (framePane)
			framePane->Undock(frame);

		if (!destroy) {
			frame->SetFrameMode(ATFrameWindow::kFrameModeUndocked);

			RECT r;
			GetWindowRect(hwndFrame, &r);

			HWND hwndOwner = GetWindow(mhwnd, GW_OWNER);
			SetParent(hwndFrame, hwndOwner);

			style &= ~WS_CHILD;
			style |= WS_OVERLAPPEDWINDOW;
			SetWindowLong(hwndFrame, GWL_STYLE, style);

			UINT exstyle = GetWindowLong(hwndFrame, GWL_EXSTYLE);
			exstyle |= WS_EX_TOOLWINDOW;
			SetWindowLong(hwndFrame, GWL_EXSTYLE, exstyle);

			SetWindowPos(hwndFrame, NULL, r.left, r.top, 0, 0, SWP_NOSIZE|SWP_FRAMECHANGED|SWP_NOACTIVATE|SWP_NOZORDER|SWP_NOCOPYBITS|SWP_NOOWNERZORDER|SWP_NOREDRAW);
			RedrawWindow(hwndFrame, NULL, NULL, RDW_FRAME | RDW_INVALIDATE);
			SendMessage(hwndFrame, WM_CHANGEUISTATE, MAKELONG(UIS_INITIALIZE, UISF_HIDEACCEL|UISF_HIDEFOCUS), 0);

			if (visible)
				ShowWindow(hwndFrame, SW_SHOWNA);

			VDASSERT(std::find(mUndockedFrames.begin(), mUndockedFrames.end(), frame) == mUndockedFrames.end());
			mUndockedFrames.push_back(frame);
			frame->AddRef();

			if (visible)
				::SetActiveWindow(hwndFrame);
		}
	}
}

void ATContainerWindow::CloseFrame(ATFrameWindow *frame) {
	VDASSERT(!mpFullScreenFrame);
	VDASSERT(!mpModalFrame);

	HWND hwnd = frame->GetHandleW32();

	SendMessageW(hwnd, WM_CLOSE, 0, 0);
}

void ATContainerWindow::SetFullScreenFrame(ATFrameWindow *frame) {
	if (mpFullScreenFrame == frame)
		return;

	if (mpFullScreenFrame)
		mpFullScreenFrame->SetFullScreen(false);

	mpFullScreenFrame = frame;

	if (frame) {
		frame->SetFullScreen(true);

		ActivateFrame(frame);

		HWND hwndFocus = frame->GetHandleW32();
		if (hwndFocus)
			::SetFocus(hwndFocus);
	}

	ATContainerResizer resizer;
	mpDockingPane->Relayout(resizer);
	resizer.Flush();
}

void ATContainerWindow::SetModalFrame(ATFrameWindow *frame) {
	if (mpModalFrame == frame)
		return;

	mpModalFrame = frame;

	if (frame)
		ActivateFrame(frame);

	if (mpDockingPane)
		mpDockingPane->UpdateModalState(frame);
}

void ATContainerWindow::ActivateFrame(ATFrameWindow *frame) {
	if (mpActiveFrame == frame)
		return;

	NotifyFrameActivated(frame);
}

void ATContainerWindow::RemoveAnyEmptyNodes() {
	if (mpDockingPane)
		mpDockingPane->RemoveAnyEmptyNodes();
}

void ATContainerWindow::NotifyFrameActivated(ATFrameWindow *frame) {
	if (mbBlockActiveUpdates)
		return;

	HWND hwndFrame = NULL;
	
	if (frame)
		hwndFrame = frame->GetHandleW32();

	VDASSERT(!hwndFrame || !(GetWindowLong(hwndFrame, GWL_STYLE) & WS_CHILD) || GetAncestor(hwndFrame, GA_ROOT) == mhwnd);
	mpActiveFrame = frame;

	if (mpDockingPane)
		mpDockingPane->UpdateActivationState(frame);
}

void ATContainerWindow::NotifyUndockedFrameDestroyed(ATFrameWindow *frame) {
	UndockedFrames::iterator it = std::find(mUndockedFrames.begin(), mUndockedFrames.end(), frame);
	if (it != mUndockedFrames.end()) {
		mUndockedFrames.erase(it);
		frame->Release();
	}
}

LRESULT ATContainerWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			if (!OnCreate())
				return -1;
			break;

		case WM_DESTROY:
			OnDestroy();
			break;

		case WM_SIZE:
			OnSize();
			break;

		case WM_PARENTNOTIFY:
			if (LOWORD(wParam) == WM_CREATE)
				OnSize();
			break;

		case WM_NCACTIVATE:
			if (wParam != 0)
				mpDockingPane->UpdateActivationState(mpActiveFrame);
			else
				mpDockingPane->UpdateActivationState(NULL);
			break;

		case WM_SETFOCUS:
			OnSetFocus((HWND)wParam);
			break;

		case WM_KILLFOCUS:
			OnKillFocus((HWND)wParam);
			break;

		case WM_ACTIVATE:
			if (OnActivate(LOWORD(wParam), HIWORD(wParam) != 0, (HWND)lParam))
				return 0;
			break;

		case WM_SYSCOLORCHANGE:
		case WM_THEMECHANGED:
			if (mpDockingPane)
				mpDockingPane->RecalcFrame();

			InvalidateRect(mhwnd, NULL, TRUE);
			break;

		case WM_NOTIFY:
			{
				const NMHDR& hdr = *(const NMHDR *)lParam;

				if (hdr.code == TCN_SELCHANGE) {
					if (mpDockingPane)
						mpDockingPane->OnTabChange(hdr.hwndFrom);
				}
			}
			break;

		case WM_DPICHANGED:
			{
				const RECT& r = *(const RECT *)lParam;

				SetWindowPos(mhwnd, NULL, r.left, r.top, r.right - r.left, r.bottom - r.top, SWP_NOZORDER | SWP_NOACTIVATE);
				RedrawWindow(mhwnd, NULL, NULL, RDW_INVALIDATE);

				UpdateMonitorDpi();
				RecreateSystemObjects();

				if (mpDockingPane)
					mpDockingPane->RecalcFrame();
			}
			return 0;

		case WM_ENTERSIZEMOVE:
			mbActivelyMovingSizing = true;
		case WM_ENTERMENULOOP:
			for(HWND hwndTarget = GetFocus(); hwndTarget; hwndTarget = GetAncestor(hwndTarget, GA_PARENT)) {
				SendMessage(hwndTarget, ATWM_FORCEKEYSUP, 0, 0);
			}
			break;

		case WM_EXITSIZEMOVE:
			mbActivelyMovingSizing = false;

			while(!mTrackingNotifyFrames.empty()) {
				ATFrameWindow *w = mTrackingNotifyFrames.back();
				mTrackingNotifyFrames.pop_back();

				w->NotifyEndTracking();
			}
			break;

		case WM_ERASEBKGND:
			{
				RECT r;
				if (GetClientRect(mhwnd, &r)) {
					FillRect((HDC)wParam, &r, (HBRUSH)(COLOR_3DDKSHADOW + 1));
					return TRUE;
				}
			}
			break;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

bool ATContainerWindow::OnCreate() {
	UpdateMonitorDpi();
	RecreateSystemObjects();
	OnSize();
	return true;
}

void ATContainerWindow::OnDestroy() {
	mpActiveFrame = NULL;
	Clear();
	DestroySystemObjects();
}

void ATContainerWindow::OnSize() {
	RECT r;
	GetClientRect(mhwnd, &r);

	ATContainerResizer resizer;
	mpDockingPane->SetArea(resizer, vdrect32(0, 0, r.right, r.bottom), false);
	resizer.Flush();
}

void ATContainerWindow::NotifyDockedFrameDestroyed(ATFrameWindow *frame) {
	SuspendLayout();

	auto it = std::lower_bound(mTrackingNotifyFrames.begin(), mTrackingNotifyFrames.end(), frame);
	if (it != mTrackingNotifyFrames.end() && *it == frame)
		mTrackingNotifyFrames.erase(it);

	if (mpActiveFrame == frame) {
		mpActiveFrame = NULL;

		auto *frameToActivate = ChooseNewActiveFrame(frame);

		if (frameToActivate) {
			HWND hwndNewFocus = frameToActivate->GetHandleW32();

			::SetFocus(hwndNewFocus);
			NotifyFrameActivated(frameToActivate);
		} else {
			::SetFocus(mhwnd);
		}
	}

	ShowWindow(frame->GetHandleW32(), SW_HIDE);
	UndockFrame(frame, false, true);

	ResumeLayout();
}

void ATContainerWindow::OnSetFocus(HWND hwndOldFocus) {
	if (mpActiveFrame) {
		VDASSERT(mpActiveFrame->GetContainer() == this);

		NotifyFrameActivated(mpActiveFrame);

		HWND hwndActiveFrame = mpActiveFrame->GetHandleW32();
		SetFocus(hwndActiveFrame);
	}
}

void ATContainerWindow::AddTrackingNotification(ATFrameWindow *w) {
	VDASSERT(w->IsDocked());

	auto it = std::lower_bound(mTrackingNotifyFrames.begin(), mTrackingNotifyFrames.end(), w);
	if (it == mTrackingNotifyFrames.end() || *it != w)
		mTrackingNotifyFrames.insert(it, w);
}

void ATContainerWindow::OnKillFocus(HWND hwndNewFocus) {
}

bool ATContainerWindow::OnActivate(UINT code, bool minimized, HWND hwnd) {
	if (code != WA_INACTIVE && !minimized) {
		if (mpActiveFrame) {
			VDASSERT(mpActiveFrame->GetContainer() == this);

			NotifyFrameActivated(mpActiveFrame);

			HWND hwndActiveFrame = mpActiveFrame->GetHandleW32();
			if (hwndActiveFrame)
				SetFocus(hwndActiveFrame);
		}
	}

	return true;
}

void ATContainerWindow::RecreateSystemObjects() {
	HFONT hfontLabelOld = mhfontLabel;
	HFONT hfontCaptionOld = mhfontCaption;
	HFONT hfontCaptionSymbolOld = mhfontCaptionSymbol;

	mhfontLabel = NULL;
	mhfontCaption = NULL;
	mhfontCaptionSymbol = NULL;

	int scaleFactor = 100;
	int globalDpi = 96;

	if (HDC hdc = GetDC(mhwnd)) {
		globalDpi = GetDeviceCaps(hdc, LOGPIXELSY);
		ReleaseDC(mhwnd, hdc);
	}

	int monitorDpi = globalDpi;
	if (mMonitorDpi) {
		monitorDpi = mMonitorDpi;
		scaleFactor = MulDiv(100, mMonitorDpi, globalDpi);
	}

	mSplitterWidth = (GetSystemMetrics(SM_CXEDGE) * scaleFactor + 99) / 100;
	mSplitterHeight = (GetSystemMetrics(SM_CYEDGE) * scaleFactor + 99) / 100;

	NONCLIENTMETRICS ncm = {
#if WINVER >= 0x0600
		offsetof(NONCLIENTMETRICS, iPaddedBorderWidth)
#else
		sizeof(NONCLIENTMETRICS)
#endif
	};

	SystemParametersInfo(SPI_GETNONCLIENTMETRICS, ncm.cbSize, &ncm, FALSE);

	ncm.lfSmCaptionFont.lfHeight = MulDiv(ncm.lfSmCaptionFont.lfHeight, scaleFactor, 100);

	mCaptionHeight = MulDiv(ncm.iSmCaptionHeight, scaleFactor, 100);
	mhfontCaption = CreateFontIndirect(&ncm.lfSmCaptionFont);

	LOGFONT lf = ncm.lfSmCaptionFont;
	lf.lfEscapement = 0;
	lf.lfWidth = 0;
	lf.lfOrientation = 0;
	lf.lfItalic = FALSE;
	lf.lfUnderline = FALSE;
	lf.lfWeight = 0;
	lf.lfCharSet = DEFAULT_CHARSET;
	lf.lfOutPrecision = OUT_DEFAULT_PRECIS;
	lf.lfClipPrecision = CLIP_DEFAULT_PRECIS;
	lf.lfQuality = DEFAULT_QUALITY;
	lf.lfPitchAndFamily = DEFAULT_PITCH | FF_DONTCARE;
	vdwcslcpy(lf.lfFaceName, L"Marlett", sizeof(lf.lfFaceName)/sizeof(lf.lfFaceName[0]));

	mhfontCaptionSymbol = CreateFontIndirect(&lf);

	mhfontLabel = CreateFontW(-MulDiv(8, monitorDpi, 72), 0, 0, 0, 0, FALSE, FALSE, FALSE, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, L"MS Shell Dlg 2");

	NotifyFontsUpdated();

	if (hfontLabelOld) {
		DeleteFont(hfontLabelOld);
		hfontLabelOld = NULL;
	}

	if (hfontCaptionOld) {
		DeleteFont(hfontCaptionOld);
		hfontCaptionOld = NULL;
	}

	if (hfontCaptionSymbolOld) {
		DeleteFont(hfontCaptionSymbolOld);
		hfontCaptionSymbolOld = NULL;
	}
}

void ATContainerWindow::DestroySystemObjects() {
	if (mhfontLabel) {
		DeleteFont(mhfontLabel);
		mhfontLabel = NULL;
	}

	if (mhfontCaption) {
		DeleteFont(mhfontCaption);
		mhfontCaption = NULL;
	}

	if (mhfontCaptionSymbol) {
		DeleteFont(mhfontCaptionSymbol);
		mhfontCaptionSymbol = NULL;
	}
}

void ATContainerWindow::UpdateMonitorDpi() {
	uint32 dpi = ATUIGetWindowDpiW32(mhwnd);

	if (dpi) {
		mMonitorDpi = dpi;
		UpdateMonitorDpi(dpi);
	}
}

void ATContainerWindow::UpdateMonitorDpi(unsigned dpiY) {
	mpDockingPane->NotifyDpiChanged(dpiY);
}

ATFrameWindow *ATContainerWindow::ChooseNewActiveFrame(ATFrameWindow *prevFrame) {
	ATFrameWindow *frameToActivate = nullptr;

	for(ATContainerDockingPane *pane = prevFrame->GetPane(); pane; pane = pane->GetParentPane()) {
		uint32 n = pane->GetContentCount();

		for(uint32 i=0; i<n; ++i) {
			ATFrameWindow *frame = pane->GetContent(i);

			if (frame && frame != prevFrame && frame->IsVisible())
				return frame;
		}
	}

	return mpDockingPane->GetAnyContent(true, prevFrame);
}

///////////////////////////////////////////////////////////////////////////

ATFrameWindow::ATFrameWindow(ATContainerWindow *container)
	: mpContainer(container)
{
}

ATFrameWindow::~ATFrameWindow() {
}

ATFrameWindow *ATFrameWindow::GetFrameWindow(HWND hwnd) {
	if (hwnd) {
		ATUINativeWindow *w = (ATUINativeWindow *)GetWindowLongPtr(hwnd, 0);
		return vdpoly_cast<ATFrameWindow *>(w);
	}

	return NULL;
}

ATFrameWindow *ATFrameWindow::GetFrameWindowFromContent(HWND hwnd) {
	if (!hwnd)
		return nullptr;

	HWND hwndParent = GetParent(hwnd);
	if (!hwndParent)
		return nullptr;

	return GetFrameWindow(hwndParent);
}

void *ATFrameWindow::AsInterface(uint32 iid) {
	if (iid == ATFrameWindow::kTypeID)
		return static_cast<ATFrameWindow *>(this);

	return ATUINativeWindow::AsInterface(iid);
}

bool ATFrameWindow::IsActivelyMovingSizing() const {
	return mFrameMode == kFrameModeUndocked ? mbActivelyMovingSizing : mpContainer->IsActivelyMovingSizing();
}

bool ATFrameWindow::IsFullScreen() const {
	return mbFullScreen;
}

void ATFrameWindow::SetFullScreen(bool fs) {
	if (mbFullScreen == fs)
		return;

	mbFullScreen = fs;

	if (mpDockingPane)
		mpDockingPane->UpdateFullScreenState();

	if (mpContainer)
		mpContainer->SetFullScreenFrame(fs ? this : NULL);

	if (mhwnd) {
		LONG style = GetWindowLong(mhwnd, GWL_STYLE);
		LONG exStyle = GetWindowLong(mhwnd, GWL_EXSTYLE);

		if (fs) {
			style &= ~(WS_CAPTION | WS_THICKFRAME | WS_POPUP);
			if (!(style & WS_CHILD))
				style |= WS_POPUP;
			exStyle &= ~WS_EX_TOOLWINDOW;
		} else {
			style &= ~WS_POPUP;
			style |= WS_CAPTION;

			if (!(style & WS_CHILD))
				style |= WS_THICKFRAME;

			exStyle |= WS_EX_TOOLWINDOW;
		}

		SetWindowLong(mhwnd, GWL_STYLE, style);
		SetWindowLong(mhwnd, GWL_EXSTYLE, exStyle);
		SetWindowPos(mhwnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

		HWND hwndChild = GetWindow(mhwnd, GW_CHILD);
		if (hwndChild)
			SendMessage(hwndChild, ATWM_SETFULLSCREEN, fs, 0);
	}
}

bool ATFrameWindow::IsVisible() const {
	return mhwnd && (GetWindowLong(mhwnd, GWL_STYLE) & WS_VISIBLE);
}

void ATFrameWindow::SetVisible(bool vis) {
	if (mhwnd)
		::ShowWindow(mhwnd, vis ? SW_SHOWNOACTIVATE : SW_HIDE);
}

void ATFrameWindow::SetFrameMode(FrameMode fm) {
	if (mFrameMode != fm) {
		mFrameMode = fm;

		// Disable theming on the window whenever the window is docked. This is
		// required so we can handle WM_NCACTIVATE properly.
		if (fm == kFrameModeUndocked)
			SetWindowTheme(mhwnd, nullptr, nullptr);
		else
			SetWindowTheme(mhwnd, L"", L"");
	}

	if (fm != kFrameModeUndocked)
		mbActivelyMovingSizing = false;
}

void ATFrameWindow::SetSplitterEdges(uint8 flags) {
	mSplitterEdgeFlags = flags;
}

void ATFrameWindow::ActivateFrame() {
	if (mpContainer)
		mpContainer->ActivateFrame(this);
}

void ATFrameWindow::EnableEndTrackNotification() {
	VDASSERT(IsActivelyMovingSizing());

	if (!mbEnableEndTrackNotification) {
		mbEnableEndTrackNotification = true;

		if (IsDocked())
			mpContainer->AddTrackingNotification(this);
	}
}

void ATFrameWindow::NotifyFontsUpdated() {
	if (mhwnd) {
		HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

		if (hwndChild)
			SendMessage(hwndChild, ATWM_FONTSUPDATED, 0, 0);
	}
}

void ATFrameWindow::NotifyDpiChanged(uint32 dpi) {
	if (mhwnd) {
		HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

		if (hwndChild)
			SendMessage(hwndChild, ATWM_INHERIT_DPICHANGED, MAKELONG(dpi, dpi), 0);
	}
}

void ATFrameWindow::NotifyEndTracking() {
	mbEnableEndTrackNotification = false;

	if (mhwnd) {
		HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

		if (hwndChild)
			SendMessage(hwndChild, ATWM_ENDTRACKING, 0, 0);
	}
}

bool ATFrameWindow::GetIdealSize(vdsize32& sz) {
	if (!mhwnd)
		return false;

	sz.w = 0;
	sz.h = 0;

	HWND hwndChild = GetWindow(mhwnd, GW_CHILD);
	if (!hwndChild)
		return false;

	if (!SendMessage(hwndChild, ATWM_GETAUTOSIZE, 0, (LPARAM)&sz))
		return false;

	if (mFrameMode == kFrameModeUndocked) {
		RECT r = {0, 0, sz.w, sz.h};

		AdjustWindowRectEx(&r, WS_POPUP | WS_VISIBLE, FALSE, WS_EX_TOOLWINDOW);

		sz.w = r.right - r.left;
		sz.h = r.bottom - r.top;
	} else if (mFrameMode != kFrameModeNone) {
		NONCLIENTMETRICS ncm = {
#if WINVER >= 0x0600
			offsetof(NONCLIENTMETRICS, iPaddedBorderWidth)
#else
			sizeof(NONCLIENTMETRICS)
#endif
		};
		SystemParametersInfo(SPI_GETNONCLIENTMETRICS, sizeof(NONCLIENTMETRICS), &ncm, FALSE);

		if (mFrameMode == kFrameModeFull)
			sz.h += ncm.iSmCaptionHeight;

		sz.w += 2*GetSystemMetrics(SM_CXEDGE);
		sz.h += 2*GetSystemMetrics(SM_CYEDGE);
	}

	return true;
}

void ATFrameWindow::RecalcFrame() {
	if (mhwnd)
		SetWindowPos(mhwnd, NULL, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
}

void ATFrameWindow::Relayout(int w, int h) {
	HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

	if (hwndChild) {
		RECT r = {0, 0, w, h};

		if (mFrameMode != kFrameModeNone) {
			if (mFrameMode == kFrameModeFull)
				r.top += mpContainer->GetCaptionHeight();

			if (r.top > r.bottom)
				r.top = r.bottom;

			int xe = GetSystemMetrics(SM_CXEDGE);
			int ye = GetSystemMetrics(SM_CYEDGE);
			r.left += xe;
			r.top += ye;
			r.right -= xe;
			r.bottom -= ye;
		}

		SetWindowPos(hwndChild, NULL, 0, 0, std::max<int>(0, r.right - r.left), std::max<int>(0, r.bottom - r.top), SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
	}
}

VDGUIHandle ATFrameWindow::Create(const wchar_t *title, int x, int y, int cx, int cy, VDGUIHandle parent) {
	return (VDGUIHandle)CreateWindowExW(WS_EX_TOOLWINDOW, (LPCWSTR)(uintptr_t)sWndClass, title, WS_OVERLAPPEDWINDOW|WS_CLIPCHILDREN|WS_CLIPSIBLINGS, x, y, cx, cy, (HWND)parent, NULL, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));
}

VDGUIHandle ATFrameWindow::CreateChild(const wchar_t *title, int x, int y, int cx, int cy, VDGUIHandle parent) {
	return (VDGUIHandle)CreateWindowExW(WS_EX_TOOLWINDOW, (LPCWSTR)(uintptr_t)sWndClass, title, WS_CHILD|WS_CAPTION|WS_CLIPCHILDREN|WS_CLIPSIBLINGS, x, y, cx, cy, (HWND)parent, NULL, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));
}

LRESULT ATFrameWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			mTitle = (const TCHAR *)((LPCREATESTRUCT)lParam)->lpszName;
			if (!OnCreate())
				return -1;
			break;

		case WM_DESTROY:
			OnDestroy();
			break;

		case WM_SIZE:
			OnSize();
			break;

		case WM_CLOSE:
			if (mpContainer && mpContainer->GetModalFrame() == this) {
				::MessageBeep(MB_ICONERROR);
				return 0;
			}
			break;

		case WM_PARENTNOTIFY:
			if (LOWORD(wParam) == WM_CREATE)
				PostMessage(mhwnd, WM_USER+100, 0, 0);
			break;

		case WM_NCLBUTTONDOWN:
			if (mpDockingPane && wParam == HTCLOSE) {
				mbCloseDown = true;
				mbCloseTracking = true;
				::SetCapture(mhwnd);
				PaintCaption(NULL);
				return 0;
			}

			if (OnNCLButtonDown((int)wParam, (int)GET_X_LPARAM(lParam), (int)GET_Y_LPARAM(lParam)))
				return 0;
			break;

		case WM_LBUTTONUP:
			if (mbCloseTracking) {
				mbCloseTracking = false;
				::ReleaseCapture();

				int x = GET_X_LPARAM(lParam);
				int y = GET_Y_LPARAM(lParam);

				POINT pt = {x, y};
				ClientToScreen(mhwnd, &pt);

				x = pt.x;
				y = pt.y;

				RECT r = {};
				GetWindowRect(mhwnd, &r);

				x -= r.left;
				y -= r.top;

				mbCloseDown = false;

				PaintCaption(NULL);

				if (mCloseRect.contains(vdpoint32(x, y)))
					SendMessage(mhwnd, WM_SYSCOMMAND, SC_CLOSE, lParam);
			}

			if (mbDragging) {
				EndDrag(true);
			}
			break;

		case WM_MOUSEMOVE:
			if (mbCloseTracking) {
				int x = GET_X_LPARAM(lParam);
				int y = GET_Y_LPARAM(lParam);

				POINT pt = {x, y};
				ClientToScreen(mhwnd, &pt);

				x = pt.x;
				y = pt.y;

				RECT r = {};
				GetWindowRect(mhwnd, &r);

				x -= r.left;
				y -= r.top;

				bool closeDown = mCloseRect.contains(vdpoint32(x, y));

				if (mbCloseDown != closeDown) {
					mbCloseDown = closeDown;

					PaintCaption(NULL);
				}

				return 0;
			}

			if (OnMouseMove(GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)))
				return 0;
			break;

		case WM_CAPTURECHANGED:
			if (mbCloseTracking) {
				mbCloseTracking = false;
				return 0;
			}

			if ((HWND)lParam != mhwnd) {
				EndDrag(false);
			}
			break;

		case WM_KEYDOWN:
			if (mbDragging) {
				if (wParam == VK_ESCAPE) {
					EndDrag(false);
				}
			}
			break;

		case WM_MOUSEACTIVATE:
			if (mFrameMode != kFrameModeUndocked) {
				// Suppress activation on click for areas that we pass onto nearby splitters.
				switch(LOWORD(lParam)) {
					case HTLEFT:
					case HTRIGHT:
					case HTTOP:
					case HTBOTTOM:
						return MA_NOACTIVATE;
				}
			}

			[[fallthrough]];
		case WM_CHILDACTIVATE:
			if (ATContainerWindow *cont = ATContainerWindow::GetContainerWindow(GetAncestor(mhwnd, GA_ROOTOWNER))) {
				cont->NotifyFrameActivated(this);

				if (msg == WM_MOUSEACTIVATE) {
					HWND focus = ::GetFocus();
					HWND hwndTest;

					for(hwndTest = focus; hwndTest && hwndTest != mhwnd; hwndTest = GetAncestor(hwndTest, GA_PARENT))
						;

					if (hwndTest != mhwnd)
						::SetFocus(mhwnd);
				}
			}
			break;

		case WM_SETFOCUS:
			{
				HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

				if (hwndChild)
					SetFocus(hwndChild);
			}
			return 0;

		case WM_ERASEBKGND:
			return TRUE;

		case WM_NCACTIVATE:
			mbActiveCaption = (wParam != 0);

			if (mpDockingPane) {
				PaintCaption(NULL);
				
				// DefWindowProc(WM_NCACTIVATE) by default redraws the caption directly without going
				// through WM_NCPAINT, which we need to suppress. We used to toggle WS_VISIBLE around
				// this call, but it turns out that's a bad thing with the DWM since it causes the
				// DWM to intermittently fail to paint the window. The Chromium source code indicates
				// that this is due to a race condition with the GPU painting thread. A workaround we
				// use is to pass -1 to lParam, which is documented as suppressing the caption redraw
				// for non-themed windows; to make this stick, we have to disable the theming whenever
				// frames are docked. That's fine, since we are doing full custom frame anyway.
				return ATUINativeWindow::WndProc(msg, wParam, -1);
			}
			break;

		case WM_NCCALCSIZE:
			if (mFrameMode != kFrameModeUndocked) {
				RECT& r = *(RECT *)lParam;
				const int x = r.left;
				const int y = r.top;

				mCaptionRect.set(0, 0, 0, 0);
				mCloseRect.set(0, 0, 0, 0);

				if (mbFullScreen) {
					mInsideBorderRect = { r.left, r.top, r.right, r.bottom };
				} else if (mFrameMode == kFrameModeEdge) {
					if (r.top > r.bottom)
						r.top = r.bottom;

					int xe = GetSystemMetrics(SM_CXEDGE);
					int ye = GetSystemMetrics(SM_CYEDGE);
					r.left += xe;
					r.top += ye;
					r.right -= xe;
					r.bottom -= ye;

					if (r.right < r.left)
						r.right = r.left;

					if (r.bottom < r.top)
						r.bottom = r.top;

					mInsideBorderRect = { r.left, r.top, r.right, r.bottom };
				} else if (mFrameMode == kFrameModeFull) {
					const int xe = GetSystemMetrics(SM_CXEDGE);
					const int ye = GetSystemMetrics(SM_CYEDGE);

					mInsideBorderRect = { 0, 0, r.right, r.bottom };

					mInsideBorderRect.left += xe;
					mInsideBorderRect.top += ye;
					mInsideBorderRect.right -= xe;
					mInsideBorderRect.bottom -= ye;

					if (mInsideBorderRect.right < mInsideBorderRect.left)
						mInsideBorderRect.right = mInsideBorderRect.left;

					if (mInsideBorderRect.bottom < mInsideBorderRect.top)
						mInsideBorderRect.bottom = mInsideBorderRect.top;

					const int h = mpContainer->GetCaptionHeight();

					mCaptionRect.set(0, 0, r.right, h);

					int bsize = std::min<int>(GetSystemMetrics(SM_CXSMSIZE), GetSystemMetrics(SM_CYSMSIZE));

					mCloseRect.set(r.right - r.left - bsize, 0, r.right - r.left, h);

					r.top += h;
					if (r.top > r.bottom)
						r.top = r.bottom;

					r.left += xe;
					r.top += ye;
					r.right -= xe;
					r.bottom -= ye;

					if (r.right < r.left)
						r.right = r.left;

					if (r.bottom < r.top)
						r.bottom = r.top;
				}

				mClientRect.set(r.left, r.top, r.right, r.bottom);
				mClientRect.translate(-x, -y);
				return 0;
			}
			break;

		case WM_NCPAINT:
			if (mFrameMode != kFrameModeUndocked) {
				PaintCaption((HRGN)wParam);
				return 0;
			}
			break;

		case WM_NCHITTEST:
			if (mFrameMode != kFrameModeUndocked) {
				if (mbFullScreen || mFrameMode == kFrameModeNone)
					return HTCLIENT;

				int x = GET_X_LPARAM(lParam);
				int y = GET_Y_LPARAM(lParam);

				POINT pt0 { x, y };

				ScreenToClient(mhwnd, &pt0);

				pt0.x += mClientRect.left;
				pt0.y += mClientRect.top;

				const vdpoint32 windowPt(pt0.x, pt0.y);

				if (!mInsideBorderRect.contains(windowPt)) {
					uint8 edgeFlags = 0;

					if (windowPt.x < mInsideBorderRect.left)
						edgeFlags |= (uint8)ATUIContainerEdgeFlags::Left;

					if (windowPt.y < mInsideBorderRect.top)
						edgeFlags |= (uint8)ATUIContainerEdgeFlags::Top;

					if (windowPt.x >= mInsideBorderRect.right)
						edgeFlags |= (uint8)ATUIContainerEdgeFlags::Right;

					if (windowPt.y >= mInsideBorderRect.bottom)
						edgeFlags |= (uint8)ATUIContainerEdgeFlags::Bottom;

					edgeFlags &= mSplitterEdgeFlags;

					if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Left)
						return HTLEFT;

					if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Right)
						return HTRIGHT;

					if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Top)
						return HTTOP;

					if (edgeFlags & (uint8)ATUIContainerEdgeFlags::Bottom)
						return HTBOTTOM;
				}

				if (mCloseRect.contains(windowPt))
					return HTCLOSE;

				if (mCaptionRect.contains(windowPt))
					return HTCAPTION;

				if (mClientRect.contains(windowPt))
					return HTCLIENT;

				return HTBORDER;
			}
			break;

		case WM_SETCURSOR:
			if (wParam == (WPARAM)mhwnd) {
				switch(LOWORD(lParam)) {
					case HTLEFT:
					case HTRIGHT:
						SetCursor(LoadCursor(nullptr, IDC_SIZEWE));
						return TRUE;

					case HTTOP:
					case HTBOTTOM:
						SetCursor(LoadCursor(nullptr, IDC_SIZENS));
						return TRUE;
				}
			}
			break;

		case WM_SETTEXT:
			mTitle = (const TCHAR *)lParam;
			if (mFrameMode != kFrameModeUndocked) {
				DWORD prevFlags = GetWindowLong(mhwnd, GWL_STYLE);
				if (prevFlags & WS_CAPTION)
					SetWindowLong(mhwnd, GWL_STYLE, prevFlags & ~WS_CAPTION);

				LRESULT r = ATUINativeWindow::WndProc(msg, wParam, lParam);

				if (prevFlags & WS_CAPTION)
					SetWindowLong(mhwnd, GWL_STYLE, prevFlags);

				return r;
			}
			break;

		case WM_ENTERSIZEMOVE:
			if (mFrameMode == kFrameModeUndocked)
				mbActivelyMovingSizing = true;
			break;

		case WM_EXITSIZEMOVE:
			if (mFrameMode == kFrameModeUndocked && mbActivelyMovingSizing) {
				mbActivelyMovingSizing = false;
				NotifyEndTracking();
			}
			break;

		case WM_DPICHANGED:
			if (mFrameMode == kFrameModeUndocked) {
				const RECT& r = *(const RECT *)lParam;

				SetWindowPos(mhwnd, NULL, r.left, r.top, r.right - r.left, r.bottom - r.top, SWP_NOZORDER | SWP_NOACTIVATE);
				RedrawWindow(mhwnd, NULL, NULL, RDW_INVALIDATE);

				HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

				if (hwndChild)
					SendMessage(mhwnd, ATWM_INHERIT_DPICHANGED, wParam, 0);

				return 0;
			}
			break;

		case ATWM_INHERIT_DPICHANGED:
			if (mFrameMode == kFrameModeUndocked) {
				HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

				if (hwndChild)
					SendMessage(mhwnd, ATWM_INHERIT_DPICHANGED, wParam, 0);

				return 0;
			}
			break;

		case WM_USER + 100:
			OnSize();
			return 0;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

void ATFrameWindow::PaintCaption(HRGN clipRegion) {
	if (mbFullScreen || !mFrameMode)
		return;

	HDC hdc;
	
	if (clipRegion && clipRegion != (HRGN)1) {
		HRGN regionCopy = CreateRectRgn(0, 0, 0, 0);
		if (!regionCopy)
			return;

		if (ERROR == CombineRgn(regionCopy, clipRegion, nullptr, RGN_COPY)) {
			DeleteObject(regionCopy);
			return;
		}

		hdc = GetDCEx(mhwnd, regionCopy, DCX_WINDOW | DCX_INTERSECTRGN | 0x10000);
		if (!hdc) {
			DeleteObject(regionCopy);
			return;
		}

	} else
		hdc = GetDCEx(mhwnd, NULL, DCX_WINDOW | 0x10000);

	if (!hdc)
		return;

	if (mFrameMode == kFrameModeEdge) {
		int xe = GetSystemMetrics(SM_CXEDGE);
		int ye = GetSystemMetrics(SM_CYEDGE);

		RECT rc;
		rc.left = mClientRect.left - xe;
		rc.top = mClientRect.top - ye;
		rc.right = mClientRect.right + xe;
		rc.bottom = mClientRect.bottom + ye;
		DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);
	} else {
		RECT r;
		GetWindowRect(mhwnd, &r);
		r.right -= r.left;
		r.bottom = mpContainer->GetCaptionHeight();
		r.top = 0;
		r.left = 0;

		int xe = GetSystemMetrics(SM_CXEDGE);
		int ye = GetSystemMetrics(SM_CYEDGE);

		RECT rc;
		rc.left = mClientRect.left - xe;
		rc.top = mClientRect.top - ye;
		rc.right = mClientRect.right + xe;
		rc.bottom = mClientRect.bottom + ye;
		DrawEdge(hdc, &rc, EDGE_SUNKEN, BF_RECT);

		RECT r2 = r;
		if (r2.right < 0)
			r2.right = 0;

		BOOL gradientsEnabled = FALSE;
		SystemParametersInfo(SPI_GETGRADIENTCAPTIONS, 0, &gradientsEnabled, FALSE);

		if (gradientsEnabled) {
			const uint32 c0 = GetSysColor(mbActiveCaption ? COLOR_ACTIVECAPTION : COLOR_INACTIVECAPTION);
			const uint32 c1 = GetSysColor(mbActiveCaption ? COLOR_GRADIENTACTIVECAPTION : COLOR_GRADIENTINACTIVECAPTION);
			TRIVERTEX v[2];
			v[0].x = r.left;
			v[0].y = r.top;
			v[0].Red = (c0 & 0xff) << 8;
			v[0].Green = c0 & 0xff00;
			v[0].Blue = (c0 & 0xff0000) >> 8;
			v[0].Alpha = 0;
			v[1].x = r.right;
			v[1].y = r.bottom;
			v[1].Red = (c1 & 0xff) << 8;
			v[1].Green = c1 & 0xff00;
			v[1].Blue = (c1 & 0xff0000) >> 8;
			v[1].Alpha = 0;

			GRADIENT_RECT gr;
			gr.UpperLeft = 0;
			gr.LowerRight = 1;
			GradientFill(hdc, v, 2, &gr, 1, GRADIENT_FILL_RECT_H);
		} else {
			FillRect(hdc, &r2, mbActiveCaption ? (HBRUSH)(COLOR_ACTIVECAPTION + 1) : (HBRUSH)(COLOR_INACTIVECAPTION + 1));
		}
		
		if (mpContainer) {
			HFONT hfont = mpContainer->GetCaptionFont();
			if (hfont) {
				HGDIOBJ holdfont = SelectObject(hdc, hfont);

				if (holdfont) {
					//SetBkMode(hdc, OPAQUE);
					SetBkMode(hdc, TRANSPARENT);
					SetBkColor(hdc, GetSysColor(mbActiveCaption ? COLOR_ACTIVECAPTION : COLOR_INACTIVECAPTION));
					SetTextColor(hdc, GetSysColor(mbActiveCaption ? COLOR_CAPTIONTEXT : COLOR_INACTIVECAPTIONTEXT));
					SetTextAlign(hdc, TA_LEFT | TA_TOP);

					RECT rc = { mCaptionRect.left + xe*2, mCaptionRect.top, mCaptionRect.right, mCaptionRect.bottom };
					DrawText(hdc, mTitle.data(), mTitle.size(), &rc, DT_NOPREFIX | DT_SINGLELINE | DT_LEFT | DT_VCENTER);
					SelectObject(hdc, holdfont);
				}
			}

			HFONT hfont2 = mpContainer->GetCaptionSymbolFont();
			if (hfont2) {
				HGDIOBJ holdfont = SelectObject(hdc, hfont2);

				if (holdfont) {
					RECT r3;
					r3.left = mCloseRect.left;
					r3.top = mCloseRect.top;
					r3.right = mCloseRect.right;
					r3.bottom = mCloseRect.bottom;

					SetTextColor(hdc, RGB(0, 0, 0));
					if (mbCloseDown)
						DrawText(hdc, _T("r"), 1, &r3, DT_NOPREFIX | DT_LEFT | DT_BOTTOM | DT_SINGLELINE);
					else
						DrawText(hdc, _T("r"), 1, &r3, DT_NOPREFIX | DT_CENTER | DT_VCENTER | DT_SINGLELINE);
					SelectObject(hdc, holdfont);
				}
			}
		}
	}

	ReleaseDC(mhwnd, hdc);
}

bool ATFrameWindow::OnCreate() {
	OnSize();
	return true;
}

void ATFrameWindow::OnDestroy() {
	if (mpContainer) {
		if (mpDockingPane)
			mpContainer->NotifyDockedFrameDestroyed(this);
		else
			mpContainer->NotifyUndockedFrameDestroyed(this);
	}
}

void ATFrameWindow::OnSize() {
	RECT r;
	if (GetClientRect(mhwnd, &r)) {
		HWND hwndChild = GetWindow(mhwnd, GW_CHILD);

		if (hwndChild)
			SetWindowPos(hwndChild, NULL, 0, 0, r.right, r.bottom, SWP_NOZORDER|SWP_NOACTIVATE);
	}

	if (mpDockingPane)
		PaintCaption(NULL);
}

bool ATFrameWindow::OnNCLButtonDown(int code, int x, int y) {
	if (mpDockingPane) {
		if (code == HTLEFT) {
			mpDockingPane->ActivateNearbySplitter(x, y, (uint8)ATUIContainerEdgeFlags::Left);
			return true;
		}

		if (code == HTRIGHT) {
			mpDockingPane->ActivateNearbySplitter(x, y, (uint8)ATUIContainerEdgeFlags::Right);
			return true;
		}

		if (code == HTTOP) {
			mpDockingPane->ActivateNearbySplitter(x, y, (uint8)ATUIContainerEdgeFlags::Top);
			return true;
		}

		if (code == HTBOTTOM) {
			mpDockingPane->ActivateNearbySplitter(x, y, (uint8)ATUIContainerEdgeFlags::Bottom);
			return true;
		}
	}

	if (code != HTCAPTION)
		return false;

	RECT r;

	mbDragging = true;
	mbDragVerified = false;
	GetWindowRect(mhwnd, &r);

	mDragOriginX = x;
	mDragOriginY = y;
	mDragOffsetX = r.left - x;
	mDragOffsetY = r.top - y;

	mpDragContainer = ATContainerWindow::GetContainerWindow(GetWindow(mhwnd, GW_OWNER));

	SetForegroundWindow(mhwnd);
	SetActiveWindow(mhwnd);
	SetFocus(mhwnd);
	SetCapture(mhwnd);
	return true;
}

bool ATFrameWindow::OnMouseMove(int x, int y) {
	if (!mbDragging)
		return false;

	POINT pt = {x, y};
	ClientToScreen(mhwnd, &pt);

	if (!mbDragVerified) {
		int dx = abs(GetSystemMetrics(SM_CXDRAG));
		int dy = abs(GetSystemMetrics(SM_CYDRAG));

		if (abs(mDragOriginX - pt.x) <= dx && abs(mDragOriginY - pt.y) <= dy)
			return true;

		mbDragVerified = true;

		UINT style = GetWindowLong(mhwnd, GWL_STYLE);
		if (style & WS_CHILD) {
			mpDragContainer->UndockFrame(this);
		}

		if (mpDragContainer) {
			mpDragContainer->InitDragHandles();
		}
	}

	SetWindowPos(mhwnd, NULL, pt.x + mDragOffsetX, pt.y + mDragOffsetY, 0, 0, SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE);

	if (mpDragContainer)
		mpDragContainer->UpdateDragHandles(pt.x, pt.y);

	return true;
}

void ATFrameWindow::EndDrag(bool success) {
	if (mbDragging) {
		mbDragging = false;		// also prevents recursion
		if (GetCapture() == mhwnd)
			ReleaseCapture();

		if (mpDragContainer) {
			if (mbDragVerified) {
				if (success)
					mpDragContainer->DockFrame(this);

				mpDragContainer->ShutdownDragHandles();
			}

			mpDragContainer = NULL;
		}
	}
}

///////////////////////////////////////////////////////////////////////////////

namespace {
	typedef vdhashmap<uint32, ATPaneCreator> PaneCreators;
	PaneCreators g_paneCreatorMap;

	typedef vdhashmap<uint32, ATPaneClassCreator> PaneClassCreators;
	PaneClassCreators g_paneClassCreatorMap;

	typedef vdhashmap<uint32, ATUIPane *> ActivePanes;
	ActivePanes g_activePanes;
}

void ATRegisterUIPaneType(uint32 id, ATPaneCreator creator) {
	g_paneCreatorMap[id] = creator;
}

void ATRegisterUIPaneClass(uint32 id, ATPaneClassCreator creator) {
	g_paneClassCreatorMap[id] = creator;
}

void ATRegisterActiveUIPane(uint32 id, ATUIPane *w) {
	g_activePanes[id] = w;
}

void ATUnregisterActiveUIPane(uint32 id, ATUIPane *w) {
	g_activePanes.erase(id);
}

void ATGetUIPanes(vdfastvector<ATUIPane *>& panes) {
	for(ActivePanes::const_iterator it(g_activePanes.begin()), itEnd(g_activePanes.end());
		it != itEnd;
		++it)
	{
		panes.push_back(it->second);
	}
}

ATUIPane *ATGetUIPane(uint32 id) {
	ActivePanes::const_iterator it(g_activePanes.find(id));

	return it != g_activePanes.end() ? it->second : NULL;
}

void *ATGetUIPaneAs(uint32 id, uint32 iid) {
	ATUIPane *pane = ATGetUIPane(id);

	return pane ? pane->AsInterface(iid) : nullptr;
}

ATUIPane *ATGetUIPaneByFrame(ATFrameWindow *frame) {
	if (!frame)
		return NULL;

	HWND hwndParent = frame->GetHandleW32();

	ActivePanes::const_iterator it(g_activePanes.begin()), itEnd(g_activePanes.end());
	for(; it != itEnd; ++it) {
		ATUIPane *pane = it->second;
		HWND hwndPane = pane->GetHandleW32();

		if (!hwndPane)
			continue;

		if (GetParent(hwndPane) == hwndParent)
			return pane;
	}

	return NULL;
}

void ATActivateUIPane(uint32 id, bool giveFocus, bool visible, uint32 relid, int reldockmode) {
	vdrefptr<ATUIPane> pane(ATGetUIPane(id));

	if (!pane) {
		if (id >= 0x100) {
			PaneClassCreators::const_iterator it(g_paneClassCreatorMap.find(id & 0xfff00));
			if (it == g_paneClassCreatorMap.end())
				return;
		
			if (!it->second(id, ~pane))
				return;
		} else {
			PaneCreators::const_iterator it(g_paneCreatorMap.find(id));
			if (it == g_paneCreatorMap.end())
				return;
		
			if (!it->second(~pane))
				return;
		}

		vdrefptr<ATFrameWindow> frame(new ATFrameWindow(g_pMainWindow));
		frame->Create(pane->GetUIPaneName(), CW_USEDEFAULT, CW_USEDEFAULT, 300, 200, (VDGUIHandle)g_pMainWindow->GetHandleW32());

		bool paneDocked = false;
		if (relid) {
			ATUIPane *relpane = ATGetUIPane(relid);

			if (relpane) {
				HWND hwndPane = relpane->GetHandleW32();

				if (hwndPane) {
					HWND hwndParent = GetParent(hwndPane);

					if (hwndParent) {
						ATFrameWindow *relframe = ATFrameWindow::GetFrameWindow(hwndParent);

						// We need to check for an undocked pane; currently you can't stack or split an undocked window.
						if (relframe && relframe->GetPane()) {
							ATContainerWindow *relcont = relframe->GetContainer();

							if (relcont) {
								relcont->DockFrame(frame, relframe->GetPane(), reldockmode);
								paneDocked = true;
							}
						}
					}
				}
			}
		}

		if (!paneDocked) {
			int preferredCode = pane->GetPreferredDockCode();
			if (preferredCode >= 0 && visible)
				g_pMainWindow->DockFrame(frame, preferredCode);
			else
				g_pMainWindow->AddUndockedFrame(frame);
		}

		pane->Create(frame);

		if (visible)
			ShowWindow(frame->GetHandleW32(), SW_SHOWNOACTIVATE);
	}

	if (giveFocus) {
		HWND hwndPane = pane->GetHandleW32();
		HWND hwndPaneParent = GetParent(hwndPane);
		SetFocus(hwndPane);

		if (hwndPaneParent) {
			ATFrameWindow *frame = ATFrameWindow::GetFrameWindow(hwndPaneParent);

			// We must not set an undocked pane as activated, as it leads to focus badness
			// (the container window keeps giving activation away).
			if (frame && frame->GetPane())
				g_pMainWindow->NotifyFrameActivated(frame);
		}
	}
}

void ATCloseUIPane(uint32 id) {
	ATUIPane *pane = ATGetUIPane(id);

	if (pane) {
		HWND hwndPane = pane->GetHandleW32();
		HWND hwndPaneParent = GetParent(hwndPane);
		SetFocus(hwndPane);

		if (hwndPaneParent) {
			ATFrameWindow *frame = ATFrameWindow::GetFrameWindow(hwndPaneParent);

			if (frame)
				frame->Close();
		}
	}
}

ATUIPane *ATUIGetActivePane() {
	ATFrameWindow *frame = g_pMainWindow->GetActiveFrame();
	if (!frame)
		return 0;

	HWND hwndFrame = frame->GetHandleW32();
	if (!hwndFrame)
		return 0;

	HWND hwndChild = GetWindow(hwndFrame, GW_CHILD);
	if (!hwndChild)
		return 0;

	ATUINativeWindow *w = (ATUINativeWindow *)GetWindowLongPtr(hwndChild, 0);
	return vdpoly_cast<ATUIPane *>(w);
}

void *ATUIGetActivePaneAs(uint32 iid) {
	ATUIPane *pane = ATUIGetActivePane();

	return pane ? pane->AsInterface(iid) : nullptr;
}

uint32 ATUIGetActivePaneId() {
	ATUIPane *pane = ATUIGetActivePane();

	return pane ? pane->GetUIPaneId() : 0;
}

///////////////////////////////////////////////////////////////////////////

ATUIPane::ATUIPane(uint32 paneId, const wchar_t *name)
	: mpName(name)
	, mPaneId(paneId)
	, mDefaultWindowStyles(WS_CHILD|WS_CLIPCHILDREN)
	, mPreferredDockCode(-1)
{
}

ATUIPane::~ATUIPane() {
}

void *ATUIPane::AsInterface(uint32 iid) {
	if (iid == kTypeID)
		return this;

	return ATUINativeWindow::AsInterface(iid);
}

bool ATUIPane::Create(ATFrameWindow *frame) {
	HWND hwnd = CreateWindow((LPCTSTR)(uintptr_t)sWndClass, _T(""), mDefaultWindowStyles & ~WS_VISIBLE, 0, 0, 0, 0, frame->GetHandleW32(), (HMENU)100, VDGetLocalModuleHandleW32(), static_cast<ATUINativeWindow *>(this));

	if (!hwnd)
		return false;

	::ShowWindow(hwnd, SW_SHOWNOACTIVATE);
	return true;
}

void ATUIPane::SetName(const wchar_t *name) {
	mpName = name;
}

LRESULT ATUIPane::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			if (!OnCreate())
				return -1;
			break;

		case WM_DESTROY:
			OnDestroy();
			break;

		case WM_SIZE:
			OnSize();
			break;

		case WM_SETFOCUS:
			OnSetFocus();
			return 0;

		case WM_COMMAND:
			if (OnCommand(LOWORD(wParam), HIWORD(wParam)))
				return 0;
			break;

		case ATWM_FONTSUPDATED:
			OnFontsUpdated();
			break;
	}

	return ATUINativeWindow::WndProc(msg, wParam, lParam);
}

bool ATUIPane::OnCreate() {
	RegisterUIPane();
	OnSize();
	return true;
}

void ATUIPane::OnDestroy() {
	UnregisterUIPane();
}

void ATUIPane::OnSize() {
}

void ATUIPane::OnSetFocus() {
}

void ATUIPane::OnFontsUpdated() {
}

bool ATUIPane::OnCommand(uint32 id, uint32 extcode) {
	return false;
}

void ATUIPane::RegisterUIPane() {
	ATRegisterActiveUIPane(mPaneId, this);
}

void ATUIPane::UnregisterUIPane() {
	ATUnregisterActiveUIPane(mPaneId, this);
}
