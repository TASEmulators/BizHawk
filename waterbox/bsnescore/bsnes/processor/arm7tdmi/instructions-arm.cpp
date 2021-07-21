auto ARM7TDMI::armALU(uint4 mode, uint4 d, uint4 n, uint32 rm) -> void {
  uint32 rn = r(n);

  switch(mode) {
  case  0: r(d) = BIT(rn & rm); break;  //AND
  case  1: r(d) = BIT(rn ^ rm); break;  //EOR
  case  2: r(d) = SUB(rn, rm, 1); break;  //SUB
  case  3: r(d) = SUB(rm, rn, 1); break;  //RSB
  case  4: r(d) = ADD(rn, rm, 0); break;  //ADD
  case  5: r(d) = ADD(rn, rm, cpsr().c); break;  //ADC
  case  6: r(d) = SUB(rn, rm, cpsr().c); break;  //SBC
  case  7: r(d) = SUB(rm, rn, cpsr().c); break;  //RSC
  case  8:        BIT(rn & rm); break;  //TST
  case  9:        BIT(rn ^ rm); break;  //TEQ
  case 10:        SUB(rn, rm, 1); break;  //CMP
  case 11:        ADD(rn, rm, 0); break;  //CMN
  case 12: r(d) = BIT(rn | rm); break;  //ORR
  case 13: r(d) = BIT(rm); break;  //MOV
  case 14: r(d) = BIT(rn & ~rm); break;  //BIC
  case 15: r(d) = BIT(~rm); break;  //MVN
  }

  if(exception() && d == 15 && (opcode & 1 << 20)) {
    cpsr() = spsr();
  }
}

auto ARM7TDMI::armMoveToStatus(uint4 field, uint1 mode, uint32 data) -> void {
  if(mode && (cpsr().m == PSR::USR || cpsr().m == PSR::SYS)) return;
  PSR& psr = mode ? spsr() : cpsr();

  if(field & 1) {
    if(mode || privileged()) {
      psr.m = data >> 0 & 31;
      psr.t = data >> 5 & 1;
      psr.f = data >> 6 & 1;
      psr.i = data >> 7 & 1;
      if(!mode && psr.t) r(15).data += 2;
    }
  }

  if(field & 8) {
    psr.v = data >> 28 & 1;
    psr.c = data >> 29 & 1;
    psr.z = data >> 30 & 1;
    psr.n = data >> 31 & 1;
  }
}

//

auto ARM7TDMI::armInstructionBranch
(int24 displacement, uint1 link) -> void {
  if(link) r(14) = r(15) - 4;
  r(15) = r(15) + displacement * 4;
}

auto ARM7TDMI::armInstructionBranchExchangeRegister
(uint4 m) -> void {
  uint32 address = r(m);
  cpsr().t = address & 1;
  r(15) = address;
}

auto ARM7TDMI::armInstructionDataImmediate
(uint8 immediate, uint4 shift, uint4 d, uint4 n, uint1 save, uint4 mode) -> void {
  uint32 data = immediate;
  carry = cpsr().c;
  if(shift) data = ROR(data, shift << 1);
  armALU(mode, d, n, data);
}

auto ARM7TDMI::armInstructionDataImmediateShift
(uint4 m, uint2 type, uint5 shift, uint4 d, uint4 n, uint1 save, uint4 mode) -> void {
  uint32 rm = r(m);
  carry = cpsr().c;

  switch(type) {
  case 0: rm = LSL(rm, shift); break;
  case 1: rm = LSR(rm, shift ? (uint)shift : 32); break;
  case 2: rm = ASR(rm, shift ? (uint)shift : 32); break;
  case 3: rm = shift ? ROR(rm, shift) : RRX(rm); break;
  }

  armALU(mode, d, n, rm);
}

auto ARM7TDMI::armInstructionDataRegisterShift
(uint4 m, uint2 type, uint4 s, uint4 d, uint4 n, uint1 save, uint4 mode) -> void {
  uint8 rs = r(s) + (s == 15 ? 4 : 0);
  uint32 rm = r(m) + (m == 15 ? 4 : 0);
  carry = cpsr().c;

  switch(type) {
  case 0: rm = LSL(rm, rs < 33 ? rs : (uint8)33); break;
  case 1: rm = LSR(rm, rs < 33 ? rs : (uint8)33); break;
  case 2: rm = ASR(rm, rs < 32 ? rs : (uint8)32); break;
  case 3: if(rs) rm = ROR(rm, rs & 31 ? uint(rs & 31) : 32); break;
  }

  armALU(mode, d, n, rm);
}

