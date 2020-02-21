//	Altirra - Atari 800/800XL/5200 emulator
//	Coprocessor library - target breakpoint impl
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_AT_ATDEBUGGER_BREAKPOINTSIMPL_H
#define f_AT_ATDEBUGGER_BREAKPOINTSIMPL_H

#include <at/atcpu/breakpoints.h>
#include <at/atdebugger/target.h>

class ATDebugTargetBreakpointsBase : public IATDebugTargetBreakpoints {
public:
	ATDebugTargetBreakpointsBase(uint32 addressLimit, bool *breakpointMap, bool *stepBreakpointMap);

	void SetStepActive(bool active);
	void SetStepHandler(IATCPUBreakpointHandler *handler) { mpStepHandler = handler; }

	template<class T>
	void BindBPHandler(T& dst) {
		SetBPHandler(
			[&dst](const bool *bpMap, IATCPUBreakpointHandler *bpHandler) {
				dst.SetBreakpointMap(bpMap, bpHandler);
			}
		);
	}

	bool CheckBP(uint32 pc) const {
		return mBreakpointCount && mpBreakpointMap[pc & (mAddressLimit - 1)] && mpBreakpointHandler->CheckBreakpoint(pc);
	}

	void SetBPHandler(vdfunction<void(const bool *, IATCPUBreakpointHandler *)> fn);

public:
	void SetBreakpointHandler(IATCPUBreakpointHandler *handler) override final;

	void ClearBreakpoint(uint16 pc) override final;
	void SetBreakpoint(uint16 pc) override final;

	void ClearAllBreakpoints() override final;
	void SetAllBreakpoints() override final;

private:
	vdfunction<void(const bool *, IATCPUBreakpointHandler *)> mpSetBPTable;
	const uint32 mAddressLimit;
	uint32 mBreakpointCount = 0;
	bool mbStepActive = false;
	bool *const mpBreakpointMap;
	bool *const mpStepBreakpointMap;

	IATCPUBreakpointHandler *mpBreakpointHandler = nullptr;
	IATCPUBreakpointHandler *mpStepHandler = nullptr;

};

template<uint32 T_AddressLimit>
class ATDebugTargetBreakpointsImplT final : public ATDebugTargetBreakpointsBase {
public:
	ATDebugTargetBreakpointsImplT()
		: ATDebugTargetBreakpointsBase(T_AddressLimit, mBreakpointMap, mStepBreakpointMap)
	{
	}

private:
	bool mBreakpointMap[T_AddressLimit] = {};
	bool mStepBreakpointMap[T_AddressLimit];
};

typedef ATDebugTargetBreakpointsImplT<0x10000> ATDebugTargetBreakpointsImpl;

#endif
