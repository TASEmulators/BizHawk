auto AI::Debugger::load(Node::Object parent) -> void {
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "AI");
}

auto AI::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "AI_DRAM_ADDRESS",
    "AI_LENGTH",
    "AI_CONTROL",
    "AI_STATUS",
    "AI_DACRATE",
    "AI_BITRATE",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "AI_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
