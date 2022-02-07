auto uPD96050::readSR() -> uint8 {
  return regs.sr >> 8;
}

auto uPD96050::writeSR(uint8 data) -> void {
}

auto uPD96050::readDR() -> uint8 {
  if(regs.sr.drc == 0) {
    //16-bit
    if(regs.sr.drs == 0) {
      regs.sr.drs = 1;
      return regs.dr >> 0;
    } else {
      regs.sr.rqm = 0;
      regs.sr.drs = 0;
      return regs.dr >> 8;
    }
  } else {
    //8-bit
    regs.sr.rqm = 0;
    return regs.dr >> 0;
  }
}

auto uPD96050::writeDR(uint8 data) -> void {
  if(regs.sr.drc == 0) {
    //16-bit
    if(regs.sr.drs == 0) {
      regs.sr.drs = 1;
      regs.dr = (regs.dr & 0xff00) | (data << 0);
    } else {
      regs.sr.rqm = 0;
      regs.sr.drs = 0;
      regs.dr = (data << 8) | (regs.dr & 0x00ff);
    }
  } else {
    //8-bit
    regs.sr.rqm = 0;
    regs.dr = (regs.dr & 0xff00) | (data << 0);
  }
}

auto uPD96050::readDP(uint12 addr) -> uint8 {
  bool hi = addr & 1;
  addr = (addr >> 1) & 2047;

  if(hi == false) {
    return dataRAM[addr] >> 0;
  } else {
    return dataRAM[addr] >> 8;
  }
}

auto uPD96050::writeDP(uint12 addr, uint8 data) -> void {
  bool hi = addr & 1;
  addr = (addr >> 1) & 2047;

  if(hi == false) {
    dataRAM[addr] = (dataRAM[addr] & 0xff00) | (data << 0);
  } else {
    dataRAM[addr] = (dataRAM[addr] & 0x00ff) | (data << 8);
  }
}
