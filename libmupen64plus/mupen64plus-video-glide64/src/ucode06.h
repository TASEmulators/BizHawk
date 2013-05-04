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

//****************************************************************

// STANDARD DRAWIMAGE - draws a 2d image based on the following structure

#define max(a,b) ((a) > (b) ? (a) : (b))
#define min(a,b) ((a) < (b) ? (a) : (b))

void uc6_sprite2d ();

typedef struct DRAWIMAGE_t {
    float frameX;
    float frameY;
    WORD frameW;
    WORD frameH;
    WORD imageX;
    WORD imageY;
    WORD imageW;
    WORD imageH;
    DWORD imagePtr;
    BYTE imageFmt;
    BYTE imageSiz;
    WORD imagePal;
    BYTE flipX;
    BYTE flipY;
    float scaleX;
    float scaleY;
} DRAWIMAGE;

void DrawHiresDepthImage (DRAWIMAGE *d)
{
  WORD * src = (WORD*)(gfx.RDRAM+d->imagePtr);
  WORD image[512*512];
  WORD * dst = image;
  for (int h = 0; h < d->imageH; h++)
  {
    for (int w = 0; w < d->imageW; w++)
    {
      *(dst++) = src[(w+h*d->imageW)^1];
    }
    dst += (512 - d->imageW);
  }
    GrTexInfo t_info;
  t_info.format = GR_TEXFMT_RGB_565;
  t_info.data = image;
  t_info.smallLodLog2 = GR_LOD_LOG2_512;
  t_info.largeLodLog2 = GR_LOD_LOG2_512;
  t_info.aspectRatioLog2 = GR_ASPECT_LOG2_1x1;
    
    grTexDownloadMipMap (rdp.texbufs[1].tmu,
     rdp.texbufs[1].begin,
     GR_MIPMAPLEVELMASK_BOTH,
     &t_info);
  grTexSource (rdp.texbufs[1].tmu,
     rdp.texbufs[1].begin,
     GR_MIPMAPLEVELMASK_BOTH,
     &t_info);
  grTexCombine( GR_TMU1, 
     GR_COMBINE_FUNCTION_LOCAL, 
     GR_COMBINE_FACTOR_NONE, 
     GR_COMBINE_FUNCTION_LOCAL, 
     GR_COMBINE_FACTOR_NONE, 
     FXFALSE, 
     FXFALSE ); 
  grTexCombine( GR_TMU0, 
     GR_COMBINE_FUNCTION_SCALE_OTHER, 
     GR_COMBINE_FACTOR_ONE, 
     GR_COMBINE_FUNCTION_SCALE_OTHER, 
     GR_COMBINE_FACTOR_ONE, 
     FXFALSE, 
     FXFALSE ); 
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
  grAlphaBlendFunction (GR_BLEND_ONE,   
     GR_BLEND_ZERO,
     GR_BLEND_ONE,
     GR_BLEND_ZERO);
  grDepthBufferFunction (GR_CMP_ALWAYS);
  grDepthMask (FXFALSE);
      
  GrLOD_t LOD = GR_LOD_LOG2_1024;
  if (settings.scr_res_x > 1024)
    LOD = GR_LOD_LOG2_2048;
      
  float lr_x = (float)d->imageW * rdp.scale_x;
  float lr_y = (float)d->imageH * rdp.scale_y;
  float lr_u = (float)d->imageW * 0.5f;// - 0.5f;
  float lr_v = (float)d->imageH * 0.5f;// - 0.5f;
  VERTEX v[4] = {
    { 0, 0, 1.0f, 1.0f, 0, 0, 0, 0 },
    { lr_x, 0, 1.0f, 1.0f, lr_u, 0, lr_u, 0 },
    { 0, lr_y, 1.0f, 1.0f, 0, lr_v, 0, lr_v },
    { lr_x, lr_y, 1.0f, 1.0f, lr_u, lr_v, lr_u, lr_v } 
  };
  for (int i=0; i<4; i++)
  {
    v[i].uc(0) = v[i].uc(1) = v[i].u0;
    v[i].vc(0) = v[i].vc(1) = v[i].v0;
  }
  grTextureBufferExt( rdp.texbufs[0].tmu, rdp.texbufs[0].begin, LOD, LOD,
    GR_ASPECT_LOG2_1x1, GR_TEXFMT_RGB_565, GR_MIPMAPLEVELMASK_BOTH ); 
  grRenderBuffer( GR_BUFFER_TEXTUREBUFFER_EXT );
  grAuxBufferExt( GR_BUFFER_AUXBUFFER );
  grSstOrigin(GR_ORIGIN_UPPER_LEFT);
  grBufferClear (0, 0, 0xFFFF);
  grDrawTriangle (&v[0], &v[2], &v[1]);
  grDrawTriangle (&v[2], &v[3], &v[1]);
  grRenderBuffer( GR_BUFFER_BACKBUFFER );
  grTextureAuxBufferExt( rdp.texbufs[0].tmu, rdp.texbufs[0].begin, LOD, LOD,
    GR_ASPECT_LOG2_1x1, GR_TEXFMT_RGB_565, GR_MIPMAPLEVELMASK_BOTH ); 
  grAuxBufferExt( GR_BUFFER_TEXTUREAUXBUFFER_EXT );
  grDepthMask (FXTRUE);
}


extern BOOL depthbuffersave;
void DrawDepthImage (DRAWIMAGE *d)
{
  if (!fullscreen || !settings.fb_depth_render)
    return;
  if (d->imageH > d->imageW)
    return;
  RDP("Depth image write\n");
  float scale_x_dst = rdp.scale_x;
  float scale_y_dst = rdp.scale_y;
  float scale_x_src = 1.0f/rdp.scale_x;
  float scale_y_src = 1.0f/rdp.scale_y;
  int src_width = d->imageW;
  int src_height = d->imageH;
  int dst_width = min(int(src_width*scale_x_dst), (int)settings.scr_res_x);
  int dst_height = min(int(src_height*scale_y_dst), (int)settings.scr_res_y);

#if 1
  if (0 && grFramebufferCopyExt) {
    static unsigned int last;
    unsigned int crc = CRC_Calculate(0, gfx.RDRAM+d->imagePtr,
                                     d->imageW * d->imageH * 2);
    printf("depth CRC %x\n", crc);
    if (last == crc) {
      // ZIGGY
      // using special idiot setting FRONT-->FRONT so the wrapper knows what to
      // do (i.e. delay actual copy until after next buffer clear)
      // UGLY !!
      grFramebufferCopyExt(0, 0, dst_width, dst_height,
                           GR_FBCOPY_BUFFER_FRONT, GR_FBCOPY_BUFFER_FRONT,
                           GR_FBCOPY_MODE_DEPTH);
      depthbuffersave = TRUE;
      return;
    }

    last = crc;
    depthbuffersave = TRUE;    
                                     
  } else {
    if (settings.fb_hires)
    {
      DrawHiresDepthImage(d);
      return;
    }
  }
#endif

  WORD * src = (WORD*)(gfx.RDRAM+d->imagePtr);
  WORD * dst = new WORD[dst_width*dst_height];

  for (int y=0; y < dst_height; y++)
  {
    for (int x=0; x < dst_width; x++)
    {
      dst[x+y*dst_width] = src[(int(x*scale_x_src)+int(y*scale_y_src)*src_width)^1];
    }
  }
  grLfbWriteRegion(GR_BUFFER_AUXBUFFER,
    0,
    0,
    GR_LFB_SRC_FMT_ZA16,
    dst_width,
    dst_height,
    FXFALSE,
    dst_width<<1,
    dst);
  delete[] dst;
}

