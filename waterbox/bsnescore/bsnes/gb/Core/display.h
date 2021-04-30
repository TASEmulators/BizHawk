#ifndef display_h
#define display_h

#include "gb.h"
#include <stdbool.h>
#include <stdint.h>

#ifdef GB_INTERNAL
void GB_display_run(GB_gameboy_t *gb, uint8_t cycles);
void GB_palette_changed(GB_gameboy_t *gb, bool background_palette, uint8_t index);
void GB_STAT_update(GB_gameboy_t *gb);
void GB_lcd_off(GB_gameboy_t *gb);

enum {
  GB_OBJECT_PRIORITY_UNDEFINED, // For save state compatibility
  GB_OBJECT_PRIORITY_X,
  GB_OBJECT_PRIORITY_INDEX,
};

#endif

typedef enum {
    GB_PALETTE_NONE,
    GB_PALETTE_BACKGROUND,
    GB_PALETTE_OAM,
    GB_PALETTE_AUTO,
} GB_palette_type_t;

typedef enum {
    GB_MAP_AUTO,
    GB_MAP_9800,
    GB_MAP_9C00,
} GB_map_type_t;

typedef enum {
    GB_TILESET_AUTO,
    GB_TILESET_8800,
    GB_TILESET_8000,
} GB_tileset_type_t;

typedef struct {
    uint32_t image[128];
    uint8_t x, y, tile, flags;
    uint16_t oam_addr;
    bool obscured_by_line_limit;
} GB_oam_info_t;

typedef enum {
    GB_COLOR_CORRECTION_DISABLED,
    GB_COLOR_CORRECTION_CORRECT_CURVES,
    GB_COLOR_CORRECTION_EMULATE_HARDWARE,
    GB_COLOR_CORRECTION_PRESERVE_BRIGHTNESS,
    GB_COLOR_CORRECTION_REDUCE_CONTRAST,
} GB_color_correction_mode_t;

void GB_draw_tileset(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index);
void GB_draw_tilemap(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index, GB_map_type_t map_type, GB_tileset_type_t tileset_type);
uint8_t GB_get_oam_info(GB_gameboy_t *gb, GB_oam_info_t *dest, uint8_t *sprite_height);
uint32_t GB_convert_rgb15(GB_gameboy_t *gb, uint16_t color, bool for_border);
void GB_set_color_correction_mode(GB_gameboy_t *gb, GB_color_correction_mode_t mode);
bool GB_is_odd_frame(GB_gameboy_t *gb);
#endif /* display_h */
