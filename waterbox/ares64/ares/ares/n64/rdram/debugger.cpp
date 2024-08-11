auto RDRAM::Debugger::load(Node::Object parent) -> void {
  memory.ram = parent->append<Node::Debugger::Memory>("RDRAM");
  if(!system.expansionPak) {
    memory.ram->setSize(4_MiB); 
  } else {
    memory.ram->setSize(4_MiB + 4_MiB);
  }
  
  memory.ram->setRead([&](u32 address) -> u8 {
    return rdram.ram.read<Byte>(address, "Ares Debugger");
  });
  memory.ram->setWrite([&](u32 address, u8 data) -> void {
    return rdram.ram.write<Byte>(address, data, "Ares Debugger");
  });

  memory.dcache = parent->append<Node::Debugger::Memory>("DCache");
  memory.dcache->setSize(4_MiB + 4_MiB);
  memory.dcache->setRead([&](u32 address) -> u8 {
    u32 vaddr = address | 0x8000'0000;
    return cpu.dcache.readDebug(vaddr, address);
  });
  memory.dcache->setWrite([&](u32 address, u8 data) -> void {
    u32 vaddr = address | 0x8000'0000;
    auto& line = cpu.dcache.line(vaddr);
    if(line.hit(address)) {
      line.write<Byte>(address, data);
    } else {
      rdram.ram.write<Byte>(address, data, "Ares Debugger");
    }
  });

  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "RDRAM");
}

auto RDRAM::Debugger::io(bool mode, u32 chipID, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "RDRAM_DEVICE_TYPE",
    "RDRAM_DEVICE_ID",
    "RDRAM_DELAY",
    "RDRAM_MODE",
    "RDRAM_REF_INTERVAL",
    "RDRAM_REF_ROW",
    "RDRAM_RAS_INTERVAL",
    "RDRAM_MIN_INTERVAL",
    "RDRAM_ADDRESS_SELECT",
    "RDRAM_DEVICE_MANUFACTURER",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "RDRAM_UNKNOWN");
    name.append("[", chipID, "]");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}

auto RDRAM::Debugger::cacheErrorContext(string peripheral) -> string {
  if(peripheral == "CPU") {
    return { "\tCurrent CPU PC: 0x", hex(cpu.ipu.pc, 16L), "\n" };
  }
  if(peripheral == "RSP DMA") {
    if(rsp.dma.current.originCpu) {
      return { "\tRSP DMA started at CPU PC: 0x", hex(rsp.dma.current.originPc, 16L), "\n" };
    } else {
      return { "\tRSP DMA started at RSP PC: 0x", hex(rsp.dma.current.originPc,  3L), "\n" };
    }
  }
  if(peripheral == "PI DMA") {
    return { "\tPI DMA started at CPU PC: 0x", hex(pi.io.originPc, 16L), "\n" };
  }
  if(peripheral == "AI DMA") {
    return { "\tAI DMA started at CPU PC: 0x", hex(ai.io.dmaOriginPc[0], 16L), "\n" };
  }
  return "";
}

auto RDRAM::Debugger::readWord(u32 address, int size, const char *peripheral) -> void {
  if (system.homebrewMode && (address & ~15) != lastReadCacheline) {
    lastReadCacheline = address & ~15;
    auto& line = cpu.dcache.line(address);
    u16 dirtyMask = ((1 << size) - 1) << (address & 0xF);
    if (line.hit(address) && (line.dirty & dirtyMask)) {
      string msg = { peripheral, " reading from RDRAM address 0x", hex(address), " which is modified in the cache (missing cache writeback?)\n"};
      msg.append(string{ "\tCacheline was loaded at CPU PC: 0x", hex(line.fillPc, 16L), "\n" });
      msg.append(string{ "\tCacheline was last written at CPU PC: 0x", hex(line.dirtyPc, 16L), "\n" });
      msg.append(cacheErrorContext(peripheral));
      debug(unusual, msg);
    }
  }
}

auto RDRAM::Debugger::writeWord(u32 address, int size, u64 value, const char *peripheral) -> void {
  if (system.homebrewMode && (address & ~15) != lastWrittenCacheline) {
    lastWrittenCacheline = address & ~15;
    auto& line = cpu.dcache.line(address);
    if (line.hit(address)) {
      string msg = { peripheral, " writing to RDRAM address 0x", hex(address), " which is cached (missing cache invalidation?)\n"};
      msg.append(string{ "\tCacheline was loaded at CPU PC: 0x", hex(line.fillPc, 16L), "\n" });
      msg.append(cacheErrorContext(peripheral));
      debug(unusual, msg);
    }
  }
}
