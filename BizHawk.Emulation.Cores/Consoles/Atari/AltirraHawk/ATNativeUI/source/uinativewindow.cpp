//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2014 Avery Lee
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
#include <tchar.h>
#include <vd2/system/win32/touch.h>
#include <vd2/system/w32assist.h>
#include <at/atnativeui/uinativewindow.h>

extern "C" IMAGE_DOS_HEADER __ImageBase;
ATOM ATUINativeWindow::sWndClass;
ATOM ATUINativeWindow::sWndClassMain;

ATUINativeWindow::ATUINativeWindow()
	: mRefCount(0)
	, mTouchMode(kATUITouchMode_Default)
{
}

ATUINativeWindow::~ATUINativeWindow() {
}

ATOM ATUINativeWindow::Register() {
	if (sWndClass)
		return sWndClass;

	WNDCLASS wc = {};
	wc.lpszClassName	= _T("ATUINativeWindow");
	sWndClass = RegisterCustom(wc);
	return sWndClass;
}

ATOM ATUINativeWindow::RegisterCustom(const WNDCLASS& wc0) {
	WNDCLASS wc(wc0);

	wc.style			|= CS_DBLCLKS;
	wc.lpfnWndProc		= StaticWndProc;
	wc.cbClsExtra		= 0;
	wc.cbWndExtra		= sizeof(ATUINativeWindow *);
	wc.hInstance		= (HINSTANCE)&__ImageBase;

	if (!wc.hCursor)
		wc.hCursor = LoadCursor(NULL, IDC_ARROW);

	if (!wc.hbrBackground)
		wc.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);

	sWndClassMain = RegisterClass(&wc);

	return sWndClassMain;
}

void ATUINativeWindow::Unregister() {
	if (sWndClass) {
		UnregisterClass((LPCTSTR)(uintptr_t)sWndClass, (HINSTANCE)&__ImageBase);
		sWndClass = NULL;
	}
}

int ATUINativeWindow::AddRef() {
	return ++mRefCount;
}

int ATUINativeWindow::Release() {
	int rc = --mRefCount;

	if (!rc)
		delete this;

	return 0;
}

void *ATUINativeWindow::AsInterface(uint32 iid) {
	return NULL;
}

bool ATUINativeWindow::CreateChild(HWND hwndParent, UINT id, int x, int y, int w, int h, DWORD styles, DWORD exstyles, const wchar_t *text) {
	VDASSERT(styles & WS_CHILD);
	return CreateWindowExW(exstyles, MAKEINTATOM(sWndClass), text ? text : L"", styles, x, y, w, h, hwndParent, (HMENU)(INT_PTR)id, VDGetLocalModuleHandleW32(), this) != nullptr;
}

void ATUINativeWindow::SetTouchMode(ATUITouchMode touchMode) {
	if (mTouchMode != touchMode) {
		mTouchMode = touchMode;

		UpdateTouchMode(touchMode);
	}
}

LRESULT ATUINativeWindow::StaticWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	ATUINativeWindow *p;

	if (msg == WM_NCCREATE) {
		p = (ATUINativeWindow *)((LPCREATESTRUCT)lParam)->lpCreateParams;
		SetWindowLongPtr(hwnd, 0, (LONG_PTR)p);
		p->AddRef();
		p->mhwnd = hwnd;
	} else
		p = (ATUINativeWindow *)GetWindowLongPtr(hwnd, 0);

	if (!p)
		return DefWindowProc(hwnd, msg, wParam, lParam);

	p->AddRef();
	LRESULT result = p->WndProc(msg, wParam, lParam);

	if (msg == WM_NCDESTROY && p) {
		p->mhwnd = NULL;
		p->Release();
		SetWindowLongPtr(hwnd, 0, NULL);
	}
	p->Release();

	return result;
}

