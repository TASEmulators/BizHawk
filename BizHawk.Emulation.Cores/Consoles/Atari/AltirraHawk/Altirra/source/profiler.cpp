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

#include <stdafx.h>
#include <at/atcore/address.h>
#include <at/atcore/scheduler.h>
#include "cpu.h"
#include "cpumemory.h"
#include "console.h"
#include "profiler.h"

ATProfileSession::~ATProfileSession() {
	for(auto *p : mpFrames)
		delete p;
}

vdnothrow ATProfileSession::ATProfileSession(ATProfileSession&& src) vdnoexcept {
	*this = std::move(src);
}

vdnothrow ATProfileSession& ATProfileSession::operator=(ATProfileSession&& src) vdnoexcept {
	if (!mpFrames.empty()) {
		for(auto *p : mpFrames)
			delete p;

		mpFrames.clear();
	}

	mpFrames = std::move(src.mpFrames);
	mContexts = std::move(src.mContexts);

	mProfileMode = src.mProfileMode;
	mCounterModes = std::move(src.mCounterModes);
	return *this;
}

///////////////////////////////////////////////////////////////////////////

void ATProfileComputeInclusiveStats(
	ATProfileCallGraphInclusiveRecord *dst,
	const ATProfileCallGraphRecord *src,
	const ATProfileCallGraphContext *contexts,
	size_t n)
{
	for(size_t i = n; i; --i) {
		const auto& exRecord = src[i - 1];
		auto& inRecord = dst[i - 1];
		inRecord.mInclusiveCycles += exRecord.mCycles;
		inRecord.mInclusiveUnhaltedCycles += exRecord.mUnhaltedCycles;
		inRecord.mInclusiveInsns += exRecord.mInsns;

		if (i > 4) {
			auto& parentInRecord = dst[contexts[i - 1].mParent];

			parentInRecord.mInclusiveCycles += inRecord.mInclusiveCycles;
			parentInRecord.mInclusiveUnhaltedCycles += inRecord.mInclusiveUnhaltedCycles;
			parentInRecord.mInclusiveInsns += inRecord.mInclusiveInsns;
		}
	}
}

namespace {
	void MergeRecords(ATProfileFrame& dst, const ATProfileSession& srcSession, uint32 start, uint32 end, ATProfileFrame::Records ATProfileFrame::*field) {
		auto& dstRecords = dst.*field;

		vdhashmap<uint32, uint32> addressLookup;

		for(uint32 i = start; i < end; ++i) {
			const auto& srcFrame = *srcSession.mpFrames[i];
			const auto& srcRecords = srcFrame.*field;

			for(const auto& srcRecord : srcRecords) {
				auto r = addressLookup.insert(srcRecord.mAddress);

				if (r.second) {
					uint32 newIndex = (uint32)dstRecords.size();
					r.first->second = newIndex;

					dstRecords.push_back(srcRecord);
				} else {
					auto& dstRecord = dstRecords[r.first->second];

					dstRecord.mCalls += srcRecord.mCalls;
					dstRecord.mInsns += srcRecord.mInsns;
					dstRecord.mCycles += srcRecord.mCycles;
					dstRecord.mUnhaltedCycles += srcRecord.mUnhaltedCycles;

					for(uint32 j = 0; j < vdcountof(srcRecord.mCounters); ++j)
						dstRecord.mCounters[j] += srcRecord.mCounters[j];
				}
			}
		}
	}

	void MergeCallGraphContextRecords(ATProfileFrame& dst, const ATProfileSession& srcSession, uint32 start, uint32 end) {
		auto& dstRecords = dst.mCallGraphRecords;

		for(uint32 i = start; i < end; ++i) {
			const auto& srcFrame = *srcSession.mpFrames[i];
			const auto& srcRecords = srcFrame.mCallGraphRecords;

			const uint32 srcCount = (uint32)srcRecords.size();
			const uint32 dstCount = (uint32)dstRecords.size();
			const uint32 minCount = std::min(srcCount, dstCount);
			
			dstRecords.resize(std::max(srcCount, dstCount));

			for(uint32 j=0; j<minCount; ++j) {
				auto& dstRecord = dstRecords[j];
				const auto& srcRecord = srcRecords[j];

				dstRecord.mInsns += srcRecord.mInsns;
				dstRecord.mCycles += srcRecord.mCycles;
				dstRecord.mUnhaltedCycles += srcRecord.mUnhaltedCycles;
				dstRecord.mCalls += srcRecord.mCalls;
			}

			if (srcCount > dstCount)
				std::copy(srcRecords.begin() + dstCount, srcRecords.end(), dstRecords.begin() + dstCount);
		}
	}
}

