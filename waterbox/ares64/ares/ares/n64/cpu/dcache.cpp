auto CPU::DataCache::Line::hit(u32 address) const -> bool {
  return valid && tag == (address & ~0xfff);
}

template<u32 Size> auto CPU::DataCache::Line::fill(u32 address, u64 data) -> void {
  cpu.step(40);
  valid = 1;
  dirty = 1;
  tag   = address & ~0xfff;
  //read words according to critical doubleword first scheme
  switch(address & 8) {
  case 0:
    if constexpr(Size != Dual) {
      words[0] = bus.read<Word>(tag | index | 0x0);
      words[1] = bus.read<Word>(tag | index | 0x4);
    }
    write<Size>(address, data);
    words[2] = bus.read<Word>(tag | index | 0x8);
    words[3] = bus.read<Word>(tag | index | 0xc);
    break;
  case 8:
    if constexpr(Size != Dual) {
      words[2] = bus.read<Word>(tag | index | 0x8);
      words[3] = bus.read<Word>(tag | index | 0xc);
    }
    write<Size>(address, data);
    words[0] = bus.read<Word>(tag | index | 0x0);
    words[1] = bus.read<Word>(tag | index | 0x4);
    break;
  }
}

auto CPU::DataCache::Line::fill(u32 address) -> void {
  cpu.step(40);
  valid = 1;
  dirty = 0;
  tag   = address & ~0xfff;
  //read words according to critical doubleword first scheme
  switch(address & 8) {
  case 0:
    words[0] = bus.read<Word>(tag | index | 0x0);
    words[1] = bus.read<Word>(tag | index | 0x4);
    words[2] = bus.read<Word>(tag | index | 0x8);
    words[3] = bus.read<Word>(tag | index | 0xc);
    break;
  case 8:
    words[2] = bus.read<Word>(tag | index | 0x8);
    words[3] = bus.read<Word>(tag | index | 0xc);
    words[0] = bus.read<Word>(tag | index | 0x0);
    words[1] = bus.read<Word>(tag | index | 0x4);
    break;
  }
}

auto CPU::DataCache::Line::writeBack() -> void {
  cpu.step(40);
  dirty = 0;
  bus.write<Word>(tag | index | 0x0, words[0]);
  bus.write<Word>(tag | index | 0x4, words[1]);
  bus.write<Word>(tag | index | 0x8, words[2]);
  bus.write<Word>(tag | index | 0xc, words[3]);
}

auto CPU::DataCache::line(u32 address) -> Line& {
  return lines[address >> 4 & 0x1ff];
}

template<u32 Size>
auto CPU::DataCache::Line::read(u32 address) const -> u64 {
  if constexpr(Size == Byte) { return bytes[address >> 0 & 15 ^ 3]; }
  if constexpr(Size == Half) { return halfs[address >> 1 &  7 ^ 1]; }
  if constexpr(Size == Word) { return words[address >> 2 &  3 ^ 0]; }
  if constexpr(Size == Dual) {
    u64 upper = words[address >> 2 & 2 | 0];
    u64 lower = words[address >> 2 & 2 | 1];
    return upper << 32 | lower << 0;
  }
}

template<u32 Size>
auto CPU::DataCache::Line::write(u32 address, u64 data) -> void {
  if constexpr(Size == Byte) { bytes[address >> 0 & 15 ^ 3] = data; }
  if constexpr(Size == Half) { halfs[address >> 1 &  7 ^ 1] = data; }
  if constexpr(Size == Word) { words[address >> 2 &  3 ^ 0] = data; }
  if constexpr(Size == Dual) {
    words[address >> 2 & 2 | 0] = data >> 32;
    words[address >> 2 & 2 | 1] = data >>  0;
  }
  dirty = 1;
}

template<u32 Size>
auto CPU::DataCache::read(u32 address) -> u64 {
  auto& line = this->line(address);
  if(!line.hit(address)) {
    if(line.valid && line.dirty) line.writeBack();
    line.fill(address);
  } else {
    cpu.step(1);
  }
  return line.read<Size>(address);
}

template<u32 Size>
auto CPU::DataCache::write(u32 address, u64 data) -> void {
  auto& line = this->line(address);
  if(!line.hit(address)) {
    if(line.valid && line.dirty) line.writeBack();
    return line.fill<Size>(address, data);
  } else {
    cpu.step(1);
  }
  line.write<Size>(address, data);
}

auto CPU::DataCache::power(bool reset) -> void {
  u32 index = 0;
  for(auto& line : lines) {
    line.valid = 0;
    line.dirty = 0;
    line.tag   = 0;
    line.index = index++ << 4 & 0xff0;
    for(auto& word : line.words) word = 0;
  }
}
