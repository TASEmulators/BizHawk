//ROM / RAM access from the S-CPU

auto SuperFX::CPUROM::data() -> uint8* {
  return superfx.rom.data();
}

auto SuperFX::CPUROM::size() const -> uint {
  return superfx.rom.size();
}

auto SuperFX::CPUROM::read(uint addr, uint8 data) -> uint8 {
  if(superfx.regs.sfr.g && superfx.regs.scmr.ron) {
    static const uint8 vector[16] = {
      0x00, 0x01, 0x00, 0x01, 0x04, 0x01, 0x00, 0x01,
      0x00, 0x01, 0x08, 0x01, 0x00, 0x01, 0x0c, 0x01,
    };
    return vector[addr & 15];
  }
  return superfx.rom.read(addr, data);
}

auto SuperFX::CPUROM::write(uint addr, uint8 data) -> void {
  superfx.rom.write(addr, data);
}

auto SuperFX::CPURAM::data() -> uint8* {
  return superfx.ram.data();
}

auto SuperFX::CPURAM::size() const -> uint {
  return superfx.ram.size();
}

auto SuperFX::CPURAM::read(uint addr, uint8 data) -> uint8 {
  if(superfx.regs.sfr.g && superfx.regs.scmr.ran) return data;
  return superfx.ram.read(addr, data);
}

auto SuperFX::CPURAM::write(uint addr, uint8 data) -> void {
  superfx.ram.write(addr, data);
}
