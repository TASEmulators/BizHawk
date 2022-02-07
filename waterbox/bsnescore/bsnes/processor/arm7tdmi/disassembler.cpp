static const string _r[] = {
  "r0", "r1",  "r2",  "r3",  "r4", "r5", "r6", "r7",
  "r8", "r9", "r10", "r11", "r12", "sp", "lr", "pc"
};
static const string _conditions[] = {
  "eq", "ne", "cs", "cc", "mi", "pl", "vs", "vc",
  "hi", "ls", "ge", "lt", "gt", "le", "",   "nv",
};
#define _s save ? "s" : ""
#define _move(mode) (mode == 13 || mode == 15)
#define _comp(mode) (mode >=  8 && mode <= 11)
#define _math(mode) (mode <=  7 || mode == 12 || mode == 14)

auto ARM7TDMI::disassemble(maybe<uint32> pc, maybe<boolean> thumb) -> string {
  if(!pc) pc = pipeline.execute.address;
  if(!thumb) thumb = cpsr().t;

  _pc = pc();
  if(!thumb()) {
    uint32 opcode = read(Word | Nonsequential, _pc & ~3);
    uint12 index = (opcode & 0x0ff00000) >> 16 | (opcode & 0x000000f0) >> 4;
    _c = _conditions[opcode >> 28];
    return {hex(_pc, 8L), "  ", armDisassemble[index](opcode)};
  } else {
    uint16 opcode = read(Half | Nonsequential, _pc & ~1);
    return {hex(_pc, 8L), "  ", thumbDisassemble[opcode]()};
  }
}

auto ARM7TDMI::disassembleRegisters() -> string {
  string output;
  for(uint n : range(16)) {
    output.append(_r[n], ":", hex(r(n), 8L), " ");
  }

  output.append("cpsr:");
  output.append(cpsr().n ? "N" : "n");
  output.append(cpsr().z ? "Z" : "z");
  output.append(cpsr().c ? "C" : "c");
  output.append(cpsr().v ? "V" : "v", "/");
  output.append(cpsr().i ? "I" : "i");
  output.append(cpsr().f ? "F" : "f");
  output.append(cpsr().t ? "T" : "t", "/");
  output.append(hex(cpsr().m, 2L));
  if(cpsr().m == PSR::USR || cpsr().m == PSR::SYS) return output;

  output.append(" spsr:");
  output.append(spsr().n ? "N" : "n");
  output.append(spsr().z ? "Z" : "z");
  output.append(spsr().c ? "C" : "c");
  output.append(spsr().v ? "V" : "v", "/");
  output.append(spsr().i ? "I" : "i");
  output.append(spsr().f ? "F" : "f");
  output.append(spsr().t ? "T" : "t", "/");
  output.append(hex(spsr().m, 2L));
  return output;
}

//

auto ARM7TDMI::armDisassembleBranch
(int24 displacement, uint1 link) -> string {
  return {"b", link ? "l" : "", _c, " 0x", hex(_pc + 8 + displacement * 4, 8L)};
}

auto ARM7TDMI::armDisassembleBranchExchangeRegister
(uint4 m) -> string {
  return {"bx", _c, " ", _r[m]};
}

auto ARM7TDMI::armDisassembleDataImmediate
(uint8 immediate, uint4 shift, uint4 d, uint4 n, uint1 save, uint4 mode) -> string {
  static const string opcode[] = {
    "and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
    "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn",
  };
  uint32 data = immediate >> (shift << 1) | immediate << 32 - (shift << 1);
  return {opcode[mode], _c,
    _move(mode) ? string{_s, " ", _r[d]} : string{},
    _comp(mode) ? string{" ", _r[n]} : string{},
    _math(mode) ? string{_s, " ", _r[d], ",", _r[n]} : string{},
    ",#0x", hex(data, 8L)};
}

auto ARM7TDMI::armDisassembleDataImmediateShift
(uint4 m, uint2 type, uint5 shift, uint4 d, uint4 n, uint1 save, uint4 mode) -> string {
  static const string opcode[] = {
    "and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
    "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn",
  };
  return {opcode[mode], _c,
    _move(mode) ? string{_s, " ", _r[d]} : string{},
    _comp(mode) ? string{" ", _r[n]} : string{},
    _math(mode) ? string{_s, " ", _r[d], ",", _r[n]} : string{},
    ",", _r[m],
    type == 0 && shift ? string{" lsl #", shift} : string{},
    type == 1 ? string{" lsr #", shift ? (uint)shift : 32} : string{},
    type == 2 ? string{" asr #", shift ? (uint)shift : 32} : string{},
    type == 3 && shift ? string{" ror #", shift} : string{},
    type == 3 && !shift ? " rrx" : ""};
}

