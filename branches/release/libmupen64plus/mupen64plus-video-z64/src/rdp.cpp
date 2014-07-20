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

#include "rdp.h"
#include "rgl.h"

#include <string.h>

const char *rdpImageFormats[] =
{ "RGBA", "YUV", "CI", "IA", "I", "???", "???", "???" };

rdpState_t rdpState;
uint32_t   rdpChanged;
//rdpColor_t rdpTlut[1024];
uint8_t    rdpTmem[4*0x1000];
int        rdpFbFormat;
int        rdpFbSize;
int        rdpFbWidth;
uint32_t   rdpFbAddress;
uint32_t   rdpZbAddress;
int        rdpTiFormat;
int        rdpTiSize;
int        rdpTiWidth;
uint32_t   rdpTiAddress;
rdpTile_t  rdpTiles[8];
int        rdpTileSet;

struct area_t {
    int start, stop;
    uint32_t from;
    int fromLine, fromFormat, fromSize;
};

#define MAX_TMEM_AREAS 16
static area_t tmemAreas[MAX_TMEM_AREAS];
static int nbTmemAreas;

#ifdef RDP_DEBUG
int rdp_dump;
#endif

#define MAXCMD 0x100000
static uint32_t rdp_cmd_data[MAXCMD+44];
static volatile int rdp_cmd_ptr = 0;
static volatile int rdp_cmd_cur = 0;
static int rdp_cmd_left = 0;

#ifdef RDP_DEBUG
uint32_t rdpTraceBuf[0x100000];
int rdpTracePos;
#endif


static void MarkTmemArea(int start, int stop, uint32_t from, uint32_t fromLine,
                         int fromFormat, int fromSize)
{
    int i;

    // remove areas that intersect
    for (i=0; i<nbTmemAreas; i++)
        while (i<nbTmemAreas &&
            tmemAreas[i].start<stop && tmemAreas[i].stop>start) {
                memmove(tmemAreas+i, tmemAreas+i+1, nbTmemAreas-i-1);
                nbTmemAreas--;
        }

        DUMP("marking tmem %x --> %x rdram %x\n", start, stop, from);

        // add new area
        //rglAssert(nbTmemAreas < MAX_TMEM_AREAS);
        if (nbTmemAreas == MAX_TMEM_AREAS) {
            LOG("tmem areas buffer full, clearing\n");
            nbTmemAreas = 0;
        }
        tmemAreas[nbTmemAreas].start = start;
        tmemAreas[nbTmemAreas].stop = stop;
        tmemAreas[nbTmemAreas].from = from;
        tmemAreas[nbTmemAreas].fromLine = fromLine;
        tmemAreas[nbTmemAreas].fromFormat = fromFormat;
        tmemAreas[nbTmemAreas].fromSize = fromSize;
        nbTmemAreas++;
}

uint32_t rdpGetTmemOrigin(int tmem, int * line, int * stop, int * format, int * size)
{
    int i;
    for (i=0; i<nbTmemAreas; i++)
        if (tmemAreas[i].start == tmem) {
            *line = tmemAreas[i].fromLine;
            *stop = tmemAreas[i].stop;
            *format = tmemAreas[i].fromFormat;
            *size = tmemAreas[i].fromSize;
            return tmemAreas[i].from;
        }

        return ~0;
}

inline uint32_t READ_RDP_DATA(uint32_t address)
{
    if (dp_status & 0x1)		// XBUS_DMEM_DMA enabled
    {
        return rsp_dmem[(address & 0xfff) / 4];
    }
    else
    {
        return rdram[(address / 4)];
    }
}

