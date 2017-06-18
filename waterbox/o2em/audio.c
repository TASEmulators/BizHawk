
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
 *   O2 audio emulation
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "cpu.h"
#include "types.h"
#include "config.h"
#include "vmachine.h"
#include "audio.h"

#define SAMPLE_RATE 44100
#define PERIOD1 11
#define PERIOD2 44

#define SOUND_BUFFER_LEN 1056

#define AUD_CTRL 0xAA
#define AUD_D0 0xA7
#define AUD_D1 0xA8
#define AUD_D2 0xA9

int sound_IRQ;

static double flt_a = 0.0, flt_b = 0.0;
static unsigned char flt_prv = 0;

void audio_process(unsigned char *buffer)
{
	unsigned long aud_data;
	int volume, re_circ, noise, enabled, intena, period, pnt, cnt, rndbit, pos;

	aud_data = (VDCwrite[AUD_D2] | (VDCwrite[AUD_D1] << 8) | (VDCwrite[AUD_D0] << 16));

	intena = VDCwrite[0xA0] & 0x04;

	pnt = cnt = 0;

	noise = VDCwrite[AUD_CTRL] & 0x10;
	enabled = VDCwrite[AUD_CTRL] & 0x80;
	rndbit = (enabled && noise) ? (rand() % 2) : 0;

	while (pnt < SOUND_BUFFER_LEN)
	{
		pos = (tweakedaudio) ? (pnt / 3) : (MAXLINES - 1);
		volume = AudioVector[pos] & 0x0F;
		enabled = AudioVector[pos] & 0x80;
		period = (AudioVector[pos] & 0x20) ? PERIOD1 : PERIOD2;
		re_circ = AudioVector[pos] & 0x40;

		buffer[pnt++] = (enabled) ? ((aud_data & 0x01) ^ rndbit) * (0x10 * volume) : 0;
		cnt++;

		if (cnt >= period)
		{
			cnt = 0;
			aud_data = (re_circ) ? ((aud_data >> 1) | ((aud_data & 1) << 23)) : (aud_data >> 1);
			rndbit = (enabled && noise) ? (rand() % 2) : 0;

			if (enabled && intena && (!sound_IRQ))
			{
				sound_IRQ = 1;
				ext_IRQ();
			}
		}
	}

	//if (app_data.filter)
		//filter(buffer, SOUND_BUFFER_LEN);
}

void update_audio(void)
{
	unsigned char scratch[4096];
	audio_process(scratch);
}

void init_audio(void)
{
	sound_IRQ = 0;
}
