/*  src/psp/gu.c: sceGu substitute library
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

#include "common.h"

#include "gu.h"

/*************************************************************************/

#ifndef USE_SCEGU  // To the end of the file

/*************************************************************************/
/************************** Library-local data ***************************/
/*************************************************************************/

/**** Data exported to inline functions in gu.h ****/

/* Current display list pointer */
uint32_t *gu_list;

/* Scissor status flag and test region (in GE command form) */
uint32_t gu_scissorcmd_min, gu_scissorcmd_max;
uint8_t gu_scissor_enabled;

/* Color/stencil (combined) value and depth value for clear operations */
uint16_t gu_clear_depth;
uint32_t gu_clear_color_stencil;

/*----------------------------------*/

/**** Data used only within this file ****/

/* Nonzero if we're writing a sublist, zero if we're writing the main list */
static uint8_t gu_is_sublist;

/* Saved display list pointer (during sublist generation, the main list
 * pointer is pushed here) */
static uint32_t *gu_saved_list;

/* sceGe ID for current display list */
static uint32_t gu_list_id;

/*************************************************************************/
/*********** Library functions (which aren't defined in gu.h) ************/
/*************************************************************************/

/**
 * guInit:  Initialize the GU library.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void guInit(void)
{
    static const uint32_t ge_init_list[] = {
        GECMD_VERTEX_POINTER<<24 | 0x000000,
        GECMD_INDEX_POINTER <<24 | 0x000000,
        GECMD_ADDRESS_BASE  <<24 | 0x000000,
        GECMD_VERTEX_FORMAT <<24 | 0x000000,
        GECMD_UNKNOWN_13    <<24 | 0x000000,
        GECMD_DRAWAREA_LOW  <<24 | 0x000000,
        GECMD_DRAWAREA_HIGH <<24 | 0x000000,
        GECMD_ENA_LIGHTING  <<24 | 0x000000,
        GECMD_ENA_LIGHT0    <<24 | 0x000000,
        GECMD_ENA_LIGHT1    <<24 | 0x000000,
        GECMD_ENA_LIGHT2    <<24 | 0x000000,
        GECMD_ENA_LIGHT3    <<24 | 0x000000,
        GECMD_ENA_ZCLIP     <<24 | 0x000000,
        GECMD_ENA_FACE_CULL <<24 | 0x000000,
        GECMD_ENA_TEXTURE   <<24 | 0x000000,
        GECMD_ENA_FOG       <<24 | 0x000000,
        GECMD_ENA_DITHER    <<24 | 0x000000,
        GECMD_ENA_BLEND     <<24 | 0x000000,
        GECMD_ENA_ALPHA_TEST<<24 | 0x000000,
        GECMD_ENA_DEPTH_TEST<<24 | 0x000000,
        GECMD_ENA_STENCIL   <<24 | 0x000000,
        GECMD_ENA_ANTIALIAS <<24 | 0x000000,
        GECMD_ENA_PATCH_CULL<<24 | 0x000000,
        GECMD_ENA_COLOR_TEST<<24 | 0x000000,
        GECMD_ENA_LOGIC_OP  <<24 | 0x000000,
        GECMD_BONE_OFFSET   <<24 | 0x000000,
        GECMD_BONE_UPLOAD   <<24 | 0x000000,
        GECMD_MORPH_0       <<24 | 0x000000,
        GECMD_MORPH_1       <<24 | 0x000000,
        GECMD_MORPH_2       <<24 | 0x000000,
        GECMD_MORPH_3       <<24 | 0x000000,
        GECMD_MORPH_4       <<24 | 0x000000,
        GECMD_MORPH_5       <<24 | 0x000000,
        GECMD_MORPH_6       <<24 | 0x000000,
        GECMD_MORPH_7       <<24 | 0x000000,
        GECMD_PATCH_SUBDIV  <<24 | 0x000000,
        GECMD_PATCH_PRIM    <<24 | 0x000000,
        GECMD_PATCH_FRONT   <<24 | 0x000000,
        GECMD_MODEL_START   <<24 | 0x000000,
        GECMD_MODEL_UPLOAD  <<24 | 0x3F8000,
        GECMD_MODEL_UPLOAD  <<24 |           0x000000,
        GECMD_MODEL_UPLOAD  <<24 |                     0x000000,
        GECMD_MODEL_UPLOAD  <<24 | 0x000000,
        GECMD_MODEL_UPLOAD  <<24 |           0x3F8000,
        GECMD_MODEL_UPLOAD  <<24 |                     0x000000,
        GECMD_MODEL_UPLOAD  <<24 | 0x000000,
        GECMD_MODEL_UPLOAD  <<24 |           0x000000,
        GECMD_MODEL_UPLOAD  <<24 |                     0x3F8000,
        GECMD_MODEL_UPLOAD  <<24 | 0x000000,
        GECMD_MODEL_UPLOAD  <<24 |           0x000000,
        GECMD_MODEL_UPLOAD  <<24 |                     0x000000,
        GECMD_VIEW_START    <<24 | 0x000000,
        GECMD_VIEW_UPLOAD   <<24 | 0x3F8000,
        GECMD_VIEW_UPLOAD   <<24 |           0x000000,
        GECMD_VIEW_UPLOAD   <<24 |                     0x000000,
        GECMD_VIEW_UPLOAD   <<24 | 0x000000,
        GECMD_VIEW_UPLOAD   <<24 |           0x3F8000,
        GECMD_VIEW_UPLOAD   <<24 |                     0x000000,
        GECMD_VIEW_UPLOAD   <<24 | 0x000000,
        GECMD_VIEW_UPLOAD   <<24 |           0x000000,
        GECMD_VIEW_UPLOAD   <<24 |                     0x3F8000,
        GECMD_VIEW_UPLOAD   <<24 | 0x000000,
        GECMD_VIEW_UPLOAD   <<24 |           0x000000,
        GECMD_VIEW_UPLOAD   <<24 |                     0x000000,
        GECMD_PROJ_START    <<24 | 0x000000,
        GECMD_PROJ_UPLOAD   <<24 | 0x3F8000,
        GECMD_PROJ_UPLOAD   <<24 |           0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                     0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                               0x000000,
        GECMD_PROJ_UPLOAD   <<24 | 0x000000,
        GECMD_PROJ_UPLOAD   <<24 |           0x3F8000,
        GECMD_PROJ_UPLOAD   <<24 |                     0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                               0x000000,
        GECMD_PROJ_UPLOAD   <<24 | 0x000000,
        GECMD_PROJ_UPLOAD   <<24 |           0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                     0x3F8000,
        GECMD_PROJ_UPLOAD   <<24 |                               0x000000,
        GECMD_PROJ_UPLOAD   <<24 | 0x000000,
        GECMD_PROJ_UPLOAD   <<24 |           0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                     0x000000,
        GECMD_PROJ_UPLOAD   <<24 |                               0x3F8000,
        GECMD_TEXTURE_START <<24 | 0x000000,
        GECMD_TEXTURE_UPLOAD<<24 | 0x3F8000,
        GECMD_TEXTURE_UPLOAD<<24 |           0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |                     0x000000,
        GECMD_TEXTURE_UPLOAD<<24 | 0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |           0x3F8000,
        GECMD_TEXTURE_UPLOAD<<24 |                     0x000000,
        GECMD_TEXTURE_UPLOAD<<24 | 0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |           0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |                     0x3F8000,
        GECMD_TEXTURE_UPLOAD<<24 | 0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |           0x000000,
        GECMD_TEXTURE_UPLOAD<<24 |                     0x000000,
        GECMD_XSCALE        <<24 | 0x000000,
        GECMD_YSCALE        <<24 | 0x000000,
        GECMD_ZSCALE        <<24 | 0x000000,
        GECMD_XPOS          <<24 | 0x000000,
        GECMD_YPOS          <<24 | 0x000000,
        GECMD_ZPOS          <<24 | 0x000000,
        GECMD_USCALE        <<24 | 0x3F8000,
        GECMD_VSCALE        <<24 | 0x3F8000,
        GECMD_UOFFSET       <<24 | 0x000000,
        GECMD_VOFFSET       <<24 | 0x000000,
        GECMD_XOFFSET       <<24 | 0x000000,
        GECMD_YOFFSET       <<24 | 0x000000,
        GECMD_SHADE_MODE    <<24 | 0x000000,
        GECMD_REV_NORMALS   <<24 | 0x000000,
        GECMD_COLOR_MATERIAL<<24 | 0x000000,
        GECMD_EMISSIVE_COLOR<<24 | 0x000000,
        GECMD_AMBIENT_COLOR <<24 | 0x000000,
        GECMD_DIFFUSE_COLOR <<24 | 0x000000,
        GECMD_SPECULAR_COLOR<<24 | 0x000000,
        GECMD_AMBIENT_ALPHA <<24 | 0x000000,
        GECMD_SPECULAR_POWER<<24 | 0x000000,
        GECMD_LIGHT_AMBCOLOR<<24 | 0x000000,
        GECMD_LIGHT_AMBALPHA<<24 | 0x000000,
        GECMD_LIGHT_MODEL   <<24 | 0x000000,
        GECMD_LIGHT0_TYPE   <<24 | 0x000000,
        GECMD_LIGHT1_TYPE   <<24 | 0x000000,
        GECMD_LIGHT2_TYPE   <<24 | 0x000000,
        GECMD_LIGHT3_TYPE   <<24 | 0x000000,
        GECMD_LIGHT0_XPOS   <<24 | 0x000000,
        GECMD_LIGHT0_YPOS   <<24 | 0x000000,
        GECMD_LIGHT0_ZPOS   <<24 | 0x000000,
        GECMD_LIGHT1_XPOS   <<24 | 0x000000,
        GECMD_LIGHT1_YPOS   <<24 | 0x000000,
        GECMD_LIGHT1_ZPOS   <<24 | 0x000000,
        GECMD_LIGHT2_XPOS   <<24 | 0x000000,
        GECMD_LIGHT2_YPOS   <<24 | 0x000000,
        GECMD_LIGHT2_ZPOS   <<24 | 0x000000,
        GECMD_LIGHT3_XPOS   <<24 | 0x000000,
        GECMD_LIGHT3_YPOS   <<24 | 0x000000,
        GECMD_LIGHT3_ZPOS   <<24 | 0x000000,
        GECMD_LIGHT0_XDIR   <<24 | 0x000000,
        GECMD_LIGHT0_YDIR   <<24 | 0x000000,
        GECMD_LIGHT0_ZDIR   <<24 | 0x000000,
        GECMD_LIGHT1_XDIR   <<24 | 0x000000,
        GECMD_LIGHT1_YDIR   <<24 | 0x000000,
        GECMD_LIGHT1_ZDIR   <<24 | 0x000000,
        GECMD_LIGHT2_XDIR   <<24 | 0x000000,
        GECMD_LIGHT2_YDIR   <<24 | 0x000000,
        GECMD_LIGHT2_ZDIR   <<24 | 0x000000,
        GECMD_LIGHT3_XDIR   <<24 | 0x000000,
        GECMD_LIGHT3_YDIR   <<24 | 0x000000,
        GECMD_LIGHT3_ZDIR   <<24 | 0x000000,
        GECMD_LIGHT0_CATT   <<24 | 0x000000,
        GECMD_LIGHT0_LATT   <<24 | 0x000000,
        GECMD_LIGHT0_QATT   <<24 | 0x000000,
        GECMD_LIGHT1_CATT   <<24 | 0x000000,
        GECMD_LIGHT1_LATT   <<24 | 0x000000,
        GECMD_LIGHT1_QATT   <<24 | 0x000000,
        GECMD_LIGHT2_CATT   <<24 | 0x000000,
        GECMD_LIGHT2_LATT   <<24 | 0x000000,
        GECMD_LIGHT2_QATT   <<24 | 0x000000,
        GECMD_LIGHT3_CATT   <<24 | 0x000000,
        GECMD_LIGHT3_LATT   <<24 | 0x000000,
        GECMD_LIGHT3_QATT   <<24 | 0x000000,
        GECMD_LIGHT0_SPOTEXP<<24 | 0x000000,
        GECMD_LIGHT1_SPOTEXP<<24 | 0x000000,
        GECMD_LIGHT2_SPOTEXP<<24 | 0x000000,
        GECMD_LIGHT3_SPOTEXP<<24 | 0x000000,
        GECMD_LIGHT0_SPOTLIM<<24 | 0x000000,
        GECMD_LIGHT1_SPOTLIM<<24 | 0x000000,
        GECMD_LIGHT2_SPOTLIM<<24 | 0x000000,
        GECMD_LIGHT3_SPOTLIM<<24 | 0x000000,
        GECMD_LIGHT0_ACOL   <<24 | 0x000000,
        GECMD_LIGHT0_DCOL   <<24 | 0x000000,
        GECMD_LIGHT0_SCOL   <<24 | 0x000000,
        GECMD_LIGHT1_ACOL   <<24 | 0x000000,
        GECMD_LIGHT1_DCOL   <<24 | 0x000000,
        GECMD_LIGHT1_SCOL   <<24 | 0x000000,
        GECMD_LIGHT2_ACOL   <<24 | 0x000000,
        GECMD_LIGHT2_DCOL   <<24 | 0x000000,
        GECMD_LIGHT2_SCOL   <<24 | 0x000000,
        GECMD_LIGHT3_ACOL   <<24 | 0x000000,
        GECMD_LIGHT3_DCOL   <<24 | 0x000000,
        GECMD_LIGHT3_SCOL   <<24 | 0x000000,
        GECMD_FACE_ORDER    <<24 | 0x000000,
        GECMD_DRAW_ADDRESS  <<24 | 0x000000,
        GECMD_DRAW_STRIDE   <<24 | 0x000000,
        GECMD_DEPTH_ADDRESS <<24 | 0x000000,
        GECMD_DEPTH_STRIDE  <<24 | 0x000000,
        GECMD_TEX0_ADDRESS  <<24 | 0x000000,
        GECMD_TEX1_ADDRESS  <<24 | 0x000000,
        GECMD_TEX2_ADDRESS  <<24 | 0x000000,
        GECMD_TEX3_ADDRESS  <<24 | 0x000000,
        GECMD_TEX4_ADDRESS  <<24 | 0x000000,
        GECMD_TEX5_ADDRESS  <<24 | 0x000000,
        GECMD_TEX6_ADDRESS  <<24 | 0x000000,
        GECMD_TEX7_ADDRESS  <<24 | 0x000000,
        GECMD_TEX0_STRIDE   <<24 | 0x040004,
        GECMD_TEX1_STRIDE   <<24 | 0x000000,
        GECMD_TEX2_STRIDE   <<24 | 0x000000,
        GECMD_TEX3_STRIDE   <<24 | 0x000000,
        GECMD_TEX4_STRIDE   <<24 | 0x000000,
        GECMD_TEX5_STRIDE   <<24 | 0x000000,
        GECMD_TEX6_STRIDE   <<24 | 0x000000,
        GECMD_TEX7_STRIDE   <<24 | 0x000000,
        GECMD_CLUT_ADDRESS_L<<24 | 0x000000,
        GECMD_CLUT_ADDRESS_H<<24 | 0x000000,
        GECMD_COPY_S_ADDRESS<<24 | 0x000000,
        GECMD_COPY_S_STRIDE <<24 | 0x000000,
        GECMD_COPY_D_ADDRESS<<24 | 0x000000,
        GECMD_COPY_D_STRIDE <<24 | 0x000000,
        GECMD_TEX0_SIZE     <<24 | 0x000101,
        GECMD_TEX1_SIZE     <<24 | 0x000000,
        GECMD_TEX2_SIZE     <<24 | 0x000000,
        GECMD_TEX3_SIZE     <<24 | 0x000000,
        GECMD_TEX4_SIZE     <<24 | 0x000000,
        GECMD_TEX5_SIZE     <<24 | 0x000000,
        GECMD_TEX6_SIZE     <<24 | 0x000000,
        GECMD_TEX7_SIZE     <<24 | 0x000000,
        GECMD_TEXTURE_MAP   <<24 | 0x000000,
        GECMD_TEXTURE_ENVMAP<<24 | 0x000000,
        GECMD_TEXTURE_MODE  <<24 | 0x000000,
        GECMD_TEXTURE_PIXFMT<<24 | 0x000000,
        GECMD_CLUT_LOAD     <<24 | 0x000000,
        GECMD_CLUT_MODE     <<24 | 0x000000,
        GECMD_TEXTURE_FILTER<<24 | 0x000000,
        GECMD_TEXTURE_WRAP  <<24 | 0x000000,
        GECMD_TEXTURE_BIAS  <<24 | 0x000000,
        GECMD_TEXTURE_FUNC  <<24 | 0x000000,
        GECMD_TEXTURE_COLOR <<24 | 0x000000,
        GECMD_TEXTURE_FLUSH <<24 | 0x000000,
        GECMD_COPY_SYNC     <<24 | 0x000000,
        GECMD_FOG_LIMIT     <<24 | 0x000000,
        GECMD_FOG_RANGE     <<24 | 0x000000,
        GECMD_FOG_COLOR     <<24 | 0x000000,
        GECMD_TEXTURE_SLOPE <<24 | 0x000000,
        GECMD_FRAME_PIXFMT  <<24 | 0x000000,
        GECMD_CLEAR_MODE    <<24 | 0x000000,
        GECMD_CLIP_MIN      <<24 | 0x000000,
        GECMD_CLIP_MAX      <<24 | 0x000000,
        GECMD_CLIP_NEAR     <<24 | 0x000000,
        GECMD_CLIP_FAR      <<24 | 0x000000,
        GECMD_COLORTEST_FUNC<<24 | 0x000000,
        GECMD_COLORTEST_REF <<24 | 0x000000,
        GECMD_COLORTEST_MASK<<24 | 0x000000,
        GECMD_ALPHATEST     <<24 | 0x000000,
        GECMD_STENCILTEST   <<24 | 0x000000,
        GECMD_STENCIL_OP    <<24 | 0x000000,
        GECMD_DEPTHTEST     <<24 | 0x000000,
        GECMD_BLEND_FUNC    <<24 | 0x000000,
        GECMD_BLEND_SRCFIX  <<24 | 0x000000,
        GECMD_BLEND_DSTFIX  <<24 | 0x000000,
        GECMD_DITHER0       <<24 | 0x000000,
        GECMD_DITHER1       <<24 | 0x000000,
        GECMD_DITHER2       <<24 | 0x000000,
        GECMD_DITHER3       <<24 | 0x000000,
        GECMD_LOGIC_OP      <<24 | 0x000000,
        GECMD_DEPTH_MASK    <<24 | 0x000000,
        GECMD_COLOR_MASK    <<24 | 0x000000,
        GECMD_ALPHA_MASK    <<24 | 0x000000,
        GECMD_COPY_S_POS    <<24 | 0x000000,
        GECMD_COPY_D_POS    <<24 | 0x000000,
        GECMD_COPY_SIZE     <<24 | 0x000000,
        GECMD_UNKNOWN_F0    <<24 | 0x000000,
        GECMD_UNKNOWN_F1    <<24 | 0x000000,
        GECMD_UNKNOWN_F2    <<24 | 0x000000,
        GECMD_UNKNOWN_F3    <<24 | 0x000000,
        GECMD_UNKNOWN_F4    <<24 | 0x000000,
        GECMD_UNKNOWN_F5    <<24 | 0x000000,
        GECMD_UNKNOWN_F6    <<24 | 0x000000,
        GECMD_UNKNOWN_F7    <<24 | 0x000000,
        GECMD_UNKNOWN_F8    <<24 | 0x000000,
        GECMD_UNKNOWN_F9    <<24 | 0x000000,
        GECMD_FINISH        <<24 | 0x000000,
        GECMD_END           <<24 | 0x000000,
        GECMD_NOP           <<24 | 0x000000,
        GECMD_NOP           <<24 | 0x000000,
    };

    int listid = sceGeListEnQueue(ge_init_list, NULL, 0, 0);
    sceGeListSync(listid, PSP_GE_LIST_DONE);
}

/*************************************************************************/

