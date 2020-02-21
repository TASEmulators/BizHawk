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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/cio.h>
#include <at/atcore/devicecio.h>
#include "cpu.h"
#include "cpumemory.h"
#include "cpuhookmanager.h"
#include "memorymanager.h"
#include "cassette.h"
#include "simulator.h"
#include "hostdevice.h"
#include "virtualscreen.h"
#include "kerneldb.h"
#include "cio.h"
#include "hleciohook.h"

class ATHLECIOHook final : public IATHLECIOHook, public IATDeviceCIOManager {
public:
	ATHLECIOHook();
	~ATHLECIOHook();

	void *AsInterface(uint32 id) override;

	void Init(ATCPUEmulator *cpu, ATSimulator *sim, ATMemoryManager *memman);
	void Shutdown();

	void WarmReset();

	bool HasCIODevice(char c) const override;
	bool GetBurstTransfersEnabled() const override;
	void SetBurstTransfersEnabled(bool enabled) override;

	bool GetCIOPatchEnabled(char c) const override;
	void SetCIOPatchEnabled(char c, bool enabled) override;

	bool IsHookPageNeeded() const;

	virtual void ReinitHooks(uint8 hookPage) override;
	void UninitHooks();

public:
	void AddCIODevice(IATDeviceCIO *dev) override;
	void RemoveCIODevice(IATDeviceCIO *dev) override;
	void NotifyCIODevicesChanged(IATDeviceCIO *dev) override;
	size_t ReadFilename(uint8 *buf, size_t buflen, uint16 filenameAddr) override;
	void ReadMemory(void *buf, uint16 addr, uint16 len) override;
	void WriteMemory(uint16 addr, const void *buf, uint16 len) override;
	uint8 ReadByte(uint16 addr) override;
	void WriteByte(uint16 addr, uint8 value) override;
	bool IsBreakActive() const override;

protected:
	uint8 OnHookGeneric(uint16 pc);
	uint8 HandleGenericDevice(uint16 pc, int function, uint8 deviceNameHint);
	uint8 OnHookContinuation(uint16 pc);
	uint8 OnHookVirtualScreen(uint16 pc);
	uint8 OnHookCIOV(uint16 pc);
	uint8 OnHookCIOVInit(uint16 pc);
	uint8 OnHookCIOINV(uint16 pc);
	uint8 OnHookCassetteOpen(uint16 pc);
	uint8 OnHookEditorGetChar(uint16 pc);
	uint8 OnHookEditorPutChar(uint16 pc);

	void InitHooks(const uint8 *lowerROM, const uint8 *upperROM);
	bool IsValidOSCIORoutine(const uint8 *lowerROM, const uint8 *upperROM, uint16 pc) const;
	void RebuildCIODeviceList();
	void RegisterCIODevices();
	void UpdateHookPage();
	void CloseAllIOCBs();
	void AbortPendingCommand(IATDeviceCIO *dev);

	ATCPUEmulator *mpCPU;
	ATSimulator *mpSim;
	ATMemoryManager *mpMemMan;

	uint32	mCIOPatchMask;
	uint8	mHookPage;
	bool	mbCIOHandlersEstablished;
	bool	mbBurstIOEnabled;
	uint16	mCassetteCIOOpenHandlerHookAddress;
	uint16	mEditorCIOGetCharHandlerHookAddress;
	uint16	mEditorCIOPutCharHandlerHookAddress;
	uint16	mPrinterHookAddresses[6] = {};

	ATCPUHookInitNode *mpInitHook;
	ATCPUHookResetNode *mpResetHook;

	ATCPUHookNode *mpDeviceRoutineHooks[24];
	ATCPUHookNode *mpCIOVHook;
	ATCPUHookNode *mpCIOVInitHook;
	ATCPUHookNode *mpCIOINVHook;
	ATCPUHookNode *mpCSOPIVHook;
	ATCPUHookNode *mpCassetteOpenHook;
	ATCPUHookNode *mpEditorGetCharHook;
	ATCPUHookNode *mpEditorPutCharHook;
	ATCPUHookNode *mpContinuationHook;

	ATMemoryLayer	*mpMemLayerHook;

	vdfunction<sint32()> mpContinuationFn;
	IATDeviceCIO *mpActiveDevice;
	uint8	mActiveCommand;
	uint8	mActiveCommandAUX[6];
	uint8	mActiveChannel;
	uint8	mActiveDeviceNo;
	uint8	mActiveDeviceName;
	uint16	mActiveBufferAddr;
	uint16	mActiveBufferLength;
	uint32	mActiveActualLength;
	uint32	mActiveTransferLength;

	vdfastvector<uint8> mLargeTransferBuffer;

	vdfastvector<uint8> mNewRegisteredDevices;
	vdfastvector<uint8> mRegisteredDevices;
	vdfastvector<IATDeviceCIO *> mCIODevices;
	
	struct ExtendedIOCB {
		IATDeviceCIO *mpDevice;
		uint8 mDeviceNo;
		uint8 mName;
	};

	ExtendedIOCB mExtIOCBs[8];

	IATDeviceCIO *mCIODeviceMap[256];
	IATDeviceCIO *mCIODeviceMapNew[256];

	uint8	mTransferBuffer[1024];

	VDALIGN(4)	uint8	mHookROM[0x100];

	static const char kStandardNames[];
};

const char ATHLECIOHook::kStandardNames[]="ESKPC";

ATHLECIOHook::ATHLECIOHook()
	: mpCPU(NULL)
	, mpSim(NULL)
	, mpMemMan(nullptr)
	, mCassetteCIOOpenHandlerHookAddress(0)
	, mEditorCIOGetCharHandlerHookAddress(0)
	, mEditorCIOPutCharHandlerHookAddress(0)
	, mCIOPatchMask(1U << ('P' - 'A'))
	, mHookPage(0)
	, mbCIOHandlersEstablished(false)
	, mbBurstIOEnabled(true)
	, mpInitHook(nullptr)
	, mpCIOVHook(NULL)
	, mpCIOVInitHook(NULL)
	, mpCIOINVHook(NULL)
	, mpCSOPIVHook(NULL)
	, mpCassetteOpenHook(NULL)
	, mpEditorGetCharHook(NULL)
	, mpEditorPutCharHook(NULL)
	, mpContinuationHook(nullptr)
	, mpMemLayerHook(nullptr)
	, mpContinuationFn()
	, mpActiveDevice(nullptr)
{
	for(auto& hook : mpDeviceRoutineHooks)
		hook = nullptr;

	for(auto& dev : mCIODeviceMap)
		dev = nullptr;

	for(auto& xiocb : mExtIOCBs)
		xiocb.mpDevice = nullptr;
}

