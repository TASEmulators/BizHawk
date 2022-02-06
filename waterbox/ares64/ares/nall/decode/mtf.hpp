#pragma once

//move to front

namespace nall::Decode {

inline auto MTF(array_view<u8> input) -> vector<u8> {
  vector<u8> output;
  output.resize(input.size());

  u8 order[256];
  for(u32 n : range(256)) order[n] = n;

  for(u32 offset : range(input.size())) {
    u32 data = input[offset];
    u32 value = order[data];
    output[offset] = value;
    memory::move(&order[1], &order[0], data);
    order[0] = value;
  }

  return output;
}

}
