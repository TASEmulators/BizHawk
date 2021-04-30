#include <sfc/sfc.hpp>
#include <processor/gsu/gsu.cpp>

namespace SuperFamicom {

#include "bus.cpp"
#include "core.cpp"
#include "memory.cpp"
#include "io.cpp"
#include "timing.cpp"
#include "serialization.cpp"
SuperFX superfx;

auto SuperFX::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto SuperFX::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    superfx.main();
  }
}

auto SuperFX::main() -> void {
  if(regs.sfr.g == 0) return step(6);

  instruction(peekpipe());

  if(regs.r[14].modified) {
    regs.r[14].modified = false;
    updateROMBuffer();
  }

  if(regs.r[15].modified) {
    regs.r[15].modified = false;
  } else {
    regs.r[15]++;
  }
}

auto SuperFX::unload() -> void {
  rom.reset();
  ram.reset();
}

auto SuperFX::power() -> void {
  double overclock = max(1.0, min(8.0, configuration.hacks.superfx.overclock / 100.0));

  GSU::power();
  create(SuperFX::Enter, Frequency * overclock);

  romMask = rom.size() - 1;
  ramMask = ram.size() - 1;

  for(uint n : range(512)) cache.buffer[n] = 0x00;
  for(uint n : range(32)) cache.valid[n] = false;
  for(uint n : range(2)) {
    pixelcache[n].offset = ~0;
    pixelcache[n].bitpend = 0x00;
  }

  regs.romcl = 0;
  regs.romdr = 0;

  regs.ramcl = 0;
  regs.ramar = 0;
  regs.ramdr = 0;
}

}
