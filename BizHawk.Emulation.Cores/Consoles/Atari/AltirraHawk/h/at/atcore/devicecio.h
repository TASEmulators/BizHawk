//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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

//=========================================================================
// Device CIO interface
//

#ifndef f_AT_ATCORE_DEVICECIO_H
#define f_AT_ATCORE_DEVICECIO_H

#include <vd2/system/unknown.h>

class IATDeviceCIO;

class IATDeviceCIOManager {
public:
	enum { kTypeID = 'adcm' };

	virtual void AddCIODevice(IATDeviceCIO *dev) = 0;
	virtual void RemoveCIODevice(IATDeviceCIO *dev) = 0;

	// Called when the list of CIO devices supported by a registered device has changed.
	virtual void NotifyCIODevicesChanged(IATDeviceCIO *dev) = 0;

	virtual size_t ReadFilename(uint8 *buf, size_t buflen, uint16 filenameAddr) = 0;
	virtual void ReadMemory(void *buf, uint16 addr, uint16 len) = 0;
	virtual void WriteMemory(uint16 addr, const void *buf, uint16 len) = 0;
	virtual uint8 ReadByte(uint16 addr) = 0;
	virtual void WriteByte(uint16 addr, uint8 value) = 0;

	virtual bool IsBreakActive() const = 0;
};

class IATDeviceCIO {
public:
	enum { kTypeID = 'adci'};

	virtual void InitCIO(IATDeviceCIOManager *mgr) = 0;

	// Get a list of which CIO devices are supported, as the device letters
	// to register in HATABS.
	virtual void GetCIODevices(char *buf, size_t len) const = 0;

	// CIO entry points.
	//
	// Channel ranges 0-7, and the return value is the CIO status code.
	// -1 can also be returned to force an asynchronous wait, in which
	// case the CIO manager will retry the operation after letting some
	// cycles pass.
	//
	// It is guaranteed that only one asynchronous operation at most
	// is in flight, and that OnCIOAbortAsync() will be called before a
	// new one is started.
	//
	virtual sint32 OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) = 0;
	virtual sint32 OnCIOClose(int channel, uint8 deviceNo) = 0;
	virtual sint32 OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) = 0;
	virtual sint32 OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) = 0;
	virtual sint32 OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) = 0;
	virtual sint32 OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) = 0;

	// Called when an asynchronous operation previously triggered by
	// returning -1 status fails to complete.
	//
	// Note that whether this is called before or after ColdReset() or
	// WarmReset() is unspecified.
	//
	virtual void OnCIOAbortAsync() = 0;
};

#endif
