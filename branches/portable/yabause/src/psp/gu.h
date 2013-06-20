/*  src/psp/gu.h: sceGu substitute library header
    Copyright 2009 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#ifndef PSP_GU_H
#define PSP_GU_H

/*
 * This library defines a substitute set of functions for the sceGu library
 * used for manipulating display lists.  These substitute functions are
 * designed for maximum efficiency when dynamically generating display
 * lists, and improve on the sceGu library in the following ways:
 *
 * - The sceGu library functions (at least as implemented in PSPSDK r2450)
 *   are designed to be robust against data corruption by using uncached
 *   memory accesses, but such accesses naturally slow the program down
 *   significantly.  The functions defined here avoid this slowdown by
 *   making judicious use of the CPU's cache, allowing the CPU to access
 *   memory in cache line units instead of individual words.
 *
 * - The sceGuDrawArray() function triggers the GE to resume processing
 *   after writing instructions to the display list.  In theory, this can
 *   improve performance by maximizing parallelism between the GE and the
 *   main Allegrex CPU; however, when constructing dynamic display lists
 *   which may by necessity include frequent DrawArray() calls, the
 *   overhead of synchronization far outweighs any performance gain.  This
 *   library defines a guCommit() function allowing the caller to specify
 *   exactly when to trigger the GE, and does not do so on its own (except
 *   when calling guFinish() on a GU_DIRECT display list).
 *
 * - The sceGuDrawArray also requires the vertex type, vertex pointer,
 *   vertex count, and primitive type to all be specified in a single
 *   call.  This can waste a significant amount of space when generating
 *   dynamic vertex arrays in which the vertex type is constant but the
 *   number of vertices cannot be easily computed or stored.  This library
 *   defines guVertexFormat(), guVertexPointer(), and guDrawPrimitive()
 *   functions which implement each of the separate functions of
 *   guDrawArray(), so only the necessary functions need to be called.
 *
 * - Many of the texture-related functions internally call sceGuTexFlush()
 *   to flush the texture cache.  While robust, this can cause significant
 *   bloat of the resulting display list.  This library never calls
 *   guTexFlush() automatically; it is thus up to the caller to flush the
 *   texture cache at appropriate points.
 *
 * - Most of the substitute functions are defined as inline functions in
 *   this file, allowing the compiler to avoid the overhead of calling a
 *   subroutine for each graphics operation and to fold constant parameters
 *   when computing GE instruction words.  GCC, at least, can also optimize
 *   out the updates of the list pointer variable after each instruction,
 *   saving a significant amount of code.  In an optimal case where all
 *   function parameters are constant, such as
 *      sceGuTexFilter(GU_NEAREST, GU_NEAREST);
 *   execution time can be reduced from >20 cycles to 3 (or even 2) cycles
 *   per call.
 *
 * Note that this library is only implemented to the extent necessary for
 * Yabause, and needs more work to be usable as a general-purpose library.
 */

/*************************************************************************/
/*************************************************************************/

/**
 * USE_SCEGU:  When defined, the guXxx() functions declared in this file
 * will instead be aliased to the standard sceGuXxx() library functions,
 * and custom functions not defined in the sceGu library such as guCommit()
 * are turned into no-ops.  The sceGu functions are slower than our custom
 * functions, so there is normally no reason to define this symbol except
 * for benchmarking or testing.
 */
// #define USE_SCEGU

/*************************************************************************/
/************************ GE command definitions *************************/
/*************************************************************************/

