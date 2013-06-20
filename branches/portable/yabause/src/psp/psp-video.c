/*  src/psp/psp-video.c: PSP video interface module
    Copyright 2009-2010 Andrew Church
    Based on src/vidogl.c by Guillaume Duhamel and others

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

#include "../vdp1.h"
#include "../vdp2.h"
#include "../vidshared.h"

#include "config.h"
#include "display.h"
#include "font.h"
#include "gu.h"
#include "misc.h"
#include "psp-video.h"
#include "psp-video-internal.h"
#include "texcache.h"
#include "timing.h"

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_video_init(void);
static void psp_video_deinit(void);
static void psp_video_resize(unsigned int width, unsigned int height,
                             int fullscreen);
static int psp_video_is_fullscreen(void);
static void psp_video_debug_message(char *format, ...);

static int psp_vdp1_reset(void);
static void psp_vdp1_draw_start(void);
static void psp_vdp1_draw_end(void);
static void psp_vdp1_normal_sprite_draw(void);
static void psp_vdp1_scaled_sprite_draw(void);
static void psp_vdp1_distorted_sprite_draw(void);
static void psp_vdp1_polygon_draw(void);
static void psp_vdp1_polyline_draw(void);
static void psp_vdp1_line_draw(void);
static void psp_vdp1_user_clipping(void);
static void psp_vdp1_system_clipping(void);
static void psp_vdp1_local_coordinate(void);

static int psp_vdp2_reset(void);
static void psp_vdp2_draw_start(void);
static void psp_vdp2_draw_end(void);
static void psp_vdp2_draw_screens(void);
static void psp_vdp2_set_resolution(u16 TVMD);
static void FASTCALL psp_vdp2_set_priority_NBG0(int priority);
static void FASTCALL psp_vdp2_set_priority_NBG1(int priority);
static void FASTCALL psp_vdp2_set_priority_NBG2(int priority);
static void FASTCALL psp_vdp2_set_priority_NBG3(int priority);
static void FASTCALL psp_vdp2_set_priority_RBG0(int priority);

/*-----------------------------------------------------------------------*/

/* Module interface definition */

VideoInterface_struct VIDPSP = {
    .id                   = VIDCORE_PSP,
    .Name                 = "PSP Video Interface",
    .Init                 = psp_video_init,
    .DeInit               = psp_video_deinit,
    .Resize               = psp_video_resize,
    .IsFullscreen         = psp_video_is_fullscreen,
    .OnScreenDebugMessage = psp_video_debug_message,

    .Vdp1Reset            = psp_vdp1_reset,
    .Vdp1DrawStart        = psp_vdp1_draw_start,
    .Vdp1DrawEnd          = psp_vdp1_draw_end,
    .Vdp1NormalSpriteDraw = psp_vdp1_normal_sprite_draw,
    .Vdp1ScaledSpriteDraw = psp_vdp1_scaled_sprite_draw,
    .Vdp1DistortedSpriteDraw = psp_vdp1_distorted_sprite_draw,
    .Vdp1PolygonDraw      = psp_vdp1_polygon_draw,
    .Vdp1PolylineDraw     = psp_vdp1_polyline_draw,
    .Vdp1LineDraw         = psp_vdp1_line_draw,
    .Vdp1UserClipping     = psp_vdp1_user_clipping,
    .Vdp1SystemClipping   = psp_vdp1_system_clipping,
    .Vdp1LocalCoordinate  = psp_vdp1_local_coordinate,

    .Vdp2Reset            = psp_vdp2_reset,
    .Vdp2DrawStart        = psp_vdp2_draw_start,
    .Vdp2DrawEnd          = psp_vdp2_draw_end,
    .Vdp2DrawScreens      = psp_vdp2_draw_screens,
    .Vdp2SetResolution    = psp_vdp2_set_resolution,
    .Vdp2SetPriorityNBG0  = psp_vdp2_set_priority_NBG0,
    .Vdp2SetPriorityNBG1  = psp_vdp2_set_priority_NBG1,
    .Vdp2SetPriorityNBG2  = psp_vdp2_set_priority_NBG2,
    .Vdp2SetPriorityNBG3  = psp_vdp2_set_priority_NBG3,
    .Vdp2SetPriorityRBG0  = psp_vdp2_set_priority_RBG0,
};

/*************************************************************************/
/************************* Global and local data *************************/
/*************************************************************************/

/**** Exported data ****/

/* Color table generated from VDP2 color RAM */
__attribute__((aligned(64))) uint16_t global_clut_16[0x800];
__attribute__((aligned(64))) uint32_t global_clut_32[0x800];

/* Displayed width and height */
unsigned int disp_width, disp_height;

/* Scale (right-shift) applied to X and Y coordinates */
unsigned int disp_xscale, disp_yscale;

/* Total number of frames to skip before we draw the next one */
unsigned int frames_to_skip;

/* Number of frames skipped so far since we drew the last one */
unsigned int frames_skipped;

/* VDP1 color component offset values (-0xFF...+0xFF) */
int32_t vdp1_rofs, vdp1_gofs, vdp1_bofs;

/*-----------------------------------------------------------------------*/

/**** Internal data ****/

/*----------------------------------*/

/* Pending infoline text (malloc()ed, or NULL if none) and color */
static char *infoline_text;
static uint32_t infoline_color;

/*----------------------------------*/

/* Current average frame rate (rolling average) */
static float average_fps;

/* Flag indicating whether graphics should be drawn this frame */
static uint8_t draw_graphics;

/* Background priorities (NBG0, NBG1, NBG2, NBG3, RBG0) */
static uint8_t bg_priority[5];

/*----------------------------------*/

/* Custom drawing function specified for each background layer */
static CustomDrawRoutine *custom_draw_func[5];

/* Is the RBG0 drawing function fast enough to consider it a normal layer
 * for timing purposes? */
static uint8_t RBG0_draw_func_is_fast;

/* Did we draw a slow RBG0 this frame? */
static uint8_t drew_slow_RBG0;

/*----------------------------------*/

/* Rendering data for sprites, polygons, and lines (a copy of all
 * parameters except priority passed to vdp1_render_queue() */
typedef struct VDP1RenderData_ {
    uint32_t texture_key;
    int primitive;
    int vertex_type;
    int count;
    const void *indices;
    const void *vertices;
} VDP1RenderData;

/* VDP1 render queues (one for each priority level) */
typedef struct VDP1RenderQueue_ {
    VDP1RenderData *queue;  // Array of entries (dynamically expanded)
    int size;               // Size of queue array, in entries
    int len;                // Number of entries currently in array
} VDP1RenderQueue;
static VDP1RenderQueue vdp1_queue[8];

/* Amount to expand a queue's array when it gets full */
#define VDP1_QUEUE_EXPAND_SIZE  1000

/*----------------------------------*/

/* Flags indicating whether each 4k page of VDP1/2 RAM contains any
 * persistently-cached texture data */
static uint8_t vdp1_page_cached[0x80], vdp2_page_cached[0x80];

/* Checksum of each VDP1/2 RAM page containing cached texture data */
static uint32_t vdp1_page_checksum[0x80], vdp2_page_checksum[0x80];

/* State of color offset settings at last cache reset */
static uint32_t vdp1_cached_cofs;
static uint32_t vdp2_cached_cofs_regs;  // CLOFEN<<16 | CLOFSL
static uint32_t vdp2_cached_cofs_A, vdp2_cached_cofs_B;

/*************************************************************************/

/**** Local function declarations ****/

static int vdp1_is_persistent(vdp1cmd_struct *cmd);
static void vdp1_draw_lines(vdp1cmd_struct *cmd, int poly);
static void vdp1_draw_quad(vdp1cmd_struct *cmd, int textured);
static uint32_t vdp1_convert_color(uint16_t color16, int textured,
                                   unsigned int CMDPMOD);
static uint32_t vdp1_get_cmd_color(vdp1cmd_struct *cmd);
static uint32_t vdp1_get_cmd_color_pri(vdp1cmd_struct *cmd, int textured,
                                       int *priority_ret);
static uint16_t vdp1_process_sprite_color(uint16_t color16, int *priority_ret,
                                          int *alpha_ret);
static uint32_t vdp1_cache_sprite_texture(
    vdp1cmd_struct *cmd, int width, int height, int *priority_ret,
    int *alpha_ret);
static inline void vdp1_queue_render(
    int priority, uint32_t texture_key, int primitive,
    int vertex_type, int count, const void *indices, const void *vertices);
static void vdp1_run_queue(int priority);

static inline void vdp2_get_color_offsets(uint16_t mask, int32_t *rofs_ret,
                                          int32_t *gofs_ret, int32_t *bofs_ret);

static void vdp2_draw_bg(void);
static void vdp2_draw_graphics(int layer);

/*************************************************************************/
/********************** General interface functions **********************/
/*************************************************************************/

/**
 * psp_video_init:  Initialize the peripheral interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_video_init(void)
{
    /* Set some reasonable defaults. */
    disp_width = 320;
    disp_height = 224;
    disp_xscale = 0;
    disp_yscale = 0;

    /* Always draw the first frame. */
    frames_to_skip = 0;
    frames_skipped = 0;

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_video_deinit:  Shut down the peripheral interface.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_video_deinit(void)
{
    /* We don't implement shutting down, so nothing to do. */
}

/*************************************************************************/

/**
 * psp_video_resize:  Resize the display window.  A no-op on PSP.
 *
 * [Parameters]
 *          width: New window width
 *         height: New window height
 *     fullscreen: Nonzero to use fullscreen mode, else zero
 * [Return value]
 *     None
 */
