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

#ifndef MEMORY_INTERFACING_H_
#define MEMORY_INTERFACING_H_

#include "dither.h"
#include "z_encode.h"
#include "blender.h"
#include "depth_test.h"
#include "coverage.h"
#include "fb_formats.h"

layout(constant_id = 0) const uint RDRAM_SIZE = 0;

layout(constant_id = 7) const int RDRAM_INCOHERENT_SCALING = 0;
const bool RDRAM_INCOHERENT = (RDRAM_INCOHERENT_SCALING & 1) != 0;
const int SCALING_LOG2 = RDRAM_INCOHERENT_SCALING >> 1;
const int SCALING_FACTOR = 1 << SCALING_LOG2;
const bool RDRAM_UNSCALED_WRITE_MASK = RDRAM_INCOHERENT && SCALING_LOG2 == 0;
const bool RDRAM_SCALED_WRITE_MASK = RDRAM_INCOHERENT && SCALING_LOG2 != 0;

const uint RDRAM_MASK_8 = RDRAM_SIZE - 1u;
const uint RDRAM_MASK_16 = RDRAM_MASK_8 >> 1u;
const uint RDRAM_MASK_32 = RDRAM_MASK_8 >> 2u;

layout(constant_id = 1) const int FB_FMT = 0;
layout(constant_id = 2) const bool FB_COLOR_DEPTH_ALIAS = false;

u8x4 current_color;
bool current_color_dirty;

u16 current_depth;
u8 current_dz;
bool current_depth_dirty;

void load_vram_color(uint index, uint slice)
{
	switch (FB_FMT)
	{
	case FB_FMT_I4:
	case FB_FMT_I8:
	{
		index &= RDRAM_MASK_8;
		index += slice * RDRAM_SIZE;
		u8 word = u8(vram8.data[index ^ 3u]);
		current_color = u8x4(word, word, word, u8(hidden_vram.data[index >> 1]));
		break;
	}

	case FB_FMT_RGBA5551:
	{
		index &= RDRAM_MASK_16;
		index += slice * (RDRAM_SIZE >> 1);
		uint word = uint(vram16.data[index ^ 1u]);
		uvec3 rgb = uvec3(word >> 8u, word >> 3u, word << 2u) & 0xf8u;
		current_color = u8x4(rgb, (u8(hidden_vram.data[index]) << U8_C(5)) | u8((word & 1) << 7));
		break;
	}

	case FB_FMT_IA88:
	{
		index &= RDRAM_MASK_16;
		index += slice * (RDRAM_SIZE >> 1);
		uint word = uint(vram16.data[index ^ 1u]);
		current_color = u8x4(u8x3(word >> 8u), word & 0xff);
		break;
	}

	case FB_FMT_RGBA8888:
	{
		index &= RDRAM_MASK_32;
		index += slice * (RDRAM_SIZE >> 2);
		uint word = vram32.data[index];
		current_color = u8x4((uvec4(word) >> uvec4(24, 16, 8, 0)) & uvec4(0xff));
		break;
	}
	}
}

void alias_color_to_depth()
{
	/* Inherit memory depth from color. */
	switch (FB_FMT)
	{
	case FB_FMT_RGBA5551:
	{
		current_dz = (current_color.a >> U8_C(3)) | (current_color.b & U8_C(8));
		uint word = (current_color.r & 0xf8u) << 6u;
		word |= (current_color.g & 0xf8u) << 1u;
		word |= (current_color.b & 0xf8u) >> 4u;
		current_depth = u16(word);
		break;
	}

	case FB_FMT_IA88:
	{
		uvec2 col = current_color.ra;
		uint word = (col.x << 8u) | col.y;
		uint hidden_word = (word & 1u) * 3u;
		current_depth = u16(word >> 2u);
		current_dz = u8(((word & 3u) << 2u) | hidden_word);
		break;
	}
	}
}

void alias_depth_to_color()
{
	uint word = (uint(current_depth) << 4u) | current_dz;

	switch (FB_FMT)
	{
	case FB_FMT_RGBA5551:
	{
		current_color.r = u8((word >> 10u) & 0xf8u);
		current_color.g = u8((word >> 5u) & 0xf8u);
		current_color.b = u8((word >> 0u) & 0xf8u);
		current_color.a = u8((word & 7u) << 5u);
		break;
	}

	case FB_FMT_IA88:
	{
		current_color.r = u8((word >> 10u) & 0xffu);
		current_color.a = u8((word >> 2u) & 0xffu);
		break;
	}
	}

	current_color_dirty = true;
}

void load_vram_depth(uint index, uint slice)
{
	index &= RDRAM_MASK_16;
	index += slice * (RDRAM_SIZE >> 1);
	u16 word = u16(vram16.data[index ^ 1u]);
	current_depth = word >> U16_C(2);
	current_dz = u8(hidden_vram.data[index]) | u8((word & U16_C(3)) << U16_C(2));
}