auto ARM7TDMI::armInstructionLoadImmediate
(uint8 immediate, uint1 half, uint4 d, uint4 n, uint1 writeback, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  uint32 rd = r(d);

  if(pre == 1) rn = up ? rn + immediate : rn - immediate;
  rd = load((half ? Half : Byte) | Nonsequential | Signed, rn);
  if(pre == 0) rn = up ? rn + immediate : rn - immediate;

  if(pre == 0 || writeback) r(n) = rn;
  r(d) = rd;
}

auto ARM7TDMI::armInstructionLoadRegister
(uint4 m, uint1 half, uint4 d, uint4 n, uint1 writeback, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  uint32 rm = r(m);
  uint32 rd = r(d);

  if(pre == 1) rn = up ? rn + rm : rn - rm;
  rd = load((half ? Half : Byte) | Nonsequential | Signed, rn);
  if(pre == 0) rn = up ? rn + rm : rn - rm;

  if(pre == 0 || writeback) r(n) = rn;
  r(d) = rd;
}

auto ARM7TDMI::armInstructionMemorySwap
(uint4 m, uint4 d, uint4 n, uint1 byte) -> void {
  uint32 word = load((byte ? Byte : Word) | Nonsequential, r(n));
  store((byte ? Byte : Word) | Nonsequential, r(n), r(m));
  r(d) = word;
}

auto ARM7TDMI::armInstructionMoveHalfImmediate
(uint8 immediate, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  uint32 rd = r(d);

  if(pre == 1) rn = up ? rn + immediate : rn - immediate;
  if(mode == 1) rd = load(Half | Nonsequential, rn);
  if(mode == 0) store(Half | Nonsequential, rn, rd);
  if(pre == 0) rn = up ? rn + immediate : rn - immediate;

  if(pre == 0 || writeback) r(n) = rn;
  if(mode == 1) r(d) = rd;
}

auto ARM7TDMI::armInstructionMoveHalfRegister
(uint4 m, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  uint32 rm = r(m);
  uint32 rd = r(d);

  if(pre == 1) rn = up ? rn + rm : rn - rm;
  if(mode == 1) rd = load(Half | Nonsequential, rn);
  if(mode == 0) store(Half | Nonsequential, rn, rd);
  if(pre == 0) rn = up ? rn + rm : rn - rm;

  if(pre == 0 || writeback) r(n) = rn;
  if(mode == 1) r(d) = rd;
}

auto ARM7TDMI::armInstructionMoveImmediateOffset
(uint12 immediate, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 byte, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  uint32 rd = r(d);

  if(pre == 1) rn = up ? rn + immediate : rn - immediate;
  if(mode == 1) rd = load((byte ? Byte : Word) | Nonsequential, rn);
  if(mode == 0) store((byte ? Byte : Word) | Nonsequential, rn, rd);
  if(pre == 0) rn = up ? rn + immediate : rn - immediate;

  if(pre == 0 || writeback) r(n) = rn;
  if(mode == 1) r(d) = rd;
}

auto ARM7TDMI::armInstructionMoveMultiple
(uint16 list, uint4 n, uint1 mode, uint1 writeback, uint1 type, uint1 up, uint1 pre) -> void {
  uint32 rn = r(n);
  if(pre == 0 && up == 1) rn = rn + 0;  //IA
  if(pre == 1 && up == 1) rn = rn + 4;  //IB
  if(pre == 1 && up == 0) rn = rn - bit::count(list) * 4 + 0;  //DB
  if(pre == 0 && up == 0) rn = rn - bit::count(list) * 4 + 4;  //DA

  if(writeback && mode == 1) {
    if(up == 1) r(n) = r(n) + bit::count(list) * 4;  //IA,IB
    if(up == 0) r(n) = r(n) - bit::count(list) * 4;  //DA,DB
  }

  auto cpsrMode = cpsr().m;
  bool usr = false;
  if(type && mode == 1 && !(list & 0x8000)) usr = true;
  if(type && mode == 0) usr = true;
  if(usr) cpsr().m = PSR::USR;

  uint sequential = Nonsequential;
  for(uint m : range(16)) {
    if(!(list & 1 << m)) continue;
    if(mode == 1) r(m) = read(Word | sequential, rn);
    if(mode == 0) write(Word | sequential, rn, r(m));
    rn += 4;
    sequential = Sequential;
  }

  if(usr) cpsr().m = cpsrMode;

  if(mode) {
    idle();
    if(type && (list & 0x8000) && cpsr().m != PSR::USR && cpsr().m != PSR::SYS) {
      cpsr() = spsr();
    }
  } else {
    pipeline.nonsequential = true;
  }

  if(writeback && mode == 0) {
    if(up == 1) r(n) = r(n) + bit::count(list) * 4;  //IA,IB
    if(up == 0) r(n) = r(n) - bit::count(list) * 4;  //DA,DB
  }
}

