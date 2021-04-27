#pragma once

#include <nall/decode/huffman.hpp>

namespace nall::Decode {

inline auto LZSA(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  uint index = 0;

  uint size = 0;
  for(uint byte : range(8)) size |= *input++ << byte * 8;
  output.resize(size);

  auto load = [&]() -> vector<uint8_t> {
    uint size = 0;
    for(uint byte : range(8)) size |= *input++ << byte * 8;
    vector<uint8_t> buffer;
    buffer.reserve(size);
    while(size--) buffer.append(*input++);
    return buffer;
  };

  auto flags = Decode::Huffman(load());
  auto literals = Decode::Huffman(load());
  auto lengths = Decode::Huffman(load());
  auto offsets = Decode::Huffman(load());

  auto flagData = flags.data();
  uint byte = 0, bits = 0;
  auto flagRead = [&]() -> bool {
    if(bits == 0) bits = 8, byte = *flagData++;
    return byte >> --bits & 1;
  };

  auto literalData = literals.data();
  auto literalRead = [&]() -> uint8_t {
    return *literalData++;
  };

  auto lengthData = lengths.data();
  auto lengthRead = [&]() -> uint64_t {
    uint byte = *lengthData++, bytes = 1;
    while(!(byte & 1)) byte >>= 1, bytes++;
    uint length = byte >> 1, shift = 8 - bytes;
    while(--bytes) length |= *lengthData++ << shift, shift += 8;
    return length;
  };

  auto offsetData = offsets.data();
  auto offsetRead = [&]() -> uint {
    uint offset = 0;
    offset |= *offsetData++ <<  0; if(index < 1 <<  8) return offset;
    offset |= *offsetData++ <<  8; if(index < 1 << 16) return offset;
    offset |= *offsetData++ << 16; if(index < 1 << 24) return offset;
    offset |= *offsetData++ << 24; return offset;
  };

  while(index < size) {
    if(!flagRead()) {
      output[index++] = literalRead();
    } else {
      uint length = lengthRead() + 6;
      uint offset = index - offsetRead();
      while(length--) output[index++] = output[offset++];
    }
  }

  return output;
}

}
