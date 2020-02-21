//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <at/atcore/propertyset.h>
#include "myide.h"
#include "memorymanager.h"
#include "ide.h"
#include "uirender.h"
#include "firmwaremanager.h"

template<bool T_Ver2, bool T_UseD5xx>
void ATCreateDeviceMyIDE(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATMyIDEEmulator> p(new ATMyIDEEmulator(T_Ver2, T_UseD5xx));

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefMyIDED1xx = { "myide-d1xx", nullptr, L"MyIDE (internal)", ATCreateDeviceMyIDE<false, false> };
extern const ATDeviceDefinition g_ATDeviceDefMyIDED5xx = { "myide-d5xx", nullptr, L"MyIDE (cartridge)", ATCreateDeviceMyIDE<false, true> };
extern const ATDeviceDefinition g_ATDeviceDefMyIDE2 = { "myide2", "myide2", L"MyIDE-II", ATCreateDeviceMyIDE<true, true>, kATDeviceDefFlag_RebootOnPlug };

ATMyIDEEmulator::ATMyIDEEmulator(bool ver2, bool useD5xx)
	: mbVersion2(ver2)
	, mbUseD5xx(useD5xx)
{
	memset(mFirmware, 0xFF, sizeof mFirmware);
}

ATMyIDEEmulator::~ATMyIDEEmulator() {
}

void *ATMyIDEEmulator::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceMemMap::kTypeID:		return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceCartridge::kTypeID:	return static_cast<IATDeviceCartridge *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceFirmware::kTypeID:	return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceParent::kTypeID:		return static_cast<IATDeviceParent *>(this);
		case ATIDEEmulator::kTypeID:		return static_cast<ATIDEEmulator *>(&mIDE[0]);
		default:
			return nullptr;
	}
}

void ATMyIDEEmulator::Init() {
	ReloadFirmware();

	mFlash.SetDirty(false);

	ATMemoryHandlerTable handlerTable = {};

	handlerTable.mbPassAnticReads = true;
	handlerTable.mbPassReads = true;
	handlerTable.mbPassWrites = true;

	if (mbVersion2) {
		mFlash.Init(mFirmware, kATFlashType_Am29F040B, mpScheduler);

		handlerTable.mpThis = this;
		handlerTable.mpDebugReadHandler = OnDebugReadByte_CCTL_V2;
		handlerTable.mpReadHandler = OnReadByte_CCTL_V2;
		handlerTable.mpWriteHandler = OnWriteByte_CCTL_V2;

		mpMemLayerIDE = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, handlerTable, 0xD5, 0x01);
		mpMemMan->SetLayerName(mpMemLayerIDE, "MyIDE-II control");

		handlerTable.mpDebugReadHandler = DebugReadByte_Cart_V2;
		handlerTable.mpReadHandler = ReadByte_Cart_V2;
		handlerTable.mpWriteHandler = WriteByte_Cart_V2;

		mpMemLayerLeftCartFlash = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay+1, handlerTable, 0xA0, 0x20);
		mpMemLayerRightCartFlash = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay+1, handlerTable, 0x80, 0x20);

		mpMemLayerLeftCart = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, mFirmware, 0xA0, 0x20, true);
		mpMemLayerRightCart = mpMemMan->CreateLayer(kATMemoryPri_CartridgeOverlay, mFirmware, 0x80, 0x20, true);

		mpMemMan->SetLayerName(mpMemLayerLeftCart, "MyIDE-II left cartridge window");
		mpMemMan->SetLayerName(mpMemLayerRightCart, "MyIDE-II right cartridge window");
		mpMemMan->SetLayerName(mpMemLayerLeftCartFlash, "MyIDE-II left cartridge flash read");
		mpMemMan->SetLayerName(mpMemLayerRightCartFlash, "MyIDE-II right cartridge flash read");

		mCartBank = 0;
		mCartBank2 = -1;

		UpdateCartBank();
		UpdateCartBank2();
	} else {
		handlerTable.mpThis = this;
		handlerTable.mpDebugReadHandler = OnDebugReadByte_CCTL;
		handlerTable.mpReadHandler = OnReadByte_CCTL;
		handlerTable.mpWriteHandler = OnWriteByte_CCTL;

		mpMemLayerIDE = mpMemMan->CreateLayer(kATMemoryPri_Cartridge1 - 1, handlerTable, mbUseD5xx ? 0xD5 : 0xD1, 0x01);
		mpMemMan->SetLayerName(mpMemLayerIDE, "MyIDE control");
		mpMemMan->EnableLayer(mpMemLayerIDE, !mbVersion2 || mbCCTLEnabled);

		mCartBank = -1;
		mCartBank2 = -1;
	}

	mpMemMan->EnableLayer(mpMemLayerIDE, true);

	mIDE[0].Init(mpScheduler, mpUIRenderer, mpBlockDevices[1] == nullptr, false);
	mIDE[1].Init(mpScheduler, mpUIRenderer, mpBlockDevices[0] == nullptr, true);

	ColdReset();
}

