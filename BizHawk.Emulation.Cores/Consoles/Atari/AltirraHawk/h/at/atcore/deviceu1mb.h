//	Altirra - Atari 800/800XL/5200 emulator
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

#ifndef f_AT_ATCORE_DEVICEU1MB_H
#define f_AT_ATCORE_DEVICEU1MB_H

#include <vd2/system/unknown.h>

enum ATU1MBControl {
	kATU1MBControl_SoundBoardBase,
	kATU1MBControl_VBXEBase,
};

// This is pretty hacky, but it's the interface by which the Ultimate1MB
// emulator can control over devices in the system. This is the emulation
// analog of the Mx/Sx control lines. Note that the mapping from U1MB
// settings to the child devices is controlled on the U1MB side.
//
class IATDeviceU1MBControllable {
public:
	enum { kTypeID = 'adul' };

	virtual void SetU1MBControl(ATU1MBControl control, sint32 value) = 0;
};

#endif
