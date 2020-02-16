//
//   Copyright (C) 2007 by sinamas <sinamas at users.sourceforge.net>
//
//   This program is free software; you can redistribute it and/or modify
//   it under the terms of the GNU General Public License version 2 as
//   published by the Free Software Foundation.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License version 2 for more details.
//
//   You should have received a copy of the GNU General Public License
//   version 2 along with this program; if not, write to the
//   Free Software Foundation, Inc.,
//   51 Franklin St, Fifth Floor, Boston, MA  02110-1301, USA.
//

#include "channel2.h"
#include "psgdef.h"
#include "../savestate.h"
#include <algorithm>

using namespace gambatte;

Channel2::Channel2()
: staticOutputTest_(*this, dutyUnit_)
, disableMaster_(master_, dutyUnit_)
, lengthCounter_(disableMaster_, 0x3F)
, envelopeUnit_(staticOutputTest_)
, soMask_(0)
, prevOut_(0)
, nr4_(0)
, master_(false)
{
	setEvent();
}

void Channel2::setEvent() {
	nextEventUnit = &envelopeUnit_;
	if (lengthCounter_.counter() < nextEventUnit->counter())
		nextEventUnit = &lengthCounter_;
}

void Channel2::setNr1(unsigned data, unsigned long cc) {
	lengthCounter_.nr1Change(data, nr4_, cc);
	dutyUnit_.nr1Change(data, cc);
	setEvent();
}

void Channel2::setNr2(unsigned data, unsigned long cc) {
	if (envelopeUnit_.nr2Change(data))
		disableMaster_();
	else
		staticOutputTest_(cc);

	setEvent();
}

void Channel2::setNr3(unsigned data, unsigned long cc) {
	dutyUnit_.nr3Change(data, cc);
	setEvent();
}

void Channel2::setNr4(unsigned data, unsigned long cc, unsigned long ref) {
	lengthCounter_.nr4Change(nr4_, data, cc);
	nr4_ = data;

	if (nr4_ & psg_nr4_init) {
		nr4_ -= psg_nr4_init;
		master_ = !envelopeUnit_.nr4Init(cc);
		staticOutputTest_(cc);
	}

	dutyUnit_.nr4Change(data, cc, ref, master_);
	setEvent();
}

void Channel2::setSo(unsigned long soMask, unsigned long cc) {
	soMask_ = soMask;
	staticOutputTest_(cc);
	setEvent();
}

void Channel2::reset() {
	dutyUnit_.reset();
	envelopeUnit_.reset();
	setEvent();
}

void Channel2::loadState(SaveState const &state) {
	dutyUnit_.loadState(state.spu.ch2.duty, state.mem.ioamhram.get()[0x116],
		state.spu.ch2.nr4, state.spu.cycleCounter);
	envelopeUnit_.loadState(state.spu.ch2.env, state.mem.ioamhram.get()[0x117],
		state.spu.cycleCounter);
	lengthCounter_.loadState(state.spu.ch2.lcounter, state.spu.cycleCounter);

	nr4_ = state.spu.ch2.nr4;
	master_ = state.spu.ch2.master;
}

void Channel2::update(uint_least32_t* buf, unsigned long const soBaseVol, unsigned long cc, unsigned long const end) {
	unsigned long const outBase = envelopeUnit_.dacIsOn() ? soBaseVol & soMask_ : 0;
	unsigned long const outLow = outBase * -15;

	while (cc < end) {
		unsigned long const outHigh = master_
			? outBase * (envelopeUnit_.getVolume() * 2l - 15)
			: outLow;
		unsigned long const nextMajorEvent = std::min(nextEventUnit->counter(), end);
		unsigned long out = dutyUnit_.isHighState() ? outHigh : outLow;

		while (dutyUnit_.counter() <= nextMajorEvent) {
			*buf += out - prevOut_;
			prevOut_ = out;
			buf += dutyUnit_.counter() - cc;
			cc = dutyUnit_.counter();
			dutyUnit_.event();
			out = dutyUnit_.isHighState() ? outHigh : outLow;
		}
		if (cc < nextMajorEvent) {
			*buf += out - prevOut_;
			prevOut_ = out;
			buf += nextMajorEvent - cc;
			cc = nextMajorEvent;
		}
		if (nextEventUnit->counter() == nextMajorEvent) {
			nextEventUnit->event();
			setEvent();
		}
	}

	if (cc >= SoundUnit::counter_max) {
		dutyUnit_.resetCounters(cc);
		lengthCounter_.resetCounters(cc);
		envelopeUnit_.resetCounters(cc);
	}
}

SYNCFUNC(Channel2)
{
	SSS(lengthCounter_);
	SSS(dutyUnit_);
	SSS(envelopeUnit_);

	EBS(nextEventUnit, 0);
	EVS(nextEventUnit, &dutyUnit_, 1);
	EVS(nextEventUnit, &envelopeUnit_, 2);
	EVS(nextEventUnit, &lengthCounter_, 3);
	EES(nextEventUnit, NULL);

	NSS(soMask_);
	NSS(prevOut_);

	NSS(nr4_);
	NSS(master_);
}

