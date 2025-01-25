#ifndef XBAND_H_
#define XBAND_H_
#include <stdint.h>
#include "serialize.h"

#define XBAND_REGS 0xE0

typedef struct {
	uint16_t cart_space[0x200000];
	uint8_t regs[XBAND_REGS];
	uint8_t kill;
	uint8_t control;
} xband;

uint8_t xband_detect(uint8_t *rom, uint32_t rom_size);
rom_info xband_configure_rom(tern_node *rom_db, void *rom, uint32_t rom_size, void *lock_on, uint32_t lock_on_size, memmap_chunk const *base_map, uint32_t base_chunks);
void xband_serialize(genesis_context *gen, serialize_buffer *buf);
void xband_deserialize(deserialize_buffer *buf, genesis_context *gen);

#endif //XBAND_H_
