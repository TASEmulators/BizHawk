auto uPD96050::exec() -> void {
  uint24 opcode = programROM[regs.pc++];
  switch(opcode >> 22) {
  case 0: execOP(opcode); break;
  case 1: execRT(opcode); break;
  case 2: execJP(opcode); break;
  case 3: execLD(opcode); break;
  }

  int32 result = (int32)regs.k * regs.l;  //sign + 30-bit result
  regs.m = result >> 15;  //store sign + top 15-bits
  regs.n = result <<  1;  //store low 15-bits + zero
}

auto uPD96050::execOP(uint24 opcode) -> void {
  uint2 pselect = opcode >> 20;  //P select
  uint4 alu     = opcode >> 16;  //ALU operation mode
  uint1 asl     = opcode >> 15;  //accumulator select
  uint2 dpl     = opcode >> 13;  //DP low modify
  uint4 dphm    = opcode >>  9;  //DP high XOR modify
  uint1 rpdcr   = opcode >>  8;  //RP decrement
  uint4 src     = opcode >>  4;  //move source
  uint4 dst     = opcode >>  0;  //move destination

  uint16 idb;
  switch(src) {
  case  0: idb = regs.trb; break;
  case  1: idb = regs.a; break;
  case  2: idb = regs.b; break;
  case  3: idb = regs.tr; break;
  case  4: idb = regs.dp; break;
  case  5: idb = regs.rp; break;
  case  6: idb = dataROM[regs.rp]; break;
  case  7: idb = 0x8000 - flags.a.s1; break;  //ASL ignored; always SA1
  case  8: idb = regs.dr; regs.sr.rqm = 1; break;
  case  9: idb = regs.dr; break;
  case 10: idb = regs.sr; break;
  case 11: idb = regs.si; break;  //MSB
  case 12: idb = regs.si; break;  //LSB
  case 13: idb = regs.k; break;
  case 14: idb = regs.l; break;
  case 15: idb = dataRAM[regs.dp]; break;
  }

  if(alu) {
    uint16 p, q, r;
    Flag flag;
    boolean c;

    switch(pselect) {
    case 0: p = dataRAM[regs.dp]; break;
    case 1: p = idb; break;
    case 2: p = regs.m; break;
    case 3: p = regs.n; break;
    }

    switch(asl) {
    case 0: q = regs.a; flag = flags.a; c = flags.b.c; break;
    case 1: q = regs.b; flag = flags.b; c = flags.a.c; break;
    }

    switch(alu) {
    case  1: r = q | p; break;                //OR
    case  2: r = q & p; break;                //AND
    case  3: r = q ^ p; break;                //XOR
    case  4: r = q - p; break;                //SUB
    case  5: r = q + p; break;                //ADD
    case  6: r = q - p - c; break;            //SBB
    case  7: r = q + p + c; break;            //ADC
    case  8: r = q - 1; p = 1; break;         //DEC
    case  9: r = q + 1; p = 1; break;         //INC
    case 10: r = ~q; break;                   //CMP
    case 11: r = q >> 1 | q & 0x8000; break;  //SHR1 (ASR)
    case 12: r = q << 1 | c; break;           //SHL1 (ROL)
    case 13: r = q << 2 | 3; break;           //SHL2
    case 14: r = q << 4 | 15; break;          //SHL4
    case 15: r = q << 8 | q >> 8; break;      //XCHG
    }

    flag.z = r == 0;
    flag.s0 = r & 0x8000;
    if(!flag.ov1) flag.s1 = flag.s0;

    switch(alu) {

    case  1:    //OR
    case  2:    //AND
    case  3:    //XOR
    case 10:    //CMP
    case 13:    //SHL2
    case 14:    //SHL4
    case 15: {  //XCHG
      flag.ov0 = 0;
      flag.ov1 = 0;
      flag.c = 0;
      break;
    }

    case  4:    //SUB
    case  5:    //ADD
    case  6:    //SBB
    case  7:    //ADC
    case  8:    //DEC
    case  9: {  //INC
      if(alu & 1) {
        //addition
        flag.ov0 = (q ^ r) & ~(q ^ p) & 0x8000;
        flag.c = r < q;
      } else {
        //subtraction
        flag.ov0 = (q ^ r) &  (q ^ p) & 0x8000;
        flag.c = r > q;
      }
      flag.ov1 = flag.ov0 & flag.ov1 ? flag.s0 == flag.s1 : flag.ov0 | flag.ov1;
      break;
    }

    case 11: {  //SHR1 (ASR)
      flag.ov0 = 0;
      flag.ov1 = 0;
      flag.c = q & 1;
      break;
    }

    case 12: {  //SHL1 (ROL)
      flag.ov0 = 0;
      flag.ov1 = 0;
      flag.c = q >> 15;
      break;
    }

    }

    switch(asl) {
    case 0: regs.a = r; flags.a = flag; break;
    case 1: regs.b = r; flags.b = flag; break;
    }
  }

  execLD(idb << 6 | dst);

  if(dst != 4) {  //if LD does not write to DP
    switch(dpl) {
    case 1: regs.dp = (regs.dp & 0xf0) + (regs.dp + 1 & 0x0f); break;  //DPINC
    case 2: regs.dp = (regs.dp & 0xf0) + (regs.dp - 1 & 0x0f); break;  //DPDEC
    case 3: regs.dp = (regs.dp & 0xf0); break;  //DPCLR
    }
    regs.dp ^= dphm << 4;
  }

  if(dst != 5) {  //if LD does not write to RP
    if(rpdcr) regs.rp--;
  }
}

