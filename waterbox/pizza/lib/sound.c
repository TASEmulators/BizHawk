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

#include "cycles.h"
#include "global.h"
#include "gpu.h"
#include "mmu.h"
#include "sound.h"
#include "utils.h"
#include "sound_output.h"

#include <string.h>
#include <strings.h>
#include <sys/time.h>

/* super variable for audio controller */
sound_t sound;

// functions to connect to blip buf
static int16_t last_sample[8];

#define BLIP_LEFT(v,i) if(1){int32_t d = (v)-last_sample[(i)*2];blip_left(d);last_sample[(i)*2] = (v);}
#define BLIP_RIGHT(v,i) if(1){int32_t d = (v)-last_sample[(i)*2+1];blip_right(d);last_sample[(i)*2+1] = (v);}

static void blip_ch1()
{
    if (sound.channel_one.active)
    {
		if (sound.nr51->ch1_to_so1)
			BLIP_RIGHT(sound.channel_one.sample, 0);
        if (sound.nr51->ch1_to_so2)
            BLIP_LEFT(sound.channel_one.sample, 0);
    }
}
static void blip_ch2()
{
    if (sound.channel_two.active)
    {
		if (sound.nr51->ch2_to_so1)
			BLIP_RIGHT(sound.channel_two.sample, 1);
        if (sound.nr51->ch2_to_so2)
            BLIP_LEFT(sound.channel_two.sample, 1);
    }
}
static void blip_ch3()
{
	if (sound.channel_three.active)
	{
		uint16_t sample;
		uint8_t shift = (sound.nr32->volume_code == 0 ?
        	4 : sound.nr32->volume_code - 1);

        /* volume is zero in any case */ 
        if (shift == 4)
            sample = 0;
        else
        {
            /* apply volume change */
            uint8_t idx = sound.channel_three.index;
            uint16_t s;

            /* extract current sample */
            if ((idx & 0x01) == 0)
                s = (sound.wave_table[idx >> 1] & 0xf0) >> 4;
            else
                s = sound.wave_table[idx >> 1] & 0x0f;

            /* transform it into signed 16 bit sample */
            sample = ((s * 0x222) >> shift); 
        }

        
        /* not silence? */
        if (sample != 0)
        {
            if (sound.nr51->ch3_to_so1)
				BLIP_RIGHT(sample, 2);
            if (sound.nr51->ch3_to_so2)
                BLIP_LEFT(sample, 2);
        }
	}
}
static void blip_ch4()
{
    if (sound.channel_four.active)
    {
		if (sound.nr51->ch4_to_so1)
			BLIP_RIGHT(sound.channel_four.sample, 3);
        if (sound.nr51->ch4_to_so2)
            BLIP_LEFT(sound.channel_four.sample, 3);
    }
}

/* internal prototypes */
void   sound_envelope_step();
void   sound_length_ctrl_step();
void   sound_sweep_step();
void   sound_write_wave(uint16_t a, uint8_t v);

void sound_init_pointers()
{
    /* point sound structures to their memory areas */
    sound.nr10 = (nr10_t *) mmu_addr(0xFF10);
    sound.nr11 = (nr11_t *) mmu_addr(0xFF11);
    sound.nr12 = (nr12_t *) mmu_addr(0xFF12);
    sound.nr13 = (nr13_t *) mmu_addr(0xFF13);
    sound.nr14 = (nr14_t *) mmu_addr(0xFF14);

    sound.nr21 = (nr21_t *) mmu_addr(0xFF16);
    sound.nr22 = (nr22_t *) mmu_addr(0xFF17);
    sound.nr23 = (nr23_t *) mmu_addr(0xFF18);
    sound.nr24 = (nr24_t *) mmu_addr(0xFF19);

    sound.nr30 = (nr30_t *) mmu_addr(0xFF1A);
    sound.nr31 = (nr31_t *) mmu_addr(0xFF1B);
    sound.nr32 = (nr32_t *) mmu_addr(0xFF1C);
    sound.nr33 = (nr33_t *) mmu_addr(0xFF1D);
    sound.nr34 = (nr34_t *) mmu_addr(0xFF1E);

    sound.nr41 = (nr41_t *) mmu_addr(0xFF20);
    sound.nr42 = (nr42_t *) mmu_addr(0xFF21);
    sound.nr43 = (nr43_t *) mmu_addr(0xFF22);
    sound.nr44 = (nr44_t *) mmu_addr(0xFF23);

    sound.nr50 = mmu_addr(0xFF24);
    sound.nr51 = mmu_addr(0xFF25);
    sound.nr52 = mmu_addr(0xFF26);

    sound.wave_table = mmu_addr(0xFF30);
}