LRESULT ATUINativeWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			VDRegisterTouchWindowW32(mhwnd, 0);
			UpdateTouchMode(mTouchMode);
			break;

		case WM_TABLET_QUERYSYSTEMGESTURESTATUS:
			{
				ATUITouchMode mode = mTouchMode;

				if (mode == kATUITouchMode_Dynamic || mode == kATUITouchMode_MultiTouchDynamic) {
					POINT pt = { (short)LOWORD(lParam), (short)HIWORD(lParam) };

					ScreenToClient(mhwnd, &pt);

					mode = GetTouchModeAtPoint(vdpoint32(pt.x, pt.y));
				}

				switch(mode) {
					case kATUITouchMode_Default:
					case kATUITouchMode_Direct:
					case kATUITouchMode_VerticalPan:
					case kATUITouchMode_2DPan:
					case kATUITouchMode_2DPanSmooth:
					case kATUITouchMode_MultiTouch:
						return TABLET_DISABLE_FLICKS;

					case kATUITouchMode_Immediate:
					case kATUITouchMode_MultiTouchImmediate:
						return TABLET_DISABLE_PRESSANDHOLD | TABLET_DISABLE_FLICKS;
				}
			}

			break;

		case WM_GESTURENOTIFY:
			if (mTouchMode == kATUITouchMode_Dynamic || mTouchMode == kATUITouchMode_MultiTouchDynamic) {
				const GESTURENOTIFYSTRUCT& gns = *(const GESTURENOTIFYSTRUCT *)lParam;
				POINT pt = { gns.ptsLocation.x, gns.ptsLocation.y };

				ScreenToClient(mhwnd, &pt);

				ATUITouchMode mode = GetTouchModeAtPoint(vdpoint32(pt.x, pt.y));

				UpdateTouchMode(mode);
			} else
				UpdateTouchMode(mTouchMode);
			break;
	}

	return DefWindowProc(mhwnd, msg, wParam, lParam);
}

ATUITouchMode ATUINativeWindow::GetTouchModeAtPoint(const vdpoint32& pt) const {
	return kATUITouchMode_Default;
}

void ATUINativeWindow::UpdateTouchMode(ATUITouchMode touchMode) {
	if (!mhwnd)
		return;

	switch(touchMode) {
		case kATUITouchMode_MultiTouch:
		case kATUITouchMode_MultiTouchImmediate:
		case kATUITouchMode_MultiTouchDynamic:
			VDRegisterTouchWindowW32(mhwnd, 0);
			break;

		default:
			VDUnregisterTouchWindowW32(mhwnd);
			break;
	}

	switch(touchMode) {
		case kATUITouchMode_Default:
			{
				GESTURECONFIG gc[] = {
					{ 0, GC_ALLGESTURES, 0 },
				};

				VDSetGestureConfigW32(mhwnd, 0, 1, &gc[0], sizeof(GESTURECONFIG));
			}
			break;
		case kATUITouchMode_MultiTouch:
		case kATUITouchMode_MultiTouchImmediate:
		case kATUITouchMode_Immediate:
		case kATUITouchMode_Direct:
			{
				GESTURECONFIG gc = {
					0,
					0,
					GC_ALLGESTURES,
				};

				VDSetGestureConfigW32(mhwnd, 0, 1, &gc, sizeof(GESTURECONFIG));
			}
			break;
		case kATUITouchMode_VerticalPan:
			{
				GESTURECONFIG gc[] = {
					{ 0, GC_ALLGESTURES, 0 },
					{ GID_PAN, GC_PAN | GC_PAN_WITH_GUTTER | GC_PAN_WITH_SINGLE_FINGER_VERTICALLY, GC_PAN_WITH_SINGLE_FINGER_HORIZONTALLY }
				};

				VDSetGestureConfigW32(mhwnd, 0, 1, &gc[0], sizeof(GESTURECONFIG));
				VDSetGestureConfigW32(mhwnd, 0, 1, &gc[1], sizeof(GESTURECONFIG));
			}
			break;
		case kATUITouchMode_2DPan:
			{
				GESTURECONFIG gc[] = {
					{ 0, GC_ALLGESTURES, 0},
				};

				VDSetGestureConfigW32(mhwnd, 0, 1, gc, sizeof(GESTURECONFIG));
			}
			break;
		case kATUITouchMode_2DPanSmooth:
			{
				GESTURECONFIG gc[] = {
					{ 0, GC_ALLGESTURES, 0},
					{ GID_PAN, 0, GC_PAN_WITH_GUTTER},
				};

				VDSetGestureConfigW32(mhwnd, 0, 1, &gc[0], sizeof(GESTURECONFIG));
				VDSetGestureConfigW32(mhwnd, 0, 1, &gc[1], sizeof(GESTURECONFIG));
			}
			break;
		case kATUITouchMode_Dynamic:
		case kATUITouchMode_MultiTouchDynamic:
			break;
	}
}