void DrawImage (DRAWIMAGE *d)
{
  if (d->imageW == 0 || d->imageH == 0 || d->frameH == 0)   return;
    
    int x_size;
    int y_size;
    int x_shift;
    int y_shift;
    int line;
    
    // choose optimum size for the format/size
    if (d->imageSiz == 0)
    {
        x_size = 128;
        y_size = 64;
        x_shift = 7;
        y_shift = 6;
        line = 8;
    }
    if (d->imageSiz == 1)
    {
        x_size = 64;
        y_size = 64;
        x_shift = 6;
        y_shift = 6;
        line = 8;
    }
    if (d->imageSiz == 2)
    {
        x_size = 64;
        y_size = 32;
        x_shift = 6;
        y_shift = 5;
        line = 16;
    }
    if (d->imageSiz == 3)
    {
        x_size = 32;
    y_size = 16;
    x_shift = 4;
    y_shift = 3;
        line = 16;
    }
  if (rdp.ci_width == 512 && !no_dlist) //RE2
  {
    WORD width = (WORD)(*gfx.VI_WIDTH_REG & 0xFFF);
    d->frameH = d->imageH = (d->frameW*d->frameH)/width;
    d->frameW = d->imageW = width; 
    if (rdp.zimg == rdp.cimg)
    {
      DrawDepthImage(d);
      rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | 
        UPDATE_ALPHA_COMPARE | UPDATE_VIEWPORT;
    return;
    }
  }
    if (d->imageW%2 == 1) d->imageW -= 1;
    if (d->imageH%2 == 1) d->imageH -= 1;
    if (d->imageY > d->imageH) d->imageY = (d->imageY%d->imageH);
    //  if (d->imageX > d->imageW) d->imageX = (d->imageX%d->imageW);
    
    if (!settings.PPL)
    {
        if ( (d->frameX > 0) && (d->frameW == rdp.ci_width) )
            d->frameW -= (WORD)(2.0f*d->frameX);
        if ( (d->frameY > 0) && (d->frameH == rdp.ci_height) )
            d->frameH -= (WORD)(2.0f*d->frameY);
    }
    
    int ul_u = (int)d->imageX;
    int ul_v = (int)d->imageY;
    int lr_u = (int)d->imageX + (int)(d->frameW * d->scaleX);
    int lr_v = (int)d->imageY + (int)(d->frameH * d->scaleY);
    
    float ul_x, ul_y, lr_x, lr_y;
    if (d->flipX)
    {
        ul_x = d->frameX + d->frameW;
        lr_x = d->frameX;
    }
    else
    {
        ul_x = d->frameX;
        lr_x = d->frameX + d->frameW;
    }
    if (d->flipY)
    {
        ul_y = d->frameY + d->frameH;
        lr_y = d->frameY;
    }
    else
    {
        ul_y = d->frameY;
        lr_y = d->frameY + d->frameH;
    }

    int min_wrap_u = ul_u / d->imageW;
    //int max_wrap_u = lr_u / d->wrapW;
    int min_wrap_v = ul_v / d->imageH;
    //int max_wrap_v = lr_v / d->wrapH;
    int min_256_u = ul_u >> x_shift;
    //int max_256_u = (lr_u-1) >> x_shift;
    int min_256_v = ul_v >> y_shift;
    //int max_256_v = (lr_v-1) >> y_shift;
    
    
    // SetTextureImage ()
    rdp.timg.format = d->imageFmt;  // RGBA
    rdp.timg.size = d->imageSiz;        // 16-bit
    rdp.timg.addr = d->imagePtr;
    rdp.timg.width = d->imageW;
    rdp.timg.set_by = 0;
    
    // SetTile ()
    TILE *tile = &rdp.tiles[0];
    tile->format = d->imageFmt; // RGBA
    tile->size = d->imageSiz;       // 16-bit
    tile->line = line;
    tile->t_mem = 0;
    tile->palette = (BYTE)d->imagePal;
    tile->clamp_t = 1;
    tile->mirror_t = 0;
    tile->mask_t = 0;
    tile->shift_t = 0;
    tile->clamp_s = 1;
    tile->mirror_s = 0;
    tile->mask_s = 0;
    tile->shift_s = 0;
    
    rdp.tiles[0].ul_s = 0;
    rdp.tiles[0].ul_t = 0;
    rdp.tiles[0].lr_s = x_size-1;
    rdp.tiles[0].lr_t = y_size-1;
    
    if (rdp.cycle_mode == 2)
    {
        rdp.allow_combine = 0;
        rdp.update &= ~UPDATE_TEXTURE;
    }
    update ();              // note: allow loading of texture
    
    float Z = 1.0f;
    
    if (fullscreen)
    {
        
        //grFogMode (GR_FOG_DISABLE);
        
    grFogMode (GR_FOG_DISABLE);
        if (rdp.zsrc == 1 && (rdp.othermode_l & 0x00000030))  // othermode check makes sure it
            // USES the z-buffer.  Otherwise it returns bad (unset) values for lot and telescope
            //in zelda:mm.
        {
            RDP("Background uses depth compare\n");
      Z = ScaleZ(rdp.prim_depth);
            grDepthBufferFunction (GR_CMP_LEQUAL);
            grDepthMask (FXTRUE);
        }
        else
        {
            RDP("Background not uses depth compare\n");
            grDepthBufferFunction (GR_CMP_ALWAYS);
            grDepthMask (FXFALSE);
        }
        
        //      grClipWindow (0, 0, settings.res_x, settings.res_y);
    if (rdp.ci_width == 512 && !no_dlist)
      //          grClipWindow (0, 0, (DWORD)(d->frameW * rdp.scale_x), (DWORD)(d->frameH * rdp.scale_y));
      grClipWindow (0, 0, settings.scr_res_x, settings.scr_res_y);
    else
        grClipWindow (rdp.scissor.ul_x, rdp.scissor.ul_y, rdp.scissor.lr_x, rdp.scissor.lr_y);
        
        grCullMode (GR_CULL_DISABLE);
        //      if (!settings.PPL)
        if (rdp.cycle_mode == 2)
        {
            rdp.allow_combine = 0;
      cmb.tmu0_func = GR_COMBINE_FUNCTION_LOCAL;
      cmb.tmu0_a_func = GR_COMBINE_FUNCTION_LOCAL;
            
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
            grConstantColorValue (0xFFFFFFFF);
            grAlphaBlendFunction (GR_BLEND_ONE, // use alpha compare, but not T0 alpha
                GR_BLEND_ZERO,
                GR_BLEND_ZERO,
                GR_BLEND_ZERO);
            rdp.update |= UPDATE_COMBINE;
        }
        
    }
    // Texture ()
    rdp.cur_tile = 0;
    rdp.tex = 1;
    
    float nul_x, nul_y, nlr_x, nlr_y;
    int nul_u, nul_v, nlr_u, nlr_v;
    float ful_u, ful_v, flr_u, flr_v;
    float ful_x, ful_y, flr_x, flr_y;
    
    float mx = (float)(lr_x - ul_x) / (float)(lr_u - ul_u);
    float bx = ul_x - mx * ul_u;
    
    float my = (float)(lr_y - ul_y) / (float)(lr_v - ul_v);
    float by = ul_y - my * ul_v;
    
    int cur_wrap_u, cur_wrap_v, cur_u, cur_v;
    int cb_u, cb_v; // coordinate-base
    int tb_u, tb_v; // texture-base
    
    nul_v = ul_v;
    nul_y = ul_y;
    
    // #162
    
    cur_wrap_v = min_wrap_v + 1;
    cur_v = min_256_v + 1;
    cb_v = ((cur_v-1)<<y_shift);
    while (cb_v >= d->imageH) cb_v -= d->imageH;
    tb_v = cb_v;
    
    while (1)
    {
        cur_wrap_u = min_wrap_u + 1 + 1024; // x wrapping is not required
        cur_u = min_256_u + 1;
        
        // calculate intersection with this point
        nlr_v = min (min (cur_wrap_v*d->imageH, (cur_v<<y_shift)), lr_v);
        nlr_y = my * nlr_v + by;
        
        nul_u = ul_u;
        nul_x = ul_x;
        cb_u = ((cur_u-1)<<x_shift);
        while (cb_u >= d->imageW) cb_u -= d->imageW;
        tb_u = cb_u;
        
        while (1)
        {
            // calculate intersection with this point
            nlr_u = min (min (cur_wrap_u*d->imageW, (cur_u<<x_shift)), lr_u);
            nlr_x = mx * nlr_u + bx;
            
            // ** Load the texture, constant portions have been set above
            // SetTileSize ()
            rdp.tiles[0].ul_s = tb_u;
            rdp.tiles[0].ul_t = tb_v;
            rdp.tiles[0].lr_s = tb_u+x_size-1;
            rdp.tiles[0].lr_t = tb_v+y_size-1;
            
            // LoadTile ()
            rdp.cmd0 = ((int)rdp.tiles[0].ul_s << 14) | ((int)rdp.tiles[0].ul_t << 2);
            rdp.cmd1 = ((int)rdp.tiles[0].lr_s << 14) | ((int)rdp.tiles[0].lr_t << 2);
            rdp_loadtile ();
            
            TexCache ();
            // **
            
            ful_u = (float)nul_u - cb_u;
            flr_u = (float)nlr_u - cb_u;
            ful_v = (float)nul_v - cb_v;
            flr_v = (float)nlr_v - cb_v;
            
            ful_u *= rdp.cur_cache[0]->scale;
            ful_v *= rdp.cur_cache[0]->scale;
            flr_u *= rdp.cur_cache[0]->scale;      
            flr_v *= rdp.cur_cache[0]->scale;
            
            ful_x = nul_x * rdp.scale_x;
            flr_x = nlr_x * rdp.scale_x;
            ful_y = nul_y * rdp.scale_y;
            flr_y = nlr_y * rdp.scale_y;
            
            // Make the vertices
            
            if ((flr_x <= rdp.scissor.lr_x) || (ful_x < rdp.scissor.lr_x))
            {
                VERTEX v[4] = {
          { ful_x, ful_y, Z, 1.0f, ful_u, ful_v },
          { flr_x, ful_y, Z, 1.0f, flr_u, ful_v },
          { ful_x, flr_y, Z, 1.0f, ful_u, flr_v },
          { flr_x, flr_y, Z, 1.0f, flr_u, flr_v } };
                    AllowShadeMods (v, 4);
                    for (int s = 0; s < 4; s++)
                        apply_shade_mods (&(v[s]));
                    ConvertCoordsConvert (v, 4);
                    
              if (fullscreen)// && /*hack for Zelda MM. Gonetz*/rdp.cur_cache[0]->addr > 0xffff && rdp.cur_cache[0]->crc != 0)
              {
                  grDrawVertexArrayContiguous (GR_TRIANGLE_STRIP, 4, v, sizeof(VERTEX));
              }
                    
                    if (debug.capture)
                    {
                        VERTEX vl[3];
                        vl[0] = v[0];
                        vl[1] = v[2];
                        vl[2] = v[1];
                        add_tri (vl, 3, TRI_BACKGROUND);
                        rdp.tri_n ++;
                        vl[0] = v[2];
                        vl[1] = v[3];
                        vl[2] = v[1];
                        add_tri (vl, 3, TRI_BACKGROUND);
                        rdp.tri_n ++;
                    }
                    else
                        rdp.tri_n += 2;
            }
            else
            {
                rdp.tri_n += 2;
                RDP("Clipped!\n");
            }
            
            // increment whatever caused this split
            tb_u += x_size - (x_size-(nlr_u-cb_u));
            cb_u = nlr_u;
            if (nlr_u == cur_wrap_u*d->imageW) 
            {
                cur_wrap_u ++;
                tb_u = 0;
            }
            if (nlr_u == (cur_u<<x_shift)) cur_u ++;
            if (nlr_u == lr_u) break;
            nul_u = nlr_u;
            nul_x = nlr_x;
        }
        
        tb_v += y_size - (y_size-(nlr_v-cb_v));
        cb_v = nlr_v;
        if (nlr_v == cur_wrap_v*d->imageH) {
            cur_wrap_v ++;
            tb_v = 0;
        }
        if (nlr_v == (cur_v<<y_shift)) cur_v ++;
        if (nlr_v == lr_v) break;
        nul_v = nlr_v;
        nul_y = nlr_y;
    }
    
    rdp.allow_combine = 1;
    
    rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | UPDATE_TEXTURE | UPDATE_ALPHA_COMPARE
        | UPDATE_VIEWPORT;
    
  if (fullscreen && settings.fog && (rdp.flags & FOG_ENABLED))
  {
    grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
  }
}


