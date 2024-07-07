auto RSP::Recompiler::measure(u12 address) -> u12 {
  u12 start = address;
  bool hasBranched = 0;
  while(true) {
    u32 instruction = self.imem.read<Word>(address);
    bool branched = isTerminal(instruction);
    address += 4;
    if(hasBranched || address == start) break;
    hasBranched = branched;
  }

  return address - start;
}

auto RSP::Recompiler::hash(u12 address, u12 size) -> u64 {
  u12 end = address + size;
  if(address < end) {
    return XXH3_64bits(self.imem.data + address, size);
  } else {
    return XXH3_64bits(self.imem.data + address, self.imem.size - address)
         ^ XXH3_64bits(self.imem.data, end);
  }
}

auto RSP::Recompiler::block(u12 address) -> Block* {
  if(dirty) {
    u12 address = 0;
    for(auto& block : context) {
      if(block && (dirty & mask(address, block->size)) != 0) {
        block = nullptr;
      }
      address += 4;
    }
    dirty = 0;
  }

  if(auto block = context[address >> 2]) return block;

  auto size = measure(address);
  auto hashcode = hash(address, size);
  hashcode ^= self.pipeline.hash();

  BlockHashPair pair;
  pair.hashcode = hashcode;
  if(auto result = blocks.find(pair)) {
    return context[address >> 2] = result->block;
  }

  auto block = emit(address);
  assert(block->size == size);
  memory::jitprotect(true);

  pair.block = block;
  if(auto result = blocks.insert(pair)) {
    return context[address >> 2] = result->block;
  }

  throw;  //should never occur
}

auto RSP::Recompiler::emit(u12 address) -> Block* {
  if(unlikely(allocator.available() < 1_MiB)) {
    print("RSP allocator flush\n");
    allocator.release();
    reset();
  }

  pipeline = self.pipeline;

  auto block = (Block*)allocator.acquire(sizeof(Block));
  beginFunction(3);

  u12 start = address;
  bool hasBranched = 0;
  while(true) {
    u32 instruction = self.imem.read<Word>(address);
    if(callInstructionPrologue) {
      mov32(reg(1), imm(instruction));
      call(&RSP::instructionPrologue);
    }
    pipeline.begin();
    OpInfo op0 = self.decoderEXECUTE(instruction);
    pipeline.issue(op0);
    bool branched = emitEXECUTE(instruction);

    if(!pipeline.singleIssue && !branched && u12(address + 4) != start) {
      u32 instruction = self.imem.read<Word>(address + 4);
      OpInfo op1 = self.decoderEXECUTE(instruction);

      if(RSP::canDualIssue(op0, op1)) {
        mov32(reg(1), imm(0));
        call(&RSP::instructionEpilogue);
        if(callInstructionPrologue) {
          mov32(reg(1), imm(instruction));
          call(&RSP::instructionPrologue);
        }
        address += 4;
        pipeline.issue(op1);
        branched = emitEXECUTE(instruction);
      }
    }

    pipeline.end();
    mov32(reg(1), imm(pipeline.clocks));
    call(&RSP::instructionEpilogue);
    address += 4;
    if(hasBranched || address == start) break;
    hasBranched = branched;
    testJumpEpilog();
  }
  jumpEpilog();

  //reset clocks to zero every time block is executed
  pipeline.clocks = 0;

  memory::jitprotect(false);
  block->code = endFunction();
  block->size = address - start;
  block->pipeline = pipeline;

//print(hex(PC, 8L), " ", instructions, " ", size(), "\n");
  return block;
}

