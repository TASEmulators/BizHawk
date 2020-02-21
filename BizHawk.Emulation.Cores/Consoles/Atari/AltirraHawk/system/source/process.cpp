#include <stdafx.h>
#include <vd2/system/process.h>
#include <vd2/system/VDString.h>
#include <vd2/system/w32assist.h>

void VDLaunchProgram(const wchar_t *path, const wchar_t *args) {
	VDStringW cmdLine(L"\"");

	cmdLine += path;

	if (cmdLine.back() == L'\\')
		cmdLine.push_back(L'\\');

	cmdLine += L"\"";

	if (args) {
		cmdLine += L' ';
		cmdLine += args;
	}

	PROCESS_INFORMATION processInfo;
	const DWORD createFlags = CREATE_NEW_PROCESS_GROUP | CREATE_DEFAULT_ERROR_MODE;
	BOOL success;

	STARTUPINFOW startupInfoW = { sizeof(STARTUPINFOW) };
	startupInfoW.dwFlags = STARTF_USESHOWWINDOW;
	startupInfoW.wShowWindow = SW_SHOWNORMAL;

	WCHAR winDir[MAX_PATH];
	success = GetWindowsDirectoryW(winDir, MAX_PATH);

	if (success)
		success = CreateProcessW(path, (LPWSTR)cmdLine.c_str(), NULL, NULL, FALSE, createFlags, NULL, winDir, &startupInfoW, &processInfo);

	if (!success)
		throw MyWin32Error("Unable to launch process: %%s", GetLastError());
}
