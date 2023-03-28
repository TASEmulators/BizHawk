#include "jaguar.h"
#include "rom.h"
#include "settings.h"
#include "memory.h"
#include "tom.h"
#include "gpu.h"
#include "dsp.h"
#include "dac.h"
#include "joystick.h"
#include "m68000/m68kinterface.h"

#include <emulibc.h>
#include <waterboxcore.h>

#include <string.h>

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

static void InitCommon(BizSettings* bizSettings)
{
	vjs.hardwareTypeNTSC = bizSettings->hardwareTypeNTSC;
	vjs.useJaguarBIOS = bizSettings->useJaguarBIOS;
	vjs.useFastBlitter = bizSettings->useFastBlitter;
	JaguarInit();
}

ECL_EXPORT bool Init(BizSettings* bizSettings, u8* boot, u8* rom, u32 sz)
{
	InitCommon(bizSettings);

	if (!JaguarLoadROM(rom, sz))
	{
		if (!AlpineLoadROM(rom, sz))
		{
			return false;
		}

		vjs.hardwareTypeAlpine = true;
	}
	else
	{
		vjs.hardwareTypeAlpine = ParseROMType(rom, sz) == JST_ALPINE;
	}

	SET32(jaguarMainRAM, 0, 0x00200000);
	memcpy(jagMemSpace + 0xE00000, boot, 0x20000);

	JaguarReset();

	return true;
}

void (*cd_toc_callback)(void * dest);
void (*cd_read_callback)(int32_t lba, void * dest);

ECL_EXPORT void SetCdCallbacks(void (*ctc)(void * dest), void (*cdrc)(int32_t lba, void * dest))
{
	cd_toc_callback = ctc;
	cd_read_callback = cdrc;
}

ECL_EXPORT void InitWithCd(BizSettings* bizSettings, u8* boot, u8* memtrack)
{
	InitCommon(bizSettings);
	if (memtrack)
	{
		JaguarLoadROM(memtrack, 0x20000);
	}
	vjs.hardwareTypeAlpine = false;

	SET32(jaguarMainRAM, 0, 0x00200000);
	memcpy(jagMemSpace + 0xE00000, boot, 0x20000);

	JaguarReset();
}

// standard cart eeprom
extern u16 eeprom_ram[64];
extern bool eeprom_dirty;

// memtrack ram (used for jagcd)
extern u8 mtMem[0x20000];
extern bool mtDirty;

static inline bool IsMemTrack()
{
	return jaguarMainROMCRC32 == 0xFDF37F47;
}

ECL_EXPORT bool SaveRamIsDirty()
{
	return IsMemTrack() ? mtDirty : eeprom_dirty;
}

ECL_EXPORT void GetSaveRam(u8* dst)
{
	if (IsMemTrack())
	{
		memcpy(dst, mtMem, sizeof(mtMem));
		mtDirty = false;
	}
	else
	{
		memcpy(dst, eeprom_ram, sizeof(eeprom_ram));
		eeprom_dirty = false;
	}
}

ECL_EXPORT void PutSaveRam(u8* src)
{
	if (IsMemTrack())
	{
		memcpy(mtMem, src, sizeof(mtMem));
		mtDirty = false;
	}
	else
	{
		memcpy(eeprom_ram, src, sizeof(eeprom_ram));
		eeprom_dirty = false;
	}
}

extern u8 gpu_ram_8[0x1000];
extern u8 dsp_ram_8[0x2000];
extern u8 cdRam[0x100];
extern u8 blitter_ram[0x100];
extern u8 tomRam8[0x4000];
extern u8 jerry_ram_8[0x10000];
static u8 unmapped;

static inline u8* GetSysBusPtr(u64 address)
{
	address &= 0xFFFFFF;
	unmapped = 0xFF;
	switch (address)
	{
		case 0x000000 ... 0x7FFFFF: return &jaguarMainRAM[address & 0x1FFFFF];
		case 0x800000 ... 0xDFFEFF: return &jaguarMainROM[address - 0x800000];
		case 0xDFFF00 ... 0xDFFFFF: return &cdRam[address & 0xFF];
		case 0xE00000 ... 0xE3FFFF: return &jagMemSpace[address];
		case 0xF00000 ... 0xF021FF: return &tomRam8[address & 0x3FFF];
		case 0xF02200 ... 0xF0229F: return &blitter_ram[address & 0xFF];
		case 0xF022A0 ... 0xF02FFF: return &tomRam8[address & 0x3FFF];
		case 0xF03000 ... 0xF03FFF: return &gpu_ram_8[address & 0xFFF];
		case 0xF04000 ... 0xF0FFFF: return &tomRam8[address & 0x3FFF];
		case 0xF10000 ... 0xF1AFFF: return &jerry_ram_8[address & 0xFFFF];
		case 0xF1B000 ... 0xF1CFFF: return &dsp_ram_8[address - 0xF1B000];
		case 0xF1D000 ... 0xF1FFFF: return &jerry_ram_8[address & 0xFFFF];
		case 0xF20000 ... 0xFFFFFF: return &unmapped;
	}
}

