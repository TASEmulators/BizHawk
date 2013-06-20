/*  src/psp/psp-video.h: PSP video interface module header
    Copyright 2009-2010 Andrew Church

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

#ifndef PSP_VIDEO_H
#define PSP_VIDEO_H

#include "../vdp1.h"  // for VideoInterface_struct

/*************************************************************************/
/********* Module interface and global-use routine declarations **********/
/*************************************************************************/

/* Module interface definition */
extern VideoInterface_struct VIDPSP;

/* Unique module ID (must be different from any in ../{vdp1,vid*}.h) */
#define VIDCORE_PSP  0x5CE  // "SCE"

/*************************************************************************/

/**
 * psp_video_infoline:  Display an information line on the bottom of the
 * screen.  The text will be displayed for one frame only; call this
 * function every frame to keep the text visible.
 *
 * [Parameters]
 *     color: Text color (0xAABBGGRR)
 *      text: Text string
 * [Return value]
 *     None
 */
extern void psp_video_infoline(uint32_t color, const char *text);

/*************************************************************************/
/************ Internal utility data and routine declarations *************/
/*************************************************************************/

/* Vertex data structure for GU drawing */

typedef struct VertexUVXYZ_ {
    int16_t u, v;
    int16_t x, y, z;
} VertexUVXYZ;

/*************************************************************************/

/**
 * global_clut_16, global_clut_32:  Global color lookup table (from VDP2
 * color RAM), in 16- and 32-bit formats.  Each array is indexed by the
 * color index value used in sprites and tiles; when the VDP2 is in 32-bit
 * color mode (Vdp2Internal.ColorMode == 2), indices 0x400-0x7FF are a
 * copy of 0x000-0x3FF.  In all cases, the alpha values are set to full
 * (1 or 0xFF).
 */
extern uint16_t global_clut_16[0x800];
extern uint32_t global_clut_32[0x800];

/*-----------------------------------------------------------------------*/

/**
 * adjust_color_16_32:  Adjust the components of a 16-bit color value,
 * returning it as a 32-bit color value.
 *
 * [Parameters]
 *     color: 16-bit color value (A1B5G5R5)
 *      rofs: Red component offset
 *      gofs: Green component offset
 *      bofs: Blue component offset
 * [Return value]
 *     Converted and djusted 32-bit color value
 */
static inline uint32_t adjust_color_16_32(uint16_t color, int32_t rofs,
                                          int32_t gofs, int32_t bofs)
{
    int32_t r = color<<3 & 0xF8;
    int32_t g = color>>2 & 0xF8;
    int32_t b = color>>7 & 0xF8;
    return bound(r+rofs, 0, 255) <<  0
         | bound(g+gofs, 0, 255) <<  8
         | bound(b+bofs, 0, 255) << 16
         | (color>>15 ? 0xFF000000 : 0);
}

/*-----------------------------------------------------------------------*/

/**
 * adjust_color_32_32:  Adjust the components of a 32-bit color value.
 *
 * [Parameters]
 *     color: 32-bit color value (ABGR)
 *      rofs: Red component offset
 *      gofs: Green component offset
 *      bofs: Blue component offset
 * [Return value]
 *     Adjusted 32-bit color value
 */
static inline uint32_t adjust_color_32_32(uint32_t color, int32_t rofs,
                                          int32_t gofs, int32_t bofs)
{
    int32_t r = color>> 0 & 0xFF;
    int32_t g = color>> 8 & 0xFF;
    int32_t b = color>>16 & 0xFF;
    return bound(r+rofs, 0, 255) <<  0
         | bound(g+gofs, 0, 255) <<  8
         | bound(b+bofs, 0, 255) << 16
         | (color & 0xFF000000);
}

/*************************************************************************/

/**
 * pspGuGetMemoryMerge:  Acquire a block of memory from the GE display
 * list.  Similar to sceGuGetMemory(), but if the most recent display list
 * operation was also a pspGuGetMemoryMerge() call, merge the two blocks
 * together to avoid long chains of jump instructions in the display list.
 *
 * [Parameters]
 *     size: Size of block to allocate, in bytes
 * [Return value]
 *     Allocated block
 */
void *pspGuGetMemoryMerge(uint32_t size);

/*************************************************************************/
/*************************************************************************/

#endif  // PSP_VIDEO_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
