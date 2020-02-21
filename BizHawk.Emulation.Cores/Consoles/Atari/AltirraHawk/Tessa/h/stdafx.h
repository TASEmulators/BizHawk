//	VirtualDub - Video processing and capture application
//	3D acceleration library
//	Copyright (C) 1998-2009 Avery Lee
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

#ifndef f_STDAFX_H
#define f_STDAFX_H

#ifdef _MSC_VER
	#pragma once
#endif

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0501
#endif

struct IUnknown;

#include <sdkddkver.h>

// Needed to work around d3d11.h issue that breaks under Clang/C2:
// 2>c:\dx9sdk6\include\d3d11.h(930,48): error : default initialization of an object of const type 'const CD3D11_DEFAULT' without a user-provided default constructor
#define D3D11_NO_HELPERS

#include <vd2/system/vdtypes.h>

#endif	// f_STDAFX_H