auto ARM7TDMI::armDisassembleDataRegisterShift
(uint4 m, uint2 type, uint4 s, uint4 d, uint4 n, uint1 save, uint4 mode) -> string {
  static const string opcode[] = {
    "and", "eor", "sub", "rsb", "add", "adc", "sbc", "rsc",
    "tst", "teq", "cmp", "cmn", "orr", "mov", "bic", "mvn",
  };
  return {opcode[mode], _c,
    _move(mode) ? string{_s, " ", _r[d]} : string{},
    _comp(mode) ? string{" ", _r[n]} : string{},
    _math(mode) ? string{_s, " ", _r[d], ",", _r[n]} : string{},
    ",", _r[m], " ",
    type == 0 ? "lsl" : "",
    type == 1 ? "lsr" : "",
    type == 2 ? "asr" : "",
    type == 3 ? "ror" : "",
    " ", _r[s]};
}

auto ARM7TDMI::armDisassembleLoadImmediate
(uint8 immediate, uint1 half, uint4 d, uint4 n, uint1 writeback, uint1 up, uint1 pre) -> string {
  string data;
  if(n == 15) data = {" =0x", hex(read((half ? Half : Byte) | Nonsequential,
    _pc + 8 + (up ? +immediate : -immediate)), half ? 4L : 2L)};

  return {"ldr", _c, half ? "sh" : "sb", " ",
    _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    immediate ? string{",", up ? "+" : "-", "0x", hex(immediate, 2L)} : string{},
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : "", data};
}

auto ARM7TDMI::armDisassembleLoadRegister
(uint4 m, uint1 half, uint4 d, uint4 n, uint1 writeback, uint1 up, uint1 pre) -> string {
  return {"ldr", _c, half ? "sh" : "sb", " ",
    _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    ",", up ? "+" : "-", _r[m],
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : ""};
}

auto ARM7TDMI::armDisassembleMemorySwap
(uint4 m, uint4 d, uint4 n, uint1 byte) -> string {
  return {"swp", _c, byte ? "b" : "", " ", _r[d], ",", _r[m], ",[", _r[n], "]"};
}

auto ARM7TDMI::armDisassembleMoveHalfImmediate
(uint8 immediate, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 up, uint1 pre) -> string {
  string data;
  if(n == 15) data = {" =0x", hex(read(Half | Nonsequential, _pc + (up ? +immediate : -immediate)), 4L)};

  return {mode ? "ldr" : "str", _c, "h ",
    _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    immediate ? string{",", up ? "+" : "-", "0x", hex(immediate, 2L)} : string{},
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : "", data};
}

auto ARM7TDMI::armDisassembleMoveHalfRegister
(uint4 m, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 up, uint1 pre) -> string {
  return {mode ? "ldr" : "str", _c, "h ",
    _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    ",", up ? "+" : "-", _r[m],
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : ""};
}

auto ARM7TDMI::armDisassembleMoveImmediateOffset
(uint12 immediate, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 byte, uint1 up, uint1 pre) -> string {
  string data;
  if(n == 15) data = {" =0x", hex(read((byte ? Byte : Word) | Nonsequential,
    _pc + 8 + (up ? +immediate : -immediate)), byte ? 2L : 4L)};
  return {mode ? "ldr" : "str", _c, byte ? "b" : "", " ", _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    immediate ? string{",", up ? "+" : "-", "0x", hex(immediate, 3L)} : string{},
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : "", data};
}

auto ARM7TDMI::armDisassembleMoveMultiple
(uint16 list, uint4 n, uint1 mode, uint1 writeback, uint1 type, uint1 up, uint1 pre) -> string {
  string registers;
  for(auto index : range(16)) {
    if((list & 1 << index)) registers.append(_r[index], ",");
  }
  registers.trimRight(",", 1L);
  return {mode ? "ldm" : "stm", _c,
    up == 0 && pre == 0 ? "da" : "",
    up == 0 && pre == 1 ? "db" : "",
    up == 1 && pre == 0 ? "ia" : "",
    up == 1 && pre == 1 ? "ib" : "",
    " ", _r[n], writeback ? "!" : "",
    ",{", registers, "}", type ? "^" : ""};
}

