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

#ifndef f_AT_UIDRAGDROP_H
#define f_AT_UIDRAGDROP_H

#include <vd2/system/vectors.h>

class IATUIDragDropObject;

enum class ATUIDropFilesTarget : uint32 {
	None,
	UseContextMenu,
	BootImage,
	MountImage,
	MountCart,
	MountDisk1,
	MountDisk2,
	MountDisk3,
	MountDisk4,
};

bool ATUICanDropFiles(IATUIDragDropObject *obj);
void ATUIDropFiles(const vdpoint32& pt, ATUIDropFilesTarget target, IATUIDragDropObject *obj);

#endif // f_AT_UIDRAGDROP_H