#pragma once

#include <nall/primitives.hpp>
#include <nall/serializer.hpp>
#include <nall/stdint.hpp>

namespace nall {

struct varint {
  virtual auto read() -> uint8_t = 0;
  virtual auto write(uint8_t) -> void = 0;

  auto readvu() -> uintmax {
    uintmax data = 0, shift = 1;
    while(true) {
      uint8_t x = read();
      data += (x & 0x7f) * shift;
      if(x & 0x80) break;
      shift <<= 7;
      data += shift;
    }
    return data;
  }

  auto readvs() -> intmax {
    uintmax data = readvu();
    bool negate = data & 1;
    data >>= 1;
    if(negate) data = ~data;
    return data;
  }

  auto writevu(uintmax data) -> void {
    while(true) {
      uint8_t x = data & 0x7f;
      data >>= 7;
      if(data == 0) return write(0x80 | x);
      write(x);
      data--;
    }
  }

  auto writevs(intmax data) -> void {
    bool negate = data < 0;
    if(negate) data = ~data;
    data = (data << 1) | negate;
    writevu(data);
  }
};

struct VariadicNatural {
  inline VariadicNatural() : mask(~0ull) { assign(0); }
  template<typename T> inline VariadicNatural(const T& value) : mask(~0ull) { assign(value); }

  inline operator uint64_t() const { return data; }
  template<typename T> inline auto& operator=(const T& value) { return assign(value); }

  inline auto operator++(int) { auto value = data; assign(data + 1); return value; }
  inline auto operator--(int) { auto value = data; assign(data - 1); return value; }

  inline auto& operator++() { return assign(data + 1); }
  inline auto& operator--() { return assign(data - 1); }

  inline auto& operator &=(const uint64_t value) { return assign(data  & value); }
  inline auto& operator |=(const uint64_t value) { return assign(data  | value); }
  inline auto& operator ^=(const uint64_t value) { return assign(data  ^ value); }
  inline auto& operator<<=(const uint64_t value) { return assign(data << value); }
  inline auto& operator>>=(const uint64_t value) { return assign(data >> value); }
  inline auto& operator +=(const uint64_t value) { return assign(data  + value); }
  inline auto& operator -=(const uint64_t value) { return assign(data  - value); }
  inline auto& operator *=(const uint64_t value) { return assign(data  * value); }
  inline auto& operator /=(const uint64_t value) { return assign(data  / value); }
  inline auto& operator %=(const uint64_t value) { return assign(data  % value); }

  inline auto resize(uint bits) {
    assert(bits <= 64);
    mask = ~0ull >> (64 - bits);
    data &= mask;
  }

  inline auto serialize(serializer& s) {
    s(data);
    s(mask);
  }

  struct Reference {
    inline Reference(VariadicNatural& self, uint lo, uint hi) : self(self), Lo(lo), Hi(hi) {}

    inline operator uint64_t() const {
      const uint64_t RangeBits = Hi - Lo + 1;
      const uint64_t RangeMask = (((1ull << RangeBits) - 1) << Lo) & self.mask;
      return (self & RangeMask) >> Lo;
    }

    inline auto& operator=(const uint64_t value) {
      const uint64_t RangeBits = Hi - Lo + 1;
      const uint64_t RangeMask = (((1ull << RangeBits) - 1) << Lo) & self.mask;
      self.data = (self.data & ~RangeMask) | ((value << Lo) & RangeMask);
      return *this;
    }

  private:
    VariadicNatural& self;
    const uint Lo;
    const uint Hi;
  };

  inline auto bits(uint lo, uint hi) -> Reference { return {*this, lo < hi ? lo : hi, hi > lo ? hi : lo}; }
  inline auto bit(uint index) -> Reference { return {*this, index, index}; }
  inline auto byte(uint index) -> Reference { return {*this, index * 8 + 0, index * 8 + 7}; }

private:
  auto assign(uint64_t value) -> VariadicNatural& {
    data = value & mask;
    return *this;
  }

  uint64_t data;
  uint64_t mask;
};

}
