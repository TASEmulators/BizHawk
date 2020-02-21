//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#ifndef AT_CPU_H
#define AT_CPU_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>
#include <at/atcpu/history.h>

class ATCPUEmulator;
class ATCPUHookManager;
class ATCPUProfiler;
class ATCPUVerifier;
class ATCPUHeatMap;
class ATSaveStateReader;
class ATSaveStateWriter;
class ATCPUEmulatorMemory;
class ATBreakpointManager;
struct ATCPUHistoryEntry;

enum ATDebugDisasmMode : uint8;

class ATCPUEmulatorCallbacks {
public:
	virtual uint32 CPUGetCycle() = 0;
	virtual uint32 CPUGetUnhaltedCycle() = 0;
	virtual void CPUGetHistoryTimes(ATCPUHistoryEntry * VDRESTRICT he) const = 0;
};

enum ATCPUStepResult {
	kATCPUStepResult_Continue,
	kATCPUStepResult_SkipCall,
	kATCPUStepResult_Stop
};

typedef ATCPUStepResult (*ATCPUStepCallback)(ATCPUEmulator *cpu, uint32 pc, bool call, void *data);

enum ATCPUMode : uint8 {
	kATCPUMode_6502,
	kATCPUMode_65C02,
	kATCPUMode_65C816,
	kATCPUModeCount
};

enum ATCPUSubMode : uint8 {
	kATCPUSubMode_6502,
	kATCPUSubMode_65C02,
	kATCPUSubMode_65C816_Emulation,
	kATCPUSubMode_65C816_NativeM16X16,
	kATCPUSubMode_65C816_NativeM16X8,
	kATCPUSubMode_65C816_NativeM8X16,
	kATCPUSubMode_65C816_NativeM8X8,
	kATCPUSubModeCount
};

enum ATCPUAdvanceMode {
	kATCPUAdvanceMode_6502,
	kATCPUAdvanceMode_65816,
	kATCPUAdvanceMode_65816HiSpeed
};

namespace AT6502 {
	enum {
		kFlagN = 0x80,
		kFlagV = 0x40,
		kFlagM = 0x20,		// 65C816 native mode only
		kFlagX = 0x10,		// 65C816 native mode only
		kFlagB = 0x10,
		kFlagD = 0x08,
		kFlagI = 0x04,
		kFlagZ = 0x02,
		kFlagC = 0x01
	};
}

class ATCPUEmulator {
public:
	ATCPUEmulator();
	~ATCPUEmulator();

	bool	Init(ATCPUEmulatorMemory *mem, ATCPUHookManager *hookmgr, ATCPUEmulatorCallbacks *callbacks);

	ATCPUEmulatorMemory *GetMemory() const { return mpMemory; }
	ATCPUHookManager *GetHookManager() const { return mpHookMgr; }

	void	SetBreakpointManager(ATBreakpointManager *bkptmanager);

	void	ColdReset();
	void	WarmReset();

	bool	GetUnusedCycle() {
		bool b = mbUnusedCycle;
		mbUnusedCycle = false;
		return b;
	}

	bool	IsAtInsnStep() const;
	bool	IsInstructionInProgress() const;
	bool	IsNextCycleWrite() const;
	uint8	GetHeldCycleValue();

	void	ForceNextCycleSlow() {
		mSubCyclesLeft = 1;
		mbForceNextCycleSlow = true;
	}

	bool	GetEmulationFlag() const { return mbEmulationFlag; }
	uint16	GetInsnPC() const { return mInsnPC; }
	uint16	GetPC() const { return mPC; }
	uint32	GetXPC() const;
	uint8	GetP() const { return mP; }
	uint8	GetS() const { return mS; }
	uint16	GetS16() const { return mS + ((uint32)mSH << 8); }
	uint8	GetA() const { return mA; }
	uint8	GetX() const { return mX; }
	uint8	GetY() const { return mY; }

	void	SetEmulationFlag(bool emu);
	void	SetPC(uint16 pc);
	void	SetP(uint8 p);
	void	SetA(uint8 a) { mA = a; }
	void	SetX(uint8 x) { mX = x; }
	void	SetY(uint8 y) { mY = y; }
	void	SetS(uint8 s) { mS = s; }
	void	SetAH(uint8 a);
	void	SetXH(uint8 x);
	void	SetYH(uint8 y);
	void	SetSH(uint8 s);
	void	SetD(uint16 dp);
	void	SetK(uint8 k);
	void	SetB(uint8 b);

