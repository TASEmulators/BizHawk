#include <processor/processor.hpp>
#include "hg51b.hpp"

namespace Processor {

#include "registers.cpp"
#include "instruction.cpp"
#include "instructions.cpp"
#include "serialization.cpp"

auto HG51B::lock() -> void {
  io.lock = 1;
}

auto HG51B::halt() -> void {
  io.halt = 1;
}

auto HG51B::wait(uint24 address) -> uint {
  if(isROM(address)) return 1 + io.wait.rom;
  if(isRAM(address)) return 1 + io.wait.ram;
  return 1;
}

auto HG51B::main() -> void {
  if(io.lock) return step(1);
  if(io.suspend.enable) return suspend();
  if(io.cache.enable) return cache(), void();
  if(io.dma.enable) return dma();
  if(io.halt) return step(1);
  return execute();
}

auto HG51B::step(uint clocks) -> void {
  if(io.bus.enable) {
    if(io.bus.pending > clocks) {
      io.bus.pending -= clocks;
    } else {
      io.bus.enable = 0;
      io.bus.pending = 0;
      if(io.bus.reading) io.bus.reading = 0, r.mdr = read(io.bus.address);
      if(io.bus.writing) io.bus.writing = 0, write(io.bus.address, r.mdr);
    }
  }
}

auto HG51B::execute() -> void {
  if(!cache()) return halt();
  auto opcode = programRAM[io.cache.page][r.pc];
  advance();
  step(1);
  instructionTable[opcode]();
}

auto HG51B::advance() -> void {
  if(++r.pc == 0) {
    if(io.cache.page == 1) return halt();
    io.cache.page = 1;
    if(io.cache.lock[io.cache.page]) return halt();
    r.pb = r.p;
    if(!cache()) return halt();
  }
}

auto HG51B::suspend() -> void {
  if(!io.suspend.duration) return step(1);  //indefinite
  step(io.suspend.duration);
  io.suspend.duration = 0;
  io.suspend.enable = 0;
}

auto HG51B::cache() -> bool {
  uint24 address = io.cache.base + r.pb * 512;

  //try to use the current page ...
  if(io.cache.address[io.cache.page] == address) return io.cache.enable = 0, true;
  //if it's not valid, try to use the other page ...
  io.cache.page ^= 1;
  if(io.cache.address[io.cache.page] == address) return io.cache.enable = 0, true;
  //if it's not valid, try to load into the other page ...
  if(io.cache.lock[io.cache.page]) io.cache.page ^= 1;
  //if it's locked, try to load into the first page ...
  if(io.cache.lock[io.cache.page]) return io.cache.enable = 0, false;

  io.cache.address[io.cache.page] = address;
  for(uint offset : range(256)) {
    step(wait(address));
    programRAM[io.cache.page][offset]  = read(address++) << 0;
    programRAM[io.cache.page][offset] |= read(address++) << 8;
  }
  return io.cache.enable = 0, true;
}

auto HG51B::dma() -> void {
  for(uint offset : range(io.dma.length)) {
    uint24 source = io.dma.source + offset;
    uint24 target = io.dma.target + offset;

    if(isROM(source) && isROM(target)) return lock();
    if(isRAM(source) && isRAM(target)) return lock();

    step(wait(source));
    auto data = read(source);

    step(wait(target));
    write(target, data);
  }

  io.dma.enable = 0;
}

auto HG51B::running() const -> bool {
  return io.cache.enable || io.dma.enable || io.bus.pending || !io.halt;
}

auto HG51B::busy() const -> bool {
  return io.cache.enable || io.dma.enable || io.bus.pending;
}

auto HG51B::power() -> void {
  r = {};
  io = {};
}

}
