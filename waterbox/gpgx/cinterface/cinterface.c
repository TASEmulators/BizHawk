#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>
#include <emulibc.h>
#include "callbacks.h"

#include <shared.h>
#include <genesis.h>
#include <md_ntsc.h>
#include <sms_ntsc.h>
#include <eeprom_i2c.h>
#include <vdp_render.h>
#include <debug/cpuhook.h>

// Functions added by us to peek at static structs
// (this is much less invasive than not making them static FYI)
extern int eeprom_i2c_get_size(void);
extern int sms_cart_is_codies(void);
extern int sms_cart_bootrom_size(void);

struct config_t config;

char GG_ROM[256] = "GG_ROM"; // game genie rom
char AR_ROM[256] = "AR_ROM"; // actin replay rom
char SK_ROM[256] = "SK_ROM"; // sanic and knuckles
char SK_UPMEM[256] = "SK_UPMEM"; // sanic and knuckles
char GG_BIOS[256] = "GG_BIOS"; // game gear bootrom
char CD_BIOS_EU[256] = "CD_BIOS_EU"; // cd bioses
char CD_BIOS_US[256] = "CD_BIOS_US";
char CD_BIOS_JP[256] = "CD_BIOS_JP";
char MD_BIOS[256] = "MD_BIOS"; // genesis tmss bootrom
char MS_BIOS_US[256] = "MS_BIOS_US"; // master system bioses
char MS_BIOS_EU[256] = "MS_BIOS_EU";
char MS_BIOS_JP[256] = "MS_BIOS_JP";

char romextension[4];

static int16 soundbuffer[4096];
static int nsamples;

int cinterface_force_sram;

int cinterface_render_bga = 1;
int cinterface_render_bgb = 1;
int cinterface_render_bgw = 1;
int cinterface_render_obj = 1;
uint8 cinterface_custom_backdrop = 0;
uint32 cinterface_custom_backdrop_color = 0xffff00ff; // pink
extern uint8 border;

#define GPGX_EX ECL_EXPORT

static int vwidth;
static int vheight;

static uint8_t brm_format[0x40] =
{
	0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x00,0x00,0x00,0x00,0x40,
	0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
	0x53,0x45,0x47,0x41,0x5f,0x43,0x44,0x5f,0x52,0x4f,0x4d,0x00,0x01,0x00,0x00,0x00,
	0x52,0x41,0x4d,0x5f,0x43,0x41,0x52,0x54,0x52,0x49,0x44,0x47,0x45,0x5f,0x5f,0x5f
};

ECL_ENTRY void (*biz_execcb)(unsigned addr);
ECL_ENTRY void (*biz_readcb)(unsigned addr);
ECL_ENTRY void (*biz_writecb)(unsigned addr);
CDCallback biz_cdcb = NULL;
ECL_ENTRY void (*cdd_readcallback)(int lba, void *dest, int subcode);
uint8 *tempsram;

static void update_viewport(void)
{
	vwidth  = bitmap.viewport.w + (bitmap.viewport.x * 2);
	vheight = bitmap.viewport.h + (bitmap.viewport.y * 2);

	if (config.ntsc)
	{
		if (reg[12] & 1)
			vwidth = MD_NTSC_OUT_WIDTH(vwidth);
		else
			vwidth = SMS_NTSC_OUT_WIDTH(vwidth);
	}

	if (config.render && interlaced)
	{
		vheight = vheight * 2;
	}
}

GPGX_EX void gpgx_get_video(int *w, int *h, int *pitch, void **buffer)
{
	if (w)
		*w = vwidth;
	if (h)
		*h = vheight;
	if (pitch)
		*pitch = bitmap.pitch;
	if (buffer)
		*buffer = bitmap.data;
}

GPGX_EX void gpgx_get_audio(int *n, void **buffer)
{
	if (n)
		*n = nsamples;
	if (buffer)
		*buffer = soundbuffer;
}

// this is most certainly wrong for interlacing
GPGX_EX void gpgx_get_fps(int *num, int *den)
{
	if (vdp_pal)
	{
		if (num)
			*num = 53203424;
		if (den)
			*den = 3420 * 313;
	}
	else
	{
		if (num)
			*num = 53693175;
		if (den)
			*den = 3420 * 262;
	}
}

