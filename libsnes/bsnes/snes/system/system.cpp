#include <snes/snes.hpp>

#define SYSTEM_CPP
namespace SNES {

System system;

#include <snes/config/config.cpp>
#include <snes/scheduler/scheduler.cpp>
#include <snes/random/random.cpp>

#include "video.cpp"
#include "audio.cpp"
#include "input.cpp"
#include "serialization.cpp"

void System::run() {
  scheduler.sync = Scheduler::SynchronizeMode::None;

  scheduler.enter();
  if(scheduler.exit_reason() == Scheduler::ExitReason::FrameEvent) {
    video.update();
  }
}

void System::runtosave() {
  if(CPU::Threaded == true) {
    scheduler.sync = Scheduler::SynchronizeMode::CPU;
    runthreadtosave();
  }

  if(SMP::Threaded == true) {
    scheduler.thread = smp.thread;
    runthreadtosave();
  }

  if(PPU::Threaded == true) {
    scheduler.thread = ppu.thread;
    runthreadtosave();
  }

  if(DSP::Threaded == true) {
    scheduler.thread = dsp.thread;
    runthreadtosave();
  }

  for(unsigned i = 0; i < cpu.coprocessors.size(); i++) {
    Processor &chip = *cpu.coprocessors[i];
    scheduler.thread = chip.thread;
    runthreadtosave();
  }
}

void System::runthreadtosave() {
  while(true) {
    scheduler.enter();
    if(scheduler.exit_reason() == Scheduler::ExitReason::SynchronizeEvent) break;
    if(scheduler.exit_reason() == Scheduler::ExitReason::FrameEvent) {
      video.update();
    }
  }
}

void System::init() {
  assert(interface != 0);

  #if defined(GAMEBOY)
  icd2.init();
  #endif
  nss.init();
  superfx.init();
  sa1.init();
  necdsp.init();
  hitachidsp.init();
  armdsp.init();
  bsxsatellaview.init();
  bsxcartridge.init();
  bsxflash.init();
  srtc.init();
  sdd1.init();
  spc7110.init();
  obc1.init();
  msu1.init();
  link.init();

  video.init();
  audio.init();

  input.connect(0, config.controller_port1);
  input.connect(1, config.controller_port2);
}

void System::term() {
}

void System::load() {
  audio.coprocessor_enable(false);

  bus.map_reset();
  bus.map_xml();

  cpu.enable();
  ppu.enable();

  if(expansion() == ExpansionPortDevice::BSX) bsxsatellaview.load();
  if(cartridge.mode() == Cartridge::Mode::Bsx) bsxcartridge.load();
  if(cartridge.mode() == Cartridge::Mode::SufamiTurbo) sufamiturbo.load();
  #if defined(GAMEBOY)
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) icd2.load();
  #endif

  if(cartridge.has_bsx_slot()) bsxflash.load();
  if(cartridge.has_nss_dip()) nss.load();
  if(cartridge.has_superfx()) superfx.load();
  if(cartridge.has_sa1()) sa1.load();
  if(cartridge.has_necdsp()) necdsp.load();
  if(cartridge.has_hitachidsp()) hitachidsp.load();
  if(cartridge.has_armdsp()) armdsp.load();
  if(cartridge.has_srtc()) srtc.load();
  if(cartridge.has_sdd1()) sdd1.load();
  if(cartridge.has_spc7110()) spc7110.load();
  if(cartridge.has_obc1()) obc1.load();
  if(cartridge.has_msu1()) msu1.load();
  if(cartridge.has_link()) link.load();

  serialize_init();
  cheat.init();
}

void System::unload() {
  if(expansion() == ExpansionPortDevice::BSX) bsxsatellaview.unload();
  if(cartridge.mode() == Cartridge::Mode::Bsx) bsxcartridge.unload();
  if(cartridge.mode() == Cartridge::Mode::SufamiTurbo) sufamiturbo.unload();
  #if defined(GAMEBOY)
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) icd2.unload();
  #endif

  if(cartridge.has_bsx_slot()) bsxflash.unload();
  if(cartridge.has_nss_dip()) nss.unload();
  if(cartridge.has_superfx()) superfx.unload();
  if(cartridge.has_sa1()) sa1.unload();
  if(cartridge.has_necdsp()) necdsp.unload();
  if(cartridge.has_hitachidsp()) hitachidsp.unload();
  if(cartridge.has_armdsp()) armdsp.unload();
  if(cartridge.has_srtc()) srtc.unload();
  if(cartridge.has_sdd1()) sdd1.unload();
  if(cartridge.has_spc7110()) spc7110.unload();
  if(cartridge.has_obc1()) obc1.unload();
  if(cartridge.has_msu1()) msu1.unload();
  if(cartridge.has_link()) link.unload();
}

