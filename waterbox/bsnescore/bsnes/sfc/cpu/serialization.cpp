auto CPU::serialize(serializer& s) -> void {
  WDC65816::serialize(s);
  Thread::serialize(s);
  PPUcounter::serialize(s);

  s.array(wram);

  s.integer(version);

  s.integer(counter.cpu);
  s.integer(counter.dma);

  s.integer(status.clockCount);

  s.integer(status.irqLock);

  s.integer(status.dramRefreshPosition);
  s.integer(status.dramRefresh);

  s.integer(status.hdmaSetupPosition);
  s.integer(status.hdmaSetupTriggered);

  s.integer(status.hdmaPosition);
  s.integer(status.hdmaTriggered);

  s.boolean(status.nmiValid);
  s.boolean(status.nmiLine);
  s.boolean(status.nmiTransition);
  s.boolean(status.nmiPending);
  s.boolean(status.nmiHold);

  s.boolean(status.irqValid);
  s.boolean(status.irqLine);
  s.boolean(status.irqTransition);
  s.boolean(status.irqPending);
  s.boolean(status.irqHold);

  s.integer(status.resetPending);
  s.integer(status.interruptPending);

  s.integer(status.dmaActive);
  s.integer(status.dmaPending);
  s.integer(status.hdmaPending);
  s.integer(status.hdmaMode);

  s.integer(status.autoJoypadCounter);

  s.integer(status.autoJoypadPort1);
  s.integer(status.autoJoypadPort2);

  s.boolean(status.cpuLatch);
  s.boolean(status.autoJoypadLatch);

  s.integer(io.wramAddress);

  s.boolean(io.hirqEnable);
  s.boolean(io.virqEnable);
  s.boolean(io.irqEnable);
  s.boolean(io.nmiEnable);
  s.boolean(io.autoJoypadPoll);

  s.integer(io.pio);

  s.integer(io.wrmpya);
  s.integer(io.wrmpyb);

  s.integer(io.wrdiva);
  s.integer(io.wrdivb);

  s.integer(io.htime);
  s.integer(io.vtime);

  s.integer(io.fastROM);

  s.integer(io.rddiv);
  s.integer(io.rdmpy);

  s.integer(io.joy1);
  s.integer(io.joy2);
  s.integer(io.joy3);
  s.integer(io.joy4);

  s.integer(alu.mpyctr);
  s.integer(alu.divctr);
  s.integer(alu.shift);

  for(auto& channel : channels) {
    s.integer(channel.dmaEnable);
    s.integer(channel.hdmaEnable);
    s.integer(channel.direction);
    s.integer(channel.indirect);
    s.integer(channel.unused);
    s.integer(channel.reverseTransfer);
    s.integer(channel.fixedTransfer);
    s.integer(channel.transferMode);
    s.integer(channel.targetAddress);
    s.integer(channel.sourceAddress);
    s.integer(channel.sourceBank);
    s.integer(channel.transferSize);
    s.integer(channel.indirectBank);
    s.integer(channel.hdmaAddress);
    s.integer(channel.lineCounter);
    s.integer(channel.unknown);
    s.integer(channel.hdmaCompleted);
    s.integer(channel.hdmaDoTransfer);
  }
}
