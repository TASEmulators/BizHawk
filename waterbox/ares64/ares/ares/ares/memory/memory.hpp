#pragma once

namespace ares::Memory {

inline auto mirror(u32 address, u32 size) -> u32 {
  if(size == 0) return 0;
  u32 base = 0;
  u32 mask = 1 << 31;
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

inline auto reduce(u32 address, u32 mask) -> u32 {
  while(mask) {
    u32 bits = (mask & -mask) - 1;
    address = address >> 1 & ~bits | address & bits;
    mask = (mask & mask - 1) >> 1;
  }
  return address;
}

}
