#pragma once

namespace nall {

template<u32 Precision> struct Real {
  static_assert(Precision == 32 || Precision == 64);
  static constexpr auto bits() -> u32 { return Precision; }
  using ftype =
    conditional_t<bits() == 32, f32,
    conditional_t<bits() == 64, f64,
    void>>;

  Real() : data(0.0) {}
  template<s32 Bits> Real(Real<Bits> value) : data((ftype)value) {}
  template<typename T> Real(const T& value) : data((ftype)value) {}
  explicit Real(const char* value) : data((ftype)toReal(value)) {}

  operator ftype() const { return data; }

  auto operator++(s32) { auto value = *this; ++data; return value; }
  auto operator--(s32) { auto value = *this; --data; return value; }

  auto& operator++() { data++; return *this; }
  auto& operator--() { data--; return *this; }

  template<typename T> auto& operator =(const T& value) { data =        value; return *this; }
  template<typename T> auto& operator*=(const T& value) { data = data * value; return *this; }
  template<typename T> auto& operator/=(const T& value) { data = data / value; return *this; }
  template<typename T> auto& operator%=(const T& value) { data = data % value; return *this; }
  template<typename T> auto& operator+=(const T& value) { data = data + value; return *this; }
  template<typename T> auto& operator-=(const T& value) { data = data - value; return *this; }

  auto serialize(serializer& s) { s(data); }

private:
  ftype data;
};

}
