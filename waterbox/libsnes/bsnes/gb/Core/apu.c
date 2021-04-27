#include <stdint.h>
#include <math.h>
#include <string.h>
#include <assert.h>
#include "gb.h"

#define likely(x)   __builtin_expect((x), 1)
#define unlikely(x) __builtin_expect((x), 0)

static const uint8_t duties[] = {
    0, 0, 0, 0, 0, 0, 0, 1,
    1, 0, 0, 0, 0, 0, 0, 1,
    1, 0, 0, 0, 0, 1, 1, 1,
    0, 1, 1, 1, 1, 1, 1, 0,
};

static void refresh_channel(GB_gameboy_t *gb, unsigned index, unsigned cycles_offset)
{
    unsigned multiplier = gb->apu_output.cycles_since_render + cycles_offset - gb->apu_output.last_update[index];
    gb->apu_output.summed_samples[index].left += gb->apu_output.current_sample[index].left * multiplier;
    gb->apu_output.summed_samples[index].right += gb->apu_output.current_sample[index].right * multiplier;
    gb->apu_output.last_update[index] = gb->apu_output.cycles_since_render + cycles_offset;
}

bool GB_apu_is_DAC_enabled(GB_gameboy_t *gb, unsigned index)
{
    if (gb->model >= GB_MODEL_AGB) {
        /* On the AGB, mixing is done digitally, so there are no per-channel
           DACs. Instead, all channels are summed digital regardless of
           whatever the DAC state would be on a CGB or earlier model. */
        return true;
    }
    
    switch (index) {
        case GB_SQUARE_1:
            return gb->io_registers[GB_IO_NR12] & 0xF8;

        case GB_SQUARE_2:
            return gb->io_registers[GB_IO_NR22] & 0xF8;

        case GB_WAVE:
            return gb->apu.wave_channel.enable;

        case GB_NOISE:
            return gb->io_registers[GB_IO_NR42] & 0xF8;
    }

    return false;
}

static uint8_t agb_bias_for_channel(GB_gameboy_t *gb, unsigned index)
{
    if (!gb->apu.is_active[index]) return 0;
    
    switch (index) {
        case GB_SQUARE_1:
            return gb->apu.square_channels[GB_SQUARE_1].current_volume;
        case GB_SQUARE_2:
            return gb->apu.square_channels[GB_SQUARE_2].current_volume;
        case GB_WAVE:
            return 0;
        case GB_NOISE:
            return gb->apu.noise_channel.current_volume;
    }
    return 0;
}

static void update_sample(GB_gameboy_t *gb, unsigned index, int8_t value, unsigned cycles_offset)
{
    if (gb->model >= GB_MODEL_AGB) {
        /* On the AGB, because no analog mixing is done, the behavior of NR51 is a bit different.
           A channel that is not connected to a terminal is idenitcal to a connected channel
           playing PCM sample 0. */
        gb->apu.samples[index] = value;
        
        if (gb->apu_output.sample_rate) {
            unsigned right_volume = (gb->io_registers[GB_IO_NR50] & 7) + 1;
            unsigned left_volume = ((gb->io_registers[GB_IO_NR50] >> 4) & 7) + 1;
            
            if (index == GB_WAVE) {
                /* For some reason, channel 3 is inverted on the AGB */
                value ^= 0xF;
            }
            
            GB_sample_t output;
            uint8_t bias = agb_bias_for_channel(gb, index);
            
            if (gb->io_registers[GB_IO_NR51] & (1 << index)) {
                output.right = (0xf - value * 2 + bias) * right_volume;
            }
            else {
                output.right = 0xf * right_volume;
            }
            
            if (gb->io_registers[GB_IO_NR51] & (0x10 << index)) {
                output.left = (0xf - value * 2 + bias) * left_volume;
            }
            else {
                output.left = 0xf * left_volume;
            }
            
            if (*(uint32_t *)&(gb->apu_output.current_sample[index]) != *(uint32_t *)&output) {
                refresh_channel(gb, index, cycles_offset);
                gb->apu_output.current_sample[index] = output;
            }
        }
        
        return;
    }
    
    if (!GB_apu_is_DAC_enabled(gb, index)) {
        value = gb->apu.samples[index];
    }
    else {
        gb->apu.samples[index] = value;
    }

    if (gb->apu_output.sample_rate) {
        unsigned right_volume = 0;
        if (gb->io_registers[GB_IO_NR51] & (1 << index)) {
            right_volume = (gb->io_registers[GB_IO_NR50] & 7) + 1;
        }
        unsigned left_volume = 0;
        if (gb->io_registers[GB_IO_NR51] & (0x10 << index)) {
            left_volume = ((gb->io_registers[GB_IO_NR50] >> 4) & 7) + 1;
        }
        GB_sample_t output = {(0xf - value * 2) * left_volume, (0xf - value * 2) * right_volume};
        if (*(uint32_t *)&(gb->apu_output.current_sample[index]) != *(uint32_t *)&output) {
            refresh_channel(gb, index, cycles_offset);
            gb->apu_output.current_sample[index] = output;
        }
    }
}

static double smooth(double x)
{
    return 3*x*x - 2*x*x*x;
}

