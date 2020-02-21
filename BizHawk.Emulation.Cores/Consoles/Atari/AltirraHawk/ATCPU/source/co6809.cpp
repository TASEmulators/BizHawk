//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - 6809 emulator
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
#include <at/atcpu/co6809.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atcpu/states6809.h>

#define ATCP_MEMORY_CONTEXT	\
	[[maybe_unused]] uint16 tmpaddr;		\
	[[maybe_unused]] uintptr tmpbase;	\
	[[maybe_unused]] uint8 tmpval;

#define ATCP_DUMMY_READ_BYTE(addr) ((void)(0))
#define ATCP_DEBUG_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? DebugReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? ReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_WRITE_BYTE(addr, value) ((void)(tmpaddr = (addr), tmpval = (value), tmpbase = mWriteMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? WriteByteSlow(tmpbase, tmpaddr, tmpval) : (void)(*(uint8 *)(tmpbase + tmpaddr) = tmpval))))

const uint8 ATCoProc6809::kInitialState = ATCPUStates6809::k6809StateReadOpcode;
const uint8 ATCoProc6809::kInitialStateNoBreak = ATCPUStates6809::k6809StateReadOpcodeNoBreak;

ATCoProc6809::ATCoProc6809() {
	ATCPUDecoderGenerator6809 gen;
	gen.RebuildTables(mDecoderTables, false, false, false);
}

void ATCoProc6809::SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]) {
	using namespace ATCPUStates6809;

	bool historyWasOn = (mpHistory != nullptr);
	bool historyNowOn = (buffer != nullptr);

	mpHistory = buffer;

	if (historyWasOn != historyNowOn) {
		for(uint8& op : mDecoderTables.mDecodeHeap) {
			if (op == k6809StateReadOpcode || op == k6809StateReadOpcodeNoBreak)
				op = k6809StateRegenerateDecodeTables;
			else if (op == k6809StateAddToHistory)
				op = k6809StateNop;
		}
	}
}

void ATCoProc6809::SetCC(uint8 cc) {
	mCC = cc;

	if (!mbIntAttention) {
		const uint8 setBits = cc & ~mCC;

		if ((setBits & 0x40) && mbFirqAsserted)
			mbIntAttention = true;
		else if ((setBits & 0x10) && mbIrqAsserted)
			mbIntAttention = true;
	}
}

void ATCoProc6809::GetExecState(ATCPUExecState& state) const {
	state.m6809.mPC = mInsnPC;
	state.m6809.mA = mA;
	state.m6809.mX = mX;
	state.m6809.mY = mY;
	state.m6809.mS = mS;
	state.m6809.mU = mU;
	state.m6809.mCC = mCC;
	state.m6809.mDP = mDP;

	state.m6809.mbAtInsnStep = (*mpNextState == ATCPUStates6809::k6809StateReadOpcodeNoBreak);
}

void ATCoProc6809::SetExecState(const ATCPUExecState& state) {
	const ATCPUExecState6809& state6809 = state.m6809;

	if (mInsnPC != state6809.mPC) {
		mPC = state6809.mPC;
		mInsnPC = state6809.mPC;

		mpNextState = &kInitialStateNoBreak;
	}

	mA = state6809.mA;
	mX = state6809.mX;
	mY = state6809.mY;
	mS = state6809.mS;
	mU = state6809.mU;
	mCC = state6809.mCC;
	mDP = state6809.mDP;
}

void ATCoProc6809::SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bpHandler) {
	bool wasEnabled = (mpBreakpointMap != nullptr);
	bool nowEnabled = (bpMap != nullptr);

	mpBreakpointMap = bpMap;
	mpBreakpointHandler = bpHandler;

	if (wasEnabled != nowEnabled) {
		if (nowEnabled) {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates6809::k6809StateReadOpcodeNoBreak)
					op = ATCPUStates6809::k6809StateReadOpcode;
			}
		} else {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates6809::k6809StateReadOpcode)
					op = ATCPUStates6809::k6809StateReadOpcodeNoBreak;
			}

			if (mpNextState == &kInitialState)
				mpNextState = &kInitialStateNoBreak;
		}
	}
}

