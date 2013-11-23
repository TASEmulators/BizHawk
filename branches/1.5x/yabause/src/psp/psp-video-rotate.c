/*  src/psp/psp-video-rotate.c: Rotated background graphics handling for
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

#include "config.h"
#include "gu.h"
#include "psp-video.h"
#include "psp-video-internal.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/**
 * USE_FIXED_POINT:  If defined, floating-point operations will be replaced
 * by signed 16.16 fixed-point operations.  This may slightly change
 * behavior due to the differing precision of computations.
 *
 * This currently (2010/3/9) gives an improvement of 5-10% with multiple
 * coefficients per row or 3-5% with one coefficient per row.
 */
#define USE_FIXED_POINT

/*************************************************************************/

/**** Floating-point / fixed-point operations ****/

#ifdef USE_FIXED_POINT

# define FIXEDFLOAT   int32_t
# define UFIXEDFLOAT  uint32_t
# define FIXED_MULT(a,b)    ((int32_t)(((int64_t)(a) * (int64_t)(b)) >> 16))
# define FIXED_TOINT(n)     ((n) >> 16)
# define FIXED_TOFLOAT(n)   ((n) / 65536.0f)

#else  // !USE_FIXED_POINT

# define FIXEDFLOAT   float
# define UFIXEDFLOAT  float
# define FIXED_MULT(a,b)    ((a) * (b))
# define FIXED_TOINT(n)     ifloorf((n))
# define FIXED_TOFLOAT(n)   ((n))

#endif  // USE_FIXED_POINT

/**** Coordinate transformation parameter structure ****/

typedef struct RotationParams_ RotationParams;
struct RotationParams_ {
    /* Transformation parameters read from VDP2 RAM (arranged for easy
     * VFPU loading, in case that ever becomes useful) */
    __attribute__((aligned(16))) FIXEDFLOAT Xst;
    FIXEDFLOAT Yst, Zst, pad0w;
    FIXEDFLOAT deltaXst, deltaYst, pad1z, pad1w;
    FIXEDFLOAT deltaX, deltaY, pad2z, pad2w;
    FIXEDFLOAT A, B, C, pad3w;
    FIXEDFLOAT D, E, F, pad4w;
    FIXEDFLOAT Px, Py, Pz, pad5w;
    FIXEDFLOAT Cx, Cy, Cz, pad6w;
    FIXEDFLOAT Mx, My, pad7z, pad7w;
    FIXEDFLOAT kx, ky, pad8z, pad8w; //May be updated in coefficient table mode

    /* Computed transformation parameters */
    FIXEDFLOAT Xp, Yp, pad9z, pad9w; //May be updated in coefficient table mode
    FIXEDFLOAT mat11, mat12, mat13, mat1_pad;
    FIXEDFLOAT mat21, mat22, mat23, mat2_pad;

    /* Coefficient table parameters read from VDP2 RAM */
    UFIXEDFLOAT KAst;
    FIXEDFLOAT deltaKAst;
    FIXEDFLOAT deltaKAx;

    /* Coefficient table base address and flags */
    uint32_t coeftbladdr;
    uint8_t coefenab;
    uint8_t coefmode;
    uint8_t coefdatasize;  // Size of a single coefficient in bytes (2 or 4)
    uint8_t coefdatashift; // log2(coefdatasize)

    /* Miscellaneous parameters */
    uint8_t screenover;  // FIXME: What is this for?
};

/*************************************************************************/

/**** Macros for pixel address calculation and pixel getting ****/

/**
 * INIT_CALC_PIXELNUM:  Precompute values used by the CALC_PIXELNUM macro.
 */
#define INIT_CALC_PIXELNUM                                              \
    const int srcx_mask = info->isbitmap                                \
                          ? info->cellw - 1                             \
                          : (8 * 64 * info->planew * 4) - 1;            \
    const int srcy_mask = info->isbitmap                                \
                          ? info->cellh - 1                             \
                          : (8 * 64 * info->planeh * 4) - 1;            \
    const int page_shift     = 3 + 6;                                   \
    const int page_mask      = (1 << page_shift) - 1;                   \
    const int tile_shift     = (info->patternwh==2) ? 4 : 3;            \
    /* Remember the last tile seen, to save the time of looking it up   \
     * while we're on nearby pixels */                                  \
    int last_tilex = -1, last_tiley = -1

/*----------------------------------*/

/**
 * INIT_PAGEMAP:  Initialize a page map array for the given parameter
 * set (either 0 or 1).  pagemap should be declared as:
 *    uint32_t pagemap[8][8];
 */
#define INIT_PAGEMAP(pagemap,set)  do {                                 \
    int plane_aoffset = ((Vdp2Regs->MPOFR >> ((set) * 4)) & 7) << 6;    \
    const uint8_t *plane_map = set ? (const uint8_t *)&Vdp2Regs->MPABRB \
                                   : (const uint8_t *)&Vdp2Regs->MPABRA;\
    const int plane_bits     = info->planew_bits + info->planeh_bits;   \
    const int plane_ashift   = 11 + (info->patternwh==1 ? 2 : 0)        \
                                  + (info->patterndatasize==2 ? 1 : 0); \
    const int plane_amask    = (0xFF >> (plane_ashift - 11))            \
                               ^ ((1 << plane_bits) - 1);               \
    unsigned int planenum = 0;                                          \
    unsigned int plane_y;                                               \
    for (plane_y = 0; plane_y < 4; plane_y++) {                         \
        unsigned int plane_x;                                           \
        for (plane_x = 0; plane_x < 4; plane_x++, planenum++) {         \
            uint32_t address =                                          \
                ((plane_aoffset | plane_map[planenum]) & plane_amask)   \
                 << plane_ashift;                                       \
            unsigned int pagenum = 0;                                   \
            unsigned int page_y;                                        \
            for (page_y = 0; page_y < info->planeh; page_y++) {         \
                unsigned int page_x;                                    \
                for (page_x = 0; page_x < info->planew; page_x++, pagenum++) {\
                    const int tilenum =                                 \
                        pagenum << (2*(page_shift-tile_shift));         \
                    pagemap[plane_y*info->planeh + page_y]              \
                           [plane_x*info->planew + page_x] =            \
                        address + (tilenum << (info->patterndatasize_bits+1));\
                }                                                       \
            }                                                           \
        }                                                               \
    }                                                                   \
} while (0)

/*----------------------------------*/

/**
 * CALC_TILEADDR_8x8, CALC_TILEADDR_16x16:  Calculate the tile data address
 * associated with the tile coordinate (tilex,tiley) for 8x8 or 16x16 tiles.
 * Helper macros for CALC_PIXELNUM.
 */
#define CALC_TILEADDR_8x8(tilex,tiley,pagemap)  do {                    \
    const int page_x = srcx >> page_shift;                              \
    const int page_y = srcy >> page_shift;                              \
    info->addr = pagemap[page_y][page_x];                               \
    const int tile_x = (srcx & page_mask) >> 3;                         \
    const int tile_y = (srcy & page_mask) >> 3;                         \
    const int tilenum = tile_y << (page_shift - 3) | tile_x;            \
    info->addr += tilenum << (info->patterndatasize_bits + 1);          \
} while (0)

#define CALC_TILEADDR_16x16(tilex,tiley,pagemap)  do {                  \
    const int page_x = srcx >> page_shift;                              \
    const int page_y = srcy >> page_shift;                              \
    info->addr = pagemap[page_y][page_x];                               \
    const int tile_x = (srcx & page_mask) >> 4;                         \
    const int tile_y = (srcy & page_mask) >> 4;                         \
    const int tilenum = tile_y << (page_shift - 4) | tile_x;            \
    info->addr += tilenum << (info->patterndatasize_bits + 1);          \
} while (0)