void DrawHiresImage(DRAWIMAGE *d, BOOL screensize = FALSE)
{
    FRDP("DrawHiresImage. addr: %08lx\n", d->imagePtr);
    if (!fullscreen) 
        return;
    HIRES_COLOR_IMAGE *hires_tex = (rdp.motionblur)?&(rdp.texbufs[rdp.cur_tex_buf^1].images[0]):rdp.hires_tex;
    
    if (rdp.cycle_mode == 2)
    {
        rdp.allow_combine = 0;
        rdp.update &= ~UPDATE_TEXTURE;
    }
    update ();              // note: allow loading of texture
    
  float Z = 1.0f;
  if (rdp.zsrc == 1 && (rdp.othermode_l & 0x00000030)) 
    {
        RDP("Background uses depth compare\n");
    Z = ScaleZ(rdp.prim_depth);
        grDepthBufferFunction (GR_CMP_LEQUAL);
    }
    else
    {
        RDP("Background not uses depth compare\n");
        grDepthBufferFunction (GR_CMP_ALWAYS);
  }
        grDepthMask (FXFALSE);
    grClipWindow (0, 0, settings.res_x, settings.res_y);
    grCullMode (GR_CULL_DISABLE);
    if (rdp.cycle_mode == 2)
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
        grConstantColorValue (0xFFFFFFFF);
        grAlphaBlendFunction (GR_BLEND_ONE, // use alpha compare, but not T0 alpha
            GR_BLEND_ZERO,
            GR_BLEND_ZERO,
            GR_BLEND_ZERO);
        rdp.allow_combine = 1;
    }
    
    if (hires_tex->tmu == GR_TMU0)
    {
        grTexCombine( GR_TMU1, 
            GR_COMBINE_FUNCTION_NONE, 
            GR_COMBINE_FACTOR_NONE, 
            GR_COMBINE_FUNCTION_NONE, 
            GR_COMBINE_FACTOR_NONE, 
            FXFALSE, 
            FXFALSE ); 
        grTexCombine( GR_TMU0, 
            GR_COMBINE_FUNCTION_LOCAL, 
            GR_COMBINE_FACTOR_NONE, 
            GR_COMBINE_FUNCTION_LOCAL, 
            GR_COMBINE_FACTOR_NONE, 
            FXFALSE, 
            FXFALSE ); 
    }
    else
    {
        grTexCombine( GR_TMU1, 
            GR_COMBINE_FUNCTION_LOCAL, 
            GR_COMBINE_FACTOR_NONE, 
            GR_COMBINE_FUNCTION_LOCAL, 
            GR_COMBINE_FACTOR_NONE, 
            FXFALSE, 
            FXFALSE ); 
        grTexCombine( GR_TMU0, 
            GR_COMBINE_FUNCTION_SCALE_OTHER, 
            GR_COMBINE_FACTOR_ONE, 
            GR_COMBINE_FUNCTION_SCALE_OTHER, 
            GR_COMBINE_FACTOR_ONE, 
            FXFALSE, 
            FXFALSE ); 
    }
    grTexSource( hires_tex->tmu, hires_tex->tex_addr, GR_MIPMAPLEVELMASK_BOTH, &(hires_tex->info) );
    
  if (d->imageW%2 == 1) d->imageW -= 1;
  if (d->imageH%2 == 1) d->imageH -= 1;
  if (d->imageY > d->imageH) d->imageY = (d->imageY%d->imageH);
  
  if (!settings.PPL)
  {
    if ( (d->frameX > 0) && (d->frameW == rdp.ci_width) )
      d->frameW -= (WORD)(2.0f*d->frameX);
    if ( (d->frameY > 0) && (d->frameH == rdp.ci_height) )
      d->frameH -= (WORD)(2.0f*d->frameY);
  }
  
  float ul_x, ul_y, ul_u, ul_v, lr_x, lr_y, lr_u, lr_v;
  if (screensize)
  {
    ul_x = 0.0f;
    ul_y = 0.0f;
    ul_u = 0.0f;
    ul_v = 0.0f;
    lr_x = (float)rdp.hires_tex->scr_width;
    lr_y = (float)rdp.hires_tex->scr_height;
    lr_u = rdp.hires_tex->u_scale * (float)(rdp.hires_tex->width);//255.0f - (1024 - settings.res_x)*0.25f;
    lr_v = rdp.hires_tex->v_scale * (float)(rdp.hires_tex->height);//255.0f - (1024 - settings.res_y)*0.25f;
  }
  else
  {
    ul_u = d->imageX;
    ul_v = d->imageY;
    lr_u = d->imageX + (d->frameW * d->scaleX) ;
    lr_v = d->imageY + (d->frameH * d->scaleY) ;
    
    ul_x = d->frameX;
    ul_y = d->frameY;
    
    lr_x = d->frameX + d->frameW;
    lr_y = d->frameY + d->frameH;
    ul_x *= rdp.scale_x;
    lr_x *= rdp.scale_x;
    ul_y *= rdp.scale_y;
    lr_y *= rdp.scale_y;
    ul_u *= rdp.hires_tex->u_scale;
    lr_u *= rdp.hires_tex->u_scale;
    ul_v *= rdp.hires_tex->v_scale;
    lr_v *= rdp.hires_tex->v_scale;
    if (lr_x > rdp.scissor.lr_x) lr_x = (float)rdp.scissor.lr_x;
    if (lr_y > rdp.scissor.lr_y) lr_y = (float)rdp.scissor.lr_y;
    }
    // Make the vertices
    VERTEX v[4] = {
    { ul_x, ul_y, Z, 1.0f, ul_u, ul_v, ul_u, ul_v },
    { lr_x, ul_y, Z, 1.0f, lr_u, ul_v, lr_u, ul_v },
    { ul_x, lr_y, Z, 1.0f, ul_u, lr_v, ul_u, lr_v },
    { lr_x, lr_y, Z, 1.0f, lr_u, lr_v, lr_u, lr_v } };
        ConvertCoordsConvert (v, 4);
        AllowShadeMods (v, 4);
        for (int s = 0; s < 4; s++)
            apply_shade_mods (&(v[s]));
        grDrawTriangle (&v[0], &v[2], &v[1]);
        grDrawTriangle (&v[2], &v[3], &v[1]);
    rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_COMBINE | UPDATE_TEXTURE | UPDATE_ALPHA_COMPARE | UPDATE_VIEWPORT;
}

//****************************************************************


struct mat2d_t {
    float A, B, C, D;
    float X, Y;
    float BaseScaleX;
    float BaseScaleY;
} mat_2d;

static void uc6_bg_1cyc ()
{
  if (render_depth_mode == 2) {
    // ZIGGY
    // Zelda LoT effect save/restore depth buffer
    RDP("bg_1cyc: saving depth buffer\n");
    printf("bg_1cyc: saving depth buffer\n");
    if (grFramebufferCopyExt)
      grFramebufferCopyExt(0, 0, settings.scr_res_x, settings.scr_res_y,
                           GR_FBCOPY_BUFFER_BACK, GR_FBCOPY_BUFFER_FRONT,
                           GR_FBCOPY_MODE_DEPTH);
    return;
  }
      
    if (rdp.skip_drawing)
    {
        RDP("bg_1cyc skipped\n");
        return;
    }
    FRDP ("uc6:bg_1cyc #%d, #%d\n", rdp.tri_n, rdp.tri_n+1);
    FRDP_E ("uc6:bg_1cyc #%d, #%d\n", rdp.tri_n, rdp.tri_n+1);
    
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    DRAWIMAGE d;
    
    d.imageX    = (((WORD *)gfx.RDRAM)[(addr+0)^1] >> 5);   // 0
    d.imageW    = (((WORD *)gfx.RDRAM)[(addr+1)^1] >> 2);   // 1
    d.frameX    = ((short*)gfx.RDRAM)[(addr+2)^1] / 4.0f;   // 2
    d.frameW    = ((WORD *)gfx.RDRAM)[(addr+3)^1] >> 2;     // 3
    
    d.imageY    = (((WORD *)gfx.RDRAM)[(addr+4)^1] >> 5);   // 4
    d.imageH    = (((WORD *)gfx.RDRAM)[(addr+5)^1] >> 2);   // 5
    d.frameY    = ((short*)gfx.RDRAM)[(addr+6)^1] / 4.0f;   // 6
    d.frameH    = ((WORD *)gfx.RDRAM)[(addr+7)^1] >> 2;     // 7
    
    d.imagePtr  = segoffset(((DWORD*)gfx.RDRAM)[(addr+8)>>1]);  // 8,9
    //  WORD  imageLoad = ((WORD *)gfx.RDRAM)[(addr+10)^1]; // 10
    d.imageFmt  = ((BYTE *)gfx.RDRAM)[(((addr+11)<<1)+0)^3];    // 11
    d.imageSiz  = ((BYTE *)gfx.RDRAM)[(((addr+11)<<1)+1)^3];    // |
    d.imagePal  = ((WORD *)gfx.RDRAM)[(addr+12)^1]; // 12
    WORD imageFlip = ((WORD *)gfx.RDRAM)[(addr+13)^1];  // 13;
    d.flipX     = (BYTE)imageFlip&0x01;
    
    d.scaleX    = ((short *)gfx.RDRAM)[(addr+14)^1] / 1024.0f;  // 14
    d.scaleY    = ((short *)gfx.RDRAM)[(addr+15)^1] / 1024.0f;  // 15
    if (settings.doraemon2) //Doraemon 2 scale fix
    {
      if (d.frameW == d.imageW)
        d.scaleX    = 1.0f;
      if (d.frameH == d.imageH)
        d.scaleY    = 1.0f;
    }
    d.flipY     = 0;
    long imageYorig= ((long *)gfx.RDRAM)[(addr+16)>>1] >> 5;
  rdp.last_bg = d.imagePtr;
    
    FRDP ("imagePtr: %08lx\n", d.imagePtr);
    FRDP ("frameX: %f, frameW: %d, frameY: %f, frameH: %d\n", d.frameX, d.frameW, d.frameY, d.frameH);
    FRDP ("imageX: %d, imageW: %d, imageY: %d, imageH: %d\n", d.imageX, d.imageW, d.imageY, d.imageH);
    FRDP ("imageYorig: %d, scaleX: %f, scaleY: %f\n", imageYorig, d.scaleX, d.scaleY);
    FRDP ("imageFmt: %d, imageSiz: %d, imagePal: %d, imageFlip: %d\n", d.imageFmt, d.imageSiz, d.imagePal, d.flipX);
    if (settings.fb_hires && FindTextureBuffer(d.imagePtr, d.imageW))
    {
        DrawHiresImage(&d);
        return;
    }
    
    if (settings.ucode == 2 || settings.PPL)
    {
        if ( (d.imagePtr != rdp.cimg) && (d.imagePtr != rdp.ocimg) && d.imagePtr) //can't draw from framebuffer
            DrawImage (&d);
        else    
        {
            RDP("uc6:bg_1cyc skipped\n");
        }
    }
    else
        DrawImage (&d);
}

