auto Cartridge::ISViewer::readWord(u32 address) -> u32 {
  u32 data = ram.read<Word>(address);
  address = (address & 0xffff) >> 2;

  if(address == 0) {
    data = 0x49533634;  //'IS64'
  }

  return data;
}

auto Cartridge::ISViewer::writeWord(u32 address, u32 data) -> void {
  ram.write<Word>(address, data);
  address = (address & 0xffff) >> 2;

  if(address == 5) {
    for(auto address : range(u16(data))) {
      char c = ram.read<Byte>(0x20 + address);
      fputc(c, stdout);
    }
  }
}
