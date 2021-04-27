#pragma once

namespace nall::Encode {

inline auto Huffman(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  for(uint byte : range(8)) output.append(input.size() >> byte * 8);

  struct Node {
    uint frequency = 0;
    uint parent = 0;
    uint lhs = 0;
    uint rhs = 0;
  };
  array<Node[512]> nodes;
  for(uint offset : range(input.size())) nodes[input[offset]].frequency++;

  uint count = 0;
  for(uint offset : range(511)) {
    if(nodes[offset].frequency) count++;
    else nodes[offset].parent = 511;
  }

  auto minimum = [&] {
    uint frequency = ~0, minimum = 511;
    for(uint index : range(511)) {
      if(!nodes[index].parent && nodes[index].frequency && nodes[index].frequency < frequency) {
        frequency = nodes[index].frequency;
        minimum = index;
      }
    }
    return minimum;
  };

  //group the least two frequently used nodes until only one node remains
  uint index = 256;
  for(uint remaining = max(2, count); remaining >= 2; remaining--) {
    uint lhs = minimum();
    nodes[lhs].parent = index;
    uint rhs = minimum();
    nodes[rhs].parent = index;
    if(remaining == 2) index = nodes[lhs].parent = nodes[rhs].parent = 511;
    nodes[index].lhs = lhs;
    nodes[index].rhs = rhs;
    nodes[index].parent = 0;
    nodes[index].frequency = nodes[lhs].frequency + nodes[rhs].frequency;
    index++;
  }

  uint byte = 0, bits = 0;
  auto write = [&](bool bit) {
    byte = byte << 1 | bit;
    if(++bits == 8) output.append(byte), bits = 0;
  };

  //only the upper half of the table is needed for decompression
  //the first 256 nodes are always treated as leaf nodes
  for(uint offset : range(256)) {
    for(uint index : reverse(range(9))) write(nodes[256 + offset].lhs >> index & 1);
    for(uint index : reverse(range(9))) write(nodes[256 + offset].rhs >> index & 1);
  }

  for(uint byte : input) {
    uint node = byte, length = 0;
    uint256_t sequence = 0;
    //traversing the array produces the bitstream in reverse order
    do {
      uint parent = nodes[node].parent;
      bool bit = nodes[nodes[node].parent].rhs == node;
      sequence = sequence << 1 | bit;
      length++;
      node = parent;
    } while(node != 511);
    //output the generated bits in the correct order
    for(uint index : range(length)) {
      write(sequence >> index & 1);
    }
  }
  while(bits) write(0);

  return output;
}

}
