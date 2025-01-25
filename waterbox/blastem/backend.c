/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "backend.h"
#include <stdlib.h>

deferred_addr * defer_address(deferred_addr * old_head, uint32_t address, uint8_t *dest)
{
	deferred_addr * new_head = malloc(sizeof(deferred_addr));
	new_head->next = old_head;
	new_head->address = address & 0xFFFFFF;
	new_head->dest = dest;
	return new_head;
}

void remove_deferred_until(deferred_addr **head_ptr, deferred_addr * remove_to)
{
	for(deferred_addr *cur = *head_ptr; cur && cur != remove_to; cur = *head_ptr)
	{
		*head_ptr = cur->next;
		free(cur);
	}
}

void process_deferred(deferred_addr ** head_ptr, void * context, native_addr_func get_native)
{
	deferred_addr * cur = *head_ptr;
	deferred_addr **last_next = head_ptr;
	while(cur)
	{
		code_ptr native = get_native(context, cur->address);//get_native_address(opts->native_code_map, cur->address);
		if (native) {
			int32_t disp = native - (cur->dest + 4);
			code_ptr out = cur->dest;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*out = disp;
			*last_next = cur->next;
			free(cur);
			cur = *last_next;
		} else {
			last_next = &(cur->next);
			cur = cur->next;
		}
	}
}

memmap_chunk const *find_map_chunk(uint32_t address, cpu_options *opts, uint16_t flags, uint32_t *size_sum)
{
	if (size_sum) {
		*size_sum = 0;
	}
	uint32_t minsize;
	if (flags == MMAP_CODE) {
		minsize = 1 << (opts->ram_flags_shift + 3);
	} else {
		minsize = 0;
	}
	address &= opts->address_mask;
	for (memmap_chunk const *cur = opts->memmap, *end = opts->memmap + opts->memmap_chunks; cur != end; cur++)
	{
		if (address >= cur->start && address < cur->end) {
			return cur;
		} else if (size_sum && (cur->flags & flags) == flags) {
			uint32_t size = chunk_size(opts, cur);
			if (size < minsize) {
				size = minsize;
			}
			*size_sum += size;
		}
	}
	return NULL;
}

void * get_native_pointer(uint32_t address, void ** mem_pointers, cpu_options * opts)
{
	memmap_chunk const * memmap = opts->memmap;
	address &= opts->address_mask;
	for (uint32_t chunk = 0; chunk < opts->memmap_chunks; chunk++)
	{
		if (address >= memmap[chunk].start && address < memmap[chunk].end) {
			if (!(memmap[chunk].flags & (MMAP_READ|MMAP_READ_CODE))) {
				return NULL;
			}
			uint8_t * base = memmap[chunk].flags & MMAP_PTR_IDX
				? mem_pointers[memmap[chunk].ptr_index]
				: memmap[chunk].buffer;
			if (!base) {
				if (memmap[chunk].flags & MMAP_AUX_BUFF) {
					return ((uint8_t *)memmap[chunk].buffer) + (address & memmap[chunk].aux_mask);
				}
				return NULL;
			}
			return base + (address & memmap[chunk].mask);
		}
	}
	return NULL;
}

void * get_native_write_pointer(uint32_t address, void ** mem_pointers, cpu_options * opts)
{
	memmap_chunk const * memmap = opts->memmap;
	address &= opts->address_mask;
	for (uint32_t chunk = 0; chunk < opts->memmap_chunks; chunk++)
	{
		if (address >= memmap[chunk].start && address < memmap[chunk].end) {
			if (!(memmap[chunk].flags & (MMAP_WRITE))) {
				return NULL;
			}
			uint8_t * base = memmap[chunk].flags & MMAP_PTR_IDX
				? mem_pointers[memmap[chunk].ptr_index]
				: memmap[chunk].buffer;
			if (!base) {
				if (memmap[chunk].flags & MMAP_AUX_BUFF) {
					return ((uint8_t *)memmap[chunk].buffer) + (address & memmap[chunk].aux_mask);
				}
				return NULL;
			}
			return base + (address & memmap[chunk].mask);
		}
	}
	return NULL;
}

uint16_t read_word(uint32_t address, void **mem_pointers, cpu_options *opts, void *context)
{
	memmap_chunk const *chunk = find_map_chunk(address, opts, 0, NULL);
	if (!chunk) {
		return 0xFFFF;
	}
	uint32_t offset = address & chunk->mask;
	if (chunk->flags & MMAP_READ) {
		uint8_t *base;
		if (chunk->flags & MMAP_PTR_IDX) {
			base = mem_pointers[chunk->ptr_index];
		} else {
			base = chunk->buffer;
		}
		if (base) {
			uint16_t val;
			if ((chunk->flags & MMAP_ONLY_ODD) || (chunk->flags & MMAP_ONLY_EVEN)) {
				offset /= 2;
				val = base[offset];
				if (chunk->flags & MMAP_ONLY_ODD) {
					val |= 0xFF00;
				} else {
					val = val << 8 | 0xFF;
				}
			} else {
				val = *(uint16_t *)(base + offset);
			}
			return val;
		}
	}
	if ((!(chunk->flags & MMAP_READ) || (chunk->flags & MMAP_FUNC_NULL)) && chunk->read_16) {
		return chunk->read_16(offset, context);
	}
	return 0xFFFF;
}

