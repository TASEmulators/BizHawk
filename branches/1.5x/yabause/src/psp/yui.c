/*  src/psp/yui.c: Yabause core interface routines
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

#include "../vidsoft.h"
#include "../yui.h"

#include "display.h"
#include "menu.h"

/*************************************************************************/
/******************** Yabause core interface routines ********************/
/*************************************************************************/

/**
 * YuiErrorMsg:  Report an error to the user.
 *
 * [Parameters]
 *     string: Error message string
 * [Return value]
 *     None
 */
void YuiErrorMsg(const char *string)
{
    PRECOND(string != NULL, return);

#ifdef PSP_DEBUG
    fprintf(stderr, "%s", string);
#endif

    /* Drop any leading/trailing newlines, and convert internal newlines to
     * spaces, before passing the message to the menu screen */
    while (*string == '\r' || *string == '\n') {
        string++;
    }
    char buf[100];
    snprintf(buf, sizeof(buf), "%s", string);
    int i;
    for (i = strlen(buf)-1; i >= 0; i--) {
        if (buf[i] == '\r' || buf[i] == '\n') {
            buf[i] = 0;
        } else {
            break;
        }
    }
    for (i = 0; buf[i]; i++) {
        if (buf[i] == '\r' || buf[i] == '\n') {
            buf[i] = ' ';
        }
    }
    menu_set_error(buf);
}

/*************************************************************************/

/**
 * YuiSwapBuffers:  Swap the front and back display buffers.  Called by the
 * software renderer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void YuiSwapBuffers(void)
{
    if (!dispbuffer) {
        return;
    }

    /* Calculate display size (shrink interlaced/hi-res displays by half) */
    int width_in, height_in, width_out, height_out;
    VIDCore->GetGlSize(&width_in, &height_in);
    if (width_in <= DISPLAY_WIDTH) {
        width_out = width_in;
    } else {
        width_out = width_in / 2;
    }
    if (height_in <= DISPLAY_HEIGHT) {
        height_out = height_in;
    } else {
        height_out = height_in / 2;
    }
    int x = (DISPLAY_WIDTH - width_out) / 2;
    int y = (DISPLAY_HEIGHT - height_out) / 2;

    /* Make sure all video buffer data is flushed to memory and cleared
     * from the cache */
    sceKernelDcacheWritebackInvalidateRange(dispbuffer,
                                            width_in * height_in * 4);

    /* Blit the data to the screen */
    display_begin_frame();
    if (width_out == width_in && height_out == height_in) {
        display_blit(dispbuffer, width_in, height_in, width_in, x, y);
    } else {
        /* The PSP can't draw textures larger than 512x512, so if we're
         * drawing a high-resolution buffer, split it in half.  The height
         * will never be greater than 512, so we don't need to check for a
         * vertical split. */
        if (width_in > 512) {
            const uint32_t *dispbuffer32 = (const uint32_t *)dispbuffer;
            display_blit_scaled(dispbuffer32, width_in/2, height_in,
                                width_in, x, y, width_out/2, height_out);
            dispbuffer32 += width_in/2;
            x += width_out/2;
            display_blit_scaled(dispbuffer32, width_in/2, height_in,
                                width_in, x, y, width_out/2, height_out);
        } else {
            display_blit_scaled(dispbuffer, width_in, height_in, width_in,
                                x, y, width_out, height_out);
        }
    }
    display_end_frame();
    sceDisplayWaitVblankStart();
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
