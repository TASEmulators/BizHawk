//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2007 Avery Lee
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

#include <windows.h>
#include <mmsystem.h>
#include <vd2/system/w32assist.h>
#include "displaymgr.h"

///////////////////////////////////////////////////////////////////////////
VDVideoDisplayClient::VDVideoDisplayClient()
	: mpManager(NULL)
	, mbPreciseMode(false)
	, mbTicksEnabled(false)
	, mbRequiresFullScreen(false)
{
}

VDVideoDisplayClient::~VDVideoDisplayClient() {
}

void VDVideoDisplayClient::Attach(VDVideoDisplayManager *pManager) {
	VDASSERT(!mpManager);
	mpManager = pManager;
	if (mbTicksEnabled)
		mpManager->ModifyTicksEnabled(true);
	if (mbPreciseMode)
		mpManager->ModifyPreciseMode(true);
}

void VDVideoDisplayClient::Detach(VDVideoDisplayManager *pManager) {
	VDASSERT(mpManager == pManager);
	if (mbPreciseMode)
		mpManager->ModifyPreciseMode(false);
	if (mbTicksEnabled)
		mpManager->ModifyTicksEnabled(false);
	mpManager = NULL;
}

void VDVideoDisplayClient::SetPreciseMode(bool enabled) {
	if (mbPreciseMode == enabled) {
		if (enabled)
			mpManager->ReaffirmPreciseMode();
		return;
	}

	mbPreciseMode = enabled;
	mpManager->ModifyPreciseMode(enabled);
}

void VDVideoDisplayClient::SetTicksEnabled(bool enabled) {
	if (mbTicksEnabled == enabled)
		return;

	mbTicksEnabled = enabled;
	mpManager->ModifyTicksEnabled(enabled);
}

void VDVideoDisplayClient::SetRequiresFullScreen(bool enabled) {
	if (mbRequiresFullScreen == enabled)
		return;

	mbRequiresFullScreen = enabled;
}

const uint8 *VDVideoDisplayClient::GetLogicalPalette() const {
	return mpManager->GetLogicalPalette();
}

HPALETTE VDVideoDisplayClient::GetPalette() const {
	return mpManager->GetPalette();
}

void VDVideoDisplayClient::RemapPalette() {
	mpManager->RemapPalette();
}

///////////////////////////////////////////////////////////////////////////

VDVideoDisplayManager::VDVideoDisplayManager()
	: mTicksEnabledCount(0)
	, mPreciseModeCount(0)
	, mPreciseModePeriod(0)
	, mPreciseModeLastUse(0)
	, mhPalette(NULL)
	, mWndClass(NULL)
	, mhwnd(NULL)
	, mbMultithreaded(false)
	, mbAppActive(false)
	, mbBackgroundFallbackEnabled(true)
	, mThreadID(0)
	, mOutstandingTicks(0)
{
}

VDVideoDisplayManager::~VDVideoDisplayManager() {
	Shutdown();
}

bool VDVideoDisplayManager::Init() {
	mbAppActive = !mbBackgroundFallbackEnabled || VDIsForegroundTaskW32();

	if (!mbMultithreaded) {
		if (!RegisterWindowClass()) {
			Shutdown();
			return false;
		}

		mhwnd = CreateWindowEx(WS_EX_NOPARENTNOTIFY, (LPCTSTR)mWndClass, L"", WS_OVERLAPPEDWINDOW, 0, 0, 0, 0, NULL, NULL, VDGetLocalModuleHandleW32(), this);
		if (!mhwnd) {
			Shutdown();
			return false;
		}

		mThreadID = VDGetCurrentThreadID();
	}

	if (!ThreadStart()) {
		Shutdown();
		return false;
	}

	mStarted.wait();

	if (mbMultithreaded) {
		mThreadID = getThreadID();
	}

	return true;
}

