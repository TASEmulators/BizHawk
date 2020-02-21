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

#ifndef f_AT_FDC_H
#define f_AT_FDC_H

#include <vd2/system/function.h>
#include <at/atio/diskimage.h>
#include <at/atcore/scheduler.h>

class ATDiskInterface;
class ATConsoleOutput;

enum ATFDCWPOverride {
	kATFDCWPOverride_None,
	kATFDCWPOverride_Invert,
	kATFDCWPOverride_WriteProtect,
	kATFDCWPOverride_WriteEnable,
};

class ATFDCEmulator final : public IATSchedulerCallback {
public:
	enum : uint32 { kTypeID = 'FDC ' };

	enum Type {
		// 1771 (810): FM only, head load
		kType_1771,

		// 2793/2797 (1050, Indus GT): MFM, head load
		kType_279X,

		// 1770 (XF551, Indus GT), MFM, motor on
		kType_1770,

		// 1772 (XF551), MFM, motor on; faster seek times than 1770
		kType_1772,
	};

	ATFDCEmulator();
	~ATFDCEmulator();

	void SetOnDrqChange(const vdfunction<void(bool)>& fn) {
		mpFnDrqChange = fn;
	}

	void SetOnIrqChange(const vdfunction<void(bool)>& fn) {
		mpFnIrqChange = fn;
	}

	void SetOnStep(const vdfunction<void(bool)>& fn) {
		mpFnStep = fn;
	}

	void SetOnMotorChange(const vdfunction<void(bool)>& fn) {
		mpFnMotorChange = fn;
	}

	void SetOnWriteEnabled(const vdfunction<void()>& fn) {
		mpFnWriteEnabled = fn;
	}

	void SetOnHeadLoadChange(const vdfunction<void(bool)>& fn) {
		mpFnHeadLoadChange = fn;
	}

	bool GetIrqStatus() const { return mbIrqPending; }
	bool GetDrqStatus() const { return mbDataReadPending || mbDataWritePending; }

	void Init(ATScheduler *sch, float rpm, Type type);
	void Shutdown();

	void DumpStatus(ATConsoleOutput& out);

	void Reset();

	void SetAccurateTimingEnabled(bool enabled);
	void SetMotorRunning(bool running);
	void SetCurrentTrack(uint32 halfTracks, bool track0);
	void SetSide(bool side2);
	void SetDensity(bool mfm);
	void SetSpeeds(float rpm, float baserpm, bool doubleClock);

	// Force write protect sense on even if the disk is write enabled.
	// This is used to emulate the obscuring of the write protect sector while a
	// disk is being changed. It is later affected by the second override.
	void SetWriteProtectOverride(bool override) { mbWriteProtectOverride = override; }

	// Second write protect override, used for override at the drive level.
	// This used to emulate the Happy 1050's write/protect/normal switch.
	// The value is -1/0/1 for off, force false, and force true.
	ATFDCWPOverride GetWriteProtectOverride2() const { return mWriteProtectOverride2; }
	void SetWriteProtectOverride2(ATFDCWPOverride mode) { mWriteProtectOverride2 = mode; }

	void SetAutoIndexPulse(bool enabled);
	void SetDiskImage(IATDiskImage *image, bool diskReady);
	void SetDiskInterface(ATDiskInterface *diskIf);

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

	void OnIndexPulse(bool asserted);

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	enum State : uint32 {
		kState_Idle,
		kState_BeginCommand,
		kState_DispatchCommand,
		kState_EndCommand,
		kState_EndCommand2,
		kState_Restore,
		kState_Restore_Step,
		kState_Seek,
		kState_Step,
		kState_StepIn,
		kState_StepOut,
		kState_ReadSector,
		kState_ReadSector_TransferFirstByte,
		kState_ReadSector_TransferFirstByteNever,
		kState_ReadSector_TransferByte,
		kState_ReadSector_TransferComplete,
		kState_WriteSector,
		kState_WriteSector_InitialDrq,
		kState_WriteSector_CheckInitialDrq,
		kState_WriteSector_TransferByte,
		kState_WriteSector_TransferComplete,
		kState_ReadAddress,
		kState_ReadTrack,
		kState_WriteTrack,
		kState_WriteTrack_WaitHeadLoad,
		kState_WriteTrack_WaitIndexMarks,
		kState_WriteTrack_TransferByte,
		kState_WriteTrack_InitialDrqTimeout,
		kState_WriteTrack_Complete,
		kState_ForceInterrupt,
	};

