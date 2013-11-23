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

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_config.h"
#include "m64p_vidext.h"
#include "3dmath.h"
#include "Util.h"
#include "Debugger.h"
#include "Combine.h"
#include "Util.h"
#include "Ini.h"
#include "Config.h"
#include "Tmem.h"
#include "TexCache.h"
#include "TexCache.h"
#include "TexBuffer.h"
#include "CRC.h"
#include "rdp.h"

#ifndef _WIN32
#include <sys/time.h>
#endif // _WIN32

char out_buf[2048];

DWORD frame_count;  // frame counter

BOOL ucode_error_report = TRUE;
int wrong_tile = -1;

int drawFlag = 1;	// draw flag for rendering callback

#if defined(__GNUC__)
  #define bswap32(x) __builtin_bswap32(x)
#elif defined(_MSC_VER) && (defined(_M_IX86) || defined(_M_X64))
  #include <stdlib.h>
  #define bswap32(x) _byteswap_ulong(x)
#else
static inline uint32_t bswap32(uint32_t val)
{
        return (((val & 0xff000000) >> 24) |
                ((val & 0x00ff0000) >>  8) |
                ((val & 0x0000ff00) <<  8) |
                ((val & 0x000000ff) << 24));
}
#endif

// global strings
const char *ACmp[4] = { "NONE", "THRESHOLD", "UNKNOWN", "DITHER" };

const char *Mode0[16] = { "COMBINED",    "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "1",        "NOISE",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0" };
const char *Mode1[16] = { "COMBINED",    "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "CENTER",     "K4",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0" };
const char *Mode2[32] = { "COMBINED",    "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "SCALE",      "COMBINED_ALPHA",
            "T0_ALPHA",     "T1_ALPHA",
            "PRIM_ALPHA",   "SHADE_ALPHA",
            "ENV_ALPHA",    "LOD_FRACTION",
            "PRIM_LODFRAC",   "K5",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0",
            "0",        "0" };
