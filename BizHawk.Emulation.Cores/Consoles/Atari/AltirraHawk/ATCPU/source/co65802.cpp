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
#include <at/atcpu/co65802.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atcpu/states.h>

#define ATCP_MEMORY_CONTEXT	\
	[[maybe_unused]] uint16 tmpaddr;		\
	[[maybe_unused]] uintptr tmpbase;	\
	[[maybe_unused]] uint8 tmpval;

#define ATCP_DUMMY_READ_BYTE(addr) ((void)(0))
#define ATCP_DEBUG_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? DebugReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? ReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))
#define ATCP_WRITE_BYTE(addr, value) ((void)(tmpaddr = (addr), tmpval = (value), tmpbase = mWriteMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? WriteByteSlow(tmpbase, tmpaddr, tmpval) : (void)(*(uint8 *)(tmpbase + tmpaddr) = tmpval))))

const uint8 ATCoProc65802::kInitialState = ATCPUStates::kStateReadOpcode;
const uint8 ATCoProc65802::kInitialStateNoBreak = ATCPUStates::kStateReadOpcodeNoBreak;

ATCoProc65802::ATCoProc65802() {
	ATCPUDecoderGenerator65816 gen;
	gen.RebuildTables(mDecoderTables, false, false, false);
}

void ATCoProc65802::SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]) {
	bool historyWasOn = (mpHistory != nullptr);
	bool historyNowOn = (buffer != nullptr);

	mpHistory = buffer;

	if (historyWasOn != historyNowOn) {
		for(uint8& op : mDecoderTables.mDecodeHeap) {
			if (op == ATCPUStates::kStateReadOpcode || op == ATCPUStates::kStateReadOpcodeNoBreak)
				op = ATCPUStates::kStateRegenerateDecodeTables;
			else if (op == ATCPUStates::kStateAddToHistory)
				op = ATCPUStates::kStateNop;
		}
	}
}

void ATCoProc65802::GetExecState(ATCPUExecState& state) const {
	state.m6502.mPC = mInsnPC;
	state.m6502.mA = mA;
	state.m6502.mX = mX;
	state.m6502.mY = mY;
	state.m6502.mS = mS;
	state.m6502.mP = mP;
	state.m6502.mAH = mAH;
	state.m6502.mXH = mXH;
	state.m6502.mYH = mYH;
	state.m6502.mSH = mSH;
	state.m6502.mB = mB;
	state.m6502.mK = mK;
	state.m6502.mDP = mDP;
	state.m6502.mbEmulationFlag = mbEmulationFlag;

	state.m6502.mbAtInsnStep = (*mpNextState == ATCPUStates::kStateReadOpcodeNoBreak);
}

void ATCoProc65802::SetExecState(const ATCPUExecState& state) {
	const ATCPUExecState6502& state6502 = state.m6502;

	if (mInsnPC != state6502.mPC) {
		mPC = state6502.mPC;
		mInsnPC = state6502.mPC;

		mExtraFlags = {};
		mpNextState = &kInitialStateNoBreak;
	}

	mA = state6502.mA;
	mX = state6502.mX;
	mY = state6502.mY;
	mS = state6502.mS;

	uint8 p = state6502.mP;
	bool redecode = false;

	if (state6502.mbEmulationFlag)
		p |= 0x30;

	if (mP != p) {
		if ((mP ^ p) & 0x30)
			redecode = true;

		mP = p;
	}

	mAH = state6502.mAH;

	if (!(mP & 0x10)) {
		mXH = state6502.mXH;
		mYH = state6502.mYH;
	}

	if (!mbEmulationFlag)
		mSH = state6502.mSH;

	mB = state6502.mB;
	mK = state6502.mK;

	if (mDP != state6502.mDP) {
		mDP = state6502.mDP;

		redecode = true;
	}

	if (mbEmulationFlag != state6502.mbEmulationFlag) {
		mbEmulationFlag = state6502.mbEmulationFlag;

		if (mbEmulationFlag) {
			mXH = 0;
			mYH = 0;
			mSH = 1;
		}
	}

	if (redecode)
		UpdateDecodeTable();
}

