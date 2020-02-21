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
#include <vd2/system/registry.h>
#include <vd2/system/w32assist.h>
#include "uimrulist.h"
#include <at/atui/uimenulist.h>

namespace {
	HMENU g_hMenuMRU;
	ATUIMenu *g_pMenuMRU;
	UINT g_menuMRUBaseId;
	UINT g_menuMRUClearId;
}

void ATClearMRUList() {
	VDRegistryAppKey key("MRU List", true);

	key.removeValue("Order");

	ATUpdateMRUListMenu();
}

void ATAddMRUListItem(const wchar_t *path) {
	VDRegistryAppKey key("MRU List", true);

	VDStringW mrustr;
	key.getString("Order", mrustr);

	// check if we already have this string
	VDStringW existingPath;
	for(VDStringW::const_iterator it(mrustr.begin()), itEnd(mrustr.end());
		it != itEnd;
		++it)
	{
		char keyname[2] = { (char)*it, 0 };
		key.getString(keyname, existingPath);

		if (existingPath.comparei(path) == 0) {
			uint32 existingIndex = (uint32)(it - mrustr.begin());
			ATPromoteMRUListItem(existingIndex);
			return;
		}
	}

	int recycleIndex = 0;
	if (mrustr.size() >= 10) {
		wchar_t c = mrustr.back();

		if (c >= L'A' && c < L'A' + 10)
			recycleIndex = c - L'A';

		mrustr.resize(9);
	} else {
		recycleIndex = mrustr.size();
	}

	mrustr.insert(mrustr.begin(), L'A' + recycleIndex);

	char keyname[2] = { (char)('A' + recycleIndex), 0 };
	key.setString(keyname, path);
	key.setString("Order", mrustr.c_str());

	ATUpdateMRUListMenu();
}

VDStringW ATGetMRUListItem(uint32 index) {
	VDRegistryAppKey key("MRU List", false);

	VDStringW mrustr;
	key.getString("Order", mrustr);

	VDStringW s;
	if (index < mrustr.size()) {
		char keyname[2] = { (char)mrustr[index], 0 };

		key.getString(keyname, s);
	}

	return s;
}

void ATPromoteMRUListItem(uint32 index) {
	if (index == 0)
		return;

	VDRegistryAppKey key("MRU List", true);

	VDStringW mrustr;
	key.getString("Order", mrustr);

	if (index < mrustr.size()) {
		const wchar_t c = mrustr[index];

		mrustr.erase(index, 1);
		mrustr.insert(mrustr.begin(), c);
		key.setString("Order", mrustr.c_str());

		ATUpdateMRUListMenu();
	}
}

void ATRegisterMRUListMenu(HMENU hmenu, ATUIMenu *pmenu, UINT baseId, UINT clearId) {
	g_hMenuMRU = hmenu;
	g_pMenuMRU = pmenu;
	g_menuMRUBaseId = baseId;
	g_menuMRUClearId = clearId;

	ATUpdateMRUListMenu();
}

void ATUnregisterMRUListMenu() {
	g_hMenuMRU = nullptr;
	g_pMenuMRU = nullptr;
}

void ATUpdateMRUListMenu() {
	const HMENU hmenu = g_hMenuMRU;
	ATUIMenu *const pmenu = g_pMenuMRU;
	const UINT baseId = g_menuMRUBaseId;
	const UINT clearId = g_menuMRUClearId;

	// clear the menu
	if (hmenu) {
		int n = GetMenuItemCount(hmenu);

		for(int i=0; i<n; ++i) {
			if (!DeleteMenu(hmenu, 0, MF_BYPOSITION))
				break;
		}
	}

	if (pmenu)
		pmenu->RemoveAllItems();

	VDRegistryAppKey key("MRU List", false);
	
	VDStringW mrustr;
	VDStringW path;
	VDStringW menustr;
	bool stringsAdded = false;
	if (key.getString("Order", mrustr)) {
		int index = 0;

		for(VDStringW::const_iterator it(mrustr.begin()), itEnd(mrustr.end());
			it != itEnd;
			++it, ++index)
		{
			char valuename[2] = { (char)*it, 0 };

			if (key.getString(valuename, path)) {
				menustr.sprintf(L"&%c %s", index == 9 ? L'0' : L'1' + index, path.c_str());

				if (hmenu)
					VDAppendMenuW32(hmenu, MF_STRING, baseId + index, menustr.c_str());

				if (pmenu) {
					ATUIMenuItem item;
					item.mText = menustr;
					item.mId = baseId + index;

					pmenu->AddItem(item);
				}

				stringsAdded = true;
			}
		}
	}

	if (stringsAdded) {
		if (hmenu) {
			VDAppendMenuSeparatorW32(hmenu);
			VDAppendMenuW32(hmenu, MF_STRING, clearId, L"Clear list");
		}

		if (pmenu) {
			pmenu->AddSeparator();

			ATUIMenuItem item;
			item.mText = L"Clear list";
			item.mId = clearId;

			pmenu->AddItem(item);
		}
	} else {
		if (hmenu) {
			VDAppendMenuW32(hmenu, MF_STRING, baseId, L"Recently used list");
			EnableMenuItem(hmenu, 0, MF_BYPOSITION | MF_GRAYED);
		}

		if (pmenu) {
			ATUIMenuItem item;
			item.mText = L"Recently used list";
			item.mId = baseId;
			item.mbDisabled = true;

			pmenu->AddItem(item);
		}
	}
}
