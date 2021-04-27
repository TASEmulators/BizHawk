auto WDC65816::instructionBitImmediate8() -> void {
L U.l = fetch();
  ZF = (U.l & A.l) == 0;
}

auto WDC65816::instructionBitImmediate16() -> void {
  U.l = fetch();
L U.h = fetch();
  ZF = (U.w & A.w) == 0;
}

auto WDC65816::instructionNoOperation() -> void {
L idleIRQ();
}

auto WDC65816::instructionPrefix() -> void {
L fetch();
}

auto WDC65816::instructionExchangeBA() -> void {
  idle();
L idle();
  A.w = A.w >> 8 | A.w << 8;
  ZF = A.l == 0;
  NF = A.l & 0x80;
}

auto WDC65816::instructionBlockMove8(int adjust) -> void {
  U.b = fetch();
  V.b = fetch();
  B = U.b;
  W.l = read(V.b << 16 | X.w);
  write(U.b << 16 | Y.w, W.l);
  idle();
  X.l += adjust;
  Y.l += adjust;
L idle();
  if(A.w--) PC.w -= 3;
}

auto WDC65816::instructionBlockMove16(int adjust) -> void {
  U.b = fetch();
  V.b = fetch();
  B = U.b;
  W.l = read(V.b << 16 | X.w);
  write(U.b << 16 | Y.w, W.l);
  idle();
  X.w += adjust;
  Y.w += adjust;
L idle();
  if(A.w--) PC.w -= 3;
}

auto WDC65816::instructionInterrupt(r16 vector) -> void {
  fetch();
N push(PC.b);
  push(PC.h);
  push(PC.l);
  push(P);
  IF = 1;
  DF = 0;
  PC.l = read(vector.w + 0);
L PC.h = read(vector.w + 1);
  PC.b = 0x00;
}

auto WDC65816::instructionStop() -> void {
  r.stp = true;
  while(r.stp && !synchronizing()) {
L   idle();
  }
}

auto WDC65816::instructionWait() -> void {
  r.wai = true;
  while(r.wai && !synchronizing()) {
L   idle();
  }
  idle();
}

auto WDC65816::instructionExchangeCE() -> void {
L idleIRQ();
  swap(CF, EF);
  if(EF) {
    XF = 1;
    MF = 1;
    X.h = 0x00;
    Y.h = 0x00;
    S.h = 0x01;
  }
}

auto WDC65816::instructionSetFlag(bool& flag) -> void {
L idleIRQ();
  flag = 1;
}

auto WDC65816::instructionClearFlag(bool& flag) -> void {
L idleIRQ();
  flag = 0;
}

auto WDC65816::instructionResetP() -> void {
  W.l = fetch();
L idle();
  P = P & ~W.l;
E XF = 1, MF = 1;
  if(XF) X.h = 0x00, Y.h = 0x00;
}

auto WDC65816::instructionSetP() -> void {
  W.l = fetch();
L idle();
  P = P | W.l;
E XF = 1, MF = 1;
  if(XF) X.h = 0x00, Y.h = 0x00;
}

auto WDC65816::instructionTransfer8(r16 F, r16& T) -> void {
L idleIRQ();
  T.l = F.l;
  ZF = T.l == 0;
  NF = T.l & 0x80;
}

auto WDC65816::instructionTransfer16(r16 F, r16& T) -> void {
L idleIRQ();
  T.w = F.w;
  ZF = T.w == 0;
  NF = T.w & 0x8000;
}

auto WDC65816::instructionTransferCS() -> void {
L idleIRQ();
  S.w = A.w;
E S.h = 0x01;
}

auto WDC65816::instructionTransferSX8() -> void {
L idleIRQ();
  X.l = S.l;
  ZF = X.l == 0;
  NF = X.l & 0x80;
}

auto WDC65816::instructionTransferSX16() -> void {
L idleIRQ();
  X.w = S.w;
  ZF = X.w == 0;
  NF = X.w & 0x8000;
}

auto WDC65816::instructionTransferXS() -> void {
L idleIRQ();
E S.l = X.l;
N S.w = X.w;
}

auto WDC65816::instructionPush8(r16 F) -> void {
  idle();
L push(F.l);
}

auto WDC65816::instructionPush16(r16 F) -> void {
  idle();
  push(F.h);
L push(F.l);
}

auto WDC65816::instructionPushD() -> void {
  idle();
  pushN(D.h);
L pushN(D.l);
E S.h = 0x01;
}

auto WDC65816::instructionPull8(r16& T) -> void {
  idle();
  idle();
L T.l = pull();
  ZF = T.l == 0;
  NF = T.l & 0x80;
}

auto WDC65816::instructionPull16(r16& T) -> void {
  idle();
  idle();
  T.l = pull();
L T.h = pull();
  ZF = T.w == 0;
  NF = T.w & 0x8000;
}

auto WDC65816::instructionPullD() -> void {
  idle();
  idle();
  D.l = pullN();
L D.h = pullN();
  ZF = D.w == 0;
  NF = D.w & 0x8000;
E S.h = 0x01;
}

auto WDC65816::instructionPullB() -> void {
  idle();
  idle();
L B = pull();
  ZF = B == 0;
  NF = B & 0x80;
}

auto WDC65816::instructionPullP() -> void {
  idle();
  idle();
L P = pull();
E XF = 1, MF = 1;
  if(XF) X.h = 0x00, Y.h = 0x00;
}

auto WDC65816::instructionPushEffectiveAddress() -> void {
  W.l = fetch();
  W.h = fetch();
  pushN(W.h);
L pushN(W.l);
E S.h = 0x01;
}

auto WDC65816::instructionPushEffectiveIndirectAddress() -> void {
  U.l = fetch();
  idle2();
  W.l = readDirectN(U.l + 0);
  W.h = readDirectN(U.l + 1);
  pushN(W.h);
L pushN(W.l);
E S.h = 0x01;
}

auto WDC65816::instructionPushEffectiveRelativeAddress() -> void {
  V.l = fetch();
  V.h = fetch();
  idle();
  W.w = PC.d + (int16)V.w;
  pushN(W.h);
L pushN(W.l);
E S.h = 0x01;
}