void VDVideoDisplayManager::Shutdown() {
	VDASSERT(mClients.empty());

	if (isThreadAttached()) {
		PostThreadMessage(getThreadID(), WM_QUIT, 0, 0);
		ThreadWait();
	}

	if (!mbMultithreaded) {
		if (mhwnd) {
			DestroyWindow(mhwnd);
			mhwnd = NULL;
		}

		UnregisterWindowClass();
		mThreadID = 0;
	}
}

void VDVideoDisplayManager::SetProfileHook(const vdfunction<void(IVDVideoDisplay::ProfileEvent)>& profileHook) {
	mpProfileHook = profileHook;
}

void VDVideoDisplayManager::SetBackgroundFallbackEnabled(bool enabled) {
	if (mhwnd)
		PostMessage(mhwnd, WM_USER+101, enabled, 0);
}

void VDVideoDisplayManager::RemoteCall(void (*function)(void *), void *data) {
	if (VDGetCurrentThreadID() == mThreadID) {
		function(data);
		return;
	}

	RemoteCallNode node;
	node.mpFunction = function;
	node.mpData = data;

	vdsynchronized(mMutex) {
		mRemoteCalls.push_back(&node);
	}

	PostThreadMessage(getThreadID(), WM_NULL, 0, 0);

	HANDLE h = node.mSignal.getHandle();
	for(;;) {
		DWORD dwResult = MsgWaitForMultipleObjects(1, &h, FALSE, INFINITE, QS_SENDMESSAGE);

		if (dwResult != WAIT_OBJECT_0+1)
			break;

		MSG msg;
		while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE | PM_QS_SENDMESSAGE)) {
			TranslateMessage(&msg);
			DispatchMessage(&msg);
		}
	}
}

void VDVideoDisplayManager::AddClient(VDVideoDisplayClient *pClient) {
	VDASSERT(VDGetCurrentThreadID() == mThreadID);
	mClients.push_back(pClient);
	pClient->Attach(this);
}

void VDVideoDisplayManager::RemoveClient(VDVideoDisplayClient *pClient) {
	VDASSERT(VDGetCurrentThreadID() == mThreadID);
	pClient->Detach(this);
	mClients.erase(mClients.fast_find(pClient));
}

void VDVideoDisplayManager::ModifyPreciseMode(bool enabled) {
	VDASSERT(VDGetCurrentThreadID() == mThreadID);

	if (!mbMultithreaded) {
		return;
	}

	if (enabled) {
		int rc = ++mPreciseModeCount;
		VDASSERT(rc < 100000);
		if (rc == 1) {
			if (mbMultithreaded)
				EnterPreciseMode();
			else {
				ReaffirmPreciseMode();
				PostThreadMessage(getThreadID(), WM_NULL, 0, 0);
			}
		}
	} else {
		int rc = --mPreciseModeCount;
		VDASSERT(rc >= 0);
		if (!rc)
			ExitPreciseMode();
	}
}

void VDVideoDisplayManager::ModifyTicksEnabled(bool enabled) {
	VDASSERT(VDGetCurrentThreadID() == mThreadID);
	if (enabled) {
		int rc = ++mTicksEnabledCount;
		VDASSERT(rc < 100000);

		if (rc == 1) {
			PostThreadMessage(getThreadID(), WM_NULL, 0, 0);
			if (!mbMultithreaded)
				mTickTimerId = SetTimer(mhwnd, kTimerID_Tick, 10, NULL);
		}
	} else {
		int rc = --mTicksEnabledCount;
		VDASSERT(rc >= 0);

		if (!rc) {
			if (!mbMultithreaded && mTickTimerId) {
				KillTimer(mhwnd, mTickTimerId);
				mTickTimerId = 0;
			}
		}
	}
}

void VDVideoDisplayManager::ThreadRun() {
	if (mbMultithreaded)
		ThreadRunFullRemote();
	else
		ThreadRunTimerOnly();
}

