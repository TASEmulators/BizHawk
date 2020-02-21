//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#include "stdafx.h"
#include <vd2/system/file.h>
#include <vd2/system/registry.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/deviceindicators.h>
#include <at/atcore/logging.h>
#include "firmwaremanager.h"
#include "memorymanager.h"
#include "rapidus.h"
#include "cpu.h"

extern ATLogChannel g_ATLCEEPROMRead;
extern ATLogChannel g_ATLCEEPROMWrite;

void ATCreateDeviceRapidus(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefRapidus = { "rapidus", nullptr, L"Rapidus", ATCreateDeviceRapidus, kATDeviceDefFlag_RebootOnPlug };

///////////////////////////////////////////////////////////////////////////

ATRapidusDevice::ATRapidusDevice() {
}

void *ATRapidusDevice::AsInterface(uint32 iid) {
	switch(iid) {
		case ATRapidusDevice::kTypeID: return this;
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceMemMap::kTypeID: return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceFirmware::kTypeID: return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
		case IATDevicePBIConnection::kTypeID: return static_cast<IATDevicePBIConnection *>(this);
		case IATDeviceSystemControl::kTypeID: return static_cast<IATDeviceSystemControl *>(this);
		case IATDeviceDiagnostics::kTypeID: return static_cast<IATDeviceDiagnostics *>(this);
	}

	return ATDevice::AsInterface(iid);
}

void ATRapidusDevice::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefRapidus;
}

