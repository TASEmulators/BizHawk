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

#include "tima.h"
#include "savestate.h"

using namespace gambatte;

namespace {
	unsigned char const timaClock[4] = { 10, 4, 6, 8 };
}

namespace gambatte {

Tima::Tima()
: divLastUpdate_(0)
, lastUpdate_(0)
, tmatime_(disabled_time)
, tima_(0)
, tma_(0)
, tac_(0)
{
}

void Tima::loadState(SaveState const &state, TimaInterruptRequester timaIrq) {
	divLastUpdate_ = state.mem.divLastUpdate - 0x100l * state.mem.ioamhram.get()[0x104];
	lastUpdate_ = state.mem.timaLastUpdate;
	tmatime_ = state.mem.tmatime;
	tima_ = state.mem.ioamhram.get()[0x105];
	tma_  = state.mem.ioamhram.get()[0x106];
	tac_  = state.mem.ioamhram.get()[0x107];

	unsigned long nextIrqEventTime = disabled_time;
	if (tac_ & 4) {
		nextIrqEventTime = tmatime_ != disabled_time && tmatime_ > state.cpu.cycleCounter
			? tmatime_
			: lastUpdate_ + ((256l - tima_) << timaClock[tac_ & 3]) + 3;
	}

	timaIrq.setNextIrqEventTime(nextIrqEventTime);
}

void Tima::resetCc(unsigned long const oldCc, unsigned long const newCc, TimaInterruptRequester timaIrq) {
	if (tac_ & 0x04) {
		updateIrq(oldCc, timaIrq);
		updateTima(oldCc);

		unsigned long const dec = oldCc - newCc;
		lastUpdate_ -= dec;
		timaIrq.setNextIrqEventTime(timaIrq.nextIrqEventTime() - dec);

		if (tmatime_ != disabled_time)
			tmatime_ -= dec;
	}
}

void Tima::updateTima(unsigned long const cc) {
	unsigned long const ticks = (cc - lastUpdate_) >> timaClock[tac_ & 3];
	lastUpdate_ += ticks << timaClock[tac_ & 3];

	if (cc >= tmatime_) {
		if (cc >= tmatime_ + 4)
			tmatime_ = disabled_time;

		tima_ = tma_;
	}

	unsigned long tmp = tima_ + ticks;
	while (tmp > 0x100)
		tmp -= 0x100 - tma_;

	if (tmp == 0x100) {
		tmp = 0;
		tmatime_ = lastUpdate_ + 3;
		if (cc >= tmatime_) {
			if (cc >= tmatime_ + 4)
				tmatime_ = disabled_time;

			tmp = tma_;
		}
	}

	tima_ = tmp;
}

void Tima::setTima(unsigned const data, unsigned long const cc, TimaInterruptRequester timaIrq) {
	if (tac_ & 0x04) {
		updateIrq(cc, timaIrq);
		updateTima(cc);

		if (tmatime_ - cc < 4)
			tmatime_ = disabled_time;

		timaIrq.setNextIrqEventTime(lastUpdate_ + ((256l - data) << timaClock[tac_ & 3]) + 3);
	}

	tima_ = data;
}

void Tima::setTma(unsigned const data, unsigned long const cc, TimaInterruptRequester timaIrq) {
	if (tac_ & 0x04) {
		updateIrq(cc, timaIrq);
		updateTima(cc);
	}

	tma_ = data;
}

void Tima::setTac(unsigned const data, unsigned long const cc, TimaInterruptRequester timaIrq, bool agbFlag) {
	if (tac_ ^ data) {
		unsigned long nextIrqEventTime = timaIrq.nextIrqEventTime();

		if (tac_ & 0x04) {
			unsigned const inc = ~(data >> 2 & (cc - divLastUpdate_) >> (timaClock[data & 3] - 1)) & 1;
			lastUpdate_ -= (inc << (timaClock[tac_ & 3] - 1)) + 3;
			nextIrqEventTime -= (inc << (timaClock[tac_ & 3] - 1)) + 3;
			if (cc >= nextIrqEventTime)
				timaIrq.flagIrq();

			updateTima(cc);
			tmatime_ = disabled_time;
			nextIrqEventTime = disabled_time;
		}

		if (data & 4) {
			if (agbFlag) {
				unsigned long diff = cc - divLastUpdate_;
				if (((diff >> (timaClock[tac_ & 3] - 1)) & 1) == 1 && ((diff >> (timaClock[data & 3] - 1)) & 1) == 0)
					tima_++;
			}

			lastUpdate_ = cc - ((cc - divLastUpdate_) & ((1u << timaClock[data & 3]) - 1));
			nextIrqEventTime = lastUpdate_ + ((256l - tima_) << timaClock[data & 3]) + 3;
		}

		timaIrq.setNextIrqEventTime(nextIrqEventTime);
	}

	tac_ = data;
}

void Tima::divReset(unsigned long cc, TimaInterruptRequester timaIrq) {
	if (tac_ & 0x04) {
		unsigned long nextIrqEventTime = timaIrq.nextIrqEventTime();
		lastUpdate_ -= (1u << (timaClock[tac_ & 3] - 1)) + 3;
		nextIrqEventTime -= (1u << (timaClock[tac_ & 3] - 1)) + 3;
		if (cc >= nextIrqEventTime)
			timaIrq.flagIrq();

		updateTima(cc);
		lastUpdate_ = cc;
		timaIrq.setNextIrqEventTime(lastUpdate_ + ((256l - tima_) << timaClock[tac_ & 3]) + 3);
	}

	divLastUpdate_ = cc;
}

void Tima::speedChange(TimaInterruptRequester timaIrq) {
	if ((tac_ & 0x07) >= 0x05) {
		lastUpdate_ -= 4;
		timaIrq.setNextIrqEventTime(timaIrq.nextIrqEventTime() - 4);
	}
}

unsigned Tima::tima(unsigned long cc) {
	if (tac_ & 0x04)
		updateTima(cc);

	return tima_;
}

void Tima::doIrqEvent(TimaInterruptRequester timaIrq) {
	timaIrq.flagIrq(timaIrq.nextIrqEventTime());
	timaIrq.setNextIrqEventTime(timaIrq.nextIrqEventTime()
		+ ((256l - tma_) << timaClock[tac_ & 3]));
}

SYNCFUNC(Tima)
{
	NSS(lastUpdate_);
	NSS(divLastUpdate_);
	NSS(tmatime_);
	NSS(tima_);
	NSS(tma_);
	NSS(tac_);
}

}
