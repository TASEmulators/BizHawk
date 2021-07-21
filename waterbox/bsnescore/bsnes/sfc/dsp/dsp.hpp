#include "SPC_DSP.h"

struct DSP {
  shared_pointer<Emulator::Stream> stream;
  uint8 apuram[64 * 1024] = {};

  auto main() -> void;
  auto read(uint8 address) -> uint8;
  auto write(uint8 address, uint8 data) -> void;

  auto load() -> bool;
  auto power(bool reset) -> void;
  auto mute() -> bool;

  auto serialize(serializer&) -> void;

  int64 clock = 0;

private:
  bool fastDSP = false;
  SPC_DSP spc_dsp;
  int16 samplebuffer[8192];

//unserialized:
  uint8 echoram[64 * 1024] = {};
};

extern DSP dsp;
