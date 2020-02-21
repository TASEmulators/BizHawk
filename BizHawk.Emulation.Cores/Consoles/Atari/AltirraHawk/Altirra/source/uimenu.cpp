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
#include <windows.h>
#include <vd2/system/error.h>
#include <vd2/system/linearalloc.h>
#include <vd2/system/VDString.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/accel.h>
#include <at/atui/uicommandmanager.h>
#include "oshelper.h"
#include "uikeyboard.h"
#include "uimenu.h"
#include <at/atui/uimenulist.h>
#include "uimrulist.h"
#include "uiportmenus.h"
#include "resource.h"

extern HWND g_hwnd;
extern HMENU g_hMenu;
extern ATUICommandManager g_ATUICommandMgr;

HMENU g_hMenuDynamic[kATUIDynamicMenuCount];
ATUIMenu *g_pMenuDynamic[kATUIDynamicMenuCount];

IATUIDynamicMenuProvider *g_pDynamicMenuProvider[kATUIDynamicMenuCount];
int g_dynamicMenuItemBaseCount[kATUIDynamicMenuCount];

VDLinearAllocator g_ATUIMenuBoundCommandsAlloc;
vdfastvector<std::pair<const char *, const wchar_t *> > g_ATUIMenuBoundCommands;
vdrefptr<ATUIMenu> g_pATUIMenu;

ATUIMenu *ATUIGetMenu() {
	return g_pATUIMenu;
}

