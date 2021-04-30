auto ARM7TDMI::thumbInstructionALU
(uint3 d, uint3 m, uint4 mode) -> void {
  switch(mode) {
  case  0: r(d) = BIT(r(d) & r(m)); break;  //AND
  case  1: r(d) = BIT(r(d) ^ r(m)); break;  //EOR
  case  2: r(d) = BIT(LSL(r(d), r(m))); break;  //LSL
  case  3: r(d) = BIT(LSR(r(d), r(m))); break;  //LSR
  case  4: r(d) = BIT(ASR(r(d), r(m))); break;  //ASR
  case  5: r(d) = ADD(r(d), r(m), cpsr().c); break;  //ADC
  case  6: r(d) = SUB(r(d), r(m), cpsr().c); break;  //SBC
  case  7: r(d) = BIT(ROR(r(d), r(m))); break;  //ROR
  case  8:        BIT(r(d) & r(m)); break;  //TST
  case  9: r(d) = SUB(0, r(m), 1); break;  //NEG
  case 10:        SUB(r(d), r(m), 1); break;  //CMP
  case 11:        ADD(r(d), r(m), 0); break;  //CMN
  case 12: r(d) = BIT(r(d) | r(m)); break;  //ORR
  case 13: r(d) = MUL(0, r(m), r(d)); break;  //MUL
  case 14: r(d) = BIT(r(d) & ~r(m)); break;  //BIC
  case 15: r(d) = BIT(~r(m)); break;  //MVN
  }
}

auto ARM7TDMI::thumbInstructionALUExtended
(uint4 d, uint4 m, uint2 mode) -> void {
  switch(mode) {
  case 0: r(d) = r(d) + r(m); break;  //ADD
  case 1: SUB(r(d), r(m), 1); break;  //SUBS
  case 2: r(d) = r(m); break;  //MOV
  }
}

auto ARM7TDMI::thumbInstructionAddRegister
(uint8 immediate, uint3 d, uint1 mode) -> void {
  switch(mode) {
  case 0: r(d) = (r(15) & ~3) + immediate * 4; break;  //ADD pc
  case 1: r(d) = r(13) + immediate * 4; break;  //ADD sp
  }
}

auto ARM7TDMI::thumbInstructionAdjustImmediate
(uint3 d, uint3 n, uint3 immediate, uint1 mode) -> void {
  switch(mode) {
  case 0: r(d) = ADD(r(n), immediate, 0); break;  //ADD
  case 1: r(d) = SUB(r(n), immediate, 1); break;  //SUB
  }
}

auto ARM7TDMI::thumbInstructionAdjustRegister
(uint3 d, uint3 n, uint3 m, uint1 mode) -> void {
  switch(mode) {
  case 0: r(d) = ADD(r(n), r(m), 0); break;  //ADD
  case 1: r(d) = SUB(r(n), r(m), 1); break;  //SUB
  }
}

auto ARM7TDMI::thumbInstructionAdjustStack
(uint7 immediate, uint1 mode) -> void {
  switch(mode) {
  case 0: r(13) = r(13) + immediate * 4; break;  //ADD
  case 1: r(13) = r(13) - immediate * 4; break;  //SUB
  }
}

auto ARM7TDMI::thumbInstructionBranchExchange
(uint4 m) -> void {
  uint32 address = r(m);
  cpsr().t = address & 1;
  r(15) = address;
}

auto ARM7TDMI::thumbInstructionBranchFarPrefix
(int11 displacement) -> void {
  r(14) = r(15) + (displacement * 2 << 11);
}

auto ARM7TDMI::thumbInstructionBranchFarSuffix
(uint11 displacement) -> void {
  r(15) = r(14) + (displacement * 2);
  r(14) = pipeline.decode.address | 1;
}

auto ARM7TDMI::thumbInstructionBranchNear
(int11 displacement) -> void {
  r(15) = r(15) + displacement * 2;
}

auto ARM7TDMI::thumbInstructionBranchTest
(int8 displacement, uint4 condition) -> void {
  if(!TST(condition)) return;
  r(15) = r(15) + displacement * 2;
}

auto ARM7TDMI::thumbInstructionImmediate
(uint8 immediate, uint3 d, uint2 mode) -> void {
  switch(mode) {
  case 0: r(d) = BIT(immediate); break;  //MOV
  case 1:        SUB(r(d), immediate, 1); break;  //CMP
  case 2: r(d) = ADD(r(d), immediate, 0); break;  //ADD
  case 3: r(d) = SUB(r(d), immediate, 1); break;  //SUB
  }
}

auto ARM7TDMI::thumbInstructionLoadLiteral
(uint8 displacement, uint3 d) -> void {
  uint32 address = (r(15) & ~3) + (displacement << 2);
  r(d) = load(Word | Nonsequential, address);
}

