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

#include "stdafx.h"
#include <at/atcpu/history.h>
#include <at/atdebugger/historytreebuilder.h>
#include <at/atdebugger/target.h>

//#define VERIFY_HISTORY_TREE 1

void ATHistoryTreeBuilder::Init(ATHistoryTree *tree) {
	mpHistoryTree = tree;

	Reset();
}

void ATHistoryTreeBuilder::Reset() {
	memset(mStackLevels, 0, sizeof mStackLevels);

	mLastS = 0xFF;

	for(uint32& data : mRepeatData)
		data = 0;

	for(auto& next : mRepeatHashNext)
		next = 0;

	for(auto& start : mRepeatHashStart)
		start = 0;

	for(auto& insnNode : mRepeatTreeInsnNode)
		insnNode = nullptr;

	for(auto& repeatNode : mRepeatTreeRepeatNode)
		repeatNode = nullptr;

	mRepeatHead = 1;
	mRepeatTail = 1;
	mRepeatLastBlockSize = 0;
}

void ATHistoryTreeBuilder::BeginUpdate(bool enableInvalidationChecks) {
	mEarliestUpdatePos = enableInvalidationChecks ? (uint32)-1 : 0;
	mpLastNode = nullptr;
}

uint32 ATHistoryTreeBuilder::EndUpdate(ATHTNode *&last) {
	last = mpLastNode;
	return mEarliestUpdatePos;
}

