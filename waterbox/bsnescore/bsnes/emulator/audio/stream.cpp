#include <emulibc.h>

ECL_INVISIBLE auto Stream::reset(uint channelCount, double inputFrequency, double outputFrequency) -> void {
  channels.reset();
  channels.resize(channelCount);

  for(auto& channel : channels) {
    channel.filters.reset();
  }

  setFrequency(inputFrequency, outputFrequency);
}

ECL_INVISIBLE auto Stream::reset() -> void {
  for(auto& channel : channels) {
    channel.resampler.reset(this->inputFrequency, this->outputFrequency);
  }
}

ECL_INVISIBLE auto Stream::frequency() const -> double {
  return inputFrequency;
}

ECL_INVISIBLE auto Stream::setFrequency(double inputFrequency, maybe<double> outputFrequency) -> void {
  this->inputFrequency = inputFrequency;
  if(outputFrequency) this->outputFrequency = outputFrequency();

  for(auto& channel : channels) {
    channel.nyquist.reset();
    channel.resampler.reset(this->inputFrequency, this->outputFrequency);
  }

  if(this->inputFrequency >= this->outputFrequency * 2) {
    //add a low-pass filter to prevent aliasing during resampling
    double cutoffFrequency = min(25000.0, this->outputFrequency / 2.0 - 2000.0);
    for(auto& channel : channels) {
      uint passes = 3;
      for(uint pass : range(passes)) {
        DSP::IIR::Biquad filter;
        double q = DSP::IIR::Biquad::butterworth(passes * 2, pass);
        filter.reset(DSP::IIR::Biquad::Type::LowPass, cutoffFrequency, this->inputFrequency, q);
        channel.nyquist.append(filter);
      }
    }
  }
}

ECL_INVISIBLE auto Stream::addDCRemovalFilter() -> void {
  return;  //todo: test to ensure this is desirable before enabling
  for(auto& channel : channels) {
    Filter filter{Filter::Mode::DCRemoval, Filter::Type::None, Filter::Order::None};
    channel.filters.append(filter);
  }
}

ECL_INVISIBLE auto Stream::addLowPassFilter(double cutoffFrequency, Filter::Order order, uint passes) -> void {
  for(auto& channel : channels) {
    for(uint pass : range(passes)) {
      if(order == Filter::Order::First) {
        Filter filter{Filter::Mode::OnePole, Filter::Type::LowPass, Filter::Order::First};
        filter.onePole.reset(DSP::IIR::OnePole::Type::LowPass, cutoffFrequency, inputFrequency);
        channel.filters.append(filter);
      }
      if(order == Filter::Order::Second) {
        Filter filter{Filter::Mode::Biquad, Filter::Type::LowPass, Filter::Order::Second};
        double q = DSP::IIR::Biquad::butterworth(passes * 2, pass);
        filter.biquad.reset(DSP::IIR::Biquad::Type::LowPass, cutoffFrequency, inputFrequency, q);
        channel.filters.append(filter);
      }
    }
  }
}

ECL_INVISIBLE auto Stream::addHighPassFilter(double cutoffFrequency, Filter::Order order, uint passes) -> void {
  for(auto& channel : channels) {
    for(uint pass : range(passes)) {
      if(order == Filter::Order::First) {
        Filter filter{Filter::Mode::OnePole, Filter::Type::HighPass, Filter::Order::First};
        filter.onePole.reset(DSP::IIR::OnePole::Type::HighPass, cutoffFrequency, inputFrequency);
        channel.filters.append(filter);
      }
      if(order == Filter::Order::Second) {
        Filter filter{Filter::Mode::Biquad, Filter::Type::HighPass, Filter::Order::Second};
        double q = DSP::IIR::Biquad::butterworth(passes * 2, pass);
        filter.biquad.reset(DSP::IIR::Biquad::Type::HighPass, cutoffFrequency, inputFrequency, q);
        channel.filters.append(filter);
      }
    }
  }
}

ECL_INVISIBLE auto Stream::pending() const -> uint {
  if(!channels) return 0;
  return channels[0].resampler.pending();
}

ECL_INVISIBLE auto Stream::read(double samples[]) -> uint {
  for(uint c : range(channels.size())) samples[c] = channels[c].resampler.read();
  return channels.size();
}

ECL_INVISIBLE auto Stream::write(const double samples[]) -> void {
  for(auto c : range(channels.size())) {
    double sample = samples[c] + 1e-25;  //constant offset used to suppress denormals
    for(auto& filter : channels[c].filters) {
      switch(filter.mode) {
      case Filter::Mode::DCRemoval: sample = filter.dcRemoval.process(sample); break;
      case Filter::Mode::OnePole: sample = filter.onePole.process(sample); break;
      case Filter::Mode::Biquad: sample = filter.biquad.process(sample); break;
      }
    }
    for(auto& filter : channels[c].nyquist) {
      sample = filter.process(sample);
    }
    channels[c].resampler.write(sample);
  }

  audio.process();
}

ECL_INVISIBLE auto Stream::serialize(serializer& s) -> void {
  for(auto& channel : channels) {
    channel.resampler.serialize(s);
  }
  s.real(inputFrequency);
  s.real(outputFrequency);
}
