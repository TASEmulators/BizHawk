auto MI::readWord(u32 address) -> u32 {
  address = (address & 0xfffff) >> 2;
  n32 data;

  if(address == 0) {
    //MI_INIT_MODE
    data.bit(0,6) = io.initializeLength;
    data.bit(7)   = io.initializeMode;
    data.bit(8)   = io.ebusTestMode;
    data.bit(9)   = io.rdramRegisterSelect;
  }

  if(address == 1) {
    //MI_VERSION
    data.byte(0) = revision.io;
    data.byte(1) = revision.rac;
    data.byte(2) = revision.rdp;
    data.byte(3) = revision.rsp;
  }

  if(address == 2) {
    //MI_INTR
    data.bit(0) = irq.sp.line;
    data.bit(1) = irq.si.line;
    data.bit(2) = irq.ai.line;
    data.bit(3) = irq.vi.line;
    data.bit(4) = irq.pi.line;
    data.bit(5) = irq.dp.line;
  }

  if(address == 3) {
    //MI_INTR_MASK
    data.bit(0) = irq.sp.mask;
    data.bit(1) = irq.si.mask;
    data.bit(2) = irq.ai.mask;
    data.bit(3) = irq.vi.mask;
    data.bit(4) = irq.pi.mask;
    data.bit(5) = irq.dp.mask;
  }

  debugger.io(Read, address, data);
  return data;
}

auto MI::writeWord(u32 address, u32 data_) -> void {
  address = (address & 0xfffff) >> 2;
  n32 data = data_;

  if(address == 0) {
    //MI_INIT_MODE
    io.initializeLength = data.bit(0,6);
    if(data.bit( 7)) io.initializeMode = 0;
    if(data.bit( 8)) io.initializeMode = 1;
    if(data.bit( 9)) io.ebusTestMode = 0;
    if(data.bit(10)) io.ebusTestMode = 1;
    if(data.bit(11)) mi.lower(MI::IRQ::DP);
    if(data.bit(12)) io.rdramRegisterSelect = 0;
    if(data.bit(13)) io.rdramRegisterSelect = 1;

    if(io.initializeMode) debug(unimplemented, "[MI::writeWord] initializeMode=1");
    if(io.ebusTestMode  ) debug(unimplemented, "[MI::writeWord] ebusTestMode=1");
  }

  if(address == 1) {
    //MI_VERSION (read-only)
  }

  if(address == 2) {
    //MI_INTR (read-only)
  }

  if(address == 3) {
    //MI_INTR_MASK
    if(data.bit( 0)) irq.sp.mask = 0;
    if(data.bit( 1)) irq.sp.mask = 1;
    if(data.bit( 2)) irq.si.mask = 0;
    if(data.bit( 3)) irq.si.mask = 1;
    if(data.bit( 4)) irq.ai.mask = 0;
    if(data.bit( 5)) irq.ai.mask = 1;
    if(data.bit( 6)) irq.vi.mask = 0;
    if(data.bit( 7)) irq.vi.mask = 1;
    if(data.bit( 8)) irq.pi.mask = 0;
    if(data.bit( 9)) irq.pi.mask = 1;
    if(data.bit(10)) irq.dp.mask = 0;
    if(data.bit(11)) irq.dp.mask = 1;
    poll();
  }

  debugger.io(Write, address, data);
}
