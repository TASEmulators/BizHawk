#include <n64/n64.hpp>

namespace ares::Nintendo64 {

SI si;
#include "dma.cpp"
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto SI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("SI");
  debugger.load(node);
}

auto SI::unload() -> void {
  debugger = {};
  node.reset();
}

auto SI::power(bool reset) -> void {
  io = {};
}

}
