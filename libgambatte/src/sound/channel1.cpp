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

#include "channel1.h"
#include "psgdef.h"
#include "../savestate.h"
#include <algorithm>

using namespace gambatte;

Channel1::SweepUnit::SweepUnit(MasterDisabler &disabler, DutyUnit &dutyUnit)
: disableMaster_(disabler)
, dutyUnit_(dutyUnit)
, shadow_(0)
, nr0_(0)
, neg_(false)
, cgb_(false)
{
}

unsigned Channel1::SweepUnit::calcFreq() {
	unsigned const freq = nr0_ & psg_nr10_neg
		? shadow_ - (shadow_ >> (nr0_ & psg_nr10_rsh))
		: shadow_ + (shadow_ >> (nr0_ & psg_nr10_rsh));
	if (nr0_ & psg_nr10_neg)
		neg_ = true;
	if (freq & 2048)
		disableMaster_();

	return freq;
}

void Channel1::SweepUnit::event() {
	unsigned long const period = (nr0_ & psg_nr10_time) / (1u * psg_nr10_time & -psg_nr10_time);

	if (period) {
		unsigned const freq = calcFreq();

		if (!(freq & 2048) && nr0_ & psg_nr10_rsh) {
			shadow_ = freq;
			dutyUnit_.setFreq(freq, counter_);
			calcFreq();
		}

		counter_ += period << 14;
	} else
		counter_ += 8ul << 14;
}

void Channel1::SweepUnit::nr0Change(unsigned newNr0) {
	if (neg_ && !(newNr0 & 0x08))
		disableMaster_();

	nr0_ = newNr0;
}

void Channel1::SweepUnit::nr4Init(unsigned long const cc) {
	neg_ = false;
	shadow_ = dutyUnit_.freq();

	unsigned const period = (nr0_ & psg_nr10_time) / (1u * psg_nr10_time & -psg_nr10_time);
	unsigned const rsh = nr0_ & psg_nr10_rsh;

	if (period | rsh)
		counter_ = ((((cc + 2 + cgb_ * 2) >> 14) + (period ? period : 8)) << 14) + 2;
	else
		counter_ = counter_disabled;

	if (rsh)
		calcFreq();
}

void Channel1::SweepUnit::reset() {
	counter_ = counter_disabled;
}

void Channel1::SweepUnit::loadState(SaveState const &state) {
	counter_ = std::max(state.spu.ch1.sweep.counter, state.spu.cycleCounter);
	shadow_ = state.spu.ch1.sweep.shadow;
	nr0_ = state.spu.ch1.sweep.nr0;
	neg_ = state.spu.ch1.sweep.neg;
}

template<bool isReader>
void Channel1::SweepUnit::SyncState(NewState *ns)
{
	NSS(counter_);
	NSS(shadow_);
	NSS(nr0_);
	NSS(neg_);
	NSS(cgb_);
}

Channel1::Channel1()
: staticOutputTest_(*this, dutyUnit_)
, disableMaster_(master_, dutyUnit_)
, lengthCounter_(disableMaster_, 0x3F)
, envelopeUnit_(staticOutputTest_)
, sweepUnit_(disableMaster_, dutyUnit_)
, nextEventUnit_(0)
, soMask_(0)
, prevOut_(0)
, nr4_(0)
, master_(false)
{
	setEvent();
}

void Channel1::setEvent() {
	nextEventUnit_ = &sweepUnit_;
	if (envelopeUnit_.counter() < nextEventUnit_->counter())
		nextEventUnit_ = &envelopeUnit_;
	if (lengthCounter_.counter() < nextEventUnit_->counter())
		nextEventUnit_ = &lengthCounter_;
}

void Channel1::setNr0(unsigned data) {
	sweepUnit_.nr0Change(data);
	setEvent();
}

void Channel1::setNr1(unsigned data, unsigned long cc) {
	lengthCounter_.nr1Change(data, nr4_, cc);
	dutyUnit_.nr1Change(data, cc);
	setEvent();
}

