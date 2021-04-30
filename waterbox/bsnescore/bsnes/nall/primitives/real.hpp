#pragma once

namespace nall {

template<uint Precision> struct Real {
  static_assert(Precision == 32 || Precision == 64);
  static inline constexpr auto bits() -> uint { return Precision; }
  using ftype =
    conditional_t<bits() == 32, float32_t,
    conditional_t<bits() == 64, float64_t,
    void>>;

  inline Real() : data(0.0) {}
  template<int Bits> inline Real(Real<Bits> value) : data((ftype)value) {}
  template<typename T> inline Real(const T& value) : data((ftype)value) {}
  explicit inline Real(const char* value) : data((ftype)toReal(value)) {}

  inline operator ftype() const { return data; }

  inline auto operator++(int) { auto value = *this; ++data; return value; }
  inline auto operator--(int) { auto value = *this; --data; return value; }

  inline auto& operator++() { data++; return *this; }
  inline auto& operator--() { data--; return *this; }

  template<typename T> inline auto& operator =(const T& value) { data =        value; return *this; }
  template<typename T> inline auto& operator*=(const T& value) { data = data * value; return *this; }
  template<typename T> inline auto& operator/=(const T& value) { data = data / value; return *this; }
  template<typename T> inline auto& operator%=(const T& value) { data = data % value; return *this; }
  template<typename T> inline auto& operator+=(const T& value) { data = data + value; return *this; }
  template<typename T> inline auto& operator-=(const T& value) { data = data - value; return *this; }

  inline auto serialize(serializer& s) { s(data); }

private:
  ftype data;
};

}