ATHLECIOHook::~ATHLECIOHook() {
	Shutdown();
}

void *ATHLECIOHook::AsInterface(uint32 id) {
	if (id == IATDeviceCIOManager::kTypeID)
		return static_cast<IATDeviceCIOManager *>(this);

	return nullptr;
}

void ATHLECIOHook::Init(ATCPUEmulator *cpu, ATSimulator *sim, ATMemoryManager *memman) {
	mpCPU = cpu;
	mpSim = sim;
	mpMemMan = memman;

	mpInitHook = cpu->GetHookManager()->AddInitHook([this](const uint8 *lower, const uint8 *upper) { InitHooks(lower, upper); });
	mpResetHook = cpu->GetHookManager()->AddResetHook([this] { WarmReset(); });

	ReinitHooks(0);
}

void ATHLECIOHook::Shutdown() {
	if (mpCPU) {
		WarmReset();

		if (mpInitHook) {
			mpCPU->GetHookManager()->RemoveInitHook(mpInitHook);
			mpInitHook = nullptr;
		}

		if (mpResetHook) {
			mpCPU->GetHookManager()->RemoveResetHook(mpResetHook);
			mpResetHook = nullptr;
		}

		mpMemMan = nullptr;
		mpCPU = nullptr;
	}
}

void ATHLECIOHook::WarmReset() {
	AbortPendingCommand(nullptr);

	mCassetteCIOOpenHandlerHookAddress = 0;
	mEditorCIOPutCharHandlerHookAddress = 0;
	mEditorCIOGetCharHandlerHookAddress = 0;

	CloseAllIOCBs();

	mbCIOHandlersEstablished = false;

	UninitHooks();
}

bool ATHLECIOHook::GetBurstTransfersEnabled() const {
	return mbBurstIOEnabled;
}

void ATHLECIOHook::SetBurstTransfersEnabled(bool enabled) {
	mbBurstIOEnabled = enabled;
}

bool ATHLECIOHook::HasCIODevice(char c) const {
	return mCIODeviceMap[(unsigned char)c] != nullptr;
}

bool ATHLECIOHook::GetCIOPatchEnabled(char c) const {
	if (c >= 'A' && c <= 'Z')
		return (mCIOPatchMask & (1 << (c - 'A'))) != 0;
	else
		return false;
}

void ATHLECIOHook::SetCIOPatchEnabled(char c, bool enabled) {
	if (c >= 'A' && c <= 'Z') {
		const uint32 bit = 1 << (c - 'A');
		const uint32 newState = enabled ? bit : 0;

		if ((mCIOPatchMask ^ newState) & bit) {
			mCIOPatchMask ^= bit;

			if (c == 'P')
				ReinitHooks(mHookPage);
		}

		if (enabled)
			mCIOPatchMask |= bit;
		else
			mCIOPatchMask &= ~bit;
	}
}

bool ATHLECIOHook::IsHookPageNeeded() const {
	IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();
	if (vs)
		return true;

	if (!mRegisteredDevices.empty())
		return true;

	return false;
}

void ATHLECIOHook::ReinitHooks(uint8 hookPage) {
	// bail if we're called before init... we'll do this at init time anyway
	if (!mpCPU)
		return;

	UninitHooks();

	mHookPage = hookPage;

	ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();
	
	IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();

	const bool genericDevs = !mCIODevices.empty();

	if (vs)
		vs->SetHookPage(hookPage);

	if (mHookPage) {
		const uint16 hookPageBase = (uint16)mHookPage << 8;

		if (genericDevs) {
			// H:
			for(int i=0; i<6; ++i)
				hookmgr.SetHookMethod(mpDeviceRoutineHooks[i], kATCPUHookMode_Always, hookPageBase + 0x71 + 2*i, 0, this, &ATHLECIOHook::OnHookGeneric);
		}

		// virtual screen
		if (vs) {
			for(int i=0; i<6; ++i)
				hookmgr.SetHookMethod(mpDeviceRoutineHooks[i+12], kATCPUHookMode_Always, hookPageBase + 0x51 + 2*i, 0, this, &ATHLECIOHook::OnHookVirtualScreen);
		}

		hookmgr.SetHookMethod(mpContinuationHook, kATCPUHookMode_Always, hookPageBase + 0x7F, 0, this, &ATHLECIOHook::OnHookContinuation);
	}

	// printer
	if (mCIODeviceMap['P'] && (mCIOPatchMask & (1 << ('P' - 'A')))) {
		for(int i=0; i<6; ++i) {
			if (!mPrinterHookAddresses[i])
				continue;

			hookmgr.SetHook(mpDeviceRoutineHooks[i+18], kATCPUHookMode_KernelROMOnly, mPrinterHookAddresses[i], 0,
				[i,this](uint16 pc) {
					return HandleGenericDevice(pc, i * 2, 'P');
				}
			);
		}
	}

	// CIO
	if (genericDevs || vs)
		hookmgr.SetHookMethod(mpCIOVHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CIOV, 0, this, &ATHLECIOHook::OnHookCIOV);

	if (genericDevs) {
		hookmgr.SetHookMethod(mpCIOVInitHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CIOV, 1, this, &ATHLECIOHook::OnHookCIOVInit);
		hookmgr.SetHookMethod(mpCIOINVHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CIOINV, 0, this, &ATHLECIOHook::OnHookCIOINV);
	}

	// Cassette hooks
	if (mpSim->IsCassetteSIOPatchEnabled()) {
		ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();

		hookmgr.SetHookMethod(mpCSOPIVHook, kATCPUHookMode_KernelROMOnly, ATKernelSymbols::CSOPIV, 0, this, &ATHLECIOHook::OnHookCassetteOpen);

		if (mCassetteCIOOpenHandlerHookAddress)
			hookmgr.SetHookMethod(mpCassetteOpenHook, kATCPUHookMode_KernelROMOnly, mCassetteCIOOpenHandlerHookAddress, 0, this, &ATHLECIOHook::OnHookCassetteOpen);
	}

	if (vs && mHookPage) {
		vs->SetGetCharAddress(mEditorCIOGetCharHandlerHookAddress);

		if (mEditorCIOPutCharHandlerHookAddress)
			hookmgr.SetHookMethod(mpEditorPutCharHook, kATCPUHookMode_KernelROMOnly, mEditorCIOPutCharHandlerHookAddress, 0, this, &ATHLECIOHook::OnHookEditorPutChar);

		if (mEditorCIOGetCharHandlerHookAddress)
			hookmgr.SetHookMethod(mpEditorGetCharHook, kATCPUHookMode_KernelROMOnly, mEditorCIOGetCharHandlerHookAddress, 0, this, &ATHLECIOHook::OnHookEditorGetChar);
	}

	// Hook layer
	if (mHookPage && IsHookPageNeeded()) {
		UpdateHookPage();

		// check if we are overlaying the PIA
		if (mHookPage == 0xD3) {
			ATMemoryHandlerTable handlers = {};

			handlers.mpDebugReadHandler = [](void *thisptr, uint32 addr) -> sint32 {
				if ((addr - 0xD340) < 0x40)
					return ((uint8 *)thisptr)[addr - 0xD340];
				return -1;
			};

			handlers.mpReadHandler = handlers.mpDebugReadHandler;
			handlers.mpWriteHandler = nullptr;
			handlers.mbPassAnticReads = true;
			handlers.mbPassReads = true;
			handlers.mbPassWrites = false;
			handlers.mpThis = mHookROM + 0x40;

			mpMemLayerHook = mpMemMan->CreateLayer(kATMemoryPri_HardwareOverlay, handlers, 0xD3, 0x01);
			mpMemMan->SetLayerName(mpMemLayerHook, "CIO device hook ROM (PIA overlay)");
			mpMemMan->EnableLayer(mpMemLayerHook, kATMemoryAccessMode_CPURead, true);
		} else {
			mpMemLayerHook = mpMemMan->CreateLayer(kATMemoryPri_ROM, mHookROM, mHookPage, 0x01, true);
			mpMemMan->SetLayerName(mpMemLayerHook, "CIO device hook ROM");
			mpMemMan->EnableLayer(mpMemLayerHook, kATMemoryAccessMode_CPURead, true);
		}
	}
}

