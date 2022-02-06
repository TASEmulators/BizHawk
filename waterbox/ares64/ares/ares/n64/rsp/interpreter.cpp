#define OP pipeline.instruction
#define RD ipu.r[RDn]
#define RT ipu.r[RTn]
#define RS ipu.r[RSn]
#define VD vpu.r[VDn]
#define VS vpu.r[VSn]
#define VT vpu.r[VTn]

#define jp(id, name, ...) case id: return decoder##name(__VA_ARGS__)
#define op(id, name, ...) case id: return name(__VA_ARGS__)
#define br(id, name, ...) case id: return name(__VA_ARGS__)
#define vu(id, name, ...) case id: \
  switch(E) { \
  case 0x0: return name<0x0>(__VA_ARGS__); \
  case 0x1: return name<0x1>(__VA_ARGS__); \
  case 0x2: return name<0x2>(__VA_ARGS__); \
  case 0x3: return name<0x3>(__VA_ARGS__); \
  case 0x4: return name<0x4>(__VA_ARGS__); \
  case 0x5: return name<0x5>(__VA_ARGS__); \
  case 0x6: return name<0x6>(__VA_ARGS__); \
  case 0x7: return name<0x7>(__VA_ARGS__); \
  case 0x8: return name<0x8>(__VA_ARGS__); \
  case 0x9: return name<0x9>(__VA_ARGS__); \
  case 0xa: return name<0xa>(__VA_ARGS__); \
  case 0xb: return name<0xb>(__VA_ARGS__); \
  case 0xc: return name<0xc>(__VA_ARGS__); \
  case 0xd: return name<0xd>(__VA_ARGS__); \
  case 0xe: return name<0xe>(__VA_ARGS__); \
  case 0xf: return name<0xf>(__VA_ARGS__); \
  }

#define SA     (OP >>  6 & 31)
#define RDn    (OP >> 11 & 31)
#define RTn    (OP >> 16 & 31)
#define RSn    (OP >> 21 & 31)
#define VDn    (OP >>  6 & 31)
#define VSn    (OP >> 11 & 31)
#define VTn    (OP >> 16 & 31)
#define IMMi16 s16(OP)
#define IMMu16 u16(OP)
#define IMMu26 (OP & 0x03ff'ffff)

auto RSP::decoderEXECUTE() -> void {
  switch(OP >> 26) {
  jp(0x00, SPECIAL);
  jp(0x01, REGIMM);
  br(0x02, J, IMMu26);
  br(0x03, JAL, IMMu26);
  br(0x04, BEQ, RS, RT, IMMi16);
  br(0x05, BNE, RS, RT, IMMi16);
  br(0x06, BLEZ, RS, IMMi16);
  br(0x07, BGTZ, RS, IMMi16);
  op(0x08, ADDIU, RT, RS, IMMi16);  //ADDI
  op(0x09, ADDIU, RT, RS, IMMi16);
  op(0x0a, SLTI, RT, RS, IMMi16);
  op(0x0b, SLTIU, RT, RS, IMMi16);
  op(0x0c, ANDI, RT, RS, IMMu16);
  op(0x0d, ORI, RT, RS, IMMu16);
  op(0x0e, XORI, RT, RS, IMMu16);
  op(0x0f, LUI, RT, IMMu16);
  jp(0x10, SCC);
  op(0x11, INVALID);  //COP1
  jp(0x12, VU);
  op(0x13, INVALID);  //COP3
  op(0x14, INVALID);  //BEQL
  op(0x15, INVALID);  //BNEL
  op(0x16, INVALID);  //BLEZL
  op(0x17, INVALID);  //BGTZL
  op(0x18, INVALID);  //DADDI
  op(0x19, INVALID);  //DADDIU
  op(0x1a, INVALID);  //LDL
  op(0x1b, INVALID);  //LDR
  op(0x1c, INVALID);
  op(0x1d, INVALID);
  op(0x1e, INVALID);
  op(0x1f, INVALID);
  op(0x20, LB, RT, RS, IMMi16);
  op(0x21, LH, RT, RS, IMMi16);
  op(0x22, INVALID);  //LWL
  op(0x23, LW, RT, RS, IMMi16);
  op(0x24, LBU, RT, RS, IMMi16);
  op(0x25, LHU, RT, RS, IMMi16);
  op(0x26, INVALID);  //LWR
  op(0x27, INVALID);  //LWU
  op(0x28, SB, RT, RS, IMMi16);
  op(0x29, SH, RT, RS, IMMi16);
  op(0x2a, INVALID);  //SWL
  op(0x2b, SW, RT, RS, IMMi16);
  op(0x2c, INVALID);  //SDL
  op(0x2d, INVALID);  //SDR
  op(0x2e, INVALID);  //SWR
  op(0x2f, INVALID);  //CACHE
  op(0x30, INVALID);  //LL
  op(0x31, INVALID);  //LWC1
  jp(0x32, LWC2);
  op(0x33, INVALID);  //LWC3
  op(0x34, INVALID);  //LLD
  op(0x35, INVALID);  //LDC1
  op(0x36, INVALID);  //LDC2
  op(0x37, INVALID);  //LD
  op(0x38, INVALID);  //SC
  op(0x39, INVALID);  //SWC1
  jp(0x3a, SWC2);
  op(0x3b, INVALID);  //SWC3
  op(0x3c, INVALID);  //SCD
  op(0x3d, INVALID);  //SDC1
  op(0x3e, INVALID);  //SDC2
  op(0x3f, INVALID);  //SD
  }
}

