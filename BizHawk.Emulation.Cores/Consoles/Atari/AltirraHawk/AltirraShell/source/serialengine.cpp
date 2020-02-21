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

#include <stdafx.h>
#include <mmsystem.h>
#include <winioctl.h>
#include <vd2/system/error.h>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <at/atcore/logging.h>
#include "serialengine.h"
#include "serialhandler.h"

ATLogChannel g_ATSLCSerialEngine(true, false, "serengine", "Serial engine");

ATSSerialEngine::ATSSerialEngine() {
}

ATSSerialEngine::~ATSSerialEngine() {
}

void ATSSerialEngine::Init(IATSSerialHandler *p) {
	mpHandler = p;

	mhCommReadEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	if (!mhCommReadEvent)
		throw MyMemoryError();

	mhCommWriteEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	if (!mhCommWriteEvent)
		throw MyMemoryError();

	mhCommStatusEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	if (!mhCommStatusEvent)
		throw MyMemoryError();

	mOverlappedRead.hEvent = mhCommReadEvent;
	mOverlappedWrite.hEvent = mhCommWriteEvent;
	mOverlappedStatus.hEvent = mhCommStatusEvent;

	mbRun = true;

	ThreadStart();
}

void ATSSerialEngine::Shutdown() {
	if (isThreadAttached()) {
		PostRequest([this]() { mbRun = false; });

		HANDLE h = getThreadHandle();
		MSG msg;
		for(;;) {
			bool actionPending = false;

			DWORD r = MsgWaitForMultipleObjects(1, &h, FALSE, actionPending ? 0 : INFINITE, QS_SENDMESSAGE);

			if (r == WAIT_OBJECT_0)
				break;

			if (r == WAIT_OBJECT_0 + 1) {
				while(PeekMessage(&msg, NULL, 0, 0, PM_QS_SENDMESSAGE | PM_NOYIELD | PM_REMOVE)) {
					TranslateMessage(&msg);
					DispatchMessage(&msg);
				}
			}
		}

		ThreadWait();
	}

	if (mhCommWriteEvent) {
		CloseHandle(mhCommWriteEvent);
		mhCommWriteEvent = nullptr;
	}

	if (mhCommReadEvent) {
		CloseHandle(mhCommReadEvent);
		mhCommReadEvent = nullptr;
	}

	if (mhCommStatusEvent) {
		CloseHandle(mhCommStatusEvent);
		mhCommStatusEvent = nullptr;
	}
}

ATSSerialConfig ATSSerialEngine::GetConfig() const {
	return mConfig;
}

void ATSSerialEngine::SetConfig(const ATSSerialConfig& cfg) {
	mConfig = cfg;

	PostRequest([=]() { InternalSetConfig(mConfig); });
}

void ATSSerialEngine::SetConfig(ATSSerialConfig&& cfg) {
	mConfig = std::move(cfg);
}

void ATSSerialEngine::InternalSetConfig(const ATSSerialConfig& cfg) {
	mActiveConfig = cfg;

	if (mPortPath != cfg.mSerialPath) {
		ClosePort();

		mPortPath = cfg.mSerialPath;
		OpenPort();
	}

	if (mpHandler)
		mpHandler->OnConfigChanged(cfg);
}

const ATSSerialConfig& ATSSerialEngine::GetActiveConfig() const {
	return mActiveConfig;
}

const void *ATSSerialEngine::LockRead(uint32& len) {
	len = mReadCookedLevel;

	return len ? mReadBuffer + mReadCookedTail : nullptr;
}

void ATSSerialEngine::UnlockRead(uint32 len) {
	VDASSERT(len <= mReadCookedLevel);

	mReadCookedLevel -= len;
	mReadCookedTail += len;
	if (mReadCookedTail >= kReadBufferSize)
		mReadCookedTail -= kReadBufferSize;

	mReadBufferLevel -= len;
	if (mReadCookedLevel == 0) {
		uint32 slackSpace = mReadTail - mReadCookedHead;

		if (mReadTail < mReadCookedHead)
			slackSpace += kReadBufferSize;

		mReadBufferLevel -= slackSpace;

		mReadCookedHead = mReadTail;
		mReadCookedTail = mReadTail;
	}
}

