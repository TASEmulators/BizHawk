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

#include <stdafx.h>
#include <vd2/system/hash.h>
#include <vd2/system/int128.h>
#include <vd2/system/math.h>
#include <at/atcore/audiosource.h>
#include <at/atcore/logging.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/deviceserial.h>
#include "audiosampleplayer.h"
#include "diskdrivexf551.h"
#include "memorymanager.h"
#include "firmwaremanager.h"
#include "debuggerlog.h"

extern ATLogChannel g_ATLCDiskEmu;

void ATCreateDeviceDiskDriveXF551(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceDiskDriveXF551> p(new ATDeviceDiskDriveXF551);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefDiskDriveXF551 = { "diskdrivexf551", "diskdrivexf551", L"XF551 disk drive (full emulation)", ATCreateDeviceDiskDriveXF551 };

ATDeviceDiskDriveXF551::ATDeviceDiskDriveXF551() {
	mBreakpointsImpl.BindBPHandler(mCoProc);
	mBreakpointsImpl.SetStepHandler(this);

	mCoProc.SetProgramBanks(mROM[0], mROM[1]);
}

ATDeviceDiskDriveXF551::~ATDeviceDiskDriveXF551() {
}

void *ATDeviceDiskDriveXF551::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceFirmware::kTypeID: return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceDiskDrive::kTypeID: return static_cast<IATDeviceDiskDrive *>(this);
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
		case IATDeviceAudioOutput::kTypeID: return static_cast<IATDeviceAudioOutput *>(&mAudioPlayer);
		case IATDeviceDebugTarget::kTypeID: return static_cast<IATDeviceDebugTarget *>(this);
		case IATDebugTargetBreakpoints::kTypeID: return static_cast<IATDebugTargetBreakpoints *>(&mBreakpointsImpl);
		case IATDebugTargetHistory::kTypeID: return static_cast<IATDebugTargetHistory *>(this);
		case IATDebugTargetExecutionControl::kTypeID: return static_cast<IATDebugTargetExecutionControl *>(this);
		case ATFDCEmulator::kTypeID: return &mFDC;
	}

	return nullptr;
}

void ATDeviceDiskDriveXF551::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefDiskDriveXF551;
}

void ATDeviceDiskDriveXF551::GetSettingsBlurb(VDStringW& buf) {
	buf.sprintf(L"D%u:", mDriveId + 1);
}

void ATDeviceDiskDriveXF551::GetSettings(ATPropertySet& settings) {
	settings.SetUint32("id", mDriveId);
}

bool ATDeviceDiskDriveXF551::SetSettings(const ATPropertySet& settings) {
	uint32 newDriveId = settings.GetUint32("id", mDriveId) & 3;

	if (mDriveId != newDriveId) {
		mDriveId = newDriveId;
		return false;
	}

	return true;
}

void ATDeviceDiskDriveXF551::Init() {
	mDriveScheduler.SetRate(VDFraction(8333333, 15));

	mCoProc.SetPortReadHandler([this](uint8 port, uint8 output) -> uint8 { return OnReadPort(port, output); });
	mCoProc.SetPortWriteHandler([this](uint8 port, uint8 data) { OnWritePort(port, data); });
	mCoProc.SetXRAMReadHandler([this](uint8 addr) -> uint8 { return OnReadXRAM(); });
	mCoProc.SetXRAMWriteHandler([this](uint8 addr, uint8 data) { OnWriteXRAM(data); });
	mCoProc.SetT0ReadHandler([this]() { return OnReadT0(); });
	mCoProc.SetT1ReadHandler([this]() { return OnReadT1(); });

	mFDC.Init(&mDriveScheduler, 300.0f, ATFDCEmulator::kType_1770);
	mFDC.SetAutoIndexPulse(true);
	mFDC.SetDiskInterface(mpDiskInterface);
	mFDC.SetOnDrqChange([this](bool drq) {  });
	mFDC.SetOnIrqChange([this](bool irq) {  });
	mFDC.SetOnStep([this](bool inward) { OnFDCStep(inward); });
	mFDC.SetOnMotorChange([this](bool active) { OnFDCMotorChange(active); });

	mDriveScheduler.UnsetEvent(mpEventDriveDiskChange);
	mDiskChangeState = 0;
	OnDiskChanged(false);

	OnWriteModeChanged();
	OnTimingModeChanged();
	OnAudioModeChanged();

	UpdateRotationStatus();
}

