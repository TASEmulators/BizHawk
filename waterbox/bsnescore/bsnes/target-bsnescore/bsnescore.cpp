#include "bsnescore.hpp"
#include <sfc/sfc.hpp>
#include <emulibc.h>
// #include <heuristics/heuristics.hpp>
// #include <heuristics/heuristics.cpp>
// #include <heuristics/super-famicom.cpp>
// #include <heuristics/global_interface.cpp>

// #include <sfc/memory/memory.hpp>
// #include <sfc/cartridge/cartridge.hpp>
#include <nall/hid.hpp>

#include <queue>

using namespace nall;
using namespace SuperFamicom;

struct fInterface : public SuperFamicom::Interface {
	typedef SuperFamicom::Interface BaseType;

  snes_video_refresh_t pvideo_refresh;
  snes_audio_sample_t paudio_sample;
  snes_input_poll_t pinput_poll;
  snes_input_state_t pinput_state;
  snes_no_lag_t pno_lag;
  snes_path_request_t ppath_request;
	snes_allocSharedMemory_t pallocSharedMemory;
	snes_freeSharedMemory_t pfreeSharedMemory;
  snes_trace_t ptrace;
  string basename;
  uint32_t *buffer;
  uint32_t *palette;

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

	//zero 27-sep-2012
	snes_scanlineStart_t pScanlineStart;
	void scanlineStart(int line)
	{
		if(pScanlineStart) pScanlineStart((int)line);
	}

  void message(const string &text) {
		messages.push(text);
  }

  void cpuTrace(uint32_t which, const char *msg) {
    if (ptrace)
			ptrace(which, (const char *)msg);
  }

