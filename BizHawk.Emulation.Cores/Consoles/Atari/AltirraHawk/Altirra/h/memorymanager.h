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

#ifndef f_AT_MEMORYMANAGER_H
#define f_AT_MEMORYMANAGER_H

#include <vd2/system/linearalloc.h>
#include <vd2/system/memory.h>
#include <vd2/system/vdstl.h>
#include "cpumemory.h"

// Read/write handlers. The address is the 16-bit or 24-bit global address
// of the access. Read routines return 0-255 if handled or -1 if not handled;
// write routines return true if handled and false otherwise.
//
// A read/write handler MUST handle an access if it modifies memory layers.
// This is because the handler chain is undefined after the change.
//
// The read handler type is used for both regular reads and debug reads.
// Debug reads are reads with all side effects skipped; it is used by the
// the debugger and the UI to avoid changing the emulation state.
//
typedef sint32 (*ATMemoryReadHandler)(void *thisptr, uint32 addr);
typedef bool (*ATMemoryWriteHandler)(void *thisptr, uint32 addr, uint8 value);

struct ATMemoryHandlerTable {
	// Set if a CPU or ANTIC read can go unhandled by the read handler or
	// debug read handler, or a CPU write by the write handler, for the given
	// region. This is an optimization to avoid populating the rest of the
	// handler chain for that page and access type. It MUST be clear if
	// the handler can pass.
	bool mbPassReads;
	bool mbPassAnticReads;
	bool mbPassWrites;

	void *mpThis;
	ATMemoryReadHandler mpDebugReadHandler;
	ATMemoryReadHandler mpReadHandler;
	ATMemoryWriteHandler mpWriteHandler;

	template<class T, sint32 (T::*T_Handler)(uint32 addr) const>
	void BindDebugReadHandler() {
		mpDebugReadHandler = [](void *thisptr0, uint32 address) { return (((T *)thisptr0)->*T_Handler)(address); };
	}

	template<class T, sint32 (T::*T_Handler)(uint32 addr)>
	void BindReadHandler() {
		mpReadHandler = [](void *thisptr0, uint32 address) { return (((T *)thisptr0)->*T_Handler)(address); };
	}

	template<class T, bool (T::*T_Handler)(uint32 addr, uint8 data)>
	void BindWriteHandler() {
		mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return (((T *)thisptr0)->*T_Handler)(address, data); };
	}
};

class ATMemoryLayer {};

enum ATMemoryAccessMode : uint8 {
	kATMemoryAccessMode_0 = 0,
	kATMemoryAccessMode_W,
	kATMemoryAccessMode_R,
	kATMemoryAccessMode_RW,
	kATMemoryAccessMode_A,
	kATMemoryAccessMode_AW,
	kATMemoryAccessMode_AR,
	kATMemoryAccessMode_ARW,
	kATMemoryAccessMode_AnticRead = kATMemoryAccessMode_A,
	kATMemoryAccessMode_CPURead = kATMemoryAccessMode_R,
	kATMemoryAccessMode_CPUWrite = kATMemoryAccessMode_W,
};

enum ATMemoryPriority {
	kATMemoryPri_BaseRAM	= 0,
	kATMemoryPri_ExtRAM		= 1,
	kATMemoryPri_Extsel		= 2,
	kATMemoryPri_ROM		= 8,
	kATMemoryPri_Cartridge2	= 16,
	kATMemoryPri_Cartridge1	= 24,
	kATMemoryPri_CartridgeOverlay	= 32,
	kATMemoryPri_PBIIRQ		= 44,
	kATMemoryPri_PBI		= 48,
	kATMemoryPri_PBISelect	= 51,
	kATMemoryPri_Hardware	= 56,
	kATMemoryPri_HardwareOverlay,
	kATMemoryPri_AccessBP	= 64
};

class ATMemoryManager final : public VDAlignedObject<32>, public ATCPUEmulatorMemory {
	ATMemoryManager(const ATMemoryManager&) = delete;
	ATMemoryManager& operator=(const ATMemoryManager&) = delete;
public:
	// Returned by the accelerated read/write routines if a slow access is
	// prohibited but required by the accessed memory layer.
	enum { kChipReadNeedsDelay = -256 };

	ATMemoryManager();
	~ATMemoryManager();

	const uintptr *GetAnticMemoryMap() const { return mAnticReadPageMap; }

