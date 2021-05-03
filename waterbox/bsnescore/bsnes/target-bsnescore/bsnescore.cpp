#include "bsnescore.hpp"
#include <sfc/sfc.hpp>
#include <emulibc.h>
#include <nall/hid.hpp>

#include <queue>

using namespace nall;
using namespace SuperFamicom;

struct fInterface : public Interface {
    typedef Interface BaseType;

    snes_trace_t ptrace;
    uint32_t *buffer;
    uint32_t *palette;

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


    //zero 23-dec-2012
    void* allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1)
    {
        void* ret;
        ret = snes_allocSharedMemory(memtype,amt);
        if(initialByte != -1)
        {
            for(unsigned i = 0; i < amt; i++) ((uint8*)ret)[i] = (uint8)initialByte;
        }
        return ret;
    }
    void freeSharedMemory(void* ptr)
    {
        snes_freeSharedMemory(ptr);
    }

  fInterface() :
        pScanlineStart(0),
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

fInterface *iface = nullptr;

#include "program.cpp"

namespace SuperFamicom {
    Interface *interface()
    {
        if(iface != nullptr) return iface;
        iface = new ::fInterface;
        emulator = iface;
        program = new Program;
        return iface;
    }
}

// if ever used from inside bsnes should probably go through platform->allocSharedMemory
void* extern_allocSharedMemory(const char* memtype, size_t amt, int initialByte = -1) {
    return iface->allocSharedMemory(memtype, amt, initialByte);
}

// leaving for now, but unused
const char* snes_library_id(void) {
    static string version = {"bsnes v", Emulator::Version};
    return version;
}


