//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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
#include <at/atcore/progress.h>

IATProgressHandler *g_pATProgressHandler;

void ATBeginProgress(uint32 total, const wchar_t *statusFormat, const wchar_t *desc) {
	if (g_pATProgressHandler)
		g_pATProgressHandler->Begin(total, statusFormat, desc);
}

void ATBeginProgressF(uint32 total, const wchar_t *statusFormat, const wchar_t *descFormat, va_list descArgs) {
	if (g_pATProgressHandler)
		g_pATProgressHandler->BeginF(total, statusFormat, descFormat, descArgs);
}

void ATUpdateProgress(uint32 count) {
	if (g_pATProgressHandler)
		g_pATProgressHandler->Update(count);
}

void ATEndProgress() {
	if (g_pATProgressHandler)
		g_pATProgressHandler->End();
}

void ATSetProgressHandler(IATProgressHandler *h) {
	g_pATProgressHandler = h;
}
