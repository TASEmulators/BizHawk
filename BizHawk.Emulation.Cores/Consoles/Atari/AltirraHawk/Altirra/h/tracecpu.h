//	Altirra - Atari 800/800XL/5200 emulator
//	Execution trace data structures - CPU history tracing
//	Copyright (C) 2009-2017 Avery Lee
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

#ifndef f_AT_TRACECPU_H
#define f_AT_TRACECPU_H

#include <vd2/system/linearalloc.h>
#include <at/atcpu/history.h>
#include <at/atdebugger/target.h>
#include "trace.h"

class ATTraceChannelCPUHistory final : public vdrefcounted<IATTraceChannel> {
public:
	static const uint32 kTypeID = 'tcch';

	ATTraceChannelCPUHistory(uint64 tickOffset, double tickScale, const wchar_t *name, ATDebugDisasmMode disasmMode, uint32 subCycles, ATTraceMemoryTracker *memTracker);

	ATDebugDisasmMode GetDisasmMode() const { return mDisasmMode; }
	uint32 GetSubCycles() const { return mSubCycles; }

	void AddEvent(uint64 tick, const ATCPUHistoryEntry& he);

	void *AsInterface(uint32 iid) override;

	const wchar_t *GetName() const override;
	double GetDuration() const override;
	bool IsEmpty() const override;
	void StartIteration(double startTime, double endTime, double eventThreshold) override;
	bool GetNextEvent(ATTraceEvent& ev) override final;

	double GetSecondsPerTick() const { return mTickScale; }
	void StartHistoryIteration(double startTime, sint32 eventOffset);
	uint32 ReadHistoryEvents(const ATCPUHistoryEntry **ppEvents, uint32 offset, uint32 n);
	uint32 FindEvent(double t);
	double GetEventTime(uint32 index);
	uint64 GetTraceSize() const { return mTraceSize; }
	uint32 GetEventCount() const { return mEventCount; }

private:
	static constexpr uint32 kBlockSizeBits = 6;
	static constexpr uint32 kBlockSize = 1 << kBlockSizeBits;
	static constexpr uint32 kUnpackedSlots = 8;

	struct Event {
		double mTime;
		ATCPUHistoryEntry mInsnInfo;
	};

	struct PackedEventBlock {
		double mTime;
		void *mpData;
	};

	void PackBlock();
	const ATCPUHistoryEntry *UnpackBlock(uint32 id);

	vdfastvector<PackedEventBlock> mPackedEventBlocks;
	vdfastvector<uint8> mUnpackMap;

	double mTailBlockTime = 0;
	uint32 mTailOffset = 0;
	uint8 mLRUClock = 0;
	uint32 mEventCount = 0;
	uint64 mTraceSize = 0;

	uint32 mIterPos = 0;
	double mTickScale = 0;
	uint64 mTickOffset = 0;
	ATDebugDisasmMode mDisasmMode = {};
	uint32 mSubCycles = 0;
	VDStringW mName;

	uint32 mPseudoLRU[kUnpackedSlots] = {};
	uint32 mUnpackedBlockIds[kUnpackedSlots] = {};
	ATCPUHistoryEntry mUnpackedBlocks[kUnpackedSlots][kBlockSize] = {};
	ATCPUHistoryEntry mTailBlock[kBlockSize] = {};

	ATTraceMemoryTracker *mpMemTracker = nullptr;
	VDLinearAllocator mBlockAllocator { 64*1024 };

	static constexpr bool kCompressionEnabled = true;
};

#endif
