//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - 65802 emulator
//	Copyright (C) 2009-2015 Avery Lee
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
#include <at/atcpu/breakpoints.h>
#include <at/atcpu/co8048.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>

ATCoProc8048::ATCoProc8048() {
	mpFnReadPort = [this](uint8, uint8 output) -> uint8 { return output; };
	mpFnWritePort = [](uint8, uint8) {};
}

void ATCoProc8048::SetProgramBanks(const void *p0, const void *p1) {
	mpProgramBanks[0] = (const uint8 *)p0;
	mpProgramBanks[1] = (const uint8 *)p1;

	mpProgramBank = mpProgramBanks[mbPBK];
}

void ATCoProc8048::SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]) {
	mpHistory = buffer;
}

void ATCoProc8048::GetExecState(ATCPUExecState& state) const {
	ATCPUExecState8048& state8048 = state.m8048;
	state8048.mPC = mPC + (mbPBK ? 0x800 : 0);
	state8048.mA = mA;
	state8048.mP1 = mP1;
	state8048.mP2 = mP2;
	memcpy(state8048.mReg[0], mRAM, 8);
	memcpy(state8048.mReg[1], mRAM + 16, 8);
}

void ATCoProc8048::SetExecState(const ATCPUExecState& state) {
	const ATCPUExecState8048& state8048 = state.m8048;

	mPC = state8048.mPC & 0x7FF;
	mbPBK = (state8048.mPC & 0x800) != 0;
	mpProgramBank = mpProgramBanks[mbPBK];

	mA = state8048.mA;
	mPSW = state8048.mPSW | 0x08;

	memcpy(mRAM, state8048.mReg[0], 8);
	memcpy(mRAM + 16, state8048.mReg[1], 8);
}

void ATCoProc8048::SetT0ReadHandler(const vdfunction<bool()>& fn) {
	mpFnReadT0 = fn;
}

void ATCoProc8048::SetT1ReadHandler(const vdfunction<bool()>& fn) {
	mpFnReadT1 = fn;
}

void ATCoProc8048::SetXRAMReadHandler(const vdfunction<uint8(uint8)>& fn) {
	mpFnReadXRAM= fn;
}

void ATCoProc8048::SetXRAMWriteHandler(const vdfunction<void(uint8, uint8)>& fn) {
	mpFnWriteXRAM = fn;
}

void ATCoProc8048::SetPortReadHandler(const vdfunction<uint8(uint8, uint8)>& fn) {
	mpFnReadPort = fn;
}

void ATCoProc8048::SetPortWriteHandler(const vdfunction<void(uint8, uint8)>& fn) {
	mpFnWritePort = fn;
}

void ATCoProc8048::SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bpHandler) {
	bool wasEnabled = (mpBreakpointMap != nullptr);
	bool nowEnabled = (bpMap != nullptr);

	mpBreakpointMap = bpMap;
	mpBreakpointHandler = bpHandler;
}

uint8 ATCoProc8048::ReadByte(uint8 addr) const {
	return mRAM[addr];
}

void ATCoProc8048::WriteByte(uint8 addr, uint8 val) {
	mRAM[addr] = val;
}

void ATCoProc8048::ColdReset() {
	memset(mRAM, 0, sizeof mRAM);
	mA = 0;
	mT = 0;

	WarmReset();
}

void ATCoProc8048::WarmReset() {
	mPSW = 0x08;
	mPC = 0;
	mbF1 = false;
	mbTF = false;
	mbIF = false;
	mbPBK = false;
	mbDBF = false;
	mbIrqEnabled = true;
	mpRegBank = &mRAM[0];
	mpProgramBank = mpProgramBanks[0];

	mbTimerActive = false;

	mP1 = 0xFF;
	mP2 = 0xFF;
	mpFnWritePort(0, 0xFF);
	mpFnWritePort(1, 0xFF);

	mTStatesLeft = 4;
}

void ATCoProc8048::AssertIrq() {
	mbIrqPending = true;

	if (mbIF)
		mbIrqAttention = true;
}

void ATCoProc8048::NegateIrq() {
	mbIrqPending = false;
}

