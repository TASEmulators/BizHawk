#ifndef display_h
#define display_h

#include "gb.h"
#ifdef GB_INTERNAL
void GB_display_run(GB_gameboy_t *gb, uint8_t cycles);
void GB_palette_changed(GB_gameboy_t *gb, bool background_palette, uint8_t index);
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

void GB_draw_tileset(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index);
void GB_draw_tilemap(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index, GB_map_type_t map_type, GB_tileset_type_t tileset_type);
uint8_t GB_get_oam_info(GB_gameboy_t *gb, GB_oam_info_t *dest, uint8_t *sprite_height);

#endif /* display_h */
