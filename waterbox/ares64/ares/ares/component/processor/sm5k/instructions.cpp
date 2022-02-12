auto SM5K::instructionADC() -> void {
  auto c = C;
  C = A + RAM[B] + c >= 0x10;
  A = A + RAM[B] + c;
  if(C) SKIP = 1;
}

auto SM5K::instructionADD() -> void {
  A += RAM[B];
}

auto SM5K::instructionADX(n4 data) -> void {
  if(A + data >= 0x10) SKIP = 1;
  A += data;
}

auto SM5K::instructionANP() -> void {
  switch(BL) {
  case 0x0: P0 &= A; break;
  case 0x2: P2 &= A; break;
  case 0x4: P4 &= A; break;
  case 0x5: P5 &= A; break;
  }
}

auto SM5K::instructionATX() -> void {
  X = A;
}

auto SM5K::instructionCALL(n12 address) -> void {
  SR[SP++] = PC;
  PC = address;
}

auto SM5K::instructionCOMA() -> void {
  A = ~A;
}

auto SM5K::instructionDECB() -> void {
  if(!--BL) SKIP = 1;
}

auto SM5K::instructionDR() -> void {
  DIV = 0;
}

auto SM5K::instructionDTA(n8 operand) -> void {
  switch(operand) {
  case 0x02: instructionTT(); break;
  case 0x03: instructionDR(); break;
  }

  static constexpr u8 rom[8] = {0xfc, 0xfc, 0xa5, 0x6c, 0x03, 0x8f, 0x1b, 0x9a};
  if(BM >= 4 && BM <= 7) {
    SKIP = rom[BM << 1 | BL >> 3] >> n3(BL) & 1;
  }
}

auto SM5K::instructionEX() -> void {
  swap(B, SB);
}

auto SM5K::instructionEXAX() -> void {
  swap(A, X);
}

auto SM5K::instructionEXBL() -> void {
  auto a = A;
  A = BL;
  BL = a;
}

auto SM5K::instructionEXBM() -> void {
  auto a = A;
  A = BM;
  BM = a;
}

auto SM5K::instructionEXC(n2 data) -> void {
  swap(A, RAM[B]);
  BM ^= data;
}

auto SM5K::instructionEXCD(n2 data) -> void {
  swap(A, RAM[B]);
  if(!--BL) SKIP = 1;
  BM ^= data;
}

auto SM5K::instructionEXCI(n2 data) -> void {
  swap(A, RAM[B]);
  if(!++BL) SKIP = 1;
  BM ^= data;
}

auto SM5K::instructionID() -> void {
  IME = 0;
}

auto SM5K::instructionIE() -> void {
  IME = 1;
}

auto SM5K::instructionHALT() -> void {
  HALT = 1;
}

auto SM5K::instructionIN() -> void {
  switch(BL) {
  case 0x1: A = P1; break;
  case 0x2: A = P2; break;
  case 0x3: A = P3; break;
  case 0x4: A = P4; break;
  case 0x5: A = P5; break;
  case 0x8: A = R8 >> 0; X = R8 >> 4; break;
  case 0x9: A = R9 >> 0; X = R9 >> 4; break;
  case 0xa: A = RA >> 0; X = RA >> 4; break;
  case 0xb: A = RB >> 0; X = RB >> 4; break;
  case 0xc: A = RC; break;
  case 0xe: A = RE; break;
  case 0xf: A = RF; break;
  }
}

auto SM5K::instructionINCB() -> void {
  if(!++BL) SKIP = 1;
}

auto SM5K::instructionINL() -> void {
  A = P1;
}

auto SM5K::instructionLAX(n4 data) -> void {
  A = data;
}

auto SM5K::instructionLBLX(n4 data) -> void {
  BL = data;
}

auto SM5K::instructionLBMX(n4 data) -> void {
  BM = data;
}

auto SM5K::instructionLDA(n2 data) -> void {
  A = RAM[B];
  BM ^= data;
}

auto SM5K::instructionORP() -> void {
  switch(BL) {
  case 0x0: P0 |= A; break;
  case 0x2: P2 |= A; break;
  case 0x4: P4 |= A; break;
  case 0x5: P5 |= A; break;
  }
}

auto SM5K::instructionOUT() -> void {
  switch(BL) {
  case 0x0: P0 = A; break;
  case 0x2: P2 = A; break;
  case 0x3: R3 = A; break;
  case 0x4: P4 = A; break;
  case 0x5: P5 = A; break;
  case 0x8: R8 = A << 0 | X << 4; break;
  case 0x9: R9 = A << 0 | X << 4; break;
  case 0xa: RA = RB; break;
  case 0xb: RB = A << 0 | X << 4; break;
  case 0xc: RC = A; break;
  case 0xe: RE = A; break;
  case 0xf: RF = A; break;
  }
}

auto SM5K::instructionOUTL() -> void {
  P0 = A;
}

auto SM5K::instructionPAT(n8) -> void {
  //should this actually modify the stack frame?
  n6 pu = 4;
  n6 pl = X << 4 | A;
  n8 data = ROM[pu << 6 | pl];
  A = data >> 0;
  X = data >> 4;
}

auto SM5K::instructionRC() -> void {
  C = 0;
}

auto SM5K::instructionRM(n2 data) -> void {
  RAM[B] &= ~(1 << data);
}

auto SM5K::instructionRTN() -> void {
  PC = SR[--SP];
}

auto SM5K::instructionRTNI() -> void {
  PC = SR[--SP];
  IME = 1;
}

auto SM5K::instructionRTNS() -> void {
  PC = SR[--SP];
  SKIP = 1;
}

auto SM5K::instructionSC() -> void {
  C = 1;
}

auto SM5K::instructionSM(n2 data) -> void {
  RAM[B] |= 1 << data;
}

auto SM5K::instructionSTOP() -> void {
  STOP = 1;
}

auto SM5K::instructionTA() -> void {
  if(IFA) SKIP = 1;
  IFA = 0;
}

auto SM5K::instructionTABL() -> void {
  if(A == BL) SKIP = 1;
}

auto SM5K::instructionTAM() -> void {
  if(A == RAM[B]) SKIP = 1;
}

auto SM5K::instructionTB() -> void {
  if(IFB) SKIP = 1;
  IFB = 0;
}

auto SM5K::instructionTC() -> void {
  if(C == 1) SKIP = 1;
}

auto SM5K::instructionTL(n12 address) -> void {
  PC = address;
}

auto SM5K::instructionTM(n2 data) -> void {
  if(RAM[B] & 1 << data) SKIP = 1;
}

auto SM5K::instructionTPB(n2 port) -> void {
  switch(port) {
  case 0: if(P0 == 1) SKIP = 1; break;
  case 1: if(P1 == 1) SKIP = 1; break;
  case 2: if(P2 == 1) SKIP = 1; break;
  case 3: if(P3 == 1) SKIP = 1; break;
  }
}

auto SM5K::instructionTR(n6 address) -> void {
  PL = address;
}

auto SM5K::instructionTRS(n5 address) -> void {
  SR[SP++] = PC;
  PU = 1;
  PL = address << 1;
}

auto SM5K::instructionTT() -> void {
  if(IFT) SKIP = 1;
  IFT = 0;
}
