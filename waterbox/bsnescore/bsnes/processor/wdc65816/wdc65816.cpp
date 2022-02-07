#include <processor/processor.hpp>
#include "wdc65816.hpp"

namespace Processor {

#include "registers.hpp"
#include "memory.cpp"
#include "algorithms.cpp"

#include "instructions-read.cpp"
#include "instructions-write.cpp"
#include "instructions-modify.cpp"
#include "instructions-pc.cpp"
#include "instructions-other.cpp"
#include "instruction.cpp"

auto WDC65816::power() -> void {
  r.pc.d = 0x000000;
  r.a  = 0x0000;
  r.x  = 0x0000;
  r.y  = 0x0000;
  r.s  = 0x01ff;
  r.d  = 0x0000;
  r.b  = 0x00;
  r.p  = 0x34;
  r.e  = 1;

  r.irq = 0;
  r.wai = 0;
  r.stp = 0;
  r.mar = 0x000000;
  r.mdr = 0x00;

  r.vector = 0xfffc;  //reset vector address
}

#include "registers.hpp"
#include "serialization.cpp"
#include "disassembler.cpp"

}
