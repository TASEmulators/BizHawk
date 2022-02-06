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

#ifndef COMBINER_H_
#define COMBINER_H_

#include "clamping.h"

ivec4 special_expand(ivec4 value)
{
	// Special sign-extend without explicit clamp.
	return bitfieldExtract(value - 0x80, 0, 9) + 0x80;
}

i16x4 combiner_equation(ivec4 a, ivec4 b, ivec4 c, ivec4 d)
{
	// Sign-extend multiplier to 9 bits.
	c = bitfieldExtract(c, 0, 9);

	// Need this to deal with very specific 9-bit sign bits ...
	a = special_expand(a);
	b = special_expand(b);
	d = special_expand(d);

	ivec4 color = (a - b) * c;
	color += 0x80;
	return i16x4(color >> 8) + i16x4(d);
}

struct CombinerInputs
{
	u8x4 constant_muladd;
	u8x4 constant_mulsub;
	u8x4 constant_mul;
	u8x4 constant_add;

	u8x4 shade;
	i16x4 combined;
	i16x4 texel0;
	i16x4 texel1;
	i16 lod_frac;
	i16 noise;
};

const int RGB_MULADD_COMBINED = 0;
const int RGB_MULADD_TEXEL0 = 1;
const int RGB_MULADD_TEXEL1 = 2;
const int RGB_MULADD_SHADE = 4;
const int RGB_MULADD_ONE = 6;
const int RGB_MULADD_NOISE = 7;

const int RGB_MULSUB_COMBINED = 0;
const int RGB_MULSUB_TEXEL0 = 1;
const int RGB_MULSUB_TEXEL1 = 2;
const int RGB_MULSUB_SHADE = 4;
const int RGB_MULSUB_K4 = 7;

const int RGB_MUL_COMBINED = 0;
const int RGB_MUL_TEXEL0 = 1;
const int RGB_MUL_TEXEL1 = 2;
const int RGB_MUL_SHADE = 4;
const int RGB_MUL_COMBINED_ALPHA = 7;
const int RGB_MUL_TEXEL0_ALPHA = 8;
const int RGB_MUL_TEXEL1_ALPHA = 9;
const int RGB_MUL_SHADE_ALPHA = 11;
const int RGB_MUL_LOD_FRAC = 13;
const int RGB_MUL_K5 = 15;

const int RGB_ADD_COMBINED = 0;
const int RGB_ADD_TEXEL0 = 1;
const int RGB_ADD_TEXEL1 = 2;
const int RGB_ADD_SHADE = 4;
const int RGB_ADD_ONE = 6;

const int ALPHA_ADDSUB_COMBINED = 0;
const int ALPHA_ADDSUB_TEXEL0_ALPHA = 1;
const int ALPHA_ADDSUB_TEXEL1_ALPHA = 2;
const int ALPHA_ADDSUB_SHADE_ALPHA = 4;
const int ALPHA_ADDSUB_ONE = 6;

const int ALPHA_MUL_LOD_FRAC = 0;
const int ALPHA_MUL_TEXEL0_ALPHA = 1;
const int ALPHA_MUL_TEXEL1_ALPHA = 2;
const int ALPHA_MUL_SHADE_ALPHA = 4;

ivec4 select_muladd(CombinerInputs inputs, int selector_rgb, int selector_alpha)
{
	ivec3 res;
	switch (selector_rgb)
	{
	case RGB_MULADD_COMBINED: res = inputs.combined.rgb; break;
	case RGB_MULADD_TEXEL0: res = inputs.texel0.rgb; break;
	case RGB_MULADD_TEXEL1: res = inputs.texel1.rgb; break;
	case RGB_MULADD_SHADE: res = inputs.shade.rgb; break;
	case RGB_MULADD_NOISE: res = ivec3(inputs.noise); break;
	case RGB_MULADD_ONE: res = ivec3(0x100); break;
	default: res = inputs.constant_muladd.rgb; break;
	}

	int alpha;
	switch (selector_alpha)
	{
	case ALPHA_ADDSUB_COMBINED: alpha = inputs.combined.a; break;
	case ALPHA_ADDSUB_TEXEL0_ALPHA: alpha = inputs.texel0.a; break;
	case ALPHA_ADDSUB_TEXEL1_ALPHA: alpha = inputs.texel1.a; break;
	case ALPHA_ADDSUB_SHADE_ALPHA: alpha = inputs.shade.a; break;
	case ALPHA_ADDSUB_ONE: alpha = 0x100; break;
	default: alpha = inputs.constant_muladd.a; break;
	}
	return ivec4(res, alpha);
}

