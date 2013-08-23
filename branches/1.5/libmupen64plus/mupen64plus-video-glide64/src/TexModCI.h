/*
*   Glide64 - Glide video plugin for Nintendo 64 emulators.
*   Copyright (c) 2002  Dave2001
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public
*   License along with this program; if not, write to the Free
*   Software Foundation, Inc., 51 Franklin Street, Fifth Floor, 
*   Boston, MA  02110-1301, USA
*/

//****************************************************************
//
// Glide64 - Glide Plugin for Nintendo 64 emulators (tested mostly with Project64)
// Project started on December 29th, 2001
//
// To modify Glide64:
// * Write your name and (optional)email, commented by your work, so I know who did it, and so that you can find which parts you modified when it comes time to send it to me.
// * Do NOT send me the whole project or file that you modified.  Take out your modified code sections, and tell me where to put them.  If people sent the whole thing, I would have many different versions, but no idea how to combine them all.
//
// Official Glide64 development channel: #Glide64 on EFnet
//
// Original author: Dave2001 (Dave2999@hotmail.com)
// Other authors: Gonetz, Gugaman
//
//****************************************************************

#ifndef _WIN32
#define min(a,b) ((a) < (b) ? (a) : (b))
#define max(a,b) ((a) > (b) ? (a) : (b))
#endif // _WIN32