void ATMyIDEEmulator::Shutdown() {
	if (mpCartridgePort) {
		mpCartridgePort->RemoveCartridge(mCartId, this);
		mpCartridgePort = nullptr;
	}

	mFlash.Shutdown();

	if (mpMemLayerRightCartFlash) {
		mpMemMan->DeleteLayer(mpMemLayerRightCartFlash);
		mpMemLayerRightCartFlash = nullptr;
	}

	if (mpMemLayerLeftCartFlash) {
		mpMemMan->DeleteLayer(mpMemLayerLeftCartFlash);
		mpMemLayerLeftCartFlash = nullptr;
	}

	if (mpMemLayerRightCart) {
		mpMemMan->DeleteLayer(mpMemLayerRightCart);
		mpMemLayerRightCart = nullptr;
	}

	if (mpMemLayerLeftCart) {
		mpMemMan->DeleteLayer(mpMemLayerLeftCart);
		mpMemLayerLeftCart = nullptr;
	}

	if (mpMemLayerIDE) {
		mpMemMan->DeleteLayer(mpMemLayerIDE);
		mpMemLayerIDE = nullptr;
	}

	for(auto& ide : mIDE)
		ide.Shutdown();

	for(auto& blockDev : mpBlockDevices) {
		if (blockDev) {
			vdpoly_cast<IATDevice *>(blockDev)->SetParent(nullptr, 0);
			blockDev = nullptr;
		}
	}

	mpScheduler = nullptr;
	mpUIRenderer = nullptr;
	mpMemMan = nullptr;
	mpFirmwareManager = nullptr;
}

void ATMyIDEEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = mbVersion2 ? &g_ATDeviceDefMyIDE2 : mbUseD5xx ? &g_ATDeviceDefMyIDED5xx : &g_ATDeviceDefMyIDED1xx;
}

void ATMyIDEEmulator::GetSettings(ATPropertySet& settings) {
	if (mbVersion2) {
		settings.SetUint32("cpldver", mbVersion2Ex ? 2 : 1);
	}
}

bool ATMyIDEEmulator::SetSettings(const ATPropertySet& settings) {
	if (mbVersion2) {
		mbVersion2Ex = settings.GetUint32("cpldver") >= 2;
	}

	return true;
}

void ATMyIDEEmulator::ColdReset() {
	if (mbVersion2) {
		mbCFPowerLatch = false;
		mbCFResetLatch = true;
	} else {
		mbCFPowerLatch = true;
		mbCFResetLatch = false;
	}

	mbCFPower = mbCFPowerLatch;
	mbCFReset = mbCFResetLatch;

	mbCFAltReg = false;

	mbSelectSlave = false;

	mLeftPage = 0;
	mRightPage = 0;
	mKeyHolePage = 0;
	mControl = 0x30;

	if (mbVersion2) {
		mCartBank = 0;
		mCartBank2 = -1;

		UpdateCartBank();
		UpdateCartBank2();

		memset(mRAM, 0xFF, sizeof mRAM);
	}

	UpdateIDEReset();
}

void ATMyIDEEmulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATMyIDEEmulator::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATMyIDEEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		if (mbUseD5xx) {
			lo = 0xD500;
			hi = 0xD5FF;
		} else {
			lo = 0xD100;
			hi = 0xD1FF;
		}

		return true;
	}

	return false;
}

