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
#include "diskdrivepercom.h"
#include "memorymanager.h"
#include "firmwaremanager.h"
#include "debuggerlog.h"

extern ATLogChannel g_ATLCDiskEmu;

void ATCreateDeviceDiskDrivePercom(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceDiskDrivePercom> p(new ATDeviceDiskDrivePercom);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefDiskDrivePercom = { "diskdrivepercom", "diskdrivepercom", L"Percom disk drive (full emulation)", ATCreateDeviceDiskDrivePercom };

///////////////////////////////////////////////////////////////////////////

void ATDeviceDiskDrivePercom::Drive::OnDiskChanged(bool mediaRemoved) {
	mpParent->OnDiskChanged(mIndex, mediaRemoved);
}

void ATDeviceDiskDrivePercom::Drive::OnWriteModeChanged() {
	mpParent->OnWriteModeChanged(mIndex);
}

void ATDeviceDiskDrivePercom::Drive::OnTimingModeChanged() {
	mpParent->OnTimingModeChanged(mIndex);
}

void ATDeviceDiskDrivePercom::Drive::OnAudioModeChanged() {
	mpParent->OnAudioModeChanged(mIndex);
}

///////////////////////////////////////////////////////////////////////////

ATDeviceDiskDrivePercom::ATDeviceDiskDrivePercom() {
	mBreakpointsImpl.BindBPHandler(mCoProc);
	mBreakpointsImpl.SetStepHandler(this);
}

ATDeviceDiskDrivePercom::~ATDeviceDiskDrivePercom() {
}

void *ATDeviceDiskDrivePercom::AsInterface(uint32 iid) {
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

void ATDeviceDiskDrivePercom::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefDiskDrivePercom;
}

void ATDeviceDiskDrivePercom::GetSettingsBlurb(VDStringW& buf) {
	bool first = true;

	for(uint32 i=0; i<kNumDrives; ++i) {
		if (mDrives[i].mType) {
			if (first)
				first = false;
			else
				buf += ',';

			buf.append_sprintf(L"D%u:", i + mDriveId + 1);
		}
	}
}

void ATDeviceDiskDrivePercom::GetSettings(ATPropertySet& settings) {
	settings.SetUint32("id", mDriveId);

	VDStringA s;
	for(uint32 i=0; i<kNumDrives; ++i) {
		s.sprintf("drivetype%u", i);
		settings.SetUint32(s.c_str(), (uint32)mDrives[i].mType);
	}
}

bool ATDeviceDiskDrivePercom::SetSettings(const ATPropertySet& settings) {
	VDStringA s;
	bool change = false;

	for(uint32 i=0; i<kNumDrives; ++i) {
		s.sprintf("drivetype%u", i);
		const uint32 driveTypeCode = settings.GetUint32(s.c_str(), i ? kDriveType_None : kDriveType_5_25_40Track);

		if (driveTypeCode <= kDriveType_5_25_80Track) {
			Drive& drive = mDrives[i];

			if (drive.mType != driveTypeCode) {
				drive.mType = (DriveType)driveTypeCode;

				if (drive.mType == kDriveType_5_25_80Track)
					drive.mMaxTrack = 180;
				else
					drive.mMaxTrack = 90;

				change = true;
			}
		}
	}

	uint32 newDriveId = settings.GetUint32("id", mDriveId) & 7;

	if (mDriveId != newDriveId) {
		mDriveId = newDriveId;
		change = true;
	}

	return !change;
}

void ATDeviceDiskDrivePercom::Init() {
	uintptr *readmap = mCoProc.GetReadMap();
	uintptr *writemap = mCoProc.GetWriteMap();

	// initialize memory map
	for(int i=0; i<256; ++i) {
		readmap[i] = (uintptr)mDummyRead - (i << 8);
		writemap[i] = (uintptr)mDummyWrite - (i << 8);
	}

	// Map hardware registers to $D000-D3FF. The individual sections are selected
	// by A2-A5, so all page mappings are the same here.
	mReadNodeHardware.mpThis = this;
	mReadNodeHardware.mpRead = [](uint32 addr, void *thisPtr) { return ((ATDeviceDiskDrivePercom *)thisPtr)->OnHardwareRead(addr); };
	mReadNodeHardware.mpDebugRead = [](uint32 addr, void *thisPtr) { return ((ATDeviceDiskDrivePercom *)thisPtr)->OnHardwareDebugRead(addr); };

	mWriteNodeHardware.mpThis = this;
	mWriteNodeHardware.mpWrite = [](uint32 addr, uint8 value, void *thisPtr) { ((ATDeviceDiskDrivePercom *)thisPtr)->OnHardwareWrite(addr, value); };

	for(int i=0; i<4; ++i) {
		readmap[i+0xD0] = mReadNodeHardware.AsBase();
		writemap[i+0xD0] = mWriteNodeHardware.AsBase();
	}

	// map RAM to $DC00-DFFF
	for(int i=0; i<4; ++i) {
		readmap[i+0xDC] = (uintptr)mRAM - 0xDC00;
		writemap[i+0xDC] = (uintptr)mRAM - 0xDC00;
	}

	// map ROM to $F000-FFFF (mirrored)
	for(int i=0; i<8; ++i) {
		readmap[i+0xF0] = (uintptr)mROM - 0xF000;
		readmap[i+0xF8] = (uintptr)mROM - 0xF800;
	}

	mDriveScheduler.SetRate(VDFraction(1000000, 1));

	// Base clock to the ACIA is 4MHz / 13.
	mACIA.Init(&mDriveScheduler);
	mACIA.SetMasterClockPeriod(13 * 16);
	mACIA.SetTransmitFn([this](uint8 v, uint32 cyclesPerBit) { OnACIATransmit(v, cyclesPerBit); });

	mFDC.Init(&mDriveScheduler, 288.0f, ATFDCEmulator::kType_279X);
	mFDC.SetAutoIndexPulse(true);
	mFDC.SetOnDrqChange([this](bool drq) { OnFDCDataRequest(drq); });
	mFDC.SetOnIrqChange([this](bool irq) { OnFDCInterruptRequest(irq); });
	mFDC.SetOnStep([this](bool inward) { OnFDCStep(inward); });

	for(auto& drive : mDrives) {
		mDriveScheduler.UnsetEvent(drive.mpEventDriveDiskChange);
		drive.mDiskChangeState = 0;

		drive.OnDiskChanged(false);

		drive.OnWriteModeChanged();
		drive.OnTimingModeChanged();
		drive.OnAudioModeChanged();
	}

	UpdateRotationStatus();
}

void ATDeviceDiskDrivePercom::Shutdown() {
	mAudioPlayer.Shutdown();

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
		mpSIOMgr->RemoveRawDevice(this);
		mpSIOMgr = nullptr;
	}

	for(auto& drive : mDrives) {
		if (drive.mpDiskInterface) {
			drive.mpDiskInterface->RemoveClient(&drive);
			drive.mpDiskInterface = nullptr;
		}
	}

	mpDiskDriveManager = nullptr;
}

