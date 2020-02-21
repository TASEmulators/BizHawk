//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2010 Avery Lee
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
#include "memorymanager.h"
#include "console.h"

void ATMemoryManager::MemoryLayer::UpdateEffectiveRange() {
	mEffectiveStart = std::max(mPageOffset, mMaskRangeStart);
	mEffectiveEnd = std::min(mPageOffset + mPageCount, mMaskRangeEnd);

	if (mEffectiveEnd < mEffectiveStart)
		mEffectiveEnd = mEffectiveStart;
}

///////////////////////////////////////////////////////////////////////////

ATMemoryManager::ATMemoryManager() {
	for(int i=0; i<256; ++i) {
		mDummyReadPageTable[i] = (uintptr)&mDummyReadNode + 1;
		mDummyWritePageTable[i] = (uintptr)&mDummyWriteNode + 1;
	}

	mDummyLayer.mPriority = -1;
	mDummyLayer.mFlags = kATMemoryAccessMode_ARW;
	mDummyLayer.mbReadOnly = false;
	mDummyLayer.mpBase = NULL;
	mDummyLayer.mAddrMask = 0xFFFFFFFF;
	mDummyLayer.mPageOffset = 0;
	mDummyLayer.mPageCount = 0x10000;
	mDummyLayer.mMaskRangeStart = 0;
	mDummyLayer.mMaskRangeEnd = 0xFFFF;
	mDummyLayer.mEffectiveStart = 0;
	mDummyLayer.mEffectiveEnd = 0xFFFF;
	mDummyLayer.mHandlers.mbPassReads = false;
	mDummyLayer.mHandlers.mbPassAnticReads = false;
	mDummyLayer.mHandlers.mbPassWrites = false;
	mDummyLayer.mHandlers.mpThis = this;
	mDummyLayer.mHandlers.mpDebugReadHandler = DummyReadHandler;
	mDummyLayer.mHandlers.mpReadHandler = DummyReadHandler;
	mDummyLayer.mHandlers.mpWriteHandler = DummyWriteHandler;
	mDummyLayer.mpName = "Unconnected";

	mDummyReadNode.mLayerOrForward = (uintptr)&mDummyLayer;
	mDummyReadNode.mpReadHandler = DummyReadHandler;
	mDummyReadNode.mNext = 1;
	mDummyReadNode.mpThis = this;
	mDummyWriteNode.mLayerOrForward = (uintptr)&mDummyLayer;
	mDummyWriteNode.mpWriteHandler = DummyWriteHandler;
	mDummyWriteNode.mNext = 1;

	mpCPUReadBankMap = &mReadBankTable;
	mpCPUWriteBankMap = &mWriteBankTable;
	mpCPUReadPageMap = &mCPUReadPageMap;
	mpCPUWritePageMap = &mCPUWritePageMap;
	mpCPUReadAddressPageMap = mCPUReadAddressSpaceMap;
}

ATMemoryManager::~ATMemoryManager() {
}

void ATMemoryManager::Init() {
	for(uint32 i=0; i<256; ++i) {
		mAnticReadPageMap[i] = (uintptr)&mDummyReadNode + 1;
		mCPUReadPageMap[i] = (uintptr)&mDummyReadNode + 1;
		mCPUWritePageMap[i] = (uintptr)&mDummyWriteNode + 1;
	}

	mReadBankTable[0] = &mCPUReadPageMap;
	mWriteBankTable[0] = &mCPUWritePageMap;

	for(uint32 i=1; i<256; ++i) {
		mReadBankTable[i] = &mHighMemoryReadPageTables[i - 1];
		mWriteBankTable[i] = &mHighMemoryWritePageTables[i - 1];
	}

	for(PageTable& pt : mHighMemoryReadPageTables) {
		for(uintptr& pte : pt)
			pte = (uintptr)&mDummyReadNode + 1;
	}

	for(PageTable& pt : mHighMemoryWritePageTables) {
		for(uintptr& pte : pt)
			pte = (uintptr)&mDummyWriteNode + 1;
	}
}

void ATMemoryManager::SetHighMemoryEnabled(bool enabled) {
	if (mbHighMemoryEnabled == enabled)
		return;

	mbHighMemoryEnabled = enabled;

	if (enabled) {
		RebuildAllNodes(0x100, 0xFF00, kATMemoryAccessMode_ARW);
	} else {
		for(uint32 i=1; i<256; ++i) {
			mReadBankTable[i] = &mCPUReadPageMap;
			mWriteBankTable[i] = &mCPUWritePageMap;
		}
	}
}

void ATMemoryManager::SetWrapBankZeroEnabled(bool enabled) {
	if (mbWrapBankZero == enabled)
		return;

	mbWrapBankZero = enabled;

	RebuildAllNodes(0x100, 1, kATMemoryAccessMode_ARW);
}

void ATMemoryManager::SetFloatingIoBus(bool floating) {
	if (mbFloatingIoBus == floating)
		return;

	mbFloatingIoBus = floating;

	// Must reinitialize debug read handler for all memory nodes.
	for(MemoryLayer *layer : mLayers) {
		if (layer->mpBase) {
			if (layer->mbIoBus && floating) {
				layer->mHandlers.mpDebugReadHandler = IoMemoryDebugReadWrapperHandler;
			} else {
				layer->mHandlers.mpDebugReadHandler = ChipReadHandler;
			}
		}
	}

	// Must rebuild all nodes now as memory nodes are handled differently in I/O mode.
	RebuildAllNodes(0, 256, kATMemoryAccessMode_ARW);
}

void ATMemoryManager::SetFastBusEnabled(bool enabled) {
	if (mbFastBusEnabled == enabled)
		return;

	mbFastBusEnabled = enabled;

	RebuildAllNodes(0, 256, kATMemoryAccessMode_RW);
}