typedef enum GECommand_ {
    GECMD_NOP           = 0x00,
    GECMD_VERTEX_POINTER= 0x01,
    GECMD_INDEX_POINTER = 0x02,
                       // 0x03 undefined
    GECMD_DRAW_PRIMITIVE= 0x04,
    GECMD_DRAW_BEZIER   = 0x05,
    GECMD_DRAW_SPLINE   = 0x06,
    GECMD_TEST_BBOX     = 0x07,
    GECMD_JUMP          = 0x08,
    GECMD_COND_JUMP     = 0x09,
    GECMD_CALL          = 0x0A,
    GECMD_RETURN        = 0x0B,
    GECMD_END           = 0x0C,
                       // 0x0D undefined
    GECMD_SIGNAL        = 0x0E,
    GECMD_FINISH        = 0x0F,

    GECMD_ADDRESS_BASE  = 0x10,
                       // 0x11 undefined
    GECMD_VERTEX_FORMAT = 0x12,
    GECMD_UNKNOWN_13    = 0x13, // psp_doc: Offset Address (BASE)
    GECMD_UNKNOWN_14    = 0x14, // psp_doc: Origin Address (BASE)
    GECMD_DRAWAREA_LOW  = 0x15,
    GECMD_DRAWAREA_HIGH = 0x16,
    GECMD_ENA_LIGHTING  = 0x17,
    GECMD_ENA_LIGHT0    = 0x18,
    GECMD_ENA_LIGHT1    = 0x19,
    GECMD_ENA_LIGHT2    = 0x1A,
    GECMD_ENA_LIGHT3    = 0x1B,
    GECMD_ENA_ZCLIP     = 0x1C,
    GECMD_ENA_FACE_CULL = 0x1D,
    GECMD_ENA_TEXTURE   = 0x1E,
    GECMD_ENA_FOG       = 0x1F,

    GECMD_ENA_DITHER    = 0x20,
    GECMD_ENA_BLEND     = 0x21,
    GECMD_ENA_ALPHA_TEST= 0x22,
    GECMD_ENA_DEPTH_TEST= 0x23,
    GECMD_ENA_STENCIL   = 0x24,
    GECMD_ENA_ANTIALIAS = 0x25,
    GECMD_ENA_PATCH_CULL= 0x26,
    GECMD_ENA_COLOR_TEST= 0x27,
    GECMD_ENA_LOGIC_OP  = 0x28,
                       // 0x29 undefined
    GECMD_BONE_OFFSET   = 0x2A,
    GECMD_BONE_UPLOAD   = 0x2B,
    GECMD_MORPH_0       = 0x2C,
    GECMD_MORPH_1       = 0x2D,
    GECMD_MORPH_2       = 0x2E,
    GECMD_MORPH_3       = 0x2F,

    GECMD_MORPH_4       = 0x30,
    GECMD_MORPH_5       = 0x31,
    GECMD_MORPH_6       = 0x32,
    GECMD_MORPH_7       = 0x33,
                       // 0x34 undefined
                       // 0x35 undefined
    GECMD_PATCH_SUBDIV  = 0x36,
    GECMD_PATCH_PRIM    = 0x37,
    GECMD_PATCH_FRONT   = 0x38,
                       // 0x39 undefined
    GECMD_MODEL_START   = 0x3A,
    GECMD_MODEL_UPLOAD  = 0x3B,
    GECMD_VIEW_START    = 0x3C,
    GECMD_VIEW_UPLOAD   = 0x3D,
    GECMD_PROJ_START    = 0x3E,
    GECMD_PROJ_UPLOAD   = 0x3F,

    GECMD_TEXTURE_START = 0x40,
    GECMD_TEXTURE_UPLOAD= 0x41,
    GECMD_XSCALE        = 0x42,
    GECMD_YSCALE        = 0x43,
    GECMD_ZSCALE        = 0x44,
    GECMD_XPOS          = 0x45,
    GECMD_YPOS          = 0x46,
    GECMD_ZPOS          = 0x47,
    GECMD_USCALE        = 0x48,
    GECMD_VSCALE        = 0x49,
    GECMD_UOFFSET       = 0x4A,
    GECMD_VOFFSET       = 0x4B,
    GECMD_XOFFSET       = 0x4C,
    GECMD_YOFFSET       = 0x4D,
                       // 0x4E undefined
                       // 0x4F undefined

    GECMD_SHADE_MODE    = 0x50,
    GECMD_REV_NORMALS   = 0x51,
                       // 0x52 undefined
    GECMD_COLOR_MATERIAL= 0x53,
    GECMD_EMISSIVE_COLOR= 0x54,
    GECMD_AMBIENT_COLOR = 0x55,
    GECMD_DIFFUSE_COLOR = 0x56,
    GECMD_SPECULAR_COLOR= 0x57,
    GECMD_AMBIENT_ALPHA = 0x58,
                       // 0x59 undefined
                       // 0x5A undefined
    GECMD_SPECULAR_POWER= 0x5B,
    GECMD_LIGHT_AMBCOLOR= 0x5C,
    GECMD_LIGHT_AMBALPHA= 0x5D,
    GECMD_LIGHT_MODEL   = 0x5E,
    GECMD_LIGHT0_TYPE   = 0x5F,

    GECMD_LIGHT1_TYPE   = 0x60,
    GECMD_LIGHT2_TYPE   = 0x61,
    GECMD_LIGHT3_TYPE   = 0x62,
    GECMD_LIGHT0_XPOS   = 0x63,
    GECMD_LIGHT0_YPOS   = 0x64,
    GECMD_LIGHT0_ZPOS   = 0x65,
    GECMD_LIGHT1_XPOS   = 0x66,
    GECMD_LIGHT1_YPOS   = 0x67,
    GECMD_LIGHT1_ZPOS   = 0x68,
    GECMD_LIGHT2_XPOS   = 0x69,
    GECMD_LIGHT2_YPOS   = 0x6A,
    GECMD_LIGHT2_ZPOS   = 0x6B,
    GECMD_LIGHT3_XPOS   = 0x6C,
    GECMD_LIGHT3_YPOS   = 0x6D,
    GECMD_LIGHT3_ZPOS   = 0x6E,
    GECMD_LIGHT0_XDIR   = 0x6F,

    GECMD_LIGHT0_YDIR   = 0x70,
    GECMD_LIGHT0_ZDIR   = 0x71,
    GECMD_LIGHT1_XDIR   = 0x72,
    GECMD_LIGHT1_YDIR   = 0x73,
    GECMD_LIGHT1_ZDIR   = 0x74,
    GECMD_LIGHT2_XDIR   = 0x75,
    GECMD_LIGHT2_YDIR   = 0x76,
    GECMD_LIGHT2_ZDIR   = 0x77,
    GECMD_LIGHT3_XDIR   = 0x78,
    GECMD_LIGHT3_YDIR   = 0x79,
    GECMD_LIGHT3_ZDIR   = 0x7A,
    GECMD_LIGHT0_CATT   = 0x7B,
    GECMD_LIGHT0_LATT   = 0x7C,
    GECMD_LIGHT0_QATT   = 0x7D,
    GECMD_LIGHT1_CATT   = 0x7E,
    GECMD_LIGHT1_LATT   = 0x7F,

    GECMD_LIGHT1_QATT   = 0x80,
    GECMD_LIGHT2_CATT   = 0x81,
    GECMD_LIGHT2_LATT   = 0x82,
    GECMD_LIGHT2_QATT   = 0x83,
    GECMD_LIGHT3_CATT   = 0x84,
    GECMD_LIGHT3_LATT   = 0x85,
    GECMD_LIGHT3_QATT   = 0x86,
    GECMD_LIGHT0_SPOTEXP= 0x87,
    GECMD_LIGHT1_SPOTEXP= 0x88,
    GECMD_LIGHT2_SPOTEXP= 0x89,
    GECMD_LIGHT3_SPOTEXP= 0x8A,
    GECMD_LIGHT0_SPOTLIM= 0x8B,
    GECMD_LIGHT1_SPOTLIM= 0x8C,
    GECMD_LIGHT2_SPOTLIM= 0x8D,
    GECMD_LIGHT3_SPOTLIM= 0x8E,
    GECMD_LIGHT0_ACOL   = 0x8F,

    GECMD_LIGHT0_DCOL   = 0x90,
    GECMD_LIGHT0_SCOL   = 0x91,
    GECMD_LIGHT1_ACOL   = 0x92,
    GECMD_LIGHT1_DCOL   = 0x93,
    GECMD_LIGHT1_SCOL   = 0x94,
    GECMD_LIGHT2_ACOL   = 0x95,
    GECMD_LIGHT2_DCOL   = 0x96,
    GECMD_LIGHT2_SCOL   = 0x97,
    GECMD_LIGHT3_ACOL   = 0x98,
    GECMD_LIGHT3_DCOL   = 0x99,
    GECMD_LIGHT3_SCOL   = 0x9A,
    GECMD_FACE_ORDER    = 0x9B,
    GECMD_DRAW_ADDRESS  = 0x9C,
    GECMD_DRAW_STRIDE   = 0x9D,
    GECMD_DEPTH_ADDRESS = 0x9E,
    GECMD_DEPTH_STRIDE  = 0x9F,

    GECMD_TEX0_ADDRESS  = 0xA0,
    GECMD_TEX1_ADDRESS  = 0xA1,
    GECMD_TEX2_ADDRESS  = 0xA2,
    GECMD_TEX3_ADDRESS  = 0xA3,
    GECMD_TEX4_ADDRESS  = 0xA4,
    GECMD_TEX5_ADDRESS  = 0xA5,
    GECMD_TEX6_ADDRESS  = 0xA6,
    GECMD_TEX7_ADDRESS  = 0xA7,
    GECMD_TEX0_STRIDE   = 0xA8,
    GECMD_TEX1_STRIDE   = 0xA9,
    GECMD_TEX2_STRIDE   = 0xAA,
    GECMD_TEX3_STRIDE   = 0xAB,
    GECMD_TEX4_STRIDE   = 0xAC,
    GECMD_TEX5_STRIDE   = 0xAD,
    GECMD_TEX6_STRIDE   = 0xAE,
    GECMD_TEX7_STRIDE   = 0xAF,

    GECMD_CLUT_ADDRESS_L= 0xB0,
    GECMD_CLUT_ADDRESS_H= 0xB1,
    GECMD_COPY_S_ADDRESS= 0xB2,
    GECMD_COPY_S_STRIDE = 0xB3,
    GECMD_COPY_D_ADDRESS= 0xB4,
    GECMD_COPY_D_STRIDE = 0xB5,
                       // 0xB6 undefined
                       // 0xB7 undefined
    GECMD_TEX0_SIZE     = 0xB8,
    GECMD_TEX1_SIZE     = 0xB9,
    GECMD_TEX2_SIZE     = 0xBA,
    GECMD_TEX3_SIZE     = 0xBB,
    GECMD_TEX4_SIZE     = 0xBC,
    GECMD_TEX5_SIZE     = 0xBD,
    GECMD_TEX6_SIZE     = 0xBE,
    GECMD_TEX7_SIZE     = 0xBF,

    GECMD_TEXTURE_MAP   = 0xC0,
    GECMD_TEXTURE_ENVMAP= 0xC1,
    GECMD_TEXTURE_MODE  = 0xC2,
    GECMD_TEXTURE_PIXFMT= 0xC3,
    GECMD_CLUT_LOAD     = 0xC4,
    GECMD_CLUT_MODE     = 0xC5,
    GECMD_TEXTURE_FILTER= 0xC6,
    GECMD_TEXTURE_WRAP  = 0xC7,
    GECMD_TEXTURE_BIAS  = 0xC8,
    GECMD_TEXTURE_FUNC  = 0xC9,
    GECMD_TEXTURE_COLOR = 0xCA,
    GECMD_TEXTURE_FLUSH = 0xCB,
    GECMD_COPY_SYNC     = 0xCC,
    GECMD_FOG_LIMIT     = 0xCD,
    GECMD_FOG_RANGE     = 0xCE,
    GECMD_FOG_COLOR     = 0xCF,

    GECMD_TEXTURE_SLOPE = 0xD0,
                       // 0xD1 undefined
    GECMD_FRAME_PIXFMT  = 0xD2,
    GECMD_CLEAR_MODE    = 0xD3,
    GECMD_CLIP_MIN      = 0xD4,
    GECMD_CLIP_MAX      = 0xD5,
    GECMD_CLIP_NEAR     = 0xD6,
    GECMD_CLIP_FAR      = 0xD7,
    GECMD_COLORTEST_FUNC= 0xD8,
    GECMD_COLORTEST_REF = 0xD9,
    GECMD_COLORTEST_MASK= 0xDA,
    GECMD_ALPHATEST     = 0xDB,
    GECMD_STENCILTEST   = 0xDC,
    GECMD_STENCIL_OP    = 0xDD,
    GECMD_DEPTHTEST     = 0xDE,
    GECMD_BLEND_FUNC    = 0xDF,

    GECMD_BLEND_SRCFIX  = 0xE0,
    GECMD_BLEND_DSTFIX  = 0xE1,
    GECMD_DITHER0       = 0xE2,
    GECMD_DITHER1       = 0xE3,
    GECMD_DITHER2       = 0xE4,
    GECMD_DITHER3       = 0xE5,
    GECMD_LOGIC_OP      = 0xE6,
    GECMD_DEPTH_MASK    = 0xE7,
    GECMD_COLOR_MASK    = 0xE8,
    GECMD_ALPHA_MASK    = 0xE9,
    GECMD_COPY          = 0xEA,
    GECMD_COPY_S_POS    = 0xEB,
    GECMD_COPY_D_POS    = 0xEC,
                       // 0xED undefined
    GECMD_COPY_SIZE     = 0xEE,
                       // 0xEF undefined

    GECMD_UNKNOWN_F0    = 0xF0,
    GECMD_UNKNOWN_F1    = 0xF1,
    GECMD_UNKNOWN_F2    = 0xF2,
    GECMD_UNKNOWN_F3    = 0xF3,
    GECMD_UNKNOWN_F4    = 0xF4,
    GECMD_UNKNOWN_F5    = 0xF5,
    GECMD_UNKNOWN_F6    = 0xF6,
    GECMD_UNKNOWN_F7    = 0xF7,
    GECMD_UNKNOWN_F8    = 0xF8,
    GECMD_UNKNOWN_F9    = 0xF9,
                       // 0xFA..0xFF undefined
} GECommand;

