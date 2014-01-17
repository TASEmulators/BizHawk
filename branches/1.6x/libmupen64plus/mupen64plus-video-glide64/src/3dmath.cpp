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
#include "3dmath.h"
#if !defined(NO_ASM)
#include <xmmintrin.h>
#endif

void calc_light (VERTEX *v)
{
    float light_intensity = 0.0f;
    register float color[3] = {rdp.light[rdp.num_lights].r, rdp.light[rdp.num_lights].g, rdp.light[rdp.num_lights].b};
    for (DWORD l=0; l<rdp.num_lights; l++)
    {
        light_intensity = DotProduct (rdp.light_vector[l], v->vec);

        if (light_intensity > 0.0f) 
        {
            color[0] += rdp.light[l].r * light_intensity;
            color[1] += rdp.light[l].g * light_intensity;
            color[2] += rdp.light[l].b * light_intensity;
        }
    }

    if (color[0] > 1.0f) color[0] = 1.0f;
    if (color[1] > 1.0f) color[1] = 1.0f;
    if (color[2] > 1.0f) color[2] = 1.0f;

    v->r = (BYTE)(color[0]*255.0f);
    v->g = (BYTE)(color[1]*255.0f);
    v->b = (BYTE)(color[2]*255.0f);
}

__inline void TransformVector (float *src, float *dst, float mat[4][4])
{
    dst[0] = mat[0][0]*src[0] + mat[1][0]*src[1] + mat[2][0]*src[2];
    dst[1] = mat[0][1]*src[0] + mat[1][1]*src[1] + mat[2][1]*src[2];
    dst[2] = mat[0][2]*src[0] + mat[1][2]*src[1] + mat[2][2]*src[2];
}

//*
void calc_linear (VERTEX *v)
{
    float vec[3];
    
    TransformVector (v->vec, vec, rdp.model);
//  TransformVector (v->vec, vec, rdp.combined);
    NormalizeVector (vec);
    float x, y;
    if (!rdp.use_lookat)
    {
    x = vec[0];
    y = vec[1];
    }
    else
    {
    x = DotProduct (rdp.lookat[0], vec);
    y = DotProduct (rdp.lookat[1], vec);
    }
    if (rdp.cur_cache[0])
    {
        // scale >> 6 is size to map to
        v->ou = (acosf(x)/3.1415f) * (rdp.tiles[rdp.cur_tile].org_s_scale >> 6);
        v->ov = (acosf(y)/3.1415f) * (rdp.tiles[rdp.cur_tile].org_t_scale >> 6);
    }
}
//*/

/*
void calc_linear (VERTEX *v)
{
    float vec[3];

    TransformVector (v->vec, vec, rdp.combined);
    NormalizeVector (vec);

    if (rdp.cur_cache[0])
    {
        // scale >> 6 is size to map to
        v->ou = (acosf(vec[0])/3.1415f) * (rdp.tiles[rdp.cur_tile].org_s_scale >> 6);
        v->ov = (acosf(vec[1])/3.1415f) * (rdp.tiles[rdp.cur_tile].org_t_scale >> 6);
    }
}
//*/

void calc_sphere (VERTEX *v)
{
  //RDP("calc_sphere\n");
  float vec[3];
  int s_scale, t_scale;
  if (settings.chopper)
  {
    s_scale = min(rdp.tiles[rdp.cur_tile].org_s_scale >> 6, rdp.tiles[rdp.cur_tile].lr_s);
    t_scale = min(rdp.tiles[rdp.cur_tile].org_t_scale >> 6, rdp.tiles[rdp.cur_tile].lr_t);
  }
  else
  {
    s_scale = rdp.tiles[rdp.cur_tile].org_s_scale >> 6;
    t_scale = rdp.tiles[rdp.cur_tile].org_t_scale >> 6;
  }
  TransformVector (v->vec, vec, rdp.model);
  //    TransformVector (v->vec, vec, rdp.combined);
  NormalizeVector (vec);
  float x = DotProduct (rdp.lookat[0], vec);
  float y = DotProduct (rdp.lookat[1], vec);
  v->ou = (x * 0.5f + 0.5f) * s_scale;
  v->ov = (y * 0.5f + 0.5f) * t_scale;
}

