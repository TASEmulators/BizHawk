/*
* Glide64 - Glide video plugin for Nintendo 64 emulators.
* Copyright (c) 2002  Dave2001
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
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
#include "Util.h"
#include "Debugger.h"
#include <stdio.h>
#include "Gfx1.3.h"
#ifndef _WIN32
#include <stdarg.h>
#include <string.h>
#endif // _WIN32

DEBUGGER debug;

const char *FBLa[] = { "G_BL_CLR_IN", "G_BL_CLR_MEM", "G_BL_CLR_BL", "G_BL_CLR_FOG" };
const char *FBLb[] = { "G_BL_A_IN", "G_BL_A_FOG", "G_BL_A_SHADE", "G_BL_0" };
const char *FBLc[] = { "G_BL_CLR_IN", "G_BL_CLR_MEM", "G_BL_CLR_BL", "G_BL_CLR_FOG"};
const char *FBLd[] = { "G_BL_1MA", "G_BL_A_MEM", "G_BL_1", "G_BL_0" };
const char *str_lod[]    = { "1", "2", "4", "8", "16", "32", "64", "128", "256" };
const char *str_aspect[] = { "1x8", "1x4", "1x2", "1x1", "2x1", "4x1", "8x1" };

#define SX(x) ((x)*rdp.scale_1024)
#define SY(x) ((x)*rdp.scale_768)

#ifdef COLORED_DEBUGGER
#define COL_CATEGORY()  grConstantColorValue(0xD288F400)
#define COL_UCC()   grConstantColorValue(0xFF000000)
#define COL_CC()    grConstantColorValue(0x88C3F400)
#define COL_UAC()   grConstantColorValue(0xFF808000)
#define COL_AC()    grConstantColorValue(0x3CEE5E00)
#define COL_TEXT()    grConstantColorValue(0xFFFFFF00)
#define COL_SEL(x)    grConstantColorValue((x)?0x00FF00FF:0x800000FF)
#else
#define COL_CATEGORY()
#define COL_UCC()
#define COL_CC()
#define COL_UAC()
#define COL_AC()
#define COL_TEXT()
#define COL_SEL(x)
#endif

#define COL_GRID    0xFFFFFF80

BOOL  grid = 0;

//
// debug_init - initialize the debugger
//

void debug_init ()
{
  debug.capture = 0;
  debug.selected = SELECTED_TRI;
  debug.screen = NULL;
  debug.tri_list = NULL;
  debug.tri_last = NULL;
  debug.tri_sel = NULL;
  debug.tmu = 0;

  debug.tex_scroll = 0;
  debug.tex_sel = 0;

  debug.draw_mode = 0;
}

//
// debug_cacheviewer - views the debugger's cache
//

void debug_cacheviewer ()
{
  grCullMode (GR_CULL_DISABLE);

   int i;
  for (i=0; i<2; i++)
  {
    grTexFilterMode (i,
      (settings.filter_cache)?GR_TEXTUREFILTER_BILINEAR:GR_TEXTUREFILTER_POINT_SAMPLED,
      (settings.filter_cache)?GR_TEXTUREFILTER_BILINEAR:GR_TEXTUREFILTER_POINT_SAMPLED);
    grTexClampMode (i,
      GR_TEXTURECLAMP_CLAMP,
      GR_TEXTURECLAMP_CLAMP);
  }

  switch (debug.draw_mode)
  {
  case 0:
    grColorCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_LOCAL_NONE,
      GR_COMBINE_OTHER_TEXTURE,
      FXFALSE);
    grAlphaCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_LOCAL_NONE,
      GR_COMBINE_OTHER_TEXTURE,
      FXFALSE);
    break;
  case 1:
    grColorCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_LOCAL_NONE,
      GR_COMBINE_OTHER_TEXTURE,
      FXFALSE);
    grAlphaCombine (GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_LOCAL_CONSTANT,
      GR_COMBINE_OTHER_NONE,
      FXFALSE);
    grConstantColorValue (0xFFFFFFFF);
    break;
  case 2:
    grColorCombine (GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_LOCAL_CONSTANT,
      GR_COMBINE_OTHER_NONE,
      FXFALSE);
    grAlphaCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_LOCAL_NONE,
      GR_COMBINE_OTHER_TEXTURE,
      FXFALSE);
    grConstantColorValue (0xFFFFFFFF);
  }

  if (debug.tmu == 1)
  {
    grTexCombine (GR_TMU1,
      GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      FXFALSE,
      FXFALSE);

    grTexCombine (GR_TMU0,
      GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      FXFALSE,
      FXFALSE);
  }
  else
  {
    grTexCombine (GR_TMU0,
      GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      FXFALSE,
      FXFALSE);
  }

  grAlphaBlendFunction (GR_BLEND_SRC_ALPHA,
    GR_BLEND_ONE_MINUS_SRC_ALPHA,
    GR_BLEND_ONE,
    GR_BLEND_ZERO);

  // Draw texture memory
  for (i=0; i<4; i++)
  {
    for (DWORD x=0; x<16; x++)
    {
      DWORD y = i+debug.tex_scroll;
      if (x+y*16 >= (DWORD)rdp.n_cached[debug.tmu]) break;

      VERTEX v[4] = {
          { SX(x*64.0f), SY(512+64.0f*i), 1, 1, 0, 0, 0, 0, {0, 0, 0, 0} },
          { SX(x*64.0f+64.0f*rdp.cache[debug.tmu][x+y*16].scale_x), SY(512+64.0f*i), 1, 1,    255*rdp.cache[debug.tmu][x+y*16].scale_x, 0, 0, 0, {0, 0, 0, 0} },
          { SX(x*64.0f), SY(512+64.0f*i+64.0f*rdp.cache[debug.tmu][x+y*16].scale_y), 1, 1,    0, 255*rdp.cache[debug.tmu][x+y*16].scale_y, 0, 0, {0, 0, 0, 0} },
          { SX(x*64.0f+64.0f*rdp.cache[debug.tmu][x+y*16].scale_x), SY(512+64.0f*i+64.0f*rdp.cache[debug.tmu][x+y*16].scale_y), 1, 1, 255*rdp.cache[debug.tmu][x+y*16].scale_x, 255*rdp.cache[debug.tmu][x+y*16].scale_y, 0, 0, {0, 0, 0, 0} } };
      for (int i=0; i<4; i++)
      {
        v[i].u1 = v[i].u0;
        v[i].v1 = v[i].v0;
      }

      ConvertCoordsConvert (v, 4);

      grTexSource(debug.tmu,
        grTexMinAddress(debug.tmu) + rdp.cache[debug.tmu][x+y*16].tmem_addr,
        GR_MIPMAPLEVELMASK_BOTH,
        &rdp.cache[debug.tmu][x+y*16].t_info);

      grDrawTriangle (&v[2], &v[1], &v[0]);
      grDrawTriangle (&v[2], &v[3], &v[1]);
    }
  }

/*  for (i=0; i<4; i++)
  {
    for (DWORD x=0; x<16; x++)
    {
      DWORD y = i+debug.tex_scroll;
      if (x+y*16 >= (DWORD)rdp.n_cached[debug.tmu]) break;

      VERTEX v[4] = {
          { SX(x*64.0f), SY(768-64.0f*i), 1, 1,       0, 0, 0, 0, 0, 0, 0 },
          { SX(x*64.0f+64.0f), SY(768-64.0f*i), 1, 1,     255, 0, 0, 0, 0, 0, 0 },
          { SX(x*64.0f), SY(768-64.0f*i-64.0f), 1, 1,     0, 255, 0, 0, 0, 0, 0 },
          { SX(x*64.0f+64.0f), SY(768-64.0f*i-64.0f), 1, 1, 255, 255, 0, 0, 0, 0, 0 } };
      for (int i=0; i<4; i++)
      {
        v[i].u1 = v[i].u0;
        v[i].v1 = v[i].v0;
      }

      ConvertCoordsConvert (v, 4);

      grTexSource(debug.tmu,
        grTexMinAddress(debug.tmu) + rdp.cache[debug.tmu][x+y*16].tmem_addr,
        GR_MIPMAPLEVELMASK_BOTH,
        &rdp.cache[debug.tmu][x+y*16].t_info);

      grDrawTriangle (&v[2], &v[1], &v[0]);
      grDrawTriangle (&v[2], &v[3], &v[1]);
    }
  }*/
}

