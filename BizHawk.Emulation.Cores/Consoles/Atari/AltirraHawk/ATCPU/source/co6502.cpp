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
#include <at/atcpu/co6502.h>
#include <at/atcpu/execstate.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atcpu/states6502.h>

#define ATCP_MEMORY_CONTEXT	\
	uint16 tmpaddr;		\
	uintptr tmpbase;

#define ATCP_READ_BYTE(addr) (tmpaddr = (addr), tmpbase = mReadMap[(uint8)(tmpaddr >> 8)], (tmpbase & 1 ? ReadByteSlow(tmpbase, tmpaddr) : *(uint8 *)(tmpbase + tmpaddr)))

const uint8 ATCoProc6502::kInitialState = ATCPUStates6502::kStateReadOpcode;
const uint8 ATCoProc6502::kInitialStateNoBreak = ATCPUStates6502::kStateReadOpcodeNoBreak;

ATCoProc6502::ATCoProc6502(bool isC02)
	: mbIs65C02(isC02)
{
	ATCPUDecoderGenerator6502 gen;
	gen.RebuildTables(mDecoderTables, false, false, false, isC02);
}

void ATCoProc6502::SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]) {
	bool historyWasOn = (mpHistory != nullptr);
	bool historyNowOn = (buffer != nullptr);

	mpHistory = buffer;

	if (historyWasOn != historyNowOn) {
		for(uint8& op : mDecoderTables.mDecodeHeap) {
			if (op == ATCPUStates6502::kStateReadOpcode || op == ATCPUStates6502::kStateReadOpcodeNoBreak)
				op = ATCPUStates6502::kStateRegenerateDecodeTables;
			else if (op == ATCPUStates6502::kStateAddToHistory)
				op = ATCPUStates6502::kStateNop;
		}
	}
}

void ATCoProc6502::GetExecState(ATCPUExecState& state) const {
	state.m6502.mPC = mInsnPC;
	state.m6502.mA = mA;
	state.m6502.mX = mX;
	state.m6502.mY = mY;
	state.m6502.mS = mS;
	state.m6502.mP = mP;
	state.m6502.mAH = 0;
	state.m6502.mXH = 0;
	state.m6502.mYH = 0;
	state.m6502.mSH = 0;
	state.m6502.mB = 0;
	state.m6502.mK = 0;
	state.m6502.mDP = 0;
	state.m6502.mbEmulationFlag = true;

	state.m6502.mbAtInsnStep = (*mpNextState == ATCPUStates6502::kStateReadOpcodeNoBreak);
}

void ATCoProc6502::SetExecState(const ATCPUExecState& state) {
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
	mP = state6502.mP | 0x30;
}

void ATCoProc6502::SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bpHandler) {
	bool wasEnabled = (mpBreakpointMap != nullptr);
	bool nowEnabled = (bpMap != nullptr);

	mpBreakpointMap = bpMap;
	mpBreakpointHandler = bpHandler;

	if (wasEnabled != nowEnabled) {
		if (nowEnabled) {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates6502::kStateReadOpcodeNoBreak)
					op = ATCPUStates6502::kStateReadOpcode;
			}
		} else {
			for(uint8& op : mDecoderTables.mDecodeHeap) {
				if (op == ATCPUStates6502::kStateReadOpcode)
					op = ATCPUStates6502::kStateReadOpcodeNoBreak;
			}

			if (mpNextState == &kInitialState)
				mpNextState = &kInitialStateNoBreak;
		}
	}
}

void ATCoProc6502::ColdReset() {
	mA = 0;
	mP = 0x30;
	mX = 0;
	mY = 0;
	mS = 0xFF;
	mPC = 0;

	WarmReset();
}

void ATCoProc6502::WarmReset() {
	ATCP_MEMORY_CONTEXT;

	mPC = ATCP_READ_BYTE(0xFFFC);
	mPC += ((uint32)ATCP_READ_BYTE(0xFFFD) << 8);

	// clear D flag
	mP &= 0xF7;

	// set MX and E flags
	mP |= 0x30;

	mpNextState = mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
	mInsnPC = mPC;
}

uint8 ATCoProc6502::DebugReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpDebugRead(addr, node->mpThis);
}

uint8 ATCoProc6502::ReadByteSlow(uintptr base, uint32 addr) {
	auto node = (ATCoProcReadMemNode *)(base - 1);

	return node->mpRead(addr, node->mpThis);
}

void ATCoProc6502::WriteByteSlow(uintptr base, uint32 addr, uint8 value) {
	auto node = (ATCoProcWriteMemNode *)(base - 1);

	node->mpWrite(addr, value, node->mpThis);
}

void ATCoProc6502::DoExtra() {
	if (mExtraFlags & kExtraFlag_SetV) {
		mP |= 0x40;
	}

	mExtraFlags = {};
}

bool ATCoProc6502::CheckBreakpoint() {
	if (mpBreakpointHandler->CheckBreakpoint(mPC)) {
		mpNextState = &kInitialStateNoBreak;
		mInsnPC = mPC;
		return true;
	}

	return false;
}

const uint8 *ATCoProc6502::RegenerateDecodeTables() {
	ATCPUDecoderGenerator6502 gen;
	gen.RebuildTables(mDecoderTables, false, mpHistory != nullptr, mpBreakpointMap != nullptr, mbIs65C02);

	return mpBreakpointMap ? &kInitialState : &kInitialStateNoBreak;
}

