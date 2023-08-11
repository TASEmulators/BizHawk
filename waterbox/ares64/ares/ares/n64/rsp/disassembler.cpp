auto RSP::Disassembler::disassemble(u32 address, u32 instruction) -> string {
  this->address = address;
  this->instruction = instruction;

  auto v = EXECUTE();
  if(!v) v.append("invalid", string{"$", hex(instruction, 8L)});
  if(!instruction) v = {"nop"};
  auto s = pad(v.takeFirst(), -8L);
  return {s, v.merge(",")};
}

auto RSP::Disassembler::EXECUTE() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto rsValue = [&] { return ipuRegisterValue(instruction >> 21 & 31); };
  auto imm16i  = [&] { return immediate(s16(instruction)); };
  auto imm16u  = [&] { return immediate(u16(instruction), 16L); };
  auto jump    = [&] { return immediate(n12(address + 4 & 0xf000'0000 | (instruction & 0x03ff'ffff) << 2)); };
  auto branch  = [&] { return immediate(n12(address + 4 + (s16(instruction) << 2))); };
  auto offset  = [&] { return ipuRegisterIndex(instruction >> 21 & 31, s16(instruction)); };

  auto ADDI = [&](string_view add, string_view sub, string_view mov) -> vector<string> {
    if(!(instruction >> 21 & 31)) return {mov, rtName(), immediate(s16(instruction), 32L)};
    return {s16(instruction) >= 0 ? add : sub, rtName(), rsValue(), immediate(abs(s16(instruction)))};
  };

  auto ALU = [&](string_view name) -> vector<string> {
    return {name, rtName(), rsValue(), immediate(u16(instruction))};
  };

  auto BRANCH1 = [&](string_view name) -> vector<string> {
    return {name, rsValue(), branch()};
  };

  auto BRANCH2 = [&](string_view name) -> vector<string> {
    return {name, rsValue(), rtValue(), branch()};
  };

  auto CACHE = [&](string_view name) -> vector<string> {
    auto cache  = instruction >> 16 & 3;
    auto op     = instruction >> 18 & 7;
    string type = "reserved";
    if(cache == 0) switch(op) {
    case 0: type = "code(IndexInvalidate)"; break;
    case 1: type = "code(IndexLoadTag)"; break;
    case 2: type = "code(IndexStoreTag)"; break;
    case 4: type = "code(HitInvalidate)"; break;
    case 5: type = "code(Fill)"; break;
    case 6: type = "code(HitWriteBack)"; break;
    }
    if(cache == 1) switch(op) {
    case 0: type = "data(IndexWriteBackInvalidate)"; break;
    case 1: type = "data(IndexLoadTag)"; break;
    case 2: type = "data(IndexStoreTag)"; break;
    case 3: type = "data(CreateDirtyExclusive)"; break;
    case 4: type = "data(HitInvalidate)"; break;
    case 5: type = "data(HitWriteBackInvalidate)"; break;
    case 6: type = "data(HitWriteBack)"; break;
    }
    return {name, type, offset()};
  };

  auto JUMP = [&](string_view name) -> vector<string> {
    return {name, jump()};
  };

  auto LOAD = [&](string_view name) -> vector<string> {
    return {name, rtName(), offset()};
  };

  auto STORE = [&](string_view name) -> vector<string> {
    return {name, rtValue(), offset()};
  };

  switch(instruction >> 26) {
  case 0x00: return SPECIAL();
  case 0x01: return REGIMM();
  case 0x02: return JUMP("j");
  case 0x03: return JUMP("jal");
  case 0x04: return BRANCH2("beq");
  case 0x05: return BRANCH2("bne");
  case 0x06: return BRANCH1("blez");
  case 0x07: return BRANCH1("bgtz");
  case 0x08: return ADDI("addi",  "subi",  "li");
  case 0x09: return ADDI("addiu", "subiu", "liu");
  case 0x0a: return ALU("slti");
  case 0x0b: return ALU("sltiu");
  case 0x0c: return ALU("andi");
  case 0x0d: return ALU("ori");
  case 0x0e: return ALU("xori");
  case 0x0f: return {"lui", rtName(), imm16u()};
  case 0x10: return SCC();
  case 0x11: break;  //COP1
  case 0x12: return VU();
  case 0x13: break;  //COP3
  case 0x14: break;  //BEQL
  case 0x15: break;  //BNEL
  case 0x16: break;  //BLEZL
  case 0x17: break;  //BGTZL
  case 0x18: break;  //DADDI
  case 0x19: break;  //DADDIU
  case 0x1a: break;  //LDL
  case 0x1b: break;  //LDR
  case 0x1c: break;
  case 0x1d: break;
  case 0x1e: break;
  case 0x1f: break;
  case 0x20: return LOAD("lb");
  case 0x21: return LOAD("lh");
  case 0x22: break;  //LWL
  case 0x23: return LOAD("lw");
  case 0x24: return LOAD("lbu");
  case 0x25: return LOAD("lhu");
  case 0x26: break;  //LWR
  case 0x27: break;  //LWU
  case 0x28: return STORE("sb");
  case 0x29: return STORE("sh");
  case 0x2a: break;  //SWL
  case 0x2b: return STORE("sw");
  case 0x2c: break;  //SDL
  case 0x2d: break;  //SDR
  case 0x2e: break;  //SWR
  case 0x2f: return CACHE("cache");
  case 0x30: break;  //LL
  case 0x31: break;  //LWC1
  case 0x32: return LWC2();
  case 0x33: break;  //LWC3
  case 0x34: break;  //LLD
  case 0x35: break;  //LDC1
  case 0x36: break;  //LDC2
  case 0x37: break;  //LD
  case 0x38: break;  //SC
  case 0x39: break;  //SWC1
  case 0x3a: return SWC2();
  case 0x3b: break;  //SWC3
  case 0x3c: break;  //SCD
  case 0x3d: break;  //SDC1
  case 0x3e: break;  //SDC2
  case 0x3f: break;  //SD
  }

  return {};
}

