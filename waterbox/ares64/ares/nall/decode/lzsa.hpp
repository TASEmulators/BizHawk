#pragma once

#include <nall/decode/huffman.hpp>

namespace nall::Decode {

inline auto LZSA(array_view<u8> input) -> vector<u8> {
  vector<u8> output;
  u32 index = 0;

  u32 size = 0;
  for(u32 byte : range(8)) size |= *input++ << byte * 8;
  output.resize(size);

  auto load = [&]() -> vector<u8> {
    u32 size = 0;
    for(u32 byte : range(8)) size |= *input++ << byte * 8;
    vector<u8> buffer;
    buffer.reserve(size);
    while(size--) buffer.append(*input++);
    return buffer;
  };

  auto flags = Decode::Huffman(load());
  auto literals = Decode::Huffman(load());
  auto lengths = Decode::Huffman(load());
  auto offsets = Decode::Huffman(load());

  auto flagData = flags.data();
  u32  byte = 0, bits = 0;
  auto flagRead = [&]() -> bool {
    if(bits == 0) bits = 8, byte = *flagData++;
    return byte >> --bits & 1;
  };

  auto literalData = literals.data();
  auto literalRead = [&]() -> u8 {
    return *literalData++;
  };

  auto lengthData = lengths.data();
  auto lengthRead = [&]() -> u64 {
    u32 byte = *lengthData++, bytes = 1;
    while(!(byte & 1)) byte >>= 1, bytes++;
    u32 length = byte >> 1, shift = 8 - bytes;
    while(--bytes) length |= *lengthData++ << shift, shift += 8;
    return length;
  };

  auto offsetData = offsets.data();
  auto offsetRead = [&]() -> u32 {
    u32 offset = 0;
    offset |= *offsetData++ <<  0; if(index < 1 <<  8) return offset;
    offset |= *offsetData++ <<  8; if(index < 1 << 16) return offset;
    offset |= *offsetData++ << 16; if(index < 1 << 24) return offset;
    offset |= *offsetData++ << 24; return offset;
  };

  while(index < size) {
    if(!flagRead()) {
      output[index++] = literalRead();
    } else {
      u32 length = lengthRead() + 6;
      u32 offset = index - offsetRead();
      while(length--) output[index++] = output[offset++];
    }
  }

  return output;
}

}
