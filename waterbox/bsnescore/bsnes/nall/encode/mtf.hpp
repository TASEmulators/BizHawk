#pragma once

//move to front

namespace nall::Encode {

inline auto MTF(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  output.resize(input.size());

  uint8_t order[256];
  for(uint n : range(256)) order[n] = n;

  for(uint offset : range(input.size())) {
    uint data = input[offset];
    for(uint index : range(256)) {
      uint value = order[index];
      if(value == data) {
        output[offset] = index;
        memory::move(&order[1], &order[0], index);
        order[0] = value;
        break;
      }
    }
  }

  return output;
}

}
