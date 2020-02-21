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

#ifndef f_VD2_MEIA_COMMON_PNG_H
#define f_VD2_MEIA_COMMON_PNG_H

#include <vd2/system/vdtypes.h>

namespace nsVDPNG {
	extern const uint8 kPNGSignature[8];

	inline int PNGPaethPredictor(int a, int b, int c) {
		int p  = a + b - c;
		int pa = abs(p - a);
		int pb = abs(p - b);
		int pc = abs(p - c);

		if (pa <= pb && pa <= pc)
			return a;
		else if (pb <= pc)
			return b;
		else
			return c;
	}
};

/// Computes the Adler-32 checksum of a block of memory.
class VDAdler32Checker {
public:
	VDAdler32Checker() : mS1(1), mS2(0) {}

	void Process(const void *src, sint32 len);

	uint32 Adler32() const { return mS1 + (mS2 << 16); }

	static uint32 Adler32(const void *src, sint32 len) {
		VDAdler32Checker checker;
		checker.Process(src, len);
		return checker.Adler32();
	}

protected:
	uint32	mS1;
	uint32	mS2;
};

#endif
