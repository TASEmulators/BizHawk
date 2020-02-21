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

#ifndef AT_HOSTDEVICE_H
#define AT_HOSTDEVICE_H

#include <vd2/system/unknown.h>

class ATCPUEmulator;
class ATCPUEmulatorMemory;
class IATUIRenderer;

class IATHostDeviceEmulator : public IVDUnknown {
public:
	enum { kTypeID = 'ahdv' };

	virtual bool IsReadOnly() const = 0;
	virtual void SetReadOnly(bool enabled) = 0;

	virtual bool IsLongNameEncodingEnabled() const = 0;
	virtual void SetLongNameEncodingEnabled(bool enabled) = 0;

	virtual bool IsLowercaseNamingEnabled() const = 0;
	virtual void SetLowercaseNamingEnabled(bool enabled) = 0;

	virtual const wchar_t *GetBasePath(int index) const = 0;
	virtual void SetBasePath(int index, const wchar_t *s) = 0;
};

IATHostDeviceEmulator *ATCreateHostDeviceEmulator();

#endif
