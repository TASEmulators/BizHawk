#include <processor/processor.hpp>
#include "gsu.hpp"

//note: multiplication results *may* sometimes be invalid when both CLSR and MS0 are set
//the product of multiplication in this mode (21mhz + fast-multiply) has not been analyzed;
//however, the timing of this mode has been confirmed to work as specified below

namespace Processor {

#include "instruction.cpp"
#include "instructions.cpp"
#include "serialization.cpp"
#include "disassembler.cpp"

auto GSU::power() -> void {
  for(auto& r : regs.r) {
    r.data = 0x0000;
    r.modified = false;
  }

  regs.sfr      = 0x0000;
  regs.pbr      = 0x00;
  regs.rombr    = 0x00;
  regs.rambr    = 0;
  regs.cbr      = 0x0000;
  regs.scbr     = 0x00;
  regs.scmr     = 0x00;
  regs.colr     = 0x00;
  regs.por      = 0x00;
  regs.bramr    = 0;
  regs.vcr      = 0x04;
  regs.cfgr     = 0x00;
  regs.clsr     = 0;
  regs.pipeline = 0x01;  //nop
  regs.ramaddr  = 0x0000;
  regs.reset();
}

}
