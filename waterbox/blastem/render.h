/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef RENDER_H_
#define RENDER_H_

#include <stdint.h>

#ifndef IS_LIB
#ifdef USE_FBDEV
#include "special_keys_evdev.h"
#define render_relative_mouse(V)
#else
#include <SDL.h>
#define RENDERKEY_UP       SDLK_UP
#define RENDERKEY_DOWN     SDLK_DOWN
#define RENDERKEY_LEFT     SDLK_LEFT
#define RENDERKEY_RIGHT    SDLK_RIGHT
#define RENDERKEY_ESC      SDLK_ESCAPE
#define RENDERKEY_DEL      SDLK_DELETE
#define RENDERKEY_LSHIFT   SDLK_LSHIFT
#define RENDERKEY_RSHIFT   SDLK_RSHIFT
#define RENDERKEY_LCTRL    SDLK_LCTRL
#define RENDERKEY_RCTRL    SDLK_RCTRL
#define RENDERKEY_LALT     SDLK_LALT
#define RENDERKEY_RALT     SDLK_RALT
#define RENDERKEY_HOME     SDLK_HOME
#define RENDERKEY_END      SDLK_END
#define RENDERKEY_PAGEUP   SDLK_PAGEUP
#define RENDERKEY_PAGEDOWN SDLK_PAGEDOWN
#define RENDERKEY_F1       SDLK_F1
#define RENDERKEY_F2       SDLK_F2
#define RENDERKEY_F3       SDLK_F3
#define RENDERKEY_F4       SDLK_F4
#define RENDERKEY_F5       SDLK_F5
#define RENDERKEY_F6       SDLK_F6
#define RENDERKEY_F7       SDLK_F7
#define RENDERKEY_F8       SDLK_F8
#define RENDERKEY_F9       SDLK_F9
#define RENDERKEY_F10      SDLK_F10
#define RENDERKEY_F11      SDLK_F11
#define RENDERKEY_F12      SDLK_F12
#define RENDERKEY_SELECT   SDLK_SELECT
#define RENDERKEY_PLAY     SDLK_AUDIOPLAY
#define RENDERKEY_SEARCH   SDLK_AC_SEARCH
#define RENDERKEY_BACK     SDLK_AC_BACK
#define RENDERKEY_NP0      SDLK_KP_0
#define RENDERKEY_NP1      SDLK_KP_1
#define RENDERKEY_NP2      SDLK_KP_2
#define RENDERKEY_NP3      SDLK_KP_3
#define RENDERKEY_NP4      SDLK_KP_4
#define RENDERKEY_NP5      SDLK_KP_5
#define RENDERKEY_NP6      SDLK_KP_6
#define RENDERKEY_NP7      SDLK_KP_7
#define RENDERKEY_NP8      SDLK_KP_8
#define RENDERKEY_NP9      SDLK_KP_9
#define RENDERKEY_NP_DIV   SDLK_KP_DIVIDE
#define RENDERKEY_NP_MUL   SDLK_KP_MULTIPLY
#define RENDERKEY_NP_MIN   SDLK_KP_MINUS
#define RENDERKEY_NP_PLUS  SDLK_KP_PLUS
#define RENDERKEY_NP_ENTER SDLK_KP_ENTER
#define RENDERKEY_NP_STOP  SDLK_KP_PERIOD
#define RENDER_DPAD_UP     SDL_HAT_UP
#define RENDER_DPAD_DOWN   SDL_HAT_DOWN
#define RENDER_DPAD_LEFT   SDL_HAT_LEFT
#define RENDER_DPAD_RIGHT  SDL_HAT_RIGHT
#define render_relative_mouse SDL_SetRelativeMouseMode
typedef SDL_Thread* render_thread;
#endif
#endif

#define MAX_JOYSTICKS 8
#define MAX_MICE 8
#define MAX_MOUSE_BUTTONS 8

#define FRAMEBUFFER_ODD 0
#define FRAMEBUFFER_EVEN 1
#define FRAMEBUFFER_UI 2
#define FRAMEBUFFER_USER_START 3

typedef enum {
	VID_NTSC,
	VID_PAL,
	NUM_VID_STD
} vid_std;

#define RENDER_DPAD_BIT 0x40000000
#define RENDER_AXIS_BIT 0x20000000
#define RENDER_AXIS_POS 0x10000000
#define RENDER_INVALID_NAME -1
#define RENDER_NOT_MAPPED -2
#define RENDER_NOT_PLUGGED_IN -3

typedef void (*drop_handler)(const char *filename);
typedef void (*window_close_handler)(uint8_t which);
typedef void (*ui_render_fun)(void);
typedef int (*render_thread_fun)(void*);

uint32_t render_map_color(uint8_t r, uint8_t g, uint8_t b);
void render_save_screenshot(char *path);
uint8_t render_create_window(char *caption, uint32_t width, uint32_t height, window_close_handler close_handler);
void render_destroy_window(uint8_t which);
uint32_t *render_get_framebuffer(uint8_t which, int *pitch);
void render_framebuffer_updated(uint8_t which, int width);
//returns the framebuffer index associated with the Window that has focus
uint8_t render_get_active_framebuffer(void);
void render_init(int width, int height, char * title, uint8_t fullscreen);
void render_set_video_standard(vid_std std);
void render_toggle_fullscreen();
void render_update_caption(char *title);
void render_wait_quit(void);
void process_events();
int render_width();
int render_height();
int render_fullscreen();
void render_set_drag_drop_handler(drop_handler handler);
void process_events();
int32_t render_translate_input_name(int32_t controller, char *name, uint8_t is_axis);
int32_t render_dpad_part(int32_t input);
int32_t render_axis_part(int32_t input);
uint8_t render_direction_part(int32_t input);
char* render_joystick_type_id(int index);
void render_errorbox(char *title, char *message);
void render_warnbox(char *title, char *message);
void render_infobox(char *title, char *message);
uint32_t render_emulated_width();
uint32_t render_emulated_height();
uint32_t render_overscan_top();
uint32_t render_overscan_bot();
uint32_t render_overscan_left();
uint32_t render_elapsed_ms(void);
void render_sleep_ms(uint32_t delay);
uint8_t render_has_gl(void);
void render_config_updated(void);
void render_set_gl_context_handlers(ui_render_fun destroy, ui_render_fun create);
void render_set_ui_render_fun(ui_render_fun);
void render_set_ui_fb_resize_handler(ui_render_fun resize);
void render_video_loop(void);
uint8_t render_should_release_on_exit(void);
void render_set_external_sync(uint8_t ext_sync_on);
void render_reset_mappings(void);
#ifndef IS_LIB
uint8_t render_create_thread(render_thread *thread, const char *name, render_thread_fun fun, void *data);
#endif

#endif //RENDER_H_

