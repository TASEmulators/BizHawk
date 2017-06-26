/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 *   Developed by Andre de la Rocha   <adlroc@users.sourceforge.net>
 *             Arlindo M. de Oliveira <dgtec@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 *
 *
 *   O2 Voice emulation
 */

#include <stdio.h>
#include "vmachine.h"
#include "cpu.h"
#include "voice.h"

//static SAMPLE *voices[9][128];
static int voice_bank = 0;
static int voice_num = -1;
static int voice_addr = 0;
static int voice_ok = 0;
static int voice_st = 0;
static unsigned long clk_voice_start = 0;

void load_voice_samples(char *path)
{
    /*int bank, sam, i, ld = 0;
    char name[MAXC];
    SAMPLE *sp = NULL;

    printf("Loading voice samples...  ");
    fflush(stdout);

    for (i = 0; i < 9; i++) {
        for (sam = 0; sam < 128; sam++) {
            if (i)
                bank = 0xE8 + i - 1;
            else
                bank = 0xE4;
            sprintf(name, "%svoice/%02x%02x.wav", path, bank, sam + 0x80);

            voices[i][sam] = load_sample(name);

            if (!voices[i][sam]) {
                sprintf(name, "%svoice/%02X%02X.WAV", path, bank, sam + 0x80);
                voices[i][sam] = load_sample(name);
            }

            if (voices[i][sam]) {
                ld++;
                if (!sp)
                    sp = voices[i][sam];
            }
        }
    }

    printf("%d samples loaded\n", ld);

    if (ld > 0) {
        voice_num = allocate_voice(sp);
        if (voice_num != -1)
            voice_ok = 1;
        else {
            printf("  ERROR: could not allocate sound card voice\n");
            voice_ok = 0;
        }
    }*/

}

void update_voice(void)
{
    /*if (!voice_ok)
        return;
    if (voice_st == 2) {
        if (voice_get_position(voice_num) < 0) {
            if ((voice_bank >= 0) && (voice_bank < 9) && (voice_addr >= 0x80)
                    && (voice_addr <= 0xff)) {
                if (voices[voice_bank][voice_addr - 0x80]) {
                    reallocate_voice(voice_num,
                            voices[voice_bank][voice_addr - 0x80]);
                    voice_set_volume(voice_num, (255 * app_data.vvolume) / 100);
                    voice_start(voice_num);
                    clk_voice_start = clk_counter;
                    voice_st = 1;
                } else {
                    voice_st = 0;
                }
            }
        }
    } else if (voice_st == 1) {
        if ((voice_get_position(voice_num) < 0)
                || (clk_counter - clk_voice_start > 20)) {
            voice_st = 0;
        }
    }*/
}

void trigger_voice(int addr)
{
    /*if (voice_ok) {
        if (voice_st)
            update_voice();
        if ((voice_st == 0) && (voice_bank >= 0) && (voice_bank < 9)
                && (addr >= 0x80) && (addr <= 0xff)) {
            voice_addr = addr;
            voice_st = 2;
            update_voice();
        }
    }*/
}

void set_voice_bank(int bank)
{
    /*if (!voice_ok)
        return;
    if ((bank >= 0) && (bank <= 8))
        voice_bank = bank;*/
}

int get_voice_status(void)
{
    /*if (voice_ok) {
        update_voice();
        if (voice_st)
            return 1;
    }*/
    return 0;
}

void reset_voice(void)
{
    /*if (voice_ok) {
        voice_stop(voice_num);
        voice_bank = 0;
        voice_addr = 0;
        voice_st = 0;
    }*/
}
