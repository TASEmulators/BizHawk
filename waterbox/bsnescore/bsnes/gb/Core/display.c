#include <stdbool.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include "gb.h"

/* FIFO functions */

static inline unsigned fifo_size(GB_fifo_t *fifo)
{
    return (fifo->write_end - fifo->read_end) & (GB_FIFO_LENGTH - 1);
}

static void fifo_clear(GB_fifo_t *fifo)
{
    fifo->read_end = fifo->write_end = 0;
}

static GB_fifo_item_t *fifo_pop(GB_fifo_t *fifo)
{
    GB_fifo_item_t *ret = &fifo->fifo[fifo->read_end];
    fifo->read_end++;
    fifo->read_end &= (GB_FIFO_LENGTH - 1);
    return ret;
}

static void fifo_push_bg_row(GB_fifo_t *fifo, uint8_t lower, uint8_t upper, uint8_t palette, bool bg_priority, bool flip_x)
{
    if (!flip_x) {
        UNROLL
        for (unsigned i = 8; i--;) {
            fifo->fifo[fifo->write_end] = (GB_fifo_item_t) {
                (lower >> 7) | ((upper >> 7) << 1),
                palette,
                0,
                bg_priority,
            };
            lower <<= 1;
            upper <<= 1;
            
            fifo->write_end++;
            fifo->write_end &= (GB_FIFO_LENGTH - 1);
        }
    }
    else {
        UNROLL
        for (unsigned i = 8; i--;) {
            fifo->fifo[fifo->write_end] = (GB_fifo_item_t) {
                (lower & 1) | ((upper & 1) << 1),
                palette,
                0,
                bg_priority,
            };
            lower >>= 1;
            upper >>= 1;
            
            fifo->write_end++;
            fifo->write_end &= (GB_FIFO_LENGTH - 1);
        }
    }
}

static void fifo_overlay_object_row(GB_fifo_t *fifo, uint8_t lower, uint8_t upper, uint8_t palette, bool bg_priority, uint8_t priority, bool flip_x)
{
    while (fifo_size(fifo) < 8) {
        fifo->fifo[fifo->write_end] = (GB_fifo_item_t) {0,};
        fifo->write_end++;
        fifo->write_end &= (GB_FIFO_LENGTH - 1);
    }
    
    uint8_t flip_xor = flip_x? 0: 0x7;
    
    UNROLL
    for (unsigned i = 8; i--;) {
        uint8_t pixel = (lower >> 7) | ((upper >> 7) << 1);
        GB_fifo_item_t *target = &fifo->fifo[(fifo->read_end + (i ^ flip_xor)) & (GB_FIFO_LENGTH - 1)];
        if (pixel != 0 && (target->pixel == 0 || target->priority > priority)) {
            target->pixel = pixel;
            target->palette = palette;
            target->bg_priority = bg_priority;
            target->priority = priority;
        }
        lower <<= 1;
        upper <<= 1;
    }
}


/*
 Each line is 456 cycles. Without scrolling, sprites or a window:
 Mode 2 - 80  cycles / OAM Transfer
 Mode 3 - 172 cycles / Rendering
 Mode 0 - 204 cycles / HBlank
 
 Mode 1 is VBlank
 */

#define MODE2_LENGTH (80)
#define LINE_LENGTH (456)
#define LINES (144)
#define WIDTH (160)
#define BORDERED_WIDTH 256
#define BORDERED_HEIGHT 224
#define FRAME_LENGTH (LCDC_PERIOD)
#define VIRTUAL_LINES (FRAME_LENGTH / LINE_LENGTH) // = 154

typedef struct __attribute__((packed)) {
    uint8_t y;
    uint8_t x;
    uint8_t tile;
    uint8_t flags;
} GB_object_t;

static void display_vblank(GB_gameboy_t *gb)
{  
    gb->vblank_just_occured = true;
    
    /* TODO: Slow in turbo mode! */
    if (GB_is_hle_sgb(gb)) {
        GB_sgb_render(gb);
    }
    
    if (gb->turbo) {
        if (GB_timing_sync_turbo(gb)) {
            return;
        }
    }
    
    bool is_ppu_stopped = !GB_is_cgb(gb) && gb->stopped && gb->io_registers[GB_IO_LCDC] & 0x80;
    
    if (!gb->disable_rendering  && ((!(gb->io_registers[GB_IO_LCDC] & 0x80) || is_ppu_stopped) || gb->cgb_repeated_a_frame)) {
        /* LCD is off, set screen to white or black (if LCD is on in stop mode) */
        if (!GB_is_sgb(gb)) {
            uint32_t color = 0;
            if (GB_is_cgb(gb)) {
                color = GB_convert_rgb15(gb, 0x7FFF, false);
            }
            else {
                color = is_ppu_stopped ?
                            gb->background_palettes_rgb[0] :
                            gb->background_palettes_rgb[4];
            }
            if (gb->border_mode == GB_BORDER_ALWAYS) {
                for (unsigned y = 0; y < LINES; y++) {
                    for (unsigned x = 0; x < WIDTH; x++) {
                        gb ->screen[x + y * BORDERED_WIDTH + (BORDERED_WIDTH - WIDTH) / 2 + (BORDERED_HEIGHT - LINES) / 2 * BORDERED_WIDTH] = color;
                    }
                }
            }
            else {
                for (unsigned i = 0; i < WIDTH * LINES; i++) {
                    gb ->screen[i] = color;
                }
            }
        }
    }
    
    if (gb->border_mode == GB_BORDER_ALWAYS && !GB_is_sgb(gb)) {
        GB_borrow_sgb_border(gb);
        uint32_t border_colors[16 * 4];
        
        if (!gb->has_sgb_border && GB_is_cgb(gb) && gb->model != GB_MODEL_AGB) {
            static uint16_t colors[] = {
                0x2095, 0x5129, 0x1EAF, 0x1EBA, 0x4648,
                0x30DA, 0x69AD, 0x2B57, 0x2B5D, 0x632C,
                0x1050, 0x3C84, 0x0E07, 0x0E18, 0x2964,
            };
            unsigned index = gb->rom? gb->rom[0x14e] % 5 : 0;
            gb->borrowed_border.palette[0] = colors[index];
            gb->borrowed_border.palette[10] = colors[5 + index];
            gb->borrowed_border.palette[14] = colors[10 + index];

        }
        
        for (unsigned i = 0; i < 16 * 4; i++) {
            border_colors[i] = GB_convert_rgb15(gb, gb->borrowed_border.palette[i], true);
        }
        
        for (unsigned tile_y = 0; tile_y < 28; tile_y++) {
            for (unsigned tile_x = 0; tile_x < 32; tile_x++) {
                if (tile_x >= 6 && tile_x < 26 && tile_y >= 5 && tile_y < 23) {
                    continue;
                }
                uint16_t tile = gb->borrowed_border.map[tile_x + tile_y * 32];
                uint8_t flip_x = (tile & 0x4000)? 0x7 : 0;
                uint8_t flip_y = (tile & 0x8000)? 0x7 : 0;
                uint8_t palette = (tile >> 10) & 3;
                for (unsigned y = 0; y < 8; y++) {
                    for (unsigned x = 0; x < 8; x++) {
                        uint8_t color = gb->borrowed_border.tiles[(tile & 0xFF) * 64 + (x ^ flip_x) + (y ^ flip_y) * 8] & 0xF;
                        uint32_t *output = gb->screen + tile_x * 8 + x + (tile_y * 8 + y) * 256;
                        if (color == 0) {
                            *output = border_colors[0];
                        }
                        else {
                            *output = border_colors[color + palette * 16];
                        }
                    }
                }
            }
        }
    }
    GB_handle_rumble(gb);

    if (gb->vblank_callback) {
        gb->vblank_callback(gb);
    }
    GB_timing_sync(gb);
}

