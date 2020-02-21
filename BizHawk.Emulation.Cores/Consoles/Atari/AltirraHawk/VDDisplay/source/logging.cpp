//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2017 Avery Lee
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
//

#include <stdafx.h>
#include <vd2/system/vdstl.h>
#include <vd2/VDDisplay/logging.h>

vdfunction<void(const char *)> g_VDDispLogHook;

void VDDispSetLogHook(vdfunction<void(const char *)> fn) {
	g_VDDispLogHook = std::move(fn);
}

void VDDispLog(const char *msg) {
	if (g_VDDispLogHook)
		g_VDDispLogHook(msg);
}

void VDDispLogF(const char *format, ...) {
	va_list args;

	va_start(args, format);
	VDDispLogV(format, args);
	va_end(args);
}

void VDDispLogV(const char *format, va_list args) {
	char buf[32];

	int n = vsnprintf(buf, vdcountof(buf), format, args);

	if (n <= 0)
		return;

	if (n < (int)vdcountof(buf)) {
		VDDispLog(buf);
	} else {
		// if for some reason we get an extremely long output, truncate it
		int limit = n > 32768 ? 32768 : n;
		vdblock<char> buf2(limit + 1);

		int n2 = vsnprintf(buf2.data(), limit + 1, format, args);

		if (n2 > 0) {
			buf2[limit] = 0;
			VDDispLog(buf2.data());
		}
	}
}

