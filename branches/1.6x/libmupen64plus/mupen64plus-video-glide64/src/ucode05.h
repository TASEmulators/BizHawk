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

int cur_mtx = 0;
BOOL billboarding = 0;
int vtx_last = 0;
DWORD dma_offset_mtx = 0;
DWORD dma_offset_vtx = 0;

static void uc5_dma_offsets ()
{
  dma_offset_mtx = rdp.cmd0 & 0x00FFFFFF;
  dma_offset_vtx = rdp.cmd1 & 0x00FFFFFF;
  vtx_last = 0;
  FRDP("uc5:dma_offsets - mtx: %08lx, vtx: %08lx\n", dma_offset_mtx, dma_offset_vtx);
}

static void uc5_matrix ()
{
  // Use segment offset to get the address
  DWORD addr = dma_offset_mtx + (segoffset(rdp.cmd1) & BMASK);

  BYTE n = (BYTE)((rdp.cmd0 >> 16) & 0xF);
  BYTE multiply;

  if (n == 0) //DKR
  {
    n = (BYTE)((rdp.cmd0 >> 22) & 0x3);
    multiply = 0;
  }
  else //JF
  {
    multiply = (BYTE)((rdp.cmd0 >> 23) & 0x1);
  }

  cur_mtx = n;

  FRDP("uc5:matrix - #%d, addr: %08lx\n", n, addr);

  addr >>= 1;

  int x,y;  // matrix index

  if (multiply)
  {
    __declspec( align(16) ) float m[4][4];
    for (x=0; x<16; x+=4) { // Adding 4 instead of one, just to remove mult. later
      for (y=0; y<4; y++) {
        m[x>>2][y] = (float)(
          (((__int32)((WORD*)gfx.RDRAM)[(addr+x+y)^1]) << 16) |
          ((WORD*)gfx.RDRAM)[(addr+x+y+16)^1]
          ) / 65536.0f;
      }
    }
    __declspec( align(16) ) float m_src[4][4];
    memcpy (m_src, rdp.dkrproj[0], 64);
    MulMatrices(m, m_src, rdp.dkrproj[n]);
  }
  else
  {
    for (x=0; x<16; x+=4) { // Adding 4 instead of one, just to remove mult. later
      for (y=0; y<4; y++) {
        rdp.dkrproj[n][x>>2][y] = (float)(
          (((__int32)((WORD*)gfx.RDRAM)[(addr+x+y)^1]) << 16) |
          ((WORD*)gfx.RDRAM)[(addr+x+y+16)^1]
          ) / 65536.0f;
      }
    }
  }
  rdp.update |= UPDATE_MULT_MAT;

#ifdef EXTREME_LOGGING
  FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[n][0][0], rdp.dkrproj[n][0][1], rdp.dkrproj[n][0][2], rdp.dkrproj[n][0][3]);
  FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[n][1][0], rdp.dkrproj[n][1][1], rdp.dkrproj[n][1][2], rdp.dkrproj[n][1][3]);
  FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[n][2][0], rdp.dkrproj[n][2][1], rdp.dkrproj[n][2][2], rdp.dkrproj[n][2][3]);
  FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[n][3][0], rdp.dkrproj[n][3][1], rdp.dkrproj[n][3][2], rdp.dkrproj[n][3][3]);

  for (int i=0; i<3; i++)
  {
    FRDP ("proj %d\n", i);
    FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[i][0][0], rdp.dkrproj[i][0][1], rdp.dkrproj[i][0][2], rdp.dkrproj[i][0][3]);
    FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[i][1][0], rdp.dkrproj[i][1][1], rdp.dkrproj[i][1][2], rdp.dkrproj[i][1][3]);
    FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[i][2][0], rdp.dkrproj[i][2][1], rdp.dkrproj[i][2][2], rdp.dkrproj[i][2][3]);
    FRDP ("{%f,%f,%f,%f}\n", rdp.dkrproj[i][3][0], rdp.dkrproj[i][3][1], rdp.dkrproj[i][3][2], rdp.dkrproj[i][3][3]);
  }
#endif
}

