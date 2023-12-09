#ifndef BSNESCORE_HPP
#define BSNESCORE_HPP

#include <stdint.h>
#include <stdlib.h>

#define EXPORT ECL_EXPORT

enum SNES_MEMORY {
    CARTRIDGE_RAM,
    CARTRIDGE_ROM,

    BSMEMORY_ROM,
    BSMEMORY_PSRAM,
    // sufamiturbo unused cause unsupported by frontend
    SUFAMI_TURBO_A_RAM,
    SUFAMI_TURBO_B_RAM,
    SA1_IRAM,
    SA1_BWRAM,

    WRAM,
    APURAM,
    VRAM,
    OAM,
    CGRAM
};


struct SnesInitData {
    int entropy;
    unsigned left_port;
    unsigned right_port;
    bool hotfixes;
    bool fast_ppu;
    bool fast_dsp;
    bool fast_coprocessors;
    int region_override;
};

struct LayerEnables
{
    bool BG1_Prio0, BG1_Prio1;
    bool BG2_Prio0, BG2_Prio1;
    bool BG3_Prio0, BG3_Prio1;
    bool BG4_Prio0, BG4_Prio1;
    bool Obj_Prio0, Obj_Prio1, Obj_Prio2, Obj_Prio3;
};

struct SnesRegisters
{
    uint32_t pc;
    uint16_t a, x, y, z, s, d;
    uint8_t b, p, mdr;
    bool e;
    uint16_t v, h;
};


enum SNES_REGISTER {
    //$2105
    BG_MODE,
    BG3_PRIORITY,
    BG1_TILESIZE,
    BG2_TILESIZE,
    BG3_TILESIZE,
    BG4_TILESIZE,
    //$2107
    BG1_SCADDR,
    BG1_SCSIZE,
    //$2108
    BG2_SCADDR,
    BG2_SCSIZE,
    //$2109,
    BG3_SCADDR,
    BG3_SCSIZE,
    //$210A
    BG4_SCADDR,
    BG4_SCSIZE,
    //$210B
    BG1_TDADDR,
    BG2_TDADDR,
    //$210C
    BG3_TDADDR,
    BG4_TDADDR,
    //$2133 SETINI
    SETINI_MODE7_EXTBG,
    SETINI_HIRES,
    SETINI_OVERSCAN,
    SETINI_OBJ_INTERLACE,
    SETINI_SCREEN_INTERLACE,
    //$2130 CGWSEL
    CGWSEL_COLORMASK,
    CGWSEL_COLORSUBMASK,
    CGWSEL_ADDSUBMODE,
    CGWSEL_DIRECTCOLOR,
    //$2101 OBSEL
    OBSEL_NAMEBASE,
    OBSEL_NAMESEL,
    OBSEL_SIZE,
    //$2131 CGADSUB
    CGADDSUB_MODE,
    CGADDSUB_HALF,
    CGADDSUB_BG4,
    CGADDSUB_BG3,
    CGADDSUB_BG2,
    CGADDSUB_BG1,
    CGADDSUB_OBJ,
    CGADDSUB_BACKDROP,
    //$212C TM
    TM_BG1,
    TM_BG2,
    TM_BG3,
    TM_BG4,
    TM_OBJ,
    //$212D TM
    TS_BG1,
    TS_BG2,
    TS_BG3,
    TS_BG4,
    TS_OBJ,
    //Mode7 regs
    M7SEL_REPEAT,
    M7SEL_HFLIP,
    M7SEL_VFLIP,
    M7A,
    M7B,
    M7C,
    M7D,
    M7X,
    M7Y,
    //BG scroll regs
    BG1HOFS,
    BG1VOFS,
    BG2HOFS,
    BG2VOFS,
    BG3HOFS,
    BG3VOFS,
    BG4HOFS,
    BG4VOFS,
    M7HOFS,
    M7VOFS
};

#endif