void ATRapidusDevice::Init() {
	ReloadFirmware();
	mFlashEmu.Init(mFlash, kATFlashType_SST39SF040, mpScheduler);

	LoadNVRAM();

	static constexpr struct SRAMWindow {
		uint32 mPageStart;
		uint32 mPageCount;
		const char *mpName;
	} kSRAMWindows[]={
		{ 0x00, 0x40,	"Rapidus $0000-3FFF SRAM window" },
		{ 0x40, 0x40,	"Rapidus $4000-7FFF SRAM window" },
		{ 0x80, 0x40,	"Rapidus $8000-BFFF SRAM window" },
		{ 0xC0, 0x40,	"Rapidus $C000-FFFF SRAM window (1)" },
		{ 0xD8, 0x28,	"Rapidus $C000-FFFF SRAM window (2)" },
	};

	static_assert(vdcountof(kSRAMWindows) == vdcountof(mpLayerBank0RAM), "SRAM window array mismatch");

	for(uint32 i=0; i<(uint32)vdcountof(kSRAMWindows); ++i) {
		const SRAMWindow& sw = kSRAMWindows[i];

		ATMemoryLayer *layer = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay + 1, mSRAM + (sw.mPageStart << 8), sw.mPageStart, sw.mPageCount, false);
		mpMemMan->SetLayerFastBus(layer, true);
		mpMemMan->SetLayerName(layer, sw.mpName);
		mpMemMan->SetLayerTag(layer, this);

		mpLayerBank0RAM[i] = layer;

	}

	ATMemoryHandlerTable writeThroughHandlers {};
	writeThroughHandlers.mbPassWrites = true;
	writeThroughHandlers.mpThis = this;
	writeThroughHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->WriteThroughSRAM(address, data); };

	ATMemoryLayer *loShadowLayer = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay + 2, writeThroughHandlers, 0, 0x40);
	mpMemMan->SetLayerName(loShadowLayer, "Rapidus low SRAM write-through shadow");
	mpMemMan->SetLayerTag(loShadowLayer, this);
	mpLayerLoBank0RAMShadow = loShadowLayer;

	ATMemoryLayer *hiShadowLayer = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay + 2, writeThroughHandlers, 0x40, 0xC0);
	mpMemMan->SetLayerName(hiShadowLayer, "Rapidus high SRAM write-through shadow");
	mpMemMan->SetLayerTag(hiShadowLayer, this);
	mpLayerHiBank0RAMShadow = hiShadowLayer;

	mpLayerSRAM = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, mSRAM + 0x10000, 0x100, 0x700, false);
	mpMemMan->SetLayerFastBus(mpLayerSRAM, true);
	mpMemMan->SetLayerName(mpLayerSRAM, "Rapidus SRAM");
	mpMemMan->SetLayerModes(mpLayerSRAM, kATMemoryAccessMode_ARW);
	mpMemMan->SetLayerTag(mpLayerSRAM, this);

	mpLayerSDRAM = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, mSDRAM, 0x800, 0xE800, false);
	mpMemMan->SetLayerFastBus(mpLayerSDRAM, true);
	mpMemMan->SetLayerName(mpLayerSDRAM, "Rapidus SDRAM");
	mpMemMan->SetLayerModes(mpLayerSDRAM, kATMemoryAccessMode_ARW);
	mpMemMan->SetLayerTag(mpLayerSDRAM, this);

	mpLayerBankedSDRAM = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 2, mSDRAMBanks[0], 0x8000, 0x4000, false);
	mpMemMan->SetLayerFastBus(mpLayerBankedSDRAM, true);
	mpMemMan->SetLayerName(mpLayerBankedSDRAM, "Rapidus banked SDRAM window");
	mpMemMan->SetLayerModes(mpLayerBankedSDRAM, kATMemoryAccessMode_ARW);
	mpMemMan->SetLayerTag(mpLayerBankedSDRAM, this);

	// low memory flash window ($4000-7FFF)
	mpLayerLoFlash = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, mFlash, 0x40, 0x40, false);
	mpMemMan->SetLayerName(mpLayerLoFlash, "Rapidus low flash window");
	mpMemMan->SetLayerTag(mpLayerLoFlash, this);

	ATMemoryHandlerTable loFlashHandlers {};
	loFlashHandlers.mpThis = this;
	loFlashHandlers.mpDebugReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->DebugReadLoFlash(address); };
	loFlashHandlers.mpReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->ReadLoFlash(address); };
	loFlashHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->WriteLoFlash(address, data); };
	mpLayerLoFlashControl = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, loFlashHandlers, 0x40, 0x40);
	mpMemMan->SetLayerFastBus(mpLayerLoFlashControl, true);
	mpMemMan->SetLayerName(mpLayerLoFlashControl, "Rapidus low flash control");
	mpMemMan->SetLayerTag(mpLayerLoFlashControl, this);

	// high memory flash window ($F0:0000-F7:FFFF)
	mpLayerHiFlash = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, mFlash, 0xF000, 0x800, false);
	mpMemMan->SetLayerName(mpLayerHiFlash, "Rapidus high flash window");
	mpMemMan->SetLayerFastBus(mpLayerHiFlash, true);
	mpMemMan->SetLayerModes(mpLayerHiFlash, kATMemoryAccessMode_R);
	mpMemMan->SetLayerTag(mpLayerHiFlash, this);

	ATMemoryHandlerTable hiFlashHandlers {};
	hiFlashHandlers.mpThis = this;
	hiFlashHandlers.mpDebugReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->DebugReadHiFlash(address); };
	hiFlashHandlers.mpReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->ReadHiFlash(address); };
	hiFlashHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->WriteHiFlash(address, data); };
	mpLayerHiFlashControl = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM + 1, hiFlashHandlers, 0xF000, 0x800);
	mpMemMan->SetLayerFastBus(mpLayerHiFlashControl, true);
	mpMemMan->SetLayerName(mpLayerHiFlashControl, "Rapidus high flash control");
	mpMemMan->SetLayerModes(mpLayerHiFlashControl, kATMemoryAccessMode_W);
	mpMemMan->SetLayerTag(mpLayerHiFlashControl, this);

	// New PBI device (priority must be higher than 816 SRAM windows)
	mpLayerPBIFirmware = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay + 3, mFlash + 0x1D800, 0xD8, 0x08, true);
	mpMemMan->SetLayerFastBus(mpLayerPBIFirmware, true);
	mpMemMan->SetLayerName(mpLayerPBIFirmware, "Rapidus PBI firmware");
	mpMemMan->SetLayerTag(mpLayerPBIFirmware, this);

	// hardware protect ($D000-D7FF)
	ATMemoryHandlerTable hwProtectHandlers {};
	hwProtectHandlers.mpThis = this;
	hwProtectHandlers.mpDebugReadHandler = [](void *thisptr0, uint32 address) { return 0xFF; };
	hwProtectHandlers.mpReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->HwProtectRead(address); };
	hwProtectHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->HwProtectWrite(address, data); };
	mpLayerHwProtect = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay + 4, hwProtectHandlers, 0xD0, 0x08);
	mpMemMan->SetLayerFastBus(mpLayerHwProtect, true);
	mpMemMan->SetLayerName(mpLayerHwProtect, "Rapidus hardware protect");
	mpMemMan->SetLayerTag(mpLayerHwProtect, this);

	// low memory registers ($D1xx)
	ATMemoryHandlerTable loRegisterHandlers {};
	loRegisterHandlers.mbPassAnticReads = true;
	loRegisterHandlers.mbPassReads = true;
	loRegisterHandlers.mbPassWrites = true;
	loRegisterHandlers.mpThis = this;
	loRegisterHandlers.mpDebugReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->DebugReadLoRegs(address); };
	loRegisterHandlers.mpReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->ReadLoRegs(address); };
	loRegisterHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->WriteLoRegs(address, data); };
	mpLayerLoRegisters = mpMemMan->CreateLayer(kATMemoryPri_PBI, loRegisterHandlers, 0xD1, 0x01);
	mpMemMan->SetLayerFastBus(mpLayerLoRegisters, true);
	mpMemMan->SetLayerName(mpLayerLoRegisters, "Rapidus low registers");
	mpMemMan->SetLayerTag(mpLayerLoRegisters, this);

	// high memory registers ($FF:xxxx)
	ATMemoryHandlerTable hiRegisterHandlers {};
	hiRegisterHandlers.mpThis = this;
	hiRegisterHandlers.mpDebugReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->DebugReadHiRegs(address); };
	hiRegisterHandlers.mpReadHandler = [](void *thisptr0, uint32 address) { return ((ATRapidusDevice *)thisptr0)->ReadHiRegs(address); };
	hiRegisterHandlers.mpWriteHandler = [](void *thisptr0, uint32 address, uint8 data) { return ((ATRapidusDevice *)thisptr0)->WriteHiRegs(address, data); };
	mpLayerHiRegisters = mpMemMan->CreateLayer(kATMemoryPri_Hardware, hiRegisterHandlers, 0xFF00, 0x100);
	mpMemMan->SetLayerFastBus(mpLayerHiRegisters, true);
	mpMemMan->SetLayerName(mpLayerHiRegisters, "Rapidus high registers");
	mpMemMan->SetLayerTag(mpLayerHiRegisters, this);
	mpMemMan->SetLayerModes(mpLayerHiRegisters, kATMemoryAccessMode_RW);

	// hardware mirror ($FF:D000-FF:D7FF)
	ATMemoryHandlerTable hardwareMirrorHandlers {};
	hardwareMirrorHandlers.mpThis = this;
	hardwareMirrorHandlers.BindDebugReadHandler<ATRapidusDevice, &ATRapidusDevice::DebugReadHardware>();
	hardwareMirrorHandlers.BindReadHandler<ATRapidusDevice, &ATRapidusDevice::ReadHardware>();
	hardwareMirrorHandlers.BindWriteHandler<ATRapidusDevice, &ATRapidusDevice::WriteHardware>();
	mpLayerHardwareMirror = mpMemMan->CreateLayer(kATMemoryPri_Hardware + 1, hardwareMirrorHandlers, 0xFFD0, 0x08);
	mpMemMan->SetLayerName(mpLayerHardwareMirror, "Rapidus hardware mirror");
	mpMemMan->SetLayerModes(mpLayerHardwareMirror, kATMemoryAccessMode_RW);

	mpPBIManager->AddDevice(this);
	UpdateKernelROM();
}