static const int rdp_command_length[64] =
{
    8,			// 0x00, No Op
    8,			// 0x01, ???
    8,			// 0x02, ???
    8,			// 0x03, ???
    8,			// 0x04, ???
    8,			// 0x05, ???
    8,			// 0x06, ???
    8,			// 0x07, ???
    32,			// 0x08, Non-Shaded Triangle
    32+16,		// 0x09, Non-Shaded, Z-Buffered Triangle
    32+64,		// 0x0a, Textured Triangle
    32+64+16,	// 0x0b, Textured, Z-Buffered Triangle
    32+64,		// 0x0c, Shaded Triangle
    32+64+16,	// 0x0d, Shaded, Z-Buffered Triangle
    32+64+64,	// 0x0e, Shaded+Textured Triangle
    32+64+64+16,// 0x0f, Shaded+Textured, Z-Buffered Triangle
    8,			// 0x10, ???
    8,			// 0x11, ???
    8,			// 0x12, ???
    8,			// 0x13, ???
    8,			// 0x14, ???
    8,			// 0x15, ???
    8,			// 0x16, ???
    8,			// 0x17, ???
    8,			// 0x18, ???
    8,			// 0x19, ???
    8,			// 0x1a, ???
    8,			// 0x1b, ???
    8,			// 0x1c, ???
    8,			// 0x1d, ???
    8,			// 0x1e, ???
    8,			// 0x1f, ???
    8,			// 0x20, ???
    8,			// 0x21, ???
    8,			// 0x22, ???
    8,			// 0x23, ???
    16,			// 0x24, Texture_Rectangle
    16,			// 0x25, Texture_Rectangle_Flip
    8,			// 0x26, Sync_Load
    8,			// 0x27, Sync_Pipe
    8,			// 0x28, Sync_Tile
    8,			// 0x29, Sync_Full
    8,			// 0x2a, Set_Key_GB
    8,			// 0x2b, Set_Key_R
    8,			// 0x2c, Set_Convert
    8,			// 0x2d, Set_Scissor
    8,			// 0x2e, Set_Prim_Depth
    8,			// 0x2f, Set_Other_Modes
    8,			// 0x30, Load_TLUT
    8,			// 0x31, ???
    8,			// 0x32, Set_Tile_Size
    8,			// 0x33, Load_Block
    8,			// 0x34, Load_Tile
    8,			// 0x35, Set_Tile
    8,			// 0x36, Fill_Rectangle
    8,			// 0x37, Set_Fill_Color
    8,			// 0x38, Set_Fog_Color
    8,			// 0x39, Set_Blend_Color
    8,			// 0x3a, Set_Prim_Color
    8,			// 0x3b, Set_Env_Color
    8,			// 0x3c, Set_Combine
    8,			// 0x3d, Set_Texture_Image
    8,			// 0x3e, Set_Mask_Image
    8			// 0x3f, Set_Color_Image
};

/*****************************************************************************/

////////////////////////
// RDP COMMANDS
////////////////////////

static void rdp_invalid(uint32_t w1, uint32_t w2)
{
    LOGERROR("RDP: invalid command  %d, %08X %08X\n", (w1 >> 24) & 0x3f, w1, w2);
}

static void rdp_noop(uint32_t w1, uint32_t w2)
{

}

static void triangle(uint32_t w1, uint32_t w2, int shade, int texture, int zbuffer)
{
    rglTriangle(w1, w2, shade, texture, zbuffer, rdp_cmd_data + rdp_cmd_cur);
}

static void rdp_tri_noshade(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 0, 0, 0);
}

static void rdp_tri_noshade_z(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 0, 0, 1);
}

static void rdp_tri_tex(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 0, 1, 0);
}

static void rdp_tri_tex_z(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 0, 1, 1);
}

static void rdp_tri_shade(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 1, 0, 0);
}

static void rdp_tri_shade_z(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 1, 0, 1);
}

static void rdp_tri_texshade(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 1, 1, 0);
}

static void rdp_tri_texshade_z(uint32_t w1, uint32_t w2)
{
    triangle(w1, w2, 1, 1, 1);
}

static void rdp_tex_rect(uint32_t w1, uint32_t w2)
{
    uint32_t w3, w4;
    rdpTexRect_t rect;

    w3 = rdp_cmd_data[rdp_cmd_cur+2];
    w4 = rdp_cmd_data[rdp_cmd_cur+3];

    rect.tilenum	= (w2 >> 24) & 0x7;
    rect.xl			= (w1 >> 12) & 0xfff;
    rect.yl			= (w1 >>  0) & 0xfff;
    rect.xh			= (w2 >> 12) & 0xfff;
    rect.yh			= (w2 >>  0) & 0xfff;
    rect.s			= (w3 >> 16) & 0xffff;
    rect.t			= (w3 >>  0) & 0xffff;
    rect.dsdx		= (w4 >> 16) & 0xffff;
    rect.dtdy		= (w4 >>  0) & 0xffff;

    rglTextureRectangle(&rect, 0);
}

