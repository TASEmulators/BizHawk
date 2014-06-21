/*  src/psp/menu.c: PSP menu interface
    Copyright 2009-2010 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include "common.h"

#include "../cs2.h"
#include "../memory.h"
#include "../peripheral.h"
#include "../scsp.h"
#include "../sh2core.h"
#include "../sh2int.h"
#include "../vdp1.h"
#include "../vidsoft.h"
#include "../yabause.h"

#include "config.h"
#include "control.h"
#include "display.h"
#include "filesel.h"
#include "font.h"
#include "gu.h"
#include "menu.h"
#include "misc.h"
#include "osk.h"
#include "psp-sh2.h"
#include "psp-video.h"
#include "sh2.h"
#include "sys.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Button input state from the last frame */
static uint32_t last_buttons;

/* Repeat delay, rate, and status */
#define REPEAT_DELAY  30  // frames
#define REPEAT_RATE    3  // frames
static uint32_t repeat_buttons;  // Button(s) to repeat
static uint8_t repeating;        // Nonzero = repeat started
static uint8_t repeat_timer;     // Counts down from REPEAT_{DELAY,RATE}

/*----------------------------------*/

/* FIXME: This menu stuff is all messy and copy-pastey because I'm too
          lazy to do it properly at the moment.  I hate writing UIs. */

/* Currently selected menu and menu option */

typedef enum MenuIndex_ {
    MENU_MAIN = 0,
    MENU_GENERAL,
    MENU_FILES,
    MENU_BUTTON,
    MENU_VIDEO,
    MENU_RENDER,
    MENU_FRAME_SKIP,
    MENU_ADVANCED,
    MENU_OPTIMIZE,
    MENU_MEDIA_ENGINE,
    MENU_YESNO,  // A yes/no dialog implemented as a "menu"
} MenuIndex;

static uint8_t cur_menu;


typedef enum MainMenuOption_ {
    OPT_MAIN_GENERAL = 0,
    OPT_MAIN_BUTTON,
    OPT_MAIN_VIDEO,
    OPT_MAIN_ADVANCED,
    OPT_MAIN_SAVE,
    OPT_MAIN_RESET,
} MainMenuOption;
#define OPT_MAIN__MAX  OPT_MAIN_RESET

typedef enum GeneralMenuOption_ {
    OPT_GENERAL_START_IN_EMU = 0,
    OPT_GENERAL_FILES,
    OPT_GENERAL_BUP_AUTOSAVE,
    OPT_GENERAL_BUP_SAVE_NOW,
    OPT_GENERAL_BUP_SAVE_AS,
} GeneralMenuOption;
#define OPT_GENERAL__MAX  OPT_GENERAL_BUP_SAVE_AS

typedef enum FilesMenuOption_ {
    OPT_FILES_PATH_BIOS = 0,
    OPT_FILES_PATH_CD,
    OPT_FILES_PATH_BUP,
} FilesMenuOption;
#define OPT_FILES__MAX  OPT_FILES_PATH_BUP

typedef enum ButtonMenuOption_ {
    OPT_BUTTON_A,
    OPT_BUTTON_B,
    OPT_BUTTON_C,
    OPT_BUTTON_X,
    OPT_BUTTON_Y,
    OPT_BUTTON_Z,
} ButtonMenuOption;
#define OPT_BUTTON__MAX  OPT_BUTTON_Z

typedef enum VideoMenuOption_ {
    OPT_VIDEO_HW = 0,
    OPT_VIDEO_SW,
    OPT_VIDEO_RENDER,
    OPT_VIDEO_FRAME_SKIP,
    OPT_VIDEO_SHOW_FPS,
} VideoMenuOption;
#define OPT_VIDEO__MAX  OPT_VIDEO_SHOW_FPS

typedef enum RenderMenuOption_ {
    OPT_RENDER_CACHE_TEXTURES = 0,
    OPT_RENDER_SMOOTH_TEXTURES,
    OPT_RENDER_SMOOTH_HIRES,
    OPT_RENDER_ENABLE_ROTATE,
    OPT_RENDER_OPTIMIZE_ROTATE,
} RenderMenuOption;
#define OPT_RENDER__MAX  OPT_RENDER_OPTIMIZE_ROTATE

typedef enum FrameSkipMenuOption_ {
    OPT_FRAME_SKIP_AUTO = 0,
    OPT_FRAME_SKIP_NUM,
    OPT_FRAME_SKIP_INTERLACE,
    OPT_FRAME_SKIP_ROTATE,
} FrameSkipMenuOption;
#define OPT_FRAME_SKIP__MAX  OPT_FRAME_SKIP_ROTATE

typedef enum AdvancedMenuOption_ {
    OPT_ADVANCED_SH2_RECOMPILER = 0,
    OPT_ADVANCED_SH2_OPTIMIZE,
    OPT_ADVANCED_MEDIA_ENGINE,
    OPT_ADVANCED_DECILINE_MODE,
    OPT_ADVANCED_AUDIO_SYNC,
    OPT_ADVANCED_CLOCK_SYNC,
    OPT_ADVANCED_CLOCK_FIXED_TIME,
} AdvancedMenuOption;
#define OPT_ADVANCED__MAX  OPT_ADVANCED_CLOCK_FIXED_TIME

typedef enum OptimizeMenuOption_ {
    OPT_OPTIMIZE_ASSUME_SAFE_DIVISION = 0,
    OPT_OPTIMIZE_FOLD_SUBROUTINES,
    OPT_OPTIMIZE_BRANCH_TO_RTS,
    OPT_OPTIMIZE_LOCAL_ACCESSES,
    OPT_OPTIMIZE_POINTERS,
    OPT_OPTIMIZE_POINTERS_MAC,
    OPT_OPTIMIZE_LOCAL_POINTERS,
    OPT_OPTIMIZE_STACK,
#if 0  // FIXME: out of space on the screen; this should be the least dangerous
    OPT_OPTIMIZE_MAC_NOSAT,
#endif
} OptimizeMenuOption;
#define OPT_OPTIMIZE__MAX  OPT_OPTIMIZE_STACK

typedef enum MediaEngineMenuOption_ {
    OPT_MEDIA_ENGINE_USE_ME = 0,
    OPT_MEDIA_ENGINE_WRITEBACK_PERIOD,
    OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY,
} MediaEngineMenuOption;
#define OPT_MEDIA_ENGINE__MAX  OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY

typedef enum YesNoMenuOption_ {
    OPT_YESNO_YES = 0,
    OPT_YESNO_NO,
} YesNoMenuOption;
#define OPT_YESNO__MAX  OPT_YESNO_NO

static uint8_t cur_option;


/* Maximum menu option index for current menu (OPT_*__MAX) */
static uint8_t max_option;

/* File selector, if a file selector is currently open */
static FileSelector *filesel;

/*----------------------------------*/

/* Previous menu and option (and max_option value) for the yes/no dialog */
static uint8_t yesno_menu, yesno_option, yesno_maxopt;

/* Prompt string for the yes/no dialog (may include newlines) */
static char yesno_prompt[1000];

/*----------------------------------*/

/* Saved pathname for "Save backup RAM as..." option */
static char *save_as_path;

/*----------------------------------*/

/* Flag: Is X the confirm button? */
static int x_is_confirm;

/*----------------------------------*/


/* Background image buffer */
static void *bgimage;
static void *bgimage_base;  // Base (unaligned) pointer for later free()ing

/* Background color for the menu, overlaid on the current emulation screen
 * (0xAABBGGRR) */
#define MENU_BGCOLOR    0xC0332C2C

/* Flag indicating whether we should draw the background image (cleared
 * when the user requests an emulator reset, to show visually that the
 * emulator has in fact been reset) */
static uint8_t draw_bgimage;

/* Cursor color, flashing period, and timer */
#define CURSOR_COLOR    0x80FFECEC
#define CURSOR_PERIOD   60  // frames
static uint8_t cursor_timer;

/* Default text color */
#define TEXT_COLOR      0xFFFFECEC

/* Text color for informational messages */
#define TEXT_COLOR_INFO 0xFFFF8040

/* Text color for "Saved"/"Failed" responses to "Save settings" */
#define TEXT_COLOR_OK   0xFF55FF40  // Also used for "Reset the emulator"
#define TEXT_COLOR_NG   0xFF5540FF  // Also used for warning text

/* Text color for disabled menu options */
#define TEXT_COLOR_DISABLED  0xFF807676

/* Status line current text, text color, display time, and timer */
static const char *status_text;
static uint32_t status_color;
#define STATUS_DISPTIME  300  // frames
static uint16_t status_timer;

/*************************************************************************/

/* Local function declarations */

static void gen_menu_bg(void);

static void do_reset(void);

static uint32_t get_new_buttons(const uint32_t buttons);
static void process_input_menu(const uint32_t buttons,
                               const uint32_t new_buttons);
static void process_option_main(const uint32_t buttons);
static void process_option_general(const uint32_t buttons);
static void process_option_files(const uint32_t buttons);
static void process_option_button(const uint32_t buttons);
static void process_option_video(const uint32_t buttons);
static void process_option_render(const uint32_t buttons);
static void process_option_frame_skip(const uint32_t buttons);
static void process_option_advanced(const uint32_t buttons);
static void process_option_optimize(const uint32_t buttons);
static void process_option_media_engine(const uint32_t buttons);
static void process_option_yesno(const uint32_t buttons);
static void process_input_filesel(const uint32_t new_buttons);
static void process_osk_result(void);

static void draw_menu(void);
static const char *cur_option_confirm_text(void);
static void draw_menu_option(int option, int x, int y, const char *format, ...)
    __attribute__((format(printf,4,5)));
