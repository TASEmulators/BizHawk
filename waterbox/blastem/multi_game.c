#include "genesis.h"

void *write_multi_game_b(uint32_t address, void *vcontext, uint8_t value)
{
	m68k_context *context = vcontext;
	genesis_context *gen = context->system;
	gen->bank_regs[0] = address;
	uint32_t base = (address & 0x3F) << 16, start = 0, end = 0x400000;
	//find the memmap chunk, so we can properly mask the base value
	for (int i = 0; i < context->options->gen.memmap_chunks; i++)
	{
		if (context->options->gen.memmap[i].flags & MMAP_PTR_IDX) {
			base &= context->options->gen.memmap[i].mask;
			start = context->options->gen.memmap[i].start;
			end = context->options->gen.memmap[i].end;
			break;
		}
	}
	context->mem_pointers[gen->mapper_start_index] = gen->cart + base/2;
	m68k_invalidate_code_range(context, start, end);
	return vcontext;
}

void *write_multi_game_w(uint32_t address, void *context, uint16_t value)
{
	return write_multi_game_b(address, context, value);
}

void multi_game_serialize(genesis_context *gen, serialize_buffer *buf)
{
	save_int8(buf, gen->bank_regs[0]);
}

void multi_game_deserialize(deserialize_buffer *buf, genesis_context *gen)
{
	write_multi_game_b(load_int8(buf), gen->m68k, 0);
}
