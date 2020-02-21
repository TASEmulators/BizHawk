//	VDCompiler - Custom shader video filter for VirtualDub
//	Copyright (C) 2007 Avery Lee
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
#include <math.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/file.h>
#include <vd2/system/text.h>
#include <vd2/system/VDString.h>
#include <vd2/system/binary.h>
#include "filecreator.h"

using namespace nsVDCompilerTokens;

namespace {
	uint32 Checksum(const char *s, int len) {
		uint32 sum = 0;

		while(len--)
			sum += *s++;

		return sum;
	}
}

void VDCompilerWriteLogOutputF(IVDCompilerLogOutput& out, const char *format ...) {
	char buf[3072];
	va_list val;

	va_start(val, format);
	int len = _vsnprintf(buf, 3071, format, val);
	va_end(val);

	if ((unsigned)len <= 3071) {
		buf[len] = 0;
		out.WriteLogOutput(buf);
	}
}

void VDCompilerLexer::Init(const char *src, size_t len, const char *context, IVDCompilerLogOutput *pOutput) {
	mpSrc = src;
	mpSrcEnd = src + len;
	mpSrcLineStart = src;
	mpToken = src;
	mpTokenLineStart = src;
	mpContext = context;
	mLineNo = 1;
	mTokenLength = 0;
	mbError = false;
	mpOutput = pOutput;
}

bool VDCompilerLexer::GetErrorInfo(VDCompilerErrorInfo& errorInfo) {
	if (!mbError)
		return false;

	errorInfo = mErrorInfo;
	return true;
}

void VDCompilerLexer::AddToken(int token, const char *s) {
	int len = (int)strlen(s);
	uint32 checksum = Checksum(s, len);
	Keyword k;

	k.len = len;
	k.text = s;
	k.token = token;

	mKeywords.insert(Keywords::value_type(checksum, k));

}

