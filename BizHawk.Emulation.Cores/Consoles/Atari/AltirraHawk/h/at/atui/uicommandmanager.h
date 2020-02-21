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

#ifndef f_AT_UICOMMANDMANAGER_H
#define f_AT_UICOMMANDMANAGER_H

#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_hashmap.h>
#include <vd2/system/linearalloc.h>

struct VDAccelToCommandEntry;
class VDStringW;

enum ATUICmdState {
	kATUICmdState_None,
	kATUICmdState_Checked,
	kATUICmdState_RadioChecked
};

typedef void (*ATUICmdExecuteFn)();
typedef bool (*ATUICmdTestFn)();
typedef ATUICmdState (*ATUICmdStateFn)();
typedef void (*ATUICmdFormatFn)(VDStringW&);

struct ATUICommand {
	const char *mpName;
	ATUICmdExecuteFn mpExecuteFn;
	ATUICmdTestFn mpTestFn;
	ATUICmdStateFn mpStateFn;
	ATUICmdFormatFn mpFormatFn;
};

class ATUICommandManager {
	ATUICommandManager(const ATUICommandManager&);
	ATUICommandManager& operator=(const ATUICommandManager&);
public:
	ATUICommandManager();
	~ATUICommandManager();

	void RegisterCommand(const ATUICommand *cmd);
	void RegisterCommands(const ATUICommand *cmd, size_t n);

	const ATUICommand *GetCommand(const char *str) const;
	bool ExecuteCommand(const char *str);

	void ListCommands(vdfastvector<VDAccelToCommandEntry>& commands) const;

protected:
	struct Node {
		Node *mpNext;
		uint32 mHash;
		const ATUICommand *mpCmd;
	};

	VDLinearAllocator mAllocator;

	enum { kHashTableSize = 257 };
	Node *mpHashTable[kHashTableSize];
};

#endif
