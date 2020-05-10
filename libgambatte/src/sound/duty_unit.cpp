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

#include "duty_unit.h"
#include "psgdef.h"
#include <algorithm>

namespace {

	int const duty_pattern_len = 8;

	bool toOutState(unsigned duty, unsigned pos) {
		return 0x7EE18180 >> (duty * duty_pattern_len + pos) & 1;
	}

	unsigned toPeriod(unsigned freq) {
		return (2048 - freq) * 2;
	}

}

using namespace gambatte;

DutyUnit::DutyUnit()
: nextPosUpdate_(counter_disabled)
, period_(4096)
, pos_(0)
, duty_(0)
, inc_(0)
, high_(false)
, enableEvents_(true)
{
}

void DutyUnit::updatePos(unsigned long const cc) {
	if (cc >= nextPosUpdate_) {
		unsigned long const inc = (cc - nextPosUpdate_) / period_ + 1;
		nextPosUpdate_ += period_ * inc;
		pos_ = (pos_ + inc) % duty_pattern_len;
		high_ = toOutState(duty_, pos_);
	}
}

void DutyUnit::setCounter() {
	static unsigned char const nextStateDistance[][duty_pattern_len] = {
		{ 7, 6, 5, 4, 3, 2, 1, 1 },
		{ 1, 6, 5, 4, 3, 2, 1, 2 },
		{ 1, 4, 3, 2, 1, 4, 3, 2 },
		{ 1, 6, 5, 4, 3, 2, 1, 2 }
	};

	if (enableEvents_ && nextPosUpdate_ != counter_disabled) {
		unsigned const npos = (pos_ + 1) % duty_pattern_len;
		counter_ = nextPosUpdate_;
		inc_ = nextStateDistance[duty_][npos];
		if (toOutState(duty_, npos) == high_) {
			counter_ += period_ * inc_;
			inc_ = nextStateDistance[duty_][(npos + inc_) % duty_pattern_len];
		}
	} else
		counter_ = counter_disabled;
}

void DutyUnit::setFreq(unsigned newFreq, unsigned long cc) {
	updatePos(cc);
	period_ = toPeriod(newFreq);
	setCounter();
}

void DutyUnit::event() {
	static unsigned char const inc[][2] = {
		{ 1, 7 },
		{ 2, 6 },
		{ 4, 4 },
		{ 6, 2 }
	};

	high_ ^= true;
	counter_ += inc_ * period_;
	inc_ = inc[duty_][high_];
}

void DutyUnit::nr1Change(unsigned newNr1, unsigned long cc) {
	updatePos(cc);
	duty_ = newNr1 >> 6;
	setCounter();
}

void DutyUnit::nr3Change(unsigned newNr3, unsigned long cc) {
	setFreq((freq() & 0x700) | newNr3, cc);
}

void DutyUnit::nr4Change(unsigned const newNr4, unsigned long const cc, unsigned long const ref, bool const master) {
	setFreq((newNr4 << 8 & 0x700) | (freq() & 0xFF), cc);

	if (newNr4 & psg_nr4_init) {
		nextPosUpdate_ = cc - (cc - ref) % 2 + period_ + 4 - (master << 1);
		setCounter();
	}
}

void DutyUnit::reset() {
	pos_ = 0;
	high_ = false;
	nextPosUpdate_ = counter_disabled;
	setCounter();
}

void DutyUnit::resetCc(unsigned long cc, unsigned long newCc) {
	if (nextPosUpdate_ == counter_disabled)
		return;

	updatePos(cc);
	nextPosUpdate_ -= cc - newCc;
	setCounter();
}

void DutyUnit::loadState(SaveState::SPU::Duty const &dstate,
		unsigned const nr1, unsigned const nr4, unsigned long const cc) {
	nextPosUpdate_ = std::max(dstate.nextPosUpdate, cc);
	pos_ = dstate.pos & 7;
	high_ = dstate.high;
	duty_ = nr1 >> 6;
	period_ = toPeriod((nr4 << 8 & 0x700) | dstate.nr3);
	enableEvents_ = true;
	setCounter();
}

void DutyUnit::resetCounters(unsigned long cc) {
	resetCc(cc, cc - counter_max);
}

void DutyUnit::killCounter() {
	enableEvents_ = false;
	setCounter();
}

void DutyUnit::reviveCounter(unsigned long const cc) {
	updatePos(cc);
	enableEvents_ = true;
	setCounter();
}

SYNCFUNC(DutyUnit)
{
	NSS(counter_);
	NSS(nextPosUpdate_);
	NSS(period_);
	NSS(pos_);
	NSS(duty_);
	NSS(inc_);
	NSS(high_);
	NSS(enableEvents_);
}
