#include "mednafen-types.h"
#include "mednafen.h"
#include "git.h"
#include "general.h"
#include "libretro.h"

static MDFNGI *game;
static retro_video_refresh_t video_cb;
static retro_audio_sample_t audio_cb;
static retro_audio_sample_batch_t audio_batch_cb;
static retro_environment_t environ_cb;
static retro_input_poll_t input_poll_cb;
static retro_input_state_t input_state_cb;

static MDFN_Surface *surf;

static bool failed_init;

std::string retro_base_directory;
std::string retro_base_name;

#if defined(WANT_PSX_EMU)
#define MEDNAFEN_CORE_NAME_MODULE "psx"
#define MEDNAFEN_CORE_NAME "Mednafen PSX"
#define MEDNAFEN_CORE_VERSION "v0.9.26"
#define MEDNAFEN_CORE_EXTENSIONS "cue|CUE|toc|TOC"
#define MEDNAFEN_CORE_TIMING_FPS 59.85398
#define MEDNAFEN_CORE_GEOMETRY_BASE_W 320
#define MEDNAFEN_CORE_GEOMETRY_BASE_H 240
#define MEDNAFEN_CORE_GEOMETRY_MAX_W 640
#define MEDNAFEN_CORE_GEOMETRY_MAX_H 480
#define MEDNAFEN_CORE_GEOMETRY_ASPECT_RATIO (4.0 / 3.0)
#define FB_WIDTH 680
#define FB_HEIGHT 576

#elif defined(WANT_PCE_FAST_EMU)
#define MEDNAFEN_CORE_NAME_MODULE "pce_fast"
#define MEDNAFEN_CORE_NAME "Mednafen PCE Fast"
#define MEDNAFEN_CORE_VERSION "v0.9.24"
#define MEDNAFEN_CORE_EXTENSIONS "pce|PCE|cue|CUE|zip|ZIP"
#define MEDNAFEN_CORE_TIMING_FPS 59.82
#define MEDNAFEN_CORE_GEOMETRY_BASE_W (game->nominal_width)
#define MEDNAFEN_CORE_GEOMETRY_BASE_H (game->nominal_height)
#define MEDNAFEN_CORE_GEOMETRY_MAX_W 512
#define MEDNAFEN_CORE_GEOMETRY_MAX_H 242
#define MEDNAFEN_CORE_GEOMETRY_ASPECT_RATIO (4.0 / 3.0)
#define FB_WIDTH 512
#define FB_HEIGHT 242

#elif defined(WANT_WSWAN_EMU)
#define MEDNAFEN_CORE_NAME_MODULE "wswan"
#define MEDNAFEN_CORE_NAME "Mednafen WonderSwan"
#define MEDNAFEN_CORE_VERSION "v0.9.22"
#define MEDNAFEN_CORE_EXTENSIONS "ws|WS|wsc|WSC|zip|ZIP"
#define MEDNAFEN_CORE_TIMING_FPS 75.47
#define MEDNAFEN_CORE_GEOMETRY_BASE_W (game->nominal_width)
#define MEDNAFEN_CORE_GEOMETRY_BASE_H (game->nominal_height)
#define MEDNAFEN_CORE_GEOMETRY_MAX_W 224
#define MEDNAFEN_CORE_GEOMETRY_MAX_H 144
#define MEDNAFEN_CORE_GEOMETRY_ASPECT_RATIO (4.0 / 3.0)
#define FB_WIDTH 224
#define FB_HEIGHT 144

#endif

#ifdef WANT_16BPP
static uint16_t mednafen_buf[FB_WIDTH * FB_HEIGHT];
#else
static uint32_t mednafen_buf[FB_WIDTH * FB_HEIGHT];
#endif
const char *mednafen_core_str = MEDNAFEN_CORE_NAME;

static void check_system_specs(void)
{
   unsigned level = 0;
#if defined(WANT_PSX_EMU)
   // Hints that we need a fairly powerful system to run this.
   level = 3;
#endif
   environ_cb(RETRO_ENVIRONMENT_SET_PERFORMANCE_LEVEL, &level);
}

