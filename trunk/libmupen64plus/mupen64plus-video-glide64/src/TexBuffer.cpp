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
* Licence along with this program; if not, write to the Free
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
//****************************************************************
//
// Dec 2003 created by Gonetz
//
//****************************************************************

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_config.h"
#include "m64p_vidext.h"
#include "rdp.h"
#include "TexBuffer.h"
#include "Gfx1.3.h"


#ifndef _WIN32
#include <string.h>
#endif // _WIN32

#define max(a,b) ((a) > (b) ? (a) : (b))
#define min(a,b) ((a) < (b) ? (a) : (b))

static HIRES_COLOR_IMAGE * AllocateTextureBuffer(COLOR_IMAGE & cimage)
{
  HIRES_COLOR_IMAGE texbuf;
  texbuf.addr = cimage.addr;
  texbuf.end_addr = cimage.addr + cimage.width*cimage.height*cimage.size;
  texbuf.width = cimage.width;
  texbuf.height = cimage.height;
  texbuf.format = (WORD)cimage.format;
    texbuf.scr_width = min(cimage.width * rdp.scale_x, settings.scr_res_x);
    float height = min(rdp.vi_height,cimage.height);
    if (cimage.status == ci_copy_self || (cimage.status == ci_copy && cimage.width == rdp.frame_buffers[rdp.main_ci_index].width)) 
        height = rdp.vi_height;
    texbuf.scr_height = height * rdp.scale_y;

  WORD max_size = max((WORD)texbuf.scr_width, (WORD)texbuf.scr_height);
  if (max_size > max_tex_size) //texture size is too large 
    return 0;
  DWORD tex_size;
  //calculate LOD
    switch ((max_size-1) >> 6)
  {
  case 0:
    // ZIGGY : fixed (was GR_LOD_LOG2_128)
        texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_64;
        tex_size = 64;
        break;
    case 1:
   texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_128;
   tex_size = 128;
   break;
    case 2:
    case 3:
   texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_256;
   tex_size = 256;
   break;
  case 4:
  case 5:
  case 6:
  case 7:
        texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_512;
        tex_size = 512;
        break;
    case 8:
    case 9:
    case 10:
    case 11:
    case 12:
    case 13:
    case 14:
    case 15:
   texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_1024;
   tex_size = 1024;
   break;
  default:
   texbuf.info.smallLodLog2 = texbuf.info.largeLodLog2 = GR_LOD_LOG2_2048;
   tex_size = 2048;
  }
  //calculate aspect
  if (texbuf.scr_width >= texbuf.scr_height)
  {
    if ((texbuf.scr_width/texbuf.scr_height) >= 2)
    {
      texbuf.info.aspectRatioLog2 = GR_ASPECT_LOG2_2x1;
      texbuf.tex_width = tex_size;
      texbuf.tex_height = tex_size >> 1;
    }
    else
    {
      texbuf.info.aspectRatioLog2 = GR_ASPECT_LOG2_1x1;
      texbuf.tex_width = texbuf.tex_height = tex_size;
    }
  }
  else
  {
    if ((texbuf.scr_height/texbuf.scr_width) >= 2)
    {
      texbuf.info.aspectRatioLog2 = GR_ASPECT_LOG2_1x2;
      texbuf.tex_width = tex_size >> 1;
      texbuf.tex_height = tex_size;
    }
    else
    {
      texbuf.info.aspectRatioLog2 = GR_ASPECT_LOG2_1x1;
      texbuf.tex_width = texbuf.tex_height = tex_size;
    }
  }
  if ((cimage.format != 0))// && (cimage.width <= 64))
    texbuf.info.format = GR_TEXFMT_ALPHA_INTENSITY_88;
  else
    texbuf.info.format = GR_TEXFMT_RGB_565;

    float lr_u = 256.0f * texbuf.scr_width / (float)tex_size;// + 1.0f;
    float lr_v = 256.0f * texbuf.scr_height / (float)tex_size;// + 1.0f;
    texbuf.tile = 0;
    texbuf.tile_uls = 0;
    texbuf.tile_ult = 0;
    texbuf.u_shift = 0;
    texbuf.v_shift = 0;
    texbuf.drawn = FALSE;
    texbuf.clear = FALSE;
    texbuf.info.data = NULL;
    texbuf.u_scale = lr_u / (float)(texbuf.width);
    texbuf.v_scale = lr_v / (float)(texbuf.height);

    FRDP("\nAllocateTextureBuffer. width: %d, height: %d, scr_width: %f, scr_height: %f, vi_width: %f, vi_height:%f, scale_x: %f, scale_y: %f, lr_u: %f, lr_v: %f, u_scale: %f, v_scale: %f\n", texbuf.width, texbuf.height, texbuf.scr_width, texbuf.scr_height, rdp.vi_width, rdp.vi_height, rdp.scale_x, rdp.scale_y, lr_u, lr_v, texbuf.u_scale, texbuf.v_scale);

  DWORD required = grTexCalcMemRequired(texbuf.info.smallLodLog2, texbuf.info.largeLodLog2, 
                                         texbuf.info.aspectRatioLog2, texbuf.info.format);
  //find free space
  for (int i = 0; i < num_tmu; i++)
  {
    DWORD available = 0;
    DWORD top = 0;
    if (rdp.texbufs[i].count)
    {
      HIRES_COLOR_IMAGE & t = rdp.texbufs[i].images[rdp.texbufs[i].count - 1];
            if (rdp.read_whole_frame)
            {
                if ((cimage.status == ci_aux) && (rdp.cur_tex_buf == i))
                {
                    top = /*rdp.texbufs[i].begin + */t.tex_addr + t.tex_width * (int)(t.scr_height+1) * 2;
                    if (rdp.texbufs[i].end - top < required)
                        return 0;
                }
                else
                    top = rdp.texbufs[i].end;
            }
            else
      top = /*rdp.texbufs[i].begin + */t.tex_addr + t.tex_width * t.tex_height * 2;
      available  = rdp.texbufs[i].end - top;
    }
    else 
    {
      available  = rdp.texbufs[i].end - rdp.texbufs[i].begin;
      top = rdp.texbufs[i].begin;
    }
    //printf("i %d count %d end %gMb avail %gMb req %gMb\n", i, rdp.texbufs[i].count, rdp.texbufs[i].end/1024.0f/1024, available/1024.0f/1024, required/1024.0f/1024);
    if (available >= required)
    {
      rdp.texbufs[i].count++;
      rdp.texbufs[i].clear_allowed = FALSE;
      texbuf.tex_addr = top;
      rdp.cur_tex_buf = i;
      // ZIGGY strange fix
      texbuf.tmu = rdp.texbufs[i].tmu;
      rdp.texbufs[i].images[rdp.texbufs[i].count - 1] = texbuf;
      return &(rdp.texbufs[i].images[rdp.texbufs[i].count - 1]);
    }
  }
  //not found. keep recently accessed bank, clear second one
  if (!rdp.texbufs[rdp.cur_tex_buf^1].clear_allowed) { //can't clear => can't allocate
    WriteLog(M64MSG_WARNING, "Can't allocate texture buffer\n");
    return 0;
  }
  rdp.cur_tex_buf ^= 1;
  rdp.texbufs[rdp.cur_tex_buf].count = 1;
    rdp.texbufs[rdp.cur_tex_buf].clear_allowed = FALSE;
  // ZIGGY strange fix
  texbuf.tmu = rdp.texbufs[rdp.cur_tex_buf].tmu;
  texbuf.tex_addr = rdp.texbufs[rdp.cur_tex_buf].begin;
  rdp.texbufs[rdp.cur_tex_buf].images[0] = texbuf;
  return &(rdp.texbufs[rdp.cur_tex_buf].images[0]);
} 