static void uc6_bg_copy ()
{
  if (render_depth_mode == 1) {
    // ZIGGY
    // Zelda LoT effect save/restore depth buffer
    RDP("bg_copy: restoring depth buffer\n");
    printf("bg_copy: restoring depth buffer\n");
    if (grFramebufferCopyExt)
      grFramebufferCopyExt(0, 0, settings.scr_res_x, settings.scr_res_y,
                           GR_FBCOPY_BUFFER_FRONT, GR_FBCOPY_BUFFER_BACK,
                           GR_FBCOPY_MODE_DEPTH);
    return;
  }
      
    if (rdp.skip_drawing)
    {
        RDP("bg_copy skipped\n");
        return;
    }
    FRDP ("uc6:bg_copy #%d, #%d\n", rdp.tri_n, rdp.tri_n+1);
    
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    DRAWIMAGE d;
    
    d.imageX    = (((WORD *)gfx.RDRAM)[(addr+0)^1] >> 5);   // 0
    d.imageW    = (((WORD *)gfx.RDRAM)[(addr+1)^1] >> 2);   // 1
    d.frameX    = ((short*)gfx.RDRAM)[(addr+2)^1] / 4.0f;   // 2
    d.frameW    = ((WORD *)gfx.RDRAM)[(addr+3)^1] >> 2;     // 3
    
    d.imageY    = (((WORD *)gfx.RDRAM)[(addr+4)^1] >> 5);   // 4
    d.imageH    = (((WORD *)gfx.RDRAM)[(addr+5)^1] >> 2);   // 5
    d.frameY    = ((short*)gfx.RDRAM)[(addr+6)^1] / 4.0f;   // 6
    d.frameH    = ((WORD *)gfx.RDRAM)[(addr+7)^1] >> 2;     // 7
    
    d.imagePtr  = segoffset(((DWORD*)gfx.RDRAM)[(addr+8)>>1]);  // 8,9
    d.imageFmt  = ((BYTE *)gfx.RDRAM)[(((addr+11)<<1)+0)^3];    // 11
    d.imageSiz  = ((BYTE *)gfx.RDRAM)[(((addr+11)<<1)+1)^3];    // |
    d.imagePal  = ((WORD *)gfx.RDRAM)[(addr+12)^1]; // 12
    WORD imageFlip = ((WORD *)gfx.RDRAM)[(addr+13)^1];  // 13;
    d.flipX     = (BYTE)imageFlip&0x01;
    
    d.scaleX    = 1.0f; // 14
    d.scaleY    = 1.0f; // 15
    d.flipY     = 0;
  rdp.last_bg = d.imagePtr;
    
    FRDP ("imagePtr: %08lx\n", d.imagePtr);
    FRDP ("frameX: %f, frameW: %d, frameY: %f, frameH: %d\n", d.frameX, d.frameW, d.frameY, d.frameH);
    FRDP ("imageX: %d, imageW: %d, imageY: %d, imageH: %d\n", d.imageX, d.imageW, d.imageY, d.imageH);
    FRDP ("imageFmt: %d, imageSiz: %d, imagePal: %d\n", d.imageFmt, d.imageSiz, d.imagePal);
    
    if (settings.fb_hires && FindTextureBuffer(d.imagePtr, d.imageW))
    {
        DrawHiresImage(&d);
        return;
    }
    
    if (settings.ucode == 2 || settings.PPL)
    {
        if ( (d.imagePtr != rdp.cimg) && (d.imagePtr != rdp.ocimg) && d.imagePtr)  //can't draw from framebuffer
            DrawImage (&d);
        else    
        {
            RDP("uc6:bg_copy skipped\n");
        }
    }
    else
        DrawImage (&d);
    
}