void ATRapidusDevice::Shutdown() {
	mFlashEmu.Shutdown();

	if (mpScheduler) {
		mpScheduler = nullptr;

		SaveNVRAM();
	}

	if (mpPBIManager) {
		mpPBIManager->RemoveDevice(this);
		mpPBIManager = nullptr;
	}

	if (mpMemMan) {
		mpMemMan->DeleteLayerPtr(&mpLayerLoFlash);
		mpMemMan->DeleteLayerPtr(&mpLayerLoFlashControl);

		for(ATMemoryLayer *&p : mpLayerBank0RAM)
			mpMemMan->DeleteLayerPtr(&p);

		mpMemMan->DeleteLayerPtr(&mpLayerLoBank0RAMShadow);
		mpMemMan->DeleteLayerPtr(&mpLayerHiBank0RAMShadow);
		mpMemMan->DeleteLayerPtr(&mpLayerSRAM);
		mpMemMan->DeleteLayerPtr(&mpLayerSDRAM);
		mpMemMan->DeleteLayerPtr(&mpLayerBankedSDRAM);
		mpMemMan->DeleteLayerPtr(&mpLayerHiFlash);
		mpMemMan->DeleteLayerPtr(&mpLayerHiFlashControl);
		mpMemMan->DeleteLayerPtr(&mpLayerPBIFirmware);
		mpMemMan->DeleteLayerPtr(&mpLayerHwProtect);
		mpMemMan->DeleteLayerPtr(&mpLayerLoRegisters);
		mpMemMan->DeleteLayerPtr(&mpLayerHiRegisters);
		mpMemMan->DeleteLayerPtr(&mpLayerHardwareMirror);

		mpMemMan->SetWrapBankZeroEnabled(false);
		mpMemMan = nullptr;
	}

	mpFwMgr = nullptr;
}

void ATRapidusDevice::ColdReset() {
	// reset FPGA, force boot on 6502
	mFPGAConfigReg = 0x40;

	mMCR = 0xFF;
	m6502CR = 0;
	WarmReset();
}

void ATRapidusDevice::WarmReset() {
	mFPGABankReg = 0;
	mMCR |= 0x7F;		// need to preserve bit 7 or RapidOS can't warmstart
	mCMCR = 0;
	mSCR = 0xFF;
	mAR = 0;
	mHPCR = 0;
	mI2CDataReg = 0;
	mEEPROMAddress = 0;

	UpdatePBIFirmware();
	UpdateLoFlashWindow();
	UpdateSRAMWindows();
	UpdateSDRAMWindow();
	UpdateHMA();
	UpdateKernelROM();
	UpdateHardwareProtect();
	ResetCPU();
}