static void psp_video_resize(unsigned int width, unsigned int height,
                             int fullscreen)
{
}

/*************************************************************************/

/**
 * psp_video_is_fullscreen:  Return whether the display is currently in
 * fullscreen mode.  Always returns true (nonzero) on PSP.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if in fullscreen mode, else zero
 */
static int psp_video_is_fullscreen(void)
{
    return 1;
}

/*************************************************************************/

/**
 * psp_video_debug_message:  Display a debug message on the screen.
 *
 * [Parameters]
 *     format: printf()-style format string
 * [Return value]
 *     None
 */
static void psp_video_debug_message(char *format, ...)
{
    /* Not implemented */
}

/*************************************************************************/
/********************* PSP-only interface functions **********************/
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
void psp_video_infoline(uint32_t color, const char *text)
{
    infoline_text = strdup(text);
    if (UNLIKELY(!infoline_text)) {
        DMSG("Failed to strdup(%s)", text);
    }
    infoline_color = color;
}

/*************************************************************************/

/**
 * psp_video_set_draw_routine:  Set a custom drawing routine for a specific
 * graphics layer.  If "is_fast" is true when setting a routine for RBG0,
 * the frame rate will not be halved regardless of the related setting in
 * the configuration menu.
 *
 * [Parameters]
 *       layer: Graphics layer (BG_*)
 *        func: Drawing routine (NULL to clear any previous setting)
 *     is_fast: For BG_RBG0, indicates whether the routine is fast enough
 *                 to be considered a non-distorted layer for the purposes
 *                 of frame rate adjustment; ignored for other layers
 * [Return value]
 *     None
 */
void psp_video_set_draw_routine(int layer, CustomDrawRoutine *func,
                                int is_fast)
{
    PRECOND(layer >= BG_NBG0 && layer <= BG_RBG0, return);
    custom_draw_func[layer] = func;
    if (layer == BG_RBG0) {
        RBG0_draw_func_is_fast = is_fast;
    }
}

/*************************************************************************/

/**
 * vdp2_is_persistent:  Return whether the tile at the given address in
 * VDP2 RAM is persistently cacheable.
 *
 * [Parameters]
 *     address: Tile address in VDP2 RAM
 * [Return value]
 *     Nonzero if tile texture can be persistently cached, else zero
 */
int vdp2_is_persistent(uint32_t address)
{
    const unsigned int page = address >> 12;
    if (!vdp2_page_cached[page]) {
        vdp2_page_checksum[page] =
            checksum_fast32((const uint32_t *)(Vdp2Ram + (page<<12)), 1024);
        vdp2_page_cached[page] = 1;
    }
    return 1;
}

/*************************************************************************/
/******************* VDP1-specific interface functions *******************/
/*************************************************************************/

/**
 * psp_vdp1_reset:  Reset the VDP1 state.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Unknown (always zero)
 */
static int psp_vdp1_reset(void)
{
    /* Nothing to do. */
    return 0;
}

/*************************************************************************/

/**
 * psp_vdp1_draw_start:  Prepare for VDP1 drawing.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_draw_start(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    /* Clear out all the rendering queues (just to be safe). */
    int priority;
    for (priority = 0; priority < 8; priority++) {
        vdp1_queue[priority].len = 0;
    }

    /* Get the color offsets. */
    vdp2_get_color_offsets(1<<6, &vdp1_rofs, &vdp1_gofs, &vdp1_bofs);
}

/*************************************************************************/


/**
 * psp_vdp1_draw_end:  Finish VDP1 drawing.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_draw_end(void)
{
    /* Nothing to do */
}

/*************************************************************************/


/**
 * psp_vdp1_normal_sprite_draw:  Draw an unscaled rectangular sprite.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_normal_sprite_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);

    int width  = ((cmd.CMDSIZE >> 8) & 0x3F) * 8;
    int height = cmd.CMDSIZE & 0xFF;
    cmd.CMDXB = cmd.CMDXA + width; cmd.CMDYB = cmd.CMDYA;
    cmd.CMDXC = cmd.CMDXA + width; cmd.CMDYC = cmd.CMDYA + height;
    cmd.CMDXD = cmd.CMDXA;         cmd.CMDYD = cmd.CMDYA + height;

    vdp1_draw_quad(&cmd, 1);
}

/*************************************************************************/

/**
 * psp_vdp1_scaled_sprite_draw:  Draw a scaled rectangular sprite.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_scaled_sprite_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);

    if (!(cmd.CMDCTRL & 0x0F00)) {
        /* Size is directly specified. */
        cmd.CMDXC++;           cmd.CMDYC++;
        cmd.CMDXB = cmd.CMDXC; cmd.CMDYB = cmd.CMDYA;
        cmd.CMDXD = cmd.CMDXA; cmd.CMDYD = cmd.CMDYC;
    } else {
        /* Scale around a particular point (left/top, center, right/bottom). */
        int new_w = cmd.CMDXB + 1;
        int new_h = cmd.CMDYB + 1;
        if ((cmd.CMDCTRL & 0x300) == 0x200) {
            cmd.CMDXA -= cmd.CMDXB / 2;
        } else if ((cmd.CMDCTRL & 0x300) == 0x300) {
            cmd.CMDXA -= cmd.CMDXB;
        }
        if ((cmd.CMDCTRL & 0xC00) == 0x800) {
            cmd.CMDYA -= cmd.CMDYB / 2;
        } else if ((cmd.CMDCTRL & 0xC00) == 0xC00) {
            cmd.CMDYA -= cmd.CMDYB;
        }
        cmd.CMDXB = cmd.CMDXA + new_w; cmd.CMDYB = cmd.CMDYA;
        cmd.CMDXC = cmd.CMDXA + new_w; cmd.CMDYC = cmd.CMDYA + new_h;
        cmd.CMDXD = cmd.CMDXA;         cmd.CMDYD = cmd.CMDYA + new_h;
    }

    vdp1_draw_quad(&cmd, 1);
}

/*************************************************************************/

/**
 * psp_vdp1_distorted_sprite_draw:  Draw a sprite on an arbitrary
 * quadrilateral.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_distorted_sprite_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);
    vdp1_draw_quad(&cmd, 1);
}

/*************************************************************************/

/**
 * psp_vdp1_polygon_draw:  Draw an untextured quadrilateral.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_polygon_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);
    vdp1_draw_quad(&cmd, 0);
}

/*************************************************************************/

/**
 * psp_vdp1_polyline_draw:  Draw four connected lines.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_polyline_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);
    vdp1_draw_lines(&cmd, 1);
}

/*************************************************************************/

/**
 * psp_vdp1_line_draw:  Draw a single line.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_line_draw(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    vdp1cmd_struct cmd;
    Vdp1ReadCommand(&cmd, Vdp1Regs->addr);
    vdp1_draw_lines(&cmd, 0);
}

/*************************************************************************/

/**
 * psp_vdp1_user_clipping:  Set the user clipping coordinates.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_user_clipping(void)
{
    Vdp1Regs->userclipX1 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0xC);
    Vdp1Regs->userclipY1 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0xE);
    Vdp1Regs->userclipX2 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0x14);
    Vdp1Regs->userclipY2 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0x16);
}

/*************************************************************************/

/**
 * psp_vdp1_system_clipping:  Set the system clipping coordinates.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_system_clipping(void)
{
    Vdp1Regs->systemclipX1 = 0;
    Vdp1Regs->systemclipY1 = 0;
    Vdp1Regs->systemclipX2 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0x14);
    Vdp1Regs->systemclipY2 = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0x16);
}

/*************************************************************************/

/**
 * psp_vdp1_local_coordinate:  Set coordinate offset values used in drawing
 * primitives.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp1_local_coordinate(void)
{
    Vdp1Regs->localX = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0xC);
    Vdp1Regs->localY = T1ReadWord(Vdp1Ram, Vdp1Regs->addr + 0xE);
}

/*************************************************************************/
/******************* VDP2-specific interface functions *******************/
/*************************************************************************/

/**
 * psp_vdp2_reset:  Reset the VDP2 state.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Unknown (always zero)
 */
static int psp_vdp2_reset(void)
{
    /* Nothing to do */
    return 0;
}

/*************************************************************************/

