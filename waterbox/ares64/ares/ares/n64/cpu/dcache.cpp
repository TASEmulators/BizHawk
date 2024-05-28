auto CPU::DataCache::Line::hit(u32 address) const -> bool {
  return valid && tag == (address & ~0x0000'0fff);
}

auto CPU::DataCache::Line::fill(u32 address) -> void {
  cpu.step(40 * 2);
  valid  = 1;
  dirty  = 0;
  tag    = address & ~0x0000'0fff;
  fillPc = cpu.ipu.pc;
  cpu.busReadBurst<DCache>(tag | index, words);
}

auto CPU::DataCache::Line::writeBack() -> void {
  cpu.step(40 * 2);
  dirty = 0;
  cpu.busWriteBurst<DCache>(tag | index, words);
}

auto CPU::DataCache::line(u32 vaddr) -> Line& {
  return lines[vaddr >> 4 & 0x1ff];
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
  dirty |= ((1 << Size) - 1) << (address & 0xF);
  dirtyPc = cpu.ipu.pc;
}

template<u32 Size>
auto CPU::DataCache::read(u32 vaddr, u32 address) -> u64 {
  auto& line = this->line(vaddr);
  if(!line.hit(address)) {
    if(line.valid && line.dirty) line.writeBack();
    line.fill(address);
  } else {
    cpu.step(1 * 2);
  }
  return line.read<Size>(address);
}

auto CPU::DataCache::readDebug(u32 vaddr, u32 address) -> u8 {
  auto& line = this->line(vaddr);
  if(!line.hit(address)) {
    Thread dummyThread{};
    return bus.read<Byte>(address, dummyThread, "Ares Debugger");
  }
  return line.read<Byte>(address);
}

template<u32 Size>
auto CPU::DataCache::write(u32 vaddr, u32 address, u64 data) -> void {
  auto& line = this->line(vaddr);
  if(!line.hit(address)) {
    if(line.valid && line.dirty) line.writeBack();
    line.fill(address);
  } else {
    cpu.step(1 * 2);
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

template
auto CPU::DataCache::Line::write<Byte>(u32 address, u64 data) -> void;
