#ifndef REALTEC_H_
#define REALTEC_H_
#include "serialize.h"

uint8_t realtec_detect(uint8_t *rom, uint32_t rom_size);
rom_info realtec_configure_rom(uint8_t *rom, uint32_t rom_size, memmap_chunk const *base_map, uint32_t base_chunks);
void realtec_serialize(genesis_context *gen, serialize_buffer *buf);
void realtec_deserialize(deserialize_buffer *buf, genesis_context *gen);

#endif //REALTEC_H_
