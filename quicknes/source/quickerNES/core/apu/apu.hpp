#pragma once

// NES 2A03 APU sound chip emulator
// Snd_Emu 0.1.7

#include "oscs.hpp"
#include <climits>
#include <cstdint>

namespace quickerNES
{

class Apu
{
  public:
  typedef uint8_t env_t[3];
  /*struct env_t {
    uint8_t delay;
    uint8_t env;
    uint8_t written;
  };*/

  struct apu_t
  {
    uint8_t w40xx[0x14]; // $4000-$4013
    uint8_t w4015;       // enables
    uint8_t w4017;       // mode
    uint16_t frame_delay;
    uint8_t frame_step;
    uint8_t irq_flag;
  };

  struct square_t
  {
    uint16_t delay;
    env_t env;
    uint8_t length_counter;
    uint8_t phase;
    uint8_t swp_delay;
    uint8_t swp_reset;
    uint8_t unused2[1];
  };

  struct triangle_t
  {
    uint16_t delay;
    uint8_t length_counter;
    uint8_t phase;
    uint8_t linear_counter;
    uint8_t linear_mode;
  };

  struct noise_t
  {
    uint16_t delay;
    env_t env;
    uint8_t length_counter;
    uint16_t shift_reg;
  };

  struct dmc_t
  {
    uint16_t delay;
    uint16_t remain;
    uint16_t addr;
    uint8_t buf;
    uint8_t bits_remain;
    uint8_t bits;
    uint8_t buf_full;
    uint8_t silence;
    uint8_t irq_flag;
  };

  struct apu_state_t
  {
    apu_t apu;
    square_t square1;
    square_t square2;
    triangle_t triangle;
    noise_t noise;
    dmc_t dmc;
  };
  static_assert(sizeof(apu_state_t) == 72);

  Apu();
  ~Apu();

  // Set buffer to generate all sound into, or disable sound if NULL
  void output(Blip_Buffer *);

  // Set memory reader callback used by DMC oscillator to fetch samples.
  // When callback is invoked, 'user_data' is passed unchanged as the
  // first parameter.
  void dmc_reader(int (*callback)(void *user_data, nes_addr_t), void *user_data = nullptr);

  // All time values are the number of CPU clock cycles relative to the
  // beginning of the current time frame. Before resetting the CPU clock
  // count, call end_frame( last_cpu_time ).

  // Write to register (0x4000-0x4017, except 0x4014 and 0x4016)
  static const uint16_t start_addr = 0x4000;
  static const uint16_t end_addr = 0x4017;
  void write_register(nes_time_t, nes_addr_t, int data);

  // Read from status register at 0x4015
  static const uint16_t status_addr = 0x4015;
  int read_status(nes_time_t);

  // Run all oscillators up to specified time, end current time frame, then
  // start a new time frame at time 0. Time frames have no effect on emulation
  // and each can be whatever length is convenient.
  void end_frame(nes_time_t);

  // Additional optional features (can be ignored without any problem)

  // Reset internal frame counter, registers, and all oscillators.
  // Use PAL timing if pal_timing is true, otherwise use NTSC timing.
  // Set the DMC oscillator's initial DAC value to initial_dmc_dac without
  // any audible click.
  void reset(bool pal_timing = false, int initial_dmc_dac = 0);

  // Save/load exact emulation state
  void save_state(apu_state_t *out) const;
  void load_state(apu_state_t const &);

  // Set overall volume (default is 1.0)
  void volume(double);

  // Set treble equalization (see notes.txt)
  void treble_eq(const blip_eq_t &);

  // Set sound output of specific oscillator to buffer. If buffer is NULL,
  // the specified oscillator is muted and emulation accuracy is reduced.
  // The oscillators are indexed as follows: 0) Square 1, 1) Square 2,
  // 2) Triangle, 3) Noise, 4) DMC.
  static const uint16_t osc_count = 5;
  void osc_output(int index, Blip_Buffer *buffer);

