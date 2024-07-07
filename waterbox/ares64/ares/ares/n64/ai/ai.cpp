#include <n64/n64.hpp>

namespace ares::Nintendo64 {

AI ai;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto AI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("AI");

  stream = node->append<Node::Audio::Stream>("AI");
  stream->setChannels(2);
  stream->setFrequency(44100.0);

  debugger.load(node);
}

auto AI::unload() -> void {
  debugger = {};
  node->remove(stream);
  stream.reset();
  node.reset();
}

auto AI::main() -> void {
  while(Thread::clock < 0) {
    f64 left = 0, right = 0;
    sample(left, right);
    stream->frame(left, right);
    step(dac.period);
  }
}

auto AI::sample(f64& left, f64& right) -> void {
  if(io.dmaCount == 0) return;

  if(io.dmaLength[0] && io.dmaEnable) {
    io.dmaAddress[0].bit(13,23) += io.dmaAddressCarry;
    auto data  = rdram.ram.read<Word>(io.dmaAddress[0], "AI");
    auto l     = s16(data >> 16);
    auto r     = s16(data >>  0);
    left       = l / 32768.0;
    right      = r / 32768.0;

    io.dmaAddress[0].bit(0,12) += 4;
    io.dmaAddressCarry          = io.dmaAddress[0].bit(0,12) == 0;
    io.dmaLength[0]            -= 4;
  }
  if(!io.dmaLength[0]) {
    if(--io.dmaCount) {
      io.dmaAddress[0]  = io.dmaAddress[1];
      io.dmaLength [0]  = io.dmaLength [1];
      io.dmaOriginPc[0] = io.dmaOriginPc[1];
      mi.raise(MI::IRQ::AI);
    }
  }
}

auto AI::power(bool reset) -> void {
  Thread::reset();

  fifo[0] = {};
  fifo[1] = {};
  io = {};
  dac.frequency = 44100;
  dac.precision = 16;
  dac.period    = system.frequency() / dac.frequency;
}

}
