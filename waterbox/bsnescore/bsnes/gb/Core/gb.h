#ifndef GB_h
#define GB_h
#define typeof __typeof__
#include <stdbool.h>
#include <stdint.h>
#include <time.h>

#include "gb_struct_def.h"
#include "save_state.h"

#include "apu.h"
#include "camera.h"
#include "debugger.h"
#include "display.h"
#include "joypad.h"
#include "mbc.h"
#include "memory.h"
#include "printer.h"
#include "timing.h"
#include "rewind.h"
#include "sm83_cpu.h"
#include "symbol_hash.h"
#include "sgb.h"
#include "cheats.h"
#include "rumble.h"
#include "workboy.h"

#define GB_STRUCT_VERSION 13

#define GB_MODEL_FAMILY_MASK 0xF00
#define GB_MODEL_DMG_FAMILY 0x000
#define GB_MODEL_MGB_FAMILY 0x100
#define GB_MODEL_CGB_FAMILY 0x200
#define GB_MODEL_PAL_BIT 0x1000
#define GB_MODEL_NO_SFC_BIT 0x2000

#ifdef GB_INTERNAL
#if __clang__
#define UNROLL _Pragma("unroll")
#elif __GNUC__ >= 8
#define UNROLL _Pragma("GCC unroll 8")
#else
#define UNROLL
#endif

#endif

#if __BYTE_ORDER__ == __ORDER_BIG_ENDIAN__
#define GB_BIG_ENDIAN
#elif __BYTE_ORDER__ == __ORDER_LITTLE_ENDIAN__
#define GB_LITTLE_ENDIAN
#else
#error Unable to detect endianess
#endif

#if __GNUC__ < 4 || (__GNUC__ == 4 && __GNUC_MINOR__ < 8)
#define __builtin_bswap16(x) ({ typeof(x) _x = (x); _x >> 8 | _x << 8; })
#endif

typedef struct {
    struct {
        uint8_t r, g, b;
    } colors[5];
} GB_palette_t;

extern const GB_palette_t GB_PALETTE_GREY;
extern const GB_palette_t GB_PALETTE_DMG;
extern const GB_palette_t GB_PALETTE_MGB;
extern const GB_palette_t GB_PALETTE_GBL;

typedef union {
    struct {
        uint8_t seconds;
        uint8_t minutes;
        uint8_t hours;
        uint8_t days;
        uint8_t high;
    };
    uint8_t data[5];
} GB_rtc_time_t;

typedef enum {
    // GB_MODEL_DMG_0 = 0x000,
    // GB_MODEL_DMG_A = 0x001,
    GB_MODEL_DMG_B = 0x002,
    // GB_MODEL_DMG_C = 0x003,
    GB_MODEL_SGB = 0x004,
    GB_MODEL_SGB_NTSC = GB_MODEL_SGB,
    GB_MODEL_SGB_PAL = GB_MODEL_SGB | GB_MODEL_PAL_BIT,
    GB_MODEL_SGB_NTSC_NO_SFC = GB_MODEL_SGB | GB_MODEL_NO_SFC_BIT,
    GB_MODEL_SGB_NO_SFC = GB_MODEL_SGB_NTSC_NO_SFC,
    GB_MODEL_SGB_PAL_NO_SFC = GB_MODEL_SGB | GB_MODEL_NO_SFC_BIT | GB_MODEL_PAL_BIT,
    // GB_MODEL_MGB = 0x100,
    GB_MODEL_SGB2 = 0x101,
    GB_MODEL_SGB2_NO_SFC = GB_MODEL_SGB2 | GB_MODEL_NO_SFC_BIT,
    // GB_MODEL_CGB_0 = 0x200,
    // GB_MODEL_CGB_A = 0x201,
    // GB_MODEL_CGB_B = 0x202,
    GB_MODEL_CGB_C = 0x203,
    // GB_MODEL_CGB_D = 0x204,
    GB_MODEL_CGB_E = 0x205,
    GB_MODEL_AGB = 0x206,
} GB_model_t;

enum {
    GB_REGISTER_AF,
    GB_REGISTER_BC,
    GB_REGISTER_DE,
    GB_REGISTER_HL,
    GB_REGISTER_SP,
    GB_REGISTERS_16_BIT /* Count */
};

/* Todo: Actually use these! */
enum {
    GB_CARRY_FLAG = 16,
    GB_HALF_CARRY_FLAG = 32,
    GB_SUBTRACT_FLAG = 64,
    GB_ZERO_FLAG = 128,
};

