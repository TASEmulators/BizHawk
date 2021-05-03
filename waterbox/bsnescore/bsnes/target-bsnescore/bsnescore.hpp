#ifndef BSNESCORE_HPP
#define BSNESCORE_HPP

#include <stdint.h>
#include <stdlib.h>

#if defined(BSNESCORE_IMPORT)
#define BSNESCORE_IMPORTDECL __declspec(dllimport)
#elif defined(BSNESCORE_EXPORT)
#define BSNESCORE_IMPORTDECL __declspec(dllexport)
#else
#define BSNESCORE_IMPORTDECL
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define SAMPLE_RATE 32040

#define SNES_PORT_1  0
#define SNES_PORT_2  1

#define SNES_DEVICE_NONE          0
#define SNES_DEVICE_JOYPAD        1
#define SNES_DEVICE_MULTITAP      2
#define SNES_DEVICE_MOUSE         3
#define SNES_DEVICE_SUPER_SCOPE   4
#define SNES_DEVICE_JUSTIFIER     5
#define SNES_DEVICE_JUSTIFIERS    6
#define SNES_DEVICE_SERIAL_CABLE  7

#define SNES_DEVICE_ID_JOYPAD_B        0
#define SNES_DEVICE_ID_JOYPAD_Y        1
#define SNES_DEVICE_ID_JOYPAD_SELECT   2
#define SNES_DEVICE_ID_JOYPAD_START    3
#define SNES_DEVICE_ID_JOYPAD_UP       4
#define SNES_DEVICE_ID_JOYPAD_DOWN     5
#define SNES_DEVICE_ID_JOYPAD_LEFT     6
#define SNES_DEVICE_ID_JOYPAD_RIGHT    7
#define SNES_DEVICE_ID_JOYPAD_A        8
#define SNES_DEVICE_ID_JOYPAD_X        9
#define SNES_DEVICE_ID_JOYPAD_L       10
#define SNES_DEVICE_ID_JOYPAD_R       11

#define SNES_DEVICE_ID_MOUSE_X      0
#define SNES_DEVICE_ID_MOUSE_Y      1
#define SNES_DEVICE_ID_MOUSE_LEFT   2
#define SNES_DEVICE_ID_MOUSE_RIGHT  3

#define SNES_DEVICE_ID_SUPER_SCOPE_X        0
#define SNES_DEVICE_ID_SUPER_SCOPE_Y        1
#define SNES_DEVICE_ID_SUPER_SCOPE_TRIGGER  2
#define SNES_DEVICE_ID_SUPER_SCOPE_CURSOR   3
#define SNES_DEVICE_ID_SUPER_SCOPE_TURBO    4
#define SNES_DEVICE_ID_SUPER_SCOPE_PAUSE    5

#define SNES_DEVICE_ID_JUSTIFIER_X        0
#define SNES_DEVICE_ID_JUSTIFIER_Y        1
#define SNES_DEVICE_ID_JUSTIFIER_TRIGGER  2
#define SNES_DEVICE_ID_JUSTIFIER_START    3

#define SNES_REGION_NTSC  0
#define SNES_REGION_PAL   1

#define SNES_MEMORY_CARTRIDGE_RAM       0
#define SNES_MEMORY_CARTRIDGE_RTC       1
#define SNES_MEMORY_BSX_RAM             2
#define SNES_MEMORY_BSX_PRAM            3
#define SNES_MEMORY_SUFAMI_TURBO_A_RAM  4
#define SNES_MEMORY_SUFAMI_TURBO_B_RAM  5
#define SNES_MEMORY_GAME_BOY_CARTRAM    6
#define SNES_MEMORY_GAME_BOY_RTC        7
#define SNES_MEMORY_GAME_BOY_WRAM       8
#define SNES_MEMORY_GAME_BOY_HRAM       9

#define SNES_MEMORY_WRAM    100
#define SNES_MEMORY_APURAM  101
#define SNES_MEMORY_VRAM    102
#define SNES_MEMORY_OAM     103
#define SNES_MEMORY_CGRAM   104

#define SNES_MEMORY_CARTRIDGE_ROM 105

#define SNES_MEMORY_SYSBUS 200
#define SNES_MEMORY_LOGICAL_REGS 201

typedef void (*snes_trace_t)(uint32_t which, const char *msg);

const char* snes_library_id(void);

// provided in pwrap.c currently as it communicates with the frontend
void snes_video_refresh(const uint32_t *data, unsigned width, unsigned height);
void snes_audio_sample(uint16_t left, uint16_t right);
void snes_input_poll(void);
int16_t snes_input_state(unsigned port, unsigned device, unsigned index, unsigned id);
void snes_no_lag();
const char* snes_path_request(int slot, const char* hint);
void* snes_allocSharedMemory(const char* memtype, size_t amt);
void snes_freeSharedMemory(void* ptr);

void snes_init(int entropy);
void snes_term(void);
void snes_power(void);
void snes_reset(void);
void snes_run(void);

bool snes_load_cartridge_normal(
    const char *base_rom_path, const uint8_t *rom_data, unsigned rom_size
);

bool snes_load_cartridge_bsx_slotted(
    const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
    const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
);

bool snes_load_cartridge_bsx(
    const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
    const char *bsx_xml, const uint8_t *bsx_data, unsigned bsx_size
);