auto RSP::Disassembler::SPECIAL() -> vector<string> {
  auto shift   = [&] { return string{instruction >> 6 & 31}; };
  auto rdName  = [&] { return ipuRegisterName (instruction >> 11 & 31); };
  auto rdValue = [&] { return ipuRegisterValue(instruction >> 11 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto rsValue = [&] { return ipuRegisterValue(instruction >> 21 & 31); };

  auto ALU = [&](string_view name, string_view by) -> vector<string> {
    return {name, rdName(), rtValue(), by};
  };

  auto JALR = [&](string_view name) -> vector<string> {
    if((instruction >> 11 & 31) == 31) return {name, rsValue()};
    return {name, rdName(), rsValue()};
  };

  auto REG = [&](string_view name) -> vector<string> {
    return {name, rdName(), rsValue(), rtValue()};
  };

  switch(instruction & 0x3f) {
  case 0x00: return ALU("sll", shift());
  case 0x01: break;
  case 0x02: return ALU("srl", shift());
  case 0x03: return ALU("sra", shift());
  case 0x04: return ALU("sllv", shift());
  case 0x05: break;
  case 0x06: return ALU("srlv", rsValue());
  case 0x07: return ALU("srav", rsValue());
  case 0x08: return {"jr", rsValue()};
  case 0x09: return JALR("jalr");
  case 0x0a: break;
  case 0x0b: break;
  case 0x0c: break;  //SYSCALL
  case 0x0d: return {"break"};
  case 0x0e: break;
  case 0x0f: break;  //SYNC
  case 0x10: break;  //MFHI
  case 0x11: break;  //MTHI
  case 0x12: break;  //MFLO
  case 0x13: break;  //MTLO
  case 0x14: break;  //DSLLV
  case 0x15: break;
  case 0x16: break;  //DSRLV
  case 0x17: break;  //DSRAV
  case 0x18: break;  //MULT
  case 0x19: break;  //MULTU
  case 0x1a: break;  //DIV
  case 0x1b: break;  //DIVU
  case 0x1c: break;  //DMULT
  case 0x1d: break;  //DMULTU
  case 0x1e: break;  //DDIV
  case 0x1f: break;  //DDIVU
  case 0x20: return REG("add");
  case 0x21: return REG("addu");
  case 0x22: return REG("sub");
  case 0x23: return REG("subu");
  case 0x24: return REG("and");
  case 0x25: return REG("or");
  case 0x26: return REG("xor");
  case 0x27: return REG("nor");
  case 0x28: break;
  case 0x29: break;
  case 0x2a: return REG("slt");
  case 0x2b: return REG("sltu");
  case 0x2c: break;  //DADD
  case 0x2d: break;  //DADDU
  case 0x2e: break;  //DSUB
  case 0x2f: break;  //DSUBU
  case 0x30: break;  //TGE
  case 0x31: break;  //TGEU
  case 0x32: break;  //TLT
  case 0x33: break;  //TLTU
  case 0x34: break;  //TEQ
  case 0x35: break;
  case 0x36: break;  //TNE
  case 0x37: break;
  case 0x38: break;  //DSLL
  case 0x39: break;
  case 0x3a: break;  //DSRL
  case 0x3b: break;  //DSRA
  case 0x3c: break;  //DSLL32
  case 0x3d: break;
  case 0x3e: break;  //DSRL32
  case 0x3f: break;  //DSRA32
  }

  return {};
}

auto RSP::Disassembler::REGIMM() -> vector<string> {
  auto rsValue = [&] { return ipuRegisterValue(instruction >> 21 & 31); };
  auto branch  = [&] { return immediate(n12(address + 4 + (s16(instruction) << 2))); };

  auto BRANCH = [&](string_view name) -> vector<string> {
    return {name, rsValue(), branch()};
  };

  switch(instruction >> 16 & 0x1f) {
  case 0x00: return BRANCH("bltz");
  case 0x01: return BRANCH("bgez");
  case 0x02: break;  //BLTZL
  case 0x03: break;  //BGEZL
  case 0x04: break;
  case 0x05: break;
  case 0x06: break;
  case 0x07: break;
  case 0x08: break;  //TGEI
  case 0x09: break;  //TGEIU
  case 0x0a: break;  //TLTI
  case 0x0b: break;  //TLTIU
  case 0x0c: break;  //TEQI
  case 0x0d: break;
  case 0x0e: break;  //TNEI
  case 0x0f: break;
  case 0x10: return BRANCH("bltzal");
  case 0x11: return BRANCH("bgezal");
  case 0x12: break;  //BLTZALL
  case 0x13: break;  //BGEZALL
  case 0x14: break;
  case 0x15: break;
  case 0x16: break;
  case 0x17: break;
  case 0x18: break;
  case 0x19: break;
  case 0x1a: break;
  case 0x1b: break;
  case 0x1c: break;
  case 0x1d: break;
  case 0x1e: break;
  case 0x1f: break;
  }

  return {};
}

auto RSP::Disassembler::SCC() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto sdName  = [&] { return sccRegisterName (instruction >> 11 & 31); };
  auto sdValue = [&] { return sccRegisterValue(instruction >> 11 & 31); };

  switch(instruction >> 21 & 0x1f) {
  case 0x00: return {"mfc0", rtName(), sdValue()};
  case 0x04: return {"mtc0", sdName(), rtValue()};
  }

  return {};
}

auto RSP::Disassembler::LWC2() -> vector<string> {
  auto vtName  = [&] { return vpuRegisterName  (instruction >> 16 & 31, instruction >> 7 & 15); };
  auto vtValue = [&] { return vpuRegisterValue (instruction >> 16 & 31, instruction >> 7 & 15); };
  auto offset  = [&](u32 multiplier) { return ipuRegisterIndex(instruction >> 21 & 31, i7(instruction) * multiplier); };

  switch(instruction >> 11 & 31) {
  case 0x00: return {"lbv", vtName(), offset( 1)};
  case 0x01: return {"lsv", vtName(), offset( 2)};
  case 0x02: return {"llv", vtName(), offset( 4)};
  case 0x03: return {"ldv", vtName(), offset( 8)};
  case 0x04: return {"lqv", vtName(), offset(16)};
  case 0x05: return {"lrv", vtName(), offset(16)};
  case 0x06: return {"lpv", vtName(), offset( 8)};
  case 0x07: return {"luv", vtName(), offset( 8)};
  case 0x08: return {"lhv", vtName(), offset(16)};
  case 0x09: return {"lfv", vtName(), offset(16)};
//case 0x0a: return {"lwv", vtName(), offset(16)};  //not present on N64 RSP
  case 0x0b: return {"ltv", vtName(), offset(16)};
  }
  return {};
}

auto RSP::Disassembler::SWC2() -> vector<string> {
  auto vtName  = [&] { return vpuRegisterName  (instruction >> 16 & 31); };
  auto vtValue = [&] { return vpuRegisterValue (instruction >> 16 & 31); };
  auto offset  = [&](u32 multiplier) { return ipuRegisterIndex(instruction >> 21 & 31, i7(instruction) * multiplier); };

  switch(instruction >> 11 & 31) {
  case 0x00: return {"sbv", vtValue(), offset( 1)};
  case 0x01: return {"ssv", vtValue(), offset( 2)};
  case 0x02: return {"slv", vtValue(), offset( 4)};
  case 0x03: return {"sdv", vtValue(), offset( 8)};
  case 0x04: return {"sqv", vtValue(), offset(16)};
  case 0x05: return {"srv", vtValue(), offset(16)};
  case 0x06: return {"spv", vtValue(), offset( 8)};
  case 0x07: return {"suv", vtValue(), offset( 8)};
  case 0x08: return {"shv", vtValue(), offset(16)};
  case 0x09: return {"sfv", vtValue(), offset(16)};
  case 0x0a: return {"swv", vtValue(), offset(16)};
  case 0x0b: return {"stv", vtValue(), offset(16)};
  }
  return {};
}

auto RSP::Disassembler::VU() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto rdName  = [&] { return vpuRegisterName  (instruction >> 11 & 31, instruction >> 7 & 15); };
  auto rdValue = [&] { return vpuRegisterValue (instruction >> 11 & 31, instruction >> 7 & 15); };
  auto cdName  = [&] { return ccrRegisterName (instruction >> 11 & 31); };
  auto cdValue = [&] { return ccrRegisterValue(instruction >> 11 & 31); };

  switch(instruction >> 21 & 0x1f) {
  case 0x00: return {"mfc2", rtName(), rdValue()};
  case 0x02: return {"cfc2", rtName(), cdValue()};
  case 0x04: return {"mtc2", rtValue(), rdName()};
  case 0x06: return {"ctc2", rtValue(), cdName()};
  }
  if(!(instruction >> 25 & 1)) return {};

  auto vdName  = [&] { return vpuRegisterName (instruction >>  6 & 31); };
  auto vdValue = [&] { return vpuRegisterValue(instruction >>  6 & 31); };
  auto vsName  = [&] { return vpuRegisterName (instruction >> 11 & 31); };
  auto vsValue = [&] { return vpuRegisterValue(instruction >> 11 & 31); };
  auto vtName  = [&] { return vpuRegisterName (instruction >> 16 & 31, instruction >> 21 & 15); };
  auto vtValue = [&] { return vpuRegisterValue(instruction >> 16 & 31, instruction >> 21 & 15); };
  auto vmName  = [&] { return vpuRegisterName (instruction >>  6 & 31, instruction >> 11 & 31); };
  auto vmValue = [&] { return vpuRegisterValue(instruction >>  6 & 31, instruction >> 11 & 31); };

  auto DST = [&](string_view name) -> vector<string> {
    return {name, vdName(), vsValue(), vtValue()};
  };

  auto DSE = [&](string_view name) -> vector<string> {
    static const string registerNames[] = {
      "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7",
      "acch", "accm", "accl", "r11", "r12", "r13", "r14", "r15",
    };
    return {name, vdName(), vsValue(), registerNames[instruction >> 21 & 15]};
  };

  auto DT = [&](string_view name) -> vector<string> {
    return {name, vmName(), vtValue()};
  };

  auto D = [&](string_view name) -> vector<string> {
    return {name, vdName()};
  };

  switch(instruction & 0x3f) {
  case 0x00: return DST("vmulf");
  case 0x01: return DST("vmulu");
  case 0x02: return DST("vrndp");
  case 0x03: return DST("vmulq");
  case 0x04: return DST("vmudl");
  case 0x05: return DST("vmudm");
  case 0x06: return DST("vmudn");
  case 0x07: return DST("vmudh");
  case 0x08: return DST("vmacf");
  case 0x09: return DST("vmacu");
  case 0x0a: return DST("vrndn");
  case 0x0b: return D("vmacq");
  case 0x0c: return DST("vmadl");
  case 0x0d: return DST("vmadm");
  case 0x0e: return DST("vmadn");
  case 0x0f: return DST("vmadh");
  case 0x10: return DST("vadd");
  case 0x11: return DST("vsub");
  case 0x12: break;
  case 0x13: return DST("vabs");
  case 0x14: return DST("vaddc");
  case 0x15: return DST("vsubc");
  case 0x16: break;
  case 0x17: break;
  case 0x18: break;
  case 0x19: break;
  case 0x1a: break;
  case 0x1b: break;
  case 0x1c: break;
  case 0x1d: return DSE("vsar");
  case 0x1e: break;
  case 0x1f: break;
  case 0x20: return DST("vlt");
  case 0x21: return DST("veq");
  case 0x22: return DST("vne");
  case 0x23: return DST("vge");
  case 0x24: return DST("vcl");
  case 0x25: return DST("vch");
  case 0x26: return DST("vcr");
  case 0x27: return DST("vmrg");
  case 0x28: return DST("vand");
  case 0x29: return DST("vnand");
  case 0x2a: return DST("vor");
  case 0x2b: return DST("vnor");
  case 0x2c: return DST("vxor");
  case 0x2d: return DST("vnxor");
  case 0x2e: break;
  case 0x2f: break;
  case 0x30: return DT("vrcp");
  case 0x31: return DT("vrcpl");
  case 0x32: return DT("vrcph");
  case 0x33: return DT("vmov");
  case 0x34: return DT("vrsq");
  case 0x35: return DT("vrsql");
  case 0x36: return DT("vrsqh");
  case 0x37: return {"vnop"};
  case 0x38: break;
  case 0x39: break;
  case 0x3a: break;
  case 0x3b: break;
  case 0x3c: break;
  case 0x3d: break;
  case 0x3e: break;
  case 0x3f: break;
  }

  return {};
}

