#pragma once

#include <nall/suffix-array.hpp>
#include <nall/encode/bwt.hpp>
#include <nall/encode/huffman.hpp>
#include <nall/encode/mtf.hpp>
#include <nall/encode/rle.hpp>

namespace nall::Encode {

inline auto LZSA(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  for(uint byte : range(8)) output.append(input.size() >> byte * 8);

  auto suffixArray = SuffixArray(input).lpf();
  uint index = 0;
  vector<uint8_t> flags;
  vector<uint8_t> literals;
  vector<uint8_t> stringLengths;
  vector<uint8_t> stringOffsets;

  uint byte = 0, bits = 0;
  auto flagWrite = [&](bool bit) {
    byte = byte << 1 | bit;
    if(++bits == 8) flags.append(byte), bits = 0;
  };

  auto literalWrite = [&](uint8_t literal) {
    literals.append(literal);
  };

  auto lengthWrite = [&](uint64_t length) {
         if(length < 1 <<  7) length = length << 1 |     0b1;
    else if(length < 1 << 14) length = length << 2 |    0b10;
    else if(length < 1 << 21) length = length << 3 |   0b100;
    else if(length < 1 << 28) length = length << 4 |  0b1000;
    else  /*length < 1 << 35*/length = length << 5 | 0b10000;
    while(length) stringLengths.append(length), length >>= 8;
  };

  auto offsetWrite = [&](uint offset) {
    stringOffsets.append(offset >>  0); if(index < 1 <<  8) return;
    stringOffsets.append(offset >>  8); if(index < 1 << 16) return;
    stringOffsets.append(offset >> 16); if(index < 1 << 24) return;
    stringOffsets.append(offset >> 24);
  };

  while(index < input.size()) {
    int length, offset;
    suffixArray.previous(length, offset, index);

/*  for(uint ahead = 1; ahead <= 2; ahead++) {
      int aheadLength, aheadOffset;
      suffixArray.previous(aheadLength, aheadOffset, index + ahead);
      if(aheadLength > length && aheadOffset >= 0) {
        length = 0;
        break;
      }
    } */

    if(length < 6 || offset < 0) {
      flagWrite(0);
      literalWrite(input[index++]);
    } else {
      flagWrite(1);
      lengthWrite(length - 6);
      offsetWrite(index - offset);
      index += length;
    }
  }
  while(bits) flagWrite(0);

  auto save = [&](const vector<uint8_t>& buffer) {
    for(uint byte : range(8)) output.append(buffer.size() >> byte * 8);
    output.append(buffer);
  };

  save(Encode::Huffman(flags));
  save(Encode::Huffman(literals));
  save(Encode::Huffman(stringLengths));
  save(Encode::Huffman(stringOffsets));

  return output;
}

}
