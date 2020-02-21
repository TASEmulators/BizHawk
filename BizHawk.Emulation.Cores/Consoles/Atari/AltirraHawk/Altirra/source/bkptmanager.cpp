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
#include <at/atcpu/breakpoints.h>
#include "bkptmanager.h"
#include "cpu.h"
#include "memorymanager.h"
#include "simulator.h"

class ATBreakpointManager::TargetBPHandler final : public IATCPUBreakpointHandler {
public:
	TargetBPHandler(ATBreakpointManager *parent, uint32 targetIndex)
		: mpParent(parent)
		, mTargetIndex(targetIndex)
	{
	}

	bool CheckBreakpoint(uint32 pc) override {
		int code = mpParent->TestPCBreakpoint(mTargetIndex, pc);
		if (code) {
			mpParent->OnTargetPCBreakpoint(code);
			return true;
		}

		return false;
	}

private:
	ATBreakpointManager *const mpParent;
	const uint32 mTargetIndex;
};

ATBreakpointManager::ATBreakpointManager()
	: mpMemMgr(NULL)
{
	memset(mAttrib, 0, sizeof mAttrib);
}

ATBreakpointManager::~ATBreakpointManager() {
}

void ATBreakpointManager::Init(ATCPUEmulator *cpu, ATMemoryManager *memmgr, ATSimulator *sim) {
	mpCPU = cpu;
	mpMemMgr = memmgr;
	mpSim = sim;
	cpu->SetBreakpointManager(this);
}

void ATBreakpointManager::Shutdown() {
	for(auto& entry : mTargets) {
		if (entry.mpBreakpoints)
			entry.mpBreakpoints->SetBreakpointHandler(nullptr);

		delete entry.mpBPHandler;
	}

	if (mpMemMgr) {
		for(AccessBPLayers::const_iterator it(mAccessBPLayers.begin()), itEnd(mAccessBPLayers.end()); it != itEnd; ++it) {
			mpMemMgr->DeleteLayer(it->second.mpMemLayer);
		}

		mpMemMgr = NULL;
	}

	mAccessBPLayers.clear();

	if (mpCPU) {
		mpCPU->SetBreakpointManager(NULL);
		mpCPU = NULL;
	}
}

void ATBreakpointManager::AttachTarget(uint32 targetIndex, IATDebugTarget *target) {
	if (mTargets.size() <= targetIndex) {
		mTargets.resize(targetIndex + 1, {});
		mInsnBreakpoints.resize(targetIndex + 1);
		mCPUBreakpoints.resize(targetIndex + 1);
	}

	auto *bps = vdpoly_cast<IATDebugTargetBreakpoints *>(target);
	mTargets[targetIndex] = { target, bps, new TargetBPHandler(this, targetIndex) };

	if (bps)
		bps->SetBreakpointHandler(mTargets[targetIndex].mpBPHandler);
}

void ATBreakpointManager::DetachTarget(uint32 targetIndex) {
	if (targetIndex < mTargets.size()) {
		auto& entry = mTargets[targetIndex];

		if (entry.mpBreakpoints)
			entry.mpBreakpoints->SetBreakpointHandler(nullptr);

		vdsafedelete <<= entry.mpBPHandler;

		mTargets[targetIndex] = {};
	} else {
		VDASSERT(!"Target out of range.");
	}
}

void ATBreakpointManager::GetAll(ATBreakpointIndices& indices) const {
	uint32 idx = 0;

	for(Breakpoints::const_iterator it(mBreakpoints.begin()), itEnd(mBreakpoints.end()); it != itEnd; ++it) {
		const BreakpointEntry& be = *it;

		if (be.mType)
			indices.push_back(idx + 1);

		++idx;
	}
}

