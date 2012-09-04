#ifdef ARMDSP_CPP

uint8 ArmDSP::bus_read(uint32 addr) {
  switch(addr & 0xe0000000) {
  case 0x00000000: return programROM[addr & 0x0001ffff];
  case 0x20000000: return pipeline.mdr.opcode >> ((addr & 3) << 3);
  case 0x40000000: break;  //MMIO
  case 0x60000000: return 0x40404001 >> ((addr & 3) << 3);
  case 0x80000000: return pipeline.mdr.opcode >> ((addr & 3) << 3);
  case 0xa0000000: return dataROM[addr & 0x00007fff];
  case 0xc0000000: return pipeline.mdr.opcode >> ((addr & 3) << 3);
  case 0xe0000000: return programRAM[addr & 0x00003fff];
  }

  addr &= 0xe000003f;

  if(addr == 0x40000010) {
    if(bridge.cputoarm.ready) {
      bridge.cputoarm.ready = false;
      return bridge.cputoarm.data;
    }
  }

  if(addr == 0x40000020) {
    return bridge.status();
  }

  return 0x00;
}

void ArmDSP::bus_write(uint32 addr, uint8 data) {
  switch(addr & 0xe0000000) {
  case 0x40000000: break;  //MMIO
  case 0xe0000000: programRAM[addr & 0x00003fff] = data; return;
  default: return;
  }

  addr &= 0xe000003f;

  if(addr == 0x40000000) {
    bridge.armtocpu.ready = true;
    bridge.armtocpu.data = data;
    return;
  }

  if(addr == 0x40000020) bridge.timerlatch = (bridge.timerlatch & 0xffff00) | (data <<  0);
  if(addr == 0x40000024) bridge.timerlatch = (bridge.timerlatch & 0xff00ff) | (data <<  8);
  if(addr == 0x40000028) bridge.timerlatch = (bridge.timerlatch & 0x00ffff) | (data << 16);

  if(addr == 0x40000028) {
    bridge.timer = bridge.timerlatch;
    bridge.busy = !bridge.timer;
  }
}

uint32 ArmDSP::bus_readbyte(uint32 addr) {
  tick();
  return bus_read(addr);
}

void ArmDSP::bus_writebyte(uint32 addr, uint32 data) {
  tick();
  return bus_write(addr, data);
}

uint32 ArmDSP::bus_readword(uint32 addr) {
  tick();
  addr &= ~3;
  return (
    (bus_read(addr + 0) <<  0)
  | (bus_read(addr + 1) <<  8)
  | (bus_read(addr + 2) << 16)
  | (bus_read(addr + 3) << 24)
  );
}

void ArmDSP::bus_writeword(uint32 addr, uint32 data) {
  tick();
  addr &= ~3;
  bus_write(addr + 0, data >>  0);
  bus_write(addr + 1, data >>  8);
  bus_write(addr + 2, data >> 16);
  bus_write(addr + 3, data >> 24);
}

#endif
