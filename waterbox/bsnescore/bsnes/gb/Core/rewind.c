#include "gb.h"
#include <stdint.h>
#include <stddef.h>
#include <stdlib.h>
#include <math.h>

static uint8_t *state_compress(const uint8_t *prev, const uint8_t *data, size_t uncompressed_size)
{
    size_t malloc_size = 0x1000;
    uint8_t *compressed = malloc(malloc_size);
    size_t counter_pos = 0;
    size_t data_pos = sizeof(uint16_t);
    bool prev_mode = true;
    *(uint16_t *)compressed = 0;
#define COUNTER (*(uint16_t *)&compressed[counter_pos])
#define DATA (compressed[data_pos])
    
    while (uncompressed_size) {
        if (prev_mode) {
            if (*data == *prev && COUNTER != 0xffff) {
                COUNTER++;
                data++;
                prev++;
                uncompressed_size--;
            }
            else {
                prev_mode = false;
                counter_pos += sizeof(uint16_t);
                data_pos = counter_pos + sizeof(uint16_t);
                if (data_pos >= malloc_size) {
                    malloc_size *= 2;
                    compressed = realloc(compressed, malloc_size);
                }
                COUNTER = 0;
            }
        }
        else {
            if (*data != *prev && COUNTER != 0xffff) {
                COUNTER++;
                DATA = *data;
                data_pos++;
                data++;
                prev++;
                uncompressed_size--;
                if (data_pos >= malloc_size) {
                    malloc_size *= 2;
                    compressed = realloc(compressed, malloc_size);
                }
            }
            else {
                prev_mode = true;
                counter_pos = data_pos;
                data_pos = counter_pos + sizeof(uint16_t);
                if (counter_pos >= malloc_size - 1) {
                    malloc_size *= 2;
                    compressed = realloc(compressed, malloc_size);
                }
                COUNTER = 0;
            }
        }
    }
    
    return  realloc(compressed, data_pos);
#undef DATA
#undef COUNTER
}


static void state_decompress(const uint8_t *prev, uint8_t *data, uint8_t *dest, size_t uncompressed_size)
{
    size_t counter_pos = 0;
    size_t data_pos = sizeof(uint16_t);
    bool prev_mode = true;
#define COUNTER (*(uint16_t *)&data[counter_pos])
#define DATA (data[data_pos])
    
    while (uncompressed_size) {
        if (prev_mode) {
            if (COUNTER) {
                COUNTER--;
                *(dest++) = *(prev++);
                uncompressed_size--;
            }
            else {
                prev_mode = false;
                counter_pos += sizeof(uint16_t);
                data_pos = counter_pos + sizeof(uint16_t);
            }
        }
        else {
            if (COUNTER) {
                COUNTER--;
                *(dest++) = DATA;
                data_pos++;
                prev++;
                uncompressed_size--;
            }
            else {
                prev_mode = true;
                counter_pos = data_pos;
                data_pos += sizeof(uint16_t);
            }
        }
    }
#undef DATA
#undef COUNTER
}