void *ATSSerialEngine::LockWrite(uint32 len) {
	VDASSERT(len <= kWriteBufferSize);

	if (kWriteBufferSize - mWriteLevel < len)
		return nullptr;

	mWriteLockLen = len;
	return mWriteBuffer + mWriteHead;
}

void *ATSSerialEngine::LockWriteAny(uint32 len, uint32& avail) {
	if (mWriteLevel == kWriteBufferSize)
		return nullptr;

	mWriteLockLen = std::min<uint32>(len, kWriteBufferSize - mWriteLevel);
	avail = mWriteLockLen;

	return mWriteBuffer + mWriteHead;
}

void ATSSerialEngine::UnlockWrite(bool flush) {
	if (mWriteLockLen) {
		VDASSERT(mWriteLockLen + mWriteLevel <= kWriteBufferSize);

		if (mWriteHead + mWriteLockLen > kWriteBufferSize) {
			uint32 size1 = kWriteBufferSize - mWriteHead;
			uint32 size2 = mWriteLockLen - size1;

			memcpy(mWriteBuffer + kWriteBufferSize + mWriteHead, mWriteBuffer + mWriteHead, size1);
			memcpy(mWriteBuffer, mWriteBuffer + kWriteBufferSize, size2);

			mWriteHead = size2;
		} else {
			memcpy(mWriteBuffer + kWriteBufferSize + mWriteHead, mWriteBuffer + mWriteHead, mWriteLockLen);
			mWriteHead += mWriteLockLen;

			if (mWriteHead == kWriteBufferSize)
				mWriteHead = 0;
		}

		mWriteLevel += mWriteLockLen;
		
		if (!mbWritePending)
			QueueWrite();
	}

	mWriteLockLen = 0;
}

void ATSSerialEngine::SetBaudRate(uint32 baudRate) {
	if (mhPort == INVALID_HANDLE_VALUE)
		return;

	PurgeComm(mhPort, PURGE_RXABORT | PURGE_TXABORT);

	DCB dcb = {};
	dcb.DCBlength = sizeof(DCB);
	dcb.BaudRate = baudRate;
	dcb.fBinary = TRUE;
	dcb.ByteSize = 8;
	dcb.fAbortOnError = TRUE;

	for(;;) {
		if (SetCommState(mhPort, &dcb))
			break;

		DWORD err = GetLastError();

		if (err == ERROR_OPERATION_ABORTED) {
			ProcessCommErrors();
			continue;
		}

		VDASSERT(!err);
		break;
	}

	g_ATSLCSerialEngine("Baud rate set to %u baud\n", baudRate);
}

void ATSSerialEngine::PostRequest(vdfunction<void()>&& fn) {
	{
		VDCriticalSection::AutoLock lock(mMutex);
		mRequests.emplace_back(std::move(fn));
	}

	mRequestSignal.signal();
}

void ATSSerialEngine::SendRequest(vdfunction<void()>&& fn) {
	bool exit = false;
	DWORD tid = ::GetCurrentThreadId();

	{
		VDCriticalSection::AutoLock lock(mMutex);
		mRequests.emplace_back(std::move(fn));
		mRequests.emplace_back([=,&exit]() { exit = true; PostThreadMessage(tid, WM_NULL, 0, 0); });
	}

	mRequestSignal.signal();

	MSG msg;
	for(;;) {
		while(PeekMessage(&msg, NULL, 0, 0, PM_QS_SENDMESSAGE | PM_NOYIELD | PM_REMOVE)) {
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}

		if (exit)
			break;

		WaitMessage();
	}
}

