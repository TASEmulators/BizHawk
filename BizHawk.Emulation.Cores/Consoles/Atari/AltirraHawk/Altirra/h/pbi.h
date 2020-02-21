//	Altirra - Atari 800/800XL/5200 emulator
//	Parallel Bus Interface device manager
//	Copyright (C) 2008-2012 Avery Lee
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

#ifndef f_AT_PBI_H
#define f_AT_PBI_H

#include <vd2/system/vdstl.h>
#include <at/atcore/devicepbi.h>

class ATMemoryManager;
class ATMemoryLayer;
struct ATPBIDeviceInfo;

class ATPBIManager final : public IATDevicePBIManager {
public:
	ATPBIManager();
	~ATPBIManager();

	void Init(ATMemoryManager *memman);
	void Shutdown();

	uint8 GetSelectRegister() const { return mSelRegister; }
	bool IsROMOverlayActive() const;

	void AddDevice(IATPBIDevice *dev) override;
	void RemoveDevice(IATPBIDevice *dev) override;
	void DeselectSelf(IATPBIDevice *dev) override;

	void ColdReset();
	void WarmReset();

	void Select(uint8 sel);

	uint8 ReadStatus();
	uint8 DebugReadStatus() const;

protected:
	void RebuildSelList();

	static sint32 OnControlDebugRead(void *thisptr, uint32 addr);
	static sint32 OnControlRead(void *thisptr, uint32 addr);
	static bool OnControlWrite(void *thisptr, uint32 addr, uint8 value);

	ATMemoryManager	*mpMemMan = nullptr;
	ATMemoryLayer *mpMemLayerPBISel = nullptr;
	ATMemoryLayer *mpMemLayerPBIIRQ = nullptr;

	uint8	mSelRegister = 0;
	IATPBIDevice *mpSelDevice = nullptr;
	IATPBIDevice *mpSelectList[8] = {};

	typedef vdfastvector<IATPBIDevice *> Devices;
	Devices mDevices;
};

#endif
