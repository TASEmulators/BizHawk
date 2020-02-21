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
#include "sio2sd.h"
#include "uirender.h"

void ATCreateDeviceSIO2SD(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATSIO2SDEmulator> p(new ATSIO2SDEmulator);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSIO2SD = { "sio2sd", nullptr, L"SIO2SD", ATCreateDeviceSIO2SD };

ATSIO2SDEmulator::ATSIO2SDEmulator()
	: mpSIOMgr(nullptr)
	, mHighSpeedCPSLo(0)
	, mHighSpeedCPSHi(0)
	, mHighSpeedIndex(0)
	, mbHighSpeedEnabled(false)
	, mbHighSpeedPhase(false)
{
}

ATSIO2SDEmulator::~ATSIO2SDEmulator() {
}

void *ATSIO2SDEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
	}

	return ATDevice::AsInterface(iid);
}

void ATSIO2SDEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefSIO2SD;
}

void ATSIO2SDEmulator::GetSettings(ATPropertySet& settings) {
}

bool ATSIO2SDEmulator::SetSettings(const ATPropertySet& settings) {
	return true;
}

void ATSIO2SDEmulator::Init() {
}

void ATSIO2SDEmulator::Shutdown() {
	if (mpUIRenderer) {
		mpUIRenderer = nullptr;
	}

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATSIO2SDEmulator::WarmReset() {
}

void ATSIO2SDEmulator::ColdReset() {
	mHighSpeedIndex = 6;
	mbHighSpeedEnabled = true;
	mbHighSpeedPhase = false;

	WarmReset();
}

void ATSIO2SDEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATSIO2SDEmulator::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATSIO2SDEmulator::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (cmd.mDevice != 0x72)
		return kCmdResponse_NotHandled;

	mbHighSpeedPhase = false;
	if (!cmd.mbStandardRate) {
		if (cmd.mCyclesPerBit >= mHighSpeedCPSLo && cmd.mCyclesPerBit <= mHighSpeedCPSHi)
			mbHighSpeedPhase = true;
		else
			return kCmdResponse_NotHandled;
	}

	if (cmd.mAUX[1] & 0x80)
		mbHighSpeedPhase = true;

	mCommand = cmd;

	switch(cmd.mCommand) {
		case 0x00:	// get device status
			{
				//uint8 status = 0;		// no card in drive
				uint8 status = 1;		// card in drive
				return DoReadCommand(&status, 1);
			}

		case 0x01:	// get disk mapping parameters
			if ((cmd.mAUX[1] & 0x7F) > 4)
				return DoNAKCommand();

			{
				int n = cmd.mAUX[1] & 0x7F;

				if (!n)			// UNDOCUMENTED -- required by conf utility
					n = 1;

				uint8 params[54*4] = {0};

				for(int i=0; i<4; ++i) {
					uint8 *info = params + 54*i;

					// blank filename (39 characters)
					memset(info, ' ', 39);
				}

				return DoReadCommand(params, n * 54);
			}

		case 0x04:	// get next entries from current directory
			{
				int n = cmd.mAUX[1] & 0x7F;

				if (!n)			// UNDOCUMENTED -- required by conf utility
					n = 1;

				if (n > 4)
					return DoNAKCommand();

				// all zeroes is end of dir
				uint8 entries[4][54] = {0};

				return DoReadCommand(entries, n * 54);
			}

		case 0x07:	// go to main directory
			return DoImpliedCommand();

		case 0x08:	// get current directory
			{
				char cwd[0x36];
				memset(cwd, ' ', sizeof cwd);
				cwd[0] = '\\';
				return DoReadCommand(cwd, sizeof cwd);
			}

		case 0x09:	// set searching mask
			return DoWriteCommand(16);

		case 0x0A:	// get number of entries in current dir matching mask
			{
				uint16 count = 0;

				return DoReadCommand(&count, 2);
			}

		case 0x11:	// get firmware version
			{
				uint8 version = 0x31;

				return DoReadCommand(&version, 1);
			}

		case 0x14:	// get virtual drive mappings
			{
				uint8 mappings[15] = {0};

				return DoReadCommand(mappings, sizeof mappings);
			}

		case 0x20:	// open file
			return DoWriteCommand(39);
	}

	return kCmdResponse_NotHandled;
}

void ATSIO2SDEmulator::OnSerialAbortCommand() {
}

void ATSIO2SDEmulator::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
}

void ATSIO2SDEmulator::OnSerialFence(uint32 id) {
}

IATDeviceSIO::CmdResponse ATSIO2SDEmulator::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}

ATSIO2SDEmulator::CmdResponse ATSIO2SDEmulator::DoReadCommand(const void *data, uint32 len) {
	mpSIOMgr->BeginCommand();
	if (mbHighSpeedPhase)
		mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
	mpSIOMgr->SendACK();
	mpSIOMgr->Delay(1000);
	mpSIOMgr->SendComplete();
	mpSIOMgr->Delay(1000);
	mpSIOMgr->SendData(data, len, true);
	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}

ATSIO2SDEmulator::CmdResponse ATSIO2SDEmulator::DoWriteCommand(uint32 len) {
	mpSIOMgr->BeginCommand();
	if (mbHighSpeedPhase)
		mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
	mpSIOMgr->SendACK();
	mpSIOMgr->ReceiveData(mCommand.mCommand, len, true);
	mpSIOMgr->Delay(1000);
	mpSIOMgr->SendComplete();
	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}

ATSIO2SDEmulator::CmdResponse ATSIO2SDEmulator::DoImpliedCommand() {
	mpSIOMgr->BeginCommand();
	if (mbHighSpeedPhase)
		mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);
	mpSIOMgr->SendACK();
	mpSIOMgr->Delay(1000);
	mpSIOMgr->SendComplete();
	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}

ATSIO2SDEmulator::CmdResponse ATSIO2SDEmulator::DoNAKCommand() {
	mpSIOMgr->BeginCommand();
	if (mbHighSpeedPhase)
		mpSIOMgr->SetTransferRate((mHighSpeedIndex + 7) * 2, (mHighSpeedIndex + 7) * 20);

	mpSIOMgr->SendNAK();
	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}
