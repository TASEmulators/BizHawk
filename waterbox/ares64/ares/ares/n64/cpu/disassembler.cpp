auto CPU::Disassembler::disassemble(u32 address, u32 instruction) -> string {
  this->address = address;
  this->instruction = instruction;

  auto v = EXECUTE();
  if(!v) v.append("invalid", string{"$", hex(instruction, 8L)});
  if(!instruction) v = {"nop"};
  auto s = pad(v.takeFirst(), -8L);
  return {s, v.merge(",")};
}

auto CPU::Disassembler::EXECUTE() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto rsValue = [&] { return ipuRegisterValue(instruction >> 21 & 31); };
  auto ftName  = [&] { return fpuRegisterName (instruction >> 16 & 31); };
  auto ftValue = [&] { return fpuRegisterValue(instruction >> 16 & 31); };
  auto imm16i  = [&] { return immediate(s16(instruction)); };
  auto imm16u  = [&] { return immediate(u16(instruction), 16L); };
  auto jump    = [&] { return immediate(address + 4 & 0xf000'0000 | (instruction & 0x03ff'ffff) << 2); };
  auto branch  = [&] { return immediate(address + 4 + (s16(instruction) << 2)); };
  auto offset  = [&] { return ipuRegisterIndex(instruction >> 21 & 31, s16(instruction)); };

  auto ALU = [&](string_view name) -> vector<string> {
    return {name, rtName(), rsValue(), immediate(u16(instruction))};
  };

  auto ADDI = [&](string_view add, string_view sub, string_view mov) -> vector<string> {
    if(!(instruction >> 21 & 31)) return {mov, rtName(), immediate(s16(instruction), 32L)};
    return {s16(instruction) >= 0 ? add : sub, rtName(), rsValue(), immediate(abs(s16(instruction)))};
  };

  auto BRANCH1 = [&](string_view name) -> vector<string> {
    return {name, rsValue(), branch()};
  };

  auto BRANCH2 = [&](string_view name) -> vector<string> {
    return {name, rsValue(), rtValue(), branch()};
  };

  auto CACHE = [&](string_view name) -> vector<string> {
    auto operation = instruction >> 16 & 31;
    string type = "reserved";
    switch(operation) {
    case 0x00: type = "code(IndexInvalidate)"; break;
    case 0x04: type = "code(IndexLoadTag)"; break;
    case 0x08: type = "code(IndexStoreTag)"; break;
    case 0x10: type = "code(HitInvalidate)"; break;
    case 0x14: type = "code(Fill)"; break;
    case 0x18: type = "code(HitWriteBack)"; break;
    case 0x01: type = "data(IndexWriteBackInvalidate)"; break;
    case 0x05: type = "data(IndexLoadTag)"; break;
    case 0x09: type = "data(IndexStoreTag)"; break;
    case 0x0d: type = "data(CreateDirtyExclusive)"; break;
    case 0x11: type = "data(HitInvalidate)"; break;
    case 0x15: type = "data(HitWriteBackInvalidate)"; break;
    case 0x19: type = "data(HitWriteBack)"; break;
    default:   type ={"reserved(0x", hex(operation, 2L), ")"}; break;
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
  case 0x11: return FPU();
  case 0x12: break;  //COP2
  case 0x13: break;  //COP3
  case 0x14: return BRANCH2("beql");
  case 0x15: return BRANCH2("bnel");
  case 0x16: return BRANCH1("blezl");
  case 0x17: return BRANCH1("bgtzl");
  case 0x18: return ADDI("daddi",  "dsubi",  "dli");
  case 0x19: return ADDI("daddiu", "dsubiu", "dliu");
  case 0x1a: return LOAD("ldl");
  case 0x1b: return LOAD("ldr");
  case 0x1c: break;
  case 0x1d: break;
  case 0x1e: break;
  case 0x1f: break;
  case 0x20: return LOAD("lb");
  case 0x21: return LOAD("lh");
  case 0x22: return LOAD("lwl");
  case 0x23: return LOAD("lw");
  case 0x24: return LOAD("lbu");
  case 0x25: return LOAD("lhu");
  case 0x26: return LOAD("lwr");
  case 0x27: return LOAD("lwu");
  case 0x28: return STORE("sb");
  case 0x29: return STORE("sh");
  case 0x2a: return STORE("swl");
  case 0x2b: return STORE("sw");
  case 0x2c: return STORE("sdl");
  case 0x2d: return STORE("sdr");
  case 0x2e: return STORE("swr");
  case 0x2f: return CACHE("cache");
  case 0x30: return LOAD("ll");
  case 0x31: return {"lwc1", ftName(), offset()};
  case 0x32: break;  //LWC2
  case 0x33: break;  //LWC3
  case 0x34: return LOAD("lld");
  case 0x35: return {"ldc1", ftName(), offset()};
  case 0x36: break;  //LDC2
  case 0x37: return LOAD("ld");
  case 0x38: return STORE("sc");
  case 0x39: return {"swc1", ftValue(), offset()};
  case 0x3a: break;  //SWC2
  case 0x3b: break;  //SWC3
  case 0x3c: return STORE("scd");
  case 0x3d: return {"sdc1", ftValue(), offset()};
  case 0x3e: break;  //SDC2
  case 0x3f: return STORE("sd");
  }
  return {};
}

