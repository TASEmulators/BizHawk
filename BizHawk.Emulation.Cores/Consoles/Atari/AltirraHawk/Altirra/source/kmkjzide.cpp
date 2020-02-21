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

///////////////////////////////////////////////////////////////////////////
//
// KMK/JZ IDE / IDE+2.0 emulation
//
// Known hardware differences:
//
//	IDE+2 rev. D
//		ID change and write protect switches
//
//	IDE+2 rev. S
//		Rev. D with Covox added
//
//	IDE+2 rev. E
//		Software ID change
//		Write protect, IRQ switches
//

#include <stdafx.h>
#include <vd2/system/bitmath.h>
#include <vd2/system/file.h>
#include <vd2/system/registry.h>
#include <vd2/system/hash.h>
#include <vd2/system/int128.h>
#include <at/atcore/blockdevice.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/propertyset.h>
#include "kmkjzide.h"
#include "memorymanager.h"
#include "ide.h"
#include "irqcontroller.h"
#include "uirender.h"
#include "firmwaremanager.h"

template<bool T_Ver2>
void ATCreateDeviceKMKJZIDE(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATKMKJZIDE> p(new ATKMKJZIDE(T_Ver2));

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefKMKJZIDE = { "kmkjzide", "kmkjzide", L"KMK/JZ IDE v1", ATCreateDeviceKMKJZIDE<false>, kATDeviceDefFlag_RebootOnPlug };
extern const ATDeviceDefinition g_ATDeviceDefKMKJZIDE2 = { "kmkjzide2", "kmkjzide2", L"KMK/JZ IDE v2", ATCreateDeviceKMKJZIDE<true>, kATDeviceDefFlag_RebootOnPlug };

ATKMKJZIDE::ATKMKJZIDE(bool version2)
	: mbVersion2(version2)
	, mRevision(version2 ? kRevision_V2_D : kRevision_V1)
{
	memset(mFlash, 0xFF, sizeof mFlash);
	memset(mSDX, 0xFF, sizeof mSDX);
	mRTC.Init();

	LoadNVRAM();
}

ATKMKJZIDE::~ATKMKJZIDE() {
	SaveNVRAM();

	Shutdown();
}

void *ATKMKJZIDE::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceFirmware::kTypeID:	return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceMemMap::kTypeID:		return static_cast<IATDeviceMemMap *>(this);
		case IATDevicePBIConnection::kTypeID:	return static_cast<IATDevicePBIConnection *>(this);
		case IATDeviceParent::kTypeID:		return static_cast<IATDeviceParent *>(this);
		case IATDeviceCartridge::kTypeID:	return static_cast<IATDeviceCartridge *>(this);
		case IATDeviceDiagnostics::kTypeID:	return static_cast<IATDeviceDiagnostics *>(this);
		case IATDeviceIRQSource::kTypeID:	return static_cast<IATDeviceIRQSource *>(this);
		case IATDeviceButtons::kTypeID:		return static_cast<IATDeviceButtons *>(this);
		case IATDeviceAudioOutput::kTypeID:	return static_cast<IATDeviceAudioOutput *>(this);
		case ATIDEEmulator::kTypeID:		return static_cast<ATIDEEmulator *>(&mIDE[0]);
	}

	return ATDevice::AsInterface(id);
}

void ATKMKJZIDE::GetSettingsBlurb(VDStringW& buf) {
	buf.sprintf(L"ID %u", (unsigned)VDFindLowestSetBit(mDeviceId));
}

void ATKMKJZIDE::GetSettings(ATPropertySet& settings) {
	if (mbVersion2) {
		settings.SetBool("enablesdx", mbSDXSwitchEnabled);

		const wchar_t *revstr;

		switch(mRevision) {
			default:
			case kRevision_V2_D:	revstr = L"d"; break;

			case kRevision_V2_C:	revstr = L"c"; break;
			case kRevision_V2_S:	revstr = L"s"; break;
			case kRevision_V2_E:	revstr = L"e"; break;
		}

		settings.SetString("revision", revstr);
		settings.SetBool("writeprotect", mbWriteProtect);
		settings.SetBool("nvramguard", mbNVRAMGuard);
	}

	settings.SetUint32("id", (uint32)VDFindLowestSetBit(mDeviceId));
}

