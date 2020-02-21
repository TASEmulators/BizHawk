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
#include <vd2/system/hash.h>
#include <vd2/Dita/accel.h>
#include <at/atui/uicommandmanager.h>

ATUICommandManager::ATUICommandManager() {
	memset(mpHashTable, 0, sizeof mpHashTable);
}

ATUICommandManager::~ATUICommandManager() {
}

void ATUICommandManager::RegisterCommand(const ATUICommand *cmd) {
	const uint32 hash = VDHashString32(cmd->mpName);
	const uint32 htidx = hash % kHashTableSize;

	Node *node = mAllocator.Allocate<Node>();
	node->mpNext = mpHashTable[htidx];
	node->mHash = hash;
	node->mpCmd = cmd;

	mpHashTable[htidx] = node;
}

void ATUICommandManager::RegisterCommands(const ATUICommand *cmd, size_t n) {
	while(n--)
		RegisterCommand(cmd++);
}

const ATUICommand *ATUICommandManager::GetCommand(const char *str) const {
	const uint32 hash = VDHashString32(str);
	const uint32 htidx = hash % kHashTableSize;

	for(const Node *node = mpHashTable[htidx]; node; node = node->mpNext) {
		const ATUICommand *cmd = node->mpCmd;

		if (!strcmp(cmd->mpName, str)) {
			return cmd;
		}
	}

	return NULL;
}

bool ATUICommandManager::ExecuteCommand(const char *str) {
	const ATUICommand *cmd = GetCommand(str);

	if (!cmd)
		return false;

	if (cmd->mpTestFn && !cmd->mpTestFn())
		return false;

	cmd->mpExecuteFn();
	return true;
}

void ATUICommandManager::ListCommands(vdfastvector<VDAccelToCommandEntry>& commands) const {
	for(uint32 i=0; i<kHashTableSize; ++i) {
		for(const Node *node = mpHashTable[i]; node; node = node->mpNext) {
			VDAccelToCommandEntry& ace = commands.push_back();

			ace.mId = 0;
			ace.mpName = node->mpCmd->mpName;
		}
	}
}
