//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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

#ifndef f_AT_DISKDRIVEATR8000_H
#define f_AT_DISKDRIVEATR8000_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/devicediskdrive.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparent.h>
#include <at/atcore/deviceprinter.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicesioimpl.h>
#include <at/atcpu/coz80.h>
#include <at/atcpu/breakpoints.h>
#include <at/atcpu/history.h>
#include <at/atcpu/memorymap.h>
#include <at/atdebugger/breakpointsimpl.h>
#include <at/atdebugger/target.h>
#include <at/atcore/scheduler.h>
#include <at/atemulation/ctc.h>
#include "fdc.h"
#include "diskdrivefullbase.h"
#include "diskinterface.h"

class ATIRQController;

class ATDeviceDiskDriveATR8000 final : public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceFirmware
	, public IATDeviceDiskDrive
	, public ATDeviceSIO
	, public IATDeviceButtons
	, public IATDevicePrinter
	, public IATDeviceParent
	, public IATDeviceDebugTarget
	, public IATDebugTarget
	, public IATDebugTargetHistory
	, public IATDebugTargetExecutionControl
	, public IATCPUBreakpointHandler
	, public IATSchedulerCallback
	, public IATDeviceRawSIO
{
public:
	ATDeviceDiskDriveATR8000();
	~ATDeviceDiskDriveATR8000();

	void *AsInterface(uint32 iid) override;

	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettingsBlurb(VDStringW& buf) override;
	void GetSettings(ATPropertySet & settings) override;
	bool SetSettings(const ATPropertySet & settings) override;
	void Init() override;
	void Shutdown() override;
	uint32 GetComputerPowerOnDelay() const override;
	void WarmReset() override;
	void ComputerColdReset() override;
	void PeripheralColdReset() override;

	void DeviceReset();

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
	uint32 GetSupportedButtons() const override;
	bool IsButtonDepressed(ATDeviceButton idx) const override;
	void ActivateButton(ATDeviceButton idx, bool state) override;

public:		// IATDevicePrinter
	void SetPrinterOutput(IATPrinterOutput *out) override;

public:
	IATDeviceBus *GetDeviceBus(uint32 index) override;

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
	double GetTimestampFrequency() const override { return 4000000.0; }

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

public:
	void OnDiskChanged(uint32 index, bool mediaRemoved);
	void OnWriteModeChanged(uint32 index);
	void OnTimingModeChanged(uint32 index);
	void OnAudioModeChanged(uint32 index);

protected:
	void CancelStep();

	void Sync();
	void AccumSubCycles();

	void OnTransmitEvent();
	void QueueNextTransmitEvent();
	void AddTransmitEdge(uint32 polarity);

	uint8 OnDebugReadFDC(uint32 addr) const;
	uint8 OnReadFDC(uint32 addr);
	void OnWriteFDC(uint32 addr, uint8 val);
	uint8 OnReadPort(uint8 addr);
	void OnWritePort(uint8 addr, uint8 val);

	uint8 OnGetIntVector();
	void OnIntAck();
	void OnHaltChange(bool halted);
	void OnCTCInterrupt(sint32 vec);
	void OnFDCStep(bool inward);
	void OnFDCDrq(bool active);
	void OnFDCIrq(bool active);
	void OnFDCMotorChange(bool enabled);

	void PlayStepSound();
	void UpdateMemoryMap();
	void UpdateRotationStatus();
	void UpdateDiskStatus();
	void UpdateWriteProtectStatus();
	void UpdateCTCTrigger0Input();
	void UpdateIndexPulseMode();
	void UpdateFDCSpeed();
	void SelectDrives(uint8 mask);

	static constexpr uint32 kNumDrives = 4;

	enum {
		kEventId_Run = 1,
		kEventId_Transmit,
		kEventId_DriveReceiveBit,
		kEventId_DriveDiskChange0,		// 4 events, one per drive
	};

	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	ATEvent *mpRunEvent = nullptr;
	ATEvent *mpTransmitEvent = nullptr;
	ATEvent *mpEventDriveReceiveBit = nullptr;
	ATEvent *mpEventDriveTransmitBit = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;
	IATDiskDriveManager *mpDiskDriveManager = nullptr;

	ATFirmwareManager *mpFwMgr = nullptr;
	bool mbFirmwareUsable = false;

	static constexpr uint32 kDiskChangeStepMS = 50;

	uint8 mDriveId = 0;

	uint32 mReceiveShiftRegister = 0;
	uint32 mReceiveTimingAccum = 0;
	uint32 mReceiveTimingStep = 0;

	uint32 mTransmitResetCounter = 0;
	uint32 mTransmitCyclesPerBit = 0;
	uint8 mTransmitShiftRegister = 0;
	uint8 mTransmitPhase = 0;
	bool mbTransmitCurrentBit = true;
	bool mbCommandState = false;
	bool mbDirectTransmitOutput = true;
	bool mbTrigger0SelectData = false;

	// The ATR8000 can select multiple drives at once, and in fact the firmware does so
	// for the power-on restore. Therefore, we must also track the multi-select case.
	uint8 mSelectedDrives = 0;
	uint8 mAvailableDrives = 0;

	uint32 mLastSync = 0;
	uint32 mLastSyncDriveTime = 0;
	uint32 mLastSyncDriveTimeSubCycles = 0;
	uint32 mSubCycleAccum = 0;
	uint8 *mpCoProcWinBase = nullptr;

	uint32 mClockDivisor = 0;
	uint8 mPendingIntVector = 0;

	bool mbSoundsEnabled = false;
	bool mbIndexPulseEnabled = true;
	bool mbIndexPulseDisabled = false;
	bool mbFastClock = false;
	bool mbMotorRunning = false;
	bool mbROMEnabled = false;

	ATDiskDriveAudioPlayer mAudioPlayer;
	uint32 mLastStepSoundTime = 0;
	uint32 mLastStepPhase = 0;

	enum DriveType : uint32 {
		kDriveType_None,
		kDriveType_5_25,
		kDriveType_8,
	};

	struct Drive : public IATDiskInterfaceClient {
		ATDeviceDiskDriveATR8000 *mpParent = nullptr;
		uint32 mIndex = 0;

		ATDiskInterface *mpDiskInterface = nullptr;
		ATEvent *mpEventDriveDiskChange = nullptr;
		uint32 mCurrentTrack = 0;
		uint8 mDiskChangeState = 0;

		DriveType mType = kDriveType_None;

		void OnDiskChanged(bool mediaRemoved) override;
		void OnWriteModeChanged() override;
		void OnTimingModeChanged() override;
		void OnAudioModeChanged() override;
	} mDrives[kNumDrives];

	ATScheduler mDriveScheduler;

	ATFDCEmulator mFDC;

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
	
	vdfunction<void(bool)> mpStepHandler = {};
	bool mbStepOut = false;
	bool mbStepNotifyPending = false;
	bool mbStepNotifyPendingBP = false;
	uint32 mStepStartSubCycle = 0;
	uint16 mStepOutSP = 0;

	uint8 mPrinterData = 0;
	bool mbPrinterStrobeAsserted = false;
	IATPrinterOutput *mpPrinterOutput = nullptr;

	ATCTCEmulator mCTC;
	
	class SerialPort;
	SerialPort *mpSerialPort = nullptr;

	uintptr mReadMapBackup[256] = {};
	uintptr mWriteMapBackup[256] = {};

	VDALIGN(4) uint8 mRAM[0x10000] = {};
	VDALIGN(4) uint8 mROM[0x1000] = {};

	ATCoProcZ80 mCoProc;

	ATDebugTargetBreakpointsImpl mBreakpointsImpl;
};

#endif