void __stdcall MulMatricesNOSSE(float m1[4][4],float m2[4][4],float r[4][4])
{

  /*for (int i=0; i<4; i++)
  {
    for (int j=0; j<4; j++)
    {
        r[i][j] =
        m1[i][0] * m2[0][j] +
        m1[i][1] * m2[1][j] +
        m1[i][2] * m2[2][j] +
        m1[i][3] * m2[3][j];
    }
  }*/
    r[0][0]  = m1[0][0]*m2[0][0] + m1[0][1]*m2[1][0] + m1[0][2]*m2[2][0] + m1[0][3]*m2[3][0];
    r[0][1]  = m1[0][0]*m2[0][1] + m1[0][1]*m2[1][1] + m1[0][2]*m2[2][1] + m1[0][3]*m2[3][1];
    r[0][2]  = m1[0][0]*m2[0][2] + m1[0][1]*m2[1][2] + m1[0][2]*m2[2][2] + m1[0][3]*m2[3][2];
    r[0][3]  = m1[0][0]*m2[0][3] + m1[0][1]*m2[1][3] + m1[0][2]*m2[2][3] + m1[0][3]*m2[3][3];

    r[1][0]  = m1[1][0]*m2[0][0] + m1[1][1]*m2[1][0] + m1[1][2]*m2[2][0] + m1[1][3]*m2[3][0];
    r[1][1]  = m1[1][0]*m2[0][1] + m1[1][1]*m2[1][1] + m1[1][2]*m2[2][1] + m1[1][3]*m2[3][1];
    r[1][2]  = m1[1][0]*m2[0][2] + m1[1][1]*m2[1][2] + m1[1][2]*m2[2][2] + m1[1][3]*m2[3][2];
    r[1][3]  = m1[1][0]*m2[0][3] + m1[1][1]*m2[1][3] + m1[1][2]*m2[2][3] + m1[1][3]*m2[3][3];

    r[2][0]  = m1[2][0]*m2[0][0] + m1[2][1]*m2[1][0] + m1[2][2]*m2[2][0] + m1[2][3]*m2[3][0];
    r[2][1]  = m1[2][0]*m2[0][1] + m1[2][1]*m2[1][1] + m1[2][2]*m2[2][1] + m1[2][3]*m2[3][1];
    r[2][2]  = m1[2][0]*m2[0][2] + m1[2][1]*m2[1][2] + m1[2][2]*m2[2][2] + m1[2][3]*m2[3][2];
    r[2][3]  = m1[2][0]*m2[0][3] + m1[2][1]*m2[1][3] + m1[2][2]*m2[2][3] + m1[2][3]*m2[3][3];

    r[3][0]  = m1[3][0]*m2[0][0] + m1[3][1]*m2[1][0] + m1[3][2]*m2[2][0] + m1[3][3]*m2[3][0];
    r[3][1]  = m1[3][0]*m2[0][1] + m1[3][1]*m2[1][1] + m1[3][2]*m2[2][1] + m1[3][3]*m2[3][1];
    r[3][2]  = m1[3][0]*m2[0][2] + m1[3][1]*m2[1][2] + m1[3][2]*m2[2][2] + m1[3][3]*m2[3][2];
    r[3][3]  = m1[3][0]*m2[0][3] + m1[3][1]*m2[1][3] + m1[3][2]*m2[2][3] + m1[3][3]*m2[3][3];
}

