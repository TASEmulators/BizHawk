#include "gb.h"
#include <assert.h>

void GB_update_joyp(GB_gameboy_t *gb)
{    
    uint8_t key_selection = 0;
    uint8_t previous_state = 0;

    /* Todo: add delay to key selection */
    previous_state = gb->io_registers[GB_IO_JOYP] & 0xF;
    key_selection = (gb->io_registers[GB_IO_JOYP] >> 4) & 3;
    gb->io_registers[GB_IO_JOYP] &= 0xF0;
    switch (key_selection) {
        case 3:
            /* Nothing is wired, all up */
            gb->io_registers[GB_IO_JOYP] |= 0x0F;
            break;

        case 2:
            /* Direction keys */
            for (uint8_t i = 0; i < 4; i++) {
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[0][i]) << i;
            }
            break;

        case 1:
            /* Other keys */
            for (uint8_t i = 0; i < 4; i++) {
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[0][i + 4]) << i;
            }
            break;

        case 0:
            for (uint8_t i = 0; i < 4; i++) {
                gb->io_registers[GB_IO_JOYP] |= (!(gb->keys[0][i] || gb->keys[0][i + 4])) << i;
            }
            break;

        default:
            __builtin_unreachable();
            break;
    }
    
    /* Todo: This assumes the keys *always* bounce, which is incorrect when emulating an SGB */
    if (previous_state != (gb->io_registers[GB_IO_JOYP] & 0xF)) {
        /* The joypad interrupt DOES occur on CGB (Tested on CGB-E), unlike what some documents say. */
        gb->io_registers[GB_IO_IF] |= 0x10;
    }
    
    gb->io_registers[GB_IO_JOYP] |= 0xC0;
}

void GB_set_key_mask(GB_gameboy_t *gb, GB_key_mask_t mask)
{
    memset(gb->keys, 0, sizeof(gb->keys));
    bool *key = &gb->keys[0][0];
    while (mask) {
        if (mask & 1) {
            *key = true;
        }
        mask >>= 1;
        key++;
    }
    
    GB_update_joyp(gb);
}