void ATBreakpointManager::GetAtPC(uint32 targetIndex, uint32 pc, ATBreakpointIndices& indices) const {
	const auto& bps = mCPUBreakpoints[targetIndex];
	BreakpointsByAddress::const_iterator it(bps.find(pc & 0xFFFF));

	if (it == bps.end()) {
		indices.clear();
		return;
	}
	
	const auto& srcIndices = it->second;
	indices.clear();
	indices.reserve(srcIndices.size());

	for(const auto& idx : srcIndices) {
		const BreakpointEntry& be = mBreakpoints[idx - 1];

		if (be.mAddress == pc)
			indices.push_back(idx);
	}
}

void ATBreakpointManager::GetAtAccessAddress(uint32 addr, ATBreakpointIndices& indices) const {
	BreakpointsByAddress::const_iterator it(mAccessBreakpoints.find(addr));

	if (it == mAccessBreakpoints.end()) {
		indices.clear();
		return;
	}
	
	indices = it->second;
}

bool ATBreakpointManager::GetInfo(uint32 idx, ATBreakpointInfo& info) const {
	if (!idx || idx > mBreakpoints.size())
		return false;

	const BreakpointEntry& be = mBreakpoints[idx - 1];

	if (!be.mType)
		return false;

	info.mTargetIndex = be.mTargetIndex;
	info.mAddress = be.mAddress;
	info.mLength = 1;
	info.mbBreakOnPC = (be.mType & kBPT_PC) != 0;
	info.mbBreakOnInsn = (be.mType & kBPT_Insn) != 0;
	info.mbBreakOnRead = (be.mType & kBPT_Read) != 0;
	info.mbBreakOnWrite = (be.mType & kBPT_Write) != 0;

	if (be.mType & kBPT_Range) {
		std::pair<AccessRangeBreakpoints::const_iterator, AccessRangeBreakpoints::const_iterator> r(std::equal_range(mAccessRangeBreakpoints.begin(), mAccessRangeBreakpoints.end(), info.mAddress, BreakpointRangeAddressPred()));

		for(; r.first != r.second; ++r.first) {
			const BreakpointRangeEntry& bre = *r.first;

			if (bre.mIndex == idx) {
				info.mLength = bre.mLength;
				break;
			}
		}
	}

	return true;
}

uint32 ATBreakpointManager::SetInsnBP(uint32 targetIndex) {
	const uint32 id = AllocBreakpoint();

	BreakpointEntry& be = mBreakpoints[id - 1];
	be.mTargetIndex = targetIndex;
	be.mAddress = 0;
	be.mType = kBPT_Insn;

	auto& insnBPs = mInsnBreakpoints[targetIndex];

	if (insnBPs.empty()) {
		if (targetIndex)
			mTargets[targetIndex].mpBreakpoints->SetAllBreakpoints();
		else
			mpCPU->SetAllBreakpoints();
	}

	auto it = std::lower_bound(insnBPs.begin(), insnBPs.end(), id);

	insnBPs.insert(it, id);
	return id;
}

uint32 ATBreakpointManager::SetAtPC(uint32 targetIndex, uint32 pc) {
	// global PC breakpoints are only supported for target 0
	if (targetIndex)
		pc &= 0xffffff;

	const uint32 idx = AllocBreakpoint();

	BreakpointEntry& be = mBreakpoints[idx - 1];
	be.mTargetIndex = targetIndex;
	be.mAddress = pc;
	be.mType = kBPT_PC;

	const uint32 encodedBPC = pc & 0x0000FFFF;
	BreakpointsByAddress::insert_return_type r(mCPUBreakpoints[targetIndex].insert(encodedBPC));

	if (r.second) {
		if (targetIndex)
			mTargets[targetIndex].mpBreakpoints->SetBreakpoint((uint16)pc);
		else
			mpCPU->SetBreakpoint((uint16)pc);
	}

	r.first->second.push_back(idx);
	return idx;
}