BOOL OpenTextureBuffer(COLOR_IMAGE & cimage)
{
  FRDP("OpenTextureBuffer. cur_tex_buf: %d, addr: %08lx, width: %d, height: %d", rdp.cur_tex_buf, cimage.addr, cimage.width, cimage.height);
  if (!fullscreen) return FALSE;

    BOOL found = FALSE, search = TRUE;
  HIRES_COLOR_IMAGE *texbuf = 0;
  DWORD addr = cimage.addr;
  DWORD end_addr = addr + cimage.width*cimage.height*cimage.size;
    if (rdp.motionblur) 
    {
        if (cimage.format != 0)
            return FALSE;
        search = FALSE;
    }
    if (rdp.read_whole_frame)
    {
        if (settings.PM) //motion blur effects in Paper Mario
        {
            rdp.cur_tex_buf = rdp.acc_tex_buf;
            FRDP("read_whole_frame. last allocated bank: %d\n", rdp.acc_tex_buf);
        }
        else
        {
            if (!rdp.texbufs[0].clear_allowed || !rdp.texbufs[1].clear_allowed)
            {
                if (cimage.status == ci_main)
                {
                    texbuf = &(rdp.texbufs[rdp.cur_tex_buf].images[0]);
                    found = TRUE;
                }
                else
                {
                    for (int t = 0; (t < rdp.texbufs[rdp.cur_tex_buf].count) && !found; t++)
                    {
                        texbuf = &(rdp.texbufs[rdp.cur_tex_buf].images[t]);
                        if (addr == texbuf->addr && cimage.width == texbuf->width)
                        {
                            texbuf->drawn = FALSE;
                            found = TRUE;
                        }
                    }
                }
            }
            search = FALSE;
        }
    }
    if (search)
  {
    for (int i = 0; (i < num_tmu) && !found; i++)
    {  
      for (int j = 0; (j < rdp.texbufs[i].count) && !found; j++)
      {
        texbuf = &(rdp.texbufs[i].images[j]);
        if (addr == texbuf->addr && cimage.width == texbuf->width)
        {
                    //texbuf->height = cimage.height;
                    //texbuf->end_addr = end_addr;
          texbuf->drawn = FALSE;
          texbuf->format = (WORD)cimage.format;
          if ((cimage.format != 0))
            texbuf->info.format = GR_TEXFMT_ALPHA_INTENSITY_88;
          else
            texbuf->info.format = GR_TEXFMT_RGB_565;
          found = TRUE;
          rdp.cur_tex_buf = i;
                    rdp.texbufs[i].clear_allowed = FALSE;
        }
        else //check intersection
        {
          if (!((end_addr <= texbuf->addr) || (addr >= texbuf->end_addr))) //intersected, remove
          {
                        grTextureBufferExt( texbuf->tmu, texbuf->tex_addr, texbuf->info.smallLodLog2, texbuf->info.largeLodLog2,
                            texbuf->info.aspectRatioLog2, texbuf->info.format, GR_MIPMAPLEVELMASK_BOTH ); 
                        grRenderBuffer( GR_BUFFER_TEXTUREBUFFER_EXT );
                        grDepthMask (FXFALSE);
                        grBufferClear (0, 0, 0xFFFF);
                        grDepthMask (FXTRUE);
                        grRenderBuffer( GR_BUFFER_BACKBUFFER );
            rdp.texbufs[i].count--;
            if (j < rdp.texbufs[i].count)
               memcpy(&(rdp.texbufs[i].images[j]), &(rdp.texbufs[i].images[j+1]), sizeof(HIRES_COLOR_IMAGE)*(rdp.texbufs[i].count-j));
          }
        }
      }
    }
  }

  if (!found)
  {
    RDP ("  not found");
    texbuf = AllocateTextureBuffer(cimage);
  }
  else
  {
    RDP ("  found");
  }

  if (!texbuf)
  {
    RDP("  KO\n");
    return FALSE;
  }

    rdp.acc_tex_buf = rdp.cur_tex_buf;
  rdp.cur_image = texbuf;
  grRenderBuffer( GR_BUFFER_TEXTUREBUFFER_EXT );
  //printf("texadr %gMb\n", rdp.cur_image->tex_addr/1024.0f/1024);
  grTextureBufferExt( rdp.cur_image->tmu, rdp.cur_image->tex_addr, rdp.cur_image->info.smallLodLog2, rdp.cur_image->info.largeLodLog2,
    rdp.cur_image->info.aspectRatioLog2, rdp.cur_image->info.format, GR_MIPMAPLEVELMASK_BOTH ); 
///*
    if (rdp.cur_image->clear && settings.fb_hires_buf_clear && cimage.changed)
  {
    rdp.cur_image->clear = FALSE;
    grDepthMask (FXFALSE);
    grBufferClear (0, 0, 0xFFFF);
    grDepthMask (FXTRUE);
  }
//*/
//  memset(gfx.RDRAM+cimage.addr, 0, cimage.width*cimage.height*cimage.size);
    FRDP("  texaddr: %08lx, tex_width: %d, tex_height: %d, cur_tex_buf: %d, texformat: %d, motionblur: %d\n", rdp.cur_image->tex_addr, rdp.cur_image->tex_width, rdp.cur_image->tex_height, rdp.cur_tex_buf, rdp.cur_image->info.format, rdp.motionblur);
  return TRUE;
}

