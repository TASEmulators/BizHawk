#include <stdafx.h>
#include <vd2/system/VDString.h>
#include "hostdeviceutils.h"

namespace {
	const wchar_t *const kReservedDeviceNamesW[]={
		L"CON",
		L"PRN",
		L"AUX",
		L"NUL",
		L"COM1",
		L"COM2",
		L"COM3",
		L"COM4",
		L"COM5",
		L"COM6",
		L"COM7",
		L"COM8",
		L"COM9",
		L"LPT1",
		L"LPT2",
		L"LPT3",
		L"LPT4",
		L"LPT5",
		L"LPT6",
		L"LPT7",
		L"LPT8",
		L"LPT9",
		NULL
	};
}

bool ATHostDeviceIsDevice(const wchar_t *s) {
	const wchar_t *ext = wcschr(s, L'.');

	VDStringSpanW fname(s, ext ? ext : s + wcslen(s));
	for(const wchar_t *const *pp = kReservedDeviceNamesW; *pp; ++pp) {
		if (!fname.comparei(*pp))
			return true;
	}

	return false;
}

void ATHostDeviceEncodeName(char xlName[13], const wchar_t *hostName, bool useLongNameEncoding) {
	const VDStringA& name8 = VDTextWToU8(hostName, -1);
	const char *name = name8.c_str();
	char xlExt[4];

	if (name[0] == L'$' || name[0] == L'!')
		++name;

	const char *ext = strrchr(name, '.');

	if (!ext)
		ext = name + strlen(name);

	int xlNameLen = 0;
	int xlExtLen = 0;
	uint32 hash32 = 2166136261;
	bool encode = false;

	for(const char *s = name; s != ext; ++s) {
		char c = *s;

		if ((uint8)(c - 'a') < 26)
			c &= 0xdf;

		if (ATHostDeviceIsValidPathCharWide(c)) {
			if (xlNameLen >= 5)
				hash32 = (hash32 ^ (uint8)c) * 16777619;

			if (xlNameLen < 8)
				xlName[xlNameLen++] = (char)c;
			else
				encode = true;
		} else {
			hash32 = (hash32 ^ (uint8)c) * 16777619;
			encode = true;
		}
	}

	if (*ext == '.')
		++ext;

	for(const char *s = ext, *end = ext + strlen(ext); s != end; ++s) {
		char c = *s;

		if ((uint8)(c - 'a') < 26)
			c &= 0xdf;

		if (ATHostDeviceIsValidPathCharWide(c) && xlExtLen < 3)
			xlExt[xlExtLen++] = (char)c;
		else {
			hash32 = (hash32 ^ (uint8)c) * 16777619;
			encode = true;
		}
	}

	if (encode && useLongNameEncoding) {
		if (xlNameLen > 5)
			xlNameLen = 5;

		uint32 hash10 = hash32 ^ (hash32 >> 10) ^ (hash32 >> 20) ^ (hash32 >> 30);

		int x = (hash10 >> 5) & 31;
		int y = hash10 & 31;

		xlName[xlNameLen++] = '_';
		xlName[xlNameLen++] = x >= 10 ? 'A' + (x - 10) : '0' + x;
		xlName[xlNameLen++] = y >= 10 ? 'A' + (y - 10) : '0' + y;
	}

	xlName[xlNameLen++] = '.';
	memcpy(xlName + xlNameLen, xlExt, xlExtLen);
	xlName[xlNameLen + xlExtLen] = 0;
}
