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

#ifndef f_VD2_VDDISPLAY_LOGGING_H
#define f_VD2_VDDISPLAY_LOGGING_H

#include <vd2/system/function.h>
#include <stdarg.h>

void VDDispSetLogHook(vdfunction<void(const char *)> fn);
void VDDispLog(const char *msg);
void VDDispLogF(const char *format, ...);
void VDDispLogV(const char *format, va_list args);

#endif
