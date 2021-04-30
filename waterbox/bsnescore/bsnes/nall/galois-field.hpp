#pragma once

//table-driven galois field modulo 2
//do not use with GF(2^17) or larger

namespace nall {

template<typename field, uint Elements, uint Polynomial>
struct GaloisField {
  using type = GaloisField;

  GaloisField(uint x = 0) : x(x) {}
  operator field() const { return x; }

  auto operator^(field y) const -> type { return x ^ y; }
  auto operator+(field y) const -> type { return x ^ y; }
  auto operator-(field y) const -> type { return x ^ y; }
  auto operator*(field y) const -> type { return x && y ? exp(log(x) + log(y)) : 0; }
  auto operator/(field y) const -> type { return x && y ? exp(log(x) + Elements - log(y)) : 0; }

  auto& operator =(field y) { return x = y, *this; }
  auto& operator^=(field y) { return x = operator^(y), *this; }
  auto& operator+=(field y) { return x = operator^(y), *this; }
  auto& operator-=(field y) { return x = operator^(y), *this; }
  auto& operator*=(field y) { return x = operator*(y), *this; }
  auto& operator/=(field y) { return x = operator/(y), *this; }

  auto pow(field y) const -> type { return exp(log(x) * y); }
  auto inv() const -> type { return exp(Elements - log(x)); }  // 1/x

  static auto log(uint x) -> uint {
    enum : uint { Size = bit::round(Elements), Mask = Size - 1 };
    static array<field[Size]> log = [] {
      uint shift = 0, polynomial = Polynomial;
      while(polynomial >>= 1) shift++;
      shift--;

      array<field[Size]> log;
      field x = 1;
      for(uint n : range(Elements)) {
        log[x] = n;
        x = x << 1 ^ (x >> shift ? Polynomial : 0);
      }
      log[0] = 0;  //-inf (undefined)
      return log;
    }();
    return log[x & Mask];
  }

  static auto exp(uint x) -> uint {
    static array<field[Elements]> exp = [] {
      uint shift = 0, polynomial = Polynomial;
      while(polynomial >>= 1) shift++;
      shift--;

      array<field[Elements]> exp;
      field x = 1;
      for(uint n : range(Elements)) {
        exp[n] = x;
        x = x << 1 ^ (x >> shift ? Polynomial : 0);
      }
      return exp;
    }();
    return exp[x % Elements];
  }

  field x;
};

}
