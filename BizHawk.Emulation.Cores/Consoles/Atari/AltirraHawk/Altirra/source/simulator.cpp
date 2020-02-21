//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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
#include <vd2/system/bitmath.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/filesys.h>
#include <vd2/system/strutil.h>
#include <vd2/system/zip.h>
#include <vd2/system/int128.h>
#include <vd2/system/hash.h>
#include <at/atcore/address.h>
#include <at/atcore/bussignal.h>
#include <at/atcore/constants.h>
#include <at/atcore/device.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/devicediskdrive.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/deviceprinter.h>
#include <at/atcore/devicevideo.h>
#include <at/atcore/deviceu1mb.h>
#include <at/atcore/media.h>
#include <at/atcore/memoryutils.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/vfs.h>
#include <at/atio/blobimage.h>
#include <at/atio/cassetteimage.h>
#include <at/atio/image.h>
#include <at/atio/programimage.h>
#include <at/atio/savestate.h>
#include "simulator.h"
#include "cassette.h"
#include "console.h"
#include "joystick.h"
#include "ksyms.h"
#include "kerneldb.h"
#include "debugger.h"
#include "debuggerlog.h"
#include "disk.h"
#include "oshelper.h"
#include "savestate.h"
#include "cartridge.h"
#include "cartridgeport.h"
#include "resource.h"
#include "inputcontroller.h"
#include "inputmanager.h"
#include "cio.h"
#include "vbxe.h"
#include "profiler.h"
#include "verifier.h"
#include "uirender.h"
#include "audiomonitor.h"
#include "audiooutput.h"
#include "audiosampleplayer.h"
#include "cheatengine.h"
#include "mmu.h"
#include "pbi.h"
#include "ultimate1mb.h"
#include "virtualscreen.h"
#include "pokeytables.h"
#include "cpuhookmanager.h"
#include "cpuheatmap.h"
#include "cputracer.h"
#include "siomanager.h"
#include "hlebasicloader.h"
#include "hleprogramloader.h"
#include "hlefpaccelerator.h"
#include "hlefastboothook.h"
#include "hleciohook.h"
#include "hleutils.h"
#include "versioninfo.h"
#include "firmwaremanager.h"
#include "slightsid.h"
#include "debugtarget.h"
#include "sapconverter.h"
#include "irqcontroller.h"
#include "trace.h"
#include "tracevideo.h"
#include "rapidus.h"
#include "mediamanager.h"

namespace {
	const char kSaveStateVersion[] = "Altirra save state V1";

	constexpr VDFraction kSlowSchedulerRateNTSC { 3579545, 228 };
	constexpr VDFraction kSlowSchedulerRatePAL { 3546895, 228 };
	constexpr VDFraction kSlowSchedulerRateSECAM { 445375, 57 };

	class PokeyDummyConnection : public IATPokeyEmulatorConnections {
	public:
		void PokeyAssertIRQ(bool cpuBased) {}
		void PokeyNegateIRQ(bool cpuBased) {}
		void PokeyBreak() {}
		bool PokeyIsInInterrupt() const { return false; }
		bool PokeyIsKeyPushOK(uint8, bool cooldownExpired) const { return true; }
	};

	PokeyDummyConnection g_pokeyDummyConnection;

	// 800XL
	void DRAMPatternFillA(void *mem, uint32 len) {
		uint32 len32 = len >> 2;
		VDMemset32(mem, VDFromBE32(0xFF00FF00), len32);

		uint8 *dst = (uint8 *)mem + (len32 << 2);

		switch(len & 3) {
			case 3:		dst[2] = 0xFF;
			case 2:		dst[1] = 0x00;
			case 1:		dst[0] = 0xFF;
			case 0:
			default:	break;
		}
	}

	// 5200 - (A0..A6) <- (A2, A1, A0, A3, A4, A5, A6)
	void DRAMPatternFillA_5200(void *mem, uint32 len) {
		static const uint32_t kPattern[4]={
			UINT32_C(0xFFFFFFFF),
			UINT32_C(0x00000000),
			UINT32_C(0xFFFFFFFF),
			UINT32_C(0x00000000),
		};

		uint32 len128 = len >> 4;
		uint32 *dst = (uint32 *)mem;

		while(len128--) {
			dst[0] = kPattern[0];
			dst[1] = kPattern[1];
			dst[2] = kPattern[2];
			dst[3] = kPattern[3];
			dst += 4;
		}

		if (len & 15)
			memcpy(dst, kPattern, len & 15);
	}

	// 130XE
	void DRAMPatternFillB(void *mem, uint32 len) {
		uint8 *dst = (uint8 *)mem;
		int phase = 0;

		while(len) {
			uint32 tc = len > 64 ? 64 : len;
			len -= tc;

			uint32 tc32 = tc >> 2;
			VDMemset32(dst, phase ? VDFromBE32(0x007F007F) : VDFromBE32(0x80FF80FF), tc32);
			dst += (tc32 << 2);

			switch(tc & 3) {
				case 3:		dst[2] = phase ? 0x80 : 0x00;
				case 2:		dst[1] = phase ? 0xFF : 0x7F;
				case 1:		dst[0] = phase ? 0x80 : 0x00;
				case 0:
				default:	break;
			}

			phase ^= 1;
		}
	}

	// 800XL-Ultimate1MB
	void DRAMPatternFillC(void *mem, uint32 len) {
		uint8 *dst = (uint8 *)mem;

		static const uint8 kPatterns[][4]={
			{ 0xF0, 0xF0, 0x00, 0x00 },
			{ 0xFF, 0xFF, 0x0F, 0x0F },
			{ 0x00, 0x00, 0xF0, 0xF0 },
			{ 0x0F, 0x0F, 0xFF, 0xFF },
		};

		static const uint8 kPatternOrder[]={
			0,1,0,1,0,1,0,1,2,3,2,3,2,3,2,3
		};

		int patternIndex = 0;
		while(len) {
			uint32 tc = len > 32 ? 32 : len;
			len -= tc;

			const uint8 *pat = kPatterns[kPatternOrder[patternIndex++ & 15]];

			while(tc >= 4) {
				tc -= 4;

				dst[0] = pat[0];
				dst[1] = pat[1];
				dst[2] = pat[2];
				dst[3] = pat[3];
				dst += 4;
			}

			for(uint32 i=0; i<tc; ++i)
				dst[i] = pat[i];
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATSimulator::PrivateData final
	: public IATCPUTimestampDecoderProvider
	, public IATDiskDriveManager
	, public IATSystemController
	, public IATCovoxController
	, public IATDeviceChangeCallback
{
public:
	PrivateData(ATSimulator& parent)
		: mParent(parent)
	{
		mFloatingInputs.mpScheduler = parent.GetScheduler();
		mFloatingInputs.mFloatingInputMask = 0;
		mFloatingInputs.mRandomSeed = 0;

		mCartPort.SetLeftMapChangedHandler([this](bool) { mParent.UpdateXLCartridgeLine(); });

		mCovoxEnableSignal.SetOnUpdated([this] {
			const bool state = IsCovoxEnabled();

			mCovoxEnableChangedNotifyList.NotifyAll([=](const auto& pfn) {
				(*pfn)(state);
			});
		});
	}

	ATCPUTimestampDecoder GetTimestampDecoder() const override {
		return mParent.GetTimestampDecoder();
	}

	ATDiskInterface *GetDiskInterface(uint32 index) override {
		return mpDiskInterfaces[index];
	}

public:		// IATSystemController
	void ResetCPU() override;
	void ResetComputer() override;
	void AssertABORT() override;
	void OverrideCPUMode(IATDeviceSystemControl *source, bool use816, uint32 multiplier) override;
	void OverrideKernelMapping(IATDeviceSystemControl *source, const void *kernelROM, sint8 highSpeed, bool priority) override;
	bool IsU1MBConfigPreLocked() override;
	void OnU1MBConfigPreLocked(bool inPreLockState) override;
	uint8 ReadConsoleButtons() const override;
	ATBusSignal& GetStereoEnableSignal() override { return mStereoEnableSignal; }
	ATBusSignal& GetCovoxEnableSignal() override { return mCovoxEnableSignal; }

public:		// IATCovoxController
	bool IsCovoxEnabled() const override { return mCovoxEnableSignal.OrDefaultTrue(); }
	ATNotifyList<const vdfunction<void(bool)> *>& GetCovoxEnableNotifyList() override { return mCovoxEnableChangedNotifyList; }

public:
	void OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) override;
	void OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) override;
	void OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) override;

public:
	void OnRemovingDeviceInterface(IATDeviceSystemControl& syscon);

	static void PIAChangeCommandLine(void *thisPtr, uint32 output);
	static void PIAChangeMotorLine(void *thisPtr, uint32 output);
	static void PIAChangeMultiJoy(void *thisPtr, uint32 output);
	static void PIAChangeBanking(void *thisPtr, uint32 output);

	ATSimulator& mParent;
	vdautoptr<ATTraceContext> mpTraceContext;
	vdautoptr<ATCPUTracer> mpCPUTracer;
	vdrefptr<IATTraceChannelVideo> mpTraceChannelVideo;
	vdrefptr<IATVideoTracer> mpVideoTracer;
	uint64 mTraceSizeLimit = UINT64_MAX;
	ATRapidusDevice *mpRapidus = nullptr;

	IATDeviceSystemControl *mpCPUModeOverrider = nullptr;
	ATCPUMode mCPUMode = kATCPUMode_6502;
	uint32 mCPUSubCycles = 1;

	uint64 mColdResetTime = 0;
	bool mbExtRAMClearedOnce = false;
	bool mbInU1MBPreLock = false;

	struct KernelROMOverride {
		IATDeviceSystemControl *mpSource = nullptr;
		const void *mpROM = nullptr;
		sint8 mSpeedOverride = 0;
	};

	KernelROMOverride mKernelROMOverrides[2] {};

	ATIRQController	mIRQController;
	ATCartridgePort mCartPort;
	ATPIAFloatingInputs mFloatingInputs = {};
	ATDeferredEventManager mDeferredEventManager;

	vdblock<uint8> mHighMemory;

	ATBusSignal mStereoEnableSignal;
	ATBusSignal mCovoxEnableSignal;
	ATNotifyList<const vdfunction<void(bool)> *> mCovoxEnableChangedNotifyList;

	ATDiskInterface *mpDiskInterfaces[15] = {};
};

void ATSimulator::PrivateData::ResetCPU() {
	mParent.mCPU.WarmReset();

	mParent.mpCPUHookManager->CallResetHooks();
	mParent.mpCPUHookManager->CallInitHooks(mParent.mpKernelLowerROM, mParent.mpKernelUpperROM);
}

void ATSimulator::PrivateData::ResetComputer() {
	mParent.InternalWarmReset(true);
}

void ATSimulator::PrivateData::AssertABORT() {
	mParent.mCPU.AssertABORT();
}

void ATSimulator::PrivateData::OverrideCPUMode(IATDeviceSystemControl *source, bool use816, uint32 multiplier) {
	mpCPUModeOverrider = source;

	mParent.mCPU.SetCPUMode(use816 ? kATCPUMode_65C816 : kATCPUMode_6502, multiplier);

	ResetCPU();
}

void ATSimulator::PrivateData::OverrideKernelMapping(IATDeviceSystemControl *source, const void *kernelROM, sint8 highSpeed, bool priority) {
	KernelROMOverride& override = mKernelROMOverrides[priority];

	if (kernelROM) {
		override.mpSource = source;
		override.mSpeedOverride = highSpeed;
	} else {
		if (override.mpSource != source)
			return;

		override.mpSource = nullptr;
		override.mSpeedOverride = 0;
	}

	if (override.mpROM != kernelROM) {
		override.mpROM = kernelROM;

		mParent.UpdateKernelROMSegments();
		mParent.UpdateKernelROMPtrs();
		mParent.UpdateKernelROMSpeeds();

		mParent.mpCPUHookManager->CallResetHooks();
		mParent.mpCPUHookManager->CallInitHooks(mParent.mpKernelLowerROM, mParent.mpKernelUpperROM);
	}
}

bool ATSimulator::PrivateData::IsU1MBConfigPreLocked() {
	return mbInU1MBPreLock;
}

void ATSimulator::PrivateData::OnU1MBConfigPreLocked(bool inPreLockState) {
	if (mbInU1MBPreLock == inPreLockState)
		return;

	mbInU1MBPreLock = inPreLockState;

	for(IATDeviceSystemControl *dsc : mParent.mpDeviceManager->GetInterfaces<IATDeviceSystemControl>(false, false)) {
		dsc->OnU1MBConfigPreLocked(inPreLockState);
	}
}

uint8 ATSimulator::PrivateData::ReadConsoleButtons() const {
	return mParent.mGTIA.ReadConsoleSwitches() & 7;
}

void ATSimulator::PrivateData::OnDeviceAdded(uint32 iid, IATDevice *dev, void *iface) {
	if (iid == ATSlightSIDEmulator::kTypeID) {
		IATUIRenderer *uir = mParent.GetUIRenderer();

		if (uir)
			uir->SetSlightSID((ATSlightSIDEmulator *)iface);
	} else if (iid == IATDeviceButtons::kTypeID) {
		mParent.UpdateCartridgeSwitch();
	} else if (iid == ATRapidusDevice::kTypeID) {
		mpRapidus = ((ATRapidusDevice *)iface);

		if (mParent.mpMemLayerLoRAM)
			mParent.mpMemMan->SetLayerFastBus(mParent.mpMemLayerLoRAM, false);

		if (mParent.mpMemLayerHiRAM)
			mParent.mpMemMan->SetLayerFastBus(mParent.mpMemLayerHiRAM, false);

		mParent.mpMemMan->SetFastBusEnabled(true);
		mParent.mpMemMan->SetHighMemoryEnabled(true);
		mParent.mpMMU->SetHighMemory(0, nullptr);
	}

	++mParent.mConfigChangeCounter;
}

void ATSimulator::PrivateData::OnDeviceRemoving(uint32 iid, IATDevice *dev, void *iface) {
	++mParent.mConfigChangeCounter;

	if (iid == ATSlightSIDEmulator::kTypeID) {
		IATUIRenderer *uir = mParent.GetUIRenderer();

		if (uir)
			uir->SetSlightSID(nullptr);
	} else if (iid == ATVBXEEmulator::kTypeID) {
		if (mParent.mpVBXE && mParent.mpVBXE == (ATVBXEEmulator *)iface) {
			mParent.mGTIA.SetVBXE(NULL);
			mParent.mpVBXE = nullptr;
		}
	} else if (iid == ATRapidusDevice::kTypeID) {
		mpRapidus = nullptr;

		if (mParent.mpMemLayerLoRAM)
			mParent.mpMemMan->SetLayerFastBus(mParent.mpMemLayerLoRAM, true);

		if (mParent.mpMemLayerHiRAM)
			mParent.mpMemMan->SetLayerFastBus(mParent.mpMemLayerHiRAM, true);

		mParent.mpMemMan->SetFastBusEnabled(mParent.mCPU.GetSubCycles() > 1);
		mParent.mpMemMan->SetHighMemoryEnabled(mParent.mHighMemoryBanks >= 0);

		if (mParent.mHighMemoryBanks >= 0) {
			mParent.mpMMU->SetHighMemory(mParent.mHighMemoryBanks, mHighMemory.data());
		}
	} else if (iid == IATDeviceSystemControl::kTypeID) {
		OnRemovingDeviceInterface(*(IATDeviceSystemControl *)iface);
	}
}

void ATSimulator::PrivateData::OnRemovingDeviceInterface(IATDeviceSystemControl& syscon) {
	if (mpCPUModeOverrider == &syscon) {
		mpCPUModeOverrider = nullptr;
		mParent.mCPU.SetCPUMode(mCPUMode, mCPUSubCycles);
	}

	for(auto& override : mKernelROMOverrides) {
		if (override.mpSource == &syscon) {
			override.mpSource = nullptr;
			override.mpROM = nullptr;
			override.mSpeedOverride = 0;

			mParent.UpdateKernelROMSegments();
			mParent.UpdateKernelROMPtrs();
			mParent.UpdateKernelROMSpeeds();
		}
	}
}

void ATSimulator::PrivateData::OnDeviceRemoved(uint32 iid, IATDevice *dev, void *iface) {
	if (iid == IATDeviceDiskDrive::kTypeID) {
		// Scan all the disk drives, and re-enable the built-in ones as
		// necessary.
		for(int i=0; i<15; ++i) {
			auto& dif = mParent.GetDiskInterface(i);

			if (dif.GetClientCount() == 1 && dif.GetDiskImage()) {
				auto& disk = mParent.GetDiskDrive(i);

				disk.SetEnabled(true);
			}
		}
	}
}
void ATSimulator::PrivateData::PIAChangeCommandLine(void *thisPtr, uint32 output) {
	ATSimulator& sim = *(ATSimulator *)thisPtr;

	const bool state = !(output & kATPIAOutput_CB2);

	sim.mPokey.SetCommandLine(state);
}

void ATSimulator::PrivateData::PIAChangeMotorLine(void *thisPtr, uint32 output) {
	ATSimulator& sim = *(ATSimulator *)thisPtr;

	const bool state = !(output & kATPIAOutput_CA2);

	sim.mPokey.SetAudioLine2(state ? 32 : 0);
}

void ATSimulator::PrivateData::PIAChangeMultiJoy(void *thisPtr, uint32 output) {
	ATSimulator& sim = *(ATSimulator *)thisPtr;

	if (sim.mpInputManager)
		sim.mpInputManager->SelectMultiJoy((output & 0x70) >> 4);
}

void ATSimulator::PrivateData::PIAChangeBanking(void *thisPtr, uint32 output) {
	ATSimulator& sim = *(ATSimulator *)thisPtr;

	sim.UpdateBanking(sim.GetBankRegister());
}

///////////////////////////////////////////////////////////////////////////

ATSimulator::ATSimulator()
	: mbRunning(false)
	, mbPaused(false)
	, mbShadowROM(true)
	, mbShadowCartridge(false)
	, mpPrivateData(nullptr)
	, mbROMAutoReloadEnabled(false)
	, mbAutoLoadKernelSymbols(false)
	, mpMemMan(nullptr)
	, mpMMU(nullptr)
	, mpPBIManager(nullptr)
	, mpCPUHookManager(nullptr)
	, mpSimEventManager(nullptr)
	, mPokey(false)
	, mPokey2(true)
	, mpAudioOutput(NULL)
	, mpAudioSamplePlayer(NULL)
	, mpPokeyTables(nullptr)
	, mpAudioMonitors()
	, mpCassette(NULL)
	, mpJoysticks(NULL)
	, mbDiskSectorCounterEnabled(false)
	, mpProfiler(NULL)
	, mpVerifier(NULL)
	, mpHeatMap(NULL)
	, mpDebugTarget(nullptr)
	, mpMemLayerLoRAM(NULL)
	, mpMemLayerHiRAM(NULL)
	, mpMemLayerExtendedRAM(NULL)
	, mpMemLayerLowerKernelROM(NULL)
	, mpMemLayerUpperKernelROM(NULL)
	, mpMemLayerBASICROM(NULL)
	, mpMemLayerGameROM(NULL)
	, mpMemLayerSelfTestROM(NULL)
	, mpMemLayerHiddenRAM(NULL)
	, mpMemLayerANTIC(NULL)
	, mpMemLayerGTIA(NULL)
	, mpMemLayerPOKEY(NULL)
	, mpMemLayerPIA(NULL)
	, mpMemLayerIoBusFloat(nullptr)
	, mpInputManager(nullptr)
	, mpPortAController(nullptr)
	, mpPortBController(nullptr)
	, mpLightPen(nullptr)
	, mpPrinterOutput(nullptr)
	, mpVBXE(NULL)
	, mpCheatEngine(NULL)
	, mpUIRenderer(NULL)
	, mpUltimate1MB(NULL)
	, mpVirtualScreenHandler(NULL)
	, mpSIOManager(nullptr)
	, mpHLEBasicLoader(NULL)
	, mpHLEProgramLoader(NULL)
	, mpHLEFPAccelerator(NULL)
	, mpHLEFastBootHook(NULL)
	, mpHLECIOHook(NULL)
	, mAxlonMemoryBits(0)
	, mbAxlonAliasingEnabled(false)
	, mHighMemoryBanks(0)
	, mConfigChangeCounter(0)
	, mpFirmwareManager(nullptr)
	, mpDeviceManager(nullptr)
{
}

ATSimulator::~ATSimulator() {
	Shutdown();
}

