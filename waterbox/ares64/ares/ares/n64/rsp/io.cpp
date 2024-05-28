auto RSP::readWord(u32 address, Thread& thread) -> u32 {
  if(address <= 0x0403'ffff) {
    if(address & 0x1000) return imem.read<Word>(address);
    else                 return dmem.read<Word>(address);
  }
  return ioRead(address, thread);
}

auto RSP::ioRead(u32 address, Thread &thread) -> u32 {
  address = (address & 0x1f) >> 2;
  n32 data;

  if(address == 0) {
    //SP_PBUS_ADDRESS
    data.bit( 0,11) = dma.current.pbusAddress;
    data.bit(12)    = dma.current.pbusRegion;
  }

  if(address == 1) {
    //SP_DRAM_ADDRESS
    data.bit(0,23) = dma.current.dramAddress;
  }

  if(address == 2 || address == 3) {
    //SP_READ_LENGTH or SP_WRITE_LENGTH
    data.bit( 0,11) = dma.current.length;
    data.bit(12,19) = dma.current.count;
    data.bit(20,31) = dma.current.skip;
  }

  if(address == 4) {
    //SP_STATUS
    data.bit( 0) = status.halted;
    data.bit( 1) = status.broken;
    data.bit( 2) = dma.busy.any();
    data.bit( 3) = dma.full.any();
    data.bit( 4) = status.full;
    data.bit( 5) = status.singleStep;
    data.bit( 6) = status.interruptOnBreak;
    data.bit( 7) = status.signal[0];
    data.bit( 8) = status.signal[1];
    data.bit( 9) = status.signal[2];
    data.bit(10) = status.signal[3];
    data.bit(11) = status.signal[4];
    data.bit(12) = status.signal[5];
    data.bit(13) = status.signal[6];
    data.bit(14) = status.signal[7];
  }

  if(address == 5) {
    //SP_DMA_FULL
    data.bit(0) = dma.full.any();
  }

  if(address == 6) {
    //SP_DMA_BUSY
    data.bit(0) = dma.busy.any();
  }

  if(address == 7) {
    //SP_SEMAPHORE
    data.bit(0) = status.semaphore;
    status.semaphore = 1;
  }

  debugger.ioSCC(Read, address, data);
  return data;
}

auto RSP::writeWord(u32 address, u32 data, Thread& thread) -> void {
  if(address <= 0x0403'ffff) {
    if(address & 0x1000) return recompiler.invalidate(address & 0xfff), imem.write<Word>(address, data);
    else                 return dmem.write<Word>(address, data);
  }
  return ioWrite(address, data, thread);
}

auto RSP::ioWrite(u32 address, u32 data_, Thread& thread) -> void {
  address = (address & 0x1f) >> 2;
  n32 data = data_;

  if(address == 0) {
    //SP_PBUS_ADDRESS
    dma.pending.pbusAddress.bit(3,11) = data.bit( 3,11);
    dma.pending.pbusRegion            = data.bit(12);
  }

  if(address == 1) {
    //SP_DRAM_ADDRESS
    dma.pending.dramAddress.bit(3,23) = data.bit(3,23);
  }

  if(address == 2) {
    //SP_READ_LENGTH
    dma.pending.length.bit(3,11) = data.bit( 3,11);
    dma.pending.count            = data.bit(12,19);
    dma.pending.skip.bit(3,11)   = data.bit(23,31);
    dma.pending.originCpu = &thread != this;
    dma.pending.originPc = dma.pending.originCpu ? cpu.ipu.pc : (u64)rsp.ipu.r[31].u32;
    dma.full.read  = 1;
    dma.full.write = 0;
    // printf("RSP DMA Read: %08x => %08x %08x\n", dma.pending.dramAddress, dma.pending.pbusAddress, dma.pending.length);
    dmaTransferStart();
  }

  if(address == 3) {
    //SP_WRITE_LENGTH
    dma.pending.length.bit(3,11) = data.bit( 3,11);
    dma.pending.count            = data.bit(12,19);
    dma.pending.skip.bit(3,11)   = data.bit(23,31);
    dma.pending.originCpu = &thread != this;
    dma.pending.originPc = dma.pending.originCpu ? cpu.ipu.pc : (u64)rsp.ipu.r[31].u32;
    dma.full.write = 1;
    dma.full.read  = 0;
    dmaTransferStart();
  }

  if(address == 4) {
    //SP_STATUS
    if(data.bit( 0) && !data.bit( 1)) status.halted = 0;
    if(data.bit( 1) && !data.bit( 0)) status.halted = 1;
    if(data.bit( 2)) status.broken = 0;
    if(data.bit( 3) && !data.bit( 4)) mi.lower(MI::IRQ::SP);
    if(data.bit( 4) && !data.bit( 3)) mi.raise(MI::IRQ::SP);
    if(data.bit( 5) && !data.bit( 6)) status.singleStep = 0;
    if(data.bit( 6) && !data.bit( 5)) status.singleStep = 1;
    if(data.bit( 7) && !data.bit( 8)) status.interruptOnBreak = 0;
    if(data.bit( 8) && !data.bit( 7)) status.interruptOnBreak = 1;
    if(data.bit( 9) && !data.bit(10)) status.signal[0] = 0;
    if(data.bit(10) && !data.bit( 9)) status.signal[0] = 1;
    if(data.bit(11) && !data.bit(12)) status.signal[1] = 0;
    if(data.bit(12) && !data.bit(11)) status.signal[1] = 1;
    if(data.bit(13) && !data.bit(14)) status.signal[2] = 0;
    if(data.bit(14) && !data.bit(13)) status.signal[2] = 1;
    if(data.bit(15) && !data.bit(16)) status.signal[3] = 0;
    if(data.bit(16) && !data.bit(15)) status.signal[3] = 1;
    if(data.bit(17) && !data.bit(18)) status.signal[4] = 0;
    if(data.bit(18) && !data.bit(17)) status.signal[4] = 1;
    if(data.bit(19) && !data.bit(20)) status.signal[5] = 0;
    if(data.bit(20) && !data.bit(19)) status.signal[5] = 1;
    if(data.bit(21) && !data.bit(22)) status.signal[6] = 0;
    if(data.bit(22) && !data.bit(21)) status.signal[6] = 1;
    if(data.bit(23) && !data.bit(24)) status.signal[7] = 0;
    if(data.bit(24) && !data.bit(23)) status.signal[7] = 1;
  }

  if(address == 5) {
    //SP_DMA_FULL (read-only)
  }

  if(address == 6) {
    //SP_DMA_BUSY (read-only)
  }

  if(address == 7) {
    //SP_SEMAPHORE
    status.semaphore = 0;
  }

  debugger.ioSCC(Write, address, data);
}

auto RSP::Status::readWord(u32 address, Thread& thread) -> u32 {
  address = (address & 0x1f) >> 2;
  n32 data;

  if(address == 0) {
    //SP_PC_REG
    if(halted) {
      data.bit(0,11) = self.ipu.pc;
    } else {
      data.bit(0,11) = random();
    }
  }

  if(address == 1) {
    //SP_IBIST
  }

  self.debugger.ioStatus(Read, address, data);
  return data;
}

auto RSP::Status::writeWord(u32 address, u32 data_, Thread& thread) -> void {
  address = (address & 0x1f) >> 2;
  n32 data = data_;

  if(address == 0) {
    //SP_PC_REG
    self.ipu.pc = data.bit(0,11) & ~3;
    self.branch.reset();
  }

  if(address == 1) {
    //SP_IBIST
  }

  self.debugger.ioStatus(Write, address, data);
}
