extern "C"
{
#include "libsameboy/Core/gb.h"
}
#include "stdio.h"

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

static u32 rgbCallback(GB_gameboy_t*, u8 r, u8 g, u8 b)
{
    return (0xFF << 24) | (r << 16) | (g << 8) | b;
}

typedef void (*input_callback_t)(void);
typedef void (*trace_callback_t)(void);
typedef void (*memory_callback_t)(u16);
typedef void (*printer_callback_t)(u32*, u8, u8, u8, u8);
typedef void (*scanline_callback_t)(u32);

typedef struct
{
	GB_gameboy_t gb;
	u32 vbuf[160 * 144];
	input_callback_t input_cb;
	trace_callback_t trace_cb;
	memory_callback_t read_cb;
	memory_callback_t write_cb;
	memory_callback_t exec_cb;
	printer_callback_t printer_cb;
	scanline_callback_t scanline_cb;
	u32 scanline_sl;
	u64 cc;
} biz_t;

static u8 ReadCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 data)
{
	((biz_t*)gb)->read_cb(addr);
	return data;
}

static bool WriteCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 data)
{
	((biz_t*)gb)->write_cb(addr);
	return true;
}

static void PrinterCallbackRelay(GB_gameboy_t* gb, u32* image, u8 height, u8 top_margin, u8 bottom_margin, u8 exposure)
{
	((biz_t*)gb)->printer_cb(image, height, top_margin, bottom_margin, exposure);
}

EXPORT int sameboy_corelen(biz_t* biz)
{
	return sizeof biz->gb;
}

EXPORT biz_t* sameboy_create(u8* romdata, u32 romlen, u8* biosdata, u32 bioslen, LoadFlags flags)
{
	biz_t* biz = new biz_t;
	GB_model_t model = GB_MODEL_DMG_B;
	if (flags)
		model = (flags & IS_AGB) ? GB_MODEL_AGB : GB_MODEL_CGB_E;

	GB_random_seed(0);
	GB_init(&biz->gb, model);
	GB_load_rom_from_buffer(&biz->gb, romdata, romlen);
	GB_load_boot_rom_from_buffer(&biz->gb, biosdata, bioslen);
	GB_set_sample_rate(&biz->gb, 44100);
	GB_set_highpass_filter_mode(&biz->gb, GB_HIGHPASS_ACCURATE);
	GB_set_rgb_encode_callback(&biz->gb, rgbCallback);
	GB_set_palette(&biz->gb, &GB_PALETTE_GREY);
	GB_set_color_correction_mode(&biz->gb, GB_COLOR_CORRECTION_EMULATE_HARDWARE);
	GB_set_rtc_mode(&biz->gb, GB_RTC_MODE_ACCURATE);
	return biz;
}

EXPORT void sameboy_destroy(biz_t* biz)
{
	GB_free(&biz->gb);
	delete biz;
}

EXPORT void sameboy_setsamplecallback(biz_t* biz, GB_sample_callback_t callback)
{
	GB_apu_set_sample_callback(&biz->gb, callback);
}

EXPORT void sameboy_setinputcallback(biz_t* biz, input_callback_t callback)
{
	biz->input_cb = callback;
}

EXPORT void sameboy_frameadvance(biz_t* biz, u32 input, u32* vbuf, bool render)
{
	GB_set_key_state(&biz->gb, GB_KEY_RIGHT,  input & (1 << 0));
	GB_set_key_state(&biz->gb, GB_KEY_LEFT,   input & (1 << 1));
	GB_set_key_state(&biz->gb, GB_KEY_UP,     input & (1 << 2));
	GB_set_key_state(&biz->gb, GB_KEY_DOWN,   input & (1 << 3));
	GB_set_key_state(&biz->gb, GB_KEY_A,      input & (1 << 4));
	GB_set_key_state(&biz->gb, GB_KEY_B,      input & (1 << 5));
	GB_set_key_state(&biz->gb, GB_KEY_SELECT, input & (1 << 6));
	GB_set_key_state(&biz->gb, GB_KEY_START,  input & (1 << 7));

	if ((biz->gb.io_registers[GB_IO_JOYP] & 0x30) != 0x30)
		biz->input_cb();

	GB_set_pixels_output(&biz->gb, biz->vbuf);
	GB_set_border_mode(&biz->gb, GB_BORDER_NEVER);
	GB_set_rendering_disabled(&biz->gb, !render);

	u32 cycles = 0;
	do
	{
		if ((biz->trace_cb || biz->exec_cb) && !biz->gb.halted && !biz->gb.stopped && !biz->gb.hdma_on && !((biz->gb.interrupt_enable & biz->gb.io_registers[GB_IO_IF] & 0x1F) && (biz->gb.ime && !biz->gb.ime_toggle)))
		{
			if (biz->trace_cb)
				biz->trace_cb();
			
			if (biz->exec_cb)
				biz->exec_cb(biz->gb.pc);
		}

		u32 oldjoyp = biz->gb.io_registers[GB_IO_JOYP] & 0x30;
		u32 oldly = biz->gb.io_registers[GB_IO_LY];
		u32 ret = GB_run(&biz->gb) >> 2;
		cycles += ret;
		biz->cc += ret;
		u32 newjoyp = biz->gb.io_registers[GB_IO_JOYP] & 0x30;
		if (oldjoyp != newjoyp && newjoyp != 0x30)
			biz->input_cb();
		u32 newly = biz->gb.io_registers[GB_IO_LY];
		if (biz->scanline_cb && oldly != newly && biz->scanline_sl == newly)
			biz->scanline_cb(biz->gb.io_registers[GB_IO_LCDC]);
	}
	while (!biz->gb.vblank_just_occured && cycles < 35112);
	
	if (biz->gb.vblank_just_occured && render)
		memcpy(vbuf, biz->vbuf, sizeof biz->vbuf);
}

