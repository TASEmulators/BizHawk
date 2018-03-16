/***************************************************************************
 *   Copyright (C) 2007 by Sindre Aamås                                    *
 *   aamas@stud.ntnu.no                                                    *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License version 2 as     *
 *   published by the Free Software Foundation.                            *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License version 2 for more details.                *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   version 2 along with this program; if not, write to the               *
 *   Free Software Foundation, Inc.,                                       *
 *   59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.             *
 ***************************************************************************/
#include "rtc.h"
#include "../savestate.h"
#include <cstdlib>

namespace gambatte {

Rtc::Rtc()
: activeData(NULL),
  activeSet(NULL),
  baseTime(0),
  haltTime(0),
  index(5),
  dataDh(0),
  dataDl(0),
  dataH(0),
  dataM(0),
  dataS(0),
  enabled(false),
  lastLatchData(false),
  timeCB(0)
{
}

void Rtc::doLatch() {
	std::uint32_t tmp = ((dataDh & 0x40) ? haltTime : timeCB()) - baseTime;
	
	while (tmp > 0x1FF * 86400) {
		baseTime += 0x1FF * 86400;
		tmp -= 0x1FF * 86400;
		dataDh |= 0x80;
	}
	
	dataDl = (tmp / 86400) & 0xFF;
	dataDh &= 0xFE;
	dataDh |= ((tmp / 86400) & 0x100) >> 8;
	tmp %= 86400;
	
	dataH = tmp / 3600;
	tmp %= 3600;
	
	dataM = tmp / 60;
	tmp %= 60;
	
	dataS = tmp;
}

void Rtc::doSwapActive() {
	if (!enabled || index > 4) {
		activeData = NULL;
		activeSet = NULL;
	} else switch (index) {
	case 0x00:
		activeData = &dataS;
		activeSet = &Rtc::setS;
		break;
	case 0x01:
		activeData = &dataM;
		activeSet = &Rtc::setM;
		break;
	case 0x02:
		activeData = &dataH;
		activeSet = &Rtc::setH;
		break;
	case 0x03:
		activeData = &dataDl;
		activeSet = &Rtc::setDl;
		break;
	case 0x04:
		activeData = &dataDh;
		activeSet = &Rtc::setDh;
		break;
	}
}

void Rtc::loadState(const SaveState &state) {
	baseTime = state.rtc.baseTime;
	haltTime = state.rtc.haltTime;
	dataDh = state.rtc.dataDh;
	dataDl = state.rtc.dataDl;
	dataH = state.rtc.dataH;
	dataM = state.rtc.dataM;
	dataS = state.rtc.dataS;
	lastLatchData = state.rtc.lastLatchData;
	
	doSwapActive();
}

void Rtc::setDh(const unsigned new_dh) {
	const std::uint32_t unixtime = (dataDh & 0x40) ? haltTime : timeCB();
	const std::uint32_t old_highdays = ((unixtime - baseTime) / 86400) & 0x100;
	baseTime += old_highdays * 86400;
	baseTime -= ((new_dh & 0x1) << 8) * 86400;
	
	if ((dataDh ^ new_dh) & 0x40) {
		if (new_dh & 0x40)
			haltTime = timeCB();
		else
			baseTime += timeCB() - haltTime;
	}
}

void Rtc::setDl(const unsigned new_lowdays) {
	const std::uint32_t unixtime = (dataDh & 0x40) ? haltTime : timeCB();
	const std::uint32_t old_lowdays = ((unixtime - baseTime) / 86400) & 0xFF;
	baseTime += old_lowdays * 86400;
	baseTime -= new_lowdays * 86400;
}

void Rtc::setH(const unsigned new_hours) {
	const std::uint32_t unixtime = (dataDh & 0x40) ? haltTime : timeCB();
	const std::uint32_t old_hours = ((unixtime - baseTime) / 3600) % 24;
	baseTime += old_hours * 3600;
	baseTime -= new_hours * 3600;
}

void Rtc::setM(const unsigned new_minutes) {
	const std::uint32_t unixtime = (dataDh & 0x40) ? haltTime : timeCB();
	const std::uint32_t old_minutes = ((unixtime - baseTime) / 60) % 60;
	baseTime += old_minutes * 60;
	baseTime -= new_minutes * 60;
}

void Rtc::setS(const unsigned new_seconds) {
	const std::uint32_t unixtime = (dataDh & 0x40) ? haltTime : timeCB();
	baseTime += (unixtime - baseTime) % 60;
	baseTime -= new_seconds;
}

SYNCFUNC(Rtc)
{
	EBS(activeData, 0);
	EVS(activeData, &dataS, 1);
	EVS(activeData, &dataM, 2);
	EVS(activeData, &dataH, 3);
	EVS(activeData, &dataDl, 4);
	EVS(activeData, &dataDh, 5);
	EES(activeData, NULL);

	EBS(activeSet, 0);
	EVS(activeSet, &Rtc::setS, 1);
	EVS(activeSet, &Rtc::setM, 2);
	EVS(activeSet, &Rtc::setH, 3);
	EVS(activeSet, &Rtc::setDl, 4);
	EVS(activeSet, &Rtc::setDh, 5);
	EES(activeSet, NULL);

	NSS(baseTime);
	NSS(haltTime);
	NSS(index);
	NSS(dataDh);
	NSS(dataDl);
	NSS(dataH);
	NSS(dataM);
	NSS(dataS);
	NSS(enabled);
	NSS(lastLatchData);
}

}