void ATCoProc8048::Run() {
	static const uint8 kInsnBytes[256]={
//		0 1 2 3 4 5 6 7 8 9 A B C D E F
		1,1,1,2,2,1,1,1,1,1,1,1,1,1,1,1,	// 1x
		1,1,2,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 1x
		1,1,1,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 2x
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// 3x
		1,1,1,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 4x
		1,1,2,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 5x
		1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,	// 6x
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// 7x
		1,1,2,1,2,1,2,1,2,2,2,1,1,1,1,1,	// 8x
		1,1,2,1,2,1,2,1,2,2,2,1,1,1,1,1,	// 9x
		1,1,2,1,2,1,1,1,1,1,1,1,1,1,1,1,	// Ax
		2,2,2,1,2,1,2,1,2,2,2,2,2,2,2,2,	// Bx
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// Cx
		1,1,2,2,2,1,1,1,1,1,1,1,1,1,1,1,	// Dx
		1,1,2,1,2,1,2,1,2,2,2,2,2,2,2,2,	// Ex
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// Fx
	};

	static const uint8 kInsnCycles[256]={
//		0 1 2 3 4 5 6 7 8 9 A B C D E F
		1,1,2,2,2,1,1,1,2,2,2,1,1,1,1,1,	// 0x
		1,1,2,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 1x
		1,1,1,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 2x
		1,1,2,1,2,1,2,1,2,2,2,1,1,1,1,1,	// 3x
		1,1,1,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 4x
		1,1,2,2,2,1,2,1,1,1,1,1,1,1,1,1,	// 5x
		1,1,1,1,2,1,1,1,1,1,1,1,1,1,1,1,	// 6x
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// 7x
		2,2,2,2,2,1,2,1,2,2,2,1,1,1,1,1,	// 8x
		2,2,2,2,2,1,2,1,2,2,2,1,2,2,2,1,	// 9x
		2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,	// Ax
		2,2,2,2,2,1,2,1,2,2,2,2,2,2,2,2,	// Bx
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// Cx
		1,1,2,2,2,1,1,1,1,1,1,1,1,1,1,1,	// Dx
		1,1,2,2,2,1,2,1,2,2,2,2,2,2,2,2,	// Ex
		1,1,2,1,2,1,2,1,1,1,1,1,1,1,1,1,	// Fx
	};

	mCyclesLeft += mCyclesSaved;
	mCyclesSaved = 0;

	while(mCyclesLeft > 0) {
		if (mbIrqAttention) {
			mbIrqAttention = false;

			if (mbIrqEnabled) {
				if (mbIrqPending && mbIF)
					DispatchIrq();
			}
		}

		if (mbTIF && !mbTF && mbTimerActive) {
			UpdateTimer();
			if (mbTF && mbIrqEnabled)
				DispatchIrq();
		}

		const uint8 opcode = mpProgramBank[mPC];
		const uint8 insnCycles = kInsnCycles[opcode];

		mTStatesLeft = insnCycles;

		if (mCyclesLeft < mTStatesLeft) {
			mCyclesSaved = mCyclesLeft;
			mCyclesLeft = 0;
			break;
		}

		if (mpBreakpointMap && mpBreakpointMap[mPC + (mbPBK ? 0x800 : 0)]) {
			bool shouldExit = CheckBreakpoint();

			if (shouldExit) {
				goto force_exit;
			}
		}

		const uint8 operand = mpProgramBank[mPC + 1];

		if (mpHistory) {
			ATCPUHistoryEntry * VDRESTRICT he = &mpHistory[mHistoryIndex++ & 131071];

			he->mCycle = mCyclesBase - mCyclesLeft;
			he->mUnhaltedCycle = mCyclesBase - mCyclesLeft;
			he->mEA = 0xFFFFFFFFUL;
			he->mPC = mPC + (mbPBK ? 0x800 : 0);
			he->mA = mA;
			he->mP = mPSW;
			he->m8048_P1 = mP1;
			he->m8048_P2 = mP2;
			he->mExt.m8048_R0 = mpRegBank[0];
			he->mExt.m8048_R1 = mpRegBank[1];
			he->mB = 0;
			he->mK = 0;
			he->mS = ~mPSW & 0x07;

			he->mOpcode[0] = opcode;
			he->mOpcode[1] = operand;
		}

		mCyclesLeft -= insnCycles;

		mPC = (mPC + kInsnBytes[opcode]) & 0x7FF;

		switch(opcode) {
			case 0x68:		// ADD A,Rr
			case 0x69:
			case 0x6A:
			case 0x6B:
			case 0x6C:
			case 0x6D:
			case 0x6E:
			case 0x6F:
				{
					const uint8 r = mpRegBank[opcode - 0x68];
					uint32 result = (uint32)mA + r;

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x60:		// ADD A,@Rr
			case 0x61:
				{
					const uint8 r = mRAM[mpRegBank[opcode - 0x60]];
					uint32 result = (uint32)mA + r;

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x03:		// ADD A,#imm
				{
					const uint8 r = operand;
					uint32 result = (uint32)mA + r;

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x78:		// ADDC A,Rr
			case 0x79:
			case 0x7A:
			case 0x7B:
			case 0x7C:
			case 0x7D:
			case 0x7E:
			case 0x7F:
				{
					const uint8 r = mpRegBank[opcode - 0x78];
					uint32 result = (uint32)mA + r + (mPSW >> 7);

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x70:		// ADDC A,@Rr
			case 0x71:
				{
					const uint8 r = mRAM[mpRegBank[opcode - 0x60]];
					uint32 result = (uint32)mA + r + (mPSW >> 7);

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x13:		// ADDC A,#imm
				{
					const uint8 r = operand;
					uint32 result = (uint32)mA + r + (mPSW >> 7);

					mPSW &= 0x3F;
					mPSW |= (result >> 1) & 0x80;

					if ((mA ^ r ^ result) & 0x10)
						mPSW |= 0x40;

					mA = (uint8)result;
				}
				break;

			case 0x58:		// ANL A,Rr
			case 0x59:
			case 0x5A:
			case 0x5B:
			case 0x5C:
			case 0x5D:
			case 0x5E:
			case 0x5F:
				mA &= mpRegBank[opcode - 0x58];
				break;

			case 0x50:		// ANL A,@Rr
			case 0x51:
				mA &= mRAM[mpRegBank[opcode - 0x50]];
				break;

			case 0x53:		// ANL A,#data
				mA &= operand;
				break;

	//		case 0x98:		// ANL BUS,#data

			case 0x99:		// ANL P1,#data
				{
					const uint8 r = mP1 & operand;

					if (mP1 != r) {
						mP1 = r;

						mpFnWritePort(0, r);
					}
				}
				break;

			case 0x9A:		// ANL P2,#data
				{
					const uint8 r = mP2 & operand;

					if (mP2 != r) {
						mP2 = r;

						mpFnWritePort(1, r);
					}
				}
				break;

			case 0x14:		// CALL address
			case 0x34:
			case 0x54:
			case 0x74:
			case 0x94:
			case 0xB4:
			case 0xD4:
			case 0xF4:
				{
					uint8 *stackEntry = &mRAM[0x08 + (mPSW & 7) * 2];

					stackEntry[0] = (uint8)mPC;
					stackEntry[1] = (uint8)((mPC >> 8) + (mPSW & 0xF0) + (mbPBK ? 0x08 : 0x00));

					mPSW = (mPSW - 0x07) | 0x08;

					mbPBK = mbDBF;
					mPC = ((uint32)(opcode & 0xE0) << 3) + operand;
					mpProgramBank = mpProgramBanks[mbPBK];
				}
				break;

			case 0x27:		// CLR A
				mA = 0;
				break;

			case 0x97:		// CLR C
				mPSW &= 0x7F;
				break;

			case 0xA5:		// CLR F1
				mbF1 = false;
				break;

			case 0x85:		// CLR F0
				mPSW &= 0xDF;
				break;

			case 0x37:		// CPL A
				mA = ~mA;
				break;

			case 0xA7:		// CPL C
				mPSW ^= 0x80;
				break;

			case 0x95:		// CPL F0
				mPSW ^= 0x20;
				break;

			case 0xB5:		// CPL F1
				mbF1 = !mbF1;
				break;

			case 0x57:		// DA A
				{
					uint32 r = mA;

					if ((r & 0x0F) >= 0x0A || (mPSW & 0x40))
						r += 0x06;

					if (r >= 0xA0 || (mPSW & 0x80))
						r += 0x60;

					mPSW &= 0x7F;
					mPSW |= (r >> 1) & 0x80;
				}
				break;

			case 0x07:		// DEC A
				--mA;
				break;

			case 0xC8:		// DEC Rr
			case 0xC9:
			case 0xCA:
			case 0xCB:
			case 0xCC:
			case 0xCD:
			case 0xCE:
			case 0xCF:
				--mpRegBank[opcode - 0xC8];
				break;

			case 0x15:		// DIS I
				mbIF = false;
				break;

			case 0x35:		// DIS TCNTI
				mbTIF = false;
				break;

			case 0xE8:		// DJNZ Rr,address
			case 0xE9:
			case 0xEA:
			case 0xEB:
			case 0xEC:
			case 0xED:
			case 0xEE:
			case 0xEF:
				if (--mpRegBank[opcode - 0xE8])
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x05:		// EN I
				mbIF = true;
				mbIrqAttention |= mbIrqPending;
				break;

			case 0x25:		// EN TCNTI
				mbTIF = true;
				mbIrqAttention |= mbTF;
				break;

	//		case 0x08:		// IN A,BUS

			case 0x09:		// IN A,P1
				mA = mpFnReadPort(0, mP1);
				break;

			case 0x0A:		// IN A,P2
				mA = mpFnReadPort(1, mP2);
				break;

			case 0x17:		// INC A
				++mA;
				break;

			case 0x18:		// INC Rr
			case 0x19:
			case 0x1A:
			case 0x1B:
			case 0x1C:
			case 0x1D:
			case 0x1E:
			case 0x1F:
				++mpRegBank[opcode - 0x18];
				break;

			case 0x10:		// INC @Rr
			case 0x11:		// INC @Rr
				++mRAM[mpRegBank[opcode - 0x10]];
				break;

			case 0x12:		// JBb address
			case 0x32:
			case 0x52:
			case 0x72:
			case 0x92:
			case 0xB2:
			case 0xD2:
			case 0xF2:
				if (mA & (1 << ((opcode >> 5) & 7)))
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0xF6:		// JC address
				if (mPSW & 0x80)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0xB6:		// JF0 address
				if (mPSW & 0x20)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x76:		// JF1 address
				if (mbF1)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x04:		// JMP address
			case 0x24:
			case 0x44:
			case 0x64:
			case 0x84:
			case 0xA4:
			case 0xC4:
			case 0xE4:
				mbPBK = mbDBF;
				mPC = ((uint32)(opcode & 0xE0) << 3) + operand;
				mpProgramBank = mpProgramBanks[mbPBK];
				break;

			case 0xB3:		// JMPP @A
				{
					const uint32 pageBase = ((mPC - 1) & 0x700);
					mPC = pageBase + mpProgramBank[pageBase + mA];
				}
				break;

			case 0xE6:		// JNC address
				if (!(mPSW & 0x80))
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x86:		// JNI address
				if (mbIrqPending)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x26:		// JNT0 address
				if (!mpFnReadT0())
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x46:		// JNT1 address
				if (!mpFnReadT1())
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x96:		// JNZ address
				if (mA)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x16:		// JTF address
				if (!mbTF && mbTimerActive)
					UpdateTimer();

				if (mbTF) {
					mbTF = false;
					UpdateTimer();
					UpdateTimerDeadline();
					mPC = ((mPC - 1) & 0x700) + operand;
				}
				break;

			case 0x36:		// JT0 address
				if (mpFnReadT0())
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x56:		// JT1 address
				if (mpFnReadT1())
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0xC6:		// JZ address
				if (!mA)
					mPC = ((mPC - 1) & 0x700) + operand;
				break;

			case 0x23:		// MOV A,#data
				mA = operand;
				break;

			case 0xC7:		// MOV A,PSW
				mA = mPSW;
				break;

			case 0xF8:		// MOV A,Rr
			case 0xF9:
			case 0xFA:
			case 0xFB:
			case 0xFC:
			case 0xFD:
			case 0xFE:
			case 0xFF:
				mA = mpRegBank[opcode - 0xF8];
				break;

			case 0xF0:		// MOV A,@Rr
			case 0xF1:
				mA = mRAM[mpRegBank[opcode - 0xF0]];
				break;

			case 0x42:		// MOV A,T
				if (mbTimerActive)
					UpdateTimer();

				mA = mT;
				break;

			case 0xD7:		// MOV PSW,A
				mPSW = mA | 0x08;
				mpRegBank = mPSW & 0x10 ? &mRAM[0x18] : mRAM;
				break;

			case 0xA8:		// MOV Rr,A
			case 0xA9:
			case 0xAA:
			case 0xAB:
			case 0xAC:
			case 0xAD:
			case 0xAE:
			case 0xAF:
				mpRegBank[opcode - 0xA8] = mA;
				break;

			case 0xB8:		// MOV Rr,#data
			case 0xB9:
			case 0xBA:
			case 0xBB:
			case 0xBC:
			case 0xBD:
			case 0xBE:
			case 0xBF:
				mpRegBank[opcode - 0xB8] = operand;
				break;

			case 0xA0:		// MOV @Rr,A
			case 0xA1:
				mRAM[mpRegBank[opcode - 0xA0]] = mA;
				break;

			case 0xB0:		// MOV @Rr,#data
			case 0xB1:
				mRAM[mpRegBank[opcode - 0xB0]] = operand;
				break;

			case 0x62:		// MOV T,A
				mT = mA;
				if (mbTimerActive)
					UpdateTimerDeadline();
				break;

			case 0xA3:		// MOVP A,@A
				mA = mpProgramBank[((mPC - 1) & 0x700) + mA];
				break;

			case 0xE3:		// MOVP3 A,@A
				mA = mpProgramBank[0x300 + mA];
				break;

			case 0x80:		// MOVX A,@Rr
			case 0x81:
				mA = mpFnReadXRAM(mRAM[opcode - 0x90]);
				break;

			case 0x90:		// MOVX @Rr,A
			case 0x91:
				mpFnWriteXRAM(mRAM[opcode - 0x80], mA);
				break;

			case 0x00:		// NOP
				break;

			case 0x48:		// ORL A,Rr
			case 0x49:
			case 0x4A:
			case 0x4B:
			case 0x4C:
			case 0x4D:
			case 0x4E:
			case 0x4F:
				mA |= mpRegBank[opcode - 0x48];
				break;

			case 0x40:		// ORL A,@Rr
			case 0x41:
				mA |= mRAM[mpRegBank[opcode - 0x40]];
				break;

			case 0x43:		// ORL A,#data
				mA |= operand;
				break;

	//		case 0x88:		// ORL BUS,#data
			case 0x89:		// ORL P1,#data
				{
					const uint8 r = mP1 | operand;

					if (mP1 != r) {
						mP1 = r;

						mpFnWritePort(0, r);
					}
				}
				break;

			case 0x8A:		// ORL P2,#data
				{
					const uint8 r = mP2 | operand;

					if (mP2 != r) {
						mP2 = r;

						mpFnWritePort(1, r);
					}
				}
				break;

	//		case 0x38:		// OUTL BUS,A

			case 0x39:		// OUTL P1,A
				if (mP1 != mA) {
					mP1 = mA;
					mpFnWritePort(0, mA);
				}
				break;

			case 0x3A:		// OUTL P2,A
				if (mP2 != mA) {
					mP2 = mA;
					mpFnWritePort(1, mA);
				}
				break;

			case 0x83:		// RET
				{
					mPSW = (mPSW - 1) | 0x08;

					const uint8 *stackEntry = &mRAM[0x08 + (mPSW & 7)*2];

					mbPBK = (stackEntry[1] & 0x08) != 0;
					mpProgramBank = mpProgramBanks[mbPBK];

					mPC = (uint32)stackEntry[0] + (((uint32)stackEntry[1] & 0x07) << 8);
				}
				break;

			case 0x93:		// RETR
				{
					mPSW = (mPSW - 1) | 0x08;

					const uint8 *stackEntry = &mRAM[0x08 + (mPSW & 7)*2];

					mbPBK = (stackEntry[1] & 0x08) != 0;
					mpProgramBank = mpProgramBanks[mbPBK];

					mPC = (uint32)stackEntry[0] + (((uint32)stackEntry[1] & 0x07) << 8);
					mPSW &= stackEntry[1] | 0x0F;
					mpRegBank = (mPSW & 0x10) ? &mRAM[0x18] : mRAM;

					mbIrqEnabled = true;

					DispatchIrq();
				}
				break;

			case 0xE7:		// RL A
				mA = (mA + mA) + (mA >> 7);
				break;

			case 0xF7:		// RLC A
				{
					uint32 r = (uint32)mA + mA;

					mA = (uint8)(r + (mPSW >> 7));
					mPSW = (mPSW & 0x7F) + ((r >> 1) & 0x80);
				}
				break;

			case 0x77:		// RR A
				mA = (mA >> 1) + (mA << 7);
				break;

			case 0x67:		// RRC A
				{
					uint8 r = mA;

					mA = (uint8)((mA >> 1) + (mPSW & 0x80));
					mPSW = (mPSW & 0x7F) + (r << 7);
				}
				break;

			case 0xE5:		// SEL MB0
				mbDBF = false;
				break;

			case 0xF5:		// SEL MB1
				mbDBF = true;
				break;

			case 0xC5:		// SEL RB0
				mPSW &= 0xEF;
				mpRegBank = mRAM;
				break;

			case 0xD5:		// SEL RB1
				mPSW |= 0x10;
				mpRegBank = &mRAM[0x18];
				break;

			case 0x65:		// STOP TCNT
				if (mbTimerActive) {
					mbTimerActive = false;

					UpdateTimer();
				}
				break;

	//		case 0x45:		// STRT TCNT

			case 0x55:		// STRT T
				mbTimerActive = true;
				UpdateTimerDeadline();
				break;

			case 0x47:		// SWAP A
				mA = (mA << 4) + (mA >> 4);
				break;

			case 0x28:		// XCH A,Rr
			case 0x29:
			case 0x2A:
			case 0x2B:
			case 0x2C:
			case 0x2D:
			case 0x2E:
			case 0x2F:
				{
					uint8 *r = &mpRegBank[opcode - 0x28];
					uint8 t = mA;
					mA = *r;
					*r = t;
				}
				break;

			case 0x20:		// XCH A,@Rr
			case 0x21:
				{
					uint8 *r = &mRAM[mpRegBank[opcode - 0x20]];
					uint8 t = mA;
					mA = *r;
					*r = t;
				}
				break;

			case 0xD8:		// XRL A,Rr
			case 0xD9:
			case 0xDA:
			case 0xDB:
			case 0xDC:
			case 0xDD:
			case 0xDE:
			case 0xDF:
				mA ^= mpRegBank[opcode - 0xD8];
				break;

			case 0xD0:		// XRL A,@Rr
			case 0xD1:
				mA ^= mRAM[mpRegBank[opcode - 0xD0]];
				break;

			case 0xD3:		// XRL A,#data
				mA ^= operand;
				break;

			default:
				mCyclesLeft = 0;
				mPC = (mPC - 1) & 0x7FF;
				break;
		}
	}

force_exit:
	;
}

bool ATCoProc8048::CheckBreakpoint() {
	if (mpBreakpointHandler->CheckBreakpoint(mPC + (mbPBK ? 0x800 : 0))) {
		return true;
	}

	return false;
}

void ATCoProc8048::DispatchIrq() {
	if (!mbIrqEnabled)
		return;

	if (mbIF && mbIrqPending) {
		mbIrqEnabled = false;

		uint8 *stackEntry = &mRAM[0x08 + (mPSW & 7) * 2];

		stackEntry[0] = (uint8)mPC;
		stackEntry[1] = (uint8)((mPC >> 8) + (mPSW & 0xF0) + (mbPBK ? 0x08 : 0x00));

		mPSW = (mPSW - 0x07) | 0x08;

		mbPBK = false;
		mpProgramBank = mpProgramBanks[false];
		mPC = 3;
	} else if (mbTIF && mbTF) {
		mbIrqEnabled = false;

		uint8 *stackEntry = &mRAM[0x08 + (mPSW & 7) * 2];

		stackEntry[0] = (uint8)mPC;
		stackEntry[1] = (uint8)((mPC >> 8) + (mPSW & 0xF0) + (mbPBK ? 0x08 : 0x00));

		mPSW = (mPSW - 0x07) | 0x08;

		mbPBK = false;
		mpProgramBank = mpProgramBanks[false];
		mPC = 7;
	}
}

void ATCoProc8048::UpdateTimer() {
	const uint32 delta = (mCyclesBase - mCyclesLeft) - mTimerDeadline;
	mT = (uint8)(delta >> 5);

	if (!mbTF && !(delta & (UINT32_C(1) << 31)))
		mbTF = true;
}

void ATCoProc8048::UpdateTimerDeadline() {
	mTimerDeadline = ((mCyclesBase - mCyclesLeft) & ~UINT32_C(31)) + ((0x100 - (uint32)mT) << 5);
}
