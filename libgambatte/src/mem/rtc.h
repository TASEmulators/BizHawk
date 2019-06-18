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

#ifndef RTC_H
#define RTC_H

#include <cstdint>
#include "time.h"
#include "newstate.h"

namespace gambatte {

struct SaveState;

class Rtc {
public:
	Rtc(Time &time);
	unsigned char const * activeData() const { return activeData_; }

	void latch(unsigned data, unsigned long const cc) {
		if (!lastLatchData_ && data == 1)
			doLatch(cc);

		lastLatchData_ = data;
	}

	void loadState(SaveState const &state);

	void set(bool enabled, unsigned bank) {
		bank &= 0xF;
		bank -= 8;

		enabled_ = enabled;
		index_ = bank;
		doSwapActive();
	}

	void write(unsigned data, unsigned long const cc) {
		(this->*activeSet_)(data, cc);
		*activeData_ = data;
	}

private:
	Time &time_;
	unsigned char *activeData_;
	void (Rtc::*activeSet_)(unsigned, unsigned long);
	std::uint32_t haltTime_;
	unsigned char index_;
	unsigned char dataDh_;
	unsigned char dataDl_;
	unsigned char dataH_;
	unsigned char dataM_;
	unsigned char dataS_;
	bool enabled_;
	bool lastLatchData_;

	void doLatch(unsigned long cycleCounter);
	void doSwapActive();
	void setDh(unsigned newDh, unsigned long cycleCounter);
	void setDl(unsigned newLowdays, unsigned long cycleCounter);
	void setH(unsigned newHours, unsigned long cycleCounter);
	void setM(unsigned newMinutes, unsigned long cycleCounter);
	void setS(unsigned newSeconds, unsigned long cycleCounter);

	std::uint32_t time(unsigned long const cc) {
		return dataDh_ & 0x40 ? haltTime_ : time_.get(cc);
	}
public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
