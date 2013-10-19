/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus - glide64/wrapper/textures.cpp                            *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2005-2006 Hacktarux                                     *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include <stdlib.h>
#include <stdio.h>

#include "glide.h"
#include "main.h"

extern BOOL isExtensionSupported(const char *extension); // defined in main.cpp

/* Napalm extensions to GrTextureFormat_t */
#define GR_TEXFMT_ARGB_CMP_FXT1           0x11
#define GR_TEXFMT_ARGB_8888               0x12
#define GR_TEXFMT_YUYV_422                0x13
#define GR_TEXFMT_UYVY_422                0x14
#define GR_TEXFMT_AYUV_444                0x15
#define GR_TEXFMT_ARGB_CMP_DXT1           0x16
#define GR_TEXFMT_ARGB_CMP_DXT2           0x17
#define GR_TEXFMT_ARGB_CMP_DXT3           0x18
#define GR_TEXFMT_ARGB_CMP_DXT4           0x19
#define GR_TEXFMT_ARGB_CMP_DXT5           0x1A
#define GR_TEXTFMT_RGB_888                0xFF

#define TMU_SIZE 8*2048*2048

int tex0_width, tex0_height, tex1_width, tex1_height;
float lambda;

static int min_filter0, mag_filter0, wrap_s0, wrap_t0;
static int min_filter1, mag_filter1, wrap_s1, wrap_t1;

unsigned char *filter(unsigned char *source, int width, int height, int *width2, int *height2);

typedef struct _texlist
{
    unsigned int id;
    struct _texlist *next;
} texlist;

static int nbTex = 0;
static texlist *list = NULL;

void remove_tex(unsigned int idmin, unsigned int idmax)
{
    GLuint *t;
    int n = 0;
    texlist *aux = list;
  int sz = nbTex;
    if (aux == NULL) return;
    t = (GLuint*)malloc(sz * sizeof(int));
    while (aux && aux->id >= idmin && aux->id < idmax)
    {
    if (n >= sz)
      t = (GLuint*)realloc(t, ++sz*sizeof(int));
        t[n++] = aux->id;
        aux = aux->next;
        free(list);
        list = aux;
        nbTex--;
    }
    while (aux != NULL && aux->next != NULL)
    {
        if (aux->next->id >= idmin && aux->next->id < idmax)
        {
            texlist *aux2 = aux->next->next;
      if (n >= sz)
        t = (GLuint*)realloc(t, ++sz*sizeof(int));
            t[n++] = aux->next->id;
            free(aux->next);
            aux->next = aux2;
            nbTex--;
        }
        aux = aux->next;
    }
  glDeleteTextures(n, t);
    free(t);
    //printf("RMVTEX nbtex is now %d (%06x - %06x)\n", nbTex, idmin, idmax);
}

// void remove_all_tex()
// {
//  texlist *aux = list;
//   int sz = nbTex;
//  if (aux == NULL) return;
//   FILE * fp = fopen("toto.txt", "w");
//  while (aux)
//  {
//     fprintf(fp, "%x\n", aux->id);
//     fflush(fp);
//      glDeleteTextures(1, &aux->id);
//     fprintf(fp, "%x %x\n", aux, aux->next);
//     fflush(fp);
//      aux = aux->next;
//      free(list);
//     fprintf(fp, "plop\n");
//     fflush(fp);
//      list = aux;
//      nbTex--;
//  }
//   fclose(fp);
// }

void add_tex(unsigned int id)
{
    texlist *aux = list;
    texlist *aux2;
    //printf("ADDTEX nbtex is now %d (%06x)\n", nbTex, id);
    if (list == NULL || id < list->id)
    {
    nbTex++;
        list = (texlist*)malloc(sizeof(texlist));
        list->next = aux;
        list->id = id;
        return;
    }
    while (aux->next != NULL && aux->next->id < id) aux = aux->next;
  // ZIGGY added this test so that add_tex now accept re-adding an existing texture
  if (aux->next != NULL && aux->next->id == id) return;
    nbTex++;
    aux2 = aux->next;
    aux->next = (texlist*)malloc(sizeof(texlist));
    aux->next->id = id;
    aux->next->next = aux2;
}