auto uPD96050::execRT(uint24 opcode) -> void {
  execOP(opcode);
  regs.pc = regs.stack[--regs.sp];
}

auto uPD96050::execJP(uint24 opcode) -> void {
  uint9 brch = opcode >> 13;  //branch
  uint11 na  = opcode >>  2;  //next address
  uint2 bank = opcode >>  0;  //bank address

  uint14 jp = regs.pc & 0x2000 | bank << 11 | na << 0;

  switch(brch) {
  case 0x000: regs.pc = regs.so; return;  //JMPSO

  case 0x080: if(flags.a.c == 0) regs.pc = jp; return;  //JNCA
  case 0x082: if(flags.a.c == 1) regs.pc = jp; return;  //JCA
  case 0x084: if(flags.b.c == 0) regs.pc = jp; return;  //JNCB
  case 0x086: if(flags.b.c == 1) regs.pc = jp; return;  //JCB

  case 0x088: if(flags.a.z == 0) regs.pc = jp; return;  //JNZA
  case 0x08a: if(flags.a.z == 1) regs.pc = jp; return;  //JZA
  case 0x08c: if(flags.b.z == 0) regs.pc = jp; return;  //JNZB
  case 0x08e: if(flags.b.z == 1) regs.pc = jp; return;  //JZB

  case 0x090: if(flags.a.ov0 == 0) regs.pc = jp; return;  //JNOVA0
  case 0x092: if(flags.a.ov0 == 1) regs.pc = jp; return;  //JOVA0
  case 0x094: if(flags.b.ov0 == 0) regs.pc = jp; return;  //JNOVB0
  case 0x096: if(flags.b.ov0 == 1) regs.pc = jp; return;  //JOVB0

  case 0x098: if(flags.a.ov1 == 0) regs.pc = jp; return;  //JNOVA1
  case 0x09a: if(flags.a.ov1 == 1) regs.pc = jp; return;  //JOVA1
  case 0x09c: if(flags.b.ov1 == 0) regs.pc = jp; return;  //JNOVB1
  case 0x09e: if(flags.b.ov1 == 1) regs.pc = jp; return;  //JOVB1

  case 0x0a0: if(flags.a.s0 == 0) regs.pc = jp; return;  //JNSA0
  case 0x0a2: if(flags.a.s0 == 1) regs.pc = jp; return;  //JSA0
  case 0x0a4: if(flags.b.s0 == 0) regs.pc = jp; return;  //JNSB0
  case 0x0a6: if(flags.b.s0 == 1) regs.pc = jp; return;  //JSB0

  case 0x0a8: if(flags.a.s1 == 0) regs.pc = jp; return;  //JNSA1
  case 0x0aa: if(flags.a.s1 == 1) regs.pc = jp; return;  //JSA1
  case 0x0ac: if(flags.b.s1 == 0) regs.pc = jp; return;  //JNSB1
  case 0x0ae: if(flags.b.s1 == 1) regs.pc = jp; return;  //JSB1

  case 0x0b0: if((regs.dp & 0x0f) == 0x00) regs.pc = jp; return;  //JDPL0
  case 0x0b1: if((regs.dp & 0x0f) != 0x00) regs.pc = jp; return;  //JDPLN0
  case 0x0b2: if((regs.dp & 0x0f) == 0x0f) regs.pc = jp; return;  //JDPLF
  case 0x0b3: if((regs.dp & 0x0f) != 0x0f) regs.pc = jp; return;  //JDPLNF

  //serial input/output acknowledge not emulated
  case 0x0b4: if(regs.sr.siack == 0) regs.pc = jp; return;  //JNSIAK
  case 0x0b6: if(regs.sr.siack == 1) regs.pc = jp; return;  //JSIAK
  case 0x0b8: if(regs.sr.soack == 0) regs.pc = jp; return;  //JNSOAK
  case 0x0ba: if(regs.sr.soack == 1) regs.pc = jp; return;  //JSOAK

  case 0x0bc: if(regs.sr.rqm == 0) regs.pc = jp; return;  //JNRQM
  case 0x0be: if(regs.sr.rqm == 1) regs.pc = jp; return;  //JRQM

  case 0x100: regs.pc = jp & ~0x2000; return;  //LJMP
  case 0x101: regs.pc = jp |  0x2000; return;  //HJMP

  case 0x140: regs.stack[regs.sp++] = regs.pc; regs.pc = jp & ~0x2000; return;  //LCALL
  case 0x141: regs.stack[regs.sp++] = regs.pc; regs.pc = jp |  0x2000; return;  //HCALL
  }
}

auto uPD96050::execLD(uint24 opcode) -> void {
  uint16 id = opcode >> 6;  //immediate data
  uint4 dst = opcode >> 0;  //destination

  switch(dst) {
  case  0: break;
  case  1: regs.a = id; break;
  case  2: regs.b = id; break;
  case  3: regs.tr = id; break;
  case  4: regs.dp = id; break;
  case  5: regs.rp = id; break;
  case  6: regs.dr = id; regs.sr.rqm = 1; break;
  case  7: regs.sr = regs.sr & 0x907c | id & ~0x907c; break;
  case  8: regs.so = id; break;  //LSB
  case  9: regs.so = id; break;  //MSB
  case 10: regs.k = id; break;
  case 11: regs.k = id; regs.l = dataROM[regs.rp]; break;
  case 12: regs.l = id; regs.k = dataRAM[regs.dp | 0x40]; break;
  case 13: regs.l = id; break;
  case 14: regs.trb = id; break;
  case 15: dataRAM[regs.dp] = id; break;
  }
}
