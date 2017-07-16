#include "gb.h"
#ifdef _WIN32
#define _WIN32_WINNT 0x0500
#include <Windows.h>
#else
#include <sys/time.h>
#endif

static int64_t get_nanoseconds(void)
{
#ifndef _WIN32
    struct timeval now;
    gettimeofday(&now, NULL);
    return (now.tv_usec) * 1000 + now.tv_sec * 1000000000L;
#else
    FILETIME time;
    GetSystemTimeAsFileTime(&time);
    return (((int64_t)time.dwHighDateTime << 32) | time.dwLowDateTime) * 100L;
#endif
}

static void nsleep(uint64_t nanoseconds)
{
#ifndef _WIN32
    struct timespec sleep = {0, nanoseconds};
    nanosleep(&sleep, NULL);
#else
    HANDLE timer;
    LARGE_INTEGER time;
    timer = CreateWaitableTimer(NULL, true, NULL);
    time.QuadPart = -(nanoseconds / 100L);
    SetWaitableTimer(timer, &time, 0, NULL, NULL, false);
    WaitForSingleObject(timer, INFINITE);
    CloseHandle(timer);
#endif
}

bool GB_timing_sync_turbo(GB_gameboy_t *gb)
{
    if (!gb->turbo_dont_skip) {
        int64_t nanoseconds = get_nanoseconds();
        if (nanoseconds <= gb->last_sync + FRAME_LENGTH) {
            return true;
        }
        gb->last_sync = nanoseconds;
    }
    return false;
}

void GB_timing_sync(GB_gameboy_t *gb)
{
    if (gb->turbo) {
        gb->cycles_since_last_sync = 0;
        return;
    }
    /* Prevent syncing if not enough time has passed.*/
    if (gb->cycles_since_last_sync < LCDC_PERIOD / 4) return;

    uint64_t target_nanoseconds = gb->cycles_since_last_sync * FRAME_LENGTH / LCDC_PERIOD;
    int64_t nanoseconds = get_nanoseconds();
    if (labs((signed long)(nanoseconds - gb->last_sync)) < target_nanoseconds ) {
        nsleep(target_nanoseconds  + gb->last_sync - nanoseconds);
        gb->last_sync += target_nanoseconds;
    }
    else {
        gb->last_sync = nanoseconds;
    }

    gb->cycles_since_last_sync = 0;
}

static void GB_ir_run(GB_gameboy_t *gb)
{
    if (gb->ir_queue_length == 0) return;
    if (gb->cycles_since_input_ir_change >= gb->ir_queue[0].delay) {
        gb->cycles_since_input_ir_change -= gb->ir_queue[0].delay;
        gb->infrared_input = gb->ir_queue[0].state;
        gb->ir_queue_length--;
        memmove(&gb->ir_queue[0], &gb->ir_queue[1], sizeof(gb->ir_queue[0]) * (gb->ir_queue_length));
    }
}

static void advance_tima_state_machine(GB_gameboy_t *gb)
{
    if (gb->tima_reload_state == GB_TIMA_RELOADED) {
        gb->tima_reload_state = GB_TIMA_RUNNING;
    }
    else if (gb->tima_reload_state == GB_TIMA_RELOADING) {
        gb->tima_reload_state = GB_TIMA_RELOADED;
    }
}