void init_textures()
{
    tex0_width = tex0_height = tex1_width = tex1_height = 2;
  // ZIGGY because free_textures isn't called (Pj64 doesn't like it), it's better
  // to leave these so that they'll be reused (otherwise we have a memory leak)
//  list = NULL;
//  nbTex = 0;
}

void free_textures()
{
    remove_tex(0x00000000, 0xFFFFFFFF);
}

FX_ENTRY FxU32 FX_CALL 
grTexMinAddress( GrChipID_t tmu )
{
    WriteLog(M64MSG_VERBOSE, "grTexMinAddress(%d)\r\n", tmu);
    return tmu*TMU_SIZE;
}

FX_ENTRY FxU32 FX_CALL 
grTexMaxAddress( GrChipID_t tmu )
{
    WriteLog(M64MSG_VERBOSE, "grTexMaxAddress(%d)\r\n", tmu);
    return tmu*TMU_SIZE + TMU_SIZE - 1;
}

FX_ENTRY FxU32 FX_CALL 
grTexTextureMemRequired( FxU32     evenOdd,
                                 GrTexInfo *info   )
{
    int width, height;
    WriteLog(M64MSG_VERBOSE, "grTextureMemRequired(%d)\r\n", evenOdd);
    if (info->largeLodLog2 != info->smallLodLog2) display_warning("grTexTextureMemRequired : loading more than one LOD");

    if (info->aspectRatioLog2 < 0)
    {
        height = 1 << info->largeLodLog2;
        width = height >> -info->aspectRatioLog2;
    }
    else
    {
        width = 1 << info->largeLodLog2;
        height = width >> info->aspectRatioLog2;
    }

    switch(info->format)
    {
    case GR_TEXFMT_ALPHA_8:
    case GR_TEXFMT_ALPHA_INTENSITY_44:
        return width*height;
        break;
    case GR_TEXFMT_ARGB_1555:
    case GR_TEXFMT_ARGB_4444:
    case GR_TEXFMT_ALPHA_INTENSITY_88:
    case GR_TEXFMT_RGB_565:
        return width*height*2;
        break;
    case GR_TEXFMT_ARGB_8888:
        return width*height*4;
        break;
    default:
        display_warning("grTexTextureMemRequired : unknown texture format: %x", info->format);
    }
    return 0;
}

FX_ENTRY FxU32 FX_CALL 
grTexCalcMemRequired(
                     GrLOD_t lodmin, GrLOD_t lodmax,
                     GrAspectRatio_t aspect, GrTextureFormat_t fmt)
{
    int width, height;
    WriteLog(M64MSG_VERBOSE, "grTexCalcMemRequired(%d, %d, %d, %d)\r\n", lodmin, lodmax, aspect, fmt);
    if (lodmax != lodmin) display_warning("grTexCalcMemRequired : loading more than one LOD");

    if (aspect < 0)
    {
        height = 1 << lodmax;
        width = height >> -aspect;
    }
    else
    {
        width = 1 << lodmax;
        height = width >> aspect;
    }

    switch(fmt)
    {
    case GR_TEXFMT_ALPHA_8:
    case GR_TEXFMT_ALPHA_INTENSITY_44:
        return width*height;
        break;
    case GR_TEXFMT_ARGB_1555:
    case GR_TEXFMT_ARGB_4444:
    case GR_TEXFMT_ALPHA_INTENSITY_88:
    case GR_TEXFMT_RGB_565:
        return width*height*2;
        break;
    case GR_TEXFMT_ARGB_8888:
        return width*height*4;
        break;
    default:
        display_warning("grTexTextureMemRequired : unknown texture format: %x", fmt);
    }
    return 0;
}

int grTexFormatSize(int fmt)
{
  int factor = -1;
  switch(fmt) {
    case GR_TEXFMT_ALPHA_8:
        factor = 1;
        break;
    case GR_TEXFMT_ALPHA_INTENSITY_44:
        factor = 1;
        break;
    case GR_TEXFMT_RGB_565:
        factor = 2;
        break;
    case GR_TEXFMT_ARGB_1555:
        factor = 2;
        break;
    case GR_TEXFMT_ALPHA_INTENSITY_88:
        factor = 2;
        break;
    case GR_TEXFMT_ARGB_4444:
        factor = 2;
        break;
    case GR_TEXFMT_ARGB_8888:
        factor = 4;
        break;
    default:
        display_warning("grTexFormatSize : unknown texture format: %x", fmt);
  }
  return factor;
}

