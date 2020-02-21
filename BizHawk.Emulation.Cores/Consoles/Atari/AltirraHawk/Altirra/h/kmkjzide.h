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

#ifndef f_AT_KMKJZIDE_H
#define f_AT_KMKJZIDE_H

#include <at/atcore/devicecart.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceindicators.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atcore/devicepbi.h>
#include "pbi.h"
#include "ide.h"
#include <at/atemulation/flash.h>
#include <at/atemulation/rtcv3021.h>
#include "covox.h"

class ATMemoryManager;
class ATMemoryLayer;
class ATIDEEmulator;
class ATFirmwareManager;
class IATBlockDevice;

class ATKMKJZIDE final : public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceIndicators
	, public IATDeviceFirmware
	, public IATDeviceMemMap
	, public IATDevicePBIConnection
	, public IATPBIDevice
	, public IATDeviceParent
	, public ATDeviceBus
	, public IATDeviceCartridge
	, public IATDeviceDiagnostics
	, public IATDeviceIRQSource
	, public IATDeviceButtons
	, public IATDeviceAudioOutput
	, public VDAlignedObject<16>
{
	ATKMKJZIDE(const ATKMKJZIDE&) = delete;
	ATKMKJZIDE& operator=(const ATKMKJZIDE&) = delete;

public:
	ATKMKJZIDE(bool version2);
	~ATKMKJZIDE();

	void *AsInterface(uint32 id) override;

	bool IsVersion2() const { return mbVersion2; }
	bool IsMainFlashDirty() const { return mFlashCtrl.IsDirty(); }
	bool IsSDXFlashDirty() const { return mSDXCtrl.IsDirty(); }

	void GetSettingsBlurb(VDStringW& buf);
	void GetSettings(ATPropertySet& settings);
	bool SetSettings(const ATPropertySet& settings);

	void Init() override;
	void Shutdown() override;

	void GetDeviceInfo(ATDeviceInfo& info) override;

	void ColdReset() override;
	void WarmReset() override;

	void LoadNVRAM();
	void SaveNVRAM();

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:		// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *indicators) override;

public:		// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override;
	bool IsWritableFirmwareDirty(uint32 idx) const override;
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override;
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:		// IATDeviceMemMap
	void InitMemMap(ATMemoryManager *memman) override;
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:		// IATDevicePBIConnection
	void InitPBI(IATDevicePBIManager *pbi) override;

public:		// IATPBIDevice
	void GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const override;
	void SelectPBIDevice(bool enable) override;
	
	bool IsPBIOverlayActive() const override;
	uint8 ReadPBIStatus(uint8 busData, bool debugOnly) override;

public:		// IATDeviceParent
	IATDeviceBus *GetDeviceBus(uint32 index) override;

public:		// IATDeviceBus
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

public:		// IATDeviceCartridge
	void InitCartridge(IATDeviceCartridgePort *cartPort) override;
	bool IsLeftCartActive() const override;
	void SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) override;
	void UpdateCartSense(bool leftActive) override;

public:		// IATDeviceDiagnostics
	void DumpStatus(ATConsoleOutput& output) override;

public:		// IATDeviceIrqSource
	void InitIRQSource(ATIRQController *irqc) override;

public:		// IATDeviceButtons
	uint32 GetSupportedButtons() const override;
	bool IsButtonDepressed(ATDeviceButton idx) const override;
	void ActivateButton(ATDeviceButton idx, bool state) override;

public:		// IATDeviceAudioOutput
	void InitAudioOutput(IATAudioMixer *mixer) override;

protected:
	static sint32 OnControlDebugRead(void *thisptr, uint32 addr);
	static sint32 OnControlRead(void *thisptr, uint32 addr);
	static bool OnControlWrite(void *thisptr, uint32 addr, uint8 value);

	static sint32 OnFlashDebugRead(void *thisptr, uint32 addr);
	static sint32 OnFlashRead(void *thisptr, uint32 addr);
	static bool OnFlashWrite(void *thisptr, uint32 addr, uint8 value);

	static sint32 OnSDXDebugRead(void *thisptr, uint32 addr);
	static sint32 OnSDXRead(void *thisptr, uint32 addr);
	static bool OnSDXWrite(void *thisptr, uint32 addr, uint8 value);

	void UpdateMemoryLayersFlash();
	void UpdateMemoryLayersSDX();
	void UpdateCartPassThrough();

	vdrefptr<IATBlockDevice> mpBlockDevices[2];
	IATDevicePBIManager *mpPBIManager = nullptr;
	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	ATScheduler		*mpScheduler = nullptr;
	ATFirmwareManager *mpFwManager = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	ATMemoryLayer	*mpMemLayerControl = nullptr;
	ATMemoryLayer	*mpMemLayerFlash = nullptr;
	ATMemoryLayer	*mpMemLayerFlashControl = nullptr;
	ATMemoryLayer	*mpMemLayerRAM = nullptr;
	ATMemoryLayer	*mpMemLayerSDX = nullptr;
	ATMemoryLayer	*mpMemLayerSDXControl = nullptr;

	uint8	mHighDataLatch = 0;
	uint8	mDeviceId = 0x01;

	enum Revision : uint8 {
		kRevision_V1,
		kRevision_V2_C,
		kRevision_V2_D,
		kRevision_V2_S,
		kRevision_V2_E
	};

	Revision	mRevision = kRevision_V2_D;
	const bool	mbVersion2;
	bool	mbFirmwareUsable = false;	
	bool	mbSDXSwitchEnabled = true;
	bool	mbSDXEnabled = false;
	bool	mbSDXUpstreamEnabled = false;
	bool	mbWriteProtect = false;
	bool	mbNVRAMGuard = true;
	bool	mbExternalEnabled = false;
	bool	mbSelected = false;
	bool	mbIDESlaveSelected = false;

	uint32	mFlashBankOffset = 0;
	uint32	mSDXBankOffset = 0;

	IATDeviceCartridgePort *mpCartPort = nullptr;
	uint32	mCartId = 0;

	ATIRQController *mpIrqController = nullptr;
	uint32	mIrq = 0;
	bool	mbIrqEnabled = false;
	bool	mbIrqActive = false;

	IATAudioMixer *mpAudioMixer = nullptr;

	ATFlashEmulator	mFlashCtrl;
	ATFlashEmulator	mSDXCtrl;

	ATRTCV3021Emulator mRTC;
	ATIDEEmulator mIDE[2];

	ATCovoxEmulator mCovox;

	VDALIGN(4) uint8	mRAM[0x8000] = {};
	VDALIGN(4) uint8	mFlash[0x20000] = {};
	VDALIGN(4) uint8	mSDX[0x80000] = {};
};

#endif