/*************************************************************************/
/*********************** Custom library functions ************************/
/*************************************************************************/

#ifndef USE_SCEGU

/*************************************************************************/

/* Force these functions to be inlined to avoid function call overhead */
#ifdef __GNUC__
# define inline  inline __attribute__((always_inline))
#endif

/* Note: we declare "extern uint32_t *gu_list" within each function rather
 * than here so that we don't export the declaration to the caller. */

/* Macros for splitting an address into high and low parts */
#define ADDRESS_HI(ptr)  (((uint32_t)(ptr) & 0x3F000000) >> 8)
#define ADDRESS_LO(ptr)   ((uint32_t)(ptr) & 0x00FFFFFF)

/* Helper function to extract the high 24 bits of a float */
static inline uint32_t trim_float(const float value) {
    uint32_t result;
    asm(".set push; .set noreorder\n"
        "mfc1 %[result], %[value]\n"
        "nop\n"
        "srl %[result], %[result], 8\n"
        ".set pop"
        : [result] "=r" (result)
        : [value] "f" (value)
    );
    return result;
}

/*************************************************************************/

/**** Functions defined in gu.c ****/

/* Custom function to commit cached data and start GE processing */
extern void guCommit(void);

extern void guFinish(void);
extern void *guGetMemory(const uint32_t size);
extern void guInit(void);
extern void guStart(const int type, void * const list);
extern void guSync(const int mode, const int target);