void ATHLECIOHook::UninitHooks() {
	ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();

	hookmgr.UnsetHook(mpEditorPutCharHook);
	hookmgr.UnsetHook(mpEditorGetCharHook);
	hookmgr.UnsetHook(mpCassetteOpenHook);
	hookmgr.UnsetHook(mpCSOPIVHook);
	hookmgr.UnsetHook(mpCIOVInitHook);
	hookmgr.UnsetHook(mpCIOVHook);
	hookmgr.UnsetHook(mpCIOINVHook);
	hookmgr.UnsetHook(mpContinuationHook);

	for(auto& hook : mpDeviceRoutineHooks)
		hookmgr.UnsetHook(hook);

	if (mpMemLayerHook) {
		mpMemMan->DeleteLayer(mpMemLayerHook);
		mpMemLayerHook = nullptr;
	}
}

void ATHLECIOHook::AddCIODevice(IATDeviceCIO *dev) {
	VDASSERT(dev);

	auto it = std::lower_bound(mCIODevices.begin(), mCIODevices.end(), dev);
	if (it == mCIODevices.end() || *it != dev) {
		mCIODevices.insert(it, dev);
		RebuildCIODeviceList();
		ReinitHooks(mHookPage);
	} else
		VDASSERT(false);
}

void ATHLECIOHook::RemoveCIODevice(IATDeviceCIO *dev) {
	if (dev) {
		AbortPendingCommand(dev);

		auto it = std::find(mCIODevices.begin(), mCIODevices.end(), dev);

		if (it != mCIODevices.end()) {
			mCIODevices.erase(it);

			for(int i=0; i<8; ++i) {
				ExtendedIOCB& xiocb = mExtIOCBs[i];

				if (xiocb.mpDevice == dev) {
					xiocb.mpDevice->OnCIOClose(i, xiocb.mDeviceNo);
					xiocb.mpDevice = nullptr;
				}
			}

			RebuildCIODeviceList();
			ReinitHooks(mHookPage);
		}
	}
}

void ATHLECIOHook::NotifyCIODevicesChanged(IATDeviceCIO *dev) {
	VDASSERT(std::binary_search(mCIODevices.begin(), mCIODevices.end(), dev));

	RebuildCIODeviceList();
}

size_t ATHLECIOHook::ReadFilename(uint8 *buf, size_t buflen, uint16 filenameAddr) {
	auto& mem = *mpCPU->GetMemory();
	uint32 n = 0;

	while(n + 1 < buflen) {
		uint8 c = mem.ReadByte(filenameAddr + n);

		// The original CIO specification in the OS manual says that paths are supposed to
		// terminated by an EOL character, but this is pretty widely violated. The most common
		// offender is a null byte ($00), but MultiBASIC 0.33 also has a bug where it fails to
		// terminate strings passed to DIR and frequently returns the end of statement ($14)
		// or end of line ($16) tokens instead. Just end on any non-printable character.
		if (c < 0x20 || c > 0x7F)
			break;

		*buf++ = c;
		++n;
	}

	*buf = 0;

	return n;
}

void ATHLECIOHook::ReadMemory(void *buf0, uint16 addr, uint16 len) {
	uint8 *buf = (uint8 *)buf0;
	auto& mem = *mpCPU->GetMemory();

	while(len--)
		*buf++ = mem.ReadByte(addr++);
}

void ATHLECIOHook::WriteMemory(uint16 addr, const void *buf0, uint16 len) {
	auto& mem = *mpCPU->GetMemory();
	const uint8 *buf = (const uint8 *)buf0;

	while(len--)
		mem.WriteByte(addr++, *buf++);
}

uint8 ATHLECIOHook::ReadByte(uint16 addr) {
	auto& mem = *mpCPU->GetMemory();

	return mem.ReadByte(addr);
}

