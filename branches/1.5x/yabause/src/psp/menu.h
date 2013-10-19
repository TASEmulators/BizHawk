/*  src/psp/menu.h: PSP menu interface header
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

#ifndef PSP_MENU_H
#define PSP_MENU_H

/*************************************************************************/

/**
 * menu_open:  Open the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void menu_open(void);

/**
 * menu_run:  Perform a single frame's processing for the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void menu_run(void);

/**
 * menu_close:  Close the menu interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void menu_close(void);

/**
 * menu_set_error:  Set an error message to be displayed on the menu
 * screen.  If message is NULL, any message currently displayed is cleared.
 *
 * [Parameters]
 *     message: Message text (NULL to clear current message)
 * [Return value]
 *     None
 */
extern void menu_set_error(const char *message);

/*************************************************************************/

#endif  // PSP_MENU_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
