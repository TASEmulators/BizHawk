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

#include "channel3.h"
#include "psgdef.h"
#include "../savestate.h"
#include <algorithm>
#include <cstring>

using namespace gambatte;

namespace {
	unsigned toPeriod(unsigned nr3, unsigned nr4) {
		return 0x800 - ((nr4 << 8 & 0x700) | nr3);
	}
}

Channel3::Channel3()
: disableMaster_(master_, waveCounter_)
, lengthCounter_(disableMaster_, 0xFF)
, soMask_(0)
, prevOut_(0)
, waveCounter_(SoundUnit::counter_disabled)
, lastReadTime_(0)
, nr0_(0)
, nr3_(0)
, nr4_(0)
, wavePos_(0)
, rshift_(4)
, sampleBuf_(0)
, master_(false)
, cgb_(false)
{
}

void Channel3::setNr0(unsigned data) {
	nr0_ = data & psg_nr4_init;
	if (!nr0_)
		disableMaster_();
}

void Channel3::setNr2(unsigned data) {
	rshift_ = std::min((data >> 5 & 3) - 1, 4u);
}

void Channel3::setNr4(unsigned const data, unsigned long const cc) {
	lengthCounter_.nr4Change(nr4_, data, cc);
	nr4_ = data & ~(1u * psg_nr4_init);

	if (data & nr0_) {
		if (!cgb_ && waveCounter_ == cc + 1) {
			int const pos = (wavePos_ + 1) / 2 % sizeof waveRam_;

			if (pos < 4)
				waveRam_[0] = waveRam_[pos];
			else
				std::memcpy(waveRam_, waveRam_ + (pos & ~3), 4);
		}

		master_ = true;
		wavePos_ = 0;
		lastReadTime_ = waveCounter_ = cc + toPeriod(nr3_, data) + 3;
	}
}

void Channel3::setSo(unsigned long soMask) {
	soMask_ = soMask;
}

void Channel3::reset() {
	sampleBuf_ = 0;
}

void Channel3::resetCc(unsigned long cc, unsigned long newCc) {
	lastReadTime_ -= cc - newCc;
	if (waveCounter_ != SoundUnit::counter_disabled)
		waveCounter_ -= cc - newCc;
}

void Channel3::init(bool cgb) {
	cgb_ = cgb;
}

void Channel3::setStatePtrs(SaveState &state) {
	state.spu.ch3.waveRam.set(waveRam_, sizeof waveRam_);
}

void Channel3::loadState(SaveState const &state) {
	lengthCounter_.loadState(state.spu.ch3.lcounter, state.spu.cycleCounter);

	waveCounter_ = std::max(state.spu.ch3.waveCounter, state.spu.cycleCounter);
	lastReadTime_ = state.spu.ch3.lastReadTime;
	nr3_ = state.spu.ch3.nr3;
	nr4_ = state.spu.ch3.nr4;
	wavePos_ = state.spu.ch3.wavePos % (2 * sizeof waveRam_);
	sampleBuf_ = state.spu.ch3.sampleBuf;
	master_ = state.spu.ch3.master;

	nr0_ = state.mem.ioamhram.get()[0x11A] & psg_nr4_init;
	setNr2(state.mem.ioamhram.get()[0x11C]);
}

void Channel3::updateWaveCounter(unsigned long const cc) {
	if (cc >= waveCounter_) {
		unsigned const period = toPeriod(nr3_, nr4_);
		unsigned long const periods = (cc - waveCounter_) / period;

		lastReadTime_ = waveCounter_ + periods * period;
		waveCounter_ = lastReadTime_ + period;
		wavePos_ = (wavePos_ + periods + 1) % (2 * sizeof waveRam_);
		sampleBuf_ = waveRam_[wavePos_ / 2];
	}
}

void Channel3::update(uint_least32_t* buf, unsigned long const soBaseVol, unsigned long cc, unsigned long const end) {
	unsigned long const outBase = nr0_ ? soBaseVol & soMask_ : 0;

	if (outBase && rshift_ != 4) {
		while (std::min(waveCounter_, lengthCounter_.counter()) <= end) {
			unsigned pos = wavePos_;
			unsigned const period = toPeriod(nr3_, nr4_), rsh = rshift_;
			unsigned long const nextMajorEvent =
				std::min(lengthCounter_.counter(), end);
			unsigned long cnt = waveCounter_, prevOut = prevOut_;
			unsigned long out = master_
				? ((pos % 2 ? sampleBuf_ & 0xF : sampleBuf_ >> 4) >> rsh) * 2l - 15
				: -15;
			out *= outBase;
			while (cnt <= nextMajorEvent) {
				*buf += out - prevOut;
				prevOut = out;
				buf += cnt - cc;
				cc = cnt;
				cnt += period;
				++pos;
				unsigned const s = waveRam_[pos / 2 % sizeof waveRam_];
				out = ((pos % 2 ? s & 0xF : s >> 4) >> rsh) * 2l - 15;
				out *= outBase;
			}
			if (cnt != waveCounter_) {
				wavePos_ = pos % (2 * sizeof waveRam_);
				sampleBuf_ = waveRam_[wavePos_ / 2];
				prevOut_ = prevOut;
				waveCounter_ = cnt;
				lastReadTime_ = cc;
			}
			if (cc < nextMajorEvent) {
				*buf += out - prevOut_;
				prevOut_ = out;
				buf += nextMajorEvent - cc;
				cc = nextMajorEvent;
			}
			if (lengthCounter_.counter() == nextMajorEvent)
				lengthCounter_.event();
		}
		if (cc < end) {
			unsigned long out = master_
				? ((wavePos_ % 2 ? sampleBuf_ & 0xF : sampleBuf_ >> 4) >> rshift_) * 2l - 15
				: -15;
			out *= outBase;
			*buf += out - prevOut_;
			prevOut_ = out;
			cc = end;
		}
	}
	else {
		unsigned long const out = outBase * -15;
		*buf += out - prevOut_;
		prevOut_ = out;
		cc = end;
		while (lengthCounter_.counter() <= cc) {
			updateWaveCounter(lengthCounter_.counter());
			lengthCounter_.event();
		}

		updateWaveCounter(cc);
	}

	if (cc >= SoundUnit::counter_max) {
		lengthCounter_.resetCounters(cc);
		lastReadTime_ -= SoundUnit::counter_max;
		if (waveCounter_ != SoundUnit::counter_disabled)
			waveCounter_ -= SoundUnit::counter_max;
	}
}

SYNCFUNC(Channel3)
{
	NSS(waveRam_);

	SSS(lengthCounter_);

	NSS(soMask_);
	NSS(prevOut_);
	NSS(waveCounter_);
	NSS(lastReadTime_);

	NSS(nr0_);
	NSS(nr3_);
	NSS(nr4_);
	NSS(wavePos_);
	NSS(rshift_);
	NSS(sampleBuf_);

	NSS(master_);
	NSS(cgb_);
}