#define Sa  (instruction >>  6 & 31)
#define Rdn (instruction >> 11 & 31)
#define Rtn (instruction >> 16 & 31)
#define Rsn (instruction >> 21 & 31)
#define Vdn (instruction >>  6 & 31)
#define Vsn (instruction >> 11 & 31)
#define Vtn (instruction >> 16 & 31)
#define Rd  sreg(1), offsetof(IPU, r) + Rdn * sizeof(r32)
#define Rt  sreg(1), offsetof(IPU, r) + Rtn * sizeof(r32)
#define Rs  sreg(1), offsetof(IPU, r) + Rsn * sizeof(r32)
#define Vd  sreg(2), offsetof(VU, r) + Vdn * sizeof(r128)
#define Vs  sreg(2), offsetof(VU, r) + Vsn * sizeof(r128)
#define Vt  sreg(2), offsetof(VU, r) + Vtn * sizeof(r128)
#define i16 s16(instruction)
#define n16 u16(instruction)
#define n26 u32(instruction & 0x03ff'ffff)
#define callvu(name) \
  switch(E) { \
  case 0x0: call(name<0x0>); break; \
  case 0x1: call(name<0x1>); break; \
  case 0x2: call(name<0x2>); break; \
  case 0x3: call(name<0x3>); break; \
  case 0x4: call(name<0x4>); break; \
  case 0x5: call(name<0x5>); break; \
  case 0x6: call(name<0x6>); break; \
  case 0x7: call(name<0x7>); break; \
  case 0x8: call(name<0x8>); break; \
  case 0x9: call(name<0x9>); break; \
  case 0xa: call(name<0xa>); break; \
  case 0xb: call(name<0xb>); break; \
  case 0xc: call(name<0xc>); break; \
  case 0xd: call(name<0xd>); break; \
  case 0xe: call(name<0xe>); break; \
  case 0xf: call(name<0xf>); break; \
  }

auto RSP::Recompiler::emitEXECUTE(u32 instruction) -> bool {
  switch(instruction >> 26) {

  //SPECIAL
  case 0x00: {
    return emitSPECIAL(instruction);
  }

  //REGIMM
  case 0x01: {
    return emitREGIMM(instruction);
  }

  //J n26
  case 0x02: {
    mov32(reg(1), imm(n26));
    call(&RSP::J);
    return 1;
  }

  //JAL n26
  case 0x03: {
    mov32(reg(1), imm(n26));
    call(&RSP::JAL);
    return 1;
  }

  //BEQ Rs,Rt,i16
  case 0x04: {
    lea(reg(1), Rs);
    lea(reg(2), Rt);
    mov32(reg(3), imm(i16));
    call(&RSP::BEQ);
    return 1;
  }

  //BNE Rs,Rt,i16
  case 0x05: {
    lea(reg(1), Rs);
    lea(reg(2), Rt);
    mov32(reg(3), imm(i16));
    call(&RSP::BNE);
    return 1;
  }

  //BLEZ Rs,i16
  case 0x06: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BLEZ);
    return 1;
  }

  //BGTZ Rs,i16
  case 0x07: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BGTZ);
    return 1;
  }

  //ADDIU Rt,Rs,i16
  case range2(0x08, 0x09): {
    add32(mem(Rt), mem(Rs), imm(i16));
    return 0;
  }

  //SLTI Rt,Rs,i16
  case 0x0a: {
    cmp32(mem(Rs), imm(i16), set_slt);
    mov32_f(mem(Rt), flag_slt);
    return 0;
  }

  //SLTIU Rt,Rs,i16
  case 0x0b: {
    cmp32(mem(Rs), imm(i16), set_ult);
    mov32_f(mem(Rt), flag_ult);
    return 0;
  }

  //ANDI Rt,Rs,n16
  case 0x0c: {
    and32(mem(Rt), mem(Rs), imm(n16));
    return 0;
  }

  //ORI Rt,Rs,n16
  case 0x0d: {
    or32(mem(Rt), mem(Rs), imm(n16));
    return 0;
  }

  //XORI Rt,Rs,n16
  case 0x0e: {
    xor32(mem(Rt), mem(Rs), imm(n16));
    return 0;
  }

  //LUI Rt,n16
  case 0x0f: {
    mov32(mem(Rt), imm(s32(n16 << 16)));
    return 0;
  }

  //SCC
  case 0x10: {
    return emitSCC(instruction);
  }

  //INVALID
  case 0x11: {
    return 0;
  }

  //VPU
  case 0x12: {
    return emitVU(instruction);
  }

  //INVALID
  case range13(0x13, 0x1f): {
    return 0;
  }

  //LB Rt,Rs,i16
  case 0x20: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LB);
    return 0;
  }

  //LH Rt,Rs,i16
  case 0x21: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LH);
    return 0;
  }

  //INVALID
  case 0x22: {
    return 0;
  }

  //LW Rt,Rs,i16
  case 0x23: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LW);
    return 0;
  }

  //LBU Rt,Rs,i16
  case 0x24: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LBU);
    return 0;
  }

  //LHU Rt,Rs,i16
  case 0x25: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LHU);
    return 0;
  }

  //INVALID
  case 0x26: {
    return 0;
  }

  //LWU Rt,Rs,i16
  case 0x27: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::LWU);
    return 0;
  }

  //SB Rt,Rs,i16
  case 0x28: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::SB);
    return 0;
  }

  //SH Rt,Rs,i16
  case 0x29: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::SH);
    return 0;
  }

  //INVALID
  case 0x2a: {
    return 0;
  }

  //SW Rt,Rs,i16
  case 0x2b: {
    lea(reg(1), Rt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i16));
    call(&RSP::SW);
    return 0;
  }

  //INVALID
  case range6(0x2c, 0x31): {
    return 0;
  }

  //LWC2
  case 0x32: {
    return emitLWC2(instruction);
  }

  //INVALID
  case range7(0x33, 0x39): {
    return 0;
  }

  //SWC2
  case 0x3a: {
    return emitSWC2(instruction);
  }

  //INVALID
  case range5(0x3b, 0x3f): {
    return 0;
  }

  }

  return 0;
}

