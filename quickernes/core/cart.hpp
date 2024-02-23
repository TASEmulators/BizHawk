#pragma once

// NES cartridge data (PRG, CHR, mapper)

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

// Emu 0.7.0. http://www.slack.net/~ant/

#include <cstdint>
#include <cstdlib>
#include <cstring>

namespace quickerNES
{

class Cart
{
  public:
  Cart() = default;

  struct ines_header_t
  {
    uint8_t signature[4];
    uint8_t prg_count; // number of 16K PRG banks
    uint8_t chr_count; // number of 8K CHR banks
    uint8_t flags;     // MMMM FTBV Mapper low, Four-screen, Trainer, Battery, V mirror
    uint8_t flags2;    // MMMM --XX Mapper high 4 bits
    uint8_t zero[8];   // if zero [7] is non-zero, treat flags2 as zero
  };
  static_assert(sizeof(ines_header_t) == 16);

  // Load iNES file
  void load_ines(const uint8_t *buffer)
  {
    ines_header_t h;

    size_t bufferPos = 0;
    {
      size_t copySize = sizeof(ines_header_t);
      memcpy(&h, &buffer[bufferPos], copySize);
      bufferPos += copySize;
    }
    if (h.zero[7]) h.flags2 = 0;
    set_mapper(h.flags, h.flags2);

    // skip trainer
    if (h.flags & 0x04) bufferPos += 512;

    // Allocating memory for prg and chr
    prg_size_ = h.prg_count * 16 * 1024L;
    chr_size_ = h.chr_count * 8 * 1024L;

    auto p = malloc(prg_size_ + chr_size_);
    prg_ = (uint8_t *)p;
    chr_ = &prg_[prg_size_];

    {
      size_t copySize = prg_size();
      memcpy(prg(), &buffer[bufferPos], copySize);
      bufferPos += copySize;
    }
    {
      size_t copySize = chr_size();
      memcpy(chr(), &buffer[bufferPos], copySize);
      bufferPos += copySize;
    }
  }

  inline bool has_battery_ram() const { return mapper & 0x02; }

  // Set mapper and information bytes. LSB and MSB are the standard iNES header
  // bytes at offsets 6 and 7.
  inline void set_mapper(int mapper_lsb, int mapper_msb)
  {
    mapper = mapper_msb * 0x100 + mapper_lsb;
  }

  inline int mapper_code() const { return ((mapper >> 8) & 0xf0) | ((mapper >> 4) & 0x0f); }

  // Size of PRG data
  long prg_size() const { return prg_size_; }

  // Size of CHR data
  long chr_size() const { return chr_size_; }

  unsigned mapper_data() const { return mapper; }

  // Initial mirroring setup
  int mirroring() const { return mapper & 0x09; }

  // Pointer to beginning of PRG data
  inline uint8_t *prg() { return prg_; }
  inline uint8_t const *prg() const { return prg_; }

  // Pointer to beginning of CHR data
  inline uint8_t *chr() { return chr_; }
  inline uint8_t const *chr() const { return chr_; }

  // End of public interface
  private:
  uint8_t *prg_;
  uint8_t *chr_;
  long prg_size_;
  long chr_size_;
  unsigned mapper;
};

} // namespace quickerNES