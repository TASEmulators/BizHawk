//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include "side.h"
#include "memorymanager.h"
#include "ide.h"
#include "uirender.h"
#include "simulator.h"
#include "firmwaremanager.h"

template<bool T_Ver2>
void ATCreateDeviceSIDE(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATSIDEEmulator> p(new ATSIDEEmulator(T_Ver2));

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSIDE = { "side", nullptr, L"SIDE", ATCreateDeviceSIDE<false>, kATDeviceDefFlag_RebootOnPlug };
extern const ATDeviceDefinition g_ATDeviceDefSIDE2 = { "side2", nullptr, L"SIDE 2", ATCreateDeviceSIDE<true>, kATDeviceDefFlag_RebootOnPlug };

ATSIDEEmulator::ATSIDEEmulator(bool v2)
	: mbVersion2(v2)
{
	memset(mFlash, 0xFF, sizeof mFlash);

	mRTC.Init();

	LoadNVRAM();
}

ATSIDEEmulator::~ATSIDEEmulator() {
	SaveNVRAM();
}

void *ATSIDEEmulator::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceMemMap::kTypeID:		return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceCartridge::kTypeID:	return static_cast<IATDeviceCartridge *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceFirmware::kTypeID:	return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceParent::kTypeID:		return static_cast<IATDeviceParent *>(this);
		case IATDeviceDiagnostics::kTypeID:	return static_cast<IATDeviceDiagnostics *>(this);
		case IATDeviceButtons::kTypeID:		return static_cast<IATDeviceButtons *>(this);
		case ATIDEEmulator::kTypeID:		return static_cast<ATIDEEmulator *>(&mIDE);
		default:
			return nullptr;
	}
}

void ATSIDEEmulator::Init() {
	ReloadFirmware();

	mFlashCtrl.Init(mFlash, kATFlashType_Am29F040B, mpScheduler);

	ATMemoryHandlerTable handlerTable = {};

	handlerTable.mpThis = this;
	handlerTable.mbPassAnticReads = true;
	handlerTable.mbPassReads = true;
	handlerTable.mbPassWrites = true;
	handlerTable.mpDebugReadHandler = OnDebugReadByte;
	handlerTable.mpReadHandler = OnReadByte;
	handlerTable.mpWriteHandler = OnWriteByte;
	mpMemLayerIDE = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, handlerTable, 0xD5, 0x01);
	mpMemMan->SetLayerName(mpMemLayerIDE, "SIDE registers");
	mpMemMan->EnableLayer(mpMemLayerIDE, true);

	mpMemLayerCart = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, mFlash, 0xA0, 0x20, true);
	mpMemMan->SetLayerName(mpMemLayerCart, "SIDE left cartridge window");

	if (mbVersion2) {
		mpMemLayerCart2 = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, mFlash, 0x80, 0x20, true);
		mpMemMan->SetLayerName(mpMemLayerCart2, "SIDE right cartridge window");
	}

	handlerTable.mbPassReads = false;
	handlerTable.mbPassWrites = false;
	handlerTable.mbPassAnticReads = false;
	handlerTable.mpDebugReadHandler = OnCartRead;
	handlerTable.mpReadHandler = OnCartRead;
	handlerTable.mpWriteHandler = OnCartWrite;

	mpMemLayerCartControl = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay+1, handlerTable, 0xA0, 0x20);
	mpMemMan->SetLayerName(mpMemLayerCartControl, "SIDE flash control (left cart window)");

	handlerTable.mpDebugReadHandler = OnCartRead2;
	handlerTable.mpReadHandler = OnCartRead2;
	handlerTable.mpWriteHandler = OnCartWrite2;

	mpMemLayerCartControl2 = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay+1, handlerTable, 0x80, 0x20);
	mpMemMan->SetLayerName(mpMemLayerCartControl2, "SIDE flash control (right cart window)");

	mIDE.Init(mpScheduler, mpUIRenderer);
}

