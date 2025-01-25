/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef VDP_H_
#define VDP_H_

#include <stdint.h>
#include <stdio.h>
#include "system.h"
#include "serialize.h"

#define VDP_REGS 24
#define CRAM_SIZE 64
#define SHADOW_OFFSET CRAM_SIZE
#define HIGHLIGHT_OFFSET (SHADOW_OFFSET+CRAM_SIZE)
#define MODE4_OFFSET (HIGHLIGHT_OFFSET+CRAM_SIZE)
#define MIN_VSRAM_SIZE 40
#define MAX_VSRAM_SIZE 64
#define VRAM_SIZE (64*1024)
#define BORDER_LEFT 13
#define BORDER_RIGHT 14
#define HORIZ_BORDER (BORDER_LEFT+BORDER_RIGHT)
#define LINEBUF_SIZE (320+HORIZ_BORDER) //H40 + full border
#define SCROLL_BUFFER_SIZE 32
#define BORDER_BOTTOM 13 //TODO: Replace with actual value
#define MAX_DRAWS_H32_MODE4 8
#define MAX_SPRITES_LINE 20
#define MAX_SPRITES_LINE_H32 16
#define MAX_SPRITES_FRAME 80
#define MAX_SPRITES_FRAME_H32 64
#define SAT_CACHE_SIZE (MAX_SPRITES_FRAME * 4)

#define FBUF_SHADOW 0x0001
#define FBUF_HILIGHT 0x0010
#define FBUF_MODE4 0x0100
#define DBG_SHADOW 0x10
#define DBG_HILIGHT 0x20
#define DBG_PRIORITY 0x8
#define DBG_SRC_MASK 0x7
#define DBG_SRC_A 0x1
#define DBG_SRC_W 0x2
#define DBG_SRC_B 0x3
#define DBG_SRC_S 0x4
#define DBG_SRC_BG 0x0

#define MCLKS_LINE 3420

#define FLAG_DOT_OFLOW     0x01
#define FLAG_CAN_MASK      0x02
#define FLAG_MASKED        0x04
#define FLAG_WINDOW        0x08
#define FLAG_PENDING       0x10
#define FLAG_READ_FETCHED  0x20
#define FLAG_DMA_RUN       0x40
#define FLAG_DMA_PROG      0x80

#define FLAG2_VINT_PENDING   0x01
#define FLAG2_HINT_PENDING   0x02
#define FLAG2_READ_PENDING   0x04
#define FLAG2_SPRITE_COLLIDE 0x08
#define FLAG2_REGION_PAL     0x10
#define FLAG2_EVEN_FIELD     0x20
#define FLAG2_BYTE_PENDING   0x40
#define FLAG2_PAUSE          0x80

#define DISPLAY_ENABLE 0x40

enum {
	REG_MODE_1=0,
	REG_MODE_2,
	REG_SCROLL_A,
	REG_WINDOW,
	REG_SCROLL_B,
	REG_SAT,
	REG_STILE_BASE,
	REG_BG_COLOR,
	REG_X_SCROLL,
	REG_Y_SCROLL,
	REG_HINT,
	REG_MODE_3,
	REG_MODE_4,
	REG_HSCROLL,
	REG_BGTILE_BASE,
	REG_AUTOINC,
	REG_SCROLL,
	REG_WINDOW_H,
	REG_WINDOW_V,
	REG_DMALEN_L,
	REG_DMALEN_H,
	REG_DMASRC_L,
	REG_DMASRC_M,
	REG_DMASRC_H,
	REG_KMOD_CTRL=29,
	REG_KMOD_MSG,
	REG_KMOD_TIMER
};

//Mode reg 1
#define BIT_VSCRL_LOCK 0x80
#define BIT_HSCRL_LOCK 0x40
#define BIT_COL0_MASK  0x20
#define BIT_HINT_EN    0x10
#define BIT_SPRITE_8PX 0x08
#define BIT_PAL_SEL    0x04
#define BIT_MODE_4     BIT_PAL_SEL
#define BIT_HVC_LATCH  0x02
#define BIT_DISP_DIS   0x01

