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
 *
 */

#ifndef PERSPECTIVE_H_
#define PERSPECTIVE_H_

const i16x2 perspective_table[64] = i16x2[](
	i16x2(0x4000, -252 * 4), i16x2(0x3f04, -244 * 4), i16x2(0x3e10, -238 * 4), i16x2(0x3d22, -230 * 4),
	i16x2(0x3c3c, -223 * 4), i16x2(0x3b5d, -218 * 4), i16x2(0x3a83, -210 * 4), i16x2(0x39b1, -205 * 4),
	i16x2(0x38e4, -200 * 4), i16x2(0x381c, -194 * 4), i16x2(0x375a, -189 * 4), i16x2(0x369d, -184 * 4),
	i16x2(0x35e5, -179 * 4), i16x2(0x3532, -175 * 4), i16x2(0x3483, -170 * 4), i16x2(0x33d9, -166 * 4),
	i16x2(0x3333, -162 * 4), i16x2(0x3291, -157 * 4), i16x2(0x31f4, -155 * 4), i16x2(0x3159, -150 * 4),
	i16x2(0x30c3, -147 * 4), i16x2(0x3030, -143 * 4), i16x2(0x2fa1, -140 * 4), i16x2(0x2f15, -137 * 4),
	i16x2(0x2e8c, -134 * 4), i16x2(0x2e06, -131 * 4), i16x2(0x2d83, -128 * 4), i16x2(0x2d03, -125 * 4),
	i16x2(0x2c86, -123 * 4), i16x2(0x2c0b, -120 * 4), i16x2(0x2b93, -117 * 4), i16x2(0x2b1e, -115 * 4),
	i16x2(0x2aab, -113 * 4), i16x2(0x2a3a, -110 * 4), i16x2(0x29cc, -108 * 4), i16x2(0x2960, -106 * 4),
	i16x2(0x28f6, -104 * 4), i16x2(0x288e, -102 * 4), i16x2(0x2828, -100 * 4), i16x2(0x27c4, -98 * 4),
	i16x2(0x2762, -96 * 4),  i16x2(0x2702, -94 * 4),  i16x2(0x26a4, -92 * 4),  i16x2(0x2648, -91 * 4),
	i16x2(0x25ed, -89 * 4),  i16x2(0x2594, -87 * 4),  i16x2(0x253d, -86 * 4),  i16x2(0x24e7, -85 * 4),
	i16x2(0x2492, -83 * 4),  i16x2(0x243f, -81 * 4),  i16x2(0x23ee, -80 * 4),  i16x2(0x239e, -79 * 4),
	i16x2(0x234f, -77 * 4),  i16x2(0x2302, -76 * 4),  i16x2(0x22b6, -74 * 4),  i16x2(0x226c, -74 * 4),
	i16x2(0x2222, -72 * 4),  i16x2(0x21da, -71 * 4),  i16x2(0x2193, -70 * 4),  i16x2(0x214d, -69 * 4),
	i16x2(0x2108, -67 * 4),  i16x2(0x20c5, -67 * 4),  i16x2(0x2082, -65 * 4),  i16x2(0x2041, -65 * 4)
);

ivec2 perspective_get_lut(int w)
{
	int shift = min(14 - findMSB(w), 14);
	int normout = (w << shift) & 0x3fff;
	int wnorm = normout & 0xff;
	ivec2 table = ivec2(perspective_table[normout >> 8]);
	int rcp = ((table.y * wnorm) >> 10) + table.x;
	return ivec2(rcp, shift);
}

ivec2 no_perspective_divide(ivec3 stw)
{
	return stw.xy;
}

// s16 divided by s1.15.
// Classic approximation of a (x * rcp) >> shift with a LUT to find rcp.
ivec2 perspective_divide(ivec3 stw, inout bool overflow)
{
	int w = stw.z;
	bool w_carry = w <= 0;
	w &= 0x7fff;

	ivec2 table = perspective_get_lut(w);
	int shift = table.y;
	ivec2 prod = stw.xy * table.x;

	int temp_mask = ((1 << 30) - 1) & -((1 << 29) >> shift);
	ivec2 out_of_bounds = prod & temp_mask;

	ivec2 temp;
	if (shift != 14)
		temp = prod = prod >> (13 - shift);
	else
		temp = prod << 1;

	if (any(notEqual(out_of_bounds, ivec2(0))))
	{
		if (out_of_bounds.x != temp_mask && out_of_bounds.x != 0)
		{
			if ((prod.x & (1 << 29)) == 0)
				temp.x = 0x7fff;
			else
				temp.x = -0x8000;
			overflow = true;
		}

		if (out_of_bounds.y != temp_mask && out_of_bounds.y != 0)
		{
			if ((prod.y & (1 << 29)) == 0)
				temp.y = 0x7fff;
			else
				temp.y = -0x8000;
			overflow = true;
		}
	}

	if (w_carry)
	{
		temp = ivec2(0x7fff);
		overflow = true;
	}

	// Perspective divide produces a 17-bit signed coordinate, which is later clamped to 16-bit signed.
	// However, the LOD computation happens in 17 bits ...
	return clamp(temp, ivec2(-0x10000), ivec2(0xffff));
}

#endif