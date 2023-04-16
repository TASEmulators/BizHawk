#ifndef joypad_h
#define joypad_h
#include "gb_struct_def.h"
#include <stdbool.h>

typedef enum {
    GB_KEY_RIGHT,
    GB_KEY_LEFT,
    GB_KEY_UP,
    GB_KEY_DOWN,
    GB_KEY_A,
    GB_KEY_B,
    GB_KEY_SELECT,
    GB_KEY_START,
    GB_KEY_MAX
} GB_key_t;

void GB_set_key_state(GB_gameboy_t *gb, GB_key_t index, bool pressed);
void GB_set_key_state_for_player(GB_gameboy_t *gb, GB_key_t index, unsigned player, bool pressed);
void GB_icd_set_joyp(GB_gameboy_t *gb, uint8_t value);

#ifdef GB_INTERNAL
void GB_update_joyp(GB_gameboy_t *gb);
#endif
#endif /* joypad_h */
