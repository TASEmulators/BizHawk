#ifndef _MSC_VER
#include <stdbool.h>
#endif
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

#ifdef _MSC_VER
#define snprintf _snprintf
#endif

#ifdef _XBOX1
#include <xtl.h>
#endif

#include "shared.h"
#include "libretro.h"
#include "state.h"
#include "genesis.h"
#include "md_ntsc.h"
#include "sms_ntsc.h"

sms_ntsc_t *sms_ntsc;
md_ntsc_t  *md_ntsc;

char GG_ROM[256];
char AR_ROM[256];
char SK_ROM[256];
char SK_UPMEM[256];
char GG_BIOS[256];
char MS_BIOS_EU[256];
char MS_BIOS_JP[256];
char MS_BIOS_US[256];
char CD_BIOS_EU[256];
char CD_BIOS_US[256];
char CD_BIOS_JP[256];
char CD_BRAM_JP[256];
char CD_BRAM_US[256];
char CD_BRAM_EU[256];
char CART_BRAM[256];

static int vwidth;
static int vheight;

static uint32_t brm_crc[2];
static uint8_t brm_format[0x40] =
{
  0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x00,0x00,0x00,0x00,0x40,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x53,0x45,0x47,0x41,0x5f,0x43,0x44,0x5f,0x52,0x4f,0x4d,0x00,0x01,0x00,0x00,0x00,
  0x52,0x41,0x4d,0x5f,0x43,0x41,0x52,0x54,0x52,0x49,0x44,0x47,0x45,0x5f,0x5f,0x5f
};

static uint8_t temp[0x10000];
static int16 soundbuffer[3068];
static uint16_t bitmap_data_[1024 * 512];
static const double pal_fps = 53203424.0 / (3420.0 * 313.0);
static const double ntsc_fps = 53693175.0 / (3420.0 * 262.0);

static char g_rom_dir[1024];

static retro_video_refresh_t video_cb;
static retro_input_poll_t input_poll_cb;
static retro_input_state_t input_state_cb;
static retro_environment_t environ_cb;
static retro_audio_sample_batch_t audio_cb;

/************************************
 * Genesis Plus GX implementation
 ************************************/
#define CHUNKSIZE   (0x10000)

void error(char * msg, ...)
{
#ifndef _XBOX1
   va_list ap;
   va_start(ap, msg);
   vfprintf(stderr, msg, ap);
   va_end(ap);
#endif
}

int load_archive(char *filename, unsigned char *buffer, int maxsize, char *extension)
{
  int size, left;

  /* Open file */
  FILE *fd = fopen(filename, "rb");

  if (!fd)
  {
    /* Master System & Game Gear BIOS are optional files */
    if (!strcmp(filename,MS_BIOS_US) || !strcmp(filename,MS_BIOS_EU) || !strcmp(filename,MS_BIOS_JP) || !strcmp(filename,GG_BIOS))
    {
      return 0;
    }
  
    /* Mega CD BIOS are required files */
    if (!strcmp(filename,CD_BIOS_US) || !strcmp(filename,CD_BIOS_EU) || !strcmp(filename,CD_BIOS_JP)) 
    {
      fprintf(stderr, "ERROR - Unable to open CD BIOS: %s.\n", filename);
      return 0;
   }

    fprintf(stderr, "ERROR - Unable to open file.\n");
    return 0;
  }

  /* Get file size */
  fseek(fd, 0, SEEK_END);
  size = ftell(fd);

  /* size limit */
  if(size > maxsize)
  {
    fclose(fd);
    fprintf(stderr, "ERROR - File is too large.\n");
    return 0;
  }

  fprintf(stderr, "INFORMATION - Loading %d bytes ...\n", size);

  /* filename extension */
  if (extension)
  {
    memcpy(extension, &filename[strlen(filename) - 3], 3);
    extension[3] = 0;
  }

  /* Read into buffer */
  left = size;
  fseek(fd, 0, SEEK_SET);
  while (left > CHUNKSIZE)
  {
    fread(buffer, CHUNKSIZE, 1, fd);
    buffer += CHUNKSIZE;
    left -= CHUNKSIZE;
  }

  /* Read remaining bytes */
  fread(buffer, left, 1, fd);

  /* Close file */
  fclose(fd);

  /* Return loaded ROM size */
  return size;
}