auto RSP::Disassembler::immediate(s64 value, u32 bits) const -> string {
  if(value < 0) return {"-$", hex(-value, bits >> 2)};
  return {"$", hex(value, bits >> 2)};
};

auto RSP::Disassembler::ipuRegisterName(u32 index) const -> string {
  static const string registers[32] = {
     "0", "at", "v0", "v1", "a0", "a1", "a2", "a3",
    "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
    "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
    "t8", "t9", "k0", "k1", "gp", "sp", "s8", "ra",
  };
  return registers[index];
}

auto RSP::Disassembler::ipuRegisterValue(u32 index) const -> string {
  if(index && showValues) return {ipuRegisterName(index), hint("{$", hex(self.ipu.r[index].u32, 8L), "}")};
  return ipuRegisterName(index);
}

auto RSP::Disassembler::ipuRegisterIndex(u32 index, s16 offset) const -> string {
  string adjust;
  if(offset >= 0) adjust = {"+$", hex( offset)};
  if(offset <  0) adjust = {"-$", hex(-offset)};
  if(index && showValues) return {ipuRegisterName(index), adjust, hint("{$", hex(self.ipu.r[index].u32 + offset, 8L), "}")};
  return {ipuRegisterName(index), adjust};
}

auto RSP::Disassembler::sccRegisterName(u32 index) const -> string {
  static const string registers[32] = {
    "SP_PBUS_ADDRESS", "SP_DRAM_ADDRESS", "SP_READ_LENGTH", "SP_WRITE_LENGTH",
    "SP_STATUS",       "SP_DMA_FULL",     "SP_DMA_BUSY",    "SP_SEMAPHORE",
    "DPC_START",       "DPC_END",         "DPC_CURRENT",    "DPC_STATUS",
    "DPC_CLOCK",       "DPC_BUSY",        "DPC_PIPE_BUSY",  "DPC_TMEM_BUSY",
  };
  return registers[index & 15];
}