void ATHLECIOHook::WriteByte(uint16 addr, uint8 value) {
	auto& mem = *mpCPU->GetMemory();

	mem.WriteByte(addr, value);
}

bool ATHLECIOHook::IsBreakActive() const {
	return !mpCPU->GetMemory()->ReadByte(ATKernelSymbols::BRKKEY);
}

uint8 ATHLECIOHook::OnHookGeneric(uint16 pc) {
	const uint8 function = (uint8)pc & 14;

	return HandleGenericDevice(pc, function, 0);
}

uint8 ATHLECIOHook::HandleGenericDevice(uint16 pc, int function, uint8 deviceNameHint) {
	// first, validate the IOCB
	const uint8 iocb = mpCPU->GetX();
	if (iocb & 0x8f) {
		mpCPU->Ldy(kATCIOStat_UnkDevice);
		return 0x60;
	}
	
	// look up the extended IOCB
	const int channel = iocb >> 4;
	ExtendedIOCB& xiocb = mExtIOCBs[channel];

	// check for OPEN
	sint32 result = kATCIOStat_NotSupported;
	auto *mem = mpCPU->GetMemory();
	ATKernelDatabase kdb(mem);

	if (function == 0) {
		// pull filename
		const uint16 addr = kdb.ICBAZ;
		const uint8 name = mem->ReadByte(addr);

		IATDeviceCIO *dev = mCIODeviceMap[name];
		if (!dev) {
			// we don't know this device (weird)
			mpCPU->Ldy(kATCIOStat_UnkDevice);
			return 0x60;
		}

		// check if this is an override device -- if so, we should bail and let
		// native CIO handle it; we will soft-open later
		if (strchr(kStandardNames, (char)name)) {
			return 0;
		}

		const uint8 deviceNo = kdb.ICDNOZ;

		// We limit the filename length here so as to not pull in a ridiculous amount
		// of memory if something goes wrong.
		VDASSERTCT(sizeof(mTransferBuffer) >= 128);
		ReadFilename(mTransferBuffer, 128, addr);

		mpActiveDevice = dev;
		mActiveChannel = channel;
		mActiveDeviceNo = deviceNo;
		mActiveDeviceName = name;
		mActiveCommandAUX[0] = kdb.ICAX1Z;
		mActiveCommandAUX[1] = kdb.ICAX2Z;

		mpContinuationFn = [this]() -> sint32 {
			sint32 result = mpActiveDevice->OnCIOOpen(mActiveChannel, mActiveDeviceNo, mActiveCommandAUX[0], mActiveCommandAUX[1], mTransferBuffer);

			if (result >= 0) {
				ExtendedIOCB& xiocb = mExtIOCBs[mActiveChannel];
				xiocb.mpDevice = mpActiveDevice;
				xiocb.mName = mActiveDeviceName;
				xiocb.mDeviceNo = mActiveDeviceNo;
			}

			return result;
		};
	} else {
		IATDeviceCIO *dev = xiocb.mpDevice;
		uint8 deviceNo = xiocb.mDeviceNo;
		bool overrideDevice = false;

		// it must be a function that's executed on an open IOCB;
		if (!dev) {
			// Uh oh -- there is no device associated with this XIOCB.
			// This may mean it got removed, or it may have already been
			// open. Check if we are overriding a standard device -- if so,
			// attempt a soft open. This allows us to handle P: after
			// P: has already been opened, for instance.
			//
			// Note that the SpartaDOS X PRN: device does some pretty
			// nasty stuff -- it calls directly into P: entry points
			// with a modified E: device whose device number has been
			// hacked to the required P: device number, but with the
			// device ID still set to E:.
			uint8 name = deviceNameHint;
			if (!name) {
				const uint8 id = (function == 6) ? mem->ReadByte(ATKernelSymbols::ICHID + iocb) : mem->ReadByte(ATKernelSymbols::ICHIDZ);

				if (id < 36)
					name = mem->ReadByte(ATKernelSymbols::HATABS + id);
			}

			IATDeviceCIO *softdev = mCIODeviceMap[name];
			if (function == 8 || function == 10) {
				// GET STATUS and SPECIAL can be called via soft open in CIO.
				dev = softdev;

				if (!dev) {
					mpCPU->Ldy(kATCIOStat_UnkDevice);
					return 0x60;
				}
			} else if (softdev && strchr(kStandardNames, (char)name)) {
				dev = softdev;
				overrideDevice = true;
			} else {
				if (deviceNameHint) {
					return 0;
				} else {
					mpCPU->Ldy(kATCIOStat_UnkDevice);
					return 0x60;
				}
			}

			deviceNo = (function == 6) ? mem->ReadByte(ATKernelSymbols::ICDNO + iocb) : mem->ReadByte(ATKernelSymbols::ICDNOZ);
		}

		// if this is an override device, reject calls not to GET BYTE, PUT BYTE, or
		// STATUS
		if (overrideDevice) {
			switch(function) {
				case 4:
				case 6:
				case 8:
					break;

				default:
					return 0;
			}
		}

		// dispatch function
		switch(function) {
			case 2:		// close
				result = dev->OnCIOClose(channel, deviceNo);

				if (result < 0) {
					mpCPU->PushWord(pc-1);
					mpCPU->PushWord(0xE4C0-1);
					return 0x60;
				}

				xiocb.mpDevice = nullptr;
				break;

			case 4:		// get byte
				// check if we can burst -- reqs:
				//	- raw I/O, not record
				//	- caller must be >=$C000

				mpActiveDevice = dev;
				mActiveChannel = channel;
				mActiveDeviceNo = deviceNo;

				if (mbBurstIOEnabled && kdb.ICCOMZ == 0x07 && mem->ReadByte(0x100 + (uint8)(mpCPU->GetS() + 2)) >= 0xC0) {
					uint16 len = kdb.ICBLZ;

					// zero bytes is a special case meaning to return one byte in the A register
					if (len) {
						uint32 tc = len;

						if (tc > vdcountof(mTransferBuffer))
							tc = vdcountof(mTransferBuffer);

						mActiveBufferAddr = kdb.ICBAZ;
						mActiveBufferLength = len;
						mActiveTransferLength = tc;
						mActiveActualLength = 0;

						mpContinuationFn = [this]() -> sint32 {
							uint32 actual = 0;
							sint32 result = mpActiveDevice->OnCIOGetBytes(mActiveChannel, mActiveDeviceNo, mTransferBuffer + mActiveActualLength, mActiveTransferLength - mActiveActualLength, actual);

							mActiveActualLength += actual;

							if (result >= 0) {
								if (mActiveActualLength) {
									uint32 actualm1 = mActiveActualLength - 1;

									WriteMemory(mActiveBufferAddr, mTransferBuffer, actualm1);

									ATKernelDatabase kdb(mpCPU->GetMemory());
									kdb.ICBAZ = (uint16)(mActiveBufferAddr + actualm1);
									kdb.ICBLZ = (uint16)(mActiveBufferLength - actualm1);

									mpCPU->SetA(mTransferBuffer[actualm1]);
								} else
									mpCPU->SetA(0);
							}

							return result;
						};

						break;
					}
				}

				mpContinuationFn = [this]() -> sint32 {
					uint8 c;
					uint32 actual;

					sint32 result = mpActiveDevice->OnCIOGetBytes(mActiveChannel, mActiveDeviceNo, &c, 1, actual);
					if (result >= 0)
						mpCPU->SetA(c);

					return result;
				};

				break;

			case 6:		// put byte
				// check if we can burst -- reqs:
				//	- raw I/O, not record
				//	- caller must be >=$C000

				mpActiveDevice = dev;
				mActiveChannel = channel;
				mActiveDeviceNo = deviceNo;
				
				if (mbBurstIOEnabled && kdb.ICCOMZ == 0x0B && mem->ReadByte(0x100 + (uint8)(mpCPU->GetS() + 2)) >= 0xC0) {
					uint16 len = kdb.ICBLZ;

					// zero bytes is a special case meaning to write one byte from the A register
					if (len) {
						uint32 tc = len;

						if (tc > vdcountof(mTransferBuffer))
							tc = vdcountof(mTransferBuffer);

						mTransferBuffer[0] = mpCPU->GetA();
						mActiveBufferAddr = kdb.ICBAZ;
						mActiveBufferLength = len;
						mActiveTransferLength = tc;
						mActiveActualLength = 0;

						ReadMemory(mTransferBuffer + 1, mActiveBufferAddr + 1, mActiveTransferLength - 1);

						mpContinuationFn = [this]() -> sint32 {
							uint32 actual = 0;
							sint32 result = mpActiveDevice->OnCIOPutBytes(mActiveChannel, mActiveDeviceNo, mTransferBuffer + mActiveActualLength, mActiveTransferLength - mActiveActualLength, actual);

							mActiveActualLength += actual;

							if (result >= 0 && mActiveActualLength > 1) {
								ATKernelDatabase kdb(mpCPU->GetMemory());
								kdb.ICBAZ = (uint16)(mActiveBufferAddr + (mActiveActualLength - 1));
								kdb.ICBLZ = (uint16)(mActiveBufferLength - (mActiveActualLength - 1));
							}

							return result;
						};

						break;
					}
				}

				mTransferBuffer[0] = mpCPU->GetA();

				mpContinuationFn = [this]() -> sint32 {
					uint32 actual;

					return mpActiveDevice->OnCIOPutBytes(mActiveChannel, mActiveDeviceNo, mTransferBuffer, 1, actual);
				};

				break;

			case 8:		// status
				for(int i=0; i<4; ++i)
					mTransferBuffer[i] = mem->ReadByte(ATKernelSymbols::DVSTAT + i);

				mpActiveDevice = dev;
				mActiveChannel = channel;
				mActiveDeviceNo = deviceNo;

				mpContinuationFn = [this]() -> sint32 {
					auto *mem = mpCPU->GetMemory();
					sint32 result = mpActiveDevice->OnCIOGetStatus(mActiveChannel, mActiveDeviceNo, mTransferBuffer);

					if (result >= 0) {
						for(int i=0; i<4; ++i)
							mem->WriteByte(ATKernelSymbols::DVSTAT + i, mTransferBuffer[i]);
					}

					return result;
				};
				break;

			case 10:	// special
				// only AUX1-2 are copied to ZIOCB; we must get 3-6 from the originating
				// IOCB
				mActiveCommandAUX[0] = kdb.ICAX1Z;
				mActiveCommandAUX[1] = kdb.ICAX2Z;

				for(int i=0; i<4; ++i)
					mActiveCommandAUX[i+2] = mem->ReadByte(ATKernelSymbols::ICAX1 + i + 2 + (channel << 4));

				mpActiveDevice = dev;
				mActiveChannel = channel;
				mActiveDeviceNo = deviceNo;
				mActiveCommand = kdb.ICCOMZ;
				mActiveBufferAddr = kdb.ICBAZ;
				mActiveBufferLength = kdb.ICBLZ;

				mpContinuationFn = [this]() -> sint32 {
					sint32 result = mpActiveDevice->OnCIOSpecial(mActiveChannel, mActiveDeviceNo, mActiveCommand, mActiveBufferAddr, mActiveBufferLength, mActiveCommandAUX);

					if (result >= 0) {
						auto *mem = mpCPU->GetMemory();

						// copy aux bytes back
						ATKernelDatabase kdb(mem);
						kdb.ICAX1Z = mActiveCommandAUX[0];
						kdb.ICAX2Z = mActiveCommandAUX[1];

						for(int i=0; i<4; ++i)
							mem->WriteByte(ATKernelSymbols::ICAX1 + i + 2 + (mActiveChannel << 4), mActiveCommandAUX[i+2]);
					}

					return result;
				};

				break;

			default:
				// can't happen -- we don't install a hook for this
				VDNEVERHERE;
				break;
		}
	}

	if (mpContinuationFn)
		return OnHookContinuation(0);

	mpCPU->Ldy(result);
	return 0x60;
}

