#pragma once

namespace Processor {

struct SPC700 {
  virtual auto idle() -> void = 0;
  virtual auto read(uint16 address) -> uint8 = 0;
  virtual auto write(uint16 address, uint8 data) -> void = 0;
  virtual auto synchronizing() const -> bool = 0;

  virtual auto readDisassembler(uint16 address) -> uint8 { return 0; }

  //spc700.cpp
  auto power() -> void;

  //memory.cpp
  inline auto fetch() -> uint8;
  inline auto load(uint8 address) -> uint8;
  inline auto store(uint8 address, uint8 data) -> void;
  inline auto pull() -> uint8;
  inline auto push(uint8 data) -> void;

  //instruction.cpp
  auto instruction() -> void;

  //algorithms.cpp
  auto algorithmADC(uint8, uint8) -> uint8;
  auto algorithmAND(uint8, uint8) -> uint8;
  auto algorithmASL(uint8) -> uint8;
  auto algorithmCMP(uint8, uint8) -> uint8;
  auto algorithmDEC(uint8) -> uint8;
  auto algorithmEOR(uint8, uint8) -> uint8;
  auto algorithmINC(uint8) -> uint8;
  auto algorithmLD (uint8, uint8) -> uint8;
  auto algorithmLSR(uint8) -> uint8;
  auto algorithmOR (uint8, uint8) -> uint8;
  auto algorithmROL(uint8) -> uint8;
  auto algorithmROR(uint8) -> uint8;
  auto algorithmSBC(uint8, uint8) -> uint8;
  auto algorithmADW(uint16, uint16) -> uint16;
  auto algorithmCPW(uint16, uint16) -> uint16;
  auto algorithmLDW(uint16, uint16) -> uint16;
  auto algorithmSBW(uint16, uint16) -> uint16;

  //instructions.cpp
  using fps = auto (SPC700::*)(uint8) -> uint8;
  using fpb = auto (SPC700::*)(uint8, uint8) -> uint8;
  using fpw = auto (SPC700::*)(uint16, uint16) -> uint16;

  auto instructionAbsoluteBitModify(uint3) -> void;
  auto instructionAbsoluteBitSet(uint3, bool) -> void;
  auto instructionAbsoluteRead(fpb, uint8&) -> void;
  auto instructionAbsoluteModify(fps) -> void;
  auto instructionAbsoluteWrite(uint8&) -> void;
  auto instructionAbsoluteIndexedRead(fpb, uint8&) -> void;
  auto instructionAbsoluteIndexedWrite(uint8&) -> void;
  auto instructionBranch(bool) -> void;
  auto instructionBranchBit(uint3, bool) -> void;
  auto instructionBranchNotDirect() -> void;
  auto instructionBranchNotDirectDecrement() -> void;
  auto instructionBranchNotDirectIndexed(uint8&) -> void;
  auto instructionBranchNotYDecrement() -> void;
  auto instructionBreak() -> void;
  auto instructionCallAbsolute() -> void;
  auto instructionCallPage() -> void;
  auto instructionCallTable(uint4) -> void;
  auto instructionComplementCarry() -> void;
  auto instructionDecimalAdjustAdd() -> void;
  auto instructionDecimalAdjustSub() -> void;
  auto instructionDirectRead(fpb, uint8&) -> void;
  auto instructionDirectModify(fps) -> void;
  auto instructionDirectWrite(uint8&) -> void;
  auto instructionDirectDirectCompare(fpb) -> void;
  auto instructionDirectDirectModify(fpb) -> void;
  auto instructionDirectDirectWrite() -> void;
  auto instructionDirectImmediateCompare(fpb) -> void;
  auto instructionDirectImmediateModify(fpb) -> void;
  auto instructionDirectImmediateWrite() -> void;
  auto instructionDirectCompareWord(fpw) -> void;
  auto instructionDirectReadWord(fpw) -> void;
  auto instructionDirectModifyWord(int) -> void;
  auto instructionDirectWriteWord() -> void;
  auto instructionDirectIndexedRead(fpb, uint8&, uint8&) -> void;
  auto instructionDirectIndexedModify(fps, uint8&) -> void;
  auto instructionDirectIndexedWrite(uint8&, uint8&) -> void;
  auto instructionDivide() -> void;
  auto instructionExchangeNibble() -> void;
  auto instructionFlagSet(bool&, bool) -> void;
  auto instructionImmediateRead(fpb, uint8&) -> void;
  auto instructionImpliedModify(fps, uint8&) -> void;
  auto instructionIndexedIndirectRead(fpb, uint8&) -> void;
  auto instructionIndexedIndirectWrite(uint8&, uint8&) -> void;
  auto instructionIndirectIndexedRead(fpb, uint8&) -> void;
  auto instructionIndirectIndexedWrite(uint8&, uint8&) -> void;
  auto instructionIndirectXRead(fpb) -> void;
  auto instructionIndirectXWrite(uint8&) -> void;
  auto instructionIndirectXIncrementRead(uint8&) -> void;
  auto instructionIndirectXIncrementWrite(uint8&) -> void;
  auto instructionIndirectXCompareIndirectY(fpb) -> void;
  auto instructionIndirectXWriteIndirectY(fpb) -> void;
  auto instructionJumpAbsolute() -> void;
  auto instructionJumpIndirectX() -> void;
  auto instructionMultiply() -> void;
  auto instructionNoOperation() -> void;
  auto instructionOverflowClear() -> void;
  auto instructionPull(uint8&) -> void;
  auto instructionPullP() -> void;
  auto instructionPush(uint8) -> void;
  auto instructionReturnInterrupt() -> void;
  auto instructionReturnSubroutine() -> void;
  auto instructionStop() -> void;
  auto instructionTestSetBitsAbsolute(bool) -> void;
  auto instructionTransfer(uint8&, uint8&) -> void;
  auto instructionWait() -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  //disassembler.cpp
  auto disassemble(uint16 address, bool p) -> string;

  struct Flags {
    bool c = 0;  //carry
    bool z = 0;  //zero
    bool i = 0;  //interrupt disable
    bool h = 0;  //half-carry
    bool b = 0;  //break
    bool p = 0;  //page
    bool v = 0;  //overflow
    bool n = 0;  //negative

    inline operator uint() const {
      return c << 0 | z << 1 | i << 2 | h << 3 | b << 4 | p << 5 | v << 6 | n << 7;
    }

    inline auto& operator=(uint8 data) {
      c = data & 0x01;
      z = data & 0x02;
      i = data & 0x04;
      h = data & 0x08;
      b = data & 0x10;
      p = data & 0x20;
      v = data & 0x40;
      n = data & 0x80;
      return *this;
    }
  };

  struct Registers {
    union Pair {
      Pair() : w(0) {}
      uint16 w;
      struct Byte { uint8 order_lsb2(l, h); } byte;
    } pc, ya;
    uint8 x = 0;
    uint8 s = 0;
    Flags p;

    bool wait = 0;
    bool stop = 0;
  } r;
};

}
