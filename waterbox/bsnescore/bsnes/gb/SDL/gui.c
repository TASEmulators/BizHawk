#include <OpenDialog/open_dialog.h>
#include <SDL.h>
#include <stdbool.h>
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include "utils.h"
#include "gui.h"
#include "font.h"

static const SDL_Color gui_palette[4] = {{8, 24, 16,}, {57, 97, 57,}, {132, 165, 99}, {198, 222, 140}};
static uint32_t gui_palette_native[4];

SDL_Window *window = NULL;
SDL_Renderer *renderer = NULL;
SDL_Texture *texture = NULL;
SDL_PixelFormat *pixel_format = NULL;
enum pending_command pending_command;
unsigned command_parameter;

#ifdef __APPLE__
#define MODIFIER_NAME " " CMD_STRING
#else
#define MODIFIER_NAME CTRL_STRING
#endif

shader_t shader;
static SDL_Rect rect;
static unsigned factor;

void render_texture(void *pixels,  void *previous)
{
    if (renderer) {
        if (pixels) {
            SDL_UpdateTexture(texture, NULL, pixels, GB_get_screen_width(&gb) * sizeof (uint32_t));
        }
        SDL_RenderClear(renderer);
        SDL_RenderCopy(renderer, texture, NULL, NULL);
        SDL_RenderPresent(renderer);
    }
    else {
        static void *_pixels = NULL;
        if (pixels) {
            _pixels = pixels;
        }
        glClearColor(0, 0, 0, 1);
        glClear(GL_COLOR_BUFFER_BIT);
        GB_frame_blending_mode_t mode = configuration.blending_mode;
        if (!previous) {
            mode = GB_FRAME_BLENDING_MODE_DISABLED;
        }
        else if (mode == GB_FRAME_BLENDING_MODE_ACCURATE) {
            if (GB_is_sgb(&gb)) {
                mode = GB_FRAME_BLENDING_MODE_SIMPLE;
            }
            else {
                mode = GB_is_odd_frame(&gb)? GB_FRAME_BLENDING_MODE_ACCURATE_ODD : GB_FRAME_BLENDING_MODE_ACCURATE_EVEN;
            }
        }
        render_bitmap_with_shader(&shader, _pixels, previous,
                                  GB_get_screen_width(&gb), GB_get_screen_height(&gb),
                                  rect.x, rect.y, rect.w, rect.h,
                                  mode);
        SDL_GL_SwapWindow(window);
    }
}

configuration_t configuration =
{
    .keys = {
        SDL_SCANCODE_RIGHT,
        SDL_SCANCODE_LEFT,
        SDL_SCANCODE_UP,
        SDL_SCANCODE_DOWN,
        SDL_SCANCODE_X,
        SDL_SCANCODE_Z,
        SDL_SCANCODE_BACKSPACE,
        SDL_SCANCODE_RETURN,
        SDL_SCANCODE_SPACE
    },
    .keys_2 = {
        SDL_SCANCODE_TAB,
        SDL_SCANCODE_LSHIFT,
    },
    .joypad_configuration = {
        13,
        14,
        11,
        12,
        0,
        1,
        9,
        8,
        10,
        4,
        -1,
        5,
    },
    .joypad_axises = {
        0,
        1,
    },
    .color_correction_mode = GB_COLOR_CORRECTION_EMULATE_HARDWARE,
    .highpass_mode = GB_HIGHPASS_ACCURATE,
    .scaling_mode = GB_SDL_SCALING_INTEGER_FACTOR,
    .blending_mode = GB_FRAME_BLENDING_MODE_ACCURATE,
    .rewind_length = 60 * 2,
    .model = MODEL_CGB,
    .volume = 100,
    .rumble_mode = GB_RUMBLE_ALL_GAMES,
    .default_scale = 2,
};


static const char *help[] = {
"Drop a ROM to play.\n"
"\n"
"Keyboard Shortcuts:\n"
" Open Menu:        Escape\n"
" Open ROM:          " MODIFIER_NAME "+O\n"
" Reset:             " MODIFIER_NAME "+R\n"
" Pause:             " MODIFIER_NAME "+P\n"
" Save state:    " MODIFIER_NAME "+(0-9)\n"
" Load state:  " MODIFIER_NAME "+" SHIFT_STRING "+(0-9)\n"
" Toggle Fullscreen  " MODIFIER_NAME "+F\n"
#ifdef __APPLE__
" Mute/Unmute:     " MODIFIER_NAME "+" SHIFT_STRING "+M\n"
#else
" Mute/Unmute:       " MODIFIER_NAME "+M\n"
#endif
" Break Debugger:    " CTRL_STRING "+C"
};

void update_viewport(void)
{
    int win_width, win_height;
    SDL_GL_GetDrawableSize(window, &win_width, &win_height);
    int logical_width, logical_height;
    SDL_GetWindowSize(window, &logical_width, &logical_height);
    factor = win_width / logical_width;
    
    double x_factor = win_width / (double) GB_get_screen_width(&gb);
    double y_factor = win_height / (double) GB_get_screen_height(&gb);
    
    if (configuration.scaling_mode == GB_SDL_SCALING_INTEGER_FACTOR) {
        x_factor = (unsigned)(x_factor);
        y_factor = (unsigned)(y_factor);
    }
    
    if (configuration.scaling_mode != GB_SDL_SCALING_ENTIRE_WINDOW) {
        if (x_factor > y_factor) {
            x_factor = y_factor;
        }
        else {
            y_factor = x_factor;
        }
    }
    
    unsigned new_width = x_factor * GB_get_screen_width(&gb);
    unsigned new_height = y_factor * GB_get_screen_height(&gb);
    
    rect = (SDL_Rect){(win_width  - new_width) / 2, (win_height - new_height) /2,
        new_width, new_height};
    
    if (renderer) {
        SDL_RenderSetViewport(renderer, &rect);
    }
    else {
        glViewport(rect.x, rect.y, rect.w, rect.h);
    }
}

static void rescale_window(void)
{
    SDL_SetWindowSize(window, GB_get_screen_width(&gb) * configuration.default_scale, GB_get_screen_height(&gb) * configuration.default_scale);
}

