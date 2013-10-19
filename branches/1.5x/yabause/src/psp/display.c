/*  src/psp/display.c: PSP display management functions
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

#include "display.h"
#include "gu.h"
#include "sys.h"

/*************************************************************************/

/* Effective display size */
static unsigned int display_width, display_height;

/* Display mode (pixel format) and bits per pixel */
static uint8_t display_mode;
static uint8_t display_bpp;

/* Currently displayed (front) buffer index */
static uint8_t displayed_surface;

/* Work (back) buffer index */
static uint8_t work_surface;

/* Pointers into VRAM */
static void *surfaces[2];         // Display buffers
static uint8_t *vram_spare_ptr;   // Spare VRAM (above display buffers)
static uint8_t *vram_next_alloc;  // Next spare address to allocate
static uint8_t *vram_top;         // Top of VRAM

/* Display list */
/* Size note:  A single VDP2 background layer, if at resolution 704x512,
 * can take up to 500k of display list memory to render. */
static uint8_t __attribute__((aligned(64))) display_list[3*1024*1024];

/* Semaphore flag to indicate whether there is a buffer swap pending; while
 * true, the main thread must not access any other variables */
static int swap_pending;

/* System clock after the sceDisplayWaitVblankStart() starting the last frame*/
static uint32_t last_frame_start;

/* Number of frames between the last two calls to sceDisplayWaitVblankStart()*/
static unsigned int last_frame_length;

/*************************************************************************/

/* Local routine declarations */

__attribute__((noreturn))
    static int buffer_swap_thread(SceSize args, void *argp);

static void do_buffer_swap(void);

/*************************************************************************/
/*************************************************************************/

/**
 * display_init:  Initialize the PSP display.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
int display_init(void)
{
    /* Have we already initialized? */
    static int initted = 0;
    if (initted) {
        return 1;
    }

    /* Clear out VRAM */
    memset(sceGeEdramGetAddr(), 0, sceGeEdramGetSize());
    sceKernelDcacheWritebackInvalidateAll();

    /* Set display mode */
    int32_t res = sceDisplaySetMode(0, DISPLAY_WIDTH, DISPLAY_HEIGHT);
    if (res < 0) {
        DMSG("sceDisplaySetMode() failed: %s", psp_strerror(res));
        return 0;
    }
    display_width = DISPLAY_WIDTH;
    display_height = DISPLAY_HEIGHT;
    display_mode = PSP_DISPLAY_PIXEL_FORMAT_8888;
    display_bpp = 32;

    /* Initialize VRAM pointers */
    uint8_t *vram_addr = sceGeEdramGetAddr();
    uint32_t vram_size = sceGeEdramGetSize();
    const uint32_t frame_size =
        DISPLAY_STRIDE * DISPLAY_HEIGHT * (display_bpp/8);
    int i;
    for (i = 0; i < lenof(surfaces); i++) {
        surfaces[i] = vram_addr + i*frame_size;
    }
    vram_spare_ptr = (uint8_t *)(vram_addr + lenof(surfaces)*frame_size);
    vram_next_alloc = vram_spare_ptr;
    vram_top = vram_addr + vram_size;
    displayed_surface = 0;
    work_surface = 1;
    swap_pending = 0;

    /* Set the currently-displayed buffer */
    sceDisplaySetFrameBuf(surfaces[displayed_surface], DISPLAY_STRIDE,
                          display_mode, PSP_DISPLAY_SETBUF_IMMEDIATE);

    /* Set up the GU library */
    guInit();
    guStart(GU_DIRECT, display_list);
    guDispBuffer(DISPLAY_WIDTH, DISPLAY_HEIGHT,
                 surfaces[displayed_surface], DISPLAY_STRIDE);
    guFinish();
    guSync(0, 0);

    /* Success */
    initted = 1;
    return 1;
}

/*-----------------------------------------------------------------------*/

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
void display_set_size(int width, int height)
{
    display_width = width;
    display_height = height;
}

/*-----------------------------------------------------------------------*/

/**
 * display_disp_buffer:  Return a pointer to the current displayed (front)
 * buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the current work buffer
 */