void ATProfileMergeFrames(const ATProfileSession& session, uint32 start, uint32 end, ATProfileMergedFrame **mergedFrameOut) {
	vdrefptr<ATProfileMergedFrame> mergedFrame { new ATProfileMergedFrame };

	MergeRecords(*mergedFrame, session, start, end, &ATProfileFrame::mRecords);
	MergeRecords(*mergedFrame, session, start, end, &ATProfileFrame::mBlockRecords);
	MergeCallGraphContextRecords(*mergedFrame, session, start, end);

	mergedFrame->mTotalCycles = 0;
	mergedFrame->mTotalUnhaltedCycles = 0;
	mergedFrame->mTotalInsns = 0;

	for(uint32 i = start; i < end; ++i) {
		const auto& frame = *session.mpFrames[i];

		mergedFrame->mTotalCycles += frame.mTotalCycles;
		mergedFrame->mTotalUnhaltedCycles += frame.mTotalUnhaltedCycles;
		mergedFrame->mTotalInsns += frame.mTotalInsns;
	}

	const size_t numRecs = mergedFrame->mCallGraphRecords.size();
	mergedFrame->mInclusiveRecords.resize(numRecs, {});

	ATProfileComputeInclusiveStats(mergedFrame->mInclusiveRecords.data(), mergedFrame->mCallGraphRecords.data(), session.mContexts.data(), numRecs);

	*mergedFrameOut = mergedFrame.release();
}

///////////////////////////////////////////////////////////////////////////

ATCPUProfileBuilder::ATCPUProfileBuilder() {
}

ATCPUProfileBuilder::~ATCPUProfileBuilder() {
	ClearContexts();
}

void ATCPUProfileBuilder::Init(ATProfileMode mode, ATProfileCounterMode c1, ATProfileCounterMode c2) {
	mProfileMode = mode;
	mbAdjustStackNext = false;
	mbKeepNextFrame = true;
	mbGlobalAddressesEnabled = false;
	mCurrentFrameAddress = ~UINT32_C(0);
	mCurrentFrameContext = 0;
	mCurrentContext = 0;
	mbCallPending = false;
	mTotalContexts = 0;

	mbCountersEnabled = c1 || c2;
	mCounterModes[0] = c1;
	mCounterModes[1] = c2;

	if (mode == kATProfileMode_CallGraph)
		std::fill(mCGStackTable, mCGStackTable+256, -1);
	else
		std::fill(mNCGStackTable, mNCGStackTable+256, NCGStackEntry { kInvalidStackEntryAddress });

	mSession.mProfileMode = mode;

	if (c1)
		mSession.mCounterModes.push_back(c1);

	if (c2)
		mSession.mCounterModes.push_back(c2);

	ClearContexts();

	if (mode == kATProfileMode_CallGraph) {
		mTotalContexts = 4;

		mSession.mContexts.resize(4);
		mSession.mContexts[0] = ATProfileCallGraphContext { 0, 0 };
		mSession.mContexts[1] = ATProfileCallGraphContext { 0, 0x4000000 };
		mSession.mContexts[2] = ATProfileCallGraphContext { 0, 0x6000000 };
		mSession.mContexts[3] = ATProfileCallGraphContext { 0, 0x8000000 };
	} else {
		mSession.mContexts.clear();
	}
}

void ATCPUProfileBuilder::SetBoundaryRule(ATProfileBoundaryRule rule, uint32 param, uint32 param2) {
	mBoundaryRule = rule;
	mBoundaryParam = param;
	mBoundaryParam2 = param2;
}

void ATCPUProfileBuilder::SetGlobalAddressesEnabled(bool enable) {
	mbGlobalAddressesEnabled = enable;
}

void ATCPUProfileBuilder::SetS(uint8 s) {
	mLastS = s;
}

void ATCPUProfileBuilder::AdvanceFrame(uint32 cycle, uint32 unhaltedCycle, bool enableCollection, const ATCPUTimestampDecoder& tsDecoder) {
	CloseFrame(cycle, unhaltedCycle, mbKeepNextFrame);
	OpenFrame(cycle, unhaltedCycle, tsDecoder);
	mbKeepNextFrame = enableCollection;
}

void ATCPUProfileBuilder::OpenFrame(uint32 cycle, uint32 unhaltedCycle, const ATCPUTimestampDecoder& tsDecoder) {
	VDASSERT(!mpCurrentFrame);

	mTotalSamples = 0;
	mStartCycleTime = cycle;
	mStartUnhaltedCycleTime = unhaltedCycle;
	mNextAutoFrameTime = tsDecoder.GetFrameStartTime(cycle - 248*114) + 248*114 + tsDecoder.mCyclesPerFrame;

	mSession.mpFrames.push_back(nullptr);
	mpCurrentFrame = new ATProfileFrame;
	mSession.mpFrames.back() = mpCurrentFrame;

	if (mProfileMode == kATProfileMode_CallGraph) {
		mpCurrentFrame->mCallGraphRecords.resize(4);

		for(int i=0; i<4; ++i) {
			ATProfileCallGraphRecord& rmain = mpCurrentFrame->mCallGraphRecords[i];
			memset(&rmain, 0, sizeof rmain);
		}
	}

	mpCurrentFrame->mCallGraphRecords.resize(mTotalContexts, ATProfileCallGraphRecord());
}

