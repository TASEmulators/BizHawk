#include "libsnes.hpp"
#include <snes/snes.hpp>

#include <nall/snes/cartridge.hpp>
#include <nall/gameboy/cartridge.hpp>

#include <queue>

using namespace nall;

struct Interface : public SNES::Interface {
  snes_video_refresh_t pvideo_refresh;
  snes_audio_sample_t paudio_sample;
  snes_input_poll_t pinput_poll;
  snes_input_state_t pinput_state;
  snes_input_notify_t pinput_notify;
	snes_path_request_t ppath_request;
  string basename;
  uint32_t *buffer;
  uint32_t *palette;

	//zero 11-sep-2012
	time_t randomSeed() { return 0; }

	//zero 26-sep-2012
	std::queue<nall::string> messages;

  void videoRefresh(const uint32_t *data, bool hires, bool interlace, bool overscan) {
    unsigned width = hires ? 512 : 256;
    unsigned height = overscan ? 239 : 224;
    unsigned pitch = 1024 >> interlace;
    if(interlace) height <<= 1;
    data += 9 * 1024;  //skip front porch

    for(unsigned y = 0; y < height; y++) {
      const uint32_t *sp = data + y * pitch;
      uint32_t *dp = buffer + y * pitch;
      for(unsigned x = 0; x < width; x++) {
        *dp++ = palette[*sp++];
      }
    }

    if(pvideo_refresh) pvideo_refresh(buffer, width, height);
    if(pinput_poll) pinput_poll();
  }

  void audioSample(int16_t left, int16_t right) {
    if(paudio_sample) return paudio_sample(left, right);
  }

	//zero 27-sep-2012
	snes_scanlineStart_t pScanlineStart;
	void scanlineStart(int line)
	{
		if(pScanlineStart) pScanlineStart((int)line);
	}

  int16_t inputPoll(bool port, SNES::Input::Device device, unsigned index, unsigned id) {
    if(pinput_state) return pinput_state(port?1:0, (unsigned)device, index, id);
    return 0;
  }

  void inputNotify(int index) {
    if (pinput_notify) pinput_notify(index);
  }
  
  void message(const string &text) {
		messages.push(text);
  }

  string path(SNES::Cartridge::Slot slot, const string &hint)
	{
		if(ppath_request)
		{
			const char* path = ppath_request((int)slot, (const char*)hint);
			return path;
		}
    return { basename, hint };

  }

  Interface() : pvideo_refresh(0), paudio_sample(0), pinput_poll(0), pinput_state(0), pinput_notify(0), ppath_request(0) {
    buffer = new uint32_t[512 * 480];
    palette = new uint32_t[16 * 32768];

    //{llll bbbbb ggggg rrrrr} -> { rrrrr ggggg bbbbb }
    for(unsigned l = 0; l < 16; l++) {
      for(unsigned r = 0; r < 32; r++) {
        for(unsigned g = 0; g < 32; g++) {
          for(unsigned b = 0; b < 32; b++) {
            //double luma = (double)l / 15.0;
            //unsigned ar = (luma * r + 0.5);
            //unsigned ag = (luma * g + 0.5);
            //unsigned ab = (luma * b + 0.5);
            //palette[(l << 15) + (r << 10) + (g << 5) + (b << 0)] = (ab << 10) + (ag << 5) + (ar << 0);

						//zero 04-sep-2012 - go ahead and turn this into a pixel format we'll want
            double luma = (double)l / 15.0;
            unsigned ar = (luma * r + 0.5);
            unsigned ag = (luma * g + 0.5);
            unsigned ab = (luma * b + 0.5);
						ar = ar * 255 / 31;
						ag = ag * 255 / 31;
						ab = ab * 255 / 31;
						unsigned color = (ab << 16) + (ag << 8) + (ar << 0) | 0xFF000000;
						palette[(l << 15) + (r << 10) + (g << 5) + (b << 0)] = color;
          }
        }
      }
    }
  }

  ~Interface() {
    delete[] buffer;
    delete[] palette;
  }
};

static Interface interface;

const char* snes_library_id(void) {
  static string version = {"bsnes v", Version};
  return version;
}

unsigned snes_library_revision_major(void) {
  return 1;
}

unsigned snes_library_revision_minor(void) {
  return 3;
}

void snes_set_video_refresh(snes_video_refresh_t video_refresh) {
  interface.pvideo_refresh = video_refresh;
}

void snes_set_audio_sample(snes_audio_sample_t audio_sample) {
  interface.paudio_sample = audio_sample;
}

void snes_set_input_poll(snes_input_poll_t input_poll) {
  interface.pinput_poll = input_poll;
}

void snes_set_input_state(snes_input_state_t input_state) {
  interface.pinput_state = input_state;
}

void snes_set_input_notify(snes_input_notify_t input_notify) {
  interface.pinput_notify = input_notify;
}