bool snes_load_cartridge_sufami_turbo(
    const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
    const char *sta_xml, const uint8_t *sta_data, unsigned sta_size,
    const char *stb_xml, const uint8_t *stb_data, unsigned stb_size
);

bool snes_load_cartridge_super_game_boy(
    const char *base_rom_path, const uint8_t *rom_data, unsigned rom_size,
    const uint8_t *sgb_rom_data, unsigned sgb_rom_size
);

void snes_unload_cartridge(void);

bool snes_get_region(void);
char snes_get_mapper(void);
uint8_t* snes_get_memory_data(unsigned id);
void snes_write_memory_data(unsigned id, unsigned index, unsigned value);
const char* snes_get_memory_id_name(unsigned id);
unsigned snes_get_memory_size(unsigned id);

//zeromus additions
bool snes_check_cartridge(const uint8_t *rom_data, unsigned rom_size);
void snes_set_layer_enable(int layer, int priority, bool enable);
typedef void (*snes_scanlineStart_t)(int);
void snes_set_scanlineStart(snes_scanlineStart_t);
void snes_set_backdropColor(int color);
//returns -1 if no messages, messagelength if there is one
int snes_poll_message();
//give us a buffer of messagelength and we'll dequeue a message into it. you better take care of the null pointer
void snes_dequeue_message(char* buffer);

void snes_set_trace_callback(uint32_t mask, void (*callback)(uint32_t mask, const char *));

// system bus implementation
uint8_t bus_read(unsigned addr);
void bus_write(unsigned addr, uint8_t val);


//$2105
#define SNES_REG_BG_MODE 0
#define SNES_REG_BG3_PRIORITY 1
#define SNES_REG_BG1_TILESIZE 2
#define SNES_REG_BG2_TILESIZE 3
#define SNES_REG_BG3_TILESIZE 4
#define SNES_REG_BG4_TILESIZE 5
//$2107
#define SNES_REG_BG1_SCADDR 10
#define SNES_REG_BG1_SCSIZE 11
//$2108
#define SNES_REG_BG2_SCADDR 12
#define SNES_REG_BG2_SCSIZE 13
//$2109
#define SNES_REG_BG3_SCADDR 14
#define SNES_REG_BG3_SCSIZE 15
//$210A
#define SNES_REG_BG4_SCADDR 16
#define SNES_REG_BG4_SCSIZE 17
//$210B
#define SNES_REG_BG1_TDADDR 20
#define SNES_REG_BG2_TDADDR 21
//$210C
#define SNES_REG_BG3_TDADDR 22
#define SNES_REG_BG4_TDADDR 23
//$2133 SETINI
#define SNES_REG_SETINI_MODE7_EXTBG 30
#define SNES_REG_SETINI_HIRES 31
#define SNES_REG_SETINI_OVERSCAN 32
#define SNES_REG_SETINI_OBJ_INTERLACE 33
#define SNES_REG_SETINI_SCREEN_INTERLACE 34
//$2130 CGWSEL
#define SNES_REG_CGWSEL_COLORMASK 40
#define SNES_REG_CGWSEL_COLORSUBMASK 41
#define SNES_REG_CGWSEL_ADDSUBMODE 42
#define SNES_REG_CGWSEL_DIRECTCOLOR 43
//$2101 OBSEL
#define SNES_REG_OBSEL_NAMEBASE 50
#define SNES_REG_OBSEL_NAMESEL 51
#define SNES_REG_OBSEL_SIZE 52
//$2131 CGADSUB
#define SNES_REG_CGADDSUB_MODE 60
#define SNES_REG_CGADDSUB_HALF 61
#define SNES_REG_CGADDSUB_BG4 62
#define SNES_REG_CGADDSUB_BG3 63
#define SNES_REG_CGADDSUB_BG2 64
#define SNES_REG_CGADDSUB_BG1 65
#define SNES_REG_CGADDSUB_OBJ 66
#define SNES_REG_CGADDSUB_BACKDROP 67
//$212C TM
#define SNES_REG_TM_BG1 70
#define SNES_REG_TM_BG2 71
#define SNES_REG_TM_BG3 72
#define SNES_REG_TM_BG4 73
#define SNES_REG_TM_OBJ 74
//$212D TM
#define SNES_REG_TS_BG1 80
#define SNES_REG_TS_BG2 81
#define SNES_REG_TS_BG3 82
#define SNES_REG_TS_BG4 83
#define SNES_REG_TS_OBJ 84
//Mode7 regs
#define SNES_REG_M7SEL_REPEAT 90
#define SNES_REG_M7SEL_HFLIP 91
#define SNES_REG_M7SEL_VFLIP 92
#define SNES_REG_M7A 93
#define SNES_REG_M7B 94
#define SNES_REG_M7C 95
#define SNES_REG_M7D 96
#define SNES_REG_M7X 97
#define SNES_REG_M7Y 98
//BG scroll regs
#define SNES_REG_BG1HOFS 100
#define SNES_REG_BG1VOFS 101
#define SNES_REG_BG2HOFS 102
#define SNES_REG_BG2VOFS 103
#define SNES_REG_BG3HOFS 104
#define SNES_REG_BG3VOFS 105
#define SNES_REG_BG4HOFS 106
#define SNES_REG_BG4VOFS 107
#define SNES_REG_M7HOFS 108
#define SNES_REG_M7VOFS 109


int snes_peek_logical_register(int reg);

#ifdef __cplusplus
}
#endif

#endif
