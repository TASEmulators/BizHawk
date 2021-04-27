#ifndef gui_h
#define gui_h

#include <SDL.h>
#include <Core/gb.h>
#include <stdbool.h> 
#include "shader.h"

#define JOYSTICK_HIGH 0x4000
#define JOYSTICK_LOW 0x3800

#ifdef __APPLE__
#define MODIFIER KMOD_GUI
#else
#define MODIFIER KMOD_CTRL
#endif

extern GB_gameboy_t gb;

extern SDL_Window *window;
extern SDL_Renderer *renderer;
extern SDL_Texture *texture;
extern SDL_PixelFormat *pixel_format;
extern SDL_Haptic *haptic;
extern shader_t shader;

enum scaling_mode {
    GB_SDL_SCALING_ENTIRE_WINDOW,
    GB_SDL_SCALING_KEEP_RATIO,
    GB_SDL_SCALING_INTEGER_FACTOR,
    GB_SDL_SCALING_MAX,
};


enum pending_command {
    GB_SDL_NO_COMMAND,
    GB_SDL_SAVE_STATE_COMMAND,
    GB_SDL_LOAD_STATE_COMMAND,
    GB_SDL_RESET_COMMAND,
    GB_SDL_NEW_FILE_COMMAND,
    GB_SDL_QUIT_COMMAND,
};

#define GB_SDL_DEFAULT_SCALE_MAX 8

extern enum pending_command pending_command;
extern unsigned command_parameter;

typedef enum {
    JOYPAD_BUTTON_LEFT,
    JOYPAD_BUTTON_RIGHT,
    JOYPAD_BUTTON_UP,
    JOYPAD_BUTTON_DOWN,
    JOYPAD_BUTTON_A,
    JOYPAD_BUTTON_B,
    JOYPAD_BUTTON_SELECT,
    JOYPAD_BUTTON_START,
    JOYPAD_BUTTON_MENU,
    JOYPAD_BUTTON_TURBO,
    JOYPAD_BUTTON_REWIND,
    JOYPAD_BUTTON_SLOW_MOTION,
    JOYPAD_BUTTONS_MAX
} joypad_button_t;

typedef enum {
      JOYPAD_AXISES_X,
      JOYPAD_AXISES_Y,
      JOYPAD_AXISES_MAX
} joypad_axis_t;

typedef struct {
    SDL_Scancode keys[9];
    GB_color_correction_mode_t color_correction_mode;
    enum scaling_mode scaling_mode;
    uint8_t blending_mode;
    
    GB_highpass_mode_t highpass_mode;
    
    bool _deprecated_div_joystick;
    bool _deprecated_flip_joystick_bit_1;
    bool _deprecated_swap_joysticks_bits_1_and_2;
    
    char filter[32];
    enum {
        MODEL_DMG,
        MODEL_CGB,
        MODEL_AGB,
        MODEL_SGB,
        MODEL_MAX,
    } model;
    
    /* v0.11 */
    uint32_t rewind_length;
    SDL_Scancode keys_2[32]; /* Rewind and underclock, + padding for the future */
    uint8_t joypad_configuration[32]; /* 12 Keys + padding for the future*/;
    uint8_t joypad_axises[JOYPAD_AXISES_MAX];
    
    /* v0.12 */
    enum {
        SGB_NTSC,
        SGB_PAL,
        SGB_2,
        SGB_MAX
    } sgb_revision;
    
    /* v0.13 */
    uint8_t dmg_palette;
    GB_border_mode_t border_mode;
    uint8_t volume;
    GB_rumble_mode_t rumble_mode;

    uint8_t default_scale;
} configuration_t;

extern configuration_t configuration;

void update_viewport(void);
void run_gui(bool is_running);
void render_texture(void *pixels, void *previous);
void connect_joypad(void);

joypad_button_t get_joypad_button(uint8_t physical_button);
joypad_axis_t get_joypad_axis(uint8_t physical_axis);

#endif