auto RSP::Recompiler::emitSPECIAL(u32 instruction) -> bool {
  switch(instruction & 0x3f) {

  //SLL Rd,Rt,Sa
  case 0x00: {
    shl32(mem(Rd), mem(Rt), imm(Sa));
    return 0;
  }

  //INVALID
  case 0x01: {
    return 0;
  }

  //SRL Rd,Rt,Sa
  case 0x02: {
    lshr32(mem(Rd), mem(Rt), imm(Sa));
    return 0;
  }

  //SRA Rd,Rt,Sa
  case 0x03: {
    ashr32(mem(Rd), mem(Rt), imm(Sa));
    return 0;
  }

  //SLLV Rd,Rt,Rs
  case 0x04: {
    mshl32(mem(Rd), mem(Rt), mem(Rs));
    return 0;
  }

  //INVALID
  case 0x05: {
    return 0;
  }

  //SRLV Rd,Rt,Rs
  case 0x06: {
    mlshr32(mem(Rd), mem(Rt), mem(Rs));
    return 0;
  }

  //SRAV Rd,Rt,Rs
  case 0x07: {
    mashr32(mem(Rd), mem(Rt), mem(Rs));
    return 0;
  }

  //JR Rs
  case 0x08: {
    lea(reg(1), Rs);
    call(&RSP::JR);
    return 1;
  }

  //JALR Rd,Rs
  case 0x09: {
    lea(reg(1), Rd);
    lea(reg(2), Rs);
    call(&RSP::JALR);
    return 1;
  }

  //INVALID
  case range3(0x0a, 0x0c): {
    return 0;
  }

  //BREAK
  case 0x0d: {
    call(&RSP::BREAK);
    return 1;
  }

  //INVALID
  case range18(0x0e, 0x1f): {
    return 0;
  }

  //ADDU Rd,Rs,Rt
  case range2(0x20, 0x21): {
    add32(mem(Rd), mem(Rs), mem(Rt));
    return 0;
  }

  //SUBU Rd,Rs,Rt
  case range2(0x22, 0x23): {
    sub32(mem(Rd), mem(Rs), mem(Rt));
    return 0;
  }

  //AND Rd,Rs,Rt
  case 0x24: {
    and32(mem(Rd), mem(Rs), mem(Rt));
    return 0;
  }

  //OR Rd,Rs,Rt
  case 0x25: {
    or32(mem(Rd), mem(Rs), mem(Rt));
    return 0;
  }

  //XOR Rd,Rs,Rt
  case 0x26: {
    xor32(mem(Rd), mem(Rs), mem(Rt));
    return 0;
  }

  //NOR Rd,Rs,Rt
  case 0x27: {
    or32(reg(0), mem(Rs), mem(Rt));
    xor32(reg(0), reg(0), imm(-1));
    mov32(mem(Rd), reg(0));
    return 0;
  }

  //INVALID
  case range2(0x28, 0x29): {
    return 0;
  }

  //SLT Rd,Rs,Rt
  case 0x2a: {
    cmp32(mem(Rs), mem(Rt), set_slt);
    mov32_f(mem(Rd), flag_slt);
    return 0;
  }

  //SLTU Rd,Rs,Rt
  case 0x2b: {
    cmp32(mem(Rs), mem(Rt), set_ult);
    mov32_f(mem(Rd), flag_ult);
    return 0;
  }

  //INVALID
  case range20(0x2c, 0x3f): {
    return 0;
  }

  }

  return 0;
}

