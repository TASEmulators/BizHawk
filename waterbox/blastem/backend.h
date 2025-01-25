/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef BACKEND_H_
#define BACKEND_H_

#include <stdint.h>
#include <stdio.h>
#include "gen.h"

#define INVALID_OFFSET 0xFFFFFFFF
#define EXTENSION_WORD 0xFFFFFFFE
#define CYCLE_NEVER 0xFFFFFFFF

#if defined(X86_32) || defined(X86_64)
typedef struct {
	int32_t disp;
	uint8_t mode;
	uint8_t base;
	uint8_t index;
} host_ea;
#else
typedef struct {
	int32_t disp;
	uint8_t mode;
	uint8_t base;
} host_ea;
#endif

typedef struct {
	uint8_t  *base;
	int32_t  *offsets;
} native_map_slot;

typedef struct deferred_addr {
	struct deferred_addr *next;
	code_ptr             dest;
	uint32_t             address;
} deferred_addr;

#include "memmap.h"
#include "system.h"

typedef struct {
	uint32_t flags;
	native_map_slot    *native_code_map;
	deferred_addr      *deferred;
	code_info          code;
	uint8_t            **ram_inst_sizes;
	memmap_chunk const *memmap;
	code_ptr           save_context;
	code_ptr           load_context;
	code_ptr           handle_cycle_limit;
	code_ptr           handle_cycle_limit_int;
	code_ptr           handle_code_write;
	code_ptr           handle_align_error_write;
	code_ptr           handle_align_error_read;
	system_str_fun_r8  debug_cmd_handler;
	uint32_t           memmap_chunks;
	uint32_t           address_mask;
	uint32_t           max_address;
	uint32_t           bus_cycles;
	uint32_t           clock_divider;
	uint32_t           move_pc_off;
	uint32_t           move_pc_size;
	int32_t            mem_ptr_off;
	int32_t            ram_flags_off;
	uint8_t            ram_flags_shift;
	uint8_t            address_size;
	uint8_t            byte_swap;
	int8_t             context_reg;
	int8_t             cycles;
	int8_t             limit;
	int8_t             scratch1;
	int8_t             scratch2;
	uint8_t            align_error_mask;
} cpu_options;

typedef uint8_t * (*native_addr_func)(void * context, uint32_t address);

deferred_addr * defer_address(deferred_addr * old_head, uint32_t address, uint8_t *dest);
void remove_deferred_until(deferred_addr **head_ptr, deferred_addr * remove_to);
void process_deferred(deferred_addr ** head_ptr, void * context, native_addr_func get_native);

void cycles(cpu_options *opts, uint32_t num);
void check_cycles_int(cpu_options *opts, uint32_t address);
void check_cycles(cpu_options * opts);
void check_code_prologue(code_info *code);
void log_address(cpu_options *opts, uint32_t address, char * format);

void retranslate_calc(cpu_options *opts);
void patch_for_retranslate(cpu_options *opts, code_ptr native_address, code_ptr handler);

code_ptr gen_mem_fun(cpu_options * opts, memmap_chunk const * memmap, uint32_t num_chunks, ftype fun_type, code_ptr *after_inc);
void * get_native_pointer(uint32_t address, void ** mem_pointers, cpu_options * opts);
void * get_native_write_pointer(uint32_t address, void ** mem_pointers, cpu_options * opts);
uint16_t read_word(uint32_t address, void **mem_pointers, cpu_options *opts, void *context);
void write_word(uint32_t address, uint16_t value, void **mem_pointers, cpu_options *opts, void *context);
uint8_t read_byte(uint32_t address, void **mem_pointers, cpu_options *opts, void *context);
void write_byte(uint32_t address, uint8_t value, void **mem_pointers, cpu_options *opts, void *context);
memmap_chunk const *find_map_chunk(uint32_t address, cpu_options *opts, uint16_t flags, uint32_t *size_sum);
uint32_t chunk_size(cpu_options *opts, memmap_chunk const *chunk);
uint32_t ram_size(cpu_options *opts);

#endif //BACKEND_H_

