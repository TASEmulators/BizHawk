#pragma once

namespace nall {

struct Interpolation {
  static inline auto Nearest(f64 mu, f64 a, f64 b, f64 c, f64 d) -> f64 {
    return (mu <= 0.5 ? b : c);
  }

  static inline auto Sublinear(f64 mu, f64 a, f64 b, f64 c, f64 d) -> f64 {
    mu = ((mu - 0.5) * 2.0) + 0.5;
    if(mu < 0) mu = 0;
    if(mu > 1) mu = 1;
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Linear(f64 mu, f64 a, f64 b, f64 c, f64 d) -> f64 {
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Cosine(f64 mu, f64 a, f64 b, f64 c, f64 d) -> f64 {
    mu = (1.0 - cos(mu * Math::Pi)) / 2.0;
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Cubic(f64 mu, f64 a, f64 b, f64 c, f64 d) -> f64 {
    f64 A = d - c - a + b;
    f64 B = a - b - A;
    f64 C = c - a;
    f64 D = b;
    return A * (mu * mu * mu) + B * (mu * mu) + C * mu + D;
  }

  static inline auto Hermite(f64 mu1, f64 a, f64 b, f64 c, f64 d) -> f64 {
    const f64 tension = 0.0;  //-1 = low, 0 = normal, +1 = high
    const f64 bias = 0.0;  //-1 = left, 0 = even, +1 = right
    f64 mu2, mu3, m0, m1, a0, a1, a2, a3;

    mu2 = mu1 * mu1;
    mu3 = mu2 * mu1;

    m0  = (b - a) * (1.0 + bias) * (1.0 - tension) / 2.0;
    m0 += (c - b) * (1.0 - bias) * (1.0 - tension) / 2.0;
    m1  = (c - b) * (1.0 + bias) * (1.0 - tension) / 2.0;
    m1 += (d - c) * (1.0 - bias) * (1.0 - tension) / 2.0;

    a0 = +2 * mu3 - 3 * mu2 + 1;
    a1 =      mu3 - 2 * mu2 + mu1;
    a2 =      mu3 -     mu2;
    a3 = -2 * mu3 + 3 * mu2;

    return (a0 * b) + (a1 * m0) + (a2 * m1) + (a3 * c);
  }
};

}
