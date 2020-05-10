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

#ifndef SOUND_CHANNEL2_H
#define SOUND_CHANNEL2_H

#include "duty_unit.h"
#include "envelope_unit.h"
#include "gbint.h"
#include "length_counter.h"
#include "static_output_tester.h"
#include "newstate.h"

namespace gambatte {

struct SaveState;

class Channel2 {
public:
	Channel2();
	void setNr1(unsigned data, unsigned long cc);
	void setNr2(unsigned data, unsigned long cc);
	void setNr3(unsigned data, unsigned long cc);
	void setNr4(unsigned data, unsigned long cc, unsigned long ref);
	void setSo(unsigned long soMask, unsigned long cc);
	bool isActive() const { return master_; }
	void update(uint_least32_t* buf, unsigned long soBaseVol, unsigned long cc, unsigned long end);
	void reset();
	void resetCc(unsigned long cc, unsigned long ncc) { dutyUnit_.resetCc(cc, ncc); }
	void loadState(SaveState const &state);

private:
	friend class StaticOutputTester<Channel2, DutyUnit>;

	StaticOutputTester<Channel2, DutyUnit> staticOutputTest_;
	DutyMasterDisabler disableMaster_;
	LengthCounter lengthCounter_;
	DutyUnit dutyUnit_;
	EnvelopeUnit envelopeUnit_;
	SoundUnit *nextEventUnit;
	unsigned long soMask_;
	unsigned long prevOut_;
	unsigned char nr4_;
	bool master_;

	void setEvent();

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