//
// debug_capture - does a frame capture event (for debugging)
//
#ifdef _WIN32
void debug_capture ()
{
  DWORD i,j;

  if (debug.tri_list == NULL) goto END;
  debug.tri_sel = debug.tri_list;
  debug.selected = SELECTED_TRI;

  // Connect the list
  debug.tri_last->pNext = debug.tri_list;

  while (!(GetAsyncKeyState(VK_INSERT) & 0x0001))
  {
    // Check for clicks
    if (GetAsyncKeyState(GetSystemMetrics(SM_SWAPBUTTON)?VK_RBUTTON:VK_LBUTTON) & 0x0001)
    {
      POINT pt;
      GetCursorPos (&pt);

      //int diff = settings.scr_res_y-settings.res_y;

      if (pt.y <= (int)settings.res_y)
      {
        int x = pt.x;
        int y = pt.y;//settings.res_y - (pt.y - diff);

        TRI_INFO *start;
        TRI_INFO *tri;
        if (debug.tri_sel == NULL) tri = debug.tri_list, start = debug.tri_list;
        else tri = debug.tri_sel->pNext, start = debug.tri_sel;

        // Select a triangle (start from the currently selected one)
        do {
          if (tri->v[0].x == tri->v[1].x &&
            tri->v[0].y == tri->v[1].y)
          {
            tri = tri->pNext;
            continue;
          }

          for (i=0; i<tri->nv; i++)
          {
            j=i+1;
            if (j==tri->nv) j=0;

            if ((y-tri->v[i].y)*(tri->v[j].x-tri->v[i].x) -
              (x-tri->v[i].x)*(tri->v[j].y-tri->v[i].y) < 0)
              break;    // It's outside
          }

          if (i==tri->nv) // all lines passed
          {
            debug.tri_sel = tri;
            break;
          }

          for (i=0; i<tri->nv; i++)
          {
            j=i+1;
            if (j==tri->nv) j=0;

            if ((y-tri->v[i].y)*(tri->v[j].x-tri->v[i].x) -
              (x-tri->v[i].x)*(tri->v[j].y-tri->v[i].y) > 0)
              break;    // It's outside
          }

          if (i==tri->nv) // all lines passed
          {
            debug.tri_sel = tri;
            break;
          }

          tri = tri->pNext;
        } while (tri != start);
      }
      else
      {
        // on a texture
        debug.tex_sel = (((DWORD)((pt.y-SY(512))/SY(64))+debug.tex_scroll)*16) +
          (DWORD)(pt.x/SX(64));
      }
    }

    debug_keys ();

    grBufferClear (0, 0, 0xFFFF);

    // Copy the screen capture back to the screen:
    // Lock the backbuffer
    GrLfbInfo_t info;
    while (!grLfbLock (GR_LFB_WRITE_ONLY,
      GR_BUFFER_BACKBUFFER,
      GR_LFBWRITEMODE_565,
      GR_ORIGIN_UPPER_LEFT,
      FXFALSE,
      &info));

    DWORD offset_src=0/*(settings.scr_res_y-settings.res_y)*info.strideInBytes*/, offset_dst=0;

    // Copy the screen
    for (DWORD y=0; y<settings.res_y; y++)
    {
      memcpy ((BYTE*)info.lfbPtr + offset_src, debug.screen + offset_dst, settings.res_x << 1);
      offset_dst += settings.res_x << 1;
      offset_src += info.strideInBytes;
    }

    // Unlock the backbuffer
    grLfbUnlock (GR_LFB_WRITE_ONLY, GR_BUFFER_BACKBUFFER);

    // Do the cacheviewer
    debug_cacheviewer ();

    // **
    // 3/16/02: Moved texture viewer out of loop, remade it.  Now it's simpler, and
    //   supports TMU1. [Dave2001]
    // Original by Gugaman

    if (debug.page == PAGE_TEX_INFO)
    {
      grTexSource(debug.tmu,
        grTexMinAddress(debug.tmu) + rdp.cache[debug.tmu][debug.tex_sel].tmem_addr,
        GR_MIPMAPLEVELMASK_BOTH,
        &rdp.cache[debug.tmu][debug.tex_sel].t_info);

#ifdef SHOW_FULL_TEXVIEWER
      float scx = 1.0f;
      float scy = 1.0f;
#else
      float scx = rdp.cache[debug.tmu][debug.tex_sel].scale_x;
      float scy = rdp.cache[debug.tmu][debug.tex_sel].scale_y;
#endif
      VERTEX v[4] = {
              { SX(704.0f), SY(221.0f), 1, 1,             0, 0,         0, 0,       0, 0, 0 },
              { SX(704.0f+256.0f*scx), SY(221.0f), 1, 1,        255*scx, 0,       255*scx, 0,     0, 0, 0 },
              { SX(704.0f), SY(221.0f+256.0f*scy), 1, 1,        0, 255*scy,       0, 255*scy,     0, 0, 0 },
              { SX(704.0f+256.0f*scx), SY(221.0f+256.0f*scy), 1, 1, 255*scx, 255*scy,   255*scx, 255*scy, 0, 0, 0 } };
      ConvertCoordsConvert (v, 4);
      VERTEX *varr[4] = { &v[0], &v[1], &v[2], &v[3] };
      grDrawVertexArray (GR_TRIANGLE_STRIP, 4, varr);
    }

    // **

    grTexFilterMode (GR_TMU0,
      GR_TEXTUREFILTER_BILINEAR,
      GR_TEXTUREFILTER_BILINEAR);

    grColorCombine (GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_LOCAL_CONSTANT,
      GR_COMBINE_OTHER_NONE,
      FXFALSE);

    grAlphaCombine (GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_LOCAL_CONSTANT,
      GR_COMBINE_OTHER_NONE,
      FXFALSE);

    grConstantColorValue (0x0000FFFF);

    VERTEX *v[8];
    if (debug.tri_sel)
    {
      // Draw the outline around the selected triangle
      for (i=0; i<debug.tri_sel->nv; i++)
      {
        j=i+1;
        if (j>=debug.tri_sel->nv) j=0;

        grDrawLine (&debug.tri_sel->v[i], &debug.tri_sel->v[j]);

        v[i] = &debug.tri_sel->v[i];
      }
    }

    // and the selected texture
    DWORD t_y = ((debug.tex_sel & 0xFFFFFFF0) >> 4) - debug.tex_scroll;
    DWORD t_x = debug.tex_sel & 0xF;
    VERTEX vt[4] = {
      { SX(t_x*64.0f), SY(512+64.0f*t_y), 1, 1 },
      { SX(t_x*64.0f+64.0f), SY(512+64.0f*t_y), 1, 1 },
      { SX(t_x*64.0f), SY(512+64.0f*t_y+64.0f), 1, 1 },
      { SX(t_x*64.0f+64.0f), SY(512+64.0f*t_y+64.0f), 1, 1 } };
    if (t_y < 4)
    {
      grDrawLine (&vt[0], &vt[1]);
      grDrawLine (&vt[1], &vt[3]);
      grDrawLine (&vt[3], &vt[2]);
      grDrawLine (&vt[2], &vt[0]);
    }

    grConstantColorValue (0xFF000020);

    if (t_y < 4)
    {
      grDrawTriangle (&vt[2], &vt[1], &vt[0]);
      grDrawTriangle (&vt[2], &vt[3], &vt[1]);
    }

    if (debug.tri_sel)
      grDrawVertexArray (GR_TRIANGLE_FAN, debug.tri_sel->nv, &v);

    // Draw the outline of the cacheviewer
    if (debug.page == PAGE_TEX_INFO)
    {
      /*grConstantColorValue (0xFF0000FF);
      float scx = rdp.cache[debug.tmu][debug.tex_sel].scale_x;
      float scy = rdp.cache[debug.tmu][debug.tex_sel].scale_y;
      VERTEX v[4] = {
              { SX(704.0f), SY(271.0f), 1, 1,             0, 0, 0, 0, 0,  0, 0, 0, 0 },
              { SX(704.0f+256.0f*scx), SY(271.0f), 1, 1,        0, 0, 0, 0, 0,  0, 0, 0, 0 },
              { SX(704.0f), SY(271.0f-256.0f*scy), 1, 1,        0, 0, 0, 0, 0,  0, 0, 0, 0 },
              { SX(704.0f+256.0f*scx), SY(271.0f-256.0f*scy), 1, 1, 0, 0, 0, 0, 0,  0, 0, 0, 0 } };
      VERTEX *varr[5] = { &v[0], &v[1], &v[3], &v[2], &v[0] };
      grDrawVertexArray (GR_LINE_STRIP, 5, varr);*/

      float scx = rdp.cache[debug.tmu][debug.tex_sel].scale_x;
      float scy = rdp.cache[debug.tmu][debug.tex_sel].scale_y;

      // And the grid
      if (grid)
      {
        grConstantColorValue (COL_GRID);

        float scale_y = (256.0f * scy) / (float)rdp.cache[debug.tmu][debug.tex_sel].height;
        for (int y=0; y<=(int)rdp.cache[debug.tmu][debug.tex_sel].height; y++)
        {
          float y_val = SY(221.0f+y*scale_y);
          VERTEX vh[2] = {
            { SX(704.0f), y_val, 1, 1 },
            { SX(704.0f+255.0f*scx), y_val, 1, 1 } };
          grDrawLine (&vh[0], &vh[1]);
        }

        float scale_x = (256.0f * scx) / (float)rdp.cache[debug.tmu][debug.tex_sel].width;
        for (int x=0; x<=(int)rdp.cache[debug.tmu][debug.tex_sel].width; x++)
        {
          float x_val = SX(704.0f+x*scale_x);
          VERTEX vv[2] = {
            { x_val, SX(221.0f), 1, 1 },
            { x_val, SX(221.0f+256.0f*scy), 1, 1 } };
          grDrawLine (&vv[0], &vv[1]);
        }
      }
    }

    grTexCombine (GR_TMU0,
        GR_COMBINE_FUNCTION_LOCAL,
        GR_COMBINE_FACTOR_NONE,
        GR_COMBINE_FUNCTION_LOCAL,
        GR_COMBINE_FACTOR_NONE,
        FXFALSE,
        FXFALSE);

    grColorCombine (GR_COMBINE_FUNCTION_LOCAL,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_LOCAL_CONSTANT,
      GR_COMBINE_OTHER_NONE,
      FXFALSE);

    grAlphaCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
      GR_COMBINE_FACTOR_ONE,
      GR_COMBINE_LOCAL_NONE,
      GR_COMBINE_OTHER_TEXTURE,
      FXFALSE);

    grConstantColorValue (0xFFFFFF00);

    // Output the information about the selected triangle
    grTexSource(GR_TMU0,      // Text
      grTexMinAddress(debug.tmu)+ offset_font,
      GR_MIPMAPLEVELMASK_BOTH,
      &fontTex);

    static char *cycle_mode_s[4] = { "1 cycle (0)", "2 cycle (1)", "copy (2)", "fill (3)" };

#define OUTPUT(fmt,other) output(642,(float)i,1,fmt,other); i-=16;
#define OUTPUT1(fmt,other,other1) output(642,(float)i,1,fmt,other,other1); i-=16;
#define OUTPUT_(fmt,cc) COL_SEL(cc); x=642; output(x,(float)i,1,fmt,0); x+=8*(strlen(fmt)+1)
#define _OUTPUT(fmt,cc) COL_SEL(cc); output(x,(float)i,1,fmt,0); x+=8*(strlen(fmt)+1)
    i = 740;
    float x;
    if (debug.page == PAGE_GENERAL && debug.tri_sel)
    {
      COL_CATEGORY();
      OUTPUT ("GENERAL (page 1):",0);
      COL_TEXT();
      OUTPUT ("tri #%d", debug.tri_sel->tri_n);
      OUTPUT ("type: %s", tri_type[debug.tri_sel->type]);
      OUTPUT ("geom:   0x%08lx", debug.tri_sel->geom_mode);
      OUTPUT ("othermode_h: 0x%08lx", debug.tri_sel->othermode_h);
      OUTPUT ("othermode_l: 0x%08lx", debug.tri_sel->othermode_l);
      OUTPUT ("flags: 0x%08lx", debug.tri_sel->flags);
      OUTPUT ("",0);
      COL_CATEGORY();
      OUTPUT ("COMBINE:",0);
      COL_TEXT();
      OUTPUT ("cycle_mode: %s", cycle_mode_s[debug.tri_sel->cycle_mode]);
      OUTPUT ("cycle1: 0x%08lx", debug.tri_sel->cycle1);
      OUTPUT ("cycle2: 0x%08lx", debug.tri_sel->cycle2);
      if (debug.tri_sel->uncombined & 1)
        COL_UCC();
      else
        COL_CC();
      OUTPUT ("a0: %s", Mode0[(debug.tri_sel->cycle1)&0x0000000F]);
      OUTPUT ("b0: %s", Mode1[(debug.tri_sel->cycle1>>4)&0x0000000F]);
      OUTPUT ("c0: %s", Mode2[(debug.tri_sel->cycle1>>8)&0x0000001F]);
      OUTPUT ("d0: %s", Mode3[(debug.tri_sel->cycle1>>13)&0x00000007]);
      if (debug.tri_sel->uncombined & 2)
        COL_UAC();
      else
        COL_AC();
      OUTPUT ("Aa0: %s", Alpha0[(debug.tri_sel->cycle1>>16)&0x00000007]);
      OUTPUT ("Ab0: %s", Alpha1[(debug.tri_sel->cycle1>>19)&0x00000007]);
      OUTPUT ("Ac0: %s", Alpha2[(debug.tri_sel->cycle1>>22)&0x00000007]);
      OUTPUT ("Ad0: %s", Alpha3[(debug.tri_sel->cycle1>>25)&0x00000007]);
      if (debug.tri_sel->uncombined & 1)
        COL_UCC();
      else
        COL_CC();
      OUTPUT ("a1: %s", Mode0[(debug.tri_sel->cycle2)&0x0000000F]);
      OUTPUT ("b1: %s", Mode1[(debug.tri_sel->cycle2>>4)&0x0000000F]);
      OUTPUT ("c1: %s", Mode2[(debug.tri_sel->cycle2>>8)&0x0000001F]);
      OUTPUT ("d1: %s", Mode3[(debug.tri_sel->cycle2>>13)&0x00000007]);
      if (debug.tri_sel->uncombined & 2)
        COL_UAC();
      else
        COL_AC();
      OUTPUT ("Aa1: %s", Alpha0[(debug.tri_sel->cycle2>>16)&0x00000007]);
      OUTPUT ("Ab1: %s", Alpha1[(debug.tri_sel->cycle2>>19)&0x00000007]);
      OUTPUT ("Ac1: %s", Alpha2[(debug.tri_sel->cycle2>>22)&0x00000007]);
      OUTPUT ("Ad1: %s", Alpha3[(debug.tri_sel->cycle2>>25)&0x00000007]);
    }
    if ((debug.page == PAGE_TEX1 || debug.page == PAGE_TEX2) && debug.tri_sel)
    {
      COL_CATEGORY ();
      OUTPUT1 ("TEXTURE %d (page %d):", debug.page-PAGE_TEX1, 2+debug.page-PAGE_TEX1);
      COL_TEXT();
      int tmu = debug.page - PAGE_TEX1;
      OUTPUT1 ("cur cache: %d,%d", debug.tri_sel->t[tmu].cur_cache[tmu]&0x0F, debug.tri_sel->t[tmu].cur_cache[tmu]>>4);
      OUTPUT ("tex_size: %d", debug.tri_sel->t[tmu].size);
      OUTPUT ("tex_format: %d", debug.tri_sel->t[tmu].format);
      OUTPUT ("width: %d", debug.tri_sel->t[tmu].width);
      OUTPUT ("height: %d", debug.tri_sel->t[tmu].height);
      OUTPUT ("palette: %d", debug.tri_sel->t[tmu].palette);
      OUTPUT ("clamp_s: %d", debug.tri_sel->t[tmu].clamp_s);
      OUTPUT ("clamp_t: %d", debug.tri_sel->t[tmu].clamp_t);
      OUTPUT ("mirror_s: %d", debug.tri_sel->t[tmu].mirror_s);
      OUTPUT ("mirror_t: %d", debug.tri_sel->t[tmu].mirror_t);
      OUTPUT ("mask_s: %d", debug.tri_sel->t[tmu].mask_s);
      OUTPUT ("mask_t: %d", debug.tri_sel->t[tmu].mask_t);
      OUTPUT ("shift_s: %d", debug.tri_sel->t[tmu].shift_s);
      OUTPUT ("shift_t: %d", debug.tri_sel->t[tmu].shift_t);
      OUTPUT ("ul_s: %d", debug.tri_sel->t[tmu].ul_s);
      OUTPUT ("ul_t: %d", debug.tri_sel->t[tmu].ul_t);
      OUTPUT ("lr_s: %d", debug.tri_sel->t[tmu].lr_s);
      OUTPUT ("lr_t: %d", debug.tri_sel->t[tmu].lr_t);
      OUTPUT ("t_ul_s: %d", debug.tri_sel->t[tmu].t_ul_s);
      OUTPUT ("t_ul_t: %d", debug.tri_sel->t[tmu].t_ul_t);
      OUTPUT ("t_lr_s: %d", debug.tri_sel->t[tmu].t_lr_s);
      OUTPUT ("t_lr_t: %d", debug.tri_sel->t[tmu].t_lr_t);
      OUTPUT ("scale_s: %f", debug.tri_sel->t[tmu].scale_s);
      OUTPUT ("scale_t: %f", debug.tri_sel->t[tmu].scale_t);
      OUTPUT ("s_mode: %s", str_cm[((debug.tri_sel->t[tmu].clamp_s << 1) | debug.tri_sel->t[tmu].mirror_s)&3]);
      OUTPUT ("t_mode: %s", str_cm[((debug.tri_sel->t[tmu].clamp_t << 1) | debug.tri_sel->t[tmu].mirror_t)&3]);
    }
    if (debug.page == PAGE_COLORS && debug.tri_sel)
    {
      COL_CATEGORY();
      OUTPUT ("COLORS (page 4)", 0);
      COL_TEXT();
      OUTPUT ("fill:  %08lx", debug.tri_sel->fill_color);
      OUTPUT ("prim:  %08lx", debug.tri_sel->prim_color);
      OUTPUT ("blend: %08lx", debug.tri_sel->blend_color);
      OUTPUT ("env:   %08lx", debug.tri_sel->env_color);
      OUTPUT ("fog: %08lx", debug.tri_sel->fog_color);
      OUTPUT ("prim_lodmin:  %d", debug.tri_sel->prim_lodmin);
      OUTPUT ("prim_lodfrac: %d", debug.tri_sel->prim_lodfrac);
    }
    if (debug.page == PAGE_FBL && debug.tri_sel)
    {
      COL_CATEGORY();
      OUTPUT ("BLENDER", 0);
      COL_TEXT();
      OUTPUT ("fbl_a0: %s", FBLa[(debug.tri_sel->othermode_l>>30)&0x3]);
      OUTPUT ("fbl_b0: %s", FBLb[(debug.tri_sel->othermode_l>>26)&0x3]);
      OUTPUT ("fbl_c0: %s", FBLc[(debug.tri_sel->othermode_l>>22)&0x3]);
      OUTPUT ("fbl_d0: %s", FBLd[(debug.tri_sel->othermode_l>>18)&0x3]);
      OUTPUT ("fbl_a1: %s", FBLa[(debug.tri_sel->othermode_l>>28)&0x3]);
      OUTPUT ("fbl_b1: %s", FBLb[(debug.tri_sel->othermode_l>>24)&0x3]);
      OUTPUT ("fbl_c1: %s", FBLc[(debug.tri_sel->othermode_l>>20)&0x3]);
      OUTPUT ("fbl_d1: %s", FBLd[(debug.tri_sel->othermode_l>>16)&0x3]);
      OUTPUT ("", 0);
      OUTPUT ("fbl:    %08lx", debug.tri_sel->othermode_l&0xFFFF0000);
      OUTPUT ("fbl #1: %08lx", debug.tri_sel->othermode_l&0xCCCC0000);
      OUTPUT ("fbl #2: %08lx", debug.tri_sel->othermode_l&0x33330000);
    }
    if (debug.page == PAGE_OTHERMODE_L && debug.tri_sel)
    {
      DWORD othermode_l = debug.tri_sel->othermode_l;
      COL_CATEGORY ();
      OUTPUT ("OTHERMODE_L: %08lx", othermode_l);
      OUTPUT_ ("AC_NONE", (othermode_l & 3) == 0);
      _OUTPUT ("AC_THRESHOLD", (othermode_l & 3) == 1);
      _OUTPUT ("AC_DITHER", (othermode_l & 3) == 3);
      i -= 16;
      OUTPUT_ ("ZS_PIXEL", !(othermode_l & 4));
      _OUTPUT ("ZS_PRIM", (othermode_l & 4));
      i -= 32;
      COL_CATEGORY ();
      OUTPUT ("RENDERMODE: %08lx", othermode_l);
      OUTPUT_ ("AA_EN", othermode_l & 0x08);
      i -= 16;
      OUTPUT_ ("Z_CMP", othermode_l & 0x10);
      i -= 16;
      OUTPUT_ ("Z_UPD", othermode_l & 0x20);
      i -= 16;
      OUTPUT_ ("IM_RD", othermode_l & 0x40);
      i -= 16;
      OUTPUT_ ("CLR_ON_CVG", othermode_l & 0x80);
      i -= 16;
      OUTPUT_ ("CVG_DST_CLAMP", (othermode_l & 0x300) == 0x000);
      _OUTPUT ("CVG_DST_WRAP", (othermode_l & 0x300) == 0x100);
      _OUTPUT (".._FULL", (othermode_l & 0x300) == 0x200);
      _OUTPUT (".._SAVE", (othermode_l & 0x300) == 0x300);
      i -= 16;
      OUTPUT_ ("ZM_OPA", (othermode_l & 0xC00) == 0x000);
      _OUTPUT ("ZM_INTER", (othermode_l & 0xC00) == 0x400);
      _OUTPUT ("ZM_XLU", (othermode_l & 0xC00) == 0x800);
      _OUTPUT ("ZM_DEC", (othermode_l & 0xC00) == 0xC00);
      i -= 16;
      OUTPUT_ ("CVG_X_ALPHA", othermode_l & 0x1000);
      i -= 16;
      OUTPUT_ ("ALPHA_CVG_SEL", othermode_l & 0x2000);
      i -= 16;
      OUTPUT_ ("FORCE_BL", othermode_l & 0x4000);
    }
    if (debug.page == PAGE_OTHERMODE_H && debug.tri_sel)
    {
      DWORD othermode_h = debug.tri_sel->othermode_h;
      COL_CATEGORY ();
      OUTPUT ("OTHERMODE_H: %08lx", othermode_h);
      OUTPUT_ ("CK_NONE", (othermode_h & 0x100) == 0);
      _OUTPUT ("CK_KEY", (othermode_h & 0x100) == 1);
      i -= 16;
      OUTPUT_  ("TC_CONV", (othermode_h & 0xE00) == 0x200);
      _OUTPUT ("TC_FILTCONV", (othermode_h & 0xE00) == 0xA00);
      _OUTPUT ("TC_FILT", (othermode_h & 0xE00) == 0xC00);
      i -= 16;
      OUTPUT_ ("TF_POINT", (othermode_h & 0x3000) == 0x0000);
      _OUTPUT ("TF_AVERAGE", (othermode_h & 0x3000) == 0x3000);
      _OUTPUT ("TF_BILERP", (othermode_h & 0x3000) == 0x2000);
      i -= 16;
      OUTPUT_ ("TT_NONE", (othermode_h & 0xC000) == 0x0000);
      _OUTPUT ("TT_RGBA16", (othermode_h & 0xC000) == 0x8000);
      _OUTPUT ("TT_IA16", (othermode_h & 0xC000) == 0xC000);
      i -= 16;
      OUTPUT_ ("TL_TILE", (othermode_h & 0x10000) == 0x00000);
      _OUTPUT ("TL_LOD", (othermode_h & 0x10000) == 0x10000);
      i -= 16;
      OUTPUT_ ("TD_CLAMP", (othermode_h & 0x60000) == 0x00000);
      _OUTPUT ("TD_SHARPEN", (othermode_h & 0x60000) == 0x20000);
      _OUTPUT ("TD_DETAIL", (othermode_h & 0x60000) == 0x40000);
      i -= 16;
      OUTPUT_ ("TP_NONE", (othermode_h & 0x80000) == 0x00000);
      _OUTPUT ("TP_PERSP", (othermode_h & 0x80000) == 0x80000);
      i -= 16;
      OUTPUT_ ("1CYCLE", (othermode_h & 0x300000) == 0x000000);
      _OUTPUT ("2CYCLE", (othermode_h & 0x300000) == 0x100000);
      _OUTPUT ("COPY", (othermode_h & 0x300000) == 0x200000);
      _OUTPUT ("FILL", (othermode_h & 0x300000) == 0x300000);
      i -= 16;
      OUTPUT_ ("PM_1PRIM", (othermode_h & 0x400000) == 0x000000);
      _OUTPUT ("PM_NPRIM", (othermode_h & 0x400000) == 0x400000);
    }
    if (debug.page == PAGE_TEXELS && debug.tri_sel)
    {
      // change these to output whatever you need, ou for triangles, or u0 for texrects
      COL_TEXT();
      OUTPUT ("n: %d", debug.tri_sel->nv);
      OUTPUT ("",0);
      for (j=0; j<debug.tri_sel->nv; j++)
      {
        OUTPUT1 ("v[%d].s0: %f", j, debug.tri_sel->v[j].ou);
        OUTPUT1 ("v[%d].t0: %f", j, debug.tri_sel->v[j].ov);
      }
      OUTPUT ("",0);
      for (j=0; j<debug.tri_sel->nv; j++)
      {
        OUTPUT1 ("v[%d].s1: %f", j, debug.tri_sel->v[j].u0);
        OUTPUT1 ("v[%d].t1: %f", j, debug.tri_sel->v[j].v0);
      }
    }
    if (debug.page == PAGE_COORDS && debug.tri_sel)
    {
      COL_TEXT();
      OUTPUT ("n: %d", debug.tri_sel->nv);
      for (j=0; j<debug.tri_sel->nv; j++)
      {
        OUTPUT1 ("v[%d].x: %f", j, debug.tri_sel->v[j].x);
        OUTPUT1 ("v[%d].y: %f", j, debug.tri_sel->v[j].y);
        OUTPUT1 ("v[%d].z: %f", j, debug.tri_sel->v[j].z);
        OUTPUT1 ("v[%d].w: %f", j, debug.tri_sel->v[j].w);
        OUTPUT1 ("v[%d].f: %f", j, 1.0f/debug.tri_sel->v[j].f);
        OUTPUT1 ("v[%d].r: %d", j, debug.tri_sel->v[j].r);
        OUTPUT1 ("v[%d].g: %d", j, debug.tri_sel->v[j].g);
        OUTPUT1 ("v[%d].b: %d", j, debug.tri_sel->v[j].b);
        OUTPUT1 ("v[%d].a: %d", j, debug.tri_sel->v[j].a);
      }
    }
    if (debug.page == PAGE_TEX_INFO && debug.tex_sel < (DWORD)rdp.n_cached[debug.tmu])
    {
      COL_CATEGORY();
      OUTPUT ("CACHE (page 0)", 0);
      COL_TEXT();
      //OUTPUT ("t_mem: %08lx", rdp.cache[0][debug.tex_sel].t_mem);
      //OUTPUT ("crc: %08lx", rdp.cache[0][debug.tex_sel].crc);
      OUTPUT ("addr: %08lx", rdp.cache[debug.tmu][debug.tex_sel].addr);
      OUTPUT ("scale_x: %f", rdp.cache[debug.tmu][debug.tex_sel].scale_x);
      OUTPUT ("scale_y: %f", rdp.cache[debug.tmu][debug.tex_sel].scale_y);
      OUTPUT ("tmem_addr: %08lx", rdp.cache[debug.tmu][debug.tex_sel].tmem_addr);
      OUTPUT ("palette: %08lx", rdp.cache[debug.tmu][debug.tex_sel].palette);
      OUTPUT ("set_by: %08lx", rdp.cache[debug.tmu][debug.tex_sel].set_by);
      OUTPUT ("texrecting: %d", rdp.cache[debug.tmu][debug.tex_sel].texrecting);

      OUTPUT ("mod: %08lx", rdp.cache[debug.tmu][debug.tex_sel].mod);
      OUTPUT ("mod_col: %08lx", rdp.cache[debug.tmu][debug.tex_sel].mod_color);
      OUTPUT ("mod_col1: %08lx", rdp.cache[debug.tmu][debug.tex_sel].mod_color1);
      i=740;
      output(800,(float)i,1,"width: %d", rdp.cache[debug.tmu][debug.tex_sel].width);
      i-=16;
      output(800,(float)i,1,"height: %d", rdp.cache[debug.tmu][debug.tex_sel].height);
      i-=16;
      output(800,(float)i,1,"format: %d", rdp.cache[debug.tmu][debug.tex_sel].format);
      i-=16;
      output(800,(float)i,1,"size: %d", rdp.cache[debug.tmu][debug.tex_sel].size);
      i-=16;
      output(800,(float)i,1,"crc: %08lx", rdp.cache[debug.tmu][debug.tex_sel].crc);
      i-=16;
      output(800,(float)i,1,"line: %d", rdp.cache[debug.tmu][debug.tex_sel].line);
      i-=16;
      output(800,(float)i,1,"mod_factor: %08lx", rdp.cache[debug.tmu][debug.tex_sel].mod_factor);
      i-=32;

      output(800,(float)i,1,"lod: %s", str_lod[rdp.cache[debug.tmu][debug.tex_sel].lod]);
      i-=16;
      output(800,(float)i,1,"aspect: %s", str_aspect[rdp.cache[debug.tmu][debug.tex_sel].aspect + 3]);

//  debug_texture(debug.tmu, rdp.cache[debug.tmu][debug.tex_sel].addr, debug.tex_sel);
    }

    // Draw the vertex numbers
    if (debug.tri_sel)
    {
      for (i=0; i<debug.tri_sel->nv; i++)
      {
        grConstantColorValue (0x000000FF);
        output (debug.tri_sel->v[i].x+1, settings.scr_res_y-debug.tri_sel->v[i].y+1, 1,
          "%d", i);
        grConstantColorValue (0xFFFFFFFF);
        output (debug.tri_sel->v[i].x, settings.scr_res_y-debug.tri_sel->v[i].y, 1,
          "%d", i);
      }
    }

    // Draw the cursor
    debug_mouse ();

    grBufferSwap (1);
  }

