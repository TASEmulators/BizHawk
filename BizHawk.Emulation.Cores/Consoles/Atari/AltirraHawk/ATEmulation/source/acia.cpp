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

//===================================================================
// 6551 ACIA module notes:
//
// - The MOS datasheet has a bunch of errors and omissions in it.
//   For instance, it has the wrong bit for receiver IRQ and it
//   doesn't tell you how to clear any of the status bits. The
//   Synertek datasheet is a little better and the WDC datasheet
//   is much better.
//

#include <stdafx.h>
#include <at/atcore/scheduler.h>
#include <at/atcore/logging.h>
#include <at/atemulation/acia.h>

ATLogChannel g_ATLCACIAIO(false, false, "ACIAIO", "6551 ACIA input/output traffic");

namespace {
	// When we are actively receiving data, we keep a timer running
	// at the precise number of cycles per second. This timer gets
	// pretty expensive to run all the time at the higher baud rates
	// if we're not actually receiving data, so once we stop receiving
	// back-to-back bytes, we stop the receive timer on the fast
	// scheduler and switch to a receive poll timer on the slow
	// scheduler. Once this poll timer detects a byte waiting, we
	// swap back to the precise timer. This also jitters up the timing
	// a bit so that not every single byte arrives exactly on the
	// same clock phase.
	//
	// At 2400 baud, it takes ~65 scanlines for a byte to arrive. When
	// we temporary stop receiving bytes, we start polling every 30
	// scanlines to see if another byte will arrive. Within one second
	// (NTSC) this will have increased to about 150 scanlines, which
	// lowers overhead considerably. To keep reasonable wake-up latency,
	// we limit this to 180 scanlines, or about 10ms.
	const uint32 kPollRateInitial = 30;
	const uint32 kPollRateMax = 180;
}

ATACIA6551Emulator::ATACIA6551Emulator()
	: mpScheduler(nullptr)
	, mpSlowScheduler(nullptr)
	, mpEventTransmit(nullptr)
	, mpEventReceive(nullptr)
	, mpEventReceivePoll(nullptr)
	, mPollRate(0)
{
}

ATACIA6551Emulator::~ATACIA6551Emulator() {
	Shutdown();
}

void ATACIA6551Emulator::Init(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;
}

void ATACIA6551Emulator::Shutdown() {
	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventTransmit);
		mpScheduler->UnsetEvent(mpEventReceive);
	}

	if (mpSlowScheduler) {
		mpSlowScheduler->UnsetEvent(mpEventReceivePoll);
	}
}

void ATACIA6551Emulator::SetInterruptFn(const vdfunction<void(bool)>& fn) {
	mpInterruptFn = fn;
}

void ATACIA6551Emulator::SetReceiveReadyFn(const vdfunction<void()>& fn) {
	mpReceiveReadyFn = fn;
}

void ATACIA6551Emulator::SetTransmitFn(const vdfunction<void(uint8, uint32)>& fn) {
	mpTransmitFn = fn;
}

void ATACIA6551Emulator::SetControlFn(const vdfunction<void(bool, bool)>& fn) {
	mpControlFn = fn;
}

bool ATACIA6551Emulator::IsReceiveReady() const {
	return mCyclesPerByte && !mpEventReceive;
}

void ATACIA6551Emulator::ReceiveByte(uint8 data, uint32 baudRate) {
	// discard byte if clock not active
	if (!mCyclesPerByte)
		return;

	// kill poll timer
	mpSlowScheduler->UnsetEvent(mpEventReceivePoll);

	// restart receive timer
	mpScheduler->SetEvent(mCyclesPerByte, this, kEventId_Receive, mpEventReceive);

	// signal a framing error if baud rate is off by more than 5%
	if (baudRate && (uint32)abs((sint32)mBaudRate - (sint32)baudRate) * 20 > mBaudRate) {
		mReceiveShift = 0x55;
		mbReceiveFramingError = true;
	} else {
		mReceiveShift = data;
		mbReceiveFramingError = false;
	}
}

