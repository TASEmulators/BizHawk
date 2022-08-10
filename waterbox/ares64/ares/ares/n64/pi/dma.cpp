auto PI::dmaRead() -> void {
  io.readLength = (io.readLength | 1) + 1;
  for(u32 address = 0; address < io.readLength; address += 2) {
    u16 data = rdram.ram.read<Half>(io.dramAddress + address);
    busWrite<Half>(io.pbusAddress + address, data);
  }
}

auto PI::dmaWrite() -> void {
  u8 mem[128];
  bool first_block = true;
  i32 length = io.writeLength+1;

  io.writeLength = 0x7F;
  if (length <= 8) io.writeLength -= io.dramAddress&7;

  while (length > 0) {
    u32 dest = io.dramAddress & 0x7FFFFE;
    i32 misalign = dest & 7;
    i32 block_len = 128 - misalign;
    i32 cur_len = min(length, block_len);

    length -= cur_len;
    if (length.bit(0)) length += 1;

    i32 rom_len = (cur_len + 1) & ~1;
    for (u32 i = 0; i < rom_len; i += 2) {
      u16 data = busRead<Half>(io.pbusAddress);
      mem[i + 0] = data >> 8;
      mem[i + 1] = data & 0xFF;
      io.pbusAddress += 2;
    }

    if (first_block) {
      if (cur_len == block_len-1) cur_len++;
      cur_len = max(cur_len-misalign, 0);
    }

    if constexpr(Accuracy::CPU::Recompiler) {
      cpu.recompiler.invalidateRange(io.dramAddress, cur_len);
    }
    for (u32 i = 0; i < cur_len; i++)
      rdram.ram.write<Byte>(io.dramAddress++, mem[i]);
    io.dramAddress = (io.dramAddress + 7) & ~7;

    first_block = false;
  }
}

auto PI::dmaFinished() -> void {
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::PI);
}
