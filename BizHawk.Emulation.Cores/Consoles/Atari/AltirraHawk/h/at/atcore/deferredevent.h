//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2016 Avery Lee
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

#ifndef AT_ATCORE_DEFERREDEVENT_H
#define AT_ATCORE_DEFERREDEVENT_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/function.h>
#include <at/atcore/notifylist.h>

class ATDeferredEvent;

class ATDeferredEventManager {
	ATDeferredEventManager(const ATDeferredEventManager&) = delete;
	ATDeferredEventManager& operator=(const ATDeferredEventManager&) = delete;
public:
	ATDeferredEventManager();
	~ATDeferredEventManager();

	void AddPendingEvent(ATDeferredEvent *e);
	void RemovePendingEvent(ATDeferredEvent *e);
	void FlushPendingEvents();

private:
	ATNotifyList<ATDeferredEvent *> mPendingEvents;
};

class ATDeferredEvent {
	ATDeferredEvent(const ATDeferredEvent&) = delete;
	ATDeferredEvent& operator=(const ATDeferredEvent&) = delete;
public:
	ATDeferredEvent() = default;

	~ATDeferredEvent() {
		if (mbDeferredNotifyPending && mpManager)
			mpManager->RemovePendingEvent(this);
	}

	void Init(ATDeferredEventManager *mgr) {
		mpManager = mgr;

		if (mbDeferredNotifyPending)
			mpManager->AddPendingEvent(this);
	}

	void Shutdown() {
		mpManager = nullptr;
	}

	void operator+=(const vdfunction<void()> *fn);
	void operator-=(const vdfunction<void()> *fn);

	void NotifyDeferred() {
		if (!mbDeferredNotifyPending) {
			mbDeferredNotifyPending = true;

			if (mpManager)
				mpManager->AddPendingEvent(this);
		}
	}

	void Flush() {
		if (mbDeferredNotifyPending) {
			if (mpManager)
				mpManager->RemovePendingEvent(this);

			Notify();
		}
	}

	void Notify() {
		mbDeferredNotifyPending = false;
		mNotifyList.Notify([](const vdfunction<void()> *fn) -> bool { (*fn)(); return false; });
	}

private:
	bool mbDeferredNotifyPending = false;
	ATNotifyList<const vdfunction<void()> *> mNotifyList;
	ATDeferredEventManager *mpManager = nullptr;
};

#endif
