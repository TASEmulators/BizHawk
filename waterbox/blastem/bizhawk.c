#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"
#include "../libco/libco.h"

#include "system.h"
#include "genesis.h"
#include "sms.h"
#include "util.h"
#include "vdp.h"
#include "render.h"
#include "io.h"

typedef struct
{
	FrameInfo b;
	uint32_t Buttons;
} MyFrameInfo;

cothread_t __host = NULL;
cothread_t __resume = NULL;
static MyFrameInfo *current_frame;

//////////////////////////////////////////
tern_node *config             = NULL;
char *save_filename           = NULL;
int headless                  = 0;
int exit_after                = 0;
int z80_enabled               = 1;
uint8_t use_native_states     = 1;
system_header *current_system = NULL;
static vid_std video_standard;
static uint32_t overscan_top, overscan_bot;
static uint32_t *fb;
static uint8_t last_fb;
static system_media current_media;
//////////////////////////////////////////

static int video_start_line;
static int video_line_count;
static int video_width;

/* blastem render backend API implementation */
uint32_t render_map_color(uint8_t r, uint8_t g, uint8_t b)
{
   return r << 16 | g << 8 | b;
}

/* Not supported in lib build */
uint8_t render_create_window(char *caption,
      uint32_t width, uint32_t height, window_close_handler close_handler)
{
   return 0;
}

/* Not supported in lib build */
void render_destroy_window(uint8_t which) { }
void render_source_paused(audio_source *src, uint8_t remaining_sources) { }
void render_source_resumed(audio_source *src)                           { }
void render_set_external_sync(uint8_t ext_sync_on)                      { }
void bindings_set_mouse_mode(uint8_t mode)                              { }
void bindings_release_capture(void)                                     { }
void bindings_reacquire_capture(void)                                   { }

void render_errorbox(char *title, char *message) { }
void render_warnbox(char *title, char *message)  { }
void render_infobox(char *title, char *message)  { }

/* Whether this is true depends on the 
 * libretro frontend implementation,
 * but the sync to audio path works better here */
uint8_t render_is_audio_sync(void)             { return 1; }
uint8_t render_should_release_on_exit(void)    { return 0; }
void *render_new_audio_opaque(void)            { return NULL; }
void render_free_audio_opaque(void *opaque)    { }
void render_lock_audio(void)                   { }
void render_unlock_audio(void)                 { }
void render_buffer_consumed(audio_source *src) { }

/* Not actually used in the sync to audio path */
uint32_t render_min_buffered(void)             { return 4; }
uint32_t render_audio_syncs_per_sec(void)      { return 0; }
void render_audio_created(audio_source *src)   { }
void render_set_video_standard(vid_std std) { video_standard = std; }
uint8_t render_get_active_framebuffer(void) { return 0; }
int render_fullscreen(void)                 { return 1; }
uint32_t render_overscan_top(void)          { return overscan_top; }
uint32_t render_overscan_bot(void)          { return overscan_bot; }

void render_do_audio_ready(audio_source *src)
{
   src->read_end = src->buffer_pos;
   int16_t *tmp         = src->front;
   src->front           = src->back;
   src->back            = tmp;
   src->front_populated = 1;
   src->buffer_pos      = 0;
}

void retro_get_system_av_info(int start_line, int line_count, int col_count)
{
   video_width = col_count;
   video_start_line = start_line;
   video_line_count = line_count;

   current_system->set_speed_percent(current_system, 100);
}

uint32_t *render_get_framebuffer(uint8_t which, int *pitch)
{
   *pitch = LINEBUF_SIZE * sizeof(uint32_t);
   if (which != last_fb)
      *pitch = *pitch * 2;

   if (which)
      return fb + LINEBUF_SIZE;
   return fb;
}

void render_framebuffer_updated(uint8_t which, int width)
{
   // all stuff for trans key event to io prot
}

void process_events(void)
{
   // all stuff for trans key event to io prot
}

void run_blastem(void)
{
  current_system->start_context(current_system, NULL);
}

