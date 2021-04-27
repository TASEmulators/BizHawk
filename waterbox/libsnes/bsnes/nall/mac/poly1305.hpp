#pragma once

#include <nall/arithmetic.hpp>

namespace nall::MAC {

struct Poly1305 {
  auto authenticate(array_view<uint8_t> memory, uint256_t nonce) -> uint128_t {
    initialize(nonce);
    process(memory.data(), memory.size());
    return finish();
  }

  auto initialize(uint256_t key) -> void {
    uint64_t t0 = key >>  0;
    uint64_t t1 = key >> 64;
    pad[0] = key >> 128;
    pad[1] = key >> 192;

    r[0] = (t0                 ) & 0xffc0fffffff;
    r[1] = (t0 >> 44 | t1 << 20) & 0xfffffc0ffff;
    r[2] = (           t1 >> 24) & 0x00ffffffc0f;

    h[0] = 0, h[1] = 0, h[2] = 0;
    offset = 0;
  }

  auto process(const uint8_t* data, uint64_t size) -> void {
    while(size--) {
      buffer[offset++] = *data++;
      if(offset >= 16) {
        block();
        offset = 0;
      }
    }
  }

  auto finish() -> uint128_t {
    if(offset) {
      buffer[offset++] = 1;
      while(offset < 16) buffer[offset++] = 0;
      block(true);
    }

    uint64_t h0 = h[0], h1 = h[1], h2 = h[2];

        uint64_t c = h1 >> 44; h1 &= 0xfffffffffff;
    h2 += c;     c = h2 >> 42; h2 &= 0x3ffffffffff;
    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;     c = h1 >> 44; h1 &= 0xfffffffffff;
    h2 += c;     c = h2 >> 42; h2 &= 0x3ffffffffff;
    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;

    uint64_t g0 = h0 + 5; c = g0 >> 44; g0 &= 0xfffffffffff;
    uint64_t g1 = h1 + c; c = g1 >> 44; g1 &= 0xfffffffffff;
    uint64_t g2 = h2 + c - (1ull << 42);

    c = (g2 >> 63) - 1;
    g0 &= c, g1 &= c, g2 &= c;
    c = ~c;
    h0 = (h0 & c) | g0;
    h1 = (h1 & c) | g1;
    h2 = (h2 & c) | g2;

    uint64_t t0 = pad[0], t1 = pad[1];

    h0 += ((t0                 ) & 0xfffffffffff)    ; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += ((t0 >> 44 | t1 << 20) & 0xfffffffffff) + c; c = h1 >> 44; h1 &= 0xfffffffffff;
    h2 += ((           t1 >> 24) & 0x3ffffffffff) + c;               h2 &= 0x3ffffffffff;

    h0 = (h0 >>  0 | h1 << 44);
    h1 = (h1 >> 20 | h2 << 24);

    r[0] = 0, r[1] = 0, r[2] = 0;
    h[0] = 0, h[1] = 0, h[2] = 0;
    pad[0] = 0, pad[1] = 0;
    memory::fill(buffer, sizeof(buffer));
    offset = 0;

    return uint128_t(h1) << 64 | h0;
  }

private:
  auto block(bool last = false) -> void {
    uint64_t r0 = r[0], r1 = r[1], r2 = r[2];
    uint64_t h0 = h[0], h1 = h[1], h2 = h[2];

    uint64_t s1 = r1 * 20;
    uint64_t s2 = r2 * 20;

    uint64_t t0 = memory::readl<8>(buffer + 0);
    uint64_t t1 = memory::readl<8>(buffer + 8);

    h0 += ((t0                 ) & 0xfffffffffff);
    h1 += ((t0 >> 44 | t1 << 20) & 0xfffffffffff);
    h2 += ((           t1 >> 24) & 0x3ffffffffff) | (last ? 0 : 1ull << 40);

    uint128_t d, d0, d1, d2;
    d0 = (uint128_t)h0 * r0; d = (uint128_t)h1 * s2; d0 += d; d = (uint128_t)h2 * s1; d0 += d;
    d1 = (uint128_t)h0 * r1; d = (uint128_t)h1 * r0; d1 += d; d = (uint128_t)h2 * s2; d1 += d;
    d2 = (uint128_t)h0 * r2; d = (uint128_t)h1 * r1; d2 += d; d = (uint128_t)h2 * r0; d2 += d;

    uint64_t c = (uint64_t)(d0 >> 44); h0 = (uint64_t)d0 & 0xfffffffffff;
    d1 += c; c = (uint64_t)(d1 >> 44); h1 = (uint64_t)d1 & 0xfffffffffff;
    d2 += c; c = (uint64_t)(d2 >> 42); h2 = (uint64_t)d2 & 0x3ffffffffff;

    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;

    h[0] = h0, h[1] = h1, h[2] = h2;
  }

  uint64_t r[3];
  uint64_t h[3];
  uint64_t pad[2];

  uint8_t buffer[16];
  uint offset;
};

}
