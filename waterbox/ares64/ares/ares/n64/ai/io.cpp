auto AI::readWord(u32 address, Thread& thread) -> u32 {
  address = (address & 0x1f) >> 2;
  n32 data;

  if(address != 3) {
    //AI_LENGTH (mirrored)
    data.bit(0,17) = io.dmaLength[0];
  }

  if(address == 3) {
    //AI_STATUS
    data.bit( 0) = io.dmaCount > 1;
    data.bit(20) = 1;
    data.bit(24) = 1;
    data.bit(25) = io.dmaEnable;
    data.bit(30) = io.dmaCount > 0;
    data.bit(31) = io.dmaCount > 1;
  }

  debugger.io(Read, address, data);
  return data;
}

auto AI::writeWord(u32 address, u32 data_, Thread& thread) -> void {
  address = (address & 0x1f) >> 2;
  n32 data = data_;

  if(address == 0) {
    //AI_DRAM_ADDRESS
    if(io.dmaCount < 2) {
      io.dmaAddress[io.dmaCount] = data.bit(0,23) & ~7;
    }
  }

  if(address == 1) {
    //AI_LENGTH
    n18 length = data.bit(0,17) & ~7;
    if(io.dmaCount < 2) {
      if(io.dmaCount == 0) mi.raise(MI::IRQ::AI);
      io.dmaLength[io.dmaCount] = length;
      io.dmaOriginPc[io.dmaCount] = cpu.ipu.pc;
      io.dmaCount++;
    }
  }

  if(address == 2) {
    //AI_CONTROL
    io.dmaEnable = data.bit(0);
  }

  if(address == 3) {
    //AI_STATUS
    mi.lower(MI::IRQ::AI);
  }

  if(address == 4) {
    //AI_DACRATE
    auto frequency = dac.frequency;
    io.dacRate = data.bit(0,13);
    dac.frequency = max(1, system.videoFrequency() / (io.dacRate + 1));
    dac.period = system.frequency() / dac.frequency;
    if(frequency != dac.frequency) stream->setFrequency(dac.frequency);
  }

  if(address == 5) {
    //AI_BITRATE
    io.bitRate = data.bit(0,3);
    dac.precision = io.bitRate + 1;
  }

  debugger.io(Write, address, data);
}
