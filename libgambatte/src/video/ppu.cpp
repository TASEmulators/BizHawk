//
//   Copyright (C) 2010 by sinamas <sinamas at users.sourceforge.net>
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

#include "ppu.h"
#include "savestate.h"
#include <algorithm>
#include <cstddef>

#include <cstring>

using namespace gambatte;

namespace {

#define PREP(u8) (((u8) << 7 & 0x80) | ((u8) << 5 & 0x40) | ((u8) << 3 & 0x20) | ((u8) << 1 & 0x10) \
                | ((u8) >> 1 & 0x08) | ((u8) >> 3 & 0x04) | ((u8) >> 5 & 0x02) | ((u8) >> 7 & 0x01))

#define EXPAND(u8) ((PREP(u8) << 7 & 0x4000) | (PREP(u8) << 6 & 0x1000) \
                  | (PREP(u8) << 5 & 0x0400) | (PREP(u8) << 4 & 0x0100) \
                  | (PREP(u8) << 3 & 0x0040) | (PREP(u8) << 2 & 0x0010) \
                  | (PREP(u8) << 1 & 0x0004) | (PREP(u8)      & 0x0001))

#define EXPAND_ROW(n) EXPAND((n)|0x0), EXPAND((n)|0x1), EXPAND((n)|0x2), EXPAND((n)|0x3), \
                      EXPAND((n)|0x4), EXPAND((n)|0x5), EXPAND((n)|0x6), EXPAND((n)|0x7), \
                      EXPAND((n)|0x8), EXPAND((n)|0x9), EXPAND((n)|0xA), EXPAND((n)|0xB), \
                      EXPAND((n)|0xC), EXPAND((n)|0xD), EXPAND((n)|0xE), EXPAND((n)|0xF)

#define EXPAND_TABLE EXPAND_ROW(0x00), EXPAND_ROW(0x10), EXPAND_ROW(0x20), EXPAND_ROW(0x30), \
                     EXPAND_ROW(0x40), EXPAND_ROW(0x50), EXPAND_ROW(0x60), EXPAND_ROW(0x70), \
                     EXPAND_ROW(0x80), EXPAND_ROW(0x90), EXPAND_ROW(0xA0), EXPAND_ROW(0xB0), \
                     EXPAND_ROW(0xC0), EXPAND_ROW(0xD0), EXPAND_ROW(0xE0), EXPAND_ROW(0xF0)

unsigned short const expand_lut[0x200] = {
	EXPAND_TABLE,

#undef PREP
#define PREP(u8) (u8)

	EXPAND_TABLE
};

#undef EXPAND_TABLE
#undef EXPAND_ROW
#undef EXPAND
#undef PREP