static void render(GB_gameboy_t *gb)
{
    GB_sample_t output = {0, 0};

    UNROLL
    for (unsigned i = 0; i < GB_N_CHANNELS; i++) {
        double multiplier = CH_STEP;
        
        if (gb->model < GB_MODEL_AGB) {
            if (!GB_apu_is_DAC_enabled(gb, i)) {
                gb->apu_output.dac_discharge[i] -= ((double) DAC_DECAY_SPEED) / gb->apu_output.sample_rate;
                if (gb->apu_output.dac_discharge[i] < 0) {
                    multiplier = 0;
                    gb->apu_output.dac_discharge[i] = 0;
                }
                else {
                    multiplier *= smooth(gb->apu_output.dac_discharge[i]);
                }
            }
            else {
                gb->apu_output.dac_discharge[i] += ((double) DAC_ATTACK_SPEED) / gb->apu_output.sample_rate;
                if (gb->apu_output.dac_discharge[i] > 1) {
                    gb->apu_output.dac_discharge[i] = 1;
                }
                else {
                    multiplier *= smooth(gb->apu_output.dac_discharge[i]);
                }
            }
        }

        if (likely(gb->apu_output.last_update[i] == 0)) {
            output.left += gb->apu_output.current_sample[i].left * multiplier;
            output.right += gb->apu_output.current_sample[i].right * multiplier;
        }
        else {
            refresh_channel(gb, i, 0);
            output.left += (signed long) gb->apu_output.summed_samples[i].left * multiplier
                            / gb->apu_output.cycles_since_render;
            output.right += (signed long) gb->apu_output.summed_samples[i].right * multiplier
                            / gb->apu_output.cycles_since_render;
            gb->apu_output.summed_samples[i] = (GB_sample_t){0, 0};
        }
        gb->apu_output.last_update[i] = 0;
    }
    gb->apu_output.cycles_since_render = 0;

    GB_sample_t filtered_output = gb->apu_output.highpass_mode?
        (GB_sample_t) {output.left - gb->apu_output.highpass_diff.left,
                       output.right - gb->apu_output.highpass_diff.right} :
        output;

    switch (gb->apu_output.highpass_mode) {
        case GB_HIGHPASS_OFF:
            gb->apu_output.highpass_diff = (GB_double_sample_t) {0, 0};
            break;
        case GB_HIGHPASS_ACCURATE:
            gb->apu_output.highpass_diff = (GB_double_sample_t)
                {output.left - filtered_output.left * gb->apu_output.highpass_rate,
                    output.right - filtered_output.right * gb->apu_output.highpass_rate};
            break;
        case GB_HIGHPASS_REMOVE_DC_OFFSET: {
            unsigned mask = gb->io_registers[GB_IO_NR51];
            unsigned left_volume = 0;
            unsigned right_volume = 0;
            UNROLL
            for (unsigned i = GB_N_CHANNELS; i--;) {
                if (gb->apu.is_active[i]) {
                    if (mask & 1) {
                        left_volume += (gb->io_registers[GB_IO_NR50] & 7) * CH_STEP * 0xF;
                    }
                    if (mask & 0x10) {
                        right_volume += ((gb->io_registers[GB_IO_NR50] >> 4) & 7) * CH_STEP * 0xF;
                    }
                }
                else {
                    left_volume += gb->apu_output.current_sample[i].left * CH_STEP;
                    right_volume += gb->apu_output.current_sample[i].right * CH_STEP;
                }
                mask >>= 1;
            }
            gb->apu_output.highpass_diff = (GB_double_sample_t)
            {left_volume * (1 - gb->apu_output.highpass_rate) + gb->apu_output.highpass_diff.left * gb->apu_output.highpass_rate,
                right_volume * (1 - gb->apu_output.highpass_rate) + gb->apu_output.highpass_diff.right * gb->apu_output.highpass_rate};

        case GB_HIGHPASS_MAX:;
        }

    }
    
    assert(gb->apu_output.sample_callback);
    gb->apu_output.sample_callback(gb, &filtered_output);
}

static uint16_t new_sweep_sample_length(GB_gameboy_t *gb)
{
    uint16_t delta = gb->apu.shadow_sweep_sample_length >> (gb->io_registers[GB_IO_NR10] & 7);
    if (gb->io_registers[GB_IO_NR10] & 8) {
        return gb->apu.shadow_sweep_sample_length - delta;
    }
    return gb->apu.shadow_sweep_sample_length + delta;
}

static void update_square_sample(GB_gameboy_t *gb, unsigned index)
{
    if (gb->apu.square_channels[index].current_sample_index & 0x80) return;

    uint8_t duty = gb->io_registers[index == GB_SQUARE_1? GB_IO_NR11 :GB_IO_NR21] >> 6;
    update_sample(gb, index,
                  duties[gb->apu.square_channels[index].current_sample_index + duty * 8]?
                  gb->apu.square_channels[index].current_volume : 0,
                  0);
}


/* the effects of NRX2 writes on current volume are not well documented and differ
   between models and variants. The exact behavior can only be verified on CGB as it
   requires the PCM12 register. The behavior implemented here was verified on *my*
   CGB, which might behave differently from other CGB revisions, as well as from the
   DMG, MGB or SGB/2 */
static void nrx2_glitch(uint8_t *volume, uint8_t value, uint8_t old_value)
{
    if (value & 8) {
        (*volume)++;
    }

    if (((value ^ old_value) & 8)) {
        *volume = 0x10 - *volume;
    }

    if ((value & 7) && !(old_value & 7) && *volume && !(value & 8)) {
        (*volume)--;
    }

    if ((old_value & 7) && (value & 8)) {
        (*volume)--;
    }

    (*volume) &= 0xF;
}

