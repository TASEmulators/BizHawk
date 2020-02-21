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

#include "stdafx.h"
#include <vd2/system/binary.h>
#include <vd2/system/registry.h>
#include <at/atcore/scheduler.h>
#include "firmwaremanager.h"
#include "warpos.h"

void ATCreateDeviceWarpOS(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefWarpOS = { "warpos", nullptr, L"APE Warp+ OS 32-in-1", ATCreateDeviceWarpOS, kATDeviceDefFlag_RebootOnPlug };

namespace {
	constexpr uint32 kBitTime = 1600;		// ~0.90ms
}

///////////////////////////////////////////////////////////////////////////

ATWarpOSDevice::ATWarpOSDevice() {
}

void *ATWarpOSDevice::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceFirmware::kTypeID: return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceSystemControl::kTypeID: return static_cast<IATDeviceSystemControl *>(this);
		case IATDevicePortInput::kTypeID: return static_cast<IATDevicePortInput *>(this);
	}

	return ATDevice::AsInterface(iid);
}

void ATWarpOSDevice::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefWarpOS;
}

void ATWarpOSDevice::Init() {
	ReloadFirmware();
	LoadNVRAM();

	UpdateKernelROM();
}

void ATWarpOSDevice::Shutdown() {
	if (mpPortManager) {
		mpPortManager->FreeInput(mPortInputIndex);
		mpPortManager->FreeOutput(mPortOutputIndex);
		mpPortManager = nullptr;
	}

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEvent);
		mpScheduler = nullptr;
	}

	mpFwMgr = nullptr;
}

void ATWarpOSDevice::ColdReset() {
	WarmReset();
}

void ATWarpOSDevice::WarmReset() {
	mpScheduler->UnsetEvent(mpEvent);
	mpPortManager->SetInput(mPortInputIndex, UINT32_C(0) - 1);
	mState = State::ShiftCommand1;

	// If Select is held, it forces the menu OS to be used.
	mCurrentSlot = mpSystemController->ReadConsoleButtons() & 0x02 ? mCurrentSetting : 0x1F;
	UpdateKernelROM();
}

void ATWarpOSDevice::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATWarpOSDevice::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMgr = fwman;
}

bool ATWarpOSDevice::ReloadFirmware() {
	const uint8 fill = 0xFF;
	bool changed = false;

	bool flashUsable = false;

	mpFwMgr->LoadFirmware(mpFwMgr->GetCompatibleFirmware(kATFirmwareType_WarpOS), mFlash, 0, sizeof mFlash, &changed, nullptr, nullptr, &fill, &flashUsable);

	if (flashUsable) {
		// Do some extra validation on the flash ROM. The last slot (slot 31) must be
		// a valid OS ROM, so check that at least its reset vector is valid.
		const uint16 resetVector = VDReadUnalignedLEU16(&mFlash[0x7FFFC]);
		if ((uint16)(resetVector - 0xC000) >= 0x1000 && (uint16)(resetVector - 0xD800) >= 0x2800)
			mFirmwareStatus = ATDeviceFirmwareStatus::Invalid;
		else
			mFirmwareStatus = ATDeviceFirmwareStatus::OK;
	} else {
		mFirmwareStatus = ATDeviceFirmwareStatus::Missing;
	}

	return changed;
}

const wchar_t *ATWarpOSDevice::GetWritableFirmwareDesc(uint32 idx) const {
	return nullptr;
}

bool ATWarpOSDevice::IsWritableFirmwareDirty(uint32 idx) const {
	return false;
}

void ATWarpOSDevice::SaveWritableFirmware(uint32 idx, IVDStream& stream) {
}

ATDeviceFirmwareStatus ATWarpOSDevice::GetFirmwareStatus() const {
	return mFirmwareStatus;
}

void ATWarpOSDevice::InitSystemControl(IATSystemController *sysctrl) {
	mpSystemController = sysctrl;
}

void ATWarpOSDevice::SetROMLayers(
	ATMemoryLayer *layerLowerKernelROM,
	ATMemoryLayer *layerUpperKernelROM,
	ATMemoryLayer *layerBASICROM,
	ATMemoryLayer *layerSelfTestROM,
	ATMemoryLayer *layerGameROM,
	const void *kernelROM)
{
	UpdateKernelROM();
}

void ATWarpOSDevice::OnU1MBConfigPreLocked(bool inPreLockState) {
}

void ATWarpOSDevice::InitPortInput(IATDevicePortManager *portmgr) {
	mpPortManager = portmgr;
	mPortInputIndex = portmgr->AllocInput();
	mPortOutputIndex = portmgr->AllocOutput(
		[](void *self, uint32 state) { ((ATWarpOSDevice *)self)->OnPortOutput(state); },
		this,
		0x8000
	);
}