uint32 ATBreakpointManager::SetAccessBP(uint32 address, bool read, bool write) {
	VDASSERT(read || write);

	address &= 0xFFFFFF;

	const uint32 idx = AllocBreakpoint();

	BreakpointEntry& be = mBreakpoints[idx - 1];
	be.mTargetIndex = 0;
	be.mAddress = address;
	be.mType = (read ? kBPT_Read : 0) + (write ? kBPT_Write : 0);

	BreakpointsByAddress::insert_return_type r(mAccessBreakpoints.insert(address));
	r.first->second.push_back(idx);

	RegisterAccessPage(address & 0xffff00, read, write);

	// set attribute flags on address
	uint8 attrFlags = 0;
	if (read)
		attrFlags |= kAttribReadBkpt;

	if (write)
		attrFlags |= kAttribWriteBkpt;

	mAttrib[address & 0xFFFF] |= attrFlags;

	return idx;	
}

uint32 ATBreakpointManager::SetAccessRangeBP(uint32 address, uint32 len, bool read, bool write) {
	VDASSERT(read || write);

	address &= 0xFFFFFF;

	if (address + len > 0x1000000)
		len = 0x1000000 - address;

	// create breakpoint entry
	const uint32 idx = AllocBreakpoint();

	BreakpointEntry& be = mBreakpoints[idx - 1];
	be.mTargetIndex = 0;
	be.mAddress = address;
	be.mType = (read ? kBPT_Read : 0) + (write ? kBPT_Write : 0) + kBPT_Range;

	// create range breakpoint entry
	BreakpointRangeEntry bre = {};
	bre.mAddress = address;
	bre.mLength = len;
	bre.mIndex = idx;
	bre.mAttrFlags = (read ? kAttribRangeReadBkpt : 0) + (write ? kAttribRangeWriteBkpt : 0);

	mAccessRangeBreakpoints.insert(std::lower_bound(mAccessRangeBreakpoints.begin(), mAccessRangeBreakpoints.end(), address, BreakpointRangeAddressPred()), bre);

	RecomputeRangePriorLimits();

	// register all access pages
	uint32 page1 = address & 0xffff00;
	uint32 page2 = (address + len - 1) & 0xffff00;

	for(uint32 page = page1; page <= page2; page += 0x100)
		RegisterAccessPage(page, read, write);

	// set attribute flags on all bytes in range
	for(uint32 i = 0; i < len; ++i)
		mAttrib[(address + i) & 0xFFFF] |= bre.mAttrFlags;

	return idx;
}

