#pragma once

#include <nall/queue.hpp>
#include <nall/serializer.hpp>
#include <nall/dsp/dsp.hpp>

namespace nall::DSP::Resampler {

struct Cubic {
  inline auto inputFrequency() const -> double { return _inputFrequency; }
  inline auto outputFrequency() const -> double { return _outputFrequency; }

  inline auto reset(double inputFrequency, double outputFrequency = 0, uint queueSize = 0) -> void;
  inline auto setInputFrequency(double inputFrequency) -> void;
  inline auto pending() const -> bool;
  inline auto read() -> double;
  inline auto write(double sample) -> void;
  inline auto serialize(serializer&) -> void;

private:
  double _inputFrequency;
  double _outputFrequency;

  double _ratio;
  double _fraction;
  double _history[4];
  queue<double> _samples;
};

auto Cubic::reset(double inputFrequency, double outputFrequency, uint queueSize) -> void {
  _inputFrequency = inputFrequency;
  _outputFrequency = outputFrequency ? outputFrequency : _inputFrequency;

  _ratio = _inputFrequency / _outputFrequency;
  _fraction = 0.0;
  for(auto& sample : _history) sample = 0.0;
  _samples.resize(queueSize ? queueSize : _outputFrequency * 0.02);  //default to 20ms max queue size
}

auto Cubic::setInputFrequency(double inputFrequency) -> void {
  _inputFrequency = inputFrequency;
  _ratio = _inputFrequency / _outputFrequency;
}

auto Cubic::pending() const -> bool {
  return _samples.pending();
}

auto Cubic::read() -> double {
  return _samples.read();
}

auto Cubic::write(double sample) -> void {
  auto& mu = _fraction;
  auto& s = _history;

  s[0] = s[1];
  s[1] = s[2];
  s[2] = s[3];
  s[3] = sample;

  while(mu <= 1.0) {
    double A = s[3] - s[2] - s[0] + s[1];
    double B = s[0] - s[1] - A;
    double C = s[2] - s[0];
    double D = s[1];

    _samples.write(A * mu * mu * mu + B * mu * mu + C * mu + D);
    mu += _ratio;
  }

  mu -= 1.0;
}

auto Cubic::serialize(serializer& s) -> void {
  s.real(_inputFrequency);
  s.real(_outputFrequency);
  s.real(_ratio);
  s.real(_fraction);
  s.array(_history);
  _samples.serialize(s);
}

}