bool ATKMKJZIDE::SetSettings(const ATPropertySet& settings) {
	if (mbVersion2) {
		mbSDXSwitchEnabled = settings.GetBool("enablesdx", true);

		const VDStringSpanW rev(settings.GetString("revision", L"d"));
		Revision newRevision;

		if (rev == L"e")
			newRevision = kRevision_V2_E;
		else if (rev == L"s")
			newRevision = kRevision_V2_S;
		else if (rev == L"c")
			newRevision = kRevision_V2_C;
		else
			newRevision = kRevision_V2_D;

		if (mRevision != newRevision) {
			mCovox.Shutdown();

			mRevision = newRevision;

			if (newRevision == kRevision_V2_S && mpAudioMixer)
				mCovox.Init(nullptr, mpScheduler, mpAudioMixer);
		}

		mbWriteProtect = settings.GetBool("writeprotect", false);
		mbNVRAMGuard = settings.GetBool("nvramguard", true);
	}

	uint32 id = settings.GetUint32("id", 0);

	if (id < 8) {
		const uint8 deviceId = 1 << id;

		if (mDeviceId != deviceId) {
			if (mpPBIManager)
				mpPBIManager->RemoveDevice(this);

			mDeviceId = deviceId;

			if (mpPBIManager)
				mpPBIManager->AddDevice(this);
		}
	}

	return true;
}

void ATKMKJZIDE::Init() {
	if (mRevision == kRevision_V2_S)
		mCovox.Init(nullptr, mpScheduler, mpAudioMixer);

	for(size_t i=0; i<2; ++i)
		mIDE[i].Init(mpScheduler, mpUIRenderer, !mpBlockDevices[i ^ 1], i > 0);

	ReloadFirmware();
}

void ATKMKJZIDE::Shutdown() {
	if (mpCartPort) {
		mpCartPort->RemoveCartridge(mCartId, this);
		mpCartPort = nullptr;
	}

	if (mpMemMan) {
		if (mpMemLayerSDXControl) {
			mpMemMan->DeleteLayer(mpMemLayerSDXControl);
			mpMemLayerSDXControl = nullptr;
		}

		if (mpMemLayerSDX) {
			mpMemMan->DeleteLayer(mpMemLayerSDX);
			mpMemLayerSDX = nullptr;
		}

		if (mpMemLayerFlashControl) {
			mpMemMan->DeleteLayer(mpMemLayerFlashControl);
			mpMemLayerFlashControl = nullptr;
		}

		if (mpMemLayerControl) {
			mpMemMan->DeleteLayer(mpMemLayerControl);
			mpMemLayerControl = nullptr;
		}

		if (mpMemLayerFlash) {
			mpMemMan->DeleteLayer(mpMemLayerFlash);
			mpMemLayerFlash = nullptr;
		}

		if (mpMemLayerRAM) {
			mpMemMan->DeleteLayer(mpMemLayerRAM);
			mpMemLayerRAM = nullptr;
		}

		mpMemMan = nullptr;
	}

	if (mpIrqController) {
		mpIrqController->FreeIRQ(mIrq);
		mpIrqController = nullptr;
	}

	if (mpPBIManager) {
		mpPBIManager->RemoveDevice(this);
		mpPBIManager = nullptr;
	}

	for(auto& blockDev : mpBlockDevices) {
		if (blockDev) {
			vdpoly_cast<IATDevice *>(blockDev)->SetParent(nullptr, 0);
			blockDev = nullptr;
		}
	}

	mpUIRenderer = nullptr;
	mpScheduler = nullptr;

	mFlashCtrl.Shutdown();
	mSDXCtrl.Shutdown();

	mCovox.Shutdown();
}

void ATKMKJZIDE::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = mbVersion2 ? &g_ATDeviceDefKMKJZIDE2 : &g_ATDeviceDefKMKJZIDE;
}

void ATKMKJZIDE::ColdReset() {
	memset(mRAM, 0xFF, sizeof mRAM);

	if (mbNVRAMGuard)
		mRTC.Restore();

	mFlashCtrl.ColdReset();
	mSDXCtrl.ColdReset();

	for(auto& ide : mIDE)
		ide.ColdReset();

	// SDX control register ($D1FE) is not cleared by warm reset.
	mpMemMan->SetLayerMemory(mpMemLayerSDX, mSDX);
	mSDXBankOffset = 0 - 0xA000;
	mbSDXEnabled = mbSDXSwitchEnabled;
	mbExternalEnabled = true;

	if (mpCartPort) {
		mpCartPort->OnLeftWindowChanged(mCartId, mbSDXEnabled);
		UpdateCartPassThrough();
	}

	UpdateMemoryLayersSDX();

	WarmReset();

	mbIDESlaveSelected = false;
}

