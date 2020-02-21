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

#ifndef f_VD2_VDJSON_JSONSTRINGOUTPUT_H
#define f_VD2_VDJSON_JSONSTRINGOUTPUT_H

#include <vd2/vdjson/jsonwriter.h>

class IVDStream;

class VDJSONStringOutput final : public IVDJSONWriterOutput {
public:
	VDJSONStringOutput(VDStringW& s) : mString(s) {}

	void WriteChars(const wchar_t *src, uint32 len) override;

private:
	VDStringW& mString;
};

class VDJSONStringOutputCRLF final : public IVDJSONWriterOutput {
public:
	VDJSONStringOutputCRLF(VDStringW& s) : mString(s) {}

	void WriteChars(const wchar_t *src, uint32 len) override;

private:
	VDStringW& mString;
};

typedef VDJSONStringOutputCRLF VDJSONStringWriterOutputSysLE;

///////////////////////////////////////////////////////////////////////////

class VDJSONStreamOutput : public IVDJSONWriterOutput {
public:
	VDJSONStreamOutput(IVDStream& stream) : mStream(stream) {}
	~VDJSONStreamOutput();

	void WriteChars(const wchar_t *src, uint32 len) override final;
	void Flush();

protected:
	virtual void WriteInternal(const uint8 *src, size_t len);

	IVDStream& mStream;
	uint32 mBufLevel = 0;
	uint8 mBuf[512];
};

class VDJSONStreamOutputCRLF final : public VDJSONStreamOutput {
public:
	VDJSONStreamOutputCRLF(IVDStream& stream) : VDJSONStreamOutput(stream) {}

protected:
	void WriteInternal(const uint8 *src, size_t len) override;
};

typedef VDJSONStreamOutputCRLF VDJSONStreamOutputSysLE;

#endif
