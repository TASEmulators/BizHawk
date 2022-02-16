/* Copyright (c) 2020 Themaister
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#ifndef DATA_STRUCTURES_BUFFERS_H_
#define DATA_STRUCTURES_BUFFERS_H_

#include "data_structures.h"

layout(set = 0, binding = 0, std430) buffer VRAM32
{
	uint data[];
} vram32;

layout(set = 0, binding = 0, std430) buffer VRAM16
{
	mem_u16 data[];
} vram16;

layout(set = 0, binding = 0, std430) buffer VRAM8
{
	mem_u8 data[];
} vram8;

layout(set = 0, binding = 1, std430) buffer HiddenVRAM
{
	mem_u8 data[];
} hidden_vram;

layout(set = 0, binding = 2, std430) readonly buffer TMEM16
{
	TMEMInstance16Mem instances[];
} tmem16;

layout(set = 0, binding = 2, std430) readonly buffer TMEM8
{
	TMEMInstance8Mem instances[];
} tmem8;

layout(set = 1, binding = 0, std430) readonly buffer TriangleSetupBuffer
{
	TriangleSetupMem elems[];
} triangle_setup;
#include "load_triangle_setup.h"

layout(set = 1, binding = 1, std430) readonly buffer AttributeSetupBuffer
{
	AttributeSetupMem elems[];
} attribute_setup;
#include "load_attribute_setup.h"

layout(set = 1, binding = 2, std430) readonly buffer DerivedSetupBuffer
{
	DerivedSetupMem elems[];
} derived_setup;
#include "load_derived_setup.h"

layout(set = 1, binding = 3, std430) readonly buffer ScissorStateBuffer
{
	ScissorStateMem elems[];
} scissor_state;
#include "load_scissor_state.h"

layout(set = 1, binding = 4, std430) readonly buffer StaticRasterStateBuffer
{
	StaticRasterizationStateMem elems[];
} static_raster_state;
#include "load_static_raster_state.h"

layout(set = 1, binding = 5, std430) readonly buffer DepthBlendStateBuffer
{
	DepthBlendStateMem elems[];
} depth_blend_state;
#include "load_depth_blend_state.h"

layout(set = 1, binding = 6, std430) readonly buffer StateIndicesBuffer
{
	InstanceIndicesMem elems[];
} state_indices;

layout(set = 1, binding = 7, std430) readonly buffer TileInfoBuffer
{
	TileInfoMem elems[];
} tile_infos;
#include "load_tile_info.h"

layout(set = 1, binding = 8, std430) readonly buffer SpanSetups
{
	SpanSetupMem elems[];
} span_setups;
#include "load_span_setup.h"

layout(set = 1, binding = 9, std430) readonly buffer SpanInfoOffsetBuffer
{
	SpanInfoOffsetsMem elems[];
} span_offsets;
#include "load_span_offsets.h"

layout(set = 1, binding = 10) uniform utextureBuffer uBlenderDividerLUT;

layout(set = 1, binding = 11, std430) readonly buffer TileBinning
{
	uint elems[];
} tile_binning;

layout(set = 1, binding = 12, std430) readonly buffer TileBinningCoarse
{
	uint elems[];
} tile_binning_coarse;

layout(set = 2, binding = 0, std140) uniform GlobalConstants
{
	GlobalFBInfo fb_info;
} global_constants;

#endif