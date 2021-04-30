#pragma once

namespace nall {

template<uint Precision> struct Natural {
  static_assert(Precision >= 1 && Precision <= 64);
  static inline constexpr auto bits() -> uint { return Precision; }
  using utype =
    conditional_t<bits() <=  8,  uint8_t,
    conditional_t<bits() <= 16, uint16_t,
    conditional_t<bits() <= 32, uint32_t,
    conditional_t<bits() <= 64, uint64_t,
    void>>>>;
  static inline constexpr auto mask() -> utype { return ~0ull >> 64 - Precision; }

  inline Natural() : data(0) {}
  template<uint Bits> inline Natural(Natural<Bits> value) { data = cast(value); }
  template<typename T> inline Natural(const T& value) { data = cast(value); }
  explicit inline Natural(const char* value) { data = cast(toNatural(value)); }

  inline operator utype() const { return data; }

  inline auto operator++(int) { auto value = *this; data = cast(data + 1); return value; }
  inline auto operator--(int) { auto value = *this; data = cast(data - 1); return value; }

  inline auto& operator++() { data = cast(data + 1); return *this; }
  inline auto& operator--() { data = cast(data - 1); return *this; }

  template<typename T> inline auto& operator  =(const T& value) { data = cast(        value); return *this; }
  template<typename T> inline auto& operator *=(const T& value) { data = cast(data  * value); return *this; }
  template<typename T> inline auto& operator /=(const T& value) { data = cast(data  / value); return *this; }
  template<typename T> inline auto& operator %=(const T& value) { data = cast(data  % value); return *this; }
  template<typename T> inline auto& operator +=(const T& value) { data = cast(data  + value); return *this; }
  template<typename T> inline auto& operator -=(const T& value) { data = cast(data  - value); return *this; }
  template<typename T> inline auto& operator<<=(const T& value) { data = cast(data << value); return *this; }
  template<typename T> inline auto& operator>>=(const T& value) { data = cast(data >> value); return *this; }
  template<typename T> inline auto& operator &=(const T& value) { data = cast(data  & value); return *this; }
  template<typename T> inline auto& operator ^=(const T& value) { data = cast(data  ^ value); return *this; }
  template<typename T> inline auto& operator |=(const T& value) { data = cast(data  | value); return *this; }

  inline auto bit(int index) -> BitRange<Precision> { return {&data, index}; }
  inline auto bit(int index) const -> const BitRange<Precision> { return {&data, index}; }

  inline auto bit(int lo, int hi) -> BitRange<Precision> { return {&data, lo, hi}; }
  inline auto bit(int lo, int hi) const -> const BitRange<Precision> { return {&data, lo, hi}; }

  inline auto byte(int index) -> BitRange<Precision> { return {&data, index * 8 + 0, index * 8 + 7}; }
  inline auto byte(int index) const -> const BitRange<Precision> { return {&data, index * 8 + 0, index * 8 + 7}; }

  inline auto mask(int index) const -> utype {
    return data & 1 << index;
  }

  inline auto mask(int lo, int hi) const -> utype {
    return data & (~0ull >> 64 - (hi - lo + 1) << lo);
  }

  inline auto slice(int index) const { return Natural<>{bit(index)}; }
  inline auto slice(int lo, int hi) const { return Natural<>{bit(lo, hi)}; }

  inline auto clamp(uint bits) -> utype {
    const uint64_t b = 1ull << bits - 1;
    const uint64_t m = b * 2 - 1;
    return data < m ? data : m;
  }

  inline auto clip(uint bits) -> utype {
    const uint64_t b = 1ull << bits - 1;
    const uint64_t m = b * 2 - 1;
    return data & m;
  }

  inline auto serialize(serializer& s) { s(data); }
  inline auto integer() const -> Integer<Precision>;

private:
  inline auto cast(utype value) const -> utype {
    return value & mask();
  }

  utype data;
};

}