END:
  // Release all data
  delete [] debug.screen;
  TRI_INFO *tri;
  for (tri=debug.tri_list; tri != debug.tri_last;)
  {
    TRI_INFO *tmp = tri;
    tri = tri->pNext;
    delete [] tmp->v;
    delete tmp;
  }
  delete [] tri->v;
  delete tri;

  // Reset all values
  debug.capture = 0;
  debug.selected = SELECTED_TRI;
  debug.screen = NULL;
  debug.tri_list = NULL;
  debug.tri_last = NULL;
  debug.tri_sel = NULL;
  debug.tex_sel = 0;
}

//
// debug_mouse - draws the debugger mouse
//

void debug_mouse ()
{
  grColorCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
    GR_COMBINE_FACTOR_ONE,
    GR_COMBINE_LOCAL_NONE,
    GR_COMBINE_OTHER_TEXTURE,
    FXFALSE);

  grAlphaCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
    GR_COMBINE_FACTOR_ONE,
    GR_COMBINE_LOCAL_NONE,
    GR_COMBINE_OTHER_TEXTURE,
    FXFALSE);

  // Draw the cursor
  POINT pt;
  GetCursorPos (&pt);
  float cx = (float)pt.x;
  float cy = (float)pt.y;

  VERTEX v[4] = {
    { cx, cy, 1, 1,     0, 0, 0, 0, 0, 0, 0 },
    { cx+32, cy, 1, 1,    255, 0, 0, 0, 0, 0, 0 },
    { cx, cy+32, 1, 1,    0, 255, 0, 0, 0, 0, 0 },
    { cx+32, cy+32, 1, 1, 255, 255, 0, 0, 0, 0, 0 } };

  ConvertCoordsKeep (v, 4);

  grTexSource(GR_TMU0,
    grTexMinAddress(GR_TMU0) + offset_cursor,
    GR_MIPMAPLEVELMASK_BOTH,
    &cursorTex);

  if (num_tmu >= 3)
    grTexCombine (GR_TMU2,
      GR_COMBINE_FUNCTION_NONE,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_FUNCTION_NONE,
      GR_COMBINE_FACTOR_NONE, FXFALSE, FXFALSE);
  if (num_tmu >= 2)
    grTexCombine (GR_TMU1,
      GR_COMBINE_FUNCTION_NONE,
      GR_COMBINE_FACTOR_NONE,
      GR_COMBINE_FUNCTION_NONE,
      GR_COMBINE_FACTOR_NONE, FXFALSE, FXFALSE);
  grTexCombine (GR_TMU0,
    GR_COMBINE_FUNCTION_LOCAL,
    GR_COMBINE_FACTOR_NONE,
    GR_COMBINE_FUNCTION_LOCAL,
    GR_COMBINE_FACTOR_NONE, FXFALSE, FXFALSE);

  grDrawTriangle (&v[0], &v[1], &v[2]);
  grDrawTriangle (&v[1], &v[3], &v[2]);
}