static void SysBusAccess(u8* buffer, u64 address, u64 count, bool write)
{
	if (write)
	{
		while (count--)
		{
			*GetSysBusPtr(address++) = *buffer++;
		}
	}
	else
	{
		while (count--)
		{
			*buffer++ = *GetSysBusPtr(address++);
		}
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{
	m[0].Data = jaguarMainRAM;
	m[0].Name = "DRAM";
	m[0].Size = 0x200000;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_PRIMARY;

	if (IsMemTrack())
	{
		m[1].Data = mtMem;
		m[1].Name = "MEMTRACK RAM";
		m[1].Size = sizeof(mtMem);
		m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SAVERAMMABLE;
	}
	else
	{
		m[1].Data = jaguarCartInserted ? eeprom_ram : NULL;
		m[1].Name = "EEPROM";
		m[1].Size = sizeof(eeprom_ram);
		m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SAVERAMMABLE;
	}

	m[2].Data = gpu_ram_8;
	m[2].Name = "GPU RAM";
	m[2].Size = sizeof(gpu_ram_8);
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[3].Data = dsp_ram_8;
	m[3].Name = "DSP RAM";
	m[3].Size = sizeof(dsp_ram_8);
	m[3].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[4].Data = tomRam8;
	m[4].Name = "TOM RAM";
	m[4].Size = sizeof(tomRam8);
	m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[5].Data = jerry_ram_8;
	m[5].Name = "JERRY RAM";
	m[5].Size = sizeof(jerry_ram_8);
	m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[6].Data = blitter_ram;
	m[6].Name = "BLITTER RAM";
	m[6].Size = sizeof(blitter_ram);
	m[6].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[7].Data = jaguarMainROM;
	m[7].Name = "ROM";
	m[7].Size = jaguarROMSize;
	m[7].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[8].Data = jagMemSpace + 0xE00000;
	m[8].Name = "BIOS";
	m[8].Size = 0x20000;
	m[8].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN;

	m[9].Data = (void*)SysBusAccess;
	m[9].Name = "System Bus";
	m[9].Size = 0x1000000;
	m[9].Flags = MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_FUNCTIONHOOK;
}

struct MyFrameInfo : public FrameInfo
{
	u32 Player1, Player2;
	bool Reset;
};

bool lagged;

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->Reset)
	{
		JaguarReset();
	}

	for (u32 i = 0; i < 21; i++)
	{
		joypad0Buttons[i] = (f->Player1 >> i) & 1;
		joypad1Buttons[i] = (f->Player2 >> i) & 1;
	}

	lagged = true;
	DACResetBuffer(f->SoundBuffer);

	JaguarAdvance();

	TOMBlit(f->VideoBuffer, f->Width, f->Height);
	f->Samples = DACResetBuffer(NULL);
	f->Lagged = lagged;
}

void (*InputCallback)() = 0;

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

void (*ReadCallback)(u32) = 0;
void (*WriteCallback)(u32) = 0;
void (*ExecuteCallback)(u32) = 0;

ECL_EXPORT void SetMemoryCallbacks(void (*rcb)(u32), void (*wcb)(u32), void (*ecb)(u32))
{
	ReadCallback = rcb;
	WriteCallback = wcb;
	ExecuteCallback = ecb;
}

void (*CPUTraceCallback)(u32*) = 0;
void (*GPUTraceCallback)(u32, u32*) = 0;
void (*DSPTraceCallback)(u32, u32*) = 0;

ECL_EXPORT void SetTraceCallbacks(void (*ccb)(u32*), void (*gcb)(u32, u32*), void (*dcb)(u32, u32*))
{
	CPUTraceCallback = ccb;
	GPUTraceCallback = gcb;
	DSPTraceCallback = dcb;
}

extern u32 gpu_pc;
extern u32 dsp_pc;

ECL_EXPORT void GetRegisters(u32* regs)
{
	for (u32 i = 0; i < 18; i++)
	{
		regs[i] = m68k_get_reg((m68k_register_t)i);
	}
	memcpy(&regs[18], gpu_reg_bank_0, 128);
	memcpy(&regs[50], gpu_reg_bank_1, 128);
	memcpy(&regs[82], dsp_reg_bank_0, 128);
	memcpy(&regs[114], dsp_reg_bank_1, 128);
	regs[146] = gpu_pc;
	regs[147] = dsp_pc;
}

ECL_EXPORT void SetRegister(u32 which, u32 val)
{
	switch (which)
	{
		case 0 ... 17: m68k_set_reg((m68k_register_t)which, val); break;
		case 18 ... 49: gpu_reg_bank_0[which - 18] = val; break;
		case 50 ... 81: gpu_reg_bank_1[which - 50] = val; break;
		case 82 ... 113: dsp_reg_bank_0[which - 82] = val; break;
		case 114 ... 145: dsp_reg_bank_1[which - 114] = val; break;
		case 146: gpu_pc = val; break;
		case 147: dsp_pc = val; break;
	}
}
