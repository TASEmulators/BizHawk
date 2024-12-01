#include "libsnes.hpp"
#include <snes/snes.hpp>

#include <nall/snes/cartridge.hpp>
#include <nall/gameboy/cartridge.hpp>

#include <queue>

using namespace nall;

struct Interface : public SNES::Interface {
	typedef SNES::Interface BaseType;

  snes_video_refresh_t pvideo_refresh;
  snes_audio_sample_t paudio_sample;
  snes_input_poll_t pinput_poll;
  snes_input_state_t pinput_state;
  snes_input_notify_t pinput_notify;
  snes_path_request_t ppath_request;
	snes_allocSharedMemory_t pallocSharedMemory;
	snes_freeSharedMemory_t pfreeSharedMemory;
  snes_trace_t ptrace;
  string basename;
  uint32_t *buffer;
  uint32_t *palette;

  SnesCartridge cart;

	//zero 11-sep-2012
	time_t randomSeed() { return 0; }

	//zero 26-sep-2012
	std::queue<nall::string> messages;

	//zero 17-oct-2012
	int backdropColor;
	int getBackdropColor()
	{
		return backdropColor;
	}

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
  
  void cpuTrace(uint32_t which, const char *msg) {
    if (ptrace)
			ptrace(which, (const char *)msg);
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


	//zero 23-dec-2012
	void* allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1)
	{
		void* ret;
		//if pallocSharedMemory isnt set up yet, we're going to have serious problems
		ret = pallocSharedMemory(memtype,amt);
		if(initialByte != -1)
		{
			for(unsigned i = 0; i < amt; i++) ((uint8*)ret)[i] = (uint8)initialByte;
		}
		return ret;
	}
	void freeSharedMemory(void* ptr)
	{
		if(!pfreeSharedMemory) return; //?? 
		pfreeSharedMemory(ptr);
	}

  Interface() : 
			pvideo_refresh(0), 
			paudio_sample(0), 
			pinput_poll(0), 
			pinput_state(0), 
			pinput_notify(0), 
			ppath_request(0),
			pScanlineStart(0),
			pallocSharedMemory(0),
			pfreeSharedMemory(0),
			backdropColor(-1),
			ptrace(0),
			cart(nullptr, 0)
	{
    buffer = (uint32_t*)alloc_invisible(512 * 480 * sizeof(uint32_t));
    palette = (uint32_t*)alloc_invisible(16 * 32768 * sizeof(uint32_t));
		memset(&cdlInfo,0,sizeof(cdlInfo));
  }

  ~Interface() {
    abort();
  }
};

void pwrap_init();
Interface *iface = nullptr;
namespace SNES {
SNES::Interface *interface()
{
	if(iface != nullptr) return iface;
	iface = new ::Interface();
	pwrap_init();
	return iface;
}
}

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

void snes_set_allocSharedMemory(snes_allocSharedMemory_t cb)
{
	iface->pallocSharedMemory = cb;
}
void snes_set_freeSharedMemory(snes_freeSharedMemory_t cb)
{
	iface->pfreeSharedMemory = cb;
}

void snes_set_video_refresh(snes_video_refresh_t video_refresh) {
  iface->pvideo_refresh = video_refresh;
}

void snes_set_color_lut(uint32_t * colors) {
  for (int i = 0; i < 16 * 32768; i++)
    iface->palette[i] = colors[i];
}

void snes_set_audio_sample(snes_audio_sample_t audio_sample) {
  iface->paudio_sample = audio_sample;
}

void snes_set_input_poll(snes_input_poll_t input_poll) {
  iface->pinput_poll = input_poll;
}

void snes_set_input_state(snes_input_state_t input_state) {
  iface->pinput_state = input_state;
}

void snes_set_input_notify(snes_input_notify_t input_notify) {
  iface->pinput_notify = input_notify;
}

void snes_set_path_request(snes_path_request_t path_request)
{
	 iface->ppath_request = path_request;
}