/*----------------------------------*/

/**
 * CALC_PIXELNUM:  Calculate the pixel index associated with the
 * transformed coordinates (srcx,srcy), store it in the variable
 * "pixelnum", and update the "info" structure's fields as necessary for
 * the associated tile (if the graphics layer is in tilemap mode).
 *
 * This is implemented as a macro so it can make use of precomputed
 * constant values within the calling routine.
 *
 * Optimization note:  The use of nearly identical code in the
 * if(tile_shift==4) and else branches may look awkward, but it allows the
 * compiler to use known values rather than having to repeatedly check and
 * branch on the tile size, giving a speed increase of 3-5%.  Greater
 * optimization could be achieved by pulling such tests further out of the
 * critical path, at the cost of code size explosion; one alternative
 * would be dynamic compilation/assembly of rotation code based on the
 * graphics layer's parameters.
 */
#define CALC_PIXELNUM(pagemap)  do {                                    \
    srcx &= srcx_mask;                                                  \
    srcy &= srcy_mask;                                                  \
    if (info->isbitmap) {                                               \
        pixelnum = srcy << info->cellw_bits | srcx;                     \
    } else if (tile_shift == 4) {                                       \
        const int tilex = srcx >> 4;                                    \
        const int tiley = srcy >> 4;                                    \
        if (tilex != last_tilex || tiley != last_tiley) {               \
            last_tilex = tilex;                                         \
            last_tiley = tiley;                                         \
            CALC_TILEADDR_16x16(tilex, tiley, pagemap);                 \
            vdp2_calc_pattern_address_16x16(info);                      \
        }                                                               \
        srcx &= 15;                                                     \
        srcy &= 15;                                                     \
        if (info->flipfunction & 1) {                                   \
            srcx ^= 15;                                                 \
        }                                                               \
        if (info->flipfunction & 2) {                                   \
            srcy ^= 15;                                                 \
        }                                                               \
        pixelnum = (srcy & 8) << (7-3)                                  \
                 | (srcx & 8) << (6-3)                                  \
                 | (srcy & 7) << 3                                      \
                 | (srcx & 7);                                          \
    } else { /* tile_shift == 3 */                                      \
        const int tilex = srcx >> 3;                                    \
        const int tiley = srcy >> 3;                                    \
        if (tilex != last_tilex || tiley != last_tiley) {               \
            last_tilex = tilex;                                         \
            last_tiley = tiley;                                         \
            CALC_TILEADDR_8x8(tilex, tiley, pagemap);                   \
            vdp2_calc_pattern_address_8x8(info);                        \
        }                                                               \
        srcx &= 7;                                                      \
        srcy &= 7;                                                      \
        if (info->flipfunction & 1) {                                   \
            srcx ^= 7;                                                  \
        }                                                               \
        if (info->flipfunction & 2) {                                   \
            srcy ^= 7;                                                  \
        }                                                               \
        pixelnum = srcy << 3 | srcx;                                    \
    }                                                                   \
} while (0)

/*-----------------------------------------------------------------------*/

/**
 * SELECT_GET_PIXEL:  Declares "get_pixel" as a function pointer to an
 * appropriate pixel-getting function based on the graphics layer's
 * parameters.
 */

#define SELECT_GET_PIXEL                                                \
    const int need_adjust =                                             \
        (info->alpha != 0xFF) || info->cor || info->cog || info->cob;   \
    uint32_t (*get_pixel)(vdp2draw_struct *, unsigned int);             \
    switch (info->colornumber) {                                        \
      case 0:                                                           \
        get_pixel = need_adjust                                         \
                        ? rotation_get_pixel_t4_adjust                  \
                        : info->transparencyenable                      \
                              ? rotation_get_pixel_t4_transparent       \
                              : rotation_get_pixel_t4;                  \
        break;                                                          \
      case 1:                                                           \
        get_pixel = need_adjust                                         \
                        ? rotation_get_pixel_t8_adjust                  \
                        : info->transparencyenable                      \
                              ? rotation_get_pixel_t8_transparent       \
                              : rotation_get_pixel_t8;                  \
        break;                                                          \
      case 2:                                                           \
        get_pixel = need_adjust                                         \
                        ? rotation_get_pixel_t16_adjust                 \
                        : info->transparencyenable                      \
                              ? rotation_get_pixel_t16_transparent      \
                              : rotation_get_pixel_t16;                 \
        break;                                                          \
      case 3:                                                           \
        get_pixel = need_adjust                                         \
                        ? rotation_get_pixel_16_adjust                  \
                        : info->transparencyenable                      \
                              ? rotation_get_pixel_16_transparent       \
                              : rotation_get_pixel_16;                  \
        break;                                                          \
      case 4:                                                           \
        get_pixel = need_adjust                                         \
                        ? rotation_get_pixel_32_adjust                  \
                        : info->transparencyenable                      \
                              ? rotation_get_pixel_32_transparent       \
                              : rotation_get_pixel_32;                  \
        break;                                                          \
      default:                                                          \
        DMSG("Invalid pixel format %d", info->colornumber);             \
        return;                                                         \
    }

/*************************************************************************/

/**** Local function declarations ****/

static void render_mode0(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2], int which);
static void render_mode0_region(uint32_t *pixelbuf, vdp2draw_struct *info,
                                RotationParams param_set[2], int which,
                                unsigned int x0, unsigned int y0,
                                unsigned int xlim, unsigned int ylim);

static void render_mode1(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2]);
static int mode1_is_split_screen(const RotationParams param_set[2],
                                 int *top_set_ret, int *switch_y_ret);

static void render_mode2(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2]);

static void get_rotation_parameters(vdp2draw_struct *info, int which,
                                    RotationParams *param_ret);

__attribute__((unused))
static int get_rotation_coefficient(RotationParams *param, uint32_t address);
static int get_rotation_coefficient_size2_mode0(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size2_mode1(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size2_mode2(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size2_mode3(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size4_mode0(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size4_mode1(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size4_mode2(RotationParams *param,
                                                const void *address);
static int get_rotation_coefficient_size4_mode3(RotationParams *param,
                                                const void *address);

static void transform_coordinates(const RotationParams *param, int x, int y,
                                  FIXEDFLOAT *srcx_ret, FIXEDFLOAT *srcy_ret);

static uint32_t rotation_get_pixel_t4(vdp2draw_struct *info,
                                      unsigned int pixelnum);
static uint32_t rotation_get_pixel_t8(vdp2draw_struct *info,
                                      unsigned int pixelnum);
static uint32_t rotation_get_pixel_t16(vdp2draw_struct *info,
                                       unsigned int pixelnum);
static uint32_t rotation_get_pixel_16(vdp2draw_struct *info,
                                      unsigned int pixelnum);
static uint32_t rotation_get_pixel_32(vdp2draw_struct *info,
                                      unsigned int pixelnum);
static uint32_t rotation_get_pixel_t4_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum);
static uint32_t rotation_get_pixel_t8_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum);
static uint32_t rotation_get_pixel_t16_transparent(vdp2draw_struct *info,
                                                   unsigned int pixelnum);
static uint32_t rotation_get_pixel_16_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum);
static uint32_t rotation_get_pixel_32_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum);
static uint32_t rotation_get_pixel_t4_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum);
static uint32_t rotation_get_pixel_t8_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum);
static uint32_t rotation_get_pixel_t16_adjust(vdp2draw_struct *info,
                                              unsigned int pixelnum);