auto RSP::decoderSPECIAL() -> void {
  switch(OP & 0x3f) {
  op(0x00, SLL, RD, RT, SA);
  op(0x01, INVALID);
  op(0x02, SRL, RD, RT, SA);
  op(0x03, SRA, RD, RT, SA);
  op(0x04, SLLV, RD, RT, RS);
  op(0x05, INVALID);
  op(0x06, SRLV, RD, RT, RS);
  op(0x07, SRAV, RD, RT, RS);
  br(0x08, JR, RS);
  br(0x09, JALR, RD, RS);
  op(0x0a, INVALID);
  op(0x0b, INVALID);
  op(0x0c, INVALID);  //SYSCALL
  br(0x0d, BREAK);
  op(0x0e, INVALID);
  op(0x0f, INVALID);  //SYNC
  op(0x10, INVALID);  //MFHI
  op(0x11, INVALID);  //MTHI
  op(0x12, INVALID);  //MFLO
  op(0x13, INVALID);  //MTLO
  op(0x14, INVALID);  //DSLLV
  op(0x15, INVALID);
  op(0x16, INVALID);  //DSRLV
  op(0x17, INVALID);  //DSRAV
  op(0x18, INVALID);  //MULT
  op(0x19, INVALID);  //MULTU
  op(0x1a, INVALID);  //DIV
  op(0x1b, INVALID);  //DIVU
  op(0x1c, INVALID);  //DMULT
  op(0x1d, INVALID);  //DMULTU
  op(0x1e, INVALID);  //DDIV
  op(0x1f, INVALID);  //DDIVU
  op(0x20, ADDU, RD, RS, RT);  //ADD
  op(0x21, ADDU, RD, RS, RT);
  op(0x22, SUBU, RD, RS, RT);  //SUB
  op(0x23, SUBU, RD, RS, RT);
  op(0x24, AND, RD, RS, RT);
  op(0x25, OR, RD, RS, RT);
  op(0x26, XOR, RD, RS, RT);
  op(0x27, NOR, RD, RS, RT);
  op(0x28, INVALID);
  op(0x29, INVALID);
  op(0x2a, SLT, RD, RS, RT);
  op(0x2b, SLTU, RD, RS, RT);
  op(0x2c, INVALID);  //DADD
  op(0x2d, INVALID);  //DADDU
  op(0x2e, INVALID);  //DSUB
  op(0x2f, INVALID);  //DSUBU
  op(0x30, INVALID);  //TGE
  op(0x31, INVALID);  //TGEU
  op(0x32, INVALID);  //TLT
  op(0x33, INVALID);  //TLTU
  op(0x34, INVALID);  //TEQ
  op(0x35, INVALID);
  op(0x36, INVALID);  //TNE
  op(0x37, INVALID);
  op(0x38, INVALID);  //DSLL
  op(0x39, INVALID);
  op(0x3a, INVALID);  //DSRL
  op(0x3b, INVALID);  //DSRA
  op(0x3c, INVALID);  //DSLL32
  op(0x3d, INVALID);
  op(0x3e, INVALID);  //DSRL32
  op(0x3f, INVALID);  //DSRA32
  }
}

