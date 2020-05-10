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

#include "sprite_mapper.h"
#include "counterdef.h"
#include "next_m0_time.h"
#include "../insertion_sort.h"
#include <algorithm>

using namespace gambatte;

namespace {

class SpxLess {
public:
	explicit SpxLess(unsigned char const *spxlut) : spxlut_(spxlut) {}

	bool operator()(unsigned char lhs, unsigned char rhs) const {
		return spxlut_[lhs] < spxlut_[rhs];
	}

private:
	unsigned char const *const spxlut_;
};

unsigned toPosCycles(unsigned long const cc, LyCounter const& lyCounter) {
	unsigned lc = lyCounter.lineCycles(cc) + 1;
	if (lc >= lcd_cycles_per_line)
		lc -= lcd_cycles_per_line;

	return lc;
}

}

SpriteMapper::OamReader::OamReader(LyCounter const &lyCounter, unsigned char const *oamram)
: lyCounter_(lyCounter)
, oamram_(oamram)
, cgb_(false)
{
	reset(oamram, false);
}

void SpriteMapper::OamReader::reset(unsigned char const* const oamram, bool const cgb) {
	oamram_ = oamram;
	cgb_ = cgb;
	setLargeSpritesSrc(false);
	lu_ = 0;
	lastChange_ = 0xFF;
	std::fill_n(lsbuf_, sizeof lsbuf_ / sizeof * lsbuf_, largeSpritesSrc_);
	for (int i = 0; i < lcd_num_oam_entries; ++i) {
		buf_[2 * i] = oamram[4 * i];
		buf_[2 * i + 1] = oamram[4 * i + 1];
	}
}

void SpriteMapper::OamReader::update(unsigned long const cc) {
	if (cc > lu_) {
		if (changed()) {
			unsigned const lulc = toPosCycles(lu_, lyCounter_);
			unsigned pos = std::min(lulc, 2u * lcd_num_oam_entries);
			unsigned distance = 2 * lcd_num_oam_entries;

			if ((cc - lu_) >> lyCounter_.isDoubleSpeed() < lcd_cycles_per_line) {
				unsigned cclc = toPosCycles(cc, lyCounter_);
				distance = std::min(cclc, 2u * lcd_num_oam_entries)
					- pos + (cclc < lulc ? 2 * lcd_num_oam_entries : 0);
			}

			{
				unsigned targetDistance =
					lastChange_ - pos + (lastChange_ <= pos ? 2 * lcd_num_oam_entries : 0);
				if (targetDistance <= distance) {
					distance = targetDistance;
					lastChange_ = 0xFF;
				}
			}

			while (distance--) {
				if (!(pos & 1)) {
					if (pos == 2 * lcd_num_oam_entries)
						pos = 0;
					if (cgb_)
						lsbuf_[pos / 2] = largeSpritesSrc_;

					buf_[pos] = oamram_[2 * pos];
					buf_[pos + 1] = oamram_[2 * pos + 1];
				}
				else
					lsbuf_[pos / 2] = (lsbuf_[pos / 2] & cgb_) | largeSpritesSrc_;

				++pos;
			}
		}

		lu_ = cc;
	}
}

void SpriteMapper::OamReader::change(unsigned long cc) {
	update(cc);
	lastChange_ = std::min(toPosCycles(lu_, lyCounter_), 2u * lcd_num_oam_entries);
}

void SpriteMapper::OamReader::setStatePtrs(SaveState& state) {
	state.ppu.oamReaderBuf.set(buf_, sizeof buf_ / sizeof * buf_);
	state.ppu.oamReaderSzbuf.set(lsbuf_, sizeof lsbuf_ / sizeof * lsbuf_);
}

void SpriteMapper::OamReader::loadState(SaveState const& ss, unsigned char const* const oamram) {
	oamram_ = oamram;
	largeSpritesSrc_ = ss.mem.ioamhram.get()[0x140] >> 2 & 1;
	lu_ = ss.ppu.enableDisplayM0Time;
	change(lu_);
}

SYNCFUNC(SpriteMapper::OamReader)
{
	NSS(buf_);
	NSS(lsbuf_);

	NSS(lu_);
	NSS(lastChange_);
	NSS(largeSpritesSrc_);
	NSS(cgb_);
}

void SpriteMapper::OamReader::enableDisplay(unsigned long cc) {
	std::fill_n(buf_, sizeof buf_ / sizeof * buf_, 0);
	std::fill_n(lsbuf_, sizeof lsbuf_ / sizeof * lsbuf_, false);
	lu_ = cc + (2 * lcd_num_oam_entries << lyCounter_.isDoubleSpeed()) + 1;
	lastChange_ = 2 * lcd_num_oam_entries;
}

SpriteMapper::SpriteMapper(NextM0Time &nextM0Time,
                           LyCounter const &lyCounter,
                           unsigned char const *oamram)
: nextM0Time_(nextM0Time)
, oamReader_(lyCounter, oamram)
{
	clearMap();
}

void SpriteMapper::reset(unsigned char const *oamram, bool cgb) {
	oamReader_.reset(oamram, cgb);
	clearMap();
}

void SpriteMapper::clearMap() {
	std::fill_n(num_, sizeof num_ / sizeof * num_, 1 * need_sorting_flag);
}

void SpriteMapper::mapSprites() {
	clearMap();

	for (int i = 0; i < lcd_num_oam_entries; ++i) {
		int const spriteHeight = 8 + 8 * largeSprites(i);
		unsigned const bottomPos = posbuf()[2 * i] - 17 + spriteHeight;

		if (bottomPos < lcd_vres - 1u + spriteHeight) {
			int ly = std::max(static_cast<int>(bottomPos) + 1 - spriteHeight, 0);
			int const end = std::min(bottomPos, lcd_vres - 1u) + 1;

			do {
				if (num_[ly] < need_sorting_flag + lcd_max_num_sprites_per_line)
					spritemap_[ly][num_[ly]++ - need_sorting_flag] = 2 * i;
			} while (++ly != end);
		}
	}

	nextM0Time_.invalidatePredictedNextM0Time();
}

void SpriteMapper::sortLine(unsigned const ly) const {
	num_[ly] &= ~(1u * need_sorting_flag);
	insertionSort(spritemap_[ly], spritemap_[ly] + num_[ly],
		SpxLess(posbuf() + 1));
}

unsigned long SpriteMapper::doEvent(unsigned long const time) {
	oamReader_.update(time);
	mapSprites();
	return oamReader_.changed()
		? time + oamReader_.lineTime()
		: static_cast<unsigned long>(disabled_time);
}

SYNCFUNC(SpriteMapper)
{
	NSS(spritemap_);
	NSS(num_);

	SSS(oamReader_);
}
