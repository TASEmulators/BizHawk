#pragma once

namespace Emulator::Memory {

inline auto mirror(uint address, uint size) -> uint {
  if(size == 0) return 0;
  uint base = 0;
  uint mask = 1 << 31;
  while(address >= size) {
    while(!(address & mask)) mask >>= 1;
    address -= mask;
    if(size > mask) {
      size -= mask;
      base += mask;
    }
    mask >>= 1;
  }
  return base + address;
}

inline auto reduce(uint address, uint mask) -> uint {
  while(mask) {
    uint bits = (mask & -mask) - 1;
    address = address >> 1 & ~bits | address & bits;
    mask = (mask & mask - 1) >> 1;
  }
  return address;
}

}
