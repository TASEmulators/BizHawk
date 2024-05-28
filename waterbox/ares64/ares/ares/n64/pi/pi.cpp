#include <n64/n64.hpp>

namespace ares::Nintendo64 {

PI pi;
#include "dma.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto PI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("PI");

  debugger.load(node);
}

auto PI::unload() -> void {
  debugger = {};
  node.reset();
}

auto PI::power(bool reset) -> void {
  io = {};
  bsd1 = {};
  bsd2 = {};
}

}