const char *Mode3[8] = { "COMBINED",    "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "1",        "0" };

const char *Alpha0[8] = { "COMBINED",   "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "1",        "0" };
const char *Alpha2[8] = { "LOD_FRACTION", "TEXEL0",
            "TEXEL1",     "PRIMITIVE",
            "SHADE",      "ENVIORNMENT",
            "PRIM_LODFRAC",   "0" };

//FIXME:unused?
//const char *FBLa[] = { "G_BL_CLR_IN", "G_BL_CLR_MEM", "G_BL_CLR_BL", "G_BL_CLR_FOG" };
//const char *FBLb[] = { "G_BL_A_IN", "G_BL_A_FOG", "G_BL_A_SHADE", "G_BL_0" };
//const char *FBLc[] = { "G_BL_CLR_IN", "G_BL_CLR_MEM", "G_BL_CLR_BL", "G_BL_CLR_FOG"};
//const char *FBLd[] = { "G_BL_1MA", "G_BL_A_MEM", "G_BL_1", "G_BL_0" };

const char *str_zs[2] = { "G_ZS_PIXEL", "G_ZS_PRIM" };

const char *str_yn[2] = { "NO", "YES" };
const char *str_offon[2] = { "OFF", "ON" };

const char *str_cull[4] = { "DISABLE", "FRONT", "BACK", "BOTH" };

// I=intensity probably
const char *str_format[8]   = { "RGBA", "YUV", "CI", "IA", "I", "?", "?", "?" };
const char *str_size[4]     = { "4bit", "8bit", "16bit", "32bit" };
const char *str_cm[4]       = { "WRAP/NO CLAMP", "MIRROR/NO CLAMP", "WRAP/CLAMP", "MIRROR/CLAMP" };

//const char *str_lod[]    = { "1", "2", "4", "8", "16", "32", "64", "128", "256" };
//const char *str_aspect[] = { "1x8", "1x4", "1x2", "1x1", "2x1", "4x1", "8x1" };

const char *str_filter[3] = { "Point Sampled", "Average (box)", "Bilinear" };

const char *str_tlut[4]   = { "TT_NONE", "TT_UNKNOWN", "TT_RGBA_16", "TT_IA_16" };

const char *CIStatus[10] = { "ci_main", "ci_zimg", "ci_unknown",  "ci_useless",
                           "ci_old_copy", "ci_copy", "ci_copy_self",
                           "ci_zcopy", "ci_aux", "ci_aux_copy" };

typedef struct
{
    int      ucode;
    int      crc;
} UcodeData;

static UcodeData UcodeList[] = 
{
    {0, 0x006bd77f},
    {2, 0x03044b84},
    {2, 0x030f4b84},
    {1, 0x05165579},
    {1, 0x05777c62},
    {1, 0x057e7c62},
    {1, 0x07200895},
    {2, 0x0bf36d36},
    {-1, 0x0d7bbffb}, 
    {5, 0x0d7cbffb},
    {2, 0x0ff79527},
    {-1, 0x0ff795bf}, 
    {1, 0x1118b3e0},
    {2, 0x168e9cd5},
    {2, 0x1a1e18a0},
    {2, 0x1a1e1920},
    {2, 0x1a62dbaf},
    {2, 0x1a62dc2f},
    {1, 0x1de712ff},
    {6, 0x1ea9e30f},
    {2, 0x21f91834},
    {2, 0x21f91874},
    {2, 0x22099872},
    {1, 0x24cd885b},
    {1, 0x26a7879a},
    {6, 0x299d5072},
    {2, 0x2b291027},
    {6, 0x2b5a89c2},
    {1, 0x2c7975d6},
    {2, 0x2f71d1d5},
    {2, 0x2f7dd1d5},
    {1, 0x327b933d},
    {1, 0x339872a6},
    {2, 0x377359b6},
    {0, 0x3a1c2b34},
    {0, 0x3a1cbac3},
    {0, 0x3f7247fb},
    {1, 0x3ff1a4ca},
    {0, 0x4165e1fd},
    {1, 0x4340ac9b},
    {1, 0x440cfad6},
    {7, 0x47d46e86},
    {2, 0x485abff2},
    {1, 0x4fe6df78},
    {0, 0x5182f610},
    {1, 0x5257cd2a},
    {1, 0x5414030c},
    {1, 0x5414030d},
    {1, 0x559ff7d4},
    {4, 0x5b5d36e3},
    {3, 0x5b5d3763},
    {0, 0x5d1d6f53},
    {2, 0x5d3099f1},
    {1, 0x5df1408c},
    {1, 0x5ef4e34a},
    {1, 0x6075e9eb},
    {1, 0x60c1dcc4},
    {2, 0x6124a508},
    {2, 0x630a61fb},
    {5, 0x63be08b1},
    {5, 0x63be08b3},
    {1, 0x64ed27e5},
    {2, 0x65201989},
    {2, 0x65201a09},
    {1, 0x66c0b10a},
    {2, 0x679e1205},
    {6, 0x6bb745c9},
    {2, 0x6d8f8f8a},
    {0, 0x6e4d50af},
    {1, 0x6eaa1da8},
    {1, 0x72a4f34e},
    {1, 0x73999a23},
    {6, 0x74af0a74},
    {2, 0x753be4a5},
    {6, 0x794c3e28},
    {1, 0x7df75834},
    {1, 0x7f2d0a2e},
    {1, 0x82f48073},
    {1, 0x841ce10f},
    {-1, 0x844b55b5},
    {1, 0x863e1ca7},
    {-1, 0x86b1593e},
    {1, 0x8805ffea},
    {1, 0x8d5735b2},
    {1, 0x8d5735b3},
    {-1, 0x8ec3e124},
    {2, 0x93d11f7b},
    {2, 0x93d11ffb},
    {2, 0x93d1ff7b},
    {2, 0x9551177b},
    {2, 0x955117fb},
    {2, 0x95cd0062},
    {1, 0x97d1b58a},
    {2, 0xa2d0f88e},
    {1, 0xa346a5cc},
    {2, 0xaa86cb1d},
    {2, 0xaae4a5b9},
    {2, 0xad0a6292},
    {2, 0xad0a6312},
    {0, 0xae08d5b9},
    {1, 0xb1821ed3},
    {1, 0xb4577b9c},
    {0, 0xb54e7f93},
    {2, 0xb62f900f},
    {2, 0xba65ea1e},
    {8, 0xba86cb1d},
    {0, 0xbc03e969},
    {2, 0xbc45382e},
    {1, 0xbe78677c},
    {1, 0xbed8b069},
    {1, 0xc3704e41},
    {1, 0xc46dbc3d},
    {1, 0xc99a4c6c},
    {2, 0xc901ce73},
    {2, 0xc901cef3},
    {2, 0xcb8c9b6c},
    {1, 0xcee7920f},
    {2, 0xcfa35a45},
    {1, 0xd1663234},
    {6, 0xd20dedbf},
    {1, 0xd2a9f59c},
    {1, 0xd41db5f7},
    {0, 0xd5604971},
    {1, 0xd57049a5},
    {-1, 0xd5c4dc96},
    {0, 0xd5d68b1f},
    {1, 0xd802ec04},
    {2, 0xda13ab96},
    {2, 0xde7d67d4},
    {2, 0xe1290fa2},
    {0, 0xe41ec47e},
    {2, 0xe65cb4ad},
    {1, 0xe89c2b92},
    {1, 0xe9231df2},
    {1, 0xec040469},
    {1, 0xee47381b},
    {1, 0xef54ee35},
    {-1, 0xf9893f70},
    {1, 0xfb816260},
    {-1, 0xff372492}
};

// ZIGGY
// depth save/restore variables
// 0 : normal mode
// 1 : writing in normal depth buffer
// 2 : writing in alternate depth buffer
static int render_depth_mode;

// ** RDP graphics functions **
static void undef();
static void spnoop();

static void rdp_noop();
static void rdp_texrect();
//static void rdp_texrectflip();
static void rdp_loadsync();
static void rdp_pipesync();
static void rdp_tilesync();
static void rdp_fullsync();
static void rdp_setkeygb();
static void rdp_setkeyr();
static void rdp_setconvert();
static void rdp_setscissor();
static void rdp_setprimdepth();
static void rdp_setothermode();
static void rdp_loadtlut();
static void rdp_settilesize();
static void rdp_loadblock();
static void rdp_loadtile();
static void rdp_settile();
static void rdp_fillrect();
static void rdp_setfillcolor();
static void rdp_setfogcolor();
static void rdp_setblendcolor();
static void rdp_setprimcolor();
static void rdp_setenvcolor();
static void rdp_setcombine();
static void rdp_settextureimage();
static void rdp_setdepthimage();
static void rdp_setcolorimage();
static void rdp_trifill();
static void rdp_trishade();
static void rdp_tritxtr();
static void rdp_trishadetxtr();
static void rdp_trifillz();
static void rdp_trishadez();
static void rdp_tritxtrz();
static void rdp_trishadetxtrz();

static void rsp_reserved0();
static void rsp_reserved1();
static void rsp_reserved2();
static void rsp_reserved3();

static void ys_memrect();

BYTE microcode[4096];
DWORD uc_crc;
void microcheck ();

// ** UCODE FUNCTIONS **
#include "Ucode00.h"
#include "ucode01.h"
#include "ucode02.h"
#include "ucode03.h"
#include "ucode04.h"
#include "ucode05.h"
#include "ucode06.h"
#include "ucode07.h"
#include "ucode08.h"
#include "ucode.h"

static BOOL reset = 0;
static int old_ucode = -1;

// rdp_reset - resets the RDP_E
void rdp_reset ()
{
    reset = 1;

    rdp.model_i = 0;

    rdp.n_cached[0] = 0;
    rdp.n_cached[1] = 0;
    rdp.cur_cache[0] = NULL;
    rdp.cur_cache[1] = NULL;
  /*
    rdp.tmem_ptr[0] = offset_textures;
    rdp.tmem_ptr[1] = offset_textures;
    if (grTextureBufferExt)
      rdp.tmem_ptr[1] = TEXMEM_2MB_EDGE * 2;
  */
    rdp.c_a0  = 0;
    rdp.c_b0  = 0;
    rdp.c_c0  = 0;
    rdp.c_d0  = 0;
    rdp.c_Aa0 = 0;
    rdp.c_Ab0 = 0;
    rdp.c_Ac0 = 0;
    rdp.c_Ad0 = 0;

    rdp.c_a1  = 0;
    rdp.c_b1  = 0;
    rdp.c_c1  = 0;
    rdp.c_d1  = 0;
    rdp.c_Aa1 = 0;
    rdp.c_Ab1 = 0;
    rdp.c_Ac1 = 0;
    rdp.c_Ad1 = 0;

    // Clear the palette CRC
    int i;
    for (i=0; i<16; i++)
        rdp.pal_8_crc[i] = 0;

    // Clear the palettes
    for (i=0; i<256; i++)
        rdp.pal_8[i] = 0;

    rdp.tlut_mode = 0;

    // Clear all segments ** VERY IMPORTANT FOR ZELDA **
    for (i=0; i<16; i++)
        rdp.segment[i] = 0;

    for (i=0; i<512; i++)
        rdp.addr[i] = 0;

    // set all vertex numbers
    for (i=0; i<MAX_VTX; i++)
        rdp.vtx[i].number = i;

    rdp.scissor_o.ul_x = 0;
    rdp.scissor_o.ul_y = 0;
    rdp.scissor_o.lr_x = 320;
    rdp.scissor_o.lr_y = 240;
    rdp.num_lights = 0;
  rdp.lookat[0][0] = rdp.lookat[1][1] = 1.0f;
  rdp.lookat[0][1] = rdp.lookat[0][2] = rdp.lookat[1][0] = rdp.lookat[1][2] = 0.0f;
    rdp.texrecting = 0;
    rdp.rm = 0;
    rdp.render_mode_changed = 0;
    rdp.othermode_h = 0;
    rdp.othermode_l = 0;

    rdp.tex_ctr = 0;

    rdp.tex = 0;

    rdp.cimg = 0;
    rdp.ocimg = 0;
    rdp.zimg = 0;
    rdp.ci_width = 0;
    rdp.cycle_mode = 2;

    rdp.allow_combine = 1;

  rdp.fog_coord_enabled = FALSE;
    rdp.skip_drawing = FALSE;

    memset(rdp.frame_buffers, 0, sizeof(rdp.frame_buffers));
    rdp.main_ci_index = 0;
  rdp.maincimg[0].addr = rdp.maincimg[1].addr = rdp.last_drawn_ci_addr = 0x7FFFFFFF;
    rdp.read_previous_ci = FALSE;
    rdp.yuv_ul_x = rdp.yuv_ul_y = rdp.yuv_lr_x = rdp.yuv_lr_y = 0;
    rdp.yuv_im_begin = 0x00FFFFFF;
    rdp.yuv_image = FALSE;
    rdp.cur_tex_buf = 0;
  rdp.acc_tex_buf = 0;
    rdp.cur_image = 0;
    rdp.hires_tex = 0;

    hotkey_info.fb_always = 0;
    hotkey_info.fb_motionblur = (settings.buff_clear == 0)?0:60;
    hotkey_info.filtering = hotkey_info.fb_motionblur;
    hotkey_info.corona = hotkey_info.fb_motionblur;
#ifdef _WIN32
    GetAsyncKeyState (VK_BACK);
    GetAsyncKeyState(0x42);
    GetAsyncKeyState(0x56);
    GetAsyncKeyState(0x43);
#endif // _WIN32
    for (i = 0; i < num_tmu; i++)
      rdp.texbufs[i].count = 0;
  rdp.vi_org_reg = *gfx.VI_ORIGIN_REG;
  rdp.view_scale[0] = 160.0f * rdp.scale_x;
  rdp.view_scale[1] = -120.0f * rdp.scale_y;
  rdp.view_trans[0] = 160.0f * rdp.scale_x;
  rdp.view_trans[1] = 120.0f * rdp.scale_y;
  rdp.view_scale[2] = 32.0f * 511.0f;
  rdp.view_trans[2] = 32.0f * 511.0f;
}

# define PCEndian
# ifdef PCEndian
#  define ByteEndian(address)  (address^3)
#  define WordEndian(address)  (address^2)
# endif
# define _Read8Endian(array, address) (*((BYTE *)(array+ByteEndian(address))))
__inline static DWORD searchrdram(const char *ct)
{
    DWORD pos, pos2;
    const char *t;
    t = ct;
    for (pos=0; pos<0x400000; pos++) {
        for (pos2=pos, t=ct; *ct != 0; t++, pos2++) {
            if (_Read8Endian(gfx.RDRAM, pos2) != *t)
                break;
            else
                if (*(t + 1) == 0)
                    return pos;
        }
    }
    return 0;
}

int LookupUcode (int crc)
{
	for (int i = 0; i < sizeof(UcodeList)/sizeof(UcodeData); i++)
    {
        if (crc == UcodeList[i].crc)
        {
            return UcodeList[i].ucode;
        }
    }

    return -2;
}

void microcheck ()
{
    DWORD i;
    uc_crc = 0;

    // Check first 3k of ucode, because the last 1k sometimes contains trash
    for (i=0; i<3072>>2; i++)
    {
        uc_crc += ((DWORD*)microcode)[i];
    }

    FRDP_E ("crc: %08lx\n", uc_crc);

#ifdef LOG_UCODE
    std::ofstream ucf;
    ucf.open ("ucode.txt", ios::out | ios::binary);
    char d;
    for (i=0; i<0x400000; i++)
    {
        d = ((char*)gfx.RDRAM)[i^3];
        ucf.write (&d, 1);
    }
    ucf.close ();
#endif

    char str[9];
  sprintf (str, "%08lx", (unsigned long)uc_crc);

    FRDP("ucode = %s\n", str);
	int uc = LookupUcode(uc_crc);
    WriteLog(M64MSG_INFO, "ucode = %d\n", uc);
    if (uc == -2 && ucode_error_report)
    {
        Config_Open();
        settings.ucode = Config_ReadInt ("ucode", "Force microcode", 0, FALSE, FALSE);

        ReleaseGfx ();
        WriteLog(M64MSG_ERROR, "Error: uCode crc not found in INI, using currently selected uCode\n\n%08lx", (unsigned long)uc_crc);

        ucode_error_report = FALSE; // don't report any more ucode errors from this game
    }
    else if (uc == -1 && ucode_error_report)
    {
        Config_Open();
        settings.ucode = Config_ReadInt ("ucode", "Force microcode", 0, FALSE, FALSE);

        ReleaseGfx ();
        WriteLog(M64MSG_ERROR, "Error: Unsupported uCode!\n\ncrc: %08lx", (unsigned long)uc_crc);

        ucode_error_report = FALSE; // don't report any more ucode errors from this game
    }
    else
    {
        old_ucode = settings.ucode;
        settings.ucode = uc;
        FRDP("microcheck: old ucode: %d,  new ucode: %d\n", old_ucode, uc);
    }
}

void drawNoFullscreenMessage()
{
    LOG ("drawNoFullscreenMessage ()\n");
}

static WORD yuv_to_rgb(BYTE y, BYTE u, BYTE v)
{
    float r = y + (1.370705f * (v-128));
    float g = y - (0.698001f * (v-128)) - (0.337633f * (u-128));
    float b = y + (1.732446f * (u-128));
    r *= 0.125f;
    g *= 0.125f;
    b *= 0.125f;
    //clipping the result
    if (r > 32) r = 32;
    if (g > 32) g = 32;
    if (b > 32) b = 32;
    if (r < 0) r = 0;
    if (g < 0) g = 0;
    if (b < 0) b = 0;

    WORD c = (WORD)(((WORD)(r) << 11) |
              ((WORD)(g) << 6) |
              ((WORD)(b) << 1) | 1);
    return c;
}

static void DrawYUVImageToFrameBuffer()
{
  WORD width = (WORD)(rdp.yuv_lr_x - rdp.yuv_ul_x);
  WORD height = (WORD)(rdp.yuv_lr_y - rdp.yuv_ul_y);
  DWORD * mb = (DWORD*)(gfx.RDRAM+rdp.yuv_im_begin); //pointer to the first macro block
  WORD * cimg = (WORD*)(gfx.RDRAM+rdp.cimg);
  //yuv macro block contains 16x16 texture. we need to put it in the proper place inside cimg
  for (WORD y = 0; y < height; y+=16)
  {
    for (WORD x = 0; x < width; x+=16)
    {
       WORD *dst = cimg + x + y * rdp.ci_width;
       for (WORD h = 0; h < 16; h++)
       {
          for (WORD w = 0; w < 8; w++)
          {
            DWORD t = *(mb++); //each DWORD contains 2 pixels
            if ((x < rdp.ci_width) && (y < rdp.ci_height)) //clipping. texture image may be larger than color image
            {
                BYTE y0 = (BYTE)t&0xFF;
                BYTE v  = (BYTE)(t>>8)&0xFF;
                BYTE y1 = (BYTE)(t>>16)&0xFF;
                BYTE u  = (BYTE)(t>>24)&0xFF;
                *(dst++) = yuv_to_rgb(y0, u, v);
                *(dst++) = yuv_to_rgb(y1, u, v);
            }
          }
          dst += rdp.ci_width - 16;
       }
       mb += 64;  //macro block is 768 bytes long, last 256 bytes are useless
    }
  }
}

static DWORD d_ul_x, d_ul_y, d_lr_x, d_lr_y;

typedef struct {
  int ul_x, ul_y, lr_x, lr_y;
} FB_PART;

static void DrawPart(int scr_ul_x, int scr_ul_y, int prt_ul_x, int prt_ul_y, int width, int height, float scale_x, float scale_y)
{
  WORD * dst = new WORD[width*height];
  DWORD shift = ((d_ul_y+prt_ul_y) * rdp.ci_width + d_ul_x + prt_ul_x) << 1;
  WORD * src = (WORD*)(gfx.RDRAM+rdp.cimg+shift);
  WORD c;
  for (int y=0; y < height; y++)
  {
    for (int x=0; x < width; x++)
    {
      c = src[(int(x*scale_x)+int(y*scale_y)*rdp.ci_width)^1];
        dst[x+y*width] = c?((c >> 1) | 0x8000):0;
    }
  }

  grLfbWriteRegion(GR_BUFFER_BACKBUFFER,
    scr_ul_x,
    scr_ul_y,
    GR_LFB_SRC_FMT_1555,
    width,
    height,
    FXTRUE,
    width<<1,
    dst);
  delete[] dst;
}

static void DrawFrameBufferToScreen()
{
  FRDP("DrawFrameBufferToScreen. cimg: %08lx, ul_x: %d, uly: %d, lr_x: %d, lr_y: %d\n", rdp.cimg, d_ul_x, d_ul_y, d_lr_x, d_lr_y);
  if (!fullscreen)
    return;
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
  grAlphaBlendFunction( GR_BLEND_SRC_ALPHA,
    GR_BLEND_ONE_MINUS_SRC_ALPHA,
    GR_BLEND_ONE,
    GR_BLEND_ZERO);
  rdp.update |= UPDATE_COMBINE;

  float scale_x_dst = (float)settings.scr_res_x / rdp.vi_width;//(float)max(rdp.frame_buffers[rdp.main_ci_index].width, rdp.ci_width);
  float scale_y_dst = (float)settings.scr_res_y / rdp.vi_height;//(float)max(rdp.frame_buffers[rdp.main_ci_index].height, rdp.ci_lower_bound);
  float scale_x_src = (float)rdp.vi_width / (float)settings.scr_res_x;//(float)max(rdp.frame_buffers[rdp.main_ci_index].width, rdp.ci_width);
  float scale_y_src = (float)rdp.vi_height / (float)settings.scr_res_y;//(float)max(rdp.frame_buffers[rdp.main_ci_index].height, rdp.ci_lower_bound);
  int src_width = d_lr_x - d_ul_x + 1;
  int src_height = d_lr_y - d_ul_y + 1;
  int dst_width, dst_height, ul_x, ul_y;

  if (!settings.fb_optimize_write || ((src_width < 33) && (src_height < 33)))
  {
    dst_width = int(src_width*scale_x_dst);
    dst_height = int(src_height*scale_y_dst);
    ul_x = int(d_ul_x*scale_x_dst);
    ul_y = int(d_ul_y*scale_y_dst);
    DrawPart(ul_x, ul_y, 0, 0, dst_width, dst_height, scale_x_src, scale_y_src);
    memset(gfx.RDRAM+rdp.cimg, 0, rdp.ci_width*rdp.ci_height*rdp.ci_size);
    return;
  }

  FB_PART parts[8];
  int p;
  for (p = 0; p < 8; p++)
  {
    parts[p].lr_x = parts[p].lr_y = 0;
    parts[p].ul_x = parts[p].ul_y = 0xFFFF;
  }

  int num_of_parts = 0;
  int cur_part = 0;
  int most_left = d_ul_x;
  int most_right = d_lr_x;
  DWORD shift = (d_ul_y * rdp.ci_width + d_ul_x) << 1;
  WORD * src = (WORD*)(gfx.RDRAM+rdp.cimg+shift);
  for (int h = 0; h < src_height; h++)
  {
    cur_part = 0;
    int w = 0;
    while (w < src_width)
    {
      while (w < src_width)
      {
        if (src[(w+h*rdp.ci_width)^1] == 0)
          w++;
        else
          break;
      }
      if (w == src_width)
        break;
      if (num_of_parts == 0) //first part
      {
        parts[0].ul_x = w;
        most_left = w;
        parts[0].ul_y = h;
        cur_part = 0;
      }
      else if (w < most_left - 2) //new part
      {
        parts[num_of_parts].ul_x = w;
        most_left = w;
        parts[num_of_parts].ul_y = h;
        cur_part = num_of_parts;
        num_of_parts++;
      }
      else if (w > most_right + 2) //new part
      {
        parts[num_of_parts].ul_x = w;
        most_right = w;
        parts[num_of_parts].ul_y = h;
        cur_part = num_of_parts;
        num_of_parts++;
      }
      else
      {
        for (p = 0; p < num_of_parts; p++)
        {
          if ((w >  parts[p].ul_x - 2) && (w < parts[p].lr_x+2))
          {
            if (w < parts[p].ul_x) parts[p].ul_x = w;
            break;
          }
        }
        cur_part = p;
      }
      while (w < src_width)
      {
        if (src[(w+h*rdp.ci_width)^1] != 0)
          w++;
        else
          break;
      }
      if (num_of_parts == 0) //first part
      {
        parts[0].lr_x = w;
        most_right = w;
        num_of_parts++;
      }
      else
      {
        if (parts[cur_part].lr_x < w) parts[cur_part].lr_x = w;
        if (most_right < w) most_right = w;
        parts[cur_part].lr_y = h;
      }
    }
  }
  /*
  for (p = 0; p < num_of_parts; p++)
  {
   FRDP("part#%d  ul_x: %d, ul_y: %d, lr_x: %d, lr_y: %d\n", p, parts[p].ul_x, parts[p].ul_y, parts[p].lr_x, parts[p].lr_y);
  }
  */
  for (p = 0; p < num_of_parts; p++)
  {
    dst_width = int((parts[p].lr_x-parts[p].ul_x + 1)*scale_x_dst);
    dst_height = int((parts[p].lr_y-parts[p].ul_y + 1)*scale_y_dst);
    ul_x = int((d_ul_x+parts[p].ul_x)*scale_x_dst);
    ul_y = int((d_ul_y+parts[p].ul_y)*scale_y_dst);
    DrawPart(ul_x, ul_y, parts[p].ul_x, parts[p].ul_y, dst_width, dst_height, scale_x_src, scale_y_src);
  }
  memset(gfx.RDRAM+rdp.cimg, 0, rdp.ci_width*rdp.ci_height*rdp.ci_size);
}

#define RGBA16TO32(color) \
  ((color&1)?0xFF:0) | \
  ((DWORD)((float)((color&0xF800) >> 11) / 31.0f * 255.0f) << 24) | \
  ((DWORD)((float)((color&0x07C0) >> 6) / 31.0f * 255.0f) << 16) | \
((DWORD)((float)((color&0x003E) >> 1) / 31.0f * 255.0f) << 8)

static void CopyFrameBuffer (GrBuffer_t buffer = GR_BUFFER_BACKBUFFER)
{
  if (!fullscreen)
    return;
  FRDP ("CopyFrameBuffer: %08lx... ", rdp.cimg);

  // don't bother to write the stuff in asm... the slow part is the read from video card,
  //   not the copy.

  int width = rdp.ci_width;//*gfx.VI_WIDTH_REG;
  int height;
  if (settings.fb_smart && !settings.PPL)
  {
    int ind = (rdp.ci_count > 0)?rdp.ci_count-1:0;
    height = rdp.frame_buffers[ind].height;
  }
  else
  {
    height = rdp.ci_lower_bound;
    if (settings.PPL)
      height -= rdp.ci_upper_bound;
  }
  FRDP ("width: %d, height: %d...  ", width, height);

  if (rdp.scale_x < 1.1f)
  {
    WORD * ptr_src = new WORD[width*height];
    if (grLfbReadRegion(buffer,
      0,
      0,//rdp.ci_upper_bound,
      width,
      height,
      width<<1,
      ptr_src))
    {
      WORD *ptr_dst = (WORD*)(gfx.RDRAM+rdp.cimg);
      DWORD *ptr_dst32 = (DWORD*)(gfx.RDRAM+rdp.cimg);
      WORD c;

      for (int y=0; y<height; y++)
      {
        for (int x=0; x<width; x++)
        {
          c = ptr_src[x + y * width];
          if (settings.fb_read_alpha)
          {
            if (c > 0)
              c = (c&0xFFC0) | ((c&0x001F) << 1) | 1;
          }
          else
          {
            c = (c&0xFFC0) | ((c&0x001F) << 1) | 1;
          }
          if (rdp.ci_size == 2)
            ptr_dst[(x + y * width)^1] = c;
          else
            ptr_dst32[x + y * width] = RGBA16TO32(c);
        }
      }
      /*
      }
      else //8bit I or CI
      {
      BYTE *ptr_dst = (BYTE*)(gfx.RDRAM+rdp.cimg);
      WORD c;

        for (int y=0; y<height; y++)
        {
        for (int x=0; x<width; x++)
        {
        c = ptr_src[x + y * width];
        BYTE b = (BYTE)((float)(c&0x1F)/31.0f*85.0f);
        BYTE g = (BYTE)((float)((c>>5)&0x3F)/63.0f*85.0f);
        BYTE r = (BYTE)((float)((c>>11)&0x1F)/31.0f*85.0f);
        c = (c&0xFFC0) | ((c&0x001F) << 1) | 1;
        //              FRDP("src: %08lx,  dst: %d\n",c,(BYTE)(r+g+b));
        ptr_dst[(x + y * width)^1] = (BYTE)(r+g+b);
        //              ptr_dst[(x + y * width)^1] = (BYTE)((c>>8)&0xFF);
        }
        }
    }  */
      RDP ("ReadRegion.  Framebuffer copy complete.\n");
    }
    else
    {
      RDP ("Framebuffer copy failed.\n");
    }
    delete[] ptr_src;
  }
  else
  {
    if (rdp.motionblur && settings.fb_hires)
    {
      return;
    }
    else
    {
      float scale_x = (float)settings.scr_res_x / rdp.vi_width;//(float)max(rdp.frame_buffers[rdp.main_ci_index].width, rdp.ci_width);
      float scale_y = (float)settings.scr_res_y / rdp.vi_height;//(float)max(rdp.frame_buffers[rdp.main_ci_index].height, rdp.ci_lower_bound);

      FRDP("width: %d, height: %d, ul_y: %d, lr_y: %d, scale_x: %f, scale_y: %f, ci_width: %d, ci_height: %d\n",width, height, rdp.ci_upper_bound, rdp.ci_lower_bound, scale_x, scale_y, rdp.ci_width, rdp.ci_height);
      GrLfbInfo_t info;
      info.size = sizeof(GrLfbInfo_t);


      // VP 888 disconnected for now
      if (1||rdp.ci_size <= 2) {
    if (grLfbLock (GR_LFB_READ_ONLY,
               buffer,
               GR_LFBWRITEMODE_565,
               GR_ORIGIN_UPPER_LEFT,
               FXFALSE,
               &info))
      {
        WORD *ptr_src = (WORD*)info.lfbPtr;
        WORD *ptr_dst = (WORD*)(gfx.RDRAM+rdp.cimg);
        DWORD *ptr_dst32 = (DWORD*)(gfx.RDRAM+rdp.cimg);
        WORD c;
        DWORD stride = info.strideInBytes>>1;

        BOOL read_alpha = settings.fb_read_alpha;
        if (settings.PM && rdp.frame_buffers[rdp.ci_count-1].status != ci_aux)
          read_alpha = FALSE;
        for (int y=0; y<height; y++)
          {
        for (int x=0; x<width; x++)
          {
            c = ptr_src[int(x*scale_x) + int(y * scale_y) * stride];
            c = (c&0xFFC0) | ((c&0x001F) << 1) | 1;
            if (read_alpha && c == 1)
              c = 0;
            if (rdp.ci_size <= 2)
              ptr_dst[(x + y * width)^1] = c;
            else
              ptr_dst32[x + y * width] = RGBA16TO32(c);
          }
          }

        // Unlock the backbuffer
        grLfbUnlock (GR_LFB_READ_ONLY, buffer);
        RDP ("LfbLock.  Framebuffer copy complete.\n");
      }
      else
      {
        RDP ("Framebuffer copy failed.\n");
      }

      } else {

    if (grLfbLock (GR_LFB_READ_ONLY,
               buffer,
               GR_LFBWRITEMODE_888,
               GR_ORIGIN_UPPER_LEFT,
               FXFALSE,
               &info))
      {
        DWORD *ptr_src = (DWORD*)info.lfbPtr;
        //FIXME: Why unused?
        //WORD *ptr_dst = (WORD*)(gfx.RDRAM+rdp.cimg);
        DWORD *ptr_dst32 = (DWORD*)(gfx.RDRAM+rdp.cimg);
        DWORD c;
        DWORD stride = info.strideInBytes>>1;

        for (int y=0; y<height; y++)
          {
        for (int x=0; x<width; x++)
          {
            c = ptr_src[int(x*scale_x) + int(y * scale_y) * stride];
            ptr_dst32[x + y * width] = c;
          }
          }

        // Unlock the backbuffer
        grLfbUnlock (GR_LFB_READ_ONLY, buffer);
        RDP ("LfbLock.  Framebuffer copy complete.\n");
      }
      else
      {
        RDP ("Framebuffer copy failed.\n");
      }
      }
    }
  }
}

/******************************************************************
Function: ProcessDList
Purpose:  This function is called when there is a Dlist to be
processed. (High level GFX list)
input:    none
output:   none
*******************************************************************/
void DetectFrameBufferUsage ();
DWORD fbreads_front = 0;
DWORD fbreads_back = 0;
BOOL cpu_fb_read_called = FALSE;
BOOL cpu_fb_write_called = FALSE;
BOOL cpu_fb_write = FALSE;
BOOL cpu_fb_ignore = FALSE;
BOOL CI_SET = TRUE;

#ifdef __cplusplus
extern "C" {
#endif

EXPORT void CALL ProcessDList(void)
{
    no_dlist = FALSE;
  update_screen_count = 0;
    ChangeSize ();

#ifdef ALTTAB_FIX
    if (!hhkLowLevelKybd)
    {
        hhkLowLevelKybd = SetWindowsHookEx(WH_KEYBOARD_LL,
            LowLevelKeyboardProc, hInstance, 0);
    }
#endif

    LOG ("ProcessDList ()\n");

    if (!fullscreen)
  {
        drawNoFullscreenMessage();
    // Set an interrupt to allow the game to continue
    *gfx.MI_INTR_REG |= 0x20;
    gfx.CheckInterrupts();
  }

    if (reset)
    {
        reset = 0;

        memset (microcode, 0, 4096);
        if (settings.autodetect_ucode)
        {
            // Thanks to ZeZu for ucode autodetection!!!

            DWORD startUcode = *(DWORD*)(gfx.DMEM+0xFD0);
            memcpy (microcode, gfx.RDRAM+startUcode, 4096);
            microcheck ();

        }
    }
  else if ( ((old_ucode == 6) && (settings.ucode == 1)) || settings.force_microcheck)
    {
        DWORD startUcode = *(DWORD*)(gfx.DMEM+0xFD0);
        memcpy (microcode, gfx.RDRAM+startUcode, 4096);
        microcheck ();
    }

    if (exception) return;

    // Switch to fullscreen?
    if (to_fullscreen)
    {
        to_fullscreen = FALSE;

        if (!InitGfx (FALSE))
        {
            LOG ("FAILED!!!\n");
            return;
        }
        fullscreen = TRUE;
    }

    // Clear out the RDP log
#ifdef RDP_LOGGING
    if (settings.logging && settings.log_clear)
    {
        CLOSE_RDP_LOG ();
        OPEN_RDP_LOG ();
    }
#endif

#ifdef UNIMP_LOG
    if (settings.log_unk && settings.unk_clear)
    {
        std::ofstream unimp;
        unimp.open("unimp.txt");
        unimp.close();
    }
#endif

  //* Set states *//
  if (settings.swapmode > 0)
    SwapOK = TRUE;
  rdp.updatescreen = 1;

    rdp.tri_n = 0;  // 0 triangles so far this frame
    rdp.debug_n = 0;

    rdp.model_i = 0; // 0 matrices so far in stack
    //stack_size can be less then 32! Important for Silicon Vally. Thanks Orkin!
    rdp.model_stack_size = min(32, (*(DWORD*)(gfx.DMEM+0x0FE4))>>6);
    if (rdp.model_stack_size == 0)
      rdp.model_stack_size = 32;
  rdp.fb_drawn = rdp.fb_drawn_front = FALSE;
    rdp.update = 0x7FFFFFFF;  // All but clear cache
    rdp.geom_mode = 0;
  rdp.acmp = 0;
    rdp.maincimg[1] = rdp.maincimg[0];
    rdp.skip_drawing = FALSE;
    rdp.s2dex_tex_loaded = FALSE;
  fbreads_front = fbreads_back = 0;
  rdp.fog_multiplier = rdp.fog_offset = 0;
  rdp.zsrc = 0;

    if (cpu_fb_write == TRUE)
      DrawFrameBufferToScreen();
    cpu_fb_write = FALSE;
  cpu_fb_read_called = FALSE;
  cpu_fb_write_called = FALSE;
  cpu_fb_ignore = FALSE;
    d_ul_x = 0xffff;
    d_ul_y = 0xffff;
    d_lr_x = 0;
    d_lr_y = 0;

    //analize possible frame buffer usage
    if (settings.fb_smart)
        DetectFrameBufferUsage();
  if (!settings.lego || rdp.num_of_ci > 1)
    rdp.last_bg = 0;
  //* End of set states *//


    // Get the start of the display list and the length of it
    DWORD dlist_start = *(DWORD*)(gfx.DMEM+0xFF0);
    DWORD dlist_length = *(DWORD*)(gfx.DMEM+0xFF4);
  FRDP("--- NEW DLIST --- crc: %08lx, ucode: %d, fbuf: %08lx, fbuf_width: %d, dlist start: %08lx, dlist_lenght: %d\n", uc_crc, settings.ucode, *gfx.VI_ORIGIN_REG, *gfx.VI_WIDTH_REG, dlist_start, dlist_length);
    FRDP_E("--- NEW DLIST --- crc: %08lx, ucode: %d, fbuf: %08lx\n", uc_crc, settings.ucode, *gfx.VI_ORIGIN_REG);

    if (settings.tonic && dlist_length < 16)
    {
    rdp_fullsync();
        FRDP_E("DLIST is too short!\n");
        return;
    }

    // Start executing at the start of the display list
    rdp.pc_i = 0;
    rdp.pc[rdp.pc_i] = dlist_start;
    rdp.dl_count = -1;
    rdp.halt = 0;
  DWORD a;

    // catches exceptions so that it doesn't freeze
#ifdef CATCH_EXCEPTIONS
    try {
#endif

        // MAIN PROCESSING LOOP
        do {

            // Get the address of the next command
            a = rdp.pc[rdp.pc_i] & BMASK;

            // Load the next command and its input
            rdp.cmd0 = ((DWORD*)gfx.RDRAM)[a>>2];   // \ Current command, 64 bit
            rdp.cmd1 = ((DWORD*)gfx.RDRAM)[(a>>2)+1]; // /
            // cmd2 and cmd3 are filled only when needed, by the function that needs them

            // Output the address before the command
#ifdef LOG_COMMANDS
            FRDP ("%08lx (c0:%08lx, c1:%08lx): ", a, rdp.cmd0, rdp.cmd1);
#else
            FRDP ("%08lx: ", a);
#endif

            // Go to the next instruction
            rdp.pc[rdp.pc_i] = (a+8) & BMASK;

#ifdef PERFORMANCE
            QueryPerformanceCounter ((LARGE_INTEGER*)&perf_cur);
#endif
            // Process this instruction
            gfx_instruction[settings.ucode][rdp.cmd0>>24] ();

            // check DL counter
            if (rdp.dl_count != -1)
            {
                rdp.dl_count --;
                if (rdp.dl_count == 0)
                {
                    rdp.dl_count = -1;

                    RDP ("End of DL\n");
                    rdp.pc_i --;
                }
            }

#ifdef PERFORMANCE
            QueryPerformanceCounter ((LARGE_INTEGER*)&perf_next);
            __int64 t = perf_next-perf_cur;
            sprintf (out_buf, "perf %08lx: %016I64d\n", a-8, t);
            rdp_log << out_buf;
#endif

        } while (!rdp.halt);
#ifdef CATCH_EXCEPTIONS
    } catch (...) {

        if (fullscreen) ReleaseGfx ();
        WriteLog(M64MSG_ERROR, "The GFX plugin caused an exception and has been disabled.");
        exception = TRUE;
    }
#endif

    if (settings.fb_smart)
    {
        rdp.scale_x = rdp.scale_x_bak;
        rdp.scale_y = rdp.scale_y_bak;
    }
    if (settings.fb_read_always)
    {
    CopyFrameBuffer ();
    }
    if (rdp.yuv_image)
    {
      DrawYUVImageToFrameBuffer();
      rdp.yuv_image = FALSE;
//        FRDP("yuv image draw. ul_x: %f, ul_y: %f, lr_x: %f, lr_y: %f, begin: %08lx\n",
//        rdp.yuv_ul_x, rdp.yuv_ul_y, rdp.yuv_lr_x, rdp.yuv_lr_y, rdp.yuv_im_begin);
      rdp.yuv_ul_x = rdp.yuv_ul_y = rdp.yuv_lr_x = rdp.yuv_lr_y = 0;
      rdp.yuv_im_begin = 0x00FFFFFF;
    }
    if (rdp.cur_image)
    CloseTextureBuffer(rdp.read_whole_frame && (settings.PM || rdp.swap_ci_index >= 0));

  if (settings.TGR2 && rdp.vi_org_reg != *gfx.VI_ORIGIN_REG && CI_SET)
  {
    newSwapBuffers ();
    CI_SET = FALSE;
  }
    RDP("ProcessDList end\n");
}

#ifdef __cplusplus
}
#endif

// undef - undefined instruction, always ignore
static void undef()
{
    FRDP_E("** undefined ** (%08lx)\n", rdp.cmd0);
    FRDP("** undefined ** (%08lx) - IGNORED\n", rdp.cmd0);
  #ifdef _FINAL_RELEASE_
  *gfx.MI_INTR_REG |= 0x20;
  gfx.CheckInterrupts();
    rdp.halt = 1;
  #endif
}

// spnoop - no operation, always ignore
static void spnoop()
{
    RDP("spnoop\n");
}

// noop - no operation, always ignore
static void rdp_noop()
{
    RDP("noop\n");
}

static void ys_memrect ()
{
    DWORD tile = (WORD)((rdp.cmd1 & 0x07000000) >> 24);

    DWORD lr_x = (WORD)((rdp.cmd0 & 0x00FFF000) >> 14);
    DWORD lr_y = (WORD)((rdp.cmd0 & 0x00000FFF) >> 2);
    DWORD ul_x = (WORD)((rdp.cmd1 & 0x00FFF000) >> 14);
    DWORD ul_y = (WORD)((rdp.cmd1 & 0x00000FFF) >> 2);

    rdp.pc[rdp.pc_i] += 16; // texrect is 196-bit

    if (lr_y > rdp.scissor_o.lr_y) lr_y = rdp.scissor_o.lr_y;

    FRDP ("memrect (%d, %d, %d, %d), ci_width: %d\n", ul_x, ul_y, lr_x, lr_y, rdp.ci_width);

    DWORD y, width = lr_x - ul_x;
    DWORD texaddr = rdp.addr[rdp.tiles[tile].t_mem];
    DWORD tex_width = rdp.tiles[tile].line << 3;

    for (y = ul_y; y < lr_y; y++) {
        BYTE *src = gfx.RDRAM + texaddr + (y - ul_y) * tex_width;
        BYTE *dst = gfx.RDRAM + rdp.cimg + ul_x + y * rdp.ci_width;
        memcpy (dst, src, width);
    }
}

static void pm_palette_mod ()
{
  BYTE envr = (BYTE)((float)((rdp.env_color >> 24)&0xFF)/255.0f*31.0f);
  BYTE envg = (BYTE)((float)((rdp.env_color >> 16)&0xFF)/255.0f*31.0f);
  BYTE envb = (BYTE)((float)((rdp.env_color >> 8)&0xFF)/255.0f*31.0f);
  WORD env16 = (WORD)((envr<<11)|(envg<<6)|(envb<<1)|1);
  BYTE prmr = (BYTE)((float)((rdp.prim_color >> 24)&0xFF)/255.0f*31.0f);
  BYTE prmg = (BYTE)((float)((rdp.prim_color >> 16)&0xFF)/255.0f*31.0f);
  BYTE prmb = (BYTE)((float)((rdp.prim_color >> 8)&0xFF)/255.0f*31.0f);
  WORD prim16 = (WORD)((prmr<<11)|(prmg<<6)|(prmb<<1)|1);
  WORD * dst = (WORD*)(gfx.RDRAM+rdp.cimg);
  for (int i = 0; i < 16; i++)
  {
    dst[i^1] = (rdp.pal_8[i]&1) ? prim16 : env16;
  }
  RDP("Texrect palette modification\n");
}

static void rdp_texrect()
{
    DWORD a = rdp.pc[rdp.pc_i];
    rdp.cmd2 = ((DWORD*)gfx.RDRAM)[(a>>2)+1];
    rdp.cmd3 = ((DWORD*)gfx.RDRAM)[(a>>2)+3];

    if (settings.ASB) //modified Rice's hack for All-Star Baseball games
    {
        DWORD dwHalf1 = (((DWORD*)gfx.RDRAM)[(a>>2)+0]) >> 24;
        if ((dwHalf1 != 0xF1)  && (dwHalf1 != 0xb3))
        {
            rdp.pc[rdp.pc_i] += 16;
        }
        else
        {
            rdp.pc[rdp.pc_i] += 8;
            rdp.cmd3 = rdp.cmd2;
            rdp.cmd2 = 0;
        }
    }
  else if (settings.yoshi && settings.ucode == 6)
  {
    ys_memrect();
    return;
  }
    else
    {
        rdp.pc[rdp.pc_i] += 16; // texrect is 196-bit
    }

  if (rdp.skip_drawing || (!settings.fb_smart && (rdp.cimg == rdp.zimg)))
  {
    if (settings.PM && rdp.ci_status == ci_useless)
    {
      pm_palette_mod ();
    }
    else
    {
        RDP("Texrect skipped\n");
    }
        return;
    }

  if ((settings.ucode == 8) && rdp.cur_image && rdp.cur_image->format)
    {
    //FRDP("Wrong Texrect. texaddr: %08lx, cimg: %08lx, cimg_end: %08lx\n", rdp.timg.addr, rdp.maincimg[1].addr, rdp.maincimg[1].addr+rdp.ci_width*rdp.ci_height*rdp.ci_size);
    RDP("Shadow texrect is skipped.\n");
        rdp.tri_n += 2;
        return;
    }

    WORD ul_x = (WORD)((rdp.cmd1 & 0x00FFF000) >> 14);
    WORD ul_y = (WORD)((rdp.cmd1 & 0x00000FFF) >> 2);
    WORD lr_x = (WORD)((rdp.cmd0 & 0x00FFF000) >> 14);
    WORD lr_y = (WORD)((rdp.cmd0 & 0x00000FFF) >> 2);
    if (ul_x >= lr_x) return;
  if (rdp.cycle_mode > 1 || settings.increase_texrect_edge)
  {
    lr_x++;
    lr_y++;
  }
  if (ul_y == lr_y)
  {
    lr_y ++;
  }

  //*
  if (rdp.hires_tex && settings.fb_optimize_texrect)
    {
       if (!rdp.hires_tex->drawn)
       {
            DRAWIMAGE d;
            d.imageX    = 0;
            d.imageW    = (WORD)rdp.hires_tex->width;
      d.frameX  = ul_x;
            d.frameW    = (WORD)(rdp.hires_tex->width);//(WORD)(ul_x + rdp.hires_tex->width);//lr_x;

            d.imageY    = 0;
            d.imageH    = (WORD)rdp.hires_tex->height;
      d.frameY  = ul_y;
            d.frameH    = (WORD)(rdp.hires_tex->height);//(ul_y + rdp.hires_tex->height);
            FRDP("texrect. ul_x: %d, ul_y: %d, lr_x: %d, lr_y: %d, width: %d, height: %d\n", ul_x, ul_y, lr_x, lr_y, rdp.hires_tex->width, rdp.hires_tex->height);
            d.scaleX    = 1.0f;
            d.scaleY    = 1.0f;
      DrawHiresImage(&d, rdp.hires_tex->width == rdp.ci_width);
            rdp.hires_tex->drawn = TRUE;
       }
       return;
    }
//*/
    // framebuffer workaround for Zelda: MM LOT
    if ((rdp.othermode_l & 0xFFFF0000) == 0x0f5a0000)
        return;

    /*Gonetz*/
    //hack for Zelda MM. it removes black texrects which cover all geometry in "Link meets Zelda" cut scene
    if (settings.zelda && rdp.timg.addr >= rdp.cimg && rdp.timg.addr < rdp.ci_end)
    {
    FRDP("Wrong Texrect. texaddr: %08lx, cimg: %08lx, cimg_end: %08lx\n", rdp.cur_cache[0]->addr, rdp.cimg, rdp.cimg+rdp.ci_width*rdp.ci_height*2);
        rdp.tri_n += 2;
        return;
    }
//*
    //hack for Banjo2. it removes black texrects under Banjo
    if (!settings.fb_hires && ((rdp.cycle1 << 16) | (rdp.cycle2 & 0xFFFF)) == 0xFFFFFFFF && (rdp.othermode_l & 0xFFFF0000) == 0x00500000)
    {
        rdp.tri_n += 2;
        return;
    }
//*/
  //*
  //remove motion blur in night vision
  if ((settings.ucode == 7) && (rdp.maincimg[1].addr != rdp.maincimg[0].addr) && (rdp.timg.addr >= rdp.maincimg[1].addr) && (rdp.timg.addr < (rdp.maincimg[1].addr+rdp.ci_width*rdp.ci_height*rdp.ci_size)))
    {
        if (settings.fb_smart)
          if (rdp.frame_buffers[rdp.ci_count-1].status == ci_copy_self || !settings.fb_motionblur)
          {
        //          FRDP("Wrong Texrect. texaddr: %08lx, cimg: %08lx, cimg_end: %08lx\n", rdp.timg.addr, rdp.maincimg[1], rdp.maincimg[1]+rdp.ci_width*rdp.ci_height*rdp.ci_size);
        RDP("Wrong Texrect.\n");
            rdp.tri_n += 2;
            return;
          }
    }
//*/

    int i;

    DWORD tile = (WORD)((rdp.cmd1 & 0x07000000) >> 24);

    // update MUST be at the beginning, b/c of update_scissor
    if (rdp.cycle_mode == 2)
    {
        rdp.tex = 1;
        rdp.allow_combine = 0;

    cmb.tmu1_func = cmb.tmu0_func = GR_COMBINE_FUNCTION_LOCAL;
    cmb.tmu1_fac = cmb.tmu0_fac = GR_COMBINE_FACTOR_NONE;
    cmb.tmu1_a_func = cmb.tmu0_a_func = GR_COMBINE_FUNCTION_LOCAL;
    cmb.tmu1_a_fac = cmb.tmu0_a_fac = GR_COMBINE_FACTOR_NONE;
    cmb.tmu1_invert = cmb.tmu0_invert = FXFALSE;
    cmb.tmu1_a_invert = cmb.tmu0_a_invert = FXFALSE;
    }

    rdp.texrecting = 1;

    DWORD prev_tile = rdp.cur_tile;
    rdp.cur_tile = tile;
    rdp.update |= UPDATE_COMBINE;
    update ();

    rdp.texrecting = 0;
    rdp.allow_combine = 1;

    if (!rdp.cur_cache[0])
    {
        rdp.cur_tile = prev_tile;
        rdp.tri_n += 2;
        return;
    }
    // ****
    // ** Texrect offset by Gugaman **
    float off_x = (float)((short)((rdp.cmd2 & 0xFFFF0000) >> 16)) / 32.0f;
    if ((int(off_x) == 512) && (rdp.timg.width < 512)) off_x = 0.0f;
    float off_y = (float)((short)(rdp.cmd2 & 0x0000FFFF)) / 32.0f;
    float dsdx = (float)((short)((rdp.cmd3 & 0xFFFF0000) >> 16)) / 1024.0f;
    float dtdy = (float)((short)(rdp.cmd3 & 0x0000FFFF)) / 1024.0f;

    if (rdp.cycle_mode == 2) dsdx /= 4.0f;

    float s_ul_x = ul_x * rdp.scale_x + rdp.offset_x;
    float s_lr_x = lr_x * rdp.scale_x + rdp.offset_x;
    float s_ul_y = ul_y * rdp.scale_y + rdp.offset_y;
    float s_lr_y = lr_y * rdp.scale_y + rdp.offset_y;

    FRDP("texrect (%d, %d, %d, %d), tile: %d, #%d, #%d\n", ul_x, ul_y, lr_x, lr_y, tile, rdp.tri_n, rdp.tri_n+1);
    FRDP ("(%f, %f) -> (%f, %f), s: (%d, %d) -> (%d, %d)\n", s_ul_x, s_ul_y, s_lr_x, s_lr_y, rdp.scissor.ul_x, rdp.scissor.ul_y, rdp.scissor.lr_x, rdp.scissor.lr_y);
    FRDP("\toff_x: %f, off_y: %f, dsdx: %f, dtdy: %f\n", off_x, off_y, dsdx, dtdy);

    float off_size_x;
    float off_size_y;

    if ( ((rdp.cmd0>>24)&0xFF) == 0xE5 ) //texrectflip
    {
      off_size_x = (float)((lr_y - ul_y - 1) * dsdx);
      off_size_y = (float)((lr_x - ul_x - 1) * dtdy);
    }
    else
    {
      off_size_x = (float)((lr_x - ul_x - 1) * dsdx);
      off_size_y = (float)((lr_y - ul_y - 1) * dtdy);
    }

    float lr_u0, lr_v0, ul_u0, ul_v0, lr_u1, lr_v1, ul_u1, ul_v1;

    if (rdp.cur_cache[0] && (rdp.tex & 1))
    {
        float sx=1, sy=1;
        if (rdp.tiles[rdp.cur_tile].shift_s)
        {
            if (rdp.tiles[rdp.cur_tile].shift_s > 10)
                sx = (float)(1 << (16 - rdp.tiles[rdp.cur_tile].shift_s));
            else
                sx = (float)1.0f/(1 << rdp.tiles[rdp.cur_tile].shift_s);
        }
        if (rdp.tiles[rdp.cur_tile].shift_t)
        {
            if (rdp.tiles[rdp.cur_tile].shift_t > 10)
                sy = (float)(1 << (16 - rdp.tiles[rdp.cur_tile].shift_t));
            else
                sy = (float)1.0f/(1 << rdp.tiles[rdp.cur_tile].shift_t);
        }
    if (rdp.hires_tex && rdp.hires_tex->tile == 0)
    {
      off_x += rdp.hires_tex->u_shift;// + rdp.tiles[0].ul_s; //commented for Paper Mario motion blur
      off_y += rdp.hires_tex->v_shift;// + rdp.tiles[0].ul_t;
      FRDP("hires_tex ul_s: %d, ul_t: %d, off_x: %f, off_y: %f\n", rdp.tiles[0].ul_s, rdp.tiles[0].ul_t, off_x, off_y);
        ul_u0 = off_x * sx;
        ul_v0 = off_y * sy;

        lr_u0 = ul_u0 + off_size_x * sx;
        lr_v0 = ul_v0 + off_size_y * sy;

        ul_u0 *= rdp.hires_tex->u_scale;
        ul_v0 *= rdp.hires_tex->v_scale;
        lr_u0 *= rdp.hires_tex->u_scale;
        lr_v0 *= rdp.hires_tex->v_scale;
        FRDP("hires_tex ul_u0: %f, ul_v0: %f, lr_u0: %f, lr_v0: %f\n", ul_u0, ul_v0, lr_u0, lr_v0);
    }
    else
    {
            ul_u0 = off_x * sx;
        ul_v0 = off_y * sy;

        ul_u0 -= rdp.tiles[rdp.cur_tile].f_ul_s;
        ul_v0 -= rdp.tiles[rdp.cur_tile].f_ul_t;

        lr_u0 = ul_u0 + off_size_x * sx;
        lr_v0 = ul_v0 + off_size_y * sy;

            ul_u0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_x * ul_u0;
            lr_u0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_x * lr_u0;
            ul_v0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_y * ul_v0;
            lr_v0 = rdp.cur_cache[0]->c_off + rdp.cur_cache[0]->c_scl_y * lr_v0;
        }
    }
    else
    {
        ul_u0 = ul_v0 = lr_u0 = lr_v0 = 0;
    }
    if (rdp.cur_cache[1] && (rdp.tex & 2))
    {
        float sx=1, sy=1;

        if (rdp.tiles[rdp.cur_tile+1].shift_s)
        {
            if (rdp.tiles[rdp.cur_tile+1].shift_s > 10)
                sx = (float)(1 << (16 - rdp.tiles[rdp.cur_tile+1].shift_s));
            else
                sx = (float)1.0f/(1 << rdp.tiles[rdp.cur_tile+1].shift_s);
        }
        if (rdp.tiles[rdp.cur_tile+1].shift_t)
        {
            if (rdp.tiles[rdp.cur_tile+1].shift_t > 10)
                sy = 1;//(float)(1 << (16 - rdp.tiles[rdp.cur_tile+1].shift_t));
            else
                sy = (float)1.0f/(1 << rdp.tiles[rdp.cur_tile+1].shift_t);
        }

    if (rdp.hires_tex && rdp.hires_tex->tile == 1)
    {
      off_x += rdp.hires_tex->u_shift;// + rdp.tiles[0].ul_s; //commented for Paper Mario motion blur
      off_y += rdp.hires_tex->v_shift;// + rdp.tiles[0].ul_t;
      FRDP("hires_tex ul_s: %d, ul_t: %d, off_x: %f, off_y: %f\n", rdp.tiles[0].ul_s, rdp.tiles[0].ul_t, off_x, off_y);
            ul_u1 = off_x * sx;
        ul_v1 = off_y * sy;

        lr_u1 = ul_u1 + off_size_x * sx;
        lr_v1 = ul_v1 + off_size_y * sy;

        ul_u1 *= rdp.hires_tex->u_scale;
        ul_v1 *= rdp.hires_tex->v_scale;
        lr_u1 *= rdp.hires_tex->u_scale;
        lr_v1 *= rdp.hires_tex->v_scale;
        FRDP("hires_tex ul_u1: %f, ul_v1: %f, lr_u1: %f, lr_v1: %f\n", ul_u0, ul_v0, lr_u0, lr_v0);

    }
    else
    {
        ul_u1 = off_x * sx;
        ul_v1 = off_y * sy;

        ul_u1 -= rdp.tiles[rdp.cur_tile+1].f_ul_s;
        ul_v1 -= rdp.tiles[rdp.cur_tile+1].f_ul_t;

        lr_u1 = ul_u1 + off_size_x * sx;
        lr_v1 = ul_v1 + off_size_y * sy;

        ul_u1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_x * ul_u1;
        lr_u1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_x * lr_u1;
        ul_v1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_y * ul_v1;
        lr_v1 = rdp.cur_cache[1]->c_off + rdp.cur_cache[1]->c_scl_y * lr_v1;
    }
  }
    else
    {
        ul_u1 = ul_v1 = lr_u1 = lr_v1 = 0;
    }
    rdp.cur_tile = prev_tile;

    // ****

    FRDP ("  scissor: (%d, %d) -> (%d, %d)\n", rdp.scissor.ul_x, rdp.scissor.ul_y, rdp.scissor.lr_x, rdp.scissor.lr_y);

    CCLIP2 (s_ul_x, s_lr_x, ul_u0, lr_u0, ul_u1, lr_u1, (float)rdp.scissor.ul_x, (float)rdp.scissor.lr_x);
    CCLIP2 (s_ul_y, s_lr_y, ul_v0, lr_v0, ul_v1, lr_v1, (float)rdp.scissor.ul_y, (float)rdp.scissor.lr_y);
//  CCLIP2 (s_lr_y, s_ul_y, lr_v0, ul_v0, lr_v1, ul_v1, (float)rdp.scissor.ul_y, (float)rdp.scissor.lr_y);

    FRDP ("  draw at: (%f, %f) -> (%f, %f)\n", s_ul_x, s_ul_y, s_lr_x, s_lr_y);

    // DO NOT SET CLAMP MODE HERE

    float Z = 1.0f;
    if (rdp.zsrc == 1 && (rdp.othermode_l & 0x00000030))  // othermode check makes sure it
        // USES the z-buffer.  Otherwise it returns bad (unset) values for lot and telescope
        //in zelda:mm.
    {
            FRDP ("prim_depth = %d\n", rdp.prim_depth);
        Z = rdp.prim_depth;
    if (settings.increase_primdepth)
          Z += 8.0f;
    Z = ScaleZ(Z);

            grDepthBufferFunction (GR_CMP_LEQUAL);
            rdp.update |= UPDATE_ZBUF_ENABLED;
        }
    else
    {
        RDP ("no prim_depth used, using 1.0\n");
    }

    VERTEX vstd[4] = {
    { s_ul_x, s_ul_y, Z, 1.0f, ul_u0, ul_v0, ul_u1, ul_v1, { 0, 0, 0, 0}, 255 },
    { s_lr_x, s_ul_y, Z, 1.0f, lr_u0, ul_v0, lr_u1, ul_v1, { 0, 0, 0, 0}, 255 },
    { s_ul_x, s_lr_y, Z, 1.0f, ul_u0, lr_v0, ul_u1, lr_v1, { 0, 0, 0, 0}, 255 },
    { s_lr_x, s_lr_y, Z, 1.0f, lr_u0, lr_v0, lr_u1, lr_v1, { 0, 0, 0, 0}, 255 } };

        if ( ((rdp.cmd0>>24)&0xFF) == 0xE5 ) //texrectflip
        {
            vstd[1].u0 = ul_u0;
            vstd[1].v0 = lr_v0;
            vstd[1].u1 = ul_u1;
            vstd[1].v1 = lr_v1;

            vstd[2].u0 = lr_u0;
            vstd[2].v0 = ul_v0;
            vstd[2].u1 = lr_u1;
            vstd[2].v1 = ul_v1;
        }

        VERTEX *vptr = vstd;
        int n_vertices = 4;

    VERTEX *vnew = 0;
//      for (int j =0; j < 4; j++)
//        FRDP("v[%d]  u0: %f, v0: %f, u1: %f, v1: %f\n", j, vstd[j].u0, vstd[j].v0, vstd[j].u1, vstd[j].v1);


        if (!rdp.hires_tex && rdp.cur_cache[0]->splits != 1)
        {
            // ** LARGE TEXTURE HANDLING **
            // *VERY* simple algebra for texrects
            float min_u, min_x, max_u, max_x;
            if (vstd[0].u0 < vstd[1].u0)
            {
                min_u = vstd[0].u0;
                min_x = vstd[0].x;
                max_u = vstd[1].u0;
                max_x = vstd[1].x;
            }
            else
            {
                min_u = vstd[1].u0;
                min_x = vstd[1].x;
                max_u = vstd[0].u0;
                max_x = vstd[0].x;
            }

            int start_u_256, end_u_256;

            if (settings.ucode == 7)
            {
              start_u_256 = 0;
              end_u_256 = (lr_x - ul_x - 1)>>8;
            }
            else
            {
              start_u_256 = (int)min_u >> 8;
              end_u_256 = (int)max_u >> 8;
            }
            //FRDP(" min_u: %f, max_u: %f start: %d, end: %d\n", min_u, max_u, start_u_256, end_u_256);

            int splitheight = rdp.cur_cache[0]->splitheight;

            int num_verts_line = 2 + ((end_u_256-start_u_256)<<1);
            vnew = new VERTEX [num_verts_line << 1];

            n_vertices = num_verts_line << 1;
            vptr = vnew;

            vnew[0] = vstd[0];
            vnew[0].u0 -= 256.0f * start_u_256;
            vnew[0].v0 += splitheight * start_u_256;
      vnew[0].u1 -= 256.0f * start_u_256;
      vnew[0].v1 += splitheight * start_u_256;
            vnew[1] = vstd[2];
            vnew[1].u0 -= 256.0f * start_u_256;
            vnew[1].v0 += splitheight * start_u_256;
      vnew[1].u1 -= 256.0f * start_u_256;
      vnew[1].v1 += splitheight * start_u_256;
            vnew[n_vertices-2] = vstd[1];
            vnew[n_vertices-2].u0 -= 256.0f * end_u_256;
            vnew[n_vertices-2].v0 += splitheight * end_u_256;
      vnew[n_vertices-2].u1 -= 256.0f * end_u_256;
      vnew[n_vertices-2].v1 += splitheight * end_u_256;
            vnew[n_vertices-1] = vstd[3];
            vnew[n_vertices-1].u0 -= 256.0f * end_u_256;
            vnew[n_vertices-1].v0 += splitheight * end_u_256;
      vnew[n_vertices-1].u1 -= 256.0f * end_u_256;
      vnew[n_vertices-1].v1 += splitheight * end_u_256;

            // find the equation of the line of u,x
            float m = (max_x - min_x) / (max_u - min_u);  // m = delta x / delta u
            float b = min_x - m * min_u;          // b = y - m * x

            for (i=start_u_256; i<end_u_256; i++)
            {
                // Find where x = current 256 multiple
                float x = m * ((i<<8)+256) + b;

                int vn = 2 + ((i-start_u_256)<<2);
                vnew[vn] = vnew[0];
                vnew[vn].x = x;
                vnew[vn].u0 = 255.5f;
                vnew[vn].v0 += (float)splitheight * i;
        vnew[vn].u1 = 255.5f;
        vnew[vn].v1 += (float)splitheight * i;

                vn ++;
                vnew[vn] = vnew[1];
                vnew[vn].x = x;
                vnew[vn].u0 = 255.5f;
                vnew[vn].v0 += (float)splitheight * i;
        vnew[vn].u1 = 255.5f;
        vnew[vn].v1 += (float)splitheight * i;

                vn ++;
                vnew[vn] = vnew[vn-2];
                vnew[vn].u0 = 0.5f;
                vnew[vn].v0 += (float)splitheight;
        vnew[vn].u1 = 0.5f;
        vnew[vn].v1 += (float)splitheight;

                vn ++;
                vnew[vn] = vnew[vn-2];
                vnew[vn].u0 = 0.5f;
                vnew[vn].v0 += (float)splitheight;
        vnew[vn].u1 = 0.5f;
        vnew[vn].v1 += (float)splitheight;
            }
        }

    AllowShadeMods (vptr, n_vertices);
        for (i=0; i<n_vertices; i++)
        {
            VERTEX *z = &vptr[i];

            z->u0 *= z->q;
            z->v0 *= z->q;
            z->u1 *= z->q;
            z->v1 *= z->q;

            apply_shade_mods (z);
        }

        if (fullscreen)
        {
            grFogMode (GR_FOG_DISABLE);

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
                if (rdp.othermode_l & 1)
                {
                    grAlphaTestFunction (GR_CMP_GEQUAL);
                    grAlphaTestReferenceValue (0x80);
                }
                else
                    grAlphaTestFunction (GR_CMP_ALWAYS);

                rdp.update |= UPDATE_ALPHA_COMPARE | UPDATE_COMBINE;
            }

            ConvertCoordsConvert (vptr, n_vertices);

            if (settings.wireframe)
            {
                SetWireframeCol ();
                grDrawLine (&vstd[0], &vstd[2]);
                grDrawLine (&vstd[2], &vstd[1]);
                grDrawLine (&vstd[1], &vstd[0]);
                grDrawLine (&vstd[2], &vstd[3]);
                grDrawLine (&vstd[3], &vstd[1]);
            }
            else
            {
                grDrawVertexArrayContiguous (GR_TRIANGLE_STRIP, n_vertices, vptr, sizeof(VERTEX));
            }

            if (debug.capture)
            {
                VERTEX vl[3];
                vl[0] = vstd[0];
                vl[1] = vstd[2];
                vl[2] = vstd[1];
                add_tri (vl, 3, TRI_TEXRECT);
                rdp.tri_n ++;
                vl[0] = vstd[2];
                vl[1] = vstd[3];
                vl[2] = vstd[1];
                add_tri (vl, 3, TRI_TEXRECT);
                rdp.tri_n ++;
            }
            else
                rdp.tri_n += 2;

      if (settings.fog && (rdp.flags & FOG_ENABLED))
      {
        grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
      }
            rdp.update |= UPDATE_CULL_MODE | UPDATE_VIEWPORT;
        }
        else
        {
            rdp.tri_n += 2;
        }

    delete[] vnew;
}

static void rdp_loadsync()
{
    RDP("loadsync - ignored\n");
}

static void rdp_pipesync()
{
    RDP("pipesync - ignored\n");
}

static void rdp_tilesync()
{
    RDP("tilesync - ignored\n");
}

static void rdp_fullsync()
{
  // Set an interrupt to allow the game to continue
  *gfx.MI_INTR_REG |= 0x20;
  gfx.CheckInterrupts();
  RDP("fullsync\n");
}

static void rdp_setkeygb()
{
    RDP_E("setkeygb - IGNORED\n");
    RDP("setkeygb - IGNORED\n");
}

static void rdp_setkeyr()
{
    RDP_E("setkeyr - IGNORED\n");
    RDP("setkeyr - IGNORED\n");
}

static void rdp_setconvert()
{
    /*
    rdp.YUV_C0 = 1.1647f  ;
    rdp.YUV_C1 = 0.79931f ;
    rdp.YUV_C2 = -0.1964f ;
    rdp.YUV_C3 = -0.40651f;
    rdp.YUV_C4 = 1.014f   ;
    */
    rdp.K5 = (BYTE)(rdp.cmd1&0x1FF);
    RDP_E("setconvert - IGNORED\n");
    RDP("setconvert - IGNORED\n");
}

//
// setscissor - sets the screen clipping rectangle
//

static void rdp_setscissor()
{
    // clipper resolution is 320x240, scale based on computer resolution
    rdp.scissor_o.ul_x = /*min(*/(DWORD)(((rdp.cmd0 & 0x00FFF000) >> 14))/*, 320)*/;
    rdp.scissor_o.ul_y = /*min(*/(DWORD)(((rdp.cmd0 & 0x00000FFF) >> 2))/*, 240)*/;
    rdp.scissor_o.lr_x = /*min(*/(DWORD)(((rdp.cmd1 & 0x00FFF000) >> 14))/*, 320)*/;
    rdp.scissor_o.lr_y = /*min(*/(DWORD)(((rdp.cmd1 & 0x00000FFF) >> 2))/*, 240)*/;

    rdp.ci_upper_bound = rdp.scissor_o.ul_y;
    rdp.ci_lower_bound = rdp.scissor_o.lr_y;

    FRDP("setscissor: (%d,%d) -> (%d,%d)\n", rdp.scissor_o.ul_x, rdp.scissor_o.ul_y,
        rdp.scissor_o.lr_x, rdp.scissor_o.lr_y);

    rdp.update |= UPDATE_SCISSOR;
}

static void rdp_setprimdepth()
{
    rdp.prim_depth = (WORD)((rdp.cmd1 >> 16) & 0x7FFF);

    FRDP("setprimdepth: %d\n", rdp.prim_depth);
}

static void rdp_setothermode()
{
#define F3DEX2_SETOTHERMODE(cmd,sft,len,data) { \
    rdp.cmd0 = (cmd<<24) | ((32-(sft)-(len))<<8) | (((len)-1)); \
    rdp.cmd1 = data; \
    gfx_instruction[settings.ucode][cmd] (); \
}
#define SETOTHERMODE(cmd,sft,len,data) { \
    rdp.cmd0 = (cmd<<24) | ((sft)<<8) | (len); \
    rdp.cmd1 = data; \
    gfx_instruction[settings.ucode][cmd] (); \
}

    RDP("rdp_setothermode\n");

    if ((settings.ucode == 2) || (settings.ucode == 8))
    {
        int cmd0 = rdp.cmd0;
        F3DEX2_SETOTHERMODE(0xE2, 0, 32, rdp.cmd1);         // SETOTHERMODE_L
        F3DEX2_SETOTHERMODE(0xE3, 0, 32, cmd0 & 0x00FFFFFF);    // SETOTHERMODE_H
    }
    else
    {
        int cmd0 = rdp.cmd0;
        SETOTHERMODE(0xB9, 0, 32, rdp.cmd1);            // SETOTHERMODE_L
        SETOTHERMODE(0xBA, 0, 32, cmd0 & 0x00FFFFFF);       // SETOTHERMODE_H
    }
}

