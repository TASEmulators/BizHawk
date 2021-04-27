auto SuperFX::read(uint addr, uint8 data) -> uint8 {
  if((addr & 0xc00000) == 0x000000) {  //$00-3f:0000-7fff,:8000-ffff
    while(!regs.scmr.ron) {
      step(6);
      synchronizeCPU();
      if(synchronizing()) break;
    }
    return rom.read((((addr & 0x3f0000) >> 1) | (addr & 0x7fff)) & romMask);
  }

  if((addr & 0xe00000) == 0x400000) {  //$40-5f:0000-ffff
    while(!regs.scmr.ron) {
      step(6);
      synchronizeCPU();
      if(synchronizing()) break;
    }
    return rom.read(addr & romMask);
  }

  if((addr & 0xe00000) == 0x600000) {  //$60-7f:0000-ffff
    while(!regs.scmr.ran) {
      step(6);
      synchronizeCPU();
      if(synchronizing()) break;
    }
    return ram.read(addr & ramMask);
  }

  return data;
}

auto SuperFX::write(uint addr, uint8 data) -> void {
  if((addr & 0xe00000) == 0x600000) {  //$60-7f:0000-ffff
    while(!regs.scmr.ran) {
      step(6);
      synchronizeCPU();
      if(synchronizing()) break;
    }
    return ram.write(addr & ramMask, data);
  }
}

auto SuperFX::readOpcode(uint16 addr) -> uint8 {
  uint16 offset = addr - regs.cbr;
  if(offset < 512) {
    if(cache.valid[offset >> 4] == false) {
      uint dp = offset & 0xfff0;
      uint sp = (regs.pbr << 16) + ((regs.cbr + dp) & 0xfff0);
      for(uint n : range(16)) {
        step(regs.clsr ? 5 : 6);
        cache.buffer[dp++] = read(sp++);
      }
      cache.valid[offset >> 4] = true;
    } else {
      step(regs.clsr ? 1 : 2);
    }
    return cache.buffer[offset];
  }

  if(regs.pbr <= 0x5f) {
    //$00-5f:0000-ffff ROM
    syncROMBuffer();
    step(regs.clsr ? 5 : 6);
    return read(regs.pbr << 16 | addr);
  } else {
    //$60-7f:0000-ffff RAM
    syncRAMBuffer();
    step(regs.clsr ? 5 : 6);
    return read(regs.pbr << 16 | addr);
  }
}

auto SuperFX::peekpipe() -> uint8 {
  uint8 result = regs.pipeline;
  regs.pipeline = readOpcode(regs.r[15]);
  regs.r[15].modified = false;
  return result;
}

auto SuperFX::pipe() -> uint8 {
  uint8 result = regs.pipeline;
  regs.pipeline = readOpcode(++regs.r[15]);
  regs.r[15].modified = false;
  return result;
}

auto SuperFX::flushCache() -> void {
  for(uint n : range(32)) cache.valid[n] = false;
}

auto SuperFX::readCache(uint16 addr) -> uint8 {
  addr = (addr + regs.cbr) & 511;
  return cache.buffer[addr];
}

auto SuperFX::writeCache(uint16 addr, uint8 data) -> void {
  addr = (addr + regs.cbr) & 511;
  cache.buffer[addr] = data;
  if((addr & 15) == 15) cache.valid[addr >> 4] = true;
}
