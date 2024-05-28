auto PI::Debugger::load(Node::Object parent) -> void {
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "PI");
}

auto PI::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "PI_DRAM_ADDRESS",
    "PI_PBUS_ADDRESS",
    "PI_READ_LENGTH",
    "PI_WRITE_LENGTH",
    "PI_STATUS",
    "PI_BSD_DOM1_LAT",
    "PI_BSD_DOM1_PWD",
    "PI_BSD_DOM1_PGS",
    "PI_BSD_DOM1_RLS",
    "PI_BSD_DOM2_LAT",
    "PI_BSD_DOM2_PWD",
    "PI_BSD_DOM2_PGS",
    "PI_BSD_DOM2_RLS",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "PI_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