void load_palette (DWORD addr, WORD start, WORD count)
{
    RDP ("Loading palette... ");
    WORD *dpal = rdp.pal_8 + start;
    WORD end = start+count;
    //  WORD *spal = (WORD*)(gfx.RDRAM + (addr & BMASK));

    for (WORD i=start; i<end; i++)
    {
        *(dpal++) = *(WORD *)(gfx.RDRAM + (addr^2));
        addr += 2;

#ifdef TLUT_LOGGING
        FRDP ("%d: %08lx\n", i, *(WORD *)(gfx.RDRAM + (addr^2)));
#endif
    }
    start >>= 4;
    end = start + (count >> 4);
    for (WORD p = start; p < end; p++)
    {
      rdp.pal_8_crc[p] = CRC_Calculate( 0xFFFFFFFF, &rdp.pal_8[(p << 4)], 32 );
    }
    rdp.pal_256_crc = CRC_Calculate( 0xFFFFFFFF, rdp.pal_8_crc, 64 );
    RDP ("Done.\n");
}

static void rdp_loadtlut()
{
    DWORD tile = (rdp.cmd1 >> 24) & 0x07;
    WORD start = rdp.tiles[tile].t_mem - 256; // starting location in the palettes
    //  WORD start = ((WORD)(rdp.cmd1 >> 2) & 0x3FF) + 1;
    WORD count = ((WORD)(rdp.cmd1 >> 14) & 0x3FF) + 1;    // number to copy

    if (rdp.timg.addr + (count<<1) > BMASK)
        count = (WORD)((BMASK - rdp.timg.addr) >> 1);

    if (start+count > 256) count = 256-start;

    FRDP("loadtlut: tile: %d, start: %d, count: %d, from: %08lx\n", tile, start, count,
        rdp.timg.addr);

    load_palette (rdp.timg.addr, start, count);

    rdp.timg.addr += count << 1;
}

