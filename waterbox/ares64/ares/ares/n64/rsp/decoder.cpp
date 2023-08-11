#define jp(id, name, ...) case id: return decoder##name(instruction)
#define op(id, name, ...) case id: { OpInfo info = {}; __VA_ARGS__; return info; }

#define RD    (instruction >> 11 & 31)
#define RT    (instruction >> 16 & 31)
#define RS    (instruction >> 21 & 31)
#define VD    (instruction >>  6 & 31)
#define VS    (instruction >> 11 & 31)
#define VT    (instruction >> 16 & 31)

#define VCO   0
#define VCC   1
#define VCE   2

#define RUse(n)   info.r.use |= 1 << n
#define RDef(n)   info.r.def |= 1 << n
#define RDefB(n)  (void)0  //bypassable
#define VUse(n)   info.v.use |= 1 << n
#define VDef(n)   info.v.def |= 1 << n
#define VGUse(n)  info.v.use |= 0xff << (n & ~7)
#define VGDef(n)  info.v.def |= 0xff << (n & ~7)
#define VCUse(n)  info.vc.use |= 1 << (n & 3)
#define VCDef(n)  info.vc.def |= 1 << (n & 3)
#define VCRef(n)  VCUse(n), VCDef(n)
#define VFRef(n)  info.vfake |= 1 << n
#define Load      info.flags |= OpInfo::Load
#define Store     info.flags |= OpInfo::Store
#define Branch    info.flags |= OpInfo::Branch
#define Vector    info.flags |= OpInfo::Vector
#define VNopGroup info.flags |= OpInfo::VNopGroup

