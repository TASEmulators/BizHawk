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

#ifndef f_AT_PROFILER_H
#define f_AT_PROFILER_H

#include <vd2/system/linearalloc.h>
#include <at/atcore/scheduler.h>

class ATCPUEmulator;
class ATCPUEmulatorMemory;
class ATCPUEmulatorCallbacks;
class IATCPUTimestampDecoderProvider;

enum ATProfileMode {
	kATProfileMode_Insns,
	kATProfileMode_Functions,
	kATProfileMode_CallGraph,
	kATProfileMode_BasicBlock,
	kATProfileMode_BasicLines,
	kATProfileModeCount
};

enum ATProfileCounterMode {
	kATProfileCounterMode_None,
	kATProfileCounterMode_BranchTaken,
	kATProfileCounterMode_BranchNotTaken,
	kATProfileCounterMode_PageCrossing,
	kATProfileCounterMode_RedundantOp,
};

enum ATProfileBoundaryRule {
	kATProfileBoundaryRule_None,
	kATProfileBoundaryRule_VBlank,
	kATProfileBoundaryRule_PCAddress,
	kATProfileBoundaryRule_PCAddressFunction
};

enum ATProfileContext : uint32 {
	kATProfileContext_Main,
	kATProfileContext_Interrupt,
	kATProfileContext_IRQ,
	kATProfileContext_VBI,
	kATProfileContext_DLI,
};

struct ATProfileRecord {
	uint32 mAddress;
	uint32 mCalls : 28;
	uint32 mContext : 4;
	uint32 mInsns : 29;
	uint32 mModeBits : 2;
	uint32 mEmulationMode : 1;
	uint32 mCycles;
	uint32 mUnhaltedCycles;
	uint32 mCounters[2];
};

struct ATProfileCallGraphRecord {
	uint32	mInsns;
	uint32	mCycles;
	uint32	mUnhaltedCycles;
	uint32	mCalls;
};

struct ATProfileCallGraphInclusiveRecord {
	uint32	mInclusiveCycles;
	uint32	mInclusiveUnhaltedCycles;
	uint32	mInclusiveInsns;
};

struct ATProfileCallGraphContext {
	uint32	mParent;
	uint32	mAddress;
};

void ATProfileComputeInclusiveStats(ATProfileCallGraphInclusiveRecord *dst, const ATProfileCallGraphRecord *src, const ATProfileCallGraphContext *contexts, size_t n);

struct ATProfileFrame {
	typedef vdfastvector<ATProfileRecord> Records;
	Records mRecords;
	Records mBlockRecords;

	typedef vdfastvector<ATProfileCallGraphRecord> CallGraphRecords;
	CallGraphRecords mCallGraphRecords;

	uint32	mTotalCycles;
	uint32	mTotalUnhaltedCycles;
	uint32	mTotalInsns;
};

struct ATProfileMergedFrame : public vdrefcount, public ATProfileFrame {
	vdfastvector<ATProfileCallGraphInclusiveRecord> mInclusiveRecords;
};

class ATProfileSession {
	ATProfileSession(const ATProfileSession&) = delete;
	ATProfileSession& operator=(const ATProfileSession&) = delete;
public:
	ATProfileSession() = default;
	vdnothrow ATProfileSession(ATProfileSession&& src) vdnoexcept;
	~ATProfileSession();

	vdnothrow ATProfileSession& operator=(ATProfileSession&& src) vdnoexcept;

	ATProfileMode mProfileMode;
	vdfastvector<ATProfileCounterMode> mCounterModes;
	vdfastvector<ATProfileFrame *> mpFrames;

	typedef vdfastvector<ATProfileCallGraphContext> CGContexts;
	CGContexts mContexts;
};

void ATProfileMergeFrames(const ATProfileSession& session, uint32 startFrame, uint32 endFrame, ATProfileMergedFrame **mergedFrame);

///////////////////////////////////////////////////////////////////////////

// CPU profile builder
//
// The CPU profiler builder converts CPU execution history into profiling data. It is isolated
// from the emulation so that it can be fed historical data for offline analysis. The typical
// method of operation is:
//
// - Initialize the builder and set boundary rules, if applicable.
// - Set the initial stack pointer.
// - Open the initial frame.
// - Pump history entries through Update() or UpdateBasicLines().
// - As needed, close and reopen new frames.
// - Finalize the builder.
// - Take the session from it.
//
class ATCPUProfileBuilder {
	ATCPUProfileBuilder(const ATCPUProfileBuilder&) = delete;
	ATCPUProfileBuilder& operator=(const ATCPUProfileBuilder&) = delete;

public:
	ATCPUProfileBuilder();
	~ATCPUProfileBuilder();

	void Init(ATProfileMode mode, ATProfileCounterMode c1, ATProfileCounterMode c2);
	void SetBoundaryRule(ATProfileBoundaryRule rule, uint32 param, uint32 param2);
	void SetGlobalAddressesEnabled(bool enable);

	void SetS(uint8 s);