static void draw_splitted_triangle(VERTEX **vtx)
{
    vtx[0]->not_zclipped = vtx[1]->not_zclipped = vtx[2]->not_zclipped = 1;
    
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
                    rdp.vtxbuf[index].z = 1;
                    rdp.vtxbuf[index].q = 1;
                    rdp.vtxbuf[index].u0 = 0.5f;
                    rdp.vtxbuf[index].v0 = v1->v0 + (v2->v0 - v1->v0) * percent +
                        rdp.cur_cache[0]->c_scl_y * cur_256 * rdp.cur_cache[0]->splitheight;
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
                    rdp.vtxbuf[index].z = 1;
                    rdp.vtxbuf[index].q = 1;
                    rdp.vtxbuf[index].u0 = 0.5f;
                    rdp.vtxbuf[index].v0 = v2->v0 + (v1->v0 - v2->v0) * percent +
                        rdp.cur_cache[0]->c_scl_y * cur_256 * rdp.cur_cache[0]->splitheight;
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
                    rdp.vtxbuf[index++] = *v2;
                }
                else      // First is in, second is out, save intersection
                {
                    percent = (right_256 - v1->u0) / (v2->u0 - v1->u0);
                    rdp.vtxbuf[index].x = v1->x + (v2->x - v1->x) * percent;
                    rdp.vtxbuf[index].y = v1->y + (v2->y - v1->y) * percent;
                    rdp.vtxbuf[index].z = 1;
                    rdp.vtxbuf[index].q = 1;
                    rdp.vtxbuf[index].u0 = 255.5f;
                    rdp.vtxbuf[index].v0 = v1->v0 + (v2->v0 - v1->v0) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(v1->b + (v2->b - v1->b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(v1->g + (v2->g - v1->g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(v1->r + (v2->r - v1->r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(v1->a + (v2->a - v1->a) * percent);
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
                    rdp.vtxbuf[index].z = 1;
                    rdp.vtxbuf[index].q = 1;
                    rdp.vtxbuf[index].u0 = 255.5f;
                    rdp.vtxbuf[index].v0 = v2->v0 + (v1->v0 - v2->v0) * percent;
                    rdp.vtxbuf[index].b = (BYTE)(v2->b + (v1->b - v2->b) * percent);
                    rdp.vtxbuf[index].g = (BYTE)(v2->g + (v1->g - v2->g) * percent);
                    rdp.vtxbuf[index].r = (BYTE)(v2->r + (v1->r - v2->r) * percent);
                    rdp.vtxbuf[index++].a = (BYTE)(v2->a + (v1->a - v2->a) * percent);
                    
                    // Save the in point
                    rdp.vtxbuf[index++] = *v2;
                }
            }
        }
        rdp.n_global = index;
        
        do_triangle_stuff_2 ();
    }
}

static float set_sprite_combine_mode ()
{
    if (rdp.cycle_mode == 2)
    {
        rdp.tex = 1;
        rdp.allow_combine = 0;
        //*
    cmb.tmu1_func = cmb.tmu0_func = GR_COMBINE_FUNCTION_LOCAL;
    cmb.tmu1_fac = cmb.tmu0_fac = GR_COMBINE_FACTOR_NONE;
    cmb.tmu1_a_func = cmb.tmu0_a_func = GR_COMBINE_FUNCTION_LOCAL;
    cmb.tmu1_a_fac = cmb.tmu0_a_fac = GR_COMBINE_FACTOR_NONE;
    cmb.tmu1_invert = cmb.tmu0_invert = FXFALSE;
    cmb.tmu1_a_invert = cmb.tmu0_a_invert = FXFALSE;
        rdp.update |= UPDATE_COMBINE;
        //*/
    }
    rdp.update |= UPDATE_TEXTURE;
    update ();
    rdp.allow_combine = 1;
    
    float Z = 1.0f;
    if (fullscreen)
    {
        grFogMode (GR_FOG_DISABLE);
        if (rdp.zsrc == 1 && (rdp.othermode_l & 0x00000030))  
        {
            RDP("Sprite uses depth compare\n");
            Z = rdp.prim_depth;
            grDepthBufferFunction (GR_CMP_LEQUAL);
            grDepthMask (FXTRUE);
        }
        else
        {
            RDP("Sprite not uses depth compare\n");
            grDepthBufferFunction (GR_CMP_ALWAYS);
            grDepthMask (FXFALSE);
        }
        
        grClipWindow (0, 0, settings.res_x, settings.res_y);
        
        grCullMode (GR_CULL_DISABLE);
        
        if (rdp.cycle_mode == 2)
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
            grAlphaBlendFunction (GR_BLEND_ONE,
                GR_BLEND_ZERO,
                GR_BLEND_ZERO,
                GR_BLEND_ZERO);
            rdp.update |= UPDATE_ALPHA_COMPARE | UPDATE_COMBINE | UPDATE_ALPHA_COMPARE;
        }
    }
    return Z;
}

static void uc6_draw_polygons (VERTEX v[4])
{
  AllowShadeMods (v, 4);
  for (int s = 0; s < 4; s++)
    apply_shade_mods (&(v[s]));
  
  // Set vertex buffers
  if (rdp.cur_cache[0]->splits > 1)
  {
    VERTEX *vptr[3];
    int i;
    for (i = 0; i < 3; i++)
      vptr[i] = &v[i];
    draw_splitted_triangle(vptr);
    
    rdp.tri_n ++;
    for (i = 0; i < 3; i++)
      vptr[i] = &v[i+1];
    draw_splitted_triangle(vptr);
    rdp.tri_n ++;
  }
  else
  {
    rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
    rdp.vtxbuf2 = rdp.vtx2;
    rdp.vtx_buffer = 0;
    rdp.n_global = 3;
    memcpy (rdp.vtxbuf, v, sizeof(VERTEX)*3);
    do_triangle_stuff_2 ();
    rdp.tri_n ++;
    
    rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
    rdp.vtxbuf2 = rdp.vtx2;
    rdp.vtx_buffer = 0;
    rdp.n_global = 3;
    memcpy (rdp.vtxbuf, v+1, sizeof(VERTEX)*3);
    do_triangle_stuff_2 ();
    rdp.tri_n ++;
  }
  rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_VIEWPORT;
  
  if (fullscreen && settings.fog && (rdp.flags & FOG_ENABLED))
  {
    grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
  }
}

static void uc6_obj_rectangle ()
{
    //  RDP ("uc6:obj_rectangle\n");
    
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    float objX      = ((short*)gfx.RDRAM)[(addr+0)^1] / 4.0f;       // 0
    float scaleW    = ((WORD *)gfx.RDRAM)[(addr+1)^1] / 1024.0f;    // 1
    short imageW    = ((short*)gfx.RDRAM)[(addr+2)^1] >> 5;         // 2, 3 is padding
    float objY      = ((short*)gfx.RDRAM)[(addr+4)^1] / 4.0f;       // 4
    float scaleH    = ((WORD *)gfx.RDRAM)[(addr+5)^1] / 1024.0f;    // 5
    short imageH    = ((short*)gfx.RDRAM)[(addr+6)^1] >> 5;         // 6, 7 is padding
    
    WORD  imageStride   = ((WORD *)gfx.RDRAM)[(addr+8)^1];          // 8
    WORD  imageAdrs     = ((WORD *)gfx.RDRAM)[(addr+9)^1];          // 9
    BYTE  imageFmt      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+0)^3];    // 10
    BYTE  imageSiz      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+1)^3];    // |
    BYTE  imagePal      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+2)^3];    // 11
    BYTE  imageFlags    = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+3)^3];    // |
  
  if (imageW < 0) 
    imageW = (short)rdp.scissor_o.lr_x - (short)objX - imageW;
  if (imageH < 0) 
    imageH = (short)rdp.scissor_o.lr_y - (short)objY - imageH;
    
    FRDP ("uc6:obj_rectangle #%d, #%d\n"
        "objX: %f, scaleW: %f, imageW: %d\n"
        "objY: %f, scaleH: %f, imageH: %d\n"
        "size: %d, format: %d\n", rdp.tri_n, rdp.tri_n+1,
        objX, scaleW, imageW, objY, scaleH, imageH, imageSiz, imageFmt);
    if (imageAdrs > 4096)
    {
        FRDP("tmem: %08lx is out of bounds! return\n", imageAdrs);
        return;
    }
    if (!rdp.s2dex_tex_loaded)
    {
        RDP("Texture was not loaded! return\n");
        return;
    }
    
    // SetTile ()
    TILE *tile = &rdp.tiles[0];
    tile->format = imageFmt;    // RGBA
    tile->size = imageSiz;      // 16-bit
    tile->line = imageStride;
    tile->t_mem = imageAdrs;
    tile->palette = imagePal;
    tile->clamp_t = 1;
    tile->mirror_t = 0;
    tile->mask_t = 0;
    tile->shift_t = 0;
    tile->clamp_s = 1;
    tile->mirror_s = 0;
    tile->mask_s = 0;
    tile->shift_s = 0;
    
    // SetTileSize ()
    rdp.tiles[0].ul_s = 0;
    rdp.tiles[0].ul_t = 0;
    rdp.tiles[0].lr_s = (imageW>0)?imageW-1:0;
    rdp.tiles[0].lr_t = (imageH>0)?imageH-1:0;
    
    float Z = set_sprite_combine_mode ();

    float ul_x = objX;
    float lr_x = objX + imageW/scaleW;
    float ul_y = objY;
    float lr_y = objY + imageH/scaleH;
    float ul_u, lr_u, ul_v, lr_v;
    if (rdp.cur_cache[0]->splits > 1)
    {
        lr_u = (float)(imageW-1);
        lr_v = (float)(imageH-1);
    }
    else
    {
        lr_u = 255.0f*rdp.cur_cache[0]->scale_x;
        lr_v = 255.0f*rdp.cur_cache[0]->scale_y;
    }
    
    if (imageFlags&0x01) //flipS
    {
        ul_u = lr_u;
        lr_u = 0.5f;
    }
    else
        ul_u = 0.5f;
    if (imageFlags&0x10) //flipT
    {
        ul_v = lr_v;
        lr_v = 0.5f;
    }
    else
        ul_v = 0.5f;
    
    // Make the vertices
    VERTEX v[4] = {
        { ul_x, ul_y, Z, 1, ul_u, ul_v },
        { lr_x, ul_y, Z, 1, lr_u, ul_v },
        { ul_x, lr_y, Z, 1, ul_u, lr_v },
        { lr_x, lr_y, Z, 1, lr_u, lr_v }
    };
        
                int i;
        for (i=0; i<4; i++)
        {
            v[i].x *= rdp.scale_x;
            v[i].y *= rdp.scale_y;
        }
        
  uc6_draw_polygons (v);
}

void uc6_obj_sprite ()
{
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    float objX      = ((short*)gfx.RDRAM)[(addr+0)^1] / 4.0f;       // 0
    float scaleW    = ((WORD *)gfx.RDRAM)[(addr+1)^1] / 1024.0f;    // 1
    short imageW    = ((short*)gfx.RDRAM)[(addr+2)^1] >> 5;         // 2, 3 is padding
    float objY      = ((short*)gfx.RDRAM)[(addr+4)^1] / 4.0f;       // 4
    float scaleH    = ((WORD *)gfx.RDRAM)[(addr+5)^1] / 1024.0f;    // 5
    short imageH    = ((short*)gfx.RDRAM)[(addr+6)^1] >> 5;         // 6, 7 is padding
    
    WORD  imageStride   = ((WORD *)gfx.RDRAM)[(addr+8)^1];          // 8
    WORD  imageAdrs     = ((WORD *)gfx.RDRAM)[(addr+9)^1];          // 9
    BYTE  imageFmt      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+0)^3];    // 10
    BYTE  imageSiz      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+1)^3];    // |
    BYTE  imagePal      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+2)^3];    // 11
    BYTE  imageFlags    = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+3)^3];    // |
    
    FRDP ("uc6:obj_sprite #%d, #%d\n"
        "objX: %f, scaleW: %f, imageW: %d\n"
        "objY: %f, scaleH: %f, imageH: %d\n"
        "size: %d, format: %d\n", rdp.tri_n, rdp.tri_n+1,
        objX, scaleW, imageW, objY, scaleH, imageH, imageSiz, imageFmt);
    
    // SetTile ()
    TILE *tile = &rdp.tiles[0];
    tile->format = imageFmt;    // RGBA
    tile->size = imageSiz;      // 16-bit
    tile->line = imageStride;
    tile->t_mem = imageAdrs;
    tile->palette = imagePal;
    tile->clamp_t = 1;
    tile->mirror_t = 0;
    tile->mask_t = 0;
    tile->shift_t = 0;
    tile->clamp_s = 1;
    tile->mirror_s = 0;
    tile->mask_s = 0;
    tile->shift_s = 0;
    
    // SetTileSize ()
    rdp.tiles[0].ul_s = 0;
    rdp.tiles[0].ul_t = 0;
    rdp.tiles[0].lr_s = (imageW>0)?imageW-1:0;
    rdp.tiles[0].lr_t = (imageH>0)?imageH-1:0;
    
    float Z = set_sprite_combine_mode ();

    float ul_x = objX;
    float lr_x = objX + imageW/scaleW;
    float ul_y = objY;
    float lr_y = objY + imageH/scaleH;
    float ul_u, lr_u, ul_v, lr_v;
    if (rdp.cur_cache[0]->splits > 1)
    {
        lr_u = (float)(imageW-1);
        lr_v = (float)(imageH-1);
    }
    else
    {
        lr_u = 255.0f*rdp.cur_cache[0]->scale_x;
        lr_v = 255.0f*rdp.cur_cache[0]->scale_y;
    }
    
    if (imageFlags&0x01) //flipS
    {
        ul_u = lr_u;
        lr_u = 0.5f;
    }
    else
        ul_u = 0.5f;
    if (imageFlags&0x10) //flipT
    {
        ul_v = lr_v;
        lr_v = 0.5f;
    }
    else
        ul_v = 0.5f;
    
    // Make the vertices
    //  FRDP("scale_x: %f, scale_y: %f\n", rdp.cur_cache[0]->scale_x, rdp.cur_cache[0]->scale_y);
    
    VERTEX v[4] = {
        { ul_x, ul_y, Z, 1, ul_u, ul_v },
        { lr_x, ul_y, Z, 1, lr_u, ul_v },
        { ul_x, lr_y, Z, 1, ul_u, lr_v },
        { lr_x, lr_y, Z, 1, lr_u, lr_v }
    };
        
                int i;
        for (i=0; i<4; i++)
        {
            float x = v[i].x;
            float y = v[i].y;
            v[i].x = (x * mat_2d.A + y * mat_2d.B + mat_2d.X) * rdp.scale_x;
            v[i].y = (x * mat_2d.C + y * mat_2d.D + mat_2d.Y) * rdp.scale_y;
        }
        
  uc6_draw_polygons (v);
}

