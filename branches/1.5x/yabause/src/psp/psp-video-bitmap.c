/*  src/psp/psp-video-bitmap.c: Bitmapped background graphics handling for
                                PSP video module
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

#include "common.h"

#include "../vidshared.h"

#include "display.h"
#include "gu.h"
#include "psp-video.h"
#include "psp-video-internal.h"
#include "texcache.h"

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * vdp2_draw_bitmap:  Draw a graphics layer bitmap.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_bitmap(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Set up vertices */
    VertexUVXYZ *vertices = pspGuGetMemoryMerge(sizeof(*vertices) * 2);
    vertices[0].u = 0;
    vertices[0].v = 0;
    vertices[0].x = info->x * info->coordincx;
    vertices[0].y = info->y * info->coordincy;
    vertices[0].z = 0;
    vertices[1].u = info->cellw;
    vertices[1].v = info->cellh;
    vertices[1].x = (info->x + info->cellw) * info->coordincx;
    vertices[1].y = (info->y + info->cellh) * info->coordincy;
    vertices[1].z = 0;

    /* FIXME: only very basic clipping processing at the moment; see
     * vidsoft.c for more details on how this works */
    if ((info->wctl & 0x3) == 0x3) {
        vertices[0].x = clip[0].xstart;
        vertices[0].y = clip[0].ystart;
        vertices[1].x = clip[0].xend + 1;
        vertices[1].y = clip[0].yend + 1;
        vertices[1].u = (vertices[1].x - vertices[0].x) / info->coordincx;
        vertices[1].v = (vertices[1].y - vertices[0].y) / info->coordincy;
        /* Offset the bitmap address appropriately */
        const int bpp = (info->colornumber==4 ? 32 :
                         info->colornumber>=2 ? 16 :
                         info->colornumber==1 ?  8 : 4);
        const int xofs = clip[0].xstart - info->x;
        const int yofs = clip[0].ystart - info->y;
        info->charaddr += (yofs * info->cellw + xofs) * bpp / 8;
    }

    /* Draw the bitmap */
    texcache_load_bitmap(
        info->charaddr,
        (vertices[1].u - vertices[0].u + 7) & -8,
        vertices[1].v - vertices[0].v,
        info->cellw, info->colornumber, info->transparencyenable,
        info->coloroffset, info->paladdr << 4,
        info->cor, info->cog, info->cob,
        0  // Bitmaps are likely to change, so don't cache them persistently
    );
    guDrawArray(GU_SPRITES,
                GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                2, NULL, vertices);
}

/*-----------------------------------------------------------------------*/

/**
 * vdp2_draw_bitmap_t8:  Draw an 8-bit indexed graphics layer bitmap.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_bitmap_t8(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Set up vertices */
    // FIXME: this will break with a final (clipped) width above 512;
    // need to split the texture in half in that case
    VertexUVXYZ *vertices = pspGuGetMemoryMerge(sizeof(*vertices) * 2);
    vertices[0].u = 0;
    vertices[0].v = 0;
    vertices[0].x = info->x * info->coordincx;
    vertices[0].y = info->y * info->coordincy;
    vertices[0].z = 0;
    vertices[1].u = info->cellw;
    vertices[1].v = info->cellh;
    vertices[1].x = (info->x + info->cellw) * info->coordincx;
    vertices[1].y = (info->y + info->cellh) * info->coordincy;
    vertices[1].z = 0;

    /* FIXME: only very basic clipping processing at the moment; see
     * vidsoft.c for more details on how this works */
    if ((info->wctl & 0x3) == 0x3) {
        vertices[0].x = clip[0].xstart;
        vertices[0].y = clip[0].ystart;
        vertices[1].x = clip[0].xend + 1;
        vertices[1].y = clip[0].yend + 1;
        vertices[1].u = (vertices[1].x - vertices[0].x) / info->coordincx;
        vertices[1].v = (vertices[1].y - vertices[0].y) / info->coordincy;
        /* Offset the bitmap address appropriately */
        const int bpp = (info->colornumber==4 ? 32 :
                         info->colornumber>=2 ? 16 :
                         info->colornumber==1 ?  8 : 4);
        const int xofs = clip[0].xstart - info->x;
        const int yofs = clip[0].ystart - info->y;
        info->charaddr += (yofs * info->cellw + xofs) * bpp / 8;
    }

    /* Set up the color table */
    guClutMode(GU_PSM_8888, 0, 0xFF, 0);
    void *ptr = vdp2_gen_t8_clut(
        info->coloroffset, info->paladdr<<4,
        info->transparencyenable, info->cor, info->cog, info->cob
    );
    if (LIKELY(ptr)) {
        guClutLoad(256/8, ptr);
    }

    /* Draw the bitmap */
    guTexMode(GU_PSM_8888, 0, 0, 0);
    guTexMode(GU_PSM_T8, 0, 0, 0);
    guTexImage(0, 512, 512, info->cellw, &Vdp2Ram[info->charaddr & 0x7FFFF]);
    guDrawArray(GU_SPRITES,
                GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                2, NULL, vertices);
}

/*-----------------------------------------------------------------------*/

