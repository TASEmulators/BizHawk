auto ARM7TDMI::fetch() -> void {
  pipeline.execute = pipeline.decode;
  pipeline.decode = pipeline.fetch;
  pipeline.decode.thumb = cpsr().t;

  uint sequential = Sequential;
  if(pipeline.nonsequential) {
    pipeline.nonsequential = false;
    sequential = Nonsequential;
  }

  uint mask = !cpsr().t ? 3 : 1;
  uint size = !cpsr().t ? Word : Half;

  r(15).data += size >> 3;
  pipeline.fetch.address = r(15) & ~mask;
  pipeline.fetch.instruction = read(Prefetch | size | sequential, pipeline.fetch.address);
}

auto ARM7TDMI::instruction() -> void {
  uint mask = !cpsr().t ? 3 : 1;
  uint size = !cpsr().t ? Word : Half;

  if(pipeline.reload) {
    pipeline.reload = false;
    r(15).data &= ~mask;
    pipeline.fetch.address = r(15) & ~mask;
    pipeline.fetch.instruction = read(Prefetch | size | Nonsequential, pipeline.fetch.address);
    fetch();
  }
  fetch();

  if(irq && !cpsr().i) {
    exception(PSR::IRQ, 0x18);
    if(pipeline.execute.thumb) r(14).data += 2;
    return;
  }

  opcode = pipeline.execute.instruction;
  if(!pipeline.execute.thumb) {
    if(!TST(opcode >> 28)) return;
    uint12 index = (opcode & 0x0ff00000) >> 16 | (opcode & 0x000000f0) >> 4;
    armInstruction[index](opcode);
  } else {
    thumbInstruction[(uint16)opcode]();
  }
}

auto ARM7TDMI::exception(uint mode, uint32 address) -> void {
  auto psr = cpsr();
  cpsr().m = mode;
  spsr() = psr;
  cpsr().t = 0;
  if(cpsr().m == PSR::FIQ) cpsr().f = 1;
  cpsr().i = 1;
  r(14) = pipeline.decode.address;
  r(15) = address;
}

