/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#ifndef _RDP_H_
#define _RDP_H_

#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_common.h"
#include "m64p_plugin.h"
#include "m64p_vidext.h"
#include "m64p_config.h"

#define LSB_FIRST 1 // TODO : check for platform
#ifdef LSB_FIRST
#define BYTE_ADDR_XOR          3
#define WORD_ADDR_XOR          1
#define BYTE4_XOR_BE(a)        ((a) ^ 3)                               /* read/write a byte to a 32-bit space */
#else
#define BYTE_ADDR_XOR          0
#define WORD_ADDR_XOR          0
#define BYTE4_XOR_BE(a)        (a)
#endif



#define RDP_PIXEL_SIZE_4BIT         0
#define RDP_PIXEL_SIZE_8BIT         1
#define RDP_PIXEL_SIZE_16BIT        2
#define RDP_PIXEL_SIZE_32BIT        3

#define RDP_FORMAT_RGBA 0
#define RDP_FORMAT_YUV  1
#define RDP_FORMAT_CI   2
#define RDP_FORMAT_IA   3
#define RDP_FORMAT_I    4

#define RDP_CYCLE_TYPE_1            0
#define RDP_CYCLE_TYPE_2            1
#define RDP_CYCLE_TYPE_COPY         2
#define RDP_CYCLE_TYPE_FILL         3

typedef uint32_t rdpColor_t;

#define RDP_GETC32_R(c) ( ((c)>>24) & 0xff )
#define RDP_GETC32_G(c) ( ((c)>>16) & 0xff )
#define RDP_GETC32_B(c) ( ((c)>> 8) & 0xff )
#define RDP_GETC32_A(c) ( ((c)>> 0) & 0xff )

#define RDP_GETC16_R(c) ( ((c)>>11) & 0x1f )
#define RDP_GETC16_G(c) ( ((c)>> 6) & 0x1f )
#define RDP_GETC16_B(c) ( ((c)>> 1) & 0x1f )
#define RDP_GETC16_A(c) ( ((c)>> 0) & 0x1 )

struct rdpRect_t {
    uint16_t xl, yl, xh, yh;            // 10.2 fixed-point
};

struct rdpTexRect_t {
    int tilenum;
    uint16_t xl, yl, xh, yh;            // 10.2 fixed-point
    int16_t s, t;                       // 10.5 fixed-point
    int16_t dsdx, dtdy;                 // 5.10 fixed-point
};

extern const char *rdpImageFormats[];

// TODO put ct ... palette in a bitfield
struct rdpTile_t {
    uint16_t line;
    uint16_t tmem;
    uint16_t sl, tl, sh, th;            // 10.2 fixed-point
    uint16_t w, h;
    int8_t format, size;
    int8_t mask_t, shift_t, mask_s, shift_s;
    int8_t ct, mt, cs, ms;
    int8_t palette;
};

struct rdpCombineModes_t {
    uint32_t w1, w2;
};

#define RDP_GETCM_SUB_A_RGB0(cm)        (((cm).w1 >> 20) & 0xf)
#define RDP_GETCM_MUL_RGB0(cm)          (((cm).w1 >> 15) & 0x1f)
#define RDP_GETCM_SUB_A_A0(cm)          (((cm).w1 >> 12) & 0x7)
#define RDP_GETCM_MUL_A0(cm)            (((cm).w1 >>  9) & 0x7)
#define RDP_GETCM_SUB_A_RGB1(cm)        (((cm).w1 >>  5) & 0xf)
#define RDP_GETCM_MUL_RGB1(cm)          (((cm).w1 >>  0) & 0x1f)

#define RDP_GETCM_SUB_B_RGB0(cm)        (((cm).w2 >> 28) & 0xf)
#define RDP_GETCM_SUB_B_RGB1(cm)        (((cm).w2 >> 24) & 0xf)
#define RDP_GETCM_SUB_A_A1(cm)          (((cm).w2 >> 21) & 0x7)
#define RDP_GETCM_MUL_A1(cm)            (((cm).w2 >> 18) & 0x7)
#define RDP_GETCM_ADD_RGB0(cm)          (((cm).w2 >> 15) & 0x7)
#define RDP_GETCM_SUB_B_A0(cm)          (((cm).w2 >> 12) & 0x7)
#define RDP_GETCM_ADD_A0(cm)            (((cm).w2 >>  9) & 0x7)
#define RDP_GETCM_ADD_RGB1(cm)          (((cm).w2 >>  6) & 0x7)
#define RDP_GETCM_SUB_B_A1(cm)          (((cm).w2 >>  3) & 0x7)
#define RDP_GETCM_ADD_A1(cm)            (((cm).w2 >>  0) & 0x7)

