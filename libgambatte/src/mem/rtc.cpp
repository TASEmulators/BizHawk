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

Rtc::Rtc()
: activeData_(0)
, activeSet_(0)
, baseTime_(0)
, haltTime_(0)
, index_(5)
, dataDh_(0)
, dataDl_(0)
, dataH_(0)
, dataM_(0)
, dataS_(0)
, enabled_(false)
, lastLatchData_(false)
, timeCB(0)
{
}

void Rtc::doLatch() {
	std::uint32_t tmp = ((dataDh_ & 0x40) ? haltTime_ : timeCB()) - baseTime_;

	while (tmp > 0x1FF * 86400) {
		baseTime_ += 0x1FF * 86400;
		tmp -= 0x1FF * 86400;
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
	baseTime_ = state.rtc.baseTime;
	haltTime_ = state.rtc.haltTime;
	dataDh_ = state.rtc.dataDh;
	dataDl_ = state.rtc.dataDl;
	dataH_ = state.rtc.dataH;
	dataM_ = state.rtc.dataM;
	dataS_ = state.rtc.dataS;
	lastLatchData_ = state.rtc.lastLatchData;
	doSwapActive();
}

void Rtc::setDh(unsigned const newDh) {
	const std::uint32_t unixtime = (dataDh_ & 0x40) ? haltTime_ : timeCB();
	const std::uint32_t oldHighdays = ((unixtime - baseTime_) / 86400) & 0x100;
	baseTime_ += oldHighdays * 86400;
	baseTime_ -= ((newDh & 0x1) << 8) * 86400;

	if ((dataDh_ ^ newDh) & 0x40) {
		if (newDh & 0x40)
			haltTime_ = timeCB();
		else
			baseTime_ += timeCB() - haltTime_;
	}
}

void Rtc::setDl(unsigned const newLowdays) {
	const std::uint32_t unixtime = (dataDh_ & 0x40) ? haltTime_ : timeCB();
	const std::uint32_t oldLowdays = ((unixtime - baseTime_) / 86400) & 0xFF;
	baseTime_ += oldLowdays * 86400;
	baseTime_ -= newLowdays * 86400;
}

void Rtc::setH(unsigned const newHours) {
	const std::uint32_t unixtime = (dataDh_ & 0x40) ? haltTime_ : timeCB();
	const std::uint32_t oldHours = ((unixtime - baseTime_) / 3600) % 24;
	baseTime_ += oldHours * 3600;
	baseTime_ -= newHours * 3600;
}

void Rtc::setM(unsigned const newMinutes) {
	const std::uint32_t unixtime = (dataDh_ & 0x40) ? haltTime_ : timeCB();
	const std::uint32_t oldMinutes = ((unixtime - baseTime_) / 60) % 60;
	baseTime_ += oldMinutes * 60;
	baseTime_ -= newMinutes * 60;
}

void Rtc::setS(unsigned const newSeconds) {
	const std::uint32_t unixtime = (dataDh_ & 0x40) ? haltTime_ : timeCB();
	baseTime_ += (unixtime - baseTime_) % 60;
	baseTime_ -= newSeconds;
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

	NSS(baseTime_);
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
