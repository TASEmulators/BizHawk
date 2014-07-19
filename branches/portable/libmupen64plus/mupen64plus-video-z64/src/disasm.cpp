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

#include "z64.h"
#include <stdio.h>

static const char *image_format[] = { "RGBA", "YUV", "CI", "IA", "I", "???", "???", "???" };
static const char *image_size[] = { "4-bit", "8-bit", "16-bit", "32-bit" };

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

int rdp_dasm(UINT32 * rdp_cmd_data, int rdp_cmd_cur, int length, char *buffer)
{
    //int i;
    int tile;
    const char *format, *size;
    char sl[32], tl[32], sh[32], th[32];
    char s[32], t[32];//, w[32];
    char dsdx[32], dtdy[32];
#if 0
    char dsdx[32], dtdx[32], dwdx[32];
    char dsdy[32], dtdy[32], dwdy[32];
    char dsde[32], dtde[32], dwde[32];
    char yl[32], yh[32], ym[32], xl[32], xh[32], xm[32];
    char dxldy[32], dxhdy[32], dxmdy[32];
    char rt[32], gt[32], bt[32], at[32];
    char drdx[32], dgdx[32], dbdx[32], dadx[32];
    char drdy[32], dgdy[32], dbdy[32], dady[32];
    char drde[32], dgde[32], dbde[32], dade[32];
#endif
    UINT32 r,g,b,a;

    UINT32 cmd[64];
    UINT32 command;

    if (length < 8)
    {
        sprintf(buffer, "ERROR: length = %d\n", length);
        return 0;
    }

    cmd[0] = rdp_cmd_data[rdp_cmd_cur+0];
    cmd[1] = rdp_cmd_data[rdp_cmd_cur+1];

    tile = (cmd[1] >> 24) & 0x7;
    sprintf(sl, "%4.2f", (float)((cmd[0] >> 12) & 0xfff) / 4.0f);
    sprintf(tl, "%4.2f", (float)((cmd[0] >>  0) & 0xfff) / 4.0f);
    sprintf(sh, "%4.2f", (float)((cmd[1] >> 12) & 0xfff) / 4.0f);
    sprintf(th, "%4.2f", (float)((cmd[1] >>  0) & 0xfff) / 4.0f);

    format = image_format[(cmd[0] >> 21) & 0x7];
    size = image_size[(cmd[0] >> 19) & 0x3];

    r = (cmd[1] >> 24) & 0xff;
    g = (cmd[1] >> 16) & 0xff;
    b = (cmd[1] >>  8) & 0xff;
    a = (cmd[1] >>  0) & 0xff;

    command = (cmd[0] >> 24) & 0x3f;
    //printf("command %x\n", command);
    switch (command)
    {
    case 0x00:	sprintf(buffer, "No Op"); break;
    case 0x08:
        sprintf(buffer, "Tri_NoShade (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0a:
        sprintf(buffer, "Tri_Tex (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0c:
        sprintf(buffer, "Tri_Shade (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0e:
        sprintf(buffer, "Tri_TexShade (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x09:
        sprintf(buffer, "TriZ_NoShade (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0b:
        sprintf(buffer, "TriZ_Tex (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0d:
        sprintf(buffer, "TriZ_Shade (%08X %08X)", cmd[0], cmd[1]); break;
    case 0x0f:
        sprintf(buffer, "TriZ_TexShade (%08X %08X)", cmd[0], cmd[1]); break;

#if 0
    case 0x08:		// Tri_NoShade
        {
            int lft = (command >> 23) & 0x1;

            if (length < rdp_command_length[command])
            {
                sprintf(buffer, "ERROR: Tri_NoShade length = %d\n", length);
                return 0;
            }

            cmd[2] = rdp_cmd_data[rdp_cmd_cur+2];
            cmd[3] = rdp_cmd_data[rdp_cmd_cur+3];
            cmd[4] = rdp_cmd_data[rdp_cmd_cur+4];
            cmd[5] = rdp_cmd_data[rdp_cmd_cur+5];
            cmd[6] = rdp_cmd_data[rdp_cmd_cur+6];
            cmd[7] = rdp_cmd_data[rdp_cmd_cur+7];

            sprintf(yl,		"%4.4f", (float)((cmd[0] >>  0) & 0x1fff) / 4.0f);
            sprintf(ym,		"%4.4f", (float)((cmd[1] >> 16) & 0x1fff) / 4.0f);
            sprintf(yh,		"%4.4f", (float)((cmd[1] >>  0) & 0x1fff) / 4.0f);
            sprintf(xl,		"%4.4f", (float)(cmd[2] / 65536.0f));
            sprintf(dxldy,	"%4.4f", (float)(cmd[3] / 65536.0f));
            sprintf(xh,		"%4.4f", (float)(cmd[4] / 65536.0f));
            sprintf(dxhdy,	"%4.4f", (float)(cmd[5] / 65536.0f));
            sprintf(xm,		"%4.4f", (float)(cmd[6] / 65536.0f));
            sprintf(dxmdy,	"%4.4f", (float)(cmd[7] / 65536.0f));

            sprintf(buffer, "Tri_NoShade            %d, XL: %s, XM: %s, XH: %s, YL: %s, YM: %s, YH: %s\n", lft, xl,xm,xh,yl,ym,yh);
            break;
        }
    case 0x0a:		// Tri_Tex
        {
            int lft = (command >> 23) & 0x1;

            if (length < rdp_command_length[command])
            {
                sprintf(buffer, "ERROR: Tri_Tex length = %d\n", length);
                return 0;
            }

            for (i=2; i < 24; i++)
            {
                cmd[i] = rdp_cmd_data[rdp_cmd_cur+i];
            }

            sprintf(yl,		"%4.4f", (float)((cmd[0] >>  0) & 0x1fff) / 4.0f);
            sprintf(ym,		"%4.4f", (float)((cmd[1] >> 16) & 0x1fff) / 4.0f);
            sprintf(yh,		"%4.4f", (float)((cmd[1] >>  0) & 0x1fff) / 4.0f);
            sprintf(xl,		"%4.4f", (float)((INT32)cmd[2] / 65536.0f));
            sprintf(dxldy,	"%4.4f", (float)((INT32)cmd[3] / 65536.0f));
            sprintf(xh,		"%4.4f", (float)((INT32)cmd[4] / 65536.0f));
            sprintf(dxhdy,	"%4.4f", (float)((INT32)cmd[5] / 65536.0f));
            sprintf(xm,		"%4.4f", (float)((INT32)cmd[6] / 65536.0f));
            sprintf(dxmdy,	"%4.4f", (float)((INT32)cmd[7] / 65536.0f));

            sprintf(s,		"%4.4f", (float)(INT32)((cmd[ 8] & 0xffff0000) | ((cmd[12] >> 16) & 0xffff)) / 65536.0f);
            sprintf(t,		"%4.4f", (float)(INT32)(((cmd[ 8] & 0xffff) << 16) | (cmd[12] & 0xffff)) / 65536.0f);
            sprintf(w,		"%4.4f", (float)(INT32)((cmd[ 9] & 0xffff0000) | ((cmd[13] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsdx,	"%4.4f", (float)(INT32)((cmd[10] & 0xffff0000) | ((cmd[14] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtdx,	"%4.4f", (float)(INT32)(((cmd[10] & 0xffff) << 16) | (cmd[14] & 0xffff)) / 65536.0f);
            sprintf(dwdx,	"%4.4f", (float)(INT32)((cmd[11] & 0xffff0000) | ((cmd[15] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsde,	"%4.4f", (float)(INT32)((cmd[16] & 0xffff0000) | ((cmd[20] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtde,	"%4.4f", (float)(INT32)(((cmd[16] & 0xffff) << 16) | (cmd[20] & 0xffff)) / 65536.0f);
            sprintf(dwde,	"%4.4f", (float)(INT32)((cmd[17] & 0xffff0000) | ((cmd[21] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsdy,	"%4.4f", (float)(INT32)((cmd[18] & 0xffff0000) | ((cmd[22] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtdy,	"%4.4f", (float)(INT32)(((cmd[18] & 0xffff) << 16) | (cmd[22] & 0xffff)) / 65536.0f);
            sprintf(dwdy,	"%4.4f", (float)(INT32)((cmd[19] & 0xffff0000) | ((cmd[23] >> 16) & 0xffff)) / 65536.0f);


            buffer+=sprintf(buffer, "Tri_Tex               %d, XL: %s, XM: %s, XH: %s, YL: %s, YM: %s, YH: %s\n", lft, xl,xm,xh,yl,ym,yh);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       S: %s, T: %s, W: %s\n", s, t, w);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDX: %s, DTDX: %s, DWDX: %s\n", dsdx, dtdx, dwdx);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDE: %s, DTDE: %s, DWDE: %s\n", dsde, dtde, dwde);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDY: %s, DTDY: %s, DWDY: %s\n", dsdy, dtdy, dwdy);
            break;
        }
    case 0x0c:		// Tri_Shade
        {
            int lft = (command >> 23) & 0x1;

            if (length < rdp_command_length[command])
            {
                sprintf(buffer, "ERROR: Tri_Shade length = %d\n", length);
                return 0;
            }

            for (i=2; i < 24; i++)
            {
                cmd[i] = rdp_cmd_data[i];
            }

            sprintf(yl,		"%4.4f", (float)((cmd[0] >>  0) & 0x1fff) / 4.0f);
            sprintf(ym,		"%4.4f", (float)((cmd[1] >> 16) & 0x1fff) / 4.0f);
            sprintf(yh,		"%4.4f", (float)((cmd[1] >>  0) & 0x1fff) / 4.0f);
            sprintf(xl,		"%4.4f", (float)((INT32)cmd[2] / 65536.0f));
            sprintf(dxldy,	"%4.4f", (float)((INT32)cmd[3] / 65536.0f));
            sprintf(xh,		"%4.4f", (float)((INT32)cmd[4] / 65536.0f));
            sprintf(dxhdy,	"%4.4f", (float)((INT32)cmd[5] / 65536.0f));
            sprintf(xm,		"%4.4f", (float)((INT32)cmd[6] / 65536.0f));
            sprintf(dxmdy,	"%4.4f", (float)((INT32)cmd[7] / 65536.0f));
            sprintf(rt,		"%4.4f", (float)(INT32)((cmd[8] & 0xffff0000) | ((cmd[12] >> 16) & 0xffff)) / 65536.0f);
            sprintf(gt,		"%4.4f", (float)(INT32)(((cmd[8] & 0xffff) << 16) | (cmd[12] & 0xffff)) / 65536.0f);
            sprintf(bt,		"%4.4f", (float)(INT32)((cmd[9] & 0xffff0000) | ((cmd[13] >> 16) & 0xffff)) / 65536.0f);
            sprintf(at,		"%4.4f", (float)(INT32)(((cmd[9] & 0xffff) << 16) | (cmd[13] & 0xffff)) / 65536.0f);
            sprintf(drdx,	"%4.4f", (float)(INT32)((cmd[10] & 0xffff0000) | ((cmd[14] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgdx,	"%4.4f", (float)(INT32)(((cmd[10] & 0xffff) << 16) | (cmd[14] & 0xffff)) / 65536.0f);
            sprintf(dbdx,	"%4.4f", (float)(INT32)((cmd[11] & 0xffff0000) | ((cmd[15] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dadx,	"%4.4f", (float)(INT32)(((cmd[11] & 0xffff) << 16) | (cmd[15] & 0xffff)) / 65536.0f);
            sprintf(drde,	"%4.4f", (float)(INT32)((cmd[16] & 0xffff0000) | ((cmd[20] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgde,	"%4.4f", (float)(INT32)(((cmd[16] & 0xffff) << 16) | (cmd[20] & 0xffff)) / 65536.0f);
            sprintf(dbde,	"%4.4f", (float)(INT32)((cmd[17] & 0xffff0000) | ((cmd[21] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dade,	"%4.4f", (float)(INT32)(((cmd[17] & 0xffff) << 16) | (cmd[21] & 0xffff)) / 65536.0f);
            sprintf(drdy,	"%4.4f", (float)(INT32)((cmd[18] & 0xffff0000) | ((cmd[22] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgdy,	"%4.4f", (float)(INT32)(((cmd[18] & 0xffff) << 16) | (cmd[22] & 0xffff)) / 65536.0f);
            sprintf(dbdy,	"%4.4f", (float)(INT32)((cmd[19] & 0xffff0000) | ((cmd[23] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dady,	"%4.4f", (float)(INT32)(((cmd[19] & 0xffff) << 16) | (cmd[23] & 0xffff)) / 65536.0f);

            buffer+=sprintf(buffer, "Tri_Shade              %d, XL: %s, XM: %s, XH: %s, YL: %s, YM: %s, YH: %s\n", lft, xl,xm,xh,yl,ym,yh);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       R: %s, G: %s, B: %s, A: %s\n", rt, gt, bt, at);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDX: %s, DGDX: %s, DBDX: %s, DADX: %s\n", drdx, dgdx, dbdx, dadx);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDE: %s, DGDE: %s, DBDE: %s, DADE: %s\n", drde, dgde, dbde, dade);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDY: %s, DGDY: %s, DBDY: %s, DADY: %s\n", drdy, dgdy, dbdy, dady);
            break;
        }
    case 0x0e:		// Tri_TexShade
        {
            int lft = (command >> 23) & 0x1;

            if (length < rdp_command_length[command])
            {
                sprintf(buffer, "ERROR: Tri_TexShade length = %d\n", length);
                return 0;
            }

            for (i=2; i < 40; i++)
            {
                cmd[i] = rdp_cmd_data[rdp_cmd_cur+i];
            }

            sprintf(yl,		"%4.4f", (float)((cmd[0] >>  0) & 0x1fff) / 4.0f);
            sprintf(ym,		"%4.4f", (float)((cmd[1] >> 16) & 0x1fff) / 4.0f);
            sprintf(yh,		"%4.4f", (float)((cmd[1] >>  0) & 0x1fff) / 4.0f);
            sprintf(xl,		"%4.4f", (float)((INT32)cmd[2] / 65536.0f));
            sprintf(dxldy,	"%4.4f", (float)((INT32)cmd[3] / 65536.0f));
            sprintf(xh,		"%4.4f", (float)((INT32)cmd[4] / 65536.0f));
            sprintf(dxhdy,	"%4.4f", (float)((INT32)cmd[5] / 65536.0f));
            sprintf(xm,		"%4.4f", (float)((INT32)cmd[6] / 65536.0f));
            sprintf(dxmdy,	"%4.4f", (float)((INT32)cmd[7] / 65536.0f));
            sprintf(rt,		"%4.4f", (float)(INT32)((cmd[8] & 0xffff0000) | ((cmd[12] >> 16) & 0xffff)) / 65536.0f);
            sprintf(gt,		"%4.4f", (float)(INT32)(((cmd[8] & 0xffff) << 16) | (cmd[12] & 0xffff)) / 65536.0f);
            sprintf(bt,		"%4.4f", (float)(INT32)((cmd[9] & 0xffff0000) | ((cmd[13] >> 16) & 0xffff)) / 65536.0f);
            sprintf(at,		"%4.4f", (float)(INT32)(((cmd[9] & 0xffff) << 16) | (cmd[13] & 0xffff)) / 65536.0f);
            sprintf(drdx,	"%4.4f", (float)(INT32)((cmd[10] & 0xffff0000) | ((cmd[14] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgdx,	"%4.4f", (float)(INT32)(((cmd[10] & 0xffff) << 16) | (cmd[14] & 0xffff)) / 65536.0f);
            sprintf(dbdx,	"%4.4f", (float)(INT32)((cmd[11] & 0xffff0000) | ((cmd[15] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dadx,	"%4.4f", (float)(INT32)(((cmd[11] & 0xffff) << 16) | (cmd[15] & 0xffff)) / 65536.0f);
            sprintf(drde,	"%4.4f", (float)(INT32)((cmd[16] & 0xffff0000) | ((cmd[20] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgde,	"%4.4f", (float)(INT32)(((cmd[16] & 0xffff) << 16) | (cmd[20] & 0xffff)) / 65536.0f);
            sprintf(dbde,	"%4.4f", (float)(INT32)((cmd[17] & 0xffff0000) | ((cmd[21] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dade,	"%4.4f", (float)(INT32)(((cmd[17] & 0xffff) << 16) | (cmd[21] & 0xffff)) / 65536.0f);
            sprintf(drdy,	"%4.4f", (float)(INT32)((cmd[18] & 0xffff0000) | ((cmd[22] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dgdy,	"%4.4f", (float)(INT32)(((cmd[18] & 0xffff) << 16) | (cmd[22] & 0xffff)) / 65536.0f);
            sprintf(dbdy,	"%4.4f", (float)(INT32)((cmd[19] & 0xffff0000) | ((cmd[23] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dady,	"%4.4f", (float)(INT32)(((cmd[19] & 0xffff) << 16) | (cmd[23] & 0xffff)) / 65536.0f);

            sprintf(s,		"%4.4f", (float)(INT32)((cmd[24] & 0xffff0000) | ((cmd[28] >> 16) & 0xffff)) / 65536.0f);
            sprintf(t,		"%4.4f", (float)(INT32)(((cmd[24] & 0xffff) << 16) | (cmd[28] & 0xffff)) / 65536.0f);
            sprintf(w,		"%4.4f", (float)(INT32)((cmd[25] & 0xffff0000) | ((cmd[29] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsdx,	"%4.4f", (float)(INT32)((cmd[26] & 0xffff0000) | ((cmd[30] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtdx,	"%4.4f", (float)(INT32)(((cmd[26] & 0xffff) << 16) | (cmd[30] & 0xffff)) / 65536.0f);
            sprintf(dwdx,	"%4.4f", (float)(INT32)((cmd[27] & 0xffff0000) | ((cmd[31] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsde,	"%4.4f", (float)(INT32)((cmd[32] & 0xffff0000) | ((cmd[36] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtde,	"%4.4f", (float)(INT32)(((cmd[32] & 0xffff) << 16) | (cmd[36] & 0xffff)) / 65536.0f);
            sprintf(dwde,	"%4.4f", (float)(INT32)((cmd[33] & 0xffff0000) | ((cmd[37] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dsdy,	"%4.4f", (float)(INT32)((cmd[34] & 0xffff0000) | ((cmd[38] >> 16) & 0xffff)) / 65536.0f);
            sprintf(dtdy,	"%4.4f", (float)(INT32)(((cmd[34] & 0xffff) << 16) | (cmd[38] & 0xffff)) / 65536.0f);
            sprintf(dwdy,	"%4.4f", (float)(INT32)((cmd[35] & 0xffff0000) | ((cmd[39] >> 16) & 0xffff)) / 65536.0f);


            buffer+=sprintf(buffer, "Tri_TexShade           %d, XL: %s, XM: %s, XH: %s, YL: %s, YM: %s, YH: %s\n", lft, xl,xm,xh,yl,ym,yh);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       R: %s, G: %s, B: %s, A: %s\n", rt, gt, bt, at);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDX: %s, DGDX: %s, DBDX: %s, DADX: %s\n", drdx, dgdx, dbdx, dadx);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDE: %s, DGDE: %s, DBDE: %s, DADE: %s\n", drde, dgde, dbde, dade);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DRDY: %s, DGDY: %s, DBDY: %s, DADY: %s\n", drdy, dgdy, dbdy, dady);

            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       S: %s, T: %s, W: %s\n", s, t, w);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDX: %s, DTDX: %s, DWDX: %s\n", dsdx, dtdx, dwdx);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDE: %s, DTDE: %s, DWDE: %s\n", dsde, dtde, dwde);
            buffer+=sprintf(buffer, "                              ");
            buffer+=sprintf(buffer, "                       DSDY: %s, DTDY: %s, DWDY: %s\n", dsdy, dtdy, dwdy);
            break;
        }
#endif
    case 0x24:
    case 0x25:
        {
            if (length < 16)
            {
                sprintf(buffer, "ERROR: Texture_Rectangle length = %d\n", length);
                return 0;
            }
            cmd[2] = rdp_cmd_data[rdp_cmd_cur+2];
            cmd[3] = rdp_cmd_data[rdp_cmd_cur+3];
            sprintf(s,    "%4.4f", (float)(INT16)((cmd[2] >> 16) & 0xffff) / 32.0f);
            sprintf(t,    "%4.4f", (float)(INT16)((cmd[2] >>  0) & 0xffff) / 32.0f);
            sprintf(dsdx, "%4.4f", (float)(INT16)((cmd[3] >> 16) & 0xffff) / 1024.0f);
            sprintf(dtdy, "%4.4f", (float)(INT16)((cmd[3] >> 16) & 0xffff) / 1024.0f);

            if (command == 0x24)
                sprintf(buffer, "Texture_Rectangle      %d, %s, %s, %s, %s,  %s, %s, %s, %s", tile, sh, th, sl, tl, s, t, dsdx, dtdy);
            else
                sprintf(buffer, "Texture_Rectangle_Flip %d, %s, %s, %s, %s,  %s, %s, %s, %s", tile, sh, th, sl, tl, s, t, dsdx, dtdy);

            break;
        }
    case 0x26:	sprintf(buffer, "Sync_Load"); break;
    case 0x27:	sprintf(buffer, "Sync_Pipe"); break;
    case 0x28:	sprintf(buffer, "Sync_Tile"); break;
    case 0x29:	sprintf(buffer, "Sync_Full"); break;
    case 0x2d:	sprintf(buffer, "Set_Scissor            %s, %s, %s, %s", sl, tl, sh, th); break;
    case 0x2e:	sprintf(buffer, "Set_Prim_Depth         %04X, %04X", (cmd[1] >> 16) & 0xffff, cmd[1] & 0xffff); break;
    case 0x2f:	sprintf(buffer, "Set_Other_Modes        %08X %08X", cmd[0], cmd[1]); break;
    case 0x30:	sprintf(buffer, "Load_TLUT              %d, %s, %s, %s, %s", tile, sl, tl, sh, th); break;
    case 0x32:	sprintf(buffer, "Set_Tile_Size          %d, %s, %s, %s, %s", tile, sl, tl, sh, th); break;
    case 0x33:	sprintf(buffer, "Load_Block             %d, %03X, %03X, %03X, %03X", tile, (cmd[0] >> 12) & 0xfff, cmd[0] & 0xfff, (cmd[1] >> 12) & 0xfff, cmd[1] & 0xfff); break;
    case 0x34:	sprintf(buffer, "Load_Tile              %d, %s, %s, %s, %s", tile, sl, tl, sh, th); break;
    case 0x35:	sprintf(buffer, "Set_Tile               %d, %s, %s, %d, %04X", tile, format, size, ((cmd[0] >> 9) & 0x1ff) * 8, (cmd[0] & 0x1ff) * 8); break;
    case 0x36:	sprintf(buffer, "Fill_Rectangle         %s, %s, %s, %s", sh, th, sl, tl); break;
    case 0x37:	sprintf(buffer, "Set_Fill_Color         R: %d, G: %d, B: %d, A: %d", r, g, b, a); break;
    case 0x38:	sprintf(buffer, "Set_Fog_Color          R: %d, G: %d, B: %d, A: %d", r, g, b, a); break;
    case 0x39:	sprintf(buffer, "Set_Blend_Color        R: %d, G: %d, B: %d, A: %d", r, g, b, a); break;
    case 0x3a:	sprintf(buffer, "Set_Prim_Color         %d, %d, R: %d, G: %d, B: %d, A: %d", (cmd[0] >> 8) & 0x1f, cmd[0] & 0xff, r, g, b, a); break;
    case 0x3b:	sprintf(buffer, "Set_Env_Color          R: %d, G: %d, B: %d, A: %d", r, g, b, a); break;
    case 0x3c:	sprintf(buffer, "Set_Combine            %08X %08X", cmd[0], cmd[1]); break;
    case 0x3d:	sprintf(buffer, "Set_Texture_Image      %s, %s, %d, %08X", format, size, (cmd[0] & 0x1ff)+1, cmd[1]); break;
    case 0x3e:	sprintf(buffer, "Set_Mask_Image         %08X", cmd[1]); break;
    case 0x3f:	sprintf(buffer, "Set_Color_Image        %s, %s, %d, %08X", format, size, (cmd[0] & 0x1ff)+1, cmd[1]); break;
    default:	sprintf(buffer, "??? (%08X %08X)", cmd[0], cmd[1]); break;
    }

    return rdp_command_length[command];
}
