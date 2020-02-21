//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
//	Debugger module - execution history tree builder
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

#ifndef f_AT_ATDEBUGGER_HISTORYTREEBUILDER_H
#define f_AT_ATDEBUGGER_HISTORYTREEBUILDER_H

#include <at/atdebugger/historytree.h>

enum ATDebugDisasmMode : uint8;
struct ATCPUHistoryEntry;

struct ATHistoryTraceInsn {
	uint32 mPC;
	bool mbInterrupt : 1;
	bool mbCall : 1;
	uint8 mS;
	uint8 mOpcode;
	uint8 mPushCount;
};

static_assert(sizeof(ATHistoryTraceInsn) == 8, "Bitfield packing incorrect");

class ATHistoryTreeBuilder {
public:
	void Init(ATHistoryTree *tree);

	void SetCollapseLoops(bool enable) { mbCollapseLoops = enable; }
	void SetCollapseCalls(bool enable) { mbCollapseCalls = enable; }
	void SetCollapseInterrupts(bool enable) { mbCollapseInterrupts = enable; }

	void Reset();

	void BeginUpdate(bool enableInvalidationChecks);
	uint32 EndUpdate(ATHTNode*& last);
	void Update(const ATHistoryTraceInsn *VDRESTRICT htab, uint32 n);

private:
	void ResetStack();
	void RefreshNode(ATHTNode *node);

	static constexpr uint32 kRepeatWindowSize = 8192;
	static constexpr uint32 kRepeatWindowMask = kRepeatWindowSize - 1;
	static constexpr uint32 kMaxNestingDepth = 64;

	ATHistoryTree *mpHistoryTree = nullptr;
	uint32 mRepeatHead = 1;
	uint32 mRepeatTail = 1;
	uint32 mRepeatSumAccum = 0;
	uint32 mRepeatLastBlockSize = 0;
	uint8 mLastS = 0xFF;
	uint32 mEarliestUpdatePos = 0;
	ATHTNode *mpLastNode = nullptr;

	bool	mbCollapseLoops = false;
	bool	mbCollapseCalls = false;
	bool	mbCollapseInterrupts = false;

	struct StackLevel {
		ATHTNode *mpTreeNode;
		int mDepth;
	};

	StackLevel mStackLevels[256] = {};

	uint32 mRepeatData[kRepeatWindowSize * 2] = {};
	uint32 mRepeatDataSum[kRepeatWindowSize] = {};
	uint32 mRepeatHashNext[kRepeatWindowSize] = {};
	ATHTNode *mRepeatTreeInsnNode[kRepeatWindowSize] = {};
	ATHTNode *mRepeatTreeRepeatNode[kRepeatWindowSize] = {};
	uint32 mRepeatHashStart[65536] = {};
};

void ATHistoryTranslateInsn6502(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const he, uint32 n);
void ATHistoryTranslateInsn65C02(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const he, uint32 n);
void ATHistoryTranslateInsn65C816(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const he, uint32 n);
void ATHistoryTranslateInsnZ80(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const he, uint32 n);
void ATHistoryTranslateInsn6809(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *he, uint32 n);

typedef void (*ATHistoryTranslateInsnFn)(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *he, uint32 n);
ATHistoryTranslateInsnFn ATHistoryGetTranslateInsnFn(ATDebugDisasmMode dmode);

#endif