auto ARM7TDMI::armDisassembleMoveRegisterOffset
(uint4 m, uint2 type, uint5 shift, uint4 d, uint4 n, uint1 mode, uint1 writeback, uint1 byte, uint1 up, uint1 pre) -> string {
  return {mode ? "ldr" : "str", _c, byte ? "b" : "", " ", _r[d], ",[", _r[n],
    pre == 0 ? "]" : "",
    ",", up ? "+" : "-", _r[m],
    type == 0 && shift ? string{" lsl #", shift} : string{},
    type == 1 ? string{" lsr #", shift ? (uint)shift : 32} : string{},
    type == 2 ? string{" asr #", shift ? (uint)shift : 32} : string{},
    type == 3 && shift ? string{" ror #", shift} : string{},
    type == 3 && !shift ? " rrx" : "",
    pre == 1 ? "]" : "",
    pre == 0 || writeback ? "!" : ""};
}

auto ARM7TDMI::armDisassembleMoveToRegisterFromStatus
(uint4 d, uint1 mode) -> string {
  return {"mrs", _c, " ", _r[d], ",", mode ? "spsr" : "cpsr"};
}

auto ARM7TDMI::armDisassembleMoveToStatusFromImmediate
(uint8 immediate, uint4 rotate, uint4 field, uint1 mode) -> string {
  uint32 data = immediate >> (rotate << 1) | immediate << 32 - (rotate << 1);
  return {"msr", _c, " ", mode ? "spsr:" : "cpsr:",
    field.bit(0) ? "c" : "",
    field.bit(1) ? "x" : "",
    field.bit(2) ? "s" : "",
    field.bit(3) ? "f" : "",
    ",#0x", hex(data, 8L)};
}

auto ARM7TDMI::armDisassembleMoveToStatusFromRegister
(uint4 m, uint4 field, uint1 mode) -> string {
  return {"msr", _c, " ", mode ? "spsr:" : "cpsr:",
    field.bit(0) ? "c" : "",
    field.bit(1) ? "x" : "",
    field.bit(2) ? "s" : "",
    field.bit(3) ? "f" : "",
    ",", _r[m]};
}

auto ARM7TDMI::armDisassembleMultiply
(uint4 m, uint4 s, uint4 n, uint4 d, uint1 save, uint1 accumulate) -> string {
  if(accumulate) {
    return {"mla", _c, _s, " ", _r[d], ",", _r[m], ",", _r[s], ",", _r[n]};
  } else {
    return {"mul", _c, _s, " ", _r[d], ",", _r[m], ",", _r[s]};
  }
}

auto ARM7TDMI::armDisassembleMultiplyLong
(uint4 m, uint4 s, uint4 l, uint4 h, uint1 save, uint1 accumulate, uint1 sign) -> string {
  return {sign ? "s" : "u", accumulate ? "mlal" : "mull", _c, _s, " ",
    _r[l], ",", _r[h], ",", _r[m], ",", _r[s]};
}

auto ARM7TDMI::armDisassembleSoftwareInterrupt
(uint24 immediate) -> string {
  return {"swi #0x", hex(immediate, 6L)};
}

auto ARM7TDMI::armDisassembleUndefined
() -> string {
  return {"undefined"};
}

//

auto ARM7TDMI::thumbDisassembleALU
(uint3 d, uint3 m, uint4 mode) -> string {
  static const string opcode[] = {
    "and", "eor", "lsl", "lsr", "asr", "adc", "sbc", "ror",
    "tst", "neg", "cmp", "cmn", "orr", "mul", "bic", "mvn",
  };
  return {opcode[mode], " ", _r[d], ",", _r[m]};
}

auto ARM7TDMI::thumbDisassembleALUExtended
(uint4 d, uint4 m, uint2 mode) -> string {
  static const string opcode[] = {"add", "sub", "mov"};
  if(d == 8 && m == 8 && mode == 2) return {"nop"};
  return {opcode[mode], " ", _r[d], ",", _r[m]};
}

auto ARM7TDMI::thumbDisassembleAddRegister
(uint8 immediate, uint3 d, uint1 mode) -> string {
  return {"add ", _r[d], ",", mode ? "sp" : "pc", ",#0x", hex(immediate, 2L)};
}

auto ARM7TDMI::thumbDisassembleAdjustImmediate
(uint3 d, uint3 n, uint3 immediate, uint1 mode) -> string {
  return {!mode ? "add" : "sub", " ", _r[d], ",", _r[n], ",#", immediate};
}

auto ARM7TDMI::thumbDisassembleAdjustRegister
(uint3 d, uint3 n, uint3 m, uint1 mode) -> string {
  return {!mode ? "add" : "sub", " ", _r[d], ",", _r[n], ",", _r[m]};
}

auto ARM7TDMI::thumbDisassembleAdjustStack
(uint7 immediate, uint1 mode) -> string {
  return {!mode ? "add" : "sub", " sp,#0x", hex(immediate * 4, 3L)};
}

