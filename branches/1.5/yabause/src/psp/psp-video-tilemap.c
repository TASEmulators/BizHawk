/*  src/psp/psp-video-tilemap.c: Tile-mapped background graphics handling
                                 for PSP video module
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
/************************** Tile-drawing macros **************************/
/*************************************************************************/

/*
 * Macros to work through the Saturn's remarkably nested tile layout (used
 * in map drawing routines).  Use like:
 *
 * TILE_LOOP_BEGIN(tilesize) {
 *     ...  // Draw a single tile based on "info" and "vertices"
 * } TILE_LOOP_END;
 *
 * where "tilesize" is 8 or 16 (info->patternwh * 8).  Note that we iterate
 * over the highest-level "maps" twice to handle wraparound.
 */

#define TILE_LOOP_BEGIN(tilesize)  \
    int x1start = info->x;                                              \
    unsigned int y1;                                                    \
    for (y1 = 0; y1 < info->mapwh*2 && info->y < info->drawh; y1++) {   \
        int y2start = info->y;                                          \
        info->x = x1start;                                              \
        unsigned int x1;                                                \
        for (x1 = 0; x1 < info->mapwh*2 && info->x < info->draww; x1++) {\
            info->PlaneAddr(info, ((y1 % info->mapwh) * info->mapwh)    \
                            + (x1 % info->mapwh));                      \
            int x2start = info->x;                                      \
            info->y = y2start;                                          \
            unsigned int y2;                                            \
            for (y2 = 0; y2 < info->planeh; y2++) {                     \
                int y3start = info->y;                                  \
                info->x = x2start;                                      \
                unsigned int x2;                                        \
                for (x2 = 0; x2 < info->planew; x2++) {                 \
                    int x3start = info->x;                              \
                    info->y = y3start;                                  \
                    unsigned int y3;                                    \
                    for (y3 = 0; y3 < info->pagewh;                     \
                         y3++, info->y += (tilesize)                    \
                    ) {                                                 \
                        if (UNLIKELY(info->y <= -(tilesize)             \
                                  || info->y >= info->drawh)) {         \
                            info->addr += info->patterndatasize * 2     \
                                          * info->pagewh;               \
                            continue;                                   \
                        }                                               \
                        info->x = x3start;                              \
                        unsigned int x3;                                \
                        for (x3 = 0; x3 < info->pagewh;                 \
                             x3++, info->x += (tilesize),               \
                                   info->addr += info->patterndatasize * 2 \
                        ) {                                             \
                            if (UNLIKELY(info->x <= -(tilesize)         \
                                      || info->x >= info->draww)) {     \
                                continue;                               \
                            }                                           \
                            vdp2_calc_pattern_address(info);

#define TILE_LOOP_END \
                        }  /* Inner (pattern) X */              \
                    }  /* Inner (pattern) Y */                  \
                }  /* Middle (page) X */                        \
            }  /* Middle (page) Y */                            \
        }  /* Outer (plane) X */                                \
    }  /* Outer (plane) Y */

/*-----------------------------------------------------------------------*/

/* Additional macros used by tile map drawing routines */

/*----------------------------------*/

/* Set up the clipping region for the graphics layer; half_height should be
 * 1 when rendering T8 tiles with the double-stride, two-pass optimization */
#define SET_CLIP_REGION(half_height)                            \
    guScissor(clip->xstart,                                     \
              half_height ? clip->ystart/2 : clip->ystart,      \
              clip->xend - clip->xstart,                        \
              half_height ? clip->yend/2 - clip->ystart/2       \
                          : clip->yend - clip->ystart)

/* Reset the clipping region to default */
#define UNSET_CLIP_REGION                                       \
    guScissor(0, 0, disp_width, disp_height)

/*----------------------------------*/

