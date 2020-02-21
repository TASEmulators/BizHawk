//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <vd2/system/cmdline.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include "uiinstance.h"

// {8437E294-45E9-4f9a-A77E-E8D3AAE385A0}
extern const uint8 kATGUID_CopyDataCmdLine[16]={
	0x94, 0xE2, 0x37, 0x84, 0xE9, 0x45, 0x9A, 0x4F, 0xA7, 0x7E, 0xE8, 0xD3, 0xAA, 0xE3, 0x85, 0xA0
};


bool ATGetProcessUser(HANDLE hProcess, vdstructex<TOKEN_USER>& tokenUser) {
	bool success = false;

	HANDLE hToken;
	if (OpenProcessToken(hProcess, TOKEN_QUERY, &hToken)) {
		DWORD actual;
		if (GetTokenInformation(hToken, TokenUser, NULL, 0, &actual) || GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
			tokenUser.resize(actual);
			if (GetTokenInformation(hToken, TokenUser, &*tokenUser, (DWORD)tokenUser.size(), &actual)) {
				success = true;
			}
		}

		CloseHandle(hToken);
	}

	return success;
}

class ATFindOtherInstanceHelper {
public:
	ATFindOtherInstanceHelper()
		: mhwndFound(NULL)
	{
	}

	HWND Run() {
		if (!ATGetProcessUser(GetCurrentProcess(), mCurrentUser))
			return NULL;

		EnumWindows(StaticCallback, (LPARAM)this);

		return mhwndFound;
	}

protected:
	static BOOL CALLBACK StaticCallback(HWND hwnd, LPARAM lParam) {
		return ((ATFindOtherInstanceHelper *)lParam)->Callback(hwnd);
	}

	BOOL Callback(HWND hwnd) {
		WCHAR className[64];
		
		if (!GetClassName(hwnd, className, vdcountof(className)))
			return TRUE;

		if (wcscmp(className, L"AltirraMainWindow"))
			return TRUE;

		DWORD pid = 0;
		GetWindowThreadProcessId(hwnd, &pid);

		if (!pid)
			return TRUE;

		HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, pid);
		if (!hProcess)
			return TRUE;

		vdstructex<TOKEN_USER> processUser;
		if (ATGetProcessUser(hProcess, processUser) && EqualSid(mCurrentUser->User.Sid, processUser->User.Sid))
			mhwndFound = hwnd;

		CloseHandle(hProcess);

		return !mhwndFound;
	}

	vdstructex<TOKEN_USER> mCurrentUser;
	HWND mhwndFound;
};

HWND ATFindOtherInstance() {
	ATFindOtherInstanceHelper helper;

	return helper.Run();
}

bool ATNotifyOtherInstance(const VDCommandLine& cmdLine) {
	HWND hwndOther = ATFindOtherInstance();

	if (!hwndOther)
		return false;

	VDStringW s;

	uint32 n = cmdLine.GetCount();
	for(uint32 i=0; i<n; ++i) {
		const VDStringSpanW arg = cmdLine(i);

		if (i)
			s += L' ';

		if (arg.find(' ') != VDStringSpanW::npos || arg.find('\\') != VDStringSpanW::npos || arg.find('"') != VDStringSpanW::npos) {
			s += L'"';

			for(VDStringSpanW::const_iterator it(arg.begin()), itEnd(arg.end()); it != itEnd; ++it) {
				const wchar_t c = *it;

				if (c == L'\\' || c == L'"')
					s += L'\\';

				s += c;
			}

			s += L'"';
		} else {
			s += arg;
		}
	}

	s += L'\0';

	// Copy the current directory and current drive paths into the block. We need this to resolve
	// relative paths properly.
	wchar_t chdir[MAX_PATH];
	chdir[0] = 0;
	DWORD actual = ::GetCurrentDirectoryW(MAX_PATH, chdir);
	if (actual && actual < MAX_PATH) {
		s += L"chdir";
		s += L'\0';
		s += chdir;
		s += L'\0';
	}

	DWORD driveMask = ::GetLogicalDrives();
	for(int i=0; i<26; ++i) {
		if (driveMask & (1 << i)) {
			wchar_t envVarName[4] = {
				L'=',
				(wchar_t)(L'A' + i),
				L':',
				0
			};

			chdir[0] = 0;
			actual = GetEnvironmentVariableW(envVarName, chdir, MAX_PATH);
			if (actual && actual < MAX_PATH) {
				s += envVarName;
				s += L'\0';
				s += chdir;
				s += L'\0';
			}
		}
	}

	vdfastvector<uint8> rawdata(16 + s.size() * sizeof(wchar_t));
	memcpy(rawdata.data(), kATGUID_CopyDataCmdLine, 16);
	memcpy(rawdata.data() + 16, s.data(), s.size()*sizeof(wchar_t));

	COPYDATASTRUCT cds;
	cds.dwData = 0xA7000001;
	cds.lpData = rawdata.data();
	cds.cbData = (DWORD)rawdata.size();

	SetForegroundWindow(hwndOther);
	SendMessage(hwndOther, WM_COPYDATA, (WPARAM)hwndOther, (LPARAM)&cds);
	return true;
}
