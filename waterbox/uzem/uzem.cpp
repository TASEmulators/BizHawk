/*
(The MIT License)

Copyright (c) 2008-2016 by
David Etherton, Eric Anderton, Alec Bourque (Uze), Filipe Rinaldi,
Sandor Zsuga (Jubatian), Matt Pandina (Artcfox)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#include "uzem.h"
#include "avr8.h"
#include "uzerom.h"
#include "blip_buf.h"
#include <limits.h>
#include <string.h>
#include <stdlib.h>
#include <time.h>
#include <stdio.h>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

// header for use with UzeRom files
static RomHeader uzeRomHeader;
static avr8 uzebox;
static blip_t* blip;

void avr8::shutdown(int errcode)
{
	printf("Oh no, that's bad!\n");
}

int main(void)
{
	return 0;
}

ECL_EXPORT bool MouseEnabled()
{
	return uzeRomHeader.mouse;
}

ECL_EXPORT bool Init()
{
	const char *heximage = "romfile";

	unsigned char *buffer = (unsigned char *)(uzebox.progmem);

	if (isUzeromFile(heximage))
	{
		printf("-- Loading UzeROM Image --\n");
		if (!loadUzeImage(heximage, &uzeRomHeader, buffer))
		{
			printf("Error: cannot load UzeRom file '%s'.\n\n", heximage);
			return false;
		}
		// enable mouse support if required
		if (uzeRomHeader.mouse)
		{
			printf("Mouse support enabled\n");
		}
	}
	else
	{
		printf("Error: Doesn't seem to be an UzeROM image\n");
		return false;
		/*printf("Loading Hex Image...\n");
		if (!loadHex(heximage, buffer))
		{
			printf("Error: cannot load HEX image '%s'.\n\n", heximage);
			return false;
		}*/
	}

	uzebox.decodeFlash();
	if (!uzebox.init_gui())
		return false;

	uzebox.randomSeed = time(NULL);
	srand(uzebox.randomSeed); //used for the watchdog timer entropy

	blip = blip_new(2048);
	blip_set_rates(blip, 315000000.0 / 11.0, 44100);

	return true;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = uzebox.sram;
	m[0].Name = "SRAM";
	m[0].Size = sramSize;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[1].Data = uzebox.eeprom;
	m[1].Name = "EEPROM";
	m[1].Size = eepromSize;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[2].Data = uzebox.progmem;
	m[2].Name = "ROM";
	m[2].Size = progSize;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE2;
}

struct MyFrameInfo : public FrameInfo
{
	uint32_t Buttons[3];
};

static int cycles;

static int audio_value;
void SampleCallback(uint8_t val)
{
	int v = (val - 128) * 100;
	int delta = v - audio_value;
	if (delta)
		blip_add_delta(blip, cycles, delta);
	audio_value = v;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	cycles = 0;
	uzebox.video_buffer = f->VideoBuffer;
	uzebox.lagged = true;
	uzebox.buttons[0] = ~f->Buttons[0];
	uzebox.buttons[1] = ~f->Buttons[1];
	if (f->Buttons[2] & 1) // power pressed
	{
		uzebox.PIND = uzebox.PIND & ~0b00001100;
	}
	else
	{
		uzebox.PIND |= 0b00001100;
	}

	while (uzebox.scanline_count == -999 && cycles < 700000)
	{
		cycles += uzebox.exec();
	}
	while (uzebox.scanline_count != -999 && cycles < 700000)
	{
		cycles += uzebox.exec();
	}
	uzebox.video_buffer = nullptr;
	f->Cycles = cycles;
	f->Width = VIDEO_DISP_WIDTH;
	f->Height = 224;
	blip_end_frame(blip, cycles);
	f->Samples = blip_read_samples(blip, f->SoundBuffer, 2048, true);
	for (int i = 0; i < f->Samples; i++)
		f->SoundBuffer[i * 2 + 1] = f->SoundBuffer[i * 2];
	f->Lagged = uzebox.lagged;
}

void (*InputCallback)();

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
