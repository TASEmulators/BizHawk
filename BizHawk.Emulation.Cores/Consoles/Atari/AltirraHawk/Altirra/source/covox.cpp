//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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
#include <vd2/system/binary.h>
#include <at/atcore/consoleoutput.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicesystemcontrol.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include "covox.h"
#include "audiooutput.h"
#include "memorymanager.h"
#include "console.h"

namespace {
	// 1/128 for mapping unsigned 8-bit PCM to [-1, 1] (well, almost)
	// 1/2 for two source channels summed on each output channel
	// 1/28 for crude box filtering to mix rate
	const float kOutputScale = 1.0f / 128.0f / 2.0f * 60.0f;

	// Two channels, for the full 28 ticks per sample, at a value of 0x80.
	const float kOutputBias = -128.0f * 2.0f * 28.0f;
}

ATCovoxEmulator::ATCovoxEmulator() {
}

ATCovoxEmulator::~ATCovoxEmulator() {
	Shutdown();
}

void ATCovoxEmulator::SetAddressRange(uint32 addrLo, uint32 addrHi, bool passWrites) {
	if (mAddrLo != addrLo || mAddrHi != addrHi || mbPassWrites != passWrites) {
		mAddrLo = addrLo;
		mAddrHi = addrHi;
		mbPassWrites = passWrites;

		InitMapping();
	}
}

void ATCovoxEmulator::SetEnabled(bool enable) {
	if (mbEnabled == enable)
		return;

	mbEnabled = enable;

	if (mpMemLayerControl)
		mpMemMan->EnableLayer(mpMemLayerControl, kATMemoryAccessMode_CPUWrite, mbEnabled);
}

void ATCovoxEmulator::Init(ATMemoryManager *memMan, ATScheduler *sch, IATAudioMixer *mixer) {
	mpMemMan = memMan;
	mpScheduler = sch;
	mpAudioMixer = mixer;

	mixer->AddSyncAudioSource(this);

	InitMapping();

	ColdReset();

	std::fill(std::begin(mVolume), std::end(mVolume), 0x80);
}

void ATCovoxEmulator::Shutdown() {
	if (mpMemMan) {
		if (mpMemLayerControl) {
			mpMemMan->DeleteLayer(mpMemLayerControl);
			mpMemLayerControl = NULL;
		}

		mpMemMan = NULL;
	}

	if (mpAudioMixer) {
		mpAudioMixer->RemoveSyncAudioSource(this);
		mpAudioMixer = nullptr;
	}
}

void ATCovoxEmulator::ColdReset() {
	mLastUpdate = ATSCHEDULER_GETTIME(mpScheduler);

	for(int i=0; i<4; ++i)
		mVolume[i] = 0x80;

	WarmReset();
}

void ATCovoxEmulator::WarmReset() {
	memset(mAccumBufferLeft, 0, sizeof mAccumBufferLeft);
	memset(mAccumBufferRight, 0, sizeof mAccumBufferRight);

	mOutputCount = 0;
	mOutputLevel = 0;
	mOutputAccumLeft = kOutputBias;
	mOutputAccumRight = kOutputBias;
	mbUnbalanced = false;
	mbUnbalancedSticky = false;
}

void ATCovoxEmulator::DumpStatus(ATConsoleOutput& output) {
	output("Channel outputs: $%02X $%02X $%02X $%02X"
		, mVolume[0]
		, mVolume[1]
		, mVolume[2]
		, mVolume[3]
	);
}

void ATCovoxEmulator::WriteControl(uint8 addr, uint8 value) {
	addr &= 3;

	const uint8 prevValue = mVolume[addr];
	if (prevValue == value)
		return;

	Flush();

	mVolume[addr] = value;

	mbUnbalanced = mVolume[0] + mVolume[3] != mVolume[1] + mVolume[2];

	if (mbUnbalanced)
		mbUnbalancedSticky = true;
}

void ATCovoxEmulator::WriteMono(uint8 value) {
	if (mVolume[0] != value ||
		mVolume[1] != value ||
		mVolume[2] != value ||
		mVolume[3] != value)
	{
		Flush();

		mVolume[0] = value;
		mVolume[1] = value;
		mVolume[2] = value;
		mVolume[3] = value;
		mbUnbalanced = false;
	}
}

