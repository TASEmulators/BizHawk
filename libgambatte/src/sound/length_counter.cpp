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

#include "length_counter.h"
#include "master_disabler.h"
#include "psgdef.h"
#include <algorithm>

using namespace gambatte;

LengthCounter::LengthCounter(MasterDisabler &disabler, unsigned const mask)
: disableMaster_(disabler)
, lengthCounter_(0)
, lengthMask_(mask)
{
	nr1Change(0, 0, 0);
}

void LengthCounter::event() {
	counter_ = counter_disabled;
	lengthCounter_ = 0;
	disableMaster_();
}

void LengthCounter::nr1Change(unsigned const newNr1, unsigned const nr4, unsigned long const cc) {
	lengthCounter_ = (~newNr1 & lengthMask_) + 1;
	counter_ = nr4 & psg_nr4_lcen
		? ((cc >> 13) + lengthCounter_) << 13
		: 1 * counter_disabled;
}

void LengthCounter::nr4Change(unsigned const oldNr4, unsigned const newNr4, unsigned long const cc) {
	if (counter_ != counter_disabled)
		lengthCounter_ = (counter_ >> 13) - (cc >> 13);

	unsigned dec = 0;
	if (newNr4 & psg_nr4_lcen) {
		dec = ~cc >> 12 & 1;
		if (!(oldNr4 & psg_nr4_lcen) && lengthCounter_) {
			if (!(lengthCounter_ -= dec))
				disableMaster_();
		}
	}

	if (newNr4 & psg_nr4_init && !lengthCounter_)
		lengthCounter_ = lengthMask_ + 1 - dec;

	counter_ = newNr4 & psg_nr4_lcen && lengthCounter_
		? ((cc >> 13) + lengthCounter_) << 13
		: 1 * counter_disabled;
}

void LengthCounter::loadState(SaveState::SPU::LCounter const &lstate, unsigned long const cc) {
	counter_ = std::max(lstate.counter, cc);
	lengthCounter_ = lstate.lengthCounter;
}

SYNCFUNC(LengthCounter)
{
	NSS(counter_);
	NSS(lengthCounter_);
}