void snes_set_controller_port_device(bool port, unsigned device) {
  SNES::input.connect(port, (SNES::Input::Device)device);
}

void snes_set_cartridge_basename(const char *basename) {
  iface->basename = basename;
}

template<typename T> inline void reconstruct(T* t) { 
	/*t->~T();
	memset(t,0,sizeof(*t));
	new(t) T();*/
}

void snes_init(void) {

	//force everything to get initialized, even though it probably already is
	SNES::interface();

	//zero 01-sep-2014 - this is too slow. made rewind totally boring. made other edits to firmware chips to preserve their roms instead
	//zero 22-may-2014 - why not this too, for the sake of completeness? 
	//reconstruct(&SNES::cartridge);

	//zero 01-dec-2012 - due to systematic variable initialization fails in bsnes components, these reconstructions are necessary,
	//and the previous comment here which called this paranoid has been removed.
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
  reconstruct(&SNES::srtc); SNES::srtc.initialize();
  reconstruct(&SNES::sdd1);
  reconstruct(&SNES::spc7110); SNES::spc7110.initialize();
  reconstruct(&SNES::obc1);
  reconstruct(&SNES::msu1);
  reconstruct(&SNES::link);
  reconstruct(&SNES::video);
  reconstruct(&SNES::audio);

	//zero 01-dec-2012 - forgot to do all these. massive desync chaos!
	//remove these to make it easier to find initialization fails in the component power-ons / constructors / etc.
	//or just forget about it. this snes_init gets called paranoidly frequently by bizhawk, so things should stay zeroed correctly
	reconstruct(&SNES::cpu); SNES::cpu.initialize();
	reconstruct(&SNES::smp); SNES::smp.initialize();
	reconstruct(&SNES::dsp);
	reconstruct(&SNES::ppu);
	SNES::ppu.initialize();
  SNES::system.init();
  
  //zero 26-aug-2013 - yup. still more
  reconstruct(&GameBoy::cpu); GameBoy::cpu.initialize();
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

//zero 21-sep-2012
void snes_set_scanlineStart(snes_scanlineStart_t cb)
{
	iface->pScanlineStart = cb;
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
		//zero 17-may-2014 TODO - enable these for other profiles
#if !defined(PROFILE_PERFORMANCE) && !defined(PROFILE_ACCURACY)
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
	case SNES_REG_CGWSEL_COLORSUBMASK: return SNES::ppu.regs.colorsub_mask;
	case SNES_REG_CGWSEL_ADDSUBMODE: return SNES::ppu.regs.addsub_mode?1:0;
	case SNES_REG_CGWSEL_DIRECTCOLOR: return SNES::ppu.regs.direct_color?1:0;
		//$2101 OBSEL
	case SNES_REG_OBSEL_NAMEBASE: return SNES::ppu.regs.oam_tdaddr>>14;
	case SNES_REG_OBSEL_NAMESEL: return SNES::ppu.regs.oam_nameselect;
	case SNES_REG_OBSEL_SIZE: return SNES::ppu.regs.oam_basesize;
		//$2131 CGADSUB
	//enum { BG1 = 0, BG2 = 1, BG3 = 2, BG4 = 3, OAM = 4, BACK = 5, COL = 5 };
	case SNES_REG_CGADSUB_MODE: return SNES::ppu.regs.color_mode;
	case SNES_REG_CGADSUB_HALF: return SNES::ppu.regs.color_halve;
	case SNES_REG_CGADSUB_BG4: return SNES::ppu.regs.color_enabled[3];
	case SNES_REG_CGADSUB_BG3: return SNES::ppu.regs.color_enabled[2];
	case SNES_REG_CGADSUB_BG2: return SNES::ppu.regs.color_enabled[1];
	case SNES_REG_CGADSUB_BG1: return SNES::ppu.regs.color_enabled[0];
	case SNES_REG_CGADSUB_OBJ: return SNES::ppu.regs.color_enabled[4];
	case SNES_REG_CGADSUB_BACKDROP: return SNES::ppu.regs.color_enabled[5];
		//$212C TM
	case SNES_REG_TM_BG1: return SNES::ppu.regs.bg_enabled[0];
	case SNES_REG_TM_BG2: return SNES::ppu.regs.bg_enabled[1];
	case SNES_REG_TM_BG3: return SNES::ppu.regs.bg_enabled[2];
	case SNES_REG_TM_BG4: return SNES::ppu.regs.bg_enabled[3];
	case SNES_REG_TM_OBJ: return SNES::ppu.regs.bg_enabled[4];
		//$212D TM
	case SNES_REG_TS_BG1: return SNES::ppu.regs.bgsub_enabled[0];
	case SNES_REG_TS_BG2: return SNES::ppu.regs.bgsub_enabled[1];
	case SNES_REG_TS_BG3: return SNES::ppu.regs.bgsub_enabled[2];
	case SNES_REG_TS_BG4: return SNES::ppu.regs.bgsub_enabled[3];
	case SNES_REG_TS_OBJ: return SNES::ppu.regs.bgsub_enabled[4];
		//Mode7 regs
	case SNES_REG_M7SEL_REPEAT: return SNES::ppu.regs.mode7_repeat;
	case SNES_REG_M7SEL_HFLIP: return SNES::ppu.regs.mode7_vflip;
	case SNES_REG_M7SEL_VFLIP: return SNES::ppu.regs.mode7_hflip;
	case SNES_REG_M7A: return SNES::ppu.regs.m7a;
	case SNES_REG_M7B: return SNES::ppu.regs.m7b;
	case SNES_REG_M7C: return SNES::ppu.regs.m7c;
	case SNES_REG_M7D: return SNES::ppu.regs.m7d;
	case SNES_REG_M7X: return SNES::ppu.regs.m7x;
	case SNES_REG_M7Y: return SNES::ppu.regs.m7y;
		//BG scroll regs
	case SNES_REG_BG1HOFS: return SNES::ppu.regs.bg_hofs[0] & 0x3FF;
	case SNES_REG_BG1VOFS: return SNES::ppu.regs.bg_vofs[0] & 0x3FF;
	case SNES_REG_BG2HOFS: return SNES::ppu.regs.bg_hofs[1] & 0x3FF;
	case SNES_REG_BG2VOFS: return SNES::ppu.regs.bg_vofs[1] & 0x3FF;
	case SNES_REG_BG3HOFS: return SNES::ppu.regs.bg_hofs[2] & 0x3FF;
	case SNES_REG_BG3VOFS: return SNES::ppu.regs.bg_vofs[2] & 0x3FF;
	case SNES_REG_BG4HOFS: return SNES::ppu.regs.bg_hofs[3] & 0x3FF;
	case SNES_REG_BG4VOFS: return SNES::ppu.regs.bg_vofs[3] & 0x3FF;
	case SNES_REG_M7HOFS: return SNES::ppu.regs.m7_hofs & 0x1FFF; //rememebr to make these signed with <<19>>19
	case SNES_REG_M7VOFS: return SNES::ppu.regs.m7_vofs & 0x1FFF; //rememebr to make these signed with <<19>>19
#endif

	}
	return 0;
}