typedef enum {
    GB_BORDER_SGB,
    GB_BORDER_NEVER,
    GB_BORDER_ALWAYS,
} GB_border_mode_t;

#define GB_MAX_IR_QUEUE 256

enum {
    /* Joypad and Serial */
    GB_IO_JOYP       = 0x00, // Joypad (R/W)
    GB_IO_SB         = 0x01, // Serial transfer data (R/W)
    GB_IO_SC         = 0x02, // Serial Transfer Control (R/W)

    /* Missing */

    /* Timers */
    GB_IO_DIV        = 0x04, // Divider Register (R/W)
    GB_IO_TIMA       = 0x05, // Timer counter (R/W)
    GB_IO_TMA        = 0x06, // Timer Modulo (R/W)
    GB_IO_TAC        = 0x07, // Timer Control (R/W)

    /* Missing */

    GB_IO_IF         = 0x0f, // Interrupt Flag (R/W)

    /* Sound */
    GB_IO_NR10       = 0x10, // Channel 1 Sweep register (R/W)
    GB_IO_NR11       = 0x11, // Channel 1 Sound length/Wave pattern duty (R/W)
    GB_IO_NR12       = 0x12, // Channel 1 Volume Envelope (R/W)
    GB_IO_NR13       = 0x13, // Channel 1 Frequency lo (Write Only)
    GB_IO_NR14       = 0x14, // Channel 1 Frequency hi (R/W)
    /* NR20 does not exist */
    GB_IO_NR21       = 0x16, // Channel 2 Sound Length/Wave Pattern Duty (R/W)
    GB_IO_NR22       = 0x17, // Channel 2 Volume Envelope (R/W)
    GB_IO_NR23       = 0x18, // Channel 2 Frequency lo data (W)
    GB_IO_NR24       = 0x19, // Channel 2 Frequency hi data (R/W)
    GB_IO_NR30       = 0x1a, // Channel 3 Sound on/off (R/W)
    GB_IO_NR31       = 0x1b, // Channel 3 Sound Length
    GB_IO_NR32       = 0x1c, // Channel 3 Select output level (R/W)
    GB_IO_NR33       = 0x1d, // Channel 3 Frequency's lower data (W)
    GB_IO_NR34       = 0x1e, // Channel 3 Frequency's higher data (R/W)
    /* NR40 does not exist */
    GB_IO_NR41       = 0x20, // Channel 4 Sound Length (R/W)
    GB_IO_NR42       = 0x21, // Channel 4 Volume Envelope (R/W)
    GB_IO_NR43       = 0x22, // Channel 4 Polynomial Counter (R/W)
    GB_IO_NR44       = 0x23, // Channel 4 Counter/consecutive, Inital (R/W)
    GB_IO_NR50       = 0x24, // Channel control / ON-OFF / Volume (R/W)
    GB_IO_NR51       = 0x25, // Selection of Sound output terminal (R/W)
    GB_IO_NR52       = 0x26, // Sound on/off

    /* Missing */

    GB_IO_WAV_START  = 0x30, // Wave pattern start
    GB_IO_WAV_END    = 0x3f, // Wave pattern end

    /* Graphics */
    GB_IO_LCDC       = 0x40, // LCD Control (R/W)
    GB_IO_STAT       = 0x41, // LCDC Status (R/W)
    GB_IO_SCY        = 0x42, // Scroll Y (R/W)
    GB_IO_SCX        = 0x43, // Scroll X (R/W)
    GB_IO_LY         = 0x44, // LCDC Y-Coordinate (R)
    GB_IO_LYC        = 0x45, // LY Compare (R/W)
    GB_IO_DMA        = 0x46, // DMA Transfer and Start Address (W)
    GB_IO_BGP        = 0x47, // BG Palette Data (R/W) - Non CGB Mode Only
    GB_IO_OBP0       = 0x48, // Object Palette 0 Data (R/W) - Non CGB Mode Only
    GB_IO_OBP1       = 0x49, // Object Palette 1 Data (R/W) - Non CGB Mode Only
    GB_IO_WY         = 0x4a, // Window Y Position (R/W)
    GB_IO_WX         = 0x4b, // Window X Position minus 7 (R/W)
    // Has some undocumented compatibility flags written at boot.
    // Unfortunately it is not readable or writable after boot has finished, so research of this
    // register is quite limited. The value written to this register, however, can be controlled
    // in some cases.
    GB_IO_KEY0 = 0x4c,