static void rdp_tex_rect_flip(uint32_t w1, uint32_t w2)
{
    uint32_t w3, w4;
    rdpTexRect_t rect;

    w3 = rdp_cmd_data[rdp_cmd_cur+2];
    w4 = rdp_cmd_data[rdp_cmd_cur+3];

    rect.tilenum	= (w2 >> 24) & 0x7;
    rect.xl			= (w1 >> 12) & 0xfff;
    rect.yl			= (w1 >>  0) & 0xfff;
    rect.xh			= (w2 >> 12) & 0xfff;
    rect.yh			= (w2 >>  0) & 0xfff;
    rect.t			= (w3 >> 16) & 0xffff;
    rect.s			= (w3 >>  0) & 0xffff;
    rect.dtdy		= (w4 >> 16) & 0xffff;
    rect.dsdx		= (w4 >>  0) & 0xffff;

    rglTextureRectangle(&rect, 1);
}

static void rdp_sync_load(uint32_t w1, uint32_t w2)
{
    // Nothing to do?
}

static void rdp_sync_pipe(uint32_t w1, uint32_t w2)
{
    // Nothing to do?
}

static void rdp_sync_tile(uint32_t w1, uint32_t w2)
{
    // Nothing to do?
}

void rdpSignalFullSync();
void rdpWaitFullSync();
#ifdef RDP_DEBUG
int nbFullSync;
#endif
static void rdp_sync_full(uint32_t w1, uint32_t w2)
{
    //printf("full sync\n");
    rglFullSync();
    rglUpdate();

    if (rglSettings.async)
        rdpSignalFullSync();
    else {
        *gfx.MI_INTR_REG |= 0x20;
        gfx.CheckInterrupts();
    }
#ifdef RDP_DEBUG
    nbFullSync++;
#endif
}

static void rdp_set_key_gb(uint32_t w1, uint32_t w2)
{
    //osd_die("RDP: unhandled command set_key_gb, %08X %08X\n", w1, w2);
}

static void rdp_set_key_r(uint32_t w1, uint32_t w2)
{
    //osd_die("RDP: unhandled command set_key_r, %08X %08X\n", w1, w2);
}

static void rdp_set_convert(uint32_t w1, uint32_t w2)
{
    rdpState.k5 = w2&0xff;
    //osd_die("RDP: unhandled command set_convert, %08X %08X\n", w1, w2);
}

static void rdp_set_scissor(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_CLIP;
    rdpState.clipMode = (w2 >> 24) & 3;
    rdpState.clip.xh = (w1 >> 12) & 0xfff;
    rdpState.clip.yh = (w1 >>  0) & 0xfff;
    rdpState.clip.xl = (w2 >> 12) & 0xfff;
    rdpState.clip.yl = (w2 >>  0) & 0xfff;
    // TODO: handle f & o?
}

static void rdp_set_prim_depth(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_MISC;
    rdpState.primitiveZ = (uint16_t)(w2 >> 16);
    rdpState.primitiveDeltaZ = (uint16_t)(w1);
}

static void rdp_set_other_modes(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_OTHER_MODES;
    rdpState.otherModes.w1 = w1;
    rdpState.otherModes.w2 = w2;
}

static void rdp_load_tlut(uint32_t w1, uint32_t w2)
{
    int tilenum = (w2 >> 24) & 0x7;

    rdpChanged |= RDP_BITS_TILE_SETTINGS;

#define tile rdpTiles[tilenum]
    //rdpTile_t tile;
    tile.sl = (w1 >> 12) & 0xfff;
    tile.tl = (w1 >>  0) & 0xfff;
    tile.sh = (w2 >> 12) & 0xfff;
    tile.th = (w2 >>  0) & 0xfff;

    int i;

    rdpChanged |= RDP_BITS_TLUT;

    int count = ((tile.sh - tile.sl + 4) >>2) * ((tile.th - tile.tl + 4) >>2);

    switch (rdpTiSize)
    {
    case RDP_PIXEL_SIZE_16BIT:
        {
            uint16_t *src = (uint16_t *)&rdram[(rdpTiAddress + (tile.tl >>2) * rdpTiWidth * 2 + ((tile.sl >>2) << rdpTiSize >> 1))/4];
            uint16_t *dst = (uint16_t *)(rdpTmem + rdpTiles[tilenum].tmem);

            //       printf("loading TLUT from %x --> %x\n",
            //              tile.th * rdpTiWidth / 2 + (tile.sh << rdpTiSize >> 1)/4

            for (i=0; i < count; i++)
            {
                dst[i*4] = src[i^1];
            }
            break;
        }
    default:	LOGERROR("RDP: load_tlut: size = %d\n", rdpTiSize);
    }
#undef tile
}