static uint32_t rotation_get_pixel_16_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum);
static uint32_t rotation_get_pixel_32_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum);

/*-----------------------------------------------------------------------*/

/* Table of coefficient-reading functions, indexed by
 * [param.coefdatasize==4][param.coefmode] */

static int (*get_coef_table[2][4])(RotationParams *, const void *) = {
    {get_rotation_coefficient_size2_mode0,
     get_rotation_coefficient_size2_mode1,
     get_rotation_coefficient_size2_mode2,
     get_rotation_coefficient_size2_mode3},
    {get_rotation_coefficient_size4_mode0,
     get_rotation_coefficient_size4_mode1,
     get_rotation_coefficient_size4_mode2,
     get_rotation_coefficient_size4_mode3}
};

/*************************************************************************/
/************************** Interface function ***************************/
/*************************************************************************/

/**
 * vdp2_draw_map_rotated:  Draw a rotated graphics layer.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     None
 */
void vdp2_draw_map_rotated(vdp2draw_struct *info, const clipping_struct *clip)
{
    /* Parse rotation parameters. */

    static __attribute__((aligned(16))) RotationParams param_set[2];
    get_rotation_parameters(info, 0, &param_set[0]);
    get_rotation_parameters(info, 1, &param_set[1]);

    /* Allocate a buffer for drawing the texture.  Note that we swizzle
     * the texture for faster drawing, since it's allocated in system
     * RAM (which the GE is slow at accessing). */

    uint32_t *pixelbuf = guGetMemory(disp_width * disp_height * 4 + 60);
    pixelbuf = (uint32_t *)(((uintptr_t)pixelbuf + 63) & -64);

    /* Render all pixels. */

    switch (info->rotatemode) {
      case 0:
        render_mode0(pixelbuf, info, clip, param_set, info->rotatenum);
        break;
      case 1:
        render_mode1(pixelbuf, info, clip, param_set);
        break;
      case 2:
        render_mode2(pixelbuf, info, clip, param_set);
        break;
    }

    /* Set up vertices for optimized GE drawing. */

    const unsigned int nverts = (disp_width / 16) * 2;
    VertexUVXYZ *vertices = guGetMemory(sizeof(*vertices) * nverts);
    VertexUVXYZ *vptr;
    unsigned int x;
    for (x = 0, vptr = vertices; x < disp_width; x += 16, vptr += 2) {
        vptr[0].u = x;
        vptr[0].v = 0;
        vptr[0].x = x >> disp_xscale;
        vptr[0].y = 0;
        vptr[0].z = 0;
        vptr[1].u = x + 16;
        vptr[1].v = disp_height;
        vptr[1].x = (x + 16) >> disp_xscale;
        vptr[1].y = disp_height >> disp_yscale;
        vptr[1].z = 0;
    }

    /* Send the texture to the GE. */

    guTexFlush();
    guTexMode(GU_PSM_8888, 0, 0, 1 /*swizzled*/);
    if (disp_width <= 512) {
        guTexImage(0, 512, 512, disp_width, pixelbuf);
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts, NULL, vertices);
    } else {
        guTexImage(0, 512, 512, disp_width, pixelbuf);
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts/2, NULL, vertices);
        for (x = disp_width/2, vptr = vertices + nverts/2; x < disp_width;
             x += 16, vptr += 2
        ) {
            vptr[0].u = x - disp_width/2;
            vptr[1].u = (x + 16) - disp_width/2;
        }
        guTexImage(0, 512, 512, disp_width, pixelbuf + (disp_width/2) * 8);
        guDrawArray(GU_SPRITES,
                    GU_TEXTURE_16BIT | GU_VERTEX_16BIT | GU_TRANSFORM_2D,
                    nverts/2, NULL, vertices + nverts/2);
    }
    guCommit();
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * render_mode0:  Render a rotated/distorted graphics layer in mode 0
 * (fixed parameter set).
 *
 * [Parameters]
 *      pixelbuf: Pointer to output pixel buffer
 *          info: Graphics layer data
 *          clip: Clipping window data
 *     param_set: Array of rotation parameter sets
 *         which: Which parameter set to use (0 or 1)
 * [Return value]
 *     None
 */
static void render_mode0(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2], int which)
{
    /*
     * If the parameter set uses a fixed transformation matrix that doesn't
     * actually perform any rotation, we can just call the regular map
     * drawing functions with appropriately scaled parameters:
     *
     *     [srcX]   (  [screenX])   [kx]   [Xp]
     *     [srcY] = (M [screenY]) * [ky] + [Yp]
     *              (  [   1   ])
     *
     *     [srcX]   ([mat11    0    mat13] [screenX])   [kx]   [Xp]
     *     [srcY] = ([  0    mat22  mat23] [screenY]) * [ky] + [Yp]
     *              (                      [   1   ])
     *
     *     srcX = (mat11*screenX + mat13) * kx + Xp
     *     srcY = (mat22*screenY + mat23) * ky + Yp
     *
     *     srcX = (mat11*kx)*screenX + (mat13*kx + Xp)
     *     srcY = (mat22*ky)*screenY + (mat23*ky + Yp)
     *
     *     (srcX - (mat13*kx + Xp)) / (mat11*kx) = screenX
     *     (srcY - (mat23*ky + Yp)) / (mat22*ky) = screenX
     *
     *     screenX = (1/(mat11*kx))*srcX - ((mat13*kx + Xp) / (mat11*kx))
     *     screenY = (1/(mat22*ky))*srcX - ((mat23*ky + Yp) / (mat22*ky))
     */

    RotationParams *param = &param_set[which];
    if (!param->coefenab && param->mat12 == 0.0f && param->mat21 == 0.0f) {
        const float xmul =
            1 / FIXED_TOFLOAT(FIXED_MULT(param->mat11, param->kx));
        const float ymul =
            1 / FIXED_TOFLOAT(FIXED_MULT(param->mat22, param->ky));
        info->coordincx = xmul;
        info->coordincy = ymul;
        info->x = -FIXED_TOFLOAT(FIXED_MULT(param->mat13, param->kx) + param->Xp) * xmul;
        info->y = -FIXED_TOFLOAT(FIXED_MULT(param->mat23, param->ky) + param->Yp) * ymul;
        void (*draw_func)(vdp2draw_struct *info, const clipping_struct *clip);
        if (info->isbitmap) {
            draw_func = &vdp2_draw_bitmap;
        } else if (info->patternwh == 2) {
            draw_func = &vdp2_draw_map_16x16;
        } else {
            draw_func = &vdp2_draw_map_8x8;
        }
        return (*draw_func)(info, clip);
    }

    /*
     * There's rotation and/or distortion going on, so we'll have to render
     * the image manually.  (Sadly, the PSP doesn't have shaders, so we
     * can't translate the coefficients into a texture coordinate map and
     * render that way.)
     */

    render_mode0_region(pixelbuf, info, param_set, which,
                        0, 0, disp_width, disp_height);
}

/*----------------------------------*/

/**
 * render_mode0_region:  Render the given portion of a rotated/distorted
 * graphics layer in mode 0 (fixed parameter set).
 *
 * [Parameters]
 *       pixelbuf: Pointer to output pixel buffer
 *           info: Graphics layer data
 *      param_set: Array of rotation parameter sets
 *          which: Which parameter set to use (0 or 1)
 *         x0, y0: Top-left coordinates of region to render
 *     xlim, ylim: Bottom-right coordinates of region to render plus one
 * [Return value]
 *     None
 */