	void Init();

	void SetHighMemoryEnabled(bool enabled);

	// Enable mirroring of page 0 into $01:00xx.
	void SetWrapBankZeroEnabled(bool enabled);

	void SetFloatingDataBus(bool floating) { mbFloatingDataBus = floating; }

	bool GetFloatingIoBus() const { return mbFloatingIoBus; }
	void SetFloatingIoBus(bool floating);

	void SetFastBusEnabled(bool enabled);

	void DumpStatus();

	ATMemoryLayer *CreateLayer(int priority, const uint8 *base, uint32 pageAddr, uint32 pages, bool readOnly);
	ATMemoryLayer *CreateLayer(int priority, const ATMemoryHandlerTable& handlers, uint32 pageOffset, uint32 pages);
	void DeleteLayer(ATMemoryLayer *layer);
	void DeleteLayerPtr(ATMemoryLayer **layer);
	void EnableLayer(ATMemoryLayer *layer, bool enable);
	void EnableLayer(ATMemoryLayer *layer, ATMemoryAccessMode mode, bool enable);
	void SetLayerModes(ATMemoryLayer *layer, ATMemoryAccessMode modes);
	void SetLayerMemory(ATMemoryLayer *layer, const uint8 *base);
	void SetLayerMemory(ATMemoryLayer *layer, const uint8 *base, uint32 pageOffset, uint32 pages, uint32 addrMask = 0xFFFFFFFFU, int readOnly = -1);
	void SetLayerMemoryAndAddressSpace(ATMemoryLayer *layer, const uint8 *base, uint32 addressSpace);
	void SetLayerMemoryAndAddressSpace(ATMemoryLayer *layer, const uint8 *base, uint32 pageOffset, uint32 pages, uint32 addressSpace, uint32 addrMask = 0xFFFFFFFFU, int readOnly = -1);
	void SetLayerAddressRange(ATMemoryLayer *layer0, uint32 pageOffset, uint32 pageCount);
	void SetLayerName(ATMemoryLayer *layer, const char *name);
	void SetLayerTag(ATMemoryLayer *layer, const void *tag);

	// Controls whether a memory layer exists on the chip RAM or fast RAM bus
	// (accelerated 65C816 mode only). The default is chip.
	void SetLayerFastBus(ATMemoryLayer *layer, bool fast);

	void SetLayerIoBus(ATMemoryLayer *layer, bool ioBus);

	void ClearLayerMaskRange(ATMemoryLayer *layer);
	void SetLayerMaskRange(ATMemoryLayer *layer, uint32 pageStart, uint32 pageCount);

	void SetLayerAddressSpace(ATMemoryLayer *layer, uint32 addressSpace);

	uint8 ReadFloatingDataBus() const;

	void WriteIoDataBus(uint8 c) { mIoBusValue = c; }
	uint8 ReadFloatingIoDataBus() const { return mIoBusValue; }

	uint8 AnticReadByte(uint32 address);
	uint8 DebugAnticReadByte(uint16 address);
	void DebugAnticReadMemory(void *dst, uint16 address, uint32 len);
	uint8 CPUReadByte(uint32 address) override;
	uint8 CPUExtReadByte(uint16 address, uint8 bank) override;
	sint32 CPUExtReadByteAccel(uint16 address, uint8 bank, bool chipOK) override;
	uint8 CPUDebugReadByte(uint16 address) const override;
	uint8 CPUDebugExtReadByte(uint16 address, uint8 bank) const override;
	void CPUWriteByte(uint16 address, uint8 value) override;
	void CPUExtWriteByte(uint16 address, uint8 bank, uint8 value) override;
	sint32 CPUExtWriteByteAccel(uint16 address, uint8 bank, uint8 value, bool chipOK) override;

	uint8 RedirectDebugReadByte(uint32 addr, const void *excludeTag);
	uint8 RedirectReadByte(uint32 addr, const void *excludeTag);
	void RedirectWriteByte(uint32 addr, uint8 value, const void *excludeTag);

protected:
	static constexpr uint32 kAddrSpaceInvalid = 0;
	