void osd_input_update(void)
{
  int i, padnum = 0;
  unsigned int temp;

  input_poll_cb();

  for(i = 0; i < MAX_INPUTS; i++)
  {
    temp = 0;
    switch (input.dev[i])
    {
      case DEVICE_PAD6B:
      {
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_L))
          temp |= INPUT_X;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_X))
          temp |= INPUT_Y;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_R))
          temp |= INPUT_Z;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_SELECT))
          temp |= INPUT_MODE;
      }

      case DEVICE_PAD3B:
      {
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_Y))
          temp |= INPUT_A;
      }

      case DEVICE_PAD2B:
      {
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_B))
          temp |= INPUT_B;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_A))
          temp |= INPUT_C;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_START))
          temp |= INPUT_START;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_UP))
          temp |= INPUT_UP;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_DOWN))
          temp |= INPUT_DOWN;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_LEFT))
          temp |= INPUT_LEFT;
        if (input_state_cb(padnum, RETRO_DEVICE_JOYPAD, 0, RETRO_DEVICE_ID_JOYPAD_RIGHT))
          temp |= INPUT_RIGHT;

        padnum++;
        break;
      }

      default:
        break;
    }

    input.pad[i] = temp;
  }
}

static void init_bitmap(void)
{
   memset(&bitmap, 0, sizeof(bitmap));
   bitmap.width      = 1024;
   bitmap.height     = 512;
   bitmap.pitch      = 1024 * 2;
   bitmap.data       = (uint8_t *)bitmap_data_;
   bitmap.viewport.w = 0;
   bitmap.viewport.h = 0;
   bitmap.viewport.x = 0;
   bitmap.viewport.y = 0;
}

static void config_default(void)
{
   int i;
   
   /* sound options */
   config.psg_preamp     = 150;
   config.fm_preamp      = 100;
   config.hq_fm          = 1; /* high-quality resampling */
   config.psgBoostNoise  = 1;
   config.filter         = 0; /* no filter */
   config.lp_range       = 0x9999; /* 0.6 in 16.16 fixed point */
   config.low_freq       = 880;
   config.high_freq      = 5000;
   config.lg             = 1.0;
   config.mg             = 1.0;
   config.hg             = 1.0;
   config.dac_bits 	     = 14; /* MAX DEPTH */ 
   config.ym2413         = 2; /* AUTO */
   config.mono           = 0; /* STEREO output */

   /* system options */
   config.system         = 0; /* AUTO */
   config.region_detect  = 0; /* AUTO */
   config.vdp_mode       = 0; /* AUTO */
   config.master_clock   = 0; /* AUTO */
   config.force_dtack    = 0;
   config.addr_error     = 1;
   config.bios           = 0;
   config.lock_on        = 0;

   /* video options */
   config.overscan = 0;
   config.gg_extra = 0;
   config.ntsc     = 0;
   config.render   = 0;
}

