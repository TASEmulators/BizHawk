#include <stdint.h>
#include <math.h>
#include <string.h>
#include "gb.h"

#undef max
#define max(a,b) \
({ __typeof__ (a) _a = (a); \
__typeof__ (b) _b = (b); \
_a > _b ? _a : _b; })

#undef min
#define min(a,b) \
({ __typeof__ (a) _a = (a); \
__typeof__ (b) _b = (b); \
_a < _b ? _a : _b; })

#define APU_FREQUENCY 0x80000

static int16_t generate_square(uint64_t phase, uint32_t wave_length, int16_t amplitude, uint8_t duty)
{
    if (!wave_length) return 0;
    if (phase % wave_length > wave_length * duty / 8) {
        return amplitude;
    }
    return 0;
}

static int16_t generate_wave(uint64_t phase, uint32_t wave_length, int16_t amplitude, int8_t *wave, uint8_t shift)
{
    if (!wave_length) wave_length = 1;
    phase = phase % wave_length;
    return ((wave[(int)(phase * 32 / wave_length)]) >> shift) * (int)amplitude / 0xF;
}

static int16_t generate_noise(int16_t amplitude, uint16_t lfsr)
{
    if (lfsr & 1) {
        return amplitude;
    }
    return 0;
}

static int16_t step_lfsr(uint16_t lfsr, bool uses_7_bit)
{
    bool xor = (lfsr & 1) ^ ((lfsr & 2) >> 1);
    lfsr >>= 1;
    if (xor) {
        lfsr |= 0x4000;
    }
    if (uses_7_bit) {
        lfsr &= ~0x40;
        if (xor) {
            lfsr |= 0x40;
        }
    }
    return lfsr;
}

/* General Todo: The APU emulation seems to fail many accuracy tests. It might require a rewrite with
   these tests in mind. */

static void GB_apu_run_internal(GB_gameboy_t *gb)
{
    while (!__sync_bool_compare_and_swap(&gb->apu_lock, false, true));
    uint32_t steps = gb->apu.apu_cycles / (CPU_FREQUENCY/APU_FREQUENCY);
    if (!steps) goto exit;

    gb->apu.apu_cycles %= (CPU_FREQUENCY/APU_FREQUENCY);
    for (uint8_t i = 0; i < 4; i++) {
        /* Phase */
        gb->apu.wave_channels[i].phase += steps;
        while (gb->apu.wave_channels[i].wave_length && gb->apu.wave_channels[i].phase >= gb->apu.wave_channels[i].wave_length) {
            if (i == 3) {
                gb->apu.lfsr = step_lfsr(gb->apu.lfsr, gb->apu.lfsr_7_bit);
            }

            gb->apu.wave_channels[i].phase -= gb->apu.wave_channels[i].wave_length;
        }
        /* Stop on Length */
        if (gb->apu.wave_channels[i].stop_on_length) {
            if (gb->apu.wave_channels[i].sound_length > 0) {
                gb->apu.wave_channels[i].sound_length -= steps;
            }
            if (gb->apu.wave_channels[i].sound_length <= 0) {
                gb->apu.wave_channels[i].amplitude = 0;
                gb->apu.wave_channels[i].is_playing = false;
                gb->apu.wave_channels[i].sound_length = i == 2? APU_FREQUENCY : APU_FREQUENCY / 4;
            }
        }
    }

    gb->apu.envelope_step_timer += steps;
    while (gb->apu.envelope_step_timer >= APU_FREQUENCY / 64) {
        gb->apu.envelope_step_timer -= APU_FREQUENCY / 64;
        for (uint8_t i = 0; i < 4; i++) {
            if (gb->apu.wave_channels[i].envelope_steps && !--gb->apu.wave_channels[i].cur_envelope_steps) {
                gb->apu.wave_channels[i].amplitude = min(max(gb->apu.wave_channels[i].amplitude + gb->apu.wave_channels[i].envelope_direction * CH_STEP, 0), MAX_CH_AMP);
                gb->apu.wave_channels[i].cur_envelope_steps = gb->apu.wave_channels[i].envelope_steps;
            }
        }
    }

    gb->apu.sweep_step_timer += steps;
    while (gb->apu.sweep_step_timer >= APU_FREQUENCY / 128) {
        gb->apu.sweep_step_timer -= APU_FREQUENCY / 128;
        if (gb->apu.wave_channels[0].sweep_steps && !--gb->apu.wave_channels[0].cur_sweep_steps) {

            // Convert back to GB format
            uint16_t temp = 2048 - gb->apu.wave_channels[0].wave_length / (APU_FREQUENCY / 131072);

            // Apply sweep
            temp = temp + gb->apu.wave_channels[0].sweep_direction *
                   (temp / (1 << gb->apu.wave_channels[0].sweep_shift));
            if (temp > 2047) {
                temp = 0;
            }

            // Back to frequency
            gb->apu.wave_channels[0].wave_length =  (2048 - temp) *  (APU_FREQUENCY / 131072);


            gb->apu.wave_channels[0].cur_sweep_steps = gb->apu.wave_channels[0].sweep_steps;
        }
    }
exit:
    gb->apu_lock = false;
}