void uc6_obj_movemem ()
{
    RDP ("uc6:obj_movemem\n");
    
    int index = rdp.cmd0 & 0xFFFF;
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    if (index == 0) {   // movemem matrix
        mat_2d.A = ((long*)gfx.RDRAM)[(addr+0)>>1] / 65536.0f;
        mat_2d.B = ((long*)gfx.RDRAM)[(addr+2)>>1] / 65536.0f;
        mat_2d.C = ((long*)gfx.RDRAM)[(addr+4)>>1] / 65536.0f;
        mat_2d.D = ((long*)gfx.RDRAM)[(addr+6)>>1] / 65536.0f;
        mat_2d.X = ((short*)gfx.RDRAM)[(addr+8)^1] / 4.0f;
        mat_2d.Y = ((short*)gfx.RDRAM)[(addr+9)^1] / 4.0f;
        mat_2d.BaseScaleX = ((WORD*)gfx.RDRAM)[(addr+10)^1] / 1024.0f;
        mat_2d.BaseScaleY = ((WORD*)gfx.RDRAM)[(addr+11)^1] / 1024.0f;
        
        FRDP ("mat_2d\nA: %f, B: %f, c: %f, D: %f\nX: %f, Y: %f\nBaseScaleX: %f, BaseScaleY: %f\n",
            mat_2d.A, mat_2d.B, mat_2d.C, mat_2d.D, mat_2d.X, mat_2d.Y, mat_2d.BaseScaleX, mat_2d.BaseScaleY);
    }
    else if (index == 2) {  // movemem submatrix
        mat_2d.X = ((short*)gfx.RDRAM)[(addr+0)^1] / 4.0f;
        mat_2d.Y = ((short*)gfx.RDRAM)[(addr+1)^1] / 4.0f;
        mat_2d.BaseScaleX = ((WORD*)gfx.RDRAM)[(addr+2)^1] / 1024.0f;
        mat_2d.BaseScaleY = ((WORD*)gfx.RDRAM)[(addr+3)^1] / 1024.0f;
        
        FRDP ("submatrix\nX: %f, Y: %f\nBaseScaleX: %f, BaseScaleY: %f\n",
            mat_2d.X, mat_2d.Y, mat_2d.BaseScaleX, mat_2d.BaseScaleY);
    }
}

static void uc6_select_dl ()
{
    RDP ("uc6:select_dl\n");
    RDP_E ("uc6:select_dl\n");
}

static void uc6_obj_rendermode ()
{
    RDP ("uc6:obj_rendermode\n");
    RDP_E ("uc6:obj_rendermode\n");
}

void uc6_obj_rectangle_r ()
{
    //  RDP ("uc6:obj_rectangle_r\n");
    
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    
    float objX      = ((short*)gfx.RDRAM)[(addr+0)^1] / 4.0f;       // 0
    float scaleW    = ((WORD *)gfx.RDRAM)[(addr+1)^1] / 1024.0f;    // 1
    short imageW    = ((short*)gfx.RDRAM)[(addr+2)^1] >> 5;         // 2, 3 is padding
    float objY      = ((short*)gfx.RDRAM)[(addr+4)^1] / 4.0f;       // 4
    float scaleH    = ((WORD *)gfx.RDRAM)[(addr+5)^1] / 1024.0f;    // 5
    short imageH    = ((short*)gfx.RDRAM)[(addr+6)^1] >> 5;         // 6, 7 is padding
    
    WORD  imageStride   = ((WORD *)gfx.RDRAM)[(addr+8)^1];          // 8
    WORD  imageAdrs     = ((WORD *)gfx.RDRAM)[(addr+9)^1];          // 9
    BYTE  imageFmt      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+0)^3];    // 10
    BYTE  imageSiz      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+1)^3];    // |
    BYTE  imagePal      = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+2)^3];    // 11
    BYTE  imageFlags    = ((BYTE *)gfx.RDRAM)[(((addr+10)<<1)+3)^3];    // |
  
  if (imageW < 0) 
    imageW = (short)rdp.scissor_o.lr_x - (short)objX - imageW;
  if (imageH < 0) 
    imageH = (short)rdp.scissor_o.lr_y - (short)objY - imageH;
    
    FRDP ("uc6:obj_rectangle_r #%d, #%d\n"
        "objX: %f, scaleW: %f, imageW: %d\n"
        "objY: %f, scaleH: %f, imageH: %d\n"
        "size: %d, format: %d\n", rdp.tri_n, rdp.tri_n+1,
        objX, scaleW, imageW, objY, scaleH, imageH, imageSiz, imageFmt);
    
    if (imageFmt == 1) //YUV
    {
        float ul_x = objX/mat_2d.BaseScaleX + mat_2d.X;
        float lr_x = (objX + imageW/scaleW)/mat_2d.BaseScaleX + mat_2d.X;
        float ul_y = objY/mat_2d.BaseScaleY + mat_2d.Y;
        float lr_y = (objY + imageH/scaleH)/mat_2d.BaseScaleY + mat_2d.Y;
        if (ul_x < rdp.yuv_ul_x) rdp.yuv_ul_x = ul_x;
        if (lr_x > rdp.yuv_lr_x) rdp.yuv_lr_x = lr_x;
        if (ul_y < rdp.yuv_ul_y) rdp.yuv_ul_y = ul_y;
        if (lr_y > rdp.yuv_lr_y) rdp.yuv_lr_y = lr_y;
        rdp.tri_n += 2;
        return;
    }
    // SetTile ()
    TILE *tile = &rdp.tiles[0];
    tile->format = imageFmt;    // RGBA
    tile->size = imageSiz;      // 16-bit
    tile->line = imageStride;
    tile->t_mem = imageAdrs;
    tile->palette = imagePal;
    tile->clamp_t = 1;
    tile->mirror_t = 0;
    tile->mask_t = 0;
    tile->shift_t = 0;
    tile->clamp_s = 1;
    tile->mirror_s = 0;
    tile->mask_s = 0;
    tile->shift_s = 0;
    
    // SetTileSize ()
    rdp.tiles[0].ul_s = 0;
    rdp.tiles[0].ul_t = 0;
    rdp.tiles[0].lr_s = (imageW>0)?imageW-1:0;
    rdp.tiles[0].lr_t = (imageH>0)?imageH-1:0;
    
    float Z = set_sprite_combine_mode ();
    
    float ul_x = objX/mat_2d.BaseScaleX;
    float lr_x = (objX + imageW/scaleW)/mat_2d.BaseScaleX;
    float ul_y = objY/mat_2d.BaseScaleY;
    float lr_y = (objY + imageH/scaleH)/mat_2d.BaseScaleY;
    float ul_u, lr_u, ul_v, lr_v;
    if (rdp.cur_cache[0]->splits > 1)
    {
        lr_u = (float)(imageW-1);
        lr_v = (float)(imageH-1);
    }
    else
    {
        lr_u = 255.0f*rdp.cur_cache[0]->scale_x;
        lr_v = 255.0f*rdp.cur_cache[0]->scale_y;
    }
    
    if (imageFlags&0x01) //flipS
    {
        ul_u = lr_u;
        lr_u = 0.5f;
    }
    else
        ul_u = 0.5f;
    if (imageFlags&0x10) //flipT
    {
        ul_v = lr_v;
        lr_v = 0.5f;
    }
    else
        ul_v = 0.5f;
    
    // Make the vertices
    VERTEX v[4] = {
        { ul_x, ul_y, Z, 1, ul_u, ul_v },
        { lr_x, ul_y, Z, 1, lr_u, ul_v },
        { ul_x, lr_y, Z, 1, ul_u, lr_v },
        { lr_x, lr_y, Z, 1, lr_u, lr_v }
    };
        
                int i;
        for (i=0; i<4; i++)
        {
            float x = v[i].x;
            float y = v[i].y;
            v[i].x = (x + mat_2d.X) * rdp.scale_x;
            v[i].y = (y + mat_2d.Y) * rdp.scale_y;
        }
        
  uc6_draw_polygons (v);
}

void uc6_obj_loadtxtr ()
{
    RDP ("uc6:obj_loadtxtr ");
    rdp.s2dex_tex_loaded = TRUE;
    rdp.update |= UPDATE_TEXTURE;
    
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    DWORD type  = ((DWORD*)gfx.RDRAM)[(addr + 0) >> 1];         // 0, 1
    
    if (type == 0x00000030) {   // TLUT
        DWORD image     = segoffset(((DWORD*)gfx.RDRAM)[(addr + 2) >> 1]);  // 2, 3
        WORD  phead     = ((WORD *)gfx.RDRAM)[(addr + 4) ^ 1] - 256;    // 4
        WORD  pnum      = ((WORD *)gfx.RDRAM)[(addr + 5) ^ 1] + 1;      // 5
        
        FRDP ("palette addr: %08lx, start: %d, num: %d\n", image, phead, pnum);
        load_palette (image, phead, pnum);
    }
    else if (type == 0x00001033) {  // TxtrBlock
        DWORD image     = segoffset(((DWORD*)gfx.RDRAM)[(addr + 2) >> 1]);  // 2, 3
        WORD  tmem      = ((WORD *)gfx.RDRAM)[(addr + 4) ^ 1];  // 4
        WORD  tsize     = ((WORD *)gfx.RDRAM)[(addr + 5) ^ 1];  // 5
        WORD  tline     = ((WORD *)gfx.RDRAM)[(addr + 6) ^ 1];  // 6
        
        FRDP ("addr: %08lx, tmem: %08lx, size: %d\n", image, tmem, tsize);
        rdp.timg.addr = image;
        
        rdp.tiles[7].t_mem = tmem;
        rdp.tiles[7].size = 1;
        rdp.cmd0 = 0;
        rdp.cmd1 = 0x07000000 | (tsize << 14) | tline;
        rdp_loadblock ();
    }
    else if (type == 0x00fc1034)
    {
        DWORD image     = segoffset(((DWORD*)gfx.RDRAM)[(addr + 2) >> 1]);  // 2, 3
        WORD  tmem      = ((WORD *)gfx.RDRAM)[(addr + 4) ^ 1];  // 4
        WORD  twidth    = ((WORD *)gfx.RDRAM)[(addr + 5) ^ 1];  // 5
        WORD  theight   = ((WORD *)gfx.RDRAM)[(addr + 6) ^ 1];  // 6
        
        FRDP ("tile addr: %08lx, tmem: %08lx, twidth: %d, theight: %d\n", image, tmem, twidth, theight);
        
        int line = (twidth + 1) >> 2;
        
        rdp.timg.addr = image;
        rdp.timg.width = line << 3;
        
        rdp.tiles[7].t_mem = tmem;
        rdp.tiles[7].line = line;
        rdp.tiles[7].size = 1;
        
        rdp.cmd0 = 0;
        rdp.cmd1 = 0x07000000 | (twidth << 14) | (theight << 2);
        
        rdp_loadtile ();
    }
    else
    {
        FRDP ("UNKNOWN (0x%08lx)\n", type);
        FRDP_E ("uc6:obj_loadtxtr UNKNOWN (0x%08lx)\n", type);
    }
}

