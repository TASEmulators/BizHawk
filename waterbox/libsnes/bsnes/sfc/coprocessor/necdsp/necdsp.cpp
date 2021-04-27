#include <sfc/sfc.hpp>
#include <processor/upd96050/upd96050.cpp>

namespace SuperFamicom {

#include "serialization.cpp"
NECDSP necdsp;

auto NECDSP::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto NECDSP::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    necdsp.main();
  }
}

auto NECDSP::main() -> void {
  exec();
  step(1);
  synchronizeCPU();
}

auto NECDSP::step(uint clocks) -> void {
  clock += clocks * (uint64_t)cpu.frequency;
}

auto NECDSP::read(uint addr, uint8) -> uint8 {
  cpu.synchronizeCoprocessors();
  if(addr & 1) {
    return uPD96050::readSR();
  } else {
    return uPD96050::readDR();
  }
}

auto NECDSP::write(uint addr, uint8 data) -> void {
  cpu.synchronizeCoprocessors();
  if(addr & 1) {
    return uPD96050::writeSR(data);
  } else {
    return uPD96050::writeDR(data);
  }
}

auto NECDSP::readRAM(uint addr, uint8) -> uint8 {
  cpu.synchronizeCoprocessors();
  return uPD96050::readDP(addr);
}

auto NECDSP::writeRAM(uint addr, uint8 data) -> void {
  cpu.synchronizeCoprocessors();
  return uPD96050::writeDP(addr, data);
}

auto NECDSP::power() -> void {
  uPD96050::power();
  create(NECDSP::Enter, Frequency);
}

}
