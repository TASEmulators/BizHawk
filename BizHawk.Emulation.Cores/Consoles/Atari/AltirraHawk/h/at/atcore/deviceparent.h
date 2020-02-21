//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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

#ifndef f_AT_ATCORE_DEVICEPARENT_H
#define f_AT_ATCORE_DEVICEPARENT_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>

class IATDevice;

class IATDeviceBus {
public:
	virtual const wchar_t *GetBusName() const = 0;
	virtual const char *GetBusTag() const = 0;
	virtual const char *GetSupportedType(uint32 index) = 0;
	virtual void GetChildDevices(vdfastvector<IATDevice *>& devs) = 0;
	virtual void GetChildDevicePrefix(uint32 index, VDStringW& s) = 0;
	virtual void AddChildDevice(IATDevice *dev) = 0;
	virtual void RemoveChildDevice(IATDevice *dev) = 0;
};

class IATDeviceParent : public IVDUnknown {
public:
	enum { kTypeID = 'adpt' };

	virtual IATDeviceBus *GetDeviceBus(uint32 index) = 0;
};

#endif
