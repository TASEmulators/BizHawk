#include <stdint.h>
#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
#include "blip_buf/blip_buf.h"

#define _Static_assert static_assert

extern "C" {
#include "gb.h"
#include "joypad.h"
#include "apu.h"
#include "sgb.h"
}

static GB_gameboy_t GB;

static uint32_t GBPixels[160 * 144];
static uint32_t *CurrentFramebuffer;
static bool sgb;
static void VBlankCallback(GB_gameboy_t *gb)
{
	if (sgb)
	{
		sgb_take_frame(GBPixels);
		sgb_render_frame(CurrentFramebuffer);
	}
	else
	{
		memcpy(CurrentFramebuffer, GBPixels, sizeof(GBPixels));
	}
}

static void LogCallback(GB_gameboy_t *gb, const char *string, GB_log_attributes attributes)
{
	fputs(string, stdout);
}

static uint32_t RgbEncodeCallback(GB_gameboy_t *gb, uint8_t r, uint8_t g, uint8_t b)
{
	return b | g << 8 | r << 16 | 0xff000000;
}

static void InfraredCallback(GB_gameboy_t *gb, bool on, long cycles_since_last_update)
{
}

static void RumbleCallback(GB_gameboy_t *gb, bool rumble_on)
{
}

static void SerialStartCallback(GB_gameboy_t *gb, uint8_t byte_to_send)
{
}

static uint8_t SerialEndCallback(GB_gameboy_t *gb)
{
	return 0;
}

static void (*FrontendInputCallback)();

static void InputCallback(GB_gameboy_t *gb)
{
	FrontendInputCallback();
}

typedef void (*FrontendPrinterCallback_t)(uint32_t *image,
										  uint8_t height,
										  uint8_t top_margin,
										  uint8_t bottom_margin,
										  uint8_t exposure);

static FrontendPrinterCallback_t FrontendPrinterCallback;

static void PrinterCallback(GB_gameboy_t *gb,
							uint32_t *image,
							uint8_t height,
							uint8_t top_margin,
							uint8_t bottom_margin,
							uint8_t exposure)
{
	FrontendPrinterCallback(image, height, top_margin, bottom_margin, exposure);
}

static blip_t *leftblip;
static blip_t *rightblip;
const int SOUND_RATE_GB = 2097152;
const int SOUND_RATE_SGB = 2147727;
static uint64_t sound_start_clock;
static GB_sample_t sample_gb;
static GB_sample_t sample_sgb;

static void SampleCallback(GB_gameboy_t *gb, GB_sample_t sample, uint64_t clock)
{
	int l = sample.left - sample_gb.left;
	int r = sample.right - sample_gb.right;
	if (l)
		blip_add_delta(leftblip, clock - sound_start_clock, l);
	if (r)
		blip_add_delta(rightblip, clock - sound_start_clock, r);
	sample_gb = sample;
}
static void SgbSampleCallback(int16_t sl, int16_t sr, uint64_t clock)
{
	int l = sl - sample_sgb.left;
	int r = sr - sample_sgb.right;
	if (l)
		blip_add_delta(leftblip, clock - sound_start_clock, l);
	if (r)
		blip_add_delta(rightblip, clock - sound_start_clock, r);
	sample_sgb.left = sl;
	sample_sgb.right = sr;
}

ECL_EXPORT bool Init(bool cgb, const uint8_t *spc, int spclen)
{
	if (spc)
	{
		GB_init_sgb(&GB);
		if (!sgb_init(spc, spclen))
			return false;
		sgb = true;
	}
	else if (cgb)
	{
		GB_init_cgb(&GB);
	}
	else
	{
		GB_init(&GB);
	}

	if (GB_load_boot_rom(&GB, "boot.rom") != 0)
		return false;
	if (GB_load_rom(&GB, "game.rom") != 0)
		return false;

	GB_set_pixels_output(&GB, GBPixels);
	GB_set_vblank_callback(&GB, VBlankCallback);
	GB_set_log_callback(&GB, LogCallback);
	GB_set_rgb_encode_callback(&GB, RgbEncodeCallback);
	GB_set_infrared_callback(&GB, InfraredCallback);
	GB_set_rumble_callback(&GB, RumbleCallback);
	GB_set_sample_callback(&GB, SampleCallback);

	leftblip = blip_new(1024);
	rightblip = blip_new(1024);
	blip_set_rates(leftblip, sgb ? SOUND_RATE_SGB : SOUND_RATE_GB, 44100);
	blip_set_rates(rightblip, sgb ? SOUND_RATE_SGB : SOUND_RATE_GB, 44100);

	return true;
}