static GrTextureFormat_t TexBufSetupCombiner(BOOL force_rgb = FALSE)
{
  grColorCombine( GR_COMBINE_FUNCTION_SCALE_OTHER, 
    GR_COMBINE_FACTOR_ONE, 
    GR_COMBINE_LOCAL_NONE, 
    GR_COMBINE_OTHER_TEXTURE, 
    FXFALSE); 
  grAlphaCombine (GR_COMBINE_FUNCTION_SCALE_OTHER,
    GR_COMBINE_FACTOR_ONE,
    GR_COMBINE_LOCAL_NONE,
    GR_COMBINE_OTHER_TEXTURE,
    FXFALSE);
  grAlphaBlendFunction (GR_BLEND_ONE,   // use alpha compare, but not T0 alpha
    GR_BLEND_ZERO,
    GR_BLEND_ONE,
    GR_BLEND_ZERO);
  grClipWindow (0, 0, settings.scr_res_x, settings.scr_res_y);
  grDepthBufferFunction (GR_CMP_ALWAYS);
  grDepthMask (FXFALSE);
  grCullMode (GR_CULL_DISABLE);
  grFogMode (GR_FOG_DISABLE);
  GrTextureFormat_t buf_format = (rdp.hires_tex) ? rdp.hires_tex->info.format : GR_TEXFMT_RGB_565;
  GrCombineFunction_t color_source = GR_COMBINE_FUNCTION_LOCAL;
  if  (!force_rgb && rdp.black_ci_index > 0 && rdp.black_ci_index <= rdp.copy_ci_index)  
  {
    color_source = GR_COMBINE_FUNCTION_LOCAL_ALPHA;
    buf_format = GR_TEXFMT_ALPHA_INTENSITY_88;
  }
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
      GR_COMBINE_FUNCTION_ZERO, 
      GR_COMBINE_FACTOR_NONE, 
      FXFALSE, 
      FXTRUE ); 
    }
    else
    {
    grTexCombine( GR_TMU1, 
      color_source, 
      GR_COMBINE_FACTOR_NONE, 
      GR_COMBINE_FUNCTION_ZERO, 
      GR_COMBINE_FACTOR_NONE, 
      FXFALSE, 
      FXTRUE ); 
    grTexCombine( GR_TMU0, 
      GR_COMBINE_FUNCTION_SCALE_OTHER, 
      GR_COMBINE_FACTOR_ONE, 
      GR_COMBINE_FUNCTION_SCALE_OTHER, 
      GR_COMBINE_FACTOR_ONE, 
      FXFALSE, 
      FXFALSE ); 
    }
  return buf_format;
}

