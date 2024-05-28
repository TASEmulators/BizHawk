auto VI::Debugger::load(Node::Object parent) -> void {
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "VI");
}

auto VI::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "VI_CONTROL",
    "VI_DRAM_ADDRESS",
    "VI_H_WIDTH",
    "VI_V_INTR",
    "VI_V_CURRENT_LINE",
    "VI_TIMING",
    "VI_V_SYNC",
    "VI_H_SYNC",
    "VI_H_SYNC_LEAP",
    "VI_H_VIDEO",
    "VI_V_VIDEO",
    "VI_V_BURST",
    "VI_X_SCALE",
    "VI_Y_SCALE",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "VI_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
