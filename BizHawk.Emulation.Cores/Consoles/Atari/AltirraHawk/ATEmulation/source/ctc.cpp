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
#include <at/atcore/consoleoutput.h>
#include <at/atcore/scheduler.h>
#include <at/atcore/logging.h>
#include <at/atemulation/ctc.h>

ATLogChannel g_ATLCCTCWR(false, false, "CTCWR", "Z8430 Counter/Timer Circuit (CTC) writes");

ATCTCEmulator::ATCTCEmulator() {
}

ATCTCEmulator::~ATCTCEmulator() {
	Shutdown();
}

void ATCTCEmulator::Init(ATScheduler *sch) {
	mpScheduler = sch;
}

void ATCTCEmulator::Shutdown() {
	if (mpScheduler) {
		for(Channel& ch : mChannels) {
			mpScheduler->UnsetEvent(ch.mpUnderflowEvent);
		}

		mpScheduler->UnsetEvent(mpInterruptEvent);
		mpScheduler = nullptr;
	}
}

void ATCTCEmulator::DumpStatus(ATConsoleOutput& out) const {
	for(size_t i=0; i<vdcountof(mChannels); ++i) {
		const Channel& ch = mChannels[i];

		out("Channel %u: %s, count %3ux%-3u, tc %3u, trigger %s, int %s (%s), %s"
			, i
			, ch.mbCounterMode ? "counter" : "timer  "
			, ComputeCounter(ch)
			, ch.mbPrescale256 ? 256 : 16
			, ch.mTimeConstant
			, ch.mbRisingEdgeTrigger ? "rising " : "falling"
			, ch.mbInterruptEnabled ? "enabled" : "disabled"
			, ch.mbInterruptActive ? "asserted" : "negated"
			, ch.mbWriteTimeConstant ? "waiting for time constant" : "normal"
		);
	}
}

void ATCTCEmulator::Reset() {
	if (mpInterruptFn)
		mpInterruptFn(-1);

	for(Channel& ch : mChannels) {
		ch.mbInterruptActive = false;
		ResetChannel(ch);
	}
}

uint8 ATCTCEmulator::DebugReadByte(uint8 address) const {
	const Channel& ch = mChannels[address & 3];

	if (ch.mbTimerRunning)
		return ComputeCounter(ch);

	return ch.mCounter;
}

uint8 ATCTCEmulator::ReadByte(uint8 address) {
	return DebugReadByte(address);
}

void ATCTCEmulator::WriteByte(uint8 address, uint8 value) {
	g_ATLCCTCWR("CTC[%02X] = %02X\n", address, value);

	const uint32 channelIndex = address & 3;
	Channel& ch = mChannels[channelIndex];

	if (ch.mbWriteTimeConstant) {
		ch.mTimeConstant = value ? value : 256;
		ch.mbWriteTimeConstant = false;

		if (ch.mbCounterMode) {
			ch.mCounter = (uint8)ch.mTimeConstant;
			ch.mbCounterRunning = true;
			ch.mbEnabled = true;
		} else if (ch.mbAutomaticTrigger && !ch.mbEnabled) {
			ch.mbEnabled = true;
			ch.mbTimerRunning = true;
			ch.mCounter = (uint8)ch.mTimeConstant;
			ch.mTimerBase = mpScheduler->GetTick64();
		}

		UpdateInterruptTime(channelIndex, ch);
	} else if (value & 1) {
		if (value & 0x02) {
			ch.mCounter = ComputeCounter(ch);
			ch.mbTimerRunning = false;
			ch.mbCounterRunning = false;
			ch.mbEnabled = false;
		}
		
		ch.mbInterruptEnabled = (value & 0x80) != 0;
		ch.mbCounterMode = (value & 0x40) != 0;
		ch.mbPrescale256 = (value & 0x20) != 0;
		ch.mbRisingEdgeTrigger = (value & 0x10) != 0;
		ch.mbAutomaticTrigger = (value & 0x08) == 0;
		ch.mbWriteTimeConstant = (value & 0x04) != 0;

		if (!(value & 0x02)) {
			ch.mbEnabled = true;

			if (ch.mbCounterMode)
				ch.mbCounterRunning = true;
		}

		UpdateInterruptTime(channelIndex, ch);
	} else {
		mInterruptVector = value & 0xF8;
	}
}

void ATCTCEmulator::SetInputState(uint8 channel, bool state) {
	Channel& ch = mChannels[channel & 3];

	if (ch.mbInputState != state) {
		ch.mbInputState = state;

		if (ch.mbRisingEdgeTrigger == state)
			TriggerChannel(ch);
	}
}

void ATCTCEmulator::SetUnderflowFn(uint32 channel, const vdfunction<void()>& fn) {
	mChannels[channel].mpUnderflowFn = fn;

	UpdateInterruptTime(channel, mChannels[channel]);
}

void ATCTCEmulator::SetInterruptFn(const vdfunction<void(sint32)>& fn) {
	mpInterruptFn = fn;
}

void ATCTCEmulator::AcknowledgeInterrupt() {
	uint32 index = 0;
	for(Channel& ch : mChannels) {
		if (ch.mbInterruptActive) {
			ch.mbInterruptActive = false;
			UpdateInterruptTime(index, ch);
			UpdateGlobalInterruptTime();
			break;
		}

		++index;
	}
}