void osd_input_update(void)
{
}

ECL_ENTRY void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

GPGX_EX void gpgx_set_input_callback(ECL_ENTRY void (*fecb)(void))
{
	input_callback_cb = fecb;
}

GPGX_EX void gpgx_set_cdd_callback(ECL_ENTRY void (*cddcb)(int lba, void *dest, int subcode))
{
	cdd_readcallback = cddcb;
}

ECL_ENTRY int (*load_archive_cb)(const char *filename, unsigned char *buffer, int maxsize);

// return 0 on failure, else actual loaded size
// extension, if not null, should be populated with the extension of the file loaded
// (up to 3 chars and null terminator, no more)
int load_archive(const char *filename, unsigned char *buffer, int maxsize, char *extension)
{
	if (extension)
		memcpy(extension, romextension, 4);

	return load_archive_cb(filename, buffer, maxsize);
}

GPGX_EX int gpgx_get_control(t_input *dest, int bytes)
{
	if (bytes != sizeof(t_input))
		return 0;
	memcpy(dest, &input, sizeof(t_input));
	return 1;
}

GPGX_EX int gpgx_put_control(t_input *src, int bytes)
{
	if (bytes != sizeof(t_input))
		return 0;
	memcpy(&input, src, sizeof(t_input));
	return 1;
}

GPGX_EX void gpgx_advance(void)
{
	if (system_hw == SYSTEM_MCD)
		system_frame_scd(0);
	else if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		system_frame_gen(0);
	else
		system_frame_sms(0);

	if (bitmap.viewport.changed & 1)
	{
		bitmap.viewport.changed &= ~1;
		update_viewport();
	}

	nsamples = audio_update(soundbuffer);
}

extern toc_t pending_toc;
extern int8 cd_index;

GPGX_EX void gpgx_swap_disc(const toc_t* toc, int8 index)
{
	if (system_hw == SYSTEM_MCD)
	{
		if (toc)
		{
			char header[0x210];
			cd_index = index;
			memcpy(&pending_toc, toc, sizeof(toc_t));
			cdd_load("HOTSWAP_CD", header);
		}
		else
		{
			cd_index = -1;
			cdd_unload();
		}

		cdd_reset();
	}
}

typedef struct
{
	uint32 width; // in cells
	uint32 height;
	uint32 baseaddr;
} nametable_t;

typedef struct
{
	uint8 *vram; // 64K vram
	uint8 *patterncache; // every pattern, first normal, then hflip, vflip, bothflip
	uint32 *colorcache; // 64 colors
	nametable_t nta;
	nametable_t ntb;
	nametable_t ntw;
} vdpview_t;


extern uint8 bg_pattern_cache[0x80000];
uint32_t pixel[0x100];

GPGX_EX void gpgx_get_vdp_view(vdpview_t *view)
{
	view->vram = vram;
	view->patterncache = bg_pattern_cache;
	view->colorcache = pixel + 0x40;
	view->nta.width = 1 << (playfield_shift - 1);
	view->ntb.width = 1 << (playfield_shift - 1);
	view->nta.height = (playfield_row_mask + 1) >> 3;
	view->ntb.height = (playfield_row_mask + 1) >> 3;
	view->ntw.width = 1 << (5 + (reg[12] & 1));
	view->ntw.height = 32;
	view->nta.baseaddr = ntab;
	view->ntb.baseaddr = ntbb;
	view->ntw.baseaddr = ntwb;
}

// internal: computes sram size (no brams)
static int saveramsize(void)
{
	// the variables in SRAM_T are all part of "configuration", so we don't have to save those.
	// the only thing that needs to be saved is the SRAM itself and the SEEPROM struct (if applicable)

	if (!sram.on)
		return 0;

	switch (sram.custom)
	{
		case 0: // plain bus access saveram
			break;
		case 1: // i2c
			return eeprom_i2c_get_size();
		case 2: // spi
			return sizeof(sram.sram); // it doesn't appear to mask anything internally
		case 3: // 93c
			return 128; // limited to 128 bytes (note: SMS only)
		default:
			return sizeof(sram.sram); // ???
	}

	// figure size for plain bus access saverams
	{
		int startaddr = sram.start / 8192;
		int endaddr = sram.end / 8192 + 1;
		int size = (endaddr - startaddr) * 8192;
		return size;
	}
}