/* init sound states */
void sound_init()
{
    /* reset structure */
    bzero(&sound, sizeof(sound_t));

    /* point sound structures to their memory areas */
    sound_init_pointers();

    /* how many cpu cycles we need to emit a 512hz clock (frame sequencer) */
    sound.fs_cycles = 4194304 / 512;

    /* how many cpu cycles to generate a single frame seq clock? */
    sound.fs_cycles_next = sound.fs_cycles;
}

void sound_set_speed(char dbl)
{
}

/* update sound internal state given CPU T-states */
void sound_step_fs()
{
    /* rotate from 0 to 7 */
    sound.fs_cycles_idx = (sound.fs_cycles_idx + 1) & 0x07;

    /* reset fs cycles counter */
    sound.fs_cycles_next = cycles.cnt + 
                   (sound.fs_cycles << global_cpu_double_speed);

    /* length controller works at 256hz */
    if ((sound.fs_cycles_idx & 0x01) == 0)
        sound_length_ctrl_step();

    /* sweep works at 128hz */
    if (sound.fs_cycles_idx == 2 || sound.fs_cycles_idx == 6)
        sound_sweep_step();

    /* envelope works at 64hz */
    if (sound.fs_cycles_idx == 7)
        sound_envelope_step();
	blip_ch1();
	blip_ch2();
	blip_ch3();
	blip_ch4();	
}

/* update all channels */
void sound_step_ch1()
{    
    /* recalc current samples */
    if ((sound.channel_one.duty >> sound.channel_one.duty_idx) & 0x01)
        sound.channel_one.sample = sound.channel_one.volume;
    else
        sound.channel_one.sample = -sound.channel_one.volume;

    /* step to the next duty value */
    sound.channel_one.duty_idx = 
        (sound.channel_one.duty_idx + 1) & 0x07;

    /* go back */
    sound.channel_one.duty_cycles_next += sound.channel_one.duty_cycles;
	blip_ch1();
}

void sound_step_ch2()
{    
    /* recalc current samples */
    if ((sound.channel_two.duty >> sound.channel_two.duty_idx) & 0x01)
        sound.channel_two.sample = sound.channel_two.volume;
    else
        sound.channel_two.sample = -sound.channel_two.volume;

    /* step to the next duty value */
    sound.channel_two.duty_idx = 
        (sound.channel_two.duty_idx + 1) & 0x07;

    /* go back */
    sound.channel_two.duty_cycles_next += sound.channel_two.duty_cycles;
	blip_ch2();
}

void sound_step_ch3()
{
    /* switch to the next wave sample */
    sound.channel_three.index = (sound.channel_three.index + 1) & 0x1F;

    /* set the new current sample */
    sound.channel_three.sample = 
        sound.channel_three.wave[sound.channel_three.index];

    /* reload new period */
    uint_fast16_t freq = sound.nr33->frequency_lsb |
                         (sound.nr34->frequency_msb << 8);

    /* qty of cpu ticks needed for a wave sample change */
    sound.channel_three.cycles = ((2048 - freq) * 2) << global_cpu_double_speed; 
    sound.channel_three.cycles_next += sound.channel_three.cycles;
	blip_ch3();
}
   
void sound_step_ch4()
{
    /* update LSFR */
    if (sound.nr43->shift < 14)
    {
        /* shift register one bit right */
        uint16_t s = sound.channel_four.reg >> 1;

        /* xor current register and the shifted version */
        /* and extract bit zero                         */
        uint16_t x = (sound.channel_four.reg ^ s) & 1;

        /* update register */
        sound.channel_four.reg = s | x << 14;

        /* if width is set... */
        if (sound.nr43->width)
            sound.channel_four.reg = 
                (sound.channel_four.reg & 0xBF) | x << 6;
    }

    /* update sample */
    if (sound.channel_four.reg & 0x01)
        sound.channel_four.sample = -sound.channel_four.volume;
    else
        sound.channel_four.sample = sound.channel_four.volume;

    /* qty of cpu ticks needed for a wave sample change */
    sound.channel_four.cycles_next += sound.channel_four.period_lfsr;
	blip_ch4();
}