//Mode reg 2
#define BIT_128K_VRAM  0x80
#define BIT_DISP_EN    0x40
#define BIT_VINT_EN    0x20
#define BIT_DMA_ENABLE 0x10
#define BIT_PAL        0x08
#define BIT_MODE_5     0x04
#define BIT_SPRITE_SZ  0x02

//Mode reg 3
#define BIT_EINT_EN    0x08
#define BIT_VSCROLL    0x04

//Mode reg 4
#define BIT_H40        0x01
#define BIT_HILIGHT    0x8
#define BIT_DOUBLE_RES 0x4
#define BIT_INTERLACE  0x2

//Test register
#define TEST_BIT_DISABLE 0x40

typedef struct {
	uint16_t address;
	int16_t x_pos;
	uint8_t pal_priority;
	uint8_t h_flip;
	uint8_t width;
	uint8_t height;
} sprite_draw;

typedef struct {
	uint8_t size;
	uint8_t index;
	int16_t y;
} sprite_info;

#define FIFO_SIZE 4

typedef struct {
	uint32_t cycle;
	uint32_t address;
	uint16_t value;
	uint8_t  cd;
	uint8_t  partial;
} fifo_entry;

enum {
	VDP_DEBUG_PLANE,
	VDP_DEBUG_VRAM,
	VDP_DEBUG_CRAM,
	VDP_DEBUG_COMPOSITE,
	VDP_NUM_DEBUG_TYPES
};

typedef struct {
	system_header  *system;
	//pointer to current line in framebuffer
	uint32_t       *output;
	//pointer to current framebuffer
	uint32_t       *fb;
	uint8_t        *done_composite;
	uint32_t       *debug_fbs[VDP_NUM_DEBUG_TYPES];
	char           *kmod_msg_buffer;
	uint32_t       kmod_buffer_storage;
	uint32_t       kmod_buffer_length;
	uint32_t       timer_start_cycle;
	uint32_t       output_pitch;
	uint32_t       debug_fb_pitch[VDP_NUM_DEBUG_TYPES];
	fifo_entry     fifo[FIFO_SIZE];
	int32_t        fifo_write;
	int32_t        fifo_read;
	uint32_t       address;
	uint32_t       address_latch;
	uint32_t       serial_address;
	uint32_t       colors[CRAM_SIZE*4];
	uint32_t       debugcolors[1 << (3 + 1 + 1 + 1)];//3 bits for source, 1 bit for priority, 1 bit for shadow, 1 bit for hilight
	uint16_t       cram[CRAM_SIZE];
	uint32_t       frame;
	uint32_t       vsram_size;
	uint8_t        cd;
	uint8_t        cd_latch;
	uint8_t	       flags;
	uint8_t        regs[VDP_REGS];
	//cycle count in MCLKs
	uint32_t       cycles;
	uint32_t       pending_vint_start;
	uint32_t       pending_hint_start;
	uint32_t       top_offset;
	uint16_t       vsram[MAX_VSRAM_SIZE];
	uint16_t       vscroll_latch[2];
	uint16_t       vcounter;
	uint16_t       inactive_start;
	uint16_t       border_top;
	uint16_t       border_bot;
	uint16_t       hscroll_a;
	uint16_t       hscroll_a_fine;
	uint16_t       hscroll_b;
	uint16_t       hscroll_b_fine;
	uint16_t       h40_lines;
	uint16_t       output_lines;
	sprite_draw    sprite_draw_list[MAX_SPRITES_LINE];
	sprite_info    sprite_info_list[MAX_SPRITES_LINE];
	uint8_t        sat_cache[SAT_CACHE_SIZE];
	uint16_t       col_1;
	uint16_t       col_2;
	uint16_t       hv_latch;
	uint16_t       prefetch;
	uint16_t       test_port;
	//stores 2-bit palette + 4-bit palette index + priority for current sprite line
	uint8_t        linebuf[LINEBUF_SIZE];
	uint8_t        compositebuf[LINEBUF_SIZE];
	uint8_t        layer_debug_buf[LINEBUF_SIZE];
	uint8_t        hslot; //hcounter/2
	uint8_t	       sprite_index;
	uint8_t        sprite_draws;
	int8_t         slot_counter;
	int8_t         cur_slot;
	uint8_t        sprite_x_offset;
	uint8_t        max_sprites_frame;
	uint8_t        max_sprites_line;
	uint8_t        fetch_tmp[2];
	uint8_t        v_offset;
	uint8_t        hint_counter;
	uint8_t        flags2;
	uint8_t        double_res;
	uint8_t        buf_a_off;
	uint8_t        buf_b_off;
	uint8_t        pending_byte;
	uint8_t        state;
	uint8_t        cur_buffer;
	uint8_t        tmp_buf_a[SCROLL_BUFFER_SIZE];
	uint8_t        tmp_buf_b[SCROLL_BUFFER_SIZE];
	uint8_t        enabled_debuggers;
	uint8_t        debug_fb_indices[VDP_NUM_DEBUG_TYPES];
	uint8_t        debug_modes[VDP_NUM_DEBUG_TYPES];
	uint8_t        pushed_frame;
	uint8_t        vdpmem[];
} vdp_context;