auto RSP::decoderREGIMM() -> void {
  switch(OP >> 16 & 0x1f) {
  br(0x00, BLTZ, RS, IMMi16);
  br(0x01, BGEZ, RS, IMMi16);
  op(0x02, INVALID);  //BLTZL
  op(0x03, INVALID);  //BGEZL
  op(0x04, INVALID);
  op(0x05, INVALID);
  op(0x06, INVALID);
  op(0x07, INVALID);
  op(0x08, INVALID);  //TGEI
  op(0x09, INVALID);  //TGEIU
  op(0x0a, INVALID);  //TLTI
  op(0x0b, INVALID);  //TLTIU
  op(0x0c, INVALID);  //TEQI
  op(0x0d, INVALID);
  op(0x0e, INVALID);  //TNEI
  op(0x0f, INVALID);
  br(0x10, BLTZAL, RS, IMMi16);
  br(0x11, BGEZAL, RS, IMMi16);
  op(0x12, INVALID);  //BLTZALL
  op(0x13, INVALID);  //BGEZALL
  op(0x14, INVALID);
  op(0x15, INVALID);
  op(0x16, INVALID);
  op(0x17, INVALID);
  op(0x18, INVALID);
  op(0x19, INVALID);
  op(0x1a, INVALID);
  op(0x1b, INVALID);
  op(0x1c, INVALID);
  op(0x1d, INVALID);
  op(0x1e, INVALID);
  op(0x1f, INVALID);
  }
}

auto RSP::decoderSCC() -> void {
  switch(OP >> 21 & 0x1f) {
  op(0x00, MFC0, RT, RDn);
  op(0x01, INVALID);  //DMFC0
  op(0x02, INVALID);  //CFC0
  op(0x03, INVALID);
  op(0x04, MTC0, RT, RDn);
  op(0x05, INVALID);  //DMTC0
  op(0x06, INVALID);  //CTC0
  op(0x07, INVALID);
  op(0x08, INVALID);  //BC0
  op(0x09, INVALID);
  op(0x0a, INVALID);
  op(0x0b, INVALID);
  op(0x0c, INVALID);
  op(0x0d, INVALID);
  op(0x0e, INVALID);
  op(0x0f, INVALID);
  }
}

auto RSP::decoderVU() -> void {
  #define E (OP >> 7 & 15)
  switch(OP >> 21 & 0x1f) {
  vu(0x00, MFC2, RT, VS);
  op(0x01, INVALID);  //DMFC2
  op(0x02, CFC2, RT, RDn);
  op(0x03, INVALID);
  vu(0x04, MTC2, RT, VS);
  op(0x05, INVALID);  //DMTC2
  op(0x06, CTC2, RT, RDn);
  op(0x07, INVALID);
  op(0x08, INVALID);  //BC2
  op(0x09, INVALID);
  op(0x0a, INVALID);
  op(0x0b, INVALID);
  op(0x0c, INVALID);
  op(0x0d, INVALID);
  op(0x0e, INVALID);
  op(0x0f, INVALID);
  }
  #undef E

  #define E  (OP >> 21 & 15)
  #define DE (OP >> 11 &  7)
  switch(OP & 0x3f) {
  vu(0x00, VMULF, VD, VS, VT);
  vu(0x01, VMULU, VD, VS, VT);
  vu(0x02, VRNDP, VD, VSn, VT);
  vu(0x03, VMULQ, VD, VS, VT);
  vu(0x04, VMUDL, VD, VS, VT);
  vu(0x05, VMUDM, VD, VS, VT);
  vu(0x06, VMUDN, VD, VS, VT);
  vu(0x07, VMUDH, VD, VS, VT);
  vu(0x08, VMACF, VD, VS, VT);
  vu(0x09, VMACU, VD, VS, VT);
  vu(0x0a, VRNDN, VD, VSn, VT);
  op(0x0b, VMACQ, VD);
  vu(0x0c, VMADL, VD, VS, VT);
  vu(0x0d, VMADM, VD, VS, VT);
  vu(0x0e, VMADN, VD, VS, VT);
  vu(0x0f, VMADH, VD, VS, VT);
  vu(0x10, VADD, VD, VS, VT);
  vu(0x11, VSUB, VD, VS, VT);
  op(0x12, INVALID);
  vu(0x13, VABS, VD, VS, VT);
  vu(0x14, VADDC, VD, VS, VT);
  vu(0x15, VSUBC, VD, VS, VT);
  op(0x16, INVALID);
  op(0x17, INVALID);
  op(0x18, INVALID);
  op(0x19, INVALID);
  op(0x1a, INVALID);
  op(0x1b, INVALID);
  op(0x1c, INVALID);
  vu(0x1d, VSAR, VD, VS);
  op(0x1e, INVALID);
  op(0x1f, INVALID);
  vu(0x20, VLT, VD, VS, VT);
  vu(0x21, VEQ, VD, VS, VT);
  vu(0x22, VNE, VD, VS, VT);
  vu(0x23, VGE, VD, VS, VT);
  vu(0x24, VCL, VD, VS, VT);
  vu(0x25, VCH, VD, VS, VT);
  vu(0x26, VCR, VD, VS, VT);
  vu(0x27, VMRG, VD, VS, VT);
  vu(0x28, VAND, VD, VS, VT);
  vu(0x29, VNAND, VD, VS, VT);
  vu(0x2a, VOR, VD, VS, VT);
  vu(0x2b, VNOR, VD, VS, VT);
  vu(0x2c, VXOR, VD, VS, VT);
  vu(0x2d, VNXOR, VD, VS, VT);
  op(0x2e, INVALID);
  op(0x2f, INVALID);
  vu(0x30, VRCP, VD, DE, VT);
  vu(0x31, VRCPL, VD, DE, VT);
  vu(0x32, VRCPH, VD, DE, VT);
  vu(0x33, VMOV, VD, DE, VT);
  vu(0x34, VRSQ, VD, DE, VT);
  vu(0x35, VRSQL, VD, DE, VT);
  vu(0x36, VRSQH, VD, DE, VT);
  op(0x37, VNOP);
  op(0x38, INVALID);
  op(0x39, INVALID);
  op(0x3a, INVALID);
  op(0x3b, INVALID);
  op(0x3c, INVALID);
  op(0x3d, INVALID);
  op(0x3e, INVALID);
  op(0x3f, INVALID);
  }
  #undef E
  #undef DE
}