static void uc5_vertex ()
{
  DWORD addr = dma_offset_vtx + (segoffset(rdp.cmd1) & BMASK);

  // | cccc cccc 1111 1??? 0000 0002 2222 2222 | cmd1 = address |
  // c = vtx command
  // 1 = method #1 of getting count
  // 2 = method #2 of getting count
  // ? = unknown, but used
  // 0 = unused

  int n = ((rdp.cmd0 >> 19) & 0x1F);// + 1;
  if (settings.diddy)
    n++;
  //int n = ((rdp.cmd0 & 0x1FF) - 8) / 18; // same thing!

  if (rdp.cmd0 & 0x00010000)
  {
    if (billboarding)
        vtx_last = 1;
  }
  else
    vtx_last = 0;

  int first = ((rdp.cmd0 >> 9) & 0x1F) + vtx_last;
  FRDP ("uc5:vertex - addr: %08lx, first: %d, count: %d, matrix: %08lx\n", addr, first, n, cur_mtx);

  int prj = cur_mtx;

  int start = 0;
  float x, y, z;
  for (int i=first; i<first+n; i++)
  {
    start = (i-first) * 10;
    VERTEX *v = &rdp.vtx[i];
    x   = (float)((short*)gfx.RDRAM)[(((addr+start) >> 1) + 0)^1];
    y   = (float)((short*)gfx.RDRAM)[(((addr+start) >> 1) + 1)^1];
    z   = (float)((short*)gfx.RDRAM)[(((addr+start) >> 1) + 2)^1];

    v->x = x*rdp.dkrproj[prj][0][0] + y*rdp.dkrproj[prj][1][0] + z*rdp.dkrproj[prj][2][0] + rdp.dkrproj[prj][3][0];
    v->y = x*rdp.dkrproj[prj][0][1] + y*rdp.dkrproj[prj][1][1] + z*rdp.dkrproj[prj][2][1] + rdp.dkrproj[prj][3][1];
    v->z = x*rdp.dkrproj[prj][0][2] + y*rdp.dkrproj[prj][1][2] + z*rdp.dkrproj[prj][2][2] + rdp.dkrproj[prj][3][2];
    v->w = x*rdp.dkrproj[prj][0][3] + y*rdp.dkrproj[prj][1][3] + z*rdp.dkrproj[prj][2][3] + rdp.dkrproj[prj][3][3];

    if (billboarding)
    {
      v->x += rdp.vtx[0].x;
      v->y += rdp.vtx[0].y;
      v->z += rdp.vtx[0].z;
      v->w += rdp.vtx[0].w;
    }
    
#ifdef EXTREME_LOGGING
    FRDP ("v%d - x: %f, y: %f, z: %f, w: %f\n", i, v->x, v->y, v->z, v->w);
#endif

    v->oow = 1.0f / v->w;
    v->x_w = v->x * v->oow;
    v->y_w = v->y * v->oow;
    v->z_w = v->z * v->oow;

    v->uv_calculated = 0xFFFFFFFF;
    v->screen_translated = 0;
    v->shade_mods_allowed = 1;

    v->scr_off = 0;
    if (v->x < -v->w) v->scr_off |= 1;
    if (v->x > v->w) v->scr_off |= 2;
    if (v->y < -v->w) v->scr_off |= 4;
    if (v->y > v->w) v->scr_off |= 8;
    if (v->w < 0.1f) v->scr_off |= 16;

    v->r = ((BYTE*)gfx.RDRAM)[(addr+start + 6)^3];
    v->g = ((BYTE*)gfx.RDRAM)[(addr+start + 7)^3];
    v->b = ((BYTE*)gfx.RDRAM)[(addr+start + 8)^3];
    v->a = ((BYTE*)gfx.RDRAM)[(addr+start + 9)^3];
    CalculateFog (v);

#ifdef EXTREME_LOGGING
    FRDP ("vtx%d: x: %f, y: %f, z: %f, w: %f\n", i, v->x, v->y, v->z, v->w);
#endif
  }

  vtx_last += n;
}

static void uc5_tridma ()
{
  vtx_last = 0;    // we've drawn something, so the vertex index needs resetting
  if (rdp.skip_drawing)
    return;

  // | cccc cccc 2222 0000 1111 1111 1111 0000 | cmd1 = address |
  // c = tridma command
  // 1 = method #1 of getting count
  // 2 = method #2 of getting count
  // 0 = unused

  DWORD addr = segoffset(rdp.cmd1) & BMASK;
  int num = (rdp.cmd0 & 0xFFF0) >> 4;
  //int num = ((rdp.cmd0 & 0x00F00000) >> 20) + 1;  // same thing!
  FRDP("uc5:tridma #%d - addr: %08lx, count: %d\n", rdp.tri_n, addr, num);

  int start, v0, v1, v2, flags;
  for (int i=0; i<num; i++)
  {
    start = i << 4;
    v0 = gfx.RDRAM[addr+start];
    v1 = gfx.RDRAM[addr+start+1];
    v2 = gfx.RDRAM[addr+start+2];

    FRDP("tri #%d - %d, %d, %d\n", rdp.tri_n, v0, v1, v2);

    VERTEX *v[3] = {
      &rdp.vtx[v0],
      &rdp.vtx[v1],
      &rdp.vtx[v2]
    };

    flags = gfx.RDRAM[addr+start+3];

    if (flags & 0x40) { // no cull
      rdp.flags &= ~CULLMASK;
      grCullMode (GR_CULL_DISABLE);
    }
    else {        // front cull
      rdp.flags &= ~CULLMASK;
      if (rdp.view_scale[0] < 0) {
        rdp.flags |= CULL_BACK;   // agh, backwards culling
        grCullMode (GR_CULL_POSITIVE);
      }
      else {
        rdp.flags |= CULL_FRONT;
        grCullMode (GR_CULL_NEGATIVE);
      }
     }
    start += 4;

    v[0]->ou = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 5] / 32.0f;
    v[0]->ov = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 4] / 32.0f;
    v[1]->ou = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 3] / 32.0f;
    v[1]->ov = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 2] / 32.0f;
    v[2]->ou = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 1] / 32.0f;
    v[2]->ov = (float)((short*)gfx.RDRAM)[((addr+start) >> 1) + 0] / 32.0f;

    v[0]->uv_calculated = 0xFFFFFFFF;
    v[1]->uv_calculated = 0xFFFFFFFF;
    v[2]->uv_calculated = 0xFFFFFFFF;

    if (cull_tri(v))
      rdp.tri_n ++;
    else
    {
      update ();

      DrawTri (v);
      rdp.tri_n ++;
    }
  }
}