auto ARM7TDMI::thumbDisassembleBranchExchange
(uint4 m) -> string {
  return {"bx ", _r[m]};
}

auto ARM7TDMI::thumbDisassembleBranchFarPrefix
(int11 displacementHi) -> string {
  uint11 displacementLo = read(Half | Nonsequential, (_pc & ~1) + 2);
  int22 displacement = displacementHi << 11 | displacementLo << 0;
  uint32 address = _pc + 4 + displacement * 2;
  return {"bl 0x", hex(address, 8L)};
}

auto ARM7TDMI::thumbDisassembleBranchFarSuffix
(uint11 displacement) -> string {
  return {"bl (suffix)"};
}

auto ARM7TDMI::thumbDisassembleBranchNear
(int11 displacement) -> string {
  uint32 address = _pc + 4 + displacement * 2;
  return {"b 0x", hex(address, 8L)};
}

auto ARM7TDMI::thumbDisassembleBranchTest
(int8 displacement, uint4 condition) -> string {
  uint32 address = _pc + 4 + displacement * 2;
  return {"b", _conditions[condition], " 0x", hex(address, 8L)};
}

auto ARM7TDMI::thumbDisassembleImmediate
(uint8 immediate, uint3 d, uint2 mode) -> string {
  static const string opcode[] = {"mov", "cmp", "add", "sub"};
  return {opcode[mode], " ", _r[d], ",#0x", hex(immediate, 2L)};
}

auto ARM7TDMI::thumbDisassembleLoadLiteral
(uint8 displacement, uint3 d) -> string {
  uint32 address = ((_pc + 4) & ~3) + (displacement << 2);
  uint32 data = read(Word | Nonsequential, address);
  return {"ldr ", _r[d], ",[pc,#0x", hex(address, 8L), "] =0x", hex(data, 8L)};
}

auto ARM7TDMI::thumbDisassembleMoveByteImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> string {
  return {mode ? "ldrb" : "strb", " ", _r[d], ",[", _r[n], ",#0x", hex(offset, 2L), "]"};
}

auto ARM7TDMI::thumbDisassembleMoveHalfImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> string {
  return {mode ? "ldrh" : "strh", " ", _r[d], ",[", _r[n], ",#0x", hex(offset * 2, 2L), "]"};
}

auto ARM7TDMI::thumbDisassembleMoveMultiple
(uint8 list, uint3 n, uint1 mode) -> string {
  string registers;
  for(uint m : range(8)) {
    if((list & 1 << m)) registers.append(_r[m], ",");
  }
  registers.trimRight(",", 1L);
  return {mode ? "ldmia" : "stmia", " ", _r[n], "!,{", registers, "}"};
}

auto ARM7TDMI::thumbDisassembleMoveRegisterOffset
(uint3 d, uint3 n, uint3 m, uint3 mode) -> string {
  static const string opcode[] = {"str", "strh", "strb", "ldsb", "ldr", "ldrh", "ldrb", "ldsh"};
  return {opcode[mode], " ", _r[d], ",[", _r[n], ",", _r[m], "]"};
}

auto ARM7TDMI::thumbDisassembleMoveStack
(uint8 immediate, uint3 d, uint1 mode) -> string {
  return {mode ? "ldr" : "str", " ", _r[d], ",[sp,#0x", hex(immediate * 4, 3L), "]"};
}

auto ARM7TDMI::thumbDisassembleMoveWordImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> string {
  return {mode ? "ldr" : "str", " ", _r[d], ",[", _r[n], ",#0x", hex(offset * 4, 2L), "]"};
}

auto ARM7TDMI::thumbDisassembleShiftImmediate
(uint3 d, uint3 m, uint5 immediate, uint2 mode) -> string {
  static const string opcode[] = {"lsl", "lsr", "asr"};
  return {opcode[mode], " ", _r[d], ",", _r[m], ",#", immediate};
}

auto ARM7TDMI::thumbDisassembleSoftwareInterrupt
(uint8 immediate) -> string {
  return {"swi #0x", hex(immediate, 2L)};
}

auto ARM7TDMI::thumbDisassembleStackMultiple
(uint8 list, uint1 lrpc, uint1 mode) -> string {
  string registers;
  for(uint m : range(8)) {
    if((list & 1 << m)) registers.append(_r[m], ",");
  }
  if(lrpc) registers.append(!mode ? "lr," : "pc,");
  registers.trimRight(",", 1L);
  return {!mode ? "push" : "pop", " {", registers, "}"};
}

auto ARM7TDMI::thumbDisassembleUndefined
() -> string {
  return {"undefined"};
}

#undef _s
#undef _move
#undef _comp
#undef _save
