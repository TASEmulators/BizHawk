//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
//	CPU emulation library - execution state
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

#ifndef AT_ATCPU_EXECSTATE_H
#define AT_ATCPU_EXECSTATE_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>

// 6502/65C02/65802/65816
struct ATCPUExecState6502 {
	uint16	mPC;
	uint8	mA;
	uint8	mX;
	uint8	mY;
	uint8	mS;
	uint8	mP;

	// 65802/65816 only
	uint8	mAH;
	uint8	mXH;
	uint8	mYH;
	uint8	mSH;
	uint8	mB;
	uint8	mK;
	uint16	mDP;

	bool	mbEmulationFlag;
	bool	mbAtInsnStep;
};

struct ATCPUExecStateZ80 {
	uint16	mPC;
	uint8	mA;
	uint8	mF;
	uint8	mB;
	uint8	mC;
	uint8	mD;
	uint8	mE;
	uint8	mH;
	uint8	mL;
	uint8	mAltA;
	uint8	mAltF;
	uint8	mAltB;
	uint8	mAltC;
	uint8	mAltD;
	uint8	mAltE;
	uint8	mAltH;
	uint8	mAltL;
	uint8	mR;
	uint8	mI;
	uint16	mIX;
	uint16	mIY;
	uint16	mSP;

	bool	mbIFF1;
	bool	mbIFF2;
	bool	mbAtInsnStep;
};

struct ATCPUExecState8048 {
	uint16	mPC;
	uint8	mA;
	uint8	mPSW;
	bool	mbF1;
	bool	mbTF;
	bool	mbIF;
	uint8	mP1;
	uint8	mP2;
	uint8	mReg[2][8];
};

struct ATCPUExecState6809 {
	uint16	mPC;
	uint8	mA;
	uint8	mB;
	uint16	mX;
	uint16	mY;
	uint16	mS;
	uint16	mU;
	uint8	mCC;
	uint8	mDP;
	bool	mbAtInsnStep;
};

struct ATCPUExecState {
	union {
		ATCPUExecState6502 m6502;
		ATCPUExecStateZ80 mZ80;
		ATCPUExecState8048 m8048;
		ATCPUExecState6809 m6809;
	};
};

#endif