//
// debug_keys - receives debugger key input
//

void debug_keys ()
{
  if ((GetAsyncKeyState (VK_RIGHT) & 0x0001) && debug.tri_sel)
  {
    TRI_INFO *start = debug.tri_sel;

    while (debug.tri_sel->pNext != start)
      debug.tri_sel = debug.tri_sel->pNext;
  }

  if ((GetAsyncKeyState (VK_LEFT) & 0x0001) && debug.tri_sel)
    debug.tri_sel = debug.tri_sel->pNext;

  // Check for page changes
  if (GetAsyncKeyState ('1') & 0x0001)
    debug.page = PAGE_GENERAL;
  if (GetAsyncKeyState ('2') & 0x0001)
    debug.page = PAGE_TEX1;
  if (GetAsyncKeyState ('3') & 0x0001)
    debug.page = PAGE_TEX2;
  if (GetAsyncKeyState ('4') & 0x0001)
    debug.page = PAGE_COLORS;
  if (GetAsyncKeyState ('5') & 0x0001)
    debug.page = PAGE_FBL;
  if (GetAsyncKeyState ('6') & 0x0001)
    debug.page = PAGE_OTHERMODE_L;
  if (GetAsyncKeyState ('7') & 0x0001)
    debug.page = PAGE_OTHERMODE_H;
  if (GetAsyncKeyState ('8') & 0x0001)
    debug.page = PAGE_TEXELS;
  if (GetAsyncKeyState ('9') & 0x0001)
    debug.page = PAGE_COORDS;
  if (GetAsyncKeyState ('0') & 0x0001)
    debug.page = PAGE_TEX_INFO;
  if (GetAsyncKeyState ('Q') & 0x0001)
    debug.tmu = 0;
  if (GetAsyncKeyState ('W') & 0x0001)
    debug.tmu = 1;

  if (GetAsyncKeyState ('G') & 0x0001)
    grid = !grid;

  // Go to texture
  if (GetAsyncKeyState (VK_SPACE) & 0x0001)
  {
    int tile = -1;
    if (debug.page == PAGE_TEX2)
      tile = 1;
    else
      tile = 0;
    if (tile != -1)
    {
      debug.tmu = debug.tri_sel->t[tile].tmu;
      debug.tex_sel = debug.tri_sel->t[tile].cur_cache[debug.tmu];
      debug.tex_scroll = (debug.tri_sel->t[tile].cur_cache[debug.tmu] >> 4) - 1;
    }
  }

  // Go to triangle
  if (GetAsyncKeyState (VK_LCONTROL) & 0x0001)
  {
    int count = rdp.debug_n - rdp.cache[debug.tmu][debug.tex_sel].uses - 1;
    if (rdp.cache[debug.tmu][debug.tex_sel].last_used == frame_count)
    {
      TRI_INFO *t = debug.tri_list;
      while (count && t) {
        t = t->pNext;
        count --;
      }
      debug.tri_sel = t;
    }
    else
      debug.tri_sel = NULL;
  }

  if (GetAsyncKeyState ('A') & 0x0001)
    debug.draw_mode = 0;  // texture & texture alpha
  if (GetAsyncKeyState ('S') & 0x0001)
    debug.draw_mode = 1;  // texture
  if (GetAsyncKeyState ('D') & 0x0001)
    debug.draw_mode = 2;  // texture alpha

  // Check for texture scrolling
  if (GetAsyncKeyState (VK_DOWN) & 0x0001)
    debug.tex_scroll ++;
  if (GetAsyncKeyState (VK_UP) & 0x0001)
    debug.tex_scroll --;
}
#endif // _WIN32
//
// output - output debugger text
//

