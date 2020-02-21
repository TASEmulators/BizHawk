//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - where it all starts
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
#include <ole2.h>
#include <vd2/system/cmdline.h>
#include <vd2/system/cpuaccel.h>
#include <vd2/system/error.h>
#include <vd2/system/function.h>
#include <vd2/system/thunk.h>
#include <vd2/system/registry.h>
#include <vd2/system/w32assist.h>
#include <at/atappbase/exceptionfilter.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/propertyset.h>
#include <at/atui/uicommandmanager.h>
#include <at/atnativeui/messageloop.h>
#include <at/atnativeui/uiframe.h>
#include "menu.h"
#include "globals.h"
#include "panes.h"
#include "logger.h"
#include "settings.h"

extern const unsigned char g_ATResMenuDef[];
extern const size_t g_ATResMenuDefLen;

HWND g_hwnd;
ATContainerWindow *g_pMainWindow;
ATUICommandManager g_ATUICmdMgr;

///////////////////////////////////////////////////////////////////////////

void ATSUIRegisterLogPane();
void ATSUIRegisterPaneDevices();

void ATSUIRegisterDialogCommands(ATUICommandManager& cm);
void ATSUIRegisterWindowCommands(ATUICommandManager& cm);
void ATSUIRegisterViewCommands(ATUICommandManager& cm);

///////////////////////////////////////////////////////////////////////////

bool ATUIDoTrapped(const vdfunction<void()>& fn) {
	try {
		fn();
	} catch(const MyError& e) {
		e.post(g_hwnd, "AltirraShell Error");
		return false;
	}

	return true;
}

const ATUICommand kProgramCommands[]={
	{ "File.Exit", []() { SendMessage(g_hwnd, WM_CLOSE, 0, 0); } }
};

///////////////////////////////////////////////////////////////////////////

class ATShellMainWindow : public ATContainerWindow {
public:
	ATShellMainWindow();
	~ATShellMainWindow();

protected:
	LRESULT WndProc(UINT msg, WPARAM wParam, LPARAM lParam);
	LRESULT WndProc2(UINT msg, WPARAM wParam, LPARAM lParam);
};

ATShellMainWindow::ATShellMainWindow() {
}

ATShellMainWindow::~ATShellMainWindow() {
}

LRESULT ATShellMainWindow::WndProc(UINT msg, WPARAM wParam, LPARAM lParam) {
	LRESULT r;
	__try {
		r = WndProc2(msg, wParam, lParam);
	} __except(ATExceptionFilter(GetExceptionCode(), GetExceptionInformation())) {
	}

	return r;
}

LRESULT ATShellMainWindow::WndProc2(UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_CREATE:
			if (ATContainerWindow::WndProc(msg, wParam, lParam) < 0)
				return -1;
			
			return 0;

		case WM_DESTROY:
			PostQuitMessage(0);
			break;

		case WM_SYSCOMMAND:
			// Need to drop capture for Alt+F4 to work.
			ReleaseCapture();
			break;

		case ATWM_QUERYSYSCHAR:
			return true;

		case WM_COMMAND:
			ATUIDoTrapped([=]() { ATUIHandleMenuCommand(g_ATUICmdMgr, wParam); });
			return 0;

		case WM_INITMENU:
			ATUIUpdateMenu(g_ATUICmdMgr);
			break;
	}

	return ATContainerWindow::WndProc(msg, wParam, lParam);
}

///////////////////////////////////////////////////////////////////////////

int ATInitShell(int nCmdShow) {
	#ifdef _MSC_VER
		_CrtSetDbgFlag(_CrtSetDbgFlag(_CRTDBG_REPORT_FLAG) | _CRTDBG_LEAK_CHECK_DF);
	#endif
	
	::SetUnhandledExceptionFilter(ATUnhandledExceptionFilter);

	CPUEnableExtensions(CPUCheckForExtensions());

	VDFastMemcpyAutodetect();
	VDInitThunkAllocator();

	OleInitialize(NULL);

	InitCommonControls();
	VDLoadSystemLibraryW32("msftedit");
	
	VDRegistryAppKey::setDefaultKey("Software\\virtualdub.org\\AltirraShell\\");

	//-----------------------------

	ATSInitDeviceManager();

	//-----------------------------

	ATUINativeWindow::Register();

	ATSInitLogger();
	ATInitUIFrameSystem();

	WNDCLASS wc = {};
	wc.lpszClassName = _T("AltirraShellMainWindow");
	ATOM atom = ATContainerWindow::RegisterCustom(wc);
	g_pMainWindow = new ATShellMainWindow;
	g_pMainWindow->AddRef();
	HWND hwnd = (HWND)g_pMainWindow->Create(atom, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, NULL, false);
	g_hwnd = hwnd;

	g_ATUICmdMgr.RegisterCommands(kProgramCommands, vdcountof(kProgramCommands));
	ATSUIRegisterDialogCommands(g_ATUICmdMgr);
	ATSUIRegisterWindowCommands(g_ATUICmdMgr);
	ATSUIRegisterViewCommands(g_ATUICmdMgr);

	ATSUIRegisterLogPane();
	ATSUIRegisterPaneDevices();

	ATUIDoTrapped([]() { ATUILoadMenu(g_ATUICmdMgr, g_ATResMenuDef, g_ATResMenuDefLen); });

	SetWindowText(hwnd, _T("AltirraShell"));

	ShowWindow(hwnd, nCmdShow);

	ATSInitSerialEngine();

	ATActivateUIPane(kATSUIPaneId_Log, true);
	ATActivateUIPane(kATSUIPaneId_Devices, true);

	ATSLoadSettings();

	return 0;
}

int ATRunShell() {
	int rc;

	__try {
		while(ATUIProcessMessages(true, rc))
			;
	} __except(ATExceptionFilter(GetExceptionCode(), GetExceptionInformation())) {
	}

	return rc;
}

void ATShutdownShell() {
	ATSSaveSettings();

	ATSShutdownSerialEngine();

	if (g_pMainWindow) {
		g_pMainWindow->Destroy();
		g_pMainWindow->Release();
		g_pMainWindow = NULL;
		g_hwnd = NULL;
	}

	ATShutdownUIFrameSystem();
	ATSShutdownLogger();

	//-----------------------------

	ATSShutdownDeviceManager();

	//-----------------------------

	OleUninitialize();

	VDShutdownThunkAllocator();
}

int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR lpCmdLine, int nCmdShow) {
	int rc = ATInitShell(nCmdShow);

	if (!rc)
		rc = ATRunShell();

	ATShutdownShell();
	return rc;
}
