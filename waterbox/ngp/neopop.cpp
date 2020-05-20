//---------------------------------------------------------------------------
// NEOPOP : Emulator as in Dreamland
//
// Copyright (c) 2001-2002 by neopop_uk
//---------------------------------------------------------------------------

//---------------------------------------------------------------------------
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version. See also the license.txt file for
//	additional informations.
//---------------------------------------------------------------------------

#include "neopop.h"

#include "Z80_interface.h"
#include "interrupt.h"
#include "mem.h"
#include "gfx.h"
#include "sound.h"
#include "dma.h"
#include "bios.h"
#include "flash.h"

#include <algorithm>
#include <time.h>

namespace MDFN_IEN_NGP
{
bool lagged;
void (*inputcallback)();


extern uint8 CPUExRAM[16384];

NGPGFX_CLASS *NGPGfx = NULL;

COLOURMODE system_colour = COLOURMODE_AUTO;

uint8 NGPJoyLatch;
uint8 settings_language;
time_t frontend_time;

int (*comms_read_cb)(uint8* buffer);
int (*comms_poll_cb)(uint8* buffer);
void (*comms_write_cb)(uint8 data);

bool system_comms_read(uint8 *buffer)
{
	if (comms_read_cb)
		return comms_read_cb(buffer);
	else
		return false;
}

bool system_comms_poll(uint8 *buffer)
{
	if (comms_poll_cb)
		return comms_poll_cb(buffer);
	else
		return false;
}

void system_comms_write(uint8 data)
{
	if (comms_write_cb)
		comms_write_cb(data);
}

void instruction_error(char *vaMessage, ...)
{
	/*char message[1000];
	va_list vl;

	va_start(vl, vaMessage);
	vsprintf(message, vaMessage, vl);
	va_end(vl);

	MDFN_printf("[PC %06X] %s\n", pc, message);*/
}

bool NGPFrameSkip;
int32 ngpc_soundTS = 0;
//static int32 main_timeaccum;
static int32 z80_runtime;

EXPORT void FrameAdvance(MyFrameInfo* frame)
{
	lagged = true;
	bool MeowMeow = 0;
	MDFN_Surface surface;
	surface.pixels = frame->VideoBuffer;
	surface.pitch32 = 160;
	frame->Width = 160;
	frame->Height = 152;
	frontend_time = frame->FrontendTime;
	storeB(0x6f82, frame->Buttons);

	ngpc_soundTS = 0;
	NGPFrameSkip = frame->SkipRendering;

	do
	{
		int32 timetime = (uint8)TLCS900h_interpret(); // This is sooo not right, but it's replicating the old behavior(which is necessary
													  // now since I've fixed the TLCS900h core and other places not to truncate cycle counts
													  // internally to 8-bits).  Switch to the #if 0'd block of code once we fix cycle counts in the
													  // TLCS900h core(they're all sorts of messed up), and investigate if certain long
													  // instructions are interruptable(by interrupts) and later resumable, RE Rockman Battle
		// & Fighters voice sample playback.

		//if(timetime > 255)
		// printf("%d\n", timetime);

		// Note: Don't call updateTimers with a time/tick/cycle/whatever count greater than 255.
		MeowMeow |= updateTimers(&surface, timetime);

		z80_runtime += timetime;

		while (z80_runtime > 0)
		{
			int z80rantime = Z80_RunOP();

			if (z80rantime < 0) // Z80 inactive, so take up all run time!
			{
				z80_runtime = 0;
				break;
			}

			z80_runtime -= z80rantime << 1;
		}
	} while (!MeowMeow);

	frame->Cycles = ngpc_soundTS;
	frame->Samples = MDFNNGPCSOUND_Flush(frame->SoundBuffer, 8192);
	frame->Lagged = lagged;
}

static MDFN_COLD bool Load(const uint8* romdata, int32 romlength)
{
	const uint64 fp_size = romlength;

	if (fp_size > 1024 * 1024 * 8) // 4MiB maximum ROM size, 2* to be a little tolerant of garbage.
		return false;
	//throw MDFN_Error(0, _("NGP/NGPC ROM image is too large."));

	ngpc_rom.length = fp_size;
	ngpc_rom.data = (uint8*)alloc_plain(ngpc_rom.length);
	memcpy(ngpc_rom.data, romdata, romlength);

	rom_loaded();
	//if (!FLASH_LoadNV())
	//	return false;

	//MDFNMP_Init(1024, 1024 * 1024 * 16 / 1024);

	NGPGfx = new NGPGFX_CLASS();

	//MDFNGameInfo->fps = (uint32)((uint64)6144000 * 65536 * 256 / 515 / 198); // 3072000 * 2 * 10000 / 515 / 198

	MDFNNGPCSOUND_Init();

	//MDFNMP_AddRAM(16384, 0x4000, CPUExRAM);

	SetFRM(); // Set up fast read memory mapping

	bios_install();

	//main_timeaccum = 0;
	z80_runtime = 0;

	reset();

	MDFNNGPC_SetSoundRate(44100);
	return true;
}
}
using namespace MDFN_IEN_NGP;

int main(void)
{
	return 0;
}

EXPORT int LoadSystem(const uint8* rom, int romlength, int language)
{
	settings_language = language;
	return Load(rom, romlength);
}

EXPORT void SetLayers(int enable) // 1, 2, 4  bg,fg,sprites
{
	NGPGfx->SetLayerEnableMask(enable);
}

EXPORT void HardReset()
{
	reset();
}

EXPORT void SetInputCallback(void (*callback)())
{
	inputcallback = callback;
}

EXPORT void SetCommsCallbacks(int (*read_cb)(uint8* buffer), int (*poll_cb)(uint8* buffer), void (*write_cb)(uint8 data))
{
	comms_read_cb = read_cb;
	comms_poll_cb = poll_cb;
	comms_write_cb = write_cb;
}

EXPORT void GetMemoryAreas(MemoryArea* m)
{
	m[0].Data = CPUExRAM;
	m[0].Name = "RAM";
	m[0].Size = 16384;
	m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_WORDSIZE4;

	m[1].Data = ngpc_rom.data;
	m[1].Name = "ROM";
	m[1].Size = ngpc_rom.length;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE4;

	m[2].Data = ngpc_rom.orig_data;
	m[2].Name = "ORIGINAL ROM";
	m[2].Size = ngpc_rom.length;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4;
}

EXPORT bool HasSaveRam()
{
	return FLASH_IsModified();
}

EXPORT bool PutSaveRam(const uint8* data, uint32 length)
{
	return FLASH_LoadNV(data, length);
}

EXPORT void GetSaveRam(void (*callback)(const uint8* data, uint32 length))
{
	FLASH_SaveNV(callback);
}