void ATDeviceDiskDriveXF551::Shutdown() {
	mAudioPlayer.Shutdown();

	mDriveScheduler.UnsetEvent(mpEventDriveReceiveBit);

	if (mpSlowScheduler) {
		mpSlowScheduler->UnsetEvent(mpRunEvent);
		mpSlowScheduler = nullptr;
	}

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpTransmitEvent);
		mpScheduler = nullptr;
	}

	mpFwMgr = nullptr;

	if (mpSIOMgr) {
		if (!mbTransmitCurrentBit) {
			mbTransmitCurrentBit = true;
			mpSIOMgr->SetRawInput(true);
		}

		mpSIOMgr->RemoveRawDevice(this);
		mpSIOMgr = nullptr;
	}

	if (mpDiskInterface) {
		mpDiskInterface->RemoveClient(this);
		mpDiskInterface = nullptr;
	}

	mpDiskDriveManager = nullptr;
}

void ATDeviceDiskDriveXF551::WarmReset() {
	mLastSync = ATSCHEDULER_GETTIME(mpScheduler);
	mLastSyncDriveTime = ATSCHEDULER_GETTIME(&mDriveScheduler);
	mLastSyncDriveTimeSubCycles = 0;

	// If the computer resets, its transmission is interrupted.
	mDriveScheduler.UnsetEvent(mpEventDriveReceiveBit);
}

void ATDeviceDiskDriveXF551::ComputerColdReset() {
	WarmReset();
}

void ATDeviceDiskDriveXF551::PeripheralColdReset() {
	mFDC.Reset();

	// clear transmission
	mDriveScheduler.UnsetEvent(mpEventDriveTransmitBit);

	mpScheduler->UnsetEvent(mpTransmitEvent);

	if (!mbTransmitCurrentBit) {
		mbTransmitCurrentBit = true;
		mpSIOMgr->SetRawInput(true);
	}

	mTransmitHead = 0;
	mTransmitTail = 0;
	mTransmitCyclesPerBit = 0;
	mTransmitPhase = 0;
	mTransmitShiftRegister = 0;

	memset(mTransmitQueue, 0, sizeof mTransmitQueue);

	mCommandQueue.clear();
	mbDriveCommandState = false;
	mDriveScheduler.UnsetEvent(mpEventDriveCommandChange);
	
	mActiveStepperPhases = 0;

	mbForcedIndexPulse = false;

	mbDirectReceiveOutput = true;
	mbDirectTransmitOutput = true;

	// start the disk drive on a track other than 0/20/39, just to make things interesting
	mCurrentTrack = 20;
	mFDC.SetCurrentTrack(mCurrentTrack, false);

	mbMotorRunning = false;
	mFDC.SetMotorRunning(false);
	mFDC.SetDensity(false);
	mFDC.SetWriteProtectOverride(false);

	mbExtendedRAMEnabled = false;

	mCoProc.ColdReset();

	// 8.33MHz master clock, 15 cycles per machine cycle in 8040, .9 fixed point
	mClockDivisor = VDRoundToInt((512.0 / 8333333.3333 * 15.0) * mpScheduler->GetRate().asDouble());

	WarmReset();
}

void ATDeviceDiskDriveXF551::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;

	mpSlowScheduler->SetEvent(1, this, 1, mpRunEvent);
}

void ATDeviceDiskDriveXF551::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMgr = fwman;

	ReloadFirmware();
}

bool ATDeviceDiskDriveXF551::ReloadFirmware() {
	const uint64 id = mpFwMgr->GetFirmwareOfType(kATFirmwareType_XF551, true);
	
	const vduint128 oldHash = VDHash128(mROM, sizeof mROM);

	uint8 firmware[4096] = {};

	uint32 len = 0;
	mpFwMgr->LoadFirmware(id, firmware, 0, sizeof firmware, nullptr, &len, nullptr, nullptr, &mbFirmwareUsable);

	memcpy(mROM[0], firmware, 2048);
	memcpy(mROM[1], firmware + 2048, 2048);
	mROM[0][2048] = mROM[0][0];
	mROM[1][2048] = mROM[1][0];

	const vduint128 newHash = VDHash128(mROM, sizeof mROM);

	return oldHash != newHash;
}

