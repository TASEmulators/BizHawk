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

#include <stdafx.h>
#include "kerneldb.h"

void ATClearPokeyTimersOnDiskIo(ATKernelDatabase& kdb) {
	// Kill all audio voices.
	for(int i=0; i<4; ++i)
		kdb.AUDC1[i+i] = 0;

	// Turn off serial interrupts.
	uint8 newMask = kdb.POKMSK & 0xC7;
	kdb.POKMSK = newMask;
	kdb.IRQEN = newMask;

	// Set AUDCTL to 1.79MHz 3, use 16-bit 3+4
	kdb.AUDCTL = 0x28;
}
