#pragma once

#include <nall/arithmetic.hpp>
#include <nall/array-view.hpp>

namespace nall::Cipher {

//64-bit nonce; 64-bit x 64-byte (256GB) counter
struct ChaCha20 {
  ChaCha20(uint256_t key, uint64_t nonce, uint64_t counter = 0) {
    static const uint128_t sigma = 0x6b20657479622d323320646e61707865_u128;  //"expand 32-byte k"

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

  auto encrypt(array_view<uint8_t> input) -> vector<uint8_t> {
    vector<uint8_t> output;
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

  auto decrypt(array_view<uint8_t> input) -> vector<uint8_t> {
    return encrypt(input);  //reciprocal cipher
  }

//protected:
  inline auto rol(uint32_t value, uint bits) -> uint32_t {
    return value << bits | value >> 32 - bits;
  }

  auto quarterRound(uint32_t x[16], uint a, uint b, uint c, uint d) -> void {
    x[a] += x[b]; x[d] = rol(x[d] ^ x[a], 16);
    x[c] += x[d]; x[b] = rol(x[b] ^ x[c], 12);
    x[a] += x[b]; x[d] = rol(x[d] ^ x[a],  8);
    x[c] += x[d]; x[b] = rol(x[b] ^ x[c],  7);
  }

  auto cipher() -> void {
    memory::copy(block, input, 64);
    for(uint n : range(10)) {
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
    for(uint n : range(16)) {
      block[n] += input[n];
    }
    if(!++input[12]) ++input[13];
  }

  uint32_t input[16];
  uint32_t block[16];
  uint64_t offset;
};

struct HChaCha20 : protected ChaCha20 {
  HChaCha20(uint256_t key, uint128_t nonce) : ChaCha20(key, nonce >> 64, nonce >> 0) {
    cipher();
  }

  auto key() const -> uint256_t {
    uint256_t key = 0;
    for(uint n : range(4)) key |= (uint256_t)block[ 0 + n] << (n + 0) * 32;
    for(uint n : range(4)) key |= (uint256_t)block[12 + n] << (n + 4) * 32;
    return key;
  }
};

//192-bit nonce; 64-bit x 64-byte (256GB) counter
struct XChaCha20 : ChaCha20 {
  XChaCha20(uint256_t key, uint192_t nonce, uint64_t counter = 0):
  ChaCha20(HChaCha20(key, nonce).key(), nonce >> 128, counter) {
  }
};

}
