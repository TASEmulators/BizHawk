//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <vd2/system/strutil.h>
#include <at/atcore/cio.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicesioimpl.h>
#include <at/atcore/logging.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include <at/atcore/sioutils.h>
#include "pokey.h"
#include "pia.h"
#include "rs232.h"
#include "modem.h"

extern ATLogChannel g_ATLCModemData;

ATLogChannel g_ATLCPocketModem(true, false, "POCKETMODEM", "Pocket Modem activity");

//////////////////////////////////////////////////////////////////////////

class ATDevicePocketModem final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceIndicators					
	, public ATDeviceSIO
	, public IATDeviceRawSIO
	, public IATSchedulerCallback
{
	ATDevicePocketModem(const ATDevicePocketModem&) = delete;
	ATDevicePocketModem& operator=(const ATDevicePocketModem&) = delete;
public:
	ATDevicePocketModem();
	~ATDevicePocketModem();

	void *AsInterface(uint32 id) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;

public:	// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:	// ATDeviceSIO
	void InitSIO(IATDeviceSIOManager *mgr) override;

public:	// IATDeviceRawSIO
	void OnCommandStateChanged(bool asserted) override;
	void OnMotorStateChanged(bool asserted) override;
	void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	void OnSendReady() override;

public:	// IATSchedulerCallback
	void OnScheduledEvent(uint32 id) override;

private:
	void OnControlStateChanged(const ATDeviceSerialStatus& status);

	enum {
		kEventId_Receive = 1,
		kEventId_DialComplete,
		kEventId_Ring,
	};

	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;

	ATEvent	*mpEventReceive = nullptr;
	ATEvent *mpEventDialComplete = nullptr;
	ATEvent *mpEventRing = nullptr;

	ATModemEmulator *mpDevice = nullptr;

	uint32	mReceiveBaudRate = 0;
	uint32	mReceiveCyclesPerBit = 0;
	uint32	mReceiveCyclesPerByte = 0;

	uint64	mModeShiftLastTime = 0;
	uint8	mModeShiftRegister = 0;
	uint8	mModeShiftState = 0;

	uint64	mPulseDialLastTime = 0;
	uint8	mPulseDialDigit = 0;
	uint8	mPulseDialDigits = 0;

	bool	mbRingAsserted = false;

	bool	mbCommActive = false;
	bool	mbOffHook = false;
	bool	mbAnswerMode = false;
	bool	mbDialMode = false;

	VDStringA mDialAddress;
	VDStringA mDialService;
};

void ATCreateDevicePocketModem(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDevicePocketModem> p(new ATDevicePocketModem);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefPocketModem = { "pocketmodem", "pocketmodem", L"Pocket Modem", ATCreateDevicePocketModem };

ATDevicePocketModem::ATDevicePocketModem() {
	// We have to init this early so it can accept settings.
	mpDevice = new ATModemEmulator;
	mpDevice->AddRef();
	mpDevice->Set1030Mode();
}

ATDevicePocketModem::~ATDevicePocketModem() {
	Shutdown();
}

void *ATDevicePocketModem::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceSIO::kTypeID:			return static_cast<IATDeviceSIO *>(this);
		case IATDeviceRawSIO::kTypeID:		return static_cast<IATDeviceRawSIO *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATDevicePocketModem::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefPocketModem;
}

void ATDevicePocketModem::GetSettings(ATPropertySet& props) {
	if (!mDialAddress.empty())
		props.SetString("dialaddr", VDTextAToW(mDialAddress).c_str());

	if (!mDialService.empty())
		props.SetString("dialsvc", VDTextAToW(mDialService).c_str());

	mpDevice->GetSettings(props);
}

bool ATDevicePocketModem::SetSettings(const ATPropertySet& props) {
	mDialAddress = VDTextWToA(props.GetString("dialaddr", L""));
	mDialService = VDTextWToA(props.GetString("dialsvc", L""));

	mpDevice->SetSettings(props);

	return true;
}

void ATDevicePocketModem::Init() {
	mpDevice->SetOnStatusChange([this](const ATDeviceSerialStatus& status) { this->OnControlStateChanged(status); });
	mpDevice->Init(mpScheduler, mpSlowScheduler, mpUIRenderer, nullptr);
}