void ATCovoxEmulator::Run(int cycles) {
	float vl = (float)(mVolume[0] + mVolume[3]);
	float vr = (float)(mVolume[1] + mVolume[2]);

	if (mOutputCount) {
		int tc = (int)(28 - mOutputCount);

		if (tc > cycles)
			tc = cycles;

		cycles -= tc;

		mOutputAccumLeft += vl * tc;
		mOutputAccumRight += vr * tc;
		mOutputCount += tc;

		if (mOutputCount < 28)
			return;

		if (mOutputLevel < kAccumBufferSize) {
			mAccumBufferLeft[mOutputLevel] = mOutputAccumLeft;
			mAccumBufferRight[mOutputLevel] = mOutputAccumRight;
			++mOutputLevel;
		}

		mOutputAccumLeft = kOutputBias;
		mOutputAccumRight = kOutputBias;
		mOutputCount = 0;
	}

	while(cycles >= 28) {
		if (mOutputLevel >= kAccumBufferSize) {
			cycles %= 28;
			break;
		}

		mAccumBufferLeft[mOutputLevel] = vl * 28 + kOutputBias;
		mAccumBufferRight[mOutputLevel] = vr * 28 + kOutputBias;
		++mOutputLevel;
		cycles -= 28;
	}

	mOutputAccumLeft = kOutputBias;
	mOutputAccumRight = kOutputBias;
	mOutputCount = 0;

	if (cycles) {
		mOutputAccumLeft += vl * cycles;
		mOutputAccumRight += vr * cycles;
		mOutputCount = cycles;
	}
}

void ATCovoxEmulator::WriteAudio(const ATSyncAudioMixInfo& mixInfo) {
	float *const dstLeft = mixInfo.mpLeft;
	float *const dstRightOpt = mixInfo.mpRight;
	const uint32 n = mixInfo.mCount;

	Flush();

	VDASSERT(n <= kAccumBufferSize);

	// if we don't have enough samples, pad out; eventually we'll catch up enough
	if (mOutputLevel < n) {
		memset(mAccumBufferLeft + mOutputLevel, 0, sizeof(mAccumBufferLeft[0]) * (n - mOutputLevel));
		memset(mAccumBufferRight + mOutputLevel, 0, sizeof(mAccumBufferRight[0]) * (n - mOutputLevel));

		mOutputLevel = n;
	}

	if (mbEnabled) {
		float volume = mixInfo.mpMixLevels[kATAudioMix_Covox] * kOutputScale;

		if (dstRightOpt) {
			for(uint32 i=0; i<n; ++i) {
				dstLeft[i] += mAccumBufferLeft[i] * volume;
				dstRightOpt[i] += mAccumBufferRight[i] * volume;
			}
		} else {
			volume *= 0.5f;

			for(uint32 i=0; i<n; ++i)
				dstLeft[i] += (mAccumBufferLeft[i] + mAccumBufferRight[i]) * volume;
		}
	}

	// shift down accumulation buffers
	uint32 samplesLeft = mOutputLevel - n;

	if (samplesLeft) {
		memmove(mAccumBufferLeft, mAccumBufferLeft + n, samplesLeft * sizeof(mAccumBufferLeft[0]));
		memmove(mAccumBufferRight, mAccumBufferRight + n, samplesLeft * sizeof(mAccumBufferRight[0]));
	}

	mOutputLevel = samplesLeft;
	mbUnbalancedSticky = mbUnbalanced;
}

void ATCovoxEmulator::Flush() {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	uint32 dt = t - mLastUpdate;
	mLastUpdate = t;

	Run(dt);
}

void ATCovoxEmulator::InitMapping() {
	if (mpMemMan) {
		if (mpMemLayerControl) {
			mpMemMan->DeleteLayer(mpMemLayerControl);
			mpMemLayerControl = NULL;
		}

		ATMemoryHandlerTable handlers = {};
		handlers.mpThis = this;

		handlers.mbPassAnticReads = true;
		handlers.mbPassReads = true;
		handlers.mbPassWrites = true;
		handlers.mpDebugReadHandler = StaticReadControl;
		handlers.mpReadHandler = StaticReadControl;
		handlers.mpWriteHandler = StaticWriteControl;

		const uint8 pageLo = (uint8)(mAddrLo >> 8);
		const uint8 pageHi = (uint8)(mAddrHi >> 8);

		mpMemLayerControl = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay, handlers, pageLo, pageHi - pageLo + 1);

		mpMemMan->EnableLayer(mpMemLayerControl, kATMemoryAccessMode_CPUWrite, mbEnabled);
		mpMemMan->SetLayerName(mpMemLayerControl, "Covox");
	}
}

sint32 ATCovoxEmulator::StaticReadControl(void *thisptr, uint32 addr) {
	return -1;
}

bool ATCovoxEmulator::StaticWriteControl(void *thisptr0, uint32 addr, uint8 value) {
	auto *thisptr = (ATCovoxEmulator *)thisptr0;

	if (addr >= thisptr->mAddrLo && addr <= thisptr->mAddrHi) {
		uint8 addr8 = (uint8)addr;

		if (thisptr->mbFourCh)
			thisptr->WriteControl(addr8, value);
		else
			thisptr->WriteMono(value);

		return !thisptr->mbPassWrites;
	}

	return false;
}

///////////////////////////////////////////////////////////////////////////