BOOL CloseTextureBuffer(BOOL draw)
{
  if (!fullscreen || !rdp.cur_image) 
  {
    RDP("CloseTextureBuffer KO\n");
    return FALSE;
  }
  grRenderBuffer( GR_BUFFER_BACKBUFFER );
  if (!draw)
  {
    RDP("CloseTextureBuffer no draw, OK\n");
    rdp.cur_image = 0;
    return TRUE;
  }

  rdp.hires_tex = rdp.cur_image;
  rdp.cur_image = 0;
  GrTextureFormat_t buf_format = rdp.hires_tex->info.format;
  rdp.hires_tex->info.format = TexBufSetupCombiner();
    float ul_x = 0.0f;
    float ul_y = 0.0f;
  float ul_u = 0.0f;
  float ul_v = 0.0f;
  float lr_x = (float)rdp.hires_tex->scr_width;
  float lr_y = (float)rdp.hires_tex->scr_height;
  float lr_u = rdp.hires_tex->u_scale * (float)(rdp.hires_tex->width);//255.0f - (1024 - settings.res_x)*0.25f;
  float lr_v = rdp.hires_tex->v_scale * (float)(rdp.hires_tex->height);//255.0f - (1024 - settings.res_y)*0.25f;
  FRDP("lr_x: %f, lr_y: %f, lr_u: %f, lr_v: %f\n", lr_x, lr_y, lr_u, lr_v);


  // Make the vertices
  VERTEX v[4] = {
    { ul_x, ul_y, 1, 1, ul_u, ul_v, ul_u, ul_v },
    { lr_x, ul_y, 1, 1, lr_u, ul_v, lr_u, ul_v },
    { ul_x, lr_y, 1, 1, ul_u, lr_v, ul_u, lr_v },
    { lr_x, lr_y, 1, 1, lr_u, lr_v, lr_u, lr_v } };
  ConvertCoordsConvert (v, 4);

  grTexSource( rdp.hires_tex->tmu, rdp.hires_tex->tex_addr, GR_MIPMAPLEVELMASK_BOTH, &(rdp.hires_tex->info) );
  grDrawTriangle (&v[0], &v[2], &v[1]);
  grDrawTriangle (&v[2], &v[3], &v[1]);
  rdp.hires_tex->info.format = buf_format;
  rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | UPDATE_TEXTURE | UPDATE_ALPHA_COMPARE;
  if (settings.fog && (rdp.flags & FOG_ENABLED))
  {
   grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
  }
  RDP("CloseTextureBuffer draw, OK\n");
  rdp.hires_tex = 0;
  return TRUE;
}