uint32 ATDeviceDiskDrivePercom::GetComputerPowerOnDelay() const {
	return 20;
}

void ATDeviceDiskDrivePercom::WarmReset() {
	mLastSync = ATSCHEDULER_GETTIME(mpScheduler);
	mLastSyncDriveTime = ATSCHEDULER_GETTIME(&mDriveScheduler);
	mLastSyncDriveTimeSubCycles = 0;
}

void ATDeviceDiskDrivePercom::ComputerColdReset() {
	WarmReset();
}

void ATDeviceDiskDrivePercom::PeripheralColdReset() {
	memset(mRAM, 0xA5, sizeof mRAM);

	mACIA.Reset();
	mFDC.Reset();

	mCommandQueue.clear();

	mbNmiState = false;
	mbNmiTimeout = false;
	mbNmiTimeoutEnabled = false;
	mDriveScheduler.UnsetEvent(mpEventDriveTimeout);

	mbDriveCommandState = false;
	mDriveScheduler.UnsetEvent(mpEventDriveCommandChange);

	mpScheduler->UnsetEvent(mpTransmitEvent);

	SelectDrive(-1);

	mTransmitHead = 0;
	mTransmitTail = 0;
	
	mbForcedIndexPulse = false;

	// start the disk drive on a track other than 0/20/39, just to make things interesting
	for(Drive& drive : mDrives)
		drive.mCurrentTrack = 20;

	mFDC.SetCurrentTrack(20, false);

	mbMotorRunning = false;
	mFDC.SetMotorRunning(false);
	mFDC.SetDensity(false);
	mFDC.SetWriteProtectOverride(false);
	mFDC.SetAutoIndexPulse(true);
	mFDC.SetSide(false);

	mbExtendedRAMEnabled = false;

	mCoProc.ColdReset();

	mClockDivisor = VDRoundToInt(512.0 * mDriveScheduler.GetRate().AsInverseDouble() * mpScheduler->GetRate().asDouble());

	WarmReset();
}

