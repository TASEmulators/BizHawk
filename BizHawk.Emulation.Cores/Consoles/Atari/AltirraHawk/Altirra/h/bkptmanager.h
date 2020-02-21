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

#ifndef f_AT_BKPTMANAGER_H
#define f_AT_BKPTMANAGER_H

#include <vd2/system/vdstl.h>
#include <vd2/system/event.h>
#include <at/atdebugger/target.h>

class ATCPUEmulator;
class ATMemoryManager;
class ATMemoryLayer;
class ATSimulator;

typedef vdfastvector<uint32> ATBreakpointIndices;

struct ATBreakpointInfo {
	uint32	mTargetIndex;
	sint32	mAddress;
	uint32	mLength;
	bool	mbBreakOnPC;
	bool	mbBreakOnInsn;
	bool	mbBreakOnRead;
	bool	mbBreakOnWrite;
};

struct ATBreakpointEvent {
	uint32	mIndex;
	uint32	mTargetIndex;
	uint32	mAddress;
	uint8	mValue;
	bool	mbBreak;
	bool	mbSilentBreak;
};

class ATBreakpointManager {
	ATBreakpointManager(const ATBreakpointManager&) = delete;
	ATBreakpointManager& operator=(const ATBreakpointManager&) = delete;

public:
	ATBreakpointManager();
	~ATBreakpointManager();

	void Init(ATCPUEmulator *cpu, ATMemoryManager *memmgr, ATSimulator *sim);
	void Shutdown();

	bool AreBreakpointsSupported(uint32 targetIndex) const {
		return mTargets[targetIndex].mpBreakpoints != nullptr;
	}

	void AttachTarget(uint32 targetIndex, IATDebugTarget *target);
	void DetachTarget(uint32 targetIndex);

	void GetAll(ATBreakpointIndices& indices) const;
	void GetAtPC(uint32 targetIndex, uint32 pc, ATBreakpointIndices& indices) const;
	void GetAtAccessAddress(uint32 addr, ATBreakpointIndices& indices) const;
	bool GetInfo(uint32 idx, ATBreakpointInfo& info) const;

	uint32 SetInsnBP(uint32 targetIndex);
	uint32 SetAtPC(uint32 targetIndex, uint32 pc);
	uint32 SetAccessBP(uint32 address, bool read, bool write);
	uint32 SetAccessRangeBP(uint32 address, uint32 len, bool read, bool write);
	bool Clear(uint32 id);
	void ClearAll();

	VDEvent<ATBreakpointManager, ATBreakpointEvent *>& OnBreakpointHit() { return mEventBreakpointHit; }

	inline int TestPCBreakpoint(uint32 targetIndex, uint32 pc);

protected:
	typedef ATBreakpointIndices BreakpointIndices;

	class TargetBPHandler;

	struct TargetInfo {
		IATDebugTarget *mpTarget;
		IATDebugTargetBreakpoints *mpBreakpoints;
		TargetBPHandler *mpBPHandler;
	};

	enum BreakpointType {
		kBPT_PC			= 0x01,
		kBPT_Insn		= 0x02,
		kBPT_Read		= 0x04,
		kBPT_Write		= 0x08,
		kBPT_Range		= 0x10
	};

	struct BreakpointEntry {
		uint32	mTargetIndex;
		uint32	mAddress;
		uint8	mType;
	};

	struct BreakpointFreePred {
		bool operator()(const BreakpointEntry& x) const {
			return !x.mType;
		}
	};

	struct BreakpointRangeEntry {
		uint32	mAddress;			///< Base address of range.
		uint32	mLength;			///< Length of range in bytes.
		uint32	mIndex;				///< Breakpoint index.
		uint32	mPriorLimit;		///< Highest address+length of any previous range.
		uint8	mAttrFlags;			
	};

	struct BreakpointRangeAddressPred {
		bool operator()(const BreakpointRangeEntry& x, const BreakpointRangeEntry& y) const {
			return x.mAddress < y.mAddress;
		}

		bool operator()(const BreakpointRangeEntry& x, uint32 address) const {
			return x.mAddress < address;
		}

		bool operator()(uint32 address, const BreakpointRangeEntry& y) const {
			return address < y.mAddress;
		}
	};

	uint32 AllocBreakpoint();

	void RecomputeRangePriorLimits();
	void RegisterAccessPage(uint32 address, bool read, bool write);
	void UnregisterAccessPage(uint32 address, bool read, bool write);

	void OnTargetPCBreakpoint(int code);
	int CheckPCBreakpoints(uint32 targetIndex, uint32 pc, const BreakpointIndices *bps);	
	static sint32 OnAccessTrapRead(void *thisptr, uint32 addr);
	static bool OnAccessTrapWrite(void *thisptr, uint32 addr, uint8 value);

	ATCPUEmulator *mpCPU;
	ATMemoryManager *mpMemMgr;
	ATSimulator *mpSim;

	vdfastvector<TargetInfo> mTargets;

	typedef vdfastvector<BreakpointEntry> Breakpoints;
	Breakpoints mBreakpoints;

	vdvector<vdfastvector<uint32>> mInsnBreakpoints;

	typedef vdhashmap<uint32, BreakpointIndices> BreakpointsByAddress;
	vdvector<BreakpointsByAddress> mCPUBreakpoints;
	BreakpointsByAddress mAccessBreakpoints;

	typedef vdfastvector<BreakpointRangeEntry> AccessRangeBreakpoints;
	AccessRangeBreakpoints mAccessRangeBreakpoints;

	struct AccessBPLayer {
		uint32 mRefCountRead;
		uint32 mRefCountWrite;
		ATMemoryLayer *mpMemLayer;
	};

	typedef vdhashmap<uint32, AccessBPLayer> AccessBPLayers;
	AccessBPLayers mAccessBPLayers;

	VDEvent<ATBreakpointManager, ATBreakpointEvent *> mEventBreakpointHit;

	enum {
		kAttribReadBkpt = 0x01,
		kAttribWriteBkpt = 0x02,
		kAttribRangeReadBkpt = 0x04,
		kAttribRangeWriteBkpt = 0x08
	};

	uint8 mAttrib[0x10000];
};

inline int ATBreakpointManager::TestPCBreakpoint(uint32 targetIndex, uint32 bpc) {
	const BreakpointsByAddress& bps = mCPUBreakpoints[targetIndex];
	BreakpointsByAddress::const_iterator it(bps.find((uint16)bpc));
	if (it == bps.end()) {
		if (mInsnBreakpoints[targetIndex].empty())
			return 0;

		return CheckPCBreakpoints(targetIndex, bpc, nullptr);
	}

	return CheckPCBreakpoints(targetIndex, bpc, &it->second);
}

#endif	// f_AT_BKPTMANAGER_H
