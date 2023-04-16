#pragma once

#include <nall/arithmetic.hpp>
#include <nall/array-view.hpp>

namespace nall::Cipher {

//64-bit nonce; 64-bit x 64-byte (256GB) counter
struct ChaCha20 {
  ChaCha20(u256 key, u64 nonce, u64 counter = 0) {
    static const u128 sigma = 0x6b20657479622d323320646e61707865_u128;  //"expand 32-byte k"

    input[ 0] = sigma   >>   0;
    input[ 1] = sigma   >>  32;
    input[ 2] = sigma   >>  64;
    input[ 3] = sigma   >>  96;
    input[ 4] = key     >>   0;
    input[ 5] = key     >>  32;
    input[ 6] = key     >>  64;
    input[ 7] = key     >>  96;
    input[ 8] = key     >> 128;
    input[ 9] = key     >> 160;
    input[10] = key     >> 192;
    input[11] = key     >> 224;
    input[12] = counter >>   0;
    input[13] = counter >>  32;
    input[14] = nonce   >>   0;
    input[15] = nonce   >>  32;

    offset = 0;
  }

  auto encrypt(array_view<u8> input) -> vector<u8> {
    vector<u8> output;
    while(input) {
      if(!offset) {
        cipher();
        increment();
      }
      auto byte = offset++;
      output.append(*input++ ^ (block[byte >> 2] >> (byte & 3) * 8));
      offset &= 63;
    }
    return output;
  }

  auto decrypt(array_view<u8> input) -> vector<u8> {
    return encrypt(input);  //reciprocal cipher
  }

//protected:
  auto rol(u32 value, u32 bits) -> u32 {
    return value << bits | value >> 32 - bits;
  }

  auto quarterRound(u32 x[16], u32 a, u32 b, u32 c, u32 d) -> void {
    x[a] += x[b]; x[d] = rol(x[d] ^ x[a], 16);
    x[c] += x[d]; x[b] = rol(x[b] ^ x[c], 12);
    x[a] += x[b]; x[d] = rol(x[d] ^ x[a],  8);
    x[c] += x[d]; x[b] = rol(x[b] ^ x[c],  7);
  }

  auto cipher() -> void {
    memory::copy(block, input, 64);
    for(u32 n : range(10)) {
      quarterRound(block, 0, 4,  8, 12);
      quarterRound(block, 1, 5,  9, 13);
      quarterRound(block, 2, 6, 10, 14);
      quarterRound(block, 3, 7, 11, 15);
      quarterRound(block, 0, 5, 10, 15);
      quarterRound(block, 1, 6, 11, 12);
      quarterRound(block, 2, 7,  8, 13);
      quarterRound(block, 3, 4,  9, 14);
    }
  }

  auto increment() -> void {
    for(u32 n : range(16)) {
      block[n] += input[n];
    }
    if(!++input[12]) ++input[13];
  }

  u32 input[16];
  u32 block[16];
  u64 offset;
};

struct HChaCha20 : protected ChaCha20 {
  HChaCha20(u256 key, u128 nonce) : ChaCha20(key, nonce >> 64, nonce >> 0) {
    cipher();
  }

  auto key() const -> u256 {
    u256 key = 0;
    for(u32 n : range(4)) key |= (u256)block[ 0 + n] << (n + 0) * 32;
    for(u32 n : range(4)) key |= (u256)block[12 + n] << (n + 4) * 32;
    return key;
  }
};

//192-bit nonce; 64-bit x 64-byte (256GB) counter
struct XChaCha20 : ChaCha20 {
  XChaCha20(u256 key, u192 nonce, u64 counter = 0):
  ChaCha20(HChaCha20(key, nonce).key(), nonce >> 128, counter) {
  }
};

}