void ATMyIDEEmulator::InitCartridge(IATDeviceCartridgePort *cartPort) {
	if (mbVersion2) {
		mpCartridgePort = cartPort;
		mpCartridgePort->AddCartridge(this, kATCartridgePriority_Internal, mCartId);
	}
}

bool ATMyIDEEmulator::IsLeftCartActive() const {
	return mCartBank >= 0;
}

void ATMyIDEEmulator::SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) {
	if (mbLeftWindowEnabled != leftEnable) {
		mbLeftWindowEnabled = leftEnable;

		if (mpMemMan && mpMemLayerLeftCart)
			UpdateCartBank();
	}

	if (mbRightWindowEnabled != rightEnable) {
		mbRightWindowEnabled = rightEnable;

		if (mpMemMan && mpMemLayerRightCart)
			UpdateCartBank2();
	}

	if (mbCCTLEnabled != cctlEnable) {
		mbCCTLEnabled = cctlEnable;

		if (mpMemLayerIDE)
			mpMemMan->EnableLayer(mpMemLayerIDE, cctlEnable);
	}
}

void ATMyIDEEmulator::UpdateCartSense(bool leftActive) {
}

void ATMyIDEEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATMyIDEEmulator::InitFirmware(ATFirmwareManager *fwman) {
	mpFirmwareManager = fwman;
}

bool ATMyIDEEmulator::ReloadFirmware() {
	if (!mbVersion2)
		return false;

	void *flash = mFirmware;
	uint32 flashSize = sizeof mFirmware;

	vduint128 oldHash = VDHash128(flash, flashSize);

	mFlash.SetDirty(false);

	memset(flash, 0xFF, flashSize);

	const uint64 id = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_MyIDE2);
	mpFirmwareManager->LoadFirmware(id, flash, 0, flashSize, nullptr, nullptr, nullptr, nullptr, &mbFirmwareUsable);

	return oldHash != VDHash128(flash, flashSize);
}

const wchar_t *ATMyIDEEmulator::GetWritableFirmwareDesc(uint32 idx) const {
	if (idx == 0)
		return L"Cartridge ROM";
	else
		return nullptr;
}

bool ATMyIDEEmulator::IsWritableFirmwareDirty(uint32 idx) const {
	return idx == 0 && mFlash.IsDirty();
}

void ATMyIDEEmulator::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
	if (mbVersion2) {
		stream.Write(mFirmware, sizeof mFirmware);

		mFlash.SetDirty(false);
	}
}

ATDeviceFirmwareStatus ATMyIDEEmulator::GetFirmwareStatus() const {
	return !mbVersion2 || mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

IATDeviceBus *ATMyIDEEmulator::GetDeviceBus(uint32 index) {
	return index ? nullptr : this;
}

const wchar_t *ATMyIDEEmulator::GetBusName() const {
	return L"IDE/CompactFlash Bus";
}

const char *ATMyIDEEmulator::GetBusTag() const {
	return "idebus";
}

const char *ATMyIDEEmulator::GetSupportedType(uint32 index) {
	if (index == 0)
		return "harddisk";

	return nullptr;
}

void ATMyIDEEmulator::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	for(const auto& blockDev : mpBlockDevices) {
		auto *cdev = vdpoly_cast<IATDevice *>(&*blockDev);

		if (cdev)
			devs.push_back(cdev);
	}
}

void ATMyIDEEmulator::AddChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);
	if (!blockDevice)
		return;

	for(size_t i=0; i<2; ++i) {
		if (!mpBlockDevices[i]) {
			mpBlockDevices[i] = blockDevice;
			dev->SetParent(this, 0);

			mIDE[i].OpenImage(blockDevice);
			mIDE[i^1].SetIsSingle(false);
			UpdateIDEReset();
			break;
		}

		if (mbVersion2)
			break;
	}
}

void ATMyIDEEmulator::RemoveChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);
	if (!blockDevice)
		return;

	for(size_t i=0; i<2; ++i) {
		if (mpBlockDevices[i] == blockDevice) {
			mIDE[i].CloseImage();
			mIDE[i^1].SetIsSingle(true);

			dev->SetParent(nullptr, 0);
			mpBlockDevices[i] = nullptr;

			// Pulling the CF device resets the state back to unpowered, reset, and
			// alt reg off. It does NOT change the latch bits!
			if (mbVersion2) {
				mbCFPower = false;
				mbCFReset = true;
				mbCFAltReg = false;
			}

			UpdateIDEReset();
		}
	}
}

