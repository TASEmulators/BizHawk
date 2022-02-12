#pragma once

namespace nall {

template<u32 Precision> struct NaturalPrimitive {
  static_assert(Precision >= 1 && Precision <= 64);
  using utype =
    conditional_t<Precision <=  8, u8,
    conditional_t<Precision <= 16, u16,
    conditional_t<Precision <= 32, u32,
    conditional_t<Precision <= 64, u64,
    void>>>>;

  NaturalPrimitive() = default;
  template<u32 Bits> NaturalPrimitive(NaturalPrimitive<Bits> value) { data = cast(value); }
  template<typename T> NaturalPrimitive(const T& value) { data = cast(value); }
  explicit NaturalPrimitive(const char* value) { data = cast(toNatural(value)); }

  operator utype() const { return data; }

  auto operator++(s32) { auto value = *this; data = cast(data + 1); return value; }
  auto operator--(s32) { auto value = *this; data = cast(data - 1); return value; }

  auto& operator++() { data = cast(data + 1); return *this; }
  auto& operator--() { data = cast(data - 1); return *this; }

  template<typename T> auto& operator  =(const T& value) { data = cast(        value); return *this; }
  template<typename T> auto& operator *=(const T& value) { data = cast(data  * value); return *this; }
  template<typename T> auto& operator /=(const T& value) { data = cast(data  / value); return *this; }
  template<typename T> auto& operator %=(const T& value) { data = cast(data  % value); return *this; }
  template<typename T> auto& operator +=(const T& value) { data = cast(data  + value); return *this; }
  template<typename T> auto& operator -=(const T& value) { data = cast(data  - value); return *this; }
  template<typename T> auto& operator<<=(const T& value) { data = cast(data << value); return *this; }
  template<typename T> auto& operator>>=(const T& value) { data = cast(data >> value); return *this; }
  template<typename T> auto& operator &=(const T& value) { data = cast(data  & value); return *this; }
  template<typename T> auto& operator ^=(const T& value) { data = cast(data  ^ value); return *this; }
  template<typename T> auto& operator |=(const T& value) { data = cast(data  | value); return *this; }

  auto serialize(serializer& s) { s(data); }

private:
  static constexpr auto mask() -> utype {
    return ~0ull >> 64 - Precision;
  }

  auto cast(utype value) const -> utype {
    return value & mask();
  }

  utype data;
};

template<u32 Precision> struct Natural {
  static_assert(Precision >= 1 && Precision <= 64);
  static constexpr auto bits() -> u32 { return Precision; }
  using utype =
    conditional_t<Precision <=  8, u8,
    conditional_t<Precision <= 16, u16,
    conditional_t<Precision <= 32, u32,
    conditional_t<Precision <= 64, u64,
    void>>>>;
  static constexpr auto mask() -> utype { return ~0ull >> 64 - Precision; }

  Natural() : data(0) {}
  template<u32 Bits> Natural(Natural<Bits> value) { data = cast(value); }
  template<typename T> Natural(const T& value) { data = cast(value); }
  explicit Natural(const char* value) { data = cast(toNatural(value)); }

  operator utype() const { return data; }

  auto operator++(s32) { auto value = *this; data = cast(data + 1); return value; }
  auto operator--(s32) { auto value = *this; data = cast(data - 1); return value; }

  auto& operator++() { data = cast(data + 1); return *this; }
  auto& operator--() { data = cast(data - 1); return *this; }

  template<typename T> auto& operator  =(const T& value) { data = cast(        value); return *this; }
  template<typename T> auto& operator *=(const T& value) { data = cast(data  * value); return *this; }
  template<typename T> auto& operator /=(const T& value) { data = cast(data  / value); return *this; }
  template<typename T> auto& operator %=(const T& value) { data = cast(data  % value); return *this; }
  template<typename T> auto& operator +=(const T& value) { data = cast(data  + value); return *this; }
  template<typename T> auto& operator -=(const T& value) { data = cast(data  - value); return *this; }
  template<typename T> auto& operator<<=(const T& value) { data = cast(data << value); return *this; }
  template<typename T> auto& operator>>=(const T& value) { data = cast(data >> value); return *this; }
  template<typename T> auto& operator &=(const T& value) { data = cast(data  & value); return *this; }
  template<typename T> auto& operator ^=(const T& value) { data = cast(data  ^ value); return *this; }
  template<typename T> auto& operator |=(const T& value) { data = cast(data  | value); return *this; }

  auto bit(s32 index) -> DynamicBitRange<Natural> { return {*this, index}; }
  auto bit(s32 index) const -> const DynamicBitRange<Natural> { return {(Natural&)*this, index}; }

  auto bit(s32 lo, s32 hi) -> DynamicBitRange<Natural> { return {*this, lo, hi}; }
  auto bit(s32 lo, s32 hi) const -> const DynamicBitRange<Natural> { return {(Natural&)*this, lo, hi}; }

  auto byte(s32 index) -> DynamicBitRange<Natural> { return {*this, index * 8 + 0, index * 8 + 7}; }
  auto byte(s32 index) const -> const DynamicBitRange<Natural> { return {(Natural&)*this, index * 8 + 0, index * 8 + 7}; }

  auto mask(s32 index) const -> utype {
    return data & 1 << index;
  }

  auto mask(s32 lo, s32 hi) const -> utype {
    return data & (~0ull >> 64 - (hi - lo + 1) << lo);
  }

  auto slice(s32 index) const { return Natural<>{bit(index)}; }
  auto slice(s32 lo, s32 hi) const { return Natural<>{bit(lo, hi)}; }

  static auto clamp(u64 value) -> utype {
    constexpr u64 b = 1ull << bits() - 1;
    constexpr u64 m = b * 2 - 1;
    return value < m ? value : m;
  }

  auto clip(u32 bits) -> utype {
    const u64 b = 1ull << bits - 1;
    const u64 m = b * 2 - 1;
    return data & m;
  }

  auto serialize(serializer& s) { s(data); }
  auto integer() const -> Integer<Precision>;

private:
  auto cast(utype value) const -> utype {
    return value & mask();
  }

  utype data;
};

}