void Channel1::setNr2(unsigned data, unsigned long cc) {
	if (envelopeUnit_.nr2Change(data))
		disableMaster_();
	else
		staticOutputTest_(cc);

	setEvent();
}

void Channel1::setNr3(unsigned data, unsigned long cc) {
	dutyUnit_.nr3Change(data, cc);
	setEvent();
}

void Channel1::setNr4(unsigned data, unsigned long cc, unsigned long ref) {
	lengthCounter_.nr4Change(nr4_, data, cc);
	dutyUnit_.nr4Change(data, cc, ref, master_);
	nr4_ = data;

	if (nr4_ & psg_nr4_init) {
		nr4_ -= psg_nr4_init;
		master_ = !envelopeUnit_.nr4Init(cc);
		sweepUnit_.nr4Init(cc);
		staticOutputTest_(cc);
	}

	setEvent();
}

void Channel1::setSo(unsigned long soMask, unsigned long cc) {
	soMask_ = soMask;
	staticOutputTest_(cc);
	setEvent();
}

void Channel1::reset() {
	dutyUnit_.reset();
	envelopeUnit_.reset();
	sweepUnit_.reset();
	setEvent();
}

void Channel1::init(bool cgb) {
	sweepUnit_.init(cgb);
}

void Channel1::loadState(SaveState const &state) {
	sweepUnit_.loadState(state);
	dutyUnit_.loadState(state.spu.ch1.duty, state.mem.ioamhram.get()[0x111],
		state.spu.ch1.nr4, state.spu.cycleCounter);
	envelopeUnit_.loadState(state.spu.ch1.env, state.mem.ioamhram.get()[0x112],
		state.spu.cycleCounter);
	lengthCounter_.loadState(state.spu.ch1.lcounter, state.spu.cycleCounter);

	nr4_ = state.spu.ch1.nr4;
	master_ = state.spu.ch1.master;
}

void Channel1::update(uint_least32_t* buf, unsigned long const soBaseVol, unsigned long cc, unsigned long const end) {
	unsigned long const outBase = envelopeUnit_.dacIsOn() ? soBaseVol & soMask_ : 0;
	unsigned long const outLow = outBase * -15;

	while (cc < end) {
		unsigned long const outHigh = master_
			? outBase * (envelopeUnit_.getVolume() * 2l - 15)
			: outLow;
		unsigned long const nextMajorEvent = std::min(nextEventUnit_->counter(), end);
		unsigned long out = dutyUnit_.isHighState() ? outHigh : outLow;

		while (dutyUnit_.counter() <= nextMajorEvent) {
			*buf = out - prevOut_;
			prevOut_ = out;
			buf += dutyUnit_.counter() - cc;
			cc = dutyUnit_.counter();
			dutyUnit_.event();
			out = dutyUnit_.isHighState() ? outHigh : outLow;
		}
		if (cc < nextMajorEvent) {
			*buf = out - prevOut_;
			prevOut_ = out;
			buf += nextMajorEvent - cc;
			cc = nextMajorEvent;
		}
		if (nextEventUnit_->counter() == nextMajorEvent) {
			nextEventUnit_->event();
			setEvent();
		}
	}

	if (cc >= SoundUnit::counter_max) {
		dutyUnit_.resetCounters(cc);
		lengthCounter_.resetCounters(cc);
		envelopeUnit_.resetCounters(cc);
		sweepUnit_.resetCounters(cc);
	}
}

SYNCFUNC(Channel1)
{
	SSS(lengthCounter_);
	SSS(dutyUnit_);
	SSS(envelopeUnit_);
	SSS(sweepUnit_);

	EBS(nextEventUnit_, 0);
	EVS(nextEventUnit_, &dutyUnit_, 1);
	EVS(nextEventUnit_, &sweepUnit_, 2);
	EVS(nextEventUnit_, &envelopeUnit_, 3);
	EVS(nextEventUnit_, &lengthCounter_, 4);
	EES(nextEventUnit_, NULL);

	NSS(soMask_);
	NSS(prevOut_);

	NSS(nr4_);
	NSS(master_);
}
