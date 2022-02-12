#pragma once

//one-pole first-order IIR filter

namespace nall::DSP::IIR {

struct OnePole {
  enum class Type : u32 {
    LowPass,
    HighPass,
  };

  auto reset(Type type, f64 cutoffFrequency, f64 samplingFrequency) -> void;
  auto process(f64 in) -> f64;  //normalized sample (-1.0 to +1.0)

private:
  Type type;
  f64 cutoffFrequency;
  f64 samplingFrequency;
  f64 a0, b1;  //coefficients
  f64 z1;      //first-order IIR
};

inline auto OnePole::reset(Type type, f64 cutoffFrequency, f64 samplingFrequency) -> void {
  this->type = type;
  this->cutoffFrequency = cutoffFrequency;
  this->samplingFrequency = samplingFrequency;

  z1 = 0.0;
  f64 x = cos(2.0 * Math::Pi * cutoffFrequency / samplingFrequency);
  if(type == Type::LowPass) {
    b1 = +2.0 - x - sqrt((+2.0 - x) * (+2.0 - x) - 1);
    a0 = 1.0 - b1;
  } else {
    b1 = -2.0 - x + sqrt((-2.0 - x) * (-2.0 - x) - 1);
    a0 = 1.0 + b1;
  }
}

inline auto OnePole::process(f64 in) -> f64 {
  return z1 = in * a0 + z1 * b1;
}

}
