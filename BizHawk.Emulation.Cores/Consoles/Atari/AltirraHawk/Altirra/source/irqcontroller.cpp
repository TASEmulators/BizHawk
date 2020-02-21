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
#include "irqcontroller.h"
#include "cpu.h"

ATIRQController::ATIRQController()
	: mActiveIRQs(0)
	, mFreeCustomIRQs(0xFFFF0000)
	, mpCPU(NULL)
{
}

ATIRQController::~ATIRQController() {
	VDASSERT(mFreeCustomIRQs == 0xFFFF0000);
}

void ATIRQController::Init(ATCPUEmulator *cpu) {
	mpCPU = cpu;
}

void ATIRQController::ColdReset() {
	mActiveIRQs = 0;
}

uint32 ATIRQController::AllocateIRQ() {
	VDASSERT(mFreeCustomIRQs);

	uint32 allocBit = mFreeCustomIRQs & (0 - mFreeCustomIRQs);

	mFreeCustomIRQs -= allocBit;

	return allocBit;
}

void ATIRQController::FreeIRQ(uint32 irqbit) {
	VDASSERT(irqbit >= 0x10000 && !(mFreeCustomIRQs & irqbit));

	mFreeCustomIRQs += irqbit;
}

void ATIRQController::Assert(uint32 sources, bool cpuBased)
{
	uint32 oldFlags = mActiveIRQs;

	mActiveIRQs |= sources;

	if (!oldFlags)
		mpCPU->AssertIRQ(cpuBased ? 0 : -1);
}

void ATIRQController::Negate(uint32 sources, bool cpuBased)
{
	uint32 oldFlags = mActiveIRQs;

	mActiveIRQs &= ~sources;

	if (oldFlags && !mActiveIRQs)
		mpCPU->NegateIRQ();
}
