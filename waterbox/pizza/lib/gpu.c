/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#include <signal.h>
#include <string.h>
#include <strings.h>
#include <time.h>

#include "cycles.h"
#include "gameboy.h"
#include "global.h"
#include "gpu.h"
#include "interrupt.h"
#include "mmu.h"
#include "utils.h"

/* Gameboy OAM 4 bytes data */
typedef struct gpu_oam_s
{
    uint8_t y;
    uint8_t x;
    uint8_t pattern;

    uint8_t palette_cgb:3;
    uint8_t vram_bank:1;
    uint8_t palette:1;
    uint8_t x_flip:1;
    uint8_t y_flip:1;
    uint8_t priority:1;

} gpu_oam_t;

/* Gameboy Color additional tile attributes */
typedef struct gpu_cgb_bg_tile_s
{
    uint8_t palette:3;
    uint8_t vram_bank:1;
    uint8_t spare:1;
    uint8_t x_flip:1;
    uint8_t y_flip:1;
    uint8_t priority:1;

} gpu_cgb_bg_tile_t;

/* ordered sprite list */
typedef struct oam_list_s
{
    int idx;
    struct oam_list_s *next;
} oam_list_t;

/* pointer to interrupt flags (handy) */
interrupts_flags_t *gpu_if;

/* internal functions prototypes */
void gpu_draw_sprite_line(gpu_oam_t *oam, 
                          uint8_t sprites_size,
                          uint8_t line);
void gpu_draw_window_line(int tile_idx, uint8_t frame_x,
                          uint8_t frame_y, uint8_t line);

/* 2 bit to 8 bit color lookup */
static uint16_t gpu_color_lookup[] = { 0xFFFF, 0xAD55, 0x52AA, 0x0000 };

/* function to call when frame is ready */
gpu_frame_ready_cb_t gpu_frame_ready_cb;

/* global state of GPU */
gpu_t gpu;


void gpu_dump_oam()
{
    /* make it point to the first OAM object */
    gpu_oam_t *oam = (gpu_oam_t *) mmu_addr(0xFE00);

    int i;

    for (i=0; i<40; i++)
    {
        if (oam[i].x != 0 && oam[i].y != 0)
            printf("OAM X %d Y %d VRAM %d PATTERN %d\n", oam[i].x, oam[i].y,
                                                         oam[i].vram_bank,
                                                         oam[i].pattern);
    }
}


/* init pointers */
void gpu_init_pointers()
{
    /* make gpu field points to the related memory area */
    gpu.lcd_ctrl   = mmu_addr(0xFF40);
    gpu.lcd_status = mmu_addr(0xFF41);
    gpu.scroll_y   = mmu_addr(0xFF42);
    gpu.scroll_x   = mmu_addr(0xFF43);
    gpu.window_y   = mmu_addr(0xFF4A);
    gpu.window_x   = mmu_addr(0xFF4B);
    gpu.ly         = mmu_addr(0xFF44);
    gpu.lyc        = mmu_addr(0xFF45);
    gpu_if         = mmu_addr(0xFF0F);
}

/* reset */
void gpu_reset()
{
    /* init counters */
    gpu.next = 456 << global_cpu_double_speed;
    gpu.frame_counter = 0;

}

/* init GPU states */
void gpu_init(gpu_frame_ready_cb_t cb)
{
    /* reset gpu structure */
    bzero(&gpu, sizeof(gpu_t));

    /* init memory pointers */
    gpu_init_pointers();

    /* init counters */
    gpu.next = 456 << global_cpu_double_speed;
    gpu.frame_counter = 0;

    /* step for normal CPU speed */
    gpu.step = 4;

    /* init palette */
    memcpy(gpu.bg_palette, gpu_color_lookup, sizeof(uint16_t) * 4);
    memcpy(gpu.obj_palette_0, gpu_color_lookup, sizeof(uint16_t) * 4);
    memcpy(gpu.obj_palette_1, gpu_color_lookup, sizeof(uint16_t) * 4);

    /* set callback */
    gpu_frame_ready_cb = cb;
}

/* turn on/off lcd */
void gpu_toggle(uint8_t state)
{
    /* from off to on */
    if (state & 0x80)
    {
        /* LCD turned on */
        gpu.next = cycles.cnt + (456 << global_cpu_double_speed);
        *gpu.ly  = 0;
        (*gpu.lcd_status).mode = 0x00;
        (*gpu.lcd_status).ly_coincidence = 0x00;
    }
    else
    {
        /* LCD turned off - reset stuff */
        gpu.next = cycles.cnt - 1; //  + (80 << global_cpu_double_speed);
        *gpu.ly = 0;
        (*gpu.lcd_status).mode = 0x00;
    }
} 

