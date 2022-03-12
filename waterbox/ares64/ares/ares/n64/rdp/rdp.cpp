#include <n64/n64.hpp>

#if defined(ANGRYLION_RDP)
#include "Gfx #1.3.h"
#endif

namespace ares::Nintendo64 {

RDP rdp;
#include "render.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto RDP::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("RDP");
  debugger.load(node);

  #if defined(ANGRYLION_RDP)
  puts("starting RDP video");
  angrylion::RomOpen();
  #endif
}

auto RDP::unload() -> void {
  debugger = {};
  node.reset();

  #if defined(ANGRYLION_RDP)
  angrylion::RomClosed();
  #endif
}

auto RDP::main() -> void {
  step(system.frequency());
}

auto RDP::step(u32 clocks) -> void {
  Thread::clock += clocks;
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