void ATDeviceDiskDrivePercom::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;

	mpSlowScheduler->SetEvent(1, this, 1, mpRunEvent);
}

void ATDeviceDiskDrivePercom::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMgr = fwman;

	ReloadFirmware();
}

bool ATDeviceDiskDrivePercom::ReloadFirmware() {
	const uint64 id = mpFwMgr->GetFirmwareOfType(kATFirmwareType_Percom, true);
	
	uint32 len = 0;
	bool changed = false;
	mpFwMgr->LoadFirmware(id, mROM, 0, sizeof mROM, &changed, &len, nullptr, nullptr, &mbFirmwareUsable);

	return changed;
}

const wchar_t *ATDeviceDiskDrivePercom::GetWritableFirmwareDesc(uint32 idx) const {
	return nullptr;
}

bool ATDeviceDiskDrivePercom::IsWritableFirmwareDirty(uint32 idx) const {
	return false;
}

void ATDeviceDiskDrivePercom::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
}

ATDeviceFirmwareStatus ATDeviceDiskDrivePercom::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATDeviceDiskDrivePercom::InitDiskDrive(IATDiskDriveManager *ddm) {
	mpDiskDriveManager = ddm;
	mAvailableDrives = 0;

	for(uint32 i=0; i<kNumDrives; ++i) {
		Drive& drive = mDrives[i];

		drive.mIndex = i;
		drive.mpParent = this;

		if (drive.mType) {
			drive.mpDiskInterface = ddm->GetDiskInterface(i + mDriveId);
			drive.mpDiskInterface->AddClient(&drive);

			mAvailableDrives |= (1 << i);
		}
	}
}

void ATDeviceDiskDrivePercom::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddRawDevice(this);
}

IATDebugTarget *ATDeviceDiskDrivePercom::GetDebugTarget(uint32 index) {
	if (index == 0)
		return this;

	return nullptr;
}

const char *ATDeviceDiskDrivePercom::GetName() {
	return "Disk Drive CPU";
}

ATDebugDisasmMode ATDeviceDiskDrivePercom::GetDisasmMode() {
	return kATDebugDisasmMode_6809;
}

void ATDeviceDiskDrivePercom::GetExecState(ATCPUExecState& state) {
	mCoProc.GetExecState(state);
}

void ATDeviceDiskDrivePercom::SetExecState(const ATCPUExecState& state) {
	mCoProc.SetExecState(state);
}

sint32 ATDeviceDiskDrivePercom::GetTimeSkew() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	const uint32 cycles = (t - mLastSync) + ((mCoProc.GetCyclesLeft() * mClockDivisor + mSubCycleAccum + 511) >> 9);

	return -(sint32)cycles;
}

uint8 ATDeviceDiskDrivePercom::ReadByte(uint32 address) {
	return DebugReadByte(address);
}

void ATDeviceDiskDrivePercom::ReadMemory(uint32 address, void *dst, uint32 n) {
	return DebugReadMemory(address, dst, n);
}

uint8 ATDeviceDiskDrivePercom::DebugReadByte(uint32 address) {
	uint8 v;
	ATCoProcReadMemory(mCoProc.GetReadMap(), &v, address, 1);

	return v;
}

