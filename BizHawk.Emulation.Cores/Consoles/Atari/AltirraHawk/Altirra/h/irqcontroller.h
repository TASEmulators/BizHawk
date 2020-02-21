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

#ifndef f_AT_IRQCONTROLLER_H
#define f_AT_IRQCONTROLLER_H

class ATCPUEmulator;

enum ATIRQSource {
	kATIRQSource_POKEY = 0x01,
	kATIRQSource_VBXE = 0x02,
	kATIRQSource_PIAA1 = 0x04,
	kATIRQSource_PIAA2 = 0x08,
	kATIRQSource_PIAB1 = 0x10,
	kATIRQSource_PIAB2 = 0x20,
	kATIRQSource_PBI = 0x40
};

class ATIRQController {
public:
	ATIRQController();
	~ATIRQController();

	void Init(ATCPUEmulator *cpu);

	void ColdReset();

	uint32 AllocateIRQ();
	void FreeIRQ(uint32 irqbit);

	void Assert(uint32 sources, bool cpuBased);
	void Negate(uint32 sources, bool cpuBased);

protected:
	uint32 mActiveIRQs;
	uint32 mFreeCustomIRQs;

	ATCPUEmulator *mpCPU;
};

#endif