void snes_set_path_request(snes_path_request_t path_request)
{
	 interface.ppath_request = path_request;
}

void snes_set_controller_port_device(bool port, unsigned device) {
  SNES::input.connect(port, (SNES::Input::Device)device);
}

void snes_set_cartridge_basename(const char *basename) {
  interface.basename = basename;
}

template<typename T> inline void reconstruct(T* t) { 
	t->~T();
	new(t) T();
}

void snes_init(void) {
  SNES::interface = &interface;

	//because we're tasers and we didnt make this core, and we're paranoid, lets reconstruct everything so we know subsequent runs are as similar as possible
  reconstruct(&SNES::icd2);
  reconstruct(&SNES::nss);
  reconstruct(&SNES::superfx);
  reconstruct(&SNES::sa1);
  reconstruct(&SNES::necdsp);
  reconstruct(&SNES::hitachidsp);
  reconstruct(&SNES::armdsp);
  reconstruct(&SNES::bsxsatellaview);
  reconstruct(&SNES::bsxcartridge);
  reconstruct(&SNES::bsxflash);
  reconstruct(&SNES::srtc);
  reconstruct(&SNES::sdd1);
  reconstruct(&SNES::spc7110);
  reconstruct(&SNES::obc1);
  reconstruct(&SNES::msu1);
  reconstruct(&SNES::link);
  reconstruct(&SNES::video);
  reconstruct(&SNES::audio);

  SNES::system.init();
  SNES::input.connect(SNES::Controller::Port1, SNES::Input::Device::Joypad);
  SNES::input.connect(SNES::Controller::Port2, SNES::Input::Device::Joypad);
}

void snes_term(void) {
  SNES::system.term();
}

void snes_power(void) {
  SNES::system.power();
}

void snes_reset(void) {
  SNES::system.reset();
}

void snes_run(void) {
  SNES::system.run();
}

unsigned snes_serialize_size(void) {
  return SNES::system.serialize_size();
}

bool snes_serialize(uint8_t *data, unsigned size) {
  SNES::system.runtosave();
  serializer s = SNES::system.serialize();
  if(s.size() > size) return false;
  memcpy(data, s.data(), s.size());
  return true;
}

bool snes_unserialize(const uint8_t *data, unsigned size) {
  serializer s(data, size);
  return SNES::system.unserialize(s);
}

struct CheatList {
  bool enable;
  string code;
  CheatList() : enable(false) {}
};

static linear_vector<CheatList> cheatList;

void snes_cheat_reset(void) {
  cheatList.reset();
  GameBoy::cheat.reset();
  GameBoy::cheat.synchronize();
  SNES::cheat.reset();
  SNES::cheat.synchronize();
}

void snes_cheat_set(unsigned index, bool enable, const char *code) {
  cheatList[index].enable = enable;
  cheatList[index].code = code;
  lstring list;
  for(unsigned n = 0; n < cheatList.size(); n++) {
    if(cheatList[n].enable) list.append(cheatList[n].code);
  }

  if(SNES::cartridge.mode() == SNES::Cartridge::Mode::SuperGameBoy) {
    GameBoy::cheat.reset();
    for(auto &code : list) {
      lstring codelist;
      codelist.split("+", code);
      for(auto &part : codelist) {
        unsigned addr, data, comp;
        if(GameBoy::Cheat::decode(part, addr, data, comp)) {
          GameBoy::cheat.append({ addr, data, comp });
        }
      }
    }
    GameBoy::cheat.synchronize();
    return;
  }

  SNES::cheat.reset();
  for(auto &code : list) {
    lstring codelist;
    codelist.split("+", code);
    for(auto &part : codelist) {
      unsigned addr, data;
      if(SNES::Cheat::decode(part, addr, data)) {
        SNES::cheat.append({ addr, data });
      }
    }
  }
  SNES::cheat.synchronize();
}

//zero 21-sep-2012
void snes_set_scanlineStart(snes_scanlineStart_t cb)
{
	interface.pScanlineStart = cb;
}

//zero 03-sep-2012
bool snes_check_cartridge(const uint8_t *rom_data, unsigned rom_size)
{
	//tries to determine whether this rom is a snes rom - BUT THIS TRIES TO ACCEPT EVERYTHING! so we cant really use it
	SnesCartridge temp(rom_data, rom_size);
	return temp.type != SnesCartridge::TypeUnknown && temp.type != SnesCartridge::TypeGameBoy;
}

