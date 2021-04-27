//WDC 65C816 CPU core
//* Ricoh 5A22
//* Nintendo SA-1

#pragma once

namespace Processor {

struct WDC65816 {
  virtual auto idle() -> void = 0;
  virtual auto idleBranch() -> void {}
  virtual auto idleJump() -> void {}
  virtual auto read(uint addr) -> uint8 = 0;
  virtual auto write(uint addr, uint8 data) -> void = 0;
  virtual auto lastCycle() -> void = 0;
  virtual auto interruptPending() const -> bool = 0;
  virtual auto interrupt() -> void;
  virtual auto synchronizing() const -> bool = 0;

  virtual auto readDisassembler(uint addr) -> uint8 { return 0; }

  inline auto irq() const -> bool { return r.irq; }
  inline auto irq(bool line) -> void { r.irq = line; }

  using r8 = uint8;

  union r16 {
    inline r16() : w(0) {}
    inline r16(uint data) : w(data) {}
    inline auto& operator=(uint data) { w = data; return *this; }

    uint16 w;
    struct { uint8 order_lsb2(l, h); };
  };

  union r24 {
    inline r24() : d(0) {}
    inline r24(uint data) : d(data) {}
    inline auto& operator=(uint data) { d = data; return *this; }

    uint24 d;
    struct { uint16 order_lsb2(w, x); };
    struct {  uint8 order_lsb4(l, h, b, y); };
  };

  //wdc65816.cpp
  auto power() -> void;

  //memory.cpp
  alwaysinline auto idleIRQ() -> void;
  alwaysinline auto idle2() -> void;
  alwaysinline auto idle4(uint16 x, uint16 y) -> void;
  alwaysinline auto idle6(uint16 address) -> void;
  alwaysinline auto fetch() -> uint8;
  alwaysinline auto pull() -> uint8;
               auto push(uint8 data) -> void;
  alwaysinline auto pullN() -> uint8;
  alwaysinline auto pushN(uint8 data) -> void;
  alwaysinline auto readDirect(uint address) -> uint8;
  alwaysinline auto writeDirect(uint address, uint8 data) -> void;
  alwaysinline auto readDirectN(uint address) -> uint8;
  alwaysinline auto readBank(uint address) -> uint8;
  alwaysinline auto writeBank(uint address, uint8 data) -> void;
  alwaysinline auto readLong(uint address) -> uint8;
  alwaysinline auto writeLong(uint address, uint8 data) -> void;
  alwaysinline auto readStack(uint address) -> uint8;
  alwaysinline auto writeStack(uint address, uint8 data) -> void;

  //algorithms.cpp
  using  alu8 = auto (WDC65816::*)( uint8) ->  uint8;
  using alu16 = auto (WDC65816::*)(uint16) -> uint16;

  auto algorithmADC8(uint8) -> uint8;
  auto algorithmADC16(uint16) -> uint16;
  auto algorithmAND8(uint8) -> uint8;
  auto algorithmAND16(uint16) -> uint16;
  auto algorithmASL8(uint8) -> uint8;
  auto algorithmASL16(uint16) -> uint16;
  auto algorithmBIT8(uint8) -> uint8;
  auto algorithmBIT16(uint16) -> uint16;
  auto algorithmCMP8(uint8) -> uint8;
  auto algorithmCMP16(uint16) -> uint16;
  auto algorithmCPX8(uint8) -> uint8;
  auto algorithmCPX16(uint16) -> uint16;
  auto algorithmCPY8(uint8) -> uint8;
  auto algorithmCPY16(uint16) -> uint16;
  auto algorithmDEC8(uint8) -> uint8;
  auto algorithmDEC16(uint16) -> uint16;
  auto algorithmEOR8(uint8) -> uint8;
  auto algorithmEOR16(uint16) -> uint16;
  auto algorithmINC8(uint8) -> uint8;
  auto algorithmINC16(uint16) -> uint16;
  auto algorithmLDA8(uint8) -> uint8;
  auto algorithmLDA16(uint16) -> uint16;
  auto algorithmLDX8(uint8) -> uint8;
  auto algorithmLDX16(uint16) -> uint16;
  auto algorithmLDY8(uint8) -> uint8;
  auto algorithmLDY16(uint16) -> uint16;
  auto algorithmLSR8(uint8) -> uint8;
  auto algorithmLSR16(uint16) -> uint16;
  auto algorithmORA8(uint8) -> uint8;
  auto algorithmORA16(uint16) -> uint16;
  auto algorithmROL8(uint8) -> uint8;
  auto algorithmROL16(uint16) -> uint16;
  auto algorithmROR8(uint8) -> uint8;
  auto algorithmROR16(uint16) -> uint16;
  auto algorithmSBC8(uint8) -> uint8;
  auto algorithmSBC16(uint16) -> uint16;
  auto algorithmTRB8(uint8) -> uint8;
  auto algorithmTRB16(uint16) -> uint16;
  auto algorithmTSB8(uint8) -> uint8;
  auto algorithmTSB16(uint16) -> uint16;

