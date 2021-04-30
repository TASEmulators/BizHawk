//Sharp SM83

//the Game Boy SoC is commonly referred to as the Sharp LR35902
//SM83 is most likely the internal CPU core, based on strong datasheet similarities
//as such, this CPU core could serve as a foundation for any SM83xx SoC

#pragma once

namespace Processor {

struct SM83 {
  virtual auto stoppable() -> bool = 0;
  virtual auto stop() -> void = 0;
  virtual auto halt() -> void = 0;
  virtual auto idle() -> void = 0;
  virtual auto read(uint16 address) -> uint8 = 0;
  virtual auto write(uint16 address, uint8 data) -> void = 0;

  //lr35902.cpp
  auto power() -> void;

  //instruction.cpp
  auto interrupt(uint16 vector) -> void;
  auto instruction() -> void;
  auto instructionCB() -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  //disassembler.cpp
  virtual auto readDebugger(uint16 address) -> uint8 { return 0; }
  auto disassemble(uint16 pc) -> string;

  //memory.cpp
  auto operand() -> uint8;
  auto operands() -> uint16;
  auto load(uint16 address) -> uint16;
  auto store(uint16 address, uint16 data) -> void;
  auto pop() -> uint16;
  auto push(uint16 data) -> void;

  //algorithms.cpp
  auto ADD(uint8, uint8, bool = 0) -> uint8;
  auto AND(uint8, uint8) -> uint8;
  auto BIT(uint3, uint8) -> void;
  auto CP(uint8, uint8) -> void;
  auto DEC(uint8) -> uint8;
  auto INC(uint8) -> uint8;
  auto OR(uint8, uint8) -> uint8;
  auto RL(uint8) -> uint8;
  auto RLC(uint8) -> uint8;
  auto RR(uint8) -> uint8;
  auto RRC(uint8) -> uint8;
  auto SLA(uint8) -> uint8;
  auto SRA(uint8) -> uint8;
  auto SRL(uint8) -> uint8;
  auto SUB(uint8, uint8, bool = 0) -> uint8;
  auto SWAP(uint8) -> uint8;
  auto XOR(uint8, uint8) -> uint8;