/**
 * psp_vdp2_draw_start:  Begin drawing a video frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp2_draw_start(void)
{
    /* Apply any game-specific optimizations or tweaks.  (This may involve
     * adjusting the frame-skip variables, so we call it before the
     * frame-skip check.) */
    psp_video_apply_tweaks();

    /* If we're skipping this frame, we don't do anything, not even start
     * a new output frame (because that forces a VBlank sync, which may
     * waste time if the previous frame completed quickly). */
    if (frames_skipped < frames_to_skip) {
        return;
    }

    /* Load the global color lookup tables from VDP2 color RAM. */
    const uint16_t *cram = (const uint16_t *)Vdp2ColorRam;
    if (Vdp2Internal.ColorMode == 2) {  // 32-bit color table
        int i;
        for (i = 0; i < 0x400; i++) {
            uint16_t xb = cram[i*2+0];
            uint16_t gr = cram[i*2+1];
            uint16_t color16 = 0x8000 | (xb<<7 & 0x7C00)
                                      | (gr<<2 & 0x3E0) | (gr>>3 & 0x1F);
            uint32_t color32 = 0xFF000000 | xb<<16 | gr;
            global_clut_16[i] = color16;
            global_clut_16[i+0x400] = color16;
            global_clut_32[i] = color32;
            global_clut_32[i+0x400] = color32;
        }
    } else {  // 16-bit color table
        int i;
        for (i = 0; i < 0x800; i++) {
            uint16_t color16 = 0x8000 | cram[i];
            uint32_t color32 = 0xFF000000 | (color16 & 0x7C00) << 9
                                          | (color16 & 0x03E0) << 6
                                          | (color16 & 0x001F) << 3;
            global_clut_16[i] = color16;
            global_clut_32[i] = color32;
        }
    }

    /* Start a new frame. */
    display_set_size(disp_width >> disp_xscale, disp_height >> disp_yscale);
    display_begin_frame();

    /* Clear the texture cache of transient data; also clear persistent
     * data if any source RAM or color offsets were changed, or if
     * persistent caching is disabled in the first place. */
    const uint32_t vdp1_cofs = (vdp1_rofs & 0x1FF) << 18
                             | (vdp1_gofs & 0x1FF) <<  9
                             | (vdp1_bofs & 0x1FF) <<  0;
    const uint32_t vdp2_cofs_regs = Vdp2Regs->CLOFEN << 16 | Vdp2Regs->CLOFSL;
    const uint32_t vdp2_cofs_A = (Vdp2Regs->COAR & 0x1FF) << 18
                               | (Vdp2Regs->COAG & 0x1FF) <<  9
                               | (Vdp2Regs->COAB & 0x1FF) <<  0;
    const uint32_t vdp2_cofs_B = (Vdp2Regs->COBR & 0x1FF) << 18
                               | (Vdp2Regs->COBG & 0x1FF) <<  9
                               | (Vdp2Regs->COBB & 0x1FF) <<  0;
    int need_reset = 0;
    if (!config_get_cache_textures()) {
        need_reset = 1;
    } else if (vdp1_cofs      != vdp1_cached_cofs
            || vdp2_cofs_regs != vdp2_cached_cofs_regs
            || vdp2_cofs_A    != vdp2_cached_cofs_A
            || vdp2_cofs_B    != vdp2_cached_cofs_B) {
        DMSG("Color offsets changed, clearing cache");
        need_reset = 1;
    } else {
        unsigned int page;
        for (page = 0; page < 0x80; page++) {
            if (vdp1_page_cached[page]) {
                const uint32_t sum =
                    checksum_fast32((const uint32_t *)(Vdp1Ram + (page<<12)), 1024);
                if (sum != vdp1_page_checksum[page]) {
                    DMSG("VDP1 page 0x%05X checksum changed (%08X -> %08X),"
                         " clearing cache",
                         page<<12, vdp1_page_checksum[page], sum);
                    need_reset = 1;
                    break;
                }
            }
            if (vdp2_page_cached[page]) {
                const uint32_t sum =
                    checksum_fast32((const uint32_t *)(Vdp2Ram + (page<<12)), 1024);
                if (sum != vdp2_page_checksum[page]) {
                    DMSG("VDP2 page 0x%05X checksum changed (%08X -> %08X),"
                         " clearing cache",
                         page<<12, vdp2_page_checksum[page], sum);
                    need_reset = 1;
                    break;
                }
            }
        }
    }
    if (need_reset) {
        texcache_reset();
        memset(vdp1_page_cached, 0, sizeof(vdp1_page_cached));
        memset(vdp2_page_cached, 0, sizeof(vdp2_page_cached));
    } else {
        texcache_clean();
    }
    vdp1_cached_cofs = vdp1_cofs;
    vdp2_cached_cofs_regs = vdp2_cofs_regs;
    vdp2_cached_cofs_A = vdp2_cofs_A;
    vdp2_cached_cofs_B = vdp2_cofs_B;

    /* Initialize the render state. */
    guTexFilter(GU_NEAREST, GU_NEAREST);
    guTexWrap(GU_CLAMP, GU_CLAMP);
    guTexFunc(GU_TFX_MODULATE, GU_TCC_RGBA);
    guBlendFunc(GU_ADD, GU_SRC_ALPHA, GU_ONE_MINUS_SRC_ALPHA, 0, 0);
    guEnable(GU_BLEND);  // We treat everything as alpha-enabled

    /* Reset the draw-graphics flag (it will be set by draw_screens() if
     * graphics are active). */
    draw_graphics = 0;
}

/*************************************************************************/

/**
 * psp_vdp2_draw_end:  Finish drawing a video frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp2_draw_end(void)
{
    if (frames_skipped >= frames_to_skip) {

        /* Draw all graphics by priority. */
        int priority;
        for (priority = 0; priority < 8; priority++) {
            /* Draw background graphics first... */
            if (draw_graphics && priority > 0) {
                if (bg_priority[BG_NBG3] == priority) {
                    vdp2_draw_graphics(BG_NBG3);
                }
                if (bg_priority[BG_NBG2] == priority) {
                    vdp2_draw_graphics(BG_NBG2);
                }
                if (bg_priority[BG_NBG1] == priority) {
                    vdp2_draw_graphics(BG_NBG1);
                }
                if (bg_priority[BG_NBG0] == priority) {
                    vdp2_draw_graphics(BG_NBG0);
                }
                if (bg_priority[BG_RBG0] == priority) {
                    vdp2_draw_graphics(BG_RBG0);
                }
            }
            /* Then draw sprites on top... */
            vdp1_run_queue(priority);
            /* And clear the rendering queue. */
            vdp1_queue[priority].len = 0;
        }

        /* Always compute average FPS (even if we're not showing it), so
         * the value is accurate as soon as the display is turned on.
         * We use a rolling average that decays by 50% every second. */
        unsigned int frame_length = display_last_frame_length();
        if (frame_length == 0) {
            frame_length = 1;  // Just in case (avoid division by 0)
        }
        unsigned int frame_count = 1 + frames_skipped;
        const float fps = (frame_count*60.0f) / frame_length;
        if (!average_fps) {
            /* When first starting up, just set the average to the first
             * frame's frame rate. */
            average_fps = fps;
        } else {
            const float weight = powf(2.0f, -(1/fps));
            average_fps = (average_fps * weight) + (fps * (1-weight));
        }
        if (config_get_show_fps()) {
            unsigned int show_fps = iroundf(average_fps*10);
            if (show_fps > 600) {
                /* FPS may momentarily exceed 60.0 due to timing jitter,
                 * but we never show more than 60.0. */
                show_fps = 600;
            }
            font_printf((disp_width >> disp_xscale) - 2, 2, 1, 0xAAFF8040,
                        "FPS: %2d.%d (%d/%2d)", show_fps/10, show_fps%10,
                        frame_count, frame_length);
        }

        if (infoline_text) {
            font_printf((disp_width >> disp_xscale) / 2,
                        (disp_height >> disp_yscale) - FONT_HEIGHT - 2, 0,
                        infoline_color, "%s", infoline_text);
            free(infoline_text);
            infoline_text = NULL;
        }

        display_end_frame();

    }  // if (frames_skipped >= frames_to_skip)

    if (frames_skipped < frames_to_skip) {
        frames_skipped++;
        timing_skip_next_sync();  // Let the emulation continue uninterrupted
    } else {
        frames_skipped = 0;
        if (config_get_frameskip_auto()) {
            // FIXME: auto frame skipping not yet implemented
            frames_to_skip = 0;
        } else {
            frames_to_skip = config_get_frameskip_num();
        }
        if (drew_slow_RBG0) {
            frames_to_skip += 1 + frames_to_skip;
        }
        if (disp_height > 272 && frames_to_skip == 0
         && config_get_frameskip_interlace()
        ) {
            frames_to_skip = 1;
        }
        drew_slow_RBG0 = 0;
    }
}

/*************************************************************************/

/**
 * psp_vdp2_draw_screens:  Draw the VDP2 background and graphics layers.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_vdp2_draw_screens(void)
{
    if (frames_skipped < frames_to_skip) {
        return;
    }

    /* Draw the background color(s). */
    vdp2_draw_bg();

    /* Flag the background graphics to be drawn. */
    draw_graphics = 1;
}

/*************************************************************************/

/**
 * psp_vdp2_set_resolution:  Change the resolution of the Saturn display.
 *
 * [Parameters]
 *     TVMD: New value of the VDP2 TVMD register
 * [Return value]
 *     None
 */
static void psp_vdp2_set_resolution(u16 TVMD)
{
    /* Set the display width from bits 0-1. */
    disp_width = (TVMD & 1) ? 352 : 320;
    if (TVMD & 2) {
        disp_width *= 2;
    }

    /* Set the display height from bits 4-5.  Note that 0x30 is an invalid
     * value for these bits and should not occur in practice; valid heights
     * are 0x00=224, 0x10=240, and (for PAL) 0x20=256. */
    disp_height = 224 + (TVMD & 0x30);
    if ((TVMD & 0xC0) == 0xC0) {
        disp_height *= 2;  // Interlaced mode
    }

    /* Hi-res or interlaced displays won't fit on the PSP screen, so cut
     * everything in half when using them. */
    disp_xscale = (disp_width  > 352);
    disp_yscale = (disp_height > 256);
}

/*************************************************************************/

/**
 * psp_vdp2_set_priority_{NBG[0-3],RBG0}:  Set the priority of the given
 * background graphics layer.
 *
 * [Parameters]
 *     priority: Priority to set
 * [Return value]
 *     None
 */
static void FASTCALL psp_vdp2_set_priority_NBG0(int priority)
{
    bg_priority[BG_NBG0] = priority;
}

static void FASTCALL psp_vdp2_set_priority_NBG1(int priority)
{
    bg_priority[BG_NBG1] = priority;
}

static void FASTCALL psp_vdp2_set_priority_NBG2(int priority)
{
    bg_priority[BG_NBG2] = priority;
}

