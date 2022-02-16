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

#ifndef NOISE_H_
#define NOISE_H_

u16 seeded_noise = U16_C(0);

// From: https://www.shadertoy.com/view/XlXcW4 with slight modifications.
void reseed_noise(uint x, uint y, uint primitive_offset)
{
	const uint NOISE_PRIME = 1103515245u;
	uvec3 seed = uvec3(x, y, primitive_offset);
	seed = ((seed >> 8u) ^ seed.yzx) * NOISE_PRIME;
	seed = ((seed >> 8u) ^ seed.yzx) * NOISE_PRIME;
	seed = ((seed >> 8u) ^ seed.yzx) * NOISE_PRIME;
	seeded_noise = u16(seed.x >> 16u);
}

i16 noise_get_combiner()
{
	return i16(((seeded_noise & U16_C(7u)) << U16_C(6u)) | U16_C(0x20u));
}

int noise_get_dither_alpha()
{
	return int(seeded_noise & U16_C(7u));
}

int noise_get_dither_color()
{
	// 3 bits of noise for RGB separately.
	return int(seeded_noise & U16_C(0x1ff));
}

u8 noise_get_blend_threshold()
{
	return u8(seeded_noise & U16_C(0xffu));
}

uvec3 noise_get_full_gamma_dither()
{
	uint seed = seeded_noise;
	return uvec3(seed & 0x3f, (seed >> 6u) & 0x3f, ((seed >> 9u) & 0x38) | (seed & 7u));
}

uvec3 noise_get_partial_gamma_dither()
{
	return (uvec3(seeded_noise) >> uvec3(0, 1, 2)) & 1u;
}

#endif
