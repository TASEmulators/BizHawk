#pragma once

#include <nall/dsp/dsp.hpp>

//one-pole first-order IIR filter

namespace nall::DSP::IIR {

struct OnePole {
  enum class Type : uint {
    LowPass,
    HighPass,
  };

  inline auto reset(Type type, double cutoffFrequency, double samplingFrequency) -> void;
  inline auto process(double in) -> double;  //normalized sample (-1.0 to +1.0)

private:
  Type type;
  double cutoffFrequency;
  double samplingFrequency;
  double a0, b1;  //coefficients
  double z1;      //first-order IIR
};

auto OnePole::reset(Type type, double cutoffFrequency, double samplingFrequency) -> void {
  this->type = type;
  this->cutoffFrequency = cutoffFrequency;
  this->samplingFrequency = samplingFrequency;

  z1 = 0.0;
  double x = cos(2.0 * Math::Pi * cutoffFrequency / samplingFrequency);
  if(type == Type::LowPass) {
    b1 = +2.0 - x - sqrt((+2.0 - x) * (+2.0 - x) - 1);
    a0 = 1.0 - b1;
  } else {
    b1 = -2.0 - x + sqrt((-2.0 - x) * (-2.0 - x) - 1);
    a0 = 1.0 + b1;
  }
}

auto OnePole::process(double in) -> double {
  return z1 = in * a0 + z1 * b1;
}

}
