#pragma once

//move to front

namespace nall::Decode {

inline auto MTF(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  output.resize(input.size());

  uint8_t order[256];
  for(uint n : range(256)) order[n] = n;

  for(uint offset : range(input.size())) {
    uint data = input[offset];
    uint value = order[data];
    output[offset] = value;
    memory::move(&order[1], &order[0], data);
    order[0] = value;
  }

  return output;
}

}