static inline uint8_t scale_channel(uint8_t x)
{
    return (x << 3) | (x >> 2);
}

static inline uint8_t scale_channel_with_curve(uint8_t x)
{
    return (uint8_t[]){0,5,8,11,16,22,28,36,43,51,59,67,77,87,97,107,119,130,141,153,166,177,188,200,209,221,230,238,245,249,252,255}[x];
}

static inline uint8_t scale_channel_with_curve_agb(uint8_t x)
{
    return (uint8_t[]){0,2,5,10,15,20,26,32,38,45,52,60,68,76,84,92,101,110,119,128,138,148,158,168,178,189,199,210,221,232,244,255}[x];
}

static inline uint8_t scale_channel_with_curve_sgb(uint8_t x)
{
    return (uint8_t[]){0,2,5,9,15,20,27,34,42,50,58,67,76,85,94,104,114,123,133,143,153,163,173,182,192,202,211,220,229,238,247,255}[x];
}


uint32_t GB_convert_rgb15(GB_gameboy_t *gb, uint16_t color, bool for_border)
{
    uint8_t r = (color) & 0x1F;
    uint8_t g = (color >> 5) & 0x1F;
    uint8_t b = (color >> 10) & 0x1F;
    
    if (gb->color_correction_mode == GB_COLOR_CORRECTION_DISABLED || (for_border && !gb->has_sgb_border)) {
        r = scale_channel(r);
        g = scale_channel(g);
        b = scale_channel(b);
    }
    else {
        if (GB_is_sgb(gb) || for_border) {
            return gb->rgb_encode_callback(gb,
                                           scale_channel_with_curve_sgb(r),
                                           scale_channel_with_curve_sgb(g),
                                           scale_channel_with_curve_sgb(b));
        }
        bool agb = gb->model == GB_MODEL_AGB;
        r = agb? scale_channel_with_curve_agb(r) : scale_channel_with_curve(r);
        g = agb? scale_channel_with_curve_agb(g) : scale_channel_with_curve(g);
        b = agb? scale_channel_with_curve_agb(b) : scale_channel_with_curve(b);
        
        if (gb->color_correction_mode != GB_COLOR_CORRECTION_CORRECT_CURVES) {
            uint8_t new_r, new_g, new_b;
            if (agb) {
                new_g = (g * 6 + b * 1) / 7;
            }
            else {
                new_g = (g * 3 + b) / 4;
            }
            new_r = r;
            new_b = b;
            if (gb->color_correction_mode == GB_COLOR_CORRECTION_REDUCE_CONTRAST) {
                r = new_r;
                g = new_r;
                b = new_r;
                
                new_r = new_r * 7 / 8 + (    g + b) / 16;
                new_g = new_g * 7 / 8 + (r   +   b) / 16;
                new_b = new_b * 7 / 8 + (r + g    ) / 16;

                
                new_r = new_r * (224 - 32) / 255 + 32;
                new_g = new_g * (220 - 36) / 255 + 36;
                new_b = new_b * (216 - 40) / 255 + 40;
            }
            else if (gb->color_correction_mode == GB_COLOR_CORRECTION_PRESERVE_BRIGHTNESS) {
                uint8_t old_max = MAX(r, MAX(g, b));
                uint8_t new_max = MAX(new_r, MAX(new_g, new_b));
                
                if (new_max != 0) {
                    new_r = new_r * old_max / new_max;
                    new_g = new_g * old_max / new_max;
                    new_b = new_b * old_max / new_max;
                }
                
                uint8_t old_min = MIN(r, MIN(g, b));
                uint8_t new_min = MIN(new_r, MIN(new_g, new_b));
                
                if (new_min != 0xff) {
                    new_r = 0xff - (0xff - new_r) * (0xff - old_min) / (0xff - new_min);
                    new_g = 0xff - (0xff - new_g) * (0xff - old_min) / (0xff - new_min);
                    new_b = 0xff - (0xff - new_b) * (0xff - old_min) / (0xff - new_min);
                }
            }
            r = new_r;
            g = new_g;
            b = new_b;
        }
    }
    
    return gb->rgb_encode_callback(gb, r, g, b);
}

void GB_palette_changed(GB_gameboy_t *gb, bool background_palette, uint8_t index)
{
    if (!gb->rgb_encode_callback || !GB_is_cgb(gb)) return;
    uint8_t *palette_data = background_palette? gb->background_palettes_data : gb->sprite_palettes_data;
    uint16_t color = palette_data[index & ~1] | (palette_data[index | 1] << 8);

    (background_palette? gb->background_palettes_rgb : gb->sprite_palettes_rgb)[index / 2] = GB_convert_rgb15(gb, color, false);
}

void GB_set_color_correction_mode(GB_gameboy_t *gb, GB_color_correction_mode_t mode)
{
    gb->color_correction_mode = mode;
    if (GB_is_cgb(gb)) {
        for (unsigned i = 0; i < 32; i++) {
            GB_palette_changed(gb, false, i * 2);
            GB_palette_changed(gb, true, i * 2);
        }
    }
}

/*
 STAT interrupt is implemented based on this finding:
 http://board.byuu.org/phpbb3/viewtopic.php?p=25527#p25531
 
 General timing is based on GiiBiiAdvance's documents:
 https://github.com/AntonioND/giibiiadvance
 
 */

void GB_STAT_update(GB_gameboy_t *gb)
{
    if (!(gb->io_registers[GB_IO_LCDC] & 0x80)) return;
    
    bool previous_interrupt_line = gb->stat_interrupt_line;
    /* Set LY=LYC bit */
    /* TODO: This behavior might not be correct for CGB revisions other than C and E */
    if (gb->ly_for_comparison != (uint16_t)-1 || gb->model <= GB_MODEL_CGB_C) {
        if (gb->ly_for_comparison == gb->io_registers[GB_IO_LYC]) {
            gb->lyc_interrupt_line = true;
            gb->io_registers[GB_IO_STAT] |= 4;
        }
        else {
            if (gb->ly_for_comparison != (uint16_t)-1) {
                gb->lyc_interrupt_line = false;
            }
            gb->io_registers[GB_IO_STAT] &= ~4;
        }
    }
    
    switch (gb->mode_for_interrupt) {
        case 0: gb->stat_interrupt_line = gb->io_registers[GB_IO_STAT] & 8; break;
        case 1: gb->stat_interrupt_line = gb->io_registers[GB_IO_STAT] & 0x10; break;
        case 2: gb->stat_interrupt_line = gb->io_registers[GB_IO_STAT] & 0x20; break;
        default: gb->stat_interrupt_line = false;
    }
    
    /* User requested a LY=LYC interrupt and the LY=LYC bit is on */
    if ((gb->io_registers[GB_IO_STAT] & 0x40) && gb->lyc_interrupt_line) {
        gb->stat_interrupt_line = true;
    }
    
    if (gb->stat_interrupt_line && !previous_interrupt_line) {
        gb->io_registers[GB_IO_IF] |= 2;
    }
}

void GB_lcd_off(GB_gameboy_t *gb)
{
    gb->display_state = 0;
    gb->display_cycles = 0;
    /* When the LCD is disabled, state is constant */
    
    /* When the LCD is off, LY is 0 and STAT mode is 0.  */
    gb->io_registers[GB_IO_LY] = 0;
    gb->io_registers[GB_IO_STAT] &= ~3;
    if (gb->hdma_on_hblank) {
        gb->hdma_on_hblank = false;
        gb->hdma_on = false;
        
        /* Todo: is this correct? */
        gb->hdma_steps_left = 0xff;
    }
    
    gb->oam_read_blocked = false;
    gb->vram_read_blocked = false;
    gb->oam_write_blocked = false;
    gb->vram_write_blocked = false;
    gb->cgb_palettes_blocked = false;
    
    gb->current_line = 0;
    gb->ly_for_comparison = 0;
    
    gb->accessed_oam_row = -1;
    gb->wy_triggered = false;
}