auto ARM7TDMI::armInitialize() -> void {
  #define bind(id, name, ...) { \
    uint index = (id & 0x0ff00000) >> 16 | (id & 0x000000f0) >> 4; \
    assert(!armInstruction[index]); \
    armInstruction[index] = [&](uint32 opcode) { return armInstruction##name(arguments); }; \
    armDisassemble[index] = [&](uint32 opcode) { return armDisassemble##name(arguments); }; \
  }

  #define pattern(s) \
    std::integral_constant<uint32_t, bit::test(s)>::value

  #define bit1(value, index) (value >> index & 1)
  #define bits(value, lo, hi) (value >> lo & (1ull << (hi - lo + 1)) - 1)

  #define arguments \
    bits(opcode, 0,23),  /* displacement */ \
    bit1(opcode,24)      /* link */
  for(uint4 displacementLo : range(16))
  for(uint4 displacementHi : range(16))
  for(uint1 link : range(2)) {
    auto opcode = pattern(".... 101? ???? ???? ???? ???? ???? ????")
                | displacementLo << 4 | displacementHi << 20 | link << 24;
    bind(opcode, Branch);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3)   /* m */
  {
    auto opcode = pattern(".... 0001 0010 ---- ---- ---- 0001 ????");
    bind(opcode, BranchExchangeRegister);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 7),  /* immediate */ \
    bits(opcode, 8,11),  /* shift */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* save */ \
    bits(opcode,21,24)   /* mode */
  for(uint4 shiftHi : range(16))
  for(uint1 save : range(2))
  for(uint4 mode : range(16)) {
    if(mode >= 8 && mode <= 11 && !save) continue;  //TST, TEQ, CMP, CMN
    auto opcode = pattern(".... 001? ???? ???? ???? ???? ???? ????") | shiftHi << 4 | save << 20 | mode << 21;
    bind(opcode, DataImmediate);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode, 5, 6),  /* type */ \
    bits(opcode, 7,11),  /* shift */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* save */ \
    bits(opcode,21,24)   /* mode */
  for(uint2 type : range(4))
  for(uint1 shiftLo : range(2))
  for(uint1 save : range(2))
  for(uint4 mode : range(16)) {
    if(mode >= 8 && mode <= 11 && !save) continue;  //TST, TEQ, CMP, CMN
    auto opcode = pattern(".... 000? ???? ???? ???? ???? ???0 ????") | type << 5 | shiftLo << 7 | save << 20 | mode << 21;
    bind(opcode, DataImmediateShift);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode, 5, 6),  /* type */ \
    bits(opcode, 8,11),  /* s */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* save */ \
    bits(opcode,21,24)   /* mode */
  for(uint2 type : range(4))
  for(uint1 save : range(2))
  for(uint4 mode : range(16)) {
    if(mode >= 8 && mode <= 11 && !save) continue;  //TST, TEQ, CMP, CMN
    auto opcode = pattern(".... 000? ???? ???? ???? ???? 0??1 ????") | type << 5 | save << 20 | mode << 21;
    bind(opcode, DataRegisterShift);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3) << 0 | bits(opcode, 8,11) << 4,  /* immediate */ \
    bit1(opcode, 5),     /* half */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint1 half : range(2))
  for(uint1 writeback : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 000? ?1?1 ???? ???? ???? 11?1 ????") | half << 5 | writeback << 21 | up << 23 | pre << 24;
    bind(opcode, LoadImmediate);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bit1(opcode, 5),     /* half */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint1 half : range(2))
  for(uint1 writeback : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 000? ?0?1 ???? ???? ---- 11?1 ????") | half << 5 | writeback << 21 | up << 23 | pre << 24;
    bind(opcode, LoadRegister);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,22)      /* byte */
  for(uint1 byte : range(2)) {
    auto opcode = pattern(".... 0001 0?00 ???? ???? ---- 1001 ????") | byte << 22;
    bind(opcode, MemorySwap);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3) << 0 | bits(opcode, 8,11) << 4,  /* immediate */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* mode */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint1 mode : range(2))
  for(uint1 writeback : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 000? ?1?? ???? ???? ???? 1011 ????") | mode << 20 | writeback << 21 | up << 23 | pre << 24;
    bind(opcode, MoveHalfImmediate);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* mode */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint1 mode : range(2))
  for(uint1 writeback : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 000? ?0?? ???? ???? ---- 1011 ????") | mode << 20 | writeback << 21 | up << 23 | pre << 24;
    bind(opcode, MoveHalfRegister);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0,11),  /* immediate */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* mode */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,22),     /* byte */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint4 immediatePart : range(16))
  for(uint1 mode : range(2))
  for(uint1 writeback : range(2))
  for(uint1 byte : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 010? ???? ???? ???? ???? ???? ????")
                | immediatePart << 4 | mode << 20 | writeback << 21 | byte << 22 | up << 23 | pre << 24;
    bind(opcode, MoveImmediateOffset);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0,15),  /* list */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* mode */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,22),     /* type */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint4 listPart : range(16))
  for(uint1 mode : range(2))
  for(uint1 writeback : range(2))
  for(uint1 type : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 100? ???? ???? ???? ???? ???? ????")
                | listPart << 4 | mode << 20 | writeback << 21 | type << 22 | up << 23 | pre << 24;
    bind(opcode, MoveMultiple);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode, 5, 6),  /* type */ \
    bits(opcode, 7,11),  /* shift */ \
    bits(opcode,12,15),  /* d */ \
    bits(opcode,16,19),  /* n */ \
    bit1(opcode,20),     /* mode */ \
    bit1(opcode,21),     /* writeback */ \
    bit1(opcode,22),     /* byte */ \
    bit1(opcode,23),     /* up */ \
    bit1(opcode,24)      /* pre */
  for(uint2 type : range(4))
  for(uint1 shiftLo : range(2))
  for(uint1 mode : range(2))
  for(uint1 writeback : range(2))
  for(uint1 byte : range(2))
  for(uint1 up : range(2))
  for(uint1 pre : range(2)) {
    auto opcode = pattern(".... 011? ???? ???? ???? ???? ???0 ????")
                | type << 5 | shiftLo << 7 | mode << 20 | writeback << 21 | byte << 22 | up << 23 | pre << 24;
    bind(opcode, MoveRegisterOffset);
  }
  #undef arguments

  #define arguments \
    bits(opcode,12,15),  /* d */ \
    bit1(opcode,22)      /* mode */
  for(uint1 mode : range(2)) {
    auto opcode = pattern(".... 0001 0?00 ---- ???? ---- 0000 ----") | mode << 22;
    bind(opcode, MoveToRegisterFromStatus);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 7),  /* immediate */ \
    bits(opcode, 8,11),  /* rotate */ \
    bits(opcode,16,19),  /* field */ \
    bit1(opcode,22)      /* mode */
  for(uint4 immediateHi : range(16))
  for(uint1 mode : range(2)) {
    auto opcode = pattern(".... 0011 0?10 ???? ---- ???? ???? ????") | immediateHi << 4 | mode << 22;
    bind(opcode, MoveToStatusFromImmediate);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode,16,19),  /* field */ \
    bit1(opcode,22)      /* mode */
  for(uint1 mode : range(2)) {
    auto opcode = pattern(".... 0001 0?10 ???? ---- ---- 0000 ????") | mode << 22;
    bind(opcode, MoveToStatusFromRegister);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode, 8,11),  /* s */ \
    bits(opcode,12,15),  /* n */ \
    bits(opcode,16,19),  /* d */ \
    bit1(opcode,20),     /* save */ \
    bit1(opcode,21)      /* accumulate */
  for(uint1 save : range(2))
  for(uint1 accumulate : range(2)) {
    auto opcode = pattern(".... 0000 00?? ???? ???? ???? 1001 ????") | save << 20 | accumulate << 21;
    bind(opcode, Multiply);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0, 3),  /* m */ \
    bits(opcode, 8,11),  /* s */ \
    bits(opcode,12,15),  /* l */ \
    bits(opcode,16,19),  /* h */ \
    bit1(opcode,20),     /* save */ \
    bit1(opcode,21),     /* accumulate */ \
    bit1(opcode,22)      /* sign */
  for(uint1 save : range(2))
  for(uint1 accumulate : range(2))
  for(uint1 sign : range(2)) {
    auto opcode = pattern(".... 0000 1??? ???? ???? ???? 1001 ????") | save << 20 | accumulate << 21 | sign << 22;
    bind(opcode, MultiplyLong);
  }
  #undef arguments

  #define arguments \
    bits(opcode, 0,23)  /* immediate */
  for(uint4 immediateLo : range(16))
  for(uint4 immediateHi : range(16)) {
    auto opcode = pattern(".... 1111 ???? ???? ???? ???? ???? ????") | immediateLo << 4 | immediateHi << 20;
    bind(opcode, SoftwareInterrupt);
  }
  #undef arguments

  #define arguments
  for(uint12 id : range(4096)) {
    if(armInstruction[id]) continue;
    auto opcode = pattern(".... ???? ???? ---- ---- ---- ???? ----") | bits(id,0,3) << 4 | bits(id,4,11) << 20;
    bind(opcode, Undefined);
  }
  #undef arguments

  #undef bind
  #undef pattern
}