void GB_rewind_push(GB_gameboy_t *gb)
{
    const size_t save_size = GB_get_save_state_size(gb);
    if (!gb->rewind_sequences) {
        if (gb->rewind_buffer_length) {
            gb->rewind_sequences = malloc(sizeof(*gb->rewind_sequences) * gb->rewind_buffer_length);
            memset(gb->rewind_sequences, 0, sizeof(*gb->rewind_sequences) * gb->rewind_buffer_length);
            gb->rewind_pos = 0;
        }
        else {
            return;
        }
    }
    
    if (gb->rewind_sequences[gb->rewind_pos].pos == GB_REWIND_FRAMES_PER_KEY) {
        gb->rewind_pos++;
        if (gb->rewind_pos == gb->rewind_buffer_length) {
            gb->rewind_pos = 0;
        }
        if (gb->rewind_sequences[gb->rewind_pos].key_state) {
            free(gb->rewind_sequences[gb->rewind_pos].key_state);
            gb->rewind_sequences[gb->rewind_pos].key_state = NULL;
        }
        for (unsigned i = 0; i < GB_REWIND_FRAMES_PER_KEY; i++) {
            if (gb->rewind_sequences[gb->rewind_pos].compressed_states[i]) {
                free(gb->rewind_sequences[gb->rewind_pos].compressed_states[i]);
                gb->rewind_sequences[gb->rewind_pos].compressed_states[i] = 0;
            }
        }
        gb->rewind_sequences[gb->rewind_pos].pos = 0;
    }
    
    if (!gb->rewind_sequences[gb->rewind_pos].key_state) {
        gb->rewind_sequences[gb->rewind_pos].key_state = malloc(save_size);
        GB_save_state_to_buffer(gb, gb->rewind_sequences[gb->rewind_pos].key_state);
    }
    else {
        uint8_t *save_state = malloc(save_size);
        GB_save_state_to_buffer(gb, save_state);
        gb->rewind_sequences[gb->rewind_pos].compressed_states[gb->rewind_sequences[gb->rewind_pos].pos++] =
            state_compress(gb->rewind_sequences[gb->rewind_pos].key_state, save_state, save_size);
        free(save_state);
    }
    
}

bool GB_rewind_pop(GB_gameboy_t *gb)
{
    if (!gb->rewind_sequences || !gb->rewind_sequences[gb->rewind_pos].key_state) {
        return false;
    }
    
    const size_t save_size = GB_get_save_state_size(gb);
    if (gb->rewind_sequences[gb->rewind_pos].pos == 0) {
        GB_load_state_from_buffer(gb, gb->rewind_sequences[gb->rewind_pos].key_state, save_size);
        free(gb->rewind_sequences[gb->rewind_pos].key_state);
        gb->rewind_sequences[gb->rewind_pos].key_state = NULL;
        gb->rewind_pos = gb->rewind_pos == 0? gb->rewind_buffer_length - 1 : gb->rewind_pos - 1;
        return true;
    }
    
    uint8_t *save_state = malloc(save_size);
    state_decompress(gb->rewind_sequences[gb->rewind_pos].key_state,
                     gb->rewind_sequences[gb->rewind_pos].compressed_states[--gb->rewind_sequences[gb->rewind_pos].pos],
                     save_state,
                     save_size);
    free(gb->rewind_sequences[gb->rewind_pos].compressed_states[gb->rewind_sequences[gb->rewind_pos].pos]);
    gb->rewind_sequences[gb->rewind_pos].compressed_states[gb->rewind_sequences[gb->rewind_pos].pos] = NULL;
    GB_load_state_from_buffer(gb, save_state, save_size);
    free(save_state);
    return true;
}

void GB_rewind_free(GB_gameboy_t *gb)
{
    if (!gb->rewind_sequences) return;
    for (unsigned i = 0; i < gb->rewind_buffer_length; i++) {
        if (gb->rewind_sequences[i].key_state) {
            free(gb->rewind_sequences[i].key_state);
        }
        for (unsigned j = 0; j < GB_REWIND_FRAMES_PER_KEY; j++) {
            if (gb->rewind_sequences[i].compressed_states[j]) {
                free(gb->rewind_sequences[i].compressed_states[j]);
            }
        }
    }
    free(gb->rewind_sequences);
    gb->rewind_sequences = NULL;
}

void GB_set_rewind_length(GB_gameboy_t *gb, double seconds)
{
    GB_rewind_free(gb);
    if (seconds == 0) {
        gb->rewind_buffer_length = 0;
    }
    else {
        gb->rewind_buffer_length = (size_t) ceil(seconds * CPU_FREQUENCY / LCDC_PERIOD / GB_REWIND_FRAMES_PER_KEY);
    }
}