/* push frame on screen */
void gpu_draw_frame()
{
    /* increase frame counter */
    gpu.frame_counter++;

    /* is it the case to push samples? */
    /*if ((global_emulation_speed == GLOBAL_EMULATION_SPEED_DOUBLE &&
        (gpu.frame_counter & 0x0001) != 0) ||
        (global_emulation_speed == GLOBAL_EMULATION_SPEED_4X &&
        (gpu.frame_counter & 0x0003) != 0))
        return;*/

    uint_fast32_t i,r,g,b,r2,g2,b2,res;

    /* simulate shitty gameboy response time of LCD                 */
    /* by calculating an average between current and previous frame */
    //for (i=0; i<(144*160); i++)
    for (i=0; i<(144 * 160); i++)
    {
/*        r = gpu.frame_buffer[i] & 0x1F;
        g = gpu.frame_buffer[i] >> 5 & 0x3F;
        b = gpu.frame_buffer[i] >> 11 & 0x1F;

        r2 = gpu.frame_buffer_prev[i] & 0x1F;
        g2 = gpu.frame_buffer_prev[i] >> 5 & 0x3F;
        b2 = gpu.frame_buffer_prev[i] >> 11 & 0x1F;

        gpu.frame_buffer_prev[i] = gpu.frame_buffer[i];

        gpu.frame_buffer[i] = (uint16_t) ((r + r2) >> 1) |
                              (((g + g2) >> 1) << 5) |
                              (((b + b2) >> 1) << 11);*/

        r = gpu.frame_buffer[i] & 0x001F;
        g = gpu.frame_buffer[i] & 0x07E0;
        b = gpu.frame_buffer[i] & 0xF800;

        r2 = gpu.frame_buffer_prev[i] & 0x001F;
        g2 = gpu.frame_buffer_prev[i] & 0x07E0;
        b2 = gpu.frame_buffer_prev[i] & 0xF800;

        // gpu.frame_buffer_prev[i] = gpu.frame_buffer[i];

        res = ((r + r2) >> 1) |
              (((g + g2) >> 1) & 0x07E0) |
              (((b + b2) >> 1) & 0xF800);

        gpu.frame_buffer_prev[i] = gpu.frame_buffer[i];
        gpu.frame_buffer[i] = res;
    } 
       
    /* call the callback */
    if (gpu_frame_ready_cb)
        (*gpu_frame_ready_cb) ();

    /* reset priority matrix */
    bzero(gpu.priority, 160 * 144);
    bzero(gpu.palette_idx, 160 * 144);

    return;
}

/* get pointer to frame buffer */
uint16_t *gpu_get_frame_buffer()
{
    return gpu.frame_buffer;
}