void ATKMKJZIDE::WarmReset() {
	mpMemMan->SetLayerMemory(mpMemLayerFlash, mFlash);
	mpMemMan->SetLayerMemory(mpMemLayerRAM, mRAM);

	mFlashBankOffset = 0 - 0xD800;

	UpdateMemoryLayersFlash();

	mbIrqEnabled = false;
	mbIrqActive = false;

	if (mpIrqController)
		mpIrqController->Negate(mIrq, false);

	if (mRevision == kRevision_V2_S)
		mCovox.WarmReset();
}

void ATKMKJZIDE::LoadNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	ATRTCV3021Emulator::NVState state;
	memset(state.mData, 0xFF, sizeof state.mData);

	if (key.getBinary("IDEPlus clock", (char *)state.mData, (int)sizeof state.mData))
		mRTC.Load(state);
}

void ATKMKJZIDE::SaveNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	ATRTCV3021Emulator::NVState state {};
	mRTC.Save(state, mbNVRAMGuard);

	key.setBinary("IDEPlus clock", (const char *)state.mData, (int)sizeof state.mData);
}

void ATKMKJZIDE::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;

	mFlashCtrl.Init(mFlash, kATFlashType_Am29F010, sch);
	mSDXCtrl.Init(mSDX, kATFlashType_Am29F040B, sch);
}

void ATKMKJZIDE::InitIndicators(IATDeviceIndicatorManager *indicators) {
	mpUIRenderer = indicators;
}

void ATKMKJZIDE::InitFirmware(ATFirmwareManager *fwman) {
	mpFwManager = fwman;
}

bool ATKMKJZIDE::ReloadFirmware() {
	bool changed = false;

	for(int i=0; i<2; ++i) {
		const bool sdx = (i == 1);
		uint64 id;

		if (mbVersion2) {
			id = mpFwManager->GetCompatibleFirmware(sdx ? kATFirmwareType_KMKJZIDE2_SDX : kATFirmwareType_KMKJZIDE2);
		} else {
			if (sdx)
				break;

			id = mpFwManager->GetCompatibleFirmware(kATFirmwareType_KMKJZIDE);
		}

		void *flash;
		uint32 flashSize;

		if (sdx) {
			flash = mSDX;
			flashSize = sizeof mSDX;
			mSDXCtrl.SetDirty(false);
		} else {
			flash = mFlash;
			flashSize = mbVersion2 ? sizeof mFlash : 0xc00;
			mFlashCtrl.SetDirty(false);
		}

		vduint128 oldHash = VDHash128(flash, flashSize);
		memset(flash, 0xFF, flashSize);

		mpFwManager->LoadFirmware(id, flash, 0, flashSize, nullptr, nullptr, nullptr, nullptr, &mbFirmwareUsable);
		if (oldHash != VDHash128(flash, flashSize))
			changed = true;
	}

	return changed;
}

const wchar_t *ATKMKJZIDE::GetWritableFirmwareDesc(uint32 idx) const {
	switch(idx) {
		case 0:
			return L"Main";

		case 1:
			return L"SDX";

		default:
			return nullptr;
	}
}

bool ATKMKJZIDE::IsWritableFirmwareDirty(uint32 idx) const {
	switch(idx) {
		case 0:
			return mFlashCtrl.IsDirty();

		case 1:
			return mSDXCtrl.IsDirty();

		default:
			return false;
	}
}

void ATKMKJZIDE::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
	if (idx >= 2)
		return;

	const bool sdx = (idx == 1);

	void *flash;
	uint32 flashSize;

	if (sdx) {
		flash = mSDX;
		flashSize = sizeof mSDX;
	} else {
		flash = mFlash;
		flashSize = mbVersion2 ? sizeof mFlash : 0xc00;
	}

	stream.Write(flash, flashSize);

	if (sdx)
		mSDXCtrl.SetDirty(false);
	else
		mFlashCtrl.SetDirty(false);
}