#define RDP_COMBINE_MASK11 ((0xfu<<20)|(0x1fu<<15)|(0x7u<<12)|(0x7u<<9))
#define RDP_COMBINE_MASK12 ((0xfu<<28)|(0x7u<<15)|(0x7u<<12)|(0x7u<<9))
#define RDP_COMBINE_MASK21 ((0xfu<<5)|(0x1fu<<0))
#define RDP_COMBINE_MASK22 ((0xfu<<24)|(0x7u<<21)|(0x7u<<18)|(0x7u<<6)|(0x7u<<3)|(0x7u<<0))

static const rdpCombineModes_t rdpCombineMasks[4] = {
    { ~RDP_COMBINE_MASK21, ~RDP_COMBINE_MASK22 },
    { ~0u, ~0u },
    { ~(RDP_COMBINE_MASK11|RDP_COMBINE_MASK21), ~(RDP_COMBINE_MASK12|RDP_COMBINE_MASK22) },
    { ~(RDP_COMBINE_MASK11|RDP_COMBINE_MASK21), ~(RDP_COMBINE_MASK12|RDP_COMBINE_MASK22) },
};

struct rdpOtherModes_t {
    uint32_t w1, w2;
};

#define RDP_OM_MISSING1 (~((3<<20)|0x80000|0x40000|0x20000|0x10000|0x08000| \
    0x04000|0x02000|0x01000|0x00800|0x00400|0x00200| \
    0x00100|(3<<6)|(3<<4)))
#define RDP_OM_MISSING2 (~(0xffff0000|0x4000|0x2000|0x1000|(3<<10)|(3<<8)| \
    0x80|0x40|0x20|0x10|0x08|0x04|0x02|0x01))

#define RDP_GETOM_CYCLE_TYPE(om)                (((om).w1 >> 20) & 0x3)
#define RDP_GETOM_PERSP_TEX_EN(om)              (((om).w1 & 0x80000) ? 1 : 0)
#define RDP_GETOM_DETAIL_TEX_EN(om)             (((om).w1 & 0x40000) ? 1 : 0)
#define RDP_GETOM_SHARPEN_TEX_EN(om)            (((om).w1 & 0x20000) ? 1 : 0)
#define RDP_GETOM_TEX_LOD_EN(om)                (((om).w1 & 0x10000) ? 1 : 0)
#define RDP_GETOM_EN_TLUT(om)                   (((om).w1 & 0x08000) ? 1 : 0)
#define RDP_GETOM_TLUT_TYPE(om)                 (((om).w1 & 0x04000) ? 1 : 0)
#define RDP_GETOM_SAMPLE_TYPE(om)               (((om).w1 & 0x02000) ? 1 : 0)
#define RDP_GETOM_MID_TEXEL(om)                 (((om).w1 & 0x01000) ? 1 : 0)
#define RDP_GETOM_BI_LERP0(om)                  (((om).w1 & 0x00800) ? 1 : 0)
#define RDP_GETOM_BI_LERP1(om)                  (((om).w1 & 0x00400) ? 1 : 0)
#define RDP_GETOM_CONVERT_ONE(om)               (((om).w1 & 0x00200) ? 1 : 0)
#define RDP_GETOM_KEY_EN(om)                    (((om).w1 & 0x00100) ? 1 : 0)
#define RDP_GETOM_RGB_DITHER_SEL(om)            (((om).w1 >> 6) & 0x3)
#define RDP_GETOM_ALPHA_DITHER_SEL(om)          (((om).w1 >> 4) & 0x3)
#define RDP_GETOM_BLEND_M1A_0(om)               (((om).w2 >> 30) & 0x3)
#define RDP_GETOM_BLEND_M1A_1(om)               (((om).w2 >> 28) & 0x3)
#define RDP_GETOM_BLEND_M1B_0(om)               (((om).w2 >> 26) & 0x3)
#define RDP_GETOM_BLEND_M1B_1(om)               (((om).w2 >> 24) & 0x3)
#define RDP_GETOM_BLEND_M2A_0(om)               (((om).w2 >> 22) & 0x3)
#define RDP_GETOM_BLEND_M2A_1(om)               (((om).w2 >> 20) & 0x3)
#define RDP_GETOM_BLEND_M2B_0(om)               (((om).w2 >> 18) & 0x3)
#define RDP_GETOM_BLEND_M2B_1(om)               (((om).w2 >> 16) & 0x3)
#define RDP_GETOM_FORCE_BLEND(om)               (((om).w2 & 0x4000) ? 1 : 0)
#define RDP_GETOM_ALPHA_CVG_SELECT(om)          (((om).w2 & 0x2000) ? 1 : 0)
#define RDP_GETOM_CVG_TIMES_ALPHA(om)           (((om).w2 & 0x1000) ? 1 : 0)
#define RDP_GETOM_Z_MODE(om)                    (((om).w2 >> 10) & 0x3)
#define RDP_GETOM_CVG_DEST(om)                  (((om).w2 >> 8) & 0x3)
#define RDP_GETOM_COLOR_ON_CVG(om)              (((om).w2 & 0x80) ? 1 : 0)
#define RDP_GETOM_IMAGE_READ_EN(om)             (((om).w2 & 0x40) ? 1 : 0)
#define RDP_GETOM_Z_UPDATE_EN(om)               (((om).w2 & 0x20) ? 1 : 0)
#define RDP_GETOM_Z_COMPARE_EN(om)              (((om).w2 & 0x10) ? 1 : 0)
#define RDP_GETOM_ANTIALIAS_EN(om)              (((om).w2 & 0x08) ? 1 : 0)
#define RDP_GETOM_Z_SOURCE_SEL(om)              (((om).w2 & 0x04) ? 1 : 0)
#define RDP_GETOM_DITHER_ALPHA_EN(om)           (((om).w2 & 0x02) ? 1 : 0)
#define RDP_GETOM_ALPHA_COMPARE_EN(om)          (((om).w2 & 0x01) ? 1 : 0)