void ATRapidusDevice::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATRapidusDevice::InitMemMap(ATMemoryManager *memman) {
	mpMemMan = memman;
}

bool ATRapidusDevice::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = 0xD190;
		hi = 0xD1A0;
		return true;
	}

	return false;
}

void ATRapidusDevice::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMgr = fwman;
}

bool ATRapidusDevice::ReloadFirmware() {
	const uint8 fill = 0xFF;
	bool changed = false;

	mFlashEmu.SetDirty(false);

	bool flashUsable = false;
	bool pbiUsable = false;

	mpFwMgr->LoadFirmware(mpFwMgr->GetCompatibleFirmware(kATFirmwareType_RapidusFlash), mFlash, 0, sizeof mFlash, &changed, nullptr, nullptr, &fill, &flashUsable);
	mpFwMgr->LoadFirmware(mpFwMgr->GetCompatibleFirmware(kATFirmwareType_RapidusCorePBI), mPBIFirmware816, 0, sizeof mPBIFirmware816, &changed, nullptr, nullptr, &fill, &pbiUsable);

	mbFirmwareUsable = flashUsable && pbiUsable;

	return changed;
}

const wchar_t *ATRapidusDevice::GetWritableFirmwareDesc(uint32 idx) const {
	switch(idx) {
		case 3: return L"Rapidus Flash";
		case 4: return L"Rapidus Core PBI Firmware";
		default: return nullptr;
	}
}

bool ATRapidusDevice::IsWritableFirmwareDirty(uint32 idx) const {
	return idx == 3 && mFlashEmu.IsDirty();
}

void ATRapidusDevice::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
	if (idx == 3) {
		stream.Write(mFlash, sizeof mFlash);
		mFlashEmu.SetDirty(false);
	} else if (idx == 4)
		stream.Write(mPBIFirmware816, sizeof mPBIFirmware816);
}

ATDeviceFirmwareStatus ATRapidusDevice::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATRapidusDevice::InitIndicators(IATDeviceIndicatorManager *indMgr) {
	mpIndicatorMgr = indMgr;
}

void ATRapidusDevice::InitPBI(IATDevicePBIManager *pbiman) {
	mpPBIManager = pbiman;
}

void ATRapidusDevice::GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const {
	devInfo.mbHasIrq = false;
	devInfo.mDeviceId = 0x01;
}

void ATRapidusDevice::SelectPBIDevice(bool enable) {
	// Once the 65C816 is running, disable the 6502 PBI even if the 6502 is re-enabled.
	if (m6502CR & 0x02)
		enable = false;

	if (mbPBIDeviceActive == enable)
		return;

	mbPBIDeviceActive = enable;
	mpMemMan->EnableLayer(mpLayerLoRegisters, enable);
	mpMemMan->EnableLayer(mpLayerPBIFirmware, enable);

	UpdateLoFlashWindow();
}

bool ATRapidusDevice::IsPBIOverlayActive() const {
	return mbPBIDeviceActive;
}

uint8 ATRapidusDevice::ReadPBIStatus(uint8 busData, bool debugOnly) {
	return busData;
}

void ATRapidusDevice::InitSystemControl(IATSystemController *sysctrl) {
	mpSystemController = sysctrl;
}

void ATRapidusDevice::SetROMLayers(
	ATMemoryLayer *layerLowerKernelROM,
	ATMemoryLayer *layerUpperKernelROM,
	ATMemoryLayer *layerBASICROM,
	ATMemoryLayer *layerSelfTestROM,
	ATMemoryLayer *layerGameROM,
	const void *kernelROM)
{
	if (mpMemMan)
		UpdateKernelROM();
}

void ATRapidusDevice::OnU1MBConfigPreLocked(bool inPreLockState) {
	if (mpMemMan)
		UpdateKernelROM();
}

