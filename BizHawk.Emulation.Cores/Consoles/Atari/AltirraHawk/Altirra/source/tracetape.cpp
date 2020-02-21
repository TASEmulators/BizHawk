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
#include "tracetape.h"

ATTraceChannelTape::ATTraceChannelTape(uint64 tickOffset, double tickScale, const wchar_t *name, bool turbo, double samplesPerSec)
	: mTickOffset(tickOffset)
	, mTickScale(tickScale)
	, mName(name)
	, mSamplesPerSec(samplesPerSec)
	, mbTurbo(turbo)
{
}

const ATTraceChannelTape::EventInfo& ATTraceChannelTape::GetLastEvent() const {
	return *std::prev(mIt);
}

void ATTraceChannelTape::AddEvent(uint64 start, EventType eventType, uint32 pos) {
	mEvents.push_back(
		EventInfo {
			(double)(start - mTickOffset) * mTickScale,
			1e+10,
			eventType,
			pos
		}
	);
}

void ATTraceChannelTape::TruncateLastEvent(uint64 tick) {
	if (!mEvents.empty()) {
		const double t = (double)(tick - mTickOffset) * mTickScale;
		EventInfo& ev = mEvents.back();

		if (ev.mEndTime > t)
			ev.mEndTime = t;
	}
}

void *ATTraceChannelTape::AsInterface(uint32 iid) {
	return nullptr;
}

const wchar_t *ATTraceChannelTape::GetName() const {
	return mName.c_str();
}

double ATTraceChannelTape::GetDuration() const {
	return mEvents.empty() ? 0 : mEvents.back().mEndTime;
}

bool ATTraceChannelTape::IsEmpty() const {
	return mEvents.empty();
}

void ATTraceChannelTape::StartIteration(double startTime, double endTime, double eventThreshold) {
	auto it = std::lower_bound(mEvents.cbegin(), mEvents.cend(), startTime,
		[](const EventInfo& ev, double t) { return ev.mStartTime < t; });

	if (it != mEvents.begin() && std::prev(it)->mEndTime > startTime - mIterThreshold)
		--it;

	mIt = it;
	mIterEndTime = endTime;
	mIterThreshold = eventThreshold;
}

bool ATTraceChannelTape::GetNextEvent(ATTraceEvent& ev) {
	if (mIt == mEvents.end())
		return false;

	const EventInfo& sev = *mIt;
	++mIt;

	if (sev.mStartTime >= mIterEndTime) {
		mIt = mEvents.end();
		return false;
	}

	ev.mEventStart = sev.mStartTime;
	ev.mEventStop = sev.mEndTime;
	ev.mFgColor = 0;
	ev.mBgColor = sev.mEventType == kEventType_Record ? kATTraceColor_Tape_Record : kATTraceColor_Tape_Play;

	if (sev.mEndTime - sev.mStartTime < mIterThreshold) {
		double t = sev.mEndTime;
		ev.mpName = nullptr;

		while(mIt != mEvents.end()) {
			const EventInfo& sev2 = *mIt;

			if (sev2.mEndTime - t > mIterThreshold || sev2.mEndTime - sev2.mStartTime >= mIterThreshold)
				break;

			t = sev2.mEndTime;
			++mIt;

			if (t >= mIterEndTime)
				break;
		}

		ev.mEventStop = t;
	} else {
		ev.mpName = sev.mEventType == kEventType_Record ? L"Record" : L"Play";
	}

	return true;
}
