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

#ifndef PSP_CONTROL_H
#define PSP_CONTROL_H

/*************************************************************************/

/**
 * control_init:  Initialize the controller input management code.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int control_init(void);

/**
 * control_update:  Update the current controller status.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void control_update(void);

/**
 * control_state:  Return the current controller status.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Current controller status (PSP_CTRL_* bitmask of buttons held down)
 */
extern uint32_t control_state(void);

/**
 * control_new_buttons:  Return any buttons newly pressed in the last call
 * to control_update().
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Newly pressed buttons (PSP_CTRL_* bitmask)
 */
extern uint32_t control_new_buttons(void);

/*************************************************************************/

#endif  // PSP_CONTROL_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
