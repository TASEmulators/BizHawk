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

static u32 rgbCallback(GB_gameboy_t *, u8 r, u8 g, u8 b)
{
    return (0xFF << 24) | (r << 16) | (g << 8) | b;
}

typedef void (*input_callback_t)(void);
typedef void (*trace_callback_t)(void);
typedef void (*memory_callback_t)(u16);

typedef struct
{
	GB_gameboy_t gb;
	input_callback_t input_cb;
	trace_callback_t trace_cb;
	memory_callback_t read_cb;
	//memory_callback_t write_cb;
	memory_callback_t exec_cb;
	u64 cc;
} biz_t;

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

EXPORT void sameboy_frameadvance(biz_t* biz, u32 input, u32* vbuf)
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

	GB_set_pixels_output(&biz->gb, vbuf);
	GB_set_border_mode(&biz->gb, GB_BORDER_NEVER);
	
	do
	{
		if ((biz->trace_cb || biz->exec_cb) && !biz->gb.halted && !biz->gb.stopped && !biz->gb.hdma_on)
		{
			if (biz->trace_cb)
				biz->trace_cb();
			
			if (biz->exec_cb)
				biz->exec_cb(biz->gb.pc);
		}

		u32 oldjoyp = biz->gb.io_registers[GB_IO_JOYP] & 0x30;
		biz->cc += GB_run(&biz->gb) >> 2;
		u32 newjoyp = biz->gb.io_registers[GB_IO_JOYP] & 0x30;
		if (oldjoyp != newjoyp && newjoyp != 0x30)
			biz->input_cb();
	}
	while (!biz->gb.vblank_just_occured);
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
	return GB_load_state_from_buffer(&biz->gb, data, len);
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
	// copying sameboy code in memory.c until API is offered to correctly peek at memory
	if (addr < 0x4000)
	{
		if (addr < 0x100 && !biz->gb.boot_rom_finished)
			return biz->gb.boot_rom[addr];

		if (addr >= 0x200 && addr < 0x900 && GB_is_cgb(&biz->gb) && !biz->gb.boot_rom_finished)
			return biz->gb.boot_rom[addr];

		if (!biz->gb.rom_size)
			return 0xFF;

		u32 effectiveAddr = (addr & 0x3FFF) + biz->gb.mbc_rom0_bank * 0x4000;
		return biz->gb.rom[effectiveAddr & (biz->gb.rom_size - 1)];
	}
	else if (addr < 0x8000)
	{
		u32 effectiveAddr = (addr & 0x3FFF) + biz->gb.mbc_rom_bank * 0x4000;
		return biz->gb.rom[effectiveAddr & (biz->gb.rom_size - 1)];
	}
	else if (addr < 0xA000)
	{
		if (biz->gb.vram_read_blocked)
			return 0xFF;

		if (biz->gb.display_state == 22 && GB_is_cgb(&biz->gb) && !biz->gb.cgb_double_speed)
		{
			if (addr & 0x1000)
				addr = biz->gb.last_tile_index_address;
			else if (biz->gb.last_tile_data_address & 0x1000) {}
			else
				addr = biz->gb.last_tile_data_address;
		}
		return biz->gb.vram[(addr & 0x1FFF) + (biz->gb.cgb_vram_bank ? 0x2000 : 0)];
	}
	else if (addr < 0xC000)
	{
		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_MBC7)
		{
			if (!biz->gb.mbc_ram_enable || !biz->gb.mbc7.secondary_ram_enable)
				return 0xFF;

			if (addr >= 0xB000)
				return 0xFF;

			switch ((addr >> 4) & 0xF)
			{
				case 2: return biz->gb.mbc7.x_latch;
				case 3: return biz->gb.mbc7.x_latch >> 8;
				case 4: return biz->gb.mbc7.y_latch;
				case 5: return biz->gb.mbc7.y_latch >> 8;
				case 6: return 0;
				case 8: return biz->gb.mbc7.eeprom_do | (biz->gb.mbc7.eeprom_di << 1) | (biz->gb.mbc7.eeprom_clk << 6) | (biz->gb.mbc7.eeprom_cs << 7);
			}

			return 0xFF;
		}

		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_HUC3)
		{
			switch (biz->gb.huc3.mode)
			{
				case 0xC:
					if (biz->gb.huc3.access_flags == 0x2)
						return 1;

					return biz->gb.huc3.read;
				case 0xD:
					return 1;
				case 0xE:
					return biz->gb.effective_ir_input;
				default:
					return 1;
				case 0:
				case 0xA:
					break;
			}
		}
		
		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_TPP1)
		{
			switch (biz->gb.tpp1.mode)
			{
				case 0:
					switch (addr & 3)
					{
						case 0: return biz->gb.tpp1.rom_bank;
						case 1: return biz->gb.tpp1.rom_bank >> 8;
						case 2: return biz->gb.tpp1.ram_bank;
						case 3: return biz->gb.rumble_strength | biz->gb.tpp1_mr4;
					}
				case 2:
				case 3:
					break;
				case 5:
					return biz->gb.rtc_latched.data[(addr & 3) ^ 3];
				default:
					return 0xFF;
			}
		}
		else if ((!biz->gb.mbc_ram_enable) && biz->gb.cartridge_type->mbc_subtype != GB_cartridge_t::GB_CAMERA && biz->gb.cartridge_type->mbc_type != GB_cartridge_t::GB_HUC1 && biz->gb.cartridge_type->mbc_type != GB_cartridge_t::GB_HUC3)
			return 0xFF;
		
		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_HUC1 && biz->gb.huc1.ir_mode)
			return 0xC0 | biz->gb.effective_ir_input;

		if (biz->gb.cartridge_type->has_rtc && biz->gb.cartridge_type->mbc_type != GB_cartridge_t::GB_HUC3 && biz->gb.mbc3.rtc_mapped)
		{
			if (biz->gb.mbc_ram_bank <= 4)
				return biz->gb.rtc_latched.data[biz->gb.mbc_ram_bank];

			return 0xFF;
		}

		if (biz->gb.camera_registers_mapped)
			return GB_camera_read_register(&biz->gb, addr);

		if (!biz->gb.mbc_ram || !biz->gb.mbc_ram_size)
			return 0xFF;

		if (biz->gb.cartridge_type->mbc_subtype == GB_cartridge_t::GB_CAMERA && biz->gb.mbc_ram_bank == 0 && addr >= 0xA100 && addr < 0xAF00)
			return GB_camera_read_image(&biz->gb, addr - 0xA100);

		u8 effectiveBank = biz->gb.mbc_ram_bank;
		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_MBC3 && !biz->gb.is_mbc30)
		{
			if (biz->gb.cartridge_type->has_rtc && effectiveBank > 3)
				return 0xFF;

			effectiveBank &= 0x3;
		}

		u8 ret = biz->gb.mbc_ram[((addr & 0x1FFF) + effectiveBank * 0x2000) & (biz->gb.mbc_ram_size - 1)];
		if (biz->gb.cartridge_type->mbc_type == GB_cartridge_t::GB_MBC2)
			ret |= 0xF0;

		return ret;
	}
	else if (addr < 0xD000)
	{
		return biz->gb.ram[addr & 0x0FFF];
	}
	else if (addr < 0xE000)
	{
		return biz->gb.ram[(addr & 0x0FFF) + biz->gb.cgb_ram_bank * 0x1000];
	}
	else if (addr < 0xF000)
	{
		return biz->gb.ram[addr & 0x0FFF];
	}
	else
	{
		if (biz->gb.hdma_on)
			return biz->gb.last_opcode_read;

		if (addr < 0xFE00)
			return biz->gb.ram[(addr & 0x0FFF) + biz->gb.cgb_ram_bank * 0x1000];
		
		if (addr < 0xFF00)
		{
			if (biz->gb.oam_write_blocked && !GB_is_cgb(&biz->gb))
				return 0xFF;

			if ((biz->gb.dma_steps_left && (biz->gb.dma_cycles > 0 || biz->gb.is_dma_restarting)))
				return 0xFF;

			if (biz->gb.oam_read_blocked)
				return 0xFF;

			if (addr < 0xFEA0)
				return biz->gb.oam[addr & 0xFF];

			if (biz->gb.oam_read_blocked)
				return 0xFF;

			if (GB_is_cgb(&biz->gb))
				return (addr & 0xF0) | ((addr >> 4) & 0xF);
			else
				return 0;
		}

		if (addr < 0xFF80)
		{
			switch (addr & 0xFF)
			{
				case GB_IO_IF:
					return biz->gb.io_registers[GB_IO_IF] | 0xE0;
				case GB_IO_TAC:
					return biz->gb.io_registers[GB_IO_TAC] | 0xF8;
				case GB_IO_STAT:
					return biz->gb.io_registers[GB_IO_STAT] | 0x80;
				case GB_IO_OPRI:
					if (!GB_is_cgb(&biz->gb))
						return 0xFF;

					return biz->gb.io_registers[GB_IO_OPRI] | 0xFE;
				case GB_IO_PCM12:
					if (!GB_is_cgb(&biz->gb))
						return 0xFF;

					return (biz->gb.apu.is_active[GB_SQUARE_2] ? (biz->gb.apu.samples[GB_SQUARE_2] << 4) : 0) | (biz->gb.apu.is_active[GB_SQUARE_1] ? (biz->gb.apu.samples[GB_SQUARE_1]) : 0);
				case GB_IO_PCM34:
					if (!GB_is_cgb(&biz->gb))
						return 0xFF;

					return (biz->gb.apu.is_active[GB_NOISE] ? (biz->gb.apu.samples[GB_NOISE] << 4) : 0) | (biz->gb.apu.is_active[GB_WAVE] ? (biz->gb.apu.samples[GB_WAVE]) : 0);
				case GB_IO_JOYP:
				case GB_IO_TMA:
				case GB_IO_LCDC:
				case GB_IO_SCY:
				case GB_IO_SCX:
				case GB_IO_LY:
				case GB_IO_LYC:
				case GB_IO_BGP:
				case GB_IO_OBP0:
				case GB_IO_OBP1:
				case GB_IO_WY:
				case GB_IO_WX:
				case GB_IO_SC:
				case GB_IO_SB:
				case GB_IO_DMA:
					return biz->gb.io_registers[addr & 0xFF];
				case GB_IO_TIMA:
					if (biz->gb.tima_reload_state == GB_TIMA_RELOADING)
						return 0;

					return biz->gb.io_registers[GB_IO_TIMA];
				case GB_IO_DIV:
					return biz->gb.div_counter >> 8;
				case GB_IO_HDMA5:
					if (!biz->gb.cgb_mode)
						return 0xFF;

					return ((biz->gb.hdma_on || biz->gb.hdma_on_hblank) ? 0 : 0x80) | ((biz->gb.hdma_steps_left - 1) & 0x7F);
				case GB_IO_SVBK:
					if (!biz->gb.cgb_mode)
						return 0xFF;

					return biz->gb.cgb_ram_bank | ~0x7;
				case GB_IO_VBK:
					if (!GB_is_cgb(&biz->gb))
						return 0xFF;

					return biz->gb.cgb_vram_bank | ~0x1;
				case GB_IO_BGPI:
				case GB_IO_OBPI:
					if (!GB_is_cgb(&biz->gb))
						return 0xFF;

					return biz->gb.io_registers[addr & 0xFF] | 0x40;
				case GB_IO_BGPD:
				case GB_IO_OBPD:
					if (!biz->gb.cgb_mode && biz->gb.boot_rom_finished)
						return 0xFF;

					if (biz->gb.cgb_palettes_blocked)
						return 0xFF;

					return ((addr & 0xFF) == GB_IO_BGPD ? biz->gb.background_palettes_data : biz->gb.sprite_palettes_data)[biz->gb.io_registers[(addr & 0xFF) - 1] & 0x3F];
				case GB_IO_KEY1:
					if (!biz->gb.cgb_mode)
						return 0xFF;

					return (biz->gb.io_registers[GB_IO_KEY1] & 0x7F) | (biz->gb.cgb_double_speed ? 0xFE : 0x7E);
				case GB_IO_RP:
				{
					if (!biz->gb.cgb_mode)
						return 0xFF;

					u8 ret = (biz->gb.io_registers[GB_IO_RP] & 0xC1) | 0x2E;
					if (biz->gb.model != GB_MODEL_CGB_E)
						ret |= 0x10;

					if (((biz->gb.io_registers[GB_IO_RP] & 0xC0) == 0xC0 && biz->gb.effective_ir_input) && biz->gb.model != GB_MODEL_AGB)
						ret &= ~2;

					return ret;
				}
				case GB_IO_PSWX:
				case GB_IO_PSWY:
					return GB_is_cgb(&biz->gb) ? biz->gb.io_registers[addr & 0xFF] : 0xFF;
				case GB_IO_PSW:
					return biz->gb.cgb_mode ? biz->gb.io_registers[addr & 0xFF] : 0xFF;
				case GB_IO_UNKNOWN5:
					return GB_is_cgb(&biz->gb) ? biz->gb.io_registers[addr & 0xFF] | 0x8F : 0xFF;
				default:
					if ((addr & 0xFF) >= GB_IO_NR10 && (addr & 0xFF) <= GB_IO_WAV_END)
						return GB_apu_read(&biz->gb, addr & 0xFF);

					return 0xFF;
			}
		}

		if (addr == 0xFFFF)
			return biz->gb.interrupt_enable;

		return biz->gb.hram[addr - 0xFF80];
	}
}

EXPORT void sameboy_cpuwrite(biz_t* biz, u16 addr, u8 value)
{
	GB_write_memory(&biz->gb, addr, value);
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

static u8 ReadCallbackRelay(GB_gameboy_t* gb, u16 addr, u8 data)
{
	((biz_t*)gb)->read_cb(addr);
	return data;
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
			// no write callbacks yet
			break;
		case 2:
			biz->exec_cb = callback;
			break;
	}
}