bool ATBreakpointManager::Clear(uint32 id) {
	if (!id || id > mBreakpoints.size())
		return false;

	BreakpointEntry& be = mBreakpoints[id - 1];

	if (!be.mType)
		return false;

	const uint32 address = be.mAddress;

	if (be.mType & (kBPT_Read | kBPT_Write)) {
		if (be.mType & kBPT_Range) {
			const bool read = (be.mType & kBPT_Read) != 0;
			const bool write = (be.mType & kBPT_Write) != 0;

			// find range breakpoint entry
			std::pair<AccessRangeBreakpoints::iterator, AccessRangeBreakpoints::iterator> r(std::equal_range(mAccessRangeBreakpoints.begin(), mAccessRangeBreakpoints.end(), be.mAddress, BreakpointRangeAddressPred()));

			bool bprfound = false;
			for(; r.first != r.second; ++r.first) {
				BreakpointRangeEntry& bre = *r.first;

				if (bre.mIndex == id) {
					const uint32 len = bre.mLength;

					// Decrement refcount over page range.
					uint32 page1 = address & 0xffff00;
					uint32 page2 = (address + len - 1) & 0xffff00;

					for(uint32 page = page1; page <= page2; page += 0x100)
						UnregisterAccessPage(page, read, write);

					// Delete range entry.
					mAccessRangeBreakpoints.erase(r.first);

					RecomputeRangePriorLimits();

					// Clear attribute flags in range.
					const uint32 limit = address + len;

					VDASSERT(limit <= 0x1000000);

					for(uint32 i = 0; i < len; ++i)
						mAttrib[(address + i) & 0xFFFF] &= ~(kAttribRangeReadBkpt | kAttribRangeWriteBkpt);

					// Reapply attribute flags for any other existing range breakpoints.
					AccessRangeBreakpoints::const_iterator itRemRange(std::upper_bound(mAccessRangeBreakpoints.begin(), mAccessRangeBreakpoints.end(), limit, BreakpointRangeAddressPred()));
					AccessRangeBreakpoints::const_iterator itRemRangeBegin(mAccessRangeBreakpoints.begin());

					while(itRemRange != itRemRangeBegin) {
						--itRemRange;
						const BreakpointRangeEntry& remRange = *itRemRange;

						// compute intersecting range
						uint32 remad1 = remRange.mAddress;
						uint32 remad2 = remRange.mAddress + remRange.mLength;

						if (remad1 < address)
							remad1 = address;

						if (remad2 > limit)
							remad2 = limit;

						// reapply attribute flags
						const uint8 remaf = remRange.mAttrFlags;

						for(uint32 remad = remad1; remad < remad2; ++remad)
							mAttrib[remad & 0xFFFF] |= remaf;

						// early out if we don't need to go any farther
						if (remRange.mPriorLimit <= address)
							break;
					}

					bprfound = true;
					break;
				}
			}

			if (!bprfound) {
				VDASSERT(!"Range breakpoint is missing range entry.");
			}
		} else {
			UnregisterAccessPage(address & 0xffff00, (be.mType & kBPT_Read) != 0, (be.mType & kBPT_Write) != 0);

			BreakpointsByAddress::iterator itAddr(mAccessBreakpoints.find(address));
			VDASSERT(itAddr != mAccessBreakpoints.end());

			BreakpointIndices& indices = itAddr->second;
			BreakpointIndices::iterator itIndex(std::find(indices.begin(), indices.end(), id));
			VDASSERT(itIndex != indices.end());

			indices.erase(itIndex);

			// recompute attributes for address
			uint8 attr = 0;
			for(itIndex = indices.begin(); itIndex != indices.end(); ++itIndex) {
				const BreakpointEntry& be = mBreakpoints[*itIndex - 1];

				if (be.mType & kBPT_Read)
					attr |= kAttribReadBkpt;

				if (be.mType & kBPT_Write)
					attr |= kAttribWriteBkpt;
			}

			mAttrib[address & 0xFFFF] = (mAttrib[address & 0xFFFF] & ~(kBPT_Read | kBPT_Write)) + attr;
		}
	}

	const uint32 targetIndex = be.mTargetIndex;
	if (be.mType & kBPT_PC) {
		auto& bps = mCPUBreakpoints[targetIndex];
		auto it = bps.find((uint16)address);
		VDASSERT(it != bps.end());

		BreakpointIndices& indices = it->second;
		BreakpointIndices::iterator itIndex(std::find(indices.begin(), indices.end(), id));
		VDASSERT(itIndex != indices.end());

		indices.erase(itIndex);

		if (indices.empty()) {
			bps.erase(it);

			if (mInsnBreakpoints[targetIndex].empty()) {
				if (targetIndex)
					mTargets[targetIndex].mpBreakpoints->ClearBreakpoint((uint16)address);
				else
					mpCPU->ClearBreakpoint((uint16)address);
			}
		}
	}

	if (be.mType & kBPT_Insn) {
		auto& insnBps = mInsnBreakpoints[targetIndex];
		auto it = std::lower_bound(insnBps.begin(), insnBps.end(), id);

		if (it != insnBps.end()) {
			insnBps.erase(it);

			if (insnBps.empty()) {
				// reinstate all regular breakpoints

				if (targetIndex)
					mTargets[targetIndex].mpBreakpoints->ClearAllBreakpoints();
				else
					mpCPU->ClearAllBreakpoints();

				for(const BreakpointEntry& be2 : mBreakpoints) {
					if (!(be2.mType & kBPT_PC))
						continue;

					if ((be2.mAddress ^ address) < 0x01000000)
						continue;

					if (targetIndex)
						mTargets[targetIndex].mpBreakpoints->SetBreakpoint((uint16)address);
					else
						mpCPU->SetBreakpoint((uint16)address);
				}
			}
		} else {
			VDFAIL("Insn breakpoint not found.");
		}
	}

	be.mType = 0;

	return true;
}