ATDeviceFirmwareStatus ATKMKJZIDE::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATKMKJZIDE::InitMemMap(ATMemoryManager *memman) {
	mpMemMan = memman;

	ATMemoryHandlerTable handlers = {};
	handlers.mpThis = this;
	handlers.mbPassReads = true;
	handlers.mbPassWrites = true;
	handlers.mbPassAnticReads = true;
	handlers.mpDebugReadHandler = OnControlDebugRead;
	handlers.mpReadHandler = OnControlRead;
	handlers.mpWriteHandler = OnControlWrite;

	mpMemLayerControl = mpMemMan->CreateLayer(kATMemoryPri_PBI, handlers, 0xD1, 0x01);
	mpMemMan->SetLayerName(mpMemLayerControl, "IDEPlus control");
	mpMemMan->EnableLayer(mpMemLayerControl, true);

	mpMemLayerFlash = mpMemMan->CreateLayer(kATMemoryPri_PBI, mFlash, 0xD8, 0x06, true);
	mpMemMan->SetLayerName(mpMemLayerFlash, "IDEPlus flash");

	handlers.mbPassReads = false;
	handlers.mbPassWrites = false;
	handlers.mbPassAnticReads = false;
	handlers.mpDebugReadHandler = OnFlashDebugRead;
	handlers.mpReadHandler = OnFlashRead;
	handlers.mpWriteHandler = OnFlashWrite;

	mpMemLayerFlashControl = mpMemMan->CreateLayer(kATMemoryPri_PBI, handlers, 0xD8, 0x06);
	mpMemMan->SetLayerName(mpMemLayerFlashControl, "IDEPlus flash control");

	mpMemLayerRAM = mpMemMan->CreateLayer(kATMemoryPri_PBI, mRAM, 0xDE, 0x02, false);
	mpMemMan->SetLayerName(mpMemLayerRAM, "IDEPlus RAM");

	mpMemLayerSDX = mpMemMan->CreateLayer(kATMemoryPri_Extsel, mSDX, 0xA0, 0x20, true);
	mpMemMan->SetLayerName(mpMemLayerSDX, "IDEPlus SDX");

	handlers.mpDebugReadHandler = OnSDXDebugRead;
	handlers.mpReadHandler = OnSDXRead;
	handlers.mpWriteHandler = OnSDXWrite;

	mpMemLayerSDXControl = mpMemMan->CreateLayer(kATMemoryPri_Extsel+1, handlers, 0xA0, 0x20);
	mpMemMan->SetLayerName(mpMemLayerSDXControl, "IDEPlus SDX control");

	ColdReset();
}

bool ATKMKJZIDE::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD100;
		hi = 0xD1FF;
		return true;
	}

	return false;
}

void ATKMKJZIDE::InitPBI(IATDevicePBIManager *pbi) {
	mpPBIManager = pbi;
	mpPBIManager->AddDevice(this);
	mbSelected = false;
}

void ATKMKJZIDE::GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const {
	devInfo.mDeviceId = mDeviceId;
	devInfo.mbHasIrq = true;
}

void ATKMKJZIDE::SelectPBIDevice(bool enable) {
	mbSelected = enable;

	if (mpMemMan) {
		mpMemMan->EnableLayer(mpMemLayerFlash, enable);
		mpMemMan->EnableLayer(mpMemLayerFlashControl, kATMemoryAccessMode_CPUWrite, enable);
		mpMemMan->EnableLayer(mpMemLayerRAM, enable);
	}
}

bool ATKMKJZIDE::IsPBIOverlayActive() const {
	return mbSelected;
}

uint8 ATKMKJZIDE::ReadPBIStatus(uint8 busData, bool debugOnly) {
	if (mbIrqActive)
		busData |= mDeviceId;
	else
		busData &= ~mDeviceId;

	return busData;
}

const wchar_t *ATKMKJZIDE::GetBusName() const {
	return L"IDE Bus";
}

const char *ATKMKJZIDE::GetBusTag() const {
	return "idebus";
}

const char *ATKMKJZIDE::GetSupportedType(uint32 index) {
	if (index == 0)
		return "harddisk";

	return nullptr;
}

IATDeviceBus *ATKMKJZIDE::GetDeviceBus(uint32 index) {
	return index ? nullptr : this;
}