void store_vram_color(uint index, uint slice)
{
	//GENERIC_MESSAGE1(index);
	if (current_color_dirty)
	{
		switch (FB_FMT)
		{
		case FB_FMT_I4:
		{
			index &= RDRAM_MASK_8;
			index += slice * RDRAM_SIZE;
			vram8.data[index ^ 3u] = mem_u8(0);
			if ((index & 1u) != 0u)
				hidden_vram.data[index >> 1u] = mem_u8(current_color.a);

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram8.data[(index ^ 3u) + RDRAM_SIZE] = mem_u8(0xff);
			}
			break;
		}

		case FB_FMT_I8:
		{
			index &= RDRAM_MASK_8;
			index += slice * RDRAM_SIZE;
			vram8.data[index ^ 3u] = mem_u8(current_color.r);
			if ((index & 1u) != 0u)
				hidden_vram.data[index >> 1u] = mem_u8((current_color.r & 1) * 3);

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram8.data[(index ^ 3u) + RDRAM_SIZE] = mem_u8(0xff);
			}
			break;
		}

		case FB_FMT_RGBA5551:
		{
			index &= RDRAM_MASK_16;
			index += slice * (RDRAM_SIZE >> 1);
			uvec4 c = uvec4(current_color);
			c.rgb &= 0xf8u;
			uint cov = c.w >> 5u;
			uint word = (c.x << 8u) | (c.y << 3u) | (c.z >> 2u) | (cov >> 2u);
			vram16.data[index ^ 1u] = mem_u16(word);
			hidden_vram.data[index] = mem_u8(cov & U8_C(3));

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram16.data[(index ^ 1u) + (RDRAM_SIZE >> 1u)] = mem_u16(0xffff);
			}
			break;
		}

		case FB_FMT_IA88:
		{
			index &= RDRAM_MASK_16;
			index += slice * (RDRAM_SIZE >> 1);
			uvec2 col = current_color.ra;
			uint word = (col.x << 8u) | col.y;
			vram16.data[index ^ 1u] = mem_u16(word);
			hidden_vram.data[index] = mem_u8((col.y & 1) * 3);

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram16.data[(index ^ 1u) + (RDRAM_SIZE >> 1u)] = mem_u16(0xffff);
			}
			break;
		}

		case FB_FMT_RGBA8888:
		{
			index &= RDRAM_MASK_32;
			index += slice * (RDRAM_SIZE >> 2);
			uvec4 col = current_color;
			uint word = (col.r << 24u) | (col.g << 16u) | (col.b << 8u) | (col.a << 0u);
			vram32.data[index] = word;
			hidden_vram.data[2u * index] = mem_u8((current_color.g & 1) * 3);
			hidden_vram.data[2u * index + 1u] = mem_u8((current_color.a & 1) * 3);

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram32.data[index + (RDRAM_SIZE >> 2u)] = ~0u;
			}
			break;
		}
		}
	}
}

void store_vram_depth(uint index, uint slice)
{
	if (!FB_COLOR_DEPTH_ALIAS)
	{
		//GENERIC_MESSAGE1(index);
		if (current_depth_dirty)
		{
			index &= RDRAM_MASK_16;
			index += slice * (RDRAM_SIZE >> 1);
			vram16.data[index ^ 1u] = mem_u16((current_depth << U16_C(2)) | (current_dz >> U16_C(2)));
			hidden_vram.data[index] = mem_u8(current_dz & U16_C(3));

			if (RDRAM_UNSCALED_WRITE_MASK)
			{
				// Need this memory barrier to ensure the mask readback does not read
				// an invalid value from RDRAM. If the mask is seen, the valid RDRAM value is
				// also coherent.
				memoryBarrierBuffer();
				vram16.data[(index ^ 1) + (RDRAM_SIZE >> 1u)] = mem_u16(0xffff);
			}
		}
	}
}

uint color_fb_index;

void init_tile(uvec2 coord, uint fb_width, uint fb_height, uint fb_addr_index, uint fb_depth_addr_index)
{
	current_color_dirty = false;
	current_depth_dirty = false;
	if (all(lessThan(coord, uvec2(fb_width, fb_height))))
	{
		uvec2 slice2d = coord & (SCALING_FACTOR - 1);
		coord >>= SCALING_LOG2;
		uint slice = slice2d.y * SCALING_FACTOR + slice2d.x;

		uint index = fb_addr_index + (fb_width >> SCALING_LOG2) * coord.y + coord.x;
		color_fb_index = index;
		load_vram_color(index, slice);

		index = fb_depth_addr_index + (fb_width >> SCALING_LOG2) * coord.y + coord.x;
		load_vram_depth(index, slice);
	}
}

