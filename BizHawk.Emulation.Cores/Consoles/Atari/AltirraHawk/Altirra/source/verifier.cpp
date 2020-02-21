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
#include "cpu.h"
#include "console.h"
#include "verifier.h"
#include "simulator.h"
#include "simeventmanager.h"
#include "ksyms.h"

ATCPUVerifier::ATCPUVerifier() {
}

ATCPUVerifier::~ATCPUVerifier() {
	Shutdown();
}

void ATCPUVerifier::Init(ATCPUEmulator *cpu, ATCPUEmulatorMemory *mem, ATSimulator *sim, ATSimulatorEventManager *simevmgr) {
	mpCPU = cpu;
	mpMemory = mem;
	mpSimulator = sim;
	mpSimEventMgr = simevmgr;

	mEventCallbackId = mpSimEventMgr->AddEventCallback(kATSimEvent_AbnormalDMA, [this] { OnAbnormalDMA(); });

	OnReset();
	ResetAllowedTargets();
}

void ATCPUVerifier::Shutdown() {
	if (mpSimEventMgr) {
		if (mEventCallbackId) {
			mpSimEventMgr->RemoveEventCallback(mEventCallbackId);
			mEventCallbackId = 0;
		}

		mpSimEventMgr = NULL;
	}
}

void ATCPUVerifier::SetFlags(uint32 flags) {
	if (flags == mFlags)
		return;

	uint32 disabledFlags = mFlags & ~flags;
	mFlags = flags;

	if (disabledFlags & kATVerifierFlag_RecursiveNMI)
		mbInNMIRoutine = false;

	if (disabledFlags & kATVerifierFlag_UndocumentedKernelEntry)
		ResetAllowedTargets();

	if (disabledFlags & kATVerifierFlag_InterruptRegs)
		memset(mStackRegState, 0, sizeof mStackRegState);
}

void ATCPUVerifier::AddAllowedTarget(uint32 addr) {
	Addresses::iterator it(std::lower_bound(mAllowedTargets.begin(), mAllowedTargets.end(), addr));

	if (it == mAllowedTargets.end() || *it != addr)
		mAllowedTargets.insert(it, addr);
}

void ATCPUVerifier::RemoveAllowedTarget(uint32 addr) {
	Addresses::iterator it(std::lower_bound(mAllowedTargets.begin(), mAllowedTargets.end(), addr));

	if (it != mAllowedTargets.end() && *it == addr)
		mAllowedTargets.erase(it);
}

void ATCPUVerifier::RemoveAllowedTargets() {
	mAllowedTargets.clear();
}

void ATCPUVerifier::ResetAllowedTargets() {
	using namespace ATKernelSymbols;
	static const uint16 kDefaultTargets[]={
		// math pack (+Atari BASIC vectors)
		AFP,
		FASC,
		IPF,
		FPI,
		ZFR0,
		ZF1,
		ZFL,
		LDBUFA,
		FADD,
		FSUB,
		FMUL,
		FDIV,
		SKPSPC,
		ISDIGT,
		NORMALIZE,
		PLYEVL,
		FLD0R,
		FLD0P,
		FLD1R,
		FLD1P,
		FST0R,
		FST0P,
		FMOVE,
		EXP,
		EXP10,
		REDRNG,
		LOG,
		LOG10,

		// initialization vectors for E:/S:/K:/P:/C:
		0xE40C,
		0xE41C,
		0xE42C,
		0xE43C,
		0xE44C,

		// standard vectors
		DISKIV,
		DSKINV,
		CIOV,
		SIOV,
		SETVBV,
		SYSVBV,
		XITVBV,
		SIOINV,
		SENDEV,
		INTINV,
		CIOINV,
		BLKBDV,
		WARMSV,
		COLDSV,
		RBLOKV,
		CSOPIV,
		PUPDIV,
		SLFTSV,
		PENTV,
		PHUNLV,
		PHINIV
	};

	mAllowedTargets.assign(kDefaultTargets, kDefaultTargets + sizeof(kDefaultTargets)/sizeof(kDefaultTargets[0]));
	std::sort(mAllowedTargets.begin(), mAllowedTargets.end());
}

void ATCPUVerifier::GetAllowedTargets(vdfastvector<uint16>& exceptions) {
	exceptions = mAllowedTargets;
}

void ATCPUVerifier::OnReset() {
	mbInNMIRoutine = false;
		memset(mStackRegState, 0, sizeof mStackRegState);
}