void ATKMKJZIDE::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	for(const auto& blockDev : mpBlockDevices) {
		auto *cdev = vdpoly_cast<IATDevice *>(&*blockDev);

		if (cdev)
			devs.push_back(cdev);
	}
}

void ATKMKJZIDE::AddChildDevice(IATDevice *dev) {
	for(size_t i=0; i<vdcountof(mpBlockDevices); ++i) {
		if (mpBlockDevices[i])
			continue;

		IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

		if (blockDevice) {
			mpBlockDevices[i] = blockDevice;
			dev->SetParent(this, 0);

			mIDE[i].OpenImage(blockDevice);
			mIDE[i^1].SetIsSingle(false);
			break;
		}
	}
}

void ATKMKJZIDE::RemoveChildDevice(IATDevice *dev) {
	IATBlockDevice *blockDevice = vdpoly_cast<IATBlockDevice *>(dev);

	if (!blockDevice)
		return;

	for(size_t i=0; i<vdcountof(mpBlockDevices); ++i) {
		if (mpBlockDevices[i] == blockDevice) {
			mIDE[i].CloseImage();
			mIDE[i^1].SetIsSingle(true);
			dev->SetParent(nullptr, 0);
			mpBlockDevices[i] = nullptr;
			break;
		}
	}
}

void ATKMKJZIDE::InitCartridge(IATDeviceCartridgePort *cartPort) {
	if (mbVersion2) {
		mpCartPort = cartPort;
		cartPort->AddCartridge(this, kATCartridgePriority_PBI, mCartId);
	}
}

bool ATKMKJZIDE::IsLeftCartActive() const {
	return mbSDXEnabled;
}

void ATKMKJZIDE::SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) {
	if (mbSDXUpstreamEnabled != leftEnable) {
		mbSDXUpstreamEnabled = leftEnable;

		UpdateMemoryLayersSDX();
	}
}

void ATKMKJZIDE::UpdateCartSense(bool leftActive) {
}

void ATKMKJZIDE::DumpStatus(ATConsoleOutput& output) {
	ATRTCV3021Emulator::NVState nvstate;
	ATRTCV3021Emulator::NVState nvstate2;
	mRTC.Save(nvstate, false);
	mRTC.Save(nvstate2, true);

	output("KMK/JZ IDE v%c status:", mbVersion2 ? '2' : '1');

	output("  NVRAM (current):         %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X"
		, nvstate.mData[0]
		, nvstate.mData[1]
		, nvstate.mData[2]
		, nvstate.mData[3]
		, nvstate.mData[4]
		, nvstate.mData[5]
		, nvstate.mData[6]
		, nvstate.mData[7]
		, nvstate.mData[8]
		, nvstate.mData[9]);

	output("  NVRAM (last user data):  %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X"
		, nvstate2.mData[0]
		, nvstate2.mData[1]
		, nvstate2.mData[2]
		, nvstate2.mData[3]
		, nvstate2.mData[4]
		, nvstate2.mData[5]
		, nvstate2.mData[6]
		, nvstate2.mData[7]
		, nvstate2.mData[8]
		, nvstate2.mData[9]);

	output("  SDX enabled:           %s%s", mbSDXEnabled ? "yes" : "no", mbSDXUpstreamEnabled ? "" : " (disabled by upstream cart)");
	output("  External cart enabled: %s", mbExternalEnabled ? "yes" : "no");
	output("  IRQ status:            %s (%s)", mbIrqEnabled ? "enabled" : "disabled", mbIrqActive ? "asserted" : "negated");
}

void ATKMKJZIDE::InitIRQSource(ATIRQController *irqc) {
	if (mbVersion2) {
		mpIrqController = irqc;
		mIrq = mpIrqController->AllocateIRQ();
	}
}

uint32 ATKMKJZIDE::GetSupportedButtons() const {
	switch(mRevision) {
		case kRevision_V2_D:
		case kRevision_V2_E:
			return (1U << kATDeviceButton_IDEPlus2SwitchDisks) | (1U << kATDeviceButton_IDEPlus2WriteProtect) | (1U << kATDeviceButton_IDEPlus2SDX);

		default:
			return 0;
	}
}