void ATUILoadMenu() {
	g_ATUIMenuBoundCommandsAlloc.Clear();
	g_ATUIMenuBoundCommands.clear();

	HMENU hmenu = CreateMenu();
	HMENU hmenuSpecialMenus[5 + kATUIDynamicMenuCount] = {NULL};
	ATUIMenu *pmenuSpecialMenus[5 + kATUIDynamicMenuCount] = {NULL};
	UINT id = 40000;

	VDStringW menuItemText;

	g_pATUIMenu = new ATUIMenu;

	try {
		vdfastvector<uint8> buf;

		ATLoadMiscResource(IDR_MENU_DEFAULT, buf);

		VDStringW buf16(VDTextU8ToW((const char *)buf.data(), (int)buf.size()));

		const wchar_t *s = buf16.data();
		const wchar_t *end = s + buf16.size();

		// skip BOM
		if (*s == 0xFEFF)
			++s;

		// parse out lines
		HMENU hmenuLevels[16] = {hmenu};
		ATUIMenu *pmenuLevels[16] = {g_pATUIMenu};

		int lineno = 0;
		int lastindent = -1;

		while(s != end) {
			const wchar_t *linestart = s;
			const wchar_t *lineend = NULL;

			++lineno;

			while(s != end) {
				const wchar_t c = *s++;

				if (c == 0x0D || c == 0x0A) {
					lineend = s - 1;

					if (s != end && *s == (c ^ (0x0D ^ 0x0A)))
						++s;
					break;
				}
			}

			if (!lineend)
				lineend = s;

			// count tabs
			int indent = 0;

			while(linestart != lineend && *linestart == '\t') {
				++linestart;
				++indent;
			}

			// skip spaces
			while(linestart != lineend && *linestart == ' ')
				++linestart;

			// check for an empty line
			if (linestart == lineend)
				continue;

			// check if not inited yet
			if (!hmenuLevels[indent])
				throw MyError("Error parsing menu on line %u: indent level %u not yet opened.", lineno, indent);

			if (indent > 15)
				throw MyError("Error parsing menu on line %u: indentation level limit exceeded.", lineno);

			// clear out old indentation levels
			for(int i=lastindent-1; i>indent; --i)
				hmenuLevels[i] = NULL;

			lastindent = indent;

			// check for a separator
			if (*linestart == '-') {
				VDAppendMenuSeparatorW32(hmenuLevels[indent]);
				pmenuLevels[indent]->AddSeparator();
				continue;
			}

			// parse out menu item text
			menuItemText.clear();

			while(linestart != lineend) {
				const wchar_t c = *linestart++;

				if (c == '{') {
					if (linestart == lineend || *linestart != '{') {
						--linestart;
						break;
					}
				}

				menuItemText.push_back(c);
			}

			// trim off trailing tabs
			while(!menuItemText.empty() && menuItemText.back() == '\t')
				menuItemText.pop_back();

			// check for command
			int specialidx = -1;

			if (linestart != lineend && *linestart == '{') {
				++linestart;

				while(linestart != lineend && *linestart == ' ')
					++linestart;

				const wchar_t *cmdstart = linestart;

				while(linestart != lineend && *linestart != '}')
					++linestart;

				const wchar_t *cmdend = linestart;

				while(cmdend != cmdstart && cmdend[-1] == ' ')
					--cmdend;

				// check for a special menu
				uint32 itemid = 0;

				if (cmdstart != cmdend && cmdstart[0] == '$') {
					VDStringSpanW cmdname(cmdstart, cmdend);

					if (cmdname == L"$mru")
						specialidx = 0;
					else if (cmdname == L"$port1")
						specialidx = 1;
					else if (cmdname == L"$port2")
						specialidx = 2;
					else if (cmdname == L"$port3")
						specialidx = 3;
					else if (cmdname == L"$port4")
						specialidx = 4;
					else if (cmdname == L"$firmware_os")
						specialidx = 5;
					else if (cmdname == L"$firmware_basic")
						specialidx = 6;
					else if (cmdname == L"$profiles")
						specialidx = 7;
					else if (cmdname == L"$port1none")
						itemid = ID_INPUT_PORT1_NONE;
					else if (cmdname == L"$port2none")
						itemid = ID_INPUT_PORT2_NONE;
					else if (cmdname == L"$port3none")
						itemid = ID_INPUT_PORT3_NONE;
					else if (cmdname == L"$port4none")
						itemid = ID_INPUT_PORT4_NONE;
					else {
						throw MyError("Error parsing menu on line %u: unknown special menu '%ls'", lineno, VDStringW(cmdname).c_str());
					}
				}

				// if it's not a special menu, just add it as a command
				if (specialidx < 0) {
					if (!itemid) {
						size_t cmdlen = cmdend - cmdstart;
						char *cmdstr = (char *)g_ATUIMenuBoundCommandsAlloc.Allocate(cmdlen + 1);

						for(size_t i=0; i<cmdlen; ++i)
							cmdstr[i] = (char)cmdstart[i];

						cmdstr[cmdlen] = 0;

						if (!g_ATUICommandMgr.GetCommand(cmdstr))
							throw MyError("Error parsing menu on line %u: unknown command '%s'", lineno, cmdstr);

						wchar_t *textstr = (wchar_t *)g_ATUIMenuBoundCommandsAlloc.Allocate((menuItemText.size() + 1) * sizeof(wchar_t));
						memcpy(textstr, menuItemText.c_str(), (menuItemText.size() + 1) * sizeof(wchar_t));

						g_ATUIMenuBoundCommands.push_back(std::make_pair(cmdstr, textstr));

						// see if we can match this command to a keyboard shortcut entry
						const VDAccelTableEntry *pAccel = ATUIGetAccelByCommand(kATUIAccelContext_Display, cmdstr);

						if (pAccel) {
							VDStringW accelText;
							VDUIGetAcceleratorString(pAccel->mAccel, accelText);

							if (!accelText.empty()) {
								menuItemText += '\t';
								menuItemText += accelText;
							}
						}

						itemid = id++;
					}

					ATUIMenuItem menuItem;

					menuItem.mText = menuItemText;
					menuItem.mId = itemid;

					pmenuLevels[indent]->AddItem(menuItem);

					VDAppendMenuW32(hmenuLevels[indent], MF_STRING | MF_ENABLED, (UINT)itemid, menuItemText.c_str());
					continue;
				}
			}

			// must be popup
			HMENU hmenuPopup = CreatePopupMenu();
			vdrefptr<ATUIMenu> pmenuPopup(new ATUIMenu);

			if (!VDAppendPopupMenuW32(hmenuLevels[indent], MF_STRING | MF_ENABLED, hmenuPopup, menuItemText.c_str())) {
				DestroyMenu(hmenuPopup);
				throw MyError("Error parsing menu on line %u: unable to create popup menu.", lineno);
			}

			ATUIMenuItem popupItem;
			popupItem.mText = menuItemText;
			popupItem.mpSubMenu = pmenuPopup;
			pmenuLevels[indent]->AddItem(popupItem);

			if (specialidx >= 0) {
				hmenuSpecialMenus[specialidx] = hmenuPopup;
				pmenuSpecialMenus[specialidx] = pmenuPopup;
			}

			hmenuLevels[indent + 1] = hmenuPopup;
			pmenuLevels[indent + 1] = pmenuPopup;
		}
	} catch(...) {
		DestroyMenu(hmenu);
		throw;
	}

	ATUnregisterMRUListMenu();

	HMENU hmenuPrev = g_hMenu;
	g_hMenu = hmenu;

	if (g_hwnd) {
		::SetMenu(g_hwnd, g_hMenu);

		if (hmenuPrev)
			DestroyMenu(hmenuPrev);
	}

	HMENU hMenuMRU = hmenuSpecialMenus[0];
	ATUIMenu *pMenuMRU = pmenuSpecialMenus[0];

	ATRegisterMRUListMenu(hMenuMRU, pMenuMRU, ID_FILE_MRU_BASE, ID_FILE_MRU_BASE + 99);

	for(int i=0; i<kATUIDynamicMenuCount; ++i) {
		g_hMenuDynamic[i] = hmenuSpecialMenus[i+5];
		g_pMenuDynamic[i] = pmenuSpecialMenus[i+5];
		g_dynamicMenuItemBaseCount[i] = pmenuSpecialMenus[i+5] ? pmenuSpecialMenus[i+5]->GetItemCount() : 0;

		ATUIRebuildDynamicMenu(i);
	}

	for(int i=0; i<4; ++i)
		ATSetPortMenu(i, hmenuSpecialMenus[i + 1], pmenuSpecialMenus[i + 1]);
}

