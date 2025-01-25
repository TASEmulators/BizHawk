#include "genesis.h"
#include <stdlib.h>
#include <string.h>
#include "util.h"

enum {
	NOR_NORMAL,
	NOR_PRODUCTID,
	NOR_BOOTBLOCK
};

enum {
	NOR_CMD_IDLE,
	NOR_CMD_AA,
	NOR_CMD_55
};

//Technically this value shoudl be slightly different between NTSC and PAL
//as it's defined as 200 micro-seconds, not in clock cycles
#define NOR_WRITE_PAUSE 10690 

void nor_flash_init(nor_state *state, uint8_t *buffer, uint32_t size, uint32_t page_size, uint16_t product_id, uint8_t bus_flags)
{
	state->buffer = buffer;
	state->page_buffer = malloc(page_size);
	memset(state->page_buffer, 0xFF, page_size);
	state->size = size;
	state->page_size = page_size;
	state->product_id = product_id;
	state->last_write_cycle = 0xFFFFFFFF;
	state->mode = NOR_NORMAL;
	state->cmd_state = NOR_CMD_IDLE;
	state->alt_cmd = 0;
	state->bus_flags = bus_flags;
	state->cmd_address1 = 0x5555;
	state->cmd_address2 = 0x2AAA;
}

void nor_run(nor_state *state, m68k_context *m68k, uint32_t cycle)
{
	if (state->last_write_cycle == 0xFFFFFFFF) {
		return;
	}
	if (cycle - state->last_write_cycle >= NOR_WRITE_PAUSE) {
		state->last_write_cycle = 0xFFFFFFFF;
		for (uint32_t i = 0; i < state->page_size; i++) {
			state->buffer[state->current_page + i] = state->page_buffer[i];
		}
		memset(state->page_buffer, 0xFF, state->page_size);
		if (state->bus_flags == RAM_FLAG_BOTH) {
			//TODO: add base address of NOR device to start and end addresses
			m68k_invalidate_code_range(m68k, state->current_page, state->current_page + state->page_size);
		}
	}
}

uint8_t nor_flash_read_b(uint32_t address, void *vcontext)
{
	m68k_context *m68k = vcontext;
	genesis_context *gen = m68k->system;
	nor_state *state = &gen->nor;
	if (
		((address & 1) && state->bus_flags == RAM_FLAG_EVEN) ||
		(!(address & 1) && state->bus_flags == RAM_FLAG_ODD)
	) {
		return 0xFF;
	}
	if (state->bus_flags != RAM_FLAG_BOTH) {
		address = address >> 1;
	}
	
	nor_run(state, m68k, m68k->current_cycle);
	switch (state->mode)
	{
	case NOR_NORMAL:
		if (state->bus_flags == RAM_FLAG_BOTH) {
			address ^= 1;
		}
		return state->buffer[address & (state->size-1)];
		break;
	case NOR_PRODUCTID:
		switch (address & (state->size - 1))
		{
		case 0:
			return state->product_id >> 8;
		case 1:
			return state->product_id;
		case 2:
			//TODO: Implement boot block protection
			return 0xFE;
		default:
			return 0xFE;
		}			//HERE
		break;
	case NOR_BOOTBLOCK:
		break;
	}
	return 0xFF;
}

uint16_t nor_flash_read_w(uint32_t address, void *context)
{
	uint16_t value = nor_flash_read_b(address, context) << 8;
	value |= nor_flash_read_b(address+1, context);
	return value;
}

void nor_write_byte(nor_state *state, uint32_t address, uint8_t value, uint32_t cycle)
{
	switch(state->mode)
	{
	case NOR_NORMAL:
		if (state->last_write_cycle != 0xFFFFFFFF) {
			state->current_page = address & (state->size - 1) & ~(state->page_size - 1);
		}
		if (state->bus_flags == RAM_FLAG_BOTH) {
			address ^= 1;
		}
		state->page_buffer[address & (state->page_size - 1)] = value;
		break;
	case NOR_PRODUCTID:
		break;
	case NOR_BOOTBLOCK:
		//TODO: Implement boot block protection
		state->mode = NOR_NORMAL;
		break;
	}
}

void *nor_flash_write_b(uint32_t address, void *vcontext, uint8_t value)
{
	m68k_context *m68k = vcontext;
	genesis_context *gen = m68k->system;
	nor_state *state = &gen->nor;
	if (
		((address & 1) && state->bus_flags == RAM_FLAG_EVEN) ||
		(!(address & 1) && state->bus_flags == RAM_FLAG_ODD)
	) {
		return vcontext;
	}
	if (state->bus_flags != RAM_FLAG_BOTH) {
		address = address >> 1;
	}
	
	nor_run(state, m68k, m68k->current_cycle);
	switch (state->cmd_state)
	{
	case NOR_CMD_IDLE:
		if (value == 0xAA && (address & (state->size - 1)) == state->cmd_address1) {
			state->cmd_state = NOR_CMD_AA;
		} else {
			nor_write_byte(state, address, value, m68k->current_cycle);
			state->cmd_state = NOR_CMD_IDLE;
		}
		break;
	case NOR_CMD_AA:
		if (value == 0x55 && (address & (state->size - 1)) == state->cmd_address2) {
			state->cmd_state = NOR_CMD_55;
		} else {
			nor_write_byte(state, state->cmd_address1, 0xAA, m68k->current_cycle);
			nor_write_byte(state, address, value, m68k->current_cycle);
			state->cmd_state = NOR_CMD_IDLE;
		}
		break;
	case NOR_CMD_55:
		if ((address & (state->size - 1)) == state->cmd_address1) {
			if (state->alt_cmd) {
				switch(value)
				{
				case 0x10:
					puts("UNIMPLEMENTED: NOR flash erase");
					break;
				case 0x20:
					puts("UNIMPLEMENTED: NOR flash disable protection");
					break;
				case 0x40:
					state->mode = NOR_BOOTBLOCK;
					break;
				case 0x60:
					state->mode = NOR_PRODUCTID;
					break;
				}
			} else {
				switch(value)
				{
				case 0x80:
					state->alt_cmd = 1;
					break;
				case 0x90:
					state->mode = NOR_PRODUCTID;
					break;
				case 0xA0:
					puts("UNIMPLEMENTED: NOR flash enable protection");
					break;
				case 0xF0:
					state->mode = NOR_NORMAL;
					break;
				default:
					printf("Unrecognized unshifted NOR flash command %X\n", value);
				}
			}
		} else {
			nor_write_byte(state, state->cmd_address1, 0xAA, m68k->current_cycle);
			nor_write_byte(state, state->cmd_address2, 0x55, m68k->current_cycle);
			nor_write_byte(state, address, value, m68k->current_cycle);
		}
		state->cmd_state = NOR_CMD_IDLE;
		break;
	}
	return vcontext;
}

void *nor_flash_write_w(uint32_t address, void *vcontext, uint16_t value)
{
	nor_flash_write_b(address, vcontext, value >> 8);
	return nor_flash_write_b(address + 1, vcontext, value);
}