static void rdp_set_tile_size(uint32_t w1, uint32_t w2)
{
    int tilenum = (w2 >> 24) & 0x7;

    rdpChanged |= RDP_BITS_TILE_SETTINGS;

#define tile rdpTiles[tilenum]
    tile.sl = (w1 >> 12) & 0xfff;
    tile.tl = (w1 >>  0) & 0xfff;
    tile.sh = (w2 >> 12) & 0xfff;
    tile.th = (w2 >>  0) & 0xfff;
#undef tile
}

static void rdp_load_block(uint32_t w1, uint32_t w2)
{
    int i, width;
    uint16_t sl, sh, tl, dxt;
    int tilenum = (w2 >> 24) & 0x7;
    uint32_t *src, *tc;
    int tb;

    rdpChanged |= RDP_BITS_TMEM;

    sl	= ((w1 >> 12) & 0xfff);
    tl	= ((w1 >>  0) & 0xfff) << 11;
    sh	= ((w2 >> 12) & 0xfff);
    dxt	= ((w2 >>  0) & 0xfff);

    width = (sh - sl + 1) << rdpTiSize >> 1;

    src = (uint32_t*)&rdram[0];
    tc = (uint32_t*)rdpTmem;
    tb = rdpTiles[tilenum].tmem/4;

    //printf("Load block to %x width %x\n", rdpTiles[tilenum].tmem, width);

    MarkTmemArea(rdpTiles[tilenum].tmem, rdpTiles[tilenum].tmem + width,
        tl * rdpTiWidth*4 + rdpTiAddress + sl*4, 0, ~0, ~0);

    if (tb+width/4 > 0x1000/4) {
        LOG("load_block : fixup too large width\n");
        width = 0x1000-tb*4;
    }

    if (dxt != 0)
    {
        int j=0;

        //rglAssert(tb+width/4 <= 0x1000/4);

        int swap = rdpTiles[tilenum].size == 3? 2 : 1;

        for (i=0; i < width / 4; i+=2)
        {
            int t = j >> 11;

            tc[(((tb+i) + 0)  ^ ((t & 1) ? swap : 0))&0x3ff] =
                src[rdpTiAddress / 4 + ((tl * rdpTiWidth) / 4) + sl + i + 0];
            tc[(((tb+i) + 1) ^ ((t & 1) ? swap : 0))&0x3ff] =
                src[rdpTiAddress / 4 + ((tl * rdpTiWidth) / 4) + sl + i + 1];

            j += dxt;
        }
    }
    else
    {
        //rglAssert(tb+width/4 <= 0x1000/4);
        for (i=0; i < width / 4; i++)
        {
            tc[(tb+i)&0x3ff] = src[((tl * rdpTiWidth) / 4) + rdpTiAddress / 4 + sl + i];
        }
    }
}