void ATBreakpointManager::ClearAll() {
	uint32 n = (uint32)mBreakpoints.size();

	for(uint32 i=0; i<n; ++i) {
		if (mBreakpoints[i].mType)
			Clear(i+1);
	}
}

uint32 ATBreakpointManager::AllocBreakpoint() {
	uint32 idx = (uint32)(std::find_if(mBreakpoints.begin(), mBreakpoints.end(), BreakpointFreePred()) - mBreakpoints.begin());

	if (idx >= mBreakpoints.size())
		mBreakpoints.push_back();

	return idx + 1;
}

void ATBreakpointManager::RecomputeRangePriorLimits() {
	uint32 priorLimit = 0;

	for(AccessRangeBreakpoints::iterator it(mAccessRangeBreakpoints.begin()), itEnd(mAccessRangeBreakpoints.end());
		it != itEnd;
		++it)
	{
		BreakpointRangeEntry& entry = *it;

		entry.mPriorLimit = priorLimit;

		const uint32 limit = entry.mAddress + entry.mLength;
		if (limit > priorLimit)
			priorLimit = limit;
	}
}

void ATBreakpointManager::RegisterAccessPage(uint32 address, bool read, bool write) {
	AccessBPLayers::insert_return_type r2(mAccessBPLayers.insert(address & 0xffff00));
	AccessBPLayer& layer = r2.first->second;
	if (r2.second) {
		ATMemoryHandlerTable handlers = {};
		handlers.mbPassAnticReads = true;
		handlers.mbPassReads = true;
		handlers.mbPassWrites = true;
		handlers.mpDebugReadHandler = NULL;
		handlers.mpReadHandler = OnAccessTrapRead;
		handlers.mpWriteHandler = OnAccessTrapWrite;
		handlers.mpThis = this;
		layer.mRefCountRead = 0;
		layer.mRefCountWrite = 0;
		layer.mpMemLayer = mpMemMgr->CreateLayer(kATMemoryPri_AccessBP, handlers, address >> 8, 1);
	}

	if (read) {
		if (!layer.mRefCountRead++)
			mpMemMgr->EnableLayer(layer.mpMemLayer, kATMemoryAccessMode_CPURead, true);
	}

	if (write) {
		if (!layer.mRefCountWrite++)
			mpMemMgr->EnableLayer(layer.mpMemLayer, kATMemoryAccessMode_CPUWrite, true);
	}
}

void ATBreakpointManager::UnregisterAccessPage(uint32 address, bool read, bool write) {
	AccessBPLayers::iterator it(mAccessBPLayers.find(address & 0xffff00));
	VDASSERT(it != mAccessBPLayers.end());

	AccessBPLayer& layer = it->second;

	if (read) {
		VDASSERT(layer.mRefCountRead);
		if (!--layer.mRefCountRead)
			mpMemMgr->EnableLayer(layer.mpMemLayer, kATMemoryAccessMode_CPURead, false);
	}

	if (write) {
		VDASSERT(layer.mRefCountWrite);
		if (!--layer.mRefCountWrite)
			mpMemMgr->EnableLayer(layer.mpMemLayer, kATMemoryAccessMode_CPUWrite, false);
	}

	if (!(layer.mRefCountRead | layer.mRefCountWrite)) {
		mpMemMgr->DeleteLayer(layer.mpMemLayer);
		mAccessBPLayers.erase(it);
	}
}

void ATBreakpointManager::OnTargetPCBreakpoint(int code) {
	mpSim->PostInterruptingEvent((ATSimulatorEvent)code);
}