bool ATKMKJZIDE::IsButtonDepressed(ATDeviceButton idx) const {
	switch(mRevision) {
		case kRevision_V2_D:
		case kRevision_V2_E:
			break;

		default:
			return false;
	}

	switch(idx) {
		case kATDeviceButton_IDEPlus2WriteProtect:
			return mbWriteProtect;

		case kATDeviceButton_IDEPlus2SDX:
			return mbSDXSwitchEnabled;

		default:
			return false;
	}
}

void ATKMKJZIDE::ActivateButton(ATDeviceButton idx, bool state) {
	switch(mRevision) {
		case kRevision_V2_D:
		case kRevision_V2_E:
			break;

		default:
			return;
	}

	if (idx == kATDeviceButton_IDEPlus2SwitchDisks && state) {
		if (mbIrqEnabled && !mbIrqActive) {
			mbIrqActive = true;
			mpIrqController->Assert(mIrq, false);
		}
	} else if (idx == kATDeviceButton_IDEPlus2WriteProtect) {
		mbWriteProtect = !mbWriteProtect;
	} else if (idx == kATDeviceButton_IDEPlus2SDX) {
		mbSDXSwitchEnabled = !mbSDXSwitchEnabled;
	}
}

void ATKMKJZIDE::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

sint32 ATKMKJZIDE::OnControlDebugRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	if (!thisptr->mbSelected)
		return -1;

	uint8 addr8 = (uint8)addr;

	switch(addr8) {
		case 0x00:
		case 0x01:
		case 0x02:
		case 0x03:
		case 0x04:
		case 0x05:
		case 0x06:
		case 0x07:
		case 0x08:
		case 0x09:
		case 0x0A:
		case 0x0B:
		case 0x0C:
		case 0x0D:
		case 0x0E:
		case 0x0F:
			return thisptr->mHighDataLatch;

		case 0x10:
		case 0x11:
		case 0x12:
		case 0x13:
		case 0x14:
		case 0x15:
		case 0x16:
		case 0x17:
			return thisptr->mIDE[thisptr->mbIDESlaveSelected && thisptr->mpBlockDevices[1] ? 1 : 0].DebugReadByte(addr8 & 0x07);

		case 0x1E:	// alternate status register
			return thisptr->mIDE[thisptr->mbIDESlaveSelected && thisptr->mpBlockDevices[1] ? 1 : 0].DebugReadByte(0x07);

		case 0x20:
			return thisptr->mRTC.DebugReadBit() ? 0xFF : 0x7F;

		case 0xF8:	// (rev.E) IRQ enable status register (ID bit)
			if (thisptr->mRevision == kRevision_V2_E)
				return thisptr->mbIrqEnabled ? 0xFF : 0xFF ^ thisptr->mDeviceId;
			break;

		case 0xFA:	// (rev.E) Write protect register (bit 4)
			if (thisptr->mRevision == kRevision_V2_E)
				return thisptr->mbWriteProtect ? 0xFF : 0xEF;

			break;

		case 0xFC:	// (rev.D) Write protect register (ID bit)
			if (thisptr->mRevision == kRevision_V2_D)
				return thisptr->mbWriteProtect ? 0xFF ^ thisptr->mDeviceId : 0xFF;

			break;

		case 0xFD:	// (rev.D) IRQ enable status register (ID bit)
			if (thisptr->mRevision == kRevision_V2_D)
				return thisptr->mbIrqEnabled ? 0xFF : 0xFF ^ thisptr->mDeviceId;
			break;

		case 0xFF:
			return -1;		// This must be handled by the PBI manager.
	}

	return 0xFF;
}

sint32 ATKMKJZIDE::OnControlRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;
	if (!thisptr->mbSelected)
		return -1;

	uint8 addr8 = (uint8)addr;

	switch(addr8) {
		case 0x10:
			{
				uint32 v = thisptr->mIDE[thisptr->mbIDESlaveSelected && thisptr->mpBlockDevices[1] ? 1 : 0].ReadDataLatch(true);

				thisptr->mHighDataLatch = (uint8)(v >> 8);
				return (uint8)v;
			}

		case 0x11:
		case 0x12:
		case 0x13:
		case 0x14:
		case 0x15:
		case 0x16:
		case 0x17:
			return thisptr->mIDE[thisptr->mbIDESlaveSelected && thisptr->mpBlockDevices[1] ? 1 : 0].ReadByte(addr8 & 0x07);

		case 0x1E:	// alternate status register
			return thisptr->mIDE[thisptr->mbIDESlaveSelected && thisptr->mpBlockDevices[1] ? 1 : 0].ReadByte(0x07);

		case 0x20:
			return thisptr->mRTC.ReadBit() ? 0xFF : 0x7F;
	}

	// Defer to debug read handler for all registers without side effects.
	return OnControlDebugRead(thisptr0, addr);
}