static void rdp_load_tile(uint32_t w1, uint32_t w2)
{
    int i, j;
    uint16_t sl, sh, tl, th;
    int width, height;
    int tilenum = (w2 >> 24) & 0x7;
    int line;

    rdpChanged |= RDP_BITS_TMEM;

    sl	= ((w1 >> 12) & 0xfff) / 4;
    tl	= ((w1 >>  0) & 0xfff) / 4;
    sh	= ((w2 >> 12) & 0xfff) / 4;
    th	= ((w2 >>  0) & 0xfff) / 4;

    width = (sh - sl) + 1;
    height = (th - tl) + 1;

    //   printf("Load tile to %x line %x height %d\n",
    //          rdpTiles[tilenum].tmem,
    //          rdpTiles[tilenum].line,
    //          height);

    rdpTiles[tilenum].size = rdpTiSize; // CHECK THIS 
    line = rdpTiles[tilenum].line;
    switch (rdpTiles[tilenum].size /*rdpTiSize*/)
    {
    case RDP_PIXEL_SIZE_8BIT:
        {
            uint8_t *src = (uint8_t*)&rdram[0];
            uint8_t *tc = (uint8_t*)rdpTmem;
            int tb = rdpTiles[tilenum].tmem;

            MarkTmemArea(tb, tb + height*line, rdpTiAddress + tl * rdpTiWidth + sl,
                rdpTiWidth, rdpTiFormat, rdpTiSize);

            if (tb + (line * (height-1) + width) > 4096)
            {
                LOGERROR("rdp_load_tile 8-bit: tmem %04X, width %d, height %d = %d\n", rdpTiles[tilenum].tmem, width, height, width*height);
                height = (4096-tb)/line;
            }

            for (j=0; j < height; j++)
            {
                int tline = tb + (rdpTiles[tilenum].line * j);
                int s = ((j + tl) * rdpTiWidth) + sl;

                for (i=0; i < width; i++)
                {
                    tc[(((tline+i) ^ BYTE_ADDR_XOR) ^ ((j & 1) ? 4 : 0))&0xfff] = src[(rdpTiAddress + s++) ^ BYTE_ADDR_XOR];
                }
            }
            break;
        }
    case RDP_PIXEL_SIZE_16BIT:
        {
            uint16_t *src = (uint16_t*)&rdram[0];
            uint16_t *tc = (uint16_t*)rdpTmem;
            int tb = (rdpTiles[tilenum].tmem / 2);

            if (tb + (line/2 * (height-1) + width) > 2048)
            {
                LOGERROR("rdp_load_tile 16-bit: tmem %04X, width %d, height %d = %d\n", rdpTiles[tilenum].tmem, width, height, width*height);
                height = (2048 - tb) / (line/2);
            }

            MarkTmemArea(tb*2, tb*2 + height*line,
                rdpTiAddress + (tl * rdpTiWidth + sl)*2,
                rdpTiWidth*2, rdpTiFormat, rdpTiSize);

            for (j=0; j < height; j++)
            {
                int tline = tb + ((rdpTiles[tilenum].line / 2) * j);
                int s = ((j + tl) * rdpTiWidth) + sl;

                for (i=0; i < width; i++)
                {
                    tc[(((tline+i) ^ WORD_ADDR_XOR) ^ ((j & 1) ? 2 : 0))&0x7ff] = src[(rdpTiAddress / 2 + s++) ^ WORD_ADDR_XOR];
                }
            }
            break;
        }
    case RDP_PIXEL_SIZE_32BIT:
        {
            uint32_t *src = (uint32_t*)&rdram[0];
            uint32_t *tc = (uint32_t*)rdpTmem;
            int tb = (rdpTiles[tilenum].tmem / 4);

            MarkTmemArea(tb*4, tb*4 + height*line*2,
                rdpTiAddress + (tl * rdpTiWidth + sl)*4,
                rdpTiWidth*4, rdpTiFormat, rdpTiSize);

            if (tb + (line/2 * (height-1) + width) > 1024)
            {
                rdp_log(M64MSG_ERROR, "rdp_load_tile 32-bit: tmem %04X, width %d, height %d = %d\n", rdpTiles[tilenum].tmem, width, height, width*height);
            }

            for (j=0; j < height; j++)
            {
                int tline = tb + ((rdpTiles[tilenum].line / 2) * j);
                int s = ((j + tl) * rdpTiWidth) + sl;

                for (i=0; i < width; i++)
                {
                    tc[((tline+i) ^ ((j & 1) ? 2 : 0))&0x3ff] = src[(rdpTiAddress / 4 + s++)];
                }
            }
            break;
        }

    default:	
        rdp_log(M64MSG_ERROR, "RDP: load_tile: size = %d\n", rdpTiSize);
    }
}