int packed_pixels_support = -1;

int grTexFormat2GLPackedFmt(int fmt, int * gltexfmt, int * glpixfmt, int * glpackfmt)
{
  int factor = -1;
  switch(fmt) {
    case GR_TEXFMT_ALPHA_8:
        factor = 1;
        *gltexfmt = GL_INTENSITY;
        *glpixfmt = GL_LUMINANCE;
        *glpackfmt = GL_UNSIGNED_BYTE;
        break;
    case GR_TEXFMT_ALPHA_INTENSITY_44:
        return -1;
//      factor = 1;
//      gltexfmt = GL_LUMINANCE4_ALPHA4;
//      glpixfmt = GL_LUMINANCE_ALPHA;
//      glpackfmt = GL_UNSIGNED_BYTE;
        break;
    case GR_TEXFMT_RGB_565:
    // trick, this format is only used actually for depth texture
//      factor = 2;
//      *gltexfmt = GL_DEPTH_COMPONENT;
//      *glpixfmt = GL_DEPTH_COMPONENT;
//      *glpackfmt = GL_UNSIGNED_SHORT;
//     break;
//     return -1;
        factor = 2;
        *gltexfmt = GL_RGB;
        *glpixfmt = GL_RGB;
        *glpackfmt = GL_UNSIGNED_SHORT_5_6_5;
        break;
    case GR_TEXFMT_ARGB_1555:
        factor = 2;
        *gltexfmt = GL_RGBA;
        *glpixfmt = GL_BGRA;
        *glpackfmt = GL_UNSIGNED_SHORT_1_5_5_5_REV;
        break;
    case GR_TEXFMT_ALPHA_INTENSITY_88:
        factor = 2;
        *gltexfmt = GL_LUMINANCE_ALPHA;
        *glpixfmt = GL_LUMINANCE_ALPHA;
        *glpackfmt = GL_UNSIGNED_BYTE;
        break;
    case GR_TEXFMT_ARGB_4444:
        factor = 2;
        *gltexfmt = GL_RGBA;
        *glpixfmt = GL_BGRA;
        *glpackfmt = GL_UNSIGNED_SHORT_4_4_4_4_REV;
        break;
    case GR_TEXFMT_ARGB_8888:
        factor = 4;
        *gltexfmt = GL_RGBA;
        *glpixfmt = GL_BGRA;
        *glpackfmt = GL_UNSIGNED_INT_8_8_8_8_REV;
        break;
    default:
        display_warning("grTexFormat2GLPackedFmt : unknown texture format: %x", fmt);
  }
  return factor;
}


