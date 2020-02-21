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

#ifndef f_AT_UINATIVEWINDOWPROXY_H
#define f_AT_UINATIVEWINDOWPROXY_H

#include <vd2/system/win32/miniwindows.h>
#include <vd2/system/vectors.h>
#include <vd2/system/VDString.h>

// ATUINativeWindowProxy
//
// This class is a lightweight wrapper around a window handle, usable either
// directly or as a base class for a control. It exposes operations that can
// be done directly with the window handle using the Win32 API. For that reason,
// the operations here are intentionally non-overridable in base classes as
// any special handling of them must be done in the window procedure instead.
//
class ATUINativeWindowProxy {
public:
	ATUINativeWindowProxy() : mhwnd(nullptr) {}
	ATUINativeWindowProxy(VDZHWND hwnd) : mhwnd(hwnd) {}

	bool IsValid() const { return mhwnd != nullptr; }
	VDZHWND GetWindowHandle() const { return mhwnd; }
	VDZHWND GetHandleW32() const { return mhwnd; }

	// Show or hide a window. The window is not activated when shown.
	bool IsVisible() const;
	void SetVisible(bool visible);
	void Show();
	void Hide();

	void Activate();

	// Set focus to a window, allowing it to receive keyboard input.
	void Focus();

	// Attempt to close a window. Note that this may be intercepted by some
	// windows, particularly to display a confirmation dialog.
	void Close();

	// Destroy the window; it is no longer valid after this call returns, with
	// no confirmation.
	void Destroy();

	// Enable or disable window.
	bool IsEnabled() const;
	void SetEnabled(bool enabled);

	// Retrieve or set the size of a window in the parent's coordinate system.
	// This includes the frame. Origin position is unchanged.
	vdsize32 GetSize() const;
	void SetSize(const vdsize32& sz);
	
	// Retrieve or set the top-left corner of a window's frame in the parent's
	// coordinate system.
	vdpoint32 GetPosition() const;
	void SetPosition(const vdpoint32& pt);

	// Retrieve or set the area (size and position) of a window in the parent's
	// coordinate system. This includes the frame.
	vdrect32 GetArea() const;
	void SetArea(const vdrect32& r);

	// Retrieve the client area of a window. The top-left point is always (0,0).
	vdrect32 GetClientArea() const;

	// Retrieve the size of the client area of a window.
	vdsize32 GetClientSize() const;

	// Retrieve the area of a window in the screen coordinate system.
	vdrect32 GetWindowArea() const;

	// Transform points between the screen and client coordinate systems.
	vdpoint32 TransformScreenToClient(const vdpoint32& pt) const;
	vdrect32 TransformScreenToClient(const vdrect32& r) const;
	vdpoint32 TransformClientToScreen(const vdpoint32& pt) const;
	vdrect32 TransformClientToScreen(const vdrect32& r) const;

	// Raise a window to the top of the Z-order amongst its siblings.
	void BringToFront();

	/// Retrieve or set the caption text of a window.
	VDStringW GetCaption() const;
	void SetCaption(const wchar_t *caption);

protected:
	union {
		VDZHWND mhwnd;
		VDZHWND mhdlg;
	};
};

#endif