auto ARM7TDMI::thumbInstructionMoveByteImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> void {
  switch(mode) {
  case 0: store(Byte | Nonsequential, r(n) + offset, r(d)); break;  //STRB
  case 1: r(d) = load(Byte | Nonsequential, r(n) + offset); break;  //LDRB
  }
}

auto ARM7TDMI::thumbInstructionMoveHalfImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> void {
  switch(mode) {
  case 0: store(Half | Nonsequential, r(n) + offset * 2, r(d)); break;  //STRH
  case 1: r(d) = load(Half | Nonsequential, r(n) + offset * 2); break;  //LDRH
  }
}

auto ARM7TDMI::thumbInstructionMoveMultiple
(uint8 list, uint3 n, uint1 mode) -> void {
  uint32 rn = r(n);

  for(uint m : range(8)) {
    if(!(list & 1 << m)) continue;
    switch(mode) {
    case 0: write(Word | Nonsequential, rn, r(m)); break;  //STMIA
    case 1: r(m) = read(Word | Nonsequential, rn); break;  //LDMIA
    }
    rn += 4;
  }

  if(mode == 0 || !(list & 1 << n)) r(n) = rn;
  if(mode == 1) idle();
}

auto ARM7TDMI::thumbInstructionMoveRegisterOffset
(uint3 d, uint3 n, uint3 m, uint3 mode) -> void {
  switch(mode) {
  case 0: store(Word | Nonsequential, r(n) + r(m), r(d)); break;  //STR
  case 1: store(Half | Nonsequential, r(n) + r(m), r(d)); break;  //STRH
  case 2: store(Byte | Nonsequential, r(n) + r(m), r(d)); break;  //STRB
  case 3: r(d) = load(Byte | Nonsequential | Signed, r(n) + r(m)); break;  //LDSB
  case 4: r(d) = load(Word | Nonsequential, r(n) + r(m)); break;  //LDR
  case 5: r(d) = load(Half | Nonsequential, r(n) + r(m)); break;  //LDRH
  case 6: r(d) = load(Byte | Nonsequential, r(n) + r(m)); break;  //LDRB
  case 7: r(d) = load(Half | Nonsequential | Signed, r(n) + r(m)); break;  //LDSH
  }
}

auto ARM7TDMI::thumbInstructionMoveStack
(uint8 immediate, uint3 d, uint1 mode) -> void {
  switch(mode) {
  case 0: store(Word | Nonsequential, r(13) + immediate * 4, r(d)); break;  //STR
  case 1: r(d) = load(Word | Nonsequential, r(13) + immediate * 4); break;  //LDR
  }
}

auto ARM7TDMI::thumbInstructionMoveWordImmediate
(uint3 d, uint3 n, uint5 offset, uint1 mode) -> void {
  switch(mode) {
  case 0: store(Word | Nonsequential, r(n) + offset * 4, r(d)); break;  //STR
  case 1: r(d) = load(Word | Nonsequential, r(n) + offset * 4); break;  //LDR
  }
}

auto ARM7TDMI::thumbInstructionShiftImmediate
(uint3 d, uint3 m, uint5 immediate, uint2 mode) -> void {
  switch(mode) {
  case 0: r(d) = BIT(LSL(r(m), immediate)); break;  //LSL
  case 1: r(d) = BIT(LSR(r(m), immediate ? (uint)immediate : 32)); break;  //LSR
  case 2: r(d) = BIT(ASR(r(m), immediate ? (uint)immediate : 32)); break;  //ASR
  }
}

auto ARM7TDMI::thumbInstructionSoftwareInterrupt
(uint8 immediate) -> void {
  exception(PSR::SVC, 0x08);
}

auto ARM7TDMI::thumbInstructionStackMultiple
(uint8 list, uint1 lrpc, uint1 mode) -> void {
  uint32 sp;
  switch(mode) {
  case 0: sp = r(13) - (bit::count(list) + lrpc) * 4; break;  //PUSH
  case 1: sp = r(13);  //POP
  }

  uint sequential = Nonsequential;
  for(uint m : range(8)) {
    if(!(list & 1 << m)) continue;
    switch(mode) {
    case 0: write(Word | sequential, sp, r(m)); break;  //PUSH
    case 1: r(m) = read(Word | sequential, sp); break;  //POP
    }
    sp += 4;
    sequential = Sequential;
  }

  if(lrpc) {
    switch(mode) {
    case 0: write(Word | sequential, sp, r(14)); break;  //PUSH
    case 1: r(15) = read(Word | sequential, sp); break;  //POP
    }
    sp += 4;
  }

  if(mode == 1) {
    idle();
    r(13) = r(13) + (bit::count(list) + lrpc) * 4;  //POP
  } else {
    pipeline.nonsequential = true;
    r(13) = r(13) - (bit::count(list) + lrpc) * 4;  //PUSH
  }
}

auto ARM7TDMI::thumbInstructionUndefined
() -> void {
  exception(PSR::UND, 0x04);
}