	struct MemoryLayer : public ATMemoryLayer {
		sint8 mPriority;
		uint8 mFlags;
		bool mbReadOnly;
		bool mbFastBus;
		bool mbIoBus;
		const uint8 *mpBase;
		uint32 mAddrMask;
		uint32 mPageOffset;
		uint32 mPageCount;
		ATMemoryHandlerTable mHandlers;
		const char *mpName;
		uint32 mMaskRangeStart;
		uint32 mMaskRangeEnd;
		uint32 mEffectiveStart;
		uint32 mEffectiveEnd;

		uint32 mAddressSpace = kAddrSpaceInvalid;

		const void *mpTag;
		ATMemoryManager *mpParent;

		void UpdateEffectiveRange();
	};

	struct MemoryLayerPred {
		bool operator()(const MemoryLayer *x, const MemoryLayer *y) const {
			return x->mPriority > y->mPriority;
		}
	};

	struct MemoryNode {
		uintptr mLayerOrForward;

		union {
			ATMemoryReadHandler mpReadHandler;
			ATMemoryWriteHandler mpWriteHandler;
		};

		void *mpThis;
		uintptr mNext;
	};

	struct AllocatorSet {
		uint32	mAllocationCount = 0;
		VDLinearAllocator mAllocator;
		VDLinearAllocator mAllocatorNext;
		VDLinearAllocator mAllocatorPrev;
	};
	
	typedef vdfastvector<MemoryLayer *> Layers;

	void RebuildAllNodes(uint32 base, uint32 n, uint8 modes);
	void RebuildNodes(PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode mode);
	void RebuildNodesSlow(Layers& VDRESTRICT layers, PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode mode);
	void RebuildNodesFast(MemoryLayer *layer, PageTable **bankTable, uint32 base, uint32 n, ATMemoryAccessMode mode);

	void SetBanksInactive(PageTable **bankTable, uint32 startBank, uint32 endBank, ATMemoryAccessMode accessMode);
	void SetBanksActive(PageTable **bankTable, uint32 startBank, uint32 endBank, ATMemoryAccessMode accessMode);

	MemoryNode *AllocNode(AllocatorSet& allocSet);
	void GarbageCollect(uint32 startBank, uint32 endBank, AllocatorSet& allocSet);

	static sint32 DummyReadHandler(void *thisptr, uint32 addr);
	static bool DummyWriteHandler(void *thisptr, uint32 addr, uint8 value);
	static sint32 ChipDebugReadHandler(void *thisptr, uint32 addr);
	static sint32 ChipReadHandler(void *thisptr, uint32 addr);
	static bool ChipWriteHandler(void *thisptr, uint32 addr, uint8 value);
	static sint32 IoMemoryFastReadWrapperHandler(void *thisptr, uint32 addr);
	static sint32 IoMemoryDebugReadWrapperHandler(void *thisptr, uint32 addr);
	static sint32 IoMemoryReadWrapperHandler(void *thisptr, uint32 addr);
	static bool IoMemoryRoWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value);
	static bool IoMemoryWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value);
	static sint32 IoHandlerReadWrapperHandler(void *thisptr, uint32 addr);
	static bool IoHandlerWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value);
	static bool IoNullWriteWrapperHandler(void *thisptr, uint32 addr, uint8 value);

	Layers mLayers;
	Layers mLayerTempList;

	bool	mbFloatingDataBus = false;
	bool	mbFloatingIoBus = false;
	bool	mbFastBusEnabled = false;
	bool	mbHighMemoryEnabled = false;
	bool	mbWrapBankZero = false;
	uint8	mIoBusValue = 0;

	AllocatorSet	mLoAllocators;
	AllocatorSet	mHiAllocators;

	VDALIGN(32) PageTable mCPUReadPageMap;
	PageTable		mCPUWritePageMap;
	PageTable		mAnticReadPageMap;
	uint32			mCPUReadAddressSpaceMap[256];

	PageTable		*mReadBankTable[256];
	PageTable		*mWriteBankTable[256];

	// All pages point to the dummy read/write node. This is used for uncommitted
	// banks.
	PageTable		mDummyReadPageTable;
	PageTable		mDummyWritePageTable;

	MemoryNode		mDummyReadNode;
	MemoryNode		mDummyWriteNode;
	MemoryLayer		mDummyLayer;

	PageTable		mHighMemoryReadPageTables[255];	// 256K!
	PageTable		mHighMemoryWritePageTables[255];	// 256K!
};

#endif	// f_AT_MEMORYMANAGER_H
