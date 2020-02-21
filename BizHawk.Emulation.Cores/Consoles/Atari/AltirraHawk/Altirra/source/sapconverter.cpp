//	Altirra - Atari 800/800XL/5200 emulator
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
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>

#include "playsap-b.inl"
#include "playsap-c.inl"
#include "playsap-d-ntsc.inl"
#include "playsap-d-pal.inl"
#include "playsap-r.inl"

namespace {
	void ATConvertATASCIIToINTERNAL(void *dst, const void *src, size_t len) {
		static const uint8 kConvTab[4] = {
			0x40,
			0x20,
			0x60,
			0x00,
		};

		uint8 *dst8 = (uint8 *)dst;
		const uint8 *src8 = (const uint8 *)src;

		while(len--) {
			uint8 c = *src8++;

			*dst8++ = c ^ kConvTab[(c >> 5) & 3];
		}
	}

	void ATSAPCheckPlayerAddressConflicts(const void *exeData, uint32 exeLen, const void *driver) {
		const uint32 driverStart = VDReadUnalignedLEU16((const uint8 *)driver + 2);
		const uint32 driverEnd = VDReadUnalignedLEU16((const uint8 *)driver + 4);

		const uint8 *src0 = (const uint8 *)exeData;
		const uint8 *src = src0;
		const uint8 *srcEnd = src0 + exeLen;

		while(srcEnd - src >= 4) {
			// read start/end addresses for this segment
			uint16 start = VDReadUnalignedLEU16(src+0);
			if (start == 0xFFFF) {
				src += 2;
				continue;
			}

			uint16 end = VDReadUnalignedLEU16(src+2);
			src += 4;

			uint32 len = (uint32)(end - start) + 1;
			if (end < start || (uint32)(srcEnd - src) < len)
				throw MyError("Unable to load SAP player: invalid address range $%04X-%04X found.", start, end);

			// check whether an unsupported overlap occurs
			if (end >= driverStart && start <= driverEnd)
				throw MyError("Unable to load SAP player: address range $%04X-%04X conflicts with driver at $%04X-%04X.", start, end, driverStart, driverEnd);

			if ((end >= 0xC000 && start <= 0xCFFF) || (end >= 0xD800 && start <= 0xFFFF))
				throw MyError("Unable to load SAP player: address range $%04X-%04X conflicts with OS ROM.", start, end);

			if (end >= 0xD800 && start <= 0xDFFF)
				throw MyError("Unable to load SAP player: address range $%04X-%04X conflicts with hardware registers.", start, end);

			src += len;
		}
	}
}

class ATInvalidSAPFileException final : public MyError {
public:
	ATInvalidSAPFileException() : MyError("The input file is not a valid SAP file.") {}
};

class ATUnsupportedSAPFileException final : public MyError {
public:
	ATUnsupportedSAPFileException() : MyError("The input SAP file is not supported.") {}
};

