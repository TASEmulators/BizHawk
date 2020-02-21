//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include <windows.h>
#include <vd2/system/strutil.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include "inputmanager.h"
#include "resource.h"
#include <at/atui/uimenulist.h>

class ATInputPortMenu {
public:
	ATInputPortMenu();
	~ATInputPortMenu();

	void Init(ATInputManager *im, int portIdx, uint32 baseId);
	void Shutdown();

	void SetMenuHandles(HMENU hSubMenu, ATUIMenu *pMenu);

	void Reload();
	void UpdateMenu();
	void HandleCommand(uint32 id);

protected:
	int mPortIdx;
	uint32 mBaseId;
	HMENU mhmenu;
	ATUIMenu *mpMenu;
	ATInputManager *mpInputMgr;

	typedef vdfastvector<ATInputMap *> InputMaps;
	InputMaps mInputMaps;

	struct InputMapSort;
};

struct ATInputPortMenu::InputMapSort {
	bool operator()(const ATInputMap *x, const ATInputMap *y) const {
		return vdwcsicmp(x->GetName(), y->GetName()) < 0;
	}
};

ATInputPortMenu::ATInputPortMenu()
	: mPortIdx(0)
	, mBaseId(0)
	, mhmenu(NULL)
	, mpMenu(NULL)
	, mpInputMgr(NULL)
{
}

ATInputPortMenu::~ATInputPortMenu() {
	Shutdown();
}

void ATInputPortMenu::Init(ATInputManager *im, int portIdx, uint32 baseId) {
	mpInputMgr = im;
	mPortIdx = portIdx;
	mBaseId = baseId;
}

void ATInputPortMenu::SetMenuHandles(HMENU subMenu, ATUIMenu *pmenu) {
	mhmenu = subMenu;
	mpMenu = pmenu;
}

void ATInputPortMenu::Shutdown() {
	while(!mInputMaps.empty()) {
		mInputMaps.back()->Release();
		mInputMaps.pop_back();
	}

	mpInputMgr = NULL;
	mhmenu = NULL;
}

void ATInputPortMenu::Reload() {
	if (!mpInputMgr)
		return;

	// clear existing items
	if (mhmenu) {
		int count = ::GetMenuItemCount(mhmenu);
		for(int i=count-1; i>=1; --i)
			::DeleteMenu(mhmenu, i, MF_BYPOSITION);
	}

	if (mpMenu)
		mpMenu->RemoveItems(1, mpMenu->GetItemCount());

	while(!mInputMaps.empty()) {
		mInputMaps.back()->Release();
		mInputMaps.pop_back();
	}

	// find input maps that touch a port
	uint32 mapCount = mpInputMgr->GetInputMapCount();
	for(uint32 i=0; i<mapCount; ++i) {
		vdrefptr<ATInputMap> imap;
		if (mpInputMgr->GetInputMapByIndex(i, ~imap)) {
			if (imap->UsesPhysicalPort(mPortIdx)) {
				mInputMaps.push_back(imap);
				imap.release();
			}
		}
	}

	// sort input maps
	std::sort(mInputMaps.begin(), mInputMaps.end(), InputMapSort());

	// populate menu
	const uint32 entryCount = (uint32)mInputMaps.size();
	for(uint32 i=0; i<entryCount; ++i) {
		ATInputMap *imap = mInputMaps[i];

		if (mhmenu)
			VDAppendMenuW32(mhmenu, MF_STRING, mBaseId + i + 1, imap->GetName());

		if (mpMenu) {
			ATUIMenuItem item;
			item.mText = imap->GetName();
			item.mId = mBaseId + i + 1;
			mpMenu->AddItem(item);
		}
	}
}

void ATInputPortMenu::UpdateMenu() {
	uint32 n = (uint32)mInputMaps.size();
	bool anyActive = false;

	for(uint32 i=0; i<n; ++i) {
		bool active = false;

		if (mpInputMgr->IsInputMapEnabled(mInputMaps[i])) {
			active = true;
			anyActive = true;
		}

		if (mhmenu)
			VDCheckRadioMenuItemByPositionW32(mhmenu, i+1, active);

		if (mpMenu) {
			ATUIMenuItem *item = mpMenu->GetItemByIndex(i+1);

			if (item)
				item->mbRadioChecked = active;
		}
	}

	if (mhmenu)
		VDCheckRadioMenuItemByPositionW32(mhmenu, 0, !anyActive);

	if (mpMenu) {
		ATUIMenuItem *item = mpMenu->GetItemByIndex(0);

		if (item)
			item->mbRadioChecked = !anyActive;
	}
}

void ATInputPortMenu::HandleCommand(uint32 id) {
	uint32 i = 1;

	for(InputMaps::const_iterator it(mInputMaps.begin()), itEnd(mInputMaps.end()); it != itEnd; ++it, ++i) {
		ATInputMap *imap = *it;

		mpInputMgr->ActivateInputMap(imap, id == (mBaseId + i));
	}
}

ATInputPortMenu g_portMenus[4];

HMENU ATFindParentMenuW32(HMENU hmenuStart, UINT id) {
	int n = GetMenuItemCount(hmenuStart);
	for(int i=0; i<n; ++i) {
		if (GetMenuItemID(hmenuStart, i) == id)
			return hmenuStart;

		HMENU hSubMenu = GetSubMenu(hmenuStart, i);
		if (hSubMenu) {
			HMENU result = ATFindParentMenuW32(hSubMenu, id);
			if (result)
				return result;
		}
	}

	return NULL;
}

void ATInitPortMenus(ATInputManager *im) {
	const uint32 kBaseIds[]={
		ID_INPUT_PORT1_NONE,
		ID_INPUT_PORT2_NONE,
		ID_INPUT_PORT3_NONE,
		ID_INPUT_PORT4_NONE,
	};

	for(int i=0; i<4; ++i) {
		g_portMenus[i].Init(im, i, kBaseIds[i]);
		g_portMenus[i].Reload();
	}
}


void ATSetPortMenu(int idx, HMENU hmenu, ATUIMenu *pmenu) {
	g_portMenus[idx].SetMenuHandles(hmenu, pmenu);
	g_portMenus[idx].Reload();
}

void ATUpdatePortMenus() {
	for(int i=0; i<4; ++i)
		g_portMenus[i].UpdateMenu();
}

void ATShutdownPortMenus() {
	for(int i=0; i<4; ++i)
		g_portMenus[i].Shutdown();
}

void ATReloadPortMenus() {
	for(int i=0; i<4; ++i)
		g_portMenus[i].Reload();
}

bool ATUIHandlePortMenuCommand(uint32 id) {
	if ((id - ID_INPUT_PORT1_NONE) < 100) {
		g_portMenus[0].HandleCommand(id);
		return true;
	}

	if ((id - ID_INPUT_PORT2_NONE) < 100) {
		g_portMenus[1].HandleCommand(id);
		return true;
	}

	if ((id - ID_INPUT_PORT3_NONE) < 100) {
		g_portMenus[2].HandleCommand(id);
		return true;
	}

	if ((id - ID_INPUT_PORT4_NONE) < 100) {
		g_portMenus[3].HandleCommand(id);
		return true;
	}

	return false;
}
