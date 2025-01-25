#include "genesis.h"

uint16_t read_sram_w(uint32_t address, m68k_context * context)
{
	genesis_context * gen = context->system;
	address &= gen->save_ram_mask;
	switch(gen->save_type)
	{
	case RAM_FLAG_BOTH:
		return gen->save_storage[address] << 8 | gen->save_storage[address+1];
	case RAM_FLAG_EVEN:
		return gen->save_storage[address >> 1] << 8 | 0xFF;
	case RAM_FLAG_ODD:
		return gen->save_storage[address >> 1] | 0xFF00;
	}
	return 0xFFFF;//We should never get here
}

uint8_t read_sram_b(uint32_t address, m68k_context * context)
{
	genesis_context * gen = context->system;
	address &= gen->save_ram_mask;
	switch(gen->save_type)
	{
	case RAM_FLAG_BOTH:
		return gen->save_storage[address];
	case RAM_FLAG_EVEN:
		if (address & 1) {
			return 0xFF;
		} else {
			return gen->save_storage[address >> 1];
		}
	case RAM_FLAG_ODD:
		if (address & 1) {
			return gen->save_storage[address >> 1];
		} else {
			return 0xFF;
		}
	}
	return 0xFF;//We should never get here
}

m68k_context * write_sram_area_w(uint32_t address, m68k_context * context, uint16_t value)
{
	genesis_context * gen = context->system;
	if ((gen->bank_regs[0] & 0x3) == 1) {
		address &= gen->save_ram_mask;
		switch(gen->save_type)
		{
		case RAM_FLAG_BOTH:
			gen->save_storage[address] = value >> 8;
			gen->save_storage[address+1] = value;
			break;
		case RAM_FLAG_EVEN:
			gen->save_storage[address >> 1] = value >> 8;
			break;
		case RAM_FLAG_ODD:
			gen->save_storage[address >> 1] = value;
			break;
		}
	}
	return context;
}

m68k_context * write_sram_area_b(uint32_t address, m68k_context * context, uint8_t value)
{
	genesis_context * gen = context->system;
	if ((gen->bank_regs[0] & 0x3) == 1) {
		address &= gen->save_ram_mask;
		switch(gen->save_type)
		{
		case RAM_FLAG_BOTH:
			gen->save_storage[address] = value;
			break;
		case RAM_FLAG_EVEN:
			if (!(address & 1)) {
				gen->save_storage[address >> 1] = value;
			}
			break;
		case RAM_FLAG_ODD:
			if (address & 1) {
				gen->save_storage[address >> 1] = value;
			}
			break;
		}
	}
	return context;
}

m68k_context * write_bank_reg_w(uint32_t address, m68k_context * context, uint16_t value)
{
	genesis_context * gen = context->system;
	address &= 0xE;
	address >>= 1;
	gen->bank_regs[address] = value;
	if (!address) {
		if (value & 1) {
			//Used for games that only use the mapper for SRAM
			if (context->mem_pointers[gen->mapper_start_index]) {
				gen->mapper_temp = context->mem_pointers[gen->mapper_start_index];
			}
			context->mem_pointers[gen->mapper_start_index] = NULL;
			//For games that need more than 4MB
			for (int i = 4; i < 8; i++)
			{
				context->mem_pointers[gen->mapper_start_index + i] = NULL;
			}
		} else {
			//Used for games that only use the mapper for SRAM
			if (!context->mem_pointers[gen->mapper_start_index]) {
				context->mem_pointers[gen->mapper_start_index] = gen->mapper_temp;
			}
			//For games that need more than 4MB
			for (int i = 4; i < 8; i++)
			{
				context->mem_pointers[gen->mapper_start_index + i] = gen->cart + 0x40000*gen->bank_regs[i];
			}
		}
	} else if (gen->mapper_type == MAPPER_SEGA) {
		void *new_ptr = gen->cart + 0x40000*value;
		if (context->mem_pointers[gen->mapper_start_index + address] != new_ptr) {
			m68k_invalidate_code_range(gen->m68k, address * 0x80000, (address + 1) * 0x80000);
			context->mem_pointers[gen->mapper_start_index + address] = new_ptr;
		}
	}
	return context;
}

m68k_context * write_bank_reg_b(uint32_t address, m68k_context * context, uint8_t value)
{
	if (address & 1) {
		write_bank_reg_w(address, context, value);
	}
	return context;
}

void sega_mapper_serialize(genesis_context *gen, serialize_buffer *buf)
{
	save_buffer8(buf, gen->bank_regs, sizeof(gen->bank_regs));
}

void sega_mapper_deserialize(deserialize_buffer *buf, genesis_context *gen)
{
	for (int i = 0; i < sizeof(gen->bank_regs); i++)
	{
		write_bank_reg_w(i * 2, gen->m68k, load_int8(buf));
	}
}