/* Does NOT check for bounds! */
static void draw_char(uint32_t *buffer, unsigned width, unsigned height, unsigned char ch, uint32_t color)
{
    if (ch < ' ' || ch > font_max) {
        ch = '?';
    }
    
    uint8_t *data = &font[(ch - ' ') * GLYPH_WIDTH * GLYPH_HEIGHT];
    
    for (unsigned y = GLYPH_HEIGHT; y--;) {
        for (unsigned x = GLYPH_WIDTH; x--;) {
            if (*(data++)) {
                (*buffer) = color;
            }
            buffer++;
        }
        buffer += width - GLYPH_WIDTH;
    }
}

static unsigned scroll = 0;
static void draw_unbordered_text(uint32_t *buffer, unsigned width, unsigned height, unsigned x, unsigned y, const char *string, uint32_t color)
{
    y -= scroll;
    unsigned orig_x = x;
    unsigned y_offset = (GB_get_screen_height(&gb) - 144) / 2;
    while (*string) {
        if (*string == '\n') {
            x = orig_x;
            y += GLYPH_HEIGHT + 4;
            string++;
            continue;
        }
        
        if (x > width - GLYPH_WIDTH || y == 0 || y - y_offset > 144 - GLYPH_HEIGHT) {
            break;
        }
        
        draw_char(&buffer[x + width * y], width, height, *string, color);
        x += GLYPH_WIDTH;
        string++;
    }
}

static void draw_text(uint32_t *buffer, unsigned width, unsigned height, unsigned x, unsigned y, const char *string, uint32_t color, uint32_t border)
{
    draw_unbordered_text(buffer, width, height, x - 1, y, string, border);
    draw_unbordered_text(buffer, width, height, x + 1, y, string, border);
    draw_unbordered_text(buffer, width, height, x, y - 1, string, border);
    draw_unbordered_text(buffer, width, height, x, y + 1, string, border);
    draw_unbordered_text(buffer, width, height, x, y, string, color);
}

enum decoration {
    DECORATION_NONE,
    DECORATION_SELECTION,
    DECORATION_ARROWS,
};

static void draw_text_centered(uint32_t *buffer, unsigned width, unsigned height, unsigned y, const char *string, uint32_t color, uint32_t border, enum decoration decoration)
{
    unsigned x = width / 2 - (unsigned) strlen(string) * GLYPH_WIDTH / 2;
    draw_text(buffer, width, height, x, y, string, color, border);
    switch (decoration) {
        case DECORATION_SELECTION:
            draw_text(buffer, width, height, x - GLYPH_WIDTH, y, SELECTION_STRING, color, border);
            break;
        case DECORATION_ARROWS:
            draw_text(buffer, width, height, x - GLYPH_WIDTH, y, LEFT_ARROW_STRING, color, border);
            draw_text(buffer, width, height, width - x, y, RIGHT_ARROW_STRING, color, border);
            break;
            
        case DECORATION_NONE:
            break;
    }
}

struct menu_item {
    const char *string;
    void (*handler)(unsigned);
    const char *(*value_getter)(unsigned);
    void (*backwards_handler)(unsigned);
};
static const struct menu_item *current_menu = NULL;
static const struct menu_item *root_menu = NULL;
static unsigned current_selection = 0;

static enum {
    SHOWING_DROP_MESSAGE,
    SHOWING_MENU,
    SHOWING_HELP,
    WAITING_FOR_KEY,
    WAITING_FOR_JBUTTON,
} gui_state;

static unsigned joypad_configuration_progress = 0;
static uint8_t joypad_axis_temp;

static void item_exit(unsigned index)
{
    pending_command = GB_SDL_QUIT_COMMAND;
}

static unsigned current_help_page = 0;
static void item_help(unsigned index)
{
    current_help_page = 0;
    gui_state = SHOWING_HELP;
}

static void enter_emulation_menu(unsigned index);
static void enter_graphics_menu(unsigned index);
static void enter_controls_menu(unsigned index);
static void enter_joypad_menu(unsigned index);
static void enter_audio_menu(unsigned index);

extern void set_filename(const char *new_filename, typeof(free) *new_free_function);
static void open_rom(unsigned index)
{
    char *filename = do_open_rom_dialog();
    if (filename) {
        set_filename(filename, free);
        pending_command = GB_SDL_NEW_FILE_COMMAND;
    }
}

static const struct menu_item paused_menu[] = {
    {"Resume", NULL},
    {"Open ROM", open_rom},
    {"Emulation Options", enter_emulation_menu},
    {"Graphic Options", enter_graphics_menu},
    {"Audio Options", enter_audio_menu},
    {"Keyboard", enter_controls_menu},
    {"Joypad", enter_joypad_menu},
    {"Help", item_help},
    {"Quit SameBoy", item_exit},
    {NULL,}
};

static const struct menu_item *const nonpaused_menu = &paused_menu[1];

static void return_to_root_menu(unsigned index)
{
    current_menu = root_menu;
    current_selection = 0;
    scroll = 0;
}

static void cycle_model(unsigned index)
{
    
    configuration.model++;
    if (configuration.model == MODEL_MAX) {
        configuration.model = 0;
    }
    pending_command = GB_SDL_RESET_COMMAND;
}

static void cycle_model_backwards(unsigned index)
{
    if (configuration.model == 0) {
        configuration.model = MODEL_MAX;
    }
    configuration.model--;
    pending_command = GB_SDL_RESET_COMMAND;
}

const char *current_model_string(unsigned index)
{
    return (const char *[]){"Game Boy", "Game Boy Color", "Game Boy Advance", "Super Game Boy"}
        [configuration.model];
}

static void cycle_sgb_revision(unsigned index)
{
    
    configuration.sgb_revision++;
    if (configuration.sgb_revision == SGB_MAX) {
        configuration.sgb_revision = 0;
    }
    pending_command = GB_SDL_RESET_COMMAND;
}