	uint8	GetAH() const { return mAH; }
	uint8	GetXH() const { return mXH; }
	uint8	GetYH() const { return mYH; }
	uint8	GetSH() const { return mSH; }
	uint8	GetB() const { return mB; }
	uint8	GetK() const { return mK; }
	uint16	GetD() const { return mDP; }

	void	SetFlagC() { mP |= AT6502::kFlagC; }
	void	ClearFlagC() { mP &= ~AT6502::kFlagC; }

	void	SetHook(uint16 pc, bool enable);

	bool	GetStep() const { return mbStep; }

	void	SetStep(bool step) {
		mbStep = step;
		mbStepOver = false;
		mStepRegionStart = 0;
		mStepRegionSize = 0;
		mpStepCallback = NULL;
		mStepStackLevel = -1;

		mDebugFlags &= ~kDebugFlag_StepNMI;

		if (step)
			mDebugFlags |= kDebugFlag_Step;
		else
			mDebugFlags &= ~kDebugFlag_Step;
	}

	void	SetStepNMI() {
		mbStep = false;
		mbStepOver = false;
		mStepRegionStart = 0;
		mStepRegionSize = 0;
		mpStepCallback = NULL;
		mStepStackLevel = -1;

		mDebugFlags |= kDebugFlag_StepNMI;
		mDebugFlags &= ~kDebugFlag_Step;
	}

	void	SetStepRange(uint32 regionStart, uint32 regionSize, ATCPUStepCallback stepcb, void *stepcbdata, bool stepOver);

	void	SetTrace(bool trace) {
		mbTrace = trace;
		if (trace)
			mDebugFlags |= kDebugFlag_Trace;
		else
			mDebugFlags &= ~kDebugFlag_Trace;
	}
	void	SetRTSBreak() { mSBrk = 0x100; mDebugFlags &= ~kDebugFlag_SBrk; }
	void	SetRTSBreak(uint8 sp) { mSBrk = sp; mDebugFlags |= kDebugFlag_SBrk; }

	void	SetCPUMode(ATCPUMode mode, uint32 subCycles);
	ATCPUMode GetCPUMode() const { return mCPUMode; }
	ATDebugDisasmMode GetDisasmMode() const { return (ATDebugDisasmMode)mCPUMode; }
	uint32 GetSubCycles() const { return mSubCycles; }
	ATCPUSubMode GetCPUSubMode() const { return mCPUSubMode; }
	ATCPUAdvanceMode GetAdvanceMode() const { return mAdvanceMode; }

	bool	IsHistoryEnabled() const { return mbHistoryEnabled; }
	void	SetHistoryEnabled(bool enable);

	void	SetTracingEnabled(bool enable);

	bool	IsPathfindingEnabled() const { return mbPathfindingEnabled; }
	void	SetPathfindingEnabled(bool enable);

	bool	IsPathBreakEnabled() const { return mbPathBreakEnabled; }
	void	SetPathBreakEnabled(bool enable) { mbPathBreakEnabled = enable; }

	void	SetProfiler(ATCPUProfiler *profiler);
	void	SetVerifier(ATCPUVerifier *verifier);
	void	SetHeatMap(ATCPUHeatMap *heatmap);

	bool	AreIllegalInsnsEnabled() const { return mbIllegalInsnsEnabled; }
	void	SetIllegalInsnsEnabled(bool enable);

	bool	GetStopOnBRK() const { return mbStopOnBRK; }
	void	SetStopOnBRK(bool en);

	bool	IsNMIBlockingEnabled() const { return mbAllowBlockedNMIs; }
	void	SetNMIBlockingEnabled(bool enable);

	uint32	GetBreakpointCount() const;
	bool	IsBreakpointSet(uint16 addr) const;
	sint32	GetNextBreakpoint(sint32 last) const;
	void	SetBreakpoint(uint16 addr);
	void	ClearBreakpoint(uint16 addr);
	void	SetAllBreakpoints();
	void	ClearAllBreakpoints();

