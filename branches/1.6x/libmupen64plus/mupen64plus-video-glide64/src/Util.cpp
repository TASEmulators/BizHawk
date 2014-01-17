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
* You should have received a copy of the GNU General Public
* License along with this program; if not, write to the Free
* Software Foundation, Inc., 51 Franklin Street, Fifth Floor, 
* Boston, MA  02110-1301, USA
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
#include "Combine.h"
#include "3dmath.h"
#include "Debugger.h"
#include "TexCache.h"
#include "DepthBufferRender.h"

#ifndef _WIN32
#include <string.h>
#include <stdlib.h>
#endif // _WIN32

#define max(a,b) ((a) > (b) ? (a) : (b))
#define min(a,b) ((a) < (b) ? (a) : (b))

#define Vj rdp.vtxbuf2[j]
#define Vi rdp.vtxbuf2[i]

VERTEX *vtx_list1[32];  // vertex indexing
VERTEX *vtx_list2[32];

//
// util_init - initialize data for the functions in this file
//

void util_init ()
{
    for (int i=0; i<32; i++)
    {
        vtx_list1[i] = &rdp.vtx1[i];
        vtx_list2[i] = &rdp.vtx2[i];
    }
}

//software backface culling. Gonetz
// mega modifications by Dave2001
BOOL cull_tri(VERTEX **v) // type changed to VERTEX** [Dave2001]
{
    int i;
    
    if (v[0]->scr_off & v[1]->scr_off & v[2]->scr_off)
    {
        RDP (" clipped\n");
        return TRUE;
    }
    
    // Triangle can't be culled, if it need clipping
    BOOL draw = FALSE;
    
    //fix for sun in zeldas
    BOOL fix_i_uv = FALSE;
    if (settings.zelda && rdp.rm == 0x0c184241 && rdp.tiles[rdp.cur_tile].format == 4) 
        fix_i_uv = TRUE;
    
    for (i=0; i<3; i++)
    {
        if (!v[i]->screen_translated)
        {
            v[i]->sx = rdp.view_trans[0] + v[i]->x_w * rdp.view_scale[0];
            v[i]->sy = rdp.view_trans[1] + v[i]->y_w * rdp.view_scale[1];
            v[i]->sz = rdp.view_trans[2] + v[i]->z_w * rdp.view_scale[2];
            if ((fix_i_uv) && (v[i]->uv_fixed == 0))
            {
                v[i]->uv_fixed = 1;
                v[i]->ou *= 0.5f;
                v[i]->ov *= 0.5f;
            }
            v[i]->screen_translated = 1;
        }
        if (v[i]->w < 0.01f) //need clip_z. can't be culled now
            draw = 1;
    }
    
    if (settings.fix_tex_coord)
      fix_tex_coord (v);
    if (draw) return FALSE; // z-clipping, can't be culled by software
    
#define SW_CULLING
#ifdef SW_CULLING
    //now we need to check, if triangle's vertices are in clockwise order
    // Use precalculated x/z and y/z coordinates.
    float x1 = v[0]->sx - v[1]->sx;
    float y1 = v[0]->sy - v[1]->sy;
    float x2 = v[2]->sx - v[1]->sx;
    float y2 = v[2]->sy - v[1]->sy;
    
    DWORD mode = (rdp.flags & CULLMASK) >> CULLSHIFT;
    switch (mode)
    {
    case 1: // cull front
        //    if ((x1*y2 - y1*x2) < 0.0f) //counter-clockwise, positive
        if ((y1*x2-x1*y2) < 0.0f) //counter-clockwise, positive
        {
            RDP (" culled!\n");
            return TRUE;
        }
        return FALSE;
    case 2: // cull back
        //    if ((x1*y2 - y1*x2) >= 0.0f) //clockwise, negative
        if ((y1*x2-x1*y2) >= 0.0f) //clockwise, negative
        {
            RDP (" culled!\n");
            return TRUE;
        }
        return FALSE;
    }
#endif
    
    return FALSE;
}


void apply_shade_mods (VERTEX *v)
{
    float col[4];
    DWORD cmb;
    memcpy (col, rdp.col, 16);
    
    if (rdp.cmb_flags)
    {
        cmb = rdp.cmb_flags;
        if (cmb & CMB_SET)
        {
            if (col[0] > 1.0f) col[0] = 1.0f;
            if (col[1] > 1.0f) col[1] = 1.0f;
            if (col[2] > 1.0f) col[2] = 1.0f;
            if (col[0] < 0.0f) col[0] = 0.0f;
            if (col[1] < 0.0f) col[1] = 0.0f;
            if (col[2] < 0.0f) col[2] = 0.0f;
            v->r = (BYTE)(255.0f * col[0]);
            v->g = (BYTE)(255.0f * col[1]);
            v->b = (BYTE)(255.0f * col[2]);
        }
        if (cmb & CMB_A_SET)
        {
            if (col[3] > 1.0f) col[3] = 1.0f;
            if (col[3] < 0.0f) col[3] = 0.0f;
            v->a = (BYTE)(255.0f * col[3]);
        }
        if (cmb & CMB_SETSHADE_SHADEALPHA)
        {
            v->r = v->g = v->b = v->a;
        }
        if (cmb & CMB_SUB)
        {
            int r = v->r - (int)(255.0f * rdp.coladd[0]);
            int g = v->g - (int)(255.0f * rdp.coladd[1]);
            int b = v->b - (int)(255.0f * rdp.coladd[2]);
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
            v->r = (BYTE)r;
            v->g = (BYTE)g;
            v->b = (BYTE)b;
        }
        if (cmb & CMB_A_SUB)
        {
            int a = v->a - (int)(255.0f * rdp.coladd[3]);
            if (a < 0) a = 0;
            v->a = (BYTE)a;
        }
        
        if (cmb & CMB_ADD)
        {
            int r = v->r + (int)(255.0f * rdp.coladd[0]);
            int g = v->g + (int)(255.0f * rdp.coladd[1]);
            int b = v->b + (int)(255.0f * rdp.coladd[2]);
            if (r > 255) r = 255;
            if (g > 255) g = 255;
            if (b > 255) b = 255;
            v->r = (BYTE)r;
            v->g = (BYTE)g;
            v->b = (BYTE)b;
        }
        if (cmb & CMB_A_ADD)
        {
            int a = v->a + (int)(255.0f * rdp.coladd[3]);
            if (a > 255) a = 255;
            v->a = (BYTE)a;
        }
        if (cmb & CMB_COL_SUB_OWN)
        {
            int r = (BYTE)(255.0f * rdp.coladd[0]) - v->r;
            int g = (BYTE)(255.0f * rdp.coladd[1]) - v->g;
            int b = (BYTE)(255.0f * rdp.coladd[2]) - v->b;
            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;
            v->r = (BYTE)r;
            v->g = (BYTE)g;
            v->b = (BYTE)b;
        }
        if (cmb & CMB_MULT)
        {
            if (col[0] > 1.0f) col[0] = 1.0f;
            if (col[1] > 1.0f) col[1] = 1.0f;
            if (col[2] > 1.0f) col[2] = 1.0f;
            if (col[0] < 0.0f) col[0] = 0.0f;
            if (col[1] < 0.0f) col[1] = 0.0f;
            if (col[2] < 0.0f) col[2] = 0.0f;
            v->r = (BYTE)(v->r * col[0]);
            v->g = (BYTE)(v->g * col[1]);
            v->b = (BYTE)(v->b * col[2]);
        }
        if (cmb & CMB_A_MULT)
        {
            if (col[3] > 1.0f) col[3] = 1.0f;
            if (col[3] < 0.0f) col[3] = 0.0f;
            v->a = (BYTE)(v->a * col[3]);
        }
        if (cmb & CMB_MULT_OWN_ALPHA)
        {
            float percent = v->a / 255.0f;
            v->r = (BYTE)(v->r * percent);
            v->g = (BYTE)(v->g * percent);
            v->b = (BYTE)(v->b * percent);
        }
        v->shade_mods_allowed = 0;
    }
    cmb = rdp.cmb_flags_2;
    if (cmb & CMB_INTER)
    {
        v->r = (BYTE)(rdp.col_2[0] * rdp.shade_factor * 255.0f + v->r * (1.0f - rdp.shade_factor));
        v->g = (BYTE)(rdp.col_2[1] * rdp.shade_factor * 255.0f + v->g * (1.0f - rdp.shade_factor));
        v->b = (BYTE)(rdp.col_2[2] * rdp.shade_factor * 255.0f + v->b * (1.0f - rdp.shade_factor));
        v->shade_mods_allowed = 0;
    } 
} 


static long dzdx = 0;

