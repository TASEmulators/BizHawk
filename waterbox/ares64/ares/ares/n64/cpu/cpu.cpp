#include <n64/n64.hpp>
#include <nall/gdb/server.hpp>

namespace ares::Nintendo64 {

CPU cpu;
#include "context.cpp"
#include "dcache.cpp"
#include "tlb.cpp"
#include "memory.cpp"
#include "exceptions.cpp"
#include "algorithms.cpp"
#include "interpreter.cpp"
#include "interpreter-ipu.cpp"
#include "interpreter-scc.cpp"
#include "interpreter-fpu.cpp"
#include "interpreter-cop2.cpp"
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
  while(!vi.refreshed) {
    instruction();
    synchronize();
  }

  vi.refreshed = false;
}

auto CPU::synchronize() -> void {
  auto clocks = Thread::clock;
  Thread::clock = 0;

   vi.clock -= clocks;
   ai.clock -= clocks;
  rsp.clock -= clocks;
  rdp.clock -= clocks;
  pif.clock -= clocks;
  vi.main();
  ai.main();
  rsp.main();
  rdp.main();
  pif.main();

  queue.step(clocks, [](u32 event) {
    switch(event) {
    case Queue::RSP_DMA:       return rsp.dmaTransferStep();
    case Queue::PI_DMA_Read:   return pi.dmaFinished();
    case Queue::PI_DMA_Write:  return pi.dmaFinished();
    case Queue::PI_BUS_Write:  return pi.writeFinished();
    case Queue::SI_DMA_Read:   return si.dmaRead();
    case Queue::SI_DMA_Write:  return si.dmaWrite();
    case Queue::SI_BUS_Write:  return si.writeFinished();
    case Queue::RTC_Tick:      return cartridge.rtc.tick();
    case Queue::DD_Clock_Tick:  return dd.rtc.tickClock();
    case Queue::DD_MECHA_Response:  return dd.mechaResponse();
    case Queue::DD_BM_Request:  return dd.bmRequest();
    case Queue::DD_Motor_Mode:  return dd.motorChange();
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
      step(1 * 2);
      return exception.interrupt();
    }
  }
  if (scc.nmiPending) {
    debugger.nmi();
    step(1 * 2);
    return exception.nmi();
  }
  if (scc.sysadFrozen) {
    step(1 * 2);
    return;
  }

  if constexpr(Accuracy::CPU::Recompiler) {
    // Fast path: attempt to lookup previously compiled blocks with devirtualizeFast
    // and fastFetchBlock, this skips exception handling, error checking, and
    // code emitting pathways for maximum lookup performance.
    // As memory writes cause recompiler block invalidation, this shouldn't be detectable.
    if (auto address = devirtualizeFast(ipu.pc)) {
      if(auto block = recompiler.fastFetchBlock(address)) {
        block->execute(*this);
        return;
      }
    }

    if (auto address = devirtualize(ipu.pc)) {
      auto block = recompiler.block(ipu.pc, *address, false);
      block->execute(*this);
    }
  }

  if constexpr(Accuracy::CPU::Interpreter) {
    auto data = fetch(ipu.pc);
    if (!data) return;
    instructionPrologue(*data);
    decoderEXECUTE();
    instructionEpilogue();
  }
}

auto CPU::instructionPrologue(u32 instruction) -> void {
  pipeline.address = ipu.pc;
  pipeline.instruction = instruction;
  debugger.instruction();
}

auto CPU::instructionEpilogue() -> s32 {
  if constexpr(Accuracy::CPU::Recompiler) {
    //simulates timings without performing actual icache loads
    icache.step(ipu.pc, devirtualizeFast(ipu.pc));
  }

  ipu.r[0].u64 = 0;

  switch(branch.state) {
  case Branch::Step: ipu.pc += 4; return 0;
  case Branch::Take: ipu.pc += 4; branch.delaySlot(true); return 0;
  case Branch::NotTaken: ipu.pc += 4; branch.delaySlot(false); return 0;
  case Branch::DelaySlotTaken: ipu.pc = branch.pc; branch.reset(); return 1;
  case Branch::DelaySlotNotTaken: ipu.pc += 4; branch.reset(); return 0;
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
  for(auto& entry : tlb.entry) entry = {}, entry.synchronize();
  tlb.physicalAddress = 0;
  for(auto& r : ipu.r) r.u64 = 0;
  ipu.lo.u64 = 0;
  ipu.hi.u64 = 0;
  ipu.r[29].u64 = 0xffff'ffff'a400'1ff0ull;  //stack pointer
  ipu.pc = 0xffff'ffff'bfc0'0000ull;
  scc = {};
  for(auto& r : fpu.r) r.u64 = 0;
  fpu.csr = {};
  cop2 = {};
  fenv.setRound(float_env::toNearest);
  context.setMode();

  if constexpr(Accuracy::CPU::Recompiler) {
    auto buffer = ares::Memory::FixedAllocator::get().tryAcquire(4_MiB);
    recompiler.allocator.resize(4_MiB, bump_allocator::executable, buffer);
    recompiler.reset();
  }
}

}