  //instructions.cpp
  auto instructionADC_Direct_Data(uint8&) -> void;
  auto instructionADC_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionADC_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionADD_Direct_Data(uint8&) -> void;
  auto instructionADD_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionADD_Direct_Direct(uint16&, uint16&) -> void;
  auto instructionADD_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionADD_Direct_Relative(uint16&) -> void;
  auto instructionAND_Direct_Data(uint8&) -> void;
  auto instructionAND_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionAND_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionBIT_Index_Direct(uint3, uint8&) -> void;
  auto instructionBIT_Index_Indirect(uint3, uint16&) -> void;
  auto instructionCALL_Condition_Address(bool) -> void;
  auto instructionCCF() -> void;
  auto instructionCP_Direct_Data(uint8&) -> void;
  auto instructionCP_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionCP_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionCPL() -> void;
  auto instructionDAA() -> void;
  auto instructionDEC_Direct(uint8&) -> void;
  auto instructionDEC_Direct(uint16&) -> void;
  auto instructionDEC_Indirect(uint16&) -> void;
  auto instructionDI() -> void;
  auto instructionEI() -> void;
  auto instructionHALT() -> void;
  auto instructionINC_Direct(uint8&) -> void;
  auto instructionINC_Direct(uint16&) -> void;
  auto instructionINC_Indirect(uint16&) -> void;
  auto instructionJP_Condition_Address(bool) -> void;
  auto instructionJP_Direct(uint16&) -> void;
  auto instructionJR_Condition_Relative(bool) -> void;
  auto instructionLD_Address_Direct(uint8&) -> void;
  auto instructionLD_Address_Direct(uint16&) -> void;
  auto instructionLD_Direct_Address(uint8&) -> void;
  auto instructionLD_Direct_Data(uint8&) -> void;
  auto instructionLD_Direct_Data(uint16&) -> void;
  auto instructionLD_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionLD_Direct_Direct(uint16&, uint16&) -> void;
  auto instructionLD_Direct_DirectRelative(uint16&, uint16&) -> void;
  auto instructionLD_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionLD_Direct_IndirectDecrement(uint8&, uint16&) -> void;
  auto instructionLD_Direct_IndirectIncrement(uint8&, uint16&) -> void;
  auto instructionLD_Indirect_Data(uint16&) -> void;
  auto instructionLD_Indirect_Direct(uint16&, uint8&) -> void;
  auto instructionLD_IndirectDecrement_Direct(uint16&, uint8&) -> void;
  auto instructionLD_IndirectIncrement_Direct(uint16&, uint8&) -> void;
  auto instructionLDH_Address_Direct(uint8&) -> void;
  auto instructionLDH_Direct_Address(uint8&) -> void;
  auto instructionLDH_Direct_Indirect(uint8&, uint8&) -> void;
  auto instructionLDH_Indirect_Direct(uint8&, uint8&) -> void;
  auto instructionNOP() -> void;
  auto instructionOR_Direct_Data(uint8&) -> void;
  auto instructionOR_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionOR_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionPOP_Direct(uint16&) -> void;
  auto instructionPUSH_Direct(uint16&) -> void;
  auto instructionRES_Index_Direct(uint3, uint8&) -> void;
  auto instructionRES_Index_Indirect(uint3, uint16&) -> void;
  auto instructionRET() -> void;
  auto instructionRET_Condition(bool) -> void;
  auto instructionRETI() -> void;
  auto instructionRL_Direct(uint8&) -> void;
  auto instructionRL_Indirect(uint16&) -> void;
  auto instructionRLA() -> void;
  auto instructionRLC_Direct(uint8&) -> void;
  auto instructionRLC_Indirect(uint16&) -> void;
  auto instructionRLCA() -> void;
  auto instructionRR_Direct(uint8&) -> void;
  auto instructionRR_Indirect(uint16&) -> void;
  auto instructionRRA() -> void;
  auto instructionRRC_Direct(uint8&) -> void;
  auto instructionRRC_Indirect(uint16&) -> void;
  auto instructionRRCA() -> void;
  auto instructionRST_Implied(uint8) -> void;
  auto instructionSBC_Direct_Data(uint8&) -> void;
  auto instructionSBC_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionSBC_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionSCF() -> void;
  auto instructionSET_Index_Direct(uint3, uint8&) -> void;
  auto instructionSET_Index_Indirect(uint3, uint16&) -> void;
  auto instructionSLA_Direct(uint8&) -> void;
  auto instructionSLA_Indirect(uint16&) -> void;
  auto instructionSRA_Direct(uint8&) -> void;
  auto instructionSRA_Indirect(uint16&) -> void;
  auto instructionSRL_Direct(uint8&) -> void;
  auto instructionSRL_Indirect(uint16&) -> void;
  auto instructionSUB_Direct_Data(uint8&) -> void;
  auto instructionSUB_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionSUB_Direct_Indirect(uint8&, uint16&) -> void;
  auto instructionSWAP_Direct(uint8& data) -> void;
  auto instructionSWAP_Indirect(uint16& address) -> void;
  auto instructionSTOP() -> void;
  auto instructionXOR_Direct_Data(uint8&) -> void;
  auto instructionXOR_Direct_Direct(uint8&, uint8&) -> void;
  auto instructionXOR_Direct_Indirect(uint8&, uint16&) -> void;

  struct Registers {
    union Pair {
      Pair() : word(0) {}
      uint16 word;
      struct Byte { uint8 order_msb2(hi, lo); } byte;
    };

    Pair af;
    Pair bc;
    Pair de;
    Pair hl;
    Pair sp;
    Pair pc;

    uint1 ei;
    uint1 halt;
    uint1 stop;
    uint1 ime;
  } r;

  //disassembler.cpp
  auto disassembleOpcode(uint16 pc) -> string;
  auto disassembleOpcodeCB(uint16 pc) -> string;
};

}
