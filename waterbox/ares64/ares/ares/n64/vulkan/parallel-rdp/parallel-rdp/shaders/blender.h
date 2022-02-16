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

#ifndef BLENDER_H_
#define BLENDER_H_

struct BlendInputs
{
	u8x4 pixel_color;
	u8x4 memory_color;
	u8x4 fog_color;
	u8x4 blend_color;
	u8 shade_alpha;
};

const int BLEND_MODE_1A_PIXEL_COLOR = 0;
const int BLEND_MODE_1A_MEMORY_COLOR = 1;
const int BLEND_MODE_1A_BLEND_COLOR = 2;
const int BLEND_MODE_1A_FOG_COLOR = 3;

const int BLEND_MODE_1B_PIXEL_ALPHA = 0;
const int BLEND_MODE_1B_FOG_ALPHA = 1;
const int BLEND_MODE_1B_SHADE_ALPHA = 2;
const int BLEND_MODE_1B_ZERO = 3;

const int BLEND_MODE_2A_PIXEL_COLOR = 0;
const int BLEND_MODE_2A_MEMORY_COLOR = 1;
const int BLEND_MODE_2A_BLEND_COLOR = 2;
const int BLEND_MODE_2A_FOG_COLOR = 3;

const int BLEND_MODE_2B_INV_PIXEL_ALPHA = 0;
const int BLEND_MODE_2B_MEMORY_ALPHA = 1;
const int BLEND_MODE_2B_ONE = 2;
const int BLEND_MODE_2B_ZERO = 3;

u8x3 blender(BlendInputs inputs, u8x4 blend_modes,
             bool force_blend, bool blend_en, bool color_on_coverage, bool coverage_wrap, u8x2 blend_shift,
             bool final_cycle)
{
	u8x3 rgb1;
	switch (int(blend_modes.z))
	{
	case BLEND_MODE_2A_PIXEL_COLOR: rgb1 = inputs.pixel_color.rgb; break;
	case BLEND_MODE_2A_MEMORY_COLOR: rgb1 = inputs.memory_color.rgb; break;
	case BLEND_MODE_2A_BLEND_COLOR: rgb1 = inputs.blend_color.rgb; break;
	case BLEND_MODE_2A_FOG_COLOR: rgb1 = inputs.fog_color.rgb; break;
	}

	if (final_cycle)
	{
		if (color_on_coverage && !coverage_wrap)
			return rgb1;
	}

	u8x3 rgb0;
	switch (int(blend_modes.x))
	{
	case BLEND_MODE_1A_PIXEL_COLOR: rgb0 = inputs.pixel_color.rgb; break;
	case BLEND_MODE_1A_MEMORY_COLOR: rgb0 = inputs.memory_color.rgb; break;
	case BLEND_MODE_1A_BLEND_COLOR: rgb0 = inputs.blend_color.rgb; break;
	case BLEND_MODE_1A_FOG_COLOR: rgb0 = inputs.fog_color.rgb; break;
	}

	if (final_cycle)
	{
		if (!blend_en || (blend_modes.y == BLEND_MODE_1B_PIXEL_ALPHA &&
						  blend_modes.w == BLEND_MODE_2B_INV_PIXEL_ALPHA &&
						  inputs.pixel_color.a == U8_C(0xff)))
		{
			return rgb0;
		}
	}

	u8 a0;
	u8 a1;

	switch (int(blend_modes.y))
	{
	case BLEND_MODE_1B_PIXEL_ALPHA: a0 = inputs.pixel_color.a; break;
	case BLEND_MODE_1B_FOG_ALPHA: a0 = inputs.fog_color.a; break;
	case BLEND_MODE_1B_SHADE_ALPHA: a0 = inputs.shade_alpha; break;
	case BLEND_MODE_1B_ZERO: a0 = U8_C(0); break;
	}

	switch (int(blend_modes.w))
	{
	case BLEND_MODE_2B_INV_PIXEL_ALPHA: a1 = ~a0 & U8_C(0xff); break;
	case BLEND_MODE_2B_MEMORY_ALPHA: a1 = inputs.memory_color.a; break;
	case BLEND_MODE_2B_ONE: a1 = U8_C(0xff); break;
	case BLEND_MODE_2B_ZERO: a1 = U8_C(0); break;
	}

	a0 >>= U8_C(3);
	a1 >>= U8_C(3);

	if (blend_modes.w == BLEND_MODE_2B_MEMORY_ALPHA)
	{
		a0 = (a0 >> blend_shift.x) & U8_C(0x3c);
		a1 = (a1 >> blend_shift.y) | U8_C(3);
	}

	i16x3 blended = i16x3(rgb0) * i16(a0) + i16x3(rgb1) * (i16(a1) + I16_C(1));

	if (!final_cycle || force_blend)
	{
		rgb0 = u8x3(blended >> I16_C(5));
	}
	else
	{
		// Serious funk here. Somehow the RDP implemented a divider to deal with weighted average.
		// Typically relevant when using blender shifters from interpenetrating Z mode.
		// Under normal condition, this is implemented as a straight integer divider, but
		// for edge cases, we need a look-up table. The results make no sense.
		int blend_sum = (int(a0) >> 2) + (int(a1) >> 2) + 1;
		blended >>= I16_C(2);
		blended &= I16_C(0x7ff);

		rgb0.r = u8(texelFetch(uBlenderDividerLUT, (blend_sum << 11) | blended.x).x);
		rgb0.g = u8(texelFetch(uBlenderDividerLUT, (blend_sum << 11) | blended.y).x);
		rgb0.b = u8(texelFetch(uBlenderDividerLUT, (blend_sum << 11) | blended.z).x);
	}

	return rgb0 & U8_C(0xff);
}

#endif