void GB_apu_get_samples_and_update_pcm_regs(GB_gameboy_t *gb, GB_sample_t *samples)
{
    GB_apu_run_internal(gb);

    samples->left = samples->right = 0;
    if (!gb->apu.global_enable) {
        return;
    }

    gb->io_registers[GB_IO_PCM_12] = 0;
    gb->io_registers[GB_IO_PCM_34] = 0;

    {
        int16_t sample = generate_square(gb->apu.wave_channels[0].phase,
                                         gb->apu.wave_channels[0].wave_length,
                                         gb->apu.wave_channels[0].amplitude,
                                         gb->apu.wave_channels[0].duty);
        if (gb->apu.wave_channels[0].left_on ) samples->left  += sample;
        if (gb->apu.wave_channels[0].right_on) samples->right += sample;
        gb->io_registers[GB_IO_PCM_12] = ((int)sample) * 0xF / MAX_CH_AMP;
    }

    {
        int16_t sample = generate_square(gb->apu.wave_channels[1].phase,
                                         gb->apu.wave_channels[1].wave_length,
                                         gb->apu.wave_channels[1].amplitude,
                                         gb->apu.wave_channels[1].duty);
        if (gb->apu.wave_channels[1].left_on ) samples->left  += sample;
        if (gb->apu.wave_channels[1].right_on) samples->right += sample;
        gb->io_registers[GB_IO_PCM_12] |= (((int)sample) * 0xF / MAX_CH_AMP) << 4;
    }

    if (gb->apu.wave_channels[2].is_playing)
    {
        int16_t sample = generate_wave(gb->apu.wave_channels[2].phase,
                                       gb->apu.wave_channels[2].wave_length,
                                       MAX_CH_AMP,
                                       gb->apu.wave_form,
                                       gb->apu.wave_shift);
        if (gb->apu.wave_channels[2].left_on ) samples->left  += sample;
        if (gb->apu.wave_channels[2].right_on) samples->right += sample;
        gb->io_registers[GB_IO_PCM_34] = ((int)sample) * 0xF / MAX_CH_AMP;
    }

    {
        int16_t sample = generate_noise(gb->apu.wave_channels[3].amplitude,
                                        gb->apu.lfsr);
        if (gb->apu.wave_channels[3].left_on ) samples->left  += sample;
        if (gb->apu.wave_channels[3].right_on) samples->right += sample;
        gb->io_registers[GB_IO_PCM_34] |= (((int)sample) * 0xF / MAX_CH_AMP) << 4;
    }

    samples->left = (int) samples->left * gb->apu.left_volume / 7;
    samples->right = (int) samples->right * gb->apu.right_volume / 7;
}

