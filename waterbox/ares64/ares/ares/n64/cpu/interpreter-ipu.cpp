#define PC ipu.pc
#define RA ipu.r[31]
#define LO ipu.lo
#define HI ipu.hi

auto CPU::ADD(r64& rd, cr64& rs, cr64& rt) -> void {
  if(~(rs.u32 ^ rt.u32) & (rs.u32 ^ rs.u32 + rt.u32) & 1 << 31) return exception.arithmeticOverflow();
  rd.u64 = s32(rs.u32 + rt.u32);
}

auto CPU::ADDI(r64& rt, cr64& rs, s16 imm) -> void {
  if(~(rs.u32 ^ imm) & (rs.u32 ^ rs.u32 + imm) & 1 << 31) return exception.arithmeticOverflow();
  rt.u64 = s32(rs.s32 + imm);
}

auto CPU::ADDIU(r64& rt, cr64& rs, s16 imm) -> void {
  rt.u64 = s32(rs.s32 + imm);
}

auto CPU::ADDU(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = s32(rs.u32 + rt.u32);
}

auto CPU::AND(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = rs.u64 & rt.u64;
}

auto CPU::ANDI(r64& rt, cr64& rs, u16 imm) -> void {
  rt.u64 = rs.u64 & imm;
}

auto CPU::BEQ(cr64& rs, cr64& rt, s16 imm) -> void {
  if(rs.u64 == rt.u64) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BEQL(cr64& rs, cr64& rt, s16 imm) -> void {
  if(rs.u64 == rt.u64) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BGEZ(cr64& rs, s16 imm) -> void {
  if(rs.s64 >= 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BGEZAL(cr64& rs, s16 imm) -> void {
  RA.u64 = s32(PC + 8);
  if(rs.s64 >= 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BGEZALL(cr64& rs, s16 imm) -> void {
  RA.u64 = s32(PC + 8);
  if(rs.s64 >= 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BGEZL(cr64& rs, s16 imm) -> void {
  if(rs.s64 >= 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BGTZ(cr64& rs, s16 imm) -> void {
  if(rs.s64 > 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BGTZL(cr64& rs, s16 imm) -> void {
  if(rs.s64 > 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BLEZ(cr64& rs, s16 imm) -> void {
  if(rs.s64 <= 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BLEZL(cr64& rs, s16 imm) -> void {
  if(rs.s64 <= 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BLTZ(cr64& rs, s16 imm) -> void {
  if(rs.s64 < 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BLTZAL(cr64& rs, s16 imm) -> void {
  RA.u64 = s32(PC + 8);
  if(rs.s64 < 0) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BLTZALL(cr64& rs, s16 imm) -> void {
  RA.u64 = s32(PC + 8);
  if(rs.s64 < 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BLTZL(cr64& rs, s16 imm) -> void {
  if(rs.s64 < 0) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BNE(cr64& rs, cr64& rt, s16 imm) -> void {
  if(rs.u64 != rt.u64) branch.take(PC + 4 + (imm << 2));
}

auto CPU::BNEL(cr64& rs, cr64& rt, s16 imm) -> void {
  if(rs.u64 != rt.u64) branch.take(PC + 4 + (imm << 2));
  else branch.discard();
}

auto CPU::BREAK() -> void {
  exception.breakpoint();
}

auto CPU::CACHE(u8 operation, cr64& rs, s16 imm) -> void {
  u32 address = rs.u64 + imm;

  switch(operation) {

  case 0x00: {  //icache index invalidate
    auto& line = icache.line(address);
    line.valid = 0;
    break;
  }

  case 0x04: {  //icache load tag
    auto& line = icache.line(address);
    scc.tagLo.primaryCacheState = line.valid << 1;
    scc.tagLo.physicalAddress   = line.tag;
    break;
  }

  case 0x08: {  //icache store tag
    auto& line = icache.line(address);
    line.valid = scc.tagLo.primaryCacheState.bit(1);
    line.tag   = scc.tagLo.physicalAddress;
    if(scc.tagLo.primaryCacheState == 0b01) debug(unusual, "[CPU] CACHE CPCS=1");
    if(scc.tagLo.primaryCacheState == 0b11) debug(unusual, "[CPU] CACHE CPCS=3");
    break;
  }

  case 0x10: {  //icache hit invalidate
    auto& line = icache.line(address);
    if(line.hit(address)) line.valid = 0;
    break;
  }

  case 0x14: {  //icache fill
    auto& line = icache.line(address);
    line.fill(address);
    break;
  }

  case 0x18: {  //icache hit write back
    auto& line = icache.line(address);
    if(line.hit(address)) line.writeBack();
    break;
  }

  case 0x01: {  //dcache index write back invalidate
    auto& line = dcache.line(address);
    if(line.valid && line.dirty) line.writeBack();
    line.valid = 0;
    break;
  }

  case 0x05: {  //dcache index load tag
    auto& line = dcache.line(address);
    scc.tagLo.primaryCacheState = line.valid << 1 | line.dirty << 0;
    scc.tagLo.physicalAddress   = line.tag;
    break;
  }

  case 0x09: {  //dcache index store tag
    auto& line = dcache.line(address);
    line.valid = scc.tagLo.primaryCacheState.bit(1);
    line.dirty = scc.tagLo.primaryCacheState.bit(0);
    line.tag   = scc.tagLo.physicalAddress;
    if(scc.tagLo.primaryCacheState == 0b01) debug(unusual, "[CPU] CACHE DPCS=1");
    if(scc.tagLo.primaryCacheState == 0b10) debug(unusual, "[CPU] CACHE DPCS=2");
    break;
  }

  case 0x0d: {  //dcache create dirty exclusive
    auto& line = dcache.line(address);
    if(!line.hit(address) && line.dirty) line.writeBack();
    line.tag   = address & ~0xfff;
    line.valid = 1;
    line.dirty = 1;
    break;
  }

  case 0x11: {  //dcache hit invalidate
    auto& line = dcache.line(address);
    if(line.hit(address)) {
      line.valid = 0;
      line.dirty = 0;
    }
    break;
  }

  case 0x15: {  //dcache hit write back invalidate
    auto& line = dcache.line(address);
    if(line.hit(address)) {
      if(line.dirty) line.writeBack();
      line.valid = 0;
    }
    break;
  }

  case 0x19: {  //dcache hit write back
    auto& line = dcache.line(address);
    if(line.hit(address)) {
      if(line.dirty) line.writeBack();
    }
    break;
  }

  }
}

auto CPU::DADD(r64& rd, cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(~(rs.u64 ^ rt.u64) & (rs.u64 ^ rs.u64 + rt.u64) & 1ull << 63) return exception.arithmeticOverflow();
  rd.u64 = rs.u64 + rt.u64;
}

auto CPU::DADDI(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(~(rs.u64 ^ imm) & (rs.u64 ^ rs.u64 + imm) & 1ull << 63) return exception.arithmeticOverflow();
  rt.u64 = rs.u64 + imm;
}

auto CPU::DADDIU(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rt.u64 = rs.u64 + imm;
}

auto CPU::DADDU(r64& rd, cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rs.u64 + rt.u64;
}

auto CPU::DDIV(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(rt.s64) {
    //cast to i128 to prevent exception on INT64_MIN / -1
    LO.u64 = s128(rs.s64) / s128(rt.s64);
    HI.u64 = s128(rs.s64) % s128(rt.s64);
  } else {
    LO.u64 = rs.s64 < 0 ? +1 : -1;
    HI.u64 = rs.s64;
  }
  step(69);
}

auto CPU::DDIVU(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(rt.u64) {
    LO.u64 = rs.u64 / rt.u64;
    HI.u64 = rs.u64 % rt.u64;
  } else {
    LO.u64 = -1;
    HI.u64 = rs.u64;
  }
  step(69);
}

auto CPU::DIV(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(rt.s32) {
    //cast to s64 to prevent exception on INT32_MIN / -1
    LO.u64 = s32(s64(rs.s32) / s64(rt.s32));
    HI.u64 = s32(s64(rs.s32) % s64(rt.s32));
  } else {
    LO.u64 = rs.s32 < 0 ? +1 : -1;
    HI.u64 = rs.s32;
  }
  step(37);
}

auto CPU::DIVU(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(rt.u32) {
    LO.u64 = s32(rs.u32 / rt.u32);
    HI.u64 = s32(rs.u32 % rt.u32);
  } else {
    LO.u64 = -1;
    HI.u64 = rs.s32;
  }
  step(37);
}

auto CPU::DMULT(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u128 result = rs.s128() * rt.s128();
  LO.u64 = result >>  0;
  HI.u64 = result >> 64;
  step(8);
}

auto CPU::DMULTU(cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u128 result = rs.u128() * rt.u128();
  LO.u64 = result >>  0;
  HI.u64 = result >> 64;
  step(8);
}

auto CPU::DSLL(r64& rd, cr64& rt, u8 sa) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.u64 << sa;
}

auto CPU::DSLLV(r64& rd, cr64& rt, cr64& rs) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.u64 << (rs.u32 & 63);
}

auto CPU::DSRA(r64& rd, cr64& rt, u8 sa) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.s64 >> sa;
}

auto CPU::DSRAV(r64& rd, cr64& rt, cr64& rs) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.s64 >> (rs.u32 & 63);
}

auto CPU::DSRL(r64& rd, cr64& rt, u8 sa) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.u64 >> sa;
}

auto CPU::DSRLV(r64& rd, cr64& rt, cr64& rs) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rt.u64 >> (rs.u32 & 63);
}

auto CPU::DSUB(r64& rd, cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if((rs.u64 ^ rt.u64) & (rs.u64 ^ rs.u64 - rt.u64) & 1ull << 63) return exception.arithmeticOverflow();
  rd.u64 = rs.u64 - rt.u64;
}

auto CPU::DSUBU(r64& rd, cr64& rs, cr64& rt) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  rd.u64 = rs.u64 - rt.u64;
}

auto CPU::J(u32 imm) -> void {
  branch.take((PC + 4 & 0xf000'0000) | (imm << 2));
}

auto CPU::JAL(u32 imm) -> void {
  RA.u64 = s32(PC + 8);
  branch.take((PC + 4 & 0xf000'0000) | (imm << 2));
}

auto CPU::JALR(r64& rd, cr64& rs) -> void {
  rd.u64 = s32(PC + 8);
  branch.take(rs.u32);
}

auto CPU::JR(cr64& rs) -> void {
  branch.take(rs.u32);
}

auto CPU::LB(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Byte>(rs.u32 + imm)) rt.u64 = s8(*data);
}

auto CPU::LBU(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Byte>(rs.u32 + imm)) rt.u64 = u8(*data);
}

auto CPU::LD(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(auto data = read<Dual>(rs.u32 + imm)) rt.u64 = *data;
}

auto CPU::LDL(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u64 address = rs.u64 + imm;
  u64 data = rt.u64;

  if(context.littleEndian())
  switch(address & 7) {
  case 0:
    data &= 0x00ffffffffffffffull;
    if(auto byte = read<Byte>(address & ~7 | 7)) data |= byte() << 56; else return;
    break;
  case 1:
    data &= 0x0000ffffffffffffull;
    if(auto half = read<Half>(address & ~7 | 6)) data |= half() << 48; else return;
    break;
  case 2:
    data &= 0x000000ffffffffffull;
    if(auto byte = read<Byte>(address & ~7 | 5)) data |= byte() << 56; else return;
    if(auto half = read<Half>(address & ~7 | 6)) data |= half() << 40; else return;
    break;
  case 3:
    data &= 0x00000000ffffffffull;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 32; else return;
    break;
  case 4:
    data &= 0x0000000000ffffffull;
    if(auto byte = read<Byte>(address & ~7 | 3)) data |= byte() << 56; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 24; else return;
    break;
  case 5:
    data &= 0x000000000000ffffull;
    if(auto half = read<Half>(address & ~7 | 2)) data |= half() << 48; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 16; else return;
    break;
  case 6:
    data &= 0x00000000000000ffull;
    if(auto byte = read<Byte>(address & ~7 | 1)) data |= byte() << 56; else return;
    if(auto half = read<Half>(address & ~7 | 2)) data |= half() << 40; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() <<  8; else return;
    break;
  case 7:
    data &= 0x0000000000000000ull;
    if(auto dual = read<Dual>(address & ~7 | 0)) data |= dual() <<  0; else return;
    break;
  }

  if(context.bigEndian())
  switch(address & 7) {
  case 0:
    data &= 0x0000000000000000ull;
    if(auto dual = read<Dual>(address & ~7 | 0)) data |= dual() <<  0; else return;
    break;
  case 1:
    data &= 0x00000000000000ffull;
    if(auto byte = read<Byte>(address & ~7 | 1)) data |= byte() << 56; else return;
    if(auto half = read<Half>(address & ~7 | 2)) data |= half() << 40; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() <<  8; else return;
    break;
  case 2:
    data &= 0x000000000000ffffull;
    if(auto half = read<Half>(address & ~7 | 2)) data |= half() << 48; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 16; else return;
    break;
  case 3:
    data &= 0x0000000000ffffffull;
    if(auto byte = read<Byte>(address & ~7 | 3)) data |= byte() << 56; else return;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 24; else return;
    break;
  case 4:
    data &= 0x00000000ffffffffull;
    if(auto word = read<Word>(address & ~7 | 4)) data |= word() << 32; else return;
    break;
  case 5:
    data &= 0x000000ffffffffffull;
    if(auto byte = read<Byte>(address & ~7 | 5)) data |= byte() << 56; else return;
    if(auto half = read<Half>(address & ~7 | 6)) data |= half() << 40; else return;
    break;
  case 6:
    data &= 0x0000ffffffffffffull;
    if(auto half = read<Half>(address & ~7 | 6)) data |= half() << 48; else return;
    break;
  case 7:
    data &= 0x00ffffffffffffffull;
    if(auto byte = read<Byte>(address & ~7 | 7)) data |= byte() << 56; else return;
    break;
  }

  rt.u64 = data;
}

auto CPU::LDR(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u64 address = rs.u64 + imm;
  u64 data = rt.u64;

  if(context.littleEndian())
  switch(address & 7) {
  case 0:
    data &= 0x0000000000000000ull;
    if(auto dual = read<Dual>(address & ~7 | 0)) data |= dual() <<  0; else return;
    break;
  case 1:
    data &= 0xff00000000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() << 24; else return;
    if(auto half = read<Half>(address & ~7 | 4)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 6)) data |= byte() <<  0; else return;
    break;
  case 2:
    data &= 0xffff000000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() << 16; else return;
    if(auto half = read<Half>(address & ~7 | 4)) data |= half() <<  0; else return;
    break;
  case 3:
    data &= 0xffffff0000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 4)) data |= byte() <<  0; else return;
    break;
  case 4:
    data &= 0xffffffff00000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() <<  0; else return;
    break;
  case 5:
    data &= 0xffffffffff000000ull;
    if(auto half = read<Half>(address & ~7 | 0)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 2)) data |= byte() <<  0; else return;
    break;
  case 6:
    data &= 0xffffffffffff0000ull;
    if(auto half = read<Half>(address & ~7 | 0)) data |= half() <<  0; else return;
    break;
  case 7:
    data &= 0xffffffffffffff00ull;
    if(auto byte = read<Byte>(address & ~7 | 0)) data |= byte() <<  0; else return;
    break;
  }

  if(context.bigEndian())
  switch(address & 7) {
  case 0:
    data &= 0xffffffffffffff00ull;
    if(auto byte = read<Byte>(address & ~7 | 0)) data |= byte() <<  0; else return;
    break;
  case 1:
    data &= 0xffffffffffff0000ull;
    if(auto half = read<Half>(address & ~7 | 0)) data |= half() <<  0; else return;
    break;
  case 2:
    data &= 0xffffffffff000000ull;
    if(auto half = read<Half>(address & ~7 | 0)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 2)) data |= byte() <<  0; else return;
    break;
  case 3:
    data &= 0xffffffff00000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() <<  0; else return;
    break;
  case 4:
    data &= 0xffffff0000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 4)) data |= byte() <<  0; else return;
    break;
  case 5:
    data &= 0xffff000000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() << 16; else return;
    if(auto half = read<Half>(address & ~7 | 4)) data |= half() <<  0; else return;
    break;
  case 6:
    data &= 0xff00000000000000ull;
    if(auto word = read<Word>(address & ~7 | 0)) data |= word() << 24; else return;
    if(auto half = read<Half>(address & ~7 | 4)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~7 | 6)) data |= byte() <<  0; else return;
    break;
  case 7:
    data &= 0x0000000000000000ull;
    if(auto dual = read<Dual>(address & ~7 | 0)) data |= dual() <<  0; else return;
    break;
  }

  rt.u64 = data;
}

auto CPU::LH(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Half>(rs.u32 + imm)) rt.u64 = s16(*data);
}

auto CPU::LHU(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Half>(rs.u32 + imm)) rt.u64 = u16(*data);
}

auto CPU::LL(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Word>(rs.u32 + imm)) {
    rt.u64 = s32(*data);
    scc.ll = tlb.physicalAddress >> 4;
    scc.llbit = 1;
  }
}

auto CPU::LLD(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(auto data = read<Dual>(rs.u32 + imm)) {
    rt.u64 = *data;
    scc.ll = tlb.physicalAddress >> 4;
    scc.llbit = 1;
  }
}

auto CPU::LUI(r64& rt, u16 imm) -> void {
  rt.u64 = s32(imm << 16);
}

auto CPU::LW(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Word>(rs.u32 + imm)) rt.u64 = s32(*data);
}

auto CPU::LWL(r64& rt, cr64& rs, s16 imm) -> void {
  u64 address = rs.u64 + imm;
  u32 data = rt.u32;

  if(context.littleEndian())
  switch(address & 3) {
  case 0:
    data &= 0x00ffffff;
    if(auto byte = read<Byte>(address & ~3 | 3)) data |= byte() << 24; else return;
    break;
  case 1:
    data &= 0x0000ffff;
    if(auto half = read<Half>(address & ~3 | 2)) data |= half() << 16; else return;
    break;
  case 2:
    data &= 0x000000ff;
    if(auto byte = read<Byte>(address & ~3 | 1)) data |= byte() << 24; else return;
    if(auto half = read<Half>(address & ~3 | 2)) data |= half() <<  8; else return;
    break;
  case 3:
    data &= 0x00000000;
    if(auto word = read<Word>(address & ~3 | 0)) data |= word() <<  0; else return;
    break;
  }

  if(context.bigEndian())
  switch(address & 3) {
  case 0:
    data &= 0x00000000;
    if(auto word = read<Word>(address & ~3 | 0)) data |= word() <<  0; else return;
    break;
  case 1:
    data &= 0x000000ff;
    if(auto byte = read<Byte>(address & ~3 | 1)) data |= byte() << 24; else return;
    if(auto half = read<Half>(address & ~3 | 2)) data |= half() <<  8; else return;
    break;
  case 2:
    data &= 0x0000ffff;
    if(auto half = read<Half>(address & ~3 | 2)) data |= half() << 16; else return;
    break;
  case 3:
    data &= 0x00ffffff;
    if(auto byte = read<Byte>(address & ~3 | 3)) data |= byte() << 24; else return;
    break;
  }

  rt.s64 = (s32)data;
}

auto CPU::LWR(r64& rt, cr64& rs, s16 imm) -> void {
  u64 address = rs.u64 + imm;
  u32 data = rt.u32;

  if(context.littleEndian())
  switch(address & 3) {
  case 0:
    data &= 0x00000000;
    if(auto word = read<Word>(address & ~3 | 0)) data |= word() <<  0; else return;
    rt.s64 = (s32)data;
    break;
  case 1:
    data &= 0xff000000;
    if(auto half = read<Half>(address & ~3 | 0)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~3 | 2)) data |= byte() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  case 2:
    data &= 0xffff0000;
    if(auto half = read<Half>(address & ~3 | 0)) data |= half() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  case 3:
    data &= 0xffffff00;
    if(auto byte = read<Byte>(address & ~3 | 0)) data |= byte() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  }

  if(context.bigEndian())
  switch(address & 3) {
  case 0:
    data &= 0xffffff00;
    if(auto byte = read<Byte>(address & ~3 | 0)) data |= byte() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  case 1:
    data &= 0xffff0000;
    if(auto half = read<Half>(address & ~3 | 0)) data |= half() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  case 2:
    data &= 0xff000000;
    if(auto half = read<Half>(address & ~3 | 0)) data |= half() <<  8; else return;
    if(auto byte = read<Byte>(address & ~3 | 2)) data |= byte() <<  0; else return;
    if(context.bits == 32) rt.u32 = data;
    if(context.bits == 64) rt.s64 = (s32)data;
    break;
  case 3:
    data &= 0x00000000;
    if(auto word = read<Word>(address & ~3 | 0)) data |= word() <<  0; else return;
    rt.s64 = (s32)data;
    break;
  }
}

auto CPU::LWU(r64& rt, cr64& rs, s16 imm) -> void {
  if(auto data = read<Word>(rs.u32 + imm)) rt.u64 = u32(*data);
}

auto CPU::MFHI(r64& rd) -> void {
  rd.u64 = HI.u64;
}

auto CPU::MFLO(r64& rd) -> void {
  rd.u64 = LO.u64;
}

auto CPU::MTHI(cr64& rs) -> void {
  HI.u64 = rs.u64;
}

auto CPU::MTLO(cr64& rs) -> void {
  LO.u64 = rs.u64;
}

auto CPU::MULT(cr64& rs, cr64& rt) -> void {
  u64 result = s64(rs.s32) * s64(rt.s32);
  LO.u64 = s32(result >>  0);
  HI.u64 = s32(result >> 32);
  step(5);
}

auto CPU::MULTU(cr64& rs, cr64& rt) -> void {
  u64 result = u64(rs.u32) * u64(rt.u32);
  LO.u64 = s32(result >>  0);
  HI.u64 = s32(result >> 32);
  step(5);
}

auto CPU::NOR(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = ~(rs.u64 | rt.u64);
}

auto CPU::OR(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = rs.u64 | rt.u64;
}

auto CPU::ORI(r64& rt, cr64& rs, u16 imm) -> void {
  rt.u64 = rs.u64 | imm;
}

auto CPU::SB(cr64& rt, cr64& rs, s16 imm) -> void {
  write<Byte>(rs.u32 + imm, rt.u32);
}

auto CPU::SC(r64& rt, cr64& rs, s16 imm) -> void {
  if(scc.llbit) {
    scc.llbit = 0;
    rt.u64 = write<Word>(rs.u32 + imm, rt.u32);
  } else {
    rt.u64 = 0;
  }
}

auto CPU::SCD(r64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  if(scc.llbit) {
    scc.llbit = 0;
    rt.u64 = write<Dual>(rs.u32 + imm, rt.u64);
  } else {
    rt.u64 = 0;
  }
}

auto CPU::SD(cr64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  write<Dual>(rs.u32 + imm, rt.u64);
}

auto CPU::SDL(cr64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u64 address = rs.u64 + imm;
  u64 data = rt.u64;

  if(context.littleEndian())
  switch(address & 7) {
  case 0:
    if(!write<Byte>(address & ~7 | 7, data >> 56)) return;
    break;
  case 1:
    if(!write<Half>(address & ~7 | 6, data >> 48)) return;
    break;
  case 2:
    if(!write<Byte>(address & ~7 | 5, data >> 56)) return;
    if(!write<Half>(address & ~7 | 6, data >> 40)) return;
    break;
  case 3:
    if(!write<Word>(address & ~7 | 4, data >> 32)) return;
    break;
  case 4:
    if(!write<Byte>(address & ~7 | 3, data >> 56)) return;
    if(!write<Word>(address & ~7 | 4, data >> 24)) return;
    break;
  case 5:
    if(!write<Half>(address & ~7 | 2, data >> 48)) return;
    if(!write<Word>(address & ~7 | 4, data >> 16)) return;
    break;
  case 6:
    if(!write<Byte>(address & ~7 | 1, data >> 56)) return;
    if(!write<Half>(address & ~7 | 2, data >> 40)) return;
    if(!write<Word>(address & ~7 | 4, data >>  8)) return;
    break;
  case 7:
    if(!write<Dual>(address & ~7 | 0, data >>  0)) return;
    break;
  }

  if(context.bigEndian())
  switch(address & 7) {
  case 0:
    if(!write<Dual>(address & ~7 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Byte>(address & ~7 | 1, data >> 56)) return;
    if(!write<Half>(address & ~7 | 2, data >> 40)) return;
    if(!write<Word>(address & ~7 | 4, data >>  8)) return;
    break;
  case 2:
    if(!write<Half>(address & ~7 | 2, data >> 48)) return;
    if(!write<Word>(address & ~7 | 4, data >> 16)) return;
    break;
  case 3:
    if(!write<Byte>(address & ~7 | 3, data >> 56)) return;
    if(!write<Word>(address & ~7 | 4, data >> 24)) return;
    break;
  case 4:
    if(!write<Word>(address & ~7 | 4, data >> 32)) return;
    break;
  case 5:
    if(!write<Byte>(address & ~7 | 5, data >> 56)) return;
    if(!write<Half>(address & ~7 | 6, data >> 40)) return;
    break;
  case 6:
    if(!write<Half>(address & ~7 | 6, data >> 48)) return;
    break;
  case 7:
    if(!write<Byte>(address & ~7 | 7, data >> 56)) return;
    break;
  }
}

auto CPU::SDR(cr64& rt, cr64& rs, s16 imm) -> void {
  if(!context.kernelMode() && context.bits == 32) return exception.reservedInstruction();
  u64 address = rs.u64 + imm;
  u64 data = rt.u64;

  if(context.littleEndian())
  switch(address & 7) {
  case 0:
    if(!write<Dual>(address & ~7 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Word>(address & ~7 | 0, data >> 24)) return;
    if(!write<Half>(address & ~7 | 4, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 6, data >>  0)) return;
    break;
  case 2:
    if(!write<Word>(address & ~7 | 0, data >> 16)) return;
    if(!write<Half>(address & ~7 | 4, data >>  0)) return;
    break;
  case 3:
    if(!write<Word>(address & ~7 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 4, data >>  0)) return;
    break;
  case 4:
    if(!write<Word>(address & ~7 | 0, data >>  0)) return;
    break;
  case 5:
    if(!write<Half>(address & ~7 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 2, data >>  0)) return;
    break;
  case 6:
    if(!write<Half>(address & ~7 | 0, data >>  0)) return;
    break;
  case 7:
    if(!write<Byte>(address & ~7 | 0, data >>  0)) return;
    break;
  }

  if(context.bigEndian())
  switch(address & 7) {
  case 0:
    if(!write<Byte>(address & ~7 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Half>(address & ~7 | 0, data >>  0)) return;
    break;
  case 2:
    if(!write<Half>(address & ~7 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 2, data >>  0)) return;
    break;
  case 3:
    if(!write<Word>(address & ~7 | 0, data >>  0)) return;
    break;
  case 4:
    if(!write<Word>(address & ~7 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 4, data >>  0)) return;
    break;
  case 5:
    if(!write<Word>(address & ~7 | 0, data >> 16)) return;
    if(!write<Half>(address & ~7 | 4, data >>  0)) return;
    break;
  case 6:
    if(!write<Word>(address & ~7 | 0, data >> 24)) return;
    if(!write<Half>(address & ~7 | 4, data >>  8)) return;
    if(!write<Byte>(address & ~7 | 6, data >>  0)) return;
    break;
  case 7:
    if(!write<Dual>(address & ~7 | 0, data >>  0)) return;
    break;
  }
}

auto CPU::SH(cr64& rt, cr64& rs, s16 imm) -> void {
  write<Half>(rs.u32 + imm, rt.u32);
}

auto CPU::SLL(r64& rd, cr64& rt, u8 sa) -> void {
  rd.u64 = s32(rt.u32 << sa);
}

auto CPU::SLLV(r64& rd, cr64& rt, cr64& rs) -> void {
  rd.u64 = s32(rt.u32 << (rs.u32 & 31));
}

auto CPU::SLT(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = rs.s64 < rt.s64;
}

auto CPU::SLTI(r64& rt, cr64& rs, s16 imm) -> void {
  rt.u64 = rs.s64 < imm;
}

auto CPU::SLTIU(r64& rt, cr64& rs, s16 imm) -> void {
  rt.u64 = rs.u64 < imm;
}

auto CPU::SLTU(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = rs.u64 < rt.u64;
}

auto CPU::SRA(r64& rd, cr64& rt, u8 sa) -> void {
  rd.u64 = s32(rt.s64 >> sa);
}

auto CPU::SRAV(r64& rd, cr64& rt, cr64& rs) -> void {
  rd.u64 = s32(rt.s64 >> (rs.u32 & 31));
}

auto CPU::SRL(r64& rd, cr64& rt, u8 sa) -> void {
  rd.u64 = s32(rt.u32 >> sa);
}

auto CPU::SRLV(r64& rd, cr64& rt, cr64& rs) -> void {
  rd.u64 = s32(rt.u32 >> (rs.u32 & 31));
}

auto CPU::SUB(r64& rd, cr64& rs, cr64& rt) -> void {
  if((rs.u32 ^ rt.u32) & (rs.u32 ^ rs.u32 - rt.u32) & 1 << 31) return exception.arithmeticOverflow();
  rd.u64 = s32(rs.u32 - rt.u32);
}

auto CPU::SUBU(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = s32(rs.u32 - rt.u32);
}

auto CPU::SW(cr64& rt, cr64& rs, s16 imm) -> void {
  write<Word>(rs.u32 + imm, rt.u32);
}

auto CPU::SWL(cr64& rt, cr64& rs, s16 imm) -> void {
  u64 address = rs.u64 + imm;
  u32 data = rt.u32;

  if(context.littleEndian())
  switch(address & 3) {
  case 0:
    if(!write<Byte>(address & ~3 | 3, data >> 24)) return;
    break;
  case 1:
    if(!write<Half>(address & ~3 | 2, data >> 16)) return;
    break;
  case 2:
    if(!write<Byte>(address & ~3 | 1, data >> 24)) return;
    if(!write<Half>(address & ~3 | 2, data >>  8)) return;
    break;
  case 3:
    if(!write<Word>(address & ~3 | 0, data >>  0)) return;
    break;
  }

  if(context.bigEndian())
  switch(address & 3) {
  case 0:
    if(!write<Word>(address & ~3 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Byte>(address & ~3 | 1, data >> 24)) return;
    if(!write<Half>(address & ~3 | 2, data >>  8)) return;
    break;
  case 2:
    if(!write<Half>(address & ~3 | 2, data >> 16)) return;
    break;
  case 3:
    if(!write<Byte>(address & ~3 | 3, data >> 24)) return;
    break;
  }
}

auto CPU::SWR(cr64& rt, cr64& rs, s16 imm) -> void {
  u64 address = rs.u64 + imm;
  u32 data = rt.u32;

  if(context.littleEndian())
  switch(address & 3) {
  case 0:
    if(!write<Word>(address & ~3 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Half>(address & ~3 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~3 | 2, data >>  0)) return;
    break;
  case 2:
    if(!write<Half>(address & ~3 | 0, data >>  0)) return;
    break;
  case 3:
    if(!write<Byte>(address & ~3 | 0, data >>  0)) return;
    break;
  }

  if(context.bigEndian())
  switch(address & 3) {
  case 0:
    if(!write<Byte>(address & ~3 | 0, data >>  0)) return;
    break;
  case 1:
    if(!write<Half>(address & ~3 | 0, data >>  0)) return;
    break;
  case 2:
    if(!write<Half>(address & ~3 | 0, data >>  8)) return;
    if(!write<Byte>(address & ~3 | 2, data >>  0)) return;
    break;
  case 3:
    if(!write<Word>(address & ~3 | 0, data >>  0)) return;
    break;
  }
}

auto CPU::SYNC() -> void {
  //no operation; for compatibility with R4000-series code
}

auto CPU::SYSCALL() -> void {
  exception.systemCall();
}

auto CPU::TEQ(cr64& rs, cr64& rt) -> void {
  if(rs.u64 == rt.u64) exception.trap();
}

auto CPU::TEQI(cr64& rs, s16 imm) -> void {
  if(rs.s64 == imm) exception.trap();
}

auto CPU::TGE(cr64& rs, cr64& rt) -> void {
  if(rs.s64 >= rt.s64) exception.trap();
}

auto CPU::TGEI(cr64& rs, s16 imm) -> void {
  if(rs.s64 >= imm) exception.trap();
}

auto CPU::TGEIU(cr64& rs, s16 imm) -> void {
  if(rs.u64 >= imm) exception.trap();
}

auto CPU::TGEU(cr64& rs, cr64& rt) -> void {
  if(rs.u64 >= rt.u64) exception.trap();
}

auto CPU::TLT(cr64& rs, cr64& rt) -> void {
  if(rs.s64 < rt.s64) exception.trap();
}

auto CPU::TLTI(cr64& rs, s16 imm) -> void {
  if(rs.s64 < imm) exception.trap();
}

auto CPU::TLTIU(cr64& rs, s16 imm) -> void {
  if(rs.u64 < imm) exception.trap();
}

auto CPU::TLTU(cr64& rs, cr64& rt) -> void {
  if(rs.u64 < rt.u64) exception.trap();
}

auto CPU::TNE(cr64& rs, cr64& rt) -> void {
  if(rs.u64 != rt.u64) exception.trap();
}

auto CPU::TNEI(cr64& rs, s16 imm) -> void {
  if(rs.s64 != imm) exception.trap();
}

auto CPU::XOR(r64& rd, cr64& rs, cr64& rt) -> void {
  rd.u64 = rs.u64 ^ rt.u64;
}

auto CPU::XORI(r64& rt, cr64& rs, u16 imm) -> void {
  rt.u64 = rs.u64 ^ imm;
}

#undef PC
#undef RA
#undef LO
#undef HI
