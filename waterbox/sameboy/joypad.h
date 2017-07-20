#ifndef joypad_h
#define joypad_h
#include "gb_struct_def.h"

void GB_set_key_state(GB_gameboy_t *gb, int keys);

#ifdef GB_INTERNAL
void GB_update_joyp(GB_gameboy_t *gb);
#endif
#endif /* joypad_h */
