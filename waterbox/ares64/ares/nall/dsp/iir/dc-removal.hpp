#pragma once

//DC offset removal IIR filter

namespace nall::DSP::IIR {

struct DCRemoval {
  auto reset() -> void;
  auto process(f64 in) -> f64;  //normalized sample (-1.0 to +1.0)

private:
  f64 x;
  f64 y;
};

inline auto DCRemoval::reset() -> void {
  x = 0.0;
  y = 0.0;
}

inline auto DCRemoval::process(f64 in) -> f64 {
  x = 0.999 * x + in - y;
  y = in;
  return x;
}

}
