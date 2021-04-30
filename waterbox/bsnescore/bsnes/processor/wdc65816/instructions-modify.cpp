auto WDC65816::instructionImpliedModify8(alu8 op, r16& M) -> void {
L idleIRQ();
  M.l = alu(M.l);
}

auto WDC65816::instructionImpliedModify16(alu16 op, r16& M) -> void {
L idleIRQ();
  M.w = alu(M.w);
}

auto WDC65816::instructionBankModify8(alu8 op) -> void {
  V.l = fetch();
  V.h = fetch();
  W.l = readBank(V.w + 0);
  idle();
  W.l = alu(W.l);
L writeBank(V.w + 0, W.l);
}

auto WDC65816::instructionBankModify16(alu16 op) -> void {
  V.l = fetch();
  V.h = fetch();
  W.l = readBank(V.w + 0);
  W.h = readBank(V.w + 1);
  idle();
  W.w = alu(W.w);
  writeBank(V.w + 1, W.h);
L writeBank(V.w + 0, W.l);
}

auto WDC65816::instructionBankIndexedModify8(alu8 op) -> void {
  V.l = fetch();
  V.h = fetch();
  idle();
  W.l = readBank(V.w + X.w + 0);
  idle();
  W.l = alu(W.l);
L writeBank(V.w + X.w + 0, W.l);
}

auto WDC65816::instructionBankIndexedModify16(alu16 op) -> void {
  V.l = fetch();
  V.h = fetch();
  idle();
  W.l = readBank(V.w + X.w + 0);
  W.h = readBank(V.w + X.w + 1);
  idle();
  W.w = alu(W.w);
  writeBank(V.w + X.w + 1, W.h);
L writeBank(V.w + X.w + 0, W.l);
}

auto WDC65816::instructionDirectModify8(alu8 op) -> void {
  U.l = fetch();
  idle2();
  W.l = readDirect(U.l + 0);
  idle();
  W.l = alu(W.l);
L writeDirect(U.l + 0, W.l);
}

auto WDC65816::instructionDirectModify16(alu16 op) -> void {
  U.l = fetch();
  idle2();
  W.l = readDirect(U.l + 0);
  W.h = readDirect(U.l + 1);
  idle();
  W.w = alu(W.w);
  writeDirect(U.l + 1, W.h);
L writeDirect(U.l + 0, W.l);
}

auto WDC65816::instructionDirectIndexedModify8(alu8 op) -> void {
  U.l = fetch();
  idle2();
  idle();
  W.l = readDirect(U.l + X.w + 0);
  idle();
  W.l = alu(W.l);
L writeDirect(U.l + X.w + 0, W.l);
}

auto WDC65816::instructionDirectIndexedModify16(alu16 op) -> void {
  U.l = fetch();
  idle2();
  idle();
  W.l = readDirect(U.l + X.w + 0);
  W.h = readDirect(U.l + X.w + 1);
  idle();
  W.w = alu(W.w);
  writeDirect(U.l + X.w + 1, W.h);
L writeDirect(U.l + X.w + 0, W.l);
}
