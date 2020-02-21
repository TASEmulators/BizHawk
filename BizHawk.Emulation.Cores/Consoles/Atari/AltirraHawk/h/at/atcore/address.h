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

#ifndef f_AT_ATCORE_ADDRESS_H
#define f_AT_ATCORE_ADDRESS_H

enum ATAddressSpace : uint32 {
	// CPU view of address space. 0000-FFFF is 6502 space, 010000-FFFFFF
	// is 65C816 space.
	kATAddressSpace_CPU		= 0x00000000,

	// ANTIC view of address space (0000-FFFF).
	kATAddressSpace_ANTIC	= 0x10000000,

	// VBXE local memory (00000-7FFFF).
	kATAddressSpace_VBXE	= 0x20000000,

	// PORTB extended memory, hardware view (00000-FFFFF).
	kATAddressSpace_EXTRAM	= 0x30000000,

	// Main memory, hardware view (0000-FFFF).
	kATAddressSpace_RAM		= 0x40000000,

	// Firmware ROM
	//	5000-57FF self test
	//	A000-BFFF BASIC
	//	C000-CFFF low OS ROM
	//	D800-FFFF high OS ROM
	//	F800-FFFF 5200 OS ROM
	kATAddressSpace_ROM		= 0x50000000,

	// Cartridge ROM (linear)
	kATAddressSpace_CART	= 0x60000000,

	// PORTB extended memory, natural view ({00-FF}4000-7FFF)
	kATAddressSpace_PORTB	= 0x70000000,

	// Cartridge ROM (banked)
	kATAddressSpace_CB		= 0x80000000,

	kATAddressOffsetMask	= 0x00FFFFFF,
	kATAddressSpaceMask		= 0xF0000000
};

const char *ATAddressGetSpacePrefix(uint32 addr);
uint32 ATAddressGetSpaceSize(uint32 addr);

#endif