auto RSP::decoderEXECUTE(u32 instruction) const -> OpInfo {
  switch(instruction >> 26) {
  jp(0x00, SPECIAL);
  jp(0x01, REGIMM);
  op(0x02, J, Branch);
  op(0x03, JAL, Branch);
  op(0x04, BEQ, RUse(RS), RUse(RT), Branch);
  op(0x05, BNE, RUse(RS), RUse(RT), Branch);
  op(0x06, BLEZ, RUse(RS), Branch);
  op(0x07, BGTZ, RUse(RS), Branch);
  op(0x08, ADDI, RDefB(RT), RUse(RS));
  op(0x09, ADDIU, RDefB(RT), RUse(RS));
  op(0x0a, SLTI, RDefB(RT), RUse(RS));
  op(0x0b, SLTIU, RDefB(RT), RUse(RS));
  op(0x0c, ANDI, RDefB(RT), RUse(RS));
  op(0x0d, ORI, RDefB(RT), RUse(RS));
  op(0x0e, XORI, RDefB(RT), RUse(RS));
  op(0x0f, LUI, RDefB(RT));
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
  op(0x20, LB, RDef(RT), RUse(RS), Load);
  op(0x21, LH, RDef(RT), RUse(RS), Load);
  op(0x22, INVALID);  //LWL
  op(0x23, LW, RDef(RT), RUse(RS), Load);
  op(0x24, LBU, RDef(RT), RUse(RS), Load);
  op(0x25, LHU, RDef(RT), RUse(RS), Load);
  op(0x26, INVALID);  //LWR
  op(0x27, LWU, RDef(RT), RUse(RS), Load);
  op(0x28, SB, RUse(RT), RUse(RS), Store);
  op(0x29, SH, RUse(RT), RUse(RS), Store);
  op(0x2a, INVALID);  //SWL
  op(0x2b, SW, RUse(RT), RUse(RS), Store);
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
  return {};
}

auto RSP::decoderSPECIAL(u32 instruction) const -> OpInfo {
  switch(instruction & 0x3f) {
  op(0x00, SLL, RDefB(RD), RUse(RT));
  op(0x01, INVALID);
  op(0x02, SRL, RDefB(RD), RUse(RT));
  op(0x03, SRA, RDefB(RD), RUse(RT));
  op(0x04, SLLV, RDefB(RD), RUse(RT), RUse(RS));
  op(0x05, INVALID);
  op(0x06, SRLV, RDefB(RD), RUse(RT), RUse(RS));
  op(0x07, SRAV, RDefB(RD), RUse(RT), RUse(RS));
  op(0x08, JR, RUse(RS), Branch);
  op(0x09, JALR, RDefB(RD), RUse(RS), Branch);
  op(0x0a, INVALID);
  op(0x0b, INVALID);
  op(0x0c, INVALID);  //SYSCALL
  op(0x0d, BREAK, Branch);
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
  op(0x20, ADDU, RDefB(RD), RUse(RS), RUse(RT));  //ADD
  op(0x21, ADDU, RDefB(RD), RUse(RS), RUse(RT));
  op(0x22, SUBU, RDefB(RD), RUse(RS), RUse(RT));  //SUB
  op(0x23, SUBU, RDefB(RD), RUse(RS), RUse(RT));
  op(0x24, AND, RDefB(RD), RUse(RS), RUse(RT));
  op(0x25, OR, RDefB(RD), RUse(RS), RUse(RT));
  op(0x26, XOR, RDefB(RD), RUse(RS), RUse(RT));
  op(0x27, NOR, RDefB(RD), RUse(RS), RUse(RT));
  op(0x28, INVALID);
  op(0x29, INVALID);
  op(0x2a, SLT, RDefB(RD), RUse(RS), RUse(RT));
  op(0x2b, SLTU, RDefB(RD), RUse(RS), RUse(RT));
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
  return {};
}

auto RSP::decoderREGIMM(u32 instruction) const -> OpInfo {
  switch(instruction >> 16 & 0x1f) {
  op(0x00, BLTZ, RUse(RS), Branch);
  op(0x01, BGEZ, RUse(RS), Branch);
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
  op(0x10, BLTZAL, RUse(RS), Branch);
  op(0x11, BGEZAL, RUse(RS), Branch);
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
  return {};
}

auto RSP::decoderSCC(u32 instruction) const -> OpInfo {
  switch(instruction >> 21 & 0x1f) {
  op(0x00, MFC0, RDef(RT), Load, Store);
  op(0x01, INVALID);  //DMFC0
  op(0x02, INVALID);  //CFC0
  op(0x03, INVALID);
  op(0x04, MTC0, RUse(RT), Load, Store);
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
  return {};
}

auto RSP::decoderVU(u32 instruction) const -> OpInfo {
  switch(instruction >> 21 & 0x1f) {
  op(0x00, MFC2, RDef(RT), VUse(VS), Load, Store);
  op(0x01, INVALID);  //DMFC2
  op(0x02, CFC2, RDef(RT), VCUse(RD), Load, Store);
  op(0x03, INVALID);
  op(0x04, MTC2, RUse(RT), VDef(VS), Load, Store, VNopGroup);
  op(0x05, INVALID);  //DMTC2
  op(0x06, CTC2, RUse(RT), VCDef(RD), Load, Store);
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

  switch(instruction & 0x3f) {
  op(0x00, VMULF, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x01, VMULU, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x02, VRNDP, VDef(VD), VUse(VT), Vector);
  op(0x03, VMULQ, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x04, VMUDL, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x05, VMUDM, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x06, VMUDN, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x07, VMUDH, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x08, VMACF, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x09, VMACU, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x0a, VRNDN, VDef(VD), VUse(VT), Vector);
  op(0x0b, VMACQ, VDef(VD), Vector);
  op(0x0c, VMADL, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x0d, VMADM, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x0e, VMADN, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x0f, VMADH, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x10, VADD, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), Vector);
  op(0x11, VSUB, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), Vector);
  op(0x12, VZERO, Vector);  //VSUT
  op(0x13, VABS, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), Vector);
  op(0x14, VADDC, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), Vector);
  op(0x15, VSUBC, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), Vector);
  op(0x16, VZERO, Vector);  //VADDB
  op(0x17, VZERO, Vector);  //VSUBB
  op(0x18, VZERO, Vector);  //VACCB
  op(0x19, VZERO, Vector);  //VSUCB
  op(0x1a, VZERO, Vector);  //VSAD
  op(0x1b, VZERO, Vector);  //VSAC
  op(0x1c, VZERO, Vector);  //VSUM
  op(0x1d, VSAR, VDef(VD), Vector);
  op(0x1e, VZERO, Vector);
  op(0x1f, VZERO, Vector);
  op(0x20, VLT, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), Vector);
  op(0x21, VEQ, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), Vector);
  op(0x22, VNE, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), Vector);
  op(0x23, VGE, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), Vector);
  op(0x24, VCL, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), VCRef(VCE), Vector);
  op(0x25, VCH, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), VCRef(VCE), Vector);
  op(0x26, VCR, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), VCRef(VCE), Vector);
  op(0x27, VMRG, VDef(VD), VUse(VS), VUse(VT), VCRef(VCO), VCRef(VCC), Vector);
  op(0x28, VAND, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x29, VNAND, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x2a, VOR, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x2b, VNOR, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x2c, VXOR, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x2d, VNXOR, VDef(VD), VUse(VS), VUse(VT), Vector);
  op(0x2e, VZERO, Vector);
  op(0x2f, VZERO, Vector);
  op(0x30, VRCP, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x31, VRCPL, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x32, VRCPH, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x33, VMOV, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x34, VRSQ, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x35, VRSQL, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x36, VRSQH, VDef(VD), VFRef(VS), VUse(VT), Vector);
  op(0x37, VNOP, VFRef(VD), Vector, VNopGroup);
  op(0x38, VZERO, Vector);  //VEXTT
  op(0x39, VZERO, Vector);  //VEXTQ
  op(0x3a, VZERO, Vector);  //VEXTN
  op(0x3b, VZERO, Vector);
  op(0x3c, VZERO, Vector);  //VINST
  op(0x3d, VZERO, Vector);  //VINSQ
  op(0x3e, VZERO, Vector);  //VINSN
  op(0x3f, VNOP, Vector);  //VNULL
  }
  return {};
}

