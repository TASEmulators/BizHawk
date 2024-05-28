auto RI::Debugger::load(Node::Object parent) -> void {
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "RI");
}

auto RI::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "RI_MODE",
    "RI_CONFIG",
    "RI_CURRENT_LOAD",
    "RI_SELECT",
    "RI_REFRESH",
    "RI_LATENCY",
    "RI_RERROR",
    "RI_WERROR",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "RI_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