void ATRapidusDevice::DumpStatus(ATConsoleOutput& output) {
	output("$D190 FPGA Config:     $%02X (%s, %s, %s, %s)"
		, mFPGAConfigReg
		, (mFPGAConfigReg & 0x01) ? "+sel" : "-sel"
		, (mFPGAConfigReg & 0x02) ? "+clear" : "-clear"
		, (mFPGAConfigReg & 0x40) ? "6502" : "65C816"
		, (mFPGAConfigReg & 0x80) ? "configured" : "cleared"
	);
	output("$FF0080 Memory CR:     $%02X (%s, %s, %s, %s, %s, %s, %s)"
		, mMCR
		, (mMCR & 0x80) ? "BaseOS" : "RapidOS"
		, (mMCR & 0x40) ? "I/O enabled" : "I/O disabled"
		, (mMCR & 0x20) ? "write-through on" : "write-through off"
		, (mMCR & 0x08) ? "slow3" : "fast3"
		, (mMCR & 0x04) ? "slow2" : "fast2"
		, (mMCR & 0x02) ? "slow1" : "fast1"
		, (mMCR & 0x01) ? "slow0" : "fast0"
	);
	output("$FF0081 Cm. memory CR: $%02X (%s, %s)"
		, mCMCR
		, (mCMCR & 0x40) ? "fastwrite3" : "nofastwrite3"
		, (mCMCR & 0x20) ? "wrap64K" : "nowrap64K"
	);
	output("$FF0082 SDRAM CR:      $%02X", mSCR);
	output("$FF0083 6502 CR:       $%02X", m6502CR);
	output("$FF0084 Add-on CR:     $%02X", mAR);
	output("$FF0090 HW Protect CR: $%02X", mHPCR);
	output <<= "";
	output("EEPROM:");

	for(uint32 i=0; i<256; i += 16) {
		output("%02X: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X"
			, i
			, mEEPROM[i+0]
			, mEEPROM[i+1]
			, mEEPROM[i+2]
			, mEEPROM[i+3]
			, mEEPROM[i+4]
			, mEEPROM[i+5]
			, mEEPROM[i+6]
			, mEEPROM[i+7]
			, mEEPROM[i+8]
			, mEEPROM[i+9]
			, mEEPROM[i+10]
			, mEEPROM[i+11]
			, mEEPROM[i+12]
			, mEEPROM[i+13]
			, mEEPROM[i+14]
			, mEEPROM[i+15]
		);
	}
}

sint32 ATRapidusDevice::DebugReadLoFlash(uint32 address) const {
	uint8 v;

	if (mFlashEmu.DebugReadByte(address - 0x4000 + mLoFlashOffset + 0x40000, v)) {
		mpMemMan->EnableLayer(mpLayerLoFlashControl, kATMemoryAccessMode_AR, false);
	}

	return v;
}

sint32 ATRapidusDevice::ReadLoFlash(uint32 address) {
	uint8 v;

	if (mFlashEmu.ReadByte(address - 0x4000 + mLoFlashOffset + 0x40000, v)) {
		mpMemMan->EnableLayer(mpLayerLoFlashControl, kATMemoryAccessMode_AR, false);
	}

	return v;
}

bool ATRapidusDevice::WriteLoFlash(uint32 address, uint8 value) {
	if (mFlashEmu.WriteByte(address - 0x4000 + mLoFlashOffset + 0x40000, value)) {
		mpMemMan->EnableLayer(mpLayerLoFlashControl, kATMemoryAccessMode_AR, false);
	}

	if (mFlashEmu.CheckForWriteActivity()) {
		if (mpIndicatorMgr)
			mpIndicatorMgr->SetFlashWriteActivity();

		mbFirmwareUsable = true;
	}

	return true;
}

sint32 ATRapidusDevice::DebugReadHiFlash(uint32 address) const {
	uint8 v;

	if (mFlashEmu.DebugReadByte(address & 0x7FFFF, v)) {
		mpMemMan->EnableLayer(mpLayerHiFlashControl, kATMemoryAccessMode_R, false);
	}

	return v;
}

sint32 ATRapidusDevice::ReadHiFlash(uint32 address) {
	uint8 v;

	if (mFlashEmu.ReadByte(address & 0x7FFFF, v)) {
		mpMemMan->EnableLayer(mpLayerHiFlashControl, kATMemoryAccessMode_R, false);
	}

	return v;
}

bool ATRapidusDevice::WriteHiFlash(uint32 address, uint8 value) {
	if (mFlashEmu.WriteByte(address & 0x7FFFF, value)) {
		mpMemMan->EnableLayer(mpLayerHiFlashControl, kATMemoryAccessMode_R, false);
	}

	if (mFlashEmu.CheckForWriteActivity()) {
		if (mpIndicatorMgr)
			mpIndicatorMgr->SetFlashWriteActivity();

		mbFirmwareUsable = true;
	}

	return true;
}

sint32 ATRapidusDevice::DebugReadLoRegs(uint32 address) const {
	switch(address) {
		case 0xD190:
			if (mFPGAConfigReg & 0x40)
				return mFPGABankReg;
			break;

		case 0xD191:
			return mFPGAConfigReg;

		case 0xD192:
			return 0xFF;

		case 0xD193:
			return 0xFF;

		case 0xD1A0:	// MCR (65C816 only)
			if (!(mFPGAConfigReg & 0x40))
				return mMCR;
			break;
	}

	return -1;
}

sint32 ATRapidusDevice::ReadLoRegs(uint32 address) {
	return DebugReadLoRegs(address);
}

