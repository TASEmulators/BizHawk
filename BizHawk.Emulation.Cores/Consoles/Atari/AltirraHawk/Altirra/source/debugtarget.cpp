//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
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
#include <at/atcore/address.h>
#include <at/atcpu/execstate.h>
#include "cpu.h"
#include "debugtarget.h"
#include "simulator.h"

extern ATSimulator g_sim;

void *ATDebuggerDefaultTarget::AsInterface(uint32 iid) {
	if (iid == IATDebugTargetHistory::kTypeID)
		return static_cast<IATDebugTargetHistory *>(this);

	return nullptr;
}

const char *ATDebuggerDefaultTarget::GetName() {
	return "Main CPU";
}

ATDebugDisasmMode ATDebuggerDefaultTarget::GetDisasmMode() {
	switch(g_sim.GetCPU().GetCPUMode()) {
		case kATCPUMode_6502:
		default:
			return kATDebugDisasmMode_6502;

		case kATCPUMode_65C02:
			return kATDebugDisasmMode_65C02;

		case kATCPUMode_65C816:
			return kATDebugDisasmMode_65C816;
	}
}

void ATDebuggerDefaultTarget::GetExecState(ATCPUExecState& state) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	ATCPUExecState6502& state6502 = state.m6502;
	state6502.mPC = cpu.GetInsnPC();
	state6502.mA = cpu.GetA();
	state6502.mX = cpu.GetX();
	state6502.mY = cpu.GetY();
	state6502.mS = cpu.GetS();
	state6502.mP = cpu.GetP();

	state6502.mAH = cpu.GetAH();
	state6502.mXH = cpu.GetXH();
	state6502.mYH = cpu.GetYH();
	state6502.mSH = cpu.GetSH();
	state6502.mB = cpu.GetB();
	state6502.mK = cpu.GetK();
	state6502.mDP = cpu.GetD();
	state6502.mbEmulationFlag = cpu.GetEmulationFlag();
	
	state6502.mbAtInsnStep = cpu.IsAtInsnStep();
}

void ATDebuggerDefaultTarget::SetExecState(const ATCPUExecState& state) {
	ATCPUEmulator& cpu = g_sim.GetCPU();
	const ATCPUExecState6502& state6502 = state.m6502;

	// we must guard this to avoid disturbing an instruction in progress
	if (state6502.mPC != cpu.GetInsnPC())
		cpu.SetPC(state6502.mPC);

	cpu.SetA(state6502.mA);
	cpu.SetX(state6502.mX);
	cpu.SetY(state6502.mY);
	cpu.SetS(state6502.mS);
	cpu.SetP(state6502.mP);
	cpu.SetAH(state6502.mAH);
	cpu.SetXH(state6502.mXH);
	cpu.SetYH(state6502.mYH);
	cpu.SetSH(state6502.mSH);
	cpu.SetB(state6502.mB);
	cpu.SetK(state6502.mK);
	cpu.SetD(state6502.mDP);
	cpu.SetEmulationFlag(state6502.mbEmulationFlag);
}

sint32 ATDebuggerDefaultTarget::GetTimeSkew() {
	return 0;
}

uint8 ATDebuggerDefaultTarget::ReadByte(uint32 address) {
	if (address < 0x1000000)
		return g_sim.DebugExtReadByte(address);

	return 0;
}

void ATDebuggerDefaultTarget::ReadMemory(uint32 address, void *dst, uint32 n) {
	const uint32 addrSpace = address & kATAddressSpaceMask;
	uint32 addrOffset = address;

	while(n--) {
		*(uint8 *)dst = ReadByte(addrSpace + (addrOffset++ & kATAddressOffsetMask));
		dst = (uint8 *)dst + 1;
	}
}

uint8 ATDebuggerDefaultTarget::DebugReadByte(uint32 address) {
	return g_sim.DebugGlobalReadByte(address);
}

void ATDebuggerDefaultTarget::DebugReadMemory(uint32 address, void *dst, uint32 n) {
	const uint32 addrSpace = address & kATAddressSpaceMask;
	uint32 addrOffset = address;

	while(n--) {
		*(uint8 *)dst = DebugReadByte(addrSpace + (addrOffset++ & kATAddressOffsetMask));
		dst = (uint8 *)dst + 1;
	}

}

void ATDebuggerDefaultTarget::WriteByte(uint32 address, uint8 value) {
	g_sim.DebugGlobalWriteByte(address, value);
}

void ATDebuggerDefaultTarget::WriteMemory(uint32 address, const void *src, uint32 n) {
	const uint32 addrSpace = address & kATAddressSpaceMask;
	uint32 addrOffset = address;

	while(n--) {
		WriteByte(addrSpace + (addrOffset++ & kATAddressOffsetMask), *(const uint8 *)src);
		src = (const uint8 *)src + 1;
	}
}

bool ATDebuggerDefaultTarget::GetHistoryEnabled() const {
	return g_sim.GetCPU().IsHistoryEnabled();
}

void ATDebuggerDefaultTarget::SetHistoryEnabled(bool enable) {
	g_sim.GetCPU().SetHistoryEnabled(enable);
}

std::pair<uint32, uint32> ATDebuggerDefaultTarget::GetHistoryRange() const {
	const auto& cpu = g_sim.GetCPU();
	const uint32 hcnt = cpu.GetHistoryCounter();
	const uint32 hlen = cpu.GetHistoryLength();

	return std::pair<uint32, uint32>(hcnt - hlen, hcnt);
}

uint32 ATDebuggerDefaultTarget::ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const {
	const auto& cpu = g_sim.GetCPU();
	const uint32 hcnt = cpu.GetHistoryCounter();
	uint32 hidx = (hcnt - 1) - start;

	for(uint32 i=n; i; --i)
		*hparray++ = &cpu.GetHistory(hidx--);

	return n;
}

uint32 ATDebuggerDefaultTarget::ConvertRawTimestamp(uint32 rawTimestamp) const {
	return rawTimestamp;
}

double ATDebuggerDefaultTarget::GetTimestampFrequency() const {
	return g_sim.GetScheduler()->GetRate().asDouble();
}