static void add_object_from_index(GB_gameboy_t *gb, unsigned index)
{
    if (gb->n_visible_objs == 10) return;
    
    /* TODO: It appears that DMA blocks PPU access to OAM, but it needs verification. */
    if (gb->dma_steps_left && (gb->dma_cycles >= 0 || gb->is_dma_restarting)) {
        return;
    }
    
    if (gb->oam_ppu_blocked) {
        return;
    }

    /* This reverse sorts the visible objects by location and priority */
    GB_object_t *objects = (GB_object_t *) &gb->oam;
    bool height_16 = (gb->io_registers[GB_IO_LCDC] & 4) != 0;
    signed y = objects[index].y - 16;
    if (y <= gb->current_line && y + (height_16? 16 : 8) > gb->current_line) {
        unsigned j = 0;
        for (; j < gb->n_visible_objs; j++) {
            if (gb->obj_comparators[j] <= objects[index].x) break;
        }
        memmove(gb->visible_objs + j + 1, gb->visible_objs + j, gb->n_visible_objs - j);
        memmove(gb->obj_comparators + j + 1, gb->obj_comparators + j, gb->n_visible_objs - j);
        gb->visible_objs[j] = index;
        gb->obj_comparators[j] = objects[index].x;
        gb->n_visible_objs++;
    }
}

static void render_pixel_if_possible(GB_gameboy_t *gb)
{
    GB_fifo_item_t *fifo_item = NULL;
    GB_fifo_item_t *oam_fifo_item = NULL;
    bool draw_oam = false;
    bool bg_enabled = true, bg_priority = false;
    
    if (fifo_size(&gb->bg_fifo)) {
        fifo_item = fifo_pop(&gb->bg_fifo);
        bg_priority = fifo_item->bg_priority;
        
        if (fifo_size(&gb->oam_fifo)) {
            oam_fifo_item = fifo_pop(&gb->oam_fifo);
            if (oam_fifo_item->pixel && (gb->io_registers[GB_IO_LCDC] & 2)) {
                draw_oam = true;
                bg_priority |= oam_fifo_item->bg_priority;
            }
        }
    }
    

    if (!fifo_item) return;

    /* Drop pixels for scrollings */
    if (gb->position_in_line >= 160 || (gb->disable_rendering && !gb->sgb)) {
        gb->position_in_line++;
        return;
    }
    
    /* Mixing */
    
    if ((gb->io_registers[GB_IO_LCDC] & 0x1) == 0) {
        if (gb->cgb_mode) {
            bg_priority = false;
        }
        else {
            bg_enabled = false;
        }
    }

    uint8_t icd_pixel = 0;
    uint32_t *dest = NULL;
    if (!gb->sgb) {
        if (gb->border_mode != GB_BORDER_ALWAYS) {
            dest = gb->screen + gb->lcd_x + gb->current_line * WIDTH;
        }
        else {
            dest = gb->screen + gb->lcd_x + gb->current_line * BORDERED_WIDTH + (BORDERED_WIDTH - WIDTH) / 2 + (BORDERED_HEIGHT - LINES) / 2 * BORDERED_WIDTH;
        }
    }
    
    {
        uint8_t pixel = bg_enabled? fifo_item->pixel : 0;
        if (pixel && bg_priority) {
            draw_oam = false;
        }
        if (!gb->cgb_mode) {
            pixel = ((gb->io_registers[GB_IO_BGP] >> (pixel << 1)) & 3);
        }
        if (gb->sgb) {
            if (gb->current_lcd_line < LINES) {
                gb->sgb->screen_buffer[gb->lcd_x + gb->current_lcd_line * WIDTH] = gb->stopped? 0 : pixel;
            }
        }
        else if (gb->model & GB_MODEL_NO_SFC_BIT) {
            if (gb->icd_pixel_callback) {
                icd_pixel = pixel;
            }
        }
        else if (gb->cgb_palettes_ppu_blocked) {
            *dest = gb->rgb_encode_callback(gb, 0, 0, 0);
        }
        else {
            *dest = gb->background_palettes_rgb[fifo_item->palette * 4 + pixel];
        }
    }
    
    if (draw_oam) {
        uint8_t pixel = oam_fifo_item->pixel;
        if (!gb->cgb_mode) {
            /* Todo: Verify access timings */
            pixel = ((gb->io_registers[oam_fifo_item->palette? GB_IO_OBP1 : GB_IO_OBP0] >> (pixel << 1)) & 3);
        }
        if (gb->sgb) {
            if (gb->current_lcd_line < LINES) {
                gb->sgb->screen_buffer[gb->lcd_x + gb->current_lcd_line * WIDTH] = gb->stopped? 0 : pixel;
            }
        }
        else if (gb->model & GB_MODEL_NO_SFC_BIT) {
            if (gb->icd_pixel_callback) {
                icd_pixel = pixel;
              //gb->icd_pixel_callback(gb, pixel);
            }
        }
        else if (gb->cgb_palettes_ppu_blocked) {
            *dest = gb->rgb_encode_callback(gb, 0, 0, 0);
        }
        else {
            *dest = gb->sprite_palettes_rgb[oam_fifo_item->palette * 4 + pixel];
        }
    }
    
    if (gb->model & GB_MODEL_NO_SFC_BIT) {
        if (gb->icd_pixel_callback) {
            gb->icd_pixel_callback(gb, icd_pixel);
        }
    }
    
    gb->position_in_line++;
    gb->lcd_x++;
    gb->window_is_being_fetched = false;
}

/* All verified CGB timings are based on CGB CPU E. CGB CPUs >= D are known to have
   slightly different timings than CPUs <= C.
 
   Todo: Add support to CPU C and older */

static inline uint8_t fetcher_y(GB_gameboy_t *gb)
{
    return gb->wx_triggered? gb->window_y : gb->current_line + gb->io_registers[GB_IO_SCY];
}

