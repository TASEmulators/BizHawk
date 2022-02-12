auto PI::readWord(u32 address) -> u32 {
  address = (address & 0xfffff) >> 2;
  n32 data;

  if(address == 0) {
    //PI_DRAM_ADDRESS
    data = io.dramAddress;
  }

  if(address == 1) {
    //PI_CART_ADDRESS
    data = io.pbusAddress;
  }

  if(address == 2) {
    //PI_READ_LENGTH
    data = io.readLength;
  }

  if(address == 3) {
    //PI_WRITE_LENGTH
    data = io.writeLength;
  }

  if(address == 4) {
    //PI_STATUS
    data.bit(0) = io.dmaBusy;
    data.bit(1) = io.ioBusy;
    data.bit(2) = io.error;
    data.bit(3) = io.interrupt;
  }

  if(address == 5) {
    //PI_BSD_DOM1_LAT
    data.bit(0,7) = bsd1.latency;
  }

  if(address == 6) {
    //PI_BSD_DOM1_PWD
    data.bit(0,7) = bsd1.pulseWidth;
  }

  if(address == 7) {
    //PI_BSD_DOM1_PGS
    data.bit(0,7) = bsd1.pageSize;
  }

  if(address == 8) {
    //PI_BSD_DOM1_RLS
    data.bit(0,7) = bsd1.releaseDuration;
  }

  if(address == 9) {
    //PI_BSD_DOM2_LAT
    data.bit(0,7) = bsd2.latency;
  }

  if(address == 10) {
    //PI_BSD_DOM2_PWD
    data.bit(0,7) = bsd2.pulseWidth;
  }

  if(address == 11) {
    //PI_BSD_DOM2_PGS
    data.bit(0,7) = bsd2.pageSize;
  }

  if(address == 12) {
    //PI_BSD_DOM2_RLS
    data.bit(0,7) = bsd2.releaseDuration;
  }

  debugger.io(Read, address, data);
  return data;
}

auto PI::writeWord(u32 address, u32 data_) -> void {
  address = (address & 0xfffff) >> 2;
  n32 data = data_;

  //only PI_STATUS can be written while PI is busy
  if(address != 4 && (io.dmaBusy || io.ioBusy)) {
    io.error = 1;
    return;
  }

  if(address == 0) {
    //PI_DRAM_ADDRESS
    io.dramAddress = n24(data) & ~1;
  }

  if(address == 1) {
    //PI_PBUS_ADDRESS
    io.pbusAddress = n29(data) & ~1;
  }

  if(address == 2) {
    //PI_READ_LENGTH
    io.readLength = n24(data);
    io.dmaBusy = 1;
    queue.insert(Queue::PI_DMA_Read, io.readLength * 9);
  }

  if(address == 3) {
    //PI_WRITE_LENGTH
    io.writeLength = n24(data);
    io.dmaBusy = 1;
    queue.insert(Queue::PI_DMA_Write, io.writeLength * 9);
  }

  if(address == 4) {
    //PI_STATUS
    if(data.bit(0)) {
      io.dmaBusy = 0;
      io.error = 0;
      queue.remove(Queue::PI_DMA_Read);
      queue.remove(Queue::PI_DMA_Write);
    }
    if(data.bit(1)) {
      io.interrupt = 0;
      mi.lower(MI::IRQ::PI);
    }
  }

  if(address == 5) {
    //PI_BSD_DOM1_LAT
    bsd1.latency = data.bit(0,7);
  }

  if(address == 6) {
    //PI_BSD_DOM1_PWD
    bsd1.pulseWidth = data.bit(0,7);
  }

  if(address == 7) {
    //PI_BSD_DOM1_PGS
    bsd1.pageSize = data.bit(0,7);
  }

  if(address == 8) {
    //PI_BSD_DOM1_RLS
    bsd1.releaseDuration = data.bit(0,7);
  }

  if(address == 9) {
    //PI_BSD_DOM2_LAT
    bsd2.latency = data.bit(0,7);
  }

  if(address == 10) {
    //PI_BSD_DOM2_PWD
    bsd2.pulseWidth = data.bit(0,7);
  }

  if(address == 11) {
    //PI_BSD_DOM2_PGS
    bsd2.pageSize = data.bit(0,7);
  }

  if(address == 12) {
    //PI_BSD_DOM2_RLS
    bsd2.releaseDuration = data.bit(0,7);
  }

  debugger.io(Write, address, data);
}
