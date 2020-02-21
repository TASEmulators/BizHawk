//	Altirra - Atari 800/800XL/5200 emulator
//	Browser (B:) device
//	Copyright (C) 2017 Avery Lee
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

#ifndef f_AT_ATDEVICES_CORVUS_H
#define f_AT_ATDEVICES_CORVUS_H

#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicecio.h>

class ATDeviceBrowser final
	: public ATDevice
	, public IATDeviceCIO
{
	ATDeviceBrowser(const ATDeviceBrowser&) = delete;
	ATDeviceBrowser& operator=(const ATDeviceBrowser&) = delete;

public:
	ATDeviceBrowser();
	~ATDeviceBrowser();
	
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Shutdown() override;

public:
	void InitCIO(IATDeviceCIOManager *mgr) override;
	void GetCIODevices(char *buf, size_t len) const override;
	sint32 OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) override;
	sint32 OnCIOClose(int channel, uint8 deviceNo) override;
	sint32 OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) override;
	sint32 OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) override;
	void OnCIOAbortAsync() override;

private:
	void FlushUrl();

	IATDeviceCIOManager *mpCIOMgr = nullptr;

	char mUrl[1024];
	uint32 mUrlLen = 0;
	bool mbUrlValid = false;
	uint32_t mLastDenyRealTick = 0;
	uint32_t mCooldownTimer = 0;
};

#endif
