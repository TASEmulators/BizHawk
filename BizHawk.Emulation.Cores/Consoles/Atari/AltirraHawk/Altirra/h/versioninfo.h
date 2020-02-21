//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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

#ifndef f_AT_VERSIONINFO_H
#define f_AT_VERSIONINFO_H

#include "version.h"

#define AT_WIDESTR1(x) L##x
#define AT_WIDESTR(x) AT_WIDESTR1(x)

#ifdef _DEBUG
	#define AT_VERSION_DEBUG_STR L"-debug"
#elif defined(ATNRELEASE)
	#define AT_VERSION_DEBUG_STR L"-profile"
#else
	#define AT_VERSION_DEBUG_STR L""
#endif

#define AT_VERSION_STR AT_WIDESTR(AT_VERSION)

#define AT_PROGRAM_NAME_STR L"Altirra"

#if defined(VD_CPU_ARM64)
	#define AT_PROGRAM_PLATFORM_STR L"/ARM64"
#elif defined(VD_CPU_AMD64)
	#define AT_PROGRAM_PLATFORM_STR L"/x64"
#else
	#define AT_PROGRAM_PLATFORM_STR L""
#endif

#if AT_VERSION_PRERELEASE
	#define	AT_VERSION_PRERELEASE_STR L" [prerelease]"
#else
	#define	AT_VERSION_PRERELEASE_STR
#endif

#define AT_FULL_VERSION_STR AT_PROGRAM_NAME_STR AT_PROGRAM_PLATFORM_STR L" " AT_VERSION_STR AT_VERSION_DEBUG_STR AT_VERSION_PRERELEASE_STR

#endif
