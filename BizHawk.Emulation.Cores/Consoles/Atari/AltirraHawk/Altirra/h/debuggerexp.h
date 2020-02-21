//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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

#ifndef f_AT_DEBUGGEREXP_H
#define f_AT_DEBUGGEREXP_H

#include <stdarg.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/error.h>
#include <at/atdebugger/expression.h>
#include <at/atdebugger/target.h>
#include <at/atcpu/execstate.h>

class VDStringA;
class ATCPUEmulator;
class ATCPUEmulatorMemory;
class ATAnticEmulator;
class ATMMUEmulator;
class IATDebuggerSymbolLookup;

struct ATDebugExpEvalContext {
	IATDebugTarget *mpTarget;
	ATAnticEmulator *mpAntic;
	ATMMUEmulator *mpMMU;
	const sint32 *mpTemporaries;

	uint32 (*mpClockFn)(void *p);
	void *mpClockFnData;

	uint32 (*mpCpuClockFn)(void *p);
	void *mpCpuClockFnData;

	uint32 (*mpXPCFn)(void *p);
	void *mpXPCFnData;

	bool mbAccessValid;
	bool mbAccessReadValid;
	bool mbAccessWriteValid;
	sint32 mAccessAddress;
	uint8 mAccessValue;
};

struct ATDebugExpEvalCache {
	bool mbExecStateValid = false;
	ATDebugDisasmMode mExecMode;
	ATCPUExecState mExecState;

	const ATCPUExecState *GetExecState(const ATDebugExpEvalContext& ctx);
};

struct ATDebuggerExprParseOpts {
	bool mbDefaultHex;
	bool mbAllowUntaggedHex;
};

ATDebugExpNode *ATDebuggerParseExpression(const char *s, IATDebuggerSymbolLookup *dbg, const ATDebuggerExprParseOpts& opts, ATDebugExpEvalContext *immContext = nullptr);
ATDebugExpNode *ATDebuggerInvertExpression(ATDebugExpNode *node);

class ATDebuggerExprParseException : public MyError {
public:
	template<class... Args>
	ATDebuggerExprParseException(Args&&... args)
		: MyError(std::forward<Args>(args)...) {}
};

#endif
