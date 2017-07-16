#ifndef apu_h
#define apu_h
#include <stdbool.h>
#include <stdint.h>
#include "gb_struct_def.h"
/* Divides nicely and never overflows with 4 channels */
#define MAX_CH_AMP 0x1E00
#define CH_STEP (0x1E00/0xF)


typedef struct
{
    int16_t left;
    int16_t right;
} GB_sample_t;

typedef struct
{
    double left;
    double right;
} GB_double_sample_t;

/* Not all used on all channels */
/* All lengths are in APU ticks */
typedef struct
{
    uint32_t phase;
    uint32_t wave_length;
    int32_t sound_length;
    bool stop_on_length;
    uint8_t duty;
    int16_t amplitude;
    int16_t start_amplitude;
    uint8_t envelope_steps;
    uint8_t cur_envelope_steps;
    int8_t envelope_direction;
    uint8_t sweep_steps;
    uint8_t cur_sweep_steps;
    int8_t sweep_direction;
    uint8_t sweep_shift;
    bool is_playing;
    uint16_t NRX3_X4_temp;
    bool left_on;
    bool right_on;
} GB_apu_channel_t;

typedef struct
{
    uint16_t apu_cycles;
    bool global_enable;
    uint32_t envelope_step_timer;
    uint32_t sweep_step_timer;
    int8_t wave_form[32];
    uint8_t wave_shift;
    bool wave_enable;
    uint16_t lfsr;
    bool lfsr_7_bit;
    uint8_t left_volume;
    uint8_t right_volume;
    GB_apu_channel_t wave_channels[4];
} GB_apu_t;

void GB_set_sample_rate(GB_gameboy_t *gb, unsigned int sample_rate);
/* Quality is the number of subsamples per sampling, for the sake of resampling.
   1 means on resampling at all, 0 is maximum quality. Default is 4. */
void GB_set_audio_quality(GB_gameboy_t *gb, unsigned quality);
void GB_apu_copy_buffer(GB_gameboy_t *gb, GB_sample_t *dest, unsigned int count);
unsigned GB_apu_get_current_buffer_length(GB_gameboy_t *gb);

#ifdef GB_INTERNAL
void GB_apu_write(GB_gameboy_t *gb, uint8_t reg, uint8_t value);
uint8_t GB_apu_read(GB_gameboy_t *gb, uint8_t reg);
void GB_apu_get_samples_and_update_pcm_regs(GB_gameboy_t *gb, GB_sample_t *samples);
void GB_apu_init(GB_gameboy_t *gb);
void GB_apu_run(GB_gameboy_t *gb);
#endif

#endif /* apu_h */
