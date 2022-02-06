auto PI::dmaRead() -> void {
  io.readLength = (io.readLength | 1) + 1;
  for(u32 address = 0; address < io.readLength; address += 2) {
    u16 data = bus.read<Half>(io.dramAddress + address);
    bus.write<Half>(io.pbusAddress + address, data);
  }
  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::PI);
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
    for (u32 i = 0; i < rom_len; i++)
      mem[i] = bus.read<Byte>(io.pbusAddress++);

    if (first_block) {
      if (cur_len == block_len-1) cur_len++;
      cur_len = max(cur_len-misalign, 0);
    }

    for (u32 i = 0; i < cur_len; i++)
      bus.write<Byte>(io.dramAddress++, mem[i]);
    io.dramAddress = (io.dramAddress + 7) & ~7;

    first_block = false;
  }

  io.dmaBusy = 0;
  io.interrupt = 1;
  mi.raise(MI::IRQ::PI);
}
