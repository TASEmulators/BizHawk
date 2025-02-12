#pragma once

// Effects_Buffer with non-linear sound
// Nes_Emu 0.7.0

#include "buffer.hpp"
#include "effectsBuffer.hpp"

namespace quickerNES
{

// Effects_Buffer uses several buffers and outputs stereo sample pairs.
class Nes_Effects_Buffer : public Effects_Buffer
{
  public:
  Nes_Effects_Buffer();
  ~Nes_Effects_Buffer();

  // Setup APU for use with buffer, including setting its output to this buffer.
  // If you're using Nes_Emu, this is automatically called for you.
  void set_apu(Apu *apu) { nonlin.set_apu(apu); }

  // Enable/disable non-linear output
  void enable_nonlinearity(bool = true);

  // See Effects_Buffer.h for reference
  const char *set_sample_rate(long rate, int msec = blip_default_length);
  void config(const config_t &);
  void clear();
  channel_t channel(int);
  long read_samples(blip_sample_t *, long);

  void SaveAudioBufferState();
  void RestoreAudioBufferState();

  private:
  Nonlinearizer nonlin;
  friend Multi_Buffer *set_apu(Nes_Effects_Buffer *, Apu *);
};

} // namespace quickerNES