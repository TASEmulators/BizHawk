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

//
// vertex - loads vertices
//

static void uc1_vertex()
{
  DWORD addr = segoffset(rdp.cmd1) & 0x00FFFFFF;
  int v0, i, n;
  float x, y, z;

  rdp.v0 = v0 = (rdp.cmd0 >> 17) & 0x7F;     // Current vertex
  rdp.vn = n = (WORD)(rdp.cmd0 >> 10) & 0x3F;    // Number to copy

  // This is special, not handled in update(), but here
  // * Matrix Pre-multiplication idea by Gonetz (Gonetz@ngs.ru)
  if (rdp.update & UPDATE_MULT_MAT)
  {
    rdp.update ^= UPDATE_MULT_MAT;
    MulMatrices(rdp.model, rdp.proj, rdp.combined);
  }
  // *

  // This is special, not handled in update()
  if (rdp.update & UPDATE_LIGHTS)
  {
    rdp.update ^= UPDATE_LIGHTS;
    
    // Calculate light vectors
    for (DWORD l=0; l<rdp.num_lights; l++)
    {
      InverseTransformVector(&rdp.light[l].dir_x, rdp.light_vector[l], rdp.model);
      NormalizeVector (rdp.light_vector[l]);
    }
  }

  FRDP ("uc1:vertex v0:%d, n:%d, from: %08lx\n", v0, n, addr);

  for (i=0; i < (n<<4); i+=16)
  {
    VERTEX *v = &rdp.vtx[v0 + (i>>4)];
    x   = (float)((short*)gfx.RDRAM)[(((addr+i) >> 1) + 0)^1];
    y   = (float)((short*)gfx.RDRAM)[(((addr+i) >> 1) + 1)^1];
    z   = (float)((short*)gfx.RDRAM)[(((addr+i) >> 1) + 2)^1];
    v->flags  = ((WORD*)gfx.RDRAM)[(((addr+i) >> 1) + 3)^1];
    v->ou = (float)((short*)gfx.RDRAM)[(((addr+i) >> 1) + 4)^1] * rdp.tiles[rdp.cur_tile].s_scale;
    v->ov = (float)((short*)gfx.RDRAM)[(((addr+i) >> 1) + 5)^1] * rdp.tiles[rdp.cur_tile].t_scale;
    v->a    = ((BYTE*)gfx.RDRAM)[(addr+i + 15)^3];

    v->x = x*rdp.combined[0][0] + y*rdp.combined[1][0] + z*rdp.combined[2][0] + rdp.combined[3][0];
    v->y = x*rdp.combined[0][1] + y*rdp.combined[1][1] + z*rdp.combined[2][1] + rdp.combined[3][1];
    v->z = x*rdp.combined[0][2] + y*rdp.combined[1][2] + z*rdp.combined[2][2] + rdp.combined[3][2];
    v->w = x*rdp.combined[0][3] + y*rdp.combined[1][3] + z*rdp.combined[2][3] + rdp.combined[3][3];

    
    v->oow = 1.0f / v->w;
    v->x_w = v->x * v->oow;
    v->y_w = v->y * v->oow;
    v->z_w = v->z * v->oow;
    CalculateFog (v);

    v->uv_calculated = 0xFFFFFFFF;
    v->screen_translated = 0;
    v->shade_mods_allowed = 1;
    v->uv_fixed = 0;

    v->scr_off = 0;
    if (v->x < -v->w) v->scr_off |= 1;
    if (v->x > v->w) v->scr_off |= 2;
    if (v->y < -v->w) v->scr_off |= 4;
    if (v->y > v->w) v->scr_off |= 8;
    if (v->w < 0.1f) v->scr_off |= 16;

    if (rdp.geom_mode & 0x00020000)
    {
      v->vec[0] = ((char*)gfx.RDRAM)[(addr+i + 12)^3];
      v->vec[1] = ((char*)gfx.RDRAM)[(addr+i + 13)^3];
      v->vec[2] = ((char*)gfx.RDRAM)[(addr+i + 14)^3];
      if (rdp.geom_mode & 0x80000) 
        calc_linear (v);
      else if (rdp.geom_mode & 0x40000) 
        calc_sphere (v);
      NormalizeVector (v->vec);

      calc_light (v);
    }
    else
    {
      v->r = ((BYTE*)gfx.RDRAM)[(addr+i + 12)^3];
      v->g = ((BYTE*)gfx.RDRAM)[(addr+i + 13)^3];
      v->b = ((BYTE*)gfx.RDRAM)[(addr+i + 14)^3];
    }
#ifdef EXTREME_LOGGING
    FRDP ("v%d - x: %f, y: %f, z: %f, w: %f, u: %f, v: %f\n", i>>4, v->x, v->y, v->z, v->w, v->ou, v->ov);
#endif

  }
}