auto ARM7TDMI::armInstructionMoveRegisterOffset
(uint4 m, uint2 type, uint5 shift, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 byte, uint1 up, uint1 pre) -> void {
  uint32 rm = r(m);
  uint32 rd = r(d);
  uint32 rn = r(n);
  carry = cpsr().c;

  switch(type) {
  case 0: rm = LSL(rm, shift); break;
  case 1: rm = LSR(rm, shift ? (uint)shift : 32); break;
  case 2: rm = ASR(rm, shift ? (uint)shift : 32); break;
  case 3: rm = shift ? ROR(rm, shift) : RRX(rm); break;
  }

  if(pre == 1) rn = up ? rn + rm : rn - rm;
  if(mode == 1) rd = load((byte ? Byte : Word) | Nonsequential, rn);
  if(mode == 0) store((byte ? Byte : Word) | Nonsequential, rn, rd);
  if(pre == 0) rn = up ? rn + rm : rn - rm;

  if(pre == 0 || writeback) r(n) = rn;
  if(mode == 1) r(d) = rd;
}

auto ARM7TDMI::armInstructionMoveToRegisterFromStatus
(uint4 d, uint1 mode) -> void {
  if(mode && (cpsr().m == PSR::USR || cpsr().m == PSR::SYS)) return;
  r(d) = mode ? spsr() : cpsr();
}

auto ARM7TDMI::armInstructionMoveToStatusFromImmediate
(uint8 immediate, uint4 rotate, uint4 field, uint1 mode) -> void {
  uint32 data = immediate;
  if(rotate) data = ROR(data, rotate << 1);
  armMoveToStatus(field, mode, data);
}

auto ARM7TDMI::armInstructionMoveToStatusFromRegister
(uint4 m, uint4 field, uint1 mode) -> void {
  armMoveToStatus(field, mode, r(m));
}

auto ARM7TDMI::armInstructionMultiply
(uint4 m, uint4 s, uint4 n, uint4 d, uint1 save, uint1 accumulate) -> void {
  if(accumulate) idle();
  r(d) = MUL(accumulate ? r(n) : 0, r(m), r(s));
}

auto ARM7TDMI::armInstructionMultiplyLong
(uint4 m, uint4 s, uint4 l, uint4 h, uint1 save, uint1 accumulate, uint1 sign) -> void {
  uint64 rm = r(m);
  uint64 rs = r(s);

  idle();
  idle();
  if(accumulate) idle();

  if(sign) {
    if(rs >>  8 && rs >>  8 != 0xffffff) idle();
    if(rs >> 16 && rs >> 16 !=   0xffff) idle();
    if(rs >> 24 && rs >> 24 !=     0xff) idle();
    rm = (int32)rm;
    rs = (int32)rs;
  } else {
    if(rs >>  8) idle();
    if(rs >> 16) idle();
    if(rs >> 24) idle();
  }

  uint64 rd = rm * rs;
  if(accumulate) rd += (uint64)r(h) << 32 | (uint64)r(l) << 0;

  r(h) = rd >> 32;
  r(l) = rd >>  0;

  if(save) {
    cpsr().z = rd == 0;
    cpsr().n = rd >> 63 & 1;
  }
}

auto ARM7TDMI::armInstructionSoftwareInterrupt
(uint24 immediate) -> void {
  exception(PSR::SVC, 0x08);
}

auto ARM7TDMI::armInstructionUndefined
() -> void {
  exception(PSR::UND, 0x04);
}