void DrawTri (VERTEX **vtx, WORD linew)
{
    if (settings.fb_depth_render && linew == 0)
    {
        float X0 =  vtx[0]->sx / rdp.scale_x;
        float Y0 =  vtx[0]->sy / rdp.scale_y;
        float X1 =  vtx[1]->sx / rdp.scale_x;
        float Y1 =  vtx[1]->sy / rdp.scale_y;
        float X2 =  vtx[2]->sx / rdp.scale_x;
        float Y2 =  vtx[2]->sy / rdp.scale_y;
        float diff12 = Y1 - Y2;
        float diff02 = Y0 - Y2;
        
        
        double denom = ((X0 - X2) * diff12 -
            (X1 - X2) * diff02);
        if(denom*denom > 0.0) 
        {
            dzdx = (long)(((vtx[0]->sz - vtx[2]->sz) * diff12 -
                (vtx[1]->sz - vtx[2]->sz) * diff02) / denom * 65536.0);
        }
        else
            dzdx = 0;
    }
    else
        dzdx = 0;
    
    for (int i=0; i<3; i++)
    {
        VERTEX *v = vtx[i];
        
        if (v->uv_calculated != rdp.tex_ctr)
        {
#ifdef EXTREME_LOGGING
            FRDP(" * CALCULATING VERTEX U/V: %d\n", v->number);
#endif
            v->uv_calculated = rdp.tex_ctr;
            
            if (!(rdp.geom_mode & 0x00020000))
            {
                if (!(rdp.geom_mode & 0x00000200))
                {
                    if (rdp.geom_mode & 0x00000004) // flat shading
                    {
#ifdef EXTREME_LOGGING
                        RDP(" * Flat shaded\n");
#endif
                        v->a = vtx[0]->a;
                        v->b = vtx[0]->b;
                        v->g = vtx[0]->g;
                        v->r = vtx[0]->r;
                    }
                    else  // prim color
                    {
#ifdef EXTREME_LOGGING
                        FRDP(" * Prim shaded %08lx\n", rdp.prim_color);
#endif
                        v->a = (BYTE)(rdp.prim_color & 0xFF);
                        v->b = (BYTE)((rdp.prim_color >> 8) & 0xFF);
                        v->g = (BYTE)((rdp.prim_color >> 16) & 0xFF);
                        v->r = (BYTE)((rdp.prim_color >> 24) & 0xFF);
                    }
                }
            }
            
            // Fix texture coordinates
            v->u1 = v->u0 = v->ou;
            v->v1 = v->v0 = v->ov;
            
            if (rdp.tex >= 1 && rdp.cur_cache[0])
            {
                if (rdp.hires_tex && rdp.hires_tex->tile == 0)
                {
                    v->u0 += rdp.hires_tex->u_shift + rdp.hires_tex->tile_uls;
                    v->v0 += rdp.hires_tex->v_shift + rdp.hires_tex->tile_ult;
                }
                
                if (rdp.tiles[rdp.cur_tile].shift_s)
                {
                    if (rdp.tiles[rdp.cur_tile].shift_s > 10)
                        v->u0 *= (float)(1 << (16 - rdp.tiles[rdp.cur_tile].shift_s));
                    else
                        v->u0 /= (float)(1 << rdp.tiles[rdp.cur_tile].shift_s);
                }
                if (rdp.tiles[rdp.cur_tile].shift_t)
                {
                    if (rdp.tiles[rdp.cur_tile].shift_t > 10)
                        v->v0 *= (float)(1 << (16 - rdp.tiles[rdp.cur_tile].shift_t));
                    else
                        v->v0 /= (float)(1 << rdp.tiles[rdp.cur_tile].shift_t);
                }
                
                if (rdp.hires_tex && rdp.hires_tex->tile == 0)
                {
                    if (rdp.hires_tex->tile_uls != (int)rdp.tiles[rdp.cur_tile].f_ul_s)
                      v->u0 -= rdp.tiles[rdp.cur_tile].f_ul_s;
                    v->u0 *= rdp.hires_tex->u_scale;
                    v->v0 *= rdp.hires_tex->u_scale;
                    v->u0 -= 0.45f;
                    v->v0 -= 0.45f;
                    FRDP("hires_tex t0: (%f, %f)->(%f, %f)\n", v->ou, v->ov, v->u0, v->v0);
                }
                else
                {
                    v->u0 -= rdp.tiles[rdp.cur_tile].f_ul_s;
                    v->v0 -= rdp.tiles[rdp.cur_tile].f_ul_t;
                    v->u0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_x * v->u0;
                    v->v0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_y * v->v0;
                }
                v->u0_w = v->u0 / v->w;
                v->v0_w = v->v0 / v->w;
            }
            
            if (rdp.tex >= 2 && rdp.cur_cache[1])
            {
                if (rdp.hires_tex && rdp.hires_tex->tile == 1)
                {
                    v->u1 += rdp.hires_tex->u_shift + rdp.hires_tex->tile_uls;
                    v->v1 += rdp.hires_tex->v_shift + rdp.hires_tex->tile_ult;
                }
                if (rdp.tiles[rdp.cur_tile+1].shift_s)
                {
                    if (rdp.tiles[rdp.cur_tile+1].shift_s > 10)
                        v->u1 *= (float)(1 << (16 - rdp.tiles[rdp.cur_tile+1].shift_s));
                    else
                        v->u1 /= (float)(1 << rdp.tiles[rdp.cur_tile+1].shift_s);
                }
                if (rdp.tiles[rdp.cur_tile+1].shift_t)
                {
                    if (rdp.tiles[rdp.cur_tile+1].shift_t > 10)
                        v->v1 *= (float)(1 << (16 - rdp.tiles[rdp.cur_tile+1].shift_t));
                    else
                        v->v1 /= (float)(1 << rdp.tiles[rdp.cur_tile+1].shift_t);
                }
                
                if (rdp.hires_tex && rdp.hires_tex->tile == 1)
                {
                    if (rdp.hires_tex->tile_uls != (int)rdp.tiles[rdp.cur_tile].f_ul_s)
                      v->u1 -= rdp.tiles[rdp.cur_tile].f_ul_s;
                    v->u1 *= rdp.hires_tex->u_scale;
                    v->v1 *= rdp.hires_tex->u_scale;
                    v->u1 -= 0.45f;
                    v->v1 -= 0.45f;
                    FRDP("hires_tex t1: (%f, %f)->(%f, %f)\n", v->ou, v->ov, v->u0, v->v0);
                }
                else
                {
                v->u1 -= rdp.tiles[rdp.cur_tile+1].f_ul_s;
                v->v1 -= rdp.tiles[rdp.cur_tile+1].f_ul_t;
                v->u1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_x * v->u1;
                v->v1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_y * v->v1;
                }
                
                v->u1_w = v->u1 / v->w;
                v->v1_w = v->v1 / v->w;
            }
            //      FRDP(" * CALCULATING VERTEX U/V: %d  u0: %f, v0: %f, u1: %f, v1: %f\n", v->number, v->u0, v->v0, v->u1, v->v1);
        }
        if (v->shade_mods_allowed)
            apply_shade_mods (v);
  } //for
  
  rdp.clip = 0;
  
  if ((vtx[0]->scr_off & 16) ||
      (vtx[1]->scr_off & 16) ||
      (vtx[2]->scr_off & 16))
      rdp.clip |= CLIP_ZMIN;
  
  vtx[0]->not_zclipped = vtx[1]->not_zclipped = vtx[2]->not_zclipped = 1;
  
  if (rdp.cur_cache[0] && (rdp.tex & 1) && (rdp.cur_cache[0]->splits > 1) && !rdp.hires_tex && !rdp.clip)
  {
      int index,i,j, min_256,max_256, cur_256,left_256,right_256;
      float percent;
      
      min_256 = min((int)vtx[0]->u0,(int)vtx[1]->u0); // bah, don't put two mins on one line
      min_256 = min(min_256,(int)vtx[2]->u0) >> 8;  // or it will be calculated twice
      
      max_256 = max((int)vtx[0]->u0,(int)vtx[1]->u0); // not like it makes much difference
      max_256 = max(max_256,(int)vtx[2]->u0) >> 8;  // anyway :P
      
      for (cur_256=min_256; cur_256<=max_256; cur_256++)
      {
          left_256 = cur_256 << 8;
          right_256 = (cur_256+1) << 8;
          
          // Set vertex buffers
          rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
          rdp.vtxbuf2 = rdp.vtx2;
          rdp.vtx_buffer = 0;
          rdp.n_global = 3;
          index = 0;
          
          // ** Left plane **
          for (i=0; i<3; i++)
          {
              j = i+1;
              if (j == 3) j = 0;
              
              VERTEX *v1 = vtx[i];
              VERTEX *v2 = vtx[j];
              
              if (v1->u0 >= left_256)
              {
                  if (v2->u0 >= left_256)   // Both are in, save the last one
                  {
                      rdp.vtxbuf[index] = *v2;
                      rdp.vtxbuf[index].u0 -= left_256;
                      rdp.vtxbuf[index++].v0 += rdp.cur_cache[0]->c_scl_y * (cur_256 * rdp.cur_cache[0]->splitheight);
                  }
                  else      // First is in, second is out, save intersection
                  {
                      percent = (left_256 - v1->u0) / (v2->u0 - v1->u0);
                      rdp.vtxbuf[index].x = v1->x + (v2->x - v1->x) * percent;
                      rdp.vtxbuf[index].y = v1->y + (v2->y - v1->y) * percent;
                      rdp.vtxbuf[index].z = v1->z + (v2->z - v1->z) * percent;
                      rdp.vtxbuf[index].w = v1->w + (v2->w - v1->w) * percent;
                      rdp.vtxbuf[index].f = v1->f + (v2->f - v1->f) * percent;
                      rdp.vtxbuf[index].u0 = 0.5f;
                      rdp.vtxbuf[index].v0 = v1->v0 + (v2->v0 - v1->v0) * percent +
                          rdp.cur_cache[0]->c_scl_y * cur_256 * rdp.cur_cache[0]->splitheight;
                      rdp.vtxbuf[index].u1 = v1->u1 + (v2->u1 - v1->u1) * percent;
                      rdp.vtxbuf[index].v1 = v1->v1 + (v2->v1 - v1->v1) * percent;
                      rdp.vtxbuf[index].b = (BYTE)(v1->b + (v2->b - v1->b) * percent);
                      rdp.vtxbuf[index].g = (BYTE)(v1->g + (v2->g - v1->g) * percent);
                      rdp.vtxbuf[index].r = (BYTE)(v1->r + (v2->r - v1->r) * percent);
                      rdp.vtxbuf[index++].a = (BYTE)(v1->a + (v2->a - v1->a) * percent);
                  }
              }
              else
              {
                  //if (v2->u0 < left_256)  // Both are out, save nothing
                  if (v2->u0 >= left_256) // First is out, second is in, save intersection & in point
                  {
                      percent = (left_256 - v2->u0) / (v1->u0 - v2->u0);
                      rdp.vtxbuf[index].x = v2->x + (v1->x - v2->x) * percent;
                      rdp.vtxbuf[index].y = v2->y + (v1->y - v2->y) * percent;
                      rdp.vtxbuf[index].z = v2->z + (v1->z - v2->z) * percent;
                      rdp.vtxbuf[index].w = v2->w + (v1->w - v2->w) * percent;
                      rdp.vtxbuf[index].f = v2->f + (v1->f - v2->f) * percent;
                      rdp.vtxbuf[index].u0 = 0.5f;
                      rdp.vtxbuf[index].v0 = v2->v0 + (v1->v0 - v2->v0) * percent +
                          rdp.cur_cache[0]->c_scl_y * cur_256 * rdp.cur_cache[0]->splitheight;
                      rdp.vtxbuf[index].u1 = v2->u1 + (v1->u1 - v2->u1) * percent;
                      rdp.vtxbuf[index].v1 = v2->v1 + (v1->v1 - v2->v1) * percent;
                      rdp.vtxbuf[index].b = (BYTE)(v2->b + (v1->b - v2->b) * percent);
                      rdp.vtxbuf[index].g = (BYTE)(v2->g + (v1->g - v2->g) * percent);
                      rdp.vtxbuf[index].r = (BYTE)(v2->r + (v1->r - v2->r) * percent);
                      rdp.vtxbuf[index++].a = (BYTE)(v2->a + (v1->a - v2->a) * percent);
                      
                      // Save the in point
                      rdp.vtxbuf[index] = *v2;
                      rdp.vtxbuf[index].u0 -= left_256;
                      rdp.vtxbuf[index++].v0 += rdp.cur_cache[0]->c_scl_y * (cur_256 * rdp.cur_cache[0]->splitheight);
                  }
              }
          }
          rdp.n_global = index;
          
          rdp.vtxbuf = rdp.vtx2;  // now vtx1 holds the value, & vtx2 is the destination
          rdp.vtxbuf2 = rdp.vtx1;
          rdp.vtx_buffer ^= 1;
          index = 0;
          
          for (i=0; i<rdp.n_global; i++)
          {
              j = i+1;
              if (j == rdp.n_global) j = 0;
              
              VERTEX *v1 = &rdp.vtxbuf2[i];
              VERTEX *v2 = &rdp.vtxbuf2[j];
              
              // ** Right plane **
              if (v1->u0 <= 256.0f)
              {
                  if (v2->u0 <= 256.0f)   // Both are in, save the last one
                  {
                      rdp.vtxbuf[index] = *v2;
                      rdp.vtxbuf[index++].not_zclipped = 0;
                  }
                  else      // First is in, second is out, save intersection
                  {
                      percent = (right_256 - v1->u0) / (v2->u0 - v1->u0);
                      rdp.vtxbuf[index].x = v1->x + (v2->x - v1->x) * percent;
                      rdp.vtxbuf[index].y = v1->y + (v2->y - v1->y) * percent;
                      rdp.vtxbuf[index].z = v1->z + (v2->z - v1->z) * percent;
                      rdp.vtxbuf[index].w = v1->w + (v2->w - v1->w) * percent;
                      rdp.vtxbuf[index].f = v1->f + (v2->f - v1->f) * percent;
                      rdp.vtxbuf[index].u0 = 255.5f;
                      rdp.vtxbuf[index].v0 = v1->v0 + (v2->v0 - v1->v0) * percent;
                      rdp.vtxbuf[index].u1 = v1->u1 + (v2->u1 - v1->u1) * percent;
                      rdp.vtxbuf[index].v1 = v1->v1 + (v2->v1 - v1->v1) * percent;
                      rdp.vtxbuf[index].b = (BYTE)(v1->b + (v2->b - v1->b) * percent);
                      rdp.vtxbuf[index].g = (BYTE)(v1->g + (v2->g - v1->g) * percent);
                      rdp.vtxbuf[index].r = (BYTE)(v1->r + (v2->r - v1->r) * percent);
                      rdp.vtxbuf[index].a = (BYTE)(v1->a + (v2->a - v1->a) * percent);
                      rdp.vtxbuf[index++].not_zclipped = 0;
                  }
              }
              else
              {
                  //if (v2->u0 > 256.0f)  // Both are out, save nothing
                  if (v2->u0 <= 256.0f) // First is out, second is in, save intersection & in point
                  {
                      percent = (right_256 - v2->u0) / (v1->u0 - v2->u0);
                      rdp.vtxbuf[index].x = v2->x + (v1->x - v2->x) * percent;
                      rdp.vtxbuf[index].y = v2->y + (v1->y - v2->y) * percent;
                      rdp.vtxbuf[index].z = v2->z + (v1->z - v2->z) * percent;
                      rdp.vtxbuf[index].w = v2->w + (v1->w - v2->w) * percent;
                      rdp.vtxbuf[index].f = v2->f + (v1->f - v2->f) * percent;
                      rdp.vtxbuf[index].u0 = 255.5f;
                      rdp.vtxbuf[index].v0 = v2->v0 + (v1->v0 - v2->v0) * percent;
                      rdp.vtxbuf[index].u1 = v2->u1 + (v1->u1 - v2->u1) * percent;
                      rdp.vtxbuf[index].v1 = v2->v1 + (v1->v1 - v2->v1) * percent;
                      rdp.vtxbuf[index].b = (BYTE)(v2->b + (v1->b - v2->b) * percent);
                      rdp.vtxbuf[index].g = (BYTE)(v2->g + (v1->g - v2->g) * percent);
                      rdp.vtxbuf[index].r = (BYTE)(v2->r + (v1->r - v2->r) * percent);
                      rdp.vtxbuf[index].a = (BYTE)(v2->a + (v1->a - v2->a) * percent);
                      rdp.vtxbuf[index++].not_zclipped = 0;
                      
                      // Save the in point
                      rdp.vtxbuf[index] = *v2;
                      rdp.vtxbuf[index++].not_zclipped = 0;
                  }
              }
          }
          rdp.n_global = index;
          
          do_triangle_stuff (linew);
    }
  }
  else
  {
      // Set vertex buffers
      rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
      rdp.vtxbuf2 = rdp.vtx2;
      rdp.vtx_buffer = 0;
      rdp.n_global = 3;
      
      rdp.vtxbuf[0] = *vtx[0];
      rdp.vtxbuf[1] = *vtx[1];
      rdp.vtxbuf[2] = *vtx[2];
      
      do_triangle_stuff (linew);
  }
}

