//	Asuka - VirtualDub Build/Post-Mortem Utility
//	Copyright (C) 2005 Avery Lee
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

#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/vdstl.h>
#include <vector>
#include <windows.h>

void tool_resbind(const std::vector<const char *>& args, const std::vector<const char *>& switches, bool amd64) {
	if (args.size() != 4) {
		printf("usage: resbind <exe-name> <source file> <restype> <resname>\n");
		exit(5);
	}

	const char *exename = args[0];
	const char *srcfile = args[1];
	const char *restype = args[2];
	const char *resname = args[3];

	VDFile file(srcfile);

	vdblock<char> buf((size_t)file.size());

	file.read(buf.data(), buf.size());
	file.close();

	HANDLE hUpdate = BeginUpdateResource(exename, FALSE);
	if (!hUpdate)
		throw MyWin32Error("Cannot open \"%s\" for resource edit: %%s.", GetLastError(), exename);

	BOOL success = UpdateResource(hUpdate, restype, resname, 0x0409, buf.data(), buf.size());
	DWORD err = GetLastError();

	EndUpdateResource(hUpdate, !success);

	if (!success)
		throw MyWin32Error("Cannot update \"%s\": %%s.", err, exename);

	printf("Adding \"%s\" to \"%s\" as %s:%s.\n", srcfile, exename, restype, resname);
}
