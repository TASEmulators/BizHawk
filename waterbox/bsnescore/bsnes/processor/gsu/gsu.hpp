#pragma once

namespace Processor {

struct GSU {
  #include "registers.hpp"

  virtual auto step(uint clocks) -> void = 0;

  virtual auto stop() -> void = 0;
  virtual auto color(uint8 source) -> uint8 = 0;
  virtual auto plot(uint8 x, uint8 y) -> void = 0;
  virtual auto rpix(uint8 x, uint8 y) -> uint8 = 0;

  virtual auto pipe() -> uint8 = 0;
  virtual auto syncROMBuffer() -> void = 0;
  virtual auto readROMBuffer() -> uint8 = 0;
  virtual auto syncRAMBuffer() -> void = 0;
  virtual auto readRAMBuffer(uint16 addr) -> uint8 = 0;
  virtual auto writeRAMBuffer(uint16 addr, uint8 data) -> void = 0;
  virtual auto flushCache() -> void = 0;

  virtual auto read(uint addr, uint8 data = 0x00) -> uint8 = 0;
  virtual auto write(uint addr, uint8 data) -> void = 0;

  //gsu.cpp
  auto power() -> void;

  //instructions.cpp
  auto instructionADD_ADC(uint n) -> void;
  auto instructionALT1() -> void;
  auto instructionALT2() -> void;
  auto instructionALT3() -> void;
  auto instructionAND_BIC(uint n) -> void;
  auto instructionASR_DIV2() -> void;
  auto instructionBranch(bool c) -> void;
  auto instructionCACHE() -> void;
  auto instructionCOLOR_CMODE() -> void;
  auto instructionDEC(uint n) -> void;
  auto instructionFMULT_LMULT() -> void;
  auto instructionFROM_MOVES(uint n) -> void;
  auto instructionGETB() -> void;
  auto instructionGETC_RAMB_ROMB() -> void;
  auto instructionHIB() -> void;
  auto instructionIBT_LMS_SMS(uint n) -> void;
  auto instructionINC(uint n) -> void;
  auto instructionIWT_LM_SM(uint n) -> void;
  auto instructionJMP_LJMP(uint n) -> void;
  auto instructionLINK(uint n) -> void;
  auto instructionLoad(uint n) -> void;
  auto instructionLOB() -> void;
  auto instructionLOOP() -> void;
  auto instructionLSR() -> void;
  auto instructionMERGE() -> void;
  auto instructionMULT_UMULT(uint n) -> void;
  auto instructionNOP() -> void;
  auto instructionNOT() -> void;
  auto instructionOR_XOR(uint n) -> void;
  auto instructionPLOT_RPIX() -> void;
  auto instructionROL() -> void;
  auto instructionROR() -> void;
  auto instructionSBK() -> void;
  auto instructionSEX() -> void;
  auto instructionStore(uint n) -> void;
  auto instructionSTOP() -> void;
  auto instructionSUB_SBC_CMP(uint n) -> void;
  auto instructionSWAP() -> void;
  auto instructionTO_MOVE(uint n) -> void;
  auto instructionWITH(uint n) -> void;

  //switch.cpp
  auto instruction(uint8 opcode) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  //disassembler.cpp
  auto disassembleOpcode(char* output) -> void;
  auto disassembleALT0(char* output) -> void;
  auto disassembleALT1(char* output) -> void;
  auto disassembleALT2(char* output) -> void;
  auto disassembleALT3(char* output) -> void;
};

}