struct MyFrameInfo : public FrameInfo
{
	int64_t Time;
	uint32_t Keys;
};

static int FrameOverflow;

ECL_EXPORT void FrameAdvance(MyFrameInfo &f)
{
	if (sgb)
	{
		sgb_set_controller_data((uint8_t *)&f.Keys);
	}
	else
	{
		GB_set_key_state(&GB, f.Keys & 0xff);
	}
	sound_start_clock = GB_epoch(&GB);
	CurrentFramebuffer = f.VideoBuffer;
	GB_set_lagged(&GB, true);
	GB.frontend_rtc_time = f.Time;

	uint32_t target = 35112 - FrameOverflow;
	f.Cycles = GB_run_cycles(&GB, target);
	FrameOverflow = f.Cycles - target;
	if (sgb)
	{
		f.Width = 256;
		f.Height = 224;
		sgb_render_audio(GB_epoch(&GB), SgbSampleCallback);
	}
	else
	{
		f.Width = 160;
		f.Height = 144;
	}
	blip_end_frame(leftblip, f.Cycles);
	blip_end_frame(rightblip, f.Cycles);
	f.Samples = blip_read_samples(leftblip, f.SoundBuffer, 2048, 1);
	blip_read_samples(rightblip, f.SoundBuffer + 1, 2048, 1);
	CurrentFramebuffer = NULL;
	f.Lagged = GB_get_lagged(&GB);
}

static void SetMemoryArea(MemoryArea *m, GB_direct_access_t access, const char *name, int32_t flags)
{
	size_t size;
	m->Name = name;
	m->Data = GB_get_direct_access(&GB, access, &size, nullptr);
	m->Size = size;
	m->Flags = flags;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	// TODO: "System Bus"
	SetMemoryArea(m + 0, GB_DIRECT_ACCESS_RAM, "WRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY);
	SetMemoryArea(m + 1, GB_DIRECT_ACCESS_ROM, "ROM", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 2, GB_DIRECT_ACCESS_VRAM, "VRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 3, GB_DIRECT_ACCESS_CART_RAM, "CartRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 4, GB_DIRECT_ACCESS_OAM, "OAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 5, GB_DIRECT_ACCESS_HRAM, "HRAM", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 6, GB_DIRECT_ACCESS_IO, "IO", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 7, GB_DIRECT_ACCESS_BOOTROM, "BOOTROM", MEMORYAREA_FLAGS_WORDSIZE1);
	SetMemoryArea(m + 8, GB_DIRECT_ACCESS_BGP, "BGP", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
	SetMemoryArea(m + 9, GB_DIRECT_ACCESS_OBP, "OBP", MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE);
}

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	FrontendInputCallback = callback;
	GB_set_input_callback(&GB, callback ? InputCallback : nullptr);
}

ECL_EXPORT void GetGpuMemory(void **p)
{
	p[0] = GB_get_direct_access(&GB, GB_DIRECT_ACCESS_VRAM, nullptr, nullptr);
	p[1] = GB_get_direct_access(&GB, GB_DIRECT_ACCESS_OAM, nullptr, nullptr);
	p[2] = GB.background_palettes_rgb;
	p[3] = GB.sprite_palettes_rgb;
}

ECL_EXPORT void SetScanlineCallback(void (*callback)(uint8_t), int ly)
{
	GB.scanline_callback = callback;
	GB.scanline_callback_ly = ly;
}
ECL_EXPORT uint8_t GetIoReg(uint8_t port)
{
	return GB.io_registers[port];
}

ECL_EXPORT void PutSaveRam()
{
	GB_load_battery(&GB, "save.ram");
}

ECL_EXPORT void GetSaveRam()
{
	GB_save_battery(&GB, "save.ram");
}

ECL_EXPORT bool HasSaveRam()
{
	if (!GB.cartridge_type->has_battery)
		return false; // Nothing to save.
	if (GB.mbc_ram_size == 0 && !GB.cartridge_type->has_rtc)
		return false; /* Claims to have battery, but has no RAM or RTC */
	return true;
}

ECL_EXPORT void SetPrinterCallback(FrontendPrinterCallback_t callback)
{
	FrontendPrinterCallback = callback;

	if (callback)
	{
		GB_connect_printer(&GB, PrinterCallback);
	}
	else
	{
		GB_set_serial_transfer_start_callback(&GB, NULL);
		GB_set_serial_transfer_end_callback(&GB, NULL);
		GB.printer.callback = NULL;
	}
}

int main()
{
	return 0;
}