void ATSimulator::Init() {
	VDASSERT(!mpPrivateData);

	mpPrivateData = new PrivateData(*this);
	mpMemMan = new ATMemoryManager;
	mpMMU = new ATMMUEmulator;
	mpMMU->Init(mpMemMan);
	mpPBIManager = new ATPBIManager;
	mpCPUHookManager = new ATCPUHookManager;
	mpSimEventManager = new ATSimulatorEventManager;
	mpPokeyTables = new ATPokeyTables;
	mpInputManager = new ATInputManager;
	mpPortAController = new ATPortController;
	mpPortBController = new ATPortController;
	mpLightPen = new ATLightPenPort;
	mpSIOManager = new ATSIOManager;
	mpFirmwareManager = new ATFirmwareManager;
	mpDeviceManager = new ATDeviceManager;

	mpDeviceManager->Init();
	mpDeviceManager->AddDeviceChangeCallback(ATSlightSIDEmulator::kTypeID, mpPrivateData);
	mpDeviceManager->AddDeviceChangeCallback(IATDeviceButtons::kTypeID, mpPrivateData);
	mpDeviceManager->AddDeviceChangeCallback(IATDeviceDiskDrive::kTypeID, mpPrivateData);
	mpDeviceManager->AddDeviceChangeCallback(ATVBXEEmulator::kTypeID, mpPrivateData);
	mpDeviceManager->AddDeviceChangeCallback(ATRapidusDevice::kTypeID, mpPrivateData);
	mpDeviceManager->AddDeviceChangeCallback(IATDeviceSystemControl::kTypeID, mpPrivateData);
	mpDeviceManager->AddInitCallback([this](IATDevice& dev) { InitDevice(dev); });

	mpAnticBusData = &mpMemMan->mBusValue;

	mCartModuleIds[0] = 0;
	mCartModuleIds[1] = 0;

	mKernelSymbolsModuleIds[0] = 0;
	mKernelSymbolsModuleIds[1] = 0;

	mpCartridge[0] = NULL;
	mpCartridge[1] = NULL;

	ATCreateUIRenderer(&mpUIRenderer);

	mpMemMan->Init();
	
	mMemoryMode = kATMemoryMode_320K;
	mKernelMode = kATKernelMode_Default;
	mKernelId = 0;
	mActualKernelId = 0;
	mActualKernelFlags = 0;
	mBasicId = 0;
	mActualBasicId = 0;
	mHardwareMode = kATHardwareMode_800XL;

	mpPBIManager->Init(mpMemMan);

	mCPU.Init(mpMemMan, mpCPUHookManager, this);
	mpCPUHookManager->Init(&mCPU, mpMMU, mpPBIManager);

	mpDebugTarget = new ATDebuggerDefaultTarget;

	mpPrivateData->mIRQController.Init(&mCPU);
	mPIA.Init(&mpPrivateData->mIRQController, &mScheduler);

	mGTIA.Init(this);
	mAntic.Init(this, &mGTIA, &mScheduler, mpSimEventManager);

	mpSIOManager->Init(&mCPU, this);
	mpHLECIOHook = ATCreateHLECIOHook(&mCPU, this, mpMemMan);

	mpLightPen->Init(&mAntic);

	mpAudioSamplePlayer = new ATAudioSamplePlayer;
	mpAudioSamplePlayer->Init(&mScheduler);

	mpAudioOutput = ATCreateAudioOutput();
	mpAudioOutput->Init(mpAudioSamplePlayer);
	mpAudioOutput->AsMixer().AddSyncAudioSource(mpAudioSamplePlayer);

	mPokey.Init(this, &mScheduler, mpAudioOutput, mpPokeyTables);
	mPokey2.Init(&g_pokeyDummyConnection, &mScheduler, NULL, mpPokeyTables);

	mpPrivateData->mStereoEnableSignal.SetOnUpdated(
		[this] {
			this->mPokey.SetStereoSoftEnable(mpPrivateData->mStereoEnableSignal.AndDefaultTrue());
		}
	);

	mPIA.AllocOutput(PrivateData::PIAChangeMotorLine, this, kATPIAOutput_CA2);
	mPIA.AllocOutput(PrivateData::PIAChangeCommandLine, this, kATPIAOutput_CB2);
	mPIA.AllocOutput(PrivateData::PIAChangeMultiJoy, this, 0x70);			// port A control lines
	mPIA.AllocOutput(PrivateData::PIAChangeBanking, this, 0xFF00);			// port B control lines

	mpPortAController->Init(&mGTIA, &mPokey, &mPIA, mpLightPen, 0);
	mpPortBController->Init(&mGTIA, &mPokey, &mPIA, mpLightPen, 2);

	mpCassette = new ATCassetteEmulator;
	mpCassette->Init(&mPokey, &mScheduler, &mSlowScheduler, &mpAudioOutput->AsMixer(), &mpPrivateData->mDeferredEventManager, mpSIOManager);
	mpCassette->SetRandomizedStartEnabled(false);
	mPokey.SetCassette(mpCassette);

	mGTIA.SetUIRenderer(mpUIRenderer);

	for(uint32 i=0; i<15; ++i) {
		auto *&diskIf = mpPrivateData->mpDiskInterfaces[i];

		diskIf = new ATDiskInterface;
		diskIf->Init(i, mpUIRenderer);
	}

	for(int i=0; i<15; ++i) {
		mpDiskDrives[i] = new ATDiskEmulator;
		mpDiskDrives[i]->InitSIO(mpSIOManager);
		mpDiskDrives[i]->Init(i, mpPrivateData->mpDiskInterfaces[i], &mScheduler, &mSlowScheduler, mpAudioSamplePlayer);
	}

	mPendingEvent = kATSimEvent_None;

	mbBreak = false;
	mbBreakOnFrameEnd = false;
	mbTurbo = false;
	mbFrameSkip = true;
	mGTIA.SetFrameSkip(true);

	mVideoStandard = kATVideoStandard_NTSC;
	mpUIRenderer->SetCyclesPerSecond(kATMasterClockFrac_NTSC.asDouble());
	mScheduler.SetRate(kATMasterClockFrac_NTSC);
	mSlowScheduler.SetRate(kSlowSchedulerRateNTSC);

	mMemoryClearMode = kATMemoryClearMode_DRAM1;
	mbFloatingIoBus = false;
	mbRandomFillEXEEnabled = false;
	mbDiskSIOOverrideDetectEnabled = false;
	mbCassetteSIOPatchEnabled = false;
	mbCassetteAutoBootEnabled = false;
	mbCassetteRandomizedStartEnabled = false;
	mbBASICEnabled = false;
	mbFPPatchEnabled = false;
	mbDualPokeys = false;
	mbFastBoot = true;
	mbKeyboardPresent = true;
	mbForcedSelfTest = false;
	mbCartridgeSwitch = true;
	mbMapRAM = false;
	mbPreserveExtRAM = false;

	mStartupDelay = 0;
	mStartupDelay2 = 0;

	SetDiskSIOPatchEnabled(true);
	SetCassetteSIOPatchEnabled(true);
	SetCassetteAutoBootEnabled(true);

	mBreakOnScanline = -1;

	mpAnticReadPageMap = mpMemMan->GetAnticMemoryMap();

	mpInputManager->Init(&mScheduler, &mSlowScheduler, mpPortAController, mpPortBController, mpLightPen);
}

void ATSimulator::Shutdown() {
	if (!mpPrivateData)
		return;

	mpPrivateData->mStereoEnableSignal.Shutdown();

	SetTracingEnabled(false);

	if (mpDeviceManager)
		mpDeviceManager->RemoveAllDevices(true);

	mpDeviceManager->RemoveDeviceChangeCallback(ATSlightSIDEmulator::kTypeID, mpPrivateData);
	mpDeviceManager->RemoveDeviceChangeCallback(ATVBXEEmulator::kTypeID, mpPrivateData);
	mpDeviceManager->RemoveDeviceChangeCallback(IATDeviceButtons::kTypeID, mpPrivateData);
	mpDeviceManager->RemoveDeviceChangeCallback(IATDeviceDiskDrive::kTypeID, mpPrivateData);
	mpDeviceManager->RemoveDeviceChangeCallback(ATRapidusDevice::kTypeID, mpPrivateData);
	mpDeviceManager->RemoveDeviceChangeCallback(IATDeviceSystemControl::kTypeID, mpPrivateData);

	if (mpHLEFastBootHook) {
		ATDestroyHLEFastBootHook(mpHLEFastBootHook);
		mpHLEFastBootHook = NULL;
	}

	if (mpHLEFPAccelerator) {
		ATDestroyHLEFPAccelerator(mpHLEFPAccelerator);
		mpHLEFPAccelerator = NULL;
	}

	if (mpHLEProgramLoader) {
		mpHLEProgramLoader->Shutdown();
		delete mpHLEProgramLoader;
		mpHLEProgramLoader = NULL;
	}

	if (mpHLEBasicLoader) {
		mpHLEBasicLoader->Shutdown();
		delete mpHLEBasicLoader;
		mpHLEBasicLoader = NULL;
	}

	if (mpDebugTarget) {
		delete static_cast<ATDebuggerDefaultTarget *>(mpDebugTarget);
		mpDebugTarget = nullptr;
	}

	vdsafedelete <<= mpDiskDrives;

	if (mpPrivateData) {
		for(auto*& diskIf : mpPrivateData->mpDiskInterfaces) {
			diskIf->Flush();
			diskIf->Shutdown();
			vdsafedelete <<= diskIf;
		}
	}

	if (mpJoysticks) {
		delete mpJoysticks;
		mpJoysticks = NULL;
	}

	if (mpInputManager)
		mpInputManager->Shutdown();

	if (mpProfiler) {
		mCPU.SetProfiler(NULL);
		delete mpProfiler;
		mpProfiler = NULL;
	}

	if (mpVerifier) {
		mCPU.SetVerifier(NULL);
		delete mpVerifier;
		mpVerifier = NULL;
	}

	if (mpHeatMap) {
		mCPU.SetHeatMap(NULL);
		delete mpHeatMap;
		mpHeatMap = NULL;
	}

	if (mpCassette) {
		mpCassette->Shutdown();
		delete mpCassette;
		mpCassette = NULL;
	}

	if (mpAudioSamplePlayer) {
		mpAudioOutput->AsMixer().RemoveSyncAudioSource(mpAudioSamplePlayer);
		mpAudioSamplePlayer->Shutdown();
		delete mpAudioSamplePlayer;
		mpAudioSamplePlayer = nullptr;
	}

	if (mpAudioOutput) {
		delete mpAudioOutput;
		mpAudioOutput = NULL;
	}

	mGTIA.SetUIRenderer(NULL);

	for(int i=0; i<2; ++i) {
		if (mpCartridge[i]) {
			delete mpCartridge[i];
			mpCartridge[i] = NULL;
		}
	}

	if (mpVirtualScreenHandler) {
		delete mpVirtualScreenHandler;
		mpVirtualScreenHandler = NULL;
	}

	if (mpUltimate1MB) {
		mpUltimate1MB->Shutdown();
		delete mpUltimate1MB;
		mpUltimate1MB = NULL;
	}

	vdsafedelete <<= mpAudioMonitors;

	delete mpJoysticks; mpJoysticks = NULL;
	delete mpInputManager; mpInputManager = NULL;
	delete mpPortAController; mpPortAController = NULL;
	delete mpPortBController; mpPortBController = NULL;
	delete mpLightPen; mpLightPen = NULL;
	delete mpCheatEngine; mpCheatEngine = NULL;
	delete mpPokeyTables; mpPokeyTables = NULL;

	vdsaferelease <<= mpPrinterOutput;

	if (mpUIRenderer) {
		mpUIRenderer->Release();
		mpUIRenderer = NULL;
	}

	if (mpPBIManager) {
		mpPBIManager->Shutdown();
		delete mpPBIManager;
		mpPBIManager = NULL;
	}

	if (mpHLECIOHook) {
		ATDestroyHLECIOHook(mpHLECIOHook);
		mpHLECIOHook = NULL;
	}

	if (mpSIOManager) {
		mpSIOManager->Shutdown();
		delete mpSIOManager;
		mpSIOManager = NULL;
	}

	if (mpCPUHookInitHook) {
		mpCPUHookManager->RemoveInitHook(mpCPUHookInitHook);
		mpCPUHookInitHook = nullptr;
	}

	if (mpCPUHookManager) {
		mpCPUHookManager->Shutdown();
		delete mpCPUHookManager;
		mpCPUHookManager = NULL;
	}

	if (mpSimEventManager) {
		mpSimEventManager->Shutdown();
		delete mpSimEventManager;
		mpSimEventManager = NULL;
	}

	delete mpMMU;
	mpMMU = NULL;

	ShutdownMemoryMap();
	delete mpMemMan; mpMemMan = NULL;

	vdsafedelete <<= mpFirmwareManager;
	vdsafedelete <<= mpDeviceManager;

	delete mpPrivateData; mpPrivateData = NULL;
}

void ATSimulator::SetJoystickManager(IATJoystickManager *jm) {
	mpJoysticks = jm;
}

bool ATSimulator::LoadROMs() {
	bool changed = UpdateKernel(true, true);

	InitMemoryMap();
	changed |= ReloadU1MBFirmware();

	for(IATDeviceFirmware *fw : mpDeviceManager->GetInterfaces<IATDeviceFirmware>(false, false))
		changed |= fw->ReloadFirmware();

	return changed;
}

void ATSimulator::NotifyEvent(ATSimulatorEvent ev) {
	mpSimEventManager->NotifyEvent(ev);
}

ATDiskInterface& ATSimulator::GetDiskInterface(int index) {
	return *mpPrivateData->mpDiskInterfaces[index];
}

bool ATSimulator::IsDiskSIOPatchEnabled() const {
	return mpSIOManager->GetDiskSIOAccelEnabled();
}

bool ATSimulator::IsDiskAccurateTimingEnabled() const {
	return mpPrivateData->mpDiskInterfaces[0]->IsAccurateSectorTimingEnabled();
}

bool ATSimulator::IsCassetteAutoRewindEnabled() const {
	return mpCassette->IsAutoRewindEnabled();
}

ATIRQController *ATSimulator::GetIRQController() {
	return &mpPrivateData->mIRQController;
}

IATDeviceSIOManager *ATSimulator::GetDeviceSIOManager() {
	return mpSIOManager;
}

IATDeviceCIOManager *ATSimulator::GetDeviceCIOManager() {
	return vdpoly_cast<IATDeviceCIOManager *>(mpHLECIOHook);
}

void ATSimulator::SetPrinterOutput(IATPrinterOutput *p) {
	if (mpPrinterOutput == p)
		return;

	if (p)
		p->AddRef();

	if (mpPrinterOutput)
		mpPrinterOutput->Release();

	mpPrinterOutput = p;

	for(IATDevicePrinter *pr : mpDeviceManager->GetInterfaces<IATDevicePrinter>(false, false))
		pr->SetPrinterOutput(p);
}

bool ATSimulator::GetDiskBurstTransfersEnabled() const {
	return mpSIOManager->GetDiskBurstTransfersEnabled();
}

void ATSimulator::SetDiskBurstTransfersEnabled(bool enabled) {
	mpSIOManager->SetDiskBurstTransfersEnabled(enabled);
}

bool ATSimulator::GetDeviceCIOBurstTransfersEnabled() const {
	return mpHLECIOHook->GetBurstTransfersEnabled();
}

void ATSimulator::SetDeviceCIOBurstTransfersEnabled(bool enabled) {
	mpHLECIOHook->SetBurstTransfersEnabled(enabled);
}

bool ATSimulator::GetDeviceSIOBurstTransfersEnabled() const {
	return mpSIOManager->GetBurstTransfersEnabled();
}

void ATSimulator::SetDeviceSIOBurstTransfersEnabled(bool enabled) {
	mpSIOManager->SetBurstTransfersEnabled(enabled);
}

bool ATSimulator::HasCIODevice(char c) const {
	return mpHLECIOHook->HasCIODevice(c);
}

bool ATSimulator::GetCIOPatchEnabled(char c) const {
	return mpHLECIOHook->GetCIOPatchEnabled(c);
}

void ATSimulator::SetCIOPatchEnabled(char c, bool enabled) const {
	mpHLECIOHook->SetCIOPatchEnabled(c, enabled);
}

bool ATSimulator::IsSIOPatchEnabled() const {
	return mpSIOManager->IsSIOPatchEnabled();
}

void ATSimulator::SetSIOPatchEnabled(bool enable) {
	mpSIOManager->SetSIOPatchEnabled(enable);
}


bool ATSimulator::IsPBIPatchEnabled() const {
	return mpDeviceManager->GetDeviceByTag("pbidisk") != nullptr;
}

void ATSimulator::SetPBIPatchEnabled(bool enable) {
	auto *dev = mpDeviceManager->GetDeviceByTag("pbidisk");

	if (enable) {
		if (!dev)
			mpDeviceManager->AddDevice("pbidisk", ATPropertySet(), false, true);
	} else {
		if (dev)
			mpDeviceManager->RemoveDevice(dev);
	}
}

bool ATSimulator::GetDeviceSIOPatchEnabled() const {
	return mpSIOManager->GetOtherSIOAccelEnabled();
}

void ATSimulator::SetDeviceSIOPatchEnabled(bool enabled) const {
	mpSIOManager->SetOtherSIOAccelEnabled(enabled);
}

uint8 ATSimulator::GetBankRegister() const {
	return mPIA.GetPortBOutput();
}

int ATSimulator::GetCartBank(uint32 unit) const {
	return mpCartridge[unit] ? mpCartridge[unit]->GetCartBank() : -1;
}

uint32 ATSimulator::RandomizeRawMemory(uint16 start, uint32 count, uint32 seed) {
	if (count > 0x10000U - start)
		count = 0x10000U - start;

	if (mpHeatMap)
		mpHeatMap->ResetMemoryRange(start, count);

	return ATRandomizeMemory(mMemory + start, count, seed);
}

void ATSimulator::SetProfilingEnabled(bool enabled) {
	if (enabled) {
		if (mpProfiler)
			return;

		mpProfiler = new ATCPUProfiler;
		mpProfiler->Init(&mCPU, mpMemMan, this, &mScheduler, &mSlowScheduler, mpPrivateData);
		mCPU.SetProfiler(mpProfiler);
	} else {
		if (!mpProfiler)
			return;

		mCPU.SetProfiler(NULL);
		delete mpProfiler;
		mpProfiler = NULL;
	}
}

void ATSimulator::SetVerifierEnabled(bool enabled) {
	if (enabled) {
		if (mpVerifier)
			return;

		mpVerifier = new ATCPUVerifier;
		mpVerifier->Init(&mCPU, mpMemMan, this, mpSimEventManager);
		mCPU.SetVerifier(mpVerifier);
	} else {
		if (!mpVerifier)
			return;

		mCPU.SetVerifier(NULL);
		mpVerifier->Shutdown();
		delete mpVerifier;
		mpVerifier = NULL;
	}
}

void ATSimulator::SetHeatMapEnabled(bool enabled) {
	if (enabled) {
		if (mpHeatMap)
			return;

		mpHeatMap = new ATCPUHeatMap;
		mpHeatMap->Init(mpSimEventManager);
		mCPU.SetHeatMap(mpHeatMap);
	} else {
		if (!mpHeatMap)
			return;

		mCPU.SetHeatMap(NULL);
		delete mpHeatMap;
		mpHeatMap = NULL;
	}
}

void ATSimulator::SetFloatingIoBusEnabled(bool enabled) {
	if (mbFloatingIoBus != enabled) {
		mbFloatingIoBus = enabled;

		if (mHardwareMode == kATHardwareMode_800)
			InitMemoryMap();
	}
}

void ATSimulator::SetRandomFillEXEEnabled(bool enabled) {
	mbRandomFillEXEEnabled = enabled;

	if (mpHLEProgramLoader)
		mpHLEProgramLoader->SetRandomizeMemoryOnLoad(enabled);
}

void ATSimulator::SetTurboModeEnabled(bool turbo) {
	mbTurbo = turbo;
	mGTIA.SetFrameSkip(mbTurbo || mbFrameSkip);
}

void ATSimulator::SetFrameSkipEnabled(bool frameskip) {
	mbFrameSkip = frameskip;
	mGTIA.SetFrameSkip(mbTurbo || mbFrameSkip);
}

void ATSimulator::SetVideoStandard(ATVideoStandard vs) {
	if (mVideoStandard == vs)
		return;

	mVideoStandard = vs;
	++mConfigChangeCounter;
	
	const bool is50Hz = vs != kATVideoStandard_NTSC && vs != kATVideoStandard_PAL60;
	mAntic.SetPALMode(is50Hz);

	const bool isSECAM = (vs == kATVideoStandard_SECAM);
	const auto schedulerRate = isSECAM ? kATMasterClockFrac_SECAM : is50Hz ? kATMasterClockFrac_PAL : kATMasterClockFrac_NTSC;
	mScheduler.SetRate(schedulerRate);
	mpUIRenderer->SetCyclesPerSecond(schedulerRate.asDouble());

	mSlowScheduler.SetRate(isSECAM ? kSlowSchedulerRateSECAM : is50Hz ? kSlowSchedulerRatePAL : kSlowSchedulerRateNTSC);

	mGTIA.SetPALMode(vs != kATVideoStandard_NTSC && vs != kATVideoStandard_NTSC50);
	mGTIA.SetSECAMMode(vs == kATVideoStandard_SECAM);
}

void ATSimulator::SetMemoryMode(ATMemoryMode mode) {
	if (mMemoryMode == mode)
		return;

	mMemoryMode = mode;
	++mConfigChangeCounter;

	InitMemoryMap();
}

void ATSimulator::SetKernel(uint64 kernelId) {
	if (kernelId == kATFirmwareId_Kernel_HLE)
		kernelId = kATFirmwareId_Kernel_LLE;

	if (mKernelId == kernelId)
		return;

	mKernelId = kernelId;

	UpdateKernel(false);
	InitMemoryMap();
}

void ATSimulator::SetBasic(uint64 basicId) {
	if (mBasicId == basicId)
		return;

	mBasicId = basicId;

	UpdateKernel(false);
	InitMemoryMap();
}

void ATSimulator::SetHardwareMode(ATHardwareMode mode) {
	if (mHardwareMode == mode)
		return;

	mHardwareMode = mode;

	++mConfigChangeCounter;

	bool is5200 = mode == kATHardwareMode_5200;

	mPokey.Set5200Mode(is5200);

	for(ATVBXEEmulator *vbxe : mpDeviceManager->GetInterfaces<ATVBXEEmulator>(false, false))
		vbxe->Set5200Mode(is5200);

	mpMemMan->SetFloatingDataBus(mode == kATHardwareMode_130XE || mode == kATHardwareMode_800 || mode == kATHardwareMode_5200);

	mpLightPen->SetIgnorePort34(mode == kATHardwareMode_800XL || mode == kATHardwareMode_1200XL || mode == kATHardwareMode_XEGS || mode == kATHardwareMode_130XE);

	mpInputManager->Set5200Mode(is5200);

	UpdateXLCartridgeLine();
	
	UpdateKernel(false);
	InitMemoryMap();
}