/* update length of channel1 */
void static inline sound_length_ctrl_step_ch(char length_enable, 
                                             uint_fast32_t *length,
                                             uint8_t *active)
{
    if (length_enable && *length != 0)
    {
        (*length)--;

        /* if ZERO is reached, turn off the channel */
        if (*length == 0)
            *active = 0;
    }
}

/* length controller step */
void sound_length_ctrl_step()
{
    /* step every channel */
    sound_length_ctrl_step_ch(sound.nr14->length_enable, 
                              &sound.channel_one.length, 
                              &sound.channel_one.active);

    sound_length_ctrl_step_ch(sound.nr24->length_enable,
                              &sound.channel_two.length,
                              &sound.channel_two.active);

    sound_length_ctrl_step_ch(sound.nr34->length_enable,
                              &sound.channel_three.length,
                              &sound.channel_three.active);

    sound_length_ctrl_step_ch(sound.nr44->length_enable,
                              &sound.channel_four.length,
                              &sound.channel_four.active);
}

/* calc the new frequency by sweep module */
uint_fast32_t sound_sweep_calc()
{
    uint_fast32_t new_freq;

    /* time to update frequency */
    uint_fast32_t diff = 
             sound.channel_one.sweep_shadow_frequency >> 
             sound.nr10->shift;

    /* the calculated diff must be summed or subtracted to frequency */
    if (sound.nr10->negate)
    {
        new_freq = sound.channel_one.sweep_shadow_frequency - diff;
        sound.channel_one.sweep_neg = 1;
    }
    else
        new_freq = sound.channel_one.sweep_shadow_frequency + diff;

    /* if freq > 2047, turn off the channel */
    if (new_freq > 2047)
        sound.channel_one.active = 0;

    return new_freq;
}

/* set channel one new frequency */
void sound_set_frequency(uint_fast32_t new_freq)
{
    /* too high? */
    if (new_freq > 2047)
    {
        sound.channel_one.active = 0;
        return;
    }

    /* update with the new frequency */
    sound.channel_one.frequency = new_freq;

    /* update them also into memory */
    sound.nr13->frequency_lsb = (uint8_t) (new_freq & 0x000000ff);
    sound.nr14->frequency_msb = (uint8_t) ((new_freq >> 8) & 0x00000007);

    /* update the duty cycles */
    sound.channel_one.duty_cycles = 
	    ((2048 - new_freq) * 4) << global_cpu_double_speed;

    /* and reset them */
    sound.channel_one.duty_cycles_next = 
	    cycles.cnt + sound.channel_one.duty_cycles;
}

/* step of frequency sweep at 128hz */
void sound_sweep_step()
{
    uint_fast32_t new_freq;

    if (sound.channel_one.active && 
        sound.channel_one.sweep_active)
    {
        /* make it rotate from 0 to 8 */
        sound.channel_one.sweep_cnt++;

        /* enough cycles? */
        if (sound.channel_one.sweep_cnt == sound.channel_one.sweep_next)
        {
            /* reload the next step - 0 is treated as 8 */
            sound.channel_one.sweep_next = 
            sound.nr10->sweep_period ? 
                sound.nr10->sweep_period : 8;

            /* reset sweep counter */
            sound.channel_one.sweep_cnt = 0;

            /* period must be > 0 if new freq gotta be updated */
            if (sound.nr10->sweep_period == 0)
                return;

            /* calc new frequency */
            new_freq = sound_sweep_calc();

            /* set it only if < 2048 and shift != 0 */
            if (sound.nr10->shift &&
                new_freq < 2048)
            {
                /* copy new_freq into shadow register */
                sound.channel_one.sweep_shadow_frequency = new_freq;

                /* update all the stuff related to new frequency */
                sound_set_frequency(new_freq);

                /* update freq again (but only in shadow register) */
                sound_sweep_calc();
            }
        }
    }  
}