uint32_t *display_disp_buffer(void)
{
    return (uint32_t *)surfaces[displayed_surface]
           + ((DISPLAY_HEIGHT - display_height) / 2) * DISPLAY_STRIDE
           + ((DISPLAY_WIDTH  - display_width ) / 2);
}

/*-----------------------------------------------------------------------*/

/**
 * display_work_buffer:  Return a pointer to the current work (back)
 * buffer.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the current work buffer
 */
uint32_t *display_work_buffer(void)
{
    return (uint32_t *)surfaces[work_surface]
           + ((DISPLAY_HEIGHT - display_height) / 2) * DISPLAY_STRIDE
           + ((DISPLAY_WIDTH  - display_width ) / 2);
}

/*-----------------------------------------------------------------------*/

/**
 * display_spare_vram:  Return a pointer to the VRAM spare area.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Pointer to the VRAM spare area
 */
void *display_spare_vram(void)
{
    return vram_spare_ptr;
}

/*-----------------------------------------------------------------------*/

/**
 * display_spare_vram_size:  Return the size of the VRAM spare area.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Size of the VRAM spare area, in bytes
 */
uint32_t display_spare_vram_size(void)
{
    return vram_top - vram_spare_ptr;
}

/*************************************************************************/

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
void *display_alloc_vram(uint32_t size)
{
    if (vram_next_alloc + size > vram_top) {
        return NULL;
    }
    void *ptr = vram_next_alloc;
    vram_next_alloc += size;
    if ((uintptr_t)vram_next_alloc & 0x3F) {  // Make sure it stays aligned
        vram_next_alloc += 0x40 - ((uintptr_t)vram_next_alloc & 0x3F);
    }
    return ptr;
}

/*************************************************************************/

/**
 * display_begin_frame:  Begin processing for a frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void display_begin_frame(void)
{
    sceKernelDelayThread(0);  // Seems to be needed for the buffer swap to work
    while (swap_pending) {
        sceKernelDelayThread(100);  // 0.1ms
    }

    vram_next_alloc = vram_spare_ptr;

    guStart(GU_DIRECT, display_list);

    /* We don't use a depth buffer, so disable depth buffer writing */
    guDepthMask(GU_TRUE);

    /* Clear the work surface--make sure to use the base pointer here, not
     * the effective pointer, lest we stomp on spare VRAM while using the
     * second buffer */
    guDrawBuffer(GU_PSM_8888, surfaces[work_surface], DISPLAY_STRIDE);
    guDisable(GU_SCISSOR_TEST);
    guClear(GU_COLOR_BUFFER_BIT);
    guCommit();

    /* Register the effective work surface pointer */
    guDrawBuffer(GU_PSM_8888, display_work_buffer(), DISPLAY_STRIDE);

    /* Set up drawing area parameters (we set the depth parameters too,
     * just in case a custom drawing routine wants to use 3D coordinates) */
    guViewport(2048, 2048, display_width, display_height);
    guOffset(2048 - display_width/2, 2048 - display_height/2);
    guScissor(0, 0, display_width, display_height);
    guEnable(GU_SCISSOR_TEST);
    guDepthRange(65535, 0);
    guDisable(GU_DEPTH_TEST);

}

/*-----------------------------------------------------------------------*/

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
void display_end_frame(void)
{
    guFinish();
    swap_pending = 1;
    /* Give the new thread a slightly higher priority than us, or else it
     * won't actually get a chance to run. */
    if (sys_start_thread("YabauseBufferSwapThread", buffer_swap_thread,
                         sceKernelGetThreadCurrentPriority() - 1,
                         0x1000, 0, NULL) < 0) {
        DMSG("Failed to start buffer swap thread");
        swap_pending = 0;
        do_buffer_swap();  // Do it ourselves
    }
}

/*-----------------------------------------------------------------------*/

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
unsigned int display_last_frame_length(void)
{
    return last_frame_length;
}

/*************************************************************************/