//
// tri1 - renders a triangle
//

static void uc1_tri1()
{
  if (rdp.skip_drawing)
  {
    RDP("uc1:tri1. skipped\n");
    return;
  }
  FRDP("uc1:tri1 #%d - %d, %d, %d - %08lx - %08lx\n", rdp.tri_n,
    ((rdp.cmd1 >> 17) & 0x7F),
    ((rdp.cmd1 >> 9) & 0x7F),
    ((rdp.cmd1 >> 1) & 0x7F), rdp.cmd0, rdp.cmd1);

  VERTEX *v[3] = {
    &rdp.vtx[(rdp.cmd1 >> 17) & 0x7F],
    &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F],
    &rdp.vtx[(rdp.cmd1 >> 1) & 0x7F]
  };

  if (cull_tri(v))
    rdp.tri_n ++;
  else
  {
    update ();

    DrawTri (v);
    rdp.tri_n ++;
  }
}

static void uc1_tri2 ()
{
  if (rdp.skip_drawing)
  {
    RDP("uc1:tri2. skipped\n");
    return;
  }
  RDP ("uc1:tri2");
  
  FRDP(" #%d, #%d - %d, %d, %d - %d, %d, %d\n", rdp.tri_n, rdp.tri_n+1,
    ((rdp.cmd0 >> 17) & 0x7F),
    ((rdp.cmd0 >> 9) & 0x7F),
    ((rdp.cmd0 >> 1) & 0x7F),
    ((rdp.cmd1 >> 17) & 0x7F),
    ((rdp.cmd1 >> 9) & 0x7F),
    ((rdp.cmd1 >> 1) & 0x7F));

  VERTEX *v[6] = {
    &rdp.vtx[(rdp.cmd0 >> 17) & 0x7F],
    &rdp.vtx[(rdp.cmd0 >> 9) & 0x7F],
    &rdp.vtx[(rdp.cmd0 >> 1) & 0x7F],
    &rdp.vtx[(rdp.cmd1 >> 17) & 0x7F],
    &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F],
    &rdp.vtx[(rdp.cmd1 >> 1) & 0x7F]
  };

  BOOL updated = 0;

  if (cull_tri(v))
    rdp.tri_n ++;
  else
  {
    updated = 1;
    update ();
    DrawTri (v);
    rdp.tri_n ++;
  }

  if (cull_tri(v+3))
    rdp.tri_n ++;
  else
  {
    if (!updated)
      update ();
    DrawTri (v+3);
    rdp.tri_n ++;
  }
}

static void uc1_line3d()
{
  if (((rdp.cmd1&0xFF000000) == 0) && ((rdp.cmd0&0x00FFFFFF) == 0))
  {
    WORD width = (WORD)(rdp.cmd1&0xFF) + 1;
    
    FRDP("uc1:line3d #%d, #%d - %d, %d\n", rdp.tri_n, rdp.tri_n+1,
      (rdp.cmd1 >> 17) & 0x7F,
      (rdp.cmd1 >> 9) & 0x7F);

    VERTEX *v[3] = {
      &rdp.vtx[(rdp.cmd1 >> 17) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F]
    };

    if (cull_tri(v))
      rdp.tri_n ++;
    else
    {
      update ();
      DrawTri (v, width);
      rdp.tri_n ++;
    }
  }
  else
  {
    FRDP("uc1:quad3d #%d, #%d\n", rdp.tri_n, rdp.tri_n+1);

    VERTEX *v[6] = {
      &rdp.vtx[(rdp.cmd1 >> 25) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 17) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 1) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 25) & 0x7F],
      &rdp.vtx[(rdp.cmd1 >> 9) & 0x7F]
    };

    BOOL updated = 0;

    if (cull_tri(v))
      rdp.tri_n ++;
    else
    {
      updated = 1;
      update ();

      DrawTri (v);
      rdp.tri_n ++;
    }

    if (cull_tri(v+3))
      rdp.tri_n ++;
    else
    {
      if (!updated)
        update ();

      DrawTri (v+3);
      rdp.tri_n ++;
    }
  }
}

DWORD branch_dl = 0;

static void uc1_rdphalf_1()
{
  RDP ("uc1:rdphalf_1\n");
  branch_dl = rdp.cmd1;
}

static void uc1_branch_z()
{
  DWORD addr = segoffset(branch_dl);
  FRDP ("uc1:branch_less_z, addr: %08lx\n", addr);
  DWORD vtx = (rdp.cmd0 & 0xFFF) >> 1;
  if( fabs(rdp.vtx[vtx].z) <= (rdp.cmd1/*&0xFFFF*/) )
  {
    rdp.pc[rdp.pc_i] = addr;
  }
}

