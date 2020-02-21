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
#include <vd2/vdjson/jsonwriter.h>
#include <vd2/vdjson/jsonvalue.h>

VDJSONWriter::VDJSONWriter() {
}

VDJSONWriter::~VDJSONWriter() {
}

void VDJSONWriter::Begin(IVDJSONWriterOutput *output, bool compact) {
	mpOutput = output;
	mbArrayMode = false;
	mbFirstItem = true;
	mbCompactMode = compact;
}

void VDJSONWriter::End() {
	if (!mbCompactMode)
		Write(L"\n", 1);
}

void VDJSONWriter::OpenArray() {
	BeginValue();
	Write(L"[", 1);

	mStack.push_back((mbArrayMode ? 1 : 0) + (mbFirstItem ? 2 : 0));
	mbArrayMode = true;
	mbFirstItem = true;
}

void VDJSONWriter::OpenObject() {
	BeginValue();
	Write(L"{", 1);

	mStack.push_back((mbArrayMode ? 1 : 0) + (mbFirstItem ? 2 : 0));
	mbArrayMode = false;
	mbFirstItem = true;
}

void VDJSONWriter::Close() {
	uint8 code = mStack.back();
	mStack.pop_back();

	if (!mbFirstItem) {
		WriteLine();
		WriteIndent();
	}

	Write(mbArrayMode ? L"]" : L"}", 1);

	mbArrayMode = (code & 1) != 0;
	mbFirstItem = (code & 2) != 0;
}

void VDJSONWriter::WriteMemberName(const wchar_t *name) {
	WriteMemberName(name, wcslen(name));
}

void VDJSONWriter::WriteMemberName(const wchar_t *name, size_t len) {
	if (!mbFirstItem)
		Write(L",", 1);
	mbFirstItem = false;

	WriteLine();
	WriteIndent();

	WriteRawString(name, len);
	Write(L": ", 2);
}

void VDJSONWriter::WriteNull() {
	BeginValue();
	Write(L"null", 4);
}

void VDJSONWriter::WriteBool(bool value) {
	BeginValue();

	if (value)
		Write(L"true", 4);
	else
		Write(L"false", 5);
}

void VDJSONWriter::WriteInt(sint64 value) {
	BeginValue();

	wchar_t buf[64];

	buf[0] = 0;
	unsigned len = (unsigned)swprintf(buf, 64, L"%lld", value);
	if (len < 64)
		Write(buf, len);
}

void VDJSONWriter::WriteReal(double value) {
	BeginValue();

	wchar_t buf[64];

	buf[0] = 0;
	unsigned len = (unsigned)swprintf(buf, 64, L"%.17g", value);
	if (len < 64)
		Write(buf, len);
}

void VDJSONWriter::WriteString(const wchar_t *s) {
	WriteString(s, wcslen(s));
}

void VDJSONWriter::WriteString(const wchar_t *s, size_t len) {
	BeginValue();
	WriteRawString(s, len);
}

void VDJSONWriter::BeginValue() {
	if (!mbArrayMode)
		return;

	if (!mbFirstItem)
		Write(L",", 1);
	mbFirstItem = false;

	if (!mStack.empty())
		WriteLine();
	WriteIndent();
}

void VDJSONWriter::WriteRawString(const wchar_t *s, size_t len) {
	Write(L"\"", 1);

	while(len) {
		size_t tc = 0;
		wchar_t c;
		while(tc < len) {
			c = s[tc];

			if ((uint32)c < 0x20 || c == L'"' || c == L'\\')
				break;

			++tc;
		}

		Write(s, tc);
		s += tc;
		len -= tc;

		if (!len)
			break;

		if (c == L'"') {
			Write(L"\\\"", 2);
		} else if (c == L'\\') {
			Write(L"\\\\", 2);
		} else {
			wchar_t buf[6];
			const wchar_t sHexDigits[16] = { L'0', L'1', L'2', L'3', L'4', L'5', L'6', L'7', L'8', L'9', L'A', L'B', L'C', L'D', L'E', L'F' };
			uint32 ci = c;

			buf[0] = L'\\';
			buf[1] = L'u';
			buf[2] = sHexDigits[(ci >> 12) & 15];
			buf[3] = sHexDigits[(ci >>  8) & 15];
			buf[4] = sHexDigits[(ci >>  4) & 15];
			buf[5] = sHexDigits[(ci >>  0) & 15];
			Write(buf, 6);
		}

		++s;
		--len;
	}

	Write(L"\"", 1);
}

void VDJSONWriter::Write(const wchar_t *s, size_t len) {
	mpOutput->WriteChars(s, len);
}

void VDJSONWriter::WriteLine() {
	if (!mbCompactMode)
		Write(L"\n", 1);
}

void VDJSONWriter::WriteIndent() {
	if (mbCompactMode)
		return;

	size_t indent = mStack.size() & 7;

	if (indent)
		Write(L"\t\t\t\t\t\t\t", indent);
}

///////////////////////////////////////////////////////////////////////////

void VDJSONWriteValue(VDJSONWriter& writer, const VDJSONValue& value, const VDJSONNameTable& nameTable) {
	switch(value.mType) {
		case VDJSONValue::kTypeNull:
			writer.WriteNull();
			break;

		case VDJSONValue::kTypeBool:
			writer.WriteBool(value.mBoolValue);
			break;

		case VDJSONValue::kTypeInt:
			writer.WriteInt(value.mIntValue);
			break;

		case VDJSONValue::kTypeReal:
			writer.WriteReal(value.mRealValue);
			break;

		case VDJSONValue::kTypeString:
			writer.WriteString(value.mpString->mpChars, value.mpString->mLength);
			break;

		case VDJSONValue::kTypeObject:
			writer.OpenArray();
			for(const VDJSONMember *member = value.mpObject; member; member = member->mpNext) {
				const uint32 nameToken = member->mNameToken;
				const wchar_t *name = nameTable.GetName(nameToken);
				uint32 nameLen = nameTable.GetNameLength(nameToken);

				writer.WriteMemberName(name, nameLen);
				VDJSONWriteValue(writer, member->mValue, nameTable);
			}
			writer.Close();
			break;

		case VDJSONValue::kTypeArray:
			writer.OpenObject();
			{
				const VDJSONArray arr(*value.mpArray);

				for(uint32 i=0; i<arr.mLength; ++i)
					VDJSONWriteValue(writer, arr.mpElements[i], nameTable);
			}
			writer.Close();
			break;
	}
}