void ATDevicePocketModem::Shutdown() {
	if (mpSIOMgr) {
		mpSIOMgr->RemoveRawDevice(this);
		mpSIOMgr = nullptr;
	}

	if (mpDevice) {
		mpDevice->Release();
		mpDevice = NULL;
	}

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventReceive);
		mpScheduler->UnsetEvent(mpEventDialComplete);
		mpScheduler->UnsetEvent(mpEventRing);
		mpScheduler = nullptr;
	}

	mpSlowScheduler = nullptr;

	mpUIRenderer = nullptr;
}

void ATDevicePocketModem::ColdReset() {
	if (mpDevice)
		mpDevice->ColdReset();

	mReceiveBaudRate = 300;
	mReceiveCyclesPerBit = 5966;
	mReceiveCyclesPerByte = 59659;

	mbRingAsserted = false;

	mModeShiftLastTime = 0;
	mModeShiftRegister = 0;
	mModeShiftState = 0;

	mPulseDialLastTime = 0;
	mPulseDialDigit = 0;
	mPulseDialDigits = 0;

	mpScheduler->UnsetEvent(mpEventReceive);
	mpScheduler->UnsetEvent(mpEventDialComplete);
	mpScheduler->UnsetEvent(mpEventRing);

	mModeShiftLastTime = mpScheduler->GetTick64();
}

void ATDevicePocketModem::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;
}

void ATDevicePocketModem::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATDevicePocketModem::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;
	mpSIOMgr->AddRawDevice(this);
	mpSIOMgr->SetSIOProceed(this, true);
	mpSIOMgr->SetSIOInterrupt(this, true);
}

void ATDevicePocketModem::OnCommandStateChanged(bool asserted) {
	// If we are in dialing mode, the command line is used to pulse
	// dial by taking the phone off the hook whenever command is
	// asserted.
	if (!mbDialMode)
		return;

	// Check if we have an on-hook transition.
	if (!asserted)
		return;

	// Check if this is a short or long delay. Require at least 5
	// pulses per second for fast dial, otherwise consider interdigit
	// tone.
	const uint64 t = mpScheduler->GetTick64();
	const uint64 delay = t - mPulseDialLastTime;
	mPulseDialLastTime = t;

	if (delay > 178977) {
		if (mPulseDialDigit) {
			++mPulseDialDigits;
			mPulseDialDigit = 0;
		}
	}

	// Set dial expiration event for two seconds after the last pulse
	// dialed.
	mpSlowScheduler->SetEvent(31400, this, kEventId_DialComplete, mpEventDialComplete);
}

void ATDevicePocketModem::OnMotorStateChanged(bool asserted) {
	// The motor line is used by the computer to shift four control state bits
	// into the Pocket Modem device. A new bit is shifted in from the command
	// state line on the falling edge of the motor line. There are no clear
	// delimiters for the shift operation, but the shift is VERY slow (3 vblanks
	// per polarity on motor line). We use a few safeguards here:
	//
	// - Edge transitions must take at least 1.5 NTSC frames and no more than
	//   4 NTSC frames.
	// - Transition that is too short or too long forces reset of shift register.
	// - Shift is automatically committed to state after fourth bit.

	// Transitions we are expecting:
	//
	//	0: motor assert -> shift in command
	//	1: motor negate
	//	2: motor assert -> shift in command
	//	3: motor negate
	//	4: motor assert -> shift in command
	//	5: motor negate
	//	6: motor assert -> shift in command, commit

	const uint64 t = mpScheduler->GetTick64();
	const uint64 timeElapsed = t - mModeShiftLastTime;

	mModeShiftLastTime = t;

	if (timeElapsed < 44802 || timeElapsed > 119472)
		mModeShiftState = 0;

	// Check if we have the right state transition.
	const bool expectedState = (mModeShiftState & 1) == 0;
	if (asserted != expectedState)
		return;

	++mModeShiftState;

	if (mModeShiftState & 1) {
		mModeShiftRegister >>= 1;

		if (!mpSIOMgr->IsSIOCommandAsserted())
			mModeShiftRegister += 8;

		if (mModeShiftState == 7) {
			mModeShiftState = 0;

			// Commit the state.
			g_ATLCPocketModem("Changing mode to $%X\n", mModeShiftRegister);

			// bit 0 = 1: comm active between computer and modem
			const bool commActive = (mModeShiftRegister & 1) != 0;

			if (mbCommActive != commActive) {
				mbCommActive = commActive;

				if (commActive)
					mpScheduler->SetEvent(mReceiveCyclesPerByte, this, kEventId_Receive, mpEventReceive);
				else
					mpScheduler->UnsetEvent(mpEventReceive);
			}

			// bit 2 = 1: answer mode (0: originate mode)
			mbAnswerMode = (mModeShiftRegister & 4) != 0;

			// bit 1 = 1: off hook
			const bool offHook = (mModeShiftRegister & 2) != 0;
			if (mbOffHook != offHook) {
				mbOffHook = offHook;

				if (offHook) {
					// We need to 'answer' only in answer mode, because the
					// phone can also be taken off hook for dialing.
					if (mbAnswerMode)
						mpDevice->Answer();
				} else
					mpDevice->HangUp();
			}

			// bit 3 = 1: dial mode
			mbDialMode = (mModeShiftRegister & 8) != 0;
		}
	}
}

