#include <n64/n64.hpp>

#include "angrylion.h"

namespace ares::Nintendo64 {

VI vi;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

bool BobDeinterlace = false;
bool FastVI = false;
u32* OutFrameBuffer;

auto VI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("VI");

  u32 width = 640;
  u32 height = 576;

  screen = node->append<Node::Video::Screen>("Screen", width, height);
  screen->setRefresh({&VI::refresh, this});
  screen->colors((1 << 24) + (1 << 15), [&](n32 color) -> n64 {
    if(color < (1 << 24)) {
      u64 a = 65535;
      u64 r = image::normalize(color >> 16 & 255, 8, 16);
      u64 g = image::normalize(color >>  8 & 255, 8, 16);
      u64 b = image::normalize(color >>  0 & 255, 8, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    } else {
      u64 a = 65535;
      u64 r = image::normalize(color >> 10 & 31, 5, 16);
      u64 g = image::normalize(color >>  5 & 31, 5, 16);
      u64 b = image::normalize(color >>  0 & 31, 5, 16);
      return a << 48 | r << 32 | g << 16 | b << 0;
    }
  });

  screen->setSize(640, 480);

  debugger.load(node);
}

auto VI::unload() -> void {
  debugger = {};
  node->remove(screen);
  screen.reset();
  node.reset();
}

auto VI::main() -> void {
  //field is not compared
  if(io.vcounter << 1 == io.coincidence) {
    mi.raise(MI::IRQ::VI);
  }

  if(++io.vcounter >= (Region::NTSC() ? 262 : 312) + io.field) {
    io.vcounter = 0;
    io.field = io.field + 1 & io.serrate;
    
    angrylion::UpdateScreen(FastVI);
    refresh();
  }

  if(Region::NTSC()) step(system.frequency() / 60 / 262);
  if(Region::PAL ()) step(system.frequency() / 50 / 312);
}

auto VI::step(u32 clocks) -> void {
  Thread::clock += clocks;
}

auto VI::refresh() -> void {
  angrylion::FinalizeFrame(BobDeinterlace);
  refreshed = true;
}

auto VI::power(bool reset) -> void {
  Thread::reset();
  screen->power();
  io = {};
  refreshed = false;
}

}
