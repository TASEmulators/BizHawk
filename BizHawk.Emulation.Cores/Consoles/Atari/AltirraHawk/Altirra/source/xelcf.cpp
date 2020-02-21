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

#include <stdafx.h>
#include <vd2/system/file.h>
#include <vd2/system/hash.h>
#include <vd2/system/int128.h>
#include <vd2/system/registry.h>
#include "xelcf.h"
#include "memorymanager.h"
#include "ide.h"
#include "uirender.h"

template<bool V3>
void ATCreateDeviceXELCF(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATXELCFEmulator> p(new ATXELCFEmulator(V3));

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefXELCF = { "xelcf", nullptr, L"XEL-CF", ATCreateDeviceXELCF<false> };
extern const ATDeviceDefinition g_ATDeviceDefXELCF3 = { "xelcf3", nullptr, L"XEL-CF3", ATCreateDeviceXELCF<true> };

ATXELCFEmulator::ATXELCFEmulator(bool isV3)
	: mbIsV3(isV3)
{
}

ATXELCFEmulator::~ATXELCFEmulator() {
}

void *ATXELCFEmulator::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceMemMap::kTypeID:		return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceParent::kTypeID:		return static_cast<IATDeviceParent *>(this);

		case IATDeviceButtons::kTypeID:
			if (mbIsV3)
				return static_cast<IATDeviceButtons *>(this);
			else
				return nullptr;

		case ATIDEEmulator::kTypeID:		return static_cast<ATIDEEmulator *>(&mIDE[0]);
		default:
			return nullptr;
	}
}

void ATXELCFEmulator::Init() {
	ATMemoryHandlerTable handlerTable = {};

	handlerTable.mpThis = this;
	handlerTable.mbPassAnticReads = true;
	handlerTable.mbPassReads = true;
	handlerTable.mbPassWrites = true;
	handlerTable.mpDebugReadHandler = OnDebugReadByte;
	handlerTable.mpReadHandler = OnReadByte;
	handlerTable.mpWriteHandler = OnWriteByte;
	mpMemLayerIDE = mpMemMan->CreateLayer(kATMemoryPri_PBI, handlerTable, 0xD1, 0x01);
	mpMemMan->SetLayerName(mpMemLayerIDE, "XEL-CF registers");
	mpMemMan->EnableLayer(mpMemLayerIDE, true);

	mIDE[0].Init(mpScheduler, mpUIRenderer, mpBlockDevices[1] == nullptr, false);
	mIDE[1].Init(mpScheduler, mpUIRenderer, mpBlockDevices[0] == nullptr, true);
}

void ATXELCFEmulator::Shutdown() {
	for(auto& ide : mIDE)
		ide.Shutdown();

	if (mpMemLayerIDE) {
		mpMemMan->DeleteLayer(mpMemLayerIDE);
		mpMemLayerIDE = NULL;
	}

	for(auto& blockDev : mpBlockDevices) {
		if (blockDev) {
			vdpoly_cast<IATDevice *>(blockDev)->SetParent(nullptr, 0);
			blockDev = nullptr;
		}
	}

	mpScheduler = nullptr;
	mpMemMan = nullptr;
	mpUIRenderer = nullptr;
}


void ATXELCFEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = mbIsV3 ? &g_ATDeviceDefXELCF3 : &g_ATDeviceDefXELCF;
}

void ATXELCFEmulator::ColdReset() {
	for(size_t i=0; i<2; ++i)
		mIDE[i].ColdReset();

	mbSelectSlave = false;
}

void ATXELCFEmulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATXELCFEmulator::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATXELCFEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD1C0;
		hi = 0xD1C7;
		return true;
	} else if (index == 1) {
		lo = 0xD1E0;
		hi = 0xD1E7;
		return true;
	}

	return false;
}

void ATXELCFEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

IATDeviceBus *ATXELCFEmulator::GetDeviceBus(uint32 index) {
	return index ? 0 : this;
}

uint32 ATXELCFEmulator::GetSupportedButtons() const {
	return UINT32_C(1) << kATDeviceButton_XELCFSwap;
}

bool ATXELCFEmulator::IsButtonDepressed(ATDeviceButton idx) const {
	return idx == kATDeviceButton_XELCFSwap && mbSwapDepressed;
}

void ATXELCFEmulator::ActivateButton(ATDeviceButton idx, bool state) {
	if (idx == kATDeviceButton_XELCFSwap) {
		if (mbSwapDepressed != state) {
			mbSwapDepressed = state;

			if (state)
				mbSwapActive = true;
		}
	}
}

const wchar_t *ATXELCFEmulator::GetBusName() const {
	return L"IDE/CompactFlash Bus";
}

const char *ATXELCFEmulator::GetBusTag() const {
	return "idebus";
}