auto RSP::Recompiler::emitREGIMM(u32 instruction) -> bool {
  switch(instruction >> 16 & 0x1f) {

  //BLTZ Rs,i16
  case 0x00: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BLTZ);
    return 1;
  }

  //BGEZ Rs,i16
  case 0x01: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BGEZ);
    return 1;
  }

  //INVALID
  case range14(0x02, 0x0f): {
    return 0;
  }

  //BLTZAL Rs,i16
  case 0x10: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BLTZAL);
    return 1;
  }

  //BGEZAL Rs,i16
  case 0x11: {
    lea(reg(1), Rs);
    mov32(reg(2), imm(i16));
    call(&RSP::BGEZAL);
    return 1;
  }

  //INVALID
  case range14(0x12, 0x1f): {
    return 0;
  }

  }

  return 0;
}

auto RSP::Recompiler::emitSCC(u32 instruction) -> bool {
  switch(instruction >> 21 & 0x1f) {

  //MFC0 Rt,Rd
  case 0x00: {
    lea(reg(1), Rt);
    mov32(reg(2), imm(Rdn));
    call(&RSP::MFC0);
    return 0;
  }

  //INVALID
  case range3(0x01, 0x03): {
    return 0;
  }

  //MTC0 Rt,Rd
  case 0x04: {
    lea(reg(1), Rt);
    mov32(reg(2), imm(Rdn));
    call(&RSP::MTC0);
    return 0;
  }

  //INVALID
  case range27(0x05, 0x1f): {
    return 0;
  }

  }

  return 0;
}

