auto HG51B::readRegister(uint7 address) -> uint24 {
  switch(address) {
  case 0x01: return r.mul >> 24 & 0xffffff;
  case 0x02: return r.mul >>  0 & 0xffffff;
  case 0x03: return r.mdr;
  case 0x08: return r.rom;
  case 0x0c: return r.ram;
  case 0x13: return r.mar;
  case 0x1c: return r.dpr;
  case 0x20: return r.pc;
  case 0x28: return r.p;
  case 0x2e:
    io.bus.enable  = 1;
    io.bus.reading = 1;
    io.bus.pending = 1 + io.wait.rom;
    io.bus.address = r.mar;
    return 0x000000;
  case 0x2f:
    io.bus.enable  = 1;
    io.bus.reading = 1;
    io.bus.pending = 1 + io.wait.ram;
    io.bus.address = r.mar;
    return 0x000000;

  //constant registers
  case 0x50: return 0x000000;
  case 0x51: return 0xffffff;
  case 0x52: return 0x00ff00;
  case 0x53: return 0xff0000;
  case 0x54: return 0x00ffff;
  case 0x55: return 0xffff00;
  case 0x56: return 0x800000;
  case 0x57: return 0x7fffff;
  case 0x58: return 0x008000;
  case 0x59: return 0x007fff;
  case 0x5a: return 0xff7fff;
  case 0x5b: return 0xffff7f;
  case 0x5c: return 0x010000;
  case 0x5d: return 0xfeffff;
  case 0x5e: return 0x000100;
  case 0x5f: return 0x00feff;

  //general purpose registers
  case 0x60: case 0x70: return r.gpr[ 0];
  case 0x61: case 0x71: return r.gpr[ 1];
  case 0x62: case 0x72: return r.gpr[ 2];
  case 0x63: case 0x73: return r.gpr[ 3];
  case 0x64: case 0x74: return r.gpr[ 4];
  case 0x65: case 0x75: return r.gpr[ 5];
  case 0x66: case 0x76: return r.gpr[ 6];
  case 0x67: case 0x77: return r.gpr[ 7];
  case 0x68: case 0x78: return r.gpr[ 8];
  case 0x69: case 0x79: return r.gpr[ 9];
  case 0x6a: case 0x7a: return r.gpr[10];
  case 0x6b: case 0x7b: return r.gpr[11];
  case 0x6c: case 0x7c: return r.gpr[12];
  case 0x6d: case 0x7d: return r.gpr[13];
  case 0x6e: case 0x7e: return r.gpr[14];
  case 0x6f: case 0x7f: return r.gpr[15];
  }

  return 0x000000;  //verified
}

auto HG51B::writeRegister(uint7 address, uint24 data) -> void {
  switch(address) {
  case 0x01: r.mul = r.mul &  0xffffffull | data << 24; return;
  case 0x02: r.mul = r.mul & ~0xffffffull | data <<  0; return;
  case 0x03: r.mdr = data; return;
  case 0x08: r.rom = data; return;
  case 0x0c: r.ram = data; return;
  case 0x13: r.mar = data; return;
  case 0x1c: r.dpr = data; return;
  case 0x20: r.pc = data; return;
  case 0x28: r.p = data; return;
  case 0x2e:
    io.bus.enable  = 1;
    io.bus.writing = 1;
    io.bus.pending = 1 + io.wait.rom;
    io.bus.address = r.mar;
    return;
  case 0x2f:
    io.bus.enable  = 1;
    io.bus.writing = 1;
    io.bus.pending = 1 + io.wait.ram;
    io.bus.address = r.mar;
    return;

  case 0x60: case 0x70: r.gpr[ 0] = data; return;
  case 0x61: case 0x71: r.gpr[ 1] = data; return;
  case 0x62: case 0x72: r.gpr[ 2] = data; return;
  case 0x63: case 0x73: r.gpr[ 3] = data; return;
  case 0x64: case 0x74: r.gpr[ 4] = data; return;
  case 0x65: case 0x75: r.gpr[ 5] = data; return;
  case 0x66: case 0x76: r.gpr[ 6] = data; return;
  case 0x67: case 0x77: r.gpr[ 7] = data; return;
  case 0x68: case 0x78: r.gpr[ 8] = data; return;
  case 0x69: case 0x79: r.gpr[ 9] = data; return;
  case 0x6a: case 0x7a: r.gpr[10] = data; return;
  case 0x6b: case 0x7b: r.gpr[11] = data; return;
  case 0x6c: case 0x7c: r.gpr[12] = data; return;
  case 0x6d: case 0x7d: r.gpr[13] = data; return;
  case 0x6e: case 0x7e: r.gpr[14] = data; return;
  case 0x6f: case 0x7f: r.gpr[15] = data; return;
  }
}
