/*
*   Glide64 - Glide video plugin for Nintendo 64 emulators.
*   Copyright (c) 2002  Dave2001
*   Copyright (c) 2008  Günther <guenther.emu@freenet.de>
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

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_config.h"
#include "m64p_vidext.h"
#include "TexCache.h"
#include "Combine.h"

void LoadTex (int id, int tmu);

BYTE tex1[512*512*4];       // temporary texture
BYTE tex2[512*512*4];
BYTE *texture;

#include "TexLoad.h"    // texture loading functions, ONLY INCLUDE IN THIS FILE!!!
#include "MiClWr16b.h"  // Mirror/Clamp/Wrap functions, ONLY INCLUDE IN THIS FILE!!!
#include "MiClWr8b.h"   // Mirror/Clamp/Wrap functions, ONLY INCLUDE IN THIS FILE!!!
#include "TexConv.h"    // texture conversions, ONLY INCLUDE IN THIS FILE!!!
#include "TexMod.h"
#include "TexModCI.h"
#include "CRC.h"

#ifndef _WIN32
#include <stdlib.h>
#endif // _WIN32

typedef struct TEXINFO_t {
    int real_image_width, real_image_height;    // FOR ALIGNMENT PURPOSES ONLY!!!
    int tile_width, tile_height;
    int mask_width, mask_height;
    int width, height;
    int wid_64, line;
    DWORD crc;
    DWORD flags;
    int splits, splitheight;
} TEXINFO;

TEXINFO texinfo[2];
int tex_found[2][MAX_TMU];

//****************************************************************
// List functions

typedef struct NODE_t {
    DWORD   crc;
    CACHE_LUT*  data;
    int     tmu;
    int     number;
    NODE_t  *pNext;
} NODE;

NODE *cachelut[256];

void AddToList (NODE **list, DWORD crc, CACHE_LUT* data, int tmu, int number)
{
    NODE *node = new NODE;
    node->crc = crc;
    node->data = data;
    node->tmu = tmu;
    node->number = number;
    node->pNext = *list;
    *list = node;
}

void DeleteList (NODE **list)
{
    while (*list)
    {
        NODE *next = (*list)->pNext;
        delete (*list);
        *list = next;
    }
}

void TexCacheInit ()
{
    for (int i=0; i<256; i++)
    {
        cachelut[i] = NULL;
    }
}

//****************************************************************
// GetTexInfo - gets information for either t0 or t1, checks if in cache & fills tex_found

void GetTexInfo (int id, int tile)
{
    FRDP (" | |-+ GetTexInfo (id: %d, tile: %d)\n", id, tile);

    TEXINFO *info = &texinfo[id];

    int tile_width, tile_height;
    int mask_width, mask_height;
    int width, height;
    int wid_64, line, bpl;

    // Get width and height
    tile_width = rdp.tiles[tile].lr_s - rdp.tiles[tile].ul_s + 1;
    tile_height = rdp.tiles[tile].lr_t - rdp.tiles[tile].ul_t + 1;

    mask_width = (rdp.tiles[tile].mask_s==0)?(tile_width):(1 << rdp.tiles[tile].mask_s);
    mask_height = (rdp.tiles[tile].mask_t==0)?(tile_height):(1 << rdp.tiles[tile].mask_t);

    if (settings.alt_tex_size)
    {
        // ** ALTERNATE TEXTURE SIZE METHOD **
        // Helps speed in some games that loaded weird-sized textures, but could break other
        //  textures.

        // Get the width/height to load
        if ((rdp.tiles[tile].clamp_s && tile_width <= 256) || (mask_width > 256))
        {
            // loading width
            width = min(mask_width, tile_width);
            // actual width
            rdp.tiles[tile].width = tile_width;
        }
        else
        {
            // wrap all the way
            width = min(mask_width, tile_width);    // changed from mask_width only
            rdp.tiles[tile].width = width;
        }

        if ((rdp.tiles[tile].clamp_t && tile_height <= 256) || (mask_height > 256))
        {
            // loading height
            height = min(mask_height, tile_height);
            // actual height
            rdp.tiles[tile].height = tile_height;
        }
        else
        {
            // wrap all the way
            height = min(mask_height, tile_height);
            rdp.tiles[tile].height = height;
        }
    }
    else
    {
        // ** NORMAL TEXTURE SIZE METHOD **
        // This is the 'correct' method for determining texture size, but may cause certain
        //  textures to load too large & make the whole game go slow.

        // Get the width/height to load
        if ((rdp.tiles[tile].clamp_s && tile_width <= 256) || (mask_width > 256))
        {
            // loading width
            width = min(mask_width, tile_width);
            // actual width
            rdp.tiles[tile].width = tile_width;
        }
        else
        {
            // wrap all the way
            width = mask_width;
            rdp.tiles[tile].width = mask_width;
        }

        if ((rdp.tiles[tile].clamp_t && tile_height <= 256) || (mask_height > 256))
        {
            // loading height
            height = min(mask_height, tile_height);
            // actual height
            rdp.tiles[tile].height = tile_height;
        }
        else
        {
            // wrap all the way
            height = mask_height;
            rdp.tiles[tile].height = mask_height;
        }
    }

    // without any large texture fixing-up; for alignment
    int real_image_width = rdp.tiles[tile].width;
    int real_image_height = rdp.tiles[tile].height;
    bpl = width << rdp.tiles[tile].size >> 1;

    // ** COMMENT THIS TO DISABLE LARGE TEXTURES
#ifdef LARGE_TEXTURE_HANDLING
    if (width > 256)
    {
        info->splits = ((width-1)>>8)+1;
        info->splitheight = rdp.tiles[tile].height;
        rdp.tiles[tile].height *= info->splits;
        rdp.tiles[tile].width = 256;
        width = 256;
    }
    else
#endif
    // **
    {
        info->splits = 1;
    }

    RDP (" | | |-+ Texture approved:\n");
    FRDP (" | | | |- tmem: %08lx\n", rdp.tiles[tile].t_mem);
    FRDP (" | | | |- load width: %d\n", width);
    FRDP (" | | | |- load height: %d\n", height);
    FRDP (" | | | |- actual width: %d\n", rdp.tiles[tile].width);
    FRDP (" | | | |- actual height: %d\n", rdp.tiles[tile].height);
    FRDP (" | | | |- size: %d\n", rdp.tiles[tile].size);
    FRDP (" | | | +- format: %d\n", rdp.tiles[tile].format);
    RDP (" | | |- Calculating CRC... ");

    // ** CRC CHECK

  wid_64 = width << (rdp.tiles[tile].size) >> 1;
    if (rdp.tiles[tile].size == 3)
    {
        if (wid_64 & 15) wid_64 += 16;
        wid_64 &= 0xFFFFFFF0;
    }
    else
    {
        if (wid_64 & 7) wid_64 += 8;    // round up
    }
    wid_64 = wid_64>>3;

    // Texture too big for tmem & needs to wrap? (trees in mm)

    if (settings.wrap_big_tex && (rdp.tiles[tile].t_mem + min(height, tile_height) * (rdp.tiles[tile].line<<3) > 4096))
    {
        RDP ("TEXTURE WRAPS TMEM!!! ");

        // calculate the y value that intersects at 4096 bytes
        int y = (4096 - rdp.tiles[tile].t_mem) / (rdp.tiles[tile].line<<3);

        rdp.tiles[tile].clamp_t = 0;
        rdp.tiles[tile].lr_t = rdp.tiles[tile].ul_t + y - 1;

        // calc mask
        int shift;
        for (shift=0; (1<<shift)<y; shift++);
        rdp.tiles[tile].mask_t = shift;

        // restart the function
        RDP ("restarting...\n");
        GetTexInfo (id, tile);
        return;
    }

    line = rdp.tiles[tile].line;
    if (rdp.tiles[tile].size == 3) line <<= 1;
    DWORD crc = 0;
    if (settings.fast_crc || bpl < 2 )
    {
        line = (line - wid_64) << 3;

        if (wid_64 < 1) wid_64 = 1;
        unsigned char * addr = rdp.tmem + (rdp.tiles[tile].t_mem<<3);
        if (height > 0)
        {

        // Check the CRC
#if !defined(__GNUC__) && !defined(NO_ASM)
        __asm {
            xor eax,eax                         // eax is final result
            mov ebx,dword ptr [line]
            mov ecx,dword ptr [height]          // ecx is height counter
            mov edi,dword ptr [addr]            // edi is ptr to texture memory
    crc_loop_y:
            push ecx

            mov ecx,dword ptr [wid_64]
    crc_loop_x:

            add eax,dword ptr [edi]     // MUST be 64-bit aligned, so manually unroll
            add eax,dword ptr [edi+4]
            mov edx,ecx
            mul edx
            add eax,edx
            add edi,8

            dec ecx
            jnz crc_loop_x

            pop ecx

            mov edx,ecx
            mul edx
            add eax,edx

            add edi,ebx

            dec ecx
            jnz crc_loop_y

            mov dword ptr [crc],eax     // store the result
            }
#elif !defined(NO_ASM)
        int i;
        int tempheight = height;
       asm volatile (
             "xor %[crc], %[crc]      \n"                           // eax is final result
             "crc_loop_y:           \n"
             
             "mov %[wid_64], %[i] \n"
             "crc_loop_x:           \n"
             
             "add (%[addr]), %[crc]    \n"      // MUST be 64-bit aligned, so manually unroll
             "add 4(%[addr]), %[crc]   \n"
             "mov %[i], %%edx      \n"
             "mul %%edx             \n" // edx:eax/crc := eax/crc * edx
             "add %%edx, %[crc]      \n"
             "add $8, %[addr]         \n"
             
             "dec %[i]             \n"
             "jnz crc_loop_x        \n"
             
             "mov %[tempheight], %%edx      \n"
             "mul %%edx             \n"
             "add %%edx, %[crc]      \n"
             
             "add %[line], %[addr]      \n"
             
             "dec %[tempheight]             \n"
             "jnz crc_loop_y        \n"
             : [crc] "=&a"(crc), [i] "=&r" (i), [tempheight] "+r"(tempheight), [addr]"+r"(addr)
             : [line] "g" ((intptr_t)line), [wid_64] "g" (wid_64)
             : "memory", "cc", "edx"
             );
#endif
        // ** END CRC CHECK
    }
    }
    else
    {
        crc = 0xFFFFFFFF;
//        unsigned __int64 * addr = (unsigned __int64 *)&rdp.tmem[rdp.tiles[tile].t_mem];
        BYTE * addr = rdp.tmem + (rdp.tiles[tile].t_mem<<3);
        DWORD line2 = max(line,1);
        line2 <<= 3;
        for (int y = 0; y < height; y++)
        {
            crc = CRC_Calculate( crc, (void*)addr, bpl );
            addr += line2;
        }
        line = (line - wid_64) << 3;
        if (wid_64 < 1) wid_64 = 1;
    }
    if ((rdp.tiles[tile].size < 2) && (rdp.tlut_mode != 0))
    {
        if (rdp.tiles[tile].size == 0)
            crc += rdp.pal_8_crc[rdp.tiles[tile].palette];
    else
            crc += rdp.pal_256_crc;
    }

    
    FRDP ("Done.  CRC is: %08lx.\n", crc);

    DWORD flags = (rdp.tiles[tile].clamp_s << 23) | (rdp.tiles[tile].mirror_s << 22) |
        (rdp.tiles[tile].mask_s << 18) | (rdp.tiles[tile].clamp_t << 17) |
        (rdp.tiles[tile].mirror_t << 16) | (rdp.tiles[tile].mask_t << 12);

    info->real_image_width = real_image_width;
    info->real_image_height = real_image_height;
    info->tile_width = tile_width;
    info->tile_height = tile_height;
    info->mask_width = mask_width;
    info->mask_height = mask_height;
    info->width = width;
    info->height = height;
    info->wid_64 = wid_64;
    info->line = line;
    info->crc = crc;
    info->flags = flags;

    // Search the texture cache for this texture
    RDP (" | | |-+ Checking cache...\n");

    int t;
    CACHE_LUT *cache;

    // this is the OLD cache searching, searches ALL textures
/*  for (t=0; t<num_tmu; t++)
    {
        tex_found[id][t] = -1;      // default, overwrite if found
        
        for (i=0; i<rdp.n_cached[t]; i++)
        {
            cache = &rdp.cache[t][i];
            if (crc == cache->crc &&
                //rdp.timg.addr == cache->addr &&       // not totally correct, but will help
                //rdp.addr[rdp.tiles[tile].t_mem] == cache->addr && // more correct
                rdp.tiles[tile].width == cache->width &&
                rdp.tiles[tile].height == cache->height &&
                rdp.tiles[tile].format == cache->format &&
                rdp.tiles[tile].size == cache->size &&
                rdp.tiles[tile].palette == cache->palette &&
                pal_crc == cache->pal_crc &&
                flags == cache->flags)
            {
                FRDP (" | | | |- Texture found in cache (tmu=%d).\n", t);
                tex_found[id][t] = i;
                break;
            }
        }
    }
    for (; t<MAX_TMU; t++)
    {
        tex_found[id][t] = -1;
    }*/

    // this is the NEW cache searching, searches only textures with similar crc's
    for (t=0; t<MAX_TMU; t++)
        tex_found[id][t] = -1;

  if (rdp.noise == noise_texture)
    return;
  
    DWORD mod, modcolor, modcolor1, modcolor2, modfactor;
    if (id == 0)
    {
    mod = cmb.mod_0;
    modcolor = cmb.modcolor_0;
    modcolor1 = cmb.modcolor1_0;
    modcolor2 = cmb.modcolor2_0;
    modfactor = cmb.modfactor_0;
    }
    else
    {
    mod = cmb.mod_1;
    modcolor = cmb.modcolor_1;
    modcolor1 = cmb.modcolor1_1;
    modcolor2 = cmb.modcolor2_1;
    modfactor = cmb.modfactor_1;
    }

    NODE *node = cachelut[crc>>24];
    DWORD mod_mask = (rdp.tiles[tile].format == 2)?0xFFFFFFFF:0xF0F0F0F0;
    while (node)
    {
        if (node->crc == crc)
        {
            cache = (CACHE_LUT*)node->data;
            if (tex_found[id][node->tmu] == -1 &&
                rdp.tiles[tile].width == cache->width &&
                rdp.tiles[tile].height == cache->height &&
                rdp.tiles[tile].format == cache->format &&
                rdp.tiles[tile].size == cache->size &&
                rdp.tiles[tile].palette == cache->palette &&
                flags == cache->flags)
            {
                if (cache->mod == mod &&
                    (cache->mod_color&mod_mask) == (modcolor&mod_mask) &&
                    (cache->mod_color1&mod_mask) == (modcolor1&mod_mask) &&
                    (cache->mod_color2&mod_mask) == (modcolor2&mod_mask) &&
                    abs(static_cast<int>(cache->mod_factor - modfactor)) < 8)
                {
                    FRDP (" | | | |- Texture found in cache (tmu=%d).\n", node->tmu);
                    tex_found[id][node->tmu] = node->number;
//                  if (rdp.addr[rdp.tiles[tile].t_mem] == cache->addr)
//                    return;
                }
            }
        }
        node = node->pNext;
    }
    
    RDP (" | | | +- Done.\n | | +- GetTexInfo end\n");
}

