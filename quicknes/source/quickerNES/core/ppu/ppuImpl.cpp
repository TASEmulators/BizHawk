// Emu 0.7.0. http://www.slack.net/~ant/

#include "ppuImpl.hpp"
#include <cstdint>
#include <cstdio>
#include <cstring>

/* Copyright (C) 2004-2006 Shay Green. This module is free software; you
can redistribute it and/or modify it under the terms of the GNU Lesser
General Public License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version. This
module is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for
more details. You should have received a copy of the GNU Lesser General
Public License along with this module; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA */

namespace quickerNES
{

int const cache_line_size = 128; // tile cache is kept aligned to this boundary

inline void set_be32(void *p, unsigned long n)
{
  ((unsigned char *)p)[0] = (unsigned char)(n >> 24);
  ((unsigned char *)p)[1] = (unsigned char)(n >> 16);
  ((unsigned char *)p)[2] = (unsigned char)(n >> 8);
  ((unsigned char *)p)[3] = (unsigned char)n;
}

#define SET_BE32(addr, data) set_be32(addr, data)

Ppu_Impl::Ppu_Impl()
{
  impl = NULL;
  chr_data = NULL;
  chr_size = 0;
  tile_cache = NULL;
  host_palette = NULL;
  max_palette_size = 0;
  tile_cache_mem = NULL;
  ppu_state_t::unused = 0;

  mmc24_enabled = false;
  mmc24_latched[0] = 0;
  mmc24_latched[1] = 0;
}

Ppu_Impl::~Ppu_Impl()
{
  close_chr();
  delete impl;
}

void Ppu_Impl::all_tiles_modified()
{
  any_tiles_modified = true;
  memset(modified_tiles, ~0, sizeof modified_tiles);
}

const char *Ppu_Impl::open_chr(uint8_t const *new_chr, long chr_data_size)
{
  close_chr();

  if (!impl)
  {
    impl = new impl_t;
    chr_ram = impl->chr_ram;
  }

  chr_data = new_chr;
  chr_size = chr_data_size;
  chr_is_writable = false;

  if (chr_data_size == 0)
  {
    // CHR RAM
    chr_data = impl->chr_ram;
    chr_size = sizeof impl->chr_ram;
    chr_is_writable = true;
  }

  // allocate aligned memory for cache
  long tile_count = chr_size / bytes_per_tile;
  tile_cache_mem = new uint8_t[tile_count * sizeof(cached_tile_t) * 2 + cache_line_size];
  tile_cache = (cached_tile_t *)(tile_cache_mem + cache_line_size -
                                 (uintptr_t)tile_cache_mem % cache_line_size);
  flipped_tiles = tile_cache + tile_count;

  // rebuild cache
  all_tiles_modified();
  if (!chr_is_writable)
  {
    any_tiles_modified = false;
    rebuild_chr(0, chr_size);
  }

  return 0;
}

void Ppu_Impl::close_chr()
{
  delete[] tile_cache_mem;
  tile_cache_mem = NULL;
}

void Ppu_Impl::set_chr_bank(int addr, int size, long data)
{
  if (data + size > chr_size)
    data %= chr_size;

  int count = (unsigned)size / chr_page_size;

  int page = (unsigned)addr / chr_page_size;
  while (count--)
  {
    chr_pages[page] = data - page * chr_page_size;
    page++;
    data += chr_page_size;
  }
}

void Ppu_Impl::set_chr_bank_ex(int addr, int size, long data)
{
  mmc24_enabled = true;

  // check( !chr_is_writable || addr == data ); // to do: is CHR RAM ever bank-switched?
  // dprintf( "Tried to set CHR RAM bank at %04X to CHR+%04X\n", addr, data );

  if (data + size > chr_size)
    data %= chr_size;

  int count = (unsigned)size / chr_page_size;
  // assert( chr_page_size * count == size );
  // assert( addr + size <= chr_addr_size );

  int page = (unsigned)addr / chr_page_size;
  while (count--)
  {
    chr_pages_ex[page] = data - page * chr_page_size;
    page++;
    data += chr_page_size;
  }
}

static uint8_t const initial_palette[0x20] =
  {
    0x0f, 0x01, 0x00, 0x01, 0x00, 0x02, 0x02, 0x0D, 0x08, 0x10, 0x08, 0x24, 0x00, 0x00, 0x04, 0x2C, 0x00, 0x01, 0x34, 0x03, 0x00, 0x04, 0x00, 0x14, 0x00, 0x3A, 0x00, 0x02, 0x00, 0x20, 0x2C, 0x08};

void Ppu_Impl::reset(bool full_reset)
{
  w2000 = 0;
  w2001 = 0;
  r2002 = 0x80;
  r2007 = 0;
  open_bus = 0;
  decay_low = 0;
  decay_high = 0;
  second_write = false;
  vram_temp = 0;
  pixel_x = 0;

  if (full_reset)
  {
    vram_addr = 0;
    w2003 = 0;
    memset(impl->chr_ram, 0xff, sizeof impl->chr_ram);
    memset(impl->nt_ram, 0xff, sizeof impl->nt_ram);
    memcpy(palette, initial_palette, sizeof palette);
  }

  set_nt_banks(0, 0, 0, 0);
  set_chr_bank(0, chr_addr_size, 0);
  memset(spr_ram, 0xff, sizeof spr_ram);
  all_tiles_modified();
  if (max_palette_size > 0)
    memset(host_palette, 0, max_palette_size * sizeof *host_palette);
}

void Ppu_Impl::capture_palette()
{
  if (palette_size + palette_increment <= max_palette_size)
  {
    palette_offset = (palette_begin + palette_size) * 0x01010101;

    short *out = host_palette + palette_size;
    palette_size += palette_increment;

    int i;

    int emph = w2001 << 1 & 0x1C0;
    int mono = (w2001 & 1 ? 0x30 : 0x3F);

    for (i = 0; i < 32; i++)
      out[i] = (palette[i] & mono) | emph;

    int bg = out[0];
    for (i = 4; i < 32; i += 4)
      out[i] = bg;

    memcpy(out + 32, out, 32 * sizeof *out);
  }
}

void Ppu_Impl::run_hblank(int count)
{
  long addr = (vram_addr & 0x7be0) + (vram_temp & 0x41f) + (count * 0x1000);
  if (w2001 & 0x08)
  {
    while (addr >= 0x8000)
    {
      int y = (addr + 0x20) & 0x3e0;
      addr = (addr - 0x8000) & ~0x3e0;
      if (y == 30 * 0x20)
        y = 0x800;
      addr ^= y;
    }
    vram_addr = addr;
  }
}

#ifdef __MWERKS__
  #pragma ppc_unroll_factor_limit 1 // messes up calc_sprite_max_scanlines loop
static int zero = 0;
#else
const int zero = 0;
#endif

// Tile cache

inline unsigned long reorder(unsigned long n)
{
  n |= n << 7;
  return ((n << 14) | n);
}

inline void Ppu_Impl::update_tile(int index)
{
  const uint8_t *in = chr_data + (index)*bytes_per_tile;
  uint8_t *out = (uint8_t *)tile_cache[index];
  uint8_t *flipped_out = (uint8_t *)flipped_tiles[index];

  unsigned long bit_mask = 0x11111111 + zero;

  for (int n = 4; n--;)
  {
    // Reorder two lines of two-bit pixels. No bits are wasted, so
    // reordered version is also four bytes.
    //
    // 12345678 to A0E4B1F5C2G6D3H7
    // ABCDEFGH
    unsigned long c =
      ((reorder(in[0]) & bit_mask) << 0) |
      ((reorder(in[8]) & bit_mask) << 1) |
      ((reorder(in[1]) & bit_mask) << 2) |
      ((reorder(in[9]) & bit_mask) << 3);
    in += 2;

    SET_BE32(out, c);
    out += 4;

    // make horizontally-flipped version
    c = ((c >> 28) & 0x000f) |
        ((c >> 20) & 0x00f0) |
        ((c >> 12) & 0x0f00) |
        ((c >> 4) & 0xf000) |
        ((c & 0xf000) << 4) |
        ((c & 0x0f00) << 12) |
        ((c & 0x00f0) << 20) |
        ((c & 0x000f) << 28);
    SET_BE32(flipped_out, c);
    flipped_out += 4;
  }
}

void Ppu_Impl::rebuild_chr(unsigned long begin, unsigned long end)
{
  unsigned end_index = (end + bytes_per_tile - 1) / bytes_per_tile;
  for (unsigned index = begin / bytes_per_tile; index < end_index; index++)
    update_tile(index);
}

void Ppu_Impl::update_tiles(int first_tile)
{
  int chunk = 0;
  do
  {
    if (!(uint32_t &)modified_tiles[chunk])
    {
      chunk += 4;
    }
    else
    {
      do
      {
        int modified = modified_tiles[chunk];
        if (modified)
        {
          modified_tiles[chunk] = 0;

          int index = first_tile + chunk * 8;
          do
          {
            if (modified & 1)
              update_tile(index);
            index++;
          } while ((modified >>= 1) != 0);
        }
      } while (++chunk & 3);
    }
  } while (chunk < chr_tile_count / 8);
}

// Sprite max

template <int height>
struct calc_sprite_max_scanlines
{
  static unsigned long func(uint8_t const *sprites, uint8_t *scanlines, int begin)
  {
    unsigned long any_hits = 0;
    unsigned long const offset = 0x01010101 + zero;
    unsigned limit = 239 + height - begin;
    for (int n = 64; n; --n)
    {
      int top = *sprites;
      sprites += 4;
      uint8_t *p = scanlines + top;
      if ((unsigned)(239 - top) < limit)
      {
        unsigned long p0 = ((uint32_t *)p)[0] + offset;
        unsigned long p4 = ((uint32_t *)p)[1] + offset;
        ((uint32_t *)p)[0] = p0;
        any_hits |= p0;
        ((uint32_t *)p)[1] = p4;
        any_hits |= p4;
        if (height > 8)
        {
          unsigned long p0 = ((uint32_t *)p)[2] + offset;
          unsigned long p4 = ((uint32_t *)p)[3] + offset;
          ((uint32_t *)p)[2] = p0;
          any_hits |= p0;
          ((uint32_t *)p)[3] = p4;
          any_hits |= p4;
        }
      }
    }

    return any_hits;
  }
};

long Ppu_Impl::recalc_sprite_max(int scanline)
{
  int const max_scanline_count = image_height;

  uint8_t sprite_max_scanlines[256 + 16];

  // recalculate sprites per scanline
  memset(sprite_max_scanlines + scanline, 0x78, last_sprite_max_scanline - scanline);
  unsigned long any_hits;
  if (w2000 & 0x20)
    any_hits = calc_sprite_max_scanlines<16>::func(spr_ram, sprite_max_scanlines, scanline);
  else
    any_hits = calc_sprite_max_scanlines<8>::func(spr_ram, sprite_max_scanlines, scanline);

  // cause search to terminate past max_scanline_count if none have 8 or more sprites
  (uint32_t &)sprite_max_scanlines[max_scanline_count] = 0;
  sprite_max_scanlines[max_scanline_count + 3] = 0x80;

  // avoid scan if no possible hits
  if (!(any_hits & 0x80808080))
    return 0;

  // find soonest scanline with 8 or more sprites
  while (true)
  {
    unsigned long const mask = 0x80808080 + zero;

    // check four at a time
    uint8_t *pos = &sprite_max_scanlines[scanline];
    unsigned long n = *((uint32_t *)pos);
    while (1)
    {
      unsigned long x = n & mask;
      pos += 4;
      n = *((uint32_t *)pos);
      if (x)
        break;
    }

    int height = sprite_height();
    int remain = 8;
    int i = 0;

    // find which of the four
    pos -= 3 + (pos[-4] >> 7 & 1);
    pos += 1 - (*pos >> 7 & 1);
    pos += 1 - (*pos >> 7 & 1);

    scanline = pos - sprite_max_scanlines;
    if (scanline >= max_scanline_count)
      break;

    // find time that max sprites flag is set (or that it won't be set)
    do
    {
      int relative = scanline - spr_ram[i];
      i += 4;
      if ((unsigned)relative < (unsigned)height && !--remain)
      {
        // now use screwey search for 9th sprite
        int offset = 0;
        while (i < 0x100)
        {
          int relative = scanline - spr_ram[i + offset];
          // dprintf( "Checking sprite %d [%d]\n", i / 4, offset );
          i += 4;
          offset = (offset + 1) & 3;
          if ((unsigned)relative < (unsigned)height)
          {
            // dprintf( "sprite max on scanline %d\n", scanline );
            return scanline * scanline_len + (unsigned)i / 2;
          }
        }
        break;
      }
    } while (i < 0x100);
    scanline++;
  }

  return 0;
}

} // namespace quickerNES