static void FASTCALL psp_vdp2_set_priority_NBG3(int priority)
{
    bg_priority[BG_NBG3] = priority;
}

static void FASTCALL psp_vdp2_set_priority_RBG0(int priority)
{
    bg_priority[BG_RBG0] = priority;
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * vdp1_is_persistent:  Return whether the given sprite drawing command
 * identifies a texture in a persistently-cacheable area of VDP1 RAM.
 *
 * [Parameters]
 *     cmd: VDP1 command structure
 * [Return value]
 *     Nonzero if texture can be persistently cached, else zero
 */
static int vdp1_is_persistent(vdp1cmd_struct *cmd)
{
    const unsigned int first_page = cmd->CMDSRCA >> 9;
    const unsigned int width_8    = (cmd->CMDSIZE >> 8) & 0x3F;
    const unsigned int height     = cmd->CMDSIZE & 0xFF;
    const unsigned int last_page  = (cmd->CMDSRCA + (width_8 * height)) >> 9;
    unsigned int page;
    for (page = first_page; page <= last_page; page++) {
        if (!vdp1_page_cached[page]) {
            vdp1_page_checksum[page] =
                checksum_fast32((const uint32_t *)(Vdp1Ram + (page<<12)), 1024);
            vdp1_page_cached[page] = 1;
        }
    }
    if ((cmd->CMDPMOD>>3 & 7) == 1) {
        page = cmd->CMDCOLR >> 9;
        if (!vdp1_page_cached[page]) {
            vdp1_page_checksum[page] =
                checksum_fast32((const uint32_t *)(Vdp1Ram + (page<<12)), 1024);
            vdp1_page_cached[page] = 1;
        }
    }
    return 1;
}

/*************************************************************************/

/**
 * vdp1_draw_lines:  Draw one or four lines based on the given VDP1 command.
 *
 * [Parameters]
 *      cmd: VDP1 command pointer
 *     poly: Nonzero = draw four connected lines, zero = draw a single line
 * [Return value]
 *     None
 */
static void vdp1_draw_lines(vdp1cmd_struct *cmd, int poly)
{
    /* Get the line color and priority. */
    // FIXME: vidogl.c suggests that the priority processing done for
    // sprites and polygons is not done here; is that correct?
    const uint32_t color32 = vdp1_get_cmd_color(cmd);
    const int priority = Vdp2Regs->PRISA & 0x7;

    /* If it's Gouraud-shaded, pick up the four endpoint colors.  (Only
     * the first two of these are used for single lines.) */
    uint32_t color_A, color_B, color_C, color_D;
    if (cmd->CMDPMOD & 4) {  // Gouraud shading bit
        const uint32_t alpha = color32 & 0xFF000000;
        if (vdp1_rofs | vdp1_gofs | vdp1_bofs) {
            unsigned int temp_A, temp_B, temp_C, temp_D;
            temp_A = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 0);
            temp_B = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 2);
            temp_C = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 4);
            temp_D = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 6);
            color_A = alpha | (adjust_color_16_32(temp_A, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_B = alpha | (adjust_color_16_32(temp_B, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_C = alpha | (adjust_color_16_32(temp_C, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_D = alpha | (adjust_color_16_32(temp_D, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
        } else {
            unsigned int temp_A, temp_B, temp_C, temp_D;
            temp_A = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 0);
            temp_B = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 2);
            temp_C = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 4);
            temp_D = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + 6);
            color_A = alpha | (temp_A & 0x7C00) << 9
                            | (temp_A & 0x03E0) << 6
                            | (temp_A & 0x001F) << 3;
            color_B = alpha | (temp_B & 0x7C00) << 9
                            | (temp_B & 0x03E0) << 6
                            | (temp_B & 0x001F) << 3;
            color_C = alpha | (temp_C & 0x7C00) << 9
                            | (temp_C & 0x03E0) << 6
                            | (temp_C & 0x001F) << 3;
            color_D = alpha | (temp_D & 0x7C00) << 9
                            | (temp_D & 0x03E0) << 6
                            | (temp_D & 0x001F) << 3;
        }
    } else {
        color_A = color_B = color_C = color_D = color32;
    }

    /* Set up the vertex array. */
    int nvertices = poly ? 5 : 2;
    struct {uint32_t color; int16_t x, y, z, pad;} *vertices;
    vertices = pspGuGetMemoryMerge(sizeof(*vertices) * nvertices);
    vertices[0].color = color_A;
    vertices[0].x = (cmd->CMDXA + Vdp1Regs->localX) >> disp_xscale;
    vertices[0].y = (cmd->CMDYA + Vdp1Regs->localY) >> disp_yscale;
    vertices[0].z = 0;
    vertices[1].color = color_B;
    vertices[1].x = (cmd->CMDXB + Vdp1Regs->localX) >> disp_xscale;
    vertices[1].y = (cmd->CMDYB + Vdp1Regs->localY) >> disp_xscale;
    vertices[1].z = 0;
    if (poly) {
        vertices[2].color = color_C;
        vertices[2].x = (cmd->CMDXC + Vdp1Regs->localX) >> disp_xscale;
        vertices[2].y = (cmd->CMDYC + Vdp1Regs->localY) >> disp_yscale;
        vertices[2].z = 0;
        vertices[3].color = color_D;
        vertices[3].x = (cmd->CMDXD + Vdp1Regs->localX) >> disp_xscale;
        vertices[3].y = (cmd->CMDYD + Vdp1Regs->localY) >> disp_yscale;
        vertices[3].z = 0;
        vertices[4] = vertices[0];
    }

    /* Queue the line(s). */
    vdp1_queue_render(priority, 0, GU_LINE_STRIP,
                      GU_COLOR_8888 | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                      nvertices, NULL, vertices);
}

/*************************************************************************/

/**
 * vdp1_draw_quad:  Draw a quadrilateral based on the given VDP1 command.
 *
 * [Parameters]
 *          cmd: VDP1 command pointer
 *     textured: Nonzero if the quadrilateral is textured (i.e. a sprite)
 * [Return value]
 *     None
 */
static void vdp1_draw_quad(vdp1cmd_struct *cmd, int textured)
{
    /* Get the width, height, and flip arguments for sprites (unused for
     * untextured polygons). */

    int width, height;
    unsigned int gouraud_flip = 0;  // XOR bitmask for Gouraud color addresses
    if (textured) {
        width  = ((cmd->CMDSIZE >> 8) & 0x3F) * 8;
        height = cmd->CMDSIZE & 0xFF;
        if (width == 0 || height == 0) {
            return;
        }
        /* If flipping is specified, swap the relevant coordinates in the
         * "cmd" structure; this helps avoid texture glitches when the
         * vertex order of a texture is changed (e.g. Panzer Dragoon Saga).
         * We use inline assembly so we can load and store in 32-bit units
         * without GCC complaining about strict aliasing violations. */
        switch (cmd->CMDCTRL & 0x30) {
          case 0x10: {  // Flip horizontally
            gouraud_flip = 2;
            uint32_t tempA, tempB, tempC, tempD;
            asm(".set push; .set noreorder\n"
                "lw %[tempA], 12(%[cmd])\n"
                "lw %[tempB], 16(%[cmd])\n"
                "lw %[tempC], 20(%[cmd])\n"
                "lw %[tempD], 24(%[cmd])\n"
                "sw %[tempA], 16(%[cmd])\n"
                "sw %[tempB], 12(%[cmd])\n"
                "sw %[tempC], 24(%[cmd])\n"
                "sw %[tempD], 20(%[cmd])\n"
                ".set pop"
                : [tempA] "=&r" (tempA), [tempB] "=&r" (tempB),
                  [tempC] "=&r" (tempC), [tempD] "=&r" (tempD),
                  "=m" (cmd->CMDXA), "=m" (cmd->CMDYA), "=m" (cmd->CMDXB),
                  "=m" (cmd->CMDYB), "=m" (cmd->CMDXC), "=m" (cmd->CMDYC),
                  "=m" (cmd->CMDXD), "=m" (cmd->CMDYD)
                : [cmd] "r" (cmd)
            );
            break;
          }  // case 0x10
          case 0x20: {  // Flip vertically
            gouraud_flip = 6;
            uint32_t tempA, tempB, tempC, tempD;
            asm(".set push; .set noreorder\n"
                "lw %[tempA], 12(%[cmd])\n"
                "lw %[tempB], 16(%[cmd])\n"
                "lw %[tempC], 20(%[cmd])\n"
                "lw %[tempD], 24(%[cmd])\n"
                "sw %[tempA], 24(%[cmd])\n"
                "sw %[tempB], 20(%[cmd])\n"
                "sw %[tempC], 16(%[cmd])\n"
                "sw %[tempD], 12(%[cmd])\n"
                ".set pop"
                : [tempA] "=&r" (tempA), [tempB] "=&r" (tempB),
                  [tempC] "=&r" (tempC), [tempD] "=&r" (tempD),
                  "=m" (cmd->CMDXA), "=m" (cmd->CMDYA), "=m" (cmd->CMDXB),
                  "=m" (cmd->CMDYB), "=m" (cmd->CMDXC), "=m" (cmd->CMDYC),
                  "=m" (cmd->CMDXD), "=m" (cmd->CMDYD)
                : [cmd] "r" (cmd)
            );
            break;
          }  // case 0x20
          case 0x30: {  // Flip horizontally and vertically
            gouraud_flip = 4;
            uint32_t tempA, tempB, tempC, tempD;
            asm(".set push; .set noreorder\n"
                "lw %[tempA], 12(%[cmd])\n"
                "lw %[tempB], 16(%[cmd])\n"
                "lw %[tempC], 20(%[cmd])\n"
                "lw %[tempD], 24(%[cmd])\n"
                "sw %[tempA], 20(%[cmd])\n"
                "sw %[tempB], 24(%[cmd])\n"
                "sw %[tempC], 12(%[cmd])\n"
                "sw %[tempD], 16(%[cmd])\n"
                ".set pop"
                : [tempA] "=&r" (tempA), [tempB] "=&r" (tempB),
                  [tempC] "=&r" (tempC), [tempD] "=&r" (tempD),
                  "=m" (cmd->CMDXA), "=m" (cmd->CMDYA), "=m" (cmd->CMDXB),
                  "=m" (cmd->CMDYB), "=m" (cmd->CMDXC), "=m" (cmd->CMDYC),
                  "=m" (cmd->CMDXD), "=m" (cmd->CMDYD)
                : [cmd] "r" (cmd)
            );
            break;
          }  // case 0x30
        }  // switch (cmd->CMDCTRL & 0x30)
    } else {
        width = height = 0;
    }


    /* Get the polygon color and priority, and load the texture if it's
     * a sprite. */

    int priority, sprite_alpha;
    uint32_t color32 = vdp1_get_cmd_color_pri(cmd, textured, &priority);
    uint32_t texture_key;
    if (textured) {
        texture_key = vdp1_cache_sprite_texture(cmd, width, height,
                                                &priority, &sprite_alpha);
        if (UNLIKELY(!texture_key)) {
            DMSG("WARNING: failed to cache texture for A=(%d,%d) B=(%d,%d)"
                 " C=(%d,%d) D=(%d,%d)",
                 cmd->CMDXA + Vdp1Regs->localX, cmd->CMDYA + Vdp1Regs->localY,
                 cmd->CMDXB + Vdp1Regs->localX, cmd->CMDYB + Vdp1Regs->localY,
                 cmd->CMDXC + Vdp1Regs->localX, cmd->CMDYC + Vdp1Regs->localY,
                 cmd->CMDXD + Vdp1Regs->localX, cmd->CMDYD + Vdp1Regs->localY);
        }
        /* Convert alpha to 0-255 */
        sprite_alpha = (sprite_alpha << 3) | (sprite_alpha >> 2);
    } else {
        texture_key = 0;
        sprite_alpha = 0xFF;
    }

    /* Apply alpha depending on the color calculation settings. */

    if (Vdp2Regs->CCCTL & 0x40) {
        const unsigned int ref_priority = Vdp2Regs->SPCTL>>8 & 0x7;
        switch (Vdp2Regs->SPCTL>>12 & 0x3) {
          case 0:
            if (priority <= ref_priority) {
                color32 = (sprite_alpha << 24) | (color32 & 0x00FFFFFF);
            }
            break;
          case 1:
            if (priority == ref_priority) {
                color32 = (sprite_alpha << 24) | (color32 & 0x00FFFFFF);
            }
            break;
          case 2:
            if (priority >= ref_priority) {
                color32 = (sprite_alpha << 24) | (color32 & 0x00FFFFFF);
            }
            break;
          case 3:
            /* Alpha blending enabled based on high bit of color value
             * (not supported in this renderer) */
            break;
        }
    }

    /* We don't support mesh shading; treat it as half-alpha instead. */

    if (cmd->CMDPMOD & 0x100) {  // Mesh shading bit
        const unsigned int alpha = color32 >> 24;
        color32 = ((alpha+1)/2) << 24 | (color32 & 0x00FFFFFF);
    }

    /* If it's a Gouraud-shaded polygon, pick up the four corner colors. */

    uint32_t color_A, color_B, color_C, color_D;
    if (cmd->CMDPMOD & 4) {  // Gouraud shading bit
        const uint32_t alpha = color32 & 0xFF000000;
        if (vdp1_rofs | vdp1_gofs | vdp1_bofs) {
            unsigned int temp_A, temp_B, temp_C, temp_D;
            temp_A = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (0^gouraud_flip));
            temp_B = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (2^gouraud_flip));
            temp_C = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (4^gouraud_flip));
            temp_D = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (6^gouraud_flip));
            color_A = alpha | (adjust_color_16_32(temp_A, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_B = alpha | (adjust_color_16_32(temp_B, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_C = alpha | (adjust_color_16_32(temp_C, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
            color_D = alpha | (adjust_color_16_32(temp_D, vdp1_rofs, vdp1_gofs,
                                                  vdp1_bofs) & 0x00FFFFFF);
        } else {
            unsigned int temp_A, temp_B, temp_C, temp_D;
            temp_A = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (0^gouraud_flip));
            temp_B = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (2^gouraud_flip));
            temp_C = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (4^gouraud_flip));
            temp_D = T1ReadWord(Vdp1Ram, (cmd->CMDGRDA<<3) + (6^gouraud_flip));
            color_A = alpha | (temp_A & 0x7C00) << 9
                            | (temp_A & 0x03E0) << 6
                            | (temp_A & 0x001F) << 3;
            color_B = alpha | (temp_B & 0x7C00) << 9
                            | (temp_B & 0x03E0) << 6
                            | (temp_B & 0x001F) << 3;
            color_C = alpha | (temp_C & 0x7C00) << 9
                            | (temp_C & 0x03E0) << 6
                            | (temp_C & 0x001F) << 3;
            color_D = alpha | (temp_D & 0x7C00) << 9
                            | (temp_D & 0x03E0) << 6
                            | (temp_D & 0x001F) << 3;
        }
    } else {
        color_A = color_B = color_C = color_D = color32;
    }

    /* Set up the vertex array using a strip of 2 triangles.  The Saturn
     * coordinate order is A,B,C,D clockwise around the texture, so we flip
     * around C and D in our vertex array.  For simplicity, we assign both
     * the color and U/V coordinates regardless of whether the polygon is
     * textured or not; the GE is fast enough that it can handle all the
     * processing in time. */

    struct {int16_t u, v; uint32_t color; int16_t x, y, z, pad;} *vertices;
    vertices = pspGuGetMemoryMerge(sizeof(*vertices) * 4);
    vertices[0].u = 0;
    vertices[0].v = 0;
    vertices[0].color = color_A;
    vertices[0].x = (cmd->CMDXA + Vdp1Regs->localX) >> disp_xscale;
    vertices[0].y = (cmd->CMDYA + Vdp1Regs->localY) >> disp_yscale;
    vertices[0].z = 0;
    vertices[1].u = width;
    vertices[1].v = 0;
    vertices[1].color = color_B;
    vertices[1].x = (cmd->CMDXB + Vdp1Regs->localX) >> disp_xscale;
    vertices[1].y = (cmd->CMDYB + Vdp1Regs->localY) >> disp_yscale;
    vertices[1].z = 0;
    vertices[2].u = 0;
    vertices[2].v = height;
    vertices[2].color = color_D;
    vertices[2].x = (cmd->CMDXD + Vdp1Regs->localX) >> disp_xscale;
    vertices[2].y = (cmd->CMDYD + Vdp1Regs->localY) >> disp_yscale;
    vertices[2].z = 0;
    vertices[3].u = width;
    vertices[3].v = height;
    vertices[3].color = color_C;
    vertices[3].x = (cmd->CMDXC + Vdp1Regs->localX) >> disp_xscale;
    vertices[3].y = (cmd->CMDYC + Vdp1Regs->localY) >> disp_yscale;
    vertices[3].z = 0;

    /* Queue the draw operation. */

    vdp1_queue_render(priority, texture_key,
                      GU_TRIANGLE_STRIP, GU_TEXTURE_16BIT | GU_COLOR_8888
                                       | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                      4, NULL, vertices);
}