GPGX_EX void gpgx_clear_sram(void)
{
	// clear sram
	if (sram.on)
		memset(sram.sram, 0xff, 0x10000);

	if (cdd.loaded)
	{
		// clear and format bram
		memset(scd.bram, 0, 0x2000);
		brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = 0x00;
		brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (0x2000 / 64) - 3;
		memcpy(scd.bram + 0x2000 - 0x40, brm_format, 0x40);

		if (scd.cartridge.id)
		{
			// clear and format ebram
			memset(scd.cartridge.area, 0x00, scd.cartridge.mask + 1);
			brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = (((scd.cartridge.mask + 1) / 64) - 3) >> 8;
			brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (((scd.cartridge.mask + 1) / 64) - 3) & 0xff;
			memcpy(scd.cartridge.area + scd.cartridge.mask + 1 - 0x40, brm_format, 0x40);
		}
	}
}

// a bit hacky:
// in order to present a single memory block to the frontend,
// we copy the both the bram and the ebram to another area

GPGX_EX void* gpgx_get_sram(int *size)
{
	if (sram.on)
	{
		// codies is not actually battery backed, don't expose it as SRAM
		if (sms_cart_is_codies())
		{
			*size = 0;
			return NULL;
		}

		*size = saveramsize();
		return sram.sram;
	}
	else if (cdd.loaded && scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		memcpy(tempsram, scd.cartridge.area, sz);
		memcpy(tempsram + sz, scd.bram, 0x2000);
		*size = sz + 0x2000;
		return tempsram;
	}
	else if (cdd.loaded)
	{
		*size = 0x2000;
		return scd.bram;
	}
	else if (scd.cartridge.id)
	{
		*size = scd.cartridge.mask + 1;
		return scd.cartridge.area;
	}
	else
	{
		*size = 0;
		return NULL;
	}
}

GPGX_EX int gpgx_put_sram(const uint8 *data, int size)
{
	if (sram.on)
	{
		if (size != saveramsize())
			return 0;
		memcpy(sram.sram, data, size);
		return 1;
	}
	else if (cdd.loaded && scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		if (size != sz + 0x2000)
			return 0;
		memcpy(scd.cartridge.area, data, sz);
		memcpy(scd.bram, data + sz, 0x2000);
		return 1;
	}
	else if (cdd.loaded)
	{
		if (size != 0x2000)
			return 0;
		memcpy(scd.bram, data, size);
		return 1;
	}
	else if (scd.cartridge.id)
	{
		int sz = scd.cartridge.mask + 1;
		if (size != sz)
			return 0;
		memcpy(scd.cartridge.area, data, size);
		return 1;
	}
	else
	{
		if (size != 0)
			return 0;
		return 1; // "successful"?
	}
}

GPGX_EX void gpgx_poke_cram(int addr, uint8 val)
{
	uint16 *p;
	uint16 data;
	int index;

	p = (uint16 *)&cram[addr & 0x7E];
	data = *p;

	if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
	{
		data = ((data & 0x1C0) << 3) | ((data & 0x038) << 2) | ((data & 0x007) << 1);
	}

	if (addr & 1)
	{
		data &= 0xFF00;
		data |= val;
	}
	else
	{
		data &= 0x00FF;
		data |= val << 8;
	}

	if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
	{
		data = ((data & 0xE00) >> 3) | ((data & 0x0E0) >> 2) | ((data & 0x00E) >> 1);
	}

	if (*p != data)
	{
		index = (addr >> 1) & 0x3F;
		*p = data;

		if (index & 0x0F)
		{
			color_update_m5(index, data);
		}

		if (index == border)
		{
			color_update_m5(0x00, data);
		}
	}
}