void GB_advance_cycles(GB_gameboy_t *gb, uint8_t cycles)
{
    // Affected by speed boost
    gb->dma_cycles += cycles;

    advance_tima_state_machine(gb);
    for (int i = 0; i < cycles; i += 4) {
        GB_set_internal_div_counter(gb, gb->div_cycles + 4);
    }

    if (cycles > 4) {
        advance_tima_state_machine(gb);
        if (cycles > 8) {
            advance_tima_state_machine(gb);
        }
    }

    uint16_t previous_serial_cycles = gb->serial_cycles;
    gb->serial_cycles += cycles;
    if (gb->serial_length) {
        if ((gb->serial_cycles & gb->serial_length) != (previous_serial_cycles & gb->serial_length)) {
            gb->serial_length = 0;
            gb->io_registers[GB_IO_SC] &= ~0x80;
            /* TODO: Does SB "update" bit by bit? */
            if (gb->serial_transfer_end_callback) {
                gb->io_registers[GB_IO_SB] = gb->serial_transfer_end_callback(gb);
            }
            else {
                gb->io_registers[GB_IO_SB] = 0xFF;
            }
            
            gb->io_registers[GB_IO_IF] |= 8;
        }
    }

    gb->debugger_ticks += cycles;

    if (gb->cgb_double_speed) {
        cycles >>=1;
    }

    // Not affected by speed boost
    gb->hdma_cycles += cycles;
    gb->apu_sample_cycles += cycles;
    gb->apu_subsample_cycles += cycles;
    gb->apu.apu_cycles += cycles;
    gb->cycles_since_ir_change += cycles;
    gb->cycles_since_input_ir_change += cycles;
    gb->cycles_since_last_sync += cycles;
    GB_dma_run(gb);
    GB_hdma_run(gb);
    GB_apu_run(gb);
    GB_display_run(gb, cycles);
    GB_ir_run(gb);
}

/* Standard Timers */
static const unsigned int GB_TAC_RATIOS[] = {1024, 16, 64, 256};

static void increase_tima(GB_gameboy_t *gb)
{
    gb->io_registers[GB_IO_TIMA]++;
    if (gb->io_registers[GB_IO_TIMA] == 0) {
        gb->io_registers[GB_IO_TIMA] = gb->io_registers[GB_IO_TMA];
        gb->io_registers[GB_IO_IF] |= 4;
        gb->tima_reload_state = GB_TIMA_RELOADING;
    }
}

static bool counter_overflow_check(uint32_t old, uint32_t new, uint32_t max)
{
    return (old & (max >> 1)) && !(new & (max >> 1));
}

void GB_set_internal_div_counter(GB_gameboy_t *gb, uint32_t value)
{
    /* TIMA increases when a specific high-bit becomes a low-bit. */
    value &= INTERNAL_DIV_CYCLES - 1;
    if ((gb->io_registers[GB_IO_TAC] & 4) &&
        counter_overflow_check(gb->div_cycles, value, GB_TAC_RATIOS[gb->io_registers[GB_IO_TAC] & 3])) {
        increase_tima(gb);
    }
    gb->div_cycles = value;
}

/* 
   This glitch is based on the expected results of mooneye-gb rapid_toggle test.
   This glitch happens because how TIMA is increased, see GB_set_internal_div_counter.
   According to GiiBiiAdvance, GBC's behavior is different, but this was not tested or implemented.
*/
void GB_emulate_timer_glitch(GB_gameboy_t *gb, uint8_t old_tac, uint8_t new_tac)
{
    /* Glitch only happens when old_tac is enabled. */
    if (!(old_tac & 4)) return;

    unsigned int old_clocks = GB_TAC_RATIOS[old_tac & 3];
    unsigned int new_clocks = GB_TAC_RATIOS[new_tac & 3];

    /* The bit used for overflow testing must have been 1 */
    if (gb->div_cycles & (old_clocks >> 1)) {
        /* And now either the timer must be disabled, or the new bit used for overflow testing be 0. */
        if (!(new_tac & 4) || gb->div_cycles & (new_clocks >> 1)) {
            increase_tima(gb);
        }
    }
}

void GB_rtc_run(GB_gameboy_t *gb)
{
    if ((gb->rtc_real.high & 0x40) == 0) { /* is timer running? */
        time_t current_time = time(NULL);
        while (gb->last_rtc_second < current_time) {
            gb->last_rtc_second++;
            if (++gb->rtc_real.seconds == 60)
            {
                gb->rtc_real.seconds = 0;
                if (++gb->rtc_real.minutes == 60)
                {
                    gb->rtc_real.minutes = 0;
                    if (++gb->rtc_real.hours == 24)
                    {
                        gb->rtc_real.hours = 0;
                        if (++gb->rtc_real.days == 0)
                        {
                            if (gb->rtc_real.high & 1) /* Bit 8 of days*/
                            {
                                gb->rtc_real.high |= 0x80; /* Overflow bit */
                            }
                            gb->rtc_real.high ^= 1;
                        }
                    }
                }
            }
        }
    }
}
