/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef M68K_CORE_H_
#define M68K_CORE_H_
#include <stdint.h>
#include <stdio.h>
#include "backend.h"
#include "serialize.h"
//#include "68kinst.h"
struct m68kinst;

#define NUM_MEM_AREAS 10
#define NATIVE_MAP_CHUNKS (64*1024)
#define NATIVE_CHUNK_SIZE ((16 * 1024 * 1024 / NATIVE_MAP_CHUNKS))
#define MAX_NATIVE_SIZE 255

#define M68K_OPT_BROKEN_READ_MODIFY 1

#define INT_PENDING_SR_CHANGE 254
#define INT_PENDING_NONE 255

#define M68K_STATUS_TRACE 0x80

typedef void (*start_fun)(uint8_t * addr, void * context);

typedef struct {
	code_ptr impl;
	uint16_t reglist;
	uint8_t  reg_to_mem;
	uint8_t  size;
	int8_t   dir;
} movem_fun;

typedef struct {
	cpu_options     gen;

	int8_t          dregs[8];
	int8_t          aregs[8];
	int8_t			flag_regs[5];
	FILE            *address_log;
	code_ptr        read_16;
	code_ptr        write_16;
	code_ptr        read_8;
	code_ptr        write_8;
	code_ptr        read_32;
	code_ptr        write_32_lowfirst;
	code_ptr        write_32_highfirst;
	code_ptr        do_sync;
	code_ptr        handle_int_latch;
	code_ptr        trap;
	start_fun       start_context;
	code_ptr        retrans_stub;
	code_ptr        native_addr;
	code_ptr        native_addr_and_sync;
	code_ptr		get_sr;
	code_ptr		set_sr;
	code_ptr		set_ccr;
	code_ptr        bp_stub;
	code_info       extra_code;
	movem_fun       *big_movem;
	uint32_t        num_movem;
	uint32_t        movem_storage;
	code_word       prologue_start;
} m68k_options;

typedef struct m68k_context m68k_context;
typedef void (*m68k_debug_handler)(m68k_context *context, uint32_t pc);

typedef struct {
	m68k_debug_handler handler;
	uint32_t           address;
} m68k_breakpoint;

struct m68k_context {
	uint8_t         flags[5];
	uint8_t         status;
	uint16_t        int_ack;
	uint32_t        dregs[8];
	uint32_t        aregs[9];
	uint32_t		target_cycle; //cycle at which the next synchronization or interrupt occurs
	uint32_t		current_cycle;
	uint32_t        sync_cycle;
	uint32_t        int_cycle;
	uint32_t        int_num;
	uint32_t        last_prefetch_address;
	uint16_t        *mem_pointers[NUM_MEM_AREAS];
	code_ptr        resume_pc;
	code_ptr        reset_handler;
	m68k_options    *options;
	void            *system;
	m68k_breakpoint *breakpoints;
	uint32_t        num_breakpoints;
	uint32_t        bp_storage;
	uint8_t         int_pending;
	uint8_t         trace_pending;
	uint8_t         should_return;
	uint8_t         ram_code_flags[];
};

typedef m68k_context *(*m68k_reset_handler)(m68k_context *context);


void translate_m68k_stream(uint32_t address, m68k_context * context);
void start_68k_context(m68k_context * context, uint32_t address);
void resume_68k(m68k_context *context);
void init_m68k_opts(m68k_options * opts, memmap_chunk * memmap, uint32_t num_chunks, uint32_t clock_divider);
m68k_context * init_68k_context(m68k_options * opts, m68k_reset_handler reset_handler);
void m68k_reset(m68k_context * context);
void m68k_options_free(m68k_options *opts);
void insert_breakpoint(m68k_context * context, uint32_t address, m68k_debug_handler bp_handler);
void remove_breakpoint(m68k_context * context, uint32_t address);
m68k_context * m68k_handle_code_write(uint32_t address, m68k_context * context);
uint32_t get_instruction_start(m68k_options *opts, uint32_t address);
uint16_t m68k_get_ir(m68k_context *context);
void m68k_print_regs(m68k_context * context);
void m68k_invalidate_code_range(m68k_context *context, uint32_t start, uint32_t end);
void m68k_serialize(m68k_context *context, uint32_t pc, serialize_buffer *buf);
void m68k_deserialize(deserialize_buffer *buf, void *vcontext);

#endif //M68K_CORE_H_

