#pragma once

#include <nall/hash/hash.hpp>

namespace nall::Hash {

struct CRC16 : Hash {
  using Hash::input;

  CRC16(array_view<uint8_t> buffer = {}) {
    reset();
    input(buffer);
  }

  auto reset() -> void override {
    checksum = ~0;
  }

  auto input(uint8_t value) -> void override {
    checksum = (checksum >> 8) ^ table(checksum ^ value);
  }

  auto output() const -> vector<uint8_t> override {
    vector<uint8_t> result;
    for(auto n : reverse(range(2))) result.append(~checksum >> n * 8);
    return result;
  }

  auto value() const -> uint16_t {
    return ~checksum;
  }

private:
  static auto table(uint8_t index) -> uint16_t {
    static uint16_t table[256] = {0};
    static bool initialized = false;

    if(!initialized) {
      initialized = true;
      for(auto index : range(256)) {
        uint16_t crc = index;
        for(auto bit : range(8)) {
          crc = (crc >> 1) ^ (crc & 1 ? 0x8408 : 0);
        }
        table[index] = crc;
      }
    }

    return table[index];
  }

  uint16_t checksum = 0;
};

}
