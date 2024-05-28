
auto PIF::addressCRC(u16 address) const -> n5 {
  n5 crc = 0;
  for(u32 i : range(16)) {
    n5 xor_ = crc & 0x10 ? 0x15 : 0x00;
    crc <<= 1;
    if(address & 0x8000) crc |= 1;
    address <<= 1;
    crc ^= xor_;
  }
  return crc;
}

auto PIF::dataCRC(array_view<u8> data) const -> n8 {
  n8 crc = 0;
  for(u32 i : range(33)) {
    for(u32 j : reverse(range(8))) {
      n8 xor_ = crc & 0x80 ? 0x85 : 0x00;
      crc <<= 1;
      if(i < 32) {
        if(data[i] & 1 << j) crc |= 1;
      }
      crc ^= xor_;
    }
  }
  return crc;
}

auto PIF::descramble(n4 *buf, int size) -> void {
  for(int i=size-1; i>0; i--) buf[i] -= buf[i-1] + 1;
}

auto PIF::ramReadCommand() -> u8 {
  return ram.read<Byte>(0x3f);
}

auto PIF::ramWriteCommand(u8 val) -> void {
  return ram.write<Byte>(0x3f, val);
}

auto PIF::memSwap(u32 address, n8 &val) -> void {
  n8 data = ram.read<Byte>(address);
  ram.write<Byte>(address, (u8)val);
  val = data;
}

auto PIF::memSwapSecrets() -> void {
  for (auto i: range(3)) memSwap(0x25+i, intram.osInfo[i]);
  for (auto i: range(6)) memSwap(0x32+i, intram.cpuChecksum[i]);
}

auto PIF::intA(bool dir, bool size) -> void {
  if(dir == Read) {
    if(size == Size64) {
      if(ramReadCommand() & 0x02) {
        challenge();
        return;
      }
      joyRun();
      return;
    }
  }
  if(dir == Write) {
    if(ramReadCommand() & 0x01) {
      ramWriteCommand(ramReadCommand() & ~0x01);
      joyInit();
      joyParse();
      return;
    }
    return;
  }
}

auto PIF::joyInit() -> void {
  for(auto i : range(5)) {
    intram.joyStatus[i].skip = 1;
    intram.joyStatus[i].reset = 0;
  }
}

auto PIF::joyParse() -> void {
  static constexpr bool Debug = 0;

  if constexpr(Debug) {
    print("joyParse:\n{\n");
    for(u32 y : range(8)) {
      print("  ");
      for(u32 x : range(8)) {
        print(hex(ram.read<Byte>(y * 8 + x), 2L), " ");
      }
      print("\n");
    }
    print("}\n");
  }

  u32 offset = 0;
  n3 channel = 0;  //0-5
  while(channel < 5 && offset < 64) {
    n8 send = ram.read<Byte>(offset++);
    if(send == 0xfe) break;     //end of packets
    if(send == 0xff) continue;  //alignment padding
    if(send == 0x00) { channel++; continue; }  // channel skip
    if(send == 0xfd) { // channel reset
      intram.joyStatus[channel++].reset = 1;
      continue;
    }
    u32 sendOffset = offset-1;
    n8 recv = ram.read<Byte>(offset++);
    send &= 0x3f;
    recv &= 0x3f;
    offset += send+recv;
    if(offset < 64) {
      intram.joyAddress[channel] = sendOffset;
      intram.joyStatus[channel].skip = 0;
      channel++;
    }
  }
}