ivec4 select_mulsub(CombinerInputs inputs, int selector_rgb, int selector_alpha)
{
	ivec3 res;
	switch (selector_rgb)
	{
	case RGB_MULSUB_COMBINED: res = inputs.combined.rgb; break;
	case RGB_MULSUB_TEXEL0: res = inputs.texel0.rgb; break;
	case RGB_MULSUB_TEXEL1: res = inputs.texel1.rgb; break;
	case RGB_MULSUB_SHADE: res = inputs.shade.rgb; break;
	case RGB_MULSUB_K4: res = ivec3((int(inputs.constant_mulsub.g) << 8) | inputs.constant_mulsub.b); break;
	default: res = inputs.constant_mulsub.rgb; break;
	}

	int alpha;
	switch (selector_alpha)
	{
	case ALPHA_ADDSUB_COMBINED: alpha = inputs.combined.a; break;
	case ALPHA_ADDSUB_TEXEL0_ALPHA: alpha = inputs.texel0.a; break;
	case ALPHA_ADDSUB_TEXEL1_ALPHA: alpha = inputs.texel1.a; break;
	case ALPHA_ADDSUB_SHADE_ALPHA: alpha = inputs.shade.a; break;
	case ALPHA_ADDSUB_ONE: alpha = 0x100; break;
	default: alpha = inputs.constant_mulsub.a; break;
	}
	return ivec4(res, alpha);
}

ivec4 select_mul(CombinerInputs inputs, int selector_rgb, int selector_alpha)
{
	ivec3 res;
	switch (selector_rgb)
	{
	case RGB_MUL_COMBINED: res = inputs.combined.rgb; break;
	case RGB_MUL_COMBINED_ALPHA: res = inputs.combined.aaa; break;
	case RGB_MUL_TEXEL0: res = inputs.texel0.rgb; break;
	case RGB_MUL_TEXEL1: res = inputs.texel1.rgb; break;
	case RGB_MUL_SHADE: res = inputs.shade.rgb; break;
	case RGB_MUL_TEXEL0_ALPHA: res = inputs.texel0.aaa; break;
	case RGB_MUL_TEXEL1_ALPHA: res = inputs.texel1.aaa; break;
	case RGB_MUL_SHADE_ALPHA: res = inputs.shade.aaa; break;
	case RGB_MUL_LOD_FRAC: res = ivec3(inputs.lod_frac); break;
	case RGB_MUL_K5: res = ivec3((int(inputs.constant_mul.g) << 8) | inputs.constant_mul.b); break;
	default: res = inputs.constant_mul.rgb; break;
	}

	int alpha;
	switch (selector_alpha)
	{
	case ALPHA_MUL_LOD_FRAC: alpha = inputs.lod_frac; break;
	case ALPHA_MUL_TEXEL0_ALPHA: alpha = inputs.texel0.a; break;
	case ALPHA_MUL_TEXEL1_ALPHA: alpha = inputs.texel1.a; break;
	case ALPHA_MUL_SHADE_ALPHA: alpha = inputs.shade.a; break;
	default: alpha = inputs.constant_mul.a; break;
	}
	return ivec4(res, alpha);
}