void ATMemoryManager::DumpStatus() {
	VDStringA s;

	Layers resortedLayers(mLayers);
	auto it1 = resortedLayers.begin(), it2 = resortedLayers.end();

	if (it2 - it1 > 1) {
		std::sort(it1, it2,
			[](const MemoryLayer *a, const MemoryLayer *b) {
				// order by ending page descending
				if (a->mPageOffset + a->mPageCount != b->mPageOffset + b->mPageCount)
					return a->mPageOffset + a->mPageCount > b->mPageOffset + b->mPageCount;

				// order by priority descending
				return a->mPriority > b->mPriority;
			}
		);

		// fix any cases where we have a higher priority layer overlapping a lower priority layer
		// before it (O(N^2) currently)
		while(it1 != it2) {
			const auto itCur = it1++;
			const MemoryLayer& cur = **itCur;

			for(auto it3 = it1; it3 != it2; ++it3) {
				const MemoryLayer& test = **it3;

				if (test.mPriority > cur.mPriority &&
					test.mPageOffset < cur.mPageOffset + cur.mPageCount &&
					cur.mPageOffset < test.mPageOffset + test.mPageCount)
				{
					// ordering violation -- move this element up and recheck it
					std::rotate(itCur, it3, it3 + 1);
					it1 = itCur;
					break;
				}
			}
		}
	}


	if (mbFastBusEnabled) {
		ATConsoleWrite("Address      Pri Bus Mode  Type            Description    \n");
		ATConsoleWrite("----------------------------------------------------------\n");
	} else {
		ATConsoleWrite("Address      Pri Mode Type                 Description\n");
		ATConsoleWrite("----------------------------------------------------------\n");
	}

	for(const MemoryLayer *p : resortedLayers) {
		const MemoryLayer& layer = *p;

		uint32 pageStart = layer.mEffectiveStart;
		uint32 pageEnd = layer.mEffectiveEnd;

		if (pageStart < pageEnd) {
			s.sprintf("%06X-%06X", pageStart << 8, (pageEnd << 8) - 1);
		} else {
			s = "<masked>     ";
		}

		s.append_sprintf(" %2u%s %c%c%c  "
			, layer.mPriority
			, mbFastBusEnabled ? layer.mbFastBus ? " fast" : " chip" : ""
			, layer.mFlags & kATMemoryAccessMode_A ? 'A' : '-'
			, layer.mFlags & kATMemoryAccessMode_R ? 'R' : '-'
			, layer.mFlags & kATMemoryAccessMode_W ? layer.mbReadOnly ? 'O' : 'W' : '-'
			);

		if (layer.mpBase) {
			s.append("direct memory");

			if (layer.mAddrMask != 0xFFFFFFFF)
				s.append_sprintf(" (mask %x)", layer.mAddrMask);
		} else {
			s += "hardware";
		}

		if (layer.mpName) {
			uint32 len = s.size();

			if (len < 42)
				s.append(42 - len, ' ');

			s.append_sprintf(" [%s]", layer.mpName);
		}

		s += '\n';
		ATConsoleWrite(s.c_str());
	}
}

ATMemoryLayer *ATMemoryManager::CreateLayer(int priority, const uint8 *base, uint32 pageOffset, uint32 pageCount, bool readOnly) {
	VDASSERT(pageOffset < 0x10000 && pageCount <= 0x10000 - pageOffset);
	VDASSERT(!((uintptr)base & 1));
	VDASSERT(base);

	MemoryLayer *layer = new MemoryLayer;

	layer->mpParent = this;
	layer->mPriority = priority;
	layer->mFlags = 0;
	layer->mbReadOnly = readOnly;
	layer->mbFastBus = false;
	layer->mbIoBus = false;
	layer->mpBase = base;
	layer->mPageOffset = pageOffset;
	layer->mPageCount = pageCount;
	layer->mHandlers.mbPassAnticReads = false;
	layer->mHandlers.mbPassReads = false;
	layer->mHandlers.mbPassWrites = false;
	layer->mHandlers.mpThis = layer;
	layer->mHandlers.mpDebugReadHandler = ChipDebugReadHandler;
	layer->mHandlers.mpReadHandler = ChipReadHandler;
	layer->mHandlers.mpWriteHandler = ChipWriteHandler;
	layer->mAddrMask = 0xFFFFFFFFU;
	layer->mpName = NULL;
	layer->mMaskRangeStart = 0x00;
	layer->mMaskRangeEnd = 0xFFFF;
	layer->mEffectiveStart = pageOffset;
	layer->mEffectiveEnd = pageOffset + pageCount;
	layer->mpTag = nullptr;

	mLayers.insert(std::lower_bound(mLayers.begin(), mLayers.end(), layer, MemoryLayerPred()), layer);
	return layer;
}

ATMemoryLayer *ATMemoryManager::CreateLayer(int priority, const ATMemoryHandlerTable& handlers, uint32 pageOffset, uint32 pageCount) {
	VDASSERT(pageOffset < 0x10000 && pageCount <= 0x10000 - pageOffset);

	MemoryLayer *layer = new MemoryLayer;

	layer->mpParent = this;
	layer->mPriority = priority;
	layer->mFlags = 0;
	layer->mbReadOnly = false;
	layer->mbFastBus = false;
	layer->mbIoBus = false;
	layer->mpBase = NULL;
	layer->mPageOffset = pageOffset;
	layer->mPageCount = pageCount;
	layer->mHandlers = handlers;
	layer->mAddrMask = 0xFFFFFFFFU;
	layer->mpName = NULL;
	layer->mMaskRangeStart = 0x00;
	layer->mMaskRangeEnd = 0xFFFF;
	layer->mEffectiveStart = pageOffset;
	layer->mEffectiveEnd = pageOffset + pageCount;
	layer->mpTag = nullptr;

	mLayers.insert(std::lower_bound(mLayers.begin(), mLayers.end(), layer, MemoryLayerPred()), layer);
	return layer;
}

void ATMemoryManager::DeleteLayer(ATMemoryLayer *layer) {
	EnableLayer(layer, false);

	mLayers.erase(std::find(mLayers.begin(), mLayers.end(), layer));

	delete (MemoryLayer *)layer;
}

void ATMemoryManager::DeleteLayerPtr(ATMemoryLayer **layer) {
	if (*layer) {
		DeleteLayer(*layer);
		*layer = nullptr;
	}
}

void ATMemoryManager::EnableLayer(ATMemoryLayer *layer, bool enable) {
	EnableLayer(layer, kATMemoryAccessMode_ARW, enable);
}

void ATMemoryManager::EnableLayer(ATMemoryLayer *layer0, ATMemoryAccessMode accessMode, bool enable) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);
	uint8 flags = layer->mFlags;

	if (enable)
		flags |= accessMode;
	else
		flags &= ~accessMode;

	SetLayerModes(layer, (ATMemoryAccessMode)flags);
}

void ATMemoryManager::SetLayerModes(ATMemoryLayer *layer0, ATMemoryAccessMode flags) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);
	const uint8 changeFlags = layer->mFlags ^ flags;

	if (!changeFlags)
		return;

	layer->mFlags = flags;

	RebuildAllNodes(layer->mPageOffset, layer->mPageCount, changeFlags);
}

