//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - miscellaneous junkpile of utilities
//	Copyright (C) 2008-2016 Avery Lee
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
#include <at/atnativeui/utils.h>

void ATUIEnableNonClientDpiScalingW32(HWND hwnd) {
	static const auto pEnableNonClientDpiScaling = (BOOL (WINAPI *)(HWND))GetProcAddress(GetModuleHandleW(L"user32"), "EnableNonClientDpiScaling");

	if (pEnableNonClientDpiScaling)
		pEnableNonClientDpiScaling(hwnd);
}

BOOL ATUIAdjustWindowRectExForDpiW32(LPRECT pRect, DWORD dwStyle, BOOL bMenu, DWORD dwExStyle, HWND hwnd) {
	static const auto pAdjustWindowRectExForDpi = (BOOL (WINAPI *)(LPRECT, DWORD, BOOL, DWORD, UINT))GetProcAddress(GetModuleHandleW(L"user32"), "AdjustWindowRectExForDpi");
	static const auto pGetDpiForWindow = (UINT (WINAPI *)(HWND))GetProcAddress(GetModuleHandleW(L"user32"), "GetDpiForWindow");

	if (pAdjustWindowRectExForDpi && pGetDpiForWindow) {
		UINT dpi = pGetDpiForWindow(hwnd);

		if (dpi)
			return pAdjustWindowRectExForDpi(pRect, dwStyle, bMenu, dwExStyle, dpi);
	}

	return AdjustWindowRectEx(pRect, dwStyle, bMenu, dwExStyle);
}
