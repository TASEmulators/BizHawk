auto MI::Debugger::load(Node::Object parent) -> void {
  tracer.interrupt = parent->append<Node::Debugger::Tracer::Notification>("Interrupt", "RCP");
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "MI");
}

auto MI::Debugger::interrupt(u8 source) -> void {
  if(unlikely(tracer.interrupt->enabled())) {
    string type = "unknown";
    if(source == (u32)MI::IRQ::SP) type = "SP";
    if(source == (u32)MI::IRQ::SI) type = "SI";
    if(source == (u32)MI::IRQ::AI) type = "AI";
    if(source == (u32)MI::IRQ::VI) type = "VI";
    if(source == (u32)MI::IRQ::PI) type = "PI";
    if(source == (u32)MI::IRQ::DP) type = "DP";
    tracer.interrupt->notify(type);
  }
}

auto MI::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "MI_INIT_MODE",
    "MI_VERSION",
    "MI_INTR",
    "MI_INTR_MASK",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "MI_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