/*************************************************************************/

/**
 * vdp1_convert_color:  Convert a VDP1 16-bit color value and pixel mode to
 * a 32-bit color value.  Helper function for vdp1_get_cmd_color() and
 * vdp1_get_cmd_color_pri().
 *
 * [Parameters]
 *      color16: 16-bit color value
 *     textured: Nonzero if a textured polygon command, else zero
 *      CMDPMOD: Value of CMDPMOD field in VDP1 command
 * [Return value]
 *     32-bit color value
 */
static uint32_t vdp1_convert_color(uint16_t color16, int textured,
                                   unsigned int CMDPMOD)
{
    uint32_t color32;
    if (textured) {
        color32 = 0xFFFFFF;
    } else if (color16 == 0) {
        color32 = adjust_color_16_32(0x0000, vdp1_rofs, vdp1_gofs, vdp1_bofs);
        return color32 & 0x00FFFFFF;  // Transparent regardless of CMDPMOD
    } else if (color16 & 0x8000) {
        color32 = adjust_color_16_32(color16, vdp1_rofs, vdp1_gofs, vdp1_bofs);
    } else {
        color32 = adjust_color_32_32(global_clut_32[color16 & 0x7FF],
                                     vdp1_rofs, vdp1_gofs, vdp1_bofs);
    }

    switch (CMDPMOD & 7) {
      default: // Impossible, but avoid a "function may not return" warning
      case 1:  // Shadow
        return 0x80000000;
      case 4 ... 7:  // Gouraud shading (handled separately)
      case 0:  // Replace
        return 0xFF000000 | color32;
      case 2:  // 50% luminance
        /* Clever, quick way to divide each component by 2 in one step
         * (borrowed from vidsoft.c) */
        return 0xFF000000 | ((color32 & 0xFEFEFE) >> 1);
      case 3:  // 50% transparency
        return 0x80000000 | color32;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * vdp1_get_cmd_color:  Return the 32-bit color value specified by a VDP1
 * line command.
 *
 * [Parameters]
 *     cmd: VDP1 command pointer
 * [Return value]
 *     32-bit color value
 */
static uint32_t vdp1_get_cmd_color(vdp1cmd_struct *cmd)
{
    return vdp1_convert_color(cmd->CMDCOLR, 0, cmd->CMDPMOD);
}

/*-----------------------------------------------------------------------*/

/**
 * vdp1_get_cmd_color_pri:  Return the 32-bit color value and priority
 * specified by a VDP1 polygon command.
 *
 * [Parameters]
 *              cmd: VDP1 command pointer
 *         textured: Nonzero if the polygon is textured, else zero
 *     priority_ret: Pointer to variable to receive priority value
 * [Return value]
 *     32-bit color value
 */
static uint32_t vdp1_get_cmd_color_pri(vdp1cmd_struct *cmd, int textured,
                                       int *priority_ret)
{
    uint16_t color16 = cmd->CMDCOLR;
    if (cmd->CMDCOLR & 0x8000) {
        *priority_ret = Vdp2Regs->PRISA & 7;
    } else {
        *priority_ret = 0;  // Default if not set by SPCTL
        int alpha_unused;  // FIXME: is this used by non-sprite quads as well?
        vdp1_process_sprite_color(color16, priority_ret, &alpha_unused);
    }
    return vdp1_convert_color(color16, textured, cmd->CMDPMOD);
}

/*-----------------------------------------------------------------------*/

/**
 * vdp1_process_sprite_color:  Return the color index mask, priority index,
 * and alpha (color calculation) index selected by the given color register
 * value and the VDP2 SPCTL register.
 *
 * [Parameters]
 *          color16: 16-bit color register value
 *     priority_ret: Pointer to variable to receive priority value
 * [Return value]
 *     Mask to apply to CMDCOLR register
 */
static uint16_t vdp1_process_sprite_color(uint16_t color16, int *priority_ret,
                                          int *alpha_ret)
{
    static const uint8_t priority_shift[16] =
        { 14, 13, 14, 13,  13, 12, 12, 12,  7, 7, 6, 0,  7, 7, 6, 0 };
    static const uint8_t priority_mask[16] =
        {  3,  7,  1,  3,   3,  7,  7,  7,  1, 1, 3, 0,  1, 1, 3, 0 };
    static const uint8_t alpha_shift[16] =
        { 11, 11, 11, 11,  10, 11, 10,  9,  0, 6, 0, 6,  0, 6, 0, 6 };
    static const uint8_t alpha_mask[16] =
        {  7,  3,  7,  3,   7,  1,  3,  7,  0, 1, 0, 3,  0, 1, 0, 3 };
    static const uint16_t color_mask[16] =
        { 0x7FF, 0x7FF, 0x7FF, 0x7FF,  0x3FF, 0x7FF, 0x3FF, 0x1FF,
           0x7F,  0x3F,  0x3F,  0x3F,   0xFF,  0xFF,  0xFF,  0xFF };

    const unsigned int type = Vdp2Regs->SPCTL & 0xF;
    *priority_ret = (color16 >> priority_shift[type]) & priority_mask[type];
    *alpha_ret    = (color16 >>    alpha_shift[type]) &    alpha_mask[type];
    return color_mask[type];
}

/*************************************************************************/

/**
 * vdp1_cache_sprite_texture:  Cache the sprite texture designated by the
 * given VDP1 command.
 *
 * [Parameters]
 *              cmd: VDP1 command pointer
 *            width: Sprite width (pixels; passed in to avoid recomputation)
 *           height: Sprite height (pixels; passed in to avoid recomputation)
 *     priority_ret: Pointer to variable to receive priority value
 *        alpha_ret: Pointer to variable to receive alpha value (0-31)
 * [Return value]
 *     Cached texture key, or zero on error
 */
static uint32_t vdp1_cache_sprite_texture(
    vdp1cmd_struct *cmd, int width, int height, int *priority_ret,
    int *alpha_ret)
{
    uint16_t pixel_mask = 0xFFFF;
    int pri_reg = 0, alpha_reg = 0;  // Default value

    int is_indexed = 1;
    uint16_t color16 = cmd->CMDCOLR;
    const int pixfmt = cmd->CMDPMOD>>3 & 7;
    if (pixfmt == 5) {
        is_indexed = 0;
    } else if (pixfmt == 1) {
        /* Indirect T4 texture; see whether the first pixel references
         * color RAM or uses raw RGB values. */
        const uint32_t addr = cmd->CMDSRCA << 3;
        const uint8_t pixel = T1ReadByte(Vdp1Ram, addr) >> 4;
        const uint32_t colortable = cmd->CMDCOLR << 3;
        const uint16_t value = T1ReadWord(Vdp1Ram, colortable + pixel*2);
        if (value & 0x8000) {
            is_indexed = 0;
        } else {
            color16 = value;
        }
    }
    if (is_indexed) {
        pixel_mask = vdp1_process_sprite_color(color16, &pri_reg, &alpha_reg);
    }

    *priority_ret = ((uint8_t *)&Vdp2Regs->PRISA)[pri_reg] & 0x7;
    *alpha_ret = 0x1F - (((uint8_t *)&Vdp2Regs->CCRSA)[alpha_reg] & 0x1F);

    /* Cache the texture data and return the key. */
    
    return texcache_cache_sprite(cmd, pixel_mask, width, height,
                                 vdp1_is_persistent(cmd));
}

/*************************************************************************/

/**
 * vdp1_queue_render:  Queue a render operation from a VDP1 command.
 *
 * [Parameters]
 *        priority: Saturn display priority (0-7)
 *     texture_key: Texture key for sprites, zero for untextured operations
 *       primitive,
 *     vertex_type,
 *           count,
 *         indices,
 *        vertices: Parameters to pass to guDrawArray()
 * [Return value]
 *     None
 */
static inline void vdp1_queue_render(
    int priority, uint32_t texture_key, int primitive,
    int vertex_type, int count, const void *indices, const void *vertices)
{
    /* Expand the queue if necessary. */
    if (UNLIKELY(vdp1_queue[priority].len >= vdp1_queue[priority].size)) {
        const int newsize = vdp1_queue[priority].size + VDP1_QUEUE_EXPAND_SIZE;
        VDP1RenderData * const newqueue = realloc(vdp1_queue[priority].queue,
                                                  newsize * sizeof(*newqueue));
        if (UNLIKELY(!newqueue)) {
            DMSG("Failed to expand priority %d queue to %d entries",
                 priority, newsize);
            return;
        }
        vdp1_queue[priority].queue = newqueue;
        vdp1_queue[priority].size  = newsize;
    }

    /* Record the data passed in. */
    const int index = vdp1_queue[priority].len++;
    VDP1RenderData * const entry = &vdp1_queue[priority].queue[index];
    entry->texture_key = texture_key;
    entry->primitive   = primitive;
    entry->vertex_type = vertex_type;
    entry->count       = count;
    entry->indices     = indices;
    entry->vertices    = vertices;
}

/*-----------------------------------------------------------------------*/

/**
 * vdp1_run_queue:  Run the rendering queue for the given priority level.
 *
 * [Parameters]
 *     priority: Priority level to run
 * [Return value]
 *     None
 */
static void vdp1_run_queue(int priority)
{
    int in_texture_mode;  // Remember which mode we're in
    VDP1RenderData *entry = vdp1_queue[priority].queue;
    VDP1RenderData * const queue_top = entry + vdp1_queue[priority].len;

    if (vdp1_queue[priority].len == 0) {
        return;  // Nothing to do
    }

    guShadeModel(GU_SMOOTH);
    guAmbientColor(0xFFFFFFFF);
    guTexFunc(GU_TFX_MODULATE, GU_TCC_RGBA);
    if (config_get_smooth_textures()) {
        guTexFilter(GU_LINEAR, GU_LINEAR);
    }
    if (entry->texture_key) {
        guEnable(GU_TEXTURE_2D);
        in_texture_mode = 1;
    } else {
        guDisable(GU_TEXTURE_2D);
        in_texture_mode = 0;
    }
    for (; entry < queue_top; entry++) {
        if (entry->texture_key) {
            texcache_load_sprite(entry->texture_key);
            if (!in_texture_mode) {
                guEnable(GU_TEXTURE_2D);
                in_texture_mode = 1;
            }
        } else {
            if (in_texture_mode) {
                guDisable(GU_TEXTURE_2D);
                in_texture_mode = 0;
            }
        }
        guDrawArray(entry->primitive, entry->vertex_type,
                    entry->count, entry->indices, entry->vertices);
    }
    if (in_texture_mode) {
        guDisable(GU_TEXTURE_2D);
    }
    if (config_get_smooth_textures()) {
        guTexFilter(GU_NEAREST, GU_NEAREST);
    }
    guShadeModel(GU_FLAT);

    guCommit();
}

/*************************************************************************/
/*************************************************************************/

/**
 * vdp2_get_color_offset:  Calculate the color offsets to use for the
 * specified CLOFEN/CLOFSL bit.
 *
 * [Parameters]
 *         mask: 1 << bit number to check
 *     rofs_ret: Pointer to variable to store red offset in
 *     gofs_ret: Pointer to variable to store green offset in
 *     bofs_ret: Pointer to variable to store blue offset in
 * [Return value]
 *     None
 */
static inline void vdp2_get_color_offsets(uint16_t mask, int32_t *rofs_ret,
                                          int32_t *gofs_ret, int32_t *bofs_ret)
{
    if (Vdp2Regs->CLOFEN & mask) {  // CoLor OFfset ENable
        /* Offsets are 9-bit signed values */
        if (Vdp2Regs->CLOFSL & mask) {  // CoLor OFfset SeLect
            *rofs_ret = ((int32_t)Vdp2Regs->COBR << 23) >> 23;
            *gofs_ret = ((int32_t)Vdp2Regs->COBG << 23) >> 23;
            *bofs_ret = ((int32_t)Vdp2Regs->COBB << 23) >> 23;
        } else {
            *rofs_ret = ((int32_t)Vdp2Regs->COAR << 23) >> 23;
            *gofs_ret = ((int32_t)Vdp2Regs->COAG << 23) >> 23;
            *bofs_ret = ((int32_t)Vdp2Regs->COAB << 23) >> 23;
        }
    } else {
        /* No color offset */
        *rofs_ret = *gofs_ret = *bofs_ret = 0;
    }
}

/*************************************************************************/

/**
 * vdp2_draw_bg:  Draw the screen background.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void vdp2_draw_bg(void)
{
    uint32_t address = ((Vdp2Regs->BKTAU & 7) << 16 | Vdp2Regs->BKTAL) << 1;
    if (!(Vdp2Regs->VRSIZE & 0x8000)) {
        address &= 0x7FFFF;
    }

    int rofs, gofs, bofs;
    vdp2_get_color_offsets(1<<6, &rofs, &gofs, &bofs);

    struct {uint32_t color; int16_t x, y, z, pad;} *vertices;

    if (Vdp2Regs->BKTAU & 0x8000) {
        /* Distinct color for each line */
        int num_vertices, y;
        if (disp_height > 272) {
            /* For interlaced screens, we average the colors of each two
             * adjacent lines */
            num_vertices = 2*(disp_height/2);
            vertices = pspGuGetMemoryMerge(sizeof(*vertices) * num_vertices);
            for (y = 0; y+1 < disp_height; y += 2, address += 4) {
                uint16_t rgb0 = T1ReadWord(Vdp2Ram, address);
                uint32_t r0 = (rgb0 & 0x001F) << 3;
                uint32_t g0 = (rgb0 & 0x03E0) >> 2;
                uint32_t b0 = (rgb0 & 0x7C00) >> 7;
                uint16_t rgb1 = T1ReadWord(Vdp2Ram, address);
                uint32_t r1 = (rgb1 & 0x001F) << 3;
                uint32_t g1 = (rgb1 & 0x03E0) >> 2;
                uint32_t b1 = (rgb1 & 0x7C00) >> 7;
                uint32_t color = bound(((r0+r1+1)/2) + rofs, 0, 255) <<  0
                               | bound(((g0+g1+1)/2) + gofs, 0, 255) <<  8
                               | bound(((b0+b1+1)/2) + bofs, 0, 255) << 16
                               | 0xFF000000;
                vertices[y+0].color = color;
                vertices[y+0].x = 0;
                vertices[y+0].y = y/2;
                vertices[y+0].z = 0;
                vertices[y+1].color = color;
                vertices[y+1].x = disp_width >> disp_xscale;
                vertices[y+1].y = y/2;
                vertices[y+1].z = 0;
            }
        } else {
            num_vertices = 2*disp_height;
            vertices = pspGuGetMemoryMerge(sizeof(*vertices) * num_vertices);
            for (y = 0; y < disp_height; y++, address += 2) {
                uint16_t rgb = T1ReadWord(Vdp2Ram, address);
                uint32_t r = bound(((rgb & 0x001F) << 3) + rofs, 0, 255);
                uint32_t g = bound(((rgb & 0x03E0) >> 2) + gofs, 0, 255);
                uint32_t b = bound(((rgb & 0x7C00) >> 7) + bofs, 0, 255);
                vertices[y*2+0].color = 0xFF000000 | r | g<<8 | b<<16;
                vertices[y*2+0].x = 0;
                vertices[y*2+0].y = y;
                vertices[y*2+0].z = 0;
                vertices[y*2+1].color = 0xFF000000 | r | g<<8 | b<<16;
                vertices[y*2+1].x = disp_width >> disp_xscale;
                vertices[y*2+1].y = y;
                vertices[y*2+1].z = 0;
            }
        }
        guDrawArray(GU_LINES,
                    GU_COLOR_8888 | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    num_vertices, NULL, vertices);
        guCommit();
    } else {
        /* Single color for the whole screen */
        vertices = pspGuGetMemoryMerge(sizeof(*vertices) * 2);
        uint16_t rgb = T1ReadWord(Vdp2Ram, address);
        uint32_t r = bound(((rgb & 0x001F) << 3) + rofs, 0, 255);
        uint32_t g = bound(((rgb & 0x03E0) >> 2) + gofs, 0, 255);
        uint32_t b = bound(((rgb & 0x7C00) >> 7) + bofs, 0, 255);
        vertices[0].color = 0xFF000000 | r | g<<8 | b<<16;
        vertices[0].x = 0;
        vertices[0].y = 0;
        vertices[0].z = 0;
        vertices[1].color = 0xFF000000 | r | g<<8 | b<<16;
        vertices[1].x = disp_width >> disp_xscale;
        vertices[1].y = disp_height >> disp_yscale;
        vertices[1].z = 0;
        guDrawArray(GU_SPRITES,
                    GU_COLOR_8888 | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    2, NULL, vertices);
        guCommit();
    }
}

