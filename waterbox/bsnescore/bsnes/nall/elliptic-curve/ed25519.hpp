#pragma once

#include <nall/hash/sha512.hpp>
#if defined(EC_REFERENCE)
  #include <nall/elliptic-curve/modulo25519-reference.hpp>
#else
  #include <nall/elliptic-curve/modulo25519-optimized.hpp>
#endif

namespace nall::EllipticCurve {

static const uint256_t L = (1_u256 << 252) + 27742317777372353535851937790883648493_u256;

struct Ed25519 {
  auto publicKey(uint256_t privateKey) const -> uint256_t {
    return compress(scalarMultiply(B, clamp(hash(privateKey)) % L));
  }

  auto sign(array_view<uint8_t> message, uint256_t privateKey) const -> uint512_t {
    uint512_t H = hash(privateKey);
    uint256_t a = clamp(H) % L;
    uint256_t A = compress(scalarMultiply(B, a));

    uint512_t r = hash(upper(H), message) % L;
    uint256_t R = compress(scalarMultiply(B, r));

    uint512_t k = hash(R, A, message) % L;
    uint256_t S = (k * a + r) % L;

    return uint512_t(S) << 256 | R;
  }

  auto verify(array_view<uint8_t> message, uint512_t signature, uint256_t publicKey) const -> bool {
    auto R = decompress(lower(signature));
    auto A = decompress(publicKey);
    if(!R || !A) return false;

    uint256_t S = upper(signature) % L;
    uint512_t r = hash(lower(signature), publicKey, message) % L;

    auto p = scalarMultiply(B, S);
    auto q = edwardsAdd(R(), scalarMultiply(A(), r));
    if(!onCurve(p) || !onCurve(q)) return false;
    if(p.x * q.z - q.x * p.z) return false;
    if(p.y * q.z - q.y * p.z) return false;
    return true;
  }

private:
  using field = Modulo25519;
  struct point { field x, y, z, t; };
  const field D = -field(121665) * reciprocal(field(121666));
  const point B = *decompress((field(4) * reciprocal(field(5)))());
  const BarrettReduction<256> L = BarrettReduction<256>{EllipticCurve::L};

  inline auto input(Hash::SHA512&) const -> void {}

  template<typename... P> inline auto input(Hash::SHA512& hash, uint256_t value, P&&... p) const -> void {
    for(uint byte : range(32)) hash.input(uint8_t(value >> byte * 8));
    input(hash, forward<P>(p)...);
  }

  template<typename... P> inline auto input(Hash::SHA512& hash, array_view<uint8_t> value, P&&... p) const -> void {
    hash.input(value);
    input(hash, forward<P>(p)...);
  }

  template<typename... P> inline auto hash(P&&... p) const -> uint512_t {
    Hash::SHA512 hash;
    input(hash, forward<P>(p)...);
    uint512_t result;
    for(auto byte : reverse(hash.output())) result = result << 8 | byte;
    return result;
  }

  inline auto clamp(uint256_t p) const -> uint256_t {
    p &= (1_u256 << 254) - 8;
    p |= (1_u256 << 254);
    return p;
  }

  inline auto onCurve(point p) const -> bool {
    if(!p.z) return false;
    if(p.x * p.y - p.z * p.t) return false;
    if(square(p.y) - square(p.x) - square(p.z) - square(p.t) * D) return false;
    return true;
  }

  inline auto decompress(uint256_t c) const -> maybe<point> {
    field y = c & ~0_u256 >> 1;
    field x = squareRoot((square(y) - 1) * reciprocal(D * square(y) + 1));
    if(c >> 255) x = -x;
    point p{x, y, 1, x * y};
    if(!onCurve(p)) return nothing;
    return p;
  }

  inline auto compress(point p) const -> uint256_t {
    field r = reciprocal(p.z);
    field x = p.x * r;
    field y = p.y * r;
    return (x & 1) << 255 | (y & ~0_u256 >> 1);
  }

  inline auto edwardsDouble(point p) const -> point {
    field a = square(p.x);
    field b = square(p.y);
    field c = square(p.z);
    field d = -a;
    field e = square(p.x + p.y) - a - b;
    field g = d + b;
    field f = g - (c + c);
    field h = d - b;
    return {e * f, g * h, f * g, e * h};
  }

  inline auto edwardsAdd(point p, point q) const -> point {
    field a = (p.y - p.x) * (q.y - q.x);
    field b = (p.y + p.x) * (q.y + q.x);
    field c = (p.t + p.t) * q.t * D;
    field d = (p.z + p.z) * q.z;
    field e = b - a;
    field f = d - c;
    field g = d + c;
    field h = b + a;
    return {e * f, g * h, f * g, e * h};
  }

  inline auto scalarMultiply(point q, uint256_t exponent) const -> point {
    point p{0, 1, 1, 0}, c;
    for(uint bit : reverse(range(253))) {
      p = edwardsDouble(p);
      c = edwardsAdd(p, q);
      bool condition = exponent >> bit & 1;
      cmove(condition, p.x, c.x);
      cmove(condition, p.y, c.y);
      cmove(condition, p.z, c.z);
      cmove(condition, p.t, c.t);
    }
    return p;
  }
};

}
