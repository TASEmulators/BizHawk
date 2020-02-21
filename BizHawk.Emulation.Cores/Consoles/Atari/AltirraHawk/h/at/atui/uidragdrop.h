//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_ATUI_UIDRAGDROP_H
#define f_AT_ATUI_UIDRAGDROP_H

#include <vd2/system/unknown.h>

enum class ATUIDragModifiers : uint32 {
	Control	= 0x01,
	Shift	= 0x02,
	Alt		= 0x04,
	LButton	= 0x08,
	MButton	= 0x10,
	RButton	= 0x20
};

inline ATUIDragModifiers operator|(ATUIDragModifiers x, ATUIDragModifiers y) {
	return ATUIDragModifiers((uint32)x | (uint32)y);
}

inline ATUIDragModifiers& operator|=(ATUIDragModifiers& x, ATUIDragModifiers y) {
	x = ATUIDragModifiers((uint32)x | (uint32)y);
	return x;
}

enum class ATUIDragEffect : uint32 {
	None,
	Move,
	Copy,
	Link
};

enum class ATUIDropIconType : uint32 {
	Default,
	NotPossible,
	None,
	Move,
	Copy,
	Link,
	Label,
	Warning
};

class IATUIDragDropObject : public IVDRefUnknown {
public:
	virtual void *GetNativeObject() const = 0;

	virtual void ClearDropDescription() = 0;
	virtual void SetDropDescription(ATUIDropIconType type, const wchar_t *templateStr, const wchar_t *insertStr) = 0;
};

#endif // f_AT_ATUI_UIDRAGDROP_H