void ATDeviceDiskDrivePercom::DebugReadMemory(uint32 address, void *dst, uint32 n) {
	ATCoProcReadMemory(mCoProc.GetReadMap(), dst, address, n);
}

void ATDeviceDiskDrivePercom::WriteByte(uint32 address, uint8 value) {
	ATCoProcWriteMemory(mCoProc.GetWriteMap(), &value, address, 1);
}

void ATDeviceDiskDrivePercom::WriteMemory(uint32 address, const void *src, uint32 n) {
	ATCoProcWriteMemory(mCoProc.GetWriteMap(), src, address, n);
}

bool ATDeviceDiskDrivePercom::GetHistoryEnabled() const {
	return !mHistory.empty();
}

void ATDeviceDiskDrivePercom::SetHistoryEnabled(bool enable) {
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

std::pair<uint32, uint32> ATDeviceDiskDrivePercom::GetHistoryRange() const {
	const uint32 hcnt = mCoProc.GetHistoryCounter();

	return std::pair<uint32, uint32>(hcnt - 131072, hcnt);
}

uint32 ATDeviceDiskDrivePercom::ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const {
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

uint32 ATDeviceDiskDrivePercom::ConvertRawTimestamp(uint32 rawTimestamp) const {
	// mLastSync is the machine cycle at which all sub-cycles have been pushed into the
	// coprocessor, and the coprocessor's time base is the sub-cycle corresponding to
	// the end of that machine cycle.
	return mLastSync - (((mCoProc.GetTimeBase() - rawTimestamp) * mClockDivisor + mSubCycleAccum + 511) >> 9);
}

void ATDeviceDiskDrivePercom::Break() {
	CancelStep();
}

bool ATDeviceDiskDrivePercom::StepInto(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = false;
	mStepStartSubCycle = mCoProc.GetTime();
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

bool ATDeviceDiskDrivePercom::StepOver(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = true;
	mStepStartSubCycle = mCoProc.GetTime();
	mStepOutSP = mCoProc.GetS();
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

bool ATDeviceDiskDrivePercom::StepOut(const vdfunction<void(bool)>& fn) {
	CancelStep();

	mpStepHandler = fn;
	mbStepOut = true;
	mStepStartSubCycle = mCoProc.GetTime();
	mStepOutSP = mCoProc.GetS() + 1;
	mBreakpointsImpl.SetStepActive(true);
	Sync();
	return true;
}

void ATDeviceDiskDrivePercom::StepUpdate() {
	Sync();
}

void ATDeviceDiskDrivePercom::RunUntilSynced() {
	CancelStep();
	Sync();
}

bool ATDeviceDiskDrivePercom::CheckBreakpoint(uint32 pc) {
	if (mCoProc.GetTime() == mStepStartSubCycle)
		return false;

	const bool bpHit = mBreakpointsImpl.CheckBP(pc);

	if (!bpHit) {
		if (mbStepOut) {
			// Keep stepping if wrapped(s < s0).
			if ((mCoProc.GetS() - mStepOutSP) & 0x8000)
				return false;
		}
	}

	mBreakpointsImpl.SetStepActive(false);

	mbStepNotifyPending = true;
	mbStepNotifyPendingBP = bpHit;
	return true;
}

void ATDeviceDiskDrivePercom::OnScheduledEvent(uint32 id) {
	if (id == kEventId_Run) {
		mpRunEvent = mpSlowScheduler->AddEvent(1, this, 1);

		mDriveScheduler.UpdateTick64();
		Sync();
	} else if (id == kEventId_Transmit) {
		mpTransmitEvent = nullptr;

		OnTransmitEvent();
	} else if (id == kEventId_DriveTimeout) {
		mpEventDriveTimeout = nullptr;

		if (!mbNmiTimeout) {
			mbNmiTimeout = true;

			UpdateNmi();
		}
	} else if (id >= kEventId_DriveDiskChange0 && id < kEventId_DriveDiskChange0 + kNumDrives) {
		const uint32 index = id - kEventId_DriveDiskChange0;
		Drive& drive = mDrives[index];

		drive.mpEventDriveDiskChange = nullptr;

		switch(++drive.mDiskChangeState) {
			case 1:		// disk being removed (write protect covered)
			case 2:		// disk removed (write protect clear)
			case 3:		// disk being inserted (write protect covered)
				mDriveScheduler.SetEvent(kDiskChangeStepMS, this, kEventId_DriveDiskChange0 + index, drive.mpEventDriveDiskChange);
				break;

			case 4:		// disk inserted (write protect normal)
				drive.mDiskChangeState = 0;
				break;
		}

		UpdateDiskStatus();
	} else if (id == kEventId_DriveCommandChange) {
		mpEventDriveCommandChange = nullptr;
		OnCommandChangeEvent();
	}
}

void ATDeviceDiskDrivePercom::OnCommandStateChanged(bool asserted) {
	if (mbCommandState != asserted) {
		mbCommandState = asserted;

		AddCommandEdge(asserted);
	}
}

void ATDeviceDiskDrivePercom::OnMotorStateChanged(bool asserted) {
}

void ATDeviceDiskDrivePercom::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	Sync();

	mACIA.ReceiveByte(c, (cyclesPerBit * 100 + (179/2)) / 179);
}

void ATDeviceDiskDrivePercom::OnSendReady() {
}

void ATDeviceDiskDrivePercom::OnDiskChanged(uint32 index, bool mediaRemoved) {
	Drive& drive = mDrives[index];

	if (mediaRemoved) {
		drive.mDiskChangeState = 0;
		mDriveScheduler.SetEvent(1, this, kEventId_DriveDiskChange0 + index, drive.mpEventDriveDiskChange);
	}

	UpdateDiskStatus();
}

void ATDeviceDiskDrivePercom::OnWriteModeChanged(uint32 index) {
	if (mSelectedDrive == (int)index)
		UpdateWriteProtectStatus();
}

void ATDeviceDiskDrivePercom::OnTimingModeChanged(uint32 index) {
	if (mSelectedDrive == (int)index) {
		const bool accurateTiming = mDrives[index].mpDiskInterface->IsAccurateSectorTimingEnabled();

		mFDC.SetAccurateTimingEnabled(accurateTiming);
	}
}

void ATDeviceDiskDrivePercom::OnAudioModeChanged(uint32 index) {
	if (mSelectedDrive == (int)index) {
		bool driveSounds = mDrives[index].mpDiskInterface->AreDriveSoundsEnabled();

		mbSoundsEnabled = driveSounds;

		UpdateRotationStatus();
	}
}

void ATDeviceDiskDrivePercom::CancelStep() {
	if (mpStepHandler) {
		mBreakpointsImpl.SetStepActive(false);

		auto p = std::move(mpStepHandler);
		mpStepHandler = nullptr;

		p(false);
	}
}


void ATDeviceDiskDrivePercom::Sync() {
	AccumSubCycles();

	for(;;) {
		if (!mCoProc.GetCyclesLeft()) {
			if (mSubCycleAccum < mClockDivisor)
				break;

			mSubCycleAccum -= mClockDivisor;

			ATSCHEDULER_ADVANCE(&mDriveScheduler);

			mCoProc.AddCycles(1);
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

void ATDeviceDiskDrivePercom::AccumSubCycles() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	const uint32 cycles = t - mLastSync;

	mLastSync = t;

	mSubCycleAccum += cycles << 9;

	mLastSyncDriveTime = ATSCHEDULER_GETTIME(&mDriveScheduler);
	mLastSyncDriveTimeSubCycles = mSubCycleAccum;
}

void ATDeviceDiskDrivePercom::QueueNextCommandEvent() {
	// exit if we already have an event queued
	if (mpEventDriveCommandChange)
		return;

	// exit if transmit queue is empty
	if (mCommandQueue.empty())
		return;

	const auto& nextEvent = mCommandQueue.front();
	uint32 delta = nextEvent.mTime - ATSCHEDULER_GETTIME(&mDriveScheduler);

	mpEventDriveCommandChange = mDriveScheduler.AddEvent((uint32)(delta - 1) & UINT32_C(0x80000000) ? 1 : delta, this, kEventId_DriveCommandChange);
}

void ATDeviceDiskDrivePercom::OnCommandChangeEvent() {
	// drain command queue entries until we're up to date
	const uint32 t = ATSCHEDULER_GETTIME(&mDriveScheduler);

	while(!mCommandQueue.empty()) {
		const auto& nextEdge = mCommandQueue.front();

		if ((t - nextEdge.mTime) & UINT32_C(0x80000000))
			break;

		bool bit = (nextEdge.mBit != 0);

		if (mbDriveCommandState != bit) {
			mbDriveCommandState = bit;

			mACIA.SetCTS(bit);
		}

		mCommandQueue.pop_front();
	}

	// if we still have events, queue another event
	if (!mCommandQueue.empty()) {
		mpScheduler->SetEvent(mCommandQueue.front().mTime - t, this, kEventId_DriveCommandChange, mpEventDriveCommandChange);
	}
}

void ATDeviceDiskDrivePercom::AddCommandEdge(uint32 polarity) {
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
	const uint32 dt = mLastSyncDriveTime + ct / mClockDivisor + kCommandLatency;

	// check if previous transition is at same time
	while(!mCommandQueue.empty()) {
		auto& prevEdge = mCommandQueue.back();

		// check if this event is at or before the last event in the queue
		if ((prevEdge.mTime - dt) & UINT32_C(0x80000000)) {
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

uint8 ATDeviceDiskDrivePercom::OnHardwareDebugRead(uint32 addr) {
	// To access the hardware registers, A4 must be high and A5 must be low.
	// A2-A3 select the unit and A0-A1 the subunit (ACIA/FDC). A6-A9 don't
	// matter.
	switch(addr & 0x3C) {
		case 0x10:
			return mFDC.DebugReadByte(addr);

		case 0x14:
			return (mDriveId & 3) + (mDriveId & 4 ? 0x10 : 0x00) + 0xE0;

		case 0x30:
			return mACIA.DebugReadByte(addr);
	}

	return 0xFF;
}

uint8 ATDeviceDiskDrivePercom::OnHardwareRead(uint32 addr) {
	// To access the hardware registers, A4 must be high and A5 must be low.
	// A2-A3 select the unit and A0-A1 the subunit (ACIA/FDC). A6-A9 don't
	// matter.
	switch(addr & 0x3C) {
		case 0x10:
			if (mbNmiTimeoutEnabled)
				SetNmiTimeout();
			return ~mFDC.ReadByte(addr);

		case 0x14:
			return OnHardwareDebugRead(addr);

		case 0x30:
			return mACIA.ReadByte(addr);
	}

	return 0xFF;
}

void ATDeviceDiskDrivePercom::OnHardwareWrite(uint32 addr, uint8 value) {
	// To access the hardware registers, A4 must be high and A5 must be low.
	// A2-A3 select the unit and A0-A1 the subunit (ACIA/FDC). A6-A9 don't
	// matter.
	switch(addr & 0x3C) {
		case 0x10:
			mFDC.WriteByte(addr, ~value);
			if (mbNmiTimeoutEnabled)
				SetNmiTimeout();
			break;

		case 0x14:
			if (value & 8) {
				SetMotorEnabled(true);
				SelectDrive((value >> 1) & 3);
			} else {
				SetMotorEnabled(false);
				SelectDrive(-1);
			}

			mFDC.SetSide((value & 1) != 0);
			break;

		case 0x18: {
			const bool nmiTimeoutEnabled = (value & 1) != 0;

			if (mbNmiTimeoutEnabled != nmiTimeoutEnabled) {
				mbNmiTimeoutEnabled = nmiTimeoutEnabled;

				if (!mbNmiTimeoutEnabled)
					mDriveScheduler.UnsetEvent(mpEventDriveTimeout);

				UpdateNmi();
			}

			mFDC.SetDensity((value & 4) != 0);
			mFDC.SetAutoIndexPulse((value & 8) == 0);
			break;
		}

		case 0x30:
			mACIA.WriteByte(addr, value);
			break;
	}
}

void ATDeviceDiskDrivePercom::OnFDCDataRequest(bool asserted) {
	if (asserted)
		mCoProc.AssertFirq();
	else
		mCoProc.NegateFirq();
}

void ATDeviceDiskDrivePercom::OnFDCInterruptRequest(bool asserted) {
	UpdateNmi();
}

void ATDeviceDiskDrivePercom::OnFDCStep(bool inward) {
	if (mSelectedDrive < 0)
		return;

	Drive& drive = mDrives[mSelectedDrive];

	if (inward) {
		// step in (increasing track number)
		if (drive.mCurrentTrack < drive.mMaxTrack) {
			drive.mCurrentTrack += 2;

			mFDC.SetCurrentTrack(drive.mCurrentTrack, drive.mCurrentTrack == 0);
		}

		PlayStepSound();
	} else {
		// step out (decreasing track number)
		if (drive.mCurrentTrack > 0) {
			drive.mCurrentTrack -= 2;

			mFDC.SetCurrentTrack(drive.mCurrentTrack, drive.mCurrentTrack == 0);

			PlayStepSound();
		}
	}
}

void ATDeviceDiskDrivePercom::OnACIATransmit(uint8 v, uint32 cyclesPerBit) {
	AddTransmitByte(v, (cyclesPerBit * 179 + 50) / 100);
}

void ATDeviceDiskDrivePercom::OnTransmitEvent() {
	// drain transmit queue entries until we're up to date
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	while((mTransmitHead ^ mTransmitTail) & kTransmitQueueMask) {
		const auto& nextEdge = mTransmitQueue[mTransmitHead & kTransmitQueueMask];

		if ((t - nextEdge.mTime) & UINT32_C(0x80000000))
			break;

		mpSIOMgr->SendRawByte(nextEdge.mData, nextEdge.mCyclesPerBit, false, false, true);

		++mTransmitHead;
	}

	QueueNextTransmitEvent();
}

void ATDeviceDiskDrivePercom::AddTransmitByte(uint8 data, uint32 cyclesPerBit) {
	VDASSERT(cyclesPerBit);
	static_assert(!(kTransmitQueueSize & (kTransmitQueueSize - 1)), "mTransmitQueue size not pow2");

	// convert device time to computer time
	uint32 dt = (mLastSyncDriveTime - ATSCHEDULER_GETTIME(&mDriveScheduler)) * mClockDivisor + mLastSyncDriveTimeSubCycles;

	dt >>= 7;

	const uint32 t = mLastSync + kTransmitLatency - dt;

	// check if previous transition is at same time
	const uint32 queueLen = (mTransmitTail - mTransmitHead) & kTransmitQueueMask;
	if (queueLen) {
		auto& prevEdge = mTransmitQueue[(mTransmitTail - 1) & kTransmitQueueMask];

		// check if this event is at or before the last event in the queue
		if (!((prevEdge.mTime - t) & UINT32_C(0x80000000))) {
			// check if we've gone backwards in time and drop event if so
			if (prevEdge.mTime == t) {
				// same time -- overwrite event
				prevEdge.mData = data;
				prevEdge.mCyclesPerBit = cyclesPerBit;
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
	newEdge.mData = data;
	newEdge.mCyclesPerBit = cyclesPerBit;

	// queue next event if needed
	QueueNextTransmitEvent();
}

void ATDeviceDiskDrivePercom::QueueNextTransmitEvent() {
	// exit if we already have an event queued
	if (mpTransmitEvent)
		return;

	// exit if transmit queue is empty
	if (!((mTransmitHead ^ mTransmitTail) & kTransmitQueueMask))
		return;

	const auto& nextEvent = mTransmitQueue[mTransmitHead & kTransmitQueueMask];
	const uint32 delta = nextEvent.mTime - ATSCHEDULER_GETTIME(mpScheduler);

	mpTransmitEvent = mpScheduler->AddEvent((uint32)(delta - 1) & UINT32_C(0x80000000) ? 1 : delta, this, kEventId_Transmit);
}

void ATDeviceDiskDrivePercom::SetMotorEnabled(bool enabled) {
	if (mbMotorRunning != enabled) {
		mbMotorRunning = enabled;

		mFDC.SetMotorRunning(enabled);
		UpdateRotationStatus();
	}
}

void ATDeviceDiskDrivePercom::PlayStepSound() {
	if (!mbSoundsEnabled)
		return;

	const uint32 t = ATSCHEDULER_GETTIME(&mDriveScheduler);
	
	if (t - mLastStepSoundTime > 50000)
		mLastStepPhase = 0;

	mAudioPlayer.PlayStepSound(kATAudioSampleId_DiskStep2, 0.3f + 0.7f * cosf((float)mLastStepPhase++ * nsVDMath::kfPi));

	mLastStepSoundTime = t;
}

void ATDeviceDiskDrivePercom::UpdateRotationStatus() {
	if (mSelectedDrive >= 0) {
		const Drive& drive = mDrives[mSelectedDrive];

		drive.mpDiskInterface->SetShowMotorActive(mbMotorRunning);

		if (mbMotorRunning && mbSoundsEnabled) {
			mAudioPlayer.SetRotationSoundEnabled(true);
			return;
		}
	}

	mAudioPlayer.SetRotationSoundEnabled(false);
}

void ATDeviceDiskDrivePercom::UpdateDiskStatus() {
	if (mSelectedDrive >= 0) {
		const Drive& drive = mDrives[mSelectedDrive];
		IATDiskImage *image = drive.mpDiskInterface->GetDiskImage();

		mFDC.SetDiskImage(image, (image != nullptr && drive.mDiskChangeState == 0));
	} else
		mFDC.SetDiskImage(nullptr, false);

	UpdateWriteProtectStatus();
}

void ATDeviceDiskDrivePercom::UpdateWriteProtectStatus() {
	bool wpoverride = false;

	if (mSelectedDrive >= 0)
		wpoverride = (mDrives[mSelectedDrive].mDiskChangeState & 1) != 0;

	mFDC.SetWriteProtectOverride(wpoverride);
}

void ATDeviceDiskDrivePercom::SelectDrive(int index) {
	if (!(mAvailableDrives & (1 << index)))
		index = -1;

	if (mSelectedDrive == index)
		return;

	if (mSelectedDrive >= 0) {
		Drive& oldDrive = mDrives[mSelectedDrive];

		oldDrive.mpDiskInterface->SetShowMotorActive(false);
	}

	mSelectedDrive = index;

	if (index >= 0) {
		Drive& drive = mDrives[index];
		mFDC.SetDiskInterface(drive.mpDiskInterface);
		mFDC.SetCurrentTrack(drive.mCurrentTrack, drive.mCurrentTrack == 0);

		OnWriteModeChanged(index);
		OnTimingModeChanged(index);
		OnAudioModeChanged(index);
	} else {
		mFDC.SetDiskInterface(nullptr);
		mFDC.SetCurrentTrack(20, false);
	}

	UpdateDiskStatus();
	UpdateRotationStatus();
}

void ATDeviceDiskDrivePercom::UpdateNmi() {
	bool nmiState = true;

	// If the FDC is requesting an interrupt, the 74LS122 is kept in cleared
	// state pulling /NMI low. $DC14-17 bit 0 can also hold this low if set
	// to 0.
	if (!mFDC.GetIrqStatus() && mbNmiTimeoutEnabled) {
		// FDC is not requesting an interrupt and the NMI is enabled. See if
		// the timeout has expired.

		nmiState = mbNmiTimeout;
	}

	if (mbNmiState != nmiState) {
		mbNmiState = nmiState;

		if (nmiState)
			mCoProc.AssertNmi();
	}
}

void ATDeviceDiskDrivePercom::SetNmiTimeout() {
	// This timeout delay is determined by an R/C network connected to
	// the 74LS123. Not being able to interpret said circuit, use a timeout
	// that is enough for at least four disk rotations.
	const uint32 kTimeoutDelay = 1000000;

	mDriveScheduler.SetEvent(kTimeoutDelay, this, kEventId_DriveTimeout, mpEventDriveTimeout);

	// trigger pulse on /NMI
	mbNmiTimeout = false;
	UpdateNmi();
}