BOOL CopyTextureBuffer(COLOR_IMAGE & fb_from, COLOR_IMAGE & fb_to)
{
  if (!fullscreen) 
    return FALSE;
  RDP("CopyTextureBuffer. ");
  if (rdp.cur_image)
  {
    if (rdp.cur_image->addr == fb_to.addr)
      return CloseTextureBuffer(TRUE);
    rdp.hires_tex = rdp.cur_image;
  }
  else if (!FindTextureBuffer(fb_from.addr, (WORD)fb_from.width))
  {
    RDP("Can't find 'from' buffer.\n");
    return FALSE;
  }
  if (!OpenTextureBuffer(fb_to))
  {
    RDP("Can't open new buffer.\n");
    return CloseTextureBuffer(TRUE);
  }
  GrTextureFormat_t buf_format = rdp.hires_tex->info.format;
  rdp.hires_tex->info.format = GR_TEXFMT_RGB_565;
  TexBufSetupCombiner(TRUE);
  float ul_x = 0.0f;
  float ul_y = 0.0f;
  float lr_x = (float)rdp.hires_tex->scr_width;
  float lr_y = (float)rdp.hires_tex->scr_height;
  float lr_u = rdp.hires_tex->u_scale * (float)(rdp.hires_tex->width);//255.0f - (1024 - settings.res_x)*0.25f;
  float lr_v = rdp.hires_tex->v_scale * (float)(rdp.hires_tex->height);//255.0f - (1024 - settings.res_y)*0.25f;
  FRDP("lr_x: %f, lr_y: %f\n", lr_x, lr_y);


    // Make the vertices
    VERTEX v[4] = {
        { ul_x, ul_y, 1, 1, 0, 0, 0, 0 },
        { lr_x, ul_y, 1, 1, lr_u, 0, lr_u, 0},
        { ul_x, lr_y, 1, 1, 0, lr_v, 0, lr_v},
        { lr_x, lr_y, 1, 1, lr_u, lr_v, lr_u, lr_v} };
    ConvertCoordsConvert (v, 4);

    grTexSource( rdp.hires_tex->tmu, rdp.hires_tex->tex_addr, GR_MIPMAPLEVELMASK_BOTH, &(rdp.hires_tex->info) );
    grDrawTriangle (&v[0], &v[2], &v[1]);
    grDrawTriangle (&v[2], &v[3], &v[1]);
  grRenderBuffer( GR_BUFFER_BACKBUFFER );
  grDrawTriangle (&v[0], &v[2], &v[1]);
  grDrawTriangle (&v[2], &v[3], &v[1]);
  rdp.hires_tex->info.format = buf_format;

    rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | UPDATE_TEXTURE | UPDATE_ALPHA_COMPARE;
  if (settings.fog && (rdp.flags & FOG_ENABLED))
    grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
  RDP("CopyTextureBuffer draw, OK\n");
    rdp.hires_tex = 0;
  rdp.cur_image = 0;
    return TRUE;
}