  string path(SuperFamicom::ID slot, const string &hint)
	{
		if(ppath_request)
		{
			const char* path = ppath_request(*(int*)&slot, hint.data());
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

  fInterface() :
			pvideo_refresh(0),
			paudio_sample(0),
			pinput_poll(0),
			pinput_state(0),
			pno_lag(0),
			ppath_request(0),
			pScanlineStart(0),
			pallocSharedMemory(0),
			pfreeSharedMemory(0),
			backdropColor(-1),
			ptrace(0)
	{
    buffer = (uint32_t*)alloc_invisible(512 * 480 * sizeof(uint32_t));
    palette = (uint32_t*)alloc_invisible(32768 * sizeof(uint32_t));
		// initialize palette here cause why not?
		for(uint color : range(32768)) {
			uint16 r = (color >> 10) & 31;
			uint16 g = (color >>  5) & 31;
			uint16 b = (color >>  0) & 31;

			r = r << 3 | r >> 2; r = r << 8 | r << 0;
			g = g << 3 | g >> 2; g = g << 8 | g << 0;
			b = b << 3 | b >> 2; b = b << 8 | b << 0;

			palette[color] = r >> 8 << 16 | g >> 8 <<  8 | b >> 8 << 0;
		}

		// memset(&cdlInfo,0,sizeof(cdlInfo));
  }
};

void pwrap_init();
fInterface *iface = nullptr;

#include "program.cpp"

namespace SuperFamicom {
	SuperFamicom::Interface *interface()
	{
		if(iface != nullptr) return iface;
		iface = new ::fInterface;
		emulator = iface;
		program = new Program;
		pwrap_init();
		return iface;
	}
}

void* extern_allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1) {
	return iface->allocSharedMemory(memtype, amt, initialByte);
}

const char* snes_library_id(void) {
  static string version = {"bsnes v", Emulator::Version};
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

void snes_set_audio_sample(snes_audio_sample_t audio_sample) {
  iface->paudio_sample = audio_sample;
}

void snes_set_input_poll(snes_input_poll_t input_poll) {
  iface->pinput_poll = input_poll;
}

void snes_set_input_state(snes_input_state_t input_state) {
  iface->pinput_state = input_state;
}

void snes_set_no_lag(snes_no_lag_t no_lag) {
  iface->pno_lag = no_lag;
}

void snes_set_path_request(snes_path_request_t path_request)
{
	 iface->ppath_request = path_request;
}

void snes_set_controller_port_device(bool port, unsigned device) {
	if (port == 0) {
		SuperFamicom::controllerPort1.connect(device);
	} else {
		SuperFamicom::controllerPort2.connect(device);
	}
}

void snes_set_cartridge_basename(const char *basename) {
  iface->basename = basename;
}


void snes_init(void) {

	fprintf(stderr, "snes_init was called!\n");
	//force everything to get initialized, even though it probably already is
	SuperFamicom::interface();

	// needed in order to get audio sync working. should probably figure out what exactly this does
	Emulator::audio.setFrequency(32040.);
}

void snes_term(void) {
	emulator->unload();
}

void snes_power(void) {
	emulator->power();
}

void snes_reset(void) {
	emulator->reset();
}

void snes_run(void) {
	if (iface->pinput_poll) iface->pinput_poll();

	// TODO: I currently have implemented separate poll and state calls, where poll updates the state and the state call just receives this
	// based on the way this is implemented this approach might be useless in terms of reducing polling load, will need confirmation here

	emulator->run();
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
	// Cartridge temp(rom_data, rom_size);
	// return temp.type != Cartridge::TypeUnknown && temp.type != Cartridge::TypeGameBoy;
	return true;
}

//zero 05-sep-2012
int snes_peek_logical_register(int reg)
{
	switch(reg)
	{
		//$2105
		//zero 17-may-2014 TODO - enable these for other profiles
#if !defined(PROFILE_PERFORMANCE) && !defined(PROFILE_ACCURACY) && defined(this_isnt_defined_fukc_you)
	case SNES_REG_BG_MODE: return SuperFamicom::ppu.regs.bg_mode;
	case SNES_REG_BG3_PRIORITY: return SuperFamicom::ppu.regs.bg3_priority;
	case SNES_REG_BG1_TILESIZE: return SuperFamicom::ppu.regs.bg_tilesize[SuperFamicom::PPU::BG1];
	case SNES_REG_BG2_TILESIZE: return SuperFamicom::ppu.regs.bg_tilesize[SuperFamicom::PPU::BG2];
	case SNES_REG_BG3_TILESIZE: return SuperFamicom::ppu.regs.bg_tilesize[SuperFamicom::PPU::BG3];
	case SNES_REG_BG4_TILESIZE: return SuperFamicom::ppu.regs.bg_tilesize[SuperFamicom::PPU::BG4];

		//$2107
	case SNES_REG_BG1_SCADDR: return SuperFamicom::ppu.regs.bg_scaddr[SuperFamicom::PPU::BG1]>>9;
	case SNES_REG_BG1_SCSIZE: return SuperFamicom::ppu.regs.bg_scsize[SuperFamicom::PPU::BG1];
		//$2108
	case SNES_REG_BG2_SCADDR: return SuperFamicom::ppu.regs.bg_scaddr[SuperFamicom::PPU::BG2]>>9;
	case SNES_REG_BG2_SCSIZE: return SuperFamicom::ppu.regs.bg_scsize[SuperFamicom::PPU::BG2];
		//$2109
	case SNES_REG_BG3_SCADDR: return SuperFamicom::ppu.regs.bg_scaddr[SuperFamicom::PPU::BG3]>>9;
	case SNES_REG_BG3_SCSIZE: return SuperFamicom::ppu.regs.bg_scsize[SuperFamicom::PPU::BG3];
		//$210A
	case SNES_REG_BG4_SCADDR: return SuperFamicom::ppu.regs.bg_scaddr[SuperFamicom::PPU::BG4]>>9;
	case SNES_REG_BG4_SCSIZE: return SuperFamicom::ppu.regs.bg_scsize[SuperFamicom::PPU::BG4];
		//$210B
	case SNES_REG_BG1_TDADDR: return SuperFamicom::ppu.regs.bg_tdaddr[SuperFamicom::PPU::BG1]>>13;
	case SNES_REG_BG2_TDADDR: return SuperFamicom::ppu.regs.bg_tdaddr[SuperFamicom::PPU::BG2]>>13;
		//$210C
	case SNES_REG_BG3_TDADDR: return SuperFamicom::ppu.regs.bg_tdaddr[SuperFamicom::PPU::BG3]>>13;
	case SNES_REG_BG4_TDADDR: return SuperFamicom::ppu.regs.bg_tdaddr[SuperFamicom::PPU::BG4]>>13;
		//$2133 SETINI
	case SNES_REG_SETINI_MODE7_EXTBG: return SuperFamicom::ppu.regs.mode7_extbg?1:0;
	case SNES_REG_SETINI_HIRES: return SuperFamicom::ppu.regs.pseudo_hires?1:0;
	case SNES_REG_SETINI_OVERSCAN: return SuperFamicom::ppu.regs.overscan?1:0;
	case SNES_REG_SETINI_OBJ_INTERLACE: return SuperFamicom::ppu.regs.oam_interlace?1:0;
	case SNES_REG_SETINI_SCREEN_INTERLACE: return SuperFamicom::ppu.regs.interlace?1:0;
		//$2130 CGWSEL
	case SNES_REG_CGWSEL_COLORMASK: return SuperFamicom::ppu.regs.color_mask;
	case SNES_REG_CGWSEL_COLORSUBMASK: return SuperFamicom::ppu.regs.colorsub_mask;
	case SNES_REG_CGWSEL_ADDSUBMODE: return SuperFamicom::ppu.regs.addsub_mode?1:0;
	case SNES_REG_CGWSEL_DIRECTCOLOR: return SuperFamicom::ppu.regs.direct_color?1:0;
		//$2101 OBSEL
	case SNES_REG_OBSEL_NAMEBASE: return SuperFamicom::ppu.regs.oam_tdaddr>>14;
	case SNES_REG_OBSEL_NAMESEL: return SuperFamicom::ppu.regs.oam_nameselect;
	case SNES_REG_OBSEL_SIZE: return SuperFamicom::ppu.regs.oam_basesize;
		//$2131 CGADSUB
	//enum { BG1 = 0, BG2 = 1, BG3 = 2, BG4 = 3, OAM = 4, BACK = 5, COL = 5 };
	case SNES_REG_CGADSUB_MODE: return SuperFamicom::ppu.regs.color_mode;
	case SNES_REG_CGADSUB_HALF: return SuperFamicom::ppu.regs.color_halve;
	case SNES_REG_CGADSUB_BG4: return SuperFamicom::ppu.regs.color_enabled[3];
	case SNES_REG_CGADSUB_BG3: return SuperFamicom::ppu.regs.color_enabled[2];
	case SNES_REG_CGADSUB_BG2: return SuperFamicom::ppu.regs.color_enabled[1];
	case SNES_REG_CGADSUB_BG1: return SuperFamicom::ppu.regs.color_enabled[0];
	case SNES_REG_CGADSUB_OBJ: return SuperFamicom::ppu.regs.color_enabled[4];
	case SNES_REG_CGADSUB_BACKDROP: return SuperFamicom::ppu.regs.color_enabled[5];
		//$212C TM
	case SNES_REG_TM_BG1: return SuperFamicom::ppu.regs.bg_enabled[0];
	case SNES_REG_TM_BG2: return SuperFamicom::ppu.regs.bg_enabled[1];
	case SNES_REG_TM_BG3: return SuperFamicom::ppu.regs.bg_enabled[2];
	case SNES_REG_TM_BG4: return SuperFamicom::ppu.regs.bg_enabled[3];
	case SNES_REG_TM_OBJ: return SuperFamicom::ppu.regs.bg_enabled[4];
		//$212D TM
	case SNES_REG_TS_BG1: return SuperFamicom::ppu.regs.bgsub_enabled[0];
	case SNES_REG_TS_BG2: return SuperFamicom::ppu.regs.bgsub_enabled[1];
	case SNES_REG_TS_BG3: return SuperFamicom::ppu.regs.bgsub_enabled[2];
	case SNES_REG_TS_BG4: return SuperFamicom::ppu.regs.bgsub_enabled[3];
	case SNES_REG_TS_OBJ: return SuperFamicom::ppu.regs.bgsub_enabled[4];
		//Mode7 regs
	case SNES_REG_M7SEL_REPEAT: return SuperFamicom::ppu.regs.mode7_repeat;
	case SNES_REG_M7SEL_HFLIP: return SuperFamicom::ppu.regs.mode7_vflip;
	case SNES_REG_M7SEL_VFLIP: return SuperFamicom::ppu.regs.mode7_hflip;
	case SNES_REG_M7A: return SuperFamicom::ppu.regs.m7a;
	case SNES_REG_M7B: return SuperFamicom::ppu.regs.m7b;
	case SNES_REG_M7C: return SuperFamicom::ppu.regs.m7c;
	case SNES_REG_M7D: return SuperFamicom::ppu.regs.m7d;
	case SNES_REG_M7X: return SuperFamicom::ppu.regs.m7x;
	case SNES_REG_M7Y: return SuperFamicom::ppu.regs.m7y;
		//BG scroll regs
	case SNES_REG_BG1HOFS: return SuperFamicom::ppu.regs.bg_hofs[0] & 0x3FF;
	case SNES_REG_BG1VOFS: return SuperFamicom::ppu.regs.bg_vofs[0] & 0x3FF;
	case SNES_REG_BG2HOFS: return SuperFamicom::ppu.regs.bg_hofs[1] & 0x3FF;
	case SNES_REG_BG2VOFS: return SuperFamicom::ppu.regs.bg_vofs[1] & 0x3FF;
	case SNES_REG_BG3HOFS: return SuperFamicom::ppu.regs.bg_hofs[2] & 0x3FF;
	case SNES_REG_BG3VOFS: return SuperFamicom::ppu.regs.bg_vofs[2] & 0x3FF;
	case SNES_REG_BG4HOFS: return SuperFamicom::ppu.regs.bg_hofs[3] & 0x3FF;
	case SNES_REG_BG4VOFS: return SuperFamicom::ppu.regs.bg_vofs[3] & 0x3FF;
	case SNES_REG_M7HOFS: return SuperFamicom::ppu.regs.m7_hofs & 0x1FFF; //rememebr to make these signed with <<19>>19
	case SNES_REG_M7VOFS: return SuperFamicom::ppu.regs.m7_vofs & 0x1FFF; //rememebr to make these signed with <<19>>19
#endif

	}
	return 0;
}

bool snes_load_cartridge_normal(
  const char *base_rom_path, const uint8_t *rom_data, unsigned rom_size
) {
	// let's hope this works
	program->superFamicom.location = base_rom_path;

	vector<uint8_t> rom_data_vector;
	for (int i = 0; i < rom_size; i++) rom_data_vector.append(rom_data[i]);
	program->superFamicom.raw_data = rom_data_vector;

  program->load();
  return true;
}

bool snes_load_cartridge_bsx_slotted(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  // if(rom_data) SuperFamicom::cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = SuperFamicom::cartridge;//SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : "";iface->cart.markup;
  // if(bsx_data) SuperFamicom::bsxflash.memory.copy(bsx_data, bsx_size);
  // string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  // SuperFamicom::cartridge.load(SuperFamicom::Cartridge::Mode::BsxSlotted, xmlrom);
  // SuperFamicom::system.power(false);
  return false;
}

bool snes_load_cartridge_bsx(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  // if(rom_data) SuperFamicom::cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
  // if(bsx_data) SuperFamicom::bsxflash.memory.copy(bsx_data, bsx_size);
  // string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  // SuperFamicom::cartridge.load(SuperFamicom::Cartridge::Mode::Bsx, xmlrom);
  // SuperFamicom::system.power(false);
  return false;
}

bool snes_load_cartridge_sufami_turbo(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *sta_xml, const uint8_t *sta_data, unsigned sta_size,
  const char *stb_xml, const uint8_t *stb_data, unsigned stb_size
) {
  // if(rom_data) SuperFamicom::cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
  // if(sta_data) SuperFamicom::sufamiturbo.slotA.rom.copy(sta_data, sta_size);
  // string xmlsta = (sta_xml && *sta_xml) ? string(sta_xml) : SnesCartridge(sta_data, sta_size).markup;
  // if(stb_data) SuperFamicom::sufamiturbo.slotB.rom.copy(stb_data, stb_size);
  // string xmlstb = (stb_xml && *stb_xml) ? string(stb_xml) : SnesCartridge(stb_data, stb_size).markup;
  // SuperFamicom::cartridge.load(SuperFamicom::Cartridge::Mode::SufamiTurbo, xmlrom);
  // SuperFamicom::system.power(false);
  return false;
}

bool snes_load_cartridge_super_game_boy(
  const char *base_rom_path, const uint8_t *rom_data, unsigned rom_size,
  const uint8_t *sgb_rom_data, unsigned sgb_rom_size
) {
	program->superFamicom.location = base_rom_path;

	vector<uint8_t> rom_data_vector;
	for (int i = 0; i < rom_size; i++) rom_data_vector.append(rom_data[i]);
	program->superFamicom.raw_data = rom_data_vector;

	vector<uint8_t> sgb_rom_data_vector;
	for (int i = 0; i < sgb_rom_size; i++) sgb_rom_data_vector.append(sgb_rom_data[i]);
	program->gameBoy.program = sgb_rom_data_vector;

  program->load();
  return true;
}

void snes_unload_cartridge(void) {
  SuperFamicom::cartridge.unload();
}

bool snes_get_region(void) {
	return SuperFamicom::Region::PAL();
}

char snes_get_mapper(void) {
	string board = program->superFamicom.document["game/board"].text();
	string mapper = board.split('-', 1)[0];
	if (mapper == "LOROM") return 0;
	if (mapper == "HIROM") return 1;
	if (mapper == "EXLOROM") return 2;
	if (mapper == "EXHIROM") return 3;
	if (mapper == "SUPERFXROM") return 4;
	if (mapper == "SA1ROM") return 5;
	if (mapper == "SPC7110ROM") return 6;
	if (mapper == "BSCLOROM") return 7;
	if (mapper == "BSCHIROM") return 8;
	if (mapper == "BSXROM") return 9;
	if (mapper == "STROM") return 10;

	return -1;
}

uint8_t* snes_get_memory_data(unsigned id) {
  if(emulator->loaded() == false) return 0;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      return SuperFamicom::cartridge.ram.data();
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SuperFamicom::cartridge.has.SharpRTC) return new uint8_t[20];// SuperFamicom::sharprtc srtc.rtc;
      if(SuperFamicom::cartridge.has.SPC7110) return new uint8_t[20];//SuperFamicom::spc7110 .rtc;
      return 0;
  //   case SNES_MEMORY_BSX_RAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::Bsx) break;
  //     return SuperFamicom::bsxcartridge.sram.data();
  //   case SNES_MEMORY_BSX_PRAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::Bsx) break;
  //     return SuperFamicom::bsxcartridge.psram.data();
  //   case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
  //     return SuperFamicom::sufamiturbo.slotA.ram.data();
  //   case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
  //     return SuperFamicom::sufamiturbo.slotB.ram.data();
  //   case SNES_MEMORY_GAME_BOY_CARTRAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  //     return GameBoy::cartridge.ramdata;
  // //case SNES_MEMORY_GAME_BOY_RTC:
  // //  if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  // //  return GameBoy::cartridge.rtcdata;
  //   case SNES_MEMORY_GAME_BOY_WRAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  //     return GameBoy::cpu.wram;
  //   case SNES_MEMORY_GAME_BOY_HRAM:
  //     if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  //     return GameBoy::cpu.hram;

  //   case SNES_MEMORY_WRAM:
  //     return SuperFamicom::cpu.wram;
  //   case SNES_MEMORY_APURAM:
  //     return SuperFamicom::smp.apuram;
  //   case SNES_MEMORY_VRAM:
  //     return SuperFamicom::ppu.vram;
  //   case SNES_MEMORY_OAM:
  //     return SuperFamicom::ppu.oam;
  //   case SNES_MEMORY_CGRAM:
  //     return SuperFamicom::ppu.cgram;

		case SNES_MEMORY_CARTRIDGE_ROM:
      return SuperFamicom::cartridge.rom.data();
  }