const wchar_t *ATDeviceDiskDriveXF551::GetWritableFirmwareDesc(uint32 idx) const {
	return nullptr;
}

bool ATDeviceDiskDriveXF551::IsWritableFirmwareDirty(uint32 idx) const {
	return false;
}

void ATDeviceDiskDriveXF551::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
}

ATDeviceFirmwareStatus ATDeviceDiskDriveXF551::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATDeviceDiskDriveXF551::InitDiskDrive(IATDiskDriveManager *ddm) {
	mpDiskDriveManager = ddm;
	mpDiskInterface = ddm->GetDiskInterface(mDriveId);
	mpDiskInterface->AddClient(this);
}

void ATDeviceDiskDriveXF551::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddRawDevice(this);
}

IATDebugTarget *ATDeviceDiskDriveXF551::GetDebugTarget(uint32 index) {
	if (index == 0)
		return this;

	return nullptr;
}

const char *ATDeviceDiskDriveXF551::GetName() {
	return "Disk Drive CPU";
}

ATDebugDisasmMode ATDeviceDiskDriveXF551::GetDisasmMode() {
	return kATDebugDisasmMode_8048;
}

void ATDeviceDiskDriveXF551::GetExecState(ATCPUExecState& state) {
	mCoProc.GetExecState(state);
}

void ATDeviceDiskDriveXF551::SetExecState(const ATCPUExecState& state) {
	mCoProc.SetExecState(state);
}

sint32 ATDeviceDiskDriveXF551::GetTimeSkew() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	const uint32 cycles = (t - mLastSync) + ((mCoProc.GetCyclesLeft() * mClockDivisor + mSubCycleAccum + 511) >> 9);

	return -(sint32)cycles;
}

uint8 ATDeviceDiskDriveXF551::ReadByte(uint32 address) {
	return DebugReadByte(address);
}

void ATDeviceDiskDriveXF551::ReadMemory(uint32 address, void *dst, uint32 n) {
	return DebugReadMemory(address, dst, n);
}

uint8 ATDeviceDiskDriveXF551::DebugReadByte(uint32 address) {
	if (address >= 0x1100)
		return 0;

	if (address < 0x0800)
		return mROM[0][address];
	else if (address < 0x1000)
		return mROM[1][address - 0x0800];

	return mCoProc.ReadByte(address);
}

void ATDeviceDiskDriveXF551::DebugReadMemory(uint32 address, void *dst, uint32 n) {
	while(n) {
		if (address >= 0x1100) {
			memset(dst, 0, n);
			break;
		}

		uint32 tc = 256 - (address & 0xff);
		if (tc > n)
			tc = n;

		if (address < 0x1000) {
			const uint8 *src = &mROM[address & 0x800 ? 1 : 0][address & 0x7FF];

			memcpy(dst, src, tc);
			address += tc;
		} else {
			for(uint32 i=0; i<tc; ++i)
				((uint8 *)dst)[i] = mCoProc.ReadByte((uint8)address++);
		}

		n -= tc;
		dst = (char *)dst + tc;
	}

}

void ATDeviceDiskDriveXF551::WriteByte(uint32 address, uint8 value) {
	if (address >= 0x100)
		return;

	mCoProc.WriteByte(address, value);
}

void ATDeviceDiskDriveXF551::WriteMemory(uint32 address, const void *src, uint32 n) {
	while(n) {
		if (address >= 0x1100)
			break;

		if (address >= 0x1000)
			mCoProc.WriteByte((uint8)address, *(const uint8 *)src);

		++address;
		src = (const uint8 *)src + 1;
		--n;
	}
}

bool ATDeviceDiskDriveXF551::GetHistoryEnabled() const {
	return !mHistory.empty();
}

