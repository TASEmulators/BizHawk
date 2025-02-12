#pragma once

// NES non-linear audio buffer
// Emu 0.7.0

#include "multiBuffer.hpp"
#include <cstdint>

namespace quickerNES
{

class Apu;

class Nonlinearizer
{
  private:
  enum
  {
    table_bits = 11
  };
  enum
  {
    table_size = 1 << table_bits
  };
  int16_t table[table_size];
  Apu *apu;
  long accum;
  long prev;

  long extra_accum;
  long extra_prev;

  public:
  Nonlinearizer();
  bool enabled;
  void clear();
  void set_apu(Apu *a) { apu = a; }
  Apu *enable(bool, Blip_Buffer *tnd);
  long make_nonlinear(Blip_Buffer &buf, long count);
  void SaveAudioBufferState();
  void RestoreAudioBufferState();
};

class Buffer : public Multi_Buffer
{
  public:
  Buffer();
  ~Buffer();

  // Setup APU for use with buffer, including setting its output to this buffer.
  // If you're using Emu, this is automatically called for you.
  void set_apu(Apu *apu) { nonlin.set_apu(apu); }

  // Enable/disable non-linear output
  void enable_nonlinearity(bool = true);

  // Blip_Buffer to output other sound chips to
  Blip_Buffer *buffer() { return &buf; }

  // See Multi_Buffer.h
  const char *set_sample_rate(long rate, int msec = blip_default_length);

  void clock_rate(long);
  void bass_freq(int);
  void clear();
  channel_t channel(int);
  void end_frame(blip_time_t, bool unused = true);
  long samples_avail() const;
  long read_samples(blip_sample_t *, long);

  private:
  Blip_Buffer buf;
  Blip_Buffer tnd;
  Nonlinearizer nonlin;
  friend Multi_Buffer *set_apu(Buffer *, Apu *);

  public:
  virtual void SaveAudioBufferState();
  virtual void RestoreAudioBufferState();
};

} // namespace quickerNES