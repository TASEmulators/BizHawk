//	VirtualDub - Video processing and capture application
//	Video decoding library
//	Copyright (C) 1998-2006 Avery Lee
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
#include "common_png.h"

namespace nsVDPNG {
	extern const uint8 kPNGSignature[8]={137,80,78,71,13,10,26,10};
};

void VDAdler32Checker::Process(const void *src, sint32 len) {
	const uint8 *s = (const uint8 *)src;

	while(len > 0) {
		uint32 tc = len;
		if (tc > 0x1000)
			tc = 0x1000;

		len -= tc;
		do {
			mS1 += *s++;
			mS2 += mS1;
		} while(--tc);

		mS1 %= 65521;
		mS2 %= 65521;
	}
}