  //instructions-read.cpp
  auto instructionImmediateRead8(alu8) -> void;
  auto instructionImmediateRead16(alu16) -> void;
  auto instructionBankRead8(alu8) -> void;
  auto instructionBankRead16(alu16) -> void;
  auto instructionBankRead8(alu8, r16) -> void;
  auto instructionBankRead16(alu16, r16) -> void;
  auto instructionLongRead8(alu8, r16 = {}) -> void;
  auto instructionLongRead16(alu16, r16 = {}) -> void;
  auto instructionDirectRead8(alu8) -> void;
  auto instructionDirectRead16(alu16) -> void;
  auto instructionDirectRead8(alu8, r16) -> void;
  auto instructionDirectRead16(alu16, r16) -> void;
  auto instructionIndirectRead8(alu8) -> void;
  auto instructionIndirectRead16(alu16) -> void;
  auto instructionIndexedIndirectRead8(alu8) -> void;
  auto instructionIndexedIndirectRead16(alu16) -> void;
  auto instructionIndirectIndexedRead8(alu8) -> void;
  auto instructionIndirectIndexedRead16(alu16) -> void;
  auto instructionIndirectLongRead8(alu8, r16 = {}) -> void;
  auto instructionIndirectLongRead16(alu16, r16 = {}) -> void;
  auto instructionStackRead8(alu8) -> void;
  auto instructionStackRead16(alu16) -> void;
  auto instructionIndirectStackRead8(alu8) -> void;
  auto instructionIndirectStackRead16(alu16) -> void;

  //instructions-write.cpp
  auto instructionBankWrite8(r16) -> void;
  auto instructionBankWrite16(r16) -> void;
  auto instructionBankWrite8(r16, r16) -> void;
  auto instructionBankWrite16(r16, r16) -> void;
  auto instructionLongWrite8(r16 = {}) -> void;
  auto instructionLongWrite16(r16 = {}) -> void;
  auto instructionDirectWrite8(r16) -> void;
  auto instructionDirectWrite16(r16) -> void;
  auto instructionDirectWrite8(r16, r16) -> void;
  auto instructionDirectWrite16(r16, r16) -> void;
  auto instructionIndirectWrite8() -> void;
  auto instructionIndirectWrite16() -> void;
  auto instructionIndexedIndirectWrite8() -> void;
  auto instructionIndexedIndirectWrite16() -> void;
  auto instructionIndirectIndexedWrite8() -> void;
  auto instructionIndirectIndexedWrite16() -> void;
  auto instructionIndirectLongWrite8(r16 = {}) -> void;
  auto instructionIndirectLongWrite16(r16 = {}) -> void;
  auto instructionStackWrite8() -> void;
  auto instructionStackWrite16() -> void;
  auto instructionIndirectStackWrite8() -> void;
  auto instructionIndirectStackWrite16() -> void;

  //instructions-modify.cpp
  auto instructionImpliedModify8(alu8, r16&) -> void;
  auto instructionImpliedModify16(alu16, r16&) -> void;
  auto instructionBankModify8(alu8) -> void;
  auto instructionBankModify16(alu16) -> void;
  auto instructionBankIndexedModify8(alu8) -> void;
  auto instructionBankIndexedModify16(alu16) -> void;
  auto instructionDirectModify8(alu8) -> void;
  auto instructionDirectModify16(alu16) -> void;
  auto instructionDirectIndexedModify8(alu8) -> void;
  auto instructionDirectIndexedModify16(alu16) -> void;

