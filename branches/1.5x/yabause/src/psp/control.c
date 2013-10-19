/*  src/psp/control.c: PSP controller input management routines
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

#include "control.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Current bitmask of buttons pressed */
static uint32_t buttons;

/* Previous value of "buttons" */
static uint32_t last_buttons;

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * control_init:  Initialize the controller input management code.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
int control_init(void)
{
    sceCtrlSetSamplingCycle(0);
    sceCtrlSetSamplingMode(PSP_CTRL_MODE_ANALOG);
    buttons = last_buttons = 0;

    return 1;
}

/*************************************************************************/

/**
 * control_update:  Update the current controller status.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void control_update(void)
{
    SceCtrlData pad_data;
    sceCtrlPeekBufferPositive(&pad_data, 1);

    last_buttons = buttons;
    buttons = pad_data.Buttons;

    /* If the directional pad isn't being used, check the analog pad instead */
    if (!(buttons & 0x00F0)) {
        if (pad_data.Lx < 32) {
            buttons |= PSP_CTRL_LEFT;
        } else if (pad_data.Lx >= 224) {
            buttons |= PSP_CTRL_RIGHT;
        }
        if (pad_data.Ly < 32) {
            buttons |= PSP_CTRL_UP;
        } else if (pad_data.Ly >= 224) {
            buttons |= PSP_CTRL_DOWN;
        }
    }

    /* The OS doesn't seem to reset the screensaver timeout when the
     * analog pad is moved, so take care of that ourselves */
    if (pad_data.Lx < 32 || pad_data.Lx >= 224
     || pad_data.Ly < 32 || pad_data.Ly >= 224
    ) {
        scePowerTick(0);
    }
}

/*************************************************************************/

/**
 * control_state:  Return the current controller state.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Current controller state (PSP_CTRL_* bitmask of buttons held down)
 */
uint32_t control_state(void)
{
    return buttons;
}

/*-----------------------------------------------------------------------*/

/**
 * control_new_buttons:  Return any buttons newly pressed in the last call
 * to control_update().
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Newly pressed buttons (PSP_CTRL_* bitmask)
 */
uint32_t control_new_buttons(void)
{
    return buttons & ~last_buttons;
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