void ATSSerialEngine::ThreadRun() {
	const double ticksToCyclesFx = 4294967296.0 * 1789772.5 / VDGetPreciseTicksPerSecond();
	const double cyclesToMilliseconds = 1000.0 / 1789772.5;

	mLastRealTime = VDGetPreciseTick();

	HANDLE h[4];
	h[0] = mRequestSignal.getHandle();
	h[1] = mhCommReadEvent;
	h[2] = mhCommStatusEvent;
	h[3] = mhCommWriteEvent;

	timeBeginPeriod(1);

	mpHandler->OnAttach(*this);

	if (!mPortPath.empty())
		OpenPort();

	while(mbRun) {
		uint32 dt1 = mScheduler.GetTicksToNextEvent();
		uint32 dt2 = mSlowScheduler.GetTicksToNextEvent() * 114 - mLastEmuSlowTimeAccum;
		uint32 timeoutDelay = (uint32)VDCeilToInt((double)std::min<uint32>(dt1, dt2) * cyclesToMilliseconds);

		DWORD result = WaitForMultipleObjectsEx(4, h, FALSE, timeoutDelay, TRUE);

		uint64 realTime = VDGetPreciseTick();
		uint64 rtDelta = realTime - mLastRealTime;
		mLastRealTime = realTime;

		if (rtDelta & (uint64(1) << 63))
			rtDelta = 0;

		mLastEmuTimeAccum += (uint64)VDRoundToInt64(ticksToCyclesFx * (double)rtDelta);

		uint32 dt = (uint32)(mLastEmuTimeAccum >> 32);
		mLastEmuTimeAccum = (uint32)mLastEmuTimeAccum;

		AdvanceTime(dt);

		if (result == WAIT_OBJECT_0)
			ProcessNextRequest();
		else if (result == WAIT_OBJECT_0+1)
			ProcessCommReadEvent();
		else if (result == WAIT_OBJECT_0+2)
			ProcessCommStatusEvent(true);
		else if (result == WAIT_OBJECT_0+3)
			ProcessCommWriteEvent();
		else if (result != WAIT_TIMEOUT)
			break;
	}
}

void ATSSerialEngine::AdvanceTime(uint32 dt) {
	while(dt) {
		uint32 q = dt;
		uint32 q1 = mScheduler.GetTicksToNextEvent();
		uint32 q2 = mSlowScheduler.GetTicksToNextEvent() * 114 + mLastEmuSlowTimeAccum;

		if (q > q1)
			q = q1;

		if (q > q2)
			q = q2;

		mScheduler.mNextEventCounter += q;
		if (!mScheduler.mNextEventCounter)
			mScheduler.ProcessNextEvent();

		mLastEmuSlowTimeAccum += q;
		uint32 slowq = mLastEmuSlowTimeAccum / 114;
		mLastEmuSlowTimeAccum %= 114;

		if (slowq) {
			mSlowScheduler.mNextEventCounter += slowq;
			mSlowScheduler.ProcessNextEvent();
		}

		dt -= q;
	}
}

void ATSSerialEngine::ProcessNextRequest() {
	for(;;) {
		vdfunction<void()> fn;
		mMutex.Lock();
		if (!mRequests.empty()) {
			fn = std::move(mRequests.front());
			mRequests.pop_front();
		}
		mMutex.Unlock();

		if (!fn)
			break;

		fn();
	}
}

void ATSSerialEngine::ProcessCommReadEvent() {
	ResetEvent(mhCommReadEvent);
	mbReadPending = false;

	DWORD actual;
	if (GetOverlappedResult(mhPort, &mOverlappedRead, &actual, TRUE))
		OnRead(actual);
	else if (GetLastError() == ERROR_OPERATION_ABORTED) {
		ProcessCommErrors();
	}
}

