#pragma once

#include <nall/arithmetic/barrett.hpp>

namespace nall::EllipticCurve {

static const uint256_t P = (1_u256 << 255) - 19;

#define Mask ((1ull << 51) - 1)

struct Modulo25519 {
  inline Modulo25519() = default;
  inline Modulo25519(const Modulo25519&) = default;
  inline Modulo25519(uint64_t a, uint64_t b = 0, uint64_t c = 0, uint64_t d = 0, uint64_t e = 0) : l{a, b, c, d, e} {}
  inline Modulo25519(uint256_t n);

  inline explicit operator bool() const { return (bool)operator()(); }
  inline auto operator[](uint index) -> uint64_t& { return l[index]; }
  inline auto operator[](uint index) const -> uint64_t { return l[index]; }
  inline auto operator()() const -> uint256_t;

private:
  uint64_t l[5];  //51-bits per limb; 255-bits total
};

inline Modulo25519::Modulo25519(uint256_t n) {
  l[0] = n >>   0 & Mask;
  l[1] = n >>  51 & Mask;
  l[2] = n >> 102 & Mask;
  l[3] = n >> 153 & Mask;
  l[4] = n >> 204 & Mask;
}

inline auto Modulo25519::operator()() const -> uint256_t {
  Modulo25519 o = *this;

  o[1] +=      (o[0] >> 51); o[0] &= Mask;
  o[2] +=      (o[1] >> 51); o[1] &= Mask;
  o[3] +=      (o[2] >> 51); o[2] &= Mask;
  o[4] +=      (o[3] >> 51); o[3] &= Mask;
  o[0] += 19 * (o[4] >> 51); o[4] &= Mask;

  o[1] +=      (o[0] >> 51); o[0] &= Mask;
  o[2] +=      (o[1] >> 51); o[1] &= Mask;
  o[3] +=      (o[2] >> 51); o[2] &= Mask;
  o[4] +=      (o[3] >> 51); o[3] &= Mask;
  o[0] += 19 * (o[4] >> 51); o[4] &= Mask;

  o[0] += 19;
  o[1] +=      (o[0] >> 51); o[0] &= Mask;
  o[2] +=      (o[1] >> 51); o[1] &= Mask;
  o[3] +=      (o[2] >> 51); o[2] &= Mask;
  o[4] +=      (o[3] >> 51); o[3] &= Mask;
  o[0] += 19 * (o[4] >> 51); o[4] &= Mask;

  o[0] += Mask - 18;
  o[1] += Mask;
  o[2] += Mask;
  o[3] += Mask;
  o[4] += Mask;

  o[1] += o[0] >> 51; o[0] &= Mask;
  o[2] += o[1] >> 51; o[1] &= Mask;
  o[3] += o[2] >> 51; o[2] &= Mask;
  o[4] += o[3] >> 51; o[3] &= Mask;
  o[4] &= Mask;

  return (uint256_t)o[0] << 0 | (uint256_t)o[1] << 51 | (uint256_t)o[2] << 102 | (uint256_t)o[3] << 153 | (uint256_t)o[4] << 204;
}

inline auto cmove(bool move, Modulo25519& l, const Modulo25519& r) -> void {
  uint64_t mask = -move;
  l[0] ^= mask & (l[0] ^ r[0]);
  l[1] ^= mask & (l[1] ^ r[1]);
  l[2] ^= mask & (l[2] ^ r[2]);
  l[3] ^= mask & (l[3] ^ r[3]);
  l[4] ^= mask & (l[4] ^ r[4]);
}

inline auto cswap(bool swap, Modulo25519& l, Modulo25519& r) -> void {
  uint64_t mask = -swap, x;
  x = mask & (l[0] ^ r[0]); l[0] ^= x; r[0] ^= x;
  x = mask & (l[1] ^ r[1]); l[1] ^= x; r[1] ^= x;
  x = mask & (l[2] ^ r[2]); l[2] ^= x; r[2] ^= x;
  x = mask & (l[3] ^ r[3]); l[3] ^= x; r[3] ^= x;
  x = mask & (l[4] ^ r[4]); l[4] ^= x; r[4] ^= x;
}

inline auto operator-(const Modulo25519& l) -> Modulo25519 {  //P - l
  Modulo25519 o;
  uint64_t c;
  o[0]  = 0xfffffffffffda - l[0];     c = o[0] >> 51; o[0] &= Mask;
  o[1]  = 0xffffffffffffe - l[1] + c; c = o[1] >> 51; o[1] &= Mask;
  o[2]  = 0xffffffffffffe - l[2] + c; c = o[2] >> 51; o[2] &= Mask;
  o[3]  = 0xffffffffffffe - l[3] + c; c = o[3] >> 51; o[3] &= Mask;
  o[4]  = 0xffffffffffffe - l[4] + c; c = o[4] >> 51; o[4] &= Mask;
  o[0] += c * 19;
  return o;
}

inline auto operator+(const Modulo25519& l, const Modulo25519& r) -> Modulo25519 {
  Modulo25519 o;
  uint64_t c;
  o[0]  = l[0] + r[0];     c = o[0] >> 51; o[0] &= Mask;
  o[1]  = l[1] + r[1] + c; c = o[1] >> 51; o[1] &= Mask;
  o[2]  = l[2] + r[2] + c; c = o[2] >> 51; o[2] &= Mask;
  o[3]  = l[3] + r[3] + c; c = o[3] >> 51; o[3] &= Mask;
  o[4]  = l[4] + r[4] + c; c = o[4] >> 51; o[4] &= Mask;
  o[0] += c * 19;
  return o;
}

inline auto operator-(const Modulo25519& l, const Modulo25519& r) -> Modulo25519 {
  Modulo25519 o;
  uint64_t c;
  o[0]  = l[0] + 0x1fffffffffffb4 - r[0];     c = o[0] >> 51; o[0] &= Mask;
  o[1]  = l[1] + 0x1ffffffffffffc - r[1] + c; c = o[1] >> 51; o[1] &= Mask;
  o[2]  = l[2] + 0x1ffffffffffffc - r[2] + c; c = o[2] >> 51; o[2] &= Mask;
  o[3]  = l[3] + 0x1ffffffffffffc - r[3] + c; c = o[3] >> 51; o[3] &= Mask;
  o[4]  = l[4] + 0x1ffffffffffffc - r[4] + c; c = o[4] >> 51; o[4] &= Mask;
  o[0] += c * 19;
  return o;
}

inline auto operator*(const Modulo25519& l, uint64_t scalar) -> Modulo25519 {
  Modulo25519 o;
  uint128_t a;
  a = (uint128_t)l[0] * scalar;                    o[0] = a & Mask;
  a = (uint128_t)l[1] * scalar + (a >> 51 & Mask); o[1] = a & Mask;
  a = (uint128_t)l[2] * scalar + (a >> 51 & Mask); o[2] = a & Mask;
  a = (uint128_t)l[3] * scalar + (a >> 51 & Mask); o[3] = a & Mask;
  a = (uint128_t)l[4] * scalar + (a >> 51 & Mask); o[4] = a & Mask;
  o[0] += (a >> 51) * 19;
  return o;
}

inline auto operator*(const Modulo25519& l, Modulo25519 r) -> Modulo25519 {
  uint128_t t[] = {
    (uint128_t)r[0] * l[0],
    (uint128_t)r[0] * l[1] + (uint128_t)r[1] * l[0],
    (uint128_t)r[0] * l[2] + (uint128_t)r[1] * l[1] + (uint128_t)r[2] * l[0],
    (uint128_t)r[0] * l[3] + (uint128_t)r[1] * l[2] + (uint128_t)r[2] * l[1] + (uint128_t)r[3] * l[0],
    (uint128_t)r[0] * l[4] + (uint128_t)r[1] * l[3] + (uint128_t)r[2] * l[2] + (uint128_t)r[3] * l[1] + (uint128_t)r[4] * l[0]
  };

  r[1] *= 19, r[2] *= 19, r[3] *= 19, r[4] *= 19;

  t[0] += (uint128_t)r[4] * l[1] + (uint128_t)r[3] * l[2] + (uint128_t)r[2] * l[3] + (uint128_t)r[1] * l[4];
  t[1] += (uint128_t)r[4] * l[2] + (uint128_t)r[3] * l[3] + (uint128_t)r[2] * l[4];
  t[2] += (uint128_t)r[4] * l[3] + (uint128_t)r[3] * l[4];
  t[3] += (uint128_t)r[4] * l[4];

  uint64_t c; r[0] = t[0] & Mask; c = (uint64_t)(t[0] >> 51);
  t[1] += c;  r[1] = t[1] & Mask; c = (uint64_t)(t[1] >> 51);
  t[2] += c;  r[2] = t[2] & Mask; c = (uint64_t)(t[2] >> 51);
  t[3] += c;  r[3] = t[3] & Mask; c = (uint64_t)(t[3] >> 51);
  t[4] += c;  r[4] = t[4] & Mask; c = (uint64_t)(t[4] >> 51);

  r[0] += c * 19; c = r[0] >> 51; r[0] &= Mask;
  r[1] += c;      c = r[1] >> 51; r[1] &= Mask;
  r[2] += c;
  return r;
}

inline auto operator&(const Modulo25519& lhs, uint256_t rhs) -> uint256_t {
  return lhs() & rhs;
}

inline auto square(const Modulo25519& lhs) -> Modulo25519 {
  Modulo25519 r{lhs};
  Modulo25519 d{r[0] * 2, r[1] * 2, r[2] * 2 * 19, r[4] * 19, r[4] * 19 * 2};

  uint128_t t[5];
  t[0] = (uint128_t)r[0] * r[0] + (uint128_t)d[4] * r[1] + (uint128_t)d[2] * r[3];
  t[1] = (uint128_t)d[0] * r[1] + (uint128_t)d[4] * r[2] + (uint128_t)r[3] * r[3] * 19;
  t[2] = (uint128_t)d[0] * r[2] + (uint128_t)r[1] * r[1] + (uint128_t)d[4] * r[3];
  t[3] = (uint128_t)d[0] * r[3] + (uint128_t)d[1] * r[2] + (uint128_t)r[4] * d[3];
  t[4] = (uint128_t)d[0] * r[4] + (uint128_t)d[1] * r[3] + (uint128_t)r[2] * r[2];

  uint64_t c; r[0] = t[0] & Mask; c = (uint64_t)(t[0] >> 51);
  t[1] += c;  r[1] = t[1] & Mask; c = (uint64_t)(t[1] >> 51);
  t[2] += c;  r[2] = t[2] & Mask; c = (uint64_t)(t[2] >> 51);
  t[3] += c;  r[3] = t[3] & Mask; c = (uint64_t)(t[3] >> 51);
  t[4] += c;  r[4] = t[4] & Mask; c = (uint64_t)(t[4] >> 51);

  r[0] += c * 19; c = r[0] >> 51; r[0] &= Mask;
  r[1] += c;      c = r[1] >> 51; r[1] &= Mask;
  r[2] += c;
  return r;
}

inline auto exponentiate(const Modulo25519& lhs, uint256_t exponent) -> Modulo25519 {
  Modulo25519 x = 1, y;
  for(uint bit : reverse(range(256))) {
    x = square(x);
    y = x * lhs;
    cmove(exponent >> bit & 1, x, y);
  }
  return x;
}

inline auto reciprocal(const Modulo25519& lhs) -> Modulo25519 {
  return exponentiate(lhs, P - 2);
}

inline auto squareRoot(const Modulo25519& lhs) -> Modulo25519 {
  static const Modulo25519 I = exponentiate(Modulo25519(2), P - 1 >> 2);  //I == sqrt(-1)
  Modulo25519 x = exponentiate(lhs, P + 3 >> 3);
  Modulo25519 y = x * I;
  cmove(bool(square(x) - lhs), x, y);
  y = -x;
  cmove(x & 1, x, y);
  return x;
}

#undef Mask

}
