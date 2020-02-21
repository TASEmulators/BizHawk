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

#ifndef f_AT_SDRIVE_H
#define f_AT_SDRIVE_H

#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atcore/devicesio.h>

class IATBlockDevice;
class IATDeviceSIOManager;

class ATSDriveEmulator final : public ATDevice
	, public IATDeviceIndicators
	, public IATDeviceSIO
{
public:
	ATSDriveEmulator();
	~ATSDriveEmulator();

	void *AsInterface(uint32 iid) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void GetSettings(ATPropertySet& settings) override;
	virtual bool SetSettings(const ATPropertySet& settings) override;
	virtual void Init() override;
	virtual void Shutdown() override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;

public:
	virtual void InitIndicators(IATDeviceIndicatorManager *r) override;

public:
	virtual void InitSIO(IATDeviceSIOManager *mgr) override;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	virtual void OnSerialAbortCommand() override;
	virtual void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	virtual void OnSerialFence(uint32 id) override;
	virtual CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

protected:
	IATDeviceSIOManager *mpSIOMgr;
	IATDeviceIndicatorManager *mpUIRenderer;
	vdrefptr<IATBlockDevice> mpDisk;

	uint32 mSectorNumber;
	uint32 mHighSpeedCPSLo;
	uint32 mHighSpeedCPSHi;
	uint8 mHighSpeedIndex;
	bool mbHighSpeedEnabled;
	bool mbHighSpeedPhase;

	ATDeviceParentSingleChild mDeviceParent;

	uint8 mSectorBuffer[512];
};

#endif
