#ifdef ARMDSP_CPP

bool ArmDSP::condition() {
  uint4 condition = instruction >> 28;
  switch(condition) {
  case  0: return cpsr.z == 1;                      //EQ (equal)
  case  1: return cpsr.z == 0;                      //NE (not equal)
  case  2: return cpsr.c == 1;                      //CS (carry set)
  case  3: return cpsr.c == 0;                      //CC (carry clear)
  case  4: return cpsr.n == 1;                      //MI (negative)
  case  5: return cpsr.n == 0;                      //PL (positive)
  case  6: return cpsr.v == 1;                      //VS (overflow)
  case  7: return cpsr.v == 0;                      //VC (no overflow)
  case  8: return cpsr.c == 1 && cpsr.z == 0;       //HI (unsigned higher)
  case  9: return cpsr.c == 0 || cpsr.z == 1;       //LS (unsigned lower or same)
  case 10: return cpsr.n == cpsr.v;                 //GE (signed greater than or equal)
  case 11: return cpsr.n != cpsr.v;                 //LT (signed less than)
  case 12: return cpsr.z == 0 && cpsr.n == cpsr.v;  //GT (signed greater than)
  case 13: return cpsr.z == 1 || cpsr.n != cpsr.v;  //LE (signed less than or equal)
  case 14: return true;                             //AL (always)
  case 15: return false;                            //NV (never)
  }
}

//rd = target
//rn = source
//rm = modifier
//ri = original target
//ro = modified target
void ArmDSP::opcode(uint32 rm) {
  uint4 opcode = instruction >> 21;
  uint1 save = instruction >> 20;
  uint4 n = instruction >> 16;
  uint4 d = instruction >> 12;

  uint32 rn = r[n];

  //comparison opcodes always update flags (debug test)
  //this can be removed later: s=0 opcode=8-11 is invalid
  if(opcode >= 8 && opcode <= 11) assert(save == 1);

  auto test = [&](uint32 result) {
    if(save) {
      cpsr.n = result >> 31;
      cpsr.z = result == 0;
      cpsr.c = shiftercarry;
    }
    return result;
  };

  auto math = [&](uint32 source, uint32 modify, bool carry) {
    uint32 result = source + modify + carry;
    if(save) {
      uint32 overflow = ~(source ^ modify) & (source ^ result);
      cpsr.n = result >> 31;
      cpsr.z = result == 0;
      cpsr.c = (1u << 31) & (overflow ^ source ^ modify ^ result);
      cpsr.v = (1u << 31) & (overflow);
    }
    return result;
  };

  switch(opcode) {
  case  0: r[d] = test(rn & rm);         break;  //AND
  case  1: r[d] = test(rn ^ rm);         break;  //EOR
  case  2: r[d] = math(rn, ~rm, 1);      break;  //SUB
  case  3: r[d] = math(rm, ~rn, 1);      break;  //RSB
  case  4: r[d] = math(rn,  rm, 0);      break;  //ADD
  case  5: r[d] = math(rn,  rm, cpsr.c); break;  //ADC
  case  6: r[d] = math(rn, ~rm, cpsr.c); break;  //SBC
  case  7: r[d] = math(rm, ~rn, cpsr.c); break;  //RSC
  case  8:        test(rn & rm);         break;  //TST
  case  9:        test(rn ^ rm);         break;  //TEQ
  case 10:        math(rn, ~rm, 1);      break;  //CMP
  case 11:        math(rn,  rm, 0);      break;  //CMN
  case 12: r[d] = test(rn | rm);         break;  //ORR
  case 13: r[d] = test(rm);              break;  //MOV
  case 14: r[d] = test(rn &~rm);         break;  //BIC
  case 15: r[d] = test(~rm);             break;  //MVN
  }
}

//logical shift left
void ArmDSP::lsl(bool &c, uint32 &rm, uint32 rs) {
  while(rs--) {
    c = rm >> 31;
    rm <<= 1;
  }
}

//logical shift right
void ArmDSP::lsr(bool &c, uint32 &rm, uint32 rs) {
  while(rs--) {
    c = rm & 1;
    rm >>= 1;
  }
}

//arithmetic shift right
void ArmDSP::asr(bool &c, uint32 &rm, uint32 rs) {
  while(rs--) {
    c = rm & 1;
    rm = (int32)rm >> 1;
  }
}

//rotate right
void ArmDSP::ror(bool &c, uint32 &rm, uint32 rs) {
  while(rs--) {
    c = rm & 1;
    rm = (rm << 31) | (rm >> 1);
  }
}

//rotate right with extend
void ArmDSP::rrx(bool &c, uint32 &rm) {
  bool carry = c;
  c = rm & 1;
  rm = (carry << 31) | (rm >> 1);
}

