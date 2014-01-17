/*  src/psp/init.c: PSP initialization routines
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
#include <time.h>  // For struct tm definition

#include "../cdbase.h"
#include "../cs0.h"
#include "../m68kcore.h"
#include "../peripheral.h"
#include "../scsp.h"
#include "../sh2core.h"
#include "../sh2int.h"
#include "../vidsoft.h"

#include "config.h"
#include "control.h"
#include "display.h"
#include "init.h"
#include "localtime.h"
#include "menu.h"
#include "sys.h"
#include "psp-cd.h"
#include "psp-m68k.h"
#include "psp-per.h"
#include "psp-sh2.h"
#include "psp-sound.h"
#include "psp-video.h"

#include "me.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Interface lists */

M68K_struct *M68KCoreList[] = {
    /* We only support the ME-enabled Q68 interface */
    &M68KPSP,
    NULL
};

SH2Interface_struct *SH2CoreList[] = {
    &SH2Interpreter,
    &SH2DebugInterpreter,
    &SH2PSP,
    NULL
};

PerInterface_struct *PERCoreList[] = {
    /* We only support the native interface */
    &PERPSP,
    NULL
};

CDInterface *CDCoreList[] = {
    /* We only support the native interface */
    &CDPSP,
    NULL
};

SoundInterface_struct *SNDCoreList[] = {
    /* We only support the native interface */
    &SNDPSP,
    NULL
};

VideoInterface_struct *VIDCoreList[] = {
    &VIDPSP,
    &VIDSoft,
    NULL
};

/*-----------------------------------------------------------------------*/

/* Local routine declarations */

static void get_base_directory(const char *argv0, char *buffer, int bufsize);

/*************************************************************************/
/************************** Interface routines ***************************/
/*************************************************************************/

/**
 * init_psp:  Perform PSP-related initialization and command-line option
 * parsing.  Aborts the program if an error occurs.
 *
 * [Parameters]
 *     argc: Command line argument count
 *     argv: Command line argument vector
 * [Return value]
 *     None
 */
void init_psp(int argc, char **argv)
{
    /* Set the CPU to maximum speed, because boy, do we need it */
    scePowerSetClockFrequency(333, 333, 166);

    /* Determine the program's base directory and change to it */
    get_base_directory(argv[0], progpath, sizeof(progpath));
    if (*progpath) {
        sceIoChdir(progpath);
    }

    /* Start the system callback monitoring thread */
    if (!sys_setup_callbacks()) {
        sceKernelExitGame();
    }

    /* Initialize controller input management */
    if (!control_init()) {
        DMSG("Failed to initialize controller");
        sceKernelExitGame();
    }

    /* Initialize the display hardware */
    if (!display_init()) {
        DMSG("Failed to initialize display");
        sceKernelExitGame();
    }

    /* Initialize localtime() */
    localtime_init();

    /* Load the ME access library (if possible) */
    int res = sys_load_module("me.prx", PSP_MEMORY_PARTITION_KERNEL);
    if (res < 0) {
        DMSG("Failed to load me.prx: %s", psp_strerror(res));
        me_available = 0;
    } else {
        me_available = 1;
    }
}

/*************************************************************************/

/**
 * init_yabause:  Initialize the emulator core.  On error, an appropriate
 * error message is registered via menu_set_error().
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on failure
 */
int init_yabause(void)
{
    yabauseinit_struct yinit;

    /* Set a default error message in case the core doesn't set one */
    menu_set_error("Failed to initialize the emulator!");

    /* Set up general defaults */
    memset(&yinit, 0, sizeof(yinit));
    yinit.m68kcoretype = M68KCORE_PSP;
    yinit.percoretype = PERCORE_PSP;
    yinit.sh2coretype = config_get_module_sh2();
    yinit.vidcoretype = config_get_module_video();
    yinit.sndcoretype = SNDCORE_PSP;
    yinit.cdcoretype = CDCORE_PSP;
    yinit.carttype = CART_NONE;
    yinit.regionid = 0;
    yinit.biospath = config_get_path_bios();
    yinit.cdpath = config_get_path_cd();
    yinit.buppath = config_get_path_bup();
    yinit.mpegpath = NULL;
    yinit.cartpath = NULL;
    yinit.videoformattype = VIDEOFORMATTYPE_NTSC;
    yinit.clocksync = config_get_clock_sync();
    const time_t basetime = 883656000;  // 1998-01-01 12:00 UTC
    yinit.basetime = config_get_clock_fixed_time()
                     ? basetime - localtime_utc_offset()
                     : 0;
    yinit.usethreads = 1;

    /* Initialize controller settings */
    PerInit(yinit.percoretype);
    PerPortReset();
    padbits = PerPadAdd(&PORTDATA1);
    static const struct {
        uint8_t key;      // Key index from peripheral.h
        uint16_t button;  // PSP button index (PSP_CTRL_*)
    } buttons[] = {
        {PERPAD_UP,             PSP_CTRL_UP},
        {PERPAD_RIGHT,          PSP_CTRL_RIGHT},
        {PERPAD_DOWN,           PSP_CTRL_DOWN},
        {PERPAD_LEFT,           PSP_CTRL_LEFT},
        {PERPAD_RIGHT_TRIGGER,  PSP_CTRL_RTRIGGER},
        {PERPAD_LEFT_TRIGGER,   PSP_CTRL_LTRIGGER},
        {PERPAD_START,          PSP_CTRL_START},
    };
    int i;
    for (i = 0; i < lenof(buttons); i++) {
        PerSetKey(buttons[i].button, buttons[i].key, padbits);
    }
    PerSetKey(config_get_button(CONFIG_BUTTON_A), PERPAD_A, padbits);
    PerSetKey(config_get_button(CONFIG_BUTTON_B), PERPAD_B, padbits);
    PerSetKey(config_get_button(CONFIG_BUTTON_C), PERPAD_C, padbits);
    PerSetKey(config_get_button(CONFIG_BUTTON_X), PERPAD_X, padbits);
    PerSetKey(config_get_button(CONFIG_BUTTON_Y), PERPAD_Y, padbits);
    PerSetKey(config_get_button(CONFIG_BUTTON_Z), PERPAD_Z, padbits);

    /* Initialize emulator state */
    if (YabauseInit(&yinit) != 0) {
        DMSG("YabauseInit() failed!");
        return 0;
    }
    YabauseSetDecilineMode(config_get_deciline_mode());
    ScspSetFrameAccurate(config_get_audio_sync());
    ScspUnMuteAudio(SCSP_MUTE_SYSTEM);

    /* Success */
    menu_set_error(NULL);
    return 1;
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * get_base_directory:  Extract the program's base directory from argv[0].
 * Stores the empty string in the destination buffer if the base directory
 * cannot be determined.
 *
 * [Parameters]
 *       argv0: Value of argv[0]
 *      buffer: Buffer to store directory path into
 *     bufsize: Size of buffer
 * [Return value]
 *     None
 */
static void get_base_directory(const char *argv0, char *buffer, int bufsize)
{
    *buffer = 0;
    if (argv0) {
        const char *s = strrchr(argv0, '/');
        if (s != NULL) {
            int n = snprintf(buffer, bufsize, "%.*s", s - argv0, argv0);
            if (n >= bufsize) {
                DMSG("argv[0] too long: %s", argv0);
                *buffer = 0;
            }
        } else {
            DMSG("argv[0] has no directory: %s", argv0);
        }
    } else {
        DMSG("argv[0] is NULL!");
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