void ATACIA6551Emulator::Reset() {
	if (mpInterruptFn)
		mpInterruptFn(false);

	mStatus = 0x10;
	mControl = 0;
	mCommand = 0;
	mbControlLineChanged = false;

	mpScheduler->UnsetEvent(mpEventTransmit);
	mpScheduler->UnsetEvent(mpEventReceive);
	mpSlowScheduler->UnsetEvent(mpEventReceivePoll);

	UpdateBaudRate();
}

uint8 ATACIA6551Emulator::DebugReadByte(uint8 address) const {
	switch(address & 3) {
		case 0:		// receive data
			return mReceiveData;

		case 1:		// status
			return mStatus;

		case 2:		// command register
			return mCommand;
			break;

		case 3:		// control register
			return mControl;

		default:
			VDNEVERHERE;
	}
}

uint8 ATACIA6551Emulator::ReadByte(uint8 address) {
	switch(address & 3) {
		case 0:		// receive data
			// clear receive data register full status bit
			mStatus &= ~0x08;
			return mReceiveData;

		case 1:		// status
			{
				uint8 v = mStatus;

				// parity, framing, overrun, and IRQ bits are cleared by status read
				mStatus &= 0x78;

				if (v & 0x80)
					mpInterruptFn(false);

				return v;
			}

		case 2:		// command register
			return mCommand;

		case 3:		// control register
			return mControl;

		default:
			VDNEVERHERE;
	}
}

void ATACIA6551Emulator::WriteByte(uint8 address, uint8 value) {
	switch(address & 3) {
		case 0:		// transmit data
			mTransmitData = value;
			
			// clear transmitter data empty status bit
			mStatus &= ~0x10;

			// load shifter now if it's idle
			if (!mbTransmitShiftBusy)
				LoadTransmitShifter();
			break;

		case 1:		// programmed reset
			mStatus &= 0xFB;
			mCommand &= 0xE0;
			break;

		case 2:		// command register
			if (mCommand != value) {
				mCommand = value;

				if (mpControlFn)
					mpControlFn((value & 0x0c) != 0, (value & 1) != 0);

				// kind of cheesy, but restarts IRQs
				UpdateBaudRate();
			}
			break;

		case 3:		// control register
			if (mControl != value) {
				mControl = value;
				UpdateBaudRate();
			}
			break;

		default:
			VDNEVERHERE;
	}
}

void ATACIA6551Emulator::OnScheduledEvent(uint32 id) {
	switch(id) {
		case kEventId_Transmit:
			mpEventTransmit = nullptr;

			if (mbTransmitShiftBusy) {
				mbTransmitShiftBusy = false;

				g_ATLCACIAIO("Transmitting byte %02X\n", mTransmitShift);

				if (mpTransmitFn)
					mpTransmitFn(mTransmitShift, mBaudRate);
			}

			// Check if we have another byte to transmit
			if (!(mStatus & 0x10)) {
				LoadTransmitShifter();
			} else {
				// We don't have another byte to transmit. However, the ACIA is a bit
				// weird in that it will keep triggering IRQs every character time until
				// it gets one. Check if the transmit IRQ is enabled and we don't already
				// have an IRQ pending.
				if (!(mStatus & 0x80) && (mCommand & 0x0C) == 0x04) {
					// Re-fire the transmit IRQ.
					mStatus |= 0x80;

					if (mpInterruptFn)
						mpInterruptFn(true);

					// restart transmit timer
					if (mCyclesPerByte)
						mpScheduler->SetEvent(mCyclesPerByte, this, kEventId_Transmit, mpEventTransmit);
				}
			}
			break;

		case kEventId_Receive:
			mpEventReceive = nullptr;
			
			// check if we already had a byte
			if (mStatus & 0x08) {
				g_ATLCACIAIO("Received byte %02X (overrun!)\n", mReceiveShift);

				// set overrun bit and do not transfer byte (see W65C51S data sheet)
				mStatus |= 0x04;
			} else {
				g_ATLCACIAIO("Received byte %02X\n", mReceiveShift);

				// move received byte into receive register
				mReceiveData = mReceiveShift;

				// set receive register full bit
				mStatus |= 0x08;

				// fire interrupt if enabled
				if (!(mCommand & 0x02)) {
					mStatus |= 0x80;

					if (mpInterruptFn)
						mpInterruptFn(true);
				}
			}

			// issue receive ready hook
			if (mpReceiveReadyFn)
				mpReceiveReadyFn();

			// if we still aren't receiving, then turn on the slow poll
			if (!mpEventReceive) {
				mPollRate = kPollRateInitial;
				mpSlowScheduler->SetEvent(mPollRate, this, kEventId_ReceivePoll, mpEventReceivePoll);
			}
			break;

		case kEventId_ReceivePoll:
			mpEventReceivePoll = nullptr;

			if (IsReceiveReady() && mpReceiveReadyFn)
				mpReceiveReadyFn();

			if (!mpEventReceive) {
				if (mPollRate < kPollRateMax)
					++mPollRate;

				mpSlowScheduler->SetEvent(mPollRate, this, kEventId_ReceivePoll, mpEventReceivePoll);
			}
			break;
	}
}