auto RSP::Recompiler::emitVU(u32 instruction) -> bool {
  #define E (instruction >> 7 & 15)
  switch(instruction >> 21 & 0x1f) {

  //MFC2 Rt,Vs(e)
  case 0x00: {
    lea(reg(1), Rt);
    lea(reg(2), Vs);
    callvu(&RSP::MFC2);
    return 0;
  }

  //INVALID
  case 0x01: {
    return 0;
  }

  //CFC2 Rt,Rd
  case 0x02: {
    lea(reg(1), Rt);
    mov32(reg(2), imm(Rdn));
    call(&RSP::CFC2);
    return 0;
  }

  //INVALID
  case 0x03: {
    return 0;
  }

  //MTC2 Rt,Vs(e)
  case 0x04: {
    lea(reg(1), Rt);
    lea(reg(2), Vs);
    callvu(&RSP::MTC2);
    return 0;
  }

  //INVALID
  case 0x05: {
    return 0;
  }

  //CTC2 Rt,Rd
  case 0x06: {
    lea(reg(1), Rt);
    mov32(reg(2), imm(Rdn));
    call(&RSP::CTC2);
    return 0;
  }

  //INVALID
  case range9(0x07, 0x0f): {
    return 0;
  }

  }
  #undef E

  #define E  (instruction >> 21 & 15)
  #define DE (instruction >> 11 &  7)
  switch(instruction & 0x3f) {

  //VMULF Vd,Vs,Vt(e)
  case 0x00: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMULF);
    return 0;
  }

  //VMULU Vd,Vs,Vt(e)
  case 0x01: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMULU);
    return 0;
  }

  //VRNDP Vd,Vs,Vt(e)
  case 0x02: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(Vsn));
    lea(reg(3), Vt);
    callvu(&RSP::VRNDP);
    return 0;
  }

  //VMULQ Vd,Vs,Vt(e)
  case 0x03: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMULQ);
    return 0;
  }

  //VMUDL Vd,Vs,Vt(e)
  case 0x04: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMUDL);
    return 0;
  }

  //VMUDM Vd,Vs,Vt(e)
  case 0x05: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMUDM);
    return 0;
  }

  //VMUDN Vd,Vs,Vt(e)
  case 0x06: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMUDN);
    return 0;
  }

  //VMUDH Vd,Vs,Vt(e)
  case 0x07: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMUDH);
    return 0;
  }

  //VMACF Vd,Vs,Vt(e)
  case 0x08: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMACF);
    return 0;
  }

  //VMACU Vd,Vs,Vt(e)
  case 0x09: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMACU);
    return 0;
  }

  //VRNDN Vd,Vs,Vt(e)
  case 0x0a: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(Vsn));
    lea(reg(3), Vt);
    callvu(&RSP::VRNDN);
    return 0;
  }

  //VMACQ Vd
  case 0x0b: {
    lea(reg(1), Vd);
    call(&RSP::VMACQ);
    return 0;
  }

  //VMADL Vd,Vs,Vt(e)
  case 0x0c: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMADL);
    return 0;
  }

  //VMADM Vd,Vs,Vt(e)
  case 0x0d: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMADM);
    return 0;
  }

  //VMADN Vd,Vs,Vt(e)
  case 0x0e: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMADN);
    return 0;
  }

  //VMADH Vd,Vs,Vt(e)
  case 0x0f: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMADH);
    return 0;
  }

  //VADD Vd,Vs,Vt(e)
  case 0x10: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VADD);
    return 0;
  }

  //VSUB Vd,Vs,Vt(e)
  case 0x11: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VSUB);
    return 0;
  }

  //VSUT (broken)
  case 0x12: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;    
  }

  //VABS Vd,Vs,Vt(e)
  case 0x13: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VABS);
    return 0;
  }

  //VADDC Vd,Vs,Vt(e)
  case 0x14: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VADDC);
    return 0;
  }

  //VSUBC Vd,Vs,Vt(e)
  case 0x15: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VSUBC);
    return 0;
  }

  //Broken opcodes: VADDB, VSUBB, VACCB, VSUCB, VSAD, VSAC, VSUM
  case range7(0x16, 0x1c): {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;    
  }

  //VSAR Vd,Vs,E
  case 0x1d: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    callvu(&RSP::VSAR);
    return 0;
  }

  //Invalid opcodes
  case range2(0x1e, 0x1f): {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;    
  }

  //VLT Vd,Vs,Vt(e)
  case 0x20: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VLT);
    return 0;
  }

  //VEQ Vd,Vs,Vt(e)
  case 0x21: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VEQ);
    return 0;
  }

  //VNE Vd,Vs,Vt(e)
  case 0x22: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VNE);
    return 0;
  }

  //VGE Vd,Vs,Vt(e)
  case 0x23: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VGE);
    return 0;
  }

  //VCL Vd,Vs,Vt(e)
  case 0x24: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VCL);
    return 0;
  }

  //VCH Vd,Vs,Vt(e)
  case 0x25: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VCH);
    return 0;
  }

  //VCR Vd,Vs,Vt(e)
  case 0x26: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VCR);
    return 0;
  }

  //VMRG Vd,Vs,Vt(e)
  case 0x27: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VMRG);
    return 0;
  }

  //VAND Vd,Vs,Vt(e)
  case 0x28: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VAND);
    return 0;
  }

  //VNAND Vd,Vs,Vt(e)
  case 0x29: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VNAND);
    return 0;
  }

  //VOR Vd,Vs,Vt(e)
  case 0x2a: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VOR);
    return 0;
  }

  //VNOR Vd,Vs,Vt(e)
  case 0x2b: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VNOR);
    return 0;
  }

  //VXOR Vd,Vs,Vt(e)
  case 0x2c: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VXOR);
    return 0;
  }

  //VNXOR Vd,Vs,Vt(e)
  case 0x2d: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VNXOR);
    return 0;
  }

  //INVALID
  case range2(0x2e, 0x2f): {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;
  }

  //VCRP Vd(de),Vt(e)
  case 0x30: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRCP);
    return 0;
  }

  //VRCPL Vd(de),Vt(e)
  case 0x31: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRCPL);
    return 0;
  }

  //VRCPH Vd(de),Vt(e)
  case 0x32: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRCPH);
    return 0;
  }

  //VMOV Vd(de),Vt(e)
  case 0x33: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VMOV);
    return 0;
  }

  //VRSQ Vd(de),Vt(e)
  case 0x34: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRSQ);
    return 0;
  }

  //VRSQL Vd(de),Vt(e)
  case 0x35: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRSQL);
    return 0;
  }

  //VRSQH Vd(de),Vt(e)
  case 0x36: {
    lea(reg(1), Vd);
    mov32(reg(2), imm(DE));
    lea(reg(3), Vt);
    callvu(&RSP::VRSQH);
    return 0;
  }

  //VNOP
  case 0x37: {
    call(&RSP::VNOP);
    return 0;
  }

  //Broken opcodes: VEXTT, VEXTQ, VEXTN
  case range3(0x38, 0x3a): {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;        
  }

  //INVALID
  case 0x3b: {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;
  }

  //Broken opcodes: VINST, VINSQ, VINSN
  case range3(0x3c, 0x3e): {
    lea(reg(1), Vd);
    lea(reg(2), Vs);
    lea(reg(3), Vt);
    callvu(&RSP::VZERO);
    return 0;        
  }

  //VNULL
  case 0x3f: {
    call(&RSP::VNOP);    
    return 0;
  }

  }
  #undef E
  #undef DE

  return 0;
}