void ATDeviceDiskDriveXF551::SetHistoryEnabled(bool enable) {
	if (enable) {
		if (mHistory.empty()) {
			mHistory.resize(131072, ATCPUHistoryEntry());
			mCoProc.SetHistoryBuffer(mHistory.data());
		}
	} else {
		if (!mHistory.empty()) {
			decltype(mHistory) tmp;
			tmp.swap(mHistory);
			mHistory.clear();
			mCoProc.SetHistoryBuffer(nullptr);
		}
	}
}

std::pair<uint32, uint32> ATDeviceDiskDriveXF551::GetHistoryRange() const {
	const uint32 hcnt = mCoProc.GetHistoryCounter();

	return std::pair<uint32, uint32>(hcnt - 131072, hcnt);
}

uint32 ATDeviceDiskDriveXF551::ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const {
	if (!n || mHistory.empty())
		return 0;

	const ATCPUHistoryEntry *hstart = mHistory.data();
	const ATCPUHistoryEntry *hend = hstart + 131072;
	const ATCPUHistoryEntry *hsrc = hstart + (start & 131071);

	for(uint32 i=0; i<n; ++i) {
		*hparray++ = hsrc;

		if (++hsrc == hend)
			hsrc = hstart;
	}

	return n;
}

uint32 ATDeviceDiskDriveXF551::ConvertRawTimestamp(uint32 rawTimestamp) const {
	// mLastSync is the machine cycle at which all sub-cycles have been pushed into the
	// coprocessor, and the coprocessor's time base is the sub-cycle corresponding to
	// the end of that machine cycle.
	return mLastSync - (((mCoProc.GetTimeBase() - rawTimestamp) * mClockDivisor + mSubCycleAccum + 511) >> 9);
}

void ATDeviceDiskDriveXF551::Break() {
	CancelStep();
}