void ATACIA6551Emulator::LoadTransmitShifter() {
	mTransmitShift = mTransmitData;
	mbTransmitShiftBusy = true;

	// set transmit data empty
	mStatus |= 0x10;

	// issue IRQ if enabled
	if ((mCommand & 0x0C) == 0x04) {
		if (!(mStatus & 0x80)) {
			mStatus |= 0x80;

			if (mpInterruptFn)
				mpInterruptFn(true);
		}
	}

	// restart transmit timer
	if (mCyclesPerByte)
		mpScheduler->SetEvent(mCyclesPerByte, this, kEventId_Transmit, mpEventTransmit);
}

void ATACIA6551Emulator::UpdateBaudRate() {
	static const uint32 kBaudTable[16]={
		0,			// 0000 = external clock
		50,			// 0001 = 50 baud
		75,			// 0010 = 75 baud
		110,		// 0011 = 109.92 baud
		135,		// 0100 = 134.5 baud
		150,		// 0101 = 150 baud
		300,		// 0110 = 300 baud
		600,		// 0111 = 600 baud
		1200,		// 1000 = 1200 baud
		1800,		// 1001 = 1800 baud
		2400,		// 1010 = 2400 baud
		2400,		// 1011 = 3600 baud
		4800,		// 1100 = 4800 baud
		7200,		// 1101 = 7200 baud
		9600,		// 1110 = 9600 baud
		19200,		// 1111 = 19200 baud
	};

	static const uint32 kPeriodTable[16]={
		     0,		// 0000 = external clock (disabled)
		357955,		// 0001 = 50 baud
		238636,		// 0010 = 75 baud
		162707,		// 0011 = 109.92 baud
		133069,		// 0100 = 134.5 baud
		119318,		// 0101 = 150 baud
		 59659,		// 0110 = 300 baud
		 29830,		// 0111 = 600 baud
		 14915,		// 1000 = 1200 baud
		  9943,		// 1001 = 1800 baud
		  7457,		// 1010 = 2400 baud
		  4972,		// 1011 = 3600 baud
		  3729,		// 1100 = 4800 baud
		  2486,		// 1101 = 7200 baud
		  1864,		// 1110 = 9600 baud
		   932,		// 1111 = 19200 baud
	};

	mBaudRate = kBaudTable[mControl & 15];
	mCyclesPerByte = kPeriodTable[mControl & 15];

	if (!mBaudRate) {
		mpSlowScheduler->UnsetEvent(mpEventReceivePoll);
		mpScheduler->UnsetEvent(mpEventReceive);
		mpScheduler->UnsetEvent(mpEventTransmit);
		mbTransmitShiftBusy = false;
	} else {
		if (IsReceiveReady() && mpReceiveReadyFn) {
			mpReceiveReadyFn();

			if (!mpEventReceive) {
				mPollRate = kPollRateInitial;
				mpSlowScheduler->SetEvent(mPollRate, this, kEventId_ReceivePoll, mpEventReceivePoll);
			}
		}

		if (!mpEventTransmit)
			mpScheduler->SetEvent(mCyclesPerByte, this, kEventId_Transmit, mpEventTransmit);
	}
}
