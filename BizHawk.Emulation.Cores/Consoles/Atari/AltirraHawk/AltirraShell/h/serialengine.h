//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - serial port interface engine
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

#ifndef f_ATS_SERIALENGINE_H
#define f_ATS_SERIALENGINE_H

#include <deque>
#include <windows.h>
#include <vd2/system/function.h>
#include <vd2/system/thread.h>
#include <vd2/system/VDString.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/scheduler.h>

#include "serialconfig.h"

class IATSSerialHandler;

class IATSSerialEngine {
public:
	virtual const ATSSerialConfig& GetActiveConfig() const = 0;

	virtual const void *LockRead(uint32& len) = 0;
	virtual void UnlockRead(uint32 len) = 0;

	virtual void *LockWrite(uint32 len) = 0;
	virtual void *LockWriteAny(uint32 len, uint32& avail) = 0;
	virtual void UnlockWrite(bool flush) = 0;

	virtual void SetBaudRate(uint32 baudRate) = 0;
	virtual void SetSIOProceed(bool asserted) = 0;
	virtual void SetSIOInterrupt(bool asserted) = 0;

	virtual ATScheduler& GetScheduler() = 0;
};

class ATSSerialEngine final : public VDThread, public IATSSerialEngine {
	ATSSerialEngine(const ATSSerialEngine&) = delete;
	ATSSerialEngine& operator=(const ATSSerialEngine&) = delete;
public:
	ATSSerialEngine();
	~ATSSerialEngine();

	void Init(IATSSerialHandler *p);
	void Shutdown();

	ATSSerialConfig GetConfig() const;
	void SetConfig(const ATSSerialConfig& cfg);
	void SetConfig(ATSSerialConfig&& cfg);

	void PostRequest(vdfunction<void()>&& fn);
	void SendRequest(vdfunction<void()>&& fn);

public:
	const ATSSerialConfig& GetActiveConfig() const override;
	const void *LockRead(uint32& len) override;
	void UnlockRead(uint32 len) override;

	void *LockWrite(uint32 len) override;
	void *LockWriteAny(uint32 len, uint32& avail) override;
	void UnlockWrite(bool flush) override;

	void SetBaudRate(uint32 baudRate) override;
	void SetSIOProceed(bool asserted) override {}
	void SetSIOInterrupt(bool asserted) override {}

	ATScheduler& GetScheduler() override { return mScheduler; }

private:
	enum CookState {
		kCookState_Idle,
		kCookState_Escape,
		kCookState_LsrData1,
		kCookState_LsrData2,
		kCookState_LsrNoData,
		kCookState_Mst
	};

	void InternalSetConfig(const ATSSerialConfig& cfg);

	void ThreadRun() override;

	void AdvanceTime(uint32 dt);

	void ProcessNextRequest();

	void ProcessCommReadEvent();
	void CookReadData();
	void OnRead(DWORD size);
	void QueueRead();

	void ProcessCommWriteEvent();
	void OnWrite();
	void QueueWrite();

	void ProcessCommErrors();
	void ProcessCommStatusEvent(bool prevActive);
	void UpdateControlState(uint8 forceToggleBits);
	void UpdateControlStateDirect(DWORD status, uint8 forceToggleBits);

	void OpenPort();
	void ClosePort();

	IATSSerialHandler *mpHandler = nullptr;

	bool		mbRun = false;
	VDSignal	mRequestSignal;
	VDStringW	mPortPath;
	HANDLE		mhPort = INVALID_HANDLE_VALUE;
	HANDLE		mhCommReadEvent = nullptr;
	HANDLE		mhCommWriteEvent = nullptr;
	HANDLE		mhCommStatusEvent = nullptr;
	DWORD		mEventMask = 0;
	OVERLAPPED	mOverlappedRead = {};
	OVERLAPPED	mOverlappedWrite = {};
	OVERLAPPED	mOverlappedStatus = {};

	uint8		mLastControlState = 0;

	bool		mbInterleavedMode = false;
	bool		mbReadPending = false;
	bool		mbWritePending = false;
	CookState	mCookState = kCookState_Idle;
	sint32		mPendingModemStatus = -1;
	sint32		mPendingLineStatus = -1;
	uint32		mReadCookedLevel = 0;
	uint32		mReadCookedTail = 0;
	uint32		mReadCookedHead = 0;
	uint32		mReadLevel = 0;
	uint32		mReadBufferLevel = 0;
	uint32		mReadTail = 0;
	uint32		mReadHead = 0;
	uint32		mReadLockLen = 0;
	uint32		mWriteLevel = 0;
	uint32		mWriteTail = 0;
	uint32		mWriteHead = 0;
	uint32		mWritePendingLen = 0;
	uint32		mWriteLockLen = 0;

	uint64		mLastRealTime = 0;
	uint64		mLastEmuTimeAccum = 0;
	uint32		mLastEmuSlowTimeAccum = 0;

	ATScheduler	mScheduler;
	ATScheduler	mSlowScheduler;

	static const uint32 kReadBufferSize = 16384;
	static const uint32 kWriteBufferSize = 16384;

	uint8		mReadBuffer[kReadBufferSize * 2];
	uint8		mWriteBuffer[kWriteBufferSize * 2];

	VDCriticalSection mMutex;
	std::deque<vdfunction<void()>> mRequests;

	ATSSerialConfig mConfig;
	ATSSerialConfig mActiveConfig;
};

#endif