  return 0;
}

const char* snes_get_memory_id_name(unsigned id) {
  if(emulator->loaded() == false) return nullptr;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      return "CARTRIDGE_RAM";
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SuperFamicom::cartridge.has.SharpRTC) return "RTC";
      if(SuperFamicom::cartridge.has.SPC7110) return "SPC7110_RTC";
      return nullptr;
    case SNES_MEMORY_BSX_RAM:
      if(SuperFamicom::cartridge.has.BSMemorySlot)// mode() != SuperFamicom::Cartridge::Mode::Bsx) break;
      return "BSX_SRAM";
    case SNES_MEMORY_BSX_PRAM:
      if(SuperFamicom::cartridge.has.BSMemorySlot)//() != SuperFamicom::Cartridge::Mode::Bsx) break;
      return "BSX_PSRAM";
    case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
      if(SuperFamicom::cartridge.has.SufamiTurboSlotA)// mode() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
			return "SUFAMI_SLOTARAM";
    case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
      if(SuperFamicom::cartridge.has.SufamiTurboSlotB)//() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
      return "SUFAMI_SLOTBRAM";
    case SNES_MEMORY_GAME_BOY_CARTRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)//() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      //return GameBoy::cartridge.ramdata;
			return "SGB_CARTRAM";
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  //  return GameBoy::cartridge.rtcdata;
    case SNES_MEMORY_GAME_BOY_WRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)//() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      //see notes in SetupMemoryDomains in bizhawk
      return "SGB_WRAM";
    case SNES_MEMORY_GAME_BOY_HRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)//() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      return "SGB_HRAM";

    case SNES_MEMORY_WRAM:
      //return SuperFamicom::cpu.wram;
			return "WRAM";
    case SNES_MEMORY_APURAM:
      //return SuperFamicom::smp.apuram;
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
  if(emulator->loaded() == false) return 0;
  unsigned size = 0;

  switch(id) {
    case SNES_MEMORY_CARTRIDGE_RAM:
      size = SuperFamicom::cartridge.ram.size();
      break;
    case SNES_MEMORY_CARTRIDGE_RTC:
      if(SuperFamicom::cartridge.has.SharpRTC || SuperFamicom::cartridge.has.SPC7110) size = 20;
      break;
    case SNES_MEMORY_BSX_RAM:
		case SNES_MEMORY_BSX_PRAM:
      if(SuperFamicom::cartridge.has.BSMemorySlot)
      size = SuperFamicom::bsmemory.size();// bsxcartridge.sram.size();
      break;
    // case SNES_MEMORY_BSX_PRAM:
      // if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::Bsx) break;
      // size = SuperFamicom::bsxcartridge.psram.size();
      // break;
    case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
      if(SuperFamicom::cartridge.has.SufamiTurboSlotA)// .mode() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
      size = SuperFamicom::sufamiturboA.ram.size();// sufamiturbo.slotA.ram.size();
      break;
    case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
      if(SuperFamicom::cartridge.has.SufamiTurboSlotB)// mode() != SuperFamicom::Cartridge::Mode::SufamiTurbo) break;
      size = SuperFamicom::sufamiturboB.ram.size();// .slotB.ram.size();
      break;
    case SNES_MEMORY_GAME_BOY_CARTRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)// mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      size = SuperFamicom::cartridge.ram.size();// GameBoy::cartridge.ramsize;
      break;
  //case SNES_MEMORY_GAME_BOY_RTC:
  //  if(SuperFamicom::cartridge.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
  //  size = GameBoy::cartridge.rtcsize;
  //  break;
    case SNES_MEMORY_GAME_BOY_WRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)// mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      //see notes in SetupMemoryDomains in bizhawk
      size = 32768;
      break;
    case SNES_MEMORY_GAME_BOY_HRAM:
      if(SuperFamicom::cartridge.has.GameBoySlot)//.mode() != SuperFamicom::Cartridge::Mode::SuperGameBoy) break;
      size = 128;
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
      size =  SuperFamicom::cartridge.rom.size();
      break;
  }

  if(size == -1U) size = 0;
  return size;
}

uint8_t bus_read(unsigned addr) {
  return SuperFamicom::bus.read(addr);
}
void bus_write(unsigned addr, uint8_t val) {
  SuperFamicom::bus.write(addr, val);
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
	// iface->wanttrace = mask;
	if (mask)
		iface->ptrace = callback;
	else
		iface->ptrace = nullptr;
}