void uc6_obj_ldtx_sprite ()
{
    RDP ("uc6:obj_ldtx_sprite\n");
    
    DWORD addr = rdp.cmd1;
    uc6_obj_loadtxtr ();
    rdp.cmd1 = addr + 24;
    uc6_obj_sprite ();
}

void uc6_obj_ldtx_rect ()
{
    RDP ("uc6:obj_ldtx_rect\n");
    
    DWORD addr = rdp.cmd1;
    uc6_obj_loadtxtr ();
    rdp.cmd1 = addr + 24;
    uc6_obj_rectangle ();
}

static void uc6_ldtx_rect_r ()
{
    RDP ("uc6:ldtx_rect_r\n");
    
    DWORD addr = rdp.cmd1;
    uc6_obj_loadtxtr ();
    rdp.cmd1 = addr + 24;
    uc6_obj_rectangle_r ();
}
#ifdef _WIN32
static void uc6_rdphalf_0 ()
{
    RDP ("uc6:rdphalf_0\n");
    RDP_E ("uc6:rdphalf_0\n");
}
#endif // _WIN32
static void uc6_rdphalf_1 ()
{
    RDP ("uc6:rdphalf_1\n");
    RDP_E ("uc6:rdphalf_1\n");
}

static void uc6_loaducode ()
{
    RDP ("uc6:load_ucode\n");
    RDP_E ("uc6:load_ucode\n");
    
    // copy the microcode data
    DWORD addr = segoffset(rdp.cmd1);
    DWORD size = (rdp.cmd0 & 0xFFFF) + 1;
    memcpy (microcode, gfx.RDRAM+addr, size);
    
    microcheck ();
}

void drawViRegBG()
{
    DWORD VIwidth = *gfx.VI_WIDTH_REG;
    
    DRAWIMAGE d;
    
    d.imageX    = 0;
  d.imageW  = (WORD)VIwidth;
    if (d.imageW%4) d.imageW -= 2;
    d.frameX    = 0;
  d.frameW  = (WORD)rdp.vi_width;
    
    d.imageY    = 0;
    d.imageH    = (WORD)rdp.vi_height;
    d.frameY    = 0; 
  d.frameH  = (WORD)(rdp.vi_height);
  RDP ("drawViRegBG\n");
  FRDP ("frameX: %f, frameW: %d, frameY: %f, frameH: %d\n", d.frameX, d.frameW, d.frameY, d.frameH);
  FRDP ("imageX: %d, imageW: %d, imageY: %d, imageH: %d\n", d.imageX, d.imageW, d.imageY, d.imageH);
  if (!settings.RE2)
  {
    d.imagePtr  = (*gfx.VI_ORIGIN_REG) - (VIwidth<<1);
    rdp.last_bg = d.imagePtr;
    rdp.cycle_mode = 2;
    d.imageSiz  = 2;
    d.imageFmt  = 0;
    d.imagePal  = 0;
    
    d.scaleX    = 1.0f; 
    d.scaleY    = 1.0f; 
    d.flipX     = 0;
    d.flipY     = 0;
    
    //  FRDP ("drawViRegBG  imageW :%d, imageH: %d\n", d.imageW, d.imageH);
    if (!d.imageW || !d.imageH)
    {
        RDP("skipped\n");
        return;
    }
    DrawImage (&d);
    if (settings.lego)
    {
      rdp.updatescreen = 1;
      newSwapBuffers ();
      DrawImage (&d);
    }
    return;
  }
  //Draw RE2 video
  d.imagePtr    = (*gfx.VI_ORIGIN_REG) - (VIwidth<<2);
  rdp.last_bg = d.imagePtr;
  if (d.imageH > 256) d.imageH = 256;
  update_screen_count = 0;
  /*
  if (settings.RE2_native_video) //draw video in native resolution and without conversion.
  {
  DWORD * image = new DWORD[d.imageW*d.imageH];
  DWORD * src = (DWORD*)(gfx.RDRAM+d.imagePtr);
  DWORD * dst = image;
  DWORD col;
  for (int h = 0; h < d.imageH; h++)
  {
  for (int w = 0; w < d.imageW; w++)
  {
  col = *(src++);
  *(dst++) = col >> 8;
  }
  }
    
    int x = (settings.scr_res_x - d.imageW) / 2;
    int y = (settings.scr_res_y - d.imageH) / 2;
    
      grLfbWriteRegion(GR_BUFFER_BACKBUFFER,
      x,
      y,
      GR_LFB_SRC_FMT_888,
      d.imageW,
      d.imageH,
      FXFALSE,
      d.imageW<<2,
      image);
      delete[] image;
      }
      else
      {
  */
  DWORD * src = (DWORD*)(gfx.RDRAM+d.imagePtr);
  GrTexInfo t_info;
  t_info.smallLodLog2 = GR_LOD_LOG2_256;
  t_info.largeLodLog2 = GR_LOD_LOG2_256;
  t_info.aspectRatioLog2 = GR_ASPECT_LOG2_1x1;
  if (sup_32bit_tex) //use 32bit textures
  {
    DWORD image[256*256];
    DWORD * dst = image;
    DWORD col;
    for (int h = 0; h < d.imageH; h++)
    {
      for (int w = 0; w < 256; w++)
      {
        col = *(src++);
        *(dst++) = (col >> 8) | 0xFF000000;
      }
      src += (d.imageW - 256);
    }
    t_info.format = GR_TEXFMT_ARGB_8888;
    t_info.data = image;
    grTexDownloadMipMap (GR_TMU0,
      grTexMinAddress(GR_TMU0)+offset_textures,
      GR_MIPMAPLEVELMASK_BOTH,
      &t_info);
  }
  else
  {
    WORD image[256*256];
    WORD * dst = image;
    DWORD col;
    BYTE r, g, b;
    for (int h = 0; h < d.imageH; h++)
    {
      for (int w = 0; w < 256; w++)
      {
        col = *(src++);
        r = (BYTE)((col >> 24)&0xFF);
        r = (BYTE)((float)r / 255.0f * 31.0f);
        g = (BYTE)((col >> 16)&0xFF);
        g = (BYTE)((float)g / 255.0f * 63.0f);
        b = (BYTE)((col >>  8)&0xFF);
        b = (BYTE)((float)b / 255.0f * 31.0f);
        *(dst++) = (r << 11) | (g << 5) | b;
      }
      src += (d.imageW - 256);
    }
    t_info.format = GR_TEXFMT_RGB_565;
    t_info.data = image;
    grTexDownloadMipMap (GR_TMU0,
      grTexMinAddress(GR_TMU0)+offset_textures,
      GR_MIPMAPLEVELMASK_BOTH,
      &t_info);
  }
  grTexSource (GR_TMU0,
        grTexMinAddress(GR_TMU0)+offset_textures,
      GR_MIPMAPLEVELMASK_BOTH,
      &t_info);
  grTexCombine( GR_TMU1, 
    GR_COMBINE_FUNCTION_NONE, 
    GR_COMBINE_FACTOR_NONE, 
    GR_COMBINE_FUNCTION_NONE, 
    GR_COMBINE_FACTOR_NONE, 
    FXFALSE, 
    FXFALSE ); 
  grTexCombine( GR_TMU0, 
    GR_COMBINE_FUNCTION_LOCAL, 
    GR_COMBINE_FACTOR_NONE, 
    GR_COMBINE_FUNCTION_LOCAL, 
    GR_COMBINE_FACTOR_NONE, 
    FXFALSE, 
    FXFALSE ); 
  grTexClampMode (GR_TMU0,
    GR_TEXTURECLAMP_WRAP,
    GR_TEXTURECLAMP_CLAMP);
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
  grAlphaBlendFunction (GR_BLEND_ONE,   
    GR_BLEND_ZERO,
    GR_BLEND_ONE,
    GR_BLEND_ZERO);
  grConstantColorValue (0xFFFFFFFF);
  grDepthBufferFunction (GR_CMP_ALWAYS);
  grDepthMask (FXFALSE);
  
  float scale_y = (float)d.imageW/rdp.vi_height;
  float height = settings.scr_res_x/scale_y;
  float ul_y = (settings.scr_res_y - height)/2.0f;
  float lr_y = settings.scr_res_y - ul_y - 1.0f;
  float lr_x = settings.scr_res_x-1.0f;
  float lr_u = d.imageW - 1.0f;
  float lr_v = d.imageH - 1.0f;
  VERTEX v[4] = {
    { 0, ul_y, 1.0f, 1.0f, 0, 0, 0, 0 },
    { lr_x, ul_y, 1.0f, 1.0f, lr_u, 0, lr_u, 0 },
    { 0, lr_y, 1.0f, 1.0f, 0, lr_v, 0, lr_v },
    { lr_x, lr_y, 1.0f, 1.0f, lr_u, lr_v, lr_u, lr_v } 
  };
  for (int i=0; i<4; i++)
  {
    v[i].uc(0) = v[i].u0;
    v[i].vc(0) = v[i].v0;
  }
  grDrawTriangle (&v[0], &v[2], &v[1]);
  grDrawTriangle (&v[2], &v[3], &v[1]);
}