	void	ResetAllPaths();
	sint32	GetNextPathInstruction(sint32 addr) const;
	bool	IsPathStart(uint16 addr) const;
	bool	IsInPath(uint16 addr) const;

	const ATCPUHistoryEntry& GetHistory(int i) const {
		return mHistory[(mHistoryIndex - i - 1) & 131071];
	}

	int GetHistoryLength() const { return 131072; }
	uint32	GetHistoryCounter() const { return mHistoryIndex; }

	void	DumpStatus(bool extended = false);

	void	BeginLoadState(ATSaveStateReader& reader);
	void	LoadState6502(ATSaveStateReader& reader);
	void	LoadState65C816(ATSaveStateReader& reader);
	void	LoadStatePrivate(ATSaveStateReader& reader);
	void	LoadStateResetPrivate(ATSaveStateReader& reader);
	void	EndLoadState(ATSaveStateReader& reader);

	void	BeginSaveState(ATSaveStateWriter& writer);
	void	SaveStateArch(ATSaveStateWriter& writer);
	void	SaveStatePrivate(ATSaveStateWriter& writer);
	void	EndSaveState(ATSaveStateWriter& writer);

	void	InjectOpcode(uint8 op);
	void	Push(uint8 v);
	void	PushWord(uint16 v);
	uint8	Pop();
	void	Jump(uint16 addr);
	void	Ldy(uint8 v);

	void	AssertIRQ(int cycleOffset);
	void	NegateIRQ();
	void	AssertNMI();
	void	AssertABORT();

	// Low-priority call needed to clean up timers to avoid time base wrapping. This needs
	// to be called no more than every 2^30 cycles, so not critical.
	void	PeriodicCleanup();

	int		Advance();
	int		Advance6502();
	int		Advance65816();
	int		Advance65816HiSpeed(bool dma);

protected:
	__declspec(noinline) uint8 ProcessDebugging();
	__declspec(noinline) void ProcessStepOver();
	__declspec(noinline) uint8 ProcessHook();
	__declspec(noinline) uint8 ProcessHook816();

	template<bool T_Accel>
	bool	ProcessInterrupts();

	template<bool is816, bool subCycles>
	void	AddHistoryEntry(bool slowFlag);

	void	UpdatePendingIRQState();
	void	RedecodeInsnWithoutBreak();
	void	Update65816DecodeTable();
	void	RebuildDecodeTables();
	void	RebuildDecodeTables6502(bool cmos);
	void	RebuildDecodeTables65816();
	bool	Decode6502(uint8 opcode);
	bool	Decode6502Ill(uint8 opcode);
	bool	Decode65C02(uint8 opcode);
	bool	Decode65C816(uint8 opcode, bool unalignedDP, bool emu, bool mode16, bool index16);
	void	DecodeReadImm();
	void	DecodeReadZp();
	void	DecodeReadZpX();
	void	DecodeReadZpY();
	void	DecodeReadAbs();
	void	DecodeReadAbsX();
	void	DecodeReadAbsY();
	void	DecodeReadIndX();
	void	DecodeReadIndY();
	void	DecodeReadInd();

	void	Decode65816AddrDp(bool unalignedDP);
	void	Decode65816AddrDpX(bool unalignedDP, bool emu);
	void	Decode65816AddrDpY(bool unalignedDP, bool emu);
	void	Decode65816AddrDpInd(bool unalignedDP);
	void	Decode65816AddrDpIndX(bool unalignedDP, bool emu);
	void	Decode65816AddrDpIndY(bool unalignedDP, bool emu, bool forceCycle);
	void	Decode65816AddrDpLongInd(bool unalignedDP);
	void	Decode65816AddrDpLongIndY(bool unalignedDP);
	void	Decode65816AddrAbs();
	void	Decode65816AddrAbsX(bool forceCycle);
	void	Decode65816AddrAbsY(bool forceCycle);
	void	Decode65816AddrAbsLong();
	void	Decode65816AddrAbsLongX();
	void	Decode65816AddrStackRel();
	void	Decode65816AddrStackRelInd();

	const uint8 *mpNextState;

	// The register arrangement here is set up to allow DWORD copies into
	// history entries.
	uint8	mA;
	uint8	mX;
	uint8	mY;
	uint8	mS;