void output (float x, float y, BOOL scale, const char *fmt, ...)
{
  va_list ap;
  va_start(ap,fmt);
  vsprintf(out_buf, fmt, ap);
  va_end(ap);

  BYTE c,r;
  for (DWORD i=0; i<strlen(out_buf); i++)
  {
    c = ((out_buf[i]-32) & 0x1F) * 8;//<< 3;
    r = (((out_buf[i]-32) & 0xE0) >> 5) * 16;//<< 4;
    VERTEX v[4] = { { SX(x), SY(768-y), 1, 1,   (float)c, r+16.0f, 0, 0, {0, 0, 0, 0} },
      { SX(x+8), SY(768-y), 1, 1,   c+8.0f, r+16.0f, 0, 0, {0, 0, 0, 0} },
      { SX(x), SY(768-y-16), 1, 1,  (float)c, (float)r, 0, 0, {0, 0, 0, 0} },
      { SX(x+8), SY(768-y-16), 1, 1,  c+8.0f, (float)r, 0, 0, {0, 0, 0, 0} } };
    if (!scale)
    {
      v[0].x = x;
      v[0].y = y;
      v[1].x = x+8;
      v[1].y = y;
      v[2].x = x;
      v[2].y = y-16;
      v[3].x = x+8;
      v[3].y = y-16;
    }

    ConvertCoordsKeep (v, 4);

    grDrawTriangle (&v[0], &v[1], &v[2]);
    grDrawTriangle (&v[1], &v[3], &v[2]);

    x+=8;
  }
}