void ATSIDEEmulator::Shutdown() {
	mFlashCtrl.Shutdown();
	mIDE.Shutdown();

	if (mpCartridgePort) {
		mpCartridgePort->RemoveCartridge(mCartId, this);
		mpCartridgePort = nullptr;
	}

	if (mpMemLayerCartControl2) {
		mpMemMan->DeleteLayer(mpMemLayerCartControl2);
		mpMemLayerCartControl2 = NULL;
	}

	if (mpMemLayerCartControl) {
		mpMemMan->DeleteLayer(mpMemLayerCartControl);
		mpMemLayerCartControl = NULL;
	}

	if (mpMemLayerCart2) {
		mpMemMan->DeleteLayer(mpMemLayerCart2);
		mpMemLayerCart2 = NULL;
	}

	if (mpMemLayerCart) {
		mpMemMan->DeleteLayer(mpMemLayerCart);
		mpMemLayerCart = NULL;
	}

	if (mpMemLayerIDE) {
		mpMemMan->DeleteLayer(mpMemLayerIDE);
		mpMemLayerIDE = NULL;
	}

	if (mpBlockDevice) {
		vdpoly_cast<IATDevice *>(mpBlockDevice)->SetParent(nullptr, 0);
		mpBlockDevice = nullptr;
	}

	mpScheduler = nullptr;
	mpMemMan = NULL;
	mpUIRenderer = NULL;
}

void ATSIDEEmulator::SetSDXEnabled(bool enable) {
	if (mbSDXEnable == enable)
		return;

	mbSDXEnable = enable;
	UpdateMemoryLayersCart();
}

void ATSIDEEmulator::ResetCartBank() {
	mSDXBankRegister = 0x00;
	SetSDXBank(0, true);

	mTopBankRegister = 0x00;
	SetTopBank(0x20, true, false);
}

void ATSIDEEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = mbVersion2 ? &g_ATDeviceDefSIDE2 : &g_ATDeviceDefSIDE;
}

void ATSIDEEmulator::ColdReset() {
	mFlashCtrl.ColdReset();
	mRTC.ColdReset();

	ResetCartBank();

	mbIDEReset = true;
	mbIDEEnabled = true;

	// If the CF card is absent, the removed flag is always set and can't be
	// cleared. If it's present, the removed flag is cleared on powerup.
	mbIDERemoved = !mpBlockDevice;

	UpdateIDEReset();
}

void ATSIDEEmulator::LoadNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	uint8 buf[0x72];
	memset(buf, 0, sizeof buf);

	if (key.getBinary("SIDE clock", (char *)buf, 0x72))
		mRTC.Load(buf);
}

void ATSIDEEmulator::SaveNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	uint8 buf[0x72];
	memset(buf, 0, sizeof buf);

	mRTC.Save(buf);

	key.setBinary("SIDE clock", (const char *)buf, 0x72);
}

void ATSIDEEmulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATSIDEEmulator::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATSIDEEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD5E0;
		hi = 0xD5FF;
		return true;
	}

	return false;
}

void ATSIDEEmulator::InitCartridge(IATDeviceCartridgePort *cartPort) {
	mpCartridgePort = cartPort;
	mpCartridgePort->AddCartridge(this, kATCartridgePriority_Internal, mCartId);
}

bool ATSIDEEmulator::IsLeftCartActive() const {
	const bool sdxRead = (mbSDXEnable && mSDXBank >= 0);
	const bool topRead = (mbTopEnable || !mbSDXEnable) && mbTopLeftEnable;

	return sdxRead || topRead;
}

void ATSIDEEmulator::SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) {
	bool changed = false;

	if (mbLeftWindowEnabled != leftEnable) {
		mbLeftWindowEnabled = leftEnable;
		changed = true;
	}

	if (mbRightWindowEnabled != rightEnable) {
		mbRightWindowEnabled = rightEnable;
		changed = true;
	}
	
	if (mbCCTLEnabled != cctlEnable) {
		mbCCTLEnabled = cctlEnable;
		changed = true;
	}

	if (changed && mpMemMan && mpMemLayerCart)
		UpdateMemoryLayersCart();
}

void ATSIDEEmulator::UpdateCartSense(bool leftActive) {
}

void ATSIDEEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATSIDEEmulator::InitFirmware(ATFirmwareManager *fwman) {
	mpFirmwareManager = fwman;
}

