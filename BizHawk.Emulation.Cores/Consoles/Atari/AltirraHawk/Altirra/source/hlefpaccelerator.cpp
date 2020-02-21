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
#include <vd2/system/vdalloc.h>
#include "hlefpaccelerator.h"
#include "ksyms.h"
#include "cpu.h"
#include "cpuhookmanager.h"
#include "decmath.h"

class ATHLEFPAcceleratorBase {
public:
	ATHLEFPAcceleratorBase()
		: mpCPU(NULL)
	{
	}

	ATCPUEmulator *mpCPU;

	typedef void FpAccelFn(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);

	template<FpAccelFn& T_Fn>
	uint8 OnFpHook(uint16) {
		T_Fn(*mpCPU, *mpCPU->GetMemory());
		return 0x60;
	}
};

static const struct {
	uint16 mPC;
	uint8 (ATHLEFPAcceleratorBase::*mpMethod)(uint16);
} kATHLEFPHookMethods[]={

#define AT_ACCEL_FP_TABLE_ENTRY(name) { ATKernelSymbols::name, &ATHLEFPAcceleratorBase::OnFpHook<ATAccel##name> }
	AT_ACCEL_FP_TABLE_ENTRY(AFP),
	AT_ACCEL_FP_TABLE_ENTRY(FASC),
	AT_ACCEL_FP_TABLE_ENTRY(IPF),
	AT_ACCEL_FP_TABLE_ENTRY(FPI),
	AT_ACCEL_FP_TABLE_ENTRY(FADD),
	AT_ACCEL_FP_TABLE_ENTRY(FSUB),
	AT_ACCEL_FP_TABLE_ENTRY(FMUL),
	AT_ACCEL_FP_TABLE_ENTRY(FDIV),
	AT_ACCEL_FP_TABLE_ENTRY(LOG),
	AT_ACCEL_FP_TABLE_ENTRY(LOG10),
	AT_ACCEL_FP_TABLE_ENTRY(EXP),
	AT_ACCEL_FP_TABLE_ENTRY(EXP10),
	AT_ACCEL_FP_TABLE_ENTRY(SKPSPC),
	AT_ACCEL_FP_TABLE_ENTRY(ISDIGT),
	AT_ACCEL_FP_TABLE_ENTRY(NORMALIZE),
	AT_ACCEL_FP_TABLE_ENTRY(PLYEVL),
	AT_ACCEL_FP_TABLE_ENTRY(ZFR0),
	AT_ACCEL_FP_TABLE_ENTRY(ZF1),
	AT_ACCEL_FP_TABLE_ENTRY(ZFL),
	AT_ACCEL_FP_TABLE_ENTRY(LDBUFA),
	AT_ACCEL_FP_TABLE_ENTRY(FLD0R),
	AT_ACCEL_FP_TABLE_ENTRY(FLD0P),
	AT_ACCEL_FP_TABLE_ENTRY(FLD1R),
	AT_ACCEL_FP_TABLE_ENTRY(FLD1P),
	AT_ACCEL_FP_TABLE_ENTRY(FST0R),
	AT_ACCEL_FP_TABLE_ENTRY(FST0P),
	AT_ACCEL_FP_TABLE_ENTRY(FMOVE),
	AT_ACCEL_FP_TABLE_ENTRY(REDRNG),
#undef AT_ACCEL_FP_TABLE_ENTRY

};

class ATHLEFPAccelerator : public ATHLEFPAcceleratorBase {
	ATHLEFPAccelerator(const ATHLEFPAccelerator&);
	ATHLEFPAccelerator& operator=(const ATHLEFPAccelerator&);
public:
	ATHLEFPAccelerator();
	~ATHLEFPAccelerator();

	void Init(ATCPUEmulator *cpu);
	void Shutdown();

private:
	void OnHook(uint16 pc);

	ATCPUHookNode *mpHookNodes[vdcountof(kATHLEFPHookMethods)];
};

ATHLEFPAccelerator::ATHLEFPAccelerator() {
	std::fill(mpHookNodes, mpHookNodes + vdcountof(mpHookNodes), (ATCPUHookNode *)NULL);
}

ATHLEFPAccelerator::~ATHLEFPAccelerator() {
	Shutdown();
}

void ATHLEFPAccelerator::Init(ATCPUEmulator *cpu) {
	mpCPU = cpu;

	ATCPUHookManager& hookMgr = *mpCPU->GetHookManager();
	for(size_t i=0; i<vdcountof(mpHookNodes); ++i) {
		hookMgr.SetHookMethod(mpHookNodes[i], kATCPUHookMode_MathPackROMOnly, kATHLEFPHookMethods[i].mPC, 0, this, kATHLEFPHookMethods[i].mpMethod);
	}
}

void ATHLEFPAccelerator::Shutdown() {
	if (mpCPU) {
		ATCPUHookManager& hookMgr = *mpCPU->GetHookManager();

		for(size_t i=0; i<vdcountof(mpHookNodes); ++i) {
			hookMgr.UnsetHook(mpHookNodes[i]);
		}

		mpCPU = NULL;
	}
}

ATHLEFPAccelerator *ATCreateHLEFPAccelerator(ATCPUEmulator *cpu) {
	vdautoptr<ATHLEFPAccelerator> accel(new ATHLEFPAccelerator);

	accel->Init(cpu);

	return accel.release();
}

void ATDestroyHLEFPAccelerator(ATHLEFPAccelerator *accel) {
	delete accel;
}