BOOL tile_set = 0;
static void rdp_settilesize()
{
    DWORD tile = (rdp.cmd1 >> 24) & 0x07;
    rdp.last_tile_size = tile;

    rdp.tiles[tile].f_ul_s = (float)((rdp.cmd0 >> 12) & 0xFFF) / 4.0f;
    rdp.tiles[tile].f_ul_t = (float)(rdp.cmd0 & 0xFFF) / 4.0f;

    int ul_s = (((WORD)(rdp.cmd0 >> 14)) & 0x03ff);
    int ul_t = (((WORD)(rdp.cmd0 >> 2 )) & 0x03ff);
    int lr_s = (((WORD)(rdp.cmd1 >> 14)) & 0x03ff);
    int lr_t = (((WORD)(rdp.cmd1 >> 2 )) & 0x03ff);

    if (lr_s == 0 && ul_s == 0)  //pokemon puzzle league set such tile size
        wrong_tile = tile;
    else if (wrong_tile == (int)tile)
        wrong_tile = -1;

    if (settings.use_sts1_only)
    {
        // ** USE FIRST SETTILESIZE ONLY **
        // This option helps certain textures while using the 'Alternate texture size method',
        //  but may break others.  (should help more than break)

        if (tile_set)
        {
            // coords in 10.2 format
            rdp.tiles[tile].ul_s = ul_s;
            rdp.tiles[tile].ul_t = ul_t;
            rdp.tiles[tile].lr_s = lr_s;
            rdp.tiles[tile].lr_t = lr_t;
            tile_set = 0;
        }
    }
    else
    {
        // coords in 10.2 format
        rdp.tiles[tile].ul_s = ul_s;
        rdp.tiles[tile].ul_t = ul_t;
        rdp.tiles[tile].lr_s = lr_s;
        rdp.tiles[tile].lr_t = lr_t;
    }

    // handle wrapping
    if (rdp.tiles[tile].lr_s < rdp.tiles[tile].ul_s) rdp.tiles[tile].lr_s += 0x400;
    if (rdp.tiles[tile].lr_t < rdp.tiles[tile].ul_t) rdp.tiles[tile].lr_t += 0x400;

    rdp.update |= UPDATE_TEXTURE;

    rdp.first = 1;

    if (tile == 0 && rdp.hires_tex)
      //if ((rdp.tiles[tile].size != 2) || ((rdp.timg.width == 1) && (rdp.hires_tex->width != (DWORD)(lr_s+1))))
      if (((rdp.tiles[tile].format == 0) && (rdp.tiles[tile].size != 2)) || ((rdp.timg.width == 1) && (rdp.hires_tex->width != (DWORD)(lr_s+1))))
        rdp.hires_tex = 0;
    if (rdp.hires_tex)
    {
      if (rdp.tiles[tile].format == 0 && rdp.hires_tex->format == 0)
      {
        if (tile == 1 && (DWORD)rdp.hires_tex->tmu != tile)
          SwapTextureBuffer();
        rdp.hires_tex->tile = tile;
        rdp.hires_tex->info.format = GR_TEXFMT_RGB_565;
        FRDP ("hires_tex: tile: %d\n", tile);
      }
      else if (tile == 0)
      {
        rdp.hires_tex->info.format = GR_TEXFMT_ALPHA_INTENSITY_88;
      }
    }
    FRDP ("settilesize: tile: %d, ul_s: %d, ul_t: %d, lr_s: %d, lr_t: %d\n",
        tile, ul_s, ul_t, lr_s, lr_t);
}

