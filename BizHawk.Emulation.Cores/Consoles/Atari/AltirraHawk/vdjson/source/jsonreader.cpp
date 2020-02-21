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
#include <vd2/vdjson/jsonreader.h>
#include <vd2/vdjson/jsonnametable.h>
#include <vd2/vdjson/jsonvalue.h>

#define DEBUG_TRACE (void)sizeof printf

///////////////////////////////////////////////////////////////////////////

VDJSONReader::VDJSONReader()
	: mNameBuffer(NULL)
	, mNameBufferIndex(0)
	, mNameBufferLength(0)
{
}

VDJSONReader::~VDJSONReader() {
	if (mNameBuffer)
		delete[] mNameBuffer;
}

bool VDJSONReader::Parse(const void *buf, size_t len, VDJSONDocument& doc) {
	mpNameTable = &doc.mNameTable;
	mpDocument = &doc;

	mpSrc = (const uint8 *)buf;
	mpSrcEnd = mpSrc + len;
	mbSrcError = false;
	mpInputBase = NULL;
	mpInputNext = NULL;
	mpInputEnd = NULL;
	mInputLine = 0;
	mInputLineNext = 0;
	mInputChar = 0;
	mInputCharNext = 0;
	mErrorLine = 0;
	mErrorChar = 0;
	mbPendingCR = false;

	mbUTF16Mode = false;
	mbUTF32Mode = false;

	// Check the sequence of null bytes at the beginning of the file to determine the encoding
	// (see RFC4627 section 3). Note that a byte order mark (BOM) is NOT allowed in JSON, and
	// that a valid UTF-8 JSON file can be two bytes (empty array or object). All valid UTF-16
	// files must be four bytes and all valid UTF-32 files must be at least eight bytes.
	if (len >= 4) {
		if (!(mpSrc[0] | mpSrc[1] | mpSrc[2])) {
			mbUTF32Mode = true;
			mbBigEndian = true;
		} else if (!(mpSrc[3] | mpSrc[1] | mpSrc[2])) {
			mbUTF32Mode = true;
			mbBigEndian = false;
		} else if (!(mpSrc[0] | mpSrc[2])) {
			mbUTF16Mode = true;
			mbBigEndian = true;
		} else if (!(mpSrc[1] | mpSrc[3])) {
			mbUTF16Mode = true;
			mbBigEndian = false;
		}
	}

	// Check for UTF-8 BOM. These are non-standard, but sometimes appear.
	if (len >= 3 && mpSrc[0] == 0xEF && mpSrc[1] == 0xBB && mpSrc[2] == 0xBF) {
		mpSrc += 3;
	} else if (len >= 2) {
		// check for UTF-16 BOM
		if (mpSrc[0] == 0xFE && mpSrc[1] == 0xFF) {
			mpSrc += 2;
			mbUTF16Mode = true;
			mbBigEndian = true;
		} else if (mpSrc[0] == 0xFF && mpSrc[1] == 0xFE) {
			mpSrc += 2;
			mbUTF16Mode = true;
			mbBigEndian = false;
		}

		if (mbUTF16Mode && ((mpSrcEnd - mpSrc) & 1))
			--mpSrcEnd;
	}

	return ParseDocument();
}

void VDJSONReader::GetErrorLocation(int& line, int& offset) const {
	int ln = mInputLine;
	int of = mInputChar;

	for(const wchar_t *p = mpInputBase; p != mpInputNext; ++p) {
		++of;
		if (*p == L'\n') {
			++ln;
			of = 0;
		}
	}

	line = ln;
	offset = of;
}

// JSON-text := object | array
// object := '{' member (',' member)* '}'
// member := string ':' value
// array := '[' value (',' value)* ']'
// value := 'false' | 'null' | 'true' | object | array | number | string
// number := '-'? int frac? exp?
// int := (0 | [1-9][0-9]*)
// frac := ('.' [0-9]+)
// exp := ([eE] ('+' | '-') [0-9]+)
// string := '"' char* '"'
// char := unescaped | escaped
// unescaped := U+0000 - U+0021 | U+0023 - U+005B | U+005D - U+10FFFF
// escaped := '\' (["/\bfnrt] | u[0-9a-fA-F]{4})

