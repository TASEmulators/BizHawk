//	Asuka - VirtualDub Build/Post-Mortem Utility
//	Copyright (C) 2005-2007 Avery Lee
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
#include <vd2/system/vdtypes.h>
#include <vd2/system/file.h>

#include <windows.h>

#include <stdio.h>
#include <map>
#include <string>

#include "utils.h"
#include "resource.h"

using namespace std;

typedef map<string, uint32> tVersionMap;
tVersionMap		g_versionMap;
int				g_version;
string		g_machineName;




void help() {
	puts("VirtualDub Build/Post-Mortem Utility Version 1.9.3 for "
#if VD_CPU_AMD64
			"AMD64"
#else
			"80x86"
#endif
			);
	puts("Copyright (C) Avery Lee 2005-2007. Licensed under GNU General Public License");
	puts("");
	puts("Usage: Asuka <command> [args...]");
	puts("");
	puts("Asuka fontencode   Extract TrueType font glyph outlines");
	puts("Asuka fxc          Compile shaders for Direct3D");
	puts("Asuka glc          Compile shaders for OpenGL");
	puts("Asuka lookup       Look up address in link map");
	puts("Asuka makearray    Convert binary file to C array");
	puts("Asuka maketable    Regenerate precomputed tables");
	puts("Asuka mapconv      Generate runtime symbol database");
	puts("Asuka snapsetup    Temporarily change windows settings for screencaps");
	puts("Asuka verinc       Increment version file");
	puts("Asuka psa          Assemble Direct3D pixel shader");
	puts("Asuka checkimports Check DLL/EXE imports");
	puts("Asuka hash         Compute string hashes");
	exit(5);
}

void fail(const char *format, ...) {
	va_list val;
	va_start(val, format);
	fputs("Asuka: Failed: ", stdout);
	vprintf(format, val);
	fputc('\n', stdout);
	va_end(val);
	exit(10);
}



void canonicalize_name(string& name) {
	string::iterator it(name.begin());

	*it = toupper(*it);
	++it;
	transform(it, name.end(), it, name.find('-') != string::npos ? toupper : tolower);
}

void canonicalize_name(VDStringA& name) {
	VDStringA::iterator it(name.begin());

	*it = toupper(*it);
	++it;
	transform(it, name.end(), it, name.find('-') != VDStringA::npos ? toupper : tolower);
}

string get_name() {
	char buf[256];
	DWORD siz = sizeof buf;

	if (!GetComputerNameA(buf, &siz))		// hostname would probably work on a Unix platform
		buf[0] = 0;

	string name(buf);

	if (name.empty())
		name = "Anonymous";
	else
		canonicalize_name(name);

	return name;
}

int get_version() {
	return g_version;
}

bool read_version() {
	g_machineName = get_name();
	g_versionMap.clear();
	g_version = 0;

	FILE *f = fopen("version2.bin","r");

	if (!f) {
		printf("    warning: can't open version2.bin for read, starting new version series\n");
		return false;
	}

	char linebuf[2048];

	while(fgets(linebuf, sizeof linebuf, f)) {
		int local_builds, local_name_start, local_name_end;
		if (1==sscanf(linebuf, "host: \"%n%*[^\"]%n\" builds: %d", &local_name_start, &local_name_end, &local_builds)) {
			string name(linebuf+local_name_start, local_name_end - local_name_start);

			canonicalize_name(name);

			g_versionMap[name] = local_builds;

			g_version += local_builds;
		} else if (linebuf[0] != '\n')
			printf("    warning: line ignored: %s", linebuf);
	}

	return true;
}

void inc_version(const char *tag = NULL) {
	++g_version;
	++g_versionMap[tag ? string(tag) : g_machineName];
}

INT_PTR CALLBACK VerincErrorDlgProc(HWND hdlg, UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
	case WM_COMMAND:
		switch(LOWORD(wParam)) {
		case IDCANCEL:
		case IDC_CHECKOUT:
		case IDC_STRIP_READONLY:
			EndDialog(hdlg, LOWORD(wParam));
			return TRUE;
		}
	}

	return FALSE;
}

bool write_version(const char *tag) {
	if (!tag)
		tag = g_machineName.c_str();

	printf("    incrementing to build %d (builds on '%s': %d)\n", g_version, tag, g_versionMap[tag]);

	for(;;) {
		if (FILE *f = fopen("version2.bin","w")) {
			tVersionMap::const_iterator it(g_versionMap.begin()), itEnd(g_versionMap.end());

			for(; it!=itEnd; ++it) {
				const tVersionMap::value_type val(*it);
				int pad = 20-val.first.length();

				if (pad < 1)
					pad = 1;

				fprintf(f, "host: \"%s\"%*cbuilds: %d\n", val.first.c_str(), pad, ' ', val.second);
			}

			fclose(f);
			return true;
		} else {
			DWORD attr = GetFileAttributesW(L"version2.bin");
			if (attr != 0xFFFFFFFF && (attr & FILE_ATTRIBUTE_READONLY)) {
				LRESULT rv = DialogBox(GetModuleHandle(NULL), MAKEINTRESOURCE(IDD_VERINC_ERROR), NULL, VerincErrorDlgProc);

				if (rv == IDC_STRIP_READONLY) {
					SetFileAttributesW(L"version2.bin", attr & ~FILE_ATTRIBUTE_READONLY);
					continue;
				} else if (rv == IDC_CHECKOUT) {
					system("p4 edit version2.bin");
					continue;
				}
			}

			fail("Can't open version2.bin for write.");
			return false;
		}
	}
}

ProjectSetup::ProjectSetup()
	: mCounterTag(get_name().c_str())
{
}

ProjectSetup::~ProjectSetup() {
}

INT_PTR CALLBACK ProjectSetupDlgProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	ProjectSetup *ps;

	switch(msg) {
		case WM_INITDIALOG:
			SetWindowLongPtr(hwnd, DWLP_USER, lParam);
			ps = (ProjectSetup *)lParam;
			SetDlgItemTextA(hwnd, IDC_COUNTER_TAG, ps->mCounterTag.c_str());
			return TRUE;

		case WM_COMMAND:
			switch(LOWORD(wParam)) {
				case IDOK:
				case IDCANCEL:
					ps = (ProjectSetup *)GetWindowLongPtr(hwnd, DWLP_USER);
					if (ps) {
						char buf[256];
						GetDlgItemTextA(hwnd, IDC_COUNTER_TAG, buf, 256);
						ps->mCounterTag = buf;
					}
					EndDialog(hwnd, 0);
					return TRUE;
			}
			break;
	}

	return FALSE;
}

void ProjectSetup::Query() {
	DialogBoxParam(GetModuleHandle(NULL), MAKEINTRESOURCE(IDD_SETUP), NULL, ProjectSetupDlgProc, (LPARAM)this);
}

void ProjectSetup::Read(const wchar_t *filename) {
	VDFileStream fs(filename);
	VDTextStream ts(&fs);

	while(const char *s = ts.GetNextLine()) {
		if (!strncmp(s, "counter-tag:", 12)) {
			mCounterTag = VDStringSpanA(s + 12).trim(" \t\r\n");
			canonicalize_name(mCounterTag);
		}
	}
}

void ProjectSetup::Write(const wchar_t *filename) {
	VDFileStream fs(filename, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	VDStringA s;

	s.sprintf("counter-tag: %s\r\n", mCounterTag.c_str());
	fs.write(s.data(), s.size());
}