/* draw a single line */
void gpu_draw_line(uint8_t line)
{
    /* avoid mess */
    if (line > 144)
        return;

    /* is it the case to push samples? */
    /*if ((global_emulation_speed == GLOBAL_EMULATION_SPEED_DOUBLE &&
        (gpu.frame_counter & 0x0001) != 0) ||
        (global_emulation_speed == GLOBAL_EMULATION_SPEED_4X &&
        (gpu.frame_counter & 0x0003) != 0))
        return;*/

    int i, t, y, px_start, px_drawn;
    uint8_t *tiles_map, tile_subline, palette_idx, x_flip, priority;
    uint16_t tiles_addr, tile_n, tile_idx, tile_line;
    uint16_t tile_y;
    
    /* gotta show BG? Answer is always YES in case of Gameboy Color */
    if ((*gpu.lcd_ctrl).bg || global_cgb)
    {
        gpu_cgb_bg_tile_t *tiles_map_cgb = NULL;
        uint8_t *tiles = NULL; 
        uint16_t *palette;

        if (global_cgb)
        {
            /* CGB tile map into VRAM0 */
            tiles_map = mmu_addr_vram0() + ((*gpu.lcd_ctrl).bg_tiles_map ?
                                  0x1C00 : 0x1800);

            /* additional attribute table is into VRAM1 */
            tiles_map_cgb = mmu_addr_vram1() + ((*gpu.lcd_ctrl).bg_tiles_map ?
                                  0x1C00 : 0x1800);
        }
        else
        {
            /* never flip */
            x_flip = 0;

            /* get tile map offset */
            tiles_map = mmu_addr((*gpu.lcd_ctrl).bg_tiles_map ? 
                                 0x9C00 : 0x9800);

            if ((*gpu.lcd_ctrl).bg_tiles)
                 tiles_addr = 0x8000;
            else
                 tiles_addr = 0x9000;

            /* get absolute address of tiles area */
            tiles = mmu_addr(tiles_addr);

            /* monochrome GB uses a single BG palette */
            palette = gpu.bg_palette; 

            /* always priority = 0 */
            priority = 0;
        }

        /* calc tile y */
        tile_y = (*(gpu.scroll_y) + line) & 0xFF;

        /* calc first tile idx */
        tile_idx = ((tile_y >> 3) * 32) + (*(gpu.scroll_x) / 8);

        /* tile line because if we reach the end of the line,   */
        /* we have to rewind to the first tile of the same line */     
        tile_line = ((tile_y >> 3) * 32); 
  
        /* calc first pixel of frame buffer of the current line */
        uint_fast16_t pos_fb = line * 160;
        uint_fast16_t pos;
 
        /* calc tile subline */
        tile_subline = tile_y % 8;

        /* walk through different tiles */
        for (t=0; t<21; t++)
        {
            /* resolv tile data memory area */ 
            if ((*gpu.lcd_ctrl).bg_tiles == 0)
                tile_n = (int8_t) tiles_map[tile_idx];
            else
                tile_n = (tiles_map[tile_idx] & 0x00FF);

            /* if color gameboy, resolv which palette is bound */
            if (global_cgb)
            {
                /* extract palette index (0-31) */
                palette_idx = tiles_map_cgb[tile_idx].palette;

                /* get palette pointer to 4 (16bit) colors */
                palette = 
                    (uint16_t *) &gpu.cgb_palette_bg_rgb565[palette_idx * 4];

                /* get priority of the tile */
                priority = tiles_map_cgb[tile_idx].priority;

                if (tiles_map_cgb[tile_idx].vram_bank)
                    tiles = mmu_addr_vram1() +
                            ((*gpu.lcd_ctrl).bg_tiles ? 0x0000 : 0x1000);
                else
                    tiles = mmu_addr_vram0() +
                            ((*gpu.lcd_ctrl).bg_tiles ? 0x0000 : 0x1000);

                /* calc subline in case of flip_y */
                if (tiles_map_cgb[tile_idx].y_flip)
                    tile_subline = 7 - (tile_y % 8);
                else
                    tile_subline = tile_y % 8;

                /* save x_flip */
                x_flip = tiles_map_cgb[tile_idx].x_flip;
            }

            /* calc tile data pointer */
            int16_t tile_ptr = (tile_n * 16) + (tile_subline * 2);

            /* pixels are handled in a super shitty way                  */
            /* bit 0 of the pixel is taken from even position tile bytes */
            /* bit 1 of the pixel is taken from odd position tile bytes  */

            uint8_t  pxa[8];
            uint8_t  shft;
            uint8_t  b1 = *(tiles + tile_ptr);
            uint8_t  b2 = *(tiles + tile_ptr + 1);

            for (y=0; y<8; y++)
            {
                 if (x_flip)
                     shft = (1 << (7 - y));
                 else
                     shft = (1 << y);

                 pxa[y] = ((b1 & shft) ? 1 : 0) |
                          ((b2 & shft) ? 2 : 0);
            }

            /* particular cases for first and last tile */ 
            /* (could be shown just a part)             */
            if (t == 0)
            {
                px_start = (*(gpu.scroll_x) % 8);

                px_drawn = 8 - px_start;

                /* set n pixels */
                for (i=0; i<px_drawn; i++)
                {
                    pos = pos_fb + (px_drawn - i - 1);
                   
                    gpu.priority[pos] = priority;
                    gpu.palette_idx[pos] = pxa[i];
                    gpu.frame_buffer[pos] = palette[pxa[i]];
                }
            }
            else if (t == 20)
            {
                px_drawn = *(gpu.scroll_x) % 8;

                /* set n pixels */
                for (i=0; i<px_drawn; i++)
                {
                    pos = pos_fb + (px_drawn - i - 1);

                    gpu.priority[pos] = priority;
                    gpu.palette_idx[pos] = pxa[i];
                    gpu.frame_buffer[pos] = palette[pxa[i + (8 - px_drawn)]];
                }
            } 
            else
            {
                /* set 8 pixels */
                for (i=0; i<8; i++)
                {
                    pos = pos_fb + (7 - i);

                    gpu.priority[pos] = priority;
                    gpu.palette_idx[pos] = pxa[i];
                    gpu.frame_buffer[pos] = palette[pxa[i]];
                }

                px_drawn = 8;
            }

            /* go to the next tile and rewind in case we reached the 32th */
            tile_idx++;

            /* don't go to the next line, just rewind */
            if (tile_idx == (tile_line + 32))
                tile_idx = tile_line;

            /* go to the next block of 8 pixels of the frame buffer */
            pos_fb += px_drawn;
        }
    }

    /* gotta show sprites? */
    if ((*gpu.lcd_ctrl).sprites)
    {
        /* make it point to the first OAM object */
        gpu_oam_t *oam = (gpu_oam_t *) mmu_addr(0xFE00);

        /* calc sprite height */
        uint8_t h = ((*gpu.lcd_ctrl).sprites_size + 1) * 8;

        int sort[40];
        int j = 0;

        /* prepare sorted list of oams */        
        for (i=0; i<40; i++)
            sort[i] = -1;

        for (i=0; i<40; i++)
        {
            /* the sprite intersects the current line? */
            if (oam[i].x != 0 && oam[i].y != 0 &&
                oam[i].x < 168 && oam[i].y < 160 &&
                line < (oam[i].y + h - 16) &&
                line >= (oam[i].y - 16))
            {
                /* color GB uses memory position as priority criteria */
                if (global_cgb)
                {
                    sort[j++] = i; 
                    continue;
                }
                    
                /* find its position on sort array */
                for (j=0; j<40; j++)
                {
                    if (sort[j] == -1)
                    {
                        sort[j] = i;
                        break;
                    }

                    if (global_cgb)
                        continue;

                    if ((oam[i].y < oam[sort[j]].y) ||
                        ((oam[i].y == oam[sort[j]].y) &&
                         (oam[i].x < oam[sort[j]].x)))
                    {
                        int z;

                        for (z=40; z>j; z--)
                            sort[z] = sort[z-1];

                        sort[j] = i;
                        break;
                    }
                } 
            }                  
        } 

        /* draw ordered sprite list */
        for (i=0; i<40 && sort[i] != -1; i++)
            gpu_draw_sprite_line(&oam[sort[i]], 
                                 (*gpu.lcd_ctrl).sprites_size, line);
        
    }

    /* wanna show window? */
    if (global_window && (*gpu.lcd_ctrl).window)
    {
        /* at least the current line is covering the window area? */
        if (line < *(gpu.window_y))
            return;

        /* TODO - reset this in a better place */ 
        if (line == *(gpu.window_y))
            gpu.window_skipped_lines = 0;

        int z, first_z;
        uint8_t tile_pos_x, tile_pos_y;

        /* gotta draw a window? check if it is inside screen coordinates */
        if (*(gpu.window_y) >= 144 ||
            *(gpu.window_x) >= 160)
        {
            gpu.window_skipped_lines++;
            return; 
        }

        /* calc the first interesting tile */
        first_z = ((line - *(gpu.window_y) - 
                    gpu.window_skipped_lines) >> 3) << 5;

        for (z=first_z; z<first_z + 21; z++)
        {
            /* calc tile coordinates on frame buffer */
            tile_pos_x = ((z & 0x1F) << 3) + *(gpu.window_x) - 7;
            tile_pos_y = ((z >> 5) << 3) + *(gpu.window_y) + 
                         gpu.window_skipped_lines;

            /* gone over the current line? */
            if (tile_pos_y > line)
                break;

            if (tile_pos_y < (line - 7))
                continue;
                
            /* gone over the screen visible X? */
            /* being between last column and first one is valid */
            if (tile_pos_x >= 160 && tile_pos_x < 248)
                break;

            /* gone over the screen visible section? stop it */
            if (tile_pos_y >= 144) // || (tile_pos_x >= 160))
                break;

            /* put tile on frame buffer */
            gpu_draw_window_line(z, (uint8_t) tile_pos_x, 
                                    (uint8_t) tile_pos_y, line);
        }
    }
}



