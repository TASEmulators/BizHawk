inline auto PI::readWord(u32 address, Thread& thread) -> u32 {
  if(address <= 0x046f'ffff) return ioRead(address);

  if (unlikely(io.ioBusy)) {
    debug(unusual, "[PI::readWord] PI read to 0x", hex(address, 8L), " will not behave as expected because PI writing is in progress");
    thread.step(writeForceFinish() * 2);
    return io.busLatch;
  }
  thread.step(250 * 2);
  io.busLatch = busRead<Word>(address);
  return io.busLatch;
}

template <u32 Size>
inline auto PI::busRead(u32 address) -> u32 {
  static_assert(Size == Half || Size == Word);  //PI bus will do 32-bit (CPU) or 16-bit (DMA) only
  const u32 unmapped = (address & 0xFFFF) | (address << 16);

  if(address <= 0x04ff'ffff) return unmapped; //Address range not memory mapped, only accessible via DMA
  if(address <= 0x0500'03ff) {
    if(_DD()) return dd.c2s.read<Size>(address);
    return unmapped;
  }
  if(address <= 0x0500'04ff) {
    if(_DD()) return dd.ds.read<Size>(address);
    return unmapped;
  }
  if(address <= 0x0500'057f) {
    if(_DD()) return dd.read<Size>(address);
    return unmapped;
  }
  if(address <= 0x0500'05bf) {
    if(_DD()) return dd.ms.read<Size>(address);
    return unmapped;
  }
  if(address <= 0x05ff'ffff) return unmapped;
  if(address <= 0x063f'ffff) {
    if(_DD()) return dd.iplrom.read<Size>(address);
    return unmapped;
  }
  if(address <= 0x07ff'ffff) return unmapped;
  if(address <= 0x0fff'ffff) {
    if(cartridge.ram  ) return cartridge.ram.read<Size>(address);
    if(cartridge.flash) return cartridge.flash.read<Size>(address);
    return unmapped;
  }
  if(cartridge.isviewer.enabled() && address >= 0x13f0'0000 && address <= 0x13ff'ffff) {
    return cartridge.isviewer.read<Size>(address);
  }
  if(address <= 0x1000'0000 + cartridge.rom.size - 1) {
    return cartridge.rom.read<Size>(address);
  }
  return unmapped;
}

inline auto PI::writeWord(u32 address, u32 data, Thread& thread) -> void {
  if(address <= 0x046f'ffff) return ioWrite(address, data);

  if(io.ioBusy) return;
  io.ioBusy = 1;
  io.busLatch = data;
  queue.insert(Queue::PI_BUS_Write, 400);
  return busWrite<Word>(address, data);
}

template <u32 Size>
inline auto PI::busWrite(u32 address, u32 data) -> void {
  static_assert(Size == Half || Size == Word);  //PI bus will do 32-bit (CPU) or 16-bit (DMA) only
  if(address <= 0x04ff'ffff) return; //Address range not memory mapped, only accessible via DMA
  if(address <= 0x0500'03ff) {
    if(_DD()) return dd.c2s.write<Size>(address, data);
    return;
  }
  if(address <= 0x0500'04ff) {
    if(_DD()) return dd.ds.write<Size>(address, data);
    return;
  }
  if(address <= 0x0500'057f) {
    if(_DD()) return dd.write<Size>(address, data);
    return;
  }
  if(address <= 0x0500'05bf) {
    if(_DD()) return dd.ms.write<Size>(address, data);
    return;
  }
  if(address <= 0x05ff'ffff) return;
  if(address <= 0x063f'ffff) {
    if(_DD()) return dd.iplrom.write<Size>(address, data);
    return;
  }
  if(address <= 0x07ff'ffff) return;
  if(address <= 0x0fff'ffff) {
    if(cartridge.ram  ) return cartridge.ram.write<Size>(address, data);
    if(cartridge.flash) return cartridge.flash.write<Size>(address, data);
    return;
  }
  if(address >= 0x13f0'0000 && address <= 0x13ff'ffff) {
    if(cartridge.isviewer.enabled()) {
      writeForceFinish(); //Debugging channel for homebrew, be gentle
      return cartridge.isviewer.write<Size>(address, data);      
    } else {
      debug(unhandled, "[PI::busWrite] attempt to write to ISViewer: ROM is too big so ISViewer is disabled");
    }
  }
  if(address <= 0x1000'0000 + cartridge.rom.size - 1) {
    return cartridge.rom.write<Size>(address, data);
  }
  if(address <= 0x7fff'ffff) return;
}

inline auto PI::writeFinished() -> void {
  io.ioBusy = 0;
}

inline auto PI::writeForceFinish() -> u32 {
  io.ioBusy = 0;
  return queue.remove(Queue::PI_BUS_Write);
}