/**
 * display_blit:  Draw an image to the display.  The image must be in
 * native 32bpp format.
 *
 * [Parameters]
 *           src: Source image data pointer
 *     src_width: Width of the source image, in pixels
 *    src_height: Height of the source image, in pixels
 *    src_stride: Line length of source image, in pixels
 *        dest_x: X coordinate at which to display image
 *        dest_y: Y coordinate at which to display image
 * [Return value]
 *     None
 */
void display_blit(const void *src, int src_width, int src_height,
                  int src_stride, int dest_x, int dest_y)
{
#if 1  // Method 1: DMA the data into VRAM (fastest)

    /* Pointer must be 64-byte aligned, so if it's not, adjust the pointer
     * and add an offset to the X coordinate */
    const unsigned int xofs = ((uintptr_t)src & 0x3F) / 4;
    const uint32_t * const src_aligned = (const uint32_t *)src - xofs;

    guCopyImage(GU_PSM_8888, xofs, 0, src_width, src_height, src_stride,
                src_aligned, dest_x, dest_y, DISPLAY_STRIDE,
                display_work_buffer());

#elif 1  // Method 2: Draw as a texture (slower, but more general)

    /* Pointer must be 64-byte aligned, so if it's not, adjust the pointer
     * and add an offset to the X coordinate */
    const unsigned int xofs = ((uintptr_t)src & 0x3F) / 4;
    const uint32_t * const src_aligned = (const uint32_t *)src - xofs;

    /* Set up the vertex array (it's faster to draw multiple vertical
     * strips than try to blit the whole thing at once) */
    const int nstrips = (src_width + 15) / 16;
    struct {
        uint16_t u, v;
        int16_t x, y, z;
    } *vertices = guGetMemory(sizeof(*vertices) * (2*nstrips));
    /* Note that guGetMemory() never fails, so we don't check for failure */
    int i, x;
    for (i = 0, x = 0; x < src_width; i += 2, x += 16) {
        int thiswidth = 16;
        if (x+16 > src_width) {
            thiswidth = src_width - x;
        }
        vertices[i+0].u = xofs + x;
        vertices[i+0].v = 0;
        vertices[i+0].x = dest_x + x;
        vertices[i+0].y = dest_y;
        vertices[i+0].z = 0;
        vertices[i+1].u = xofs + x + thiswidth;
        vertices[i+1].v = src_height;
        vertices[i+1].x = dest_x + x + thiswidth;
        vertices[i+1].y = dest_y + src_height;
        vertices[i+1].z = 0;
    }

    /* Draw the array */
    guEnable(GU_TEXTURE_2D);
    guTexFilter(GU_NEAREST, GU_NEAREST);
    guTexWrap(GU_CLAMP, GU_CLAMP);
    guTexFunc(GU_TFX_REPLACE, GU_TCC_RGB);
    /* Always use a 512x512 texture size (the GE can only handle sizes that
     * are powers of 2, and the size itself doesn't seem to affect speed) */
    guTexFlush();
    guTexMode(GU_PSM_8888, 0, 0, 0);
    guTexImage(0, 512, 512, src_stride, src_aligned);
    guDrawArray(GU_SPRITES,
                GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                2*nstrips, NULL, vertices);
    guDisable(GU_TEXTURE_2D);

#else  // Method 3: Copy using the CPU (slowest)

    const uint32_t *src32 = (const uint32_t *)src;
    uint32_t *dest32 = display_work_buffer() + dest_y*DISPLAY_STRIDE + dest_x;
    int y;
    for (y = 0; y < src_height;
         y++, src32 += src_stride, dest32 += DISPLAY_STRIDE
    ) {
        memcpy(dest32, src32, src_width*4);
    }

#endif
}

