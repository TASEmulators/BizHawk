auto PIF::readInt(u32 address) -> u32 {
  address &= 0x7ff;
  if(address <= 0x7bf) {
    if(io.romLockout) return 0;
    return rom.read<Word>(address);
  }
  return ram.read<Word>(address);
}

auto PIF::writeInt(u32 address, u32 data) -> void {
  address &= 0x7ff;
  if(address <= 0x7bf) {
    if(io.romLockout) return;
    return rom.write<Word>(address, data);
  }
  return ram.write<Word>(address, data);
}

auto PIF::readWord(u32 address) -> u32 {
  intA(Read, Size4);
  return readInt(address);
}

auto PIF::writeWord(u32 address, u32 data) -> void {
  writeInt(address, data);  
  intA(Write, Size4);
  mainHLE();
}

auto PIF::dmaRead(u32 address, u32 ramAddress) -> void {
  intA(Read, Size64);
  for(u32 offset = 0; offset < 64; offset += 4) {
    u32 data = readInt(address + offset);
    rdram.ram.write<Word>(ramAddress + offset, data, "SI DMA");
  }
}

auto PIF::dmaWrite(u32 address, u32 ramAddress) -> void {
  for(u32 offset = 0; offset < 64; offset += 4) {
    u32 data = rdram.ram.read<Word>(ramAddress + offset, "SI DMA");
    writeInt(address + offset, data);
  }
  intA(Write, Size64);
  mainHLE();
}