    /* General CGB features */
    GB_IO_KEY1       = 0x4d, // CGB Mode Only - Prepare Speed Switch

    /* Missing */

    GB_IO_VBK        = 0x4f, // CGB Mode Only - VRAM Bank
    GB_IO_BANK       = 0x50, // Write to disable the BIOS mapping

    /* CGB DMA */
    GB_IO_HDMA1      = 0x51, // CGB Mode Only - New DMA Source, High
    GB_IO_HDMA2      = 0x52, // CGB Mode Only - New DMA Source, Low
    GB_IO_HDMA3      = 0x53, // CGB Mode Only - New DMA Destination, High
    GB_IO_HDMA4      = 0x54, // CGB Mode Only - New DMA Destination, Low
    GB_IO_HDMA5      = 0x55, // CGB Mode Only - New DMA Length/Mode/Start

    /* IR */
    GB_IO_RP         = 0x56, // CGB Mode Only - Infrared Communications Port

    /* Missing */

    /* CGB Paletts */
    GB_IO_BGPI       = 0x68, // CGB Mode Only - Background Palette Index
    GB_IO_BGPD       = 0x69, // CGB Mode Only - Background Palette Data
    GB_IO_OBPI       = 0x6a, // CGB Mode Only - Sprite Palette Index
    GB_IO_OBPD       = 0x6b, // CGB Mode Only - Sprite Palette Data
    GB_IO_OPRI       = 0x6c, // Affects object priority (X based or index based)

    /* Missing */

    GB_IO_SVBK       = 0x70, // CGB Mode Only - WRAM Bank
    GB_IO_UNKNOWN2   = 0x72, // (00h) - Bit 0-7 (Read/Write)
    GB_IO_UNKNOWN3   = 0x73, // (00h) - Bit 0-7 (Read/Write)
    GB_IO_UNKNOWN4   = 0x74, // (00h) - Bit 0-7 (Read/Write) - CGB Mode Only
    GB_IO_UNKNOWN5   = 0x75, // (8Fh) - Bit 4-6 (Read/Write)
    GB_IO_PCM_12     = 0x76, // Channels 1 and 2 amplitudes
    GB_IO_PCM_34     = 0x77, // Channels 3 and 4 amplitudes
    GB_IO_UNKNOWN8   = 0x7F, // Unknown, write only
};

typedef enum {
    GB_LOG_BOLD = 1,
    GB_LOG_DASHED_UNDERLINE = 2,
    GB_LOG_UNDERLINE = 4,
    GB_LOG_UNDERLINE_MASK =  GB_LOG_DASHED_UNDERLINE | GB_LOG_UNDERLINE
} GB_log_attributes;

typedef enum {
    GB_BOOT_ROM_DMG0,
    GB_BOOT_ROM_DMG,
    GB_BOOT_ROM_MGB,
    GB_BOOT_ROM_SGB,
    GB_BOOT_ROM_SGB2,
    GB_BOOT_ROM_CGB0,
    GB_BOOT_ROM_CGB,
    GB_BOOT_ROM_AGB,
} GB_boot_rom_t;

#ifdef GB_INTERNAL
#define LCDC_PERIOD 70224
#define CPU_FREQUENCY 0x400000
#define SGB_NTSC_FREQUENCY (21477272 / 5)
#define SGB_PAL_FREQUENCY (21281370 / 5)
#define DIV_CYCLES (0x100)
#define INTERNAL_DIV_CYCLES (0x40000)

#if !defined(MIN)
#define MIN(A, B)    ({ __typeof__(A) __a = (A); __typeof__(B) __b = (B); __a < __b ? __a : __b; })
#endif

#if !defined(MAX)
#define MAX(A, B)    ({ __typeof__(A) __a = (A); __typeof__(B) __b = (B); __a < __b ? __b : __a; })
#endif
#endif

