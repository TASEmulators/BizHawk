#pragma once

#include <nall/primitives.hpp>
#include <nall/serializer.hpp>
#include <nall/stdint.hpp>

namespace nall {

struct varint {
  virtual auto read() -> u8 = 0;
  virtual auto write(u8) -> void = 0;

  auto readvu() -> u64 {
    u64 data = 0, shift = 1;
    while(true) {
      u8 x = read();
      data += (x & 0x7f) * shift;
      if(x & 0x80) break;
      shift <<= 7;
      data += shift;
    }
    return data;
  }

  auto readvs() -> s64 {
    u64 data = readvu();
    bool negate = data & 1;
    data >>= 1;
    if(negate) data = ~data;
    return data;
  }

  auto writevu(u64 data) -> void {
    while(true) {
      u8 x = data & 0x7f;
      data >>= 7;
      if(data == 0) return write(0x80 | x);
      write(x);
      data--;
    }
  }

  auto writevs(s64 data) -> void {
    bool negate = data < 0;
    if(negate) data = ~data;
    data = (data << 1) | negate;
    writevu(data);
  }
};

struct VariadicNatural {
  VariadicNatural() : mask(~0ull) { assign(0); }
  template<typename T> VariadicNatural(const T& value) : mask(~0ull) { assign(value); }

  operator u64() const { return data; }
  template<typename T> auto& operator=(const T& value) { return assign(value); }

  auto operator++(s32) { auto value = data; assign(data + 1); return value; }
  auto operator--(s32) { auto value = data; assign(data - 1); return value; }

  auto& operator++() { return assign(data + 1); }
  auto& operator--() { return assign(data - 1); }

  auto& operator &=(const u64 value) { return assign(data  & value); }
  auto& operator |=(const u64 value) { return assign(data  | value); }
  auto& operator ^=(const u64 value) { return assign(data  ^ value); }
  auto& operator<<=(const u64 value) { return assign(data << value); }
  auto& operator>>=(const u64 value) { return assign(data >> value); }
  auto& operator +=(const u64 value) { return assign(data  + value); }
  auto& operator -=(const u64 value) { return assign(data  - value); }
  auto& operator *=(const u64 value) { return assign(data  * value); }
  auto& operator /=(const u64 value) { return assign(data  / value); }
  auto& operator %=(const u64 value) { return assign(data  % value); }

  auto resize(u32 bits) {
    assert(bits <= 64);
    mask = ~0ull >> (64 - bits);
    data &= mask;
  }

  auto serialize(serializer& s) {
    s(data);
    s(mask);
  }

  struct Reference {
    Reference(VariadicNatural& self, u32 lo, u32 hi) : self(self), Lo(lo), Hi(hi) {}

    operator u64() const {
      const u64 RangeBits = Hi - Lo + 1;
      const u64 RangeMask = (((1ull << RangeBits) - 1) << Lo) & self.mask;
      return (self & RangeMask) >> Lo;
    }

    auto& operator=(const u64 value) {
      const u64 RangeBits = Hi - Lo + 1;
      const u64 RangeMask = (((1ull << RangeBits) - 1) << Lo) & self.mask;
      self.data = (self.data & ~RangeMask) | ((value << Lo) & RangeMask);
      return *this;
    }

  private:
    VariadicNatural& self;
    const u32 Lo;
    const u32 Hi;
  };

  auto bits(u32 lo, u32 hi) -> Reference { return {*this, lo < hi ? lo : hi, hi > lo ? hi : lo}; }
  auto bit(u32 index) -> Reference { return {*this, index, index}; }
  auto byte(u32 index) -> Reference { return {*this, index * 8 + 0, index * 8 + 7}; }

private:
  auto assign(u64 value) -> VariadicNatural& {
    data = value & mask;
    return *this;
  }

  u64 data;
  u64 mask;
};

}
