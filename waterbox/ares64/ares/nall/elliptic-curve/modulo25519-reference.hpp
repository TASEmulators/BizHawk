#pragma once

//warning: this implementation leaks side-channel information
//use modulo25519-optimized.hpp in production

#include <nall/arithmetic/barrett.hpp>

namespace nall::EllipticCurve {

static const u256 P = (1_u256 << 255) - 19;

struct Modulo25519 {
  Modulo25519() = default;
  Modulo25519(const Modulo25519& source) : value(source.value) {}
  template<typename T> Modulo25519(const T& value) : value(value) {}
  explicit operator bool() const { return (bool)value; }
  auto operator()() const -> u256 { return value; }

private:
  u256 value;
};

inline auto operator-(const Modulo25519& lhs) -> Modulo25519 {
  return P - lhs();
}

inline auto operator+(const Modulo25519& lhs, const Modulo25519& rhs) -> Modulo25519 {
  u512 value = (u512)lhs() + rhs();
  if(value >= P) value -= P;
  return value;
}

inline auto operator-(const Modulo25519& lhs, const Modulo25519& rhs) -> Modulo25519 {
  u512 value = (u512)lhs();
  if(value < rhs()) value += P;
  return u256(value - rhs());
}

inline auto operator*(const Modulo25519& lhs, const Modulo25519& rhs) -> Modulo25519 {
  static const BarrettReduction<256> P{EllipticCurve::P};
  u256 hi, lo;
  mul(lhs(), rhs(), hi, lo);
  return u512{hi, lo} % P;
}

inline auto operator&(const Modulo25519& lhs, u256 rhs) -> u256 {
  return lhs() & rhs;
}

inline auto square(const Modulo25519& lhs) -> Modulo25519 {
  static const BarrettReduction<256> P{EllipticCurve::P};
  u256 hi, lo;
  square(lhs(), hi, lo);
  return u512{hi, lo} % P;
}

inline auto exponentiate(const Modulo25519& lhs, u256 exponent) -> Modulo25519 {
  if(exponent == 0) return 1;
  Modulo25519 value = square(exponentiate(lhs, exponent >> 1));
  if(exponent & 1) value = value * lhs;
  return value;
}

inline auto reciprocal(const Modulo25519& lhs) -> Modulo25519 {
  return exponentiate(lhs, P - 2);
}

inline auto squareRoot(const Modulo25519& lhs) -> Modulo25519 {
  static const Modulo25519 I = exponentiate(Modulo25519(2), P - 1 >> 2);  //I = sqrt(-1)
  Modulo25519 value = exponentiate(lhs, P + 3 >> 3);
  if(square(value) - lhs) value = value * I;
  if(value & 1) value = -value;
  return value;
}

inline auto cmove(bool condition, Modulo25519& lhs, const Modulo25519& rhs) -> void {
  if(condition) lhs = rhs;
}

inline auto cswap(bool condition, Modulo25519& lhs, Modulo25519& rhs) -> void {
  if(condition) swap(lhs, rhs);
}

}
