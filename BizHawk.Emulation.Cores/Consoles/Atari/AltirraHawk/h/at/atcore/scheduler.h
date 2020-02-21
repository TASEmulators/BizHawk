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

#ifndef AT_SCHEDULER_H
#define AT_SCHEDULER_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/fraction.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/linearalloc.h>

// Advances the scheduler time by one cycle and executes pending events.
#define ATSCHEDULER_ADVANCE(pThis) if(++static_cast<ATScheduler *>(pThis)->mNextEventCounter);else((pThis)->ProcessNextEvent()); VDASSERT((pThis)->mNextEventCounter >= 0x80000000);
#define ATSCHEDULER_ADVANCE_N(pThis, amount) if(static_cast<ATScheduler *>(pThis)->mNextEventCounter += static_cast<uint32>((amount)));else((pThis)->ProcessNextEvent()); VDASSERT((pThis)->mNextEventCounter >= 0x80000000);

// Returns the number of cycles to next event
#define ATSCHEDULER_GETTIMETONEXT(pThis) (uint32(0)-static_cast<ATScheduler *>(pThis)->mNextEventCounter)

// Returns the absolute time currently maintained by the scheduler.
#define ATSCHEDULER_GETTIME(pThis) (static_cast<const ATScheduler *>(pThis)->mNextEventCounter + (pThis)->mTimeBase)

class IATSchedulerCallback {
public:
	// Called when an event has fired.
	//
	// IMPORTANT: The event is implicitly removed when it is fired, so RemoveEvent() must NOT
	// be called on the event. This is not checked for performance reasons. This means that
	// the first thing OnScheduledEvent() should generally do is null out the pointer to the
	// event.
	//
	virtual void OnScheduledEvent(uint32 id) = 0;
};

class ATEvent;

struct ATEventLink {
	ATEventLink *mpNext;
	ATEventLink *mpPrev;
};

// The scheduler's job is to maintain a timer queue for simulation events. It is among one of
// the most time-critical pieces of the emulator since the main scheduler instance runs at
// machine cycle speed and is therefore called extremely frequently. For instance, when
// POKEY IRQs are enabled, an event is scheduled each time the timer rolls over. As such, it
// is important that the scheduler be fast both at dispatching events and also at adding
// and removing them.
//
// The emulator maintains two schedulers, one at cycle speed (1.79MHz) and another at scanline
// speed (15.7KHz). The second is used for events that have much longer delays and have lower
// precision requirements, thus lowering the load on the fast scheduler.
//
// The scheduler also maintains the master cycle clock of the system. One use for this is to
// maintain time delays passively by means of timestamp deltas, where the timestamp advances
// at 1.79MHz rate. This avoids the need to schedule events entirely. The disk drive emulation
// code does this to track disk rotation, since nothing actually needs to happen directly on the
// index mark. Scheduler time is always monotonic, even when save states are restored.
//
// == Wrap warning ==
// Differencing timestamps is a powerful and low-cost way to manage delays. However, at
// machine cycle rate (1.79MHz), the 32-bit counter wraps in 20 minutes. At turbo speeds this
// can be less than a minute of real time. This means that systems that use this technique
// need to handle wrapping. One way is to get a periodic callback to ensure that any really
// old timestamps are caught up. The other way is to use GetTick64() to use a 64-bit timestamp.
//
class ATScheduler {
	ATScheduler(const ATScheduler&) = delete;
	ATScheduler& operator=(const ATScheduler&) = delete;
public:
	ATScheduler();
	~ATScheduler();

	void ProcessNextEvent();

	// Removes a pending event currently held by a pointer, if any, and replaces it with a new
	// event. Equivalent to UnsetEvent + AddEvent.
	void SetEvent(uint32 ticks, IATSchedulerCallback *cb, uint32 id, ATEvent *&ptr);

	// Removes a pending event currently held by a pointer variable, if any, and nulls the
	// pointer afterward.
	void UnsetEvent(ATEvent *&ptr);

	// Add a new event to the scheduler with the specified delay. The event delay must be
	// non-zero. id is a callback-specific value to identify the event and may be any
	// arbitrary value.
	ATEvent	*AddEvent(uint32 ticks, IATSchedulerCallback *cb, uint32 id);

	// Removes an event from the scheduler. It is an error to call this method with null,
	// an event already removed, or an event that has already fired.
	void	RemoveEvent(ATEvent *);

	uint32	GetTick() const { return mNextEventCounter + mTimeBase; }
	int		GetTicksToEvent(ATEvent *) const;

	// 64-bit ticks are useful in circumstances where performance is less critical and
	// dealing with wrapping is annoying. At machine cycle rate, a 32-bit counter rolls
	// over in 40 minutes, while a 64-bit tick counter will take about 326,000 years.
	// Should be enough. UpdateTick64() must be called by a central service no more than
	// 2^32-1 cycles apart to update the 64-bit time base. It's fairly cheap as it just
	// stores the result of GetTick64().
	uint64	GetTick64() const;
	void	UpdateTick64();

	uint32	GetTicksToNextEvent() const;

	void SetRate(const VDFraction& f) { mRate = f; }
	VDFraction GetRate() const { return mRate; }

public:
	uint32	mNextEventCounter;
	uint32	mTimeBase;

protected:
	ATEventLink mActiveEvents;
	ATEventLink *mpFreeEvents;

	uint64		mTick64Floor;
	VDFraction	mRate;

	VDLinearAllocator mAllocator;
};

#endif