/* step of envelope at 64hz */
void sound_envelope_step()
{
    if (sound.channel_one.active && sound.nr12->period)
    {
        /* update counter */
        sound.channel_one.envelope_cnt++;

        /* if counter reaches period, update volume */
        if (sound.channel_one.envelope_cnt == sound.nr12->period)
        {
            if (sound.nr12->add)
            {
                if (sound.channel_one.volume < (14 * 0x111))
                    sound.channel_one.volume += 0x111;
            }
            else
            {
                if (sound.channel_one.volume >= 0x111)
                    sound.channel_one.volume -= 0x111;
            }

            /* reset counter */
            sound.channel_one.envelope_cnt = 0;
        }
    }

    if (sound.channel_two.active && sound.nr22->period)
    {
        /* update counter */
        sound.channel_two.envelope_cnt++;

        /* if counter reaches period, update volume */
        if (sound.channel_two.envelope_cnt == sound.nr22->period)
        {
            if (sound.nr22->add)
            {
                if (sound.channel_two.volume < (14 * 0x111))
                    sound.channel_two.volume += 0x111;
            }
            else
            {
                if (sound.channel_two.volume >= 0x111)
                    sound.channel_two.volume -= 0x111;
            }

            /* reset counter */
            sound.channel_two.envelope_cnt = 0;
        }
    }

    if (sound.channel_four.active && sound.nr42->period)
    {
        /* update counter */
        sound.channel_four.envelope_cnt++;

        /* if counter reaches period, update volume */
        if (sound.channel_four.envelope_cnt == sound.nr42->period)
        {
            if (sound.nr42->add)
            {
                if (sound.channel_four.volume < (14 * 0x111))
                    sound.channel_four.volume += 0x111;
            }
            else
            {
                if (sound.channel_four.volume > 0x111)
                    sound.channel_four.volume -= 0x111;
            }

            /* reset counter */
            sound.channel_four.envelope_cnt = 0;
        }
    }
}

uint8_t sound_read_reg(uint16_t a, uint8_t v)
{
    switch (a)
    {
        /* NR1X */
        case 0xFF10: return v | 0x80;
        case 0xFF11: return v | 0x3F;
        case 0xFF12: return v;
        case 0xFF13: return v | 0xFF;
        case 0xFF14: return v | 0xBF;
        /* NR2X */
        case 0xFF15: return v | 0xFF;
        case 0xFF16: return v | 0x3F;
        case 0xFF17: return v;
        case 0xFF18: return v | 0xFF;
        case 0xFF19: return v | 0xBF;
        /* NR3X */
        case 0xFF1A: return v | 0x7F;
        case 0xFF1B: return v | 0xFF;
        case 0xFF1C: return v | 0x9F;
        case 0xFF1D: return v | 0xFF;
        case 0xFF1E: return v | 0xBF;
        /* NR4X */
        case 0xFF1F: return v | 0xFF;
        case 0xFF20: return v | 0xFF;
        case 0xFF21: return v;
        case 0xFF22: return v;
        case 0xFF23: return v | 0xBF;
        /* NR5X */
        case 0xFF24: return v;
        case 0xFF25: return v;
        case 0xFF26: 
            if (sound.nr52->power) 
                return 0xf0                              | 
                       sound.channel_one.active          | 
                       (sound.channel_two.active << 1)   | 
                       (sound.channel_three.active << 2) | 
                       (sound.channel_four.active << 3);
            else 
                return 0x70;
        case 0xFF27: 
        case 0xFF28:
        case 0xFF29: 
        case 0xFF2A: 
        case 0xFF2B: 
        case 0xFF2C: 
        case 0xFF2D: 
        case 0xFF2E: 
        case 0xFF2F: return 0xFF;
        case 0xFF30: 
        case 0xFF31: 
        case 0xFF32: 
        case 0xFF33: 
        case 0xFF34: 
        case 0xFF35: 
        case 0xFF36: 
        case 0xFF37: 
        case 0xFF38: 
        case 0xFF39: 
        case 0xFF3A: 
        case 0xFF3B: 
        case 0xFF3C: 
        case 0xFF3D: 
        case 0xFF3E: 
        case 0xFF3F: 
            if (sound.channel_three.active)
            {
/*                if (!global_cgb && sound.channel_three.ram_access != 0)
                {
                    printf("RAM ACCESSO NON ZERO %u - CNT %d NEXT %d\n",
                            sound.channel_three.ram_access, cycles.cnt, sound.channel_three.ram_access_next);
                    return 0xFF;
                }*/
                if (!global_cgb && 
                    cycles.cnt < sound.channel_three.ram_access_next)
                    return 0xFF;

                return sound.wave_table[sound.channel_three.index >> 1];
            }

        default: return v;
    }
}

