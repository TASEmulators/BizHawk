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

#include <stdio.h>
#include <stdint.h>
#include <sys/types.h>
#include <emulibc.h>
#include <string.h>

#define EXPORT ECL_EXPORT

#include "cartridge.h"
#include "cycles.h"
#include "gameboy.h"
#include "global.h"
#include "gpu.h"
#include "input.h"
#include "sound.h"
#include "serial.h"
#include "utils.h"
#include "mmu.h"
#include "sound_output.h"

/* proto */
void cb();
void connected_cb();
void disconnected_cb();
void rumble_cb(uint8_t rumble);
void network_send_data(uint8_t v);
void *start_thread(void *args);
void *start_thread_network(void *args);

/* frame buffer pointer */
uint16_t *fb;

/* magnify rate */
float magnify_rate = 1.f;

/* cartridge name */
char cart_name[64];

int main(void)
{
}

EXPORT int Init(const void *rom, int romlen)
{
	/* init global variables */
	global_init();

	/* first, load cartridge */
	char ret = cartridge_load(rom, romlen);

	if (ret != 0)
		return 0; // failure

	gameboy_init();

	/* init GPU */
	gpu_init(&cb);

	/* set rumble cb */
	mmu_set_rumble_cb(&rumble_cb);

	/* get frame buffer reference */
	fb = gpu_get_frame_buffer();

	sound_output_init(2 * 1024 * 1024, 44100);

	return 1;
}

static uint32_t fb32[160 * 144];

typedef struct
{
	uint32_t* vbuff;
	int16_t* sbuff;
	int32_t clocks; // desired(in) actual(out) time to run; 2MHZ
	int32_t samples; // actual number of samples produced
	uint16_t keys; // keypad input
} frameinfo_t;


EXPORT void FrameAdvance(frameinfo_t* frame)
{
	input_set_keys(frame->keys);
	uint64_t current = cycles.sampleclock;
	gameboy_run(current + frame->clocks);
	frame->clocks = cycles.sampleclock - current;
	memcpy(frame->vbuff, fb32, 160 * 144 * sizeof(uint32_t));
	frame->samples = sound_output_read(frame->sbuff);
}

EXPORT int IsCGB(void)
{
	return global_cgb;
}

void cb()
{
	// frame received into fb
	uint16_t *src = fb;
	uint8_t *dst = (uint8_t *)fb32;

	for (int i = 0; i < 160 * 144; i++)
	{
		uint16_t c = *src++;
		*dst++ = c << 3 & 0xf8 | c >> 2 & 7;
		*dst++ = c >> 3 & 0xfa | c >> 9 & 3;
		*dst++ = c >> 8 & 0xf8 | c >> 13 & 7;
		*dst++ = 0xff;
	}
}

void connected_cb()
{
	utils_log("Connected\n");
}

void disconnected_cb()
{
	utils_log("Disconnected\n");
}

void rumble_cb(uint8_t rumble)
{
	if (rumble)
		printf("RUMBLE\n");
}
