/*  src/psp/psp-video-tweaks.c: Game-specific tweaks for PSP video module
    Copyright 2010 Andrew Church

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

#include "../memory.h"
#include "../vdp2.h"
#include "../vidshared.h"

#include "config.h"
#include "gu.h"
#include "psp-video.h"
#include "psp-video-internal.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Local routine declarations (internal helpers are declared in their
 * respective sections) */

static void Azel_fix_registers(void);
static int Azel_draw_NBG1(vdp2draw_struct *info,
                          const clipping_struct *clip);
static void Azel_reset_cache(void);
static void Azel_cache_RBG0(void);
static int Azel_draw_RBG0(vdp2draw_struct *info,
                          const clipping_struct *clip);

/*************************************************************************/
/************************** Interface function ***************************/
/*************************************************************************/

/**
 * psp_video_apply_tweaks:  Apply game-specific optimizations and tweaks
 * for faster/better PSP video output.  Called at the beginning of drawing
 * each frame.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void psp_video_apply_tweaks(void)
{
    /**** Azel: Panzer Dragoon RPG (JP) ****/

    if (memcmp(HighWram+0x42F34, "APDNAR\0003", 8) == 0) {

        /* For reasons unknown (but possibly an emulator bug), VDP1 and
         * RBG0 are one frame out of sync, with RBG0 lagging behind.
         * However, if the game is in 30fps mode and we're also skipping
         * one frame (or in fact any number of odd frames) per frame drawn,
         * we can hide this lag by always skipping the frame in which the
         * VDP1 command table is updated.  The game finalizes the command
         * list by updating the jump target at VDP1 address 0x00082, so we
         * watch for changes and use that to sync the frame skipper. */
        static uint8_t just_adjusted = 0; // Avoid infinite loops, just in case
        static uint16_t last_00082 = 0xFFFF;  // Impossible value
        const uint16_t this_00082 = T1ReadWord(Vdp1Ram, 0x00082);
        const int game_framerate = T2ReadLong(HighWram, 0x4BC94);
        if (!just_adjusted
         && game_framerate == 2
         && this_00082 != last_00082
         && frames_to_skip % 2 == 1
         && frames_skipped % 2 == 1
        ) {
            frames_skipped--;
            just_adjusted = 1;
        } else {
            just_adjusted = 0;
        }
        last_00082 = this_00082;

        /* Fix bogus/suboptimal register settings. */
        Azel_fix_registers();

        /* Apply the top/bottom black border to NBG1 implemented using
         * line scrolling. */
        psp_video_set_draw_routine(BG_NBG1, Azel_draw_NBG1, 0);

        /* Draw sky/ground RBG0 graphics more efficiently, if requested. */
        if (config_get_optimize_rotate() && (Vdp2Regs->BGON & 0x0010)) {
            Azel_cache_RBG0();
            psp_video_set_draw_routine(BG_RBG0, Azel_draw_RBG0, 1);
        } else {
            Azel_reset_cache();
            psp_video_set_draw_routine(BG_RBG0, NULL, 0);
        }

    }
}

/*************************************************************************/
/**************************** Local functions ****************************/
/*************************************************************************/

/**** Azel: Panzer Dragoon RPG (JP) optimizers ****/

/*-----------------------------------------------------------------------*/

/* Exported variables for RBG0 slope and first coefficient reciprocal,
 * set by the optimized RBG0 coefficient generator in satopt-sh2.c. */
#define SLOPE_UNSET  1e10f
float psp_video_tweaks_Azel_RBG0_slope = SLOPE_UNSET;
float psp_video_tweaks_Azel_RBG0_first_recip;

/*----------------------------------*/

/* Do we have data for RBG0 cached? */
static uint8_t Azel_RBG0_cached;

/* Palette indices (0-7) for sky (flat) and ground (scaled) planes. */
static uint8_t Azel_sky_palette, Azel_ground_palette;

/* Does the sky wrap vertically? */
static uint8_t Azel_sky_wrap_v;

/* Is the ground texture already reduced by half? */
static uint8_t Azel_ground_reduced;

/* VDP2 plane addresses for sky and ground planes. */
static uint32_t Azel_sky_plane_address, Azel_ground_plane_address;

/* Checksum for plane data. */
static uint32_t Azel_plane_data_checksum;

/* Pixel buffers for cached plane data. */
static uint8_t *Azel_sky_cache, *Azel_ground_cache;

/* Data structure used for coordinate calculation. */
struct Azel_RBG0_coord {int x, y, overdraw_x, overdraw_y;};

/*----------------------------------*/

static void Azel_cache_plane(uint32_t plane_address, uint32_t tile_base,
                             uint8_t *dest);
static void Azel_make_mipmap(const uint8_t *in, unsigned int size,
                             uint8_t *out, unsigned int stride);
static void Azel_get_rotation_matrix(uint32_t address, float matrix[2][3],
                                     float *kx_ret, float *ky_ret,
                                     float *Xp_ret, float *Yp_ret);
static void Azel_transform_coordinates(const float x, const float y,
                                       float *u_ret, float *v_ret,
                                       float M[2][3],
                                       const float kx, const float ky,
                                       const float Xp, const float Yp);
static inline void Azel_compute_switch(
    uint32_t coef_base, uint32_t coef_switch_index,
    const uint32_t coef_index_UL, const uint32_t coef_index_UR,
    const uint32_t coef_index_LL, const uint32_t coef_index_LR,
    float coef_dx, float coef_dy,
    int *switch_x0, int *switch_y0, int *switch_x1, int *switch_y1);
static inline void Azel_compute_vertices(
    int UL_is_sky, int UR_is_sky, int LL_is_sky, int LR_is_sky,
    int switch_x0, int switch_y0, int switch_x1, int switch_y1,
    struct Azel_RBG0_coord coord[2][5], unsigned int nverts[2]);

/*-----------------------------------------------------------------------*/

