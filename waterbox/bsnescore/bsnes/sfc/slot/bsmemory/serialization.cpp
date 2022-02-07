auto BSMemory::serialize(serializer& s) -> void {
  if(ROM) return;
  Thread::serialize(s);

  s.array(memory.data(), memory.size());

  s.integer(pin.writable);

  s.integer(chip.vendor);
  s.integer(chip.device);
  s.integer(chip.serial);

  s.array(page.buffer[0]);
  s.array(page.buffer[1]);

  for(auto& block : blocks) {
    s.integer(block.id);
    s.integer(block.erased);
    s.integer(block.locked);
    s.integer(block.erasing);
    s.integer(block.status.vppLow);
    s.integer(block.status.queueFull);
    s.integer(block.status.aborted);
    s.integer(block.status.failed);
    s.integer(block.status.locked);
    s.integer(block.status.ready);
  }

  s.integer(compatible.status.vppLow);
  s.integer(compatible.status.writeFailed);
  s.integer(compatible.status.eraseFailed);
  s.integer(compatible.status.eraseSuspended);
  s.integer(compatible.status.ready);

  s.integer(global.status.page);
  s.integer(global.status.pageReady);
  s.integer(global.status.pageAvailable);
  s.integer(global.status.queueFull);
  s.integer(global.status.sleeping);
  s.integer(global.status.failed);
  s.integer(global.status.suspended);
  s.integer(global.status.ready);

  s.integer(mode);

  s.integer(readyBusyMode);

  queue.serialize(s);
}

auto BSMemory::Queue::serialize(serializer& s) -> void {
  s.integer(history[0].valid);
  s.integer(history[0].address);
  s.integer(history[0].data);

  s.integer(history[1].valid);
  s.integer(history[1].address);
  s.integer(history[1].data);

  s.integer(history[2].valid);
  s.integer(history[2].address);
  s.integer(history[2].data);

  s.integer(history[3].valid);
  s.integer(history[3].address);
  s.integer(history[3].data);
}
