//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#ifndef AT_DISK_H
#define AT_DISK_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/time.h>
#include <vd2/system/VDString.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/devicesio.h>
#include <at/atcore/scheduler.h>
#include <at/atio/diskimage.h>
#include "diskinterface.h"
#include "pokey.h"

class ATPokeyEmulator;
class ATAudioSamplePlayer;

class ATCPUEmulatorMemory;
class VDFile;
class IVDRandomAccessStream;
class IATDeviceIndicatorManager;
class IATUIRenderer;

struct ATTraceContext;
class ATTraceChannelFormatted;

enum ATMediaWriteMode : uint8;
enum class ATSoundId : uint32;

enum ATDiskEmulationMode {
	kATDiskEmulationMode_Generic,
	kATDiskEmulationMode_FastestPossible,
	kATDiskEmulationMode_810,
	kATDiskEmulationMode_1050,
	kATDiskEmulationMode_XF551,
	kATDiskEmulationMode_USDoubler,
	kATDiskEmulationMode_Speedy1050,
	kATDiskEmulationMode_IndusGT,
	kATDiskEmulationMode_Happy1050,
	kATDiskEmulationMode_1050Turbo,
	kATDiskEmulationMode_Generic57600,
	kATDiskEmulationMode_Happy810,
	kATDiskEmulationModeCount
};

