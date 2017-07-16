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
#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
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
#include "sgb.h"

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

EXPORT int Init(const void *rom, int romlen, int sgb, const void *spc, int spclen)
{
	/* init global variables */
	global_init();

	/* first, load cartridge */
	char ret = cartridge_load(rom, romlen);

	if (ret != 0)
		return 0; // failure
	global_sgb = !!sgb;
	if (global_sgb && global_cgb)
		utils_log("Warn: CGB game in SGB mode\n");
	if (sgb && !sgb_init((const uint8_t*)spc, spclen))
		return 0;

	gameboy_init();

	/* init GPU */
	gpu_init(frame_cb);

	/* set rumble cb */
	mmu_set_rumble_cb(&rumble_cb);

	sound_output_init(global_sgb ? 2147727 : 2097152, 44100);

	return 1;
}

typedef struct
{
	uint32_t *VideoBuffer;
	int16_t *SoundBuffer;
	int64_t Cycles;
	int32_t Width;
	int32_t Height;
	int32_t Samples;
	int32_t Lagged;
	int64_t Time;
	uint32_t Keys;
} MyFrameInfo;

static uint32_t *current_vbuff;
static uint64_t overflow;

EXPORT void FrameAdvance(MyFrameInfo *frame)
{
	if (global_sgb)
		sgb_set_controller_data((uint8_t *)&frame->Keys);
	else
		input_set_keys(frame->Keys);
	current_vbuff = frame->VideoBuffer;
	global_lagged = 1;
	global_currenttime = frame->Time;

	uint64_t current = cycles.sampleclock;
	uint64_t target = current + 35112 - overflow;
	gameboy_run(target);
	uint64_t elapsed = cycles.sampleclock - current;
	frame->Cycles = elapsed;
	overflow = cycles.sampleclock - target;

	frame->Samples = sound_output_read(frame->SoundBuffer);
	if (global_sgb)
	{
		frame->Width = 256;
		frame->Height = 224;
	}
	else
	{
		frame->Width = 160;
		frame->Height = 144;
	}
	frame->Lagged = global_lagged;
	current_vbuff = NULL;
}

EXPORT int IsCGB(void)
{
	return global_cgb;
}

EXPORT void SetInputCallback(void (*callback)(void))
{
	global_input_callback = callback;
}

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = mmu.memory;
	m[0].Name = "Fake System Bus";
	m[0].Size = 0x10000;
	m[0].Flags = MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE1;
}

EXPORT int GetSaveramSize(void)
{
	return mmu_saveram_size();
}

EXPORT void PutSaveram(const uint8_t* data, int size)
{
	mmu_restore_saveram(data, size);
}

EXPORT void GetSaveram(uint8_t* data, int size)
{
	mmu_save_saveram(data, size);
}

void frame_cb()
{
	if (global_sgb)
	{
		sgb_render_frame(current_vbuff);
	}
	else
	{
		memcpy(current_vbuff, gpu.frame_buffer, sizeof(gpu.frame_buffer));
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