ECL_EXPORT int Init(int cd, int _32xPreinit, int regionAutoOrder, int regionOverride)
{
   static system_type console_type;

   render_audio_initialized(RENDER_AUDIO_S16,
      44100, 2, 1024, sizeof(int16_t));

   // cart_open
   FILE *f = fopen("romfile.md", "rb");
   if (f == NULL)
   {
      printf("blastem Init: Rom file not found\n");
      return 0;
   }
   fseek(f, 0, SEEK_END);
   uint32_t rom_size = ftell(f);
   fseek(f, 0, SEEK_SET);

	current_media.dir       = path_dirname("romfile.md");
	current_media.name      = basename_no_extension("romfile.md");
	current_media.extension = path_extension("romfile.md");
   current_media.buffer = alloc_sealed(nearest_pow2(rom_size));
   current_media.size = rom_size;

   // cart_read
   if (!fread(current_media.buffer, 1, rom_size, f))
   {
      printf("blastem Init: Cart read failed\n");
      fclose(f);
      return 0;
   }
   fclose(f);

   // allocate frame buffer
   fb = alloc_invisible(LINEBUF_SIZE * 294 * 2 * sizeof(uint32_t));

   console_type = detect_system_type(&current_media);
   current_system = alloc_config_system(console_type, &current_media, 0, 0);
   retro_get_system_av_info(0, 294, LINEBUF_SIZE);

   if (!current_system)
      return 0;

   __resume = co_create(65536 * sizeof(void*), run_blastem);
   if (!__resume)
      return 0;
	return 1;
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
   genesis_context *gen = (genesis_context *)current_system;
   m[0].Data = (uint8_t *)gen->work_ram;
   m[0].Name = "68K RAM";
   m[0].Size = RAM_WORDS * sizeof(uint16_t);
   m[0].Flags = MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_WORDSIZE2 | MEMORYAREA_FLAGS_PRIMARY | MEMORYAREA_FLAGS_YUGEENDIAN | MEMORYAREA_FLAGS_SWAPPED;
}

void (*EMInputCallback)(void);
ECL_EXPORT void SetInputCallback(void (*callback)(void))
{
   // all stuff for input callback link
   EMInputCallback = callback;
}

static void Blit(void)
{
   const uint32_t *src = fb;
   FrameInfo *f = &current_frame->b;
   f->Width = video_width;
   f->Height = video_line_count;
   uint8_t *dst = (uint8_t *)f->VideoBuffer;
   src += video_width * video_start_line;

   for (int j = 0; j < video_line_count * video_width; j++)
   {
      uint16_t c1 = (*src & 0xffff);
      uint16_t c2 = (*src++ >> 16);
      *dst++ = c1 & 0xff;
      *dst++ = c1 >> 8;
      *dst++ = c2;
      *dst++ = 0xff;
   }
}

static uint32_t PrevButtons;
ECL_EXPORT void FrameAdvance(MyFrameInfo *f)
{
   int16_t pad_state[2];
   current_frame = f;
   pad_state[0] = f->Buttons & 0xfff;
   pad_state[1] = f->Buttons >> 12 & 0xfff;
   PrevButtons = f->Buttons;

   for (uint8_t btn = 0; btn < 12; btn++)
   {
      for (uint8_t port = 0; port < 2; port++)
      {
         if (pad_state[port] & (1 << btn))
         {
            current_system->gamepad_down(current_system, port+1, btn+1);
         }
         else
         {
            current_system->gamepad_up(current_system, port+1, btn+1);
         }
      }
   }

   // host coroutine
   Blit();
   f->b.Lagged = false;

   // audio
   if (all_sources_ready())
   {
      int16_t buffer[2048];
      mix_and_convert((uint8_t *)buffer, sizeof(buffer), NULL);
      int samples = sizeof(buffer) / (2 * sizeof(int16_t));
      current_frame->b.Samples = samples;
      memcpy(current_frame->b.SoundBuffer, buffer, sizeof(buffer));
   }
   else
   {
      current_frame->b.Samples = 0;
   }

   current_frame = NULL;

   // cpu coroutine
   __host = co_active();
   co_switch(__resume);
}

extern const char rom_db_data[];
char *read_bundled_file(char *name, uint32_t *sizeret)
{
   if (!strcmp(name, "rom.db"))
   {
      *sizeret  = strlen(rom_db_data);
      char *ret = malloc(*sizeret+1);
      memcpy(ret, rom_db_data, *sizeret + 1);
      return ret;
   }
   return NULL;
}
