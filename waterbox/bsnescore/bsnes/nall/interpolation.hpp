#pragma once

namespace nall {

struct Interpolation {
  static inline auto Nearest(double mu, double a, double b, double c, double d) -> double {
    return (mu <= 0.5 ? b : c);
  }

  static inline auto Sublinear(double mu, double a, double b, double c, double d) -> double {
    mu = ((mu - 0.5) * 2.0) + 0.5;
    if(mu < 0) mu = 0;
    if(mu > 1) mu = 1;
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Linear(double mu, double a, double b, double c, double d) -> double {
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Cosine(double mu, double a, double b, double c, double d) -> double {
    mu = (1.0 - cos(mu * Math::Pi)) / 2.0;
    return b * (1.0 - mu) + c * mu;
  }

  static inline auto Cubic(double mu, double a, double b, double c, double d) -> double {
    double A = d - c - a + b;
    double B = a - b - A;
    double C = c - a;
    double D = b;
    return A * (mu * mu * mu) + B * (mu * mu) + C * mu + D;
  }

  static inline auto Hermite(double mu1, double a, double b, double c, double d) -> double {
    const double tension = 0.0;  //-1 = low, 0 = normal, +1 = high
    const double bias = 0.0;  //-1 = left, 0 = even, +1 = right
    double mu2, mu3, m0, m1, a0, a1, a2, a3;

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
