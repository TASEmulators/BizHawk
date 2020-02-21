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

#ifndef f_AT_ACIA6850_H
#define f_AT_ACIA6850_H

#include <vd2/system/function.h>
#include <at/atcore/scheduler.h>

class ATACIA6850Emulator : public IATSchedulerCallback {
public:
	ATACIA6850Emulator();
	~ATACIA6850Emulator();

	void Init(ATScheduler *sch);
	void Shutdown();

	void SetInterruptFn(const vdfunction<void(bool)>& fn);
	void SetTransmitFn(const vdfunction<void(uint8, uint32)>& fn);
	void SetControlFn(const vdfunction<void(bool, bool)>& fn);

	void SetMasterClockPeriod(uint32 cyclesPerBitX64);

	void SetCTS(bool asserted);
	void SetDCD(bool asserted);

	void ReceiveByte(uint8 data, uint32 cyclesPerBit);

	void Reset();

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

public:
	virtual void OnScheduledEvent(uint32 id);

protected:
	void LoadTransmitShifter();
	void UpdateIrqStatus();
	void UpdateBaudRate();

	enum {
		kEventId_Transmit = 1
	};

	uint8 mReceiveData = 0;
	uint8 mReceiveShift = 0;
	uint8 mTransmitData = 0;
	uint8 mTransmitShift = 0;
	bool mbTransmitShiftBusy = false;
	uint8 mStatus = 0;
	uint8 mControl = 0;

	uint32 mCyclesPerBit = 0;
	uint32 mCyclesPerBitX64 = 0;

	ATScheduler *mpScheduler = nullptr;
	ATEvent *mpEventTransmit = nullptr;

	vdfunction<void(bool)> mpInterruptFn;
	vdfunction<void(uint8, uint32)> mpTransmitFn;
	vdfunction<void(bool, bool)> mpControlFn;
};

#endif