void uc6_sprite2d ()
{
    DWORD a = rdp.pc[rdp.pc_i] & BMASK;
    DWORD cmd0 = ((DWORD*)gfx.RDRAM)[a>>2]; //check next command
    if ( (cmd0>>24) != 0xBE )
        return;
    
    FRDP ("uc6:uc6_sprite2d #%d, #%d\n", rdp.tri_n, rdp.tri_n+1);
    DWORD addr = segoffset(rdp.cmd1) >> 1;
    DRAWIMAGE d;
    
    d.imagePtr  = segoffset(((DWORD*)gfx.RDRAM)[(addr+0)>>1]);  // 0,1
    DWORD tlut      = ((DWORD*)gfx.RDRAM)[(addr + 2) >> 1]; // 2, 3
    if (tlut) 
    {
        rdp.tlut_mode = 2;
        load_palette (segoffset(tlut), 0, 256);
    }
    WORD stride = (((WORD *)gfx.RDRAM)[(addr+4)^1]);    // 4
    d.imageW    = (((WORD *)gfx.RDRAM)[(addr+5)^1]);    // 5
    d.imageH    = (((WORD *)gfx.RDRAM)[(addr+6)^1]);    // 6
    d.imageFmt  = ((BYTE *)gfx.RDRAM)[(((addr+7)<<1)+0)^3]; // 7
    d.imageSiz  = ((BYTE *)gfx.RDRAM)[(((addr+7)<<1)+1)^3]; // |
    d.imagePal  = 0;
    d.imageX    = (((WORD *)gfx.RDRAM)[(addr+8)^1]);    // 8
    d.imageY    = (((WORD *)gfx.RDRAM)[(addr+9)^1]);    // 9
    
    if (d.imageW == 0)
        return;//     d.imageW = stride;

    cmd0 = ((DWORD*)gfx.RDRAM)[a>>2]; //check next command
    while (1)
    {
        if ( (cmd0>>24) == 0xBE )
        {
            DWORD cmd1 = ((DWORD*)gfx.RDRAM)[(a>>2)+1]; 
            rdp.pc[rdp.pc_i] = (a+8) & BMASK;
            
            d.scaleX    = ((cmd1>>16)&0xFFFF)/1024.0f;
            d.scaleY    = (cmd1&0xFFFF)/1024.0f;
            if( (cmd1&0xFFFF) < 0x100 )
                d.scaleY = d.scaleX;
            d.flipX = (BYTE)((cmd0>>8)&0xFF);
            d.flipY = (BYTE)(cmd0&0xFF);
            
            
            a = rdp.pc[rdp.pc_i] & BMASK;
            rdp.pc[rdp.pc_i] = (a+8) & BMASK;
            cmd0 = ((DWORD*)gfx.RDRAM)[a>>2]; //check next command
        }
        if ( (cmd0>>24) == 0xBD )
        {
            DWORD cmd1 = ((DWORD*)gfx.RDRAM)[(a>>2)+1]; 
            
            d.frameX    = ((short)((cmd1>>16)&0xFFFF)) / 4.0f;
            d.frameY    = ((short)(cmd1&0xFFFF)) / 4.0f;    
            d.frameW    = (WORD) (d.imageW / d.scaleX);
            d.frameH    = (WORD) (d.imageH / d.scaleY);
      if (settings.nitro)
      {
        int scaleY = (int)d.scaleY;
        d.imageH    /= scaleY;
        d.imageY    /= scaleY;
        stride      *= scaleY;
        d.scaleY    = 1.0f;
      }
            FRDP ("imagePtr: %08lx\n", d.imagePtr);
            FRDP ("frameX: %f, frameW: %d, frameY: %f, frameH: %d\n", d.frameX, d.frameW, d.frameY, d.frameH);
            FRDP ("imageX: %d, imageW: %d, imageY: %d, imageH: %d\n", d.imageX, d.imageW, d.imageY, d.imageH);
            FRDP ("imageFmt: %d, imageSiz: %d, imagePal: %d, imageStride: %d\n", d.imageFmt, d.imageSiz, d.imagePal, stride);
            FRDP ("scaleX: %f, scaleY: %f\n", d.scaleX, d.scaleY);
        }
        else
        {
          return;
        }

        DWORD texsize = d.imageW * d.imageH;
        if (d.imageSiz == 0)
            texsize >>= 1;
        else
            texsize <<= (d.imageSiz-1);
        
        if (texsize > 4096)
        {
            d.imageW    = stride;
            d.imageH    += d.imageY;
            DrawImage (&d);
        }
        else
        {
            WORD line = d.imageW;
            if (line & 7) line += 8;    // round up
            line >>= 3;
            if (d.imageSiz == 0)
            {
              if (line%2)
                line++;
              line >>= 1;
            }
            else 
            {
                line <<= (d.imageSiz-1);
            }
            if (line == 0)
                line = 1;
            
            rdp.timg.addr = d.imagePtr;
            rdp.timg.width = stride;
            rdp.tiles[7].t_mem = 0;
            rdp.tiles[7].line = line;//(d.imageW>>3);
            rdp.tiles[7].size = d.imageSiz;
            rdp.cmd0 = (d.imageX << 14) | (d.imageY << 2);
            rdp.cmd1 = 0x07000000 | ((d.imageX+d.imageW-1) << 14) | ((d.imageY+d.imageH-1) << 2);
            rdp_loadtile ();
            
            // SetTile ()
            TILE *tile = &rdp.tiles[0];
            tile->format = d.imageFmt;  
            tile->size = d.imageSiz;        
            tile->line = line;//(d.imageW>>3);
            tile->t_mem = 0;
            tile->palette = 0;
            tile->clamp_t = 1;
            tile->mirror_t = 0;
            tile->mask_t = 0;
            tile->shift_t = 0;
            tile->clamp_s = 1;
            tile->mirror_s = 0;
            tile->mask_s = 0;
            tile->shift_s = 0;
            
            // SetTileSize ()
            rdp.tiles[0].ul_s = d.imageX;
            rdp.tiles[0].ul_t = d.imageY;
            rdp.tiles[0].lr_s = d.imageX+d.imageW-1;
            rdp.tiles[0].lr_t = d.imageY+d.imageH-1;
            
            float Z = set_sprite_combine_mode ();
            
            float ul_x, ul_y, lr_x, lr_y;
            if (d.flipX)
            {
                ul_x = d.frameX + d.frameW;
                lr_x = d.frameX;
            }
            else
            {
                ul_x = d.frameX;
                lr_x = d.frameX + d.frameW;
            }
            if (d.flipY)
            {
                ul_y = d.frameY + d.frameH;
                lr_y = d.frameY;
            }
            else
            {
                ul_y = d.frameY;
                lr_y = d.frameY + d.frameH;
            }
            
            float lr_u, lr_v;
            if (rdp.cur_cache[0]->splits > 1)
            {
                lr_u = (float)(d.imageW-1);
                lr_v = (float)(d.imageH-1);
            }
            else
            {
                lr_u = 255.0f*rdp.cur_cache[0]->scale_x;
                lr_v = 255.0f*rdp.cur_cache[0]->scale_y;
            }
            
            // Make the vertices
            VERTEX v[4] = {
                { ul_x, ul_y, Z, 1, 0.5f, 0.5f },
                { lr_x, ul_y, Z, 1, lr_u, 0.5f },
                { ul_x, lr_y, Z, 1, 0.5f, lr_v },
                { lr_x, lr_y, Z, 1, lr_u, lr_v } };
                
                        int i;
                for (i=0; i<4; i++)
                {
                    v[i].x *= rdp.scale_x;
                    v[i].y *= rdp.scale_y;
                }
                
                //  ConvertCoordsConvert (v, 4);
                AllowShadeMods (v, 4);
                for (int s = 0; s < 4; s++)
                    apply_shade_mods (&(v[s]));
                
                // Set vertex buffers
                if (rdp.cur_cache[0]->splits > 1)
                {
                    VERTEX *vptr[3];
                    for (i = 0; i < 3; i++)
                        vptr[i] = &v[i];
                    draw_splitted_triangle(vptr);
                    
                    rdp.tri_n ++;
                    for (i = 0; i < 3; i++)
                        vptr[i] = &v[i+1];
                    draw_splitted_triangle(vptr);
                    rdp.tri_n ++;
                }
                else
                {
                    rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
                    rdp.vtxbuf2 = rdp.vtx2;
                    rdp.vtx_buffer = 0;
                    rdp.n_global = 3;
                    memcpy (rdp.vtxbuf, v, sizeof(VERTEX)*3);
                    do_triangle_stuff_2 ();
                    rdp.tri_n ++;
                    
                    rdp.vtxbuf = rdp.vtx1;  // copy from v to rdp.vtx1
                    rdp.vtxbuf2 = rdp.vtx2;
                    rdp.vtx_buffer = 0;
                    rdp.n_global = 3;
                    memcpy (rdp.vtxbuf, v+1, sizeof(VERTEX)*3);
                    do_triangle_stuff_2 ();
                    rdp.tri_n ++;
                }
                rdp.update |= UPDATE_ZBUF_ENABLED | UPDATE_VIEWPORT;
                
          if (fullscreen && settings.fog && (rdp.flags & FOG_ENABLED))
          {
            grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
          }
                
        }
        a = rdp.pc[rdp.pc_i] & BMASK;
        cmd0 = ((DWORD*)gfx.RDRAM)[a>>2]; //check next command
        if (( (cmd0>>24) == 0xBD ) || ( (cmd0>>24) == 0xBE ))
            rdp.pc[rdp.pc_i] = (a+8) & BMASK;
        else
            return;
    }
}