static void CopyswapBlock(int *pDst, unsigned int cnt, unsigned int SrcOffs)
{
    // copy and byteswap a block of 8-byte dwords
    int rem = SrcOffs & 3;
    if (rem == 0)
    {
        int *pSrc = (int *) ((uintptr_t) gfx.RDRAM + SrcOffs);
        for (unsigned int x = 0; x < cnt; x++)
        {
            int s1 = bswap32(*pSrc++);
            int s2 = bswap32(*pSrc++);
            *pDst++ = s1;
            *pDst++ = s2;
        }
    }
    else
    {
        // set source pointer to 4-byte aligned RDRAM location before the start
        int *pSrc = (int *) ((uintptr_t) gfx.RDRAM + (SrcOffs & 0xfffffffc));
        // do the first partial 32-bit word
        int s0 = bswap32(*pSrc++);
        for (int x = 0; x < rem; x++)
            s0 >>= 8;
        for (int x = 4; x > rem; x--)
        {
            *((char *) pDst) = s0 & 0xff;
            pDst = (int *) ((char *) pDst + 1);
            s0 >>= 8;
        }
        // do one full 32-bit word
        s0 = bswap32(*pSrc++);
        *pDst++ = s0;
        // do 'cnt-1' 64-bit dwords
        for (unsigned int x = 0; x < cnt-1; x++)
        {
            int s1 = bswap32(*pSrc++);
            int s2 = bswap32(*pSrc++);
            *pDst++ = s1;
            *pDst++ = s2;
        }
        // do last partial 32-bit word
        s0 = bswap32(*pSrc++);
        for (; rem > 0; rem--)
        {
            *((char *) pDst) = s0 & 0xff;
            pDst = (int *) ((char *) pDst + 1);
            s0 >>= 8;
        }
    }
}

static void WordswapBlock(int *pDst, unsigned int cnt, unsigned int TileSize)
{
    // Since it's not loading 32-bit textures as the N64 would, 32-bit textures need to
    // be swapped by 64-bits, not 32.
    if (TileSize == 3)
    {
        // swapblock64 dst, cnt
        for (unsigned int x = 0; x < cnt / 2; x++, pDst += 4)
        {
            long long s1 = ((long long *) pDst)[0];
            long long s2 = ((long long *) pDst)[1];
            ((long long *) pDst)[0] = s2;
            ((long long *) pDst)[1] = s1;
        }
    }
    else
    {
        // swapblock32 dst, cnt
        for (unsigned int x = 0; x < cnt; x++, pDst += 2)
        {
            int s1 = pDst[0];
            int s2 = pDst[1];
            pDst[0] = s2;
            pDst[1] = s1;
        }
    }
}

static void rdp_loadblock()
{
    if (rdp.skip_drawing)
    {
       RDP("loadblock skipped\n");
        return;
    }
    DWORD tile = (DWORD)((rdp.cmd1 >> 24) & 0x07);
    DWORD dxt = (DWORD)(rdp.cmd1 & 0x0FFF);

    rdp.addr[rdp.tiles[tile].t_mem] = rdp.timg.addr;

    // ** DXT is used for swapping every other line
    /*  double fdxt = (double)0x8000000F/(double)((DWORD)(2047/(dxt-1))); // F for error
    DWORD _dxt = (DWORD)fdxt;*/

    // 0x00000800 -> 0x80000000 (so we can check the sign bit instead of the 11th bit)
    DWORD _dxt = dxt << 20;

    DWORD addr = segoffset(rdp.timg.addr) & BMASK;

    // lr_s specifies number of 64-bit words to copy
    // 10.2 format
    WORD ul_s = (WORD)(rdp.cmd0 >> 14) & 0x3FF;
    WORD ul_t = (WORD)(rdp.cmd0 >>  2) & 0x3FF;
    WORD lr_s = (WORD)(rdp.cmd1 >> 14) & 0x3FF;

    rdp.tiles[tile].ul_s = ul_s;
    rdp.tiles[tile].ul_t = ul_t;
    rdp.tiles[tile].lr_s = lr_s;

    rdp.timg.set_by = 0;  // load block

    // do a quick boundary check before copying to eliminate the possibility for exception
    if (ul_s >= 512) {
        lr_s = 1;   // 1 so that it doesn't die on memcpy
        ul_s = 511;
    }
    if (ul_s+lr_s > 512)
        lr_s = 512-ul_s;

    if (addr+(lr_s<<3) > BMASK+1)
        lr_s = (WORD)((BMASK-addr)>>3);

    DWORD offs = rdp.timg.addr;
    DWORD cnt = lr_s+1;
    if (rdp.tiles[tile].size == 3)
        cnt <<= 1;
  //FIXME: unused? DWORD start_line = 0;

  //    if (lr_s > 0)
    rdp.timg.addr += cnt << 3;

    int * pDst = (int *) ((uintptr_t)rdp.tmem+(rdp.tiles[tile].t_mem<<3));

    // Load the block from RDRAM and byteswap it as it loads
    CopyswapBlock(pDst, cnt, offs);

    // now do 32-bit or 64-bit word swapping on every other row of data
    int dxt_accum = 0;
    while (cnt > 0)
    {
        // skip over unswapped blocks
        do
        {
            pDst += 2;
            if (--cnt == 0)
                break;
            dxt_accum += _dxt;
        } while (!(dxt_accum & 0x80000000));
        // count number of blocks to swap
        if (cnt == 0) break;
        int swapcnt = 0;
        do
        {
            swapcnt++;
            if (--cnt == 0)
                break;
            dxt_accum += _dxt;
        } while (dxt_accum & 0x80000000);
        // do 32-bit or 64-bit swap operation on this block
        WordswapBlock(pDst, swapcnt, rdp.tiles[tile].size);
        pDst += swapcnt * 2;
    }

    rdp.update |= UPDATE_TEXTURE;

    FRDP ("loadblock: tile: %d, ul_s: %d, ul_t: %d, lr_s: %d, dxt: %08lx -> %08lx\n",
        tile, ul_s, ul_t, lr_s,
        dxt, _dxt);
}

static void rdp_loadtile()
{
    if (rdp.skip_drawing)
        return;
    rdp.timg.set_by = 1;  // load tile

    DWORD tile = (DWORD)((rdp.cmd1 >> 24) & 0x07);
    if (rdp.tiles[tile].format == 1)
    {
        rdp.yuv_image = TRUE;
        if (rdp.timg.addr < rdp.yuv_im_begin) rdp.yuv_im_begin = rdp.timg.addr;
        return;
    }

    rdp.addr[rdp.tiles[tile].t_mem] = rdp.timg.addr;

    WORD ul_s = (WORD)((rdp.cmd0 >> 14) & 0x03FF);
    WORD ul_t = (WORD)((rdp.cmd0 >> 2 ) & 0x03FF);
    WORD lr_s = (WORD)((rdp.cmd1 >> 14) & 0x03FF);
    WORD lr_t = (WORD)((rdp.cmd1 >> 2 ) & 0x03FF);

    if (lr_s < ul_s || lr_t < ul_t) return;

    if (wrong_tile >= 0)  //there was a tile with zero length
    {
        rdp.tiles[wrong_tile].lr_s = lr_s;

        if (rdp.tiles[tile].size > rdp.tiles[wrong_tile].size)
            rdp.tiles[wrong_tile].lr_s <<= (rdp.tiles[tile].size - rdp.tiles[wrong_tile].size);
        else if (rdp.tiles[tile].size < rdp.tiles[wrong_tile].size)
            rdp.tiles[wrong_tile].lr_s >>= (rdp.tiles[wrong_tile].size - rdp.tiles[tile].size);
        rdp.tiles[wrong_tile].lr_t = lr_t;
        //     wrong_tile = -1;
    }

  if (rdp.hires_tex)// && (rdp.tiles[tile].format == 0))
  {
    FRDP("loadtile: hires_tex ul_s: %d, ul_t:%d\n", ul_s, ul_t);
      rdp.hires_tex->tile_uls = ul_s;
      rdp.hires_tex->tile_ult = ul_t;
    }

    if (settings.tonic && tile == 7)
    {
        rdp.tiles[0].ul_s = ul_s;
        rdp.tiles[0].ul_t = ul_t;
        rdp.tiles[0].lr_s = lr_s;
        rdp.tiles[0].lr_t = lr_t;
    }

    DWORD height = lr_t - ul_t + 1;   // get height
    DWORD width = lr_s - ul_s + 1;

    DWORD wid_64 = rdp.tiles[tile].line;

    // CHEAT: it's very unlikely that it loads more than 1 32-bit texture in one command,
    //   so i don't bother to write in two different places at once.  Just load once with
    //   twice as much data.
    if (rdp.tiles[tile].size == 3)
        wid_64 <<= 1;

    int line_n = rdp.timg.width;
    if (rdp.tiles[tile].size == 0)
        line_n >>= 1;
    else
        line_n <<= (rdp.tiles[tile].size-1);

    int offs = ul_t * line_n;
  offs += ul_s << rdp.tiles[tile].size >> 1;
    offs += rdp.timg.addr;
  if ((unsigned int) offs >= BMASK)
    return;

    // check if points to bad location
  DWORD size = width * height;
    if (rdp.tiles[tile].size == 0)
        size >>= 1;
    else
        size <<= (rdp.tiles[tile].size-1);

  if (offs + line_n*height > BMASK)
    height = (BMASK - offs) / line_n;

    int * pDst = (int *) ((uintptr_t)rdp.tmem+(rdp.tiles[tile].t_mem<<3));
    int * pEnd = (int *) ((uintptr_t)rdp.tmem+4096 - (wid_64<<3));

    for (unsigned int y = 0; y < height; y++)
    {
        if (pDst > pEnd) break;
        CopyswapBlock(pDst, wid_64, offs);
        if (y & 1)
        {
            WordswapBlock(pDst, wid_64, rdp.tiles[tile].size);
        }
        pDst += wid_64 * 2;
        offs += line_n;
    }

    FRDP("loadtile: tile: %d, ul_s: %d, ul_t: %d, lr_s: %d, lr_t: %d\n", tile,
        ul_s, ul_t, lr_s, lr_t);
}

static void rdp_settile()
{
    tile_set = 1; // used to check if we only load the first settilesize

    rdp.first = 0;

    //rdp.cur_tile_n = (DWORD)((rdp.cmd1 >> 24) & 0x07);
    //rdp.cur_tile = &rdp.tiles[rdp.cur_tile_n];

    rdp.last_tile = (DWORD)((rdp.cmd1 >> 24) & 0x07);
    TILE *tile = &rdp.tiles[rdp.last_tile];

    tile->format = (BYTE)((rdp.cmd0 >> 21) & 0x07);
    tile->size = (BYTE)((rdp.cmd0 >> 19) & 0x03);
    tile->line = (WORD)((rdp.cmd0 >> 9) & 0x01FF);
    tile->t_mem = (WORD)(rdp.cmd0 & 0x1FF);
    tile->palette = (BYTE)((rdp.cmd1 >> 20) & 0x0F);
    tile->clamp_t = (BYTE)((rdp.cmd1 >> 19) & 0x01);
    tile->mirror_t = (BYTE)((rdp.cmd1 >> 18) & 0x01);
    tile->mask_t = (BYTE)((rdp.cmd1 >> 14) & 0x0F);
    tile->shift_t = (BYTE)((rdp.cmd1 >> 10) & 0x0F);
    tile->clamp_s = (BYTE)((rdp.cmd1 >> 9) & 0x01);
    tile->mirror_s = (BYTE)((rdp.cmd1 >> 8) & 0x01);
    tile->mask_s = (BYTE)((rdp.cmd1 >> 4) & 0x0F);
    tile->shift_s = (BYTE)(rdp.cmd1 & 0x0F);

    rdp.update |= UPDATE_TEXTURE;

    FRDP ("settile: tile: %d, format: %s, size: %s, line: %d, "
        "t_mem: %08lx, palette: %d, clamp_t/mirror_t: %s, mask_t: %d, "
        "shift_t: %d, clamp_s/mirror_s: %s, mask_s: %d, shift_s: %d\n",
        rdp.last_tile, str_format[tile->format], str_size[tile->size], tile->line,
        tile->t_mem, tile->palette, str_cm[(tile->clamp_t<<1)|tile->mirror_t], tile->mask_t,
        tile->shift_t, str_cm[(tile->clamp_s<<1)|tile->mirror_s], tile->mask_s, tile->shift_s);
}

//
// fillrect - fills a rectangle
//