bool ATSimulator::IsCPUModeOverridden() const {
	return mpPrivateData->mpCPUModeOverrider != nullptr;
}

bool ATSimulator::IsRapidusEnabled() const {
	return mpPrivateData->mpRapidus != nullptr;
}

void ATSimulator::SetCPUMode(ATCPUMode mode, uint32 subCycles) {
	if (subCycles < 1 || mode != kATCPUMode_65C816)
		subCycles = 1;

	if (subCycles > 16)
		subCycles = 16;

	if (mpPrivateData->mCPUMode == mode && mpPrivateData->mCPUSubCycles == subCycles)
		return;

	mpPrivateData->mCPUMode = mode;
	mpPrivateData->mCPUSubCycles = subCycles;
	++mConfigChangeCounter;

	if (!mpPrivateData->mpRapidus)
		mCPU.SetCPUMode(mode, subCycles);
}

ATCPUMode ATSimulator::GetCPUMode() const {
	return mpPrivateData->mCPUMode;
}

uint32 ATSimulator::GetCPUSubCycles() const {
	return mpPrivateData->mCPUSubCycles;
}

void ATSimulator::SetAxlonMemoryMode(uint8 bits) {
	if (bits > 8)
		bits = 8;

	if (mAxlonMemoryBits != bits) {
		mAxlonMemoryBits = bits;

		uint32 reqSize = (UINT32_C(0x4000) << bits) - 0x4000;
		if (mAxlonMemory.size() != reqSize) {
			mAxlonMemory.resize(reqSize);

			ResetMemoryBuffer(mAxlonMemory.data(), reqSize, 0xBF1ADAEE);
		}

		mpMMU->SetAxlonMemory(bits, mbAxlonAliasingEnabled, mAxlonMemory.data());
	}
}

void ATSimulator::SetAxlonAliasingEnabled(bool enabled) {
	if (mbAxlonAliasingEnabled != enabled) {
		mbAxlonAliasingEnabled = enabled;
		mpMMU->SetAxlonMemory(mAxlonMemoryBits, enabled, mAxlonMemory.data());
	}
}

void ATSimulator::SetHighMemoryBanks(sint32 banks) {
	if (banks < -1)
		banks = -1;
	else if (banks > 255)
		banks = 255;

	if (mHighMemoryBanks == banks)
		return;

	mHighMemoryBanks = banks;

	const uint32 numBanks = banks < 0 ? 0 : (uint32)banks;
	const uint32 highMemSize = numBanks << 16;

	if (mpPrivateData->mHighMemory.size() != highMemSize) {
		mpPrivateData->mHighMemory.resize(highMemSize);

		ResetMemoryBuffer(mpPrivateData->mHighMemory.data(), mpPrivateData->mHighMemory.size(), 0x324CBA17);
	}

	if (!mpPrivateData->mpRapidus) {
		// disable highmem first and enable it after layer changes to reduce thrashing
		if (banks < 0) {
			mpMemMan->SetHighMemoryEnabled(false);
			mpMMU->SetHighMemory(0, nullptr);
		} else {
			mpMMU->SetHighMemory(numBanks, mpPrivateData->mHighMemory.data());
			mpMemMan->SetHighMemoryEnabled(true);
		}
	}
}

void ATSimulator::SetDiskSIOPatchEnabled(bool enable) {
	mpSIOManager->SetDiskSIOAccelEnabled(enable);
}

void ATSimulator::SetDiskSIOOverrideDetectEnabled(bool enable) {
	if (mbDiskSIOOverrideDetectEnabled == enable)
		return;

	mbDiskSIOOverrideDetectEnabled = enable;
}

void ATSimulator::SetDiskAccurateTimingEnabled(bool enable) {
	for(auto *diskIf : mpPrivateData->mpDiskInterfaces)
		diskIf->SetAccurateSectorTimingEnabled(enable);
}

void ATSimulator::SetDiskSectorCounterEnabled(bool enable) {
	mbDiskSectorCounterEnabled = enable;

	for(auto *diskIf : mpPrivateData->mpDiskInterfaces)
		diskIf->SetShowSectorCounter(enable);
}

void ATSimulator::SetCassetteSIOPatchEnabled(bool enable) {
	if (mbCassetteSIOPatchEnabled == enable)
		return;

	mbCassetteSIOPatchEnabled = enable;

	mpSIOManager->ReinitHooks();
	mpHLECIOHook->ReinitHooks(mHookPage);
}

void ATSimulator::SetCassetteAutoBootEnabled(bool enable) {
	mbCassetteAutoBootEnabled = enable;
}

void ATSimulator::SetCassetteAutoBasicBootEnabled(bool enable) {
	mbCassetteAutoBasicBootEnabled = enable;
}

void ATSimulator::SetCassetteAutoRewindEnabled(bool enable) {
	mpCassette->SetAutoRewindEnabled(enable);
}

void ATSimulator::SetCassetteRandomizedStartEnabled(bool enable) {
	mbCassetteRandomizedStartEnabled = enable;
	mpCassette->SetRandomizedStartEnabled(enable);
}

void ATSimulator::SetFPPatchEnabled(bool enable) {
	if (mbFPPatchEnabled == enable)
		return;

	if (enable) {
		if (!mpHLEFPAccelerator)
			mpHLEFPAccelerator = ATCreateHLEFPAccelerator(&mCPU);
	} else {
		if (mpHLEFPAccelerator) {
			ATDestroyHLEFPAccelerator(mpHLEFPAccelerator);
			mpHLEFPAccelerator = NULL;
		}
	}

	mbFPPatchEnabled = enable;
}

void ATSimulator::SetBASICEnabled(bool enable) {
	if (mbBASICEnabled == enable)
		return;

	mbBASICEnabled = enable;
	++mConfigChangeCounter;
}

void ATSimulator::SetROMAutoReloadEnabled(bool enable) {
	mbROMAutoReloadEnabled = enable;
}

void ATSimulator::SetAutoLoadKernelSymbolsEnabled(bool enable) {
	mbAutoLoadKernelSymbols = enable;
}

void ATSimulator::SetDualPokeysEnabled(bool enable) {
	if (mbDualPokeys != enable) {
		mbDualPokeys = enable;

		mPokey.SetSlave(enable ? &mPokey2 : NULL);

		// we need to flash the audio monitors if they're enabled to add or
		// remove the second monitor
		if (IsAudioMonitorEnabled()) {
			SetAudioMonitorEnabled(false);
			SetAudioMonitorEnabled(true);
		}
	}
}

void ATSimulator::SetFastBootEnabled(bool enable) {
	if (mbFastBoot == enable)
		return;

	mbFastBoot = enable;
	mpSIOManager->ReinitHooks();
}

void ATSimulator::SetKeyboardPresent(bool enable) {
	if (mbKeyboardPresent == enable)
		return;

	mbKeyboardPresent = enable;
	UpdateKeyboardPresentLine();
}

void ATSimulator::SetForcedSelfTest(bool enable) {
	if (mbForcedSelfTest == enable)
		return;

	mbForcedSelfTest = enable;
	UpdateForcedSelfTestLine();
}

void ATSimulator::SetCartridgeSwitch(bool enable) {
	if (mbCartridgeSwitch == enable)
		return;

	mbCartridgeSwitch = enable;
	UpdateCartridgeSwitch();
}

void ATSimulator::SetUltimate1MBEnabled(bool enable) {
	if (enable) {
		if (mpUltimate1MB)
			return;

		++mConfigChangeCounter;

		// Force memory mode to 1MB (required)
		mMemoryMode = kATMemoryMode_1088K;

		mpUltimate1MB = new ATUltimate1MBEmulator;
		InitDevice(*mpUltimate1MB);
		mpUltimate1MB->Init(mMemory, mpMMU, mpPBIManager, mpMemMan, mpUIRenderer, &mScheduler, mpCPUHookManager);
		mpUltimate1MB->SetVBXEPageCallback(ATBINDCALLBACK(this, &ATSimulator::UpdateVBXEPage));
		mpUltimate1MB->SetSBPageCallback(ATBINDCALLBACK(this, &ATSimulator::UpdateSoundBoardPage));

		ReloadU1MBFirmware();

		// We need to force a kernel reload as U1MB needs XL/XE kernel mappings forced on -- the
		// currently selected kernel is irrelevant.
		UpdateKernel(false, true);

		InitMemoryMap();
		ReinitHookPage();

		UpdateSoundBoardPage();
		UpdateVBXEPage();
	} else {
		if (!mpUltimate1MB)
			return;

		++mConfigChangeCounter;

		mpPrivateData->OnRemovingDeviceInterface(*static_cast<IATDeviceSystemControl *>(mpUltimate1MB));

		mpUltimate1MB->Shutdown();
		delete mpUltimate1MB;
		mpUltimate1MB = NULL;

		if (mpMemLayerGameROM)
			mpMemMan->SetLayerMemory(mpMemLayerGameROM, mGameROM);

		if (mpMemLayerBASICROM)
			mpMemMan->SetLayerMemory(mpMemLayerBASICROM, mBASICROM);

		UpdateKernelROMPtrs();

		ReinitHookPage();
		UpdateKernel(false, true);
		UpdateSoundBoardPage();
		UpdateVBXEPage();
	}
}

void ATSimulator::SetMapRAMEnabled(bool enable) {
	if (mbMapRAM == enable)
		return;

	mbMapRAM = enable;
	InitMemoryMap();
}

void ATSimulator::SetCheatEngineEnabled(bool enable) {
	if (enable) {
		if (mpCheatEngine)
			return;

		mpCheatEngine = new ATCheatEngine;
		mpCheatEngine->Init(mMemory, 49152);
	} else {
		if (mpCheatEngine) {
			delete mpCheatEngine;
			mpCheatEngine = NULL;
		}
	}
}

void ATSimulator::SetAudioMonitorEnabled(bool enable) {
	if (enable) {
		if (!mpAudioMonitors[0]) {
			mpAudioMonitors[0] = new ATAudioMonitor;
			mpAudioMonitors[0]->Init(&mPokey, mpUIRenderer, false);
		}

		if (mbDualPokeys && !mpAudioMonitors[1]) {
			mpAudioMonitors[1] = new ATAudioMonitor;
			mpAudioMonitors[1]->Init(&mPokey2, mpUIRenderer, true);
		}
	} else {
		vdsafedelete <<= mpAudioMonitors;
	}
}

void ATSimulator::SetVirtualScreenEnabled(bool enable) {
	if (enable) {
		if (!mpVirtualScreenHandler) {
			mpVirtualScreenHandler = ATCreateVirtualScreenHandler();
			ReinitHookPage();
		}
	} else {
		if (mpVirtualScreenHandler) {
			delete mpVirtualScreenHandler;
			mpVirtualScreenHandler = NULL;

			ReinitHookPage();
		}
	}
}

void ATSimulator::SetShadowROMEnabled(bool enabled) {
	if (mbShadowROM == enabled)
		return;

	mbShadowROM = enabled;

	UpdateKernelROMSpeeds();

	if (mpMemLayerBASICROM)			mpMemMan->SetLayerFastBus(mpMemLayerBASICROM, enabled);
	if (mpMemLayerGameROM)			mpMemMan->SetLayerFastBus(mpMemLayerGameROM, enabled);

}

void ATSimulator::SetShadowCartridgeEnabled(bool enabled) {
	if (mbShadowCartridge == enabled)
		return;

	mbShadowCartridge = enabled;

	for(int i=0; i<2; ++i) {
		if (mpCartridge[i])
			mpCartridge[i]->SetFastBus(enabled);
	}
}

void ATSimulator::ClearPendingHeldKey() {
	if (mPendingHeldKey >= 0) {
		mPendingHeldKey = -1;

		mpUIRenderer->SetPendingHeldKey(-1);
	}
}

void ATSimulator::SetPendingHeldKey(uint8 key) {
	if (mPendingHeldKey != key) {
		mPendingHeldKey = key;

		mpUIRenderer->SetPendingHeldKey(key);
	}
}

void ATSimulator::SetPendingHeldSwitches(uint8 switches) {
	switches &= 7;

	if (mPendingHeldSwitches != switches) {
		mPendingHeldSwitches = switches;

		mpUIRenderer->SetPendingHeldButtons(switches);
	}
}

int ATSimulator::GetPowerOnDelay() const {
	return mPowerOnDelay;
}

void ATSimulator::SetPowerOnDelay(int tenthsOfSeconds) {
	mPowerOnDelay = tenthsOfSeconds;
}

ATTraceCollection *ATSimulator::GetTraceCollection() const {
	auto *context = mpPrivateData->mpTraceContext.get();
	return context ? context->mpCollection.get() : nullptr;
}

bool ATSimulator::GetTracingEnabled() const {
	return mpPrivateData->mpTraceContext != nullptr;
}

void ATSimulator::SetTracingEnabled(const ATTraceSettings *settings) {
	ATTraceContext *context = mpPrivateData->mpTraceContext;

	if (settings) {
		if (context)
			return;

		context = new ATTraceContext;
		context->mBaseTime = mScheduler.GetTick64();
		context->mBaseTickScale = mScheduler.GetRate().AsInverseDouble();
		context->mpCollection = new ATTraceCollection;
		mpPrivateData->mpCPUTracer = new ATCPUTracer;
		mpPrivateData->mpCPUTracer->Init(&mCPU, &mScheduler, &mSlowScheduler, mpPrivateData, context, settings->mbTraceCpuInsns, settings->mbTraceBasic);

		mpPrivateData->mpTraceChannelVideo = ATCreateTraceChannelVideo(L"Video", &context->mMemTracker);

		if (settings->mbTraceVideo) {
			mpPrivateData->mpVideoTracer = ATCreateVideoTracer();
			mpPrivateData->mpVideoTracer->Init(mpPrivateData->mpTraceChannelVideo, context->mBaseTime, context->mBaseTickScale, settings->mTraceVideoDivisor);
			mGTIA.AddVideoTap(mpPrivateData->mpVideoTracer->AsVideoTap());

			context->mpCollection->AddGroup(L"Video", kATTraceGroupType_Video)->AddChannel(mpPrivateData->mpTraceChannelVideo->AsTraceChannel());
		}

		mpPrivateData->mTraceSizeLimit = UINT64_MAX;

		if (settings->mbAutoLimitTraceMemory) {
#if VD_PTR_SIZE > 4
		mpPrivateData->mTraceSizeLimit = UINT64_C(4) << 30;		// 4GB
#else
		mpPrivateData->mTraceSizeLimit = UINT64_C(500) << 20;	// 500MB
#endif
		}

		if (mpUIRenderer)
			mpUIRenderer->SetTracingSize(0);
	} else {
		if (!context)
			return;

		if (mpUIRenderer)
			mpUIRenderer->SetTracingSize(-1);

		if (mpPrivateData->mpVideoTracer) {
			mGTIA.RemoveVideoTap(mpPrivateData->mpVideoTracer->AsVideoTap());
			mpPrivateData->mpVideoTracer->Shutdown();
			mpPrivateData->mpVideoTracer = nullptr;
			mpPrivateData->mpTraceChannelVideo = nullptr;
		}

		mpPrivateData->mpCPUTracer->Shutdown();
		mpPrivateData->mpCPUTracer = nullptr;
		context = nullptr;
	}

	mPIA.SetTraceContext(context);
	mAntic.SetTraceContext(context);
	mPokey.SetTraceContext(context);

	for(ATDiskEmulator *disk : mpDiskDrives) {
		if (disk)
			disk->SetTraceContext(context);
	}

	if (mpCassette)
		mpCassette->SetTraceContext(context);

	mpSIOManager->SetTraceContext(context);

	for(IATDevice *dev : mpDeviceManager->GetDevices(false, false))
		dev->SetTraceContext(context);

	// Must be last in case we're destroying the context and collection.
	mpPrivateData->mpTraceContext = context;
}

uint64 ATSimulator::TimeSinceColdReset() const {
	return mScheduler.GetTick64() - mpPrivateData->mColdResetTime;
}

void ATSimulator::ColdReset() {
	mpPrivateData->mColdResetTime = mScheduler.GetTick64();

	int powerOnDelay = mPowerOnDelay;

	if (powerOnDelay < 0) {
		uint32 longestDelay = 0;

		for(IATDevice *dev : mpDeviceManager->GetDevices(false, false)) {
			const uint32 delay = dev->GetComputerPowerOnDelay();

			if (delay > longestDelay)
				longestDelay = delay;
		}

		powerOnDelay = (int)longestDelay;
	}

	mPoweronDelayCounter = (uint32)powerOnDelay * (IsVideo50Hz() ? 5 : 6);
	mbPowered = !mPoweronDelayCounter;

	InternalColdReset(false);
}

void ATSimulator::ColdResetComputerOnly() {
	mPoweronDelayCounter = 0;
	mbPowered = true;

	InternalColdReset(true);
}

void ATSimulator::InternalColdReset(bool computerOnly) {
	ResetAutoHeldButtons();
	SetupPendingHeldButtons();

	if (mpHLEProgramLoader && !mpHLEProgramLoader->IsLaunchPending()) {
		mpHLEProgramLoader->Shutdown();
		delete mpHLEProgramLoader;
		mpHLEProgramLoader = NULL;
	}

	if (mpHLEBasicLoader && !mpHLEBasicLoader->IsLaunchPending()) {
		mpHLEBasicLoader->Shutdown();
		delete mpHLEBasicLoader;
		mpHLEBasicLoader = NULL;
	}

	// check if we need a hook page
	ReinitHookPage();

	if (mbROMAutoReloadEnabled) {
		LoadROMs();
	}

	mpPrivateData->mIRQController.ColdReset();

	// PIA forces all registers to 0 on reset.
	mPIA.ColdReset();

	mCPU.ColdReset();
	mAntic.ColdReset();
	mGTIA.ColdReset();
	mPokey.ColdReset();

	mpSIOManager->ColdReset();

	mpCassette->ColdReset();

	if (!computerOnly) {
		for(int i=0; i<15; ++i) {
			mpDiskDrives[i]->Reset();
		}
	}

	if (mpVirtualScreenHandler)
		mpVirtualScreenHandler->ColdReset();

	mpPBIManager->ColdReset();

	// Nuke memory with a deterministic pattern. We used to clear it, but that's not realistic.
	bool clearExtRAM = !mbPreserveExtRAM;

	if (!mpPrivateData->mbExtRAMClearedOnce) {
		// PreserveExtRAM is set, but we haven't ever cleared extended memory. Do it
		// at least once at startup.
		mpPrivateData->mbExtRAMClearedOnce = true;

		clearExtRAM = true;
	}

	ResetMemoryBuffer(mMemory, clearExtRAM ? sizeof mMemory : 0x10000, 0xA702819E);
	ResetMemoryBuffer(mpPrivateData->mHighMemory.data(), mpPrivateData->mHighMemory.size(), 0x324CBA17);
	ResetMemoryBuffer(mAxlonMemory.data(), mAxlonMemory.size(), 0xBF1ADAEE);

	if (mpHeatMap) {
		mpHeatMap->ResetMemoryRange(0, 0x10000);
		mpHeatMap->MarkMemoryRangeHardware(0xD000, 0x0800);
		mpHeatMap->PresetMemoryRange(0xC000, 0x1000);
		mpHeatMap->PresetMemoryRange(0xD800, 0x2800);
		mpHeatMap->SetEarlyState(true);
	}

	// The VBXE has static RAM, so we always clear it to noise in non-zero modes. This needs to
	// be done after the main memory clear as VBXE's memory type has priority in shared memory
	// modes (extended memory is coming from VBXE).
	for(ATVBXEEmulator *devvbxe : mpDeviceManager->GetInterfaces<ATVBXEEmulator>(false, false)) {
		void *vbxeMem = devvbxe->GetMemoryBase();
		if (mMemoryClearMode != kATMemoryClearMode_Zero)
			ATRandomizeMemory((uint8 *)vbxeMem, 0x80000, 0x9B274CA3);
		else
			memset(vbxeMem, 0, 0x80000);
	}

	for(int i=0; i<2; ++i) {
		if (mpCartridge[i])
			mpCartridge[i]->ColdReset();
	}

	if (computerOnly) {
		for(IATDevice *dev : mpDeviceManager->GetDevices(false, false))
			dev->ComputerColdReset();
	} else {
		for(IATDevice *dev : mpDeviceManager->GetDevices(false, false)) {
			dev->ColdReset();
			dev->PeripheralColdReset();
		}
	}

	InitMemoryMap();

	mpMMU->SetAxlonBank(0);

	if (mpUltimate1MB)
		mpUltimate1MB->ColdReset();

	UpdateXLCartridgeLine();
	UpdateKeyboardPresentLine();
	UpdateForcedSelfTestLine();

	mpUIRenderer->SetLedStatus(0);

	if (mHardwareMode == kATHardwareMode_800 || mHardwareMode == kATHardwareMode_5200)
		mGTIA.SetControllerTrigger(3, false);

	if (mHardwareMode != kATHardwareMode_XEGS)
		mGTIA.SetControllerTrigger(2, false);

	if (mHardwareMode != kATHardwareMode_800XL && mHardwareMode != kATHardwareMode_1200XL && mHardwareMode != kATHardwareMode_XEGS && mHardwareMode != kATHardwareMode_130XE) {
		mPokey.SetPotPos(4, 228);
	} else {
		// POT 4 is the forced self-test line.
		mPokey.SetPotPos(5, 228);
		mPokey.SetPotPos(6, 228);
		mPokey.SetPotPos(7, 228);

		// We need to force this in case the MMU was previously in 800 mode. The PIA may be
		// out of sync with the MMU and won't send an update until a change actually occurs.
		// It's reset to input mode, so the MMU sees all lines pulled up.
		mpMMU->SetBankRegister(0xFF);
	}

	if (mpHLEFastBootHook) {
		ATDestroyHLEFastBootHook(mpHLEFastBootHook);
		mpHLEFastBootHook = NULL;
	}

	if (mbFastBoot)
		mpHLEFastBootHook = ATCreateHLEFastBootHook(&mCPU);

	SetupAutoHeldButtonHook();

	// notify CPU hooks that we're reinitializing the OS
	mpCPUHookManager->CallResetHooks();

	// Check if we need to toggle BASIC for cassette boot (must be before we call the init hooks,
	// which set up auto-hold-START).
	InitCassetteAutoBasicBoot();

	// If Ultimate1MB is enabled, we must suppress all OS hooks until the BIOS has locked config.
	if (mpUltimate1MB) {
		mpCPUHookManager->EnableOSHooks(false);
	} else {
		mpCPUHookManager->EnableOSHooks(true);
		mpCPUHookManager->CallInitHooks(mpKernelLowerROM, mpKernelUpperROM);
	}

	NotifyEvent(kATSimEvent_ColdReset);
}

