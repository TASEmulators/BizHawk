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

#ifndef f_ATCOPROC_CO65802_H
#define f_ATCOPROC_CO65802_H

#include <at/atcpu/decode65816.h>

struct ATCPUExecState;
struct ATCPUHistoryEntry;
class IATCPUBreakpointHandler;

class ATCoProc65802 {
public:
	ATCoProc65802();

	uintptr *GetReadMap() { return mReadMap; }
	uintptr *GetWriteMap() { return mWriteMap; }

	void SetHistoryBuffer(ATCPUHistoryEntry buffer[131072]);
	uint32 GetHistoryCounter() const { return mHistoryIndex; }
	uint32 GetTime() const { return mCyclesBase - mCyclesLeft; }
	uint32 GetTimeBase() const { return mCyclesBase; }

	void GetExecState(ATCPUExecState& state) const;
	void SetExecState(const ATCPUExecState& state);

	uint16 GetS() const { return ((uint16)mSH << 8) + mS; }
	void SetV() { mExtraFlags = (ExtraFlags)(mExtraFlags | kExtraFlag_SetV); }

	void SetBreakpointMap(const bool bpMap[65536], IATCPUBreakpointHandler *bphandler);

	void ColdReset();
	void WarmReset();

	uint32 GetCyclesLeft() const { return mCyclesLeft; }
	void AddCycles(sint32 cycles) { mCyclesBase += cycles;  mCyclesLeft += cycles; }
	void Run();

private:
	inline uint8 DebugReadByteSlow(uintptr base, uint32 addr);
	inline uint8 ReadByteSlow(uintptr base, uint32 addr);
	inline void WriteByteSlow(uintptr base, uint32 addr, uint8 value);
	void DoExtra();
	bool CheckBreakpoint();
	void UpdateDecodeTable();
	const uint8 *RegenerateDecodeTables();

	enum SubMode {
		kSubMode_Emulation,
		kSubMode_NativeM16X16,
		kSubMode_NativeM16X8,
		kSubMode_NativeM8X16,
		kSubMode_NativeM8X8,
		kSubModeCount
	};

	enum ExtraFlags : uint8 {
		kExtraFlag_SetV = 0x01
	};

	SubMode		mSubMode = {};
	ExtraFlags	mExtraFlags = {};

	uint8		mA = 0;
	uint8		mAH = 0;
	uint8		mP = 0;
	bool		mbEmulationFlag = false;
	uint8		mX = 0;
	uint8		mXH = 0;
	uint8		mY = 0;
	uint8		mYH = 0;
	uint8		mS = 0;
	uint8		mSH = 0;
	uint16		mDP = 0;
	uint8		mB = 0;
	uint8		mK = 0;
	uint8		mData = 0;
	uint16		mData16 = 0;
	uint16		mAddr = 0;
	uint16		mAddr2 = 0;
	uint8		mAddrBank = 0;
	uint16		mPC = 0;
	uint16		mInsnPC = 0;
	sint32		mCyclesLeft = 0;
	uint32		mCyclesBase = 0;
	const uint8	*mpNextState = nullptr;
	const uint16 *mpDecodePtrs = nullptr;
	const bool	*mpBreakpointMap = nullptr;
	IATCPUBreakpointHandler *mpBreakpointHandler = nullptr;
	ATCPUHistoryEntry *mpHistory = nullptr;
	uint32		mHistoryIndex = 0;

	uintptr		mReadMap[256] = {};
	uintptr		mWriteMap[256] = {};

	ATCPUDecoderTables65816 mDecoderTables {};

	static const uint8 kInitialState;
	static const uint8 kInitialStateNoBreak;
};

#endif	// f_ATCOPROC_CO65802_H
