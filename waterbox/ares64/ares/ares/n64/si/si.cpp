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

/*if(auto fp = system.pak->read("pif.sm5.rom")) {
    //load 1KB ROM and mirror it to 4KB
    fp->read({SM5K::ROM, 1024});
    memory::copy(&SM5K::ROM[1024], &SM5K::ROM[0], 1024);
    memory::copy(&SM5K::ROM[2048], &SM5K::ROM[0], 1024);
    memory::copy(&SM5K::ROM[3072], &SM5K::ROM[0], 1024);
  }*/
}

auto SI::unload() -> void {
  debugger = {};
  node.reset();
}

auto SI::power(bool reset) -> void {
  io = {};
}

}
