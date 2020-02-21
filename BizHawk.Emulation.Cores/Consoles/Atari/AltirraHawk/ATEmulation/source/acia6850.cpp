//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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
#include <at/atcore/scheduler.h>
#include <at/atcore/logging.h>
#include <at/atemulation/acia6850.h>

ATACIA6850Emulator::ATACIA6850Emulator() {
}

ATACIA6850Emulator::~ATACIA6850Emulator() {
	Shutdown();
}

void ATACIA6850Emulator::Init(ATScheduler *sch) {
	mpScheduler = sch;
}

void ATACIA6850Emulator::Shutdown() {
	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventTransmit);
		mpScheduler = nullptr;
	}
}

void ATACIA6850Emulator::SetInterruptFn(const vdfunction<void(bool)>& fn) {
	mpInterruptFn = fn;
}

void ATACIA6850Emulator::SetTransmitFn(const vdfunction<void(uint8, uint32)>& fn) {
	mpTransmitFn = fn;
}

void ATACIA6850Emulator::SetControlFn(const vdfunction<void(bool, bool)>& fn) {
	mpControlFn = fn;
}

void ATACIA6850Emulator::SetMasterClockPeriod(uint32 cyclesPerByteX64) {
	mCyclesPerBitX64 = cyclesPerByteX64;
}

void ATACIA6850Emulator::SetCTS(bool asserted) {
	if (asserted)
		mStatus |= 0x08;
	else
		mStatus &= ~0x08;
}

void ATACIA6850Emulator::SetDCD(bool asserted) {
	if (asserted)
		mStatus |= 0x04;
	else
		mStatus &= ~0x04;
}

void ATACIA6850Emulator::ReceiveByte(uint8 data, uint32 cyclesPerBit) {
	// discard byte if clock not active
	if (!mCyclesPerBit)
		return;

	mReceiveData = data;

	// clear parity and framing error bits (these are updated on each byte
	// in the 6850, not sticky)
	mStatus &= 0xAF;

	// signal a framing error if baud rate is off by more than 5%
	if (cyclesPerBit && (uint32)abs((sint32)mCyclesPerBit - (sint32)cyclesPerBit) * 20 > mCyclesPerBit) {
		mReceiveShift = 0x55;
		mStatus |= 0x10;
	}

	// set receive data register full bit
	mStatus |= 0x01;

	UpdateIrqStatus();
}

void ATACIA6850Emulator::Reset() {
	if (mpInterruptFn)
		mpInterruptFn(false);

	// Clear everything except DCD and CTS bits, which are controlled
	// externally.
	mStatus &= 0x0C;
	mControl = 0;

	mpScheduler->UnsetEvent(mpEventTransmit);

	UpdateBaudRate();
}

uint8 ATACIA6850Emulator::DebugReadByte(uint8 address) const {
	if (address & 1)
		return mReceiveData;
	else
		return mStatus;
}

uint8 ATACIA6850Emulator::ReadByte(uint8 address) {
	if (address & 1) {
		// clear IRQ, overrun, and recv full bits
		mStatus &= ~0x21;
		UpdateIrqStatus();
		return mReceiveData;
	} else {
		return mStatus;
	}
}

void ATACIA6850Emulator::WriteByte(uint8 address, uint8 value) {
	if (address & 1) {		// transmit
		mTransmitData = value;
			
		// clear transmitter data empty status bit
		mStatus &= ~0x02;

		// load shifter now if it's idle
		if (!mbTransmitShiftBusy)
			LoadTransmitShifter();
	} else {				// control
		if (mControl != value) {
			const uint8 oldControl = mControl;
			const uint8 delta = value ^ oldControl;
			mControl = value;

			if (delta & 0x03)
				UpdateBaudRate();

			if (delta & 0xE0)
				UpdateIrqStatus();
		}
	}
}

void ATACIA6850Emulator::OnScheduledEvent(uint32 id) {
	switch(id) {
		case kEventId_Transmit:
			mpEventTransmit = nullptr;

			if (mbTransmitShiftBusy) {
				mbTransmitShiftBusy = false;

				if (mpTransmitFn)
					mpTransmitFn(mTransmitShift, mCyclesPerBit);
			}

			// Check if we have another byte to transmit
			if (!(mStatus & 0x02))
				LoadTransmitShifter();
			break;
	}
}

void ATACIA6850Emulator::LoadTransmitShifter() {
	mTransmitShift = mTransmitData;
	mbTransmitShiftBusy = true;

	// set transmit data empty
	mStatus |= 0x02;

	UpdateIrqStatus();

	// restart transmit timer
	if (mCyclesPerBit)
		mpScheduler->SetEvent(mCyclesPerBit * 10, this, kEventId_Transmit, mpEventTransmit);
}

void ATACIA6850Emulator::UpdateIrqStatus() {
	uint8 newIrqStatus = 0;

	// assert transmit IRQ if enabled and TDR empty
	if ((mControl & 0x60) == 0x20 && (mStatus & 0x02))
		newIrqStatus = 0x80;

	// assert receive IRQ if enabled and RDR full
	if ((mControl & 0x80) && (mStatus & 0x01))
		newIrqStatus = 0x80;

	// check if IRQ status changed
	if ((mStatus ^ newIrqStatus) & 0x80) {
		mStatus ^= 0x80;

		if (mpInterruptFn)
			mpInterruptFn(newIrqStatus != 0);
	}
}

void ATACIA6850Emulator::UpdateBaudRate() {
	switch(mControl & 3) {
		case 0:		// div1
			mCyclesPerBit = (mCyclesPerBitX64 + 32) >> 6;
			break;

		case 1:		// div16
			mCyclesPerBit = (mCyclesPerBitX64 + 2) >> 2;
			break;

		case 2:		// div64
			mCyclesPerBit = mCyclesPerBitX64;
			break;

		case 3:
			mCyclesPerBit = 0;
			break;
	}

	if (!mCyclesPerBit) {
		mpScheduler->UnsetEvent(mpEventTransmit);
		mbTransmitShiftBusy = false;
	} else {
		if (!mpEventTransmit)
			mpScheduler->SetEvent(mCyclesPerBit, this, kEventId_Transmit, mpEventTransmit);
	}
}