static void uc5_dl_in_mem ()
{
  DWORD addr = segoffset(rdp.cmd1) & BMASK;
  int count = (rdp.cmd0 & 0x00FF0000) >> 16;
  FRDP ("uc5:dl_in_mem - addr: %08lx, count: %d\n", addr, count);

  if (rdp.pc_i >= 9) {
    RDP_E ("** DL stack overflow **\n");
    RDP ("** DL stack overflow **\n");
    return;
  }
  rdp.pc_i ++;  // go to the next PC in the stack
  rdp.pc[rdp.pc_i] = addr;  // jump to the address
  rdp.dl_count = count + 1;
}

static void uc5_moveword()
{
  RDP("uc5:moveword ");

  // Find which command this is (lowest byte of cmd0)
  switch (rdp.cmd0 & 0xFF)
  {
  case 0x02:  // moveword matrix 2 billboard
    billboarding = (rdp.cmd1 & 1);
    FRDP ("matrix billboard - %s\n", str_offon[billboarding]);
    break;

  case 0x04:  // clip (verified same)
    FRDP ("clip %08lx, %08lx\n", rdp.cmd0, rdp.cmd1);
    break;

  case 0x06:  // segment (verified same)
    FRDP ("segment: %08lx -> seg%d\n", rdp.cmd1, (rdp.cmd0 >> 10) & 0x0F);
    rdp.segment[(rdp.cmd0 >> 10) & 0x0F] = rdp.cmd1;
    break;

  case 0x08:
    {
      rdp.fog_multiplier = (short)(rdp.cmd1 >> 16);
      rdp.fog_offset = (short)(rdp.cmd1 & 0x0000FFFF);
      FRDP ("fog: multiplier: %f, offset: %f\n", rdp.fog_multiplier, rdp.fog_offset);
      //      rdp.update |= UPDATE_FOG_ENABLED;
    }
    break;
    
  case 0x0a:  // moveword matrix select
    cur_mtx = (rdp.cmd1 >> 6) & 3;
    FRDP ("matrix select - mtx: %d\n", cur_mtx);
    break;

  default:
    FRDP ("(unknown) %02lx - IGNORED\n", rdp.cmd0&0xFF);
  }
}

static void uc5_setgeometrymode()
{
  FRDP("uc0:setgeometrymode %08lx\n", rdp.cmd1);

  rdp.geom_mode |= rdp.cmd1;

  if (rdp.cmd1 & 0x00000001)  // Z-Buffer enable
  {
    if (!(rdp.flags & ZBUF_ENABLED))
    {
      rdp.flags |= ZBUF_ENABLED;
      rdp.update |= UPDATE_ZBUF_ENABLED;
    }
  }
  
  //Added by Gonetz
  if (rdp.cmd1 & 0x00010000)      // Fog enable
  {
    if (!(rdp.flags & FOG_ENABLED))
    {
      rdp.flags |= FOG_ENABLED;
      rdp.update |= UPDATE_FOG_ENABLED;
    }
  }
}

static void uc5_cleargeometrymode()
{
  FRDP("uc0:cleargeometrymode %08lx\n", rdp.cmd1);

  rdp.geom_mode &= (~rdp.cmd1);

  if (rdp.cmd1 & 0x00000001)  // Z-Buffer enable
  {
    if (rdp.flags & ZBUF_ENABLED)
    {
      rdp.flags ^= ZBUF_ENABLED;
      rdp.update |= UPDATE_ZBUF_ENABLED;
    }
  }
  //Added by Gonetz
  if (rdp.cmd1 & 0x00010000)      // Fog enable
  {
    if (rdp.flags & FOG_ENABLED)
    {
      rdp.flags ^= FOG_ENABLED;
      rdp.update |= UPDATE_FOG_ENABLED;
}
  }
}