static void bram_load(void)
{
    FILE *fp;

    /* automatically load internal backup RAM */
    switch (region_code)
    {
       case REGION_JAPAN_NTSC:
          fp = fopen(CD_BRAM_JP, "rb");
          break;
       case REGION_EUROPE:
          fp = fopen(CD_BRAM_EU, "rb");
          break;
       case REGION_USA:
          fp = fopen(CD_BRAM_US, "rb");
          break;
       default:
          return;
    }

    if (fp != NULL)
    {
      fread(scd.bram, 0x2000, 1, fp);
      fclose(fp);

      /* update CRC */
      brm_crc[0] = crc32(0, scd.bram, 0x2000);
    }
    else
    {
      /* force internal backup RAM format (does not use previous region backup RAM) */
      scd.bram[0x1fff] = 0;
    }

    /* check if internal backup RAM is correctly formatted */
    if (memcmp(scd.bram + 0x2000 - 0x20, brm_format + 0x20, 0x20))
    {
      /* clear internal backup RAM */
      memset(scd.bram, 0x00, 0x2000 - 0x40);

      /* internal Backup RAM size fields */
      brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = 0x00;
      brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (sizeof(scd.bram) / 64) - 3;

      /* format internal backup RAM */
      memcpy(scd.bram + 0x2000 - 0x40, brm_format, 0x40);

      /* clear CRC to force file saving (in case previous region backup RAM was also formatted) */
      brm_crc[0] = 0;
    }

    /* automatically load cartridge backup RAM (if enabled) */
    if (scd.cartridge.id)
    {
      fp = fopen(CART_BRAM, "rb");
      if (fp != NULL)
      {
        int filesize = scd.cartridge.mask + 1;
        int done = 0;
        
        /* Read into buffer (2k blocks) */
        while (filesize > CHUNKSIZE)
        {
          fread(scd.cartridge.area + done, CHUNKSIZE, 1, fp);
          done += CHUNKSIZE;
          filesize -= CHUNKSIZE;
        }

        /* Read remaining bytes */
        if (filesize)
        {
          fread(scd.cartridge.area + done, filesize, 1, fp);
        }

        /* close file */
        fclose(fp);

        /* update CRC */
        brm_crc[1] = crc32(0, scd.cartridge.area, scd.cartridge.mask + 1);
      }

      /* check if cartridge backup RAM is correctly formatted */
      if (memcmp(scd.cartridge.area + scd.cartridge.mask + 1 - 0x20, brm_format + 0x20, 0x20))
      {
        /* clear cartridge backup RAM */
        memset(scd.cartridge.area, 0x00, scd.cartridge.mask + 1);

        /* Cartridge Backup RAM size fields */
        brm_format[0x10] = brm_format[0x12] = brm_format[0x14] = brm_format[0x16] = (((scd.cartridge.mask + 1) / 64) - 3) >> 8;
        brm_format[0x11] = brm_format[0x13] = brm_format[0x15] = brm_format[0x17] = (((scd.cartridge.mask + 1) / 64) - 3) & 0xff;

        /* format cartridge backup RAM */
        memcpy(scd.cartridge.area + scd.cartridge.mask + 1 - 0x40, brm_format, 0x40);
      }
    }
}

static void bram_save(void)
{
    FILE *fp;

    /* verify that internal backup RAM has been modified */
    if (crc32(0, scd.bram, 0x2000) != brm_crc[0])
    {
      /* check if it is correctly formatted before saving */
      if (!memcmp(scd.bram + 0x2000 - 0x20, brm_format + 0x20, 0x20))
      {
        switch (region_code)
        {
          case REGION_JAPAN_NTSC:
            fp = fopen(CD_BRAM_JP, "wb");
            break;
          case REGION_EUROPE:
            fp = fopen(CD_BRAM_EU, "wb");
            break;
          case REGION_USA:
            fp = fopen(CD_BRAM_US, "wb");
            break;
          default:
            return;
        }

        if (fp != NULL)
        {
          fwrite(scd.bram, 0x2000, 1, fp);
          fclose(fp);

          /* update CRC */
          brm_crc[0] = crc32(0, scd.bram, 0x2000);
        }
      }
    }

    /* verify that cartridge backup RAM has been modified */
    if (scd.cartridge.id && (crc32(0, scd.cartridge.area, scd.cartridge.mask + 1) != brm_crc[1]))
    {
      /* check if it is correctly formatted before saving */
      if (!memcmp(scd.cartridge.area + scd.cartridge.mask + 1 - 0x20, brm_format + 0x20, 0x20))
      {
        fp = fopen(CART_BRAM, "wb");
        if (fp != NULL)
        {
          int filesize = scd.cartridge.mask + 1;
          int done = 0;
        
          /* Write to file (2k blocks) */
          while (filesize > CHUNKSIZE)
          {
            fwrite(scd.cartridge.area + done, CHUNKSIZE, 1, fp);
            done += CHUNKSIZE;
            filesize -= CHUNKSIZE;
          }

          /* Write remaining bytes */
          if (filesize)
          {
            fwrite(scd.cartridge.area + done, filesize, 1, fp);
          }

          /* Close file */
          fclose(fp);

          /* update CRC */
          brm_crc[1] = crc32(0, scd.cartridge.area, scd.cartridge.mask + 1);
        }
      }
    }
}

static void extract_directory(char *buf, const char *path, size_t size)
{
   char *base;
   strncpy(buf, path, size - 1);
   buf[size - 1] = '\0';

   base = strrchr(buf, '/');
   if (!base)
      base = strrchr(buf, '\\');

   if (base)
      *base = '\0';
   else
      buf[0] = '\0';
}

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

