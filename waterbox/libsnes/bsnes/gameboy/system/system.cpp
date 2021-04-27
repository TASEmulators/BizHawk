#include <gameboy/gameboy.hpp>

#define SYSTEM_CPP
namespace GameBoy {

#include "bootrom-dmg.cpp"
#include "bootrom-sgb.cpp"
#include "bootrom-cgb.cpp"
#include "serialization.cpp"
System system;

void System::run() {
  scheduler.sync = Scheduler::SynchronizeMode::None;

  scheduler.enter();
  if(scheduler.exit_reason() == Scheduler::ExitReason::FrameEvent) {
  }
}

void System::runtosave() {
  scheduler.sync = Scheduler::SynchronizeMode::CPU;
  runthreadtosave();

  scheduler.active_thread = lcd.thread;
  runthreadtosave();
}

void System::runthreadtosave() {
  while(true) {
    scheduler.enter();
    if(scheduler.exit_reason() == Scheduler::ExitReason::SynchronizeEvent) break;
    if(scheduler.exit_reason() == Scheduler::ExitReason::FrameEvent) {
    }
  }
}

void System::init() {
  assert(interface != 0);
}

void System::load(Revision revision) {
  this->revision = revision;
  serialize_init();
}

void System::power() {
  bus.power();
  cartridge.power();
  cpu.power();
  apu.power();
  lcd.power();
  scheduler.init();

  clocks_executed = 0;
}

}
