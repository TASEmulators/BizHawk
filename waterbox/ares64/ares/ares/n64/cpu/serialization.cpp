auto CPU::serialize(serializer& s) -> void {
  Thread::serialize(s);

  s(pipeline.address);
  s(pipeline.instruction);

  s(branch.pc);
  s(branch.state);

  s(context.endian);
  s(context.mode);
  s(context.bits);
  s(context.segment);

  for(auto& line : icache.lines) {
    s(line.valid);
    s(line.tag);
    s(line.index);
    s(line.words);
  }

  for(auto& line : dcache.lines) {
    s(line.valid);
    s(line.dirty);
    s(line.tag);
    s(line.index);
    s(line.words);
  }

  for(auto& e : tlb.entry) {
    s(e.global);
    s(e.valid);
    s(e.dirty);
    s(e.cacheAlgorithm);
    s(e.physicalAddress);
    s(e.pageMask);
    s(e.virtualAddress);
    s(e.addressSpaceID);
    s(e.region);
    s(e.globals);
    s(e.addressMaskHi);
    s(e.addressMaskLo);
    s(e.addressSelect);
    s(e.addressCompare);
  }
  s(tlb.physicalAddress);

  for(auto& r : ipu.r) s(r.u64);
  s(ipu.lo.u64);
  s(ipu.hi.u64);
  s(ipu.pc);

  s(scc.index.tlbEntry);
  s(scc.index.probeFailure);
  s(scc.random.index);
  s(scc.random.unused);
  s(scc.tlb.global);
  s(scc.tlb.valid);
  s(scc.tlb.dirty);
  s(scc.tlb.cacheAlgorithm);
  s(scc.tlb.physicalAddress);
  s(scc.tlb.pageMask);
  s(scc.tlb.virtualAddress);
  s(scc.tlb.addressSpaceID);
  s(scc.tlb.region);
  s(scc.tlb.globals);
  s(scc.tlb.addressMaskHi);
  s(scc.tlb.addressMaskLo);
  s(scc.tlb.addressSelect);
  s(scc.tlb.addressCompare);
  s(scc.context.badVirtualAddress);
  s(scc.context.pageTableEntryBase);
  s(scc.wired.index);
  s(scc.wired.unused);
  s(scc.badVirtualAddress);
  s(scc.count);
  s(scc.compare);
  s(scc.status.interruptEnable);
  s(scc.status.exceptionLevel);
  s(scc.status.errorLevel);
  s(scc.status.privilegeMode);
  s(scc.status.userExtendedAddressing);
  s(scc.status.kernelExtendedAddressing);
  s(scc.status.interruptMask);
  s(scc.status.de);
  s(scc.status.ce);
  s(scc.status.condition);
  s(scc.status.softReset);
  s(scc.status.tlbShutdown);
  s(scc.status.vectorLocation);
  s(scc.status.instructionTracing);
  s(scc.status.reverseEndian);
  s(scc.status.floatingPointMode);
  s(scc.status.lowPowerMode);
  s(scc.status.enable.coprocessor0);
  s(scc.status.enable.coprocessor1);
  s(scc.status.enable.coprocessor2);
  s(scc.status.enable.coprocessor3);
  s(scc.cause.exceptionCode);
  s(scc.cause.interruptPending);
  s(scc.cause.coprocessorError);
  s(scc.cause.branchDelay);
  s(scc.epc);
  s(scc.configuration.coherencyAlgorithmKSEG0);
  s(scc.configuration.cu);
  s(scc.configuration.bigEndian);
  s(scc.configuration.sysadWritebackPattern);
  s(scc.configuration.systemClockRatio);
  s(scc.ll);
  s(scc.llbit);
  s(scc.watchLo.trapOnWrite);
  s(scc.watchLo.trapOnRead);
  s(scc.watchLo.physicalAddress);
  s(scc.watchHi.physicalAddressExtended);
  s(scc.xcontext.badVirtualAddress);
  s(scc.xcontext.region);
  s(scc.xcontext.pageTableEntryBase);
  s(scc.parityError.diagnostic);
  s(scc.tagLo.primaryCacheState);
  s(scc.tagLo.physicalAddress);
  s(scc.epcError);

  for(auto& r : fpu.r) s(r.u64);
  s(fpu.csr.roundMode);
  s(fpu.csr.flag.inexact);
  s(fpu.csr.flag.underflow);
  s(fpu.csr.flag.overflow);
  s(fpu.csr.flag.divisionByZero);
  s(fpu.csr.flag.invalidOperation);
  s(fpu.csr.enable.inexact);
  s(fpu.csr.enable.underflow);
  s(fpu.csr.enable.overflow);
  s(fpu.csr.enable.divisionByZero);
  s(fpu.csr.enable.invalidOperation);
  s(fpu.csr.cause.inexact);
  s(fpu.csr.cause.underflow);
  s(fpu.csr.cause.overflow);
  s(fpu.csr.cause.divisionByZero);
  s(fpu.csr.cause.invalidOperation);
  s(fpu.csr.cause.unimplementedOperation);
  s(fpu.csr.compare);
  s(fpu.csr.flushed);

  if constexpr(Accuracy::CPU::Recompiler) {
    recompiler.reset();
  }
}
