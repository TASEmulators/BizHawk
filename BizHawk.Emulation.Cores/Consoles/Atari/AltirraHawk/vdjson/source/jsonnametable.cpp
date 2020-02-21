//	VirtualDub - Video processing and capture application
//	JSON I/O library
//	Copyright (C) 1998-2010 Avery Lee
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
#include <vd2/vdjson/jsonnametable.h>
#include <vd2/system/VDString.h>

///////////////////////////////////////////////////////////////////////////

VDJSONNameTable::VDJSONNameTable() {
}

VDJSONNameTable::~VDJSONNameTable() {
}

const wchar_t *VDJSONNameTable::GetName(uint32 token) const {
	--token;

	return token < mNameTable.size() ? mNameTable[token]->c_str() : L"";
}

uint32 VDJSONNameTable::GetNameLength(uint32 token) const {
	--token;

	return token < mNameTable.size() ? mNameTable[token]->size() : 0;
}

uint32 VDJSONNameTable::AddName(const wchar_t *s) {
	return AddName(s, wcslen(s));
}

uint32 VDJSONNameTable::AddName(const wchar_t *s, size_t len) {
	std::pair<NameMap::iterator, uint32> r(mNameMap.insert(NameMap::value_type(VDStringW(s, s+len), 0)));
	if (!r.second)
		return r.first->second;

	mNameTable.push_back(&r.first->first);
	uint32 token = mNameMap.size();
	r.first->second = token;

	return token;
}

VDJSONNameToken VDJSONNameTable::GetToken(const char *s) const {
	VDStringW tmp;
	size_t len = strlen(s);
	tmp.resize(len);
	for(size_t i=0; i<len; ++i)
		tmp[i] = s[i];

	NameMap::const_iterator it(mNameMap.find(tmp));
	uint32 token = 0;

	if (it != mNameMap.end())
		token = it->second;

	return VDJSONNameToken(token);
}

VDJSONNameToken VDJSONNameTable::GetToken(const wchar_t *s) const {
	NameMap::const_iterator it(mNameMap.find_as(s));
	uint32 token = 0;

	if (it != mNameMap.end())
		token = it->second;

	return VDJSONNameToken(token);
}
