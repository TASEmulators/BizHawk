auto WDC65816::instructionBranch(bool take) -> void {
  if(!take) {
L   fetch();
  } else {
    U.l = fetch();
    V.w = PC.d + (int8)U.l;
    idle6(V.w);
L   idle();
    PC.w = V.w;
    idleBranch();
  }
}

auto WDC65816::instructionBranchLong() -> void {
  U.l = fetch();
  U.h = fetch();
  V.w = PC.d + (int16)U.w;
L idle();
  PC.w = V.w;
  idleBranch();
}

auto WDC65816::instructionJumpShort() -> void {
  W.l = fetch();
L W.h = fetch();
  PC.w = W.w;
  idleJump();
}

auto WDC65816::instructionJumpLong() -> void {
  V.l = fetch();
  V.h = fetch();
L V.b = fetch();
  PC.d = V.d;
  idleJump();
}

auto WDC65816::instructionJumpIndirect() -> void {
  V.l = fetch();
  V.h = fetch();
  W.l = read(uint16(V.w + 0));
L W.h = read(uint16(V.w + 1));
  PC.w = W.w;
  idleJump();
}

auto WDC65816::instructionJumpIndexedIndirect() -> void {
  V.l = fetch();
  V.h = fetch();
  idle();
  W.l = read(PC.b << 16 | uint16(V.w + X.w + 0));
L W.h = read(PC.b << 16 | uint16(V.w + X.w + 1));
  PC.w = W.w;
  idleJump();
}

auto WDC65816::instructionJumpIndirectLong() -> void {
  U.l = fetch();
  U.h = fetch();
  V.l = read(uint16(U.w + 0));
  V.h = read(uint16(U.w + 1));
L V.b = read(uint16(U.w + 2));
  PC.d = V.d;
  idleJump();
}

auto WDC65816::instructionCallShort() -> void {
  W.l = fetch();
  W.h = fetch();
  idle();
  PC.w--;
  push(PC.h);
L push(PC.l);
  PC.w = W.w;
  idleJump();
}

auto WDC65816::instructionCallLong() -> void {
  V.l = fetch();
  V.h = fetch();
  pushN(PC.b);
  idle();
  V.b = fetch();
  PC.w--;
  pushN(PC.h);
L pushN(PC.l);
  PC.d = V.d;
E S.h = 0x01;
  idleJump();
}

auto WDC65816::instructionCallIndexedIndirect() -> void {
  V.l = fetch();
  pushN(PC.h);
  pushN(PC.l);
  V.h = fetch();
  idle();
  W.l = read(PC.b << 16 | uint16(V.w + X.w + 0));
L W.h = read(PC.b << 16 | uint16(V.w + X.w + 1));
  PC.w = W.w;
E S.h = 0x01;
  idleJump();
}

auto WDC65816::instructionReturnInterrupt() -> void {
  idle();
  idle();
  P = pull();
E XF = 1, MF = 1;
  if(XF) X.h = 0x00, Y.h = 0x00;
  PC.l = pull();
  if(EF) {
  L PC.h = pull();
  } else {
    PC.h = pull();
  L PC.b = pull();
  }
  idleJump();
}

auto WDC65816::instructionReturnShort() -> void {
  idle();
  idle();
  W.l = pull();
  W.h = pull();
L idle();
  PC.w = W.w;
  PC.w++;
  idleJump();
}

auto WDC65816::instructionReturnLong() -> void {
  idle();
  idle();
  V.l = pullN();
  V.h = pullN();
L V.b = pullN();
  PC.d = V.d;
  PC.w++;
E S.h = 0x01;
  idleJump();
}