auto PIF::joyRun() -> void {
  static constexpr bool Debug = 0;

  ControllerPort* controllers[4] = {
    &controllerPort1,
    &controllerPort2,
    &controllerPort3,
    &controllerPort4,
  };

  for (i32 channel=4; channel>=0; channel--) {
    if (intram.joyStatus[channel].reset) {
      if (channel < 4 && controllers[channel]->device)
        controllers[channel]->device->reset();
      continue;
    }
    if (intram.joyStatus[channel].skip) continue;

    u32 offset = intram.joyAddress[channel];
    n8 send = ram.read<Byte>(offset++);
    if(send & 0x80) continue; // skip (another way to do it)
    if(send & 0x40) { //reset (another way to do it)
      if (channel < 4 && controllers[channel]->device)
        controllers[channel]->device->reset();
      continue;
    }
    u32 recvOffset = offset;
    n8 recv = ram.read<Byte>(offset++);
    send &= 0x3f;
    recv &= 0x3f;

    n8 input[64];
    for(u32 index : range(send)) {
      input[index] = ram.read<Byte>(offset++);
    }
    n8 output[64];
    b1 valid = 0;
    b1 over = 0;

    //controller port communication
    if (channel < 4 && controllers[channel]->device) {
      n2 status = controllers[channel]->device->comm(send, recv, input, output);
      valid = status.bit(0);
      over = status.bit(1);
    }
    //cartrige joybus communication
    if (channel == 4) {
      n2 status = cartridge.joybusComm(send, recv, input, output);
      valid = status.bit(0);
      over = status.bit(1);
    }

    if(!valid) ram.write<Byte>(recvOffset, 0x80 | recv);
    if(over)   ram.write<Byte>(recvOffset, 0x40 | recv);
    if (valid) {
      for(u32 index : range(recv)) {
        ram.write<Byte>(offset++, output[index]);
      }
    }
  }

  if constexpr(Debug) {
    print("joyRun:\n[\n");
    for(u32 y : range(8)) {
      print("  ");
      for(u32 x : range(8)) {
        print(hex(ram.read<Byte>(y * 8 + x), 2L), " ");
      }
      print("\n");
    }
    print("]\n");
  }
}

auto PIF::estimateTiming() -> u32 {
  ControllerPort* controllers[4] = {
    &controllerPort1,
    &controllerPort2,
    &controllerPort3,
    &controllerPort4,
  };

  u32 cycles = 13600;
  u32 short_cmds = 0;

  u32 offset = 0;
  u32 channel = 0;
  while(offset < 64 && channel < 5) {
    n8 send = ram.read<Byte>(offset++);
    if(send == 0xfe) { short_cmds++; break; }     //end of packets
    if(send == 0x00) { short_cmds++; channel++; continue; }
    if(send == 0xfd) { short_cmds++; channel++; continue;  } //channel reset
    if(send == 0xff) { short_cmds++; continue;  } //alignment padding

    n8 recv = ram.read<Byte>(offset++);

    //clear flags from lengths
    send &= 0x3f;
    recv &= 0x3f;
    n8 input[64];
    for(u32 index : range(send)) {
      input[index] = ram.read<Byte>(offset++);
    }
    offset += recv;

    if (channel < 4) {
      if (controllers[channel]->device) {
        cycles += 22000;
      } else {
        cycles += 18000;
      }
    } else {
      //accessories(TBD)
      cycles += 20000;
    }

    channel++;
  }

  cycles += 1420 * short_cmds;
  return cycles;
}

auto PIF::challenge() -> void {
  cic.writeBit(1); cic.writeBit(0); //challenge command
  cic.readNibble(); //ignore timeout value returned by CIC (we simulate instant response)
  cic.readNibble(); //timeout high nibble
  for(u32 address : range(15)) {
    auto data = ram.read<Byte>(0x30 + address);
    cic.writeNibble(data >> 4 & 0xf);
    cic.writeNibble(data >> 0 & 0xf);
  }
  cic.readBit(); //ignore start bit
  for(u32 address : range(15)) {
    u8 data = 0;
    data |= cic.readNibble() << 4;
    data |= cic.readNibble() << 0;
    ram.write<Byte>(0x30 + address, data);
  }
}

