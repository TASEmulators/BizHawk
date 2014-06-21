/*  src/psp/osk.h: PSP on-screen keyboard management header
    Copyright 2010 Andrew Church

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

#ifndef PSP_OSK_H
#define PSP_OSK_H

/*************************************************************************/

/* Result codes returned from osk_result(). */

typedef enum OSKResult_ {
    OSK_RESULT_NONE      = 0,
    OSK_RESULT_RUNNING,
    OSK_RESULT_UNCHANGED,
    OSK_RESULT_CHANGED,
    OSK_RESULT_CANCELLED,
    OSK_RESULT_ERROR,
} OSKResult;

/*-----------------------------------------------------------------------*/

/**
 * osk_open:  Open the on-screen keyboard with the given prompt string and
 * default text.
 *
 * [Parameters]
 *      prompt: Prompt string
 *     deftext: Default text
 *      maxlen: Maximum length (number of _characters_, not bytes) of
 *                 entered text, not including the trailing null
 * [Return value]
 *     Nonzero on success, zero on failure
 */
extern int osk_open(const char *prompt, const char *deftext,
                    unsigned int maxlen);

/**
 * osk_update:  Update the on-screen keyboard if it is active.  Must be
 * called once per frame while the on-screen keyboard is active; may be
 * called at any other time (the function does nothing in that case).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void osk_update(void);

/**
 * osk_status:  Return whether the on-screen keyboard is currently active.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the on-screen keyboard is active, else zero
 */
extern int osk_status(void);

/**
 * osk_result:  Return the result status from the on-screen keyboard.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Result status (OSK_RESULT_*)
 */
extern OSKResult osk_result(void);

/**
 * osk_get_text:  Return the text entered by the user from the on-screen
 * keyboard in a newly malloc()ed buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Entered text, or NULL if not available (e.g., if the OSK was cancelled)
 */
extern char *osk_get_text(void);

/**
 * osk_close:  Close the on-screen keyboard and discard all associated
 * resources (including the entered text).  If the on-screen keyboard is
 * not active, this function does nothing.
 *
 * Even after calling this function, the caller MUST continue to call
 * osk_update() once per frame until osk_status() returns zero.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void osk_close(void);

/*************************************************************************/

#endif  // PSP_OSK_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
