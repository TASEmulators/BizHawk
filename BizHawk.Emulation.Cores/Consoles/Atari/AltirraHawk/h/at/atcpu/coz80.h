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

#ifndef f_ATCOPROC_COZ80_H
#define f_ATCOPROC_COZ80_H

#include <vd2/system/function.h>
#include <at/atcpu/decodez80.h>

struct ATCPUExecState;
struct ATCPUHistoryEntry;
class IATCPUBreakpointHandler;

class ATCoProcZ80 {
public:
	ATCoProcZ80();

	uintptr *GetReadMap() { return mReadMap; }
	uintptr *GetWriteMap() { return mWriteMap; }

	void SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]);
	uint32 GetHistoryCounter() const { return mHistoryIndex; }
	uint32 GetTime() const { return mCyclesBase - mCyclesLeft; }
	uint32 GetTimeBase() const { return mCyclesBase; }

	void GetExecState(ATCPUExecState& state) const;
	void SetExecState(const ATCPUExecState& state);

	uint16 GetSP() const { return mSP; }

	bool IsHalted() const;

	void SetPortReadHandler(const vdfunction<uint8(uint8)>& fn);
	void SetPortWriteHandler(const vdfunction<void(uint8, uint8)>& fn);
	void SetIntVectorHandler(const vdfunction<uint8()>& fn);
	void SetIntAckHandler(const vdfunction<void()>& fn);
	void SetHaltChangeHandler(const vdfunction<void(bool)>& fn);
	void SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bphandler);

	void ColdReset();
	void WarmReset();

	void AssertIrq();
	void NegateIrq();

	void AssertNmi();

	uint32 GetTStatesPending() const { return mTStatesLeft; }
	uint32 GetCyclesLeft() const { return mCyclesLeft; }
	void AddCycles(sint32 cycles) { mCyclesBase += cycles;  mCyclesLeft += cycles; }
	void Run();

private:
	inline uint8 DebugReadByteSlow(uintptr base, uint32 addr);
	inline uint8 ReadByteSlow(uintptr base, uint32 addr);
	inline void WriteByteSlow(uintptr base, uint32 addr, uint8 value);
	bool CheckBreakpoint();
	const uint8 *RegenerateDecodeTables();

	int		mTStatesLeft = 0;

	uint8		mF = 0;
	uint8		mA = 0;
	uint8		mC = 0;
	uint8		mB = 0;
	uint8		mE = 0;
	uint8		mD = 0;
	uint8		mL = 0;
	uint8		mH = 0;
	uint8		mIXL = 0;
	uint8		mIXH = 0;
	uint8		mIYL = 0;
	uint8		mIYH = 0;
	uint8		mAltF = 0;
	uint8		mAltA = 0;
	uint8		mAltC = 0;
	uint8		mAltB = 0;
	uint8		mAltE = 0;
	uint8		mAltD = 0;
	uint8		mAltL = 0;
	uint8		mAltH = 0;
	uint16		mSP = 0;
	uint8		mI = 0;
	uint8		mR = 0;
	uint8		mIntMode = 0;
	bool		mbIFF1 = false;
	bool		mbIFF2 = false;
	bool		mbIntActionNeeded = false;
	bool		mbIrqPending = false;
	bool		mbNmiPending = false;
	bool		mbEiPending = false;
	bool		mbUseIY = false;
	bool		mbMarkHistoryIrq = false;
	bool		mbMarkHistoryNmi = false;

	uint8		mDataL = 0;
	uint8		mDataH = 0;
	uint16		mAddr = 0;
	uint16		mPC = 0;
	uint16		mInsnPC = 0;
	sint32		mCyclesLeft = 0;
	uint32		mCyclesBase = 0;
	const uint8	*mpNextState = nullptr;
	const bool	*mpBreakpointMap = nullptr;
	IATCPUBreakpointHandler *mpBreakpointHandler = nullptr;
	ATCPUHistoryEntry *mpHistory = nullptr;
	uint32		mHistoryIndex = 0;
	uint8		mReadOpcodeState = 0;

	vdfunction<uint8(uint8)> mpFnReadPort;
	vdfunction<void(uint8, uint8)> mpFnWritePort;
	vdfunction<uint8()> mpFnIntVec;
	vdfunction<void()> mpFnIntAck;
	vdfunction<void(bool)> mpFnHaltChange;

	uintptr		mReadMap[256] = {};
	uintptr		mWriteMap[256] = {};

	ATCPUDecoderTablesZ80 mDecoderTables {};

	static const uint8 kInitialState;
	static const uint8 kInitialStateNoBreak;
	static const uint8 kIrqSequence[];
};

#endif	// f_ATCOPROC_CO65802_H