void ATSimulator::WarmReset() {
	InternalWarmReset(false);
}

void ATSimulator::InternalWarmReset(bool enableHeldKeys) {
	if (!mbPowered)
		return;

	if (mpHLEProgramLoader) {
		mpHLEProgramLoader->Shutdown();
		delete mpHLEProgramLoader;
		mpHLEProgramLoader = NULL;
	}

	if (mpHLEBasicLoader) {
		mpHLEBasicLoader->Shutdown();
		delete mpHLEBasicLoader;
		mpHLEBasicLoader = NULL;
	}

	// The Atari 400/800 route the reset button to /RNMI on ANTIC, which then fires an
	// NMI with the system reset (bit 5) set in NMIST. On the XL series, /RNMI is permanently
	// tied to +5V by pullup and the reset button directly resets the 6502, ANTIC, FREDDIE,
	// and the PIA.
	if (mHardwareMode == kATHardwareMode_800XL || mHardwareMode == kATHardwareMode_1200XL || mHardwareMode == kATHardwareMode_XEGS || mHardwareMode == kATHardwareMode_130XE) {
		mPIA.WarmReset();

		mpMMU->SetBankRegister(0xFF);

		mCPU.WarmReset();
		mAntic.WarmReset();
	} else {
		mAntic.RequestNMI();
	}

	mpMMU->SetAxlonBank(0);

	if (mpVirtualScreenHandler)
		mpVirtualScreenHandler->WarmReset();

	if (mpUltimate1MB)
		mpUltimate1MB->WarmReset();

	if (mpHeatMap)
		mpHeatMap->SetEarlyState(true);

	mpPBIManager->WarmReset();

	mpSIOManager->WarmReset();

	if (mpCPUHookInitHook) {
		mpCPUHookManager->RemoveInitHook(mpCPUHookInitHook);
		mpCPUHookInitHook = nullptr;
	}

	if (enableHeldKeys) {
		ResetAutoHeldButtons();
		SetupAutoHeldButtonHook();
	}

	// notify CPU hooks that we're reinitializing the OS
	mpCPUHookManager->CallResetHooks();

	// If Ultimate1MB is enabled, we must suppress all OS hooks until the BIOS has locked config.
	if (mpUltimate1MB) {
		mpCPUHookManager->EnableOSHooks(false);
	} else {
		mpCPUHookManager->EnableOSHooks(true);
		mpCPUHookManager->CallInitHooks(mpKernelLowerROM, mpKernelUpperROM);
	}

	for(IATDevice *dev : mpDeviceManager->GetDevices(false, false))
		dev->WarmReset();

	NotifyEvent(kATSimEvent_WarmReset);

	SetupPendingHeldButtons();
}

void ATSimulator::Resume() {
	mbRunSingleCycle = false;

	if (mbRunning)
		return;

	mbRunning = true;
	mbPaused = false;

	if (mpUIRenderer)
		mpUIRenderer->SetPaused(false);

	if (mPendingEvent == kATSimEvent_AnonymousInterrupt)
		mPendingEvent = kATSimEvent_None;
}

void ATSimulator::ResumeSingleCycle() {
	mbRunSingleCycle = true;

	if (mbRunning)
		return;

	mbRunning = true;
	mbPaused = false;

	if (mpUIRenderer)
		mpUIRenderer->SetPaused(false);

	if (mPendingEvent == kATSimEvent_AnonymousInterrupt)
		mPendingEvent = kATSimEvent_None;
}

void ATSimulator::Pause() {
	mbRunning = false;
	mbPaused = true;

	if (mpUIRenderer)
		mpUIRenderer->SetPaused(true);
}

void ATSimulator::Suspend() {
	if (mbRunning) {
		mbRunning = false;

		if (!mPendingEvent)
			mPendingEvent = kATSimEvent_AnonymousInterrupt;
	}
}

void ATSimulator::FlushDeferredEvents() {
	mpPrivateData->mDeferredEventManager.FlushPendingEvents();
}

void ATSimulator::GetDirtyStorage(vdfastvector<ATStorageId>& ids, uint32 storageTypeMask) const {
	if (storageTypeMask & kATStorageTypeMask_Disk) {
		for(int i=0; i<vdcountof(mpDiskDrives); ++i) {
			ATStorageId id = (ATStorageId)(kATStorageId_Disk + i);
			if (IsStorageDirty(id))
				ids.push_back(id);
		}
	}

	static const ATStorageId kIds[]={
		kATStorageId_Tape,
		kATStorageId_Cartridge,
		(ATStorageId)(kATStorageId_Cartridge + 1),
		kATStorageId_Firmware,
		(ATStorageId)(kATStorageId_Firmware+1),
		(ATStorageId)(kATStorageId_Firmware+2),
		(ATStorageId)(kATStorageId_Firmware+3),
		(ATStorageId)(kATStorageId_Firmware+4),
	};

	for(const ATStorageId id : kIds) {
		const uint32 type = (id & kATStorageId_TypeMask) >> kATStorageIdTypeShift;

		if (!(storageTypeMask & (1 << type)))
			continue;

		if (IsStorageDirty(id))
			ids.push_back(id);
	}
}

bool ATSimulator::IsStorageDirty(ATStorageId mediaId) const {
	if (mediaId == kATStorageId_All) {
		if (IsStorageDirty(kATStorageId_Cartridge))
			return true;

		if (IsStorageDirty(kATStorageId_Tape))
			return true;

		for(int i=0; i<vdcountof(mpDiskDrives); ++i) {
			ATStorageId id = (ATStorageId)(kATStorageId_Disk + i);

			if (IsStorageDirty(id))
				return true;
		}

		for(int i=0; i<5; ++i) {
			if (IsStorageDirty((ATStorageId)(kATStorageId_Firmware + i)))
				return true;
		}

		return false;
	}

	const uint32 type = mediaId & kATStorageId_TypeMask;
	const uint32 unit = mediaId & kATStorageId_UnitMask;
	switch(type) {
		case kATStorageId_Cartridge:
			if (unit < 2 && mpCartridge[unit])
				return mpCartridge[unit]->IsDirty();
			break;

		case kATStorageId_Disk:
			if (unit < vdcountof(mpPrivateData->mpDiskInterfaces))
				return mpPrivateData->mpDiskInterfaces[unit]->IsDirty();
			break;
			
		case kATStorageId_Tape:
			if (unit == 0)
				return mpCassette && mpCassette->IsImageDirty();

			break;

		case kATStorageId_Firmware:
			if (unit == 2)
				return mpUltimate1MB && mpUltimate1MB->IsFirmwareDirty();

			{
				bool found = false;

				for(IATDeviceFirmware *fw : mpDeviceManager->GetInterfaces<IATDeviceFirmware>(false, false)) {
					if (fw->IsWritableFirmwareDirty(unit))
						found = true;
				}

				return found;
			}
			break;
	}

	return false;
}

bool ATSimulator::IsStoragePresent(ATStorageId mediaId) const {
	const uint32 type = mediaId & kATStorageId_TypeMask;
	const uint32 unit = mediaId & kATStorageId_UnitMask;
	switch(type) {
		case kATStorageId_Cartridge:
			if (unit < 2 && mpCartridge[unit])
				return true;
			break;

		case kATStorageId_Disk:
			if (unit < vdcountof(mpDiskDrives))
				return mpDiskDrives[unit]->IsEnabled();
			break;

		case kATStorageId_Tape:
			if (unit == 0)
				return mpCassette != 0 && mpCassette->GetImage();
			break;

		case kATStorageId_Firmware:
			if (unit == 2)
				return mpUltimate1MB != NULL;

			{
				bool found = false;

				for(IATDeviceFirmware *fw : mpDeviceManager->GetInterfaces<IATDeviceFirmware>(false, false)) {
					if (fw->GetWritableFirmwareDesc(unit))
						found = true;
				}

				return found;
			}
			break;
	}

	return false;
}

void ATSimulator::SaveStorage(ATStorageId storageId, const wchar_t *path) {
	const uint32 unit = storageId & kATStorageId_UnitMask;

	if (unit == 2)
		mpUltimate1MB->SaveFirmware(path);
	else {
		IATDeviceFirmware *foundfw = nullptr;

		for(IATDeviceFirmware *fw : mpDeviceManager->GetInterfaces<IATDeviceFirmware>(false, false)) {
			if (fw->GetWritableFirmwareDesc(unit)) {
				foundfw = fw;
				break;
			}
		}

		if (foundfw) {
			VDFileStream f;
			f.open(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);

			foundfw->SaveWritableFirmware(unit, f);
		}
	}
}

void ATSimulator::SwapDrives(int src, int dst) {
	if ((unsigned)src < 15 && (unsigned)dst < 15 && src != dst) {
		auto& if1 = mpPrivateData->mpDiskInterfaces[src];
		auto& if2 = mpPrivateData->mpDiskInterfaces[dst];

		if1->SwapSettings(*if2);

		mpDiskDrives[src]->SetEnabled(if1->GetClientCount() == 1 && if1->IsDiskLoaded());
		mpDiskDrives[dst]->SetEnabled(if2->GetClientCount() == 1 && if2->IsDiskLoaded());
	}
}

void ATSimulator::RotateDrives(int count, int delta) {
	if (count <= 1)
		return;

	delta %= count;
	if (!delta)
		return;

	if (delta < 0)
		delta += count;

	// This algorithm for rotate() appears in a few places, including cplusplus.com, cppreference.com,
	// and StackOverflow. It essentially bubbles up the first block in [first, middle) until it reaches
	// the end, where there may be a partial block. At that point, it swaps the partial block with the
	// first part of the full block, and then resets itself to begin shifting the two halves of the
	// full block in a tail recursive fashion. Kind of neat.
	uint32 first = 0;
	uint32 middle = delta;
	uint32 last = count;
	uint32 next = middle;

	while(first != next) {
		mpPrivateData->mpDiskInterfaces[first++]->SwapSettings(*mpPrivateData->mpDiskInterfaces[next++]);

		if (next == last)
			next = middle;
		else if (first == middle)
			middle = next;
	}
}

void ATSimulator::UnloadAll(uint32 storageTypeMask) {
	if (storageTypeMask & kATStorageTypeMask_Cartridge) {
		for(int i=0; i<2; ++i)
			UnloadCartridge(i);

		if (mHardwareMode == kATHardwareMode_5200)
			LoadCartridge5200Default();
	}

	if (storageTypeMask & kATStorageTypeMask_Tape)
		mpCassette->Unload();

	if (storageTypeMask & kATStorageTypeMask_Disk) {
		for(auto *diskIf : mpPrivateData->mpDiskInterfaces)
			diskIf->UnloadDisk();

		for(auto *disk : mpDiskDrives)
			disk->SetEnabled(false);
	}
}

bool ATSimulator::Load(const wchar_t *path, ATMediaWriteMode writeMode, ATImageLoadContext *loadCtx) {
	ATMediaLoadContext mediaCtx {};

	mediaCtx.mOriginalPath = path;
	mediaCtx.mWriteMode = writeMode;
	mediaCtx.mpImageLoadContext = loadCtx;

	return Load(mediaCtx);
}

bool ATSimulator::Load(const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream, ATMediaWriteMode writeMode, ATImageLoadContext *loadCtx) {
	ATMediaLoadContext mediaCtx {};

	if (origPath)
		mediaCtx.mOriginalPath = origPath;

	if (imagePath)
		mediaCtx.mImageName = imagePath;

	mediaCtx.mWriteMode = writeMode;
	mediaCtx.mpStream = &stream;
	mediaCtx.mpImageLoadContext = loadCtx;

	return Load(mediaCtx);
}

bool ATSimulator::Load(const wchar_t *origPath, const wchar_t *imagePath, IATImage *image, ATMediaWriteMode writeMode, ATImageLoadContext *loadCtx) {
	ATMediaLoadContext mediaCtx {};

	if (origPath)
		mediaCtx.mOriginalPath = origPath;

	if (imagePath)
		mediaCtx.mImageName = imagePath;

	mediaCtx.mWriteMode = writeMode;
	mediaCtx.mpImage = image;
	mediaCtx.mpImageLoadContext = loadCtx;

	return Load(mediaCtx);
}

bool ATSimulator::Load(ATMediaLoadContext& ctx) {
	ATImageLoadContext *const loadCtx = ctx.mpImageLoadContext;
	const wchar_t *origPath = ctx.mOriginalPath.empty() ? nullptr : ctx.mOriginalPath.c_str();
	const wchar_t *imagePath = ctx.mImageName.empty() ? nullptr : ctx.mImageName.c_str();

	ctx.mbModeIncompatible = false;
	ctx.mbMemoryConflictBasic = false;
	ctx.mbMode5200Required = false;
	ctx.mbModeComputerRequired = false;

	if (!ctx.mpImage) {
		VDStringW resultPath;
		bool canUpdate = false;

		if (!ctx.mpStream) {
			if (!origPath)
				throw MyError("Cannot load image: no name specified.");

			VDStringW basePath;
			VDStringW subPath;
			if (ATParseVFSPath(origPath, basePath, subPath) == kATVFSProtocol_File) {
				VDFileStream stream(origPath, nsVDFile::kRead | nsVDFile::kDenyWrite | nsVDFile::kOpenExisting | nsVDFile::kSequential);

				if (stream.getAttributes() & kVDFileAttr_ReadOnly)
					ctx.mWriteMode = (ATMediaWriteMode)(ctx.mWriteMode & ~kATMediaWriteMode_AutoFlush);

				if (!ATImageLoadAuto(origPath, origPath, stream, loadCtx, &resultPath, &canUpdate, ~ctx.mpImage))
					return false;

				ctx.mImageName = resultPath.empty() ? origPath : resultPath.c_str();
				imagePath = ctx.mImageName.c_str();
			} else {
				vdrefptr<ATVFSFileView> view;
		
				ATVFSOpenFileView(origPath, false, ~view);

				ctx.mWriteMode = (ATMediaWriteMode)(ctx.mWriteMode & ~kATMediaWriteMode_AutoFlush);

				if (!ATImageLoadAuto(origPath, view->GetFileName(), view->GetStream(), loadCtx, &resultPath, &canUpdate, ~ctx.mpImage))
					return false;

				ctx.mImageName = view->GetFileName();
				imagePath = ctx.mImageName.c_str();
			}

			if (!resultPath.empty()) {
				ctx.mOriginalPath = resultPath;
				origPath = ctx.mOriginalPath.c_str();
			}
		} else {
			if (!ATImageLoadAuto(origPath, imagePath, *ctx.mpStream, loadCtx, &resultPath, &canUpdate, ~ctx.mpImage))
				return false;
		}

		if (!canUpdate)
			ctx.mWriteMode = (ATMediaWriteMode)(ctx.mWriteMode & ~kATMediaWriteMode_AutoFlush);
	}

	if (ctx.mbStopOnModeIncompatibility || ctx.mbStopAfterImageLoaded) {
		switch(ctx.mpImage->GetImageType()) {
			case kATImageType_Disk:
			case kATImageType_Tape:
			case kATImageType_Program:
			case kATImageType_BasicProgram:
				if (mHardwareMode == kATHardwareMode_5200 || ctx.mbStopAfterImageLoaded) {
					ctx.mbModeIncompatible = true;
					ctx.mbModeComputerRequired = true;
					return false;
				}
				break;

			case kATImageType_Cartridge:
				if (ATIsCartridge5200Mode(vdpoly_cast<IATCartridgeImage *>(ctx.mpImage)->GetMode())) {
					if (mHardwareMode != kATHardwareMode_5200 || ctx.mbStopAfterImageLoaded) {
						ctx.mbModeIncompatible = true;
						ctx.mbMode5200Required = true;
						return false;
					}
				} else {
					if (mHardwareMode == kATHardwareMode_5200 || ctx.mbStopAfterImageLoaded) {
						ctx.mbModeIncompatible = true;
						ctx.mbModeComputerRequired = true;
						return false;
					}
				}
				break;

			case kATImageType_SaveState:
			case kATImageType_Zip:
			case kATImageType_GZip:
			case kATImageType_SAP:
				break;
		}
	}

	if (ctx.mbStopAfterImageLoaded) {
		ctx.mbModeIncompatible = true;
		return false;
	}

	IATImage *const image = ctx.mpImage;
	ATMediaWriteMode writeMode = ctx.mWriteMode;

	int loadIndex = 0;
	if (loadCtx && loadCtx->mLoadIndex >= 0)
		loadIndex = loadCtx->mLoadIndex;

	const ATImageType loadType = image->GetImageType();

	if (loadType == kATImageType_Program) {
		IATBlobImage *programImage = vdpoly_cast<IATBlobImage *>(image);
		const bool internalBasicEnabled = mbBASICEnabled && SupportsInternalBasic();
		if (ctx.mbStopOnMemoryConflictBasic && internalBasicEnabled) {
			vdfastvector<ATProgramBlock> blocks;
			ATParseProgramImage(programImage->GetBuffer(), programImage->GetSize(), blocks);

			if (internalBasicEnabled && ATCheckProgramOverlap(0xA000, 0x2000, blocks)) {
				ctx.mbMemoryConflictBasic = true;
				return false;
			}
		}

		LoadProgram(origPath, programImage, false);
		return true;
	} else if (loadType == kATImageType_BasicProgram) {
		LoadProgram(origPath, vdpoly_cast<IATBlobImage *>(image), true);
		return true;
	} else if (loadType == kATImageType_Cartridge) {
		if (loadIndex >= 2)
			throw MyError("Invalid cartridge unit %u.\n", loadIndex);

		LoadCartridge(loadIndex, origPath, vdpoly_cast<IATCartridgeImage *>(image));
		return true;
	} else if (loadType == kATImageType_Tape) {
		mpCassette->Load(vdpoly_cast<IATCassetteImage *>(image), origPath ? origPath : imagePath, origPath != nullptr);
		mpCassette->Play();
		return true;
	} else if (loadType == kATImageType_Disk) {
		if (loadIndex >= 15)
			throw MyError("Invalid disk drive D%d:.\n", loadIndex + 1);

		ATDiskInterface& diskIf = *mpPrivateData->mpDiskInterfaces[loadIndex];
		IATDiskImage *diskImage = vdpoly_cast<IATDiskImage *>(image);
		diskIf.LoadDisk(origPath, imagePath, diskImage);

		if (!diskImage->IsUpdatable())
			writeMode = (ATMediaWriteMode)(writeMode & ~kATMediaWriteMode_AutoFlush);

		diskIf.SetWriteMode(writeMode);

		if (diskIf.GetClientCount() < 2)
			mpDiskDrives[loadIndex]->SetEnabled(true);

		return true;
	} else if (loadType == kATImageType_SaveState) {
		IATBlobImage *saveStateImage = vdpoly_cast<IATBlobImage *>(image);

		ATSaveStateReader reader((const uint8 *)saveStateImage->GetBuffer(), saveStateImage->GetSize());

		return LoadState(reader, loadCtx ? loadCtx->mpStateLoadContext : NULL);
	} else if (loadType == kATImageType_SAP) {
		IATBlobImage *blobImage = vdpoly_cast<IATBlobImage *>(image);

		vdfastvector<uint8> exe;
		ATConvertSAPToPlayer(blobImage->GetBuffer(), blobImage->GetSize(), exe);

		vdrefptr<IATBlobImage> exeBlobImage;
		ATCreateBlobImage(kATImageType_Program, exe.data(), (uint32)exe.size(), ~exeBlobImage);

		LoadProgram(origPath, exeBlobImage, false);
		return true;
	} else {
		throw MyError("Unsupported image type: %u.", loadType);
	}
}