static void render_mode0_region(uint32_t *pixelbuf, vdp2draw_struct *info,
                                RotationParams param_set[2], int which,
                                unsigned int x0, unsigned int y0,
                                unsigned int xlim, unsigned int ylim)
{
    RotationParams *param = &param_set[which];

    /* Precalculate tilemap/bitmap coordinate masks, shift counts, and
     * tile page addresses. */

    INIT_CALC_PIXELNUM;
    uint32_t pagemap[8][8];
    if (!info->isbitmap) {
        INIT_PAGEMAP(pagemap, which);
    }

    /* Choose appropriate coefficient-read and pixel-read functions. */

    int (*get_coef)(RotationParams *, const void *) =
        get_coef_table[param->coefdatasize==4][param->coefmode];
    SELECT_GET_PIXEL;

    /* Actually render the graphics layer. */

    /* These two variables are intentionally float rather than FIXEDFLOAT
     * because coef_y can exceed the range of a fixed-point value. */
    float coef_dy = FIXED_TOFLOAT(param->deltaKAst);
    float coef_y = y0 * coef_dy;
    unsigned int y;

    for (y = y0; y < ylim; y++, coef_y += coef_dy) {

        const uint32_t coef_base = param->coeftbladdr
            + (ifloorf(coef_y) << param->coefdatashift);
        const uint8_t *coef_baseptr = Vdp2Ram + (coef_base & 0x7FFFF);
        uint32_t * const dest_base = pixelbuf + (y/8)*(disp_width*8) + (y%8)*4;

        if (!param->coefenab) {

            /* Constant parameters for the whole screen (FIXME: we could
             * draw this in hardware if we had code to rotate vertices) */
            FIXEDFLOAT srcx_f, srcy_f;
            transform_coordinates(param, x0, y, &srcx_f, &srcy_f);
            const FIXEDFLOAT delta_srcx = FIXED_MULT(param->mat11, param->kx);
            const FIXEDFLOAT delta_srcy = FIXED_MULT(param->mat21, param->ky);
            unsigned int x;
            for (x = x0; x < xlim;
                 x++, srcx_f += delta_srcx, srcy_f += delta_srcy
            ) {
                uint32_t *dest = dest_base + (x/4)*32 + x%4;
                int srcx = FIXED_TOINT(srcx_f);
                int srcy = FIXED_TOINT(srcy_f);
                unsigned int pixelnum;
                CALC_PIXELNUM(pagemap);
                *dest = (*get_pixel)(info, pixelnum);
            }

        } else if (param->deltaKAx == 0) {

            /* One coefficient for the whole row */
            if (!(*get_coef)(param, coef_baseptr)) {
                /* Empty row */
                uint32_t *dest = dest_base + (x0/4)*32;
                unsigned int x = x0;
                if (UNLIKELY(x & 3)) {
                    for (; x & 3; x++) {
                        dest[x & 3] = 0;
                    }
                    dest += 32;
                }
                for (; x < (xlim & ~3); x += 4, dest += 32) {
                    dest[0] = dest[1] = dest[2] = dest[3] = 0;
                }
                for (; x < xlim; x++) {
                    dest[x & 3] = 0;
                }
                continue;
            }
            FIXEDFLOAT srcx_f, srcy_f;
            transform_coordinates(param, x0, y, &srcx_f, &srcy_f);
            const FIXEDFLOAT delta_srcx = FIXED_MULT(param->mat11, param->kx);
            const FIXEDFLOAT delta_srcy = FIXED_MULT(param->mat21, param->ky);
            unsigned int x;
            for (x = x0; x < xlim;
                 x++, srcx_f += delta_srcx, srcy_f += delta_srcy
            ) {
                uint32_t *dest = dest_base + (x/4)*32 + x%4;
                int srcx = FIXED_TOINT(srcx_f);
                int srcy = FIXED_TOINT(srcy_f);
                unsigned int pixelnum;
                CALC_PIXELNUM(pagemap);
                *dest = (*get_pixel)(info, pixelnum);
            }

        } else {  // param->coefenab && param->deltaKAx != 0

            /* Multiple coefficients per row */
            const FIXEDFLOAT coef_dx = param->deltaKAx;
            FIXEDFLOAT coef_x = 0;
            int last_coef_x = -1;
            int empty_pixel = 0;
            unsigned int x;
            for (x = x0; x < xlim; x++, coef_x += coef_dx) {
                uint32_t *dest = dest_base + (x/4)*32 + x%4;
                if (FIXED_TOINT(coef_x) != last_coef_x) {
                    last_coef_x = FIXED_TOINT(coef_x);
                    const uint8_t *coef_ptr =
                        coef_baseptr + (last_coef_x << param->coefdatashift);
                    empty_pixel = !(*get_coef)(param, coef_ptr);
                }
                if (empty_pixel) {
                    *dest = 0;
                } else {
                    FIXEDFLOAT srcx_f, srcy_f;
                    transform_coordinates(param, x, y, &srcx_f, &srcy_f);
                    int srcx = FIXED_TOINT(srcx_f);
                    int srcy = FIXED_TOINT(srcy_f);
                    unsigned int pixelnum;
                    CALC_PIXELNUM(pagemap);
                    *dest = (*get_pixel)(info, pixelnum);
                }
            }

        }  // if (!param->coefenab)

    }  // for (y = y0; y < ylim; y++, coef_y += coef_dy)
}

/*-----------------------------------------------------------------------*/

/**
 * render_mode1:  Render a rotated/distorted graphics layer in mode 1
 * (parameter set selected by top bit of coefficient).
 *
 * [Parameters]
 *     pixelbuf: Pointer to output pixel buffer
 *         info: Graphics layer data
 *         clip: Clipping window data
 *    param_set: Array of rotation parameter sets
 * [Return value]
 *     None
 */