/* Declare tiledatasize as the size of a single tile's data in bytes. */
#define GET_TILEDATASIZE                                        \
    int tiledatasize;                                           \
    switch (info->colornumber) {                                \
        case 0:  /*  4bpp */ tiledatasize = 8*8/2; break;       \
        case 1:  /*  8bpp */ tiledatasize = 8*8*1; break;       \
        case 2:  /* 16bpp */ tiledatasize = 8*8*2; break;       \
        case 3:  /* 16bpp */ tiledatasize = 8*8*2; break;       \
        case 4:  /* 32bpp */ tiledatasize = 8*8*4; break;       \
        default: DMSG("Bad tile pixel type %d", info->colornumber); \
                 tiledatasize = 0; break;                        \
    }

/* Allocate memory for "vertspertile" vertices per "size"x"size" tile. */
#define GET_VERTICES(size,vertspertile)                         \
    /* We add 4 here to handle up to 15 pixels of partial tiles \
     * on each edge of the display area */                      \
    const int tilew = info->draww / (size) + 4;                 \
    const int tileh = info->drawh / (size) + 4;                 \
    const int nvertices = tilew * tileh * (vertspertile);       \
    VertexUVXYZ *vertices = pspGuGetMemoryMerge(sizeof(*vertices) * nvertices);

/* Initialize variables for 8-bit indexed tile palette handling.  There can
 * be up to 128 different palettes, selectable on a per-tile basis, so to
 * save time, we only create (on the fly) those which are actually used. */
#define INIT_T8_PALETTE                                         \
    uint32_t *palettes[128];                                    \
    memset(palettes, 0, sizeof(palettes));                      \
    int cur_palette = -1;  /* So it's always set the first time */ \
    guClutMode(GU_PSM_8888, 0, 0xFF, 0);

/* Set the texture pixel format for 8-bit indexed tiles.  16-byte-wide
 * textures are effectively swizzled already, so set the swizzled flag for
 * whatever speed boost it gives us. */
#define INIT_T8_TEXTURE                                         \
    guTexMode(GU_PSM_T8, 0, 0, 1);

/* Set the vertex type for 8-bit indexed tiles. */
#define INIT_T8_VERTEX                                          \
    guVertexFormat(GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D);

/* Initialize the work buffer pointers for 8-bit indexed tiles. */
#define INIT_T8_WORK_BUFFER                                     \
    uint32_t * const work_buffer_0 = display_work_buffer();     \
    uint32_t * const work_buffer_1 = work_buffer_0 + DISPLAY_STRIDE;

/* Initialize a variable for tracking empty tiles (initialize with an
 * impossible address so it matches nothing by default). */
#define INIT_EMPTY_TILE                                         \
    uint32_t empty_tile_addr = 1;

/*----------------------------------*/

/* Check whether this is a known empty tile, and skip the loop body if so. */
static const uint8_t CHECK_EMPTY_TILE_bppshift_lut[8] = {2,3,4,4,5,5,5,5};
#define CHECK_EMPTY_TILE(tilesize)                              \
    if (info->charaddr == empty_tile_addr) {                    \
        continue;                                               \
    }                                                           \
    if (info->transparencyenable && empty_tile_addr == 1) {     \
        const uint32_t bppshift =                               \
            CHECK_EMPTY_TILE_bppshift_lut[info->colornumber];   \
        const uint32_t tile_nwords = ((tilesize)*(tilesize)/32) << bppshift; \
        empty_tile_addr = info->charaddr;                       \
        const uint32_t *ptr = (const uint32_t *)&Vdp2Ram[info->charaddr]; \
        const uint32_t *top = ptr + tile_nwords;                \
        for (; ptr < top; ptr++) {                              \
            if (*ptr != 0) {                                    \
                empty_tile_addr = 1;                            \
                break;                                          \
            }                                                   \
        }                                                       \
        if (empty_tile_addr != 1) {                             \
            /* The tile was empty, so we don't need to draw anything */ \
            continue;                                           \
        }                                                       \
    }

/* Declare flip_* and priority with the proper values for "size"x"size"
 * tiles (either 8x8 or 16x16). */
#define GET_FLIP_PRI(size)                                      \
    const int flip_u0 = (info->flipfunction & 1) << ((size)==16 ? 4 : 3); \
    const int flip_u1 = flip_u0 ^ (size);                       \
    const int flip_v0 = (info->flipfunction & 2) << ((size)==16 ? 3 : 2); \
    const int flip_v1 = flip_v0 ^ (size);                       \
    int priority;                                               \
    if (info->specialprimode == 1) {                            \
        priority = (info->priority & ~1) | info->specialfunction; \
    } else {                                                    \
        priority = info->priority;                              \
    }