#define DECLARE_FUNC(n, id) \
	enum { ID##n = id }; \
	void f##n (PPUPriv &); \
	unsigned predictCyclesUntilXpos_f##n (PPUPriv const &, int targetxpos, unsigned cycles); \
	PPUState const f##n##_ = { f##n, predictCyclesUntilXpos_f##n, ID##n }

namespace M2_Ly0    { DECLARE_FUNC(0, 0); }
namespace M2_LyNon0 { DECLARE_FUNC(0, 0); DECLARE_FUNC(1, 0); }
namespace M3Start { DECLARE_FUNC(0, 0); DECLARE_FUNC(1, 0); }
namespace M3Loop {
namespace Tile {
	DECLARE_FUNC(0, 0x80);
	DECLARE_FUNC(1, 0x81);
	DECLARE_FUNC(2, 0x82);
	DECLARE_FUNC(3, 0x83);
	DECLARE_FUNC(4, 0x84);
	DECLARE_FUNC(5, 0x85);
}
namespace LoadSprites {
	DECLARE_FUNC(0, 0x88);
	DECLARE_FUNC(1, 0x89);
	DECLARE_FUNC(2, 0x8A);
	DECLARE_FUNC(3, 0x8B);
	DECLARE_FUNC(4, 0x8C);
	DECLARE_FUNC(5, 0x8D);
}
namespace StartWindowDraw {
	DECLARE_FUNC(0, 0x90);
	DECLARE_FUNC(1, 0x91);
	DECLARE_FUNC(2, 0x92);
	DECLARE_FUNC(3, 0x93);
	DECLARE_FUNC(4, 0x94);
	DECLARE_FUNC(5, 0x95);
}
} // namespace M3Loop

#undef DECLARE_FUNC

enum {
	attr_cgbpalno = 0x07, attr_tdbank = 0x08, attr_dmgpalno = 0x10, attr_xflip = 0x20,
	attr_yflip = 0x40, attr_bgpriority = 0x80
};
enum { win_draw_start = 1, win_draw_started = 2 };

int const max_m3start_cycles = 80;
int const tile_bpp = 2;
int const tile_bpp_mask = (1 << tile_bpp) - 1;
int const tile_len = 8;
int const tile_line_size = tile_bpp * tile_len / 8;
int const tile_size = tile_line_size * tile_len;
int const tile_map_begin = 0x1800;
int const tile_map_len = 0x20;
int const tile_map_size = tile_map_len * tile_map_len;
int const tile_pattern_table_size = 0x1000;
int const vram_bank_size = 0x2000;
int const xpos_end = 168;

inline int spx(PPUPriv::Sprite const& s) { return s.spx; }

inline int lcdcEn(PPUPriv const& p) { return p.lcdc & lcdc_en; }
inline int lcdcWinEn(PPUPriv const& p) { return p.lcdc & lcdc_we; }
inline int lcdcObj2x(PPUPriv const& p) { return p.lcdc & lcdc_obj2x; }
inline int lcdcObjEn(PPUPriv const& p) { return p.lcdc & lcdc_objen; }
inline int lcdcBgEn(PPUPriv const& p) { return p.lcdc & lcdc_bgen; }

inline int weMasterCheckLy0LineCycle(bool cgb) { return 1 + cgb; }
inline int weMasterCheckPriorToLyIncLineCycle(bool /*cgb*/) { return 450; }
inline int weMasterCheckAfterLyIncLineCycle(bool /*cgb*/) { return 454; }
inline int m3StartLineCycle(bool cgb) { return 83 + cgb; }

inline void nextCall(int const cycles, PPUState const &state, PPUPriv &p) {
	int const c = p.cycles - cycles;
	if (c >= 0) {
		p.cycles = c;
		return state.f(p);
	}

	p.cycles = c;
	p.nextCallPtr = &state;
}

inline unsigned long const* cgbSpPalette(PPUPriv const& p, unsigned const attrib) {
	if (!p.cgbDmg)
		return p.spPalette + (attrib & attr_cgbpalno) * num_palette_entries;
	else
		return p.spPalette + (attrib & attr_dmgpalno ? num_palette_entries : 0);
}

namespace M2_Ly0 {
	void f0(PPUPriv& p) {
		p.weMaster = lcdcWinEn(p) && 0 == p.wy;
		p.winYPos = 0xFF;
		nextCall(m3StartLineCycle(p.cgb) - weMasterCheckLy0LineCycle(p.cgb), M3Start::f0_, p);
	}
}

namespace M2_LyNon0 {
	void f0(PPUPriv& p) {
		p.weMaster |= lcdcWinEn(p) && p.lyCounter.ly() == p.wy;
		nextCall(weMasterCheckAfterLyIncLineCycle(p.cgb)
			- weMasterCheckPriorToLyIncLineCycle(p.cgb), f1_, p);
	}

	void f1(PPUPriv& p) {
		p.weMaster |= lcdcWinEn(p) && p.lyCounter.ly() + 1 == p.wy;
		nextCall(lcd_cycles_per_line - weMasterCheckAfterLyIncLineCycle(p.cgb) + m3StartLineCycle(p.cgb),
			M3Start::f0_, p);
	}
}

/*
namespace M2 {
	struct SpriteLess {
		bool operator()(Sprite lhs, Sprite rhs) const {
			return lhs.spx < rhs.spx;
		}
	};

	void f0(PPUPriv &p) {
		std::memset(&p.spLut, 0, sizeof p.spLut);
		p.reg0 = 0;
		p.nextSprite = 0;
		p.nextCallPtr = &f1_;
		f1(p);
	}

	void f1(PPUPriv &p) {
		int const oam_entry_size = 4;
		int const oam_size = oam_entry_size * lcd_num_oam_entries;
		int cycles = p.cycles;
		unsigned oampos = p.reg0;
		unsigned nextSprite = p.nextSprite;
		unsigned const nly = (p.lyCounter.ly() + 1 == lcd_lines_per_frame ? 0 : p.lyCounter.ly() + 1)
			+ (p.lyCounter.time() - (p.now - p.cycles) <= 4);
		bool const ls = p.spriteMapper.largeSpritesSource();

		do {
			unsigned const spy = p.spriteMapper.oamram()[oampos];
			unsigned const spx = p.spriteMapper.oamram()[oampos + 1];
			unsigned const ydiff = spy - nly;

			if (ls ? ydiff < 2u * tile_len : ydiff - 1u * tile_len < 1u * tile_len) {
				p.spriteList[nextSprite].spx = spx;
				p.spriteList[nextSprite].line = 2 * tile_len - 1 - ydiff;
				p.spriteList[nextSprite].oampos = oampos;

				if (++nextSprite == lcd_max_num_sprites_per_line) {
					cycles -= (oam_size - oam_entry_size - oampos) >> 1;
					oampos = oam_size - oam_entry_size;
				}
			}

			oampos += oam_entry_size;
		} while ((cycles -= 2) >= 0 && oampos != oam_size);

		p.reg0 = oampos;
		p.nextSprite = nextSprite;
		p.cycles = cycles;

		if (oampos == oam_size) {
			insertionSort(p.spriteList, p.spriteList + nextSprite, SpriteLess());
			p.spriteList[nextSprite].spx = 0xFF;
			p.nextSprite = 0;
			nextCall(0, M3Start::f0_, p);
		}
	}
}
*/

int loadTileDataByte0(PPUPriv const& p) {
	unsigned const yoffset = p.winDrawState & win_draw_started
		? p.winYPos
		: p.scy + p.lyCounter.ly();

	return p.vram[tile_pattern_table_size
		+ vram_bank_size / attr_tdbank * (p.nattrib & attr_tdbank)
		- ((2 * tile_size * p.reg1 | tile_pattern_table_size / lcdc_tdsel * p.lcdc)
			& tile_pattern_table_size)
		+ p.reg1 * tile_size
		+ ((p.nattrib & attr_yflip ? -1 : 0) ^ yoffset) % tile_len * tile_line_size];
}

int loadTileDataByte1(PPUPriv const& p) {
	unsigned const yoffset = p.winDrawState & win_draw_started
		? p.winYPos
		: p.scy + p.lyCounter.ly();

	return p.vram[tile_pattern_table_size
		+ vram_bank_size / attr_tdbank * (p.nattrib & attr_tdbank)
		- ((2 * tile_size * p.reg1 | tile_pattern_table_size / lcdc_tdsel * p.lcdc)
			& tile_pattern_table_size)
		+ p.reg1 * tile_size
		+ ((p.nattrib & attr_yflip ? -1 : 0) ^ yoffset) % tile_len * tile_line_size + 1];
}

namespace M3Start {
	void f0(PPUPriv& p) {
		p.xpos = 0;

		if ((p.winDrawState & win_draw_start) && lcdcWinEn(p)) {
			p.winDrawState = win_draw_started;
			p.wscx = tile_len + p.scx % tile_len;
			++p.winYPos;
		}
		else
			p.winDrawState = 0;

		p.nextCallPtr = &f1_;
		f1(p);
	}

	void f1(PPUPriv& p) {
		while (p.xpos < max_m3start_cycles) {
			if (p.xpos % tile_len == p.scx % tile_len)
				break;

			switch (p.xpos % tile_len) {
			case 0:
				if (p.winDrawState & win_draw_started) {
					p.reg1 = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
						+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
						+ p.wscx / tile_len % tile_map_len
						+ tile_map_begin];
					p.nattrib = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
						+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
						+ p.wscx / tile_len % tile_map_len
						+ tile_map_begin + vram_bank_size];
				}
				else {
					p.reg1 = p.vram[((tile_map_size / lcdc_bgtmsel * p.lcdc | p.scx / tile_len)
						& (tile_map_size + tile_map_len - 1))
						+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
						+ tile_map_begin];
					p.nattrib = p.vram[((tile_map_size / lcdc_bgtmsel * p.lcdc | p.scx / tile_len)
						& (tile_map_size + tile_map_len - 1))
						+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
						+ tile_map_begin + vram_bank_size];
				}

				break;
			case 2:
				p.reg0 = loadTileDataByte0(p);
				break;
			case 4:
			{
				int const r1 = loadTileDataByte1(p);
				p.ntileword = (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[p.reg0]
					+ (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[r1] * 2;
			}

			break;
			}

			++p.xpos;

			if (--p.cycles < 0)
				return;
		}

		{
			int const ly = p.lyCounter.ly();
			int const numSprites = p.spriteMapper.numSprites(ly);
			unsigned char const* const sprites = p.spriteMapper.sprites(ly);
			for (int i = 0; i < numSprites; ++i) {
				int const pos = sprites[i];
				int const spy = p.spriteMapper.posbuf()[pos];
				int const spx = p.spriteMapper.posbuf()[pos + 1];

				p.spriteList[i].spx = spx;
				p.spriteList[i].line = ly + 2 * tile_len - spy;
				p.spriteList[i].oampos = pos * 2;
				p.spwordList[i] = 0;
			}

			p.spriteList[numSprites].spx = 0xFF;
			p.nextSprite = 0;
		}

		p.xpos = 0;
		p.endx = tile_len - p.scx % tile_len;

		static PPUState const* const flut[] = {
			&M3Loop::Tile::f0_,
			&M3Loop::Tile::f1_,
			&M3Loop::Tile::f2_,
			&M3Loop::Tile::f3_,
			&M3Loop::Tile::f4_,
			&M3Loop::Tile::f5_,
			&M3Loop::Tile::f5_,
			&M3Loop::Tile::f5_
		};

		nextCall(1 - p.cgb, *flut[p.scx % tile_len], p);
	}
}

namespace M3Loop {

	void doFullTilesUnrolledDmg(PPUPriv& p, int const xend, uint_least32_t* const dbufline,
		unsigned char const* const tileMapLine, unsigned const tileline, unsigned tileMapXpos) {
		int const tileIndexSign = p.lcdc & lcdc_tdsel ? 0 : tile_pattern_table_size / tile_size / 2;
		unsigned char const* const tileDataLine = p.vram + 2 * tile_size * tileIndexSign
			+ tileline * tile_line_size;
		int xpos = p.xpos;

		do {
			int nextSprite = p.nextSprite;

			if (spx(p.spriteList[nextSprite]) < xpos + tile_len) {
				int cycles = p.cycles - tile_len;

				if (lcdcObjEn(p)) {
					cycles -= std::max(11 - (spx(p.spriteList[nextSprite]) - xpos), 6);
					for (int i = nextSprite + 1; spx(p.spriteList[i]) < xpos + tile_len; ++i)
						cycles -= 6;

					if (cycles < 0)
						break;

					p.cycles = cycles;

					do {
						unsigned char const* const oam = p.spriteMapper.oamram();
						unsigned reg0, reg1 = oam[p.spriteList[nextSprite].oampos + 2] * tile_size;
						unsigned const attrib = oam[p.spriteList[nextSprite].oampos + 3];
						unsigned const spline = (attrib & attr_yflip
							? p.spriteList[nextSprite].line ^ (2 * tile_len - 1)
							: p.spriteList[nextSprite].line) * tile_line_size;
						unsigned const ts = tile_size;
						reg0 = p.vram[(lcdcObj2x(p) ? (reg1 & ~ts) | spline : reg1 | (spline & ~ts))];
						reg1 = p.vram[(lcdcObj2x(p) ? (reg1 & ~ts) | spline : reg1 | (spline & ~ts)) + 1];

						p.spwordList[nextSprite] =
							expand_lut[reg0 + (0x100 / attr_xflip * attrib & 0x100)]
							+ expand_lut[reg1 + (0x100 / attr_xflip * attrib & 0x100)] * 2;
						p.spriteList[nextSprite].attrib = attrib;
						++nextSprite;
					} while (spx(p.spriteList[nextSprite]) < xpos + tile_len);
				}
				else {
					if (cycles < 0)
						break;

					p.cycles = cycles;

					do {
						++nextSprite;
					} while (spx(p.spriteList[nextSprite]) < xpos + tile_len);
				}

				p.nextSprite = nextSprite;
			}
			else if (nextSprite - 1 < 0 || spx(p.spriteList[nextSprite - 1]) <= xpos - tile_len) {
				if (!(p.cycles & -1ul * tile_len))
					break;

				int n = (std::min(spx(p.spriteList[nextSprite]), xend + tile_len - 1) - xpos) & -1u * tile_len;
				n = std::min<long>(n, p.cycles & -1ul * tile_len);
				p.cycles -= n;

				unsigned ntileword = p.ntileword;
				uint_least32_t* dst = dbufline + xpos - tile_len;
				uint_least32_t* const dstend = dst + n;
				xpos += n;

				if (!lcdcBgEn(p)) {
					do { *dst++ = p.bgPalette[0]; } while (dst != dstend);
					tileMapXpos += n / (1u * tile_len);

					unsigned const tno = tileMapLine[(tileMapXpos - 1) % tile_map_len];
					int const ts = tile_size;
					ntileword = expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[0]]
						+ expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[1]] * 2;
				}
				else do {
					dst[0] = p.bgPalette[ntileword & tile_bpp_mask];
					dst[1] = p.bgPalette[(ntileword & tile_bpp_mask << 1 * tile_bpp) >> 1 * tile_bpp];
					dst[2] = p.bgPalette[(ntileword & tile_bpp_mask << 2 * tile_bpp) >> 2 * tile_bpp];
					dst[3] = p.bgPalette[(ntileword & tile_bpp_mask << 3 * tile_bpp) >> 3 * tile_bpp];
					dst[4] = p.bgPalette[(ntileword & tile_bpp_mask << 4 * tile_bpp) >> 4 * tile_bpp];
					dst[5] = p.bgPalette[(ntileword & tile_bpp_mask << 5 * tile_bpp) >> 5 * tile_bpp];
					dst[6] = p.bgPalette[(ntileword & tile_bpp_mask << 6 * tile_bpp) >> 6 * tile_bpp];
					dst[7] = p.bgPalette[ntileword >> 7 * tile_bpp];
					dst += tile_len;

					unsigned const tno = tileMapLine[tileMapXpos % tile_map_len];
					int const ts = tile_size;
					tileMapXpos = tileMapXpos % tile_map_len + 1;
					ntileword = expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[0]]
						+ expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[1]] * 2;
				} while (dst != dstend);


				p.ntileword = ntileword;
				continue;
			}
			else {
				int cycles = p.cycles - tile_len;
				if (cycles < 0)
					break;

				p.cycles = cycles;
			}


			uint_least32_t* const dst = dbufline + (xpos - tile_len);
			unsigned const tileword = -(p.lcdc & 1u * lcdc_bgen) & p.ntileword;

			dst[0] = p.bgPalette[tileword & tile_bpp_mask];
			dst[1] = p.bgPalette[(tileword & tile_bpp_mask << 1 * tile_bpp) >> 1 * tile_bpp];
			dst[2] = p.bgPalette[(tileword & tile_bpp_mask << 2 * tile_bpp) >> 2 * tile_bpp];
			dst[3] = p.bgPalette[(tileword & tile_bpp_mask << 3 * tile_bpp) >> 3 * tile_bpp];
			dst[4] = p.bgPalette[(tileword & tile_bpp_mask << 4 * tile_bpp) >> 4 * tile_bpp];
			dst[5] = p.bgPalette[(tileword & tile_bpp_mask << 5 * tile_bpp) >> 5 * tile_bpp];
			dst[6] = p.bgPalette[(tileword & tile_bpp_mask << 6 * tile_bpp) >> 6 * tile_bpp];
			dst[7] = p.bgPalette[tileword >> 7 * tile_bpp];

			int i = nextSprite - 1;

			if (!lcdcObjEn(p)) {
				do {
					int const pos = spx(p.spriteList[i]) - xpos;
					int const sa = pos * tile_bpp >= 0
						? tile_len * tile_bpp - pos * tile_bpp
						: tile_len * tile_bpp + pos * tile_bpp;
					p.spwordList[i] = 1l * p.spwordList[i] >> sa;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);
			}
			else {
				do {
					int n;
					int pos = spx(p.spriteList[i]) - xpos;
					if (pos < 0) {
						n = pos + tile_len;
						pos = 0;
					}
					else
						n = tile_len - pos;

					unsigned const attrib = p.spriteList[i].attrib;
					long spword = p.spwordList[i];
					unsigned long const* const spPalette = p.spPalette
						+ (attrib & attr_dmgpalno) / (attr_dmgpalno / num_palette_entries);
					uint_least32_t* d = dst + pos;

					if (!(attrib & attr_bgpriority)) {
						int const bpp = tile_bpp, m = tile_bpp_mask;
						switch (n) {
						case 8: if (spword >> 7 * bpp) { d[7] = spPalette[spword >> 7 * bpp]; } // fall through
						case 7: if (spword >> 6 * bpp & m) { d[6] = spPalette[spword >> 6 * bpp & m]; } // fall through
						case 6: if (spword >> 5 * bpp & m) { d[5] = spPalette[spword >> 5 * bpp & m]; } // fall through
						case 5: if (spword >> 4 * bpp & m) { d[4] = spPalette[spword >> 4 * bpp & m]; } // fall through
						case 4: if (spword >> 3 * bpp & m) { d[3] = spPalette[spword >> 3 * bpp & m]; } // fall through
						case 3: if (spword >> 2 * bpp & m) { d[2] = spPalette[spword >> 2 * bpp & m]; } // fall through
						case 2: if (spword >> 1 * bpp & m) { d[1] = spPalette[spword >> 1 * bpp & m]; } // fall through
						case 1: if (spword & m) { d[0] = spPalette[spword & m]; }
						}

						spword >>= n * bpp;

						/*do {
							if (spword & tile_bpp_mask)
								dst[pos] = spPalette[spword & tile_bpp_mask];

							spword >>= tile_bpp;
							++pos;
						} while (--n);*/
					}
					else {
						unsigned tw = tileword >> pos * tile_bpp;
						d += n;
						n = -n;

						do {
							if (spword & tile_bpp_mask) {
								d[n] = tw & tile_bpp_mask
									? p.bgPalette[tw & tile_bpp_mask]
									: spPalette[spword & tile_bpp_mask];
							}

							spword >>= tile_bpp;
							tw >>= tile_bpp;
						} while (++n);
					}

					p.spwordList[i] = spword;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);
			}


			unsigned const tno = tileMapLine[tileMapXpos % tile_map_len];
			int const ts = tile_size;
			tileMapXpos = tileMapXpos % tile_map_len + 1;
			p.ntileword = expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[0]]
				+ expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[1]] * 2;

			xpos = xpos + tile_len;
		} while (xpos < xend);

		p.xpos = xpos;
	}

	void doFullTilesUnrolledCgb(PPUPriv &p, int const xend, uint_least32_t* const dbufline,
		unsigned char const *const tileMapLine, unsigned const tileline, unsigned tileMapXpos) {
		int const tileIndexSign = p.lcdc & lcdc_tdsel ? 0 : tile_pattern_table_size / tile_size / 2;
		unsigned char const* const tileDataLine = p.vram + 2 * tile_size * tileIndexSign
			+ tileline * tile_line_size;
		int xpos = p.xpos;
		unsigned char const* const vram = p.vram;
		unsigned const tdoffset = tileline * tile_line_size
			+ tile_pattern_table_size / lcdc_tdsel * (~p.lcdc & lcdc_tdsel);

		do {
			int nextSprite = p.nextSprite;

			if (spx(p.spriteList[nextSprite]) < xpos + tile_len) {
				int cycles = p.cycles - tile_len;
				cycles -= std::max(11 - (spx(p.spriteList[nextSprite]) - xpos), 6);
				for (int i = nextSprite + 1; spx(p.spriteList[i]) < xpos + tile_len; ++i)
					cycles -= 6;

				if (cycles < 0)
					break;

				p.cycles = cycles;

				do {
					unsigned char const* const oam = p.spriteMapper.oamram();
					unsigned reg0, reg1 = oam[p.spriteList[nextSprite].oampos + 2] * tile_size;
					unsigned const attrib = oam[p.spriteList[nextSprite].oampos + 3];
					unsigned const spline = (attrib & attr_yflip
						? p.spriteList[nextSprite].line ^ (2 * tile_len - 1)
						: p.spriteList[nextSprite].line) * tile_line_size;
					unsigned const ts = tile_size;
					reg0 = vram[vram_bank_size / attr_tdbank * (attrib & attr_tdbank)
						+ (lcdcObj2x(p) ? (reg1 & ~ts) | spline : reg1 | (spline & ~ts))];
					reg1 = vram[vram_bank_size / attr_tdbank * (attrib & attr_tdbank)
						+ (lcdcObj2x(p) ? (reg1 & ~ts) | spline : reg1 | (spline & ~ts)) + 1];

					p.spwordList[nextSprite] =
						expand_lut[reg0 + (0x100 / attr_xflip * attrib & 0x100)]
						+ expand_lut[reg1 + (0x100 / attr_xflip * attrib & 0x100)] * 2;
					p.spriteList[nextSprite].attrib = attrib;
					++nextSprite;
				} while (spx(p.spriteList[nextSprite]) < xpos + tile_len);

				p.nextSprite = nextSprite;
			}
			else if (nextSprite - 1 < 0 || spx(p.spriteList[nextSprite - 1]) <= xpos - tile_len) {
				if (!(p.cycles & -1ul * tile_len))
					break;

				int n = (std::min(spx(p.spriteList[nextSprite]), xend + tile_len - 1) - xpos) & -1u * tile_len;
				n = std::min<long>(n, p.cycles & -1ul * tile_len);
				p.cycles -= n;

				unsigned ntileword = p.ntileword;
				unsigned nattrib = p.nattrib;
				uint_least32_t* dst = dbufline + xpos - tile_len;
				uint_least32_t* const dstend = dst + n;
				xpos += n;

				if (!lcdcBgEn(p) && p.cgbDmg) {
					do { *dst++ = p.bgPalette[0]; } while (dst != dstend);
					tileMapXpos += n / (1u * tile_len);

					unsigned const tno = tileMapLine[(tileMapXpos - 1) % tile_map_len];
					int const ts = tile_size;
					ntileword = expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[0]]
						+ expand_lut[(tileDataLine + ts * tno - 2 * ts * (tno & tileIndexSign))[1]] * 2;
				}
				else do {
					unsigned long const* const bgPalette = p.bgPalette
						+ (nattrib & attr_cgbpalno) * num_palette_entries;
					dst[0] = bgPalette[ntileword & tile_bpp_mask];
					dst[1] = bgPalette[(ntileword & tile_bpp_mask << 1 * tile_bpp) >> 1 * tile_bpp];
					dst[2] = bgPalette[(ntileword & tile_bpp_mask << 2 * tile_bpp) >> 2 * tile_bpp];
					dst[3] = bgPalette[(ntileword & tile_bpp_mask << 3 * tile_bpp) >> 3 * tile_bpp];
					dst[4] = bgPalette[(ntileword & tile_bpp_mask << 4 * tile_bpp) >> 4 * tile_bpp];
					dst[5] = bgPalette[(ntileword & tile_bpp_mask << 5 * tile_bpp) >> 5 * tile_bpp];
					dst[6] = bgPalette[(ntileword & tile_bpp_mask << 6 * tile_bpp) >> 6 * tile_bpp];
					dst[7] = bgPalette[ntileword >> 7 * tile_bpp];
					dst += tile_len;

					unsigned const tno = tileMapLine[tileMapXpos % tile_map_len];
					nattrib = tileMapLine[tileMapXpos % tile_map_len + vram_bank_size];
					tileMapXpos = tileMapXpos % tile_map_len + 1;

					unsigned const tdo = tdoffset & ~(tno << 5);
					unsigned char const* const td = vram + tno * tile_size
						+ (nattrib & attr_yflip ? tdo ^ tile_line_size * (tile_len - 1) : tdo)
						+ vram_bank_size / attr_tdbank * (nattrib & attr_tdbank);
					unsigned short const* const explut = expand_lut + (0x100 / attr_xflip * nattrib & 0x100);
					ntileword = explut[td[0]] + explut[td[1]] * 2;
				} while (dst != dstend);


				p.ntileword = ntileword;
				p.nattrib = nattrib;
				continue;
			}
			else {
				int cycles = p.cycles - tile_len;
				if (cycles < 0)
					break;

				p.cycles = cycles;
			}

			uint_least32_t* const dst = dbufline + (xpos - tile_len);
			unsigned const tileword = ((p.lcdc & 1u * lcdc_bgen) | !p.cgbDmg) * p.ntileword;;
			unsigned const attrib = p.nattrib;
			unsigned long const* const bgPalette = p.bgPalette
				+ (attrib & attr_cgbpalno) * num_palette_entries;
			dst[0] = bgPalette[tileword & tile_bpp_mask];
			dst[1] = bgPalette[(tileword & tile_bpp_mask << 1 * tile_bpp) >> 1 * tile_bpp];
			dst[2] = bgPalette[(tileword & tile_bpp_mask << 2 * tile_bpp) >> 2 * tile_bpp];
			dst[3] = bgPalette[(tileword & tile_bpp_mask << 3 * tile_bpp) >> 3 * tile_bpp];
			dst[4] = bgPalette[(tileword & tile_bpp_mask << 4 * tile_bpp) >> 4 * tile_bpp];
			dst[5] = bgPalette[(tileword & tile_bpp_mask << 5 * tile_bpp) >> 5 * tile_bpp];
			dst[6] = bgPalette[(tileword & tile_bpp_mask << 6 * tile_bpp) >> 6 * tile_bpp];
			dst[7] = bgPalette[tileword >> 7 * tile_bpp];

			int i = nextSprite - 1;

			if (!lcdcObjEn(p)) {
				do {
					int const pos = spx(p.spriteList[i]) - xpos;
					int const sa = pos * tile_bpp >= 0
						? tile_len * tile_bpp - pos * tile_bpp
						: tile_len * tile_bpp + pos * tile_bpp;
					p.spwordList[i] = 1l * p.spwordList[i] >> sa;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);
			}
			else {
				unsigned char idtab[] = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
				unsigned const bgprioritymask = p.lcdc << 7;

				do {
					int n;
					int pos = spx(p.spriteList[i]) - xpos;
					if (pos < 0) {
						n = pos + tile_len;
						pos = 0;
					}
					else
						n = tile_len - pos;

					unsigned char const id = p.spriteList[i].oampos;
					unsigned const sattrib = p.spriteList[i].attrib;
					long spword = p.spwordList[i];
					unsigned long const* const spPalette = cgbSpPalette(p, sattrib);

					if (!((attrib | sattrib) & bgprioritymask)) {
						unsigned char* const idt = idtab + pos;
						uint_least32_t* const   d = dst + pos;

						switch (n) {
						case 8: if ((spword >> 7 * tile_bpp) && id < idt[7]) {
							idt[7] = id;
							d[7] = spPalette[spword >> 7 * tile_bpp];
						} // fall through
						case 7: if ((spword >> 6 * tile_bpp & tile_bpp_mask) && id < idt[6]) {
							idt[6] = id;
							d[6] = spPalette[spword >> 6 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 6: if ((spword >> 5 * tile_bpp & tile_bpp_mask) && id < idt[5]) {
							idt[5] = id;
							d[5] = spPalette[spword >> 5 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 5: if ((spword >> 4 * tile_bpp & tile_bpp_mask) && id < idt[4]) {
							idt[4] = id;
							d[4] = spPalette[spword >> 4 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 4: if ((spword >> 3 * tile_bpp & tile_bpp_mask) && id < idt[3]) {
							idt[3] = id;
							d[3] = spPalette[spword >> 3 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 3: if ((spword >> 2 * tile_bpp & tile_bpp_mask) && id < idt[2]) {
							idt[2] = id;
							d[2] = spPalette[spword >> 2 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 2: if ((spword >> 1 * tile_bpp & tile_bpp_mask) && id < idt[1]) {
							idt[1] = id;
							d[1] = spPalette[spword >> 1 * tile_bpp & tile_bpp_mask];
						} // fall through
						case 1: if ((spword & tile_bpp_mask) && id < idt[0]) {
							idt[0] = id;
							d[0] = spPalette[spword & tile_bpp_mask];
						}
						}

						spword >>= n * tile_bpp;

						/*do {
							if ((spword & tile_bpp_mask) && id < idtab[pos]) {
								idtab[pos] = id;
									dst[pos] = spPalette[spword & tile_bpp_mask];
							}

							spword >>= tile_bpp;
							++pos;
						} while (--n);*/
					}
					else {
						unsigned tw = tileword >> pos * tile_bpp;

						do {
							if ((spword & tile_bpp_mask) && id < idtab[pos]) {
								idtab[pos] = id;
								dst[pos] = tw & tile_bpp_mask
									? bgPalette[tw & tile_bpp_mask]
									: spPalette[spword & tile_bpp_mask];
							}

							spword >>= tile_bpp;
							tw >>= tile_bpp;
							++pos;
						} while (--n);
					}

					p.spwordList[i] = spword;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);
			}


			{
				unsigned const tno = tileMapLine[tileMapXpos % tile_map_len];
				unsigned const nattrib = tileMapLine[tileMapXpos % tile_map_len + vram_bank_size];
				tileMapXpos = tileMapXpos % tile_map_len + 1;

				unsigned const tdo = tdoffset & ~(tno << 5);
				unsigned char const* const td = vram + tno * tile_size
					+ (nattrib & attr_yflip ? tdo ^ tile_line_size * (tile_len - 1) : tdo)
					+ vram_bank_size / attr_tdbank * (nattrib & attr_tdbank);
				unsigned short const* const explut = expand_lut + (0x100 / attr_xflip * nattrib & 0x100);
				p.ntileword = explut[td[0]] + explut[td[1]] * 2;
				p.nattrib = nattrib;
			}

			xpos = xpos + tile_len;
		} while (xpos < xend);

		p.xpos = xpos;
	}

	void doFullTilesUnrolled(PPUPriv& p) {
		int xpos = p.xpos;
		int const xend = p.wx < p.xpos || p.wx >= xpos_end
			? lcd_hres + 1
			: static_cast<int>(p.wx) - 7;
		if (xpos >= xend)
			return;

		uint_least32_t* const dbufline = p.framebuf.fbline();
		unsigned char const* tileMapLine;
		unsigned tileline;
		unsigned tileMapXpos;
		if (p.winDrawState & win_draw_started) {
			tileMapLine = p.vram + tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
				+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
				+ tile_map_begin;
			tileMapXpos = (xpos + p.wscx) / (1u * tile_len);
			tileline = p.winYPos % tile_len;
		}
		else {
			tileMapLine = p.vram + tile_map_size / lcdc_bgtmsel * (p.lcdc & lcdc_bgtmsel)
				+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
				+ tile_map_begin;
			tileMapXpos = (p.scx + xpos + 1 - p.cgb) / (1u * tile_len);
			tileline = (p.scy + p.lyCounter.ly()) % tile_len;
		}

		if (xpos < tile_len) {
			uint_least32_t prebuf[2 * tile_len];
			if (p.cgb) {
				doFullTilesUnrolledCgb(p, std::min(tile_len, xend), prebuf + (tile_len - xpos),
					tileMapLine, tileline, tileMapXpos);
			}
			else {
				doFullTilesUnrolledDmg(p, std::min(tile_len, xend), prebuf + (tile_len - xpos),
					tileMapLine, tileline, tileMapXpos);
			}

			int const newxpos = p.xpos;
			if (newxpos > tile_len) {
				std::memcpy(dbufline, prebuf + (tile_len - xpos), (newxpos - tile_len) * sizeof * dbufline);
			}
			else if (newxpos < tile_len)
				return;

			if (newxpos >= xend)
				return;

			tileMapXpos += (newxpos - xpos) / (1u * tile_len);
		}

		p.cgb
			? doFullTilesUnrolledCgb(p, xend, dbufline, tileMapLine, tileline, tileMapXpos)
			: doFullTilesUnrolledDmg(p, xend, dbufline, tileMapLine, tileline, tileMapXpos);
	}

	void plotPixel(PPUPriv& p) {
		int const xpos = p.xpos;
		unsigned const tileword = p.tileword;

		uint_least32_t* const fbline = p.framebuf.fbline();

		if (p.wx == xpos
			&& (p.weMaster || (p.wy2 == p.lyCounter.ly() && lcdcWinEn(p)))
			&& xpos < lcd_hres + 7) {
			if (p.winDrawState == 0 && lcdcWinEn(p)) {
				p.winDrawState = win_draw_start | win_draw_started;
				++p.winYPos;
			}
			else if (!p.cgb && (p.winDrawState == 0 || xpos == lcd_hres + 6))
				p.winDrawState |= win_draw_start;
		}

		unsigned const twdata = tileword & ((p.lcdc & lcdc_bgen) | (p.cgb * !p.cgbDmg)) * tile_bpp_mask;
		unsigned long pixel = p.bgPalette[twdata + (p.attrib & attr_cgbpalno) * num_palette_entries];
		int i = static_cast<int>(p.nextSprite) - 1;

		if (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len) {
			unsigned spdata = 0;
			unsigned attrib = 0;

			if (p.cgb) {
				unsigned minId = 0xFF;

				do {
					if ((p.spwordList[i] & tile_bpp_mask) && p.spriteList[i].oampos < minId) {
						spdata = p.spwordList[i] & tile_bpp_mask;
						attrib = p.spriteList[i].attrib;
						minId = p.spriteList[i].oampos;
					}

					p.spwordList[i] >>= tile_bpp;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);

				if (spdata && lcdcObjEn(p)
					&& (!((attrib | p.attrib) & attr_bgpriority) || !twdata || !lcdcBgEn(p))) {
					pixel = *(cgbSpPalette(p, attrib) + spdata);
				}
			}
			else {
				do {
					if (p.spwordList[i] & tile_bpp_mask) {
						spdata = p.spwordList[i] & tile_bpp_mask;
						attrib = p.spriteList[i].attrib;
					}

					p.spwordList[i] >>= tile_bpp;
					--i;
				} while (i >= 0 && spx(p.spriteList[i]) > xpos - tile_len);

				if (spdata && lcdcObjEn(p) && (!(attrib & attr_bgpriority) || !twdata))
					pixel = p.spPalette[(attrib & attr_dmgpalno ? num_palette_entries : 0) + spdata];
			}
		}

		if (xpos - tile_len >= 0)
			fbline[xpos - tile_len] = pixel;


		p.xpos = xpos + 1;
		p.tileword = tileword >> tile_bpp;
	}

static void plotPixelIfNoSprite(PPUPriv &p) {
	if (p.spriteList[p.nextSprite].spx == p.xpos) {
		if (!(lcdcObjEn(p) | p.cgb)) {
			do {
				++p.nextSprite;
			} while (p.spriteList[p.nextSprite].spx == p.xpos);

			plotPixel(p);
		}
	} else
		plotPixel(p);
}

unsigned long nextM2Time(PPUPriv const& p) {
	int const nm2 = p.lyCounter.ly() < lcd_vres - 1
		? weMasterCheckPriorToLyIncLineCycle(p.cgb)
		: lcd_cycles_per_line * (lcd_lines_per_frame - p.lyCounter.ly())
		+ weMasterCheckLy0LineCycle(p.cgb);
	return p.lyCounter.time() - p.lyCounter.lineTime() + (nm2 << p.lyCounter.isDoubleSpeed());
}

void xposEnd(PPUPriv& p) {
	p.lastM0Time = p.now - (p.cycles << p.lyCounter.isDoubleSpeed());

	unsigned long const nextm2 = nextM2Time(p);
	p.cycles = p.now >= nextm2
		? static_cast<long>((p.now - nextm2) >> p.lyCounter.isDoubleSpeed())
		: -static_cast<long>((nextm2 - p.now) >> p.lyCounter.isDoubleSpeed());
	nextCall(0, p.lyCounter.ly() == lcd_vres - 1 ? M2_Ly0::f0_ : M2_LyNon0::f0_, p);
}

bool handleWinDrawStartReq(PPUPriv const &p, int const xpos, unsigned char &winDrawState) {
	bool const startWinDraw = (xpos < lcd_hres + 7 || p.cgb)
		&& (winDrawState &= win_draw_started);
	if (!lcdcWinEn(p))
		winDrawState &= ~(1u * win_draw_started);

	return startWinDraw;
}

static bool handleWinDrawStartReq(PPUPriv &p) {
	return handleWinDrawStartReq(p, p.xpos, p.winDrawState);
}

namespace StartWindowDraw {
	void inc(PPUState const& nextf, PPUPriv& p) {
		if (!lcdcWinEn(p) && p.cgb) {
			plotPixelIfNoSprite(p);

			if (p.xpos == p.endx) {
				if (p.xpos < xpos_end) {
					nextCall(1, Tile::f0_, p);
				}
				else
					xposEnd(p);

				return;
			}
		}

		nextCall(1, nextf, p);
	}

	void f0(PPUPriv& p) {
		if (p.xpos == p.endx) {
			p.tileword = p.ntileword;
			p.attrib = p.nattrib;
			p.endx = std::min(1u * xpos_end, p.xpos + 1u * tile_len);
		}

		p.wscx = tile_len - p.xpos;

		if (p.winDrawState & win_draw_started) {
			p.reg1 = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
				+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
				+ tile_map_begin];
			p.nattrib = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
				+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
				+ tile_map_begin + vram_bank_size];
		}
		else {
			p.reg1 = p.vram[tile_map_size / lcdc_bgtmsel * (p.lcdc & lcdc_bgtmsel)
				+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
				+ tile_map_begin];
			p.nattrib = p.vram[tile_map_size / lcdc_bgtmsel * (p.lcdc & lcdc_bgtmsel)
				+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
				+ tile_map_begin + vram_bank_size];
		}

		inc(f1_, p);
	}

	void f1(PPUPriv& p) {
		inc(f2_, p);
	}

	void f2(PPUPriv& p) {
		p.reg0 = loadTileDataByte0(p);
		inc(f3_, p);
	}

	void f3(PPUPriv& p) {
		inc(f4_, p);
	}

	void f4(PPUPriv& p) {
		int const r1 = loadTileDataByte1(p);

		p.ntileword = (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[p.reg0]
			+ (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[r1] * 2;

		inc(f5_, p);
	}

	void f5(PPUPriv& p) {
		inc(Tile::f0_, p);
	}
}

namespace LoadSprites {
	void inc(PPUState const& nextf, PPUPriv& p) {
		plotPixelIfNoSprite(p);

		if (p.xpos == p.endx) {
			if (p.xpos < xpos_end) {
				nextCall(1, Tile::f0_, p);
			}
			else
				xposEnd(p);
		}
		else
			nextCall(1, nextf, p);
	}

	void f0(PPUPriv& p) {
		p.reg1 = p.spriteMapper.oamram()[p.spriteList[p.currentSprite].oampos + 2];
		nextCall(1, f1_, p);
	}

	void f1(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		p.spriteList[p.currentSprite].attrib =
			p.spriteMapper.oamram()[p.spriteList[p.currentSprite].oampos + 3];
		inc(f2_, p);
	}

	void f2(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		unsigned const spline =
			(p.spriteList[p.currentSprite].attrib & attr_yflip
				? p.spriteList[p.currentSprite].line ^ (2 * tile_len - 1)
				: p.spriteList[p.currentSprite].line) * tile_line_size;
		unsigned const ts = tile_size;
		p.reg0 = p.vram[vram_bank_size / attr_tdbank
			* (p.spriteList[p.currentSprite].attrib & p.cgb * attr_tdbank)
			+ (lcdcObj2x(p) ? (p.reg1 * ts & ~ts) | spline : p.reg1 * ts | (spline & ~ts))];
		inc(f3_, p);
	}

	void f3(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		inc(f4_, p);
	}

	void f4(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		unsigned const spline =
			(p.spriteList[p.currentSprite].attrib & attr_yflip
				? p.spriteList[p.currentSprite].line ^ (2 * tile_len - 1)
				: p.spriteList[p.currentSprite].line) * tile_line_size;
		unsigned const ts = tile_size;
		p.reg1 = p.vram[vram_bank_size / attr_tdbank
			* (p.spriteList[p.currentSprite].attrib & p.cgb * attr_tdbank)
			+ (lcdcObj2x(p) ? (p.reg1 * ts & ~ts) | spline : p.reg1 * ts | (spline & ~ts)) + 1];
		inc(f5_, p);
	}

	void f5(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		plotPixelIfNoSprite(p);

		unsigned entry = p.currentSprite;

		if (entry == p.nextSprite) {
			++p.nextSprite;
		}
		else {
			entry = p.nextSprite - 1;
			p.spriteList[entry] = p.spriteList[p.currentSprite];
		}

		p.spwordList[entry] =
			expand_lut[p.reg0 + (0x100 / attr_xflip * p.spriteList[entry].attrib & 0x100)]
			+ expand_lut[p.reg1 + (0x100 / attr_xflip * p.spriteList[entry].attrib & 0x100)] * 2;
		p.spriteList[entry].spx = p.xpos;

		if (p.xpos == p.endx) {
			if (p.xpos < xpos_end) {
				nextCall(1, Tile::f0_, p);
			}
			else
				xposEnd(p);
		}
		else {
			p.nextCallPtr = &Tile::f5_;
			nextCall(1, Tile::f5_, p);
		}
	}
}

namespace Tile {
	void inc(PPUState const& nextf, PPUPriv& p) {
		plotPixelIfNoSprite(p);

		if (p.xpos == xpos_end) {
			xposEnd(p);
		}
		else
			nextCall(1, nextf, p);
	}

	void f0(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		doFullTilesUnrolled(p);

		if (p.xpos == xpos_end) {
			++p.cycles;
			return xposEnd(p);
		}

		p.tileword = p.ntileword;
		p.attrib = p.nattrib;
		p.endx = std::min(1u * xpos_end, p.xpos + 1u * tile_len);

		if (p.winDrawState & win_draw_started) {
			p.reg1 = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
				+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
				+ (p.xpos + p.wscx) / tile_len % tile_map_len + tile_map_begin];
			p.nattrib = p.vram[tile_map_size / lcdc_wtmsel * (p.lcdc & lcdc_wtmsel)
				+ tile_map_len / tile_len * (p.winYPos & (0x100 - tile_len))
				+ (p.xpos + p.wscx) / tile_len % tile_map_len + tile_map_begin
				+ vram_bank_size];
		}
		else {
			p.reg1 = p.vram[((tile_map_size / lcdc_bgtmsel * p.lcdc | (p.scx + p.xpos + 1u - p.cgb) / tile_len)
				& (tile_map_size + tile_map_len - 1))
				+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
				+ tile_map_begin];
			p.nattrib = p.vram[((tile_map_size / lcdc_bgtmsel * p.lcdc | (p.scx + p.xpos + 1u - p.cgb) / tile_len)
				& (tile_map_size + tile_map_len - 1))
				+ tile_map_len / tile_len * ((p.scy + p.lyCounter.ly()) & (0x100 - tile_len))
				+ tile_map_begin + vram_bank_size];
		}

		inc(f1_, p);
	}

	void f1(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		inc(f2_, p);
	}

	void f2(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		p.reg0 = loadTileDataByte0(p);
		inc(f3_, p);
	}

	void f3(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		inc(f4_, p);
	}

	void f4(PPUPriv& p) {
		if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
			return StartWindowDraw::f0(p);

		int const r1 = loadTileDataByte1(p);

		p.ntileword = (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[p.reg0]
			+ (expand_lut + (0x100 / attr_xflip * p.nattrib & 0x100))[r1] * 2;

		plotPixelIfNoSprite(p);

		if (p.xpos == xpos_end) {
			xposEnd(p);
		}
		else
			nextCall(1, f5_, p);
	}

	void f5(PPUPriv& p) {
		int endx = p.endx;
		p.nextCallPtr = &f5_;

		do {
			if ((p.winDrawState & win_draw_start) && handleWinDrawStartReq(p))
				return StartWindowDraw::f0(p);

			if (p.spriteList[p.nextSprite].spx == p.xpos) {
				if (lcdcObjEn(p) | p.cgb) {
					p.currentSprite = p.nextSprite;
					return LoadSprites::f0(p);
				}

				do {
					++p.nextSprite;
				} while (p.spriteList[p.nextSprite].spx == p.xpos);
			}

			plotPixel(p);

			if (p.xpos == endx) {
				if (endx < xpos_end) {
					nextCall(1, f0_, p);
				}
				else
					xposEnd(p);

				return;
			}
		} while (--p.cycles >= 0);
	}
}

} // namespace M3Loop

namespace M2_Ly0 {
	static unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, unsigned winDrawState,
		int targetxpos, unsigned cycles);
}

namespace M2_LyNon0 {
	static unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, unsigned winDrawState,
		int targetxpos, unsigned cycles);
}

namespace M3Loop {

	unsigned predictCyclesUntilXposNextLine(
		PPUPriv const& p, unsigned winDrawState, int const targetx) {
		if (p.wx == lcd_hres + 6 && !p.cgb && p.xpos < lcd_hres + 7
			&& (p.weMaster || (p.wy2 == p.lyCounter.ly() && lcdcWinEn(p)))) {
			winDrawState = win_draw_start | (lcdcWinEn(p) ? win_draw_started : 0);
		}

		unsigned const cycles = (nextM2Time(p) - p.now) >> p.lyCounter.isDoubleSpeed();

		return p.lyCounter.ly() == lcd_vres - 1
			? M2_Ly0::predictCyclesUntilXpos_f0(p, winDrawState, targetx, cycles)
			: M2_LyNon0::predictCyclesUntilXpos_f0(p, winDrawState, targetx, cycles);
	}


namespace StartWindowDraw {
	static unsigned predictCyclesUntilXpos_fn(PPUPriv const &p, int xpos,
		int endx, unsigned ly, unsigned nextSprite, bool weMaster,
		unsigned winDrawState, int fno, int targetx, unsigned cycles);
}

namespace Tile {
	unsigned char const* addSpriteCycles(unsigned char const* nextSprite,
		unsigned char const* spriteEnd, unsigned char const* const spxOf,
		unsigned const maxSpx, unsigned const firstTileXpos,
		unsigned prevSpriteTileNo, unsigned* const cyclesAccumulator) {
		int sum = 0;

		for (; nextSprite < spriteEnd && spxOf[*nextSprite] <= maxSpx; ++nextSprite) {
			int cycles = 6;
			int const distanceFromTileStart = (spxOf[*nextSprite] - firstTileXpos) % tile_len;
			unsigned const tileNo = (spxOf[*nextSprite] - firstTileXpos) & -tile_len;

			if (distanceFromTileStart < 5 && tileNo != prevSpriteTileNo)
				cycles = 11 - distanceFromTileStart;

			prevSpriteTileNo = tileNo;
			sum += cycles;
		}

		*cyclesAccumulator += sum;

		return nextSprite;
	}

	unsigned predictCyclesUntilXpos_fn(PPUPriv const& p, int const xpos,
		int const endx, unsigned const ly, unsigned const nextSprite,
		bool const weMaster, unsigned char winDrawState, int const fno,
		int const targetx, unsigned cycles) {
		if ((winDrawState & win_draw_start)
			&& handleWinDrawStartReq(p, xpos, winDrawState)) {
			return StartWindowDraw::predictCyclesUntilXpos_fn(p, xpos, endx,
				ly, nextSprite, weMaster, winDrawState, 0, targetx, cycles);
		}

		if (xpos > targetx)
			return predictCyclesUntilXposNextLine(p, winDrawState, targetx);

		enum { tileno_none = 1 }; // low bit set, so it will never be equal to an actual tile number.

		int nwx = 0xFF;
		cycles += targetx - xpos;

		if (p.wx - 1u * xpos < targetx - 1u * xpos
			&& lcdcWinEn(p) && (weMaster || p.wy2 == ly)
			&& !(winDrawState & win_draw_started)
			&& (p.cgb || p.wx != lcd_hres + 6)) {
			nwx = p.wx;
			cycles += 6;
		}

		if (lcdcObjEn(p) | p.cgb) {
			unsigned char const* sprite = p.spriteMapper.sprites(ly);
			unsigned char const* const spriteEnd = sprite + p.spriteMapper.numSprites(ly);
			sprite += nextSprite;

			if (sprite < spriteEnd) {
				int const spx = p.spriteMapper.posbuf()[*sprite + 1];
				unsigned firstTileXpos = endx % (1u * tile_len); // ok even if endx is capped at 168,
																 // because fno will be used.
				unsigned prevSpriteTileNo = (xpos - firstTileXpos) & -tile_len; // this tile. all sprites on this
																				// tile will now add 6 cycles.
				// except this one.
				if (fno + spx - xpos < 5 && spx <= nwx) {
					cycles += 11 - (fno + spx - xpos);
					sprite += 1;
				}

				if (nwx < targetx) {
					sprite = addSpriteCycles(sprite, spriteEnd, p.spriteMapper.posbuf() + 1,
						nwx, firstTileXpos, prevSpriteTileNo, &cycles);
					firstTileXpos = nwx + 1;
					prevSpriteTileNo = tileno_none;
				}

				addSpriteCycles(sprite, spriteEnd, p.spriteMapper.posbuf() + 1,
					targetx, firstTileXpos, prevSpriteTileNo, &cycles);
			}
		}

		return cycles;
	}

	unsigned predictCyclesUntilXpos_fn(PPUPriv const& p,
		int endx, int fno, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.xpos, endx, p.lyCounter.ly(),
			p.nextSprite, p.weMaster, p.winDrawState, fno, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, std::min(1u * xpos_end, p.xpos + 1u * tile_len), 0, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f1(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 1, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f2(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 2, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f3(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 3, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f4(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 4, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f5(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 5, targetx, cycles);
	}
}

namespace StartWindowDraw {
	unsigned predictCyclesUntilXpos_fn(PPUPriv const &p, int xpos,
			int const endx, unsigned const ly, unsigned const nextSprite, bool const weMaster,
			unsigned const winDrawState, int const fno, int const targetx, unsigned cycles) {
		if (xpos > targetx)
			return predictCyclesUntilXposNextLine(p, winDrawState, targetx);

		int cinc = 6 - fno;

		if (!lcdcWinEn(p) && p.cgb) {
			int xinc = std::min<int>(cinc, std::min(endx, targetx + 1) - xpos);

			if ((lcdcObjEn(p) | p.cgb) && p.spriteList[nextSprite].spx < xpos + xinc) {
				xpos = p.spriteList[nextSprite].spx;
			} else {
				cinc = xinc;
				xpos += xinc;
			}
		}

		cycles += cinc;

		if (xpos <= targetx) {
			return Tile::predictCyclesUntilXpos_fn(p, xpos, std::min(xpos_end, xpos + tile_len),
				ly, nextSprite, weMaster, winDrawState, 0, targetx, cycles);
		}

		return cycles - 1;
	}

	unsigned predictCyclesUntilXpos_fn(PPUPriv const &p,
			int endx, int fno, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.xpos, endx, p.lyCounter.ly(),
			p.nextSprite, p.weMaster, p.winDrawState, fno, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, int targetx, unsigned cycles) {
		int endx = p.xpos == p.endx
			? std::min(1u * xpos_end, p.xpos + 1u * tile_len)
			: p.endx;
		return predictCyclesUntilXpos_fn(p, endx, 0, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f1(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 1, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f2(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 2, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f3(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 3, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f4(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 4, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f5(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, p.endx, 5, targetx, cycles);
	}
}

namespace LoadSprites {
	unsigned predictCyclesUntilXpos_fn(PPUPriv const &p,
			int const fno, int const targetx, unsigned cycles) {
		unsigned nextSprite = p.nextSprite;
		if (lcdcObjEn(p) | p.cgb) {
			cycles += 6 - fno;
			nextSprite += 1;
		}

		return Tile::predictCyclesUntilXpos_fn(p, p.xpos, p.endx, p.lyCounter.ly(),
			nextSprite, p.weMaster, p.winDrawState, 5, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 0, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f1(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 1, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f2(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 2, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f3(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 3, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f4(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 4, targetx, cycles);
	}
	unsigned predictCyclesUntilXpos_f5(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_fn(p, 5, targetx, cycles);
	}
}

} // namespace M3Loop

namespace M3Start {
	unsigned predictCyclesUntilXpos_f1(PPUPriv const& p, unsigned xpos, unsigned ly,
		bool weMaster, unsigned winDrawState, int targetx, unsigned cycles) {
		cycles += std::min((p.scx - xpos) % tile_len, max_m3start_cycles - xpos) + 1 - p.cgb;
		return M3Loop::Tile::predictCyclesUntilXpos_fn(p, 0, tile_len - p.scx % tile_len, ly, 0,
			weMaster, winDrawState, std::min(p.scx % (1u * tile_len), 5u), targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, unsigned ly,
			bool weMaster, unsigned winDrawState, int targetx, unsigned cycles) {
		winDrawState = (winDrawState & win_draw_start) && lcdcWinEn(p) ? win_draw_started : 0;
		return predictCyclesUntilXpos_f1(p, 0, ly, weMaster, winDrawState, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const &p, int targetx, unsigned cycles) {
		unsigned ly = p.lyCounter.ly() + (p.lyCounter.time() - p.now < 16);
		return predictCyclesUntilXpos_f0(p, ly, p.weMaster, p.winDrawState, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f1(PPUPriv const &p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_f1(p, p.xpos, p.lyCounter.ly(), p.weMaster,
		                                 p.winDrawState, targetx, cycles);
	}
}

namespace M2_Ly0 {
	unsigned predictCyclesUntilXpos_f0(PPUPriv const& p,
		unsigned winDrawState, int targetx, unsigned cycles) {
		bool weMaster = lcdcWinEn(p) && 0 == p.wy;
		unsigned ly = 0;

		return M3Start::predictCyclesUntilXpos_f0(p, ly, weMaster, winDrawState, targetx,
			cycles + m3StartLineCycle(p.cgb) - weMasterCheckLy0LineCycle(p.cgb));

	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_f0(p, p.winDrawState, targetx, cycles);
	}
}

namespace M2_LyNon0 {
	unsigned predictCyclesUntilXpos_f1(PPUPriv const& p, bool weMaster,
		unsigned winDrawState, int targetx, unsigned cycles) {
		unsigned ly = p.lyCounter.ly() + 1;
		weMaster |= lcdcWinEn(p) && ly == p.wy;

		return M3Start::predictCyclesUntilXpos_f0(p, ly, weMaster, winDrawState, targetx,
			cycles + lcd_cycles_per_line - weMasterCheckAfterLyIncLineCycle(p.cgb) + m3StartLineCycle(p.cgb));
	}

	unsigned predictCyclesUntilXpos_f1(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_f1(p, p.weMaster, p.winDrawState, targetx, cycles);
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const& p,
		unsigned winDrawState, int targetx, unsigned cycles) {
		bool weMaster = p.weMaster || (lcdcWinEn(p) && p.lyCounter.ly() == p.wy);

		return predictCyclesUntilXpos_f1(p, weMaster, winDrawState, targetx,
			cycles + weMasterCheckAfterLyIncLineCycle(p.cgb)
			- weMasterCheckPriorToLyIncLineCycle(p.cgb));
	}

	unsigned predictCyclesUntilXpos_f0(PPUPriv const& p, int targetx, unsigned cycles) {
		return predictCyclesUntilXpos_f0(p, p.winDrawState, targetx, cycles);
	}
}

} // anon namespace

PPUPriv::PPUPriv(NextM0Time &nextM0Time, unsigned char const *const oamram, unsigned char const *const vram)
: spriteList()
, spwordList()
, nextSprite(0)
, currentSprite(0xFF)
, vram(vram)
, nextCallPtr(&M2_Ly0::f0_)
, now(0)
, lastM0Time(0)
, cycles(-4396)
, tileword(0)
, ntileword(0)
, spriteMapper(nextM0Time, lyCounter, oamram)
, lcdc(0)
, scy(0)
, scx(0)
, wy(0)
, wy2(0)
, wx(0)
, winDrawState(0)
, wscx(0)
, winYPos(0)
, reg0(0)
, reg1(0)
, attrib(0)
, nattrib(0)
, xpos(0)
, endx(0)
, cgb(false)
, weMaster(false)
{
}

namespace {

template<class T, class K, std::size_t start, std::size_t len>
struct BSearch {
	static std::size_t upperBound(T const a[], K e) {
		if (e < a[start + len / 2])
			return BSearch<T, K, start, len / 2>::upperBound(a, e);

		return BSearch<T, K, start + len / 2 + 1, len - (len / 2 + 1)>::upperBound(a, e);
	}
};

template<class T, class K, std::size_t start>
struct BSearch<T, K, start, 0> {
	static std::size_t upperBound(T const [], K ) {
		return start;
	}
};

template<std::size_t len, class T, class K>
std::size_t upperBound(T const a[], K e) {
	return BSearch<T, K, 0, len>::upperBound(a, e);
}

struct CycleState {
	PPUState const *state;
	long cycle;
	operator long() const { return cycle; }
};

PPUState const * decodeM3LoopState(unsigned state) {
	switch (state) {
	case M3Loop::Tile::ID0: return &M3Loop::Tile::f0_;
	case M3Loop::Tile::ID1: return &M3Loop::Tile::f1_;
	case M3Loop::Tile::ID2: return &M3Loop::Tile::f2_;
	case M3Loop::Tile::ID3: return &M3Loop::Tile::f3_;
	case M3Loop::Tile::ID4: return &M3Loop::Tile::f4_;
	case M3Loop::Tile::ID5: return &M3Loop::Tile::f5_;

	case M3Loop::LoadSprites::ID0: return &M3Loop::LoadSprites::f0_;
	case M3Loop::LoadSprites::ID1: return &M3Loop::LoadSprites::f1_;
	case M3Loop::LoadSprites::ID2: return &M3Loop::LoadSprites::f2_;
	case M3Loop::LoadSprites::ID3: return &M3Loop::LoadSprites::f3_;
	case M3Loop::LoadSprites::ID4: return &M3Loop::LoadSprites::f4_;
	case M3Loop::LoadSprites::ID5: return &M3Loop::LoadSprites::f5_;

	case M3Loop::StartWindowDraw::ID0: return &M3Loop::StartWindowDraw::f0_;
	case M3Loop::StartWindowDraw::ID1: return &M3Loop::StartWindowDraw::f1_;
	case M3Loop::StartWindowDraw::ID2: return &M3Loop::StartWindowDraw::f2_;
	case M3Loop::StartWindowDraw::ID3: return &M3Loop::StartWindowDraw::f3_;
	case M3Loop::StartWindowDraw::ID4: return &M3Loop::StartWindowDraw::f4_;
	case M3Loop::StartWindowDraw::ID5: return &M3Loop::StartWindowDraw::f5_;
	}

	return 0;
}

long cyclesUntilM0Upperbound(PPUPriv const& p) {
	long cycles = xpos_end - p.xpos + 6;
	for (int i = p.nextSprite; i < lcd_max_num_sprites_per_line && p.spriteList[i].spx < xpos_end; ++i)
		cycles += 11;

	return cycles;
}

void saveSpriteList(PPUPriv const& p, SaveState& ss) {
	for (int i = 0; i < lcd_max_num_sprites_per_line; ++i) {
		ss.ppu.spAttribList[i] = p.spriteList[i].attrib;
		ss.ppu.spByte0List[i] = p.spwordList[i] & 0xFF;
		ss.ppu.spByte1List[i] = p.spwordList[i] >> 8;
	}

	ss.ppu.nextSprite = p.nextSprite;
	ss.ppu.currentSprite = p.currentSprite;
}

void loadSpriteList(PPUPriv& p, SaveState const& ss) {
	if (ss.ppu.videoCycles < 1ul * lcd_vres * lcd_cycles_per_line && ss.ppu.xpos < xpos_end) {
		int const ly = ss.ppu.videoCycles / lcd_cycles_per_line;
		int const numSprites = p.spriteMapper.numSprites(ly);
		unsigned char const* const sprites = p.spriteMapper.sprites(ly);

		for (int i = 0; i < numSprites; ++i) {
			int const pos = sprites[i];
			int const spy = p.spriteMapper.posbuf()[pos];
			int const spx = p.spriteMapper.posbuf()[pos + 1];

			p.spriteList[i].spx = spx;
			p.spriteList[i].line = ly + 2 * tile_len - spy;
			p.spriteList[i].oampos = pos * 2;
			p.spriteList[i].attrib = ss.ppu.spAttribList[i] & 0xFF;
			p.spwordList[i] = (ss.ppu.spByte1List[i] * 0x100l + ss.ppu.spByte0List[i]) & 0xFFFF;
		}

		p.spriteList[numSprites].spx = 0xFF;
		p.nextSprite = std::min(1u * ss.ppu.nextSprite, 1u * numSprites);

		while (p.spriteList[p.nextSprite].spx < ss.ppu.xpos)
			++p.nextSprite;

		p.currentSprite = std::min(p.nextSprite, ss.ppu.currentSprite);
	}
}

}

void PPU::loadState(SaveState const& ss, unsigned char const* const oamram) {
	PPUState const* const m3loopState = decodeM3LoopState(ss.ppu.state);
	long const videoCycles = std::min(ss.ppu.videoCycles, lcd_cycles_per_frame - 1ul);
	bool const ds = p_.cgb & ss.mem.ioamhram.get()[0x14D] >> 7;
	long const lineCycles = static_cast<unsigned long>(videoCycles) % lcd_cycles_per_line;

	p_.now = ss.cpu.cycleCounter;
	p_.lcdc = ss.mem.ioamhram.get()[0x140];
	p_.lyCounter.setDoubleSpeed(ds);
	p_.lyCounter.reset(videoCycles, ss.cpu.cycleCounter);
	p_.spriteMapper.loadState(ss, oamram);
	p_.winYPos = ss.ppu.winYPos;
	p_.scy = ss.mem.ioamhram.get()[0x142];
	p_.scx = ss.mem.ioamhram.get()[0x143];
	p_.wy = ss.mem.ioamhram.get()[0x14A];
	p_.wy2 = ss.ppu.oldWy;
	p_.wx = ss.mem.ioamhram.get()[0x14B];
	p_.xpos = std::min(1u * xpos_end, 1u * ss.ppu.xpos);
	p_.endx = (p_.xpos & -1u * tile_len) + ss.ppu.endx % tile_len;
	p_.endx = std::min(1u * xpos_end, p_.endx <= p_.xpos ? p_.endx + 1u * tile_len : p_.endx);
	p_.reg0 = ss.ppu.reg0 & 0xFF;
	p_.reg1 = ss.ppu.reg1 & 0xFF;
	p_.tileword = ss.ppu.tileword & 0xFFFF;
	p_.ntileword = ss.ppu.ntileword & 0xFFFF;
	p_.attrib = ss.ppu.attrib & 0xFF;
	p_.nattrib = ss.ppu.nattrib & 0xFF;
	p_.wscx = ss.ppu.wscx;
	p_.weMaster = ss.ppu.weMaster;
	p_.winDrawState = ss.ppu.winDrawState & (win_draw_start | win_draw_started);
	p_.lastM0Time = p_.now - ss.ppu.lastM0Time;
	p_.cgbDmg = !ss.ppu.notCgbDmg;
	loadSpriteList(p_, ss);

	if (m3loopState && videoCycles < 1l * lcd_vres * lcd_cycles_per_line && p_.xpos < xpos_end
		&& lineCycles + cyclesUntilM0Upperbound(p_) < weMasterCheckPriorToLyIncLineCycle(p_.cgb)) {
		p_.nextCallPtr = m3loopState;
		p_.cycles = -1;
	}
	else if (videoCycles < (lcd_vres - 1l) * lcd_cycles_per_line + m3StartLineCycle(p_.cgb) + max_m3start_cycles) {
		CycleState const lineCycleStates[] = {
			{   &M3Start::f0_, m3StartLineCycle(p_.cgb) },
			{   &M3Start::f1_, m3StartLineCycle(p_.cgb) + max_m3start_cycles },
			{ &M2_LyNon0::f0_, weMasterCheckPriorToLyIncLineCycle(p_.cgb) },
			{ &M2_LyNon0::f1_, weMasterCheckAfterLyIncLineCycle(p_.cgb) },
			{   &M3Start::f0_, m3StartLineCycle(p_.cgb) + lcd_cycles_per_line }
		};

		std::size_t const pos =
			upperBound<sizeof lineCycleStates / sizeof * lineCycleStates - 1>(lineCycleStates, lineCycles);

		p_.cycles = lineCycles - lineCycleStates[pos].cycle;
		p_.nextCallPtr = lineCycleStates[pos].state;

		if (&M3Start::f1_ == lineCycleStates[pos].state) {
			p_.xpos = lineCycles - m3StartLineCycle(p_.cgb) + 1;
			p_.cycles = -1;
		}
	}
	else {
		p_.cycles = videoCycles - lcd_cycles_per_frame - weMasterCheckLy0LineCycle(p_.cgb);
		p_.nextCallPtr = &M2_Ly0::f0_;
	}
}

void PPU::reset(unsigned char const *oamram, unsigned char const *vram, bool cgb) {
	p_.vram = vram;
	p_.cgb = cgb;
	p_.cgbDmg = false;
	p_.spriteMapper.reset(oamram, cgb);
}

void PPU::resetCc(unsigned long const oldCc, unsigned long const newCc) {
	unsigned long const dec = oldCc - newCc;
	unsigned long const videoCycles = lcdcEn(p_) ? p_.lyCounter.frameCycles(p_.now) : 0;

	p_.now -= dec;
	p_.lastM0Time = p_.lastM0Time ? p_.lastM0Time - dec : p_.lastM0Time;
	p_.lyCounter.reset(videoCycles, p_.now);
	p_.spriteMapper.resetCycleCounter(oldCc, newCc);
}

void PPU::speedChange() {
	unsigned long const now = p_.now;
	unsigned long const videoCycles = lcdcEn(p_) ? p_.lyCounter.frameCycles(now) : 0;

	p_.now -= p_.lyCounter.isDoubleSpeed();
	p_.spriteMapper.resetCycleCounter(now, p_.now);
	p_.lyCounter.setDoubleSpeed(!p_.lyCounter.isDoubleSpeed());
	p_.lyCounter.reset(videoCycles, p_.now);
}

unsigned long PPU::predictedNextXposTime(unsigned xpos) const {
	return p_.now
	    + (p_.nextCallPtr->predictCyclesUntilXpos_f(p_, xpos, -p_.cycles) << p_.lyCounter.isDoubleSpeed());
}

void PPU::setLcdc(unsigned const lcdc, unsigned long const cc) {
	if ((p_.lcdc ^ lcdc) & lcdc & lcdc_en) {
		p_.now = cc;
		p_.lastM0Time = 0;
		p_.lyCounter.reset(0, p_.now);
		p_.spriteMapper.enableDisplay(cc);
		p_.weMaster = (lcdc & lcdc_we) && 0 == p_.wy;
		p_.winDrawState = 0;
		p_.nextCallPtr = &M3Start::f0_;
		p_.cycles = -(m3StartLineCycle(p_.cgb) + 2);
	}
	else if ((p_.lcdc ^ lcdc) & lcdc_we) {
		if (!(lcdc & lcdc_we)) {
			if (p_.winDrawState == win_draw_started || p_.xpos == xpos_end)
				p_.winDrawState &= ~(1u * win_draw_started);
		}
		else if (p_.winDrawState == win_draw_start) {
			p_.winDrawState |= win_draw_started;
			++p_.winYPos;
		}
	}

	if ((p_.lcdc ^ lcdc) & lcdc_obj2x) {
		if (p_.lcdc & lcdc & lcdc_en)
			p_.spriteMapper.oamChange(cc);

		p_.spriteMapper.setLargeSpritesSource(lcdc & lcdc_obj2x);
	}

	p_.lcdc = lcdc;
}

void PPU::update(unsigned long const cc) {
	long const cycles = (cc - p_.now) >> p_.lyCounter.isDoubleSpeed();

	p_.now += cycles << p_.lyCounter.isDoubleSpeed();
	p_.cycles += cycles;

	if (p_.cycles >= 0) {
		p_.framebuf.setFbline(p_.lyCounter.ly());
		p_.nextCallPtr->f(p_);
	}
}

SYNCFUNC(PPU)
{
	NSS(p_.bgPalette);
	NSS(p_.spPalette);
	NSS(p_.spriteList);
	NSS(p_.spwordList);
	NSS(p_.nextSprite);
	NSS(p_.currentSprite);
	NSS(p_.layersMask);

	EBS(p_.nextCallPtr, 0);
	EVS(p_.nextCallPtr, &M2_Ly0::f0_, 1);
	EVS(p_.nextCallPtr, &M2_LyNon0::f0_, 2);
	EVS(p_.nextCallPtr, &M2_LyNon0::f1_, 3);
	EVS(p_.nextCallPtr, &M3Start::f0_, 4);
	EVS(p_.nextCallPtr, &M3Start::f1_, 5);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f0_, 6);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f1_, 7);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f2_, 8);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f3_, 9);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f4_, 10);
	EVS(p_.nextCallPtr, &M3Loop::Tile::f5_, 11);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f0_, 12);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f1_, 13);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f2_, 14);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f3_, 15);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f4_, 16);
	EVS(p_.nextCallPtr, &M3Loop::LoadSprites::f5_, 17);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f0_, 18);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f1_, 19);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f2_, 20);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f3_, 21);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f4_, 22);
	EVS(p_.nextCallPtr, &M3Loop::StartWindowDraw::f5_, 23);
	EES(p_.nextCallPtr, NULL);

	NSS(p_.now);
	NSS(p_.lastM0Time);
	NSS(p_.cycles);

	NSS(p_.tileword);
	NSS(p_.ntileword);

	SSS(p_.spriteMapper);
	SSS(p_.lyCounter);

	NSS(p_.lcdc);
	NSS(p_.scy);
	NSS(p_.scx);
	NSS(p_.wy);
	NSS(p_.wy2);
	NSS(p_.wx);
	NSS(p_.winDrawState);
	NSS(p_.wscx);
	NSS(p_.winYPos);
	NSS(p_.reg0);
	NSS(p_.reg1);
	NSS(p_.attrib);
	NSS(p_.nattrib);
	NSS(p_.xpos);
	NSS(p_.endx);

	NSS(p_.cgb);
	NSS(p_.cgbDmg);
	NSS(p_.weMaster);
}