FX_ENTRY void FX_CALL 
grTexDownloadMipMap( GrChipID_t tmu,
                     FxU32      startAddress,
                     FxU32      evenOdd,
                     GrTexInfo  *info )
{
    int width, height, i, j;
    unsigned char* texture = NULL;
    unsigned char* filtered_texture = NULL;
    int factor;
    int glformat = GL_RGBA8;
    int gltexfmt, glpixfmt, glpackfmt;
    gltexfmt = glpixfmt = glpackfmt = 0;
    WriteLog(M64MSG_VERBOSE, "grTexDownloadMipMap(%d,%d,%d)\r\n", tmu, startAddress, evenOdd);
    if (info->largeLodLog2 != info->smallLodLog2) display_warning("grTexDownloadMipMap : loading more than one LOD");

    if (info->aspectRatioLog2 < 0)
    {
        height = 1 << info->largeLodLog2;
        width = height >> -info->aspectRatioLog2;
    }
    else
    {
        width = 1 << info->largeLodLog2;
        height = width >> info->aspectRatioLog2;
    }

    if (packed_pixels_support < 0) {
      if (isExtensionSupported("GL_EXT_packed_pixels") == FALSE)
        packed_pixels_support = 0;
      else
        packed_pixels_support = 1;
    }
    if (!packed_pixels_support || getFilter())
      factor = -1;
    else
      factor = grTexFormat2GLPackedFmt(info->format, &gltexfmt, &glpixfmt, &glpackfmt);
    if (factor < 0) {
      texture = (unsigned char*)malloc(width*height*4);
    // VP fixed the texture conversions to be more accurate, also swapped
    // the for i/j loops so that is is less likely to break the memory cache
    switch(info->format)
    {
      case GR_TEXFMT_ALPHA_8:
        for (i=0; i<height; i++)
        { 
          for (j=0; j<width; j++)
          {
            texture[i*width*4+j*4+0]= ((unsigned char*)info->data)[i*width+j];
            texture[i*width*4+j*4+1]= ((unsigned char*)info->data)[i*width+j];
            texture[i*width*4+j*4+2]= ((unsigned char*)info->data)[i*width+j];
            texture[i*width*4+j*4+3]= ((unsigned char*)info->data)[i*width+j];
          }
        }
        factor = 1;
        glformat = GL_INTENSITY8;
        break;
      case GR_TEXFMT_ALPHA_INTENSITY_44:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            // VP fix : we want F --> FF and 0 --> 00
            FxU32 a = ((unsigned char*)info->data)[i*width+j]&0xF0;
            a = a | (a>>4);
            texture[i*width*4+j*4+3]= a | (a>>4);
            a = ((unsigned char*)info->data)[i*width+j]&0x0F;
            a = a | (a<<4);
            texture[i*width*4+j*4+0]= a;
            texture[i*width*4+j*4+1]= a;
            texture[i*width*4+j*4+2]= a;
          }
        }
        factor = 1;
        glformat = GL_LUMINANCE4_ALPHA4;
        break;
      case GR_TEXFMT_RGB_565:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            FxU32 a;
            texture[i*width*4+j*4+3] = 0;
            a = ((((unsigned short*)info->data)[i*width+j]>>11)&0x1F);
            texture[i*width*4+j*4+0]=(a<<3) | (a>>2);
            a = ((((unsigned short*)info->data)[i*width+j]>> 5)&0x3F);
            texture[i*width*4+j*4+1]=(a<<2) | (a>>4);
            a = ((((unsigned short*)info->data)[i*width+j]>> 0)&0x1F);
            texture[i*width*4+j*4+2]=(a<<3) | (a>>2);
          }
        }
        factor = 2;
        break;
      case GR_TEXFMT_ARGB_1555:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            FxU32 a;
            texture[i*width*4+j*4+3]=(((unsigned short*)info->data)[i*width+j]>>15)!=0 ? 0xFF : 0;
            a = ((((unsigned short*)info->data)[i*width+j]>>10)&0x1F);
            texture[i*width*4+j*4+0]=(a<<3) | (a>>2);
            a = ((((unsigned short*)info->data)[i*width+j]>> 5)&0x1F);
            texture[i*width*4+j*4+1]=(a<<3) | (a>>2);
            a = ((((unsigned short*)info->data)[i*width+j]>> 0)&0x1F);
            texture[i*width*4+j*4+2]=(a<<3) | (a>>2);
          }
        }
        factor = 2;
        glformat = GL_RGB5_A1;
        break;
      case GR_TEXFMT_ALPHA_INTENSITY_88:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            texture[i*width*4+j*4+3]= ((unsigned char*)info->data)[i*width*2+j*2+1];
            texture[i*width*4+j*4+0]= ((unsigned char*)info->data)[i*width*2+j*2];
            texture[i*width*4+j*4+1]= ((unsigned char*)info->data)[i*width*2+j*2];
            texture[i*width*4+j*4+2]= ((unsigned char*)info->data)[i*width*2+j*2];
          }
        }
        factor = 2;
        glformat = GL_LUMINANCE8_ALPHA8;
        break;
      case GR_TEXFMT_ARGB_4444:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            // VP fix : we want F --> FF and 0 --> 00
            FxU32 a = ((((unsigned short*)info->data)[i*width+j]>>12));
            texture[i*width*4+j*4+3]=a | (a<<4);
            a = ((((unsigned short*)info->data)[i*width+j]>> 8)&0xF);
            texture[i*width*4+j*4+0]=a | (a<<4);
            a = ((((unsigned short*)info->data)[i*width+j]>> 4)&0xF);
            texture[i*width*4+j*4+1]=a | (a<<4);
            a = ((((unsigned short*)info->data)[i*width+j]    )&0xF);
            texture[i*width*4+j*4+2]=a | (a<<4);
          }
        }
        factor = 2;
        glformat = GL_RGBA4;
        break;
      case GR_TEXFMT_ARGB_8888:
        for (i=0; i<height; i++)
        {
          for (j=0; j<width; j++)
          {
            texture[i*width*4+j*4+3]= ((unsigned char*)info->data)[i*width*4+j*4+3];
            texture[i*width*4+j*4+0]= ((unsigned char*)info->data)[i*width*4+j*4+2];
            texture[i*width*4+j*4+1]= ((unsigned char*)info->data)[i*width*4+j*4+1];
            texture[i*width*4+j*4+2]= ((unsigned char*)info->data)[i*width*4+j*4+0];
          }
        }
        factor = 4;
        glformat = GL_RGBA8;
        break;
      default:
        display_warning("grTexDownloadMipMap : unknown texture format: %x", info->format);
        factor = 0;
    }
    }
    if (nbTextureUnits <= 2)
        glActiveTextureARB(GL_TEXTURE1_ARB);
    else
        glActiveTextureARB(GL_TEXTURE2_ARB);
    remove_tex(startAddress+1, startAddress+1+width*height*factor);
    add_tex(startAddress+1);
    glBindTexture(GL_TEXTURE_2D, startAddress+1);
    if (texture != NULL) {
      if (getFilter() == 0) {
        glTexImage2D(GL_TEXTURE_2D, 0, glformat, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, texture);
      } else
    {
      int width2, height2;
      filtered_texture = filter(texture, width, height, &width2, &height2);
      glTexImage2D(GL_TEXTURE_2D, 0, 4, width2, height2, 0, GL_RGBA, GL_UNSIGNED_BYTE, filtered_texture);
    }
    } else {
      glTexImage2D(GL_TEXTURE_2D, 0, gltexfmt, width, height, 0, glpixfmt, glpackfmt, info->data);
//     if (info->format == GR_TEXFMT_RGB_565) {
//       GLint ifmt;
//       glGetTexLevelParameteriv(GL_TEXTURE_2D, 0, GL_TEXTURE_INTERNAL_FORMAT, &ifmt);
//       LOG("dltex (%06x) %3dx%3d format %x --> %x\n", startAddress, width, height, info->format, ifmt);
//       printf("dltex (%06x) %3dx%3d format %x --> %x\n", startAddress, width, height, info->format, ifmt);
//     }
  }
    glBindTexture(GL_TEXTURE_2D, default_texture);
    if (texture) free(texture);
    if (filtered_texture) free(filtered_texture);
}

