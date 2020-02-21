//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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
#include "simeventmanager.h"

ATSimulatorEventManager::ATSimulatorEventManager() {
}

ATSimulatorEventManager::~ATSimulatorEventManager() {
}

void ATSimulatorEventManager::Shutdown() {
}

void ATSimulatorEventManager::AddCallback(IATSimulatorCallback *cb) {
	if (!cb) {
		VDFAIL("Invalid null callback.");
		return;
	}

	auto it = std::find(mCallbacks.begin(), mCallbacks.end(), cb);

	if (it == mCallbacks.end()) {
		// Add the new callback to the list. Note that we deliberately
		// aren't updating the size values in the iterator list; this
		// prevents the new entry from being called from an iteration
		// in progress.
		mCallbacks.push_back(cb);
	}
}

void ATSimulatorEventManager::RemoveCallback(IATSimulatorCallback *cb) {
	if (!cb)
		return;

	auto it = std::find(mCallbacks.begin(), mCallbacks.end(), cb);
	if (it != mCallbacks.end()) {
		const size_t index = (size_t)(it - mCallbacks.begin());

		// Normally, we would move the last entry into the current entry. We avoid
		// doing so because with our iteration style it could cause an entry to be
		// skipped or doubly called during an active iteration. We do need to decrement
		// the global size for any active iterations that have captured the current
		// entry.

		mCallbacks.erase(it);

		for(auto *p = mpIteratorList; p; p = p->mpNext) {
			if (p->mGlobalSize > index)
				--p->mGlobalSize;
		}
	}
}

uint32 ATSimulatorEventManager::AddEventCallback(ATSimulatorEvent ev, const vdfunction<void()>& fn) {
	if (ev <= kATSimEvent_None || ev >= kATSimEventCount) {
		VDFAIL("Invalid callback ID passed to AddEventCallback.");
		return 0;
	}

	// see if we have a free entry
	uint32 index = mECFreeList;
	if (!index) {
		mEventCallbackTable.push_back();
		index = (uint32)mEventCallbackTable.size();

		auto& ne = mEventCallbackTable.back();
		ne.mValidId = index + 0x1000000;
		ne.mNext = 0;
	}

	auto& e = mEventCallbackTable[index - 1];
	VDASSERT((e.mValidId & 0xFFFFFF) == index);
	VDASSERT(!e.mpFunction);

	mECFreeList = e.mNext;

	e.mpFunction = fn;
	e.mNext = mEventCallbackLists[ev - 1];
	e.mValidId += 0x1000000 + ((uint32)ev << 16);
	mEventCallbackLists[ev - 1] = index;

	return e.mValidId;
}

void ATSimulatorEventManager::RemoveEventCallback(uint32 id) {
	if (id == 0)
		return;

	uint32 index = id & 0xFFFF;

	if (!index || index > mEventCallbackTable.size()) {
		VDFAIL("Invalid callback ID passed to RemoveEventCallback.");
		return;
	}

	auto& e = mEventCallbackTable[index - 1];
	if (e.mValidId != id) {
		VDFAIL("Invalid callback ID passed to RemoveEventCallback.");
		return;
	}

	// check if we need to bump any active iterators
	for(auto *p = mpIteratorList; p; p = p->mpNext) {
		if (p->mEventIdx == index)
			p->mEventIdx = e.mNext;
	}

	ATSimulatorEvent ev = (ATSimulatorEvent)((id >> 16) & 0xFF);
	e.mpFunction = {};
	e.mValidId = (e.mValidId + 0x01000000) & 0xFF00FFFF;

	uint32& headIndex = mEventCallbackLists[(int)ev - 1];
	uint32 curIndex = headIndex;
	uint32 prevIndex = 0;
	for(;;) {
		if (curIndex == 0) {
			VDFAIL("Event callback not found in the list it is supposed to be registered in.");
			return;
		}

		if (curIndex == index)
			break;

		const auto& curEC = mEventCallbackTable[curIndex - 1];
		VDASSERT((curEC.mValidId & 0xFFFFFF) == (curIndex + (id & 0xFF0000)));

		prevIndex = curIndex;
		curIndex = curEC.mNext;
	}

	if (prevIndex)
		mEventCallbackTable[prevIndex - 1].mNext = e.mNext;
	else
		headIndex = e.mNext;

	e.mNext = mECFreeList;
	mECFreeList = index;
}

void ATSimulatorEventManager::NotifyEvent(ATSimulatorEvent ev) {
	if (ev == kATSimEvent_AnonymousInterrupt || !ev)
		return;

	Iterator it = { mpIteratorList, 0, mCallbacks.size(), mEventCallbackLists[ev - 1] };
	mpIteratorList = &it;

	while(it.mGlobalIdx < it.mGlobalSize) {
		IATSimulatorCallback *cb = mCallbacks[it.mGlobalIdx++];

		cb->OnSimulatorEvent(ev);
	}

	while(it.mEventIdx) {
		const auto& e = mEventCallbackTable[it.mEventIdx - 1];
		const auto& fn = e.mpFunction;

		it.mEventIdx = e.mNext;

		fn();
	}

	VDASSERT(mpIteratorList == &it);
	mpIteratorList = it.mpNext;
}
