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
#include "oshelper.h"
#include "firmwaremanager.h"
#include "uifirmwaremenu.h"
#include "uimenu.h"
#include <at/atui/uimenulist.h>
#include "simulator.h"

extern ATSimulator g_sim;
extern void ATUISwitchKernel(uint64 id);
extern void ATUISwitchBasic(uint64 id);

namespace {
	struct SortFirmwarePtrsByName {
		bool operator()(const ATFirmwareInfo *x, const ATFirmwareInfo *y) const {
			return x->mName.comparei(y->mName) < 0;
		}
	};
}

class ATUIFirmwareMenuProvider final : public IATUIDynamicMenuProvider {
public:
	void Init(bool basic);

	bool IsRebuildNeeded() const override;
	void RebuildMenu(ATUIMenu& menu, uint32 idbase) override;
	void UpdateMenu(ATUIMenu& menu, uint32 firstIndex, uint32 n) override;
	void HandleMenuCommand(uint32 index) override;

protected:
	bool mbIsBasic = false;;
	ATHardwareMode mLastHardwareMode = kATHardwareModeCount;	// deliberately invalid
	vdfastvector<uint64> mFirmwareIds;
};

void ATUIFirmwareMenuProvider::Init(bool basic) {
	mbIsBasic = basic;
}

bool ATUIFirmwareMenuProvider::IsRebuildNeeded() const {
	return !mbIsBasic && g_sim.GetHardwareMode() != mLastHardwareMode;
}

void ATUIFirmwareMenuProvider::RebuildMenu(ATUIMenu& menu, uint32 idbase) {
	ATFirmwareManager *fwmgr = g_sim.GetFirmwareManager();

	vdvector<ATFirmwareInfo> fws;

	fwmgr->GetFirmwareList(fws);

	typedef vdfastvector<const ATFirmwareInfo *> SortedFirmwares;
	SortedFirmwares sortedFirmwares;

	if (mbIsBasic) {
		ATUIGetBASICFirmwareList(fws, sortedFirmwares);
	} else {
		mLastHardwareMode = g_sim.GetHardwareMode();
		ATUIGetKernelFirmwareList(mLastHardwareMode, fws, sortedFirmwares);
	}

	mFirmwareIds.clear();

	for(SortedFirmwares::const_iterator it(sortedFirmwares.begin()), itEnd(sortedFirmwares.end());
		it != itEnd;
		++it)
	{
		const ATFirmwareInfo& fwi = **it;

		mFirmwareIds.push_back(fwi.mId);

		ATUIMenuItem item;
		item.mId = idbase++;
		item.mText = fwi.mName;
		menu.AddItem(item);
	}
}

void ATUIFirmwareMenuProvider::UpdateMenu(ATUIMenu& menu, uint32 firstIndex, uint32 n) {
	uint64 id = mbIsBasic ? g_sim.GetBasicId() : g_sim.GetKernelId();

	for(uint32 i=0; i<n; ++i) {
		ATUIMenuItem *item = menu.GetItemByIndex(firstIndex + i);

		if (i < mFirmwareIds.size())
			item->mbRadioChecked = (mFirmwareIds[i] == id);
	}
}

void ATUIFirmwareMenuProvider::HandleMenuCommand(uint32 index) {
	if (index < mFirmwareIds.size()) {
		if (mbIsBasic)
			ATUISwitchBasic(mFirmwareIds[index]);
		else
			ATUISwitchKernel(mFirmwareIds[index]);
	}
}

///////////////////////////////////////////////////////////////////////////

ATUIFirmwareMenuProvider g_ATUIFirmwareMenuProviders[2];

void ATUIInitFirmwareMenuCallbacks(ATFirmwareManager *fwmgr) {
	g_ATUIFirmwareMenuProviders[0].Init(false);
	ATUISetDynamicMenuProvider(0, &g_ATUIFirmwareMenuProviders[0]);

	g_ATUIFirmwareMenuProviders[1].Init(true);
	ATUISetDynamicMenuProvider(1, &g_ATUIFirmwareMenuProviders[1]);
}

///////////////////////////////////////////////////////////////////////////

void ATUIGetKernelFirmwareList(ATHardwareMode hwmode, vdvector<ATFirmwareInfo>& firmwareList, vdfastvector<const ATFirmwareInfo *>& sortedFirmwareList) {
	ATFirmwareManager *fwmgr = g_sim.GetFirmwareManager();

	fwmgr->GetFirmwareList(firmwareList);

	for(const ATFirmwareInfo& fwi : firmwareList) {
		if (!fwi.mbVisible)
			continue;

		switch(fwi.mType) {
			case kATFirmwareType_Kernel800_OSA:
			case kATFirmwareType_Kernel800_OSB:
				if (hwmode == kATHardwareMode_5200)
					continue;
				break;

			case kATFirmwareType_KernelXL:
			case kATFirmwareType_KernelXEGS:
			case kATFirmwareType_Kernel1200XL:
				if (hwmode != kATHardwareMode_1200XL &&
					hwmode != kATHardwareMode_130XE &&
					hwmode != kATHardwareMode_800XL &&
					hwmode != kATHardwareMode_XEGS)
					continue;
				break;

			case kATFirmwareType_Kernel5200:
				if (hwmode != kATHardwareMode_5200)
					continue;
				break;

			default:
				continue;
		}

		sortedFirmwareList.push_back(&fwi);
	}

	std::sort(sortedFirmwareList.begin(), sortedFirmwareList.end(),
		[](const ATFirmwareInfo *x, const ATFirmwareInfo *y) { return x->mName.comparei(y->mName) < 0; });
}

void ATUIGetBASICFirmwareList(vdvector<ATFirmwareInfo>& firmwareList, vdfastvector<const ATFirmwareInfo *>& sortedFirmwareList) {
	ATFirmwareManager *fwmgr = g_sim.GetFirmwareManager();

	fwmgr->GetFirmwareList(firmwareList);

	for(const ATFirmwareInfo& fwi : firmwareList) {
		if (fwi.mbVisible && fwi.mType == kATFirmwareType_Basic)
			sortedFirmwareList.push_back(&fwi);
	}

	std::sort(sortedFirmwareList.begin(), sortedFirmwareList.end(),
		[](const ATFirmwareInfo *x, const ATFirmwareInfo *y) { return x->mName.comparei(y->mName) < 0; });
}