auto RSP::decoderLWC2(u32 instruction) const -> OpInfo {
  switch(instruction >> 11 & 0x1f) {
  op(0x00, LBV, VDef(VT), RUse(RS), Load);
  op(0x01, LSV, VDef(VT), RUse(RS), Load);
  op(0x02, LLV, VDef(VT), RUse(RS), Load);
  op(0x03, LDV, VDef(VT), RUse(RS), Load);
  op(0x04, LQV, VDef(VT), RUse(RS), Load);
  op(0x05, LRV, VDef(VT), RUse(RS), Load);
  op(0x06, LPV, VDef(VT), RUse(RS), Load);
  op(0x07, LUV, VDef(VT), RUse(RS), Load);
  op(0x08, LHV, VDef(VT), RUse(RS), Load);
  op(0x09, LFV, VDef(VT), RUse(RS), Load);
//op(0x0a, LWV, VDef(VT), RUse(RS), Load);  //not present on N64 RSP
  op(0x0b, LTV, VGDef(VT), RUse(RS), Load, VNopGroup);
  }
  return {};
}

auto RSP::decoderSWC2(u32 instruction) const -> OpInfo {
  switch(instruction >> 11 & 0x1f) {
  op(0x00, SBV, VUse(VT), RUse(RS), Store);
  op(0x01, SSV, VUse(VT), RUse(RS), Store);
  op(0x02, SLV, VUse(VT), RUse(RS), Store);
  op(0x03, SDV, VUse(VT), RUse(RS), Store);
  op(0x04, SQV, VUse(VT), RUse(RS), Store);
  op(0x05, SRV, VUse(VT), RUse(RS), Store);
  op(0x06, SPV, VUse(VT), RUse(RS), Store);
  op(0x07, SUV, VUse(VT), RUse(RS), Store);
  op(0x08, SHV, VUse(VT), RUse(RS), Store);
  op(0x09, SFV, VUse(VT), RUse(RS), Store);
  op(0x0a, SWV, VUse(VT), RUse(RS), Store);
  op(0x0b, STV, VGUse(VT), RUse(RS), Store);
  }
  return {};
}

#undef RUse
#undef RDef
#undef RDefB
#undef VUse
#undef VDef
#undef VGUse
#undef VGDef
#undef VCUse
#undef VCDef
#undef VCRef
#undef VFRef
#undef Load
#undef Store
#undef Branch
#undef Vector
#undef VNopGroup

#undef VCO
#undef VCC
#undef VCE

#undef RD
#undef RT
#undef RS
#undef VD
#undef VS
#undef VT

#undef jp
#undef op