/* draw a tile in x,y coordinates */
void gpu_draw_window_line(int tile_idx, uint8_t frame_x, 
                          uint8_t frame_y, uint8_t line)
{
    int i, p, y, pos;
    int16_t tile_n;
    uint8_t *tiles_map;
    gpu_cgb_bg_tile_t *tiles_map_cgb = NULL;
    uint8_t *tiles, x_flip;
    uint16_t *palette;

    if (global_cgb)
    {
        /* CGB tile map into VRAM0 */
        tiles_map = mmu_addr_vram0() + ((*gpu.lcd_ctrl).window_tiles_map ?
                              0x1C00 : 0x1800);

        /* additional attribute table is into VRAM1 */
        tiles_map_cgb = mmu_addr_vram1() + ((*gpu.lcd_ctrl).window_tiles_map ?
                              0x1C00 : 0x1800);

        /* get palette index */
        uint8_t palette_idx = tiles_map_cgb[tile_idx].palette;
        x_flip = tiles_map_cgb[tile_idx].x_flip;

        /* get palette pointer to 4 (16bit) colors */
        palette = (uint16_t *) &gpu.cgb_palette_bg_rgb565[palette_idx * 4];

        /* attribute table will tell us where is the tile */
        if (tiles_map_cgb[tile_idx].vram_bank)
            tiles = mmu_addr_vram1() + 
                    ((*gpu.lcd_ctrl).bg_tiles ? 0x0000 : 0x1000);
        else
            tiles = mmu_addr_vram0() + 
                    ((*gpu.lcd_ctrl).bg_tiles ? 0x0000 : 0x1000);
           
    }
    else
    {
        /* get tile map offset */
        tiles_map = mmu_addr((*gpu.lcd_ctrl).window_tiles_map ?
                             0x9C00 : 0x9800);

        /* get tile offset */
        if ((*gpu.lcd_ctrl).bg_tiles)
            tiles = mmu_addr(0x8000);
        else
            tiles = mmu_addr(0x9000);

        /* monochrome GB uses a single BG palette */
        palette = gpu.bg_palette;

        /* never flip */
        x_flip = 0;
    }

    /* obtain tile number */
    if ((*gpu.lcd_ctrl).bg_tiles == 0)
        tile_n = (int8_t) tiles_map[tile_idx];
    else
        tile_n = (tiles_map[tile_idx] & 0x00ff);

    /* calc vertical offset INSIDE the tile */
    p = (line - frame_y) * 2; 

    /* calc frame position buffer for 4 pixels */
    uint32_t pos_fb = (line * 160); 

    /* calc tile pointer */
    int16_t tile_ptr = (tile_n * 16) + p;

    /* pixels are handled in a super shitty way */
    /* bit 0 of the pixel is taken from even position tile bytes */
    /* bit 1 of the pixel is taken from odd position tile bytes */

    uint8_t  pxa[8];
    uint8_t  shft;

    for (y=0; y<8; y++)
    {
         //uint8_t shft = (1 << y);

         if (x_flip)
             shft = (1 << (7 - y));
         else
             shft = (1 << y);

         pxa[y] = ((*(tiles + tile_ptr) & shft) ? 1 : 0) |
                  ((*(tiles + tile_ptr + 1) & shft) ? 2 : 0);
    }

    /* set 8 pixels (full tile line) */
    for (i=0; i<8; i++)
    {
        /* over the last column? */
        uint8_t x = frame_x + (7 - i);

        if (x > 159)
            continue;

        /* calc position on frame buffer */
        pos = pos_fb + x; 

        /* can overwrite sprites? depends on pixel priority */
        if (gpu.priority[pos] != 0x02)
            gpu.frame_buffer[pos] = palette[pxa[i]];
    }
}