#define RDP_BLEND_MASK1 ((3u<<30)|(3u<<26)|(3u<<22)|(3u<<18))
#define RDP_BLEND_MASK2 ((3u<<28)|(3u<<24)|(3u<<20)|(3u<<16))

static const rdpOtherModes_t rdpBlendMasks[4] = {
    { ~0u, ~RDP_BLEND_MASK2 },
    { ~0u, ~0u },
    { ~0u, ~(RDP_BLEND_MASK1|RDP_BLEND_MASK2) },
    { ~0u, ~(RDP_BLEND_MASK1|RDP_BLEND_MASK2) },
};

struct rdpState_t {
    rdpCombineModes_t combineModes;
    rdpOtherModes_t   otherModes;
    rdpColor_t        blendColor;
    rdpColor_t        primColor;
    rdpColor_t        envColor;
    rdpColor_t        fogColor;
    rdpColor_t        fillColor;
    int               primitiveZ;
    int               primitiveDeltaZ;
    rdpRect_t         clip;
    uint8_t           k5, clipMode;
};

extern rdpState_t rdpState;
extern uint32_t   rdpChanged;
//extern rdpColor_t rdpTlut[];
#define rdpTlut ((uint16_t *) (rdpTmem + 0x800))
extern uint8_t    rdpTmem[];
extern int        rdpFbFormat;
extern int        rdpFbSize;
extern int        rdpFbWidth;
extern uint32_t   rdpFbAddress;
extern uint32_t   rdpZbAddress;
extern int        rdpTiFormat;
extern int        rdpTiSize;
extern int        rdpTiWidth;
extern uint32_t   rdpTiAddress;
extern rdpTile_t  rdpTiles[8];
extern int        rdpTileSet;

#define RDP_BITS_COMBINE_MODES (1<<0)
#define RDP_BITS_OTHER_MODES   (1<<1)
#define RDP_BITS_CLIP          (1<<2)
#define RDP_BITS_BLEND_COLOR   (1<<3)
#define RDP_BITS_PRIM_COLOR    (1<<4)
#define RDP_BITS_ENV_COLOR     (1<<5)
#define RDP_BITS_FOG_COLOR     (1<<6)
#define RDP_BITS_FB_SETTINGS   (1<<7)
#define RDP_BITS_ZB_SETTINGS   (1<<8)
#define RDP_BITS_TI_SETTINGS   (1<<9)
#define RDP_BITS_TMEM          (1<<10)
#define RDP_BITS_TLUT          (1<<11)
#define RDP_BITS_TILE_SETTINGS (1<<12)
#define RDP_BITS_FILL_COLOR    (1<<13)
#define RDP_BITS_MISC          (1<<14)

// return where the data in rdram came from at this address in tmem
uint32_t rdpGetTmemOrigin(int tmem, int * line, int * stop, int * fromFormat, int * size);


int rdp_init();
int rdp_dasm(uint32_t * rdp_cmd_data, int rdp_cmd_cur, int length, char *buffer);
void rdp_process_list(void);
int rdp_store_list(void);

void rdp_log(m64p_msg_level level, const char *msg, ...);

#ifdef RDP_DEBUG

extern uint32_t rdpTraceBuf[];
extern int rdpTracePos;

extern int rdp_dump;

#define DUMP if (!rdp_dump) ; else LOG

static void LOG(const char * s, ...)
{
    va_list ap;
    va_start(ap, s);
    vfprintf(stderr, s, ap);
    va_end(ap);
}
#define LOGERROR LOG

#else // RDP_DEBUG

#define DUMP(...) rdp_log(M64MSG_VERBOSE, __VA_ARGS__)
#define LOG(...) rdp_log(M64MSG_VERBOSE, __VA_ARGS__)
#define LOGERROR(...) rdp_log(M64MSG_WARNING, __VA_ARGS__)

#endif // RDP_DEBUG


#endif // _RDP_H_