BOOL SwapTextureBuffer()
{
  if (!fullscreen || !rdp.hires_tex) 
    return FALSE;
  RDP("SwapTextureBuffer.");
  HIRES_COLOR_IMAGE * texbuf = AllocateTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
  if (!texbuf)
  {
    RDP(" failed!\n");
    return FALSE;
  }
  TexBufSetupCombiner();

    float ul_x = 0.0f;
    float ul_y = 0.0f;
    float lr_x = (float)rdp.hires_tex->scr_width;
    float lr_y = (float)rdp.hires_tex->scr_height;
    float lr_u = rdp.hires_tex->u_scale * (float)(rdp.hires_tex->width);//255.0f - (1024 - settings.res_x)*0.25f;
    float lr_v = rdp.hires_tex->v_scale * (float)(rdp.hires_tex->height);//255.0f - (1024 - settings.res_y)*0.25f;

    // Make the vertices
    VERTEX v[4] = {
        { ul_x, ul_y, 1, 1, 0, 0, 0, 0 },
        { lr_x, ul_y, 1, 1, lr_u, 0, lr_u, 0},
        { ul_x, lr_y, 1, 1, 0, lr_v, 0, lr_v},
        { lr_x, lr_y, 1, 1, lr_u, lr_v, lr_u, lr_v} };
    int tex = rdp.tex;
    rdp.tex = 1;
    ConvertCoordsConvert (v, 4);
    rdp.tex = tex;

    grTexSource( rdp.hires_tex->tmu, rdp.hires_tex->tex_addr, GR_MIPMAPLEVELMASK_BOTH, &(rdp.hires_tex->info) );
    texbuf->tile_uls = rdp.hires_tex->tile_uls;
    texbuf->tile_ult = rdp.hires_tex->tile_ult;
    texbuf->v_shift = rdp.hires_tex->v_shift;
    rdp.cur_image = texbuf;
    grRenderBuffer( GR_BUFFER_TEXTUREBUFFER_EXT );
    grSstOrigin(GR_ORIGIN_UPPER_LEFT);
    grTextureBufferExt( rdp.cur_image->tmu, rdp.cur_image->tex_addr, rdp.cur_image->info.smallLodLog2, rdp.cur_image->info.largeLodLog2,
      rdp.cur_image->info.aspectRatioLog2, rdp.cur_image->info.format, GR_MIPMAPLEVELMASK_BOTH ); 
    grDrawTriangle (&v[0], &v[2], &v[1]);
    grDrawTriangle (&v[2], &v[3], &v[1]);
    rdp.texbufs[rdp.hires_tex->tmu].clear_allowed = TRUE;
    rdp.texbufs[rdp.hires_tex->tmu].count = 0;
    rdp.hires_tex = rdp.cur_image;
    rdp.cur_image = 0;
    grRenderBuffer( GR_BUFFER_BACKBUFFER );

    rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | UPDATE_TEXTURE | UPDATE_ALPHA_COMPARE;
    if (settings.fog && (rdp.flags & FOG_ENABLED))
    {
        grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
    }
    RDP("SwapTextureBuffer draw, OK\n");
    return TRUE;
}

