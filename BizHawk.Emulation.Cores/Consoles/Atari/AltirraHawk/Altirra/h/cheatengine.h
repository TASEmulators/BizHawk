//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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

#ifndef f_AT_CHEATENGINE_H
#define f_AT_CHEATENGINE_H

#include <vd2/system/vdstl.h>

enum ATCheatSnapshotMode {
	kATCheatSnapMode_Replace,
	kATCheatSnapMode_Equal,
	kATCheatSnapMode_NotEqual,
	kATCheatSnapMode_Less,
	kATCheatSnapMode_LessEqual,
	kATCheatSnapMode_Greater,
	kATCheatSnapMode_GreaterEqual,
	kATCheatSnapMode_EqualRef,
	kATCheatSnapModeCount
};

class ATCheatEngine {
	ATCheatEngine(const ATCheatEngine&);
	ATCheatEngine& operator=(const ATCheatEngine&);
public:
	ATCheatEngine();
	~ATCheatEngine();

	void Init(const void *src, uint32 len);

	void Clear();
	void Load(const wchar_t *filename);
	void Save(const wchar_t *filename);

	void Snapshot(ATCheatSnapshotMode mode, uint32 value, bool bit16);

	uint32 GetValidOffsets(uint32 *dst, uint32 maxResults) const;
	uint32 GetOffsetCurrentValue(uint32 offset, bool bit16) const;

	struct Cheat {
		uint32 mAddress;
		uint16 mValue;
		bool mb16Bit;
		bool mbEnabled;
	};

	uint32 GetCheatCount() const;
	const Cheat& GetCheatByIndex(uint32 index) const;
	void AddCheat(uint32 offset, bool bit16);
	void AddCheat(const Cheat& cheat);
	void RemoveCheatByIndex(uint32 index);
	void UpdateCheat(uint32 index, const Cheat& cheat);
	void ApplyCheats();

protected:
	struct CheatPred;

	uint8 *mpMemory;
	uint32	mMemorySize;

	vdfastvector<uint8> mLastData;
	vdfastvector<uint8> mValidFlags;

	typedef vdfastvector<Cheat> Cheats;
	Cheats mCheats;
};

#endif	// f_AT_CHEATENGINE_H