static void tick_square_envelope(GB_gameboy_t *gb, enum GB_CHANNELS index)
{
    uint8_t nrx2 = gb->io_registers[index == GB_SQUARE_1? GB_IO_NR12 : GB_IO_NR22];

    if (gb->apu.square_channels[index].volume_countdown || (nrx2 & 7)) {
        if (!gb->apu.square_channels[index].volume_countdown || !--gb->apu.square_channels[index].volume_countdown) {
            if (gb->cgb_double_speed) {
                if (index == GB_SQUARE_1) {
                    gb->apu.pcm_mask[0] &= gb->apu.square_channels[GB_SQUARE_1].current_volume | 0xF1;
                }
                else {
                    gb->apu.pcm_mask[0] &= (gb->apu.square_channels[GB_SQUARE_2].current_volume << 2) | 0x1F;
                }
            }
            
            if ((nrx2 & 8) && gb->apu.square_channels[index].current_volume < 0xF) {
                gb->apu.square_channels[index].current_volume++;
            }

            else if (!(nrx2 & 8) && gb->apu.square_channels[index].current_volume > 0) {
                gb->apu.square_channels[index].current_volume--;
            }

            gb->apu.square_channels[index].volume_countdown = nrx2 & 7;

            if (gb->apu.is_active[index]) {
                update_square_sample(gb, index);
            }
        }
    }
}

static void tick_noise_envelope(GB_gameboy_t *gb)
{
    uint8_t nr42 = gb->io_registers[GB_IO_NR42];

    if (gb->apu.noise_channel.volume_countdown || (nr42 & 7)) {
        if (!gb->apu.noise_channel.volume_countdown || !--gb->apu.noise_channel.volume_countdown) {
            if (gb->cgb_double_speed) {
                gb->apu.pcm_mask[0] &= (gb->apu.noise_channel.current_volume << 2) | 0x1F;
            }
            if ((nr42 & 8) && gb->apu.noise_channel.current_volume < 0xF) {
                gb->apu.noise_channel.current_volume++;
            }

            else if (!(nr42 & 8) && gb->apu.noise_channel.current_volume > 0) {
                gb->apu.noise_channel.current_volume--;
            }

            gb->apu.noise_channel.volume_countdown = nr42 & 7;

            if (gb->apu.is_active[GB_NOISE]) {
                update_sample(gb, GB_NOISE,
                              (gb->apu.noise_channel.lfsr & 1) ?
                              gb->apu.noise_channel.current_volume : 0,
                              0);
            }
        }
    }
}

void GB_apu_div_event(GB_gameboy_t *gb)
{
    if (!gb->apu.global_enable) return;
    if (gb->apu.skip_div_event == GB_SKIP_DIV_EVENT_SKIP) {
        gb->apu.skip_div_event = GB_SKIP_DIV_EVENT_SKIPPED;
        return;
    }
    if (gb->apu.skip_div_event == GB_SKIP_DIV_EVENT_SKIPPED) {
        gb->apu.skip_div_event = GB_SKIP_DIV_EVENT_INACTIVE;
    }
    else {
        gb->apu.div_divider++;
    }

    if ((gb->apu.div_divider & 1) == 0) {
        for (unsigned i = GB_SQUARE_2 + 1; i--;) {
            uint8_t nrx2 = gb->io_registers[i == GB_SQUARE_1? GB_IO_NR12 : GB_IO_NR22];
            if (gb->apu.is_active[i] && gb->apu.square_channels[i].volume_countdown == 0 && (nrx2 & 7)) {
                tick_square_envelope(gb, i);
            }
        }

        if (gb->apu.is_active[GB_NOISE] && gb->apu.noise_channel.volume_countdown == 0 && (gb->io_registers[GB_IO_NR42] & 7)) {
            tick_noise_envelope(gb);
        }
    }

    if ((gb->apu.div_divider & 7) == 0) {
        for (unsigned i = GB_SQUARE_2 + 1; i--;) {
            tick_square_envelope(gb, i);
        }

        tick_noise_envelope(gb);
    }

    if ((gb->apu.div_divider & 1) == 1) {
        for (unsigned i = GB_SQUARE_2 + 1; i--;) {
            if (gb->apu.square_channels[i].length_enabled) {
                if (gb->apu.square_channels[i].pulse_length) {
                    if (!--gb->apu.square_channels[i].pulse_length) {
                        gb->apu.is_active[i] = false;
                        update_sample(gb, i, 0, 0);
                    }
                }
            }
        }

        if (gb->apu.wave_channel.length_enabled) {
            if (gb->apu.wave_channel.pulse_length) {
                if (!--gb->apu.wave_channel.pulse_length) {
                    gb->apu.is_active[GB_WAVE] = false;
                    update_sample(gb, GB_WAVE, 0, 0);
                }
            }
        }

        if (gb->apu.noise_channel.length_enabled) {
            if (gb->apu.noise_channel.pulse_length) {
                if (!--gb->apu.noise_channel.pulse_length) {
                    gb->apu.is_active[GB_NOISE] = false;
                    update_sample(gb, GB_NOISE, 0, 0);
                }
            }
        }
    }

    if ((gb->apu.div_divider & 3) == 3) {
        if (!gb->apu.sweep_enabled) {
            return;
        }
        if (gb->apu.square_sweep_countdown) {
            if (!--gb->apu.square_sweep_countdown) {
                if ((gb->io_registers[GB_IO_NR10] & 0x70) && (gb->io_registers[GB_IO_NR10] & 0x07)) {
                    gb->apu.square_channels[GB_SQUARE_1].sample_length =
                        gb->apu.shadow_sweep_sample_length =
                        gb->apu.new_sweep_sample_length;
                }

                if (gb->io_registers[GB_IO_NR10] & 0x70) {
                    /* Recalculation and overflow check only occurs after a delay */
                    gb->apu.square_sweep_calculate_countdown = 0x13 - gb->apu.lf_div;
                }

                gb->apu.square_sweep_countdown = ((gb->io_registers[GB_IO_NR10] >> 4) & 7);
                if (!gb->apu.square_sweep_countdown) gb->apu.square_sweep_countdown = 8;
            }
        }
    }
}