auto RSP::Recompiler::emitLWC2(u32 instruction) -> bool {
  #define E  (instruction >> 7 & 15)
  #define i7 (s8(instruction << 1) >> 1)
  switch(instruction >> 11 & 0x1f) {

  //LBV Vt(e),Rs,i7
  case 0x00: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LBV);
    return 0;
  }

  //LSV Vt(e),Rs,i7
  case 0x01: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LSV);
    return 0;
  }

  //LLV Vt(e),Rs,i7
  case 0x02: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LLV);
    return 0;
  }

  //LDV Vt(e),Rs,i7
  case 0x03: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LDV);
    return 0;
  }

  //LQV Vt(e),Rs,i7
  case 0x04: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LQV);
    return 0;
  }

  //LRV Vt(e),Rs,i7
  case 0x05: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LRV);
    return 0;
  }

  //LPV Vt(e),Rs,i7
  case 0x06: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LPV);
    return 0;
  }

  //LUV Vt(e),Rs,i7
  case 0x07: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LUV);
    return 0;
  }

  //LHV Vt(e),Rs,i7
  case 0x08: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LHV);
    return 0;
  }

  //LFV Vt(e),Rs,i7
  case 0x09: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LFV);
    return 0;
  }

  //LWV (not present on N64 RSP)
  case 0x0a: {
    return 0;
  }

  //LTV Vt(e),Rs,i7
  case 0x0b: {
    mov32(reg(1), imm(Vtn));
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::LTV);
    return 0;
  }

  //INVALID
  case range20(0x0c, 0x1f): {
    return 0;
  }

  }
  #undef E
  #undef i7

  return 0;
}

