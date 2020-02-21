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

#ifndef f_AT_SIDE_H
#define f_AT_SIDE_H

#include <at/atcore/blockdevice.h>
#include <at/atcore/devicecart.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atemulation/flash.h>
#include <at/atemulation/rtcds1305.h>
#include "ide.h"

class IATDeviceIndicatorManager;
class ATMemoryManager;
class ATMemoryLayer;
class ATIDEEmulator;
class ATSimulator;
class ATFirmwareManager;

class ATSIDEEmulator
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceMemMap
	, public IATDeviceCartridge
	, public IATDeviceIndicators
	, public IATDeviceFirmware
	, public IATDeviceParent
	, public ATDeviceBus
	, public IATDeviceDiagnostics
	, public IATDeviceButtons
{
	ATSIDEEmulator(const ATSIDEEmulator&) = delete;
	ATSIDEEmulator& operator=(const ATSIDEEmulator&) = delete;
public:
	ATSIDEEmulator(bool v2);
	~ATSIDEEmulator();

	void *AsInterface(uint32 id) override;

	void Init() override;
	void Shutdown() override;
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void ColdReset() override;

	bool IsSDXEnabled() const { return mbSDXEnable; }
	void SetSDXEnabled(bool enable);

	void ResetCartBank();

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch);

public:		// IATDeviceMemMap
	void InitMemMap(ATMemoryManager *memmap);
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const;

public:		// IATDeviceCartridge
	void InitCartridge(IATDeviceCartridgePort *cartPort) override;
	bool IsLeftCartActive() const override;
	void SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) override;
	void UpdateCartSense(bool leftActive) override;

public:		// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:		// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override;
	bool IsWritableFirmwareDirty(uint32 idx) const override;
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override;
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:		// IATDeviceBus
	IATDeviceBus *GetDeviceBus(uint32 index) override;

public:		// IATDeviceParent
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

public:		// IATDeviceDiagnostics
	void DumpStatus(ATConsoleOutput& output) override;
	
public:		// IATDeviceButtons
	uint32 GetSupportedButtons() const override;
	bool IsButtonDepressed(ATDeviceButton idx) const override;
	void ActivateButton(ATDeviceButton idx, bool state) override;

protected:
	void LoadNVRAM();
	void SaveNVRAM();

	void SetSDXBank(sint32 bank, bool topEnable);
	void SetTopBank(sint32 bank, bool topLeftEnable, bool topRightEnable);

	static sint32 OnDebugReadByte(void *thisptr, uint32 addr);
	static sint32 OnReadByte(void *thisptr, uint32 addr);
	static bool OnWriteByte(void *thisptr, uint32 addr, uint8 value);

	static sint32 OnCartDebugRead(void *thisptr, uint32 addr);
	static sint32 OnCartDebugRead2(void *thisptr, uint32 addr);
	static sint32 OnCartRead(void *thisptr, uint32 addr);
	static sint32 OnCartRead2(void *thisptr, uint32 addr);
	static bool OnCartWrite(void *thisptr, uint32 addr, uint8 value);
	static bool OnCartWrite2(void *thisptr, uint32 addr, uint8 value);

	void UpdateMemoryLayersCart();
	void UpdateIDEReset();

	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATMemoryLayer *mpMemLayerIDE = nullptr;
	ATMemoryLayer *mpMemLayerCart = nullptr;
	ATMemoryLayer *mpMemLayerCart2 = nullptr;
	ATMemoryLayer *mpMemLayerCartControl = nullptr;
	ATMemoryLayer *mpMemLayerCartControl2 = nullptr;
	bool	mbExternalEnable = false;
	bool	mbSDXEnable = false;
	bool	mbTopEnable = false;
	bool	mbTopLeftEnable = false;
	bool	mbTopRightEnable = false;
	bool	mbIDERemoved = true;
	bool	mbIDEEnabled = false;
	bool	mbIDEReset = false;
	bool	mbFirmwareUsable = false;
	const bool mbVersion2;
	uint8	mSDXBankRegister = 0;
	uint8	mTopBankRegister = 0;
	sint32	mSDXBank = 0;
	sint32	mTopBank = 0;
	sint32	mBankOffset = 0;
	sint32	mBankOffset2 = 0;

	vdrefptr<IATBlockDevice> mpBlockDevice;

	ATFirmwareManager *mpFirmwareManager = nullptr;

	IATDeviceCartridgePort *mpCartridgePort = nullptr;
	uint32	mCartId = 0;
	bool	mbLeftWindowEnabled = false;
	bool	mbRightWindowEnabled = false;
	bool	mbCCTLEnabled = false;

	ATFlashEmulator	mFlashCtrl;
	ATRTCDS1305Emulator mRTC;
	ATIDEEmulator mIDE;

	VDALIGN(4) uint8	mFlash[0x80000];
};

#endif
