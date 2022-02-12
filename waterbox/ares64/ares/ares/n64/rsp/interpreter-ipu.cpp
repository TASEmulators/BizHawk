#define PC ipu.pc
#define RA ipu.r[31]

auto RSP::ADDIU(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = s32(rs.u32 + imm);
}

auto RSP::ADDU(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = s32(rs.u32 + rt.u32);
}

auto RSP::AND(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = rs.u32 & rt.u32;
}

auto RSP::ANDI(r32& rt, cr32& rs, u16 imm) -> void {
  rt.u32 = rs.u32 & imm;
}

auto RSP::BEQ(cr32& rs, cr32& rt, s16 imm) -> void {
  if(rs.u32 == rt.u32) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BGEZ(cr32& rs, s16 imm) -> void {
  if(rs.s32 >= 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BGEZAL(cr32& rs, s16 imm) -> void {
  RA.u32 = s32(PC + 8);
  if(rs.s32 >= 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BGTZ(cr32& rs, s16 imm) -> void {
  if(rs.s32 > 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BLEZ(cr32& rs, s16 imm) -> void {
  if(rs.s32 <= 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BLTZ(cr32& rs, s16 imm) -> void {
  if(rs.s32 < 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BLTZAL(cr32& rs, s16 imm) -> void {
  RA.u32 = s32(PC + 8);
  if(rs.s32 < 0) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BNE(cr32& rs, cr32& rt, s16 imm) -> void {
  if(rs.u32 != rt.u32) branch.take(PC + 4 + (imm << 2));
}

auto RSP::BREAK() -> void {
  status.halted = 1;
  status.broken = 1;
  if(status.interruptOnBreak) mi.raise(MI::IRQ::SP);
}

auto RSP::J(u32 imm) -> void {
  branch.take((PC + 4 & 0xf000'0000) | (imm << 2));
}

auto RSP::JAL(u32 imm) -> void {
  RA.u32 = s32(PC + 8);
  branch.take((PC + 4 & 0xf000'0000) | (imm << 2));
}

auto RSP::JALR(r32& rd, cr32& rs) -> void {
  rd.u32 = s32(PC + 8);
  branch.take(rs.u32);
}

auto RSP::JR(cr32& rs) -> void {
  branch.take(rs.u32);
}

auto RSP::LB(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = s8(dmem.read<Byte>(rs.u32 + imm));
}

auto RSP::LBU(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = u8(dmem.read<Byte>(rs.u32 + imm));
}

auto RSP::LH(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = s16(dmem.readUnaligned<Half>(rs.u32 + imm));
}

auto RSP::LHU(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = u16(dmem.readUnaligned<Half>(rs.u32 + imm));
}

auto RSP::LUI(r32& rt, u16 imm) -> void {
  rt.u32 = s32(imm << 16);
}

auto RSP::LW(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = s32(dmem.readUnaligned<Word>(rs.u32 + imm));
}

auto RSP::NOR(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = ~(rs.u32 | rt.u32);
}

auto RSP::OR(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = rs.u32 | rt.u32;
}

auto RSP::ORI(r32& rt, cr32& rs, u16 imm) -> void {
  rt.u32 = rs.u32 | imm;
}

auto RSP::SB(cr32& rt, cr32& rs, s16 imm) -> void {
  dmem.write<Byte>(rs.u32 + imm, rt.u32);
}

auto RSP::SH(cr32& rt, cr32& rs, s16 imm) -> void {
  dmem.writeUnaligned<Half>(rs.u32 + imm, rt.u32);
}

auto RSP::SLL(r32& rd, cr32& rt, u8 sa) -> void {
  rd.u32 = s32(rt.u32 << sa);
}

auto RSP::SLLV(r32& rd, cr32& rt, cr32& rs) -> void {
  rd.u32 = s32(rt.u32 << (rs.u32 & 31));
}

auto RSP::SLT(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = rs.s32 < rt.s32;
}

auto RSP::SLTI(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = rs.s32 < imm;
}

auto RSP::SLTIU(r32& rt, cr32& rs, s16 imm) -> void {
  rt.u32 = rs.u32 < imm;
}

auto RSP::SLTU(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = rs.u32 < rt.u32;
}

auto RSP::SRA(r32& rd, cr32& rt, u8 sa) -> void {
  rd.u32 = rt.s32 >> sa;
}

auto RSP::SRAV(r32& rd, cr32& rt, cr32& rs) -> void {
  rd.u32 = rt.s32 >> (rs.u32 & 31);
}

auto RSP::SRL(r32& rd, cr32& rt, u8 sa) -> void {
  rd.u32 = s32(rt.u32 >> sa);
}

auto RSP::SRLV(r32& rd, cr32& rt, cr32& rs) -> void {
  rd.u32 = s32(rt.u32 >> (rs.u32 & 31));
}

auto RSP::SUBU(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = s32(rs.u32 - rt.u32);
}

auto RSP::SW(cr32& rt, cr32& rs, s16 imm) -> void {
  dmem.writeUnaligned<Word>(rs.u32 + imm, rt.u32);
}

auto RSP::XOR(r32& rd, cr32& rs, cr32& rt) -> void {
  rd.u32 = rs.u32 ^ rt.u32;
}

auto RSP::XORI(r32& rt, cr32& rs, u16 imm) -> void {
  rt.u32 = rs.u32 ^ imm;
}

#undef PC
#undef RA
