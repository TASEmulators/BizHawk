/*  src/psp/filesel.h: PSP file selector header
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

#ifndef PSP_FILESEL_H
#define PSP_FILESEL_H

/*************************************************************************/

/* Data type of a file selector instance (contents are opaque) */
typedef struct FileSelector_ FileSelector;

/*-----------------------------------------------------------------------*/

/**
 * filesel_create:  Create a new file selector.
 *
 * [Parameters]
 *     title: File selector window title
 *       dir: Directory to select file from
 * [Return value]
 *     Newly-created file selector, or NULL on error
 */
extern FileSelector *filesel_create(const char *title, const char *dir);

/**
 * filesel_process:  Process input for a file selector.
 *
 * [Parameters]
 *     filesel: File selector
 *     buttons: Newly-pressed (or repeating) buttons (PSP_CTRL_* bitmask)
 * [Return value]
 *     None
 */
extern void filesel_process(FileSelector *filesel, uint32_t buttons);

/**
 * filesel_draw:  Draw a file selector.
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     None
 */
extern void filesel_draw(FileSelector *filesel);

/**
 * filesel_done:  Return whether a file selector's work is done (i.e.,
 * whether the user has either selected a file or cancelled the selector).
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     True (nonzero) if any of the following are true:
 *         - The user selected a file
 *         - The user cancelled the file selector
 *         - An error occurred which prevents the file selector's
 *               processing from continuing normally
 *     False (zero) if none of the above are true
 */
extern int filesel_done(FileSelector *filesel);

/**
 * filesel_selected_file:  Return the name of the file selected by the
 * user, if any.
 *
 * [Parameters]
 *     filesel: File selector
 * [Return value]
 *     The name of the file selected by the user, or NULL if the user has
 *     not selected a file (including if the user has cancelled the file
 *     selector)
 */
extern const char *filesel_selected_file(FileSelector *filesel);

/**
 * filesel_destroy:  Destroy a file selector.  Does nothing if filesel==NULL.
 *
 * [Parameters]
 *     filesel: File selector to destroy
 * [Return value]
 *     None
 */
extern void filesel_destroy(FileSelector *filesel);

/*************************************************************************/

#endif  // PSP_FILESEL_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
