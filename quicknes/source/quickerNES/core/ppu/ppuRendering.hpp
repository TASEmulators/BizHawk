#pragma once

// NES PPU emulator graphics rendering
// Emu 0.7.0

#include "ppuImpl.hpp"

namespace quickerNES
{

class Ppu_Rendering : public Ppu_Impl
{
  typedef Ppu_Impl base;

  public:
  Ppu_Rendering();

  int sprite_limit;

  uint8_t *host_pixels;
  long host_row_bytes;

  protected:
  long sprite_hit_found; // -1: sprite 0 didn't hit, 0: no hit so far, > 0: y * 341 + x
  void draw_background(int start, int count);
  void draw_sprites(int start, int count);

  private:
  void draw_scanlines(int start, int count, uint8_t *pixels, long pitch, int mode);
  void draw_background_(int count);

  // destination for draw functions; avoids extra parameters
  uint8_t *scanline_pixels;
  long scanline_row_bytes;

  // fill/copy
  void fill_background(int count);
  void clip_left(int count);
  void save_left(int count);
  void restore_left(int count);

  // sprites
  enum
  {
    max_sprites = 64
  };
  uint8_t sprite_scanlines[image_height]; // number of sprites on each scanline
  void draw_sprites_(int start, int count);
  bool sprite_hit_possible(int scanline) const;
  void check_sprite_hit(int begin, int end);
};

inline Ppu_Rendering::Ppu_Rendering()
{
  sprite_limit = 8;
  host_pixels = nullptr;
}

inline void Ppu_Rendering::draw_sprites(int start, int count)
{
  draw_scanlines(start, count, host_pixels + host_row_bytes * start, host_row_bytes, 2);
}

} // namespace quickerNES