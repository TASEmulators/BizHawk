#pragma once

namespace nall::Encode {

template<uint S = 1, uint M = 4 / S>  //S = word size; M = match length
inline auto RLE(array_view<uint8_t> input) -> vector<uint8_t> {
  vector<uint8_t> output;
  for(uint byte : range(8)) output.append(input.size() >> byte * 8);

  uint base = 0;
  uint skip = 0;

  auto load = [&](uint offset) -> uint8_t {
    return input(offset);
  };

  auto read = [&](uint offset) -> uint64_t {
    uint64_t value = 0;
    for(uint byte : range(S)) value |= load(offset + byte) << byte * 8;
    return value;
  };

  auto write = [&](uint64_t value) -> void {
    for(uint byte : range(S)) output.append(value >> byte * 8);
  };

  auto flush = [&] {
    output.append(skip - 1);
    do {
      write(read(base));
      base += S;
    } while(--skip);
  };

  while(base + S * skip < input.size()) {
    uint same = 1;
    for(uint offset = base + S * (skip + 1); offset < input.size(); offset += S) {
      if(read(offset) != read(base + S * skip)) break;
      if(++same == 127 + M) break;
    }

    if(same < M) {
      if(++skip == 128) flush();
    } else {
      if(skip) flush();
      output.append(128 | same - M);
      write(read(base));
      base += S * same;
    }
  }
  if(skip) flush();

  return output;
}

}
