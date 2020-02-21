//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include "gtia.h"
#include "gtiatables.h"
#include "gtiarenderer.h"

using namespace ATGTIA;

extern const VDALIGN(16) uint8 kATAnalysisColorTable[]={
	// players
	0x1a,
	0x5a,
	0x7a,
	0x9a,

	// playfields
	0x03,
	0x07,
	0x0b,
	0x0f,

	// background
	0x01,

	// black
	0x00,

	// p0+p1, p2+p3
	0x3a,
	0x8a,

	// pf0+p0/p1/p0p1
	0x13,
	0x53,
	0x33,

	// pf1+p0/p1/p0p1
	0x17,
	0x57,
	0x37,

	// pf2+p2/p3/p2p3
	0x1b,
	0x5b,
	0x3b,

	// pf3+p2/p3/p2p3
	0x1f,
	0x5f,
	0x3f,
};

void ATInitGTIAPriorityTables(uint8 priorityTables[32][256]) {
	// Priority table initialization
	//
	// The priority logic in the GTIA works as follows:
	//
	//	SP0 = P0 * /(PF01*PRI23) * /(PRI2*PF23)
	//	SP1 = P1 * /(PF01*PRI23) * /(PRI2*PF23) * (/P0 + MULTI)
	//	SP2 = P2 * /P01 * /(PF23*PRI12) * /(PF01*/PRI0)
	//	SP3 = P3 * /P01 * /(PF23*PRI12) * /(PF01*/PRI0) * (/P2 + MULTI)
	//	SF0 = PF0 * /(P23*PRI0) * /(P01*PRI01) * /SF3
	//	SF1 = PF1 * /(P23*PRI0) * /(P01*PRI01) * /SF3
	//	SF2 = PF2 * /(P23*PRI03) * /(P01*/PRI2) * /SF3
	//	SF3 = PF3 * /(P23*PRI03) * /(P01*/PRI2)
	//	SB  = /P01 * /P23 * /PF01 * /PF23
	//
	// Normally, both players and missiles contribute to P0-P3. If fifth player enable
	// is set, missiles 0-3 contribute to PF3 instead of P0-P3.
	//
	// There are a couple of notable anomalies in the above:
	//
	//	* When all priority bits are zero, the result is NOT all black as
	//	  the hardware manual implies. In fact, priority breaks and players
	//	  0-1 and playfields 0-1 and 3 can mix.
	//
	//	* The fifth player always has priority over all playfields.
	//
	// The result is that there are 24 colors possible:
	//
	//	* black
	//	* BAK
	//	* P0 - P3
	//	* PF0 - PF3
	//	* P0 | P1
	//	* P2 | P3
	//	* PF0 | P0
	//	* PF0 | P1
	//	* PF0 | P0 | P1
	//	* PF1 | P0
	//	* PF1 | P1
	//	* PF1 | P0 | P1
	//	* PF2 | P2
	//	* PF2 | P3
	//	* PF2 | P2 | P3
	//	* PF3 | P2
	//	* PF3 | P3
	//	* PF3 | P2 | P3
	//
	// A maximum of 23 of these can be accessed at a time, via a PRIOR setting of
	// xxx10000. xxx00000 gets to 17, and the rest of the illegal modes access
	// either 10 or 12 through the addition of black.

	memset(priorityTables, 0, 32*256);
	
	for(int prior=0; prior<32; ++prior) {
		const bool multi = (prior & 16) != 0;
		const bool pri0 = (prior & 1) != 0;
		const bool pri1 = (prior & 2) != 0;
		const bool pri2 = (prior & 4) != 0;
		const bool pri3 = (prior & 8) != 0;
		const bool pri01 = pri0 | pri1;
		const bool pri12 = pri1 | pri2;
		const bool pri23 = pri2 | pri3;
		const bool pri03 = pri0 | pri3;

		for(int i=0; i<256; ++i) {
			// The way the ANx decode logic works in GTIA, there is no possibility of any
			// conflict between playfield bits except for PF3, which can conflict due to
			// being reused for the fifth player. Therefore, we remap the table so that
			// any conflicts are resolved in favor of the higher playfield.
			static const uint8 kPlayfieldPriorityTable[8]={
				0,
				1,
				2,
				2,
				4,
				4,
				4,
				4,
			};

			const uint8 v = kPlayfieldPriorityTable[i & 7];

			const bool pf0 = (v & 1) != 0;
			const bool pf1 = (v & 2) != 0;
			const bool pf2 = (v & 4) != 0;
			const bool pf3 = (i & 8) != 0;
			const bool p0 = (i & 16) != 0;
			const bool p1 = (i & 32) != 0;
			const bool p2 = (i & 64) != 0;
			const bool p3 = (i & 128) != 0;

			const bool p01 = p0 | p1;
			const bool p23 = p2 | p3;
			const bool pf01 = pf0 | pf1;
			const bool pf23 = pf2 | pf3;

			const bool sp0 = p0 & !(pf01 & pri23) & !(pri2 & pf23);
			const bool sp1 = p1 & !(pf01 & pri23) & !(pri2 & pf23) & (!p0 | multi);
			const bool sp2 = p2 & !p01 & !(pf23 & pri12) & !(pf01 & !pri0);
			const bool sp3 = p3 & !p01 & !(pf23 & pri12) & !(pf01 & !pri0) & (!p2 | multi);

			const bool sf3 = pf3 & !(p23 & pri03) & !(p01 & !pri2);
			const bool sf2 = pf2 & !(p23 & pri03) & !(p01 & !pri2) & !sf3;
			const bool sf1 = pf1 & !(p23 & pri0) & !(p01 & pri01) & !sf3;
			const bool sf0 = pf0 & !(p23 & pri0) & !(p01 & pri01) & !sf3;

			const bool sb = !p01 & !p23 & !pf01 & !pf23;

			int out = 0;
			if (sf0) out += 0x001;
			if (sf1) out += 0x002;
			if (sf2) out += 0x004;
			if (sf3) out += 0x008;
			if (sp0) out += 0x010;
			if (sp1) out += 0x020;
			if (sp2) out += 0x040;
			if (sp3) out += 0x080;
			if (sb ) out += 0x100;

			uint8 c;

			switch(out) {
				default:
					VDASSERT(!"Invalid priority table decode detected.");
				case 0x000:		c = kColorBlack;	break;
				case 0x001:		c = kColorPF0;		break;
				case 0x002:		c = kColorPF1;		break;
				case 0x004:		c = kColorPF2;		break;
				case 0x008:		c = kColorPF3;		break;
				case 0x010:		c = kColorP0;		break;
				case 0x011:		c = kColorPF0P0;	break;
				case 0x012:		c = kColorPF1P0;	break;
				case 0x020:		c = kColorP1;		break;
				case 0x021:		c = kColorPF0P1;	break;
				case 0x022:		c = kColorPF1P1;	break;
				case 0x030:		c = kColorP0P1;		break;
				case 0x031:		c = kColorPF0P0P1;	break;
				case 0x032:		c = kColorPF1P0P1;	break;
				case 0x040:		c = kColorP2;		break;
				case 0x044:		c = kColorPF2P2;	break;
				case 0x048:		c = kColorPF3P2;	break;
				case 0x080:		c = kColorP3;		break;
				case 0x084:		c = kColorPF2P3;	break;
				case 0x088:		c = kColorPF3P3;	break;
				case 0x0c0:		c = kColorP2P3;		break;
				case 0x0c4:		c = kColorPF2P2P3;	break;
				case 0x0c8:		c = kColorPF3P2P3;	break;
				case 0x100:		c = kColorBAK;		break;
			}

			priorityTables[prior][i] = c;
		}
	}
}

void ATComputeLumaRamp(ATLumaRampMode mode, float lumaRamp[16]) {
	if (mode == kATLumaRampMode_Linear) {
		for(int i=0; i<16; ++i)
			lumaRamp[i] = (float)i / 15.0f;

		return;
	}

	// Empirically determined resistor bank outputs, based on linear least
	// squares of luma output from 800XL:
	//
	//   14161
	//   7553
    //   3941
    //   1808
	//
	// Doesn't quite match the 4.7K/9.1K/18K/36K resistor set in the schematic.
	// There doesn't seem to be noticeable non-linearity in the output as
	// the CGIA doc would suggest, but the short step between levels 7 and 8
	// is definitely visible.

	const float kAltRamp[16]={
		0.0f,
		0.0658340f,
		0.1435022f,
		0.2093362f,
		0.2750246f,
		0.3408586f,
		0.4185267f,
		0.4843608f,
		0.5156392f,
		0.5814733f,
		0.6591414f,
		0.7249754f,
		0.7906638f,
		0.8564978f,
		0.9341660f,
		1.0f
 	};

	memcpy(lumaRamp, kAltRamp, sizeof(float)*16);
}
