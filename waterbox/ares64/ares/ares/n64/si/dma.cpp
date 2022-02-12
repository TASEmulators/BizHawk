auto SI::dmaRead() -> void {
  run();
  for(u32 offset = 0; offset < 64; offset += 2) {
    u16 data = bus.read<Half>(io.readAddress + offset);
    bus.write<Half>(io.dramAddress + offset, data);
  }
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
}

auto SI::dmaWrite() -> void {
  for(u32 offset = 0; offset < 64; offset += 2) {
    u16 data = bus.read<Half>(io.dramAddress + offset);
    bus.write<Half>(io.writeAddress + offset, data);
  }
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::SI);
  run();
}
