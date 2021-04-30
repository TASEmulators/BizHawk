//DMA clock divider
auto CPU::dmaCounter() const -> uint {
  return counter.cpu & 7;
}

//joypad auto-poll clock divider
auto CPU::joypadCounter() const -> uint {
  return counter.cpu & 127;
}

auto CPU::stepOnce() -> void {
  counter.cpu += 2;
  tick();
  if(hcounter() & 2) nmiPoll(), irqPoll();
  if(joypadCounter() == 0) joypadEdge();
}

template<uint Clocks, bool Synchronize>
auto CPU::step() -> void {
  static_assert(Clocks == 2 || Clocks == 4 || Clocks == 6 || Clocks == 8 || Clocks == 10 || Clocks == 12);

  for(auto coprocessor : coprocessors) {
    if(coprocessor == &icd || coprocessor == &msu1) continue;
    coprocessor->clock -= Clocks * (uint64)coprocessor->frequency;
  }

  if(overclocking.target) {
    overclocking.counter += Clocks;
    if(overclocking.counter < overclocking.target) {
      if constexpr(Synchronize) {
        if(configuration.hacks.coprocessor.delayedSync) return;
        synchronizeCoprocessors();
      }
      return;
    }
  }

  if constexpr(Clocks >=  2) stepOnce();
  if constexpr(Clocks >=  4) stepOnce();
  if constexpr(Clocks >=  6) stepOnce();
  if constexpr(Clocks >=  8) stepOnce();
  if constexpr(Clocks >= 10) stepOnce();
  if constexpr(Clocks >= 12) stepOnce();

  smp.clock -= Clocks * (uint64)smp.frequency;
  ppu.clock -= Clocks;
  for(auto coprocessor : coprocessors) {
    if(coprocessor != &icd && coprocessor != &msu1) continue;
    coprocessor->clock -= Clocks * (uint64)coprocessor->frequency;
  }

  if(!status.dramRefresh && hcounter() >= status.dramRefreshPosition) {
    //note: pattern should technically be 5-3, 5-3, 5-3, 5-3, 5-3 per logic analyzer
    //result averages out the same as no coprocessor polls refresh() at > frequency()/2
    status.dramRefresh = 1; step<6,0>(); status.dramRefresh = 2; step<2,0>(); aluEdge();
    status.dramRefresh = 1; step<6,0>(); status.dramRefresh = 2; step<2,0>(); aluEdge();
    status.dramRefresh = 1; step<6,0>(); status.dramRefresh = 2; step<2,0>(); aluEdge();
    status.dramRefresh = 1; step<6,0>(); status.dramRefresh = 2; step<2,0>(); aluEdge();
    status.dramRefresh = 1; step<6,0>(); status.dramRefresh = 2; step<2,0>(); aluEdge();
  }

  if(!status.hdmaSetupTriggered && hcounter() >= status.hdmaSetupPosition) {
    status.hdmaSetupTriggered = true;
    hdmaReset();
    if(hdmaEnable()) {
      status.hdmaPending = true;
      status.hdmaMode = 0;
    }
  }

  if(!status.hdmaTriggered && hcounter() >= status.hdmaPosition) {
    status.hdmaTriggered = true;
    if(hdmaActive()) {
      status.hdmaPending = true;
      status.hdmaMode = 1;
    }
  }

  if constexpr(Synchronize) {
    if(configuration.hacks.coprocessor.delayedSync) return;
    synchronizeCoprocessors();
  }
}

auto CPU::step(uint clocks) -> void {
  switch(clocks) {
  case  2: return step< 2,1>();
  case  4: return step< 4,1>();
  case  6: return step< 6,1>();
  case  8: return step< 8,1>();
  case 10: return step<10,1>();
  case 12: return step<12,1>();
  }
}