void ATUISetMenuEnabled(bool enabled) {
	if (g_hMenu) {
		UINT n = GetMenuItemCount(g_hMenu);

		for(UINT i=0; i<n; ++i)
			EnableMenuItem(g_hMenu, i, MF_BYPOSITION | (enabled ? MF_ENABLED : MF_GRAYED));

		DrawMenuBar(g_hwnd);
	}
}

void ATUISetDynamicMenuProvider(int index, IATUIDynamicMenuProvider *provider) {
	g_pDynamicMenuProvider[index] = provider;
}

void ATUIRebuildDynamicMenu(int index) {
	ATUIMenu *menu = g_pMenuDynamic[index];

	if (!menu)
		return;

	menu->RemoveItems(g_dynamicMenuItemBaseCount[index], menu->GetItemCount() - g_dynamicMenuItemBaseCount[index]);

	if (g_pDynamicMenuProvider[index])
		g_pDynamicMenuProvider[index]->RebuildMenu(*menu, ID_DYNAMIC_BASE + 100*index);

	HMENU hmenu = g_hMenuDynamic[index];
	if (hmenu) {
		int count = ::GetMenuItemCount(hmenu);
		for(int i=count-1; i>=g_dynamicMenuItemBaseCount[index]; --i)
			::DeleteMenu(hmenu, i, MF_BYPOSITION);

		uint32 n = menu->GetItemCount() - g_dynamicMenuItemBaseCount[index];
		for(uint32 i=0; i<n; ++i) {
			ATUIMenuItem *item = menu->GetItemByIndex(i + g_dynamicMenuItemBaseCount[index]);

			if (item->mbSeparator)
				VDAppendMenuSeparatorW32(hmenu);
			else
				VDAppendMenuW32(hmenu, MF_STRING | (item->mbDisabled ? MF_DISABLED : MF_ENABLED), item->mId, item->mText.c_str());
		}
	}
}

