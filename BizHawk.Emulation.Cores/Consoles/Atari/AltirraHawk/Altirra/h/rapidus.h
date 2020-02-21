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

#ifndef f_AT_RAPIDUS_H
#define f_AT_RAPIDUS_H

#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicepbi.h>
#include <at/atcore/devicesystemcontrol.h>
#include <at/atemulation/flash.h>

class ATMemoryLayer;

class ATRapidusDevice final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceMemMap
	, public IATDeviceFirmware
	, public IATDeviceIndicators
	, public IATDevicePBIConnection
	, public IATPBIDevice
	, public IATDeviceSystemControl
	, public IATDeviceDiagnostics
{
	ATRapidusDevice(const ATRapidusDevice&) = delete;
	ATRapidusDevice& operator=(const ATRapidusDevice&) = delete;
public:
	enum : uint32 { kTypeID = 'rapi' };

	ATRapidusDevice();

public:
	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;
	void WarmReset() override;

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:		// IATDeviceMemMap
	void InitMemMap(ATMemoryManager *memman) override;
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:		// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override;
	bool IsWritableFirmwareDirty(uint32 idx) const override;
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override;
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:		// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:		// IATDevicePBIConnection
	void InitPBI(IATDevicePBIManager *pbiman) override;

public:		// IATPBIDevice
	void GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const override;
	void SelectPBIDevice(bool enable) override;
	bool IsPBIOverlayActive() const override;
	uint8 ReadPBIStatus(uint8 busData, bool debugOnly) override;

public:		// IATDeviceSystemControl
	void InitSystemControl(IATSystemController *sysctrl) override;
	void SetROMLayers(
		ATMemoryLayer *layerLowerKernelROM,
		ATMemoryLayer *layerUpperKernelROM,
		ATMemoryLayer *layerBASICROM,
		ATMemoryLayer *layerSelfTestROM,
		ATMemoryLayer *layerGameROM,
		const void *kernelROM) override;
	void OnU1MBConfigPreLocked(bool inPreLockState) override;

public:		// IATDeviceDiagnostics
	void DumpStatus(ATConsoleOutput& output) override;

private:
	sint32 DebugReadLoFlash(uint32 address) const;
	sint32 ReadLoFlash(uint32 address);
	bool WriteLoFlash(uint32 address, uint8 value);

	sint32 DebugReadHiFlash(uint32 address) const;
	sint32 ReadHiFlash(uint32 address);
	bool WriteHiFlash(uint32 address, uint8 value);

	sint32 DebugReadLoRegs(uint32 address) const;
	sint32 ReadLoRegs(uint32 address);
	bool WriteLoRegs(uint32 address, uint8 value);

	sint32 DebugReadHiRegs(uint32 address) const;
	sint32 ReadHiRegs(uint32 address);
	bool WriteHiRegs(uint32 address, uint8 value);

	sint32 DebugReadHardware(uint32 address) const;
	sint32 ReadHardware(uint32 address);
	bool WriteHardware(uint32 address, uint8 value);

	sint32 HwProtectRead(uint32 address);
	bool HwProtectWrite(uint32 address, uint8 value);

	bool WriteThroughSRAM(uint32 address, uint8 value);

	void SwitchCPU();
	void ResetCPU();
	void WriteI2CCommand(uint8 value);
	void SetMCR(uint8 v);
	void SetCMCR(uint8 v);
	void SetHPCR(uint8 v);
	void UpdateLoFlashWindow();
	void UpdatePBIFirmware();
	void UpdateSRAMWindows();
	void UpdateSDRAMWindow();
	void UpdateHMA();
	void UpdateKernelROM();
	void UpdateHardwareProtect();
	void LoadNVRAM();
	void SaveNVRAM();

	ATScheduler *mpScheduler = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	IATDevicePBIManager *mpPBIManager = nullptr;
	IATDeviceIndicatorManager *mpIndicatorMgr = nullptr;
	ATFirmwareManager *mpFwMgr = nullptr;
	IATSystemController *mpSystemController = nullptr;

	bool mbPBIDeviceActive = false;
	bool mbFirmwareUsable = false;

	uint8 mFPGABankReg = 0;
	uint8 mFPGAConfigReg = 0;
	uint8 mMCR = 0;			// $FF0080 Memory Configuration Register
	uint8 mCMCR = 0;		// $FF0081 Complementary Memory Configuration Register
	uint8 mSCR = 0;			// $FF0082 SDRAM Control Register
	uint8 mAR = 0;			// $FF0083 Addons Register
	uint8 m6502CR = 0;		// $FF0084 6502 Control Register
	uint8 mHPCR = 0;		// $FF0090 Hardware Protect Control Register
	uint32 mLoFlashOffset = 0;

	enum I2CState : uint8 {
		kI2CState_Address,
		kI2CState_Ignore,
		kI2CState_EEPROM_Address,
		kI2CState_EEPROM_Transfer
	};

	I2CState mI2CState {};
	uint8 mI2CDataReg = 0;
	uint8 mEEPROMAddress = 0;

	ATMemoryLayer *mpLayerLoFlash = nullptr;				// $00:4000-7FFF read
	ATMemoryLayer *mpLayerLoFlashControl = nullptr;			// $00:4000-7FFF read/write
	ATMemoryLayer *mpLayerBank0RAM[5] {};					// 4 x 16KB in bank 0, last block fragmented into $C000-CFFF/D7FF and D800-FFFF
	ATMemoryLayer *mpLayerLoBank0RAMShadow = nullptr;		// $00:0000-3FFF write only
	ATMemoryLayer *mpLayerHiBank0RAMShadow = nullptr;		// $00:4000-FFFF write only
	ATMemoryLayer *mpLayerSRAM = nullptr;					// $01:0000-07:FFFF or 0F:FFFF
	ATMemoryLayer *mpLayerSDRAM = nullptr;					// $08:0000-EF:FFFF
	ATMemoryLayer *mpLayerBankedSDRAM = nullptr;			// $80:0000-BF:FFFF
	ATMemoryLayer *mpLayerHiFlash = nullptr;				// $F0:0000-F7:FFFF read
	ATMemoryLayer *mpLayerHiFlashControl = nullptr;			// $F0:0000-F7:FFFF read/write
	ATMemoryLayer *mpLayerPBIFirmware = nullptr;			// $00:D800-00:DFFF read only
	ATMemoryLayer *mpLayerHwProtect = nullptr;				// $00:D000-00:D7FF read/write
	ATMemoryLayer *mpLayerLoRegisters = nullptr;			// $00:D1xx
	ATMemoryLayer *mpLayerHiRegisters = nullptr;			// $FF:0000-FF:FFFF read/write
	ATMemoryLayer *mpLayerHardwareMirror = nullptr;			// $FF:D000-FF:D7FF read/write

	// borrowed memory layers
	ATFlashEmulator mFlashEmu;

	uint8 mEEPROM[256] {};

	alignas(2) uint8 mFlash[512 * 1024] {};					// 512KB
	alignas(2) uint8 mSRAM[1024 * 1024] {};					// 512KB / 1MB
	alignas(2) uint8 mSDRAM[(14 * 1024 + 512) * 1024] {};	// 14.5MB SDRAM ($080000-EFFFFF)
	alignas(2) uint8 mSDRAMBanks[4][4 * 1024 * 1024];		// 4 x 4MB banked SDRAM ($800000-BFFFFF)
	alignas(2) uint8 mPBIFirmware816[0x800] {};				// 2KB
};

#endif
