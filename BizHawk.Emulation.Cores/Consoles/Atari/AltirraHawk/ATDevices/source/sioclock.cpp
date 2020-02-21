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
#include <vd2/system/date.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicesioimpl.h>

class ATDeviceSIOClock final : public ATDevice
					, public ATDeviceSIO
{
public:
	ATDeviceSIOClock();

	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void Shutdown() override;

public:	// IATDeviceSIO
	virtual void InitSIO(IATDeviceSIOManager *siomgr) override;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;

private:
	IATDeviceSIOManager *mpSIOMgr;
};

void ATCreateDeviceSIOClock(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceSIOClock> p(new ATDeviceSIOClock);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSIOClock = { "sioclock", nullptr, L"SIO Real-Time Clock", ATCreateDeviceSIOClock };

ATDeviceSIOClock::ATDeviceSIOClock()
	: mpSIOMgr(nullptr)
{
}

void *ATDeviceSIOClock::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceSIO::kTypeID:
			return static_cast<IATDeviceSIO *>(this);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceSIOClock::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefSIOClock;
}

void ATDeviceSIOClock::Shutdown() {
	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATDeviceSIOClock::InitSIO(IATDeviceSIOManager *siomgr) {
	mpSIOMgr = siomgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATDeviceSIOClock::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	// check for APETIME command (45 93 EE A0)
	// check for AspeQt command (46 93)
	if ((cmd.mDevice == 0x45
			&& cmd.mCommand == 0x93
			&& cmd.mAUX[0] == 0xEE
			&& cmd.mAUX[1] == 0xA0)
		|| (cmd.mDevice == 0x46 && cmd.mCommand == 0x93)
		)
	{
		const auto date = VDGetLocalDate(VDGetCurrentDate());

		uint8 buf[6];
		buf[0] = (uint8)date.mDay;
		buf[1] = (uint8)date.mMonth;
		buf[2] = (uint8)(date.mYear % 100);
		buf[3] = (uint8)date.mHour;
		buf[4] = (uint8)date.mMinute;
		buf[5] = (uint8)date.mSecond;

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();
		mpSIOMgr->SendData(buf, 6, true);
		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	}

	// check for SIO2USB command
	if (cmd.mDevice == 0x70 && cmd.mCommand == 0x02) {
		const auto date = VDGetLocalDate(VDGetCurrentDate());

		uint8 buf[128] = {0};

		buf[0] = 0x06;
		buf[1] = (uint8)date.mSecond;
		buf[2] = (uint8)date.mMinute;
		buf[3] = (uint8)date.mHour;
		buf[4] = (uint8)date.mDay;
		buf[5] = (uint8)date.mMonth;
		buf[6] = (uint8)(date.mYear % 100);
		buf[7] = 0xFF;

		// convert to BCD
		for(int i=1; i<=6; ++i) {
			uint8 c = buf[i];

			buf[i] = (uint8)(((c / 10) << 4) + (c % 10));
		}

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();
		mpSIOMgr->SendData(buf, 128, true);
		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	}

	return kCmdResponse_NotHandled;
}
