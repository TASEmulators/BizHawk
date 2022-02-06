#pragma once

namespace nall::Encode {

inline auto Huffman(array_view<u8> input) -> vector<u8> {
  vector<u8> output;
  for(u32 byte : range(8)) output.append(input.size() >> byte * 8);

  struct Node {
    u32 frequency = 0;
    u32 parent = 0;
    u32 lhs = 0;
    u32 rhs = 0;
  };
  array<Node[512]> nodes;
  for(u32 offset : range(input.size())) nodes[input[offset]].frequency++;

  u32 count = 0;
  for(u32 offset : range(511)) {
    if(nodes[offset].frequency) count++;
    else nodes[offset].parent = 511;
  }

  auto minimum = [&] {
    u32 frequency = ~0, minimum = 511;
    for(u32 index : range(511)) {
      if(!nodes[index].parent && nodes[index].frequency && nodes[index].frequency < frequency) {
        frequency = nodes[index].frequency;
        minimum = index;
      }
    }
    return minimum;
  };

  //group the least two frequently used nodes until only one node remains
  u32 index = 256;
  for(u32 remaining = max(2, count); remaining >= 2; remaining--) {
    u32 lhs = minimum();
    nodes[lhs].parent = index;
    u32 rhs = minimum();
    nodes[rhs].parent = index;
    if(remaining == 2) index = nodes[lhs].parent = nodes[rhs].parent = 511;
    nodes[index].lhs = lhs;
    nodes[index].rhs = rhs;
    nodes[index].parent = 0;
    nodes[index].frequency = nodes[lhs].frequency + nodes[rhs].frequency;
    index++;
  }

  u32 byte = 0, bits = 0;
  auto write = [&](bool bit) {
    byte = byte << 1 | bit;
    if(++bits == 8) output.append(byte), bits = 0;
  };

  //only the upper half of the table is needed for decompression
  //the first 256 nodes are always treated as leaf nodes
  for(u32 offset : range(256)) {
    for(u32 index : reverse(range(9))) write(nodes[256 + offset].lhs >> index & 1);
    for(u32 index : reverse(range(9))) write(nodes[256 + offset].rhs >> index & 1);
  }

  for(u32 byte : input) {
    u32 node = byte, length = 0;
    u256 sequence = 0;
    //traversing the array produces the bitstream in reverse order
    do {
      u32 parent = nodes[node].parent;
      bool bit = nodes[nodes[node].parent].rhs == node;
      sequence = sequence << 1 | bit;
      length++;
      node = parent;
    } while(node != 511);
    //output the generated bits in the correct order
    for(u32 index : range(length)) {
      write(sequence >> index & 1);
    }
  }
  while(bits) write(0);

  return output;
}

}