class ATDiskEmulator final
	: public IATDeviceSIO
	, public IATSchedulerCallback
	, public IATDiskInterfaceClient
{
public:
	ATDiskEmulator();
	~ATDiskEmulator();

	void Init(int unit, ATDiskInterface *dif, ATScheduler *sched, ATScheduler *slowsched, ATAudioSamplePlayer *mixer);
	void Shutdown();

	void Rename(int unit);

	bool IsEnabled() const { return mbEnabled; }

	void SetEnabled(bool enabled);

	void SetEmulationMode(ATDiskEmulationMode mode);
	ATDiskEmulationMode GetEmulationMode() { return mEmuMode; }

	void Reset();

	void SetForcedPhantomSector(uint16 sector, uint8 index, int order);
	int GetForcedPhantomSector(uint16 sector, uint8 index);

	void SetTraceContext(ATTraceContext *context);

public:
	void OnScheduledEvent(uint32 id) override;

public:
	void InitSIO(IATDeviceSIOManager *mgr) override;
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	void OnSerialAbortCommand() override;
	void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	void OnSerialFence(uint32 id) override; 
	CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

public:		// IATDiskInterfaceClient
	void OnDiskChanged(bool mediaRemoved) override;
	void OnWriteModeChanged() override;
	void OnTimingModeChanged() override;
	void OnAudioModeChanged() override;

protected:
	void UpdateAccelTimeSkew();
	void InitSectorInfoArrays();
	void SetupTransferSpeed(bool highSpeed);
	void BeginTransferACKCmd();
	void BeginTransferACK();
	void BeginTransferComplete();
	void BeginTransferError();
	void BeginTransferNAK();
	void SendResult(bool successful, uint32 length);
	void Send(uint32 length);
	void BeginReceive(uint32 len);
	void WarpOrDelay(uint32 cycles, uint32 minCycles = 0);
	void Wait(uint32 nextState);
	void EndCommand();
	void AbortCommand();
	void UpdateRotationalCounter();

	void ProcessCommand();
	void ProcessUnsupportedCommand();

	void ProcessCommandStatus();
	bool ProcessCommandReadWriteCommon(bool isWrite);
	void ProcessCommandRead();
	void ProcessCommandWrite();
	void ProcessCommandReadPERCOMBlock();
	void ProcessCommandWritePERCOMBlock();
	void ProcessCommandHappy();
	void ProcessCommandHappyQuiet();
	void ProcessCommandHappyRAMTest();
	void ProcessCommandHappyHeadPosTest();
	void ProcessCommandHappyRPMTest();

	void ProcessCommandExecuteIndusGT();
	void ProcessCommandGetHighSpeedIndex();
	void ProcessCommandFormat();

	void ComputeGeometry();
	void ComputePERCOMBlock();
	void ComputeSupportedProfile();
	void UpdateHighSpeedTiming();
	bool SetPERCOMData(const uint8 *data);
	void TurnOffMotor();
	bool TurnOnMotor(uint32 delay = 0);
	void PlaySeekSound(uint32 initialDelay, uint32 trackCount);

	IATDeviceSIOManager *mpSIOMgr = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	ATAudioSamplePlayer *mpAudioSyncMixer = nullptr;
	int		mUnit = 0;

	ATEvent		*mpMotorOffEvent = nullptr;

	uint32	mLastRotationUpdateCycle = 0;
	uint32	mLastAccelTimeSkew = 0;
	bool	mbReceiveChecksumOK = false;
	uint32	mTransferLength = 0;
	uint32	mTransferCompleteRotPos = 0;
	uint8	mFDCStatus = 0;
	uint8	mOriginalDevice = 0;
	uint8	mOriginalCommand = 0;
	uint8	mActiveCommand = 0;
	bool	mbActiveCommandHighSpeed = false;
	bool	mbActiveCommandWait = false;
	uint32	mActiveCommandState = 0;
	uint32	mActiveCommandSector = 0;
	sint32	mActiveCommandPhysSector = 0;
	float	mActiveCommandStartRotPos = 0;
	uint8	mCustomCodeState = 0;
	uint32	mPhantomSectorCounter = 0;
	uint32	mRotationalCounter = 0;
	uint32	mRotations = 0;
	uint32	mCurrentTrack = 0;
	uint32	mSectorsPerTrack = 0;
	uint32	mTrackCount = 0;
	uint32	mSideCount = 0;
	bool	mbMFM = false;

	bool	mbFormatEnabled = false;
	bool	mbWriteEnabled = false;

	bool	mbCommandMode = false;
	bool	mbCommandValid = false;
	bool	mbCommandFrameHighSpeed = false;
	bool	mbEnabled = false;
	bool	mbDriveSoundsEnabled = false;
	bool	mbAccurateSectorTiming = false;
	bool	mbAccurateSectorPrediction = false;
	bool	mbLastOpError = false;

	int		mBootSectorCount = 0;
	int		mTotalSectorCount = 0;
	int		mSectorSize = 0;
	uint32	mLastSector = 0;

	ATSoundId	mRotationSoundId = {};

	uint8	mPERCOM[12] = {};
	int		mFormatSectorSize = 0;
	int		mFormatSectorCount = 0;
	int		mFormatBootSectorCount = 0;

	ATDiskEmulationMode mEmuMode = kATDiskEmulationMode_Generic;
	bool	mbSupportedNotReady = false;
	bool	mbSupportedCmdHighSpeed = false;
	bool	mbSupportedCmdFrameHighSpeed = false;
	bool	mbSupportedCmdPERCOM = false;
	bool	mbSupportedCmdFormatSkewed = false;
	bool	mbSupportedCmdGetHighSpeedIndex = false;
	uint8	mHighSpeedIndex = 0;
	uint8	mHighSpeedCmdIndex = 0;
	uint8	mHighSpeedCmdFrameRateLo = 0;
	uint8	mHighSpeedCmdFrameRateHi = 0;
	uint8	mHighSpeedDataFrameRateLo = 0;
	uint8	mHighSpeedDataFrameRateHi = 0;
	uint32	mCyclesPerSIOByte = 1;
	uint32	mCyclesPerSIOBit = 1;
	uint32	mCyclesPerSIOByteHighSpeed = 1;
	uint32	mCyclesPerSIOBitHighSpeed = 1;
	uint32	mCyclesToACKSent = 1;
	uint32	mCyclesToFDCCommand = 1;
	uint32	mCyclesToCompleteAccurate = 1;
	uint32	mCyclesToCompleteAccurateED = 1;
	uint32	mCyclesToCompleteFast = 1;
	uint32	mCyclesPerDiskRotation = 1;
	uint32	mCyclesPerTrackStep = 1;
	uint32	mCyclesForHeadSettle = 1;
	uint32	mCyclesCEToDataFrame = 0;
	uint32	mCyclesCEToDataFramePBDiv256 = 0;
	uint32	mCyclesCEToDataFrameHighSpeed = 0;
	uint32	mCyclesCEToDataFrameHighSpeedPBDiv256 = 0;
	bool	mbSeekHalfTracks = false;
	bool	mbRetryMode1050 = false;
	bool	mbReverseOnForwardSeeks = false;

	ATDiskInterface *mpDiskInterface = nullptr;
	ATTraceContext *mpTraceContext = nullptr;
	ATTraceChannelFormatted *mpTraceChannel = nullptr;
	uint64	mTraceCommandStartTime = 0;

	struct ExtPhysSector {
		sint8	mForcedOrder;
	};

	typedef vdfastvector<ExtPhysSector> ExtPhysSectors;
	ExtPhysSectors mExtPhysSectors;

	struct ExtVirtSector {
		uint32	mPhantomSectorCounter;
	};
	typedef vdfastvector<ExtVirtSector> ExtVirtSectors;
	ExtVirtSectors mExtVirtSectors;

	uint32	mWeakBitLFSR = 0;

	uint8	mSendPacket[8192 + 16] = {};
	uint8	mReceivePacket[8192 + 16] = {};
	uint8	mDriveRAM[8192] = {};
};

#endif
