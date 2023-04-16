#define OP pipeline.instruction
#define RD ipu.r[RDn]
#define RT ipu.r[RTn]
#define RS ipu.r[RSn]

#define jp(id, name, ...) case id: return decoder##name(__VA_ARGS__)
#define op(id, name, ...) case id: return name(__VA_ARGS__)
#define br(id, name, ...) case id: return name(__VA_ARGS__)

#define SA     (OP >>  6 & 31)
#define RDn    (OP >> 11 & 31)
#define RTn    (OP >> 16 & 31)
#define RSn    (OP >> 21 & 31)
#define FD     (OP >>  6 & 31)
#define FS     (OP >> 11 & 31)
#define FT     (OP >> 16 & 31)
#define IMMi16 s16(OP)
#define IMMu16 u16(OP)
#define IMMu26 (OP & 0x03ff'ffff)

auto CPU::decoderEXECUTE() -> void {
  switch(OP >> 26) {
  jp(0x00, SPECIAL);
  jp(0x01, REGIMM);
  br(0x02, J, IMMu26);
  br(0x03, JAL, IMMu26);
  br(0x04, BEQ, RS, RT, IMMi16);
  br(0x05, BNE, RS, RT, IMMi16);
  br(0x06, BLEZ, RS, IMMi16);
  br(0x07, BGTZ, RS, IMMi16);
  op(0x08, ADDI, RT, RS, IMMi16);
  op(0x09, ADDIU, RT, RS, IMMi16);
  op(0x0a, SLTI, RT, RS, IMMi16);
  op(0x0b, SLTIU, RT, RS, IMMi16);
  op(0x0c, ANDI, RT, RS, IMMu16);
  op(0x0d, ORI, RT, RS, IMMu16);
  op(0x0e, XORI, RT, RS, IMMu16);
  op(0x0f, LUI, RT, IMMu16);
  jp(0x10, SCC);
  jp(0x11, FPU);
  br(0x12, COP2);
  br(0x13, COP3);
  br(0x14, BEQL, RS, RT, IMMi16);
  br(0x15, BNEL, RS, RT, IMMi16);
  br(0x16, BLEZL, RS, IMMi16);
  br(0x17, BGTZL, RS, IMMi16);
  op(0x18, DADDI, RT, RS, IMMi16);
  op(0x19, DADDIU, RT, RS, IMMi16);
  op(0x1a, LDL, RT, RS, IMMi16);
  op(0x1b, LDR, RT, RS, IMMi16);
  br(0x1c, INVALID);
  br(0x1d, INVALID);
  br(0x1e, INVALID);
  br(0x1f, INVALID);
  op(0x20, LB, RT, RS, IMMi16);
  op(0x21, LH, RT, RS, IMMi16);
  op(0x22, LWL, RT, RS, IMMi16);
  op(0x23, LW, RT, RS, IMMi16);
  op(0x24, LBU, RT, RS, IMMi16);
  op(0x25, LHU, RT, RS, IMMi16);
  op(0x26, LWR, RT, RS, IMMi16);
  op(0x27, LWU, RT, RS, IMMi16);
  op(0x28, SB, RT, RS, IMMi16);
  op(0x29, SH, RT, RS, IMMi16);
  op(0x2a, SWL, RT, RS, IMMi16);
  op(0x2b, SW, RT, RS, IMMi16);
  op(0x2c, SDL, RT, RS, IMMi16);
  op(0x2d, SDR, RT, RS, IMMi16);
  op(0x2e, SWR, RT, RS, IMMi16);
  op(0x2f, CACHE, OP >> 16 & 31, RS, IMMi16);
  op(0x30, LL, RT, RS, IMMi16);
  op(0x31, LWC1, FT, RS, IMMi16);
  br(0x32, COP2);  //LWC2
  br(0x33, COP3);  //LWC3
  op(0x34, LLD, RT, RS, IMMi16);
  op(0x35, LDC1, FT, RS, IMMi16);
  br(0x36, COP2);  //LDC2
  op(0x37, LD, RT, RS, IMMi16);
  op(0x38, SC, RT, RS, IMMi16);
  op(0x39, SWC1, FT, RS, IMMi16);
  br(0x3a, COP2);  //SWC2
  br(0x3b, COP3);  //SWC3
  op(0x3c, SCD, RT, RS, IMMi16);
  op(0x3d, SDC1, FT, RS, IMMi16);
  br(0x3e, COP2);  //SDC2
  op(0x3f, SD, RT, RS, IMMi16);
  }
}

