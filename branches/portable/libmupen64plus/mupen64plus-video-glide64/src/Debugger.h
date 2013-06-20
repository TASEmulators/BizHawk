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
*   Licence along with this program; if not, write to the Free
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

#define SELECTED_NONE   0x00000000
#define SELECTED_TRI    0x00000001
#define SELECTED_TEX    0x00000002

typedef struct TEX_INFO_t
{
    DWORD cur_cache[2]; // Current cache #
    BYTE format;
    BYTE size;
    DWORD width, height;
    WORD line, wid;
    BYTE palette;
    BYTE clamp_s, clamp_t;
    BYTE mirror_s, mirror_t;
    BYTE mask_s, mask_t;
    BYTE shift_s, shift_t;
    WORD ul_s, ul_t, lr_s, lr_t;
    WORD t_ul_s, t_ul_t, t_lr_s, t_lr_t;
    float scale_s, scale_t;
    int tmu;
} TEX_INFO;

typedef struct TRI_INFO_t
{
    DWORD   nv;         // Number of vertices
    VERTEX  *v;         // Vertices (2d screen coords) of the triangle, used to outline
    DWORD   cycle1, cycle2, cycle_mode; // Combine mode at the time of rendering
    BYTE    uncombined; // which is uncombined: 0x01=color 0x02=alpha 0x03=both
    DWORD   geom_mode;  // geometry mode flags
    DWORD   othermode_h;    // setothermode_h flags
    DWORD   othermode_l;    // setothermode_l flags
    DWORD   tri_n;      // Triangle number
    DWORD   flags;

    int     type;   // 0-normal, 1-texrect, 2-fillrect

    // texture info
    TEX_INFO t[2];

    // colors
    DWORD fog_color; 
    DWORD fill_color; 
    DWORD prim_color; 
    DWORD blend_color; 
    DWORD env_color; 
    DWORD prim_lodmin, prim_lodfrac;

    TRI_INFO_t  *pNext;
} TRI_INFO;

typedef struct
{
    BOOL capture;   // Capture moment for debugging?

    DWORD selected; // Selected object (see flags above)
    TRI_INFO *tri_sel;

    DWORD tex_scroll;   // texture scrolling
    DWORD tex_sel;

    // CAPTURE INFORMATION
    BYTE *screen;       // Screen capture
    TRI_INFO *tri_list; // Triangle information list
    TRI_INFO *tri_last; // Last in the list (first in)

    DWORD tmu;  // tmu #

    DWORD draw_mode;

    // Page number
    int page;

} DEBUGGER;

#define PAGE_GENERAL    0
#define PAGE_TEX1       1
#define PAGE_TEX2       2
#define PAGE_COLORS     3
#define PAGE_FBL        4
#define PAGE_OTHERMODE_L    5
#define PAGE_OTHERMODE_H    6
#define PAGE_TEXELS     7
#define PAGE_COORDS     8
#define PAGE_TEX_INFO   9

#define TRI_TRIANGLE    0
#define TRI_TEXRECT     1
#define TRI_FILLRECT    2
#define TRI_BACKGROUND  3
#ifdef _WIN32
static char *tri_type[4] = { "TRIANGLE", "TEXRECT", "FILLRECT", "BACKGROUND" };
#endif // _WIN32
extern DEBUGGER debug;

void debug_init ();
void debug_capture ();
void debug_cacheviewer ();
void debug_mouse ();
void debug_keys ();
void output (float x, float y, BOOL scale, const char *fmt, ...);

