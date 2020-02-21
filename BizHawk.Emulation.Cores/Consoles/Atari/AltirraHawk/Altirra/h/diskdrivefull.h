//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_AT_DISKDRIVEFULL_H
#define f_AT_DISKDRIVEFULL_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/devicediskdrive.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicesioimpl.h>
#include <at/atcpu/co6502.h>
#include <at/atcpu/breakpoints.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atdebugger/breakpointsimpl.h>
#include <at/atdebugger/target.h>
#include <at/atcore/scheduler.h>
#include <at/atemulation/riot.h>
#include "fdc.h"
#include "diskdrivefullbase.h"
#include "diskinterface.h"

class ATIRQController;

class ATDeviceDiskDriveFull final : public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceFirmware
	, public IATDeviceDiskDrive
	, public ATDeviceSIO
	, public IATDeviceButtons
	, public IATDeviceDebugTarget
	, public IATDebugTarget
	, public IATDebugTargetHistory
	, public IATDebugTargetExecutionControl
	, public IATCPUBreakpointHandler
	, public IATSchedulerCallback
	, public IATDeviceRawSIO
	, public IATDiskInterfaceClient
{
public:
	enum DeviceType : uint8 {
		kDeviceType_810,
		kDeviceType_Happy810,
		kDeviceType_810Archiver,
		kDeviceType_1050,
		kDeviceType_USDoubler,
		kDeviceType_Speedy1050,
		kDeviceType_Happy1050,
		kDeviceType_SuperArchiver,
		kDeviceType_TOMS1050,
		kDeviceType_Tygrys1050,
		kDeviceType_1050Duplicator,
		kDeviceType_1050Turbo,
		kDeviceType_1050TurboII,
		kDeviceType_ISPlate,
		kDeviceTypeCount,
	};

	ATDeviceDiskDriveFull(bool is1050, DeviceType deviceType);
	~ATDeviceDiskDriveFull();

	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettingsBlurb(VDStringW& buf) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Init() override;
	void Shutdown() override;
	uint32 GetComputerPowerOnDelay() const override;
	void WarmReset() override;
	void ComputerColdReset() override;
	void PeripheralColdReset() override;

public:
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:		// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override;
	bool IsWritableFirmwareDirty(uint32 idx) const override;
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override;
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:		// IATDeviceDiskDrive
	void InitDiskDrive(IATDiskDriveManager *ddm) override;

public:		// ATDeviceSIO
	void InitSIO(IATDeviceSIOManager *mgr) override;

public:		// IATDeviceButtons
	uint32 GetSupportedButtons() const;
	bool IsButtonDepressed(ATDeviceButton idx) const;
	void ActivateButton(ATDeviceButton idx, bool state);

public:	// IATDeviceDebugTarget
	IATDebugTarget *GetDebugTarget(uint32 index) override;

public:	// IATDebugTarget
	const char *GetName() override;
	ATDebugDisasmMode GetDisasmMode() override;

	void GetExecState(ATCPUExecState& state) override;
	void SetExecState(const ATCPUExecState& state) override;

	sint32 GetTimeSkew() override;

	uint8 ReadByte(uint32 address) override;
	void ReadMemory(uint32 address, void *dst, uint32 n) override;

	uint8 DebugReadByte(uint32 address) override;
	void DebugReadMemory(uint32 address, void *dst, uint32 n) override;

	void WriteByte(uint32 address, uint8 value) override;
	void WriteMemory(uint32 address, const void *src, uint32 n) override;

public:	// IATDebugTargetHistory
	bool GetHistoryEnabled() const override;
	void SetHistoryEnabled(bool enable) override;

	std::pair<uint32, uint32> GetHistoryRange() const override;
	uint32 ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const override;
	uint32 ConvertRawTimestamp(uint32 rawTimestamp) const override;
	double GetTimestampFrequency() const override { return mb1050 ? 1000000.0 : 500000.0; }

public:	// IATDebugTargetExecutionControl
	void Break() override;
	bool StepInto(const vdfunction<void(bool)>& fn) override;
	bool StepOver(const vdfunction<void(bool)>& fn) override;
	bool StepOut(const vdfunction<void(bool)>& fn) override;
	void StepUpdate() override;
	void RunUntilSynced() override;

public:	// IATCPUBreakpointHandler
	bool CheckBreakpoint(uint32 pc) override;

public:	// IATSchedulerCallback
	void OnScheduledEvent(uint32 id) override;

public:	// IATDeviceRawSIO
	void OnCommandStateChanged(bool asserted) override;
	void OnMotorStateChanged(bool asserted) override;
	void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	void OnSendReady() override;

public:	// IATDiskInterfaceClient
	void OnDiskChanged(bool mediaRemoved) override;
	void OnWriteModeChanged() override;
	void OnTimingModeChanged() override;
	void OnAudioModeChanged() override;

protected:
	void CancelStep();

	void Sync();
	void AccumSubCycles();

	void OnTransmitEvent();
	void QueueNextTransmitEvent();
	void AddTransmitEdge(uint32 polarity);

	void OnRIOTRegisterWrite(uint32 addr, uint8 val);

	void PlayStepSound();
	void UpdateRotationStatus();
	void UpdateDiskStatus();
	void UpdateWriteProtectStatus();
	void UpdateROMBank();
	void UpdateROMBankSuperArchiver();
	void UpdateROMBankHappy810();
	void UpdateROMBankHappy1050();
	void UpdateROMBank1050Turbo();
	void OnToggleFastSlow();
	void ClearWriteProtect();
	void OnToggleWriteProtect();
	void OnWriteEnabled();
	void UpdateWriteProtectOverride();
	void UpdateSlowSwitch();

	enum {
		kEventId_Run = 1,
		kEventId_Transmit,
		kEventId_DriveReceiveBit,
		kEventId_DriveDiskChange,
	};

	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	ATEvent *mpRunEvent = nullptr;
	ATEvent *mpTransmitEvent = nullptr;
	ATEvent *mpEventDriveReceiveBit = nullptr;
	ATEvent *mpEventDriveTransmitBit = nullptr;
	ATEvent *mpEventDriveDiskChange = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;
	IATDiskDriveManager *mpDiskDriveManager = nullptr;
	ATDiskInterface *mpDiskInterface = nullptr;

	ATFirmwareManager *mpFwMgr = nullptr;

	static constexpr uint32 kDiskChangeStepMS = 50;

	uint32 mReceiveShiftRegister = 0;
	uint32 mReceiveTimingAccum = 0;
	uint32 mReceiveTimingStep = 0;

	uint32 mTransmitResetCounter = 0;
	uint32 mTransmitCyclesPerBit = 0;
	uint8 mTransmitShiftRegister = 0;
	uint8 mTransmitPhase = 0;
	bool mbTransmitCurrentBit = true;

	uint32 mCurrentTrack = 0;
	bool mbROMBankAlt = false;
	uint8 mROMBank = 0;

	uint32 mLastSync = 0;
	uint32 mLastSyncDriveTime = 0;
	uint32 mLastSyncDriveTimeSubCycles = 0;
	uint32 mSubCycleAccum = 0;
	uint8 *mpCoProcWinBase = nullptr;

	const bool mb1050;
	const DeviceType mDeviceType;
	uint8 mDriveId = 0;
	uint32 mClockDivisor = 0;

	bool mbFirmwareUsable = false;
	bool mbSoundsEnabled = false;
	bool mbSlowSwitch = false;
	bool mbFastSlowToggle = false;
	bool mbWPToggle = false;
	bool mbWPEnable = false;
	bool mbWPDisable = false;

	ATDiskDriveAudioPlayer mAudioPlayer;

	uint32 mLastStepSoundTime = 0;
	uint32 mLastStepPhase = 0;
	uint8 mDiskChangeState = 0;

	ATCoProcReadMemNode mReadNodeFDCRAM {};
	ATCoProcReadMemNode mReadNodeRIOTRAM {};
	ATCoProcReadMemNode mReadNodeRIOTRegisters {};
	ATCoProcReadMemNode mReadNodeROMBankSwitch {};
	ATCoProcReadMemNode mReadNodeFastSlowToggle {};
	ATCoProcReadMemNode mReadNodeWriteProtectToggle {};
	ATCoProcWriteMemNode mWriteNodeFDCRAM {};
	ATCoProcWriteMemNode mWriteNodeRIOTRAM {};
	ATCoProcWriteMemNode mWriteNodeRIOTRegisters {};
	ATCoProcWriteMemNode mWriteNodeROMBankSwitch {};
	ATCoProcWriteMemNode mWriteNodeFastSlowToggle {};
	ATCoProcWriteMemNode mWriteNodeWriteProtectToggle {};

	ATScheduler mDriveScheduler;
	ATCoProc6502 mCoProc;

	ATFDCEmulator mFDC;
	ATRIOT6532Emulator mRIOT;

	vdfastvector<ATCPUHistoryEntry> mHistory;

	struct TransmitEvent {
		uint32 mTime : 31;
		uint32 mBit : 1;
	};

	// How long of a delay there is in computer cycles between the drive sending SIO data
	// and the computer receiving it. This is non-realistic but allows us to batch drive
	// execution.
	static constexpr uint32 kTransmitLatency = 128;

	uint32 mTransmitHead = 0;
	uint32 mTransmitTail = 0;

	static constexpr uint32 kTransmitQueueSize = kTransmitLatency;
	static constexpr uint32 kTransmitQueueMask = kTransmitQueueSize - 1;
	TransmitEvent mTransmitQueue[kTransmitQueueSize] = {};

	VDALIGN(4) uint8 mRAM[0x4100] = {};
	VDALIGN(4) uint8 mROM[0x4000] = {};
	VDALIGN(4) uint8 mDummyWrite[0x100] = {};
	VDALIGN(4) uint8 mDummyRead[0x100] = {};
	
	vdfunction<void(bool)> mpStepHandler = {};
	bool mbStepOut = false;
	bool mbStepNotifyPending = false;
	bool mbStepNotifyPendingBP = false;
	uint32 mStepStartSubCycle = 0;
	uint16 mStepOutS = 0;

	ATDebugTargetBreakpointsImpl mBreakpointsImpl;
};

#endif