/**
 * guStart:  Start a new display list.
 *
 * [Parameters]
 *     type: List type (GU_DIRECT or GU_CALL)
 *     list: List pointer
 */
void guStart(const int type, void * const list)
{
    if (type == GU_CALL) {
        gu_saved_list = gu_list;
        gu_is_sublist = 1;
    } else {
        gu_is_sublist = 0;
        gu_list_id = sceGeListEnQueue(list, list, 0, 0);
    }
    gu_list = list;
}

/*-----------------------------------------------------------------------*/

/**
 * guFinish:  End the current display list.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void guFinish(void)
{
    if (gu_is_sublist) {
        *gu_list = GECMD_RETURN<<24;
        gu_list = gu_saved_list;
        gu_saved_list = NULL;
        gu_is_sublist = 0;
    } else {
        *gu_list++ = GECMD_FINISH<<24;
        *gu_list++ = GECMD_END<<24;
        guCommit();
        gu_list = NULL;
    }
}

/*************************************************************************/

/**
 * guCommit:  Commit all previously-added commands to memory and start
 * GE processing.  Does nothing if the current display list is not a
 * GU_DIRECT list.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void guCommit(void)
{
    if (gu_list && !gu_is_sublist) {
        /* If a partially-used cache line hangs around until the next frame
         * and the next frame's display list is different, we could end up
         * clobbering the display list with old data, so invalidate all the
         * cache lines we touched in addition to writing them back to
         * memory. */
        sceKernelDcacheWritebackInvalidateAll();
        sceGeListUpdateStallAddr(gu_list_id, gu_list);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * guSync:  Wait for GE processing to complete.
 *
 * [Parameters]
 *       mode: Synchronization mode (GU_SYNC_*)
 *     target: Synchronization target (GU_SYNC_WHAT_*)
 * [Return value]
 *     None
 */
