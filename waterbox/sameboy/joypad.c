#include <stdio.h>
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
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[i]) << i;
            }
            /* Forbid pressing two opposing keys, this breaks a lot of games; even if it's somewhat possible. */
            if (!(gb->io_registers[GB_IO_JOYP] & 1)) {
                gb->io_registers[GB_IO_JOYP] |= 2;
            }
            if (!(gb->io_registers[GB_IO_JOYP] & 4)) {
                gb->io_registers[GB_IO_JOYP] |= 8;
            }
            break;

        case 1:
            /* Other keys */
            for (uint8_t i = 0; i < 4; i++) {
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[i + 4]) << i;
            }
            break;

        case 0:
            /* Todo: verifiy this is correct */
            for (uint8_t i = 0; i < 4; i++) {
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[i]) << i;
                gb->io_registers[GB_IO_JOYP] |= (!gb->keys[i + 4]) << i;
            }
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

void GB_set_key_state(GB_gameboy_t *gb, GB_key_t index, bool pressed)
{
    assert(index >= 0 && index < GB_KEY_MAX);
    gb->keys[index] = pressed;
}