void GB_apu_run(GB_gameboy_t *gb)
{
    /* Convert 4MHZ to 2MHz. apu_cycles is always divisable by 4. */
    uint8_t cycles = gb->apu.apu_cycles >> 2;
    gb->apu.apu_cycles = 0;
    if (!cycles) return;
        
    if (likely(!gb->stopped || GB_is_cgb(gb))) {
        /* To align the square signal to 1MHz */
        gb->apu.lf_div ^= cycles & 1;
        gb->apu.noise_channel.alignment += cycles;

        if (gb->apu.square_sweep_calculate_countdown) {
            if (gb->apu.square_sweep_calculate_countdown > cycles) {
                gb->apu.square_sweep_calculate_countdown -= cycles;
            }
            else {
                /* APU bug: sweep frequency is checked after adding the sweep delta twice */
                gb->apu.new_sweep_sample_length = new_sweep_sample_length(gb);
                if (gb->apu.new_sweep_sample_length > 0x7ff) {
                    gb->apu.is_active[GB_SQUARE_1] = false;
                    update_sample(gb, GB_SQUARE_1, 0, gb->apu.square_sweep_calculate_countdown - cycles);
                    gb->apu.sweep_enabled = false;
                }
                gb->apu.sweep_decreasing |= gb->io_registers[GB_IO_NR10] & 8;
                gb->apu.square_sweep_calculate_countdown = 0;
            }
        }

        UNROLL
        for (unsigned i = GB_SQUARE_1; i <= GB_SQUARE_2; i++) {
            if (gb->apu.is_active[i]) {
                uint8_t cycles_left = cycles;
                while (unlikely(cycles_left > gb->apu.square_channels[i].sample_countdown)) {
                    cycles_left -= gb->apu.square_channels[i].sample_countdown + 1;
                    gb->apu.square_channels[i].sample_countdown = (gb->apu.square_channels[i].sample_length ^ 0x7FF) * 2 + 1;
                    gb->apu.square_channels[i].current_sample_index++;
                    gb->apu.square_channels[i].current_sample_index &= 0x7;
                    if (cycles_left == 0 && gb->apu.samples[i] == 0) {
                        gb->apu.pcm_mask[0] &= i == GB_SQUARE_1? 0xF0 : 0x0F;
                    }

                    update_square_sample(gb, i);
                }
                if (cycles_left) {
                    gb->apu.square_channels[i].sample_countdown -= cycles_left;
                }
            }
        }

        gb->apu.wave_channel.wave_form_just_read = false;
        if (gb->apu.is_active[GB_WAVE]) {
            uint8_t cycles_left = cycles;
            while (unlikely(cycles_left > gb->apu.wave_channel.sample_countdown)) {
                cycles_left -= gb->apu.wave_channel.sample_countdown + 1;
                gb->apu.wave_channel.sample_countdown = gb->apu.wave_channel.sample_length ^ 0x7FF;
                gb->apu.wave_channel.current_sample_index++;
                gb->apu.wave_channel.current_sample_index &= 0x1F;
                gb->apu.wave_channel.current_sample =
                    gb->apu.wave_channel.wave_form[gb->apu.wave_channel.current_sample_index];
                update_sample(gb, GB_WAVE,
                              gb->apu.wave_channel.current_sample >> gb->apu.wave_channel.shift,
                              cycles - cycles_left);
                gb->apu.wave_channel.wave_form_just_read = true;
            }
            if (cycles_left) {
                gb->apu.wave_channel.sample_countdown -= cycles_left;
                gb->apu.wave_channel.wave_form_just_read = false;
            }
        }

        if (gb->apu.is_active[GB_NOISE]) {
            uint8_t cycles_left = cycles;
            while (unlikely(cycles_left > gb->apu.noise_channel.sample_countdown)) {
                cycles_left -= gb->apu.noise_channel.sample_countdown + 1;
                gb->apu.noise_channel.sample_countdown = gb->apu.noise_channel.sample_length * 4 + 3;

                /* Step LFSR */
                unsigned high_bit_mask = gb->apu.noise_channel.narrow ? 0x4040 : 0x4000;
                bool new_high_bit = (gb->apu.noise_channel.lfsr ^ (gb->apu.noise_channel.lfsr >> 1) ^ 1) & 1;
                gb->apu.noise_channel.lfsr >>= 1;

                if (new_high_bit) {
                    gb->apu.noise_channel.lfsr |= high_bit_mask;
                }
                else {
                    /* This code is not redundent, it's relevant when switching LFSR widths */
                    gb->apu.noise_channel.lfsr &= ~high_bit_mask;
                }

                gb->apu.current_lfsr_sample = gb->apu.noise_channel.lfsr & 1;

                if (cycles_left == 0 && gb->apu.samples[GB_NOISE] == 0) {
                    gb->apu.pcm_mask[1] &= 0x0F;
                }
                
                update_sample(gb, GB_NOISE,
                              gb->apu.current_lfsr_sample ?
                              gb->apu.noise_channel.current_volume : 0,
                              0);
            }
            if (cycles_left) {
                gb->apu.noise_channel.sample_countdown -= cycles_left;
            }
        }
    }

    if (gb->apu_output.sample_rate) {
        gb->apu_output.cycles_since_render += cycles;

        if (gb->apu_output.sample_cycles >= gb->apu_output.cycles_per_sample) {
            gb->apu_output.sample_cycles -= gb->apu_output.cycles_per_sample;
            render(gb);
        }
    }
}
void GB_apu_init(GB_gameboy_t *gb)
{
    memset(&gb->apu, 0, sizeof(gb->apu));
    /* Restore the wave form */
    for (unsigned reg = GB_IO_WAV_START; reg <= GB_IO_WAV_END; reg++) {
        gb->apu.wave_channel.wave_form[(reg - GB_IO_WAV_START) * 2]     = gb->io_registers[reg] >> 4;
        gb->apu.wave_channel.wave_form[(reg - GB_IO_WAV_START) * 2 + 1] = gb->io_registers[reg] & 0xF;
    }
    gb->apu.lf_div = 1;
    /* APU glitch: When turning the APU on while DIV's bit 4 (or 5 in double speed mode) is on,
       the first DIV/APU event is skipped. */
    if (gb->div_counter & (gb->cgb_double_speed? 0x2000 : 0x1000)) {
        gb->apu.skip_div_event = GB_SKIP_DIV_EVENT_SKIP;
        gb->apu.div_divider = 1;
    }
}

