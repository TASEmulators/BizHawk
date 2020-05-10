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

#include "channel4.h"
#include "psgdef.h"
#include "../savestate.h"
#include <algorithm>

using namespace gambatte;

namespace {
	static unsigned long toPeriod(unsigned const nr3) {
		unsigned s = nr3 / (1u * psg_nr43_s & -psg_nr43_s) + 3;
		unsigned r = nr3 & psg_nr43_r;

		if (!r) {
			r = 1;
			--s;
		}

		return r << s;
	}
}

Channel4::Lfsr::Lfsr()
: backupCounter_(counter_disabled)
, reg_(0x7FFF)
, nr3_(0)
, master_(false)
{
}

void Channel4::Lfsr::updateBackupCounter(unsigned long const cc) {
	if (backupCounter_ <= cc) {
		unsigned long const period = toPeriod(nr3_);
		unsigned long periods = (cc - backupCounter_) / period + 1;
		backupCounter_ += periods * period;

		if (master_ && nr3_ < 0xE * (1u * psg_nr43_s & -psg_nr43_s)) {
			if (nr3_ & psg_nr43_7biten) {
				while (periods > 6) {
					unsigned const xored = (reg_ << 1 ^ reg_) & 0x7E;
					reg_ = (reg_ >> 6 & ~0x7Eu) | xored | xored << 8;
					periods -= 6;
				}

				unsigned const xored = ((reg_ ^ reg_ >> 1) << (7 - periods)) & 0x7F;
				reg_ = (reg_ >> periods & ~(0x80u - (0x80 >> periods))) | xored | xored << 8;
			}
			else {
				while (periods > 15) {
					reg_ = reg_ ^ reg_ >> 1;
					periods -= 15;
				}

				reg_ = reg_ >> periods | (((reg_ ^ reg_ >> 1) << (15 - periods)) & 0x7FFF);
			}
		}
	}
}

void Channel4::Lfsr::reviveCounter(unsigned long cc) {
	updateBackupCounter(cc);
	counter_ = backupCounter_;
}

inline void Channel4::Lfsr::event() {
	if (nr3_ < 0xE * (1u * psg_nr43_s & -psg_nr43_s)) {
		unsigned const shifted = reg_ >> 1;
		unsigned const xored = (reg_ ^ shifted) & 1;
		reg_ = shifted | xored << 14;

		if (nr3_ & psg_nr43_7biten)
			reg_ = (reg_ & ~0x40u) | xored << 6;
	}

	counter_ += toPeriod(nr3_);
	backupCounter_ = counter_;
}

void Channel4::Lfsr::nr3Change(unsigned newNr3, unsigned long cc) {
	updateBackupCounter(cc);
	nr3_ = newNr3;
	counter_ = cc;
}

void Channel4::Lfsr::nr4Init(unsigned long cc) {
	disableMaster();
	updateBackupCounter(cc);
	master_ = true;
	backupCounter_ += 4;
	counter_ = backupCounter_;
}

void Channel4::Lfsr::reset(unsigned long cc) {
	nr3_ = 0;
	disableMaster();
	backupCounter_ = cc + toPeriod(nr3_);
}

void Channel4::Lfsr::resetCc(unsigned long cc, unsigned long newCc) {
	updateBackupCounter(cc);
	backupCounter_ -= cc - newCc;
	if (counter_ != counter_disabled)
		counter_ -= cc - newCc;
}

void Channel4::Lfsr::resetCounters(unsigned long oldCc) {
	updateBackupCounter(oldCc);
	backupCounter_ -= counter_max;
	SoundUnit::resetCounters(oldCc);
}

void Channel4::Lfsr::loadState(SaveState const &state) {
	counter_ = backupCounter_ = std::max(state.spu.ch4.lfsr.counter, state.spu.cycleCounter);
	reg_ = state.spu.ch4.lfsr.reg;
	master_ = state.spu.ch4.master;
	nr3_ = state.mem.ioamhram.get()[0x122];
}

template<bool isReader>
void Channel4::Lfsr::SyncState(NewState *ns)
{
	NSS(counter_);
	NSS(backupCounter_);
	NSS(reg_);
	NSS(nr3_);
	NSS(master_);
}

