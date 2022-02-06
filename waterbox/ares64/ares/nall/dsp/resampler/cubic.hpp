#pragma once

#include <nall/queue.hpp>
#include <nall/serializer.hpp>

namespace nall::DSP::Resampler {

struct Cubic {
  auto inputFrequency() const -> f64 { return _inputFrequency; }
  auto outputFrequency() const -> f64 { return _outputFrequency; }

  auto reset(f64 inputFrequency, f64 outputFrequency = 0, u32 queueSize = 0) -> void;
  auto setInputFrequency(f64 inputFrequency) -> void;
  auto pending() const -> bool;
  auto read() -> f64;
  auto write(f64 sample) -> void;
  auto serialize(serializer&) -> void;

private:
  f64 _inputFrequency;
  f64 _outputFrequency;

  f64 _ratio;
  f64 _fraction;
  f64 _history[4];
  queue<f64> _samples;
};

inline auto Cubic::reset(f64 inputFrequency, f64 outputFrequency, u32 queueSize) -> void {
  _inputFrequency = inputFrequency;
  _outputFrequency = outputFrequency ? outputFrequency : _inputFrequency;

  _ratio = _inputFrequency / _outputFrequency;
  _fraction = 0.0;
  for(auto& sample : _history) sample = 0.0;
  _samples.resize(queueSize ? queueSize : _outputFrequency * 0.02);  //default to 20ms max queue size
}

inline auto Cubic::setInputFrequency(f64 inputFrequency) -> void {
  _inputFrequency = inputFrequency;
  _ratio = _inputFrequency / _outputFrequency;
}

inline auto Cubic::pending() const -> bool {
  return _samples.pending();
}

inline auto Cubic::read() -> double {
  return _samples.read();
}

inline auto Cubic::write(f64 sample) -> void {
  auto& mu = _fraction;
  auto& s = _history;

  s[0] = s[1];
  s[1] = s[2];
  s[2] = s[3];
  s[3] = sample;

  while(mu <= 1.0) {
    f64 A = s[3] - s[2] - s[0] + s[1];
    f64 B = s[0] - s[1] - A;
    f64 C = s[2] - s[0];
    f64 D = s[1];

    _samples.write(A * mu * mu * mu + B * mu * mu + C * mu + D);
    mu += _ratio;
  }

  mu -= 1.0;
}

inline auto Cubic::serialize(serializer& s) -> void {
  s(_inputFrequency);
  s(_outputFrequency);
  s(_ratio);
  s(_fraction);
  s(_history);
  s(_samples);
}

}
