
#pragma once

// Konami VRC7 sound chip emulator
// Snd_Emu 0.1.7. Copyright (C) 2003-2005 Shay Green. GNU LGPL license.

#include "../blipBuffer.hpp"
#include "emu2413_state.hpp"
#include <cstdint>

namespace quickerNES
{

struct vrc7_snapshot_t;
typedef long nes_time_t;

class Vrc7
{
  public:
  Vrc7();
  ~Vrc7();

  // See Apu.h for reference
  void reset();
  void volume(double);
  void treble_eq(blip_eq_t const &);
  void output(Blip_Buffer *);
  enum
  {
    osc_count = 6
  };
  void osc_output(int index, Blip_Buffer *);
  void end_frame(nes_time_t);
  void save_snapshot(vrc7_snapshot_t *);
  void load_snapshot(vrc7_snapshot_t &, int dataSize);
  void update_last_amp();

  void write_reg(int reg);
  void write_data(nes_time_t, int data);

  private:
  // noncopyable
  Vrc7(const Vrc7 &);
  Vrc7 &operator=(const Vrc7 &);

  struct Vrc7_Osc
  {
    uint8_t regs[3];
    Blip_Buffer *output;
    int last_amp;
  };

  void *opll;
  nes_time_t last_time;

  Blip_Synth<blip_med_quality, 2048 * 2> synth; // DB2LIN_AMP_BITS == 11, * 2
  int count;
  Vrc7_Osc oscs[osc_count];

  void run_until(nes_time_t);
};

struct vrc7_snapshot_t
{
  uint8_t latch;
  uint8_t inst[8];
  uint8_t regs[6][3];
  uint8_t count;
  int internal_opl_state_size;
  OPLL_STATE internal_opl_state;
};
static_assert(sizeof(vrc7_snapshot_t) == 28 + 440 + 4);

inline void Vrc7::osc_output(int i, Blip_Buffer *buf)
{
  oscs[i].output = buf;
}

} // namespace quickerNES