int VDCompilerLexer::Token() {
	bool inComment = false;
	char c;

	// parse out whitespace and comments
	for(;;) {
		if (mpSrc == mpSrcEnd) {
			mpTokenLineStart = mpSrcLineStart;
			mpToken = mpSrc;
			mTokenLength = 0;
			return 0;
		}

		c = *mpSrc++;
		if (c == ' ' || c == '\t')
			continue;

		if (c == '\n' || c == '\r') {
			if (mpSrc != mpSrcEnd) {
				char d = *mpSrc;
				if ((c ^ d) == ('\n' ^ '\r'))
					++mpSrc;
			}
			++mLineNo;
			mpSrcLineStart = mpSrc;
			continue;
		}

		if (inComment) {
			if (c == '*' && mpSrc != mpSrcEnd && *mpSrc == '/') {
				++mpSrc;

				inComment = false;
			}
		} else {
			if (c == '/' && mpSrc != mpSrcEnd) {
				char d = *mpSrc;
				if (d == '/') {				// C++-style comment
					++mpSrc;
					while(mpSrc != mpSrcEnd) {
						c = *mpSrc;

						if (c == '\n' || c == '\r')
							break;

						++mpSrc;
					}
					continue;
				} else if (d == '*') {		// C-style comment
					++mpSrc;

					inComment = true;
					continue;
				}
			}

			break;
		}
	}

	mpToken = mpSrc - 1;
	mpTokenLineStart = mpSrcLineStart;

	// check for numeric tokens
	if ((unsigned char)(c - '0') < 10) {
		bool overflow = false;
		int value = c - '0';

		if (!value && mpSrc != mpSrcEnd && (*mpSrc == 'x' || *mpSrc == 'X')) {
			++mpSrc;

			int digits = 0;
			mIntVal = 0;
			while(mpSrc != mpSrcEnd) {
				c = *mpSrc;
				if (!isxdigit((unsigned char)c))
					break;
				++mpSrc;

				c = (c - '0') & 0x1f;
				if (c >= 10)
					c -= 7;

				mIntVal = (mIntVal << 4) + c;
				++digits;
			}

			if (!digits) {
				SetError("Invalid hex constant");
				return 0;
			}

			return kTokenInt;
		}

		while(mpSrc != mpSrcEnd) {
			c = *mpSrc;

			if (c == '.') {
				double fvalue = 0;

				for(const char *s = mpToken; s != mpSrc; ++s)
					fvalue = fvalue * 10.0 + (float)(*s - '0');

				++mpSrc;

				double scale = 0.1;
				while(mpSrc != mpSrcEnd) {
					c = *mpSrc;

					int digit = (c - '0');
					if ((unsigned)digit >= 10)
						break;

					++mpSrc;
					fvalue += scale * (float)digit;
					scale *= 0.1;
				}

				if (mpSrc != mpSrcEnd) {
					c = *mpSrc;
					if (c == 'e' || c == 'E') {
						++mpSrc;

						const char *pExpBase = mpSrc;
						int exp = 0;

						while(mpSrc != mpSrcEnd) {
							c = *mpSrc;

							int digit = (c - '0');
							if ((unsigned)digit >= 10)
								break;

							exp = (exp*10) + digit;

							++mpSrc;
						}

						if (mpSrc == pExpBase) {
							SetError("Invalid real number");
							return 0;
						}

						fvalue *= pow(10.0, exp);
					}
				}

				if (mpSrc != mpSrcEnd) {
					c = *mpSrc;
					if (c == 'f' || c == 'F')
						++mpSrc;
				}

				mFloatVal = (float)fvalue;
				mTokenLength = mpSrc - mpToken;
				return kTokenFloat;
			}

			int digit = (c - '0');
			if ((unsigned)digit >= 10)
				break;
			++mpSrc;

			if (value > 214748364)
				overflow = true;

			value *= 10;

			if (value > 2147483647 - digit)
				overflow = true;

			value += digit;
		
		}

		if (overflow) {
			SetError("Integer literal too large");
			return 0;
		}

		mIntVal = value;
		mTokenLength = mpSrc - mpToken;
		return kTokenInt;
	}

	// check for identifiers
	if (c == '_' || (unsigned char)((c & 0xdf) - 'A') < 26) {
		while(mpSrc != mpSrcEnd) {
			c = *mpSrc++;

			if (c != '_' && (unsigned char)((c & 0xdf) - 'A') >= 26 && (unsigned char)(c - '0') >= 10) {
				--mpSrc;
				break;
			}
		}

		mTokenLength = mpSrc - mpToken;

		uint32 checksum = Checksum(mpToken, mTokenLength);

		std::pair<Keywords::const_iterator, Keywords::const_iterator> range(mKeywords.equal_range(checksum));

		for(; range.first != range.second; ++range.first)
		{
			const Keyword& kw = range.first->second;

			if (kw.len == mTokenLength && !memcmp(kw.text, mpToken, mTokenLength))
				return kw.token;
		}

		return kTokenIdent;
	}

	// check for single character
	if (c == '\'') {
		if (mpSrc == mpSrcEnd) {
			SetError("End of file encountered in character literal");
			return 0;
		}

		c = *mpSrc++;
		bool escaped = false;
		if (c == '\\') {
			escaped = true;
			if (mpSrc == mpSrcEnd) {
				SetError("End of file encountered in character literal");
				return 0;
			}
			c = *mpSrc++;
		} else {
			if (c == '\'') {
				SetError("Empty character literal");
				return 0;
			}
		}

		mIntVal = (int)(uint8)c;

		if (mpSrc == mpSrcEnd) {
			SetError("End of file encountered in character literal");
			return 0;
		}

		c = *mpSrc++;
		if (c != '\'') {
			SetError("Expected \' at end of character literal");
			return 0;
		}

		mTokenLength = mpSrc - mpToken;
		return kTokenInt;
	}

	// check for string
	if (c == '"') {
		mString.clear();
		for(;;) {
			if (mpSrc == mpSrcEnd) {
				SetError("End of file encountered in string literal");
				return 0;
			}

			c = *mpSrc++;
			if (c == '\r' || c == '\n') {
				SetError("Newline encountered in string literal");
				return 0;
			}

			if (c == '"')
				break;

			if (c == '\\') {
				if (mpSrc == mpSrcEnd) {
					SetError("Invalid escape sequence");
					return 0;
				}

				c = *mpSrc++;
				switch(c) {
					case 'a':	c = '\a';	break;
					case 'b':	c = '\b';	break;
					case 'f':	c = '\f';	break;
					case 'n':	c = '\n';	break;
					case 'r':	c = '\r';	break;
					case 't':	c = '\t';	break;
					case 'v':	c = '\v';	break;
					case 'x':
						c = 0;
						for(;;) {
							if (mpSrc == mpSrcEnd) {
								SetError("Unterminated hex escape sequence");
								return 0;
							}

							char d = *mpSrc;
							if (!isxdigit((unsigned char)d))
								break;
							++mpSrc;

							d = (d - 0x30) & 0x1f;
							if (d >= 10)
								d -= 7;

							c = (c << 4) + d;
						}
						break;
					case '\\':
						break;
					default:
						SetErrorF("Unknown escape sequence \\%c", c);
						return 0;
				}
			}
			mString.push_back(c);
		}

		mTokenLength = mpSrc - mpToken;
		return kTokenString;
	}

	if ((unsigned char)(c - 0x20) >= 0x5f) {
		SetErrorF("Unrecognized character '%c'", c);
		return 0;
	}

	mTokenLength = 1;
	return c;
}

void VDCompilerLexer::Push() {
	mpSrc = mpToken;
}

bool VDCompilerLexer::SetError(const char *s) {
	if (mbError)
		return false;

	mbError = true;
	mErrorInfo.mLine = mLineNo;
	mErrorInfo.mColumn = (mpToken - mpSrcLineStart) + 1;
	mErrorInfo.mLength = mTokenLength;

	char buf[2048];
	if (_snprintf(buf, 2047, "%s(%d,%d): Error! %s\r\n", mpContext, mLineNo, (int)(mpSrc - mpSrcLineStart) + 1, s) > 0) {
		buf[2047] = 0;
		mpOutput->WriteLogOutput(buf);
	}
	return false;
}

bool VDCompilerLexer::SetErrorF(const char *format, ...) {
	if (mbError)
		return false;

	mbError = true;
	mErrorInfo.mLine = mLineNo;
	mErrorInfo.mColumn = (mpToken - mpSrcLineStart) + 1;
	mErrorInfo.mLength = mTokenLength;

	va_list val;
	char buf[2048], *s = buf, *limit = s + 2047;
	int len;
	len = _snprintf(s, limit-s, "%s(%d,%d): Error! ", mpContext, mLineNo, (int)(mpSrc - mpSrcLineStart) + 1);
	if (len >= 0) {
		s += len;
		va_start(val, format);
		len = _vsnprintf(s, limit-s, format, val);
		va_end(val);

		if (len >= 0) {
			s += len;

			if (limit-s >= 2) {
				*s++ = '\r';
				*s++ = '\n';
				*s = 0;
				mpOutput->WriteLogOutput(buf);
			}
		}
	}
	return false;
}