uint8_t GB_apu_read(GB_gameboy_t *gb, uint8_t reg)
{
    if (reg == GB_IO_NR52) {
        uint8_t value = 0;
        for (unsigned i = 0; i < GB_N_CHANNELS; i++) {
            value >>= 1;
            if (gb->apu.is_active[i]) {
                value |= 0x8;
            }
        }
        if (gb->apu.global_enable) {
            value |= 0x80;
        }
        value |= 0x70;
        return value;
    }

    static const char read_mask[GB_IO_WAV_END - GB_IO_NR10 + 1] = {
     /* NRX0  NRX1  NRX2  NRX3  NRX4 */
        0x80, 0x3F, 0x00, 0xFF, 0xBF, // NR1X
        0xFF, 0x3F, 0x00, 0xFF, 0xBF, // NR2X
        0x7F, 0xFF, 0x9F, 0xFF, 0xBF, // NR3X
        0xFF, 0xFF, 0x00, 0x00, 0xBF, // NR4X
        0x00, 0x00, 0x70, 0xFF, 0xFF, // NR5X

        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // Unused
        // Wave RAM
        0, /* ... */
    };

    if (reg >= GB_IO_WAV_START && reg <= GB_IO_WAV_END && gb->apu.is_active[GB_WAVE]) {
        if (!GB_is_cgb(gb) && !gb->apu.wave_channel.wave_form_just_read) {
            return 0xFF;
        }
        reg = GB_IO_WAV_START + gb->apu.wave_channel.current_sample_index / 2;
    }

    return gb->io_registers[reg] | read_mask[reg - GB_IO_NR10];
}