static void cycle_sgb_revision_backwards(unsigned index)
{
    if (configuration.sgb_revision == 0) {
        configuration.sgb_revision = SGB_MAX;
    }
    configuration.sgb_revision--;
    pending_command = GB_SDL_RESET_COMMAND;
}

const char *current_sgb_revision_string(unsigned index)
{
    return (const char *[]){"Super Game Boy NTSC", "Super Game Boy PAL", "Super Game Boy 2"}
    [configuration.sgb_revision];
}

static const uint32_t rewind_lengths[] = {0, 10, 30, 60, 60 * 2, 60 * 5, 60 * 10};
static const char *rewind_strings[] = {"Disabled",
                                       "10 Seconds",
                                       "30 Seconds",
                                       "1 Minute",
                                       "2 Minutes",
                                       "5 Minutes",
                                       "10 Minutes",
};

static void cycle_rewind(unsigned index)
{
    for (unsigned i = 0; i < sizeof(rewind_lengths) / sizeof(rewind_lengths[0]) - 1; i++) {
        if (configuration.rewind_length == rewind_lengths[i]) {
            configuration.rewind_length = rewind_lengths[i + 1];
            GB_set_rewind_length(&gb, configuration.rewind_length);
            return;
        }
    }
    configuration.rewind_length = rewind_lengths[0];
    GB_set_rewind_length(&gb, configuration.rewind_length);
}

static void cycle_rewind_backwards(unsigned index)
{
    for (unsigned i = 1; i < sizeof(rewind_lengths) / sizeof(rewind_lengths[0]); i++) {
        if (configuration.rewind_length == rewind_lengths[i]) {
            configuration.rewind_length = rewind_lengths[i - 1];
            GB_set_rewind_length(&gb, configuration.rewind_length);
            return;
        }
    }
    configuration.rewind_length = rewind_lengths[sizeof(rewind_lengths) / sizeof(rewind_lengths[0]) - 1];
    GB_set_rewind_length(&gb, configuration.rewind_length);
}

const char *current_rewind_string(unsigned index)
{
    for (unsigned i = 0; i < sizeof(rewind_lengths) / sizeof(rewind_lengths[0]); i++) {
        if (configuration.rewind_length == rewind_lengths[i]) {
            return rewind_strings[i];
        }
    }
    return "Custom";
}

static const struct menu_item emulation_menu[] = {
    {"Emulated Model:", cycle_model, current_model_string, cycle_model_backwards},
    {"SGB Revision:", cycle_sgb_revision, current_sgb_revision_string, cycle_sgb_revision_backwards},
    {"Rewind Length:", cycle_rewind, current_rewind_string, cycle_rewind_backwards},
    {"Back", return_to_root_menu},
    {NULL,}
};

static void enter_emulation_menu(unsigned index)
{
    current_menu = emulation_menu;
    current_selection = 0;
    scroll = 0;
}

const char *current_scaling_mode(unsigned index)
{
    return (const char *[]){"Fill Entire Window", "Retain Aspect Ratio", "Retain Integer Factor"}
        [configuration.scaling_mode];
}

const char *current_default_scale(unsigned index)
{
    return (const char *[]){"1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x"}
        [configuration.default_scale - 1];
}

const char *current_color_correction_mode(unsigned index)
{
    return (const char *[]){"Disabled", "Correct Color Curves", "Emulate Hardware", "Preserve Brightness", "Reduce Contrast"}
        [configuration.color_correction_mode];
}

const char *current_palette(unsigned index)
{
    return (const char *[]){"Greyscale", "Lime (Game Boy)", "Olive (Pocket)", "Teal (Light)"}
        [configuration.dmg_palette];
}

const char *current_border_mode(unsigned index)
{
    return (const char *[]){"SGB Only", "Never", "Always"}
        [configuration.border_mode];
}

void cycle_scaling(unsigned index)
{
    configuration.scaling_mode++;
    if (configuration.scaling_mode == GB_SDL_SCALING_MAX) {
        configuration.scaling_mode = 0;
    }
    update_viewport();
    render_texture(NULL, NULL);
}

void cycle_scaling_backwards(unsigned index)
{
    if (configuration.scaling_mode == 0) {
        configuration.scaling_mode = GB_SDL_SCALING_MAX - 1;
    }
    else {
        configuration.scaling_mode--;
    }
    update_viewport();
    render_texture(NULL, NULL);
}

void cycle_default_scale(unsigned index)
{
    if (configuration.default_scale == GB_SDL_DEFAULT_SCALE_MAX) {
        configuration.default_scale = 1;
    }
    else {
        configuration.default_scale++;
    }

    rescale_window();
    update_viewport();
}

void cycle_default_scale_backwards(unsigned index)
{
    if (configuration.default_scale == 1) {
        configuration.default_scale = GB_SDL_DEFAULT_SCALE_MAX;
    }
    else {
        configuration.default_scale--;
    }

    rescale_window();
    update_viewport();
}

static void cycle_color_correction(unsigned index)
{
    if (configuration.color_correction_mode == GB_COLOR_CORRECTION_REDUCE_CONTRAST) {
        configuration.color_correction_mode = GB_COLOR_CORRECTION_DISABLED;
    }
    else {
        configuration.color_correction_mode++;
    }
}

static void cycle_color_correction_backwards(unsigned index)
{
    if (configuration.color_correction_mode == GB_COLOR_CORRECTION_DISABLED) {
        configuration.color_correction_mode = GB_COLOR_CORRECTION_REDUCE_CONTRAST;
    }
    else {
        configuration.color_correction_mode--;
    }
}

static void cycle_palette(unsigned index)
{
    if (configuration.dmg_palette == 3) {
        configuration.dmg_palette = 0;
    }
    else {
        configuration.dmg_palette++;
    }
}

static void cycle_palette_backwards(unsigned index)
{
    if (configuration.dmg_palette == 0) {
        configuration.dmg_palette = 3;
    }
    else {
        configuration.dmg_palette--;
    }
}

static void cycle_border_mode(unsigned index)
{
    if (configuration.border_mode == GB_BORDER_ALWAYS) {
        configuration.border_mode = GB_BORDER_SGB;
    }
    else {
        configuration.border_mode++;
    }
}