/*-----------------------------------------------------------------------*/

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
void display_blit_scaled(const void *src, int src_width, int src_height,
                         int src_stride, int dest_x, int dest_y,
                         int dest_width, int dest_height)
{
    /* Pointer must be 64-byte aligned, so if it's not, adjust the pointer
     * and add an offset to the X coordinate */
    const unsigned int xofs = ((uintptr_t)src & 0x3F) / 4;
    const uint32_t * const src_aligned = (const uint32_t *)src - xofs;

    /* Set up the vertex array (4 vertices for a 2-triangle strip) */
    struct {
        uint16_t u, v;
        int16_t x, y, z;
    } *vertices = guGetMemory(sizeof(*vertices) * 4);
    vertices[0].u = xofs;
    vertices[0].v = 0;
    vertices[0].x = dest_x;
    vertices[0].y = dest_y;
    vertices[0].z = 0;
    vertices[1].u = xofs;
    vertices[1].v = src_height;
    vertices[1].x = dest_x;
    vertices[1].y = dest_y + dest_height;
    vertices[1].z = 0;
    vertices[2].u = xofs + src_width;
    vertices[2].v = 0;
    vertices[2].x = dest_x + dest_width;
    vertices[2].y = dest_y;
    vertices[2].z = 0;
    vertices[3].u = xofs + src_width;
    vertices[3].v = src_height;
    vertices[3].x = dest_x + dest_width;
    vertices[3].y = dest_y + dest_height;
    vertices[3].z = 0;

    /* Draw the array */
    guEnable(GU_TEXTURE_2D);
    guTexFilter(GU_LINEAR, GU_LINEAR);
    guTexWrap(GU_CLAMP, GU_CLAMP);
    guTexFunc(GU_TFX_REPLACE, GU_TCC_RGB);
    guTexFlush();
    guTexMode(GU_PSM_8888, 0, 0, 0);
    guTexImage(0, 512, 512, src_stride, src_aligned);
    guDrawArray(GU_TRIANGLE_STRIP,
                GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                4, NULL, vertices);
    guDisable(GU_TEXTURE_2D);
}

/*************************************************************************/

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
void display_fill_box(int x1, int y1, int x2, int y2, uint32_t color)
{
    struct {
        uint32_t color;
        int16_t x, y, z, pad;
    } *vertices = guGetMemory(sizeof(*vertices) * 2);
    vertices[0].color = color;
    vertices[0].x = x1;
    vertices[0].y = y1;
    vertices[0].z = 0;
    vertices[1].color = color;
    vertices[1].x = x2 + 1;
    vertices[1].y = y2 + 1;
    vertices[1].z = 0;

    guDrawArray(GU_SPRITES,
                GU_COLOR_8888 | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                2, NULL, vertices);
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * buffer_swap_thread:  Perform a buffer swap and clear the "swap_pending"
 * variable to false.  Designed to run as a background thread while the
 * main emulation proceeds.
 *
 * [Parameters]
 *     args, argp: Thread argument size and pointer (unused)
 * [Return value]
 *     Does not return
 */
static int buffer_swap_thread(SceSize args, void *argp)
{
    do_buffer_swap();
    swap_pending = 0;
    sceKernelExitDeleteThread(0);
}

/*----------------------------------*/

/**
 * do_buffer_swap:  Perform a display buffer swap (call guSync(), swap the
 * display and work surfaces, wait for the following vertical blank, and
 * calculate the length of time between this newly displayed frame and the
 * previous one).  Called either from the buffer swap thread or (if the
 * swap thread fails to start) from the main thread.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void do_buffer_swap(void)
{
    guSync(0, 0);
    sceDisplaySetFrameBuf(surfaces[work_surface], DISPLAY_STRIDE,
                          display_mode, PSP_DISPLAY_SETBUF_NEXTFRAME);
    displayed_surface = work_surface;
    work_surface = (work_surface + 1) % lenof(surfaces);
    sceDisplayWaitVblankStart();

    /* Update the frame length variables.  If this is the first frame
     * we've drawn (signaled by last_frame_start == 0), just set a frame
     * length of 1 (1/60 sec) since we have nothing to compare it against. */
    const uint32_t now = sceKernelGetSystemTimeLow();
    const uint32_t last_frame_time = now - last_frame_start;
    const uint32_t time_unit = (1001000+30)/60;
    if (last_frame_start != 0) {
        last_frame_length = (last_frame_time + time_unit/2) / time_unit;
    } else {
        last_frame_length = 1;
    }
    /* Make sure we don't accidentally signal the next frame as the
     * first frame drawn. */
    last_frame_start = (now != 0) ? now : 1;
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