bool snes_load_cartridge_normal(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size
) {
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  iface->cart = SnesCartridge(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
  SNES::cartridge.load(SNES::Cartridge::Mode::Normal, { xmlrom });
  SNES::system.power();
  return true;
}

bool snes_load_cartridge_bsx_slotted(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  iface->cart = SnesCartridge(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
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
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  iface->cart = SnesCartridge(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
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
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  iface->cart = SnesCartridge(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
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
  if(rom_data) SNES::cartridge.rom.copy(rom_data, rom_size);
  iface->cart = SnesCartridge(rom_data, rom_size);
  string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
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

char snes_get_mapper(void) {
  return iface->cart.mapper;
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
    case SNES_MEMORY_GAME_BOY_CARTRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      return GameBoy::cartridge.ramdata;
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
  //  return GameBoy::cartridge.rtcdata;
    case SNES_MEMORY_GAME_BOY_WRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      return GameBoy::cpu.wram;  
    case SNES_MEMORY_GAME_BOY_HRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      return GameBoy::cpu.hram;        
    case SNES_MEMORY_SA1_IRAM:
      if(!SNES::cartridge.has_sa1()) break;
      return SNES::sa1.iram.data();

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
    
		case SNES_MEMORY_CARTRIDGE_ROM:
      return SNES::cartridge.rom.data();
  }

  return 0;
}

const char* snes_get_memory_id_name(unsigned id) {
  if(SNES::cartridge.loaded() == false) return nullptr;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      return "CARTRIDGE_RAM";
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SNES::cartridge.has_srtc()) return "RTC";
      if(SNES::cartridge.has_spc7110rtc()) return "SPC7110_RTC";
      return nullptr;
    case SNES_MEMORY_BSX_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      return "BSX_SRAM"; 
    case SNES_MEMORY_BSX_PRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::Bsx) break;
      return "BSX_PSRAM";
    case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
			return "SUFAMI_SLOTARAM";
    case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SufamiTurbo) break;
      return "SUFAMI_SLOTBRAM";
    case SNES_MEMORY_GAME_BOY_CARTRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      //return GameBoy::cartridge.ramdata;
			return "SGB_CARTRAM";
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
  //  return GameBoy::cartridge.rtcdata;
    case SNES_MEMORY_GAME_BOY_WRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      //see notes in SetupMemoryDomains in bizhawk
      return "SGB_WRAM";
    case SNES_MEMORY_GAME_BOY_HRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      return "SGB_HRAM";
    case SNES_MEMORY_SA1_IRAM:
      if(!SNES::cartridge.has_sa1()) break;
      return "SA1_IRAM";

    case SNES_MEMORY_WRAM:
      //return SNES::cpu.wram;
			return "WRAM";
    case SNES_MEMORY_APURAM:
      //return SNES::smp.apuram;
			return "APURAM";
    case SNES_MEMORY_VRAM:
      return "VRAM";
    case SNES_MEMORY_OAM:
      return "OAM";
    case SNES_MEMORY_CGRAM:
      return "CGRAM";

    case SNES_MEMORY_CARTRIDGE_ROM:
      return "CARTRIDGE_ROM";
  }

  return nullptr;
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
    case SNES_MEMORY_GAME_BOY_CARTRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      size = GameBoy::cartridge.ramsize;
      break;
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
  //  size = GameBoy::cartridge.rtcsize;
  //  break;
    case SNES_MEMORY_GAME_BOY_WRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      //see notes in SetupMemoryDomains in bizhawk
      size = 32768;
      break;  
    case SNES_MEMORY_GAME_BOY_HRAM:
      if(SNES::cartridge.mode() != SNES::Cartridge::Mode::SuperGameBoy) break;
      size = 128;
      break;
    case SNES_MEMORY_SA1_IRAM:
      if(!SNES::cartridge.has_sa1()) break;
      size = SNES::sa1.iram.size();
      break;

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

    case SNES_MEMORY_CARTRIDGE_ROM:
      size =  SNES::cartridge.rom.size();
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
	if(iface->messages.empty()) return -1;
	return iface->messages.front().length();
}
void snes_dequeue_message(char* buffer)
{
	int len = iface->messages.front().length();
	memcpy(buffer,(const char*)iface->messages.front(),len);
	iface->messages.pop();
}

void snes_set_backdropColor(int color)
{
	iface->backdropColor = color;
}

void snes_set_trace_callback(uint32_t mask, snes_trace_t callback)
{
	iface->wanttrace = mask;
	if (mask)
		iface->ptrace = callback;
	else
		iface->ptrace = nullptr;
}