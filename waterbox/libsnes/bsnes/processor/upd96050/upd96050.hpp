//NEC uPD7725
//NEC uPD96050

#pragma once

namespace Processor {

struct uPD96050 {
  auto power() -> void;
  auto exec() -> void;
  auto serialize(serializer&) -> void;

  auto execOP(uint24 opcode) -> void;
  auto execRT(uint24 opcode) -> void;
  auto execJP(uint24 opcode) -> void;
  auto execLD(uint24 opcode) -> void;

  auto readSR() -> uint8;
  auto writeSR(uint8 data) -> void;

  auto readDR() -> uint8;
  auto writeDR(uint8 data) -> void;

  auto readDP(uint12 addr) -> uint8;
  auto writeDP(uint12 addr, uint8 data) -> void;

  auto disassemble(uint14 ip) -> string;

  enum class Revision : uint { uPD7725, uPD96050 } revision;
  uint24 programROM[16384];
  uint16 dataROM[2048];
  uint16 dataRAM[2048];

  struct Flag {
    inline operator uint() const {
      return ov0 << 0 | ov1 << 1 | z << 2 | c << 3 | s0 << 4 | s1 << 5;
    }

    inline auto operator=(uint16 data) -> Flag& {
      ov0 = data >> 0 & 1;
      ov1 = data >> 1 & 1;
      z   = data >> 2 & 1;
      c   = data >> 3 & 1;
      s0  = data >> 4 & 1;
      s1  = data >> 5 & 1;
      return *this;
    }

    auto serialize(serializer&) -> void;

    boolean ov0;  //overflow 0
    boolean ov1;  //overflow 1
    boolean z;    //zero
    boolean c;    //carry
    boolean s0;   //sign 0
    boolean s1;   //sign 1
  };

  struct Status {
    inline operator uint() const {
      bool _drs = drs & !drc;  //when DRC=1, DRS=0
      return p0 << 0 | p1 << 1 | ei << 7 | sic << 8 | soc << 9 | drc << 10
           | dma << 11 | _drs << 12 | usf0 << 13 | usf1 << 14 | rqm << 15;
    }

    inline auto operator=(uint16 data) -> Status& {
      p0   = data >>  0 & 1;
      p1   = data >>  1 & 1;
      ei   = data >>  7 & 1;
      sic  = data >>  8 & 1;
      soc  = data >>  9 & 1;
      drc  = data >> 10 & 1;
      dma  = data >> 11 & 1;
      drs  = data >> 12 & 1;
      usf0 = data >> 13 & 1;
      usf1 = data >> 14 & 1;
      rqm  = data >> 15 & 1;
      return *this;
    }

    auto serialize(serializer&) -> void;

    boolean p0;    //output port 0
    boolean p1;    //output port 1
    boolean ei;    //enable interrupts
    boolean sic;   //serial input control  (0 = 16-bit; 1 = 8-bit)
    boolean soc;   //serial output control (0 = 16-bit; 1 = 8-bit)
    boolean drc;   //data register size    (0 = 16-bit; 1 = 8-bit)
    boolean dma;   //data register DMA mode
    boolean drs;   //data register status  (1 = active; 0 = stopped)
    boolean usf0;  //user flag 0
    boolean usf1;  //user flag 1
    boolean rqm;   //request for master (=1 on internal access; =0 on external access)

    //internal
    boolean siack;  //serial input acknowledge
    boolean soack;  //serial output acknowledge
  };

  struct Registers {
    auto serialize(serializer&) -> void;

    uint16 stack[16];    //LIFO
    VariadicNatural pc;  //program counter
    VariadicNatural rp;  //ROM pointer
    VariadicNatural dp;  //data pointer
    uint4 sp;            //stack pointer
    uint16 si;           //serial input
    uint16 so;           //serial output
    int16 k;
    int16 l;
    int16 m;
    int16 n;
    int16 a;             //accumulator
    int16 b;             //accumulator
    uint16 tr;           //temporary register
    uint16 trb;          //temporary register
    uint16 dr;           //data register
    Status sr;           //status register
  } regs;

  struct Flags {
    Flag a;
    Flag b;
  } flags;
};

}