void ATSimulator::LoadProgram(const wchar_t *path, IATBlobImage *image, bool basic) {
	if (mpHLEProgramLoader) {
		mpHLEProgramLoader->Shutdown();
		delete mpHLEProgramLoader;
		mpHLEProgramLoader = NULL;
	}

	if (mpHLEBasicLoader) {
		mpHLEBasicLoader->Shutdown();
		delete mpHLEBasicLoader;
		mpHLEBasicLoader = NULL;
	}

	if (basic) {
		vdautoptr<ATHLEBasicLoader> loader(new ATHLEBasicLoader);

		loader->Init(&mCPU, mpSimEventManager, this);
		loader->LoadProgram(image);

		mpHLEBasicLoader = loader.release();

		SetBASICEnabled(true);

		if (!SupportsInternalBasic())
			LoadCartridgeBASIC();
	} else {
		vdautoptr<ATHLEProgramLoader> loader(new ATHLEProgramLoader);

		loader->Init(&mCPU, mpSimEventManager, this, mpSIOManager);
		loader->LoadProgram(path, image, mProgramLoadMode);

		mpHLEProgramLoader = loader.release();
		mpHLEProgramLoader->SetRandomizeMemoryOnLoad(mbRandomFillEXEEnabled);
	}

	// cold restart the system and wait for DSKINV hook to fire
	ColdReset();
}

bool ATSimulator::IsCartridgeAttached(uint32 unit) const {
	return mpCartridge[unit] != NULL;
}

void ATSimulator::UnloadCartridge(uint32 index) {

	if (index == 0) {
		IATDebugger *d = ATGetDebugger();

		if (d) {
			for(int i=0; i<3; ++i) {
				if (!mCartModuleIds[i])
					continue;

				d->UnloadSymbols(mCartModuleIds[i]);	
				mCartModuleIds[i] = 0;
			}
		}
	}

	if (mpCartridge[index]) {
		mpCartridge[index]->Unload();
		delete mpCartridge[index];
		mpCartridge[index] = NULL;
	}

	UpdateXLCartridgeLine();
}

bool ATSimulator::LoadCartridge(uint32 unit, const wchar_t *path, ATCartLoadContext *loadCtx) {
	ATImageLoadContext ctx;

	if (loadCtx)
		ctx.mpCartLoadContext = loadCtx;

	ctx.mLoadType = kATImageType_Cartridge;
	ctx.mLoadIndex = unit;

	return Load(path, kATMediaWriteMode_RO, &ctx);
}

bool ATSimulator::LoadCartridge(uint32 unit, const wchar_t *origPath, const wchar_t *imagePath, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx) {
	vdrefptr<IATCartridgeImage> cartImage;
	if (!ATLoadCartridgeImage(origPath, stream, loadCtx, ~cartImage))
		return false;

	LoadCartridge(unit, origPath, cartImage);
	return true;
}

void ATSimulator::LoadCartridge(uint32 unit, const wchar_t *origPath, IATCartridgeImage *image) {
	UnloadCartridge(unit);

	IATDebugger *d = ATGetDebugger();

	// We have to use the mpCartridge variable because the cartridge code will
	// set up memory mappings during init that will eventually trigger
	// UpdateXLCartridgeLine().
	ATCartridgeEmulator *cart = new ATCartridgeEmulator;
	mpCartridge[unit] = cart;
	try {
		cart->InitCartridge(&mpPrivateData->mCartPort);
		cart->Init(mpMemMan, &mScheduler, unit ? kATMemoryPri_Cartridge2 : kATMemoryPri_Cartridge1, unit ? kATCartridgePriority_PassThrough : kATCartridgePriority_Default, mbShadowCartridge);

		cart->Load(image);
	} catch(const MyError&) {
		ATCartridgeEmulator *tmp = cart;
		mpCartridge[unit] = NULL;
		delete tmp;

		if (!unit && mHardwareMode == kATHardwareMode_5200)
			LoadCartridge5200Default();

		throw;
	}

	mpCartridge[unit]->SetUIRenderer(mpUIRenderer);

	UpdateXLCartridgeLine();

	if (d && d->IsSymbolLoadingEnabled() && origPath) {
		static const wchar_t *const kSymExts[]={
			// Need to load .lab before .lst in case there are symbol-relative
			// ASSERTs in the listing file.
			L".lab", L".lst", L".lbl"
		};

		VDASSERTCT(vdcountof(kSymExts) == vdcountof(mCartModuleIds));

		const VDStringSpanW symbolHintPath = VDFileSplitExtLeftSpan(VDStringSpanW(origPath));

		VDStringW sympath;
		for(size_t i=0; i<vdcountof(mCartModuleIds); ++i) {
			sympath = symbolHintPath;
			sympath += kSymExts[i];

			try {
				uint32 moduleId = d->LoadSymbols(sympath.c_str(), false);

				if (moduleId) {
					mCartModuleIds[i] = moduleId;

					ATConsolePrintf("Loaded symbols %ls\n", sympath.c_str());
				}
			} catch(const MyError&) {
				// ignore
			}
		}

		// process directives AFTER all symbols have been loaded
		for(size_t i=0; i<vdcountof(mCartModuleIds); ++i) {
			if (mCartModuleIds[i])
				d->ProcessSymbolDirectives(mCartModuleIds[i]);
		}

		// load debugger script
		sympath = symbolHintPath;
		sympath += L".atdbg";

		d->QueueAutoLoadBatchFile(sympath.c_str());
	}
}

void ATSimulator::LoadCartridge5200Default() {
	UnloadCartridge(0);

	mpCartridge[0] = new ATCartridgeEmulator;
	mpCartridge[0]->InitCartridge(&mpPrivateData->mCartPort);
	mpCartridge[0]->Init(mpMemMan, &mScheduler, kATMemoryPri_Cartridge1, kATCartridgePriority_Default, mbShadowCartridge);
	mpCartridge[0]->SetUIRenderer(mpUIRenderer);
	mpCartridge[0]->Load5200Default();
}

void ATSimulator::LoadNewCartridge(int mode) {
	UnloadCartridge(0);

	mpCartridge[0] = new ATCartridgeEmulator;
	mpCartridge[0]->InitCartridge(&mpPrivateData->mCartPort);
	mpCartridge[0]->Init(mpMemMan, &mScheduler, kATMemoryPri_Cartridge1, kATCartridgePriority_Default, mbShadowCartridge);
	mpCartridge[0]->SetUIRenderer(mpUIRenderer);
	mpCartridge[0]->LoadNewCartridge((ATCartridgeMode)mode);
}

void ATSimulator::LoadCartridgeBASIC() {
	UnloadCartridge(0);

	ATCartridgeEmulator *cart = new ATCartridgeEmulator;
	mpCartridge[0] = cart;
	cart->InitCartridge(&mpPrivateData->mCartPort);
	cart->Init(mpMemMan, &mScheduler, kATMemoryPri_Cartridge1, kATCartridgePriority_Default, mbShadowCartridge);
	cart->SetUIRenderer(mpUIRenderer);

	ATCartLoadContext ctx = {};
	ctx.mCartMapper = kATCartridgeMode_8K;

	VDMemoryStream memstream(mBASICROM, sizeof mBASICROM);
	cart->Load(L"special:basic", memstream, &ctx);
}

ATSimulator::AdvanceResult ATSimulator::AdvanceUntilInstructionBoundary() {
	for(;;) {
		if (!mCPU.IsInstructionInProgress())
			return kAdvanceResult_Running;

		if (!mbRunning)
			return kAdvanceResult_Stopped;

		ATSimulatorEvent cpuEvent = kATSimEvent_None;

		if (mCPU.GetUnusedCycle())
			cpuEvent = (ATSimulatorEvent)mCPU.Advance();
		else {
			int x = mAntic.GetBeamX();

			if (!x && mAntic.GetBeamY() == 0) {
				mGTIA.BeginFrame(true, false);
			}

			ATSCHEDULER_ADVANCE(&mScheduler);
			uint8 fetchMode = mAntic.PreAdvance();

			if (!((fetchMode | mAntic.GetWSYNCFlag()) & 1))
				cpuEvent = (ATSimulatorEvent)mCPU.Advance();

			mAntic.PostAdvance(fetchMode);

			if (!(cpuEvent | mPendingEvent))
				continue;
		}

		ATSimulatorEvent ev = mPendingEvent;
		mPendingEvent = kATSimEvent_None;
		mbRunning = false;

		if (cpuEvent)
			NotifyEvent(cpuEvent);
		if (ev)
			NotifyEvent(ev);

		if (!mbRunning) {
			mGTIA.UpdateScreen(true, false);
			return kAdvanceResult_Stopped;
		}
	}
}

ATSimulator::AdvanceResult ATSimulator::Advance(bool dropFrame) {
	if (!mbRunning)
		return kAdvanceResult_Stopped;
	
	ATSimulatorEvent cpuEvent = kATSimEvent_None;

	if (!mbPowered) {
		int scansLeft = (mVideoStandard == kATVideoStandard_NTSC || mVideoStandard == kATVideoStandard_PAL60 ? 262 : 312) - mAntic.GetBeamY();
		if (scansLeft > 8)
			scansLeft = 8;

		if (mbRunSingleCycle)
			PostInterruptingEvent(kATSimEvent_AnonymousInterrupt);

		for(int scans = 0; scans < scansLeft; ++scans) {
			int x = mAntic.GetBeamX();

			if (!x && mAntic.GetBeamY() == 0) {
				if (!mGTIA.BeginFrame(false, dropFrame))
					return kAdvanceResult_WaitingForFrame;
			}

			int cycles = 114 - x;
			if (cycles > 0) {
				while(cycles--) {
					ATSCHEDULER_ADVANCE(&mScheduler);

					mAntic.PostAdvance(mAntic.PreAdvance());

					if (mPendingEvent)
						goto handle_event;
				}
			}
		}

		return mbRunning ? kAdvanceResult_Running : kAdvanceResult_Stopped;
	}

	if (mCPU.GetUnusedCycle()) {
		cpuEvent = (ATSimulatorEvent)mCPU.Advance();
	}

	if (!cpuEvent) {
		int scansLeft = (mVideoStandard == kATVideoStandard_NTSC || mVideoStandard == kATVideoStandard_PAL60 ? 262 : 312) - mAntic.GetBeamY();
		if (scansLeft > 8)
			scansLeft = 8;
		else if (scansLeft < 0)
			scansLeft = 1;

		if (mbRunSingleCycle)
			PostInterruptingEvent(kATSimEvent_AnonymousInterrupt);

		for(int scans = 0; scans < scansLeft; ++scans) {
			int x = mAntic.GetBeamX();

			if (!x && mAntic.GetBeamY() == 0) {
				if (!mGTIA.BeginFrame(false, dropFrame))
					return kAdvanceResult_WaitingForFrame;
			}

			int cycles = 114 - x;
			if (cycles > 0) {
				switch(mCPU.GetAdvanceMode()) {
				case kATCPUAdvanceMode_65816HiSpeed:
					while(cycles--) {
						ATSCHEDULER_ADVANCE(&mScheduler);
						uint8 fetchMode = mAntic.PreAdvance();

						if (!mAntic.GetWSYNCFlag())
							cpuEvent = (ATSimulatorEvent)mCPU.Advance65816HiSpeed((fetchMode & 1) != 0);

						mAntic.PostAdvance(fetchMode);

						if (cpuEvent | mPendingEvent)
							goto handle_event;
					}
					break;

				case kATCPUAdvanceMode_65816:
					while(cycles--) {
						ATSCHEDULER_ADVANCE(&mScheduler);
						uint8 fetchMode = mAntic.PreAdvance();

						if (!((fetchMode | mAntic.GetWSYNCFlag()) & 1))
							cpuEvent = (ATSimulatorEvent)mCPU.Advance65816();

						mAntic.PostAdvance(fetchMode);

						if (cpuEvent | mPendingEvent)
							goto handle_event;
					}
					break;

				case kATCPUAdvanceMode_6502:
					while(cycles--) {
						ATSCHEDULER_ADVANCE(&mScheduler);
						uint8 fetchMode = mAntic.PreAdvance();

						if (!((fetchMode | mAntic.GetWSYNCFlag()) & 1))
							cpuEvent = (ATSimulatorEvent)mCPU.Advance6502();

						mAntic.PostAdvance(fetchMode);

						if (cpuEvent | mPendingEvent)
							goto handle_event;
					}
					break;
				}
			}
		}

		return mbRunning ? kAdvanceResult_Running : kAdvanceResult_Stopped;
	}

handle_event:
	ATSimulatorEvent ev = mPendingEvent;
	mPendingEvent = kATSimEvent_None;
	mbRunning = false;

	if (cpuEvent)
		NotifyEvent(cpuEvent);
	if (ev)
		NotifyEvent(ev);

	return mbRunning ? kAdvanceResult_Running : kAdvanceResult_Stopped;
}

uint32 ATSimulator::GetTimestamp() const {
	return ATSCHEDULER_GETTIME(&mScheduler);
}

ATCPUTimestampDecoder ATSimulator::GetTimestampDecoder() const {
	static const sint32 kCyclesPerVS[] = {
		262*114,
		312*114,
		312*114,
		262*114,
		312*114,
	};

	static_assert(vdcountof(kCyclesPerVS) == kATVideoStandardCount, "CyclesPerVS array out of sync");

	return ATCPUTimestampDecoder { mAntic.GetFrameStartTime(), mAntic.GetFrameCounter(), kCyclesPerVS[mVideoStandard] };
}

uint8 ATSimulator::DebugReadByte(uint16 addr) const {
	return mpMemMan->DebugReadByte(addr);
}

uint16 ATSimulator::DebugReadWord(uint16 address) {
	return (uint16)(DebugReadByte(address) + ((uint32)DebugReadByte(address+1) << 8));
}

uint32 ATSimulator::DebugRead24(uint16 address) {
	return (uint32)DebugReadByte(address)
		+ ((uint32)DebugReadByte(address+1) << 8)
		+ ((uint32)DebugReadByte(address+2) << 16);
}

uint8 ATSimulator::DebugExtReadByte(uint32 address) {
	address &= 0xffffff;

	return mpMemMan->DebugExtReadByte((uint16)address, (uint8)(address >> 16));
}

uint8 ATSimulator::DebugGlobalReadByte(uint32 address) {
	switch(address & kATAddressSpaceMask) {
		case kATAddressSpace_CPU:
			return DebugExtReadByte(address);

		case kATAddressSpace_ANTIC:
			return DebugAnticReadByte(address);

		case kATAddressSpace_EXTRAM:
			return mMemory[0x10000 + (address & 0xfffff)];

		case kATAddressSpace_RAM:
			return mMemory[address & 0xffff];

		case kATAddressSpace_VBXE:
			if (!mpVBXE)
				return 0;

			return mpVBXE->GetMemoryBase()[address & 0x7ffff];

		case kATAddressSpace_ROM: {
			const uint32 offset = address & 0xFFFF;

			if (offset >= 0x5000 && offset < 0x5800) {
				if (mpKernelSelfTestROM)
					return mpKernelSelfTestROM[offset & 0x07FF];
			} else if (offset >= 0xA000 && offset < 0xC000) {
				if (mpMemLayerBASICROM)
					return mBASICROM[offset - 0xA000];
			} else if (offset >= 0xC000 && offset < 0xD000) {
				if (mpKernelLowerROM)
					return mpKernelLowerROM[offset - 0xC000];
			} else {
				if (mpKernelUpperROM && offset >= 0xD800 && offset < 0x10000)
					return mpKernelUpperROM[offset - 0xD800];
				else if (mpKernel5200ROM && offset >= 0xF000 && offset < 0x10000)
					return mpKernel5200ROM[offset & 0x7FF];
			}

			return 0;
		}

		case kATAddressSpace_PORTB:
			if (const uint32 addr16 = address & 0xFFFF; addr16 >= 0x4000 && addr16 < 0x8000)
				return mMemory[mpMMU->ExtBankToMemoryOffset((uint8)(address >> 16)) + (addr16 - 0x4000)];
			return 0;

		case kATAddressSpace_CART:
			for(ATCartridgeEmulator *cart : mpCartridge) {
				if (cart)
					return cart->DebugReadLinear(address & kATAddressOffsetMask);
			}

			return 0;

		case kATAddressSpace_CB:
			for(ATCartridgeEmulator *cart : mpCartridge) {
				if (cart)
					return cart->DebugReadBanked(address & kATAddressOffsetMask);
			}

			return 0;

		default:
			return 0;
	}
}

uint16 ATSimulator::DebugGlobalReadWord(uint32 address) {
	uint32 atype = address & kATAddressSpaceMask;
	uint32 aoffset = address & kATAddressOffsetMask;

	return (uint32)DebugGlobalReadByte(address)
		+ ((uint32)DebugGlobalReadByte(atype + ((aoffset + 1) & kATAddressOffsetMask)) << 8);
}

uint32 ATSimulator::DebugGlobalRead24(uint32 address) {
	uint32 atype = address & kATAddressSpaceMask;
	uint32 aoffset = address & kATAddressOffsetMask;

	return (uint32)DebugGlobalReadByte(address)
		+ ((uint32)DebugGlobalReadByte(atype + ((aoffset + 1) & kATAddressOffsetMask)) << 8)
		+ ((uint32)DebugGlobalReadByte(atype + ((aoffset + 2) & kATAddressOffsetMask)) << 16);
}

void ATSimulator::DebugGlobalWriteByte(uint32 address, uint8 value) {
	switch(address & kATAddressSpaceMask) {
		case kATAddressSpace_CPU:
			mpMemMan->ExtWriteByte((uint16)address, (uint8)(address >> 16), value);
			break;

		case kATAddressSpace_EXTRAM:
			mMemory[0x10000 + (address & 0xfffff)] = value;
			break;

		case kATAddressSpace_VBXE:
			if (mpVBXE)
				mpVBXE->GetMemoryBase()[address & 0x7ffff] = value;
			break;

		case kATAddressSpace_RAM:
			mMemory[address & 0xffff] = value;
			break;

		case kATAddressSpace_ROM:
			break;

		case kATAddressSpace_PORTB:
			if (const uint32 addr16 = address & 0xFFFF; addr16 >= 0x4000 && addr16 < 0x8000)
				mMemory[mpMMU->ExtBankToMemoryOffset((uint8)(address >> 16)) + (addr16 - 0x4000)] = value;
			break;
	}
}

uint8 ATSimulator::DebugAnticReadByte(uint16 address) {
	return mpMemMan->DebugAnticReadByte(address);
}

bool ATSimulator::IsKernelROMLocation(uint16 address) const {
	// 5000-57FF
	// C000-CFFF
	// D800-FFFF
	uint32 addr = address;

	// check for self-test ROM
	if ((addr - 0x5000) < 0x0800)
		return (mHardwareMode == kATHardwareMode_800XL || mHardwareMode == kATHardwareMode_1200XL || mHardwareMode == kATHardwareMode_XEGS || mHardwareMode == kATHardwareMode_130XE) && (GetBankRegister() & 0x81) == 0x01;

	// check for XL/XE ROM extension
	if ((addr - 0xC000) < 0x1000)
		return (mHardwareMode == kATHardwareMode_800XL || mHardwareMode == kATHardwareMode_1200XL || mHardwareMode == kATHardwareMode_XEGS || mHardwareMode == kATHardwareMode_130XE) && (GetBankRegister() & 0x01);

	// check for main kernel ROM
	if ((addr - 0xD800) < 0x2800)
		return mHardwareMode == kATHardwareMode_800 || (GetBankRegister() & 0x01);

	// it's not kernel ROM
	return false;
}

namespace {
	uint32 GetMemorySizeForMemoryMode(ATMemoryMode mode) {
		switch(mode) {
			case kATMemoryMode_8K:
				return 0x2000;

			case kATMemoryMode_16K:
				return 0x4000;

			case kATMemoryMode_24K:
				return 0x6000;

			case kATMemoryMode_32K:
				return 0x8000;

			case kATMemoryMode_40K:
				return 0xA000;

			case kATMemoryMode_48K:
			default:
				return 0xC000;

			case kATMemoryMode_52K:
				return 0xD000;

			case kATMemoryMode_64K:
				return 0x10000;

			case kATMemoryMode_128K:
				return 0x20000;

			case kATMemoryMode_256K:
				return 0x40000;

			case kATMemoryMode_320K:
				return 0x50000;

			case kATMemoryMode_576K:
				return 0x90000;

			case kATMemoryMode_1088K:
				return 0x110000;
		}
	}
}

namespace {
	// Compute a Fletcher-32 checksum.
	//
	// The Fletcher-32 checksum is a split checksum where a simple checksum is computed
	// modulo 2^16-1 and a complex checksum is computed off of the incrementally computed
	// simple checksums. It shares the byte swapping capability of a one's complement
	// checksum -- such as the IPv4 checksum -- but can detect reordered data and is only
	// moderately more expensive to compute.
	// 
	uint32 ComputeFletcher32(const void *src, size_t len, uint32 base = 0) {
		size_t len2 = len >> 1;
		uint32 acc1 = (base & 0xffff);
		uint32 acc2 = (base >> 16);

		const uint16 *src16 = (const uint16 *)src;
		while(len2) {
			// We have to break up the input into packets of 359 words or less. The
			// reason:
			//
			// The incoming accumulators can be from 0 to 0x1fffe due to the previous
			// round of folding.
			// The limiting factor is the second accumulator (complex checksum).
			// The largest value of N for which 0x1fffe+sum_i{1..n}(0x1fffe+0xffff*i) < 2^32
			// is 359:
			//
			//   0x1fffe + sum_i{1..n}(0x1fffe+0xffff*i)
			// = 0x1fffe + 0x1fffe*n + sum_i{1..n}(0xffff*i)
			// = 0x1fffe + 0x1fffe*n + 0xffff*sum_i{0..n-1}(i)
			// = 0x1fffe + 0x1fffe*n + 0xffff*(n-1)*n/2
			// = 0x1fffe + 0x1fffe*n + 0xffff/2*n^2 - 0xffff/2*n
			// 
			// Solving the quadratic formula:
			//  32767.5x^2 + 163837.5x + (131070 - 4294967295) = 0
			//
			// ...gives x = 359.54454.

			uint32 tc = (uint32)len2 > 359 ? 359 : (uint32)len2;
			len2 -= tc;

			do {
				acc1 += *src16++;
				acc2 += acc1;
			} while(--tc);

			// fold down to 16 bits
			acc1 = (acc1 & 0xffff) + (acc1 >> 16);
			acc2 = (acc2 & 0xffff) + (acc2 >> 16);
		}

		// handle extra byte (basically, pad with 00)
		if (len & 1) {
			const uint8 c = *(const uint8 *)src16;

			acc1 += c;
			acc2 += acc1;

			// fold down to 16 bits
			acc1 = (acc1 & 0xffff) + (acc1 >> 16);
			acc2 = (acc2 & 0xffff) + (acc2 >> 16);
		}

		// fold down to 16 bits
		acc1 = (acc1 & 0xffff) + (acc1 >> 16);
		acc2 = (acc2 & 0xffff) + (acc2 >> 16);

		// return merged sum
		return (acc2 << 16) + acc1;
	}
}

