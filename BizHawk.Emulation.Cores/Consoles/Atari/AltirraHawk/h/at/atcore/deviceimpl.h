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

#ifndef f_AT_ATCORE_DEVICEIMPL_H
#define f_AT_ATCORE_DEVICEIMPL_H

#include <vd2/system/refcount.h>
#include <at/atcore/device.h>

class ATDevice : public vdrefcounted<IATDevice> {
	ATDevice(const ATDevice&) = delete;
	ATDevice& operator=(const ATDevice&) = delete;
public:
	ATDevice();
	~ATDevice();

	virtual void *AsInterface(uint32 iid) override;

	virtual IATDeviceParent *GetParent() override;
	virtual uint32 GetParentBusIndex() override;
	virtual void SetParent(IATDeviceParent *parent, uint32 busIndex) override;
	virtual void GetSettingsBlurb(VDStringW& buf) override;
	virtual void GetSettings(ATPropertySet& settings) override;
	virtual bool SetSettings(const ATPropertySet& settings) override;
	virtual void Init() override;
	virtual void Shutdown() override;
	virtual uint32 GetComputerPowerOnDelay() const override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;
	virtual void ComputerColdReset() override;
	virtual void PeripheralColdReset() override;

	virtual void SetTraceContext(ATTraceContext *context) override;

protected:
	IATDeviceParent *mpDeviceParent;
	uint32 mDeviceParentBusIndex;
};

#endif