static void rdp_set_tile(uint32_t w1, uint32_t w2)
{
    int tilenum = (w2 >> 24) & 0x7;
    //int i;

    rdpChanged |= RDP_BITS_TILE_SETTINGS;
    rdpTileSet |= 1<<tilenum;

#define tile rdpTiles[tilenum]
    tile.format	= (w1 >> 21) & 0x7;
    tile.size		= (w1 >> 19) & 0x3;
    tile.line		= ((w1 >>  9) & 0x1ff) * 8;
    tile.tmem		= ((w1 >>  0) & 0x1ff) * 8;
    tile.palette= (w2 >> 20) & 0xf;
    tile.ct			= (w2 >> 19) & 0x1;
    tile.mt			= (w2 >> 18) & 0x1;
    tile.mask_t	= (w2 >> 14) & 0xf;
    tile.shift_t= (w2 >> 10) & 0xf;
    if (tile.shift_t >= 12) tile.shift_t -= 16;
    tile.cs			= (w2 >>  9) & 0x1;
    tile.ms			= (w2 >>  8) & 0x1;
    tile.mask_s	= (w2 >>  4) & 0xf;
    tile.shift_s= (w2 >>  0) & 0xf;
    if (tile.shift_s >= 12) tile.shift_s -= 16;
#undef tile
}

static void rdp_fill_rect(uint32_t w1, uint32_t w2)
{
    rdpRect_t rect;
    rect.xl = (w1 >> 12) & 0xfff;
    rect.yl = (w1 >>  0) & 0xfff;
    rect.xh = (w2 >> 12) & 0xfff;
    rect.yh = (w2 >>  0) & 0xfff;

    rglFillRectangle(&rect);
}

static void rdp_set_fill_color(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_FILL_COLOR;
    rdpState.fillColor = w2;
}

static void rdp_set_fog_color(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_FOG_COLOR;
    rdpState.fogColor = w2;
}

static void rdp_set_blend_color(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_BLEND_COLOR;
    rdpState.blendColor = w2;
}

static void rdp_set_prim_color(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_PRIM_COLOR;
    // TODO: prim min level, prim_level
    rdpState.primColor = w2;
}

static void rdp_set_env_color(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_ENV_COLOR;
    rdpState.envColor = w2;
}

static void rdp_set_combine(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_COMBINE_MODES;

    rdpState.combineModes.w1 = w1;
    rdpState.combineModes.w2 = w2;
}

static void rdp_set_texture_image(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_TI_SETTINGS;

    rdpTiFormat	= (w1 >> 21) & 0x7;
    rdpTiSize		= (w1 >> 19) & 0x3;
    rdpTiWidth	= (w1 & 0x3ff) + 1;
    rdpTiAddress	= w2 & 0x01ffffff;
}

static void rdp_set_mask_image(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_ZB_SETTINGS;
    rdpZbAddress	= w2 & 0x01ffffff;
}

static void rdp_set_color_image(uint32_t w1, uint32_t w2)
{
    rdpChanged |= RDP_BITS_FB_SETTINGS;
    rdpFbFormat 	= (w1 >> 21) & 0x7;
    rdpFbSize		= (w1 >> 19) & 0x3;
    rdpFbWidth	= (w1 & 0x3ff) + 1;
    rdpFbAddress	= w2 & 0x01ffffff;
}

/*****************************************************************************/

static void (* rdp_command_table[64])(uint32_t w1, uint32_t w2) =
{
    /* 0x00 */
    rdp_noop,			rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_tri_noshade,	rdp_tri_noshade_z,		rdp_tri_tex,			rdp_tri_tex_z,
    rdp_tri_shade,		rdp_tri_shade_z,		rdp_tri_texshade,		rdp_tri_texshade_z,
    /* 0x10 */
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    /* 0x20 */
    rdp_invalid,		rdp_invalid,			rdp_invalid,			rdp_invalid,
    rdp_tex_rect,		rdp_tex_rect_flip,		rdp_sync_load,			rdp_sync_pipe,
    rdp_sync_tile,		rdp_sync_full,			rdp_set_key_gb,			rdp_set_key_r,
    rdp_set_convert,	rdp_set_scissor,		rdp_set_prim_depth,		rdp_set_other_modes,
    /* 0x30 */
    rdp_load_tlut,		rdp_invalid,			rdp_set_tile_size,		rdp_load_block,
    rdp_load_tile,		rdp_set_tile,			rdp_fill_rect,			rdp_set_fill_color,
    rdp_set_fog_color,	rdp_set_blend_color,	rdp_set_prim_color,		rdp_set_env_color,
    rdp_set_combine,	rdp_set_texture_image,	rdp_set_mask_image,		rdp_set_color_image
};