auto RSP::Recompiler::emitSWC2(u32 instruction) -> bool {
  #define E  (instruction >> 7 & 15)
  #define i7 (s8(instruction << 1) >> 1)
  switch(instruction >> 11 & 0x1f) {

  //SBV Vt(e),Rs,i7
  case 0x00: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SBV);
    return 0;
  }

  //SSV Vt(e),Rs,i7
  case 0x01: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SSV);
    return 0;
  }

  //SLV Vt(e),Rs,i7
  case 0x02: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SLV);
    return 0;
  }

  //SDV Vt(e),Rs,i7
  case 0x03: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SDV);
    return 0;
  }

  //SQV Vt(e),Rs,i7
  case 0x04: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SQV);
    return 0;
  }

  //SRV Vt(e),Rs,i7
  case 0x05: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SRV);
    return 0;
  }

  //SPV Vt(e),Rs,i7
  case 0x06: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SPV);
    return 0;
  }

  //SUV Vt(e),Rs,i7
  case 0x07: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SUV);
    return 0;
  }

  //SHV Vt(e),Rs,i7
  case 0x08: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SHV);
    return 0;
  }

  //SFV Vt(e),Rs,i7
  case 0x09: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SFV);
    return 0;
  }

  //SWV Vt(e),Rs,i7
  case 0x0a: {
    lea(reg(1), Vt);
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::SWV);
    return 0;
  }

  //STV Vt(e),Rs,i7
  case 0x0b: {
    mov32(reg(1), imm(Vtn));
    lea(reg(2), Rs);
    mov32(reg(3), imm(i7));
    callvu(&RSP::STV);
    return 0;
  }

  //INVALID
  case range20(0x0c, 0x1f): {
    return 0;
  }

  }
  #undef E
  #undef i7

  return 0;
}

auto RSP::Recompiler::isTerminal(u32 instruction) -> bool {
  switch(instruction >> 26) {

  //SPECIAL
  case 0x00: {
    switch(instruction & 0x3f) {

    //JR Rs
    case 0x08:
    //JALR Rd,Rs
    case 0x09:
    //BREAK
    case 0x0d:
      return 1;

    }

    break;
  }

  //REGIMM
  case 0x01: {
    switch(instruction >> 16 & 0x1f) {

    //BLTZ Rs,i16
    case 0x00:
    //BGEZ Rs,i16
    case 0x01:
    //BLTZAL Rs,i16
    case 0x10:
    //BGEZAL Rs,i16
    case 0x11:
      return 1;

    }

    break;
  }

  //J n26
  case 0x02:
  //JAL n26
  case 0x03:
  //BEQ Rs,Rt,i16
  case 0x04:
  //BNE Rs,Rt,i16
  case 0x05:
  //BLEZ Rs,i16
  case 0x06:
  //BGTZ Rs,i16
  case 0x07:
    return 1;

  }

  return 0;
}

#undef Sa
#undef Rdn
#undef Rtn
#undef Rsn
#undef Vdn
#undef Vsn
#undef Vtn
#undef Rd
#undef Rt
#undef Rs
#undef Vd
#undef Vs
#undef Vt
#undef i16
#undef n16
#undef n26
#undef callvu