/*************************************************************************/

/**** Inline functions ****/

static inline void guAlphaFunc(const int func, const int value, const int mask)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_ALPHATEST<<24 | mask<<16 | value<<8 | func;
}

static inline void guAmbientColor(const uint32_t color)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_AMBIENT_COLOR<<24 | (color & 0xFFFFFF);
    *gu_list++ = GECMD_AMBIENT_ALPHA<<24 | color>>24;
}

static inline void guBlendFunc(const int func, const int src, const int dest,
                               const uint32_t srcfix, const uint32_t destfix)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_BLEND_FUNC<<24 | func<<8 | dest<<4 | src;
    if (src == GU_FIX) {
        *gu_list++ = GECMD_BLEND_SRCFIX<<24 | srcfix;
    }
    if (dest == GU_FIX) {
        *gu_list++ = GECMD_BLEND_DSTFIX<<24 | destfix;
    }
}

static inline void guCallList(const void * const list)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_ADDRESS_BASE<<24 | ADDRESS_HI(list);
    *gu_list++ = GECMD_CALL<<24         | ADDRESS_LO(list);
}

static inline void guClear(const int mode)
{
    extern uint32_t *gu_list;
    extern uint32_t gu_clear_color_stencil;
    extern uint16_t gu_clear_depth;
    struct {uint32_t color, xy, z;} *vertices;
    vertices = guGetMemory(sizeof(*vertices) * 2);
    vertices[0].color = 0;
    vertices[0].xy = 0;
    vertices[0].z = gu_clear_depth;
    vertices[1].color = gu_clear_color_stencil;
    vertices[1].xy = 480 | 272<<16;
    vertices[1].z = 0;
    *gu_list++ = GECMD_CLEAR_MODE<<24     | mode<<8 | 1;
    *gu_list++ = GECMD_ENA_BLEND<<24      | 0;
    *gu_list++ = GECMD_VERTEX_FORMAT<<24
               | GU_TRANSFORM_2D | GU_COLOR_8888 | GU_VERTEX_16BIT;
    *gu_list++ = GECMD_ADDRESS_BASE<<24   | ADDRESS_HI(vertices);
    *gu_list++ = GECMD_VERTEX_POINTER<<24 | ADDRESS_LO(vertices);
    *gu_list++ = GECMD_DRAW_PRIMITIVE<<24 | GU_SPRITES<<16 | 2;
    *gu_list++ = GECMD_CLEAR_MODE<<24     | 0;
}

