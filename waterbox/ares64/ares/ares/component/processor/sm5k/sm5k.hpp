//Sharp SM5K

#pragma once

namespace ares {

struct SM5K {
  //sm5k.cpp
  auto setP1(n4 data) -> void;
  auto power() -> void;

  //timer.cpp
  auto timerStep() -> void;
  auto timerIncrement() -> void;

  //memory.cpp
  auto fetch() -> n8;

  //instruction.cpp
  auto interrupt(n2) -> void;
  auto instruction() -> void;

  //instructions.cpp
  auto instructionADC() -> void;
  auto instructionADD() -> void;
  auto instructionADX(n4 data) -> void;
  auto instructionANP() -> void;
  auto instructionATX() -> void;
  auto instructionCALL(n12 address) -> void;
  auto instructionCOMA() -> void;
  auto instructionDECB() -> void;
  auto instructionDR() -> void;
  auto instructionDTA(n8) -> void;
  auto instructionEX() -> void;
  auto instructionEXAX() -> void;
  auto instructionEXBL() -> void;
  auto instructionEXBM() -> void;
  auto instructionEXC(n2 data) -> void;
  auto instructionEXCD(n2 data) -> void;
  auto instructionEXCI(n2 data) -> void;
  auto instructionHALT() -> void;
  auto instructionID() -> void;
  auto instructionIE() -> void;
  auto instructionIN() -> void;
  auto instructionINCB() -> void;
  auto instructionINL() -> void;
  auto instructionLAX(n4 data) -> void;
  auto instructionLBLX(n4 data) -> void;
  auto instructionLBMX(n4 data) -> void;
  auto instructionLDA(n2 data) -> void;
  auto instructionORP() -> void;
  auto instructionOUT() -> void;
  auto instructionOUTL() -> void;
  auto instructionPAT(n8) -> void;
  auto instructionRC() -> void;
  auto instructionRM(n2 data) -> void;
  auto instructionRTN() -> void;
  auto instructionRTNI() -> void;
  auto instructionRTNS() -> void;
  auto instructionSC() -> void;
  auto instructionSM(n2 data) -> void;
  auto instructionSTOP() -> void;
  auto instructionTA() -> void;
  auto instructionTABL() -> void;
  auto instructionTAM() -> void;
  auto instructionTB() -> void;
  auto instructionTC() -> void;
  auto instructionTL(n12 address) -> void;
  auto instructionTM(n2 data) -> void;
  auto instructionTPB(n2 port) -> void;
  auto instructionTR(n6 address) -> void;
  auto instructionTRS(n5 address) -> void;
  auto instructionTT() -> void;

  //serialization.cpp
  auto serialize(serializer&) -> void;

  n8 ROM[4096];
  n4 RAM[256];

  n4  A;      //accumulator
  n4  X;      //auxiliary accumulator
  n8  B;      //RAM bank register
  BitRange<8,0,3> BL{&B};
  BitRange<8,4,7> BM{&B};
  n1  C;      //carry flag
  n1  IFA;    //interrupt flag A
  n1  IFB;    //interrupt flag B
  n1  IFT;    //interrupt flag T
  n1  IME;    //interrupt mask enable
  n4  P0;     //CMOS inverting output port
  n4  P1;     //input port with pull-up resistor
  n4  P2;     //I/O port with pull-up resistor
  n4  P3;     //input port with pull-up resistor
  n4  P4;     //I/O port with pull-up resistor
  n4  P5;     //I/O port with pull-up resistor
  n12 PC;     //program counter
  BitRange<12,0, 5> PL{&PC};
  BitRange<12,6,11> PU{&PC};
  n2  SP;     //stack pointer
  n12 SR[4];  //stack registers
  n4  R3;     //A/D pin selection register
  n8  R8;     //A/D conversion control and A/D data register
  n8  R9;     //A/D data register
  n8  RA;     //count register
  n8  RB;     //modulo register
  n4  RC;     //timer control
  n4  RE;     //interrupt mask flag
  n4  RF;     //P2 port direction register
  n8  SB;     //auxiliary RAM bank register
  n1  SKIP;   //skip next instruction flag
  n1  STOP;   //STOP instruction executed flag
  n1  HALT;   //HALT instruction executed flag
  n16 DIV;    //divider for timer

  //disassembler.cpp
  auto disassembleInstruction() -> string;
  auto disassembleContext() -> string;
};

}
