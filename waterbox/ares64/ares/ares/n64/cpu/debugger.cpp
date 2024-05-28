auto CPU::Debugger::load(Node::Object parent) -> void {
  tracer.instruction = parent->append<Node::Debugger::Tracer::Instruction>("Instruction", "CPU");
  tracer.instruction->setAddressBits(64, 2);
  tracer.instruction->setDepth(64);
  if constexpr(Accuracy::CPU::Recompiler) {
    tracer.instruction->setToggle([&] {
      cpu.recompiler.reset();
      cpu.recompiler.callInstructionPrologue = tracer.instruction->enabled();
    });
  }

  tracer.exception = parent->append<Node::Debugger::Tracer::Notification>("Exception", "CPU");
  tracer.interrupt = parent->append<Node::Debugger::Tracer::Notification>("Interrupt", "CPU");
  tracer.tlb = parent->append<Node::Debugger::Tracer::Notification>("TLB", "CPU");
}

auto CPU::Debugger::unload() -> void {
  tracer.instruction.reset();
  tracer.exception.reset();
  tracer.interrupt.reset();
  tracer.tlb.reset();
}

auto CPU::Debugger::instruction() -> void {
  if(unlikely(tracer.instruction->enabled())) {
    u64 address = cpu.pipeline.address;
    u32 instruction = cpu.pipeline.instruction;
    if(tracer.instruction->address(address)) {
      cpu.disassembler.showColors = 0;
      tracer.instruction->notify(cpu.disassembler.disassemble(address, instruction), {});
      cpu.disassembler.showColors = 1;
    }
  }
}

auto CPU::Debugger::exception(u8 code) -> void {
  if(unlikely(tracer.exception->enabled())) {
    if(code == 0) return;  //ignore interrupt exceptions
    string type = {"unknown(0x", hex(code, 2L), ")"};
    switch(code) {
    case  0: type = "interrupt"; break;
    case  1: type = "TLB modification"; break;
    case  2: type = "TLB load"; break;
    case  3: type = "TLB store"; break;
    case  4: type = "address load"; break;
    case  5: type = "address store"; break;
    case  6: type = "bus instruction"; break;
    case  7: type = "bus data"; break;
    case  8: type = "system call"; break;
    case  9: type = "breakpoint"; break;
    case 10: type = "reserved instruction"; break;
    case 11: type = "coprocessor"; break;
    case 12: type = "arithmetic overflow"; break;
    case 13: type = "trap"; break;
    case 15: type = "floating point"; break;
    case 23: type = "watch address"; break;
    }
    type.append(string{" (PC=", hex(cpu.ipu.pc, 16L), ")"});
    tracer.exception->notify(type);
  }
}

auto CPU::Debugger::interrupt(u8 mask) -> void {
  if(unlikely(tracer.interrupt->enabled())) {
    vector<string> sources;
    if(mask & 0x01) sources.append("software 0");
    if(mask & 0x02) sources.append("software 1");
    if(mask & 0x04) sources.append("RCP");
    if(mask & 0x08) sources.append("cartridge");
    if(mask & 0x10) sources.append("reset");
    if(mask & 0x20) sources.append("read RDB");
    if(mask & 0x40) sources.append("write RDB");
    if(mask & 0x80) sources.append("timer");
    tracer.interrupt->notify(sources.merge(","));
  }
}

auto CPU::Debugger::nmi() -> void {
  if(unlikely(tracer.exception->enabled())) {
    tracer.exception->notify("NMI");
  }
}

auto CPU::Debugger::tlbWrite(u32 index) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    auto entry = cpu.tlb.entry[index & 31];
    tracer.tlb->notify({"write: ", index, " {"});
    tracer.tlb->notify({"  global:           ", entry.global[0], ",", entry.global[1]});
    tracer.tlb->notify({"  valid:            ", entry.valid[0],  ",", entry.valid[1]});
    tracer.tlb->notify({"  physical address: 0x", hex(entry.physicalAddress[0]), ",0x", hex(entry.physicalAddress[1])});
    tracer.tlb->notify({"  page mask:        0x", hex(entry.pageMask)});
    tracer.tlb->notify({"  virtual address:  0x", hex(entry.virtualAddress)});
    tracer.tlb->notify({"  address space ID: 0x", hex(entry.addressSpaceID)});
    tracer.tlb->notify({"}"});
  }
}

auto CPU::Debugger::tlbModification(u64 address) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"modification: 0x", hex(address)});
  }
}

auto CPU::Debugger::tlbLoad(u64 address, u64 physical) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"load: 0x", hex(address), " => 0x", hex(physical)});
  }
}

auto CPU::Debugger::tlbLoadInvalid(u64 address) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"load invalid: 0x", hex(address)});
  }
}

auto CPU::Debugger::tlbLoadMiss(u64 address) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"load miss: 0x", hex(address)});
  }
}

auto CPU::Debugger::tlbStore(u64 address, u64 physical) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"store: 0x", hex(address), " => 0x", hex(physical)});
  }
}

auto CPU::Debugger::tlbStoreInvalid(u64 address) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"store invalid: 0x", hex(address)});
  }
}

auto CPU::Debugger::tlbStoreMiss(u64 address) -> void {
  if(unlikely(tracer.tlb->enabled())) {
    tracer.tlb->notify({"store miss: 0x", hex(address)});
  }
}