void ATCPUProfileBuilder::CloseFrame(uint32 cycle, uint32 unhaltedCycle, bool keepFrame) {
	VDASSERT(mpCurrentFrame);

	ATProfileFrame& frame = *mpCurrentFrame;

	frame.mTotalCycles = cycle - mStartCycleTime;
	frame.mTotalUnhaltedCycles = unhaltedCycle - mStartUnhaltedCycleTime;
	frame.mTotalInsns = mTotalSamples;

	for(const HashLink *hl : mpHashTable) {
		for(; hl; hl = hl->mpNext)
			frame.mRecords.push_back(hl->mRecord);
	}

	for(const HashLink *hl : mpBlockHashTable) {
		for(; hl; hl = hl->mpNext)
			frame.mBlockRecords.push_back(hl->mRecord);
	}

	if (!keepFrame) {
		mSession.mpFrames.pop_back();
		delete mpCurrentFrame;
	}

	mpCurrentFrame = nullptr;

	std::fill(std::begin(mpHashTable), std::end(mpHashTable), nullptr);
	std::fill(std::begin(mpBlockHashTable), std::end(mpBlockHashTable), nullptr);

	mHashLinkAllocator.Clear();
}

void ATCPUProfileBuilder::Finalize() {
	ClearContexts();
}

void ATCPUProfileBuilder::TakeSession(ATProfileSession& session) {
	session = std::move(mSession);
}

void ATCPUProfileBuilder::Update(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddrs) {
	if (!mbGlobalAddressesEnabled)
		useGlobalAddrs = false;

	VDASSERT(mProfileMode != kATProfileMode_BasicLines);

	switch(mProfileMode) {
		case kATProfileMode_Insns:
			UpdateNonCallGraph<kATProfileMode_Insns>(tsDecoder, hents, n, useGlobalAddrs);
			break;

		case kATProfileMode_Functions:
			UpdateNonCallGraph<kATProfileMode_Functions>(tsDecoder, hents, n, useGlobalAddrs);
			break;

		case kATProfileMode_BasicBlock:
			UpdateNonCallGraph<kATProfileMode_BasicBlock>(tsDecoder, hents, n, useGlobalAddrs);
			break;

		case kATProfileMode_CallGraph:
			UpdateCallGraph(tsDecoder, hents, n, useGlobalAddrs);
			break;
	}
}

