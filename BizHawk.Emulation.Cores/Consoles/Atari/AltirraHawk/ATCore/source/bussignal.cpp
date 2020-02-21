//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - generic bus signal implementation
//	Copyright (C) 2009-2019 Avery Lee
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

#include <stdafx.h>
#include <at/atcore/bussignal.h>

ATBusSignal::~ATBusSignal() {
	Shutdown();
}

void ATBusSignal::Shutdown() {
	for(ATBusSignalOutput *output : mOutputs) {
		output->mSignalAndLocalValue &= 1;
	}

	mOutputs.clear();

	mpUpdateFn = nullptr;
}

void ATBusSignal::SetOnUpdated(vdfunction<void()> fn) {
	mpUpdateFn = std::move(fn);
}

void ATBusSignal::Adjust(sint32 deltaHigh, sint32 deltaLow) {
	mHighCount += (uint32)deltaHigh;
	mLowCount += (uint32)deltaLow;

	if (mpUpdateFn)
		mpUpdateFn();
}

void ATBusSignal::AddOutput(ATBusSignalOutput& output) {
	mOutputs.push_back(&output);
}

void ATBusSignal::RemoveOutput(ATBusSignalOutput& output) {
	auto it = std::find(mOutputs.begin(), mOutputs.end(), &output);

	if (it == mOutputs.end()) {
		VDFAIL("RemoveOutput() could not find output.");
	} else {
		*it = mOutputs.back();
		mOutputs.pop_back();
	}
}

ATBusSignalOutput::~ATBusSignalOutput() {
	Disconnect();
}

void ATBusSignalOutput::SetValue(bool v) {
	uintptr bit = v ? 1 : 0;

	if ((mSignalAndLocalValue & 1) != bit) {
		mSignalAndLocalValue ^= 1;

		if (mSignalAndLocalValue >= 2) {
			ATBusSignal& signal = *(ATBusSignal *)(mSignalAndLocalValue & ~(uintptr)1);

			signal.Adjust(bit ? 1 : -1, bit ? -1 : 1);
		}
	}
}

void ATBusSignalOutput::Connect(ATBusSignal& signal) {
	Disconnect();

	signal.AddOutput(*this);

	const sint32 currentBit = (sint32)(mSignalAndLocalValue & 1);
	signal.Adjust(currentBit, currentBit ^ 1);

	mSignalAndLocalValue += (uintptr)&signal;
}

void ATBusSignalOutput::Disconnect() {
	if (mSignalAndLocalValue >= 2) {
		ATBusSignal& signal = *(ATBusSignal *)(mSignalAndLocalValue & ~(uintptr)1);

		const sint32 currentBit = -(sint32)(mSignalAndLocalValue & 1);
		signal.Adjust(currentBit, ~currentBit);
		signal.RemoveOutput(*this);
		mSignalAndLocalValue &= 1;
	}
}