void ATHistoryTreeBuilder::Update(const ATHistoryTraceInsn *VDRESTRICT htab, uint32 n) {	

	while(n--) {
		const ATHistoryTraceInsn& hent = *htab++;
		const uint32 insnOffset = mRepeatHead - 1;

		// If we've had a change in stack height or an interrupt, terminate
		// any loop tracking.
		const uint8 s = hent.mS;

		// If we're higher on the stack than before (pop/return), pop entries off the tree parent stack.
		// Note that we try to gracefully handle wrapping here. The idea is that generally the stack
		// won't go down by more than 8 entries or so (JSL+interrupt), whereas it may go up way more
		// than that when TXS is used.

		if (mLastS != s) {
			if ((uint8)(mLastS - s) >= 8) {		// s > mLastS, with some wraparound slop
				while(s != mLastS) {				// note that mLastS is a uint8 and will wrap
					mStackLevels[mLastS++] = { nullptr, 0 };
				}
			} else {
				while(s != mLastS) {				// note that mLastS is a uint8 and will wrap
					mStackLevels[--mLastS] = { nullptr, 0 };
				}
			}
		}

		// Check if we have a parent to use.
		ATHTNode *parent = mStackLevels[s].mpTreeNode;
		int parentDepth = mStackLevels[s].mDepth;

		if (!parent) {
			uint8 s2 = s + 1;
			for(int i=0; i<8; ++i, ++s2) {
				parent = mStackLevels[s2].mpTreeNode;

				if (parent) {
					parentDepth = mStackLevels[s2].mDepth;
					break;
				}
			}

			if (!parent)
				parent = mpHistoryTree->GetRootNode();

			if (hent.mbInterrupt) {
				if (mbCollapseInterrupts) {
					if (parentDepth >= kMaxNestingDepth) {
						parent = mpHistoryTree->GetRootNode();
						parentDepth = 0;
						ResetStack();
					}

					parent = mpHistoryTree->InsertNode(parent, parent->mpLastChild, insnOffset, kATHTNodeType_Interrupt);
					++parentDepth;
				}
			} else if (mbCollapseCalls) {
				if (parentDepth >= kMaxNestingDepth) {
					parent = mpHistoryTree->GetRootNode();
					parentDepth = 0;
					ResetStack();
				}

				if (parent->mpLastChild) {
					ATHTNode *newParent = parent->mpLastChild;

					// check if we need to fragment off the call
					const uint32 numInsns = newParent->mVisibleLines;
					if (numInsns > 1) {
						--newParent->mInsn.mCount;
						--newParent->mHeight;
						--newParent->mVisibleLines;
							
						mpHistoryTree->MoveNodesUpAfter(newParent, 1);

						const uint32 newInsnOffset = newParent->mInsn.mOffset + numInsns - 1;
						ATHTNode *newNode = mpHistoryTree->InsertNode(parent, newParent, newInsnOffset, kATHTNodeType_Insn);

						if (mRepeatTreeInsnNode[(newInsnOffset + 1) & kRepeatWindowMask] == newParent)
							mRepeatTreeInsnNode[(newInsnOffset + 1) & kRepeatWindowMask] = newNode;

						newParent = newNode;
					}

					parent = newParent;
				} else
					parent = mpHistoryTree->InsertLabelNode(parent, parent->mpLastChild, "Subroutine call");

				++parentDepth;
			}

			mStackLevels[s].mpTreeNode = parent;
			mStackLevels[s].mDepth = parentDepth;
		}

		mLastS = s;

		if (hent.mbCall) {
			mStackLevels[(uint8)--mLastS] = { nullptr, 0 };
			mStackLevels[(uint8)--mLastS] = { nullptr, 0 };
		}

		if (hent.mPushCount) {
			for(uint32 i = hent.mPushCount; i; --i)
				mStackLevels[(uint8)--mLastS] = { parent, parentDepth };

		}

		// add new node
		ATHTNode *insnNode;
			
		insnNode = parent->mpLastChild;

#if VERIFY_HISTORY_TREE
		mpHistoryTree->VerifyNode(parent, 2);
#endif

		if (insnNode && insnNode->mNodeType == kATHTNodeType_Insn && !insnNode->mpFirstChild) {
			++insnNode->mInsn.mCount;
			++insnNode->mVisibleLines;
			++insnNode->mHeight;

			mpHistoryTree->MoveNodesDownAfter(insnNode, 1);
		} else {
			insnNode = mpHistoryTree->InsertNode(parent, parent->mpLastChild, insnOffset, kATHTNodeType_Insn);
		}

		mpLastNode = insnNode;

#if VERIFY_HISTORY_TREE
		mpHistoryTree->VerifyNode(parent, 2);
#endif

		// check if we have a match on the repeat window
		int repeatOffset = -1;

		typedef uint16 repeatHasher;

		const uint32 repeatData = hent.mPC + ((uint32)hent.mOpcode << 24);
		const uint16 repeatHash = repeatHasher(repeatData);
		ATHTNode *repeatFirstInsnNode = nullptr;
		ATHTNode *repeatNode = nullptr;

		if (mbCollapseLoops) {
			uint32 pos = mRepeatHashStart[repeatHash];
			const uint32 basePos = mRepeatHead > mRepeatTail + (kRepeatWindowSize / 3) ? mRepeatHead - (kRepeatWindowSize / 3) : mRepeatTail;

			for(int limit=0; limit<16 && pos >= basePos; ++limit) {
				const uint32 winPos = pos & kRepeatWindowMask;

				if (mRepeatData[winPos] != repeatData)
					goto reject_match;

				{
					// Compute the block size -- which is just the number of instructions from head to
					// the first match.
					const uint32 blockSize = ((mRepeatHead - pos) & kRepeatWindowMask);

					// Compare the block size against the longest match we found at the last instruction.
					// If we found a match of length M at distance N, then we are highly likely to find a
					// match of M+1 at distance N now. 

					// We found a matching instruction earlier in the window. Do a quick check to see
					// if the rolling sum matches for the blocks.
					const uint32 winPos2 = (pos - blockSize) & kRepeatWindowMask;
					if (blockSize > 1 && mRepeatData[winPos2 + 1] != mRepeatData[winPos + 1])
						goto reject_match;

					const uint32 rollingSum1 = mRepeatSumAccum - mRepeatDataSum[winPos];
					const uint32 rollingSum2 = mRepeatDataSum[winPos] - mRepeatDataSum[winPos2];

					if (rollingSum1 != rollingSum2)
						goto reject_match;

					if (mRepeatLastBlockSize < blockSize - 1) {
						// No dice -- do exhaustive check.

						for(uint32 i=1; i<blockSize; ++i) {
							if (mRepeatData[winPos + i] != mRepeatData[winPos2 + i])
								goto reject_match;
						}
					}

					// Block successfully matched. Find the first instruction node for the repeated section
					// we matched.
					repeatFirstInsnNode = insnNode;

					if (blockSize > 1) {
						repeatFirstInsnNode = mRepeatTreeInsnNode[(mRepeatHead - blockSize + 1) & kRepeatWindowMask];

						// Check if it is at the same nesting level as the current node. If not, we can't collapse.
						if (repeatFirstInsnNode->mpParent != parent)
							goto reject_match;
					}

					// Find the repeat node for the block, if there is one.
					repeatNode = mRepeatTreeRepeatNode[(mRepeatHead - blockSize * 2) & kRepeatWindowMask];
					if (repeatNode) {
						VDASSERT(repeatNode->mNodeType == kATHTNodeType_Repeat);

						if (repeatNode->mpParent != parent)
							goto reject_match;
					}

					uint32 cleanupLen = blockSize - 1;
					if (!repeatNode || repeatNode->mRepeat.mSize != blockSize) {
						// Hmm, there's no repeat node. We don't want to create a repeated section
						// unless we have at least three iterations (2 repeats). That means we need
						// to check another section.
						const uint32 winPos3 = (winPos2 - blockSize) & kRepeatWindowMask;
						const uint32 rollingSum3 = mRepeatDataSum[winPos2] - mRepeatDataSum[winPos3];

						if (rollingSum2 != rollingSum3)
							goto reject_match;

						for(uint32 i=0; i<blockSize; ++i) {
							if (mRepeatData[(winPos2 + i) & kRepeatWindowMask] != mRepeatData[(winPos3 + i) & kRepeatWindowMask])
								goto reject_match;
						}

						const uint32 repeatBase = mRepeatHead - blockSize * 2 + 1;
						repeatFirstInsnNode = mRepeatTreeInsnNode[(mRepeatHead - blockSize * 2 + 1) & kRepeatWindowMask];

						// Block match if the first instruction node is at a different nesting level than where
						// we are currently adding instructions. This happens if the detected loop is a recursion
						// loop rather than an iterative loop. We need to block this to prevent parent loops in
						// the tree.
						if (repeatFirstInsnNode->mpParent != parent)
							goto reject_match;

						if (repeatFirstInsnNode->mNodeType == kATHTNodeType_Insn) {
							const uint32 splitOffset = (repeatBase - 1) - repeatFirstInsnNode->mInsn.mOffset;

							VDASSERT(splitOffset < repeatFirstInsnNode->mInsn.mCount);

							if (splitOffset > 0) {

								repeatFirstInsnNode = mpHistoryTree->SplitInsnNode(repeatFirstInsnNode, splitOffset);

	#if VERIFY_HISTORY_TREE
								mpHistoryTree->VerifyNode(repeatFirstInsnNode->mpParent, 2);
	#endif
							}
						}

						repeatNode = mpHistoryTree->InsertNode(parent, repeatFirstInsnNode->mpPrevSibling, 0, kATHTNodeType_Repeat);
						repeatNode->mRepeat.mSize = blockSize;
						repeatNode->mRepeat.mCount = 1;

						cleanupLen = blockSize * 2 - 1;
					}

					// Scan the hash chains and delink all nodes that we've found as part of the repeating
					// section; these can no longer start a repeat.
					{
						uint32 cleanupPos = mRepeatHead - 1;
						uint32 cleanupWindowPos = (cleanupPos & kRepeatWindowMask) + kRepeatWindowSize;

						for(uint32 i=0; i < cleanupLen; ++i, --cleanupPos, --cleanupWindowPos) {
							mRepeatTreeInsnNode[cleanupWindowPos & kRepeatWindowMask] = repeatNode;

							const uint32 hash = repeatHasher(mRepeatData[cleanupWindowPos]);

							if (mRepeatHashStart[hash] == cleanupPos) {
								VDASSERT(mRepeatHashNext[cleanupWindowPos & kRepeatWindowMask] < mRepeatHead);
								mRepeatHashStart[hash] = mRepeatHashNext[cleanupWindowPos & kRepeatWindowMask];
							}
						}
					}

					repeatOffset = (int)blockSize;
					break;
				}
reject_match:
				const uint32 nextPos = mRepeatHashNext[winPos];
				VDASSERT(nextPos < pos);

				pos = nextPos;
			}
		}

		const uint32 repeatIndex = mRepeatHead & kRepeatWindowMask;

		if (repeatOffset >= 0) {
			if (repeatNode->mpParent == parent) {
				mRepeatTreeRepeatNode[(mRepeatHead - repeatOffset) & kRepeatWindowMask] = repeatNode;

				++repeatNode->mRepeat.mCount;

				RefreshNode(repeatNode);

				// splice lines into repeat node
				VDASSERT(repeatFirstInsnNode->mpPrevSibling == repeatNode);

#if VERIFY_HISTORY_TREE
				mpHistoryTree->VerifyNode(repeatNode->mpParent, 2);
#endif

				ATHTNode *insertAfter = repeatNode->mpLastChild;
				mpHistoryTree->SpliceNodes(repeatFirstInsnNode, repeatFirstInsnNode->mpParent->mpLastChild, repeatNode, insertAfter);

				// see if we can fuse nodes
				if (insertAfter && insertAfter->mNodeType == kATHTNodeType_Insn && !insertAfter->mpFirstChild
					&& repeatFirstInsnNode->mNodeType == kATHTNodeType_Insn && !repeatFirstInsnNode->mpFirstChild)
				{
					insertAfter->mInsn.mCount += repeatFirstInsnNode->mInsn.mCount;
					insertAfter->mHeight += repeatFirstInsnNode->mHeight;
					insertAfter->mVisibleLines += repeatFirstInsnNode->mVisibleLines;
					repeatFirstInsnNode->mInsn.mCount = 0;
					repeatFirstInsnNode->mHeight = 0;
					repeatFirstInsnNode->mVisibleLines = 0;

					mpHistoryTree->RemoveNode(repeatFirstInsnNode);
				}

#if VERIFY_HISTORY_TREE
				mpHistoryTree->VerifyNode(repeatNode->mpParent, 2);
#endif

				mpLastNode = repeatNode;
			}

			mRepeatLastBlockSize = (uint32)repeatOffset;
		} else {
			mRepeatLastBlockSize = 0;
		}

		// shift in new instruction into repeat window
		mRepeatHashNext[repeatIndex] = mRepeatHashStart[repeatHash];
		mRepeatHashStart[repeatHash] = mRepeatHead;
		mRepeatTreeInsnNode[repeatIndex] = insnNode;
		mRepeatTreeRepeatNode[repeatIndex] = nullptr;
		mRepeatDataSum[repeatIndex] = mRepeatSumAccum;
		mRepeatSumAccum += repeatData;
		mRepeatData[repeatIndex] = repeatData;
		mRepeatData[repeatIndex + kRepeatWindowSize] = repeatData;
		++mRepeatHead;
	}
}