Channel4::Channel4()
: staticOutputTest_(*this, lfsr_)
, disableMaster_(master_, lfsr_)
, lengthCounter_(disableMaster_, 0x3F)
, envelopeUnit_(staticOutputTest_)
, nextEventUnit_(0)
, soMask_(0)
, prevOut_(0)
, nr4_(0)
, master_(false)
{
	setEvent();
}

void Channel4::setEvent() {
	nextEventUnit_ = &envelopeUnit_;
	if (lengthCounter_.counter() < nextEventUnit_->counter())
		nextEventUnit_ = &lengthCounter_;
}

void Channel4::setNr1(unsigned data, unsigned long cc) {
	lengthCounter_.nr1Change(data, nr4_, cc);
	setEvent();
}

void Channel4::setNr2(unsigned data, unsigned long cc) {
	if (envelopeUnit_.nr2Change(data))
		disableMaster_();
	else
		staticOutputTest_(cc);

	setEvent();
}

void Channel4::setNr4(unsigned const data, unsigned long const cc) {
	lengthCounter_.nr4Change(nr4_, data, cc);
	nr4_ = data;

	if (nr4_ & psg_nr4_init) {
		nr4_ -= psg_nr4_init;
		master_ = !envelopeUnit_.nr4Init(cc);
		if (master_)
			lfsr_.nr4Init(cc);

		staticOutputTest_(cc);
	}

	setEvent();
}

void Channel4::setSo(unsigned long soMask, unsigned long cc) {
	soMask_ = soMask;
	staticOutputTest_(cc);
	setEvent();
}

void Channel4::reset(unsigned long cc) {
	lfsr_.reset(cc);
	envelopeUnit_.reset();
	setEvent();
}

void Channel4::loadState(SaveState const &state) {
	lfsr_.loadState(state);
	envelopeUnit_.loadState(state.spu.ch4.env, state.mem.ioamhram.get()[0x121],
		state.spu.cycleCounter);
	lengthCounter_.loadState(state.spu.ch4.lcounter, state.spu.cycleCounter);

	nr4_ = state.spu.ch4.nr4;
	master_ = state.spu.ch4.master;
}

void Channel4::update(uint_least32_t* buf, unsigned long const soBaseVol, unsigned long cc, unsigned long const end) {
	unsigned long const outBase = envelopeUnit_.dacIsOn() ? soBaseVol & soMask_ : 0;
	unsigned long const outLow = outBase * -15;

	while (cc < end) {
		unsigned long const outHigh = outBase * (envelopeUnit_.getVolume() * 2l - 15);
		unsigned long const nextMajorEvent = std::min(nextEventUnit_->counter(), end);
		unsigned long out = lfsr_.isHighState() ? outHigh : outLow;
		if (lfsr_.counter() <= nextMajorEvent) {
			Lfsr lfsr = lfsr_;
			while (lfsr.counter() <= nextMajorEvent) {
				*buf += out - prevOut_;
				prevOut_ = out;
				buf += lfsr.counter() - cc;
				cc = lfsr.counter();
				lfsr.event();
				out = lfsr.isHighState() ? outHigh : outLow;
			}
			lfsr_ = lfsr;
		}
		if (cc < nextMajorEvent) {
			*buf += out - prevOut_;
			prevOut_ = out;
			buf += nextMajorEvent - cc;
			cc = nextMajorEvent;
		}
		if (nextEventUnit_->counter() == nextMajorEvent) {
			nextEventUnit_->event();
			setEvent();
		}
	}

	if (cc >= SoundUnit::counter_max) {
		lengthCounter_.resetCounters(cc);
		lfsr_.resetCounters(cc);
		envelopeUnit_.resetCounters(cc);
	}
}

SYNCFUNC(Channel4)
{
	SSS(lengthCounter_);
	SSS(envelopeUnit_);
	SSS(lfsr_);

	EBS(nextEventUnit_, 0);
	EVS(nextEventUnit_, &lfsr_, 1);
	EVS(nextEventUnit_, &envelopeUnit_, 2);
	EVS(nextEventUnit_, &lengthCounter_, 3);
	EES(nextEventUnit_, NULL);

	NSS(soMask_);
	NSS(prevOut_);

	NSS(nr4_);
	NSS(master_);
}
