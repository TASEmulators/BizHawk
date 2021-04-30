auto HG51B::push() -> void {
  stack[7] = stack[6];
  stack[6] = stack[5];
  stack[5] = stack[4];
  stack[4] = stack[3];
  stack[3] = stack[2];
  stack[2] = stack[1];
  stack[1] = stack[0];
  stack[0] = r.pb << 8 | r.pc << 0;
}

auto HG51B::pull() -> void {
  auto pc  = stack[0];
  stack[0] = stack[1];
  stack[1] = stack[2];
  stack[2] = stack[3];
  stack[3] = stack[4];
  stack[4] = stack[5];
  stack[5] = stack[6];
  stack[6] = stack[7];
  stack[7] = 0x0000;

  r.pb = pc >> 8;
  r.pc = pc >> 0;
}

//

auto HG51B::algorithmADD(uint24 x, uint24 y) -> uint24 {
  int z = x + y;
  r.n = z & 0x800000;
  r.z = (uint24)z == 0;
  r.c = z > 0xffffff;
  r.v = ~(x ^ y) & (x ^ z) & 0x800000;
  return z;
}

auto HG51B::algorithmAND(uint24 x, uint24 y) -> uint24 {
  x = x & y;
  r.n = x & 0x800000;
  r.z = x == 0;
  return x;
}

auto HG51B::algorithmASR(uint24 a, uint5 s) -> uint24 {
  if(s > 24) s = 0;
  a = (int24)a >> s;
  r.n = a & 0x800000;
  r.z = a == 0;
  return a;
}

auto HG51B::algorithmMUL(int24 x, int24 y) -> uint48 {
  return (int48)x * (int48)y;
}

auto HG51B::algorithmOR(uint24 x, uint24 y) -> uint24 {
  x = x | y;
  r.n = x & 0x800000;
  r.z = x == 0;
  return x;
}

auto HG51B::algorithmROR(uint24 a, uint5 s) -> uint24 {
  if(s > 24) s = 0;
  a = (a >> s) | (a << 24 - s);
  r.n = a & 0x800000;
  r.z = a == 0;
  return a;
}

auto HG51B::algorithmSHL(uint24 a, uint5 s) -> uint24 {
  if(s > 24) s = 0;
  a = a << s;
  r.n = a & 0x800000;
  r.z = a == 0;
  return a;
}

auto HG51B::algorithmSHR(uint24 a, uint5 s) -> uint24 {
  if(s > 24) s = 0;
  a = a >> s;
  r.n = a & 0x800000;
  r.z = a == 0;
  return a;
}

auto HG51B::algorithmSUB(uint24 x, uint24 y) -> uint24 {
  int z = x - y;
  r.n = z & 0x800000;
  r.z = (uint24)z == 0;
  r.c = z >= 0;
  r.v = ~(x ^ y) & (x ^ z) & 0x800000;
  return z;
}

auto HG51B::algorithmSX(uint24 x) -> uint24 {
  r.n = x & 0x800000;
  r.z = x == 0;
  return x;
}

auto HG51B::algorithmXNOR(uint24 x, uint24 y) -> uint24 {
  x = ~x ^ y;
  r.n = x & 0x800000;
  r.z = x == 0;
  return x;
}

auto HG51B::algorithmXOR(uint24 x, uint24 y) -> uint24 {
  x = x ^ y;
  r.n = x & 0x800000;
  r.z = x == 0;
  return x;
}

//

auto HG51B::instructionADD(uint7 reg, uint5 shift) -> void {
  r.a = algorithmADD(r.a << shift, readRegister(reg));
}

auto HG51B::instructionADD(uint8 imm, uint5 shift) -> void {
  r.a = algorithmADD(r.a << shift, imm);
}

auto HG51B::instructionAND(uint7 reg, uint5 shift) -> void {
  r.a = algorithmAND(r.a << shift, readRegister(reg));
}

auto HG51B::instructionAND(uint8 imm, uint5 shift) -> void {
  r.a = algorithmAND(r.a << shift, imm);
}

auto HG51B::instructionASR(uint7 reg) -> void {
  r.a = algorithmASR(r.a, readRegister(reg));
}

auto HG51B::instructionASR(uint5 imm) -> void {
  r.a = algorithmASR(r.a, imm);
}

auto HG51B::instructionCLEAR() -> void {
  r.a = 0;
  r.p = 0;
  r.ram = 0;
  r.dpr = 0;
}

auto HG51B::instructionCMP(uint7 reg, uint5 shift) -> void {
  algorithmSUB(r.a << shift, readRegister(reg));
}

auto HG51B::instructionCMP(uint8 imm, uint5 shift) -> void {
  algorithmSUB(r.a << shift, imm);
}

auto HG51B::instructionCMPR(uint7 reg, uint5 shift) -> void {
  algorithmSUB(readRegister(reg), r.a << shift);
}

auto HG51B::instructionCMPR(uint8 imm, uint5 shift) -> void {
  algorithmSUB(imm, r.a << shift);
}

auto HG51B::instructionHALT() -> void {
  halt();
}

auto HG51B::instructionINC(uint24& reg) -> void {
  reg++;
}

auto HG51B::instructionJMP(uint8 data, uint1 far, const uint1& take) -> void {
  if(!take) return;
  if(far) r.pb = r.p;
  r.pc = data;
  step(2);
}

