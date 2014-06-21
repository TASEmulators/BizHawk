/*
* Glide64 - Glide video plugin for Nintendo 64 emulators.
* Copyright (c) 2002  Dave2001
* Copyright (c) 2008  Günther <guenther.emu@freenet.de>
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
// Original author: Dave2001 (Dave2999@hotmail.com)
// Other authors: Gonetz, Gugaman
//
//****************************************************************

// Call this macro to automatically switch out of fullscreen, then break. :)
// useful for debugging fullscreen areas that can't otherwise be accessed
#ifndef RDP_H
#define RDP_H

#ifdef _WIN32
#include <windows.h>
#else // _WIN32
#include "winlnxdefs.h"
#endif // _WIN32
#include "glide.h"

//#ifdef GCC
#define max(a,b) ((a) > (b) ? (a) : (b))
#define min(a,b) ((a) < (b) ? (a) : (b))
//#endif

extern char out_buf[2048];

extern BOOL capture_screen;
extern char capture_path[256];

extern DWORD frame_count; // frame counter

#define MAX_CACHE   1024
#define MAX_TRI_CACHE 768 // this is actually # of vertices, not triangles
#define MAX_VTX     256

#define MAX_TMU     2

#define TEXMEM_2MB_EDGE 2097152

// Supported flags
#define SUP_TEXMIRROR 0x00000001

// Clipping flags
#define CLIP_XMAX 0x00000001
#define CLIP_XMIN 0x00000002
#define CLIP_YMAX 0x00000004
#define CLIP_YMIN 0x00000008
#define CLIP_ZMIN 0x00000010

// Flags
#define ZBUF_ENABLED  0x00000001
#define ZBUF_DECAL    0x00000002
#define ZBUF_COMPARE  0x00000004
#define ZBUF_UPDATE   0x00000008
#define ALPHA_COMPARE 0x00000010
#define FORCE_BL    0x00000020
#define CULL_FRONT    0x00001000  // * must be here
#define CULL_BACK   0x00002000  // * must be here
#define FOG_ENABLED   0x00010000 

#define CULLMASK    0x00003000
#define CULLSHIFT   12

// Update flags
#define UPDATE_ZBUF_ENABLED 0x00000001

#define UPDATE_TEXTURE    0x00000002  // \ Same thing!
#define UPDATE_COMBINE    0x00000002  // /

#define UPDATE_CULL_MODE  0x00000004
#define UPDATE_LIGHTS   0x00000010
#define UPDATE_BIASLEVEL  0x00000020
#define UPDATE_ALPHA_COMPARE  0x00000040
#define UPDATE_VIEWPORT   0x00000080
#define UPDATE_MULT_MAT   0x00000100
#define UPDATE_SCISSOR    0x00000200
#define UPDATE_FOG_ENABLED  0x00010000 

#define CMB_MULT    0x00000001
#define CMB_SET     0x00000002
#define CMB_SUB     0x00000004
#define CMB_ADD     0x00000008
#define CMB_A_MULT    0x00000010
#define CMB_A_SET   0x00000020
#define CMB_A_SUB   0x00000040
#define CMB_A_ADD   0x00000080
#define CMB_SETSHADE_SHADEALPHA 0x00000100
#define CMB_INTER   0x00000200
#define CMB_MULT_OWN_ALPHA  0x00000400
#define CMB_COL_SUB_OWN  0x00000800

#define uc(x) coord[x<<1]
#define vc(x) coord[(x<<1)+1]

// Vertex structure
typedef struct
{
  float x, y, z, q;
  float u0, v0, u1, v1;
  float coord[4];
  float w;
  WORD  flags;

  BYTE  b;  // These values are arranged like this so that *(DWORD*)(VERTEX+?) is
  BYTE  g;  // ARGB format that glide can use.
  BYTE  r;
  BYTE  a;

  float f; //fog

  float vec[3]; // normal vector

  float sx, sy, sz;
  float x_w, y_w, z_w, u0_w, v0_w, u1_w, v1_w, oow;
  BYTE  not_zclipped;
  BYTE  screen_translated;
  BYTE  shade_mods_allowed;
  BYTE  uv_fixed;
  DWORD uv_calculated;  // like crc

  float ou, ov;

  int   number;   // way to identify it
  int   scr_off, z_off; // off the screen?
} VERTEX;

// Clipping (scissors)
typedef struct {
  DWORD ul_x;
  DWORD ul_y;
  DWORD lr_x;
  DWORD lr_y;
} SCISSOR;

typedef struct {
  BYTE  card_id;

  DWORD res_x, scr_res_x;
  DWORD res_y, scr_res_y;
  DWORD res_data, res_data_org;

  BOOL  autodetect_ucode;
  DWORD ucode;

  BOOL  wireframe;
  int   wfmode;
  int   lodmode;
  BYTE  filtering;
  BOOL  fog;
  BOOL  buff_clear;
//  BOOL  clear_8;
  BOOL  vsync;
  BOOL  fast_crc;
  BYTE  swapmode;

  BOOL  logging;
  BOOL  elogging;
  BOOL  log_clear;
  BOOL  filter_cache;

  BOOL  unk_as_red;
  BOOL  log_unk;
  BOOL  unk_clear;

  BYTE  show_fps;

  BOOL  clock;
  BOOL  clock_24_hr;
   
  DWORD full_res;
  DWORD tex_filter;
  BOOL noditheredalpha;
  BOOL noglsl;
  BOOL FBO;
  BOOL disable_auxbuf;

  //Frame buffer emulation options
  BOOL  fb_read_always;
  BOOL  fb_read_alpha;
  BOOL  fb_smart;
  BOOL  fb_motionblur;
  BOOL  fb_hires;
  BOOL  fb_hires_buf_clear;
  BOOL  fb_depth_clear;
  BOOL  fb_depth_render;
  BOOL  fb_optimize_texrect;
  BOOL  fb_optimize_write;
  BOOL  fb_ignore_aux_copy;
  BOOL  fb_ignore_previous;
  BOOL  fb_get_info;

  // Special fixes
  int   offset_x, offset_y;
  int   scale_x, scale_y;
  BOOL  alt_tex_size;
  BOOL  use_sts1_only;
  BOOL  wrap_big_tex;
  BOOL  flame_corona; //hack for zeldas flame's corona
  int   fix_tex_coord;
  int   depth_bias;
  BOOL  soft_depth_compare; // use GR_CMP_LEQUAL instead of GR_CMP_LESS
  BOOL  increase_texrect_edge; // add 1 to lower right corner coordinates of texrect
  BOOL  decrease_fillrect_edge; // sub 1 from lower right corner coordinates of fillrect
  int   stipple_mode;  //used for dithered alpha emulation
  DWORD stipple_pattern; //used for dithered alpha emulation
  BOOL  force_microcheck; //check microcode each frame, for mixed F3DEX-S2DEX games

  BOOL  custom_ini;
  BOOL  hotkeys;

  //Special game hacks
  BOOL  force_depth_compare; //NFL Quarterback Club 99 and All-Star Baseball 2000
  BOOL  fillcolor_fix; //use first part of fillcolor in fillrects
  BOOL  cpu_write_hack; //show images writed directly by CPU
  BOOL  increase_primdepth;  //increase prim_depth value for texrects

  BOOL  zelda;    //zeldas hacks
  BOOL  bomberman64; //bomberman64 hacks
  BOOL  diddy;    //diddy kong racing
  BOOL  tonic;    //tonic trouble
  BOOL  PPL;      //pokemon puzzle league requires many special fixes
  BOOL  ASB;      //All-Star Baseball games
  BOOL  doraemon2;//Doraemon 2
  BOOL  invaders; //Space Invaders
  BOOL  BAR;      //Beetle Adventure Racing
  BOOL  ISS64;    //International Superstar Soccer 64
  BOOL  RE2;      //Resident Evil 2
  BOOL  nitro;    //WCW Nitro
  BOOL  chopper;  //Chopper Attack
  BOOL  yoshi;    // Yoshi Story
  BOOL  fzero;    // F-Zero
  BOOL  PM;       //Paper Mario
  BOOL  TGR;      //Top Gear Rally
  BOOL  TGR2;     //Top Gear Rally 2
  BOOL  KI;       //Killer Instinct
  BOOL  lego;     //LEGO Racers
} SETTINGS;

typedef struct
{
  BYTE fb_always; 
  BYTE fb_motionblur; 
  BYTE filtering; 
  BYTE corona; 
} HOTKEY_INFO;

// This structure is what is passed in by rdp:settextureimage
typedef struct {
  BYTE format;  // format: ARGB, IA, ...
  BYTE size;    // size: 4,8,16, or 32 bit
  WORD width;   // used in settextureimage
  DWORD addr;   // address in RDRAM to load the texture from
  BOOL set_by;  // 0-loadblock 1-loadtile
} TEXTURE_IMAGE;

// This structure is a tile descriptor (as used by rdp:settile and rdp:settilesize)
typedef struct
{
  // rdp:settile
  BYTE format;  // format: ARGB, IA, ...
  BYTE size;    // size: 4,8,16, or 32 bit
  WORD line;    // size of one row (x axis) in 64 bit words
  WORD t_mem;   // location in texture memory (in 64 bit words, max 512 (4MB))
  BYTE palette; // palette # to use
  BYTE clamp_t; // clamp or wrap (y axis)?
  BYTE mirror_t;  // mirroring on (y axis)?
  BYTE mask_t;  // mask to wrap around (ex: 5 would wrap around 32) (y axis)
  BYTE shift_t; // ??? (scaling)
  BYTE clamp_s; // clamp or wrap (x axis)?
  BYTE mirror_s;  // mirroring on (x axis)?
  BYTE mask_s;  // mask to wrap around (x axis)
  BYTE shift_s; // ??? (scaling)

  DWORD hack;   // any hacks needed

  // rdp:settilesize
  WORD ul_s;    // upper left s coordinate
  WORD ul_t;    // upper left t coordinate
  WORD lr_s;    // lower right s coordinate
  WORD lr_t;    // lower right t coordinate

  float f_ul_s;
  float f_ul_t;

  // these are set by loadtile
  WORD t_ul_s;    // upper left s coordinate
  WORD t_ul_t;    // upper left t coordinate
  WORD t_lr_s;    // lower right s coordinate
  WORD t_lr_t;    // lower right t coordinate

  DWORD width;
  DWORD height;

  // uc0:texture
  BYTE on;
  float s_scale;
  float t_scale;

  WORD org_s_scale;
  WORD org_t_scale;
} TILE;

// This structure forms the lookup table for cached textures
typedef struct {
  DWORD addr;     // address in RDRAM
  DWORD crc;      // CRC check
  DWORD palette;    // Palette #
  DWORD width;    // width
  DWORD height;   // height
  DWORD format;   // format
  DWORD size;     // size
  DWORD last_used;  // what frame # was this texture last used (used for replacing)

  DWORD line;

  DWORD flags;    // clamp/wrap/mirror flags

  DWORD realwidth;  // width of actual texture
  DWORD realheight; // height of actual texture
  DWORD lod;
  DWORD aspect;

  BOOL set_by;
  DWORD texrecting;

  float scale_x;    // texture scaling
  float scale_y;
  float scale;    // general scale to 256

  GrTexInfo t_info; // texture info (glide)
  DWORD tmem_addr;  // addres in texture memory (glide)

  int uses;   // 1 triangle that uses this texture

  int splits;   // number of splits
  int splitheight;

  float c_off;  // ul center texel offset (both x and y)
  float c_scl_x;  // scale to lower-right center-texel x
  float c_scl_y;  // scale to lower-right center-texel y

  DWORD mod, mod_color, mod_color1, mod_color2, mod_factor;

} CACHE_LUT;

// Lights
typedef struct {
  float r, g, b, a;       // color
  float dir_x, dir_y, dir_z;  // direction towards the light source
  float x, y, z, w;  // light position
  float ca, la, qa;
  DWORD nonblack;
  DWORD nonzero;
} LIGHT;

typedef enum {
  noise_none, 
  noise_combine, 
  noise_texture 
} NOISE_MODE;

typedef enum {
  ci_main,      //0, main color image
  ci_zimg,      //1, depth image
  ci_unknown,   //2, status is unknown
  ci_useless,   //3, status is unclear
  ci_old_copy,  //4, auxilary color image, copy of last color image from previous frame
  ci_copy,      //5, auxilary color image, copy of previous color image
  ci_copy_self, //6, main color image, it's content will be used to draw into itself
  ci_zcopy,     //7, auxilary color image, copy of depth image
  ci_aux,       //8, auxilary color image
  ci_aux_copy   //9, auxilary color image, partial copy of previous color image
} CI_STATUS;

// Frame buffers
typedef struct
{
    DWORD addr;   //color image address
    DWORD format; 
    DWORD size;   
    DWORD width;
    DWORD height;
    CI_STATUS status;
    int   changed;
} COLOR_IMAGE;

typedef struct
{
    GrChipID_t tmu;
    DWORD addr;  //address of color image
    DWORD end_addr; 
    DWORD tex_addr; //address in video memory
    DWORD width;    //width of color image
    DWORD height;   //height of color image
    WORD  format;   //format of color image
    BOOL  clear;  //flag. texture buffer must be cleared
    BOOL  drawn;  //flag. if equal to 1, this image was already drawn in current frame
    float scr_width; //width of rendered image
    float scr_height; //height of rendered image
    DWORD tex_width;  //width of texture buffer
    DWORD tex_height; //height of texture buffer
    int   tile;     //
    WORD  tile_uls; //shift from left bound of the texture
    WORD  tile_ult; //shift from top of the texture
    DWORD v_shift; //shift from top of the texture
    DWORD u_shift; //shift from left of the texture
    float u_scale; //used to map vertex u,v coordinates into hires texture 
    float v_scale; //used to map vertex u,v coordinates into hires texture 
    GrTexInfo info;
} HIRES_COLOR_IMAGE;

typedef struct
{
    GrChipID_t tmu;
    DWORD begin; //start of the block in video memory
    DWORD end;   //end of the block in video memory
    BYTE count;  //number of allocated texture buffers
    BOOL clear_allowed; //stack of buffers can be cleared
    HIRES_COLOR_IMAGE images[256]; 
} TEXTURE_BUFFER;

#define NUMTEXBUF 92

typedef struct
{
  float vi_width;
  float vi_height;

  BOOL window_changed;

  float offset_x, offset_y;

  float scale_x, scale_1024, scale_x_bak;
  float scale_y, scale_768, scale_y_bak;

  DWORD res_scale_x;
  DWORD res_scale_y;

  float view_scale[3];
  float view_trans[3];

  BOOL updatescreen;

  DWORD tri_n;  // triangle counter
  DWORD debug_n;

  // Program counter
  DWORD pc[10]; // DList PC stack
  DWORD pc_i;   // current PC index in the stack
  int dl_count; // number of instructions before returning

  // Segments
  DWORD segment[16];  // Segment pointer

  // Marks the end of DList execution (done in uc?:enddl)
  int halt;

  // Next command
  DWORD cmd0;
  DWORD cmd1;
  DWORD cmd2;
  DWORD cmd3;

  // Clipping
  SCISSOR scissor_o;
  SCISSOR scissor;

  // Colors
  DWORD fog_color;
  DWORD fill_color;
  DWORD prim_color;
  DWORD blend_color;
  DWORD env_color;
  DWORD prim_lodmin, prim_lodfrac;
  WORD prim_depth;
  BYTE K5;
  NOISE_MODE noise;

  float col[4];   // color multiplier
  float coladd[4];  // color add/subtract
  float shade_factor;

  float col_2[4];

  DWORD cmb_flags, cmb_flags_2;

  // othermode_l flags
  int acmp; // 0 = none, 1 = threshold, 2 = dither
  int zsrc; // 0 = pixel, 1 = prim

  // Clipping
  int clip;     // clipping flags
  VERTEX vtx1[256]; // copy vertex buffer #1 (used for clipping)
  VERTEX vtx2[256]; // copy vertex buffer #2
  VERTEX *vtxbuf;   // current vertex buffer (reset to vtx, used to determine current
            //   vertex buffer)
  VERTEX *vtxbuf2;
  int n_global;   // Used to pass the number of vertices from clip_z to clip_tri

  int vtx_buffer;

  // Matrices
  __declspec( align(16) ) float model[4][4];
  __declspec( align(16) ) float proj[4][4];
  __declspec( align(16) ) float combined[4][4];
  __declspec( align(16) ) float dkrproj[3][4][4];

  __declspec( align(16) ) float model_stack[32][4][4];  // 32 deep, will warn if overflow
  int model_i;          // index in the model matrix stack
  int model_stack_size;

  // Textures
  TEXTURE_IMAGE timg;       // 1 for each tmem address
  TILE tiles[8];          // 8 tile descriptors
  BYTE tmem[4096];        // 4k tmem
  DWORD addr[512];        // 512 addresses (used to determine address loaded from)

  int     cur_tile;   // current tile
  int     mipmap_level;
  int     last_tile;   // last tile set
  int     last_tile_size;   // last tile size set

  CACHE_LUT cache[MAX_TMU][MAX_CACHE];
  CACHE_LUT *cur_cache[2];
  DWORD   cur_cache_n[2];
  int     n_cached[MAX_TMU];
  DWORD   tmem_ptr[MAX_TMU];

  int     t0, t1;
  int     best_tex; // if no 2-tmus, which texture? (0 or 1)
  int     tex;
  int     filter_mode;

  // Texture palette
  WORD pal_8[256];
  DWORD pal_8_crc[16];
  DWORD pal_256_crc;
  BYTE tlut_mode;
  BOOL LOD_en;

  // Lighting
  DWORD num_lights;
  LIGHT light[12];
  float light_vector[12][3];
  float lookat[2][3];
  BOOL  use_lookat;

  // Combine modes
  DWORD cycle1, cycle2, cycle_mode;
  BYTE c_a0, c_b0, c_c0, c_d0, c_Aa0, c_Ab0, c_Ac0, c_Ad0;
  BYTE c_a1, c_b1, c_c1, c_d1, c_Aa1, c_Ab1, c_Ac1, c_Ad1;

  BYTE fbl_a0, fbl_b0, fbl_c0, fbl_d0;
  BYTE fbl_a1, fbl_b1, fbl_c1, fbl_d1;

  BYTE uncombined;  // which is uncombined: 0x01=color 0x02=alpha 0x03=both

//  float YUV_C0, YUV_C1, YUV_C2, YUV_C3, YUV_C4; //YUV textures conversion coefficients
  BOOL yuv_image;
  float yuv_ul_x, yuv_ul_y, yuv_lr_x, yuv_lr_y;
  DWORD yuv_im_begin;

  // What needs updating
  DWORD update;
  DWORD flags;

  BOOL first;

  // Vertices
  VERTEX vtx[MAX_VTX];
  int v0, vn;

  DWORD tex_ctr;    // same as above, incremented every time textures are updated

  BOOL allow_combine; // allow combine updating?

  BOOL s2dex_tex_loaded;

  // Debug stuff
  DWORD rm; // use othermode_l instead, this just as a check for changes
  DWORD render_mode_changed;
  DWORD geom_mode;

  DWORD othermode_h;
  DWORD othermode_l;

  // used to check if in texrect while loading texture
  DWORD texrecting;

  //frame buffer related slots. Added by Gonetz
  COLOR_IMAGE frame_buffers[NUMTEXBUF+2];
  DWORD cimg, ocimg, zimg, tmpzimg, vi_org_reg;
  COLOR_IMAGE maincimg[2];
  DWORD last_drawn_ci_addr;
  DWORD main_ci, main_ci_end, main_ci_bg, main_ci_last_tex_addr, zimg_end, last_bg;
  DWORD ci_width, ci_height, ci_size, ci_end;
  DWORD zi_width;
  int zi_lrx, zi_lry;
  BYTE  ci_count, num_of_ci, main_ci_index, copy_ci_index;
  int swap_ci_index, black_ci_index;
  DWORD ci_upper_bound, ci_lower_bound;
  BOOL  motionblur, fb_drawn, fb_drawn_front, read_previous_ci, read_whole_frame;
  CI_STATUS ci_status;
  TEXTURE_BUFFER texbufs[2];
  HIRES_COLOR_IMAGE * cur_image;  //image currently being drawn
  HIRES_COLOR_IMAGE * hires_tex;  //image, which corresponds to currently selected texture
  BYTE  cur_tex_buf;
  BYTE  acc_tex_buf;
  BOOL skip_drawing; //rendering is not required. used for frame buffer emulation

  //fog related slots. Added by Gonetz
  float fog_multiplier, fog_offset;
  BOOL fog_coord_enabled;

} RDP;


void SetWireframeCol ();
void ChangeSize ();

extern RDP rdp;
extern SETTINGS settings;
extern HOTKEY_INFO hotkey_info;

extern GrTexInfo  fontTex;
extern GrTexInfo  cursorTex;
extern DWORD    offset_font;
extern DWORD    offset_cursor;
extern DWORD    offset_textures;
extern DWORD    offset_texbuf1;

extern BOOL     ucode_error_report;

// RDP functions
void rdp_reset ();

// global strings
extern const char *ACmp[4];
extern const char *Mode0[16];
extern const char *Mode1[16];
extern const char *Mode2[32];
extern const char *Mode3[8];
extern const char *Alpha0[8];
extern const char *Alpha2[8];
extern const char *str_zs[2];
extern const char *str_yn[2];
extern const char *str_offon[2];
extern const char *str_cull[4];
extern const char *str_format[8];
extern const char *str_size[4];
extern const char *str_cm[4];
extern const char *str_filter[3];
extern const char *str_tlut[4];
extern const char *CIStatus[10];

#define Alpha1 Alpha0
#define Alpha3 Alpha0

#define FBL_D_1 2
#define FBL_D_0 3


// Convert from u0/v0/u1/v1 to the real coordinates without regard to tmu
__inline void ConvertCoordsKeep (VERTEX *v, int n)
{
  for (int i=0; i<n; i++)
  {
    v[i].uc(0) = v[i].u0;
    v[i].vc(0) = v[i].v0;
    v[i].uc(1) = v[i].u1;
    v[i].vc(1) = v[i].v1;
  }
}

// Convert from u0/v0/u1/v1 to the real coordinates based on the tmu they are on
__inline void ConvertCoordsConvert (VERTEX *v, int n)
{

  if (rdp.hires_tex && rdp.tex != 3)
  {
    for (int i=0; i<n; i++)
    {
      v[i].u1 = v[i].u0;
      v[i].v1 = v[i].v0;
    }
  }

//  float z;
  for (int i=0; i<n; i++)
  {
    v[i].uc(rdp.t0) = v[i].u0;
    v[i].vc(rdp.t0) = v[i].v0;
    v[i].uc(rdp.t1) = v[i].u1;
    v[i].vc(rdp.t1) = v[i].v1;
  }
}

__inline void AllowShadeMods (VERTEX *v, int n)
{
  for (int i=0; i<n; i++)
  {
    v[i].shade_mods_allowed = 1;
  }
}

__inline float ScaleZ(float z)
{
//  z *= 2.0f;
//  if (z > 65535.0f) return 65535.0f;
//  return (z / 65535.0f) * z;
//  if (z  < 4096.0f) return z * 0.25f;
//  z = (z / 16384.0f) * z;
  z *= 1.9f;
  if (z > 65534.0f) return 65534.0f;
  return z;
}

__inline void CalculateFog (VERTEX *v)
{
    if (rdp.flags & FOG_ENABLED) 
    {
        v->f = min(255.0f, max(0.0f, v->z_w * rdp.fog_multiplier + rdp.fog_offset));    
        v->a = (BYTE)v->f;
    }
    else
    {
      v->f = 1.0f;
    }
}

void newSwapBuffers();
extern BOOL SwapOK;

// ** utility functions
void load_palette (DWORD addr, WORD start, WORD count);

#endif  // ifndef RDP_H

