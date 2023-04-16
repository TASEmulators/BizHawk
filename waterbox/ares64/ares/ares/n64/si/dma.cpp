auto SI::dmaRead() -> void {
  pif.run();
  for(u32 offset = 0; offset < 64; offset += 4) {
    u32 data = pif.readWord(io.readAddress + offset);
    rdram.ram.write<Word>(io.dramAddress + offset, data);
  }
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
}

auto SI::dmaWrite() -> void {
  for(u32 offset = 0; offset < 64; offset += 4) {
    u32 data = rdram.ram.read<Word>(io.dramAddress + offset);
    pif.writeWord(io.writeAddress + offset, data);
  }
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
  pif.run();
}