sint32 ATMyIDEEmulator::OnDebugReadByte_CCTL(void *thisptr0, uint32 addr) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;
	int selIdx = (thisptr->mbSelectSlave && thisptr->mpBlockDevices[1] ? 1 : 0);

	if (!thisptr->mpBlockDevices[selIdx])
		return 0xFF;

	return (uint8)thisptr->mIDE[selIdx].DebugReadByte((uint8)addr);
}

sint32 ATMyIDEEmulator::OnReadByte_CCTL(void *thisptr0, uint32 addr) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;
	int selIdx = (thisptr->mbSelectSlave && thisptr->mpBlockDevices[1] ? 1 : 0);

	if (!thisptr->mpBlockDevices[selIdx])
		return 0xFF;

	return (uint8)thisptr->mIDE[selIdx].ReadByte((uint8)addr);
}

bool ATMyIDEEmulator::OnWriteByte_CCTL(void *thisptr0, uint32 addr, uint8 value) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	for(size_t i=0; i<2; ++i) {
		if (thisptr->mpBlockDevices[i])
			thisptr->mIDE[i].WriteByte((uint8)addr, value);
	}

	// check for a write to drive/head register
	if ((addr & 7) == 6)
		thisptr->mbSelectSlave = (value & 0x10) != 0;

	return true;
}

sint32 ATMyIDEEmulator::OnDebugReadByte_CCTL_V2(void *thisptr0, uint32 addr) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	// The updated V2 maps $D540-D57F to the data register.
	if (thisptr->mbVersion2Ex && addr >= 0xD540 && addr < 0xD580) {
		addr = 0xD500;
	}

	if (addr < 0xD508) {

		if (!thisptr->mbCFPower || !thisptr->mpBlockDevices[0])
			return 0xFF;

		if (thisptr->mbCFAltReg)
			return (uint8)thisptr->mIDE[0].ReadByteAlt((uint8)addr);
		else
			return (uint8)thisptr->mIDE[0].DebugReadByte((uint8)addr);
	}

	return OnReadByte_CCTL_V2(thisptr0, addr);
}

sint32 ATMyIDEEmulator::OnReadByte_CCTL_V2(void *thisptr0, uint32 addr) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	// The updated V2 maps $D540-D57F to the data register.
	if (thisptr->mbVersion2Ex && addr >= 0xD540 && addr < 0xD580) {
		addr = 0xD500;
	}

	if (addr < 0xD510) {
		if (addr >= 0xD508) {
			// bit 7 = CF present
			// bit 6 = CF /RESET
			// bit 5 = CF powered
			//
			// Bits 5 and 7 always reflect the actual state of the device and
			// are always 0 when no CF device is inserted. They do NOT report
			// the state of bits 0 and 1 written to $D50E, which have no effect
			// but ARE remembered in this state (and can cause problems once
			// a CF card is reinserted).
			//
			// The green light on the MyIDE-II also lights up when the CF card
			// is powered.

			uint8 status = 0x1F;

			if (thisptr->mpBlockDevices[0]) {
				status |= 0x80;

				if (!thisptr->mbCFReset)
					status |= 0x40;

				if (thisptr->mbCFPower)
					status |= 0x20;
			}

			return status;
		}

		if (!thisptr->mbCFPower || !thisptr->mpBlockDevices[0])
			return 0xFF;

		if (thisptr->mbCFAltReg)
			return (uint8)thisptr->mIDE[0].ReadByteAlt((uint8)addr);
		else
			return (uint8)thisptr->mIDE[0].ReadByte((uint8)addr);
	} else if (addr >= 0xD580) {
		uint8 data;

		switch(thisptr->mControl & 0x0c) {
			case 0x00:		// R/W SRAM
			case 0x04:		// R/O SRAM
			default:
				return thisptr->mRAM[thisptr->mKeyHolePage + (addr - 0xD580)];

			case 0x08:
				thisptr->mFlash.ReadByte(thisptr->mKeyHolePage + (addr - 0xD580), data);
				return data;

			case 0x0c:		// disabled
				break;
		}
	}

	return -1;
}

