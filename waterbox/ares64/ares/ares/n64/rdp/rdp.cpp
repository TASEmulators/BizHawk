#include <n64/n64.hpp>

#include "angrylion.h"

namespace ares::Nintendo64 {

RDP rdp;
#include "render.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto RDP::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("RDP");
  debugger.load(node);

  angrylion::Init();
}

auto RDP::unload() -> void {
  debugger = {};
  node.reset();
}

auto RDP::crash(const char *reason) -> void {
  debug(unusual, "[RDP] software triggered a hardware bug; RDP crashed and will stop responding. Reason: ", reason);
  command.crashed = 1;
  //guard against asynchronous reporting of crash state. We want the RDP to report that it's busy forever
  command.pipeBusy = 1;
  command.bufferBusy = 1;
}

auto RDP::main() -> void {
  const u32 clocks = system.frequency();
  while(Thread::clock < 0) {
    step(clocks);
    command.clock += clocks / 3;
  }
}

auto RDP::power(bool reset) -> void {
  Thread::reset();
  command = {};
  edge = {};
  shade = {};
  texture = {};
  zbuffer = {};
  rectangle = {};
  other = {};
  fog = {};
  blend = {};
  primitive = {};
  environment = {};
  combine = {};
  tlut = {};
  load_ = {};
  tileSize = {};
  tile = {};
  set = {};
  primitiveDepth = {};
  scissor = {};
  convert = {};
  key = {};
  fillRectangle_ = {};
  io.bist = {};
  io.test = {};
}

}