void VDVideoDisplayManager::ThreadRunFullRemote() {
	if (RegisterWindowClass()) {
		mhwnd = CreateWindowEx(WS_EX_NOPARENTNOTIFY, (LPCTSTR)mWndClass, L"", WS_OVERLAPPEDWINDOW, 0, 0, 0, 0, NULL, NULL, VDGetLocalModuleHandleW32(), this);

		if (mhwnd) {
			MSG msg;
			PeekMessage(&msg, NULL, 0, 0, PM_NOREMOVE);
			mThreadID = VDGetCurrentThreadID();
			mStarted.signal();

			bool timerActive = false;
			for(;;) {
				DWORD ret = MsgWaitForMultipleObjects(0, NULL, TRUE, 1, QS_ALLINPUT);

				if (ret == WAIT_OBJECT_0) {
					bool success = false;
					while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
						if (msg.message == WM_QUIT)
							goto xit;
						success = true;
						TranslateMessage(&msg);
						DispatchMessage(&msg);
					}

					DispatchRemoteCalls();

					if (success)
						continue;

					ret = WAIT_TIMEOUT;
					::Sleep(1);
				}
				
				if (ret == WAIT_TIMEOUT) {
					if (mTicksEnabledCount > 0) {
						if (!timerActive) {
							timerActive = true;
							mTickTimerId = SetTimer(mhwnd, kTimerID_Tick, 10, NULL);
						}
						DispatchTicks();
					} else {
						if (timerActive) {
							timerActive = false;
							if (mTickTimerId) {
								KillTimer(mhwnd, mTickTimerId);
								mTickTimerId = 0;
							}
						}
						WaitMessage();
					}
				} else
					break;
			}
xit:
			DestroyWindow(mhwnd);
			mhwnd = NULL;
		}
	}
	UnregisterWindowClass();
}

void VDVideoDisplayManager::ThreadRunTimerOnly() {
	MSG msg;
	PeekMessage(&msg, NULL, 0, 0, PM_NOREMOVE);
	mStarted.signal();

	bool precise = false;
	for(;;) {
		uint32 timeSinceLastPrecise = ::GetTickCount() - mPreciseModeLastUse;

		if (precise) {
			if (timeSinceLastPrecise > 1000) {
				precise = false;
				ExitPreciseMode();
			}
		} else {
			if (timeSinceLastPrecise < 500) {
				precise = true;
				EnterPreciseMode();
			}
		}

		DWORD ret = MsgWaitForMultipleObjects(0, NULL, TRUE, 1, QS_ALLINPUT);

		if (ret == WAIT_OBJECT_0) {
			bool success = false;
			while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
				if (msg.message == WM_QUIT)
					return;
				success = true;
				TranslateMessage(&msg);
				DispatchMessage(&msg);
			}

			if (success)
				continue;

			ret = WAIT_TIMEOUT;
			::Sleep(1);
		}
		
		if (ret == WAIT_TIMEOUT) {
			if (mTicksEnabledCount > 0)
				PostTick();
			else
				WaitMessage();
		} else
			break;
	}

	if (precise)
		ExitPreciseMode();
}

void VDVideoDisplayManager::DispatchTicks() {
	if (mpProfileHook)
		mpProfileHook(IVDVideoDisplay::kProfileEvent_BeginTick);

	Clients::iterator it(mClients.begin()), itEnd(mClients.end());
	for(; it!=itEnd; ++it) {
		VDVideoDisplayClient *pClient = *it;

		if (pClient->mbTicksEnabled)
			pClient->OnTick();
	}

	if (mpProfileHook)
		mpProfileHook(IVDVideoDisplay::kProfileEvent_EndTick);
}

void VDVideoDisplayManager::PostTick() {
	if (!mOutstandingTicks.xchg(1)) {
		PostMessage(mhwnd, WM_TIMER, kTimerID_Tick, 0);
	}
}

void VDVideoDisplayManager::DispatchRemoteCalls() {
	vdsynchronized(mMutex) {
		while(!mRemoteCalls.empty()) {
			RemoteCallNode *rcn = mRemoteCalls.back();
			mRemoteCalls.pop_back();
			rcn->mpFunction(rcn->mpData);
			rcn->mSignal.signal();
		}
	}
}

