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

#include <stdafx.h>
#include <at/atcore/propertyset.h>
#include "pbidisk.h"
#include "memorymanager.h"
#include "siomanager.h"
#include "pbidisk.inl"

static_assert(sizeof(g_ATPBIDiskFirmware) == 0x400, "PBIDisk firmware is wrong size");

void ATCreateDevicePBIDiskEmulator(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATPBIDiskEmulator> p(new ATPBIDiskEmulator);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefPBIDisk = { "pbidisk", "pbidisk", L"PBI Disk Patch", ATCreateDevicePBIDiskEmulator };

void *ATPBIDiskEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceMemMap::kTypeID: return static_cast<IATDeviceMemMap *>(this);
		case IATDevicePBIConnection::kTypeID: return static_cast<IATDevicePBIConnection *>(this);
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
	}

	return nullptr;
}

void ATPBIDiskEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefPBIDisk;
}

void ATPBIDiskEmulator::Init() {
}

void ATPBIDiskEmulator::Shutdown() {
	if (mpPBIManager) {
		mpPBIManager->RemoveDevice(this);
		mpPBIManager = nullptr;
	}

	mpSIOManager = nullptr;

	if (mpMemLayerFirmware) {
		mpMemMan->DeleteLayer(mpMemLayerFirmware);
		mpMemLayerFirmware = nullptr;
	}

	if (mpMemLayerControl) {
		mpMemMan->DeleteLayer(mpMemLayerControl);
		mpMemLayerControl = nullptr;
	}

	mpMemMan = nullptr;
}

void ATPBIDiskEmulator::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;

	memcpy(mFirmware, g_ATPBIDiskFirmware, 0x400);
	memset(mFirmware + 0x400, 0xFF, 0x400);

	mpMemLayerFirmware = mpMemMan->CreateLayer(kATMemoryPri_PBI, mFirmware, 0xD8, 0x08, true);
	mpMemMan->SetLayerName(mpMemLayerFirmware, "PBIDisk ROM");
	mpMemMan->SetLayerFastBus(mpMemLayerFirmware, true);

	ATMemoryHandlerTable handlers = {};
	handlers.mpThis = this;
	handlers.mpWriteHandler = [](void *thisptr, uint32 addr, uint8 value) {
		return ((ATPBIDiskEmulator *)thisptr)->OnWriteByte(addr, value);
	};

	mpMemLayerControl = mpMemMan->CreateLayer(kATMemoryPri_PBI + 1, handlers, 0xDC, 0x04);
	mpMemMan->SetLayerName(mpMemLayerControl, "PBIDisk control registers");
	mpMemMan->SetLayerFastBus(mpMemLayerControl, true);
}

bool ATPBIDiskEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	return false;
}

void ATPBIDiskEmulator::InitPBI(IATDevicePBIManager *pbiman) {
	mpPBIManager = pbiman;
	mpPBIManager->AddDevice(this);
}

void ATPBIDiskEmulator::GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const {
	devInfo.mDeviceId = 0x80;
	devInfo.mbHasIrq = false;
}

void ATPBIDiskEmulator::SelectPBIDevice(bool enable) {
	if (mbSelected != enable) {
		mbSelected = enable;

		mpMemMan->EnableLayer(mpMemLayerFirmware, enable);
		mpMemMan->SetLayerModes(mpMemLayerControl, enable ? kATMemoryAccessMode_W : kATMemoryAccessMode_0);
	}
}

bool ATPBIDiskEmulator::IsPBIOverlayActive() const {
	return mbSelected;
}

uint8 ATPBIDiskEmulator::ReadPBIStatus(uint8 busData, bool debugOnly) {
	return busData;
}

void ATPBIDiskEmulator::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOManager = mgr;
}

bool ATPBIDiskEmulator::OnWriteByte(uint32 address, uint8 value) {
	if (address == 0xDCEF) {
		// Time to cheat.
		static_cast<ATSIOManager *>(mpSIOManager)->TryAccelPBIRequest();
	}

	return true;
}