/*************************************************************************/

/**
 * vdp2_draw_graphics:  Draw a single VDP2 background graphics layer.
 *
 * [Parameters]
 *     layer: Background graphics layer (BG_* constant)
 * [Return value]
 *     None
 */
static void vdp2_draw_graphics(int layer)
{
    vdp2draw_struct info;
    clipping_struct clip[2];

    /* Is this background layer enabled? */
    if (!(Vdp2Regs->BGON & Vdp2External.disptoggle & (1 << layer))) {
        return;
    }
    if (layer == BG_RBG0 && !config_get_enable_rotate()) {
        return;
    }

    /* Check whether we should smooth the graphics. */
    const int smooth_hires =
        (disp_width > 352 || disp_height > 272) && config_get_smooth_hires();

    /* Find out whether it's a bitmap or not. */
    switch (layer) {
      case BG_NBG0: info.isbitmap = Vdp2Regs->CHCTLA & 0x0002; break;
      case BG_NBG1: info.isbitmap = Vdp2Regs->CHCTLA & 0x0200; break;
      case BG_RBG0: info.isbitmap = Vdp2Regs->CHCTLB & 0x0200; break;
      default:      info.isbitmap = 0; break;
    }

    /* Determine color-related data. */
    info.transparencyenable = !(Vdp2Regs->BGON & (0x100 << layer));
    /* FIXME: specialprimode is not actually supported by the map drawing
     * functions */
    info.specialprimode = (Vdp2Regs->SFPRMD >> (2*layer)) & 3;
    switch (layer) {
      case BG_NBG0:
        info.colornumber = (Vdp2Regs->CHCTLA & 0x0070) >>  4;
        break;
      case BG_NBG1:
        info.colornumber = (Vdp2Regs->CHCTLA & 0x3000) >> 12;
        break;
      case BG_NBG2:
        info.colornumber = (Vdp2Regs->CHCTLB & 0x0002) >>  1;
        break;
      case BG_NBG3:
        info.colornumber = (Vdp2Regs->CHCTLB & 0x0020) >>  5;
        break;
      case BG_RBG0:
        info.colornumber = (Vdp2Regs->CHCTLB & 0x7000) >> 12;
        break;
    }
    if (Vdp2Regs->CCCTL & (1 << layer)) {
        const uint8_t *ptr = (const uint8_t *)&Vdp2Regs->CCRNA;
        info.alpha = ((~ptr[layer] & 0x1F) << 3) + 7;
    } else {
        info.alpha = 0xFF;
    }
    if (layer == BG_RBG0) {
        info.coloroffset = (Vdp2Regs->CRAOFB & 7) << 8;
    } else {
        info.coloroffset = ((Vdp2Regs->CRAOFA >> (4*layer)) & 7) << 8;
    }
    vdp2_get_color_offsets(1 << layer, (int32_t *)&info.cor,
                           (int32_t *)&info.cog, (int32_t *)&info.cob);

    /* Extract rotation information for RBG0. */
    if (layer == BG_RBG0) {
        switch (Vdp2Regs->RPMD & 3) {
          case 0:
            info.rotatenum = 0;
            info.rotatemode = 0;
            break;
          case 1:
            info.rotatenum = 1;
            info.rotatemode = 0;
            break;
          case 2:
            info.rotatenum = 0;
            info.rotatemode = 1;
            break;
          case 3:
            info.rotatenum = 0;
            info.rotatemode = 2;
            break;
        }
    }

    /* Determine tilemap/bitmap size and display offset. */
    if (info.isbitmap) {
        if (layer == BG_RBG0) {
            ReadBitmapSize(&info, Vdp2Regs->CHCTLB >> 10, 0x3);
            info.charaddr = ((Vdp2Regs->MPOFR >> (4*info.rotatenum)) & 7) << 17;
            info.paladdr = (Vdp2Regs->BMPNB & 0x7) << 4;
        } else {
            ReadBitmapSize(&info, Vdp2Regs->CHCTLA >> (2 + layer*8), 0x3);
            info.charaddr = ((Vdp2Regs->MPOFN >> (4*layer)) & 7) << 17;
            info.paladdr = ((Vdp2Regs->BMPNA >> (8*layer)) & 7) << 4;
        }
        info.flipfunction = 0;
        info.specialfunction = 0;
        switch (layer) {
          case BG_NBG0:
            info.x = - ((Vdp2Regs->SCXIN0 & 0x7FF) % info.cellw);
            info.y = - ((Vdp2Regs->SCYIN0 & 0x7FF) % info.cellh);
            break;
          case BG_NBG1:
            info.x = - ((Vdp2Regs->SCXIN1 & 0x7FF) % info.cellw);
            info.y = - ((Vdp2Regs->SCYIN1 & 0x7FF) % info.cellh);
            break;
          case BG_RBG0:
            /* Transformation is handled separately; nothing to do here. */
            break;
          default:
            DMSG("info.isbitmap set for invalid layer %d", layer);
            return;
        }
    } else {
        if (layer == BG_RBG0) {
            info.mapwh = 4;
            ReadPlaneSize(&info, Vdp2Regs->PLSZ >> (8 + 4*info.rotatenum));
        } else {
            info.mapwh = 2;
            ReadPlaneSize(&info, Vdp2Regs->PLSZ >> (2*layer));
        }
        const int scx_mask = (512 * info.planew * info.mapwh) - 1;
        const int scy_mask = (512 * info.planeh * info.mapwh) - 1;
        switch (layer) {
          case BG_NBG0:
            info.x = - (Vdp2Regs->SCXIN0 & scx_mask);
            info.y = - (Vdp2Regs->SCYIN0 & scy_mask);
            ReadPatternData(&info, Vdp2Regs->PNCN0, Vdp2Regs->CHCTLA & 0x0001);
            break;
          case BG_NBG1:
            info.x = - (Vdp2Regs->SCXIN1 & scx_mask);
            info.y = - (Vdp2Regs->SCYIN1 & scy_mask);
            ReadPatternData(&info, Vdp2Regs->PNCN1, Vdp2Regs->CHCTLA & 0x0100);
            break;
          case BG_NBG2:
            info.x = - (Vdp2Regs->SCXN2 & scx_mask);
            info.y = - (Vdp2Regs->SCYN2 & scy_mask);
            ReadPatternData(&info, Vdp2Regs->PNCN2, Vdp2Regs->CHCTLB & 0x0001);
            break;
          case BG_NBG3:
            info.x = - (Vdp2Regs->SCXN3 & scx_mask);
            info.y = - (Vdp2Regs->SCYN3 & scy_mask);
            ReadPatternData(&info, Vdp2Regs->PNCN3, Vdp2Regs->CHCTLB & 0x0010);
            break;
          case BG_RBG0:
            ReadPatternData(&info, Vdp2Regs->PNCR,  Vdp2Regs->CHCTLB & 0x0100);
            break;
        }
    }

    /* Determine coordinate scaling. */
    // FIXME: scaled graphics may be distorted because integers are used
    // for vertex coordinates
    switch (layer) {
      case BG_NBG0:
        info.coordincx = 65536.0f / (Vdp2Regs->ZMXN0.all & 0x7FF00 ?: 65536);
        info.coordincy = 65536.0f / (Vdp2Regs->ZMYN0.all & 0x7FF00 ?: 65536);
        break;
      case BG_NBG1:
        info.coordincx = 65536.0f / (Vdp2Regs->ZMXN1.all & 0x7FF00 ?: 65536);
        info.coordincy = 65536.0f / (Vdp2Regs->ZMYN1.all & 0x7FF00 ?: 65536);
        break;
      default:
        info.coordincx = info.coordincy = 1;
        break;
    }
    if (disp_xscale == 1) {
        info.coordincx /= 2;
    }
    if (disp_yscale == 1) {
        info.coordincy /= 2;
    }

    /* Get clipping data. */
    info.wctl = ((uint8_t *)&Vdp2Regs->WCTLA)[layer];
    clip[0].xstart = 0; clip[0].xend = disp_width;
    clip[0].ystart = 0; clip[0].yend = disp_height;
    clip[1].xstart = 0; clip[1].xend = disp_width;
    clip[1].ystart = 0; clip[1].yend = disp_height;
    ReadWindowData(info.wctl, clip);

    /* Check for a zero-size clip window, which some games seem to use to
     * temporarily disable a screen. */
    if (clip[0].xstart >= clip[0].xend
     || clip[0].ystart >= clip[0].yend
     || clip[1].xstart >= clip[1].xend
     || clip[1].ystart >= clip[1].yend
    ) {
        return;
    }

    info.priority = bg_priority[layer];
    switch (layer) {
        case BG_NBG0: info.PlaneAddr = (void *)Vdp2NBG0PlaneAddr; break;
        case BG_NBG1: info.PlaneAddr = (void *)Vdp2NBG1PlaneAddr; break;
        case BG_NBG2: info.PlaneAddr = (void *)Vdp2NBG2PlaneAddr; break;
        case BG_NBG3: info.PlaneAddr = (void *)Vdp2NBG3PlaneAddr; break;
        case BG_RBG0: if (info.rotatenum == 0) {
                          info.PlaneAddr = (void *)Vdp2ParameterAPlaneAddr;
                      } else {
                          info.PlaneAddr = (void *)Vdp2ParameterBPlaneAddr;
                      }
                      break;
        default:      DMSG("No PlaneAddr for layer %d", layer); return;
    }
    info.patternpixelwh = 8 * info.patternwh;
    info.draww = (int)((float)(disp_width  >> disp_xscale) / info.coordincx);
    info.drawh = (int)((float)(disp_height >> disp_yscale) / info.coordincy);

    /* Set up for rendering. */
    guEnable(GU_TEXTURE_2D);
    guAmbientColor(info.alpha<<24 | 0xFFFFFF);
    if (smooth_hires) {
        guTexFilter(GU_LINEAR, GU_LINEAR);
    }

    /* If a custom drawing function has been specified for this layer, call
     * it first. */
    int custom_draw_succeeded = 0;
    if (custom_draw_func[layer]) {
        custom_draw_succeeded = (*custom_draw_func[layer])(&info, clip);
        if (custom_draw_succeeded && layer == BG_RBG0) {
            drew_slow_RBG0 = !RBG0_draw_func_is_fast;
        }
    }

    if (!custom_draw_succeeded) {

        /* Select a rendering function based on the tile layout and format. */
        void (*draw_map_func)(vdp2draw_struct *info,
                              const clipping_struct *clip);
        if (layer == BG_RBG0) {
            draw_map_func = &vdp2_draw_map_rotated;
        } else if (info.isbitmap) {
            switch (layer) {
              case BG_NBG0:
                if ((Vdp2Regs->SCRCTL & 7) == 7) {
                    DMSG("WARNING: line scrolling not supported");
                }
                /* fall through */
              case BG_NBG1:
                if (info.colornumber == 1 && !smooth_hires) {
                    draw_map_func = &vdp2_draw_bitmap_t8;
                } else if (info.colornumber == 4 && !smooth_hires
                           && info.coordincx == 1 && info.coordincy == 1) {
                    draw_map_func = &vdp2_draw_bitmap_32;
                } else {
                    draw_map_func = &vdp2_draw_bitmap;
                }
                break;
              default:
                DMSG("info.isbitmap set for invalid layer %d", layer);
                return;
            }
        } else if (info.patternwh == 2) {
            if (info.colornumber == 1 && !smooth_hires) {
                draw_map_func = &vdp2_draw_map_16x16_t8;
            } else {
                draw_map_func = &vdp2_draw_map_16x16;
            }
        } else {
            if (info.colornumber == 1 && !smooth_hires) {
                draw_map_func = &vdp2_draw_map_8x8_t8;
            } else {
                draw_map_func = &vdp2_draw_map_8x8;
            }
        }

        /* Render the graphics. */
        (*draw_map_func)(&info, clip);
        if (layer == BG_RBG0) {
            drew_slow_RBG0 = 1;
        }

    }  // if (!custom_draw_succeeded)

    /* All done. */
    if (smooth_hires) {
        guTexFilter(GU_NEAREST, GU_NEAREST);
    }
    guAmbientColor(0xFFFFFFFF);
    guDisable(GU_TEXTURE_2D);
    guCommit();
}