auto CPU::Disassembler::SPECIAL() -> vector<string> {
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

  auto ST = [&](string_view name) -> vector<string> {
    return {name, rsValue(), rtValue()};
  };

  switch(instruction & 0x3f) {
  case 0x00: return ALU("sll", shift());
  case 0x01: break;
  case 0x02: return ALU("srl", shift());
  case 0x03: return ALU("sra", shift());
  case 0x04: return ALU("sllv", rsValue());
  case 0x05: break;
  case 0x06: return ALU("srlv", rsValue());
  case 0x07: return ALU("srav", rsValue());
  case 0x08: return {"jr", rsValue()};
  case 0x09: return JALR("jalr");
  case 0x0a: break;
  case 0x0b: break;
  case 0x0c: return {"syscall"};
  case 0x0d: return {"break"};
  case 0x0e: break;
  case 0x0f: return {"sync"};
  case 0x10: return {"mfhi", rdName(), {"hi", hint("{$", hex(self.ipu.hi.u64, 8L), "}")}};
  case 0x11: return {"mthi", rsValue(), "hi"};
  case 0x12: return {"mflo", rdName(), {"lo", hint("{$", hex(self.ipu.lo.u64, 8L), "}")}};
  case 0x13: return {"mtlo", rsValue(), "lo"};
  case 0x14: return ALU("dsllv", rsValue());
  case 0x15: break;
  case 0x16: return ALU("dsrlv", rsValue());
  case 0x17: return ALU("dsrav", rsValue());
  case 0x18: return ST("mult");
  case 0x19: return ST("multu");
  case 0x1a: return ST("div");
  case 0x1b: return ST("divu");
  case 0x1c: return ST("dmult");
  case 0x1d: return ST("dmultu");
  case 0x1e: return ST("ddiv");
  case 0x1f: return ST("ddivu");
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
  case 0x2c: return REG("dadd");
  case 0x2d: return REG("daddu");
  case 0x2e: return REG("dsub");
  case 0x2f: return REG("dsubu");
  case 0x30: return ST("tge");
  case 0x31: return ST("tgeu");
  case 0x32: return ST("tlt");
  case 0x33: return ST("tltu");
  case 0x34: return ST("teq");
  case 0x35: break;
  case 0x36: return ST("tne");
  case 0x37: break;
  case 0x38: return ALU("dsll", shift());
  case 0x39: break;
  case 0x3a: return ALU("dsrl", shift());
  case 0x3b: return ALU("dsra", shift());
  case 0x3c: return ALU("dsll32", shift());
  case 0x3d: break;
  case 0x3e: return ALU("dsrl32", shift());
  case 0x3f: return ALU("dsra32", shift());
  }

  return {};
}