template<ATProfileMode T_ProfileMode>
void ATCPUProfileBuilder::UpdateNonCallGraph(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddrs) {
	static_assert(T_ProfileMode == kATProfileMode_Insns
		|| T_ProfileMode == kATProfileMode_Functions
		|| T_ProfileMode == kATProfileMode_BasicBlock
		);

	while(n) {
		uint32 leftInSegment = ScanForFrameBoundary(tsDecoder, hents, n);

		mTotalSamples += leftInSegment;
		n -= leftInSegment;

		while(leftInSegment--) {
			const ATCPUHistoryEntry *hentp = *hents++;
			const ATCPUHistoryEntry *hentn = *hents;

			uint32 cycles = (uint16)(hentn->mCycle - hentp->mCycle);
			uint32 unhaltedCycles = (uint16)(hentn->mUnhaltedCycle - hentp->mUnhaltedCycle);
			uint32 extpc = hentp->mPC + (hentp->mK << 16);
			
			if (useGlobalAddrs) {
				extpc += hentp->mGlobalPCBase;
			}

			uint32 addr = extpc;
			uint32 addrContext = 0;

			if constexpr (T_ProfileMode == kATProfileMode_Insns) {
				if (hentp->mP & AT6502::kFlagI)
					addrContext = kATProfileContext_Interrupt;
			} else {
				bool adjustStack = mbAdjustStackNext;

				// In current versions, IRQ and NMI have dedicated history entries, so we
				// should treat them like a call instruction and only adjust context on the
				// next entry.
				mbAdjustStackNext = hentp->mbIRQ || hentp->mbNMI;

				uint8 opcode = hentp->mOpcode[0];
				switch(opcode) {
					case 0x20:		// JSR
					case 0x60:		// RTS
					case 0x40:		// RTI
					case 0x6C:		// JMP (abs)
						mbAdjustStackNext = true;
						break;
				}

				bool isCall = false;

				if constexpr (T_ProfileMode == kATProfileMode_BasicBlock) {
					if (mbCallPending) {
						mbCallPending = false;

						mCurrentFrameAddress = extpc;
						isCall = true;
					}
				}

				if (adjustStack) {
					uint32 newFrameContext = mCurrentFrameContext;
					if (hentp->mbNMI) {
						if (tsDecoder.IsInterruptPositionVBI(hentp->mCycle))
							newFrameContext = kATProfileContext_VBI;
						else
							newFrameContext = kATProfileContext_DLI;
					} else if (hentp->mbIRQ) {
						newFrameContext = kATProfileContext_IRQ;
					} else if (!(hentp->mP & AT6502::kFlagI)) {
						newFrameContext = kATProfileContext_Main;
					}

					sint8 sdir = hentp->mS - mLastS;
					if (sdir > 0) {
						// pop
						do {
							uint32 prevFrame = mNCGStackTable[mLastS].mAddress;

							if (prevFrame != kInvalidStackEntryAddress) {
								mCurrentFrameAddress = prevFrame;
								mNCGStackTable[mLastS].mAddress = kInvalidStackEntryAddress;
							}
						} while(++mLastS != hentp->mS);
					} else if (sdir < 0) {
						// push
						while(--mLastS != hentp->mS) {
							mNCGStackTable[mLastS].mAddress = kInvalidStackEntryAddress;
						}

						mNCGStackTable[mLastS] = NCGStackEntry { mCurrentFrameAddress, mCurrentFrameContext };
						mCurrentFrameAddress = extpc;
						mCurrentFrameContext = newFrameContext;
						isCall = true;
					} else {
						mCurrentFrameAddress = extpc;
						mCurrentFrameContext = newFrameContext;
						isCall = true;
					}
				}

				if constexpr (T_ProfileMode == kATProfileMode_BasicBlock) {
					switch(opcode) {
						case 0x20:		// JSR
						case 0x60:		// RTS
						case 0x40:		// RTI
						case 0x4C:		// JMP
						case 0x6C:		// JMP (abs)
						case 0x10:		// Bcc
						case 0x30:
						case 0x50:
						case 0x70:
						case 0x90:
						case 0xB0:
						case 0xD0:
						case 0xF0:
							mbCallPending = true;
							break;
					}
				}

				const uint32 frameAddr = mCurrentFrameAddress;
				const uint32 frameAddrContext = mCurrentFrameContext;

				uint32 hc = frameAddr & 0xFF;
				HashLink *hh = mpBlockHashTable[hc];
				HashLink *hl = hh;

				for(; hl; hl = hl->mpNext) {
					if (hl->mRecord.mAddress == frameAddr && hl->mRecord.mContext == frameAddrContext)
						break;
				}

				if (!hl) {
					hl = mHashLinkAllocator.Allocate<HashLink>();
					hl->mpNext = hh;
					hl->mRecord.mAddress = frameAddr;
					hl->mRecord.mContext = frameAddrContext;
					hl->mRecord.mCycles = 0;
					hl->mRecord.mUnhaltedCycles = 0;
					hl->mRecord.mInsns = 0;
					hl->mRecord.mModeBits = (hentp->mP >> 4) & 3;
					hl->mRecord.mEmulationMode = hentp->mbEmulation;
					hl->mRecord.mCalls = 0;
					memset(hl->mRecord.mCounters, 0, sizeof hl->mRecord.mCounters);
					mpBlockHashTable[hc] = hl;
				}

				hl->mRecord.mCycles += cycles;
				hl->mRecord.mUnhaltedCycles += unhaltedCycles;
				++hl->mRecord.mInsns;

				if (isCall)
					++hl->mRecord.mCalls;
			}

			uint32 hc = addr & 0xFF;
			HashLink *hh = mpHashTable[hc];
			HashLink *hl = hh;

			for(; hl; hl = hl->mpNext) {
				if (hl->mRecord.mAddress == addr && hl->mRecord.mContext == addrContext)
					break;
			}

			if (!hl) {
				hl = mHashLinkAllocator.Allocate<HashLink>();
				hl->mpNext = hh;
				hl->mRecord.mAddress = addr;
				hl->mRecord.mContext = addrContext;
				hl->mRecord.mCycles = 0;
				hl->mRecord.mUnhaltedCycles = 0;
				hl->mRecord.mInsns = 0;
				hl->mRecord.mModeBits = (hentp->mP >> 4) & 3;
				hl->mRecord.mEmulationMode = hentp->mbEmulation;
				hl->mRecord.mCalls = 0;
				memset(hl->mRecord.mCounters, 0, sizeof hl->mRecord.mCounters);
				mpHashTable[hc] = hl;
			}

			hl->mRecord.mCycles += cycles;
			hl->mRecord.mUnhaltedCycles += unhaltedCycles;
			++hl->mRecord.mInsns;

			if (mbCountersEnabled)
				UpdateCounters(hl->mRecord.mCounters, *hentp);
		}
	}
}

