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

// SIO Type 3/4 Poll test device
//
// This device loads a relocatable handler through the XL/XE type 3/4
// poll mechanism. The CIO device is called T: and is a null device,
// but this tests whether the polling mechanism works in the OS. It
// can be tested through a BASIC program like this:
//
// DPOKE $2E7,$2000:NEW
// DPOKE $2E7,$0700
// POKE $2E9,1:OPEN #1,8,0,"T"
// POKE $2E9,1:DPOKE $2EC,$700:PUT #1,X
//
class ATDeviceTestSIOPoll final : public ATDevice
					, public ATDeviceSIO
{
public:
	ATDeviceTestSIOPoll(bool type4);

	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void Shutdown() override;
	virtual void ColdReset() override;

public:	// IATDeviceSIO
	virtual void InitSIO(IATDeviceSIOManager *siomgr) override;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;

private:
	IATDeviceSIOManager *mpSIOMgr;
	const bool mbType4;
	bool mbType3PollActive;

	static const uint8 kHandlerData[];
};

const uint8 ATDeviceTestSIOPoll::kHandlerData[]={
	// text record
	0x00, 0x2C,
		0x00, 0x00,			//	relative load address (0)
		0x26, 0x00,			//			dta		a(DevOpen-1)
		0x26, 0x00,			//			dta		a(DevClose-1)
		0x24, 0x00,			//			dta		a(DevGetByte-1)
		0x26, 0x00,			//			dta		a(DevPutByte-1)
		0x26, 0x00,			//			dta		a(DevGetStatus-1)
		0x28, 0x00,			//			dta		a(DevSpecial-1)
		0x4C, 0x16, 0x00,	//			jmp		DevInit
		0x00,				//			dta		0			;checksum area
		0x2A, 0x00,			//			dta		a(size)		;handler size
		0x00, 0x00,			//			dta		a(0)		;link pointer
		0x00, 0x00,			//			dta		a(0)		;unused
		0xA2, 0x54,			//	DevInit	ldx		#'T'
		0xA9, 0x00,			//			lda		#>HandlerTable
		0xA0, 0x00,			//			ldy		#<HandlerTable
		0x20, 0x86, 0xE4,	//			jsr		pentv
		0x30, 0x02,			//			bmi		fail
		0x18,				//			clc
		0x60,				//			rts
		0x38,				//	fail	sec
		0x60,				//			rts
		0xA9, 0x00,			//	DevGetByte	lda	#0
		0xA0, 0x01,			//	DevOpen		ldy	#1		;also close, put byte, get status
		0x60,				//	DevSpecial	rts

	// word relocations
	0x06, 0x07,
		0x00,
		0x02,
		0x04,
		0x06,
		0x08,
		0x0A,
		0x0D,

	// low byte relocations
	0x02, 0x01,
		0x1B,

	// high byte relocations
	0x08, 0x02,
		0x19, 0x00,

	// end
	0x0B, 0x00, 0x00, 0x00
};

template<bool T_Type4>
void ATCreateDeviceTestSIOPoll(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceTestSIOPoll> p(new ATDeviceTestSIOPoll(T_Type4));

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefTestSIOPoll4 = { "testsiopoll4", nullptr, L"SIO Type 4 Poll Test Device", ATCreateDeviceTestSIOPoll<true> };
extern const ATDeviceDefinition g_ATDeviceDefTestSIOPoll3 = { "testsiopoll3", nullptr, L"SIO Type 3 Poll Test Device", ATCreateDeviceTestSIOPoll<false> };

ATDeviceTestSIOPoll::ATDeviceTestSIOPoll(bool type4)
	: mpSIOMgr(nullptr)
	, mbType4(type4)
	, mbType3PollActive(true)
{
}

void *ATDeviceTestSIOPoll::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceSIO::kTypeID:
			return static_cast<IATDeviceSIO *>(this);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceTestSIOPoll::GetDeviceInfo(ATDeviceInfo& info) {
	if (mbType4)
		info.mpDef = &g_ATDeviceDefTestSIOPoll4;
	else
		info.mpDef = &g_ATDeviceDefTestSIOPoll3;
}

void ATDeviceTestSIOPoll::Shutdown() {
	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATDeviceTestSIOPoll::ColdReset() {
	mbType3PollActive = true;
}

void ATDeviceTestSIOPoll::InitSIO(IATDeviceSIOManager *siomgr) {
	mpSIOMgr = siomgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATDeviceTestSIOPoll::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	uint8 buf[128];

	if (mbType4) {
		// check for type 4 poll command
		if (cmd.mDevice == 0x4F && cmd.mCommand == 0x40) {
			// check if it is for T:
			if (cmd.mAUX[0] == (uint8)'T') {
				// send back handler info
				buf[0] = (uint8)(sizeof(kHandlerData) >> 0);
				buf[1] = (uint8)(sizeof(kHandlerData) >> 8);
				buf[2] = 0xFE;
				buf[3] = 0x00;

				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();
				mpSIOMgr->SendData(buf, 4, true);
				mpSIOMgr->EndCommand();

				return kCmdResponse_Start;
			}
		}
	} else {
		// Check for type 3 poll command.
		//
		// The poll count is completely arbitrary, the only hard requirement
		// being that it be before the OS finishes its retries (26). There is
		// no central registry of device poll counts....
		//
		if (cmd.mDevice == 0x4F && cmd.mCommand == 0x40 && cmd.mAUX[0] == cmd.mAUX[1]) {
			if (cmd.mAUX[0] == 0x00 && cmd.mPollCount == 10 && mbType3PollActive) {
				mbType3PollActive = false;

				// send back handler info
				buf[0] = (uint8)(sizeof(kHandlerData) >> 0);
				buf[1] = (uint8)(sizeof(kHandlerData) >> 8);
				buf[2] = 0xFE;
				buf[3] = 0x00;

				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();
				mpSIOMgr->SendData(buf, 4, true);
				mpSIOMgr->EndCommand();

				return kCmdResponse_Start;
			} else if (cmd.mAUX[0] == 0x4F) {
				// Poll Reset -- do not respond, but re-enable poll
				mbType3PollActive = true;
			}
		}
	}
	
	if (cmd.mDevice == 0xFE) {
		if (cmd.mCommand == 0x26) {
			// compute block number and check if it is valid
			const uint32 block = cmd.mAUX[0];

			if (block * 128 >= sizeof kHandlerData)
				return kCmdResponse_Fail_NAK;

			// return handler data
			memset(buf, 0, sizeof buf);

			const uint32 offset = block * 128;
			memcpy(buf, kHandlerData + offset, std::min<uint32>(128, sizeof(kHandlerData) - offset));

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
			mpSIOMgr->SendData(buf, 128, true);
			mpSIOMgr->EndCommand();

			return kCmdResponse_Start;
		}

		// eh, we don't know this command
		return kCmdResponse_Fail_NAK;
	}

	return kCmdResponse_NotHandled;
}
