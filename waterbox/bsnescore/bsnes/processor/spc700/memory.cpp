auto SPC700::fetch() -> uint8 {
  return read(PC++);
}

auto SPC700::load(uint8 address) -> uint8 {
  return read(PF << 8 | address);
}

auto SPC700::store(uint8 address, uint8 data) -> void {
  return write(PF << 8 | address, data);
}

auto SPC700::pull() -> uint8 {
  return read(1 << 8 | ++S);
}

auto SPC700::push(uint8 data) -> void {
  return write(1 << 8 | S--, data);
}
