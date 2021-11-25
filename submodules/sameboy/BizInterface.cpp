extern "C"
{
#include "libsameboy/Core/gb.h"
}

#ifdef _WIN32
	#define EXPORT extern "C" __declspec(dllexport)
#else
	#define EXPORT extern "C"
#endif

typedef uint8_t u8;
typedef uint16_t u16;
typedef uint32_t u32;
typedef uint64_t u64;

typedef enum
{
	IS_DMG = 0,
	IS_CGB = 1,
	IS_AGB = 2,
} LoadFlags;

static u32 rgbCallback(GB_gameboy_t *, u8 r, u8 g, u8 b)
{
    return (0xFF << 24) | (r << 16) | (g << 8) | b;
}

EXPORT GB_gameboy_t* sameboy_create(u8* romdata, u32 romlen, u8* biosdata, u32 bioslen, LoadFlags flags)
{
	GB_gameboy_t* gb = new GB_gameboy_t;
	GB_model_t model = GB_MODEL_DMG_B;
	if (flags)
		model = (flags & IS_AGB) ? GB_MODEL_AGB : GB_MODEL_CGB_E;

	GB_init(gb, model);
	GB_load_rom_from_buffer(gb, romdata, romlen);
	GB_load_boot_rom_from_buffer(gb, biosdata, bioslen);
	GB_set_sample_rate(gb, 44100);
	GB_set_highpass_filter_mode(gb, GB_HIGHPASS_ACCURATE);
	GB_set_rgb_encode_callback(gb, rgbCallback);
	GB_set_palette(gb, &GB_PALETTE_GBL);
	GB_set_color_correction_mode(gb, GB_COLOR_CORRECTION_EMULATE_HARDWARE);
	GB_set_rtc_mode(gb, GB_RTC_MODE_ACCURATE);
	return gb;
}

EXPORT void sameboy_destroy(GB_gameboy_t* gb)
{
	GB_free(gb);
	delete gb;
}

EXPORT void sameboy_setsamplecallback(GB_gameboy_t* gb, GB_sample_callback_t callback)
{
	GB_apu_set_sample_callback(gb, callback);
}

EXPORT u64 sameboy_frameadvance(GB_gameboy_t* gb, u32 input, u32* vbuf)
{
	GB_set_key_state(gb, GB_KEY_RIGHT,  input & (1 << 0));
	GB_set_key_state(gb, GB_KEY_LEFT,   input & (1 << 1));
	GB_set_key_state(gb, GB_KEY_UP,     input & (1 << 2));
	GB_set_key_state(gb, GB_KEY_DOWN,   input & (1 << 3));
	GB_set_key_state(gb, GB_KEY_A,      input & (1 << 4));
	GB_set_key_state(gb, GB_KEY_B,      input & (1 << 5));
	GB_set_key_state(gb, GB_KEY_SELECT, input & (1 << 6));
	GB_set_key_state(gb, GB_KEY_START,  input & (1 << 7));

	GB_set_pixels_output(gb, vbuf);
	GB_set_border_mode(gb, GB_BORDER_NEVER);
	u32 cycles = 0;
	while (true)
	{
		cycles += GB_run(gb);
		if (gb->vblank_just_occured)
			return cycles >> 2;
	}
}

EXPORT void sameboy_reset(GB_gameboy_t* gb)
{
	GB_reset(gb);
}

EXPORT void sameboy_savesram(GB_gameboy_t* gb, u8* dest)
{
	GB_save_battery_to_buffer(gb, dest, GB_save_battery_size(gb));
}

EXPORT void sameboy_loadsram(GB_gameboy_t* gb, u8* data, u32 len)
{
	GB_load_battery_from_buffer(gb, data, len);
}

EXPORT u32 sameboy_sramlen(GB_gameboy_t* gb)
{
	return GB_save_battery_size(gb);
}

EXPORT void sameboy_savestate(GB_gameboy_t* gb, u8* data)
{
	GB_save_state_to_buffer(gb, data);
}

EXPORT u32 sameboy_loadstate(GB_gameboy_t* gb, u8* data, u32 len)
{
	return GB_load_state_from_buffer(gb, data, len);
}

EXPORT u32 sameboy_statelen(GB_gameboy_t* gb)
{
	return GB_get_save_state_size(gb);
}