auto ARM7TDMI::ADD(uint32 source, uint32 modify, bool carry) -> uint32 {
  uint32 result = source + modify + carry;
  if(cpsr().t || (opcode & 1 << 20)) {
    uint32 overflow = ~(source ^ modify) & (source ^ result);
    cpsr().v = 1 << 31 & (overflow);
    cpsr().c = 1 << 31 & (overflow ^ source ^ modify ^ result);
    cpsr().z = result == 0;
    cpsr().n = result >> 31;
  }
  return result;
}

auto ARM7TDMI::ASR(uint32 source, uint8 shift) -> uint32 {
  carry = cpsr().c;
  if(shift == 0) return source;
  carry = shift > 32 ? source & 1 << 31 : source & 1 << shift - 1;
  source = shift > 31 ? (int32)source >> 31 : (int32)source >> shift;
  return source;
}

auto ARM7TDMI::BIT(uint32 result) -> uint32 {
  if(cpsr().t || (opcode & 1 << 20)) {
    cpsr().c = carry;
    cpsr().z = result == 0;
    cpsr().n = result >> 31;
  }
  return result;
}

auto ARM7TDMI::LSL(uint32 source, uint8 shift) -> uint32 {
  carry = cpsr().c;
  if(shift == 0) return source;
  carry = shift > 32 ? 0 : source & 1 << 32 - shift;
  source = shift > 31 ? 0 : source << shift;
  return source;
}

auto ARM7TDMI::LSR(uint32 source, uint8 shift) -> uint32 {
  carry = cpsr().c;
  if(shift == 0) return source;
  carry = shift > 32 ? 0 : source & 1 << shift - 1;
  source = shift > 31 ? 0 : source >> shift;
  return source;
}

auto ARM7TDMI::MUL(uint32 product, uint32 multiplicand, uint32 multiplier) -> uint32 {
  idle();
  if(multiplier >>  8 && multiplier >>  8 != 0xffffff) idle();
  if(multiplier >> 16 && multiplier >> 16 !=   0xffff) idle();
  if(multiplier >> 24 && multiplier >> 24 !=     0xff) idle();
  product += multiplicand * multiplier;
  if(cpsr().t || (opcode & 1 << 20)) {
    cpsr().z = product == 0;
    cpsr().n = product >> 31;
  }
  return product;
}

auto ARM7TDMI::ROR(uint32 source, uint8 shift) -> uint32 {
  carry = cpsr().c;
  if(shift == 0) return source;
  if(shift &= 31) source = source << 32 - shift | source >> shift;
  carry = source & 1 << 31;
  return source;
}

auto ARM7TDMI::RRX(uint32 source) -> uint32 {
  carry = source & 1;
  return cpsr().c << 31 | source >> 1;
}

auto ARM7TDMI::SUB(uint32 source, uint32 modify, bool carry) -> uint32 {
  return ADD(source, ~modify, carry);
}

auto ARM7TDMI::TST(uint4 mode) -> bool {
  switch(mode) {
  case  0: return cpsr().z == 1;                          //EQ (equal)
  case  1: return cpsr().z == 0;                          //NE (not equal)
  case  2: return cpsr().c == 1;                          //CS (carry set)
  case  3: return cpsr().c == 0;                          //CC (carry clear)
  case  4: return cpsr().n == 1;                          //MI (negative)
  case  5: return cpsr().n == 0;                          //PL (positive)
  case  6: return cpsr().v == 1;                          //VS (overflow)
  case  7: return cpsr().v == 0;                          //VC (no overflow)
  case  8: return cpsr().c == 1 && cpsr().z == 0;         //HI (unsigned higher)
  case  9: return cpsr().c == 0 || cpsr().z == 1;         //LS (unsigned lower or same)
  case 10: return cpsr().n == cpsr().v;                   //GE (signed greater than or equal)
  case 11: return cpsr().n != cpsr().v;                   //LT (signed less than)
  case 12: return cpsr().z == 0 && cpsr().n == cpsr().v;  //GT (signed greater than)
  case 13: return cpsr().z == 1 || cpsr().n != cpsr().v;  //LE (signed less than or equal)
  case 14: return true;                                   //AL (always)
  case 15: return false;                                  //NV (never)
  }
  unreachable;
}
