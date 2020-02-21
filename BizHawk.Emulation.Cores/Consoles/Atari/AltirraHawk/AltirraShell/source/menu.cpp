//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - UI menu parser
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
#include <windows.h>
#include <vd2/system/error.h>
#include <vd2/system/linearalloc.h>
#include <vd2/system/VDString.h>
#include <vd2/system/w32assist.h>
#include <vd2/Dita/accel.h>
#include <at/atui/uimenulist.h>
#include <at/atui/uicommandmanager.h>
#include "menu.h"

#define ID_DYNAMIC_BASE 10000

extern HWND g_hwnd;
HMENU g_hMenu;

HMENU g_hMenuFirmware[2];
ATUIMenu *g_pMenuFirmware[2];

IATUIDynamicMenuProvider *g_pDynamicMenuProvider[2];
int g_dynamicMenuItemBaseCount[2];

VDLinearAllocator g_ATUIMenuBoundCommandsAlloc;
vdfastvector<std::pair<const char *, const wchar_t *> > g_ATUIMenuBoundCommands;
vdrefptr<ATUIMenu> g_pATUIMenu;

ATUIMenu *ATUIGetMenu() {
	return g_pATUIMenu;
}

void ATUILoadMenu(ATUICommandManager& cmdmgr, const void *src, size_t len) {
	g_ATUIMenuBoundCommandsAlloc.Clear();
	g_ATUIMenuBoundCommands.clear();

	HMENU hmenu = CreateMenu();
	HMENU hmenuSpecialMenus[7] = {NULL};
	ATUIMenu *pmenuSpecialMenus[7] = {NULL};
	UINT id = 40000;

	VDStringW menuItemText;

	g_pATUIMenu = new ATUIMenu;

	try {
		VDStringW buf16(VDTextU8ToW((const char *)src, len));

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

					throw MyError("Error parsing menu on line %u: unknown special menu '%ls'", lineno, VDStringW(cmdname).c_str());
				}

				// if it's not a special menu, just add it as a command
				if (specialidx < 0) {
					if (!itemid) {
						size_t cmdlen = cmdend - cmdstart;
						char *cmdstr = (char *)g_ATUIMenuBoundCommandsAlloc.Allocate(cmdlen + 1);

						for(size_t i=0; i<cmdlen; ++i)
							cmdstr[i] = (char)cmdstart[i];

						cmdstr[cmdlen] = 0;

						if (!cmdmgr.GetCommand(cmdstr))
							throw MyError("Error parsing menu on line %u: unknown command '%s'", lineno, cmdstr);

						wchar_t *textstr = (wchar_t *)g_ATUIMenuBoundCommandsAlloc.Allocate((menuItemText.size() + 1) * sizeof(wchar_t));
						memcpy(textstr, menuItemText.c_str(), (menuItemText.size() + 1) * sizeof(wchar_t));

						g_ATUIMenuBoundCommands.push_back(std::make_pair(cmdstr, textstr));

#if 0
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
#endif

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

	HMENU hmenuPrev = g_hMenu;
	g_hMenu = hmenu;

	if (g_hwnd) {
		::SetMenu(g_hwnd, g_hMenu);

		if (hmenuPrev)
			DestroyMenu(hmenuPrev);
	}


	for(int i=0; i<2; ++i) {
		g_hMenuFirmware[i] = hmenuSpecialMenus[i+5];
		g_pMenuFirmware[i] = pmenuSpecialMenus[i+5];

		if (pmenuSpecialMenus[i+5])
			g_dynamicMenuItemBaseCount[i] = pmenuSpecialMenus[i+5]->GetItemCount();
		else
			g_dynamicMenuItemBaseCount[i] = 0;

		ATUIRebuildDynamicMenu(i);
	}
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
	ATUIMenu *menu = g_pMenuFirmware[index];

	if (!menu)
		return;

	menu->RemoveItems(g_dynamicMenuItemBaseCount[index], menu->GetItemCount() - g_dynamicMenuItemBaseCount[index]);

	if (g_pDynamicMenuProvider[index])
		g_pDynamicMenuProvider[index]->RebuildMenu(*menu, ID_DYNAMIC_BASE + 100*index);

	HMENU hmenu = g_hMenuFirmware[index];
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

void ATUIUpdateMenu(ATUICommandManager& cmdmgr) {
	vdfastvector<std::pair<const char *, const wchar_t *> >::const_iterator it = g_ATUIMenuBoundCommands.begin();
	vdfastvector<std::pair<const char *, const wchar_t *> >::const_iterator itEnd = g_ATUIMenuBoundCommands.end();

	uint32 id = 40000;
	for(; it != itEnd; ++it, ++id) {
		const char *cmdname = it->first;

		const ATUICommand *cmd = cmdmgr.GetCommand(cmdname);

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

	for(int i=0; i<2; ++i) {
		if (g_pDynamicMenuProvider[i] && g_pMenuFirmware[i]) {
			ATUIMenu& menu = *g_pMenuFirmware[i];
			const uint32 base = g_dynamicMenuItemBaseCount[i];
			const uint32 n = menu.GetItemCount() - g_dynamicMenuItemBaseCount[i];

			vdfastvector<uint8> oldState(n);

			for(uint32 j=0; j<n; ++j) {
				const ATUIMenuItem *item = menu.GetItemByIndex(base + j);

				oldState[j] = (item->mbChecked ? 1 : 0) + (item->mbRadioChecked ? 2 : 0);
			}

			g_pDynamicMenuProvider[i]->UpdateMenu(menu, base, n);

			if (g_hMenuFirmware[i]) {
				for(uint32 j=0; j<n; ++j) {
					const ATUIMenuItem *item = menu.GetItemByIndex(base + j);

					if (oldState[j] != ((item->mbChecked ? 1 : 0) + (item->mbRadioChecked ? 2 : 0))) {
						if (item->mbRadioChecked)
							VDCheckRadioMenuItemByPositionW32(g_hMenuFirmware[i], base + j, true);
						else if (item->mbChecked)
							VDCheckMenuItemByPositionW32(g_hMenuFirmware[i], base + j, true);
						else
							VDCheckMenuItemByPositionW32(g_hMenuFirmware[i], base + j, false);
					}
				}				
			}
		}
	}
}

bool ATUIHandleMenuCommand(ATUICommandManager& cmdmgr, uint32 id) {
	if (id >= ID_DYNAMIC_BASE && id < ID_DYNAMIC_BASE + 200) {
		int dynOffset = id - ID_DYNAMIC_BASE;
		int menuIndex = dynOffset / 100;
		int itemIndex = dynOffset % 100;

		if (g_pDynamicMenuProvider[menuIndex])
			g_pDynamicMenuProvider[menuIndex]->HandleMenuCommand(itemIndex);
		return true;
	}

	uint32 offset = id - 40000;

	if (offset < (uint32)g_ATUIMenuBoundCommands.size()) {
		cmdmgr.ExecuteCommand(g_ATUIMenuBoundCommands[offset].first);
		return true;
	}

	return false;
}