static void advance_fetcher_state_machine(GB_gameboy_t *gb)
{
    typedef enum {
        GB_FETCHER_GET_TILE,
        GB_FETCHER_GET_TILE_DATA_LOWER,
        GB_FETCHER_GET_TILE_DATA_HIGH,
        GB_FETCHER_PUSH,
        GB_FETCHER_SLEEP,
    } fetcher_step_t;
    
    fetcher_step_t fetcher_state_machine [8] = {
        GB_FETCHER_SLEEP,
        GB_FETCHER_GET_TILE,
        GB_FETCHER_SLEEP,
        GB_FETCHER_GET_TILE_DATA_LOWER,
        GB_FETCHER_SLEEP,
        GB_FETCHER_GET_TILE_DATA_HIGH,
        GB_FETCHER_PUSH,
        GB_FETCHER_PUSH,
    };
    switch (fetcher_state_machine[gb->fetcher_state & 7]) {
        case GB_FETCHER_GET_TILE: {
            uint16_t map = 0x1800;
            
            if (!(gb->io_registers[GB_IO_LCDC] & 0x20)) {
                gb->wx_triggered = false;
                gb->wx166_glitch = false;
            }
            
            /* Todo: Verified for DMG (Tested: SGB2), CGB timing is wrong. */
            if (gb->io_registers[GB_IO_LCDC] & 0x08  && !gb->wx_triggered) {
                map = 0x1C00;
            }
            else if (gb->io_registers[GB_IO_LCDC] & 0x40 && gb->wx_triggered) {
                map = 0x1C00;
            }
            
            /* Todo: Verified for DMG (Tested: SGB2), CGB timing is wrong. */
            uint8_t y = fetcher_y(gb);
            uint8_t x = 0;
            if (gb->wx_triggered) {
                x = gb->window_tile_x;
            }
            else {
                x = ((gb->io_registers[GB_IO_SCX] / 8) + gb->fetcher_x) & 0x1F;
            }
            if (gb->model > GB_MODEL_CGB_C) {
                /* This value is cached on the CGB-D and newer, so it cannot be used to mix tiles together */
                gb->fetcher_y = y;
            }
            gb->last_tile_index_address = map + x + y / 8 * 32;
            gb->current_tile = gb->vram[gb->last_tile_index_address];
            if (gb->vram_ppu_blocked) {
                gb->current_tile = 0xFF;
            }
            if (GB_is_cgb(gb)) {
                /* The CGB actually accesses both the tile index AND the attributes in the same T-cycle.
                   This probably means the CGB has a 16-bit data bus for the VRAM. */
                gb->current_tile_attributes = gb->vram[gb->last_tile_index_address + 0x2000];
                if (gb->vram_ppu_blocked) {
                    gb->current_tile_attributes = 0xFF;
                }
            }
        }
        gb->fetcher_state++;
        break;
            
        case GB_FETCHER_GET_TILE_DATA_LOWER: {
            uint8_t y_flip = 0;
            uint16_t tile_address = 0;
            uint8_t y = gb->model > GB_MODEL_CGB_C ? gb->fetcher_y : fetcher_y(gb);
            
            /* Todo: Verified for DMG (Tested: SGB2), CGB timing is wrong. */
            if (gb->io_registers[GB_IO_LCDC] & 0x10) {
                tile_address = gb->current_tile * 0x10;
            }
            else {
                tile_address =  (int8_t)gb->current_tile * 0x10 + 0x1000;
            }
            if (gb->current_tile_attributes & 8) {
                tile_address += 0x2000;
            }
            if (gb->current_tile_attributes & 0x40) {
                y_flip = 0x7;
            }
            gb->current_tile_data[0] =
                gb->vram[tile_address + ((y & 7) ^ y_flip) * 2];
            if (gb->vram_ppu_blocked) {
                gb->current_tile_data[0] = 0xFF;
            }
        }
        gb->fetcher_state++;
        break;
            
        case GB_FETCHER_GET_TILE_DATA_HIGH: {
            /* Todo: Verified for DMG (Tested: SGB2), CGB timing is wrong.
             Additionally, on CGB-D and newer mixing two tiles by changing the tileset
             bit mid-fetching causes a glitched mixing of the two, in comparison to the
             more logical DMG version. */
            uint16_t tile_address = 0;
            uint8_t y = gb->model > GB_MODEL_CGB_C ? gb->fetcher_y : fetcher_y(gb);
            
            if (gb->io_registers[GB_IO_LCDC] & 0x10) {
                tile_address = gb->current_tile * 0x10;
            }
            else {
                tile_address =  (int8_t)gb->current_tile * 0x10 + 0x1000;
            }
            if (gb->current_tile_attributes & 8) {
                tile_address += 0x2000;
            }
            uint8_t y_flip = 0;
            if (gb->current_tile_attributes & 0x40) {
                y_flip = 0x7;
            }
            gb->last_tile_data_address = tile_address +  ((y & 7) ^ y_flip) * 2 + 1;
            gb->current_tile_data[1] =
                gb->vram[gb->last_tile_data_address];
            if (gb->vram_ppu_blocked) {
                gb->current_tile_data[1] = 0xFF;
            }
        }
        if (gb->wx_triggered) {
            gb->window_tile_x++;
            gb->window_tile_x &= 0x1f;
        }
            
        // fallthrough
        case GB_FETCHER_PUSH: {
            if (gb->fetcher_state == 6) {
                /* The background map index increase at this specific point. If this state is not reached,
                   it will simply not increase. */
                gb->fetcher_x++;
                gb->fetcher_x &= 0x1f;
            }
            if (gb->fetcher_state < 7) {
                gb->fetcher_state++;
            }
            if (fifo_size(&gb->bg_fifo) > 0) break;
            
            fifo_push_bg_row(&gb->bg_fifo, gb->current_tile_data[0], gb->current_tile_data[1],
                             gb->current_tile_attributes & 7, gb->current_tile_attributes & 0x80, gb->current_tile_attributes & 0x20);
            gb->fetcher_state = 0;
        }
        break;
            
        case GB_FETCHER_SLEEP:
        {
            gb->fetcher_state++;
        }
        break;
    }
}

static uint16_t get_object_line_address(GB_gameboy_t *gb, const GB_object_t *object)
{
    /* TODO: what does the PPU read if DMA is active? */
    if (gb->oam_ppu_blocked) {
        static const GB_object_t blocked = {0xFF, 0xFF, 0xFF, 0xFF};
        object = &blocked;
    }
    
    bool height_16 = (gb->io_registers[GB_IO_LCDC] & 4) != 0; /* Todo: Which T-cycle actually reads this? */
    uint8_t tile_y = (gb->current_line - object->y) & (height_16? 0xF : 7);
    
    if (object->flags & 0x40) { /* Flip Y */
        tile_y ^= height_16? 0xF : 7;
    }
    
    /* Todo: I'm not 100% sure an access to OAM can't trigger the OAM bug while we're accessing this */
    uint16_t line_address = (height_16? object->tile & 0xFE : object->tile) * 0x10 + tile_y * 2;
    
    if (gb->cgb_mode && (object->flags & 0x8)) { /* Use VRAM bank 2 */
        line_address += 0x2000;
    }
    return line_address;
}

/*
 TODO: It seems that the STAT register's mode bits are always "late" by 4 T-cycles.
       The PPU logic can be greatly simplified if that delay is simply emulated.
 */