void ATCPUProfileBuilder::UpdateBasicLines(const ATCPUTimestampDecoder& tsDecoder, uint32 lineNo, const ATCPUHistoryEntry *const *hents, uint32 n) {
	VDASSERT(mProfileMode == kATProfileMode_BasicLines);

	while(n) {
		uint32 leftInSegment = ScanForFrameBoundary(tsDecoder, hents, n);

		mTotalSamples += leftInSegment;
		n -= leftInSegment;

		while(leftInSegment--) {
			const ATCPUHistoryEntry *hentp = *hents++;
			const ATCPUHistoryEntry *hentn = *hents;
			const uint32 cycles = (uint16)(hentn->mCycle - hentp->mCycle);
			const uint32 unhaltedCycles = (uint16)(hentn->mUnhaltedCycle - hentp->mUnhaltedCycle);

			const uint32 hc = lineNo & 0xFF;
			HashLink *hh = mpHashTable[hc];
			HashLink *hl = hh;

			for(; hl; hl = hl->mpNext) {
				if (hl->mRecord.mAddress == lineNo)
					break;
			}

			if (!hl) {
				hl = mHashLinkAllocator.Allocate<HashLink>();
				hl->mpNext = hh;
				hl->mRecord.mAddress = lineNo;
				hl->mRecord.mCycles = 0;
				hl->mRecord.mUnhaltedCycles = 0;
				hl->mRecord.mInsns = 0;
				hl->mRecord.mModeBits = (hentp->mP >> 4) & 3;
				hl->mRecord.mEmulationMode = hentp->mbEmulation;
				hl->mRecord.mCalls = 0;
				memset(hl->mRecord.mCounters, 0, sizeof hl->mRecord.mCounters);
				mpHashTable[hc] = hl;
			}

			hl->mRecord.mCycles += cycles;
			hl->mRecord.mUnhaltedCycles += unhaltedCycles;
			++hl->mRecord.mInsns;

			if (mbCountersEnabled)
				UpdateCounters(hl->mRecord.mCounters, *hentp);

			hentp = hentn;
		}
	}
}

void ATCPUProfileBuilder::UpdateCallGraph(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n, bool useGlobalAddresses) {
	while(n) {
		uint32 leftInSegment = ScanForFrameBoundary(tsDecoder, hents, n);
		n -= leftInSegment;

		mTotalSamples += leftInSegment;

		while(leftInSegment--) {
			const ATCPUHistoryEntry *hentp = *hents++;
			const ATCPUHistoryEntry *hentn = *hents;
			uint32 cycles = (uint16)(hentn->mCycle - hentp->mCycle);
			uint32 unhaltedCycles = (uint16)(hentn->mUnhaltedCycle - hentp->mUnhaltedCycle);

			bool adjustStack = mbAdjustStackNext || hentp->mbIRQ || hentp->mbNMI;
			mbAdjustStackNext = false;

			uint8 opcode = hentp->mOpcode[0];
			switch(opcode) {
				case 0x20:		// JSR
				case 0x60:		// RTS
				case 0x40:		// RTI
				case 0x6C:		// JMP (abs)
					mbAdjustStackNext = true;
					break;
			}

			if (adjustStack) {
				sint8 sdir = hentp->mS - mLastS;
				if (sdir > 0) {
					// pop
					do {
						sint32 prevContext = mCGStackTable[mLastS];

						if (prevContext >= 0) {
							mCurrentContext = prevContext;
							mCGStackTable[mLastS] = -1;
						}
					} while(++mLastS != hentp->mS);
				} else {
					if (sdir < 0) {
						// push
						while(--mLastS != hentp->mS) {
							mCGStackTable[mLastS] = -1;
						}

						mCGStackTable[mLastS] = mCurrentContext;
					}

					if (hentp->mbNMI) {
						if (tsDecoder.IsInterruptPositionVBI(hentp->mCycle))
							mCurrentContext = 2;
						else
							mCurrentContext = 3;
					} else if (hentp->mbIRQ)
						mCurrentContext = 1;

					const uint32 newScope = hentp->mPC + (hentp->mK << 16) + (useGlobalAddresses ? hentp->mGlobalPCBase : 0);
					const uint32 hc = newScope & 0xFF;
					CallGraphHashLink *hh = mpCGHashTable[hc];
					CallGraphHashLink *hl = hh;

					for(; hl; hl = hl->mpNext) {
						if (hl->mScope == newScope && hl->mParentContext == mCurrentContext)
							break;
					}

					if (!hl) {
						hl = mCGHashLinkAllocator.Allocate<CallGraphHashLink>();
						hl->mpNext = hh;
						hl->mContext = (uint32)mpCurrentFrame->mCallGraphRecords.size();
						hl->mParentContext = mCurrentContext;
						hl->mScope = newScope;
						mpCGHashTable[hc] = hl;

						ATProfileCallGraphRecord& cgr = mpCurrentFrame->mCallGraphRecords.push_back();
						memset(&cgr, 0, sizeof cgr);

						mSession.mContexts.push_back(ATProfileCallGraphContext { (uint32)mCurrentContext, newScope });

						++mTotalContexts;
					}

					mCurrentContext = hl->mContext;
					++mpCurrentFrame->mCallGraphRecords[mCurrentContext].mCalls;
				}
			}

			auto& cgRecord = mpCurrentFrame->mCallGraphRecords[mCurrentContext];
			cgRecord.mCycles += cycles;
			cgRecord.mUnhaltedCycles += unhaltedCycles;
			++cgRecord.mInsns;

			uint32 addr = hentp->mPC + (hentp->mK << 16) + (useGlobalAddresses ? hentp->mGlobalPCBase : 0);
			uint32 hc = addr & 0xFF;
			HashLink *hh = mpHashTable[hc];
			HashLink *hl = hh;

			for(; hl; hl = hl->mpNext) {
				if (hl->mRecord.mAddress == addr)
					break;
			}

			if (!hl) {
				hl = mHashLinkAllocator.Allocate<HashLink>();
				hl->mpNext = hh;
				hl->mRecord.mAddress = addr;
				hl->mRecord.mCycles = 0;
				hl->mRecord.mUnhaltedCycles = 0;
				hl->mRecord.mInsns = 0;
				hl->mRecord.mCalls = 0;
				hl->mRecord.mModeBits = (hentp->mP >> 4) & 3;
				hl->mRecord.mEmulationMode = hentp->mbEmulation;
				memset(hl->mRecord.mCounters, 0, sizeof hl->mRecord.mCounters);
				mpHashTable[hc] = hl;
			}

			hl->mRecord.mCycles += cycles;
			hl->mRecord.mUnhaltedCycles += unhaltedCycles;
			++hl->mRecord.mInsns;
		}
	}
}

