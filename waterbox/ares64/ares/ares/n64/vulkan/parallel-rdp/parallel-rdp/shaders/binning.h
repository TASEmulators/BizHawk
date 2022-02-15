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

#ifndef BINNING_H_
#define BINNING_H_

// There are 4 critical Y coordinates to test when binning. Top, bottom, mid, and mid - 1.

const int SUBPIXELS_Y = 4;

ivec4 quantize_x(ivec4 x)
{
	return x >> 15;
}

int minimum4(ivec4 v)
{
	ivec2 minimum2 = min(v.xy, v.zw);
	return min(minimum2.x, minimum2.y);
}

int maximum4(ivec4 v)
{
	ivec2 maximum2 = max(v.xy, v.zw);
	return max(maximum2.x, maximum2.y);
}

ivec4 madd_32_64(ivec4 a, int b, int c, out ivec4 hi_bits)
{
	ivec4 lo, hi;
	imulExtended(a, ivec4(b), hi, lo);
	uvec4 carry;
	lo = ivec4(uaddCarry(lo, uvec4(c), carry));
	hi += ivec4(carry);
	hi_bits = hi;
	return lo;
}

ivec2 interpolate_xs(TriangleSetup setup, ivec4 ys, bool flip, int scaling)
{
	int yh_interpolation_base = setup.yh & ~(SUBPIXELS_Y - 1);
	int ym_interpolation_base = setup.ym;

	yh_interpolation_base *= scaling;
	ym_interpolation_base *= scaling;

	// Interpolate in 64-bit so we can detect quirky overflow scenarios.
	ivec4 xh_hi, xm_hi, xl_hi;
	ivec4 xh = madd_32_64(ys - yh_interpolation_base, setup.dxhdy, scaling * setup.xh, xh_hi);
	ivec4 xm = madd_32_64(ys - yh_interpolation_base, setup.dxmdy, scaling * setup.xm, xm_hi);
	ivec4 xl = madd_32_64(ys - ym_interpolation_base, setup.dxldy, scaling * setup.xl, xl_hi);
	xl = mix(xl, xm, lessThan(ys, ivec4(scaling * setup.ym)));
	xl_hi = mix(xl_hi, xm_hi, lessThan(ys, ivec4(scaling * setup.ym)));

	// Handle overflow scenarios. Saturate 64-bit signed to 32-bit signed without 64-bit math.
	xh = mix(xh, ivec4(0x7fffffff), greaterThan(xh_hi, ivec4(0)));
	xh = mix(xh, ivec4(-0x80000000), lessThan(xh_hi, ivec4(-1)));
	xl = mix(xl, ivec4(0x7fffffff), greaterThan(xl_hi, ivec4(0)));
	xl = mix(xl, ivec4(-0x80000000), lessThan(xl_hi, ivec4(-1)));

	ivec4 xh_shifted = quantize_x(xh);
	ivec4 xl_shifted = quantize_x(xl);

	ivec4 xleft, xright;
	if (flip)
	{
		xleft = xh_shifted;
		xright = xl_shifted;
	}
	else
	{
		xleft = xl_shifted;
		xright = xh_shifted;
	}

	// If one of the results are out of range, we have overflow, and we need to be conservative when binning.
	int max_range = maximum4(max(abs(xleft), abs(xright)));
	ivec2 range;
	if (max_range <= 2047 * scaling)
		range = ivec2(minimum4(xleft), maximum4(xright));
	else
		range = ivec2(0, 0x7fffffff);

	return range;
}

bool bin_primitive(TriangleSetup setup, ivec2 lo, ivec2 hi, int scaling)
{
	int start_y = lo.y * SUBPIXELS_Y;
	int end_y = (hi.y * SUBPIXELS_Y) + (SUBPIXELS_Y - 1);

	// First, we clip start/end against y_lo, y_hi.
	start_y = max(start_y, scaling * int(setup.yh));
	end_y = min(end_y, scaling * int(setup.yl) - 1);

	// Y is clipped out, exit early.
	if (end_y < start_y)
		return false;

	bool flip = (setup.flags & TRIANGLE_SETUP_FLIP_BIT) != 0;

	// Sample the X ranges for min and max Y, and potentially the mid-point as well.
	ivec4 ys = ivec4(start_y, end_y, clamp(setup.ym * scaling + ivec2(-1, 0), ivec2(start_y), ivec2(end_y)));
	ivec2 x_range = interpolate_xs(setup, ys, flip, scaling);

	x_range.x = max(x_range.x, lo.x);
	x_range.y = min(x_range.y, hi.x);
	return x_range.x <= x_range.y;
}

#endif