//****************************************************************
// ChooseBestTmu - chooses the best TMU to load to (the one with the most memory)

int ChooseBestTmu (int tmu1, int tmu2)
{
    if (!fullscreen) return tmu1;

    if (tmu1 >= num_tmu) return tmu2;
    if (tmu2 >= num_tmu) return tmu1;

    if (grTexMaxAddress(tmu1)-rdp.tmem_ptr[tmu1] >
        grTexMaxAddress(tmu2)-rdp.tmem_ptr[tmu2])
        return tmu1;
    else
        return tmu2;
}

//****************************************************************
// SelectHiresTex - select texture from texture buffer 

static void SelectHiresTex()
{
  FRDP ("SelectHiresTex: tex: %d, tmu: %d, tile: %d\n", rdp.tex, rdp.hires_tex->tmu, rdp.hires_tex->tile);
  grTexSource( rdp.hires_tex->tmu, rdp.hires_tex->tex_addr, GR_MIPMAPLEVELMASK_BOTH, &(rdp.hires_tex->info) );
  if (rdp.tex == 3 && rdp.hires_tex->tmu == rdp.hires_tex->tile)
    return;
  GrCombineFunction_t color_source = 
    (rdp.hires_tex->info.format == GR_TEXFMT_RGB_565) ? GR_COMBINE_FUNCTION_LOCAL : GR_COMBINE_FUNCTION_LOCAL_ALPHA;
  if (rdp.hires_tex->tmu == GR_TMU0)
  {
    grTexCombine( GR_TMU1, 
      GR_COMBINE_FUNCTION_NONE, 
      GR_COMBINE_FACTOR_NONE, 
      GR_COMBINE_FUNCTION_NONE, 
      GR_COMBINE_FACTOR_NONE, 
      FXFALSE, 
      FXFALSE ); 
    grTexCombine( GR_TMU0, 
      color_source, 
      GR_COMBINE_FACTOR_NONE, 
      GR_COMBINE_FUNCTION_LOCAL, 
      GR_COMBINE_FACTOR_NONE, 
      FXFALSE, 
      FXFALSE); 
  }
  else
  {
    grTexCombine( GR_TMU1, 
      color_source, 
      GR_COMBINE_FACTOR_NONE, 
      GR_COMBINE_FUNCTION_LOCAL, 
      GR_COMBINE_FACTOR_NONE, 
      FXFALSE, 
      FXFALSE); 
    grTexCombine( GR_TMU0, 
      GR_COMBINE_FUNCTION_SCALE_OTHER, 
      GR_COMBINE_FACTOR_ONE, 
      GR_COMBINE_FUNCTION_SCALE_OTHER, 
      GR_COMBINE_FACTOR_ONE, 
      FXFALSE, 
      FXFALSE ); 
  }
}

