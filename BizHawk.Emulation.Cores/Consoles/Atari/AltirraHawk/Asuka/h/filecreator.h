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

#ifndef MAKEFILE_H
#define MAKEFILE_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/refcount.h>
#include <map>

class IVDCompilerLogOutput {
public:
	virtual void WriteLogOutput(const char *s) = 0;
};

void VDCompilerWriteLogOutputF(IVDCompilerLogOutput& out, const char *format ...);

namespace nsVDCompilerTokens {
	enum VDCompilerToken {
		kTokenInt		= 0x100,
		kTokenFloat,
		kTokenString,
		kTokenIdent,
		kTokenKeywordBase	= 0x0200
	};
}

struct VDCompilerErrorInfo {
	int mLine;
	int mColumn;
	int mLength;
};

class VDCompilerLexer {
public:
	void Init(const char *src, size_t len, const char *context, IVDCompilerLogOutput *pOutput);
	bool GetErrorInfo(VDCompilerErrorInfo& errorInfo);

	IVDCompilerLogOutput *GetLogOutput() const { return mpOutput; }
	bool IsErrorFlagSet() const { return mbError; }

	void AddToken(int token, const char *s);

	int Token();
	void Push();

	bool SetError(const char *s);
	bool SetErrorF(const char *format, ...);

public:
	const char *mpToken;
	const char *mpTokenLineStart;
	int mTokenLength;
	union {
		int		mIntVal;
		float	mFloatVal;
	};
	vdfastvector<char> mString;

protected:
	void SkipComment();

	const char *mpSrc;
	const char *mpSrcEnd;
	const char *mpSrcLineStart;
	const char *mpContext;
	int mLineNo;

	bool mbError;
	VDCompilerErrorInfo mErrorInfo;

	IVDCompilerLogOutput *mpOutput;

	struct Keyword {
		int token;
		int len;
		const char *text;
	};

	typedef std::multimap<uint32, Keyword> Keywords;
	Keywords mKeywords;
};

class IVDFileCreator : public IVDRefCount {
public:
	virtual void Create(const wchar_t *filename) = 0;
};

class VDFileCreator;
struct VDFileCreatorValue;

class VDFileCreatorParser {
public:
	VDFileCreatorParser();
	~VDFileCreatorParser();

	bool Parse(const char *src, size_t len, const char *context, IVDCompilerLogOutput *pOutput, IVDFileCreator **ppCreator);

	bool GetErrorInfo(VDCompilerErrorInfo& errorInfo);

protected:
	bool TryParseGlobalStatement();
	bool ParseChunk();
	bool ParseIndexedChunk();
	bool ParseExpression(VDFileCreatorValue& value);
	bool EmitExpressionI1();
	bool EmitExpressionI2();
	bool EmitExpressionI4();
	bool EmitExpressionI8();
	bool EmitExpressionU1();
	bool EmitExpressionU2();
	bool EmitExpressionU4();
	bool EmitExpressionU8();
	bool EmitExpressionR4();
	bool EmitExpressionR8();
	bool EmitExpressionFCC();

	void Collect();

	VDCompilerLexer	mLexer;

	vdrefptr<VDFileCreator> mpCreator;

	typedef vdfastvector<const char *> TempStrings;
	TempStrings mTempStrings;
};

#endif