auto CPU::decoderSPECIAL() -> void {
  switch(OP & 0x3f) {
  op(0x00, SLL, RD, RT, SA);
  br(0x01, INVALID);
  op(0x02, SRL, RD, RT, SA);
  op(0x03, SRA, RD, RT, SA);
  op(0x04, SLLV, RD, RT, RS);
  br(0x05, INVALID);
  op(0x06, SRLV, RD, RT, RS);
  op(0x07, SRAV, RD, RT, RS);
  br(0x08, JR, RS);
  br(0x09, JALR, RD, RS);
  br(0x0a, INVALID);
  br(0x0b, INVALID);
  br(0x0c, SYSCALL);
  br(0x0d, BREAK);
  br(0x0e, INVALID);
  op(0x0f, SYNC);
  op(0x10, MFHI, RD);
  op(0x11, MTHI, RS);
  op(0x12, MFLO, RD);
  op(0x13, MTLO, RS);
  op(0x14, DSLLV, RD, RT, RS);
  br(0x15, INVALID);
  op(0x16, DSRLV, RD, RT, RS);
  op(0x17, DSRAV, RD, RT, RS);
  op(0x18, MULT, RS, RT);
  op(0x19, MULTU, RS, RT);
  op(0x1a, DIV, RS, RT);
  op(0x1b, DIVU, RS, RT);
  op(0x1c, DMULT, RS, RT);
  op(0x1d, DMULTU, RS, RT);
  op(0x1e, DDIV, RS, RT);
  op(0x1f, DDIVU, RS, RT);
  op(0x20, ADD, RD, RS, RT);
  op(0x21, ADDU, RD, RS, RT);
  op(0x22, SUB, RD, RS, RT);
  op(0x23, SUBU, RD, RS, RT);
  op(0x24, AND, RD, RS, RT);
  op(0x25, OR, RD, RS, RT);
  op(0x26, XOR, RD, RS, RT);
  op(0x27, NOR, RD, RS, RT);
  br(0x28, INVALID);
  br(0x29, INVALID);
  op(0x2a, SLT, RD, RS, RT);
  op(0x2b, SLTU, RD, RS, RT);
  op(0x2c, DADD, RD, RS, RT);
  op(0x2d, DADDU, RD, RS, RT);
  op(0x2e, DSUB, RD, RS, RT);
  op(0x2f, DSUBU, RD, RS, RT);
  op(0x30, TGE, RS, RT);
  op(0x31, TGEU, RS, RT);
  op(0x32, TLT, RS, RT);
  op(0x33, TLTU, RS, RT);
  op(0x34, TEQ, RS, RT);
  br(0x35, INVALID);
  op(0x36, TNE, RS, RT);
  br(0x37, INVALID);
  op(0x38, DSLL, RD, RT, SA);
  br(0x39, INVALID);
  op(0x3a, DSRL, RD, RT, SA);
  op(0x3b, DSRA, RD, RT, SA);
  op(0x3c, DSLL, RD, RT, SA + 32);
  br(0x3d, INVALID);
  op(0x3e, DSRL, RD, RT, SA + 32);
  op(0x3f, DSRA, RD, RT, SA + 32);
  }
}