static inline void guClearColor(const uint32_t color)
{
    extern uint32_t gu_clear_color_stencil;
    gu_clear_color_stencil = (gu_clear_color_stencil & 0xFF000000)
                           | (color & 0x00FFFFFF);
}

static inline void guClearDepth(const unsigned int depth)
{
    extern uint16_t gu_clear_depth;
    gu_clear_depth = depth;
}

static inline void guClearStencil(const unsigned int stencil)
{
    extern uint32_t gu_clear_color_stencil;
    gu_clear_color_stencil = (gu_clear_color_stencil & 0xFFFFFF) | stencil<<24;
}

static inline void guClutLoad(const int count, const void * const address)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_CLUT_ADDRESS_L<<24 | ADDRESS_LO(address);
    *gu_list++ = GECMD_CLUT_ADDRESS_H<<24 | ADDRESS_HI(address);
    *gu_list++ = GECMD_CLUT_LOAD<<24 | count;
}

static inline void guClutMode(const int format, const int shift,
                              const int mask, const int unknown)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_CLUT_MODE<<24 | format | shift<<2 | mask<<8;
}

static inline void guCopyImage(const int mode,
                               const int src_x, const int src_y,
                               const int src_w, const int src_h,
                               const int src_stride,
                               const void * const src_ptr,
                               const int dest_x, const int dest_y,
                               const int dest_stride,
                               const void * const dest_ptr)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_COPY_S_ADDRESS<<24 | ADDRESS_LO(src_ptr);
    *gu_list++ = GECMD_COPY_S_STRIDE<<24  | ADDRESS_HI(src_ptr) | src_stride;
    *gu_list++ = GECMD_COPY_S_POS<<24     | src_y<<10 | src_x;
    *gu_list++ = GECMD_COPY_D_ADDRESS<<24 | ADDRESS_LO(dest_ptr);
    *gu_list++ = GECMD_COPY_D_STRIDE<<24  | ADDRESS_HI(dest_ptr) | dest_stride;
    *gu_list++ = GECMD_COPY_D_POS<<24     | dest_y<<10 | dest_x;
    *gu_list++ = GECMD_COPY_SIZE<<24      | (src_h-1)<<10 | (src_w-1);
    *gu_list++ = GECMD_COPY<<24           | (mode==GU_PSM_8888 ? 1 : 0);
}

static inline void guDepthBuffer(const void * const address, const int stride)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DEPTH_ADDRESS<<24 | ADDRESS_LO(address);
    *gu_list++ = GECMD_DEPTH_STRIDE<<24  | ADDRESS_HI(address) | stride;
}

static inline void guDepthFunc(const int function)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DEPTHTEST<<24 | function;
}

static inline void guDepthMask(const int mask)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DEPTH_MASK<<24 | mask;
}

static inline void guDepthRange(const int near, const int far)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_ZSCALE<<24    | trim_float((far - near) / 2.0f);
    *gu_list++ = GECMD_ZPOS<<24      | trim_float((far + near) / 2.0f);
    *gu_list++ = GECMD_CLIP_NEAR<<24 | (near<far ? near : far);
    *gu_list++ = GECMD_CLIP_FAR<<24  | (near<far ? far : near);
}

static inline void guDisable(const int mode)
{
    extern uint32_t *gu_list;
    if        (mode == GU_LIGHTING) {
        *gu_list++ = GECMD_ENA_LIGHTING<<24 | 0;
    } else if (mode == GU_LIGHT0) {
        *gu_list++ = GECMD_ENA_LIGHT0<<24 | 0;
    } else if (mode == GU_LIGHT1) {
        *gu_list++ = GECMD_ENA_LIGHT1<<24 | 0;
    } else if (mode == GU_LIGHT2) {
        *gu_list++ = GECMD_ENA_LIGHT2<<24 | 0;
    } else if (mode == GU_LIGHT3) {
        *gu_list++ = GECMD_ENA_LIGHT3<<24 | 0;
    } else if (mode == GU_CLIP_PLANES) {
        *gu_list++ = GECMD_ENA_ZCLIP<<24 | 0;
    } else if (mode == GU_CULL_FACE) {
        *gu_list++ = GECMD_ENA_FACE_CULL<<24 | 0;
    } else if (mode == GU_TEXTURE_2D) {
        *gu_list++ = GECMD_ENA_TEXTURE<<24 | 0;
    } else if (mode == GU_FOG) {
        *gu_list++ = GECMD_ENA_FOG<<24 | 0;
    } else if (mode == GU_DITHER) {
        *gu_list++ = GECMD_ENA_DITHER<<24 | 0;
    } else if (mode == GU_BLEND) {
        *gu_list++ = GECMD_ENA_BLEND<<24 | 0;
    } else if (mode == GU_ALPHA_TEST) {
        *gu_list++ = GECMD_ENA_ALPHA_TEST<<24 | 0;
    } else if (mode == GU_DEPTH_TEST) {
        *gu_list++ = GECMD_ENA_DEPTH_TEST<<24 | 0;
    } else if (mode == GU_STENCIL_TEST) {
        *gu_list++ = GECMD_ENA_STENCIL<<24 | 0;
    } else if (mode == GU_PATCH_CULL_FACE) {
        *gu_list++ = GECMD_ENA_PATCH_CULL<<24 | 0;
    } else if (mode == GU_COLOR_TEST) {
        *gu_list++ = GECMD_ENA_COLOR_TEST<<24 | 0;
    } else if (mode == GU_COLOR_LOGIC_OP) {
        *gu_list++ = GECMD_ENA_LOGIC_OP<<24 | 0;
    } else if (mode == GU_FACE_NORMAL_REVERSE) {
        *gu_list++ = GECMD_REV_NORMALS<<24 | 0;
    } else if (mode == GU_SCISSOR_TEST) {
        extern uint8_t gu_scissor_enabled;
        gu_scissor_enabled = 0;
        *gu_list++ = GECMD_CLIP_MIN<<24 | 0<<10 | 0;
        *gu_list++ = GECMD_CLIP_MAX<<24 | (272-1)<<10 | (480-1);
    }
}

