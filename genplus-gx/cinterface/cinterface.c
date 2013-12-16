#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

#ifdef _MSC_VER
#define snprintf _snprintf
#endif

#include "shared.h"
#include "libretro.h"
#include "state.h"
#include "genesis.h"
#include "md_ntsc.h"
#include "sms_ntsc.h"

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

#define GPGX_EX __declspec(dllexport)

static int vwidth;
static int vheight;

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

GPGX_EX int gpgx_state_size(void)
{
	return STATE_SIZE;
}

GPGX_EX int gpgx_state_save(void *dest, int size)
{
	if (size != STATE_SIZE)
		return 0;

	return !!state_save((unsigned char*) dest);
}

GPGX_EX int gpgx_state_load(void *src, int size)
{
	if (size != STATE_SIZE)
		return 0;

	return !!state_load((unsigned char *) src);
}

void osd_input_update(void)
{
}

int (*load_archive_cb)(const char *filename, unsigned char *buffer, int maxsize);

// return 0 on failure, else actual loaded size
// extension, if not null, should be populated with the extension of the file loaded
// (up to 3 chars and null terminator, no more)
int load_archive(char *filename, unsigned char *buffer, int maxsize, char *extension)
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


GPGX_EX int gpgx_init(const char *feromextension, int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize), int sixbutton, char system_a, char system_b)
{
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
	config.filter= 0; /* no filter */
	config.lp_range = 0x9999; /* 0.6 in 16.16 fixed point */
	config.low_freq = 880;
	config.high_freq= 5000;
	config.lg = 1.0;
	config.mg = 1.0;
	config.hg = 1.0;
	config.dac_bits 	  = 14; /* MAX DEPTH */ 
	config.ym2413= 2; /* AUTO */
	config.mono  = 0; /* STEREO output */

	/* system options */
	config.system= 0; /* AUTO */
	config.region_detect  = 0; /* AUTO */
	config.vdp_mode = 0; /* AUTO */
	config.master_clock= 0; /* AUTO */
	config.force_dtack = 0;
	config.addr_error  = 1;
	config.bios  = 0;
	config.lock_on  = 0;

	/* video options */
	config.overscan = 0;
	config.gg_extra = 0;
	config.ntsc  = 0;
	config.render= 0;

	// set overall input system type
	// usual is MD GAMEPAD or NONE
	// TEAMPLAYER, WAYPLAY, ACTIVATOR, XEA1P, MOUSE need to be specified
	// everything else is auto or master system only
	// XEA1P is port 1 only
	// WAYPLAY is both ports at same time only
	input.system[0] = system_a;
	input.system[1] = system_b;

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

	return 1;
}


