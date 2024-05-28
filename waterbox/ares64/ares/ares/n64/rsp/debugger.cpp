#define rsp Nintendo64::rsp

auto RSP::Debugger::load(Node::Object parent) -> void {
  memory.dmem = parent->append<Node::Debugger::Memory>("RSP DMEM");
  memory.dmem->setSize(4_KiB);
  memory.dmem->setRead([&](u32 address) -> u8 {
    return rsp.dmem.read<Byte>(address);
  });
  memory.dmem->setWrite([&](u32 address, u8 data) -> void {
    return rsp.dmem.write<Byte>(address, data);
  });

  memory.imem = parent->append<Node::Debugger::Memory>("RSP IMEM");
  memory.imem->setSize(4_KiB);
  memory.imem->setRead([&](u32 address) -> u8 {
    return rsp.imem.read<Byte>(address);
  });
  memory.imem->setWrite([&](u32 address, u8 data) -> void {
    return rsp.imem.write<Byte>(address, data);
  });

  tracer.instruction = parent->append<Node::Debugger::Tracer::Instruction>("Instruction", "RSP");
  tracer.instruction->setAddressBits(12, 2);
  tracer.instruction->setDepth(64);
  if constexpr(Accuracy::RSP::Recompiler) {
    tracer.instruction->setToggle([&] {
      rsp.recompiler.reset();
      rsp.recompiler.callInstructionPrologue = tracer.instruction->enabled();
    });
  }

  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "RSP");

  if (system.homebrewMode) {
    for (auto& taintWord : taintMask.dmem) {
      taintWord = {};
    }
    for (auto& taintWord : taintMask.imem) {
      taintWord = {};
    }
  }
}

auto RSP::Debugger::unload() -> void {
  memory.dmem.reset();
  memory.imem.reset();
  tracer.instruction.reset();
  tracer.io.reset();
}

auto RSP::Debugger::instruction() -> void {
  if(unlikely(tracer.instruction->enabled())) {
    u32 address = rsp.pipeline.address & 0xfff;
    u32 instruction = rsp.pipeline.instruction;
    if(tracer.instruction->address(address)) {
      rsp.disassembler.showColors = 0;
      tracer.instruction->notify(rsp.disassembler.disassemble(address, instruction), {});
      rsp.disassembler.showColors = 1;
    }
  }
}

auto RSP::Debugger::ioSCC(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "SP_PBUS_ADDRESS",
    "SP_DRAM_ADDRESS",
    "SP_READ_LENGTH",
    "SP_WRITE_LENGTH",
    "SP_STATUS",
    "SP_DMA_FULL",
    "SP_DMA_BUSY",
    "SP_SEMAPHORE",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "SP_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}

auto RSP::Debugger::ioStatus(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "SP_PC_REG",
    "SP_IBIST",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "SP_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}

auto RSP::Debugger::dmaReadWord(u32 rdramAddress, u32 pbusRegion, u32 pbusAddress) -> void {
  if (system.homebrewMode) {
    auto& line = cpu.dcache.line(rdramAddress);
    u16 dmaMask = 0xff << (rdramAddress & 0xF);
    auto& tm = !pbusRegion ? taintMask.dmem : taintMask.imem;
    auto& taintWord = tm[pbusAddress >> 3];
    if (line.hit(rdramAddress) && (line.dirty & dmaMask)) {
      taintWord.dirty              = (line.dirty & dmaMask) >> (rdramAddress & 0x8);
      taintWord.ctxDmaRdramAddress = rdramAddress & ~0x7;
      taintWord.ctxDmaOriginPc     = rsp.dma.current.originPc;
      taintWord.ctxDmaOriginCpu    = rsp.dma.current.originCpu;
      taintWord.ctxCacheFillPc     = line.fillPc;
      taintWord.ctxCacheDirtyPc    = line.dirtyPc;
    } else {
      taintWord.dirty = 0;
    }
  }
}

auto RSP::Debugger::dmemReadWord(u12 address, int size, const char *peripheral) -> void {
  if (system.homebrewMode) {
    u8 readMask = ((1 << size) - 1) << (address & 0x7);
    auto& taintWord = taintMask.dmem[address >> 3];
    if (taintWord.dirty & readMask) {
      u32 rdramAddress = taintWord.ctxDmaRdramAddress + (address & 0x7);
      string msg = { peripheral, " reading from DMEM address 0x", hex(address), " which contains a value which is not cache coherent\n"};
      msg.append(string{ "\tCurrent RSP PC: 0x", hex(rsp.ipu.pc, 3L), "\n" });
      msg.append(string{ "\tThe value read was previously written by RSP DMA from RDRAM address 0x", hex(rdramAddress, 8L), "\n" });
      if(taintWord.ctxDmaOriginCpu) {
        msg.append(string{ "\tRSP DMA started at CPU PC: 0x", hex(taintWord.ctxDmaOriginPc, 16L), "\n" });
      } else {
        msg.append(string{ "\tRSP DMA started at RSP PC: 0x", hex(taintWord.ctxDmaOriginPc,  3L), "\n" });
      }
      msg.append(string{ "\tThe relative CPU cacheline was dirty (missing cache writeback?)\n" });
      msg.append(string{ "\tCacheline was last written at CPU PC: 0x", hex(taintWord.ctxCacheDirtyPc, 16L), "\n" });
      msg.append(string{ "\tCacheline was loaded at CPU PC: 0x", hex(taintWord.ctxCacheFillPc, 16L), "\n" });
      debug(unusual, msg);
      taintWord.dirty = 0;
    }
  }
}

auto RSP::Debugger::dmemReadUnalignedWord(u12 address, int size, const char *peripheral) -> void {
  if (system.homebrewMode) {
    u32 addressAlignedStart = address            & ~7;
    u32 addressAlignedEnd   = address + size - 1 & ~7;
    if(addressAlignedStart == addressAlignedEnd) {
      dmemReadWord(address, size, "RSP");
    } else {
      int sizeStart = addressAlignedEnd - address;
      dmemReadWord(address,             sizeStart,        "RSP");
      dmemReadWord(address + sizeStart, size - sizeStart, "RSP");
    }
  }
}

auto RSP::Debugger::dmemWriteWord(u12 address, int size, u64 value) -> void {
  if (system.homebrewMode) {
    auto& taintWord = taintMask.dmem[address >> 3];
    taintWord.dirty &= ~(((1 << size) - 1) << (address & 0x7));
  }
}

auto RSP::Debugger::dmemWriteUnalignedWord(u12 address, int size, u64 value) -> void {
  if (system.homebrewMode) {
    u32 addressAlignedStart = address            & ~7;
    u32 addressAlignedEnd   = address + size - 1 & ~7;
    if(addressAlignedStart == addressAlignedEnd) {
      dmemWriteWord(address, size, value);
    } else {
      int sizeStart = addressAlignedEnd - address;
      dmemWriteWord(address,             sizeStart,        value);
      dmemWriteWord(address + sizeStart, size - sizeStart, value);
    }
  }
}

#undef rsp