auto ARM7TDMI::thumbInitialize() -> void {
  #define bind(id, name, ...) { \
    assert(!thumbInstruction[id]); \
    thumbInstruction[id] = [=] { return thumbInstruction##name(__VA_ARGS__); }; \
    thumbDisassemble[id] = [=] { return thumbDisassemble##name(__VA_ARGS__); }; \
  }

  #define pattern(s) \
    std::integral_constant<uint16_t, bit::test(s)>::value

  for(uint3 d : range(8))
  for(uint3 m : range(8))
  for(uint4 mode : range(16)) {
    auto opcode = pattern("0100 00?? ???? ????") | d << 0 | m << 3 | mode << 6;
    bind(opcode, ALU, d, m, mode);
  }

  for(uint4 d : range(16))
  for(uint4 m : range(16))
  for(uint2 mode : range(4)) {
    if(mode == 3) continue;
    auto opcode = pattern("0100 01?? ???? ????") | bits(d,0,2) << 0 | m << 3 | bit1(d,3) << 7 | mode << 8;
    bind(opcode, ALUExtended, d, m, mode);
  }

  for(uint8 immediate : range(256))
  for(uint3 d : range(8))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1010 ???? ???? ????") | immediate << 0 | d << 8 | mode << 11;
    bind(opcode, AddRegister, immediate, d, mode);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint3 immediate : range(8))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("0001 11?? ???? ????") | d << 0 | n << 3 | immediate << 6 | mode << 9;
    bind(opcode, AdjustImmediate, d, n, immediate, mode);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint3 m : range(8))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("0001 10?? ???? ????") | d << 0 | n << 3 | m << 6 | mode << 9;
    bind(opcode, AdjustRegister, d, n, m, mode);
  }

  for(uint7 immediate : range(128))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1011 0000 ???? ????") | immediate << 0 | mode << 7;
    bind(opcode, AdjustStack, immediate, mode);
  }

  for(uint3 _ : range(8))
  for(uint4 m : range(16)) {
    auto opcode = pattern("0100 0111 0??? ?---") | _ << 0 | m << 3;
    bind(opcode, BranchExchange, m);
  }

  for(uint11 displacement : range(2048)) {
    auto opcode = pattern("1111 0??? ???? ????") | displacement << 0;
    bind(opcode, BranchFarPrefix, displacement);
  }

  for(uint11 displacement : range(2048)) {
    auto opcode = pattern("1111 1??? ???? ????") | displacement << 0;
    bind(opcode, BranchFarSuffix, displacement);
  }

  for(uint11 displacement : range(2048)) {
    auto opcode = pattern("1110 0??? ???? ????") | displacement << 0;
    bind(opcode, BranchNear, displacement);
  }

  for(uint8 displacement : range(256))
  for(uint4 condition : range(16)) {
    if(condition == 15) continue;  //BNV
    auto opcode = pattern("1101 ???? ???? ????") | displacement << 0 | condition << 8;
    bind(opcode, BranchTest, displacement, condition);
  }

  for(uint8 immediate : range(256))
  for(uint3 d : range(8))
  for(uint2 mode : range(4)) {
    auto opcode = pattern("001? ???? ???? ????") | immediate << 0 | d << 8 | mode << 11;
    bind(opcode, Immediate, immediate, d, mode);
  }

  for(uint8 displacement : range(256))
  for(uint3 d : range(8)) {
    auto opcode = pattern("0100 1??? ???? ????") | displacement << 0 | d << 8;
    bind(opcode, LoadLiteral, displacement, d);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint5 immediate : range(32))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("0111 ???? ???? ????") | d << 0 | n << 3 | immediate << 6 | mode << 11;
    bind(opcode, MoveByteImmediate, d, n, immediate, mode);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint5 immediate : range(32))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1000 ???? ???? ????") | d << 0 | n << 3 | immediate << 6 | mode << 11;
    bind(opcode, MoveHalfImmediate, d, n, immediate, mode);
  }

  for(uint8 list : range(256))
  for(uint3 n : range(8))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1100 ???? ???? ????") | list << 0 | n << 8 | mode << 11;
    bind(opcode, MoveMultiple, list, n, mode);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint3 m : range(8))
  for(uint3 mode : range(8)) {
    auto opcode = pattern("0101 ???? ???? ????") | d << 0 | n << 3 | m << 6 | mode << 9;
    bind(opcode, MoveRegisterOffset, d, n, m, mode);
  }

  for(uint8 immediate : range(256))
  for(uint3 d : range(8))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1001 ???? ???? ????") | immediate << 0 | d << 8 | mode << 11;
    bind(opcode, MoveStack, immediate, d, mode);
  }

  for(uint3 d : range(8))
  for(uint3 n : range(8))
  for(uint5 offset : range(32))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("0110 ???? ???? ????") | d << 0 | n << 3 | offset << 6 | mode << 11;
    bind(opcode, MoveWordImmediate, d, n, offset, mode);
  }

  for(uint3 d : range(8))
  for(uint3 m : range(8))
  for(uint5 immediate : range(32))
  for(uint2 mode : range(4)) {
    if(mode == 3) continue;
    auto opcode = pattern("000? ???? ???? ????") | d << 0 | m << 3 | immediate << 6 | mode << 11;
    bind(opcode, ShiftImmediate, d, m, immediate, mode);
  }

  for(uint8 immediate : range(256)) {
    auto opcode = pattern("1101 1111 ???? ????") | immediate << 0;
    bind(opcode, SoftwareInterrupt, immediate);
  }

  for(uint8 list : range(256))
  for(uint1 lrpc : range(2))
  for(uint1 mode : range(2)) {
    auto opcode = pattern("1011 ?10? ???? ????") | list << 0 | lrpc << 8 | mode << 11;
    bind(opcode, StackMultiple, list, lrpc, mode);
  }

  for(uint16 id : range(65536)) {
    if(thumbInstruction[id]) continue;
    auto opcode = pattern("???? ???? ???? ????") | id << 0;
    bind(opcode, Undefined);
  }

  #undef bit1
  #undef bits

  #undef bind
  #undef pattern
}