int CheckTextureBufferFormat(GrChipID_t tmu, FxU32 startAddress, GrTexInfo *info );

FX_ENTRY void FX_CALL 
grTexSource( GrChipID_t tmu,
             FxU32      startAddress,
             FxU32      evenOdd,
             GrTexInfo  *info )
{
    WriteLog(M64MSG_VERBOSE, "grTexSource(%d,%d,%d)\r\n", tmu, startAddress, evenOdd);
    //if ((startAddress+1) == pBufferAddress && render_to_texture) updateTexture();
    //if ((startAddress+1) == pBufferAddress) display_warning("texsource");
    
    if (tmu == GR_TMU1 || nbTextureUnits <= 2)
    {
        if (tmu == GR_TMU1 && nbTextureUnits <= 2) return;
        glActiveTextureARB(GL_TEXTURE0_ARB);

        if (info->aspectRatioLog2 < 0)
        {
            tex0_height = 256;
            tex0_width = tex0_height >> -info->aspectRatioLog2;
        }
        else
        {
            tex0_width = 256;
            tex0_height = tex0_width >> info->aspectRatioLog2;
        }

        glBindTexture(GL_TEXTURE_2D, startAddress+1);
#ifdef VPDEBUG
        dump_tex(startAddress+1);
#endif
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_filter0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, mag_filter0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_s0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_t0);
        if(!glsl_support)
        {
            if (need_lambda[0])
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, lambda_color[0]);
            else
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, texture_env_color);
            updateCombiner(0);
            updateCombinera(0);
        }
    //printf("grTexSource %x %dx%d fmt %x\n", startAddress+1, tex0_width, tex0_height, info->format);
    }
    else
    {
        glActiveTextureARB(GL_TEXTURE1_ARB);

        if (info->aspectRatioLog2 < 0)
        {
            tex1_height = 256;
            tex1_width = tex1_height >> -info->aspectRatioLog2;
        }
        else
        {
            tex1_width = 256;
            tex1_height = tex1_width >> info->aspectRatioLog2;
        }

        glBindTexture(GL_TEXTURE_2D, startAddress+1);
#ifdef VPDEBUG
        dump_tex(startAddress+1);
#endif
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_filter1);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, mag_filter1);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_s1);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_t1);
        if(!glsl_support)
        {
            if (need_lambda[1])
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, lambda_color[1]);
            else
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, texture_env_color);
            updateCombiner(1);
            updateCombinera(1);
        }
    //printf("grTexSource %x %dx%d fmt %x\n", startAddress+1, tex1_width, tex1_height, info->format);
    }
    if(!CheckTextureBufferFormat(tmu, startAddress+1, info))
    {
        if(tmu == 0 && blackandwhite1 != 0)
        {
            blackandwhite1 = 0;
            need_to_compile = 1;
        }
        if(tmu == 1 && blackandwhite0 != 0)
        {
            blackandwhite0 = 0;
            need_to_compile = 1;
        }
    }
}

