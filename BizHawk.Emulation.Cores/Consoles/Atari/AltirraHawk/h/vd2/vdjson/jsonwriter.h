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

#ifndef f_VD2_VDJSON_JSONWRITER_H
#define f_VD2_VDJSON_JSONWRITER_H

#include <vd2/system/vdstl.h>

struct VDJSONValue;
class VDJSONNameTable;

class IVDJSONWriterOutput {
public:
	virtual void WriteChars(const wchar_t *src, uint32 len) = 0;
};

class VDJSONWriter {
public:
	VDJSONWriter();
	~VDJSONWriter();

	void Begin(IVDJSONWriterOutput *output, bool compact = false);
	void End();

	void OpenArray();
	void OpenObject();
	void Close();
	void WriteMemberName(const wchar_t *name);
	void WriteMemberName(const wchar_t *name, size_t len);
	void WriteNull();
	void WriteBool(bool value);
	void WriteInt(sint64 value);
	void WriteReal(double value);
	void WriteString(const wchar_t *s);
	void WriteString(const wchar_t *s, size_t len);

protected:
	void BeginValue();
	void WriteRawString(const wchar_t *s, size_t len);
	void Write(const wchar_t *s, size_t len);
	void WriteLine();
	void WriteIndent();

	bool mbFirstItem;
	bool mbArrayMode;
	bool mbCompactMode;

	IVDJSONWriterOutput *mpOutput;
	vdfastvector<uint8> mStack;
};

void VDJSONWriteValue(VDJSONWriter& writer, const VDJSONValue& value, const VDJSONNameTable& nameTable);

#endif
