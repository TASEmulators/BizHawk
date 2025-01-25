/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef GENESIS_H_
#define GENESIS_H_

#include <stdint.h>
#include "system.h"
#include "m68k_core.h"
#ifdef NEW_CORE
#include "z80.h"
#else
#include "z80_to_x86.h"
#endif
#include "ym2612.h"
#include "vdp.h"
#include "psg.h"
#include "io.h"
#include "romdb.h"
#include "arena.h"
#include "i2c.h"

typedef struct genesis_context genesis_context;

struct genesis_context {
	system_header   header;
	m68k_context    *m68k;
	z80_context     *z80;
	vdp_context     *vdp;
	ym2612_context  *ym;
	psg_context     *psg;
	uint16_t        *cart;
	uint16_t        *lock_on;
	uint16_t        *work_ram;
	uint8_t         *zram;
	void            *extra;
	uint8_t         *save_storage;
	void            *mapper_temp;
	eeprom_map      *eeprom_map;
	write_16_fun    tmss_write_16;
	write_8_fun     tmss_write_8;
	read_16_fun     tmss_read_16;
	read_8_fun      tmss_read_8;
	uint16_t        *tmss_pointers[NUM_MEM_AREAS];
	uint8_t         *tmss_buffer;
	uint8_t         *serialize_tmp;
	size_t          serialize_size;
	uint32_t        num_eeprom;
	uint32_t        save_size;
	uint32_t        save_ram_mask;
	uint32_t        master_clock; //Current master clock value
	uint32_t        normal_clock; //Normal master clock (used to restore master clock after turbo mode)
	uint32_t        frame_end;
	uint32_t        max_cycles;
	uint32_t        int_latency_prev1;
	uint32_t        int_latency_prev2;
	uint32_t        reset_cycle;
	uint32_t        last_frame;
	uint32_t        last_flush_cycle;
	uint32_t        soft_flush_cycles;
	uint32_t        tmss_write_offset;
	uint8_t         bank_regs[8];
	uint16_t        z80_bank_reg;
	uint16_t        tmss_lock[2];
	uint16_t        mapper_start_index;
	uint8_t         mapper_type;
	uint8_t         save_type;
	sega_io         io;
	uint8_t         version_reg;
	uint8_t         bus_busy;
	uint8_t         reset_requested;
	uint8_t         tmss;
	uint8_t         vdp_unlocked;
	eeprom_state    eeprom;
	nor_state       nor;
};

#define RAM_WORDS 32 * 1024
#define Z80_RAM_BYTES 8 * 1024

m68k_context * sync_components(m68k_context *context, uint32_t address);
genesis_context *alloc_config_genesis(void *rom, uint32_t rom_size, void *lock_on, uint32_t lock_on_size, uint32_t system_opts, uint8_t force_region);
void genesis_serialize(genesis_context *gen, serialize_buffer *buf, uint32_t m68k_pc, uint8_t all);
void genesis_deserialize(deserialize_buffer *buf, genesis_context *gen);

#endif //GENESIS_H_

