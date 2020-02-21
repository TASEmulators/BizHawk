//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2018 Avery Lee
//	Debugger module - command argument parsing support
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_ATDEBUGGER_ARGPARSE_H
#define f_AT_ATDEBUGGER_ARGPARSE_H

#include <vd2/system/vdalloc.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/VDString.h>
#include <at/atdebugger/expression.h>

class ATDebugExpNode;

class ATDebuggerCmdSwitch {
public:
	ATDebuggerCmdSwitch(const char *name, bool defaultState)
		: mpName(name)
		, mbState(defaultState)
	{
	}

	operator bool() const { return mbState; }

protected:
	friend class ATDebuggerCmdParser;

	const char *mpName;
	bool mbState;
};

class ATDebuggerCmdSwitchStrArg {
public:
	ATDebuggerCmdSwitchStrArg(const char *name)
		: mpName(name)
		, mbValid(false)
	{
	}

	bool IsValid() const { return mbValid; }
	const char *GetValue() const { return mValue.c_str(); }

protected:
	friend class ATDebuggerCmdParser;

	const char *mpName;
	bool mbValid;
	VDStringA mValue;
};

class ATDebuggerCmdSwitchNumArg {
public:
	ATDebuggerCmdSwitchNumArg(const char *name, sint32 minVal, sint32 maxVal, sint32 defaultValue = 0)
		: mpName(name)
		, mValue(defaultValue)
		, mMinVal(minVal)
		, mMaxVal(maxVal)
		, mbValid(false)
	{
	}

	bool IsValid() const { return mbValid; }
	sint32 GetValue() const { return mValue; }

protected:
	friend class ATDebuggerCmdParser;

	const char *mpName;
	sint32 mValue;
	sint32 mMinVal;
	sint32 mMaxVal;
	bool mbValid;
};

class ATDebuggerCmdSwitchExprNumArg {
public:
	ATDebuggerCmdSwitchExprNumArg(const char *name, sint32 minVal, sint32 maxVal, sint32 defaultValue = 0)
		: mpName(name)
		, mValue(defaultValue)
		, mMinVal(minVal)
		, mMaxVal(maxVal)
		, mbValid(false)
	{
	}

	bool IsValid() const { return mbValid; }
	sint32 GetValue() const { return mValue; }

protected:
	friend class ATDebuggerCmdParser;

	const char *mpName;
	sint32 mValue;
	sint32 mMinVal;
	sint32 mMaxVal;
	bool mbValid;
};

class ATDebuggerCmdBool{
public:
	ATDebuggerCmdBool(bool required, bool defaultValue = false)
		: mbRequired(required)
		, mbValid(false)
		, mbValue(defaultValue)
	{
	}

	bool IsValid() const { return mbValid; }
	operator bool() const { return mbValue; }

protected:
	friend class ATDebuggerCmdParser;

	bool mbRequired;
	bool mbValid;
	bool mbValue;
};

class ATDebuggerCmdNumber{
public:
	ATDebuggerCmdNumber(bool required, sint32 minVal, sint32 maxVal, sint32 defaultValue = 0)
		: mbRequired(required)
		, mbValid(false)
		, mValue(defaultValue)
		, mMinVal(minVal)
		, mMaxVal(maxVal)
	{
	}

	bool IsValid() const { return mbValid; }
	sint32 GetValue() const { return mValue; }

protected:
	friend class ATDebuggerCmdParser;

	bool mbRequired;
	bool mbValid;
	sint32 mValue;
	sint32 mMinVal;
	sint32 mMaxVal;
};

class ATDebuggerCmdAddress {
public:
	ATDebuggerCmdAddress(bool general, bool required, bool allowStar = false)
		: mAddress(0)
		, mbGeneral(general)
		, mbRequired(required)
		, mbAllowStar(allowStar)
		, mbStar(false)
		, mbValid(false)
	{
	}

	bool IsValid() const { return mbValid; }
	bool IsStar() const { return mbStar; }

	operator uint32() const { return mAddress; }

protected:
	friend class ATDebuggerCmdParser;

	uint32 mAddress;
	bool mbGeneral;
	bool mbRequired;
	bool mbAllowStar;
	bool mbStar;
	bool mbValid;
};

class ATDebuggerCmdExprAddr;

class ATDebuggerCmdLength {
public:
	ATDebuggerCmdLength(uint32 defaultLen, bool required, ATDebuggerCmdExprAddr *anchor)
		: mLength(defaultLen)
		, mbRequired(required)
		, mbValid(false)
		, mpAnchor(anchor)
	{
	}

	bool IsValid() const { return mbValid; }

	operator uint32() const { return mLength; }

protected:
	friend class ATDebuggerCmdParser;

	uint32 mLength;
	bool mbRequired;
	bool mbValid;
	ATDebuggerCmdExprAddr *mpAnchor;
};

class ATDebuggerCmdName {
public:
	ATDebuggerCmdName(bool required)
		: mbValid(false)
		, mbRequired(required)
	{
	}