//(mul,mla){condition}{s} rd,rm,rs,rn
//cccc 0000 00as dddd nnnn ssss 1001 mmmm
//c = condition
//a = accumulate
//s = save flags
//d = rd
//n = rn
//s = rs
//n = rm
void ArmDSP::op_multiply() {
  uint1 accumulate = instruction >> 21;
  uint1 save = instruction >> 20;
  uint4 d = instruction >> 16;
  uint4 n = instruction >> 12;
  uint4 s = instruction >> 8;
  uint4 m = instruction >> 0;

  //Booth's algorithm: two bit steps
  uint32 temp = r[s];
  while(temp) {
    temp >>= 2;
    tick();
  }
  r[d] = r[m] * r[s];

  if(accumulate) {
    tick();
    r[d] += r[n];
  }

  if(save) {
    cpsr.n = r[d] >> 31;
    cpsr.z = r[d] == 0;
    cpsr.c = 0;  //undefined
  }
}

//mrs{condition} rd,(c,s)psr
//cccc 0001 0r00 ++++ dddd ---- 0000 ----
//c = condition
//r = SPSR (0 = CPSR)
//d = rd
void ArmDSP::op_move_to_register_from_status_register() {
  uint1 source = instruction >> 22;
  uint4 d = instruction >> 12;

  r[d] = source ? spsr : cpsr;
}

//msr{condition} (c,s)psr:{fields},rm
//cccc 0001 0r10 ffff ++++ ---- 0000 mmmm
//c = condition
//r = SPSR (0 = CPSR)
//f = field mask
//m = rm
void ArmDSP::op_move_to_status_register_from_register() {
  uint1 source = instruction >> 22;
  uint4 field = instruction >> 16;
  uint4 m = instruction;

  PSR &psr = source ? spsr : cpsr;
  if(field & 1) psr.setc(r[m]);
  if(field & 2) psr.setx(r[m]);
  if(field & 4) psr.sets(r[m]);
  if(field & 8) psr.setf(r[m]);
}

//{opcode}{condition}{s} rd,rm {shift} #immediate
//{opcode}{condition} rn,rm {shift} #immediate
//{opcode}{condition}{s} rd,rn,rm {shift} #immediate
//cccc 000o ooos nnnn dddd llll lss0 mmmm
//c = condition
//o = opcode
//s = save flags
//n = rn
//d = rd
//l = shift immmediate
//s = shift
//m = rm
void ArmDSP::op_data_immediate_shift() {
  uint1 save = instruction >> 20;
  uint5 shift = instruction >> 7;
  uint2 mode = instruction >> 5;
  uint4 m = instruction;

  uint32 rs = shift;
  uint32 rm = r[m];
  bool c = cpsr.c;

  if(mode == 0) lsl(c, rm, rs);
  if(mode == 1) lsr(c, rm, rs ? rs : 32);
  if(mode == 2) asr(c, rm, rs ? rs : 32);
  if(mode == 3) rs ? ror(c, rm, rs) : rrx(c, rm);

  shiftercarry = c;
  opcode(rm);
}

//{opcode}{condition}{s} rd,rm {shift} rs
//{opcode}{condition} rn,rm {shift} rs
//{opcode}{condition}{s} rd,rn,rm {shift} rs
//cccc 000o ooos nnnn dddd ssss 0ss1 mmmm
//c = condition
//o = opcode
//s = save flags
//n = rn
//d = rd
//s = rs
//s = shift
//m = rm
void ArmDSP::op_data_register_shift() {
  uint1 save = instruction >> 20;
  uint4 s = instruction >> 8;
  uint2 mode = instruction >> 5;
  uint4 m = instruction >> 0;

  uint8 rs = r[s];
  uint32 rm = r[m];
  bool c = cpsr.c;

  if(mode == 0) lsl(c, rm, rs < 33 ? rs : 33);
  if(mode == 1) lsr(c, rm, rs < 33 ? rs : 33);
  if(mode == 2) asr(c, rm, rs < 32 ? rs : 32);
  if(mode == 3 && rs) ror(c, rm, rs & 31 == 0 ? 32 : rs & 31);

  shiftercarry = c;
  opcode(rm);
}

//{opcode}{condition}{s} rd,#immediate
//{opcode}{condition} rn,#immediate
//{opcode}{condition}{s} rd,rn,#immediate
//cccc 001o ooos nnnn dddd llll iiii iiii
//c = condition
//o = opcode
//s = save flags
//n = rn
//d = rd
//l = shift immediate
//i = immediate
void ArmDSP::op_data_immediate() {
  uint1 save = instruction >> 20;
  uint4 shift = instruction >> 8;
  uint8 immediate = instruction;

  uint32 rs = shift << 1;
  uint32 rm = (immediate >> rs) | (immediate << (32 - rs));
  if(rs) shiftercarry = immediate >> 31;

  opcode(rm);
}

