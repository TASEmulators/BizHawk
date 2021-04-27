#include <sfc/sfc.hpp>

namespace SuperFamicom {

ICD icd;
#include "interface.cpp"
#include "io.cpp"
#include "boot-roms.cpp"
#include "serialization.cpp"

namespace SameBoy {
  static auto hreset(GB_gameboy_t*) -> void {
    icd.ppuHreset();
  }

  static auto vreset(GB_gameboy_t*) -> void {
    icd.ppuVreset();
  }

  static auto icd_pixel(GB_gameboy_t*, uint8_t pixel) -> void {
    icd.ppuWrite(pixel);
  }

  static auto joyp_write(GB_gameboy_t*, uint8_t value) -> void {
    bool p14 = value & 0x10;
    bool p15 = value & 0x20;
    icd.joypWrite(p14, p15);
  }

  static auto read_memory(GB_gameboy_t*, uint16_t addr, uint8_t data) -> uint8_t {
    if(auto replace = icd.cheats.find(addr, data)) return replace();
    return data;
  }

  static auto rgb_encode(GB_gameboy_t*, uint8_t r, uint8_t g, uint8_t b) -> uint32_t {
    return r << 16 | g << 8 | b << 0;
  }

  static auto sample(GB_gameboy_t*, GB_sample_t* sample) -> void {
    float left  = sample->left  / 32768.0f;
    float right = sample->right / 32768.0f;
    icd.apuWrite(left, right);
  }

  static auto vblank(GB_gameboy_t*) -> void {
  }
}

auto ICD::synchronizeCPU() -> void {
  if(clock >= 0) scheduler.resume(cpu.thread);
}

auto ICD::Enter() -> void {
  while(true) {
    scheduler.synchronize();
    icd.main();
  }
}

auto ICD::main() -> void {
  if(r6003 & 0x80) {
    auto clocks = GB_run(&sameboy);
    step(clocks >> 1);
  } else {  //DMG halted
    apuWrite(0.0, 0.0);
    step(128);
  }
  synchronizeCPU();
}

auto ICD::step(uint clocks) -> void {
  clock += clocks * (uint64_t)cpu.frequency;
}

//SGB1 uses the CPU oscillator (~2.4% faster than a real Game Boy)
//SGB2 uses a dedicated oscillator (same speed as a real Game Boy)
auto ICD::clockFrequency() const -> uint {
  return Frequency ? Frequency : system.cpuFrequency();
}

auto ICD::load() -> bool {
  information = {};

  GB_random_set_enabled(configuration.hacks.entropy != "None");
  if(Frequency == 0) {
    GB_init(&sameboy, GB_MODEL_SGB_NO_SFC);
    GB_load_boot_rom_from_buffer(&sameboy, (const unsigned char*)&SGB1BootROM[0], 256);
  } else {
    GB_init(&sameboy, GB_MODEL_SGB2_NO_SFC);
    GB_load_boot_rom_from_buffer(&sameboy, (const unsigned char*)&SGB2BootROM[0], 256);
  }
  GB_set_sample_rate_by_clocks(&sameboy, 256);
  GB_set_highpass_filter_mode(&sameboy, GB_HIGHPASS_ACCURATE);
  GB_set_icd_hreset_callback(&sameboy, &SameBoy::hreset);
  GB_set_icd_vreset_callback(&sameboy, &SameBoy::vreset);
  GB_set_icd_pixel_callback(&sameboy, &SameBoy::icd_pixel);
  GB_set_joyp_write_callback(&sameboy, &SameBoy::joyp_write);
  GB_set_read_memory_callback(&sameboy, &SameBoy::read_memory);
  GB_set_rgb_encode_callback(&sameboy, &SameBoy::rgb_encode);
  GB_apu_set_sample_callback(&sameboy, &SameBoy::sample);
  GB_set_vblank_callback(&sameboy, &SameBoy::vblank);
  GB_set_pixels_output(&sameboy, &bitmap[0]);
  if(auto loaded = platform->load(ID::GameBoy, "Game Boy", "gb")) {
    information.pathID = loaded.pathID;
  } else return unload(), false;
  if(auto fp = platform->open(pathID(), "manifest.bml", File::Read, File::Required)) {
    auto manifest = fp->reads();
    cartridge.slotGameBoy.load(manifest);
  } else return unload(), false;
  if(auto fp = platform->open(pathID(), "program.rom", File::Read, File::Required)) {
    auto size = fp->size();
    auto data = (uint8_t*)malloc(size);
    cartridge.information.sha256 = Hash::SHA256({data, (uint64_t)size}).digest();
    fp->read(data, size);
    GB_load_rom_from_buffer(&sameboy, data, size);
    free(data);
  } else return unload(), false;
  if(auto fp = platform->open(pathID(), "save.ram", File::Read)) {
    auto size = fp->size();
    auto data = (uint8_t*)malloc(size);
    fp->read(data, size);
    GB_load_battery_from_buffer(&sameboy, data, size);
    free(data);
  }
  return true;
}

auto ICD::save() -> void {
  if(auto size = GB_save_battery_size(&sameboy)) {
    auto data = (uint8_t*)malloc(size);
    GB_save_battery_to_buffer(&sameboy, data, size);
    if(auto fp = platform->open(pathID(), "save.ram", File::Write)) {
      fp->write(data, size);
    }
    free(data);
  }
}

auto ICD::unload() -> void {
  save();
  GB_free(&sameboy);
}

auto ICD::power(bool reset) -> void {
  auto frequency = clockFrequency() / 5;
  create(ICD::Enter, frequency);
  if(!reset) stream = Emulator::audio.createStream(2, frequency / 128);

  for(auto& packet : this->packet) packet = {};
  packetSize = 0;

  joypID = 0;
  joypLock = 1;
  pulseLock = 1;
  strobeLock = 0;
  packetLock = 0;
  joypPacket = {};
  packetOffset = 0;
  bitData = 0;
  bitOffset = 0;

  for(auto& n : output) n = 0xff;
  readBank = 0;
  readAddress = 0;
  writeBank = 0;

  r6003 = 0x00;
  r6004 = 0xff;
  r6005 = 0xff;
  r6006 = 0xff;
  r6007 = 0xff;
  for(auto& r : r7000) r = 0x00;
  mltReq = 0;

  hcounter = 0;
  vcounter = 0;

  GB_reset(&sameboy);
}

}