static void rdp_fillrect()
{
  DWORD ul_x = ((rdp.cmd1 & 0x00FFF000) >> 14);
  DWORD ul_y = (rdp.cmd1 & 0x00000FFF) >> 2;
  DWORD lr_x = ((rdp.cmd0 & 0x00FFF000) >> 14) + 1;
  DWORD lr_y = ((rdp.cmd0 & 0x00000FFF) >> 2) + 1;
    if ((rdp.cimg == rdp.zimg) || (settings.fb_smart && rdp.frame_buffers[rdp.ci_count-1].status == ci_zimg))
    {
        RDP ("Fillrect - cleared the depth buffer\n");
        if (fullscreen)
        {

            grDepthMask (FXTRUE);
            grColorMask (FXFALSE, FXFALSE);
            grBufferClear (0, 0, 0xFFFF);
            grColorMask (FXTRUE, FXTRUE);
            rdp.update |= UPDATE_ZBUF_ENABLED;
            if (settings.fb_depth_clear)
            {
        ul_x = min(max(ul_x, rdp.scissor_o.ul_x), rdp.scissor_o.lr_x);
        lr_x = min(max(lr_x, rdp.scissor_o.ul_x), rdp.scissor_o.lr_x);
        ul_y = min(max(ul_y, rdp.scissor_o.ul_y), rdp.scissor_o.lr_y);
        lr_y = min(max(lr_y, rdp.scissor_o.ul_y), rdp.scissor_o.lr_y);
        //FIXME:unused? DWORD zi_height = lr_y - ul_y - 1;
        //              rdp.zi_nb_pixels = rdp.zi_width * zi_height;
        rdp.zi_lry = lr_y - 1;
        rdp.zi_lrx = lr_x - 1;
        //              FRDP ("zi_width: %d, zi_height: %d\n", rdp.zi_width, zi_height);
        DWORD fillrect_width_in_dwords = (lr_x-ul_x) >> 1;
        DWORD zi_width_in_dwords = rdp.zi_width >> 1;
        ul_x >>= 1;
                DWORD * dst = (DWORD*)(gfx.RDRAM+rdp.cimg);
        dst += ul_y * zi_width_in_dwords;
        for (DWORD y = ul_y; y < lr_y; y++)
        {
          for (DWORD x = ul_x; x < fillrect_width_in_dwords; x++)
          {
            dst[x] = rdp.fill_color;
          }
          dst += zi_width_in_dwords;
        }
            }
        }
        return;
    }

    if (rdp.skip_drawing)
    {
        RDP("Fillrect skipped\n");
        return;
    }

    // Update scissor
    update_scissor ();

    if ((ul_x > lr_x) || (ul_y > lr_y)) return;
    if (settings.bomberman64 && (lr_x == rdp.ci_width) && (rdp.cimg == rdp.ocimg)) //bomberman64 hack
        return;

  if (rdp.cur_image && (rdp.cur_image->format != 0) && (rdp.cycle_mode == 3) && (rdp.cur_image->width == lr_x))
  {
    DWORD color = rdp.fill_color;
    color = ((color&1)?0xFF:0) |
      ((DWORD)((float)((color&0xF800) >> 11) / 31.0f * 255.0f) << 24) |
      ((DWORD)((float)((color&0x07C0) >> 6) / 31.0f * 255.0f) << 16) |
      ((DWORD)((float)((color&0x003E) >> 1) / 31.0f * 255.0f) << 8);
        grDepthMask (FXFALSE);
        grBufferClear (color, 0, 0xFFFF);
        grDepthMask (FXTRUE);
        rdp.update |= UPDATE_ZBUF_ENABLED;
        return;
    }

  if (settings.decrease_fillrect_edge && rdp.cycle_mode == 0)
    {
        lr_x--; lr_y--;
    }
    FRDP("fillrect (%d,%d) -> (%d,%d), cycle mode: %d, #%d, #%d\n", ul_x, ul_y, lr_x, lr_y, rdp.cycle_mode,
        rdp.tri_n, rdp.tri_n+1);

    FRDP("scissor (%d,%d) -> (%d,%d)\n", rdp.scissor.ul_x, rdp.scissor.ul_y, rdp.scissor.lr_x,
        rdp.scissor.lr_y);

    // KILL the floating point error with 0.01f
    DWORD s_ul_x = (DWORD)min(max(ul_x * rdp.scale_x + rdp.offset_x + 0.01f, rdp.scissor.ul_x), rdp.scissor.lr_x);
    DWORD s_lr_x = (DWORD)min(max(lr_x * rdp.scale_x + rdp.offset_x + 0.01f, rdp.scissor.ul_x), rdp.scissor.lr_x);
    DWORD s_ul_y = (DWORD)min(max(ul_y * rdp.scale_y + rdp.offset_y + 0.01f, rdp.scissor.ul_y), rdp.scissor.lr_y);
    DWORD s_lr_y = (DWORD)min(max(lr_y * rdp.scale_y + rdp.offset_y + 0.01f, rdp.scissor.ul_y), rdp.scissor.lr_y);

    if (s_lr_x < 0.0f) s_lr_x = 0;
    if (s_lr_y < 0.0f) s_lr_y = 0;
    if (s_ul_x > (float)settings.res_x) s_ul_x = settings.res_x;
    if (s_ul_y > (float)settings.res_y) s_ul_y = settings.res_y;

    FRDP (" - %d, %d, %d, %d\n", s_ul_x, s_ul_y, s_lr_x, s_lr_y);

    if (fullscreen)
    {
        grFogMode (GR_FOG_DISABLE);

        grClipWindow (0, 0, settings.res_x, settings.res_y);

    float Z = 1.0f;
    if (rdp.zsrc == 1 && (rdp.othermode_l & 0x00000030))
    {
            Z = ScaleZ(rdp.prim_depth);
        grDepthBufferFunction (GR_CMP_LEQUAL);
        //          grDepthMask (FXTRUE);
        FRDP ("prim_depth = %d\n", rdp.prim_depth);
    }
    else
    {
      grDepthBufferFunction (GR_CMP_ALWAYS);
      grDepthMask (FXFALSE);
            RDP ("no prim_depth used, using 1.0\n");
    }
        // Draw the rectangle
        VERTEX v[4] = {
      { (float)s_ul_x, (float)s_ul_y, Z, 1.0f,  0,0,0,0, { 0,0,0,0 }, 0,0, 0,0,0,0 },
      { (float)s_lr_x, (float)s_ul_y, Z, 1.0f,  0,0,0,0, { 0,0,0,0 }, 0,0, 0,0,0,0 },
      { (float)s_ul_x, (float)s_lr_y, Z, 1.0f,  0,0,0,0, { 0,0,0,0 }, 0,0, 0,0,0,0 },
      { (float)s_lr_x, (float)s_lr_y, Z, 1.0f,  0,0,0,0, { 0,0,0,0 }, 0,0, 0,0,0,0 } };

            if (rdp.cycle_mode == 3)
            {
        DWORD color = (settings.fillcolor_fix) ? rdp.fill_color : (rdp.fill_color >> 16);

        if (settings.PM && rdp.frame_buffers[rdp.ci_count-1].status == ci_aux)
        {
          //background of auxilary frame buffers must have zero alpha.
          //make it black, set 0 alpha to plack pixels on frame buffer read
          color = 0;
        }
        else
        {
          color = ((color&1)?0xFF:0) |
            ((DWORD)((float)((color&0xF800) >> 11) / 31.0f * 255.0f) << 24) |
            ((DWORD)((float)((color&0x07C0) >> 6) / 31.0f * 255.0f) << 16) |
            ((DWORD)((float)((color&0x003E) >> 1) / 31.0f * 255.0f) << 8);
        }
                grConstantColorValue (color);

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

                grAlphaBlendFunction (GR_BLEND_ONE, GR_BLEND_ZERO, GR_BLEND_ONE, GR_BLEND_ZERO);

                rdp.update |= UPDATE_COMBINE;
            }
            else
            {
                Combine ();
                TexCache ();  // (to update combiner)
        DWORD cmb_mode_c = (rdp.cycle1 << 16) | (rdp.cycle2 & 0xFFFF);
        DWORD cmb_mode_a = (rdp.cycle1 & 0x0FFF0000) | ((rdp.cycle2 >> 16) & 0x00000FFF);
        if (cmb_mode_c == 0x9fff9fff || cmb_mode_a == 0x09ff09ff) //shade
        {
          AllowShadeMods (v, 4);
          for (int k = 0; k < 4; k++)
            apply_shade_mods (&v[k]);
            }
            }

            grAlphaTestFunction (GR_CMP_ALWAYS);
      if (grStippleModeExt)
        grStippleModeExt(GR_STIPPLE_DISABLE);

            grCullMode(GR_CULL_DISABLE);

            if (settings.wireframe)
            {
                SetWireframeCol ();
                grDrawLine (&v[0], &v[2]);
                grDrawLine (&v[2], &v[1]);
                grDrawLine (&v[1], &v[0]);
                grDrawLine (&v[2], &v[3]);
                grDrawLine (&v[3], &v[1]);
                //grDrawLine (&v[1], &v[2]);
            }
            else
            {
                grDrawTriangle (&v[0], &v[2], &v[1]);
                grDrawTriangle (&v[2], &v[3], &v[1]);
            }

            if (debug.capture)
            {
                VERTEX v1[3];
                v1[0] = v[0];
                v1[1] = v[2];
                v1[2] = v[1];
                add_tri (v1, 3, TRI_FILLRECT);
                rdp.tri_n ++;
                v1[0] = v[2];
                v1[1] = v[3];
                add_tri (v1, 3, TRI_FILLRECT);
                rdp.tri_n ++;
            }
            else
                rdp.tri_n += 2;

      if (settings.fog && (rdp.flags & FOG_ENABLED))
      {
        grFogMode (GR_FOG_WITH_TABLE_ON_FOGCOORD_EXT);
      }

            rdp.update |= UPDATE_CULL_MODE | UPDATE_ALPHA_COMPARE | UPDATE_ZBUF_ENABLED;
  }
  else
  {
      rdp.tri_n += 2;
  }
}

//
// setfillcolor - sets the filling color
//

static void rdp_setfillcolor()
{
  rdp.fill_color = rdp.cmd1;
    rdp.update |= UPDATE_ALPHA_COMPARE | UPDATE_COMBINE;

    FRDP("setfillcolor: %08lx\n", rdp.cmd1);
}

static void rdp_setfogcolor()
{
  rdp.fog_color = rdp.cmd1;
    rdp.update |= UPDATE_COMBINE | UPDATE_FOG_ENABLED;

    FRDP("setfogcolor - %08lx\n", rdp.cmd1);
}

static void rdp_setblendcolor()
{
  rdp.blend_color = rdp.cmd1;
    rdp.update |= UPDATE_COMBINE;

    FRDP("setblendcolor: %08lx\n", rdp.cmd1);
}

static void rdp_setprimcolor()
{
  rdp.prim_color = rdp.cmd1;
    rdp.prim_lodmin = (rdp.cmd0 >> 8) & 0xFF;
  rdp.prim_lodfrac = max(rdp.cmd0 & 0xFF, rdp.prim_lodmin);
    rdp.update |= UPDATE_COMBINE;

    FRDP("setprimcolor: %08lx, lodmin: %d, lodfrac: %d\n", rdp.cmd1, rdp.prim_lodmin,
        rdp.prim_lodfrac);
}

static void rdp_setenvcolor()
{
  rdp.env_color = rdp.cmd1;
    rdp.update |= UPDATE_COMBINE;

    FRDP("setenvcolor: %08lx\n", rdp.cmd1);
}

static void rdp_setcombine()
{
    rdp.c_a0  = (BYTE)((rdp.cmd0 >> 20) & 0xF);
    rdp.c_b0  = (BYTE)((rdp.cmd1 >> 28) & 0xF);
    rdp.c_c0  = (BYTE)((rdp.cmd0 >> 15) & 0x1F);
    rdp.c_d0  = (BYTE)((rdp.cmd1 >> 15) & 0x7);
    rdp.c_Aa0 = (BYTE)((rdp.cmd0 >> 12) & 0x7);
    rdp.c_Ab0 = (BYTE)((rdp.cmd1 >> 12) & 0x7);
    rdp.c_Ac0 = (BYTE)((rdp.cmd0 >> 9)  & 0x7);
    rdp.c_Ad0 = (BYTE)((rdp.cmd1 >> 9)  & 0x7);

    rdp.c_a1  = (BYTE)((rdp.cmd0 >> 5)  & 0xF);
    rdp.c_b1  = (BYTE)((rdp.cmd1 >> 24) & 0xF);
    rdp.c_c1  = (BYTE)((rdp.cmd0 >> 0)  & 0x1F);
    rdp.c_d1  = (BYTE)((rdp.cmd1 >> 6)  & 0x7);
    rdp.c_Aa1 = (BYTE)((rdp.cmd1 >> 21) & 0x7);
    rdp.c_Ab1 = (BYTE)((rdp.cmd1 >> 3)  & 0x7);
    rdp.c_Ac1 = (BYTE)((rdp.cmd1 >> 18) & 0x7);
    rdp.c_Ad1 = (BYTE)((rdp.cmd1 >> 0)  & 0x7);

    rdp.cycle1 = (rdp.c_a0<<0)  | (rdp.c_b0<<4)  | (rdp.c_c0<<8)  | (rdp.c_d0<<13)|
        (rdp.c_Aa0<<16)| (rdp.c_Ab0<<19)| (rdp.c_Ac0<<22)| (rdp.c_Ad0<<25);
    rdp.cycle2 = (rdp.c_a1<<0)  | (rdp.c_b1<<4)  | (rdp.c_c1<<8)  | (rdp.c_d1<<13)|
        (rdp.c_Aa1<<16)| (rdp.c_Ab1<<19)| (rdp.c_Ac1<<22)| (rdp.c_Ad1<<25);

    rdp.update |= UPDATE_COMBINE;

    FRDP("setcombine\na0=%s b0=%s c0=%s d0=%s\nAa0=%s Ab0=%s Ac0=%s Ad0=%s\na1=%s b1=%s c1=%s d1=%s\nAa1=%s Ab1=%s Ac1=%s Ad1=%s\n",
        Mode0[rdp.c_a0], Mode1[rdp.c_b0], Mode2[rdp.c_c0], Mode3[rdp.c_d0],
        Alpha0[rdp.c_Aa0], Alpha1[rdp.c_Ab0], Alpha2[rdp.c_Ac0], Alpha3[rdp.c_Ad0],
        Mode0[rdp.c_a1], Mode1[rdp.c_b1], Mode2[rdp.c_c1], Mode3[rdp.c_d1],
        Alpha0[rdp.c_Aa1], Alpha1[rdp.c_Ab1], Alpha2[rdp.c_Ac1], Alpha3[rdp.c_Ad1]);
}

//
// settextureimage - sets the source for an image copy
//

static void rdp_settextureimage()
{
    static const char *format[]   = { "RGBA", "YUV", "CI", "IA", "I", "?", "?", "?" };
    static const char *size[]     = { "4bit", "8bit", "16bit", "32bit" };

    rdp.timg.format = (BYTE)((rdp.cmd0 >> 21) & 0x07);
    rdp.timg.size = (BYTE)((rdp.cmd0 >> 19) & 0x03);
    rdp.timg.width = (WORD)(1 + (rdp.cmd0 & 0x00000FFF));
    rdp.timg.addr = segoffset(rdp.cmd1);
    rdp.s2dex_tex_loaded = TRUE;
    rdp.update |= UPDATE_TEXTURE;

  if (rdp.frame_buffers[rdp.ci_count-1].status == ci_copy_self && (rdp.timg.addr >= rdp.cimg) && (rdp.timg.addr < rdp.ci_end))
        {
            if (!rdp.fb_drawn)
            {
            if (!rdp.cur_image)
          CopyFrameBuffer();
        else if (rdp.frame_buffers[rdp.ci_count].status != ci_copy)
          CloseTextureBuffer(TRUE);
                rdp.fb_drawn = TRUE;
            }
    }

    if (settings.fb_hires) //search this texture among drawn texture buffers
    {
      if (settings.zelda)
      {
         if (rdp.timg.size == 2)
           FindTextureBuffer(rdp.timg.addr, rdp.timg.width);
      }
      else
           FindTextureBuffer(rdp.timg.addr, rdp.timg.width);
    }

    FRDP("settextureimage: format: %s, size: %s, width: %d, addr: %08lx\n",
        format[rdp.timg.format], size[rdp.timg.size],
        rdp.timg.width, rdp.timg.addr);
}

static void rdp_setdepthimage()
{
    rdp.zimg = segoffset(rdp.cmd1) & BMASK;
  rdp.zi_width = rdp.ci_width;
    FRDP("setdepthimage - %08lx\n", rdp.zimg);
}


BOOL SwapOK = TRUE;
static void RestoreScale()
{
    FRDP("Return to original scale: x = %f, y = %f\n", rdp.scale_x_bak, rdp.scale_y_bak);
    rdp.scale_x = rdp.scale_x_bak;
    rdp.scale_y = rdp.scale_y_bak;
  //    update_scissor();
  rdp.view_scale[0] *= rdp.scale_x;
  rdp.view_scale[1] *= rdp.scale_y;
  rdp.view_trans[0] *= rdp.scale_x;
  rdp.view_trans[1] *= rdp.scale_y;
  rdp.update |= UPDATE_VIEWPORT | UPDATE_SCISSOR;
  //*
  if (fullscreen)
  {
    grDepthMask (FXFALSE);
    grBufferClear (0, 0, 0xFFFF);
    grDepthMask (FXTRUE);
  }
  //*/
}

static DWORD swapped_addr = 0;