void ATCPUProfileBuilder::ClearContexts() {
	std::fill(std::begin(mpCGHashTable), std::end(mpCGHashTable), nullptr);

	mCGHashLinkAllocator.Clear();
}

void ATCPUProfileBuilder::UpdateCounters(uint32 *p, const ATCPUHistoryEntry& he) {
	static constexpr struct BranchOpInfo {
		uint8 mFlagsAnd;
		uint8 mFlagsXor;
	} kBranchOps[8]={
		{ AT6502::kFlagN, AT6502::kFlagN	},		// BPL
		{ AT6502::kFlagN, 0					},		// BMI
		{ AT6502::kFlagV, AT6502::kFlagV	},		// BVC
		{ AT6502::kFlagV, 0					},		// BVS
		{ AT6502::kFlagC, AT6502::kFlagC	},		// BCC
		{ AT6502::kFlagC, 0					},		// BCS
		{ AT6502::kFlagZ, AT6502::kFlagZ	},		// BNE
		{ AT6502::kFlagZ, 0					},		// BEQ
	};

	for(int i=0; i<vdcountof(mCounterModes); ++i) {
		const uint8 opcode = he.mOpcode[0];

		switch(mCounterModes[i]) {
			case kATProfileCounterMode_BranchTaken:
				if ((opcode & 0x1F) == 0x10) {
					const BranchOpInfo& info = kBranchOps[opcode >> 5];

					if ((he.mP & info.mFlagsAnd) ^ info.mFlagsXor)
						++p[i];
				}
				break;

			case kATProfileCounterMode_BranchNotTaken:
				if ((opcode & 0x1F) == 0x10) {
					const BranchOpInfo& info = kBranchOps[opcode >> 5];

					if (!((he.mP & info.mFlagsAnd) ^ info.mFlagsXor))
						++p[i];
				}
				break;

			case kATProfileCounterMode_PageCrossing:
				{
					switch(opcode) {
						case 0x10:	// BPL rel
						case 0x30:	// BMI rel
						case 0x50:	// BVC
						case 0x70:	// BVS
						case 0x90:	// BCC rel8
						case 0xB0:	// BCS rel8
						case 0xD0:	// BNE rel8
						case 0xF0:	// BEQ rel8
							if (((he.mPC + 2) ^ ((he.mPC + 2) + (sint32)(sint8)he.mOpcode[1])) & 0xFF00) {
								const auto& branchOpInfo = kBranchOps[opcode >> 5];

								if ((he.mP & branchOpInfo.mFlagsAnd) ^ branchOpInfo.mFlagsXor)
									++p[i];
							}
							break;

						case 0x1D:	// ORA abs,X
						case 0x3D:	// AND abs,X
						case 0x5D:	// EOR abs,X
						case 0x7D:	// ADC abs,X
						case 0xBC:	// LDY abs,X
						case 0xBD:	// LDA abs,X
						case 0xDD:	// CMP abs,X
						case 0xFD:	// SBC abs,X
							if ((uint32)he.mOpcode[1] + he.mX >= 0x100)
								++p[i];
							break;

						case 0x11:	// ORA (zp),Y
						case 0x31:	// AND (zp),Y
						case 0x51:	// EOR (zp),Y
						case 0x71:	// ADC (zp),Y
						case 0xB1:	// LDA (zp),Y
						case 0xD1:	// CMP (zp),Y
						case 0xF1:	// SBC (zp),Y
							if ((he.mEA & 0xFF) < he.mY)
								++p[i];
							break;

						case 0x19:	// ORA abs,Y
						case 0x39:	// AND abs,Y
						case 0x59:	// EOR abs,Y
						case 0x79:	// ADC abs,Y
						case 0xB9:	// LDA abs,Y
						case 0xBE:	// LDX abs,Y
						case 0xD9:	// CMP abs,Y
						case 0xF9:	// SBC abs,Y
							if ((uint32)he.mOpcode[1] + he.mY >= 0x100)
								++p[i];
							break;
					}
				}
				break;

			case kATProfileCounterMode_RedundantOp:
				switch(opcode) {
					case 0x18:	// CLC
						if (!(he.mP & AT6502::kFlagC))
							++p[i];
						break;
					case 0x38:	// SEC
						if (he.mP & AT6502::kFlagC)
							++p[i];
						break;
					case 0x58:	// CLI
						if (!(he.mP & AT6502::kFlagI))
							++p[i];
						break;
					case 0x78:	// SEI
						if (he.mP & AT6502::kFlagI)
							++p[i];
						break;
					case 0xB8:	// CLV
						if (!(he.mP & AT6502::kFlagV))
							++p[i];
						break;
					case 0xD8:	// CLD
						if (!(he.mP & AT6502::kFlagD))
							++p[i];
						break;
					case 0xF8:	// SED
						if (he.mP & AT6502::kFlagD)
							++p[i];
						break;

					case 0xA9:	// LDA #imm
						if (he.mOpcode[1] == he.mA)
							++p[i];
						break;

					case 0xA2:	// LDX #imm
						if (he.mOpcode[1] == he.mX)
							++p[i];
						break;

					case 0xA0:	// LDY #imm
						if (he.mOpcode[1] == he.mY)
							++p[i];
						break;

				}
				break;
		}
	}
}

