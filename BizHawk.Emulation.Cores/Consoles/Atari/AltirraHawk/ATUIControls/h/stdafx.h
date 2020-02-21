//	Altirra - Atari 800/800XL emulator
//	UI control library
//	Copyright (C) 2016-2017 Avery Lee
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

#pragma once

#pragma warning(disable: 4351)	// warning C4351: new behavior: elements of array '...' will be default initialized
#pragma warning(disable: 4355)	// warning C4355: 'this' : used in base member initializer list

#define _SCL_SECURE_NO_WARNINGS
#include <stddef.h>
#include <string.h>
#include <stdio.h>
#include <math.h>
#include <tchar.h>
#include <vd2/system/function.h>
#include <vd2/system/linearalloc.h>
#include <vd2/system/refcount.h>
#include <vd2/system/unknown.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <deque>
#include <iterator>
