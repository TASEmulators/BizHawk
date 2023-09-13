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

auto PI::dmaDuration(bool read) -> u32 {
  auto len = read ? io.readLength : io.writeLength;
  len = (len | 1) + 1;

  BSD bsd;
  switch (io.pbusAddress.bit(24,31)) {
    case 0x05:               bsd = bsd2; break; 
    case range8(0x08, 0x0F): bsd = bsd2; break;
    default:                 bsd = bsd1; break;
  }

  auto pageShift = bsd.pageSize + 2;
  auto pageSize = 1 << pageShift;
  auto pageMask = pageSize - 1;
  auto pbusFirst = io.pbusAddress;
  auto pbusLast  = io.pbusAddress + len - 2;

  auto pbusFirstPage = pbusFirst >> pageShift;
  auto pbusLastPage  = pbusLast  >> pageShift;
  auto pbusPages = pbusLastPage - pbusFirstPage + 1;
  auto numBuffers = 0;
  auto partialBytes = 0;

  if (pbusFirstPage == pbusLastPage) {
    if (len == 128) numBuffers = 1;
    else partialBytes = len;
  } else {
    bool fullFirst = (pbusFirst & pageMask) == 0;
    bool fullLast  = ((pbusLast + 2) & pageMask) == 0;

    if (fullFirst) numBuffers++;
    else           partialBytes += pageSize - (pbusFirst & pageMask);
    if (fullLast)  numBuffers++;
    else           partialBytes += (pbusLast & pageMask) + 2;

    if (pbusFirstPage + 1 < pbusLastPage)
      numBuffers += (pbusPages - 2) * pageSize / 128;
  }

  u32 cycles = 0;
  cycles += (14 + bsd.latency + 1) * pbusPages;
  cycles += (bsd.pulseWidth + 1 + bsd.releaseDuration + 1) * len / 2;
  cycles += numBuffers * 28;
  cycles += partialBytes * 1;
  return cycles * 3;
}
