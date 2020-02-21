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

#ifndef f_ASUKA_UTILS_H
#define f_ASUKA_UTILS_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/VDString.h>
#include <string>

void VDNORETURN help();
void VDNORETURN fail(const char *format, ...);

void canonicalize_name(std::string& name);
void canonicalize_name(VDStringA& name);
std::string get_name();
int get_version();
bool read_version();
void inc_version(const char *tag);
bool write_version(const char *tag);

class ProjectSetup {
public:
	ProjectSetup();
	~ProjectSetup();

	void Query();
	void Read(const wchar_t *filename);
	void Write(const wchar_t *filename);

	VDStringA	mCounterTag;
};


#endif
