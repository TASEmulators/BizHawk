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

#ifndef LY_COUNTER_H
#define LY_COUNTER_H

#include "newstate.h"
#include "lcddef.h"

namespace gambatte {

struct SaveState;

class LyCounter {
public:
	LyCounter();
	void doEvent();
	bool isDoubleSpeed() const { return ds_; }

	unsigned long frameCycles(unsigned long cc) const {
		return 1l * ly_ * lcd_cycles_per_line + lineCycles(cc);
	}

	unsigned lineCycles(unsigned long cc) const {
		return lcd_cycles_per_line - ((time_ - cc) >> isDoubleSpeed());
	}

	unsigned lineTime() const { return lineTime_; }
	unsigned ly() const { return ly_; }
	unsigned long nextLineCycle(unsigned lineCycle, unsigned long cycleCounter) const;
	unsigned long nextFrameCycle(unsigned long frameCycle, unsigned long cycleCounter) const;
	void reset(unsigned long videoCycles, unsigned long lastUpdate);
	void setDoubleSpeed(bool ds);
	unsigned long time() const { return time_; }

private:
	unsigned long time_;
	unsigned short lineTime_;
	unsigned char ly_;
	bool ds_;

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
