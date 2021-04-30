auto SuperFX::step(uint clocks) -> void {
  if(regs.romcl) {
    regs.romcl -= min(clocks, regs.romcl);
    if(regs.romcl == 0) {
      regs.sfr.r = 0;
      regs.romdr = read((regs.rombr << 16) + regs.r[14]);
    }
  }

  if(regs.ramcl) {
    regs.ramcl -= min(clocks, regs.ramcl);
    if(regs.ramcl == 0) {
      write(0x700000 + (regs.rambr << 16) + regs.ramar, regs.ramdr);
    }
  }

  clock += clocks * (uint64_t)cpu.frequency;
  synchronizeCPU();
}

auto SuperFX::syncROMBuffer() -> void {
  if(regs.romcl) step(regs.romcl);
}

auto SuperFX::readROMBuffer() -> uint8 {
  syncROMBuffer();
  return regs.romdr;
}

auto SuperFX::updateROMBuffer() -> void {
  regs.sfr.r = 1;
  regs.romcl = regs.clsr ? 5 : 6;
}

auto SuperFX::syncRAMBuffer() -> void {
  if(regs.ramcl) step(regs.ramcl);
}

auto SuperFX::readRAMBuffer(uint16 addr) -> uint8 {
  syncRAMBuffer();
  return read(0x700000 + (regs.rambr << 16) + addr);
}

auto SuperFX::writeRAMBuffer(uint16 addr, uint8 data) -> void {
  syncRAMBuffer();
  regs.ramcl = regs.clsr ? 5 : 6;
  regs.ramar = addr;
  regs.ramdr = data;
}
