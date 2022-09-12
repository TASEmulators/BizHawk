#include "jaguar.h"
#include "file.h"
#include "settings.h"
#include "memory.h"
#include "tom.h"
#include "joystick.h"
#include "blip_buf.h"

#include <emulibc.h>
#include <waterboxcore.h>

#include <string.h>

#define EXPORT extern "C" ECL_EXPORT

typedef int8_t s8;
typedef int16_t s16;
typedef int32_t s32;
typedef int64_t s64;

typedef uint8_t u8;
typedef uint16_t u16;
typedef uint32_t u32;
typedef uint64_t u64;

struct BizSettings
{
	u8 hardwareTypeNTSC;
	u8 useJaguarBIOS;
	u8 useFastBlitter;
};

void SoundCallback(u16 * buffer, int length);
static u16* soundBuf;
static blip_t* blipL;
static blip_t* blipR;
static s16 latchL, latchR;

EXPORT bool Init(BizSettings* bizSettings, u8* boot, u8* rom, u32 sz)
{
	vjs.GPUEnabled = true;
	vjs.DSPEnabled = true;
	vjs.usePipelinedDSP = false;
	vjs.renderType = RT_NORMAL;
	vjs.hardwareTypeNTSC = bizSettings->hardwareTypeNTSC;
	vjs.useJaguarBIOS = bizSettings->useJaguarBIOS;
	vjs.useFastBlitter = bizSettings->useFastBlitter;
	soundBuf = alloc_invisible<u16>(2048);
	blipL = blip_new(1024);
	blipR = blip_new(1024);
	blip_set_rates(blipL, 48000, 44100);
	blip_set_rates(blipR, 48000, 44100);

	JaguarInit();

	if (!JaguarLoadFile(rom, sz))
	{
		if (!AlpineLoadFile(rom, sz))
		{
			return false;
		}

		vjs.hardwareTypeAlpine = true;
	}
	else
	{
		vjs.hardwareTypeAlpine = ParseFileType(rom, sz) == JST_ALPINE;
	}

	SET32(jaguarMainRAM, 0, 0x00200000);
	memcpy(jagMemSpace + 0xE00000, boot, 0x20000);

	JaguarReset();

	return true;
}

extern uint16_t eeprom_ram[64];
extern bool eeprom_dirty;

EXPORT bool SaveRamIsDirty()
{
	return eeprom_dirty;
}

EXPORT void GetSaveRam(u8* dst)
{
	memcpy(dst, eeprom_ram, sizeof(eeprom_ram));
	eeprom_dirty = false;
}

EXPORT void PutSaveRam(u8* src)
{
	memcpy(eeprom_ram, src, sizeof(eeprom_ram));
	eeprom_dirty = false;
}

EXPORT void GetMemoryAreas(MemoryArea* m)
{
	m[0].Data = jaguarMainRAM;
	m[0].Name = "Main RAM";
	m[0].Size = 0x200000;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[1].Data = eeprom_ram;
	m[1].Name = "EEPROM";
	m[1].Size = sizeof(eeprom_ram);
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_SAVERAMMABLE;

	m[2].Data = gpuRAM;
	m[2].Name = "GPU RAM";
	m[2].Size = 0x18000;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[3].Data = dspRAM;
	m[3].Name = "DSP RAM";
	m[3].Size = 0x5000;
	m[3].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[4].Data = TOMGetRamPointer();
	m[4].Name = "TOM RAM";
	m[4].Size = 0x4000;
	m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[5].Data = jaguarMainROM;
	m[5].Name = "ROM";
	m[5].Size = jaguarROMSize;
	m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[6].Data = jagMemSpace + 0xE00000;
	m[6].Name = "BIOS";
	m[6].Size = 0x20000;
	m[6].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;

	m[7].Data = jagMemSpace;
	m[7].Name = "System Bus";
	m[7].Size = 0xF20000;
	m[7].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE;
}

struct MyFrameInfo : public FrameInfo
{
	u32 player1, player2;
	bool reset;
};

bool lagged;
void (*inputcb)() = 0;

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->reset)
	{
		JaguarReset();
	}

	for (u32 i = 0; i < 21; i++)
	{
		joypad0Buttons[i] = (f->player1 >> i) & 1;
		joypad1Buttons[i] = (f->player2 >> i) & 1;
	}

	lagged = true;
	JaguarSetScreenPitch(TOMGetVideoModeWidth());
	JaguarSetScreenBuffer(f->VideoBuffer);

	JaguarExecuteNew();

	JaguarSetScreenBuffer(nullptr);
	f->Width = TOMGetVideoModeWidth();
	f->Height = TOMGetVideoModeHeight();
	f->Lagged = lagged;

	u32 samples = 48000 / (vjs.hardwareTypeNTSC ? 60 : 50);
	SoundCallback(soundBuf, samples * 4);
	s16* sb = reinterpret_cast<s16*>(soundBuf);
	for (u32 i = 0; i < samples; i++)
	{
		s16 l = *sb++;
		if (latchL != l)
		{
			blip_add_delta(blipL, i, latchL - l);
			latchL = l;
		}

		s16 r = *sb++;
		if (latchR != r)
		{
			blip_add_delta(blipR, i, latchR - r);
			latchR = r;
		}
	}

	blip_end_frame(blipL, samples);
	blip_end_frame(blipR, samples);

	f->Samples = blip_samples_avail(blipL);
	blip_read_samples(blipL, f->SoundBuffer + 0, f->Samples, 1);
	blip_read_samples(blipR, f->SoundBuffer + 1, f->Samples, 1);
}

EXPORT void SetInputCallback(void (*callback)())
{
	inputcb = callback;
}