static void render_mode1(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2])
{
    /* Set up a second vdp2draw_struct for the second parameter set. */

    vdp2draw_struct *info0 = info;
    vdp2draw_struct info1_buf = *info;
    vdp2draw_struct *info1 = &info1_buf;
    info1->charaddr = ((Vdp2Regs->MPOFR >> 4) & 7) << 17;
    info1->planew_bits = (Vdp2Regs->PLSZ >> 12) & 1;
    info1->planeh_bits = (Vdp2Regs->PLSZ >> 13) & 1;
    info1->planew = 1 << info->planew_bits;
    info1->planeh = 1 << info->planeh_bits;
    if (info1->planew != info0->planew || info1->planeh != info0->planeh) {
        DMSG("WARNING: mixed plane sizes not supported for RPMD=2"
             " (set A: %dx%d, set B: %dx%d)", info0->planew, info0->planeh,
             info1->planew, info1->planeh);
    }

    /* If set A has one coefficient per row, it might simply be splitting
     * the screen into two differently-rendered regions (such as sky and
     * ground).  We can potentially draw that more efficiently as mode 0. */

    int top_set, switch_y;
    if (mode1_is_split_screen(param_set, &top_set, &switch_y)) {
        if (switch_y < 0) {
            return render_mode0(pixelbuf, info, clip, param_set, top_set);
        } else {
            render_mode0_region(pixelbuf, top_set==0 ? info0 : info1,
                                param_set, top_set,
                                0, 0, disp_width, switch_y);
            return render_mode0_region(pixelbuf, top_set==0 ? info1 : info0,
                                       param_set, top_set ^ 1,
                                       0, switch_y, disp_width, disp_height);
        }
    }

    /* Precalculate tilemap/bitmap coordinate masks, shift counts, and
     * tile page addresses. */

    INIT_CALC_PIXELNUM;
    uint32_t pagemap[2][8][8];
    if (!info->isbitmap) {
        INIT_PAGEMAP(pagemap[0], 0);
        INIT_PAGEMAP(pagemap[1], 1);
    }

    /* Choose appropriate coefficient-read and pixel-read functions. */

    int (*get_coef0)(RotationParams *, const void *) =
        get_coef_table[param_set[0].coefdatasize==4][param_set[0].coefmode];
    int (*get_coef1)(RotationParams *, const void *) =
        get_coef_table[param_set[1].coefdatasize==4][param_set[1].coefmode];
    SELECT_GET_PIXEL;

    /* Actually render the graphics layer. */

    /* These are intentionally float rather than FIXEDFLOAT, as in
     * render_mode0_region(). */
    float coef0_dy = FIXED_TOFLOAT(param_set[0].deltaKAst);
    float coef0_y = 0;
    float coef1_dy = FIXED_TOFLOAT(param_set[1].deltaKAst);
    float coef1_y = 0;
    unsigned int y;

    for (y = 0; y < disp_height;
         y++, coef0_y += coef0_dy, coef1_y += coef1_dy
    ) {

        const uint32_t coef0_base = param_set[0].coeftbladdr
            + (ifloorf(coef0_y) << param_set[0].coefdatashift);
        const uint8_t *coef0_baseptr = Vdp2Ram + (coef0_base & 0x7FFFF);
        const uint32_t coef1_base = param_set[1].coeftbladdr
            + (ifloorf(coef1_y) << param_set[1].coefdatashift);
        const uint8_t *coef1_baseptr = Vdp2Ram + (coef1_base & 0x7FFFF);
        uint32_t * const dest_base = pixelbuf + (y/8)*(disp_width*8) + (y%8)*4;

        if (param_set[0].deltaKAx == 0
         && (!param_set[1].coefenab || param_set[1].deltaKAx == 0)
        ) {

            /* One coefficient for the whole row in both sets */
            int which;
            if ((*get_coef0)(&param_set[0], coef0_baseptr)) {
                which = 0;
                info = info0;
            } else if (!param_set[1].coefenab
                       || (*get_coef1)(&param_set[1], coef1_baseptr)) {
                which = 1;
                info = info1;
            } else {
                /* Empty row */
                uint32_t *dest = dest_base;
                unsigned int x;
                for (x = 0; x < disp_width; x += 4, dest += 32) {
                    dest[0] = dest[1] = dest[2] = dest[3] = 0;
                }
                continue;
            }
            RotationParams *param = &param_set[which];
            FIXEDFLOAT srcx_f, srcy_f;
            transform_coordinates(param, 0, y, &srcx_f, &srcy_f);
            const FIXEDFLOAT delta_srcx = FIXED_MULT(param->mat11, param->kx);
            const FIXEDFLOAT delta_srcy = FIXED_MULT(param->mat21, param->ky);
            unsigned int x;
            for (x = 0; x < disp_width;
                 x++, srcx_f += delta_srcx, srcy_f += delta_srcy
            ) {
                uint32_t *dest = dest_base + (x/4)*32 + x%4;
                int srcx = FIXED_TOINT(srcx_f);
                int srcy = FIXED_TOINT(srcy_f);
                unsigned int pixelnum;
                CALC_PIXELNUM(pagemap[which]);
                *dest = (*get_pixel)(info, pixelnum);
            }

        } else {

            /* Multiple coefficients per row in one or both sets */
            const FIXEDFLOAT coef0_dx = param_set[0].deltaKAx;
            FIXEDFLOAT coef0_x = 0;
            const FIXEDFLOAT coef1_dx = param_set[1].deltaKAx;
            FIXEDFLOAT coef1_x = 0;
            int last_coef0_x = -1;
            int last_coef1_x = (param_set[1].coefenab ? -1 : 0);
            int have_coef0 = 1;
            int have_coef1 = 1;
            unsigned int x;
            for (x = 0; x < disp_width;
                 x++, coef0_x += coef0_dx, coef1_x += coef1_dx
            ) {
                uint32_t *dest = dest_base + (x/4)*32 + x%4;
                if (FIXED_TOINT(coef0_x) != last_coef0_x) {
                    last_coef0_x = FIXED_TOINT(coef0_x);
                    const uint8_t *coef0_ptr = coef0_baseptr
                        + (last_coef0_x << param_set[0].coefdatashift);
                    have_coef0 = (*get_coef0)(&param_set[0], coef0_ptr);
                }
                if (FIXED_TOINT(coef1_x) != last_coef1_x) {
                    last_coef1_x = FIXED_TOINT(coef1_x);
                    const uint8_t *coef1_ptr = coef1_baseptr
                        + (last_coef1_x << param_set[1].coefdatashift);
                    have_coef1 = (*get_coef1)(&param_set[1], coef1_ptr);
                }
                int which;
                if (have_coef0) {
                    which = 0;
                    info = info0;
                } else if (have_coef1) {
                    which = 1;
                    info = info1;
                } else {  // Empty pixel
                    *dest = 0;
                    continue;
                }
                RotationParams *param = &param_set[which];
                FIXEDFLOAT srcx_f, srcy_f;
                transform_coordinates(param, x, y, &srcx_f, &srcy_f);
                int srcx = FIXED_TOINT(srcx_f);
                int srcy = FIXED_TOINT(srcy_f);
                unsigned int pixelnum;
                CALC_PIXELNUM(pagemap[which]);
                *dest = (*get_pixel)(info, pixelnum);
            }

        }

    }  // for (y = 0; y < disp_height; y++, coef_y += coef_dy)
}

/*----------------------------------*/

/**
 * mode1_is_split_screen:  Return whether the parameters and coefficients
 * for a mode 1 rotated/distorted graphics layer have the effect of
 * splitting the screen into a top and bottom region, each with one of the
 * two parameter sets.  Parameters which specify a single parameter set for
 * the entire screen are considered to be a split screen with an empty
 * bottom portion for the purposes of this function.
 *
 * If this function returns true, *top_set_ret will be set to the index of
 * the parameter set used for the top line of the screen, and *switch_y_ret
 * will be set to the first line at which the other parameter set is used.
 * If the entire screen uses a single parameter set (effectively mode 0),
 * *switch_y_ret will be set to -1.
 *
 * [Parameters]
 *        param_set: Array of rotation parameter sets
 *      top_set_ret: Pointer to variable to receive the index of the
 *                      parameter set used on screen line 0
 *     switch_y_ret: Pointer to variable to receive the first line at which
 *                      the alternate parameter set is used
 * [Return value]
 *     True (nonzero) if the rotation parameters have the effect of
 *     splitting the screen, else false (zero)
 */