bool ATMyIDEEmulator::OnWriteByte_CCTL_V2(void *thisptr0, uint32 addr, uint8 value) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	if (addr >= 0xD580) {
		switch(thisptr->mControl & 0x0c) {
			case 0x00:		// R/W SRAM
			default:
				thisptr->mRAM[thisptr->mKeyHolePage + (addr - 0xD580)] = value;
				break;

			case 0x04:		// R/O SRAM
			case 0x0c:		// disabled
				break;

			case 0x08:
				thisptr->mFlash.WriteByte(thisptr->mKeyHolePage + (addr - 0xD580), value);
				break;
		}
		return true;
	}

	// The updated V2 maps $D540-D57F to the data register.
	if (thisptr->mbVersion2Ex && addr >= 0xD540) {
		addr = 0xD500;
	}
	
	switch(addr) {
		case 0xD500:
		case 0xD501:
		case 0xD502:
		case 0xD503:
		case 0xD504:
		case 0xD505:
		case 0xD506:
		case 0xD507:
			if (thisptr->mbCFPower && thisptr->mpBlockDevices[0]) {
				if (thisptr->mbCFAltReg)
					thisptr->mIDE[0].WriteByteAlt((uint8)addr, value);
				else
					thisptr->mIDE[0].WriteByte((uint8)addr, value);
			}
			break;

		case 0xD508:
			value &= 0x3f;

			if (thisptr->mLeftPage != value) {
				thisptr->mLeftPage = value;

				if ((thisptr->mControl & 0xc0) < 0xc0)
					thisptr->SetCartBank(((thisptr->mControl & 0xc0) << 2) + value);
			}
			return true;

		case 0xD50A:
			value &= 0x3f;

			if (thisptr->mRightPage != value) {
				thisptr->mRightPage = value;

				if ((thisptr->mControl & 0x30) < 0x30)
					thisptr->SetCartBank2(((thisptr->mControl & 0x30) << 4) + value);
			}
			return true;

		case 0xD50C:
			thisptr->mKeyHolePage = (thisptr->mKeyHolePage & 0x7f8000) + ((uint32)value << 7);
			return true;

		case 0xD50D:
			thisptr->mKeyHolePage = (thisptr->mKeyHolePage & 0x007f80) + ((uint32)(value & 0x0f) << 15);
			return true;

		case 0xD50E:
			// bit 1 = CF power
			// bit 0 = CF reset unlock & alternate status
			//
			// When a CF card is inserted, it is initially unpowered. A rising edge on
			// bit 1 is required to transition to the powered-reset state; after that,
			// a rising edge on bit 0 transitions to powered-active, and bit 0 can then
			// be toggled afterward to switch between normal and alternate status. The
			// device cannot be put back into reset without powering it down.
			//
			// If bits 0 and 1 are simultaneously raised at the same time, the device
			// only powers up and is still held in reset, and the transition on bit 0
			// is ignored.
			//
			// Both bits are NOT reset when the CF card is removed. If bit 1 is raised
			// when a CF card is inserted, it must be lowered and raised again for the
			// device to power up. The green LED does NOT light if bit 1 is raised and
			// the device is not actually powered.

			if (thisptr->mpBlockDevices[0]) {
				const bool power = (value & 2) != 0;
				const bool reset = (value & 1) == 0;

				if (!thisptr->mbCFPower) {		// unpowered
					// possibly powering up device
					if (power && !thisptr->mbCFPowerLatch)
						thisptr->mbCFPower = true;
				} else if (!power) {
					// powering down device
					thisptr->mbCFPower = false;
					thisptr->mbCFReset = true;
					thisptr->mbCFAltReg = false;
				} else if (thisptr->mbCFReset) {
					// device is powered and in reset state -- check if we are
					// pulling it out of reset
					if (!reset && thisptr->mbCFResetLatch)
						thisptr->mbCFReset = false;
				} else {
					// device is not in reset state -- toggle alt reg status
					thisptr->mbCFAltReg = reset;
				}

				// store new state in latches, regardless of any effect it actually
				// had
				thisptr->mbCFPowerLatch = power;
				thisptr->mbCFResetLatch = reset;
			}

			thisptr->UpdateIDEReset();
			break;

		case 0xD50F:
			value &= 0xfd;

			if (thisptr->mControl != value) {
				uint8 delta = thisptr->mControl ^ value;

				thisptr->mControl = value;

				if (delta & 0xc0) {
					switch(value & 0xc0) {
						case 0x00:
							thisptr->SetCartBank(thisptr->mLeftPage);
							break;
						case 0x40:
							thisptr->SetCartBank(thisptr->mLeftPage + 0x100);
							break;
						case 0x80:
							thisptr->SetCartBank(thisptr->mLeftPage + 0x200);
							break;
						case 0xc0:
							thisptr->SetCartBank(-1);
							break;
					}
				}

				if (delta & 0x30) {
					switch(value & 0x30) {
						case 0x00:
							thisptr->SetCartBank2(thisptr->mRightPage);
							break;
						case 0x10:
							thisptr->SetCartBank2(thisptr->mRightPage + 0x100);
							break;
						case 0x20:
							thisptr->SetCartBank2(thisptr->mRightPage + 0x200);
							break;
						case 0x30:
							thisptr->SetCartBank2(-1);
							break;
					}
				}
			}
			return true;
	}

	return false;
}