static void rdp_setcolorimage()
{
  render_depth_mode = 0;
  if (settings.fb_smart && (rdp.num_of_ci < NUMTEXBUF))
    {
    COLOR_IMAGE & cur_fb = rdp.frame_buffers[rdp.ci_count];
    COLOR_IMAGE & prev_fb = rdp.frame_buffers[rdp.ci_count-1];
    COLOR_IMAGE & next_fb = rdp.frame_buffers[rdp.ci_count+1];
    switch (cur_fb.status)
        {
        case ci_main:
            {

                if (rdp.ci_count == 0)
                {
                    if (rdp.ci_status == ci_aux) //for PPL
                    {
                        float sx = rdp.scale_x;
                        float sy = rdp.scale_y;
                        rdp.scale_x = 1.0f;
                        rdp.scale_y = 1.0f;
                CopyFrameBuffer ();
                        rdp.scale_x = sx;
                        rdp.scale_y = sy;
                    }
                    if (!settings.fb_hires)
                    {
                        if ((rdp.num_of_ci > 1) &&
                  (next_fb.status == ci_aux) &&
                  (next_fb.width >= cur_fb.width))
                        {
                            rdp.scale_x = 1.0f;
                            rdp.scale_y = 1.0f;
                        }
                    }
              else if (rdp.copy_ci_index && settings.PM) //tidal wave
                OpenTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
                }
                else if (!rdp.motionblur && settings.fb_hires && !SwapOK && (rdp.ci_count <= rdp.copy_ci_index))
        {
          if (next_fb.status == ci_aux_copy)
            OpenTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
          else
                  OpenTextureBuffer(rdp.frame_buffers[rdp.copy_ci_index]);
        }
        else if (settings.fb_hires && rdp.read_whole_frame && prev_fb.status == ci_aux)
        {
          OpenTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
        }
                //else if (rdp.ci_status == ci_aux && !rdp.copy_ci_index)
                //  CloseTextureBuffer();

                rdp.skip_drawing = FALSE;
            }
            break;
        case ci_copy:
            {
                if (!rdp.motionblur || settings.fb_motionblur)
                {
          if (cur_fb.width == rdp.ci_width)
          {
            if (CopyTextureBuffer(prev_fb, cur_fb))
              //                      if (CloseTextureBuffer(TRUE))
                      ;
            else
                    {
              if (!rdp.fb_drawn || prev_fb.status == ci_copy_self)
                        {
                CopyFrameBuffer ();
                            rdp.fb_drawn = TRUE;
                        }
              memcpy(gfx.RDRAM+cur_fb.addr,gfx.RDRAM+rdp.cimg, (cur_fb.width*cur_fb.height)<<cur_fb.size>>1);
                    }
          }
          else
                    {
            CloseTextureBuffer(TRUE);
          }
        }
        else
        {
          memset(gfx.RDRAM+cur_fb.addr, 0, cur_fb.width*cur_fb.height*rdp.ci_size);
        }
        rdp.skip_drawing = TRUE;
      }
      break;
    case ci_aux_copy:
      {
                    rdp.skip_drawing = FALSE;
        if (CloseTextureBuffer(prev_fb.status != ci_aux_copy))
          ;
        else if (!rdp.fb_drawn)
        {
          CopyFrameBuffer ();
          rdp.fb_drawn = TRUE;
                    }
        if (settings.fb_hires)
          OpenTextureBuffer(cur_fb);
            }
            break;
        case ci_old_copy:
            {
                if (!rdp.motionblur || settings.fb_motionblur)
                {
          if (cur_fb.width == rdp.ci_width)
                    {
            memcpy(gfx.RDRAM+cur_fb.addr,gfx.RDRAM+rdp.maincimg[1].addr, (cur_fb.width*cur_fb.height)<<cur_fb.size>>1);
                    }
                    //rdp.skip_drawing = TRUE;
                }
                else
                {
          memset(gfx.RDRAM+cur_fb.addr, 0, (cur_fb.width*cur_fb.height)<<rdp.ci_size>>1);
                }
            }
            break;
            /*
            else if (rdp.frame_buffers[rdp.ci_count].status == ci_main_i)
            {
      //       CopyFrameBuffer ();
            rdp.scale_x = rdp.scale_x_bak;
            rdp.scale_y = rdp.scale_y_bak;
            rdp.skip_drawing = FALSE;
            }
            */
        case ci_aux:
            {
        if (!settings.fb_hires && cur_fb.format != 0)
                    rdp.skip_drawing = TRUE;
                else
                {
                    rdp.skip_drawing = FALSE;
          if (settings.fb_hires && OpenTextureBuffer(cur_fb))
                      ;
                    else
                    {
                        if (cur_fb.format != 0)
                  rdp.skip_drawing = TRUE;
                        if (rdp.ci_count == 0)
                        {
                            //           if (rdp.num_of_ci > 1)
                            //           {
                            rdp.scale_x = 1.0f;
                            rdp.scale_y = 1.0f;
                            //           }
                        }
                else if (!settings.fb_hires && (prev_fb.status == ci_main) &&
                  (prev_fb.width == cur_fb.width)) // for Pokemon Stadium
                  CopyFrameBuffer ();
                    }
                }
        cur_fb.status = ci_aux;
            }
            break;
        case ci_zimg:
          // ZIGGY
          // Zelda LoT effect save/restore depth buffer
          if (cur_fb.addr == rdp.zimg) {
            render_depth_mode = 1;
          } else {
            render_depth_mode = 2;
          }
          rdp.skip_drawing = TRUE;
          break;
        case ci_useless:
            //case ci_zcopy:
            rdp.skip_drawing = TRUE;
            break;
        case ci_copy_self:
      if (settings.fb_hires && (rdp.ci_count <= rdp.copy_ci_index) && (!SwapOK || settings.swapmode == 2))
        OpenTextureBuffer(cur_fb);
            rdp.skip_drawing = FALSE;
            /*
            if (settings.fb_hires)
            {
              if (SwapOK)
              {
                rdp.cimg = rdp.frame_buffers[rdp.ci_count].addr;
      rdp.maincimg[0].addr = rdp.cimg;
                newSwapBuffers();
                SwapOK = FALSE;
                OpenTextureBuffer(rdp.frame_buffers[rdp.ci_count]);
              }
            }
            */
            break;
        default:
            rdp.skip_drawing = FALSE;
        }

    if ((rdp.ci_count > 0) && (prev_fb.status >= ci_aux)) //for Pokemon Stadium
        {
      if (!settings.fb_hires && prev_fb.format == 0)
        CopyFrameBuffer ();
        }
    if (!settings.fb_hires && cur_fb.status == ci_copy)
        {
      if (!rdp.motionblur && (rdp.num_of_ci > rdp.ci_count+1) && (next_fb.status != ci_aux))
            {
        RestoreScale();
            }
        }
    if (!settings.fb_hires && cur_fb.status == ci_aux)
        {
      if (cur_fb.format == 0)
            {
                if (settings.PPL && (rdp.scale_x < 1.1f))  //need to put current image back to frame buffer
                {
          int width = cur_fb.width;
          int height = cur_fb.height;
                    WORD *ptr_dst = new WORD[width*height];
          WORD *ptr_src = (WORD*)(gfx.RDRAM+cur_fb.addr);
                    WORD c;

                    for (int y=0; y<height; y++)
                    {
                        for (int x=0; x<width; x++)
                        {
                            c = ((ptr_src[(x + y * width)^1]) >> 1) | 0x8000;
                            ptr_dst[x + y * width] = c;
                        }
                    }
                    grLfbWriteRegion(GR_BUFFER_BACKBUFFER,
                        0,
                        0,
                        GR_LFB_SRC_FMT_555,
                        width,
                        height,
                        FXFALSE,
                        width<<1,
                        ptr_dst);
                    delete[] ptr_dst;
                }
                /*
                else  //just clear buffer
                {

                  grColorMask(FXTRUE, FXTRUE);
                  grBufferClear (0, 0, 0xFFFF);
                  }
                */
            }
        }

    if ((cur_fb.status == ci_main) && (rdp.ci_count > 0))
        {
            BOOL to_org_res = TRUE;
            for (int i = rdp.ci_count + 1; i < rdp.num_of_ci; i++)
            {
        if ((rdp.frame_buffers[i].status != ci_main) && (rdp.frame_buffers[i].status != ci_zimg) && (rdp.frame_buffers[i].status != ci_zcopy))
                {
                    to_org_res = FALSE;
                    break;
                }
            }
            if (to_org_res)
            {
        RDP("return to original scale\n");
                rdp.scale_x = rdp.scale_x_bak;
                rdp.scale_y = rdp.scale_y_bak;
        if (settings.fb_hires && !rdp.read_whole_frame)
                  CloseTextureBuffer();
            }
      if (settings.fb_hires && !rdp.read_whole_frame && (prev_fb.status >= ci_aux) && (rdp.ci_count > rdp.copy_ci_index))
                  CloseTextureBuffer();

        }
    rdp.ci_status = cur_fb.status;
        rdp.ci_count++;
  }

  rdp.ocimg = rdp.cimg;
  rdp.cimg = segoffset(rdp.cmd1) & BMASK;
  rdp.ci_width = (rdp.cmd0 & 0xFFF) + 1;
  if (settings.fb_smart)
    rdp.ci_height = rdp.frame_buffers[rdp.ci_count-1].height;
  else if (rdp.ci_width == 32)
    rdp.ci_height = 32;
  else
    rdp.ci_height = rdp.scissor_o.lr_y;
  if (rdp.zimg == rdp.cimg)
  {
    rdp.zi_width = rdp.ci_width;
    //    int zi_height = min((int)rdp.zi_width*3/4, (int)rdp.vi_height);
    //    rdp.zi_words = rdp.zi_width * zi_height;
  }
  DWORD format = (rdp.cmd0 >> 21) & 0x7;
  rdp.ci_size = (rdp.cmd0 >> 19) & 0x3;
  rdp.ci_end = rdp.cimg + ((rdp.ci_width*rdp.ci_height)<<(rdp.ci_size-1));
  FRDP("setcolorimage - %08lx, width: %d,  height: %d, format: %d, size: %d\n", rdp.cmd1, rdp.ci_width, rdp.ci_height, format, rdp.ci_size);
  FRDP("cimg: %08lx, ocimg: %08lx, SwapOK: %d\n", rdp.cimg, rdp.ocimg, SwapOK);

  if (format != 0 && !rdp.cur_image) //can't draw into non RGBA buffer
  {
      if (settings.fb_hires && rdp.ci_width <= 64)
        OpenTextureBuffer(rdp.frame_buffers[rdp.ci_count - 1]);
      else if (format > 2)
        rdp.skip_drawing = TRUE;
      return;
  }
  else
  {
      if (!settings.fb_smart)
          rdp.skip_drawing = FALSE;
  }

  CI_SET = TRUE;
  if (settings.swapmode > 0)
  {
      if (rdp.zimg == rdp.cimg)
          rdp.updatescreen = 1;

    BOOL viSwapOK = ((settings.swapmode == 2) && (rdp.vi_org_reg == *gfx.VI_ORIGIN_REG)) ? FALSE : TRUE;
      if ((rdp.zimg != rdp.cimg) && (rdp.ocimg != rdp.cimg) && SwapOK && viSwapOK && !rdp.cur_image)
      {
          if (settings.fb_smart)
        rdp.maincimg[0] = rdp.frame_buffers[rdp.main_ci_index];
          else
        rdp.maincimg[0].addr = rdp.cimg;
      rdp.last_drawn_ci_addr = (settings.swapmode == 2) ? swapped_addr : rdp.maincimg[0].addr;
      swapped_addr = rdp.cimg;
          newSwapBuffers();
      rdp.vi_org_reg = *gfx.VI_ORIGIN_REG;
          SwapOK = FALSE;
      if (settings.fb_hires)
          {
        if (rdp.copy_ci_index && (rdp.frame_buffers[rdp.ci_count-1].status != ci_zimg))
        {
                int idx = (rdp.frame_buffers[rdp.ci_count].status == ci_aux_copy) ? rdp.main_ci_index : rdp.copy_ci_index;
            FRDP("attempt open tex buffer. status: %s, addr: %08lx\n", CIStatus[rdp.frame_buffers[idx].status], rdp.frame_buffers[idx].addr);
            OpenTextureBuffer(rdp.frame_buffers[idx]);
            if (rdp.frame_buffers[rdp.copy_ci_index].status == ci_main) //tidal wave
              rdp.copy_ci_index = 0;
          }
        else if (rdp.read_whole_frame && !rdp.cur_image)
        {
          OpenTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
            }
      }
      }
  }
}

static void rdp_trifill()
{
    RDP_E("trifill - IGNORED\n");
    RDP("trifill - IGNORED\n");
}

static void rdp_trishade()
{
    RDP_E("trishade - IGNORED\n");
    RDP("trishade - IGNORED\n");
}

static void rdp_tritxtr()
{
    RDP_E("tritxtr - IGNORED\n");
    RDP("tritxtr - IGNORED\n");
}

static void rdp_trishadetxtr()
{
    RDP_E("trishadetxtr - IGNORED\n");
    RDP("trishadetxtr - IGNORED\n");
}

static void rdp_trifillz()
{
    RDP_E("trifillz - IGNORED\n");
    RDP("trifillz - IGNORED\n");
}

static void rdp_trishadez()
{
    RDP_E("trishadez - IGNORED\n");
    RDP("trishadez - IGNORED\n");
}

static void rdp_tritxtrz()
{
    RDP_E("tritxtrz - IGNORED\n");
    RDP("tritxtrz - IGNORED\n");
}

static void rdp_trishadetxtrz()
{
    RDP_E("trishadetxtrz - IGNORED\n");
    RDP("trishadetxtrz - IGNORED\n");
}

static void rsp_reserved0()
{
    RDP_E("reserved0 - IGNORED\n");
    RDP("reserved0 - IGNORED\n");
}

static void rsp_reserved1()
{
    RDP("reserved1 - ignored\n");
}

static void rsp_reserved2()
{
    RDP("reserved2\n");
}

static void rsp_reserved3()
{
    RDP("reserved3 - ignored\n");
}

void SetWireframeCol ()
{
    if (!fullscreen) return;

    switch (settings.wfmode)
    {
        //case 0: // normal colors, don't do anything
    case 1: // vertex colors
        grColorCombine (GR_COMBINE_FUNCTION_LOCAL,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_LOCAL_ITERATED,
            GR_COMBINE_OTHER_NONE,
            FXFALSE);
        grAlphaCombine (GR_COMBINE_FUNCTION_LOCAL,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_LOCAL_ITERATED,
            GR_COMBINE_OTHER_NONE,
            FXFALSE);
        grAlphaBlendFunction (GR_BLEND_ONE,
            GR_BLEND_ZERO,
            GR_BLEND_ZERO,
            GR_BLEND_ZERO);
        grTexCombine (GR_TMU0,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            FXFALSE, FXFALSE);
        grTexCombine (GR_TMU1,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            FXFALSE, FXFALSE);
        break;
    case 2: // red only
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
    grConstantColorValue (0xFF0000FF);
        grAlphaBlendFunction (GR_BLEND_ONE,
            GR_BLEND_ZERO,
            GR_BLEND_ZERO,
            GR_BLEND_ZERO);
        grTexCombine (GR_TMU0,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            FXFALSE, FXFALSE);
        grTexCombine (GR_TMU1,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            GR_COMBINE_FUNCTION_ZERO,
            GR_COMBINE_FACTOR_NONE,
            FXFALSE, FXFALSE);
        break;
    }

    grAlphaTestFunction (GR_CMP_ALWAYS);
    grCullMode (GR_CULL_DISABLE);

    //grDepthBufferFunction (GR_CMP_ALWAYS);
    //grDepthMask (FXFALSE);

    rdp.update |= UPDATE_COMBINE | UPDATE_ALPHA_COMPARE;
}

#ifdef __cplusplus
extern "C" {
#endif

/******************************************************************
Function: FrameBufferRead
Purpose:  This function is called to notify the dll that the
frame buffer memory is beening read at the given address.
DLL should copy content from its render buffer to the frame buffer
in N64 RDRAM
DLL is responsible to maintain its own frame buffer memory addr list
DLL should copy 4KB block content back to RDRAM frame buffer.
Emulator should not call this function again if other memory
is read within the same 4KB range
input:    addr      rdram address
val         val
size        1 = BYTE, 2 = WORD, 4 = DWORD
output:   none
*******************************************************************/
EXPORT void CALL FBRead(unsigned int addr)
{
    LOG ("FBRead ()\n");

  if (cpu_fb_ignore)
    return;
  if (cpu_fb_write_called)
  {
    cpu_fb_ignore = TRUE;
    cpu_fb_write = FALSE;
    return;
  }
  cpu_fb_read_called = TRUE;
    DWORD a = segoffset(addr);
    FRDP("FBRead. addr: %08lx\n", a);
    if (!rdp.fb_drawn && (a >= rdp.cimg) && (a < rdp.ci_end))
    {
    fbreads_back++;
    //if (fbreads_back > 2) //&& (rdp.ci_width <= 320))
    {
      CopyFrameBuffer ();
            rdp.fb_drawn = TRUE;
    }
  }
  if (!rdp.fb_drawn_front && (a >= rdp.maincimg[1].addr) && (a < rdp.maincimg[1].addr + rdp.ci_width*rdp.ci_height*2))
  {
    fbreads_front++;
    //if (fbreads_front > 2)//&& (rdp.ci_width <= 320))
    {
            DWORD cimg = rdp.cimg;
        rdp.cimg = rdp.maincimg[1].addr;
        if (settings.fb_smart)
        {
          rdp.ci_width = rdp.maincimg[1].width;
          rdp.ci_count = 0;
          DWORD h = rdp.frame_buffers[0].height;
          rdp.frame_buffers[0].height = rdp.maincimg[1].height;
          CopyFrameBuffer(GR_BUFFER_FRONTBUFFER);
          rdp.frame_buffers[0].height = h;
        }
        else
        {
          CopyFrameBuffer(GR_BUFFER_FRONTBUFFER);
        }
        rdp.cimg = cimg;
        rdp.fb_drawn_front = TRUE;
        }
    }
}

#if 0
//TODO: remove
/******************************************************************
Function: FrameBufferWriteList
Purpose:  This function is called to notify the dll that the
frame buffer has been modified by CPU at the given address.
input:    FrameBufferModifyEntry *plist
size = size of the plist, max = 1024
output:   none
*******************************************************************/
EXPORT void CALL FBWList(FrameBufferModifyEntry *plist, DWORD size)
{
    LOG ("FBWList ()\n");
    FRDP("FBWList. size: %d\n", size);
    printf("FBWList. size: %d\n", size);
}
#endif

/******************************************************************
Function: FrameBufferWrite
Purpose:  This function is called to notify the dll that the
frame buffer has been modified by CPU at the given address.
input:    addr      rdram address
val         val
size        1 = BYTE, 2 = WORD, 4 = DWORD
output:   none
*******************************************************************/
EXPORT void CALL FBWrite(unsigned int addr, unsigned int size)
{
    LOG ("FBWrite ()\n");
  if (cpu_fb_ignore)
    return;
  if (cpu_fb_read_called)
  {
    cpu_fb_ignore = TRUE;
    cpu_fb_write = FALSE;
    return;
  }
  cpu_fb_write_called = TRUE;
    DWORD a = segoffset(addr);
    FRDP("FBWrite. addr: %08lx\n", a);
    // ZIGGY : added a test on ci_width, otherwise we crash on zero division below
    if (!rdp.ci_width || a < rdp.cimg || a > rdp.ci_end)
      return;
    cpu_fb_write = TRUE;
    DWORD shift_l = (a-rdp.cimg) >> 1;
    DWORD shift_r = shift_l+2;

    d_ul_x = min(d_ul_x, shift_l%rdp.ci_width);
    d_ul_y = min(d_ul_y, shift_l/rdp.ci_width);
    d_lr_x = max(d_lr_x, shift_r%rdp.ci_width);
    d_lr_y = max(d_lr_y, shift_r/rdp.ci_width);
}


/************************************************************************
Function: FBGetFrameBufferInfo
Purpose:  This function is called by the emulator core to retrieve frame
          buffer information from the video plugin in order to be able
          to notify the video plugin about CPU frame buffer read/write
          operations

          size:
            = 1     byte
            = 2     word (16 bit) <-- this is N64 default depth buffer format
            = 4     dword (32 bit)

          when frame buffer information is not available yet, set all values
          in the FrameBufferInfo structure to 0

input:    FrameBufferInfo pinfo[6]
          pinfo is pointed to a FrameBufferInfo structure which to be
          filled in by this function
output:   Values are return in the FrameBufferInfo structure
          Plugin can return up to 6 frame buffer info
************************************************************************/
///*
#if 0
//TODO: remove
typedef struct
{
    DWORD addr;
    DWORD size;
    DWORD width;
    DWORD height;
} FrameBufferInfo;
#endif

EXPORT void CALL FBGetFrameBufferInfo(void *p)
{
  LOG ("FBGetFrameBufferInfo ()\n");
    FrameBufferInfo * pinfo = (FrameBufferInfo *)p;
        memset(pinfo,0,sizeof(FrameBufferInfo)*6);
  if (!settings.fb_get_info)
    return;
  RDP("FBGetFrameBufferInfo ()\n");
  //*
    if (settings.fb_smart)
    {
    pinfo[0].addr   = rdp.maincimg[1].addr;
    pinfo[0].size   = rdp.maincimg[1].size;
    pinfo[0].width  = rdp.maincimg[1].width;
    pinfo[0].height = rdp.maincimg[1].height;
    int info_index = 1;
    for (int i = 0; i < rdp.num_of_ci && info_index < 6; i++)
      {
      COLOR_IMAGE & cur_fb = rdp.frame_buffers[i];
      if (cur_fb.status == ci_main || cur_fb.status == ci_copy_self ||
        cur_fb.status == ci_old_copy)
         {
        pinfo[info_index].addr   = cur_fb.addr;
        pinfo[info_index].size   = cur_fb.size;
        pinfo[info_index].width  = cur_fb.width;
        pinfo[info_index].height = cur_fb.height;
            info_index++;
         }
      }
  }
  else
      {
    pinfo[0].addr   = rdp.maincimg[0].addr;
    pinfo[0].size   = rdp.ci_size;
    pinfo[0].width  = rdp.ci_width;
    pinfo[0].height = rdp.ci_width*3/4;
    pinfo[1].addr   = rdp.maincimg[1].addr;
    pinfo[1].size   = rdp.ci_size;
    pinfo[1].width  = rdp.ci_width;
    pinfo[1].height = rdp.ci_width*3/4;
    }
//*/
}
#ifdef __cplusplus
}
#endif

