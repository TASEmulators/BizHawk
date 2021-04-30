#include "gb.h"
#include <time.h>

static inline uint8_t int_to_bcd(uint8_t i)
{
    return (i % 10) + ((i / 10) << 4);
}

static inline uint8_t bcd_to_int(uint8_t i)
{
    return (i & 0xF) + (i >> 4) * 10;
}

/*
    Note: This peripheral was never released. This is a hacky software reimplementation of it that allows
          reaccessing all of the features present in Workboy's ROM. Some of the implementation details are
          obviously wrong, but without access to the actual hardware, this is the best I can do.
*/

static void serial_start(GB_gameboy_t *gb, bool bit_received)
{
    gb->workboy.byte_being_received <<= 1;
    gb->workboy.byte_being_received |= bit_received;
    gb->workboy.bits_received++;
    if (gb->workboy.bits_received == 8) {
        gb->workboy.byte_to_send = 0;
        if (gb->workboy.mode != 'W' && gb->workboy.byte_being_received == 'R') {
            gb->workboy.byte_to_send = 'D';
            gb->workboy.key = GB_WORKBOY_NONE;
            gb->workboy.mode = gb->workboy.byte_being_received;
            gb->workboy.buffer_index = 1;
            
            time_t time = gb->workboy_get_time_callback(gb);
            struct tm tm;
            tm = *localtime(&time);
            memset(gb->workboy.buffer, 0, sizeof(gb->workboy.buffer));
            
            gb->workboy.buffer[0] = 4; // Unknown, unused, but appears to be expected to be 4
            gb->workboy.buffer[2] = int_to_bcd(tm.tm_sec); // Seconds, BCD
            gb->workboy.buffer[3] = int_to_bcd(tm.tm_min); // Minutes, BCD
            gb->workboy.buffer[4] = int_to_bcd(tm.tm_hour); // Hours, BCD
            gb->workboy.buffer[5] = int_to_bcd(tm.tm_mday); // Days, BCD. Upper most 2 bits are added to Year for some reason
            gb->workboy.buffer[6] = int_to_bcd(tm.tm_mon + 1); // Months, BCD
            gb->workboy.buffer[0xF] = tm.tm_year; // Years, plain number, since 1900

        }
        else if (gb->workboy.mode != 'W' && gb->workboy.byte_being_received == 'W') {
            gb->workboy.byte_to_send = 'D'; // It is actually unknown what this value should be
            gb->workboy.key = GB_WORKBOY_NONE;
            gb->workboy.mode = gb->workboy.byte_being_received;
            gb->workboy.buffer_index = 0;
        }
        else if (gb->workboy.mode != 'W' && (gb->workboy.byte_being_received == 'O' || gb->workboy.mode == 'O')) {
            gb->workboy.mode = 'O';
            gb->workboy.byte_to_send = gb->workboy.key;
            if (gb->workboy.key != GB_WORKBOY_NONE) {
                if (gb->workboy.key & GB_WORKBOY_REQUIRE_SHIFT) {
                    gb->workboy.key &= ~GB_WORKBOY_REQUIRE_SHIFT;
                    if (gb->workboy.shift_down) {
                        gb->workboy.byte_to_send = gb->workboy.key;
                        gb->workboy.key = GB_WORKBOY_NONE;
                    }
                    else {
                        gb->workboy.byte_to_send = GB_WORKBOY_SHIFT_DOWN;
                        gb->workboy.shift_down = true;
                    }
                }
                else if (gb->workboy.key & GB_WORKBOY_FORBID_SHIFT) {
                    gb->workboy.key &= ~GB_WORKBOY_FORBID_SHIFT;
                    if (!gb->workboy.shift_down) {
                        gb->workboy.byte_to_send = gb->workboy.key;
                        gb->workboy.key = GB_WORKBOY_NONE;
                    }
                    else {
                        gb->workboy.byte_to_send = GB_WORKBOY_SHIFT_UP;
                        gb->workboy.shift_down = false;
                    }
                }
                else {
                    if (gb->workboy.key == GB_WORKBOY_SHIFT_DOWN) {
                        gb->workboy.shift_down = true;
                        gb->workboy.user_shift_down = true;
                    }
                    else if (gb->workboy.key == GB_WORKBOY_SHIFT_UP) {
                        gb->workboy.shift_down = false;
                        gb->workboy.user_shift_down = false;
                    }
                    gb->workboy.byte_to_send = gb->workboy.key;
                    gb->workboy.key = GB_WORKBOY_NONE;
                }
            }
        }
        else if (gb->workboy.mode == 'R') {
            if (gb->workboy.buffer_index / 2  >= sizeof(gb->workboy.buffer)) {
                gb->workboy.byte_to_send = 0;
            }
            else {
                if (gb->workboy.buffer_index & 1) {
                    gb->workboy.byte_to_send = "0123456789ABCDEF"[gb->workboy.buffer[gb->workboy.buffer_index / 2] & 0xF];
                }
                else {
                    gb->workboy.byte_to_send = "0123456789ABCDEF"[gb->workboy.buffer[gb->workboy.buffer_index / 2] >> 4];
                }
                gb->workboy.buffer_index++;
            }
        }
        else if (gb->workboy.mode == 'W') {
            gb->workboy.byte_to_send = 'D';
            if (gb->workboy.buffer_index < 2) {
                gb->workboy.buffer_index++;
            }
            else if ((gb->workboy.buffer_index - 2) < sizeof(gb->workboy.buffer)) {
                gb->workboy.buffer[gb->workboy.buffer_index - 2] = gb->workboy.byte_being_received;
                gb->workboy.buffer_index++;
                if (gb->workboy.buffer_index - 2 == sizeof(gb->workboy.buffer)) {
                    struct tm tm = {0,};
                    tm.tm_sec = bcd_to_int(gb->workboy.buffer[7]);
                    tm.tm_min = bcd_to_int(gb->workboy.buffer[8]);
                    tm.tm_hour = bcd_to_int(gb->workboy.buffer[9]);
                    tm.tm_mday = bcd_to_int(gb->workboy.buffer[0xA]);
                    tm.tm_mon = bcd_to_int(gb->workboy.buffer[0xB] & 0x3F) - 1;
                    tm.tm_year = (uint8_t)(gb->workboy.buffer[0x14] + (gb->workboy.buffer[0xA] >> 6)); // What were they thinking?
                    gb->workboy_set_time_callback(gb, mktime(&tm));
                    gb->workboy.mode = 'O';
                }
            }
        }
        gb->workboy.bits_received = 0;
        gb->workboy.byte_being_received = 0;
    }
}

