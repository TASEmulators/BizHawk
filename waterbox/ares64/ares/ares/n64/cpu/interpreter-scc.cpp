auto CPU::getControlRegister(n5 index) -> u64 {
  n64 data;
  switch(index) {
  case  0:  //index
    data.bit( 0, 5) = scc.index.tlbEntry;
    data.bit(31)    = scc.index.probeFailure;
    break;
  case  1:  //random
    data.bit(0,4) = scc.random.index;
    data.bit(5)   = scc.random.unused;
    break;
  case  2:  //entrylo0
    data.bit(0)    = scc.tlb.global[0];
    data.bit(1)    = scc.tlb.valid[0];
    data.bit(2)    = scc.tlb.dirty[0];
    data.bit(3, 5) = scc.tlb.cacheAlgorithm[0];
    data.bit(6,29) = scc.tlb.physicalAddress[0].bit(12,35);
    break;
  case  3:  //entrylo1
    data.bit(0)    = scc.tlb.global[1];
    data.bit(1)    = scc.tlb.valid[1];
    data.bit(2)    = scc.tlb.dirty[1];
    data.bit(3, 5) = scc.tlb.cacheAlgorithm[1];
    data.bit(6,29) = scc.tlb.physicalAddress[1].bit(12,35);
    break;
  case  4:  //context
    data.bit( 4,22) = scc.context.badVirtualAddress;
    data.bit(23,63) = scc.context.pageTableEntryBase;
    break;
  case  5:  //pagemask
    data.bit(13,24) = scc.tlb.pageMask.bit(13,24);
    break;
  case  6:  //wired
    data.bit(0,4) = scc.wired.index;
    data.bit(5)   = scc.wired.unused;
    break;
  case  8:  //badvaddr
    data = scc.badVirtualAddress;
    break;
  case  9:  //count
    data.bit(0,31) = scc.count >> 1;
    break;
  case 10:  //entryhi
    data.bit( 0, 7) = scc.tlb.addressSpaceID;
    data.bit(13,39) = scc.tlb.virtualAddress.bit(13,39);
    data.bit(40,61) = 0;
    data.bit(62,63) = scc.tlb.region;
    break;
  case 11:  //compare
    data.bit(0,31) = scc.compare >> 1;
    break;
  case 12:  //status
    data.bit( 0)    = scc.status.interruptEnable;
    data.bit( 1)    = scc.status.exceptionLevel;
    data.bit( 2)    = scc.status.errorLevel;
    data.bit( 3, 4) = scc.status.privilegeMode;
    data.bit( 5)    = scc.status.userExtendedAddressing;
    data.bit( 6)    = scc.status.supervisorExtendedAddressing;
    data.bit( 7)    = scc.status.kernelExtendedAddressing;
    data.bit( 8,15) = scc.status.interruptMask;
    data.bit(16)    = scc.status.de;
    data.bit(17)    = scc.status.ce;
    data.bit(18)    = scc.status.condition;
    data.bit(20)    = scc.status.softReset;
    data.bit(21)    = scc.status.tlbShutdown;
    data.bit(22)    = scc.status.vectorLocation;
    data.bit(24)    = scc.status.instructionTracing;
    data.bit(25)    = scc.status.reverseEndian;
    data.bit(26)    = scc.status.floatingPointMode;
    data.bit(27)    = scc.status.lowPowerMode;
    data.bit(28)    = scc.status.enable.coprocessor0;
    data.bit(29)    = scc.status.enable.coprocessor1;
    data.bit(30)    = scc.status.enable.coprocessor2;
    data.bit(31)    = scc.status.enable.coprocessor3;
    context.setMode();
    break;
  case 13:  //cause
    data.bit( 2, 6) = scc.cause.exceptionCode;
    data.bit( 8,15) = scc.cause.interruptPending;
    data.bit(28,29) = scc.cause.coprocessorError;
    data.bit(31)    = scc.cause.branchDelay;
    break;
  case 14:  //exception program counter
    data = scc.epc;
    break;
  case 15:  //coprocessor revision identifier
    data.bit(0, 7) = scc.coprocessor.revision;
    data.bit(8,15) = scc.coprocessor.implementation;
    break;
  case 16:  //configuration
    data.bit( 0, 1) = scc.configuration.coherencyAlgorithmKSEG0;
    data.bit( 2, 3) = scc.configuration.cu;
    data.bit(15)    = scc.configuration.bigEndian;
    data.bit(24,27) = scc.configuration.sysadWritebackPattern;
    data.bit(28,30) = scc.configuration.systemClockRatio;
    break;
  case 17:  //load linked address
    data = scc.ll;
    break;
  case 18:  //watchlo
    data.bit(0)    = scc.watchLo.trapOnWrite;
    data.bit(1)    = scc.watchLo.trapOnRead;
    data.bit(3,31) = scc.watchLo.physicalAddress.bit(3,31);
    break;
  case 19:  //watchhi
    data.bit(0,3) = scc.watchHi.physicalAddressExtended;
    break;
  case 20:  //xcontext
    data.bit( 4,30) = scc.xcontext.badVirtualAddress;
    data.bit(31,32) = scc.xcontext.region;
    data.bit(33,63) = scc.xcontext.pageTableEntryBase;
    break;
  case 26:  //parity error
    data.bit(0,7) = scc.parityError.diagnostic;
    break;
  case 27:  //cache error (unused)
    data.bit(0,31) = 0;
    break;
  case 28:  //taglo
    data.bit(6, 7) = scc.tagLo.primaryCacheState;
    data.bit(8,27) = scc.tagLo.physicalAddress.bit(12,31);
    break;
  case 29:  //taghi
    data.bit(0,31) = 0;
    break;
  case 30:  //error exception program counter
    data = scc.epcError;
    break;
  }
  return data;
}

