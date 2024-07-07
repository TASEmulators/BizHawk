#pragma once

#include <nall/stdint.hpp>

namespace nall {

template<u32 bits, typename T> inline auto uclamp(T x) -> u64 {
  enum : u64 { b = 1ull << (bits - 1), y = b * 2 - 1 };
  if constexpr(is_unsigned_v<T>) {
    return y + ((x - y) & -(x < y));  //min(x, y);
  }
  if constexpr(is_signed_v<T>) {
    return x < 0 ? 0 : x > y ? y : x;
  }
}

template<u32 bits> inline auto uclip(u64 x) -> u64 {
  enum : u64 { b = 1ull << (bits - 1), m = b * 2 - 1 };
  return (x & m);
}

template<u32 bits> inline auto sclamp(s64 x) -> s64 {
  enum : s64 { b = 1ull << (bits - 1), m = b - 1 };
  return (x > m) ? m : (x < -b) ? -b : x;
}

template<u32 bits> inline auto sclip(s64 x) -> s64 {
  enum : u64 { b = 1ull << (bits - 1), m = b * 2 - 1 };
  return ((x & m) ^ b) - b;
}

namespace bit {
  constexpr inline auto mask(const char* s, u64 sum = 0) -> u64 {
    return (
      *s == '0' || *s == '1' ? mask(s + 1, (sum << 1) | 1) :
      *s == ' ' || *s == '_' ? mask(s + 1, sum) :
      *s ? mask(s + 1, sum << 1) :
      sum
    );
  }

  constexpr inline auto test(const char* s, u64 sum = 0) -> u64 {
    return (
      *s == '0' || *s == '1' ? test(s + 1, (sum << 1) | (*s - '0')) :
      *s == ' ' || *s == '_' ? test(s + 1, sum) :
      *s ? test(s + 1, sum << 1) :
      sum
    );
  }

  //lowest(0b1110) == 0b0010
  constexpr inline auto lowest(const u64 x) -> u64 {
    return x & -x;
  }

  //clear_lowest(0b1110) == 0b1100
  constexpr inline auto clearLowest(const u64 x) -> u64 {
    return x & (x - 1);
  }

  //set_lowest(0b0101) == 0b0111
  constexpr inline auto setLowest(const u64 x) -> u64 {
    return x | (x + 1);
  }

  //count number of bits set in a byte
  constexpr inline auto count(u64 x) -> u32 {
    u32 count = 0;
    while(x) x &= x - 1, count++;  //clear the least significant bit
    return count;
  }

  //return index of the first bit set (or zero of no bits are set)
  //first(0b1000) == 3
  constexpr inline auto first(u64 x) -> u32 {
    u32 first = 0;
    while(x) { if(x & 1) break; x >>= 1; first++; }
    return first;
  }

  //return index of the last bit set (or zero of no bits are set)
  //last(0b11000) == 4
  constexpr inline auto last(u64 x) -> u32 {
    u32 i = 0;
    while(x) { x >>= 1; i++; }
    return i > 0 ? --i : i;
  }

  //round up to next highest single bit:
  //round(15) == 16, round(16) == 16, round(17) == 32
  constexpr inline auto round(u64 x) -> u64 {
    if((x & (x - 1)) == 0) return x;
    while(x & (x - 1)) x &= x - 1;
    return x << 1;
  }

  template<typename T>
  constexpr inline auto reverse(T x) -> T {
    static_assert(sizeof(T) == 1 || sizeof(T) == 2 || sizeof(T) == 4 || sizeof(T) == 8);
    if constexpr(sizeof(T) == 1) {
      #if __has_builtin(__builtin_bitreverse8)
      return __builtin_bitreverse8(x);
      #else
      x = (x & 0xaa) >> 1 | (x & 0x55) << 1;
      x = (x & 0xcc) >> 2 | (x & 0x33) << 2;
      x = (x & 0xf0) >> 4 | (x & 0x0f) << 4;
      return x;
      #endif
    }
    if constexpr(sizeof(T) == 2) {
      #if __has_builtin(__builtin_bitreverse16)
      return __builtin_bitreverse16(x);
      #else
      x = (x & 0xaaaa) >> 1 | (x & 0x5555) << 1;
      x = (x & 0xcccc) >> 2 | (x & 0x3333) << 2;
      x = (x & 0xf0f0) >> 4 | (x & 0x0f0f) << 4;
      x = (x & 0xff00) >> 8 | (x & 0x00ff) << 8;
      return x;
      #endif
    }
    if constexpr(sizeof(T) == 4) {
      #if __has_builtin(__builtin_bitreverse32)
      return __builtin_bitreverse32(x);
      #else
      x = (x & 0xaaaaaaaa) >>  1 | (x & 0x55555555) <<  1;
      x = (x & 0xcccccccc) >>  2 | (x & 0x33333333) <<  2;
      x = (x & 0xf0f0f0f0) >>  4 | (x & 0x0f0f0f0f) <<  4;
      x = (x & 0xff00ff00) >>  8 | (x & 0x00ff00ff) <<  8;
      x = (x & 0xffff0000) >> 16 | (x & 0x0000ffff) << 16;
      return x;
      #endif
    }
    if constexpr(sizeof(T) == 8) {
      #if __has_builtin(__builtin_bitreverse64)
      return __builtin_bitreverse64(x);
      #else
      x = (x & 0xaaaaaaaaaaaaaaaaULL) >>  1 | (x & 0x5555555555555555ULL) <<  1;
      x = (x & 0xccccccccccccccccULL) >>  2 | (x & 0x3333333333333333ULL) <<  2;
      x = (x & 0xf0f0f0f0f0f0f0f0ULL) >>  4 | (x & 0x0f0f0f0f0f0f0f0fULL) <<  4;
      x = (x & 0xff00ff00ff00ff00ULL) >>  8 | (x & 0x00ff00ff00ff00ffULL) <<  8;
      x = (x & 0xffff0000ffff0000ULL) >> 16 | (x & 0x0000ffff0000ffffULL) << 16;
      x = (x & 0xffffffff00000000ULL) >> 32 | (x & 0x00000000ffffffffULL) << 32;
      return x;
      #endif
    }
  }
}

}
