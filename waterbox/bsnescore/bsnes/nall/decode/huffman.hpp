#pragma once

namespace nall::Decode {

inline auto Huffman(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;

  uint size = 0;
  for(uint byte : range(8)) size |= *input++ << byte * 8;
  output.reserve(size);

  uint byte = 0, bits = 0;
  auto read = [&]() -> bool {
    if(bits == 0) bits = 8, byte = *input++;
    return byte >> --bits & 1;
  };

  uint nodes[256][2] = {};
  for(uint offset : range(256)) {
    for(uint index : range(9)) nodes[offset][0] = nodes[offset][0] << 1 | read();
    for(uint index : range(9)) nodes[offset][1] = nodes[offset][1] << 1 | read();
  }

  uint node = 511;
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