bool ATSIDEEmulator::ReloadFirmware() {
	void *flash = mFlash;
	uint32 flashSize = sizeof mFlash;

	vduint128 oldHash = VDHash128(flash, flashSize);

	mFlashCtrl.SetDirty(false);

	memset(flash, 0xFF, flashSize);

	const uint64 id = mpFirmwareManager->GetCompatibleFirmware(mbVersion2 ? kATFirmwareType_SIDE2 : kATFirmwareType_SIDE);
	mpFirmwareManager->LoadFirmware(id, flash, 0, flashSize, nullptr, nullptr, nullptr, nullptr, &mbFirmwareUsable);

	return oldHash != VDHash128(flash, flashSize);
}

const wchar_t *ATSIDEEmulator::GetWritableFirmwareDesc(uint32 idx) const {
	if (idx == 0)
		return L"Cartridge ROM";
	else
		return nullptr;
}

bool ATSIDEEmulator::IsWritableFirmwareDirty(uint32 idx) const {
	return idx == 0 && mFlashCtrl.IsDirty();
}

void ATSIDEEmulator::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
	stream.Write(mFlash, sizeof mFlash);

	mFlashCtrl.SetDirty(false);
}

ATDeviceFirmwareStatus ATSIDEEmulator::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

IATDeviceBus *ATSIDEEmulator::GetDeviceBus(uint32 index) {
	return index ? 0 : this;
}

const wchar_t *ATSIDEEmulator::GetBusName() const {
	return L"CompactFlash Bus";
}

const char *ATSIDEEmulator::GetBusTag() const {
	return "idebus";
}

const char *ATSIDEEmulator::GetSupportedType(uint32 index) {
	if (index == 0)
		return "harddisk";

	return nullptr;
}

void ATSIDEEmulator::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	auto *cdev = vdpoly_cast<IATDevice *>(&*mpBlockDevice);

	if (cdev)
		devs.push_back(cdev);
}

void ATSIDEEmulator::AddChildDevice(IATDevice *dev) {
	if (mpBlockDevice)
		return;

	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

	if (blockDevice) {
		mpBlockDevice = blockDevice;
		dev->SetParent(this, 0);

		mIDE.OpenImage(blockDevice);
		UpdateIDEReset();
	}
}

void ATSIDEEmulator::RemoveChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

	if (mpBlockDevice == blockDevice) {
		mIDE.CloseImage();
		dev->SetParent(nullptr, 0);
		mpBlockDevice = nullptr;

		mbIDERemoved = true;
		UpdateIDEReset();
	}
}

void ATSIDEEmulator::DumpStatus(ATConsoleOutput& output) {
	mRTC.DumpStatus(output);
}

uint32 ATSIDEEmulator::GetSupportedButtons() const {
	return (1 << kATDeviceButton_CartridgeResetBank) | (1 << kATDeviceButton_CartridgeSDXEnable);
}

bool ATSIDEEmulator::IsButtonDepressed(ATDeviceButton idx) const {
	return idx == kATDeviceButton_CartridgeSDXEnable && mbSDXEnable;
}

void ATSIDEEmulator::ActivateButton(ATDeviceButton idx, bool state) {
	if (idx == kATDeviceButton_CartridgeResetBank) {
		ResetCartBank();
	} else if (idx == kATDeviceButton_CartridgeSDXEnable) {
		SetSDXEnabled(state);
	}
}

void ATSIDEEmulator::SetSDXBank(sint32 bank, bool topEnable) {
	if (mSDXBank == bank && mbTopEnable == topEnable)
		return;

	mSDXBank = bank;
	mbTopEnable = topEnable;

	UpdateMemoryLayersCart();
	mpCartridgePort->OnLeftWindowChanged(mCartId, IsLeftCartActive());
}

void ATSIDEEmulator::SetTopBank(sint32 bank, bool topLeftEnable, bool topRightEnable) {
	// If the top cartridge is enabled in 16K mode, the LSB bank bit is ignored.
	// We force the LSB on in that case so the right cart window is in the right
	// place and the left cart window is 8K below that (mask LSB back off).
	if (topRightEnable)
		bank |= 0x01;

	if (mTopBank == bank && mbTopRightEnable == topRightEnable && mbTopLeftEnable == topLeftEnable)
		return;

	mTopBank = bank;
	mbTopLeftEnable = topLeftEnable;
	mbTopRightEnable = topRightEnable;

	UpdateMemoryLayersCart();
	mpCartridgePort->OnLeftWindowChanged(mCartId, IsLeftCartActive());
}