/**
 * vdp2_draw_bitmap_32:  Draw a 32-bit ARGB1888 unscaled graphics layer
 * bitmap.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_bitmap_32(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Determine the area to be drawn */
    unsigned int x0, y0, width, height;
    /* FIXME: only very basic clipping processing at the moment; see
     * vidsoft.c for more details on how this works */
    if ((info->wctl & 0x3) == 0x3) {
        x0 = clip[0].xstart;
        y0 = clip[0].ystart;
        width = (clip[0].xend + 1) - x0;
        height = (clip[0].yend + 1) - y0;
        /* Offset the bitmap address appropriately */
        const int xofs = clip[0].xstart - info->x;
        const int yofs = clip[0].ystart - info->y;
        info->charaddr += (yofs * info->cellw + xofs) * 4;
    } else {
        x0 = info->x;
        y0 = info->y;
        width = info->cellw;
        height = info->cellh;
    }

    /* Set up vertices (using optimized 64-byte-wide strips) */
    // FIXME: this will work incorrectly on bitmaps wider than 512 pixels,
    // if there are any such (VDP2 RAM is only big enough for 512x256)
    const uint32_t nverts = ((width+15) / 16) * 2;
    VertexUVXYZ *vertices = pspGuGetMemoryMerge(sizeof(*vertices) * nverts);
    unsigned int x, i;
    for (x = i = 0; x < width; x += 16, i += 2) {
        const unsigned int thisw = (width-x > 16 ? 16 : width-x);
        vertices[i+0].u = x;
        vertices[i+0].v = 0;
        vertices[i+0].x = x0 + x;
        vertices[i+0].y = y0;
        vertices[i+0].z = 0;
        vertices[i+1].u = x + thisw;
        vertices[i+1].v = height;
        vertices[i+1].x = x0 + x + thisw;
        vertices[i+1].y = y0 + height;
        vertices[i+1].z = 0;
    }

    /* Set up GE parameters for drawing */
    guTexFlush();
    guTexMode(GU_PSM_T32, 0, 0, 0);
    guTexImage(0, 512, 512, info->cellw, &Vdp2Ram[info->charaddr & 0x7FFFF]);
    guAmbientColor(0xFFFFFFFF);
    guTexFunc(GU_TFX_REPLACE, 1);
    guDisable(GU_BLEND);

    /* If transparency is enabled, set up an offscreen buffer for
     * rendering; if we can't (not enough spare VRAM), just draw with
     * transparency disabled */
    uint32_t *offscreen_buffer = NULL;
    uint32_t offscreen_stride = 0;
    if (info->transparencyenable) {
        offscreen_stride = (width + 3) & -4;
        offscreen_buffer = display_alloc_vram(offscreen_stride * height);
        if (offscreen_buffer) {
            /* Adjust the draw buffer pointer so we don't have to mess
             * with the vertex coordinates */
            const uint32_t offset =
                vertices[0].y * offscreen_stride + vertices[0].x;
            guDrawBuffer(GU_PSM_8888,
                         offscreen_buffer - offset, offscreen_stride);
        }
    }

    /* Draw each of the RGB components independently */
    unsigned int rgb;
    for (rgb = 0; rgb < 3; rgb++) {
        /* Set up the color table for this component */
        const int ofs = (rgb==0 ? info->cor : rgb==1 ? info->cog : info->cob);
        void *clut = vdp2_gen_32_clut(ofs);
        if (clut) {
            guClutMode(GU_PSM_8888, (3-rgb)*8, 0xFF, 0);
            guClutLoad(256/8, clut);
        }

        /* Blit this component to the screen.  If we're using transparency,
         * also clear the alpha byte on the first blit so we only need a
         * single "set" operation later. */
        if (offscreen_buffer && rgb == 0) {
            guStencilOp(GU_ZERO, GU_ZERO, GU_ZERO);  // Clear alpha bytes
            guEnable(GU_STENCIL_TEST);
        }
        guPixelMask(~(0xFF0000FF << (rgb*8)));
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts, NULL, vertices);
        if (offscreen_buffer && rgb == 0) {
            guDisable(GU_STENCIL_TEST);
        }
    }
    guPixelMask(0);

    /* Mask off transparent pixels and blit back to the display buffer
     * if appropriate */
    if (offscreen_buffer) {
        static const __attribute__((aligned(64))) uint32_t mask_clut[8] =
            {0, ~0};
        guClutMode(GU_PSM_8888, 7, 0x1, 0);
        guClutLoad(1, mask_clut);
        guAlphaFunc(GU_EQUAL, 0xFF, 0xFF);  // Only pass non-transparent pixels
        guEnable(GU_ALPHA_TEST);
        guStencilFunc(GU_ALWAYS, 0xFF, 0xFF);  // Set drawn alpha bytes to 255
        guStencilOp(GU_REPLACE, GU_REPLACE, GU_REPLACE);
        guEnable(GU_STENCIL_TEST);
        guPixelMask(0xFFFFFF);  // Only modify the alpha byte
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts, NULL, vertices);
        guPixelMask(0);
        guStencilOp(GU_KEEP, GU_KEEP, GU_KEEP);
        guDisable(GU_STENCIL_TEST);
        guDisable(GU_ALPHA_TEST);

        guDrawBuffer(GU_PSM_8888, display_work_buffer(), DISPLAY_STRIDE);
        guTexFlush();
        guTexMode(GU_PSM_8888, 0, 0, 0);
        guTexImage(0, 512, 512, offscreen_stride, offscreen_buffer);
        guTexFunc(GU_TFX_REPLACE, 1);
        guEnable(GU_BLEND);
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts, NULL, vertices);
    }

    /* Turn blending back on before returning (in case we didn't do so for
     * transparency handling), since everyone else expects it to be on */
    guEnable(GU_BLEND);
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