static void check_variables(void)
{
  unsigned orig_value;
  bool update_viewports = false;
  bool reinit = false;
  struct retro_variable var = {0};

  var.key = "system_hw";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.system;
    if (!strcmp(var.value, "sg-1000"))
      config.system = SYSTEM_SG;
    else if (!strcmp(var.value, "mark-III"))
      config.system = SYSTEM_MARKIII;
    else if (!strcmp(var.value, "master system"))
      config.system = SYSTEM_SMS;
    else if (!strcmp(var.value, "master system II"))
      config.system = SYSTEM_SMS2;
    else if (!strcmp(var.value, "game gear"))
      config.system = SYSTEM_GG;
    else if (!strcmp(var.value, "mega drive / genesis"))
      config.system = SYSTEM_MD;
    else
      config.system = 0;

    if (orig_value != config.system)
    {
      if (system_hw)
      {
        switch (config.system)
        {
          case 0:
            system_hw = romtype; /* AUTO */
            break;

          case SYSTEM_MD:
            system_hw = (romtype & SYSTEM_MD) ? romtype : SYSTEM_PBC;
            break;

          case SYSTEM_GG:
            system_hw = (romtype == SYSTEM_GG) ? SYSTEM_GG : SYSTEM_GGMS;
            break;

          default:
            system_hw = config.system;
            break;
        }

        reinit = true;
      }
    }
  }

  var.key = "region_detect";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.region_detect;
    if (!strcmp(var.value, "ntsc-u"))
      config.region_detect = 1;
    else if (!strcmp(var.value, "pal"))
      config.region_detect = 2;
    else if (!strcmp(var.value, "ntsc-j"))
      config.region_detect = 3;
    else
      config.region_detect = 0;

    if (orig_value != config.region_detect)
    {
      if (system_hw)
      {
        get_region(NULL);
        reinit = true;
      }
    }
  }

  var.key = "force_dtack";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    if (!strcmp(var.value, "enabled"))
      config.force_dtack = 1;
    else
      config.force_dtack = 0;
  }

  var.key = "addr_error";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    if (!strcmp(var.value, "enabled"))
      m68k.aerr_enabled = config.addr_error = 1;
    else
      m68k.aerr_enabled = config.addr_error = 0;
  }

  var.key = "lock_on";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.lock_on;
    if (!strcmp(var.value, "game genie"))
      config.lock_on = TYPE_GG;
    else if (!strcmp(var.value, "action replay (pro)"))
      config.lock_on = TYPE_AR;
    else if (!strcmp(var.value, "sonic & knuckles"))
      config.lock_on = TYPE_SK;
    else
      config.lock_on = 0;

    if (orig_value != config.lock_on)
    {
      if (system_hw == SYSTEM_MD)
        reinit = true;
    }
  }

  var.key = "ym2413";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.ym2413;
    if (!strcmp(var.value, "enabled"))
      config.ym2413 = 1;
    else if (!strcmp(var.value, "disabled"))
      config.ym2413 = 0;
    else
      config.ym2413 = 2;

    if (orig_value != config.ym2413)
    {
      if (system_hw && (config.ym2413 & 2) && ((system_hw & SYSTEM_PBC) != SYSTEM_MD))
      {
        memcpy(temp, sram.sram, sizeof(temp));
        sms_cart_init();
        memcpy(sram.sram, temp, sizeof(temp));
      }
    }
  }

  var.key = "dac_bits";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    if (!strcmp(var.value, "enabled"))
      config.dac_bits = 9;
    else
      config.dac_bits = 14;

    YM2612Config(config.dac_bits);
  }

  var.key = "blargg_ntsc_filter";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.ntsc;

    if (strcmp(var.value, "disabled") == 0)
      config.ntsc = 0;
    else if (strcmp(var.value, "monochrome") == 0)
    {
      config.ntsc = 1;
      sms_ntsc_init(sms_ntsc, &sms_ntsc_monochrome);
      md_ntsc_init(md_ntsc,   &md_ntsc_monochrome);
    }
    else if (strcmp(var.value, "composite") == 0)
    {
      config.ntsc = 1;
      sms_ntsc_init(sms_ntsc, &sms_ntsc_composite);
      md_ntsc_init(md_ntsc,   &md_ntsc_composite);
    }
    else if (strcmp(var.value, "svideo") == 0)
    {
      config.ntsc = 1;
      sms_ntsc_init(sms_ntsc, &sms_ntsc_svideo);
      md_ntsc_init(md_ntsc,   &md_ntsc_svideo);
    }
    else if (strcmp(var.value, "rgb") == 0)
    {
      config.ntsc = 1;
      sms_ntsc_init(sms_ntsc, &sms_ntsc_rgb);
      md_ntsc_init(md_ntsc,   &md_ntsc_rgb);
    }

    if (orig_value != config.ntsc)
      update_viewports = true;
  }

  var.key = "overscan";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.overscan;
    if (strcmp(var.value, "disabled") == 0)
      config.overscan = 0;
    else if (strcmp(var.value, "top/bottom") == 0)
      config.overscan = 1;
    else if (strcmp(var.value, "left/right") == 0)
      config.overscan = 2;
    else if (strcmp(var.value, "full") == 0)
      config.overscan = 3;
    if (orig_value != config.overscan)
      update_viewports = true;
  }

  var.key = "gg_extra";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.gg_extra;
    if (strcmp(var.value, "disabled") == 0)
      config.gg_extra = 0;
    else if (strcmp(var.value, "enabled") == 0)
      config.gg_extra = 1;
    if (orig_value != config.gg_extra)
      update_viewports = true;
  }

  var.key = "render";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  {
    orig_value = config.render;
    if (strcmp(var.value, "normal") == 0)
      config.render = 0;
    else
      config.render = 1;
    if (orig_value != config.render)
      update_viewports = true;
  }

  if (reinit)
  {
    audio_init(44100, snd.frame_rate);
    memcpy(temp, sram.sram, sizeof(temp));
    system_init();
    system_reset();
    memcpy(sram.sram, temp, sizeof(temp));
  }

  if (update_viewports)
  {
    bitmap.viewport.changed = 3;
    if ((system_hw == SYSTEM_GG) && !config.gg_extra)
      bitmap.viewport.x = (config.overscan & 2) ? 14 : -48;
    else
      bitmap.viewport.x = (config.overscan & 2) * 7;
  }
}

