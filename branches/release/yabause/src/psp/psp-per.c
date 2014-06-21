/*  src/psp/psp-per.c: PSP peripheral interface module
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

#include "../peripheral.h"

#include "control.h"
#include "psp-per.h"

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_per_init(void);
static void psp_per_deinit(void);
static int psp_per_handle_events(void);
#ifdef PERKEYNAME
static void psp_per_key_name(u32 key, char *name, int size);
#endif

/*-----------------------------------------------------------------------*/

/* Module interface definition */

PerInterface_struct PERPSP = {
    .id           = PERCORE_PSP,
    .Name         = "PSP Peripheral Interface",
    .Init         = psp_per_init,
    .DeInit       = psp_per_deinit,
    .HandleEvents = psp_per_handle_events,
    .canScan      = 0,
#ifdef PERKEYNAME
    .KeyName      = psp_per_key_name,
#endif
};

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * psp_per_init:  Initialize the peripheral interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_per_init(void)
{
    /* Nothing to do */
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_per_deinit:  Shut down the peripheral interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_per_deinit(void)
{
    /* Nothing to do */
}

/*************************************************************************/

/**
 * psp_per_handle_events:  Process pending peripheral events, and run one
 * iteration of the emulation.
 *
 * For the PSP, the main loop is located in main.c; we only update the
 * current button status here.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_per_handle_events(void)
{
    static uint32_t last_buttons;

    const uint32_t buttons = control_state();
    const uint32_t changed_buttons = buttons ^ last_buttons;
    last_buttons = buttons;

    int i;
    for (i = 0; i < 16; i++) {
        const uint32_t button = 1 << i;
        if (changed_buttons & button) {
            if (buttons & button) {
                PerKeyDown(button);
            } else {
                PerKeyUp(button);
            }
        }
    }

    YabauseExec();
    return 0;
}

/*************************************************************************/

#ifdef PERKEYNAME

/**
 * psp_per_key_name:  Return the name corresponding to a system-dependent
 * key value.
 *
 * [Parameters]
 *      key: Key value to return name for
 *     name: Buffer into which name is to be stored
 *     size: Size of buffer in bytes
 * [Return value]
 *     None
 */
static void psp_per_key_name(u32 key, char *name, int size)
{
    /* Not supported on PSP */
    *name = 0;
}

#endif  // PERKEYNAME

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