//****************************************************************
// TexCache - does texture loading after combiner is set

void TexCache ()
{
    RDP (" |-+ TexCache called\n");

    if (rdp.tex & 1)
        GetTexInfo (0, rdp.cur_tile);
    if (rdp.tex & 2)
        GetTexInfo (1, rdp.cur_tile+1);

#define TMUMODE_NORMAL      0
#define TMUMODE_PASSTHRU    1
#define TMUMODE_NONE        2

    int tmu_0, tmu_1;
    int tmu_0_mode=0, tmu_1_mode=0;

    // Select the best TMUs to use (removed 3 tmu support, unnecessary)
    if (rdp.tex == 3)   // T0 and T1
    {
        tmu_0 = 0;
        tmu_1 = 1;
    }
    else if (rdp.tex == 2)  // T1
    {
        if (tex_found[1][0] != -1)  // T1 found in tmu 0
            tmu_1 = 0;
        else if (tex_found[1][1] != -1) // T1 found in tmu 1
            tmu_1 = 1;
        else    // T1 not found
            tmu_1 = ChooseBestTmu (0, 1);

        tmu_0 = !tmu_1;
        tmu_0_mode = (tmu_0==1)?TMUMODE_NONE:TMUMODE_PASSTHRU;
    }
    else if (rdp.tex == 1)  // T0
    {
        if (tex_found[0][0] != -1)  // T0 found in tmu 0
            tmu_0 = 0;
        else if (tex_found[0][1] != -1) // T0 found in tmu 1
            tmu_0 = 1;
        else    // T0 not found
            tmu_0 = ChooseBestTmu (0, 1);

        tmu_1 = !tmu_0;
        tmu_1_mode = (tmu_1==1)?TMUMODE_NONE:TMUMODE_PASSTHRU;
    }
    else    // no texture
    {
        tmu_0 = 0;
        tmu_0_mode = TMUMODE_NONE;
        tmu_1 = 0;
        tmu_1_mode = TMUMODE_NONE;
    }

    FRDP (" | |-+ Modes set:\n | | |- tmu_0 = %d\n | | |- tmu_1 = %d\n",
        tmu_0, tmu_1);
    FRDP (" | | |- tmu_0_mode = %d\n | | |- tmu_1_mode = %d\n",
        tmu_0_mode, tmu_1_mode);

    if (tmu_0_mode == TMUMODE_PASSTHRU) {
    cmb.tmu0_func = cmb.tmu0_a_func = GR_COMBINE_FUNCTION_SCALE_OTHER;
    cmb.tmu0_fac = cmb.tmu0_a_fac = GR_COMBINE_FACTOR_ONE;
    if (cmb.tex_cmb_ext_use)
    {
      cmb.t0c_ext_a = GR_CMBX_OTHER_TEXTURE_RGB;
      cmb.t0c_ext_a_mode = GR_FUNC_MODE_X;
      cmb.t0c_ext_b = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t0c_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t0c_ext_c = GR_CMBX_ZERO;
      cmb.t0c_ext_c_invert = 1;
      cmb.t0c_ext_d = GR_CMBX_ZERO;
      cmb.t0c_ext_d_invert = 0;
      cmb.t0a_ext_a = GR_CMBX_OTHER_TEXTURE_ALPHA;
      cmb.t0a_ext_a_mode = GR_FUNC_MODE_X;
      cmb.t0a_ext_b = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t0a_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t0a_ext_c = GR_CMBX_ZERO;
      cmb.t0a_ext_c_invert = 1;
      cmb.t0a_ext_d = GR_CMBX_ZERO;
      cmb.t0a_ext_d_invert = 0;
    }
    }
    else if (tmu_0_mode == TMUMODE_NONE) {
    cmb.tmu0_func = cmb.tmu0_a_func = GR_COMBINE_FUNCTION_NONE;
    cmb.tmu0_fac = cmb.tmu0_a_fac = GR_COMBINE_FACTOR_NONE;
    if (cmb.tex_cmb_ext_use)
    {
      cmb.t0c_ext_a = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t0c_ext_a_mode = GR_FUNC_MODE_ZERO;
      cmb.t0c_ext_b = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t0c_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t0c_ext_c = GR_CMBX_ZERO;
      cmb.t0c_ext_c_invert = 0;
      cmb.t0c_ext_d = GR_CMBX_ZERO;
      cmb.t0c_ext_d_invert = 0;
      cmb.t0a_ext_a = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t0a_ext_a_mode = GR_FUNC_MODE_ZERO;
      cmb.t0a_ext_b = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t0a_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t0a_ext_c = GR_CMBX_ZERO;
      cmb.t0a_ext_c_invert = 0;
      cmb.t0a_ext_d = GR_CMBX_ZERO;
      cmb.t0a_ext_d_invert = 0;
    }
    }
    if (tmu_1_mode == TMUMODE_PASSTHRU) {
    cmb.tmu1_func = cmb.tmu1_a_func = GR_COMBINE_FUNCTION_SCALE_OTHER;
    cmb.tmu1_fac = cmb.tmu1_a_fac = GR_COMBINE_FACTOR_ONE;
    if (cmb.tex_cmb_ext_use)
    {
      cmb.t1c_ext_a = GR_CMBX_OTHER_TEXTURE_RGB;
      cmb.t1c_ext_a_mode = GR_FUNC_MODE_X;
      cmb.t1c_ext_b = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t1c_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t1c_ext_c = GR_CMBX_ZERO;
      cmb.t1c_ext_c_invert = 1;
      cmb.t1c_ext_d = GR_CMBX_ZERO;
      cmb.t1c_ext_d_invert = 0;
      cmb.t1a_ext_a = GR_CMBX_OTHER_TEXTURE_ALPHA;
      cmb.t1a_ext_a_mode = GR_FUNC_MODE_X;
      cmb.t1a_ext_b = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t1a_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t1a_ext_c = GR_CMBX_ZERO;
      cmb.t1a_ext_c_invert = 1;
      cmb.t1a_ext_d = GR_CMBX_ZERO;
      cmb.t1a_ext_d_invert = 0;
    }
    }
    else if (tmu_1_mode == TMUMODE_NONE) {
    cmb.tmu1_func = cmb.tmu1_a_func = GR_COMBINE_FUNCTION_NONE;
    cmb.tmu1_fac = cmb.tmu1_a_fac = GR_COMBINE_FACTOR_NONE;
    if (cmb.tex_cmb_ext_use)
    {
      cmb.t1c_ext_a = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t1c_ext_a_mode = GR_FUNC_MODE_ZERO;
      cmb.t1c_ext_b = GR_CMBX_LOCAL_TEXTURE_RGB;
      cmb.t1c_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t1c_ext_c = GR_CMBX_ZERO;
      cmb.t1c_ext_c_invert = 0;
      cmb.t1c_ext_d = GR_CMBX_ZERO;
      cmb.t1c_ext_d_invert = 0;
      cmb.t1a_ext_a = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t1a_ext_a_mode = GR_FUNC_MODE_ZERO;
      cmb.t1a_ext_b = GR_CMBX_LOCAL_TEXTURE_ALPHA;
      cmb.t1a_ext_b_mode = GR_FUNC_MODE_ZERO;
      cmb.t1a_ext_c = GR_CMBX_ZERO;
      cmb.t1a_ext_c_invert = 0;
      cmb.t1a_ext_d = GR_CMBX_ZERO;
      cmb.t1a_ext_d_invert = 0;
    }
    }

    // little change to make single-tmu cards look better, use first texture no matter what
    
    if (num_tmu == 1)
    {
        if (rdp.best_tex == 0)
        {
      cmb.tmu0_func = cmb.tmu0_a_func = GR_COMBINE_FUNCTION_LOCAL;
      cmb.tmu0_fac = cmb.tmu0_a_fac = GR_COMBINE_FACTOR_NONE;
            tmu_0 = 0;
            tmu_1 = 1;
        }
        else
        {
      cmb.tmu1_func = cmb.tmu1_a_func = GR_COMBINE_FUNCTION_LOCAL;
      cmb.tmu1_fac = cmb.tmu1_a_fac = GR_COMBINE_FACTOR_NONE;
            tmu_1 = 0;
            tmu_0 = 1;
        }
    }
    

    rdp.t0 = tmu_0;
    rdp.t1 = tmu_1;

    // SET the combiner
    if (fullscreen)
    {
        if (rdp.allow_combine)
        {
            // Now actually combine
      if (cmb.cmb_ext_use)
      {
        RDP (" | | | |- combiner extension\n");
        if (!(cmb.cmb_ext_use & COMBINE_EXT_COLOR))
          ColorCombinerToExtension ();
        if (!(cmb.cmb_ext_use & COMBINE_EXT_ALPHA))
          AlphaCombinerToExtension ();
        cmb.grColorCombineExt(cmb.c_ext_a, cmb.c_ext_a_mode, 
          cmb.c_ext_b, cmb.c_ext_b_mode, 
          cmb.c_ext_c, cmb.c_ext_c_invert, 
          cmb.c_ext_d, cmb.c_ext_d_invert, 0, 0);
        cmb.grAlphaCombineExt(cmb.a_ext_a, cmb.a_ext_a_mode, 
          cmb.a_ext_b, cmb.a_ext_b_mode, 
          cmb.a_ext_c, cmb.a_ext_c_invert, 
          cmb.a_ext_d, cmb.a_ext_d_invert, 0, 0);
      }
      else
      {
        grColorCombine (cmb.c_fnc, cmb.c_fac, cmb.c_loc, cmb.c_oth, FXFALSE);
        grAlphaCombine (cmb.a_fnc, cmb.a_fac, cmb.a_loc, cmb.a_oth, FXFALSE);
      }
      grConstantColorValue (cmb.ccolor);
      grAlphaBlendFunction (cmb.abf1, cmb.abf2, GR_BLEND_ZERO, GR_BLEND_ZERO);
        }
    
        if (tmu_1 < num_tmu)
        {
      if (cmb.tex_cmb_ext_use)
      {
        RDP (" | | | |- combiner extension tmu1\n");
        if (!(cmb.tex_cmb_ext_use & TEX_COMBINE_EXT_COLOR))
          TexColorCombinerToExtension (GR_TMU1);
        if (!(cmb.tex_cmb_ext_use & TEX_COMBINE_EXT_ALPHA))
          TexAlphaCombinerToExtension (GR_TMU1);
        cmb.grTexColorCombineExt(tmu_1, cmb.t1c_ext_a, cmb.t1c_ext_a_mode, 
          cmb.t1c_ext_b, cmb.t1c_ext_b_mode, 
          cmb.t1c_ext_c, cmb.t1c_ext_c_invert, 
          cmb.t1c_ext_d, cmb.t1c_ext_d_invert, 0, 0);
        cmb.grTexAlphaCombineExt(tmu_1, cmb.t1a_ext_a, cmb.t1a_ext_a_mode, 
          cmb.t1a_ext_b, cmb.t1a_ext_b_mode, 
          cmb.t1a_ext_c, cmb.t1a_ext_c_invert, 
          cmb.t1a_ext_d, cmb.t1a_ext_d_invert, 0, 0);
        cmb.grConstantColorValueExt(tmu_1, cmb.tex_ccolor);
      }
      else
      {
        grTexCombine (tmu_1, cmb.tmu1_func, cmb.tmu1_fac, cmb.tmu1_a_func, cmb.tmu1_a_fac, cmb.tmu1_invert, cmb.tmu1_a_invert);
        if (cmb.combine_ext)
          cmb.grConstantColorValueExt(tmu_1, 0);
      }
      grTexDetailControl (tmu_1, cmb.dc1_lodbias, cmb.dc1_detailscale, cmb.dc1_detailmax);
      grTexLodBiasValue (tmu_1, cmb.lodbias1);
        }
        if (tmu_0 < num_tmu)
        {
      if (cmb.tex_cmb_ext_use)
      {
        RDP (" | | | |- combiner extension tmu0\n");
        if (!(cmb.tex_cmb_ext_use & TEX_COMBINE_EXT_COLOR))
          TexColorCombinerToExtension (GR_TMU0);
        if (!(cmb.tex_cmb_ext_use & TEX_COMBINE_EXT_ALPHA))
          TexAlphaCombinerToExtension (GR_TMU0);
        cmb.grTexColorCombineExt(tmu_0, cmb.t0c_ext_a, cmb.t0c_ext_a_mode, 
          cmb.t0c_ext_b, cmb.t0c_ext_b_mode, 
          cmb.t0c_ext_c, cmb.t0c_ext_c_invert, 
          cmb.t0c_ext_d, cmb.t0c_ext_d_invert, 0, 0);
        cmb.grTexAlphaCombineExt(tmu_0, cmb.t0a_ext_a, cmb.t0a_ext_a_mode, 
          cmb.t0a_ext_b, cmb.t0a_ext_b_mode, 
          cmb.t0a_ext_c, cmb.t0a_ext_c_invert, 
          cmb.t0a_ext_d, cmb.t0a_ext_d_invert, 0, 0);
        cmb.grConstantColorValueExt(tmu_0, cmb.tex_ccolor);
      }
      else
      {
        grTexCombine (tmu_0, cmb.tmu0_func, cmb.tmu0_fac, cmb.tmu0_a_func, cmb.tmu0_a_fac, cmb.tmu0_invert, cmb.tmu0_a_invert);
        if (cmb.combine_ext)
          cmb.grConstantColorValueExt(tmu_0, 0);
      }
      grTexDetailControl (tmu_0, cmb.dc0_lodbias, cmb.dc0_detailscale, cmb.dc0_detailmax);
      grTexLodBiasValue (tmu_0, cmb.lodbias0);
        }
    }

    if ((rdp.tex & 1) && tmu_0 < num_tmu)
    {
        if (tex_found[0][tmu_0] != -1)
        {
            RDP (" | |- T0 found in cache.\n");
            if (fullscreen)
            {
                CACHE_LUT *cache = &rdp.cache[tmu_0][tex_found[0][tmu_0]];
                rdp.cur_cache_n[0] = tex_found[0][tmu_0];
                rdp.cur_cache[0] = cache;
                rdp.cur_cache[0]->last_used = frame_count;
                rdp.cur_cache[0]->uses = rdp.debug_n;
                grTexSource (tmu_0,
                    (grTexMinAddress(tmu_0) + cache->tmem_addr),
                    GR_MIPMAPLEVELMASK_BOTH,
                    &cache->t_info);
            }
        }
        else
            LoadTex (0, tmu_0);
    }
    if ((rdp.tex & 2) && tmu_1 < num_tmu)
    {
        if (tex_found[1][tmu_1] != -1)
        {
            if (fullscreen)
            {
                CACHE_LUT *cache = &rdp.cache[tmu_1][tex_found[1][tmu_1]];
                rdp.cur_cache_n[1] = tex_found[1][tmu_1];
                rdp.cur_cache[1] = cache;
                rdp.cur_cache[1]->last_used = frame_count;
                rdp.cur_cache[1]->uses = rdp.debug_n;
                grTexSource (tmu_1,
                    (grTexMinAddress(tmu_1) + cache->tmem_addr),
                    GR_MIPMAPLEVELMASK_BOTH,
                    &cache->t_info);
            }
        }
        else
            LoadTex (1, tmu_1);
    }

    if (fullscreen)
    {
        for (int i=0; i<2; i++)
        {
            int tmu;
            if (i==0) tmu=tmu_0;
            else tmu=tmu_1;

            if (tmu >= num_tmu) continue;

            int tile = rdp.cur_tile + i;

            if (settings.filtering == 0)
            {
                int filter = (rdp.filter_mode!=2)?GR_TEXTUREFILTER_POINT_SAMPLED:GR_TEXTUREFILTER_BILINEAR;
                grTexFilterMode (tmu, filter, filter);
            }
            else
            {
                int filter = (settings.filtering==1)?GR_TEXTUREFILTER_BILINEAR:GR_TEXTUREFILTER_POINT_SAMPLED;
                grTexFilterMode (tmu, filter, filter);
            }

            DWORD mode_s, mode_t;

            if ((rdp.tiles[tile].clamp_s || rdp.tiles[tile].mask_s == 0) &&
                rdp.tiles[tile].lr_s-rdp.tiles[tile].ul_s < 256)
                mode_s = GR_TEXTURECLAMP_CLAMP;
            else
            {
                if (rdp.tiles[tile].mirror_s && sup_mirroring)
                    mode_s = GR_TEXTURECLAMP_MIRROR_EXT;
                else
                    mode_s = GR_TEXTURECLAMP_WRAP;
            }

            if ((rdp.tiles[tile].clamp_t || rdp.tiles[tile].mask_t == 0) &&
                rdp.tiles[tile].lr_t-rdp.tiles[tile].ul_t < 256)
                mode_t = GR_TEXTURECLAMP_CLAMP;
            else
            {
                if (rdp.tiles[tile].mirror_t && sup_mirroring)
                    mode_t = GR_TEXTURECLAMP_MIRROR_EXT;
                else
                    mode_t = GR_TEXTURECLAMP_WRAP;
            }

            grTexClampMode (tmu,
                mode_s,
                mode_t);
    }
           if (rdp.hires_tex)
             SelectHiresTex();
        }

    RDP (" | +- TexCache End\n");
}

