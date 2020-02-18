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

#ifndef SOUND_CHANNEL4_H
#define SOUND_CHANNEL4_H

#include "envelope_unit.h"
#include "gbint.h"
#include "length_counter.h"
#include "master_disabler.h"
#include "static_output_tester.h"
#include "newstate.h"

namespace gambatte {

struct SaveState;

class Channel4 {
public:
	Channel4();
	void setNr1(unsigned data, unsigned long cc);
	void setNr2(unsigned data, unsigned long cc);
	void setNr3(unsigned data, unsigned long cc) { lfsr_.nr3Change(data, cc); }
	void setNr4(unsigned data, unsigned long cc);
	void setSo(unsigned long soMask, unsigned long cc);
	bool isActive() const { return master_; }
	void update(uint_least32_t* buf, unsigned long soBaseVol, unsigned long cc, unsigned long end);
	void reset(unsigned long cc);
	void resetCc(unsigned long cc, unsigned long newCc) { lfsr_.resetCc(cc, newCc); }
	void loadState(SaveState const &state);

private:
	class Lfsr : public SoundUnit {
	public:
		Lfsr();
		virtual void event();
		virtual void resetCounters(unsigned long oldCc);
		bool isHighState() const { return ~reg_ & 1; }
		void nr3Change(unsigned newNr3, unsigned long cc);
		void nr4Init(unsigned long cc);
		void reset(unsigned long cc);
		void resetCc(unsigned long cc, unsigned long newCc);
		void loadState(SaveState const &state);
		void disableMaster() { killCounter(); master_ = false; reg_ = 0x7FFF; }
		void killCounter() { counter_ = counter_disabled; }
		void reviveCounter(unsigned long cc);

	private:
		unsigned long backupCounter_;
		unsigned short reg_;
		unsigned char nr3_;
		bool master_;

		void updateBackupCounter(unsigned long cc);

	public:
		template<bool isReader>void SyncState(NewState *ns);
	};

	class Ch4MasterDisabler : public MasterDisabler {
	public:
		Ch4MasterDisabler(bool &m, Lfsr &lfsr) : MasterDisabler(m), lfsr_(lfsr) {}
		virtual void operator()() { MasterDisabler::operator()(); lfsr_.disableMaster(); }

	private:
		Lfsr &lfsr_;
	};

	friend class StaticOutputTester<Channel4, Lfsr>;

	StaticOutputTester<Channel4, Lfsr> staticOutputTest_;
	Ch4MasterDisabler disableMaster_;
	LengthCounter lengthCounter_;
	EnvelopeUnit envelopeUnit_;
	Lfsr lfsr_;
	SoundUnit *nextEventUnit_;
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