void ATDevicePocketModem::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	if (!mbCommActive)
		return;

	// fast reject any bytes well above 500 baud
	if (cyclesPerBit < 3000)
		return;

	static const struct {
		uint32 mBaudRate;
		uint32 mCyclesPerByte;
		uint32 mCyclesPerBit;
		uint32 mCyclesPerBitLo;
		uint32 mCyclesPerBitHi;
	} kBaudRates[] = {
		{ 300,  59659,  5966,  5666,  6265 },
		{ 210,  85227,  8523,  8096,  8950 },
		{ 110, 162707, 16271, 15457, 17085 },
		{ 500,  35795,  3580,  3401,  3759 },
	};

	for(const auto& rate : kBaudRates) {
		if (cyclesPerBit >= rate.mCyclesPerBitLo && cyclesPerBit <= rate.mCyclesPerBitHi) {
			// check if we should auto-switch the receive rate
			if (mReceiveBaudRate != rate.mBaudRate) {
				mReceiveBaudRate = rate.mBaudRate;
				mReceiveCyclesPerBit = rate.mCyclesPerBit;
				mReceiveCyclesPerByte = rate.mCyclesPerByte;
			}

			g_ATLCModemData("Sending byte to modem: $%02X (%u baud)\n", c, rate.mBaudRate);
			mpDevice->Write(rate.mBaudRate, c);
			break;
		}
	}
}

void ATDevicePocketModem::OnSendReady() {
}

void ATDevicePocketModem::OnScheduledEvent(uint32 id) {
	if (id == kEventId_Receive) {
		mpEventReceive = mpScheduler->AddEvent(mReceiveCyclesPerByte, this, kEventId_Receive);

		uint8 c;
		bool framingError;

		if (mpDevice->Read(0, c, framingError)) {
			if (mbCommActive) {
				g_ATLCModemData("Receiving byte from modem: $%02X\n", c);

				mpSIOMgr->SendRawByte(c, mReceiveCyclesPerBit);
			}
		}
	} else if (id == kEventId_DialComplete) {
		mpEventDialComplete = nullptr;

		if (mbOffHook && !mDialAddress.empty() && !mDialService.empty())
			mpDevice->Dial(mDialAddress.c_str(), mDialService.c_str());
	} else if (id == kEventId_Ring) {
		mpEventRing = mpSlowScheduler->AddEvent(1500, this, kEventId_Ring);

		mbRingAsserted = !mbRingAsserted;
		mpSIOMgr->SetSIOInterrupt(this, mbRingAsserted);
	}
}

void ATDevicePocketModem::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	// The Pocket Modem _raises_ the proceed line on a carrier.
	// Yes, this is weird.
	mpSIOMgr->SetSIOProceed(this, !status.mbCarrierDetect);

	// Toggle interrupt whenever the phone is ringing.
	if (status.mbRinging) {
		// Toggle interrupt line at ~5 rings/sec
		mpSlowScheduler->SetEvent(1500, this, kEventId_Ring, mpEventRing);
	} else {
		if (mpEventRing) {
			mpSlowScheduler->UnsetEvent(mpEventRing);
			mpSIOMgr->SetSIOInterrupt(this, false);
		}
	}
}
