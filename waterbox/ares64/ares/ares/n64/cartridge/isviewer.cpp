auto Cartridge::ISViewer::readHalf(u32 address) -> u16 {
  address = (address & 0xffff);
  return ram.read<Half>(address);
}

auto Cartridge::ISViewer::readWord(u32 address) -> u32 {
  address = (address & 0xffff);
  return ram.read<Word>(address);
}

auto Cartridge::ISViewer::messageChar(char c) -> void {
  if(!tracer->enabled()) return;
  tracer->notify(c);
}

auto Cartridge::ISViewer::writeHalf(u32 address, u16 data) -> void {
  address = (address & 0xffff);

  if(address == 0x16) {
    // HACK: allow printf output to work for both libultra and libdragon
    // Libultra expects a real IS-Viewer device and treats this address as a
    // pointer to the end of the buffer, reading the current value, writing N
    // bytes, then updating the buffer pointer.
    // libdragon instead treats this as a "number of bytes" register, only
    // writing an "output byte count"
    // In order to satisfy both libraries, we assume it behaves as libdragon
    // expects, and by forcing the write to never hit ram, libultra remains
    // functional.
    for(auto address : range(data)) {
      char c = ram.read<Byte>(0x20 + address);
      messageChar(c);
    }
    return;
  }

  ram.write<Half>(address, data);
}

auto Cartridge::ISViewer::writeWord(u32 address, u32 data) -> void {
  address = (address & 0xffff);
  writeHalf(address+0, data >> 16);
  writeHalf(address+2, data & 0xffff);
}