EXPORT void sameboy_reset(biz_t* biz)
{
	GB_random_seed(0);
	GB_reset(&biz->gb);
}

EXPORT void sameboy_savesram(biz_t* biz, u8* dest)
{
	GB_save_battery_to_buffer(&biz->gb, dest, GB_save_battery_size(&biz->gb));
}

EXPORT void sameboy_loadsram(biz_t* biz, u8* data, u32 len)
{
	GB_load_battery_from_buffer(&biz->gb, data, len);
}

EXPORT u32 sameboy_sramlen(biz_t* biz)
{
	return GB_save_battery_size(&biz->gb);
}

EXPORT void sameboy_savestate(biz_t* biz, u8* data)
{
	GB_save_state_to_buffer(&biz->gb, data);
}

EXPORT u32 sameboy_loadstate(biz_t* biz, u8* data, u32 len)
{
	u32 ret = GB_load_state_from_buffer(&biz->gb, data, len);
	biz->gb.debugger_ticks = 0;
	biz->gb.absolute_debugger_ticks = 0;
	biz->gb.rumble_off_cycles = 0;
	biz->gb.vblank_just_occured = 0;
	auto temp = biz->gb.apu_output.sample_callback;
	memset(&biz->gb.apu_output, 0, sizeof biz->gb.apu_output);
	GB_apu_set_sample_callback(&biz->gb, temp);
	GB_set_sample_rate(&biz->gb, 44100);
	return ret;
}

EXPORT u32 sameboy_statelen(biz_t* biz)
{
	return GB_get_save_state_size(&biz->gb);
}

EXPORT bool sameboy_getmemoryarea(biz_t* biz, GB_direct_access_t which, void** data, size_t* len)
{
	if (which > GB_DIRECT_ACCESS_IE || which < GB_DIRECT_ACCESS_ROM)
		return false;
	
	u16 bank;
	*data = GB_get_direct_access(&biz->gb, which, len, &bank);
	return true;
}

EXPORT u8 sameboy_cpuread(biz_t* biz, u16 addr)
{
	GB_set_read_memory_callback(&biz->gb, nullptr);
	u8 ret = GB_safe_read_memory(&biz->gb, addr);
	GB_set_read_memory_callback(&biz->gb, biz->read_cb ? ReadCallbackRelay : nullptr);
	return ret;
}

EXPORT void sameboy_cpuwrite(biz_t* biz, u16 addr, u8 value)
{
	GB_set_write_memory_callback(&biz->gb, nullptr);
	GB_write_memory(&biz->gb, addr, value);
	GB_set_write_memory_callback(&biz->gb, biz->write_cb ? WriteCallbackRelay : nullptr);
}

EXPORT u64 sameboy_getcyclecount(biz_t* biz)
{
	return biz->cc;
}

EXPORT void sameboy_setcyclecount(biz_t* biz, u64 newCc)
{
	biz->cc = newCc;
}

EXPORT void sameboy_settracecallback(biz_t* biz, trace_callback_t callback)
{
	biz->trace_cb = callback;
}

EXPORT void sameboy_getregs(biz_t* biz, u32* buf)
{
	buf[0] = biz->gb.pc & 0xFFFF;
	buf[1] = biz->gb.a & 0xFF;
	buf[2] = biz->gb.f & 0xFF;
	buf[3] = biz->gb.b & 0xFF;
	buf[4] = biz->gb.c & 0xFF;
	buf[5] = biz->gb.d & 0xFF;
	buf[6] = biz->gb.e & 0xFF;
	buf[7] = biz->gb.h & 0xFF;
	buf[8] = biz->gb.l & 0xFF;
	buf[9] = biz->gb.sp & 0xFFFF;
}

EXPORT void sameboy_setreg(biz_t* biz, u32 which, u32 value)
{
	switch (which)
	{
		case 0:
			biz->gb.pc = value & 0xFFFF;
			break;
		case 1:
			biz->gb.a = value & 0xFF;
			break;
		case 2:
			biz->gb.f = value & 0xFF;
			break;
		case 3:
			biz->gb.b = value & 0xFF;
			break;
		case 4:
			biz->gb.c = value & 0xFF;
			break;
		case 5:
			biz->gb.d = value & 0xFF;
			break;
		case 6:
			biz->gb.e = value & 0xFF;
			break;
		case 7:
			biz->gb.h = value & 0xFF;
			break;
		case 8:
			biz->gb.l = value & 0xFF;
			break;
		case 9:
			biz->gb.sp = value & 0xFFFF;
			break;
	}
}

EXPORT void sameboy_setmemorycallback(biz_t* biz, u32 which, memory_callback_t callback)
{
	switch (which)
	{
		case 0:
			biz->read_cb = callback;
			GB_set_read_memory_callback(&biz->gb, callback ? ReadCallbackRelay : nullptr);
			break;
		case 1:
			biz->write_cb = callback;
			GB_set_write_memory_callback(&biz->gb, callback ? WriteCallbackRelay : nullptr);
			break;
		case 2:
			biz->exec_cb = callback;
			break;
	}
}

EXPORT void sameboy_setprintercallback(biz_t* biz, printer_callback_t callback)
{
	biz->printer_cb = callback;
	GB_connect_printer(&biz->gb, callback ? PrinterCallbackRelay : nullptr);
	if (!callback)
	{
		GB_set_serial_transfer_bit_start_callback(&biz->gb, nullptr);
		GB_set_serial_transfer_bit_end_callback(&biz->gb, nullptr);
	}
}

EXPORT void sameboy_setscanlinecallback(biz_t* biz, scanline_callback_t callback, u32 sl)
{
	biz->scanline_cb = callback;
	biz->scanline_sl = sl;
}