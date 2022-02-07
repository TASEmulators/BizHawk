#pragma once

#include <nall/dsp/dsp.hpp>

//DC offset removal IIR filter

namespace nall::DSP::IIR {

struct DCRemoval {
  inline auto reset() -> void;
  inline auto process(double in) -> double;  //normalized sample (-1.0 to +1.0)

private:
  double x;
  double y;
};

auto DCRemoval::reset() -> void {
  x = 0.0;
  y = 0.0;
}

auto DCRemoval::process(double in) -> double {
  x = 0.999 * x + in - y;
  y = in;
  return x;
}

}
