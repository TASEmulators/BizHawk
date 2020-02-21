//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - common media definitions
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

#ifndef f_AT_ATCORE_MEDIA_H
#define f_AT_ATCORE_MEDIA_H

#include <vd2/system/vdtypes.h>

enum ATMediaWriteMode : uint8 {
	kATMediaWriteMode_AllowWrite = 1,
	kATMediaWriteMode_AutoFlush = 2,
	kATMediaWriteMode_AllowFormat = 4,
	kATMediaWriteMode_All = 7,

	kATMediaWriteMode_RO = 0,
	kATMediaWriteMode_VRWSafe = kATMediaWriteMode_AllowWrite,
	kATMediaWriteMode_VRW = kATMediaWriteMode_AllowWrite | kATMediaWriteMode_AllowFormat,
	kATMediaWriteMode_RW = kATMediaWriteMode_AllowWrite | kATMediaWriteMode_AutoFlush | kATMediaWriteMode_AllowFormat
};

#endif