typedef void (*GB_vblank_callback_t)(GB_gameboy_t *gb);
typedef void (*GB_log_callback_t)(GB_gameboy_t *gb, const char *string, GB_log_attributes attributes);
typedef char *(*GB_input_callback_t)(GB_gameboy_t *gb);
typedef uint32_t (*GB_rgb_encode_callback_t)(GB_gameboy_t *gb, uint8_t r, uint8_t g, uint8_t b);
typedef void (*GB_infrared_callback_t)(GB_gameboy_t *gb, bool on, uint64_t cycles_since_last_update);
typedef void (*GB_rumble_callback_t)(GB_gameboy_t *gb, double rumble_amplitude);
typedef void (*GB_serial_transfer_bit_start_callback_t)(GB_gameboy_t *gb, bool bit_to_send);
typedef bool (*GB_serial_transfer_bit_end_callback_t)(GB_gameboy_t *gb);
typedef void (*GB_update_input_hint_callback_t)(GB_gameboy_t *gb);
typedef void (*GB_joyp_write_callback_t)(GB_gameboy_t *gb, uint8_t value);
typedef void (*GB_icd_pixel_callback_t)(GB_gameboy_t *gb, uint8_t row);
typedef void (*GB_icd_hreset_callback_t)(GB_gameboy_t *gb);
typedef void (*GB_icd_vreset_callback_t)(GB_gameboy_t *gb);
typedef void (*GB_boot_rom_load_callback_t)(GB_gameboy_t *gb, GB_boot_rom_t type);

typedef struct {
    bool state;
    uint64_t delay;
} GB_ir_queue_item_t;

struct GB_breakpoint_s;
struct GB_watchpoint_s;

typedef struct {
    uint8_t pixel; // Color, 0-3
    uint8_t palette; // Palette, 0 - 7 (CGB); 0-1 in DMG (or just 0 for BG)
    uint8_t priority; // Sprite priority – 0 in DMG, OAM index in CGB
    bool bg_priority; // For sprite FIFO – the BG priority bit. For the BG FIFO – the CGB attributes priority bit
} GB_fifo_item_t;

#define GB_FIFO_LENGTH 16
typedef struct {
    GB_fifo_item_t fifo[GB_FIFO_LENGTH];
    uint8_t read_end;
    uint8_t write_end;
} GB_fifo_t;

/* When state saving, each section is dumped independently of other sections.
   This allows adding data to the end of the section without worrying about future compatibility.
   Some other changes might be "safe" as well.
   This struct is not packed, but dumped sections exclusively use types that have the same alignment in both 32 and 64
   bit platforms. */

#ifdef GB_INTERNAL
struct GB_gameboy_s {
#else
struct GB_gameboy_internal_s {
#endif
    GB_SECTION(header,
        /* The magic makes sure a state file is:
            - Indeed a SameBoy state file.
            - Has the same endianess has the current platform. */
        volatile uint32_t magic;
        /* The version field makes sure we don't load save state files with a completely different structure.
           This happens when struct fields are removed/resized in an backward incompatible manner. */
        uint32_t version;
    );

    GB_SECTION(core_state,
        /* Registers */
        uint16_t pc;
           union {
               uint16_t registers[GB_REGISTERS_16_BIT];
               struct {
                   uint16_t af,
                            bc,
                            de,
                            hl,
                            sp;
               };
               struct {
#ifdef GB_BIG_ENDIAN
                   uint8_t a, f,
                           b, c,
                           d, e,
                           h, l;
#else
                   uint8_t f, a,
                           c, b,
                           e, d,
                           l, h;
#endif
               };
               
           };
        uint8_t ime;
        uint8_t interrupt_enable;
        uint8_t cgb_ram_bank;

        /* CPU and General Hardware Flags*/
        GB_model_t model;
        bool cgb_mode;
        bool cgb_double_speed;
        bool halted;
        bool stopped;
        bool boot_rom_finished;
        bool ime_toggle; /* ei has delayed a effect.*/
        bool halt_bug;
        bool just_halted;

        /* Misc state */
        bool infrared_input;
        GB_printer_t printer;
        uint8_t extra_oam[0xff00 - 0xfea0];
        uint32_t ram_size; // Different between CGB and DMG
        GB_workboy_t workboy;
    );

    /* DMA and HDMA */
    GB_SECTION(dma,
        bool hdma_on;
        bool hdma_on_hblank;
        uint8_t hdma_steps_left;
        int16_t hdma_cycles; // in 8MHz units
        uint16_t hdma_current_src, hdma_current_dest;

        uint8_t dma_steps_left;
        uint8_t dma_current_dest;
        uint16_t dma_current_src;
        int16_t dma_cycles;
        bool is_dma_restarting;
        uint8_t last_opcode_read; /* Required to emulte HDMA reads from Exxx */
        bool hdma_starting;
    );
    
