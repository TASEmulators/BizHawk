#include <n64/n64.hpp>

namespace ares::Nintendo64 {

PIF pif;
#include "hle.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto PIF::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("PIF");
  rom.allocate(0x7c0);
  ram.allocate(0x040);

  debugger.load(node);
}

auto PIF::unload() -> void {
  debugger = {};
  rom.reset();
  ram.reset();
  node.reset();
}

auto PIF::main() -> void {
  while(Thread::clock < 0) {
    mainHLE();
  }
}

auto PIF::power(bool reset) -> void {
  Thread::reset();

  string pifrom = Region::PAL() ? "pif.pal.rom" : "pif.ntsc.rom";
  if(auto fp = system.pak->read(pifrom)) {
    rom.load(fp);
  }

  ram.fill();
  io = {};
  intram = {};
  state = Init;
}

}