void sound_write_reg(uint16_t a, uint8_t v)
{
    /* when turned off, only write to NR52 (0xFF26) is legit */
    if (!sound.nr52->power && a != 0xFF26)
    {
        /* CGB mode doesnt allow any write on register during power off */
        if (global_cgb)
            return;

        /* in DMG mode, update length is legit while no power */ 
        switch (a)
        {
            case 0xFF11: sound.channel_one.length = 64 - (v & 0x3f); return;
            case 0xFF16: sound.channel_two.length = 64 - (v & 0x3f); return;
            case 0xFF1B: sound.channel_three.length = 256 - v; return;
            case 0xFF20: sound.channel_four.length = 64 - (v & 0x3f); return;
            default: return;
        }
    }

    /* wave write */
    if (a >= 0xFF30 && a <= 0xFF3F)
        return sound_write_wave(a, v);

    /* save old value */
    uint8_t old = *((uint8_t *) mmu_addr(a));

    /* confirm write on memory */
    *((uint8_t *) mmu_addr(a)) = v;

    switch (a)
    {
        case 0xFF10:

            if (!sound.nr10->negate && sound.channel_one.sweep_neg)
                sound.channel_one.active = 0; 

            break;

        case 0xFF11:

            /* set length as 64 - length_load */
            sound.channel_one.length = 64 - sound.nr11->length_load;

            /* update duty type */
            switch (sound.nr11->duty)
            {
                           /* 12.5 % */
                case 0x00: sound.channel_one.duty = 0x80;
                           break;

                           /* 25% */
                case 0x01: sound.channel_one.duty = 0x81;
                           break;

                           /* 50% */
                case 0x02: sound.channel_one.duty = 0xE1;
                           break;

                           /* 75% */
                case 0x03: sound.channel_one.duty = 0x7E;
                           break;
            }

            break;

        case 0xFF12:
            
            /* volume 0 = turn off the DAC = turn off channeru */
            if (sound.nr12->volume == 0 &&
                sound.nr12->add == 0)
                sound.channel_one.active = 0;

            break;

        case 0xFF13:

            /* update frequncy */
            sound.channel_one.frequency = sound.nr13->frequency_lsb |
                                          (sound.nr14->frequency_msb << 8);

            /* update duty cycles */
            sound.channel_one.duty_cycles =
                ((2048 - sound.channel_one.frequency) * 4) 
		<< global_cpu_double_speed;

            break;

        case 0xFF14:

            /* length counter turned on */
            if (sound.nr14->length_enable)
            {
                nr14_t *old_nr14 = (nr14_t *) &old;

                /* give an extra length clock if                 */
                /* 1) we switched from off to on the len counter */
                /* 2) we are in the first half of len clock      */
                /* 3) actual length is not zero                  */
                if ((old_nr14->length_enable == 0) &&
                    ((sound.fs_cycles_idx & 0x01) == 0x00) &&
                    (sound.channel_one.length != 0))
                    sound_length_ctrl_step_ch(sound.nr14->length_enable,
                               &sound.channel_one.length,
                               &sound.channel_one.active);
            }

            /* always update frequency, even if it's not a trigger */
            sound.channel_one.frequency = sound.nr13->frequency_lsb |
                                          (sound.nr14->frequency_msb << 8);

            /* qty of cpu ticks needed for a duty change */
            /* (1/8 of wave cycle) */
            sound.channel_one.duty_cycles = 
                ((2048 - sound.channel_one.frequency) * 4) 
                << global_cpu_double_speed;

            if (v & 0x80) 
            {
                /* if we switch from OFF to ON, reset duty idx */
                if (sound.channel_two.active == 0)
                    sound.channel_two.duty_idx = 0;

                /* setting internal modules data with stuff taken from memory */
                sound.channel_one.active = 1;
                sound.channel_one.duty_cycles_next = 
         	    cycles.cnt + sound.channel_one.duty_cycles;

                /* set the 8 phase of a duty cycle by setting 8 bits */
                switch (sound.nr11->duty)
                {
                               /* 12.5 % */
                    case 0x00: sound.channel_one.duty = 0x80;
                               break;

                               /* 25% */
                    case 0x01: sound.channel_one.duty = 0x81;
                               break;

                               /* 50% */
                    case 0x02: sound.channel_one.duty = 0xE1;
                               break;

                               /* 75% */
                    case 0x03: sound.channel_one.duty = 0x7E;
                               break;
                }

                /* calc length */
                if (sound.channel_one.length == 0)
                    sound.channel_one.length = 64;

                /* base volume */
                sound.channel_one.volume = 
                    sound.nr12->volume * 0x111;

                /* reset envelope counter */
                sound.channel_one.envelope_cnt = 0;

                /* save current freq into sweep shadow register */
                sound.channel_one.sweep_shadow_frequency = 
                    sound.channel_one.frequency;

                /* reset sweep timer */
                sound.channel_one.sweep_cnt = 0;

                /* reset sweep neg bool  */
                sound.channel_one.sweep_neg = 0;

                /* reload the next step */
                sound.channel_one.sweep_next = sound.nr10->sweep_period ? 
                                               sound.nr10->sweep_period : 8;

                /* set sweep as active if period != 0 or shift != 0 */
                if (sound.nr10->sweep_period != 0 ||
                    sound.nr10->shift != 0)
                    sound.channel_one.sweep_active = 1;
                else
                    sound.channel_one.sweep_active = 0;

                /* if shift is != 0, calc the new frequency */
                if (sound.nr10->shift != 0)
                {
                    uint32_t new_freq = sound_sweep_calc();

                    /* update all the stuff related to new frequency */
                    sound_set_frequency(new_freq);
                }

                /* if DAC is off, turn off the channel */
                if (sound.nr12->add == 0 &&
                    sound.nr12->volume == 0)
                    sound.channel_one.active = 0;

                /* extra length clock if length == 64 */
                /* and FS is in the fist half         */
                if ((sound.fs_cycles_idx & 0x01) == 0x00 &&
                     sound.channel_one.length == 64)
                    sound_length_ctrl_step_ch(sound.nr14->length_enable,
                               &sound.channel_one.length,
                               &sound.channel_one.active);
            }


            break;

        case 0xFF16:
     
            sound.channel_two.length = 64 - sound.nr21->length_load;

            /* update duty type */
            switch (sound.nr21->duty)
            {
                           /* 12.5 % */
                case 0x00: sound.channel_two.duty = 0x80;
                           break;

                           /* 25% */
                case 0x01: sound.channel_two.duty = 0x81;
                           break;

                           /* 50% */
                case 0x02: sound.channel_two.duty = 0xE1;
                           break;

                           /* 75% */
                case 0x03: sound.channel_two.duty = 0x7E;
                           break;
            }
       
            break;

        case 0xFF17:

            /* volume 0 = turn off the DAC = turn off channeru */
            if (sound.nr22->volume == 0 &&
                sound.nr22->add == 0)
                sound.channel_two.active = 0;

            break;

        case 0xFF18:

            /* update frequncy */
            sound.channel_two.frequency = (sound.nr23->frequency_lsb |
                                          (sound.nr24->frequency_msb << 8));

            /* update duty cycles */
            sound.channel_two.duty_cycles =
                ((2048 - sound.channel_two.frequency) * 4) 
                << global_cpu_double_speed;

            break;

        case 0xFF19:

            /* length counter turned on */
            if (sound.nr24->length_enable)
            {
                nr24_t *old_nr24 = (nr24_t *) &old;

                /* give an extra length clock if                 */
                /* 1) we switched from off to on the len counter */
                /* 2) we are in the first half of len clock      */
                /* 3) actual length is not zero                  */
                if ((old_nr24->length_enable == 0) &&
                    ((sound.fs_cycles_idx & 0x01) == 0x00) &&
                    (sound.channel_two.length != 0))
                    sound_length_ctrl_step_ch(sound.nr24->length_enable,
                               &sound.channel_two.length,
                               &sound.channel_two.active);
            }

            /* always update frequency, even if it's not a trigger */
            sound.channel_two.frequency = sound.nr23->frequency_lsb |
                                          (sound.nr24->frequency_msb << 8);

            /* qty of cpu ticks needed for a duty change */ 
            /* (1/8 of wave cycle) */
            sound.channel_two.duty_cycles = 
                ((2048 - sound.channel_two.frequency) * 4) 
                << global_cpu_double_speed;

            if (v & 0x80) 
            {
                /* if we switch from OFF to ON, reset duty idx */
                if (sound.channel_two.active == 0)
                    sound.channel_two.duty_idx = 0;

                /* setting internal modules data with stuff taken from memory */
                sound.channel_two.active = 1;
                sound.channel_two.duty_cycles_next = 
         	    cycles.cnt + sound.channel_two.duty_cycles;

                /* set the 8 phase of a duty cycle by setting 8 bits */
                switch (sound.nr21->duty)
                {
                               /* 12.5 % */
                    case 0x00: sound.channel_two.duty = 0x80;
                               break;

                               /* 25% */
                    case 0x01: sound.channel_two.duty = 0x81;
                               break;

                               /* 50% */
                    case 0x02: sound.channel_two.duty = 0xE1;
                               break;

                               /* 75% */
                    case 0x03: sound.channel_two.duty = 0x7E;
                               break;
                }

                /* calc length */
                if (sound.channel_two.length == 0)
                    sound.channel_two.length = 64;

                /* base volume */
                sound.channel_two.volume = 
                    sound.nr22->volume * 0x111;

                /* reset envelope counter */
                sound.channel_two.envelope_cnt = 0;

                /* if DAC is off, turn off the channel */
                if (sound.nr22->add == 0 &&
                    sound.nr22->volume == 0)
                    sound.channel_two.active = 0;

                /* extra length clock if length == 64 */
                /* and FS is in the fist half         */
                if ((sound.fs_cycles_idx & 0x01) == 0x00 &&
                     sound.channel_two.length == 64)
                    sound_length_ctrl_step_ch(sound.nr24->length_enable,
                               &sound.channel_two.length,
                               &sound.channel_two.active);
            }

            break;

        case 0xFF1A:

            /* if DAC is off, disable the channel */
            if (sound.nr30->dac == 0)
                sound.channel_three.active = 0;

            break;

        case 0xFF1B:
      
            sound.channel_three.length = 
                256 - sound.nr31->length_load;

            break;

        case 0xFF1C:

            break;

        case 0xFF1E:

            /* length counter turned on */
            if (sound.nr34->length_enable)
            {
                nr34_t *old_nr34 = (nr34_t *) &old;

                /* give an extra length clock if                 */
                /* 1) we switched from off to on the len counter */
                /* 2) we are in the first half of len clock      */
                /* 3) actual length is not zero                  */
                if ((old_nr34->length_enable == 0) &&
                    ((sound.fs_cycles_idx & 0x01) == 0x00) &&
                    (sound.channel_three.length != 0))
                    sound_length_ctrl_step_ch(sound.nr34->length_enable,
                               &sound.channel_three.length,
                               &sound.channel_three.active);
            }

            if (v & 0x80) 
            {
                uint16_t freq = sound.nr33->frequency_lsb |
                                (sound.nr34->frequency_msb << 8);

                /* setting internal modules data with stuff taken from memory */
                sound.channel_three.active = 1;

                uint_fast32_t old_cycles = sound.channel_three.cycles;

                /* qty of cpu ticks needed for a wave sample change */
                sound.channel_three.cycles = 
                    (((2048 - freq) * 2) + 6) << global_cpu_double_speed; 


                /* treat obscure behaviours.... */
                if (!global_cgb && 
                    cycles.cnt + 8 == sound.channel_three.cycles_next + 
                                      sound.channel_three.cycles - 
                                      old_cycles)
                {
                    uint8_t next = 
                        ((sound.channel_three.index + 1) & 0x1F) >> 1;

                    if (next < 4)
                        sound.wave_table[0] = sound.wave_table[next];
                    else
                        memcpy(sound.wave_table, 
                               &sound.wave_table[next & 0xfc], 4);
                }

                /* init wave table index */
                sound.channel_three.index = 0;
                sound.channel_three.cycles_next = 
                    cycles.cnt + sound.channel_three.cycles;

                /* calc length */
                if (sound.channel_three.length == 0)
                    sound.channel_three.length = 256;

                /* if DAC is off, disable the channel */
                if (sound.nr30->dac == 0)
                    sound.channel_three.active = 0;

                /* extra length clock if length == 256 */
                /* and FS is in the fist half          */
                if ((sound.fs_cycles_idx & 0x01) == 0x00 &&
                     sound.channel_three.length == 256)
                    sound_length_ctrl_step_ch(sound.nr34->length_enable,
                               &sound.channel_three.length,
                               &sound.channel_three.active);

                /* i accessed to the wave RAM... */
                sound.channel_three.ram_access = sound.channel_three.cycles; 

                if (sound.channel_three.cycles % 4 == 0)
                    sound.channel_three.ram_access_next = 
                        cycles.cnt + sound.channel_three.cycles; 
                    else 
                        sound.channel_three.ram_access_next = -1;

/*                printf("RAM ACCESS RICARICATO %u - CNT %d CYCLES %d \n",
                       sound.channel_three.ram_access, 
                       cycles.cnt, sound.channel_three.cycles);*/
            }
            break;

        case 0xFF20:

            sound.channel_four.length = 64 - sound.nr41->length_load;

            break;

        case 0xFF21:

            /* highest 5 bits cleared = turn off the DAC = turn off channeru */
            if (sound.nr42->volume == 0 &&
                sound.nr42->add == 0)
                sound.channel_four.active = 0;

            break;

        case 0xFF23:

            /* length counter turned on */
            if (sound.nr44->length_enable)
            {
                nr44_t *old_nr44 = (nr44_t *) &old;

                /* give an extra length clock if                 */
                /* 1) we switched from off to on the len counter */
                /* 2) we are in the first half of len clock      */
                /* 3) actual length is not zero                  */
                if ((old_nr44->length_enable == 0) &&
                    ((sound.fs_cycles_idx & 0x01) == 0x00) &&
                    (sound.channel_four.length != 0))
                    sound_length_ctrl_step_ch(sound.nr44->length_enable,
                               &sound.channel_four.length,
                               &sound.channel_four.active);
            }

            if (v & 0x80) 
            {
                /* setting internal modules data with stuff taken from memory */
                sound.channel_four.active = 1;

                /* calc length */
                if (sound.channel_four.length == 0)
                    sound.channel_four.length = 64;

                uint16_t divisor;

                /* calc LFSR period */
                switch (sound.nr43->divisor)
                {
                    case 0: divisor = 8;   break;
                    case 1: divisor = 16;  break;
                    case 2: divisor = 32;  break;
                    case 3: divisor = 48;  break;
                    case 4: divisor = 64;  break; 
                    case 5: divisor = 80;  break;
                    case 6: divisor = 96;  break;
                    case 7: divisor = 112; break;
                }

                /* calc LFSR period */
                sound.channel_four.period_lfsr = divisor << sound.nr43->shift;
                sound.channel_four.cycles_next = 
                    cycles.cnt + sound.channel_four.period_lfsr;

                /* init reg to all bits to 1 */
                sound.channel_four.reg = 0x7FFF;

                /* base volume */
                sound.channel_four.volume =
                    sound.nr42->volume * 0x111;

                /* reset envelope counter */
                sound.channel_four.envelope_cnt = 0;

                /* if DAC is off, turn off the channel */
                if (sound.nr42->add == 0 &&
                    sound.nr42->volume == 0)
                    sound.channel_four.active = 0;

                /* extra length clock if length == 64 */
                /* and FS is in the fist half         */
                if ((sound.fs_cycles_idx & 0x01) == 0x00 &&
                     sound.channel_four.length == 64)
                    sound_length_ctrl_step_ch(sound.nr44->length_enable,
                                              &sound.channel_four.length,
                                              &sound.channel_four.active);
            }

            break;

        case 0xFF26:

            if (v & 0x80)
            {
                /* power from off to on! */ 
                if (!(old & 0x80))
                {
                    /* reset frame sequencer so the next step will be zero */
                    sound.fs_cycles_idx = 7;

                    /* reset wave index */
                    sound.channel_three.index = 0;

                    /* wave samples are resetted */
                    bzero(sound.wave_table, 16);
                }
            } 
            else
            {
                /* power off */

                /* clear all the sound memory */
                bzero(mmu_addr(0xFF10), 22);

                if (global_cgb)
                {
                    sound.nr41->length_load = 0;
                    sound.channel_four.length = 0;
                }

                /* turn off every channeru */
                sound.channel_one.active = 0;
                sound.channel_two.active = 0;
                sound.channel_three.active = 0;
                sound.channel_four.active = 0;
            }
            
    }
}

void sound_write_wave(uint16_t a, uint8_t v)
{
    if (sound.channel_three.active)
    {
//        if (!global_cgb && sound.channel_three.ram_access != 0)
//            return;
        if (!global_cgb && cycles.cnt < sound.channel_three.ram_access_next)
            return;

        sound.wave_table[sound.channel_three.index >> 1] = v;

        return;
    }

    sound.wave_table[a - 0xFF30] = v;
}
