/*  src/psp/init.h: PSP initialization routine header
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

#ifndef PSP_INIT_H
#define PSP_INIT_H

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
extern void init_psp(int argc, char **argv);

/**
 * init_yabause:  Initialize the emulator core.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on failure
 */
extern int init_yabause(void);

/*************************************************************************/

#endif  // PSP_INIT_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
