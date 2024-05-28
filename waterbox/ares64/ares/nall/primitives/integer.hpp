#pragma once

namespace nall {

template<u32 Precision> struct IntegerPrimitive {
  static_assert(Precision >= 1 && Precision <= 64);
  using stype =
    conditional_t<Precision <=  8, s8,
    conditional_t<Precision <= 16, s16,
    conditional_t<Precision <= 32, s32,
    conditional_t<Precision <= 64, s64,
    void>>>>;
  using utype = typename Natural<Precision>::utype;

  IntegerPrimitive() = default;
  template<u32 Bits> IntegerPrimitive(IntegerPrimitive<Bits> value) { data = cast(value); }
  template<typename T> IntegerPrimitive(const T& value) { data = cast(value); }
  explicit IntegerPrimitive(const char* value) { data = cast(toInteger(value)); }

  operator stype() const { return data; }

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

private:
  static constexpr auto mask() -> utype {
    return ~0ull >> 64 - Precision;
  }

  static constexpr auto sign() -> utype {
    return 1ull << Precision - 1;
  }

  auto cast(stype value) const -> stype {
    return (value & mask() ^ sign()) - sign();
  }

  stype data;
};

template<u32 Precision> struct Integer {
  static_assert(Precision >= 1 && Precision <= 64);
  static constexpr auto bits() -> u32 { return Precision; }
  using stype =
    conditional_t<Precision <=  8, s8,
    conditional_t<Precision <= 16, s16,
    conditional_t<Precision <= 32, s32,
    conditional_t<Precision <= 64, s64,
    void>>>>;
  using utype = typename Natural<Precision>::utype;
  static constexpr auto mask() -> utype { return ~0ull >> 64 - Precision; }
  static constexpr auto sign() -> utype { return 1ull << Precision - 1; }

  Integer() : data(0) {}
  template<u32 Bits> Integer(Integer<Bits> value) { data = cast(value); }
  template<typename T> Integer(const T& value) { data = cast(value); }
  explicit Integer(const char* value) { data = cast(toInteger(value)); }

  operator stype() const { return data; }

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

  auto bit(s32 index) -> DynamicBitRange<Integer> { return {*this, index}; }
  auto bit(s32 index) const -> const DynamicBitRange<Integer> { return {(Integer&)*this, index}; }

  auto bit(s32 lo, s32 hi) -> DynamicBitRange<Integer> { return {*this, lo, hi}; }
  auto bit(s32 lo, s32 hi) const -> const DynamicBitRange<Integer> { return {(Integer&)*this, lo, hi}; }

  auto byte(s32 index) -> DynamicBitRange<Integer> { return {*this, index * 8 + 0, index * 8 + 7}; }
  auto byte(s32 index) const -> const DynamicBitRange<Integer> { return {(Integer&)*this, index * 8 + 0, index * 8 + 7}; }

  auto mask(s32 index) const -> utype {
    return data & 1 << index;
  }

  auto mask(s32 lo, s32 hi) const -> utype {
    return data & (~0ull >> 64 - (hi - lo + 1) << lo);
  }

  auto slice(s32 index) const { return Natural<>{bit(index)}; }
  auto slice(s32 lo, s32 hi) const { return Natural<>{bit(lo, hi)}; }

  static auto clamp(s64 value) -> stype {
    constexpr s64 b = 1ull << bits() - 1;
    constexpr s64 m = b - 1;
    return value > m ? m : value < -b ? -b : value;
  }

  auto clip(u32 bits) -> stype {
    const u64 b = 1ull << bits - 1;
    const u64 m = b * 2 - 1;
    return (data & m ^ b) - b;
  }

  auto serialize(serializer& s) { s(data); }
  auto natural() const -> Natural<Precision>;

private:
  auto cast(stype value) const -> stype {
    return (value & mask() ^ sign()) - sign();
  }

  stype data;
};

}