uint8 ATHLECIOHook::OnHookContinuation(uint16) {
	if (!mpContinuationFn) {
		mpCPU->Ldy(kATCIOStat_UnkDevice);
		return 0x60;
	}

	sint32 r = mpContinuationFn();

	if (r < 0) {
		mpCPU->PushWord((uint16)(((uint32)mHookPage << 8) + 0x7F - 1));
		mpCPU->PushWord(0xE4C0 - 1);
		return 0x60;
	}

	mpContinuationFn = nullptr;

	mpCPU->Ldy((uint8)r);

	return 0x60;
}

uint8 ATHLECIOHook::OnHookVirtualScreen(uint16 pc) {
	IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();

	if (!vs)
		return 0;

	vs->OnCIOVector(mpCPU, mpCPU->GetMemory(), pc & 14);
	return 0x60;
}

uint8 ATHLECIOHook::OnHookCIOVInit(uint16 pc) {
	return OnHookCIOINV(pc);
}

uint8 ATHLECIOHook::OnHookCIOINV(uint16 pc) {
	ATCPUEmulatorMemory *mem = mpCPU->GetMemory();

	ATCPUHookManager& hookmgr = *mpCPU->GetHookManager();
	hookmgr.UnsetHook(mpCIOVInitHook);

	// Check if HATABS has been initialized. If it hasn't (OS-B), we have to rely on
	// the CIOV hook to do this instead.
	if (mem->ReadByte(ATKernelSymbols::HATABS)) {
		mbCIOHandlersEstablished = true;

		mNewRegisteredDevices.swap(mRegisteredDevices);
		mRegisteredDevices.clear();
		RegisterCIODevices();
	}

	return 0;
}