///////////////////////////////////////////////////////////////////////////////

namespace {
	enum Opcode {
		kInsnEnd,
		kInsnLoadConstI4,
		kInsnLoadConstI8,
		kInsnLoadConstR8,
		kInsnDupI4,
		kInsnWriteI1,
		kInsnWriteI2,
		kInsnWriteI4,
		kInsnWriteI8,
		kInsnWriteR4,
		kInsnWriteR8,
		kInsnSetIndexAnchor,
		kInsnWriteIndex,
		kInsnBeginChunk,
		kInsnEndChunk,
		kInsnEndIndexedChunk
	};
}

class VDFileCreator : public vdrefcounted<IVDFileCreator> {
public:
	VDFileCreator();

	void Create(const wchar_t *filename);

	void Emit8(uint8 c);
	void Emit16(uint16 c);
	void Emit32(uint32 c);
	void Emit64(uint64 c);
	void EmitR8(double c);

protected:
	sint64	mIndexOffset;

	typedef vdfastvector<uint8> Bytecode;
	Bytecode	mBytecode;

	union StackVal {
		uint32	i4;
		uint64	i8;
		double	r8;
		const char *s;
	};

	typedef vdfastvector<StackVal> Stack;
	Stack	mStack;

	struct IndexEntry {
		uint32 pos;
		uint32 size;
		uint32 flags;
		uint32 ckid;
	};

	typedef vdfastvector<IndexEntry> Index;
	Index mIndex;

	typedef vdfastvector<sint64> Chunks;
	Chunks mChunks;
};

VDFileCreator::VDFileCreator()
	: mIndexOffset(0)
{
}

void VDFileCreator::Create(const wchar_t *filename) {
	VDFile file(filename, nsVDFile::kWrite | nsVDFile::kDenyRead | nsVDFile::kCreateAlways);

	const uint8 *ip = mBytecode.data();
	while(uint8 op = *ip++) {
		switch(op) {
			case kInsnLoadConstI4:
				mStack.push_back().i4 = VDReadUnalignedS32(ip);
				ip += 4;
				break;

			case kInsnLoadConstI8:
				mStack.push_back().i8 = VDReadUnalignedS64(ip);
				ip += 8;
				break;

			case kInsnLoadConstR8:
				mStack.push_back().r8 = VDReadUnalignedD(ip);
				ip += 8;
				break;

			case kInsnDupI4:
				{
					sint32 v = mStack.back().i4;
					mStack.push_back().i4 = v;
				}
				break;

			case kInsnWriteI1:
				{
					uint8 c = (uint8)mStack.back().i4;
					mStack.pop_back();

					file.write(&c, 1);
				}
				break;

			case kInsnWriteI2:
				{
					uint16 c = (uint16)mStack.back().i4;
					mStack.pop_back();

					file.write(&c, 2);
				}
				break;

			case kInsnWriteI4:
				{
					uint32 c = mStack.back().i4;
					mStack.pop_back();

					file.write(&c, 4);
				}
				break;

			case kInsnWriteI8:
				{
					uint64 c = mStack.back().i8;
					mStack.pop_back();

					file.write(&c, 8);
				}
				break;

			case kInsnWriteR4:
				{
					float c = (float)mStack.back().r8;
					mStack.pop_back();

					file.write(&c, 4);
				}
				break;

			case kInsnWriteR8:
				{
					double c = mStack.back().r8;
					mStack.pop_back();

					file.write(&c, 8);
				}
				break;

			case kInsnSetIndexAnchor:
				mIndexOffset = file.tell();
				break;

			case kInsnWriteIndex:
				{
					Index::const_iterator it(mIndex.begin()), itEnd(mIndex.end());
					for(; it!=itEnd; ++it) {
						const IndexEntry& ie = *it;

						uint32 t[4];
						t[0] = ie.ckid;
						t[1] = ie.flags;
						t[2] = ie.pos;
						t[3] = ie.size;

						file.write(t, sizeof t);
					}
				}
				break;

			case kInsnBeginChunk:
				{
					mChunks.push_back(file.tell());

					uint32 c = 0;
					file.write(&c, 4);
				}
				break;

			case kInsnEndChunk:
				{
					sint64 pos = file.tell();
					sint64 cpos = mChunks.back();
					mChunks.pop_back();
					file.seek(cpos);

					uint32 size = (uint32)(pos - (cpos + 4));
					file.write(&size, 4);

					uint8 pad = 0;
					if (pos & 1)
						file.write(&pad, 1);

					file.seek(pos);
				}
				break;

			case kInsnEndIndexedChunk:
				{
					uint32 flags = mStack.back().i4;
					mStack.pop_back();

					uint32 ckid = mStack.back().i4;
					mStack.pop_back();

					sint64 pos = file.tell();
					sint64 cpos = mChunks.back();
					mChunks.pop_back();
					file.seek(cpos);

					uint32 size = (uint32)(pos - (cpos + 4));
					file.write(&size, 4);

					uint8 pad = 0;
					if (pos & 1)
						file.write(&pad, 1);

					file.seek(pos);

					IndexEntry ie;
					ie.pos = (uint32)((cpos - 4) - mIndexOffset);
					ie.size = size;
					ie.flags = flags;
					ie.ckid = ckid;

					mIndex.push_back(ie);
				}
				break;
		}
	}
}

void VDFileCreator::Emit8(uint8 c) {
	mBytecode.push_back(c);
}

void VDFileCreator::Emit16(uint16 c) {
	mBytecode.insert(mBytecode.end(), (const uint8 *)&c, (const uint8 *)(&c+1));
}