    /* MBC */
    GB_SECTION(mbc,
        uint16_t mbc_rom_bank;
        uint8_t mbc_ram_bank;
        uint32_t mbc_ram_size;
        bool mbc_ram_enable;
        union {
            struct {
                uint8_t bank_low:5;
                uint8_t bank_high:2;
                uint8_t mode:1;
            } mbc1;

            struct {
                uint8_t rom_bank:4;
            } mbc2;

            struct {
                uint8_t rom_bank:8;
                uint8_t ram_bank:3;
            } mbc3;

            struct {
                uint8_t rom_bank_low;
                uint8_t rom_bank_high:1;
                uint8_t ram_bank:4;
            } mbc5;
            
            struct {
                uint8_t bank_low:6;
                uint8_t bank_high:3;
                bool mode:1;
                bool ir_mode:1;
            } huc1;

            struct {
                uint8_t rom_bank:7;
                uint8_t padding:1;
                uint8_t ram_bank:4;
            } huc3;
        };
        uint16_t mbc_rom0_bank; /* For some MBC1 wirings. */
        bool camera_registers_mapped;
        uint8_t camera_registers[0x36];
        bool rumble_state;
        bool cart_ir;
        
        // TODO: move to huc3/mbc3 struct when breaking save compat
        uint8_t huc3_mode;
        uint8_t huc3_access_index;
        uint16_t huc3_minutes, huc3_days;
        uint16_t huc3_alarm_minutes, huc3_alarm_days;
        bool huc3_alarm_enabled;
        uint8_t huc3_read;
        uint8_t huc3_access_flags;
        bool mbc3_rtc_mapped;
    );


    /* HRAM and HW Registers */
    GB_SECTION(hram,
        uint8_t hram[0xFFFF - 0xFF80];
        uint8_t io_registers[0x80];
    );

    /* Timing */
    GB_SECTION(timing,
        GB_UNIT(display);
        GB_UNIT(div);
        uint16_t div_counter;
        uint8_t tima_reload_state; /* After TIMA overflows, it becomes 0 for 4 cycles before actually reloading. */
        uint16_t serial_cycles;
        uint16_t serial_length;
        uint8_t double_speed_alignment;
        uint8_t serial_count;
    );

    /* APU */
    GB_SECTION(apu,
        GB_apu_t apu;
    );

    /* RTC */
    GB_SECTION(rtc,
        GB_rtc_time_t rtc_real, rtc_latched;
        uint64_t last_rtc_second;
        bool rtc_latch;
    );

    /* Video Display */
    GB_SECTION(video,
        uint32_t vram_size; // Different between CGB and DMG
        uint8_t cgb_vram_bank;
        uint8_t oam[0xA0];
        uint8_t background_palettes_data[0x40];
        uint8_t sprite_palettes_data[0x40];
        uint8_t position_in_line;
        bool stat_interrupt_line;
        uint8_t effective_scx;
        uint8_t window_y;
        /* The LCDC will skip the first frame it renders after turning it on.
           On the CGB, a frame is not skipped if the previous frame was skipped as well.
           See https://www.reddit.com/r/EmuDev/comments/6exyxu/ */
               
        /* TODO: Drop this and properly emulate the dropped vreset signal*/
        enum {
            GB_FRAMESKIP_LCD_TURNED_ON, // On a DMG, the LCD renders a blank screen during this state,
                                        // on a CGB, the previous frame is repeated (which might be
                                        // blank if the LCD was off for more than a few cycles)
            GB_FRAMESKIP_FIRST_FRAME_SKIPPED, // This state is 'skipped' when emulating a DMG
            GB_FRAMESKIP_SECOND_FRAME_RENDERED,
        } frame_skip_state;
        bool oam_read_blocked;
        bool vram_read_blocked;
        bool oam_write_blocked;
        bool vram_write_blocked;
        bool fifo_insertion_glitch;
        uint8_t current_line;
        uint16_t ly_for_comparison;
        GB_fifo_t bg_fifo, oam_fifo;
        uint8_t fetcher_x;
        uint8_t fetcher_y;
        uint16_t cycles_for_line;
        uint8_t current_tile;
        uint8_t current_tile_attributes;
        uint8_t current_tile_data[2];
        uint8_t fetcher_state;
        bool window_is_being_fetched;
        bool wx166_glitch;
        bool wx_triggered;
        uint8_t visible_objs[10];
        uint8_t obj_comparators[10];
        uint8_t n_visible_objs;
        uint8_t oam_search_index;
        uint8_t accessed_oam_row;
        uint8_t extra_penalty_for_sprite_at_0;
        uint8_t mode_for_interrupt;
        bool lyc_interrupt_line;
        bool cgb_palettes_blocked;
        uint8_t current_lcd_line; // The LCD can go out of sync since the vsync signal is skipped in some cases.
        uint32_t cycles_in_stop_mode;
        uint8_t object_priority;
        bool oam_ppu_blocked;
        bool vram_ppu_blocked;
        bool cgb_palettes_ppu_blocked;
        bool object_fetch_aborted;
        bool during_object_fetch;
        uint16_t object_low_line_address;
        bool wy_triggered;
        uint8_t window_tile_x;
        uint8_t lcd_x; // The LCD can go out of sync since the push signal is skipped in some cases.
        bool is_odd_frame;
        uint16_t last_tile_data_address;
        uint16_t last_tile_index_address;
        bool cgb_repeated_a_frame;
    );

