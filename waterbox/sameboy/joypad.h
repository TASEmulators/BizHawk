#ifndef joypad_h
#define joypad_h
#include "gb_struct_def.h"

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

#ifdef GB_INTERNAL
void GB_update_joyp(GB_gameboy_t *gb);
#endif
#endif /* joypad_h */
