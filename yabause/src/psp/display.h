/*  src/psp/display.h: PSP display management header
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

#ifndef PSP_DISPLAY_H
#define PSP_DISPLAY_H

/*************************************************************************/

/* Physical screen layout */
#define DISPLAY_WIDTH  480
#define DISPLAY_HEIGHT 272
#define DISPLAY_STRIDE 512

/*-----------------------------------------------------------------------*/

/**
 * display_init:  Initialize the PSP display.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int display_init(void);

/**
 * display_set_size:  Set the effective display size.  The display will
 * be centered on the PSP's screen.
 *
 * This routine should not be called while drawing a frame (i.e. between
 * display_begin_frame() and display_end_frame()).
 *
 * [Parameters]
 *      width: Effective display width (pixels)
 *     height: Effective display height (pixels)
 * [Return value]
 *     None
 */
extern void display_set_size(int width, int height);

/**
 * display_disp_buffer:  Return a pointer to the current displayed (front)
 * buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the current work buffer
 */
extern uint32_t *display_disp_buffer(void);

/**
 * display_work_buffer:  Return a pointer to the current work (back)
 * buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the current work buffer
 */
extern uint32_t *display_work_buffer(void);

/**
 * display_spare_vram:  Return a pointer to the VRAM spare area.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the VRAM spare area
 */
extern void *display_spare_vram(void);

/**
 * display_spare_vram_size:  Return the size of the VRAM spare area.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Size of the VRAM spare area, in bytes
 */
extern uint32_t display_spare_vram_size(void);

/*----------------------------------*/

/**
 * display_alloc_vram:  Allocate memory from the spare VRAM area, aligned
 * to a multiple of 64 bytes.  All allocated VRAM will be automatically
 * freed at the next call to display_begin_frame().
 *
 * [Parameters]
 *     size: Amount of memory to allocate, in bytes
 * [Return value]
 *     Pointer to allocated memory, or NULL on failure (out of memory)
 */
extern void *display_alloc_vram(uint32_t size);

/*----------------------------------*/

/**
 * display_begin_frame:  Begin processing for a frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void display_begin_frame(void);

/**
 * display_end_frame:  End processing for the current frame, then swap the
 * displayed buffer and work buffer.  The current frame will not actually
 * be shown until the next vertical blank.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void display_end_frame(void);

/**
 * display_last_frame_length:  Returns the length of time the last frame
 * was displayed, in hardware frame (1/59.94sec) units.  Only valid between
 * display_begin_frame() and display_end_frame().
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Length of time the last frame was displayed
 */
extern unsigned int display_last_frame_length(void);

/*----------------------------------*/

/**
 * display_blit:  Draw an image to the display.  The image must be in
 * native 32bpp format.
 *
 * [Parameters]
 *            src: Source image data pointer
 *      src_width: Width of the source image, in pixels
 *     src_height: Height of the source image, in pixels
 *     src_stride: Line length of source image, in pixels
 *         dest_x: X coordinate at which to display image
 *         dest_y: Y coordinate at which to display image
 * [Return value]
 *     None
 */
extern void display_blit(const void *src, int src_width, int src_height,
                         int src_stride, int dest_x, int dest_y);

/**
 * display_blit_scaled:  Scale and draw an image to the display.  The image
 * must be in native 32bpp format.
 *
 * [Parameters]
 *            src: Source image data pointer
 *      src_width: Width of the source image, in pixels
 *     src_height: Height of the source image, in pixels
 *     src_stride: Line length of source image, in pixels
 *         dest_x: X coordinate at which to display image
 *         dest_y: Y coordinate at which to display image
 *     dest_width: Width of the displayed image, in pixels
 *    dest_height: Height of the displayed image, in pixels
 * [Return value]
 *     None
 */
extern void display_blit_scaled(const void *src, int src_width, int src_height,
                                int src_stride, int dest_x, int dest_y,
                                int dest_width, int dest_height);

/**
 * display_fill_box:  Draw a filled box with a specified color.
 *
 * [Parameters]
 *     x1, y1: Upper-left coordinates of box
 *     x2, y2: Lower-right coordinates of box
 *      color: Color to fill with (0xAABBGGRR)
 * [Return value]
 *     None
 */
extern void display_fill_box(int x1, int y1, int x2, int y2, uint32_t color);

/*************************************************************************/

#endif  // PSP_DISPLAY_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
