/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#ifndef __SOUND_HDR__
#define __SOUND_HDR__

#define SOUND_SAMPLES 4096

typedef struct nr10_s
{
    uint8_t shift:3;
    uint8_t negate:1;
    uint8_t sweep_period:3;
    uint8_t spare:1;

} nr10_t;

typedef struct nr11_s
{
    uint8_t length_load:6;
    uint8_t duty:2;

} nr11_t;

typedef struct nr12_s
{
    uint8_t period:3;
    uint8_t add:1;
    uint8_t volume:4;

} nr12_t;

typedef struct nr13_s
{
    uint8_t frequency_lsb;

} nr13_t;

typedef struct nr14_s
{
    uint8_t frequency_msb:3;
    uint8_t spare:3;
    uint8_t length_enable:1;
    uint8_t trigger:1;

} nr14_t;

typedef struct nr21_s
{
    uint8_t length_load:6;
    uint8_t duty:2;

} nr21_t;

typedef struct nr22_s
{
    uint8_t period:3;
    uint8_t add:1;
    uint8_t volume:4;

} nr22_t;

typedef struct nr23_s
{
    uint8_t frequency_lsb;

} nr23_t;

typedef struct nr24_s
{
    uint8_t frequency_msb:3;
    uint8_t spare:3;
    uint8_t length_enable:1;
    uint8_t trigger:1;

} nr24_t;


typedef struct nr30_s
{
    uint8_t spare:7;
    uint8_t dac:1;

} nr30_t;

typedef struct nr31_s
{
    uint8_t length_load;

} nr31_t;

typedef struct nr32_s
{
    uint8_t spare:5;
    uint8_t volume_code:2;
    uint8_t spare2:1;

} nr32_t;

typedef struct nr33_s
{
    uint8_t frequency_lsb;

} nr33_t;

typedef struct nr34_s
{
    uint8_t frequency_msb:3;
    uint8_t spare:3;
    uint8_t length_enable:1;
    uint8_t trigger:1;

} nr34_t;

typedef struct nr41_s
{
    uint8_t length_load:6;
    uint8_t spare:2;

} nr41_t;

typedef struct nr42_s
{
    uint8_t period:3;
    uint8_t add:1;
    uint8_t volume:4;

} nr42_t;

typedef struct nr43_s
{
    uint8_t divisor:3;
    uint8_t width:1;
    uint8_t shift:4;

} nr43_t;

typedef struct nr44_s
{
    uint8_t spare:6;
    uint8_t length_enable:1;
    uint8_t trigger:1;

} nr44_t;

typedef struct nr50_s
{
    uint8_t so1_volume:3;
    uint8_t vin_to_so1:1;
    uint8_t so2_volume:3;
    uint8_t vin_to_so2:1;
} nr50_t;

typedef struct nr51_s
{
    uint8_t ch1_to_so1:1;
    uint8_t ch2_to_so1:1;
    uint8_t ch3_to_so1:1;
    uint8_t ch4_to_so1:1;
    uint8_t ch1_to_so2:1;
    uint8_t ch2_to_so2:1;
    uint8_t ch3_to_so2:1;
    uint8_t ch4_to_so2:1;
} nr51_t;

typedef struct nr52_s
{
    uint8_t spare:7;
    uint8_t power:1;    
} nr52_t;

typedef struct channel_square_s
{
    uint8_t       active;
    uint8_t       duty;
    uint8_t       duty_idx;
    uint8_t       envelope_cnt;
    uint_fast16_t duty_cycles;
    uint64_t duty_cycles_next;
    uint_fast32_t length;
    uint_fast32_t frequency;
    int16_t       sample;
    int16_t       spare;
    uint_fast16_t sweep_active;
    uint_fast16_t sweep_cnt;
    uint_fast16_t sweep_neg;
    uint_fast16_t sweep_next;
    int16_t       volume;
    int16_t       spare2;
    uint32_t      sweep_shadow_frequency;

} channel_square_t;

typedef struct channel_wave_s
{
    uint8_t  active;
    uint8_t  index;
    uint16_t ram_access;
    int16_t  sample;
    int16_t  spare;
    int16_t  wave[32];
    uint_fast32_t cycles;
    uint64_t cycles_next;
    uint_fast32_t ram_access_next;
    uint_fast32_t length;

} channel_wave_t;

typedef struct channel_noise_s
{
    uint8_t       active;
    uint8_t       envelope_cnt;
    uint16_t      spare;
    uint_fast32_t length;
    uint_fast16_t period_lfsr;
    uint64_t cycles_next;
    int16_t       volume;
    int16_t       sample;
    uint16_t      reg;
    uint16_t      spare2;
 
} channel_noise_t;

typedef struct sound_s
{
    nr10_t  *nr10;
    nr11_t  *nr11;
    nr12_t  *nr12;
    nr13_t  *nr13;
    nr14_t  *nr14;

    nr21_t  *nr21;
    nr22_t  *nr22;
    nr23_t  *nr23;
    nr24_t  *nr24;

    nr30_t  *nr30;
    nr31_t  *nr31;
    nr32_t  *nr32;
    nr33_t  *nr33;
    nr34_t  *nr34;

    nr41_t  *nr41;
    nr42_t  *nr42;
    nr43_t  *nr43;
    nr44_t  *nr44;

    nr50_t  *nr50;
    nr51_t  *nr51;
    nr52_t  *nr52;

    uint8_t              *wave_table;

    channel_square_t  channel_one;    
    channel_square_t  channel_two;    
    channel_wave_t    channel_three;
    channel_noise_t   channel_four;

    /* emulation speed stuff */
    uint_fast16_t     frame_counter;

    /* output rate */
    uint_fast32_t     output_rate;

    /* CPU cycles to internal cycles counters */
    uint_fast32_t     fs_cycles;
    uint_fast32_t     fs_cycles_idx;
    uint64_t     fs_cycles_next;
} sound_t;
 
extern sound_t sound; 

/* prototypes */
void     sound_init();
uint8_t  sound_read_reg(uint16_t a, uint8_t v);
void     sound_set_speed(char dbl);
void     sound_step_fs();
void     sound_step_ch1();
void     sound_step_ch2();
void     sound_step_ch3();
void     sound_step_ch4();
void     sound_write_reg(uint16_t a, uint8_t v);

#endif
