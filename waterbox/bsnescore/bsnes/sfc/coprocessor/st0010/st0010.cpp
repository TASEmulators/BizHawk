#include <sfc/sfc.hpp>

namespace SuperFamicom {

#define ST0010_CPP
#include "data.hpp"
#include "opcodes.cpp"

ST0010 st0010;
#include "serialization.cpp"

auto ST0010::power() -> void {
  memset(ram, 0x00, sizeof ram);
}

auto ST0010::read(uint addr, uint8 data) -> uint8 {
  return readb(addr);
}

auto ST0010::write(uint addr, uint8 data) -> void {
  writeb(addr, data);

  if((addr & 0xfff) == 0x0021 && (data & 0x80)) {
    switch(ram[0x0020]) {
      case 0x01: op_01(); break;
      case 0x02: op_02(); break;
      case 0x03: op_03(); break;
      case 0x04: op_04(); break;
      case 0x05: op_05(); break;
      case 0x06: op_06(); break;
      case 0x07: op_07(); break;
      case 0x08: op_08(); break;
    }

    ram[0x0021] &= ~0x80;
  }
}

}
