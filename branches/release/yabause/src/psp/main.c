/*  src/psp/main.c: PSP entry point and main loop
    Copyright 2009 Andrew Church

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

#include "../memory.h"
#include "../peripheral.h"

#include "config.h"
#include "control.h"
#include "init.h"
#include "menu.h"
#include "misc.h"
#include "osk.h"
#include "psp-video.h"
#include "timing.h"

#ifdef SYS_PROFILE_H
# include "profile.h"  // Can only be ours
#endif

/*************************************************************************/
/************************ PSP module information *************************/
/*************************************************************************/

#define MODULE_FLAGS     0
#define MODULE_VERSION   0
#define MODULE_REVISION  9

PSP_MODULE_INFO("Yabause", MODULE_FLAGS, MODULE_VERSION, MODULE_REVISION);
const PSP_MAIN_THREAD_PRIORITY(THREADPRI_MAIN);
const PSP_MAIN_THREAD_STACK_SIZE_KB(128);
const PSP_MAIN_THREAD_ATTR(PSP_THREAD_ATTR_USER);
const PSP_HEAP_SIZE_KB(-64);  // Leave 64k for thread stacks

/*************************************************************************/
/****************************** Global data ******************************/
/*************************************************************************/

/* Program directory (determined from argv[0], and exported) */
char progpath[256];

/* Saturn control pad handle (set at initialization time, and used by menu
 * code to change button assignments) */
void *padbits;

/* Flag indicating whether the ME is available for use */
int me_available;

/* Have we successfully initialized the Yabause core? */
int yabause_initted;

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Flag indicating whether the menu is currently displayed */
static int in_menu;

/* Delay timer for backup RAM autosave */
static int bup_autosave_timer;
#define BUP_AUTOSAVE_DELAY  60  // frames

/* Timer for autosave overlay message */
static int bup_autosave_info_timer;
#define BUP_AUTOSAVE_INFO_TIME  300  // frames

/* Text and color for autosave overlay message */
#define BUP_AUTOSAVE_INFO_TEXT   "Backup RAM saved."
#define BUP_AUTOSAVE_INFO_COLOR  0xFF55FF40  // TEXT_COLOR_OK from menu.c

/*-----------------------------------------------------------------------*/

/* Local routine declarations */

static void iterate_main_loop(void);
static void emulate_one_frame(void);
static void check_autosave(void);

/*************************************************************************/
/************************** Program entry point **************************/
/*************************************************************************/

/**
 * main:  Program entry point.  Performs initialization and then loops
 * indefinitely, running the emulator.
 *
 * [Parameters]
 *     argc: Command line argument count
 *     argv: Command line argument vector
 * [Return value]
 *     Does not return
 */
int main(int argc, char **argv)
{
    in_menu = 0;
    bup_autosave_timer = 0;
    bup_autosave_info_timer = 0;

    init_psp(argc, argv);
    config_load();
    if (!config_get_start_in_emu()) {
        /* Don't initialize yet -- the user may need to set filenames first. */
        menu_open();
        in_menu = 1;
    } else {
        if (init_yabause()) {
            yabause_initted = 1;
        } else {
            /* Start in the menu so the user sees the error message. */
            menu_open();
            in_menu = 1;
        }
    }

    timing_init();
    for (;;) {
        iterate_main_loop();
    }
}

/*************************************************************************/
/*********** Main loop iteration routine and helper functions ************/
/*************************************************************************/

/**
 * iterate_main_loop:  Run one iteration of the main loop.  Either emulates
 * one Saturn frame or runs the menu for one PSP frame.
 *
 * [Parameters]
 *     buttons: Current state of PSP control buttons
 * [Return value]
 *     None
 */
static void iterate_main_loop(void)
{
    timing_sync();
    control_update();

    if (control_new_buttons() & PSP_CTRL_SELECT) {
        if (in_menu) {
            if (osk_status()) {
                /* If the OSK is active, SELECT is used to switch character
                 * sets, so we shouldn't respond to it. */
            } else if (!yabause_initted && !init_yabause()) {
                /* We failed to start the emulator, so stay in the menu. */
            } else {
                yabause_initted = 1;  // In case we just successfully initted
                menu_close();
                in_menu = 0;
            }
        } else {
            menu_open();
            in_menu = 1;
        }
    }

    if (in_menu) {
        menu_run();
    } else {
        emulate_one_frame();
    }
}

/*************************************************************************/

/**
 * emulate_one_frame:  Run the emulator for one frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void emulate_one_frame(void)
{
    PERCore->HandleEvents();  // Also runs the actual emulation

    check_autosave();

    /* Reset the screensaver timeout, so people don't have to deal with
     * the backlight dimming during FMV */
    scePowerTick(0);

#ifdef SYS_PROFILE_H  // Print out profiling info every 100 frames
    static unsigned int frame = 0;
    frame++;
    if (frame % 100 == 0) {
        printf("Profiling statistics at frame %u:\n", frame);
        PROFILE_PRINT();
        PROFILE_RESET();
    }
#endif
}

/*-----------------------------------------------------------------------*/

/**
 * check_autosave:  Check whether to autosave backup RAM and/or display the
 * autosave message.  Should be called exactly once per emulated frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void check_autosave(void)
{
    if (BupRamWritten) {
        /* Wait BUP_AUTOSAVE_DELAY frames from the last write before we
         * update the file, so we don't update on every frame while the
         * emulated game is saving its data. */
        bup_autosave_timer = BUP_AUTOSAVE_DELAY;
        BupRamWritten = 0;
    } else if (bup_autosave_timer > 0) {
        bup_autosave_timer--;
        if (bup_autosave_timer == 0) {
            save_backup_ram();
            bup_autosave_info_timer = BUP_AUTOSAVE_INFO_TIME;
        }
    }

    if (bup_autosave_info_timer > 0) {
        uint32_t color;
        if (bup_autosave_info_timer >= BUP_AUTOSAVE_INFO_TIME / 2) {
            color = BUP_AUTOSAVE_INFO_COLOR;
        } else {
            const uint32_t alpha = 
                (255 * bup_autosave_info_timer) / (BUP_AUTOSAVE_INFO_TIME / 2);
            color = alpha<<24 | (BUP_AUTOSAVE_INFO_COLOR & 0x00FFFFFF);
        }
        psp_video_infoline(color, BUP_AUTOSAVE_INFO_TEXT);
        bup_autosave_info_timer--;
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
