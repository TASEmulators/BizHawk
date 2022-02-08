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

#ifndef SHADING_H_
#define SHADING_H_

#ifdef RASTERIZER_SPEC_CONSTANT
const int SCALING_LOG2 = (STATIC_STATE_FLAGS >> RASTERIZATION_UPSCALING_LOG2_BIT_OFFSET) & 3;
const int SCALING_FACTOR = 1 << SCALING_LOG2;
#endif

#include "coverage.h"
#include "interpolation.h"
#include "perspective.h"
#include "texture.h"
#include "dither.h"
#include "combiner.h"

bool shade_pixel(int x, int y, uint primitive_index, out ShadedData shaded)
{
	SpanInfoOffsets span_offsets = load_span_offsets(primitive_index);
	if ((y < (SCALING_FACTOR * span_offsets.ylo)) || (y > (span_offsets.yhi * SCALING_FACTOR + (SCALING_FACTOR - 1))))
		return false;

	uint setup_flags = uint(triangle_setup.elems[primitive_index].flags);
	if (SCALING_FACTOR > 1)
	{
		if ((setup_flags & TRIANGLE_SETUP_DISABLE_UPSCALING_BIT) != 0u)
		{
			x &= ~(SCALING_FACTOR - 1);
			y &= ~(SCALING_FACTOR - 1);
		}
	}

	SpanSetup span_setup = load_span_setup(SCALING_FACTOR * span_offsets.offset + (y - SCALING_FACTOR * span_offsets.ylo));
	if (span_setup.valid_line == U16_C(0))
		return false;

	uint setup_tile = uint(triangle_setup.elems[primitive_index].tile);
	AttributeSetup attr = load_attribute_setup(primitive_index);

	uvec4 states = uvec4(state_indices.elems[primitive_index].static_depth_tmem);
	uint static_state_index = states.x;
	uint tmem_instance_index = states.z;

	StaticRasterizationState static_state = load_static_rasterization_state(static_state_index);
	uint static_state_flags = static_state.flags;
	int static_state_dither = static_state.dither;
	u8x4 combiner_inputs_rgb0 = static_state.combiner_inputs_rgb0;
	u8x4 combiner_inputs_alpha0 = static_state.combiner_inputs_alpha0;
	u8x4 combiner_inputs_rgb1 = static_state.combiner_inputs_rgb1;
	u8x4 combiner_inputs_alpha1 = static_state.combiner_inputs_alpha1;

#ifdef RASTERIZER_SPEC_CONSTANT
	if ((STATIC_STATE_FLAGS & RASTERIZATION_USE_SPECIALIZATION_CONSTANT_BIT) != 0)
	{
		static_state_flags = STATIC_STATE_FLAGS;
		static_state_dither = DITHER;

		combiner_inputs_rgb0.x = u8(COMBINER_INPUT_RGB0_MULADD);
		combiner_inputs_rgb0.y = u8(COMBINER_INPUT_RGB0_MULSUB);
		combiner_inputs_rgb0.z = u8(COMBINER_INPUT_RGB0_MUL);
		combiner_inputs_rgb0.w = u8(COMBINER_INPUT_RGB0_ADD);

		combiner_inputs_alpha0.x = u8(COMBINER_INPUT_ALPHA0_MULADD);
		combiner_inputs_alpha0.y = u8(COMBINER_INPUT_ALPHA0_MULSUB);
		combiner_inputs_alpha0.z = u8(COMBINER_INPUT_ALPHA0_MUL);
		combiner_inputs_alpha0.w = u8(COMBINER_INPUT_ALPHA0_ADD);

		combiner_inputs_rgb1.x = u8(COMBINER_INPUT_RGB1_MULADD);
		combiner_inputs_rgb1.y = u8(COMBINER_INPUT_RGB1_MULSUB);
		combiner_inputs_rgb1.z = u8(COMBINER_INPUT_RGB1_MUL);
		combiner_inputs_rgb1.w = u8(COMBINER_INPUT_RGB1_ADD);

		combiner_inputs_alpha1.x = u8(COMBINER_INPUT_ALPHA1_MULADD);
		combiner_inputs_alpha1.y = u8(COMBINER_INPUT_ALPHA1_MULSUB);
		combiner_inputs_alpha1.z = u8(COMBINER_INPUT_ALPHA1_MUL);
		combiner_inputs_alpha1.w = u8(COMBINER_INPUT_ALPHA1_ADD);
	}
#endif

	// This is a great case for specialization constants.
	bool tlut = (static_state_flags & RASTERIZATION_TLUT_BIT) != 0;
	bool tlut_type = (static_state_flags & RASTERIZATION_TLUT_TYPE_BIT) != 0;
	bool sample_quad = (static_state_flags & RASTERIZATION_SAMPLE_MODE_BIT) != 0;
	bool cvg_times_alpha = (static_state_flags & RASTERIZATION_CVG_TIMES_ALPHA_BIT) != 0;
	bool alpha_cvg_select = (static_state_flags & RASTERIZATION_ALPHA_CVG_SELECT_BIT) != 0;
	bool perspective = (static_state_flags & RASTERIZATION_PERSPECTIVE_CORRECT_BIT) != 0;
	bool tex_lod_en = (static_state_flags & RASTERIZATION_TEX_LOD_ENABLE_BIT) != 0;
	bool sharpen_lod_en = (static_state_flags & RASTERIZATION_SHARPEN_LOD_ENABLE_BIT) != 0;
	bool detail_lod_en = (static_state_flags & RASTERIZATION_DETAIL_LOD_ENABLE_BIT) != 0;
	bool aa_enable = (static_state_flags & RASTERIZATION_AA_BIT) != 0;
	bool multi_cycle = (static_state_flags & RASTERIZATION_MULTI_CYCLE_BIT) != 0;
	bool interlace_en = (static_state_flags & RASTERIZATION_INTERLACE_FIELD_BIT) != 0;
	bool fill_en = (static_state_flags & RASTERIZATION_FILL_BIT) != 0;
	bool copy_en = (static_state_flags & RASTERIZATION_COPY_BIT) != 0;
	bool alpha_test = (static_state_flags & RASTERIZATION_ALPHA_TEST_BIT) != 0;
	bool alpha_test_dither = (static_state_flags & RASTERIZATION_ALPHA_TEST_DITHER_BIT) != 0;
	bool mid_texel = (static_state_flags & RASTERIZATION_SAMPLE_MID_TEXEL_BIT) != 0;
	bool uses_texel0 = (static_state_flags & RASTERIZATION_USES_TEXEL0_BIT) != 0;
	bool uses_texel1 = (static_state_flags & RASTERIZATION_USES_TEXEL1_BIT) != 0;
	bool uses_pipelined_texel1 = (static_state_flags & RASTERIZATION_USES_PIPELINED_TEXEL1_BIT) != 0;
	bool uses_lod = (static_state_flags & RASTERIZATION_USES_LOD_BIT) != 0;
	bool convert_one = (static_state_flags & RASTERIZATION_CONVERT_ONE_BIT) != 0;
	bool bilerp0 = (static_state_flags & RASTERIZATION_BILERP_0_BIT) != 0;
	bool bilerp1 = (static_state_flags & RASTERIZATION_BILERP_1_BIT) != 0;

	if ((static_state_flags & RASTERIZATION_NEED_NOISE_BIT) != 0)
		reseed_noise(x, y, primitive_index + global_constants.fb_info.base_primitive_index);

	bool flip = (setup_flags & TRIANGLE_SETUP_FLIP_BIT) != 0;

	if (copy_en)
	{
		bool valid = x >= span_setup.start_x && x <= span_setup.end_x;
		if (!valid)
			return false;

		ivec2 st;
		int s_offset;
		interpolate_st_copy(span_setup, attr.dstzw_dx, x, perspective, flip, st, s_offset);

		uint tile0 = uint(setup_tile) & 7u;
		uint tile_info_index0 = uint(state_indices.elems[primitive_index].tile_infos[tile0]);
		TileInfo tile_info0 = load_tile_info(tile_info_index0);
#ifdef RASTERIZER_SPEC_CONSTANT
		if ((STATIC_STATE_FLAGS & RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT) != 0)
		{
			tile_info0.fmt = u8(TEX_FMT);
			tile_info0.size = u8(TEX_SIZE);
		}
#endif
		int texel0 = sample_texture_copy(tile_info0, tmem_instance_index, st, s_offset, tlut, tlut_type);
		shaded.z_dith = texel0;
		shaded.coverage_count = U8_C(COVERAGE_COPY_BIT);

		if (alpha_test && global_constants.fb_info.fb_size == 2 && (texel0 & 1) == 0)
			return false;

		return true;
	}
	else if (fill_en)
	{
		shaded.coverage_count = U8_C(COVERAGE_FILL_BIT);
		return x >= span_setup.start_x && x <= span_setup.end_x;
	}

	int coverage = compute_coverage(span_setup.xleft, span_setup.xright, x);

	// There is no way we can gain coverage here.
	// Reject work as fast as possible.
	if (coverage == 0)
		return false;

	int coverage_count = bitCount(coverage);

	// If we're not using AA, only the first coverage bit is relevant.
	if (!aa_enable && (coverage & 1) == 0)
		return false;

	DerivedSetup derived = load_derived_setup(primitive_index);

	int dx = x - span_setup.interpolation_base_x;
	int interpolation_direction = flip ? 1 : -1;

	// Interpolate attributes.
	u8x4 shade = interpolate_rgba(span_setup.rgba, attr.drgba_dx, attr.drgba_dy,
	                              dx, coverage);

	ivec2 st, st_dx, st_dy;
	int z;
	bool perspective_overflow = false;

	int tex_interpolation_direction = interpolation_direction;
	if (SCALING_FACTOR > 1 && uses_lod)
		if ((setup_flags & TRIANGLE_SETUP_NATIVE_LOD_BIT) != 0)
			tex_interpolation_direction *= SCALING_FACTOR;

	interpolate_stz(span_setup.stzw, attr.dstzw_dx, attr.dstzw_dy, dx, coverage, perspective, uses_lod,
	                tex_interpolation_direction, st, st_dx, st_dy, z, perspective_overflow);

	// Sample textures.
	uint tile0 = uint(setup_tile) & 7u;
	uint tile1 = (tile0 + 1) & 7u;
	uint max_level = uint(setup_tile) >> 3u;
	int min_lod = derived.min_lod;

	i16 lod_frac;
	if (uses_lod)
	{
		compute_lod_2cycle(tile0, tile1, lod_frac, max_level, min_lod, st, st_dx, st_dy, perspective_overflow,
		                   tex_lod_en, sharpen_lod_en, detail_lod_en);
	}

	i16x4 texel0, texel1;

	if (uses_texel0)
	{
		uint tile_info_index0 = uint(state_indices.elems[primitive_index].tile_infos[tile0]);
		TileInfo tile_info0 = load_tile_info(tile_info_index0);
#ifdef RASTERIZER_SPEC_CONSTANT
		if ((STATIC_STATE_FLAGS & RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT) != 0)
		{
			tile_info0.fmt = u8(TEX_FMT);
			tile_info0.size = u8(TEX_SIZE);
		}
#endif
		texel0 = sample_texture(tile_info0, tmem_instance_index, st, tlut, tlut_type, sample_quad, mid_texel, false, i16x4(0));
		if (!sample_quad && !bilerp0)
			texel0 = texture_convert_factors(texel0, derived.factors);
	}

	// A very awkward mechanism where we peek into the next pixel, or in some cases, the next scanline's first pixel.
	if (uses_pipelined_texel1)
	{
		bool valid_line = uint(span_setups.elems[SCALING_FACTOR * span_offsets.offset + (y - SCALING_FACTOR * span_offsets.ylo + 1)].valid_line) != 0u;
		bool long_span = span_setup.lodlength >= 8;
		bool end_span = x == (flip ? span_setup.end_x : span_setup.start_x);

		if (end_span && long_span && valid_line)
		{
			ivec3 stw = span_setups.elems[SCALING_FACTOR * span_offsets.offset + (y - SCALING_FACTOR * span_offsets.ylo + 1)].stzw.xyw >> 16;
			if (perspective)
			{
				bool st_overflow;
				st = perspective_divide(stw, st_overflow);
			}
			else
				st = no_perspective_divide(stw);
		}
		else
			st = interpolate_st_single(span_setup.stzw, attr.dstzw_dx, dx + interpolation_direction * SCALING_FACTOR, perspective);

		tile1 = tile0;
		uses_texel1 = true;
	}

	if (uses_texel1)
	{
		if (convert_one && !bilerp1)
		{
			texel1 = texture_convert_factors(texel0, derived.factors);
		}
		else
		{
			uint tile_info_index1 = uint(state_indices.elems[primitive_index].tile_infos[tile1]);
			TileInfo tile_info1 = load_tile_info(tile_info_index1);
#ifdef RASTERIZER_SPEC_CONSTANT
			if ((STATIC_STATE_FLAGS & RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT) != 0)
			{
				tile_info1.fmt = u8(TEX_FMT);
				tile_info1.size = u8(TEX_SIZE);
			}
#endif
			texel1 = sample_texture(tile_info1, tmem_instance_index, st, tlut, tlut_type, sample_quad, mid_texel,
			                        convert_one, texel0);

			if (!sample_quad && !tlut && !bilerp1)
				texel1 = texture_convert_factors(texel1, derived.factors);
		}
	}

	int rgb_dith, alpha_dith;
	dither_coefficients(x, y >> int(interlace_en), static_state_dither >> 2, static_state_dither & 3, rgb_dith, alpha_dith);

	// Run combiner.
	u8x4 combined;
	u8 alpha_reference;
	if (multi_cycle)
	{
		CombinerInputs combined_inputs =
				CombinerInputs(derived.constant_muladd0, derived.constant_mulsub0, derived.constant_mul0, derived.constant_add0,
				               shade, u8x4(0), texel0, texel1, lod_frac, noise_get_combiner());

		combined_inputs.combined = combiner_cycle0(combined_inputs,
		                                           combiner_inputs_rgb0,
		                                           combiner_inputs_alpha0,
		                                           alpha_dith, coverage_count, cvg_times_alpha, alpha_cvg_select,
		                                           alpha_test, alpha_reference);

		combined_inputs.constant_muladd = derived.constant_muladd1;
		combined_inputs.constant_mulsub = derived.constant_mulsub1;
		combined_inputs.constant_mul = derived.constant_mul1;
		combined_inputs.constant_add = derived.constant_add1;

		// Pipelining, texel1 is promoted to texel0 in cycle1.
		// I don't think hardware ever intended for you to access texels in second cycle due to this nature.
		i16x4 tmp_texel = combined_inputs.texel0;
		combined_inputs.texel0 = combined_inputs.texel1;
		// Following the pipelining, texel1 should become texel0 of next pixel,
		// but let's not go there ...
		combined_inputs.texel1 = tmp_texel;

		combined = u8x4(combiner_cycle1(combined_inputs,
		                                combiner_inputs_rgb1,
		                                combiner_inputs_alpha1,
		                                alpha_dith, coverage_count, cvg_times_alpha, alpha_cvg_select));
	}
	else
	{
		CombinerInputs combined_inputs =
				CombinerInputs(derived.constant_muladd1, derived.constant_mulsub1, derived.constant_mul1, derived.constant_add1,
				               shade, u8x4(0), texel0, texel1, lod_frac, noise_get_combiner());

		combined = u8x4(combiner_cycle1(combined_inputs,
		                                combiner_inputs_rgb1,
		                                combiner_inputs_alpha1,
		                                alpha_dith, coverage_count, cvg_times_alpha, alpha_cvg_select));

		alpha_reference = combined.a;
	}

	// After combiner, color can be modified to 0 through alpha-to-cvg, so check for potential write_enable here.
	// If we're not using AA, the first coverage bit is used instead, coverage count is ignored.
	if (aa_enable && coverage_count == 0)
		return false;

	if (alpha_test)
	{
		u8 alpha_threshold;
		if (alpha_test_dither)
			alpha_threshold = noise_get_blend_threshold();
		else
			alpha_threshold = derived.blend_color.a;

		if (alpha_reference < alpha_threshold)
			return false;
	}

	shaded.combined = combined;
	shaded.z_dith = (z << 9) | rgb_dith;
	shaded.coverage_count = u8(coverage_count);
	// Shade alpha needs to be passed separately since it might affect the blending stage.
	shaded.shade_alpha = u8(min(shade.a + alpha_dith, 0xff));
	return true;
}

#endif