void __stdcall MulMatricesSSE(float m1[4][4],float m2[4][4],float r[4][4])
{
#if defined(__GNUC__) && !defined(NO_ASM)
    /* [row][col]*/
    typedef float v4sf __attribute__ ((vector_size (16)));
    v4sf row0 = __builtin_ia32_loadups(m2[0]);
    v4sf row1 = __builtin_ia32_loadups(m2[1]);
    v4sf row2 = __builtin_ia32_loadups(m2[2]);
    v4sf row3 = __builtin_ia32_loadups(m2[3]);

    for (int i = 0; i < 4; ++i)
    {
    v4sf leftrow = __builtin_ia32_loadups(m1[i]);
    
    // Fill tmp with four copies of leftrow[0]
    v4sf tmp = leftrow;
    tmp = _mm_shuffle_ps (tmp, tmp, 0);
    // Calculate the four first summands
    v4sf destrow = tmp * row0;
    
    // Fill tmp with four copies of leftrow[1]
    tmp = leftrow;
    tmp = _mm_shuffle_ps (tmp, tmp, 1 + (1 << 2) + (1 << 4) + (1 << 6));
    destrow += tmp * row1;
    
    // Fill tmp with four copies of leftrow[2]
    tmp = leftrow;
    tmp = _mm_shuffle_ps (tmp, tmp, 2 + (2 << 2) + (2 << 4) + (2 << 6));
    destrow += tmp * row2;
    
    // Fill tmp with four copies of leftrow[3]
    tmp = leftrow;
    tmp = _mm_shuffle_ps (tmp, tmp, 3 + (3 << 2) + (3 << 4) + (3 << 6));
    destrow += tmp * row3;
    
    __builtin_ia32_storeups(r[i], destrow);
    }
#elif !defined(NO_ASM)
    __asm
    {
        mov     eax, dword ptr [r]  
        mov     ecx, dword ptr [m1]
        mov     edx, dword ptr [m2]

        movaps  xmm0,[edx]
        movaps  xmm1,[edx+16]
        movaps  xmm2,[edx+32]
        movaps  xmm3,[edx+48]

// r[0][0],r[0][1],r[0][2],r[0][3]

        movaps  xmm4,xmmword ptr[ecx]
        movaps  xmm5,xmm4
        movaps  xmm6,xmm4
        movaps  xmm7,xmm4

        shufps  xmm4,xmm4,00000000b
        shufps  xmm5,xmm5,01010101b
        shufps  xmm6,xmm6,10101010b
        shufps  xmm7,xmm7,11111111b

        mulps   xmm4,xmm0
        mulps   xmm5,xmm1
        mulps   xmm6,xmm2
        mulps   xmm7,xmm3

        addps   xmm4,xmm5
        addps   xmm4,xmm6
        addps   xmm4,xmm7

        movaps  xmmword ptr[eax],xmm4

// r[1][0],r[1][1],r[1][2],r[1][3]

        movaps  xmm4,xmmword ptr[ecx+16]
        movaps  xmm5,xmm4
        movaps  xmm6,xmm4
        movaps  xmm7,xmm4

        shufps  xmm4,xmm4,00000000b
        shufps  xmm5,xmm5,01010101b
        shufps  xmm6,xmm6,10101010b
        shufps  xmm7,xmm7,11111111b

        mulps   xmm4,xmm0
        mulps   xmm5,xmm1
        mulps   xmm6,xmm2
        mulps   xmm7,xmm3

        addps   xmm4,xmm5
        addps   xmm4,xmm6
        addps   xmm4,xmm7

        movaps  xmmword ptr[eax+16],xmm4


// r[2][0],r[2][1],r[2][2],r[2][3]

        movaps  xmm4,xmmword ptr[ecx+32]
        movaps  xmm5,xmm4
        movaps  xmm6,xmm4
        movaps  xmm7,xmm4

        shufps  xmm4,xmm4,00000000b
        shufps  xmm5,xmm5,01010101b
        shufps  xmm6,xmm6,10101010b
        shufps  xmm7,xmm7,11111111b

        mulps   xmm4,xmm0
        mulps   xmm5,xmm1
        mulps   xmm6,xmm2
        mulps   xmm7,xmm3

        addps   xmm4,xmm5
        addps   xmm4,xmm6
        addps   xmm4,xmm7

        movaps  xmmword ptr[eax+32],xmm4

// r[3][0],r[3][1],r[3][2],r[3][3]

        movaps  xmm4,xmmword ptr[ecx+48]
        movaps  xmm5,xmm4
        movaps  xmm6,xmm4
        movaps  xmm7,xmm4

        shufps  xmm4,xmm4,00000000b
        shufps  xmm5,xmm5,01010101b
        shufps  xmm6,xmm6,10101010b
        shufps  xmm7,xmm7,11111111b

        mulps   xmm4,xmm0
        mulps   xmm5,xmm1
        mulps   xmm6,xmm2
        mulps   xmm7,xmm3

        addps   xmm4,xmm5
        addps   xmm4,xmm6
        addps   xmm4,xmm7

        movaps  xmmword ptr[eax+48],xmm4
    }
#endif // _WIN32
}

MULMATRIX MulMatrices = MulMatricesNOSSE;

void math_init()
{
  BOOL IsSSE = FALSE;
#if defined(__GNUC__) && !defined(NO_ASM)
    int edx, eax;
  #if defined(__x86_64__)
    asm volatile(" cpuid;        "
                 : "=a"(eax), "=d"(edx)
                 : "0"(1)
                 : "rbx", "rcx"
                 );
  #else
    asm volatile(" push %%ebx;   "
                 " push %%ecx;   "
                 " cpuid;        "
                 " pop %%ecx;    "
                 " pop %%ebx;    "
                 : "=a"(eax), "=d"(edx)
                 : "0"(1)
                 :
                 );
  #endif
    // Check for SSE
    if (edx & (1 << 25))
    IsSSE = TRUE;
#elif !defined(NO_ASM)
  DWORD dwEdx;
  __try
  {
    __asm 
    {
      mov  eax,1
      cpuid
      mov dwEdx,edx
    }  
  }
  __except(EXCEPTION_EXECUTE_HANDLER)
  {
    return;
  }

  if (dwEdx & (1<<25)) 
  {
    if (dwEdx & (1<<24))
    {      
      __try
      {
        __asm xorps xmm0, xmm0
        IsSSE = TRUE;
      }
      __except(EXCEPTION_EXECUTE_HANDLER)
      {
        return;
      }
    }
  }
#endif // _WIN32
  if (IsSSE)
  {
    MulMatrices = MulMatricesSSE;
    WriteLog(M64MSG_INFO, "SSE detected.\n");
  }
}