GPGX_EX void gpgx_poke_vram(int addr, uint8 val)
{
	uint8 *p;
	addr &= 0xFFFF;
	p = &vram[addr];
	if (*p != val)
	{
		int name;
		*p = val;
		// copy of MARK_BG_DIRTY(addr) (to avoid putting this code in vdp_ctrl.c)
		name = (addr >> 5) & 0x7FF;
		if (bg_name_dirty[name] == 0)
		{
			bg_name_list[bg_list_index++] = name;
		}
		bg_name_dirty[name] |= (1 << ((addr >> 2) & 7));
	}
}

GPGX_EX void gpgx_flush_vram(void)
{
	if (bg_list_index)
	{
		update_bg_pattern_cache(bg_list_index);
		bg_list_index = 0;
	}
}

GPGX_EX const char* gpgx_get_memdom(int which, void **area, int *size)
{
	if (!area || !size)
		return NULL;
	switch (which)
	{
	case 0:
		if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		{
			*area = work_ram;
			*size = 0x10000;
			return "68K RAM";
		}
		else if (system_hw == SYSTEM_SG)
		{
			*area = work_ram;
			*size = 0x400;
			return "Main RAM";
		}
		else if (system_hw == SYSTEM_SGII)
		{
			*area = work_ram;
			*size = 0x800;
			return "Main RAM";
		}
		else
		{
			*area = work_ram;
			*size = 0x2000;
			return "Main RAM";
		}
	case 1:
		if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		{
			*area = zram;
			*size = 0x2000;
			return "Z80 RAM";
		}
		else return NULL;
	case 2:
		if (!cdd.loaded)
		{
			*area = ext.md_cart.rom;
			*size = ext.md_cart.romsize;
			if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
			{
				return "MD CART";
			}
			else
			{
				return "ROM";
			}
		}
		else if (scd.cartridge.id)
		{
			*area = scd.cartridge.area;
			*size = scd.cartridge.mask + 1;
			return "EBRAM";
		}
		else return NULL;
	case 3:
		if (cdd.loaded)
		{
			*area = scd.bootrom;
			*size = 0x20000;
			return "CD BOOT ROM";
		}
		else return NULL;
	case 4:
		if (cdd.loaded)
		{
			*area = scd.prg_ram;
			*size = 0x80000;
			return "CD PRG RAM";
		}
		else return NULL;
	case 5:
		if (cdd.loaded)
		{
			*area = scd.word_ram[0];
			*size = 0x20000;
			return "CD WORD RAM[0] (1M)";
		}
		else return NULL;
	case 6:
		if (cdd.loaded)
		{
			*area = scd.word_ram[1];
			*size = 0x20000;
			return "CD WORD RAM[1] (1M)";
		}
		else return NULL;
	case 7:
		if (cdd.loaded)
		{
			*area = scd.word_ram_2M;
			*size = 0x40000;
			return "CD WORD RAM (2M)";
		}
		else return NULL;
	case 8:
		if (cdd.loaded)
		{
			*area = scd.bram;
			*size = 0x2000;
			return "CD BRAM";
		}
		else return NULL;
	case 9:
		if (system_bios & SYSTEM_MD)
		{
			*area = boot_rom;
			*size = 0x800;
			return "MD BOOT ROM";
		}
		else if (system_bios & (SYSTEM_SMS | SYSTEM_GG))
		{
			*area = &ext.md_cart.rom[0x400000];
			*size = sms_cart_bootrom_size();
			return "BOOT ROM";
		}
		else return NULL;
	case 10:
		// these should be mutually exclusive
		if (sram.on)
		{
			*area = sram.sram;
			*size = saveramsize();

			// Codemasters mapper SRAM is only used by 1 game
			// and that 1 game does not actually have a battery
			// (this also mimics SMSHawk's behavior)
			if (sms_cart_is_codies())
			{
				return "Cart (Volatile) RAM";
			}

			return "SRAM";
		}
		else if ((system_hw & SYSTEM_PBC) != SYSTEM_MD)
		{
			*area = &work_ram[0x2000];
			*size = sms_cart_ram_size();
			return "Cart (Volatile) RAM";
		}
		else return NULL;
	case 11:
		if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		{
			// MD has more CRAM
			*size = 0x80;
		}
		else
		{
			*size = 0x40;
		}
		*area = cram;
		return "CRAM";
	case 12:
		*area = vsram;
		*size = 128;
		return "VSRAM";
	case 13:
		if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
		{
			// MD has more VRAM
			*size = 0x10000;
		}
		else
		{
			*size = 0x4000;
		}
		*area = vram;
		return "VRAM";
	default:
		return NULL;
	}
}

