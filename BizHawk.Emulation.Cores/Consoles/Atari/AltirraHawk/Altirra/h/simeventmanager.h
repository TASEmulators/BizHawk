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

#ifndef f_AT_SIMEVENTMANAGER_H
#define f_AT_SIMEVENTMANAGER_H

#include <vd2/system/function.h>
#include <vd2/system/vdstl.h>

enum ATSimulatorEvent {
	kATSimEvent_None,
	kATSimEvent_AnonymousInterrupt,
	kATSimEvent_CPUSingleStep,
	kATSimEvent_CPUStackBreakpoint,
	kATSimEvent_CPUPCBreakpoint,
	kATSimEvent_CPUPCBreakpointsUpdated,
	kATSimEvent_CPUIllegalInsn,
	kATSimEvent_CPUNewPath,
	kATSimEvent_ReadBreakpoint,
	kATSimEvent_WriteBreakpoint,
	kATSimEvent_DiskSectorBreakpoint,
	kATSimEvent_EndOfFrame,
	kATSimEvent_ScanlineBreakpoint,
	kATSimEvent_VerifierFailure,
	kATSimEvent_ColdReset,
	kATSimEvent_WarmReset,
	kATSimEvent_FrameTick,
	kATSimEvent_EXELoad,
	kATSimEvent_EXEInitSegment,
	kATSimEvent_EXERunSegment,
	kATSimEvent_StateLoaded,
	kATSimEvent_AbnormalDMA,
	kATSimEvent_VBI,
	kATSimEvent_VBLANK,
	kATSimEvent_TracingLimitReached,
	kATSimEventCount
};

class IATSimulatorCallback {
public:
	virtual void OnSimulatorEvent(ATSimulatorEvent ev) = 0;
};

class ATSimulatorEventManager {
	ATSimulatorEventManager(const ATSimulatorEventManager&) = delete;
	ATSimulatorEventManager& operator=(const ATSimulatorEventManager&) = delete;
public:
	ATSimulatorEventManager();
	~ATSimulatorEventManager();

	void Shutdown();

	void AddCallback(IATSimulatorCallback *cb);
	void RemoveCallback(IATSimulatorCallback *cb);

	uint32 AddEventCallback(ATSimulatorEvent ev, const vdfunction<void()>& fn);
	void RemoveEventCallback(uint32 id);

	void NotifyEvent(ATSimulatorEvent ev);

protected:
	typedef vdfastvector<IATSimulatorCallback *> Callbacks;

	struct Iterator {
		Iterator *mpNext;

		size_t mGlobalIdx;
		size_t mGlobalSize;
		uint32 mEventIdx;
	};

	Iterator	*mpIteratorList = nullptr;

	Callbacks	mCallbacks;

	struct EventCallback {
		uint32 mPrev;
		uint32 mNext;
		uint32 mValidId;
		vdfunction<void()> mpFunction;
	};

	vdvector<EventCallback> mEventCallbackTable;
	uint32		mECFreeList = 0;
	uint32		mEventCallbackLists[kATSimEventCount - 1] = {};
};

#endif	// f_AT_SIMEVENTMANAGER_H