bool VDJSONReader::ParseDocument() {
	wchar_t c = GetNonWhitespaceChar();

	if (c == '{') {
		if (!ParseObject(mpDocument->mValue))
			return false;
	} else if (c == '[') {
		if (!ParseArray(mpDocument->mValue))
			return false;
	} else {
		return false;
	}

	return true;
}

bool VDJSONReader::ParseObject(VDJSONValue& obj) {
	obj.mType = VDJSONValue::kTypeObject;
	obj.mpObject = NULL;

	wchar_t c = GetNonWhitespaceChar();
	if (c != L'{') {
		UngetChar();

		typedef vdhashmap<uint32, VDJSONValue *> Lookup;
		Lookup lookup;

		for(;;) {
			if (GetNonWhitespaceChar() != L'"')
				return false;

			if (!ParseString())
				return false;

			uint32 nameToken = GetTokenForName();

			if (GetNonWhitespaceChar() != L':')
				return false;

			VDJSONValue val;
			if (!ParseValue(val))
				return false;

			std::pair<Lookup::iterator, bool> result(lookup.insert(Lookup::value_type(nameToken, (VDJSONValue *)NULL)));
			if (result.second)
				result.first->second = mpDocument->mPool.AddObjectMember(obj, nameToken);

			*result.first->second = val;

			c = GetNonWhitespaceChar();
			if (c == L'}')
				break;

			if (c != L',')
				return false;
		}
	}

	return true;
}

bool VDJSONReader::ParseArray(VDJSONValue& arr) {
	size_t arrayBase = mArrayStack.size();
	size_t arrayLen = arrayBase;

	wchar_t c = GetNonWhitespaceChar();
	if (c != L']') {
		UngetChar();

		for(;;) {
			mArrayStack.resize(++arrayLen);

			VDJSONValue val;
			if (!ParseValue(val))
				return false;

			// must delay this as ParseValue() may parse an array
			mArrayStack.back() = val;

			wchar_t c = GetNonWhitespaceChar();
			if (c == L']')
				break;

			if (c != L',')
				return false;
		}
	}

	mpDocument->mPool.AddArray(arr, arrayLen - arrayBase);

	if (arrayLen != arrayBase) {
		memcpy(arr.mpArray->mpElements, &mArrayStack[arrayBase], sizeof(VDJSONValue) * (arrayLen - arrayBase));

		mArrayStack.resize(arrayBase);
	}

	return true;
}

bool VDJSONReader::ParseValue(VDJSONValue& val) {
	wchar_t c = GetNonWhitespaceChar();

	if (c == L'{') {
		if (!ParseObject(val))
			return false;
	} else if (c == L'[') {
		if (!ParseArray(val))
			return false;
	} else if (c == L'"') {
		if (!ParseString())
			return false;

		mpDocument->mPool.AddString(val, mNameBuffer, mNameBufferIndex);
	} else if (c == L't') {
		if (!Expect(L'r') || !Expect(L'u') || !Expect(L'e'))
			return false;

		val.Set(true);
	} else if (c == L'f') {
		if (!Expect(L'a') || !Expect(L'l') || !Expect(L's') || !Expect(L'e'))
			return false;

		val.Set(false);
	} else if (c == L'n') {
		if (!Expect(L'u') || !Expect(L'l') || !Expect(L'l'))
			return false;

		val.Set();
	} else if (c == L'-' || (c >= L'0' && c <= L'9')) {
		bool neg = false;
		bool isReal = false;

		ClearNameBuffer();
		uint64 v = 0;

		if (c == L'-') {
			neg = true;
			AddNameChar(c);
			c = GetChar();
		}

		if (c == L'0') {
			AddNameChar(c);
			c = GetChar();
		} else if (c >= L'1' && c <= L'9') {
			do {
				if (!isReal) {
					uint64 vNew = v * 10 + (int)(c - L'0');

					if (vNew < v)
						isReal = true;
					else
						v = vNew;
				}

				AddNameChar(c);
				c = GetChar();
			} while(c >= L'0' && c <= L'9');

			if (neg) {
				if (v > 0x7FFFFFFFFFFFFFFFULL)
					isReal = true;
			} else {
				if (v > 0x8000000000000000ULL)
					isReal = true;
			}
		} else
			return false;

		if (c == L'.') {
			isReal = true;

			do {
				AddNameChar(c);
				c = GetChar();
			} while(c >= L'0' && c <= L'9');
		}

		if (c == L'e' || c == L'E') {
			isReal = true;

			AddNameChar(c);
			c = GetChar();

			if (c != L'-' && c != L'+')
				return false;

			do {
				AddNameChar(c);
				c = GetChar();
			} while(c >= L'0' && c <= L'9');
		}

		EndName();

		if (isReal)
			val.Set(wcstod(mNameBuffer, NULL));
		else if (neg)
			val.Set(-(sint64)v);
		else
			val.Set((sint64)v);

		UngetChar();
	} else
		return false;

	return true;
}

