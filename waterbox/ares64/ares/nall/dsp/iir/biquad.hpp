#pragma once

//transposed direct form II biquadratic second-order IIR filter

namespace nall::DSP::IIR {

struct Biquad {
  enum class Type : u32 {
    LowPass,
    HighPass,
    BandPass,
    Notch,
    Peak,
    LowShelf,
    HighShelf,
  };

  auto reset(Type type, f64 cutoffFrequency, f64 samplingFrequency, f64 quality, f64 gain = 0.0) -> void;
  auto process(f64 in) -> f64;  //normalized sample (-1.0 to +1.0)

  static auto shelf(f64 gain, f64 slope) -> f64;
  static auto butterworth(u32 order, u32 phase) -> f64;

private:
  Type type;
  f64 cutoffFrequency;
  f64 samplingFrequency;
  f64 quality;             //frequency response quality
  f64 gain;                //peak gain
  f64 a0, a1, a2, b1, b2;  //coefficients
  f64 z1, z2;              //second-order IIR
};

inline auto Biquad::reset(Type type, f64 cutoffFrequency, f64 samplingFrequency, f64 quality, f64 gain) -> void {
  this->type = type;
  this->cutoffFrequency = cutoffFrequency;
  this->samplingFrequency = samplingFrequency;
  this->quality = quality;
  this->gain = gain;

  z1 = 0.0;
  z2 = 0.0;

  f64 v = pow(10, fabs(gain) / 20.0);
  f64 k = tan(Math::Pi * cutoffFrequency / samplingFrequency);
  f64 q = quality;
  f64 n = 0.0;

  switch(type) {

  case Type::LowPass:
    n = 1 / (1 + k / q + k * k);
    a0 = k * k * n;
    a1 = 2 * a0;
    a2 = a0;
    b1 = 2 * (k * k - 1) * n;
    b2 = (1 - k / q + k * k) * n;
    break;

  case Type::HighPass:
    n = 1 / (1 + k / q + k * k);
    a0 = 1 * n;
    a1 = -2 * a0;
    a2 = a0;
    b1 = 2 * (k * k - 1) * n;
    b2 = (1 - k / q + k * k) * n;
    break;

  case Type::BandPass:
    n = 1 / (1 + k / q + k * k);
    a0 = k / q * n;
    a1 = 0;
    a2 = -a0;
    b1 = 2 * (k * k - 1) * n;
    b2 = (1 - k / q + k * k) * n;
    break;

  case Type::Notch:
    n = 1 / (1 + k / q + k * k);
    a0 = (1 + k * k) * n;
    a1 = 2 * (k * k - 1) * n;
    a2 = a0;
    b1 = a1;
    b2 = (1 - k / q + k * k) * n;
    break;

  case Type::Peak:
    if(gain >= 0) {
      n = 1 / (1 + 1 / q * k + k * k);
      a0 = (1 + v / q * k + k * k) * n;
      a1 = 2 * (k * k - 1) * n;
      a2 = (1 - v / q * k + k * k) * n;
      b1 = a1;
      b2 = (1 - 1 / q * k + k * k) * n;
    } else {
      n = 1 / (1 + v / q * k + k * k);
      a0 = (1 + 1 / q * k + k * k) * n;
      a1 = 2 * (k * k - 1) * n;
      a2 = (1 - 1 / q * k + k * k) * n;
      b1 = a1;
      b2 = (1 - v / q * k + k * k) * n;
    }
    break;

  case Type::LowShelf:
    if(gain >= 0) {
      n = 1 / (1 + k / q + k * k);
      a0 = (1 + sqrt(v) / q * k + v * k * k) * n;
      a1 = 2 * (v * k * k - 1) * n;
      a2 = (1 - sqrt(v) / q * k + v * k * k) * n;
      b1 = 2 * (k * k - 1) * n;
      b2 = (1 - k / q + k * k) * n;
    } else {
      n = 1 / (1 + sqrt(v) / q * k + v * k * k);
      a0 = (1 + k / q + k * k) * n;
      a1 = 2 * (k * k - 1) * n;
      a2 = (1 - k / q + k * k) * n;
      b1 = 2 * (v * k * k - 1) * n;
      b2 = (1 - sqrt(v) / q * k + v * k * k) * n;
    }
    break;

  case Type::HighShelf:
    if(gain >= 0) {
      n = 1 / (1 + k / q + k * k);
      a0 = (v + sqrt(v) / q * k + k * k) * n;
      a1 = 2 * (k * k - v) * n;
      a2 = (v - sqrt(v) / q * k + k * k) * n;
      b1 = 2 * (k * k - 1) * n;
      b2 = (1 - k / q + k * k) * n;
    } else {
      n = 1 / (v + sqrt(v) / q * k + k * k);
      a0 = (1 + k / q + k * k) * n;
      a1 = 2 * (k * k - 1) * n;
      a2 = (1 - k / q + k * k) * n;
      b1 = 2 * (k * k - v) * n;
      b2 = (v - sqrt(v) / q * k + k * k) * n;
    }
    break;

  }
}

inline auto Biquad::process(f64 in) -> f64 {
  f64 out = in * a0 + z1;
  z1 = in * a1 + z2 - b1 * out;
  z2 = in * a2 - b2 * out;
  return out;
}

//compute Q values for low-shelf and high-shelf filtering
inline auto Biquad::shelf(f64 gain, f64 slope) -> f64 {
  f64 a = pow(10, gain / 40);
  return 1 / sqrt((a + 1 / a) * (1 / slope - 1) + 2);
}

//compute Q values for Nth-order butterworth filtering
inline auto Biquad::butterworth(u32 order, u32 phase) -> f64 {
  return -0.5 / cos(Math::Pi * (phase + order + 0.5) / order);
}

}