uint32 ATSimulator::ComputeKernelChecksum() const {
	uint32 sum = 0;

	if (mpKernel5200ROM)
		sum = ComputeFletcher32(mpKernel5200ROM, 0x0800, sum);

	if (mpKernelLowerROM)
		sum = ComputeFletcher32(mpKernelLowerROM, 0x1000, sum);

	if (mpKernelSelfTestROM)
		sum = ComputeFletcher32(mpKernelSelfTestROM, 0x0800, sum);

	if (mpKernelUpperROM)
		sum = ComputeFletcher32(mpKernelUpperROM, 0x2800, sum);

	return sum;
}

uint32 ATSimulator::ComputeKernelCRC32() const {
	VDCRCChecker crcChecker(VDCRCTable::CRC32);

	if (mpKernel5200ROM)
		crcChecker.Process(mpKernel5200ROM, 0x0800);

	if (mpKernelLowerROM)
		crcChecker.Process(mpKernelLowerROM, 0x1000);

	if (mpKernelSelfTestROM)
		crcChecker.Process(mpKernelSelfTestROM, 0x0800);

	if (mpKernelUpperROM)
		crcChecker.Process(mpKernelUpperROM, 0x2800);

	return crcChecker.CRC();
}

bool ATSimulator::LoadState(ATSaveStateReader& reader, ATStateLoadContext *pctx) {
	ATStateLoadContext dummyCtx = {};

	if (!pctx)
		pctx = &dummyCtx;

	// check header
	uint8 header[12];
	reader.ReadData(header, 12);
	if (memcmp(header, kATSaveStateHeader, 12))
		throw ATInvalidSaveStateException();

	// --- past this point we force a cold reset on failure ---

	bool machineDescFound = false;
	bool archStateFound = false;
	bool privateStateOK = false;
	bool privateStateLoaded = false;

	try {
		while(reader.GetAvailable() >= 8) {
			uint32 fcc = reader.ReadUint32();
			uint32 len = reader.ReadUint32();

			reader.OpenChunk(len);

			// quick sanity check
			VDASSERT(fcc >= 0x20202020 && fcc <= 0x7E7E7E7E);

			switch(fcc) {
				case VDMAKEFOURCC('M', 'A', 'C', 'H'):
					LoadStateMachineDesc(reader);
					machineDescFound = true;

					// init load handlers now
					mCPU.BeginLoadState(reader);
					mAntic.BeginLoadState(reader);
					mPokey.BeginLoadState(reader);
					mGTIA.BeginLoadState(reader);
					mPIA.BeginLoadState(reader);

					for(int i=0; i<2; ++i) {
						if (mpCartridge[i])
							mpCartridge[i]->BeginLoadState(reader);
					}

					reader.RegisterHandlerMethod(kATSaveStateSection_Arch, VDMAKEFOURCC('R', 'A', 'M', ' '), this, &ATSimulator::LoadStateMemoryArch);
					break;

				case VDMAKEFOURCC('I', 'S', 'V', 'R'):
					if (reader.GetAvailable() == sizeof(kSaveStateVersion) - 1) {
						char buf[sizeof(kSaveStateVersion) - 1];

						reader.ReadData(buf, sizeof buf);

						if (!memcmp(buf, kSaveStateVersion, sizeof buf))
							privateStateOK = true;
					}
					break;

				case VDMAKEFOURCC('L', 'I', 'S', 'T'):
					fcc = reader.ReadUint32();

					if (!machineDescFound)
						throw ATInvalidSaveStateException();

					switch(fcc) {
						case VDMAKEFOURCC('R', 'E', 'F', 'S'):
							LoadStateRefs(reader, *pctx);

							// check for errors
							if (!pctx->mbAllowKernelMismatch && pctx->mbKernelMismatchDetected)
								return false;

							break;

						case VDMAKEFOURCC('A', 'R', 'C', 'H'):
							LoadStateSection(reader, kATSaveStateSection_Arch);
							archStateFound = true;
							break;

						case VDMAKEFOURCC('A', 'T', 'R', 'A'):
							if (!archStateFound)
								throw ATInvalidSaveStateException();

							if (privateStateOK) {
								LoadStateSection(reader, kATSaveStateSection_Private);
								privateStateLoaded = true;
							}
							break;
					}
					break;
			}

			reader.CloseChunk();
		}

		if (!machineDescFound || !archStateFound)
			throw ATInvalidSaveStateException();

		if (!privateStateLoaded)
			reader.DispatchChunk(kATSaveStateSection_ResetPrivate, 0);

		reader.DispatchChunk(kATSaveStateSection_End, 0);
	} catch(const MyError&) {
		ColdReset();
		throw;
	}

	pctx->mbPrivateStateLoaded = privateStateLoaded;

	// sync state
	UpdateXLCartridgeLine();
	UpdateBanking(GetBankRegister());

	NotifyEvent(kATSimEvent_StateLoaded);
	return true;
}

void ATSimulator::LoadStateMachineDesc(ATSaveStateReader& reader) {
	uint8 memoryModeId = kATMemoryMode_64K;
	uint8 kernelModeId = kATKernelMode_Default;
	uint8 hardwareModeId = kATHardwareMode_800XL;
	uint8 videoStandardId = kATVideoStandard_NTSC;
	bool stereo = false;
	bool mapram = false;
	uint16 vbxebase = 0;
	bool vbxeshared = false;
	uint16 sbbase = 0;
	bool kernelRefPresent = false;
	bool basicRefPresent = false;
	VDStringW kernelRefString;
	VDStringW basicRefString;

	while(reader.GetAvailable() >= 8) {
		const uint32 fcc = reader.ReadUint32();
		const uint32 len = reader.ReadUint32();

		VDASSERT(fcc >= 0x20202020 && fcc <= 0x7E7E7E7E);

		reader.OpenChunk(len);

		switch(fcc) {
			case VDMAKEFOURCC('R', 'A', 'M', ' '):
				memoryModeId = reader.ReadUint8();
				if (memoryModeId >= kATMemoryModeCount)
					throw ATUnsupportedSaveStateException();
				break;

			case VDMAKEFOURCC('K', 'R', 'N', 'L'):
				kernelModeId = reader.ReadUint8();
				if (kernelModeId >= kATKernelModeCount)
					throw ATUnsupportedSaveStateException();
				break;

			case VDMAKEFOURCC('K', 'R', 'N', '2'):
				reader.ReadString(kernelRefString);
				kernelRefPresent = true;
				break;

			case VDMAKEFOURCC('B', 'A', 'S', 'C'):
				reader.ReadString(basicRefString);
				basicRefPresent = true;
				break;

			case VDMAKEFOURCC('H', 'D', 'W', 'R'):
				hardwareModeId = reader.ReadUint8();
				if (hardwareModeId >= kATHardwareModeCount)
					throw ATUnsupportedSaveStateException();
				break;

			case VDMAKEFOURCC('V', 'I', 'D', 'O'):
				videoStandardId = reader.ReadUint8();
				if (videoStandardId >= kATVideoStandardCount)
					throw ATUnsupportedSaveStateException();
				break;

			case VDMAKEFOURCC('S', 'T', 'R', 'O'):
				stereo = true;
				break;

			case VDMAKEFOURCC('V', 'B', 'X', 'E'):
				vbxebase = reader.ReadUint16();
				if (vbxebase != 0xD600 && vbxebase != 0xD700)
					throw ATUnsupportedSaveStateException();

				vbxeshared = reader.ReadBool();
				break;

			case VDMAKEFOURCC('M', 'P', 'R', 'M'):
				mapram = true;
				break;

			case VDMAKEFOURCC('S', 'N', 'B', 'D'):
				sbbase = reader.ReadUint16();

				if (sbbase != 0xD2C0 && sbbase != 0xD500 && sbbase != 0xD600)
					throw ATUnsupportedSaveStateException();
				break;
		}

		reader.CloseChunk();
	}	

	SetMemoryMode((ATMemoryMode)memoryModeId);

	if (kernelRefPresent) {
		uint64 kernelFirmware = mpFirmwareManager->GetFirmwareByRefString(kernelRefString.c_str());

		SetKernel(kernelFirmware);
	}

	if (basicRefPresent) {
		uint64 basicFirmware = mpFirmwareManager->GetFirmwareByRefString(basicRefString.c_str());

		SetBasic(basicFirmware);
	}


	SetHardwareMode((ATHardwareMode)hardwareModeId);
	SetVideoStandard((ATVideoStandard)videoStandardId);

	SetDualPokeysEnabled(stereo);
	SetMapRAMEnabled(mapram);

	// Update the kernel now so we can validate it.
	UpdateKernel(false);

	// Issue a baseline reset so everything is sane.
	ColdReset();

	mStartupDelay = 0;
	mStartupDelay2 = 0;
	mGTIA.SetForcedConsoleSwitches(0xF);
	mpUIRenderer->SetHeldButtonStatus(0);
}

void ATSimulator::LoadStateRefs(ATSaveStateReader& reader, ATStateLoadContext& ctx) {
	while(reader.GetAvailable() >= 8) {
		const uint32 fcc = reader.ReadUint32();
		const uint32 len = reader.ReadUint32();

		VDASSERT(fcc >= 0x20202020 && fcc <= 0x7E7E7E7E);

		reader.OpenChunk(len);

		switch(fcc) {
			case VDMAKEFOURCC('K', 'R', 'N', 'L'):
				if (ComputeKernelChecksum() != reader.ReadUint32())
					ctx.mbKernelMismatchDetected = true;
				break;
		}

		reader.CloseChunk();
	}	
}

void ATSimulator::LoadStateSection(ATSaveStateReader& reader, int section) {
	while(reader.GetAvailable() >= 8) {
		const uint32 fcc = reader.ReadUint32();
		const uint32 len = reader.ReadUint32();

		VDASSERT(fcc >= 0x20202020 && fcc <= 0x7E7E7E7E);

		reader.OpenChunk(len);
		reader.DispatchChunk((ATSaveStateSection)section, fcc);
		reader.CloseChunk();
	}	
}

void ATSimulator::LoadStateMemoryArch(ATSaveStateReader& reader) {
	uint32 offset = reader.ReadUint32();

	uint32 size = GetMemorySizeForMemoryMode(mMemoryMode);
	uint32 rsize = reader.GetAvailable();

	if (rsize > size || size - offset < rsize)
		throw ATInvalidSaveStateException();

	// We need to handle the XRAM split in 128K and 320K modes (groan).
	const bool splitRequired = (size > 0x10000 && size < 0x90000);
	while(rsize) {
		uint32 tc = rsize;
		uint32 loadoffset = offset;

		if (splitRequired) {
			if (loadoffset < 0x10000)
				tc = std::min<uint32>(tc, 0x10000 - loadoffset);
			else
				loadoffset += 0x40000;
		}

		reader.ReadData(mMemory + loadoffset, tc);
		offset += tc;
		rsize -= tc;
	}
}

void ATSimulator::SaveState(ATSaveStateWriter& writer) {
	if (kAdvanceResult_Running != AdvanceUntilInstructionBoundary())
		throw MyError("The emulation state cannot be saved because emulation is stopped in the middle of an instruction.");

	// set up write handlers
	mCPU.BeginSaveState(writer);
	mAntic.BeginSaveState(writer);
	mPokey.BeginSaveState(writer);
	mGTIA.BeginSaveState(writer);
	mPIA.BeginSaveState(writer);

	for(int i=0; i<2; ++i) {
		if (mpCartridge[i])
			mpCartridge[i]->BeginSaveState(writer);
	}

	// write file header
	writer.WriteData(kATSaveStateHeader, sizeof kATSaveStateHeader);

	// write description
	writer.BeginChunk(VDMAKEFOURCC('M', 'A', 'C', 'H'));
		writer.BeginChunk(VDMAKEFOURCC('R', 'A', 'M', ' '));
			writer.WriteUint8(mMemoryMode);
		writer.EndChunk();
		writer.BeginChunk(VDMAKEFOURCC('K', 'R', 'N', '2'));
			writer.WriteString(mpFirmwareManager->GetFirmwareRefString(mKernelId).c_str());
		writer.EndChunk();
		writer.BeginChunk(VDMAKEFOURCC('H', 'D', 'W', 'R'));
			writer.WriteUint8(mHardwareMode);
		writer.EndChunk();
		writer.BeginChunk(VDMAKEFOURCC('V', 'I', 'D', 'O'));
			writer.WriteUint8(mVideoStandard);
		writer.EndChunk();
		writer.BeginChunk(VDMAKEFOURCC('B', 'A', 'S', 'C'));
			writer.WriteString(mpFirmwareManager->GetFirmwareRefString(mBasicId).c_str());
		writer.EndChunk();

		if (mbDualPokeys) {
			writer.BeginChunk(VDMAKEFOURCC('S', 'T', 'R', 'O'));
			writer.EndChunk();
		}

		if (mbMapRAM) {
			writer.BeginChunk(VDMAKEFOURCC('M', 'P', 'R', 'M'));
			writer.EndChunk();
		}

	writer.EndChunk();

	// write save state version
	writer.BeginChunk(VDMAKEFOURCC('I', 'S', 'V', 'R'));
	writer.WriteData(kSaveStateVersion, (sizeof kSaveStateVersion) - 1);
	writer.EndChunk();

	// write program info
	writer.BeginChunk(VDMAKEFOURCC('I', 'P', 'R', 'G'));
	const VDStringA& verStr = VDTextWToU8(AT_PROGRAM_NAME_STR L" " AT_VERSION_STR AT_VERSION_DEBUG_STR AT_VERSION_PRERELEASE_STR, -1);
	writer.WriteData(verStr.data(), verStr.size());
	writer.EndChunk();

	// write references
	writer.BeginChunk(VDMAKEFOURCC('L', 'I', 'S', 'T'));
	writer.WriteUint32(VDMAKEFOURCC('R', 'E', 'F', 'S'));

		// write kernel checksum chunk
		writer.BeginChunk(VDMAKEFOURCC('K', 'R', 'N', 'L'));
		writer.WriteUint32(ComputeKernelChecksum());
		writer.EndChunk();

	writer.EndChunk();

	// write architectural state
	writer.BeginChunk(VDMAKEFOURCC('L', 'I', 'S', 'T'));
	writer.WriteUint32(VDMAKEFOURCC('A', 'R', 'C', 'H'));
	writer.WriteSection(kATSaveStateSection_Arch);

	uint32 size = GetMemorySizeForMemoryMode(mMemoryMode);

	// In XRAM modes that have less than 576K of memory we push up
	// the XRAM in mMemory to aid in VBXE emu, which we must account
	// for here.
	if (size > 0x10000 && size < 0x90000) {
		writer.BeginChunk(VDMAKEFOURCC('R', 'A', 'M', ' '));
		writer.WriteUint32(0);
		writer.WriteData(mMemory, 0x10000);
		writer.WriteData(mMemory + 0x50000, size - 0x10000);
		writer.EndChunk();
	} else {
		writer.BeginChunk(VDMAKEFOURCC('R', 'A', 'M', ' '));
		writer.WriteUint32(0);
		writer.WriteData(mMemory, size);
		writer.EndChunk();
	}

	writer.EndChunk();

	// write private state
	writer.BeginChunk(VDMAKEFOURCC('L', 'I', 'S', 'T'));
	writer.WriteUint32(VDMAKEFOURCC('A', 'T', 'R', 'A'));

	writer.WriteSection(kATSaveStateSection_Private);

	writer.EndChunk();

	// call termination handlers
	writer.WriteSection(kATSaveStateSection_End);
}

void ATSimulator::UpdateXLCartridgeLine() {
	if (mHardwareMode == kATHardwareMode_800XL || mHardwareMode == kATHardwareMode_1200XL || mHardwareMode == kATHardwareMode_XEGS || mHardwareMode == kATHardwareMode_130XE) {
		// TRIG3 indicates a cartridge on XL/XE. MULE fails if this isn't set properly,
		// because for some bizarre reason it jumps through WARMSV!
		const bool rd5 = mpPrivateData->mCartPort.IsLeftMapped();

		mGTIA.SetControllerTrigger(3, !rd5);
	}
}

void ATSimulator::UpdateKeyboardPresentLine() {
	if (mHardwareMode == kATHardwareMode_XEGS)
		mGTIA.SetControllerTrigger(2, !mbKeyboardPresent);
}

void ATSimulator::UpdateForcedSelfTestLine() {
	switch(mHardwareMode) {
		case kATHardwareMode_800XL:
		case kATHardwareMode_1200XL:
		case kATHardwareMode_XEGS:
		case kATHardwareMode_130XE:
			mPokey.SetPotPos(4, mbForcedSelfTest ? 0 : 229);
			break;
	}
}

void ATSimulator::UpdateCartridgeSwitch() {
	const bool state = mbCartridgeSwitch;

	for(IATDeviceButtons *devbtn : mpDeviceManager->GetInterfaces<IATDeviceButtons>(false, false))
		devbtn->ActivateButton(kATDeviceButton_CartridgeSDXEnable, state);
}

void ATSimulator::UpdateBanking(uint8 currBank) {
	if (mHardwareMode == kATHardwareMode_1200XL)
		mpUIRenderer->SetLedStatus((~currBank >> 2) & 3);

	if (mHardwareMode == kATHardwareMode_800XL ||
		mHardwareMode == kATHardwareMode_1200XL ||
		mHardwareMode == kATHardwareMode_XEGS ||
		mHardwareMode == kATHardwareMode_130XE ||
		mMemoryMode == kATMemoryMode_128K ||
		mMemoryMode == kATMemoryMode_320K ||
		mMemoryMode == kATMemoryMode_576K ||
		mMemoryMode == kATMemoryMode_1088K)
	{
		// If we are doing auto-Option to suppress BASIC, release it as soon as the
		// self-test ROM is banked in. By this point, the XL/XE OS will have read
		// Option.
		if (mStartupDelay && !(currBank & 0x80) && !mbStartupHeldKey) {
			uint8 forcedSwitches = mGTIA.GetForcedConsoleSwitches();

			if (!(forcedSwitches & 4)) {
				forcedSwitches |= 4;
				mGTIA.SetForcedConsoleSwitches(forcedSwitches);
				mpUIRenderer->SetHeldButtonStatus(~forcedSwitches);
			}
		}

		mpMMU->SetBankRegister(currBank);
	}
}