//****************************************************************
// ClearCache - clear the texture cache for BOTH tmus

void ClearCache ()
{
    rdp.tmem_ptr[0] = offset_textures;
    rdp.n_cached[0] = 0;
    rdp.tmem_ptr[1] = offset_texbuf1;
    rdp.n_cached[1] = 0;

    for (int i=0; i<256; i++)
    {
            DeleteList (&cachelut[i]);
    }
}

//****************************************************************
// LoadTex - does the actual texture loading after everything is prepared

void LoadTex (int id, int tmu)
{
    FRDP (" | |-+ LoadTex (id: %d, tmu: %d)\n", id, tmu);

    int td = rdp.cur_tile + id;
    int lod, aspect;
    CACHE_LUT *cache;

    if (texinfo[id].width < 0 ||
        texinfo[id].height < 0) return;

    // Clear the cache if it's full
    if (rdp.n_cached[tmu] >= MAX_CACHE)
    {
        RDP ("Cache count reached, clearing...\n");
        ClearCache ();
        if (id == 1 && rdp.tex == 3)
            LoadTex (0, rdp.t0);
    }
    
    // Get this cache object
    cache = &rdp.cache[tmu][rdp.n_cached[tmu]];
    rdp.cur_cache[id] = cache;
    rdp.cur_cache_n[id] = rdp.n_cached[tmu];

    // Set the data
    cache->line = rdp.tiles[td].line;
    cache->addr = rdp.addr[rdp.tiles[td].t_mem];
    cache->crc = texinfo[id].crc;
    cache->palette = rdp.tiles[td].palette;
    cache->width = rdp.tiles[td].width;
    cache->height = rdp.tiles[td].height;
    cache->format = rdp.tiles[td].format;
    cache->size = rdp.tiles[td].size;
    cache->tmem_addr = rdp.tmem_ptr[tmu];
    cache->set_by = rdp.timg.set_by;
    cache->texrecting = rdp.texrecting;
    cache->last_used = frame_count;
    cache->uses = rdp.debug_n;
    cache->flags = texinfo[id].flags;

    // Add this cache to the list
    AddToList (&cachelut[cache->crc>>24], cache->crc, cache, tmu, rdp.n_cached[tmu]);

    rdp.n_cached[tmu] ++;

    // temporary
    cache->t_info.format = GR_TEXFMT_ARGB_1555;

    // Calculate lod and aspect
    DWORD size_x = rdp.tiles[td].width;
    DWORD size_y = rdp.tiles[td].height;

    // make size_x and size_y both powers of two
    if (size_x > 256) size_x = 256;
    if (size_y > 256) size_y = 256;

    int shift;
    for (shift=0; (1<<shift) < (int)size_x; shift++);
    size_x = 1 << shift;
    for (shift=0; (1<<shift) < (int)size_y; shift++);
    size_y = 1 << shift;

    // Voodoo 1 support is all here, it will automatically mirror to the full extent.
    if (!sup_mirroring)
    {
        if (rdp.tiles[td].mirror_s && !rdp.tiles[td].clamp_s && size_x <= 128)
            size_x <<= 1;
        if (rdp.tiles[td].mirror_t && !rdp.tiles[td].clamp_t && size_y <= 128)
            size_y <<= 1;
    }

    // Calculate the maximum size
    int size_max = max (size_x, size_y);
    DWORD real_x=size_max, real_y=size_max;
    switch (size_max)
    {
    case 1:
        lod = GR_LOD_LOG2_1;
        cache->scale = 256.0f;
        break;
    case 2:
        lod = GR_LOD_LOG2_2;
        cache->scale = 128.0f;
        break;
    case 4:
        lod = GR_LOD_LOG2_4;
        cache->scale = 64.0f;
        break;
    case 8:
        lod = GR_LOD_LOG2_8;
        cache->scale = 32.0f;
        break;
    case 16:
        lod = GR_LOD_LOG2_16;
        cache->scale = 16.0f;
        break;
    case 32:
        lod = GR_LOD_LOG2_32;
        cache->scale = 8.0f;
        break;
    case 64:
        lod = GR_LOD_LOG2_64;
        cache->scale = 4.0f;
        break;
    case 128:
        lod = GR_LOD_LOG2_128;
        cache->scale = 2.0f;
        break;
    //case 256:
    default:
        lod = GR_LOD_LOG2_256;
        cache->scale = 1.0f;
        break;

        // No default here, can't be a non-power of two, see above
    }

    // Calculate the aspect ratio
    if (size_x >= size_y)
    {
        int ratio = size_x / size_y;
        switch (ratio)
        {
        case 1:
            aspect = GR_ASPECT_LOG2_1x1;
            cache->scale_x = 1.0f;
            cache->scale_y = 1.0f;
            break;
        case 2:
            aspect = GR_ASPECT_LOG2_2x1;
            cache->scale_x = 1.0f;
            cache->scale_y = 0.5f;
            real_y >>= 1;
            break;
        case 4:
            aspect = GR_ASPECT_LOG2_4x1;
            cache->scale_x = 1.0f;
            cache->scale_y = 0.25f;
            real_y >>= 2;
            break;
        //case 8:
        default:
            aspect = GR_ASPECT_LOG2_8x1;
            cache->scale_x = 1.0f;
            cache->scale_y = 0.125f;
            real_y >>= 3;
            break;
        }
    }
    else
    {
        int ratio = size_y / size_x;
        switch (ratio)
        {
        case 2:
            aspect = GR_ASPECT_LOG2_1x2;
            cache->scale_x = 0.5f;
            cache->scale_y = 1.0f;
            real_x >>= 1;
            break;
        case 4:
            aspect = GR_ASPECT_LOG2_1x4;
            cache->scale_x = 0.25f;
            cache->scale_y = 1.0f;
            real_x >>= 2;
            break;
        //case 8:
        default:
            aspect = GR_ASPECT_LOG2_1x8;
            cache->scale_x = 0.125f;
            cache->scale_y = 1.0f;
            real_x >>= 3;
            break;
        }
    }

    if (real_x != cache->width || real_y != cache->height)
    {
        cache->scale_x *= (float)cache->width / (float)real_x;
        cache->scale_y *= (float)cache->height / (float)real_y;
    }

    int splits = texinfo[id].splits;
    cache->splits = texinfo[id].splits;
    cache->splitheight = real_y / cache->splits;
    if (cache->splitheight < texinfo[id].splitheight)
        cache->splitheight = texinfo[id].splitheight;

    // ** Calculate alignment values
    int wid = cache->width;
    int hei = cache->height;

    if (splits > 1)
    {
        wid = texinfo[id].real_image_width;
        hei = texinfo[id].real_image_height;
    }

    float center_off = cache->scale / 2.0f;
    float c_lr_x = (float)real_x*cache->scale - center_off;
    float c_lr_y = (float)real_y*cache->scale - center_off;
    float c_scl_x;
    if (real_x != 1) c_scl_x = (c_lr_x - center_off) / (real_x-1);
    else c_scl_x = 0.0f;
    float c_scl_y;
    if (real_y != 1) c_scl_y = (c_lr_y - center_off) / (real_y-1);
    else c_scl_y = 0.0f;
    c_lr_x = center_off + c_scl_x * (wid-1);
    c_lr_y = center_off + c_scl_y * (hei-1);
    cache->c_off = center_off;
    if (wid != 1) cache->c_scl_x = (c_lr_x - center_off) / (float)(wid-1);
    else cache->c_scl_x = 0.0f;
    if (hei != 1) cache->c_scl_y = (c_lr_y - center_off) / (float)(hei-1);
    else cache->c_scl_y = 0.0f;
    // **

    DWORD mod, modcolor, modcolor1, modcolor2, modfactor;
    if (id == 0)
    {
    mod = cmb.mod_0;
    modcolor = cmb.modcolor_0;
    modcolor1 = cmb.modcolor1_0;
    modcolor2 = cmb.modcolor2_0;
    modfactor = cmb.modfactor_0;
  }
  else
  {
    mod = cmb.mod_1;
    modcolor = cmb.modcolor_1;
    modcolor1 = cmb.modcolor1_1;
    modcolor2 = cmb.modcolor2_1;
    modfactor = cmb.modfactor_1;
    }

    WORD tmp_pal[256];
    BOOL modifyPalette = (mod && (cache->format == 2) && (rdp.tlut_mode == 2));

    if (modifyPalette)
    {
      memcpy(tmp_pal, rdp.pal_8, 512);
      ModifyPalette(mod, modcolor, modcolor1, modfactor);
    }
    
    cache->mod = mod;
    cache->mod_color = modcolor;
    cache->mod_color1 = modcolor1;
    cache->mod_factor = modfactor;

  if (rdp.hires_tex && rdp.hires_tex->tile == id) //texture buffer will be used instead of frame buffer texture
  {
    RDP("hires_tex selected\n");
      return;
  }

    DWORD result = 0;   // keep =0 so it doesn't mess up on the first split

    texture = tex1;

    // ** handle texture splitting **
    if (splits > 1)
    {
        cache->scale_y = 0.125f;

        int i;
        for (i=0; i<splits; i++)
        {
            int start_dst = i * cache->splitheight * 256;   // start lower
            start_dst <<= HIWORD(result);   // 1st time, result is set to 0, but start_dst is 0 anyway so it doesn't matter

            int start_src = i * 256;    // start 256 more to the right
      start_src = start_src << (rdp.tiles[td].size) >> 1;

            result = load_table[rdp.tiles[td].size][rdp.tiles[td].format]
                (texture+start_dst, rdp.tmem+(rdp.tiles[td].t_mem<<3)+start_src,
                texinfo[id].wid_64, texinfo[id].height, texinfo[id].line, real_x, td);

            DWORD size = HIWORD(result);
            // clamp so that it looks somewhat ok when wrapping
            if (size == 1)
                Clamp16bT (texture+start_dst, texinfo[id].height, real_x, cache->splitheight);
            else
                Clamp8bT (texture+start_dst, texinfo[id].height, real_x, cache->splitheight);
        }
    }
    // ** end texture splitting **
    else
    {   
        result = load_table[rdp.tiles[td].size][rdp.tiles[td].format]
            (texture, rdp.tmem+(rdp.tiles[td].t_mem<<3),
            texinfo[id].wid_64, texinfo[id].height, texinfo[id].line, real_x, td);

        DWORD size = HIWORD(result);

        int min_x, min_y;
        if (rdp.tiles[td].mask_s != 0)
            min_x = min((int)real_x, 1<<rdp.tiles[td].mask_s);
        else
            min_x = real_x;
        if (rdp.tiles[td].mask_t != 0)
            min_y  = min((int)real_y, 1<<rdp.tiles[td].mask_t);
        else
            min_y = real_y;

        // Load using mirroring/clamping
        if (min_x > texinfo[id].width)
        {
            if (size == 1)
                Clamp16bS (texture, texinfo[id].width, min_x, real_x, texinfo[id].height);
            else
                Clamp8bS (texture, texinfo[id].width, min_x, real_x, texinfo[id].height);
        }

        if (texinfo[id].width < (int)real_x)
        {
            if (rdp.tiles[td].mirror_s)
            {
                if (size == 1)
                    Mirror16bS (texture, rdp.tiles[td].mask_s,
                        real_x, real_x, texinfo[id].height);
                else
                    Mirror8bS (texture, rdp.tiles[td].mask_s,
                        real_x, real_x, texinfo[id].height);
            }
            else
            {
                if (size == 1)
                    Wrap16bS (texture, rdp.tiles[td].mask_s,
                        real_x, real_x, texinfo[id].height);
                else
                    Wrap8bS (texture, rdp.tiles[td].mask_s,
                        real_x, real_x, texinfo[id].height);
            }
        }

        if (min_y > texinfo[id].height)
        {
            if (size == 1)
                Clamp16bT (texture, texinfo[id].height, real_x, min_y);
            else
                Clamp8bT (texture, texinfo[id].height, real_x, min_y);
        }

        if (texinfo[id].height < (int)real_y)
        {
            if (rdp.tiles[td].mirror_t)
            {
                if (size == 1)
                    Mirror16bT (texture, rdp.tiles[td].mask_t,
                        real_y, real_x);
                else
                    Mirror8bT (texture, rdp.tiles[td].mask_t,
                        real_y, real_x);
            }
            else
            {
                if (size == 1)
                    Wrap16bT (texture, rdp.tiles[td].mask_t,
                        real_y, real_x);
                else
                    Wrap8bT (texture, rdp.tiles[td].mask_t,
                        real_y, real_x);
            }
        }
    }

    if (modifyPalette)
    {
      memcpy(rdp.pal_8, tmp_pal, 512);
    }
    
    if (mod && !modifyPalette)
    {
        // Convert the texture to ARGB 4444
        if (LOWORD(result) == GR_TEXFMT_ARGB_1555)
            TexConv_ARGB1555_ARGB4444 (tex1, tex2, real_x, real_y);
        if (LOWORD(result) == GR_TEXFMT_ALPHA_INTENSITY_88)
            TexConv_AI88_ARGB4444 (tex1, tex2, real_x, real_y);
        if (LOWORD(result) == GR_TEXFMT_ALPHA_INTENSITY_44)
            TexConv_AI44_ARGB4444 (tex1, tex2, real_x, real_y);
        if (LOWORD(result) == GR_TEXFMT_ALPHA_8)
            TexConv_A8_ARGB4444 (tex1, tex2, real_x, real_y);
        if (LOWORD(result) == GR_TEXFMT_ARGB_4444)
            memcpy (tex2, tex1, (real_x*real_y) << 1);
        texture = tex2;
        result = (1 << 16) | GR_TEXFMT_ARGB_4444;

        // Now convert the color to the same
        modcolor = ((modcolor & 0xF0000000) >> 16) | ((modcolor & 0x00F00000) >> 12) |
            ((modcolor & 0x0000F000) >> 8) | ((modcolor & 0x000000F0) >> 4);
        modcolor1 = ((modcolor1 & 0xF0000000) >> 16) | ((modcolor1 & 0x00F00000) >> 12) |
            ((modcolor1 & 0x0000F000) >> 8) | ((modcolor1 & 0x000000F0) >> 4);
        modcolor2 = ((modcolor2 & 0xF0000000) >> 16) | ((modcolor2 & 0x00F00000) >> 12) |
            ((modcolor2 & 0x0000F000) >> 8) | ((modcolor2 & 0x000000F0) >> 4);

        int size = (real_x * real_y) << 1;

        switch (mod)
        {
        case TMOD_TEX_INTER_COLOR_USING_FACTOR:
            mod_tex_inter_color_using_factor ((WORD*)tex2, size, modcolor, modfactor);
            break;
        case TMOD_TEX_INTER_COL_USING_COL1:
            mod_tex_inter_col_using_col1 ((WORD*)tex2, size, modcolor, modcolor1);
            break;
        case TMOD_FULL_COLOR_SUB_TEX:
            mod_full_color_sub_tex ((WORD*)tex2, size, modcolor);
            break;
        case TMOD_COL_INTER_COL1_USING_TEX:
            mod_col_inter_col1_using_tex ((WORD*)tex2, size, modcolor, modcolor1);
            break;
        case TMOD_COL_INTER_COL1_USING_TEXA:
            mod_col_inter_col1_using_texa ((WORD*)tex2, size, modcolor, modcolor1);
            break;
        case TMOD_COL_INTER_COL1_USING_TEXA__MUL_TEX:
            mod_col_inter_col1_using_texa__mul_tex ((WORD*)tex2, size, modcolor, modcolor1);
            break;
        case TMOD_COL_INTER_TEX_USING_TEXA:
            mod_col_inter_tex_using_texa ((WORD*)tex2, size, modcolor);
            break;
        case TMOD_COL2_INTER__COL_INTER_COL1_USING_TEX__USING_TEXA:
            mod_col2_inter__col_inter_col1_using_tex__using_texa ((WORD*)tex2, size, modcolor, modcolor1, modcolor2);
            break;
        case TMOD_TEX_SCALE_FAC_ADD_FAC:
            mod_tex_scale_fac_add_fac ((WORD*)tex2, size, modfactor);
            break;
        case TMOD_TEX_SUB_COL_MUL_FAC_ADD_TEX:
            mod_tex_sub_col_mul_fac_add_tex ((WORD*)tex2, size, modcolor, modfactor);
            break;
        case TMOD_TEX_SCALE_COL_ADD_COL:
            mod_tex_scale_col_add_col ((WORD*)tex2, size, modcolor, modfactor);
            break;
        case TMOD_TEX_ADD_COL:
            mod_tex_add_col ((WORD*)tex2, size, modcolor);
            break;
        case TMOD_TEX_SUB_COL:
            mod_tex_sub_col ((WORD*)tex2, size, modcolor);
            break;
        case TMOD_TEX_SUB_COL_MUL_FAC:
            mod_tex_sub_col_mul_fac ((WORD*)tex2, size, modcolor, modfactor);
            break;
        case TMOD_COL_INTER_TEX_USING_COL1:
            mod_col_inter_tex_using_col1 ((WORD*)tex2, size, modcolor, modcolor1);
            break;
        case TMOD_COL_MUL_TEXA_ADD_TEX:
            mod_col_mul_texa_add_tex((WORD*)tex2, size, modcolor);
            break;
        case TMOD_COL_INTER_TEX_USING_TEX:
            mod_col_inter_tex_using_tex ((WORD*)tex2, size, modcolor);
            break;
    case TMOD_TEX_INTER_NOISE_USING_COL:
      mod_tex_inter_noise_using_col ((WORD*)tex2, size, modcolor);
      break;
    case TMOD_TEX_INTER_COL_USING_TEXA:
      mod_tex_inter_col_using_texa ((WORD*)tex2, size, modcolor);
      break;
    case TMOD_TEX_MUL_COL:
      mod_tex_mul_col ((WORD*)tex2, size, modcolor);
      break;
    case TMOD_TEX_SCALE_FAC_ADD_COL:
      mod_tex_scale_fac_add_col ((WORD*)tex2, size, modcolor, modfactor);
      break;
        default:
            ;
       }
    }


    cache->t_info.format = LOWORD(result);

    cache->realwidth = real_x;
    cache->realheight = real_y;
    cache->lod = lod;
    cache->aspect = aspect;

    if (fullscreen)
    {
        // Load the texture into texture memory
        GrTexInfo *t_info = &cache->t_info;
        t_info->data = texture;
        t_info->smallLodLog2 = lod;
        t_info->largeLodLog2 = lod;
        t_info->aspectRatioLog2 = aspect;

        DWORD texture_size = grTexTextureMemRequired (GR_MIPMAPLEVELMASK_BOTH, t_info);

        // Check for 2mb boundary
        if ((rdp.tmem_ptr[tmu] < TEXMEM_2MB_EDGE) && (rdp.tmem_ptr[tmu]+texture_size > TEXMEM_2MB_EDGE))
        {
            rdp.tmem_ptr[tmu] = TEXMEM_2MB_EDGE;
            cache->tmem_addr = rdp.tmem_ptr[tmu];
        }

        // Check for end of memory (too many textures to fit, clear cache)
        if (rdp.tmem_ptr[tmu]+texture_size >= grTexMaxAddress(tmu))
        {
            RDP ("Cache size reached, clearing...\n");
            ClearCache ();

            if (id == 1 && rdp.tex == 3)
                LoadTex (0, rdp.t0);

            LoadTex (id, tmu);
            return;
            // DON'T CONTINUE (already done)
        }

        grTexDownloadMipMap (tmu,
            grTexMinAddress(tmu) + rdp.tmem_ptr[tmu],
            GR_MIPMAPLEVELMASK_BOTH,
            t_info);

        grTexSource (tmu,
            grTexMinAddress(tmu) + rdp.tmem_ptr[tmu],
            GR_MIPMAPLEVELMASK_BOTH,
            t_info);
        rdp.tmem_ptr[tmu] += texture_size;
    }

    RDP (" | | +- LoadTex end\n");
}

