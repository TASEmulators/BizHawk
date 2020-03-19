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

#ifndef SPRITE_MAPPER_H
#define SPRITE_MAPPER_H

#include "ly_counter.h"
#include "../savestate.h"
#include "newstate.h"

namespace gambatte {

class NextM0Time;

class SpriteMapper {
public:
	SpriteMapper(NextM0Time &nextM0Time,
	             LyCounter const &lyCounter,
	             unsigned char const *oamram);
	void reset(unsigned char const *oamram, bool cgb);
	unsigned long doEvent(unsigned long time);
	bool largeSprites(int spno) const { return oamReader_.largeSprites(spno); }
	int numSprites(unsigned ly) const { return num_[ly] & ~(1u * need_sorting_flag); }
	void oamChange(unsigned long cc) { oamReader_.change(cc); }
	void oamChange(unsigned char const *oamram, unsigned long cc) { oamReader_.change(oamram, cc); }
	unsigned char const * oamram() const { return oamReader_.oam(); }
	unsigned char const * posbuf() const { return oamReader_.spritePosBuf(); }

	void resetCycleCounter(unsigned long oldCc, unsigned long newCc) {
		oamReader_.update(oldCc);
		oamReader_.resetCycleCounter(oldCc, newCc);
	}

	void setLargeSpritesSource(bool src) { oamReader_.setLargeSpritesSrc(src); }

	unsigned char const* sprites(unsigned ly) const {
		if (num_[ly] & need_sorting_flag)
			sortLine(ly);

		return spritemap_[ly];
	}

	void setStatePtrs(SaveState &state) { oamReader_.setStatePtrs(state); }
	void enableDisplay(unsigned long cc) { oamReader_.enableDisplay(cc); }

	void loadState(SaveState const &state, unsigned char const *oamram) {
		oamReader_.loadState(state, oamram);
		mapSprites();
	}

	bool inactivePeriodAfterDisplayEnable(unsigned long cc) const {
		return oamReader_.inactivePeriodAfterDisplayEnable(cc);
	}

	static unsigned long schedule(LyCounter const& lyCounter, unsigned long cc) {
		return lyCounter.nextLineCycle(2 * lcd_num_oam_entries, cc);
	}

private:
	class OamReader {
	public:
		OamReader(LyCounter const &lyCounter, unsigned char const *oamram);
		void reset(unsigned char const *oamram, bool cgb);
		void change(unsigned long cc);
		void change(unsigned char const *oamram, unsigned long cc) { change(cc); oamram_ = oamram; }
		bool changed() const { return lastChange_ != 0xFF; }
		bool largeSprites(int spNo) const { return lsbuf_[spNo]; }
		unsigned char const * oam() const { return oamram_; }
		void resetCycleCounter(unsigned long oldCc, unsigned long newCc) { lu_ -= oldCc - newCc; }
		void setLargeSpritesSrc(bool src) { largeSpritesSrc_ = src; }
		void update(unsigned long cc);
		unsigned char const * spritePosBuf() const { return buf_; }
		void setStatePtrs(SaveState &state);
		void enableDisplay(unsigned long cc);
		void loadState(SaveState const &ss, unsigned char const *oamram);
		bool inactivePeriodAfterDisplayEnable(unsigned long cc) const { return cc < lu_; }
		unsigned lineTime() const { return lyCounter_.lineTime(); }

	private:
		unsigned char buf_[2 * lcd_num_oam_entries];
		bool lsbuf_[lcd_num_oam_entries];
		LyCounter const &lyCounter_;
		unsigned char const *oamram_;
		unsigned long lu_;
		unsigned char lastChange_;
		bool largeSpritesSrc_;
		bool cgb_;

	public:
		template<bool isReader>void SyncState(NewState *ns);
	};

	enum { need_sorting_flag = 0x80 };

	mutable unsigned char spritemap_[lcd_vres][lcd_max_num_sprites_per_line];
	mutable unsigned char num_[lcd_vres];
	NextM0Time &nextM0Time_;
	OamReader oamReader_;

	void clearMap();
	void mapSprites();
	void sortLine(unsigned ly) const;

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