static bool serial_end(GB_gameboy_t *gb)
{
    bool ret = gb->workboy.bit_to_send;
    gb->workboy.bit_to_send = gb->workboy.byte_to_send & 0x80;
    gb->workboy.byte_to_send <<= 1;
    return ret;
}

void GB_connect_workboy(GB_gameboy_t *gb,
                        GB_workboy_set_time_callback set_time_callback,
                        GB_workboy_get_time_callback get_time_callback)
{
    memset(&gb->workboy, 0, sizeof(gb->workboy));
    GB_set_serial_transfer_bit_start_callback(gb, serial_start);
    GB_set_serial_transfer_bit_end_callback(gb, serial_end);
    gb->workboy_set_time_callback = set_time_callback;
    gb->workboy_get_time_callback = get_time_callback;
}

bool GB_workboy_is_enabled(GB_gameboy_t *gb)
{
    return gb->workboy.mode;
}

void GB_workboy_set_key(GB_gameboy_t *gb, uint8_t key)
{
    if (gb->workboy.user_shift_down != gb->workboy.shift_down &&
        (key & (GB_WORKBOY_REQUIRE_SHIFT | GB_WORKBOY_FORBID_SHIFT)) == 0) {
        if (gb->workboy.user_shift_down) {
            key |= GB_WORKBOY_REQUIRE_SHIFT;
        }
        else {
            key |= GB_WORKBOY_FORBID_SHIFT;
        }
    }
    gb->workboy.key = key;
}
