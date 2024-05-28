auto RDP::Debugger::load(Node::Object parent) -> void {
  tracer.command = parent->append<Node::Debugger::Tracer::Notification>("Command", "RDP");
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "RDP");
}

auto RDP::Debugger::command(string_view message) -> void {
  if(unlikely(tracer.command->enabled())) {
    tracer.command->notify(message);
  }
}

auto RDP::Debugger::ioDPC(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "DPC_START",
    "DPC_END",
    "DPC_CURRENT",
    "DPC_STATUS",
    "DPC_CLOCK",
    "DPC_BUSY",
    "DPC_PIPE_BUSY",
    "DPC_TMEM_BUSY",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "DPC_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}

auto RDP::Debugger::ioDPS(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "DPS_TBIST",
    "DPS_TEST_MODE",
    "DPS_BUFTEST_ADDR",
    "DPS_BUFTEST_DATA",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "DPS_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