void ATCoProc65802::SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bpHandler) {
	bool wasEnabled = (mpBreakpointMap != nullptr);
	bool nowEnabled = (bpMap != nullptr);

	mpBreakpointMap = bpMap;
	mpBreakpointHandler = bpHandler;

	if (wasEnabled != nowEnabled) {
		if (nowEnabled) {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates::kStateReadOpcodeNoBreak)
					op = ATCPUStates::kStateReadOpcode;
			}
		} else {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates::kStateReadOpcode)
					op = ATCPUStates::kStateReadOpcodeNoBreak;
			}

			if (mpNextState == &kInitialState)
				mpNextState = &kInitialStateNoBreak;
		}
	}
}

void ATCoProc65802::ColdReset() {
	mA = 0;
	mAH = 0;
	mP = 0x30;
	mX = 0;
	mXH = 0;
	mY = 0;
	mYH = 0;
	mS = 0xFF;
	mSH = 0x01;
	mDP = 0;
	mB = 0;
	mK = 0;
	mPC = 0;

	WarmReset();
}

void ATCoProc65802::WarmReset() {
	ATCP_MEMORY_CONTEXT;

	mPC = ATCP_READ_BYTE(0xFFFC);
	mPC += ((uint32)ATCP_READ_BYTE(0xFFFD) << 8);

	// clear D flag
	mP &= 0xF7;

	// set MX and E flags
	mP |= 0x30;
	mbEmulationFlag = true;

	mK = 0;
	mB = 0;
	mDP = 0;
	mSH = 1;
	mSubMode = kSubMode_Emulation;

	mpNextState = mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
	mInsnPC = mPC;

	UpdateDecodeTable();
}

void ATCoProc65802::Run() {
	using namespace ATCPUStates;

	ATCP_MEMORY_CONTEXT;

	if (mCyclesLeft <= 0)
		return;

	uint32		cyclesLeft = mCyclesLeft;
	const uint32 cyclesBase = mCyclesBase;

	const uint8 *nextState = mpNextState;

	#include <co65802.inl>

	mpNextState = nextState;
	mCyclesLeft = (sint32)cyclesLeft;
}

inline uint8 ATCoProc65802::DebugReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpDebugRead(addr, node->mpThis);
}

inline uint8 ATCoProc65802::ReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpRead(addr, node->mpThis);
}

inline void ATCoProc65802::WriteByteSlow(uintptr base, uint32 addr, uint8 value) {
	auto node = (ATCoProcWriteMemNode *)(base - 1);

	node->mpWrite(addr, value, node->mpThis);
}

void ATCoProc65802::DoExtra() {
	if (mExtraFlags & kExtraFlag_SetV) {
		mP |= 0x40;
	}

	mExtraFlags = {};
}

bool ATCoProc65802::CheckBreakpoint() {
	if (mpBreakpointHandler->CheckBreakpoint(((uint32)mK << 16) + mPC)) {
		mpNextState = &kInitialStateNoBreak;
		mInsnPC = mPC;
		return true;
	}

	return false;
}

void ATCoProc65802::UpdateDecodeTable() {
	SubMode subMode = kSubMode_Emulation;

	if (!mbEmulationFlag)
		subMode = (SubMode)(kSubMode_NativeM16X16 + ((mP >> 4) & 3));

	if (mSubMode != subMode) {
		mSubMode = subMode;

		if (mbEmulationFlag) {
			mSH = 0x01;
			mXH = 0;
			mYH = 0;
		} else {
			if (mP & 0x10) {
				mXH = 0;
				mYH = 0;
			}
		}
	}

	uint8 decMode = (uint8)(subMode + ((mDP & 0xff) ? 5 : 0) - kSubMode_Emulation);

	mpDecodePtrs = mDecoderTables.mInsnPtrs[decMode];
}

const uint8 *ATCoProc65802::RegenerateDecodeTables() {
	ATCPUDecoderGenerator65816 gen;
	gen.RebuildTables(mDecoderTables, false, mpHistory != nullptr, mpBreakpointMap != nullptr);

	return mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
}

