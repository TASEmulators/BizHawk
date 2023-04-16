auto DD::Debugger::load(Node::Object parent) -> void {
  tracer.io = parent->append<Node::Debugger::Tracer::Notification>("I/O", "DD");
}

auto DD::Debugger::io(bool mode, u32 address, u32 data) -> void {
  static const vector<string> registerNames = {
    "ASIC_DATA",
    "ASIC_MISC_REG",
    "ASIC_STATUS|ASIC_CMD",
    "ASIC_CUR_TK",
    "ASIC_BM_STATUS|ASIC_BM_CTL",
    "ASIC_ERR_SECTOR",
    "ASIC_SEQ_STATUS|ASIC_SEQ_CTL",
    "ASIC_CUR_SECTOR",
    "ASIC_HARD_RESET",
    "ASIC_C1_S0",
    "ASIC_HOST_SECBYE",
    "ASIC_C1_S2",
    "ASIC_SEC_BYTE",
    "ASIC_C1_S4",
    "ASIC_C1_S6",
    "ASIC_CUR_ADDRESS",
    "ASIC_ID_REG",
    "ASIC_TEST_REG",
    "ASIC_TEST_PIN_SEL",
  };

  if(unlikely(tracer.io->enabled())) {
    string message;
    string name = registerNames(address, "ASIC_UNKNOWN");
    if(mode == Read) {
      message = {name.split("|").first(), " => ", hex(data, 8L)};
    }
    if(mode == Write) {
      message = {name.split("|").last(), " <= ", hex(data, 8L)};
    }
    tracer.io->notify(message);
  }
}