static void configure_controls(void)
{
  int i;
  struct retro_variable var;

  input.system[0] = SYSTEM_MD_GAMEPAD;
  input.system[1] = SYSTEM_MD_GAMEPAD;

  var.key = "padtype";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  if (!strcmp(var.value, "6-buttons"))
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD6B;
  else if (!strcmp(var.value, "3-buttons"))
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD3B;
  else if (!strcmp(var.value, "2-buttons"))
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD2B;
  else if ((system_hw & SYSTEM_PBC) != SYSTEM_MD)
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD2B;
  else if (rominfo.peripherals & 2)
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD6B;
  else
    for(i = 0; i < MAX_INPUTS; i++)
      config.input[i].padtype = DEVICE_PAD3B;

  var.key = "multitap";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  if (!strcmp(var.value, "4-wayplay"))
    input.system[0] = input.system[1] = SYSTEM_WAYPLAY;
  else if (!strcmp(var.value, "teamplayer 1"))
    input.system[0] = SYSTEM_TEAMPLAYER;
  else if (!strcmp(var.value, "teamplayer 2"))
    input.system[1] = SYSTEM_TEAMPLAYER;
  else if (!strcmp(var.value, "teamplayer 1&2"))
    input.system[0] = input.system[1] = SYSTEM_TEAMPLAYER;

  var.key = "portb";
  environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE, &var);
  if (!strcmp(var.value, "disabled"))
    input.system[1] = NO_SYSTEM;

  input_init();
}

/************************************
 * libretro implementation
 ************************************/
unsigned retro_api_version(void) { return RETRO_API_VERSION; }