static inline void guDispBuffer(const int width, const int height,
                                const void * const address, const int stride)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DRAWAREA_LOW<<24  | 0<<10 | 0;
    *gu_list++ = GECMD_DRAWAREA_HIGH<<24 | (height-1)<<10 | (width-1);
}

static inline void guDrawArray(const int primitive, const uint32_t vertexfmt,
                               const int count, const void * const indexptr,
                               const void * const vertexptr)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_VERTEX_FORMAT<<24  | vertexfmt;
    *gu_list++ = GECMD_ADDRESS_BASE<<24   | ADDRESS_HI(vertexptr);
    *gu_list++ = GECMD_VERTEX_POINTER<<24 | ADDRESS_LO(vertexptr);
    if (indexptr) {
        *gu_list++ = GECMD_ADDRESS_BASE<<24  | ADDRESS_HI(indexptr);
        *gu_list++ = GECMD_INDEX_POINTER<<24 | ADDRESS_LO(indexptr);
    }
    *gu_list++ = GECMD_DRAW_PRIMITIVE<<24 | primitive<<16 | count;
}

static inline void guDrawBuffer(const int format, const void * const address,
                                const int stride)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DRAW_ADDRESS<<24 | ADDRESS_LO(address);
    *gu_list++ = GECMD_DRAW_STRIDE<<24  | ADDRESS_HI(address) | stride;
    *gu_list++ = GECMD_FRAME_PIXFMT<<24 | format;
}

/* Custom function to draw primitives following guVertex{Format,Pointer}() */
static inline void guDrawPrimitive(const int primitive, const int count)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_DRAW_PRIMITIVE<<24 | primitive<<16 | count;
}

static inline void guEnable(const int mode)
{
    extern uint32_t *gu_list;
    if        (mode == GU_LIGHTING) {
        *gu_list++ = GECMD_ENA_LIGHTING<<24 | 1;
    } else if (mode == GU_LIGHT0) {
        *gu_list++ = GECMD_ENA_LIGHT0<<24 | 1;
    } else if (mode == GU_LIGHT1) {
        *gu_list++ = GECMD_ENA_LIGHT1<<24 | 1;
    } else if (mode == GU_LIGHT2) {
        *gu_list++ = GECMD_ENA_LIGHT2<<24 | 1;
    } else if (mode == GU_LIGHT3) {
        *gu_list++ = GECMD_ENA_LIGHT3<<24 | 1;
    } else if (mode == GU_CLIP_PLANES) {
        *gu_list++ = GECMD_ENA_ZCLIP<<24 | 1;
    } else if (mode == GU_CULL_FACE) {
        *gu_list++ = GECMD_ENA_FACE_CULL<<24 | 1;
    } else if (mode == GU_TEXTURE_2D) {
        *gu_list++ = GECMD_ENA_TEXTURE<<24 | 1;
    } else if (mode == GU_FOG) {
        *gu_list++ = GECMD_ENA_FOG<<24 | 1;
    } else if (mode == GU_DITHER) {
        *gu_list++ = GECMD_ENA_DITHER<<24 | 1;
    } else if (mode == GU_BLEND) {
        *gu_list++ = GECMD_ENA_BLEND<<24 | 1;
    } else if (mode == GU_ALPHA_TEST) {
        *gu_list++ = GECMD_ENA_ALPHA_TEST<<24 | 1;
    } else if (mode == GU_DEPTH_TEST) {
        *gu_list++ = GECMD_ENA_DEPTH_TEST<<24 | 1;
    } else if (mode == GU_STENCIL_TEST) {
        *gu_list++ = GECMD_ENA_STENCIL<<24 | 1;
    } else if (mode == GU_PATCH_CULL_FACE) {
        *gu_list++ = GECMD_ENA_PATCH_CULL<<24 | 1;
    } else if (mode == GU_COLOR_TEST) {
        *gu_list++ = GECMD_ENA_COLOR_TEST<<24 | 1;
    } else if (mode == GU_COLOR_LOGIC_OP) {
        *gu_list++ = GECMD_ENA_LOGIC_OP<<24 | 1;
    } else if (mode == GU_FACE_NORMAL_REVERSE) {
        *gu_list++ = GECMD_REV_NORMALS<<24 | 1;
    } else if (mode == GU_SCISSOR_TEST) {
        extern uint32_t gu_scissorcmd_min, gu_scissorcmd_max;
        extern uint8_t gu_scissor_enabled;
        gu_scissor_enabled = 1;
        *gu_list++ = gu_scissorcmd_min;
        *gu_list++ = gu_scissorcmd_max;
    }
}

static inline void guLogicalOp(const int op)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_LOGIC_OP<<24 | op;
}

static inline void guOffset(const int xofs, const int yofs)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_XOFFSET<<24 | xofs<<4;
    *gu_list++ = GECMD_YOFFSET<<24 | yofs<<4;
}

