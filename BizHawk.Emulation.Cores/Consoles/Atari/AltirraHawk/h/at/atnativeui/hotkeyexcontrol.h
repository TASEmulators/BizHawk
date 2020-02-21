//	Altirra - Atari 800/800XL/5200 emulator
//	UI library
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

#ifndef f_AT_ATUI_HOTKEYCONTROL_H
#define f_AT_ATUI_HOTKEYCONTROL_H

#include <vd2/system/event.h>
#include <vd2/system/unknown.h>

struct VDUIAccelerator;
class IVDUIHotKeyExControl;

#define VDUIHOTKEYEXCLASS _T("VDHotKeyEx")

bool VDUIRegisterHotKeyExControl();
IVDUIHotKeyExControl *VDGetUIHotKeyExControl(VDGUIHandle h);

class IVDUIHotKeyExControl : public IVDRefUnknown {
public:
	enum { kTypeID = 'uihk' };

	virtual void SetCookedMode(bool enable) = 0;

	virtual void GetAccelerator(VDUIAccelerator& accel) = 0;
	virtual void SetAccelerator(const VDUIAccelerator& accel) = 0;

	virtual VDEvent<IVDUIHotKeyExControl, VDUIAccelerator>& OnChange() = 0;
};

#endif