/* Declare flip_* and priority for 8-bit indexed tiles. */
static const int flip_t8_u[4][4] =
    { {0,8,8,16}, {8,0,16,8}, {8,16,0,8}, {16,8,8,0} };
#define GET_FLIP_PRI_T8                                         \
    const int flip_u0 = flip_t8_u[info->flipfunction][0];       \
    const int flip_u1 = flip_t8_u[info->flipfunction][1];       \
    const int flip_u2 = flip_t8_u[info->flipfunction][2];       \
    const int flip_u3 = flip_t8_u[info->flipfunction][3];       \
    const int flip_v0 = (info->flipfunction & 2) << 1;          \
    const int flip_v1 = flip_v0 ^ 4;                            \
    int priority;                                               \
    if (info->specialprimode == 1) {                            \
        priority = (info->priority & ~1) | info->specialfunction; \
    } else {                                                    \
        priority = info->priority;                              \
    }

/* Update the current palette for an 8-bit indexed tile, if necessary. */
#define UPDATE_T8_PALETTE                                       \
    if (info->paladdr != cur_palette) {                         \
        cur_palette = info->paladdr;                            \
        if (UNLIKELY(!palettes[cur_palette])) {                 \
            palettes[cur_palette] = vdp2_gen_t8_clut(           \
                info->coloroffset, info->paladdr<<4,            \
                info->transparencyenable, info->cor, info->cog, info->cob \
            );                                                  \
        }                                                       \
        if (LIKELY(palettes[cur_palette])) {                    \
            const uint32_t * const clut = palettes[cur_palette];\
            guClutLoad(256/8, clut);                            \
        }                                                       \
    }

/* Define 2 vertices for a generic 8x8 or 16x16 tile. */
#define SET_VERTICES(tilex,tiley,xsize,ysize)                   \
    vertices[0].u = flip_u0;                                    \
    vertices[0].v = flip_v0;                                    \
    vertices[0].x = (tilex);                                    \
    vertices[0].y = (tiley);                                    \
    vertices[0].z = 0;                                          \
    vertices[1].u = flip_u1;                                    \
    vertices[1].v = flip_v1;                                    \
    vertices[1].x = (tilex) + (xsize);                          \
    vertices[1].y = (tiley) + (ysize);                          \
    vertices[1].z = 0;

/* Define 2 vertices for the even lines of an 8-bit indexed 8x8 tile. */
#define SET_VERTICES_T8_EVEN(tilex,tiley,xsize,ysize)           \
    vertices[0].u = flip_u0;                                    \
    vertices[0].v = yofs + flip_v0;                             \
    vertices[0].x = (tilex);                                    \
    vertices[0].y = (tiley) / 2;                                \
    vertices[0].z = 0;                                          \
    vertices[1].u = flip_u1;                                    \
    vertices[1].v = yofs + flip_v1;                             \
    vertices[1].x = (tilex) + (xsize);                          \
    vertices[1].y = ((tiley) + (ysize)) / 2;                    \
    vertices[1].z = 0;

/* Define 2 vertices for the odd lines of an 8-bit indexed 8x8 tile. */
#define SET_VERTICES_T8_ODD(tilex,tiley,xsize,ysize)            \
    vertices[2].u = flip_u2;                                    \
    vertices[2].v = flip_v0;                                    \
    vertices[2].x = (tilex);                                    \
    vertices[2].y = (tiley) / 2;                                \
    vertices[2].z = 0;                                          \
    vertices[3].u = flip_u3;                                    \
    vertices[3].v = flip_v1;                                    \
    vertices[3].x = (tilex) + (xsize);                          \
    vertices[3].y = ((tiley) + (ysize)) / 2;                    \
    vertices[3].z = 0;

/* Load the texture pointer for an 8-bit indexed 8x8 tile. */
#define LOAD_T8_TILE                                            \
    guTexFlush();                                               \
    guTexImage(0, 512, 512, 16, src);

