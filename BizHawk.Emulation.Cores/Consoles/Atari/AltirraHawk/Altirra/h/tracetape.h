//	Altirra - Atari 800/800XL/5200 emulator
//	Execution trace data structures - tape
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

#ifndef f_AT_TRACETAPE_H
#define f_AT_TRACETAPE_H

#include "trace.h"

class ATTraceChannelTape : public vdrefcounted<IATTraceChannel> {
public:
	enum EventType {
		kEventType_Play,
		kEventType_Record
	};

	struct EventInfo {
		double mStartTime;
		double mEndTime;
		EventType mEventType;
		uint32 mPosition;
	};

	ATTraceChannelTape(uint64 tickOffset, double tickScale, const wchar_t *name, bool turbo, double samplesPerSec);

	bool IsTurbo() const { return mbTurbo; }

	// Returns the number of tape samples per second.
	double GetSamplesPerSec() const { return mSamplesPerSec; }

	const EventInfo& GetLastEvent() const;
	void AddEvent(uint64 tickStart, EventType eventType, uint32 pos);
	void TruncateLastEvent(uint64 tick);

	void *AsInterface(uint32 iid) override;

	const wchar_t *GetName() const override final;
	double GetDuration() const override final;
	bool IsEmpty() const override final;
	void StartIteration(double startTime, double endTime, double eventThreshold) override final;
	bool GetNextEvent(ATTraceEvent& ev) override final;

private:
	vdfastdeque<EventInfo> mEvents;
	vdfastdeque<EventInfo>::const_iterator mIt;
	double mIterEndTime;
	double mIterThreshold;
	double mTickScale;
	uint64 mTickOffset;
	double mSamplesPerSec;
	VDStringW mName;
	bool mbTurbo;
};

#endif
