#pragma once

#include <nall/hash/sha512.hpp>
#if defined(EC_REFERENCE)
  #include <nall/elliptic-curve/modulo25519-reference.hpp>
#else
  #include <nall/elliptic-curve/modulo25519-optimized.hpp>
#endif

namespace nall::EllipticCurve {

static const u256 L = (1_u256 << 252) + 27742317777372353535851937790883648493_u256;

struct Ed25519 {
  auto publicKey(u256 privateKey) const -> u256 {
    return compress(scalarMultiply(B, clamp(hash(privateKey)) % L));
  }

  auto sign(array_view<u8> message, u256 privateKey) const -> u512 {
    u512 H = hash(privateKey);
    u256 a = clamp(H) % L;
    u256 A = compress(scalarMultiply(B, a));

    u512 r = hash(upper(H), message) % L;
    u256 R = compress(scalarMultiply(B, r));

    u512 k = hash(R, A, message) % L;
    u256 S = (k * a + r) % L;

    return u512(S) << 256 | R;
  }

  auto verify(array_view<u8> message, u512 signature, u256 publicKey) const -> bool {
    auto R = decompress(lower(signature));
    auto A = decompress(publicKey);
    if(!R || !A) return false;

    u256 S = upper(signature) % L;
    u512 r = hash(lower(signature), publicKey, message) % L;

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

  auto input(Hash::SHA512&) const -> void {}

  template<typename... P> auto input(Hash::SHA512& hash, u256 value, P&&... p) const -> void {
    for(u32 byte : range(32)) hash.input(u8(value >> byte * 8));
    input(hash, forward<P>(p)...);
  }

  template<typename... P> auto input(Hash::SHA512& hash, array_view<u8> value, P&&... p) const -> void {
    hash.input(value);
    input(hash, forward<P>(p)...);
  }

  template<typename... P> auto hash(P&&... p) const -> u512 {
    Hash::SHA512 hash;
    input(hash, forward<P>(p)...);
    u512 result;
    for(auto byte : reverse(hash.output())) result = result << 8 | byte;
    return result;
  }

  auto clamp(u256 p) const -> u256 {
    p &= (1_u256 << 254) - 8;
    p |= (1_u256 << 254);
    return p;
  }

  auto onCurve(point p) const -> bool {
    if(!p.z) return false;
    if(p.x * p.y - p.z * p.t) return false;
    if(square(p.y) - square(p.x) - square(p.z) - square(p.t) * D) return false;
    return true;
  }

  auto decompress(u256 c) const -> maybe<point> {
    field y = c & ~0_u256 >> 1;
    field x = squareRoot((square(y) - 1) * reciprocal(D * square(y) + 1));
    if(c >> 255) x = -x;
    point p{x, y, 1, x * y};
    if(!onCurve(p)) return nothing;
    return p;
  }

  auto compress(point p) const -> u256 {
    field r = reciprocal(p.z);
    field x = p.x * r;
    field y = p.y * r;
    return (x & 1) << 255 | (y & ~0_u256 >> 1);
  }

  auto edwardsDouble(point p) const -> point {
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

  auto edwardsAdd(point p, point q) const -> point {
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

  auto scalarMultiply(point q, u256 exponent) const -> point {
    point p{0, 1, 1, 0}, c;
    for(u32 bit : reverse(range(253))) {
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
