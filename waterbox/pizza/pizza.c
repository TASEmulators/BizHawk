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
void frame_cb();
void connected_cb();
void disconnected_cb();
void rumble_cb(uint8_t rumble);
void network_send_data(uint8_t v);
void *start_thread(void *args);
void *start_thread_network(void *args);

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
	gpu_init(frame_cb);

	/* set rumble cb */
	mmu_set_rumble_cb(&rumble_cb);

	sound_output_init(2 * 1024 * 1024, 44100);

	return 1;
}

typedef struct
{
	uint32_t* vbuff;
	int16_t* sbuff;
	int32_t clocks; // desired(in) actual(out) time to run; 2MHZ
	int32_t samples; // actual number of samples produced
	uint16_t keys; // keypad input
} frameinfo_t;

static uint32_t* current_vbuff;

EXPORT void FrameAdvance(frameinfo_t* frame)
{
	input_set_keys(frame->keys);
	uint64_t current = cycles.sampleclock;
	current_vbuff = frame->vbuff;
	gameboy_run(current + frame->clocks);
	frame->clocks = cycles.sampleclock - current;
	frame->samples = sound_output_read(frame->sbuff);
	current_vbuff = NULL;
}

EXPORT int IsCGB(void)
{
	return global_cgb;
}

void frame_cb()
{
	memcpy(current_vbuff, gpu.frame_buffer, sizeof(gpu.frame_buffer));
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