uint32 ATCPUProfileBuilder::ScanForFrameBoundary(const ATCPUTimestampDecoder& tsDecoder, const ATCPUHistoryEntry *const *hents, uint32 n) {
	switch(mBoundaryRule) {
		case kATProfileBoundaryRule_None:
		default:
			break;

		case kATProfileBoundaryRule_VBlank:
			{
				for(uint32 i=0; i<n; ++i) {
					const ATCPUHistoryEntry *hent = hents[i];

					if (hent->mCycle - mNextAutoFrameTime < (1U << 31)) {
						if (i)
							return i;

						AdvanceFrame(hent->mCycle, hent->mUnhaltedCycle, true, tsDecoder);
					}
				}
			}
			break;

		case kATProfileBoundaryRule_PCAddress:
			for(uint32 i=0; i<n; ++i) {
				const ATCPUHistoryEntry *hent = hents[i];

				if (hent->mPC == mBoundaryParam) {
					if (i)
						return i;

					AdvanceFrame(hent->mCycle, hent->mUnhaltedCycle, true, tsDecoder);
				} else if (hent->mPC == mBoundaryParam2) {
					if (i)
						return i;

					AdvanceFrame(hent->mCycle, hent->mUnhaltedCycle, false, tsDecoder);
				}
			}
			break;

		case kATProfileBoundaryRule_PCAddressFunction:
			for(uint32 i=0; i<n; ++i) {
				const ATCPUHistoryEntry *hent = hents[i];

				if (hent->mPC == mBoundaryParam) {
					if (i)
						return i;

					AdvanceFrame(hent->mCycle, hent->mUnhaltedCycle, true, tsDecoder);
					mBoundaryParam2 = hent->mS;
				} else if (((hent->mS - mBoundaryParam2 - 1) & 0xFF) < 0x0F) {
					if (i)
						return i;

					AdvanceFrame(hent->mCycle, hent->mUnhaltedCycle, false, tsDecoder);
				}
			}
			break;
	}

	return n;
}

///////////////////////////////////////////////////////////////////////////

ATCPUProfiler::ATCPUProfiler()
	: mpTSDProvider(nullptr)
	, mpCPU(nullptr)
	, mpMemory(nullptr)
	, mpCallbacks(nullptr)
	, mpFastScheduler(nullptr)
	, mpSlowScheduler(nullptr)
	, mpUpdateEvent(nullptr)
{
}

ATCPUProfiler::~ATCPUProfiler() {
}

void ATCPUProfiler::SetBoundaryRule(ATProfileBoundaryRule rule, uint32 param, uint32 param2) {
	mBuilder.SetBoundaryRule(rule, param, param2);
}

void ATCPUProfiler::SetGlobalAddressesEnabled(bool enable) {
	mBuilder.SetGlobalAddressesEnabled(enable);
}