static inline void guPixelMask(const unsigned int mask)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_COLOR_MASK<<24 | (mask & 0xFFFFFF);
    *gu_list++ = GECMD_ALPHA_MASK<<24 | mask>>24;
}

static inline void guScissor(const int left, const int top,
                             const int width, const int height)
{
    extern uint32_t *gu_list;
    extern uint32_t gu_scissorcmd_min, gu_scissorcmd_max;
    extern uint8_t gu_scissor_enabled;
    gu_scissorcmd_min = GECMD_CLIP_MIN<<24 | top<<10 | left;
    gu_scissorcmd_max = GECMD_CLIP_MAX<<24 | (top+height-1)<<10
                                           | (left+width-1);
    if (gu_scissor_enabled) {
        *gu_list++ = gu_scissorcmd_min;
        *gu_list++ = gu_scissorcmd_max;
    }
}

/*
 * Note:  As with sceGuSetMatrix(), 4x3 matrices are laid out in a 4x4
 * array as follows:
 *    { {_11, _12, _13, _ignored},
 *      {_21, _22, _23, _ignored},
 *      {_31, _32, _33, _ignored},
 *      {_41, _42, _43, _ignored} }
 */
static inline void guSetMatrix(int type, const float *matrix)
{
    extern uint32_t *gu_list;
    if (type == GU_MODEL) {
        *gu_list++ = GECMD_MODEL_START<<24;
        unsigned int i;
        for (i = 0; i < 4; i++) {
            unsigned int j;
            for (j = 0; j < 3; j++) {
                *gu_list++ = GECMD_MODEL_UPLOAD<<24
                           | trim_float(matrix[i*4+j]);
            }
        }
    } else if (type == GU_VIEW) {
        *gu_list++ = GECMD_VIEW_START<<24;
        unsigned int i;
        for (i = 0; i < 4; i++) {
            unsigned int j;
            for (j = 0; j < 3; j++) {
                *gu_list++ = GECMD_VIEW_UPLOAD<<24
                           | trim_float(matrix[i*4+j]);
            }
        }
    } else if (type == GU_PROJECTION) {
        *gu_list++ = GECMD_PROJ_START<<24;
        unsigned int i;
        for (i = 0; i < 4; i++) {
            unsigned int j;
            for (j = 0; j < 4; j++) {
                *gu_list++ = GECMD_PROJ_UPLOAD<<24
                           | trim_float(matrix[i*4+j]);
            }
        }
    } else if (type == GU_TEXTURE) {
        *gu_list++ = GECMD_TEXTURE_START<<24;
        unsigned int i;
        for (i = 0; i < 4; i++) {
            unsigned int j;
            for (j = 0; j < 3; j++) {
                *gu_list++ = GECMD_TEXTURE_UPLOAD<<24
                           | trim_float(matrix[i*4+j]);
            }
        }
    }
}

static inline void guShadeModel(const int mode)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_SHADE_MODE<<24 | mode;
}

static inline void guStencilFunc(const int func, const int ref, const int mask)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_STENCILTEST<<24 | mask<<16 | ref<<8 | func;
}

static inline void guStencilOp(const int fail, const int zfail, const int zpass)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_STENCIL_OP<<24 | zpass<<16 | zfail<<8 | fail;
}

static inline void guTexFilter(const int min, const int mag)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_FILTER<<24 | mag<<8 | min;
}

static inline void guTexFlush(void)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_FLUSH<<24;
}

static inline void guTexFunc(const int func, const int alpha)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_FUNC<<24 | alpha<<8 | func;
}

static inline void guTexImage(const int level, const int width,
                              const int height, const int stride,
                              const void * const address)
{
    extern uint32_t *gu_list;
    const int log2_width  = 31 - __builtin_clz(width);
    const int log2_height = 31 - __builtin_clz(height);
    *gu_list++ = (GECMD_TEX0_ADDRESS+level)<<24 | ADDRESS_LO(address);
    *gu_list++ = (GECMD_TEX0_STRIDE+level)<<24  | ADDRESS_HI(address) | stride;
    *gu_list++ = (GECMD_TEX0_SIZE+level)<<24    | log2_height<<8 | log2_width;
}

static inline void guTexLevelMode(const int mode, const float bias)
{
    extern uint32_t *gu_list;
    const int bias_int = (int)(bias*16 + 0.5f) & 0xFF;
    *gu_list++ = GECMD_TEXTURE_BIAS<<24 | bias_int<<16 | mode;
}

static inline void guTexMode(const int format, const int mipmaps,
                             const int unknown, const int swizzle)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_MODE<<24   | (mipmaps ? mipmaps-1 : 0) << 16
                                          | swizzle;
    *gu_list++ = GECMD_TEXTURE_PIXFMT<<24 | format;
}

static inline void guTexSlope(const float slope)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_SLOPE<<24 | trim_float(slope);
}

static inline void guTexWrap(const int u_mode, const int v_mode)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_TEXTURE_WRAP<<24 | v_mode<<8 | u_mode;
}

/* Custom function to set vertex format independently of guDrawArray() */
static inline void guVertexFormat(const uint32_t format)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_VERTEX_FORMAT<<24 | format;
}

/* Custom function to set vertex pointer independently of guDrawArray() */
static inline void guVertexPointer(const void * const address)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_ADDRESS_BASE<<24   | ADDRESS_HI(address);
    *gu_list++ = GECMD_VERTEX_POINTER<<24 | ADDRESS_LO(address);
}