bool VDJSONReader::ParseString() {
	ClearNameBuffer();

	for(;;) {
		wchar_t c = GetChar();

		if (c == L'"')
			break;

		if ((unsigned)c < 0x20)
			return false;

		if (c == L'\\') {
			c = GetChar();

			switch(c) {
				case L'"':
				case L'\\':
				case L'/':
					break;

				case L'b':
					c = (wchar_t)L'\b';
					break;

				case L'f':
					c = (wchar_t)L'\b';
					break;

				case L'n':
					c = (wchar_t)L'\n';
					break;

				case L'r':
					c = (wchar_t)L'\r';
					break;

				case L't':
					c = (wchar_t)L'\t';
					break;

				case L'u':
					for(int i=0; i<4; ++i) {
						c = GetChar();

						if ((c < L'0' || c > '9') && (c < L'a' || c > 'f') && (c < L'A' || c > 'F'))
							return false;
					}
					break;
			}
		}

		AddNameChar(c);
	}

	EndName();

	return true;
}

bool VDJSONReader::Expect(wchar_t expected) {
	wchar_t c = GetChar();

	if (c != expected)
		return false;

	return true;
}

void VDJSONReader::ClearNameBuffer() {
	mNameBufferIndex = 0;
}

bool VDJSONReader::AddNameChar(wchar_t c) {
	if (mNameBufferIndex >= mNameBufferLength) {
		int newLen = mNameBufferLength ? mNameBufferLength*2 : 64;
		wchar_t *newbuf = new wchar_t[newLen];
		if (!newbuf)
			return false;

		memcpy(newbuf, mNameBuffer, mNameBufferIndex * sizeof(mNameBuffer[0]));
		delete[] mNameBuffer;
		mNameBuffer = newbuf;
		mNameBufferLength = newLen;
	}

	mNameBuffer[mNameBufferIndex++] = c;
	return true;
}

bool VDJSONReader::EndName() {
	if (!AddNameChar(0))
		return false;

	--mNameBufferIndex;
	return true;
}

bool VDJSONReader::IsWhitespaceChar(wchar_t c) {
	return (c == 0x20 || c == 0x09 || c == 0x0d || c == 0x0a);
}

void VDJSONReader::UngetChar() {
	if (mpInputNext != mpInputBase)
		--mpInputNext;
}

wchar_t VDJSONReader::GetNonWhitespaceChar() {
	for(;;) {
		wchar_t c = GetChar();

		if (!IsWhitespaceChar(c))
			return c;
	}
}

wchar_t VDJSONReader::GetChar() {
	if (mpInputNext == mpInputEnd)
		return GetCharSlow();

	wchar_t c = *mpInputNext++;

	//putchar(c);
	return c;
}

