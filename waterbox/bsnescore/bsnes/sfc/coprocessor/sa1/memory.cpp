auto SA1::idle() -> void {
  step();
}

//RTx, JMx, JSx
auto SA1::idleJump() -> void {
  //ROM access penalty cycle: does not apply to BWRAM or IRAM
  if((r.pc.d & 0x408000) == 0x008000  //00-3f,80-bf:8000-ffff
  || (r.pc.d & 0xc00000) == 0xc00000  //c0-ff:0000-ffff
  ) {
    step();
    if(rom.conflict()) step();
  }
}

//Bxx
auto SA1::idleBranch() -> void {
  if(r.pc.d & 1) idleJump();
}

auto SA1::read(uint address) -> uint8 {
  r.mar = address;
  uint8 data = r.mdr;

  if((address & 0x40fe00) == 0x002200  //00-3f,80-bf:2200-23ff
  ) {
    step();
    return r.mdr = readIOSA1(address, data);
  }

  if((address & 0x408000) == 0x008000  //00-3f,80-bf:8000-ffff
  || (address & 0xc00000) == 0xc00000  //c0-ff:0000-ffff
  ) {
    step();
    if(rom.conflict()) step();
    return r.mdr = rom.readSA1(address, data);
  }

  if((address & 0x40e000) == 0x006000  //00-3f,80-bf:6000-7fff
  || (address & 0xf00000) == 0x400000  //40-4f:0000-ffff
  || (address & 0xf00000) == 0x600000  //60-6f:0000-ffff
  ) {
    step();
    step();
    if(bwram.conflict()) step();
    if(bwram.conflict()) step();
    if((address & 1 << 22) && (address & 1 << 21)) return r.mdr = bwram.readBitmap(address, data);
    if((address & 1 << 22)) return r.mdr = bwram.readLinear(address, data);
    return r.mdr = bwram.readSA1(address, data);
  }

  if((address & 0x40f800) == 0x000000  //00-3f,80-bf:0000-07ff
  || (address & 0x40f800) == 0x003000  //00-3f,80-bf:3000-37ff
  ) {
    step();
    if(iram.conflict()) step();
    if(iram.conflict()) step();
    return r.mdr = iram.readSA1(address, data);
  }

  step();
  return data;
}

auto SA1::write(uint address, uint8 data) -> void {
  r.mar = address;
  r.mdr = data;

  if((address & 0x40fe00) == 0x002200  //00-3f,80-bf:2200-23ff
  ) {
    step();
    return writeIOSA1(address, data);
  }

  if((address & 0x408000) == 0x008000  //00-3f,80-bf:8000-ffff
  || (address & 0xc00000) == 0xc00000  //c0-ff:0000-ffff
  ) {
    step();
    if(rom.conflict()) step();
    return rom.writeSA1(address, data);
  }

  if((address & 0x40e000) == 0x006000  //00-3f,80-bf:6000-7fff
  || (address & 0xf00000) == 0x400000  //40-4f:0000-ffff
  || (address & 0xf00000) == 0x600000  //60-6f:0000-ffff
  ) {
    step();
    step();
    if(bwram.conflict()) step();
    if(bwram.conflict()) step();
    if((address & 1 << 22) && (address & 1 << 21)) return bwram.writeBitmap(address, data);
    if((address & 1 << 22)) return bwram.writeLinear(address, data);
    return bwram.writeSA1(address, data);
  }

  if((address & 0x40f800) == 0x000000  //00-3f,80-bf:0000-07ff
  || (address & 0x40f800) == 0x003000  //00-3f,80-bf:3000-37ff
  ) {
    step();
    if(iram.conflict()) step();
    if(iram.conflict()) step();
    return iram.writeSA1(address, data);
  }

  step();
  return;
}

//$230c (VDPL), $230d (VDPH) use this bus to read variable-length data.
//this is used both to keep VBR-reads from accessing MMIO registers, and
//to avoid syncing the S-CPU and SA-1*; as both chips are able to access
//these ports.
auto SA1::readVBR(uint address, uint8 data) -> uint8 {
  if((address & 0x408000) == 0x008000  //00-3f,80-bf:8000-ffff
  || (address & 0xc00000) == 0xc00000  //c0-ff:0000-ffff
  ) {
    return rom.readSA1(address, data);
  }

  if((address & 0x40e000) == 0x006000  //00-3f,80-bf:6000-7fff
  || (address & 0xf00000) == 0x400000  //40-4f:0000-ffff
  ) {
    return bwram.read(address, data);
  }

  if((address & 0x40f800) == 0x000000  //00-3f,80-bf:0000-07ff
  || (address & 0x40f800) == 0x003000  //00-3f,80-bf:3000-37ff
  ) {
    return iram.read(address, data);
  }

  return 0xff;
}

auto SA1::readDisassembler(uint address) -> uint8 {
  //TODO: this is a hack; SA1::read() advances the clock; whereas Bus::read() does not
  //the CPU and SA1 bus are identical for ROM, but have differences in BWRAM and IRAM
  return bus.read(address, r.mdr);
}
