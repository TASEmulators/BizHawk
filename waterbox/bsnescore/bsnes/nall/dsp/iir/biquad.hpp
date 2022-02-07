#pragma once

#include <nall/dsp/dsp.hpp>

//transposed direct form II biquadratic second-order IIR filter

namespace nall::DSP::IIR {

struct Biquad {
  enum class Type : uint {
    LowPass,
    HighPass,
    BandPass,
    Notch,
    Peak,
    LowShelf,
    HighShelf,
  };

  inline auto reset(Type type, double cutoffFrequency, double samplingFrequency, double quality, double gain = 0.0) -> void;
  inline auto process(double in) -> double;  //normalized sample (-1.0 to +1.0)

  inline static auto shelf(double gain, double slope) -> double;
  inline static auto butterworth(uint order, uint phase) -> double;

private:
  Type type;
  double cutoffFrequency;
  double samplingFrequency;
  double quality;             //frequency response quality
  double gain;                //peak gain
  double a0, a1, a2, b1, b2;  //coefficients
  double z1, z2;              //second-order IIR
};

auto Biquad::reset(Type type, double cutoffFrequency, double samplingFrequency, double quality, double gain) -> void {
  this->type = type;
  this->cutoffFrequency = cutoffFrequency;
  this->samplingFrequency = samplingFrequency;
  this->quality = quality;
  this->gain = gain;

  z1 = 0.0;
  z2 = 0.0;

  double v = pow(10, fabs(gain) / 20.0);
  double k = tan(Math::Pi * cutoffFrequency / samplingFrequency);
  double q = quality;
  double n = 0.0;

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

auto Biquad::process(double in) -> double {
  double out = in * a0 + z1;
  z1 = in * a1 + z2 - b1 * out;
  z2 = in * a2 - b2 * out;
  return out;
}

//compute Q values for low-shelf and high-shelf filtering
auto Biquad::shelf(double gain, double slope) -> double {
  double a = pow(10, gain / 40);
  return 1 / sqrt((a + 1 / a) * (1 / slope - 1) + 2);
}

//compute Q values for Nth-order butterworth filtering
auto Biquad::butterworth(uint order, uint phase) -> double {
  return -0.5 / cos(Math::Pi * (phase + order + 0.5) / order);
}

}