void ATCPUProfiler::Init(ATCPUEmulator *cpu, ATCPUEmulatorMemory *mem, ATCPUEmulatorCallbacks *callbacks, ATScheduler *scheduler, ATScheduler *slowScheduler, IATCPUTimestampDecoderProvider *tsdprovider) {
	mpTSDProvider = tsdprovider;
	mpCPU = cpu;
	mpMemory = mem;
	mpCallbacks = callbacks;
	mpFastScheduler = scheduler;
	mpSlowScheduler = slowScheduler;
}

void ATCPUProfiler::Start(ATProfileMode mode, ATProfileCounterMode c1, ATProfileCounterMode c2) {
	mBuilder.Init(mode, c1, c2);
	mBuilder.SetS(mpCPU->GetS());

	mProfileMode = mode;
	mLastHistoryCounter = mpCPU->GetHistoryCounter();
	mbDropFirstSample = true;

	if (mode == kATProfileMode_BasicLines)
		mpUpdateEvent = mpSlowScheduler->AddEvent(2, this, 2);
	else
		mpUpdateEvent = mpSlowScheduler->AddEvent(32, this, 1);

	OpenFrame();
}

void ATCPUProfiler::BeginFrame() {
	Update();
	AdvanceFrame(true);
}

void ATCPUProfiler::EndFrame() {
	Update();
	AdvanceFrame(false);
}

void ATCPUProfiler::End() {
	Update();

	if (mpUpdateEvent) {
		mpSlowScheduler->RemoveEvent(mpUpdateEvent);
		mpUpdateEvent = NULL;
	}

	CloseFrame();

	mBuilder.Finalize();
}

void ATCPUProfiler::GetSession(ATProfileSession& session) {
	mBuilder.TakeSession(session);
}

void ATCPUProfiler::OnScheduledEvent(uint32 id) {
	if (id == 1) {
		mpUpdateEvent = mpSlowScheduler->AddEvent(32, this, 1);

		Update();
	} else if (id == 2) {
		mpUpdateEvent = mpSlowScheduler->AddEvent(2, this, 1);

		Update();
	}
}

void ATCPUProfiler::Update() {
	uint32 nextHistoryCounter = mpCPU->GetHistoryCounter();
	uint32 count = (nextHistoryCounter - mLastHistoryCounter) & (mpCPU->GetHistoryLength() - 1);
	mLastHistoryCounter = nextHistoryCounter;

	if (!count)
		return;

	// If we have not taken any samples yet, toss the first sample. This is because we
	// always need to have one additional sample due to differencing for timestamps, so
	// we can only process one less than the total number of available samples.
	if (mbDropFirstSample) {
		mbDropFirstSample = false;

		--count;
	}

	const ATCPUHistoryEntry *heptrs[256];
	const ATCPUTimestampDecoder& tsDecoder = mpTSDProvider->GetTimestampDecoder();
	uint32 lineNo = 0;

	if (mProfileMode == kATProfileMode_BasicLines) {
		uint32 lineAddr = (uint32)mpMemory->DebugReadByte(0x8A) + ((uint32)mpMemory->DebugReadByte(0x8B) << 8);
		lineNo = (uint32)mpMemory->DebugReadByte(lineAddr) + ((uint32)mpMemory->DebugReadByte(lineAddr + 1) << 8);
	}

	const bool useGlobalAddrs = (mpCPU->GetCPUMode() == kATCPUMode_6502);

	while(count) {
		// Note that we are constrained to always overlap by one entry, so we consume
		// one less entry than we read.
		const uint32 toRead = std::min<uint32>((uint32)vdcountof(heptrs) - 1, count);

		for(uint32 i = 0; i <= toRead; ++i)
			heptrs[i] = &mpCPU->GetHistory(count - i);

		if (mProfileMode == kATProfileMode_BasicLines)
			mBuilder.UpdateBasicLines(tsDecoder, lineNo, heptrs, toRead);
		else
			mBuilder.Update(tsDecoder, heptrs, toRead, useGlobalAddrs);

		count -= toRead;
	}
}

void ATCPUProfiler::AdvanceFrame(bool enableCollection) {
	mBuilder.AdvanceFrame(mpCallbacks->CPUGetCycle(), mpCallbacks->CPUGetUnhaltedCycle(), enableCollection, mpTSDProvider->GetTimestampDecoder());
}

void ATCPUProfiler::OpenFrame() {
	mBuilder.OpenFrame(mpCallbacks->CPUGetCycle(), mpCallbacks->CPUGetUnhaltedCycle(), mpTSDProvider->GetTimestampDecoder());
}

void ATCPUProfiler::CloseFrame() {
	mBuilder.CloseFrame(mpCallbacks->CPUGetCycle(), mpCallbacks->CPUGetUnhaltedCycle(), true);
}