ivec4 select_add(CombinerInputs inputs, int selector_rgb, int selector_alpha)
{
	ivec3 res;
	switch (selector_rgb)
	{
	case RGB_ADD_COMBINED: res = inputs.combined.rgb; break;
	case RGB_ADD_TEXEL0: res = inputs.texel0.rgb; break;
	case RGB_ADD_TEXEL1: res = inputs.texel1.rgb; break;
	case RGB_ADD_SHADE: res = inputs.shade.rgb; break;
	case RGB_ADD_ONE: res = ivec3(0x100); break;
	default: res = inputs.constant_add.rgb; break;
	}

	int alpha;
	switch (selector_alpha)
	{
	case ALPHA_ADDSUB_COMBINED: alpha = inputs.combined.a; break;
	case ALPHA_ADDSUB_TEXEL0_ALPHA: alpha = inputs.texel0.a; break;
	case ALPHA_ADDSUB_TEXEL1_ALPHA: alpha = inputs.texel1.a; break;
	case ALPHA_ADDSUB_SHADE_ALPHA: alpha = inputs.shade.a; break;
	case ALPHA_ADDSUB_ONE: alpha = 0x100; break;
	default: alpha = inputs.constant_add.a; break;
	}
	return ivec4(res, alpha);
}

i16x4 combiner_cycle0(CombinerInputs inputs, u8x4 combiner_inputs_rgb, u8x4 combiner_inputs_alpha, int alpha_dith,
                      int coverage, bool cvg_times_alpha, bool alpha_cvg_select, bool alpha_test, out u8 alpha_test_reference)
{
	ivec4 muladd = select_muladd(inputs, combiner_inputs_rgb.x, combiner_inputs_alpha.x);
	ivec4 mulsub = select_mulsub(inputs, combiner_inputs_rgb.y, combiner_inputs_alpha.y);
	ivec4 mul = select_mul(inputs, combiner_inputs_rgb.z, combiner_inputs_alpha.z);
	ivec4 add = select_add(inputs, combiner_inputs_rgb.w, combiner_inputs_alpha.w);

	i16x4 combined = combiner_equation(muladd, mulsub, mul, add);

	if (alpha_test)
	{
		int clamped_alpha = clamp_9bit(combined.a);
		// Expands 0xff to 0x100 to avoid having to divide by 2**n - 1.
		int expanded_alpha = clamped_alpha + ((clamped_alpha + 1) >> 8);

		if (alpha_cvg_select)
		{
			int modulated_alpha;
			if (cvg_times_alpha)
				modulated_alpha = (expanded_alpha * coverage + 4) >> 3;
			else
				modulated_alpha = coverage << 5;
			expanded_alpha = modulated_alpha;
		}
		else
			expanded_alpha += alpha_dith;

		alpha_test_reference = u8(clamp(expanded_alpha, 0, 0xff));
	}
	else
		alpha_test_reference = U8_C(0);

	return combined;
}

i16x4 combiner_cycle1(CombinerInputs inputs, u8x4 combiner_inputs_rgb, u8x4 combiner_inputs_alpha, int alpha_dith,
		              inout int coverage, bool cvg_times_alpha, bool alpha_cvg_select)
{
	ivec4 muladd = select_muladd(inputs, combiner_inputs_rgb.x, combiner_inputs_alpha.x);
	ivec4 mulsub = select_mulsub(inputs, combiner_inputs_rgb.y, combiner_inputs_alpha.y);
	ivec4 mul = select_mul(inputs, combiner_inputs_rgb.z, combiner_inputs_alpha.z);
	ivec4 add = select_add(inputs, combiner_inputs_rgb.w, combiner_inputs_alpha.w);

	i16x4 combined = combiner_equation(muladd, mulsub, mul, add);

	combined = clamp_9bit_notrunc(combined);

	// Expands 0xff to 0x100 to avoid having to divide by 2**n - 1.
	int expanded_alpha = combined.a + ((combined.a + 1) >> 8);

	int modulated_alpha;
	if (cvg_times_alpha)
	{
		modulated_alpha = (expanded_alpha * coverage + 4) >> 3;
		coverage = modulated_alpha >> 5;
	}
	else
		modulated_alpha = coverage << 5;

	if (alpha_cvg_select)
		expanded_alpha = modulated_alpha;
	else
		expanded_alpha += alpha_dith;

	combined.a = i16(clamp(expanded_alpha, 0, 0xff));

	return combined;
}

#endif