GPGX_EX void gpgx_write_z80_bus(unsigned addr, unsigned data)
{
	// note: this is not valid for MD
	z80_writemap[addr >> 10][addr & 0x3FF] = data;
}

GPGX_EX void gpgx_write_m68k_bus(unsigned addr, unsigned data)
{
	cpu_memory_map m = m68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.write8)
		m.base[(addr & 0xffff) ^ 1] = data;
}

GPGX_EX void gpgx_write_s68k_bus(unsigned addr, unsigned data)
{
	cpu_memory_map m = s68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.write8)
		m.base[(addr & 0xffff) ^ 1] = data;
}

GPGX_EX unsigned gpgx_peek_z80_bus(unsigned addr)
{
	// note: this is not valid for MD
	return z80_readmap[addr >> 10][addr & 0x3FF];
}

GPGX_EX unsigned gpgx_peek_m68k_bus(unsigned addr)
{
	cpu_memory_map m = m68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.read8)
		return m.base[(addr & 0xffff) ^ 1];
	else
		return 0xff;
}

GPGX_EX unsigned gpgx_peek_s68k_bus(unsigned addr)
{
	cpu_memory_map m = s68k.memory_map[addr >> 16 & 0xff];
	if (m.base && !m.read8)
		return m.base[(addr & 0xffff) ^ 1];
	else
		return 0xff;
}

enum SMSFMSoundChipType
{
	YM2413_DISABLED,
	YM2413_MAME,
	YM2413_NUKED
};

enum GenesisFMSoundChipType
{
	MAME_YM2612,
	MAME_ASIC_YM3438,
	MAME_Enhanced_YM3438,
	Nuked_YM2612,
	Nuked_YM3438
};

struct InitSettings
{
	uint32_t BackdropColor;
	int32_t Region;
	int32_t ForceVDP;
	uint16_t LowPassRange;
	int16_t LowFreq;
	int16_t HighFreq;
	int16_t LowGain;
	int16_t MidGain;
	int16_t HighGain;
	uint8_t Filter;
	uint8_t InputSystemA;
	uint8_t InputSystemB;
	uint8_t SixButton;
	uint8_t ForceSram;
	uint8_t SMSFMSoundChip;
	uint8_t GenesisFMSoundChip;
	uint8_t SpritesAlwaysOnTop;
	uint8_t LoadBios;
	uint8_t Overscan;
	uint8_t GGExtra;
};


#ifdef HOOK_CPU
#ifdef USE_BIZHAWK_CALLBACKS

void CDLog68k(uint addr, uint flags)
{
	addr &= 0x00FFFFFF;

	//check for sram region first
	if(sram.on)
	{
		if(addr >= sram.start && addr <= sram.end)
		{
			biz_cdcb(addr - sram.start, eCDLog_AddrType_SRAM, flags);
			return;
		}
	}

	if(addr < 0x400000)
	{
		uint block64k_rom;

		//apply memory map to process rom address
		unsigned char* block64k = m68k.memory_map[((addr)>>16)&0xff].base;
		
		//outside the ROM range. complex mapping logic/accessories; not sure how to handle any of this
		if(block64k < cart.rom || block64k >= cart.rom + cart.romsize)
			return;

		block64k_rom = block64k - cart.rom;
		addr = ((addr) & 0xffff) + block64k_rom;

		//outside the ROM range somehow
		if(addr >= cart.romsize)
			return;

		biz_cdcb(addr, eCDLog_AddrType_MDCART, flags);
		return;
	}

	if(addr > 0xFF0000)
	{
		//no memory map needed
		biz_cdcb(addr & 0xFFFF, eCDLog_AddrType_RAM68k, flags);
		return;
	}
}

