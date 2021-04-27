#ifndef rumble_h
#define rumble_h

#include "gb_struct_def.h"

typedef enum {
    GB_RUMBLE_DISABLED,
    GB_RUMBLE_CARTRIDGE_ONLY,
    GB_RUMBLE_ALL_GAMES
} GB_rumble_mode_t;

#ifdef GB_INTERNAL
void GB_handle_rumble(GB_gameboy_t *gb);
#endif
void GB_set_rumble_mode(GB_gameboy_t *gb, GB_rumble_mode_t mode);

#endif /* rumble_h */