void ATSSerialEngine::CookReadData() {
	if (!mbInterleavedMode) {
		mReadCookedLevel += mReadLevel;
		mReadLevel = 0;
		mReadTail = mReadHead;
		mReadCookedHead = mReadTail;
		return;
	}

	while(mReadLevel) {
		uint8 c = mReadBuffer[mReadTail];

		switch(mCookState) {
			case kCookState_Escape:
				if (c == SERIAL_LSRMST_LSR_DATA) {
					mCookState = kCookState_LsrData1;
					break;
				} else if (c == SERIAL_LSRMST_LSR_NODATA) {
					mCookState = kCookState_LsrNoData;
					break;
				} else if (c == SERIAL_LSRMST_MST) {
					mCookState = kCookState_Mst;
					break;
				} else if (c == SERIAL_LSRMST_ESCAPE) {
					if ((mPendingLineStatus & mPendingModemStatus) >= 0)
						goto done;

					c = 0xFF;
				}

				mCookState = kCookState_Idle;
				// fall through

			case kCookState_Idle:
				if (c == 0xFF)
					mCookState = kCookState_Escape;
				else {
					if ((mPendingLineStatus & mPendingModemStatus) >= 0)
						goto done;

					mReadBuffer[mReadCookedHead] = mReadBuffer[mReadCookedHead + kReadBufferSize] = c;
					++mReadCookedLevel;
					if (++mReadCookedHead >= kReadBufferSize)
						mReadCookedHead = 0;
				}
				break;


			case kCookState_LsrData1:
				if ((mPendingLineStatus & mPendingModemStatus) >= 0)
					goto done;

				mPendingLineStatus = c;
				mCookState = kCookState_LsrData2;
				break;

			case kCookState_LsrData2:
				mReadBuffer[mReadCookedHead] = mReadBuffer[mReadCookedHead + kReadBufferSize] = c;
				mCookState = kCookState_Idle;
				break;

			case kCookState_LsrNoData:
				if ((mPendingLineStatus & mPendingModemStatus) >= 0)
					goto done;

				mPendingLineStatus = c;
				mCookState = kCookState_Idle;
				break;

			case kCookState_Mst:
				if ((mPendingLineStatus & mPendingModemStatus) >= 0)
					goto done;

				mPendingModemStatus = c;
				mCookState = kCookState_Idle;
				break;
		}

		if (++mReadTail >= kReadBufferSize)
			mReadTail = 0;

		--mReadLevel;
	}

done:
	QueueRead();
}

void ATSSerialEngine::OnRead(DWORD size) {
	if (size) {
		mReadLevel += size;
		VDASSERT(mReadLevel <= kReadBufferSize);

		mReadBufferLevel += size;
		VDASSERT(mReadBufferLevel <= kReadBufferSize);

		memcpy(mReadBuffer + kReadBufferSize + mReadHead, mReadBuffer + mReadHead, size);

		mReadHead += size;
		VDASSERT(mReadHead <= kReadBufferSize);
		if (mReadHead >= kReadBufferSize)
			mReadHead = 0;

		for(;;) {
			CookReadData();

			if (mReadCookedLevel) {
				uint32 level = mReadCookedLevel;

				mpHandler->OnReadDataAvailable(mReadCookedLevel);

				if (mReadCookedLevel == level)
					break;
			} else {
				if ((mPendingLineStatus & mPendingModemStatus) < 0)
					break;

				if (mPendingLineStatus >= 0) {
					const uint8 lineStatus = (uint8)mPendingLineStatus;
					mPendingLineStatus = -1;

					if (lineStatus & CE_FRAME)
						mpHandler->OnReadFramingError();
				}

				if (mPendingModemStatus >= 0) {
					uint8 status = (uint8)mPendingModemStatus;
					mPendingModemStatus = -1;

					UpdateControlStateDirect(status, 0);
				}
			}
		}

		if (mReadBufferLevel < kReadBufferSize)
			QueueRead();
	}
}

void ATSSerialEngine::QueueRead() {
	if (mhPort == INVALID_HANDLE_VALUE || mbReadPending)
		return;

	for(;;) {
		DWORD actual = 0;
		ResetEvent(&mOverlappedRead.hEvent);

		const uint32 rlen = std::min<uint32>(kReadBufferSize - mReadHead, kReadBufferSize - mReadBufferLevel);
		if (!rlen)
			break;

		if (!ReadFile(mhPort, mReadBuffer + mReadHead, 1, &actual, &mOverlappedRead)) {
			DWORD err = GetLastError();

			if (err == ERROR_OPERATION_ABORTED) {
				ProcessCommErrors();
				continue;
			}

			if (err == ERROR_IO_PENDING)
				mbReadPending = true;

			break;
		}

		if (!actual)
			break;

		OnRead(actual);
	}
}