void retro_init()
{
   std::vector<MDFNGI*> ext;
   MDFNI_InitializeModules(ext);

   const char *dir = NULL;

   std::vector<MDFNSetting> settings;

   if (environ_cb(RETRO_ENVIRONMENT_GET_SYSTEM_DIRECTORY, &dir) && dir)
   {
      retro_base_directory = dir;
      // Make sure that we don't have any lingering slashes, etc, as they break Windows.
      size_t last = retro_base_directory.find_last_not_of("/\\");
      if (last != std::string::npos)
         last++;

      retro_base_directory = retro_base_directory.substr(0, last);

      MDFNI_SetBaseDirectory(retro_base_directory.c_str());
   }
   else
   {
	/* TODO: Add proper fallback */
      fprintf(stderr, "System directory is not defined. Fallback on using same dir as ROM for system directory later ...\n");
      failed_init = true;
   }

#if defined(WANT_16BPP) && defined(FRONTEND_SUPPORTS_RGB565)
   enum retro_pixel_format rgb565 = RETRO_PIXEL_FORMAT_RGB565;
   if(environ_cb(RETRO_ENVIRONMENT_SET_PIXEL_FORMAT, &rgb565))
      fprintf(stderr, "Frontend supports RGB565 - will use that instead of XRGB1555.\n");
#endif

   check_system_specs();
}

void retro_reset()
{
   MDFNI_Reset();
}

bool retro_load_game_special(unsigned, const struct retro_game_info *, size_t)
{
   return false;
}

bool retro_load_game(const struct retro_game_info *info)
{
   if (failed_init)
      return false;

#ifdef WANT_32BPP
   enum retro_pixel_format fmt = RETRO_PIXEL_FORMAT_XRGB8888;
   if (!environ_cb(RETRO_ENVIRONMENT_SET_PIXEL_FORMAT, &fmt))
   {
      fprintf(stderr, "Pixel format XRGB8888 not supported by platform, cannot use %s.\n", MEDNAFEN_CORE_NAME);
      return false;
   }
#endif

   const char *base = strrchr(info->path, '/');
   if (!base)
      base = strrchr(info->path, '\\');

   if (base)
      retro_base_name = base + 1;
   else
      retro_base_name = info->path;

   retro_base_name = retro_base_name.substr(0, retro_base_name.find_last_of('.'));
 
   game = MDFNI_LoadGame(MEDNAFEN_CORE_NAME_MODULE, info->path);
   if (!game)
      return false;

   MDFN_PixelFormat pix_fmt(MDFN_COLORSPACE_RGB, 16, 8, 0, 24);

   surf = new MDFN_Surface(mednafen_buf, FB_WIDTH, FB_HEIGHT, FB_WIDTH, pix_fmt);

   return game;
}

void retro_unload_game()
{
   MDFNI_CloseGame();
}

static unsigned retro_devices[2];

