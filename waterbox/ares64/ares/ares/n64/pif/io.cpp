auto PIF::readWord(u32 address) -> u32 {
  address &= 0x7ff;
  if(address <= 0x7bf) {
    if(io.romLockout) return 0;
    return rom.read<Word>(address);
  }
  return ram.read<Word>(address);
}

auto PIF::writeWord(u32 address, u32 data) -> void {
  address &= 0x7ff;
  if(address <= 0x7bf) {
    if(io.romLockout) return;
    return rom.write<Word>(address, data);
  }
  return ram.write<Word>(address, data);
}
