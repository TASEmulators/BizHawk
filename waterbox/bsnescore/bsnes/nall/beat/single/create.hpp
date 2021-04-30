#pragma once

#include <nall/suffix-array.hpp>

namespace nall::Beat::Single {

inline auto create(array_view<uint8_t> source, array_view<uint8_t> target, string_view manifest = {}) -> vector<uint8_t> {
  vector<uint8_t> beat;

  auto write = [&](uint8_t data) {
    beat.append(data);
  };

  auto encode = [&](uint64_t data) {
    while(true) {
      uint64_t x = data & 0x7f;
      data >>= 7;
      if(data == 0) { write(0x80 | x); break; }
      write(x);
      data--;
    }
  };

  write('B'), write('P'), write('S'), write('1');
  encode(source.size()), encode(target.size()), encode(manifest.size());
  for(auto& byte : manifest) write(byte);

  //generating lrcp() arrays for source requires O(4n) computations, and O(16m) memory,
  //but it reduces find() complexity from O(n log m) to O(n + log m). and yet in practice,
  //no matter how large n scales to, the O(n + log m) find() is paradoxically slower.
  auto sourceArray = SuffixArray(source);
  auto targetArray = SuffixArray(target).lpf();

  enum : uint { SourceRead, TargetRead, SourceCopy, TargetCopy };
  uint outputOffset = 0, sourceRelativeOffset = 0, targetRelativeOffset = 0;

  uint targetReadLength = 0;
  auto flush = [&] {
    if(!targetReadLength) return;
    encode(TargetRead | ((targetReadLength - 1) << 2));
    uint offset = outputOffset - targetReadLength;
    while(targetReadLength) write(target[offset++]), targetReadLength--;
  };

  uint overlap = min(source.size(), target.size());
  while(outputOffset < target.size()) {
    uint mode = TargetRead, longestLength = 3, longestOffset = 0;
    int length = 0, offset = outputOffset;

    while(offset < overlap) {
      if(source[offset] != target[offset]) break;
      length++, offset++;
    }
    if(length > longestLength) {
      mode = SourceRead, longestLength = length;
    }

    sourceArray.find(length, offset, {target.data() + outputOffset, target.size() - outputOffset});
    if(length > longestLength) {
      mode = SourceCopy, longestLength = length, longestOffset = offset;
    }

    targetArray.previous(length, offset, outputOffset);
    if(length > longestLength) {
      mode = TargetCopy, longestLength = length, longestOffset = offset;
    }

    if(mode == TargetRead) {
      targetReadLength++;  //queue writes to group sequential commands
      outputOffset++;
    } else {
      flush();
      encode(mode | ((longestLength - 1) << 2));
      if(mode == SourceCopy) {
        int relativeOffset = longestOffset - sourceRelativeOffset;
        sourceRelativeOffset = longestOffset + longestLength;
        encode(relativeOffset < 0 | abs(relativeOffset) << 1);
      }
      if(mode == TargetCopy) {
        int relativeOffset = longestOffset - targetRelativeOffset;
        targetRelativeOffset = longestOffset + longestLength;
        encode(relativeOffset < 0 | abs(relativeOffset) << 1);
      }
      outputOffset += longestLength;
    }
  }
  flush();

  auto sourceHash = Hash::CRC32(source);
  for(uint shift : range(0, 32, 8)) write(sourceHash.value() >> shift);
  auto targetHash = Hash::CRC32(target);
  for(uint shift : range(0, 32, 8)) write(targetHash.value() >> shift);
  auto beatHash = Hash::CRC32(beat);
  for(uint shift : range(0, 32, 8)) write(beatHash.value() >> shift);

  return beat;
}

}