/* Set the even-lines work buffer for 8-bit indexed 8x8 tiles. */
#define SET_T8_BUFFER_0                                         \
    guDrawBuffer(GU_PSM_8888, work_buffer_0, DISPLAY_STRIDE*2);

/* Draw the even lines of an 8-bit indexed 8x8 tile */
#define RENDER_T8_EVEN                                          \
    guVertexPointer(vertices);                                  \
    guDrawPrimitive(GU_SPRITES, 2);

/* Set the odd-lines work buffer for 8-bit indexed 8x8 tiles. */
#define SET_T8_BUFFER_1                                         \
    guDrawBuffer(GU_PSM_8888, work_buffer_1, DISPLAY_STRIDE*2);

/* Draw the odd lines of an 8-bit indexed 8x8 tile. */
#define RENDER_T8_ODD                                           \
    guVertexPointer(vertices+2);                                \
    guDrawPrimitive(GU_SPRITES, 2);

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * vdp2_draw_map_8x8:  Draw a graphics layer composed of 8x8 patterns of
 * any format.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_map_8x8(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Allocate vertex memory and perform other initialization */
    SET_CLIP_REGION(0);
    GET_VERTICES(8, 2);
    INIT_EMPTY_TILE;

    /* Loop through tiles */
    TILE_LOOP_BEGIN(8) {
        CHECK_EMPTY_TILE(8);
        GET_FLIP_PRI(8);
        SET_VERTICES(info->x * info->coordincx, info->y * info->coordincy,
                     8 * info->coordincx, 8 * info->coordincy);
        texcache_load_tile(8, info->charaddr, info->colornumber,
                           info->transparencyenable,
                           info->coloroffset, info->paladdr << 4,
                           info->cor, info->cog, info->cob,
                           vdp2_is_persistent(info->charaddr));
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    2, NULL, vertices);
        vertices += 2;
    } TILE_LOOP_END;

    /* Reset locally-changed GE settings */
    UNSET_CLIP_REGION;
}

/*-----------------------------------------------------------------------*/

/**
 * vdp2_draw_map_8x8_t8:  Draw a graphics layer composed of 8-bit indexed
 * color 8x8 patterns.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_map_8x8_t8(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Check the current screen mode; if we're in interlaced mode, we can
     * cheat by treating each 8x8 tile as a 16x4 texture and drawing only
     * the left or right half, thus omitting alternate lines for free. */
    const int interlaced = (disp_height > 272);

    /* Allocate vertex memory and perform other initialization.  Note that
     * we need 2 sprites to draw each tile if we're not optimizing
     * interlaced graphics. */
    SET_CLIP_REGION(!interlaced);
    GET_VERTICES(8, interlaced ? 2 : 4);
    INIT_T8_PALETTE;
    INIT_T8_TEXTURE;
    INIT_T8_VERTEX;
    INIT_T8_WORK_BUFFER;
    INIT_EMPTY_TILE;

    /* Loop through tiles */
    TILE_LOOP_BEGIN(8) {
        CHECK_EMPTY_TILE(8);
        GET_FLIP_PRI_T8;
        UPDATE_T8_PALETTE;

        /* Set up vertices and draw the tile */
        const uint8_t *src = &Vdp2Ram[info->charaddr];
        int yofs = ((uintptr_t)src & 63) / 16;
        src = (const uint8_t *)((uintptr_t)src & ~63);
        SET_VERTICES_T8_EVEN(info->x * info->coordincx,
                             info->y * info->coordincy,
                             8 * info->coordincx, 8 * info->coordincy);
        if (!interlaced) {
            SET_VERTICES_T8_ODD(info->x * info->coordincx,
                                info->y * info->coordincy,
                                8 * info->coordincx, 8 * info->coordincy);
        } else {
            /* We don't modify the work buffer stride in this case, so double
             * the Y coordinates (which were set assuming a doubled stride) */
            vertices[0].y *= 2;
            vertices[1].y *= 2;
        }
        LOAD_T8_TILE;
        if (!interlaced) {
            SET_T8_BUFFER_0;
        }
        RENDER_T8_EVEN;
        if (!interlaced) {
            SET_T8_BUFFER_1;
            RENDER_T8_ODD;
            vertices += 4;
        } else {
            /* Interlaced, so drop odd lines of tile */
            vertices += 2;
        }
    } TILE_LOOP_END;

    /* Reset locally-changed GE settings */
    UNSET_CLIP_REGION;
    if (!interlaced) {
        guDrawBuffer(GU_PSM_8888, work_buffer_0, DISPLAY_STRIDE);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * vdp2_draw_map_16x16:  Draw a graphics layer composed of 16x16 patterns
 * of any format.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_map_16x16(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Determine tile data size */
    GET_TILEDATASIZE;

    /* Allocate vertex memory and perform other initialization */
    SET_CLIP_REGION(0);
    GET_VERTICES(16, 2);
    INIT_EMPTY_TILE;

    /* Loop through tiles */
    TILE_LOOP_BEGIN(16) {
        CHECK_EMPTY_TILE(16);
        GET_FLIP_PRI(16);

        SET_VERTICES(info->x * info->coordincx, info->y * info->coordincy,
                     16 * info->coordincx, 16 * info->coordincy);
        texcache_load_tile(16, info->charaddr, info->colornumber,
                           info->transparencyenable,
                           info->coloroffset, info->paladdr << 4,
                           info->cor, info->cog, info->cob,
                           vdp2_is_persistent(info->charaddr));
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    2, NULL, vertices);
        vertices += 2;
    } TILE_LOOP_END;

    /* Reset locally-changed GE settings */
    UNSET_CLIP_REGION;
}