auto RSP::decoderLWC2() -> void {
  #define E     (OP >> 7 & 15)
  #define IMMi7 i7(OP)
  switch(OP >> 11 & 0x1f) {
  vu(0x00, LBV, VT, RS, IMMi7);
  vu(0x01, LSV, VT, RS, IMMi7);
  vu(0x02, LLV, VT, RS, IMMi7);
  vu(0x03, LDV, VT, RS, IMMi7);
  vu(0x04, LQV, VT, RS, IMMi7);
  vu(0x05, LRV, VT, RS, IMMi7);
  vu(0x06, LPV, VT, RS, IMMi7);
  vu(0x07, LUV, VT, RS, IMMi7);
  vu(0x08, LHV, VT, RS, IMMi7);
  vu(0x09, LFV, VT, RS, IMMi7);
//vu(0x0a, LWV, VT, RS, IMMi7);  //not present on N64 RSP
  vu(0x0b, LTV, VTn, RS, IMMi7);
  }
  #undef E
  #undef IMMi7
}

auto RSP::decoderSWC2() -> void {
  #define E     (OP >> 7 & 15)
  #define IMMi7 i7(OP)
  switch(OP >> 11 & 0x1f) {
  vu(0x00, SBV, VT, RS, IMMi7);
  vu(0x01, SSV, VT, RS, IMMi7);
  vu(0x02, SLV, VT, RS, IMMi7);
  vu(0x03, SDV, VT, RS, IMMi7);
  vu(0x04, SQV, VT, RS, IMMi7);
  vu(0x05, SRV, VT, RS, IMMi7);
  vu(0x06, SPV, VT, RS, IMMi7);
  vu(0x07, SUV, VT, RS, IMMi7);
  vu(0x08, SHV, VT, RS, IMMi7);
  vu(0x09, SFV, VT, RS, IMMi7);
  vu(0x0a, SWV, VT, RS, IMMi7);
  vu(0x0b, STV, VTn, RS, IMMi7);
  }
  #undef E
  #undef IMMi7
}

auto RSP::INVALID() -> void {
}

#undef SA
#undef RDn
#undef RTn
#undef RSn
#undef VDn
#undef VSn
#undef VTn
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
#undef VD
#undef VS
#undef VT
