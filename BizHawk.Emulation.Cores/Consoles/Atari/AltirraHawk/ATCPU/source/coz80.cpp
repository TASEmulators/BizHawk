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
#include <at/atcpu/coz80.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atcpu/statesz80.h>

#define ATCP_MEMORY_CONTEXT	\
	uint16 tmpaddr;	\
	uintptr tmpbase;	\
	uint8 tmpval;

#define ATCP_DUMMY_READ_BYTE(addr) ((void)(0))
#define ATCP_DEBUG_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? DebugReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? ReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_WRITE_BYTE(addr, value) ((void)(tmpaddr = (addr), tmpval = (value), tmpbase = mWriteMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? WriteByteSlow(tmpbase, tmpaddr, tmpval) : (void)(*(uint8 *)(tmpbase + tmpaddr) = tmpval))))

const uint8 ATCoProcZ80::kInitialState = ATCPUStatesZ80::kZ80StateReadOpcode;
const uint8 ATCoProcZ80::kInitialStateNoBreak = ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak;

ATCoProcZ80::ATCoProcZ80() {
	mpFnReadPort = [](uint8) -> uint8 { return 0xFF; };
	mpFnWritePort = [](uint8, uint8) {};
	mpFnIntVec = [] { return 0; };
	mpFnIntAck = [] {};
	mpFnHaltChange = [](bool) {};

	ATCPUDecoderGeneratorZ80 gen;
	gen.RebuildTables(mDecoderTables, false, false, false);

	mpNextState = &kInitialState;
}

void ATCoProcZ80::SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]) {
	bool historyWasOn = (mpHistory != nullptr);
	bool historyNowOn = (buffer != nullptr);

	mpHistory = buffer;

	if (historyWasOn != historyNowOn) {
		for(uint8& op : mDecoderTables.mDecodeHeap) {
			if (op == ATCPUStatesZ80::kZ80StateReadOpcode || op == ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak)
				op = ATCPUStatesZ80::kZ80StateRegenerateDecodeTables;
			else if (op == ATCPUStatesZ80::kZ80StateAddToHistory)
				op = ATCPUStatesZ80::kZ80StateNop;
		}
	}
}

void ATCoProcZ80::GetExecState(ATCPUExecState& state) const {
	ATCPUExecStateZ80& stateZ80 = state.mZ80;
	stateZ80.mPC = mInsnPC;
	stateZ80.mA = mA;
	stateZ80.mF = mF;
	stateZ80.mB = mB;
	stateZ80.mC = mC;
	stateZ80.mD = mD;
	stateZ80.mE = mE;
	stateZ80.mH = mH;
	stateZ80.mL = mL;
	stateZ80.mAltA = mAltA;
	stateZ80.mAltF = mAltF;
	stateZ80.mAltB = mAltB;
	stateZ80.mAltC = mAltC;
	stateZ80.mAltD = mAltD;
	stateZ80.mAltE = mAltE;
	stateZ80.mAltH = mAltH;
	stateZ80.mAltL = mAltL;
	stateZ80.mI = mI;
	stateZ80.mR = mR;
	stateZ80.mIX = (uint16)(((uint32)mIXH << 8) + mIXL);
	stateZ80.mIY = (uint16)(((uint32)mIYH << 8) + mIYL);
	stateZ80.mSP = mSP;
	stateZ80.mbIFF1 = mbIFF1;
	stateZ80.mbIFF2 = mbIFF2;

	stateZ80.mbAtInsnStep = (*mpNextState == ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak);
}

void ATCoProcZ80::SetExecState(const ATCPUExecState& state) {
	const ATCPUExecStateZ80& stateZ80 = state.mZ80;

	if (mInsnPC != stateZ80.mPC) {
		mPC = stateZ80.mPC;
		mInsnPC = stateZ80.mPC;

		mpNextState = &kInitialStateNoBreak;
	}

	mA = stateZ80.mA;
	mF = stateZ80.mF;
	mB = stateZ80.mB;
	mC = stateZ80.mC;
	mD = stateZ80.mD;
	mE = stateZ80.mE;
	mH = stateZ80.mH;
	mL = stateZ80.mL;
	mAltA = stateZ80.mAltA;
	mAltF = stateZ80.mAltF;
	mAltB = stateZ80.mAltB;
	mAltC = stateZ80.mAltC;
	mAltD = stateZ80.mAltD;
	mAltE = stateZ80.mAltE;
	mAltH = stateZ80.mAltH;
	mAltL = stateZ80.mAltL;
	mIXL = (uint8)stateZ80.mIX;
	mIXH = (uint8)(stateZ80.mIX >> 8);
	mIYL = (uint8)stateZ80.mIY;
	mIYH = (uint8)(stateZ80.mIY >> 8);
	mI = stateZ80.mI;
	mR = stateZ80.mR;
	mSP = stateZ80.mSP;
	mbIFF1 = stateZ80.mbIFF1;
	mbIFF2 = stateZ80.mbIFF2;
}

