#include <sfc/sfc.hpp>

namespace SuperFamicom {

namespace DSP4i {
  #define  bool8  uint8_t
  #define   int8   int8_t
  #define  int16  int16_t
  #define  int32  int32_t
  #define  int64  int64_t
  #define  uint8  uint8_t
  #define uint16 uint16_t
  #define uint32 uint32_t
  #define uint64 uint64_t
  #define DSP4_CPP
  inline uint16 READ_WORD(uint8 *addr) {
    return (addr[0]) + (addr[1] << 8);
  }
  inline uint32 READ_DWORD(uint8 *addr) {
    return (addr[0]) + (addr[1] << 8) + (addr[2] << 16) + (addr[3] << 24);
  }
  inline void WRITE_WORD(uint8 *addr, uint16 data) {
    addr[0] = data;
    addr[1] = data >> 8;
  }
  #include "dsp4emu.h"
  #include "dsp4emu.c"
  #undef  bool8
  #undef   int8
  #undef  int16
  #undef  int32
  #undef  int64
  #undef  uint8
  #undef uint16
  #undef uint32
  #undef uint64
}

DSP4 dsp4;
#include "serialization.cpp"

auto DSP4::power() -> void {
  DSP4i::InitDSP4();
}

auto DSP4::read(uint addr, uint8 data) -> uint8 {
  if(addr & 1) return 0x80;

  DSP4i::dsp4_address = addr;
  DSP4i::DSP4GetByte();
  return DSP4i::dsp4_byte;
}

auto DSP4::write(uint addr, uint8 data) -> void {
  if(addr & 1) return;

  DSP4i::dsp4_address = addr;
  DSP4i::dsp4_byte = data;
  DSP4i::DSP4SetByte();
}

}