//called by ppu.tick() when Hcounter=0
auto CPU::scanline() -> void {
  //forcefully sync S-CPU to other processors, in case chips are not communicating
  synchronizeSMP();
  synchronizePPU();
  synchronizeCoprocessors();

  if(vcounter() == 0) {
    //HDMA setup triggers once every frame
    status.hdmaSetupPosition = (version == 1 ? 12 + 8 - dmaCounter() : 12 + dmaCounter());
    status.hdmaSetupTriggered = false;

    status.autoJoypadCounter = 33;  //33 = inactive
  }

  //DRAM refresh occurs once every scanline
  if(version == 2) status.dramRefreshPosition = 530 + 8 - dmaCounter();
  status.dramRefresh = 0;

  //HDMA triggers once every visible scanline
  if(vcounter() < ppu.vdisp()) {
    status.hdmaPosition = 1104;
    status.hdmaTriggered = false;
  }

  //overclocking
  if(vcounter() == (Region::NTSC() ? 261 : 311)) {
    overclocking.counter = 0;
    overclocking.target = 0;
    double overclock = configuration.hacks.cpu.overclock / 100.0;
    if(overclock > 1.0) {
      int clocks = (Region::NTSC() ? 262 : 312) * 1364;
      overclocking.target = clocks * overclock - clocks;
    }
  }

  //handle video frame events from the CPU core to prevent a race condition between
  //games polling inputs during NMI and the PPU thread not being 100% synchronized.
  if(vcounter() == ppu.vdisp()) {
    if(auto device = controllerPort2.device) device->latch();  //light guns
    synchronizePPU();
    if(system.fastPPU()) PPUfast::Line::flush();
    scheduler.leave(Scheduler::Event::Frame);
  }
}

auto CPU::aluEdge() -> void {
  if(alu.mpyctr) {
    alu.mpyctr--;
    if(io.rddiv & 1) io.rdmpy += alu.shift;
    io.rddiv >>= 1;
    alu.shift <<= 1;
  }

  if(alu.divctr) {
    alu.divctr--;
    io.rddiv <<= 1;
    alu.shift >>= 1;
    if(io.rdmpy >= alu.shift) {
      io.rdmpy -= alu.shift;
      io.rddiv |= 1;
    }
  }
}

auto CPU::dmaEdge() -> void {
  //H/DMA pending && DMA inactive?
  //.. Run one full CPU cycle
  //.. HDMA pending && HDMA enabled ? DMA sync + HDMA run
  //.. DMA pending && DMA enabled ? DMA sync + DMA run
  //.... HDMA during DMA && HDMA enabled ? DMA sync + HDMA run
  //.. Run one bus CPU cycle
  //.. CPU sync

  if(status.dmaActive) {
    if(status.hdmaPending) {
      status.hdmaPending = false;
      if(hdmaEnable()) {
        if(!dmaEnable()) {
          step(counter.dma = 8 - dmaCounter());
        }
        status.hdmaMode == 0 ? hdmaSetup() : hdmaRun();
        if(!dmaEnable()) {
          step(status.clockCount - counter.dma % status.clockCount);
          status.dmaActive = false;
        }
      }
    }

    if(status.dmaPending) {
      status.dmaPending = false;
      if(dmaEnable()) {
        step(counter.dma = 8 - dmaCounter());
        dmaRun();
        step(status.clockCount - counter.dma % status.clockCount);
        status.dmaActive = false;
      }
    }
  }

  if(!status.dmaActive) {
    if(status.dmaPending || status.hdmaPending) {
      status.dmaActive = true;
    }
  }
}

//called every 128 clocks from inside the CPU::stepOnce() function
auto CPU::joypadEdge() -> void {
  //it is not yet confirmed if polling can be stopped early and/or (re)started later
  if(!io.autoJoypadPoll) return;

  if(vcounter() == ppu.vdisp() && hcounter() >= 130 && hcounter() <= 256) {
    //begin new polling sequence
    status.autoJoypadCounter = 0;
  }

  //stop after polling has been completed for this frame
  if(status.autoJoypadCounter >= 33) return;

  if(status.autoJoypadCounter == 0) {
    //latch controller states on the first polling cycle
    controllerPort1.device->latch(1);
    controllerPort2.device->latch(1);
  }

  if(status.autoJoypadCounter == 1) {
    //release latch and begin reading on the second cycle
    controllerPort1.device->latch(0);
    controllerPort2.device->latch(0);

    //shift registers are cleared to zero at start of auto-joypad polling
    io.joy1 = 0;
    io.joy2 = 0;
    io.joy3 = 0;
    io.joy4 = 0;
  }

  if(status.autoJoypadCounter >= 2 && !(status.autoJoypadCounter & 1)) {
    //sixteen bits are shifted into joy{1-4}, one bit per 256 clocks
    uint2 port0 = controllerPort1.device->data();
    uint2 port1 = controllerPort2.device->data();

    io.joy1 = io.joy1 << 1 | port0.bit(0);
    io.joy2 = io.joy2 << 1 | port1.bit(0);
    io.joy3 = io.joy3 << 1 | port0.bit(1);
    io.joy4 = io.joy4 << 1 | port1.bit(1);
  }

  status.autoJoypadCounter++;
}
