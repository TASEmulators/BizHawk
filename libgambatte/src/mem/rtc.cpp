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

#include "rtc.h"
#include "../savestate.h"
#include <cstdlib>

namespace gambatte {

Rtc::Rtc(Time &time)
: time_(time)
, activeData_(0)
, activeSet_(0)
, haltTime_(0)
, index_(5)
, dataDh_(0)
, dataDl_(0)
, dataH_(0)
, dataM_(0)
, dataS_(0)
, enabled_(false)
, lastLatchData_(false)
{
}

void Rtc::doLatch(unsigned long const cc) {
	std::uint32_t tmp = time(cc);

	if (tmp >= 0x200 * 86400) {
		tmp %= 0x200 * 86400;
		time_.set(tmp, cc);
		dataDh_ |= 0x80;
	}

	dataDl_ = (tmp / 86400) & 0xFF;
	dataDh_ &= 0xFE;
	dataDh_ |= ((tmp / 86400) & 0x100) >> 8;
	tmp %= 86400;

	dataH_ = tmp / 3600;
	tmp %= 3600;

	dataM_ = tmp / 60;
	tmp %= 60;

	dataS_ = tmp;
}

void Rtc::doSwapActive() {
	if (!enabled_ || index_ > 4) {
		activeData_ = 0;
		activeSet_ = 0;
	} else switch (index_) {
	case 0x00:
		activeData_ = &dataS_;
		activeSet_ = &Rtc::setS;
		break;
	case 0x01:
		activeData_ = &dataM_;
		activeSet_ = &Rtc::setM;
		break;
	case 0x02:
		activeData_ = &dataH_;
		activeSet_ = &Rtc::setH;
		break;
	case 0x03:
		activeData_ = &dataDl_;
		activeSet_ = &Rtc::setDl;
		break;
	case 0x04:
		activeData_ = &dataDh_;
		activeSet_ = &Rtc::setDh;
		break;
	}
}

void Rtc::loadState(SaveState const &state) {
	haltTime_ = state.rtc.haltTime;
	dataDh_ = state.rtc.dataDh;
	dataDl_ = state.rtc.dataDl;
	dataH_ = state.rtc.dataH;
	dataM_ = state.rtc.dataM;
	dataS_ = state.rtc.dataS;
	lastLatchData_ = state.rtc.lastLatchData;
	doSwapActive();
}

void Rtc::setDh(unsigned const newDh, unsigned const long cc) {
	std::uint32_t seconds = time(cc);
	std::uint32_t const oldHighdays = (seconds / 86400) & 0x100;
	seconds -= oldHighdays * 86400;
	seconds += ((newDh & 0x1) << 8) * 86400;
	time_.set(seconds, cc);

	if ((dataDh_ ^ newDh) & 0x40) {
		if (newDh & 0x40)
			haltTime_ = seconds;
		else
			time_.set(haltTime_, cc);
	}
}

void Rtc::setDl(unsigned const newLowdays, unsigned const long cc) {
	std::uint32_t seconds = time(cc);
	std::uint32_t const oldLowdays = (seconds / 86400) & 0xFF;
	seconds -= oldLowdays * 86400;
	seconds += newLowdays * 86400;
	time_.set(seconds, cc);
}

void Rtc::setH(unsigned const newHours, unsigned const long cc) {
	std::uint32_t seconds = time(cc);
	std::uint32_t const oldHours = (seconds / 3600) % 24;
	seconds -= oldHours * 3600;
	seconds += newHours * 3600;
	time_.set(seconds, cc);
}

void Rtc::setM(unsigned const newMinutes, unsigned const long cc) {
	std::uint32_t seconds = time(cc);
	std::uint32_t const oldMinutes = (seconds / 60) % 60;
	seconds -= oldMinutes * 60;
	seconds += newMinutes * 60;
	time_.set(seconds, cc);
}

void Rtc::setS(unsigned const newSeconds, unsigned const long cc) {
	std::uint32_t seconds = time(cc);
	seconds -= seconds % 60;
	seconds += newSeconds;
	time_.reset(seconds, cc);
}

SYNCFUNC(Rtc)
{
	EBS(activeData_, 0);
	EVS(activeData_, &dataS_, 1);
	EVS(activeData_, &dataM_, 2);
	EVS(activeData_, &dataH_, 3);
	EVS(activeData_, &dataDl_, 4);
	EVS(activeData_, &dataDh_, 5);
	EES(activeData_, NULL);

	EBS(activeSet_, 0);
	EVS(activeSet_, &Rtc::setS, 1);
	EVS(activeSet_, &Rtc::setM, 2);
	EVS(activeSet_, &Rtc::setH, 3);
	EVS(activeSet_, &Rtc::setDl, 4);
	EVS(activeSet_, &Rtc::setDh, 5);
	EES(activeSet_, NULL);

	NSS(haltTime_);
	NSS(index_);
	NSS(dataDh_);
	NSS(dataDl_);
	NSS(dataH_);
	NSS(dataM_);
	NSS(dataS_);
	NSS(enabled_);
	NSS(lastLatchData_);
}

}