static void draw_disabled_menu_option(int option, int x, int y,
                                      const char *format, ...)
    __attribute__((format(printf,4,5)));

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * menu_open:  Open the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void menu_open(void)
{
    last_buttons = control_state();
    repeat_buttons = 0;

    cur_menu = MENU_MAIN;
    cur_option = 0;
    max_option = OPT_MAIN__MAX;
    filesel = NULL;

    int res = sceUtilityGetSystemParamInt(PSP_SYSTEMPARAM_ID_INT_X_IS_CONFIRM,
                                          &x_is_confirm);
    if (res < 0) {
        DMSG("Failed to get X_IS_CONFIRM: %s", psp_strerror(res));
        x_is_confirm = 0;  // Default to O
    }
    draw_bgimage = 1;
    cursor_timer = 0;

    display_set_size(DISPLAY_WIDTH, DISPLAY_HEIGHT);
    gen_menu_bg();
}

/*************************************************************************/

/**
 * menu_run:  Perform a single frame's processing for the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void menu_run(void)
{
    const uint32_t buttons = control_state();

    /* Update timers */
    cursor_timer = (cursor_timer + 1) % CURSOR_PERIOD;
    if (status_timer > 0) {
        status_timer--;
    }

    /* Update the on-screen keyboard in case it's active */
    osk_update();

    /* Check for and process input */
    const uint32_t new_buttons = get_new_buttons(buttons);
    if (osk_status()) {
        process_osk_result();
    } else {
        if (filesel) {
            process_input_filesel(new_buttons);
        } else {
            process_input_menu(buttons, new_buttons);
        }
    }

    /* Draw the menu (and dim the display if the OSK is active) */
    display_begin_frame();
    draw_menu();
    if (osk_status()) {
        display_fill_box(0, 0, DISPLAY_WIDTH-1, DISPLAY_HEIGHT-1, 0xAA000000);
    }
    display_end_frame();
    sceDisplayWaitVblankStart();
}

/*************************************************************************/

/**
 * menu_close:  Close the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void menu_close(void)
{
    /* If the on-screen keyboard is active, close it */

    if (osk_status()) {
        osk_close();
        while (osk_status()) {
            osk_update();
            sceDisplayWaitVblankStart();
        }
    }

    /* If there's a file selector running, kill it */

    if (filesel) {
        filesel_destroy(filesel);
        filesel = NULL;
    }

    /* Make sure to clear both display buffers */

    display_begin_frame();
    if (draw_bgimage) {
        guCopyImage(GU_PSM_8888, 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT,
                    DISPLAY_STRIDE, bgimage,
                    0, 0, DISPLAY_STRIDE, display_work_buffer());
    } else {
        display_fill_box(0, 0, DISPLAY_WIDTH-1, DISPLAY_HEIGHT-1, 0xFF000000);
    }
    display_end_frame();
    sceDisplayWaitVblankStart();

    display_begin_frame();
    if (draw_bgimage) {
        guCopyImage(GU_PSM_8888, 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT,
                    DISPLAY_STRIDE, bgimage,
                    0, 0, DISPLAY_STRIDE, display_work_buffer());
    } else {
        display_fill_box(0, 0, DISPLAY_WIDTH-1, DISPLAY_HEIGHT-1, 0xFF000000);
    }
    display_end_frame();
    sceDisplayWaitVblankStart();

    /* Free the background image buffer */

    free(bgimage_base);
    bgimage = bgimage_base = NULL;

    /* Clear the status line so any current message doesn't get held over */

    status_text = "";
    status_timer = 0;
}

/*************************************************************************/

/**
 * menu_set_error:  Set an error message to be displayed on the menu
 * screen.  If message is NULL, any message currently displayed is cleared.
 *
 * [Parameters]
 *     message: Message text (NULL to clear current message)
 * [Return value]
 *     None
 */