/* draw a sprite tile in x,y coordinates */
void gpu_draw_sprite_line(gpu_oam_t *oam, uint8_t sprites_size, uint8_t line)
{
    int_fast32_t x, y, pos, fb_x, off;
    uint_fast16_t p, i, j;
    uint8_t  sprite_bytes;
    int16_t  tile_ptr;
    uint16_t *palette;
    uint8_t *tiles;

    /* REMEMBER! position of sprites is relative to the visible screen area */
    /* ... and y is shifted by 16 pixels, x by 8                            */
    y = oam->y - 16;
    x = oam->x - 8;
  
    if (x < -7)
        return;

    /* first pixel on frame buffer position */
    uint32_t tile_pos_fb = (y * 160) + x;

    /* choose palette */
    if (global_cgb)
    {
         uint8_t palette_idx = oam->palette_cgb;

         /* get palette pointer to 4 (16bit) colors */
         palette = (uint16_t *) &gpu.cgb_palette_oam_rgb565[palette_idx * 4];
   
         /* tiles are into vram0 */
         if (oam->vram_bank)
             tiles = mmu_addr_vram1();
         else
             tiles = mmu_addr_vram0();
    }
    else
    {
        /* tiles are int fixed 0x8000 address */
        tiles = mmu_addr(0x8000);

        if (oam->palette)
            palette = gpu.obj_palette_1;
        else
            palette = gpu.obj_palette_0;
    }

    /* calc sprite in byte */
    sprite_bytes = 16 * (sprites_size + 1);

    /* walk through 8x8 pixels (2bit per pixel -> 4 pixels per byte) */
    /* 1 line is 8 pixels -> 2 bytes per line                        */
    for (p=0; p<sprite_bytes; p+=2)
    {
        uint8_t tile_y = p / 2;

        if (tile_y + y != line)
            continue;

        /* calc frame position buffer for 4 pixels */
        uint32_t pos_fb = (tile_pos_fb + (tile_y * 160)) & 0xFFFF; //% 65536; 

        /* calc tile pointer */
        if (oam->y_flip)
             tile_ptr = (oam->pattern * 16) + (sprite_bytes - p - 2);
        else
             tile_ptr = (oam->pattern * 16) + p;

        /* pixels are handled in a super shitty way */
        /* bit 0 of the pixel is taken from even position tile bytes */
        /* bit 1 of the pixel is taken from odd position tile bytes */

        uint8_t  pxa[8];

        for (j=0; j<8; j++)
        {
             uint8_t shft = (1 << j);

             pxa[j] = ((*(tiles + tile_ptr) & shft) ? 1 : 0) |
                      ((*(tiles + tile_ptr + 1) & shft) ? 2 : 0);
        }

        /* set 8 pixels (full tile line) */
        for (i=0; i<8; i++)
        {
            if (oam->x_flip)
                off = i;
            else
                off = 7 - i; 

            /* is it on screen? */
            fb_x = x + off;

            if (fb_x < 0 || fb_x > 160)
                continue;

            /* set serial position on frame buffer */
            pos = pos_fb + off;

            /* is it inside the screen? */
            if (pos >= 144 * 160 || pos < 0)
                continue;

            if (global_cgb)
            {
                /* sprite color 0 = transparent */
                if (pxa[i] != 0x00) 
                {
                    /* flag clr = sprites always on top of bg and window */
                    if ((*gpu.lcd_ctrl).bg == 0)
                    {
                        gpu.frame_buffer[pos] = palette[pxa[i]];
                        gpu.priority[pos] = 0x02; 
                    } 
                    else 
                    {
                        if (((gpu.priority[pos] == 0) &&
                            (oam->priority == 0 ||
                            (oam->priority == 1 &&
                             gpu.palette_idx[pos] == 0x00))) ||
                            (gpu.priority[pos] == 1 &&
                             gpu.palette_idx[pos] == 0x00))
                        {
                            gpu.frame_buffer[pos] = palette[pxa[i]];
                            gpu.priority[pos] = (oam->priority ? 0x00 : 0x02);
                        }
                    }
                }
            }
            else
            {
                /* push on screen pixels not set to zero (transparent) */
                /* and if the priority is set to one, overwrite just   */
                /* bg pixels set to zero                               */
                if ((pxa[i] != 0x00) &&
                    (oam->priority == 0 || 
                    (oam->priority == 1 && 
                     gpu.frame_buffer[pos] == gpu.bg_palette[0x00])))
                {
                    gpu.frame_buffer[pos] = palette[pxa[i]];
                    gpu.priority[pos] = (oam->priority ? 0x00 : 0x02);
                }
            }
        }
    }
}