auto HG51B::instructionJSR(uint8 data, uint1 far, const uint1& take) -> void {
  if(!take) return;
  push();
  if(far) r.pb = r.p;
  r.pc = data;
  step(2);
}

auto HG51B::instructionLD(uint24& out, uint7 reg) -> void {
  out = readRegister(reg);
}

auto HG51B::instructionLD(uint15& out, uint4 reg) -> void {
  out = r.gpr[reg];
}

auto HG51B::instructionLD(uint24& out, uint8 imm) -> void {
  out = imm;
}

auto HG51B::instructionLD(uint15& out, uint8 imm) -> void {
  out = imm;
}

auto HG51B::instructionLDL(uint15& out, uint8 imm) -> void {
  out = out & 0x7f00 | imm << 0;
}

auto HG51B::instructionLDH(uint15& out, uint7 imm) -> void {
  out = out & 0x00ff | (imm & 0x7f) << 8;
}

auto HG51B::instructionMUL(uint7 reg) -> void {
  r.mul = algorithmMUL(r.a, readRegister(reg));
}

auto HG51B::instructionMUL(uint8 imm) -> void {
  r.mul = algorithmMUL(r.a, imm);
}

auto HG51B::instructionNOP() -> void {
}

auto HG51B::instructionOR(uint7 reg, uint5 shift) -> void {
  r.a = algorithmOR(r.a << shift, readRegister(reg));
}

auto HG51B::instructionOR(uint8 imm, uint5 shift) -> void {
  r.a = algorithmOR(r.a << shift, imm);
}

auto HG51B::instructionRDRAM(uint2 byte, uint24& a) -> void {
  uint12 address = a;
  if(address >= 0xc00) address -= 0x400;
  r.ram.byte(byte) = dataRAM[address];
}

auto HG51B::instructionRDRAM(uint2 byte, uint8 imm) -> void {
  uint12 address = r.dpr + imm;
  if(address >= 0xc00) address -= 0x400;
  r.ram.byte(byte) = dataRAM[address];
}

auto HG51B::instructionRDROM(uint24& reg) -> void {
  r.rom = dataROM[(uint10)reg];
}

auto HG51B::instructionRDROM(uint10 imm) -> void {
  r.rom = dataROM[imm];
}

auto HG51B::instructionROR(uint7 reg) -> void {
  r.a = algorithmROR(r.a, readRegister(reg));
}

auto HG51B::instructionROR(uint5 imm) -> void {
  r.a = algorithmROR(r.a, imm);
}

auto HG51B::instructionRTS() -> void {
  pull();
  step(2);
}

auto HG51B::instructionSKIP(uint1 take, const uint1& flag) -> void {
  if(flag != take) return;
  advance();
  step(1);
}

auto HG51B::instructionSHL(uint7 reg) -> void {
  r.a = algorithmSHL(r.a, readRegister(reg));
}

auto HG51B::instructionSHL(uint5 imm) -> void {
  r.a = algorithmSHL(r.a, imm);
}

auto HG51B::instructionSHR(uint7 reg) -> void {
  r.a = algorithmSHR(r.a, readRegister(reg));
}

auto HG51B::instructionSHR(uint5 imm) -> void {
  r.a = algorithmSHR(r.a, imm);
}

auto HG51B::instructionST(uint7 reg, uint24& in) -> void {
  writeRegister(reg, in);
}

auto HG51B::instructionSUB(uint7 reg, uint5 shift) -> void {
  r.a = algorithmSUB(r.a << shift, readRegister(reg));
}

auto HG51B::instructionSUB(uint8 imm, uint5 shift) -> void {
  r.a = algorithmSUB(r.a << shift, imm);
}

auto HG51B::instructionSUBR(uint7 reg, uint5 shift) -> void {
  r.a = algorithmSUB(readRegister(reg), r.a << shift);
}

auto HG51B::instructionSUBR(uint8 imm, uint5 shift) -> void {
  r.a = algorithmSUB(imm, r.a << shift);
}

auto HG51B::instructionSWAP(uint24& a, uint4 reg) -> void {
  swap(a, r.gpr[reg]);
}

auto HG51B::instructionSXB() -> void {
  r.a = algorithmSX((int8)r.a);
}

auto HG51B::instructionSXW() -> void {
  r.a = algorithmSX((int16)r.a);
}

auto HG51B::instructionWAIT() -> void {
  if(!io.bus.enable) return;
  return step(io.bus.pending);
}

auto HG51B::instructionWRRAM(uint2 byte, uint24& a) -> void {
  uint12 address = a;
  if(address >= 0xc00) address -= 0x400;
  dataRAM[address] = r.ram.byte(byte);
}

auto HG51B::instructionWRRAM(uint2 byte, uint8 imm) -> void {
  uint12 address = r.dpr + imm;
  if(address >= 0xc00) address -= 0x400;
  dataRAM[address] = r.ram.byte(byte);
}

auto HG51B::instructionXNOR(uint7 reg, uint5 shift) -> void {
  r.a = algorithmXNOR(r.a << shift, readRegister(reg));
}

auto HG51B::instructionXNOR(uint8 imm, uint5 shift) -> void {
  r.a = algorithmXNOR(r.a << shift, imm);
}

auto HG51B::instructionXOR(uint7 reg, uint5 shift) -> void {
  r.a = algorithmXOR(r.a << shift, readRegister(reg));
}

auto HG51B::instructionXOR(uint8 imm, uint5 shift) -> void {
  r.a = algorithmXOR(r.a << shift, imm);
}