vdp_context *init_vdp_context(uint8_t region_pal, uint8_t has_max_vsram);
void vdp_free(vdp_context *context);
void vdp_run_context_full(vdp_context * context, uint32_t target_cycles);
void vdp_run_context(vdp_context * context, uint32_t target_cycles);
//runs from current cycle count to VBLANK for the current mode, returns ending cycle count
uint32_t vdp_run_to_vblank(vdp_context * context);
//runs until the target cycle is reached or the current DMA operation has completed, whicever comes first
void vdp_run_dma_done(vdp_context * context, uint32_t target_cycles);
uint8_t vdp_load_gst(vdp_context * context, FILE * state_file);
uint8_t vdp_save_gst(vdp_context * context, FILE * outfile);
int vdp_control_port_write(vdp_context * context, uint16_t value);
void vdp_control_port_write_pbc(vdp_context * context, uint8_t value);
int vdp_data_port_write(vdp_context * context, uint16_t value);
void vdp_data_port_write_pbc(vdp_context * context, uint8_t value);
void vdp_test_port_write(vdp_context * context, uint16_t value);
uint16_t vdp_control_port_read(vdp_context * context);
uint16_t vdp_data_port_read(vdp_context * context);
uint8_t vdp_data_port_read_pbc(vdp_context * context);
void vdp_latch_hv(vdp_context *context);
uint16_t vdp_hv_counter_read(vdp_context * context);
void vdp_adjust_cycles(vdp_context * context, uint32_t deduction);
uint32_t vdp_next_hint(vdp_context * context);
uint32_t vdp_next_vint(vdp_context * context);
uint32_t vdp_next_vint_z80(vdp_context * context);
uint32_t vdp_next_nmi(vdp_context *context);
void vdp_int_ack(vdp_context * context);
void vdp_print_sprite_table(vdp_context * context);
void vdp_print_reg_explain(vdp_context * context);
void latch_mode(vdp_context * context);
uint32_t vdp_cycles_to_frame_end(vdp_context * context);
void write_cram_internal(vdp_context * context, uint16_t addr, uint16_t value);
void vdp_check_update_sat_byte(vdp_context *context, uint32_t address, uint8_t value);
void vdp_pbc_pause(vdp_context *context);
void vdp_release_framebuffer(vdp_context *context);
void vdp_reacquire_framebuffer(vdp_context *context);
void vdp_serialize(vdp_context *context, serialize_buffer *buf);
void vdp_deserialize(deserialize_buffer *buf, void *vcontext);
void vdp_force_update_framebuffer(vdp_context *context);
void vdp_toggle_debug_view(vdp_context *context, uint8_t debug_type);
void vdp_inc_debug_mode(vdp_context *context);
//to be implemented by the host system
uint16_t read_dma_value(uint32_t address);
void vdp_replay_event(vdp_context *context, uint8_t event, event_reader *reader);

#endif //VDP_H_