static int mode1_is_split_screen(const RotationParams param_set[2],
                                 int *top_set_ret, int *switch_y_ret)
{
    /* If parameter set A doesn't have coefficients enabled, set B can
     * never be selected, so this is just the same as mode 0 with set A. */

    if (!param_set[0].coefenab) {
        *top_set_ret = 0;
        *switch_y_ret = -1;
        return 1;
    }

    /* If there is more than one coefficient per line, assume that the
     * rotation operation is more complex than a simple split-screen effect. */

    if (param_set[0].deltaKAx != 0) {
        return 0;
    }

    /* Scan over the set A coefficients (now known to be one per line) and
     * see how many times the selected set changes. */

    int cur_set = -1;
    *top_set_ret = -1;
    *switch_y_ret = -1;

    /* These are intentionally float rather than FIXEDFLOAT, as in
     * render_mode0_region(). */
    float coef0_dy = FIXED_TOFLOAT(param_set[0].deltaKAst);
    float coef0_y = 0;
    int y;

    for (y = 0; y < disp_height; y++, coef0_y += coef0_dy) {

        const uint32_t coef0_addr = param_set[0].coeftbladdr
            + (ifloorf(coef0_y) << param_set[0].coefdatashift);
        const uint8_t * const coef0_ptr = Vdp2Ram + (coef0_addr & 0x7FFFF);
        /* VDP memory is organized by bytes, so coef0_ptr is now pointing
         * to the top byte of the coefficient value regardless of the data
         * size setting. */
        const int set = (*(int8_t *)coef0_ptr < 0) ? 1 : 0;

        if (y == 0) {
            *top_set_ret = cur_set = set;
        } else if (set != cur_set) {
            if (*switch_y_ret < 0) {
                *switch_y_ret = y;
            } else {
                /* This is the second change we've seen, so it's not a
                 * split-screen effect. */
printf("set change 2 at %d\n",y);
                return 0;
            }
            cur_set = set;
        }

    }

    /* If we got this far, it must be a split-screen effect. */

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * render_mode2:  Render a rotated/distorted graphics layer in mode 2
 * (parameter set selected by clipping window).
 *
 * [Parameters]
 *      pixelbuf: Pointer to output pixel buffer
 *          info: Graphics layer data
 *          clip: Clipping window data
 *     param_set: Array of rotation parameter sets
 * [Return value]
 *     None
 */
static void render_mode2(uint32_t *pixelbuf, vdp2draw_struct *info,
                         const clipping_struct *clip,
                         RotationParams param_set[2])
{
    /*
     * FIXME: This logic gets the Panzer Dragoon Saga name entry screen's
     * background to display properly, but since PDS is the only example
     * of this mode to which I have access, I have no clue whether this is
     * the Right Thing To Do.  I'm using the height of the clip window (set
     * as <0,0>-<352,224> by the game) based on vidsoft.c comments, but it
     * could also be just a top-half/bottom-half-of-screen thing.
     */

    render_mode0_region(pixelbuf, info, param_set, 0,
                        0, 0, disp_width, clip[0].yend / 2);

    info->charaddr = ((Vdp2Regs->MPOFR >> 4) & 7) << 17;
    info->planew_bits = (Vdp2Regs->PLSZ >> 12) & 1;
    info->planeh_bits = (Vdp2Regs->PLSZ >> 13) & 1;
    info->planew = 1 << info->planew_bits;
    info->planeh = 1 << info->planeh_bits;
    render_mode0_region(pixelbuf, info, param_set, 1,
                        0, clip[0].yend / 2, disp_width, disp_height);
}

/*************************************************************************/

/**
 * get_rotation_parameters:  Retrieve the rotation parameters for a rotated
 * graphics layer and perform necessary precalculations.
 *
 * [Parameters]
 *          info: Graphics layer data
 *         which: Which parameter set to retrieve (0 or 1)
 *     param_ret: Pointer to structure to receive parsed parameters
 * [Return value]
 *     None
 */
static void get_rotation_parameters(vdp2draw_struct *info, int which,
                                    RotationParams *param_ret)
{
    uint32_t addr = (Vdp2Regs->RPTA.all << 1) & 0x7FF7C;
    unsigned int KTCTL, KTAOF;
    if (which == 0) {
        KTCTL = Vdp2Regs->KTCTL & 0xFF;
        KTAOF = Vdp2Regs->KTAOF & 0xFF;
        param_ret->screenover = (Vdp2Regs->PLSZ >> 10) & 3;
    } else {
        addr |= 0x80;
        KTCTL = (Vdp2Regs->KTCTL >> 8) & 0xFF;
        KTAOF = (Vdp2Regs->KTAOF >> 8) & 0xFF;
        param_ret->screenover = (Vdp2Regs->PLSZ >> 14) & 3;
    }
    param_ret->coefenab = KTCTL & 1;
    param_ret->coefdatashift = (KTCTL & 2) ? 1 : 2;
    param_ret->coefdatasize = 1 << param_ret->coefdatashift;
    param_ret->coefmode = (KTCTL >> 2) & 3;
    param_ret->coeftbladdr = ((KTAOF & 7) << param_ret->coefdatashift) << 16;

#ifdef USE_FIXED_POINT
    #define GET_SHORT(nbits) \
        (addr += 2, (int32_t)((int16_t)T1ReadWord(Vdp2Ram,addr-2) \
                              << (16-nbits)) << nbits)
    #define GET_SIGNED_FLOAT(nbits) \
        (addr += 4, ((((int32_t)T1ReadLong(Vdp2Ram,addr-4) \
                      << (32-nbits)) >> (32-nbits)) & ~0x3F))
    #define GET_UNSIGNED_FLOAT(nbits) \
        (addr += 4, (((uint32_t)T1ReadLong(Vdp2Ram,addr-4) \
                      & (0xFFFFFFFFU >> (32-nbits))) & ~0x3F))
#else  // !USE_FIXED_POINT
    #define GET_SHORT(nbits) \
        (addr += 2, ((int16_t)T1ReadWord(Vdp2Ram,addr-2) \
                      << (16-nbits)) >> (16-nbits))
    #define GET_SIGNED_FLOAT(nbits) \
        (addr += 4, ((((int32_t)T1ReadLong(Vdp2Ram,addr-4) \
                      << (32-nbits)) >> (32-nbits)) & ~0x3F) / 65536.0f)
    #define GET_UNSIGNED_FLOAT(nbits) \
        (addr += 4, (((uint32_t)T1ReadLong(Vdp2Ram,addr-4) \
                      & (0xFFFFFFFFU >> (32-nbits))) & ~0x3F) / 65536.0f)
#endif  // !USE_FIXED_POINT

    param_ret->Xst      = GET_SIGNED_FLOAT(29);
    param_ret->Yst      = GET_SIGNED_FLOAT(29);
    param_ret->Zst      = GET_SIGNED_FLOAT(29);
    param_ret->deltaXst = GET_SIGNED_FLOAT(19);
    param_ret->deltaYst = GET_SIGNED_FLOAT(19);
    param_ret->deltaX   = GET_SIGNED_FLOAT(19);
    param_ret->deltaY   = GET_SIGNED_FLOAT(19);
    param_ret->A        = GET_SIGNED_FLOAT(20);
    param_ret->B        = GET_SIGNED_FLOAT(20);
    param_ret->C        = GET_SIGNED_FLOAT(20);
    param_ret->D        = GET_SIGNED_FLOAT(20);
    param_ret->E        = GET_SIGNED_FLOAT(20);
    param_ret->F        = GET_SIGNED_FLOAT(20);
    param_ret->Px       = GET_SHORT(14);
    param_ret->Py       = GET_SHORT(14);
    param_ret->Pz       = GET_SHORT(14);
    addr += 2;
    param_ret->Cx       = GET_SHORT(14);
    param_ret->Cy       = GET_SHORT(14);
    param_ret->Cz       = GET_SHORT(14);
    addr += 2;
    param_ret->Mx       = GET_SIGNED_FLOAT(30);
    param_ret->My       = GET_SIGNED_FLOAT(30);
    param_ret->kx       = GET_SIGNED_FLOAT(24);
    param_ret->ky       = GET_SIGNED_FLOAT(24);
    if (param_ret->coefenab) {
        param_ret->KAst      = GET_UNSIGNED_FLOAT(32);
        param_ret->deltaKAst = GET_SIGNED_FLOAT(26);
        param_ret->deltaKAx  = GET_SIGNED_FLOAT(26);
        param_ret->coeftbladdr +=
            FIXED_TOINT(param_ret->KAst) * param_ret->coefdatasize;
    } else {
        param_ret->KAst      = 0;
        param_ret->deltaKAst = 0;
        param_ret->deltaKAx  = 0;
    }

    #undef GET_SHORT
    #undef GET_SIGNED_FLOAT
    #undef GET_UNSIGNED_FLOAT

    /*
     * The coordinate transformation performed for rotated graphics layers
     * works out to the following:
     *
     *     [srcX]   (  [screenX])   [kx]   [Xp]
     *     [srcY] = (M [screenY]) * [ky] + [Yp]
     *              (  [   1   ])
     *
     * where the "*" operator is multiplication by components (not matrix
     * multiplication), M is the 2x3 constant matrix product:
     *
     *         [A  B  C] [deltaX  deltaXst  (Xst - Px)]
     *     M = [D  E  F] [deltaY  deltaYst  (Yst - Py)]
     *                   [  0        0      (Zst - Pz)]
     *
     * and <Xp,Yp> is a constant vector computed as:
     *
     *     [Xp]   ([A  B  C] [Px - Cx])   [Cx]   [Mx]
     *     [Yp] = ([D  E  F] [Py - Cy]) + [Cy] + [My]
     *            (          [Pz - Cz])
     */

    param_ret->mat11 = FIXED_MULT(param_ret->A, param_ret->deltaX)
                     + FIXED_MULT(param_ret->B, param_ret->deltaY);
    param_ret->mat12 = FIXED_MULT(param_ret->A, param_ret->deltaXst)
                     + FIXED_MULT(param_ret->B, param_ret->deltaYst);
    param_ret->mat13 = FIXED_MULT(param_ret->A, (param_ret->Xst - param_ret->Px))
                     + FIXED_MULT(param_ret->B, (param_ret->Yst - param_ret->Py))
                     + FIXED_MULT(param_ret->C, (param_ret->Zst - param_ret->Pz));
    param_ret->mat21 = FIXED_MULT(param_ret->D, param_ret->deltaX)
                     + FIXED_MULT(param_ret->E, param_ret->deltaY);
    param_ret->mat22 = FIXED_MULT(param_ret->D, param_ret->deltaXst)
                     + FIXED_MULT(param_ret->E, param_ret->deltaYst);
    param_ret->mat23 = FIXED_MULT(param_ret->D, (param_ret->Xst - param_ret->Px))
                     + FIXED_MULT(param_ret->E, (param_ret->Yst - param_ret->Py))
                     + FIXED_MULT(param_ret->F, (param_ret->Zst - param_ret->Pz));
    param_ret->Xp    = FIXED_MULT(param_ret->A, (param_ret->Px - param_ret->Cx))
                     + FIXED_MULT(param_ret->B, (param_ret->Py - param_ret->Cy))
                     + FIXED_MULT(param_ret->C, (param_ret->Pz - param_ret->Cz))
                     + param_ret->Cx
                     + param_ret->Mx;
    param_ret->Yp    = FIXED_MULT(param_ret->D, (param_ret->Px - param_ret->Cx))
                     + FIXED_MULT(param_ret->E, (param_ret->Py - param_ret->Cy))
                     + FIXED_MULT(param_ret->F, (param_ret->Pz - param_ret->Cz))
                     + param_ret->Cy
                     + param_ret->My;
}

/*-----------------------------------------------------------------------*/

/**
 * get_rotation_coefficient:  Retrieve a single rotation coefficient for a
 * rotated graphics layer and update the rotation parameter set accordingly.
 *
 * This function is only to demonstrate the behavior of coefficient
 * processing, and is not actually called; the per-mode-and-size optimized
 * versions below are used instead.
 *
 * [Parameters]
 *       param: Rotation parameter set
 *     address: Address in VDP2 RAM of coefficient
 * [Return value]
 *     Zero if this pixel is blank, else nonzero
 */
__attribute__((unused))
static int get_rotation_coefficient(RotationParams *param, uint32_t address)
{
    FIXEDFLOAT value;
    if (param->coefdatasize == 2) {
        const int16_t data = T1ReadWord(Vdp2Ram, address & 0x7FFFE);
        if (data < 0) {
            return 0;
        }
#ifdef USE_FIXED_POINT
        value = (int32_t)(data << 1) << 5;
#else
        value = (float)((data << 1) >> 1) / 1024.0f;
#endif
    } else {
        const int32_t data = T1ReadLong(Vdp2Ram, address & 0x7FFFC);
        if (data < 0) {
            return 0;
        }
#ifdef USE_FIXED_POINT
        value = (data << 8) >> 8;
#else
        value = (float)((data << 8) >> 8) / 65536.0f;
#endif
    }
    switch (param->coefmode) {
      case 0: param->kx = param->ky = value; break;
      case 1: param->kx =             value; break;
      case 2:             param->ky = value; break;
      case 3:
#ifdef USE_FIXED_POINT
              param->Xp = value << 8;
#else
              param->Xp = value * 256.0f;
#endif
                                             break;
    }
    return 1;
}

/*----------------------------------*/

/**
 * get_rotation_coefficient_sizeX_modeY:  Retrieve a single rotation
 * coefficient for a rotated graphics layer and update the rotation
 * parameter set accordingly.  Each routine is optimized for the specific
 * case of param->coefdatasize == X and param->coefmode == Y.
 *
 * [Parameters]
 *       param: Rotation parameter set
 *     address: Native address of coefficient
 * [Return value]
 *     Zero if this pixel is blank, else nonzero
 */

static int get_rotation_coefficient_size2_mode0(RotationParams *param,
                                                const void *address)
{
    const int16_t data = BSWAP16(*(const int16_t *)address);
    if (data < 0) {
        return 0;
    }
    param->kx = param->ky =
#ifdef USE_FIXED_POINT
        (int32_t)(data << 1) << 5;
#else
        (float)(data << 1) / 2048.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size2_mode1(RotationParams *param,
                                                const void *address)
{
    const int16_t data = BSWAP16(*(const int16_t *)address);
    if (data < 0) {
        return 0;
    }
    param->kx =
#ifdef USE_FIXED_POINT
        (int32_t)(data << 1) << 5;
#else
        (float)(data << 1) / 2048.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size2_mode2(RotationParams *param,
                                                const void *address)
{
    const int16_t data = BSWAP16(*(const int16_t *)address);
    if (data < 0) {
        return 0;
    }
    param->ky =
#ifdef USE_FIXED_POINT
        (int32_t)(data << 1) << 5;
#else
        (float)(data << 1) / 2048.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size2_mode3(RotationParams *param,
                                                const void *address)
{
    const int16_t data = BSWAP16(*(const int16_t *)address);
    if (data < 0) {
        return 0;
    }
    param->Xp =
#ifdef USE_FIXED_POINT
        (int32_t)(data << 1) << 13;
#else
        (float)(data << 1) / 8.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size4_mode0(RotationParams *param,
                                                const void *address)
{
    const int32_t data = BSWAP32(*(const int32_t *)address);
    if (data < 0) {
        return 0;
    }
    param->kx = param->ky =
#ifdef USE_FIXED_POINT
        (data << 8) >> 8;
#else
        (float)(data << 8) / 16777216.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size4_mode1(RotationParams *param,
                                                const void *address)
{
    const int32_t data = BSWAP32(*(const int32_t *)address);
    if (data < 0) {
        return 0;
    }
    param->kx =
#ifdef USE_FIXED_POINT
        (data << 8) >> 8;
#else
        (float)(data << 8) / 16777216.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size4_mode2(RotationParams *param,
                                                const void *address)
{
    const int32_t data = BSWAP32(*(const int32_t *)address);
    if (data < 0) {
        return 0;
    }
    param->ky =
#ifdef USE_FIXED_POINT
        (data << 8) >> 8;
#else
        (float)(data << 8) / 16777216.0f;
#endif
    return 1;
}

static int get_rotation_coefficient_size4_mode3(RotationParams *param,
                                                const void *address)
{
    const int32_t data = BSWAP32(*(const int32_t *)address);
    if (data < 0) {
        return 0;
    }
    param->Xp =
#ifdef USE_FIXED_POINT
        data << 8;
#else
        (float)(data << 8) / 65536.0f;
#endif
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * transform_coordinates:  Transform screen coordinates to source
 * coordinates based on the current rotation parameters.
 *
 * [Parameters]
 *                  param: Rotation parameter set
 *                   x, y: Screen coordinates
 *     srcx_ret, srcy_ret: Pointers to variables to receive source coordinates
 * [Return value]
 *     None
 */
static void transform_coordinates(const RotationParams *param, int x, int y,
                                  FIXEDFLOAT *srcx_ret, FIXEDFLOAT *srcy_ret)
{
    *srcx_ret = FIXED_MULT(param->mat11*x + param->mat12*y + param->mat13,
                           param->kx) + param->Xp;
    *srcy_ret = FIXED_MULT(param->mat21*x + param->mat22*y + param->mat23,
                           param->ky) + param->Yp;
}

/*************************************************************************/

/**
 * rotation_get_pixel_*:  Retrieve a pixel from tiles or bitmaps of various
 * pixel formats, with transparency disabled.  info->charaddr is assumed to
 * contain the offset of the tile or bitmap in VDP2 RAM.
 *
 * [Parameters]
 *         info: Graphics layer data
 *     pixelnum: Index of pixel to retrieve (y*w+x)
 * [Return value]
 *     Pixel value as 0xAABBGGRR
 */

/*----------------------------------*/

static uint32_t rotation_get_pixel_t4(vdp2draw_struct *info,
                                      unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum/2;
    /* For speed, we assume the tile/bitmap won't wrap around the end of
     * VDP2 RAM */
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t byte = *ptr;
    const uint8_t pixel = (pixelnum & 1) ? byte & 0x0F : byte >> 4;
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t8(vdp2draw_struct *info,
                                      unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum;
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t pixel = *ptr;
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t16(vdp2draw_struct *info,
                                       unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    const unsigned int colornum = info->coloroffset + pixel;
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_16(vdp2draw_struct *info,
                                      unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    return 0xFF000000
           | (pixel & 0x7C00) << 9
           | (pixel & 0x03E0) << 6
           | (pixel & 0x001F) << 3;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_32(vdp2draw_struct *info,
                                      unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*4;
    const uint32_t *ptr = (const uint32_t *)(Vdp2Ram + address);
    const uint32_t pixel = BSWAP32(*ptr);
    return 0xFF000000 | pixel;
}

/*-----------------------------------------------------------------------*/

/**
 * rotation_get_pixel_*_transparent:  Retrieve a pixel from tiles or
 * bitmaps of various pixel formats, with transparency enabled.
 * info->charaddr is assumed to contain the offset of the tile or bitmap in
 * VDP2 RAM.
 *
 * [Parameters]
 *         info: Graphics layer data
 *     pixelnum: Index of pixel to retrieve (y*w+x)
 * [Return value]
 *     Pixel value as 0xAABBGGRR
 */

/*----------------------------------*/

static uint32_t rotation_get_pixel_t4_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum/2;
    /* For speed, we assume the tile/bitmap won't wrap around the end of
     * VDP2 RAM */
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t byte = *ptr;
    const uint8_t pixel = (pixelnum & 1) ? byte & 0x0F : byte >> 4;
    if (!pixel) {
        return 0x00000000;
    }
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t8_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum;
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t pixel = *ptr;
    if (!pixel) {
        return 0x00000000;
    }
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t16_transparent(vdp2draw_struct *info,
                                                   unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    if (!pixel) {
        return 0x00000000;
    }
    const unsigned int colornum = info->coloroffset + pixel;
    return global_clut_32[colornum & 0x7FF];
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_16_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    if (!(pixel & 0x8000)) {
        return 0x00000000;
    }
    return 0xFF000000
           | (pixel & 0x7C00) << 9
           | (pixel & 0x03E0) << 6
           | (pixel & 0x001F) << 3;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_32_transparent(vdp2draw_struct *info,
                                                  unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*4;
    const uint32_t *ptr = (const uint32_t *)(Vdp2Ram + address);
    const uint32_t pixel = BSWAP32(*ptr);
    if (!(pixel & 0x80000000)) {
        return 0x00000000;
    }
    return 0xFF000000 | pixel;
}

/*-----------------------------------------------------------------------*/

/**
 * rotation_get_pixel_*_adjust:  Retrieve a pixel from tiles or bitmaps of
 * various pixel formats, and apply alpha and color adjustments.
 * info->charaddr is assumed to contain the offset of the tile or bitmap in
 * VDP2 RAM.
 *
 * [Parameters]
 *         info: Graphics layer data
 *     pixelnum: Index of pixel to retrieve (y*w+x)
 * [Return value]
 *     Pixel value as 0xAABBGGRR
 */

/*----------------------------------*/

static uint32_t rotation_get_pixel_t4_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum/2;
    /* For speed, we assume the tile/bitmap won't wrap around the end of
     * VDP2 RAM */
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t byte = *ptr;
    const uint8_t pixel = (pixelnum & 1) ? byte & 0x0F : byte >> 4;
    if (!pixel && info->transparencyenable) {
        return 0x00000000;
    }
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return (adjust_color_32_32(global_clut_32[colornum & 0x7FF],
                               info->cor, info->cog, info->cob) & 0xFFFFFF)
           | info->alpha << 24;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t8_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum;
    const uint8_t *ptr = (const uint8_t *)(Vdp2Ram + address);
    const uint8_t pixel = *ptr;
    if (!pixel && info->transparencyenable) {
        return 0x00000000;
    }
    const unsigned int colornum =
        info->coloroffset + (info->paladdr<<4 | pixel);
    return (adjust_color_32_32(global_clut_32[colornum & 0x7FF],
                               info->cor, info->cog, info->cob) & 0xFFFFFF)
           | info->alpha << 24;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_t16_adjust(vdp2draw_struct *info,
                                              unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    if (!pixel && info->transparencyenable) {
        return 0x00000000;
    }
    const unsigned int colornum = info->coloroffset + pixel;
    return (adjust_color_32_32(global_clut_32[colornum & 0x7FF],
                               info->cor, info->cog, info->cob) & 0xFFFFFF)
           | info->alpha << 24;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_16_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*2;
    const uint16_t *ptr = (const uint16_t *)(Vdp2Ram + address);
    const uint16_t pixel = BSWAP16(*ptr);
    if (!(pixel & 0x8000) && info->transparencyenable) {
        return 0x00000000;
    }
    return (adjust_color_16_32(pixel,
                               info->cor, info->cog, info->cob) & 0xFFFFFF)
           | info->alpha << 24;
}

/*----------------------------------*/

static uint32_t rotation_get_pixel_32_adjust(vdp2draw_struct *info,
                                             unsigned int pixelnum)
{
    const uint32_t address = info->charaddr + pixelnum*4;
    const uint32_t *ptr = (const uint32_t *)(Vdp2Ram + address);
    const uint32_t pixel = BSWAP32(*ptr);
    if (!(pixel & 0x80000000) && info->transparencyenable) {
        return 0x00000000;
    }
    return (adjust_color_32_32(pixel,
                               info->cor, info->cog, info->cob) & 0xFFFFFF)
           | info->alpha << 24;
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