auto PIF::mainHLE() -> void {
  constexpr u32 clocks = 10240 * 8;
  step(clocks);
  if(intram.bootTimeout > 0) intram.bootTimeout -= clocks;

  if(likely(state == Run)) {
    //cicCompare()
    return;
  }

  if(state == Init) {
    n4 hello = cic.readNibble();
    if (hello.bit(0,1) != 1) {
      debug(unusual, "[PIF::main] invalid CIC hello message ", hex(hello, 4L));
      state = Error;
      return;
    }
    if constexpr(Accuracy::PIF::RegionLock) {
      if(hello.bit(2) != (u32)system.region()) {
        const char *region[2] = { "NTSC", "PAL" };
        debug(unusual, "[PIF::main] CIC region mismatch: console is ", region[(u32)system.region()], " but cartridge is ", region[(int)hello.bit(4)]);
        state = Error;
        return;
      }
    }
    n4 osinfo = 0;
    osinfo.bit(2) = 1;              //"version" bit (unknown, always set)
    osinfo.bit(3) = hello.bit(3);   //64dd

    n4 buf[6];
    for (auto i: range(6)) buf[i] = cic.readNibble();
    for (auto i: range(2)) descramble(buf, 6);

    intram.osInfo[0].bit(4,7) = buf[0];
    intram.osInfo[0].bit(0,3) = buf[1];
    intram.osInfo[1].bit(4,7) = buf[2];
    intram.osInfo[1].bit(0,3) = buf[3];
    intram.osInfo[2].bit(4,7) = buf[4];
    intram.osInfo[2].bit(0,3) = buf[5];

    intram.osInfo[0].bit(0,3) = osinfo;
    ramWriteCommand(0x00);
    memSwapSecrets();  //show osinfo+seeds in external memory
    state = WaitLockout;
    return;
  }

  if(state == WaitLockout && (ramReadCommand() & 0x10)) {
    io.romLockout = 1;
    joyInit();
    state = WaitGetChecksum;
    return;
  }

  if(state == WaitGetChecksum && (ramReadCommand() & 0x20)) {
    memSwapSecrets();  //hide osinfo+seeds, copy+hide checksum to internal memory 
    ramWriteCommand(ramReadCommand() | 0x80);
    state = WaitCheckChecksum;
    return;
  }

  if(state == WaitCheckChecksum && (ramReadCommand() & 0x40)) {
    if (true) { // only on cold boot
      n4 buf[16];
      for (auto i: range(16)) buf[i] = cic.readNibble();
      for (auto i: range(4))  descramble(buf, 16);
      for (auto i: range(6)) {
        intram.cicChecksum[i].bit(4,7) = buf[i*2+4];
        intram.cicChecksum[i].bit(0,3) = buf[i*2+5];
      }
      intram.osInfo[0].bit(1) = 1;  //warm boot (NMI) flag (ready in case a reset is made in the future)
    }

    for (auto i: range(6)) {
      u8 data = intram.cpuChecksum[i];
      if (intram.cicChecksum[i] != data) {
        debug(unusual, "[PIF::main] invalid IPL2 checksum: ", cic.model, ":",
          hex(intram.cicChecksum[0], 2L), hex(intram.cicChecksum[1], 2L), hex(intram.cicChecksum[2], 2L),
          hex(intram.cicChecksum[3], 2L), hex(intram.cicChecksum[4], 2L), hex(intram.cicChecksum[5], 2L),
          " != cpu:", 
          hex(intram.cpuChecksum[0], 2L), hex(intram.cpuChecksum[1], 2L), hex(intram.cpuChecksum[2], 2L),
          hex(intram.cpuChecksum[3], 2L), hex(intram.cpuChecksum[4], 2L), hex(intram.cpuChecksum[5], 2L));
        state = Error;
        return;
      }
    }
    for (auto i: range(6)) intram.cpuChecksum[i] = 0;
    state = WaitTerminateBoot;
    intram.bootTimeout = 6 * 187500000;  //6 seconds
    return;
  }

  if(state == WaitTerminateBoot && (ramReadCommand() & 0x08)) {
    ramWriteCommand(0x00);
    io.resetEnabled = 1;
    state = Run;
    return;
  }

  if(state == WaitTerminateBoot && intram.bootTimeout <= 0) {
    debug(unusual, "[PIF::main] boot timeout: CPU has not sent the boot termination command within 5 seconds. Halting the CPU");
    state = Error;
    return;
  }

  if(state == Error) {
    cpu.scc.nmiPending = 1;
    return;
  }
}
