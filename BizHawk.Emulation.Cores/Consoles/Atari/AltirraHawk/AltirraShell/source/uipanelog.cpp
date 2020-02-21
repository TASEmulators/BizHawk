//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - UI logging pane
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
#include <windowsx.h>
#include <richedit.h>
#include <vd2/system/w32assist.h>
#include <at/atnativeui/uiframe.h>
#include "logger.h"
#include "panes.h"
#include "resource.h"

class ATSUILogPane : public ATUIPane {
public:
	ATSUILogPane();
	~ATSUILogPane();

	void Activate() {
		if (mhwnd)
			SetForegroundWindow(mhwnd);
	}

	void Write(const char *s);
	void ShowEnd();

protected:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT LogWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam);

	bool OnCreate();
	void OnDestroy();
	void OnSize();
	void OnFontsUpdated();

	void AddToHistory(const char *s);
	void FlushAppendBuffer();

	void OnNewLogMessage();

	enum {
		kTimerId_AddText = 501,
	};

	HWND	mhwndLog = nullptr;
	HMENU	mMenu = nullptr;

	bool	mbAppendTimerStarted = false;

	VDAtomicBool	mLogMessageQueued = false;
};

ATSUILogPane::ATSUILogPane()
	: ATUIPane(kATSUIPaneId_Log, L"Log")
	, mMenu(LoadMenu(VDGetLocalModuleHandleW32(), MAKEINTRESOURCE(IDR_LOG_MENU)))
{
	mPreferredDockCode = kATContainerDockBottom;
}

ATSUILogPane::~ATSUILogPane() {
	if (mMenu)
		DestroyMenu(mMenu);
}

LRESULT ATSUILogPane::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
	case WM_SIZE:
		OnSize();
		return 0;
	case WM_COMMAND:
		switch(LOWORD(wParam)) {
			case ID_CONTEXT_CLEARALL:
				SetWindowText(mhwndLog, _T(""));
				
				{
					auto *p = ATSGetLogger();

					p->BeginAsync();
					p->UnlockTextAndClear();
					p->EndAsync();
				}
				return 0;
		}
		break;

	case WM_SETFOCUS:
		SetFocus(mhwndLog);
		return 0;

	case WM_SYSCOMMAND:
		// block F10... unfortunately, this blocks plain Alt too
		if (!lParam)
			return 0;
		break;
	case WM_CONTEXTMENU:
		{
			int x = GET_X_LPARAM(lParam);
			int y = GET_Y_LPARAM(lParam);

			TrackPopupMenu(GetSubMenu(mMenu, 0), TPM_LEFTALIGN|TPM_TOPALIGN, x, y, 0, mhwnd, NULL);
		}
		return 0;

	case WM_TIMER:
		if (wParam == kTimerId_AddText) {
			FlushAppendBuffer();

			mbAppendTimerStarted = false;
			KillTimer(mhwnd, wParam);
			return 0;
		}

		break;

	case WM_USER+200:
		{
			auto *p = ATSGetLogger();

			mLogMessageQueued = false;

			p->BeginAsync();
			if (p->GetTextLength() > 4096) {
				if (mbAppendTimerStarted) {
					mbAppendTimerStarted = false;

					KillTimer(mhwnd, kTimerId_AddText);
				}

				FlushAppendBuffer();
			} else {
				if (!mbAppendTimerStarted) {
					mbAppendTimerStarted = true;

					SetTimer(mhwnd, kTimerId_AddText, 10, NULL);
				}
			}
			p->EndAsync();
		}
		break;
	}

	return ATUIPane::WndProc(msg, wParam, lParam);
}

bool ATSUILogPane::OnCreate() {
	if (!ATUIPane::OnCreate())
		return false;

	mhwndLog = CreateWindowEx(WS_EX_CLIENTEDGE, MSFTEDIT_CLASS, _T(""), ES_READONLY|ES_MULTILINE|ES_AUTOVSCROLL|WS_VSCROLL|WS_VISIBLE|WS_CHILD, 0, 0, 0, 0, mhwnd, (HMENU)100, VDGetLocalModuleHandleW32(), NULL);
	if (!mhwndLog)
		return false;

	OnFontsUpdated();

	OnSize();

	ATSGetLogger()->SetLogHandler([this]() { OnNewLogMessage(); });

	OnNewLogMessage();

	return true;
}

void ATSUILogPane::OnDestroy() {
	ATSGetLogger()->SetLogHandler(nullptr);

	if (mhwndLog) {
		DestroyWindow(mhwndLog);
		mhwndLog = NULL;
	}

	ATUIPane::OnDestroy();
}

void ATSUILogPane::OnSize() {
	RECT r;

	if (GetClientRect(mhwnd, &r)) {
		if (mhwndLog)
			VDVERIFY(SetWindowPos(mhwndLog, NULL, 0, 0, r.right, r.bottom, SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE));
	}
}

void ATSUILogPane::OnFontsUpdated() {
	if (mhwndLog) {
		HWND hwndParent = GetParent(mhwnd);

		if (hwndParent) {
			auto *frame = ATFrameWindow::GetFrameWindow(hwndParent);

			if (frame)
				SendMessage(mhwndLog, WM_SETFONT, (WPARAM)frame->GetContainer()->GetLabelFont(), TRUE);
		}
	}

	OnSize();
}

void ATSUILogPane::ShowEnd() {
	SendMessage(mhwndLog, EM_SCROLLCARET, 0, 0);
}

void ATSUILogPane::FlushAppendBuffer() {
	auto *p = ATSGetLogger();

	p->BeginAsync();

	const wchar_t *s = p->LockText();

	if (s && *s) {
		if (SendMessage(mhwndLog, EM_GETLINECOUNT, 0, 0) > 5000) {
			POINT pt;
			SendMessage(mhwndLog, EM_GETSCROLLPOS, 0, (LPARAM)&pt);
			int idx = SendMessage(mhwndLog, EM_LINEINDEX, 2000, 0);
			SendMessage(mhwndLog, EM_SETSEL, 0, idx);
			SendMessage(mhwndLog, EM_REPLACESEL, FALSE, (LPARAM)_T(""));
			SendMessage(mhwndLog, EM_SETSCROLLPOS, 0, (LPARAM)&pt);
		}

		SendMessageW(mhwndLog, EM_SETSEL, -1, -1);
		SendMessageW(mhwndLog, EM_REPLACESEL, FALSE, (LPARAM)s);
		SendMessageW(mhwndLog, EM_SETSEL, -1, -1);
		SendMessageW(mhwndLog, EM_SCROLLCARET, 0, 0);
	}

	p->UnlockTextAndClear();

	p->EndAsync();
}

void ATSUILogPane::OnNewLogMessage() {
	if (!mLogMessageQueued.xchg(true))
		PostMessage(mhwnd, WM_USER+200, 0, 0);
}

///////////////////////////////////////////////////////////////////////////

void ATSUIRegisterLogPane() {
	ATRegisterUIPaneType(kATSUIPaneId_Log, VDRefCountObjectFactory<ATSUILogPane>);
}