auto CPU::decoderREGIMM() -> void {
  switch(OP >> 16 & 0x1f) {
  br(0x00, BLTZ, RS, IMMi16);
  br(0x01, BGEZ, RS, IMMi16);
  br(0x02, BLTZL, RS, IMMi16);
  br(0x03, BGEZL, RS, IMMi16);
  br(0x04, INVALID);
  br(0x05, INVALID);
  br(0x06, INVALID);
  br(0x07, INVALID);
  op(0x08, TGEI, RS, IMMi16);
  op(0x09, TGEIU, RS, IMMi16);
  op(0x0a, TLTI, RS, IMMi16);
  op(0x0b, TLTIU, RS, IMMi16);
  op(0x0c, TEQI, RS, IMMi16);
  br(0x0d, INVALID);
  op(0x0e, TNEI, RS, IMMi16);
  br(0x0f, INVALID);
  br(0x10, BLTZAL, RS, IMMi16);
  br(0x11, BGEZAL, RS, IMMi16);
  br(0x12, BLTZALL, RS, IMMi16);
  br(0x13, BGEZALL, RS, IMMi16);
  br(0x14, INVALID);
  br(0x15, INVALID);
  br(0x16, INVALID);
  br(0x17, INVALID);
  br(0x18, INVALID);
  br(0x19, INVALID);
  br(0x1a, INVALID);
  br(0x1b, INVALID);
  br(0x1c, INVALID);
  br(0x1d, INVALID);
  br(0x1e, INVALID);
  br(0x1f, INVALID);
  }
}

auto CPU::decoderSCC() -> void {
  switch(OP >> 21 & 0x1f) {
  op(0x00, MFC0, RT, RDn);
  op(0x01, DMFC0, RT, RDn);
  br(0x02, INVALID);  //CFC0
  br(0x03, INVALID);
  op(0x04, MTC0, RT, RDn);
  op(0x05, DMTC0, RT, RDn);
  br(0x06, INVALID);  //CTC0
  br(0x07, INVALID);
  br(0x08, INVALID);  //BC0
  br(0x09, INVALID);
  br(0x0a, INVALID);
  br(0x0b, INVALID);
  br(0x0c, INVALID);
  br(0x0d, INVALID);
  br(0x0e, INVALID);
  br(0x0f, INVALID);
  }

  switch(OP & 0x3f) {
  op(0x01, TLBR);
  op(0x02, TLBWI);
  op(0x06, TLBWR);
  op(0x08, TLBP);
  br(0x18, ERET);
  }

  //undefined instructions do not throw a reserved instruction exception
}