static void cycle_border_mode_backwards(unsigned index)
{
    if (configuration.border_mode == GB_BORDER_SGB) {
        configuration.border_mode = GB_BORDER_ALWAYS;
    }
    else {
        configuration.border_mode--;
    }
}

struct shader_name {
    const char *file_name;
    const char *display_name;
} shaders[] =
{
    {"NearestNeighbor", "Nearest Neighbor"},
    {"Bilinear", "Bilinear"},
    {"SmoothBilinear", "Smooth Bilinear"},
    {"MonoLCD", "Monochrome LCD"},
    {"LCD", "LCD Display"},
    {"CRT", "CRT Display"},
    {"Scale2x", "Scale2x"},
    {"Scale4x", "Scale4x"},
    {"AAScale2x", "Anti-aliased Scale2x"},
    {"AAScale4x", "Anti-aliased Scale4x"},
    {"HQ2x", "HQ2x"},
    {"OmniScale", "OmniScale"},
    {"OmniScaleLegacy", "OmniScale Legacy"},
    {"AAOmniScaleLegacy", "AA OmniScale Legacy"},
};

static void cycle_filter(unsigned index)
{
    unsigned i = 0;
    for (; i < sizeof(shaders) / sizeof(shaders[0]); i++) {
        if (strcmp(shaders[i].file_name, configuration.filter) == 0) {
            break;
        }
    }
    

    i += 1;
    if (i >= sizeof(shaders) / sizeof(shaders[0])) {
        i -= sizeof(shaders) / sizeof(shaders[0]);
    }
    
    strcpy(configuration.filter, shaders[i].file_name);
    free_shader(&shader);
    if (!init_shader_with_name(&shader, configuration.filter)) {
        init_shader_with_name(&shader, "NearestNeighbor");
    }
}

static void cycle_filter_backwards(unsigned index)
{
    unsigned i = 0;
    for (; i < sizeof(shaders) / sizeof(shaders[0]); i++) {
        if (strcmp(shaders[i].file_name, configuration.filter) == 0) {
            break;
        }
    }
    
    i -= 1;
    if (i >= sizeof(shaders) / sizeof(shaders[0])) {
        i = sizeof(shaders) / sizeof(shaders[0]) - 1;
    }
    
    strcpy(configuration.filter, shaders[i].file_name);
    free_shader(&shader);
    if (!init_shader_with_name(&shader, configuration.filter)) {
        init_shader_with_name(&shader, "NearestNeighbor");
    }

}
const char *current_filter_name(unsigned index)
{
    unsigned i = 0;
    for (; i < sizeof(shaders) / sizeof(shaders[0]); i++) {
        if (strcmp(shaders[i].file_name, configuration.filter) == 0) {
            break;
        }
    }
    
    if (i == sizeof(shaders) / sizeof(shaders[0])) {
        i = 0;
    }
    
    return shaders[i].display_name;
}

static void cycle_blending_mode(unsigned index)
{
    if (configuration.blending_mode == GB_FRAME_BLENDING_MODE_ACCURATE) {
        configuration.blending_mode = GB_FRAME_BLENDING_MODE_DISABLED;
    }
    else {
        configuration.blending_mode++;
    }
}

static void cycle_blending_mode_backwards(unsigned index)
{
    if (configuration.blending_mode == GB_FRAME_BLENDING_MODE_DISABLED) {
        configuration.blending_mode = GB_FRAME_BLENDING_MODE_ACCURATE;
    }
    else {
        configuration.blending_mode--;
    }
}

const char *blending_mode_string(unsigned index)
{
    return (const char *[]){"Disabled", "Simple", "Accurate"}
    [configuration.blending_mode];
}

static const struct menu_item graphics_menu[] = {
    {"Scaling Mode:", cycle_scaling, current_scaling_mode, cycle_scaling_backwards},
    {"Default Window Scale:", cycle_default_scale, current_default_scale, cycle_default_scale_backwards},
    {"Scaling Filter:", cycle_filter, current_filter_name, cycle_filter_backwards},
    {"Color Correction:", cycle_color_correction, current_color_correction_mode, cycle_color_correction_backwards},
    {"Frame Blending:", cycle_blending_mode, blending_mode_string, cycle_blending_mode_backwards},
    {"Mono Palette:", cycle_palette, current_palette, cycle_palette_backwards},
    {"Display Border:", cycle_border_mode, current_border_mode, cycle_border_mode_backwards},
    {"Back", return_to_root_menu},
    {NULL,}
};

static void enter_graphics_menu(unsigned index)
{
    current_menu = graphics_menu;
    current_selection = 0;
    scroll = 0;
}

const char *highpass_filter_string(unsigned index)
{
    return (const char *[]){"None (Keep DC Offset)", "Accurate", "Preserve Waveform"}
        [configuration.highpass_mode];
}

void cycle_highpass_filter(unsigned index)
{
    configuration.highpass_mode++;
    if (configuration.highpass_mode == GB_HIGHPASS_MAX) {
        configuration.highpass_mode = 0;
    }
}

void cycle_highpass_filter_backwards(unsigned index)
{
    if (configuration.highpass_mode == 0) {
        configuration.highpass_mode = GB_HIGHPASS_MAX - 1;
    }
    else {
        configuration.highpass_mode--;
    }
}

const char *volume_string(unsigned index)
{
    static char ret[5];
    sprintf(ret, "%d%%", configuration.volume);
    return ret;
}

void increase_volume(unsigned index)
{
    configuration.volume += 5;
    if (configuration.volume > 100) {
        configuration.volume = 100;
    }
}

void decrease_volume(unsigned index)
{
    configuration.volume -= 5;
    if (configuration.volume > 100) {
        configuration.volume = 0;
    }
}

static const struct menu_item audio_menu[] = {
    {"Highpass Filter:", cycle_highpass_filter, highpass_filter_string, cycle_highpass_filter_backwards},
    {"Volume:", increase_volume, volume_string, decrease_volume},
    {"Back", return_to_root_menu},
    {NULL,}
};