void rdp_process_list(void)
{
    //int i;
    uint32_t cmd;//, length, cmd_length;

    rglUpdateStatus();
    if (!rglSettings.threaded)
        rdp_store_list();

    if (rglStatus == RGL_STATUS_CLOSED)
        return;

    // this causes problem with depth writeback in zelda mm
    // but is necessary for in fisherman
    rglUpdate();

    while (rdp_cmd_cur != rdp_cmd_ptr)
    {
        cmd = (rdp_cmd_data[rdp_cmd_cur] >> 24) & 0x3f;
        //  if (((rdp_cmd_data[rdp_cmd_cur] >> 24) & 0xc0) != 0xc0)
        //  {
        //      LOGERROR("rdp_process_list: invalid rdp command %08X at %08X\n", rdp_cmd_data[rdp_cmd_cur], dp_start+(rdp_cmd_cur * 4));
        //  }

        if ((((rdp_cmd_ptr-rdp_cmd_cur)&(MAXCMD-1)) * 4) < rdp_command_length[cmd])
        {
            // 			LOGERROR("rdp_process_list: not enough rdp command data: cur = %d, ptr = %d, expected = %d\n", rdp_cmd_cur, rdp_cmd_ptr, rdp_command_length[cmd]);
            // 			return;
            break;
        }

#ifdef RDP_DEBUG
        if (rdp_dump)
        {
            char string[4000];
            int rdp_dasm(uint32_t * rdp_cmd_data, int rdp_cmd_cur, int length, char *buffer);
            rdp_dasm(rdp_cmd_data, rdp_cmd_cur, rdp_command_length[cmd], string);

            fprintf(stderr, "%08X: %08X %08X   %s\n", dp_start+(rdp_cmd_cur * 4), rdp_cmd_data[rdp_cmd_cur+0], rdp_cmd_data[rdp_cmd_cur+1], string);
        }
#endif

#ifdef RDP_DEBUG
        memcpy(rdpTraceBuf+rdpTracePos, rdp_cmd_data+rdp_cmd_cur, rdp_command_length[cmd]);
#endif

        if (rdp_cmd_cur + rdp_command_length[cmd]/4 > MAXCMD)
            memcpy(rdp_cmd_data + MAXCMD, rdp_cmd_data, rdp_command_length[cmd] - (MAXCMD - rdp_cmd_cur)*4);

        // execute the command
        rdp_command_table[cmd](rdp_cmd_data[rdp_cmd_cur+0], rdp_cmd_data[rdp_cmd_cur+1]);

#ifdef RDP_DEBUG
        rdpTracePos += rdp_command_length[cmd] / 4;
        rglAssert(rdpTracePos < sizeof(rdpTraceBuf)/sizeof(rdpTraceBuf[0]));
#endif

        rdp_cmd_cur = (rdp_cmd_cur + rdp_command_length[cmd] / 4) & (MAXCMD-1);
    }

    // 	dp_current = dp_end;
    // 	dp_start = dp_end;
    dp_start = dp_current;

    dp_status &= ~0x0002;
}

int rdp_store_list(void)
{
    uint32_t i;
    uint32_t data, cmd, length;
    int sync = 0;

    //   while (dp_current < dp_end) {

    //   }
    //   dp_status &= ~0x0002;

    length = dp_end - dp_current;

    //   LOG("rdp start %x cur %x end %x length %d dp_status %x\n",
    //       dp_start, dp_current, dp_end,
    //       length, dp_status);

    if (dp_end <= dp_current) {
        return 0;
    }

    // load command data
    for (i=0; i < length; i += 4)
    {
        data = READ_RDP_DATA(dp_current + i);
        if (rglSettings.async) {
            if (rdp_cmd_left) {
                rdp_cmd_left--;
            } else {
                cmd = (data >> 24) & 0x3f;
                rdp_cmd_left = rdp_command_length[cmd]/4-1;
                if (cmd == 0x29) // full_sync
                    sync = 1;
            }
        }
        rdp_cmd_data[rdp_cmd_ptr] = data;
        rdp_cmd_ptr = (rdp_cmd_ptr + 1) & (MAXCMD-1);
    }

    dp_current += length;

    return sync;
}


int rdp_init()
{
    rdp_cmd_cur = rdp_cmd_ptr = 0;
    rdp_cmd_left = 0;
#ifdef RDP_DEBUG
    rdpTracePos = 0;
#endif
    nbTmemAreas = 0;
    return rglInit();
}