void ATMemoryManager::SetLayerMemory(ATMemoryLayer *layer0, const uint8 *base) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (base != layer->mpBase) {
		layer->mpBase = base;

		uint32 rewriteOffset = layer->mPageOffset;
		uint32 rewriteCount = layer->mPageCount;

		RebuildAllNodes(rewriteOffset, rewriteCount, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerMemory(ATMemoryLayer *layer0, const uint8 *base, uint32 pageOffset, uint32 pageCount, uint32 addrMask, int readOnly) {
	VDASSERT(base);
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	bool ro = (readOnly >= 0) ? readOnly != 0 : layer->mbReadOnly;

	if (base != layer->mpBase || layer->mPageOffset != pageOffset || layer->mPageCount != pageCount || addrMask != layer->mAddrMask || ro != layer->mbReadOnly) {
		uint32 oldBegin = layer->mEffectiveStart;
		uint32 oldEnd = layer->mEffectiveEnd;

		layer->mpBase = base;
		layer->mAddrMask = addrMask;
		layer->mPageOffset = pageOffset;
		layer->mPageCount = pageCount;
		layer->mbReadOnly = ro;

		layer->UpdateEffectiveRange();

		uint32 newBegin = layer->mEffectiveStart;
		uint32 newEnd = layer->mEffectiveEnd;

		uint32 rewriteOffset = std::min<uint32>(oldBegin, newBegin);
		uint32 rewriteCount = std::max<uint32>(oldEnd, newEnd) - rewriteOffset;

		RebuildAllNodes(rewriteOffset, rewriteCount, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerMemoryAndAddressSpace(ATMemoryLayer *layer0, const uint8 *base, uint32 addressSpace) {
	VDASSERT(base);
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (base != layer->mpBase || addressSpace != layer->mAddressSpace) {
		layer->mpBase = base;
		layer->mAddressSpace = addressSpace;

		if (layer->mEffectiveEnd > layer->mEffectiveStart)
			RebuildAllNodes(layer->mEffectiveStart, layer->mEffectiveEnd - layer->mEffectiveStart, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerMemoryAndAddressSpace(ATMemoryLayer *layer0, const uint8 *base, uint32 pageOffset, uint32 pageCount, uint32 addressSpace, uint32 addrMask, int readOnly) {
	VDASSERT(base);
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	bool ro = (readOnly >= 0) ? readOnly != 0 : layer->mbReadOnly;

	if (base != layer->mpBase || layer->mPageOffset != pageOffset || layer->mPageCount != pageCount || addrMask != layer->mAddrMask || ro != layer->mbReadOnly
		|| addressSpace != layer->mAddressSpace) {
		uint32 oldBegin = layer->mEffectiveStart;
		uint32 oldEnd = layer->mEffectiveEnd;

		layer->mpBase = base;
		layer->mAddrMask = addrMask;
		layer->mPageOffset = pageOffset;
		layer->mPageCount = pageCount;
		layer->mbReadOnly = ro;
		layer->mAddressSpace = addressSpace;

		layer->UpdateEffectiveRange();

		uint32 newBegin = layer->mEffectiveStart;
		uint32 newEnd = layer->mEffectiveEnd;

		uint32 rewriteOffset = std::min<uint32>(oldBegin, newBegin);
		uint32 rewriteCount = std::max<uint32>(oldEnd, newEnd) - rewriteOffset;

		RebuildAllNodes(rewriteOffset, rewriteCount, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerAddressRange(ATMemoryLayer *layer0, uint32 pageOffset, uint32 pageCount) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (layer->mPageOffset != pageOffset || layer->mPageCount != pageCount) {
		uint32 oldBegin = layer->mEffectiveStart;
		uint32 oldEnd = layer->mEffectiveEnd;

		layer->mPageOffset = pageOffset;
		layer->mPageCount = pageCount;
		
		layer->UpdateEffectiveRange();

		uint32 newBegin = layer->mEffectiveStart;
		uint32 newEnd = layer->mEffectiveEnd;

		uint32 rewriteOffset = std::min<uint32>(oldBegin, newBegin);
		uint32 rewriteCount = std::max<uint32>(oldEnd, newEnd) - rewriteOffset;

		RebuildAllNodes(rewriteOffset, rewriteCount, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerName(ATMemoryLayer *layer0, const char *name) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	layer->mpName = name;
}

void ATMemoryManager::SetLayerTag(ATMemoryLayer *layer0, const void *tag) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	layer->mpTag = tag;
}

void ATMemoryManager::SetLayerFastBus(ATMemoryLayer *layer0, bool fast) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (layer->mbFastBus != fast) {
		layer->mbFastBus = fast;

		if (layer->mpBase) {
			RebuildAllNodes(layer->mPageOffset, layer->mPageCount, kATMemoryAccessMode_RW);
		}
	}
}

void ATMemoryManager::SetLayerIoBus(ATMemoryLayer *layer0, bool ioBus) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (layer->mbIoBus == ioBus)
		return;

	layer->mbIoBus = ioBus;

	// I/O bus setting is ignored if I/O bus is not enabled.
	if (!mbFloatingIoBus)
		return;

	if (layer->mpBase) {
		if (ioBus) {
			layer->mHandlers.mpDebugReadHandler = IoMemoryDebugReadWrapperHandler;
		} else {
			layer->mHandlers.mpDebugReadHandler = ChipDebugReadHandler;
		}
	}

	RebuildAllNodes(layer->mPageOffset, layer->mPageCount, layer->mFlags);
}

void ATMemoryManager::ClearLayerMaskRange(ATMemoryLayer *layer) {
	SetLayerMaskRange(layer, 0, 0x10000);
}

void ATMemoryManager::SetLayerMaskRange(ATMemoryLayer *layer0, uint32 pageStart, uint32 pageCount) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (pageStart > 0x10000)
		pageStart = 0x10000;

	const uint32 pageEnd = std::min<uint32>(pageStart + pageCount, 0x10000);

	if (layer->mMaskRangeStart == pageStart &&
		layer->mMaskRangeEnd == pageEnd)
	{
		return;
	}

	const auto rangeIntersect = [](uint32 a0, uint32 a1, uint32 b0, uint32 b1) {
		uint32 as = (uint32)abs((sint32)a0 - (sint32)a1);
		uint32 bs = (uint32)abs((sint32)b0 - (sint32)b1);

		// The ranges intersect if:
		//	- Both ranges are non-empty
		//	- The distance between the centers is less than half the sum of the lengths
		return as && bs && (uint32)abs((sint32)(a0+a1) - (sint32)(b0+b1)) < (as+bs);
	};

	// check if an update is needed -- this is true if the ranges covered by the
	// changes to either the start or end of the mask range intersect the layer
	const bool changed = rangeIntersect(pageStart, layer->mMaskRangeStart, layer->mPageOffset, layer->mPageOffset + layer->mPageCount)
		|| rangeIntersect(pageEnd, layer->mMaskRangeEnd, layer->mPageOffset, layer->mPageOffset + layer->mPageCount);

	layer->mMaskRangeStart = pageStart;
	layer->mMaskRangeEnd = pageEnd;

	layer->UpdateEffectiveRange();

	if (changed) {
		RebuildAllNodes(layer->mPageOffset, layer->mPageCount, layer->mFlags);
	}
}

void ATMemoryManager::SetLayerAddressSpace(ATMemoryLayer *layer0, uint32 addressSpace) {
	MemoryLayer *const layer = static_cast<MemoryLayer *>(layer0);

	if (layer->mAddressSpace != addressSpace) {
		layer->mAddressSpace = addressSpace;

		if (layer->mFlags & kATMemoryAccessMode_R)
			RebuildAllNodes(layer->mPageOffset, layer->mPageCount, kATMemoryAccessMode_R);
	}
}

uint8 ATMemoryManager::ReadFloatingDataBus() const {
	if (mbFloatingDataBus)
		return mBusValue;

	// Star Raiders 5200 has a bug where some 800 code to read the joysticks via the PIA
	// wasn't removed, so it needs a non-zero value to be returned here.
	return 0xFF;
}

uint8 ATMemoryManager::AnticReadByte(uint32 address) {
	uintptr p = mAnticReadPageMap[(uint8)(address >> 8)];
	address &= 0xffff;

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		sint32 v = node.mpReadHandler(node.mpThis, address);
		if (v >= 0)
			return (uint8)v;

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

uint8 ATMemoryManager::DebugAnticReadByte(uint16 address) {
	uintptr p = mAnticReadPageMap[(uint8)(address >> 8)];
	address &= 0xffff;

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		ATMemoryReadHandler handler = ((MemoryLayer *)node.mLayerOrForward)->mHandlers.mpDebugReadHandler;
		if (handler) {
			sint32 v = handler(node.mpThis, address);
			if (v >= 0)
				return (uint8)v;
		}

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

void ATMemoryManager::DebugAnticReadMemory(void *dst, uint16 address, uint32 len) {
	uint8 *dst8 = (uint8 *)dst;

	while(len--) {
		*dst8++ = DebugAnticReadByte(address++);
	}
}

uint8 ATMemoryManager::CPUReadByte(uint32 address) {
	uintptr p = mCPUReadPageMap[(uint8)(address >> 8)];
	address &= 0xffff;

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		sint32 v = node.mpReadHandler(node.mpThis, address);
		if (v >= 0)
			return (uint8)v;

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

uint8 ATMemoryManager::CPUExtReadByte(uint16 address, uint8 bank) {
	uintptr p = (*mReadBankTable[bank])[(uint8)(address >> 8)];
	const uint32 addr32 = (uint32)address + ((uint32)bank << 16);

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		sint32 v = node.mpReadHandler(node.mpThis, addr32);
		if (v >= 0)
			return (uint8)v;

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

sint32 ATMemoryManager::CPUExtReadByteAccel(uint16 address, uint8 bank, bool chipOK) {
	uintptr p = (*mReadBankTable[bank])[(uint8)(address >> 8)];
	const uint32 addr32 = (uint32)address + ((uint32)bank << 16);

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		if (!chipOK && !((MemoryLayer *)node.mLayerOrForward)->mbFastBus)
			return kChipReadNeedsDelay;

		// We need to precache this as the read handler may rewrite the memory config
		// and invalidate the node.
		const uintptr layerOrForward = node.mLayerOrForward;

		sint32 v = node.mpReadHandler(node.mpThis, addr32);
		if (v >= 0) {
			if (!((MemoryLayer *)layerOrForward)->mbFastBus)
				v |= 0x80000000;
			
			return v;
		}

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

uint8 ATMemoryManager::CPUDebugReadByte(uint16 address) const {
	uintptr p = mCPUReadPageMap[(uint8)(address >> 8)];
	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		const ATMemoryHandlerTable& handlers = ((MemoryLayer *)node.mLayerOrForward)->mHandlers;
		ATMemoryReadHandler handler = handlers.mpDebugReadHandler;
		if (handler) {
			// We need to use mpThis from the layer handler table because the I/O layer thunk may have
			// replaced the this pointer in the node.
			sint32 v = handler(handlers.mpThis, address);
			if (v >= 0)
				return (uint8)v;
		}

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

uint8 ATMemoryManager::CPUDebugExtReadByte(uint16 address, uint8 bank) const {
	uintptr p = (*mReadBankTable[bank])[(uint8)(address >> 8)];
	const uint32 addr32 = (uint32)address + ((uint32)bank << 16);

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		const ATMemoryHandlerTable& handlers = ((MemoryLayer *)node.mLayerOrForward)->mHandlers;
		ATMemoryReadHandler handler = handlers.mpDebugReadHandler;
		if (handler) {
			// We need to use mpThis from the layer handler table because the I/O layer thunk may have
			// replaced the this pointer in the node.
			sint32 v = handler(handlers.mpThis, addr32);
			if (v >= 0)
				return (uint8)v;
		}

		p = node.mNext;
	}

	return ((uint8 *)p)[address];
}

void ATMemoryManager::CPUWriteByte(uint16 address, uint8 value) {
	uintptr p = mCPUWritePageMap[(uint8)(address >> 8)];

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		if (node.mpWriteHandler) {
			if (node.mpWriteHandler(node.mpThis, address, value))
				return;
		}

		p = node.mNext;
		if (p == 1)
			return;
	}

	((uint8 *)p)[address] = value;
}

void ATMemoryManager::CPUExtWriteByte(uint16 address, uint8 bank, uint8 value) {
	uintptr p = (*mWriteBankTable[bank])[(uint8)(address >> 8)];
	const uint32 addr32 = (uint32)address + ((uint32)bank << 16);

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		if (node.mpWriteHandler) {
			if (node.mpWriteHandler(node.mpThis, addr32, value))
				return;
		}

		p = node.mNext;
		if (p == 1)
			return;
	}

	((uint8 *)p)[address] = value;
}

sint32 ATMemoryManager::CPUExtWriteByteAccel(uint16 address, uint8 bank, uint8 value, bool chipOK) {
	uintptr p = (*mWriteBankTable[bank])[(uint8)(address >> 8)];
	const uint32 addr32 = (uint32)address + ((uint32)bank << 16);

	while(ATCPUMEMISSPECIAL(p)) {
		const MemoryNode& node = *(const MemoryNode *)(p - 1);

		if (node.mpWriteHandler) {
			if (!chipOK && !((MemoryLayer *)node.mLayerOrForward)->mbFastBus)
				return kChipReadNeedsDelay;

			// We need to precache this as the read handler may rewrite the memory config
			// and invalidate the node.
			const uintptr layerOrForward = node.mLayerOrForward;
			if (node.mpWriteHandler(node.mpThis, addr32, value)) {
				return ((MemoryLayer *)layerOrForward)->mbFastBus ? 0 : -1;
			}
		}

		p = node.mNext;
		if (p == 1)
			return 0;
	}

	((uint8 *)p)[address] = value;
	return 0;
}

uint8 ATMemoryManager::RedirectDebugReadByte(uint32 addr32, const void *excludeTag) {
	const uint32 addrPage = addr32 >> 8;

	for(MemoryLayer *layer : mLayers) {
		// reject if disabled for read
		if (!(layer->mFlags & kATMemoryAccessMode_R))
			continue;

		// reject if out of range
		if (addrPage < layer->mEffectiveStart || addrPage > layer->mEffectiveEnd)
			continue;

		// reject if tag is excluded
		if (layer->mpTag == excludeTag)
			continue;

		// check if direct memory
		if (layer->mpBase)
			return layer->mpBase[(addr32 - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)];

		// call handler
		if (layer->mHandlers.mpDebugReadHandler) {
			sint32 v = layer->mHandlers.mpDebugReadHandler(layer->mHandlers.mpThis, addr32);
			if (v >= 0) {
				if (!layer->mbFastBus)
					v |= 0x80000000;
			
				return v;
			}
		}
	}

	// return bus noise
	return ReadFloatingDataBus();
}

uint8 ATMemoryManager::RedirectReadByte(uint32 addr32, const void *excludeTag) {
	const uint32 addrPage = addr32 >> 8;

	for(MemoryLayer *layer : mLayers) {
		// reject if disabled for read
		if (!(layer->mFlags & kATMemoryAccessMode_R))
			continue;

		// reject if out of range
		if (addrPage < layer->mEffectiveStart || addrPage > layer->mEffectiveEnd)
			continue;

		// reject if tag is excluded
		if (layer->mpTag == excludeTag)
			continue;

		// check if direct memory
		if (layer->mpBase)
			return layer->mpBase[(addr32 - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)];

		// call handler
		sint32 v = layer->mHandlers.mpReadHandler(layer->mHandlers.mpThis, addr32);
		if (v >= 0) {
			if (!layer->mbFastBus)
				v |= 0x80000000;
			
			return v;
		}
	}

	// return bus noise
	return ReadFloatingDataBus();
}

void ATMemoryManager::RedirectWriteByte(uint32 addr32, uint8 value, const void *excludeTag) {
	const uint32 addrPage = addr32 >> 8;

	for(MemoryLayer *layer : mLayers) {
		// reject if disabled for read
		if (!(layer->mFlags & kATMemoryAccessMode_W))
			continue;

		// reject if out of range
		if (addrPage < layer->mEffectiveStart || addrPage >= layer->mEffectiveEnd)
			continue;

		// reject if tag is excluded
		if (layer->mpTag == excludeTag)
			continue;

		// check if direct memory
		if (layer->mpBase) {
			((uint8 *)layer->mpBase)[(addr32 - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)] = value;
			return;
		}

		// call write handler
		if (layer->mHandlers.mpWriteHandler(layer->mHandlers.mpThis, addr32, value))
			return;
	}
}

void ATMemoryManager::RebuildAllNodes(uint32 base, uint32 n, uint8 modes) {
	// if high memory is disabled, limit updates to bank 0
	if (!mbHighMemoryEnabled) {
		if (base >= 0x100)
			return;

		n = std::min<uint32>(n, 0x100 - base);
	}

	// The ANTIC address space only has one bank.
	if ((modes & kATMemoryAccessMode_AnticRead) && base < 0x100) {
		PageTable *anticBankTable = &mAnticReadPageMap;
		RebuildNodes(&anticBankTable, base, std::min<uint32>(0x100 - base, n), kATMemoryAccessMode_AnticRead);
	}

	if (modes & kATMemoryAccessMode_CPURead)
		RebuildNodes(&mReadBankTable[0], base, n, kATMemoryAccessMode_CPURead);

	if (modes & kATMemoryAccessMode_CPUWrite)
		RebuildNodes(&mWriteBankTable[0], base, n, kATMemoryAccessMode_CPUWrite);
}

void ATMemoryManager::RebuildNodes(PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode accessMode) {
	Layers pertinentLayers;
	pertinentLayers.swap(mLayerTempList);
	pertinentLayers.clear();

	// collect layers overlapping the update range and check if any layer completely covers the range,
	// blocking all lower layers
	bool completeBaseLayer = false;
	for(MemoryLayer *layer : mLayers) {
		// skip layer if it isn't active for the current access mode
		if (!(layer->mFlags & accessMode))
			continue;

		// skip layer if it doesn't overlap the update region
		if (base >= layer->mEffectiveEnd || layer->mEffectiveStart >= base + n)
			continue;

		// add layer to the list for this update
		pertinentLayers.push_back(layer);

		// check if the layer entirely contains the update region and is a plain memory mapping
		if (layer->mpBase &&
			layer->mEffectiveStart <= base &&
			layer->mEffectiveEnd >= (base + n))
		{
			// yes -- mark that we have a complete layer and ignore any further layers as they are
			// shadowed
			completeBaseLayer = true;
			break;
		}
	}

	// if we have a complete base layer and there are no other layers, see if we can optimize
	// the update with a fill (important for $4000-7FFF extended memory window)
	if (completeBaseLayer && pertinentLayers.size() == 1) {
		MemoryLayer *layer = pertinentLayers.front();

		if (layer->mpBase && layer->mAddrMask == 0xFFFFFFFFU && !mbFastBusEnabled && !mbFloatingIoBus) {
			RebuildNodesFast(layer, bankTable, base, n, accessMode);

			// if bank 0 and read, fill the address space map
			if ((accessMode & kATMemoryAccessMode_R) && base < 0x100) {
				uint32 addressSpace = layer->mAddressSpace ? layer->mAddressSpace - (layer->mPageOffset << 8) : 0;
				const uint32 asPagesToFill = std::min<uint32>(0x100 - base, n);

				for (uint32 i = 0; i < asPagesToFill; ++i)
					mCPUReadAddressSpaceMap[base + i] = addressSpace;
			}

			pertinentLayers.swap(mLayerTempList);
			return;
		}
	}

	// If the range covers both bank 0 and bank 1+, we need to split the update as it covers multiple allocators.
	if (base < 0x100 && 0x100 - base < n) {
		uint32 bank0Pages = 0x100 - base;

		RebuildNodesSlow(pertinentLayers, bankTable, base, bank0Pages, accessMode);

		base += bank0Pages;
		n -= bank0Pages;
	}

	RebuildNodesSlow(pertinentLayers, bankTable, base, n, accessMode);

	pertinentLayers.swap(mLayerTempList);
}

void ATMemoryManager::RebuildNodesSlow(Layers& VDRESTRICT pertinentLayers, PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode accessMode) {
	AllocatorSet& allocSet = base < 0x100 ? mLoAllocators : mHiAllocators;

	const uint32 startingBank = base >> 8;
	const uint32 endingBank = (base + n + 255) >> 8;

	const uintptr dummyNode = (accessMode == kATMemoryAccessMode_CPUWrite)
		? (uintptr)&mDummyWriteNode + 1
		: (uintptr)&mDummyReadNode + 1;

	// check if we should rewrite high tables
	if (base >= 0x100) {
		if (pertinentLayers.empty()) {
			// if bank 0 wrapping is active then we cannot deactivate bank 01
			if (mbWrapBankZero && base < 0x200) {
				SetBanksActive(bankTable, 1, 2, accessMode);

				if (n <= 0x200 - base)
					return;

				SetBanksInactive(bankTable, 2, endingBank, accessMode);
			} else {
				SetBanksInactive(bankTable, startingBank, endingBank, accessMode);
				return;
			}
		} else {
			SetBanksActive(bankTable, startingBank, endingBank, accessMode);
		}
	}

	for(uint32 bank = startingBank; bank < endingBank; ++bank) {
		const uint32 bankPageStart = bank << 8;
		const uint32 bankPageEnd = bankPageStart + 256;
		const uint32 pageStart = std::max<uint32>(base, bankPageStart);
		const uint32 pageEnd = std::min<uint32>(base + n, bankPageEnd);
		bool boundaryBits[257] = { false };

		boundaryBits[pageStart & 0xFF] = true;

		for(const MemoryLayer *layer : pertinentLayers) {
			const uint32 layerStartInBank = std::max<uint32>(layer->mEffectiveStart, bankPageStart);
			const uint32 layerEndInBank = std::min<uint32>(layer->mEffectiveEnd, bankPageEnd);

			if (layerStartInBank >= layerEndInBank)
				continue;

			boundaryBits[layerStartInBank - bankPageStart] = true;
			boundaryBits[layerEndInBank - bankPageStart] = true;

			if (layer->mAddrMask < UINT32_C(0x80000000)) {
				const auto lowestZeroBit = [](uint32 v) { return ~v & (v + 1); };

				static_assert(lowestZeroBit(0) == 1);
				static_assert(lowestZeroBit(1) == 2);
				static_assert(lowestZeroBit(59) == 4);

				uint32 step = lowestZeroBit(layer->mAddrMask);

				for(uint32 page = layerStartInBank; page < layerEndInBank; page += step)
					boundaryBits[page - bankPageStart] = true;
			}
		}

		PageTable& pageTable = *bankTable[bank];
		uintptr *dst = bank == startingBank ? &pageTable[pageStart & 255] : &pageTable[0];
		uint32 *addrSpaceTable = (bank == 0 && accessMode == kATMemoryAccessMode_R) ? &mCPUReadAddressSpaceMap[pageStart & 255] : nullptr;

		for(uint32 page = pageStart; page < pageEnd; ++page) {
			uintptr *root = dst++;

			if (!boundaryBits[page - bankPageStart]) {
				*root = root[-1];

				if (addrSpaceTable)
					*addrSpaceTable++ = addrSpaceTable[-1];
				continue;
			}

			uintptr terminatingNode = dummyNode;

			switch(accessMode) {
				case kATMemoryAccessMode_AnticRead:
					for(MemoryLayer *layer : pertinentLayers) {
						if (page < layer->mEffectiveStart || page >= layer->mEffectiveEnd)
							continue;

						if (mbFloatingIoBus && layer->mbIoBus) {
							MemoryNode *node = AllocNode(allocSet);
							node->mpThis = layer;
							node->mLayerOrForward = (uintptr)layer;

							if (layer->mpBase)
								node->mpReadHandler = (layer->mAddrMask == (uint32)0 - 1) ? IoMemoryFastReadWrapperHandler : IoMemoryReadWrapperHandler;
							else
								node->mpReadHandler = IoHandlerReadWrapperHandler;

							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (layer->mpBase || !layer->mHandlers.mbPassAnticReads) {
								terminatingNode = 1;
								break;
							}
						} else if (layer->mpBase) {
							terminatingNode = (uintptr)layer->mpBase + (((uintptr)((page - layer->mPageOffset) & layer->mAddrMask) - (page & 0xff)) << 8);
							break;
						} else {
							MemoryNode *node = AllocNode(allocSet);

							node->mLayerOrForward = (uintptr)layer;
							node->mpReadHandler = layer->mHandlers.mpReadHandler;
							node->mpThis = layer->mHandlers.mpThis;
							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (!layer->mHandlers.mbPassAnticReads) {
								terminatingNode = 1;
								break;
							}
						}
					}
					break;

				case kATMemoryAccessMode_CPURead: {
					uint32 addrSpace = kAddrSpaceInvalid;

					for(MemoryLayer *layer : pertinentLayers) {
						if (page < layer->mEffectiveStart || page >= layer->mEffectiveEnd)
							continue;

						if (layer->mAddressSpace != kAddrSpaceInvalid && addrSpace == kAddrSpaceInvalid)
							addrSpace = layer->mAddressSpace - (layer->mPageOffset << 8);

						if (mbFloatingIoBus && layer->mbIoBus) {
							MemoryNode *node = AllocNode(allocSet);
							node->mpThis = layer;
							node->mLayerOrForward = (uintptr)layer;

							if (layer->mpBase)
								node->mpReadHandler = (layer->mAddrMask == (uint32)0 - 1) ? IoMemoryFastReadWrapperHandler : IoMemoryReadWrapperHandler;
							else
								node->mpReadHandler = IoHandlerReadWrapperHandler;

							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (layer->mpBase || !layer->mHandlers.mbPassReads) {
								terminatingNode = 1;
								break;
							}
						} else if (layer->mpBase) {
							if (mbFastBusEnabled && !layer->mbFastBus) {
								MemoryNode *node = AllocNode(allocSet);

								node->mLayerOrForward = (uintptr)layer;
								node->mpReadHandler = ChipReadHandler;
								node->mpThis = (void *)(layer->mpBase + ((uintptr)((page - layer->mPageOffset) & layer->mAddrMask) << 8) - ((page & 0xff) << 8));
								terminatingNode = (uintptr)node + 1;
								node->mNext = 1;
							} else {
								terminatingNode = (uintptr)layer->mpBase + (((uintptr)((page - layer->mPageOffset) & layer->mAddrMask) - (page & 0xff)) << 8);
							}
							break;
						} else {
							MemoryNode *node = AllocNode(allocSet);

							node->mLayerOrForward = (uintptr)layer;
							node->mpReadHandler = layer->mHandlers.mpReadHandler;
							node->mpThis = layer->mHandlers.mpThis;
							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (!layer->mHandlers.mbPassReads) {
								terminatingNode = 1;
								break;
							}
						}
					}

					if (addrSpaceTable)
						*addrSpaceTable++ = addrSpace;
					break;
				}

				case kATMemoryAccessMode_CPUWrite:
					for(MemoryLayer *layer : pertinentLayers) {
						if (page < layer->mEffectiveStart || page >= layer->mEffectiveEnd)
							continue;

						if (mbFloatingIoBus && layer->mbIoBus) {
							MemoryNode *node = AllocNode(allocSet);
							node->mpThis = layer;
							node->mLayerOrForward = (uintptr)layer;

							if (layer->mbReadOnly)
								node->mpWriteHandler = IoNullWriteWrapperHandler;
							else if (layer->mpBase)
								node->mpWriteHandler = IoMemoryWriteWrapperHandler;
							else
								node->mpWriteHandler = IoHandlerWriteWrapperHandler;

							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (layer->mpBase || !layer->mHandlers.mbPassWrites) {
								terminatingNode = 1;
								break;
							}
						} else if (layer->mbReadOnly) {
							terminatingNode = (uintptr)&mDummyWriteNode + 1;
							break;
						} else if (layer->mpBase) {
							if (mbFastBusEnabled && !layer->mbFastBus) {
								MemoryNode *node = AllocNode(allocSet);

								node->mLayerOrForward = (uintptr)layer;
								node->mpWriteHandler = ChipWriteHandler;
								node->mpThis = (void *)(layer->mpBase + ((uintptr)((page - layer->mPageOffset) & layer->mAddrMask) << 8) - ((page & 0xff) << 8));
								terminatingNode = (uintptr)node + 1;
								node->mNext = 1;
							} else {
								terminatingNode = (uintptr)layer->mpBase + (((uintptr)((page - layer->mPageOffset) & layer->mAddrMask) - (page & 0xff)) << 8);
							}
							break;
						} else {
							MemoryNode *node = AllocNode(allocSet);

							node->mLayerOrForward = (uintptr)layer;
							node->mpWriteHandler = layer->mHandlers.mpWriteHandler;
							node->mpThis = layer->mHandlers.mpThis;
							*root = (uintptr)node + 1;
							root = &node->mNext;

							if (!layer->mHandlers.mbPassWrites) {
								terminatingNode = 1;
								break;
							}
						}
					}
					break;
			}

			*root = terminatingNode;
		}
	}

	if (allocSet.mAllocationCount >= 4096) {
		if (base < 0x100) {
			GarbageCollect(0, 1, allocSet);

			// force re-reflect page 0
			if (mbWrapBankZero && (accessMode & kATMemoryAccessMode_RW))
				(*bankTable[1])[0] = (*bankTable[0])[0];
		} else
			GarbageCollect(1, 255, allocSet);
	}

	// if bank zero wrapping is active and we covered either page 0 or bank 1 page 0,
	// reflect it now
	if (mbWrapBankZero && (accessMode & kATMemoryAccessMode_RW) && (!base || (base < 0x0101 && n >= 0x0101 - base)))
		(*bankTable[1])[0] = (*bankTable[0])[0];
}

void ATMemoryManager::RebuildNodesFast(MemoryLayer *layer, PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode mode) {
	// update bank table entries
	if (base >= 0x100) {
		const uint32 startingBank = base >> 8;
		const uint32 endingBank = (base + n + 255) >> 8;

		SetBanksActive(bankTable, startingBank, endingBank, mode);
	}

	uintptr layerPtr = (uintptr)layer->mpBase - ((uintptr)(layer->mPageOffset & 0xFF) << 8);
	uintptr layerPtrInc = 0x10000;

	// if the layer is read only, fill the write page table with dummy write entries
	if (mode == kATMemoryAccessMode_CPUWrite && layer->mbReadOnly) {
		layerPtr = (uintptr)&mDummyWriteNode + 1;
		layerPtrInc = 0;
	}

	// fill page table entries
	uint32 pageStart = base;
	uint32 pageEnd = base + n;

	while(pageStart < pageEnd) {
		PageTable& pt = *bankTable[pageStart >> 8];
		const uint32 pageStop = std::min<uint32>((pageStart + 256) & ~UINT32_C(255), pageEnd);
		
		uintptr *dst = &pt[pageStart & 255];
		uint32 bankPages = pageStop - pageStart;

		while(bankPages--)
			*dst++ = layerPtr;

		pageStart = pageStop;
		layerPtr += layerPtrInc;
	}

	// if bank 0 wrapping is active and we covered either page 0 or bank 1 page 0, reflect it now
	if (mbWrapBankZero && (mode & kATMemoryAccessMode_RW) && (!base || (base < 0x0101 && n >= 0x0101 - base)))
		(*bankTable[1])[0] = (*bankTable[0])[0];
}

void ATMemoryManager::SetBanksInactive(PageTable **bankTable, uint32 startBank, uint32 endBank, ATMemoryAccessMode accessMode) {
	PageTable *dummyPageTable = (accessMode == kATMemoryAccessMode_CPUWrite) ? &mDummyWritePageTable : &mDummyReadPageTable;

	for(uint32 bank = startBank; bank < endBank; ++bank) {
		bankTable[bank] = dummyPageTable;
	}
}

void ATMemoryManager::SetBanksActive(PageTable **bankTable, uint32 startBank, uint32 endBank, ATMemoryAccessMode accessMode) {
	const uintptr dummyNode = (accessMode == kATMemoryAccessMode_CPUWrite)
		? (uintptr)&mDummyWriteNode + 1
		: (uintptr)&mDummyReadNode + 1;

	PageTable *pageTables = (accessMode == kATMemoryAccessMode_CPUWrite) ? &mHighMemoryWritePageTables[0] : &mHighMemoryReadPageTables[0];

	for(uint32 bank = startBank; bank < endBank; ++bank) {
		PageTable& pt = pageTables[bank - 1];

		if (bankTable[bank] != &pt) {
			bankTable[bank] = &pt;

			for(uintptr& pte : pt)
				pte = dummyNode;
		}
	}
}

ATMemoryManager::MemoryNode *ATMemoryManager::AllocNode(AllocatorSet& allocSet) {
	++allocSet.mAllocationCount;
	return allocSet.mAllocator.Allocate<MemoryNode>();
}

void ATMemoryManager::GarbageCollect(uint32 startBank, uint32 endBank, AllocatorSet& allocSet0) {
	AllocatorSet& VDRESTRICT allocSet = allocSet0;

	// temporarily whack the dummy nodes so they forward to themselves
	mDummyReadNode.mLayerOrForward = (uintptr)&mDummyReadNode + 1;
	mDummyWriteNode.mLayerOrForward = (uintptr)&mDummyWriteNode + 1;

	PageTable *pageTables[3]={
		nullptr,
		nullptr,
		&mAnticReadPageMap,		// bank 0 only
	};

	// mark and copy all used nodes
	const uint32 numModesToScan = (startBank ? 2 : 3);

	for(uint32 bank = startBank; bank < endBank; ++bank) {
		pageTables[0] = mReadBankTable[bank];
		pageTables[1] = mWriteBankTable[bank];

		for(uint32 modeIndex = 0; modeIndex < numModesToScan; ++modeIndex) {
			PageTable& pt = *pageTables[modeIndex];

			// avoid sweeping the dummy page tables (won't hurt, but useless)
			if (&pt == &mDummyReadPageTable || &pt == &mDummyWritePageTable)
				continue;

			uint32 startingPage = 0;

			// If we are in bank 1 and page 0 wrapping is enabled, we must skip page 0 as it is
			// borrowed from bank 0, which uses a different allocator.
			if (mbWrapBankZero && bank == 1)
				startingPage = 1;

			uintptr *pLink = &pt[startingPage];
			uint32 numPages = 256 - startingPage;
			while(numPages--) {
				uintptr *pRef = pLink;

				for(;;) {
					uintptr p = *pRef;

					if (!ATCPUMEMISSPECIAL(p))
						break;

					MemoryNode *pNode = (MemoryNode *)(p - 1);
					if (!pNode)
						break;

					// check if link was already copied
					if (pNode->mLayerOrForward & 1) {
						// yes -- update the link and exit
						*pRef = pNode->mLayerOrForward;

						VDASSERT(*pRef == mDummyReadNode.mLayerOrForward || *pRef == mDummyWriteNode.mLayerOrForward || allocSet.mAllocatorNext.Contains((const void *)*pRef));
						break;
					}

					// copy the link
					MemoryNode *pNewNode = allocSet.mAllocatorNext.Allocate<MemoryNode>();
					*pNewNode = *pNode;

					// set up forwarding
					pNode->mLayerOrForward = (uintptr)pNewNode + 1;

					// update the reference
					*pRef = (uintptr)pNewNode + 1;

					// check the next node
					pRef = &pNewNode->mNext;
				}

				++pLink;
			}

#if 0
			// verify that all nodes are in new allocator
			for(int i=startingPage; i<256; ++i) {
				uintptr p = (*pageTables[modeIndex])[i];

				for(;;) {
					if (!ATCPUMEMISSPECIAL(p))
						break;

					MemoryNode *pNode = (MemoryNode *)(p - 1);
					if (!pNode)
						break;

					if (!allocSet.mAllocatorNext.Contains(pNode) && pNode != &mDummyReadNode && pNode != &mDummyWriteNode)
						__debugbreak();

					p = pNode->mNext;
				}
			}
#endif
		}
	}

	// trim and swap the allocators
	// NOTE: We must keep the previous allocator alive! This is because we might be
	// in the middle of a memory access and can't drop the allocation chain until
	// it completes.
	allocSet.mAllocatorPrev.Reset();
	allocSet.mAllocatorPrev.Swap(allocSet.mAllocatorNext);
	allocSet.mAllocator.Swap(allocSet.mAllocatorPrev);
	allocSet.mAllocationCount = 0;
	
	// restore the dummy nodes
	mDummyReadNode.mLayerOrForward = (uintptr)&mDummyLayer;
	mDummyWriteNode.mLayerOrForward = (uintptr)&mDummyLayer;
}

sint32 ATMemoryManager::DummyReadHandler(void *thisptr0, uint32 addr) {
	ATMemoryManager *thisptr = (ATMemoryManager *)thisptr0;

	return thisptr->ReadFloatingDataBus();
}

bool ATMemoryManager::DummyWriteHandler(void *thisptr, uint32 addr, uint8 value) {
	return true;
}

sint32 ATMemoryManager::ChipDebugReadHandler(void *thisptr, uint32 addr) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	return layer->mpBase[(addr - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)];
}

sint32 ATMemoryManager::ChipReadHandler(void *thisptr, uint32 addr) {
	return ((const uint8 *)thisptr)[addr & 0xffff];
}

bool ATMemoryManager::ChipWriteHandler(void *thisptr, uint32 addr, uint8 value) {
	((uint8 *)thisptr)[addr & 0xffff] = value;
	return true;
}

sint32 ATMemoryManager::IoMemoryFastReadWrapperHandler(void *thisptr, uint32 addr) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	uint8 c = layer->mpBase[addr - (layer->mPageOffset << 8)];

	layer->mpParent->mIoBusValue = c;
	return c;
}

sint32 ATMemoryManager::IoMemoryDebugReadWrapperHandler(void *thisptr, uint32 addr) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	return layer->mpBase[(addr - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)];
}

sint32 ATMemoryManager::IoMemoryReadWrapperHandler(void *thisptr, uint32 addr) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	uint8 c = layer->mpBase[(addr - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)];

	layer->mpParent->mIoBusValue = c;
	return c;
}

bool ATMemoryManager::IoMemoryRoWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	layer->mpParent->mIoBusValue = value;
	return true;
}

bool ATMemoryManager::IoMemoryWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	((uint8 *)layer->mpBase)[(addr - (layer->mPageOffset << 8)) & ((layer->mAddrMask << 8) + 0xFF)] = value;

	layer->mpParent->mIoBusValue = value;
	return true;
}

sint32 ATMemoryManager::IoHandlerReadWrapperHandler(void *thisptr, uint32 addr) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	sint32 v = layer->mHandlers.mpReadHandler(layer->mHandlers.mpThis, addr);

	if (v >= 0)
		layer->mpParent->mIoBusValue = (uint8)v;

	return v;
}

bool ATMemoryManager::IoHandlerWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	layer->mHandlers.mpWriteHandler(layer->mHandlers.mpThis, addr, value);
	layer->mpParent->mIoBusValue = value;
	return layer->mHandlers.mbPassWrites;
}

bool ATMemoryManager::IoNullWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value) {
	MemoryLayer *layer = (MemoryLayer *)thisptr;
	layer->mpParent->mIoBusValue = value;
	return layer->mHandlers.mbPassWrites;
}