// Hardcoded for PSX. No reason to parse lots of structures ...
// See mednafen/psx/input/gamepad.cpp
static void update_input(void)
{
#if defined(WANT_PSX_EMU)
   union
   {
      uint32_t u32[2][1 + 8];
      uint8_t u8[2][2 * sizeof(uint16_t) + 8 * sizeof(uint32_t)];
   } static buf;

   uint16_t input_buf[2] = {0};
   static unsigned map[] = {
      RETRO_DEVICE_ID_JOYPAD_SELECT,
      RETRO_DEVICE_ID_JOYPAD_L3,
      RETRO_DEVICE_ID_JOYPAD_R3,
      RETRO_DEVICE_ID_JOYPAD_START,
      RETRO_DEVICE_ID_JOYPAD_UP,
      RETRO_DEVICE_ID_JOYPAD_RIGHT,
      RETRO_DEVICE_ID_JOYPAD_DOWN,
      RETRO_DEVICE_ID_JOYPAD_LEFT,
      RETRO_DEVICE_ID_JOYPAD_L2,
      RETRO_DEVICE_ID_JOYPAD_R2,
      RETRO_DEVICE_ID_JOYPAD_L,
      RETRO_DEVICE_ID_JOYPAD_R,
      RETRO_DEVICE_ID_JOYPAD_X,
      RETRO_DEVICE_ID_JOYPAD_A,
      RETRO_DEVICE_ID_JOYPAD_B,
      RETRO_DEVICE_ID_JOYPAD_Y,
   };

   for (unsigned j = 0; j < 2; j++)
   {
      for (unsigned i = 0; i < 16; i++)
         input_buf[j] |= input_state_cb(j, RETRO_DEVICE_JOYPAD, 0, map[i]) ? (1 << i) : 0;
   }

   // Buttons.
   buf.u8[0][0] = (input_buf[0] >> 0) & 0xff;
   buf.u8[0][1] = (input_buf[0] >> 8) & 0xff;
   buf.u8[1][0] = (input_buf[1] >> 0) & 0xff;
   buf.u8[1][1] = (input_buf[1] >> 8) & 0xff;

   // Analogs
   for (unsigned j = 0; j < 2; j++)
   {
      int analog_left_x = input_state_cb(j, RETRO_DEVICE_ANALOG, RETRO_DEVICE_INDEX_ANALOG_LEFT,
            RETRO_DEVICE_ID_ANALOG_X);

      int analog_left_y = input_state_cb(j, RETRO_DEVICE_ANALOG, RETRO_DEVICE_INDEX_ANALOG_LEFT,
            RETRO_DEVICE_ID_ANALOG_Y);

      int analog_right_x = input_state_cb(j, RETRO_DEVICE_ANALOG, RETRO_DEVICE_INDEX_ANALOG_RIGHT,
            RETRO_DEVICE_ID_ANALOG_X);

      int analog_right_y = input_state_cb(j, RETRO_DEVICE_ANALOG, RETRO_DEVICE_INDEX_ANALOG_RIGHT,
            RETRO_DEVICE_ID_ANALOG_Y);

      uint32_t r_right = analog_right_x > 0 ?  analog_right_x : 0;
      uint32_t r_left  = analog_right_x < 0 ? -analog_right_x : 0;
      uint32_t r_down  = analog_right_y > 0 ?  analog_right_y : 0;
      uint32_t r_up    = analog_right_y < 0 ? -analog_right_y : 0;

      uint32_t l_right = analog_left_x > 0 ?  analog_left_x : 0;
      uint32_t l_left  = analog_left_x < 0 ? -analog_left_x : 0;
      uint32_t l_down  = analog_left_y > 0 ?  analog_left_y : 0;
      uint32_t l_up    = analog_left_y < 0 ? -analog_left_y : 0;

      buf.u32[j][1] = r_right;
      buf.u32[j][2] = r_left;
      buf.u32[j][3] = r_down;
      buf.u32[j][4] = r_up;

      buf.u32[j][5] = l_right;
      buf.u32[j][6] = l_left;
      buf.u32[j][7] = l_down;
      buf.u32[j][8] = l_up;
   }

   for (int j = 0; j < 2; j++)
   {
      switch (retro_devices[j])
      {
         case RETRO_DEVICE_ANALOG:
            game->SetInput(j, "dualanalog", &buf.u8[j]);
            break;
         default:
            game->SetInput(j, "gamepad", &buf.u8[j]);
            break;
      }
   }
#elif defined(WANT_PCE_FAST_EMU)
   static uint8_t input_buf[5][2];

   static unsigned map[] = {
      RETRO_DEVICE_ID_JOYPAD_Y,
      RETRO_DEVICE_ID_JOYPAD_B,
      RETRO_DEVICE_ID_JOYPAD_SELECT,
      RETRO_DEVICE_ID_JOYPAD_START,
      RETRO_DEVICE_ID_JOYPAD_UP,
      RETRO_DEVICE_ID_JOYPAD_RIGHT,
      RETRO_DEVICE_ID_JOYPAD_DOWN,
      RETRO_DEVICE_ID_JOYPAD_LEFT,
      RETRO_DEVICE_ID_JOYPAD_A,
      RETRO_DEVICE_ID_JOYPAD_X,
      RETRO_DEVICE_ID_JOYPAD_L,
      RETRO_DEVICE_ID_JOYPAD_R,
      RETRO_DEVICE_ID_JOYPAD_L2
   };

   if (input_state_cb)
   {
      for (unsigned j = 0; j < 5; j++)
      {
         uint16_t input_state = 0;
         for (unsigned i = 0; i < 13; i++)
            input_state |= input_state_cb(j, RETRO_DEVICE_JOYPAD, 0, map[i]) ? (1 << i) : 0;

         // Input data must be little endian.
         input_buf[j][0] = (input_state >> 0) & 0xff;
         input_buf[j][1] = (input_state >> 8) & 0xff;
      }
   }

   // Possible endian bug ...
   for (unsigned i = 0; i < 5; i++)
      MDFNI_SetInput(i, "gamepad", &input_buf[i][0], 0);
#elif defined(WANT_WSWAN_EMU)
   static uint16_t input_buf;
   input_buf = 0;

   static unsigned map[] = {
      RETRO_DEVICE_ID_JOYPAD_UP, //X Cursor horizontal-layout games
      RETRO_DEVICE_ID_JOYPAD_RIGHT, //X Cursor horizontal-layout games
      RETRO_DEVICE_ID_JOYPAD_DOWN, //X Cursor horizontal-layout games
      RETRO_DEVICE_ID_JOYPAD_LEFT, //X Cursor horizontal-layout games
      RETRO_DEVICE_ID_JOYPAD_R2, //Y Cursor UP vertical-layout games
      RETRO_DEVICE_ID_JOYPAD_R, //Y Cursor RIGHT vertical-layout games
      RETRO_DEVICE_ID_JOYPAD_L2, //Y Cursor DOWN vertical-layout games
      RETRO_DEVICE_ID_JOYPAD_L, //Y Cursor LEFT vertical-layout games
      RETRO_DEVICE_ID_JOYPAD_START,
      RETRO_DEVICE_ID_JOYPAD_A,
      RETRO_DEVICE_ID_JOYPAD_B,
   };

for (unsigned i = 0; i < 11; i++)
 input_buf |= map[i] != -1u &&
    input_state_cb(0, RETRO_DEVICE_JOYPAD, 0, map[i]) ? (1 << i) : 0;

#ifdef MSB_FIRST
   union {
      uint8_t b[2];
      uint16_t s;
   } u;
   u.s = input_buf;
   input_buf = u.b[0] | u.b[1] << 8;
#endif

   game->SetInput(0, "gamepad", &input_buf);
#endif
}

