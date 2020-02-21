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
#include <windows.h>

bool ATUIClipIsTextAvailable() {
	return !!IsClipboardFormatAvailable(CF_TEXT);
}

bool ATUIClipGetText(VDStringA& s8, VDStringW& s16, bool& use16) {
	bool success = false;

	if (OpenClipboard(NULL)) {
		bool unicodeSuccessful = false;
		bool unicodePreferred = false;

		UINT format = 0;
		while(format = EnumClipboardFormats(format)) {
			if (format == CF_UNICODETEXT) {
				unicodePreferred = true;
				break;
			} else if (format == CF_TEXT || format == CF_OEMTEXT)
				break;
		}

		if (unicodePreferred) {
			HANDLE hData = GetClipboardData(CF_UNICODETEXT);

			if (hData) {
				void *udata = GlobalLock(hData);

				if (udata) {
					size_t len = GlobalSize(hData) / sizeof(WCHAR);
					const WCHAR *s = (const WCHAR *)udata;

					s16.assign(s, s + len);

					GlobalUnlock(hData);

					auto nullPos = s16.find(L'\0');
					if (nullPos != s16.npos)
						s16.erase(nullPos);

					success = true;
					use16 = true;
					unicodeSuccessful = true;
				}
			}
		}

		if (!unicodeSuccessful) {
			HANDLE hData = GetClipboardData(CF_TEXT);

			if (hData) {
				void *data = GlobalLock(hData);

				if (data) {
					size_t len = GlobalSize(hData);
					const char *s = (const char *)data;

					s8.assign(s, s + len);

					GlobalUnlock(hData);

					auto nullPos = s8.find('\0');
					if (nullPos != s8.npos)
						s8.erase(nullPos);

					success = true;
					use16 = false;
				}
			}
		}

		CloseClipboard();
	}

	return success;
}