sint32 ATMyIDEEmulator::DebugReadByte_Cart_V2(void *thisptr0, uint32 address) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	// A tricky part here: it's possible that both left and right banks are pointing to the same
	// bank and therefore we may have to remap BOTH banks on a flash state change. Fortunately,
	// we don't have to worry about the keyhole as we always use memory routines for that. We
	// are a bit lazy here and just turn off the read layer for the window that was hit, and turn
	// of the other window if/when that one gets hit too.

	uint8 data;

	if (address < 0xA000) {
		if (thisptr->mFlash.DebugReadByte(address - 0x8000 + (thisptr->mCartBank2 << 13), data)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPURead, false);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_AnticRead, false);
		}
	} else {
		if (thisptr->mFlash.DebugReadByte(address - 0xA000 + (thisptr->mCartBank << 13), data)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_CPURead, false);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_AnticRead, false);
		}
	}

	return data;
}

sint32 ATMyIDEEmulator::ReadByte_Cart_V2(void *thisptr0, uint32 address) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	// A tricky part here: it's possible that both left and right banks are pointing to the same
	// bank and therefore we may have to remap BOTH banks on a flash state change. Fortunately,
	// we don't have to worry about the keyhole as we always use memory routines for that. We
	// are a bit lazy here and just turn off the read layer for the window that was hit, and turn
	// of the other window if/when that one gets hit too.

	uint8 data;

	if (address < 0xA000) {
		if (thisptr->mFlash.ReadByte(address - 0x8000 + (thisptr->mCartBank2 << 13), data)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPURead, false);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_AnticRead, false);
		}
	} else {
		if (thisptr->mFlash.ReadByte(address - 0xA000 + (thisptr->mCartBank << 13), data)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_CPURead, false);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_AnticRead, false);
		}
	}

	return data;
}

bool ATMyIDEEmulator::WriteByte_Cart_V2(void *thisptr0, uint32 address, uint8 value) {
	ATMyIDEEmulator *thisptr = (ATMyIDEEmulator *)thisptr0;

	// A tricky part here: it's possible that both left and right banks are pointing to the same
	// bank and therefore we may have to remap BOTH banks on a flash state change. Fortunately,
	// we don't have to worry about the keyhole as we always use memory routines for that. Unlike
	// the read routine, we cannot ignore it here as it may involve turning ON the read mapping.

	bool remap;

	if (address < 0xA000)
		remap = thisptr->mFlash.WriteByte(address - 0x8000 + (thisptr->mCartBank2 << 13), value);
	else
		remap = thisptr->mFlash.WriteByte(address - 0xA000 + (thisptr->mCartBank << 13), value);

	if (thisptr->mFlash.CheckForWriteActivity()) {
		if (thisptr->mpUIRenderer)
			thisptr->mpUIRenderer->SetFlashWriteActivity();

		thisptr->mbFirmwareUsable = true;
	}

	if (remap) {
		bool enabled = thisptr->mFlash.IsControlReadEnabled();

		if (!(thisptr->mCartBank & 0xf00)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPURead, enabled);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerLeftCartFlash, kATMemoryAccessMode_AnticRead, enabled);
		}

		if (!(thisptr->mCartBank2 & 0xf00)) {
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_CPURead, enabled);
			thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRightCartFlash, kATMemoryAccessMode_AnticRead, enabled);
		}
	}

	return true;
}