void System::power() {
  random.seed((unsigned)time(0));

  region = config.region;
  expansion = config.expansion_port;
  if(region == Region::Autodetect) {
    region = (cartridge.region() == Cartridge::Region::NTSC ? Region::NTSC : Region::PAL);
  }

  cpu_frequency = region() == Region::NTSC ? config.cpu.ntsc_frequency : config.cpu.pal_frequency;
  apu_frequency = region() == Region::NTSC ? config.smp.ntsc_frequency : config.smp.pal_frequency;

  cpu.power();
  smp.power();
  dsp.power();
  ppu.power();

  if(expansion() == ExpansionPortDevice::BSX) bsxsatellaview.power();
  if(cartridge.mode() == Cartridge::Mode::Bsx) bsxcartridge.power();
  #if defined(GAMEBOY)
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) icd2.power();
  #endif

  if(cartridge.has_bsx_slot()) bsxflash.power();
  if(cartridge.has_nss_dip()) nss.power();
  if(cartridge.has_superfx()) superfx.power();
  if(cartridge.has_sa1()) sa1.power();
  if(cartridge.has_necdsp()) necdsp.power();
  if(cartridge.has_hitachidsp()) hitachidsp.power();
  if(cartridge.has_armdsp()) armdsp.power();
  if(cartridge.has_srtc()) srtc.power();
  if(cartridge.has_sdd1()) sdd1.power();
  if(cartridge.has_spc7110()) spc7110.power();
  if(cartridge.has_obc1()) obc1.power();
  if(cartridge.has_msu1()) msu1.power();
  if(cartridge.has_link()) link.power();

  reset();
}

void System::reset() {
  cpu.reset();
  smp.reset();
  dsp.reset();
  ppu.reset();

  if(expansion() == ExpansionPortDevice::BSX) bsxsatellaview.reset();

  if(cartridge.mode() == Cartridge::Mode::Bsx) bsxcartridge.reset();
  #if defined(GAMEBOY)
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) icd2.reset();
  #endif

  if(cartridge.has_bsx_slot()) bsxflash.reset();
  if(cartridge.has_nss_dip()) nss.reset();
  if(cartridge.has_superfx()) superfx.reset();
  if(cartridge.has_sa1()) sa1.reset();
  if(cartridge.has_necdsp()) necdsp.reset();
  if(cartridge.has_hitachidsp()) hitachidsp.reset();
  if(cartridge.has_armdsp()) armdsp.reset();
  if(cartridge.has_srtc()) srtc.reset();
  if(cartridge.has_sdd1()) sdd1.reset();
  if(cartridge.has_spc7110()) spc7110.reset();
  if(cartridge.has_obc1()) obc1.reset();
  if(cartridge.has_msu1()) msu1.reset();
  if(cartridge.has_link()) link.reset();

  #if defined(GAMEBOY)
  if(cartridge.mode() == Cartridge::Mode::SuperGameBoy) cpu.coprocessors.append(&icd2);
  #endif
  if(cartridge.has_superfx()) cpu.coprocessors.append(&superfx);
  if(cartridge.has_sa1()) cpu.coprocessors.append(&sa1);
  if(cartridge.has_necdsp()) cpu.coprocessors.append(&necdsp);
  if(cartridge.has_hitachidsp()) cpu.coprocessors.append(&hitachidsp);
  if(cartridge.has_armdsp()) cpu.coprocessors.append(&armdsp);
  if(cartridge.has_msu1()) cpu.coprocessors.append(&msu1);
  if(cartridge.has_link()) cpu.coprocessors.append(&link);

  scheduler.init();
  input.connect(0, config.controller_port1);
  input.connect(1, config.controller_port2);
}

void System::scanline() {
  video.scanline();
  if(cpu.vcounter() == 241) scheduler.exit(Scheduler::ExitReason::FrameEvent);
}

void System::frame() {
}

System::System() {
  region = Region::Autodetect;
  expansion = ExpansionPortDevice::BSX;
}

}


//zero 04-sep-2012
extern "C" void snes_set_layer_enable(int layer, int priority, bool enable)
{
	SNES::ppu.layer_enable(layer, priority, enable);
}