static void mod_tex_inter_color_using_factor_CI (DWORD color, DWORD factor)
{
    float percent = factor / 255.0f;
    float percent_i = 1 - percent;
    BYTE cr, cg, cb;
    WORD col;
    BYTE a, r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = (BYTE)(min(255, percent_i * r + percent * cr));
        g = (BYTE)(min(255, percent_i * g + percent * cg));
        b = (BYTE)(min(255, percent_i * b + percent * cb));
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_tex_inter_col_using_col1_CI (DWORD color0, DWORD color1)
{
    BYTE cr, cg, cb;
    WORD col;
    BYTE a, r, g, b;

    float percent_r = ((color1 >> 24) & 0xFF) / 255.0f;
    float percent_g = ((color1 >> 16) & 0xFF) / 255.0f;
    float percent_b = ((color1 >> 8)  & 0xFF) / 255.0f;
    float percent_r_i = 1.0f - percent_r;
    float percent_g_i = 1.0f - percent_g;
    float percent_b_i = 1.0f - percent_b;

    cr = (BYTE)((color0 >> 24) & 0xFF);
    cg = (BYTE)((color0 >> 16) & 0xFF);
    cb = (BYTE)((color0 >> 8)  & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = (BYTE)(min(255, percent_r_i * r + percent_r * cr));
        g = (BYTE)(min(255, percent_g_i * g + percent_g * cg));
        b = (BYTE)(min(255, percent_b_i * b + percent_b * cb));
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_full_color_sub_tex_CI (DWORD color)
{
    BYTE cr, cg, cb, ca;
    WORD col;
    BYTE a, r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);
    ca = (BYTE)(color & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        a = max(0, ca - a);
        r = max(0, cr - r);
        g = max(0, cg - g);
        b = max(0, cb - b);
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_col_inter_col1_using_tex_CI (DWORD color0, DWORD color1)
{
    DWORD cr0, cg0, cb0, cr1, cg1, cb1;
    WORD col;
    BYTE a, r, g, b;
    float percent_r, percent_g, percent_b;

    cr0 = (BYTE)((color0 >> 24) & 0xFF);
    cg0 = (BYTE)((color0 >> 16) & 0xFF);
    cb0 = (BYTE)((color0 >> 8)  & 0xFF);
    cr1 = (BYTE)((color1 >> 24) & 0xFF);
    cg1 = (BYTE)((color1 >> 16) & 0xFF);
    cb1 = (BYTE)((color1 >> 8)  & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        percent_r = ((col&0xF800) >> 11) / 31.0f;
        percent_g = ((col&0x07C0) >> 6) / 31.0f;
        percent_b = ((col&0x003E) >> 1) / 31.0f;
        r = (BYTE)(min((1.0f-percent_r) * cr0 + percent_r * cr1, 255));
        g = (BYTE)(min((1.0f-percent_g) * cg0 + percent_g * cg1, 255));
        b = (BYTE)(min((1.0f-percent_b) * cb0 + percent_b * cb1, 255));
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}



static void mod_tex_sub_col_mul_fac_add_tex_CI (DWORD color, DWORD factor)
{
    float percent = factor / 255.0f;
    BYTE cr, cg, cb, a;
    WORD col;
    float r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = (r - cr) * percent + r;
        if (r > 255.0f) r = 255.0f;
        if (r < 0.0f) r = 0.0f;
        g = (g - cg) * percent + g;
        if (g > 255.0f) g = 255.0f;
        if (g < 0.0f) g = 0.0f;
        b = (b - cb) * percent + b;
        if (b > 255.0f) g = 255.0f;
        if (b < 0.0f) b = 0.0f;
        rdp.pal_8[i] = (WORD)(((WORD)((BYTE)(r) >> 3) << 11) |
                  ((WORD)((BYTE)(g) >> 3) << 6) |
                  ((WORD)((BYTE)(b) >> 3) << 1) |
                  (WORD)(a) );
    }
}

static void mod_tex_scale_col_add_col_CI (DWORD color, DWORD factor)
{
    float percent = factor / 255.0f;
    float percent_r = (1.0f - ((color >> 24) & 0xFF) / 255.0f) * percent;
    float percent_g = (1.0f - ((color >> 16) & 0xFF) / 255.0f) * percent;
    float percent_b = (1.0f - ((color >> 8)  & 0xFF) / 255.0f) * percent;
    WORD col;
    float base = (1.0f - percent) * 255.0f;
    BYTE a, r, g, b;

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = (BYTE)(min(base + percent_r * r, 255));
        g = (BYTE)(min(base + percent_g * g, 255));
        b = (BYTE)(min(base + percent_b * b, 255));
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  (WORD)(a) );
    }
}


static void mod_tex_add_col_CI (DWORD color)
{
    BYTE cr, cg, cb;
    WORD col;
    BYTE a, r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = min(cr + r, 255);
        g = min(cg + g, 255);
        b = min(cb + b, 255);
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_tex_sub_col_CI (DWORD color)
{
    BYTE cr, cg, cb;
    WORD col;
    BYTE a, r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = max(r - cr, 0);
        g = max(g - cg, 0);
        b = max(b - cb, 0);
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_tex_sub_col_mul_fac_CI (DWORD color, DWORD factor)
{
    float percent = factor / 255.0f;
    BYTE cr, cg, cb;
    WORD col;
    BYTE a;
    float r, g, b;

    cr = (BYTE)((color >> 24) & 0xFF);
    cg = (BYTE)((color >> 16) & 0xFF);
    cb = (BYTE)((color >> 8) & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);
        r = (float)((col&0xF800) >> 11) / 31.0f * 255.0f;
        g = (float)((col&0x07C0) >> 6) / 31.0f * 255.0f;
        b = (float)((col&0x003E) >> 1) / 31.0f * 255.0f;
        r = (r - cr) * percent;
        if (r > 255.0f) r = 255.0f;
        if (r < 0.0f) r = 0.0f;
        g = (g - cg) * percent;
        if (g > 255.0f) g = 255.0f;
        if (g < 0.0f) g = 0.0f;
        b = (b - cb) * percent;
        if (b > 255.0f) g = 255.0f;
        if (b < 0.0f) b = 0.0f;

        rdp.pal_8[i] = (WORD)(((WORD)((BYTE)(r) >> 3) << 11) |
                  ((WORD)((BYTE)(g) >> 3) << 6) |
                  ((WORD)((BYTE)(b) >> 3) << 1) |
                  (WORD)(a) );
    }
}

static void mod_col_inter_tex_using_col1_CI (DWORD color0, DWORD color1)
{
    BYTE cr, cg, cb;
    WORD col;
    BYTE a, r, g, b;

    float percent_r = ((color1 >> 24) & 0xFF) / 255.0f;
    float percent_g = ((color1 >> 16) & 0xFF) / 255.0f;
    float percent_b = ((color1 >> 8)  & 0xFF) / 255.0f;
    float percent_r_i = 1.0f - percent_r;
    float percent_g_i = 1.0f - percent_g;
    float percent_b_i = 1.0f - percent_b;

    cr = (BYTE)((color0 >> 24) & 0xFF);
    cg = (BYTE)((color0 >> 16) & 0xFF);
    cb = (BYTE)((color0 >> 8)  & 0xFF);

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) / 31.0f * 255.0f);
        g = (BYTE)((float)((col&0x07C0) >> 6) / 31.0f * 255.0f);
        b = (BYTE)((float)((col&0x003E) >> 1) / 31.0f * 255.0f);
        r = (BYTE)(min(255, percent_r * r + percent_r_i * cr));
        g = (BYTE)(min(255, percent_g * g + percent_g_i * cg));
        b = (BYTE)(min(255, percent_b * b + percent_b_i * cb));
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void mod_tex_inter_col_using_texa_CI (DWORD color)
{
    BYTE a, r, g, b;

    r = (BYTE)((float)((color >> 24) & 0xFF) / 255.0f * 31.0f);
    g = (BYTE)((float)((color >> 16) & 0xFF) / 255.0f * 31.0f);
    b = (BYTE)((float)((color >> 8)  & 0xFF) / 255.0f * 31.0f);
    a = (color&0xFF) ? 1 : 0;
    WORD col16 = (WORD)((r<<11)|(g<<6)|(b<<1)|a);

    for (int i=0; i<256; i++)
    {
        if (rdp.pal_8[i]&1)
          rdp.pal_8[i] = col16;
    }
}

static void mod_tex_mul_col_CI (DWORD color)
{
    BYTE a, r, g, b;
    WORD col;
    float cr, cg, cb;

    cr = (float)((color >> 24) & 0xFF) / 255.0f;
    cg = (float)((color >> 16) & 0xFF) / 255.0f;
    cb = (float)((color >> 8)  & 0xFF) / 255.0f;

    for (int i=0; i<256; i++)
    {
        col = rdp.pal_8[i];
        a = (BYTE)(col&0x0001);;
        r = (BYTE)((float)((col&0xF800) >> 11) * cr);
        g = (BYTE)((float)((col&0x07C0) >> 6) * cg);
        b = (BYTE)((float)((col&0x003E) >> 1) * cb);
        rdp.pal_8[i] = (WORD)(((WORD)(r >> 3) << 11) |
                  ((WORD)(g >> 3) << 6) |
                  ((WORD)(b >> 3) << 1) |
                  ((WORD)(a ) << 0));
    }
}

static void ModifyPalette(DWORD mod, DWORD modcolor, DWORD modcolor1, DWORD modfactor)
{
        switch (mod)
        {
        case TMOD_TEX_INTER_COLOR_USING_FACTOR:
            mod_tex_inter_color_using_factor_CI (modcolor, modfactor);
            break;
        case TMOD_TEX_INTER_COL_USING_COL1:
            mod_tex_inter_col_using_col1_CI (modcolor, modcolor1);
            break;
        case TMOD_FULL_COLOR_SUB_TEX:
            mod_full_color_sub_tex_CI (modcolor);
            break;
        case TMOD_COL_INTER_COL1_USING_TEX:
            mod_col_inter_col1_using_tex_CI (modcolor, modcolor1);
            break;
        case TMOD_TEX_SUB_COL_MUL_FAC_ADD_TEX:
            mod_tex_sub_col_mul_fac_add_tex_CI (modcolor, modfactor);
            break;
        case TMOD_TEX_SCALE_COL_ADD_COL:
            mod_tex_scale_col_add_col_CI (modcolor, modfactor);
            break;
        case TMOD_TEX_ADD_COL:
            mod_tex_add_col_CI (modcolor);
            break;
        case TMOD_TEX_SUB_COL:
            mod_tex_sub_col_CI (modcolor);
            break;
        case TMOD_TEX_SUB_COL_MUL_FAC:
            mod_tex_sub_col_mul_fac_CI (modcolor, modfactor);
            break;
        case TMOD_COL_INTER_TEX_USING_COL1:
            mod_col_inter_tex_using_col1_CI (modcolor, modcolor1);
            break;
        case TMOD_TEX_INTER_COL_USING_TEXA:
            mod_tex_inter_col_using_texa_CI (modcolor);
            break;
        case TMOD_TEX_MUL_COL:
            mod_tex_mul_col_CI (modcolor);
            break;
        default:
            ;
       }
}

