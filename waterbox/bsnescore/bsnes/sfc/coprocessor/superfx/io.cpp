auto SuperFX::readIO(uint addr, uint8) -> uint8 {
  cpu.synchronizeCoprocessors();
  addr = 0x3000 | addr & 0x3ff;

  if(addr >= 0x3100 && addr <= 0x32ff) {
    return readCache(addr - 0x3100);
  }

  if(addr >= 0x3000 && addr <= 0x301f) {
    return regs.r[(addr >> 1) & 15] >> ((addr & 1) << 3);
  }

  switch(addr) {
  case 0x3030: {
    return regs.sfr >> 0;
  }

  case 0x3031: {
    uint8 r = regs.sfr >> 8;
    regs.sfr.irq = 0;
    cpu.irq(0);
    return r;
  }

  case 0x3034: {
    return regs.pbr;
  }

  case 0x3036: {
    return regs.rombr;
  }

  case 0x303b: {
    return regs.vcr;
  }

  case 0x303c: {
    return regs.rambr;
  }

  case 0x303e: {
    return regs.cbr >> 0;
  }

  case 0x303f: {
    return regs.cbr >> 8;
  }
  }

  return 0x00;
}

auto SuperFX::writeIO(uint addr, uint8 data) -> void {
  cpu.synchronizeCoprocessors();
  addr = 0x3000 | addr & 0x3ff;

  if(addr >= 0x3100 && addr <= 0x32ff) {
    return writeCache(addr - 0x3100, data);
  }

  if(addr >= 0x3000 && addr <= 0x301f) {
    uint n = (addr >> 1) & 15;
    if((addr & 1) == 0) {
      regs.r[n] = (regs.r[n] & 0xff00) | data;
    } else {
      regs.r[n] = (data << 8) | (regs.r[n] & 0xff);
    }
    if(n == 14) updateROMBuffer();

    if(addr == 0x301f) regs.sfr.g = 1;
    return;
  }

  switch(addr) {
  case 0x3030: {
    bool g = regs.sfr.g;
    regs.sfr = (regs.sfr & 0xff00) | (data << 0);
    if(g == 1 && regs.sfr.g == 0) {
      regs.cbr = 0x0000;
      flushCache();
    }
  } break;

  case 0x3031: {
    regs.sfr = (data << 8) | (regs.sfr & 0x00ff);
  } break;

  case 0x3033: {
    regs.bramr = data & 0x01;
  } break;

  case 0x3034: {
    regs.pbr = data & 0x7f;
    flushCache();
  } break;

  case 0x3037: {
    regs.cfgr = data;
  } break;

  case 0x3038: {
    regs.scbr = data;
  } break;

  case 0x3039: {
    regs.clsr = data & 0x01;
  } break;

  case 0x303a: {
    regs.scmr = data;
  } break;
  }
}