void retro_set_environment(retro_environment_t cb)
{
   static const struct retro_variable vars[] = {
      { "system_hw", "System hardware; auto|sg-1000|mark-III|master system|master system II|game gear|mega drive / genesis" },
      { "region_detect", "System region; auto|ntsc-u|pal|ntsc-j" },
      { "force_dtack", "System lockups; enabled|disabled" },
      { "addr_error", "68k address error; enabled|disabled" },
      { "lock_on", "Cartridge lock-on; disabled|game genie|action replay (pro)|sonic & knuckles" },
      { "padtype", "Gamepad type; auto|6-buttons|3-buttons|2-buttons" },
      { "multitap", "Multi Tap; disabled|4-wayplay|teamplayer (port 1)|teamplayer (port 2)|teamplayer (both)" },
      { "portb", "Control Port 2; enabled|disabled" },
      { "ym2413", "Master System FM; auto|disabled|enabled" },
      { "dac_bits", "YM2612 DAC quantization; disabled|enabled" },
      { "blargg_ntsc_filter", "Blargg NTSC filter; disabled|monochrome|composite|svideo|rgb" },
      { "overscan", "Borders; disabled|top/bottom|left/right|full" },
      { "gg_extra", "Game Gear extended screen; disabled|enabled" },
      { "render", "Interlaced mode resolution; normal|double" },
      { NULL, NULL },
   };

   environ_cb = cb;
   cb(RETRO_ENVIRONMENT_SET_VARIABLES, (void*)vars);
}

void retro_set_video_refresh(retro_video_refresh_t cb) { video_cb = cb; }
void retro_set_audio_sample(retro_audio_sample_t cb) { (void)cb; }
void retro_set_audio_sample_batch(retro_audio_sample_batch_t cb) { audio_cb = cb; }
void retro_set_input_poll(retro_input_poll_t cb) { input_poll_cb = cb; }
void retro_set_input_state(retro_input_state_t cb) { input_state_cb = cb; }

void retro_get_system_info(struct retro_system_info *info)
{
   info->library_name = "Genesis Plus GX";
   info->library_version = "v1.7.4";
   info->valid_extensions = "mdx|md|smd|gen|bin|cue|iso|sms|gg|sg";
   info->block_extract = false;
   info->need_fullpath = true;
}

void retro_get_system_av_info(struct retro_system_av_info *info)
{
   info->geometry.base_width    = vwidth;
   info->geometry.base_height   = vheight;
   info->geometry.max_width     = 1024;
   info->geometry.max_height    = 512;
   info->geometry.aspect_ratio  = 4.0 / 3.0;
   info->timing.fps             = snd.frame_rate;
   info->timing.sample_rate     = 44100;
}

void retro_set_controller_port_device(unsigned port, unsigned device)
{
   (void)port;
   (void)device;
}

size_t retro_serialize_size(void) { return STATE_SIZE; }

bool retro_serialize(void *data, size_t size)
{ 
   if (size != STATE_SIZE)
      return FALSE;

   state_save(data);

   return TRUE;
}

bool retro_unserialize(const void *data, size_t size)
{
   if (size != STATE_SIZE)
      return FALSE;

   if (!state_load((uint8_t*)data))
      return FALSE;

   return TRUE;
}

void retro_cheat_reset(void) {}
void retro_cheat_set(unsigned index, bool enabled, const char *code)
{
   (void)index;
   (void)enabled;
   (void)code;
}

