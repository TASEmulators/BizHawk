#pragma once

#if defined(EC_REFERENCE)
  #include <nall/elliptic-curve/modulo25519-reference.hpp>
#else
  #include <nall/elliptic-curve/modulo25519-optimized.hpp>
#endif

namespace nall::EllipticCurve {

struct Curve25519 {
  auto sharedKey(u256 secretKey, u256 basepoint = 9) const -> u256 {
    secretKey &= (1_u256 << 254) - 8;
    secretKey |= (1_u256 << 254);
    basepoint &= ~0_u256 >> 1;

    point p = scalarMultiply(basepoint % P, secretKey);
    field k = p.x * reciprocal(p.z);
    return k();
  }

private:
  using field = Modulo25519;
  struct point { field x, z; };
  const BarrettReduction<256> P = BarrettReduction<256>{EllipticCurve::P};

  auto montgomeryDouble(point p) const -> point {
    field a = square(p.x + p.z);
    field b = square(p.x - p.z);
    field c = a - b;
    field d = a + c * 121665;
    return {a * b, c * d};
  }

  auto montgomeryAdd(point p, point q, field b) const -> point {
    return {
      square(p.x * q.x - p.z * q.z),
      square(p.x * q.z - p.z * q.x) * b
    };
  }

  auto scalarMultiply(field b, u256 exponent) const -> point {
    point p{1, 0}, q{b, 1};
    for(u32 bit : reverse(range(255))) {
      bool condition = exponent >> bit & 1;
      cswap(condition, p.x, q.x);
      cswap(condition, p.z, q.z);
      q = montgomeryAdd(p, q, b);
      p = montgomeryDouble(p);
      cswap(condition, p.x, q.x);
      cswap(condition, p.z, q.z);
    }
    return p;
  }
};

}