	bool	mbHistoryActive;
	uint8	mP;
	uint16	mInsnPC;
	uint16	mPC;
	uint16	mAddr;
	uint16	mAddr2;
	sint16	mRelOffset;
	uint8	mData;
	uint8	mOpcode;
	uint16	mData16;
	uint8	mAddrBank;

	uint8	mB;		// data bank register
	uint8	mK;		// program bank register
	uint8	mSH;
	uint8	mAH;
	uint8	mXH;
	uint8	mYH;
	uint16	mDP;

	// These are in bitfields so the insn fetch code can check them all at once.
	enum : uint8 {
		kIntFlag_IRQReleasePending = 0x01,
		kIntFlag_IRQSetPending = 0x02,
		kIntFlag_IRQActive = 0x04,
		kIntFlag_IRQPending = 0x08,
		kIntFlag_NMIPending = 0x10
	};

	uint8	mIntFlags;

	bool	mbNMIForced;

	bool	mbTrace;			// must also affect mDebugFlags
	bool	mbStep;				// must also affect mDebugFlags
	bool	mbStepOver;			// set if we want to avoid subroutines

	enum {
		kDebugFlag_Step = 0x01,		// mbStep is set
		kDebugFlag_SBrk = 0x02,		// mSBrk is active
		kDebugFlag_Trace = 0x04,	// mbTrace is set
		kDebugFlag_BP = 0x08,		// Breakpoints are set
		kDebugFlag_StepNMI = 0x10	// NMI step is pending
	};

	uint8	mDebugFlags;

	uint32	mStepRegionStart;
	uint32	mStepRegionSize;
	int		mStepStackLevel;
	ATCPUStepCallback mpStepCallback;
	void	*mpStepCallbackData;

	uint32	mSubCyclesLeft;
	bool	mbForceNextCycleSlow;
	bool	mbUnusedCycle;
	bool	mbEmulationFlag;
	uint32	mNMIIgnoreUnhaltedCycle;
	uint32	mNMIAssertTime;
	uint32	mIRQAssertTime;
	uint32	mIRQAcknowledgeTime;
	uint32	mSBrk;				// must also affect mDebugFlags
	ATCPUMode	mCPUMode;
	uint32	mSubCycles;
	ATCPUSubMode	mCPUSubMode;
	uint8	mDecodeTableMode816;
	ATCPUAdvanceMode	mAdvanceMode;

	ATCPUEmulatorMemory	*mpMemory;
	ATCPUHookManager *mpHookMgr;
	ATCPUEmulatorCallbacks	*mpCallbacks;
	ATBreakpointManager *mpBkptManager;

	ATCPUProfiler	*mpProfiler;
	ATCPUVerifier	*mpVerifier;
	ATCPUHeatMap	*mpHeatMap;

	uint8 *mpDstState;
	uint8	mStates[16];

	enum HistoryEnableFlags : uint8 {
		kHistoryEnableFlag_None = 0x00,
		kHistoryEnableFlag_Direct = 0x01,
		kHistoryEnableFlag_Profiler = 0x02,
		kHistoryEnableFlag_Tracer = 0x04
	};

	HistoryEnableFlags	mHistoryEnableFlags;
	bool	mbHistoryEnabled;
	bool	mbPathfindingEnabled;
	bool	mbPathBreakEnabled;
	bool	mbIllegalInsnsEnabled;
	bool	mbStopOnBRK;
	bool	mbMarkHistoryIRQ;
	bool	mbMarkHistoryNMI;
	bool	mbAllowBlockedNMIs;

	uint32	mBreakpointCount;

	enum {
		kInsnFlagBreakPt		= 0x01,
		kInsnFlagHook			= 0x08,
		kInsnFlagPathStart		= 0x10,
		kInsnFlagPathExecuted	= 0x20
	};

	const uint8 *mpDecodePtrIRQ;
	const uint8 *mpDecodePtrNMI;
	const uint8 *mpDecodePtrABORT;
	uint16	mDecodePtrs[256];
	uint16	mDecodePtrs816[10][259];
	uint8	mDecodeHeap[0x5000];
	uint8	mInsnFlags[65536];

	typedef ATCPUHistoryEntry HistoryEntry;
	HistoryEntry mHistory[131072];
	int mHistoryIndex;
};

#endif
