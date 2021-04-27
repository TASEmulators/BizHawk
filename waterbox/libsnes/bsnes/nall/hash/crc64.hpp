#pragma once

#include <nall/hash/hash.hpp>

namespace nall::Hash {

struct CRC64 : Hash {
  using Hash::input;

  CRC64(array_view<uint8_t> buffer = {}) {
    reset();
    input(buffer);
  }

  auto reset() -> void override {
    checksum = ~0;
  }

  auto input(uint8_t value) -> void override {
    checksum = (checksum >> 8) ^ table(checksum ^ value);
  }

  auto output() const -> vector<uint8_t> {
    vector<uint8_t> result;
    for(auto n : reverse(range(8))) result.append(~checksum >> n * 8);
    return result;
  }

  auto value() const -> uint64_t {
    return ~checksum;
  }

private:
  static auto table(uint8_t index) -> uint64_t {
    static uint64_t table[256] = {0};
    static bool initialized = false;

    if(!initialized) {
      initialized = true;
      for(auto index : range(256)) {
        uint64_t crc = index;
        for(auto bit : range(8)) {
          crc = (crc >> 1) ^ (crc & 1 ? 0xc96c'5795'd787'0f42 : 0);
        }
        table[index] = crc;
      }
    }

    return table[index];
  }

  uint64_t checksum = 0;
};

}