bool ATDeviceDiskDriveXF551::StepInto(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = false;
	mStepStartSubCycle = mCoProc.GetTime();
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

bool ATDeviceDiskDriveXF551::StepOver(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = true;
	mStepStartSubCycle = mCoProc.GetTime();
	mStepOutSP = mCoProc.GetSP();
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

bool ATDeviceDiskDriveXF551::StepOut(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = true;
	mStepStartSubCycle = mCoProc.GetTime();
	mStepOutSP = mCoProc.GetSP() + 1;
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

void ATDeviceDiskDriveXF551::StepUpdate() {
	Sync();
}

void ATDeviceDiskDriveXF551::RunUntilSynced() {
	CancelStep();
	Sync();
}

bool ATDeviceDiskDriveXF551::CheckBreakpoint(uint32 pc) {
	if (mCoProc.GetTime() == mStepStartSubCycle)
		return false;

	const bool bpHit = mBreakpointsImpl.CheckBP(pc);

	if (!bpHit) {
		if (mbStepOut) {
			// Keep stepping if wrapped(s < s0).
			if ((mCoProc.GetSP() - mStepOutSP) & 0x8000)
				return false;
		}
	}

	mBreakpointsImpl.SetStepActive(false);

	mbStepNotifyPending = true;
	mbStepNotifyPendingBP = bpHit;
	return true;
}

void ATDeviceDiskDriveXF551::OnScheduledEvent(uint32 id) {
	if (id == kEventId_Run) {
		mpRunEvent = mpSlowScheduler->AddEvent(1, this, 1);

		mDriveScheduler.UpdateTick64();
		Sync();
	} else if (id == kEventId_Transmit) {
		mpTransmitEvent = nullptr;

		OnTransmitEvent();
	} else if (id == kEventId_DriveReceiveBit) {
		mReceiveShiftRegister >>= 1;
		mpEventDriveReceiveBit = nullptr;

		if (mReceiveShiftRegister >= 2) {
			mReceiveTimingAccum += mReceiveTimingStep;
			mpEventDriveReceiveBit = mDriveScheduler.AddEvent(mReceiveTimingAccum >> 10, this, kEventId_DriveReceiveBit);
			mReceiveTimingAccum &= 0x3FF;
		}
	} else if (id == kEventId_DriveDiskChange) {
		mpEventDriveDiskChange = nullptr;

		switch(++mDiskChangeState) {
			case 1:		// disk being removed (write protect covered)
			case 2:		// disk removed (write protect clear)
			case 3:		// disk being inserted (write protect covered)
				mDriveScheduler.SetEvent(kDiskChangeStepMS, this, kEventId_DriveDiskChange, mpEventDriveDiskChange);
				break;

			case 4:		// disk inserted (write protect normal)
				mDiskChangeState = 0;
				break;
		}

		UpdateDiskStatus();
	} else if (id == kEventId_DriveCommandChange) {
		mpEventDriveCommandChange = nullptr;
		OnCommandChangeEvent();
	}
}

void ATDeviceDiskDriveXF551::OnCommandStateChanged(bool asserted) {
	if (mbCommandState != asserted) {
		mbCommandState = asserted;

		AddCommandEdge(asserted);
	}
}

void ATDeviceDiskDriveXF551::OnMotorStateChanged(bool asserted) {
}

void ATDeviceDiskDriveXF551::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	Sync();

	mReceiveShiftRegister = (c + c + 0x200) * 2 + 1;

	// The conversion fraction we need here is 512/1649, but that denominator is awkward.
	// Approximate it with 318/1024.
	mReceiveTimingAccum = 0x200;
	mReceiveTimingStep = cyclesPerBit * 318;

	mDriveScheduler.SetEvent(1, this, kEventId_DriveReceiveBit, mpEventDriveReceiveBit);
}

void ATDeviceDiskDriveXF551::OnSendReady() {
}

void ATDeviceDiskDriveXF551::OnDiskChanged(bool mediaRemoved) {
	if (mediaRemoved) {
		mDiskChangeState = 0;
		mDriveScheduler.SetEvent(1, this, kEventId_DriveDiskChange, mpEventDriveDiskChange);
	}

	UpdateDiskStatus();
}

void ATDeviceDiskDriveXF551::OnWriteModeChanged() {
	UpdateWriteProtectStatus();
}

void ATDeviceDiskDriveXF551::OnTimingModeChanged() {
	mFDC.SetAccurateTimingEnabled(mpDiskInterface->IsAccurateSectorTimingEnabled());
}

void ATDeviceDiskDriveXF551::OnAudioModeChanged() {
	mbSoundsEnabled = mpDiskInterface->AreDriveSoundsEnabled();

	UpdateRotationStatus();
}

void ATDeviceDiskDriveXF551::CancelStep() {
	if (mpStepHandler) {
		mBreakpointsImpl.SetStepActive(false);

		auto p = std::move(mpStepHandler);
		mpStepHandler = nullptr;

		p(false);
	}
}


void ATDeviceDiskDriveXF551::Sync() {
	AccumSubCycles();

	for(;;) {
		if (!mCoProc.GetCyclesLeft()) {
			if (mSubCycleAccum < mClockDivisor)
				break;

			uint32 tc = ATSCHEDULER_GETTIMETONEXT(&mDriveScheduler);
			uint32 tc2 = mCoProc.GetTStatesPending();

			if (!tc2)
				tc2 = 1;

			if (tc > tc2)
				tc = tc2;

			uint32 subCycles = mClockDivisor * tc;

			if (mSubCycleAccum < subCycles) {
				subCycles = mClockDivisor;
				tc = 1;
			}

			mSubCycleAccum -= subCycles;

			ATSCHEDULER_ADVANCE_N(&mDriveScheduler, tc);
			mCoProc.AddCycles(tc);
		}

		mCoProc.Run();

		if (mCoProc.GetCyclesLeft())
			break;
	}

	if (mbStepNotifyPending) {
		mbStepNotifyPending = false;

		auto p = std::move(mpStepHandler);
		mpStepHandler = nullptr;

		if (p)
			p(!mbStepNotifyPendingBP);
	}
}

void ATDeviceDiskDriveXF551::AccumSubCycles() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	const uint32 cycles = t - mLastSync;

	mLastSync = t;

	mSubCycleAccum += cycles << 9;

	mLastSyncDriveTime = ATSCHEDULER_GETTIME(&mDriveScheduler);
	mLastSyncDriveTimeSubCycles = mSubCycleAccum;
}

void ATDeviceDiskDriveXF551::OnTransmitEvent() {
	// drain transmit queue entries until we're up to date
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	while((mTransmitHead ^ mTransmitTail) & kTransmitQueueMask) {
		const auto& nextEdge = mTransmitQueue[mTransmitHead & kTransmitQueueMask];

		if ((t - nextEdge.mTime) & UINT32_C(0x40000000))
			break;

		bool bit = (nextEdge.mBit != 0);

		if (mbTransmitCurrentBit != bit) {
			mbTransmitCurrentBit = bit;
			mpSIOMgr->SetRawInput(bit);
		}

		++mTransmitHead;
	}

	const uint32 resetCounter = mpSIOMgr->GetRecvResetCounter();

	if (mTransmitResetCounter != resetCounter) {
		mTransmitResetCounter = resetCounter;

		mTransmitCyclesPerBit = 0;
	}

	// check if we're waiting for a start bit
	if (!mTransmitCyclesPerBit) {
		if (!mbTransmitCurrentBit) {
			// possible start bit -- reset transmission
			mTransmitCyclesPerBit = 0;
			mTransmitShiftRegister = 0;
			mTransmitPhase = 0;

			const uint32 cyclesPerBit = mpSIOMgr->GetCyclesPerBitRecv();

			// reject transmission speed below ~16000 baud or above ~178Kbaud
			if (cyclesPerBit < 10 || cyclesPerBit > 114)
				return;

			mTransmitCyclesPerBit = cyclesPerBit;

			// queue event to half bit in
			mpScheduler->SetEvent(cyclesPerBit >> 1, this, kEventId_Transmit, mpTransmitEvent);
		}
		return;
	}

	// check for a bogus start bit
	if (mTransmitPhase == 0 && mbTransmitCurrentBit) {
		mTransmitCyclesPerBit = 0;

		QueueNextTransmitEvent();
		return;
	}

	// send byte to POKEY if done
	if (++mTransmitPhase == 10) {
		mpSIOMgr->SendRawByte(mTransmitShiftRegister, mTransmitCyclesPerBit, false, !mbTransmitCurrentBit, false);
		mTransmitCyclesPerBit = 0;
		QueueNextTransmitEvent();
		return;
	}

	// shift new bit into shift register
	mTransmitShiftRegister = (mTransmitShiftRegister >> 1) + (mbTransmitCurrentBit ? 0x80 : 0);

	// queue another event one bit later
	mpScheduler->SetEvent(mTransmitCyclesPerBit, this, kEventId_Transmit, mpTransmitEvent);
}

void ATDeviceDiskDriveXF551::AddTransmitEdge(uint32 polarity) {
	static_assert(!(kTransmitQueueSize & (kTransmitQueueSize - 1)), "mTransmitQueue size not pow2");

	// convert device time to computer time
	uint32 dt = (mLastSyncDriveTime - ATSCHEDULER_GETTIME(&mDriveScheduler)) * mClockDivisor + mLastSyncDriveTimeSubCycles;

	dt >>= 9;

	const uint32 t = (mLastSync + kTransmitLatency - dt) & UINT32_C(0x7FFFFFFF);

	// check if previous transition is at same time
	const uint32 queueLen = (mTransmitTail - mTransmitHead) & kTransmitQueueMask;
	if (queueLen) {
		auto& prevEdge = mTransmitQueue[(mTransmitTail - 1) & kTransmitQueueMask];

		// check if this event is at or before the last event in the queue
		if (!((prevEdge.mTime - t) & UINT32_C(0x40000000))) {
			// check if we've gone backwards in time and drop event if so
			if (prevEdge.mTime == t) {
				// same time -- overwrite event
				prevEdge.mBit = polarity;
			} else {
				VDASSERT(!"Dropping new event earlier than tail in transmit queue.");
			}

			return;
		}
	}

	// check if we have room for a new event
	if (queueLen >= kTransmitQueueMask) {
		VDASSERT(!"Transmit queue full.");
		return;
	}

	// add the new event
	auto& newEdge = mTransmitQueue[mTransmitTail++ & kTransmitQueueMask];

	newEdge.mTime = t;
	newEdge.mBit = polarity;

	// queue next event if needed
	QueueNextTransmitEvent();
}

void ATDeviceDiskDriveXF551::QueueNextTransmitEvent() {
	// exit if we already have an event queued
	if (mpTransmitEvent)
		return;

	// exit if transmit queue is empty
	if (!((mTransmitHead ^ mTransmitTail) & kTransmitQueueMask))
		return;

	const auto& nextEvent = mTransmitQueue[mTransmitHead & kTransmitQueueMask];
	uint32 delta = (nextEvent.mTime - ATSCHEDULER_GETTIME(mpScheduler)) & UINT32_C(0x7FFFFFFF);

	mpTransmitEvent = mpScheduler->AddEvent((uint32)(delta - 1) & UINT32_C(0x40000000) ? 1 : delta, this, kEventId_Transmit);
}

void ATDeviceDiskDriveXF551::QueueNextCommandEvent() {
	// exit if we already have an event queued
	if (mpEventDriveCommandChange)
		return;

	// exit if transmit queue is empty
	if (mCommandQueue.empty())
		return;

	const auto& nextEvent = mCommandQueue.front();
	uint32 delta = (nextEvent.mTime - ATSCHEDULER_GETTIME(&mDriveScheduler)) & UINT32_C(0x7FFFFFFF);

	mpEventDriveCommandChange = mDriveScheduler.AddEvent((uint32)(delta - 1) & UINT32_C(0x40000000) ? 1 : delta, this, kEventId_DriveCommandChange);
}

void ATDeviceDiskDriveXF551::OnCommandChangeEvent() {
	// drain command queue entries until we're up to date
	const uint32 t = ATSCHEDULER_GETTIME(&mDriveScheduler);

	while(!mCommandQueue.empty()) {
		const auto& nextEdge = mCommandQueue.front();

		if ((t - nextEdge.mTime) & UINT32_C(0x40000000))
			break;

		bool bit = (nextEdge.mBit != 0);

		if (mbDriveCommandState != bit) {
			mbDriveCommandState = bit;

			if (bit)
				mCoProc.AssertIrq();
			else
				mCoProc.NegateIrq();
		}

		mCommandQueue.pop_front();
	}

	// if we still have events, queue another event
	if (!mCommandQueue.empty()) {
		mpScheduler->SetEvent((mCommandQueue.front().mTime - t) & 0x7FFFFFFF, this, kEventId_DriveCommandChange, mpEventDriveCommandChange);
	}
}

void ATDeviceDiskDriveXF551::AddCommandEdge(uint32 polarity) {
	// Convert computer time to device time.
	//
	// We have a problem here because transmission is delayed by a byte time but we don't
	// necessarily know that delay when the command line is dropped. The XF551 has strict
	// requirements for the command line pulse because it needs about 77 machine cycles
	// from command asserted to start bit, but more importantly requires it to still be
	// asserted after the end of the last byte. To solve this, we assert /COMMAND
	// immediately but stretch the deassert a bit.

	const uint32 kCommandLatency = polarity ? 0 : 400;
	const uint32 ct = (ATSCHEDULER_GETTIME(mpScheduler) - mLastSync) * 512;
	const uint32 dt = (mLastSyncDriveTime + ct / mClockDivisor + kCommandLatency) & UINT32_C(0x7FFFFFFF);

	// check if previous transition is at same time
	while(!mCommandQueue.empty()) {
		auto& prevEdge = mCommandQueue.back();

		// check if this event is at or before the last event in the queue
		if ((prevEdge.mTime - dt) & UINT32_C(0x40000000)) {
			// The previous edge is before the time of this edge, so we're good.
			break;
		}

		// check if we've gone backwards in time and drop event if so
		if (prevEdge.mTime == dt) {
			// same time -- overwrite event and exit
			prevEdge.mBit = polarity;
			return;
		}
			
		if (polarity && !prevEdge.mBit) {
			// If we're asserting /COMMAND, allow it to supercede earlier deassert events.
			mCommandQueue.pop_back();
		} else {
			VDASSERT(!"Dropping new event earlier than tail in command change queue.");
			return;
		}
	}

	// add the new event
	mCommandQueue.push_back( { dt, polarity } );

	// queue next event if needed
	QueueNextCommandEvent();
}

void ATDeviceDiskDriveXF551::OnFDCStep(bool inward) {
	if (inward) {
		// step in (increasing track number)
		if (mCurrentTrack < 90U) {
			mCurrentTrack += 2;

			mFDC.SetCurrentTrack(mCurrentTrack, mCurrentTrack == 0);
		}

		PlayStepSound();
	} else {
		// step out (decreasing track number)
		if (mCurrentTrack > 0) {
			mCurrentTrack -= 2;

			mFDC.SetCurrentTrack(mCurrentTrack, mCurrentTrack == 0);

			PlayStepSound();
		}
	}
}

void ATDeviceDiskDriveXF551::OnFDCMotorChange(bool enabled) {
	if (mbMotorRunning != enabled) {
		mbMotorRunning = enabled;

		mFDC.SetMotorRunning(enabled);
		UpdateRotationStatus();
	}
}

bool ATDeviceDiskDriveXF551::OnReadT0() const {
	// T0 = inverted SIO DATA OUT (computer -> peripheral).
	return !(mReceiveShiftRegister & 1);
}

bool ATDeviceDiskDriveXF551::OnReadT1() const {
	// T1 = DRQ
	return mFDC.GetDrqStatus();
}

uint8 ATDeviceDiskDriveXF551::OnReadPort(uint8 addr, uint8 output) {
	if (addr == 1)
		return output & (0x3F | (mDriveId << 6));

	uint8 v = output;

	if (!mFDC.GetIrqStatus())
		v &= 0xFB;

	return v;
}

void ATDeviceDiskDriveXF551::OnWritePort(uint8 addr, uint8 output) {
	if (addr == 0) {
		// P1:
		//	D1:D0	Output: FDC address
		//	D2		Input: FDC interrupt
		//	D3		Output: FDC density (1 = FM)
		//	D4		Output: FDC reset
		//	D5		Output: FDC read/write (1 = read)
		//	D6		Output: FDC side select
		//	D7		Output: SIO DATA IN

		mFDC.SetDensity(!(output & 8));
		mFDC.SetSide((output & 0x40) != 0);

		if (!(output & 0x10))
			mFDC.Reset();

		bool directTransmitOutput = !(output & 0x80);
		if (mbDirectTransmitOutput != directTransmitOutput) {
			mbDirectTransmitOutput = directTransmitOutput;
			AddTransmitEdge(directTransmitOutput);
		}
	} else {
		// P2:
		//	D7:D6	Input: Drive select
	}
}

uint8 ATDeviceDiskDriveXF551::OnReadXRAM() {
	// check if write line on FDC is inactive
	const uint8 p1 = mCoProc.GetPort1Output();

	if (!(p1 & 0x20))
		return 0xFF;

	return mFDC.ReadByte(p1 & 0x03);
}

void ATDeviceDiskDriveXF551::OnWriteXRAM(uint8 val) {
	// check if write line on FDC is active
	const uint8 p1 = mCoProc.GetPort1Output();
	if (p1 & 0x20)
		return;

	mFDC.WriteByte(p1 & 0x03, val);
}

void ATDeviceDiskDriveXF551::PlayStepSound() {
	if (!mbSoundsEnabled)
		return;

	const uint32 t = ATSCHEDULER_GETTIME(&mDriveScheduler);
	
	if (t - mLastStepSoundTime > 50000)
		mLastStepPhase = 0;

	mAudioPlayer.PlayStepSound(kATAudioSampleId_DiskStep2, 0.3f + 0.7f * cosf((float)mLastStepPhase++ * nsVDMath::kfPi));

	mLastStepSoundTime = t;
}

void ATDeviceDiskDriveXF551::UpdateRotationStatus() {
	mpDiskInterface->SetShowMotorActive(mbMotorRunning);

	mAudioPlayer.SetRotationSoundEnabled(mbMotorRunning && mbSoundsEnabled);
}

void ATDeviceDiskDriveXF551::UpdateDiskStatus() {
	IATDiskImage *image = mpDiskInterface->GetDiskImage();

	mFDC.SetDiskImage(image, (image != nullptr && mDiskChangeState == 0));

	UpdateWriteProtectStatus();
}

void ATDeviceDiskDriveXF551::UpdateWriteProtectStatus() {
	const bool wpoverride = (mDiskChangeState & 1) != 0;
	const bool wpsense = mpDiskInterface->GetDiskImage() && !mpDiskInterface->IsDiskWritable();

	mFDC.SetWriteProtectOverride(wpoverride);
}