void GB_display_run(GB_gameboy_t *gb, uint8_t cycles)
{
    /* The PPU does not advance while in STOP mode on the DMG */
    if (gb->stopped && !GB_is_cgb(gb)) {
        gb->cycles_in_stop_mode += cycles;
        if (gb->cycles_in_stop_mode >= LCDC_PERIOD) {
            gb->cycles_in_stop_mode -= LCDC_PERIOD;
            display_vblank(gb);
        }
        return;
    }
    GB_object_t *objects = (GB_object_t *) &gb->oam;
    
    GB_STATE_MACHINE(gb, display, cycles, 2) {
        GB_STATE(gb, display, 1);
        GB_STATE(gb, display, 2);
        // GB_STATE(gb, display, 3);
        // GB_STATE(gb, display, 4);
        // GB_STATE(gb, display, 5);
        GB_STATE(gb, display, 6);
        GB_STATE(gb, display, 7);
        GB_STATE(gb, display, 8);
        // GB_STATE(gb, display, 9);
        GB_STATE(gb, display, 10);
        GB_STATE(gb, display, 11);
        GB_STATE(gb, display, 12);
        GB_STATE(gb, display, 13);
        GB_STATE(gb, display, 14);
        GB_STATE(gb, display, 15);
        GB_STATE(gb, display, 16);
        GB_STATE(gb, display, 17);
        // GB_STATE(gb, display, 19);
        GB_STATE(gb, display, 20);
        GB_STATE(gb, display, 21);
        GB_STATE(gb, display, 22);
        GB_STATE(gb, display, 23);
        // GB_STATE(gb, display, 24);
        GB_STATE(gb, display, 25);
        GB_STATE(gb, display, 26);
        GB_STATE(gb, display, 27);
        GB_STATE(gb, display, 28);
        GB_STATE(gb, display, 29);
        GB_STATE(gb, display, 30);
        // GB_STATE(gb, display, 31);
        GB_STATE(gb, display, 32);
        GB_STATE(gb, display, 33);
        GB_STATE(gb, display, 34);
        GB_STATE(gb, display, 35);
        GB_STATE(gb, display, 36);
        GB_STATE(gb, display, 37);
        GB_STATE(gb, display, 38);
        GB_STATE(gb, display, 39);
        GB_STATE(gb, display, 40);
        GB_STATE(gb, display, 41);
        GB_STATE(gb, display, 42);
    }
    
    if (!(gb->io_registers[GB_IO_LCDC] & 0x80)) {
        while (true) {
            GB_SLEEP(gb, display, 1, LCDC_PERIOD);
            display_vblank(gb);
            gb->cgb_repeated_a_frame = true;
        }
        return;
    }
    
    gb->is_odd_frame = false;
    
    if (!GB_is_cgb(gb)) {
        GB_SLEEP(gb, display, 23, 1);
    }

    /* Handle mode 2 on the very first line 0 */
    gb->current_line = 0;
    gb->window_y = -1;
    /* Todo: verify timings */
    if (gb->io_registers[GB_IO_WY] == 0) {
        gb->wy_triggered = true;
    }
    else {
        gb->wy_triggered = false;
    }
    
    gb->ly_for_comparison = 0;
    gb->io_registers[GB_IO_STAT] &= ~3;
    gb->mode_for_interrupt = -1;
    gb->oam_read_blocked = false;
    gb->vram_read_blocked = false;
    gb->oam_write_blocked = false;
    gb->vram_write_blocked = false;
    gb->cgb_palettes_blocked = false;
    gb->cycles_for_line = MODE2_LENGTH - 4;
    GB_STAT_update(gb);
    GB_SLEEP(gb, display, 2, MODE2_LENGTH - 4);
    
    gb->oam_write_blocked = true;
    gb->cycles_for_line += 2;
    GB_STAT_update(gb);
    GB_SLEEP(gb, display, 34, 2);
    
    gb->n_visible_objs = 0;
    gb->cycles_for_line += 8; // Mode 0 is shorter on the first line 0, so we augment cycles_for_line by 8 extra cycles.

    gb->io_registers[GB_IO_STAT] &= ~3;
    gb->io_registers[GB_IO_STAT] |= 3;
    gb->mode_for_interrupt = 3;

    gb->oam_write_blocked = true;
    gb->oam_read_blocked = true;
    gb->vram_read_blocked = gb->cgb_double_speed;
    gb->vram_write_blocked = gb->cgb_double_speed;
    if (!GB_is_cgb(gb)) {
        gb->vram_read_blocked = true;
        gb->vram_write_blocked = true;
    }
    gb->cycles_for_line += 2;
    GB_SLEEP(gb, display, 37, 2);
    
    gb->cgb_palettes_blocked = true;
    gb->cycles_for_line += (GB_is_cgb(gb) && gb->model <= GB_MODEL_CGB_C)? 2 : 3;
    GB_SLEEP(gb, display, 38, (GB_is_cgb(gb) && gb->model <= GB_MODEL_CGB_C)? 2 : 3);
    
    gb->vram_read_blocked = true;
    gb->vram_write_blocked = true;
    gb->wx_triggered = false;
    gb->wx166_glitch = false;
    goto mode_3_start;
    
    while (true) {
        /* Lines 0 - 143 */
        gb->window_y = -1;
        for (; gb->current_line < LINES; gb->current_line++) {
            /* Todo: verify timings */
            if ((gb->io_registers[GB_IO_WY] == gb->current_line ||
                (gb->current_line != 0 && gb->io_registers[GB_IO_WY] == gb->current_line - 1))) {
                gb->wy_triggered = true;
            }
            
            gb->oam_write_blocked = GB_is_cgb(gb) && !gb->cgb_double_speed;
            gb->accessed_oam_row = 0;
            
            GB_SLEEP(gb, display, 35, 2);
            gb->oam_write_blocked = GB_is_cgb(gb);
            
            GB_SLEEP(gb, display, 6, 1);
            gb->io_registers[GB_IO_LY] = gb->current_line;
            gb->oam_read_blocked = true;
            gb->ly_for_comparison = gb->current_line? -1 : 0;
            
            /* The OAM STAT interrupt occurs 1 T-cycle before STAT actually changes, except on line 0.
             PPU glitch? */
            if (gb->current_line != 0) {
                gb->mode_for_interrupt = 2;
                gb->io_registers[GB_IO_STAT] &= ~3;
            }
            else if (!GB_is_cgb(gb)) {
                gb->io_registers[GB_IO_STAT] &= ~3;
            }
            GB_STAT_update(gb);

            GB_SLEEP(gb, display, 7, 1);
            
            gb->io_registers[GB_IO_STAT] &= ~3;
            gb->io_registers[GB_IO_STAT] |= 2;
            gb->mode_for_interrupt = 2;
            gb->oam_write_blocked = true;
            gb->ly_for_comparison = gb->current_line;
            GB_STAT_update(gb);
            gb->mode_for_interrupt = -1;
            GB_STAT_update(gb);
            gb->n_visible_objs = 0;
            
            for (gb->oam_search_index = 0; gb->oam_search_index < 40; gb->oam_search_index++) {
                if (GB_is_cgb(gb)) {
                    add_object_from_index(gb, gb->oam_search_index);
                    /* The CGB does not care about the accessed OAM row as there's no OAM bug */
                }
                GB_SLEEP(gb, display, 8, 2);
                if (!GB_is_cgb(gb)) {
                    add_object_from_index(gb, gb->oam_search_index);
                    gb->accessed_oam_row = (gb->oam_search_index & ~1) * 4 + 8;
                }
                if (gb->oam_search_index == 37) {
                    gb->vram_read_blocked = !GB_is_cgb(gb);
                    gb->vram_write_blocked = false;
                    gb->cgb_palettes_blocked = false;
                    gb->oam_write_blocked = GB_is_cgb(gb);
                    GB_STAT_update(gb);
                }
            }
            gb->cycles_for_line = MODE2_LENGTH + 4;

            gb->accessed_oam_row = -1;
            gb->io_registers[GB_IO_STAT] &= ~3;
            gb->io_registers[GB_IO_STAT] |= 3;
            gb->mode_for_interrupt = 3;
            gb->vram_read_blocked = true;
            gb->vram_write_blocked = true;
            gb->cgb_palettes_blocked = false;
            gb->oam_write_blocked = true;
            gb->oam_read_blocked = true;

            GB_STAT_update(gb);

            
            uint8_t idle_cycles = 3;
            if (GB_is_cgb(gb) && gb->model <= GB_MODEL_CGB_C) {
                idle_cycles = 2;
            }
            gb->cycles_for_line += idle_cycles;
            GB_SLEEP(gb, display, 10, idle_cycles);
            
            gb->cgb_palettes_blocked = true;
            gb->cycles_for_line += 2;
            GB_SLEEP(gb, display, 32, 2);
        mode_3_start:

            fifo_clear(&gb->bg_fifo);
            fifo_clear(&gb->oam_fifo);
            /* Fill the FIFO with 8 pixels of "junk", it's going to be dropped anyway. */
            fifo_push_bg_row(&gb->bg_fifo, 0, 0, 0, false, false);
            /* Todo: find out actual access time of SCX */
            gb->position_in_line = - (gb->io_registers[GB_IO_SCX] & 7) - 8;
            gb->lcd_x = 0;
          
            gb->fetcher_x = 0;
            gb->extra_penalty_for_sprite_at_0 = (gb->io_registers[GB_IO_SCX] & 7);

            
            /* The actual rendering cycle */
            gb->fetcher_state = 0;
            while (true) {
                /* Handle window */
                /* TODO: It appears that WX checks if the window begins *next* pixel, not *this* pixel. For this reason,
                   WX=167 has no effect at all (It checks if the PPU X position is 161, which never happens) and WX=166
                   has weird artifacts (It appears to activate the window during HBlank, as PPU X is temporarily 160 at
                   that point. The code should be updated to represent this, and this will fix the time travel hack in
                   WX's access conflict code. */
                
                if (!gb->wx_triggered && gb->wy_triggered && (gb->io_registers[GB_IO_LCDC] & 0x20)) {
                    bool should_activate_window = false;
                    if (gb->io_registers[GB_IO_WX] == 0) {
                        static const uint8_t scx_to_wx0_comparisons[] = {-7, -9, -10, -11, -12, -13, -14, -14};
                        if (gb->position_in_line == scx_to_wx0_comparisons[gb->io_registers[GB_IO_SCX] & 7]) {
                            should_activate_window = true;
                        }
                    }
                    else if (gb->wx166_glitch) {
                        static const uint8_t scx_to_wx166_comparisons[] = {-8, -9, -10, -11, -12, -13, -14, -15};
                        if (gb->position_in_line == scx_to_wx166_comparisons[gb->io_registers[GB_IO_SCX] & 7]) {
                            should_activate_window = true;
                        }
                    }
                    else if (gb->io_registers[GB_IO_WX] < 166 + GB_is_cgb(gb)) {
                        if (gb->io_registers[GB_IO_WX] == (uint8_t) (gb->position_in_line + 7)) {
                            should_activate_window = true;
                        }
                        else if (gb->io_registers[GB_IO_WX] == (uint8_t) (gb->position_in_line + 6) && !gb->wx_just_changed) {
                            should_activate_window = true;
                            /* LCD-PPU horizontal desync! It only appears to happen on DMGs, but not all of them.
                               This doesn't seem to be CPU revision dependent, but most revisions */
                            if ((gb->model & GB_MODEL_FAMILY_MASK) == GB_MODEL_DMG_FAMILY && !GB_is_sgb(gb)) {
                                if (gb->lcd_x > 0) {
                                    gb->lcd_x--;
                                }
                            }
                        }
                    }
                    
                    if (should_activate_window) {
                        gb->window_y++;
                        /* TODO: Verify fetcher access timings in this case */
                        if (gb->io_registers[GB_IO_WX] == 0 && (gb->io_registers[GB_IO_SCX] & 7)) {
                            gb->cycles_for_line++;
                            GB_SLEEP(gb, display, 42, 1);
                        }
                        gb->wx_triggered = true;
                        gb->window_tile_x = 0;
                        fifo_clear(&gb->bg_fifo);
                        gb->fetcher_state = 0;
                        gb->window_is_being_fetched = true;
                    }
                    else if (!GB_is_cgb(gb) && gb->io_registers[GB_IO_WX] == 166 && gb->io_registers[GB_IO_WX] == (uint8_t) (gb->position_in_line + 7)) {
                        gb->window_y++;
                    }
                }
                
                /* TODO: What happens when WX=0? */
                if (!GB_is_cgb(gb) && gb->wx_triggered && !gb->window_is_being_fetched &&
                    gb->fetcher_state == 0 && gb->io_registers[GB_IO_WX] == (uint8_t) (gb->position_in_line + 7) ) {
                    // Insert a pixel right at the FIFO's end
                    gb->bg_fifo.read_end--;
                    gb->bg_fifo.read_end &= GB_FIFO_LENGTH - 1;
                    gb->bg_fifo.fifo[gb->bg_fifo.read_end] = (GB_fifo_item_t){0,};
                    gb->window_is_being_fetched = false;
                }

                /* Handle objects */
                /* When the sprite enabled bit is off, this proccess is skipped entirely on the DMG, but not on the CGB.
                   On the CGB, this bit is checked only when the pixel is actually popped from the FIFO. */
                
                while (gb->n_visible_objs != 0 &&
                       (gb->position_in_line < 160 || gb->position_in_line >= (uint8_t)(-8)) &&
                       gb->obj_comparators[gb->n_visible_objs - 1] < (uint8_t)(gb->position_in_line + 8)) {
                    gb->n_visible_objs--;
                }
                
                gb->during_object_fetch = true;
                while (gb->n_visible_objs != 0 &&
                       (gb->io_registers[GB_IO_LCDC] & 2 || GB_is_cgb(gb)) &&
                       gb->obj_comparators[gb->n_visible_objs - 1] == (uint8_t)(gb->position_in_line + 8)) {
                    
                    while (gb->fetcher_state < 5 || fifo_size(&gb->bg_fifo) == 0) {
                        advance_fetcher_state_machine(gb);
                        gb->cycles_for_line++;
                        GB_SLEEP(gb, display, 27, 1);
                        if (gb->object_fetch_aborted) {
                            goto abort_fetching_object;
                        }
                    }
                    
                    /* Todo: Measure if penalty occurs before or after waiting for the fetcher. */
                    if (gb->extra_penalty_for_sprite_at_0 != 0) {
                        if (gb->obj_comparators[gb->n_visible_objs - 1] == 0) {
                            gb->cycles_for_line += gb->extra_penalty_for_sprite_at_0;
                            GB_SLEEP(gb, display, 28, gb->extra_penalty_for_sprite_at_0);
                            gb->extra_penalty_for_sprite_at_0 = 0;
                            if (gb->object_fetch_aborted) {
                                goto abort_fetching_object;
                            }
                        }
                    }
                    
                    /* TODO: Can this be deleted?  { */
                    advance_fetcher_state_machine(gb);
                    gb->cycles_for_line++;
                    GB_SLEEP(gb, display, 41, 1);
                    if (gb->object_fetch_aborted) {
                        goto abort_fetching_object;
                    }
                    /* } */
                    
                    advance_fetcher_state_machine(gb);
                    
                    gb->cycles_for_line += 3;
                    GB_SLEEP(gb, display, 20, 3);
                    if (gb->object_fetch_aborted) {
                        goto abort_fetching_object;
                    }
                    
                    gb->object_low_line_address = get_object_line_address(gb, &objects[gb->visible_objs[gb->n_visible_objs - 1]]);
                    
                    gb->cycles_for_line++;
                    GB_SLEEP(gb, display, 39, 1);
                    if (gb->object_fetch_aborted) {
                        goto abort_fetching_object;
                    }
                    
                    gb->during_object_fetch = false;
                    gb->cycles_for_line++;
                    GB_SLEEP(gb, display, 40, 1);

                    const GB_object_t *object = &objects[gb->visible_objs[gb->n_visible_objs - 1]];
                    
                    uint16_t line_address = get_object_line_address(gb, object);
                    
                    uint8_t palette = (object->flags & 0x10) ? 1 : 0;
                    if (gb->cgb_mode) {
                        palette = object->flags & 0x7;
                    }
                    fifo_overlay_object_row(&gb->oam_fifo,
                                            gb->vram_ppu_blocked? 0xFF : gb->vram[gb->object_low_line_address],
                                            gb->vram_ppu_blocked? 0xFF : gb->vram[line_address + 1],
                                            palette,
                                            object->flags & 0x80,
                                            gb->object_priority == GB_OBJECT_PRIORITY_INDEX? gb->visible_objs[gb->n_visible_objs - 1] : 0,
                                            object->flags & 0x20);
                    
                    gb->n_visible_objs--;
                }
                
abort_fetching_object:
                gb->object_fetch_aborted = false;
                gb->during_object_fetch = false;
                
                render_pixel_if_possible(gb);
                advance_fetcher_state_machine(gb);

                if (gb->position_in_line == 160) break;
                gb->cycles_for_line++;
                GB_SLEEP(gb, display, 21, 1);
            }
            
            while (gb->lcd_x != 160 && !gb->disable_rendering && gb->screen && !gb->sgb) {
                /* Oh no! The PPU and LCD desynced! Fill the rest of the line whith white. */
                uint32_t *dest = NULL;
                if (gb->border_mode != GB_BORDER_ALWAYS) {
                    dest = gb->screen + gb->lcd_x + gb->current_line * WIDTH;
                }
                else {
                    dest = gb->screen + gb->lcd_x + gb->current_line * BORDERED_WIDTH + (BORDERED_WIDTH - WIDTH) / 2 + (BORDERED_HEIGHT - LINES) / 2 * BORDERED_WIDTH;
                }
                *dest = gb->background_palettes_rgb[0];
                gb->lcd_x++;

            }
            
            /* TODO: Verify timing */
            if (!GB_is_cgb(gb) && gb->wy_triggered && (gb->io_registers[GB_IO_LCDC] & 0x20) && gb->io_registers[GB_IO_WX] == 166) {
                gb->wx166_glitch = true;
            }
            else {
                gb->wx166_glitch = false;
            }
            gb->wx_triggered = false;
            
            if (GB_is_cgb(gb) && gb->model <= GB_MODEL_CGB_C) {
                gb->cycles_for_line++;
                GB_SLEEP(gb, display, 30, 1);
            }
            
            if (!gb->cgb_double_speed) {
                gb->io_registers[GB_IO_STAT] &= ~3;
                gb->mode_for_interrupt = 0;
                gb->oam_read_blocked = false;
                gb->vram_read_blocked = false;
                gb->oam_write_blocked = false;
                gb->vram_write_blocked = false;
            }
            
            gb->cycles_for_line++;
            GB_SLEEP(gb, display, 22, 1);
            
            gb->io_registers[GB_IO_STAT] &= ~3;
            gb->mode_for_interrupt = 0;
            gb->oam_read_blocked = false;
            gb->vram_read_blocked = false;
            gb->oam_write_blocked = false;
            gb->vram_write_blocked = false;
            GB_STAT_update(gb);

            /* Todo: Measure this value */
            gb->cycles_for_line += 2;
            GB_SLEEP(gb, display, 33, 2);
            gb->cgb_palettes_blocked = !gb->cgb_double_speed;
            
            gb->cycles_for_line += 2;
            GB_SLEEP(gb, display, 36, 2);
            gb->cgb_palettes_blocked = false;
            
            gb->cycles_for_line += 8;
            GB_SLEEP(gb, display, 25, 8);
            
            if (gb->hdma_on_hblank) {
                gb->hdma_starting = true;
            }
            GB_SLEEP(gb, display, 11, LINE_LENGTH - gb->cycles_for_line);
            gb->mode_for_interrupt = 2;
          
            // Todo: unverified timing
            gb->current_lcd_line++;
            if (gb->current_lcd_line == LINES && GB_is_sgb(gb)) {
                display_vblank(gb);
            }
            
            if (gb->icd_hreset_callback) {
                gb->icd_hreset_callback(gb);
            }
        }
        gb->wx166_glitch = false;
        /* Lines 144 - 152 */
        for (; gb->current_line < VIRTUAL_LINES - 1; gb->current_line++) {
            gb->io_registers[GB_IO_LY] = gb->current_line;
            gb->ly_for_comparison = -1;
            GB_SLEEP(gb, display, 26, 2);
            if (gb->current_line == LINES) {
                gb->mode_for_interrupt = 2;
            }
            GB_STAT_update(gb);
            GB_SLEEP(gb, display, 12, 2);
            gb->ly_for_comparison = gb->current_line;
            
            if (gb->current_line == LINES) {
                /* Entering VBlank state triggers the OAM interrupt */
                gb->io_registers[GB_IO_STAT] &= ~3;
                gb->io_registers[GB_IO_STAT] |= 1;
                gb->io_registers[GB_IO_IF] |= 1;
                gb->mode_for_interrupt = 2;
                GB_STAT_update(gb);
                gb->mode_for_interrupt = 1;
                GB_STAT_update(gb);
                
                if (gb->frame_skip_state == GB_FRAMESKIP_LCD_TURNED_ON) {
                    if (GB_is_cgb(gb)) {
                        GB_timing_sync(gb);
                        gb->frame_skip_state = GB_FRAMESKIP_FIRST_FRAME_SKIPPED;
                    }
                    else {
                        if (!GB_is_sgb(gb) || gb->current_lcd_line < LINES) {
                            gb->is_odd_frame ^= true;
                            display_vblank(gb);
                        }
                        gb->frame_skip_state = GB_FRAMESKIP_SECOND_FRAME_RENDERED;
                    }
                }
                else {
                    if (!GB_is_sgb(gb) || gb->current_lcd_line < LINES) {
                        gb->is_odd_frame ^= true;
                        display_vblank(gb);
                    }
                    if (gb->frame_skip_state == GB_FRAMESKIP_FIRST_FRAME_SKIPPED) {
                        gb->cgb_repeated_a_frame = true;
                        gb->frame_skip_state = GB_FRAMESKIP_SECOND_FRAME_RENDERED;
                    }
                    else {
                        gb->cgb_repeated_a_frame = false;
                    }
                }
            }
            
            GB_STAT_update(gb);
            GB_SLEEP(gb, display, 13, LINE_LENGTH - 4);
        }
        
        /* TODO: Verified on SGB2 and CGB-E. Actual interrupt timings not tested. */
        /* Lines 153 */
        gb->io_registers[GB_IO_LY] = 153;
        gb->ly_for_comparison = -1;
        GB_STAT_update(gb);
        GB_SLEEP(gb, display, 14, (gb->model > GB_MODEL_CGB_C)? 4: 6);
        
        if (!GB_is_cgb(gb)) {
            gb->io_registers[GB_IO_LY] = 0;
        }
        gb->ly_for_comparison = 153;
        GB_STAT_update(gb);
        GB_SLEEP(gb, display, 15, (gb->model > GB_MODEL_CGB_C)? 4: 2);
        
        gb->io_registers[GB_IO_LY] = 0;
        gb->ly_for_comparison = (gb->model > GB_MODEL_CGB_C)? 153 : -1;
        GB_STAT_update(gb);
        GB_SLEEP(gb, display, 16, 4);
        
        gb->ly_for_comparison = 0;
        GB_STAT_update(gb);
        GB_SLEEP(gb, display, 29, 12); /* Writing to LYC during this period on a CGB has side effects */
        GB_SLEEP(gb, display, 17, LINE_LENGTH - 24);
        
        
        gb->current_line = 0;
        /* Todo: verify timings */
        if ((gb->io_registers[GB_IO_LCDC] & 0x20) &&
            (gb->io_registers[GB_IO_WY] == 0)) {
            gb->wy_triggered = true;
        }
        else {
            gb->wy_triggered = false;
        }
        
        // TODO: not the correct timing
        gb->current_lcd_line = 0;
        if (gb->icd_vreset_callback) {
            gb->icd_vreset_callback(gb);
        }
    }
}