BOOL FindTextureBuffer(DWORD addr, WORD width)
{
  if (rdp.skip_drawing)
    return FALSE;
  FRDP("FindTextureBuffer. addr: %08lx, width: %d, scale_x: %f\n", addr, width, rdp.scale_x);
  BOOL found = FALSE;
  DWORD shift = 0;
  for (int i = 0; i < num_tmu && !found; i++)
  {  
    BYTE index = rdp.cur_tex_buf^i;
    for (int j = 0; j < rdp.texbufs[index].count && !found; j++)
    {
      rdp.hires_tex = &(rdp.texbufs[index].images[j]);
      if(addr >= rdp.hires_tex->addr && addr < rdp.hires_tex->end_addr)// && rdp.timg.format == 0)
      {
        if (width == 1 || rdp.hires_tex->width == width)
        {
          shift = addr - rdp.hires_tex->addr;
          if (!rdp.motionblur)
            rdp.cur_tex_buf = index;
          found = TRUE;
//    FRDP("FindTextureBuffer, found in TMU%d buffer: %d\n", rdp.hires_tex->tmu, j);
        }
        else //new texture is loaded into this place, texture buffer is not valid anymore
        {
          rdp.texbufs[index].count--;
          if (j < rdp.texbufs[index].count)
             memcpy(&(rdp.texbufs[index].images[j]), &(rdp.texbufs[index].images[j+1]), sizeof(HIRES_COLOR_IMAGE)*(rdp.texbufs[index].count-j));
        }
      }
    }
  }
  if (found)
  {
    rdp.hires_tex->tile_uls = 0;
    rdp.hires_tex->tile_ult = 0;
    if (shift > 0)
    {
      shift >>= 1;
      rdp.hires_tex->v_shift = shift / rdp.hires_tex->width;
      rdp.hires_tex->u_shift = shift % rdp.hires_tex->width;
    }
    else
    {
      rdp.hires_tex->v_shift = 0;
      rdp.hires_tex->u_shift = 0;
    }
    /*
    if (rdp.timg.format == 0) //RGB
      rdp.hires_tex->info.format = GR_TEXFMT_RGB_565;
    else  //IA
      rdp.hires_tex->info.format = GR_TEXFMT_ALPHA_INTENSITY_88;
    */
    FRDP("FindTextureBuffer, found, v_shift: %d, format: %d\n", rdp.hires_tex->v_shift, rdp.hires_tex->info.format);
    return TRUE;
  }
  rdp.hires_tex = 0;
  RDP("FindTextureBuffer, not found\n");
  return FALSE;
}

