//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - broadcast notification module
//	Copyright (C) 2009-2015 Avery Lee
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
#include <vd2/system/thread.h>
#include <vd2/system/vdstl.h>
#include "broadcaster.h"

struct ATSEventRegHandle : public vdlist_node {
	ATSEventImpl *mpEvent;
	vdfunction<void(const void *)> mpFn;

	ATSEventRegHandle(ATSEventImpl *pEvent, const vdfunction<void(const void *)>& fn)
		: mpEvent(pEvent)
		, mpFn(fn)
	{
	}
};

class ATSEventImpl {
public:
	ATSEventImpl();

	ATSEventRegHandle *Register(const vdfunction<void(const void *)>& fn);
	void Unregister(ATSEventRegHandle *p);
	void Raise(const void *args);

private:
	vdlist_node mRegistrations;

	struct ActiveIterator {
		ActiveIterator *mpNextIterator;
		vdlist_node *mpNextNode;
	};

	ActiveIterator *mpActiveIteratorList = nullptr;
	VDThreadID mThreadId;
};

ATSEventImpl::ATSEventImpl()
	: mThreadId(VDGetCurrentThreadID())
{
	mRegistrations.mListNodePrev = &mRegistrations;
	mRegistrations.mListNodeNext = &mRegistrations;
}

///////////////////////////////////////////////////////////////////////////

ATSEventRegHandle *ATSEventImpl::Register(const vdfunction<void(const void *)>& fn) {
	auto *p = new ATSEventRegHandle(this, fn);

	VDASSERT(mThreadId == VDGetCurrentThreadID());

	p->mListNodePrev = mRegistrations.mListNodePrev;
	p->mListNodeNext = &mRegistrations;
	p->mListNodePrev->mListNodeNext = p;
	mRegistrations.mListNodePrev = p;
	return p;
}

void ATSEventImpl::Unregister(ATSEventRegHandle *p) {
	VDASSERT(mThreadId == VDGetCurrentThreadID());

	for(auto *it = mpActiveIteratorList; it; it = it->mpNextIterator) {
		if (it->mpNextNode == p)
			it->mpNextNode = p->mListNodeNext;
	}

	vdlist_base::unlink(*p);

	delete p;
}

void ATSEventImpl::Raise(const void *args) {
	VDASSERT(mThreadId == VDGetCurrentThreadID());
	
	ActiveIterator it = { mpActiveIteratorList, mRegistrations.mListNodeNext };
	mpActiveIteratorList = &it;

	while(it.mpNextNode != &mRegistrations) {
		auto *p = static_cast<ATSEventRegHandle *>(it.mpNextNode);
		it.mpNextNode = p->mListNodeNext;

		p->mpFn(args);
	}

	VDASSERT(mpActiveIteratorList == &it);
	mpActiveIteratorList = it.mpNextIterator;
}

///////////////////////////////////////////////////////////////////////////

ATSEventImpl *ATSInitEvent(void (VDCDECL *deleter)()) {
	atexit(deleter);

	return new ATSEventImpl;
}

void ATSDestroyEvent(ATSEventImpl *p) {
	delete p;
}

ATSEventRegHandle *ATSRegisterForEvent(ATSEventImpl *eventImpl, const vdfunction<void(const void *)>& fn) {
	return eventImpl->Register(fn);
}

void ATSUnregisterForEvent(ATSEventRegHandle *p) {
	if (!p)
		return;

	p->mpEvent->Unregister(p);
}

void ATSRaiseEvent(ATSEventImpl *eventImpl, const void *args) {
	eventImpl->Raise(args);
}

ATSEventRegistration::~ATSEventRegistration() {
	Unregister();
}

void ATSEventRegistration::Unregister() {
	if (mpRegistration) {
		ATSUnregisterForEvent(mpRegistration);
		mpRegistration = nullptr;
	}
}