auto RSP::Disassembler::sccRegisterValue(u32 index) const -> string {
  u32 value = 0; Thread thread;
  if(index <= 6) value = rsp.readWord((index & 7) << 2, thread);
  if(index == 7) value = self.status.semaphore;  //rsp.readSCC(7) has side-effects
  if(index >= 8) value = rdp.readWord((index & 7) << 2, thread);
  if(showValues) return {sccRegisterName(index), hint("{$", hex(value, 8L), "}")};
  return sccRegisterName(index);
}

auto RSP::Disassembler::vpuRegisterName(u32 index, u32 element) const -> string {
  if(element) return {"v", index, "[", element, "]"};
  return {"v", index};
}

auto RSP::Disassembler::vpuRegisterValue(u32 index, u32 element) const -> string {
  if(showValues) {
    vector<string> elements;
    for(u32 e : range(8)) elements.append(hex(self.vpu.r[index].element(e), 4L));
    return {vpuRegisterName(index, element), hint("{$", elements.merge("|"), "}")};
  }
  return vpuRegisterName(index, element);
}

auto RSP::Disassembler::ccrRegisterName(u32 index) const -> string {
  static const string registers[32] = {"vco", "vcc", "vce"};
  if(index < 3) return registers[index];
  return {"vc", index};
}

auto RSP::Disassembler::ccrRegisterValue(u32 index) const -> string {
  if(showValues) return {ccrRegisterName(index)};  //todo
  return ccrRegisterName(index);
}

template<typename... P>
auto RSP::Disassembler::hint(P&&... p) const -> string {
  if(showColors) return {terminal::csi, "0m", terminal::csi, "37m", std::forward<P>(p)..., terminal::csi, "0m"};
  return {std::forward<P>(p)...};
}