void bk_cpu_hook(hook_type_t type, int width, unsigned int address, unsigned int value)
{
	switch (type)
	{
		case HOOK_M68K_E:
		{
			if (biz_execcb)
				biz_execcb(address);

			if (biz_cdcb)
			{
				CDLog68k(address, eCDLog_Flags_Exec68k);
				CDLog68k(address + 1, eCDLog_Flags_Exec68k);
			}

			break;
		}

		case HOOK_M68K_R:
		{
			if (biz_readcb)
				biz_readcb(address);

			break;
		}

		case HOOK_M68K_W:
		{
			if (biz_writecb)
				biz_writecb(address);

			break;
		}

		default: break;
	}
}

#endif // USE_BIZHAWK_CALLBACKS
#endif // HOOK_CPU

GPGX_EX int gpgx_init(const char* feromextension,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	fprintf(stderr, "Initializing GPGX native...\n");

	cinterface_force_sram = settings->ForceSram;

	memset(&bitmap, 0, sizeof(bitmap));

	strncpy(romextension, feromextension, 3);
	romextension[3] = 0;

	load_archive_cb = feload_archive_cb;

	bitmap.width  = 1024;
	bitmap.height = 512;
	bitmap.pitch  = 1024 * 4;
	bitmap.data   = alloc_invisible(2 * 1024 * 1024);
	tempsram      = alloc_invisible(0x100000 + 0x2000);

	// Initializing ram deepfreeze list
#ifdef USE_RAM_DEEPFREEZE
	deepfreeze_list_size = 0;
#endif

	/* sound options */
	config.psg_preamp            = 150;
	config.fm_preamp             = 100;
	config.cdda_volume           = 100;
	config.pcm_volume            = 100;
	config.hq_fm                 = 1;
	config.hq_psg                = 1;
	config.filter                = settings->Filter; //0; /* no filter */
	config.lp_range              = settings->LowPassRange; //0x9999; /* 0.6 in 16.16 fixed point */
	config.low_freq              = settings->LowFreq; //880;
	config.high_freq             = settings->HighFreq; //5000;
	config.lg                    = settings->LowGain; //100;
	config.mg                    = settings->MidGain; //100;
	config.hg                    = settings->HighGain; //100;
	config.mono                  = 0;
	config.ym3438                = 0;

	// Selecting FM Sound chip to use for SMS / GG emulation. Using a default for now, until we also
	// accept this core for SMS/GG emulation in BizHawk
	switch (settings->SMSFMSoundChip)
	{
		case YM2413_DISABLED:
			config.opll = 0;
			config.ym2413 = 0;
			break;

		case YM2413_MAME:
			config.opll = 0;
			config.ym2413 = 1;
			break;

		case YM2413_NUKED:
			config.opll = 1;
			config.ym2413 = 1;
			break;
	}

	// Selecting FM Sound chip to use for Genesis / Megadrive / CD emulation
	switch (settings->GenesisFMSoundChip)
	{
		case MAME_YM2612:
			config.ym2612 = YM2612_DISCRETE;
			YM2612Config(YM2612_DISCRETE);
			break;

		case MAME_ASIC_YM3438:
			config.ym2612 = YM2612_INTEGRATED;
			YM2612Config(YM2612_INTEGRATED);
			break;

		case MAME_Enhanced_YM3438:
			config.ym2612 = YM2612_ENHANCED;
			YM2612Config(YM2612_ENHANCED);
			break;

		case Nuked_YM2612:
			OPN2_SetChipType(ym3438_mode_ym2612);
			config.ym3438 = 1;
			break;

		case Nuked_YM3438:
			OPN2_SetChipType(ym3438_mode_readmode);
			config.ym3438 = 2;
			break;
	}

	/* system options */
	config.system         = 0; /* = AUTO (or SYSTEM_SG, SYSTEM_SGII, SYSTEM_SGII_RAM_EXT, SYSTEM_MARKIII, SYSTEM_SMS, SYSTEM_SMS2, SYSTEM_GG, SYSTEM_MD) */
	config.region_detect  = settings->Region; /* 0 = AUTO, 1 = USA, 2 = EUROPE, 3 = JAPAN/NTSC, 4 = JAPAN/PAL */
	config.vdp_mode       = settings->ForceVDP; /* 0 = AUTO, 1 = NTSC, 2 = PAL */
	config.master_clock   = 0; /* = AUTO (1 = NTSC, 2 = PAL) */
	config.force_dtack    = 0;
	config.addr_error     = 1;
	config.bios           = settings->LoadBios ? 3 : 0; /* Load BIOS and do NOT unload cartridge (bit 0: load bios, bit 1: keep cartridge loaded) */
	config.lock_on        = 0; /* = OFF (or TYPE_SK, TYPE_GG & TYPE_AR) */
	config.add_on         = 0; /* = HW_ADDON_AUTO (or HW_ADDON_MEGACD, HW_ADDON_MEGASD & HW_ADDON_ONE) */
	config.cd_latency     = 1;

	/* display options */
	config.overscan               = settings->Overscan; /* 3 = all borders (0 = no borders , 1 = vertical borders only, 2 = horizontal borders only) */
	config.gg_extra               = settings->GGExtra; /* 1 = show extended Game Gear screen (256x192) */
	config.render                 = 1;  /* 1 = double resolution output (only when interlaced mode 2 is enabled) */
	config.ntsc                   = 0;
	config.lcd                    = 0;  /* 0.8 fixed point */
	config.enhanced_vscroll       = 0;
	config.enhanced_vscroll_limit = 8;
	config.sprites_always_on_top  = settings->SpritesAlwaysOnTop;

	// set overall input system type
	// usual is MD GAMEPAD or NONE
	// TEAMPLAYER, WAYPLAY, ACTIVATOR, XEA1P, MOUSE need to be specified
	// everything else is auto or master system only
	// XEA1P is port 1 only
	// WAYPLAY is both ports at same time only
	input.system[0] = settings->InputSystemA;
	input.system[1] = settings->InputSystemB;

	cinterface_custom_backdrop_color = settings->BackdropColor;

 	// Default: Genesis
	// apparently, the only part of config.input used is the padtype identifier,
	// and that's used only for choosing pad type when system_md
	for (int i = 0; i < MAX_INPUTS; i++)
		config.input[i].padtype = settings->SixButton ? DEVICE_PAD6B : DEVICE_PAD3B;

	// Hacky but effective. Setting the correct controller type here if this is sms or GG
	// note that we can't use system_hw yet, that's set in load_rom
	if (memcmp("GEN", &romextension[0], 3) != 0)
	{
		for (int i = 0; i < MAX_INPUTS; i++)
			config.input[i].padtype = DEVICE_PAD2B;
	}
	else
	{
		// gpgx won't load the genesis bootrom itself, we have to do that manually
		if (config.bios & 1)
		{
			// not fatal if this fails (unless we're recording a movie, which we handle elsewhere)
			if (load_archive(MD_BIOS, boot_rom, sizeof(boot_rom), NULL) != 0)
			{
#ifdef LSB_FIRST
				// gpgx also expects us to byteswap the boot rom
				for (int i = 0; i < sizeof(boot_rom); i += 2)
				{
					uint8 temp = boot_rom[i];
					boot_rom[i] = boot_rom[i+1];
					boot_rom[i+1] = temp;
				}
#endif
				system_bios |= SYSTEM_MD;
			}
		}
	}

	// first try to load our main CD
	if (!load_rom("PRIMARY_CD"))
	{
		// otherwise, try to load our ROM
		if (!load_rom("PRIMARY_ROM"))
		{
			return 0;
		}
	}

	audio_init(44100, 0);
	system_init();
	system_reset();

	update_viewport();
	gpgx_clear_sram();

	load_archive_cb = NULL; // don't hold onto load_archive_cb for longer than we need it for

	return 1;
}