/**
 * Azel_fix_registers:  Fix VDP2 registers which are set improperly (but do
 * not exhibit problems on a real Saturn due to hardware idiosyncrasies) or
 * inefficiently (so that they waste more resources than necessary).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void Azel_fix_registers(void)
{
    /* Fix bogus sprite alpha setting in the Uru underwater tunnel. */
    if (Vdp2Regs->PNCN1 == 0xC100
     && Vdp2Regs->MPABN1 == 0x0B0B
     && Vdp2Regs->MPCDN1 == 0x0B0B
     && Vdp2Regs->RPTA.all == 0x14000
     && T1ReadLong(Vdp2Ram, 0x28054) == 0x80640000
     && T1ReadLong(Vdp2Ram, 0x28058) == 0x10000
     && (int32_t)T1ReadLong(Vdp2Ram, 0x20190 + 223*4) < 0
     && Vdp2Regs->SPCTL == 0x1523
     && Vdp2Regs->CCCTL == 0x0053
     && Vdp2Regs->SFCCMD == 0x0008
     && Vdp2Regs->PRISA == 0x0405
     && Vdp2Regs->PRINA == 0x0604
     && Vdp2Regs->CCRSA == 0x000C
     && Vdp2Regs->CCRNA == 0x101F
    ) {
        Vdp2Regs->CCRSA &= 0xFF00;
    }

    /* Fix bogus sprite alpha setting in the imperial base. */
    if (Vdp2Regs->BGON == 0x011B
     && Vdp2Regs->SFSEL == 0x0002
     && Vdp2Regs->CHCTLA == 0x0101
     && Vdp2Regs->CHCTLB == 0x1100
     && Vdp2Regs->PNCN0 == 0x8080
     && Vdp2Regs->PNCN1 == 0xC100
     && Vdp2Regs->PLSZ == 0x0000
     && Vdp2Regs->MPABN0 == 0x3E3E
     && Vdp2Regs->MPABN0 == 0x3E3E
     && Vdp2Regs->MPABN1 == 0x0B0B
     && Vdp2Regs->MPCDN1 == 0x0B0B
     && T1ReadLong(Vdp2Ram, 0x1F000) == 0x02010202
     && T1ReadLong(Vdp2Ram, 0x1008C) == 0x99889999
     && Vdp2Regs->SPCTL == 0x1423
     && (Vdp2Regs->CCCTL & ~0x0010) == 0x0143
     && Vdp2Regs->SFCCMD == 0x0008
     && Vdp2Regs->PRISA == 0x0404
     && (Vdp2Regs->PRINA & ~0x0001) == 0x0604
     && (Vdp2Regs->PRINA & 0xF) == (Vdp2Regs->CCCTL>>4 & 0xF)
     && Vdp2Regs->CCRSA == 0x180D
     && Vdp2Regs->CCRNA == 0x1017
    ) {
        Vdp2Regs->CCRSA &= 0xFF00;
    }

    /* Fix missing(?) alpha setting for the NBG0 cloud overlay used in
     * Mel-Kava and in Atolm battles.  (NBG0 is at the third-highest
     * priority level; how does it get color calculation enabled in the
     * first place?) */
    if ((Vdp2Regs->BGON & ~0x0100) == 0x001B
     && Vdp2Regs->SFSEL == 0x0002
     && Vdp2Regs->CHCTLA == 0x0101
     && Vdp2Regs->CHCTLB == 0x1100
     && (Vdp2Regs->PNCN0 & ~0x0020) == 0x8080
     && Vdp2Regs->PNCN1 == 0xC100
     && Vdp2Regs->PLSZ == 0x0000
     && Vdp2Regs->MPABN0 == 0x3C3C
     && Vdp2Regs->MPABN0 == 0x3C3C
     && Vdp2Regs->MPABN1 == 0x0B0B
     && Vdp2Regs->MPCDN1 == 0x0B0B
     && T1ReadLong(Vdp2Ram, 0x1E000) == 0x02010202
     && T1ReadLong(Vdp2Ram, 0x100F8) == 0x11111222
     && (Vdp2Regs->CCCTL & ~0x0010) == 0x0103
     && Vdp2Regs->SFCCMD == 0x0008
     && (Vdp2Regs->PRINA & ~0x0001) == 0x0604
     && (Vdp2Regs->PRINA & 0x1) == (Vdp2Regs->CCCTL>>4 & 0x1)
     && Vdp2Regs->CCRNA == 0x1000
    ) {
        Vdp2Regs->CCRNA |= 0x0017;
    }

    /* Display movies with transparency disabled to improve draw speed. */
    if ((Vdp2Regs->CHCTLA & 0x0070) == 0x0040) {
        Vdp2Regs->BGON |= 0x0100;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_draw_NBG1:  Draw NBG1, along with any black borders specified by
 * the line scroll table.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     Nonzero if the graphics layer was drawn, zero if not
 */
static int Azel_draw_NBG1(vdp2draw_struct *info,
                          const clipping_struct *clip)
{
    /* First check whether this is an optimizable case, and punt back to
     * the default if not. */

    if ((Vdp2Regs->SCRCTL & 0x3F00) != 0x400) {
        return 0;  // No black bars to draw
    }
    if (UNLIKELY(info->isbitmap || info->patternwh != 2)) {
        DMSG("Bad NBG1 parameters: isbitmap=%d patternwh=%d",
             info->isbitmap, info->patternwh);
        return 0;
    }

    /* Draw the screen itself as usual. */

    vdp2_draw_map_16x16(info, clip);

    /* Render black bars for lines scrolled off the screen (i.e., to black)
     * in the line scroll table. */

    const uint32_t address = (Vdp2Regs->LSTA1.all & 0x3FFFE) << 1;
    const uint8_t *table = &Vdp2Ram[address];

    guDisable(GU_TEXTURE_2D);

    int in_black_bar = 0;
    unsigned int black_bar_top = 0;
    unsigned int y;
    for (y = 0; y <= disp_height; y++, table += 4) {  // Deliberately "<="
        if (y < disp_height && *table != 0) {
            if (!in_black_bar) {
                black_bar_top = y;
                in_black_bar = 1;
            }
        } else {
            if (in_black_bar) {
                struct {uint32_t color; int16_t x, y, z, pad;} *vertices;
                vertices = guGetMemory(sizeof(*vertices) * 2);
                vertices[0].color = 0xFF000000;
                vertices[0].x = 0;
                vertices[0].y = black_bar_top >> disp_yscale;
                vertices[0].z = 0;
                vertices[1].color = 0xFF000000;
                vertices[1].x = disp_width >> disp_xscale;
                vertices[1].y = y >> disp_yscale;
                vertices[1].z = 0;
                guDrawArray(GU_SPRITES,
                            GU_TRANSFORM_2D | GU_COLOR_8888 | GU_VERTEX_16BIT,
                            2, NULL, vertices);
                in_black_bar = 0;
            }
        }
    }

    guEnable(GU_TEXTURE_2D);
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_reset_cache:  Clear all cached RBG0 data.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void Azel_reset_cache(void)
{
    Azel_RBG0_cached = 0;
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_cache_RBG0:  Check whether the current RBG0 data matches the cached
 * data, and cache it if not.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void Azel_cache_RBG0(void)
{
    /* Look up the graphics layer format and make sure it's what we're
     * looking for. */

    const unsigned int colornumber = Vdp2Regs->CHCTLB>>12 & 0x7;
    const unsigned int patternwh = Vdp2Regs->CHCTLB & 0x100 ? 2 : 1;
    const unsigned int patterndatasize = Vdp2Regs->PNCR & 0x8000 ? 2 : 4;
    const unsigned int auxmode = Vdp2Regs->PNCR & 0x4000 ? 1 : 0;
    const unsigned int supplementdata = Vdp2Regs->PNCR & 0x3FF;
    const unsigned int planewh_A = Vdp2Regs->PLSZ>>8 & 0x3;
    const unsigned int planewh_B = Vdp2Regs->PLSZ>>12 & 0x3;
    const unsigned int rpmd = Vdp2Regs->RPMD & 0x3;
    const unsigned int raovr = Vdp2Regs->PLSZ>>10 & 0x3;
    const unsigned int raopn = Vdp2Regs->OVPNRA;
    if (colornumber != 1 || patternwh != 2 || patterndatasize != 2
     || auxmode || planewh_A != 0 || planewh_B != 0 
     || (rpmd != 2 && !(rpmd == 0 && raovr == 1 && raopn == 0x5000))
    ) {
        DMSG("Wrong RBG0 format for cache: colornumber=%u patternwh=%u"
             " patterndatasize=%u auxmode=%u planewh=%u/%u rpmd=%u",
             colornumber, patternwh, patterndatasize, auxmode, planewh_A,
             planewh_B, rpmd);
        Azel_RBG0_cached = 0;
        return;
    }

    /* Calculate the plane data addresses for the sky and ground planes,
     * and check that they haven't changed from the cached values. */

    const unsigned int plane_size =
        (64/patternwh) * (64/patternwh) * patterndatasize;
    const unsigned int sky_plane =
        (Vdp2Regs->MPOFR>>4 & 7) << 6 | (Vdp2Regs->MPABRB & 0xFF);
    const unsigned int ground_plane =
        (Vdp2Regs->MPOFR>>0 & 7) << 6 | (Vdp2Regs->MPABRA & 0xFF);
    const unsigned int sky_plane_address = sky_plane * plane_size;
    const unsigned int ground_plane_address = ground_plane * plane_size;
    if (Azel_RBG0_cached
        && (sky_plane_address != Azel_sky_plane_address
            || ground_plane_address != Azel_ground_plane_address)
    ) {
        DMSG("Plane addresses changed, now sky=%05X ground=%05X",
             sky_plane_address, ground_plane_address);
        Azel_RBG0_cached = 0;
    }

    /* Calculate a checksum for the plane data and check it against the
     * cached checksum.  If the plane data matches, we assume the tile
     * (pixel) data matches as well. */

    uint16_t sum1 = 1, sum2 = 0;
    const uint16_t *ptr, *top;
    for (ptr = (const uint16_t *)&Vdp2Ram[sky_plane_address],
             top = ptr + plane_size/2; ptr < top; ptr++
    ) {
        const uint16_t data = BSWAP16(*ptr);
        sum1 += data;
        sum2 += sum1;
    }
    for (ptr = (const uint16_t *)&Vdp2Ram[ground_plane_address],
             top = ptr + plane_size/2; ptr < top; ptr++
    ) {
        const uint16_t data = BSWAP16(*ptr);
        sum1 += data;
        sum2 += sum1;
    }
    const uint32_t checksum = sum1 | sum2<<16;
    if (checksum != Azel_plane_data_checksum) {
        DMSG("Plane data checksum changed, now 0x%08X", checksum);
        Azel_RBG0_cached = 0;
    }

    /* If Azel_RBG0_cached is still set, the current RBG0 data already
     * matches the cache, so we don't have to do anything else. */

    if (Azel_RBG0_cached) {
        return;
    }

    /* Allocate a cache buffer if we haven't done so yet.  (We don't free
     * the buffers once we allocate them, on the assumption that only one
     * game will be played per Yabause boot.) */

    if (!Azel_sky_cache) {
        uint8_t *base = malloc(63 + 512*512
                                  + 512*512 + 256*256 + 128*128 + 64*64);
        if (!base) {
            DMSG("No memory for sky/ground pixel buffers");
            return;
        }
        uintptr_t base_aligned = ((uintptr_t)base + 63) & -64;
        Azel_sky_cache = (uint8_t *)base_aligned;
        Azel_ground_cache = (uint8_t *)(base_aligned + 512*512);
    }

    /* Cache the graphics data (two 512x512-pixel planes) in
     * Azel_{sky,ground}_cache as 512x512 T8 swizzled textures.  Also add
     * mipmaps for the ground texture to improve drawing performance for
     * distant regions. */

    Azel_cache_plane(sky_plane_address,
                     (supplementdata & 0xC) << 15 | (supplementdata & 3) << 5,
                     Azel_sky_cache);
    if ((Vdp2Regs->RPMD & 3) == 0
     && Vdp2Regs->MPABRA == 0x0100
     && Vdp2Regs->MPEFRA == 0x0203
    ) {
        /* Special case for the dome area of the Uru underground dungeon,
         * which is a shrunken 1024x1024 map.  We reduce it to 512x512 and
         * adjust the scale factors appropriately when drawing. */
        Azel_ground_reduced = 1;
        uint8_t *temp = malloc(512*512);
        if (!temp) {
            DMSG("No temporary memory for reducing dome RBG0");
            return;
        }
        Azel_cache_plane(ground_plane_address,
                         (supplementdata & 0xC)<<15 | (supplementdata & 3)<<5,
                         temp);
        Azel_make_mipmap(temp, 512, Azel_ground_cache, 512);
        Azel_cache_plane(ground_plane_address + 0x800,
                         (supplementdata & 0xC)<<15 | (supplementdata & 3)<<5,
                         temp);
        Azel_make_mipmap(temp, 512, Azel_ground_cache + 256*8, 512);
        Azel_cache_plane(ground_plane_address + 0x1800,
                         (supplementdata & 0xC)<<15 | (supplementdata & 3)<<5,
                         temp);
        Azel_make_mipmap(temp, 512, Azel_ground_cache + 512*256, 512);
        Azel_cache_plane(ground_plane_address + 0x1000,
                         (supplementdata & 0xC)<<15 | (supplementdata & 3)<<5,
                         temp);
        Azel_make_mipmap(temp, 512, Azel_ground_cache + 512*256 + 256*8, 512);
        free(temp);
    } else {
        Azel_ground_reduced = 0;
        Azel_cache_plane(ground_plane_address,
                         (supplementdata & 0xC)<<15 | (supplementdata & 3)<<5,
                         Azel_ground_cache);
    }
    Azel_make_mipmap(Azel_ground_cache, 512,
                     Azel_ground_cache + 512*512, 256);
    Azel_make_mipmap(Azel_ground_cache + 512*512, 256,
                     Azel_ground_cache + 512*512 + 256*256, 128);
    Azel_make_mipmap(Azel_ground_cache + 512*512 + 256*256, 128,
                     Azel_ground_cache + 512*512 + 256*256 + 128*128, 64);

    /* Record other data in relevant variables and set the cached flag. */

    Azel_sky_palette = Vdp2Ram[sky_plane_address] >> 4;
    Azel_ground_palette = Vdp2Ram[ground_plane_address] >> 4;
    Azel_sky_wrap_v = (Vdp2Regs->MPEFRB == Vdp2Regs->MPABRB);
    Azel_sky_plane_address = sky_plane_address;
    Azel_ground_plane_address = ground_plane_address;
    Azel_plane_data_checksum = checksum;
    Azel_RBG0_cached = 1;
    psp_video_tweaks_Azel_RBG0_slope = SLOPE_UNSET;
}

/*----------------------------------*/

/**
 * Azel_cache_plane:  Cache a single plane of graphics data as a 512x512
 * T8-format swizzled texture.
 *
 * [Parameters]
 *     plane_address: Address of plane data in VDP2 RAM
 *         tile_base: Base address of tile (pixel) data in VDP2 RAM
 *              dest: Pointer to output buffer (512*512 bytes)
 * [Return value]
 *     None
 */
static void Azel_cache_plane(uint32_t plane_address, uint32_t tile_base,
                             uint8_t *dest)
{
    const uint16_t *src = (const uint16_t *)&Vdp2Ram[plane_address];

    unsigned int tile_y;
    for (tile_y = 0; tile_y < 32; tile_y++) {
        uint8_t *out_ptr = &dest[(tile_y * 16) * 512];
        unsigned int tile_x;
        for (tile_x = 0; tile_x < 32; tile_x++, src++, out_ptr += 128) {
            const uint16_t data = BSWAP16(*src);
            unsigned int tile_index = (data & 0x3FF) << 2;
            unsigned int flip_x = (data & 0x400) ?  8 : 0;
            unsigned int flip_y = (data & 0x800) ? 15 : 0;
            const uint8_t *tile_data = &Vdp2Ram[tile_base + (tile_index<<5)];
            unsigned int char_y;
            for (char_y = 0; char_y < 2; char_y++) {
                unsigned int char_x;
                for (char_x = 0; char_x < 2; char_x++) {
                    unsigned int pixel_y;
                    for (pixel_y = 0; pixel_y < 8; pixel_y++, tile_data += 8) {
                        const unsigned int y = (char_y*8 + pixel_y) ^ flip_y;
                        uint8_t * const y_ptr =
                            &out_ptr[((y/8) * (512*8)) + ((y%8) * 16)
                                     + ((char_x*8) ^ flip_x)];
                        const uint32_t pix0_3 =
                            ((const uint32_t *)tile_data)[0];
                        const uint32_t pix4_7 =
                            ((const uint32_t *)tile_data)[1];
                        if (flip_x) {
                            ((uint32_t *)y_ptr)[0] = BSWAP32(pix4_7);
                            ((uint32_t *)y_ptr)[1] = BSWAP32(pix0_3);
                        } else {
                            ((uint32_t *)y_ptr)[0] = pix0_3;
                            ((uint32_t *)y_ptr)[1] = pix4_7;
                        }
                    }  // pixel_y
                }  // char_x
            }  // char_y
        }  // tile_x
    }  // tile_y
}

/*----------------------------------*/

/**
 * Azel_make_mipmap:  Create a half-size mipmap from the given pixel buffer
 * by dropping every second pixel.
 *
 * [Parameters]
 *         in: Input pixel buffer
 *       size: Size (width and height) of input pixel buffer
 *        out: Output pixel buffer
 *     stride: Line length of output buffer (normally size/2)
 * [Return value]
 *     None
 */
static void Azel_make_mipmap(const uint8_t *in, unsigned int size,
                             uint8_t *out, unsigned int stride)
{
#define SHRINK                          \
    asm(".set push; .set noreorder\n"   \
        "srl %[temp], %[a], 16\n"       \
        "ins %[a], %[temp], 8, 8\n"     \
        "ins %[a], %[b], 16, 8\n"       \
        "srl %[b], %[b], 16\n"          \
        "ins %[a], %[b], 24, 8\n"       \
        "srl %[temp], %[c], 16\n"       \
        "ins %[c], %[temp], 8, 8\n"     \
        "ins %[c], %[d], 16, 8\n"       \
        "srl %[d], %[d], 16\n"          \
        "ins %[c], %[d], 24, 8\n"       \
        ".set pop"                      \
        : [a] "=r" (a), [b] "=r" (b), [c] "=r" (c), [d] "=r" (d), \
          [temp] "=&r" (temp)           \
        : "0" (a), "1" (b), "2" (c), "3" (d) \
    )

    unsigned int y;
    for (y = 0; y < size; y += 16, in += size*8, out += (stride - size/2)*8) {
        unsigned int x;
        for (x = 0; x < size; x += 32, in += 128, out += 64) {
            unsigned int line;
            for (line = 0; line < 4; line++, in += 32, out += 16) {
                uint32_t a, b, c, d, temp;

                a = ((const uint32_t *)in)[0];
                b = ((const uint32_t *)in)[1];
                c = ((const uint32_t *)in)[2];
                d = ((const uint32_t *)in)[3];
                SHRINK;
                ((uint32_t *)out)[0] = a;
                ((uint32_t *)out)[1] = c;

                a = ((const uint32_t *)in)[32];
                b = ((const uint32_t *)in)[33];
                c = ((const uint32_t *)in)[34];
                d = ((const uint32_t *)in)[35];
                SHRINK;
                ((uint32_t *)out)[2] = a;
                ((uint32_t *)out)[3] = c;

                a = ((const uint32_t *)in)[size*2+0];
                b = ((const uint32_t *)in)[size*2+1];
                c = ((const uint32_t *)in)[size*2+2];
                d = ((const uint32_t *)in)[size*2+3];
                SHRINK;
                ((uint32_t *)out)[16] = a;
                ((uint32_t *)out)[17] = c;

                a = ((const uint32_t *)in)[size*2+32];
                b = ((const uint32_t *)in)[size*2+33];
                c = ((const uint32_t *)in)[size*2+34];
                d = ((const uint32_t *)in)[size*2+35];
                SHRINK;
                ((uint32_t *)out)[18] = a;
                ((uint32_t *)out)[19] = c;
            }
        }
    }
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_draw_RBG0:  Draw a sky/ground RBG0 layer from cached data.
 *
 * [Parameters]
 *     info: Graphics layer data
 *     clip: Clipping window data
 * [Return value]
 *     Nonzero if the graphics layer was drawn, zero if not
 */
static int Azel_draw_RBG0(vdp2draw_struct *info,
                          const clipping_struct *clip)
{
    /* Define a macro to read a coefficient as a signed value, given its
     * index (longword offset into VDP2 RAM).  coef_mask is defined below. */
    #define READ_COEF(index) \
        ((int32_t)T1ReadLong(Vdp2Ram, (index)*4) & coef_mask)

    if (!Azel_RBG0_cached) {
        return 0;
    }

    /* Make sure it's an RPMD mode 2 layer with 4-byte, mode-0 coefficients
     * for the ground (set A) and no coefficients for the sky (set B).
     * However, also allow mode 0 with certain parameter settings used in
     * the Uru underground dungeon. */

    if ((Vdp2Regs->RPMD & 3) != 2
     && !((Vdp2Regs->RPMD & 3) == 0
          && (Vdp2Regs->PLSZ>>10 & 3) == 1
          && Vdp2Regs->OVPNRA == 0x5000)
    ) {
        DMSG("Can't optimize RBG0 (bad rotatemode=%d)", info->rotatemode);
        return 0;
    }
    const int has_sky = ((Vdp2Regs->RPMD & 3) == 2);
    const uint32_t param_address = (Vdp2Regs->RPTA.all << 1) & 0x7FF7C;
    const uint32_t coef_base_high = (Vdp2Regs->KTAOF & 1) << 18;
    if ((Vdp2Regs->KTCTL & 0x0F0F) != 0x0001) {
        DMSG("Can't optimize RBG0 (bad KTCTL=%04X)", Vdp2Regs->KTCTL);
        return 0;
    }

    /* Load rotation matrix parameters. */

    float sky_M[2][3], sky_kx, sky_ky, sky_Xp, sky_Yp;
    float ground_M[2][3], ground_Xp, ground_Yp;
    Azel_get_rotation_matrix(param_address + 0x80,
                             sky_M, &sky_kx, &sky_ky, &sky_Xp, &sky_Yp);
    Azel_get_rotation_matrix(param_address,
                             ground_M, NULL, NULL, &ground_Xp, &ground_Yp);

    const float coef_base = coef_base_high/4
        + (uint32_t)T1ReadLong(Vdp2Ram, param_address+0x54) / 65536.0f;
    const float coef_dy =
        ((int32_t)T1ReadLong(Vdp2Ram, param_address+0x58) << 6) / 4194304.0f;
    const float coef_dx =
        ((int32_t)T1ReadLong(Vdp2Ram, param_address+0x5C) << 6) / 4194304.0f;
    const float coef_right_offset = (disp_width - 1) * coef_dx;

    /* Find the beginning and end of the coefficient table, and determine
     * where the sky/ground boundary is.  For a non-rotated background,
     * the table length will simply be the number of lines on the screen,
     * but when the background is rotated, we need to check all four
     * corners of the screen to find the table's extent.
     *
     * Coefficient values are treated as signed 8.16 bit fixed point, but
     * the program does not clamp its coefficient values properly; as a
     * result, the coefficient value can spill over into the sign bit or
     * the ignored high bits for lines very close to the horizon.  We treat
     * these as sky lines in order to avoid mathematical trouble when
     * drawing.  Due to this, we optimize the (data > 0 && data < 0x800000)
     * test into ((unsigned)data < 0x800000) for more efficient processing.
     * However, there are some areas (such as Mel-Kava) which intentionally
     * store data into the upper 8 bits, which we detect by finding four
     * consecutive coefficients with upper 8 bits between 0x01-0x7F and
     * lower 24 bits less than 0x800000; in that case we mask off bits
     * 24-30 when reading the coefficients.
     *
     * We also check to see whether the ground coefficients are zero (as
     * seems to happen sometimes during a scene change) in order to avoid
     * division by zero when rendering.  We assume that either all or no
     * coefficients are zero, so we only check the first one in the ground
     * region. */

    const float coef_index_UL = coef_base;
    const float coef_index_UR = coef_index_UL + coef_right_offset;
    const float coef_index_LL = coef_base + (disp_height - 1) * coef_dy;
    const float coef_index_LR = coef_index_LL + coef_right_offset;
    const uint32_t first_coef_index =
        MIN(MIN(ifloorf(coef_index_UL), ifloorf(coef_index_UR)),
            MIN(ifloorf(coef_index_LL), ifloorf(coef_index_LR)));
    const uint32_t last_coef_index =
        MAX(MAX(ifloorf(coef_index_UL), ifloorf(coef_index_UR)),
            MAX(ifloorf(coef_index_LL), ifloorf(coef_index_LR)));
    const uint32_t num_coefs = last_coef_index + first_coef_index - 1;

    int high_bits_used_count = 0;
    int32_t coef_mask = -1;
    int ground_is_zero;
    unsigned int coef_switch_index;

  retry_coef_scan:

    if ((uint32_t)READ_COEF(first_coef_index) >= 0x800000) {

        ground_is_zero = 0;
        unsigned int offset;
        for (offset = 0; offset < num_coefs; offset++) {
            const uint32_t data = READ_COEF(first_coef_index + offset);
            if (coef_mask == -1) {
                if ((data>>24 >= 0x01 && data>>24 <= 0x7F)
                 && (data & 0xFFFFFF) < 0x800000
                ) {
                    high_bits_used_count++;
                    if (high_bits_used_count >= 4) {
                        coef_mask = (int32_t)0x80FFFFFF;
                        goto retry_coef_scan;
                    }
                } else {
                    high_bits_used_count = 0;
                }
            }
            if (data < 0x800000) {
                ground_is_zero = (data == 0);
                break;
            }
        }
        coef_switch_index = first_coef_index + offset;

    } else {

        ground_is_zero = (READ_COEF(first_coef_index) == 0);
        unsigned int offset;
        for (offset = 0; offset < num_coefs; offset++) {
            const uint32_t data = READ_COEF(first_coef_index + offset);
            if (data >= 0x800000) {
                break;
            }
        }
        coef_switch_index = first_coef_index + offset;

    }

    /* Determine the endpoints of the sky/ground dividing line, if any. */

    const int has_switch = (coef_switch_index <= last_coef_index);
    int switch_x0, switch_y0, switch_x1, switch_y1;

    if (has_switch) {
        Azel_compute_switch(coef_base, coef_switch_index, coef_index_UL,
                            coef_index_UR, coef_index_LL, coef_index_LR,
                            coef_dx, coef_dy, &switch_x0, &switch_y0,
                            &switch_x1, &switch_y1);
        if (UNLIKELY(switch_x0 < 0 || switch_x1 < 0)) {
            DMSG("Failed to find swith line endpoints (base=%05X dx=%.3f"
                 " dy=%.3f", ifloorf(coef_base)*4, coef_dx, coef_dy);
            return 0;
        }
    } else {  // !has_switch
        /* Set the switch line at the bottom of the screen for simplicity. */
        switch_x0 = 0;
        switch_y0 = disp_height;
        switch_x1 = disp_width;
        switch_y1 = disp_height;
    }  // if (has_switch)

    /* Work out the vertex coordinates of the sky and ground regions.
     * Depending on where the switch line falls, these could be three-,
     * four-, or five-sided polygons.  To avoid unnecessary code
     * duplication, we first compute the vertices themselves, then choose
     * which vertex set (coord[0] or coord[1]) to use for the sky and
     * ground regions.
     *
     * The "overdraw_x" and "overdraw_y" fields are set to either +1 or
     * -1 to indicate the direction each coordinate should be shifted for
     * overdrawing when rendering the sky region. */

    struct Azel_RBG0_coord coord[2][5];
    unsigned int nverts[2];

    const int UL_is_sky =
        ((uint32_t)READ_COEF(ifloorf(coef_index_UL)) >= 0x800000);
    const int UR_is_sky =
        ((uint32_t)READ_COEF(ifloorf(coef_index_UR)) >= 0x800000);
    const int LL_is_sky =
        ((uint32_t)READ_COEF(ifloorf(coef_index_LL)) >= 0x800000);
    const int LR_is_sky =
        ((uint32_t)READ_COEF(ifloorf(coef_index_LR)) >= 0x800000);

    Azel_compute_vertices(UL_is_sky, UR_is_sky, LL_is_sky, LR_is_sky,
                          switch_x0, switch_y0, switch_x1, switch_y1,
                          coord, nverts);

    const unsigned int sky_coord_set = UL_is_sky ? 0 : 1;
    unsigned int ground_coord_set = sky_coord_set ^ 1;  // May be changed later

    /* Generate color tables for the two background portions. */

    void *sky_clut, *ground_clut;
    sky_clut = vdp2_gen_t8_clut(Azel_sky_palette << 8, 0,
                                info->transparencyenable,
                                info->cor, info->cog, info->cob);
    if (!sky_clut) {
        DMSG("Failed to generate sky CLUT (palette %u)", Azel_sky_palette);
        return 0;
    }
    ground_clut = vdp2_gen_t8_clut(Azel_ground_palette << 8, 0,
                                   info->transparencyenable,
                                   info->cor, info->cog, info->cob);
    if (!ground_clut) {
        DMSG("Failed to generate ground CLUT (palette %u)",
             Azel_ground_palette);
        return 0;
    }

    /* Set up a vertex structure for rendering. */

    struct {float u, v, x, y, z;} *vertices;
    const uint32_t vertex_type = GU_TEXTURE_32BITF | GU_VERTEX_32BITF;

    /* Draw the sky (flat) portion of the background.  We overdraw by one
     * pixel in each direction to avoid the possibility of undrawn pixels
     * along the switch (horizon) line, since the ground coordinates are
     * adjusted by the Z factor and may end up slightly different from the
     * original values in the coord[] array. */

    if (has_sky && nverts[sky_coord_set] > 0) {

        vertices = guGetMemory(sizeof(*vertices) * nverts[sky_coord_set]);
        unsigned int i;
        for (i = 0; i < nverts[sky_coord_set]; i++) {
            vertices[i].x =
                coord[sky_coord_set][i].x + coord[sky_coord_set][i].overdraw_x;
            vertices[i].y =
                coord[sky_coord_set][i].y + coord[sky_coord_set][i].overdraw_y;
            vertices[i].z = 0;
            Azel_transform_coordinates(vertices[i].x, vertices[i].y,
                                       &vertices[i].u, &vertices[i].v,
                                       sky_M, sky_kx, sky_ky, sky_Xp, sky_Yp);
            /* We deliberately shift the "sky" portion 2 pixels in the
             * overdraw direction because there are occasionally
             * transparent gaps where the background color shows through
             * (e.g. 禁止区域). */
            if (UL_is_sky || UR_is_sky) {
                vertices[i].v -= 2;
            } else {
                vertices[i].v += 2;
            }
        }

        guClutMode(GU_PSM_8888, 0, 255, 0);
        guClutLoad(32, sky_clut);
        guTexMode(GU_PSM_T8, 0, 0, 1);
        guTexImage(0, 512, 512, 512, Azel_sky_cache);
        guTexWrap(GU_REPEAT, Azel_sky_wrap_v ? GU_REPEAT : GU_CLAMP);
        guTexFlush();
        guDrawArray(GU_TRIANGLE_STRIP, GU_TRANSFORM_2D | vertex_type,
                    nverts[sky_coord_set], NULL, vertices);

    }  // if (nverts[sky_coord_set] > 0)

    /* Draw the ground (scaled) portion of the background.  The scale
     * factors in the coefficient table are essentially distance (Z)
     * values, so we set up a projection matrix that allows us to draw
     * a small number of 3D triangles instead of rendering each line
     * individually.  For ease of coordinate handling, the projection
     * matrix maps 3D coordinate (x,y,1) directly to screen coordinate
     * (x+disp_width/2,y+disp_height/2). */

    guClutMode(GU_PSM_8888, 0, 255, 0);
    guClutLoad(32, ground_clut);
    guTexMode(GU_PSM_T8, 0, 0, 1);
    if ((Vdp2Regs->PLSZ>>10 & 3) == 0) {
        guTexWrap(GU_REPEAT, GU_REPEAT);
    } else {
        guTexWrap(GU_CLAMP, GU_CLAMP);
    }

    float Mproj[4][4];
    Mproj[0][0] = 2.0f / disp_width;
    Mproj[0][1] = 0;
    Mproj[0][2] = 0;
    Mproj[0][3] = 0;
    Mproj[1][0] = 0;
    Mproj[1][1] = -2.0f / disp_height;
    Mproj[1][2] = 0;
    Mproj[1][3] = 0;
    Mproj[2][0] = 0;
    Mproj[2][1] = 0;
    Mproj[2][2] = 1.0f / 128;  // Coefficient value range is [0,128).
    Mproj[2][3] = 1;
    Mproj[3][0] = 0;
    Mproj[3][1] = 0;
    Mproj[3][2] = 0;
    Mproj[3][3] = 0;
    guSetMatrix(GU_PROJECTION, &Mproj[0][0]);

    if (nverts[ground_coord_set] == 0) {

        /* Nothing to do. */

    } else if (ground_is_zero) {

        /* Degenerate case: ground coefficients are all zero.  This
         * should only happen while changing scenes, so we don't bother
         * drawing anything. */

    } else if (coef_switch_index == last_coef_index) {

        /* Degenerate case: only one ground coefficient (we also come here
         * if we detect a slope of zero).  We can't compute appropriate 3D
         * coordinates for this case, but since it's effectively flat like
         * the sky, just draw it that way. */

      draw_as_flat:;
        const int32_t data = READ_COEF(coef_switch_index);
        const float kx = data / 65536.0f;
        const float ky = kx;

        vertices = guGetMemory(sizeof(*vertices) * nverts[ground_coord_set]);
        unsigned int i;
        for (i = 0; i < nverts[ground_coord_set]; i++) {
            vertices[i].x = coord[ground_coord_set][i].x;
            vertices[i].y = coord[ground_coord_set][i].y;
            vertices[i].z = 0;
            Azel_transform_coordinates(vertices[i].x, vertices[i].y,
                                       &vertices[i].u, &vertices[i].v,
                                       ground_M, kx, ky, ground_Xp, ground_Yp);
        }
        if (kx >= 6.0f) {
            guTexImage(0,  64,  64,  64,
                       Azel_ground_cache + 512*512 + 256*256 + 128*128);
        } else if (kx >= 3.0f) {
            guTexImage(0, 128, 128, 128, Azel_ground_cache + 512*512 + 256*256);
        } else if (kx >= 1.5f) {
            guTexImage(0, 256, 256, 256, Azel_ground_cache + 512*512);
        } else {
            guTexImage(0, 512, 512, 512, Azel_ground_cache);
        }
        guTexFlush();
        guDrawArray(GU_TRIANGLE_STRIP, GU_TRANSFORM_2D | vertex_type,
                    nverts[ground_coord_set], NULL, vertices);

    } else {

        /* Determine the indices of the first and last ground coefficients. */

        uint32_t first_ground_index, last_ground_index;
        if ((uint32_t)READ_COEF(first_coef_index) < 0x800000) {
            first_ground_index = first_coef_index;
            if (has_switch) {
                last_ground_index = coef_switch_index - 1;
            } else {
                last_ground_index = last_coef_index;
            }
        } else {
            first_ground_index = coef_switch_index;
            last_ground_index = last_coef_index;
        }

        /* Compute the approximate plane slope from the average of the
         * slopes from the first coefficient to all other coefficients.
         * (For certain "ground" textures, like water, the actual
         * coefficients are modulated by a small amount to produce a
         * "shimmering" effect, so they won't give a single, consistent
         * result from one coefficient to the next; thus we take the
         * average and use that instead.)
         *
         * If we have a hint from the satopt-sh2.c optimizer, we use that
         * instead; however, it seems to be delayed by a frame, so we use
         * a local static variable to accomplish the same thing. */

        static float slope_hint_delayed = SLOPE_UNSET;
        static float first_recip_hint_delayed;
        const float slope_hint = slope_hint_delayed;
        const float first_recip_hint = first_recip_hint_delayed;
        slope_hint_delayed = psp_video_tweaks_Azel_RBG0_slope;
        first_recip_hint_delayed = psp_video_tweaks_Azel_RBG0_first_recip;

        float slope = 0;
        const int32_t first_data = READ_COEF(first_ground_index);
        float first_recip;
        if (slope_hint != SLOPE_UNSET && slope_hint_delayed != SLOPE_UNSET) {
            slope = slope_hint;
            first_recip = first_recip_hint;
            if (slope != 0) {
                /* This may be slightly off (e.g. due to an out-of-range
                 * coefficient), so adjust as necessary. */
                float diff;
                if (slope > 0) {
                    diff = first_recip - 65536.0f/first_data;
                } else {
                    const float last_recip =
                        first_recip + slope * (last_ground_index - first_ground_index);
                    const float recip_test =
                        65536.0f / READ_COEF(last_ground_index);
                    diff = last_recip - recip_test;
                }
                if (fabsf(diff) > (slope/2)) {
                    first_recip -= roundf(diff / fabsf(slope)) * fabsf(slope);
                }
            }
        } else {
            first_recip = 65536.0f / first_data;
            uint32_t i;
            for (i = first_ground_index + 1; i <= last_ground_index; i++) {
                const int32_t data = READ_COEF(i);
                const float recip = 65536.0f / data;
                slope += (recip - first_recip) / (i - first_ground_index);
            }
            slope /= last_ground_index - first_ground_index;
        }

        if (slope == 0) {  // Avoid division by zero below.
            goto draw_as_flat;
        }

        /* Go over the coefficient list and find the variance from the
         * linear slope we just derived, to determine whether or not we
         * need to apply our own "shimmering" effect. */

        float variance = 0;
        uint32_t index;
        for (index = first_ground_index; index <= last_ground_index; index++) {
            const int32_t data = READ_COEF(index);
            const float recip = 65536.0f / data;
            const float expected =
                first_recip + slope * (index - first_ground_index);
            const float error = (recip - expected) / slope;
            variance += error * error;
        }
        variance /= last_ground_index - first_ground_index + 1;
        const int do_shimmer = (variance > 0.01f);
        const float shimmer_step =
            (sceKernelGetSystemTimeLow() & 0x7FFFFF) / (float)0x800000;

        /* If the entire screen is "ground" and the slope is positive,
         * move the switch line to the top of the screen so we draw
         * the proper portion as mipmapped. */

        if (switch_y0 == disp_height && slope > 0) {
            switch_y0 = switch_y1 = 0;
        }

        /* If the ground plane needs to be split into separate mipmapped
         * and normal regions, first draw the distant (mipmapped) part of
         * the plane.
         * FIXME: This section is making me nauseous, which is a sign that
         * it's poorly written and desperately needs refactoring--or perhaps
         * a complete rethinking...
         */

        index = (slope > 0) ? first_ground_index : last_ground_index + 1;
        int32_t mipmap_scale;
        for (mipmap_scale = 8;
             mipmap_scale > 1 && (slope > 0 ? index <= last_ground_index
                                            : index > first_ground_index);
             mipmap_scale /= 2
        ) {

            /* Find the index of the coefficient at which the mipmap scale
             * switches.  We normally use mipmaps only at scales above the
             * mipmap's level, but we're more aggressive about using
             * mipmaps in water areas because we need the speed in Uru. */

            const float mipmap_scale_recip =
                (do_shimmer ? 2.0f : 1.0f) / mipmap_scale;
            const int32_t mipmap_switch_offset =
                iceilf((mipmap_scale_recip - first_recip) / slope);
            if (mipmap_switch_offset < 0) {
                continue;
            }
            uint32_t mipmap_switch_index =
                first_ground_index + mipmap_switch_offset;
            if (mipmap_switch_index > last_ground_index + 1) {
                mipmap_switch_index = last_ground_index + 1;
            }
            if (slope > 0) {
                /* If there's only a small section to draw for this mipmap
                 * level, skip this iteration and draw it as part of the
                 * next, since we'll probably get better use of the GE's
                 * texture cache that way. */
                if (mipmap_switch_index <= index + 4) {
                    continue;
                }
                /* If we've covered the entire region with this mipmap
                 * level--or if this would leave only a small amount to
                 * draw with the next level, which again would make poor
                 * use of the texture cache, break out of the loop and let
                 * the last pass handle it with the already-computed
                 * vertices.  Note that we explicitly don't update "index"
                 * in this case so the last pass is not skipped. */
                if (mipmap_switch_index > last_ground_index - 4) {
                    break;
                }
            } else {  // slope < 0
                if (mipmap_switch_index >= index - 4) {
                    continue;
                }
                if (index <= first_ground_index + 4) {
                    index--;
                }
            }
            index = mipmap_switch_index;  // Save it for next time around.

            /* Compute switch line coordinates for the mipmap line. */

            int mipmap_x0, mipmap_y0, mipmap_x1, mipmap_y1;
            Azel_compute_switch(coef_base, mipmap_switch_index, coef_index_UL,
                                coef_index_UR, coef_index_LL, coef_index_LR,
                                coef_dx, coef_dy, &mipmap_x0, &mipmap_y0,
                                &mipmap_x1, &mipmap_y1);

            /* Compute vertices for the mipmapped region.  This is a
             * rectangle if the horizon line is horizontal or vertical, but
             * may be a four-, five-, or six-sided polygon if the horizon
             * is tilted. */

            struct {int x, y;} mipmap_coords[6];
            unsigned int mipmap_nverts;
            if (coef_dx == 0 || coef_dy == 0) {
                mipmap_coords[0].x = switch_x0;
                mipmap_coords[0].y = switch_y0;
                mipmap_coords[1].x = switch_x1;
                mipmap_coords[1].y = switch_y1;
                mipmap_coords[2].x = mipmap_x0;
                mipmap_coords[2].y = mipmap_y0;
                mipmap_coords[3].x = mipmap_x1;
                mipmap_coords[3].y = mipmap_y1;
                mipmap_nverts = 4;
            } else {
                /* First determine which line is on top (has smaller
                 * Y coordinates), to simplify computations.  We make use
                 * of the knowledge that Azel_compute_switch() assigns
                 * coordinates in the preference order top > left > right
                 * > bottom edge.  Also swap the lower line's coordinates
                 * if necessary so they are in the same order as the upper
                 * line; depending on the position of the lines, the
                 * preference order may result in coordinates being
                 * swapped. */
                int switch_is_top, invert_second;
                if (switch_y0 == 0) {
                    if (mipmap_y0 != 0) {
                        switch_is_top = 1;
                        invert_second = (switch_x1 < switch_x0
                                         && mipmap_x0 == 0);
                    } else if (switch_x1 < switch_x0) {
                        switch_is_top = (switch_x0 < mipmap_x0);
                        invert_second = 0;
                    } else {
                        switch_is_top = (switch_x0 > mipmap_x0);
                        invert_second = 0;
                    }
                } else if (mipmap_y0 == 0) {
                    switch_is_top = 0;
                        invert_second = (mipmap_x1 < mipmap_x0
                                         && switch_x0 == 0);
                } else if (switch_x0 == mipmap_x0) {
                    switch_is_top = (switch_y0 < mipmap_y0);
                    invert_second = 0;
                } else {
                    /* One line connects the left and right edges, while
                     * the other runs from the right edge to the bottom.
                     * The line connecting the left and right edges is on
                     * top, and the coordinate order is switched. */
                    switch_is_top = (switch_x0 == 0);
                    invert_second = 1;
                }
                const int x0 = switch_is_top ? switch_x0 : mipmap_x0;
                const int y0 = switch_is_top ? switch_y0 : mipmap_y0;
                const int x1 = switch_is_top ? switch_x1 : mipmap_x1;
                const int y1 = switch_is_top ? switch_y1 : mipmap_y1;
                const int x2 = (invert_second
                                ? (switch_is_top ? mipmap_x1 : switch_x1)
                                : (switch_is_top ? mipmap_x0 : switch_x0));
                const int y2 = (invert_second
                                ? (switch_is_top ? mipmap_y1 : switch_y1)
                                : (switch_is_top ? mipmap_y0 : switch_y0));
                const int x3 = (invert_second
                                ? (switch_is_top ? mipmap_x0 : switch_x0)
                                : (switch_is_top ? mipmap_x1 : switch_x1));
                const int y3 = (invert_second
                                ? (switch_is_top ? mipmap_y0 : switch_y0)
                                : (switch_is_top ? mipmap_y1 : switch_y1));
                /* The first two vertices are always the endpoints of the
                 * top line.  Depending on where the region is located on
                 * the screen, one or two corners may also be included; we
                 * insert either a corner or an endpoint of the lower line
                 * as vertices 2 and 3, then add any remaining endpoints as
                 * vertices 4 and 5 if necessary. */
                mipmap_nverts = 4;
                mipmap_coords[0].x = x0;
                mipmap_coords[0].y = y0;
                mipmap_coords[1].x = x1;
                mipmap_coords[1].y = y1;
                if ((y0 == 0 && x0 != 0 && x0 != disp_width) && y2 != 0) {
                    /* Region includes the upper-left or upper-right corner. */
                    mipmap_coords[2].y = 0;
                    if (x2 == 0) {
                        mipmap_coords[2].x = 0;
                    } else {
                        mipmap_coords[2].x = disp_width;
                    }
                    mipmap_coords[mipmap_nverts].x = x2;
                    mipmap_coords[mipmap_nverts].y = y2;
                    mipmap_nverts++;
                    if (x1 == 0 && y1 != disp_height && x3 != 0) {
                        /* Region also includes the lower-left corner. */
                        mipmap_coords[3].x = 0;
                        mipmap_coords[3].y = disp_height;
                        mipmap_coords[mipmap_nverts].x = x3;
                        mipmap_coords[mipmap_nverts].y = y3;
                        mipmap_nverts++;
                    } else if (x1 == disp_width && y1 != disp_height
                               && x3 != disp_width) {
                        /* Region also includes the lower-right corner. */
                        mipmap_coords[3].x = disp_width;
                        mipmap_coords[3].y = disp_height;
                        mipmap_coords[mipmap_nverts].x = x3;
                        mipmap_coords[mipmap_nverts].y = y3;
                        mipmap_nverts++;
                    } else {
                        mipmap_coords[3].x = x3;
                        mipmap_coords[3].y = y3;
                    }
                } else if ((x0 == 0 && y0 != 0 && y0 != disp_height) && x2 != 0) {
                    /* Region includes the lower-left corner.  (In this
                     * case, the top line slants up-and-right and ends on
                     * the right edge of the screen.) */
                    mipmap_coords[2].x = 0;
                    mipmap_coords[2].y = disp_height;
                    mipmap_coords[mipmap_nverts].x = x2;
                    mipmap_coords[mipmap_nverts].y = y2;
                    mipmap_nverts++;
                    mipmap_coords[3].x = x3;
                    mipmap_coords[3].y = y3;
                } else {
                    /* Endpoint 0 of the horizon and mipmap lines are on
                     * the same edge of the screen. */
                    mipmap_coords[2].x = x2;
                    mipmap_coords[2].y = y2;
                    if ((y0 == 0 || x0 == disp_width)
                     && (x1 == 0 && y1 != disp_height)
                     && x3 != 0
                    ) {
                        /* Region includes the lower-left corner. */
                        mipmap_coords[3].x = 0;
                        mipmap_coords[3].y = disp_height;
                        mipmap_coords[mipmap_nverts].x = x3;
                        mipmap_coords[mipmap_nverts].y = y3;
                        mipmap_nverts++;
                    } else if ((y0 == 0 || x0 == 0)
                            && (x1 == disp_width && y1 != disp_height)
                            && x3 != disp_width
                    ) {
                        /* Region includes the lower-right corner. */
                        mipmap_coords[3].x = disp_width;
                        mipmap_coords[3].y = disp_height;
                        mipmap_coords[mipmap_nverts].x = x3;
                        mipmap_coords[mipmap_nverts].y = y3;
                        mipmap_nverts++;
                    } else {
                        mipmap_coords[3].x = x3;
                        mipmap_coords[3].y = y3;
                    }
                }
            }

            /* Draw the mipmapped region computed above. */

            vertices = guGetMemory(sizeof(*vertices) * mipmap_nverts);
            unsigned int i;
            for (i = 0; i < mipmap_nverts; i++) {
                vertices[i].x = mipmap_coords[i].x;
                vertices[i].y = mipmap_coords[i].y;
                const float coef_offset = coef_base
                                        + vertices[i].x * coef_dx
                                        + vertices[i].y * coef_dy
                                        - first_ground_index;
                vertices[i].z =
                    1 / MAX(first_recip + coef_offset * slope, 1/127.5f);
                Azel_transform_coordinates(vertices[i].x, vertices[i].y,
                                           &vertices[i].u, &vertices[i].v,
                                           ground_M,
                                           vertices[i].z, vertices[i].z,
                                           ground_Xp, ground_Yp);
                vertices[i].u /= 512;
                vertices[i].v /= 512;
                if (do_shimmer) {
                    vertices[i].v += 0.01f * sinf(shimmer_step * (2*(float)M_PI));
                }
                vertices[i].x = (vertices[i].x - disp_width/2)  * vertices[i].z;
                vertices[i].y = (vertices[i].y - disp_height/2) * vertices[i].z;
            }
            const unsigned int texture_size = 512 / mipmap_scale;
            const uint8_t *texture_data = Azel_ground_cache;
            for (i = 512; i > texture_size; i /= 2) {
                texture_data += i * i;
            }
            guTexImage(0, texture_size, texture_size, texture_size,
                       texture_data);
            guTexFlush();
            guDrawArray(GU_TRIANGLE_STRIP, GU_TRANSFORM_3D | vertex_type,
                        mipmap_nverts, NULL, vertices);

            /* Recompute the vertex sets based on the mipmap line, so the
             * final render iteration (with the full-size ground texture)
             * skips the part we just drew.  Since Azel_compute_vertices()
             * uses the *_is_sky variables to determine whether the top or
             * bottom portion is "ground", we need to tweak those variables
             * in case the entire screen is "ground" so we get the proper
             * region registered.  Note that we don't check the actual
             * coefficients, because that may give incorrect results due to
             * the "shimmering" effect; instead, we compare the coefficient
             * indices to the mipmap switch index. */

            int UL_sky_2, UR_sky_2, LL_sky_2, LR_sky_2;
            if (slope > 0) {
                UL_sky_2 = (coef_index_UL < mipmap_switch_index);
                UR_sky_2 = (coef_index_UR < mipmap_switch_index);
                LL_sky_2 = (coef_index_LL < mipmap_switch_index);
                LR_sky_2 = (coef_index_LR < mipmap_switch_index);
            } else {
                UL_sky_2 = (coef_index_UL >= mipmap_switch_index);
                UR_sky_2 = (coef_index_UR >= mipmap_switch_index);
                LL_sky_2 = (coef_index_LL >= mipmap_switch_index);
                LR_sky_2 = (coef_index_LR >= mipmap_switch_index);
            }
            Azel_compute_vertices(UL_sky_2, UR_sky_2, LL_sky_2, LR_sky_2,
                                  mipmap_x0, mipmap_y0, mipmap_x1, mipmap_y1,
                                  coord, nverts);
            ground_coord_set = (UL_sky_2 ? 1 : 0);

            /* Copy the mipmap line coordinates to switch_* for the next
             * mipmap loop. */

            switch_x0 = mipmap_x0;
            switch_y0 = mipmap_y0;
            switch_x1 = mipmap_x1;
            switch_y1 = mipmap_y1;

        }  // if using mipmap

        /* Generate and render vertices for the lowest-scale portion of the
         * the plane, if any. */

        if (slope > 0 ? index <= last_ground_index
                      : index > first_ground_index) {
            vertices = guGetMemory(sizeof(*vertices) * nverts[ground_coord_set]);
            unsigned int i;
            for (i = 0; i < nverts[ground_coord_set]; i++) {
                vertices[i].x = coord[ground_coord_set][i].x;
                vertices[i].y = coord[ground_coord_set][i].y;
                const float coef_offset = coef_base
                                        + vertices[i].x * coef_dx
                                        + vertices[i].y * coef_dy
                                        - first_ground_index;
                vertices[i].z =
                    1 / MAX(first_recip + coef_offset * slope, 1/127.5f);
                Azel_transform_coordinates(vertices[i].x, vertices[i].y,
                                           &vertices[i].u, &vertices[i].v,
                                           ground_M,
                                           vertices[i].z, vertices[i].z,
                                           ground_Xp, ground_Yp);
                vertices[i].u /= (Azel_ground_reduced ? 1024 : 512);
                vertices[i].v /= (Azel_ground_reduced ? 1024 : 512);
                if (do_shimmer) {
                    /* Fake the "shimmering" effect with a simple sinusoidal
                     * offset.  The PSP doesn't have enough hardware operators
                     * to do what we really want (which is multiply each
                     * texture coordinate by a*sin(b*y/z), where a and b are
                     * constants). */
                    vertices[i].v += 0.01f * sinf(shimmer_step * (2*(float)M_PI));
                }
                vertices[i].x = (vertices[i].x - disp_width/2)  * vertices[i].z;
                vertices[i].y = (vertices[i].y - disp_height/2) * vertices[i].z;
            }
            const unsigned int texture_size = 512 / mipmap_scale;
            const uint8_t *texture_data = Azel_ground_cache;
            for (i = 512; i > texture_size; i /= 2) {
                texture_data += i * i;
            }
            guTexImage(0, texture_size, texture_size, texture_size,
                       texture_data);
            guTexFlush();
            guDrawArray(GU_TRIANGLE_STRIP, GU_TRANSFORM_3D | vertex_type,
                        nverts[ground_coord_set], NULL, vertices);
        }

    }  // if (ground_min == ground_max)

    /* All done.  Make sure to reset the wrapping flags since other code
     * will expect wraparound to be disabled. */

    guTexWrap(GU_CLAMP, GU_CLAMP);
    return 1;

    #undef READ_COEF
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_get_rotation_matrix:  Calculate the rotation matrix and scaling
 * parameters for a sky or ground parameter set.  Helper function for
 * Azel_draw_RBG0().
 *
 * [Parameters]
 *     address: Address of parameter set in VDP2 RAM
 *           M: Pointer to 2x3 array to receive rotation matrix values
 *      kx_ret: Pointer to variable to receive X scale value (NULL allowed)
 *      ky_ret: Pointer to variable to receive Y scale value (NULL allowed)
 *      Xp_ret: Pointer to variable to receive X offset value
 *      Yp_ret: Pointer to variable to receive Y offset value
 * [Return value]
 *     None
 */
static void Azel_get_rotation_matrix(uint32_t address, float M[2][3],
                                     float *kx_ret, float *ky_ret,
                                     float *Xp_ret, float *Yp_ret)
{
    /* The GET_* macros are borrowed from psp-video-rotate.c. */

    #define GET_SHORT(nbits) \
        (address += 2, (int32_t)((int16_t)T1ReadWord(Vdp2Ram,address-2) \
                                 << (16-nbits)) >> (16-nbits))
    #define GET_SIGNED_FLOAT(nbits) \
        (address += 4, ((((int32_t)T1ReadLong(Vdp2Ram,address-4) \
                      << (32-nbits)) >> (32-nbits)) & ~0x3F) / 65536.0f)
    #define GET_UNSIGNED_FLOAT(nbits) \
        (address += 4, (((uint32_t)T1ReadLong(Vdp2Ram,address-4) \
                      & (0xFFFFFFFFU >> (32-nbits))) & ~0x3F) / 65536.0f)

    const float Xst      = GET_SIGNED_FLOAT(29);
    const float Yst      = GET_SIGNED_FLOAT(29);
    const float Zst      = GET_SIGNED_FLOAT(29);
    const float deltaXst = GET_SIGNED_FLOAT(19);
    const float deltaYst = GET_SIGNED_FLOAT(19);
    const float deltaX   = GET_SIGNED_FLOAT(19);
    const float deltaY   = GET_SIGNED_FLOAT(19);
    const float A        = GET_SIGNED_FLOAT(20);
    const float B        = GET_SIGNED_FLOAT(20);
    const float C        = GET_SIGNED_FLOAT(20);
    const float D        = GET_SIGNED_FLOAT(20);
    const float E        = GET_SIGNED_FLOAT(20);
    const float F        = GET_SIGNED_FLOAT(20);
    const float Px       = GET_SHORT(14);
    const float Py       = GET_SHORT(14);
    const float Pz       = GET_SHORT(14);
    address += 2;
    const float Cx       = GET_SHORT(14);
    const float Cy       = GET_SHORT(14);
    const float Cz       = GET_SHORT(14);
    address += 2;
    const float Mx       = GET_SIGNED_FLOAT(30);
    const float My       = GET_SIGNED_FLOAT(30);
    const float kx       = GET_SIGNED_FLOAT(24);
    const float ky       = GET_SIGNED_FLOAT(24);

    #undef GET_SHORT
    #undef GET_SIGNED_FLOAT
    #undef GET_UNSIGNED_FLOAT

    M[0][0] = (A * deltaX)     + (B * deltaY);
    M[0][1] = (A * deltaXst)   + (B * deltaYst);
    M[0][2] = (A * (Xst - Px)) + (B * (Yst - Py)) + (C * (Zst - Pz));
    M[1][0] = (D * deltaX)     + (E * deltaY);
    M[1][1] = (D * deltaXst)   + (E * deltaYst);
    M[1][2] = (D * (Xst - Px)) + (E * (Yst - Py)) + (F * (Zst - Pz));
    *Xp_ret = (A * (Px - Cx))  + (B * (Py - Cy))  + (C * (Pz - Cz))  + Cx + Mx;
    *Yp_ret = (D * (Px - Cx))  + (E * (Py - Cy))  + (F * (Pz - Cz))  + Cy + My;

    if (kx_ret) {
        *kx_ret = kx;
    }
    if (ky_ret) {
        *ky_ret = ky;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_transform_coordinates:  Transform screen to texture coordinates
 * given the rotation matrix and scaling parameters for a sky or ground
 * parameter set.  Helper function for Azel_draw_RBG0().
 *
 * [Parameters]
 *             x, y: Screen coordinates to transform
 *     u_ret, v_ret: Pointers to variables to receive transformed coordinates
 *                M: Pointer to 2x3 array containing rotation matrix values
 *           kx, ky: X/Y scale values
 *           Xp, Yp: X/Y offset values
 * [Return value]
 *     None
 */
static void Azel_transform_coordinates(const float x, const float y,
                                       float *u_ret, float *v_ret,
                                       float M[2][3],
                                       const float kx, const float ky,
                                       const float Xp, const float Yp)
{
    *u_ret = (M[0][0]*x + M[0][1]*y + M[0][2]) * kx + Xp;
    *v_ret = (M[1][0]*x + M[1][1]*y + M[1][2]) * ky + Yp;
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_compute_switch:  Compute the endpoints of the switch (horizon) line
 * between sky and ground.  Helper function for Azel_draw_RBG0().
 *
 * [Parameters]
 *     All parameters are local variables or pointers thereto passed from
 *     Azel_draw_RBG0()
 * [Return value]
 *     None
 */
static inline void Azel_compute_switch(
    uint32_t coef_base, uint32_t coef_switch_index,
    const uint32_t coef_index_UL, const uint32_t coef_index_UR,
    const uint32_t coef_index_LL, const uint32_t coef_index_LR,
    float coef_dx, float coef_dy,
    int *switch_x0, int *switch_y0, int *switch_x1, int *switch_y1)
{
    *switch_x0 = *switch_y0 = *switch_x1 = *switch_y1 = -1;

    if (coef_dx == 0) {

        *switch_x0 = 0;
        *switch_y0 = iceilf((coef_switch_index - coef_base) / coef_dy);
        *switch_x1 = disp_width;
        *switch_y1 = *switch_y0;

    } else if (coef_dy == 0) {

        *switch_x0 = iceilf((coef_switch_index - coef_base) / coef_dx);
        *switch_y0 = 0;
        *switch_x1 = *switch_x0;
        *switch_y1 = disp_height;

    } else {  // coef_dx != 0 && coef_dy != 0

        const float top_x =
            (int32_t)(coef_switch_index - coef_index_UL) / coef_dx;
        if (top_x > -1 && top_x <= disp_width) {
            *switch_x0 = iceilf(top_x);
            *switch_y0 = 0;
        }
        const float bottom_x =
            (int32_t)(coef_switch_index - coef_index_LL) / coef_dx;
        if (bottom_x > -1 && bottom_x <= disp_width) {
            *switch_x1 = iceilf(bottom_x);
            *switch_y1 = disp_height;
        }
        const float left_y =
            (int32_t)(coef_switch_index - coef_index_UL) / coef_dy;
        if (left_y > -1 && left_y <= disp_height) {
            if (*switch_x0 < 0) {
                *switch_x0 = 0;
                *switch_y0 = iceilf(left_y);
            } else if (*switch_x1 < 0) {
                *switch_x1 = 0;
                *switch_y1 = iceilf(left_y);
            }
        }
        const float right_y =
            (int32_t)(coef_switch_index - coef_index_UR) / coef_dy;
        if (right_y > -1 && right_y <= disp_height) {
            if (*switch_x0 < 0) {
                *switch_x0 = disp_width;
                *switch_y0 = iceilf(right_y);
            } else if (*switch_x1 < 0) {
                *switch_x1 = disp_width;
                *switch_y1 = iceilf(right_y);
            }
        }

    }
}

/*-----------------------------------------------------------------------*/

/**
 * Azel_compute_vertices:  Compute the two sets of vertices for drawing
 * sky and ground sections of an RBG0 layer.  Helper function for
 * Azel_draw_RBG0().
 *
 * [Parameters]
 *     All parameters are local variables/arrays passed from Azel_draw_RBG0()
 * [Return value]
 *     None
 */
static inline void Azel_compute_vertices(
    int UL_is_sky, int UR_is_sky, int LL_is_sky, int LR_is_sky,
    int switch_x0, int switch_y0, int switch_x1, int switch_y1,
    struct Azel_RBG0_coord coord[2][5], unsigned int nverts[2])
{
    coord[0][0].x = 0;
    coord[0][0].y = 0;
    coord[0][0].overdraw_x = -1;
    coord[0][0].overdraw_y = -1;

    if (UR_is_sky == UL_is_sky) {

        coord[0][1].x = disp_width;
        coord[0][1].y = 0;
        coord[0][1].overdraw_x = +1;
        coord[0][1].overdraw_y = -1;

        if (LL_is_sky == LR_is_sky) {

            coord[0][2].x = switch_x0;
            coord[0][2].y = switch_y0;
            coord[0][2].overdraw_x = -1;
            coord[0][2].overdraw_y = +1;
            coord[0][3].x = switch_x1;
            coord[0][3].y = switch_y1;
            coord[0][3].overdraw_x = +1;
            coord[0][3].overdraw_y = +1;
            nverts[0] = 4;

            if (LL_is_sky == UL_is_sky) {
                nverts[1] = 0;
            } else {
                coord[1][0].x = switch_x0;
                coord[1][0].y = switch_y0;
                coord[1][0].overdraw_x = -1;
                coord[1][0].overdraw_y = -1;
                coord[1][1].x = switch_x1;
                coord[1][1].y = switch_y1;
                coord[1][1].overdraw_x = +1;
                coord[1][1].overdraw_y = -1;
                coord[1][2].x = 0;
                coord[1][2].y = disp_height;
                coord[1][2].overdraw_x = -1;
                coord[1][2].overdraw_y = +1;
                coord[1][3].x = disp_width;
                coord[1][3].y = disp_height;
                coord[1][3].overdraw_x = +1;
                coord[1][3].overdraw_y = +1;
                nverts[1] = 4;
            }

        } else if (LL_is_sky == UL_is_sky) {

            coord[0][2].x = 0;
            coord[0][2].y = disp_height;
            coord[0][2].overdraw_x = -1;
            coord[0][2].overdraw_y = +1;
            coord[0][3].x = switch_x0;
            coord[0][3].y = switch_y0;
            coord[0][3].overdraw_x = +1;
            coord[0][3].overdraw_y = +1;
            coord[0][4].x = switch_x1;
            coord[0][4].y = switch_y1;
            coord[0][4].overdraw_x = +1;
            coord[0][4].overdraw_y = +1;
            nverts[0] = 5;

            coord[1][0].x = switch_x0;
            coord[1][0].y = switch_y0;
            coord[1][0].overdraw_x = +1;
            coord[1][0].overdraw_y = -1;
            coord[1][1].x = switch_x1;
            coord[1][1].y = switch_y1;
            coord[1][1].overdraw_x = -1;
            coord[1][1].overdraw_y = +1;
            coord[1][2].x = disp_width;
            coord[1][2].y = disp_height;
            coord[1][3].overdraw_x = +1;
            coord[1][3].overdraw_y = +1;
            nverts[1] = 3;

        } else {  // LR_is_sky == UL_is_sky

            coord[0][2].x = switch_x0;
            coord[0][2].y = switch_y0;
            coord[0][2].overdraw_x = -1;
            coord[0][2].overdraw_y = +1;
            coord[0][3].x = disp_width;
            coord[0][3].y = disp_height;
            coord[0][3].overdraw_x = +1;
            coord[0][3].overdraw_y = +1;
            coord[0][4].x = switch_x1;
            coord[0][4].y = switch_y1;
            coord[0][4].overdraw_x = -1;
            coord[0][4].overdraw_y = +1;
            nverts[0] = 5;

            coord[1][0].x = switch_x0;
            coord[1][0].y = switch_y0;
            coord[1][0].overdraw_x = -1;
            coord[1][0].overdraw_y = -1;
            coord[1][1].x = switch_x1;
            coord[1][1].y = switch_y1;
            coord[1][1].overdraw_x = +1;
            coord[1][1].overdraw_y = +1;
            coord[1][2].x = 0;
            coord[1][2].y = disp_height;
            coord[1][2].overdraw_x = -1;
            coord[1][2].overdraw_y = +1;
            nverts[1] = 3;

        }

    } else if (LL_is_sky == UL_is_sky) {

        coord[0][1].x = 0;
        coord[0][1].y = disp_height;
        coord[0][1].overdraw_x = -1;
        coord[0][1].overdraw_y = +1;

        if (LR_is_sky != UL_is_sky) {

            coord[0][2].x = switch_x0;
            coord[0][2].y = switch_y0;
            coord[0][2].overdraw_x = +1;
            coord[0][2].overdraw_y = -1;
            coord[0][3].x = switch_x1;
            coord[0][3].y = switch_y1;
            coord[0][3].overdraw_x = +1;
            coord[0][3].overdraw_y = +1;
            nverts[0] = 4;

            coord[1][0].x = switch_x0;
            coord[1][0].y = switch_y0;
            coord[1][0].overdraw_x = -1;
            coord[1][0].overdraw_y = -1;
            coord[1][1].x = switch_x1;
            coord[1][1].y = switch_y1;
            coord[1][1].overdraw_x = -1;
            coord[1][1].overdraw_y = +1;
            coord[1][2].x = disp_width;
            coord[1][2].y = 0;
            coord[1][2].overdraw_x = +1;
            coord[1][2].overdraw_y = -1;
            coord[1][3].x = disp_width;
            coord[1][3].y = disp_height;
            coord[1][3].overdraw_x = +1;
            coord[1][3].overdraw_y = +1;
            nverts[1] = 4;

        } else {  // LR_is_sky == UL_is_sky

            coord[0][2].x = switch_x0;
            coord[0][2].y = switch_y0;
            coord[0][2].overdraw_x = +1;
            coord[0][2].overdraw_y = -1;
            coord[0][3].x = disp_width;
            coord[0][3].y = disp_height;
            coord[0][3].overdraw_x = +1;
            coord[0][3].overdraw_y = +1;
            coord[0][4].x = switch_x1;
            coord[0][4].y = switch_y1;
            coord[0][4].overdraw_x = +1;
            coord[0][4].overdraw_y = -1;
            nverts[0] = 5;

            coord[1][0].x = switch_x0;
            coord[1][0].y = switch_y0;
            coord[1][0].overdraw_x = -1;
            coord[1][0].overdraw_y = -1;
            coord[1][1].x = switch_x1;
            coord[1][1].y = switch_y1;
            coord[1][1].overdraw_x = +1;
            coord[1][1].overdraw_y = +1;
            coord[1][2].x = disp_width;
            coord[1][2].y = 0;
            coord[1][2].overdraw_x = +1;
            coord[1][2].overdraw_y = -1;
            nverts[1] = 3;

        }

    } else {

        coord[0][1].x = switch_x0;
        coord[0][1].y = switch_y0;
        coord[0][1].overdraw_x = +1;
        coord[0][1].overdraw_y = -1;
        coord[0][2].x = switch_x1;
        coord[0][2].y = switch_y1;
        coord[0][2].overdraw_x = -1;
        coord[0][2].overdraw_y = +1;
        nverts[0] = 3;

        coord[1][0].x = switch_x0;
        coord[1][0].y = switch_y0;
        coord[1][0].overdraw_x = -1;
        coord[1][0].overdraw_y = -1;
        coord[1][1].x = disp_width;
        coord[1][1].y = 0;
        coord[1][1].overdraw_x = +1;
        coord[1][1].overdraw_y = -1;
        coord[1][2].x = switch_x1;
        coord[1][2].y = switch_y1;
        coord[1][2].overdraw_x = -1;
        coord[1][2].overdraw_y = -1;
        coord[1][3].x = disp_width;
        coord[1][3].y = disp_height;
        coord[1][3].overdraw_x = +1;
        coord[1][3].overdraw_y = +1;
        coord[1][4].x = 0;
        coord[1][4].y = disp_height;
        coord[1][4].overdraw_x = -1;
        coord[1][4].overdraw_y = +1;
        nverts[1] = 5;

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