int ATBreakpointManager::CheckPCBreakpoints(uint32 targetIndex, uint32 bpc, const BreakpointIndices *bpidxs) {
	bool shouldBreak = false;
	bool noisyBreak = false;

	uint32 xpc;
	bool haveXpc = false;
	if (bpidxs) {
		for(const uint32 idx : *bpidxs) {
			const BreakpointEntry& bpe = mBreakpoints[idx - 1];

			// check if we have a global address breakpoint
			if (bpe.mAddress >= 0x1000000) {
				// global breakpoint -- fetch XPC if we don't have it
				// already and check it
				if (!haveXpc) {
					haveXpc = true;
					xpc = mpCPU->GetXPC();
				}

				if (bpe.mAddress != xpc)
					continue;
			} else {
				// regular breakpoint -- check PBK:PC
				if (bpe.mAddress != bpc)
					continue;
			}

			ATBreakpointEvent ev;
			ev.mIndex = idx;
			ev.mTargetIndex = targetIndex;
			ev.mAddress = bpc;
			ev.mValue = 0;
			ev.mbBreak = false;
			ev.mbSilentBreak = false;

			mEventBreakpointHit.Raise(this, &ev);

			if (ev.mbBreak) {
				shouldBreak = true;
				if (!ev.mbSilentBreak)
					noisyBreak = true;
			}
		}
	}

	const auto& insnBPs = mInsnBreakpoints[targetIndex];

	for(const uint32 id : insnBPs) {
		ATBreakpointEvent ev;
		ev.mIndex = id;
		ev.mTargetIndex = targetIndex;
		ev.mAddress = bpc;
		ev.mValue = 0;
		ev.mbBreak = false;
		ev.mbSilentBreak = false;

		mEventBreakpointHit.Raise(this, &ev);

		if (ev.mbBreak) {
			shouldBreak = true;
			if (!ev.mbSilentBreak)
				noisyBreak = true;
		}
	}

	return shouldBreak ? noisyBreak ? kATSimEvent_CPUPCBreakpoint : kATSimEvent_AnonymousInterrupt : kATSimEvent_None;
}

sint32 ATBreakpointManager::OnAccessTrapRead(void *thisptr0, uint32 addr) {
	ATBreakpointManager *thisptr = (ATBreakpointManager *)thisptr0;
	const uint8 attr = thisptr->mAttrib[addr & 0xFFFF];

	if (!(attr & (kAttribReadBkpt | kAttribRangeReadBkpt)))
		return -1;

	bool shouldBreak = false;
	bool noisyBreak = false;

	if (attr & kAttribReadBkpt) {
		BreakpointIndices& bpidxs = thisptr->mAccessBreakpoints.find(addr)->second;

		for(BreakpointIndices::const_iterator it(bpidxs.begin()), itEnd(bpidxs.end()); it != itEnd; ++it) {
			const uint32 idx = *it;
			const BreakpointEntry& bpe = thisptr->mBreakpoints[idx - 1];

			if (!(bpe.mType & kBPT_Read))
				continue;

			ATBreakpointEvent ev;
			ev.mIndex = idx;
			ev.mTargetIndex = 0;
			ev.mAddress = addr;
			ev.mValue = 0;
			ev.mbBreak = false;
			ev.mbSilentBreak = false;

			thisptr->mEventBreakpointHit.Raise(thisptr, &ev);

			if (ev.mbBreak) {
				shouldBreak = true;
				if (!ev.mbSilentBreak)
					noisyBreak = true;
			}
		}
	}

	if (attr & kAttribRangeReadBkpt) {
		AccessRangeBreakpoints::const_iterator it2(std::upper_bound(thisptr->mAccessRangeBreakpoints.begin(), thisptr->mAccessRangeBreakpoints.end(), addr, BreakpointRangeAddressPred()));
		AccessRangeBreakpoints::const_iterator it2Begin(thisptr->mAccessRangeBreakpoints.begin());
		while(it2 != it2Begin) {
			--it2;

			const BreakpointRangeEntry& bre = *it2;

			if ((bre.mAttrFlags & kAttribRangeReadBkpt) && (addr - bre.mAddress) < bre.mLength) {
				const uint32 idx = bre.mIndex;

				ATBreakpointEvent ev;
				ev.mIndex = idx;
				ev.mTargetIndex = 0;
				ev.mAddress = addr;
				ev.mValue = 0;
				ev.mbBreak = false;
				ev.mbSilentBreak = false;

				thisptr->mEventBreakpointHit.Raise(thisptr, &ev);

				if (ev.mbBreak) {
					shouldBreak = true;
					if (!ev.mbSilentBreak)
						noisyBreak = true;
				}
			}

			if (bre.mPriorLimit <= addr)
				break;
		}
	}

	if (shouldBreak)
		thisptr->mpSim->PostInterruptingEvent(noisyBreak ? kATSimEvent_ReadBreakpoint : kATSimEvent_AnonymousInterrupt);

	return -1;
}