void guSync(const int mode, const int target)
{
    if (gu_list_id) {
        if (mode != GU_SYNC_FINISH || target != GU_SYNC_WHAT_DONE) {
            return;
        }
        sceGeDrawSync(PSP_GE_LIST_DONE);
        sceGeListDeQueue(gu_list_id);
        gu_list_id = 0;
    }
}

/*************************************************************************/

/**
 * guGetMemory:  Allocate a block of memory from the current GU_DIRECT
 * list.  The allocation size will automatically be rounded up to a
 * multiple of 4 bytes.
 *
 * This function may not be called unless there is a current GU_DIRECT
 * list.
 *
 * [Parameters]
 *     size: Number of bytes to allocate
 */
void *guGetMemory(const uint32_t size)
{
    uint32_t **list_ref = (gu_is_sublist ? &gu_saved_list : &gu_list);
    const uint32_t alloc_words = (size + 3) / 4;
    uint32_t * const alloc_ptr = (*list_ref) + 2;
    uint32_t * const target = alloc_ptr + alloc_words;
    (*list_ref)[0] = GECMD_ADDRESS_BASE<<24
                   | ((uint32_t)target & 0xFF000000) >> 8;
    (*list_ref)[1] = GECMD_JUMP<<24
                   | ((uint32_t)target & 0x00FFFFFF);
    (*list_ref) = target;
    return alloc_ptr;
}

/*************************************************************************/

#endif  // USE_SCEGU

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
