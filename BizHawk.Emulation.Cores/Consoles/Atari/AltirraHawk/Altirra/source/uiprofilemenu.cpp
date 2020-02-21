//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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
#include <at/atui/uimenulist.h>
#include "settings.h"
#include "uimenu.h"

class ATUIProfileMenuProvider final : public IATUIDynamicMenuProvider {
public:
	bool IsRebuildNeeded() const override { return false; }
	void RebuildMenu(ATUIMenu& menu, uint32 idbase) override;
	void UpdateMenu(ATUIMenu& menu, uint32 firstIndex, uint32 n) override;
	void HandleMenuCommand(uint32 index) override;

protected:
	vdfastvector<uint32> mProfileIds;
};

void ATUIProfileMenuProvider::RebuildMenu(ATUIMenu& menu, uint32 idbase) {
	mProfileIds.clear();

	ATSettingsProfileEnum(mProfileIds);
	mProfileIds.insert(mProfileIds.begin(), 0);

	mProfileIds.erase(
		std::remove_if(mProfileIds.begin(), mProfileIds.end(), [](uint32 id) { return !ATSettingsProfileGetVisible(id); }),
		mProfileIds.end());

	const size_t n = mProfileIds.size();
	vdvector<VDStringW> profileNames(n);

	if (n) {	// needed to work around bogus VS2013 null pointer assert on empty ranges
		std::transform(mProfileIds.begin(), mProfileIds.end(), profileNames.begin(),
			[](uint32 id) { return ATSettingsProfileGetName(id); });
	}

	vdfastvector<uint32> profileSort(n);

	for(size_t i=0; i<n; ++i)
		profileSort[i] = (uint32)i;

	std::sort(profileSort.begin(), profileSort.end(),
		[&](uint32 i, uint32 j) { return profileNames[i].comparei(profileNames[j]) < 0; });

	for(uint32 index : profileSort) {
		ATUIMenuItem item;
		item.mId = idbase++;
		item.mText = profileNames[index];
		menu.AddItem(item);
	}

	std::transform(profileSort.begin(), profileSort.end(), profileSort.begin(),
		[this](uint32 i) { return mProfileIds[i]; });

	mProfileIds.swap(profileSort);
}

void ATUIProfileMenuProvider::UpdateMenu(ATUIMenu& menu, uint32 firstIndex, uint32 n) {
	const uint32 id = ATSettingsGetCurrentProfileId();

	for(uint32 i=0; i<n; ++i) {
		ATUIMenuItem *item = menu.GetItemByIndex(firstIndex + i);

		if (i < mProfileIds.size())
			item->mbRadioChecked = (mProfileIds[i] == id);
	}
}

void ATUIProfileMenuProvider::HandleMenuCommand(uint32 index) {
	if (index < mProfileIds.size()) {
		ATSettingsSwitchProfile(mProfileIds[index]);
	}
}

///////////////////////////////////////////////////////////////////////////

ATUIProfileMenuProvider g_ATUIProfileMenuProviders;

void ATUIInitProfileMenuCallbacks() {
	ATUISetDynamicMenuProvider(kATUIDynamicMenu_Profile, &g_ATUIProfileMenuProviders);
}