//zero 05-sep-2012
int snes_peek_logical_register(int reg)
{
	switch(reg)
	{
		//$2105
	case SNES_REG_BG_MODE: return SNES::ppu.regs.bg_mode;
	case SNES_REG_BG3_PRIORITY: return SNES::ppu.regs.bg3_priority;
	case SNES_REG_BG1_TILESIZE: return SNES::ppu.regs.bg_tilesize[SNES::PPU::BG1];
	case SNES_REG_BG2_TILESIZE: return SNES::ppu.regs.bg_tilesize[SNES::PPU::BG2];
	case SNES_REG_BG3_TILESIZE: return SNES::ppu.regs.bg_tilesize[SNES::PPU::BG3];
	case SNES_REG_BG4_TILESIZE: return SNES::ppu.regs.bg_tilesize[SNES::PPU::BG4];

		//$2107
	case SNES_REG_BG1_SCADDR: return SNES::ppu.regs.bg_scaddr[SNES::PPU::BG1]>>9;
	case SNES_REG_BG1_SCSIZE: return SNES::ppu.regs.bg_scsize[SNES::PPU::BG1];
		//$2108
	case SNES_REG_BG2_SCADDR: return SNES::ppu.regs.bg_scaddr[SNES::PPU::BG2]>>9;
	case SNES_REG_BG2_SCSIZE: return SNES::ppu.regs.bg_scsize[SNES::PPU::BG2];
		//$2109
	case SNES_REG_BG3_SCADDR: return SNES::ppu.regs.bg_scaddr[SNES::PPU::BG3]>>9;
	case SNES_REG_BG3_SCSIZE: return SNES::ppu.regs.bg_scsize[SNES::PPU::BG3];
		//$210A
	case SNES_REG_BG4_SCADDR: return SNES::ppu.regs.bg_scaddr[SNES::PPU::BG4]>>9;
	case SNES_REG_BG4_SCSIZE: return SNES::ppu.regs.bg_scsize[SNES::PPU::BG4];
		//$210B
	case SNES_REG_BG1_TDADDR: return SNES::ppu.regs.bg_tdaddr[SNES::PPU::BG1]>>13;
	case SNES_REG_BG2_TDADDR: return SNES::ppu.regs.bg_tdaddr[SNES::PPU::BG2]>>13;
		//$210C
	case SNES_REG_BG3_TDADDR: return SNES::ppu.regs.bg_tdaddr[SNES::PPU::BG3]>>13;
	case SNES_REG_BG4_TDADDR: return SNES::ppu.regs.bg_tdaddr[SNES::PPU::BG4]>>13;
		//$2133 SETINI
	case SNES_REG_SETINI_MODE7_EXTBG: return SNES::ppu.regs.mode7_extbg?1:0;
	case SNES_REG_SETINI_HIRES: return SNES::ppu.regs.pseudo_hires?1:0;
	case SNES_REG_SETINI_OVERSCAN: return SNES::ppu.regs.overscan?1:0;
	case SNES_REG_SETINI_OBJ_INTERLACE: return SNES::ppu.regs.oam_interlace?1:0;
	case SNES_REG_SETINI_SCREEN_INTERLACE: return SNES::ppu.regs.interlace?1:0;
		//$2130 CGWSEL
	case SNES_REG_CGWSEL_COLORMASK: return SNES::ppu.regs.color_mask;
	case SNES_REG_CGWSEL_COLORSUBMASK: return SNES::ppu.regs.color_mask;
	case SNES_REG_CGWSEL_ADDSUBMODE: return SNES::ppu.regs.addsub_mode?1:0;
	case SNES_REG_CGWSEL_DIRECTCOLOR: return SNES::ppu.regs.direct_color?1:0;
	}
	return 0;
}

bool snes_load_cartridge_normal(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size
) {
  snes_cheat_reset();
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : SnesCartridge(rom_data, rom_size).markup;
  SNES::cartridge.load(SNES::Cartridge::Mode::Normal, { xmlrom });
  SNES::system.power();
  return true;
}

bool snes_load_cartridge_bsx_slotted(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  snes_cheat_reset();
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : SnesCartridge(rom_data, rom_size).markup;
  if(bsx_data) SNES::bsxflash.memory.copy(bsx_data, bsx_size);
  string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  SNES::cartridge.load(SNES::Cartridge::Mode::BsxSlotted, xmlrom);
  SNES::system.power();
  return true;
}

bool snes_load_cartridge_bsx(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  snes_cheat_reset();
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : SnesCartridge(rom_data, rom_size).markup;
  if(bsx_data) SNES::bsxflash.memory.copy(bsx_data, bsx_size);
  string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  SNES::cartridge.load(SNES::Cartridge::Mode::Bsx, xmlrom);
  SNES::system.power();
  return true;
}

