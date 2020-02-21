//	VirtualDub - Video processing and capture application
//	JSON I/O library
//	Copyright (C) 1998-2016 Avery Lee
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
#include <vd2/system/file.h>
#include <vd2/vdjson/jsonoutput.h>

void VDJSONStringOutput::WriteChars(const wchar_t *src, uint32 len) {
	mString.append(src, len);
}

void VDJSONStringOutputCRLF::WriteChars(const wchar_t *src, uint32 len) {
	while(len--) {
		wchar_t c = *src++;

		if (c == '\n')
			mString += '\r';

		mString += c;
	}
}

///////////////////////////////////////////////////////////////////////////

VDJSONStreamOutput::~VDJSONStreamOutput() {
	try {
		Flush();
	} catch(...) {
	}
}

void VDJSONStreamOutput::WriteChars(const wchar_t *src, uint32 len) {
	uint8 buf[64];

	while(len) {
		size_t srcUsed;
		size_t dstUsed = VDCodePointToU8(buf, vdcountof(buf), src, len, srcUsed);

		WriteInternal(buf, dstUsed);

		src += srcUsed;
		len -= srcUsed;
	}
}

void VDJSONStreamOutput::Flush() {
	if (mBufLevel) {
		mStream.Write(mBuf, mBufLevel);
		mBufLevel = 0;
	}
}

void VDJSONStreamOutput::WriteInternal(const uint8 *src, size_t len) {
	while(len) {
		size_t tc = sizeof mBuf - mBufLevel;
		if (tc > len)
			tc = len;

		memcpy(mBuf + mBufLevel, src, tc);
		mBufLevel += tc;

		if (mBufLevel >= sizeof mBuf)
			Flush();

		len -= tc;
		src += tc;
	}
}

///////////////////////////////////////////////////////////////////////////

void VDJSONStreamOutputCRLF::WriteInternal(const uint8 *src, size_t len) {
	while(len) {
		size_t i = 0;

		for(;;) {
			if (i >= len) {
				if (i)
					VDJSONStreamOutput::WriteInternal(src, i);

				return;
			}

			if (src[i] == (uint8)'\n')
				break;

			++i;
		}

		if (i)
			VDJSONStreamOutput::WriteInternal(src, i);

		src += i + 1;
		len -= i + 1;

		const uint8 crlf[2] = { (uint8)'\r', (uint8)'\n' };
		VDJSONStreamOutput::WriteInternal(crlf, 2);
	}
}
