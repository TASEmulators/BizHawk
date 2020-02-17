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

#include "ly_counter.h"
#include "../savestate.h"

namespace gambatte {

LyCounter::LyCounter()
: time_(0)
, lineTime_(0)
, ly_(0)
, ds_(false)
{
	setDoubleSpeed(false);
	reset(0, 0);
}

void LyCounter::doEvent() {
	++ly_;
	if (ly_ == lcd_lines_per_frame)
		ly_ = 0;

	time_ = time_ + lineTime_;
}

unsigned long LyCounter::nextLineCycle(unsigned const lineCycle, unsigned long const cc) const {
	unsigned long tmp = time_ + (lineCycle << ds_);
	if (tmp - cc > lineTime_)
		tmp -= lineTime_;

	return tmp;
}

unsigned long LyCounter::nextFrameCycle(unsigned long const frameCycle, unsigned long const cc) const {
	unsigned long tmp = time_ + (((lcd_lines_per_frame - 1l - ly()) * lcd_cycles_per_line + frameCycle) << ds_);
	if (tmp - cc > 1ul * lcd_cycles_per_frame << ds_)
		tmp -= 1ul * lcd_cycles_per_frame << ds_;

	return tmp;
}

void LyCounter::reset(unsigned long videoCycles, unsigned long lastUpdate) {
	ly_ = videoCycles / lcd_cycles_per_line;
	time_ = lastUpdate + ((lcd_cycles_per_line
		- (videoCycles - 1l * ly_ * lcd_cycles_per_line)) << isDoubleSpeed());
}

void LyCounter::setDoubleSpeed(bool ds) {
	ds_ = ds;
	lineTime_ = lcd_cycles_per_line << ds;
}

SYNCFUNC(LyCounter)
{
	NSS(time_);
	NSS(lineTime_);
	NSS(ly_);
	NSS(ds_);
}

}
