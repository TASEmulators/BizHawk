#pragma once

//move to front

namespace nall::Encode {

inline auto MTF(array_view<u8> input) -> vector<u8> {
  vector<u8> output;
  output.resize(input.size());

  u8 order[256];
  for(u32 n : range(256)) order[n] = n;

  for(u32 offset : range(input.size())) {
    u32 data = input[offset];
    for(u32 index : range(256)) {
      u32 value = order[index];
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