FX_ENTRY void FX_CALL 
grTexDetailControl(
                   GrChipID_t tmu,
                   int lod_bias,
                   FxU8 detail_scale,
                   float detail_max
                   )
{
    WriteLog(M64MSG_VERBOSE, "grTexDetailControl(%d,%d,%d,%f)\r\n", tmu, lod_bias, detail_scale, detail_max);
    if (lod_bias != 31 && detail_scale != 7)
    {
        if (!lod_bias && !detail_scale && !detail_max) return;
        else
            display_warning("grTexDetailControl : %d, %d, %f", lod_bias, detail_scale, detail_max);
    }
    lambda = detail_max;
    if(lambda > 1.0f)
    {
        lambda = 1.0f - (255.0f - lambda);
    }
    if(lambda > 1.0f) display_warning("lambda:%f", lambda);
    
    if(!glsl_support)
    {
        if (tmu == GR_TMU1 || nbTextureUnits <= 2)
        {
            if (tmu == GR_TMU1 && nbTextureUnits <= 2) return;
            if (need_lambda[0])
            {
                int i;
                glActiveTextureARB(GL_TEXTURE0_ARB);
                for (i=0; i<3; i++) lambda_color[0][i] = texture_env_color[i];
                lambda_color[0][3] = lambda;
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, lambda_color[0]);
            }
        }
        else
        {
            if (need_lambda[1])
            {
                int i;
                glActiveTextureARB(GL_TEXTURE1_ARB);
                for (i=0; i<3; i++) lambda_color[1][i] = texture_env_color[i];
                lambda_color[1][3] = lambda;
                glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, lambda_color[1]);
            }
        }
    }
    else
        set_lambda();
}

FX_ENTRY void FX_CALL 
grTexLodBiasValue(GrChipID_t tmu, float bias )
{
    WriteLog(M64MSG_VERBOSE, "grTexLodBiasValue(%d,%f)\r\n", tmu, bias);
    /*if (bias != 0 && bias != 1.0f)
        display_warning("grTexLodBiasValue : %f", bias);*/
}

