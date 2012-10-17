#ifndef LIBSNES_HPP
#define LIBSNES_HPP

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

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
#define SNES_MEMORY_GAME_BOY_RAM        6
#define SNES_MEMORY_GAME_BOY_RTC        7

#define SNES_MEMORY_WRAM    100
#define SNES_MEMORY_APURAM  101
#define SNES_MEMORY_VRAM    102
#define SNES_MEMORY_OAM     103
#define SNES_MEMORY_CGRAM   104

typedef void (*snes_video_refresh_t)(const uint32_t *data, unsigned width, unsigned height);
typedef void (*snes_audio_sample_t)(uint16_t left, uint16_t right);
typedef void (*snes_input_poll_t)(void);
typedef int16_t (*snes_input_state_t)(unsigned port, unsigned device, unsigned index, unsigned id);
typedef void (*snes_input_notify_t)(int index);

const char* snes_library_id(void);
unsigned snes_library_revision_major(void);
unsigned snes_library_revision_minor(void);

void snes_set_video_refresh(snes_video_refresh_t);
void snes_set_audio_sample(snes_audio_sample_t);
void snes_set_input_poll(snes_input_poll_t);
void snes_set_input_state(snes_input_state_t);
void snes_set_input_notify(snes_input_notify_t);

void snes_set_controller_port_device(bool port, unsigned device);
void snes_set_cartridge_basename(const char *basename);

void snes_init(void);
void snes_term(void);
void snes_power(void);
void snes_reset(void);
void snes_run(void);

unsigned snes_serialize_size(void);
bool snes_serialize(uint8_t *data, unsigned size);
bool snes_unserialize(const uint8_t *data, unsigned size);

void snes_cheat_reset(void);
void snes_cheat_set(unsigned index, bool enable, const char *code);

bool snes_load_cartridge_normal(
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size
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
  const char *rom_xml, const uint8_t *rom_data, unsigned rom_size,
  const char *dmg_xml, const uint8_t *dmg_data, unsigned dmg_size
);

void snes_unload_cartridge(void);

bool snes_get_region(void);
uint8_t* snes_get_memory_data(unsigned id);
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
typedef const char* (*snes_path_request_t)(int slot, const char* hint);
void snes_set_path_request(snes_path_request_t path_request);

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

int snes_peek_logical_register(int reg);

#ifdef __cplusplus
}
#endif

#endif