bool ATKMKJZIDE::OnControlWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;
	if (!thisptr->mbSelected) {
		
		if (addr != 0xD1FE && (addr != 0xD1FB || thisptr->mRevision != kRevision_V2_S))
			return false;
	}

	uint8 addr8 = (uint8)addr;

	switch(addr8) {
		case 0x00:
		case 0x01:
		case 0x02:
		case 0x03:
		case 0x04:
		case 0x05:
		case 0x06:
		case 0x07:
		case 0x08:
		case 0x09:
		case 0x0A:
		case 0x0B:
		case 0x0C:
		case 0x0D:
		case 0x0E:
		case 0x0F:
			thisptr->mHighDataLatch = value;
			return true;

		case 0x10:
			for(size_t i=0; i<2; ++i)
				thisptr->mIDE[i].WriteDataLatch(value, thisptr->mHighDataLatch);

			return true;

		case 0x16:
			// For the device/head select register, we also need to capture the master/slave
			// select. This is normally done by the drives themselves, but we do it here for
			// convenience.
			thisptr->mbIDESlaveSelected = (value & 0x10) != 0;

			// fall through
		case 0x11:
		case 0x12:
		case 0x13:
		case 0x14:
		case 0x15:
		case 0x17:
			// Both devices receive writes to all registers. This includes the command register,
			// although for most commands only one drive will act (diagnostic being an exception).
			for(size_t i=0; i<2; ++i)
				thisptr->mIDE[i].WriteByte(addr8 & 0x07, value);

			return true;

		case 0x20:
			thisptr->mRTC.WriteBit((value & 0x80) != 0);
			return true;

		case 0xA0:
			if (!thisptr->mbVersion2) {
				thisptr->mFlashBankOffset = 0x600 - 0xD800;

				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerFlash, thisptr->mFlash + 0x600);
				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerRAM, thisptr->mRAM + 0x200);
			}
			return true;

		case 0xC0:
			if (!thisptr->mbVersion2) {
				thisptr->mFlashBankOffset = 0 - 0xD800;

				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerFlash, thisptr->mFlash);
				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerRAM, thisptr->mRAM);
			}
			return true;

		case 0xF8:	// IRQ enable register (rev.D/E)
			if (thisptr->mRevision == kRevision_V2_D || thisptr->mRevision == kRevision_V2_E) {
				// D0=1 enables IRQ for device ID 0.
				bool enable = (value & thisptr->mDeviceId) != 0;

				if (thisptr->mbIrqEnabled != enable) {
					thisptr->mbIrqEnabled = enable;

					if (!enable && thisptr->mbIrqActive) {
						thisptr->mbIrqActive = false;
						thisptr->mpIrqController->Negate(thisptr->mIrq, true);
					}
				}
			}
			return true;

		case 0xFB:	// Covox (rev. S only)
			if (thisptr->mRevision == kRevision_V2_S) {
				thisptr->mCovox.WriteMono(value);
			}
			return true;

		case 0xFC:
			if (thisptr->mbVersion2) {
				uint32 offset = ((uint32)(value & 0x3f) << 11);

				thisptr->mFlashBankOffset = offset - 0xD800;

				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerFlash, thisptr->mFlash + offset);
			}
			return true;

		case 0xFD:
			if (thisptr->mbVersion2) {
				uint32 offset = ((uint32)(value & 0x3f) << 9);

				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerRAM, thisptr->mRAM + offset);
			}
			return true;

		case 0xFE:
			if (thisptr->mbVersion2) {
				uint32 offset = ((uint32)(value & 0x3f) << 13);

				thisptr->mSDXBankOffset = offset - 0xA000;

				thisptr->mpMemMan->SetLayerMemory(thisptr->mpMemLayerSDX, thisptr->mSDX + offset);

				bool sdxEnabled = !(value & 0x80);
				if (thisptr->mbSDXEnabled != sdxEnabled) {
					thisptr->mbSDXEnabled = sdxEnabled;

					if (thisptr->mpCartPort)
						thisptr->mpCartPort->OnLeftWindowChanged(thisptr->mCartId, sdxEnabled);
				}

				bool extEnabled = (value & 0x81) == 0x80;
				if (thisptr->mbExternalEnabled != extEnabled) {
					thisptr->mbExternalEnabled = extEnabled;

					thisptr->UpdateCartPassThrough();
				}

				thisptr->UpdateMemoryLayersSDX();
			}
			return true;
	}

	return true;
}

