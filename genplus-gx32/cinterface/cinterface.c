#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>
#include "callbacks.h"

#ifdef _MSC_VER
#define snprintf _snprintf
#endif

#include "shared.h"
#include "libretro.h"
#include "state.h"
#include "genesis.h"
#include "md_ntsc.h"
#include "sms_ntsc.h"
#include "eeprom_i2c.h"

char GG_ROM[256] = "GG_ROM"; // game genie rom
char AR_ROM[256] = "AR_ROM"; // actin replay rom
char SK_ROM[256] = "SK_ROM"; // sanic and knuckles
char SK_UPMEM[256] = "SK_UPMEM"; // sanic and knuckles
char GG_BIOS[256] = "GG_BIOS"; // game gear bootrom
char CD_BIOS_EU[256] = "CD_BIOS_EU"; // cd bioses
char CD_BIOS_US[256] = "CD_BIOS_US";
char CD_BIOS_JP[256] = "CD_BIOS_JP";
char MS_BIOS_US[256] = "MS_BIOS_US"; // master system bioses
char MS_BIOS_EU[256] = "MS_BIOS_EU";
char MS_BIOS_JP[256] = "MS_BIOS_JP";

char romextension[4];

static uint32_t bitmap_data_[1024 * 512];

static int16 soundbuffer[4096];
static int nsamples;

int cinterface_render_bga = 1;
int cinterface_render_bgb = 1;
int cinterface_render_bgw = 1;
int cinterface_render_obj = 1;
uint8 cinterface_custom_backdrop = 0;
uint32 cinterface_custom_backdrop_color = 0xffff00ff; // pink
extern uint8 border;

#define GPGX_EX __declspec(dllexport)

static int vwidth;
static int vheight;

static uint8_t brm_format[0x40] =
{
  0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x00,0x00,0x00,0x00,0x40,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x53,0x45,0x47,0x41,0x5f,0x43,0x44,0x5f,0x52,0x4f,0x4d,0x00,0x01,0x00,0x00,0x00,
  0x52,0x41,0x4d,0x5f,0x43,0x41,0x52,0x54,0x52,0x49,0x44,0x47,0x45,0x5f,0x5f,0x5f
};

extern void zap(void);

void (*biz_execcb)(unsigned addr) = NULL;
void (*biz_readcb)(unsigned addr) = NULL;
void (*biz_writecb)(unsigned addr) = NULL;
CDCallback biz_cdcallback = NULL;
unsigned biz_lastpc = 0;

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

GPGX_EX int gpgx_state_max_size(void)
{
	// original state size, plus 64K sram or 16K ebram, plus 8K ibram or seeprom control structures
	return STATE_SIZE + (64 + 8) * 1024;
}

GPGX_EX int gpgx_state_size(void *dest, int size)
{
	int actual = 0;
	if (size < gpgx_state_max_size())
		return -1;

	actual = state_save((unsigned char*) dest);
	if (actual > size)
		// fixme!
		return -1;
	return actual;
}

GPGX_EX int gpgx_state_save(void *dest, int size)
{
	return state_save((unsigned char*) dest) == size;
}

GPGX_EX int gpgx_state_load(void *src, int size)
{
	if (!size)
		return 0;

	if (state_load((unsigned char *) src) == size)
	{
		update_viewport();
		return 1;
	}
	else
		return 0;
}

void osd_input_update(void)
{
}

void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

GPGX_EX void gpgx_set_input_callback(void (*fecb)(void))
{
	input_callback_cb = fecb;
}

int (*load_archive_cb)(const char *filename, unsigned char *buffer, int maxsize);

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