void menu_set_error(const char *message)
{
    static char buf[100];  // Just in case the message is in a local buffer

    if (message) {
        snprintf(buf, sizeof(buf), "%s", message);
        status_text = buf;
        status_color = TEXT_COLOR_NG;
        status_timer = STATUS_DISPTIME;
    } else {
        status_text = NULL;
        status_timer = 0;
    }
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * gen_menu_bg:  Generate the background image for the menu (a copy of the
 * currently-displayed image).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void gen_menu_bg(void)
{
    bgimage_base = malloc((DISPLAY_STRIDE * DISPLAY_HEIGHT * 4) + 63);
    if (UNLIKELY(!bgimage_base)) {
        DMSG("Out of memory for bgimage");
        bgimage = NULL;
        return;
    }
    bgimage = (void *)(((uintptr_t)bgimage_base + 63) & -64);
    memcpy(bgimage, display_disp_buffer(),
           DISPLAY_STRIDE * DISPLAY_HEIGHT * 4);
}

/*************************************************************************/

/**
 * do_reset:  Reset the emulator in response to a user action.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void do_reset(void)
{
    YabauseReset();
    status_text = "Resetting the emulator.";
    status_color = TEXT_COLOR_OK;
    status_timer = STATUS_DISPTIME;
    draw_bgimage = 0;
}

/*************************************************************************/

/**
 * get_new_buttons:  Return which controller buttons were pressed this
 * frame.  This may include fake button presses generated by auto-repeat.
 *
 * If the on-screen keyboard is open, this function always returns zero;
 * however, it should still be called every frame to ensure proper
 * behavior when the OSK is closed.
 *
 * [Parameters]
 *     buttons: Buttons which are currently held down (PSP_CTRL_* bitmask)
 * [Return value]
 *     Buttons which were newly pressed this frame (PSP_CTRL_* bitmask)
 */
static uint32_t get_new_buttons(const uint32_t buttons)
{
    uint32_t new_buttons = ~last_buttons & buttons;

    if (osk_status()) {
        new_buttons = 0;
        repeat_buttons = 0;
    }

    if (new_buttons) {
        /* Only allow repeat of up/down/left/right */
        repeat_buttons = new_buttons & (PSP_CTRL_UP | PSP_CTRL_DOWN
                                        | PSP_CTRL_LEFT | PSP_CTRL_RIGHT);
        repeating = 0;
        repeat_timer = REPEAT_DELAY;
    } else if ((buttons & repeat_buttons) != repeat_buttons) {
        repeat_buttons = 0;
    }
    if (repeat_buttons != 0) {
        repeat_timer--;
        if (repeat_timer == 0) {
            new_buttons = repeat_buttons;
            repeating = 1;
            repeat_timer = REPEAT_RATE;
        }
    }

    last_buttons = buttons;
    return new_buttons;
}

/*-----------------------------------------------------------------------*/

/**
 * process_input_menu:  Process input directed to the menu (i.e. not a file
 * selector.
 *
 * [Parameters]
 *         buttons: Buttons which are currently held down (PSP_CTRL_* bitmask)
 *     new_buttons: Buttons which were pressed this frame (PSP_CTRL_* bitmask)
 * [Return value]
 *     None
 */
static void process_input_menu(const uint32_t buttons,
                               const uint32_t new_buttons)
{
    const uint32_t confirm_button =
        x_is_confirm ? PSP_CTRL_CROSS : PSP_CTRL_CIRCLE;
    const uint32_t cancel_button =
        x_is_confirm ? PSP_CTRL_CIRCLE : PSP_CTRL_CROSS;

    if (new_buttons & PSP_CTRL_UP) {
        if (cur_menu != MENU_YESNO && cur_option > 0) {
            cur_option--;
            cursor_timer = 0;
        }

    } else if (new_buttons & PSP_CTRL_DOWN) {
        if (cur_menu != MENU_YESNO && cur_option < max_option) {
            cur_option++;
            cursor_timer = 0;
        }

    } else if (new_buttons & (PSP_CTRL_LEFT | PSP_CTRL_RIGHT)) {
        const int dir = (new_buttons & PSP_CTRL_RIGHT) ? 1 : -1;

        if (cur_menu == MENU_FRAME_SKIP
         && cur_option == OPT_FRAME_SKIP_NUM
        ) {
            const int new_num = config_get_frameskip_num() + dir;
            if (!config_set_frameskip_num(new_num)) {
                status_text = "Failed to change fixed frame skip count!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            }

        } else if (cur_menu == MENU_MEDIA_ENGINE
                && cur_option == OPT_MEDIA_ENGINE_WRITEBACK_PERIOD
        ) {
            if (me_available && config_get_use_me()) {
                const unsigned int period = config_get_me_writeback_period();
                unsigned int new_period = (dir > 0) ? period<<1 : period>>1;
                if (new_period < 1) {
                    new_period = 1;
                } else if (new_period > 64) {  // Should be more than enough
                    new_period = 64;
                }
                if (!config_set_me_writeback_period(new_period)) {
                    status_text = "Failed to change ME writeback frequency!";
                    status_color = TEXT_COLOR_NG;
                    status_timer = STATUS_DISPTIME;
                }
            }

        } else if (cur_menu == MENU_MEDIA_ENGINE
                && cur_option == OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY
        ) {
            if (me_available && config_get_use_me()) {
                const unsigned int value = config_get_me_uncached_boundary();
                unsigned int new_value = (dir > 0)
                                       ? (value==0 ? 0x400 : value<<1)
                                       : value>>1;
                if (new_value < 0x400) {  // Shouldn't need to deal with
                    new_value = 0;        // fractions of a kilobyte
                } else if (new_value > 0x80000) {
                    new_value = 0x80000;
                }
                if (!config_set_me_uncached_boundary(new_value)) {
                    status_text = "Failed to change uncached boundary address!";
                    status_color = TEXT_COLOR_NG;
                    status_timer = STATUS_DISPTIME;
                }
            }

        } else if (cur_menu == MENU_YESNO) {
            if (dir < 0) {
                if (cur_option == OPT_YESNO_NO) {
                    cur_option = OPT_YESNO_YES;
                    cursor_timer = 0;
                }
            } else {
                if (cur_option == OPT_YESNO_YES) {
                    cur_option = OPT_YESNO_NO;
                    cursor_timer = 0;
                }
            }
        }

    } else if (new_buttons != 0 && cur_menu == MENU_BUTTON) {
        if (new_buttons & (PSP_CTRL_CIRCLE | PSP_CTRL_CROSS
                           | PSP_CTRL_TRIANGLE | PSP_CTRL_SQUARE)) {
            process_option_button(buttons);
        } else if (new_buttons & PSP_CTRL_START) {
            cur_menu = MENU_MAIN;
            cur_option = OPT_MAIN_BUTTON;
            max_option = OPT_MAIN__MAX;
        }

    } else if (new_buttons & confirm_button) {
        switch ((MenuIndex)cur_menu) {
            case MENU_MAIN:        process_option_main(buttons);         break;
            case MENU_GENERAL:     process_option_general(buttons);      break;
            case MENU_FILES:       process_option_files(buttons);        break;
            case MENU_BUTTON:      /* impossible (handled above) */      break;
            case MENU_VIDEO:       process_option_video(buttons);        break;
            case MENU_RENDER:      process_option_render(buttons);       break;
            case MENU_FRAME_SKIP:  process_option_frame_skip(buttons);   break;
            case MENU_ADVANCED:    process_option_advanced(buttons);     break;
            case MENU_OPTIMIZE:    process_option_optimize(buttons);     break;
            case MENU_MEDIA_ENGINE:process_option_media_engine(buttons); break;
            case MENU_YESNO:       process_option_yesno(buttons);        break;
        }

    } else if (new_buttons & cancel_button) {
        if (cur_menu != MENU_MAIN) {
            switch ((MenuIndex)cur_menu) {
              case MENU_MAIN:
              case MENU_BUTTON:
                /* Impossible, but included to avoid a compiler warning */
                break;
              case MENU_GENERAL:
                cur_menu = MENU_MAIN;
                cur_option = OPT_MAIN_GENERAL;
                max_option = OPT_MAIN__MAX;
                break;
              case MENU_FILES:
                cur_menu = MENU_GENERAL;
                cur_option = OPT_GENERAL_FILES;
                max_option = OPT_GENERAL__MAX;
                break;
              case MENU_VIDEO:
                cur_menu = MENU_MAIN;
                cur_option = OPT_MAIN_VIDEO;
                max_option = OPT_MAIN__MAX;
                break;
              case MENU_RENDER:
                cur_menu = MENU_VIDEO;
                cur_option = OPT_VIDEO_RENDER;
                max_option = OPT_VIDEO__MAX;
                break;
              case MENU_FRAME_SKIP:
                cur_menu = MENU_VIDEO;
                cur_option = OPT_VIDEO_FRAME_SKIP;
                max_option = OPT_VIDEO__MAX;
                break;
              case MENU_ADVANCED:
                cur_menu = MENU_MAIN;
                cur_option = OPT_MAIN_ADVANCED;
                max_option = OPT_MAIN__MAX;
                break;
              case MENU_OPTIMIZE:
                cur_menu = MENU_ADVANCED;
                cur_option = OPT_ADVANCED_SH2_OPTIMIZE;
                max_option = OPT_ADVANCED__MAX;
                break;
              case MENU_MEDIA_ENGINE:
                cur_menu = MENU_ADVANCED;
                cur_option = OPT_ADVANCED_MEDIA_ENGINE;
                max_option = OPT_ADVANCED__MAX;
                break;
              case MENU_YESNO:
                cur_option = OPT_YESNO_NO;
                process_option_yesno(confirm_button);
                break;
            }
        }

    }
}

/*----------------------------------*/

/**
 * process_option_*:  Process a "confirm" button press on the currently
 * selected menu option.  Each menu has its own handler function.
 *
 * [Parameters]
 *     buttons: Buttons which are currently held down (PSP_CTRL_* bitmask)
 * [Return value]
 *     None
 */

static void process_option_main(const uint32_t buttons)
{
    switch ((MainMenuOption)cur_option) {

      case OPT_MAIN_GENERAL:
        cur_menu = MENU_GENERAL;
        cur_option = 0;
        max_option = OPT_GENERAL__MAX;
        break;

      case OPT_MAIN_BUTTON:
        cur_menu = MENU_BUTTON;
        cur_option = 0;
        max_option = OPT_BUTTON__MAX;
        break;

      case OPT_MAIN_VIDEO:
        cur_menu = MENU_VIDEO;
        cur_option = 0;
        max_option = OPT_VIDEO__MAX;
        break;

      case OPT_MAIN_ADVANCED:
        cur_menu = MENU_ADVANCED;
        cur_option = 0;
        max_option = OPT_ADVANCED__MAX;
        break;

      case OPT_MAIN_SAVE:
        if (config_save()) {
            status_text = "Settings saved.";
            status_color = TEXT_COLOR_OK;
        } else {
            status_text = "Failed to save settings!";
            status_color = TEXT_COLOR_NG;
        }
        status_timer = STATUS_DISPTIME;
        break;

      case OPT_MAIN_RESET:
        if (yabause_initted
         && (buttons & PSP_CTRL_LTRIGGER)
         && (buttons & PSP_CTRL_RTRIGGER)
        ) {
            do_reset();
        }

    }
}

/*----------------------------------*/

static void process_option_general(const uint32_t buttons)
{
    switch ((GeneralMenuOption)cur_option) {

      case OPT_GENERAL_START_IN_EMU:
        if (!config_set_start_in_emu(!config_get_start_in_emu())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_GENERAL_FILES:
        cur_menu = MENU_FILES;
        cur_option = 0;
        max_option = OPT_FILES__MAX;
        break;

      case OPT_GENERAL_BUP_AUTOSAVE:
        if (!config_set_bup_autosave(!config_get_bup_autosave())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_GENERAL_BUP_SAVE_NOW: {
        if (!yabause_initted) {
            break;
        }
        const char *path = config_get_path_bup();
        if (!path || !*path) {  // Check this early just in case
            status_text = "No backup RAM file configured!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        } else if (!save_backup_ram()) {
            status_text = "Error saving backup RAM!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        } else {
            status_text = "Backup RAM saved.";
            status_color = TEXT_COLOR_OK;
            status_timer = STATUS_DISPTIME;
        }
        break;
      }  // case OPT_GENERAL_BUP_SAVE_NOW

      case OPT_GENERAL_BUP_SAVE_AS: {
        if (!yabause_initted) {
            break;
        }
        const unsigned int maxlen = 100;  // Reasonable limit
        const char *path = config_get_path_bup();
        if (!path) {
            path = "backup.bin";  // Just in case
        }
        if (!osk_open("Enter new backup RAM filename.", path, maxlen)) {
            status_text = "Unable to open on-screen keyboard!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
            break;
        }
        /* process_osk_result() will take care of handling the result from
         * the OSK and saving the new file. */
        break;
      }  // case OPT_GENERAL_BUP_SAVE_AS

    }
}

/*----------------------------------*/

static void process_option_files(const uint32_t buttons)
{
    switch ((FilesMenuOption)cur_option) {

      case OPT_FILES_PATH_BIOS:
        filesel = filesel_create("Select BIOS image file", progpath);
        if (!filesel) {
            status_text = "Failed to open file selector!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_FILES_PATH_CD:
        filesel = filesel_create("Select CD image file (ISO or CUE)",
                                 progpath);
        if (!filesel) {
            status_text = "Failed to open file selector!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_FILES_PATH_BUP:
        filesel = filesel_create("Select backup RAM image file",
                                 progpath);
        if (!filesel) {
            status_text = "Failed to open file selector!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

    }
}

/*----------------------------------*/

static void process_option_button(const uint32_t buttons)
{
    static const uint8_t perkey_map[] = {
        [CONFIG_BUTTON_A] = PERPAD_A,
        [CONFIG_BUTTON_B] = PERPAD_B,
        [CONFIG_BUTTON_C] = PERPAD_C,
        [CONFIG_BUTTON_X] = PERPAD_X,
        [CONFIG_BUTTON_Y] = PERPAD_Y,
        [CONFIG_BUTTON_Z] = PERPAD_Z,
    };

    if (buttons & (buttons-1)) {
        status_text = "Press only one button at a time.";
        status_color = TEXT_COLOR_NG;
        status_timer = STATUS_DISPTIME;
        return;
    }

    const unsigned int saturn_button =
        CONFIG_BUTTON_A + (cur_option - OPT_BUTTON_A);
    const uint32_t psp_button = buttons;

    /* If this button is currently assigned to something else, swap the
     * buttons around. */
    unsigned int other_button;
    for (other_button = CONFIG_BUTTON_A; other_button <= CONFIG_BUTTON_Z;
         other_button++
    ) {
        if (config_get_button(other_button) == psp_button) {
            break;
        }
    }
    if (other_button <= CONFIG_BUTTON_Z) {
        if (!config_set_button(other_button,config_get_button(saturn_button))){
            status_text = "Failed to reassign button!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
            return;
        }
        PerSetKey(config_get_button(other_button), perkey_map[other_button],
                  padbits);
    }

    if (!config_set_button(saturn_button, psp_button)) {
        status_text = "Failed to assign button!";
        status_color = TEXT_COLOR_NG;
        status_timer = STATUS_DISPTIME;
        return;
    }
    PerSetKey(psp_button, perkey_map[saturn_button], padbits);
}

/*----------------------------------*/

static void process_option_video(const uint32_t buttons)
{
    switch ((VideoMenuOption)cur_option) {

      case OPT_VIDEO_HW:
        if (config_get_module_video() != VIDCORE_PSP) {
            if (VideoChangeCore(VIDCORE_PSP) < 0
             || !config_set_module_video(VIDCORE_PSP)
            ) {
                status_text = "Failed to change video interface!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            }
        }
        break;

      case OPT_VIDEO_SW:
        if (config_get_module_video() != VIDCORE_SOFT) {
            if (VideoChangeCore(VIDCORE_SOFT) < 0
             || !config_set_module_video(VIDCORE_SOFT)
            ) {
                status_text = "Failed to change video interface!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            }
        }
        break;

      case OPT_VIDEO_RENDER:
        cur_menu = MENU_RENDER;
        cur_option = 0;
        max_option = OPT_RENDER__MAX;
        break;

      case OPT_VIDEO_FRAME_SKIP:
        cur_menu = MENU_FRAME_SKIP;
        cur_option = 0;
        max_option = OPT_FRAME_SKIP__MAX;
        break;

      case OPT_VIDEO_SHOW_FPS:
        if (!config_set_show_fps(!config_get_show_fps())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

    }
}

/*----------------------------------*/

static void process_option_render(const uint32_t buttons)
{
    switch ((RenderMenuOption)cur_option) {

      case OPT_RENDER_CACHE_TEXTURES:
        if (!config_set_cache_textures(!config_get_cache_textures())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_RENDER_SMOOTH_TEXTURES:
        if (!config_set_smooth_textures(!config_get_smooth_textures())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_RENDER_SMOOTH_HIRES:
        if (!config_set_smooth_hires(!config_get_smooth_hires())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_RENDER_ENABLE_ROTATE:
        if (!config_set_enable_rotate(!config_get_enable_rotate())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_RENDER_OPTIMIZE_ROTATE:
        if (!config_set_optimize_rotate(!config_get_optimize_rotate())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

    }
}

/*----------------------------------*/

static void process_option_frame_skip(const uint32_t buttons)
{
    switch ((FrameSkipMenuOption)cur_option) {

      case OPT_FRAME_SKIP_AUTO:
        if (!config_set_frameskip_auto(!config_get_frameskip_auto())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_FRAME_SKIP_NUM:
        /* Don't do anything for the confirm button */
        break;

      case OPT_FRAME_SKIP_INTERLACE:
        if (!config_set_frameskip_interlace(!config_get_frameskip_interlace())){
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_FRAME_SKIP_ROTATE:
        if (!config_set_frameskip_rotate(!config_get_frameskip_rotate())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

    }
}

/*----------------------------------*/

static void process_option_advanced(const uint32_t buttons)
{
    switch ((AdvancedMenuOption)cur_option) {

      case OPT_ADVANCED_SH2_RECOMPILER:
        if ((buttons & PSP_CTRL_LTRIGGER) && (buttons & PSP_CTRL_RTRIGGER)) {
            int new_module;
            if (config_get_module_sh2() == SH2CORE_PSP) {
                new_module = SH2CORE_INTERPRETER;
            } else {
                new_module = SH2CORE_PSP;
            }
            if (!config_set_module_sh2(new_module)) {
                status_text = "Failed to change SH-2 core!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            }
            if (yabause_initted) {
                SH2DeInit();
                SH2Init(new_module);
                do_reset();
            }
        }
        break;

      case OPT_ADVANCED_SH2_OPTIMIZE:
        cur_menu = MENU_OPTIMIZE;
        cur_option = 0;
        max_option = OPT_OPTIMIZE__MAX;
        break;

      case OPT_ADVANCED_MEDIA_ENGINE:
        if (me_available) {
            cur_menu = MENU_MEDIA_ENGINE;
            cur_option = 0;
            max_option = OPT_MEDIA_ENGINE__MAX;
        }
        break;

      case OPT_ADVANCED_DECILINE_MODE:
        if (!config_set_deciline_mode(!config_get_deciline_mode())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        YabauseSetDecilineMode(config_get_deciline_mode());
        break;

      case OPT_ADVANCED_AUDIO_SYNC:
        if (!config_set_audio_sync(!config_get_audio_sync())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        ScspSetFrameAccurate(config_get_audio_sync());
        break;

      case OPT_ADVANCED_CLOCK_SYNC:
        if (!config_set_clock_sync(!config_get_clock_sync())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

      case OPT_ADVANCED_CLOCK_FIXED_TIME:
        if (!config_set_clock_fixed_time(!config_get_clock_fixed_time())) {
            status_text = "Failed to change option!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
        }
        break;

    }
}

/*----------------------------------*/

static void process_option_optimize(const uint32_t buttons)
{
    uint32_t optflags = config_get_sh2_optimizations();

    switch ((OptimizeMenuOption)cur_option) {
      case OPT_OPTIMIZE_ASSUME_SAFE_DIVISION:
        optflags ^= SH2_OPTIMIZE_ASSUME_SAFE_DIVISION;
        break;
      case OPT_OPTIMIZE_FOLD_SUBROUTINES:
        optflags ^= SH2_OPTIMIZE_FOLD_SUBROUTINES;
        break;
      case OPT_OPTIMIZE_BRANCH_TO_RTS:
        optflags ^= SH2_OPTIMIZE_BRANCH_TO_RTS;
        break;
      case OPT_OPTIMIZE_LOCAL_ACCESSES:
        optflags ^= SH2_OPTIMIZE_LOCAL_ACCESSES;
        break;
      case OPT_OPTIMIZE_POINTERS:
        optflags ^= SH2_OPTIMIZE_POINTERS;
        break;
      case OPT_OPTIMIZE_POINTERS_MAC:
        optflags ^= SH2_OPTIMIZE_POINTERS_MAC;
        break;
      case OPT_OPTIMIZE_LOCAL_POINTERS:
        optflags ^= SH2_OPTIMIZE_LOCAL_POINTERS;
        break;
      case OPT_OPTIMIZE_STACK:
        optflags ^= SH2_OPTIMIZE_STACK;
        break;
#if 0  // FIXME: out of space on the screen
      case OPT_OPTIMIZE_MAC_NOSAT:
        optflags ^= SH2_OPTIMIZE_MAC_NOSAT;
        break;
#endif
    }

    if (!config_set_sh2_optimizations(optflags)) {
        status_text = "Failed to set optimization flags!";
        status_color = TEXT_COLOR_NG;
        status_timer = STATUS_DISPTIME;
        return;
    }
    sh2_set_optimizations(optflags);
}

/*----------------------------------*/

static void process_option_media_engine(const uint32_t buttons)
{
    switch ((MediaEngineMenuOption)cur_option) {
      case OPT_MEDIA_ENGINE_USE_ME:
        if (me_available) {
            if (!config_set_use_me(!config_get_use_me())) {
                status_text = "Failed to change option!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            }
        }
        break;

      case OPT_MEDIA_ENGINE_WRITEBACK_PERIOD:
      case OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY:
        /* Don't do anything for the confirm button */
        break;
    }
}

/*----------------------------------*/

static void process_option_yesno(const uint32_t buttons)
{
    if (yesno_menu == MENU_GENERAL && yesno_option == OPT_GENERAL_BUP_SAVE_AS) {
        if (cur_option == OPT_YESNO_YES) {
            if (!config_set_path_bup(save_as_path)) {
                status_text = "Failed to store new backup RAM filename!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            } else if (!save_backup_ram()) {
                status_text = "Error saving backup RAM!";
                status_color = TEXT_COLOR_NG;
                status_timer = STATUS_DISPTIME;
            } else {
                static char buf[50];
                snprintf(buf, sizeof(buf), "Backup RAM saved to: %.25s%s",
                         save_as_path, strlen(save_as_path) > 25 ? "..." : "");
                status_text = buf;
                status_color = TEXT_COLOR_OK;
                status_timer = STATUS_DISPTIME;
            }
        }
        free(save_as_path);

    } else {
        DMSG("Invalid previous menu/option: %u/%u", yesno_menu, yesno_option);
    }

    cur_menu = yesno_menu;
    cur_option = yesno_option;
    max_option = yesno_maxopt;
}

/*-----------------------------------------------------------------------*/

/**
 * process_input_filesel:  Process input directed to a file selector.
 *
 * [Parameters]
 *     new_buttons: Buttons which were pressed this frame (PSP_CTRL_* bitmask)
 * [Return value]
 *     None
 */
static void process_input_filesel(const uint32_t new_buttons)
{
    filesel_process(filesel, new_buttons);

    if (filesel_done(filesel)) {

        const char *filename = filesel_selected_file(filesel);

        if (filename) {

            /* We only need to reset if the emulator has already been
             * started */
            int need_reset = yabause_initted;

            /* We can only come here from the "Files" menu, so there's no
             * need to check cur_menu */

            switch ((FilesMenuOption)cur_option) {

              case OPT_FILES_PATH_BIOS:
                if (strcmp(config_get_path_bios(), filename) == 0) {
                    need_reset = 0;
                }
                if (!config_set_path_bios(filename)) {
                    status_text = "Failed to set BIOS image filename!";
                    status_color = TEXT_COLOR_NG;
                    status_timer = STATUS_DISPTIME;
                    need_reset = 0;
                }
                if (yabause_initted) {
                    if (LoadBios(filename) != 0) {
                        status_text = "Failed to load BIOS image!";
                        status_color = TEXT_COLOR_NG;
                        status_timer = STATUS_DISPTIME;
                        need_reset = 0;
                    }
                }
                break;

              case OPT_FILES_PATH_CD:
                if (strcmp(config_get_path_cd(), filename) == 0) {
                    need_reset = 0;
                }
                if (!config_set_path_cd(filename)) {
                    status_text = "Failed to set CD image filename!";
                    status_color = TEXT_COLOR_NG;
                    status_timer = STATUS_DISPTIME;
                    need_reset = 0;
                }
                if (yabause_initted) {
                    /* Unfortunately, Cs2ChangeCDCore() doesn't return an error
                     * if the load fails, so we'll just hope it worked. */
                    Cs2ChangeCDCore(CDCORE_ISO, filename);
                }
                break;

              case OPT_FILES_PATH_BUP:
                if (strcmp(config_get_path_bup(), filename) == 0) {
                    need_reset = 0;
                }
                if (!config_set_path_bup(filename)) {
                    status_text =
                        "Failed to set backup RAM image filename!";
                    status_color = TEXT_COLOR_NG;
                    status_timer = STATUS_DISPTIME;
                    need_reset = 0;
                }
                if (yabause_initted) {
                    if (LoadBackupRam(filename) != 0) {
                        status_text = "Failed to load backup RAM image!";
                        status_color = TEXT_COLOR_NG;
                        status_timer = STATUS_DISPTIME;
                        need_reset = 0;
                    }
                }
                break;

              default:  // impossible
                break;

            }

            if (need_reset) {
                do_reset();
            }

        }  // if (filename)

        filesel_destroy(filesel);
        filesel = NULL;

    }  // if (filesel_done(filesel))
}

/*-----------------------------------------------------------------------*/

/**
 * process_osk_result:  Process the result of input to the on-screen
 * keyboard.  Assumes the OSK is currently active.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void process_osk_result(void)
{
    PRECOND(osk_status(), return);

    /* The OSK is currently only used for OPT_GENERAL_BUP_SAVE_AS, so we
     * don't need to check the current menu or option index. */

    switch (osk_result()) {
      case OSK_RESULT_NONE:
        /* We've already requested a close, so nothing else to do. */
        break;

      case OSK_RESULT_ERROR:
        status_text = "An error occurred during on-screen keyboard input!";
        status_color = TEXT_COLOR_NG;
        status_timer = STATUS_DISPTIME;
        osk_close();
        break;

      case OSK_RESULT_RUNNING:
        break;

      case OSK_RESULT_CANCELLED:
        osk_close();
        break;

      case OSK_RESULT_UNCHANGED:
      case OSK_RESULT_CHANGED: {
        char *path = osk_get_text();
        osk_close();
        if (!path) {
            status_text = "An error occurred during on-screen keyboard input!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
            break;
        } else if (!*path) {
            status_text = "No filename was entered!";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
            free(path);
            break;
        } else if (path[strcspn(path,":/\\")]) {
            status_text = "These characters cannot be used in a filename: / \\ :";
            status_color = TEXT_COLOR_NG;
            status_timer = STATUS_DISPTIME;
            free(path);
            break;
        }

        /* Confirm the action with the user.  Default to "Yes" if the file
         * does not exist, "No" if it already exists. */
        save_as_path = path;
        yesno_menu = cur_menu;
        yesno_option = cur_option;
        yesno_maxopt = max_option;
        cur_menu = MENU_YESNO;
        max_option = OPT_YESNO__MAX;
        FILE *test = fopen(path, "r");
        if (test) {
            fclose(test);
            cur_option = OPT_YESNO_NO;
            snprintf(yesno_prompt, sizeof(yesno_prompt),
                     "The file \"%.25s%s\" already exists!\n"
                     "Do you want to overwrite this file?\n",
                     path, strlen(path) > 25 ? "..." : "");
        } else {
            cur_option = OPT_YESNO_YES;
            snprintf(yesno_prompt, sizeof(yesno_prompt),
                     "Save backup RAM to the file%s\"%.50s%s\"?\n",
                     strlen(path) < 20 ? " " : "\n",
                     path, strlen(path) > 50 ? "..." : "");
        }
        break;
      }
    }
}

/*************************************************************************/

/**
 * draw_menu:  Draw the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void draw_menu(void)
{
    /* Set up basic settings */

    guDisable(GU_TEXTURE_2D);
    guEnable(GU_BLEND);
    guBlendFunc(GU_ADD, GU_SRC_ALPHA, GU_ONE_MINUS_SRC_ALPHA, 0, 0);

    /* Draw the background image, if appropriate, and overlay color */

    if (bgimage && draw_bgimage) {
        guCopyImage(GU_PSM_8888, 0, 0, DISPLAY_WIDTH, DISPLAY_HEIGHT,
                    DISPLAY_STRIDE, bgimage,
                    0, 0, DISPLAY_STRIDE, display_work_buffer());
    }
    display_fill_box(0, 0, DISPLAY_WIDTH-1, DISPLAY_HEIGHT-1, MENU_BGCOLOR);

    /* Draw the upper and lower info bars */

    font_printf(DISPLAY_WIDTH/2, 5, 0, TEXT_COLOR_INFO,
                "Yabause %s", PACKAGE_VERSION);
    display_fill_box(0, 21, DISPLAY_WIDTH - 1, 21, TEXT_COLOR_INFO);
    display_fill_box(0, 22, DISPLAY_WIDTH - 1, 22, 0xFF000000);

    const char *confirm_text =
        (filesel != NULL)
            ? "U/D/L/R: Move cursor    O: Select file    X: Previous menu"
            : cur_option_confirm_text();
    if (x_is_confirm) {
        static char swapbuf[100];
        unsigned int i;
        for (i = 0; i < sizeof(swapbuf)-1 && confirm_text[i]; i++) {
            if (confirm_text[i] == 'O' && confirm_text[i+1] == ':') {
                swapbuf[i] = 'X';
            } else if (confirm_text[i] == 'X' && confirm_text[i+1] == ':') {
                swapbuf[i] = 'O';
            } else {
                swapbuf[i] = confirm_text[i];
            }
        }
        swapbuf[i] = 0;
        confirm_text = swapbuf;
    }
    font_printf(DISPLAY_WIDTH/2, DISPLAY_HEIGHT - 16, 0, TEXT_COLOR_INFO,
                "Select: Exit menu    %s", confirm_text);
    display_fill_box(0, DISPLAY_HEIGHT - 22,
                     DISPLAY_WIDTH - 1, DISPLAY_HEIGHT - 22, TEXT_COLOR_INFO);
    display_fill_box(0, DISPLAY_HEIGHT - 21,
                     DISPLAY_WIDTH - 1, DISPLAY_HEIGHT - 21, 0xFF000000);
    if (status_timer > 0) {
        const float alpha = status_timer >= STATUS_DISPTIME/2 ? 1.0f :
            (status_timer / (float)(STATUS_DISPTIME/2));
        const uint32_t alpha_byte =
            floorf((status_color>>24 & 0xFF) * alpha + 0.5f);
        font_printf(DISPLAY_WIDTH/2, DISPLAY_HEIGHT - 27 - FONT_HEIGHT, 0,
                    alpha_byte<<24 | (status_color & 0x00FFFFFF),
                    "%s", status_text);
    }

    /* Draw the menu options and cursor */

    const int menu_left_edge = 130;
    const int line_height = FONT_HEIGHT*5/4;
    const int menu_title_y = (DISPLAY_HEIGHT/2 - line_height/2)
                           - (7*line_height + FONT_HEIGHT) / 2 - 2*line_height;
    const int menu_help_y = (DISPLAY_HEIGHT/2 - line_height/2)
                          - (7*line_height + FONT_HEIGHT) / 2 + 7*line_height;
    const int menu_center_y = (menu_title_y + menu_help_y) / 2 + FONT_HEIGHT/2;
    int x, y;

    switch ((MenuIndex)cur_menu) {

      case MENU_MAIN:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Yabause Main Menu");
        y = menu_center_y - (5*line_height + line_height/2 + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_MAIN_GENERAL, menu_left_edge, y,
                         "Configure general options...");
        y += line_height;
        draw_menu_option(OPT_MAIN_BUTTON, menu_left_edge, y,
                         "Configure controller buttons...");
        y += line_height;
        draw_menu_option(OPT_MAIN_VIDEO, menu_left_edge, y,
                         "Configure video options...");
        y += line_height;
        draw_menu_option(OPT_MAIN_ADVANCED, menu_left_edge, y,
                         "Configure advanced settings...");
        y += line_height*3/2;
        draw_menu_option(OPT_MAIN_SAVE, menu_left_edge, y, "Save settings");
        y += line_height;
        if (yabause_initted) {
            draw_menu_option(OPT_MAIN_RESET, menu_left_edge, y,
                             "Reset emulator");
        } else {
            draw_disabled_menu_option(OPT_MAIN_RESET, menu_left_edge, y,
                                      "Reset emulator");
        }
        y = menu_help_y;
        switch ((MainMenuOption)cur_option) {
          case OPT_MAIN_GENERAL:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Select data files"
                        " for use with the emulator, or change");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "other general"
                        " settings.");
            y += line_height;
            break;
          case OPT_MAIN_BUTTON:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Choose which PSP"
                        " controls to use for the Saturn");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "controller buttons.");
            y += line_height;
            break;
          case OPT_MAIN_VIDEO:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Choose between"
                        " hardware and software video rendering,");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "or configure display"
                        " settings.");
            y += line_height;
            break;
          case OPT_MAIN_ADVANCED:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Configure advanced"
                        " emulation options.");
            y += line_height;
            break;
          case OPT_MAIN_SAVE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Save the current"
                        " settings, so Yabause will use them");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "automatically the"
                        " next time you start it up.");
            y += line_height;
            break;
          case OPT_MAIN_RESET:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Reset the emulator,"
                        " as though you had pressed the");
            y += line_height;
            x = font_printf(75, y, -1, TEXT_COLOR_INFO, "Saturn's RESET"
                            " button.  ");
            font_printf(x, y, -1, TEXT_COLOR_NG, "Hold the L and R buttons");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "while selecting this"
                        " option.");
            break;
        }
        break;

      case MENU_GENERAL:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure general options");
        y = menu_center_y - (5*line_height + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_GENERAL_START_IN_EMU, menu_left_edge, y,
                         "[%c] Start emulator immediately",
                         config_get_start_in_emu() ? '*' : ' ');
        y += line_height*3/2;
        draw_menu_option(OPT_GENERAL_FILES, menu_left_edge, y,
                         "    Select BIOS/CD/backup files...");
        y += line_height*3/2;
        draw_menu_option(OPT_GENERAL_BUP_AUTOSAVE, menu_left_edge, y,
                         "[%c] Auto-save backup RAM",
                         config_get_bup_autosave() ? '*' : ' ');
        y += line_height;
        if (yabause_initted) {
            draw_menu_option(OPT_GENERAL_BUP_SAVE_NOW, menu_left_edge, y,
                             "    Save backup RAM now");
        } else {
            draw_disabled_menu_option(OPT_GENERAL_BUP_SAVE_NOW, menu_left_edge,
                                      y, "    Save backup RAM now");
        }
        y += line_height;
        if (yabause_initted) {
            draw_menu_option(OPT_GENERAL_BUP_SAVE_AS, menu_left_edge, y,
                             "    Save backup RAM as...");
        } else {
            draw_disabled_menu_option(OPT_GENERAL_BUP_SAVE_AS, menu_left_edge,
                                      y, "    Save backup RAM as...");
        }
        y = menu_help_y;
        switch ((GeneralMenuOption)cur_option) {
          case OPT_GENERAL_START_IN_EMU:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Start the Saturn"
                        " emulation immediately when you start");
            y += line_height;
            x = font_printf(75, y, -1, TEXT_COLOR_INFO, "Yabause, rather"
                        " than displaying this menu.  ");
            font_printf(x, y, -1, TEXT_COLOR_NG, "Remember");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "to \"Save settings\""
                        " after changing this option!");
            y += line_height;
            break;
          case OPT_GENERAL_FILES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Select the files"
                        " containing the BIOS image, CD image,");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "and backup data you"
                        " want to use.");
            y += line_height;
            break;
          case OPT_GENERAL_BUP_AUTOSAVE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Save the contents of"
                        " backup RAM whenever you save your");
            y += line_height;
            x = font_printf(75, y, -1, TEXT_COLOR_INFO, "game in the"
                            " emulator.  ");
            font_printf(x, y, -1, TEXT_COLOR_NG, "Even if this option is"
                        " enabled,");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "backup RAM is NOT saved"
                        " when you quit Yabause.");
            y += line_height;
            break;
          case OPT_GENERAL_BUP_SAVE_NOW:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Save the current"
                        " contents of backup RAM to your");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Memory Stick.");
            y += line_height;
            break;
          case OPT_GENERAL_BUP_SAVE_AS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Save the current"
                        " contents of backup RAM in a new file");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "on your Memory Stick."
                        "  The new file will be used for");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "automatic and manual"
                        " saves until you quit Yabause.");
            y += line_height;
            break;
        }
        break;

      case MENU_FILES:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Select BIOS/CD/backup files");
        y = menu_center_y - (2*line_height + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_FILES_PATH_BIOS, menu_left_edge, y,
                         "Select BIOS image (%s)", config_get_path_bios());
        y += line_height;
        draw_menu_option(OPT_FILES_PATH_CD, menu_left_edge, y,
                         "Select CD image (%s)", config_get_path_cd());
        y += line_height;
        draw_menu_option(OPT_FILES_PATH_BUP, menu_left_edge, y,
                         "Select backup RAM file (%s)", config_get_path_bup());
        y = menu_help_y;
        switch ((FilesMenuOption)cur_option) {
          case OPT_FILES_PATH_BIOS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Select the file"
                        " containing the Saturn BIOS image.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "Changing this file"
                        " will reset the emulator.");
            y += line_height;
            break;
          case OPT_FILES_PATH_CD:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Select the file"
                        " containing the CD image you want to");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "use.  This can be either"
                        " an ISO file or a CUE file.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "Changing this file"
                        " will reset the emulator.");
            y += line_height;
            break;
          case OPT_FILES_PATH_BUP:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Select the file"
                        " containing your backup RAM data.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "Changing this file"
                        " will reset the emulator.");
            y += line_height;
            break;
        }
        break;

      case MENU_BUTTON: {
        static const char saturn_buttons[] = {
            [OPT_BUTTON_A] = 'A',
            [OPT_BUTTON_B] = 'B',
            [OPT_BUTTON_C] = 'C',
            [OPT_BUTTON_X] = 'X',
            [OPT_BUTTON_Y] = 'Y',
            [OPT_BUTTON_Z] = 'Z',
        };
        auto inline const char *psp_button_name(const uint32_t bitmask);
        auto inline const char *psp_button_name(const uint32_t bitmask) {
            static const char * const psp_button_names[] = {
                [31 - __builtin_clz(PSP_CTRL_CIRCLE  )] = "Circle",
                [31 - __builtin_clz(PSP_CTRL_CROSS   )] = "Cross",
                [31 - __builtin_clz(PSP_CTRL_TRIANGLE)] = "Triangle",
                [31 - __builtin_clz(PSP_CTRL_SQUARE  )] = "Square",
            };
            const unsigned int index = 31 - __builtin_clz(bitmask);
            return (index < lenof(psp_button_names) && psp_button_names[index])
                   ? psp_button_names[index] : "(None)";
        }

        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure controller buttons");
        y = menu_center_y - (5*line_height + FONT_HEIGHT) / 2;
        unsigned int i;
        for (i = OPT_BUTTON_A; i <= OPT_BUTTON_Z; i++) {
            const uint32_t psp_button =
                config_get_button(CONFIG_BUTTON_A + (i - OPT_BUTTON_A));
            draw_menu_option(i, menu_left_edge, y, "%c --- %s",
                             saturn_buttons[i], psp_button_name(psp_button));
            y += line_height;
        }
        y = menu_help_y;
        font_printf(75, y, -1, TEXT_COLOR_INFO, "Press one of the"
                    " Circle/Cross/Triangle/Square buttons");
        y += line_height;
        font_printf(75, y, -1, TEXT_COLOR_INFO, "to assign that PSP button"
                    " to the Saturn controller's");
        y += line_height;
        font_printf(75, y, -1, TEXT_COLOR_INFO, "%c button.  (L, R, and Start"
                    " cannot be reassigned.)", saturn_buttons[cur_option]);
        y += line_height;
        break;
      }  // case MENU_BUTTON

      case MENU_VIDEO:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure video options");
        y = menu_center_y - (4*line_height + line_height/2 + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_VIDEO_HW, menu_left_edge, y,
                         "(%c) Use hardware video renderer",
                         config_get_module_video()==VIDCORE_PSP ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_VIDEO_SW, menu_left_edge, y,
                         "(%c) Use software video renderer",
                         config_get_module_video()==VIDCORE_SOFT ? '*' : ' ');
        y += line_height*3/2;
        draw_menu_option(OPT_VIDEO_RENDER, menu_left_edge, y,
                         "    Configure hardware rendering settings...");
        y += line_height;
        draw_menu_option(OPT_VIDEO_FRAME_SKIP, menu_left_edge, y,
                         "    Configure frame-skip settings...");
        y += line_height;
        draw_menu_option(OPT_VIDEO_SHOW_FPS, menu_left_edge, y,
                         "[%c] Show FPS", config_get_show_fps() ? '*' : ' ');
        y = menu_help_y;
        switch ((VideoMenuOption)cur_option) {
          case OPT_VIDEO_HW:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The hardware video"
                        " renderer is fast, but may not always");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "display accurate"
                        " graphics.");
            y += line_height;
            break;
          case OPT_VIDEO_SW:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The software video"
                        " renderer is slow, but more faithful");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "to the actual Saturn"
                        " display.");
            y += line_height;
            break;
          case OPT_VIDEO_RENDER:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This submenu lets you"
                        " change certain aspects of the");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "hardware video"
                        " renderer's behavior.  The options do not");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "affect the software"
                        " video renderer.");
            y += line_height;
            break;
          case OPT_VIDEO_FRAME_SKIP:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The hardware renderer"
                        " can be configured to skip drawing");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "some frames to speed"
                        " up the emulation.  This submenu lets");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "you choose whether to"
                        " skip frames and how many to skip.");
            y += line_height;
            break;
          case OPT_VIDEO_SHOW_FPS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Display the emulator's"
                        " current speed in frames per second");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "in the upper-right"
                        " corner of the screen.");
            y += line_height;
            break;
        }
        break;

      case MENU_RENDER:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure hardware rendering settings");
        y = menu_center_y - (4*line_height + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_RENDER_CACHE_TEXTURES, menu_left_edge, y,
                         "[%c] Aggressively cache pixel data",
                         config_get_cache_textures() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_RENDER_SMOOTH_TEXTURES, menu_left_edge, y,
                         "[%c] Smooth textures and sprites",
                         config_get_smooth_textures() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_RENDER_SMOOTH_HIRES, menu_left_edge, y,
                         "[%c] Smooth high-resolution graphics",
                         config_get_smooth_hires() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_RENDER_ENABLE_ROTATE, menu_left_edge, y,
                         "[%c] Enable rotated/distorted graphics",
                         config_get_enable_rotate() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_RENDER_OPTIMIZE_ROTATE, menu_left_edge, y,
                         "[%c] Optimize rotated/distorted graphics",
                         config_get_optimize_rotate() ? '*' : ' ');
        y = menu_help_y;
        switch ((RenderMenuOption)cur_option) {
          case OPT_RENDER_CACHE_TEXTURES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Try to cache native"
                        " pixel data to speed up drawing.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This may cause the"
                        " wrong graphics to be shown in");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "rare cases.");
            y += line_height;
            break;
          case OPT_RENDER_SMOOTH_TEXTURES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Apply smoothing"
                        " (antialiasing) to textures and sprites.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This can reduce"
                        " jaggedness in 3-D environments, but may");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "also blur images"
                        " originally intended to look pixelated.");
            y += line_height;
            break;
          case OPT_RENDER_SMOOTH_HIRES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Apply smoothing to"
                        " high-resolution background graphics.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This can make high"
                        "-resolution screens look clearer, but");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "may also slow down"
                        " the emulator.");
            y += line_height;
            break;
          case OPT_RENDER_ENABLE_ROTATE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Rotated or distorted"
                        " background graphics can slow down");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "the emulator"
                        " significantly.  Disable this option to");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "turn them off.");
            y += line_height;
            break;
          case OPT_RENDER_OPTIMIZE_ROTATE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Some types of rotated"
                        " graphics can be drawn quickly,");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "at the expense of"
                        " accuracy.  Disable this option to");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "always use the"
                        " accurate (but slow) drawing method.");
            y += line_height;
            break;
        }
        break;

      case MENU_FRAME_SKIP:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure frame-skip settings");
        y = menu_center_y - (3*line_height + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_FRAME_SKIP_AUTO, menu_left_edge, y,
                         "    Frame-skip mode: [%s]",
                         config_get_frameskip_auto() ? " Auto " : "Manual");
        y += line_height;
        draw_menu_option(OPT_FRAME_SKIP_NUM, menu_left_edge, y,
                         "    Number of frames to skip: [%d]",
                         config_get_frameskip_num());
        y += line_height;
        draw_menu_option(OPT_FRAME_SKIP_INTERLACE, menu_left_edge, y,
                         "[%c] Limit to 30fps for interlaced display",
                         config_get_frameskip_interlace() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_FRAME_SKIP_ROTATE, menu_left_edge, y,
                         "[%c] Halve framerate for rotated backgrounds",
                         config_get_frameskip_rotate() ? '*' : ' ');
        y = menu_help_y;
        switch ((FrameSkipMenuOption)cur_option) {
          case OPT_FRAME_SKIP_AUTO:
            if (config_get_frameskip_auto()) {
                font_printf(75, y, -1, TEXT_COLOR_INFO, "In Auto mode, the"
                            " emulator will automatically choose the");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_INFO, "number of frames to"
                            " skip depending on the emulation speed.");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_NG, "Note: Auto mode is"
                            " not currently implemented.");
                y += line_height;
            } else {
                font_printf(75, y, -1, TEXT_COLOR_INFO, "In Manual mode, the"
                            " emulator will always skip the number");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_INFO, "of frames set in"
                            " this menu.");
                y += line_height;
            }
            break;
          case OPT_FRAME_SKIP_NUM:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The number of frames"
                        " to skip for every frame drawn when");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Manual mode is"
                        " selected.  0 means \"draw every frame\",");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "1 means \"draw every"
                        " second frame\", and so on.");
            y += line_height;
            break;
          case OPT_FRAME_SKIP_INTERLACE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Always skip at least"
                        " one frame when drawing interlaced");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "(high-resolution)"
                        " screens.  Has no effect unless the");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "\"number of frames"
                        " to skip\" is set to zero.");
            y += line_height;
            break;
          case OPT_FRAME_SKIP_ROTATE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Reduce the frame rate"
                        " by half when rotated or distorted");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "background graphics are"
                        " displayed.");
            y += line_height;
            break;
        }
        break;

      case MENU_ADVANCED:
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure advanced emulation options");
        y = menu_center_y - (6*(line_height-2) + 2*((line_height-2)/2) + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_ADVANCED_SH2_RECOMPILER, menu_left_edge, y,
                         "[%c] Use SH-2 recompiler",
                         config_get_module_sh2()==SH2CORE_PSP ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_ADVANCED_SH2_OPTIMIZE, menu_left_edge, y,
                         "    Select SH-2 optimizations...");
        y += (line_height-2)*3/2;
        if (me_available) {
            draw_menu_option(OPT_ADVANCED_MEDIA_ENGINE, menu_left_edge, y,
                             "    Configure Media Engine options...");
        } else {
            draw_disabled_menu_option(OPT_ADVANCED_MEDIA_ENGINE,
                                      menu_left_edge, y,
                                      "    Configure Media Engine options...");
        }
        y += (line_height-2)*3/2;
        draw_menu_option(OPT_ADVANCED_DECILINE_MODE, menu_left_edge, y,
                         "[%c] Use more precise emulation timing",
                         config_get_deciline_mode() ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_ADVANCED_AUDIO_SYNC, menu_left_edge, y,
                         "[%c] Sync audio output to emulation",
                         config_get_audio_sync() ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_ADVANCED_CLOCK_SYNC, menu_left_edge, y,
                         "[%c] Sync Saturn clock to emulation",
                         config_get_clock_sync() ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_ADVANCED_CLOCK_FIXED_TIME, menu_left_edge, y,
                         "[%c] Always start from 1998-01-01 12:00",
                         config_get_clock_fixed_time() ? '*' : ' ');
        y = menu_help_y;
        switch ((AdvancedMenuOption)cur_option) {
          case OPT_ADVANCED_SH2_RECOMPILER:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Use the SH-2 recompiler"
                        " instead of the much slower");
            y += line_height;
            x = font_printf(75, y, -1, TEXT_COLOR_INFO, "interpreter.  ");
            font_printf(x, y, -1, TEXT_COLOR_NG, "Changing this option will"
                        " reset the emulator.");
            y += line_height;
            break;
          case OPT_ADVANCED_SH2_OPTIMIZE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Enable or disable"
                        " specific optimizations used by the");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "SH-2 recompiler.");
            y += line_height;
            break;
          case OPT_ADVANCED_MEDIA_ENGINE:
            if (me_available) {
                font_printf(75, y, -1, TEXT_COLOR_INFO, "Enable or disable"
                            " use of the PSP's Media Engine for");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_INFO, "emulation, and"
                            " configure various parameters used by");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_INFO, "the Media Engine.");
                y += line_height;
            } else {
                font_printf(75, y, -1, TEXT_COLOR_NG, "The Media Engine access"
                            " library (me.prx) was not found,");
                y += line_height;
                font_printf(75, y, -1, TEXT_COLOR_NG, "so the Media Engine"
                            " cannot be used for emulation.");
                y += line_height;
            }
            break;
          case OPT_ADVANCED_DECILINE_MODE:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Increase the precision"
                        " of the emulator's execution timing.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This slows down the"
                        " emulation, but may be required for");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "some games to work"
                        " correctly.");
            y += line_height;
            break;
          case OPT_ADVANCED_AUDIO_SYNC:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Synchronize the"
                        " generated audio data with the emulation.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "This keeps the audio"
                        " and video in sync, but causes audio");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "dropouts when the"
                        " emulator runs slower than real time.");
            y += line_height;
            break;
          case OPT_ADVANCED_CLOCK_SYNC:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Synchronize the"
                        " Saturn's internal clock with the");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "emulation, rather"
                        " than following real time regardless");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "of emulation speed.");
            y += line_height;
            break;
          case OPT_ADVANCED_CLOCK_FIXED_TIME:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Start the Saturn's"
                        " internal clock at 1998-01-01 12:00");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "every time the"
                        " emulator is reset.  Generally only");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "useful for debugging.");
            y += line_height;
            break;
        }
        break;

      case MENU_OPTIMIZE: {
        const uint32_t optflags = config_get_sh2_optimizations();
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Select SH-2 emulation optimizations");
        y = menu_center_y - (7*(line_height-2) + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_OPTIMIZE_ASSUME_SAFE_DIVISION, menu_left_edge, y,
                         "[%c] Assume safe division operations",
                         optflags & SH2_OPTIMIZE_ASSUME_SAFE_DIVISION ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_FOLD_SUBROUTINES, menu_left_edge, y,
                         "[%c] Fold short subroutines into callers",
                         optflags & SH2_OPTIMIZE_FOLD_SUBROUTINES ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_BRANCH_TO_RTS, menu_left_edge, y,
                         "[%c] Optimize branch/return pairs",
                         optflags & SH2_OPTIMIZE_BRANCH_TO_RTS ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_LOCAL_ACCESSES, menu_left_edge, y,
                         "[%c] Optimize accesses to local data",
                         optflags & SH2_OPTIMIZE_LOCAL_ACCESSES ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_POINTERS, menu_left_edge, y,
                         "[%c] Optimize pointer register accesses",
                         optflags & SH2_OPTIMIZE_POINTERS ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_POINTERS_MAC, menu_left_edge, y,
                         "    [%c] Try harder for MAC instructions",
                         optflags & SH2_OPTIMIZE_POINTERS_MAC ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_LOCAL_POINTERS, menu_left_edge, y,
                         "    [%c] Assume consistent local pointers",
                         optflags & SH2_OPTIMIZE_LOCAL_ACCESSES ? '*' : ' ');
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_STACK, menu_left_edge, y,
                         "[%c] Optimize stack accesses",
                         optflags & SH2_OPTIMIZE_STACK ? '*' : ' ');