void ATCoProc6809::ColdReset() {
	mA = 0;
	mB = 0;
	mX = 0;
	mY = 0;
	mS = 0;
	mU = 0;
	mDP = 0;
	mPC = 0;

	WarmReset();
}

void ATCoProc6809::WarmReset() {
	ATCP_MEMORY_CONTEXT;

	mPC = ATCP_READ_BYTE(0xFFFF);
	mPC += ((uint32)ATCP_READ_BYTE(0xFFFE) << 8);

	mpNextState = mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
	mInsnPC = mPC;

	mbNmiAsserted = false;
	mbNmiArmed = false;
	mCC |= 0x50;
	mDP = 0;
}

void ATCoProc6809::AssertIrq() {
	if (!mbIrqAsserted) {
		mbIrqAsserted = true;

		if (mCC & 0x10)
			mbIntAttention = true;
	}
}

void ATCoProc6809::NegateIrq() {
	mbIrqAsserted = false;
}

void ATCoProc6809::AssertFirq() {
	if (!mbFirqAsserted) {
		mbFirqAsserted = true;

		if (mCC & 0x40)
			mbIntAttention = true;
	}
}

void ATCoProc6809::NegateFirq() {
	mbFirqAsserted = false;
}

void ATCoProc6809::AssertNmi() {
	if (!mbNmiAsserted) {
		mbNmiAsserted = true;

		if (mbNmiArmed)
			mbIntAttention = true;
	}
}

#include <co6809.inl>

inline uint8 ATCoProc6809::DebugReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpDebugRead(addr, node->mpThis);
}

inline uint8 ATCoProc6809::ReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpRead(addr, node->mpThis);
}

inline void ATCoProc6809::WriteByteSlow(uintptr base, uint32 addr, uint8 value) {
	auto node = (ATCoProcWriteMemNode *)(base - 1);

	node->mpWrite(addr, value, node->mpThis);
}

void ATCoProc6809::ArmNmi() {
	if (!mbNmiArmed) {
		mbNmiArmed = true;

		if (mbNmiAsserted)
			mbIntAttention = true;
	}
}

bool ATCoProc6809::CheckBreakpoint() {
	if (mpBreakpointHandler->CheckBreakpoint(mPC)) {
		mpNextState = &kInitialStateNoBreak;
		mInsnPC = mPC;
		return true;
	}

	return false;
}

void ATCoProc6809::RecordInterrupt(bool irq, bool nmi) {
	if (!mpHistory)
		return;

	ATCPUHistoryEntry * VDRESTRICT he = &mpHistory[mHistoryIndex++ & 131071];

	he->mCycle = mCyclesBase - mCyclesLeft;
	he->mUnhaltedCycle = mCyclesBase - mCyclesLeft;
	he->mEA = 0xFFFFFFFFUL;
	he->mPC = mPC - 1;
	he->mP = mCC;
	he->mA = mA;
	he->mExt.mAH = mB;
	he->mX = (uint8)mX;
	he->mExt.mXH = (uint8)(mX >> 8);
	he->mY = (uint8)mY;
	he->mExt.mYH = (uint8)(mY >> 8);
	he->mS = (uint8)mS;
	he->mExt.mSH = (uint8)(mS >> 8);
	he->mbIRQ = irq;
	he->mbNMI = nmi;
	he->mSubCycle = 0;
	he->mbEmulation = true;
	he->mK = mDP;
	he->mB = 0;
	he->mD = mU;
}

const uint8 *ATCoProc6809::RegenerateDecodeTables() {
	ATCPUDecoderGenerator6809 gen;
	gen.RebuildTables(mDecoderTables, false, mpHistory != nullptr, mpBreakpointMap != nullptr);

	return mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
}