bool ATSimulator::UpdateKernel(bool trackChanges, bool forceReload) {
	bool changed = !trackChanges;

	// If U1MB is active, it takes over all base ROMs and we need to force XL/XE kernel mode.
	if (mpUltimate1MB) {
		mKernelMode = kATKernelMode_XL;
		mActualKernelFlags = 0;
	} else {
		uint64 actualKernelId = mKernelId;

		if (!actualKernelId) {
			switch(mHardwareMode) {
				case kATHardwareMode_800:
					actualKernelId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_Kernel800_OSB);
					break;

				case kATHardwareMode_5200:
					actualKernelId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_Kernel5200);
					break;

				case kATHardwareMode_1200XL:
					actualKernelId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_Kernel1200XL);
					break;

				case kATHardwareMode_800XL:
				case kATHardwareMode_130XE:
					actualKernelId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_KernelXL);
					break;

				case kATHardwareMode_XEGS:
					actualKernelId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_KernelXEGS);
					break;
			}
		}

		if (forceReload || mActualKernelId != actualKernelId) {
			mActualKernelId = actualKernelId;
			++mConfigChangeCounter;

			vduint128 krhash = VDHash128(mKernelROM, sizeof mKernelROM);

			memset(mKernelROM, 0, sizeof mKernelROM);

			ATFirmwareInfo kernelInfo;
			bool extended800 = false;

			if (mpFirmwareManager->GetFirmwareInfo(mActualKernelId, kernelInfo)) {
				uint32 fwsize = 0;
				bool fixup800 = false;

				switch(kernelInfo.mType) {
					case kATFirmwareType_Kernel800_OSA:
					case kATFirmwareType_Kernel800_OSB:
						fwsize = 0x4000;
						fixup800 = true;
						break;
					case kATFirmwareType_KernelXL:
					case kATFirmwareType_KernelXEGS:
					case kATFirmwareType_Kernel1200XL:
						fwsize = 0x4000;
						break;
					case kATFirmwareType_Kernel5200:
						fwsize = 0x0800;
						break;
				}

				uint32 actualLen = 0;
				mpFirmwareManager->LoadFirmware(mActualKernelId, mKernelROM + sizeof mKernelROM - fwsize, 0, fwsize, nullptr, &actualLen);

				// fix up 800 mode ROM depending on whether it's 10K (standard) or 16K
				// ($Cxxx extended).
				if (fixup800) {
					if (actualLen <= 0x2800) {
						// 10K only -- move it up to the top and zero the unused $Cxxx and
						// self-test space
						memmove(mKernelROM + 0x1800, mKernelROM, 0x2800);
						memset(mKernelROM, 0, 0x1800);
					} else {
						// 4K + 2Kpad + 10K -- leave as-is
						extended800 = true;
					}
				}

				mActualKernelFlags = kernelInfo.mFlags;
			}

			if (VDHash128(mKernelROM, sizeof mKernelROM) != krhash)
				changed = true;

			switch(kernelInfo.mType) {
				case kATFirmwareType_Kernel800_OSA:
				case kATFirmwareType_Kernel800_OSB:
					if (extended800)
						mKernelMode = kATKernelMode_800Extended;
					else
						mKernelMode = kATKernelMode_800;
					break;

				case kATFirmwareType_KernelXL:
				case kATFirmwareType_KernelXEGS:
				case kATFirmwareType_Kernel1200XL:
					mKernelMode = kATKernelMode_XL;
					break;

				case kATFirmwareType_Kernel5200:
					mKernelMode = kATKernelMode_5200;
					break;
			}
		}

		uint64 basicId = mBasicId;
		if (!basicId)
			basicId = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_Basic);

		if (forceReload || mActualBasicId != basicId) {
			mActualBasicId = basicId;

			vduint128 bhash = changed ? vduint128(0) : VDHash128(mBASICROM, sizeof mBASICROM);

			memset(mBASICROM, 0, sizeof mBASICROM);

			mpFirmwareManager->LoadFirmware(mActualBasicId, mBASICROM, 0, sizeof mBASICROM);

			if (!changed && VDHash128(mBASICROM, sizeof mBASICROM) != bhash)
				changed = true;
		}

		vduint128 ghash = changed ? vduint128(0) : VDHash128(mGameROM, sizeof mGameROM);

		memset(mGameROM, 0, sizeof mGameROM);
		mpFirmwareManager->LoadFirmware(mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_Game), mGameROM, 0, sizeof mGameROM);

		if (!changed && VDHash128(mGameROM, sizeof mGameROM) != ghash)
			changed = true;
	}

	IATDebugger *deb = ATGetDebugger();

	for(size_t i=0; i<vdcountof(mKernelSymbolsModuleIds); ++i) {
		if (mKernelSymbolsModuleIds[i]) {
			deb->UnloadSymbols(mKernelSymbolsModuleIds[i]);
			mKernelSymbolsModuleIds[i] = 0;
		}
	}

	UpdateKernelROMSegments();

	return trackChanges && changed;
}

void ATSimulator::UpdateVBXEPage() {
	sint32 baseOverride = mpUltimate1MB ? mpUltimate1MB->GetVBXEPage() : -1;

	for(IATDeviceU1MBControllable *dev : mpDeviceManager->GetInterfaces<IATDeviceU1MBControllable>(false, false))
		dev->SetU1MBControl(kATU1MBControl_VBXEBase, baseOverride);
}

void ATSimulator::UpdateSoundBoardPage() {
	sint32 baseOverride = mpUltimate1MB ? mpUltimate1MB->IsSoundBoardEnabled() ? 0xD2C0 : 0 : -1;

	for(IATDeviceU1MBControllable *dev : mpDeviceManager->GetInterfaces<IATDeviceU1MBControllable>(false, false))
		dev->SetU1MBControl(kATU1MBControl_SoundBoardBase, baseOverride);
}

bool ATSimulator::ReloadU1MBFirmware() {
	bool changed = false;

	if (mpUltimate1MB) {
		uint64 id = mpFirmwareManager->GetCompatibleFirmware(kATFirmwareType_U1MB);

		if (id)
			changed = mpUltimate1MB->LoadFirmware(*mpFirmwareManager, id);
		else
			changed = mpUltimate1MB->LoadFirmware(NULL, 0);
	}

	return changed;
}

namespace {
	template<class T_Class, uint8 (T_Class::*T_ReadHandler)(uint8 address) const>
	sint32 BindReadHandlerConst(void *thisptr, uint32 addr) {
		return (uint8)(((T_Class *)thisptr)->*T_ReadHandler)((uint8)addr);
	}

	template<class T_Class, uint8 (T_Class::*T_ReadHandler)(uint8 address)>
	sint32 BindReadHandler(void *thisptr, uint32 addr) {
		return (uint8)(((T_Class *)thisptr)->*T_ReadHandler)((uint8)addr);
	}

	template<class T_Class, void (T_Class::*T_WriteHandler)(uint8 address, uint8 data)>
	bool BindWriteHandler(void *thisptr, uint32 addr, uint8 data) {
		(((T_Class *)thisptr)->*T_WriteHandler)((uint8)addr, data);
		return true;
	}
}

void ATSimulator::InitMemoryMap() {
	ShutdownMemoryMap();

	if (!mpPrivateData->mpRapidus)
		mpMemMan->SetFastBusEnabled(mCPU.GetSubCycles() > 1);

	const bool ioBusFloat = (mHardwareMode == kATHardwareMode_800 && mbFloatingIoBus);
	mpMemMan->SetFloatingIoBus(ioBusFloat);

	if (ioBusFloat) {
		ATMemoryHandlerTable pfReadHandlers = {};
		pfReadHandlers.mpThis = mpMemMan;
		pfReadHandlers.mpReadHandler = [](void *thisPtr, uint32 addr) -> sint32 {
			return ((ATMemoryManager *)thisPtr)->ReadFloatingIoDataBus();
		};
		pfReadHandlers.mpWriteHandler = [](void *thisPtr, uint32 addr, uint8 value) -> bool {
			((ATMemoryManager *)thisPtr)->WriteIoDataBus(value);

			return false;
		};
		pfReadHandlers.mpDebugReadHandler = pfReadHandlers.mpReadHandler;

		mpMemLayerIoBusFloat = mpMemMan->CreateLayer(kATMemoryPri_BaseRAM, pfReadHandlers, 0xC0, 0x18);
		mpMemMan->SetLayerName(mpMemLayerIoBusFloat, "I/O floating bus trap");
		mpMemMan->SetLayerModes(mpMemLayerIoBusFloat, kATMemoryAccessMode_ARW);
	}

	// create main RAM layer
	uint32 loMemPages = 0;
	uint32 hiMemPages = 0;

	switch(mMemoryMode) {
		case kATMemoryMode_8K:
			loMemPages = 8*4;
			break;

		case kATMemoryMode_16K:
			loMemPages = 16*4;
			break;

		case kATMemoryMode_24K:
			loMemPages = 24*4;
			break;

		case kATMemoryMode_32K:
			loMemPages = 32*4;
			break;

		case kATMemoryMode_40K:
			loMemPages = 40*4;
			break;

		case kATMemoryMode_48K:
			loMemPages = 48*4;
			break;

		case kATMemoryMode_52K:
			loMemPages = 52*4;
			break;

		default:
			if (mHardwareMode == kATHardwareMode_800) {
				loMemPages = 48*4;
			} else {
				loMemPages = 52*4;
				hiMemPages = 10*4;
			}
			break;
	}

	mpMemLayerLoRAM = mpMemMan->CreateLayer(kATMemoryPri_BaseRAM, mMemory, 0, loMemPages, false);
	mpMemMan->SetLayerName(mpMemLayerLoRAM, "Low RAM");
	mpMemMan->SetLayerFastBus(mpMemLayerLoRAM, true);
	mpMemMan->EnableLayer(mpMemLayerLoRAM, true);

	if (hiMemPages) {
		mpMemLayerHiRAM = mpMemMan->CreateLayer(kATMemoryPri_BaseRAM, mMemory + 0xD800, 0xD8, hiMemPages, false);
		mpMemMan->SetLayerName(mpMemLayerHiRAM, "High RAM");
		mpMemMan->SetLayerFastBus(mpMemLayerHiRAM, true);
		mpMemMan->EnableLayer(mpMemLayerHiRAM, true);
	}

	// create extended RAM layer
	mpMemLayerExtendedRAM = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM, mMemory, 0x40, 16 * 4, false);
	mpMemMan->SetLayerName(mpMemLayerExtendedRAM, "Extended RAM");
	mpMemMan->SetLayerFastBus(mpMemLayerExtendedRAM, true);

	// create kernel ROM layer(s)
	if (mpKernelSelfTestROM) {
		mpMemLayerSelfTestROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mpKernelSelfTestROM, 0x50, 0x08, true);
		mpMemMan->SetLayerName(mpMemLayerSelfTestROM, "Self-test ROM");
		mpMemMan->SetLayerFastBus(mpMemLayerSelfTestROM, mbShadowROM);
		mpMemMan->SetLayerAddressSpace(mpMemLayerSelfTestROM, kATAddressSpace_ROM + 0x5000);
	}

	if (mpKernelLowerROM) {
		mpMemLayerLowerKernelROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mpKernelLowerROM, 0xC0, 0x10, true);
		mpMemMan->SetLayerAddressSpace(mpMemLayerLowerKernelROM, kATAddressSpace_ROM + 0xC000);
	} else if (mpKernel5200ROM) {
		mpMemLayerLowerKernelROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mpKernel5200ROM, 0xF0, 0x08, true);
		mpMemMan->SetLayerAddressSpace(mpMemLayerLowerKernelROM, kATAddressSpace_ROM + 0xF000);
	}

	if (mpKernelUpperROM) {
		mpMemLayerUpperKernelROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mpKernelUpperROM, 0xD8, 0x28, true);
		mpMemMan->SetLayerAddressSpace(mpMemLayerUpperKernelROM, kATAddressSpace_ROM + 0xD800);
	} else if (mpKernel5200ROM) {
		mpMemLayerUpperKernelROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mpKernel5200ROM, 0xF8, 0x08, true);
		mpMemMan->SetLayerAddressSpace(mpMemLayerUpperKernelROM, kATAddressSpace_ROM + 0xF000);
	}

	if (mpMemLayerLowerKernelROM) {
		mpMemMan->SetLayerName(mpMemLayerLowerKernelROM, "Lower kernel ROM");
		mpMemMan->SetLayerFastBus(mpMemLayerLowerKernelROM, mbShadowROM);
		mpMemMan->EnableLayer(mpMemLayerLowerKernelROM, true);
	}

	if (mpMemLayerUpperKernelROM) {
		mpMemMan->SetLayerName(mpMemLayerUpperKernelROM, "Upper kernel ROM");
		mpMemMan->SetLayerFastBus(mpMemLayerUpperKernelROM, mbShadowROM);
		mpMemMan->SetLayerIoBus(mpMemLayerUpperKernelROM, true);
		mpMemMan->EnableLayer(mpMemLayerUpperKernelROM, true);
	}

	// create BASIC ROM layer
	mpMemLayerBASICROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mBASICROM, 0xA0, 0x20, true);
	mpMemMan->SetLayerName(mpMemLayerBASICROM, "BASIC ROM");
	mpMemMan->SetLayerFastBus(mpMemLayerBASICROM, mbShadowROM);
	mpMemMan->SetLayerAddressSpace(mpMemLayerBASICROM, kATAddressSpace_ROM + 0xA000);

	// create game ROM layer
	if (mHardwareMode == kATHardwareMode_XEGS) {
		mpMemLayerGameROM = mpMemMan->CreateLayer(kATMemoryPri_ROM, mGameROM, 0xA0, 0x20, true);
		mpMemMan->SetLayerName(mpMemLayerGameROM, "Game ROM");
		mpMemMan->SetLayerFastBus(mpMemLayerGameROM, mbShadowROM);
	}

	// create hardware layers
	ATMemoryHandlerTable handlerTable;

	handlerTable.mbPassAnticReads = false;
	handlerTable.mbPassReads = false;
	handlerTable.mbPassWrites = false;

	handlerTable.mpThis = &mAntic;
	handlerTable.mpDebugReadHandler = BindReadHandlerConst<ATAnticEmulator, &ATAnticEmulator::ReadByte>;
	handlerTable.mpReadHandler = BindReadHandlerConst<ATAnticEmulator, &ATAnticEmulator::ReadByte>;
	handlerTable.mpWriteHandler = BindWriteHandler<ATAnticEmulator, &ATAnticEmulator::WriteByte>;
	mpMemLayerANTIC = mpMemMan->CreateLayer(kATMemoryPri_ROM, handlerTable, 0xD4, 0x01);
	mpMemMan->SetLayerName(mpMemLayerANTIC, "ANTIC");

	handlerTable.mpThis = &mGTIA;
	handlerTable.mpDebugReadHandler = BindReadHandler<ATGTIAEmulator, &ATGTIAEmulator::DebugReadByte>;
	handlerTable.mpReadHandler = BindReadHandler<ATGTIAEmulator, &ATGTIAEmulator::ReadByte>;
	handlerTable.mpWriteHandler = BindWriteHandler<ATGTIAEmulator, &ATGTIAEmulator::WriteByte>;

	if (mHardwareMode == kATHardwareMode_5200)
		mpMemLayerGTIA = mpMemMan->CreateLayer(kATMemoryPri_Hardware, handlerTable, 0xC0, 0x04);
	else
		mpMemLayerGTIA = mpMemMan->CreateLayer(kATMemoryPri_Hardware, handlerTable, 0xD0, 0x01);

	mpMemMan->SetLayerName(mpMemLayerGTIA, "GTIA");

	handlerTable.mpThis = &mPokey;
	handlerTable.mpDebugReadHandler = BindReadHandlerConst<ATPokeyEmulator, &ATPokeyEmulator::DebugReadByte>;
	handlerTable.mpReadHandler = BindReadHandler<ATPokeyEmulator, &ATPokeyEmulator::ReadByte>;
	handlerTable.mpWriteHandler = BindWriteHandler<ATPokeyEmulator, &ATPokeyEmulator::WriteByte>;

	if (mHardwareMode == kATHardwareMode_5200)
		mpMemLayerPOKEY = mpMemMan->CreateLayer(kATMemoryPri_Hardware, handlerTable, 0xE8, 0x04);
	else
		mpMemLayerPOKEY = mpMemMan->CreateLayer(kATMemoryPri_Hardware, handlerTable, 0xD2, 0x01);

	mpMemMan->SetLayerIoBus(mpMemLayerPOKEY, true);
	mpMemMan->SetLayerName(mpMemLayerPOKEY, "POKEY");

	mpMemMan->EnableLayer(mpMemLayerANTIC, true);
	mpMemMan->EnableLayer(mpMemLayerGTIA, true);
	mpMemMan->EnableLayer(mpMemLayerPOKEY, true);

	if (mHardwareMode != kATHardwareMode_5200) {
		if (mbMapRAM) {
			mpMemLayerHiddenRAM = mpMemMan->CreateLayer(kATMemoryPri_ExtRAM, mMemory + 0xD000, 0x50, 0x08, false);
			mpMemMan->SetLayerName(mpMemLayerHiddenRAM, "Hidden RAM");
		}

		handlerTable.mpThis = &mPIA;
		handlerTable.mpDebugReadHandler = BindReadHandlerConst<ATPIAEmulator, &ATPIAEmulator::DebugReadByte>;
		handlerTable.mpReadHandler = BindReadHandler<ATPIAEmulator, &ATPIAEmulator::ReadByte>;
		handlerTable.mpWriteHandler = BindWriteHandler<ATPIAEmulator, &ATPIAEmulator::WriteByte>;
		mpMemLayerPIA = mpMemMan->CreateLayer(kATMemoryPri_Hardware, handlerTable, 0xD3, 0x01);
		mpMemMan->SetLayerName(mpMemLayerPIA, "PIA");
		mpMemMan->SetLayerIoBus(mpMemLayerPIA, true);
		mpMemMan->EnableLayer(mpMemLayerPIA, true);

		mpMMU->InitMapping(mHardwareMode,
			mMemoryMode,
			mMemory,
			mpMemLayerExtendedRAM,
			mpMemLayerSelfTestROM,
			mpMemLayerLowerKernelROM,
			mpMemLayerUpperKernelROM,
			mpMemLayerBASICROM,
			mpMemLayerGameROM,
			mpMemLayerHiddenRAM);
	}

	if (mpUltimate1MB) {
		mpUltimate1MB->SetROMLayers(
			mpMemLayerLowerKernelROM,
			mpMemLayerUpperKernelROM,
			mpMemLayerBASICROM,
			mpMemLayerSelfTestROM,
			mpMemLayerGameROM,
			mKernelROM);
	}

	for(IATDeviceSystemControl *syscon : mpDeviceManager->GetInterfaces<IATDeviceSystemControl>(false, false)) {
		syscon->SetROMLayers(
			mpMemLayerLowerKernelROM,
			mpMemLayerUpperKernelROM,
			mpMemLayerBASICROM,
			mpMemLayerSelfTestROM,
			mpMemLayerGameROM,
			mKernelROM);
	}

	// Compute which bits on PIA port B should float:
	//
	//	400/800:
	//		No floating bits -- pull-ups on all lines
	//
	//	1200XL:
	//		Bits 1-6 float
	//
	//	600/800XL:
	//		Bits 2-6 float
	//
	//	130XE:
	//		Bit 6 floats
	//
	//	XEGS:
	//		Bits 2-5 float
	//
	// In addition, any bits used for banking are excluded:
	// 128K:		bits 2, 3
	// 192K:		bits 2, 3, 6
	// 320K:		bits 2, 3, 5, 6
	// 576K:		bits 1, 2, 3, 5, 6
	// 1088K:		bits 1, 2, 3, 5, 6, 7
	// 320K COMPY:	bits 2, 3, 6, 7
	// 576K COMPY:	bits 1, 2, 3, 6, 7
	//
	// Note that we must ignore the memory mode override set by U1MB. U1MB
	// emulates mapping modes by shadowing the PIA, so it doesn't see
	// floating bits. For sanity reasons, we assume that the U1MB system
	// doesn't have a standard memory expansion also installed, and therefore
	// doesn't have additional port B bits pulled up.
	//
	// Approximate decay times as seen on real hardware:
	//	800XL: $04B6-$059C scanline pairs
	//	800XL+U1MB: $02EE-$03F2 scanline pairs
	//	130XE: $0793-$0E14 scanline pairs
	//
	// Note that these are likely to vary widely between individual systems even
	// of the same model. We use these values just to have some grounding in
	// reality.
	//
	
	uint8 floatMask = 0;
	uint32 floatMin = 0;
	uint32 floatMax = 0;

	switch(mHardwareMode) {
		case kATHardwareMode_1200XL:
			floatMask = 0x7E;
			floatMin = 0x04B6 * 228;	// placeholder - not observed
			floatMax = 0x059C * 228;
			break;

		case kATHardwareMode_800XL:
			floatMask = 0x7C;
			floatMin = 0x04B6 * 228;
			floatMax = 0x059C * 228;
			break;

		case kATHardwareMode_130XE:
			floatMask = 0x40;
			floatMin = 0x0793 * 228;
			floatMax = 0x0E14 * 228;
			break;

		case kATHardwareMode_XEGS:
			floatMask = 0x3C;
			floatMin = 0x04B6 * 228;	// placeholder - not observed
			floatMax = 0x059C * 228;
			break;
	}

	static_assert(kATHardwareModeCount == 6, "Add new hardware mode to floating port B bit init");

	if (!mpUltimate1MB) {
		switch(mMemoryMode) {
			case kATMemoryMode_128K:		floatMask &= ~0x0C; break;
			case kATMemoryMode_256K:		floatMask &= ~0x4C; break;
			case kATMemoryMode_320K:		floatMask &= ~0xCC; break;
			case kATMemoryMode_576K:		floatMask &= ~0x6C; break;
			case kATMemoryMode_1088K:		floatMask &= ~0x6E; break;
			case kATMemoryMode_320K_Compy:	floatMask &= ~0xCC; break;
			case kATMemoryMode_576K_Compy:	floatMask &= ~0xCE; break;
		}

		static_assert(kATMemoryModeCount == 15, "Add new memory mode to floating port B bit init");
	}

	if (mpPrivateData->mFloatingInputs.mFloatingInputMask != floatMask ||
		mpPrivateData->mFloatingInputs.mDecayTimeMin != floatMin)
	{
		mpPrivateData->mFloatingInputs.mFloatingInputMask = floatMask;
		mpPrivateData->mFloatingInputs.mDecayTimeMin = floatMin;
		mpPrivateData->mFloatingInputs.mDecayTimeRange = floatMax - floatMin;

		if (floatMask)
			mPIA.SetPortBFloatingInputs(&mpPrivateData->mFloatingInputs);
		else
			mPIA.SetPortBFloatingInputs(nullptr);
	}
}

void ATSimulator::ShutdownMemoryMap() {
	if (mpUltimate1MB) {
		mpUltimate1MB->SetROMLayers(
			nullptr,
			nullptr,
			nullptr,
			nullptr,
			nullptr,
			mKernelROM);
	}

	for(IATDeviceSystemControl *syscon : mpDeviceManager->GetInterfaces<IATDeviceSystemControl>(false, false)) {
		syscon->SetROMLayers(
			nullptr,
			nullptr,
			nullptr,
			nullptr,
			nullptr,
			mKernelROM);
	}

	if (mpMMU)
		mpMMU->ShutdownMapping();

#define X(layer) if (layer) { mpMemMan->DeleteLayer(layer); layer = NULL; }
	X(mpMemLayerLoRAM)
	X(mpMemLayerHiRAM)
	X(mpMemLayerExtendedRAM)
	X(mpMemLayerLowerKernelROM)
	X(mpMemLayerUpperKernelROM)
	X(mpMemLayerBASICROM)
	X(mpMemLayerGameROM)
	X(mpMemLayerHiddenRAM)
	X(mpMemLayerSelfTestROM)
	X(mpMemLayerANTIC)
	X(mpMemLayerGTIA)
	X(mpMemLayerPOKEY)
	X(mpMemLayerPIA)
	X(mpMemLayerIoBusFloat)
#undef X
}