auto CPU::decoderFPU() -> void {
  switch(OP >> 21 & 0x1f) {
  op(0x00, MFC1, RT, FS);
  op(0x01, DMFC1, RT, FS);
  op(0x02, CFC1, RT, RDn);
  br(0x03, INVALID);
  op(0x04, MTC1, RT, FS);
  op(0x05, DMTC1, RT, FS);
  op(0x06, CTC1, RT, RDn);
  br(0x07, INVALID);
  br(0x08, BC1, OP >> 16 & 1, OP >> 17 & 1, IMMi16);
  br(0x09, INVALID);
  br(0x0a, INVALID);
  br(0x0b, INVALID);
  br(0x0c, INVALID);
  br(0x0d, INVALID);
  br(0x0e, INVALID);
  br(0x0f, INVALID);
  }

  if((OP >> 21 & 31) == 16)
  switch(OP & 0x3f) {
  op(0x00, FADD_S, FD, FS, FT);
  op(0x01, FSUB_S, FD, FS, FT);
  op(0x02, FMUL_S, FD, FS, FT);
  op(0x03, FDIV_S, FD, FS, FT);
  op(0x04, FSQRT_S, FD, FS);
  op(0x05, FABS_S, FD, FS);
  op(0x06, FMOV_S, FD, FS);
  op(0x07, FNEG_S, FD, FS);
  op(0x08, FROUND_L_S, FD, FS);
  op(0x09, FTRUNC_L_S, FD, FS);
  op(0x0a, FCEIL_L_S, FD, FS);
  op(0x0b, FFLOOR_L_S, FD, FS);
  op(0x0c, FROUND_W_S, FD, FS);
  op(0x0d, FTRUNC_W_S, FD, FS);
  op(0x0e, FCEIL_W_S, FD, FS);
  op(0x0f, FFLOOR_W_S, FD, FS);
  op(0x21, FCVT_D_S, FD, FS);
  op(0x24, FCVT_W_S, FD, FS);
  op(0x25, FCVT_L_S, FD, FS);
  op(0x30, FC_F_S, FS, FT);
  op(0x31, FC_UN_S, FS, FT);
  op(0x32, FC_EQ_S, FS, FT);
  op(0x33, FC_UEQ_S, FS, FT);
  op(0x34, FC_OLT_S, FS, FT);
  op(0x35, FC_ULT_S, FS, FT);
  op(0x36, FC_OLE_S, FS, FT);
  op(0x37, FC_ULE_S, FS, FT);
  op(0x38, FC_SF_S, FS, FT);
  op(0x39, FC_NGLE_S, FS, FT);
  op(0x3a, FC_SEQ_S, FS, FT);
  op(0x3b, FC_NGL_S, FS, FT);
  op(0x3c, FC_LT_S, FS, FT);
  op(0x3d, FC_NGE_S, FS, FT);
  op(0x3e, FC_LE_S, FS, FT);
  op(0x3f, FC_NGT_S, FS, FT);
  }

  if((OP >> 21 & 31) == 17)
  switch(OP & 0x3f) {
  op(0x00, FADD_D, FD, FS, FT);
  op(0x01, FSUB_D, FD, FS, FT);
  op(0x02, FMUL_D, FD, FS, FT);
  op(0x03, FDIV_D, FD, FS, FT);
  op(0x04, FSQRT_D, FD, FS);
  op(0x05, FABS_D, FD, FS);
  op(0x06, FMOV_D, FD, FS);
  op(0x07, FNEG_D, FD, FS);
  op(0x08, FROUND_L_D, FD, FS);
  op(0x09, FTRUNC_L_D, FD, FS);
  op(0x0a, FCEIL_L_D, FD, FS);
  op(0x0b, FFLOOR_L_D, FD, FS);
  op(0x0c, FROUND_W_D, FD, FS);
  op(0x0d, FTRUNC_W_D, FD, FS);
  op(0x0e, FCEIL_W_D, FD, FS);
  op(0x0f, FFLOOR_W_D, FD, FS);
  op(0x20, FCVT_S_D, FD, FS);
  op(0x24, FCVT_W_D, FD, FS);
  op(0x25, FCVT_L_D, FD, FS);
  op(0x30, FC_F_D, FS, FT);
  op(0x31, FC_UN_D, FS, FT);
  op(0x32, FC_EQ_D, FS, FT);
  op(0x33, FC_UEQ_D, FS, FT);
  op(0x34, FC_OLT_D, FS, FT);
  op(0x35, FC_ULT_D, FS, FT);
  op(0x36, FC_OLE_D, FS, FT);
  op(0x37, FC_ULE_D, FS, FT);
  op(0x38, FC_SF_D, FS, FT);
  op(0x39, FC_NGLE_D, FS, FT);
  op(0x3a, FC_SEQ_D, FS, FT);
  op(0x3b, FC_NGL_D, FS, FT);
  op(0x3c, FC_LT_D, FS, FT);
  op(0x3d, FC_NGE_D, FS, FT);
  op(0x3e, FC_LE_D, FS, FT);
  op(0x3f, FC_NGT_D, FS, FT);
  }

  if((OP >> 21 & 31) == 20)
  switch(OP & 0x3f) {
  op(0x20, FCVT_S_W, FD, FS);
  op(0x21, FCVT_D_W, FD, FS);
  }

  if((OP >> 21 & 31) == 21)
  switch(OP & 0x3f) {
  op(0x20, FCVT_S_L, FD, FS);
  op(0x21, FCVT_D_L, FD, FS);
  }

  //undefined instructions do not throw a reserved instruction exception
}

auto CPU::COP2() -> void {
  exception.coprocessor2();
}

auto CPU::COP3() -> void {
  exception.coprocessor3();
}

auto CPU::INVALID() -> void {
  exception.reservedInstruction();
}

#undef SA
#undef RDn
#undef RTn
#undef RSn
#undef FD
#undef FS
#undef FT
#undef IMMi16
#undef IMMu16
#undef IMMu26

#undef jp
#undef op
#undef br

#undef OP
#undef RD
#undef RT
#undef RS