#if 0  // FIXME: out of space on the screen
        y += line_height-2;
        draw_menu_option(OPT_OPTIMIZE_MAC_NOSAT, menu_left_edge, y,
                         "[%c] Optimize unsaturated multiplication",
                         optflags & SH2_OPTIMIZE_MAC_NOSAT ? '*' : ' ');
#endif
        y = menu_help_y;
        switch ((OptimizeMenuOption)cur_option) {
          case OPT_OPTIMIZE_ASSUME_SAFE_DIVISION:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Speed up division"
                        " operations by assuming that quotients");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "will not overflow"
                        " and division by zero will not occur.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_FOLD_SUBROUTINES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Fold (inline) short,"
                        " simple subroutines into the");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "routines which call"
                        " them.  May cause crashes");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "with games that use"
                        " self-modifying code.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_BRANCH_TO_RTS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Speed up branches"
                        " which target RTS instructions.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_LOCAL_ACCESSES:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Detect and speed up"
                        " accesses to function-local data.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_POINTERS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Detect SH-2 registers"
                        " which are used as data pointers,");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "and speed up memory"
                        " accesses through those registers.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_POINTERS_MAC:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Try harder to optimize"
                        " pointers to MAC (multiply-and-");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "accumulate) instructions."
                        "  Ineffective unless pointer");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "optimizations are also"
                        " enabled.");
            y += line_height;
            break;
          case OPT_OPTIMIZE_LOCAL_POINTERS:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Assume that pointers"
                        " loaded from local data or with a");
            y += line_height-2;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "MOVA instruction will"
                        " always access the same region of");
            y += line_height-2;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "memory.  Ineffective"
                        " unless pointer and local data");
            y += line_height-2;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "optimizations are also"
                        " enabled.");
            y += line_height-2;
            break;
          case OPT_OPTIMIZE_STACK:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Speed up accesses to"
                        " the SH-2 stack.");
            y += line_height;
            break;