bool ATBreakpointManager::OnAccessTrapWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATBreakpointManager *thisptr = (ATBreakpointManager *)thisptr0;
	const uint8 attr = thisptr->mAttrib[addr & 0xFFFF];

	if (!(attr & (kAttribWriteBkpt | kAttribRangeWriteBkpt)))
		return false;

	bool shouldBreak = false;
	bool noisyBreak = false;

	if (attr & kAttribWriteBkpt) {
		BreakpointIndices& bpidxs = thisptr->mAccessBreakpoints.find(addr)->second;

		for(BreakpointIndices::const_iterator it(bpidxs.begin()), itEnd(bpidxs.end()); it != itEnd; ++it) {
			const uint32 idx = *it;
			const BreakpointEntry& bpe = thisptr->mBreakpoints[idx - 1];

			if (!(bpe.mType & kBPT_Write))
				continue;

			ATBreakpointEvent ev;
			ev.mIndex = idx;
			ev.mTargetIndex = 0;
			ev.mAddress = addr;
			ev.mValue = value;
			ev.mbBreak = false;
			ev.mbSilentBreak = false;

			thisptr->mEventBreakpointHit.Raise(thisptr, &ev);

			if (ev.mbBreak) {
				shouldBreak = true;
				if (!ev.mbSilentBreak)
					noisyBreak = true;
			}
		}
	}

	if (attr & kAttribRangeWriteBkpt) {
		AccessRangeBreakpoints::const_iterator it2(std::upper_bound(thisptr->mAccessRangeBreakpoints.begin(), thisptr->mAccessRangeBreakpoints.end(), addr, BreakpointRangeAddressPred()));
		AccessRangeBreakpoints::const_iterator it2Begin(thisptr->mAccessRangeBreakpoints.begin());
		while(it2 != it2Begin) {
			--it2;

			const BreakpointRangeEntry& bre = *it2;

			if ((bre.mAttrFlags & kAttribRangeWriteBkpt) && (addr - bre.mAddress) < bre.mLength) {
				const uint32 idx = bre.mIndex;

				ATBreakpointEvent ev;
				ev.mIndex = idx;
				ev.mTargetIndex = 0;
				ev.mAddress = addr;
				ev.mValue = value;
				ev.mbBreak = false;
				ev.mbSilentBreak = false;

				thisptr->mEventBreakpointHit.Raise(thisptr, &ev);

				if (ev.mbBreak) {
					shouldBreak = true;
					if (!ev.mbSilentBreak)
						noisyBreak = true;
				}
			}

			if (bre.mPriorLimit <= addr)
				break;
		}
	}

	if (shouldBreak)
		thisptr->mpSim->PostInterruptingEvent(noisyBreak ? kATSimEvent_WriteBreakpoint : kATSimEvent_AnonymousInterrupt);

	return false;
}
