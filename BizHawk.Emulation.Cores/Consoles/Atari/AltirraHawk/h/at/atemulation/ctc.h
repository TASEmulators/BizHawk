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

#ifndef f_AT_CTC_H
#define f_AT_CTC_H

#include <vd2/system/function.h>
#include <at/atcore/scheduler.h>

class ATCTCEmulator final : public IATSchedulerCallback {
public:
	enum : uint32 { kTypeID = 'ctc ' };

	ATCTCEmulator();
	~ATCTCEmulator();

	void Init(ATScheduler *sch);
	void Shutdown();

	void DumpStatus(ATConsoleOutput& out) const;

	void Reset();

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

	void SetInputState(uint8 channel, bool state);

	void SetUnderflowFn(uint32 channel, const vdfunction<void()>& fn);
	void SetInterruptFn(const vdfunction<void(sint32)>& fn);
	void AcknowledgeInterrupt();

public:
	virtual void OnScheduledEvent(uint32 id);

protected:
	enum : uint32 {
		kEventId_Interrupt = 1,
		kEventId_Underflow0,
	};

	struct Channel {
		uint8 mCounter = 0;
		uint16 mTimeConstant = 0;
		bool mbEnabled = false;
		bool mbInterruptEnabled = false;
		bool mbInterruptActive = false;
		bool mbCounterMode = false;
		bool mbRisingEdgeTrigger = false;
		bool mbAutomaticTrigger = false;
		bool mbTimerRunning = false;
		bool mbCounterRunning = false;
		bool mbWriteTimeConstant = false;
		bool mbPrescale256 = false;
		bool mbInputState = false;

		uint64 mTimerBase = 0;
		uint64 mInterruptTime = 0;
		uint64 mUnderflowTime = 0;

		ATEvent *mpUnderflowEvent = nullptr;
		vdfunction<void()> mpUnderflowFn;
	};

	void ResetChannel(Channel& ch);
	void TriggerChannel(Channel& ch);
	uint8 ComputeCounter(const Channel& ch) const;
	uint64 ComputeNextExpiration(const Channel& ch) const;
	void UpdateInterruptTime(uint32 channelIndex, Channel& ch);
	void UpdateGlobalInterruptTime();
	void AssertInterrupt();

	ATScheduler *mpScheduler = nullptr;
	ATEvent *mpInterruptEvent = nullptr;
	uint8 mInterruptVector = 0;

	Channel mChannels[4] = {};

	vdfunction<void(sint32)> mpInterruptFn;
};

#endif