//(ldr,str){condition}{b} rd,[rn{,+/-offset}]{!}
//(ldr,str){condition}{b} rd,[rn]{,+/-offset}
//cccc 010p ubwl nnnn dddd iiii iiii iiii
//c = condition
//p = pre (0 = post-indexed addressing)
//u = up (add/sub offset to base)
//b = byte (1 = 32-bit)
//w = writeback
//l = load (0 = save)
//n = rn
//d = rd
//i = immediate
void ArmDSP::op_move_immediate_offset() {
  uint1 p = instruction >> 24;
  uint1 u = instruction >> 23;
  uint1 b = instruction >> 22;
  uint1 w = instruction >> 21;
  uint1 l = instruction >> 20;
  uint4 n = instruction >> 16;
  uint4 d = instruction >> 12;
  uint12 rm = instruction;

  uint32 rn = r[n];
  auto &rd = r[d];

  if(p == 1) rn = u ? rn + rm : rn - rm;
  if(l) rd = b ? bus_readbyte(rn) : bus_readword(rn);
  else b ? bus_writebyte(rn, rd) : bus_writeword(rn, rd);
  if(p == 0) rn = u ? rn + rm : rn - rm;

  if(p == 0 || w == 1) r[n] = rn;
}

//(ldr)(str){condition}{b} rd,[rn,rm {mode} #immediate]{!}
//(ldr)(str){condition}{b} rd,[rn],rm {mode} #immediate
//cccc 011p ubwl nnnn dddd llll lss0 mmmm
//c = condition
//p = pre (0 = post-indexed addressing)
//u = up
//b = byte (1 = 32-bit)
//w = writeback
//l = load (0 = save)
//n = rn
//d = rd
//l = shift immediate
//s = shift mode
//m = rm
void ArmDSP::op_move_register_offset() {
  uint1 p = instruction >> 24;
  uint1 u = instruction >> 23;
  uint1 b = instruction >> 22;
  uint1 w = instruction >> 21;
  uint1 l = instruction >> 20;
  uint4 n = instruction >> 16;
  uint4 d = instruction >> 12;
  uint5 immediate = instruction >> 7;
  uint2 mode = instruction >> 5;
  uint4 m = instruction;

  uint32 rn = r[n];
  auto &rd = r[d];
  uint32 rs = immediate;
  uint32 rm = r[m];
  bool c = cpsr.c;

  if(mode == 0) lsl(c, rm, rs);
  if(mode == 1) lsr(c, rm, rs ? rs : 32);
  if(mode == 2) asr(c, rm, rs ? rs : 32);
  if(mode == 3) rs ? ror(c, rm, rs) : rrx(c, rm);

  if(p == 1) rn = u ? rn + rm : rn - rm;
  if(l) rd = b ? bus_readbyte(rn) : bus_readword(rn);
  else b ? bus_writebyte(rn, rd) : bus_writeword(rn, rd);
  if(p == 0) rn = u ? rn + rm : rn - rm;

  if(p == 0 || w == 1) r[n] = rn;
}

//(ldm,stm){condition}{mode} rn{!},{r...}
//cccc 100p uswl nnnn llll llll llll llll
//c = condition
//p = pre (0 = post-indexed addressing)
//u = up (add/sub offset to base)
//s = ???
//w = writeback
//l = load (0 = save)
//n = rn
//l = register list
void ArmDSP::op_move_multiple() {
  uint1 p = instruction >> 24;
  uint1 u = instruction >> 23;
  uint1 s = instruction >> 22;
  uint1 w = instruction >> 21;
  uint1 l = instruction >> 20;
  uint4 n = instruction >> 16;
  uint16 list = instruction;

  uint32 rn = r[n];
  if(p == 0 && u == 1) rn = rn + 0;  //IA
  if(p == 1 && u == 1) rn = rn + 4;  //IB
  if(p == 1 && u == 0) rn = rn - bit::count(list) * 4 + 0;  //DB
  if(p == 0 && u == 0) rn = rn - bit::count(list) * 4 + 4;  //DA

  for(unsigned n = 0; n < 16; n++) {
    if(list & (1 << n)) {
      if(l) r[n] = bus_readword(rn);
      else bus_writeword(rn, r[n]);
      rn += 4;
    }
  }

  if(w) {
    if(u == 1) r[n] = r[n] + bit::count(list) * 4;  //IA, IB
    if(u == 0) r[n] = r[n] - bit::count(list) * 4;  //DA, DB
  }
}

//b{l}{condition} address
//cccc 101l dddd dddd dddd dddd dddd dddd
//c = condition
//l = link
//d = displacement (24-bit signed)
void ArmDSP::op_branch() {
  uint1 l = instruction >> 24;
  int24 displacement = instruction;

  if(l) r[14] = r[15] - 4;
  r[15] += displacement * 4;
}

#endif