void GB_draw_tileset(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index)
{
    uint32_t none_palette[4];
    uint32_t *palette = NULL;
    
    switch (GB_is_cgb(gb)? palette_type : GB_PALETTE_NONE) {
        default:
        case GB_PALETTE_NONE:
            none_palette[0] = gb->rgb_encode_callback(gb, 0xFF, 0xFF, 0xFF);
            none_palette[1] = gb->rgb_encode_callback(gb, 0xAA, 0xAA, 0xAA);
            none_palette[2] = gb->rgb_encode_callback(gb, 0x55, 0x55, 0x55);
            none_palette[3] = gb->rgb_encode_callback(gb, 0,    0,    0   );
            palette = none_palette;
            break;
        case GB_PALETTE_BACKGROUND:
            palette = gb->background_palettes_rgb + (4 * (palette_index & 7));
            break;
        case GB_PALETTE_OAM:
            palette = gb->sprite_palettes_rgb + (4 * (palette_index & 7));
            break;
    }
    
    for (unsigned y = 0; y < 192; y++) {
        for (unsigned x = 0; x < 256; x++) {
            if (x >= 128 && !GB_is_cgb(gb)) {
                *(dest++) = gb->background_palettes_rgb[0];
                continue;
            }
            uint16_t tile = (x % 128) / 8 + y / 8 * 16;
            uint16_t tile_address = tile * 0x10 + (x >= 128? 0x2000 : 0);
            uint8_t pixel = (((gb->vram[tile_address + (y & 7) * 2    ] >> ((~x)&7)) & 1 ) |
                             ((gb->vram[tile_address + (y & 7) * 2 + 1] >> ((~x)&7)) & 1) << 1);
            
            if (!gb->cgb_mode) {
                if (palette_type == GB_PALETTE_BACKGROUND) {
                    pixel = ((gb->io_registers[GB_IO_BGP] >> (pixel << 1)) & 3);
                }
                else if (!gb->cgb_mode) {
                    if (palette_type == GB_PALETTE_OAM) {
                        pixel = ((gb->io_registers[palette_index == 0? GB_IO_OBP0 : GB_IO_OBP1] >> (pixel << 1)) & 3);
                    }
                }
            }
            
            
            *(dest++) = palette[pixel];
        }
    }
}