void GB_apu_run(GB_gameboy_t *gb)
{
    if (gb->sample_rate == 0) {
        if (gb->apu.apu_cycles > 0xFF00) {
            GB_sample_t dummy;
            GB_apu_get_samples_and_update_pcm_regs(gb, &dummy);
        }
        return;
    }
    while (gb->audio_copy_in_progress);
    double ticks_per_sample = (double) CPU_FREQUENCY / gb->sample_rate;

    if (gb->audio_quality == 0) {
        GB_sample_t sample;
        GB_apu_get_samples_and_update_pcm_regs(gb, &sample);
        gb->current_supersample.left += sample.left;
        gb->current_supersample.right += sample.right;
        gb->n_subsamples++;
    }
    else if (gb->audio_quality != 1) {
        double ticks_per_subsample = ticks_per_sample / gb->audio_quality;
        if (ticks_per_subsample < 1) {
            ticks_per_subsample = 1;
        }
        if (gb->apu_subsample_cycles > ticks_per_subsample) {
            gb->apu_subsample_cycles -= ticks_per_subsample;
        }
        
        GB_sample_t sample;
        GB_apu_get_samples_and_update_pcm_regs(gb, &sample);
        gb->current_supersample.left += sample.left;
        gb->current_supersample.right += sample.right;
        gb->n_subsamples++;
    }

    if (gb->apu_sample_cycles > ticks_per_sample) {
        gb->apu_sample_cycles -= ticks_per_sample;
        if (gb->audio_position == gb->buffer_size) {
            /*
             if (!gb->turbo) {
                 GB_log(gb, "Audio overflow\n");
             }
             */
        }
        else {
            if (gb->audio_quality == 1) {
                GB_apu_get_samples_and_update_pcm_regs(gb, &gb->audio_buffer[gb->audio_position++]);
            }
            else {
                gb->audio_buffer[gb->audio_position].left = round(gb->current_supersample.left / gb->n_subsamples);
                gb->audio_buffer[gb->audio_position].right = round(gb->current_supersample.right / gb->n_subsamples);
                gb->n_subsamples = 0;
                gb->current_supersample = (GB_double_sample_t){0, };
                gb->audio_position++;
            }
        }
    }
}

void GB_apu_copy_buffer(GB_gameboy_t *gb, GB_sample_t *dest, unsigned int count)
{
    gb->audio_copy_in_progress = true;

    if (!gb->audio_stream_started) {
        // Intentionally fail the first copy to sync the stream with the Gameboy.
        gb->audio_stream_started = true;
        gb->audio_position = 0;
    }

    if (count > gb->audio_position) {
        // GB_log(gb, "Audio underflow: %d\n", count - gb->audio_position);
        if (gb->audio_position != 0) {
            for (unsigned i = 0; i < count - gb->audio_position; i++) {
                dest[gb->audio_position + i] = gb->audio_buffer[gb->audio_position - 1];
            }
        }
        else {
            memset(dest + gb->audio_position, 0, (count - gb->audio_position) * sizeof(*gb->audio_buffer));
        }
        count = gb->audio_position;
    }
    memcpy(dest, gb->audio_buffer, count * sizeof(*gb->audio_buffer));
    memmove(gb->audio_buffer, gb->audio_buffer + count, (gb->audio_position - count) * sizeof(*gb->audio_buffer));
    gb->audio_position -= count;

    gb->audio_copy_in_progress = false;
}

void GB_apu_init(GB_gameboy_t *gb)
{
    memset(&gb->apu, 0, sizeof(gb->apu));
    gb->apu.wave_channels[0].duty = gb->apu.wave_channels[1].duty = 4;
    gb->apu.lfsr = 0x7FFF;
    gb->apu.left_volume = 7;
    gb->apu.right_volume = 7;
    for (int i = 0; i < 4; i++) {
        gb->apu.wave_channels[i].left_on = gb->apu.wave_channels[i].right_on = 1;
    }
}

