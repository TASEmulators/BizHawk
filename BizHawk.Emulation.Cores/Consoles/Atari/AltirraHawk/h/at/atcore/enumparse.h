//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2017 Avery Lee
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

#ifndef f_AT_ATCORE_ENUMPARSE_H
#define f_AT_ATCORE_ENUMPARSE_H

#include <vd2/system/vdtypes.h>

class VDStringSpanA;

struct ATEnumLookupTable;
template<typename T> const ATEnumLookupTable& ATGetEnumLookupTable() = delete;

template<typename T>
struct ATEnumParseResult {
	bool mValid;
	T mValue;
};

const char *ATEnumToString(const ATEnumLookupTable& table, uint32 value);
ATEnumParseResult<uint32> ATParseEnum(const ATEnumLookupTable& table, const VDStringSpanA& str);
ATEnumParseResult<uint32> ATParseEnum(const ATEnumLookupTable& table, const VDStringSpanW& str);

template<typename T>
ATEnumParseResult<T> ATParseEnum(const VDStringSpanA& str) {
	auto v = ATParseEnum(ATGetEnumLookupTable<T>(), str);

	return { v.mValid, (T)v.mValue };
}

template<typename T>
const char *ATEnumToString(T value) {
	return ATEnumToString(ATGetEnumLookupTable<T>(), (uint32)value);
}

#define AT_DECLARE_ENUM_TABLE(enumName) template<> const ATEnumLookupTable& ATGetEnumLookupTable<enumName>()


#endif