auto CPU::Disassembler::REGIMM() -> vector<string> {
  auto rsValue = [&] { return ipuRegisterValue(instruction >> 21 & 31); };
  auto imm16i  = [&] { return immediate(s16(instruction)); };
  auto branch  = [&] { return immediate(address + 4 + (s16(instruction) << 2)); };

  auto BRANCH = [&](string_view name) -> vector<string> {
    return {name, rsValue(), branch()};
  };

  auto TRAP = [&](string_view name) -> vector<string> {
    return {name, rsValue(), imm16i()};
  };

  switch(instruction >> 16 & 0x1f) {
  case 0x00: return BRANCH("bltz");
  case 0x01: return BRANCH("bgez");
  case 0x02: return BRANCH("bltzl");
  case 0x03: return BRANCH("bgezl");
  case 0x04: break;
  case 0x05: break;
  case 0x06: break;
  case 0x07: break;
  case 0x08: return TRAP("tgei");
  case 0x09: return TRAP("tgeiu");
  case 0x0a: return TRAP("tlti");
  case 0x0b: return TRAP("tltiu");
  case 0x0c: return TRAP("teqi");
  case 0x0d: break;
  case 0x0e: return TRAP("tnei");
  case 0x0f: break;
  case 0x10: return BRANCH("bltzal");
  case 0x11: return BRANCH("bgezal");
  case 0x12: return BRANCH("bltzall");
  case 0x13: return BRANCH("bgezall");
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

auto CPU::Disassembler::SCC() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto sdName  = [&] { return sccRegisterName (instruction >> 11 & 31); };
  auto sdValue = [&] { return sccRegisterValue(instruction >> 11 & 31); };
  auto branch  = [&] { return immediate(address + 4 + (s16(instruction) << 2)); };

  switch(instruction >> 21 & 0x1f) {
  case 0x00: return {"mfc0",  rtName(), sdValue()};
  case 0x01: return {"dmfc0", rtName(), sdValue()};
  case 0x02: break;  //CFC0
  case 0x04: return {"mtc0",  rtValue(), sdName()};
  case 0x05: return {"dmtc0", rtValue(), sdName()};
  case 0x06: break;  //CTC0
  case 0x08: break;  //BC0
  }
  if(!(instruction >> 25 & 1)) return {};
  switch(instruction & 0x3f) {
  case 0x01: return {"tlbr"};
  case 0x02: return {"tlbwi"};
  case 0x06: return {"tlbwr"};
  case 0x08: return {"tlbp"};
  case 0x18: return {"eret"};
  }

  return {};
}

auto CPU::Disassembler::FPU() -> vector<string> {
  auto rtName  = [&] { return ipuRegisterName (instruction >> 16 & 31); };
  auto rtValue = [&] { return ipuRegisterValue(instruction >> 16 & 31); };
  auto rdName  = [&] { return fpuRegisterName (instruction >> 11 & 31); };
  auto rdValue = [&] { return fpuRegisterValue(instruction >> 11 & 31); };
  auto cdName  = [&] { return ccrRegisterName (instruction >> 11 & 31); };
  auto cdValue = [&] { return ccrRegisterValue(instruction >> 11 & 31); };  //todo
  auto branch  = [&] { return immediate(address + 4 + (s16(instruction) << 2)); };

  switch(instruction >> 21 & 0x1f) {
  case 0x00: return {"mfc1",  rtName(), rdValue()};
  case 0x01: return {"dmfc1", rtName(), rdValue()};
  case 0x02: return {"cfc1",  rtName(), cdValue()};
  case 0x04: return {"mtc1",  rtValue(), rdName()};
  case 0x05: return {"dmtc1", rtValue(), rdName()};
  case 0x06: return {"ctc1",  rtValue(), cdName()};
  case 0x08: switch(instruction >> 16 & 3) {
    case 0x00: return {"bc1f",  branch()};
    case 0x01: return {"bc1t",  branch()};
    case 0x02: return {"bc1fl", branch()};
    case 0x03: return {"bc1tl", branch()};
    }
  }
  if(!(instruction >> 25 & 1)) return {};

  auto fdName  = [&] { return fpuRegisterName (instruction >>  6 & 31); };
  auto fsValue = [&] { return fpuRegisterValue(instruction >> 11 & 31); };
  auto ftValue = [&] { return fpuRegisterValue(instruction >> 16 & 31); };

  auto DS = [&](string_view name) -> vector<string> {
    return {name, fdName(), fsValue()};
  };

  auto DST = [&](string_view name) -> vector<string> {
    return {name, fdName(), fsValue(), ftValue()};
  };

  auto ST = [&](string_view name) -> vector<string> {
    return {name, fsValue(), ftValue()};
  };

  bool s = (instruction & 1 << 21) == 0;
  bool i = (instruction & 1 << 23) != 0;

  switch(instruction & 0x3f) {
  case 0x00: return DST(s ? "adds"    : "addd"   );
  case 0x01: return DST(s ? "subs"    : "subd"   );
  case 0x02: return DST(s ? "muls"    : "muld"   );
  case 0x03: return DST(s ? "divs"    : "divd"   );
  case 0x04: return DS (s ? "sqrts"   : "sqrtd"  );
  case 0x05: return DS (s ? "abss"    : "absd"   );
  case 0x06: return DS (s ? "movs"    : "movd"   );
  case 0x07: return DS (s ? "negs"    : "negd"   );
  case 0x08: return DS (s ? "roundls" : "roundld");
  case 0x09: return DS (s ? "truncls" : "truncld");
  case 0x0a: return DS (s ? "ceills"  : "ceilld" );
  case 0x0b: return DS (s ? "floorls" : "floorld");
  case 0x0c: return DS (s ? "roundws" : "roundwd");
  case 0x0d: return DS (s ? "truncws" : "truncwd");
  case 0x0e: return DS (s ? "ceilws"  : "ceilwd" );
  case 0x0f: return DS (s ? "floorws" : "floorwd");
  case 0x20: return DS (i ? (s ? "cvtsw" : "cvtsl") : "cvtsd");
  case 0x21: return DS (i ? (s ? "cvtdw" : "cvtdl") : "cvtds");
  case 0x24: return DS (s ? "cvtws" : "cvtwd" );
  case 0x25: return DS (s ? "cvtls" : "cvtld" );
  case 0x30: return ST(s ? "cfs"    : "cfd"   );
  case 0x31: return ST(s ? "cuns"   : "cund"  );
  case 0x32: return ST(s ? "ceqs"   : "ceqd"  );
  case 0x33: return ST(s ? "cueqs"  : "cueqd" );
  case 0x34: return ST(s ? "colts"  : "coltd" );
  case 0x35: return ST(s ? "cults"  : "cultd" );
  case 0x36: return ST(s ? "coles"  : "coled" );
  case 0x37: return ST(s ? "cules"  : "culed" );
  case 0x38: return ST(s ? "csfs"   : "csfd"  );
  case 0x39: return ST(s ? "cngles" : "cngled");
  case 0x3a: return ST(s ? "cseqs"  : "cseqd" );
  case 0x3b: return ST(s ? "cngls"  : "cngld" );
  case 0x3c: return ST(s ? "clts"   : "cltd"  );
  case 0x3d: return ST(s ? "cnges"  : "cnged" );
  case 0x3e: return ST(s ? "cles"   : "cled"  );
  case 0x3f: return ST(s ? "cngts"  : "cngtd" );
  }

  return {};
}

auto CPU::Disassembler::immediate(s64 value, u32 bits) const -> string {
  if(value < 0) return {"-$", hex(-value, bits >> 2)};
  return {"$", hex(value, bits >> 2)};
};

auto CPU::Disassembler::ipuRegisterName(u32 index) const -> string {
  static const string registers[32] = {
     "0", "at", "v0", "v1", "a0", "a1", "a2", "a3",
    "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
    "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
    "t8", "t9", "k0", "k1", "gp", "sp", "s8", "ra",
  };
  return registers[index];
}

auto CPU::Disassembler::ipuRegisterValue(u32 index) const -> string {
  if(index && showValues) return {ipuRegisterName(index), hint("{$", hex(self.ipu.r[index].u64, 8L), "}")};
  return ipuRegisterName(index);
}

auto CPU::Disassembler::ipuRegisterIndex(u32 index, s16 offset) const -> string {
  string adjust;
  if(offset >= 0) adjust = {"+$", hex( offset)};
  if(offset <  0) adjust = {"-$", hex(-offset)};
  if(index && showValues) return {ipuRegisterName(index), adjust, hint("{$", hex(self.ipu.r[index].u64 + offset, 8L), "}")};
  return {ipuRegisterName(index), adjust};
}

auto CPU::Disassembler::sccRegisterName(u32 index) const -> string {
  static const string registers[32] = {
    "Index",    "Random",   "EntryLo0",    "EntryLo1",
    "Context",  "PageMask", "Wired",       "Unused7",
    "BadVAddr", "Count",    "EntryHi",     "Compare",
    "Status",   "Cause",    "EPC",         "PrID",
    "Config",   "LLAddr",   "WatchLo",     "WatchHi",
    "XContext", "Unused21", "Unused22",    "Unused23",
    "Unused24", "Unused25", "ParityError", "CacheError",
    "TagLo",    "TagHi",    "ErrorEPC",    "Unused31",
  };
  return registers[index];
}

auto CPU::Disassembler::sccRegisterValue(u32 index) const -> string {
  if(showValues) return {sccRegisterName(index), hint("{$", hex(self.getControlRegister(index), 8L), "}")};
  return sccRegisterName(index);
}

auto CPU::Disassembler::fpuRegisterName(u32 index) const -> string {
  static const string registers[32] = {
     "f0",  "f1",  "f2",  "f3",  "f4",  "f5",  "f6",  "f7",
     "f8",  "f9", "f10", "f11", "f12", "f13", "f14", "f15",
    "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23",
    "f24", "f25", "f26", "f27", "f28", "f29", "f30", "f31",
  };
  return registers[index];
}

auto CPU::Disassembler::fpuRegisterValue(u32 index) const -> string {
  bool f32 = (instruction & 1 << 21) == 0;
  bool f64 = (instruction & 1 << 21) != 0;
  if(f32 && showValues) return {fpuRegisterName(index), hint("{", self.fpu.r[index].f32, "}")};
  if(f64 && showValues) return {fpuRegisterName(index), hint("{", self.fpu.r[index].f64, "}")};
  return fpuRegisterName(index);
}

auto CPU::Disassembler::ccrRegisterName(u32 index) const -> string {
  return {"ccr", index};
}

auto CPU::Disassembler::ccrRegisterValue(u32 index) const -> string {
  if(showValues) return {ccrRegisterName(index), hint("{$", hex(self.getControlRegisterFPU(index)), "}")};
  return ccrRegisterName(index);
}

template<typename... P>
auto CPU::Disassembler::hint(P&&... p) const -> string {
  if(showColors) return {"\e[0m\e[37m", forward<P>(p)..., "\e[0m"};
  return {forward<P>(p)...};
}