#if 0  // FIXME: out of space on the screen
          case OPT_OPTIMIZE_MAC_NOSAT:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Speed up multiply-and"
                        "-accumulate operations which are");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "known not to use"
                        " saturation.");
            y += line_height;
#endif
        }
        break;
      }

      case MENU_MEDIA_ENGINE: {
        font_printf(DISPLAY_WIDTH/2, menu_title_y, 0, TEXT_COLOR_INFO,
                    "Configure Media Engine options");
        y = menu_center_y - (2*line_height + FONT_HEIGHT) / 2;
        draw_menu_option(OPT_MEDIA_ENGINE_USE_ME, menu_left_edge, y,
                         "[%c] Use Media Engine for emulation",
                         config_get_use_me() ? '*' : ' ');
        y += line_height;
        draw_menu_option(OPT_MEDIA_ENGINE_WRITEBACK_PERIOD, menu_left_edge, y,
                         "    Cache writeback frequency: [1/%-2u]",
                         config_get_me_writeback_period());
        y += line_height;
        draw_menu_option(OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY, menu_left_edge, y,
                         "    Sound RAM write-through region: [%3uk]",
                         config_get_me_uncached_boundary() / 1024);
        y = menu_help_y;
        switch ((MediaEngineMenuOption)cur_option) {
          case OPT_MEDIA_ENGINE_USE_ME:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "Use the Media Engine"
                        " CPU for emulation.  This option");
            y += line_height;
            x = font_printf(75, y, -1, TEXT_COLOR_INFO, "is ");
            x = font_printf(x, y, -1, TEXT_COLOR_NG, "EXPERIMENTAL");
            font_printf(x, y, -1, TEXT_COLOR_INFO, "; see the manual"
                        " for details.");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_NG, "You must restart"
                        " Yabause after changing this option.");
            y += line_height;
            break;
          case OPT_MEDIA_ENGINE_WRITEBACK_PERIOD:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The relative"
                        " frequency of data cache synchronization when");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "using the Media"
                        " Engine for emulation.  1/1 is safest;");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "lower frequencies"
                        " may run faster, but may also crash.");
            y += line_height;
            break;
          case OPT_MEDIA_ENGINE_UNCACHED_BOUNDARY:
            font_printf(75, y, -1, TEXT_COLOR_INFO, "The size of the"
                        " region at the beginning of sound RAM");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "which is written"
                        " through the PSP's cache.  Larger values");
            y += line_height;
            font_printf(75, y, -1, TEXT_COLOR_INFO, "are safer but slower;"
                        " 2k should be good for most games.");
            y += line_height;
            break;
        }
        break;
      }

      case MENU_YESNO: {
        int nlines;
        char buf[100];
        const char *s, *eol;
        for (s = yesno_prompt; *s; s = eol+1) {
            eol = s + strcspn(s, "\n");
            nlines++;
        }
        y = DISPLAY_HEIGHT/2 - ((nlines+1) * line_height + FONT_HEIGHT) / 2;
        for (s = yesno_prompt; *s; s = eol+1, y += line_height) {
            eol = s + strcspn(s, "\n");
            snprintf(buf, sizeof(buf), "%.*s", eol - s, s);
            font_printf(DISPLAY_WIDTH/2, y, 0, TEXT_COLOR_INFO, buf);
        }
        y += line_height;
        draw_menu_option(OPT_YESNO_YES, DISPLAY_WIDTH/2 - 45, y, "Yes");
        draw_menu_option(OPT_YESNO_NO,  DISPLAY_WIDTH/2 + 30, y, "No");
      }

    }  // switch (cur_menu)

    /* If a file selector is open, draw it */

    if (filesel) {
        filesel_draw(filesel);
    }
}

