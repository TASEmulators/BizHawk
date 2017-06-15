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

#ifndef __GPU_HDR__
#define __GPU_HDR__

#include <stdio.h>
#include <stdint.h>

/* callback function */ 
typedef void (*gpu_frame_ready_cb_t) ();

/* prototypes */
void      gpu_dump_oam();
uint16_t *gpu_get_frame_buffer();
void      gpu_init(gpu_frame_ready_cb_t cb);
void      gpu_reset();
void      gpu_set_speed(char speed);
void      gpu_step();
void      gpu_toggle(uint8_t state);
void      gpu_write_reg(uint16_t a, uint8_t v);
uint8_t   gpu_read_reg(uint16_t a);


/* Gameboy LCD Control - R/W accessing 0xFF40 address */
typedef struct gpu_lcd_ctrl_s
{
    uint8_t bg:1;                   /* 0 = BG off, 1 = BG on        */ 
    uint8_t sprites:1;              /* ???                          */
    uint8_t sprites_size:1;         /* 0 = 8x8, 1 = 8x16            */
    uint8_t bg_tiles_map:1;         /* 0 = 9800-9BFF, 1 = 9C00-9FFF */
    uint8_t bg_tiles:1;             /* 0 = 8800-97FF, 1 = 8000-8FFF */
    uint8_t window:1;               /* 0 = window off, 1 = on       */
    uint8_t window_tiles_map:1;     /* 0 = 9800-9BFF, 1 = 9C00-9FFF */
    uint8_t display:1;              /* 0 = LCD off, 1 = LCD on      */
} gpu_lcd_ctrl_t; 

/* Gameboy LCD Status - R/W accessing 0xFF41 address */
typedef struct gpu_lcd_status_s
{
    uint8_t mode:2;
    uint8_t ly_coincidence:1;
    uint8_t ir_mode_00:1;
    uint8_t ir_mode_01:1;
    uint8_t ir_mode_10:1;
    uint8_t ir_ly_coincidence:1;
    uint8_t spare:1;
} gpu_lcd_status_t;

/* RGB color */
typedef struct rgb_s
{
    uint8_t r;
    uint8_t g;
    uint8_t b;
    uint8_t a;
} rgb_t;

/* Gameboy GPU status */
typedef struct gpu_s 
{
    gpu_lcd_ctrl_t   *lcd_ctrl;
    gpu_lcd_status_t *lcd_status;

    /* scroll positions */
    uint8_t   *scroll_x;
    uint8_t   *scroll_y;

    /* window position  */
    uint8_t   *window_x;
    uint8_t   *window_y;

    /* current scanline and it's compare values */
    uint8_t   *ly;
    uint8_t   *lyc;

    /* clocks counter   */
    uint64_t   next;

    /* gpu step span */
    uint_fast32_t   step;

    /* window last drawn lines */
    uint8_t   window_last_ly;
    uint8_t   window_skipped_lines;
    uint16_t  spare;

    /* frame counter */
    uint_fast16_t  frame_counter;

    /* BG palette       */
    uint16_t  bg_palette[4]; 

    /* Obj palette 0/1  */
    uint16_t  obj_palette_0[4]; 
    uint16_t  obj_palette_1[4]; 

    /* CGB palette for background */
    uint16_t  cgb_palette_bg_rgb565[0x20];
    uint16_t  cgb_palette_bg[0x20];
    uint8_t   cgb_palette_bg_idx;
    uint8_t   cgb_palette_bg_autoinc;

    /* CGB palette for sprites */
    uint16_t  cgb_palette_oam_rgb565[0x20];
    uint16_t  cgb_palette_oam[0x20];
    uint8_t   cgb_palette_oam_idx;
    uint8_t   cgb_palette_oam_autoinc;

    /* frame buffer     */
    uint16_t  frame_buffer_prev[160 * 144];
    uint16_t  frame_buffer[160 * 144];
    uint8_t   priority[160 * 144];
    uint8_t   palette_idx[160 * 144];
} gpu_t;

extern gpu_t gpu;

#endif
