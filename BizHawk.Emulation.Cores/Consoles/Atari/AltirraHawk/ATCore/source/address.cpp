//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/address.h>

const char *ATAddressGetSpacePrefix(uint32 addr) {
	static constexpr const char *kPrefixes[]={
		"",			// CPU view
		"n:",		// ANTIC view
		"v:",		// VBXE
		"x:",		// PORTB
		"r:",		// RAM
		"rom:",		// ROM
		"cart:",	// Cartridge
		"",
		"t:",
		"",
		"",
		"",
		"",
		"",
		"",
		"",
	};

	static_assert(vdcountof(kPrefixes) == 16);

	return kPrefixes[addr >> 28];
}

uint32 ATAddressGetSpaceSize(uint32 addr) {
	static constexpr uint32 kLimits[] = {
		0x1000000,	// CPU
		0x10000,	// ANTIC
		0x80000,	// VBXE
		0x1000000,	// PORTB
		0x10000,	// RAM
		0x10000,	// ROM
		0x10000000,	// Cartridge
		0x1000000,	// Cartridge (banked)
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
	};

	static_assert(vdcountof(kLimits) == 16);

	return kLimits[addr >> 28];
}