/*-----------------------------------------------------------------------*/

/**
 * vdp2_draw_map_16x16_t8:  Draw a graphics layer composed of 8-bit indexed
 * color 16x16 patterns.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_map_16x16_t8(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Check the current screen mode */
    const int interlaced = (disp_height > 272);

    /* Allocate vertex memory and perform other initialization */
    SET_CLIP_REGION(!interlaced);
    GET_VERTICES(8, interlaced ? 2 : 4);
    INIT_T8_PALETTE;
    INIT_T8_TEXTURE;
    INIT_T8_VERTEX;
    INIT_T8_WORK_BUFFER;
    INIT_EMPTY_TILE;

    /* Loop through tiles */
    TILE_LOOP_BEGIN(16) {
        CHECK_EMPTY_TILE(16);
        GET_FLIP_PRI_T8;
        UPDATE_T8_PALETTE;

        const uint8_t *src = &Vdp2Ram[info->charaddr];
        int yofs = ((uintptr_t)src & 63) / 16;
        src = (const uint8_t *)((uintptr_t)src & ~63);
        int tilenum;
        for (tilenum = 0; tilenum < 4; tilenum++, src += 8*8*1) {
            const int tilex = info->x + (8 * ((tilenum % 2) ^ (info->flipfunction & 1)));
            const int tiley = info->y + (8 * ((tilenum / 2) ^ ((info->flipfunction & 2) >> 1)));
            SET_VERTICES_T8_EVEN(tilex * info->coordincx,
                                 tiley * info->coordincy,
                                 8 * info->coordincx, 8 * info->coordincy);
            if (!interlaced) {
                SET_VERTICES_T8_ODD(tilex * info->coordincx,
                                    tiley * info->coordincy,
                                    8 * info->coordincx, 8 * info->coordincy);
            } else {
                vertices[0].y *= 2;
                vertices[1].y *= 2;
            }

            LOAD_T8_TILE;
            if (!interlaced) {
                SET_T8_BUFFER_0;
            }
            RENDER_T8_EVEN;
            if (!interlaced) {
                SET_T8_BUFFER_1;
                RENDER_T8_ODD;
                vertices += 4;
            } else {
                vertices += 2;
            }
        }
    } TILE_LOOP_END;

    /* Reset locally-changed GE settings */
    UNSET_CLIP_REGION;
    if (!interlaced) {
        guDrawBuffer(GU_PSM_8888, work_buffer_0, DISPLAY_STRIDE);
    }
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
