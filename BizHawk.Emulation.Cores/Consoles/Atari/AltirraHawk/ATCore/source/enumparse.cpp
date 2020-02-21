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

#include <stdafx.h>
#include <vd2/system/VDString.h>
#include <at/atcore/enumparseimpl.h>

const char *ATEnumToString(const ATEnumLookupTable& table, uint32 value) {
	for(size_t i=0; i<table.mTableEntries; ++i) {
		if (table.mpTable[i].mValue == value)
			return table.mpTable[i].mpName;
	}

	return "";
}

ATEnumParseResult<uint32> ATParseEnum(const ATEnumLookupTable& table, const VDStringSpanA& str) {
	uint32 hash = 2166136261U;

	for(const char c : str) {
		hash = hash * 16777619U;
		hash ^= (unsigned char)c & 0xDFU;
	}

	for(size_t i=0; i<table.mTableEntries; ++i) {
		const auto& ent = table.mpTable[i];

		if (ent.mHash == hash && !str.comparei(ent.mpName))
			return { true, ent.mValue };
	}

	return { false, table.mDefaultValue };
}

ATEnumParseResult<uint32> ATParseEnum(const ATEnumLookupTable& table, const VDStringSpanW& str) {
	uint32 hash = 2166136261U;

	for(const wchar_t c : str) {
		if (c != (unsigned char)c)
			return { false, table.mDefaultValue };

		hash = hash * 16777619U;
		hash ^= (unsigned char)c & 0xDFU;
	}

	const auto strl = str.size();
	for(size_t i=0; i<table.mTableEntries; ++i) {
		const auto& ent = table.mpTable[i];

		if (ent.mHash == hash) {
			bool match = true;

			for(size_t i = 0; i < strl; ++i) {
				// str[i] is OK to truncate as we've confirmed above that it's byte-safe
				unsigned char c = (unsigned char)str[i];
				unsigned char x = c ^ (unsigned char)ent.mpName[i];

				if ((unsigned char)((c & 0xDF) - 0x41) < 26)
					x &= 0xDF;

				if (x) {
					match = false;
					break;
				}
			}

			if (match && !ent.mpName[strl])
				return { true, ent.mValue };
		}
	}

	return { false, table.mDefaultValue };
}