bool retro_load_game(const struct retro_game_info *info)
{
   int i;
   const char *dir;
#if defined(_WIN32)
   char slash = '\\';
#else
   char slash = '/';
#endif

   extract_directory(g_rom_dir, info->path, sizeof(g_rom_dir));

   if (!environ_cb(RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY, &dir) || !dir)
   {
      fprintf(stderr, "[genplus]: Defaulting system directory to %s.\n", g_rom_dir);
      dir = g_rom_dir;
   }

   snprintf(GG_ROM, sizeof(GG_ROM), "%s%cggenie.bin", dir, slash);
   snprintf(AR_ROM, sizeof(AR_ROM), "%s%careplay.bin", dir, slash);
   snprintf(SK_ROM, sizeof(SK_ROM), "%s%csk.bin", dir, slash);
   snprintf(SK_UPMEM, sizeof(SK_UPMEM), "%s%cs2k.bin", dir, slash);
   snprintf(CD_BIOS_EU, sizeof(CD_BIOS_EU), "%s%cbios_CD_E.bin", dir, slash);
   snprintf(CD_BIOS_US, sizeof(CD_BIOS_US), "%s%cbios_CD_U.bin", dir, slash);
   snprintf(CD_BIOS_JP, sizeof(CD_BIOS_JP), "%s%cbios_CD_J.bin", dir, slash);
   snprintf(CD_BRAM_EU, sizeof(CD_BRAM_EU), "%s%cscd_E.brm", dir, slash);
   snprintf(CD_BRAM_US, sizeof(CD_BRAM_US), "%s%cscd_U.brm", dir, slash);
   snprintf(CD_BRAM_JP, sizeof(CD_BRAM_JP), "%s%cscd_J.brm", dir, slash);
   snprintf(CART_BRAM, sizeof(CART_BRAM), "%s%ccart.brm", dir, slash);
   fprintf(stderr, "Game Genie ROM should be located at: %s\n", GG_ROM);
   fprintf(stderr, "Action Replay (Pro) ROM should be located at: %s\n", AR_ROM);
   fprintf(stderr, "Sonic & Knuckles (2 MB) ROM should be located at: %s\n", SK_ROM);
   fprintf(stderr, "Sonic & Knuckles UPMEM (256 KB) ROM should be located at: %s\n", SK_UPMEM);
   fprintf(stderr, "Mega CD PAL BIOS should be located at: %s\n", CD_BIOS_EU);
   fprintf(stderr, "Sega CD NTSC-U BIOS should be located at: %s\n", CD_BIOS_US);
   fprintf(stderr, "Mega CD NTSC-J BIOS should be located at: %s\n", CD_BIOS_JP);
   fprintf(stderr, "Mega CD PAL BRAM is located at: %s\n", CD_BRAM_EU);
   fprintf(stderr, "Sega CD NTSC-U BRAM is located at: %s\n", CD_BRAM_US);
   fprintf(stderr, "Mega CD NTSC-J BRAM is located at: %s\n", CD_BRAM_JP);
   fprintf(stderr, "Mega CD RAM CART is located at: %s\n", CART_BRAM);

   check_variables();
   if (!load_rom((char *)info->path))
      return false;

   configure_controls();
     
   audio_init(44100, vdp_pal ? pal_fps : ntsc_fps);
   system_init();
   system_reset();

   if (system_hw == SYSTEM_MCD)
     bram_load();

   update_viewport();

   return true;
}

bool retro_load_game_special(unsigned game_type, const struct retro_game_info *info, size_t num_info)
{
   (void)game_type;
   (void)info;
   (void)num_info;
   return FALSE;
}

void retro_unload_game(void) 
{
   if (system_hw == SYSTEM_MCD)
      bram_save();
}

unsigned retro_get_region(void) { return vdp_pal ? RETRO_REGION_PAL : RETRO_REGION_NTSC; }

void *retro_get_memory_data(unsigned id)
{
   if (!sram.on)
      return NULL;

   switch (id)
   {
      case RETRO_MEMORY_SAVE_RAM:
         return sram.sram;

      default:
         return NULL;
   }
}

size_t retro_get_memory_size(unsigned id)
{
   if (!sram.on)
      return 0;

   switch (id)
   {
      case RETRO_MEMORY_SAVE_RAM:
         return 0x10000;

      default:
         return 0;
   }
}

void retro_init(void)
{
   unsigned level, rgb565;
   sms_ntsc = calloc(1, sizeof(sms_ntsc_t));
   md_ntsc  = calloc(1, sizeof(md_ntsc_t));

   init_bitmap();
   config_default();

   level = 1;
   environ_cb(RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL, &level);

#ifdef FRONTEND_SUPPORTS_RGB565
   rgb565 = RETRO_PIXEL_FORMAT_RGB565;
   if(environ_cb(RETRO_ENVIRONMENT_SET_PIXEL_FORMAT, &rgb565))
      fprintf(stderr, "Frontend supports RGB565 - will use that instead of XRGB1555.\n");
#endif
}

void retro_deinit(void)
{
   audio_shutdown();
   free(md_ntsc);
   free(sms_ntsc);
}

void retro_reset(void) { system_reset(); }

void retro_run(void) 
{
   bool updated = false;

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

   video_cb(bitmap.data, vwidth, vheight, 1024 * 2);
   audio_cb(soundbuffer, audio_update(soundbuffer));

   environ_cb(RETRO_ENVIRONMENT_GET_VARIABLE_UPDATE, &updated);
   if (updated)
   {
      check_variables();
      configure_controls();
   }
}