void do_triangle_stuff (WORD linew) // what else?? do the triangle stuff :P (to keep from writing code twice)
{
    int i;
    
    //  if (rdp.zsrc != 1)
    clip_z ();
    
    for (i=0; i<rdp.n_global; i++)
    {
        if (rdp.vtxbuf[i].not_zclipped)// && rdp.zsrc != 1)
        {
#ifdef EXTREME_LOGGING
            FRDP (" * NOT ZCLIPPPED: %d\n", rdp.vtxbuf[i].number);
#endif
            rdp.vtxbuf[i].x = rdp.vtxbuf[i].sx;
            rdp.vtxbuf[i].y = rdp.vtxbuf[i].sy;
            rdp.vtxbuf[i].z = rdp.vtxbuf[i].sz; 
            rdp.vtxbuf[i].q = rdp.vtxbuf[i].oow;
            rdp.vtxbuf[i].u0 = rdp.vtxbuf[i].u0_w;
            rdp.vtxbuf[i].v0 = rdp.vtxbuf[i].v0_w;
            rdp.vtxbuf[i].u1 = rdp.vtxbuf[i].u1_w;
            rdp.vtxbuf[i].v1 = rdp.vtxbuf[i].v1_w;
        }
        else
        {
#ifdef EXTREME_LOGGING
            FRDP (" * ZCLIPPED: %d\n", rdp.vtxbuf[i].number);
#endif
            rdp.vtxbuf[i].q = 1.0f / rdp.vtxbuf[i].w;
            rdp.vtxbuf[i].x = rdp.view_trans[0] + rdp.vtxbuf[i].x * rdp.vtxbuf[i].q * rdp.view_scale[0];
            rdp.vtxbuf[i].y = rdp.view_trans[1] + rdp.vtxbuf[i].y * rdp.vtxbuf[i].q * rdp.view_scale[1];
            rdp.vtxbuf[i].z = rdp.view_trans[2] + rdp.vtxbuf[i].z * rdp.vtxbuf[i].q * rdp.view_scale[2];
            if (rdp.tex >= 1)
            {
                rdp.vtxbuf[i].u0 *= rdp.vtxbuf[i].q;
                rdp.vtxbuf[i].v0 *= rdp.vtxbuf[i].q;
            }
            if (rdp.tex >= 2)
            {
                rdp.vtxbuf[i].u1 *= rdp.vtxbuf[i].q;
                rdp.vtxbuf[i].v1 *= rdp.vtxbuf[i].q;
            }
        }
        
        if (rdp.zsrc == 1)
            rdp.vtxbuf[i].z = rdp.prim_depth;
        
        // Don't remove clipping, or it will freeze
        if (rdp.vtxbuf[i].x > rdp.scissor.lr_x) rdp.clip |= CLIP_XMAX;
        if (rdp.vtxbuf[i].x < rdp.scissor.ul_x) rdp.clip |= CLIP_XMIN;
        if (rdp.vtxbuf[i].y > rdp.scissor.lr_y) rdp.clip |= CLIP_YMAX;
        if (rdp.vtxbuf[i].y < rdp.scissor.ul_y) rdp.clip |= CLIP_YMIN;
    }
    
    clip_tri (linew);
}

void do_triangle_stuff_2 (WORD linew)
{
    rdp.clip = 0;
    
    for (int i=0; i<rdp.n_global; i++)
    {
        // Don't remove clipping, or it will freeze
        if (rdp.vtxbuf[i].x > rdp.scissor.lr_x) rdp.clip |= CLIP_XMAX;
        if (rdp.vtxbuf[i].x < rdp.scissor.ul_x) rdp.clip |= CLIP_XMIN;
        if (rdp.vtxbuf[i].y > rdp.scissor.lr_y) rdp.clip |= CLIP_YMAX;
        if (rdp.vtxbuf[i].y < rdp.scissor.ul_y) rdp.clip |= CLIP_YMIN;
    }
    
    clip_tri (linew);
}

//
// clip_z - clips along the z-axis, also copies the vertex buffer for clip_tri
//   * ALWAYS * processes it, even if it does not need z-clipping.  It needs
//   to copy the buffer anyway.
//

