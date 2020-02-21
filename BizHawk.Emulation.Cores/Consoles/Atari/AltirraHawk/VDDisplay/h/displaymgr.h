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

#ifndef f_VD2_RIZA_DISPLAYMGR_H
#define f_VD2_RIZA_DISPLAYMGR_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdstl.h>
#include <vd2/system/thread.h>
#include <vd2/VDDisplay/display.h>

class VDVideoDisplayManager;

class VDVideoDisplayClient : public vdlist_node {
public:
	VDVideoDisplayClient();
	~VDVideoDisplayClient();

	void Attach(VDVideoDisplayManager *pManager);
	void Detach(VDVideoDisplayManager *pManager);
	void SetPreciseMode(bool enabled);
	void SetTicksEnabled(bool enabled);
	void SetRequiresFullScreen(bool fs);

	const uint8 *GetLogicalPalette() const;
	struct HPALETTE__ *GetPalette() const;
	void RemapPalette();

	virtual void OnTick() {}
	virtual void OnDisplayChange() {}
	virtual void OnForegroundChange(bool foreground) {}
	virtual void OnRealizePalette() {}

protected:
	friend class VDVideoDisplayManager;

	VDVideoDisplayManager	*mpManager;

	bool	mbPreciseMode;
	bool	mbTicksEnabled;
	bool	mbRequiresFullScreen;
};

class IVDVideoDisplayManager {
};

class VDVideoDisplayManager final : public VDThread, public IVDVideoDisplayManager {
public:
	VDVideoDisplayManager();
	~VDVideoDisplayManager();

	bool	Init();
	void	Shutdown();

	void	SetProfileHook(const vdfunction<void(IVDVideoDisplay::ProfileEvent)>& profileHook);

	void	SetBackgroundFallbackEnabled(bool enabled);

	void	RemoteCall(void (*function)(void *), void *data);

	void	AddClient(VDVideoDisplayClient *pClient);
	void	RemoveClient(VDVideoDisplayClient *pClient);
	void	ModifyPreciseMode(bool enabled);
	void	ReaffirmPreciseMode();
	void	ModifyTicksEnabled(bool enabled);

	void RemapPalette();
	HPALETTE	GetPalette() const { return mhPalette; }
	const uint8 *GetLogicalPalette() const { return mLogicalPalette; }

protected:
	void	ThreadRun();
	void	ThreadRunFullRemote();
	void	ThreadRunTimerOnly();

	void	DispatchTicks();
	void	PostTick();
	void	DispatchRemoteCalls();

	bool	RegisterWindowClass();
	void	UnregisterWindowClass();

	bool	IsDisplayPaletted();
	void	CreateDitheringPalette();
	void	DestroyDitheringPalette();
	void	CheckForegroundState();

	void	EnterPreciseMode();
	void	ExitPreciseMode();

	static LRESULT CALLBACK StaticWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

protected:
	enum {
		kTimerID_ForegroundPoll	= 10,
		kTimerID_Tick			= 11
	};

	VDAtomicInt	mTicksEnabledCount;
	uintptr	mTickTimerId;

	int		mPreciseModeCount;
	uint32	mPreciseModePeriod;
	VDAtomicInt	mPreciseModeLastUse;

	HPALETTE	mhPalette;
	ATOM		mWndClass;
	HWND		mhwnd;

	bool		mbMultithreaded;
	bool		mbAppActive;
	bool		mbBackgroundFallbackEnabled;

	typedef vdlist<VDVideoDisplayClient> Clients;
	Clients		mClients;

	VDThreadID			mThreadID;
	VDAtomicInt			mOutstandingTicks;

	VDSignal			mStarted;
	VDCriticalSection	mMutex;

	struct RemoteCallNode : vdlist_node {
		void (*mpFunction)(void *data);
		void *mpData;
		VDSignal mSignal;
	};

	typedef vdlist<RemoteCallNode> RemoteCalls;
	RemoteCalls	mRemoteCalls;

	vdfunction<void(IVDVideoDisplay::ProfileEvent)> mpProfileHook;

	uint8	mLogicalPalette[256];
};

#endif
