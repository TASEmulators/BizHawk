#pragma once

//error detection code

namespace nall::CD::EDC {

//polynomial(x) = (x^16 + x^15 + x^2 + 1) * (x^16 + x^2 + x + 1)
inline auto polynomial(u8 x) -> u32 {
  static u32 lookup[256]{};
  static bool once = false;
  if(!once) { once = true;
    for(u32 n : range(256)) {
      u32 edc = n;
      for(u32 b : range(8)) edc = edc >> 1 ^ (edc & 1 ? 0xd8018001 : 0);
      lookup[n] = edc;
    }
  }
  return lookup[x];
}

//

inline auto create(array_view<u8> input) -> u32 {
  u32 sum = 0;
  for(auto& byte : input) sum = sum >> 8 ^ polynomial(sum ^ byte);
  return sum;
}

inline auto create(array_view<u8> input, array_span<u8> output) -> bool {
  if(output.size() != 4) return false;
  auto sum = create(input);
  output[0] = sum >>  0;
  output[1] = sum >>  8;
  output[2] = sum >> 16;
  output[3] = sum >> 24;
  return true;
}

inline auto createMode1(array_span<u8> sector) -> bool {
  if(sector.size() != 2352) return false;
  return create({sector.data(), 2064}, {sector.data() + 2064, 4});
}

//

inline auto verify(array_view<u8> input, u32 edc) -> bool {
  return edc == create(input);
}

inline auto verify(array_view<u8> input, array_view<u8> compare) -> bool {
  if(compare.size() != 4) return false;
  auto sum = create(input);
  if(compare[0] != u8(sum >>  0)) return false;
  if(compare[1] != u8(sum >>  8)) return false;
  if(compare[2] != u8(sum >> 16)) return false;
  if(compare[3] != u8(sum >> 24)) return false;
  return true;
}

inline auto verifyMode1(array_view<u8> sector) -> bool {
  if(sector.size() != 2352) return false;
  return verify({sector.data(), 2064}, {sector.data() + 2064, 4});
}

}
