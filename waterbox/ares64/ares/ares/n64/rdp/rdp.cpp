#include <n64/n64.hpp>

#if defined(MAME_RDP)
#include "emu.h"
#include "includes/n64.h"

struct n64_periphs_impl : public n64_periphs {
  auto dp_full_sync() -> void override {
    ares::Nintendo64::rdp.syncFull();
  }

  static auto instance() -> n64_periphs_impl* {
    static n64_periphs_impl* inst = new n64_periphs_impl();
    return inst;
  }
};
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

  #if defined(MAME_RDP)
  state = new n64_state((u32*)rdram.ram.data, (u32*)rsp.dmem.data, n64_periphs_impl::instance());
  state->video_start();
  #endif
}

auto RDP::unload() -> void {
  debugger = {};
  node.reset();

  #if defined(MAME_RDP)
  state.reset();
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
