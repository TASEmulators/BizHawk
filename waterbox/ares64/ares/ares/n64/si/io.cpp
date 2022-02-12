auto SI::readWord(u32 address) -> u32 {
  address = (address & 0xfffff) >> 2;
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

auto SI::writeWord(u32 address, u32 data_) -> void {
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
    queue.insert(Queue::SI_DMA_Read, 2304);
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
    queue.insert(Queue::SI_DMA_Write, 2304);
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
