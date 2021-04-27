#ifndef sgb_h
#define sgb_h
#include "gb_struct_def.h"
#include <stdint.h>
#include <stdbool.h>

typedef struct GB_sgb_s GB_sgb_t;
typedef struct {
    uint8_t tiles[0x100 * 8 * 8]; /* High nibble not used*/
    union {
        struct {
            uint16_t map[32 * 32];
            uint16_t palette[16 * 4];
        };
        uint16_t raw_data[0x440];
    };
} GB_sgb_border_t;

#ifdef GB_INTERNAL
struct GB_sgb_s {
    uint8_t command[16 * 7];
    uint16_t command_write_index;
    bool ready_for_pulse;
    bool ready_for_write;
    bool ready_for_stop;
    bool disable_commands;
    
    /* Screen buffer */
    uint8_t screen_buffer[160 * 144]; // Live image from the Game Boy
    uint8_t effective_screen_buffer[160 * 144]; // Image actually rendered to the screen
    
    /* Multiplayer Input */
    uint8_t player_count, current_player;
    
    /* Mask */
    uint8_t mask_mode;
    
    /* Data Transfer */
    uint8_t vram_transfer_countdown, transfer_dest;
    
    /* Border */
    GB_sgb_border_t border, pending_border;
    uint8_t border_animation;
    
    /* Colorization */
    uint16_t effective_palettes[4 * 4];
    uint16_t ram_palettes[4 * 512];
    uint8_t attribute_map[20 * 18];
    uint8_t attribute_files[0xFE0];
    
    /* Intro */
    int16_t intro_animation;
    
    /* GB Header */
    uint8_t received_header[0x54];
    
    /* Multiplayer (cont) */
    bool mlt_lock;
};

void GB_sgb_write(GB_gameboy_t *gb, uint8_t value);
void GB_sgb_render(GB_gameboy_t *gb);
void GB_sgb_load_default_data(GB_gameboy_t *gb);

#endif

#endif