void GB_draw_tilemap(GB_gameboy_t *gb, uint32_t *dest, GB_palette_type_t palette_type, uint8_t palette_index, GB_map_type_t map_type, GB_tileset_type_t tileset_type)
{
    uint32_t none_palette[4];
    uint32_t *palette = NULL;
    uint16_t map = 0x1800;
    
    switch (GB_is_cgb(gb)? palette_type : GB_PALETTE_NONE) {
        case GB_PALETTE_NONE:
            none_palette[0] = gb->rgb_encode_callback(gb, 0xFF, 0xFF, 0xFF);
            none_palette[1] = gb->rgb_encode_callback(gb, 0xAA, 0xAA, 0xAA);
            none_palette[2] = gb->rgb_encode_callback(gb, 0x55, 0x55, 0x55);
            none_palette[3] = gb->rgb_encode_callback(gb, 0,    0,    0   );
            palette = none_palette;
            break;
        case GB_PALETTE_BACKGROUND:
            palette = gb->background_palettes_rgb + (4 * (palette_index & 7));
            break;
        case GB_PALETTE_OAM:
            palette = gb->sprite_palettes_rgb + (4 * (palette_index & 7));
            break;
        case GB_PALETTE_AUTO:
            break;
    }
    
    if (map_type == GB_MAP_9C00 || (map_type == GB_MAP_AUTO && gb->io_registers[GB_IO_LCDC] & 0x08)) {
        map = 0x1c00;
    }
    
    if (tileset_type == GB_TILESET_AUTO) {
        tileset_type = (gb->io_registers[GB_IO_LCDC] & 0x10)? GB_TILESET_8800 : GB_TILESET_8000;
    }
    
    for (unsigned y = 0; y < 256; y++) {
        for (unsigned x = 0; x < 256; x++) {
            uint8_t tile = gb->vram[map + x/8 + y/8 * 32];
            uint16_t tile_address;
            uint8_t attributes = 0;
            
            if (tileset_type == GB_TILESET_8800) {
                tile_address = tile * 0x10;
            }
            else {
                tile_address = (int8_t) tile * 0x10 + 0x1000;
            }
            
            if (gb->cgb_mode) {
                attributes = gb->vram[map + x/8 + y/8 * 32 + 0x2000];
            }
            
            if (attributes & 0x8) {
                tile_address += 0x2000;
            }
            
            uint8_t pixel = (((gb->vram[tile_address + (((attributes & 0x40)? ~y : y) & 7) * 2    ] >> (((attributes & 0x20)? x : ~x)&7)) & 1 ) |
                             ((gb->vram[tile_address + (((attributes & 0x40)? ~y : y) & 7) * 2 + 1] >> (((attributes & 0x20)? x : ~x)&7)) & 1) << 1);
            
            if (!gb->cgb_mode && (palette_type == GB_PALETTE_BACKGROUND || palette_type == GB_PALETTE_AUTO)) {
                pixel = ((gb->io_registers[GB_IO_BGP] >> (pixel << 1)) & 3);
            }
            
            if (palette) {
                *(dest++) = palette[pixel];
            }
            else {
                *(dest++) = gb->background_palettes_rgb[(attributes & 7) * 4 + pixel];
            }
        }
    }
}