void VDVideoDisplayManager::EnterPreciseMode() {
	TIMECAPS tc;
	if (!mPreciseModePeriod &&
		TIMERR_NOERROR == ::timeGetDevCaps(&tc, sizeof tc) &&
		TIMERR_NOERROR == ::timeBeginPeriod(tc.wPeriodMin))
	{
		mPreciseModePeriod = tc.wPeriodMin;
		SetThreadPriority(getThreadHandle(), THREAD_PRIORITY_HIGHEST);
	}
}

void VDVideoDisplayManager::ExitPreciseMode() {
	if (mPreciseModePeriod) {
		timeEndPeriod(mPreciseModePeriod);
		mPreciseModePeriod = 0;
	}
}

void VDVideoDisplayManager::ReaffirmPreciseMode() {
	mPreciseModeLastUse = ::GetTickCount();
}

///////////////////////////////////////////////////////////////////////////////

bool VDVideoDisplayManager::RegisterWindowClass() {
	WNDCLASS wc;
	HMODULE hInst = VDGetLocalModuleHandleW32();

	wc.style			= 0;
	wc.lpfnWndProc		= StaticWndProc;
	wc.cbClsExtra		= 0;
	wc.cbWndExtra		= sizeof(VDVideoDisplayManager *);
	wc.hInstance		= hInst;
	wc.hIcon			= 0;
	wc.hCursor			= 0;
	wc.hbrBackground	= 0;
	wc.lpszMenuName		= 0;

	wchar_t buf[64];
	swprintf(buf, vdcountof(buf), L"VDVideoDisplayManager(%p)", this);
	wc.lpszClassName	= buf;

	mWndClass = RegisterClass(&wc);

	return mWndClass != NULL;
}

void VDVideoDisplayManager::UnregisterWindowClass() {
	if (mWndClass) {
		HMODULE hInst = VDGetLocalModuleHandleW32();
		UnregisterClass((LPCTSTR)mWndClass, hInst);
		mWndClass = NULL;
	}
}

void VDVideoDisplayManager::RemapPalette() {
	PALETTEENTRY pal[216];
	struct {
		LOGPALETTE hdr;
		PALETTEENTRY palext[255];
	} physpal;

	physpal.hdr.palVersion = 0x0300;
	physpal.hdr.palNumEntries = 256;

	int i;

	for(i=0; i<216; ++i) {
		pal[i].peRed	= (BYTE)((i / 36) * 51);
		pal[i].peGreen	= (BYTE)(((i%36) / 6) * 51);
		pal[i].peBlue	= (BYTE)((i%6) * 51);
	}

	for(i=0; i<256; ++i) {
		physpal.hdr.palPalEntry[i].peRed	= 0;
		physpal.hdr.palPalEntry[i].peGreen	= 0;
		physpal.hdr.palPalEntry[i].peBlue	= (BYTE)i;
		physpal.hdr.palPalEntry[i].peFlags	= PC_EXPLICIT;
	}

	if (HDC hdc = GetDC(0)) {
		GetSystemPaletteEntries(hdc, 0, 256, physpal.hdr.palPalEntry);
		ReleaseDC(0, hdc);
	}

	if (HPALETTE hpal = CreatePalette(&physpal.hdr)) {
		for(i=0; i<216; ++i) {
			mLogicalPalette[i] = (uint8)GetNearestPaletteIndex(hpal, RGB(pal[i].peRed, pal[i].peGreen, pal[i].peBlue));
		}

		DeleteObject(hpal);
	}
}

bool VDVideoDisplayManager::IsDisplayPaletted() {
	bool bPaletted = false;

	if (HDC hdc = GetDC(0)) {
		if (GetDeviceCaps(hdc, BITSPIXEL) <= 8)		// RC_PALETTE doesn't seem to be set if you switch to 8-bit in Win98 without rebooting.
			bPaletted = true;
		ReleaseDC(0, hdc);
	}

	return bPaletted;
}