uint8 ATHLECIOHook::OnHookCIOV(uint16 pc) {
	// If we haven't hooked HATABS yet, do so now. This is necessary for OS-B, which inits
	// HATABS last.
	if (!mbCIOHandlersEstablished) {
		mbCIOHandlersEstablished = true;

		mNewRegisteredDevices.swap(mRegisteredDevices);
		mRegisteredDevices.clear();
		RegisterCIODevices();
	}

	ATCPUEmulatorMemory *mem = mpCPU->GetMemory();

	// validate IOCB index
	const uint8 iocbIdx = mpCPU->GetX();
	if (iocbIdx & 0x8F) {
		// invalid IOCB - punt to native CIO
		return 0;
	}

	// check character in HATABS and see what device is involved
	ATKernelDatabase kdb(mem);
	const uint8 devid = kdb.ICHID[iocbIdx];
	if (devid >= 36) {
		// not open or provisionally open -- punt
		return 0;
	}

	// look up XIOCB and see if we have this device
	ExtendedIOCB& xiocb = mExtIOCBs[iocbIdx >> 4];

	if (!xiocb.mpDevice) {
		// look up device name
		const uint8 devname = kdb.HATABS[devid];

		// see if we should accelerate this device
		if (devname >= 'A' && devname <= 'Z') {
			if (!(mCIOPatchMask & (1U << (devname - 'A'))))
				return 0;
		}

		// check if this is a standard device name -- we should not attempt to accelerate
		// these as we need cooperation of the native device
		IATDeviceCIO *softdev = mCIODeviceMap[devname];

		if (softdev && strchr(kStandardNames, devname))
			return 0;
	}

	if (xiocb.mpDevice) {
		bool accelok = true;

		if (xiocb.mName >= 'A' && xiocb.mName <= 'Z') {
			if (!(mCIOPatchMask & (1U << (xiocb.mName - 'A'))))
				accelok = false;
		}

		uint8 cmd = kdb.ICCMD[iocbIdx];

		if (accelok && (cmd == kATCIOCmd_PutRecord || cmd == kATCIOCmd_PutChars)) {
			// check permissions
			const uint8 perms = kdb.ICAX1;

			if (!(perms & 0x08)) {
				// no write permissions -- punt
				return 0;
			}

			// read buffer info
			uint16 addr = kdb.ICBAL[iocbIdx].r16();
			uint16 len = kdb.ICBLL[iocbIdx].r16();

			// check if the buffer crosses any dangerous regions and punt if so
			const uint32 kDangerousRegions[][2] = {
				{ 0x0000, 0x0080 },
				{ 0x0100, 0x0300 },
			};

			for(const auto& region : kDangerousRegions) {
				if ((uint32)(addr - region[0]) < region[1])
					return 0;
			}

			// copy first 12 bytes from IOCB to ZIOCB, like CIO does
			for(int i=0; i<12; ++i)
				kdb.ICCOMZ[i] = kdb.ICHID[iocbIdx + i];

			// read data into the buffer
			mLargeTransferBuffer.clear();
			mLargeTransferBuffer.resize(len);

			if (len == 0) {
				// len=0 is a special case -- pull char from A
				mLargeTransferBuffer.push_back(mpCPU->GetA());
			} else if (cmd == kATCIOCmd_PutRecord) {
				for(uint32 i=0; i<len; ++i) {
					mLargeTransferBuffer[i] = mem->ReadByte((addr + i) & 0xFFFF);
					if (mLargeTransferBuffer[i] == 0x9B) {
						mLargeTransferBuffer.resize(i + 1);
						break;
					}
				}

				// Add an additional EOL if the buffer didn't have one. This is
				// only possible if the buffer is full, and it can never be empty
				// at this point.
				if (mLargeTransferBuffer.back() != 0x9B)
					mLargeTransferBuffer.push_back(0x9B);
			} else {	// PutChars
				for(uint32 i=0; i<len; ++i)
					mLargeTransferBuffer[i] = mem->ReadByte((addr + i) & 0xFFFF);
			}

			mpActiveDevice = xiocb.mpDevice;
			mActiveChannel = iocbIdx >> 4;
			mActiveDeviceNo = xiocb.mDeviceNo;
			mActiveTransferLength = (uint32)mLargeTransferBuffer.size();
			mActiveActualLength = 0;

			mpContinuationFn = [this]() -> sint32 {
				uint32 actual = 0;
				sint32 result = mpActiveDevice->OnCIOPutBytes(mActiveChannel, mActiveDeviceNo, mLargeTransferBuffer.data() + mActiveActualLength, mActiveTransferLength - mActiveActualLength, actual);

				mActiveActualLength += actual;

				if (result < 0)
					return -1;

				// set status on IOCB and ZIOCB
				ATKernelDatabase kdb(mpCPU->GetMemory());
				const uint8 status = (uint8)result;
				kdb.ICSTA[mActiveChannel << 4] = status;
				kdb.ICSTAZ = status;

				// set last char
				if (!mLargeTransferBuffer.empty()) {
					const uint8 lastByte = mLargeTransferBuffer.back();
					kdb.CIOCHR = lastByte;
					mpCPU->SetA(lastByte);
				}

				// set status in Y and flags
				mpCPU->Ldy(status);

				// all done
				return 0x60;
			};

			return OnHookContinuation(0);
		}
	}

	// check character in HATABS and see if it's the printer or editor
	const uint8 devname = kdb.HATABS[devid];
	if (devname == 'E') {
		IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();

		if (vs) {
			const uint8 cmd = kdb.ICCMD[iocbIdx];

			switch(cmd) {
				case ATCIOSymbols::CIOCmdOpen:
					vs->OnCIOVector(mpCPU, mem, 0);
					break;

				case ATCIOSymbols::CIOCmdClose:
					vs->OnCIOVector(mpCPU, mem, 2);
					break;
			}
		}
	}

	return 0;
}