#ifdef USE_RAM_DEEPFREEZE

GPGX_EX int gpgx_add_deepfreeze_list_entry(const int address, const uint8_t value)
{
	// Prevent overflowing
	if (deepfreeze_list_size == MAX_DEEP_FREEZE_ENTRIES) return -1;

	deepfreeze_list[deepfreeze_list_size].address = address;
	deepfreeze_list[deepfreeze_list_size].value = value;
	deepfreeze_list_size++;

	return 0;
}

GPGX_EX void gpgx_clear_deepfreeze_list()
{
	deepfreeze_list_size = 0;
}

#endif

GPGX_EX void gpgx_reset(int hard)
{
	if (hard)
		system_reset();
	else
		gen_reset(0);
}

GPGX_EX void gpgx_set_mem_callback(ECL_ENTRY void (*read)(unsigned), ECL_ENTRY void (*write)(unsigned), ECL_ENTRY void (*exec)(unsigned))
{
	biz_readcb = read;
	biz_writecb = write;
	biz_execcb = exec;
	set_cpu_hook((biz_readcb || biz_writecb || biz_execcb || biz_cdcb) ? bk_cpu_hook : NULL);
}

GPGX_EX void gpgx_set_cd_callback(CDCallback cdcallback)
{
	biz_cdcb = cdcallback;
	set_cpu_hook((biz_readcb || biz_writecb || biz_execcb || biz_cdcb) ? bk_cpu_hook : NULL);
}