	void UpdateIndexPulse();
	void AbortCommand();
	void RunStateMachine();
	void SetTransition(State nextState, uint32 ticks);
	void UpdateRotationalPosition();
	void UpdateAutoIndexPulse();
	void SetMotorIdleTimer();
	void ClearMotorIdleTimer();
	void UpdateDensity();
	void FinalizeWriteTrack();
	uint32 GetSelectedVSec(uint32 sector) const;
	bool ModifyWriteProtect(bool wp) const;

	enum : uint32 {
		kEventId_StateMachine = 1,
		kEventId_AutoIndexOff,
		kEventId_AutoIndexOn,
		kEventId_AutoMotorIdle
	};

	ATScheduler *mpScheduler = nullptr;
	ATEvent *mpStateEvent = nullptr;
	ATEvent *mpAutoIndexOnEvent = nullptr;
	ATEvent *mpAutoIndexOffEvent = nullptr;
	ATEvent *mpAutoMotorIdleEvent = nullptr;

	State mState {};

	uint8 mRegCommand = 0;
	uint8 mRegTrack = 0;
	uint8 mRegSector = 0;
	uint8 mRegData = 0;
	uint8 mRegStatus = 0;
	bool mbRegStatusTypeI = false;
	bool mbDataReadPending = false;
	bool mbDataWritePending = false;
	bool mbIrqPending = false;

	uint32 mOpCount = 0;
	uint32 mTransferIndex = 0;
	uint32 mTransferLength = 0;
	uint8 mActiveSectorStatus = 0;
	uint8 mActivePhysSectorStatus = 0;
	uint32 mActivePhysSector = 0;
	uint32 mActiveOpIndexMarks = 0;

	uint32 mRotPos = 0;
	uint32 mRotations = 0;
	uint64 mRotTimeBase = 0;
	uint32 mIdleIndexPulses = 0;
	bool mbMotorRunning = false;
	bool mbMotorEnabled = false;
	bool mbHeadLoaded = false;
	bool mbManualIndexPulse = false;
	bool mbAutoIndexPulse = false;
	bool mbAutoIndexPulseEnabled = false;
	bool mbIndexPulse = false;
	bool mbTrack0 = false;
	bool mbSide2 = false;
	bool mbDiskReady = false;
	bool mbMFM = false;
	bool mbDoubleClock = false;
	bool mbWriteProtectOverride = false;
	ATFDCWPOverride mWriteProtectOverride2 = {};

	uint32 mWeakBitLFSR = 1;
	uint32 mPhysHalfTrack = 0;
	vdrefptr<IATDiskImage> mpDiskImage;
	ATDiskInterface *mpDiskInterface = nullptr;

	ATDiskGeometryInfo mDiskGeometry {};

	bool mbUseAccurateTiming = false;
	Type mType = {};
	uint32 mCyclesPerRotation = 0;
	uint32 mCyclesPerByteFM = 1;
	uint32 mCyclesPerByteMFM = 1;
	uint32 mCyclesPerByte = 1;
	uint32 mCyclesPerIndexPulse = 1;
	uint32 mCycleStepTable[4] = {};

	vdfunction<void(bool)> mpFnDrqChange;
	vdfunction<void(bool)> mpFnIrqChange;
	vdfunction<void(bool)> mpFnStep;
	vdfunction<void(bool)> mpFnMotorChange;
	vdfunction<void()> mpFnWriteEnabled;
	vdfunction<void(bool)> mpFnHeadLoadChange;

	vdblock<uint8> mWriteTrackBuffer;
	uint32 mWriteTrackIndex;

	uint8 mTransferBuffer[4096] = {};

	static constexpr uint32 kMaxBytesPerTrackFM = 3256;
	static constexpr uint32 kMaxBytesPerTrackMFM = 6512;
	static constexpr uint32 kWriteTrackBufferSize = kMaxBytesPerTrackMFM * 2 + 1024;	// +1024 in case data sector is incomplete at end
};

#endif
