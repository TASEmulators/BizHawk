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

  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "RSP");
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

#undef rsp