wchar_t VDJSONReader::GetCharSlow() {
	if (mbSrcError)
		return 0;
	
	if (mpSrc != mpSrcEnd) {
		int len = 0;

		if (mbPendingCR) {
			mInputBuffer[0] = L'\n';
			len = 1;
		}

		bool encodingError = false;

		if (mbUTF32Mode) {
			if (sizeof(wchar_t) >= 4) {
				int tc = (mpSrcEnd - mpSrc) >> 2;

				if (tc > kInputBufferSize - len)
					tc = kInputBufferSize - len;

				if (mbBigEndian) {
					for(int i=0; i<tc; ++i) {
						mInputBuffer[i+len] = (wchar_t)(((uint32)mpSrc[0] << 24) + ((uint32)mpSrc[1] << 16) + ((uint32)mpSrc[2] << 8) + mpSrc[3]);
						mpSrc += 4;
					}
				} else {
					memcpy(mInputBuffer + len, mpSrc, tc * sizeof(wchar_t));
					mpSrc += tc*4;
				}

				// validate code points
				for(int i=0; i<tc; ++i) {
					uint32 c = mInputBuffer[i + len];

					if ((c - 0xD800) < 0x0800 || c >= 0x110000) {
						encodingError = true;
						tc = i;
						break;
					}
				}

				len += tc;
			} else {
				if (mbBigEndian) {
					while(len < kInputBufferSize && mpSrcEnd - mpSrc > 4) {
						uint32 c = ((uint32)mpSrc[0] << 24) + ((uint32)mpSrc[1] << 16) + ((uint32)mpSrc[2] << 8) + mpSrc[3];
						mpSrc += 4;

						// validate code point
						if ((c - 0xD800) < 0x0800 || c >= 0x110000) {
							encodingError = true;
							break;
						}

						// check if we need to fragment
						if (c >= 0x10000) {
							// check if have space for the second surrogate
							if (len >= kInputBufferSize - 1)
								break;

							// write the first surrogate
							c -= 0x10000;

							mInputBuffer[len++] = 0xD800 + (c >> 10);
							c = (wchar_t)(0xDC00 + (c & 0x03FF));
						}

						mInputBuffer[len++] = (wchar_t)c;
					}
				} else {
					while(len < kInputBufferSize && mpSrcEnd - mpSrc > 4) {
						uint32 c = ((uint32)mpSrc[3] << 24) + ((uint32)mpSrc[2] << 16) + ((uint32)mpSrc[1] << 8) + mpSrc[0];
						mpSrc += 4;

						// validate code point
						if ((c - 0xD800) < 0x0800 || c >= 0x110000) {
							encodingError = true;
							break;
						}

						// check if we need to fragment
						if (c >= 0x10000) {
							// check if have space for the second surrogate
							if (len >= kInputBufferSize - 1)
								break;

							// write the first surrogate
							c -= 0x10000;

							mInputBuffer[len++] = 0xD800 + (c >> 10);
							c = (wchar_t)(0xDC00 + (c & 0x03FF));
						}

						mInputBuffer[len++] = (wchar_t)c;
					}
				}
			}
		} else if (mbUTF16Mode) {
			if (sizeof(wchar_t) > 2) {		// UTF-16 -> UTF-32 conversion
				if (mbBigEndian) {
					while(len < kInputBufferSize && (mpSrcEnd - mpSrc) > 2) {
						uint32 c0 = ((uint32)mpSrc[0] << 8) + mpSrc[1];
						mpSrc += 2;

						if ((c0 - 0xD800) < 0x0800) {
							if (c0 >= 0xDC00 || mpSrcEnd - mpSrc < 2) {
								encodingError = true;
								break;
							}

							uint32 c1 = ((uint32)mpSrc[0] << 8) + mpSrc[1];
							mpSrc += 2;

							if ((c0 - 0xDC00) >= 0x0800) {
								encodingError = true;
								break;
							}

							c0 = (c0 << 10) + c1 - ((0xD800 << 10) + 0xDC00);
						}

						mInputBuffer[len++] = c0;
					}
				} else {
					while(len < kInputBufferSize && (mpSrcEnd - mpSrc) > 2) {
						uint32 c0 = ((uint32)mpSrc[1] << 8) + mpSrc[0];
						mpSrc += 2;

						if ((c0 - 0xD800) < 0x0800) {
							if (c0 >= 0xDC00 || mpSrcEnd - mpSrc < 2) {
								encodingError = true;
								break;
							}

							uint32 c1 = ((uint32)mpSrc[1] << 8) + mpSrc[0];
							mpSrc += 2;

							if ((c0 - 0xDC00) >= 0x0800) {
								encodingError = true;
								break;
							}

							c0 = (c0 << 10) + c1 - ((0xD800 << 10) + 0xDC00);
						}

						mInputBuffer[len++] = c0;
					}
				}
			} else {
				int tc = (mpSrcEnd - mpSrc) >> 1;

				if (tc > kInputBufferSize - len)
					tc = kInputBufferSize - len;

				if (mbBigEndian) {
					for(int i=0; i<tc; ++i) {
						mInputBuffer[i+len] = (wchar_t)((mpSrc[0] << 8) + mpSrc[1]);
						mpSrc += 2;
					}
				} else {
					memcpy(mInputBuffer + len, mpSrc, tc * sizeof(wchar_t));
					mpSrc += tc*2;
				}

				len += tc;
			}
		} else {
			while(len < kInputBufferSize && mpSrc != mpSrcEnd) {
				uint8 c = *mpSrc++;
				wchar_t d = c;

				if ((c >= 0x80 && c < 0xC2) || c >= 0xF5) {		// invalid: follower without leader or too high of a code point
					encodingError = true;
					break;
				} else if (c >= 0xC2 && c < 0xDF) {		// U+0080 to U+07FF
					if (mpSrc == mpSrcEnd) {
						encodingError = true;
						break;
					}

					uint8 x0 = *mpSrc++;
					if (x0 < 0x80 || x0 > 0xBF) {
						encodingError = true;
						break;
					}

					d = ((c - 0xC0) << 6) + x0;
				} else if (c >= 0xE0 && c <= 0xEF) {	// U+0800 to U+FFFF
					if (mpSrcEnd - mpSrc < 2) {
						encodingError = true;
						break;
					}

					uint8 x0 = *mpSrc++;
					uint8 x1 = *mpSrc++;
					if (x0 < 0x80 || x0 > 0xBF || x1 < 0x80 || x1 > 0xBF) {
						encodingError = true;
						break;
					}

					d = ((c - 0xE0) << 12) + (((wchar_t)x0 - 0x80) << 6) + (wchar_t)(x1 - 0x80);

					// reject invalid code points
					if (d < 0x0800 || (d - 0xD800) < 0x0800) {
						encodingError = true;
						break;
					}
				} else if (c >= 0xF0 && c <= 0xF7) {
					if (mpSrcEnd - mpSrc < 3) {
						encodingError = true;
						break;
					}

					uint8 x0 = *mpSrc++;
					uint8 x1 = *mpSrc++;
					uint8 x2 = *mpSrc++;
					if (x0 < 0x80 || x0 > 0xBF || x1 < 0x80 || x1 > 0xBF || x2 < 0x80 || x2 > 0xBF) {
						encodingError = true;
						break;
					}

					uint32 e = ((uint32)(c - 0xF0) << 18) + (((uint32)x0 - 0x80) << 12) + (((uint32)x1 - 0x80) << 6) + (uint32)(x2 - 0x80);
					if (e < 0x10000) {
						encodingError = true;
						break;
					}

					// fragment into surrogates if required
					if (sizeof(wchar_t) <= 2) {
						if (len >= kInputBufferSize - 1)
							break;

						e -= 0x10000;

						mInputBuffer[len++] = (wchar_t)(0xD800 + (e >> 10));
						d = (wchar_t)(0xDC00 + (e & 0x03FF));
					}
				}

				mInputBuffer[len++] = d;
			}
		}

		mbPendingCR = false;

		wchar_t *dst = mInputBuffer;

		mInputLine = mInputLineNext;
		mInputChar = mInputCharNext;

		for(int i=0; i<len; ++i) {
			wchar_t c = mInputBuffer[i];

			if (c == L'\r') {
				mbPendingCR = true;
				c = L'\n';
			} else if (mbPendingCR) {
				mbPendingCR = false;

				if (c == L'\n')
					continue;
			}

			*dst++ = c;

			++mInputCharNext;
			if (c == L'\n') {
				mInputCharNext = 0;
				++mInputLineNext;
			}
		}

		mpInputBase = mInputBuffer;
		mpInputNext = mInputBuffer;
		mpInputEnd = dst;

		if (encodingError) {
			mpInputNext = dst;
			mbSrcError = true;
			return 0;
		}

		if (dst != mInputBuffer)
			return *mpInputNext++;
	}

	if (mbPendingCR) {
		mbPendingCR = false;
		mInputBuffer[0] = L'\n';
		mpInputBase = mInputBuffer;
		mpInputNext = mInputBuffer;
		mpInputEnd = mInputBuffer + 1;
		return L'\n';
	}

	mpInputBase = NULL;
	mpInputNext = NULL;
	mpInputEnd = NULL;
	return 0;
}

uint32 VDJSONReader::GetTokenForName() {
	return mpNameTable->AddName(mNameBuffer, wcslen(mNameBuffer));
}
