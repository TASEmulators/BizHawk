auto SI::readWord(u32 address, Thread& thread) -> u32 {
  if(address <= 0x048f'ffff) return ioRead(address);

  if (unlikely(io.ioBusy)) {
    writeForceFinish(); //technically, we should wait until Queue::SI_BUS_Write
    return io.busLatch;
  }
  return pif.read<Word>(address);
}

auto SI::ioRead(u32 address) -> u32 {
  address = (address & 0x1f) >> 2;
  n32 data;

  if(address == 0) {
    //SI_DRAM_ADDRESS
    data.bit(0,23) = io.dramAddress;
  }

  if(address == 1) {
    //SI_PIF_ADDRESS_READ64B
    data.bit(0,31) = io.readAddress;
  }

  if(address == 2) {
    //SI_INT_ADDRESS_WRITE64B
  }

  if(address == 3) {
    //SI_RESERVED
  }

  if(address == 4) {
    //SI_PIF_ADDRESS_WRITE64B
    data.bit(0,31) = io.writeAddress;
  }

  if(address == 5) {
    //SI_INT_ADDRESS_READ64B
  }

  if(address == 6) {
    //SI_STATUS
    data.bit( 0)    = io.dmaBusy;
    data.bit( 1)    = io.ioBusy;
    data.bit( 2)    = io.readPending;
    data.bit( 3)    = io.dmaError;
    data.bit( 4, 7) = io.pchState;
    data.bit( 8,11) = io.dmaState;
    data.bit(12)    = io.interrupt;
  }

  debugger.io(Read, address, data);
  return data;
}

auto SI::writeWord(u32 address, u32 data, Thread& thread) -> void {
  if(address <= 0x048f'ffff) return ioWrite(address, data);

  if(io.ioBusy) return;
  io.ioBusy = 1;
  io.busLatch = data;
  queue.insert(Queue::SI_BUS_Write, 2150*3);
  return pif.write<Word>(address, data);
}

auto SI::ioWrite(u32 address, u32 data_) -> void {
  address = (address & 0xfffff) >> 2;
  n32 data = data_;

  if(address == 0) {
    //SI_DRAM_ADDRESS
    io.dramAddress = data.bit(0,23) & ~7;
  }

  if(address == 1) {
    //SI_PIF_ADDRESS_READ64B
    io.readAddress = data.bit(0,31) & ~1;
    io.dmaBusy = 1;
    int cycles = pif.estimateTiming();
    queue.insert(Queue::SI_DMA_Read, cycles*3);
  }

  if(address == 2) {
    //SI_INT_ADDRESS_WRITE64B
  }

  if(address == 3) {
    //SI_RESERVED
  }

  if(address == 4) {
    //SI_PIF_ADDRESS_WRITE64B
    io.writeAddress = data.bit(0,31) & ~1;
    io.dmaBusy = 1;
    queue.insert(Queue::SI_DMA_Write, 4065*3);
  }

  if(address == 5) {
    //SI_INT_ADDRESS_READ64B
  }

  if(address == 6) {
    //SI_STATUS
    io.interrupt = 0;
    mi.lower(MI::IRQ::SI);
  }

  debugger.io(Write, address, data);
}

auto SI::writeFinished() -> void {
  io.ioBusy = 0;
}

auto SI::writeForceFinish() -> void {
  io.ioBusy = 0;
  queue.remove(Queue::SI_BUS_Write);
}
