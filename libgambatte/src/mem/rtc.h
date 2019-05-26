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
#include "newstate.h"

namespace gambatte {

struct SaveState;

class Rtc {
public:
	Rtc();

	unsigned char const * activeData() const { return activeData_; }
	std::uint32_t getBaseTime() const { return baseTime_; }

	void setBaseTime(const std::uint32_t baseTime) {
		this->baseTime_ = baseTime;
	}

	void latch(unsigned data) {
		if (!lastLatchData_ && data == 1)
			doLatch();

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

	void write(unsigned data) {
		(this->*activeSet_)(data);
		*activeData_ = data;
	}

	void setRTCCallback(std::uint32_t (*callback)()) {
		timeCB = callback;
	}

private:
	unsigned char *activeData_;
	void (Rtc::*activeSet_)(unsigned);
	std::uint32_t baseTime_;
	std::uint32_t haltTime_;
	unsigned char index_;
	unsigned char dataDh_;
	unsigned char dataDl_;
	unsigned char dataH_;
	unsigned char dataM_;
	unsigned char dataS_;
	bool enabled_;
	bool lastLatchData_;
	std::uint32_t (*timeCB)();

	void doLatch();
	void doSwapActive();
	void setDh(unsigned newDh);
	void setDl(unsigned newLowdays);
	void setH(unsigned newHours);
	void setM(unsigned newMinutes);
	void setS(unsigned newSeconds);

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
