//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#include <stdafx.h>
#include <windows.h>
#include <vd2/system/w32assist.h>
#include <at/atnativeui/nativewindowproxy.h>

void ATUINativeWindowProxy::SetVisible(bool visible) {
	if (mhwnd)
		ShowWindow(mhwnd, visible ? SW_SHOWNOACTIVATE : SW_HIDE);
}

void ATUINativeWindowProxy::Show() {
	if (mhwnd)
		ShowWindow(mhwnd, SW_SHOWNOACTIVATE);
}

void ATUINativeWindowProxy::Hide() {
	if (mhwnd)
		ShowWindow(mhwnd, SW_HIDE);
}

void ATUINativeWindowProxy::Activate() {
	if (mhwnd) {
		ShowWindow(mhwnd, SW_SHOW);

		if (GetActiveWindow() != mhwnd)
			SetFocus(mhwnd);
	}
}

void ATUINativeWindowProxy::Focus() {
	if (mhwnd)
		SetFocus(mhwnd);
}

void ATUINativeWindowProxy::Close() {
	if (mhwnd)
		SendMessage(mhwnd, WM_CLOSE, 0, 0);
}

void ATUINativeWindowProxy::Destroy() {
	if (mhwnd)
		DestroyWindow(mhwnd);
}

bool ATUINativeWindowProxy::IsEnabled() const {
	return mhwnd && !(GetWindowLong(mhwnd, GWL_STYLE) & WS_DISABLED);
}

void ATUINativeWindowProxy::SetEnabled(bool enabled) {
	if (mhwnd)
		EnableWindow(mhwnd, enabled);
}

bool ATUINativeWindowProxy::IsVisible() const {
	return mhwnd && (GetWindowLong(mhwnd, GWL_STYLE) & WS_VISIBLE);
}

vdsize32 ATUINativeWindowProxy::GetSize() const {
	if (!mhwnd)
		return vdsize32(0, 0);

	RECT r;
	if (!GetWindowRect(mhwnd, &r))
		return vdsize32(0, 0);

	return vdsize32(r.right - r.left, r.bottom - r.top);
}

void ATUINativeWindowProxy::SetSize(const vdsize32& sz) {
	if (!mhwnd)
		return;

	SetWindowPos(mhwnd, NULL, 0, 0, sz.w, sz.h, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
}

vdpoint32 ATUINativeWindowProxy::GetPosition() const {
	if (!mhwnd)
		return vdpoint32{0, 0};

	RECT r;
	if (!GetWindowRect(mhwnd, &r))
		return vdpoint32{0, 0};

	HWND hwndParent = GetAncestor(mhwnd, GA_PARENT);
	if (hwndParent) {
		SetLastError(0);

		if (!MapWindowPoints(NULL, hwndParent, (LPPOINT)&r, 2) && GetLastError())
			return vdpoint32{0, 0};
	}

	return vdpoint32{r.left, r.top};
}

void ATUINativeWindowProxy::SetPosition(const vdpoint32& pt) {
	if (!mhwnd)
		return;

	SetWindowPos(mhwnd, NULL, pt.x, pt.y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
}

vdrect32 ATUINativeWindowProxy::GetArea() const {
	if (!mhwnd)
		return vdrect32(0, 0, 0, 0);

	RECT r;
	if (!GetWindowRect(mhwnd, &r))
		return vdrect32(0, 0, 0, 0);

	HWND hwndParent = GetAncestor(mhwnd, GA_PARENT);
	if (hwndParent) {
		SetLastError(0);

		if (!MapWindowPoints(NULL, hwndParent, (LPPOINT)&r, 2) && GetLastError())
			return vdrect32(0, 0, 0, 0);
	}

	return vdrect32(r.left, r.top, r.right, r.bottom);
}

void ATUINativeWindowProxy::SetArea(const vdrect32& r) {
	if (mhwnd)
		SetWindowPos(mhwnd, NULL, r.left, r.top, std::max<sint32>(0, r.width()), std::max<sint32>(0, r.height()), SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE);
}

void ATUINativeWindowProxy::BringToFront() {
	if (!mhwnd)
		return;

	SetWindowPos(mhwnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);	
}

vdrect32 ATUINativeWindowProxy::GetClientArea() const {
	if (!mhwnd)
		return vdrect32(0,0,0,0);

	RECT r = {0};
	GetClientRect(mhwnd, &r);
	return vdrect32(r.left, r.top, r.right, r.bottom);
}

vdsize32 ATUINativeWindowProxy::GetClientSize() const {
	RECT r {};

	if (mhwnd)
		GetClientRect(mhwnd, &r);

	return vdsize32{r.right, r.bottom};
}

vdrect32 ATUINativeWindowProxy::GetWindowArea() const {
	RECT r {};

	if (mhwnd)
		GetWindowRect(mhwnd, &r);

	return vdrect32(r.left, r.top, r.right, r.bottom);
}

vdpoint32 ATUINativeWindowProxy::TransformScreenToClient(const vdpoint32& pt) const {
	POINT pt2 = { pt.x, pt.y };

	if (mhwnd)
		ScreenToClient(mhwnd, &pt2);

	return vdpoint32(pt2.x, pt2.y);
}

vdrect32 ATUINativeWindowProxy::TransformScreenToClient(const vdrect32& r) const {
	vdpoint32 p1 = TransformScreenToClient(r.top_left());
	vdpoint32 p2 = TransformScreenToClient(r.bottom_right());

	return vdrect32(p1.x, p1.y, p2.x, p2.y);
}

vdpoint32 ATUINativeWindowProxy::TransformClientToScreen(const vdpoint32& pt) const {
	POINT pt2 = { pt.x, pt.y };

	if (mhwnd)
		ClientToScreen(mhwnd, &pt2);

	return vdpoint32(pt2.x, pt2.y);
}

vdrect32 ATUINativeWindowProxy::TransformClientToScreen(const vdrect32& r) const {
	vdpoint32 p1 = TransformClientToScreen(r.top_left());
	vdpoint32 p2 = TransformClientToScreen(r.bottom_right());

	return vdrect32(p1.x, p1.y, p2.x, p2.y);
}

VDStringW ATUINativeWindowProxy::GetCaption() const {
	return mhwnd ? VDGetWindowTextW32(mhwnd) : VDStringW();
}

void ATUINativeWindowProxy::SetCaption(const wchar_t *caption) {
	if (mhwnd)
		VDSetWindowTextW32(mhwnd, caption);
}
