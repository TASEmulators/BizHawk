auto RSP::serialize(serializer& s) -> void {
  Thread::serialize(s);
  s(dmem);
  s(imem);

  s(pipeline.address);
  s(pipeline.instruction);

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

  for(auto& r : vpu.r) s(r.u128);
  s(vpu.acch.u128);
  s(vpu.accm.u128);
  s(vpu.accl.u128);
  s(vpu.vcoh.u128);
  s(vpu.vcol.u128);
  s(vpu.vcch.u128);
  s(vpu.vccl.u128);
  s(vpu.vce.u128);
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
