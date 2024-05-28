auto RI::readWord(u32 address, Thread& thread) -> u32 {
  address = (address & 0x1f) >> 2;
  n32 data = 0;

  if(address == 0) {
    //RI_MODE
    data = io.mode;
  }

  if(address == 1) {
    //RI_CONFIG
    data = io.config;
  }

  if(address == 2) {
    //RI_CURRENT_LOAD
    data = io.currentLoad;
  }

  if(address == 3) {
    //RI_SELECT
    data = io.select;
    if constexpr(!Accuracy::RDRAM::Broadcasting) {
      //this register is read by IPL3 to check if RDRAM initialization should be
      //skipped. if we are forcing it to be skipped, we should also consume
      //enough cycles to not inadvertently speed up the boot process.
      //Wave Race 64 Shindou Pak Taiou Version will freeze on the N64 logo if
      //the SCC count register, which increments at half the CPU clock rate, has
      //too small a value.
      //after a cold boot on real hardware with no expansion pak and using the
      //CIC-NUS-6102 IPL3, upon reaching the test ROM's entry point the count
      //register was measured to be ~0x1184000.
      cpu.step(17'641'000 * 2);
    }
  }

  if(address == 4) {
    //RI_REFRESH
    data = io.refresh;
  }

  if(address == 5) {
    //RI_LATENCY
    data = io.latency;
  }

  if(address == 6) {
    //RI_RERROR
    data = io.readError;
  }

  if(address == 7) {
    //RI_WERROR
    data = io.writeError;
  }

  debugger.io(Read, address, data);
  return data;
}

auto RI::writeWord(u32 address, u32 data_, Thread& thread) -> void {
  address = (address & 0x1f) >> 2;
  n32 data = data_;

  if(address == 0) {
    //RI_MODE
    io.mode = data;
  }

  if(address == 1) {
    //RI_CONFIG
    io.config = data;
  }

  if(address == 2) {
    //RI_CURRENT_LOAD
    io.currentLoad = data;
  }

  if(address == 3) {
    //RI_SELECT
    io.select = data;
  }

  if(address == 4) {
    //RI_REFRESH
    io.refresh = data;
  }

  if(address == 5) {
    //RI_LATENCY
    io.latency = data;
  }

  if(address == 6) {
    //RI_RERROR
    io.readError = data;
  }

  if(address == 7) {
    //RI_WERROR
    io.writeError = data;
  }

  debugger.io(Write, address, data);
}
