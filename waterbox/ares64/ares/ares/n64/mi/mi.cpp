#include <n64/n64.hpp>

namespace ares::Nintendo64 {

MI mi;
#include "io.cpp"
#include "debugger.cpp"
#include "serialization.cpp"

auto MI::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("MI");

  debugger.load(node);
}

auto MI::unload() -> void {
  node.reset();
  debugger = {};
}

auto MI::raise(IRQ source) -> void {
  debugger.interrupt((u32)source);
  switch(source) {
  case IRQ::SP: irq.sp.line = 1; break;
  case IRQ::SI: irq.si.line = 1; break;
  case IRQ::AI: irq.ai.line = 1; break;
  case IRQ::VI: irq.vi.line = 1; break;
  case IRQ::PI: irq.pi.line = 1; break;
  case IRQ::DP: irq.dp.line = 1; break;
  }
  poll();
}

auto MI::lower(IRQ source) -> void {
  switch(source) {
  case IRQ::SP: irq.sp.line = 0; break;
  case IRQ::SI: irq.si.line = 0; break;
  case IRQ::AI: irq.ai.line = 0; break;
  case IRQ::VI: irq.vi.line = 0; break;
  case IRQ::PI: irq.pi.line = 0; break;
  case IRQ::DP: irq.dp.line = 0; break;
  }
  poll();
}

auto MI::poll() -> void {
  bool line = 0;
  line |= irq.sp.line & irq.sp.mask;
  line |= irq.si.line & irq.si.mask;
  line |= irq.ai.line & irq.ai.mask;
  line |= irq.vi.line & irq.vi.mask;
  line |= irq.pi.line & irq.pi.mask;
  line |= irq.dp.line & irq.dp.mask;
  cpu.scc.cause.interruptPending.bit(2) = line;
}

auto MI::power(bool reset) -> void {
  irq = {};
  io = {};
}

}
