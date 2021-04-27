//$00 stop
auto GSU::instructionSTOP() -> void {
  if(regs.cfgr.irq == 0) {
    regs.sfr.irq = 1;
    stop();
  }
  regs.sfr.g = 0;
  regs.pipeline = 0x01;  //nop
  regs.reset();
}

//$01 nop
auto GSU::instructionNOP() -> void {
  regs.reset();
}

//$02 cache
auto GSU::instructionCACHE() -> void {
  if(regs.cbr != (regs.r[15] & 0xfff0)) {
    regs.cbr = regs.r[15] & 0xfff0;
    flushCache();
  }
  regs.reset();
}

//$03 lsr
auto GSU::instructionLSR() -> void {
  regs.sfr.cy = (regs.sr() & 1);
  regs.dr() = regs.sr() >> 1;
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$04 rol
auto GSU::instructionROL() -> void {
  bool carry = (regs.sr() & 0x8000);
  regs.dr() = (regs.sr() << 1) | regs.sfr.cy;
  regs.sfr.s  = (regs.dr() & 0x8000);
  regs.sfr.cy = carry;
  regs.sfr.z  = (regs.dr() == 0);
  regs.reset();
}

//$05 bra e
//$06 blt e
//$07 bge e
//$08 bne e
//$09 beq e
//$0a bpl e
//$0b bmi e
//$0c bcc e
//$0d bcs e
//$0e bvc e
//$0f bvs e
auto GSU::instructionBranch(bool take) -> void {
  auto displacement = (int8)pipe();
  if(take) regs.r[15] += displacement;
}

//$10-1f(b0) to rN
//$10-1f(b1) move rN
auto GSU::instructionTO_MOVE(uint n) -> void {
  if(!regs.sfr.b) {
    regs.dreg = n;
  } else {
    regs.r[n] = regs.sr();
    regs.reset();
  }
}

//$20-2f with rN
auto GSU::instructionWITH(uint n) -> void {
  regs.sreg = n;
  regs.dreg = n;
  regs.sfr.b = 1;
}

//$30-3b(alt0) stw (rN)
//$30-3b(alt1) stb (rN)
auto GSU::instructionStore(uint n) -> void {
  regs.ramaddr = regs.r[n];
  writeRAMBuffer(regs.ramaddr, regs.sr());
  if(!regs.sfr.alt1) writeRAMBuffer(regs.ramaddr ^ 1, regs.sr() >> 8);
  regs.reset();
}

//$3c loop
auto GSU::instructionLOOP() -> void {
  regs.r[12]--;
  regs.sfr.s = (regs.r[12] & 0x8000);
  regs.sfr.z = (regs.r[12] == 0);
  if(!regs.sfr.z) regs.r[15] = regs.r[13];
  regs.reset();
}

//$3d alt1
auto GSU::instructionALT1() -> void {
  regs.sfr.b = 0;
  regs.sfr.alt1 = 1;
}

//$3e alt2
auto GSU::instructionALT2() -> void {
  regs.sfr.b = 0;
  regs.sfr.alt2 = 1;
}

//$3f alt3
auto GSU::instructionALT3() -> void {
  regs.sfr.b = 0;
  regs.sfr.alt1 = 1;
  regs.sfr.alt2 = 1;
}

//$40-4b(alt0) ldw (rN)
//$40-4b(alt1) ldb (rN)
auto GSU::instructionLoad(uint n) -> void {
  regs.ramaddr = regs.r[n];
  regs.dr() = readRAMBuffer(regs.ramaddr);
  if(!regs.sfr.alt1) regs.dr() |= readRAMBuffer(regs.ramaddr ^ 1) << 8;
  regs.reset();
}

//$4c(alt0) plot
//$4c(alt1) rpix
auto GSU::instructionPLOT_RPIX() -> void {
  if(!regs.sfr.alt1) {
    plot(regs.r[1], regs.r[2]);
    regs.r[1]++;
  } else {
    regs.dr() = rpix(regs.r[1], regs.r[2]);
    regs.sfr.s = (regs.dr() & 0x8000);
    regs.sfr.z = (regs.dr() == 0);
  }
  regs.reset();
}

//$4d swap
auto GSU::instructionSWAP() -> void {
  regs.dr() = regs.sr() >> 8 | regs.sr() << 8;
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$4e(alt0) color
//$4e(alt1) cmode
auto GSU::instructionCOLOR_CMODE() -> void {
  if(!regs.sfr.alt1) {
    regs.colr = color(regs.sr());
  } else {
    regs.por = regs.sr();
  }
  regs.reset();
}

//$4f not
auto GSU::instructionNOT() -> void {
  regs.dr() = ~regs.sr();
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$50-5f(alt0) add rN
//$50-5f(alt1) adc rN
//$50-5f(alt2) add #N
//$50-5f(alt3) adc #N
auto GSU::instructionADD_ADC(uint n) -> void {
  if(!regs.sfr.alt2) n = regs.r[n];
  int r = regs.sr() + n + (regs.sfr.alt1 ? regs.sfr.cy : 0);
  regs.sfr.ov = ~(regs.sr() ^ n) & (n ^ r) & 0x8000;
  regs.sfr.s  = (r & 0x8000);
  regs.sfr.cy = (r >= 0x10000);
  regs.sfr.z  = ((uint16)r == 0);
  regs.dr() = r;
  regs.reset();
}

//$60-6f(alt0) sub rN
//$60-6f(alt1) sbc rN
//$60-6f(alt2) sub #N
//$60-6f(alt3) cmp rN
auto GSU::instructionSUB_SBC_CMP(uint n) -> void {
  if(!regs.sfr.alt2 || regs.sfr.alt1) n = regs.r[n];
  int r = regs.sr() - n - (!regs.sfr.alt2 && regs.sfr.alt1 ? !regs.sfr.cy : 0);
  regs.sfr.ov = (regs.sr() ^ n) & (regs.sr() ^ r) & 0x8000;
  regs.sfr.s  = (r & 0x8000);
  regs.sfr.cy = (r >= 0);
  regs.sfr.z  = ((uint16)r == 0);
  if(!regs.sfr.alt2 || !regs.sfr.alt1) regs.dr() = r;
  regs.reset();
}

//$70 merge
auto GSU::instructionMERGE() -> void {
  regs.dr() = (regs.r[7] & 0xff00) | (regs.r[8] >> 8);
  regs.sfr.ov = (regs.dr() & 0xc0c0);
  regs.sfr.s  = (regs.dr() & 0x8080);
  regs.sfr.cy = (regs.dr() & 0xe0e0);
  regs.sfr.z  = (regs.dr() & 0xf0f0);
  regs.reset();
}

//$71-7f(alt0) and rN
//$71-7f(alt1) bic rN
//$71-7f(alt2) and #N
//$71-7f(alt3) bic #N
auto GSU::instructionAND_BIC(uint n) -> void {
  if(!regs.sfr.alt2) n = regs.r[n];
  regs.dr() = regs.sr() & (regs.sfr.alt1 ? ~n : n);
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$80-8f(alt0) mult rN
//$80-8f(alt1) umult rN
//$80-8f(alt2) mult #N
//$80-8f(alt3) umult #N
auto GSU::instructionMULT_UMULT(uint n) -> void {
  if(!regs.sfr.alt2) n = regs.r[n];
  regs.dr() = (!regs.sfr.alt1 ? uint16((int8)regs.sr() * (int8)n) : uint16((uint8)regs.sr() * (uint8)n));
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
  if(!regs.cfgr.ms0) step(regs.clsr ? 1 : 2);
}

//$90 sbk
auto GSU::instructionSBK() -> void {
  writeRAMBuffer(regs.ramaddr ^ 0, regs.sr() >> 0);
  writeRAMBuffer(regs.ramaddr ^ 1, regs.sr() >> 8);
  regs.reset();
}

//$91-94 link #N
auto GSU::instructionLINK(uint n) -> void {
  regs.r[11] = regs.r[15] + n;
  regs.reset();
}

//$95 sex
auto GSU::instructionSEX() -> void {
  regs.dr() = (int8)regs.sr();
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$96(alt0) asr
//$96(alt1) div2
auto GSU::instructionASR_DIV2() -> void {
  regs.sfr.cy = (regs.sr() & 1);
  regs.dr() = ((int16)regs.sr() >> 1) + (regs.sfr.alt1 ? ((regs.sr() + 1) >> 16) : 0);
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$97 ror
auto GSU::instructionROR() -> void {
  bool carry = (regs.sr() & 1);
  regs.dr() = (regs.sfr.cy << 15) | (regs.sr() >> 1);
  regs.sfr.s  = (regs.dr() & 0x8000);
  regs.sfr.cy = carry;
  regs.sfr.z  = (regs.dr() == 0);
  regs.reset();
}

//$98-9d(alt0) jmp rN
//$98-9d(alt1) ljmp rN
auto GSU::instructionJMP_LJMP(uint n) -> void {
  if(!regs.sfr.alt1) {
    regs.r[15] = regs.r[n];
  } else {
    regs.pbr = regs.r[n] & 0x7f;
    regs.r[15] = regs.sr();
    regs.cbr = regs.r[15] & 0xfff0;
    flushCache();
  }
  regs.reset();
}

//$9e lob
auto GSU::instructionLOB() -> void {
  regs.dr() = regs.sr() & 0xff;
  regs.sfr.s = (regs.dr() & 0x80);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$9f(alt0) fmult
//$9f(alt1) lmult
auto GSU::instructionFMULT_LMULT() -> void {
  uint32 result = (int16)regs.sr() * (int16)regs.r[6];
  if(regs.sfr.alt1) regs.r[4] = result;
  regs.dr() = result >> 16;
  regs.sfr.s  = (regs.dr() & 0x8000);
  regs.sfr.cy = (result & 0x8000);
  regs.sfr.z  = (regs.dr() == 0);
  regs.reset();
  step((regs.cfgr.ms0 ? 3 : 7) * (regs.clsr ? 1 : 2));
}

//$a0-af(alt0) ibt rN,#pp
//$a0-af(alt1) lms rN,(yy)
//$a0-af(alt2) sms (yy),rN
auto GSU::instructionIBT_LMS_SMS(uint n) -> void {
  if(regs.sfr.alt1) {
    regs.ramaddr = pipe() << 1;
    uint8 lo  = readRAMBuffer(regs.ramaddr ^ 0) << 0;
    regs.r[n] = readRAMBuffer(regs.ramaddr ^ 1) << 8 | lo;
  } else if(regs.sfr.alt2) {
    regs.ramaddr = pipe() << 1;
    writeRAMBuffer(regs.ramaddr ^ 0, regs.r[n] >> 0);
    writeRAMBuffer(regs.ramaddr ^ 1, regs.r[n] >> 8);
  } else {
    regs.r[n] = (int8)pipe();
  }
  regs.reset();
}

//$b0-bf(b0) from rN
//$b0-bf(b1) moves rN
auto GSU::instructionFROM_MOVES(uint n) -> void {
  if(!regs.sfr.b) {
    regs.sreg = n;
  } else {
    regs.dr() = regs.r[n];
    regs.sfr.ov = (regs.dr() & 0x80);
    regs.sfr.s  = (regs.dr() & 0x8000);
    regs.sfr.z  = (regs.dr() == 0);
    regs.reset();
  }
}

//$c0 hib
auto GSU::instructionHIB() -> void {
  regs.dr() = regs.sr() >> 8;
  regs.sfr.s = (regs.dr() & 0x80);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$c1-cf(alt0) or rN
//$c1-cf(alt1) xor rN
//$c1-cf(alt2) or #N
//$c1-cf(alt3) xor #N
auto GSU::instructionOR_XOR(uint n) -> void {
  if(!regs.sfr.alt2) n = regs.r[n];
  regs.dr() = (!regs.sfr.alt1 ? (regs.sr() | n) : (regs.sr() ^ n));
  regs.sfr.s = (regs.dr() & 0x8000);
  regs.sfr.z = (regs.dr() == 0);
  regs.reset();
}

//$d0-de inc rN
auto GSU::instructionINC(uint n) -> void {
  regs.r[n]++;
  regs.sfr.s = (regs.r[n] & 0x8000);
  regs.sfr.z = (regs.r[n] == 0);
  regs.reset();
}

//$df(alt0) getc
//$df(alt2) ramb
//$df(alt3) romb
auto GSU::instructionGETC_RAMB_ROMB() -> void {
  if(!regs.sfr.alt2) {
    regs.colr = color(readROMBuffer());
  } else if(!regs.sfr.alt1) {
    syncRAMBuffer();
    regs.rambr = regs.sr() & 0x01;
  } else {
    syncROMBuffer();
    regs.rombr = regs.sr() & 0x7f;
  }
  regs.reset();
}

//$e0-ee dec rN
auto GSU::instructionDEC(uint n) -> void {
  regs.r[n]--;
  regs.sfr.s = (regs.r[n] & 0x8000);
  regs.sfr.z = (regs.r[n] == 0);
  regs.reset();
}

//$ef(alt0) getb
//$ef(alt1) getbh
//$ef(alt2) getbl
//$ef(alt3) getbs
auto GSU::instructionGETB() -> void {
  switch(regs.sfr.alt2 << 1 | regs.sfr.alt1 << 0) {
  case 0: regs.dr() = readROMBuffer(); break;
  case 1: regs.dr() = readROMBuffer() << 8 | (uint8)regs.sr(); break;
  case 2: regs.dr() = (regs.sr() & 0xff00) | readROMBuffer(); break;
  case 3: regs.dr() = (int8)readROMBuffer(); break;
  }
  regs.reset();
}

//$f0-ff(alt0) iwt rN,#xx
//$f0-ff(alt1) lm rN,(xx)
//$f0-ff(alt2) sm (xx),rN
auto GSU::instructionIWT_LM_SM(uint n) -> void {
  if(regs.sfr.alt1) {
    regs.ramaddr  = pipe() << 0;
    regs.ramaddr |= pipe() << 8;
    uint8 lo  = readRAMBuffer(regs.ramaddr ^ 0) << 0;
    regs.r[n] = readRAMBuffer(regs.ramaddr ^ 1) << 8 | lo;
  } else if(regs.sfr.alt2) {
    regs.ramaddr  = pipe() << 0;
    regs.ramaddr |= pipe() << 8;
    writeRAMBuffer(regs.ramaddr ^ 0, regs.r[n] >> 0);
    writeRAMBuffer(regs.ramaddr ^ 1, regs.r[n] >> 8);
  } else {
    uint8 lo  = pipe();
    regs.r[n] = pipe() << 8 | lo;
  }
  regs.reset();
}