void ATConvertSAPToPlayer(const void *sap, uint32 len, vdfastvector<uint8>& result) {
	if (len < 5 || memcmp("SAP\r\n", sap, 5))
		throw ATInvalidSAPFileException();
	
	const char *s = (const char *)sap;
	const char *end = s + len;
	char type = 0;
	sint32 initAddr = -1;
	sint32 playerAddr = -1;
	sint32 musicAddr = -1;
	uint8 defSong = 0;
	uint8 songCount = 1;
	bool pal = true;
	uint8 vcountsPerTick = 0;

	VDStringA author;
	VDStringA name;

	for(;;) {
		const char *linestart = s;
		const char *typeend = nullptr;

		if (end-s >= 2 && s[0] == (char)0xFF && s[1] == (char)0xFF)
			break;

		for(;;) {
			if (s == end)
				throw ATInvalidSAPFileException();

			if (*s == '\r')
				break;

			if (*s == ' ' && !typeend)
				typeend = s;

			++s;
		}

		const char *lineend = s;

		++s;
		if (s == end || *s != '\n')
			throw ATInvalidSAPFileException();

		++s;

		// stop on a blank line or two FF FF bytes
		if (lineend == linestart)
			break;

		if (!typeend)
			typeend = lineend;

		VDStringSpanA typeStr(linestart, typeend);

		VDStringSpanA argStr;
		const char *argstart = nullptr;
		const char *argend = nullptr;
		bool argquoted = false;
		
		if (*typeend == ' ') {
			argstart = typeend + 1;
			argend = lineend;

			if (argstart[0] == '"' && argend[-1] == '"' && argend - argstart >= 2)
				argquoted = true;

			argStr = VDStringSpanA(argstart, argend);
		}

		if (typeStr == "AUTHOR") {
			if (!argquoted)
				throw ATInvalidSAPFileException();

			author.assign(argStr.begin() + 1, argStr.end() - 1);
		} else if (typeStr == "NAME") {
			if (!argquoted)
				throw ATInvalidSAPFileException();

			name.assign(argStr.begin() + 1, argStr.end() - 1);
		} else if (typeStr == "TYPE") {
			if (argStr.size() != 1)
				throw ATUnsupportedSAPFileException();

			type = argStr[0];
		} else if (typeStr == "INIT") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long addr = strtoul(str2.c_str(), &term, 16);
			if (!term || *term || addr >= 0x10000)
				throw ATInvalidSAPFileException();

			initAddr = (sint32)addr;
		} else if (typeStr == "PLAYER") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long addr = strtoul(str2.c_str(), &term, 16);
			if (!term || *term || addr >= 0x10000)
				throw ATInvalidSAPFileException();

			playerAddr = (sint32)addr;
		} else if (typeStr == "MUSIC") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long addr = strtoul(str2.c_str(), &term, 16);
			if (!term || *term || addr >= 0x10000)
				throw ATInvalidSAPFileException();

			musicAddr = (sint32)addr;
		} else if (typeStr == "NTSC") {
			pal = false;
		} else if (typeStr == "FASTPLAY") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long step = strtoul(str2.c_str(), &term, 10);
			if (!term || *term || !step)
				throw ATInvalidSAPFileException();

			if (step < 2)
				throw ATUnsupportedSAPFileException();

			step >>= 1;

			vcountsPerTick = step > 255 ? 255 : (uint8)step;
		} else if (typeStr == "DEFSONG") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long defSongVal = strtoul(str2.c_str(), &term, 10);
			if (!term || *term)
				throw ATInvalidSAPFileException();

			if (defSongVal > 99)
				throw ATUnsupportedSAPFileException();

			defSong = (uint8)defSongVal;
		} else if (typeStr == "SONGS") {
			VDStringA str2(argStr);
			char *term = nullptr;

			unsigned long songVal = strtoul(str2.c_str(), &term, 10);
			if (!term || *term || !songVal)
				throw ATInvalidSAPFileException();

			if (songVal > 99)
				throw ATUnsupportedSAPFileException();

			songCount = (uint8)songVal;
		}
	}

	if (author.size() > 30)
		author.resize(30);

	if (name.size() > 30)
		name.resize(30);

	if (!vcountsPerTick)
		vcountsPerTick = pal ? 312/2 : 262/2;

	if (type == 'B') {
		if (initAddr < 0 || playerAddr < 0)
			throw ATInvalidSAPFileException();

		if (end - s < 2 || s[0] != (char)0xFF || s[1] != (char)0xFF)
			throw ATInvalidSAPFileException();

		s += 2;

		ATSAPCheckPlayerAddressConflicts(s, (uint32)(end - s), g_ATSapPlayerTypeB);

		result.assign(std::begin(g_ATSapPlayerTypeB), std::end(g_ATSapPlayerTypeB));

		ATConvertATASCIIToINTERNAL(&result[25+40], author.data(), author.size());
		ATConvertATASCIIToINTERNAL(&result[25], name.data(), name.size());

		// write song count in INTERNAL
		result[25+80+3] = songCount >= 10 ? 0x10 + (songCount / 10) : 0;
		result[25+80+4] = 0x10 + (songCount % 10);

		VDWriteUnalignedLEU16(&result[7], (uint16)initAddr);
		VDWriteUnalignedLEU16(&result[10], (uint16)playerAddr);
		result[12] = vcountsPerTick;
		result[13] = (uint8)defSong;
		result[14] = (uint8)songCount;

		result.insert(result.end(), (const uint8 *)s, (const uint8 *)end);
	} else if (type == 'C') {
		if (defSong >= songCount || musicAddr < 0 || playerAddr < 0)
			throw ATInvalidSAPFileException();

		if (end - s < 2 || s[0] != (char)0xFF || s[1] != (char)0xFF)
			throw ATInvalidSAPFileException();

		s += 2;
		
		ATSAPCheckPlayerAddressConflicts(s, (uint32)(end - s), g_ATSapPlayerTypeC);

		result.assign(std::begin(g_ATSapPlayerTypeC), std::end(g_ATSapPlayerTypeC));

		ATConvertATASCIIToINTERNAL(&result[27], name.data(), name.size());
		ATConvertATASCIIToINTERNAL(&result[27+40], author.data(), author.size());

		// write song count in INTERNAL
		result[27+80+3] = songCount >= 10 ? 0x10 + (songCount / 10) : 0;
		result[27+80+4] = 0x10 + (songCount % 10);

		VDWriteUnalignedLEU16(&result[7], (uint16)(playerAddr + 3));
		VDWriteUnalignedLEU16(&result[10], (uint16)(playerAddr + 6));
		result[12] = vcountsPerTick;
		result[13] = (uint8)defSong;
		result[14] = (uint8)songCount;
		VDWriteUnalignedLEU16(&result[15], (uint16)musicAddr);

		result.insert(result.end(), (const uint8 *)s, (const uint8 *)end);
	} else if (type == 'D') {
		if (initAddr < 0)
			throw ATInvalidSAPFileException();

		if (end - s < 2 || s[0] != (char)0xFF || s[1] != (char)0xFF)
			throw ATInvalidSAPFileException();

		s += 2;
		
		if (pal)
			result.assign(std::begin(g_ATSapPlayerTypeDPAL), std::end(g_ATSapPlayerTypeDPAL));
		else
			result.assign(std::begin(g_ATSapPlayerTypeDNTSC), std::end(g_ATSapPlayerTypeDNTSC));

		ATConvertATASCIIToINTERNAL(&result[22], name.data(), std::min<size_t>(24, name.size()));
		ATConvertATASCIIToINTERNAL(&result[22+24], pal ? "(PAL) " : "(NTSC)", 6);
		ATConvertATASCIIToINTERNAL(&result[22+40], author.data(), author.size());

		VDWriteUnalignedLEU16(&result[7], (uint16)initAddr);
		if (playerAddr >= 0)
			VDWriteUnalignedLEU16(&result[10], (uint16)playerAddr);

		result.insert(result.end(), (const uint8 *)s, (const uint8 *)end);
	} else if (type == 'R') {
		result.assign(std::begin(g_ATSapPlayerTypeR), std::end(g_ATSapPlayerTypeR));

		result[6] = vcountsPerTick;
		ATConvertATASCIIToINTERNAL(&result[17+40], author.data(), author.size());
		ATConvertATASCIIToINTERNAL(&result[17], name.data(), name.size());

		result.push_back(0x00);
		result.push_back(0x10);
		result.push_back(0x00);
		result.push_back(0x10);

		const size_t basePos = result.size();

		// parse remaining data
		uint8 prevdat[9] = {0};
		bool first = true;
		size_t lastduroff = 0;
		size_t lastcmd = 0;

		uint8 dmhistory[16] = {0};
		int dmhindex = 0;

		if (end - s >= 9) {
			memcpy(prevdat, s, 9);

			for(int i=0; i<9; ++i)
				prevdat[i] = ~prevdat[i];
		}

		while(end - s >= 9) {
			const uint8 *pokeydat = (const uint8 *)s;

			uint32 deltaMask = 0;
			if (memcmp(prevdat, pokeydat, 9)) {
				for(int i=0; i<9; ++i) {
					if (prevdat[i] != pokeydat[i]) {
						prevdat[i] = pokeydat[i];

						deltaMask |= (1 << i);
					}
				}

				// check if we can shorten the last command
				if (lastduroff && result[lastduroff] == 1) {
					result[lastduroff + 1] |= 0x80;
					result.erase(result.begin() + lastduroff);
				}

				lastduroff = 0;
			}

			if (result[lastduroff] == 0x7F) {
				lastduroff = 0;
				deltaMask = 0;
			}

			if (lastduroff == 0 || deltaMask) {
				lastcmd = lastduroff = result.size();

				result.push_back(1);

				if (deltaMask == 0) {
					// push a no-op command
					result.push_back(0);
				} else {
					// see if we can reuse a previous delta mask
					int reuseIdx = -1;
					uint8 dmask = (uint8)deltaMask;

					for(int i=0; i<16; ++i) {
						if (dmhistory[(dmhindex + i) & 15] == dmask) {
							reuseIdx = i;
							break;
						}
					}

					if (reuseIdx >= 0) {
						if (deltaMask & 0x100)
							result.push_back((uint8)(0x61 + reuseIdx*2));
						else
							result.push_back((uint8)(0x60 + reuseIdx*2));
					} else if (dmask == 0xFF) {
						result.push_back(2);
						result.insert(result.end(), pokeydat, pokeydat+9);
						deltaMask = 0;
					} else {
						dmhistory[dmhindex++ & 15] = dmask;
						result.push_back(deltaMask & 0x100 ? 5 : 4);
						result.push_back(dmask);
					}

					for(int i=8; i>=0; --i) {
						if (deltaMask & (1 << i))
							result.push_back(pokeydat[i]);
					}
				}
			} else
				++result[lastduroff];

			s += 9;
		}

		// push a terminate command
		result.push_back(0x81);

		// backpatch size
		uint32 endAddr = (uint32)(result.size() - basePos) + 0x1000 - 1;

		if (endAddr > 0xBFFF)
			throw MyError("Unable to load SAP type D file because sound data is too large (%u bytes after encoding).", endAddr - 0x1000 + 1);

		result[basePos - 2] = (uint8)(endAddr >> 0);
		result[basePos - 1] = (uint8)(endAddr >> 8);
	} else {
		if (type >= 'A' && type <= 'Z')
			throw MyError("SAP type %c is not currently supported.", type);

		throw ATUnsupportedSAPFileException();
	}

	// all done
}

void ATConvertSAPToPlayer(const wchar_t *outputPath, const wchar_t *inputPath) {
	VDFile fi;
	fi.open(inputPath);

	sint64 len = fi.size();
	if (len > 0x1000000)
		throw MyError("The input file is too large to read as a SAP file.");

	uint32 len32 = (uint32)len;
	vdblock<uint8> buf(len32);
	
	fi.read(buf.data(), len32);
	fi.close();

	vdfastvector<uint8> result;
	ATConvertSAPToPlayer(buf.data(), len32, result);

	VDFile fo;
	fo.open(outputPath, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	fo.write(result.data(), (long)result.size());
	fo.close();
}