void ATSSerialEngine::ProcessCommWriteEvent() {
	ResetEvent(mhCommWriteEvent);
	mbWritePending = false;

	DWORD actual;
	if (!GetOverlappedResult(mhPort, &mOverlappedWrite, &actual, TRUE)) {
		if (GetLastError() == ERROR_OPERATION_ABORTED)
			ProcessCommErrors();
	}

	OnWrite();
}

void ATSSerialEngine::OnWrite() {
	VDASSERT(mWriteLevel >= mWritePendingLen);
	mWriteLevel -= mWritePendingLen;

	mWriteTail += mWritePendingLen;
	VDASSERT(mWriteTail <= kWriteBufferSize);
	if (mWriteTail >= kWriteBufferSize)
		mWriteTail = 0;

	mWritePendingLen = 0;

	if (mWriteLevel)
		QueueWrite();

	mpHandler->OnWriteSpaceAvailable(kWriteBufferSize - mWriteLevel);
}

void ATSSerialEngine::QueueWrite() {
	if (mhPort == INVALID_HANDLE_VALUE || mbWritePending || !mWriteLevel)
		return;

	for(;;) {
		uint32 toWrite = mWriteLevel;

		if (toWrite > kWriteBufferSize - mWriteTail)
			toWrite = kWriteBufferSize - mWriteTail;

		DWORD actual = 0;
		ResetEvent(&mOverlappedWrite.hEvent);

		mWritePendingLen = toWrite;

		g_ATSLCSerialEngine("Queuing write of %u bytes (%02X)\n", toWrite, mWriteBuffer[mWriteTail]);
		if (!WriteFile(mhPort, mWriteBuffer + mWriteTail, toWrite, &actual, &mOverlappedWrite)) {
			DWORD err = GetLastError();

			if (err == ERROR_OPERATION_ABORTED) {
				ProcessCommErrors();
				continue;
			}

			if (err == ERROR_IO_PENDING)
				mbWritePending = true;

			break;
		}

		OnWrite();
	}
}

void ATSSerialEngine::ProcessCommStatusEvent(bool prevActive) {
	for(;;) {
		if (prevActive) {
			DWORD actual = 0;
			BOOL result = GetOverlappedResult(mhPort, &mOverlappedStatus, &actual, TRUE);
			if (!result) {
				DWORD err = GetLastError();
				if (err == ERROR_OPERATION_ABORTED) {
					ProcessCommErrors();
				} else {
					VDASSERT(!err);
				}
			}
		}

		uint32 evMask = mEventMask;
		mEventMask = 0;

		if (evMask & EV_ERR)
			ProcessCommErrors();

		ResetEvent(mhCommStatusEvent);

		BOOL result;
		for (;;) {
			result = WaitCommEvent(mhPort, &mEventMask, &mOverlappedStatus);
			if (result)
				break;

			if (!result) {
				DWORD err = GetLastError();
				if (err == ERROR_IO_PENDING)
					break;

				if (err == ERROR_OPERATION_ABORTED)
					ProcessCommErrors();
				else {
					VDASSERT(!err);
					break;
				}
			}
		}

		g_ATSLCSerialEngine("[%u] EvMask: %08X\n", VDGetAccurateTick(), evMask);

		if (evMask & EV_TXEMPTY)
			mpHandler->OnWriteBufferEmpty();

		if (evMask & EV_RXCHAR)
			QueueRead();

		if (evMask & (EV_RING | EV_CTS | EV_DSR | EV_RLSD)) {
			uint8 forceToggleBits = 0;

			if (evMask & EV_RING)
				forceToggleBits |= kATSerialCtlState_Command;

			UpdateControlState(forceToggleBits);
		}

		if (!result)
			break;
	}
}