void ATCTCEmulator::OnScheduledEvent(uint32 id) {
	if (id == kEventId_Interrupt) {
		mpInterruptEvent = nullptr;

		AssertInterrupt();
	} else if (id >= kEventId_Underflow0 && id < kEventId_Underflow0 + 4) {
		const uint32 index = id - kEventId_Underflow0;
		Channel& ch = mChannels[index];

		ch.mpUnderflowEvent = nullptr;

		UpdateInterruptTime(index, ch);

		if (ch.mpUnderflowFn)
			ch.mpUnderflowFn();
	}
}

void ATCTCEmulator::ResetChannel(Channel& ch) {
	ch.mCounter = 0;
	ch.mTimeConstant = 0x100;
	ch.mbWriteTimeConstant = false;
	ch.mbEnabled = false;
	ch.mbTimerRunning = false;
	ch.mbCounterRunning = false;
	ch.mInterruptTime = 0;
	ch.mUnderflowTime = 0;

	mpScheduler->UnsetEvent(ch.mpUnderflowEvent);
}

void ATCTCEmulator::TriggerChannel(Channel& ch) {
	if (ch.mbCounterRunning) {
		--ch.mCounter;

		if (!ch.mCounter) {
			ch.mCounter = (uint8)ch.mTimeConstant;

			if (!ch.mbInterruptActive) {
				ch.mInterruptTime = mpScheduler->GetTick64();

				mpScheduler->UnsetEvent(mpInterruptEvent);
				AssertInterrupt();
			}
		}
	} else if (ch.mbEnabled && !ch.mbTimerRunning) {
		ch.mbTimerRunning = true;
		ch.mCounter = (uint8)ch.mTimeConstant;
		ch.mTimerBase = mpScheduler->GetTick64();
	}
}

uint8 ATCTCEmulator::ComputeCounter(const Channel& ch) const {
	// compute time elapsed since timer base
	uint64 dt = mpScheduler->GetTick64() - ch.mTimerBase;

	if (ch.mbPrescale256)
		dt >>= 8;
	else
		dt >>= 4;

	const uint32 baseCount = ch.mCounter ? ch.mCounter : 0x100;
	if (dt < baseCount)
		return (uint8)(baseCount - (uint8)dt);

	dt -= ch.mCounter;
	dt %= ch.mTimeConstant;

	return ch.mTimeConstant - (uint8)dt;
}

uint64 ATCTCEmulator::ComputeNextExpiration(const Channel& ch) const {
	if (!ch.mbTimerRunning)
		return 0;

	// compute time for initial expiration
	const uint32 baseCount = ch.mCounter ? ch.mCounter : 0x100;
	uint64 nextExpirationTime = ch.mTimerBase + (ch.mbPrescale256 ? baseCount << 8 : baseCount << 4);

	// check if we're in the repeating portion
	const uint64 t = mpScheduler->GetTick64();
	if (t >= nextExpirationTime) {
		// yes.. round time to next multiple
		const uint64 tickPeriod = ch.mbPrescale256 ? (uint64)ch.mTimeConstant << 8 : (uint64)ch.mTimeConstant << 4;
		const uint64 cycleDelta = t - nextExpirationTime;

		nextExpirationTime += cycleDelta - cycleDelta % tickPeriod;
		nextExpirationTime += tickPeriod;
	}

	return nextExpirationTime;
}

void ATCTCEmulator::UpdateInterruptTime(uint32 channelIndex, Channel& ch) {
	uint64 expirationTime = 0;
	uint64 interruptTime = 0;
	uint64 underflowTime = 0;

	if (ch.mbTimerRunning && (ch.mbInterruptEnabled || ch.mpUnderflowFn)) {
		expirationTime = ComputeNextExpiration(ch);

		if (ch.mbInterruptEnabled && !ch.mbInterruptActive)
			interruptTime = expirationTime;

		if (ch.mpUnderflowFn)
			underflowTime = expirationTime;
	}


	if (ch.mInterruptTime != interruptTime) {
		ch.mInterruptTime = interruptTime;

		UpdateGlobalInterruptTime();
	}

	if (ch.mUnderflowTime != underflowTime) {
		ch.mUnderflowTime = underflowTime;

		if (underflowTime) {
			const uint64 t = mpScheduler->GetTick64();
			mpScheduler->SetEvent(std::max<uint64>(underflowTime, t + 1) - t, this, kEventId_Underflow0 + channelIndex, ch.mpUnderflowEvent);
		} else
			mpScheduler->UnsetEvent(ch.mpUnderflowEvent);
	}
}

void ATCTCEmulator::UpdateGlobalInterruptTime() {
	uint64 interruptTime = UINT64_MAX;

	for(const Channel& ch : mChannels) {
		if (ch.mbInterruptActive)
			return;

		if (ch.mInterruptTime && ch.mInterruptTime < interruptTime)
			interruptTime = ch.mInterruptTime;
	}

	if (interruptTime != UINT64_MAX) {
		const uint64 t = mpScheduler->GetTick64();
		const uint64 delay = interruptTime <= t ? 1 : interruptTime - t;

		mpScheduler->SetEvent((uint32)delay, this, kEventId_Interrupt, mpInterruptEvent);
	}
}

void ATCTCEmulator::AssertInterrupt() {
	const uint64 t = mpScheduler->GetTick64();

	uint32 vec = mInterruptVector;
	for(Channel& ch : mChannels) {
		VDASSERT(!ch.mbInterruptActive);

		if (ch.mInterruptTime && ch.mInterruptTime <= t) {
			ch.mbInterruptActive = true;

			if (mpInterruptFn)
				mpInterruptFn(vec);
			break;
		}

		vec += 2;
	}
}
