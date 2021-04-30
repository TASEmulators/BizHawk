#pragma once

//Hitachi HG51B S169

namespace Processor {

struct HG51B {
  //instruction.cpp
  HG51B();

  //hg51b.cpp
  virtual auto step(uint clocks) -> void;
  virtual auto isROM(uint address) -> bool = 0;
  virtual auto isRAM(uint address) -> bool = 0;
  virtual auto read(uint address) -> uint8 = 0;
  virtual auto write(uint address, uint8 data) -> void = 0;
  virtual auto lock() -> void;
  virtual auto halt() -> void;
  auto wait(uint24 address) -> uint;
  auto main() -> void;
  auto execute() -> void;
  auto advance() -> void;
  auto suspend() -> void;
  auto cache() -> bool;
  auto dma() -> void;
  auto running() const -> bool;
  auto busy() const -> bool;

  auto power() -> void;

  //instructions.cpp
  auto push() -> void;
  auto pull() -> void;

  auto algorithmADD(uint24 x, uint24 y) -> uint24;
  auto algorithmAND(uint24 x, uint24 y) -> uint24;
  auto algorithmASR(uint24 a, uint5 s) -> uint24;
  auto algorithmMUL(int24 x, int24 y) -> uint48;
  auto algorithmOR(uint24 x, uint24 y) -> uint24;
  auto algorithmROR(uint24 a, uint5 s) -> uint24;
  auto algorithmSHL(uint24 a, uint5 s) -> uint24;
  auto algorithmSHR(uint24 a, uint5 s) -> uint24;
  auto algorithmSUB(uint24 x, uint24 y) -> uint24;
  auto algorithmSX(uint24 x) -> uint24;
  auto algorithmXNOR(uint24 x, uint24 y) -> uint24;
  auto algorithmXOR(uint24 x, uint24 y) -> uint24;

  auto instructionADD(uint7 reg, uint5 shift) -> void;
  auto instructionADD(uint8 imm, uint5 shift) -> void;
  auto instructionAND(uint7 reg, uint5 shift) -> void;
  auto instructionAND(uint8 imm, uint5 shift) -> void;
  auto instructionASR(uint7 reg) -> void;
  auto instructionASR(uint5 imm) -> void;
  auto instructionCLEAR() -> void;
  auto instructionCMP(uint7 reg, uint5 shift) -> void;
  auto instructionCMP(uint8 imm, uint5 shift) -> void;
  auto instructionCMPR(uint7 reg, uint5 shift) -> void;
  auto instructionCMPR(uint8 imm, uint5 shift) -> void;
  auto instructionHALT() -> void;
  auto instructionINC(uint24& reg) -> void;
  auto instructionJMP(uint8 data, uint1 far, const uint1& take) -> void;
  auto instructionJSR(uint8 data, uint1 far, const uint1& take) -> void;
  auto instructionLD(uint24& out, uint7 reg) -> void;
  auto instructionLD(uint15& out, uint4 reg) -> void;
  auto instructionLD(uint24& out, uint8 imm) -> void;
  auto instructionLD(uint15& out, uint8 imm) -> void;
  auto instructionLDL(uint15& out, uint8 imm) -> void;
  auto instructionLDH(uint15& out, uint7 imm) -> void;
  auto instructionMUL(uint7 reg) -> void;
  auto instructionMUL(uint8 imm) -> void;
  auto instructionNOP() -> void;
  auto instructionOR(uint7 reg, uint5 shift) -> void;
  auto instructionOR(uint8 imm, uint5 shift) -> void;
  auto instructionRDRAM(uint2 byte, uint24& a) -> void;
  auto instructionRDRAM(uint2 byte, uint8 imm) -> void;
  auto instructionRDROM(uint24& reg) -> void;
  auto instructionRDROM(uint10 imm) -> void;
  auto instructionROR(uint7 reg) -> void;
  auto instructionROR(uint5 imm) -> void;
  auto instructionRTS() -> void;
  auto instructionSHL(uint7 reg) -> void;
  auto instructionSHL(uint5 imm) -> void;
  auto instructionSHR(uint7 reg) -> void;
  auto instructionSHR(uint5 imm) -> void;
  auto instructionSKIP(uint1 take, const uint1& flag) -> void;
  auto instructionST(uint7 reg, uint24& in) -> void;
  auto instructionSUB(uint7 reg, uint5 shift) -> void;
  auto instructionSUB(uint8 imm, uint5 shift) -> void;
  auto instructionSUBR(uint7 reg, uint5 shift) -> void;
  auto instructionSUBR(uint8 imm, uint5 shift) -> void;
  auto instructionSWAP(uint24& a, uint4 reg) -> void;
  auto instructionSXB() -> void;
  auto instructionSXW() -> void;
  auto instructionWAIT() -> void;
  auto instructionWRRAM(uint2 byte, uint24& a) -> void;
  auto instructionWRRAM(uint2 byte, uint8 imm) -> void;
  auto instructionXNOR(uint7 reg, uint5 shift) -> void;
  auto instructionXNOR(uint8 imm, uint5 shift) -> void;
  auto instructionXOR(uint7 reg, uint5 shift) -> void;
  auto instructionXOR(uint8 imm, uint5 shift) -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  uint16 programRAM[2][256];  //instruction cache
  uint24 dataROM[1024];
  uint8  dataRAM[3072];

  //registers.cpp
  auto readRegister(uint7 address) -> uint24;
  auto writeRegister(uint7 address, uint24 data) -> void;

protected:
  struct Registers {
    uint15 pb;  //program bank
    uint8  pc;  //program counter

    boolean n;  //negative
    boolean z;  //zero
    boolean c;  //carry
    boolean v;  //overflow
    boolean i;  //interrupt

    uint24 a;        //accumulator
    uint15 p;        //page register
    uint48 mul;      //multiplier
    uint24 mdr;      //bus memory data register
    uint24 rom;      //data ROM data buffer
    uint24 ram;      //data RAM data buffer
    uint24 mar;      //bus memory address register
    uint24 dpr;      //data RAM address pointer
    uint24 gpr[16];  //general purpose registers
  } r;

  struct IO {
    uint1 lock;
    uint1 halt = 1;
    uint1 irq;      //0 = enable, 1 = disable
    uint1 rom = 1;  //0 = 2 ROMs, 1 = 1 ROM
    uint8 vector[32];

    struct Wait {
      uint3 rom = 3;
      uint3 ram = 3;
    } wait;

    struct Suspend {
      uint1 enable;
      uint8 duration;
    } suspend;

    struct Cache {
      uint1  enable;
      uint1  page;
      uint1  lock[2];
      uint24 address[2];  //cache address is in bytes; so 24-bit
      uint24 base;        //base address is also in bytes
      uint15 pb;
      uint8  pc;
    } cache;

    struct DMA {
      uint1  enable;
      uint24 source;
      uint24 target;
      uint16 length;
    } dma;

    struct Bus {
      uint1  enable;
      uint1  reading;
      uint1  writing;
      uint4  pending;
      uint24 address;
    } bus;
  } io;

  uint23 stack[8];
  function<void ()> instructionTable[65536];
};

}