uint8_t GB_apu_read(GB_gameboy_t *gb, uint8_t reg)
{
    GB_apu_run_internal(gb);

    if (reg == GB_IO_NR52) {
        uint8_t value = 0;
        for (int i = 0; i < 4; i++) {
            value >>= 1;
            if (gb->apu.wave_channels[i].is_playing) {
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

    if (reg >= GB_IO_WAV_START && reg <= GB_IO_WAV_END && gb->apu.wave_channels[2].is_playing) {
        if (gb->apu.wave_channels[2].wave_length == 0) {
            return gb->apu.wave_form[0];
        }
        gb->apu.wave_channels[2].phase %= gb->apu.wave_channels[2].wave_length;
        return gb->apu.wave_form[(int)(gb->apu.wave_channels[2].phase * 32 / gb->apu.wave_channels[2].wave_length)];
    }

    return gb->io_registers[reg] | read_mask[reg - GB_IO_NR10];
}

void GB_apu_write(GB_gameboy_t *gb, uint8_t reg, uint8_t value)
{
    GB_apu_run_internal(gb);

    static const uint8_t duties[] = {1, 2, 4, 6}; /* Values are in 1/8 */
    uint8_t channel = 0;

    if (!gb->apu.global_enable && reg != GB_IO_NR52) {
        return;
    }

    gb->io_registers[reg] = value;

    switch (reg) {
        case GB_IO_NR10:
        case GB_IO_NR11:
        case GB_IO_NR12:
        case GB_IO_NR13:
        case GB_IO_NR14:
            channel = 0;
            break;
        case GB_IO_NR21:
        case GB_IO_NR22:
        case GB_IO_NR23:
        case GB_IO_NR24:
            channel = 1;
            break;
        case GB_IO_NR33:
        case GB_IO_NR34:
            channel = 2;
            break;
        case GB_IO_NR41:
        case GB_IO_NR42:
            channel = 3;
        default:
            break;
    }

    switch (reg) {
        case GB_IO_NR10:
            gb->apu.wave_channels[channel].sweep_direction = value & 8? -1 : 1;
            gb->apu.wave_channels[channel].cur_sweep_steps =
            gb->apu.wave_channels[channel].sweep_steps = (value & 0x70) >> 4;
            gb->apu.wave_channels[channel].sweep_shift = value & 7;
            break;
        case GB_IO_NR11:
        case GB_IO_NR21:
        case GB_IO_NR41:
            gb->apu.wave_channels[channel].duty = duties[value >> 6];
            gb->apu.wave_channels[channel].sound_length = (64 - (value & 0x3F)) * (APU_FREQUENCY / 256);
            if (gb->apu.wave_channels[channel].sound_length == 0) {
                gb->apu.wave_channels[channel].is_playing = false;
            }
            break;
        case GB_IO_NR12:
        case GB_IO_NR22:
        case GB_IO_NR42:
            gb->apu.wave_channels[channel].start_amplitude =
            gb->apu.wave_channels[channel].amplitude = CH_STEP * (value >> 4);
            if (value >> 4 == 0) {
                gb->apu.wave_channels[channel].is_playing = false;
            }
            gb->apu.wave_channels[channel].envelope_direction = value & 8? 1 : -1;
            gb->apu.wave_channels[channel].cur_envelope_steps =
            gb->apu.wave_channels[channel].envelope_steps = value & 7;
            break;
        case GB_IO_NR13:
        case GB_IO_NR23:
        case GB_IO_NR33:
            gb->apu.wave_channels[channel].NRX3_X4_temp = (gb->apu.wave_channels[channel].NRX3_X4_temp & 0xFF00) | value;
            gb->apu.wave_channels[channel].wave_length =  (2048 - gb->apu.wave_channels[channel].NRX3_X4_temp) *  (APU_FREQUENCY / 131072);
            if (channel == 2) {
                gb->apu.wave_channels[channel].wave_length *= 2;
            }
            break;
        case GB_IO_NR14:
        case GB_IO_NR24:
        case GB_IO_NR34:
            gb->apu.wave_channels[channel].stop_on_length = value & 0x40;
            if ((value & 0x80) && (channel != 2 || gb->apu.wave_enable)) {
                gb->apu.wave_channels[channel].is_playing = true;
                gb->apu.wave_channels[channel].phase = 0;
                gb->apu.wave_channels[channel].amplitude = gb->apu.wave_channels[channel].start_amplitude;
                gb->apu.wave_channels[channel].cur_envelope_steps = gb->apu.wave_channels[channel].envelope_steps;
            }

            gb->apu.wave_channels[channel].NRX3_X4_temp = (gb->apu.wave_channels[channel].NRX3_X4_temp & 0xFF) | ((value & 0x7) << 8);
            gb->apu.wave_channels[channel].wave_length =  (2048 - gb->apu.wave_channels[channel].NRX3_X4_temp) *  (APU_FREQUENCY / 131072);
            if (channel == 2) {
                gb->apu.wave_channels[channel].wave_length *= 2;
            }
            break;
        case GB_IO_NR30:
            gb->apu.wave_enable = value & 0x80;
            gb->apu.wave_channels[2].is_playing &= gb->apu.wave_enable;
            break;
        case GB_IO_NR31:
            gb->apu.wave_channels[2].sound_length = (256 - value) * (APU_FREQUENCY / 256);
            if (gb->apu.wave_channels[2].sound_length == 0) {
                gb->apu.wave_channels[2].is_playing = false;
            }
            break;
        case GB_IO_NR32:
            gb->apu.wave_shift = ((value >> 5) + 3) & 3;
            if (gb->apu.wave_shift == 3) {
                gb->apu.wave_shift = 4;
            }
            break;
        case GB_IO_NR43:
        {
            double r = value & 0x7;
            if (r == 0) r = 0.5;
            uint8_t s = value >> 4;
            gb->apu.wave_channels[3].wave_length = r * (1 << s) * (APU_FREQUENCY / 262144) ;
            gb->apu.lfsr_7_bit = value & 0x8;
            break;
        }
        case GB_IO_NR44:
            gb->apu.wave_channels[3].stop_on_length = value & 0x40;
            if (value & 0x80) {
                gb->apu.wave_channels[3].is_playing = true;
                gb->apu.lfsr = 0x7FFF;
                gb->apu.wave_channels[3].amplitude = gb->apu.wave_channels[3].start_amplitude;
                gb->apu.wave_channels[3].cur_envelope_steps = gb->apu.wave_channels[3].envelope_steps;
            }
            break;

        case GB_IO_NR50:
            gb->apu.left_volume = (value & 7);
            gb->apu.right_volume = ((value >> 4) & 7);
            break;

        case GB_IO_NR51:
            for (int i = 0; i < 4; i++) {
                gb->apu.wave_channels[i].left_on = value & 1;
                gb->apu.wave_channels[i].right_on = value & 0x10;
                value >>= 1;
            }
            break;
        case GB_IO_NR52:

            if ((value & 0x80) && !gb->apu.global_enable) {
                GB_apu_init(gb);
                gb->apu.global_enable = true;
            }
            else if (!(value & 0x80) && gb->apu.global_enable)  {
                memset(&gb->apu, 0, sizeof(gb->apu));
                memset(gb->io_registers + GB_IO_NR10, 0, GB_IO_WAV_START - GB_IO_NR10);
            }
            break;

        default:
            if (reg >= GB_IO_WAV_START && reg <= GB_IO_WAV_END) {
                gb->apu.wave_form[(reg - GB_IO_WAV_START) * 2] = value >> 4;
                gb->apu.wave_form[(reg - GB_IO_WAV_START) * 2 + 1] = value & 0xF;
            }
            break;
    }
}

void GB_set_audio_quality(GB_gameboy_t *gb, unsigned quality)
{
    gb->audio_quality = quality;
}

unsigned GB_apu_get_current_buffer_length(GB_gameboy_t *gb)
{
    return  gb->audio_position;
}
