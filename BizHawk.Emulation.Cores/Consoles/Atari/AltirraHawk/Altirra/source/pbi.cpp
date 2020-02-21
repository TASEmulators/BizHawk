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

#include <stdafx.h>
#include <at/atcore/devicepbi.h>
#include <vd2/system/bitmath.h>
#include "memorymanager.h"
#include "pbi.h"

ATPBIManager::ATPBIManager() {
}

ATPBIManager::~ATPBIManager() {
	Shutdown();
}

void ATPBIManager::Init(ATMemoryManager *memman) {
	mpMemMan = memman;

	ATMemoryHandlerTable handlers = {};
	handlers.mbPassAnticReads = true;
	handlers.mbPassReads = true;
	handlers.mbPassWrites = true;
	handlers.mpThis = this;
	handlers.mpWriteHandler = OnControlWrite;
	mpMemLayerPBISel = mpMemMan->CreateLayer(kATMemoryPri_PBISelect, handlers, 0xD1, 0x01);
	mpMemMan->SetLayerName(mpMemLayerPBISel, "PBI shared select register");
	mpMemMan->EnableLayer(mpMemLayerPBISel, kATMemoryAccessMode_CPUWrite, true);

	ATMemoryHandlerTable handlers2 = {};
	handlers2.mbPassAnticReads = true;
	handlers2.mbPassReads = true;
	handlers2.mbPassWrites = true;
	handlers2.mpThis = this;
	handlers2.mpDebugReadHandler = OnControlDebugRead;
	handlers2.mpReadHandler = OnControlRead;
	mpMemLayerPBIIRQ = mpMemMan->CreateLayer(kATMemoryPri_PBIIRQ, handlers2, 0xD1, 0x01);
	mpMemMan->SetLayerName(mpMemLayerPBIIRQ, "PBI shared IRQ register");
	mpMemMan->SetLayerModes(mpMemLayerPBIIRQ, kATMemoryAccessMode_0);
}

void ATPBIManager::Shutdown() {
	if (mpSelDevice) {
		mpSelDevice->SelectPBIDevice(false);
		mpSelDevice = nullptr;
	}

	if (mpMemLayerPBISel) {
		mpMemMan->EnableLayer(mpMemLayerPBISel, false);
		mpMemMan->DeleteLayer(mpMemLayerPBISel);
		mpMemLayerPBISel = nullptr;
	}

	if (mpMemLayerPBIIRQ) {
		mpMemMan->EnableLayer(mpMemLayerPBIIRQ, false);
		mpMemMan->DeleteLayer(mpMemLayerPBIIRQ);
		mpMemLayerPBIIRQ = nullptr;
	}

	mpMemMan = nullptr;
}

bool ATPBIManager::IsROMOverlayActive() const {
	return mpSelDevice && mpSelDevice->IsPBIOverlayActive();
}

void ATPBIManager::AddDevice(IATPBIDevice *dev) {
	mDevices.push_back(dev);

	RebuildSelList();
}

void ATPBIManager::RemoveDevice(IATPBIDevice *dev) {
	if (mpSelDevice == dev) {
		mpSelDevice->SelectPBIDevice(false);
		mpSelDevice = NULL;
	}

	Devices::iterator it(std::find(mDevices.begin(), mDevices.end(), dev));

	if (it != mDevices.end())
		mDevices.erase(it);

	RebuildSelList();
}

void ATPBIManager::DeselectSelf(IATPBIDevice *dev) {
	if (mpSelDevice == dev)
		Select(0);
}

void ATPBIManager::ColdReset() {
	Select(0);
}

void ATPBIManager::WarmReset() {
	Select(0);
}

void ATPBIManager::Select(uint8 selval) {
	if (mSelRegister == selval)
		return;

	mSelRegister = selval;

	IATPBIDevice *sel = nullptr;

	if (selval)
		sel = mpSelectList[VDFindHighestSetBit(selval)];

	if (mpSelDevice != sel) {
		if (mpSelDevice)
			mpSelDevice->SelectPBIDevice(false);

		mpSelDevice = sel;

		if (mpSelDevice)
			mpSelDevice->SelectPBIDevice(true);
	}
}

uint8 ATPBIManager::ReadStatus() {
	uint8 busData = mpMemMan->ReadFloatingDataBus();

	for(auto *p : mDevices)
		busData = p->ReadPBIStatus(busData, false);

	return busData;
}

uint8 ATPBIManager::DebugReadStatus() const {
	uint8 busData = mpMemMan->ReadFloatingDataBus();

	for(auto *p : mDevices)
		busData = p->ReadPBIStatus(busData, true);

	return busData;
}

void ATPBIManager::RebuildSelList() {
	std::fill(std::begin(mpSelectList), std::end(mpSelectList), nullptr);

	IATPBIDevice *sel = nullptr;
	bool haveIrqDevices = false;

	for(IATPBIDevice *dev : mDevices) {
		ATPBIDeviceInfo devInfo {};
		dev->GetPBIDeviceInfo(devInfo);

		if (devInfo.mDeviceId) {
			mpSelectList[VDFindHighestSetBit(devInfo.mDeviceId)] = dev;

			if (mSelRegister & devInfo.mDeviceId)
				sel = dev;
		}

		haveIrqDevices |= devInfo.mbHasIrq;
	}

	if (mpSelDevice != sel) {
		if (mpSelDevice)
			mpSelDevice->SelectPBIDevice(false);

		mpSelDevice = sel;

		if (mpSelDevice)
			mpSelDevice->SelectPBIDevice(true);
	}

	const bool haveDevices = !mDevices.empty();

	mpMemMan->SetLayerModes(mpMemLayerPBISel, haveDevices ? kATMemoryAccessMode_W : kATMemoryAccessMode_0);
	mpMemMan->SetLayerModes(mpMemLayerPBIIRQ, haveIrqDevices ? kATMemoryAccessMode_AR : kATMemoryAccessMode_0);
}

sint32 ATPBIManager::OnControlDebugRead(void *thisptr, uint32 addr) {
	if (addr == 0xD1FF)
		return ((ATPBIManager *)thisptr)->DebugReadStatus();

	return -1;
}

sint32 ATPBIManager::OnControlRead(void *thisptr, uint32 addr) {
	if (addr == 0xD1FF)
		return ((ATPBIManager *)thisptr)->ReadStatus();

	return -1;
}

bool ATPBIManager::OnControlWrite(void *thisptr, uint32 addr, uint8 value) {
	if (addr == 0xD1FF) {
		((ATPBIManager *)thisptr)->Select(value);
		return true;
	}

	return false;
}
