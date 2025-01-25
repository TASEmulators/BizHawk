#ifndef SEGA_MAPPER_H_
#define SEGA_MAPPER_H_
#include "serialize.h"

uint16_t read_sram_w(uint32_t address, m68k_context * context);
uint8_t read_sram_b(uint32_t address, m68k_context * context);
m68k_context * write_sram_area_w(uint32_t address, m68k_context * context, uint16_t value);
m68k_context * write_sram_area_b(uint32_t address, m68k_context * context, uint8_t value);
m68k_context * write_bank_reg_w(uint32_t address, m68k_context * context, uint16_t value);
m68k_context * write_bank_reg_b(uint32_t address, m68k_context * context, uint8_t value);
void sega_mapper_serialize(genesis_context *gen, serialize_buffer *buf);
void sega_mapper_deserialize(deserialize_buffer *buf, genesis_context *gen);

#endif //SEGA_MAPPER_H_