void ATCPUVerifier::OnIRQEntry() {
	if (mFlags & kATVerifierFlag_InterruptRegs) {
		StackRegState& rs = mStackRegState[mpCPU->GetS()];
		rs.mA = mpCPU->GetA();
		rs.mX = mpCPU->GetX();
		rs.mY = mpCPU->GetY();
		rs.mbActive = true;
		rs.mPC = mpCPU->GetInsnPC();
		rs.mPad2 = 0;
	}
}

void ATCPUVerifier::OnNMIEntry() {
	if (mFlags & kATVerifierFlag_InterruptRegs) {
		StackRegState& rs = mStackRegState[mpCPU->GetS()];
		rs.mA = mpCPU->GetA();
		rs.mX = mpCPU->GetX();
		rs.mY = mpCPU->GetY();
		rs.mbActive = true;
		rs.mPC = mpCPU->GetInsnPC();
		rs.mPad2 = 0;
	}

	if (!(mFlags & kATVerifierFlag_RecursiveNMI))
		return;

	if (mbInNMIRoutine) {
		ATConsolePrintf("\n");
		ATConsolePrintf("VERIFIER: Recursive NMI handler execution detected.\n");
		ATConsolePrintf("          PC: %04X\n", mpCPU->GetPC());
		ATConsolePrintf("\n");
		mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
	} else {
		mNMIStackLevel = mpCPU->GetS();
		mbInNMIRoutine = true;
	}
}

