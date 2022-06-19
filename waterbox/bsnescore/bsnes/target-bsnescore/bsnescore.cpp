#include "bsnescore.hpp"
#include "callbacks.h"
#include <sfc/sfc.hpp>
#include <emulibc.h>
#include <nall/hid.hpp>

using namespace nall;
using namespace SuperFamicom;

#include "program.cpp"


//zero 05-sep-2012
// currently unused; was only used in the graphics debugger as far as i can see
int snes_peek_logical_register(int reg)
{
    if (SuperFamicom::system.fastPPU())
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


//
// fresh dll interface functions
//

// callbacks, set initially by the frontend
SnesCallbacks snesCallbacks;

EXPORT void snes_set_callbacks(SnesCallbacks* callbacks)
{
    snesCallbacks = SnesCallbacks(*callbacks);
}

EXPORT void snes_init(SnesInitData* init_data)
{
    fprintf(stderr, "snes_init was called!\n");
    emulator = new SuperFamicom::Interface;
    program = new Program;

    string entropy_string;
    switch (init_data->entropy)
    {
        case 0: entropy_string = "None"; break;
        case 1: entropy_string = "Low"; break;
        case 2: entropy_string = "High"; break;
    }
    emulator->configure("Hacks/Entropy", entropy_string);

    emulator->connect(ID::Port::Controller1, init_data->left_port);
    emulator->connect(ID::Port::Controller2, init_data->right_port);

    emulator->configure("Hacks/Hotfixes", init_data->hotfixes);
    emulator->configure("Hacks/PPU/Fast", init_data->fast_ppu);
    emulator->configure("Hacks/DSP/Fast", init_data->fast_dsp);
    emulator->configure("Hacks/Coprocessor/DelayedSync", init_data->fast_coprocessors);

    emulator->configure("Video/BlurEmulation", false); // blurs the video when not using fast ppu. I don't like it so I disable it here :)
    // needed in order to get audio sync working. should probably figure out what exactly this does or how to change that properly
    Emulator::audio.setFrequency(SAMPLE_RATE);

    program->regionOverride = init_data->region_override;
}

EXPORT void snes_power(void)
{
    emulator->power();
}

// unused currently? should it be?
EXPORT void snes_term(void)
{
    emulator->unload();
}

EXPORT void snes_reset(void)
{
    emulator->reset();
}

// note: run with runahead doesn't work yet, i suspect it's due to the serialize thing breaking (cause of libco)
EXPORT void snes_run(void)
{
    emulator->run();
}

// not used, but would probably be nice
void snes_hd_scale(int scale)
{
    emulator->configure("Hacks/PPU/Mode7/Scale", scale);
}

EXPORT int snes_serialized_size()
{
    return emulator->serialize().size();
}

// waiting for libco update in order to be able to use this deterministically (no synchronize)
EXPORT void snes_serialize(uint8_t* data, int size)
{
    auto serializer = emulator->serialize();
    memcpy(data, serializer.data(), size);
}

EXPORT void snes_unserialize(const uint8_t* data, int size)
{
    serializer s(data, size);
    emulator->unserialize(s);
}

EXPORT void snes_load_cartridge_normal(
  const uint8_t* rom_data, int rom_size
) {
    program->superFamicom.raw_data.resize(rom_size);
    memcpy(program->superFamicom.raw_data.data(), rom_data, rom_size);

    program->load();
}

// TODO: merged_rom_sizes is bad, fix this
EXPORT void snes_load_cartridge_super_gameboy(
  const uint8_t* rom_data, const uint8_t* sgb_rom_data, int rom_size, int sgb_rom_size
) {
    program->superFamicom.raw_data.resize(rom_size);
    memcpy(program->superFamicom.raw_data.data(), rom_data, rom_size);

    program->gameBoy.program.resize(sgb_rom_size);
    memcpy(program->gameBoy.program.data(), sgb_rom_data, sgb_rom_size);

    program->load();
}
// Note that bsmemory and sufamiturbo (a and b) are never loaded
// I have no idea what that is but it probably should be supported frontend


EXPORT void snes_set_layer_enables(LayerEnables* layerEnables)
{
    if (SuperFamicom::system.fastPPU()) {
        ppufast.io.bg1.priority_enabled[0] = layerEnables->BG1_Prio0;
        ppufast.io.bg1.priority_enabled[1] = layerEnables->BG1_Prio1;
        ppufast.io.bg2.priority_enabled[0] = layerEnables->BG2_Prio0;
        ppufast.io.bg2.priority_enabled[1] = layerEnables->BG2_Prio1;
        ppufast.io.bg3.priority_enabled[0] = layerEnables->BG3_Prio0;
        ppufast.io.bg3.priority_enabled[1] = layerEnables->BG3_Prio1;
        ppufast.io.bg4.priority_enabled[0] = layerEnables->BG4_Prio0;
        ppufast.io.bg4.priority_enabled[1] = layerEnables->BG4_Prio1;
        ppufast.io.obj.priority_enabled[0] = layerEnables->Obj_Prio0;
        ppufast.io.obj.priority_enabled[1] = layerEnables->Obj_Prio1;
        ppufast.io.obj.priority_enabled[2] = layerEnables->Obj_Prio2;
        ppufast.io.obj.priority_enabled[3] = layerEnables->Obj_Prio3;
    }
}

EXPORT void snes_set_audio_enabled(bool enable)
{
    SuperFamicom::system.renderAudio = enable;
}

EXPORT void snes_set_video_enabled(bool enable)
{
    SuperFamicom::system.renderVideo = enable;
}

EXPORT void snes_set_trace_enabled(bool enabled)
{
    platform->traceEnabled = enabled;
}

EXPORT void snes_set_hooks_enabled(bool read_hook_enabled, bool write_hook_enabled, bool execute_hook_enabled)
{
    platform->readHookEnabled = read_hook_enabled;
    platform->writeHookEnabled = write_hook_enabled;
    platform->executeHookEnabled = execute_hook_enabled;
}


uint8_t* snes_get_effective_saveram(int* ram_size) {
    if (cartridge.has.SA1) {
        *ram_size = sa1.bwram.size();
        return sa1.bwram.data();
    } else if (cartridge.has.SuperFX) {
        *ram_size = superfx.ram.size();
        return superfx.ram.data();
    } else if (cartridge.has.HitachiDSP) {
        *ram_size = hitachidsp.ram.size();
        return hitachidsp.ram.data();
    } else if (cartridge.has.SPC7110) {
        *ram_size = spc7110.ram.size();
        return spc7110.ram.data();
    } else if (cartridge.has.OBC1) {
        *ram_size = obc1.ram.size();
        return obc1.ram.data();
    }

    // note: if sufamiturbo is ever implemented frontend, this will need some additional consideration
    // because sufamiturbo can have up to 2 cartridges (and respective save rams)
    *ram_size = cartridge.ram.size();
    return cartridge.ram.data();
}

EXPORT System::Region snes_get_region(void) {
    return SuperFamicom::system.region();
}

EXPORT char snes_get_mapper(void) {
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

EXPORT void* snes_get_memory_region(int id, int* size, int* word_size)
{
    if(!emulator->loaded()) return nullptr;
    bool fast_ppu = SuperFamicom::system.fastPPU();

    switch(id)
    {
        // this cartridge ram is a generalized memory region that can be anything that is considered
        // cartridge or save ram for any coprocessor like SA-1, or just the basic cartridge ram
        case SNES_MEMORY::CARTRIDGE_RAM:
            *word_size = 1;
            return snes_get_effective_saveram(size);
        case SNES_MEMORY::CARTRIDGE_ROM:
            *size = program->superFamicom.program.size();
            *word_size = 1;
            return program->superFamicom.program.data();
        case SNES_MEMORY::SGB_ROM:
            *size = program->gameBoy.program.size();
            *word_size = 1;
            return program->gameBoy.program.data();

        // unused
        case SNES_MEMORY::BSX_RAM:
            if (!cartridge.has.BSMemorySlot) break;
            *size = mcc.rom.size();
            *word_size = 1;
            return mcc.rom.data();
        case SNES_MEMORY::BSX_PRAM:
            if (!cartridge.has.BSMemorySlot) break;
            *size = mcc.psram.size();
            *word_size = 1;
            return mcc.psram.data();
        case SNES_MEMORY::SUFAMI_TURBO_A_RAM:
            if (!cartridge.has.SufamiTurboSlotA) break;
            *size = sufamiturboA.ram.size();
            *word_size = 1;
            return sufamiturboA.ram.data();
        case SNES_MEMORY::SUFAMI_TURBO_B_RAM:
            if (!cartridge.has.SufamiTurboSlotB) break;
            *size = sufamiturboB.ram.size();
            *word_size = 1;
            return sufamiturboB.ram.data();

        case SNES_MEMORY::SA1_IRAM:
            if (!cartridge.has.SA1) break;
            *size = sa1.iram.size();
            *word_size = 1;
            return sa1.iram.data();
        case SNES_MEMORY::SA1_BWRAM:
            // effectively the cartridge ram, listed here to allow direct BWRAM access
            // instead of relying on the CARTRIDGE_RAM domain
            if (!cartridge.has.SA1) break;
            *size = sa1.bwram.size();
            *word_size = 1;
            return sa1.bwram.data();

        case SNES_MEMORY::WRAM:
            *size = sizeof(cpu.wram);
            *word_size = 1;
            return cpu.wram;
        case SNES_MEMORY::APURAM:
            *size = sizeof(dsp.apuram);
            *word_size = 1;
            return dsp.apuram;
        case SNES_MEMORY::VRAM:
            if (!fast_ppu) break;
            *size = sizeof(ppufast.vram);
            *word_size = sizeof(*ppufast.vram);
            return ppufast.vram;
        // case SNES_MEMORY::OAM: // probably weird since bsnes uses "object"s instead of bytes for oam rn
            // return (uint8_t*) ppufast.objects;
        case SNES_MEMORY::CGRAM:
            if (!fast_ppu) break;
            *size = sizeof(ppufast.cgram);
            *word_size = sizeof(*ppufast.cgram);
            return ppufast.cgram;
    }

    return nullptr;
}

EXPORT uint8_t snes_bus_read(unsigned addr)
{
    return bus.peek(addr);
}

EXPORT void snes_bus_write(unsigned addr, uint8_t value)
{
    bus.write(addr, value);
}

EXPORT void snes_get_cpu_registers(SnesRegisters* registers)
{
    registers->pc = SuperFamicom::cpu.r.pc.d;
    registers->a = SuperFamicom::cpu.r.a.w;
    registers->x = SuperFamicom::cpu.r.x.w;
    registers->y = SuperFamicom::cpu.r.y.w;
    registers->z = SuperFamicom::cpu.r.z.w;
    registers->s = SuperFamicom::cpu.r.s.w;
    registers->d = SuperFamicom::cpu.r.d.w;

    registers->b = SuperFamicom::cpu.r.b;
    registers->p = SuperFamicom::cpu.r.p;
    registers->mdr = SuperFamicom::cpu.r.mdr;
    registers->e = SuperFamicom::cpu.r.e;

    registers->v = SuperFamicom::cpu.vcounter();
    registers->h = SuperFamicom::cpu.hdot();
}

EXPORT void snes_set_cpu_register(char* _register, uint32_t value)
{
    string register = _register;
    if (register == "PC") SuperFamicom::cpu.r.pc = value;
    if (register == "A") SuperFamicom::cpu.r.a = value;
    if (register == "X") SuperFamicom::cpu.r.x = value;
    if (register == "Y") SuperFamicom::cpu.r.y = value;
    if (register == "Z") SuperFamicom::cpu.r.z = value;
    if (register == "S") SuperFamicom::cpu.r.s = value;
    if (register == "D") SuperFamicom::cpu.r.d = value;
    if (register == "B") SuperFamicom::cpu.r.b = value;
    if (register == "P") SuperFamicom::cpu.r.p = value;
    if (register == "E") SuperFamicom::cpu.r.e = value;
    if (register == "MDR") SuperFamicom::cpu.r.mdr = value;

    if (register == "FLAG C") SuperFamicom::cpu.r.p.c = value;
    if (register == "FLAG Z") SuperFamicom::cpu.r.p.z = value;
    if (register == "FLAG I") SuperFamicom::cpu.r.p.i = value;
    if (register == "FLAG D") SuperFamicom::cpu.r.p.d = value;
    if (register == "FLAG X") SuperFamicom::cpu.r.p.x = value;
    if (register == "FLAG M") SuperFamicom::cpu.r.p.m = value;
    if (register == "FLAG V") SuperFamicom::cpu.r.p.v = value;
    if (register == "FLAG N") SuperFamicom::cpu.r.p.n = value;
}

EXPORT bool snes_cpu_step()
{
    scheduler.StepOnce = true;
    emulator->run();
    scheduler.StepOnce = false;
    return scheduler.event == Scheduler::Event::Frame;
}

// should be called on savestate load, to get msu files loaded and in the correct state
EXPORT void snes_msu_sync()
{
    if (cartridge.has.MSU1) {
        msu1.dataOpen();
        msu1.audioOpen();
    }
}
