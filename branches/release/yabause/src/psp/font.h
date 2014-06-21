/*  src/psp/font.h: Header for PSP menu font
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

#ifndef PSP_FONT_H
#define PSP_FONT_H

/*************************************************************************/

/* Height of the font, in pixels */

#define FONT_HEIGHT  13

/*-----------------------------------------------------------------------*/

/**
 * font_printf:  Draw text to the screen at the given position and with the
 * given color.  The string can contain printf()-style format specifiers.
 *
 * It is assumed that a display list has been started with sceGuStart()
 * before calling this function.
 *
 * [Parameters]
 *       x, y: Upper-left coordinates of first character
 *      align: Horizontal alignment (<0: left, 0: center, >0: right)
 *      color: Text color (0xAABBGGRR)
 *     format: Format string for text
 *        ...: Arguments for format string
 * [Return value]
 *     X coordinate immediately following the last character printed
 */
extern int font_printf(int x, int y, int align, uint32_t color,
                       const char *format, ...)
    __attribute__((format(printf,5,6)));

/*************************************************************************/

#endif  // PSP_FONT_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
