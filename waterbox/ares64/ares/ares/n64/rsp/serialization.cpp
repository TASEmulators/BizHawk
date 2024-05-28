auto RSP::serialize(serializer& s) -> void {
  Thread::serialize(s);
  s(dmem);
  s(imem);

  s(pipeline.address);
  s(pipeline.instruction);
  s(pipeline.singleIssue);
  for(auto& p : pipeline.previous) {
    s(p.load);
    s(p.rWrite);
    s(p.vWrite);
  }

  s(dma.pending);
  s(dma.current);
  s(dma.busy.read);
  s(dma.busy.write);
  s(dma.full.read);
  s(dma.full.write);

  s(status.semaphore);
  s(status.halted);
  s(status.broken);
  s(status.full);
  s(status.singleStep);
  s(status.interruptOnBreak);
  s(status.signal);

  for(auto& r : ipu.r) s(r.u32);
  s(ipu.pc);

  s(branch.pc);
  s(branch.state);

  for(auto& r : vpu.r) s(r);
  s(vpu.acch);
  s(vpu.accm);
  s(vpu.accl);
  s(vpu.vcoh);
  s(vpu.vcol);
  s(vpu.vcch);
  s(vpu.vccl);
  s(vpu.vce);
  s(vpu.divin);
  s(vpu.divout);
  s(vpu.divdp);

  if constexpr(Accuracy::RSP::Recompiler) {
    recompiler.reset();
  }
}

auto RSP::DMA::Regs::serialize(serializer& s) -> void {
  s(pbusRegion);
  s(pbusAddress);
  s(dramAddress);
  s(length);
  s(skip);
  s(count);
}

auto RSP::r128::serialize(serializer& s) -> void {
  s(u128.lo);
  s(u128.hi);
}