  // Set IRQ time callback that is invoked when the time of earliest IRQ
  // may have changed, or NULL to disable. When callback is invoked,
  // 'user_data' is passed unchanged as the first parameter.
  void irq_notifier(void (*callback)(void *user_data), void *user_data = nullptr);

  // Get time that APU-generated IRQ will occur if no further register reads
  // or writes occur. If IRQ is already pending, returns irq_waiting. If no
  // IRQ will occur, returns no_irq.
  static const uint64_t no_irq = LONG_MAX / 2 + 1;
  static const uint16_t irq_waiting = 0;
  nes_time_t earliest_irq(nes_time_t) const;

  // Count number of DMC reads that would occur if 'run_until( t )' were executed.
  // If last_read is not NULL, set *last_read to the earliest time that
  // 'count_dmc_reads( time )' would result in the same result.
  int count_dmc_reads(nes_time_t t, nes_time_t *last_read = nullptr) const;

  // Time when next DMC memory read will occur
  nes_time_t next_dmc_read_time() const;

  // Run DMC until specified time, so that any DMC memory reads can be
  // accounted for (i.e. inserting CPU wait states).
  void run_until(nes_time_t);

  // End of public interface.
  private:
  friend class Nonlinearizer;
  void enable_nonlinear(double volume);
  static double nonlinear_tnd_gain() { return 0.75; }

  private:
  friend struct Dmc;

  // noncopyable
  Apu(const Apu &);
  Apu &operator=(const Apu &);

  Osc *oscs[osc_count];
  Square square1;
  Square square2;
  Noise noise;
  Triangle triangle;
  Dmc dmc;

  nes_time_t last_time; // has been run until this time in current frame
  nes_time_t last_dmc_time;
  nes_time_t earliest_irq_;
  nes_time_t next_irq;
  int frame_period;
  int frame_delay; // cycles until frame counter runs next
  int frame;       // current frame (0-3)
  int osc_enables;
  int frame_mode;
  bool irq_flag;
  void (*irq_notifier_)(void *user_data);
  void *irq_data;
  Square::Synth square_synth; // shared by squares

  void irq_changed();
  void state_restored();
  void run_until_(nes_time_t);

  // TODO: remove
  friend class Core;
};

inline void Apu::osc_output(int osc, Blip_Buffer *buf)
{
  oscs[osc]->output = buf;
}

inline nes_time_t Apu::earliest_irq(nes_time_t) const
{
  return earliest_irq_;
}

inline void Apu::dmc_reader(int (*func)(void *, nes_addr_t), void *user_data)
{
  dmc.prg_reader_data = user_data;
  dmc.prg_reader = func;
}

inline void Apu::irq_notifier(void (*func)(void *user_data), void *user_data)
{
  irq_notifier_ = func;
  irq_data = user_data;
}

inline int Apu::count_dmc_reads(nes_time_t time, nes_time_t *last_read) const
{
  return dmc.count_reads(time, last_read);
}

inline nes_time_t Dmc::next_read_time() const
{
  if (length_counter == 0)
    return Apu::no_irq; // not reading

  return apu->last_dmc_time + delay + long(bits_remain - 1) * period;
}

inline nes_time_t Apu::next_dmc_read_time() const { return dmc.next_read_time(); }

template <int mode>
struct apu_reflection
{
#define REFLECT(apu, state) (mode ? void(apu = state) : void(state = apu))

  static void reflect_env(Apu::env_t *state, Envelope &osc)
  {
    REFLECT((*state)[0], osc.env_delay);
    REFLECT((*state)[1], osc.envelope);
    REFLECT((*state)[2], osc.reg_written[3]);
  }

  static void reflect_square(Apu::square_t &state, Square &osc)
  {
    reflect_env(&state.env, osc);
    REFLECT(state.delay, osc.delay);
    REFLECT(state.length_counter, osc.length_counter);
    REFLECT(state.phase, osc.phase);
    REFLECT(state.swp_delay, osc.sweep_delay);
    REFLECT(state.swp_reset, osc.reg_written[1]);
  }