void ATCPUVerifier::VerifyInsn(const ATCPUEmulator& cpu, uint8 opcode, uint16 target) {
	switch(opcode) {
		case 0xA5:	// LDA dp
		case 0xA6:	// LDX dp
		case 0xA4:	// LDY dp
			if (mFlags & kATVerifierFlag_AddressZero) {
				if (target == 0) {
					ATConsolePrintf("\n");
					ATConsolePrintf("VERIFIER: Possibly incorrect direct load of absolute address zero instead of immediate zero.\n");
					ATConsolePrintf("          PC: %04X   Fault address: %04X\n", mpCPU->GetPC(), target);
					ATConsolePrintf("\n");
					mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
				}
			}
			break;

		case 0x20:	// JSR abs
		case 0x4C:	// JMP abs
			if (mFlags & kATVerifierFlag_UndocumentedKernelEntry) {
				uint16 pc = mpCPU->GetInsnPC();

				// we only care if the target is in kernel ROM space
				if (!mpSimulator->IsKernelROMLocation(target))
					return;

				// ignore jumps from kernel ROM
				if (mpSimulator->IsKernelROMLocation(pc))
					return;

				// check for an allowed target
				if (std::binary_search(mAllowedTargets.begin(), mAllowedTargets.end(), target))
					return;

				// trip a verifier failure
				ATConsolePrintf("\n");
				ATConsolePrintf("VERIFIER: Invalid jump into kernel ROM space detected.\n");
				ATConsolePrintf("          PC: %04X   Fault address: %04X\n", pc, target);
				ATConsolePrintf("\n");
				mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
			}

			if (mFlags & kATVerifierFlag_CallingConventionViolations) {
				if (target == ATKernelSymbols::SIOV && mpSimulator->IsKernelROMLocation(target)) {
					if (mpCPU->GetP() & AT6502::kFlagI) {
						ATConsolePrintf("\n");
						ATConsolePrintf("VERIFIER: OS calling convention violation -- calling into SIO with I flag set (will hang)\n");
						ATConsolePrintf("          PC: %04X   Fault address: %04X\n", mpCPU->GetPC(), target);
						ATConsolePrintf("\n");
						mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
					}
				}
			}

			if (mFlags & kATVerifierFlag_LoadingOverDisplayList) {
				if (target == ATKernelSymbols::SIOV && mpSimulator->IsKernelROMLocation(target)) {
					uint8 dir = mpSimulator->DebugReadByte(ATKernelSymbols::DSTATS);

					if (dir & 0x40) {
						const ATAnticEmulator& antic = mpSimulator->GetAntic();

						uint16 ptr = mpSimulator->DebugReadWord(ATKernelSymbols::DBUFLO);
						uint16 len = mpSimulator->DebugReadWord(ATKernelSymbols::DBYTLO);
						uint16 dlist = antic.GetDisplayListPtr();

						if (antic.IsDisplayListEnabled() && ((dlist - ptr) & 0xffff) < len) {
							ATConsolePrintf("\n");
							ATConsolePrintf("VERIFIER: Loading over active display list.\n");
							ATConsolePrintf("          PC: $%04X   Read range: $%04X-%04X  DLIST: $%04X\n", mpCPU->GetPC(), ptr, ptr + len - 1, dlist);
							ATConsolePrintf("\n");
						}
					}
				}
			}
			break;

		case 0x40:	// RTI
			if (mFlags & kATVerifierFlag_InterruptRegs) {
				StackRegState& rs = mStackRegState[mpCPU->GetS()];
				uint8 a = mpCPU->GetA();
				uint8 x = mpCPU->GetX();
				uint8 y = mpCPU->GetY();

				if (rs.mbActive) {
					rs.mbActive = false;

					if (rs.mA != a || rs.mX != x || rs.mY != y) {
						ATConsolePrintf("\n");
						ATConsolePrintf("VERIFIER: Register mismatch between interrupt handler entry and exit.\n");
						ATConsolePrintf("          Entry: PC=%04x  A=%02x X=%02x Y=%02x\n", rs.mPC, rs.mA, rs.mX, rs.mY);
						ATConsolePrintf("          Exit:  PC=%04x  A=%02x X=%02x Y=%02x\n", mpCPU->GetInsnPC(), a, x, y);
						mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
						return;
					}
				}
			}

			if (mFlags & kATVerifierFlag_RecursiveNMI) {
				if (mbInNMIRoutine) {
					uint8 s = mpCPU->GetS();

					if ((uint8)(s - mNMIStackLevel) < 8)
						mbInNMIRoutine = false;
				}
			}
			break;

		case 0x1D:	// ORA abs,X
		case 0x1E:	// ASL abs,X
		case 0x3D:	// AND abs,X
		case 0x3E:	// ROL abs,X
		case 0x5D:	// EOR abs,X
		case 0x5E:	// LSR abs,X
		case 0x7D:	// ADC abs,X
		case 0x7E:	// ROR abs,X
		case 0x9D:	// STA abs,X
		case 0xBC:	// LDY abs,X
		case 0xBD:	// LDA abs,X
		case 0xDD:	// CMP abs,X
		case 0xDE:	// DEC abs,X
		case 0xFD:	// SBC abs,X
		case 0xFE:	// INC abs,X
			if (mFlags & kATVerifierFlag_64KWrap) {
				if (target < mpCPU->GetX()) {
					ATConsolePrintf("\n");
					ATConsolePrintf("VERIFIER: 64K address space wrap detected on abs,X indexing mode.\n");
					ATConsolePrintf("          PC=%04x  X=%02x  Target=%04x\n", mpCPU->GetInsnPC(), mpCPU->GetX(), target);
					mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
					return;
				}
			}
			break;

		case 0x19:	// ORA abs,Y
		case 0x39:	// AND abs,Y
		case 0x59:	// EOR abs,Y
		case 0x79:	// ADC abs,Y
		case 0x99:	// STA abs,Y
		case 0xB9:	// LDA abs,Y
		case 0xBE:	// LDX abs,Y
		case 0xD9:	// CMP abs,Y
		case 0xF9:	// SBC abs,Y
			if (mFlags & kATVerifierFlag_64KWrap) {
				if (target < mpCPU->GetY()) {
					ATConsolePrintf("\n");
					ATConsolePrintf("VERIFIER: 64K address space wrap detected on abs,Y indexing mode.\n");
					ATConsolePrintf("          PC=%04x  Y=%02x  Target=%04x\n", mpCPU->GetInsnPC(), mpCPU->GetY(), target);
					mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
					return;
				}
			}
			break;

		case 0x11:	// ORA (zp),Y
		case 0x31:	// AND (zp),Y
		case 0x51:	// EOR (zp),Y
		case 0x71:	// ADC (zp),Y
		case 0x91:	// STA (zp),Y
		case 0xB1:	// LDA (zp),Y
		case 0xD1:	// CMP (zp),Yn
		case 0xF1:	// SBC (zp),Y
			if (mFlags & kATVerifierFlag_64KWrap) {
				if (target < mpCPU->GetY()) {
					ATConsolePrintf("\n");
					ATConsolePrintf("VERIFIER: 64K address space wrap detected on (zp),Y indexing mode.\n");
					ATConsolePrintf("          PC=%04x  Y=%02x  Target=%04x\n", mpCPU->GetInsnPC(), mpCPU->GetY(), target);
					mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
					return;
				}
			}
			break;
	}
}

void ATCPUVerifier::OnAbnormalDMA() {
	if (mFlags & kATVerifierFlag_AbnormalDMA) {
		ATConsolePrintf("\n");
		ATConsolePrintf("VERIFIER: Abnormal playfield DMA detected.\n");
		mpSimEventMgr->NotifyEvent(kATSimEvent_VerifierFailure);
	}
}