  //instructions-pc.cpp
  auto instructionBranch(bool = 1) -> void;
  auto instructionBranchLong() -> void;
  auto instructionJumpShort() -> void;
  auto instructionJumpLong() -> void;
  auto instructionJumpIndirect() -> void;
  auto instructionJumpIndexedIndirect() -> void;
  auto instructionJumpIndirectLong() -> void;
  auto instructionCallShort() -> void;
  auto instructionCallLong() -> void;
  auto instructionCallIndexedIndirect() -> void;
  auto instructionReturnInterrupt() -> void;
  auto instructionReturnShort() -> void;
  auto instructionReturnLong() -> void;

  //instructions-misc.cpp
  auto instructionBitImmediate8() -> void;
  auto instructionBitImmediate16() -> void;
  auto instructionNoOperation() -> void;
  auto instructionPrefix() -> void;
  auto instructionExchangeBA() -> void;
  auto instructionBlockMove8(int) -> void;
  auto instructionBlockMove16(int) -> void;
  auto instructionInterrupt(r16) -> void;
  auto instructionStop() -> void;
  auto instructionWait() -> void;
  auto instructionExchangeCE() -> void;
  auto instructionSetFlag(bool&) -> void;
  auto instructionClearFlag(bool&) -> void;
  auto instructionResetP() -> void;
  auto instructionSetP() -> void;
  auto instructionTransfer8(r16, r16&) -> void;
  auto instructionTransfer16(r16, r16&) -> void;
  auto instructionTransferCS() -> void;
  auto instructionTransferSX8() -> void;
  auto instructionTransferSX16() -> void;
  auto instructionTransferXS() -> void;
  auto instructionPush8(r16) -> void;
  auto instructionPush16(r16) -> void;
  auto instructionPushD() -> void;
  auto instructionPull8(r16&) -> void;
  auto instructionPull16(r16&) -> void;
  auto instructionPullD() -> void;
  auto instructionPullB() -> void;
  auto instructionPullP() -> void;
  auto instructionPushEffectiveAddress() -> void;
  auto instructionPushEffectiveIndirectAddress() -> void;
  auto instructionPushEffectiveRelativeAddress() -> void;

  //instruction.cpp
  auto instruction() -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  //disassembler.cpp
  auto disassemble() -> string;
  auto disassemble(uint24 addr, bool e, bool m, bool x) -> string;

  struct f8 {
    bool c = 0;  //carry
    bool z = 0;  //zero
    bool i = 0;  //interrupt disable
    bool d = 0;  //decimal mode
    bool x = 0;  //index register mode
    bool m = 0;  //accumulator mode
    bool v = 0;  //overflow
    bool n = 0;  //negative

    inline operator uint() const {
      return c << 0 | z << 1 | i << 2 | d << 3 | x << 4 | m << 5 | v << 6 | n << 7;
    }

    inline auto& operator=(uint data) {
      c = data & 0x01;
      z = data & 0x02;
      i = data & 0x04;
      d = data & 0x08;
      x = data & 0x10;
      m = data & 0x20;
      v = data & 0x40;
      n = data & 0x80;
      return *this;
    }
  };

  struct Registers {
    r24 pc;
    r16 a;
    r16 x;
    r16 y;
    r16 z;
    r16 s;
    r16 d;
     r8 b;
     f8 p;

    bool e   = 0;  //emulation mode
    bool irq = 0;  //IRQ pin (0 = low, 1 = trigger)
    bool wai = 0;  //raised during wai, cleared after interrupt triggered
    bool stp = 0;  //raised during stp, never cleared

    uint16 vector;  //interrupt vector address
    uint24 mar;     //memory address register
     r8 mdr;      //memory data register

    r24 u;  //temporary register
    r24 v;  //temporary register
    r24 w;  //temporary register
  } r;
};

}