void clip_z ()
{
    int i,j,index,n=rdp.n_global;
    float percent;
    
    if (rdp.clip & CLIP_ZMIN)
    {
        // Swap vertex buffers
        VERTEX *tmp = rdp.vtxbuf2;
        rdp.vtxbuf2 = rdp.vtxbuf;
        rdp.vtxbuf = tmp;
        rdp.vtx_buffer ^= 1;
        index = 0;
        
        // Check the vertices for clipping
        for (i=0; i<n; i++)
        {
            j = i+1;
            if (j == n) j = 0;
            
            if (Vi.w >= 0.01f)
            {
                if (Vj.w >= 0.01f)    // Both are in, save the last one
                {
                    rdp.vtxbuf[index] = Vj;
                    rdp.vtxbuf[index++].not_zclipped = 1;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (-Vi.w) / (Vj.w - Vi.w);
                    rdp.vtxbuf[index].not_zclipped = 0;
                    rdp.vtxbuf[index].x = Vi.x + (Vj.x - Vi.x) * percent;
                    rdp.vtxbuf[index].y = Vi.y + (Vj.y - Vi.y) * percent;
                    rdp.vtxbuf[index].z = Vi.z + (Vj.z - Vi.z) * percent;
                    rdp.vtxbuf[index].f = Vi.f + (Vj.f - Vi.f) * percent;
                    rdp.vtxbuf[index].w = 0.01f;
                    rdp.vtxbuf[index].u0 = Vi.u0 + (Vj.u0 - Vi.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vi.v0 + (Vj.v0 - Vi.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vi.u1 + (Vj.u1 - Vi.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vi.v1 + (Vj.v1 - Vi.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vi.b + (Vj.b - Vi.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vi.g + (Vj.g - Vi.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vi.r + (Vj.r - Vi.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vi.a + (Vj.a - Vi.a) * percent);
                }
            }
            else
            {
                //if (Vj.w < 0.01f) // Both are out, save nothing
                if (Vj.w >= 0.01f)  // First is out, second is in, save intersection & in point
                {
                    percent = (-Vj.w) / (Vi.w - Vj.w);
                    rdp.vtxbuf[index].not_zclipped = 0;
                    rdp.vtxbuf[index].x = Vj.x + (Vi.x - Vj.x) * percent;
                    rdp.vtxbuf[index].y = Vj.y + (Vi.y - Vj.y) * percent;
                    rdp.vtxbuf[index].z = Vj.z + (Vi.z - Vj.z) * percent;
                    rdp.vtxbuf[index].f = Vj.f + (Vi.f - Vj.f) * percent;
                    rdp.vtxbuf[index].w = 0.01f;
                    rdp.vtxbuf[index].u0 = Vj.u0 + (Vi.u0 - Vj.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vj.v0 + (Vi.v0 - Vj.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vj.u1 + (Vi.u1 - Vj.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vj.v1 + (Vi.v1 - Vj.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vj.b + (Vi.b - Vj.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vj.g + (Vi.g - Vj.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vj.r + (Vi.r - Vj.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vj.a + (Vi.a - Vj.a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index] = Vj;
                    rdp.vtxbuf[index++].not_zclipped = 1;
                }
            }
        }
        rdp.n_global = index;
    }
}

static void CalculateLOD(VERTEX **v, int n)
{
    //rdp.update |= UPDATE_TEXTURE;
    /*
    if (rdp.lod_calculated)
    {
    float detailmax;
    if (dc0_detailmax < 0.5)
    detailmax = rdp.lod_fraction;
    else
    detailmax = 1.0f - rdp.lod_fraction;
    grTexDetailControl (GR_TMU0, dc0_lodbias, dc0_detailscale, detailmax);
    if (num_tmu == 2)   
    grTexDetailControl (GR_TMU1, dc1_lodbias, dc1_detailscale, detailmax);
    return;
    }
    */
    float deltaS, deltaT; 
    float deltaX, deltaY; 
    double deltaTexels, deltaPixels, lodFactor = 0;
    double intptr;
    float s_scale = rdp.tiles[rdp.cur_tile].width / 255.0f;
    float t_scale = rdp.tiles[rdp.cur_tile].height / 255.0f;
    if (settings.lodmode == 1)
    {
        deltaS = (v[1]->u0/v[1]->q - v[0]->u0/v[0]->q) * s_scale;
        deltaT = (v[1]->v0/v[1]->q - v[0]->v0/v[0]->q) * t_scale;
        deltaTexels = sqrt( deltaS * deltaS + deltaT * deltaT );
        
        deltaX = (v[1]->x - v[0]->x)/rdp.scale_x;
        deltaY = (v[1]->y - v[0]->y)/rdp.scale_y;
        deltaPixels = sqrt( deltaX * deltaX + deltaY * deltaY );
        
        lodFactor = deltaTexels / deltaPixels;
    }
    else
    {
        int i, j;
        for (i = 0; i < n; i++)
        {
            j = (i < n-1) ? i + 1 : 0;
            
            deltaS = (v[j]->u0/v[j]->q - v[i]->u0/v[i]->q) * s_scale;
            deltaT = (v[j]->v0/v[j]->q - v[i]->v0/v[i]->q) * t_scale;
            //    deltaS = v[j]->ou - v[i]->ou;
            //    deltaT = v[j]->ov - v[i]->ov;
            deltaTexels = sqrt( deltaS * deltaS + deltaT * deltaT );
            
            deltaX = (v[j]->x - v[i]->x)/rdp.scale_x;
            deltaY = (v[j]->y - v[i]->y)/rdp.scale_y;
            deltaPixels = sqrt( deltaX * deltaX + deltaY * deltaY );
            
            lodFactor += deltaTexels / deltaPixels;
        }
        // Divide by n (n edges) to find average
        lodFactor = lodFactor / n;
    }
    long ilod = (long)lodFactor;
    int lod_tile = min((int)(log((double)ilod)/log(2.0)), rdp.cur_tile + rdp.mipmap_level);
    float lod_fraction = 1.0f;
    if (lod_tile < rdp.cur_tile + rdp.mipmap_level)
    {
        lod_fraction = max((float)modf(lodFactor / pow(2.0f,lod_tile),&intptr), rdp.prim_lodmin / 255.0f);
    }
    float detailmax;
    if (cmb.dc0_detailmax < 0.5f)
        detailmax = lod_fraction;
    else
        detailmax = 1.0f - lod_fraction;
    grTexDetailControl (GR_TMU0, cmb.dc0_lodbias, cmb.dc0_detailscale, detailmax);
    if (num_tmu == 2)   
        grTexDetailControl (GR_TMU1, cmb.dc1_lodbias, cmb.dc1_detailscale, detailmax);
    FRDP("CalculateLOD factor: %f, tile: %d, lod_fraction: %f\n", (float)lodFactor, lod_tile, lod_fraction);
}

static void DepthBuffer(VERTEX ** vtx, int n)
{
    if (settings.RE2)
    {
        for(int i=0; i<n; i++) 
        {
            int fz = (int)(vtx[i]->z*8.0f+0.5f);
            if (fz < 0) fz = 0;
            else if (fz >= 0x40000) fz = 0x40000 - 1;
            vtx[i]->z = (float)zLUT[fz];
        }
        return;
    }
    if (settings.fb_depth_render && dzdx && (rdp.flags & ZBUF_UPDATE))
    {
        vertexi v[12];
        
        for(int i=0; i<n; i++) 
        {
            v[i].x = (long)(vtx[i]->x / rdp.scale_x * 65536.0);
            v[i].y = (long)(vtx[i]->y / rdp.scale_y * 65536.0);
            v[i].z = (long)(vtx[i]->z * 65536.0);
        }
        Rasterize(v, n, dzdx);
    }
    for(int i=0; i<n; i++) 
        vtx[i]->z = ScaleZ(vtx[i]->z);
}

void clip_tri (WORD linew)
{
    int i,j,index,n=rdp.n_global;
    float percent;
    
    // rdp.vtxbuf and rdp.vtxbuf2 were set by clip_z
    
    // Check which clipping is needed
    if (rdp.clip & CLIP_XMAX) // right of the screen
    {
        // Swap vertex buffers
        VERTEX *tmp = rdp.vtxbuf2;
        rdp.vtxbuf2 = rdp.vtxbuf;
        rdp.vtxbuf = tmp;
        rdp.vtx_buffer ^= 1;
        index = 0;
        
        // Check the vertices for clipping
        for (i=0; i<n; i++)
        {
            j = i+1;
            if (j == n) j = 0;
            
            if (Vi.x <= rdp.scissor.lr_x)
            {
                if (Vj.x <= rdp.scissor.lr_x)   // Both are in, save the last one
                {
                    rdp.vtxbuf[index++] = Vj;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (rdp.scissor.lr_x - Vi.x) / (Vj.x - Vi.x);
                    rdp.vtxbuf[index].x = (float)rdp.scissor.lr_x + 0.001f;
                    rdp.vtxbuf[index].y = Vi.y + (Vj.y - Vi.y) * percent;
                    rdp.vtxbuf[index].z = Vi.z + (Vj.z - Vi.z) * percent;
                    rdp.vtxbuf[index].q = Vi.q + (Vj.q - Vi.q) * percent;
                    rdp.vtxbuf[index].f = Vi.f + (Vj.f - Vi.f) * percent;
                    rdp.vtxbuf[index].u0 = Vi.u0 + (Vj.u0 - Vi.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vi.v0 + (Vj.v0 - Vi.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vi.u1 + (Vj.u1 - Vi.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vi.v1 + (Vj.v1 - Vi.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vi.b + (Vj.b - Vi.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vi.g + (Vj.g - Vi.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vi.r + (Vj.r - Vi.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vi.a + (Vj.a - Vi.a) * percent);
                }
            }
            else
            {
                //if (Vj.x > rdp.scissor.lr_x)  // Both are out, save nothing
                if (Vj.x <= rdp.scissor.lr_x) // First is out, second is in, save intersection & in point
                {
                    percent = (rdp.scissor.lr_x - Vj.x) / (Vi.x - Vj.x);
                    rdp.vtxbuf[index].x = (float)rdp.scissor.lr_x + 0.001f;
                    rdp.vtxbuf[index].y = Vj.y + (Vi.y - Vj.y) * percent;
                    rdp.vtxbuf[index].z = Vj.z + (Vi.z - Vj.z) * percent;
                    rdp.vtxbuf[index].q = Vj.q + (Vi.q - Vj.q) * percent;
                    rdp.vtxbuf[index].f = Vj.f + (Vi.f - Vj.f) * percent;
                    rdp.vtxbuf[index].u0 = Vj.u0 + (Vi.u0 - Vj.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vj.v0 + (Vi.v0 - Vj.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vj.u1 + (Vi.u1 - Vj.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vj.v1 + (Vi.v1 - Vj.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vj.b + (Vi.b - Vj.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vj.g + (Vi.g - Vj.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vj.r + (Vi.r - Vj.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vj.a + (Vi.a - Vj.a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index++] = Vj;
                }
            }
        }
        n = index;
    }
    if (rdp.clip & CLIP_XMIN) // left of the screen
    {
        // Swap vertex buffers
        VERTEX *tmp = rdp.vtxbuf2;
        rdp.vtxbuf2 = rdp.vtxbuf;
        rdp.vtxbuf = tmp;
        rdp.vtx_buffer ^= 1;
        index = 0;
        
        // Check the vertices for clipping
        for (i=0; i<n; i++)
        {
            j = i+1;
            if (j == n) j = 0;
            
            if (Vi.x >= rdp.scissor.ul_x)
            {
                if (Vj.x >= rdp.scissor.ul_x)   // Both are in, save the last one
                {
                    rdp.vtxbuf[index++] = Vj;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (rdp.scissor.ul_x - Vi.x) / (Vj.x - Vi.x);
                    rdp.vtxbuf[index].x = (float)rdp.scissor.ul_x + 0.001f;
                    rdp.vtxbuf[index].y = Vi.y + (Vj.y - Vi.y) * percent;
                    rdp.vtxbuf[index].z = Vi.z + (Vj.z - Vi.z) * percent;
                    rdp.vtxbuf[index].q = Vi.q + (Vj.q - Vi.q) * percent;
                    rdp.vtxbuf[index].f = Vi.f + (Vj.f - Vi.f) * percent;
                    rdp.vtxbuf[index].u0 = Vi.u0 + (Vj.u0 - Vi.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vi.v0 + (Vj.v0 - Vi.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vi.u1 + (Vj.u1 - Vi.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vi.v1 + (Vj.v1 - Vi.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vi.b + (Vj.b - Vi.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vi.g + (Vj.g - Vi.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vi.r + (Vj.r - Vi.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vi.a + (Vj.a - Vi.a) * percent);
                }
            }
            else
            {
                //if (Vj.x < rdp.scissor.ul_x)  // Both are out, save nothing
                if (Vj.x >= rdp.scissor.ul_x) // First is out, second is in, save intersection & in point
                {
                    percent = (rdp.scissor.ul_x - Vj.x) / (Vi.x - Vj.x);
                    rdp.vtxbuf[index].x = (float)rdp.scissor.ul_x + 0.001f;
                    rdp.vtxbuf[index].y = Vj.y + (Vi.y - Vj.y) * percent;
                    rdp.vtxbuf[index].z = Vj.z + (Vi.z - Vj.z) * percent;
                    rdp.vtxbuf[index].q = Vj.q + (Vi.q - Vj.q) * percent;
                    rdp.vtxbuf[index].f = Vj.f + (Vi.f - Vj.f) * percent;
                    rdp.vtxbuf[index].u0 = Vj.u0 + (Vi.u0 - Vj.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vj.v0 + (Vi.v0 - Vj.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vj.u1 + (Vi.u1 - Vj.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vj.v1 + (Vi.v1 - Vj.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vj.b + (Vi.b - Vj.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vj.g + (Vi.g - Vj.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vj.r + (Vi.r - Vj.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vj.a + (Vi.a - Vj.a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index++] = Vj;
                }
            }
        }
        n = index;
    }
    if (rdp.clip & CLIP_YMAX) // top of the screen
    {
        // Swap vertex buffers
        VERTEX *tmp = rdp.vtxbuf2;
        rdp.vtxbuf2 = rdp.vtxbuf;
        rdp.vtxbuf = tmp;
        rdp.vtx_buffer ^= 1;
        index = 0;
        
        // Check the vertices for clipping
        for (i=0; i<n; i++)
        {
            j = i+1;
            if (j == n) j = 0;
            
            if (Vi.y <= rdp.scissor.lr_y)
            {
                if (Vj.y <= rdp.scissor.lr_y)   // Both are in, save the last one
                {
                    rdp.vtxbuf[index++] = Vj;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (rdp.scissor.lr_y - Vi.y) / (Vj.y - Vi.y);
                    rdp.vtxbuf[index].x = Vi.x + (Vj.x - Vi.x) * percent;
                    rdp.vtxbuf[index].y = (float)rdp.scissor.lr_y + 0.001f;
                    rdp.vtxbuf[index].z = Vi.z + (Vj.z - Vi.z) * percent;
                    rdp.vtxbuf[index].q = Vi.q + (Vj.q - Vi.q) * percent;
                    rdp.vtxbuf[index].f = Vi.f + (Vj.f - Vi.f) * percent;
                    rdp.vtxbuf[index].u0 = Vi.u0 + (Vj.u0 - Vi.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vi.v0 + (Vj.v0 - Vi.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vi.u1 + (Vj.u1 - Vi.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vi.v1 + (Vj.v1 - Vi.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vi.b + (Vj.b - Vi.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vi.g + (Vj.g - Vi.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vi.r + (Vj.r - Vi.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vi.a + (Vj.a - Vi.a) * percent);
                }
            }
            else
            {
                //if (Vj.y > rdp.scissor.lr_y)  // Both are out, save nothing
                if (Vj.y <= rdp.scissor.lr_y) // First is out, second is in, save intersection & in point
                {
                    percent = (rdp.scissor.lr_y - Vj.y) / (Vi.y - Vj.y);
                    rdp.vtxbuf[index].x = Vj.x + (Vi.x - Vj.x) * percent;
                    rdp.vtxbuf[index].y = (float)rdp.scissor.lr_y + 0.001f;
                    rdp.vtxbuf[index].z = Vj.z + (Vi.z - Vj.z) * percent;
                    rdp.vtxbuf[index].q = Vj.q + (Vi.q - Vj.q) * percent;
                    rdp.vtxbuf[index].f = Vj.f + (Vi.f - Vj.f) * percent;
                    rdp.vtxbuf[index].u0 = Vj.u0 + (Vi.u0 - Vj.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vj.v0 + (Vi.v0 - Vj.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vj.u1 + (Vi.u1 - Vj.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vj.v1 + (Vi.v1 - Vj.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vj.b + (Vi.b - Vj.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vj.g + (Vi.g - Vj.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vj.r + (Vi.r - Vj.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vj.a + (Vi.a - Vj.a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index++] = Vj;
                }
            }
        }
        n = index;
    }
    if (rdp.clip & CLIP_YMIN) // bottom of the screen
    {
        // Swap vertex buffers
        VERTEX *tmp = rdp.vtxbuf2;
        rdp.vtxbuf2 = rdp.vtxbuf;
        rdp.vtxbuf = tmp;
        rdp.vtx_buffer ^= 1;
        index = 0;
        
        // Check the vertices for clipping
        for (i=0; i<n; i++)
        {
            j = i+1;
            if (j == n) j = 0;
            
            if (Vi.y >= rdp.scissor.ul_y)
            {
                if (Vj.y >= rdp.scissor.ul_y)   // Both are in, save the last one
                {
                    rdp.vtxbuf[index++] = Vj;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (rdp.scissor.ul_y - Vi.y) / (Vj.y - Vi.y);
                    rdp.vtxbuf[index].x = Vi.x + (Vj.x - Vi.x) * percent;
                    rdp.vtxbuf[index].y = (float)rdp.scissor.ul_y + 0.001f;
                    rdp.vtxbuf[index].z = Vi.z + (Vj.z - Vi.z) * percent;
                    rdp.vtxbuf[index].q = Vi.q + (Vj.q - Vi.q) * percent;
                    rdp.vtxbuf[index].f = Vi.f + (Vj.f - Vi.f) * percent;
                    rdp.vtxbuf[index].u0 = Vi.u0 + (Vj.u0 - Vi.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vi.v0 + (Vj.v0 - Vi.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vi.u1 + (Vj.u1 - Vi.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vi.v1 + (Vj.v1 - Vi.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vi.b + (Vj.b - Vi.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vi.g + (Vj.g - Vi.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vi.r + (Vj.r - Vi.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vi.a + (Vj.a - Vi.a) * percent);
                }
            }
            else
            {
                //if (Vj.y < rdp.scissor.ul_y)  // Both are out, save nothing
                if (Vj.y >= rdp.scissor.ul_y) // First is out, second is in, save intersection & in point
                {
                    percent = (rdp.scissor.ul_y - Vj.y) / (Vi.y - Vj.y);
                    rdp.vtxbuf[index].x = Vj.x + (Vi.x - Vj.x) * percent;
                    rdp.vtxbuf[index].y = (float)rdp.scissor.ul_y + 0.001f;
                    rdp.vtxbuf[index].z = Vj.z + (Vi.z - Vj.z) * percent;
                    rdp.vtxbuf[index].q = Vj.q + (Vi.q - Vj.q) * percent;
                    rdp.vtxbuf[index].f = Vj.f + (Vi.f - Vj.f) * percent;
                    rdp.vtxbuf[index].u0 = Vj.u0 + (Vi.u0 - Vj.u0) * percent;
                    rdp.vtxbuf[index].v0 = Vj.v0 + (Vi.v0 - Vj.v0) * percent;
                    rdp.vtxbuf[index].u1 = Vj.u1 + (Vi.u1 - Vj.u1) * percent;
                    rdp.vtxbuf[index].v1 = Vj.v1 + (Vi.v1 - Vj.v1) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(Vj.b + (Vi.b - Vj.b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(Vj.g + (Vi.g - Vj.g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(Vj.r + (Vi.r - Vj.r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(Vj.a + (Vi.a - Vj.a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index++] = Vj;
                }
            }
        }
        n = index;
    }
    
    if (n < 3) 
    {
        FRDP (" * clip_tri: n < 3\n");
        return;
    }
    ConvertCoordsConvert (rdp.vtxbuf, n);
    if (rdp.fog_coord_enabled)
    {
        for (i = 0; i < n; i++)
        {
            rdp.vtxbuf[i].f = 1.0f/max(16.0f,rdp.vtxbuf[i].f);
        }
    }
    
    if (settings.lodmode > 0 && rdp.cur_tile < rdp.mipmap_level)
        CalculateLOD(rdp.vtx_buffer?(vtx_list2):(vtx_list1), n);
    
    cmb.cmb_ext_use = cmb.tex_cmb_ext_use = 0;
    
    /*
    if (rdp.hires_tex)
    {
    for (int k = 0; k < 3; k++)
    {
    FRDP("v%d %f->%f, width: %d. height: %d, tex_width: %d, tex_height: %d, lr_u: %f, lr_v: %f\n", k, vv0[k], pv[k]->v1, rdp.hires_tex->width, rdp.hires_tex->height, rdp.hires_tex->tex_width, rdp.hires_tex->tex_height, rdp.hires_tex->lr_u, rdp.hires_tex->lr_v);
    }
    }
    */
    if (fullscreen)
    {
        if (settings.wireframe)
        {
            SetWireframeCol ();
            for (i=0; i<n; i++)
            {
                j = i+1;
                if (j == n) j = 0;
                grDrawLine (&rdp.vtxbuf[i], &rdp.vtxbuf[j]);
            }
        }
        else
        {
            
            //      VERTEX ** pv = rdp.vtx_buffer?(vtx_list2):(vtx_list1);
            //      for (int k = 0; k < n; k ++) 
//          FRDP ("DRAW[%d]: v.x = %f, v.y = %f, v.z = %f, v.u = %f, v.v = %f\n", k, pv[k]->x, pv[k]->y, pv[k]->z, pv[k]->coord[rdp.t0<<1], pv[k]->coord[(rdp.t0<<1)+1]);
            //        pv[k]->y = settings.res_y - pv[k]->y;
            
            if (linew > 0)
            {
                if (linew == 1)
                {
                    for (i=0; i<n; i++)
                    {
                        rdp.vtxbuf[i].z = ScaleZ(rdp.vtxbuf[i].z);
                        j = i+1;
                        if (j == n) j = 0;
                        grDrawLine (&rdp.vtxbuf[i], &rdp.vtxbuf[j]);
                    }
                }
                else if (n == 3)
                {
                    rdp.vtxbuf[0].z = ScaleZ(rdp.vtxbuf[0].z);
                    rdp.vtxbuf[1].z = ScaleZ(rdp.vtxbuf[1].z);
                    VERTEX v[4];
                    v[0] = rdp.vtxbuf[0];
                    v[1] = rdp.vtxbuf[0];
                    v[2] = rdp.vtxbuf[1];
                    v[3] = rdp.vtxbuf[1];
                    float width = (linew-1)*0.25f;
                    if(rdp.vtxbuf[0].y == rdp.vtxbuf[1].y )
                    {
                        v[0].x = v[1].x = rdp.vtxbuf[0].x;
                        v[2].x = v[3].x = rdp.vtxbuf[1].x;
                        
                        v[0].y = v[2].y = rdp.vtxbuf[0].y-width*rdp.scale_y+1;
                        v[1].y = v[3].y = rdp.vtxbuf[0].y+width*rdp.scale_y;
                    }
                    else
                    {
                        v[0].y = v[1].y = rdp.vtxbuf[0].y;
                        v[2].y = v[3].y = rdp.vtxbuf[1].y;
                        
                        v[0].x = v[2].x = rdp.vtxbuf[0].x-width*rdp.scale_x+1;
                        v[1].x = v[3].x = rdp.vtxbuf[0].x+width*rdp.scale_x;
                    }
                    grDrawTriangle(&v[0], &v[1], &v[2]);    
                    grDrawTriangle(&v[1], &v[2], &v[3]);    
                }
            }
            else
            {
                if (settings.ucode == 5)
                    for (i=0; i<n; i++)
                        if (rdp.vtxbuf[i].z < -1000.0f)  return;//rdp.vtxbuf[i].z = 0.0f;
                        DepthBuffer(rdp.vtx_buffer?(vtx_list2):(vtx_list1), n);
                grDrawVertexArray (GR_TRIANGLE_FAN, n, rdp.vtx_buffer?(&vtx_list2):(&vtx_list1));
            }
            //grDrawVertexArrayContiguous (GR_TRIANGLE_FAN, n, rdp.vtxbuf, sizeof(VERTEX));
        }
    }
    
    if (debug.capture) add_tri (rdp.vtxbuf, n, TRI_TRIANGLE);
}

void add_tri (VERTEX *v, int n, int type)
{
    //FRDP ("ENTER (%f, %f, %f), (%f, %f, %f), (%f, %f, %f)\n", v[0].x, v[0].y, v[0].w,
    //  v[1].x, v[1].y, v[1].w, v[2].x, v[2].y, v[2].w);
    
    // Debug capture
    if (debug.capture)
    {
        rdp.debug_n ++;
        
        TRI_INFO *info = new TRI_INFO;
        info->nv = n;
        info->v = new VERTEX [n];
        memcpy (info->v, v, sizeof(VERTEX)*n);
        info->cycle_mode = rdp.cycle_mode;
        info->cycle1 = rdp.cycle1;
        info->cycle2 = rdp.cycle2;
        info->uncombined = rdp.uncombined;
        info->geom_mode = rdp.geom_mode;
        info->othermode_h = rdp.othermode_h;
        info->othermode_l = rdp.othermode_l;
        info->tri_n = rdp.tri_n;
        info->type = type;
        
        for (int i=0; i<2; i++)
        {
            int j = rdp.cur_tile+i;
            if (i == 0)
                info->t[i].tmu = rdp.t0;
            else
                info->t[i].tmu = rdp.t1;
            info->t[i].cur_cache[0] = rdp.cur_cache_n[rdp.t0];
            info->t[i].cur_cache[1] = rdp.cur_cache_n[rdp.t1];
            info->t[i].format = rdp.tiles[j].format;
            info->t[i].size = rdp.tiles[j].size;
            info->t[i].width = rdp.tiles[j].width;
            info->t[i].height = rdp.tiles[j].height;
            info->t[i].line = rdp.tiles[j].line;
            info->t[i].palette = rdp.tiles[j].palette;
            info->t[i].clamp_s = rdp.tiles[j].clamp_s;
            info->t[i].clamp_t = rdp.tiles[j].clamp_t;
            info->t[i].mirror_s = rdp.tiles[j].mirror_s;
            info->t[i].mirror_t = rdp.tiles[j].mirror_t;
            info->t[i].shift_s = rdp.tiles[j].shift_s;
            info->t[i].shift_t = rdp.tiles[j].shift_t;
            info->t[i].mask_s = rdp.tiles[j].mask_s;
            info->t[i].mask_t = rdp.tiles[j].mask_t;
            info->t[i].ul_s = rdp.tiles[j].ul_s;
            info->t[i].ul_t = rdp.tiles[j].ul_t;
            info->t[i].lr_s = rdp.tiles[j].lr_s;
            info->t[i].lr_t = rdp.tiles[j].lr_t;
            info->t[i].t_ul_s = rdp.tiles[7].t_ul_s;
            info->t[i].t_ul_t = rdp.tiles[7].t_ul_t;
            info->t[i].t_lr_s = rdp.tiles[7].t_lr_s;
            info->t[i].t_lr_t = rdp.tiles[7].t_lr_t;
            info->t[i].scale_s = rdp.tiles[j].s_scale;
            info->t[i].scale_t = rdp.tiles[j].t_scale;
        }
        
        info->fog_color = rdp.fog_color;
        info->fill_color = rdp.fill_color;
        info->prim_color = rdp.prim_color;
        info->blend_color = rdp.blend_color;
        info->env_color = rdp.env_color;
        info->prim_lodmin = rdp.prim_lodmin;
        info->prim_lodfrac = rdp.prim_lodfrac;
        
        info->pNext = debug.tri_list;
        debug.tri_list = info;
        
        if (debug.tri_last == NULL)
            debug.tri_last = debug.tri_list;
    }
}

void update_scissor ()
{
    if (rdp.update & UPDATE_SCISSOR)
    {
        rdp.update ^= UPDATE_SCISSOR;
        
        // KILL the floating point error with 0.01f
        rdp.scissor.ul_x = (DWORD) max(min((rdp.scissor_o.ul_x * rdp.scale_x + rdp.offset_x + 0.01f),settings.res_x),0);
        rdp.scissor.lr_x = (DWORD) max(min((rdp.scissor_o.lr_x * rdp.scale_x + rdp.offset_x + 0.01f),settings.res_x),0);
        rdp.scissor.ul_y = (DWORD) max(min((rdp.scissor_o.ul_y * rdp.scale_y + rdp.offset_y + 0.01f),settings.res_y),0);
        rdp.scissor.lr_y = (DWORD) max(min((rdp.scissor_o.lr_y * rdp.scale_y + rdp.offset_y + 0.01f),settings.res_y),0);
        FRDP (" |- scissor - (%d, %d) -> (%d, %d)\n", rdp.scissor.ul_x, rdp.scissor.ul_y,
            rdp.scissor.lr_x, rdp.scissor.lr_y);
    }
}

//
// update - update states if they need it
//

typedef struct
{
    unsigned int    c2_m2b:2;
    unsigned int    c1_m2b:2;
    unsigned int    c2_m2a:2;
    unsigned int    c1_m2a:2;
    unsigned int    c2_m1b:2;
    unsigned int    c1_m1b:2;
    unsigned int    c2_m1a:2;
    unsigned int    c1_m1a:2;
} rdp_blender_setting;

void update ()
{
    RDP ("-+ update called\n");
    // Check for rendermode changes
    // Z buffer
    if (rdp.render_mode_changed & 0x00000C30)
    {
        FRDP (" |- render_mode_changed zbuf - decal: %s, update: %s, compare: %s\n",
            str_yn[(rdp.othermode_l&0x00000C00) == 0x00000C00],
            str_yn[(rdp.othermode_l&0x00000020)?1:0],
            str_yn[(rdp.othermode_l&0x00000010)?1:0]);
        
        rdp.render_mode_changed &= ~0x00000C30;
        rdp.update |= UPDATE_ZBUF_ENABLED;
        
        // Decal?
        //    if ((rdp.othermode_l & 0x00000C00) == 0x00000C00)
        if (rdp.othermode_l & 0x00000800)
            rdp.flags |= ZBUF_DECAL;
        else
            rdp.flags &= ~ZBUF_DECAL;
        
        // Update?
        if ((rdp.othermode_l & 0x00000020))
            rdp.flags |= ZBUF_UPDATE;
        else
            rdp.flags &= ~ZBUF_UPDATE;
        
        // Compare?
        if (rdp.othermode_l & 0x00000010)
            rdp.flags |= ZBUF_COMPARE;
        else
            rdp.flags &= ~ZBUF_COMPARE;
    }
    
    // Alpha compare
    if (rdp.render_mode_changed & 0x00001000)
    {
        FRDP (" |- render_mode_changed alpha compare - on: %s\n",
            str_yn[(rdp.othermode_l&0x00001000)?1:0]);
        rdp.render_mode_changed &= ~0x00001000;
        rdp.update |= UPDATE_ALPHA_COMPARE;
        
        if (rdp.othermode_l & 0x00001000)
            rdp.flags |= ALPHA_COMPARE;
        else
            rdp.flags &= ~ALPHA_COMPARE;
    }
    
    if (rdp.render_mode_changed & 0x00002000) // alpha cvg sel
    {
        FRDP (" |- render_mode_changed alpha cvg sel - on: %s\n",
            str_yn[(rdp.othermode_l&0x00002000)?1:0]);
        rdp.render_mode_changed &= ~0x00002000;
        rdp.update |= UPDATE_COMBINE;
    }
    
    // Force blend
    if (rdp.render_mode_changed & 0xFFFF0000)
    {
        FRDP (" |- render_mode_changed force_blend - %08lx\n", rdp.othermode_l&0xFFFF0000);
        rdp.render_mode_changed &= 0x0000FFFF;
        
        rdp.fbl_a0 = (BYTE)((rdp.othermode_l>>30)&0x3);
        rdp.fbl_b0 = (BYTE)((rdp.othermode_l>>26)&0x3);
        rdp.fbl_c0 = (BYTE)((rdp.othermode_l>>22)&0x3);
        rdp.fbl_d0 = (BYTE)((rdp.othermode_l>>18)&0x3);
        rdp.fbl_a1 = (BYTE)((rdp.othermode_l>>28)&0x3);
        rdp.fbl_b1 = (BYTE)((rdp.othermode_l>>24)&0x3);
        rdp.fbl_c1 = (BYTE)((rdp.othermode_l>>20)&0x3);
        rdp.fbl_d1 = (BYTE)((rdp.othermode_l>>16)&0x3);
        
        rdp.update |= UPDATE_COMBINE;
    }
    
    //if (fullscreen)
    //{
    // Combine MUST go before texture
    if ((rdp.update & UPDATE_COMBINE) && rdp.allow_combine)
    {
        RDP (" |-+ update_combine\n");
        Combine ();
    }
    
    if (rdp.update & UPDATE_TEXTURE)  // note: UPDATE_TEXTURE and UPDATE_COMBINE are the same
    {
        rdp.tex_ctr ++;
        if (rdp.tex_ctr == 0xFFFFFFFF)
            rdp.tex_ctr = 0;
        
        TexCache ();
        if (rdp.noise == noise_none)
            rdp.update ^= UPDATE_TEXTURE;
    }
    
    if (fullscreen)
    {
        // Z buffer
        if (rdp.update & UPDATE_ZBUF_ENABLED)
        {
            // already logged above
            rdp.update ^= UPDATE_ZBUF_ENABLED;
            
            if (rdp.flags & ZBUF_DECAL)
            {
                if ((rdp.othermode_l & 0x00000C00) == 0x00000C00)
                {
                    grDepthBiasLevel (settings.depth_bias);//(-32);
                    FRDP("depth bias: %d\n", settings.depth_bias);
                }
                else
                {
                  // VP changed that to -1 (was -4)
                    grDepthBiasLevel (-4);//-16);
                    RDP("depth bias: -4");
                }
            }
            else
            {
                grDepthBiasLevel (0);
            }
            
            if ((rdp.flags & ZBUF_ENABLED) || (settings.force_depth_compare && rdp.zsrc == 1))
            {
                if ((rdp.flags & ZBUF_COMPARE))
                {
                    if (settings.soft_depth_compare)
                    {
                        grDepthBufferFunction (GR_CMP_LEQUAL);
                    }
                    else
                    {
                        grDepthBufferFunction (GR_CMP_LESS);
                    }
                }
                else
                {
                    grDepthBufferFunction (GR_CMP_ALWAYS);
                }
                
                if ((rdp.flags & ZBUF_UPDATE)
            // || (rdp.flags & ZBUF_DECAL) // FOR DEBUGGING ONLY
        )
                {
                    grDepthMask (FXTRUE);
                }
                else
                {
                    grDepthMask (FXFALSE);
                }
            }
            else
            {
                grDepthBufferFunction (GR_CMP_ALWAYS);
                grDepthMask (FXFALSE);
            }
        }
        // Alpha compare
        if (rdp.update & UPDATE_ALPHA_COMPARE)
        {
            // already logged above
            rdp.update ^= UPDATE_ALPHA_COMPARE;
            
            //    if (rdp.acmp == 1 && !(rdp.othermode_l & 0x00002000) && !force_full_alpha)
            //      if (rdp.acmp == 1 && !(rdp.othermode_l & 0x00002000) && (rdp.blend_color&0xFF))
            if (rdp.acmp == 1 && !(rdp.othermode_l & 0x00002000) && (!(rdp.othermode_l & 0x00004000) || (rdp.blend_color&0xFF)))
            {
                BYTE reference = (BYTE)(rdp.blend_color&0xFF);
                if (reference)
                  grAlphaTestFunction (GR_CMP_GEQUAL);
                else
                  grAlphaTestFunction (GR_CMP_GREATER);
                grAlphaTestReferenceValue (reference);
                FRDP (" |- alpha compare: blend: %02lx\n", reference);
            }
            else
            {
                if (rdp.flags & ALPHA_COMPARE)
                {
                    if ((rdp.othermode_l & 0x5000) != 0x5000)
                    {
                        grAlphaTestFunction (GR_CMP_GEQUAL);
                        grAlphaTestReferenceValue (0x20);//0xA0);
                        RDP (" |- alpha compare: 0x20\n");
                    }
                    else
                    {
                        grAlphaTestFunction (GR_CMP_GREATER);
                        if (rdp.acmp == 3)
                        {
                            grAlphaTestReferenceValue ((BYTE)(rdp.blend_color&0xFF));
                            FRDP (" |- alpha compare: blend: %02lx\n", rdp.blend_color&0xFF);
                        }
                        else
                        {
                            grAlphaTestReferenceValue (0x00);
                            RDP (" |- alpha compare: 0x00\n");
                        }
                    }
                }
                else
                {
                    grAlphaTestFunction (GR_CMP_ALWAYS);
                    RDP (" |- alpha compare: none\n");
                }
            }
            if (rdp.acmp == 3) 
            {
                if (grStippleModeExt)
                {
                    RDP (" |- alpha compare: dither\n");
                    grStippleModeExt(settings.stipple_mode);
                    //              grStippleModeExt(GR_STIPPLE_PATTERN);
                }
            }
            else
            {
                if (grStippleModeExt)
                {
                    //RDP (" |- alpha compare: dither disabled\n");
                    grStippleModeExt(GR_STIPPLE_DISABLE);
                }
            }
        }
        // Cull mode (leave this in for z-clipped triangles)
        if (rdp.update & UPDATE_CULL_MODE)
        {
            rdp.update ^= UPDATE_CULL_MODE;
            DWORD mode = (rdp.flags & CULLMASK) >> CULLSHIFT;
            FRDP (" |- cull_mode - mode: %s\n", str_cull[mode]);
            switch (mode)
            {
            case 0: // cull none
            case 3: // cull both
                grCullMode(GR_CULL_DISABLE);
                break;
            case 1: // cull front
                //        grCullMode(GR_CULL_POSITIVE);
                grCullMode(GR_CULL_NEGATIVE);
                break;
            case 2: // cull back
                //        grCullMode (GR_CULL_NEGATIVE);
                grCullMode (GR_CULL_POSITIVE);
                break;
            }
        }
        
        //Added by Gonetz.
        if (settings.fog && (rdp.update & UPDATE_FOG_ENABLED))
        {
            rdp.update ^= UPDATE_FOG_ENABLED;
            
            if (rdp.flags & FOG_ENABLED) 
                {
                typedef union { WORD *w; rdp_blender_setting *b; } BLEND;
                WORD blword = (WORD) (rdp.othermode_l >> 16);
                BLEND bl;
                bl.w =  &blword;
                if((rdp.fog_multiplier > 0) && (bl.b->c1_m1a==3 || bl.b->c1_m2a == 3 || bl.b->c2_m1a == 3 || bl.b->c2_m2a == 3))
                    {
                grFogColorValue(rdp.fog_color);
                    grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
                    rdp.fog_coord_enabled = TRUE;
                    RDP("fog enabled \n");
                }
                else
                {
                    RDP("fog disabled in blender\n");
                    rdp.fog_coord_enabled = FALSE;
                grFogMode (GR_FOG_DISABLE);
        }
  }
            else
            {
                RDP("fog disabled\n");
                rdp.fog_coord_enabled = FALSE;
                grFogMode (GR_FOG_DISABLE);
            }
        }
  }
  
  if (rdp.update & UPDATE_VIEWPORT)
  {
      rdp.update ^= UPDATE_VIEWPORT;
      if (fullscreen)
      {
          if (settings.RE2)
          {
        grClipWindow (0, 0, settings.res_x-1, settings.res_y-1);
          }
          else
          {
      float scale_x = (float)fabs(rdp.view_scale[0]);
      float scale_y = (float)fabs(rdp.view_scale[1]);
      //printf("scale_y %g\n", scale_y);
      
      DWORD min_x = (DWORD) max(rdp.view_trans[0] - scale_x, 0);
      DWORD min_y = (DWORD) max(rdp.view_trans[1] - scale_y, 0);
      DWORD max_x = (DWORD) min(rdp.view_trans[0] + scale_x + 1, settings.res_x);
      DWORD max_y = (DWORD) min(rdp.view_trans[1] + scale_y + 1, settings.res_y);
      
      FRDP (" |- viewport - (%d, %d, %d, %d)\n", min_x, min_y, max_x, max_y);
          grClipWindow (min_x, min_y, max_x, max_y);
          //printf("viewport %d %d %d %d\n", min_x, min_y, max_x, max_y);
          }
      }
  }
  
  if (rdp.update & UPDATE_SCISSOR)
      update_scissor ();
  
  RDP (" + update end\n");
}

void set_message_combiner ()
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
    if (settings.buff_clear && (settings.show_fps & 0x08))
        grAlphaBlendFunction (GR_BLEND_SRC_ALPHA,
        GR_BLEND_ONE_MINUS_SRC_ALPHA,
        GR_BLEND_ZERO,
        GR_BLEND_ZERO);
    else
        grAlphaBlendFunction (GR_BLEND_ONE,
        GR_BLEND_ZERO,
        GR_BLEND_ZERO,
        GR_BLEND_ZERO);
    grAlphaTestFunction (GR_CMP_ALWAYS);
    if (grStippleModeExt)
    {
        grStippleModeExt(GR_STIPPLE_DISABLE);
    }
    grTexCombine (GR_TMU1,
        GR_COMBINE_FUNCTION_NONE,
        GR_COMBINE_FACTOR_NONE,
        GR_COMBINE_FUNCTION_NONE,
        GR_COMBINE_FACTOR_NONE,
        FXFALSE, FXFALSE);
    grTexCombine (GR_TMU0,
        GR_COMBINE_FUNCTION_LOCAL,
        GR_COMBINE_FACTOR_NONE,
        GR_COMBINE_FUNCTION_LOCAL,
        GR_COMBINE_FACTOR_NONE,
        FXFALSE, FXFALSE);
    grTexSource(GR_TMU0,
        grTexMinAddress(GR_TMU0) + offset_font,
        GR_MIPMAPLEVELMASK_BOTH,
        &fontTex);
    grFogMode (GR_FOG_DISABLE);
}

/*
  1 bit: common
  2 bit: I textures, V-Rally 99
  3 bit: South Park, Polaris
  4 bit: Mace
  5 bit: CyberTiger
  6 bit: Yoshi Story
*/
void fix_tex_coord (VERTEX **v)
{
  BOOL fix = FALSE;
    if (settings.fix_tex_coord & 449)
  {
//    if ( (rdp.tiles[rdp.last_tile_size].format == 2) || 
//         ( (rdp.tiles[rdp.last_tile_size].size != 2)) )
    if (rdp.tiles[rdp.last_tile_size].size != 2)
    {
            if (settings.fix_tex_coord & 128)
            {
                if (v[0]->sz != v[1]->sz || v[0]->sz != v[2]->sz)
                    return;
            }
            
            if (settings.fix_tex_coord & 256) //dr.mario
            {
                if ((rdp.tiles[rdp.last_tile_size].format == 2) && (rdp.tiles[rdp.last_tile_size].size == 0))
                    return;
            }
            
//      int lu = (rdp.tiles[rdp.last_tile_size].ul_s)<<1;
      int ru = (rdp.tiles[rdp.last_tile_size].lr_s+1)<<1;
      int rv = (rdp.tiles[rdp.last_tile_size].lr_t+1)<<1;
      int diff = (settings.fix_tex_coord & 64) ? 5 : 3;

      for (int t = 0; t < 3; t++)
      {
        if (v[t]->uv_fixed == 0) //&& (((short)v[t]->ou > 0) || ((short)v[t]->ov > 0)))
        {
          if ( (abs((short)v[t]->ou - ru) < diff) || (abs((short)v[t]->ov - rv) < diff) )
//          if ( ((short)v[t]->ou == lu) || (abs((short)v[t]->ou - ru) < 3) )
          {
             fix = TRUE;
             break;
          }
        }
        else
        {
           fix = TRUE;
           break;
        }
      }
      if (fix)
      {
        for (int t = 0; t < 3; t++)
        {
          if (v[t]->uv_fixed == 0)
          {
            v[t]->uv_fixed = 1;
            FRDP("v[%d] uv_fixed (%f, %f)->(%f,%f)\n",t, v[t]->ou, v[t]->ov, v[t]->ou*0.5f, v[t]->ov*0.5f);
            v[t]->ou *= 0.5f;
            v[t]->ov *= 0.5f;
          }
        }
        return;
      }
    }
  }
  if (settings.fix_tex_coord & 2)
  {
    if (rdp.tiles[rdp.last_tile_size].format == 4)
    {
        for (int t = 0; t < 3; t++)
        {
          if (v[t]->uv_fixed == 0)
          {
            v[t]->uv_fixed = 1;
            v[t]->ou *= 0.5f;
            v[t]->ov *= 0.5f;
          }
        }
        return;
    }
  }
  if (settings.fix_tex_coord & 4)
  {
        TILE & last_tile = rdp.tiles[rdp.last_tile_size];
        if ((last_tile.format == 2) && 
            (last_tile.size == 0) && 
            (last_tile.line%2 == 0) &&
            (last_tile.lr_s >= last_tile.lr_t))
    {
            int ru = (rdp.tiles[rdp.last_tile_size].lr_s+1);
            int rv = (rdp.tiles[rdp.last_tile_size].lr_t+1);
            int t;
            for (t = 0; t < 3; t++)
            {
                if (v[t]->uv_fixed == 0)
                {
                    if ( (abs((short)v[t]->ou - ru) < 3) || (abs((short)v[t]->ov - rv) < 3) )
                      return;
                }
            }
            for (t = 0; t < 3; t++)
        {
          if (v[t]->uv_fixed == 0)
          {
            v[t]->uv_fixed = 1;
            v[t]->ou *= 0.5f;
            v[t]->ov *= 0.5f;
          }
        }
        return;
    }
  }
  if (settings.fix_tex_coord & 8)
  {
        if (rdp.tiles[rdp.last_tile_size].format == 3 && rdp.tiles[rdp.last_tile_size].size == 1)
    {
      short width = (rdp.tiles[rdp.last_tile_size].ul_s<<1)+1 ;
      for (int t = 0; t < 3; t++)
      {
        if (v[t]->uv_fixed == 0)
        {
          if (short(v[t]->ou) == width)
          {
             fix = TRUE;
             break;
          }
        }
        else
        {
           fix = TRUE;
           break;
        }
      }
      if (fix)
      {
                RDP("texcoord fixed!\n");
        for (int t = 0; t < 3; t++)
        {
          if (v[t]->uv_fixed == 0)
          {
            v[t]->uv_fixed = 1;
            v[t]->ou *= 0.5f;
            v[t]->ov *= 0.5f;
          }
        }
        return;
      }
    }
  }
  if (settings.fix_tex_coord & 16)
  {
    if ((rdp.tiles[rdp.last_tile_size].format == 2) && (rdp.tiles[rdp.last_tile_size].size == 0))
    {
      short width = rdp.tiles[rdp.last_tile_size].lr_s + 1;
      short height = rdp.tiles[rdp.last_tile_size].lr_t + 1;
      for (int t = 0; t < 3; t++)
      {
        if (v[t]->uv_fixed == 0)
        {
          if ((short(v[t]->ou) > width) || (short(v[t]->ov) > height))
          {
             fix = TRUE;
             break;
          }
        }
        else
        {
           fix = TRUE;
           break;
        }
      }
      if (fix)
      {
        for (int t = 0; t < 3; t++)
        {
          if (v[t]->uv_fixed == 0)
          {
            v[t]->uv_fixed = 1;
            v[t]->ou *= 0.5f;
            v[t]->ov *= 0.5f;
          }
        }
        RDP("texcoord fixed!\n");
        return;
      }
    }
  }
  if (settings.fix_tex_coord & 32)
  {
    if (!rdp.vtx[rdp.v0].uv_fixed && 
    (rdp.tiles[rdp.last_tile_size].format == 2) && 
    (rdp.tiles[rdp.last_tile_size].size == 1) &&
    (rdp.tiles[rdp.last_tile_size].lr_s >= 31) &&
    (rdp.tiles[rdp.last_tile_size].lr_t >= 31))
    {
      int ru = (rdp.tiles[rdp.last_tile_size].lr_s+1)<<1;
      int rv = (rdp.tiles[rdp.last_tile_size].lr_t+1)<<1;
      int top = rdp.v0 + rdp.vn;
      for (int t = rdp.v0; t < top; t++)
      {
        if ( (abs((short)rdp.vtx[t].ou - ru) < 2) || (abs((short)rdp.vtx[t].ov - rv) < 2) )
        {
           fix = TRUE;
           break;
        }
      }
      if (fix)
      {
        for (int t = rdp.v0; t < top; t++)
        {
          rdp.vtx[t].uv_fixed = 1;
          rdp.vtx[t].ou *= 0.5f;
          rdp.vtx[t].ov *= 0.5f;
        }
        RDP("texcoord fixed!\n");
        return;
      }
    }
  }
}

