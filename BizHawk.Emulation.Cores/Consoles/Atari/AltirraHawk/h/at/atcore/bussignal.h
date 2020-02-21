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

#ifndef f_AT_ATCORE_BUSSIGNAL_H
#define f_AT_ATCORE_BUSSIGNAL_H

#include <vd2/system/function.h>
#include <vd2/system/vdstl.h>

class ATBusSignalOutput;

// ATBusSignal
//
// Signal aggregated from multiple outputs, with notification when an output
// changes. Bus signals are optimized for low transition costs -- writes and
// reads are both localized when possible and aggregation is done incrementally
// rather than by re-polling all outputs. Only connection and disconnection
// are O(N).
//
// Signals and signal outputs automatically disconnect on destruction.
//
// Bus signal objects are not thread-safe. All connections and updates must
// be externally synchronized.
//
class ATBusSignal {
	ATBusSignal(const ATBusSignal&) = delete;
	ATBusSignal& operator=(const ATBusSignal&) = delete;
public:
	ATBusSignal() = default;
	~ATBusSignal();

	void Shutdown();

	// Return result of all outputs combined with AND or OR, returning a
	// specific value if there are no outputs.
	bool AndDefaultFalse() const { return mLowCount == 0 && mHighCount > 0; }
	bool AndDefaultTrue() const { return mLowCount == 0; }
	bool OrDefaultFalse() const { return mHighCount > 0; }
	bool OrDefaultTrue() const { return mHighCount > 0 || mLowCount == 0; }

	void SetOnUpdated(vdfunction<void()> fn);

protected:
	friend class ATBusSignalOutput;

	void Adjust(sint32 deltaHigh, sint32 deltaLow);
	void AddOutput(ATBusSignalOutput& output);
	void RemoveOutput(ATBusSignalOutput& output);

	uint32 mHighCount = 0;
	uint32 mLowCount = 0;

	vdfastvector<ATBusSignalOutput *> mOutputs;
	vdfunction<void()> mpUpdateFn;
};

// ATBusSignalOutput
//
// Output feeding into a bus signal. State changes are kept locally,
// only propagating them into the bus on a change and maintaining local
// state even when disconnected. Local state is pushed into the bus on
// connection and preserved on disconnection so that order dependencies
// are avoided.
//
// Signal outputs automatically disconnected on destruction.
//
class ATBusSignalOutput {
	ATBusSignalOutput(const ATBusSignalOutput&) = delete;
	ATBusSignalOutput& operator=(const ATBusSignalOutput&) = delete;

public:
	ATBusSignalOutput() = default;
	~ATBusSignalOutput();

	bool GetValue() const { return (mSignalAndLocalValue & 1) != 0; }
	void SetValue(bool value);

	void Connect(ATBusSignal& signal);
	void Disconnect();

private:
	friend class ATBusSignal;

	uintptr mSignalAndLocalValue = 0;
};

#endif