void VDFileCreator::Emit32(uint32 c) {
	mBytecode.insert(mBytecode.end(), (const uint8 *)&c, (const uint8 *)(&c+1));
}

void VDFileCreator::Emit64(uint64 c) {
	mBytecode.insert(mBytecode.end(), (const uint8 *)&c, (const uint8 *)(&c+1));
}

void VDFileCreator::EmitR8(double c) {
	mBytecode.insert(mBytecode.end(), (const uint8 *)&c, (const uint8 *)(&c+1));
}

///////////////////////////////////////////////////////////////////////////////

namespace {
	enum {
		kKeywordWrite_i1 = kTokenKeywordBase,
		kKeywordWrite_i2,
		kKeywordWrite_i4,
		kKeywordWrite_i8,
		kKeywordWrite_u1,
		kKeywordWrite_u2,
		kKeywordWrite_u4,
		kKeywordWrite_u8,
		kKeywordWrite_r4,
		kKeywordWrite_r8,
		kKeywordWrite_fcc,
		kKeywordSet_index_anchor,
		kKeywordAdd_to_index,
		kKeywordWrite_index,
		kKeywordChunk,
		kKeywordIndexed_chunk
	};

	enum ExpressionOp {
		kOpNone,
		kOpAdd,
		kOpSub,
		kOpMul,
		kOpDiv,
		kOpNegate,
		kOpSwizzle,
	};

	enum VDFileCreatorValueType {
		kTypeI1,
		kTypeI2,
		kTypeI4,
		kTypeI8,
		kTypeU1,
		kTypeU2,
		kTypeU4,
		kTypeU8,
		kTypeR4,
		kTypeR8,
		kTypeS
	};

	const char *GetTypeName(VDFileCreatorValueType type) {
		switch(type) {
			case kTypeI1:
				return "int8";

			case kTypeI2:
				return "int16";

			case kTypeI4:
				return "int32";

			case kTypeI8:
				return "int64";

			case kTypeU1:
				return "uint8";

			case kTypeU2:
				return "uint16";

			case kTypeU4:
				return "uint32";

			case kTypeU8:
				return "uint64";

			case kTypeR4:
				return "float";

			case kTypeR8:
				return "double";

			case kTypeS:
				return "string";

			default:
				return "???";
		}
	}

	VDFileCreatorValueType GetBinaryResultType(VDFileCreatorValueType type1, VDFileCreatorValueType type2) {
		// promote to at least double or int32/uint32
		switch(type1) {
			case kTypeI1:
			case kTypeI2:
				type1 = kTypeI4;
				break;
			case kTypeU1:
			case kTypeU2:
				type1 = kTypeU4;
				break;
			case kTypeR4:
				type1 = kTypeR8;
				break;
		}

		switch(type2) {
			case kTypeI1:
			case kTypeI2:
				type2 = kTypeI4;
				break;
			case kTypeU1:
			case kTypeU2:
				type2 = kTypeU4;
				break;
			case kTypeR4:
				type2 = kTypeR8;
				break;
		}

		// promote to double if either is double
		if (type1 == kTypeR8 || type2 == kTypeR8)
			return kTypeR8;

		// promote to uint64 if either is uint64
		if (type1 == kTypeU8 || type2 == kTypeU8)
			return kTypeU8;

		// promote to int64 if either is int64
		if (type1 == kTypeI8 || type2 == kTypeI8)
			return kTypeI8;

		// promote to int64 if uint32 and int32 are mixed
		if ((type1 == kTypeI4 && type2 == kTypeU4) || (type1 == kTypeU4 && type2 == kTypeI4))
			return kTypeI8;

		VDASSERT(type1 == type2);
		return type1;
	}
}

struct VDFileCreatorValue {
	VDFileCreatorValueType	mType;
	union {
		sint32 i4;
		sint64 i8;
		double r8;
		const char *s;
	};

	bool ConvertToType(VDFileCreatorValueType type);
};