/*----------------------------------*/

/**
 * cur_option_confirm_text:  Return the text to be displayed for the
 * "confirm" button help.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     String to display for the "confirm" button help
 */
static const char *cur_option_confirm_text(void)
{
    switch ((MenuIndex)cur_menu) {
      case MENU_MAIN:
        switch ((MainMenuOption)cur_option) {
          case OPT_MAIN_SAVE:
            return "O: Save settings";
          case OPT_MAIN_RESET:
            return yabause_initted ? "L+R+O: Reset emulator" : "";
          default:
            return "O: Enter submenu";
        }
      case MENU_GENERAL:
        switch ((GeneralMenuOption)cur_option) {
          case OPT_GENERAL_FILES:
            return "O: Enter submenu    X: Return to previous menu";
          case OPT_GENERAL_BUP_SAVE_NOW:
          case OPT_GENERAL_BUP_SAVE_AS:
            if (yabause_initted) {
                return "O: Save backup RAM    X: Return to previous menu";
            } else {
                return "X: Return to previous menu";
            }
          default:
            return "O: Toggle on/off    X: Return to previous menu";
        }
      case MENU_FILES:
        return "O: Select file    X: Return to previous menu";
      case MENU_BUTTON:
        return "O/X/Triangle/Square: Assign button    Start: Previous menu";
      case MENU_VIDEO:
        switch ((VideoMenuOption)cur_option) {
          case OPT_VIDEO_RENDER:
          case OPT_VIDEO_FRAME_SKIP:
            return "O: Enter submenu    X: Return to previous menu";
          case OPT_VIDEO_SHOW_FPS:
            return "O: Toggle on/off    X: Return to previous menu";
          default:
            return "O: Select    X: Return to previous menu";
        }
      case MENU_RENDER:
        return "O: Toggle on/off    X: Return to previous menu";
      case MENU_FRAME_SKIP:
        switch ((FrameSkipMenuOption)cur_option) {
          case OPT_FRAME_SKIP_AUTO:
            return "O: Change setting    X: Return to previous menu";
          case OPT_FRAME_SKIP_NUM:
            return "Left/Right: Change setting    X: Return to previous menu";
          default:
            return "O: Toggle on/off    X: Return to previous menu";
        }
      case MENU_ADVANCED:
        switch ((AdvancedMenuOption)cur_option) {
          case OPT_ADVANCED_SH2_RECOMPILER:
            return "L+R+O: Toggle on/off    X: Return to previous menu";
          case OPT_ADVANCED_SH2_OPTIMIZE:
            return "O: Enter submenu    X: Return to previous menu";
          case OPT_ADVANCED_MEDIA_ENGINE:
            if (me_available) {
                return "O: Enter submenu    X: Return to previous menu";
            } else {
                return "X: Return to previous menu";
            }
          default:
            return "O: Toggle on/off    X: Return to previous menu";
        }
      case MENU_OPTIMIZE:
        return "O: Toggle on/off    X: Return to previous menu";
      case MENU_MEDIA_ENGINE:
        switch ((FrameSkipMenuOption)cur_option) {
          case OPT_MEDIA_ENGINE_USE_ME:
            return "O: Change setting    X: Return to previous menu";
          default:
            return "Left/Right: Change setting    X: Return to previous menu";
        }
      case MENU_YESNO:
        return "O: Confirm selection    X: Cancel";
    }
    DMSG("Invalid menu/option %d/%d", cur_menu, cur_option);
    return "O: Confirm";
}

