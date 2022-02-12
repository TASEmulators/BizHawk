#include <n64/n64.hpp>

namespace ares::Nintendo64 {

RSP rsp;
#include "dma.cpp"
#include "io.cpp"
#include "interpreter.cpp"
#include "interpreter-ipu.cpp"
#include "interpreter-scc.cpp"
#include "interpreter-vpu.cpp"
#include "recompiler.cpp"
#include "debugger.cpp"
#include "serialization.cpp"
#include "disassembler.cpp"

auto RSP::load(Node::Object parent) -> void {
  node = parent->append<Node::Object>("RSP");
  dmem.allocate(4_KiB);
  imem.allocate(4_KiB);
  debugger.load(node);
}

auto RSP::unload() -> void {
  debugger.unload();
  dmem.reset();
  imem.reset();
  node.reset();
}

auto RSP::main() -> void {
  if(status.halted) return step(128);
  instruction();
}

auto RSP::step(u32 clocks) -> void {
  Thread::clock += clocks;
}

auto RSP::instruction() -> void {
  if constexpr(Accuracy::RSP::Recompiler) {
    auto block = recompiler.block(ipu.pc);
    block->execute(*this);
  }

  if constexpr(Accuracy::RSP::Interpreter) {
    pipeline.address = ipu.pc;
    pipeline.instruction = imem.read<Word>(pipeline.address);
    debugger.instruction();
    decoderEXECUTE();
    instructionEpilogue();
    step(3);
  }
}

auto RSP::instructionEpilogue() -> s32 {
  if constexpr(Accuracy::RSP::Recompiler) {
    step(3);
  }

  ipu.r[0].u32 = 0;

  switch(branch.state) {
  case Branch::Step: ipu.pc += 4; return status.halted;
  case Branch::Take: ipu.pc += 4; branch.delaySlot(); return status.halted;
  case Branch::DelaySlot: ipu.pc = branch.pc; branch.reset(); return 1;
  }

  unreachable;
}

auto RSP::power(bool reset) -> void {
  Thread::reset();
  dmem.fill();
  imem.fill();

  pipeline = {};
  dma = {};
  status.semaphore = 0;
  status.halted = 1;
  status.broken = 0;
  status.full = 0;
  status.singleStep = 0;
  status.interruptOnBreak = 0;
  for(auto& signal : status.signal) signal = 0;
  for(auto& r : ipu.r) r.u32 = 0;
  ipu.pc = 0;
  branch = {};
  for(auto& r : vpu.r) r.u128 = 0;
  vpu.acch.u128 = 0;
  vpu.accm.u128 = 0;
  vpu.accl.u128 = 0;
  vpu.vcoh.u128 = 0;
  vpu.vcol.u128 = 0;
  vpu.vcch.u128 = 0;
  vpu.vccl.u128 = 0;
  vpu.vce.u128 = 0;
  vpu.divin = 0;
  vpu.divout = 0;
  vpu.divdp = 0;

  reciprocals[0] = u16(~0);
  for(u16 index : range(1, 512)) {
    u64 a = index + 512;
    u64 b = (u64(1) << 34) / a;
    reciprocals[index] = u16(b + 1 >> 8);
  }

  for(u16 index : range(0, 512)) {
    u64 a = index + 512 >> (index % 2 == 1);
    u64 b = 1 << 17;
    //find the largest b where b < 1.0 / sqrt(a)
    while(a * (b + 1) * (b + 1) < (u64(1) << 44)) b++;
    inverseSquareRoots[index] = u16(b >> 1);
  }

  if constexpr(Accuracy::RSP::Recompiler) {
    auto buffer = ares::Memory::FixedAllocator::get().tryAcquire(64_MiB);
    recompiler.allocator.resize(64_MiB, bump_allocator::executable | bump_allocator::zero_fill, buffer);
    recompiler.reset();
  }
}

}