sint32 ATSIDEEmulator::OnDebugReadByte(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	if (addr < 0xD5E0)
		return -1;

	switch(addr) {
		case 0xD5F0:
		case 0xD5F1:
		case 0xD5F2:
		case 0xD5F3:
		case 0xD5F4:
		case 0xD5F5:
		case 0xD5F6:
		case 0xD5F7:
			if (thisptr->mbIDEEnabled && thisptr->mpBlockDevice)
				return (uint8)thisptr->mIDE.DebugReadByte((uint8)addr & 7);
			else
				return 0xFF;
	}

	return OnReadByte(thisptr0, addr);
}

sint32 ATSIDEEmulator::OnReadByte(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	if (addr < 0xD5E0)
		return -1;

	switch(addr) {
		case 0xD5E1:	// SDX bank register
			if (thisptr->mbVersion2)
				return thisptr->mSDXBankRegister;

			break;

		case 0xD5E2:	// DS1305 RTC
			return thisptr->mRTC.ReadState() ? 0x08 : 0x00;

		case 0xD5E4:	// top cartridge bank switching
			if (thisptr->mbVersion2)
				return thisptr->mTopBankRegister;

			return 0xFF;

		case 0xD5F0:
		case 0xD5F1:
		case 0xD5F2:
		case 0xD5F3:
		case 0xD5F4:
		case 0xD5F5:
		case 0xD5F6:
		case 0xD5F7:
			return thisptr->mbIDEEnabled && thisptr->mpBlockDevice ? (uint8)thisptr->mIDE.ReadByte((uint8)addr & 7) : 0xFF;

		case 0xD5F8:
			if (thisptr->mbVersion2)
				return 0x32;

			break;

		case 0xD5F9:
			if (thisptr->mbVersion2) {
				// LSB=1 is currently card removed, which we don't support
				// yet.
				return thisptr->mbIDERemoved ? 1 : 0;
			}

			break;

		case 0xD5FC:	return thisptr->mbSDXEnable ? 'S' : ' ';
		case 0xD5FD:	return 'I';
		case 0xD5FE:	return 'D';
		case 0xD5FF:	return 'E';
	}

	return -1;
}

bool ATSIDEEmulator::OnWriteByte(void *thisptr0, uint32 addr, uint8 value) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	if (addr < 0xD5E0)
		return false;

	switch(addr) {
		case 0xD5E0:
			if (!thisptr->mbVersion2 && thisptr->mSDXBankRegister != value) {
				thisptr->mSDXBankRegister = value;

				thisptr->SetSDXBank(value & 0x80 ? -1 : (value & 0x3f), !(value & 0x40));
			}
			break;

		case 0xD5E1:
			if (thisptr->mbVersion2 && thisptr->mSDXBankRegister != value) {
				thisptr->mSDXBankRegister = value;

				thisptr->SetSDXBank(value & 0x80 ? -1 : (value & 0x3f), !(value & 0x40));
			}
			break;

		case 0xD5E2:	// DS1305 RTC
			thisptr->mRTC.WriteState((value & 1) != 0, !(value & 2), (value & 4) != 0);
			break;

		case 0xD5E4:	// top cartridge bank switching
			if (thisptr->mTopBankRegister != value) {
				thisptr->mTopBankRegister = value;
				thisptr->SetTopBank((value & 0x3f) ^ 0x20, (value & 0x80) == 0, thisptr->mbVersion2 && ((value & 0x40) != 0));
			}
			break;

		case 0xD5F0:
		case 0xD5F1:
		case 0xD5F2:
		case 0xD5F3:
		case 0xD5F4:
		case 0xD5F5:
		case 0xD5F6:
		case 0xD5F7:
			if (thisptr->mbIDEEnabled && thisptr->mpBlockDevice)
				thisptr->mIDE.WriteByte((uint8)addr & 7, value);
			break;

		case 0xD5F8:	// F8-FB: D0 = /reset
		case 0xD5F9:
		case 0xD5FA:
		case 0xD5FB:
			if (thisptr->mbVersion2) {
				if (addr == 0xD5F9) {
					// Strobe to clear CARD_REMOVED. This can't be done if there isn't actually a
					// card.
					if (thisptr->mpBlockDevice)
						thisptr->mbIDERemoved = false;
				}

				thisptr->mbIDEEnabled = !(value & 0x80);
			}

			// SIDE 1 allows reset on F8-FB; SIDE 2 is only F8
			if (!thisptr->mbVersion2 || addr == 0xD5F8) {
				thisptr->mbIDEReset = !(value & 1);
				thisptr->UpdateIDEReset();
			}
			break;
	}

	return true;
}