	void AdvanceFrame(uint32 cycle, uint32 unhaltedCycle, bool enableCollection, const ATCPUTimestampDecoder& tsDecoder);

	// Mark the beginning of a frame.
	void OpenFrame(uint32 cycle, uint32 unhaltedCycle, const ATCPUTimestampDecoder& tsDecoder);

	// Mark the end of a frame. If keepFrame=false, the detailed frame data is discarded instead of kept.
	// Frame must be closed exactly once.
	void CloseFrame(uint32 cycle, uint32 unhaltedCycle, bool keepFrame);

	// End collection and finalize profiling data. The last frame must be closed before calling this.
	void Finalize();

	// Retrieve the profile session data (destructive).
	void TakeSession(ATProfileSession& session);

	// Process new history entries, in chronological order. History entry arrays must always overlap by
	// one entry between updates, and consequently the array must have one more entry than specified by
	// the count. The first N entries are the ones processed; the last one is used only for timestamp
	// delta purposes.
	void Update(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddrs);
	void UpdateBasicLines(const ATCPUTimestampDecoder& tsDecoder, uint32 lineNo, const ATCPUHistoryEntry *const *hents, uint32 n);

private:
	template<ATProfileMode T_ProfileMode>
	void UpdateNonCallGraph(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddrs);
	void UpdateCallGraph(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddrs);
	void ClearContexts();
	void UpdateCounters(uint32 *p, const ATCPUHistoryEntry& he);
	uint32 ScanForFrameBoundary(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n);

	struct HashLink {
		HashLink *mpNext;
		ATProfileRecord mRecord;
	};

	struct CallGraphHashLink {
		CallGraphHashLink *mpNext;
		uint32	mParentContext;
		uint32	mScope;
		uint32	mContext;
	};
	
	bool mbAdjustStackNext;
	bool mbCountersEnabled;
	bool mbKeepNextFrame;
	bool mbGlobalAddressesEnabled;

	uint8 mLastS;
	uint32	mCurrentFrameAddress;
	uint32	mCurrentFrameContext;
	sint32	mCurrentContext;
	bool	mbCallPending;
	uint32 mTotalSamples;
	uint32 mTotalContexts;
	uint32 mStartCycleTime;
	uint32 mStartUnhaltedCycleTime;
	uint32 mNextAutoFrameTime;

	ATProfileMode mProfileMode;
	ATProfileBoundaryRule mBoundaryRule = kATProfileBoundaryRule_None;
	uint32	mBoundaryParam = 0;
	uint32	mBoundaryParam2 = 0;

	ATProfileCounterMode mCounterModes[2] {};

	VDLinearAllocator mHashLinkAllocator { 262144 - 128 };
	VDLinearAllocator mCGHashLinkAllocator { 262144 - 128 };
	ATProfileSession mSession;
	ATProfileFrame *mpCurrentFrame = nullptr;

	HashLink *mpHashTable[256] {};
	HashLink *mpBlockHashTable[256] {};
	CallGraphHashLink *mpCGHashTable[256] {};

	static constexpr uint32 kInvalidStackEntryAddress = ~UINT32_C(0);

	struct NCGStackEntry {
		uint32 mAddress;
		uint32 mContext;
	};

	union {
		NCGStackEntry mNCGStackTable[256];
		sint32	mCGStackTable[256];
	};
};

class ATCPUProfiler final : public IATSchedulerCallback {
	ATCPUProfiler(const ATCPUProfiler&) = delete;
	ATCPUProfiler& operator=(const ATCPUProfiler&) = delete;
public:
	ATCPUProfiler();
	~ATCPUProfiler();

	bool IsRunning() const { return mpUpdateEvent != NULL; }

	void SetBoundaryRule(ATProfileBoundaryRule rule, uint32 param, uint32 param2);
	void SetGlobalAddressesEnabled(bool enable);

	void Init(ATCPUEmulator *cpu, ATCPUEmulatorMemory *mem, ATCPUEmulatorCallbacks *callbacks, ATScheduler *scheduler, ATScheduler *slowScheduler, IATCPUTimestampDecoderProvider *tsdprovider);
	void Start(ATProfileMode mode, ATProfileCounterMode c1, ATProfileCounterMode c2);
	void BeginFrame();
	void EndFrame();
	void End();

	void GetSession(ATProfileSession& session);

private:
	void OnScheduledEvent(uint32 id);
	void Update();
	void AdvanceFrame(bool enableCollection);
	void OpenFrame();
	void CloseFrame();

	IATCPUTimestampDecoderProvider *mpTSDProvider;
	ATCPUEmulator *mpCPU;
	ATCPUEmulatorMemory *mpMemory;
	ATCPUEmulatorCallbacks *mpCallbacks;
	ATScheduler *mpFastScheduler;
	ATScheduler *mpSlowScheduler;
	ATEvent *mpUpdateEvent;

	ATProfileMode mProfileMode;
	uint32 mFramePeriod;
	uint32 mLastHistoryCounter;
	bool mbDropFirstSample;

	ATCPUProfileBuilder mBuilder;
};

#endif	// f_AT_PROFILER_H