FX_ENTRY void FX_CALL 
grTexFilterMode(
                GrChipID_t tmu,
                GrTextureFilterMode_t minfilter_mode,
                GrTextureFilterMode_t magfilter_mode
                )
{
    WriteLog(M64MSG_VERBOSE, "grTexFilterMode(%d,%d,%d)\r\n", tmu, minfilter_mode, magfilter_mode);
    if (tmu == GR_TMU1 || nbTextureUnits <= 2)
    {
        if (tmu == GR_TMU1 && nbTextureUnits <= 2) return;
        if (minfilter_mode == GR_TEXTUREFILTER_POINT_SAMPLED) min_filter0 = GL_NEAREST;
        else min_filter0 = GL_LINEAR;

        if (magfilter_mode == GR_TEXTUREFILTER_POINT_SAMPLED) mag_filter0 = GL_NEAREST;
        else mag_filter0 = GL_LINEAR;

        glActiveTextureARB(GL_TEXTURE0_ARB);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_filter0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, mag_filter0);
    }
    else
    {
        if (minfilter_mode == GR_TEXTUREFILTER_POINT_SAMPLED) min_filter1 = GL_NEAREST;
        else min_filter1 = GL_LINEAR;

        if (magfilter_mode == GR_TEXTUREFILTER_POINT_SAMPLED) mag_filter1 = GL_NEAREST;
        else mag_filter1 = GL_LINEAR;

        glActiveTextureARB(GL_TEXTURE1_ARB);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, min_filter1);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, mag_filter1);
    }
}

FX_ENTRY void FX_CALL 
grTexClampMode(
               GrChipID_t tmu,
               GrTextureClampMode_t s_clampmode,
               GrTextureClampMode_t t_clampmode
               )
{
    WriteLog(M64MSG_VERBOSE, "grTexClampMode(%d, %d, %d)\r\n", tmu, s_clampmode, t_clampmode);
    if (tmu == GR_TMU1 || nbTextureUnits <= 2)
    {
        if (tmu == GR_TMU1 && nbTextureUnits <= 2) return;
        switch(s_clampmode)
        {
        case GR_TEXTURECLAMP_WRAP:
            wrap_s0 = GL_REPEAT;
            break;
        case GR_TEXTURECLAMP_CLAMP:
            wrap_s0 = GL_CLAMP_TO_EDGE;
            break;
        case GR_TEXTURECLAMP_MIRROR_EXT:
            wrap_s0 = GL_MIRRORED_REPEAT_ARB;
            break;
        default:
            display_warning("grTexClampMode : unknown s_clampmode : %x", s_clampmode);
        }
        switch(t_clampmode)
        {
        case GR_TEXTURECLAMP_WRAP:
            wrap_t0 = GL_REPEAT;
            break;
        case GR_TEXTURECLAMP_CLAMP:
            wrap_t0 = GL_CLAMP_TO_EDGE;
            break;
        case GR_TEXTURECLAMP_MIRROR_EXT:
            wrap_t0 = GL_MIRRORED_REPEAT_ARB;
            break;
        default:
            display_warning("grTexClampMode : unknown t_clampmode : %x", t_clampmode);
        }
        glActiveTextureARB(GL_TEXTURE0_ARB);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_s0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_t0);
    }
    else
    {
        switch(s_clampmode)
        {
        case GR_TEXTURECLAMP_WRAP:
            wrap_s1 = GL_REPEAT;
            break;
        case GR_TEXTURECLAMP_CLAMP:
            wrap_s1 = GL_CLAMP_TO_EDGE;
            break;
        case GR_TEXTURECLAMP_MIRROR_EXT:
            wrap_s1 = GL_MIRRORED_REPEAT_ARB;
            break;
        default:
            display_warning("grTexClampMode : unknown s_clampmode : %x", s_clampmode);
        }
        switch(t_clampmode)
        {
        case GR_TEXTURECLAMP_WRAP:
            wrap_t1 = GL_REPEAT;
            break;
        case GR_TEXTURECLAMP_CLAMP:
            wrap_t1 = GL_CLAMP_TO_EDGE;
            break;
        case GR_TEXTURECLAMP_MIRROR_EXT:
            wrap_t1 = GL_MIRRORED_REPEAT_ARB;
            break;
        default:
            display_warning("grTexClampMode : unknown t_clampmode : %x", t_clampmode);
        }
        glActiveTextureARB(GL_TEXTURE1_ARB);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap_s1);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap_t1);
    }
}