/* update GPU internal state given CPU T-states */
void gpu_step()
{
    char ly_changed = 0;
    char mode_changed = 0;

    /* take different action based on current state */
    switch((*gpu.lcd_status).mode)
    {
        /*
         * during HBLANK (CPU can access VRAM)
         */
        case 0: 
                /* handle HDMA stuff during hblank */
                cycles_hdma();

                /*
                 * if current line == 143 (and it's about to turn 144)
                 * enter mode 01 (VBLANK)
                 */
                if (*gpu.ly == 143)
                {
                    /* notify mode has changes */
                    mode_changed = 1;

                    (*gpu.lcd_status).mode = 0x01;

                    /* mode one lasts 456 cycles */
                    gpu.next = cycles.cnt + 
                               (456 << global_cpu_double_speed);

                    /* DRAW! TODO */
                    /* CHECK INTERRUPTS! TODO */
                    cycles_vblank();

                    /* set VBLANK interrupt flag */
                    gpu_if->lcd_vblank = 1;

                    /* apply gameshark patches */
                    //mmu_apply_gs();

                    /* and finally push it on screen! */
                    gpu_draw_frame();
                } 
                else
                {
                    /* notify mode has changed */
                    mode_changed = 1;

                    /* enter OAM mode */
                    (*gpu.lcd_status).mode = 0x02;

                    /* mode 2 needs 80 cycles */
                    gpu.next = cycles.cnt + 
                               (80 << global_cpu_double_speed);

                }

                /* notify mode has changed */
                ly_changed = 1;

                /* inc current line */
                (*gpu.ly)++;

//                cycles_hblank(*gpu.ly);

                break;

        /*
         * during VBLANK (CPU can access VRAM)
         */
        case 1: 
                /* notify ly has changed */
                ly_changed = 1;

                /* inc current line */
                (*gpu.ly)++;

                /* reached the bottom? */
                if ((*gpu.ly) > 153)
                {
                    /* go back to line 0 */
                    (*gpu.ly) = 0;

                    /* switch to OAM mode */
                    (*gpu.lcd_status).mode = 0x02;

                    /* */
                    gpu.next = 
                        cycles.cnt + (80 << global_cpu_double_speed);
                }
                else
                    gpu.next = 
                        cycles.cnt + (456 << global_cpu_double_speed);

                break;

        /*
         * during OAM (LCD access FE00-FE90, so CPU cannot)
         */
        case 2: 
                /* reset clock counter */
                gpu.next = 
	                    cycles.cnt + (172 << global_cpu_double_speed);

                /* notify mode has changed */
                mode_changed = 1;

                /* switch to VRAM mode */
                (*gpu.lcd_status).mode = 0x03;

                break;

        /*
         * during VRAM (LCD access both OAM and VRAM, so CPU cannot)
         */
        case 3: 
                /* reset clock counter */
                gpu.next = 
                    cycles.cnt + (204 << global_cpu_double_speed);

                /* notify mode has changed */
                mode_changed = 1;

                /* go back to HBLANK mode */
                (*gpu.lcd_status).mode = 0x00;

                /* draw line */
                gpu_draw_line(*gpu.ly);

                /* notify cycles */
//                cycles_hblank(*gpu.ly);

                //printf("COLLA %d\n", *gpu.ly);

                break;
    }

    /* ly changed? is it the case to trig an interrupt? */
    if (ly_changed)
    {
        /* check if we gotta trigger an interrupt */
        if ((*gpu.ly) == (*gpu.lyc))
        { 
            /* set lcd status flags indicating there's a concidence */
            (*gpu.lcd_status).ly_coincidence = 1;

            /* an interrupt is desiderable? */
            if ((*gpu.lcd_status).ir_ly_coincidence)
                gpu_if->lcd_ctrl = 1;
        }
        else
        {
            /* set lcd status flags indicating there's NOT a concidence */
            (*gpu.lcd_status).ly_coincidence = 0;
        }
    }

    /* mode changed? is is the case to trig an interrupt? */
    if (mode_changed)
    {
        if ((*gpu.lcd_status).mode == 0x00 &&
            (*gpu.lcd_status).ir_mode_00)
            gpu_if->lcd_ctrl = 1;
        else if ((*gpu.lcd_status).mode == 0x01 &&
                 (*gpu.lcd_status).ir_mode_01)
            gpu_if->lcd_ctrl = 1;
        else if ((*gpu.lcd_status).mode == 0x02 &&
                 (*gpu.lcd_status).ir_mode_10)
            gpu_if->lcd_ctrl = 1;
    }
}

