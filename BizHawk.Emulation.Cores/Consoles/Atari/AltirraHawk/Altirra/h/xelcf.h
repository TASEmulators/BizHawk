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

#ifndef f_AT_XELCF_H
#define f_AT_XELCF_H

#include <at/atcore/blockdevice.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparentimpl.h>
#include "ide.h"

class IATDeviceIndicatorManager;
class ATMemoryManager;
class ATMemoryLayer;
class ATIDEEmulator;
class ATSimulator;
class ATFirmwareManager;

class ATXELCFEmulator final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceMemMap
	, public IATDeviceIndicators
	, public IATDeviceButtons
	, public IATDeviceParent
	, public ATDeviceBus
{
	ATXELCFEmulator(const ATXELCFEmulator&) = delete;
	ATXELCFEmulator& operator=(const ATXELCFEmulator&) = delete;
public:
	ATXELCFEmulator(bool v3);
	~ATXELCFEmulator();

	void *AsInterface(uint32 id) override;

	void Init() override;
	void Shutdown() override;
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void ColdReset() override;

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch);

public:		// IATDeviceMemMap
	void InitMemMap(ATMemoryManager *memmap);
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const;

public:		// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:		// IATDeviceBus
	IATDeviceBus *GetDeviceBus(uint32 index) override;

public:		// IATDeviceButtons
	uint32 GetSupportedButtons() const override;
	bool IsButtonDepressed(ATDeviceButton idx) const override;
	void ActivateButton(ATDeviceButton idx, bool state) override;

public:		// IATDeviceParent
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

protected:
	static sint32 OnDebugReadByte(void *thisptr, uint32 addr);
	static sint32 OnReadByte(void *thisptr, uint32 addr);
	static bool OnWriteByte(void *thisptr, uint32 addr, uint8 value);

	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATMemoryLayer *mpMemLayerIDE = nullptr;

	bool mbIsV3 = false;
	bool mbSelectSlave = false;
	bool mbSwapActive = false;
	bool mbSwapDepressed = false;

	vdrefptr<IATBlockDevice> mpBlockDevices[2];

	ATIDEEmulator mIDE[2];
};

#endif