class ATDeviceCovox final : public VDAlignedObject<16>
					, public ATDevice
					, public IATDeviceMemMap
					, public IATDeviceScheduling
					, public IATDeviceAudioOutput
					, public IATDeviceDiagnostics
					, public IATDeviceCovoxControl
{
public:
	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;
	virtual void GetSettings(ATPropertySet& settings) override;
	virtual bool SetSettings(const ATPropertySet& settings) override;
	virtual void Init() override;
	virtual void Shutdown() override;

public: // IATDeviceMemMap
	virtual void InitMemMap(ATMemoryManager *memmap) override;
	virtual bool GetMappedRange(uint32 index, uint32& lo, uint32& hi) const override;

public:	// IATDeviceScheduling
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceAudioOutput
	virtual void InitAudioOutput(IATAudioMixer *mixer) override;

public:	// IATDeviceDiagnostics
	virtual void DumpStatus(ATConsoleOutput& output) override;

public:	// IATDeviceCovoxControl
	virtual void InitCovoxControl(IATCovoxController& controller) override;

private:
	void OnCovoxEnabled(bool enabled);

	ATMemoryManager *mpMemMan = nullptr;
	ATScheduler *mpScheduler = nullptr;
	IATAudioMixer *mpAudioMixer = nullptr;

	uint32 mAddrLo = 0xD600;
	uint32 mAddrHi = 0xD6FF;

	IATCovoxController *mpCovoxController = nullptr;
	vdfunction<void(bool)> mCovoxCallback;

	ATCovoxEmulator mCovox;
};

void ATCreateDeviceCovox(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceCovox> p(new ATDeviceCovox);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefCovox = { "covox", "covox", L"Covox", ATCreateDeviceCovox };

void *ATDeviceCovox::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceMemMap::kTypeID:
			return static_cast<IATDeviceMemMap *>(this);

		case IATDeviceScheduling::kTypeID:
			return static_cast<IATDeviceScheduling *>(this);

		case IATDeviceAudioOutput::kTypeID:
			return static_cast<IATDeviceAudioOutput *>(this);

		case IATDeviceDiagnostics::kTypeID:
			return static_cast<IATDeviceDiagnostics *>(this);

		case IATDeviceCovoxControl::kTypeID:
			return static_cast<IATDeviceCovoxControl *>(this);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceCovox::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefCovox;
}

void ATDeviceCovox::WarmReset() {
	mCovox.WarmReset();
}

void ATDeviceCovox::ColdReset() {
	mCovox.ColdReset();
}

void ATDeviceCovox::GetSettings(ATPropertySet& settings) {
	settings.SetUint32("base", mAddrLo);
	settings.SetUint32("channels", mCovox.IsFourChannels() ? 4 : 1);
}

bool ATDeviceCovox::SetSettings(const ATPropertySet& settings) {
	uint32 baseAddr = settings.GetUint32("base", 0xD600);

	switch(baseAddr) {
		case 0xD280:
			mAddrLo = baseAddr;
			mAddrHi = baseAddr + 0x7F;
			mCovox.SetAddressRange(0xD280, 0xD2FF, false);
			break;

		case 0xD100:
		case 0xD500:
		case 0xD600:
		case 0xD700:
			mAddrLo = baseAddr;
			mAddrHi = baseAddr + 0xFF;
			mCovox.SetAddressRange(baseAddr, baseAddr + 0xFF, true);
			break;
	}

	uint32 channels = settings.GetUint32("channels", 4);
	mCovox.SetFourChannels(channels > 1);

	return true;
}

void ATDeviceCovox::Init() {
	mCovox.SetAddressRange(mAddrLo, mAddrHi, mAddrLo != 0xD280);
	mCovox.Init(mpMemMan, mpScheduler, mpAudioMixer);
}

void ATDeviceCovox::Shutdown() {
	if (mpCovoxController) {
		mpCovoxController->GetCovoxEnableNotifyList().Remove(&mCovoxCallback);
		mpCovoxController = nullptr;
	}

	mCovox.Shutdown();

	mpAudioMixer = nullptr;
	mpScheduler = nullptr;
	mpMemMan = nullptr;
}

void ATDeviceCovox::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;
}

bool ATDeviceCovox::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	if (index == 0) {
		lo = mAddrLo;
		hi = mAddrHi + 1;
		return true;
	}

	return false;
}

void ATDeviceCovox::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATDeviceCovox::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

void ATDeviceCovox::DumpStatus(ATConsoleOutput& output) {
	mCovox.DumpStatus(output);
}

void ATDeviceCovox::InitCovoxControl(IATCovoxController& controller) {
	mpCovoxController = &controller;
	mpCovoxController->GetCovoxEnableNotifyList().Add(&mCovoxCallback);

	OnCovoxEnabled(mpCovoxController->IsCovoxEnabled());
	mCovoxCallback = [this](bool enable) { OnCovoxEnabled(enable); };
}

void ATDeviceCovox::OnCovoxEnabled(bool enabled) {
	mCovox.SetEnabled(enabled);
}