  static void reflect_triangle(Apu::triangle_t &state, Triangle &osc)
  {
    REFLECT(state.delay, osc.delay);
    REFLECT(state.length_counter, osc.length_counter);
    REFLECT(state.linear_counter, osc.linear_counter);
    REFLECT(state.phase, osc.phase);
    REFLECT(state.linear_mode, osc.reg_written[3]);
  }

  static void reflect_noise(Apu::noise_t &state, Noise &osc)
  {
    reflect_env(&state.env, osc);
    REFLECT(state.delay, osc.delay);
    REFLECT(state.length_counter, osc.length_counter);
    REFLECT(state.shift_reg, osc.noise);
  }

  static void reflect_dmc(Apu::dmc_t &state, Dmc &osc)
  {
    REFLECT(state.delay, osc.delay);
    REFLECT(state.remain, osc.length_counter);
    REFLECT(state.buf, osc.buf);
    REFLECT(state.bits_remain, osc.bits_remain);
    REFLECT(state.bits, osc.bits);
    REFLECT(state.buf_full, osc.buf_full);
    REFLECT(state.silence, osc.silence);
    REFLECT(state.irq_flag, osc.irq_flag);
    if (mode)
      state.addr = osc.address | 0x8000;
    else
      osc.address = state.addr & 0x7fff;
  }
};

inline void Apu::save_state(apu_state_t *state) const
{
  for (int i = 0; i < osc_count * 4; i++)
  {
    int index = i >> 2;
    state->apu.w40xx[i] = oscs[index]->regs[i & 3];
    // if ( index < 4 )
    //   state->length_counters [index] = oscs [index]->length_counter;
  }
  state->apu.w40xx[0x11] = dmc.dac;

  state->apu.w4015 = osc_enables;
  state->apu.w4017 = frame_mode;
  state->apu.frame_delay = frame_delay;
  state->apu.frame_step = frame;
  state->apu.irq_flag = irq_flag;

  typedef apu_reflection<1> refl;
  Apu &apu = *(Apu *)this; // const_cast
  refl::reflect_square(state->square1, apu.square1);
  refl::reflect_square(state->square2, apu.square2);
  refl::reflect_triangle(state->triangle, apu.triangle);
  refl::reflect_noise(state->noise, apu.noise);
  refl::reflect_dmc(state->dmc, apu.dmc);
}

inline void Apu::load_state(apu_state_t const &state)
{
  reset();

  write_register(0, 0x4017, state.apu.w4017);
  write_register(0, 0x4015, state.apu.w4015);
  osc_enables = state.apu.w4015; // DMC clears bit 4

  for (int i = 0; i < osc_count * 4; i++)
  {
    int n = state.apu.w40xx[i];
    int index = i >> 2;
    oscs[index]->regs[i & 3] = n;
    write_register(0, 0x4000 + i, n);
    // if ( index < 4 )
    //   oscs [index]->length_counter = state.length_counters [index];
  }

  frame_delay = state.apu.frame_delay;
  frame = state.apu.frame_step;
  irq_flag = state.apu.irq_flag;

  typedef apu_reflection<0> refl;
  apu_state_t &st = (apu_state_t &)state; // const_cast
  refl::reflect_square(st.square1, square1);
  refl::reflect_square(st.square2, square2);
  refl::reflect_triangle(st.triangle, triangle);
  refl::reflect_noise(st.noise, noise);
  refl::reflect_dmc(st.dmc, dmc);
  dmc.recalc_irq();

  // force channels to have correct last_amp levels after load state
  square1.run(last_time, last_time);
  square2.run(last_time, last_time);
  triangle.run(last_time, last_time);
  noise.run(last_time, last_time);
  dmc.run(last_time, last_time);
}

} // namespace quickerNES