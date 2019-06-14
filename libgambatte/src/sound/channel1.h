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

#ifndef SOUND_CHANNEL1_H
#define SOUND_CHANNEL1_H

#include "duty_unit.h"
#include "envelope_unit.h"
#include "gbint.h"
#include "length_counter.h"
#include "master_disabler.h"
#include "static_output_tester.h"
#include "newstate.h"

namespace gambatte {

struct SaveState;

class Channel1 {
public:
	Channel1();
	void setNr0(unsigned data);
	void setNr1(unsigned data);
	void setNr2(unsigned data);
	void setNr3(unsigned data);
	void setNr4(unsigned data);
	void setSo(unsigned long soMask);
	bool isActive() const { return master_; }
	void update(uint_least32_t *buf, unsigned long soBaseVol, unsigned long cycles);
	void reset();
	void init(bool cgb);
	void loadState(SaveState const &state);

private:
	class SweepUnit : public SoundUnit {
	public:
		SweepUnit(MasterDisabler &disabler, DutyUnit &dutyUnit);
		virtual void event();
		void nr0Change(unsigned newNr0);
		void nr4Init(unsigned long cycleCounter);
		void reset();
		void init(bool cgb) { cgb_ = cgb; }
		void loadState(SaveState const &state);

	private:
		MasterDisabler &disableMaster_;
		DutyUnit &dutyUnit_;
		unsigned short shadow_;
		unsigned char nr0_;
		bool negging_;
		bool cgb_;

		unsigned calcFreq();

	public:
		template<bool isReader>void SyncState(NewState *ns);
	};

	friend class StaticOutputTester<Channel1, DutyUnit>;

	StaticOutputTester<Channel1, DutyUnit> staticOutputTest_;
	DutyMasterDisabler disableMaster_;
	LengthCounter lengthCounter_;
	DutyUnit dutyUnit_;
	EnvelopeUnit envelopeUnit_;
	SweepUnit sweepUnit_;
	SoundUnit *nextEventUnit_;
	unsigned long cycleCounter_;
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