GPGX_EX void gpgx_set_draw_mask(int mask)
{
	cinterface_render_bga = !!(mask & 1);
	cinterface_render_bgb = !!(mask & 2);
	cinterface_render_bgw = !!(mask & 4);
	cinterface_render_obj = !!(mask & 8);
	cinterface_custom_backdrop = !!(mask & 16);

	if (reg[1] & 0x04)
	{
		if (cinterface_custom_backdrop)
			color_update_m5(0, 0);
		else
			color_update_m5(0x00, *(uint16 *)&cram[border << 1]);
	}
}

GPGX_EX void gpgx_set_sprite_limit_enabled(int enabled)
{
	config.no_sprite_limit = !enabled;
}

GPGX_EX void gpgx_invalidate_pattern_cache(void)
{
	bg_list_index = (reg[1] & 0x04) ? 0x800 : 0x200;
	for (int i = 0; i < bg_list_index; i++)
	{
		bg_name_list[i] = i;
		bg_name_dirty[i] = 0xFF;
	}
}

typedef struct
{
	unsigned int value;
	const char *name;
} gpregister_t;

GPGX_EX int gpgx_getmaxnumregs(void)
{
	return 57;
}

GPGX_EX int gpgx_getregs(gpregister_t *regs)
{
	int ret = 0;

	// 22
	if ((system_hw & SYSTEM_PBC) == SYSTEM_MD)
	{
#define MAKEREG(x) regs->name = "M68K " #x; regs->value = m68k_get_reg(M68K_REG_##x); regs++; ret++;
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(PC);
	MAKEREG(SR);
	MAKEREG(SP);
	MAKEREG(USP);
	MAKEREG(ISP);
	MAKEREG(IR);
#undef MAKEREG
	}

	// 13
#define MAKEREG(x) regs->name = "Z80 " #x; regs->value = Z80.x.d; regs++; ret++;
	MAKEREG(pc);
	MAKEREG(sp);
	MAKEREG(af);
	MAKEREG(bc);
	MAKEREG(de);
	MAKEREG(hl);
	MAKEREG(ix);
	MAKEREG(iy);
	MAKEREG(wz);
	MAKEREG(af2);
	MAKEREG(bc2);
	MAKEREG(de2);
	MAKEREG(hl2);
#undef MAKEREG

	// 22
	if (system_hw == SYSTEM_MCD)
	{
#define MAKEREG(x) regs->name = "S68K " #x; regs->value = s68k_get_reg(M68K_REG_##x); regs++; ret++;
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(PC);
	MAKEREG(SR);
	MAKEREG(SP);
	MAKEREG(USP);
	MAKEREG(ISP);
	MAKEREG(IR);
#undef MAKEREG
	}

	return ret;
}
