//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

//#define TRACK_VTABLES

class ATEvent : public ATEventLink {
public:
	IATSchedulerCallback *mpCB;

#ifdef TRACK_VTABLES
	void *mpVtbl;
#endif

	uint32 mId;
	uint32 mNextTime;
};

ATScheduler::ATScheduler()
	: mNextEventCounter(0U-1000)
	, mTimeBase(0xFFF00000 + 1000)
	, mTick64Floor(mTimeBase + mNextEventCounter)
	, mpFreeEvents(NULL)
{
	mActiveEvents.mpNext = mActiveEvents.mpPrev = &mActiveEvents;
}

ATScheduler::~ATScheduler() {
}

void ATScheduler::ProcessNextEvent() {

	uint32 timeToNext = 100000;
	while(mActiveEvents.mpNext != &mActiveEvents) {
		ATEvent *ev = static_cast<ATEvent *>(mActiveEvents.mpNext);
		uint32 timeToNextEvent = ev->mNextTime - (mTimeBase + mNextEventCounter);

		VDASSERT(timeToNextEvent<100000000);

		if (timeToNextEvent) {
			if (timeToNext > timeToNextEvent)
				timeToNext = timeToNextEvent;
			break;
		}

		IATSchedulerCallback *cb = ev->mpCB;
		uint32 id = ev->mId;
		ev->mId = 0;

		VDASSERT(id);

		mActiveEvents.mpNext = ev->mpNext;
		mActiveEvents.mpNext->mpPrev = &mActiveEvents;

		ev->mpNext = mpFreeEvents;
		mpFreeEvents = ev;

		cb->OnScheduledEvent(id);
	}

	VDASSERT((uint32)(timeToNext - 1) < 100000);
	mTimeBase += mNextEventCounter;
	mNextEventCounter = 0U - timeToNext;
	mTimeBase -= mNextEventCounter;
}

void ATScheduler::SetEvent(uint32 ticks, IATSchedulerCallback *cb, uint32 id, ATEvent *&ptr) {
	if (ptr)
		RemoveEvent(ptr);

	ptr = AddEvent(ticks, cb, id);
}

void ATScheduler::UnsetEvent(ATEvent *&ptr) {
	if (ptr) {
		RemoveEvent(ptr);
		ptr = NULL;
	}
}

ATEvent *ATScheduler::AddEvent(uint32 ticks, IATSchedulerCallback *cb, uint32 id) {
	VDASSERT(ticks > 0 && ticks < 100000000);
	VDASSERT(id);

	ATEvent *ev;
	if (mpFreeEvents) {
		ev = static_cast<ATEvent *>(mpFreeEvents);
		mpFreeEvents = ev->mpNext;
	} else {
		ev = mAllocator.Allocate<ATEvent>();
		ev->mId = 0;
	}

	VDASSERT(!ev->mId);

	const uint32 t = mTimeBase + mNextEventCounter;

	ev->mpCB = cb;
	ev->mId = id;
	ev->mNextTime = t + ticks;

#ifdef TRACK_VTABLES
	ev->mpVtbl = *(void **)cb;
#endif

	ATEventLink *it = mActiveEvents.mpNext;
	for(; it != &mActiveEvents; it = it->mpNext) {
		ATEvent *ev2 = static_cast<ATEvent *>(it);

		if (ticks < ev2->mNextTime - t)
			break;
	}

	// adjust time base if we added a new event at the front
	if (it == mActiveEvents.mpNext) {
		mTimeBase += mNextEventCounter;
		mNextEventCounter = 0U - ticks;
		mTimeBase -= mNextEventCounter;
		VDASSERT((uint32)0-mNextEventCounter < 100000000);
	}

	ATEventLink *prev = it->mpPrev;
	prev->mpNext = ev;
	ev->mpPrev = prev;
	it->mpPrev = ev;
	ev->mpNext = it;

	return ev;
}

void ATScheduler::RemoveEvent(ATEvent *p) {
	bool wasFront = (mActiveEvents.mpNext == p);

	VDASSERT(p->mId);

	// unlink from active events
	ATEventLink *prev = p->mpPrev;
	ATEventLink *next = p->mpNext;
	prev->mpNext = next;
	next->mpPrev = prev;

	p->mId = 0;

	// free event
	p->mpNext = mpFreeEvents;
	mpFreeEvents = p;

	// check if we need to update the next time
	if (wasFront && p->mNextTime != (mTimeBase + mNextEventCounter))
		ProcessNextEvent();
}

int ATScheduler::GetTicksToEvent(ATEvent *ev) const {
	return ev->mNextTime - (mTimeBase + mNextEventCounter);
}

uint64 ATScheduler::GetTick64() const {
	uint32 tick32 = GetTick();
	uint64 tick64 = (mTick64Floor & 0xFFFFFFFF00000000ull) + tick32;

	if (tick32 < (uint32)mTick64Floor)
		tick64 += 0x100000000ull;

	return tick64;
}

void ATScheduler::UpdateTick64() {
	mTick64Floor = GetTick64();
}

uint32 ATScheduler::GetTicksToNextEvent() const {
	return uint32(0) - mNextEventCounter;
}
