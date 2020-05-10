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

#ifndef TIME_H
#define TIME_H

#include <chrono>
#include <cstdint>
#include <ctime>
#include "newstate.h"

namespace gambatte {

struct SaveState;

struct timeval {
	std::uint32_t tv_sec;
	std::uint32_t tv_usec;
};

class Time {
public:
	static timeval now() {
		long long micros = std::chrono::duration_cast<std::chrono::microseconds>(
				std::chrono::high_resolution_clock::now().time_since_epoch())
			.count();
		timeval t;
		t.tv_usec = micros % 1000000;
		t.tv_sec = micros / 1000000;
		return t;
	}

	Time();
	void loadState(SaveState const &state);

	std::uint32_t get(unsigned long cycleCounter);
	void set(std::uint32_t seconds, unsigned long cycleCounter);
	void reset(std::uint32_t seconds, unsigned long cycleCounter);
	void resetCc(unsigned long oldCc, unsigned long newCc);
	void speedChange(unsigned long cycleCounter);

	timeval baseTime(unsigned long cycleCounter);
	void setBaseTime(timeval baseTime, unsigned long cycleCounter);
	void setTimeMode(bool useCycles, unsigned long cycleCounter);
	void setRtcDivisorOffset(long const rtcDivisorOffset) { rtcDivisor_ = 0x400000L + rtcDivisorOffset; }

private:
	std::uint32_t seconds_;
	timeval lastTime_;
	unsigned long lastCycles_;
	bool useCycles_;
	unsigned long rtcDivisor_;
	bool ds_;

	void update(unsigned long cycleCounter);
	void cyclesFromTime(unsigned long cycleCounter);
	void timeFromCycles(unsigned long cycleCounter);

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
