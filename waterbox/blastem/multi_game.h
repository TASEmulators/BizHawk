#ifndef MULTI_GAME_H_
#define MULTI_GAME_H_
#include "serialize.h"

void *write_multi_game_b(uint32_t address, void *context, uint8_t value);
void *write_multi_game_w(uint32_t address, void *context, uint16_t value);
void multi_game_serialize(genesis_context *gen, serialize_buffer *buf);
void multi_game_deserialize(deserialize_buffer *buf, genesis_context *gen);
#endif //MULTI_GAME_H_
