//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_AT_RIOT_H
#define f_AT_RIOT_H

#include <vd2/system/function.h>
#include <at/atcore/scheduler.h>

class ATConsoleOutput;

class ATRIOT6532Emulator final : public IATSchedulerCallback {
public:
	enum : uint32 { kTypeID = 'RIOT' };

	ATRIOT6532Emulator();
	~ATRIOT6532Emulator();

	void SetOnIrqChanged(const vdfunction<void(bool)>& fn) {
		mpFnIrqChanged = fn;
	}

	void Init(ATScheduler *sch);
	void Shutdown();

	void DumpStatus(ATConsoleOutput& out);

	void Reset();

	void SetInputA(uint8 value, uint8 mask);
	void SetInputB(uint8 value, uint8 mask);

	uint8 ReadOutputA() const {
		return mInputA & (mORA | ~mDDRA);
	}

	uint8 ReadOutputB() const {
		return ((mInputB ^ mORB) & mDDRB) ^ mInputB;
	}

	uint8 DebugReadByte(uint8 address) const;
	uint8 ReadByte(uint8 address);
	void WriteByte(uint8 address, uint8 value);

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	void UpdateTimerIrqEvent();
	void UpdateIrqStatus();

	ATScheduler *mpScheduler = nullptr;
	ATEvent *mpTimerIrqEvent = nullptr;

	uint8 mInputA = 0xFF;
	uint8 mInputB = 0xFF;
	uint8 mORA = 0;
	uint8 mORB = 0;
	uint8 mDDRA = 0;
	uint8 mDDRB = 0;
	bool mbEdgePositive = false;
	bool mbEdgeIrqActive = false;
	bool mbEdgeIrqEnabled = false;
	bool mbTimerIrqActive = false;
	bool mbTimerIrqEnabled = false;
	bool mbIrqActive = false;
	int mTimerPrescalerShift = 0;

	uint64 mTimerDeadline = 0;

	vdfunction<void(bool)> mpFnIrqChanged;
};

#endif