static void enter_audio_menu(unsigned index)
{
    current_menu = audio_menu;
    current_selection = 0;
    scroll = 0;
}

static void modify_key(unsigned index)
{
    gui_state = WAITING_FOR_KEY;
}

static const char *key_name(unsigned index);

static const struct menu_item controls_menu[] = {
    {"Right:", modify_key, key_name,},
    {"Left:", modify_key, key_name,},
    {"Up:", modify_key, key_name,},
    {"Down:", modify_key, key_name,},
    {"A:", modify_key, key_name,},
    {"B:", modify_key, key_name,},
    {"Select:", modify_key, key_name,},
    {"Start:", modify_key, key_name,},
    {"Turbo:", modify_key, key_name,},
    {"Rewind:", modify_key, key_name,},
    {"Slow-Motion:", modify_key, key_name,},
    {"Back", return_to_root_menu},
    {NULL,}
};

static const char *key_name(unsigned index)
{
    if (index >= 8) {
        if (index == 8) {
            return SDL_GetScancodeName(configuration.keys[8]);
        }
        return SDL_GetScancodeName(configuration.keys_2[index - 9]);
    }
    return SDL_GetScancodeName(configuration.keys[index]);
}

static void enter_controls_menu(unsigned index)
{
    current_menu = controls_menu;
    current_selection = 0;
    scroll = 0;
}

static unsigned joypad_index = 0;
static SDL_Joystick *joystick = NULL;
static SDL_GameController *controller = NULL;
SDL_Haptic *haptic = NULL;

const char *current_joypad_name(unsigned index)
{
    static char name[23] = {0,};
    const char *orig_name = joystick? SDL_JoystickName(joystick) : NULL;
    if (!orig_name) return "Not Found";
    unsigned i = 0;
    
    // SDL returns a name with repeated and trailing spaces
    while (*orig_name && i < sizeof(name) - 2) {
        if (orig_name[0] != ' ' || orig_name[1] != ' ') {
            name[i++] = *orig_name;
        }
        orig_name++;
    }
    if (i && name[i - 1] == ' ') {
        i--;
    }
    name[i] = 0;
    
    return name;
}

static void cycle_joypads(unsigned index)
{
    joypad_index++;
    if (joypad_index >= SDL_NumJoysticks()) {
        joypad_index = 0;
    }
    
    if (haptic) {
        SDL_HapticClose(haptic);
        haptic = NULL;
    }
    
    if (controller) {
        SDL_GameControllerClose(controller);
        controller = NULL;
    }
    else if (joystick) {
        SDL_JoystickClose(joystick);
        joystick = NULL;
    }
    if ((controller = SDL_GameControllerOpen(joypad_index))) {
        joystick = SDL_GameControllerGetJoystick(controller);
    }
    else {
        joystick = SDL_JoystickOpen(joypad_index);
    }
    if (joystick) {
        haptic = SDL_HapticOpenFromJoystick(joystick);
    }}

static void cycle_joypads_backwards(unsigned index)
{
    joypad_index--;
    if (joypad_index >= SDL_NumJoysticks()) {
        joypad_index = SDL_NumJoysticks() - 1;
    }
    
    if (haptic) {
        SDL_HapticClose(haptic);
        haptic = NULL;
    }
    
    if (controller) {
        SDL_GameControllerClose(controller);
        controller = NULL;
    }
    else if (joystick) {
        SDL_JoystickClose(joystick);
        joystick = NULL;
    }
    if ((controller = SDL_GameControllerOpen(joypad_index))) {
        joystick = SDL_GameControllerGetJoystick(controller);
    }
    else {
        joystick = SDL_JoystickOpen(joypad_index);
    }
    if (joystick) {
        haptic = SDL_HapticOpenFromJoystick(joystick);
    }}

static void detect_joypad_layout(unsigned index)
{
    gui_state = WAITING_FOR_JBUTTON;
    joypad_configuration_progress = 0;
    joypad_axis_temp = -1;
}

static void cycle_rumble_mode(unsigned index)
{
    if (configuration.rumble_mode == GB_RUMBLE_ALL_GAMES) {
        configuration.rumble_mode = GB_RUMBLE_DISABLED;
    }
    else {
        configuration.rumble_mode++;
    }
}

static void cycle_rumble_mode_backwards(unsigned index)
{
    if (configuration.rumble_mode == GB_RUMBLE_DISABLED) {
        configuration.rumble_mode = GB_RUMBLE_ALL_GAMES;
    }
    else {
        configuration.rumble_mode--;
    }
}

const char *current_rumble_mode(unsigned index)
{
    return (const char *[]){"Disabled", "Rumble Game Paks Only", "All Games"}
    [configuration.rumble_mode];
}

static const struct menu_item joypad_menu[] = {
    {"Joypad:", cycle_joypads, current_joypad_name, cycle_joypads_backwards},
    {"Configure layout", detect_joypad_layout},
    {"Rumble Mode:", cycle_rumble_mode, current_rumble_mode, cycle_rumble_mode_backwards},
    {"Back", return_to_root_menu},
    {NULL,}
};

static void enter_joypad_menu(unsigned index)
{
    current_menu = joypad_menu;
    current_selection = 0;
    scroll = 0;
}

joypad_button_t get_joypad_button(uint8_t physical_button)
{
    for (unsigned i = 0; i < JOYPAD_BUTTONS_MAX; i++) {
        if (configuration.joypad_configuration[i] == physical_button) {
            return i;
        }
    }
    return JOYPAD_BUTTONS_MAX;
}

joypad_axis_t get_joypad_axis(uint8_t physical_axis)
{
    for (unsigned i = 0; i < JOYPAD_AXISES_MAX; i++) {
        if (configuration.joypad_axises[i] == physical_axis) {
            return i;
        }
    }
    return JOYPAD_AXISES_MAX;
}


