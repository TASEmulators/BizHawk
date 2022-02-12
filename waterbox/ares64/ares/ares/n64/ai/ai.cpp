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
  sample();
  step(dac.period);
}

auto AI::sample() -> void {
  if(io.dmaCount == 0) return stream->frame(0.0, 0.0);

  auto data  = rdram.ram.read<Word>(io.dmaAddress[0]);
  auto left  = s16(data >> 16);
  auto right = s16(data >>  0);
  stream->frame(left / 32768.0, right / 32768.0);

  io.dmaAddress[0] += 4;
  io.dmaLength [0] -= 4;
  if(!io.dmaLength[0]) {
    mi.raise(MI::IRQ::AI);
    if(--io.dmaCount) {
      io.dmaAddress[0] = io.dmaAddress[1];
      io.dmaLength [0] = io.dmaLength [1];
    }
  }
}

auto AI::step(u32 clocks) -> void {
  Thread::clock += clocks;
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
