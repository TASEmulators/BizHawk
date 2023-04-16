#include <n64/n64.hpp>

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
  pif.clock -= clocks;
  while( vi.clock < 0)  vi.main();
  while( ai.clock < 0)  ai.main();
  while(rsp.clock < 0) rsp.main();
  while(rdp.clock < 0) rdp.main();
  while(pif.clock < 0) pif.main();

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
    case Queue::DD_Clock_Tick:  return dd.rtcTickClock();
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
      step(1);
      return exception.interrupt();
    }
  }
  if (scc.nmiPending) {
    debugger.nmi();
    step(1);
    return exception.nmi();
  }

  if constexpr(Accuracy::CPU::Recompiler) {
    if (auto address = devirtualize(ipu.pc)) {
      auto block = recompiler.block(*address);
      block->execute(*this);
    }
  }

  if constexpr(Accuracy::CPU::Interpreter) {
    pipeline.address = ipu.pc;
    auto data = fetch(ipu.pc);
    if (!data) return;
    pipeline.instruction = *data;
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
    recompiler.allocator.resize(4_MiB, bump_allocator::executable | bump_allocator::zero_fill, buffer);
    recompiler.reset();
  }
}

}