void ATHistoryTreeBuilder::ResetStack() {
	std::fill(std::begin(mStackLevels), std::end(mStackLevels), StackLevel {});
	mRepeatTail = mRepeatHead;
}

void ATHistoryTreeBuilder::RefreshNode(ATHTNode *node) {
	const uint32 pos = mpHistoryTree->GetLineYPos(ATHTLineIterator { node, 0 });

	if (pos < mEarliestUpdatePos)
		mEarliestUpdatePos = pos;
}

///////////////////////////////////////////////////////////////////////////

void ATHistoryTranslateInsn6502(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		switch(he->mOpcode[0]) {
			case 0x48:	// PHA
			case 0x08:	// PHP
				dst->mPushCount = 1;
				break;
		}

		++dst;
	}
}

void ATHistoryTranslateInsn65C02(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		switch(he->mOpcode[0]) {
			case 0x48:	// PHA
			case 0x08:	// PHP
			case 0x5A:	// PHY
			case 0xDA:	// PHX
				dst->mPushCount = 1;
				break;
		}

		++dst;
	}
}

void ATHistoryTranslateInsn65C816(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		switch(he->mOpcode[0]) {
			case 0x48:	// PHA
				if (!(he->mP & 0x20))
					dst->mPushCount = 2;
				else
					dst->mPushCount = 1;
				break;

			case 0x08:	// PHP
				dst->mPushCount = 1;
				break;

			case 0x5A:	// PHY
			case 0xDA:	// PHX
				if (!(he->mP & 0x10))
					dst->mPushCount = 2;
				else
					dst->mPushCount = 1;
				break;

			case 0x8B:	// PHB
			case 0x4B:	// PHK
				dst->mPushCount = 1;
				break;

			case 0x0B:	// PHD
			case 0xF4:	// PEA
			case 0x62:	// PER
			case 0xD4:	// PEI
				dst->mPushCount = 2;
				break;
		}

		++dst;
	}
}

