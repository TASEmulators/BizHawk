#include <n64/n64.hpp>

namespace ares::Nintendo64 {

CPU cpu;
#include "context.cpp"
#include "icache.cpp"
#include "dcache.cpp"
#include "tlb.cpp"
#include "memory.cpp"
#include "exceptions.cpp"
#include "interpreter.cpp"
#include "interpreter-ipu.cpp"
#include "interpreter-scc.cpp"
#include "interpreter-fpu.cpp"
#include "recompiler.cpp"
#include "debugger.cpp"
#include "serialization.cpp"
#include "disassembler.cpp"

auto CPU::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("CPU");
  debugger.load(node);
}

auto CPU::unload() -> void {
  debugger.unload();
  node.reset();
}

auto CPU::main() -> void {
  instruction();
  synchronize();
}

auto CPU::step(u32 clocks) -> void {
  Thread::clock += clocks;
}

auto CPU::synchronize() -> void {
  auto clocks = Thread::clock * 2;
  Thread::clock = 0;

   vi.clock -= clocks;
   ai.clock -= clocks;
  rsp.clock -= clocks;
  rdp.clock -= clocks;
  while( vi.clock < 0)  vi.main();
  while( ai.clock < 0)  ai.main();
  while(rsp.clock < 0) rsp.main();
  while(rdp.clock < 0) rdp.main();

  queue.step(clocks, [](u32 event) {
    switch(event) {
    case Queue::RSP_DMA:       return rsp.dmaTransfer();
    case Queue::PI_DMA_Read:   return pi.dmaRead();
    case Queue::PI_DMA_Write:  return pi.dmaWrite();
    case Queue::SI_DMA_Read:   return si.dmaRead();
    case Queue::SI_DMA_Write:  return si.dmaWrite();
    }
  });

  clocks >>= 1;
  if(scc.count < scc.compare && scc.count + clocks >= scc.compare) {
    scc.cause.interruptPending.bit(Interrupt::Timer) = 1;
  }
  scc.count += clocks;
}

auto CPU::instruction() -> void {
  if(auto interrupts = scc.cause.interruptPending & scc.status.interruptMask) {
    if(scc.status.interruptEnable && !scc.status.exceptionLevel && !scc.status.errorLevel) {
      debugger.interrupt(scc.cause.interruptPending);
      step(1);
      return exception.interrupt();
    }
  }

  if constexpr(Accuracy::CPU::Recompiler) {
    auto address = devirtualize(ipu.pc)(0);
    auto block = recompiler.block(address);
    block->execute(*this);
  }

  if constexpr(Accuracy::CPU::Interpreter) {
    pipeline.address = ipu.pc;
    pipeline.instruction = fetch(ipu.pc);
    debugger.instruction();
    decoderEXECUTE();
    instructionEpilogue();
  }
}

auto CPU::instructionEpilogue() -> s32 {
  if constexpr(Accuracy::CPU::Recompiler) {
    icache.step(ipu.pc);  //simulates timings without performing actual icache loads
  }

  ipu.r[0].u64 = 0;

  if(--scc.random.index < scc.wired.index) {
    scc.random.index = 31;
  }

  switch(branch.state) {
  case Branch::Step: ipu.pc += 4; return 0;
  case Branch::Take: ipu.pc += 4; branch.delaySlot(); return 0;
  case Branch::DelaySlot: ipu.pc = branch.pc; branch.reset(); return 1;
  case Branch::Exception: branch.reset(); return 1;
  case Branch::Discard: ipu.pc += 8; branch.reset(); return 1;
  }

  unreachable;
}

auto CPU::power(bool reset) -> void {
  Thread::reset();

  pipeline = {};
  branch = {};
  context.endian = Context::Endian::Big;
  context.mode = Context::Mode::Kernel;
  context.bits = 64;
  for(auto& segment : context.segment) segment = Context::Segment::Unused;
  icache.power(reset);
  dcache.power(reset);
  for(auto& entry : tlb.entry) entry = {};
  tlb.physicalAddress = 0;
  for(auto& r : ipu.r) r.u64 = 0;
  ipu.lo.u64 = 0;
  ipu.hi.u64 = 0;
  ipu.r[29].u64 = u32(0xa400'1ff0);  //stack pointer
  ipu.pc = u32(0xbfc0'0000);
  scc = {};
  for(auto& r : fpu.r) r.u64 = 0;
  fpu.csr = {};
  fesetround(FE_TONEAREST);
  context.setMode();

  if constexpr(Accuracy::CPU::Recompiler) {
    auto buffer = ares::Memory::FixedAllocator::get().tryAcquire(64_MiB);
    recompiler.allocator.resize(64_MiB, bump_allocator::executable | bump_allocator::zero_fill, buffer);
    recompiler.reset();
  }
}

}
