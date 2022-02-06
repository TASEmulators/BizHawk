#pragma once

namespace nall::CD::Scrambler {

//polynomial(x) = x^15 + x + 1
inline auto polynomial(u32 x) -> u8 {
  static u8 lookup[2340]{};
  static bool once = false;
  if(!once) { once = true;
    u16 shift = 0x0001;
    for(u32 n : range(2340)) {
      lookup[n] = shift;
      for(u32 b : range(8)) {
        bool carry = shift & 1 ^ shift >> 1 & 1;
        shift = (carry << 15 | shift) >> 1;
      }
    }
  }
  return lookup[x];
}

//

inline auto transform(array_span<u8> sector) -> bool {
  if(sector.size() == 2352) sector += 12;  //header is not scrambled
  if(sector.size() != 2340) return false;  //F1 frames only

  for(u32 index : range(2340)) {
    sector[index] ^= polynomial(index);
  }

  return true;
}

}
