#include <n64/n64.hpp>

namespace ares::Nintendo64 {

RSP rsp;
#include "decoder.cpp"
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
  while(Thread::clock < 0) {
    if(status.halted) return step(128);
    instruction();
  }
}

auto RSP::instruction() -> void {
  if constexpr(Accuracy::RSP::Recompiler) {
    auto block = recompiler.block(ipu.pc);
    block->execute(*this);
  }

  if constexpr(Accuracy::RSP::Interpreter) {
    u32 instruction = imem.read<Word>(ipu.pc);
    instructionPrologue(instruction);
    pipeline.begin();
    OpInfo op0 = decoderEXECUTE(instruction);
    pipeline.issue(op0);
    interpreterEXECUTE();

    if(!pipeline.singleIssue && !op0.branch()) {
      u32 instruction = imem.read<Word>(ipu.pc + 4);
      OpInfo op1 = decoderEXECUTE(instruction);

      if(canDualIssue(op0, op1)) {
        instructionEpilogue(0);
        instructionPrologue(instruction);
        pipeline.issue(op1);
        interpreterEXECUTE();
      }
    }

    pipeline.end();
    instructionEpilogue(0);
  }

  //this handles all stepping for the interpreter
  //with the recompiler, it only steps for taken branch stalls
  step(pipeline.clocks);
}

auto RSP::instructionPrologue(u32 instruction) -> void {
  pipeline.address = ipu.pc;
  pipeline.instruction = instruction;
  debugger.instruction();
}

auto RSP::instructionEpilogue(u32 clocks) -> s32 {
  if constexpr(Accuracy::RSP::Recompiler) {
    step(clocks);
  }

  ipu.r[0].u32 = 0;

  switch(branch.state) {
  case Branch::Step: ipu.pc += 4; return status.halted;
  case Branch::Take: ipu.pc += 4; branch.delaySlot(); return status.halted;
  case Branch::DelaySlot:
    ipu.pc = branch.pc;
    branch.reset();
    pipeline.stall();
    if(branch.pc & 4) pipeline.singleIssue = 1;
    return 1;
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
  for(auto& r : vpu.r) r = zero;
  vpu.acch = zero;
  vpu.accm = zero;
  vpu.accl = zero;
  vpu.vcoh = zero;
  vpu.vcol = zero;
  vpu.vcch = zero;
  vpu.vccl = zero;
  vpu.vce = zero;
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
    auto buffer = ares::Memory::FixedAllocator::get().tryAcquire(4_MiB);
    recompiler.allocator.resize(64_MiB, bump_allocator::executable, buffer);
    recompiler.reset();
  }

  if constexpr(Accuracy::RSP::SISD) {
    platform->status("RSP vectorization disabled (no SSE 4.1 support)");
  }
}

}