void GB_apu_write(GB_gameboy_t *gb, uint8_t reg, uint8_t value)
{
    if (!gb->apu.global_enable && reg != GB_IO_NR52 && reg < GB_IO_WAV_START && (GB_is_cgb(gb) ||
                                                                                (
                                                                                reg != GB_IO_NR11 &&
                                                                                reg != GB_IO_NR21 &&
                                                                                reg != GB_IO_NR31 &&
                                                                                reg != GB_IO_NR41
                                                                                )
                                                                                )) {
        return;
    }

    if (reg >= GB_IO_WAV_START && reg <= GB_IO_WAV_END && gb->apu.is_active[GB_WAVE]) {
        if (!GB_is_cgb(gb) && !gb->apu.wave_channel.wave_form_just_read) {
            return;
        }
        reg = GB_IO_WAV_START + gb->apu.wave_channel.current_sample_index / 2;
    }

    /* Todo: this can and should be rewritten with a function table. */
    switch (reg) {
        /* Globals */
        case GB_IO_NR50:
        case GB_IO_NR51:
            gb->io_registers[reg] = value;
            /* These registers affect the output of all 4 channels (but not the output of the PCM registers).*/
            /* We call update_samples with the current value so the APU output is updated with the new outputs */
            for (unsigned i = GB_N_CHANNELS; i--;) {
                update_sample(gb, i, gb->apu.samples[i], 0);
            }
            break;
        case GB_IO_NR52: {

            uint8_t old_nrx1[] = {
                gb->io_registers[GB_IO_NR11],
                gb->io_registers[GB_IO_NR21],
                gb->io_registers[GB_IO_NR31],
                gb->io_registers[GB_IO_NR41]
            };
            if ((value & 0x80) && !gb->apu.global_enable) {
                GB_apu_init(gb);
                gb->apu.global_enable = true;
            }
            else if (!(value & 0x80) && gb->apu.global_enable)  {
                for (unsigned i = GB_N_CHANNELS; i--;) {
                    update_sample(gb, i, 0, 0);
                }
                memset(&gb->apu, 0, sizeof(gb->apu));
                memset(gb->io_registers + GB_IO_NR10, 0, GB_IO_WAV_START - GB_IO_NR10);
                old_nrx1[0] &= 0x3F;
                old_nrx1[1] &= 0x3F;

                gb->apu.global_enable = false;
            }

            if (!GB_is_cgb(gb) && (value & 0x80)) {
                GB_apu_write(gb, GB_IO_NR11, old_nrx1[0]);
                GB_apu_write(gb, GB_IO_NR21, old_nrx1[1]);
                GB_apu_write(gb, GB_IO_NR31, old_nrx1[2]);
                GB_apu_write(gb, GB_IO_NR41, old_nrx1[3]);
            }
        }
        break;

        /* Square channels */
        case GB_IO_NR10:
            if (gb->apu.sweep_decreasing && !(value & 8)) {
                gb->apu.is_active[GB_SQUARE_1] = false;
                update_sample(gb, GB_SQUARE_1, 0, 0);
                gb->apu.sweep_enabled = false;
                gb->apu.square_sweep_calculate_countdown = 0;
            }
            if ((value & 0x70) == 0) {
                /* Todo: what happens if we set period to 0 while a calculate event is scheduled, and then
                         re-set it to non-zero? */
                gb->apu.square_sweep_calculate_countdown = 0;
            }
            break;

        case GB_IO_NR11:
        case GB_IO_NR21: {
            unsigned index = reg == GB_IO_NR21? GB_SQUARE_2: GB_SQUARE_1;
            gb->apu.square_channels[index].pulse_length = (0x40 - (value & 0x3f));
            if (!gb->apu.global_enable) {
                value &= 0x3f;
            }
            break;
        }

        case GB_IO_NR12:
        case GB_IO_NR22: {
            unsigned index = reg == GB_IO_NR22? GB_SQUARE_2: GB_SQUARE_1;
            if (((value & 0x7) == 0) && ((gb->io_registers[reg] & 0x7) != 0)) {
                /* Envelope disabled */
                gb->apu.square_channels[index].volume_countdown = 0;
            }
            if ((value & 0xF8) == 0) {
                /* This disables the DAC */
                gb->io_registers[reg] = value;
                gb->apu.is_active[index] = false;
                update_sample(gb, index, 0, 0);
            }
            else if (gb->apu.is_active[index]) {
                nrx2_glitch(&gb->apu.square_channels[index].current_volume, value, gb->io_registers[reg]);
                update_square_sample(gb, index);
            }

            break;
        }

        case GB_IO_NR13:
        case GB_IO_NR23: {
            unsigned index = reg == GB_IO_NR23? GB_SQUARE_2: GB_SQUARE_1;
            gb->apu.square_channels[index].sample_length &= ~0xFF;
            gb->apu.square_channels[index].sample_length |= value & 0xFF;
            break;
        }

        case GB_IO_NR14:
        case GB_IO_NR24: {
            unsigned index = reg == GB_IO_NR24? GB_SQUARE_2: GB_SQUARE_1;
            
            /* TODO: When the sample length changes right before being updated, the countdown should change to the
                     old length, but the current sample should not change. Because our write timing isn't accurate to
                     the T-cycle, we hack around it by stepping the sample index backwards. */
            if ((value & 0x80) == 0 && gb->apu.is_active[index]) {
                /* On an AGB, as well as on CGB C and earlier (TODO: Tested: 0, B and C), it behaves slightly different on
                   double speed. */
                if (gb->model == GB_MODEL_CGB_E /* || gb->model == GB_MODEL_CGB_D */ || gb->apu.square_channels[index].sample_countdown & 1) {
                    if (gb->apu.square_channels[index].sample_countdown >> 1 == (gb->apu.square_channels[index].sample_length ^ 0x7FF)) {
                        gb->apu.square_channels[index].current_sample_index--;
                        gb->apu.square_channels[index].current_sample_index &= 7;
                    }
                }
            }

            gb->apu.square_channels[index].sample_length &= 0xFF;
            gb->apu.square_channels[index].sample_length |= (value & 7) << 8;
            if (index == GB_SQUARE_1) {
                gb->apu.shadow_sweep_sample_length =
                    gb->apu.new_sweep_sample_length =
                    gb->apu.square_channels[0].sample_length;
            }
            if (value & 0x80) {
                /* Current sample index remains unchanged when restarting channels 1 or 2. It is only reset by
                   turning the APU off. */
                if (!gb->apu.is_active[index]) {
                    gb->apu.square_channels[index].sample_countdown = (gb->apu.square_channels[index].sample_length ^ 0x7FF) * 2 + 6 - gb->apu.lf_div;
                }
                else {
                    /* Timing quirk: if already active, sound starts 2 (2MHz) ticks earlier.*/
                    gb->apu.square_channels[index].sample_countdown = (gb->apu.square_channels[index].sample_length ^ 0x7FF) * 2 + 4 - gb->apu.lf_div;
                }
                gb->apu.square_channels[index].current_volume = gb->io_registers[index == GB_SQUARE_1 ? GB_IO_NR12 : GB_IO_NR22] >> 4;

                /* The volume changes caused by NRX4 sound start take effect instantly (i.e. the effect the previously
                   started sound). The playback itself is not instant which is why we don't update the sample for other
                   cases. */
                if (gb->apu.is_active[index]) {
                    update_square_sample(gb, index);
                }

                gb->apu.square_channels[index].volume_countdown = gb->io_registers[index == GB_SQUARE_1 ? GB_IO_NR12 : GB_IO_NR22] & 7;

                if ((gb->io_registers[index == GB_SQUARE_1 ? GB_IO_NR12 : GB_IO_NR22] & 0xF8) != 0 && !gb->apu.is_active[index]) {
                    gb->apu.is_active[index] = true;
                    update_sample(gb, index, 0, 0);
                    /* We use the highest bit in current_sample_index to mark this sample is not actually playing yet, */
                    gb->apu.square_channels[index].current_sample_index |= 0x80;
                }
                if (gb->apu.square_channels[index].pulse_length == 0) {
                    gb->apu.square_channels[index].pulse_length = 0x40;
                    gb->apu.square_channels[index].length_enabled = false;
                }

                if (index == GB_SQUARE_1) {
                    gb->apu.sweep_decreasing = false;
                    if (gb->io_registers[GB_IO_NR10] & 7) {
                        /* APU bug: if shift is nonzero, overflow check also occurs on trigger */
                        gb->apu.square_sweep_calculate_countdown = 0x13 - gb->apu.lf_div;
                    }
                    else {
                        gb->apu.square_sweep_calculate_countdown = 0;
                    }
                    gb->apu.sweep_enabled = gb->io_registers[GB_IO_NR10] & 0x77;
                    gb->apu.square_sweep_countdown = ((gb->io_registers[GB_IO_NR10] >> 4) & 7);
                    if (!gb->apu.square_sweep_countdown) gb->apu.square_sweep_countdown = 8;
                }

            }

            /* APU glitch - if length is enabled while the DIV-divider's LSB is 1, tick the length once. */
            if ((value & 0x40) &&
                !gb->apu.square_channels[index].length_enabled &&
                (gb->apu.div_divider & 1) &&
                gb->apu.square_channels[index].pulse_length) {
                gb->apu.square_channels[index].pulse_length--;
                if (gb->apu.square_channels[index].pulse_length == 0) {
                    if (value & 0x80) {
                        gb->apu.square_channels[index].pulse_length = 0x3F;
                    }
                    else {
                        gb->apu.is_active[index] = false;
                        update_sample(gb, index, 0, 0);
                    }
                }
            }
            gb->apu.square_channels[index].length_enabled = value & 0x40;
            break;
        }

        /* Wave channel */
        case GB_IO_NR30:
            gb->apu.wave_channel.enable = value & 0x80;
            if (!gb->apu.wave_channel.enable) {
                gb->apu.is_active[GB_WAVE] = false;
                update_sample(gb, GB_WAVE, 0, 0);
            }
            break;
        case GB_IO_NR31:
            gb->apu.wave_channel.pulse_length = (0x100 - value);
            break;
        case GB_IO_NR32:
            gb->apu.wave_channel.shift = (uint8_t[]){4, 0, 1, 2}[(value >> 5) & 3];
            if (gb->apu.is_active[GB_WAVE]) {
                update_sample(gb, GB_WAVE, gb->apu.wave_channel.current_sample >> gb->apu.wave_channel.shift, 0);
            }
            break;
        case GB_IO_NR33:
            gb->apu.wave_channel.sample_length &= ~0xFF;
            gb->apu.wave_channel.sample_length |= value & 0xFF;
            break;
        case GB_IO_NR34:
            gb->apu.wave_channel.sample_length &= 0xFF;
            gb->apu.wave_channel.sample_length |= (value & 7) << 8;
            if ((value & 0x80)) {
                /* DMG bug: wave RAM gets corrupted if the channel is retriggerred 1 cycle before the APU
                            reads from it. */
                if (!GB_is_cgb(gb) &&
                    gb->apu.is_active[GB_WAVE] &&
                    gb->apu.wave_channel.sample_countdown == 0 &&
                    gb->apu.wave_channel.enable) {
                    unsigned offset = ((gb->apu.wave_channel.current_sample_index + 1) >> 1) & 0xF;

                    /* This glitch varies between models and even specific instances:
                       DMG-B:     Most of them behave as emulated. A few behave differently.
                       SGB:       As far as I know, all tested instances behave as emulated.
                       MGB, SGB2: Most instances behave non-deterministically, a few behave as emulated.

                      Additionally, I believe DMGs, including those we behave differently than emulated,
                      are all deterministic. */
                    if (offset < 4) {
                        gb->io_registers[GB_IO_WAV_START] = gb->io_registers[GB_IO_WAV_START + offset];
                        gb->apu.wave_channel.wave_form[0] = gb->apu.wave_channel.wave_form[offset / 2];
                        gb->apu.wave_channel.wave_form[1] = gb->apu.wave_channel.wave_form[offset / 2 + 1];
                    }
                    else {
                        memcpy(gb->io_registers + GB_IO_WAV_START,
                               gb->io_registers + GB_IO_WAV_START + (offset & ~3),
                               4);
                        memcpy(gb->apu.wave_channel.wave_form,
                               gb->apu.wave_channel.wave_form + (offset & ~3) * 2,
                               8);
                    }
                }
                if (!gb->apu.is_active[GB_WAVE]) {
                    gb->apu.is_active[GB_WAVE] = true;
                    update_sample(gb, GB_WAVE,
                                  gb->apu.wave_channel.current_sample >> gb->apu.wave_channel.shift,
                                  0);
                }
                gb->apu.wave_channel.sample_countdown = (gb->apu.wave_channel.sample_length ^ 0x7FF) + 3;
                gb->apu.wave_channel.current_sample_index = 0;
                if (gb->apu.wave_channel.pulse_length == 0) {
                    gb->apu.wave_channel.pulse_length = 0x100;
                    gb->apu.wave_channel.length_enabled = false;
                }
                /* Note that we don't change the sample just yet! This was verified on hardware. */
            }

            /* APU glitch - if length is enabled while the DIV-divider's LSB is 1, tick the length once. */
            if ((value & 0x40) &&
                !gb->apu.wave_channel.length_enabled &&
                (gb->apu.div_divider & 1) &&
                gb->apu.wave_channel.pulse_length) {
                gb->apu.wave_channel.pulse_length--;
                if (gb->apu.wave_channel.pulse_length == 0) {
                    if (value & 0x80) {
                        gb->apu.wave_channel.pulse_length = 0xFF;
                    }
                    else {
                        gb->apu.is_active[GB_WAVE] = false;
                        update_sample(gb, GB_WAVE, 0, 0);
                    }
                }
            }
            gb->apu.wave_channel.length_enabled = value & 0x40;
            if (gb->apu.is_active[GB_WAVE] && !gb->apu.wave_channel.enable) {
                gb->apu.is_active[GB_WAVE] = false;
                update_sample(gb, GB_WAVE, 0, 0);
            }

            break;

        /* Noise Channel */

        case GB_IO_NR41: {
            gb->apu.noise_channel.pulse_length = (0x40 - (value & 0x3f));
            break;
        }

        case GB_IO_NR42: {
            if (((value & 0x7) == 0) && ((gb->io_registers[reg] & 0x7) != 0)) {
                /* Envelope disabled */
                gb->apu.noise_channel.volume_countdown = 0;
            }
            if ((value & 0xF8) == 0) {
                /* This disables the DAC */
                gb->io_registers[reg] = value;
                gb->apu.is_active[GB_NOISE] = false;
                update_sample(gb, GB_NOISE, 0, 0);
            }
            else if (gb->apu.is_active[GB_NOISE]) {
                nrx2_glitch(&gb->apu.noise_channel.current_volume, value, gb->io_registers[reg]);
                update_sample(gb, GB_NOISE,
                              gb->apu.current_lfsr_sample ?
                              gb->apu.noise_channel.current_volume : 0,
                              0);
            }
            break;
        }

        case GB_IO_NR43: {
            gb->apu.noise_channel.narrow = value & 8;
            unsigned divisor = (value & 0x07) << 1;
            if (!divisor) divisor = 1;
            gb->apu.noise_channel.sample_length = (divisor << (value >> 4)) - 1;

            /* Todo: changing the frequency sometimes delays the next sample. This is probably
               due to how the frequency is actually calculated in the noise channel, which is probably
               not by calculating the effective sample length and counting simiarly to the other channels.
               This is not emulated correctly. */
            break;
        }

        case GB_IO_NR44: {
            if (value & 0x80) {
                gb->apu.noise_channel.sample_countdown = (gb->apu.noise_channel.sample_length) * 2 + 6 - gb->apu.lf_div;

                /* I'm COMPLETELY unsure about this logic, but it passes all relevant tests.
                   See comment in NR43. */
                if ((gb->io_registers[GB_IO_NR43] & 7) && (gb->apu.noise_channel.alignment & 2) == 0) {
                    if ((gb->io_registers[GB_IO_NR43] & 7) == 1) {
                        gb->apu.noise_channel.sample_countdown += 2;
                    }
                    else {
                        gb->apu.noise_channel.sample_countdown -= 2;
                    }
                }
                if (gb->apu.is_active[GB_NOISE]) {
                    gb->apu.noise_channel.sample_countdown += 2;
                }

                gb->apu.noise_channel.current_volume = gb->io_registers[GB_IO_NR42] >> 4;

                /* The volume changes caused by NRX4 sound start take effect instantly (i.e. the effect the previously
                 started sound). The playback itself is not instant which is why we don't update the sample for other
                 cases. */
                if (gb->apu.is_active[GB_NOISE]) {
                    update_sample(gb, GB_NOISE,
                                  gb->apu.current_lfsr_sample ?
                                  gb->apu.noise_channel.current_volume : 0,
                                  0);
                }
                gb->apu.noise_channel.lfsr = 0;
                gb->apu.current_lfsr_sample = false;
                gb->apu.noise_channel.volume_countdown = gb->io_registers[GB_IO_NR42] & 7;

                if (!gb->apu.is_active[GB_NOISE] && (gb->io_registers[GB_IO_NR42] & 0xF8) != 0) {
                    gb->apu.is_active[GB_NOISE] = true;
                    update_sample(gb, GB_NOISE, 0, 0);
                }

                if (gb->apu.noise_channel.pulse_length == 0) {
                    gb->apu.noise_channel.pulse_length = 0x40;
                    gb->apu.noise_channel.length_enabled = false;
                }
            }

            /* APU glitch - if length is enabled while the DIV-divider's LSB is 1, tick the length once. */
            if ((value & 0x40) &&
                !gb->apu.noise_channel.length_enabled &&
                (gb->apu.div_divider & 1) &&
                gb->apu.noise_channel.pulse_length) {
                gb->apu.noise_channel.pulse_length--;
                if (gb->apu.noise_channel.pulse_length == 0) {
                    if (value & 0x80) {
                        gb->apu.noise_channel.pulse_length = 0x3F;
                    }
                    else {
                        gb->apu.is_active[GB_NOISE] = false;
                        update_sample(gb, GB_NOISE, 0, 0);
                    }
                }
            }
            gb->apu.noise_channel.length_enabled = value & 0x40;
            break;
        }

        default:
            if (reg >= GB_IO_WAV_START && reg <= GB_IO_WAV_END) {
                gb->apu.wave_channel.wave_form[(reg - GB_IO_WAV_START) * 2]     = value >> 4;
                gb->apu.wave_channel.wave_form[(reg - GB_IO_WAV_START) * 2 + 1] = value & 0xF;
            }
    }
    gb->io_registers[reg] = value;
}

