
auto CPU::MFC2(r64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  rt.u64 = s32(cop2.latch);
}

auto CPU::DMFC2(r64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  rt.u64 = cop2.latch;
}

auto CPU::CFC2(r64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  rt.u64 = s32(cop2.latch);
}

auto CPU::MTC2(cr64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  cop2.latch = rt.u64;
}

auto CPU::DMTC2(cr64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  cop2.latch = rt.u64;
}

auto CPU::CTC2(cr64& rt, u8 rd) -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  cop2.latch = rt.u64;
}

auto CPU::COP2INVALID() -> void {
  if(!scc.status.enable.coprocessor2) return exception.coprocessor2();
  exception.reservedInstructionCop2();
}