void connect_joypad(void)
{
    if (joystick && !SDL_NumJoysticks()) {
        if (controller) {
            SDL_GameControllerClose(controller);
            controller = NULL;
            joystick = NULL;
        }
        else {
            SDL_JoystickClose(joystick);
            joystick = NULL;
        }
    }
    else if (!joystick && SDL_NumJoysticks()) {
        if ((controller = SDL_GameControllerOpen(0))) {
            joystick = SDL_GameControllerGetJoystick(controller);
        }
        else {
            joystick = SDL_JoystickOpen(0);
        }
    }
    if (joystick) {
        haptic = SDL_HapticOpenFromJoystick(joystick);
    }
}

void run_gui(bool is_running)
{
    SDL_ShowCursor(SDL_ENABLE);
    connect_joypad();
    
    /* Draw the background screen */
    static SDL_Surface *converted_background = NULL;
    if (!converted_background) {
        SDL_Surface *background = SDL_LoadBMP(resource_path("background.bmp"));
        SDL_SetPaletteColors(background->format->palette, gui_palette, 0, 4);
        converted_background = SDL_ConvertSurface(background, pixel_format, 0);
        SDL_LockSurface(converted_background);
        SDL_FreeSurface(background);
        
        for (unsigned i = 4; i--; ) {
            gui_palette_native[i] = SDL_MapRGB(pixel_format, gui_palette[i].r, gui_palette[i].g, gui_palette[i].b);
        }
    }

    unsigned width = GB_get_screen_width(&gb);
    unsigned height = GB_get_screen_height(&gb);
    unsigned x_offset = (width - 160) / 2;
    unsigned y_offset = (height - 144) / 2;
    uint32_t pixels[width * height];
    
    if (width != 160 || height != 144) {
        for (unsigned i = 0; i < width * height; i++) {
            pixels[i] = gui_palette_native[0];
        }
    }
    
    SDL_Event event = {0,};
    gui_state = is_running? SHOWING_MENU : SHOWING_DROP_MESSAGE;
    bool should_render = true;
    current_menu = root_menu = is_running? paused_menu : nonpaused_menu;
    current_selection = 0;
    scroll = 0;
    do {
        /* Convert Joypad and mouse events (We only generate down events) */
        if (gui_state != WAITING_FOR_KEY && gui_state != WAITING_FOR_JBUTTON) {
            switch (event.type) {
                case SDL_WINDOWEVENT:
                    should_render = true;
                    break;
                case SDL_MOUSEBUTTONDOWN:
                    if (gui_state == SHOWING_HELP) {
                        event.type = SDL_KEYDOWN;
                        event.key.keysym.scancode = SDL_SCANCODE_RETURN;
                    }
                    else if (gui_state == SHOWING_DROP_MESSAGE) {
                        event.type = SDL_KEYDOWN;
                        event.key.keysym.scancode = SDL_SCANCODE_ESCAPE;
                    }
                    else if (gui_state == SHOWING_MENU) {
                        signed x = (event.button.x - rect.x / factor) * width / (rect.w / factor) - x_offset;
                        signed y = (event.button.y - rect.y / factor) * height / (rect.h / factor) - y_offset;
                        
                        if (strcmp("CRT", configuration.filter) == 0) {
                            y = y * 8 / 7;
                            y -= 144 / 16;
                        }
                        y += scroll;
                        
                        if (x < 0 || x >= 160 || y < 24) {
                            continue;
                        }
                        
                        unsigned item_y = 24;
                        unsigned index = 0;
                        for (const struct menu_item *item = current_menu; item->string; item++, index++) {
                            if (!item->backwards_handler) {
                                if (y >= item_y && y < item_y + 12) {
                                    break;
                                }
                                item_y += 12;
                            }
                            else {
                                if (y >= item_y && y < item_y + 24) {
                                    break;
                                }
                                item_y += 24;
                            }
                        }
                        
                        if (!current_menu[index].string) continue;
                        
                        current_selection = index;
                        event.type = SDL_KEYDOWN;
                        if (current_menu[index].backwards_handler) {
                            event.key.keysym.scancode = x < 80? SDL_SCANCODE_LEFT : SDL_SCANCODE_RIGHT;
                        }
                        else {
                            event.key.keysym.scancode = SDL_SCANCODE_RETURN;
                        }

                    }
                    break;
                case SDL_JOYBUTTONDOWN:
                    event.type = SDL_KEYDOWN;
                    joypad_button_t button = get_joypad_button(event.jbutton.button);
                    if (button == JOYPAD_BUTTON_A) {
                        event.key.keysym.scancode = SDL_SCANCODE_RETURN;
                    }
                    else if (button == JOYPAD_BUTTON_MENU) {
                        event.key.keysym.scancode = SDL_SCANCODE_ESCAPE;
                    }
                    else if (button == JOYPAD_BUTTON_UP) event.key.keysym.scancode = SDL_SCANCODE_UP;
                    else if (button == JOYPAD_BUTTON_DOWN) event.key.keysym.scancode = SDL_SCANCODE_DOWN;
                    else if (button == JOYPAD_BUTTON_LEFT) event.key.keysym.scancode = SDL_SCANCODE_LEFT;
                    else if (button == JOYPAD_BUTTON_RIGHT) event.key.keysym.scancode = SDL_SCANCODE_RIGHT;
                    break;

                case SDL_JOYHATMOTION: {
                    uint8_t value = event.jhat.value;
                    if (value != 0) {
                        uint32_t scancode =
                            value == SDL_HAT_UP ? SDL_SCANCODE_UP
                            : value == SDL_HAT_DOWN ? SDL_SCANCODE_DOWN
                            : value == SDL_HAT_LEFT ? SDL_SCANCODE_LEFT
                            : value == SDL_HAT_RIGHT ? SDL_SCANCODE_RIGHT
                            : 0;

                        if (scancode != 0) {
                            event.type = SDL_KEYDOWN;
                            event.key.keysym.scancode = scancode;
                        }
                    }
                    break;
               }
                    
                case SDL_JOYAXISMOTION: {
                    static bool axis_active[2] = {false, false};
                    joypad_axis_t axis = get_joypad_axis(event.jaxis.axis);
                    if (axis == JOYPAD_AXISES_X) {
                        if (!axis_active[0] && event.jaxis.value > JOYSTICK_HIGH) {
                            axis_active[0] = true;
                            event.type = SDL_KEYDOWN;
                            event.key.keysym.scancode = SDL_SCANCODE_RIGHT;
                        }
                        else if (!axis_active[0] && event.jaxis.value < -JOYSTICK_HIGH) {
                            axis_active[0] = true;
                            event.type = SDL_KEYDOWN;
                            event.key.keysym.scancode = SDL_SCANCODE_LEFT;
                            
                        }
                        else if (axis_active[0] && event.jaxis.value < JOYSTICK_LOW && event.jaxis.value > -JOYSTICK_LOW) {
                            axis_active[0] = false;
                        }
                    }
                    else if (axis == JOYPAD_AXISES_Y) {
                        if (!axis_active[1] && event.jaxis.value > JOYSTICK_HIGH) {
                            axis_active[1] = true;
                            event.type = SDL_KEYDOWN;
                            event.key.keysym.scancode = SDL_SCANCODE_DOWN;
                        }
                        else if (!axis_active[1] && event.jaxis.value < -JOYSTICK_HIGH) {
                            axis_active[1] = true;
                            event.type = SDL_KEYDOWN;
                            event.key.keysym.scancode = SDL_SCANCODE_UP;
                        }
                        else if (axis_active[1] && event.jaxis.value < JOYSTICK_LOW && event.jaxis.value > -JOYSTICK_LOW) {
                            axis_active[1] = false;
                        }
                    }
                }
            }
        }
        switch (event.type) {
            case SDL_QUIT: {
                if (!is_running) {
                    exit(0);
                }
                else {
                    pending_command = GB_SDL_QUIT_COMMAND;
                    return;
                }
                
            }
            case SDL_WINDOWEVENT: {
                if (event.window.event == SDL_WINDOWEVENT_RESIZED) {
                    update_viewport();
                    render_texture(NULL, NULL);
                }
                break;
            }
            case SDL_DROPFILE: {
                set_filename(event.drop.file, SDL_free);
                pending_command = GB_SDL_NEW_FILE_COMMAND;
                return;
            }
            case SDL_JOYBUTTONDOWN:
            {
                if (gui_state == WAITING_FOR_JBUTTON && joypad_configuration_progress != JOYPAD_BUTTONS_MAX) {
                    should_render = true;
                    configuration.joypad_configuration[joypad_configuration_progress++] = event.jbutton.button;
                }
                break;
            }
                
            case SDL_JOYAXISMOTION: {
                if (gui_state == WAITING_FOR_JBUTTON &&
                    joypad_configuration_progress == JOYPAD_BUTTONS_MAX &&
                    abs(event.jaxis.value) >= 0x4000) {
                    if (joypad_axis_temp == (uint8_t)-1) {
                        joypad_axis_temp = event.jaxis.axis;
                    }
                    else if (joypad_axis_temp != event.jaxis.axis) {
                        if (joypad_axis_temp < event.jaxis.axis) {
                            configuration.joypad_axises[JOYPAD_AXISES_X] = joypad_axis_temp;
                            configuration.joypad_axises[JOYPAD_AXISES_Y] = event.jaxis.axis;
                        }
                        else {
                            configuration.joypad_axises[JOYPAD_AXISES_Y] = joypad_axis_temp;
                            configuration.joypad_axises[JOYPAD_AXISES_X] = event.jaxis.axis;
                        }
                        
                        gui_state = SHOWING_MENU;
                        should_render = true;
                    }
                }
                break;
            }

            case SDL_KEYDOWN:
                if (event.key.keysym.scancode == SDL_SCANCODE_F && event.key.keysym.mod & MODIFIER) {
                    if ((SDL_GetWindowFlags(window) & SDL_WINDOW_FULLSCREEN_DESKTOP) == false) {
                        SDL_SetWindowFullscreen(window, SDL_WINDOW_FULLSCREEN_DESKTOP);
                    }
                    else {
                        SDL_SetWindowFullscreen(window, 0);
                    }
                    update_viewport();
                }
                if (event.key.keysym.scancode == SDL_SCANCODE_O) {
                    if (event.key.keysym.mod & MODIFIER) {
                        char *filename = do_open_rom_dialog();
                        if (filename) {
                            set_filename(filename, free);
                            pending_command = GB_SDL_NEW_FILE_COMMAND;
                            return;
                        }
                    }
                }
                else if (event.key.keysym.scancode == SDL_SCANCODE_RETURN && gui_state == WAITING_FOR_JBUTTON) {
                    should_render = true;
                    if (joypad_configuration_progress != JOYPAD_BUTTONS_MAX) {
                        configuration.joypad_configuration[joypad_configuration_progress] = -1;
                    }
                    else {
                        configuration.joypad_axises[0] = -1;
                        configuration.joypad_axises[1] = -1;
                    }
                    joypad_configuration_progress++;
                    
                    if (joypad_configuration_progress > JOYPAD_BUTTONS_MAX) {
                        gui_state = SHOWING_MENU;
                    }
                }
                else if (event.key.keysym.scancode == SDL_SCANCODE_ESCAPE) {
                    if (is_running) {
                        return;
                    }
                    else {
                        if (gui_state == SHOWING_DROP_MESSAGE) {
                            gui_state = SHOWING_MENU;
                        }
                        else if (gui_state == SHOWING_MENU) {
                            gui_state = SHOWING_DROP_MESSAGE;
                        }
                        current_selection = 0;
                        scroll = 0;
                        current_menu = root_menu;
                        should_render = true;
                    }
                }
                else if (gui_state == SHOWING_MENU) {
                    if (event.key.keysym.scancode == SDL_SCANCODE_DOWN && current_menu[current_selection + 1].string) {
                        current_selection++;
                        should_render = true;
                    }
                    else if (event.key.keysym.scancode == SDL_SCANCODE_UP && current_selection) {
                        current_selection--;
                        should_render = true;
                    }
                    else if (event.key.keysym.scancode == SDL_SCANCODE_RETURN  && !current_menu[current_selection].backwards_handler) {
                        if (current_menu[current_selection].handler) {
                            current_menu[current_selection].handler(current_selection);
                            if (pending_command == GB_SDL_RESET_COMMAND && !is_running) {
                                pending_command = GB_SDL_NO_COMMAND;
                            }
                            if (pending_command) {
                                if (!is_running && pending_command == GB_SDL_QUIT_COMMAND) {
                                    exit(0);
                                }
                                return;
                            }
                            should_render = true;
                        }
                        else {
                            return;
                        }
                    }
                    else if (event.key.keysym.scancode == SDL_SCANCODE_RIGHT && current_menu[current_selection].backwards_handler) {
                        current_menu[current_selection].handler(current_selection);
                        should_render = true;
                    }
                    else if (event.key.keysym.scancode == SDL_SCANCODE_LEFT && current_menu[current_selection].backwards_handler) {
                        current_menu[current_selection].backwards_handler(current_selection);
                        should_render = true;
                    }
                }
                else if (gui_state == SHOWING_HELP) {
                    current_help_page++;
                    if (current_help_page == sizeof(help) / sizeof(help[0])) {
                        gui_state = SHOWING_MENU;
                    }
                    should_render = true;
                }
                else if (gui_state == WAITING_FOR_KEY) {
                    if (current_selection >= 8) {
                        if (current_selection == 8) {
                            configuration.keys[8] = event.key.keysym.scancode;
                        }
                        else {
                            configuration.keys_2[current_selection - 9] = event.key.keysym.scancode;
                        }
                    }
                    else {
                        configuration.keys[current_selection] = event.key.keysym.scancode;
                    }
                    gui_state = SHOWING_MENU;
                    should_render = true;
                }
                break;
        }
        
        if (should_render) {
            should_render = false;
            rerender:
            if (width == 160 && height == 144) {
                memcpy(pixels, converted_background->pixels, sizeof(pixels));
            }
            else {
                for (unsigned y = 0; y < 144; y++) {
                    memcpy(pixels + x_offset + width * (y + y_offset), ((uint32_t *)converted_background->pixels) + 160 * y, 160 * 4);
                }
            }
            
            switch (gui_state) {
                case SHOWING_DROP_MESSAGE:
                    draw_text_centered(pixels, width, height, 8 + y_offset, "Press ESC for menu", gui_palette_native[3], gui_palette_native[0], false);
                    draw_text_centered(pixels, width, height, 116 + y_offset, "Drop a GB or GBC", gui_palette_native[3], gui_palette_native[0], false);
                    draw_text_centered(pixels, width, height, 128 + y_offset, "file to play", gui_palette_native[3], gui_palette_native[0], false);
                    break;
                case SHOWING_MENU:
                    draw_text_centered(pixels, width, height, 8 + y_offset, "SameBoy", gui_palette_native[3], gui_palette_native[0], false);
                    unsigned i = 0, y = 24;
                    for (const struct menu_item *item = current_menu; item->string; item++, i++) {
                        if (i == current_selection) {
                            if (y < scroll) {
                                scroll = y - 4;
                                goto rerender;
                            }
                        }
                        if (i == current_selection && i == 0 && scroll != 0) {
                            scroll = 0;
                            goto rerender;
                        }
                        if (item->value_getter && !item->backwards_handler) {
                            char line[25];
                            snprintf(line, sizeof(line), "%s%*s", item->string, 24 - (unsigned)strlen(item->string), item->value_getter(i));
                            draw_text_centered(pixels, width, height, y + y_offset, line, gui_palette_native[3], gui_palette_native[0],
                                               i == current_selection ? DECORATION_SELECTION : DECORATION_NONE);
                            y += 12;
                            
                        }
                        else {
                            draw_text_centered(pixels, width, height, y + y_offset, item->string, gui_palette_native[3], gui_palette_native[0],
                                               i == current_selection && !item->value_getter ? DECORATION_SELECTION : DECORATION_NONE);
                            y += 12;
                            if (item->value_getter) {
                                draw_text_centered(pixels, width, height, y + y_offset, item->value_getter(i), gui_palette_native[3], gui_palette_native[0],
                                                   i == current_selection ? DECORATION_ARROWS : DECORATION_NONE);
                                y += 12;
                            }
                        }
                        if (i == current_selection) {
                            if (y > scroll + 144) {
                                scroll = y - 144;
                                goto rerender;
                            }
                        }

                    }
                    break;
                case SHOWING_HELP:
                    draw_text(pixels, width, height, 2 + x_offset, 2 + y_offset, help[current_help_page], gui_palette_native[3], gui_palette_native[0]);
                    break;
                case WAITING_FOR_KEY:
                    draw_text_centered(pixels, width, height, 68 + y_offset, "Press a Key", gui_palette_native[3], gui_palette_native[0], DECORATION_NONE);
                    break;
                case WAITING_FOR_JBUTTON:
                    draw_text_centered(pixels, width, height, 68 + y_offset,
                                       joypad_configuration_progress != JOYPAD_BUTTONS_MAX ? "Press button for" : "Move the Analog Stick",
                                       gui_palette_native[3], gui_palette_native[0], DECORATION_NONE);
                    draw_text_centered(pixels, width, height, 80 + y_offset,
                                      (const char *[])
                                       {
                                           "Right",
                                           "Left",
                                           "Up",
                                           "Down",
                                           "A",
                                           "B",
                                           "Select",
                                           "Start",
                                           "Open Menu",
                                           "Turbo",
                                           "Rewind",
                                           "Slow-Motion",
                                           "",
                                       } [joypad_configuration_progress],
                                       gui_palette_native[3], gui_palette_native[0], DECORATION_NONE);
                    draw_text_centered(pixels, width, height, 104 + y_offset, "Press Enter to skip", gui_palette_native[3], gui_palette_native[0], DECORATION_NONE);
                    break;
            }
            
            render_texture(pixels, NULL);
#ifdef _WIN32
            /* Required for some Windows 10 machines, god knows why */
            render_texture(pixels, NULL);
#endif
        }
    } while (SDL_WaitEvent(&event));
}