void VDVideoDisplayManager::CreateDitheringPalette() {
	if (mhPalette)
		return;

	struct {
		LOGPALETTE hdr;
		PALETTEENTRY palext[255];
	} pal;

	pal.hdr.palVersion = 0x0300;
	pal.hdr.palNumEntries = 216;

	for(int i=0; i<216; ++i) {
		pal.hdr.palPalEntry[i].peRed	= (BYTE)((i / 36) * 51);
		pal.hdr.palPalEntry[i].peGreen	= (BYTE)(((i%36) / 6) * 51);
		pal.hdr.palPalEntry[i].peBlue	= (BYTE)((i%6) * 51);
		pal.hdr.palPalEntry[i].peFlags	= 0;
	}

	mhPalette = CreatePalette(&pal.hdr);
}

void VDVideoDisplayManager::DestroyDitheringPalette() {
	if (mhPalette) {
		DeleteObject(mhPalette);
		mhPalette = NULL;
	}
}

void VDVideoDisplayManager::CheckForegroundState() {
	bool appActive = true;
	
	if (mbBackgroundFallbackEnabled)
		appActive = VDIsForegroundTaskW32();

	if (mbAppActive != appActive) {
		mbAppActive = appActive;

		// Don't handle this synchronously in case we're handling a message in the minidriver!
		PostMessage(mhwnd, WM_USER + 100, 0, 0);
	}
}

LRESULT CALLBACK VDVideoDisplayManager::StaticWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	if (msg == WM_NCCREATE) {
		const CREATESTRUCT& cs = *(const CREATESTRUCT *)lParam;

		SetWindowLongPtr(hwnd, 0, (LONG_PTR)cs.lpCreateParams);
	} else {
		VDVideoDisplayManager *pThis = (VDVideoDisplayManager *)GetWindowLongPtr(hwnd, 0);

		if (pThis)
			return pThis->WndProc(hwnd, msg, wParam, lParam);
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

LRESULT CALLBACK VDVideoDisplayManager::WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			if (mbMultithreaded)
				SetTimer(hwnd, kTimerID_ForegroundPoll, 500, NULL);
			break;

		case WM_ACTIVATEAPP:
			CheckForegroundState();
			break;

		case WM_TIMER:
			switch(wParam) {
			case kTimerID_ForegroundPoll:
				CheckForegroundState();
				break;

			case kTimerID_Tick:
				if (mOutstandingTicks.xchg(0))
					DispatchTicks();
				break;
			}
			break;

		case WM_DISPLAYCHANGE:
			{
				bool bPaletted = IsDisplayPaletted();

				if (bPaletted)
					CreateDitheringPalette();

				for(Clients::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
					VDVideoDisplayClient *p = *it;

					if (!p->mbRequiresFullScreen)
						p->OnDisplayChange();
				}

				if (!bPaletted)
					DestroyDitheringPalette();
			}
			break;

		// Yes, believe it or not, we still support palettes, even when DirectDraw is active.
		// Why?  Very occasionally, people still have to run in 8-bit mode, and a program
		// should still display something half-decent in that case.  Besides, it's kind of
		// neat to be able to dither in safe mode.
		case WM_PALETTECHANGED:
			{
				DWORD dwProcess;

				GetWindowThreadProcessId((HWND)wParam, &dwProcess);

				if (dwProcess != GetCurrentProcessId()) {
					for(Clients::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
						VDVideoDisplayClient *p = *it;

						if (!p->mbRequiresFullScreen)
							p->OnRealizePalette();
					}
				}
			}
			break;

		case WM_USER+100:
			{
				for(Clients::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
					VDVideoDisplayClient *p = *it;

					p->OnForegroundChange(mbAppActive);
				}
			}
			break;

		case WM_USER+101:
			{
				bool enabled = wParam != 0;

				if (mbBackgroundFallbackEnabled != enabled) {
					mbBackgroundFallbackEnabled = enabled;

					CheckForegroundState();
				}
			}
			break;
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