bool ATRapidusDevice::WriteLoRegs(uint32 address, uint8 value) {
	switch(address) {
		case 0xD190:	// FPGA bank register
			if (mFPGAConfigReg & 0x40) {
				value &= 0x1F;

				if (mFPGABankReg != value) {
					mFPGABankReg = value;

					UpdateLoFlashWindow();
				}
			}
			return true;

		case 0xD191:	// FPGA config register (bits 0/1/6 writable, bits 0/1/6/7 readable)
			if (mFPGAConfigReg & 0x40) {
				value ^= (value ^ mFPGAConfigReg) & 0xBC;

				if (mFPGAConfigReg != value) {
					const uint8 delta = mFPGAConfigReg ^ value;

					mFPGAConfigReg = value;

					// check for FPGA clear (bit 2)
					if (mFPGAConfigReg & 0x02) {
						// reset FPGA initialization status (bit 7)
						mFPGAConfigReg &= 0x7F;
					}

					// check for CPU switch
					if (delta & 0x40) {
						SwitchCPU();
					}
				}
			}
			return true;

		case 0xD192:	// FPGA config data
			if (mFPGAConfigReg & 0x40) {
				// We cheat a lot here: if the chip is selected, not in clear mode, and receives any data, it
				// becomes magically configured.
				if ((mFPGAConfigReg & 0x03) == 0x01) {
					mFPGAConfigReg |= 0x80;
				}
			}
			return true;

		case 0xD193:	// Signal disable register
			return true;

		case 0xD1A0:	// Memory control register (65C816 only)
			if (!(mFPGAConfigReg & 0x40))
				SetMCR(value);
			return true;
	}

	return false;
}

sint32 ATRapidusDevice::DebugReadHiRegs(uint32 address) const {
	uint32 addr16 = address & 0xFFFF;

	if (addr16 < 8) {
		static constexpr uint8 kSignature[8]={
			(uint8)'6',		// Xilinx Spartan 6
			(uint8)'S',		// Xilinx Spartan
			(uint8)'9',		//
			(uint8)'0',		// Version 0.38
			(uint8)'3',		//
			(uint8)'8',		//
			(uint8)'E',		// External clock
			(uint8)' ',
		};

		return kSignature[addr16];
	} else switch(addr16) {
		case 0x0080:
			return mMCR;
		
		case 0x0081:
			return mCMCR;

		case 0x0082:
			return mSCR;

		case 0x0083:
			return mAR;

		case 0x0084:
			return m6502CR;

		case 0x008C:		// I2C data register (EEPROM access)
			return mI2CDataReg;

		case 0x008E:		// I2C status register (EEPROM access)
			return 0x00;

		case 0x0090:
			return mHPCR;
	}

	return 0xFF;
}

sint32 ATRapidusDevice::ReadHiRegs(uint32 address) {
	if (address == 0x0090) {
		// HPCR read also clears abort bit (bit 2).
		const uint8 v = mHPCR;

		mHPCR &= 0xFB;

		return v;
	}

	return DebugReadHiRegs(address);
}

bool ATRapidusDevice::WriteHiRegs(uint32 address, uint8 value) {
	switch(address & 0xFFFF) {
		case 0x0080:
			SetMCR(value);
			break;

		case 0x0081:
			SetCMCR(value);
			break;

		case 0x0082:		// SDRAM Control Register
			// D7:	SDRAM 4K cache (1 = disabled, 0 = enabled)
			if (const uint8 delta = mSCR ^ value) {
				mSCR = value;

				// check if banking window is affected
				if ((delta & 0x04) || ((value & 0x04) && (delta & 0x03))) {
					UpdateSDRAMWindow();
				}
			}
			break;

		case 0x0083:		// Addons Register
			mAR = value & 0x03;
			break;

		case 0x0084:		// 6502 Control Register
			if (m6502CR != value) {
				m6502CR = value;

				if (value & 0x02) {
					// switch back to 6502
					mFPGAConfigReg |= 0x40;
					SwitchCPU();
				}
			}
			break;

		case 0x008C:		// I2C data register (EEPROM access)
			mI2CDataReg = value;
			break;

		case 0x008D:		// I2C command register (EEPROM access)
			WriteI2CCommand(value);
			break;

		case 0x0090:		// Hardware Protect Control Register
			SetHPCR(value);
			break;
	}

	return true;
}

sint32 ATRapidusDevice::DebugReadHardware(uint32 address) const {
	return mpMemMan->RedirectDebugReadByte(0xD000 + (address & 0x7FF), this);
}

sint32 ATRapidusDevice::ReadHardware(uint32 address) {
	return mpMemMan->RedirectReadByte(0xD000 + (address & 0x7FF), this);
}

bool ATRapidusDevice::WriteHardware(uint32 address, uint8 value) {
	mpMemMan->RedirectWriteByte(0xD000 + (address & 0x7FF), value, this);
	return true;
}

sint32 ATRapidusDevice::HwProtectRead(uint32 address) {
	mpSystemController->AssertABORT();
	return 0xFF;
}

