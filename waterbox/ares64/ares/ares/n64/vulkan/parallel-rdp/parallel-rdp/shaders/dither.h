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

#ifndef DITHER_H_
#define DITHER_H_

const u8 dither_matrices[2][16] = u8[][](
		u8[](U8_C(0), U8_C(6), U8_C(1), U8_C(7), U8_C(4), U8_C(2), U8_C(5), U8_C(3), U8_C(3), U8_C(5), U8_C(2), U8_C(4), U8_C(7), U8_C(1), U8_C(6), U8_C(0)),
		u8[](U8_C(0), U8_C(4), U8_C(1), U8_C(5), U8_C(4), U8_C(0), U8_C(5), U8_C(1), U8_C(3), U8_C(7), U8_C(2), U8_C(6), U8_C(7), U8_C(3), U8_C(6), U8_C(2)));

u8x3 rgb_dither(ivec3 orig_rgb, int dith)
{
	ivec3 rgb_dith = (ivec3(dith) >> ivec3(0, 3, 6)) & 7;
	ivec3 rgb = mix((orig_rgb & 0xf8) + 8, ivec3(255), greaterThan(orig_rgb, ivec3(247)));
	ivec3 replace_sign = (rgb_dith - (orig_rgb & 7)) >> 31;
	ivec3 dither_diff = rgb - orig_rgb;
	rgb = orig_rgb + (dither_diff & replace_sign);
	return u8x3(rgb & 0xff);
}

void dither_coefficients(int x, int y, int dither_mode_rgb, int dither_mode_alpha, out int rgb_dither, out int alpha_dither)
{
	const int DITHER_SPLAT = (1 << 0) | (1 << 3) | (1 << 6);

	if (dither_mode_rgb < 2)
		rgb_dither = int(dither_matrices[dither_mode_rgb][(y & 3) * 4 + (x & 3)]) * DITHER_SPLAT;
	else if (dither_mode_rgb == 2)
		rgb_dither = noise_get_dither_color();
	else
		rgb_dither = 0;

	if (dither_mode_alpha == 3)
		alpha_dither = 0;
	else
	{
		if (dither_mode_alpha == 2)
		{
			alpha_dither = noise_get_dither_alpha();
		}
		else
		{
			alpha_dither = dither_mode_rgb >= 2 ?
				int(dither_matrices[dither_mode_rgb & 1][(y & 3) * 4 + (x & 3)]) : (rgb_dither & 7);

			if (dither_mode_alpha == 1)
				alpha_dither = ~alpha_dither & 7;
		}
	}
}

#endif