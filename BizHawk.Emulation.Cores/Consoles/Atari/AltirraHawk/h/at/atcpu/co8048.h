//	Altirra - Atari 800/800XL/5200 aemulator
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

#ifndef f_ATCOPROC_CO8048_H
#define f_ATCOPROC_CO8048_H

#include <vd2/system/function.h>

struct ATCPUExecState;
struct ATCPUHistoryEntry;
class IATCPUBreakpointHandler;

class ATCoProc8048 {
public:
	ATCoProc8048();

	void SetProgramBanks(const void *p0, const void *p1);

	uint8 GetSP() const { return mPSW & 7; }
	uint8 GetPort1Output() const { return mP1; }
	uint8 GetPort2Output() const { return mP2; }

	void SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]);
	uint32 GetHistoryCounter() const { return mHistoryIndex; }
	uint32 GetTime() const { return mCyclesBase - mCyclesLeft; }
	uint32 GetTimeBase() const { return mCyclesBase; }

	void GetExecState(ATCPUExecState& state) const;
	void SetExecState(const ATCPUExecState& state);

	void SetT0ReadHandler(const vdfunction<bool()>& fn);
	void SetT1ReadHandler(const vdfunction<bool()>& fn);
	void SetXRAMReadHandler(const vdfunction<uint8(uint8)>& fn);
	void SetXRAMWriteHandler(const vdfunction<void(uint8, uint8)>& fn);
	void SetPortReadHandler(const vdfunction<uint8(uint8, uint8)>& fn);
	void SetPortWriteHandler(const vdfunction<void(uint8, uint8)>& fn);
	void SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bphandler);

	uint8 ReadByte(uint8 addr) const;
	void WriteByte(uint8 addr, uint8 val);

	void ColdReset();
	void WarmReset();

	void AssertIrq();
	void NegateIrq();

	uint32 GetTStatesPending() const { return mTStatesLeft; }
	uint32 GetCyclesLeft() const { return mCyclesLeft; }
	void AddCycles(sint32 cycles) { mCyclesBase += cycles;  mCyclesLeft += cycles; }
	void Run();

private:
	bool CheckBreakpoint();
	void DispatchIrq();
	void UpdateTimer();
	void UpdateTimerDeadline();

	int		mTStatesLeft = 0;

	uint8	mA;
	uint8	mPSW;
	uint8	*mpRegBank;
	const uint8	*mpProgramBanks[2];
	const uint8	*mpProgramBank;

	uint8	mT = 0;
	uint8	mP1 = 0xFF;
	uint8	mP2 = 0xFF;
	bool	mbDBF = false;
	bool	mbPBK = false;
	bool	mbIF = false;
	bool	mbTIF = false;
	bool	mbTF = false;
	bool	mbF1 = false;
	bool	mbIrqEnabled = false;
	bool	mbIrqPending = false;
	bool	mbIrqAttention = false;

	uint16		mPC = 0;
	sint32		mCyclesLeft = 0;
	uint32		mCyclesBase = 0;
	uint32		mCyclesSaved = 0;

	bool	mbTimerActive = false;
	uint32		mTimerDeadline = 0;

	const uint8	*mpNextState = nullptr;
	const bool	*mpBreakpointMap = nullptr;
	IATCPUBreakpointHandler *mpBreakpointHandler = nullptr;
	ATCPUHistoryEntry *mpHistory = nullptr;
	uint32		mHistoryIndex = 0;
	uint8		mReadOpcodeState = 0;

	vdfunction<bool()> mpFnReadT0;
	vdfunction<bool()> mpFnReadT1;
	vdfunction<uint8(uint8)> mpFnReadXRAM;
	vdfunction<void(uint8, uint8)> mpFnWriteXRAM;
	vdfunction<uint8(uint8, uint8)> mpFnReadPort;
	vdfunction<void(uint8, uint8)> mpFnWritePort;
	
	uint8	mRAM[256];

	static const uint8 kInitialState;
	static const uint8 kInitialStateNoBreak;
	static const uint8 kIrqSequence[];
};

#endif	// f_ATCOPROC_CO8048_H