void ATHistoryTranslateInsnZ80(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		switch(he->mOpcode[0]) {
			case 0xC5:		// PUSH BC
			case 0xD5:		// PUSH DE
			case 0xE5:		// PUSH HL
			case 0xF5:		// PUSH AF
				dst->mPushCount = 2;
				break;

			case 0xDD:
			case 0xFD:
				switch(he->mOpcode[1]) {
					case 0xE5:		// PUSH IX/IY
						dst->mPushCount = 2;
						break;
				}
				break;
		}

		++dst;
	}
}

void ATHistoryTranslateInsn8048(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		++dst;
	}
}

void ATHistoryTranslateInsn6809(ATHistoryTraceInsn *dst, const ATCPUHistoryEntry *const *hep, uint32 n) {
	while(n--) {
		const ATCPUHistoryEntry *he = *hep++;
		dst->mPC = he->mPC;
		dst->mbInterrupt = he->mbIRQ != he->mbNMI;
		dst->mbCall = false;
		dst->mS = he->mS;
		dst->mOpcode = he->mOpcode[0];
		dst->mPushCount = 0;

		if (he->mbIRQ || he->mbNMI) {
			if (he->mP & 0x80) {
				// E=1: push all registers (12 bytes)
				dst->mPushCount = 12;
			} else {
				// E=0: push CC and PC only (3 bytes)
				dst->mPushCount = 3;
			}
		} else {
			switch(he->mOpcode[0]) {
				case 0x34: {		// PSHS
					const uint8 mask = he->mOpcode[1];

					// PC
					if (mask & 0x80)
						dst->mbCall = true;

					// X/Y/U
					if (mask & 0x40) dst->mPushCount += 2;
					if (mask & 0x20) dst->mPushCount += 2;
					if (mask & 0x10) dst->mPushCount += 2;

					// DP/B/A/CC
					if (mask & 0x08) ++dst->mPushCount;
					if (mask & 0x04) ++dst->mPushCount;
					if (mask & 0x02) ++dst->mPushCount;
					if (mask & 0x01) ++dst->mPushCount;

					break;
				}

				case 0x3C:		// CWAI
					dst->mPushCount = 12;
					break;
			}
		}

		++dst;
	}
}

ATHistoryTranslateInsnFn ATHistoryGetTranslateInsnFn(ATDebugDisasmMode dmode) {
	switch(dmode) {
		case kATDebugDisasmMode_6502:
			return ATHistoryTranslateInsn6502;

		case kATDebugDisasmMode_65C02:
			return ATHistoryTranslateInsn65C02;

		case kATDebugDisasmMode_65C816:
			return ATHistoryTranslateInsn65C816;

		case kATDebugDisasmMode_Z80:
			return ATHistoryTranslateInsnZ80;

		case kATDebugDisasmMode_8048:
			return ATHistoryTranslateInsn8048;

		case kATDebugDisasmMode_6809:
			return ATHistoryTranslateInsn6809;

		default:
			return nullptr;
	}
}
