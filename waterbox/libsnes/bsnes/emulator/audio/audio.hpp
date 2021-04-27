#pragma once

#include <nall/dsp/iir/dc-removal.hpp>
#include <nall/dsp/iir/one-pole.hpp>
#include <nall/dsp/iir/biquad.hpp>
#include <nall/dsp/resampler/cubic.hpp>

namespace Emulator {

struct Interface;
struct Audio;
struct Filter;
struct Stream;

struct Audio {
  ~Audio();
  auto reset(Interface* interface) -> void;

  inline auto channels() const -> uint { return _channels; }
  inline auto frequency() const -> double { return _frequency; }
  inline auto volume() const -> double { return _volume; }
  inline auto balance() const -> double { return _balance; }

  auto setFrequency(double frequency) -> void;
  auto setVolume(double volume) -> void;
  auto setBalance(double balance) -> void;

  auto createStream(uint channels, double frequency) -> shared_pointer<Stream>;

private:
  auto process() -> void;

  Interface* _interface = nullptr;
  vector<shared_pointer<Stream>> _streams;

  uint _channels = 0;
  double _frequency = 48000.0;

  double _volume = 1.0;
  double _balance = 0.0;

  friend class Stream;
};

struct Filter {
  enum class Mode : uint { DCRemoval, OnePole, Biquad } mode;
  enum class Type : uint { None, LowPass, HighPass } type;
  enum class Order : uint { None, First, Second } order;

  DSP::IIR::DCRemoval dcRemoval;
  DSP::IIR::OnePole onePole;
  DSP::IIR::Biquad biquad;
};

struct Stream {
  auto reset(uint channels, double inputFrequency, double outputFrequency) -> void;
  auto reset() -> void;

  auto frequency() const -> double;
  auto setFrequency(double inputFrequency, maybe<double> outputFrequency = nothing) -> void;

  auto addDCRemovalFilter() -> void;
  auto addLowPassFilter(double cutoffFrequency, Filter::Order order, uint passes = 1) -> void;
  auto addHighPassFilter(double cutoffFrequency, Filter::Order order, uint passes = 1) -> void;

  auto pending() const -> uint;
  auto read(double samples[]) -> uint;
  auto write(const double samples[]) -> void;

  template<typename... P> auto sample(P&&... p) -> void {
    double samples[sizeof...(P)] = {forward<P>(p)...};
    write(samples);
  }

  auto serialize(serializer&) -> void;

private:
  struct Channel {
    vector<Filter> filters;
    vector<DSP::IIR::Biquad> nyquist;
    DSP::Resampler::Cubic resampler;
  };
  vector<Channel> channels;
  double inputFrequency;
  double outputFrequency;

  friend class Audio;
};

extern Audio audio;

}

#undef double