    /* Unsaved data. This includes all pointers, as well as everything that shouldn't be on a save state */
    /* This data is reserved on reset and must come last in the struct */
    GB_SECTION(unsaved,
        /* ROM */
        uint8_t *rom;
        uint32_t rom_size;
        const GB_cartridge_t *cartridge_type;
        enum {
            GB_STANDARD_MBC1_WIRING,
            GB_MBC1M_WIRING,
        } mbc1_wiring;
        bool is_mbc30;

        unsigned pending_cycles;
               
        /* Various RAMs */
        uint8_t *ram;
        uint8_t *vram;
        uint8_t *mbc_ram;

        /* I/O */
        uint32_t *screen;
        uint32_t background_palettes_rgb[0x20];
        uint32_t sprite_palettes_rgb[0x20];
        const GB_palette_t *dmg_palette;
        GB_color_correction_mode_t color_correction_mode;
        bool keys[4][GB_KEY_MAX];
        GB_border_mode_t border_mode;
        GB_sgb_border_t borrowed_border;
        bool tried_loading_sgb_border;
        bool has_sgb_border;
               
        /* Timing */
        uint64_t last_sync;
        uint64_t cycles_since_last_sync; // In 8MHz units

        /* Audio */
        GB_apu_output_t apu_output;

        /* Callbacks */
        void *user_data;
        GB_log_callback_t log_callback;
        GB_input_callback_t input_callback;
        GB_input_callback_t async_input_callback;
        GB_rgb_encode_callback_t rgb_encode_callback;
        GB_vblank_callback_t vblank_callback;
        GB_infrared_callback_t infrared_callback;
        GB_camera_get_pixel_callback_t camera_get_pixel_callback;
        GB_camera_update_request_callback_t camera_update_request_callback;
        GB_rumble_callback_t rumble_callback;
        GB_serial_transfer_bit_start_callback_t serial_transfer_bit_start_callback;
        GB_serial_transfer_bit_end_callback_t serial_transfer_bit_end_callback;
        GB_update_input_hint_callback_t update_input_hint_callback;
        GB_joyp_write_callback_t joyp_write_callback;
        GB_icd_pixel_callback_t icd_pixel_callback;
        GB_icd_vreset_callback_t icd_hreset_callback;
        GB_icd_vreset_callback_t icd_vreset_callback;
        GB_read_memory_callback_t read_memory_callback;
        GB_boot_rom_load_callback_t boot_rom_load_callback;
        GB_print_image_callback_t printer_callback;
        GB_workboy_set_time_callback workboy_set_time_callback;
        GB_workboy_get_time_callback workboy_get_time_callback;
               
        /* IR */
        uint64_t cycles_since_ir_change; // In 8MHz units
        uint64_t cycles_since_input_ir_change; // In 8MHz units
        GB_ir_queue_item_t ir_queue[GB_MAX_IR_QUEUE];
        size_t ir_queue_length;

        /*** Debugger ***/
        volatile bool debug_stopped, debug_disable;
        bool debug_fin_command, debug_next_command;

        /* Breakpoints */
        uint16_t n_breakpoints;
        struct GB_breakpoint_s *breakpoints;
        bool has_jump_to_breakpoints, has_software_breakpoints;
        void *nontrivial_jump_state;
        bool non_trivial_jump_breakpoint_occured;

        /* SLD (Todo: merge with backtrace) */
        bool stack_leak_detection;
        signed debug_call_depth;
        uint16_t sp_for_call_depth[0x200]; /* Should be much more than enough */
        uint16_t addr_for_call_depth[0x200];

        /* Backtrace */
        unsigned backtrace_size;
        uint16_t backtrace_sps[0x200];
        struct {
            uint16_t bank;
            uint16_t addr;
        } backtrace_returns[0x200];

        /* Watchpoints */
        uint16_t n_watchpoints;
        struct GB_watchpoint_s *watchpoints;

        /* Symbol tables */
        GB_symbol_map_t *bank_symbols[0x200];
        GB_reversed_symbol_map_t reversed_symbol_map;

        /* Ticks command */
        uint64_t debugger_ticks;
               
        /* Rewind */
#define GB_REWIND_FRAMES_PER_KEY 255
        size_t rewind_buffer_length;
        struct {
            uint8_t *key_state;
            uint8_t *compressed_states[GB_REWIND_FRAMES_PER_KEY];
            unsigned pos;
        } *rewind_sequences; // lasts about 4 seconds
        size_t rewind_pos;
               
        /* SGB - saved and allocated optionally */
        GB_sgb_t *sgb;
        
        double sgb_intro_jingle_phases[7];
        double sgb_intro_sweep_phase;
        double sgb_intro_sweep_previous_sample;
               
        /* Cheats */
        bool cheat_enabled;
        size_t cheat_count;
        GB_cheat_t **cheats;
        GB_cheat_hash_t *cheat_hash[256];

        /* Misc */
        bool turbo;
        bool turbo_dont_skip;
        bool disable_rendering;
        uint8_t boot_rom[0x900];
        bool vblank_just_occured; // For slow operations involving syscalls; these should only run once per vblank
        uint8_t cycles_since_run; // How many cycles have passed since the last call to GB_run(), in 8MHz units
        double clock_multiplier;
        GB_rumble_mode_t rumble_mode;
        uint32_t rumble_on_cycles;
        uint32_t rumble_off_cycles;
               
        /* Temporary state */
        bool wx_just_changed;
   );
};
    
#ifndef GB_INTERNAL
struct GB_gameboy_s {
    char __internal[sizeof(struct GB_gameboy_internal_s)];
};
#endif


#ifndef __printflike
/* Missing from Linux headers. */
#define __printflike(fmtarg, firstvararg) \
__attribute__((__format__ (__printf__, fmtarg, firstvararg)))
#endif

void GB_init(GB_gameboy_t *gb, GB_model_t model);
bool GB_is_inited(GB_gameboy_t *gb);
bool GB_is_cgb(GB_gameboy_t *gb);
bool GB_is_sgb(GB_gameboy_t *gb); // Returns true if the model is SGB or SGB2
bool GB_is_hle_sgb(GB_gameboy_t *gb); // Returns true if the model is SGB or SGB2 and the SFC/SNES side is HLE'd
GB_model_t GB_get_model(GB_gameboy_t *gb);
void GB_free(GB_gameboy_t *gb);
void GB_reset(GB_gameboy_t *gb);
void GB_switch_model_and_reset(GB_gameboy_t *gb, GB_model_t model);

/* Returns the time passed, in 8MHz ticks. */
uint8_t GB_run(GB_gameboy_t *gb);
/* Returns the time passed since the last frame, in nanoseconds */
uint64_t GB_run_frame(GB_gameboy_t *gb);

typedef enum {
    GB_DIRECT_ACCESS_ROM,
    GB_DIRECT_ACCESS_RAM,
    GB_DIRECT_ACCESS_CART_RAM,
    GB_DIRECT_ACCESS_VRAM,
    GB_DIRECT_ACCESS_HRAM,
    GB_DIRECT_ACCESS_IO, /* Warning: Some registers can only be read/written correctly via GB_memory_read/write. */
    GB_DIRECT_ACCESS_BOOTROM,
    GB_DIRECT_ACCESS_OAM,
    GB_DIRECT_ACCESS_BGP,
    GB_DIRECT_ACCESS_OBP,
    GB_DIRECT_ACCESS_IE,
} GB_direct_access_t;

/* Returns a mutable pointer to various hardware memories. If that memory is banked, the current bank
   is returned at *bank, even if only a portion of the memory is banked. */
void *GB_get_direct_access(GB_gameboy_t *gb, GB_direct_access_t access, size_t *size, uint16_t *bank);

void *GB_get_user_data(GB_gameboy_t *gb);
void GB_set_user_data(GB_gameboy_t *gb, void *data);



int GB_load_boot_rom(GB_gameboy_t *gb, const char *path);
void GB_load_boot_rom_from_buffer(GB_gameboy_t *gb, const unsigned char *buffer, size_t size);
int GB_load_rom(GB_gameboy_t *gb, const char *path);
void GB_load_rom_from_buffer(GB_gameboy_t *gb, const uint8_t *buffer, size_t size);
int GB_load_isx(GB_gameboy_t *gb, const char *path);
    
int GB_save_battery_size(GB_gameboy_t *gb);
int GB_save_battery_to_buffer(GB_gameboy_t *gb, uint8_t *buffer, size_t size);
int GB_save_battery(GB_gameboy_t *gb, const char *path);

void GB_load_battery_from_buffer(GB_gameboy_t *gb, const uint8_t *buffer, size_t size);
void GB_load_battery(GB_gameboy_t *gb, const char *path);

void GB_set_turbo_mode(GB_gameboy_t *gb, bool on, bool no_frame_skip);
void GB_set_rendering_disabled(GB_gameboy_t *gb, bool disabled);
    
void GB_log(GB_gameboy_t *gb, const char *fmt, ...) __printflike(2, 3);
void GB_attributed_log(GB_gameboy_t *gb, GB_log_attributes attributes, const char *fmt, ...) __printflike(3, 4);

void GB_set_pixels_output(GB_gameboy_t *gb, uint32_t *output);
void GB_set_border_mode(GB_gameboy_t *gb, GB_border_mode_t border_mode);
    
void GB_set_infrared_input(GB_gameboy_t *gb, bool state);
void GB_queue_infrared_input(GB_gameboy_t *gb, bool state, uint64_t cycles_after_previous_change); /* In 8MHz units*/
    
void GB_set_vblank_callback(GB_gameboy_t *gb, GB_vblank_callback_t callback);
void GB_set_log_callback(GB_gameboy_t *gb, GB_log_callback_t callback);
void GB_set_input_callback(GB_gameboy_t *gb, GB_input_callback_t callback);
void GB_set_async_input_callback(GB_gameboy_t *gb, GB_input_callback_t callback);
void GB_set_rgb_encode_callback(GB_gameboy_t *gb, GB_rgb_encode_callback_t callback);
void GB_set_infrared_callback(GB_gameboy_t *gb, GB_infrared_callback_t callback);
void GB_set_rumble_callback(GB_gameboy_t *gb, GB_rumble_callback_t callback);
void GB_set_update_input_hint_callback(GB_gameboy_t *gb, GB_update_input_hint_callback_t callback);
/* Called when a new boot ROM is needed. The callback should call GB_load_boot_rom or GB_load_boot_rom_from_buffer */
void GB_set_boot_rom_load_callback(GB_gameboy_t *gb, GB_boot_rom_load_callback_t callback);
    
void GB_set_palette(GB_gameboy_t *gb, const GB_palette_t *palette);

/* These APIs are used when using internal clock */
void GB_set_serial_transfer_bit_start_callback(GB_gameboy_t *gb, GB_serial_transfer_bit_start_callback_t callback);
void GB_set_serial_transfer_bit_end_callback(GB_gameboy_t *gb, GB_serial_transfer_bit_end_callback_t callback);

/* These APIs are used when using external clock */
bool GB_serial_get_data_bit(GB_gameboy_t *gb);
void GB_serial_set_data_bit(GB_gameboy_t *gb, bool data);
    
void GB_disconnect_serial(GB_gameboy_t *gb);
    
/* For cartridges with an alarm clock */
unsigned GB_time_to_alarm(GB_gameboy_t *gb); // 0 if no alarm
    
/* For integration with SFC/SNES emulators */
void GB_set_joyp_write_callback(GB_gameboy_t *gb, GB_joyp_write_callback_t callback);
void GB_set_icd_pixel_callback(GB_gameboy_t *gb, GB_icd_pixel_callback_t callback);
void GB_set_icd_hreset_callback(GB_gameboy_t *gb, GB_icd_hreset_callback_t callback);
void GB_set_icd_vreset_callback(GB_gameboy_t *gb, GB_icd_vreset_callback_t callback);
    
#ifdef GB_INTERNAL
uint32_t GB_get_clock_rate(GB_gameboy_t *gb);
#endif
void GB_set_clock_multiplier(GB_gameboy_t *gb, double multiplier);

unsigned GB_get_screen_width(GB_gameboy_t *gb);
unsigned GB_get_screen_height(GB_gameboy_t *gb);
double GB_get_usual_frame_rate(GB_gameboy_t *gb);
unsigned GB_get_player_count(GB_gameboy_t *gb);

#endif /* GB_h */
