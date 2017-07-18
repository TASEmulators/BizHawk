#include <stdio.h>
#include "gb.h"
#include <assert.h>

void GB_update_joyp(GB_gameboy_t *gb)
{
    uint8_t key_selection = 0;
    uint8_t previous_state = 0;

    /* Todo: add delay to key selection */
    previous_state = gb->io_registers[GB_IO_JOYP] & 0xF;
    key_selection = gb->io_registers[GB_IO_JOYP] >> 4 & 3;
    gb->io_registers[GB_IO_JOYP] &= 0xF0;
    switch (key_selection) {
        case 3:
            /* Nothing is wired, all up */
            gb->io_registers[GB_IO_JOYP] |= 0x0F;
            break;

        case 2:
            /* Direction keys */
			gb->io_registers[GB_IO_JOYP] |= ~gb->keys >> 4 & 0xf;
            break;

        case 1:
            /* Other keys */
			gb->io_registers[GB_IO_JOYP] |= ~gb->keys & 0xf;
            break;

        case 0:
            /* Todo: verifiy this is correct */
			gb->io_registers[GB_IO_JOYP] |= ~(gb->keys >> 4 & gb->keys) & 0xf;
            break;

        default:
            break;
    }
    if (previous_state != (gb->io_registers[GB_IO_JOYP] & 0xF)) {
        /* Todo: disable when emulating CGB */
        gb->io_registers[GB_IO_IF] |= 0x10;
    }
    gb->io_registers[GB_IO_JOYP] |= 0xC0; // No SGB support
}

void GB_set_key_state(GB_gameboy_t *gb, int keys)
{
    gb->keys = keys;
}