//*/
#include "UcodeFB.h"

void DetectFrameBufferUsage ()
{
    RDP("DetectFrameBufferUsage\n");

    DWORD dlist_start = *(DWORD*)(gfx.DMEM+0xFF0);
#ifdef _WIN32
    DWORD dlist_length = *(DWORD*)(gfx.DMEM+0xFF4);
#endif // _WIN32
    DWORD a;

  BOOL tidal = FALSE;
  if (settings.PM && (rdp.copy_ci_index || rdp.frame_buffers[rdp.copy_ci_index].status == ci_copy_self))
    tidal = TRUE;
    DWORD ci = rdp.cimg, zi = rdp.zimg; // ci_width = rdp.ci_width;
  rdp.main_ci = rdp.main_ci_end = rdp.main_ci_bg = rdp.ci_count = 0;
  rdp.main_ci_index = rdp.copy_ci_index = 0;
    rdp.zimg_end = 0;
    rdp.tmpzimg = 0;
    rdp.motionblur = FALSE;
  rdp.main_ci_last_tex_addr = 0;
    BOOL previous_ci_was_read = rdp.read_previous_ci;
    rdp.read_previous_ci = FALSE;
  rdp.read_whole_frame = FALSE;
  rdp.swap_ci_index = rdp.black_ci_index = -1;
  SwapOK = TRUE;

    // Start executing at the start of the display list
    rdp.pc_i = 0;
    rdp.pc[rdp.pc_i] = dlist_start;
    rdp.dl_count = -1;
    rdp.halt = 0;
    rdp.scale_x_bak = rdp.scale_x;
    rdp.scale_y_bak = rdp.scale_y;

    // MAIN PROCESSING LOOP
    do {

        // Get the address of the next command
        a = rdp.pc[rdp.pc_i] & BMASK;

        // Load the next command and its input
        rdp.cmd0 = ((DWORD*)gfx.RDRAM)[a>>2];   // \ Current command, 64 bit
        rdp.cmd1 = ((DWORD*)gfx.RDRAM)[(a>>2)+1]; // /

        // Output the address before the command

        // Go to the next instruction
        rdp.pc[rdp.pc_i] = (a+8) & BMASK;

        if ((intptr_t)(gfx_instruction_lite[settings.ucode][rdp.cmd0>>24]))
            gfx_instruction_lite[settings.ucode][rdp.cmd0>>24] ();

        // check DL counter
        if (rdp.dl_count != -1)
        {
            rdp.dl_count --;
            if (rdp.dl_count == 0)
            {
                rdp.dl_count = -1;

                RDP ("End of DL\n");
                rdp.pc_i --;
            }
        }

    } while (!rdp.halt);
  SwapOK = TRUE;
  if (rdp.ci_count > NUMTEXBUF) //overflow
  {
    rdp.cimg = ci;
    rdp.zimg = zi;
    rdp.num_of_ci = rdp.ci_count;
    rdp.scale_x = rdp.scale_x_bak;
    rdp.scale_y = rdp.scale_y_bak;
    return;
  }

  if (rdp.black_ci_index > 0 && rdp.black_ci_index < rdp.copy_ci_index)
    rdp.frame_buffers[rdp.black_ci_index].status = ci_main;

    if (rdp.frame_buffers[rdp.ci_count-1].status == ci_unknown)
    {
        if (rdp.ci_count > 1)
            rdp.frame_buffers[rdp.ci_count-1].status = ci_aux;
        else
            rdp.frame_buffers[rdp.ci_count-1].status = ci_main;
    }

    if ((rdp.frame_buffers[rdp.ci_count-1].status == ci_aux) &&
        (rdp.frame_buffers[rdp.main_ci_index].width < 320) &&
        (rdp.frame_buffers[rdp.ci_count-1].width > rdp.frame_buffers[rdp.main_ci_index].width))
    {
        for (int i = 0; i < rdp.ci_count; i++)
        {
            if (rdp.frame_buffers[i].status == ci_main)
                rdp.frame_buffers[i].status = ci_aux;
            else if (rdp.frame_buffers[i].addr == rdp.frame_buffers[rdp.ci_count-1].addr)
                rdp.frame_buffers[i].status = ci_main;
//          FRDP("rdp.frame_buffers[%d].status = %d\n", i, rdp.frame_buffers[i].status);
        }
        rdp.main_ci_index = rdp.ci_count-1;
    }

    BOOL all_zimg = TRUE;
        int i;
    for (i = 0; i < rdp.ci_count; i++)
    {
        if (rdp.frame_buffers[i].status != ci_zimg)
        {
          all_zimg = FALSE;
          break;
        }
    }
    if (all_zimg)
    {
      for (i = 0; i < rdp.ci_count; i++)
        rdp.frame_buffers[i].status = ci_main;
    }

    RDP("detect fb final results: \n");
    for (i = 0; i < rdp.ci_count; i++)
    {
    FRDP("rdp.frame_buffers[%d].status = %s, addr: %08lx, height: %d\n", i, CIStatus[rdp.frame_buffers[i].status], rdp.frame_buffers[i].addr, rdp.frame_buffers[i].height);
    }

    rdp.cimg = ci;
    rdp.zimg = zi;
    rdp.num_of_ci = rdp.ci_count;
  if (rdp.read_previous_ci && previous_ci_was_read)
    if (!settings.fb_hires || !rdp.copy_ci_index)
      rdp.motionblur = TRUE;
    if (rdp.motionblur || settings.fb_hires || (rdp.frame_buffers[rdp.copy_ci_index].status == ci_aux_copy))
    {
        rdp.scale_x = rdp.scale_x_bak;
        rdp.scale_y = rdp.scale_y_bak;
    }

    if ((rdp.read_previous_ci || previous_ci_was_read) && !rdp.copy_ci_index)
      rdp.read_whole_frame = TRUE;
    if (rdp.read_whole_frame)
    {
      if (settings.fb_hires && !settings.fb_ignore_previous)
      {
        if (rdp.swap_ci_index < 0)
        {
          rdp.texbufs[0].clear_allowed = TRUE;
          OpenTextureBuffer(rdp.frame_buffers[rdp.main_ci_index]);
        }
      }
      else
    {
      if (rdp.motionblur)
      {
        if (settings.fb_motionblur)
            CopyFrameBuffer();
        else
          memset(gfx.RDRAM+rdp.cimg, 0, rdp.ci_width*rdp.ci_height*rdp.ci_size);
      }
        else //if (ci_width == rdp.frame_buffers[rdp.main_ci_index].width)
        {
          if (rdp.maincimg[0].height > 65) //for 1080
          {
            rdp.cimg = rdp.maincimg[0].addr;
            rdp.ci_width = rdp.maincimg[0].width;
            rdp.ci_count = 0;
            DWORD h = rdp.frame_buffers[0].height;
            rdp.frame_buffers[0].height = rdp.maincimg[0].height;
            CopyFrameBuffer();
            rdp.frame_buffers[0].height = h;
          }
          else //conker
          {
            CopyFrameBuffer();
          }
        }
      }
    }

    if (settings.fb_hires)
    {
      for (i = 0; i < num_tmu; i++)
      {
        rdp.texbufs[i].clear_allowed = TRUE;
        for (int j = 0; j < 256; j++)
        {
          rdp.texbufs[i].images[j].drawn = FALSE;
          rdp.texbufs[i].images[j].clear = TRUE;
        }
      }
      if (tidal)
      {
        //RDP("Tidal wave!\n");
        rdp.copy_ci_index = rdp.main_ci_index;
      }
    }
    rdp.ci_count = 0;
    if (settings.fb_ignore_previous)
      rdp.read_whole_frame = FALSE;
    else
      rdp.maincimg[0] = rdp.frame_buffers[rdp.main_ci_index];
    //  rdp.scale_x = rdp.scale_x_bak;
    //  rdp.scale_y = rdp.scale_y_bak;
    RDP("DetectFrameBufferUsage End\n");
}




#ifdef __cplusplus
extern "C" {
#endif

/******************************************************************
Function: ProcessRDPList
Purpose:  This function is called when there is a Dlist to be
processed. (Low level GFX list)
input:    none
output:   none
*******************************************************************/
EXPORT void CALL ProcessRDPList(void)
{
  if (settings.KI)
  {
    *gfx.MI_INTR_REG |= 0x20;
    gfx.CheckInterrupts();
  }
  LOG ("ProcessRDPList ()\n");

  no_dlist = FALSE;
  update_screen_count = 0;
  ChangeSize ();

#ifdef ALTTAB_FIX
    if (!hhkLowLevelKybd)
    {
        hhkLowLevelKybd = SetWindowsHookEx(WH_KEYBOARD_LL,
            LowLevelKeyboardProc, hInstance, 0);
    }
#endif

    LOG ("ProcessDList ()\n");

    if (!fullscreen)
  {
        drawNoFullscreenMessage();
    // Set an interrupt to allow the game to continue
    *gfx.MI_INTR_REG |= 0x20;
    gfx.CheckInterrupts();
  }

    if (reset)
    {
        reset = 0;

        memset (microcode, 0, 4096);
        if (settings.autodetect_ucode)
        {
            // Thanks to ZeZu for ucode autodetection!!!

            DWORD startUcode = *(DWORD*)(gfx.DMEM+0xFD0);
            memcpy (microcode, gfx.RDRAM+startUcode, 4096);
            microcheck ();

        }
    }
  else if ( ((old_ucode == 6) && (settings.ucode == 1)) || settings.force_microcheck)
    {
        DWORD startUcode = *(DWORD*)(gfx.DMEM+0xFD0);
        memcpy (microcode, gfx.RDRAM+startUcode, 4096);
        microcheck ();
    }

    if (exception) return;

    // Switch to fullscreen?
    if (to_fullscreen)
    {
        to_fullscreen = FALSE;

        if (!InitGfx (FALSE))
        {
            LOG ("FAILED!!!\n");
            return;
        }
        fullscreen = TRUE;
    }

    // Clear out the RDP log
#ifdef RDP_LOGGING
    if (settings.logging && settings.log_clear)
    {
        CLOSE_RDP_LOG ();
        OPEN_RDP_LOG ();
    }
#endif

#ifdef UNIMP_LOG
    if (settings.log_unk && settings.unk_clear)
    {
        std::ofstream unimp;
        unimp.open("unimp.txt");
        unimp.close();
    }
#endif

  //* Set states *//
  if (settings.swapmode > 0)
    SwapOK = TRUE;
  rdp.updatescreen = 1;

    rdp.tri_n = 0;  // 0 triangles so far this frame
    rdp.debug_n = 0;

    rdp.model_i = 0; // 0 matrices so far in stack
    //stack_size can be less then 32! Important for Silicon Vally. Thanks Orkin!
    rdp.model_stack_size = min(32, (*(DWORD*)(gfx.DMEM+0x0FE4))>>6);
    if (rdp.model_stack_size == 0)
      rdp.model_stack_size = 32;
  rdp.fb_drawn = rdp.fb_drawn_front = FALSE;
    rdp.update = 0x7FFFFFFF;  // All but clear cache
    rdp.geom_mode = 0;
  rdp.acmp = 0;
    rdp.maincimg[1] = rdp.maincimg[0];
    rdp.skip_drawing = FALSE;
    rdp.s2dex_tex_loaded = FALSE;
  fbreads_front = fbreads_back = 0;
  rdp.fog_multiplier = rdp.fog_offset = 0;
  rdp.zsrc = 0;

    if (cpu_fb_write == TRUE)
      DrawFrameBufferToScreen();
    cpu_fb_write = FALSE;
  cpu_fb_read_called = FALSE;
  cpu_fb_write_called = FALSE;
  cpu_fb_ignore = FALSE;
    d_ul_x = 0xffff;
    d_ul_y = 0xffff;
    d_lr_x = 0;
    d_lr_y = 0;

    //analize possible frame buffer usage
    if (settings.fb_smart)
        DetectFrameBufferUsage();
  if (!settings.lego || rdp.num_of_ci > 1)
    rdp.last_bg = 0;
  //* End of set states *//


    // Get the start of the display list and the length of it
//  DWORD dlist_start = *(DWORD*)(gfx.DMEM+0xFF0);
//  DWORD dlist_length = *(DWORD*)(gfx.DMEM+0xFF4);
  DWORD dlist_start = *gfx.DPC_CURRENT_REG;
    DWORD dlist_length = *gfx.DPC_END_REG - *gfx.DPC_CURRENT_REG;
  FRDP("--- NEW DLIST --- crc: %08lx, ucode: %d, fbuf: %08lx, fbuf_width: %d, dlist start: %08lx, dlist_lenght: %d\n", uc_crc, settings.ucode, *gfx.VI_ORIGIN_REG, *gfx.VI_WIDTH_REG, dlist_start, dlist_length);
    FRDP_E("--- NEW DLIST --- crc: %08lx, ucode: %d, fbuf: %08lx\n", uc_crc, settings.ucode, *gfx.VI_ORIGIN_REG);

    if (settings.tonic && dlist_length < 16)
    {
    rdp_fullsync();
        FRDP_E("DLIST is too short!\n");
        return;
    }

    // Start executing at the start of the display list
    rdp.pc_i = 0;
    rdp.pc[rdp.pc_i] = dlist_start;
    rdp.dl_count = -1;
    rdp.halt = 0;
  DWORD a;

    // catches exceptions so that it doesn't freeze
#ifdef CATCH_EXCEPTIONS
    try {
#endif

        // MAIN PROCESSING LOOP
        do {

            // Get the address of the next command
            a = rdp.pc[rdp.pc_i] & BMASK;

            // Load the next command and its input
            rdp.cmd0 = ((DWORD*)gfx.RDRAM)[a>>2];   // \ Current command, 64 bit
            rdp.cmd1 = ((DWORD*)gfx.RDRAM)[(a>>2)+1]; // /
            // cmd2 and cmd3 are filled only when needed, by the function that needs them

            // Output the address before the command
#ifdef LOG_COMMANDS
            FRDP ("%08lx (c0:%08lx, c1:%08lx): ", a, rdp.cmd0, rdp.cmd1);
#else
            FRDP ("%08lx: ", a);
#endif

            // Go to the next instruction
            rdp.pc[rdp.pc_i] = (a+8) & BMASK;

#ifdef PERFORMANCE
            QueryPerformanceCounter ((LARGE_INTEGER*)&perf_cur);
#endif
            // Process this instruction
            gfx_instruction[settings.ucode][((rdp.cmd0>>24)&0x3f) + 0x100-0x40] ();

            // check DL counter
            if (rdp.dl_count != -1)
            {
                rdp.dl_count --;
                if (rdp.dl_count == 0)
                {
                    rdp.dl_count = -1;

                    RDP ("End of DL\n");
                    rdp.pc_i --;
                }
            }

#ifdef PERFORMANCE
            QueryPerformanceCounter ((LARGE_INTEGER*)&perf_next);
            __int64 t = perf_next-perf_cur;
            sprintf (out_buf, "perf %08lx: %016I64d\n", a-8, t);
            rdp_log << out_buf;
#endif

        } while (0);
#ifdef CATCH_EXCEPTIONS
    } catch (...) {

        if (fullscreen) ReleaseGfx ();
        WriteLog(M64MSG_ERROR, "The GFX plugin caused an exception and has been disabled.");
        exception = TRUE;
    }
#endif

    if (settings.fb_smart)
    {
        rdp.scale_x = rdp.scale_x_bak;
        rdp.scale_y = rdp.scale_y_bak;
    }
    if (settings.fb_read_always)
    {
    CopyFrameBuffer ();
    }
    if (rdp.yuv_image)
    {
      DrawYUVImageToFrameBuffer();
      rdp.yuv_image = FALSE;
//        FRDP("yuv image draw. ul_x: %f, ul_y: %f, lr_x: %f, lr_y: %f, begin: %08lx\n",
//        rdp.yuv_ul_x, rdp.yuv_ul_y, rdp.yuv_lr_x, rdp.yuv_lr_y, rdp.yuv_im_begin);
      rdp.yuv_ul_x = rdp.yuv_ul_y = rdp.yuv_lr_x = rdp.yuv_lr_y = 0;
      rdp.yuv_im_begin = 0x00FFFFFF;
    }
    if (rdp.cur_image)
    CloseTextureBuffer(rdp.read_whole_frame && (settings.PM || rdp.swap_ci_index >= 0));

  if (settings.TGR2 && rdp.vi_org_reg != *gfx.VI_ORIGIN_REG && CI_SET)
  {
    newSwapBuffers ();
    CI_SET = FALSE;
  }
    RDP("ProcessDList end\n");






  WriteLog(M64MSG_VERBOSE, "ProcessRPDList %x %x %x\n",
         *gfx.DPC_START_REG,
         *gfx.DPC_END_REG,
         *gfx.DPC_CURRENT_REG);
  //*gfx.DPC_STATUS_REG = 0xffffffff; // &= ~0x0002;

  *gfx.DPC_START_REG = *gfx.DPC_END_REG;
  *gfx.DPC_CURRENT_REG = *gfx.DPC_END_REG;
}

#ifdef __cplusplus
}
#endif





//  Local Variables: ***
//  tab-width:4 ***
//  c-file-offset:4 ***
//  End: ***