void ATSimulator::UpdateKernelROMSegments() {
	mpKernelLowerROM = NULL;
	mpKernelSelfTestROM = NULL;
	mpKernelUpperROM = NULL;
	mpKernel5200ROM = NULL;

	const uint8 *kernelROM = mKernelROM;

	mKernelFlagsMask = ~UINT32_C(0);
	
	for(const auto& override : mpPrivateData->mKernelROMOverrides) {
		if (override.mpROM) {
			kernelROM = (const uint8 *)override.mpROM;
			mKernelFlagsMask = 0;
		}
	}

	switch(mKernelMode) {
		case kATKernelMode_800:
			mpKernelUpperROM = kernelROM + 0x1800;
			break;
		case kATKernelMode_800Extended:
			mpKernelLowerROM = kernelROM;
			mpKernelUpperROM = kernelROM + 0x1800;
			break;
		case kATKernelMode_XL:
			mpKernelLowerROM = kernelROM;
			mpKernelSelfTestROM = kernelROM + 0x1000;
			mpKernelUpperROM = kernelROM + 0x1800;
			break;
		case kATKernelMode_5200:
			mpKernel5200ROM = kernelROM + 0x3800;
			break;
	}
}

void ATSimulator::UpdateKernelROMPtrs() {
	if (mpMemLayerLowerKernelROM)
		mpMemMan->SetLayerMemory(mpMemLayerLowerKernelROM, mpKernelLowerROM);

	if (mpMemLayerUpperKernelROM)
		mpMemMan->SetLayerMemory(mpMemLayerUpperKernelROM, mpKernelUpperROM);

	if (mpMemLayerSelfTestROM)
		mpMemMan->SetLayerMemory(mpMemLayerSelfTestROM, mpKernelSelfTestROM);
}

void ATSimulator::UpdateKernelROMSpeeds() {
	bool highSpeed = mbShadowROM;

	for(const auto& override : mpPrivateData->mKernelROMOverrides) {
		if (override.mSpeedOverride)
			highSpeed = override.mSpeedOverride > 0;
	}

	if (mpMemLayerSelfTestROM)		mpMemMan->SetLayerFastBus(mpMemLayerSelfTestROM, highSpeed);
	if (mpMemLayerLowerKernelROM)	mpMemMan->SetLayerFastBus(mpMemLayerLowerKernelROM, highSpeed);
	if (mpMemLayerUpperKernelROM)	mpMemMan->SetLayerFastBus(mpMemLayerUpperKernelROM, highSpeed);
}

uint32 ATSimulator::CPUGetCycle() {
	return ATSCHEDULER_GETTIME(&mScheduler);
}

uint32 ATSimulator::CPUGetUnhaltedCycle() {
	return ATSCHEDULER_GETTIME(&mScheduler) - mAntic.GetHaltedCycleCount();
}

void ATSimulator::CPUGetHistoryTimes(ATCPUHistoryEntry * VDRESTRICT he) const {
	const uint32 t = ATSCHEDULER_GETTIME(&mScheduler);
	const uint32 hc = mAntic.GetHaltedCycleCount();

	he->mCycle = t;
	he->mUnhaltedCycle = t - hc;
}

uint8 ATSimulator::AnticReadByte(uint32 addr) {
	return *mpAnticBusData = mpMemMan->AnticReadByte(addr);
}

void ATSimulator::AnticAssertNMI_VBI() {
	mCPU.AssertNMI();

	NotifyEvent(kATSimEvent_VBI);
}

void ATSimulator::AnticAssertNMI_DLI() {
	mCPU.AssertNMI();
}

void ATSimulator::AnticAssertNMI_RES() {
	mCPU.AssertNMI();
}

void ATSimulator::AnticEndFrame() {
	mPokey.AdvanceFrame(mGTIA.IsFrameInProgress(), mScheduler.GetTick64());

	if (mAntic.GetAnalysisMode()) {
		mGTIA.SetForcedBorder(true);
		mGTIA.RenderActivityMap(mAntic.GetActivityMap());
	} else {
		mGTIA.SetForcedBorder(false);
	}

	NotifyEvent(kATSimEvent_FrameTick);

	mpUIRenderer->SetCassetteIndicatorVisible(mpCassette->IsLoaded() && mpCassette->IsMotorRunning());
	mpUIRenderer->SetCassettePosition(mpCassette->GetPosition(), mpCassette->GetLength(), mpCassette->IsRecordEnabled(), mpCassette->IsFSKDecodingEnabled());

	if (mpPrivateData->mpTraceContext) {
		uint64 traceSize = mpPrivateData->mpTraceContext->mMemTracker.GetSize();

		mpUIRenderer->SetTracingSize(traceSize);

		if (traceSize >= mpPrivateData->mTraceSizeLimit) {
			mpSimEventManager->NotifyEvent(kATSimEvent_TracingLimitReached);

			SetTracingEnabled(nullptr);
		}
	}

	mpUIRenderer->Update();

	mGTIA.UpdateScreen(false, false);

	const bool palTick = (mVideoStandard != kATVideoStandard_NTSC && mVideoStandard != kATVideoStandard_PAL60);
	const uint32 ticks300Hz = palTick ? 6 : 5;

	for(IATDeviceVideoOutput *vo : mpDeviceManager->GetInterfaces<IATDeviceVideoOutput>(false, false))
		vo->Tick(ticks300Hz);

	if (mbBreakOnFrameEnd) {
		mbBreakOnFrameEnd = false;
		PostInterruptingEvent(kATSimEvent_EndOfFrame);
	}

	const float dt = palTick ? 1.0f / 50.0f : 1.0f / 60.0f;
	mpInputManager->Poll(dt);

	// Turn off automatic OPTION key for disabling BASIC once the VBI is enabled.
	if (mbPowered) {
		if (mStartupDelay && mAntic.IsPlayfieldDMAEnabled()) {
			if (!--mStartupDelay) {
				mGTIA.SetForcedConsoleSwitches(0x0F);
				mpUIRenderer->SetHeldButtonStatus(0);

				if (mbStartupHeldKey) {
					mPokey.ReleaseAllRawKeys(false);
					mbStartupHeldKey = false;
				}

				mStartupDelay2 = 0;

				if (mbCassetteAutoBootEnabled && mpCassette->IsLoaded()) {
					if (mbCassetteAutoBootUseBasic) {
						mStartupDelay2 = 1;
					} else if (!mbCassetteSIOPatchEnabled) {
						// push a space into the keyboard routine (only needed if the SIO patch is
						// not suppressing this)
						mPokey.PushKey(0x21, false);
					}
				}

				if (mpHeatMap)
					mpHeatMap->SetEarlyState(false);
			}
		}

		if (mStartupDelay2 && !--mStartupDelay2) {
			mStartupDelay = 0;
			mStartupDelay2 = 0;

			mGTIA.SetForcedConsoleSwitches(0x0F);
			mpUIRenderer->SetHeldButtonStatus(0);

			if (mpHeatMap)
				mpHeatMap->SetEarlyState(false);

			if (mbCassetteAutoBootEnabled && mpCassette->IsLoaded() && mbCassetteAutoBootUseBasic) {
				if (mpHLEBasicLoader) {
					mpHLEBasicLoader->Shutdown();
					delete mpHLEBasicLoader;
					mpHLEBasicLoader = NULL;
				}

				vdautoptr<ATHLEBasicLoader> loader(new ATHLEBasicLoader);

				loader->Init(&mCPU, mpSimEventManager, this);
				loader->LoadTape(!mbCassetteSIOPatchEnabled);

				mpHLEBasicLoader = loader.release();
			}

		}
	} else {
		if (!mbPowered) {
			if (mPoweronDelayCounter)
				--mPoweronDelayCounter;
			else {
				mbPowered = true;
				InternalColdReset(true);
			}
		}
	}

	mCPU.PeriodicCleanup();

	mScheduler.UpdateTick64();
	mSlowScheduler.UpdateTick64();

	mpPrivateData->mDeferredEventManager.FlushPendingEvents();
}

void ATSimulator::AnticEndScanline() {
	int y = mAntic.GetBeamY();

	if (y == mBreakOnScanline) {
		mBreakOnScanline = -1;
		NotifyEvent(kATSimEvent_ScanlineBreakpoint);
	}

	if (y == 247) {
		if (mpCheatEngine)
			mpCheatEngine->ApplyCheats();

		NotifyEvent(kATSimEvent_VBLANK);
	}

	mPokey.AdvanceScanLine();

	ATSCHEDULER_ADVANCE(&mSlowScheduler);
}

bool ATSimulator::AnticIsNextCPUCycleWrite() {
	return mCPU.IsNextCycleWrite();
}

uint8 ATSimulator::AnticGetCPUHeldCycleValue() {
	return mCPU.GetHeldCycleValue();
}

void ATSimulator::AnticForceNextCPUCycleSlow() {
	mCPU.ForceNextCycleSlow();
}

void ATSimulator::AnticOnVBlank() {
	if (mpJoysticks)
		mpJoysticks->Poll();
}

uint32 ATSimulator::GTIAGetXClock() {
	return mAntic.GetBeamX() * 2;
}

uint32 ATSimulator::GTIAGetTimestamp() const {
	return ATSCHEDULER_GETTIME(&mScheduler);
}

uint64 ATSimulator::GTIAGetTimestamp64() const {
	return mScheduler.GetTick64();
}

void ATSimulator::GTIASetSpeaker(bool newState) {
	mPokey.SetSpeaker(newState);
}

void ATSimulator::GTIASelectController(uint8 index, bool potsEnabled) {
	if (mpInputManager)
		mpInputManager->Select5200Controller(index, potsEnabled);
}

void ATSimulator::GTIARequestAnticSync(int offset) {
	mAntic.SyncWithGTIA(offset);
}

uint32 ATSimulator::GTIAGetLineEdgeTimingId(uint32 offset) const {
	uint32 t = ATSCHEDULER_GETTIME(&mScheduler);
	uint32 x = mAntic.GetBeamX();

	t -= x;

	if (x >= offset)
		t += 114;

	return t;
}

void ATSimulator::PokeyAssertIRQ(bool cpuBased) {
	mpPrivateData->mIRQController.Assert(kATIRQSource_POKEY, cpuBased);
}

void ATSimulator::PokeyNegateIRQ(bool cpuBased) {
	mpPrivateData->mIRQController.Negate(kATIRQSource_POKEY, cpuBased);
}

void ATSimulator::PokeyBreak() {
	mbBreak = true;
}

bool ATSimulator::PokeyIsInInterrupt() const {
	return (mCPU.GetP() & AT6502::kFlagI) != 0;
}

bool ATSimulator::PokeyIsKeyPushOK(uint8 c, bool cooldownExpired) const {
	if (mStartupDelay)
		return false;

	if (cooldownExpired)
		return true;

	// check for char still pending in K:
	if (DebugReadByte(ATKernelSymbols::CH) != 0xFF)
		return false;

	// check for debounce
	if (DebugReadByte(ATKernelSymbols::CH1) == c) {
		if (DebugReadByte(ATKernelSymbols::KEYDEL))
			return false;
	}

	return true;
}

void ATSimulator::ReinitHookPage() {
	mHookPage = 0;

	// try to find a good page for the hook page
	enum HookPage : uint8 {
		D6 = 1,
		D7 = 2,
		D1 = 4,
		D5 = 8,
		All = 15
	};

	uint8 hookPagesOccupied = 0;

	for(IATDeviceMemMap *mm : mpDeviceManager->GetInterfaces<IATDeviceMemMap>(false, false)) {
		uint32 lo, hi;
		for(uint32 i=0; mm->GetMappedRange(i, lo, hi); ++i) {
			if (hi > 0xD100 && lo < 0xD200)
				hookPagesOccupied |= HookPage::D1;

			if (hi > 0xD500 && lo < 0xD600)
				hookPagesOccupied |= HookPage::D5;

			if (hi > 0xD600 && lo < 0xD700)
				hookPagesOccupied |= HookPage::D6;

			if (hi > 0xD700 && lo < 0xD800)
				hookPagesOccupied |= HookPage::D7;
		}
	}

	if (mpUltimate1MB)
		hookPagesOccupied |= HookPage::D1 | HookPage::D6 | HookPage::D7;

	if (mpCartridge[0] || mpCartridge[1])
		hookPagesOccupied |= HookPage::D5;

	if (hookPagesOccupied == HookPage::All) {
		mHookPage = 0xD3;
	} else {
		static const uint8 kHookPages[]={ 0xD6, 0xD7, 0xD1, 0xD5 };

		mHookPage = kHookPages[VDFindLowestSetBitFast(~hookPagesOccupied)];
	}

	mpHLECIOHook->ReinitHooks(mHookPage);
}

void ATSimulator::SetupPendingHeldButtons() {
	if (mPendingHeldKey >= 0 || mPendingHeldSwitches) {
		mStartupDelay = 10;
		mStartupDelay2 = 150;

		const uint8 consoleSwitches = 0x08 | (~mPendingHeldSwitches & 7);

		if (mPendingHeldKey != 0xFF) {
			mPokey.PushRawKey((uint8)mPendingHeldKey, false);
			mbStartupHeldKey = true;
		}

		mPendingHeldKey = -1;
		mPendingHeldSwitches = 0;

		mpUIRenderer->SetPendingHeldButtons(0);
		mpUIRenderer->SetPendingHeldKey(-1);

		mGTIA.SetForcedConsoleSwitches(consoleSwitches);
		mpUIRenderer->SetHeldButtonStatus(~consoleSwitches);
	}
}

void ATSimulator::ResetAutoHeldButtons() {
	mStartupDelay = 0;
	mStartupDelay2 = 0;

	if (mbStartupHeldKey) {
		mbStartupHeldKey = false;

		mPokey.ReleaseAllRawKeys(false);
	}

	mGTIA.SetForcedConsoleSwitches(0xF);
	mpUIRenderer->SetHeldButtonStatus(0);
}

void ATSimulator::SetupAutoHeldButtonHook() {
	if (!mpCPUHookInitHook)
		mpCPUHookInitHook = mpCPUHookManager->AddInitHook([this](const uint8 *, const uint8 *) { SetupAutoHeldButtons(); });
}

void ATSimulator::SetupAutoHeldButtons() {
	if (mStartupDelay)
		return;

	// press option during power on if BASIC is disabled
	// press start during power on if cassette auto-boot is enabled
	uint8 consoleSwitches = 0x0F;

	mStartupDelay = 1;
	mStartupDelay2 = 150;
	mbStartupHeldKey = false;

	if (SupportsInternalBasic()) {
		// If the kernel is being externally overridden, the kernel flags we have are no
		// good as they pertain to the unoverridden base ROM.
		const bool holdOptionForBasic = (mActualKernelFlags & mKernelFlagsMask & kATFirmwareFlags_InvertOPTION) != 0;

		if (mbBASICEnabled == holdOptionForBasic) {
			if (!mpCartridge[0] || mpCartridge[0]->IsBASICDisableAllowed()) {
				// Don't hold OPTION for a diagnostic cart.
				if (!mpCartridge[0] || DebugReadByte(0xBFFD) < 0x80)
					consoleSwitches &= ~4;
			}
		}
	}

	if (mpCassette->IsLoaded() && mbCassetteAutoBootEnabled && !mbCassetteAutoBootUseBasic) {
		consoleSwitches &= ~1;
		mStartupDelay = 5;
	}

	mGTIA.SetForcedConsoleSwitches(consoleSwitches);
	mpUIRenderer->SetHeldButtonStatus(~consoleSwitches);
}

void ATSimulator::InitDevice(IVDUnknown& dev) {
	if (auto devmm = vdpoly_cast<IATDeviceMemMap *>(&dev))
		devmm->InitMemMap(GetMemoryManager());

	if (auto devfw = vdpoly_cast<IATDeviceFirmware *>(&dev))
		devfw->InitFirmware(GetFirmwareManager());

	if (auto devirq = vdpoly_cast<IATDeviceIRQSource *>(&dev))
		devirq->InitIRQSource(GetIRQController());

	if (auto devsch = vdpoly_cast<IATDeviceScheduling *>(&dev))
		devsch->InitScheduling(GetScheduler(), GetSlowScheduler());

	if (auto devin = vdpoly_cast<IATDeviceIndicators *>(&dev))
		devin->InitIndicators(GetUIRenderer());

	if (auto devaudio = vdpoly_cast<IATDeviceAudioOutput *>(&dev))
		devaudio->InitAudioOutput(&GetAudioOutput()->AsMixer());

	if (auto devportinput = vdpoly_cast<IATDevicePortInput *>(&dev))
		devportinput->InitPortInput(&GetPIA());

	if (auto devsio = vdpoly_cast<IATDeviceSIO *>(&dev))
		devsio->InitSIO(GetDeviceSIOManager());

	if (auto devcio = vdpoly_cast<IATDeviceCIO *>(&dev))
		devcio->InitCIO(GetDeviceCIOManager());

	if (auto devpr = vdpoly_cast<IATDevicePrinter *>(&dev))
		devpr->SetPrinterOutput(GetPrinterOutput());

	if (auto devpbi = vdpoly_cast<IATDevicePBIConnection *>(&dev))
		devpbi->InitPBI(mpPBIManager);

	if (auto devcart = vdpoly_cast<IATDeviceCartridge *>(&dev))
		devcart->InitCartridge(&mpPrivateData->mCartPort);

	if (auto devsyscon = vdpoly_cast<IATDeviceSystemControl *>(&dev)) {
		devsyscon->InitSystemControl(mpPrivateData);

		devsyscon->SetROMLayers(
			mpMemLayerLowerKernelROM,
			mpMemLayerUpperKernelROM,
			mpMemLayerBASICROM,
			mpMemLayerSelfTestROM,
			mpMemLayerGameROM,
			mKernelROM);
	}

	if (auto devcovoxcon = vdpoly_cast<IATDeviceCovoxControl *>(&dev))
		devcovoxcon->InitCovoxControl(*mpPrivateData);

	if (auto devdisk = vdpoly_cast<IATDeviceDiskDrive *>(&dev)) {
		devdisk->InitDiskDrive(mpPrivateData);

		for(int i=0; i<15; ++i) {
			if (mpPrivateData->GetDiskInterface(i)->GetClientCount() > 1)
				mpDiskDrives[i]->SetEnabled(false);
		}
	}

	if (auto devvbxe = vdpoly_cast<IATVBXEDevice *>(&dev)) {
		devvbxe->SetSharedMemory(mMemory + 0x10000);

		auto *vbxe = vdpoly_cast<ATVBXEEmulator *>(devvbxe);

		if (!mpVBXE) {
			mpVBXE = vbxe;
			++mConfigChangeCounter;

			mGTIA.SetVBXE(vbxe);
		}

		vbxe->Set5200Mode(mHardwareMode == kATHardwareMode_5200);
	}
}

void ATSimulator::ResetMemoryBuffer(void *dst, size_t len, uint32 seed) {
	switch(mMemoryClearMode) {
		case kATMemoryClearMode_Zero:
			memset(dst, 0, len);
			break;

		case kATMemoryClearMode_Random:
			ATRandomizeMemory(dst, len, seed);
			break;

		case kATMemoryClearMode_DRAM1:
			if (mHardwareMode == kATHardwareMode_5200)
				DRAMPatternFillA_5200(dst, len);
			else
				DRAMPatternFillA(dst, len);
			break;

		case kATMemoryClearMode_DRAM2:
			DRAMPatternFillB(dst, len);
			break;

		case kATMemoryClearMode_DRAM3:
			DRAMPatternFillC(dst, len);
			break;
	}
}

void ATSimulator::InitCassetteAutoBasicBoot() {
	if (mbCassetteAutoBootEnabled) {
		mbCassetteAutoBootUseBasic = false;

		if (mbCassetteAutoBasicBootEnabled && mpCassette) {
			const auto needBasic = mpCassette->AutodetectBasicNeeded();

			if (needBasic.has_value()) {
				const bool useBasic = needBasic.value();

				mbCassetteAutoBootUseBasic = useBasic;

				// Toggle the BASIC enable state.
				SetBASICEnabled(useBasic);

				// Check if we're in a hardware mode that supports internal BASIC. If not, we need
				// to also insert or remove the BASIC cartridge.
				const bool alreadyHasBasic = mpCartridge[0] && wcscmp(mpCartridge[0]->GetPath(), L"special:basic") == 0;

				if (!SupportsInternalBasic()) {
					// We need to insert or remove the cart. Check if there is already a cart
					// inserted that is modified. If so, we should not automatically remove it
					// as this would cause data loss.
					if (useBasic != alreadyHasBasic) {
						if (!mpCartridge[0] || !mpCartridge[0]->IsDirty()) {
							if (useBasic)
								LoadCartridgeBASIC();
							else
								UnloadCartridge(0);
						}
					}
				} else if (alreadyHasBasic && !useBasic) {
					UnloadCartridge(0);
				}

				if (useBasic) {
					mPokey.ClearKeyQueue();
				}
			}
		}
	}
}

bool ATSimulator::SupportsInternalBasic() const {
	switch(mHardwareMode) {
		case kATHardwareMode_1200XL:
			// hack -- Ultimate1MB takes over MMU, so it can do BASIC
			return mpUltimate1MB != nullptr;

		case kATHardwareMode_800XL:
		case kATHardwareMode_XEGS:
		case kATHardwareMode_130XE:
			return true;

		default:
			return false;
	}
}