static uint64_t video_frames, audio_frames;

void retro_run()
{
   input_poll_cb();

   update_input();

   static int16_t sound_buf[0x10000];
#if defined(WANT_WSWAN_EMU)
   static MDFN_Rect rects[FB_WIDTH];
#else
   static MDFN_Rect rects[FB_HEIGHT];
#endif
#if defined(WANT_PSX_EMU) || defined(WANT_WSWAN_EMU)
   rects[0].w = ~0;
#endif

   EmulateSpecStruct spec = {0};
   spec.surface = surf;
   spec.SoundRate = 44100;
   spec.SoundBuf = sound_buf;
   spec.LineWidths = rects;
   spec.SoundBufMaxSize = sizeof(sound_buf) / 2;
   spec.SoundVolume = 1.0;
   spec.soundmultiplier = 1.0;

   MDFNI_Emulate(&spec);

#if defined(WANT_PSX_EMU)
   unsigned width = rects[0].w;
   unsigned height = spec.DisplayRect.h;
   unsigned int ptrDiff = 0;
#elif defined(WANT_WSWAN_EMU)
   unsigned width = FB_WIDTH;
   unsigned height = FB_HEIGHT;
#else
   unsigned width = rects->w;
   unsigned height = rects->h;
#endif

#ifdef WANT_PSX_EMU
   // This is for PAL, the core implements PAL over NTSC TV so you get the
   // infamous PAL borders. This removes them. The PS1 supports only two horizontal
   // resolutions so it's OK to use constants and not percentage.
   bool isPal = false;
   if (height == FB_HEIGHT)
   {
       ptrDiff += width * 47;
       height = 480;
       isPal = true;
   }
   else if (height == 288)
   {
       // TODO: This seems to be OK as is, but I might be wrong.
       isPal = true;
   }

   if (isPal && width == FB_WIDTH)
       ptrDiff += 7;

   // The core handles vertical overscan for NTSC pretty well, but it ignores
   // horizontal overscan. This is a tough estimation of what the horizontal
   // overscan should be, tested with all major NTSC resolutions. Mayeb make it
   // configurable?
   float hoverscan = 0.941176471;

   width = width * hoverscan;
   ptrDiff += ((rects[0].w - width) / 2);

   const uint32_t *ptr = surf->pixels;
   ptr += ptrDiff;

   video_cb(ptr, width, height, FB_WIDTH << 2);
#elif defined(WANT_16BPP)
   const uint16_t *pix = surf->pixels16;
   video_cb(pix, width, height, FB_WIDTH << 1);
#endif

   video_frames++;
   audio_frames += spec.SoundBufSize;

   audio_batch_cb(spec.SoundBuf, spec.SoundBufSize);
}