void write_word(uint32_t address, uint16_t value, void **mem_pointers, cpu_options *opts, void *context)
{
	memmap_chunk const *chunk = find_map_chunk(address, opts, 0, NULL);
	if (!chunk) {
		return;
	}
	uint32_t offset = address & chunk->mask;
	if (chunk->flags & MMAP_WRITE) {
		uint8_t *base;
		if (chunk->flags & MMAP_PTR_IDX) {
			base = mem_pointers[chunk->ptr_index];
		} else {
			base = chunk->buffer;
		}
		if (base) {
			if ((chunk->flags & MMAP_ONLY_ODD) || (chunk->flags & MMAP_ONLY_EVEN)) {
				offset /= 2;
				if (chunk->flags & MMAP_ONLY_EVEN) {
					value >>= 16;
				}
				base[offset] = value;
			} else {
				*(uint16_t *)(base + offset) = value;
			}
			return;
		}
	}
	if ((!(chunk->flags & MMAP_WRITE) || (chunk->flags & MMAP_FUNC_NULL)) && chunk->write_16) {
		chunk->write_16(offset, context, value);
	}
}

uint8_t read_byte(uint32_t address, void **mem_pointers, cpu_options *opts, void *context)
{
	memmap_chunk const *chunk = find_map_chunk(address, opts, 0, NULL);
	if (!chunk) {
		return 0xFF;
	}
	uint32_t offset = address & chunk->mask;
	if (chunk->flags & MMAP_READ) {
		uint8_t *base;
		if (chunk->flags & MMAP_PTR_IDX) {
			base = mem_pointers[chunk->ptr_index];
		} else {
			base = chunk->buffer;
		}
		if (base) {
			if ((chunk->flags & MMAP_ONLY_ODD) || (chunk->flags & MMAP_ONLY_EVEN)) {
				if (address & 1) {
					if (chunk->flags & MMAP_ONLY_EVEN) {
						return 0xFF;
					}
				} else if (chunk->flags & MMAP_ONLY_ODD) {
					return 0xFF;
				}
				offset /= 2;
			} else if(opts->byte_swap) {
				offset ^= 1;
			}
			return base[offset];
		}
	}
	if ((!(chunk->flags & MMAP_READ) || (chunk->flags & MMAP_FUNC_NULL)) && chunk->read_8) {
		return chunk->read_8(offset, context);
	}
	return 0xFF;
}

void write_byte(uint32_t address, uint8_t value, void **mem_pointers, cpu_options *opts, void *context)
{
	memmap_chunk const *chunk = find_map_chunk(address, opts, 0, NULL);
	if (!chunk) {
		return;
	}
	uint32_t offset = address & chunk->mask;
	if (chunk->flags & MMAP_WRITE) {
		uint8_t *base;
		if (chunk->flags & MMAP_PTR_IDX) {
			base = mem_pointers[chunk->ptr_index];
		} else {
			base = chunk->buffer;
		}
		if (base) {
			if ((chunk->flags & MMAP_ONLY_ODD) || (chunk->flags & MMAP_ONLY_EVEN)) {
				if (address & 1) {
					if (chunk->flags & MMAP_ONLY_EVEN) {
						return;
					}
				} else if (chunk->flags & MMAP_ONLY_ODD) {
					return;
				}
				offset /= 2;
			} else if(opts->byte_swap) {
				offset ^= 1;
			}
			base[offset] = value;
		}
	}
	if ((!(chunk->flags & MMAP_WRITE) || (chunk->flags & MMAP_FUNC_NULL)) && chunk->write_8) {
		chunk->write_8(offset, context, value);
	}
}

uint32_t chunk_size(cpu_options *opts, memmap_chunk const *chunk)
{
	if (chunk->mask == opts->address_mask) {
		return chunk->end - chunk->start;
	} else {
		return chunk->mask + 1;
	}
}

uint32_t ram_size(cpu_options *opts)
{
	uint32_t size = 0;
	uint32_t minsize = 1 << (opts->ram_flags_shift + 3);
	for (int i = 0; i < opts->memmap_chunks; i++)
	{
		if (opts->memmap[i].flags & MMAP_CODE) {
			uint32_t cursize = chunk_size(opts, opts->memmap + i);
			if (cursize < minsize) {
				size += minsize;
			} else {
				size += cursize;
			}
		}
	}
	return size;
}
