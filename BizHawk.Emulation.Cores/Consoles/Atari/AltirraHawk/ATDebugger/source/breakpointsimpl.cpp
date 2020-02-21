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

#include <stdafx.h>
#include <at/atdebugger/breakpointsimpl.h>

ATDebugTargetBreakpointsBase::ATDebugTargetBreakpointsBase(uint32 addressLimit, bool *breakpointMap, bool *stepBreakpointMap)
	: mAddressLimit(addressLimit)
	, mpBreakpointMap(breakpointMap)
	, mpStepBreakpointMap(stepBreakpointMap)
{
	std::fill(mpBreakpointMap, mpBreakpointMap + mAddressLimit, false);
	std::fill(mpStepBreakpointMap, mpStepBreakpointMap + mAddressLimit, true);
}

void ATDebugTargetBreakpointsBase::SetStepActive(bool active) {
	if (mbStepActive == active)
		return;

	mbStepActive = active;

	if (active)
		mpSetBPTable(mpStepBreakpointMap, mpStepHandler);
	else if (mBreakpointCount)
		mpSetBPTable(mpBreakpointMap, mpBreakpointHandler);
	else
		mpSetBPTable(nullptr, nullptr);
}

void ATDebugTargetBreakpointsBase::SetBPHandler(vdfunction<void(const bool *, IATCPUBreakpointHandler *)> fn) {
	mpSetBPTable = std::move(fn);
}

void ATDebugTargetBreakpointsBase::SetBreakpointHandler(IATCPUBreakpointHandler *handler) {
	mpBreakpointHandler = handler;
}

void ATDebugTargetBreakpointsBase::ClearBreakpoint(uint16 pc) {
	if (pc >= mAddressLimit)
		return;

	if (mpBreakpointMap[pc]) {
		mpBreakpointMap[pc] = false;

		if (!--mBreakpointCount && !mbStepActive)
			mpSetBPTable(nullptr, nullptr);
	}
}

void ATDebugTargetBreakpointsBase::SetBreakpoint(uint16 pc) {
	if (pc >= mAddressLimit)
		return;

	if (!mpBreakpointMap[pc]) {
		mpBreakpointMap[pc] = true;

		if (!mBreakpointCount++ && !mbStepActive)
			mpSetBPTable(mpBreakpointMap, mpBreakpointHandler);
	}
}

void ATDebugTargetBreakpointsBase::ClearAllBreakpoints() {
	if (mBreakpointCount) {
		mBreakpointCount = 0;

		std::fill(mpBreakpointMap, mpBreakpointMap + mAddressLimit, false);

		if (!mbStepActive)
			mpSetBPTable(nullptr, nullptr);
	}
}

void ATDebugTargetBreakpointsBase::SetAllBreakpoints() {
	if (mBreakpointCount < mAddressLimit) {
		mBreakpointCount = mAddressLimit;
		std::fill(mpBreakpointMap, mpBreakpointMap + mAddressLimit, true);

		if (!mbStepActive)
			mpSetBPTable(mpBreakpointMap, mpBreakpointHandler);
	}
}