static inline void guViewport(const int cx, const int cy,
                              const int width, const int height)
{
    extern uint32_t *gu_list;
    *gu_list++ = GECMD_XPOS<<24   | trim_float(cx);
    *gu_list++ = GECMD_YPOS<<24   | trim_float(cy);
    *gu_list++ = GECMD_XSCALE<<24 | trim_float(width/2);
    *gu_list++ = GECMD_YSCALE<<24 | trim_float(-height/2);
}

/*************************************************************************/

#undef ADDRESS_HI
#undef ADDRESS_LO

#ifdef __GNUC__
# undef inline  // Cancel the always_inline definition above
#endif

#endif  // !USE_SCEGU

/*************************************************************************/
/************************* sceGu library aliases *************************/
/*************************************************************************/

#ifdef USE_SCEGU

#define guAlphaFunc(func,value,mask) \
    sceGuAlphaFunc((func), (value), (mask))
#define guAmbientColor(color) \
    sceGuAmbientColor((color))
#define guBlendFunc(func,src,dest,srcfix,destfix) \
    sceGuBlendFunc((func), (src), (dest), (srcfix), (destfix))
#define guCallList(list) \
    sceGuCallList((list))
#define guClear(mode) \
    sceGuClear((mode))
#define guClearColor(color) \
    sceGuClearColor((color))
#define guClearDepth(depth) \
    sceGuClearDepth((depth))
#define guClearStencil(stencil) \
    sceGuClearStencil((stencil))
#define guClutLoad(count,address) \
    sceGuClutLoad((count), (address))
#define guClutMode(format,shift,mask,unknown) \
    sceGuClutMode((format), (shift), (mask), (unknown))
#define guCopyImage(mode,src_x,src_y,src_w,src_h,src_stride,src_ptr,dest_x,dest_y,dest_stride,dest_ptr) \
    sceGuCopyImage((mode), (src_x), (src_y), (src_w), (src_h), (src_stride), \
                   (src_ptr), (dest_x), (dest_y), (dest_stride), (dest_ptr))
#define guDepthBuffer(address,stride) \
    sceGuDepthBuffer((address), (stride))
#define guDepthFunc(function) \
    sceGuDepthFunc((function))
#define guDepthMask(mask) \
    sceGuDepthMask((mask))
#define guDepthRange(near,far) \
    sceGuDepthRange((near), (far))
#define guDisable(mode) \
    sceGuDisable((mode))
#define guDispBuffer(width,height,address,stride) \
    sceGuDispBuffer((width), (height), (address), (stride))
#define guDrawArray(primitive,vertexfmt,count,indexptr,vertexptr) \
    sceGuDrawArray((primitive), (vertexfmt), (count), (indexptr), (vertexptr))
#define guDrawBuffer(format,address,stride) \
    sceGuDrawBuffer((format), (address), (stride))
#define guEnable(mode) \
    sceGuEnable((mode))
#define guFinish() \
    sceGuFinish()
#define guGetMemory(size) \
    sceGuGetMemory((size))
#define guInit() \
    sceGuInit()
#define guLogicalOp(op) \
    sceGuLogicalOp((op))
#define guOffset(xofs,yofs) \
    sceGuOffset((xofs), (yofs))
#define guPixelMask(mask) \
    sceGuPixelMask((mask))
#define guScissor(left,top,width,height) \
    sceGuScissor((left), (top), (width), (height))
#define guSetMatrix(type,matrix) \
    sceGuSetMatrix((type), (const ScePspFMatrix4 *)(matrix))
#define guShadeModel(mode) \
    sceGuShadeModel((mode))
#define guStart(type,list) \
    sceGuStart((type), (list))
#define guStencilFunc(func,ref,mask) \
    sceGuStencilFunc((func), (ref), (mask))
#define guStencilOp(fail,zfail,zpass) \
    sceGuStencilOp((fail), (zfail), (zpass))
#define guSync(mode,target) \
    sceGuSync((mode), (target))
#define guTexFilter(min,mag) \
    sceGuTexFilter((min), (mag))
#define guTexFlush() \
    sceGuTexFlush()
#define guTexFunc(func,alpha) \
    sceGuTexFunc((func), (alpha))
#define guTexImage(level,width,height,stride,address) \
    sceGuTexImage((level), (width), (height), (stride), (address))
#define guTexLevelMode(mode, bias) \
    sceGuTexLevelMode((mode), (bias))
#define guTexMode(format,mipmaps,unknown,swizzle) \
    sceGuTexMode((format), (mipmaps), (unknown), (swizzle))
#define guTexSlope(slope) \
    sceGuTexSlope((slope))
#define guTexWrap(u_mode,v_mode) \
    sceGuTexWrap((u_mode), (v_mode))
#define guViewport(cx,cy,width,height) \
    sceGuViewport((cx), (cy), (width), (height))

/* Custom functions */
#define guCommit()  /* no-op */
#define guDrawPrimitive(primitive,count) \
    sceGuSendCommandi(GECMD_DRAW_PRIMITIVE, (primitive)<<16 | (count))
#define guVertexFormat(format) \
    sceGuSendCommandi(GECMD_VERTEX_FORMAT, (format))
#define guVertexPointer(address)  do { \
    const uint32_t __address = (uint32_t)(address); \
    sceGuSendCommandi(GECMD_ADDRESS_BASE,  (__address & 0xFF000000) >> 8); \
    sceGuSendCommandi(GECMD_VERTEX_POINTER, __address & 0x00FFFFFF); \
} while (0)

#endif  // USE_SCEGU

/*************************************************************************/
/*************************************************************************/

#endif  // PSP_GU_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
