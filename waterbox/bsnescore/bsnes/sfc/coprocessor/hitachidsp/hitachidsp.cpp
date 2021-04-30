#include <sfc/sfc.hpp>
#include <processor/hg51b/hg51b.cpp>

namespace SuperFamicom {

#include "memory.cpp"
#include "serialization.cpp"
#include "data-rom.cpp"
HitachiDSP hitachidsp;

auto HitachiDSP::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto HitachiDSP::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    hitachidsp.main();
  }
}

auto HitachiDSP::step(uint clocks) -> void {
  HG51B::step(clocks);
  clock += clocks * (uint64_t)cpu.frequency;
  synchronizeCPU();
}

auto HitachiDSP::halt() -> void {
  HG51B::halt();
  if(io.irq == 0) cpu.irq(r.i = 1);
}

auto HitachiDSP::unload() -> void {
  rom.reset();
  ram.reset();
}

auto HitachiDSP::power() -> void {
  HG51B::power();
  create(HitachiDSP::Enter, Frequency);
}

}