uint8_t gpu_read_reg(uint16_t a)
{
    switch (a)
    {
        case 0xFF68:

            return (gpu.cgb_palette_bg_autoinc << 7 | gpu.cgb_palette_bg_idx);

        case 0xFF69:

            if ((gpu.cgb_palette_bg_idx & 0x01) == 0x00)
                return gpu.cgb_palette_bg[gpu.cgb_palette_bg_idx / 2] & 
                       0x00ff;
            else
                return (gpu.cgb_palette_bg[gpu.cgb_palette_bg_idx / 2] & 
                       0xff00) >> 8;

        case 0xFF6A:

            return (gpu.cgb_palette_oam_autoinc << 7 | gpu.cgb_palette_oam_idx);

        case 0xFF6B:

            if ((gpu.cgb_palette_oam_idx & 0x01) == 0x00)
                return gpu.cgb_palette_oam[gpu.cgb_palette_oam_idx / 2] & 
                       0x00ff;
            else
                return (gpu.cgb_palette_oam[gpu.cgb_palette_oam_idx / 2] & 
                       0xff00) >> 8;


    }

    return 0x00;
}

void gpu_write_reg(uint16_t a, uint8_t v)
{
    int i;
    uint8_t r,g,b;

    switch (a)
    {
        case 0xFF47:

            gpu.bg_palette[0] = gpu_color_lookup[v & 0x03]; 
            gpu.bg_palette[1] = gpu_color_lookup[(v & 0x0c) >> 2];
            gpu.bg_palette[2] = gpu_color_lookup[(v & 0x30) >> 4];
            gpu.bg_palette[3] = gpu_color_lookup[(v & 0xc0) >> 6];

            break;

        case 0xFF48:

            gpu.obj_palette_0[0] = gpu_color_lookup[v & 0x03]; 
            gpu.obj_palette_0[1] = gpu_color_lookup[(v & 0x0c) >> 2];
            gpu.obj_palette_0[2] = gpu_color_lookup[(v & 0x30) >> 4];
            gpu.obj_palette_0[3] = gpu_color_lookup[(v & 0xc0) >> 6];

            break;

        case 0xFF49:

            gpu.obj_palette_1[0] = gpu_color_lookup[v & 0x03];
            gpu.obj_palette_1[1] = gpu_color_lookup[(v & 0x0c) >> 2];
            gpu.obj_palette_1[2] = gpu_color_lookup[(v & 0x30) >> 4];
            gpu.obj_palette_1[3] = gpu_color_lookup[(v & 0xc0) >> 6];

            break;

        case 0xFF68:

            gpu.cgb_palette_bg_idx = (v & 0x3f);
            gpu.cgb_palette_bg_autoinc = ((v & 0x80) == 0x80);

            break;

        case 0xFF69:

            i = gpu.cgb_palette_bg_idx / 2;

            if ((gpu.cgb_palette_bg_idx & 0x01) == 0x00)
            {
                gpu.cgb_palette_bg[i] &= 0xff00;
                gpu.cgb_palette_bg[i] |= v;
            }
            else
            {
                gpu.cgb_palette_bg[i] &= 0x00ff;
                gpu.cgb_palette_bg[i] |= (v << 8); 
            }

            r = gpu.cgb_palette_bg[i] & 0x1F;
            g = gpu.cgb_palette_bg[i] >> 5 & 0x1F;
            b = gpu.cgb_palette_bg[i] >> 10 & 0x1F;

   	    gpu.cgb_palette_bg_rgb565[i] = 
                (((r * 13 + g * 2 + b + 8) << 7) & 0xF800) |
                 ((g * 3 + b + 1) >> 1) << 5 |
                 ((r * 3 + g * 2 + b * 11 + 8) >> 4);
 
            if (gpu.cgb_palette_bg_autoinc)
                gpu.cgb_palette_bg_idx = ((gpu.cgb_palette_bg_idx + 1) & 0x3f);

            break;

        case 0xFF6A:

            gpu.cgb_palette_oam_idx = v & 0x3f;
            gpu.cgb_palette_oam_autoinc = ((v & 0x80) == 0x80);

            break;

        case 0xFF6B:

            i = gpu.cgb_palette_oam_idx / 2;

            if ((gpu.cgb_palette_oam_idx & 0x01) == 0x00)
            {
                gpu.cgb_palette_oam[i] &= 0xff00;
                gpu.cgb_palette_oam[i] |= v;
            }
            else
            {
                gpu.cgb_palette_oam[i] &= 0x00ff;
                gpu.cgb_palette_oam[i] |= (v << 8);
            }

            r = gpu.cgb_palette_oam[i] & 0x1F;
            g = gpu.cgb_palette_oam[i] >> 5 & 0x1F;
            b = gpu.cgb_palette_oam[i] >> 10 & 0x1F;

            gpu.cgb_palette_oam_rgb565[i] =
                (((r * 13 + g * 2 + b + 8) << 7) & 0xF800) |
                 ((g * 3 + b + 1) >> 1) << 5 |
                 ((r * 3 + g * 2 + b * 11 + 8) >> 4);

            if (gpu.cgb_palette_oam_autoinc)
                gpu.cgb_palette_oam_idx = 
                    ((gpu.cgb_palette_oam_idx + 1) & 0x3f);

            break;

    }
}

void gpu_set_speed(char speed)
{
    if (speed == 1)
        gpu.step = 2;
    else
        gpu.step = 4;
}
