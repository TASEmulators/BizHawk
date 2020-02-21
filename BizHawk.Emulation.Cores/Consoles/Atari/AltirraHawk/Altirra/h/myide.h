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

#ifndef f_AT_MYIDE_H
#define f_AT_MYIDE_H

#include <at/atcore/blockdevice.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicecart.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atemulation/flash.h>
#include "ide.h"

class ATMemoryManager;
class ATMemoryLayer;
class ATIDEEmulator;
class ATScheduler;
class ATFirmwareManager;

class ATMyIDEEmulator final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceMemMap
	, public IATDeviceCartridge
	, public IATDeviceIndicators
	, public IATDeviceFirmware
	, public IATDeviceParent
	, public ATDeviceBus
{
	ATMyIDEEmulator(const ATMyIDEEmulator&) = delete;
	ATMyIDEEmulator& operator=(const ATMyIDEEmulator&) = delete;
public:
	ATMyIDEEmulator(bool version2, bool useD5xx);
	~ATMyIDEEmulator();

	void *AsInterface(uint32 id);

	bool IsUsingD5xx() const { return mbUseD5xx; }

	void Init() override;
	void Shutdown() override;
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void ColdReset() override;

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:		// IATDeviceMemMap
	void InitMemMap(ATMemoryManager *memmap) override;
	bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

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

public:		// IATDeviceParent
	IATDeviceBus *GetDeviceBus(uint32 index) override;

public:		// IATDeviceBus
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

protected:
	static sint32 OnDebugReadByte_CCTL(void *thisptr, uint32 addr);
	static sint32 OnReadByte_CCTL(void *thisptr, uint32 addr);
	static bool OnWriteByte_CCTL(void *thisptr, uint32 addr, uint8 value);

	static sint32 OnDebugReadByte_CCTL_V2(void *thisptr, uint32 addr);
	static sint32 OnReadByte_CCTL_V2(void *thisptr, uint32 addr);
	static bool OnWriteByte_CCTL_V2(void *thisptr, uint32 addr, uint8 value);

	static sint32 DebugReadByte_Cart_V2(void *thisptr0, uint32 address);
	static sint32 ReadByte_Cart_V2(void *thisptr0, uint32 address);
	static bool WriteByte_Cart_V2(void *thisptr0, uint32 address, uint8 value);

	void UpdateIDEReset();

	void SetCartBank(int bank);
	void SetCartBank2(int bank);

	void UpdateCartBank();
	void UpdateCartBank2();

	ATScheduler *mpScheduler = nullptr;
	ATMemoryManager *mpMemMan = nullptr;
	ATFirmwareManager *mpFirmwareManager = nullptr;
	ATMemoryLayer *mpMemLayerIDE = nullptr;
	ATMemoryLayer *mpMemLayerLeftCart = nullptr;
	ATMemoryLayer *mpMemLayerLeftCartFlash = nullptr;
	ATMemoryLayer *mpMemLayerRightCart = nullptr;
	ATMemoryLayer *mpMemLayerRightCartFlash = nullptr;
	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	bool mbCFPower = false;
	bool mbCFPowerLatch = false;
	bool mbCFReset = false;
	bool mbCFResetLatch = false;
	bool mbCFAltReg = false;
	bool mbSelectSlave = false;
	const bool mbVersion2;
	bool mbVersion2Ex = false;
	const bool mbUseD5xx;
	bool mbFirmwareUsable = false;

	IATDeviceCartridgePort *mpCartridgePort = nullptr;
	uint32 mCartId = 0;
	bool mbLeftWindowEnabled = false;
	bool mbRightWindowEnabled = false;
	bool mbCCTLEnabled = false;

	vdrefptr<IATBlockDevice> mpBlockDevices[2];

	// MyIDE II control registers
	int	mCartBank = 0;
	int	mCartBank2 = 0;
	uint8	mLeftPage = 0;
	uint8	mRightPage = 0;
	uint32	mKeyHolePage = 0;
	uint8	mControl = 0;

	ATFlashEmulator	mFlash;
	ATIDEEmulator mIDE[2];

	VDALIGN(4) uint8 mFirmware[0x80000];	// cleared to 0XFF in ctor
	VDALIGN(4) uint8 mRAM[0x80000] = {};
};

#endif
