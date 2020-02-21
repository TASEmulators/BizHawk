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

#ifndef f_AT_ACIA_H
#define f_AT_ACIA_H

#include <vd2/system/function.h>
#include <at/atcore/scheduler.h>

class ATACIA6551Emulator : public IATSchedulerCallback {
public:
	ATACIA6551Emulator();
	~ATACIA6551Emulator();

	void Init(ATScheduler *sch, ATScheduler *slowsch);
	void Shutdown();

	void SetInterruptFn(const vdfunction<void(bool)>& fn);
	void SetReceiveReadyFn(const vdfunction<void()>& fn);
	void SetTransmitFn(const vdfunction<void(uint8, uint32)>& fn);
	void SetControlFn(const vdfunction<void(bool, bool)>& fn);

	bool IsReceiveReady() const;
	void ReceiveByte(uint8 data, uint32 baudRate);

	void Reset();

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

public:
	virtual void OnScheduledEvent(uint32 id);

protected:
	void LoadTransmitShifter();
	void UpdateBaudRate();

	enum {
		kEventId_Transmit = 1,
		kEventId_Receive = 2,
		kEventId_ReceivePoll = 3
	};

	uint8 mReceiveData;
	uint8 mReceiveShift;
	bool mbReceiveFramingError;
	uint8 mTransmitData;
	uint8 mTransmitShift;
	bool mbTransmitShiftBusy;
	bool mbControlLineChanged;
	uint8 mBufferedStatus;
	uint8 mStatus;
	uint8 mControl;
	uint8 mCommand;

	uint32 mBaudRate;
	uint32 mCyclesPerByte;

	ATScheduler *mpScheduler;
	ATScheduler *mpSlowScheduler;
	ATEvent *mpEventTransmit;
	ATEvent *mpEventReceive;
	ATEvent *mpEventReceivePoll;
	uint32 mPollRate;

	vdfunction<void(bool)> mpInterruptFn;
	vdfunction<void()> mpReceiveReadyFn;
	vdfunction<void(uint8, uint32)> mpTransmitFn;
	vdfunction<void(bool, bool)> mpControlFn;
};

#endif