	bool IsValid() const { return mbValid; }

	const VDStringA& operator*() const { return mName; }
	const VDStringA *operator->() const { return &mName; }

protected:
	friend class ATDebuggerCmdParser;

	VDStringA mName;
	bool mbRequired;
	bool mbValid;
};

class ATDebuggerCmdPath {
public:
	ATDebuggerCmdPath(bool required)
		: mbValid(false)
		, mbRequired(required)
	{
	}

	bool IsValid() const { return mbValid; }

	const VDStringA *operator->() const { return &mPath; }

protected:
	friend class ATDebuggerCmdParser;

	VDStringA mPath;
	bool mbRequired;
	bool mbValid;
};

class ATDebuggerCmdString {
public:
	ATDebuggerCmdString(bool required)
		: mbValid(false)
		, mbRequired(required)
	{
	}

	bool IsValid() const { return mbValid; }

	const VDStringA& operator*() const { return mName; }
	const VDStringA *operator->() const { return &mName; }

protected:
	friend class ATDebuggerCmdParser;

	VDStringA mName;
	bool mbRequired;
	bool mbValid;
};

class ATDebuggerCmdQuotedString {
public:
	ATDebuggerCmdQuotedString(bool required)
		: mbValid(false)
		, mbRequired(required)
	{
	}

	bool IsValid() const { return mbValid; }

	const VDStringA& GetValue() const { return mName; }
	const VDStringA *operator->() const { return &mName; }
		
protected:
	friend class ATDebuggerCmdParser;

	VDStringA mName;
	bool mbRequired;
	bool mbValid;
};

class ATDebuggerCmdExpr {
public:
	ATDebuggerCmdExpr(bool required)
		: mbRequired(required)
		, mpExpr(NULL)
	{
	}

	ATDebugExpNode *DetachValue() { return mpExpr.release(); }
	ATDebugExpNode *GetValue() const { return mpExpr; }

protected:
	friend class ATDebuggerCmdParser;

	bool mbRequired;
	vdautoptr<ATDebugExpNode> mpExpr;
};

class ATDebuggerCmdExprNum {
public:
	ATDebuggerCmdExprNum(bool required, bool hex = true, sint32 minVal = -0x7FFFFFFF-1, sint32 maxVal = 0x7FFFFFFF, sint32 defaultValue = 0, bool allowStar = false)
		: mbRequired(required)
		, mbValid(false)
		, mbAllowStar(allowStar)
		, mbStar(false)
		, mbHexDefault(hex)
		, mValue(defaultValue)
		, mMinVal(minVal)
		, mMaxVal(maxVal)
	{
	}

	bool IsValid() const { return mbValid; }
	bool IsStar() const { return mbStar; }
	sint32 GetValue() const { return mValue; }
	const char *GetOriginalText() const { return mOriginalText.c_str(); }

protected:
	friend class ATDebuggerCmdParser;

	bool mbRequired;
	bool mbValid;
	bool mbStar;
	bool mbAllowStar;
	bool mbHexDefault;
	sint32 mValue;
	sint32 mMinVal;
	sint32 mMaxVal;
	VDStringA mOriginalText;
};

class ATDebuggerCmdExprAddr {
public:
	ATDebuggerCmdExprAddr(bool general, bool required, bool allowStar = false, uint32 defaultValue = 0)
		: mbRequired(required)
		, mbAllowStar(allowStar)
		, mbValid(false)
		, mbStar(false)
		, mValue(defaultValue)
	{
	}

	bool IsValid() const { return mbValid; }
	bool IsStar() const { return mbStar; }
	uint32 GetValue() const { return mValue; }

protected:
	friend class ATDebuggerCmdParser;

	bool mbRequired;
	bool mbAllowStar;
	bool mbValid;
	bool mbStar;
	uint32 mValue;
};

class ATDebuggerCmdParser {
public:
	ATDebuggerCmdParser(int argc, const char *const *argv);

	bool IsEmpty() const { return mArgs.empty(); }

	const char *const *GetArguments() const { return mArgs.data(); }
	size_t GetArgumentCount() const { return mArgs.size(); }

	const char *GetNextArgument();

	ATDebuggerCmdParser& operator>>(ATDebuggerCmdSwitch& sw);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdSwitchStrArg& sw);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdSwitchNumArg& sw);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdSwitchExprNumArg& sw);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdBool& bo);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdNumber& nu);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdAddress& ad);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdLength& ln);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdName& nm);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdPath& nm);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdString& nm);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdQuotedString& nm);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdExpr& nu);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdExprNum& nu);
	ATDebuggerCmdParser& operator>>(ATDebuggerCmdExprAddr& nu);
	ATDebuggerCmdParser& operator>>(int);

	template<class T>
	ATDebuggerCmdParser& operator,(T& dst) {
		operator>>(dst);
		return *this;
	}

protected:
	typedef vdfastvector<const char *> Args;

	Args mArgs;
};

#endif
