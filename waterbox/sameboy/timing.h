#ifndef timing_h
#define timing_h
#include "gb.h"

#ifdef GB_INTERNAL
void GB_advance_cycles(GB_gameboy_t *gb, uint8_t cycles);
void GB_set_internal_div_counter(GB_gameboy_t *gb, uint32_t value);
void GB_rtc_run(GB_gameboy_t *gb);
void GB_emulate_timer_glitch(GB_gameboy_t *gb, uint8_t old_tac, uint8_t new_tac);
bool GB_timing_sync_turbo(GB_gameboy_t *gb); /* Returns true if should skip frame */
void GB_timing_sync(GB_gameboy_t *gb);


enum {
    GB_TIMA_RUNNING = 0,
    GB_TIMA_RELOADING = 1,
    GB_TIMA_RELOADED = 2
};
#endif

#endif /* timing_h */