void ATWarpOSDevice::OnScheduledEvent(uint32 id) {
	mpEvent = nullptr;

	if (mState == State((uint8)State::ShiftCommand1 + 1)
		|| mState == State((uint8)State::ShiftCommand2 + 1)
		|| mState == State((uint8)State::ShiftCommand3 + 1))
	{
		if (mbLastPB7State) {
			// start bit invalid (glitch) -- reset to idle state
			mState = State::ShiftCommand1;
		} else {
			// start bit valid -- begin shifting
			mShiftStart = mpScheduler->GetTick64();
			mState = State((uint8)mState + 1);

			// set timer for the stop bit
			mpScheduler->SetEvent(kBitTime * 9, this, 1, mpEvent);
		}
	} else if ((uint8)mState < (uint8)State::ShiftResult) {
		UpdateInputShifter();
	} else if (mState == State::Reset) {
		mpSystemController->ResetComputer();
	} else if ((uint8)mState >= (uint8)State::ShiftResult) {
		// the read routine doesn't have a special provision for the start bit,
		// so we must make the start bit half width
		if (mState == State((uint8)State::ShiftResult + 31)) {
			mState = State::ShiftCommand1;

			mpPortManager->SetInput(mPortInputIndex, ~UINT32_C(0));
		} else {
			if (mState == State::ShiftResult) {
				mpScheduler->SetEvent(kBitTime / 2, this, 1, mpEvent);
			} else {
				mpScheduler->SetEvent(kBitTime, this, 1, mpEvent);
			}

			mState = (State)((uint8)mState + 1);

			mpPortManager->SetInput(mPortInputIndex, mShifter & 1 ? ~UINT32_C(0) : ~UINT32_C(0x8000));
			mShifter >>= 1;
		}
	}
}

void ATWarpOSDevice::OnPortOutput(uint32 state) {
	const bool pb7 = (state & 0x8000) != 0;
	
	if (mState == State::ShiftCommand1 || mState == State::ShiftCommand2 || mState == State::ShiftCommand3) {
		if (!pb7) {
			mState = State((uint8)mState + 1);
			mpScheduler->SetEvent(kBitTime / 2, this, 1, mpEvent);
		}
	} else if ((uint8)mState < (uint8)State::ShiftResult) {
		UpdateInputShifter();
	}

	mbLastPB7State = pb7;
}

void ATWarpOSDevice::UpdateInputShifter() {
	// We ware guaranteed to call this by 9 bits, so we don't need 64-bit time.
	uint32 shiftDelta = mpScheduler->GetTick() - (uint32)mShiftStart;

	if (shiftDelta < kBitTime)
		return;

	const uint32 bitsToShift = shiftDelta / kBitTime;
	
	// shift in bits from bit 8 down
	mShifter >>= bitsToShift;

	if (mbLastPB7State) {
		mShifter += UINT32_C(0x200) - (UINT32_C(0x200) >> bitsToShift);
	}

	// advance state
	mShiftStart += kBitTime * bitsToShift;

	mState = State((uint8)mState + bitsToShift);

	if (mState == State::ShiftCommand2 || mState == State::ShiftCommand3 || mState == State::ShiftResult) {
		mpScheduler->UnsetEvent(mpEvent);

		// check stop bit
		if (!(mShifter & 0x100)) {
			// invalid stop bit -- reset to base state
			mState = State::ShiftCommand1;
		} else if (mState == State::ShiftCommand2) {
			// first byte must be $55 for any valid command
			if (mShifter != 0x155) {
				mState = State::ShiftCommand1;
			}
		} else if (mState == State::ShiftCommand3) {
			// valid commands are $00-1F to write or $20 to read
			if (mShifter >= 0x121)
				mState = State::ShiftCommand1;

			mCommand = (uint8)mShifter;
		} else if (mState == State::ShiftResult) {
			if (mCommand != (uint8)mShifter) {
				// third byte doesn't match second byte -- invalid command
				mState = State::ShiftCommand1;
			} else {
				// valid command -- initiate change + reset or readback
				if (mCommand < 0x20) {
					if (mCurrentSetting != mCommand) {
						mCurrentSetting = mCommand;

						SaveNVRAM();
					}

					mState = State::Reset;
					mpScheduler->SetEvent(1, this, 1, mpEvent);
				} else {
					// begin shifting data: $AA <setting> <setting> (with 2SB)
					mShifter = (0xAA + 0x300) << 1;
					mShifter += (mCurrentSetting + 0x300) << 12;
					mShifter += (mCurrentSetting + 0x300) << 23;
					mpScheduler->SetEvent(1, this, kBitTime, mpEvent);
				}
			}
		}
	}
}

void ATWarpOSDevice::UpdateKernelROM() {
	if (mpSystemController)
		mpSystemController->OverrideKernelMapping(this, mFlash + mCurrentSlot * 0x4000, 0, false);
}

void ATWarpOSDevice::LoadNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM", false);

	mCurrentSetting = key.getInt("Warp+ OS Selection", mCurrentSetting);

	// ensure sanity
	if (mCurrentSetting > 31)
		mCurrentSetting = 0;
}

void ATWarpOSDevice::SaveNVRAM() {
	VDRegistryAppKey key("Nonvolatile RAM");

	key.setInt("Warp+ OS Selection", mCurrentSetting);
}

///////////////////////////////////////////////////////////////////////////

void ATCreateDeviceWarpOS(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATWarpOSDevice> p(new ATWarpOSDevice);

	*dev = p.release();
}