void ATMyIDEEmulator::UpdateIDEReset() {
	for(size_t i=0; i<2; ++i)
		mIDE[i].SetReset(!mbCFPower || mbCFReset || !mpBlockDevices[i]);
}

void ATMyIDEEmulator::SetCartBank(int bank) {
	if (mCartBank == bank)
		return;

	mCartBank = bank;
	UpdateCartBank();
}

void ATMyIDEEmulator::SetCartBank2(int bank) {
	if (mCartBank2 == bank)
		return;

	mCartBank2 = bank;
	UpdateCartBank2();
}

void ATMyIDEEmulator::UpdateCartBank() {
	// Note that we need to tell the cartridge port when we want to
	// activate the left window, and not if it is actually active. This should
	// NOT include the enable that the cartridge port pushes down to us.
	mpCartridgePort->OnLeftWindowChanged(mCartId, IsLeftCartActive());

	if (!mbLeftWindowEnabled || mCartBank < 0) {
		mpMemMan->EnableLayer(mpMemLayerLeftCart, false);
		mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, false);
		return;
	}

	const bool flashControlRead = !(mCartBank & 0xf00) && mFlash.IsControlReadEnabled();
	mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPURead, flashControlRead);
	mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_AnticRead, flashControlRead);

	bool enabled = true;
	switch(mCartBank & 0xf00) {
		case 0x000:
			enabled = true;
			mpMemMan->SetLayerMemory(mpMemLayerLeftCart, mFirmware + (mCartBank << 13), 0xA0, 0x20, (uint32)-1, true);
			mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPUWrite, true);
			break;

		case 0x100:
			enabled = true;
			mpMemMan->SetLayerMemory(mpMemLayerLeftCart, mRAM + ((mCartBank - 0x100) << 13), 0xA0, 0x20, (uint32)-1, false);
			mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;

		case 0x200:
			enabled = true;
			mpMemMan->SetLayerMemory(mpMemLayerLeftCart, mRAM + ((mCartBank - 0x200) << 13), 0xA0, 0x20, (uint32)-1, true);
			mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;

		default:
			enabled = false;
			mpMemMan->EnableLayer(mpMemLayerLeftCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;
	}
	
	mpMemMan->EnableLayer(mpMemLayerLeftCart, enabled);
}

void ATMyIDEEmulator::UpdateCartBank2() {
	if (!mbRightWindowEnabled || mCartBank2 < 0) {
		mpMemMan->EnableLayer(mpMemLayerRightCart, false);
		mpMemMan->EnableLayer(mpMemLayerRightCartFlash, false);
		return;
	}

	const bool flashControlRead = !(mCartBank2 & 0xf00) && mFlash.IsControlReadEnabled();
	mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_CPURead, flashControlRead);
	mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_AnticRead, flashControlRead);

	switch(mCartBank2 & 0xf00) {
		case 0x000:
			mpMemMan->SetLayerMemory(mpMemLayerRightCart, mFirmware + (mCartBank2 << 13), 0x80, 0x20, (uint32)-1, true);
			mpMemMan->EnableLayer(mpMemLayerRightCart, true);
			mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_CPUWrite, true);
			break;

		case 0x100:
			mpMemMan->SetLayerMemory(mpMemLayerRightCart, mRAM + ((mCartBank2 - 0x100) << 13), 0x80, 0x20, (uint32)-1, false);
			mpMemMan->EnableLayer(mpMemLayerRightCart, true);
			mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;

		case 0x200:
			mpMemMan->SetLayerMemory(mpMemLayerRightCart, mRAM + ((mCartBank2 - 0x200) << 13), 0x80, 0x20, (uint32)-1, true);
			mpMemMan->EnableLayer(mpMemLayerRightCart, true);
			mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;

		default:
			mpMemMan->EnableLayer(mpMemLayerRightCart, false);
			mpMemMan->EnableLayer(mpMemLayerRightCartFlash, kATMemoryAccessMode_CPUWrite, false);
			break;
	}
}
