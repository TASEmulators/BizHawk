//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
//	Debugger module - target access interface
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

#ifndef f_AT_ATDEBUGGER_TARGET_H
#define f_AT_ATDEBUGGER_TARGET_H

#include <vd2/system/function.h>
#include <vd2/system/unknown.h>

struct ATCPUExecState;
struct ATCPUHistoryEntry;
class IATCPUBreakpointHandler;

enum ATDebugDisasmMode : uint8 {
	kATDebugDisasmMode_6502,
	kATDebugDisasmMode_65C02,
	kATDebugDisasmMode_65C816,
	kATDebugDisasmMode_Z80,
	kATDebugDisasmMode_8048,
	kATDebugDisasmMode_6809
};

class IATDebugTarget : public IVDUnknown {
public:
	virtual const char *GetName() = 0;
	virtual ATDebugDisasmMode GetDisasmMode() = 0;

	virtual void GetExecState(ATCPUExecState& state) = 0;
	virtual void SetExecState(const ATCPUExecState& state) = 0;

	virtual sint32 GetTimeSkew() = 0;

	virtual uint8 ReadByte(uint32 address) = 0;
	virtual void ReadMemory(uint32 address, void *dst, uint32 n) = 0;

	virtual uint8 DebugReadByte(uint32 address) = 0;
	virtual void DebugReadMemory(uint32 address, void *dst, uint32 n) = 0;

	virtual void WriteByte(uint32 address, uint8 value) = 0;
	virtual void WriteMemory(uint32 address, const void *src, uint32 n) = 0;
};

class IATDeviceDebugTarget {
public:
	enum { kTypeID = 'addt' };

	virtual IATDebugTarget *GetDebugTarget(uint32 index) = 0;
};

class IATDebugTargetBreakpoints {
public:
	enum { kTypeID = 'adtb' };

	virtual void SetBreakpointHandler(IATCPUBreakpointHandler *handler) = 0;

	virtual void ClearBreakpoint(uint16 pc) = 0;
	virtual void SetBreakpoint(uint16 pc) = 0;

	virtual void ClearAllBreakpoints() = 0;

	// Set a breakpoint on all addresses. This is used to force a breakpoint test
	// at every instruction.
	virtual void SetAllBreakpoints() = 0;
};

class IATDebugTargetHistory {
public:
	enum { kTypeID = 'adth' };

	virtual bool GetHistoryEnabled() const = 0;
	virtual void SetHistoryEnabled(bool enable) = 0;

	virtual std::pair<uint32, uint32> GetHistoryRange() const = 0;
	virtual uint32 ExtractHistory(const ATCPUHistoryEntry **hparray, uint32 start, uint32 n) const = 0;
	virtual uint32 ConvertRawTimestamp(uint32 rawTimestamp) const = 0;
	virtual double GetTimestampFrequency() const = 0;
};

class IATDebugTargetExecutionControl {
public:
	enum { kTypeID = 'adtx' };

	virtual void Break() = 0;
	virtual bool StepInto(const vdfunction<void(bool)>& fn) = 0;
	virtual bool StepOver(const vdfunction<void(bool)>& fn) = 0;
	virtual bool StepOut(const vdfunction<void(bool)>& fn) = 0;
	virtual void StepUpdate() = 0;
	virtual void RunUntilSynced() = 0;
};

#endif