void retro_get_system_info(struct retro_system_info *info)
{
   memset(info, 0, sizeof(*info));
   info->library_name     = MEDNAFEN_CORE_NAME;
   info->library_version  = MEDNAFEN_CORE_VERSION;
   info->need_fullpath    = true;
   info->valid_extensions = MEDNAFEN_CORE_EXTENSIONS;
   info->block_extract    = false;
}

void retro_get_system_av_info(struct retro_system_av_info *info)
{
   memset(info, 0, sizeof(*info));
   info->timing.fps            = MEDNAFEN_CORE_TIMING_FPS; // Determined from empirical testing.
   info->timing.sample_rate    = 44100;
   info->geometry.base_width   = MEDNAFEN_CORE_GEOMETRY_BASE_W;
   info->geometry.base_height  = MEDNAFEN_CORE_GEOMETRY_BASE_H;
   info->geometry.max_width    = MEDNAFEN_CORE_GEOMETRY_MAX_W;
   info->geometry.max_height   = MEDNAFEN_CORE_GEOMETRY_MAX_H;
   info->geometry.aspect_ratio = MEDNAFEN_CORE_GEOMETRY_ASPECT_RATIO;
}

void retro_deinit()
{
   delete surf;
   surf = NULL;

   fprintf(stderr, "[%s]: Samples / Frame: %.5f\n",
         mednafen_core_str, (double)audio_frames / video_frames);

   fprintf(stderr, "[%s]: Estimated FPS: %.5f\n",
         mednafen_core_str, (double)video_frames * 44100 / audio_frames);
}

unsigned retro_get_region(void)
{
   return RETRO_REGION_NTSC;
}

unsigned retro_api_version(void)
{
   return RETRO_API_VERSION;
}

void retro_set_controller_port_device(unsigned in_port, unsigned device)
{
#ifdef WANT_PSX_EMU
   if (in_port > 1)
   {
      fprintf(stderr,
            "[%s]: Only the 2 main ports are supported at the moment", mednafen_core_str);
      return;
   }

   switch (device)
   {
      // TODO: Add support for other input types
      case RETRO_DEVICE_JOYPAD:
      case RETRO_DEVICE_ANALOG:
         fprintf(stderr, "[%s]: Selected controller type %u", mednafen_core_str, device);
         retro_devices[in_port] = device;
         break;
      default:
         retro_devices[in_port] = RETRO_DEVICE_JOYPAD;
         fprintf(stderr,
               "[%s]: Unsupported controller device, falling back to gamepad", mednafen_core_str);
   }
#endif
}

void retro_set_environment(retro_environment_t cb)
{
   environ_cb = cb;
}

void retro_set_audio_sample(retro_audio_sample_t cb)
{
   audio_cb = cb;
}

void retro_set_audio_sample_batch(retro_audio_sample_batch_t cb)
{
   audio_batch_cb = cb;
}

void retro_set_input_poll(retro_input_poll_t cb)
{
   input_poll_cb = cb;
}

void retro_set_input_state(retro_input_state_t cb)
{
   input_state_cb = cb;
}

void retro_set_video_refresh(retro_video_refresh_t cb)
{
   video_cb = cb;
}

static size_t serialize_size;

size_t retro_serialize_size(void)
{
   //if (serialize_size)
   //   return serialize_size;

   if (!game->StateAction)
   {
      fprintf(stderr, "[mednafen]: Module %s doesn't support save states.\n", game->shortname);
      return 0;
   }

   StateMem st;
   memset(&st, 0, sizeof(st));

   if (!MDFNSS_SaveSM(&st, 0, 0, NULL, NULL, NULL))
   {
      fprintf(stderr, "[mednafen]: Module %s doesn't support save states.\n", game->shortname);
      return 0;
   }

   free(st.data);
   return serialize_size = st.len;
}

bool retro_serialize(void *data, size_t size)
{
   StateMem st;
   memset(&st, 0, sizeof(st));
   st.data     = (uint8_t*)data;
   st.malloced = size;

   return MDFNSS_SaveSM(&st, 0, 0, NULL, NULL, NULL);
}

bool retro_unserialize(const void *data, size_t size)
{
   StateMem st;
   memset(&st, 0, sizeof(st));
   st.data = (uint8_t*)data;
   st.len  = size;

   return MDFNSS_LoadSM(&st, 0, 0);
}

void *retro_get_memory_data(unsigned)
{
   return NULL;
}

size_t retro_get_memory_size(unsigned)
{
   return 0;
}

void retro_cheat_reset(void)
{}

void retro_cheat_set(unsigned, bool, const char *)
{}