bool ATRapidusDevice::HwProtectWrite(uint32 address, uint8 value) {
	mpSystemController->AssertABORT();
	return true;
}

bool ATRapidusDevice::WriteThroughSRAM(uint32 address, uint8 value) {
	mSRAM[address & 0xFFFF] = value;
	return false;
}

void ATRapidusDevice::SwitchCPU() {
	// switch PBI firmware
	UpdatePBIFirmware();

	// disable the PBI device -- required or else PBI stays enabled and ROM check fails
	mpPBIManager->DeselectSelf(this);

	// update memory map, since many features are 65C816 only
	UpdateSRAMWindows();
	UpdateKernelROM();
	UpdateHardwareProtect();

	// reset the CPU
	ResetCPU();
}

void ATRapidusDevice::ResetCPU() {
	if (mFPGAConfigReg & 0x40) {
		mpSystemController->OverrideCPUMode(this, false, 1);
	} else {
		mpSystemController->OverrideCPUMode(this, true, 11);
	}
}

void ATRapidusDevice::UpdatePBIFirmware() {
	if (mFPGAConfigReg & 0x40) {
		// running on 6502 - use PBI firmware from flash
		mpMemMan->SetLayerMemory(mpLayerPBIFirmware, mFlash + 0x1D800);
	} else {
		// running on 65C816 - use PBI firmware from core
		mpMemMan->SetLayerMemory(mpLayerPBIFirmware, mPBIFirmware816);
	}
}

void ATRapidusDevice::SetMCR(uint8 v) {
	if (mMCR == v)
		return;

	const uint8 delta = mMCR ^ v;
	mMCR = v;

	if (delta & 0x7F)
		UpdateSRAMWindows();

	if (delta & 0x80)
		UpdateKernelROM();
}

void ATRapidusDevice::SetCMCR(uint8 v) {
	if (mCMCR == v)
		return;

	const uint8 delta = mCMCR ^ v;
	mCMCR = v;

	if (delta & 0x20)
		UpdateHMA();

	if (delta & 0x40)
		UpdateSRAMWindows();
}

void ATRapidusDevice::SetHPCR(uint8 v) {
	// only bits 0-1 can be changed
	const uint8 delta = (mHPCR ^ v) & 3;

	if (!delta)
		return;

	mHPCR ^= delta;

	UpdateHardwareProtect();
}

void ATRapidusDevice::WriteI2CCommand(uint8 value) {
	// check for read or write
	if (value & 0x10) {		// write
		// check for start sequence
		if (value & 0x04) {	// start
			mI2CState = kI2CState_Address;
		}

		if (mI2CState == kI2CState_Address) {
			// check for correct address
			if ((mI2CDataReg & 0xFE) == 0xA0)
				mI2CState = kI2CState_EEPROM_Address;
			else
				mI2CState = kI2CState_Ignore;
		} else if (mI2CState == kI2CState_EEPROM_Address) {
			mEEPROMAddress = mI2CDataReg;
			mI2CState = kI2CState_EEPROM_Transfer;
		} else if (mI2CState == kI2CState_EEPROM_Transfer) {
			g_ATLCEEPROMWrite("Write[$%02X] = $%02X\n", mEEPROMAddress & 0xFF, mI2CDataReg);

			mEEPROM[mEEPROMAddress++ & 0xFF] = mI2CDataReg;
		}
	} else {				// read
		mI2CDataReg = mEEPROM[mEEPROMAddress++ & 0xFF];

		g_ATLCEEPROMRead("Read[$%02X] = $%02X\n", (mEEPROMAddress - 1) & 0xFF, mI2CDataReg);
	}
}

void ATRapidusDevice::UpdateLoFlashWindow() {
	// check that window is enabled, 6502 is running, and PBI device is enabled
	if ((mFPGABankReg & 0x10) && (mFPGAConfigReg & 0x40) && mbPBIDeviceActive) {
		// ptb docs are wrong, bits 3 and 4 are swapped
		//const uint32 flashOffset = (uint32)((value & 0x17) + (value & 0x07)) << 13;
		const uint32 flashOffset = (uint32)(mFPGABankReg & 0x0F) << 14;

		mLoFlashOffset = flashOffset;

		mpMemMan->SetLayerModes(mpLayerLoFlash, kATMemoryAccessMode_AR);
		mpMemMan->SetLayerMemory(mpLayerLoFlash, mFlash + 256*1024 + flashOffset);

		mpMemMan->SetLayerModes(mpLayerLoFlashControl, mFlashEmu.IsControlReadEnabled() ? kATMemoryAccessMode_ARW : kATMemoryAccessMode_W);
	} else {
		mpMemMan->SetLayerModes(mpLayerLoFlash, kATMemoryAccessMode_0);
		mpMemMan->SetLayerModes(mpLayerLoFlashControl, kATMemoryAccessMode_0);
	}
}