void ATSSerialEngine::ProcessCommErrors() {
	DWORD errors = 0;
	if (ClearCommError(mhPort, &errors, NULL)) {
		if (errors & CE_FRAME)
			mpHandler->OnReadFramingError();
	}
}

void ATSSerialEngine::UpdateControlState(uint8 forceToggleBits) {
	DWORD status = 0;

	if (mhPort != INVALID_HANDLE_VALUE) {
		for(;;) {
			if (GetCommModemStatus(mhPort, &status))
				break;

			DWORD err = GetLastError();

			if (err == ERROR_OPERATION_ABORTED) {
				ProcessCommErrors();
				continue;
			}

			VDASSERT(!err);
			break;
		}

		g_ATSLCSerialEngine("CommStatus: %08X\n", status);
	}

	UpdateControlStateDirect(status, forceToggleBits);
}

void ATSSerialEngine::UpdateControlStateDirect(DWORD status, uint8 forceToggleBits) {
	uint8 state = 0;

	if (status & MS_RING_ON)
		state |= kATSerialCtlState_Command;

	if (state != mLastControlState) {
		mLastControlState = state;
		mpHandler->OnControlStateChanged(state);
	} else {
		mLastControlState ^= forceToggleBits;
		mpHandler->OnControlStateChanged(mLastControlState);
		mLastControlState ^= forceToggleBits;
		mpHandler->OnControlStateChanged(mLastControlState);
	}
}

void ATSSerialEngine::OpenPort() {
	if (mhPort != INVALID_HANDLE_VALUE)
		return;

	ResetEvent(mhCommStatusEvent);
	ResetEvent(mhCommReadEvent);
	ResetEvent(mhCommWriteEvent);

	mhPort = CreateFileW(mPortPath.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, NULL);
	if (mhPort == INVALID_HANDLE_VALUE)
		return;

	SetBaudRate(19200);

#if 0
	BYTE eventChar = 0xFF;
	BOOL interleavedModeSuccess = DeviceIoControl(mhPort, IOCTL_SERIAL_LSRMST_INSERT, &eventChar, 1, NULL, 0, NULL, &mOverlappedWrite);
	if (!interleavedModeSuccess) {
		if (GetLastError() == ERROR_IO_PENDING) {
			DWORD actual = 0;
			interleavedModeSuccess = GetOverlappedResult(mhPort, &mOverlappedWrite, &actual, TRUE);
		}
	}

	mbInterleavedMode = (interleavedModeSuccess != 0);
#else
	mbInterleavedMode = false;
#endif

	ResetEvent(mhCommWriteEvent);

	COMMTIMEOUTS ct = {};
	ct.ReadIntervalTimeout = MAXDWORD;
	ct.ReadTotalTimeoutMultiplier = 0;
	ct.ReadTotalTimeoutConstant = 0;
	ct.WriteTotalTimeoutMultiplier = 0;
	ct.WriteTotalTimeoutConstant = 0;

	SetCommTimeouts(mhPort, &ct);

	if (mbInterleavedMode)
		SetCommMask(mhPort, EV_RXCHAR | EV_TXEMPTY);
	else
		SetCommMask(mhPort, EV_CTS | EV_DSR | EV_RING | EV_RLSD | EV_RXCHAR | EV_TXEMPTY);

	mEventMask = 0;

	ProcessCommStatusEvent(false);

	UpdateControlState(0);

	QueueRead();

	g_ATSLCSerialEngine("Opened serial port: %ls\n", mPortPath.c_str());

	if (mbInterleavedMode)
		g_ATSLCSerialEngine("Serial port interleaved status mode is enabled.\n", mPortPath.c_str());
}

void ATSSerialEngine::ClosePort() {
	if (mhPort != INVALID_HANDLE_VALUE) {
		CancelIo(mhPort);
		CloseHandle(mhPort);
		mhPort = INVALID_HANDLE_VALUE;
	}
}