bool VDFileCreatorValue::ConvertToType(VDFileCreatorValueType type) {
	switch(type) {
	case kTypeI1:
		switch(mType) {
		case kTypeU1:	i4 = (sint8 )(uint8 )i4;	break;
		case kTypeI1:	i4 = (sint8 )(sint8 )i4;	break;
		case kTypeI2:	i4 = (sint8 )(sint16)i4;	break;
		case kTypeU2:	i4 = (sint8 )(uint16)i4;	break;
		case kTypeI4:	i4 = (sint8 )(sint32)i4;	break;
		case kTypeU4:	i4 = (sint8 )(uint32)i4;	break;
		case kTypeI8:	i4 = (sint8 )(sint64)i8;	break;
		case kTypeU8:	i4 = (sint8 )(uint64)i8;	break;
		case kTypeR4:	i4 = (sint8 )(float )r8;	break;
		case kTypeR8:	i4 = (sint8 )(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeU1:
		switch(mType) {
		case kTypeU1:	i4 = (uint8 )(uint8 )i4;	break;
		case kTypeI1:	i4 = (uint8 )(sint8 )i4;	break;
		case kTypeI2:	i4 = (uint8 )(sint16)i4;	break;
		case kTypeU2:	i4 = (uint8 )(uint16)i4;	break;
		case kTypeI4:	i4 = (uint8 )(sint32)i4;	break;
		case kTypeU4:	i4 = (uint8 )(uint32)i4;	break;
		case kTypeI8:	i4 = (uint8 )(sint64)i8;	break;
		case kTypeU8:	i4 = (uint8 )(uint64)i8;	break;
		case kTypeR4:	i4 = (uint8 )(float )r8;	break;
		case kTypeR8:	i4 = (uint8 )(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeI2:
		switch(mType) {
		case kTypeU1:	i4 = (sint16)(uint8 )i4;	break;
		case kTypeI1:	i4 = (sint16)(sint8 )i4;	break;
		case kTypeI2:	i4 = (sint16)(sint16)i4;	break;
		case kTypeU2:	i4 = (sint16)(uint16)i4;	break;
		case kTypeI4:	i4 = (sint16)(sint32)i4;	break;
		case kTypeU4:	i4 = (sint16)(uint32)i4;	break;
		case kTypeI8:	i4 = (sint16)(sint64)i8;	break;
		case kTypeU8:	i4 = (sint16)(uint64)i8;	break;
		case kTypeR4:	i4 = (sint16)(float )r8;	break;
		case kTypeR8:	i4 = (sint16)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeU2:
		switch(mType) {
		case kTypeU1:	i4 = (uint16)(uint8 )i4;	break;
		case kTypeI1:	i4 = (uint16)(sint8 )i4;	break;
		case kTypeI2:	i4 = (uint16)(sint16)i4;	break;
		case kTypeU2:	i4 = (uint16)(uint16)i4;	break;
		case kTypeI4:	i4 = (uint16)(sint32)i4;	break;
		case kTypeU4:	i4 = (uint16)(uint32)i4;	break;
		case kTypeI8:	i4 = (uint16)(sint64)i8;	break;
		case kTypeU8:	i4 = (uint16)(uint64)i8;	break;
		case kTypeR4:	i4 = (uint16)(float )r8;	break;
		case kTypeR8:	i4 = (uint16)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeI4:
		switch(mType) {
		case kTypeU1:	i4 = (sint32)(uint8 )i4;	break;
		case kTypeI1:	i4 = (sint32)(sint8 )i4;	break;
		case kTypeI2:	i4 = (sint32)(sint16)i4;	break;
		case kTypeU2:	i4 = (sint32)(uint16)i4;	break;
		case kTypeI4:	i4 = (sint32)(sint32)i4;	break;
		case kTypeU4:	i4 = (sint32)(uint32)i4;	break;
		case kTypeI8:	i4 = (sint32)(sint64)i8;	break;
		case kTypeU8:	i4 = (sint32)(uint64)i8;	break;
		case kTypeR4:	i4 = (sint32)(float )r8;	break;
		case kTypeR8:	i4 = (sint32)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeU4:
		switch(mType) {
		case kTypeU1:	i4 = (uint32)(uint8 )i4;	break;
		case kTypeI1:	i4 = (uint32)(sint8 )i4;	break;
		case kTypeI2:	i4 = (uint32)(sint16)i4;	break;
		case kTypeU2:	i4 = (uint32)(uint16)i4;	break;
		case kTypeI4:	i4 = (uint32)(sint32)i4;	break;
		case kTypeU4:	i4 = (uint32)(uint32)i4;	break;
		case kTypeI8:	i4 = (uint32)(sint64)i8;	break;
		case kTypeU8:	i4 = (uint32)(uint64)i8;	break;
		case kTypeR4:	i4 = (uint32)(float )r8;	break;
		case kTypeR8:	i4 = (uint32)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeI8:
		switch(mType) {
		case kTypeU1:	i8 = (sint64)(uint8 )i4;	break;
		case kTypeI1:	i8 = (sint64)(sint8 )i4;	break;
		case kTypeI2:	i8 = (sint64)(sint16)i4;	break;
		case kTypeU2:	i8 = (sint64)(uint16)i4;	break;
		case kTypeI4:	i8 = (sint64)(sint32)i4;	break;
		case kTypeU4:	i8 = (sint64)(uint32)i4;	break;
		case kTypeI8:	i8 = (sint64)(sint64)i8;	break;
		case kTypeU8:	i8 = (sint64)(uint64)i8;	break;
		case kTypeR4:	i8 = (sint64)(float )r8;	break;
		case kTypeR8:	i8 = (sint64)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeU8:
		switch(mType) {
		case kTypeU1:	i8 = (uint64)(uint8 )i4;	break;
		case kTypeI1:	i8 = (uint64)(sint8 )i4;	break;
		case kTypeI2:	i8 = (uint64)(sint16)i4;	break;
		case kTypeU2:	i8 = (uint64)(uint16)i4;	break;
		case kTypeI4:	i8 = (uint64)(sint32)i4;	break;
		case kTypeU4:	i8 = (uint64)(uint32)i4;	break;
		case kTypeI8:	i8 = (uint64)(sint64)i8;	break;
		case kTypeU8:	i8 = (uint64)(uint64)i8;	break;
		case kTypeR4:	i8 = (uint64)(float )r8;	break;
		case kTypeR8:	i8 = (uint64)(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeR4:
		switch(mType) {
		case kTypeU1:	r8 = (float )(uint8 )i4;	break;
		case kTypeI1:	r8 = (float )(sint8 )i4;	break;
		case kTypeI2:	r8 = (float )(sint16)i4;	break;
		case kTypeU2:	r8 = (float )(uint16)i4;	break;
		case kTypeI4:	r8 = (float )(sint32)i4;	break;
		case kTypeU4:	r8 = (float )(uint32)i4;	break;
		case kTypeI8:	r8 = (float )(sint64)i8;	break;
		case kTypeU8:	r8 = (float )(uint64)i8;	break;
		case kTypeR4:	r8 = (float )(float )r8;	break;
		case kTypeR8:	r8 = (float )(double)r8;	break;
		default:		return false;
		}
		break;

	case kTypeR8:
		switch(mType) {
		case kTypeU1:	r8 = (double)(uint8 )i4;	break;
		case kTypeI1:	r8 = (double)(sint8 )i4;	break;
		case kTypeI2:	r8 = (double)(sint16)i4;	break;
		case kTypeU2:	r8 = (double)(uint16)i4;	break;
		case kTypeI4:	r8 = (double)(sint32)i4;	break;
		case kTypeU4:	r8 = (double)(uint32)i4;	break;
		case kTypeI8:	r8 = (double)(sint64)i8;	break;
		case kTypeU8:	r8 = (double)(uint64)i8;	break;
		case kTypeR4:	r8 = (double)(float )r8;	break;
		case kTypeR8:	r8 = (double)(double)r8;	break;
		default:		return false;
		}
		break;

	default:
		return false;
	}

	mType = type;
	return true;
}

VDFileCreatorParser::VDFileCreatorParser() {
	mLexer.AddToken(kKeywordWrite_i1, "write_i1");
	mLexer.AddToken(kKeywordWrite_i2, "write_i2");
	mLexer.AddToken(kKeywordWrite_i4, "write_i4");
	mLexer.AddToken(kKeywordWrite_i8, "write_i8");
	mLexer.AddToken(kKeywordWrite_u1, "write_u1");
	mLexer.AddToken(kKeywordWrite_u2, "write_u2");
	mLexer.AddToken(kKeywordWrite_u4, "write_u4");
	mLexer.AddToken(kKeywordWrite_u8, "write_u8");
	mLexer.AddToken(kKeywordWrite_r4, "write_r4");
	mLexer.AddToken(kKeywordWrite_r8, "write_r8");
	mLexer.AddToken(kKeywordWrite_fcc, "write_fcc");
	mLexer.AddToken(kKeywordSet_index_anchor, "set_index_anchor");
	mLexer.AddToken(kKeywordIndexed_chunk, "indexed_chunk");
	mLexer.AddToken(kKeywordWrite_index, "write_index");
	mLexer.AddToken(kKeywordChunk, "chunk");
}

VDFileCreatorParser::~VDFileCreatorParser() {
}

bool VDFileCreatorParser::Parse(const char *src, size_t len, const char *context, IVDCompilerLogOutput *pOutput, IVDFileCreator **ppCreator) {
	mpCreator = new VDFileCreator;

	mLexer.Init(src, len, context, pOutput);

	while(TryParseGlobalStatement())
		Collect();

	mpCreator->Emit8(kInsnEnd);

	if (mLexer.IsErrorFlagSet())
		return false;

	*ppCreator = mpCreator.release();
	return true;
}

bool VDFileCreatorParser::GetErrorInfo(VDCompilerErrorInfo& errorInfo) {
	return mLexer.GetErrorInfo(errorInfo);
}

bool VDFileCreatorParser::TryParseGlobalStatement() {
	int tok = mLexer.Token();

	if (!tok)
		return false;

	switch(tok) {
		case '{':
			for(;;) {
				int tok = mLexer.Token();

				if (!tok)
					return mLexer.SetError("Unmatched '{'");

				if (tok == '}')
					break;

				mLexer.Push();
				if (!TryParseGlobalStatement())
					return false;
			}
			return true;

		case kKeywordWrite_i1:
			if (!EmitExpressionI1())
				return false;
			mpCreator->Emit8(kInsnWriteI1);
			break;

		case kKeywordWrite_i2:
			if (!EmitExpressionI2())
				return false;
			mpCreator->Emit8(kInsnWriteI2);
			break;

		case kKeywordWrite_i4:
			if (!EmitExpressionI4())
				return false;
			mpCreator->Emit8(kInsnWriteI4);
			break;

		case kKeywordWrite_i8:
			if (!EmitExpressionI8())
				return false;
			mpCreator->Emit8(kInsnWriteI8);
			break;

		case kKeywordWrite_u1:
			if (!EmitExpressionU1())
				return false;
			mpCreator->Emit8(kInsnWriteI1);
			break;

		case kKeywordWrite_u2:
			if (!EmitExpressionU2())
				return false;
			mpCreator->Emit8(kInsnWriteI2);
			break;

		case kKeywordWrite_u4:
			if (!EmitExpressionU4())
				return false;
			mpCreator->Emit8(kInsnWriteI4);
			break;

		case kKeywordWrite_u8:
			if (!EmitExpressionU8())
				return false;
			mpCreator->Emit8(kInsnWriteI8);
			break;

		case kKeywordWrite_r4:
			if (!EmitExpressionR4())
				return false;
			mpCreator->Emit8(kInsnWriteR4);
			break;

		case kKeywordWrite_r8:
			if (!EmitExpressionR8())
				return false;
			mpCreator->Emit8(kInsnWriteR8);
			break;

		case kKeywordWrite_fcc:
			if (!EmitExpressionFCC())
				return false;
			mpCreator->Emit8(kInsnWriteI4);
			break;

		case kKeywordSet_index_anchor:
			mpCreator->Emit8(kInsnSetIndexAnchor);
			break;

		case kKeywordWrite_index:
			mpCreator->Emit8(kInsnWriteIndex);
			break;

		case kKeywordChunk:
			if (!ParseChunk())
				return false;
			return true;

		case kKeywordIndexed_chunk:
			if (!ParseIndexedChunk())
				return false;
			return true;

		default:
			return mLexer.SetError("Syntax error");
	}

	tok = mLexer.Token();
	if (tok != ';')
		return mLexer.SetError("Expected end of statement");

	return true;
}

bool VDFileCreatorParser::ParseChunk() {
	if (!EmitExpressionFCC())
		return false;
	mpCreator->Emit8(kInsnWriteI4);
	mpCreator->Emit8(kInsnBeginChunk);
	if (!TryParseGlobalStatement())
		return false;
	mpCreator->Emit8(kInsnEndChunk);
	return true;
}

bool VDFileCreatorParser::ParseIndexedChunk() {
	if (!EmitExpressionFCC())
		return false;
	mpCreator->Emit8(kInsnDupI4);
	mpCreator->Emit8(kInsnWriteI4);

	int tok = mLexer.Token();
	if (tok != ',')
		return mLexer.SetError("Expected ','");

	if (!EmitExpressionU4())
		return false;

	mpCreator->Emit8(kInsnBeginChunk);
	if (!TryParseGlobalStatement())
		return false;
	mpCreator->Emit8(kInsnEndIndexedChunk);
	return true;
}

bool VDFileCreatorParser::ParseExpression(VDFileCreatorValue& value) {
	vdfastvector<uint32> opStack;
	vdfastvector<VDFileCreatorValue> valStack;
	bool expectValue = true;
	int parenLevel = 0;

	for(;;) {
		int tok = mLexer.Token();

		if (expectValue) {
			if (tok == '+') {		// unary +
				continue;
			} else if (tok == '-') {
				opStack.push_back(kOpNegate + (9<<8));
				continue;
			} else if (tok == '(') {
				parenLevel += 0x10000;
			} else if (tok == kTokenFloat) {
				VDFileCreatorValue val;
				val.mType = kTypeR8;
				val.r8 = mLexer.mFloatVal;

				valStack.push_back(val);
				expectValue = false;
			} else if (tok == kTokenInt) {
				VDFileCreatorValue val;
				val.mType = kTypeI4;
				val.i4 = mLexer.mIntVal;

				valStack.push_back(val);
				expectValue = false;
			} else if (tok == kTokenString) {
				int len = (int)mLexer.mString.size();
				char *s = new char[len + 1];
				mTempStrings.push_back(s);
				memcpy(s, mLexer.mString.data(), len);
				s[len] = 0;

				VDFileCreatorValue val;
				val.mType = kTypeS;
				val.s = s;

				valStack.push_back(val);
				expectValue = false;
			} else {
				return mLexer.SetError("Expected expression value");
			}
		} else {
			int op = 0;
			if (tok == '+')
				op = kOpAdd + (1 << 8) + parenLevel;
			else if (tok == '-')
				op = kOpSub + (1 << 8) + parenLevel;
			else if (tok == '*')
				op = kOpMul + (2 << 8) + parenLevel;
			else if (tok == '/')
				op = kOpDiv + (2 << 8) + parenLevel;
			else if (tok == ')') {
				if (parenLevel) {
					parenLevel -= 0x10000;
					continue;
				}
			}

			// reduce as necessary
			while(!opStack.empty()) {
				int otherop = opStack.back();

				if (op > otherop)
					break;

				opStack.pop_back();

				ExpressionOp baseop = (ExpressionOp)(otherop & 0xff);

				if (baseop == kOpNegate) {
					VDFileCreatorValue& v = valStack.back();

					switch(v.mType) {
						case kTypeU1:
						case kTypeI1:
						case kTypeU2:
						case kTypeI2:
						case kTypeU4:
						case kTypeI4:
							v.i4 = -v.i4;
							break;
						case kTypeU8:
						case kTypeI8:
							v.i8 = -v.i8;
							break;
						case kTypeR4:
						case kTypeR8:
							v.r8 = -v.r8;
							break;
					}
				} else {
					// coerce
					VDFileCreatorValue& var1 = *(valStack.end() - 2);
					VDFileCreatorValue& var2 = valStack.back();

					VDFileCreatorValueType commonType = GetBinaryResultType(var1.mType, var2.mType);
					if (!commonType)
						return mLexer.SetErrorF("Cannot operate between types '%s' and '%s'", GetTypeName(var1.mType), GetTypeName(var2.mType));

					if (!var1.ConvertToType(commonType))
						return mLexer.SetErrorF("Cannot convert value from '%s' to '%s'", GetTypeName(var1.mType), GetTypeName(commonType));

					if (!var2.ConvertToType(commonType))
						return mLexer.SetErrorF("Cannot convert value from '%s' to '%s'", GetTypeName(var2.mType), GetTypeName(commonType));

					switch(baseop) {
						case kOpAdd:
							switch(commonType) {
								case kTypeI4:
								case kTypeU4:
									var1.i4 += var2.i4;
									break;
								case kTypeI8:
								case kTypeU8:
									var1.i8 += var2.i8;
									break;
								case kTypeR8:
									var1.r8 += var2.r8;
									break;
								default:
									return mLexer.SetErrorF("Cannot add values of type '%s'", GetTypeName(commonType));
							}
							break;
						case kOpSub:
							switch(commonType) {
								case kTypeI4:
								case kTypeU4:
									var1.i4 -= var2.i4;
									break;
								case kTypeI8:
								case kTypeU8:
									var1.i8 -= var2.i8;
									break;
								case kTypeR8:
									var1.r8 -= var2.r8;
									break;
								default:
									return mLexer.SetErrorF("Cannot subtract values of type '%s'", GetTypeName(commonType));
							}
							break;
						case kOpMul:
							switch(commonType) {
								case kTypeI4:
									var1.i4 *= var2.i4;
									break;
								case kTypeU4:
									var1.i4 = (uint32)var1.i4 * (uint32)var2.i4;
									break;
								case kTypeI8:
									var1.i8 *= var2.i8;
									break;
								case kTypeU8:
									var1.i8 = (uint64)var1.i8 * (uint64)var2.i8;
									break;
								case kTypeR8:
									var1.r8 *= var2.r8;
									break;
								default:
									return mLexer.SetErrorF("Cannot multiply values of type '%s'", GetTypeName(commonType));
							}
							break;
						case kOpDiv:
							switch(commonType) {
								case kTypeI4:
									if (!var2.i4)
										return mLexer.SetError("Divide by zero");
									var1.i4 /= var2.i4;
									break;
								case kTypeU4:
									if (!var2.i4)
										return mLexer.SetError("Divide by zero");
									var1.i4 = (sint32)((uint32)var1.i4 / (uint32)var2.i4);
									break;
								case kTypeI8:
									if (!var2.i8)
										return mLexer.SetError("Divide by zero");
									var1.i8 /= var2.i8;
									break;
								case kTypeU8:
									if (!var2.i8)
										return mLexer.SetError("Divide by zero");
									var1.i8 = (sint64)((uint64)var1.i8 / (uint64)var2.i8);
									break;
								case kTypeR8:
									if (var2.r8 == 0.0)
										return mLexer.SetError("Divide by zero");
									var1.r8 /= var2.r8;
									break;
								default:
									return mLexer.SetErrorF("Cannot divide values of type '%s'", GetTypeName(commonType));
							}
							break;
					}

					var1.mType = commonType;
					valStack.pop_back();
				}
			}

			expectValue = true;

			if (!op)
				break;

			opStack.push_back(op);
		}
	}

	if (parenLevel)
		return mLexer.SetError("Unmatched '(' in expression");

	mLexer.Push();

	VDASSERT(valStack.size() == 1);
	value = valStack.back();
	return true;
}

bool VDFileCreatorParser::EmitExpressionI1() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeI1))
		return mLexer.SetError("Cannot convert value to type 'int8'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionI2() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeI2))
		return mLexer.SetError("Cannot convert value to type 'int16'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionI4() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeI4))
		return mLexer.SetError("Cannot convert value to type 'int32'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionI8() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeI8))
		return mLexer.SetError("Cannot convert value to type 'int64'");
	mpCreator->Emit8(kInsnLoadConstI8);
	mpCreator->Emit64(value.i8);
	return true;
}

bool VDFileCreatorParser::EmitExpressionU1() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeU1))
		return mLexer.SetError("Cannot convert value to type 'uint8'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionU2() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeU2))
		return mLexer.SetError("Cannot convert value to type 'uint16'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionU4() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeU4))
		return mLexer.SetError("Cannot convert value to type 'uint32'");
	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

bool VDFileCreatorParser::EmitExpressionU8() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeU8))
		return mLexer.SetError("Cannot convert value to type 'uint64'");
	mpCreator->Emit8(kInsnLoadConstI8);
	mpCreator->Emit64(value.i8);
	return true;
}

bool VDFileCreatorParser::EmitExpressionR4() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeR4))
		return mLexer.SetError("Cannot convert value to type 'float'");
	mpCreator->Emit8(kInsnLoadConstR8);
	mpCreator->EmitR8(value.r8);
	return true;
}

bool VDFileCreatorParser::EmitExpressionR8() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;
	if (!value.ConvertToType(kTypeR8))
		return mLexer.SetError("Cannot convert value to type 'double'");
	mpCreator->Emit8(kInsnLoadConstR8);
	mpCreator->EmitR8(value.r8);
	return true;
}

bool VDFileCreatorParser::EmitExpressionFCC() {
	VDFileCreatorValue value;
	if (!ParseExpression(value))
		return false;

	if (value.mType == kTypeS) {
		const char *s = value.s;
		int len = (int)strlen(s);

		if (len > 4)
			return mLexer.SetError("String too long for FOURCC");

		char buf[4]={' ', ' ', ' ', ' '};
		memcpy(buf, s, len);

		value.i4 = *(uint32 *)buf;
	} else if (!value.ConvertToType(kTypeU4))
		return mLexer.SetError("Cannot convert value to FOURCC");

	mpCreator->Emit8(kInsnLoadConstI4);
	mpCreator->Emit32(value.i4);
	return true;
}

void VDFileCreatorParser::Collect() {
	while(!mTempStrings.empty()) {
		const char *s = mTempStrings.back();
		mTempStrings.pop_back();
		delete[] s;
	}
}

///////////////////////////////////////////////////////////////////////////////

class VDCompilerLogOutputStdout : public IVDCompilerLogOutput {
public:
	void WriteLogOutput(const char *s) {
		fputs(s, stdout);
	}
};

void tool_filecreate(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() != 2) {
		puts("usage: asuka filecreate source.filescript target.bin");
		exit(5);
	}
	const char *fnin = args[0];
	const char *fnout = args[1];

	VDFile file(fnin);
	vdfastvector<char> buf((uint32)file.size());
	file.read(buf.data(), buf.size());
	file.close();

	VDFileCreatorParser parser;
	VDCompilerLogOutputStdout out;

	vdrefptr<IVDFileCreator> creator;
	if (!parser.Parse(buf.data(), buf.size(), fnin, &out, ~creator))
		exit(5);

	creator->Create(VDTextAToW(fnout).c_str());
}