void ATUIUpdateMenu() {
	vdfastvector<std::pair<const char *, const wchar_t *> >::const_iterator it = g_ATUIMenuBoundCommands.begin();
	vdfastvector<std::pair<const char *, const wchar_t *> >::const_iterator itEnd = g_ATUIMenuBoundCommands.end();

	uint32 id = 40000;
	for(; it != itEnd; ++it, ++id) {
		const char *cmdname = it->first;

		const ATUICommand *cmd = g_ATUICommandMgr.GetCommand(cmdname);

		if (!cmd)
			continue;

		ATUIMenuItem *item = g_pATUIMenu->GetItemById(id, true);

		if (cmd->mpTestFn) {
			bool enabled = cmd->mpTestFn();
			VDEnableMenuItemByCommandW32(g_hMenu, id, enabled);

			item->mbDisabled = !enabled;
		}

		item->mbChecked = false;
		item->mbRadioChecked = false;

		if (cmd->mpStateFn) {
			switch(cmd->mpStateFn()) {
				case kATUICmdState_None:
					VDCheckMenuItemByCommandW32(g_hMenu, id, false);
					break;

				case kATUICmdState_Checked:
					VDCheckMenuItemByCommandW32(g_hMenu, id, true);
					item->mbChecked = true;
					item->mbRadioChecked = false;
					break;

				case kATUICmdState_RadioChecked:
					VDCheckRadioMenuItemByCommandW32(g_hMenu, id, true);
					item->mbChecked = false;
					item->mbRadioChecked = true;
					break;
			}
		}
		
		if (cmd->mpFormatFn) {
			const wchar_t *s = it->second;
			const wchar_t *formatpt = wcschr(s, '%');

			if (formatpt) {
				item->mText.assign(s, formatpt);
				cmd->mpFormatFn(item->mText);
				item->mText.append(formatpt + 1);

				VDSetMenuItemTextByCommandW32(g_hMenu, id, item->mText.c_str());
			}
		}
	}

	for(int i=0; i<kATUIDynamicMenuCount; ++i) {
		if (g_pDynamicMenuProvider[i] && g_pMenuDynamic[i]) {
			if (g_pDynamicMenuProvider[i]->IsRebuildNeeded())
				ATUIRebuildDynamicMenu(i);

			ATUIMenu& menu = *g_pMenuDynamic[i];
			const uint32 base = g_dynamicMenuItemBaseCount[i];
			const uint32 n = menu.GetItemCount() - g_dynamicMenuItemBaseCount[i];

			vdfastvector<uint8> oldState(n);

			for(uint32 j=0; j<n; ++j) {
				const ATUIMenuItem *item = menu.GetItemByIndex(base + j);

				oldState[j] = (item->mbChecked ? 1 : 0) + (item->mbRadioChecked ? 2 : 0);
			}

			g_pDynamicMenuProvider[i]->UpdateMenu(menu, base, n);

			if (g_hMenuDynamic[i]) {
				for(uint32 j=0; j<n; ++j) {
					const ATUIMenuItem *item = menu.GetItemByIndex(base + j);

					if (oldState[j] != ((item->mbChecked ? 1 : 0) + (item->mbRadioChecked ? 2 : 0))) {
						if (item->mbRadioChecked)
							VDCheckRadioMenuItemByPositionW32(g_hMenuDynamic[i], base + j, true);
						else if (item->mbChecked)
							VDCheckMenuItemByPositionW32(g_hMenuDynamic[i], base + j, true);
						else
							VDCheckMenuItemByPositionW32(g_hMenuDynamic[i], base + j, false);
					}
				}				
			}
		}
	}
}

bool ATUIHandleMenuCommand(uint32 id) {
	if (id >= ID_DYNAMIC_BASE && id < ID_DYNAMIC_BASE + 300) {
		int dynOffset = id - ID_DYNAMIC_BASE;
		int menuIndex = dynOffset / 100;
		int itemIndex = dynOffset % 100;

		if (g_pDynamicMenuProvider[menuIndex])
			g_pDynamicMenuProvider[menuIndex]->HandleMenuCommand(itemIndex);
		return true;
	}

	uint32 offset = id - 40000;

	if (offset < (uint32)g_ATUIMenuBoundCommands.size()) {
		try {
			g_ATUICommandMgr.ExecuteCommand(g_ATUIMenuBoundCommands[offset].first);
		} catch(const MyError& e) {
			e.post(g_hwnd, "Altirra Error");
		}
		return true;
	}

	return false;
}
