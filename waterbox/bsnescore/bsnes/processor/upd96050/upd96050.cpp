#include <processor/processor.hpp>
#include "upd96050.hpp"

namespace Processor {

#include "instructions.cpp"
#include "memory.cpp"
#include "disassembler.cpp"
#include "serialization.cpp"

auto uPD96050::power() -> void {
  if(revision == Revision::uPD7725) {
    regs.pc.resize(11);
    regs.rp.resize(10);
    regs.dp.resize( 8);
  }

  if(revision == Revision::uPD96050) {
    regs.pc.resize(14);
    regs.rp.resize(11);
    regs.dp.resize(11);
  }

  for(auto n : range(16)) regs.stack[n] = 0x0000;
  regs.pc = 0x0000;
  regs.rp = 0x0000;
  regs.dp = 0x0000;
  regs.sp = 0x0;
  regs.si = 0x0000;
  regs.so = 0x0000;
  regs.k = 0x0000;
  regs.l = 0x0000;
  regs.m = 0x0000;
  regs.n = 0x0000;
  regs.a = 0x0000;
  regs.b = 0x0000;
  regs.tr = 0x0000;
  regs.trb = 0x0000;
  regs.dr = 0x0000;
  regs.sr = 0x0000;
  regs.sr.siack = 0;
  regs.sr.soack = 0;

  flags.a = 0x0000;
  flags.b = 0x0000;
}

}
