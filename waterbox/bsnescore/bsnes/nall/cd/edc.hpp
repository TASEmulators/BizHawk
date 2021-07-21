#pragma once

//error detection code

namespace nall::CD::EDC {

//polynomial(x) = (x^16 + x^15 + x^2 + 1) * (x^16 + x^2 + x + 1)
inline auto polynomial(uint8_t x) -> uint32_t {
  static uint32_t lookup[256]{};
  static bool once = false;
  if(!once) { once = true;
    for(uint n : range(256)) {
      uint32_t edc = n;
      for(uint b : range(8)) edc = edc >> 1 ^ (edc & 1 ? 0xd8018001 : 0);
      lookup[n] = edc;
    }
  }
  return lookup[x];
}

//

inline auto create(array_view<uint8_t> input) -> uint32_t {
  uint32_t sum = 0;
  for(auto& byte : input) sum = sum >> 8 ^ polynomial(sum ^ byte);
  return sum;
}

inline auto create(array_view<uint8_t> input, array_span<uint8_t> output) -> bool {
  if(output.size() != 4) return false;
  auto sum = create(input);
  output[0] = sum >>  0;
  output[1] = sum >>  8;
  output[2] = sum >> 16;
  output[3] = sum >> 24;
  return true;
}

inline auto createMode1(array_span<uint8_t> sector) -> bool {
  if(sector.size() != 2352) return false;
  return create({sector, 2064}, {sector + 2064, 4});
}

//

inline auto verify(array_view<uint8_t> input, uint32_t edc) -> bool {
  return edc == create(input);
}

inline auto verify(array_view<uint8_t> input, array_view<uint8_t> compare) -> bool {
  if(compare.size() != 4) return false;
  auto sum = create(input);
  if(compare[0] != uint8_t(sum >>  0)) return false;
  if(compare[1] != uint8_t(sum >>  8)) return false;
  if(compare[2] != uint8_t(sum >> 16)) return false;
  if(compare[3] != uint8_t(sum >> 24)) return false;
  return true;
}

inline auto verifyMode1(array_view<uint8_t> sector) -> bool {
  if(sector.size() != 2352) return false;
  return verify({sector, 2064}, {sector + 2064, 4});
}

}
