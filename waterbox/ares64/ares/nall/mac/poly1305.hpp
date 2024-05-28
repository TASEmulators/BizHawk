#pragma once

#include <nall/arithmetic.hpp>

namespace nall::MAC {

struct Poly1305 {
  auto authenticate(array_view<u8> memory, u256 nonce) -> u128 {
    initialize(nonce);
    process(memory.data(), memory.size());
    return finish();
  }

  auto initialize(u256 key) -> void {
    u64 t0 = key >>  0;
    u64 t1 = key >> 64;
    pad[0] = key >> 128;
    pad[1] = key >> 192;

    r[0] = (t0                 ) & 0xffc0fffffff;
    r[1] = (t0 >> 44 | t1 << 20) & 0xfffffc0ffff;
    r[2] = (           t1 >> 24) & 0x00ffffffc0f;

    h[0] = 0, h[1] = 0, h[2] = 0;
    offset = 0;
  }

  auto process(const u8* data, u64 size) -> void {
    while(size--) {
      buffer[offset++] = *data++;
      if(offset >= 16) {
        block();
        offset = 0;
      }
    }
  }

  auto finish() -> u128 {
    if(offset) {
      buffer[offset++] = 1;
      while(offset < 16) buffer[offset++] = 0;
      block(true);
    }

    u64 h0 = h[0], h1 = h[1], h2 = h[2];

             u64 c = h1 >> 44; h1 &= 0xfffffffffff;
    h2 += c;     c = h2 >> 42; h2 &= 0x3ffffffffff;
    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;     c = h1 >> 44; h1 &= 0xfffffffffff;
    h2 += c;     c = h2 >> 42; h2 &= 0x3ffffffffff;
    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;

    u64 g0 = h0 + 5; c = g0 >> 44; g0 &= 0xfffffffffff;
    u64 g1 = h1 + c; c = g1 >> 44; g1 &= 0xfffffffffff;
    u64 g2 = h2 + c - (1ull << 42);

    c = (g2 >> 63) - 1;
    g0 &= c, g1 &= c, g2 &= c;
    c = ~c;
    h0 = (h0 & c) | g0;
    h1 = (h1 & c) | g1;
    h2 = (h2 & c) | g2;

    u64 t0 = pad[0], t1 = pad[1];

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

    return u128(h1) << 64 | h0;
  }

private:
  auto block(bool last = false) -> void {
    u64 r0 = r[0], r1 = r[1], r2 = r[2];
    u64 h0 = h[0], h1 = h[1], h2 = h[2];

    u64 s1 = r1 * 20;
    u64 s2 = r2 * 20;

    u64 t0 = memory::readl<8>(buffer + 0);
    u64 t1 = memory::readl<8>(buffer + 8);

    h0 += ((t0                 ) & 0xfffffffffff);
    h1 += ((t0 >> 44 | t1 << 20) & 0xfffffffffff);
    h2 += ((           t1 >> 24) & 0x3ffffffffff) | (last ? 0 : 1ull << 40);

    u128 d, d0, d1, d2;
    d0 = (u128)h0 * r0; d = (u128)h1 * s2; d0 += d; d = (u128)h2 * s1; d0 += d;
    d1 = (u128)h0 * r1; d = (u128)h1 * r0; d1 += d; d = (u128)h2 * s2; d1 += d;
    d2 = (u128)h0 * r2; d = (u128)h1 * r1; d2 += d; d = (u128)h2 * r0; d2 += d;

         u64 c = (u64)(d0 >> 44); h0 = (u64)d0 & 0xfffffffffff;
    d1 += c; c = (u64)(d1 >> 44); h1 = (u64)d1 & 0xfffffffffff;
    d2 += c; c = (u64)(d2 >> 42); h2 = (u64)d2 & 0x3ffffffffff;

    h0 += c * 5; c = h0 >> 44; h0 &= 0xfffffffffff;
    h1 += c;

    h[0] = h0, h[1] = h1, h[2] = h2;
  }

  u64 r[3];
  u64 h[3];
  u64 pad[2];

  u8  buffer[16];
  u32 offset;
};

}