const char *ATXELCFEmulator::GetSupportedType(uint32 index) {
	if (index == 0)
		return "harddisk";

	return nullptr;
}

void ATXELCFEmulator::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	for(const auto& blockDev : mpBlockDevices) {
		auto *cdev = vdpoly_cast<IATDevice *>(&*blockDev);

		if (cdev)
			devs.push_back(cdev);
	}
}

void ATXELCFEmulator::AddChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

	if (!blockDevice)
		return;

	for(size_t i=0; i<2; ++i) {
		if (!mpBlockDevices[i]) {
			mpBlockDevices[i] = blockDevice;
			dev->SetParent(this, 0);

			mIDE[i].OpenImage(blockDevice);
			mIDE[i^1].SetIsSingle(false);
			break;
		}
	}
}

void ATXELCFEmulator::RemoveChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

	if (!blockDevice)
		return;

	for(size_t i=0; i<2; ++i) {
		if (mpBlockDevices[i] == blockDevice) {
			mIDE[i].CloseImage();
			mIDE[i^1].SetIsSingle(true);

			dev->SetParent(nullptr, 0);
			mpBlockDevices[i] = nullptr;
		}
	}
}

sint32 ATXELCFEmulator::OnDebugReadByte(void *thisptr0, uint32 addr) {
	ATXELCFEmulator *thisptr = (ATXELCFEmulator *)thisptr0;

	switch(addr & 0xFFF8) {
		case 0xD1C0:
			// The XEL has pull-ups on the data bus, and the XEL-CF requires the MPBI specific to the
			// 1088XEL, so we can safely assume that the data bus is pulled up here. V2 simply doesn't
			// drive the bus, while V3 pulls D6 low as a signature bit and pulls D7 low if the swap
			// latch is set.
			return thisptr->mbIsV3 ? 0x3F + (thisptr->mbSwapActive ? 0x00 : 0x80) : 0xFF;

		case 0xD1E0:
			{
				int selIdx = (thisptr->mbSelectSlave && thisptr->mpBlockDevices[1] ? 1 : 0);

				if (!thisptr->mpBlockDevices[selIdx])
					return 0xFF;

				return (uint8)thisptr->mIDE[selIdx].DebugReadByte((uint8)(addr & 7));
			}
	}

	return OnReadByte(thisptr0, addr);
}

sint32 ATXELCFEmulator::OnReadByte(void *thisptr0, uint32 addr) {
	ATXELCFEmulator *thisptr = (ATXELCFEmulator *)thisptr0;

	switch(addr & 0xFFF8) {
		case 0xD1C0:
			// XEL-CF2 does a reset on any access to $D1C0-D1DF. XEL-CF3 only does so on writes.
			if (!thisptr->mbIsV3) {
				for(int i=0; i<2; ++i) {
					if (thisptr->mpBlockDevices[i])
						thisptr->mIDE[i].ColdReset();
				}
			}

			return OnDebugReadByte(thisptr0, addr);

		case 0xD1E0:
			{
				int selIdx = (thisptr->mbSelectSlave && thisptr->mpBlockDevices[1] ? 1 : 0);

				if (!thisptr->mpBlockDevices[selIdx])
					return 0xFF;

				return (uint8)thisptr->mIDE[selIdx].ReadByte((uint8)(addr & 7));
			}
			break;
	}

	return -1;
}

bool ATXELCFEmulator::OnWriteByte(void *thisptr0, uint32 addr, uint8 value) {
	ATXELCFEmulator *thisptr = (ATXELCFEmulator *)thisptr0;

	switch(addr & 0xFFF8) {
		case 0xD1C0:
			// XEL-CF2: Always reset device
			// XEL-CF3: Always reset swap latch, also reset device if D0=0
			if (!thisptr->mbIsV3 || !(value & 0x01)) {
				for(int i=0; i<2; ++i) {
					if (thisptr->mpBlockDevices[i])
						thisptr->mIDE[i].ColdReset();
				}
			}

			// The XEL-CF3's swap latch is a classic S-R latch, with the Q output
			// sensed by the CPU and /Q shown by LED. This means that if both
			// set and reset inputs are asserted, both Q and /Q both temporarily
			// go high and the the last input to deassert wins. Since the CPU
			// can't reset and read the latch at the same time, holding down the
			// button will always win.
			if (!thisptr->mbSwapDepressed)
				thisptr->mbSwapActive = false;
			break;

		case 0xD1E0:
			for(size_t i=0; i<2; ++i) {
				if (thisptr->mpBlockDevices[i])
					thisptr->mIDE[i].WriteByte((uint8)(addr & 7), value);
			}

			// check for a write to drive/head register
			if ((addr & 7) == 6)
				thisptr->mbSelectSlave = (value & 0x10) != 0;
			break;
	}

	return false;
}
