auto SM83::operand() -> uint8 {
  return read(PC++);
}

auto SM83::operands() -> uint16 {
  uint16 data = read(PC++) << 0;
  return data | read(PC++) << 8;
}

auto SM83::load(uint16 address) -> uint16 {
  uint16 data = read(address++) << 0;
  return data | read(address++) << 8;
}

auto SM83::store(uint16 address, uint16 data) -> void {
  write(address++, data >> 0);
  write(address++, data >> 8);
}

auto SM83::pop() -> uint16 {
  uint16 data = read(SP++) << 0;
  return data | read(SP++) << 8;
}

auto SM83::push(uint16 data) -> void {
  write(--SP, data >> 8);
  write(--SP, data >> 0);
}
