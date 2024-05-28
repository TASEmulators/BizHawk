#include <ares/ares.hpp>
#include "sm5k.hpp"

namespace ares {

#include "timer.cpp"
#include "memory.cpp"
#include "instruction.cpp"
#include "instructions.cpp"
#include "serialization.cpp"
#include "disassembler.cpp"

auto SM5K::setP1(n4 data) -> void {
  if(P1.bit(0) && !data.bit(0)) IFA = 1;
  if(P1.bit(1) && !data.bit(1)) IFB = 1;
  if(P1.bit(1) && !data.bit(1) && RC == 3) timerIncrement();
  P1 = data;
}

auto SM5K::power() -> void {
  static const n8 Undefined = 0;

  PC    = 0;
  SP    = 0;
  SR[0] = 0;
  SR[1] = 0;
  SR[2] = 0;
  SR[3] = 0;
  A     = Undefined;
  X     = Undefined;
  P0    = 0;
  P1    = 0;
  P2    = 0;
  P3    = 0;
  P4    = 0;
  P5    = 0;
  IFA   = 0;
  IFB   = 0;
  IFT   = 0;
  IME   = 0;
  C     = Undefined;
  B     = Undefined;
  R3    = 0;
  R8    = 0;
  R9    = 0;
  RA    = 0;
  RB    = 0;
  RC    = 0;
  RE    = 0;
  RF    = 0;
  SKIP  = 0;
}

}
