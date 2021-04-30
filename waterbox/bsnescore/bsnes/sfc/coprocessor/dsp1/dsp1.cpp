#include <sfc/sfc.hpp>

namespace SuperFamicom {

#define   int8   int8_t
#define  int16  int16_t
#define  int32  int32_t
#define  int64  int64_t
#define  uint8  uint8_t
#define uint16 uint16_t
#define uint32 uint32_t
#define uint64 uint64_t
#define DSP1_CPP
#include "dsp1emu.hpp"
#include "dsp1emu.cpp"
Dsp1 dsp1emu;
#undef   int8
#undef  int16
#undef  int32
#undef  int64
#undef  uint8
#undef uint16
#undef uint32
#undef uint64

DSP1 dsp1;
#include "serialization.cpp"

auto DSP1::power() -> void {
  dsp1emu.reset();
}

auto DSP1::read(uint addr, uint8 data) -> uint8 {
  if(addr & 1) {
    return dsp1emu.getSr();
  } else {
    return dsp1emu.getDr();
  }
}

auto DSP1::write(uint addr, uint8 data) -> void {
  if(addr & 1) {
  } else {
    return dsp1emu.setDr(data);
  }
}

}
