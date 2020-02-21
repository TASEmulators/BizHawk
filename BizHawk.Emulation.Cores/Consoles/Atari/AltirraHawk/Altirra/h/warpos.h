//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_WARPOS_H
#define f_AT_WARPOS_H

#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceport.h>
#include <at/atcore/devicesystemcontrol.h>

class ATMemoryLayer;
class ATEvent;

class ATWarpOSDevice final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceFirmware
	, public IATDeviceSystemControl
	, public IATDevicePortInput
	, public IATSchedulerCallback
{
	ATWarpOSDevice(const ATWarpOSDevice&) = delete;
	ATWarpOSDevice& operator=(const ATWarpOSDevice&) = delete;
public:
	ATWarpOSDevice();

public:
	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;
	void WarmReset() override;

public:		// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:		// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override;
	bool IsWritableFirmwareDirty(uint32 idx) const override;
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override;
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

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

public:		// IATDevicePortInput
	void InitPortInput(IATDevicePortManager *portmgr) override;
	
public:		// IATScheduledEvent
	void OnScheduledEvent(uint32 id) override;

private:
	void OnPortOutput(uint32 state);
	void UpdateInputShifter();
	void UpdateKernelROM();
	void LoadNVRAM();
	void SaveNVRAM();

	ATScheduler *mpScheduler = nullptr;
	ATFirmwareManager *mpFwMgr = nullptr;
	IATSystemController *mpSystemController = nullptr;
	IATDevicePortManager *mpPortManager = nullptr;
	sint32 mPortInputIndex = -1;
	sint32 mPortOutputIndex = -1;
	ATEvent *mpEvent = nullptr;

	ATDeviceFirmwareStatus mFirmwareStatus = ATDeviceFirmwareStatus::Missing;
	bool mbLastPB7State = false;
	uint8 mCurrentSlot = 31;
	uint8 mCurrentSetting = 0;
	uint8 mCommand = 0;
	uint32 mShifter = 0;
	uint64 mShiftStart = 0;

	enum class State : uint8 {
		ShiftCommand1,
		ShiftCommand2 = ShiftCommand1 + 11,
		ShiftCommand3 = ShiftCommand2 + 11,
		ShiftResult = ShiftCommand3 + 11,
		Reset = ShiftResult + 32
	};

	State mState = State::ShiftCommand1;

	alignas(2) uint8 mFlash[512 * 1024] {};					// 512KB
};

#endif
