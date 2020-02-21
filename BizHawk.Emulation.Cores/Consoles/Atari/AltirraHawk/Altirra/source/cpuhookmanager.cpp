//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include "cpuhookmanager.h"
#include "cpu.h"
#include "mmu.h"
#include "pbi.h"

ATCPUHookManager::ATCPUHookManager() {
}

ATCPUHookManager::~ATCPUHookManager() {
}

void ATCPUHookManager::Init(ATCPUEmulator *cpu, ATMMUEmulator *mmu, ATPBIManager *pbi) {
	mpCPU = cpu;
	mpMMU = mmu;
	mpPBI = pbi;
}

void ATCPUHookManager::Shutdown() {
	for(int i=0; i<256; ++i) {
		HashNode *node = mpHashTable[i];

		while(node) {
			uint16 pc = node->mPC;

			do {
				node->mpHookFn = nullptr;
				node = node->mpNext;
			} while(node && node->mPC == pc);

			mpCPU->SetHook(pc, false);
		}
	}

	std::fill(mpHashTable, mpHashTable + 256, (HashNode *)NULL);
	mAllocator.Clear();

	mpInitChain = nullptr;
	mpInitFreeList = nullptr;
	mpFreeList = nullptr;

	mpPBI = NULL;
	mpMMU = NULL;
	mpCPU = NULL;
}

uint8 ATCPUHookManager::OnHookHit(uint16 pc) const {
	HashNode *node = mpHashTable[pc & 0xff];
	HashNode *next;

	for(; node; node = next) {
		next = node->mpNext;

		if (node->mPC == pc) {
			switch(node->mMode) {
				case kATCPUHookMode_Always:
				default:
					break;

				case kATCPUHookMode_KernelROMOnly:
					if (!mbOSHooksEnabled || !mpMMU->IsKernelROMEnabled())
						continue;
					break;

				case kATCPUHookMode_MathPackROMOnly:
					if (!mbOSHooksEnabled || !mpMMU->IsKernelROMEnabled())
						continue;

					if (mpPBI->IsROMOverlayActive())
						continue;
					break;
			}

			uint8 opcode = node->mpHookFn(pc);

			if (opcode)
				return opcode;
		}
	}

	return 0;
}

void ATCPUHookManager::CallResetHooks() {
	for(ResetNode *node = mpResetChain; node; ) {
		ResetNode *next = node->mpNext;

		node->mpHookFn();

		node = next;
	}
}

ATCPUHookResetNode *ATCPUHookManager::AddResetHook(ATCPUHookResetFn fn) {
	if (!mpInitFreeList) {
		ResetNode *node = mAllocator.Allocate<ResetNode>();

		node->mpNext = NULL;
		node->mpHookFn = nullptr;
		mpResetFreeList = node;
	}

	ResetNode *node = mpResetFreeList;
	mpResetFreeList = node->mpNext;
	VDASSERT(!node->mpHookFn);

	node->mpNext = mpResetChain;
	mpResetChain = node;

	node->mpHookFn = std::move(fn);

	return node;
}

void ATCPUHookManager::RemoveResetHook(ATCPUHookResetNode *hook) {
	if (!hook)
		return;

	ResetNode **prev = &mpResetChain;
	ResetNode *node = *prev;

	while(node) {
		if (node == hook) {
			*prev = node->mpNext;

			node->mpNext = mpResetFreeList;
			mpResetFreeList = node;

			node->mpHookFn = nullptr;
			return;
		}

		prev = &node->mpNext;
		node = *prev;
	}

	VDFAIL("Attempt to remove invalid reset hook!");
}

void ATCPUHookManager::CallInitHooks(const uint8 *lowerKernelROM, const uint8 *upperKernelROM) {
	InitNode *node = mpInitChain;

	while(node) {
		InitNode *next = node->mpNext;

		node->mpHookFn(lowerKernelROM, upperKernelROM);

		node = next;
	}
}

ATCPUHookInitNode *ATCPUHookManager::AddInitHook(const ATCPUHookInitFn& fn) {
	if (!mpInitFreeList) {
		InitNode *node = mAllocator.Allocate<InitNode>();

		node->mpNext = NULL;
		node->mpHookFn = nullptr;
		mpInitFreeList = node;
	}

	InitNode *node = mpInitFreeList;
	mpInitFreeList = node->mpNext;
	VDASSERT(!node->mpHookFn);

	node->mpNext = mpInitChain;
	mpInitChain = node;

	node->mpHookFn = fn;

	return node;
}

void ATCPUHookManager::RemoveInitHook(ATCPUHookInitNode *hook) {
	if (!hook)
		return;

	InitNode **prev = &mpInitChain;
	InitNode *node = *prev;

	while(node) {
		if (node == hook) {
			*prev = node->mpNext;

			node->mpNext = mpInitFreeList;
			mpInitFreeList = node;

			node->mpHookFn = nullptr;
			return;
		}

		prev = &node->mpNext;
		node = *prev;
	}

	VDFAIL("Attempt to remove invalid init hook!");
}

ATCPUHookNode *ATCPUHookManager::AddHook(ATCPUHookMode mode, uint16 pc, sint8 priority, const ATCPUHookFn& fn) {
	if (!mpFreeList) {
		HashNode *node = mAllocator.Allocate<HashNode>();

		node->mpNext = NULL;
		node->mpHookFn = nullptr;
		mpFreeList = node;
	}

	HashNode *node = mpFreeList;
	mpFreeList = node->mpNext;
	VDASSERT(!node->mpHookFn);

	node->mpHookFn = fn;
	node->mMode = mode;
	node->mPriority = priority;
	node->mPC = pc;

	int hc = pc & 0xff;
	HashNode **insertPrev = &mpHashTable[hc];
	HashNode *insertNext = *insertPrev;

	while(insertNext && insertNext->mPC != pc) {
		insertPrev = &insertNext->mpNext;
		insertNext = *insertPrev;
	}

	while(insertNext && insertNext->mPC == pc && insertNext->mPriority >= priority) {
		insertPrev = &insertNext->mpNext;
		insertNext = *insertPrev;
	}

	*insertPrev = node;
	node->mpNext = insertNext;

	mpCPU->SetHook(pc, true);
	return node;
}

void ATCPUHookManager::RemoveHook(ATCPUHookNode *hook) {
	if (!hook)
		return;

	const uint16 pc = static_cast<HashNode *>(hook)->mPC;
	HashNode **prev = &mpHashTable[pc & 0xff];
	HashNode *node = *prev;
	sint32 prevpc = -1;

	while(node) {
		if (node == hook) {
			*prev = node->mpNext;

			if (prevpc != pc && (!node->mpNext || node->mpNext->mPC != pc))
				mpCPU->SetHook(pc, false);

			node->mpNext = mpFreeList;
			mpFreeList = node;

			node->mpHookFn = nullptr;
			return;
		}

		prevpc = node->mPC;
		prev = &node->mpNext;
		node = *prev;
	}

	VDASSERT(!"Attempt to remove invalid CPU hook!");
}
