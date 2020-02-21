//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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
#include <windows.h>
#include <commctrl.h>
#include <vd2/system/error.h>
#include <at/atcore/progress.h>
#include <at/atnativeui/dialog.h>
#include <at/atnativeui/progress.h>
#include "resource.h"
#include "uiprogress.h"

///////////////////////////////////////////////////////////////////////////

class ATUIProgressDialogW32 : public VDDialogFrameW32 {
public:
	ATUIProgressDialogW32();

	void Init(const wchar_t *desc, const wchar_t *statusFormat, uint32 total, VDGUIHandle parent);
	void Update(uint32 value);
	void Shutdown();

	bool OnLoaded();
	bool OnClose();

protected:
	HWND mhwndParent;
	HWND mhwndProgress;
	HWND mhwndStatus;
	bool mbParentWasEnabled;
	bool mbAborted;
	int mValueShift;
	uint32 mValue;
	uint32 mTotal;
	VDStringW mDesc;
	VDStringW mStatusFormat;
	VDStringW mStatusBuffer;
	DWORD mLastUpdateTime;
};

ATUIProgressDialogW32::ATUIProgressDialogW32()
	: VDDialogFrameW32(IDD_PROGRESS)
	, mhwndParent(NULL)
	, mhwndProgress(NULL)
	, mhwndStatus(NULL)
	, mbAborted(false)
	, mLastUpdateTime(0)
{
}

void ATUIProgressDialogW32::Init(const wchar_t *desc, const wchar_t *statusFormat, uint32 total, VDGUIHandle parent) {
	mDesc = desc;

	if (statusFormat)
		mStatusFormat = statusFormat;

	mValueShift = 0;
	for(uint32 t = total; t > 0xFFFF; t >>= 1)
		++mValueShift;

	mTotal = total;
	mValue = 0;
	mhwndParent = (HWND)parent;

	Create(parent);
}

void ATUIProgressDialogW32::Update(uint32 value) {
	if (mbAborted)
		throw MyUserAbortError();

	DWORD t = GetTickCount();

	if (t - mLastUpdateTime < 100)
		return;

	mLastUpdateTime = t;

	if (value > mTotal)
		value = mTotal;

	if (mValue != value) {
		mValue = value;

		if (mhwndProgress) {
			const uint32 pos = mValue >> mValueShift;

			// Workaround for progress bar lagging behind in Vista/Win7.
			if (pos < 0xFFFFFFFFUL)
				SendMessage(mhwndProgress, PBM_SETPOS, (WPARAM)(pos + 1), 0);

			SendMessage(mhwndProgress, PBM_SETPOS, (WPARAM)pos, 0);
		}

		if (mhwndStatus && !mStatusFormat.empty()) {
			mStatusBuffer.sprintf(mStatusFormat.c_str(), mValue, mTotal);

			SetWindowTextW(mhwndStatus, mStatusBuffer.c_str());
		}
	}

	MSG msg;
	while(!mbAborted && PeekMessage(&msg, NULL, 0, 0, PM_REMOVE | PM_NOYIELD)) {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
}

void ATUIProgressDialogW32::Shutdown() {
	Close();
}

bool ATUIProgressDialogW32::OnLoaded() {
	mbParentWasEnabled = mhwndParent && !(GetWindowLong(mhwndParent, GWL_STYLE) & WS_DISABLED);

	if (mhwndParent)
		EnableWindow(mhwndParent, FALSE);

	SetControlText(IDC_STATIC_DESC, mDesc.c_str());

	mhwndProgress = GetControl(IDC_PROGRESS);
	if (mhwndProgress)
		SendMessage(mhwndProgress, PBM_SETRANGE32, (WPARAM)0, (LPARAM)(mTotal >> mValueShift));

	mhwndStatus = GetControl(IDC_STATIC_STATUS);

	return VDDialogFrameW32::OnLoaded();
}

bool ATUIProgressDialogW32::OnClose() {
	if (mhwndParent) {
		if (mbParentWasEnabled) {
			EnableWindow(mhwndParent, TRUE);
			SetWindowLong(mhdlg, GWL_STYLE, GetWindowLong(mhdlg, GWL_STYLE) | WS_POPUP);
		}

		mhwndParent = NULL;
	}

	mbAborted = true;
	Destroy();
	return true;
}

/////////////////////////////////////////////////////////////////////////////

class ATUIProgressHandler final : public IATProgressHandler {
public:
	ATUIProgressHandler();

	void Begin(uint32 total, const wchar_t *status, const wchar_t *desc) override;
	void BeginF(uint32 total, const wchar_t *status, const wchar_t *descFormat, va_list descArgs) override;
	void Update(uint32 value) override;
	void End() override;

private:
	ATUIProgressDialogW32 *mpDialog = nullptr;
	uint32 mNestingCount = 0;
};

ATUIProgressHandler::ATUIProgressHandler() {
}

void ATUIProgressHandler::Begin(uint32 total, const wchar_t *status, const wchar_t *desc) {
	if (!mNestingCount++) {
		mpDialog = new_nothrow ATUIProgressDialogW32;
		if (mpDialog) {
			mpDialog->Init(desc, status, total, ATUIGetProgressParent());
		}
	}
}

void ATUIProgressHandler::BeginF(uint32 total, const wchar_t *status, const wchar_t *descFormat, va_list descArgs) {
	VDStringW desc;
	desc.append_vsprintf(descFormat, descArgs);

	Begin(total, status, desc.c_str());
}

void ATUIProgressHandler::Update(uint32 value) {
	if (mpDialog && mNestingCount == 1)
		mpDialog->Update(value);
}

void ATUIProgressHandler::End() {
	VDASSERT(mNestingCount > 0);

	if (!--mNestingCount) {
		if (mpDialog) {
			auto p = mpDialog;
			mpDialog = nullptr;

			p->Shutdown();
			delete p;
		}
	}
}

/////////////////////////////////////////////////////////////////////////////

ATUIProgressHandler& ATUIGetProgressHandler() {
	static ATUIProgressHandler sHandler;

	return sHandler;
}

void ATUIInitProgressDialog() {
	ATSetProgressHandler(&ATUIGetProgressHandler());
}

void ATUIShutdownProgressDialog() {
	ATSetProgressHandler(nullptr);
}
