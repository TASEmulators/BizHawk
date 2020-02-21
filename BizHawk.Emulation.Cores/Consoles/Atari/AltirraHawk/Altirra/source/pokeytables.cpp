//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#include "pokeytables.h"

ATPokeyTables::ATPokeyTables() {
	// The 4-bit and 5-bit polynomial counters are of the XNOR variety, which means
	// that the all-1s case causes a lockup. The INIT mode shifts zeroes into the
	// register.

	int poly4 = 0;
	for(int i=0; i<131071; ++i) {
		poly4 = (poly4+poly4) + (~((poly4 >> 2) ^ (poly4 >> 3)) & 1);

		mPolyBuffer[i] = (poly4 & 1) << 3;
	}

	int poly5 = 0;
	for(int i=0; i<131071; ++i) {
		poly5 = (poly5+poly5) + (~((poly5 >> 2) ^ (poly5 >> 4)) & 1);

		mPolyBuffer[i] |= (poly5 & 1) << 2;
	}

	// The 17-bit polynomial counter is also of the XNOR variety, but one big
	// difference is that you're allowed to read out 8 bits of it. The RANDOM
	// register actually reports the INVERTED state of these bits (the Q' output
	// of the flip flops is connected to the data bus). This means that even
	// though we clear the register to 0, it reads as FF.
	//
	// From the perspective of the CPU through RANDOM, the LFSR shifts to the
	// right, and new bits appear on the left. The equivalent operation for the
	// 9-bit LFSR would be to set carry equal to (D0 ^ D5) and then execute
	// a ROR.
	int poly9 = 0;
	for(int i=0; i<131071; ++i) {
		// Note: This one is actually verified against a real Atari.
		// At WSYNC time, the pattern goes: 00 DF EE 16 B9....
		poly9 = (poly9 >> 1) + (~((poly9 << 8) ^ (poly9 << 3)) & 0x100);

		mPolyBuffer[i] |= (poly9 & 1) << 1;
	}

	// The 17-bit mode inserts an additional 8 register bits immediately after
	// the XNOR. The generator polynomial is unchanged.
	int poly17 = 0;
	for(int i=0; i<131071; ++i) {
		poly17 = (poly17 >> 1) + (~((poly17 << 16) ^ (poly17 << 11)) & 0x10000);

		mPolyBuffer[i] |= (poly17 >> 8) & 1;
	}

	memcpy(mPolyBuffer + 131071, mPolyBuffer, 131071);

	memset(mInitModeBuffer, 0xFF, sizeof mInitModeBuffer);
}