bool ATCoProcZ80::IsHalted() const {
	return *mpNextState == ATCPUStatesZ80::kZ80StateHalt;
}

void ATCoProcZ80::SetPortReadHandler(const vdfunction<uint8(uint8)>& fn) {
	mpFnReadPort = fn;
}

void ATCoProcZ80::SetPortWriteHandler(const vdfunction<void(uint8, uint8)>& fn) {
	mpFnWritePort = fn;
}

void ATCoProcZ80::SetIntVectorHandler(const vdfunction<uint8()>& fn) {
	mpFnIntVec = fn;
}

void ATCoProcZ80::SetIntAckHandler(const vdfunction<void()>& fn) {
	mpFnIntAck = fn;
}

void ATCoProcZ80::SetHaltChangeHandler(const vdfunction<void(bool)>& fn) {
	mpFnHaltChange = fn;
}

void ATCoProcZ80::SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bpHandler) {
	bool wasEnabled = (mpBreakpointMap != nullptr);
	bool nowEnabled = (bpMap != nullptr);

	mpBreakpointMap = bpMap;
	mpBreakpointHandler = bpHandler;

	if (wasEnabled != nowEnabled) {
		if (nowEnabled) {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak)
					op = ATCPUStatesZ80::kZ80StateReadOpcode;
			}

			mReadOpcodeState = ATCPUStatesZ80::kZ80StateReadOpcode;
		} else {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStatesZ80::kZ80StateReadOpcode)
					op = ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak;
			}

			if (mpNextState == &kInitialState)
				mpNextState = &kInitialStateNoBreak;

			mReadOpcodeState = ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak;
		}
	}
}

void ATCoProcZ80::ColdReset() {
	mA = 0;
	mF = 0;
	mB = 0;
	mC = 0;
	mD = 0;
	mE = 0;
	mH = 0;
	mL = 0;
	mIXL = 0;
	mIXH = 0;
	mIYL = 0;
	mIYH = 0;
	mAltA = 0;
	mAltF = 0;
	mAltB = 0;
	mAltC = 0;
	mAltD = 0;
	mAltE = 0;
	mAltH = 0;
	mAltL = 0;

	mPC = 0;
	mSP = 0;

	WarmReset();
}

void ATCoProcZ80::WarmReset() {
	mPC = 0;
	mIntMode = 0;
	mI = 0;
	mR = 0;

	mbIFF1 = false;
	mbIFF2 = false;
	mbIntActionNeeded = mbIrqPending;
	mbEiPending = false;
	mbNmiPending = false;
	mbMarkHistoryIrq = false;
	mbMarkHistoryNmi = false;

	mpNextState = mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
	mReadOpcodeState = mpBreakpointMap ? ATCPUStatesZ80::kZ80StateReadOpcode : ATCPUStatesZ80::kZ80StateReadOpcodeNoBreak;
	mInsnPC = mPC;

	mTStatesLeft = 4;
}

void ATCoProcZ80::AssertIrq() {
	mbIrqPending = true;
	mbIntActionNeeded = true;
}

void ATCoProcZ80::NegateIrq() {
	mbIrqPending = false;
	mbIntActionNeeded = mbEiPending;
}

void ATCoProcZ80::AssertNmi() {
	mbIntActionNeeded = true;
	mbNmiPending = true;
}

void ATCoProcZ80::Run() {
	using namespace ATCPUStatesZ80;

	ATCP_MEMORY_CONTEXT;

	if (mCyclesLeft <= 0)
		return;

	#include <coz80.inl>
}

inline uint8 ATCoProcZ80::DebugReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpDebugRead(addr, node->mpThis);
}

inline uint8 ATCoProcZ80::ReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpRead(addr, node->mpThis);
}

inline void ATCoProcZ80::WriteByteSlow(uintptr base, uint32 addr, uint8 value) {
	auto node = (ATCoProcWriteMemNode *)(base - 1);

	node->mpWrite(addr, value, node->mpThis);
}

bool ATCoProcZ80::CheckBreakpoint() {
	if (mpBreakpointHandler->CheckBreakpoint(mPC)) {
		mpNextState = &kInitialStateNoBreak;
		mInsnPC = mPC;
		return true;
	}

	return false;
}

const uint8 *ATCoProcZ80::RegenerateDecodeTables() {
	ATCPUDecoderGeneratorZ80 gen;
	gen.RebuildTables(mDecoderTables, false, mpHistory != nullptr, mpBreakpointMap != nullptr);

	return mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
}
