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
#include <vd2/system/error.h>
#include <vd2/system/math.h>
#include <at/atcore/blockdevice.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/devicesio.h>
#include "sdrive.h"
#include "uirender.h"

void ATCreateDeviceSDrive(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATSDriveEmulator> p(new ATSDriveEmulator);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSDrive = { "sdrive", nullptr, L"SDrive", ATCreateDeviceSDrive };

ATSDriveEmulator::ATSDriveEmulator()
	: mpSIOMgr(nullptr)
	, mSectorNumber(0)
	, mHighSpeedCPSLo(0)
	, mHighSpeedCPSHi(0)
	, mHighSpeedIndex(0)
	, mbHighSpeedEnabled(false)
	, mbHighSpeedPhase(false)
{
}

ATSDriveEmulator::~ATSDriveEmulator() {
}

void *ATSDriveEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceParent::kTypeID: return static_cast<IATDeviceParent *>(&mDeviceParent);
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
	}

	return nullptr;
}

void ATSDriveEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefSDrive;
}

void ATSDriveEmulator::GetSettings(ATPropertySet& settings) {
}

bool ATSDriveEmulator::SetSettings(const ATPropertySet& settings) {
	return true;
}

void ATSDriveEmulator::Init() {
	mDeviceParent.Init(IATBlockDevice::kTypeID, "harddisk", L"SD Card Bus", "sdbus", this);
	mDeviceParent.SetOnAttach([this] { mpDisk = mDeviceParent.GetChild<IATBlockDevice>(); });
	mDeviceParent.SetOnDetach([this] { mpDisk = nullptr; });
}

void ATSDriveEmulator::Shutdown() {
	mDeviceParent.Shutdown();

	if (mpUIRenderer) {
		mpUIRenderer = nullptr;
	}

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATSDriveEmulator::WarmReset() {
}

void ATSDriveEmulator::ColdReset() {
	mSectorNumber = 0;
	memset(mSectorBuffer, 0, sizeof mSectorBuffer);

	mHighSpeedIndex = 40;
	mbHighSpeedEnabled = false;
	mbHighSpeedPhase = false;

	WarmReset();
}

void ATSDriveEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATSDriveEmulator::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATSDriveEmulator::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	// The SDrive only has one USART at constant speed, so it can't monitor
	// both speeds. Therefore, it has to toggle between them instead.
	if (mbHighSpeedPhase) {
		if (cmd.mCyclesPerBit < mHighSpeedCPSLo || cmd.mCyclesPerBit > mHighSpeedCPSHi) {
			mbHighSpeedPhase = !mbHighSpeedPhase;
			return kCmdResponse_NotHandled;
		}
	} else {
		if (!cmd.mbStandardRate) {
			if (mbHighSpeedEnabled)
				mbHighSpeedPhase = !mbHighSpeedPhase;

			return kCmdResponse_NotHandled;
		}
	}

	if (cmd.mDevice < 0x71 || cmd.mDevice > 0x74)
		return kCmdResponse_NotHandled;

	switch(cmd.mCommand) {
		case 0xC1:
			if (cmd.mAUX[0] > 0xF9)
				return kCmdResponse_Fail_NAK;

			mHighSpeedIndex = cmd.mAUX[0];
			mHighSpeedCPSLo = VDRoundToInt((float)(mHighSpeedIndex + 7) * 1.95f);
			mHighSpeedCPSHi = VDRoundToInt((float)(mHighSpeedIndex + 7) * 2.05f);
			mbHighSpeedEnabled = (mHighSpeedIndex != 40);
			mbHighSpeedPhase = false;
			break;

		case 0xDD:	// set SD sector number
			mpSIOMgr->BeginCommand();
			if (mbHighSpeedPhase)
				mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
			mpSIOMgr->SendACK();
			mpSIOMgr->ReceiveData(cmd.mCommand, 4, true);
			mpSIOMgr->SendComplete();
			mpSIOMgr->EndCommand();
			return kCmdResponse_Start;

		case 0xDE:	// read SD sector
			mpSIOMgr->BeginCommand();
			if (mbHighSpeedPhase)
				mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
			mpSIOMgr->SendACK();
			mpSIOMgr->InsertFence(cmd.mCommand);
			return kCmdResponse_Start;

		case 0xDF:	// write SD sector
			mpSIOMgr->BeginCommand();
			if (mbHighSpeedPhase)
				mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
			mpSIOMgr->SendACK();
			mpSIOMgr->ReceiveData(cmd.mCommand, 512, true);
			return kCmdResponse_Start;
	}

	return kCmdResponse_NotHandled;
}

void ATSDriveEmulator::OnSerialAbortCommand() {
}

void ATSDriveEmulator::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
	switch(id) {
		case 0xDD:	// set SD sector number
			mSectorNumber = VDReadUnalignedLEU32(data);
			break;

		case 0xDF:	// write SD sector
			if (!mpDisk || mSectorNumber >= mpDisk->GetSectorCount()) {
				mpSIOMgr->SendError();
			} else {
				try {
					mpUIRenderer->SetIDEActivity(true, mSectorNumber);
					mpDisk->WriteSectors(mSectorBuffer, mSectorNumber, 1);
					mpSIOMgr->SendComplete();
				} catch(const MyError&) {
					mpSIOMgr->SendError();
				}
			}

			mpSIOMgr->EndCommand();
			break;
	}
}

void ATSDriveEmulator::OnSerialFence(uint32 id) {
	switch(id) {
		case 0xDE:	// read SD sector
			if (!mpDisk || mSectorNumber >= mpDisk->GetSectorCount()) {
				mpSIOMgr->SendError();
			} else  {
				try {
					mpUIRenderer->SetIDEActivity(false, mSectorNumber);
					mpDisk->ReadSectors(mSectorBuffer, mSectorNumber, 1);
					mpSIOMgr->SendComplete();
					mpSIOMgr->SendData(mSectorBuffer, 512, true);
				} catch(const MyError&) {
					mpSIOMgr->SendError();
				}
			}

			mpSIOMgr->SendData(mSectorBuffer, 512, true);
			mpSIOMgr->EndCommand();
			break;
	}
}

IATDeviceSIO::CmdResponse ATSDriveEmulator::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}