sint32 ATKMKJZIDE::OnFlashDebugRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.DebugReadByte(thisptr->mFlashBankOffset + addr, value))
		thisptr->UpdateMemoryLayersFlash();

	return value;
}

sint32 ATKMKJZIDE::OnFlashRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	uint8 value;
	if (thisptr->mFlashCtrl.ReadByte(thisptr->mFlashBankOffset + addr, value))
		thisptr->UpdateMemoryLayersFlash();

	return value;
}

bool ATKMKJZIDE::OnFlashWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	if (thisptr->mFlashCtrl.WriteByte(thisptr->mFlashBankOffset + addr, value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mFlashCtrl.CheckForWriteActivity()) {
				thisptr->mpUIRenderer->SetFlashWriteActivity();
				thisptr->mbFirmwareUsable = true;
			}
		}

		thisptr->UpdateMemoryLayersFlash();
	}

	return true;
}

sint32 ATKMKJZIDE::OnSDXDebugRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	uint8 value;
	if (thisptr->mSDXCtrl.DebugReadByte(thisptr->mSDXBankOffset + addr, value)) {
		thisptr->UpdateMemoryLayersSDX();
	}

	return value;
}

sint32 ATKMKJZIDE::OnSDXRead(void *thisptr0, uint32 addr) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	uint8 value;
	if (thisptr->mSDXCtrl.ReadByte(thisptr->mSDXBankOffset + addr, value)) {
		thisptr->UpdateMemoryLayersSDX();
	}

	return value;
}

bool ATKMKJZIDE::OnSDXWrite(void *thisptr0, uint32 addr, uint8 value) {
	ATKMKJZIDE *thisptr = (ATKMKJZIDE *)thisptr0;

	if (thisptr->mSDXCtrl.WriteByte(thisptr->mSDXBankOffset + addr, value)) {
		if (thisptr->mpUIRenderer) {
			if (thisptr->mSDXCtrl.CheckForWriteActivity())
				thisptr->mpUIRenderer->SetFlashWriteActivity();
		}

		thisptr->UpdateMemoryLayersSDX();
	}

	return true;
}

void ATKMKJZIDE::UpdateMemoryLayersFlash() {
	const bool controlRead = mFlashCtrl.IsControlReadEnabled();

	mpMemMan->EnableLayer(mpMemLayerFlashControl, kATMemoryAccessMode_AnticRead, controlRead);
	mpMemMan->EnableLayer(mpMemLayerFlashControl, kATMemoryAccessMode_CPURead, controlRead);
}

void ATKMKJZIDE::UpdateMemoryLayersSDX() {
	const bool sdxEnabled = mbVersion2 && mbSDXEnabled && mbSDXUpstreamEnabled;
	const bool controlRead = sdxEnabled && mSDXCtrl.IsControlReadEnabled();

	mpMemMan->EnableLayer(mpMemLayerSDXControl, kATMemoryAccessMode_AnticRead, controlRead);
	mpMemMan->EnableLayer(mpMemLayerSDXControl, kATMemoryAccessMode_CPURead, controlRead);
	mpMemMan->EnableLayer(mpMemLayerSDXControl, kATMemoryAccessMode_CPUWrite, sdxEnabled);
	mpMemMan->EnableLayer(mpMemLayerSDX, sdxEnabled);

	UpdateCartPassThrough();
}

void ATKMKJZIDE::UpdateCartPassThrough() {
	if (mpCartPort) {
		const bool cartEnabled = mbExternalEnabled && !mbSDXEnabled;
		mpCartPort->EnablePassThrough(mCartId, cartEnabled, cartEnabled, cartEnabled);
	}
}
