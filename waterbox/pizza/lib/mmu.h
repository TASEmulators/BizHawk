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

#ifndef __MMU_HDR__
#define __MMU_HDR__

#include <stdio.h>
#include <stdint.h>
#include <sys/time.h>

typedef struct mmu_s {
    /* main 64K of memory */
    uint8_t memory[65536];

    /* vram in standby */
    uint8_t vram0[0x2000];
    uint8_t vram1[0x2000];

    /* vram current idx */
    uint8_t  vram_idx;
    uint8_t  spare;
    uint16_t spare2;

    /* internal RAM */
    uint8_t ram_internal[0x2000];
    uint8_t ram_external_enabled;
    uint8_t ram_current_bank;

    /* cartridge type */
    uint8_t carttype;

    /* number of switchable roms */
    uint8_t roms;

    /* current ROM bank */
    uint8_t rom_current_bank;

    /* type of banking */
    uint8_t banking;

    /* working RAM (only CGB) */
    uint8_t wram[0x8000];

    /* current WRAM bank (only CGB) */
    uint8_t  wram_current_bank;
    uint8_t  spare3;
    uint16_t spare4;

    /* DMA transfer stuff */
    uint_fast16_t dma_address;
    uint_fast16_t dma_cycles;

    /* HDMA transfer stuff */
    uint16_t hdma_src_address;
    uint16_t hdma_dst_address;
    uint16_t hdma_to_transfer;
    uint8_t  hdma_transfer_mode;
    uint8_t  hdma_current_line;

    /* RTC stuff */
    uint8_t  rtc_mode;
    uint8_t  spare5;
    uint16_t spare6;
    time_t   rtc_time;
    time_t   rtc_latch_time;

    uint64_t   dma_next;
} mmu_t;

extern mmu_t mmu;

/* callback function */
typedef void (*mmu_rumble_cb_t) (uint8_t onoff);

/* functions prototypes */
void         *mmu_addr(uint16_t a);
void         *mmu_addr_vram0();
void         *mmu_addr_vram1();
void          mmu_dump_all();
void          mmu_init(uint8_t c, uint8_t rn);
void          mmu_init_ram(uint32_t c);
void          mmu_load(uint8_t *data, size_t sz, uint16_t a);
void          mmu_load_cartridge(uint8_t *data, size_t sz);
void          mmu_move(uint16_t d, uint16_t s);
uint8_t       mmu_read_no_cyc(uint16_t a);
uint8_t       mmu_read(uint16_t a);
unsigned int  mmu_read_16(uint16_t a);
void          mmu_restore_ram(char *fn);
void          mmu_restore_rtc(char *fn);
void          mmu_save_ram(char *fn);
void          mmu_save_rtc(char *fn);
void          mmu_set_rumble_cb(mmu_rumble_cb_t cb);
void          mmu_step();
void          mmu_term();
void          mmu_write_no_cyc(uint16_t a, uint8_t v);
void          mmu_write(uint16_t a, uint8_t v);
void          mmu_write_16(uint16_t a, uint16_t v);

#endif