void ATRapidusDevice::UpdateSRAMWindows() {
	const uint8 effectiveMCR = (mFPGAConfigReg & 0x40) ? 0x8F : mMCR;
	const bool enableWriteThrough = (mMCR & 0x20) != 0;

	// setting bit 6 of the CMCR disables write-through on $0000-3FFF.
	const bool fastMem0 = (mCMCR & 0x40) != 0;

	// First three 16K windows
	for(int i=0; i<3; ++i) {
		const bool windowEnabled = (effectiveMCR & (1 << i)) == 0;

		mpMemMan->EnableLayer(mpLayerBank0RAM[i], kATMemoryAccessMode_AR, windowEnabled);
		mpMemMan->EnableLayer(mpLayerBank0RAM[i], kATMemoryAccessMode_W, windowEnabled && (!enableWriteThrough || (i == 0 && fastMem0)));
	}

	// $C000-FFFF window (can be fragmented by hardware $D000-D7FF window)
	if (effectiveMCR & 0x08) {
		mpMemMan->EnableLayer(mpLayerBank0RAM[3], kATMemoryAccessMode_ARW, false);
		mpMemMan->EnableLayer(mpLayerBank0RAM[4], kATMemoryAccessMode_ARW, false);
	} else {
		mpMemMan->EnableLayer(mpLayerBank0RAM[3], kATMemoryAccessMode_AR, true);
		mpMemMan->EnableLayer(mpLayerBank0RAM[3], kATMemoryAccessMode_W, !enableWriteThrough);

		if (effectiveMCR & 0x40) {
			mpMemMan->SetLayerMaskRange(mpLayerBank0RAM[3], 0xC0, 0x10);
			mpMemMan->EnableLayer(mpLayerBank0RAM[4], kATMemoryAccessMode_AR, true);
			mpMemMan->EnableLayer(mpLayerBank0RAM[4], kATMemoryAccessMode_W, !enableWriteThrough);
		} else {
			mpMemMan->SetLayerMaskRange(mpLayerBank0RAM[3], 0xC0, 0x40);
			mpMemMan->EnableLayer(mpLayerBank0RAM[4], kATMemoryAccessMode_ARW, false);
		}
	}

	mpMemMan->EnableLayer(mpLayerLoBank0RAMShadow, kATMemoryAccessMode_W, enableWriteThrough && !fastMem0);
	mpMemMan->EnableLayer(mpLayerHiBank0RAMShadow, kATMemoryAccessMode_W, enableWriteThrough);
}

void ATRapidusDevice::UpdateSDRAMWindow() {
	// check if window is enabled
	if (mSCR & 0x04) {
		mpMemMan->SetLayerMemory(mpLayerBankedSDRAM, mSDRAMBanks[mSCR & 0x03]);
		mpMemMan->EnableLayer(mpLayerBankedSDRAM, true);
	} else {
		mpMemMan->EnableLayer(mpLayerBankedSDRAM, false);
	}
}

void ATRapidusDevice::UpdateHMA() {
	mpMemMan->SetWrapBankZeroEnabled((mCMCR & 0x20) != 0);
}

void ATRapidusDevice::UpdateKernelROM() {
	const void *kernel = nullptr;
	sint8 highSpeedOverride = 0;

	if (!(mFPGAConfigReg & 0x40) && !(mMCR & 0x80) && !mpSystemController->IsU1MBConfigPreLocked()) {
		// 65C816 active and OS flash disable clear -- use flash $0C000-0FFFF
		kernel = mFlash + 0xC000;
		highSpeedOverride = 1;
	}

	mpSystemController->OverrideKernelMapping(this, kernel, highSpeedOverride, true);
}

void ATRapidusDevice::UpdateHardwareProtect() {
	const bool is6502 = (mFPGAConfigReg & 0x40) != 0;

	if (is6502)
		mpMemMan->EnableLayer(mpLayerHwProtect, false);
	else {
		mpMemMan->EnableLayer(mpLayerHwProtect, kATMemoryAccessMode_R, (mHPCR & 0x01) != 0);
		mpMemMan->EnableLayer(mpLayerHwProtect, kATMemoryAccessMode_W, (mHPCR & 0x02) != 0);
	}
}

void ATRapidusDevice::LoadNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	uint8 buf[sizeof(mEEPROM)] {};

	key.getBinary("Rapidus EEPROM", (char *)mEEPROM, sizeof(mEEPROM));
}

void ATRapidusDevice::SaveNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	uint8 buf[sizeof(mEEPROM)] {};

	key.setBinary("Rapidus EEPROM", (const char *)mEEPROM, sizeof(mEEPROM));
}


///////////////////////////////////////////////////////////////////////////

void ATCreateDeviceRapidus(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATRapidusDevice> p(new ATRapidusDevice);

	*dev = p.release();
}
