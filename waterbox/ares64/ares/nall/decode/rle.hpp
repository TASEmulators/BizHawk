#pragma once

namespace nall::Decode {

template<u32 S = 1, u32 M = 4 / S>  //S = word size; M = match length
inline auto RLE(array_view<u8> input) -> vector<u8> {
  vector<u8> output;

  auto load = [&]() -> u8 {
    return input ? *input++ : 0;
  };

  u32 base = 0;
  u64 size = 0;
  for(u32 byte : range(8)) size |= load() << byte * 8;
  output.resize(size);

  auto read = [&]() -> u64 {
    u64 value = 0;
    for(u32 byte : range(S)) value |= load() << byte * 8;
    return value;
  };

  auto write = [&](u64 value) -> void {
    if(base >= size) return;
    for(u32 byte : range(S)) output[base++] = value >> byte * 8;
  };

  while(base < size) {
    auto byte = load();
    if(byte < 128) {
      byte++;
      while(byte--) write(read());
    } else {
      auto value = read();
      byte = (byte & 127) + M;
      while(byte--) write(value);
    }
  }

  return output;
}

}
