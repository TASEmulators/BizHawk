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

#include "lyc_irq.h"
#include "counterdef.h"
#include "lcddef.h"
#include "ly_counter.h"
#include "savestate.h"
#include <algorithm>

using namespace gambatte;

namespace {

	unsigned long schedule(unsigned statReg,
		unsigned lycReg, LyCounter const& lyCounter, unsigned long cc) {
		return (statReg & lcdstat_lycirqen) && lycReg < lcd_lines_per_frame
			? lyCounter.nextFrameCycle(lycReg
				? 1l * lycReg * lcd_cycles_per_line - 2
				: (lcd_lines_per_frame - 1l) * lcd_cycles_per_line + 6, cc)
			: 1 * disabled_time;
	}

	bool lycIrqBlockedByM2OrM1StatIrq(unsigned ly, unsigned statreg) {
		return ly <= lcd_vres && ly > 0
			? statreg & lcdstat_m2irqen
			: statreg & lcdstat_m1irqen;
	}

}

LycIrq::LycIrq()
: time_(disabled_time)
, lycRegSrc_(0)
, statRegSrc_(0)
, lycReg_(0)
, statReg_(0)
, cgb_(false)
{
}

void LycIrq::regChange(unsigned const statReg,
	unsigned const lycReg, LyCounter const& lyCounter, unsigned long const cc) {
	unsigned long const timeSrc = schedule(statReg, lycReg, lyCounter, cc);
	statRegSrc_ = statReg;
	lycRegSrc_ = lycReg;
	time_ = std::min(time_, timeSrc);

	if (cgb_) {
		if (time_ - cc > 6u + 4 * lyCounter.isDoubleSpeed() || (timeSrc != time_ && time_ - cc > 2))
			lycReg_ = lycReg;
		if (time_ - cc > 2)
			statReg_ = statReg;
	}
	else {
		if (time_ - cc > 4 || timeSrc != time_)
			lycReg_ = lycReg;

		statReg_ = statReg;
	}
}

bool LycIrq::doEvent(LyCounter const& lyCounter) {
	bool flagIrq = false;
	if ((statReg_ | statRegSrc_) & lcdstat_lycirqen) {
		unsigned const cmpLy = lyCounter.ly() == lcd_lines_per_frame - 1
			? 0
			: lyCounter.ly() + 1;
		flagIrq = lycReg_ == cmpLy && !lycIrqBlockedByM2OrM1StatIrq(lycReg_, statReg_);
	}

	lycReg_ = lycRegSrc_;
	statReg_ = statRegSrc_;
	time_ = schedule(statReg_, lycReg_, lyCounter, time_);
	return flagIrq;
}

void LycIrq::loadState(SaveState const &state) {
	lycRegSrc_ = state.mem.ioamhram.get()[0x145];
	statRegSrc_ = state.mem.ioamhram.get()[0x141];
	lycReg_ = state.ppu.lyc;
	statReg_ = statRegSrc_;
}

void LycIrq::reschedule(LyCounter const &lyCounter, unsigned long cc) {
	time_ = std::min(schedule(statReg_   , lycReg_   , lyCounter, cc),
	                 schedule(statRegSrc_, lycRegSrc_, lyCounter, cc));
}

void LycIrq::lcdReset() {
	statReg_ = statRegSrc_;
	lycReg_ = lycRegSrc_;
}

SYNCFUNC(LycIrq)
{
	NSS(time_);
	NSS(lycRegSrc_);
	NSS(statRegSrc_);
	NSS(lycReg_);
	NSS(statReg_);
	NSS(cgb_);
}
