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

#pragma warning(disable: 4786)		// SHUT UP

#include <stdafx.h>
#include <windows.h>
#include <objbase.h>

#include <stdio.h>
#include <stdlib.h>
#include <stddef.h>

#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/error.h>

#include <vector>
#include <algorithm>

#include "utils.h"

#pragma comment(lib, "gdi32")
#pragma comment(lib, "ole32")
#pragma comment(lib, "advapi32")

void tool_fxc10(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_makearray(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_glc(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_fontextract(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_fontencode(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_fontrender(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_filecreate(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_maketables(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_checkimports(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);
void tool_hash(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches);

int VDCDECL main(int argc, char **argv) {
	--argc;
	++argv;

	vdfastvector<const char *> switches, args;
	bool amd64 = false;

	while(const char *s = *argv++) {
		if (s[0] == '/') {
			if (!_stricmp(s+1, "amd64"))
				amd64 = true;
			else
				switches.push_back(s+1);
		} else {
			args.push_back(s);
		}
	}

	// look for mode
	if (args.empty())
		help();

	const char *s = args[0];

	args.erase(args.begin());

	CoInitialize(NULL);

	try {
		if (!_stricmp(s, "fxc10")) {
			tool_fxc10(args, switches);
		} else if (!_stricmp(s, "makearray")) {
			tool_makearray(args, switches);
		} else if (!_stricmp(s, "glc")) {
			tool_glc(args, switches);
		} else if (!_stricmp(s, "fontextract")) {
			tool_fontextract(args, switches);
		} else if (!_stricmp(s, "fontencode")) {
			tool_fontencode(args, switches);
		} else if (!_stricmp(s, "fontrender")) {
			tool_fontrender(args, switches);
		} else if (!_stricmp(s, "filecreate")) {
			tool_filecreate(args, switches);
		} else if (!_stricmp(s, "maketables")) {
			tool_maketables(args, switches);
		} else if (!_stricmp(s, "checkimports")) {
			tool_checkimports(args, switches);
		} else if (!_stricmp(s, "hash")) {
			tool_hash(args, switches);
		} else
			help();
	} catch(const char *s) {
		fail("%s", s);
	} catch(const MyError& e) {
		fail("%s", e.gets());
	}

	CoUninitialize();
	return 0;
}