void emit_scaled_write_masks(uvec2 unscaled_coord, uint unscaled_fb_width)
{
	// Merge write masks across pixels.
	// We reserved a chunk of memory after scaled RDRAM to store 2 bits per pixel holding
	// a write mask for color and depth. The resolve stage will only resolve a pixel
	// and trigger a write if any sub-sample was marked as written.

	// Write masks are organized in 4x4 blocks of unscaled pixels for locality purposes.
	// This guarantees a minimum number of loop iterations to resolve the write masks.
	uint unscaled_block = (unscaled_coord.y >> 2u) * ((unscaled_fb_width + 3u) >> 2u) + (unscaled_coord.x >> 2u);
	uvec2 unscaled_sub = unscaled_coord & 3u;
	uint word = uint(current_color_dirty) + 2u * uint(current_depth_dirty);
	word <<= 2u * (unscaled_sub.x + unscaled_sub.y * 4u);

#if SUBGROUP
	// This should only need one iteration .
	bool is_active = true;
	do
	{
		if (subgroupBroadcastFirst(unscaled_block) == unscaled_block)
		{
			uint merged = subgroupOr(word);
			if (subgroupElect())
				atomicOr(vram32.data[SCALING_FACTOR * SCALING_FACTOR * (RDRAM_SIZE >> 2) + unscaled_block], merged);
			is_active = false;
		}
	} while (is_active);
#else
	// Just use atomics directly. With subgroup support, we can be a bit smarter about it.
	if (word != 0u)
		atomicOr(vram32.data[SCALING_FACTOR * SCALING_FACTOR * (RDRAM_SIZE >> 2) + unscaled_block], word);
#endif
}

void finish_tile(uvec2 coord, uint fb_width, uint fb_height, uint fb_addr_index, uint fb_depth_addr_index)
{
	if (all(lessThan(coord, uvec2(fb_width, fb_height))))
	{
		uint unscaled_fb_width = fb_width >> SCALING_LOG2;

		uvec2 slice2d = coord & (SCALING_FACTOR - 1);
		coord >>= SCALING_LOG2;
		uint slice = slice2d.y * SCALING_FACTOR + slice2d.x;

		uint index = fb_addr_index + unscaled_fb_width * coord.y + coord.x;
		store_vram_color(index, slice);

		index = fb_depth_addr_index + unscaled_fb_width * coord.y + coord.x;
		store_vram_depth(index, slice);

		if (RDRAM_SCALED_WRITE_MASK)
			emit_scaled_write_masks(coord, unscaled_fb_width);
	}
}

u8x4 decode_memory_color(bool image_read_en)
{
	u8 memory_coverage = image_read_en ? (current_color.a & U8_C(0xe0)) : U8_C(0xe0);

	u8x3 color;
	switch (FB_FMT)
	{
	case FB_FMT_I4:
		color = u8x3(0);
		memory_coverage = U8_C(0xe0);
		break;

	case FB_FMT_I8:
		color = current_color.rrr;
		memory_coverage = U8_C(0xe0);
		break;

	case FB_FMT_RGBA5551:
		color = current_color.rgb & U8_C(0xf8);
		break;

	case FB_FMT_IA88:
		color = current_color.rrr;
		break;

	case FB_FMT_RGBA8888:
		color = current_color.rgb;
		break;
	}
	return u8x4(color, memory_coverage);
}

void write_color(u8x4 col)
{
	if (FB_FMT == FB_FMT_I4)
		current_color.rgb = col.rgb;
	else
		current_color = col;
	current_color_dirty = true;
}

void copy_pipeline(uint word, uint primitive_index)
{
	switch (FB_FMT)
	{
	case FB_FMT_I4:
	{
		current_color = u8x4(0);
		current_color_dirty = true;
		break;
	}

	case FB_FMT_I8:
	{
		// Alpha testing needs to only look at the low dword for some bizarre reason.
		// I don't think alpha testing is supposed to be used at all with 8-bit FB ...
		word &= 0xffu;
		write_color(u8x4(word));
		break;
	}

	case FB_FMT_RGBA5551:
	{
		uint r = (word >> 8) & 0xf8u;
		uint g = (word >> 3) & 0xf8u;
		uint b = (word << 2) & 0xf8u;
		uint a = (word & 1) * 0xe0u;
		write_color(u8x4(r, g, b, a));
		break;
	}
	}

	if (FB_COLOR_DEPTH_ALIAS)
		alias_color_to_depth();
}