uint8 ATHLECIOHook::OnHookCassetteOpen(uint16 pc) {
	// read I/O bits in ICAX1Z
	ATKernelDatabase kdb(mpCPU->GetMemory());

	if (pc == mCassetteCIOOpenHandlerHookAddress) {
		// abort emulation if it's not an open-for-read request
		if ((kdb.ICAX1Z & 0x0C) != 0x04)
			return 0;

		// save ICAX2Z into FTYPE (IRG mode)
		kdb.FTYPE = kdb.ICAX2Z;
	}

	// press play on cassette
	ATCassetteEmulator& cassette = mpSim->GetCassette();
	cassette.Play();

	// turn motor on by clearing port A control bit 3
	kdb.PACTL &= ~0x08;

	// skip forward by nine seconds
	cassette.SkipForward(9.0f);

	// set open mode to read
	kdb.WMODE = 0;

	// set cassette buffer size to 128 bytes and mark it as empty
	kdb.BPTR = 0x80;
	kdb.BLIM = 0x80;

	// clear EOF flag
	kdb.FEOF = 0x00;

	// kill any pending character if there happens to be one
	kdb.CH = 0xFF;

	// set success
	mpCPU->Ldy(0x01);
	return 0x60;
}

uint8 ATHLECIOHook::OnHookEditorGetChar(uint16 pc) {
	IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();

	if (!vs)
		return 0;

	vs->OnCIOVector(mpCPU, mpCPU->GetMemory(), 4 /* get byte */);
	return 0x60;
}

uint8 ATHLECIOHook::OnHookEditorPutChar(uint16 pc) {
	IATVirtualScreenHandler *vs = mpSim->GetVirtualScreenHandler();

	if (!vs)
		return 0;

	vs->OnCIOVector(mpCPU, mpCPU->GetMemory(), 6 /* put byte */);
	return 0x60;
}

void ATHLECIOHook::InitHooks(const uint8 *lowerROM, const uint8 *upperROM) {
	mCassetteCIOOpenHandlerHookAddress = 0;
	mEditorCIOPutCharHandlerHookAddress = 0;
	mEditorCIOGetCharHandlerHookAddress = 0;

	for(auto& vec : mPrinterHookAddresses)
		vec = 0;

	if (upperROM) {
		// read cassette OPEN vector from kernel ROM
		uint16 openVec = VDReadUnalignedLEU16(&upperROM[ATKernelSymbols::CASETV - 0xD800]) + 1;

		if (IsValidOSCIORoutine(lowerROM, upperROM, openVec))
			mCassetteCIOOpenHandlerHookAddress = openVec;

		uint16 editorPutVec = VDReadUnalignedLEU16(&upperROM[ATKernelSymbols::EDITRV - 0xD800 + 6]) + 1;

		if (IsValidOSCIORoutine(lowerROM, upperROM, editorPutVec))
			mEditorCIOPutCharHandlerHookAddress = editorPutVec;

		uint16 editorGetVec = VDReadUnalignedLEU16(&upperROM[ATKernelSymbols::EDITRV - 0xD800 + 4]) + 1;

		if (IsValidOSCIORoutine(lowerROM, upperROM, editorGetVec))
			mEditorCIOGetCharHandlerHookAddress = editorGetVec;

		// Currently we only hook get byte, put byte and status for the printer.
		for(int i=2; i<5; ++i) {
			const uint16 prvec = VDReadUnalignedLEU16(&upperROM[ATKernelSymbols::PRINTV - 0xD800 + 2*i]) + 1;

			if (IsValidOSCIORoutine(lowerROM, upperROM, prvec))
				mPrinterHookAddresses[i] = prvec;
		}
	}

	ReinitHooks(mHookPage);
}

bool ATHLECIOHook::IsValidOSCIORoutine(const uint8 *lowerROM, const uint8 *upperROM, uint16 pc) const {
	// check if address is within kernel ROM
	if (pc < 0xC000)
		return false;

	// check if the routine is simply stubbed with RTS (not supported) or LDY+RTS
	if (pc < 0xD000) {
		if (!lowerROM)
			return false;

		const uint8 *routine = &lowerROM[pc - 0xC000];
		if (routine[0] == 0x60)
			return false;

		if (pc < 0xCFFE && lowerROM[0] == 0xA0 && lowerROM[2] == 0x60)
			return false;
	} else {
		if (pc < 0xD800)
			return false;

		const uint8 *routine = &upperROM[pc - 0xD800];

		if (routine[0] == 0x60)
			return false;

		if (routine[0] == 0xA0 && routine[2] == 0x60)
			return false;
	}

	return true;
}

void ATHLECIOHook::RebuildCIODeviceList() {
	mNewRegisteredDevices.clear();
	std::fill(std::begin(mCIODeviceMapNew), std::end(mCIODeviceMapNew), nullptr);

	for(IATDeviceCIO *dev : mCIODevices) {
		char ldevs[16] = {0};

		dev->GetCIODevices(ldevs, vdcountof(ldevs));

		for(const char c : ldevs) {
			if (!c)
				break;

			mNewRegisteredDevices.push_back((uint8)c);
			mCIODeviceMapNew[(uint8)c] = dev;
		}
	}

	std::sort(mNewRegisteredDevices.begin(), mNewRegisteredDevices.end());

	if (mNewRegisteredDevices != mRegisteredDevices) {
		RegisterCIODevices();
	}
}