/*************************************************************************/
/***** Utility routines exported to background graphics drawing code *****/
/*************************************************************************/

/* Last block allocated with pspGuGetMemoryMerge() */
static void *merge_last_ptr;
static uint32_t merge_last_size;

/*-----------------------------------------------------------------------*/

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
void *pspGuGetMemoryMerge(uint32_t size)
{
    /* Make sure size is 32-bit aligned. */
    size = (size + 3) & -4;

    /* Start off by allocating the block normally.  Ideally, we'd check
     * first whether the current list pointer is immediately past the last
     * block allocated, but since there's apparently no interface for
     * either getting the current pointer or deleting the last instruction,
     * we're out of luck and can't save the 8 bytes taken by the jump, even
     * if we end up not needing it. */
    void *ptr = guGetMemory(size);

    /* If the pointer we got back is equal to the end of the previously
     * allocated block plus 8 bytes (2 GU instructions), we can merge. */
    if ((uint8_t *)ptr == (uint8_t *)merge_last_ptr + merge_last_size + 8) {
        /* Make sure the instruction before the last block really is a
         * jump instruction before we update it. */
        uint32_t *jump_ptr = (uint32_t *)merge_last_ptr - 1;
        if (*jump_ptr >> 24 == 0x08) {
            void *block_end = (uint8_t *)ptr + size;
            *jump_ptr = 0x08<<24 | ((uintptr_t)block_end & 0xFFFFFF);
            merge_last_size = (uint8_t *)block_end - (uint8_t *)merge_last_ptr;
            return ptr;
        }
    }

    /* We couldn't merge, so reset the last-block-allocated variables and
     * return the block we allocated above. */
    merge_last_ptr = ptr;
    merge_last_size = size;
    return ptr;
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