void GB_set_sample_rate(GB_gameboy_t *gb, unsigned sample_rate)
{

    gb->apu_output.sample_rate = sample_rate;
    if (sample_rate) {
        gb->apu_output.highpass_rate = pow(0.999958,  GB_get_clock_rate(gb) / (double)sample_rate);
    }
    gb->apu_output.rate_set_in_clocks = false;
    GB_apu_update_cycles_per_sample(gb);
}

void GB_set_sample_rate_by_clocks(GB_gameboy_t *gb, double cycles_per_sample)
{

    if (cycles_per_sample == 0) {
        GB_set_sample_rate(gb, 0);
        return;
    }
    gb->apu_output.cycles_per_sample = cycles_per_sample;
    gb->apu_output.sample_rate = GB_get_clock_rate(gb) / cycles_per_sample * 2;
    gb->apu_output.highpass_rate = pow(0.999958, cycles_per_sample);
    gb->apu_output.rate_set_in_clocks = true;
}

void GB_apu_set_sample_callback(GB_gameboy_t *gb, GB_sample_callback_t callback)
{
    gb->apu_output.sample_callback = callback;
}

void GB_set_highpass_filter_mode(GB_gameboy_t *gb, GB_highpass_mode_t mode)
{
    gb->apu_output.highpass_mode = mode;
}

void GB_apu_update_cycles_per_sample(GB_gameboy_t *gb)
{
    if (gb->apu_output.rate_set_in_clocks) return;
    if (gb->apu_output.sample_rate) {
        gb->apu_output.cycles_per_sample = 2 * GB_get_clock_rate(gb) / (double)gb->apu_output.sample_rate; /* 2 * because we use 8MHz units */
    }
}