void ATHLECIOHook::RegisterCIODevices() {
	if (!mbCIOHandlersEstablished) {
		mRegisteredDevices.swap(mNewRegisteredDevices);
		memcpy(mCIODeviceMap, mCIODeviceMapNew, sizeof mCIODeviceMap);
		return;
	}

	// read HATABS
	auto& mem = *mpCPU->GetMemory();
	uint8 hatabs[36];
	for(int i=0; i<36; ++i)
		hatabs[i] = mem.ReadByte(ATKernelSymbols::HATABS + i);

	uint8 iocbIDs[8];
	for(int i=0; i<8; ++i)
		iocbIDs[i] = mem.ReadByte(ATKernelSymbols::ICHID + (i << 4));

	// first, remove devices that are dead
	for(uint32 i=0; i<36; i += 3) {
		uint8 name = hatabs[i];

		if (!name)
			break;

		// skip standard names
		if (strchr(kStandardNames, (char)name))
			continue;

		// check if this device was registered
		if (!std::binary_search(mRegisteredDevices.begin(), mRegisteredDevices.end(), name)) {
			// nope -- ignore it
			continue;
		}

		// check if this device is one we want
		if (std::binary_search(mNewRegisteredDevices.begin(), mNewRegisteredDevices.end(), name)) {
			// yes -- either we already registered it, or it already
			// existed, or it's been hooked. In any case, we don't want
			// to change it.
			continue;
		}

		// check if this device is being referenced by an open IOCB -- if so,
		// we can't pull it yet
		bool referenced = false;
		for(uint8 id : iocbIDs) {
			if (id == i) {
				referenced = true;
				break;
			}
		}

		if (!referenced) {
			// zero out the entry and continue
			hatabs[i] = 0;
			hatabs[i+1] = 0;
			hatabs[i+2] = 0;
		}
	}

	// add new devices
	uint32 insertPos = 0;

	for(const uint8 name : mNewRegisteredDevices) {
		if (std::binary_search(mRegisteredDevices.begin(), mRegisteredDevices.end(), name))
			continue;

		// skip standard names
		if (strchr(kStandardNames, (char)name))
			continue;

		// try to find a free location in HATABS
		bool alreadyExists = false;

		while(hatabs[insertPos] && insertPos < 36) {
			if (hatabs[insertPos] == name) {
				alreadyExists = true;
				break;
			}

			insertPos += 3;
		}

		// bail if this name already exists
		if (alreadyExists)
			continue;

		// bail if no slots are free
		if (insertPos >= 36)
			break;

		// install handler
		hatabs[insertPos++] = name;
		hatabs[insertPos++] = 0x70;
		hatabs[insertPos++] = mHookPage;
	}

	// compact HATABS
	uint32 tail = 33;

	for(uint32 head = 0; head < tail; head += 3) {
		if (hatabs[head])
			continue;

		// we have a hole -- find last filled entry
		while(tail > head && !hatabs[tail])
			tail -= 3;

		if (tail == head)
			break;

		// move last entry into hole
		hatabs[head] = hatabs[tail];
		hatabs[head+1] = hatabs[tail+1];
		hatabs[head+2] = hatabs[tail+2];
		hatabs[tail] = 0;
		hatabs[tail+1] = 0;
		hatabs[tail+2] = 0;

		// correct any open IOCBs
		for(int i=0; i<8; ++i) {
			uint8 id = mem.ReadByte(ATKernelSymbols::ICHID + (i << 4));

			if (id == tail)
				mem.WriteByte(ATKernelSymbols::ICHID + (i << 4), head);
		}
	}

	// write HATABS back to emulated memory
	for(int i=0; i<36; ++i)
		mem.WriteByte(ATKernelSymbols::HATABS + i, hatabs[i]);

	// update registered devices
	mRegisteredDevices.swap(mNewRegisteredDevices);

	memcpy(mCIODeviceMap, mCIODeviceMapNew, sizeof mCIODeviceMap);
}

void ATHLECIOHook::UpdateHookPage() {
	// initialize hook page
	memset(mHookROM, 0xFF, 0x100);

	uint8 *dst = mHookROM;
	const uint8 page = mHookPage;

	for(uint32 i=0; i<128; i += 16) {
		// open
		*dst++ = i + 0x00;
		*dst++ = page;

		// close
		*dst++ = i + 0x02;
		*dst++ = page;

		// get byte
		*dst++ = i + 0x04;
		*dst++ = page;

		// put byte
		*dst++ = i + 0x06;
		*dst++ = page;

		// status
		*dst++ = i + 0x08;
		*dst++ = page;

		// special
		*dst++ = i + 0x0A;
		*dst++ = page;

		// init
		*dst++ = 0x4C;
		*dst++ = i + 0x0D;
		*dst++ = page;

		*dst++ = 0x60;
	}

	// New Rally X demo has a dumb bug where it jumps to $D510 and expects to execute
	// through to the floating point ROM. We throw a JMP instruction in our I/O space
	// to make this work.

	const uint8 kROM[]={
		0xEA,
		0xEA,
		0x4C,
		0x00,
		(uint8)(page+1),
	};

	memcpy(mHookROM, kROM, 5);
}

void ATHLECIOHook::CloseAllIOCBs() {
	// force close all XIOCBs
	for(int i=0; i<8; ++i) {
		ExtendedIOCB& xiocb = mExtIOCBs[i];

		if (xiocb.mpDevice) {
			xiocb.mpDevice->OnCIOClose(i, xiocb.mDeviceNo);
			xiocb.mpDevice = nullptr;
		}
	}
}

void ATHLECIOHook::AbortPendingCommand(IATDeviceCIO *dev) {
	if (!mpContinuationFn)
		return;

	if (dev && mpActiveDevice != dev) {
		mpActiveDevice->OnCIOAbortAsync();

		mpActiveDevice = nullptr;
		mpContinuationFn = nullptr;
	}
}

///////////////////////////////////////////////////////////////////////////

IATHLECIOHook *ATCreateHLECIOHook(ATCPUEmulator *cpu, ATSimulator *sim, ATMemoryManager *memmgr) {
	vdautoptr<ATHLECIOHook> hook(new ATHLECIOHook);

	hook->Init(cpu, sim, memmgr);

	return hook.release();
}

void ATDestroyHLECIOHook(IATHLECIOHook *hook) {
	delete static_cast<ATHLECIOHook *>(hook);
}
