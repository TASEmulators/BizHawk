#pragma once

namespace nall::Decode {

inline auto Huffman(array_view<u8> input) -> vector<u8> {
  vector<u8> output;

  u32 size = 0;
  for(u32 byte : range(8)) size |= *input++ << byte * 8;
  output.reserve(size);

  u32 byte = 0, bits = 0;
  auto read = [&]() -> bool {
    if(bits == 0) bits = 8, byte = *input++;
    return byte >> --bits & 1;
  };

  u32 nodes[256][2] = {};
  for(u32 offset : range(256)) {
    for(u32 index : range(9)) nodes[offset][0] = nodes[offset][0] << 1 | read();
    for(u32 index : range(9)) nodes[offset][1] = nodes[offset][1] << 1 | read();
  }

  u32 node = 511;
  while(output.size() < size) {
    node = nodes[node - 256][read()];
    if(node < 256) {
      output.append(node);
      node = 511;
    }
  }

  return output;
}

}