auto CPU::setControlRegister(n5 index, n64 data) -> void {
  //read-only variables are defined but commented out for documentation purposes
  switch(index) {
  case  0:  //index
    scc.index.tlbEntry     = data.bit( 0,5);
    scc.index.probeFailure = data.bit(31);
    break;
  case  1:  //random
  //scc.random.index  = data.bit(0,4);
    scc.random.unused = data.bit(5);
    break;
  case  2:  //entrylo0
    scc.tlb.global[0]                     = data.bit(0);
    scc.tlb.valid[0]                      = data.bit(1);
    scc.tlb.dirty[0]                      = data.bit(2);
    scc.tlb.cacheAlgorithm[0]             = data.bit(3, 5);
    scc.tlb.physicalAddress[0].bit(12,35) = data.bit(6,29);
    scc.tlb.synchronize();
    break;
  case  3:  //entrylo1
    scc.tlb.global[1]                     = data.bit(0);
    scc.tlb.valid[1]                      = data.bit(1);
    scc.tlb.dirty[1]                      = data.bit(2);
    scc.tlb.cacheAlgorithm[1]             = data.bit(3, 5);
    scc.tlb.physicalAddress[1].bit(12,35) = data.bit(6,29);
    scc.tlb.synchronize();
    break;
  case  4:  //context
    scc.context.badVirtualAddress  = data.bit( 4,22);
    scc.context.pageTableEntryBase = data.bit(23,63);
    break;
  case  5:  //pagemask
    scc.tlb.pageMask.bit(13,24) = data.bit(13,24);
    scc.tlb.synchronize();
    break;
  case  6:  //wired
    scc.wired.index  = data.bit(0,4);
    scc.wired.unused = data.bit(5);
    scc.random.index = 31;
    break;
  case  8:  //badvaddr
  //scc.badVirtualAddress = data;  //read-only
    break;
  case  9:  //count
    scc.count = data.bit(0,31) << 1;
    break;
  case 10:  //entryhi
    scc.tlb.addressSpaceID            = data.bit( 0, 7);
    scc.tlb.virtualAddress.bit(13,39) = data.bit(13,39);
    scc.tlb.region                    = data.bit(62,63);
    scc.tlb.synchronize();
    break;
  case 11:  //compare
    scc.compare = data.bit(0,31) << 1;
    scc.cause.interruptPending.bit(Interrupt::Timer) = 0;
    break;
  case 12: {//status
    bool floatingPointMode = scc.status.floatingPointMode;
    scc.status.interruptEnable              = data.bit( 0);
    scc.status.exceptionLevel               = data.bit( 1);
    scc.status.errorLevel                   = data.bit( 2);
    scc.status.privilegeMode                = data.bit( 3, 4);
    scc.status.userExtendedAddressing       = data.bit( 5);
    scc.status.supervisorExtendedAddressing = data.bit( 6);
    scc.status.kernelExtendedAddressing     = data.bit( 7);
    scc.status.interruptMask                = data.bit( 8,15);
    scc.status.de                           = data.bit(16);
    scc.status.ce                           = data.bit(17);
    scc.status.condition                    = data.bit(18);
    scc.status.softReset                    = data.bit(20);
  //scc.status.tlbShutdown                  = data.bit(21);  //read-only
    scc.status.vectorLocation               = data.bit(22);
    scc.status.instructionTracing           = data.bit(24);
    scc.status.reverseEndian                = data.bit(25);
    scc.status.floatingPointMode            = data.bit(26);
    scc.status.lowPowerMode                 = data.bit(27);
    scc.status.enable.coprocessor0          = data.bit(28);
    scc.status.enable.coprocessor1          = data.bit(29);
    scc.status.enable.coprocessor2          = data.bit(30);
    scc.status.enable.coprocessor3          = data.bit(31);
    if(floatingPointMode != scc.status.floatingPointMode) {
      fpu.setFloatingPointMode(scc.status.floatingPointMode);
    }
    context.setMode();
    if(scc.status.instructionTracing) {
      debug(unimplemented, "[CPU::setControlRegister] instructionTracing=1");
    }
  } break;
  case 13:  //cause
    scc.cause.interruptPending.bit(0) = data.bit(8);
    scc.cause.interruptPending.bit(1) = data.bit(9);
    break;
  case 14:  //exception program counter
    scc.epc = data;
    break;
  case 15:  //coprocessor revision identifier
  //scc.coprocessor.revision       = data.bit(0, 7);  //read-only
  //scc.coprocessor.implementation = data.bit(8,15);  //read-only
    break;
  case 16:  //configuration
    scc.configuration.coherencyAlgorithmKSEG0 = data.bit( 0, 1);
    scc.configuration.cu                      = data.bit( 2, 3);
    scc.configuration.bigEndian               = data.bit(15);
    scc.configuration.sysadWritebackPattern   = data.bit(24,27);
  //scc.configuration.systemClockRatio        = data.bit(28,30);  //read-only
    context.setMode();
    break;
  case 17:  //load linked address
    scc.ll = data;
    break;
  case 18:  //watchlo
    scc.watchLo.trapOnWrite               = data.bit(0);
    scc.watchLo.trapOnRead                = data.bit(1);
    scc.watchLo.physicalAddress.bit(3,31) = data.bit(3,31);
    break;
  case 19:  //watchhi
    scc.watchHi.physicalAddressExtended = data.bit(0,3);
    break;
  case 20:  //xcontext
    scc.xcontext.badVirtualAddress  = data.bit( 4,30);
    scc.xcontext.region             = data.bit(31,32);
    scc.xcontext.pageTableEntryBase = data.bit(33,63);
    break;
  case 26:  //parity error
    scc.parityError.diagnostic = data.bit(0,7);
    break;
  case 27:  //cache error (unused)
    break;
  case 28:  //taglo
    scc.tagLo.primaryCacheState          = data.bit(6, 7);
    scc.tagLo.physicalAddress.bit(12,31) = data.bit(8,27);
    break;
  case 29:  //taghi
    break;
  case 30:  //error exception program counter
    scc.epcError = data;
    break;
  }
}

