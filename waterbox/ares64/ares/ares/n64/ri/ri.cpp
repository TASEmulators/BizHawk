#include <n64/n64.hpp>

namespace ares::Nintendo64 {

RI ri;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto RI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("RI");
  debugger.load(node);
}

auto RI::unload() -> void {
  debugger = {};
  node.reset();
}

auto RI::power(bool reset) -> void {
  io = {};
  if constexpr(!Accuracy::RDRAM::Broadcasting) {
    //simulate PIF ROM RDRAM power-on self test
    io.mode    = 0x0e;
    io.config  = 0x40;
    io.select  = 0x14;
    io.refresh = 0x0006'3634;

    //store RDRAM size result into memory
    rdram.ram.write<Word>(0x318, rdram.ram.size, "IPL3");  //CIC-NUS-6102
    rdram.ram.write<Word>(0x3f0, rdram.ram.size, "IPL3");  //CIC-NUS-6105
  }
}

}