uint8_t GB_get_oam_info(GB_gameboy_t *gb, GB_oam_info_t *dest, uint8_t *sprite_height)
{
    uint8_t count = 0;
    *sprite_height = (gb->io_registers[GB_IO_LCDC] & 4) ? 16:8;
    uint8_t oam_to_dest_index[40] = {0,};
    for (unsigned y = 0; y < LINES; y++) {
        GB_object_t *sprite = (GB_object_t *) &gb->oam;
        uint8_t sprites_in_line = 0;
        for (uint8_t i = 0; i < 40; i++, sprite++) {
            signed sprite_y = sprite->y - 16;
            bool obscured = false;
            // Is sprite not in this line?
            if (sprite_y > y || sprite_y + *sprite_height <= y) continue;
            if (++sprites_in_line == 11) obscured = true;
            
            GB_oam_info_t *info = NULL;
            if (!oam_to_dest_index[i]) {
                info = dest + count;
                oam_to_dest_index[i] = ++count;
                info->x = sprite->x;
                info->y = sprite->y;
                info->tile = *sprite_height == 16? sprite->tile & 0xFE : sprite->tile;
                info->flags = sprite->flags;
                info->obscured_by_line_limit = false;
                info->oam_addr = 0xFE00 + i * sizeof(*sprite);
            }
            else {
                info = dest + oam_to_dest_index[i] - 1;
            }
            info->obscured_by_line_limit |= obscured;
        }
    }
    
    for (unsigned i = 0; i < count; i++) {
        uint16_t vram_address = dest[i].tile * 0x10;
        uint8_t flags = dest[i].flags;
        uint8_t palette = gb->cgb_mode? (flags & 7) : ((flags & 0x10)? 1 : 0);
        if (GB_is_cgb(gb) && (flags & 0x8)) {
            vram_address += 0x2000;
        }

        for (unsigned y = 0; y < *sprite_height; y++) {
            UNROLL
            for (unsigned x = 0; x < 8; x++) {
                uint8_t color = (((gb->vram[vram_address    ] >> ((~x)&7)) & 1 ) |
                                 ((gb->vram[vram_address + 1] >> ((~x)&7)) & 1) << 1 );
                
                if (!gb->cgb_mode) {
                    color = (gb->io_registers[palette? GB_IO_OBP1:GB_IO_OBP0] >> (color << 1)) & 3;
                }
                dest[i].image[((flags & 0x20)?7-x:x) + ((flags & 0x40)?*sprite_height - 1 -y:y) * 8] = gb->sprite_palettes_rgb[palette * 4 + color];
            }
            vram_address += 2;
        }
    }
    return count;
}


bool GB_is_odd_frame(GB_gameboy_t *gb)
{
    return gb->is_odd_frame;
}