void fill_color(uint col)
{
	switch (FB_FMT)
	{
	case FB_FMT_RGBA8888:
	{
		uint r = (col >> 24u) & 0xffu;
		uint g = (col >> 16u) & 0xffu;
		uint b = (col >> 8u) & 0xffu;
		uint a = (col >> 0u) & 0xffu;
		write_color(u8x4(r, g, b, a));
		break;
	}

	case FB_FMT_RGBA5551:
	{
		col >>= ((color_fb_index & 1u) ^ 1u) * 16u;
		uint r = (col >> 8u) & 0xf8u;
		uint g = (col >> 3u) & 0xf8u;
		uint b = (col << 2u) & 0xf8u;
		uint a = (col & 1u) * 0xe0u;
		write_color(u8x4(r, g, b, a));
		break;
	}

	case FB_FMT_IA88:
	{
		col >>= ((color_fb_index & 1u) ^ 1u) * 16u;
		col &= 0xffffu;
		uint r = (col >> 8u) & 0xffu;
		uint a = (col >> 0u) & 0xffu;
		write_color(u8x4(r, r, r, a));
		break;
	}

	case FB_FMT_I8:
	{
		col >>= ((color_fb_index & 3u) ^ 3u) * 8u;
		col &= 0xffu;
		write_color(u8x4(col));
		break;
	}
	}

	if (FB_COLOR_DEPTH_ALIAS)
		alias_color_to_depth();
}

void depth_blend(int x, int y, uint primitive_index, ShadedData shaded)
{
	int z = shaded.z_dith >> 9;
	int dith = shaded.z_dith & 0x1ff;
	int coverage_count = shaded.coverage_count;
	u8x4 combined = shaded.combined;
	u8 shade_alpha = shaded.shade_alpha;

	uint blend_state_index = uint(state_indices.elems[primitive_index].static_depth_tmem.y);
	DerivedSetup derived = load_derived_setup(primitive_index);
	DepthBlendState depth_blend = load_depth_blend_state(blend_state_index);

	bool force_blend = (depth_blend.flags & DEPTH_BLEND_FORCE_BLEND_BIT) != 0;
	bool z_compare = (depth_blend.flags & DEPTH_BLEND_DEPTH_TEST_BIT) != 0;
	bool z_update = (depth_blend.flags & DEPTH_BLEND_DEPTH_UPDATE_BIT) != 0;
	bool image_read_enable = (depth_blend.flags & DEPTH_BLEND_IMAGE_READ_ENABLE_BIT) != 0;
	bool color_on_coverage = (depth_blend.flags & DEPTH_BLEND_COLOR_ON_COVERAGE_BIT) != 0;
	bool blend_multicycle = (depth_blend.flags & DEPTH_BLEND_MULTI_CYCLE_BIT) != 0;
	bool aa_enable = (depth_blend.flags & DEPTH_BLEND_AA_BIT) != 0;
	bool dither_en = (depth_blend.flags & DEPTH_BLEND_DITHER_ENABLE_BIT) != 0;

	bool blend_en;
	bool coverage_wrap;
	u8x2 blend_shift;

	u8x4 memory_color = decode_memory_color(image_read_enable);
	u8 memory_coverage = memory_color.a >> U8_C(5);

	bool z_pass = depth_test(z, derived.dz, derived.dz_compressed,
	                         current_depth, current_dz,
	                         coverage_count, memory_coverage,
	                         z_compare, depth_blend.z_mode,
	                         force_blend, aa_enable,
	                         blend_en, coverage_wrap, blend_shift);

	GENERIC_MESSAGE3(combined.x, combined.y, combined.z);

	// Pixel tests.
	if (z_pass && (!aa_enable || coverage_count != 0))
	{
		// Blending
		BlendInputs blender_inputs =
				BlendInputs(combined, memory_color,
							derived.fog_color, derived.blend_color, shade_alpha);

		u8x4 blend_modes = depth_blend.blend_modes0;
		if (blend_multicycle)
		{
			blender_inputs.pixel_color.rgb =
					blender(blender_inputs,
							blend_modes,
							force_blend, blend_en, color_on_coverage, coverage_wrap, blend_shift, false);
			blend_modes = depth_blend.blend_modes1;
		}
		u8x3 rgb = blender(blender_inputs,
						   blend_modes,
						   force_blend, blend_en, color_on_coverage, coverage_wrap, blend_shift, true);

		// Dither
		if (dither_en)
			rgb = rgb_dither(rgb, dith);

		// Coverage blending
		int new_coverage = blend_coverage(coverage_count, memory_coverage, blend_en, depth_blend.coverage_mode);

		GENERIC_MESSAGE3(rgb.x, rgb.y, rgb.z);

		// Writeback
		write_color(u8x4(rgb, new_coverage << 5));

		// Z-writeback.
		if (z_update)
		{
			current_depth = z_compress(z);
			current_dz = u8(derived.dz_compressed);
			current_depth_dirty = true;

			if (FB_COLOR_DEPTH_ALIAS)
				alias_depth_to_color();
		}
		else if (FB_COLOR_DEPTH_ALIAS)
			alias_color_to_depth();
	}
}

#endif