extern uint8 bg_pattern_cache[];
extern uint32 pixel[];

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
int saveramsize(void)
{
	return sram_get_actual_size();
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
		brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (sizeof(scd.bram) / 64) - 3;
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
// we copy the bram bits next to the ebram bits

GPGX_EX void gpgx_sram_prepread(void)
{
	if (!sram.on && cdd.loaded && scd.cartridge.id)
	{
		void *dest = scd.cartridge.area + scd.cartridge.mask + 1;
		memcpy(dest, scd.bram, 0x2000);
	}
}

GPGX_EX void gpgx_sram_commitwrite(void)
{
	if (!sram.on && cdd.loaded && scd.cartridge.id)
	{
		void *src = scd.cartridge.area + scd.cartridge.mask + 1;
		memcpy(scd.bram, src, 0x2000);
	}
}

GPGX_EX void gpgx_poke_vram(int addr, uint8 val)
{
	write_vram_byte(addr, val);
}

GPGX_EX void gpgx_flush_vram(void)
{
	flush_vram_cache();
}

GPGX_EX const char* gpgx_get_memdom(int which, void **area, int *size)
{
	if (!area || !size)
		return NULL;
	switch (which)
	{
	case 0:
		*area = work_ram;
		*size = 0x10000;
		return "68K RAM";
	case 1:
		*area = zram;
		*size = 0x2000;
		return "Z80 RAM";
	case 2:
		if (!cdd.loaded)
		{
			*area = ext.md_cart.rom;
			*size = ext.md_cart.romsize;
			return "MD CART";
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
		*area = boot_rom;
		*size = 0x800;
		return "BOOT ROM";
	default:
		return NULL;
	case 10:
		if (sram.on)
		{
			*area = sram.sram;
			*size = saveramsize();
			return "SRAM";
		}
		else return NULL;
	case 11:
		*area = cram;
		*size = 128;
		return "CRAM";
	case 12:
		*area = vsram;
		*size = 128;
		return "VSRAM";
	case 13:
		*area = vram;
		*size = 65536;
		return "VRAM";
	}
}

GPGX_EX void gpgx_write_m68k_bus(unsigned addr, unsigned data)
{
	unsigned char *base = m68k.memory_map[addr >> 16 & 0xff].base;
	if (base)
		base[addr & 0xffff ^ 1] = data;
}

GPGX_EX void gpgx_write_s68k_bus(unsigned addr, unsigned data)
{
	unsigned char *base = s68k.memory_map[addr >> 16 & 0xff].base;
	if (base)
		base[addr & 0xffff ^ 1] = data;
}
GPGX_EX unsigned gpgx_peek_m68k_bus(unsigned addr)
{
	unsigned char *base = m68k.memory_map[addr >> 16 & 0xff].base;
	if (base)
		return base[addr & 0xffff ^ 1];
	else
		return 0xff;
}
GPGX_EX unsigned gpgx_peek_s68k_bus(unsigned addr)
{
	unsigned char *base = s68k.memory_map[addr >> 16 & 0xff].base;
	if (base)
		return base[addr & 0xffff ^ 1];
	else
		return 0xff;
}

GPGX_EX void gpgx_get_sram(void **area, int *size)
{
	if (!area || !size)
		return;

	if (sram.on)
	{
		*area = sram.sram;
		*size = saveramsize();
	}
	else if (scd.cartridge.id)
	{
		*area = scd.cartridge.area;
		*size = scd.cartridge.mask + 1 + 0x2000;
	}
	else if (cdd.loaded)
	{
		*area = scd.bram;
		*size = 0x2000;
	}
	else
	{
		if (area)
			*area = NULL;
		if (size)
			*size = 0;
	}
}

struct InitSettings
{
	uint8_t Filter;
	uint16_t LowPassRange;
	int16_t LowFreq;
	int16_t HighFreq;
	int16_t LowGain;
	int16_t MidGain;
	int16_t HighGain;
	uint32_t BackdropColor;
};

GPGX_EX int gpgx_init(const char *feromextension, int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize), int sixbutton, char system_a, char system_b, int region, struct InitSettings *settings)
{
	zap();

	memset(&bitmap, 0, sizeof(bitmap));
	memset(bitmap_data_, 0, sizeof(bitmap_data_));
	
	strncpy(romextension, feromextension, 3);
	romextension[3] = 0;

	load_archive_cb = feload_archive_cb;

	bitmap.width = 1024;
	bitmap.height = 512;
	bitmap.pitch = 1024 * 4;
	bitmap.data = (uint8_t *)bitmap_data_;

	/* sound options */
	config.psg_preamp  = 150;
	config.fm_preamp= 100;
	config.hq_fm = 1; /* high-quality resampling */
	config.psgBoostNoise  = 1;
	config.filter = settings->Filter; //0; /* no filter */
	config.lp_range = settings->LowPassRange; //0x9999; /* 0.6 in 16.16 fixed point */
	config.low_freq = settings->LowFreq; //880;
	config.high_freq = settings->HighFreq; //5000;
	config.lg = settings->LowGain; //1.0;
	config.mg = settings->MidGain; //1.0;
	config.hg = settings->HighGain; //1.0;
	config.dac_bits = 14; /* MAX DEPTH */ 
	config.ym2413= 2; /* AUTO */
	config.mono  = 0; /* STEREO output */

	/* system options */
	config.system = 0; /* AUTO */
	config.region_detect = region; // see loadrom.c
	config.vdp_mode = 0; /* AUTO */
	config.master_clock = 0; /* AUTO */
	config.force_dtack = 0;
	config.addr_error = 1;
	config.bios = 0;
	config.lock_on = 0;

	/* video options */
	config.overscan = 0;
	config.gg_extra = 0;
	config.ntsc = 0;
	config.render = 0;

	// set overall input system type
	// usual is MD GAMEPAD or NONE
	// TEAMPLAYER, WAYPLAY, ACTIVATOR, XEA1P, MOUSE need to be specified
	// everything else is auto or master system only
	// XEA1P is port 1 only
	// WAYPLAY is both ports at same time only
	input.system[0] = system_a;
	input.system[1] = system_b;

	cinterface_custom_backdrop_color = settings->BackdropColor;

	// apparently, the only part of config.input used is the padtype identifier,
	// and that's used only for choosing pad type when system_md
	{
		int i;
		for (i = 0; i < MAX_INPUTS; i++)
			config.input[i].padtype = sixbutton ? DEVICE_PAD6B : DEVICE_PAD3B;
	}

	if (!load_rom("PRIMARY_ROM"))
		return 0;

	audio_init(44100, 0);
	system_init();
	system_reset();

	update_viewport();
	gpgx_clear_sram();

	return 1;
}

GPGX_EX void gpgx_reset(int hard)
{
	if (hard)
		system_reset();
	else
		gen_reset(0);
}

GPGX_EX void gpgx_set_mem_callback(void (*read)(unsigned), void (*write)(unsigned), void (*exec)(unsigned))
{
	biz_readcb = read;
	biz_writecb = write;
	biz_execcb = exec;
}

GPGX_EX void gpgx_set_cd_callback(CDCallback cdcallback)
{
	biz_cdcallback = cdcallback;
}

GPGX_EX void gpgx_set_draw_mask(int mask)
{
	cinterface_render_bga = !!(mask & 1);
	cinterface_render_bgb = !!(mask & 2);
	cinterface_render_bgw = !!(mask & 4);
	cinterface_render_obj = !!(mask & 8);
	cinterface_custom_backdrop = !!(mask & 16);
	if (cinterface_custom_backdrop)
		color_update_m5(0, 0);
	else
		color_update_m5(0x00, *(uint16 *)&cram[border << 1]);
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
#define MAKEREG(x) regs->name = "M68K " #x; regs->value = m68k_get_reg(M68K_REG_##x); regs++; ret++;
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
	MAKEREG(PC);
	MAKEREG(SR);
	MAKEREG(SP);
	MAKEREG(USP);
	MAKEREG(ISP);
	MAKEREG(IR);
#undef MAKEREG

	(regs-6)->value = biz_lastpc; // during read/write callbacks, PC runs away due to prefetch. restore it.

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
	MAKEREG(D0);
	MAKEREG(D1);
	MAKEREG(D2);
	MAKEREG(D3);
	MAKEREG(D4);
	MAKEREG(D5);
	MAKEREG(D6);
	MAKEREG(D7);
	MAKEREG(A0);
	MAKEREG(A1);
	MAKEREG(A2);
	MAKEREG(A3);
	MAKEREG(A4);
	MAKEREG(A5);
	MAKEREG(A6);
	MAKEREG(A7);
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