bool snes_load_cartridge_sufami_turbo(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *sta_xml, const uint8_t *sta_data, unsigned sta_size,
  const char *stb_xml, const uint8_t *stb_data, unsigned stb_size
) {
  snes_cheat_reset();
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : SnesCartridge(rom_data, rom_size).markup;
  if(sta_data) SNES::sufamiturbo.slotA.rom.copy(sta_data, sta_size);
  string xmlsta = (sta_xml && *sta_xml) ? string(sta_xml) : SnesCartridge(sta_data, sta_size).markup;
  if(stb_data) SNES::sufamiturbo.slotB.rom.copy(stb_data, stb_size);
  string xmlstb = (stb_xml && *stb_xml) ? string(stb_xml) : SnesCartridge(stb_data, stb_size).markup;
  SNES::cartridge.load(SNES::Cartridge::Mode::SufamiTurbo, xmlrom);
  SNES::system.power();
  return true;
}

bool snes_load_cartridge_super_game_boy(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *dmg_xml, const uint8_t *dmg_data, unsigned dmg_size
) {
  snes_cheat_reset();
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : SnesCartridge(rom_data, rom_size).markup;
  if(dmg_data) {
    //GameBoyCartridge needs to modify dmg_data (for MMM01 emulation); so copy data
    uint8_t *data = new uint8_t[dmg_size];
    memcpy(data, dmg_data, dmg_size);
    string xmldmg = (dmg_xml && *dmg_xml) ? string(dmg_xml) : GameBoyCartridge(data, dmg_size).markup;
    GameBoy::cartridge.load(GameBoy::System::Revision::SuperGameBoy, xmldmg, data, dmg_size);
    delete[] data;
  }
  SNES::cartridge.load(SNES::Cartridge::Mode::SuperGameBoy, xmlrom);
  SNES::system.power();
  return true;
}

void snes_unload_cartridge(void) {
  SNES::cartridge.unload();
}

bool snes_get_region(void) {
  return SNES::system.region() == SNES::System::Region::NTSC ? 0 : 1;
}

uint8_t* snes_get_memory_data(unsigned id) {
  if(SNES::cartridge.loaded() == false) return 0;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      return SNES::cartridge.ram.data();
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SNES::cartridge.has_srtc()) return SNES::srtc.rtc;
      if(SNES::cartridge.has_spc7110rtc()) return SNES::spc7110.rtc;
      return 0;
    case SNES_MEMORY_BSX_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      return SNES::bsxcartridge.sram.data();
    case SNES_MEMORY_BSX_PRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      return SNES::bsxcartridge.psram.data();
    case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
      return SNES::sufamiturbo.slotA.ram.data();
    case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
      return SNES::sufamiturbo.slotB.ram.data();
    case SNES_MEMORY_GAME_BOY_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      return GameBoy::cartridge.ramdata;
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
  //  return GameBoy::cartridge.rtcdata;

    case SNES_MEMORY_WRAM:
      return SNES::cpu.wram;
    case SNES_MEMORY_APURAM:
      return SNES::smp.apuram;
    case SNES_MEMORY_VRAM:
      return SNES::ppu.vram;
    case SNES_MEMORY_OAM:
      return SNES::ppu.oam;
    case SNES_MEMORY_CGRAM:
      return SNES::ppu.cgram;
  }

  return 0;
}

unsigned snes_get_memory_size(unsigned id) {
  if(SNES::cartridge.loaded() == false) return 0;
  unsigned size = 0;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      size = SNES::cartridge.ram.size();
      break;
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SNES::cartridge.has_srtc() || SNES::cartridge.has_spc7110rtc()) size = 20;
      break;
    case SNES_MEMORY_BSX_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      size = SNES::bsxcartridge.sram.size();
      break;
    case SNES_MEMORY_BSX_PRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      size = SNES::bsxcartridge.psram.size();
      break;
    case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
      size = SNES::sufamiturbo.slotA.ram.size();
      break;
    case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
      size = SNES::sufamiturbo.slotB.ram.size();
      break;
    case SNES_MEMORY_GAME_BOY_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      size = GameBoy::cartridge.ramsize;
      break;
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
  //  size = GameBoy::cartridge.rtcsize;
  //  break;

    case SNES_MEMORY_WRAM:
      size = 128 * 1024;
      break;
    case SNES_MEMORY_APURAM:
      size = 64 * 1024;
      break;
    case SNES_MEMORY_VRAM:
      size = 64 * 1024;
      break;
    case SNES_MEMORY_OAM:
      size = 544;
      break;
    case SNES_MEMORY_CGRAM:
      size = 512;
      break;
  }

  if(size == -1U) size = 0;
  return size;
}

uint8_t bus_read(unsigned addr) {
  return SNES::bus.read(addr);
}
void bus_write(unsigned addr, uint8_t val) {
  SNES::bus.write(addr, val);
}

int snes_poll_message()
{
	if(interface.messages.size() == 0) return -1;
	return interface.messages.front().length();
}
void snes_dequeue_message(char* buffer)
{
	int len = interface.messages.front().length();
	memcpy(buffer,(const char*)interface.messages.front(),len);
	interface.messages.pop();
}