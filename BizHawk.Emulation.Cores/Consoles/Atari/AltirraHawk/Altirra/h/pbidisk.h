//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_AT_PBIDISK_H
#define f_AT_PBIDISK_H

#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicepbi.h>
#include <at/atcore/devicesioimpl.h>

class ATMemoryLayer;

class ATPBIDiskEmulator final : public ATDevice
	, public IATDeviceMemMap
	, public IATDevicePBIConnection
	, public IATPBIDevice
	, public ATDeviceSIO
{
public:
	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Init() override;
	void Shutdown() override;

public:
	void InitMemMap(ATMemoryManager *memmap) override;
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:
	void InitPBI(IATDevicePBIManager *pbiman) override;

public:
	void GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const override;
	void SelectPBIDevice(bool enable) override;
	bool IsPBIOverlayActive() const override;
	uint8 ReadPBIStatus(uint8 busData, bool debugOnly) override;

public:
	void InitSIO(IATDeviceSIOManager *mgr) override;

private:
	bool OnWriteByte(uint32 address, uint8 value);

	ATMemoryManager *mpMemMan = nullptr;
	ATMemoryLayer *mpMemLayerFirmware = nullptr;
	ATMemoryLayer *mpMemLayerControl = nullptr;

	IATDevicePBIManager *mpPBIManager = nullptr;
	bool mbSelected = false;

	IATDeviceSIOManager *mpSIOManager = nullptr;

	VDALIGN(2) uint8 mFirmware[0x0800] = {};
};

#endif