void snes_init(int entropy)
{
    fprintf(stderr, "snes_init was called!\n");
    interface();

    string entropy_string;
    switch (entropy)
    {
        case 0: entropy_string = "None"; break;
        case 1: entropy_string = "Low"; break;
        case 2: entropy_string = "High"; break;
    }
    emulator->configure("Hacks/Entropy", entropy_string);

    // needed in order to get audio sync working. should probably figure out what exactly this does or how to change that properly
    Emulator::audio.setFrequency(SAMPLE_RATE);
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

// static int runahead_frames = 1;

// run with runahead doesn't work yet, i suspect it's due to either waterbox or the serialize thing breaking
// static void run_with_runahead(const int frames)
// {
//     assert(frames > 0);

//     emulator->setRunAhead(true);
//     emulator->run();
//     auto state = emulator->serialize(false);
//     for (int i = 0; i < frames - 1; i++) {
//         emulator->run();
//     }
//     emulator->setRunAhead(false);
//     emulator->run();
//     state.setMode(serializer::Mode::Load);
//     emulator->unserialize(state);
// }

void snes_run(void) {
    snes_input_poll();

    // TODO: I currently have implemented separate poll and state calls, where poll updates the state and the state call just receives this
    // based on the way this is implemented this approach might be useless in terms of reducing polling load, will need confirmation here

    // if (runahead_frames > 0)
        // run_with_runahead(runahead_frames);
    // else
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
    if (emulator->configuration("Hacks/PPU/Fast") == "true")
    switch(reg)
    {
        //zero 17-may-2014
        // 3-may-2021 above timestamp left for reference because i like it

        //$2105
        case SNES_REG_BG_MODE: return ppufast.io.bgMode;
        case SNES_REG_BG3_PRIORITY: return ppufast.io.bgPriority;
        case SNES_REG_BG1_TILESIZE: return ppufast.io.bg1.tileSize;
        case SNES_REG_BG2_TILESIZE: return ppufast.io.bg2.tileSize;
        case SNES_REG_BG3_TILESIZE: return ppufast.io.bg3.tileSize;
        case SNES_REG_BG4_TILESIZE: return ppufast.io.bg4.tileSize;
            //$2107
        case SNES_REG_BG1_SCADDR: return ppufast.io.bg1.screenAddress >> 8;
        case SNES_REG_BG1_SCSIZE: return ppufast.io.bg1.screenSize;
            //$2108
        case SNES_REG_BG2_SCADDR: return ppufast.io.bg2.screenAddress >> 8;
        case SNES_REG_BG2_SCSIZE: return ppufast.io.bg2.screenSize;
            //$2109
        case SNES_REG_BG3_SCADDR: return ppufast.io.bg3.screenAddress >> 8;
        case SNES_REG_BG3_SCSIZE: return ppufast.io.bg3.screenSize;
            //$210A
        case SNES_REG_BG4_SCADDR: return ppufast.io.bg4.screenAddress >> 8;
        case SNES_REG_BG4_SCSIZE: return ppufast.io.bg4.screenSize;
            //$210B
        case SNES_REG_BG1_TDADDR: return ppufast.io.bg1.tiledataAddress >> 12;
        case SNES_REG_BG2_TDADDR: return ppufast.io.bg2.tiledataAddress >> 12;
            //$210C
        case SNES_REG_BG3_TDADDR: return ppufast.io.bg3.tiledataAddress >> 12;
        case SNES_REG_BG4_TDADDR: return ppufast.io.bg4.tiledataAddress >> 12;
            //$2133 SETINI
        case SNES_REG_SETINI_MODE7_EXTBG: return ppufast.io.extbg;
        case SNES_REG_SETINI_HIRES: return ppufast.io.pseudoHires;
        case SNES_REG_SETINI_OVERSCAN: return ppufast.io.overscan;
        case SNES_REG_SETINI_OBJ_INTERLACE: return ppufast.io.obj.interlace;
        case SNES_REG_SETINI_SCREEN_INTERLACE: return ppufast.io.interlace;
            //$2130 CGWSEL
        case SNES_REG_CGWSEL_COLORMASK: return ppufast.io.col.window.aboveMask;
        case SNES_REG_CGWSEL_COLORSUBMASK: return ppufast.io.col.window.belowMask;
        case SNES_REG_CGWSEL_ADDSUBMODE: return ppufast.io.col.blendMode;
        case SNES_REG_CGWSEL_DIRECTCOLOR: return ppufast.io.col.directColor;
            //$2101 OBSEL
        case SNES_REG_OBSEL_NAMEBASE: return ppufast.io.obj.tiledataAddress >> 13; // TODO: figure out why these shifts are only in specific places
        case SNES_REG_OBSEL_NAMESEL: return ppufast.io.obj.nameselect;
        case SNES_REG_OBSEL_SIZE: return ppufast.io.obj.baseSize;
            //$2131 CGADDSUB
        //enum { BG1 = 0, BG2 = 1, BG3 = 2, BG4 = 3, OAM = 4, BACK = 5, COL = 5 };
        case SNES_REG_CGADDSUB_BG1: return ppufast.io.col.enable[PPUfast::Source::BG1];
        case SNES_REG_CGADDSUB_BG2: return ppufast.io.col.enable[PPUfast::Source::BG2];
        case SNES_REG_CGADDSUB_BG3: return ppufast.io.col.enable[PPUfast::Source::BG3];
        case SNES_REG_CGADDSUB_BG4: return ppufast.io.col.enable[PPUfast::Source::BG4];
        case SNES_REG_CGADDSUB_OBJ: return ppufast.io.col.enable[PPUfast::Source::OBJ2];
        case SNES_REG_CGADDSUB_BACKDROP: return ppufast.io.col.enable[PPUfast::Source::COL];
        case SNES_REG_CGADDSUB_HALF: return ppufast.io.col.halve;
        case SNES_REG_CGADDSUB_MODE: return ppufast.io.col.mathMode;
            //$212C TM
        case SNES_REG_TM_BG1: return ppufast.io.bg1.aboveEnable;
        case SNES_REG_TM_BG2: return ppufast.io.bg2.aboveEnable;
        case SNES_REG_TM_BG3: return ppufast.io.bg3.aboveEnable;
        case SNES_REG_TM_BG4: return ppufast.io.bg4.aboveEnable;
        case SNES_REG_TM_OBJ: return ppufast.io.obj.aboveEnable;
            //$212D TS
        case SNES_REG_TS_BG1: return ppufast.io.bg1.belowEnable;
        case SNES_REG_TS_BG2: return ppufast.io.bg2.belowEnable;
        case SNES_REG_TS_BG3: return ppufast.io.bg3.belowEnable;
        case SNES_REG_TS_BG4: return ppufast.io.bg4.belowEnable;
        case SNES_REG_TS_OBJ: return ppufast.io.obj.belowEnable;
            //Mode7 regs
        case SNES_REG_M7SEL_HFLIP: return ppufast.io.mode7.hflip;
        case SNES_REG_M7SEL_VFLIP: return ppufast.io.mode7.vflip;
        case SNES_REG_M7SEL_REPEAT: return ppufast.io.mode7.repeat;
        case SNES_REG_M7A: return ppufast.io.mode7.a;
        case SNES_REG_M7B: return ppufast.io.mode7.b;
        case SNES_REG_M7C: return ppufast.io.mode7.c;
        case SNES_REG_M7D: return ppufast.io.mode7.d;
        case SNES_REG_M7X: return ppufast.io.mode7.x;
        case SNES_REG_M7Y: return ppufast.io.mode7.y;
            //BG scroll regs
        case SNES_REG_BG1HOFS: return ppufast.io.bg1.hoffset;
        case SNES_REG_BG1VOFS: return ppufast.io.bg1.voffset;
        case SNES_REG_BG2HOFS: return ppufast.io.bg2.hoffset;
        case SNES_REG_BG2VOFS: return ppufast.io.bg2.voffset;
        case SNES_REG_BG3HOFS: return ppufast.io.bg3.hoffset;
        case SNES_REG_BG3VOFS: return ppufast.io.bg3.voffset;
        case SNES_REG_BG4HOFS: return ppufast.io.bg4.hoffset;
        case SNES_REG_BG4VOFS: return ppufast.io.bg4.voffset;
        case SNES_REG_M7HOFS: return ppufast.io.mode7.hoffset; // TODO figure out what that comment means .regs.m7_hofs & 0x1FFF; //rememebr to make these signed with <<19>>19
        case SNES_REG_M7VOFS: return ppufast.io.mode7.voffset; //rememebr to make these signed with <<19>>19
    }
    else; // no fast ppu
    // TODO: potentially provide register values even in this case? currently all those are private in ppu.hpp
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
  // if(rom_data) cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = cartridge;//SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : "";iface->cart.markup;
  // if(bsx_data) bsxflash.memory.copy(bsx_data, bsx_size);
  // string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  // cartridge.load(Cartridge::Mode::BsxSlotted, xmlrom);
  // system.power(false);
    return false;
}

bool snes_load_cartridge_bsx(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
) {
  // if(rom_data) cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
  // if(bsx_data) bsxflash.memory.copy(bsx_data, bsx_size);
  // string xmlbsx = (bsx_xml && *bsx_xml) ? string(bsx_xml) : SnesCartridge(bsx_data, bsx_size).markup;
  // cartridge.load(Cartridge::Mode::Bsx, xmlrom);
  // system.power(false);
    return false;
}

bool snes_load_cartridge_sufami_turbo(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *sta_xml, const uint8_t *sta_data, unsigned sta_size,
  const char *stb_xml, const uint8_t *stb_data, unsigned stb_size
) {
  // if(rom_data) cartridge.rom.copy(rom_data, rom_size);
  // iface->cart = SnesCartridge(rom_data, rom_size);
  // string xmlrom = (rom_xml && *rom_xml) ? string(rom_xml) : iface->cart.markup;
  // if(sta_data) sufamiturbo.slotA.rom.copy(sta_data, sta_size);
  // string xmlsta = (sta_xml && *sta_xml) ? string(sta_xml) : SnesCartridge(sta_data, sta_size).markup;
  // if(stb_data) sufamiturbo.slotB.rom.copy(stb_data, stb_size);
  // string xmlstb = (stb_xml && *stb_xml) ? string(stb_xml) : SnesCartridge(stb_data, stb_size).markup;
  // cartridge.load(Cartridge::Mode::SufamiTurbo, xmlrom);
  // system.power(false);
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
    cartridge.unload();
}

bool snes_get_region(void) {
    return Region::PAL();
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

void snes_set_layer_enabled(int layer, int priority, bool enable)
{
    // fprintf(stderr, "snes_set_layer_enabled was called with layer %d, priority %d and bool %d\n", layer, priority, enable);
    if (emulator->configuration("Hacks/PPU/Fast") == "true")
    switch (layer)
    {
        case 0: ppufast.io.bg1.priority_enabled[priority] = enable; break;
        case 1: ppufast.io.bg2.priority_enabled[priority] = enable; break;
        case 2: ppufast.io.bg3.priority_enabled[priority] = enable; break;
        case 3: ppufast.io.bg4.priority_enabled[priority] = enable; break;
        case 4: ppufast.io.obj.priority_enabled[priority] = enable; break;
    }
}

static uint8_t sharprtc_data[16];
static uint8_t epsonrtc_data[16];

void snes_write_memory_data(unsigned id, unsigned index, unsigned value)
{
    uint8_t* data = snes_get_memory_data(id);
    if (!data) return;

    data[index] = value;

    if (id == SNES_MEMORY_CARTRIDGE_RTC) {
        if (cartridge.has.SharpRTC) sharprtc.load(data);
        if (cartridge.has.EpsonRTC) epsonrtc.load(data);
    }
}

uint8_t* snes_get_memory_data(unsigned id)
{
    if(!emulator->loaded()) return 0;

    switch(id)
    {
        case SNES_MEMORY_CARTRIDGE_RAM:
            return cartridge.ram.data();
        case SNES_MEMORY_CARTRIDGE_RTC:
            if(cartridge.has.SharpRTC) {
                sharprtc.save(sharprtc_data);
                return sharprtc_data;
            }
            if(cartridge.has.EpsonRTC) {
                epsonrtc.save(epsonrtc_data);
                return epsonrtc_data;
            }
            break;
        case SNES_MEMORY_BSX_RAM:
            if(!cartridge.has.BSMemorySlot) break;
            return mcc.rom.data();
        case SNES_MEMORY_BSX_PRAM:
            if(!cartridge.has.BSMemorySlot) break;
            return mcc.psram.data();
        case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
            if(!cartridge.has.SufamiTurboSlotA) break;
            return sufamiturboA.ram.data();
        case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
            if(!cartridge.has.SufamiTurboSlotB) break;
            return sufamiturboB.ram.data();
        // case SNES_MEMORY_GAME_BOY_CARTRAM:
        //     if(!cartridge.has.GameBoySlot) break;
        //     return cartridge.ram;
        // case SNES_MEMORY_GAME_BOY_RTC:
        //     if(!cartridge.has.GameBoySlot) break;
        //     return GameBoy::cartridge.rtcdata;
        // case SNES_MEMORY_GAME_BOY_WRAM:
        //   if(!cartridge.has.GameBoySlot) break;
        //   return GameBoy::cpu.wram;
        // case SNES_MEMORY_GAME_BOY_HRAM:
        //   if(!cartridge.has.GameBoySlot) break;
        //   return GameBoy::cpu.hram;

        case SNES_MEMORY_WRAM:
            return cpu.wram;
        case SNES_MEMORY_APURAM:
            return dsp.apuram;
        case SNES_MEMORY_VRAM:
            return (uint8_t*) ppufast.vram;
        case SNES_MEMORY_OAM:
            return (uint8_t*) ppufast.objects;
        case SNES_MEMORY_CGRAM:
            return (uint8_t*) ppufast.cgram;

        case SNES_MEMORY_CARTRIDGE_ROM:
            return cartridge.rom.data();
    }

    return nullptr;
}

const char* snes_get_memory_id_name(unsigned id) {
    if(!emulator->loaded()) return nullptr;

    switch(id) {
        case SNES_MEMORY_CARTRIDGE_RAM:
            return "CARTRIDGE_RAM";
        case SNES_MEMORY_CARTRIDGE_RTC:
            if(cartridge.has.SharpRTC) return "RTC";
            if(cartridge.has.SPC7110) return "SPC7110_RTC";
            return nullptr;
        case SNES_MEMORY_BSX_RAM:
            if(!cartridge.has.BSMemorySlot) break;// mode() != Cartridge::Mode::Bsx) break;
            return "BSX_SRAM";
        case SNES_MEMORY_BSX_PRAM:
            if(!cartridge.has.BSMemorySlot) break;//() != Cartridge::Mode::Bsx) break;
            return "BSX_PSRAM";
        case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
            if(!cartridge.has.SufamiTurboSlotA) break;// mode() != Cartridge::Mode::SufamiTurbo) break;
            return "SUFAMI_SLOTARAM";
        case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
            if(!cartridge.has.SufamiTurboSlotB) break;//() != Cartridge::Mode::SufamiTurbo) break;
            return "SUFAMI_SLOTBRAM";
        case SNES_MEMORY_GAME_BOY_CARTRAM:
            if(!cartridge.has.GameBoySlot) break;//() != Cartridge::Mode::SuperGameBoy) break;
            return "SGB_CARTRAM";
        case SNES_MEMORY_GAME_BOY_WRAM:
            if(!cartridge.has.GameBoySlot) break;//() != Cartridge::Mode::SuperGameBoy) break;
            //see notes in SetupMemoryDomains in bizhawk
            return "SGB_WRAM";
        case SNES_MEMORY_GAME_BOY_HRAM:
            if(!cartridge.has.GameBoySlot) break;//() != Cartridge::Mode::SuperGameBoy) break;
            return "SGB_HRAM";
        case SNES_MEMORY_WRAM:
            return "WRAM";
        case SNES_MEMORY_APURAM:
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
    if(!emulator->loaded()) return 0;
    unsigned size = 0;

    switch(id)
    {
        case SNES_MEMORY_CARTRIDGE_RAM:
            size = cartridge.ram.size();
            break;
        case SNES_MEMORY_CARTRIDGE_RTC:
            if(cartridge.has.SharpRTC || cartridge.has.EpsonRTC)
                size = 16;
            break;
        case SNES_MEMORY_BSX_RAM:
            if(cartridge.has.BSMemorySlot)
                size = mcc.rom.size();
            break;
        case SNES_MEMORY_BSX_PRAM:
            if(cartridge.has.BSMemorySlot)
                size = mcc.psram.size();
            break;
        case SNES_MEMORY_SUFAMI_TURBO_A_RAM:
            if(cartridge.has.SufamiTurboSlotA)// .mode() != Cartridge::Mode::SufamiTurbo) break;
                size = sufamiturboA.ram.size();// sufamiturbo.slotA.ram.size();
            break;
        case SNES_MEMORY_SUFAMI_TURBO_B_RAM:
            if(cartridge.has.SufamiTurboSlotB)// mode() != Cartridge::Mode::SufamiTurbo) break;
                size = sufamiturboB.ram.size();// .slotB.ram.size();
            break;
        // case SNES_MEMORY_GAME_BOY_CARTRAM:
        //     if(cartridge.has.GameBoySlot)// mode() != Cartridge::Mode::SuperGameBoy) break;
        //         size = cartridge.ram.size();// GameBoy::cartridge.ramsize;
        //     break;
        case SNES_MEMORY_GAME_BOY_WRAM:
            if(cartridge.has.GameBoySlot)// mode() != Cartridge::Mode::SuperGameBoy) break;
                //see notes in SetupMemoryDomains in bizhawk
                size = 32768;
            break;
        case SNES_MEMORY_GAME_BOY_HRAM:
            if(cartridge.has.GameBoySlot)//.mode() != Cartridge::Mode::SuperGameBoy) break;
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
            size = 10 * 128;
            break;
        case SNES_MEMORY_CGRAM:
            size = 512;
            break;

        case SNES_MEMORY_CARTRIDGE_ROM:
            size =  cartridge.rom.size();
            break;
    }

    if(size == -1U) size = 0;
    return size;
}

uint8_t bus_read(unsigned addr) {
    return bus.read(addr);
}
void bus_write(unsigned addr, uint8_t val) {
    bus.write(addr, val);
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
