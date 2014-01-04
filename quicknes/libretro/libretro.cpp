#include "libretro.h"
#include <stdint.h>
#include <string.h>
#include <math.h>
#include <stdlib.h>
#include <stdio.h>
#include "Nes_Emu.h"
#include "fex/Data_Reader.h"
#include "abstract_file.h"

static Nes_Emu *emu;

void retro_init(void)
{
   delete emu;
   emu = new Nes_Emu;
}

void retro_deinit(void)
{
   delete emu;
   emu = 0;
}

unsigned retro_api_version(void)
{
   return RETRO_API_VERSION;
}

void retro_set_controller_port_device(unsigned, unsigned)
{
}

void retro_get_system_info(struct retro_system_info *info)
{
   memset(info, 0, sizeof(*info));
   info->library_name     = "QuickNES";
   info->library_version  = "v1";
   info->need_fullpath    = false;
   info->valid_extensions = "nes"; // Anything is fine, we don't care.
}

void retro_get_system_av_info(struct retro_system_av_info *info)
{
   const retro_system_timing timing = { Nes_Emu::frame_rate, 44100.0 };
   info->timing = timing;

   const retro_game_geometry geom = {
      Nes_Emu::image_width,
      Nes_Emu::image_height,
      Nes_Emu::image_width,
      Nes_Emu::image_height,
      4.0 / 3.0,
   };
   info->geometry = geom;
}

static retro_video_refresh_t video_cb;
static retro_audio_sample_t audio_cb;
static retro_audio_sample_batch_t audio_batch_cb;
static retro_environment_t environ_cb;
static retro_input_poll_t input_poll_cb;
static retro_input_state_t input_state_cb;

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

void retro_reset(void)
{
   if (emu)
      emu->reset();
}

#define JOY_A           1
#define JOY_B           2
#define JOY_SELECT      4
#define JOY_START       8
#define JOY_UP       0x10
#define JOY_DOWN     0x20
#define JOY_LEFT     0x40
#define JOY_RIGHT    0x80

typedef struct
{
   unsigned retro;
   unsigned nes;
} keymap;

static const keymap bindmap[] = {
   { RETRO_DEVICE_ID_JOYPAD_A, JOY_A },
   { RETRO_DEVICE_ID_JOYPAD_B, JOY_B },
   { RETRO_DEVICE_ID_JOYPAD_SELECT, JOY_SELECT },
   { RETRO_DEVICE_ID_JOYPAD_START, JOY_START },
   { RETRO_DEVICE_ID_JOYPAD_UP, JOY_UP },
   { RETRO_DEVICE_ID_JOYPAD_DOWN, JOY_DOWN },
   { RETRO_DEVICE_ID_JOYPAD_LEFT, JOY_LEFT },
   { RETRO_DEVICE_ID_JOYPAD_RIGHT, JOY_RIGHT },
};

static void update_input(int pads[2])
{
   pads[0] = pads[1] = 0;
   input_poll_cb();

   for (unsigned p = 0; p < 2; p++)
      for (unsigned bind = 0; bind < sizeof(bindmap) / sizeof(bindmap[0]); bind++)
         pads[p] |= input_state_cb(p, RETRO_DEVICE_JOYPAD, 0, bindmap[bind].retro) ? bindmap[bind].nes : 0;
}

void retro_run(void)
{
   int pads[2] = {0};
   update_input(pads);

   emu->emulate_frame(pads[0], pads[1]);
   const Nes_Emu::frame_t &frame = emu->frame();

   static uint32_t video_buffer[Nes_Emu::image_width * Nes_Emu::image_height];

   const uint8_t *in_pixels = frame.pixels;
   uint32_t *out_pixels = video_buffer;

   for (unsigned h = 0; h < Nes_Emu::image_height;
         h++, in_pixels += frame.pitch, out_pixels += Nes_Emu::image_width)
   {
      for (unsigned w = 0; w < Nes_Emu::image_width; w++)
      {
         unsigned col = frame.palette[in_pixels[w]];
         const Nes_Emu::rgb_t& rgb = emu->nes_colors[col];
         unsigned r = rgb.red;
         unsigned g = rgb.green;
         unsigned b = rgb.blue;
         out_pixels[w] = (r << 16) | (g << 8) | (b << 0);
      }
   }

   video_cb(video_buffer, Nes_Emu::image_width, Nes_Emu::image_height,
         Nes_Emu::image_width * sizeof(uint32_t));

   // Mono -> Stereo.
   int16_t samples[2048];
   long read_samples = emu->read_samples(samples, 2048);
   int16_t out_samples[4096];
   for (long i = 0; i < read_samples; i++)
      out_samples[(i << 1)] = out_samples[(i << 1) + 1] = samples[i];

   audio_batch_cb(out_samples, read_samples);
}

bool retro_load_game(const struct retro_game_info *info)
{
   if (!emu)
      return false;

   enum retro_pixel_format fmt = RETRO_PIXEL_FORMAT_XRGB8888;
   if (!environ_cb(RETRO_ENVIRONMENT_SET_PIXEL_FORMAT, &fmt))
   {
      fprintf(stderr, "XRGB8888 is not supported.\n");
      return false;
   }

   emu->set_sample_rate(44100);
   emu->set_equalizer(Nes_Emu::nes_eq);
   emu->set_palette_range(0);

   static uint8_t video_buffer[Nes_Emu::image_width * Nes_Emu::image_height];
   emu->set_pixels(video_buffer, Nes_Emu::image_width);

   Mem_File_Reader reader(info->data, info->size);
   return !emu->load_ines(reader);
}

void retro_unload_game(void)
{
   emu->close();
}

unsigned retro_get_region(void)
{
   return RETRO_REGION_NTSC;
}

bool retro_load_game_special(unsigned, const struct retro_game_info *, size_t)
{
   return false;
}

size_t retro_serialize_size(void)
{
   Mem_Writer writer;
   if (emu->save_state(writer))
      return 0;

   return writer.size();
}

bool retro_serialize(void *data, size_t size)
{
   Mem_Writer writer(data, size);
   return !emu->save_state(writer);
}

bool retro_unserialize(const void *data, size_t size)
{
   Mem_File_Reader reader(data, size);
   return !emu->load_state(reader);
}

void *retro_get_memory_data(unsigned id)
{
   switch (id)
   {
      case RETRO_MEMORY_SAVE_RAM:
         return emu->high_mem();
      case RETRO_MEMORY_SYSTEM_RAM:
         return emu->low_mem();
      default:
         return 0;
   }
}

size_t retro_get_memory_size(unsigned id)
{
   switch (id)
   {
      case RETRO_MEMORY_SAVE_RAM:
         return Nes_Emu::high_mem_size;
      case RETRO_MEMORY_SYSTEM_RAM:
         return Nes_Emu::low_mem_size;
      default:
         return 0;
   }
}

void retro_cheat_reset(void)
{}

void retro_cheat_set(unsigned, bool, const char *)
{}