/*----------------------------------*/

/**
 * draw_menu_option:  Draw a single menu option.  If the option is
 * currently selected, also draws the menu cursor.
 *
 * [Parameters]
 *     option: Option ID (OPT_*)
 *       x, y: Text position
 *     format: Format string for option text
 *        ...: Format arguments
 * [Return value]
 *     None
 */
static void draw_menu_option(int option, int x, int y, const char *format, ...)
{
    PRECOND(format != NULL, return);

    char buf[1000];
    va_list args;
    va_start(args, format);
    vsnprintf(buf, sizeof(buf), format, args);
    va_end(args);
    int x2 = font_printf(x, y, -1, TEXT_COLOR, "%s", buf);

    if (cur_option == option) {
        const float cursor_alpha =
            (sinf((cursor_timer / (float)CURSOR_PERIOD) * (float)M_TWOPI) + 1)
            / 2;
        const uint32_t cursor_alpha_byte =
            floorf((CURSOR_COLOR>>24 & 0xFF) * cursor_alpha + 0.5f);
        display_fill_box(x-2, y-2, x2+1, (y+FONT_HEIGHT)+1,
                         cursor_alpha_byte<<24 | (CURSOR_COLOR & 0x00FFFFFF));
    }
}

/*----------------------------------*/

/**
 * draw_disabled_menu_option:  Draw a single disabled menu option.  If the
 * option is currently selected, also draws the menu cursor.
 *
 * [Parameters]
 *     option: Option ID (OPT_*)
 *       x, y: Text position
 *     format: Format string for option text
 *        ...: Format arguments
 * [Return value]
 *     None
 */
static void draw_disabled_menu_option(int option, int x, int y,
                                      const char *format, ...)
{
    PRECOND(format != NULL, return);

    char buf[1000];
    va_list args;
    va_start(args, format);
    vsnprintf(buf, sizeof(buf), format, args);
    va_end(args);
    int x2 = font_printf(x, y, -1, TEXT_COLOR_DISABLED, "%s", buf);

    if (cur_option == option) {
        const float cursor_alpha =
            (sinf((cursor_timer / (float)CURSOR_PERIOD) * (float)M_TWOPI) + 1)
            / 2;
        const uint32_t cursor_alpha_byte =
            floorf((CURSOR_COLOR>>24 & 0xFF) * cursor_alpha + 0.5f);
        display_fill_box(x-2, y-2, x2+1, (y+FONT_HEIGHT)+1,
                         cursor_alpha_byte<<24 | (CURSOR_COLOR & 0x00FFFFFF));
    }
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
