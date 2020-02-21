//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - joystick port dongle emulation
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_ATDEVICES_DONGLE_H
#define f_AT_ATDEVICES_DONGLE_H

#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/deviceimpl.h>

class ATPropertySet;

class ATDeviceDongle final
	: public ATDevice
	, public IATDevicePortInput
{
	ATDeviceDongle(const ATDeviceDongle&) = delete;
	ATDeviceDongle& operator=(const ATDeviceDongle&) = delete;

public:
	ATDeviceDongle();
	~ATDeviceDongle();
	
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& pset) override;
	bool SetSettings(const ATPropertySet& pset) override;
	void Init() override;
	void Shutdown() override;

public:	// IATDevicePortInput
	void InitPortInput(IATDevicePortManager *portmgr) override;

private:
	void OnPortOutputChanged(uint32 outputState);
	void ReinitPortOutput();
	void UpdatePortOutput();

	IATDevicePortManager *mpPortManager = nullptr;
	int mPortInput = -1;
	int mPortOutput = -1;
	uint32 mLastPortState = 0;
	uint8 mPortShift = 0;

	uint8 mResponseTable[16] = {};
};

#endif