auto CPU::DMFC0(r64& rt, u8 rd) -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
    if(context.bits == 32) return exception.reservedInstruction();
  }
  rt.u64 = getControlRegister(rd);
}

auto CPU::DMTC0(cr64& rt, u8 rd) -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
    if(context.bits == 32) return exception.reservedInstruction();
  }
  setControlRegister(rd, rt.u64);
}

auto CPU::ERET() -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  branch.exception();
  if(scc.status.errorLevel) {
    ipu.pc = scc.epcError;
    scc.status.errorLevel = 0;
  } else {
    ipu.pc = scc.epc;
    scc.status.exceptionLevel = 0;
  }
  scc.llbit = 0;
  context.setMode();
}

auto CPU::MFC0(r64& rt, u8 rd) -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  rt.u64 = s32(getControlRegister(rd));
}

auto CPU::MTC0(cr64& rt, u8 rd) -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  setControlRegister(rd, s32(rt.u32));
}

auto CPU::TLBP() -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  scc.index.tlbEntry = 0;  //technically undefined
  scc.index.probeFailure = 1;
  for(u32 index : range(TLB::Entries)) {
    auto& entry = tlb.entry[index];
    auto mask = ~entry.pageMask & ~0x1fff;
    if((entry.virtualAddress & mask) != (scc.tlb.virtualAddress & mask)) continue;
    if(!entry.global[0] || !entry.global[1]) {
      if(entry.addressSpaceID != scc.tlb.addressSpaceID) continue;
    }
    scc.index.tlbEntry = index;
    scc.index.probeFailure = 0;
    break;
  }
}

auto CPU::TLBR() -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  if(scc.index.tlbEntry >= TLB::Entries) return;
  scc.tlb = tlb.entry[scc.index.tlbEntry];
}

auto CPU::TLBWI() -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  if(scc.index.tlbEntry >= TLB::Entries) return;
  tlb.entry[scc.index.tlbEntry] = scc.tlb;
  debugger.tlbWrite(scc.index.tlbEntry);
}

auto CPU::TLBWR() -> void {
  if(!context.kernelMode()) {
    if(!scc.status.enable.coprocessor0) return exception.coprocessor0();
  }
  if(scc.random.index >= TLB::Entries) return;
  tlb.entry[scc.random.index] = scc.tlb;
  debugger.tlbWrite(scc.random.index);
}