sint32 ATSIDEEmulator::OnCartDebugRead(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.DebugReadByte(thisptr->mBankOffset + (addr - 0xA000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return value;
}

sint32 ATSIDEEmulator::OnCartDebugRead2(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.DebugReadByte(thisptr->mBankOffset2 + (addr - 0x8000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return value;
}

sint32 ATSIDEEmulator::OnCartRead(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.ReadByte(thisptr->mBankOffset + (addr - 0xA000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return value;
}

sint32 ATSIDEEmulator::OnCartRead2(void *thisptr0, uint32 addr) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.ReadByte(thisptr->mBankOffset2 + (addr - 0x8000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return value;
}

bool ATSIDEEmulator::OnCartWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	if (thisptr->mFlashCtrl.WriteByte(thisptr->mBankOffset + (addr - 0xA000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return true;
}

bool ATSIDEEmulator::OnCartWrite2(void *thisptr0, uint32 addr, uint8 value) {
	ATSIDEEmulator *thisptr = (ATSIDEEmulator *)thisptr0;

	if (thisptr->mFlashCtrl.WriteByte(thisptr->mBankOffset2 + (addr - 0x8000), value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersCart();
	}

	return true;
}

void ATSIDEEmulator::UpdateMemoryLayersCart() {
	if (mSDXBank >= 0 && mbSDXEnable)
		mBankOffset = mSDXBank << 13;
	else if (mbTopEnable || !mbSDXEnable)
		mBankOffset = mTopBank << 13;

	mBankOffset2 = mBankOffset & ~0x2000;

	mpMemMan->SetLayerMemory(mpMemLayerCart, mFlash + mBankOffset);

	if (mbVersion2)
		mpMemMan->SetLayerMemory(mpMemLayerCart2, mFlash + mBankOffset2);

	// SDX disabled by switch => top cartridge enabled, SDX control bits ignored
	// else   SDX disabled, top cartridge disabled => no cartridge
	//        SDX disabled, top cartridge enabled => top cartridge
	//        other => SDX cartridge

	const bool sdxRead = (mbSDXEnable && mSDXBank >= 0);
	const bool topRead = mbTopEnable || !mbSDXEnable;
	const bool topLeftRead = topRead && mbTopLeftEnable;

	const bool flashRead = mbLeftWindowEnabled && (sdxRead || topLeftRead);
	const bool controlRead = flashRead && mFlashCtrl.IsControlReadEnabled();

	mpMemMan->EnableLayer(mpMemLayerCartControl, kATMemoryAccessMode_AnticRead, controlRead);
	mpMemMan->EnableLayer(mpMemLayerCartControl, kATMemoryAccessMode_CPURead, controlRead);
	mpMemMan->EnableLayer(mpMemLayerCartControl, kATMemoryAccessMode_CPUWrite, flashRead);
	mpMemMan->EnableLayer(mpMemLayerCart, flashRead);

	if (mbVersion2) {
		const bool flashReadRight = mbRightWindowEnabled && topRead && !sdxRead && mbTopRightEnable;
		const bool controlReadRight = flashReadRight && mFlashCtrl.IsControlReadEnabled();

		mpMemMan->EnableLayer(mpMemLayerCartControl2, kATMemoryAccessMode_AnticRead, controlReadRight);
		mpMemMan->EnableLayer(mpMemLayerCartControl2, kATMemoryAccessMode_CPURead, controlReadRight);
		mpMemMan->EnableLayer(mpMemLayerCartControl2, kATMemoryAccessMode_CPUWrite, flashReadRight);
		mpMemMan->EnableLayer(mpMemLayerCart2, flashReadRight);
	}

	mpMemMan->EnableLayer(mpMemLayerIDE, mbCCTLEnabled);
}

void ATSIDEEmulator::UpdateIDEReset() {
	mIDE.SetReset(mbIDEReset || !mpBlockDevice);
}
