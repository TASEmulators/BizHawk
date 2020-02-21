//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - UI menu support
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

#ifndef f_ATS_MENU_H
#define f_ATS_MENU_H

#pragma once

class ATUIMenu;
class ATUICommandManager;

class IATUIDynamicMenuProvider {
public:
	virtual void RebuildMenu(ATUIMenu& menu, uint32 idbase) = 0;
	virtual void UpdateMenu(ATUIMenu& menu, uint32 firstIndex, uint32 n) = 0;
	virtual void HandleMenuCommand(uint32 index) = 0;
};

ATUIMenu *ATUIGetMenu();
void ATUILoadMenu(ATUICommandManager& cmdmgr, const void *src, size_t len);
void ATUISetMenuEnabled(bool enabled);
void ATUISetDynamicMenuProvider(int index, IATUIDynamicMenuProvider *provider);
void ATUIRebuildDynamicMenu(int index);
void ATUIUpdateMenu(ATUICommandManager& cmdmgr);
bool ATUIHandleMenuCommand(ATUICommandManager& cmdmgr, uint32 id);

#endif
