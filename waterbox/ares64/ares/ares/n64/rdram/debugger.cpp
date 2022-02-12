auto RDRAM::Debugger::load(Node::Object parent) -> void {
  memory.ram = parent->append<Node::Debugger::Memory>("RDRAM");
  memory.ram->setSize(4_MiB + 4_MiB);
  memory.ram->setRead([&](u32 address) -> u8 {
    return rdram.ram.read<Byte>(address);
  });
  memory.ram->setWrite([&](u32 address, u8 data) -> void {
    return rdram.ram.write<Byte>(address, data);
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
